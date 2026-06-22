// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;

namespace SIL.FieldWorks.Common.FwAvalonia.PreviewHost
{
	internal static class PreviewHostLogging
	{
		private const string ListenerName = "FwPreviewHostFile";
		private static bool s_initialized;

		public static string LogFilePath { get; private set; } = string.Empty;

		public static void Initialize()
		{
			if (s_initialized)
				return;
			s_initialized = true;

			try
			{
				var baseDir = AppContext.BaseDirectory;
				var configuredPath = Environment.GetEnvironmentVariable("FW_PREVIEW_TRACE_LOG");
				var logPath = string.IsNullOrWhiteSpace(configuredPath)
					? Path.Combine(baseDir, "FieldWorks.trace.log")
					: configuredPath;

				var logDir = Path.GetDirectoryName(logPath);
				if (!string.IsNullOrWhiteSpace(logDir))
					Directory.CreateDirectory(logDir);

				LogFilePath = logPath;

				if (!HasListener(ListenerName))
				{
					var fileStream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
					var writer = new StreamWriter(fileStream) { AutoFlush = true };
					Trace.Listeners.Add(new TextWriterTraceListener(writer, ListenerName));
				}

				Trace.AutoFlush = true;
				Trace.WriteLine($"[FwAvaloniaPreviewHost] Logging initialized. LogFile='{logPath}' BaseDir='{baseDir}'");
			}
			catch (Exception ex)
			{
				try
				{
					Trace.WriteLine($"[FwAvaloniaPreviewHost] Failed to initialize logging: {ex}");
				}
				catch
				{
				}
			}
		}

		private static bool HasListener(string name)
		{
			foreach (TraceListener listener in Trace.Listeners)
			{
				if (string.Equals(listener.Name, name, StringComparison.Ordinal))
					return true;
			}

			return false;
		}
	}
}
