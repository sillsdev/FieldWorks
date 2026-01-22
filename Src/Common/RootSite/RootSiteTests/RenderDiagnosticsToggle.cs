// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// Manages the diagnostics and trace output toggle for render benchmarks.
	/// Provides methods to enable/disable trace output and configure trace listeners.
	/// </summary>
	public class RenderDiagnosticsToggle : IDisposable
	{
		private readonly string m_flagsFilePath;
		private readonly string m_traceLogPath;
		private TextWriterTraceListener m_traceListener;
		private StreamWriter m_traceWriter;
		private bool m_disposed;
		private bool m_originalDiagnosticsState;

		/// <summary>
		/// Gets whether diagnostics are currently enabled.
		/// </summary>
		public bool DiagnosticsEnabled { get; private set; }

		/// <summary>
		/// Gets whether trace output is currently enabled.
		/// </summary>
		public bool TraceEnabled { get; private set; }

		/// <summary>
		/// Gets the path to the trace log file.
		/// </summary>
		public string TraceLogPath => m_traceLogPath;

		/// <summary>
		/// Gets the default output directory for benchmark artifacts.
		/// </summary>
		public static string DefaultOutputDirectory => Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Output", "RenderBenchmarks");

		/// <summary>
		/// Initializes a new instance of the <see cref="RenderDiagnosticsToggle"/> class.
		/// </summary>
		/// <param name="flagsFilePath">Path to the flags JSON file.</param>
		/// <param name="traceLogPath">Path for trace log output.</param>
		public RenderDiagnosticsToggle(string flagsFilePath = null, string traceLogPath = null)
		{
			m_flagsFilePath = flagsFilePath ?? RenderScenarioDataBuilder.DefaultFlagsPath;
			m_traceLogPath = traceLogPath ?? Path.Combine(DefaultOutputDirectory, "render-trace.log");

			LoadFlags();
		}

		/// <summary>
		/// Enables diagnostics and trace output.
		/// </summary>
		/// <param name="persist">Whether to persist the change to the flags file.</param>
		public void EnableDiagnostics(bool persist = false)
		{
			m_originalDiagnosticsState = DiagnosticsEnabled;
			DiagnosticsEnabled = true;
			TraceEnabled = true;

			if (persist)
			{
				SaveFlags();
			}

			SetupTraceListener();
		}

		/// <summary>
		/// Disables diagnostics and trace output.
		/// </summary>
		/// <param name="persist">Whether to persist the change to the flags file.</param>
		public void DisableDiagnostics(bool persist = false)
		{
			DiagnosticsEnabled = false;
			TraceEnabled = false;

			if (persist)
			{
				SaveFlags();
			}

			RemoveTraceListener();
		}

		/// <summary>
		/// Restores the original diagnostics state before Enable/Disable was called.
		/// </summary>
		public void RestoreOriginalState()
		{
			if (m_originalDiagnosticsState)
			{
				EnableDiagnostics(persist: false);
			}
			else
			{
				DisableDiagnostics(persist: false);
			}
		}

		/// <summary>
		/// Writes a render trace entry to the trace log.
		/// </summary>
		/// <param name="stage">The rendering stage name.</param>
		/// <param name="durationMs">The stage duration in milliseconds.</param>
		/// <param name="context">Optional context information.</param>
		public void WriteTraceEntry(string stage, double durationMs, string context = null)
		{
			if (!TraceEnabled)
				return;

			var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff");
			var contextPart = string.IsNullOrEmpty(context) ? "" : $" Context={context}";
			var entry = $"[{timestamp}] [RENDER] Stage={stage} Duration={durationMs:F3}ms{contextPart}";

			Trace.WriteLine(entry);
		}

		/// <summary>
		/// Writes an informational message to the trace log.
		/// </summary>
		/// <param name="message">The message to write.</param>
		public void WriteInfo(string message)
		{
			if (!DiagnosticsEnabled)
				return;

			var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff");
			Trace.WriteLine($"[{timestamp}] [INFO] {message}");
		}

		/// <summary>
		/// Writes a warning message to the trace log.
		/// </summary>
		/// <param name="message">The warning message.</param>
		public void WriteWarning(string message)
		{
			if (!DiagnosticsEnabled)
				return;

			var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff");
			Trace.TraceWarning($"[{timestamp}] [WARN] {message}");
		}

		/// <summary>
		/// Flushes any buffered trace output.
		/// </summary>
		public void Flush()
		{
			m_traceListener?.Flush();
			m_traceWriter?.Flush();
			Trace.Flush();
		}

		/// <summary>
		/// Gets the contents of the trace log file.
		/// </summary>
		/// <returns>The trace log content, or empty string if not available.</returns>
		public string GetTraceLogContent()
		{
			Flush();

			if (!File.Exists(m_traceLogPath))
				return string.Empty;

			try
			{
				// Need to read without locking since we may still have the file open
				using (var stream = new FileStream(m_traceLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
			catch
			{
				return string.Empty;
			}
		}

		/// <summary>
		/// Clears the trace log file.
		/// </summary>
		public void ClearTraceLog()
		{
			RemoveTraceListener();

			if (File.Exists(m_traceLogPath))
			{
				try
				{
					File.Delete(m_traceLogPath);
				}
				catch
				{
					// Ignore deletion errors
				}
			}

			if (TraceEnabled)
			{
				SetupTraceListener();
			}
		}

		private void LoadFlags()
		{
			var flags = BenchmarkFlags.LoadFromFile(m_flagsFilePath);
			DiagnosticsEnabled = flags.DiagnosticsEnabled;
			TraceEnabled = flags.TraceEnabled;
			m_originalDiagnosticsState = DiagnosticsEnabled;

			if (TraceEnabled)
			{
				SetupTraceListener();
			}
		}

		private void SaveFlags()
		{
			var flags = new BenchmarkFlags
			{
				DiagnosticsEnabled = DiagnosticsEnabled,
				TraceEnabled = TraceEnabled,
				CaptureMode = "DrawToBitmap"
			};

			var directory = Path.GetDirectoryName(m_flagsFilePath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var json = JsonConvert.SerializeObject(flags, Formatting.Indented);
			File.WriteAllText(m_flagsFilePath, json, Encoding.UTF8);
		}

		private void SetupTraceListener()
		{
			if (m_traceListener != null)
				return; // Already set up

			var directory = Path.GetDirectoryName(m_traceLogPath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			m_traceWriter = new StreamWriter(m_traceLogPath, append: true, encoding: Encoding.UTF8)
			{
				AutoFlush = true
			};

			m_traceListener = new TextWriterTraceListener(m_traceWriter, "RenderBenchmark");
			Trace.Listeners.Add(m_traceListener);
		}

		private void RemoveTraceListener()
		{
			if (m_traceListener != null)
			{
				Trace.Listeners.Remove(m_traceListener);
				m_traceListener.Dispose();
				m_traceListener = null;
			}

			if (m_traceWriter != null)
			{
				m_traceWriter.Dispose();
				m_traceWriter = null;
			}
		}

		/// <summary>
		/// Releases all resources used by the toggle.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases the unmanaged resources and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">True to release both managed and unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (m_disposed)
				return;

			if (disposing)
			{
				RemoveTraceListener();
			}

			m_disposed = true;
		}
	}
}
