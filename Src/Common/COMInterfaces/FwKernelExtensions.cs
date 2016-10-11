// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// <summary>
	/// Extensions methods for <see cref="ITsString"/>.
	/// </summary>
	public static class FwKernelExtensions
	{
		/// <summary>
		/// Gets the string property value.
		/// </summary>
		public static string get_StringProperty(this ITsString tss, int iRun, int tpt)
		{
			return tss.get_Properties(iRun).GetStrPropValue(tpt);
		}

		/// <summary>
		/// Gets the string property value at the specified character offset.
		/// </summary>
		public static string get_StringPropertyAt(this ITsString tss, int ich, int tpt)
		{
			return tss.get_StringProperty(tss.get_RunAt(ich), tpt);
		}

		/// <summary>
		/// Gets the writing system.
		/// </summary>
		public static int get_WritingSystem(this ITsString tss, int irun)
		{
			int var;
			return tss.get_Properties(irun).GetIntPropValues((int) FwTextPropType.ktptWs, out var);
		}

		/// <summary>
		/// Gets the writing system at the specified character offset.
		/// </summary>
		public static int get_WritingSystemAt(this ITsString tss, int ich)
		{
			return tss.get_WritingSystem(tss.get_RunAt(ich));
		}

		/// <summary>
		/// Determines if the specified run is an ORC character.
		/// </summary>
		public static bool get_IsRunOrc(this ITsString tss, int iRun)
		{
			return tss.get_RunText(iRun) == "\ufffc";
		}
	}
}
