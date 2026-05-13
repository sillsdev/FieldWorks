namespace SIL.FieldWorks.Common.Avalonia.Diagnostics;

public static class FwLog
{
	public static IFwLogger ForComponent(string componentName) => new FwTraceLogger(componentName);
}
