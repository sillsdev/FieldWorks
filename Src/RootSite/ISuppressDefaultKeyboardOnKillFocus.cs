// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Marker interface which a window (e.g., FocusBoxController) may implement to indicate
	/// that when focus switches there from a root box, we should NOT restore the default keyboard.
	/// </summary>
	public interface ISuppressDefaultKeyboardOnKillFocus
	{
	}
}