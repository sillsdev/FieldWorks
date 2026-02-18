using System;
using SIL.FieldWorks.Common.Avalonia.Diagnostics;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia;

internal static class AdvancedEntryTrace
{
	private const string ComponentName = "AdvancedEntry.Avalonia";
	private static readonly IFwLogger Logger = FwLog.ForComponent(ComponentName);

	public static void Info(string message) => Logger.Info(message);

	public static void Warn(string message) => Logger.Warn(message);

	public static void Error(string message, Exception? exception = null)
	{
		if (exception is null)
			Logger.Error(message);
		else
			Logger.Error(message, exception);
	}
}