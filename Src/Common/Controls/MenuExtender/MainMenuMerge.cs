// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MainMenuMerge.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Merge two main menus. The difference to the .NET merge is that we create a copy of
	/// the menu items, so that we have a complete menu. Without it, we ended up having a
	/// different owner than what we expect (i.e. the unmerged menu instead of the merged one).
	/// </summary>
	/// <remarks>This class gets called from the menu extender</remarks>
	/// ----------------------------------------------------------------------------------------
	internal class MainMenuMerge
	{
		MenuExtender m_menuExtender;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="menuExtender">The menu extender</param>
		/// ------------------------------------------------------------------------------------
		internal MainMenuMerge(MenuExtender menuExtender)
		{
			m_menuExtender = menuExtender;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a copy of the menu item and all its sub-menu items by cloning it and
		/// initializing the menu extender properties.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="fRemoveSrc"><c>true</c> to remove old menu item from menu extender
		/// (as when merging, as opposed to cloning, menus)</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal MenuItem CopyMenu(MenuItem src, bool fRemoveSrc)
		{
			MenuItem menuItem = src.CloneMenu();
			// we want to deal with the sub-menu items separately, so get rid of those that
			// CloneMenu created. Unfortunately there's no way to clone only the menu item but
			// not the sub-menu items.
			menuItem.MenuItems.Clear();
			m_menuExtender.InitializeMenuItem(src, menuItem, fRemoveSrc);
			MergeMenu(menuItem, src);
			return menuItem;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// <p>Merge menu2 into menu1.</p>
		/// <p>Side effect: This method changes the main menu of the form if one of the two
		/// passed in menus is set as main menu of the form!</p>
		/// </summary>
		/// <remarks>This method is called recursively.</remarks>
		/// <param name="menu1">Menu being merged into.</param>
		/// <param name="menu2">Menu to be merged into menu1.</param>
		/// ------------------------------------------------------------------------------------
		internal void MergeMenu(Menu menu1, Menu menu2)
		{
			if (menu1 == menu2)
				return;

			if (menu2.MenuItems.Count <= 0)
				return;

			for (int n2 = 0; n2 < menu2.MenuItems.Count; n2++)
			{
				MenuItem item2 = menu2.MenuItems[n2];
				MenuItem item1;
				switch (item2.MergeType)
				{
					case MenuMerge.Add:
						menu1.MenuItems.Add(FindMergePosition(menu1, item2.MergeOrder),
							CopyMenu(item2, true));
						break;
					case MenuMerge.Replace:
					case MenuMerge.MergeItems:
						int n1;
						for (n1 = FindMergePosition(menu1, item2.MergeOrder - 1);
							n1 < menu1.MenuItems.Count; n1++)
						{
							item1 = menu1.MenuItems[n1];
							if (item1.MergeOrder != item2.MergeOrder)
							{
								menu1.MenuItems.Add(n1, CopyMenu(item2, true));
								break;
							}
							if (item1.MergeType != MenuMerge.Add)
							{
								if (item1.MergeType != MenuMerge.MergeItems
									|| item2.MergeType != MenuMerge.MergeItems)
								{
									item1.Dispose();
									menu1.MenuItems.Add(n1, CopyMenu(item2, true));
									break;
								}
								MergeMenu(item1, item2);
								break;
							}
						}
						if (n1 >= menu1.MenuItems.Count)
							menu1.MenuItems.Add(n1, CopyMenu(item2, true));
						break;
					case MenuMerge.Remove:
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the index of the menu item after which a new menu item with the same merge
		/// order should be inserted.
		/// </summary>
		/// <param name="menu">Menu</param>
		/// <param name="mergeOrder">Merge order of the new menu item</param>
		/// <returns>New position</returns>
		/// ------------------------------------------------------------------------------------
		private int FindMergePosition(Menu menu, int mergeOrder)
		{
			int iSearchMin = 0;
			int iSearchLim = menu.MenuItems.Count;
			int iMiddle;
			while (iSearchMin < iSearchLim)
			{
				iMiddle = (iSearchMin + iSearchLim) / 2;
				if (menu.MenuItems[iMiddle].MergeOrder <= mergeOrder)
					iSearchMin = iMiddle + 1;
				else
					iSearchLim = iMiddle;
			}
			return iSearchMin;
		}
	}
}
