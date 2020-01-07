// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Just a shell class for containing runtime Switches for controlling the diagnostic output.
	/// </summary>
	internal static class RuntimeSwitches
	{
		/// Tracing variable - used to control when and what is output to the debug and trace listeners
		public static TraceSwitch RecordTimingSwitch = new TraceSwitch("FilterRecordTiming", "Used for diagnostic timing output", "Off");
	}
}