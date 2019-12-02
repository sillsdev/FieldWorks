// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

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
				var retval = new Dictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>();
				foreach (MainMenu mainMenu in Enum.GetValues(typeof(MainMenu)))
				{
					retval.Add(mainMenu, new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>());
				}
				return retval;
			}
		}

		/// <summary>
		/// Add all known main tool bars.
		/// </summary>
		internal static Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> PopulateForToolBars
		{
			get
			{
				var retval = new Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>();
				foreach (ToolBar toolBar in Enum.GetValues(typeof(ToolBar)))
				{
					retval.Add(toolBar, new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>());
				}
				return retval;
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