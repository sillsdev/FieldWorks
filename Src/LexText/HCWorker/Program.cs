// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using SIL.FieldWorks.WordWorks.Parser;

namespace SIL.FieldWorks.WordWorks.Parser.HCWorker
{
	/// <summary>
	/// Entry point for the out-of-process Server-GC HermitCrab worker. Spawned lazily by
	/// FieldWorks' HCWorkerProcessManager (Src\LexText\ParserCore) as:
	///   HCWorker.exe &lt;pipeName&gt; &lt;parentProcessId&gt;
	/// The WCF contract (IHCWorkerService + DTOs), the pipe binding, and the Form/Msa/InflType key
	/// constants all live in ParserCore, so this host and the FieldWorks-side client share one
	/// definition. See RUSTIFY-fieldworks-worker-design.md.
	/// </summary>
	public static class Program
	{
		public static int Main(string[] args)
		{
			if (args.Length < 2 || !int.TryParse(args[1], out int parentProcessId))
			{
				Console.Error.WriteLine("Usage: HCWorker.exe <pipeName> <parentProcessId>");
				return 1;
			}
			string pipeName = args[0];

			// Safety net mirroring FLExBridgeHelper.cs's process watchdog: if FieldWorks dies
			// (crash, kill, normal exit without an explicit shutdown of us) this ensures the
			// worker - and its Server-GC memory footprint - does not outlive it.
			StartParentWatchdog(parentProcessId);

			NetNamedPipeBinding pipeBinding = PipeBindingFactory.Create();

			using (var host = new ServiceHost(new HCWorkerService()))
			{
				host.AddServiceEndpoint(typeof(IHCWorkerService), pipeBinding, "net.pipe://localhost/" + pipeName);
				host.Open();

				// Readiness = the pipe is open; FieldWorks calls UpdateGrammar immediately after
				// spawn and every parse call fails with a clear error until that lands
				// (HCWorkerService.RequireMorpher), so there is no separate ready handshake.
				Console.Out.WriteLine("READY");
				Console.Out.Flush();

				// Block forever; the process exits via the parent watchdog above or being killed
				// directly by FieldWorks (design §4 "Shutdown").
				Thread.Sleep(Timeout.Infinite);
			}
			return 0;
		}

		private static void StartParentWatchdog(int parentProcessId)
		{
			Process parent;
			try
			{
				parent = Process.GetProcessById(parentProcessId);
			}
			catch (ArgumentException)
			{
				// Parent already gone before we even started - exit immediately rather than
				// leaking a Server-GC process with nothing to serve.
				Environment.Exit(0);
				return;
			}

			var watchdog = new Thread(() =>
			{
				try
				{
					parent.WaitForExit();
				}
				catch (Exception)
				{
					// Handle may already be invalid; either way, treat it as "parent is gone."
				}
				Environment.Exit(0);
			})
			{
				IsBackground = true,
				Name = "HCWorker parent-process watchdog"
			};
			watchdog.Start();
		}
	}
}
