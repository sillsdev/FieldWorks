// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace LanguageExplorer
{
	/// <summary>
	/// Class that populates the known main menus and tool bars.
	/// </summary>
	internal static class UiWidgetServices
	{
		/// <summary>
		/// Add all known main menus.
		/// </summary>
		internal static Dictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> PopulateForMenus
		{
			get
			{
				return Enum.GetValues(typeof(MainMenu)).Cast<MainMenu>().ToDictionary(mainMenu => mainMenu, mainMenu => new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>());
			}
		}

		/// <summary>
		/// Add all known main tool bars.
		/// </summary>
		internal static Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> PopulateForToolBars
		{
			get
			{
				return Enum.GetValues(typeof(ToolBar)).Cast<ToolBar>().ToDictionary(toolBar => toolBar, toolBar => new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>());
			}
		}

		internal static string CreateShowHiddenFieldsPropertyName(string toolMachineName)
		{
			return $"{LanguageExplorerConstants.ShowHiddenFields}_{toolMachineName}";
		}

		internal static string CreateShowFailingItemsPropertyName(string toolMachineName)
		{
			return $"{LanguageExplorerConstants.ShowFailingItems}_{toolMachineName}";
		}

		internal static Tuple<bool, bool> CanSeeAndDo => new Tuple<bool, bool>(true, true);

		internal static void InsertPair(IDictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> toolBarDictionary, IDictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> menuDictionary, Command key, Tuple<EventHandler, Func<Tuple<bool, bool>>> currentTuple)
		{
			toolBarDictionary.Add(key, currentTuple);
			menuDictionary.Add(key, currentTuple);
		}
	}
}