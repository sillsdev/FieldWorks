using System;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.Avalonia.Diagnostics;

public sealed class FwTraceLogger : IFwLogger
{
	private readonly string m_component;
	private readonly TraceSwitch m_switch;

	public FwTraceLogger(string componentName)
		: this(componentName, componentName)
	{
	}

	public FwTraceLogger(string componentName, string switchName)
	{
		m_component = componentName;
		m_switch = new TraceSwitch(switchName, $"Diagnostics for {componentName}", "Off");
	}

	public void Info(string message)
	{
		Trace.WriteLineIf(m_switch.TraceInfo, Format(message), m_switch.DisplayName);
	}

	public void Warn(string message)
	{
		Trace.WriteLineIf(m_switch.TraceWarning, Format($"WARN: {message}"), m_switch.DisplayName);
	}

	public void Error(string message)
	{
		Trace.WriteLineIf(m_switch.TraceError, Format($"ERROR: {message}"), m_switch.DisplayName);
	}

	public void Error(string message, Exception exception)
	{
		Trace.WriteLineIf(m_switch.TraceError, Format($"ERROR: {message}{Environment.NewLine}{exception}"), m_switch.DisplayName);
	}

	private string Format(string message) => $"[{m_component}] {message}";
}
