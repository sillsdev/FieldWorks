// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// The check state
	/// </summary>
	[Flags]
	public enum TriStateTreeViewCheckState
	{
		/// <summary>Unchecked</summary>
		Unchecked = 1,
		/// <summary>Checked</summary>
		Checked = 2,
		/// <summary>grayed out</summary>
		GrayChecked = Unchecked | Checked,
	}
}