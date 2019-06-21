// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// Add List area specific behavior, to enable handling custom lists
	/// </summary>
	internal interface IListArea : IArea
	{
		/// <summary>
		/// Let client know the tools in the area need to be updated.
		/// </summary>
		event EventHandler ListAreaToolsChanged;

		/// <summary>
		/// Add a new custom list to the area, and to the Tab.
		/// </summary>
		void OnAddCustomList(ICmPossibilityList customList);

		/// <summary>
		/// Remove a custom list's tool from the area and from the Tab
		/// </summary>
		void OnRemoveCustomListTool(ITool gonerTool);

		/// <summary>
		/// Change the display name of the list in the Tab.
		/// </summary>
		void OnUpdateListDisplayName(ITool gonerTool, ICmPossibilityList customList);
	}
}