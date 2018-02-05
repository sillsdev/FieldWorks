// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// A control implements this if it wants to snap the split position to particular points.
	/// </summary>
	internal interface ISnapSplitPosition
	{
		/// <summary>
		/// An implementor answers true if it wants to take control of the split position.
		/// It may alter the position.
		/// If it answers true, the position will not be modified further by the MultiPane.
		/// Width is the width this pane will be after the splitter is positioned.
		/// </summary>
		bool SnapSplitPosition(ref int width);
	}
}