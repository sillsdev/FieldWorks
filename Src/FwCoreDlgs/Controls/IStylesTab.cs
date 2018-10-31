// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// Interface for the styles dialog to communicate to its tabs
	/// </summary>
	public interface IStylesTab
	{
		/// <summary>
		/// Saves the information on the tab to the specified style info.
		/// </summary>
		void SaveToInfo(StyleInfo styleInfo);

		/// <summary>
		/// Updates the information on the tab with the information in the specified style info.
		/// </summary>
		void UpdateForStyle(StyleInfo styleInfo);
	}
}