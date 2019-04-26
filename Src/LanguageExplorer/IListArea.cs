// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.SilSidePane;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// Add List area specific behavior, to enable handling custom lists
	/// </summary>
	internal interface IListArea : IArea
	{
		/// <summary>
		/// Set the list area sidebar tab, so it can be updated as custom lists gets added/removed, or get names changed.
		/// </summary>
		Tab ListAreaTab { set; }

		/// <summary>
		/// Add a new custom list to the area, and to the Tab.
		/// </summary>
		/// <param name="newList"></param>
		void AddCustomList(ICmPossibilityList newList);

		/// <summary>
		/// Remove a custom list's tool from the area and from the Tab
		/// </summary>
		/// <param name="gonerTool"></param>
		void RemoveCustomListTool(ITool gonerTool);

		/// <summary>
		/// Change the display name of the list in the Tab.
		/// </summary>
		void ModifiedListDisplayName(ITool tool);
	}
}