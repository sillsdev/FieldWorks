using System;
using System.Diagnostics;
using System.IO;

namespace SIL.FieldWorks.Common.Avalonia.PreviewHost;

internal static class PreviewHostLogging
{
	private const string ListenerName = "FwPreviewHostFile";
	private static bool s_initialized;

	public static string LogFilePath { get; private set; } = "";

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
			// Last-resort: avoid crashing the preview host due to logging initialization.
			try
			{
				Trace.WriteLine($"[FwAvaloniaPreviewHost] Failed to initialize logging: {ex}");
			}
			catch
			{
				// ignored
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
