// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This is used in m_layoutStates of DataTree to keep track of various special situations that
	/// affect what is done during OnLayout and HandleLayout1.
	/// </summary>
	internal enum LayoutStates : byte
	{
		klsNormal, // OnLayout executes normally, nothing special is happening
		klsChecking, // OnPaint is checking that all slices that intersect the client area are ready to function.
		klsLayoutSuspended, // Had to suspend layout during paint, need to resume at end and repaint.
		klsClearingAll, // In the process of clearing all slices, ignore any intermediate layout messages.
		klsDoingLayout, // We are executing HandleLayout1 (other than from OnPaint), or laying out a single slice in FieldAt().
	}
}