// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>Which UI framework renders a tool. Legacy is the safe default.</summary>
	public enum UIFramework
	{
		/// <summary>The legacy WinForms framework (the DataTree/Slice detail path).</summary>
		Legacy,

		/// <summary>The Avalonia framework.</summary>
		Avalonia
	}
}
