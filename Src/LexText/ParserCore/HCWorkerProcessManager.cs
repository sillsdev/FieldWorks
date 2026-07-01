// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Spawns and watches the out-of-process HermitCrab worker (RUSTIFY-fieldworks-worker-design.md
	/// §2/§4). Modeled directly on FLExBridgeHelper.cs's Process.Start + WaitForExit watchdog
	/// pattern (Src\Common\FwUtils\FLExBridgeHelper.cs) - that is this codebase's existing
	/// precedent for "spawn a helper process and notice if it dies," so this doesn't invent a new
	/// one. One instance is expected to live for the lifetime of a FieldWorks process (owned by
	/// HCWorkerClient); it does not itself talk WCF - that's HCWorkerClient's job, kept separate so
	/// process lifecycle and channel lifecycle can fail/retry independently, matching the design's
	/// architecture diagram (HCWorkerProcessManager box distinct from the client proxy box).
	/// </summary>
	public class HCWorkerProcessManager : IDisposable
	{
		private const string WorkerExeName = "HCWorker.exe";

		private readonly object m_lock = new object();
		private Process m_process;
		private string m_pipeName;

		/// <summary>
		/// Pipe name of the currently running worker, or null if none is running. Unique per
		/// FieldWorks process (not per-launch) so a respawned worker after a crash still gets a
		/// fresh, non-colliding pipe name.
		/// </summary>
		public string PipeName => m_pipeName;

		public bool IsRunning
		{
			get
			{
				lock (m_lock)
				{
					return m_process != null && !m_process.HasExited;
				}
			}
		}

		/// <summary>
		/// Starts the worker if it is not already running (design §4: lazy start on first HC
		/// parse request per session, not eagerly at FieldWorks startup). Returns the pipe name to
		/// connect to. Safe to call repeatedly/concurrently.
		/// </summary>
		public string EnsureStarted()
		{
			lock (m_lock)
			{
				if (m_process != null && !m_process.HasExited)
					return m_pipeName;

				m_pipeName = "HCWorker_" + Guid.NewGuid().ToString("N");
				string exePath = Path.Combine(FwDirectoryFinder.ExeOrDllDirectory, WorkerExeName);

				var startInfo = new ProcessStartInfo
				{
					UseShellExecute = false,
					FileName = exePath,
					Arguments = $"{m_pipeName} {Process.GetCurrentProcess().Id}",
					CreateNoWindow = true,
					RedirectStandardOutput = true
				};

				var process = new Process { StartInfo = startInfo };
				process.Start();

				// Safety net mirroring FLExBridgeHelper.cs's process watchdog: if the worker dies
				// (crash, killed, exits on its own parent-watchdog per Program.cs) this notices so
				// the next EnsureStarted()/HCWorkerClient retry respawns it rather than talking to
				// a dead pipe.
				var watchdog = new Thread(() =>
				{
					try
					{
						process.WaitForExit();
					}
					catch (Exception)
					{
						// Process handle may already be invalid; either way treat it as exited.
					}
				})
				{ IsBackground = true, Name = "HCWorker process watchdog" };
				watchdog.Start();

				m_process = process;
				return m_pipeName;
			}
		}

		/// <summary>
		/// Kills the worker (FieldWorks exit, or an idle timeout releasing its Server-GC memory
		/// footprint - design §4 "Shutdown"). Safe to call when nothing is running.
		/// </summary>
		public void Shutdown()
		{
			lock (m_lock)
			{
				if (m_process == null)
					return;
				try
				{
					if (!m_process.HasExited)
						m_process.Kill();
				}
				catch (Exception)
				{
					// Already exited or exiting; nothing more to do.
				}
				finally
				{
					m_process.Dispose();
					m_process = null;
					m_pipeName = null;
				}
			}
		}

		public void Dispose()
		{
			Shutdown();
		}
	}
}
