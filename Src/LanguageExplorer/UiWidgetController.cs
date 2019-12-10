// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.Code;

namespace LanguageExplorer
{
	/// <summary>
	/// This class aims to be a focal point for the areas, tools, and controls to declare which main menu items and toolbar buttons
	/// are to be activated/deactivated, as the areas, tools, controls change.
	/// </summary>
	/// <remarks>
	/// Registration must occur in this order: 1) area, 2) tool, 3) user controls, which is optional.
	/// Global will happen early on in the main window creation.
	/// </remarks>
	internal sealed class UiWidgetController
	{
		private readonly MainMenusController _mainMenusController;
		private readonly ToolBarsController _toolBarsController;
		private bool _hasCalledAddGlobalHandlers;

		#region Currently registered

		private IArea _area;
		private ITool _tool;
		private readonly List<UserControl> _userControls = new List<UserControl>();

		#endregion Currently registered

		internal UiWidgetController(MainMenusParameterObject mainMenusParameterObject,  MainToolBarsParameterObject mainToolBarsParameterObject)
		{
			Guard.AgainstNull(mainMenusParameterObject, nameof(mainMenusParameterObject));
			Guard.AgainstNull(mainToolBarsParameterObject, nameof(mainToolBarsParameterObject));

			_mainMenusController = new MainMenusController(mainMenusParameterObject);
			_toolBarsController = new ToolBarsController(mainToolBarsParameterObject);
		}

		#region Menus

		internal IReadOnlyDictionary<Command, ToolStripItem> FileMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.File);

		internal IReadOnlyDictionary<Command, ToolStripItem> SendReceiveMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.SendReceive);

		internal IReadOnlyDictionary<Command, ToolStripItem> EditMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.Edit);

		internal IReadOnlyDictionary<Command, ToolStripItem> ViewMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.View);

		internal IReadOnlyDictionary<Command, ToolStripItem> DataMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.Data);

		internal IReadOnlyDictionary<Command, ToolStripItem> InsertMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.Insert);

		internal IReadOnlyDictionary<Command, ToolStripItem> FormatMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.Format);

		internal IReadOnlyDictionary<Command, ToolStripItem> ToolsMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.Tools);

		internal IReadOnlyDictionary<Command, ToolStripItem> ParserMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.Parser);

		internal IReadOnlyDictionary<Command, ToolStripItem> WindowMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.Window);

		internal IReadOnlyDictionary<Command, ToolStripItem> HelpMenuDictionary => _mainMenusController.GetMenuCommands(MainMenu.Help);

		#endregion Menus

		#region ToolBars

		internal IReadOnlyDictionary<Command, ToolStripItem> StandardToolBarDictionary => _toolBarsController.GetToolBarCommands(ToolBar.Standard);

		internal IReadOnlyDictionary<Command, ToolStripItem> ViewToolBarDictionary => _toolBarsController.GetToolBarCommands(ToolBar.View);

		internal IReadOnlyDictionary<Command, ToolStripItem> InsertToolBarDictionary => _toolBarsController.GetToolBarCommands(ToolBar.Insert);

		internal IReadOnlyDictionary<Command, ToolStripItem> FormatToolBarDictionary => _toolBarsController.GetToolBarCommands(ToolBar.Format);

		#endregion ToolBars

		#region Registration

		/// <summary>
		/// Add handlers for the more global type menus/tool bar buttons.
		/// </summary>
		/// <remarks>
		/// This can be called only once.
		/// </remarks>
		internal void AddGlobalHandlers(GlobalUiWidgetParameterObject globalParameterObject)
		{
			if (_hasCalledAddGlobalHandlers)
			{
				throw new InvalidOperationException("Cannot call 'AddGlobalHandlers' more than once.");
			}
			_mainMenusController.AddGlobalHandlers(globalParameterObject.GlobalMenuItems);
			_toolBarsController.AddGlobalHandlers(globalParameterObject.GlobalToolBarItems);
			_hasCalledAddGlobalHandlers = true;
		}

		/// <summary>
		/// Register an area's menu/tool bars.
		/// </summary>
		/// <remarks>
		/// This can be called only once, per area.
		/// It must be called, even if there are no menus or tool bar buttons the area is interested in handling.
		/// </remarks>
		/// <exception cref="InvalidOperationException">Thrown if:
		/// 1) An area is being registered, without the earlier one being unregistered.
		/// 2) A former tool has not been unregistered.
		/// 3) Former user controls have not been unregistered.</exception>
		internal void AddHandlers(AreaUiWidgetParameterObject areaParameterObject)
		{
			if (_area != null)
			{
				if (areaParameterObject.Area == _area)
				{
					throw new InvalidOperationException("Cannot register an area more than once.");
				}
				throw new InvalidOperationException("Cannot register an area, before the old one has been unregistered.");
			}
			if (_tool != null)
			{
				throw new InvalidOperationException("Cannot register an area, before a tool has been unregistered.");
			}
			CheckForExistingUserControls("an area");
			// Wire up given menus to provided events for area.
			// NB: visibility/enabling is handled via: 1) application idle event for tool bar buttons, and 2) drop down opening event handler on the respective main menu.
			// Store provided handlers, etc for area.
			_area = areaParameterObject.Area;
			_mainMenusController.AddAreaHandlers(areaParameterObject.MenuItemsForArea);
			_toolBarsController.AddAreaHandlers(areaParameterObject.ToolBarItemsForArea);
		}

		/// <summary>
		/// Register an tool's menu/tool bars.
		/// </summary>
		/// <remarks>
		/// This cannot be called more than once, per tool.
		/// </remarks>
		/// <exception cref="InvalidOperationException">Thrown if: 1) the same tool is registered more than once,
		/// 2) multiple tools are registered, without the earlier one being unregistered,
		/// or 3) if the tool is not in the previously registered area.</exception>
		internal void AddHandlers(ToolUiWidgetParameterObject toolParameterObject)
		{
			CheckForNullArea("tool");
			if (toolParameterObject.Tool.Area != _area)
			{
				throw new InvalidOperationException("Cannot register a tool, when the currently registered area is not for the tool being registered.");
			}
			if (_tool != null)
			{
				if (toolParameterObject.Tool == _tool)
				{
					throw new InvalidOperationException("Cannot register a tool more than once.");
				}
				throw new InvalidOperationException("Cannot register a tool, before the old one has been unregistered.");
			}
			CheckForExistingUserControls("a tool");
			// Store provided handlers, etc for tool.
			_tool = toolParameterObject.Tool;
			_mainMenusController.AddToolHandlers(toolParameterObject.MenuItemsForTool);
			_toolBarsController.AddToolHandlers(toolParameterObject.ToolBarItemsForTool);
		}

		/// <summary>
		/// Register an UserControl's menu/tool bars.
		/// </summary>
		/// <remarks>
		/// This can be called more than once, but only once per UserControl in a given tool.
		/// </remarks>
		/// <exception cref="InvalidOperationException">Thrown if the same UserControl is registered more than once, without the earlier one being unregistered.</exception>
		internal void AddHandlers(UserControlUiWidgetParameterObject userControlParameterObject)
		{
			CheckForNullArea("user control");
			if (_area == null)
			{
				throw new InvalidOperationException("Cannot register a user control, when the respective area has not been registered, even if the area had nothing to register.");
			}
			if (_tool == null)
			{
				throw new InvalidOperationException("Cannot register a user control, when the respective tool has not been registered, even if the tool had nothing to register.");
			}
			if (_userControls.FirstOrDefault(currentControl => currentControl == userControlParameterObject.UserControl) != null)
			{
				throw new InvalidOperationException("Cannot register a user control more than once.");
			}
			// Store provided handlers, etc for user controls.
			_userControls.Add(userControlParameterObject.UserControl);
			// Wire up given menus and/or tool bar buttons to provided events.
			// NB: visibility/enabling is handled via: 1) application idle event for tool bar buttons, and 2) drop down opening event handler on the respective main menu.
			_mainMenusController.AddUserControlHandlers(userControlParameterObject.UserControl, userControlParameterObject.MenuItemsForUserControl);
			_toolBarsController.AddUserControlHandlers(userControlParameterObject.UserControl, userControlParameterObject.ToolBarItemsForUserControl);
		}

		/// <summary>
		/// Called when switching from one area to another.
		/// </summary>
		///  <remarks>
		/// There is no need to call the other two un-register methods, if this one is called, as this call will un-register the tool and its controls.
		/// </remarks>
		internal void RemoveAreaHandlers()
		{
			if (_area == null)
			{
				// Already removed.
				return;
			}
			// Unwire stuff from area (in that order). Include handling the application idle handler and the drop down handler here.
			RemoveToolHandlers();
			// Now, un-register area.
			_mainMenusController.RemoveAreaHandlers();
			_toolBarsController.RemoveAreaHandlers();
			_area = null;
		}

		/// <summary>
		/// Called when switching from one tool to another In the same area.
		/// </summary>
		///  <remarks>
		/// There is no need to call the other two un-register methods, if this one is called. This call will un-register the current tool's controls.
		/// </remarks>
		internal void RemoveToolHandlers()
		{
			if (_tool == null)
			{
				// Already removed.
				return;
			}
			foreach (var userControl in _userControls.ToList())
			{
				RemoveUserControlHandlers(userControl);
			}
			//// Now, un-register tool.
			_mainMenusController.RemoveToolHandlers();
			_toolBarsController.RemoveToolHandlers();
			_tool = null;
		}

		/// <summary>
		/// Called when needed by a UserControl when it no longer can deal with menus or toolbar buttons (e.g., two FocusBoxController instances going in/out of scope).
		/// </summary>
		internal void RemoveUserControlHandlers(UserControl userControl)
		{
			if (!_userControls.Contains(userControl))
			{
				// Already removed.
				return;
			}
			_userControls.Remove(userControl);
			_mainMenusController.RemoveUserControlHandlers(userControl);
			_toolBarsController.RemoveUserControlHandlers(userControl);
		}

		private void CheckForExistingUserControls(string message)
		{
			if (_userControls.Any())
			{
				throw new InvalidOperationException($"Cannot register {message}, when former user controls have not been unregistered.");
			}
		}

		private void CheckForNullArea(string message)
		{
			if (_area == null)
			{
				throw new InvalidOperationException($"Cannot register a {message}, when the respective area has not been registered, even if the area had nothing to register.");
			}
		}

		#endregion Registration

		#region Private controller classes that do the real work

		private sealed class MainMenusController
		{
			private readonly IReadOnlyDictionary<MainMenu, Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>> _supportedMenuItems;
			private readonly IReadOnlyDictionary<MainMenu, MainMenuController> _mainMenuControllers;

			internal MainMenusController(MainMenusParameterObject mainMenusParameterObject)
			{
				_supportedMenuItems = mainMenusParameterObject.SupportedMenuItems;
				_mainMenuControllers = mainMenusParameterObject.MainMenus.ToDictionary(mainMenuKvp => mainMenuKvp.Key, mainMenuKvp => new MainMenuController(mainMenuKvp.Value, _supportedMenuItems[mainMenuKvp.Key].Item1, _supportedMenuItems[mainMenuKvp.Key].Item2));
			}

			internal IReadOnlyDictionary<Command, ToolStripItem> GetMenuCommands(MainMenu mainMenu)
			{
				return _supportedMenuItems[mainMenu].Item1;
			}

			internal void AddGlobalHandlers(IReadOnlyDictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> globalMenuItems)
			{
				foreach (var mainMenuKvp in globalMenuItems)
				{
					_mainMenuControllers[mainMenuKvp.Key].AddGlobalHandlers(mainMenuKvp.Value);
				}
			}

			internal void AddAreaHandlers(IReadOnlyDictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> menuItemsForArea)
			{
				foreach (var mainMenuKvp in menuItemsForArea)
				{
					_mainMenuControllers[mainMenuKvp.Key].AddAreaHandlers(mainMenuKvp.Value);
				}
			}

			internal void AddToolHandlers(IReadOnlyDictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> menuItemsForTool)
			{
				foreach (var mainMenuKvp in menuItemsForTool)
				{
					_mainMenuControllers[mainMenuKvp.Key].AddToolHandlers(mainMenuKvp.Value);
				}
			}

			internal void AddUserControlHandlers(UserControl userControl, Dictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> menuItemsForUserControl)
			{
				foreach (var mainMenuKvp in menuItemsForUserControl)
				{
					_mainMenuControllers[mainMenuKvp.Key].AddUserControlHandlers(userControl, mainMenuKvp.Value);
				}
			}

			internal void RemoveAreaHandlers()
			{
				foreach (var mainMenuController in _mainMenuControllers.Values)
				{
					mainMenuController.RemoveAreaHandlers();
				}
			}

			internal void RemoveToolHandlers()
			{
				foreach (var mainMenuController in _mainMenuControllers.Values)
				{
					mainMenuController.RemoveToolHandlers();
				}
			}

			internal void RemoveUserControlHandlers(UserControl userControl)
			{
				foreach (var mainMenuController in _mainMenuControllers.Values)
				{
					mainMenuController.RemoveUserControlHandlers(userControl);
				}
			}

			private sealed class MainMenuController
			{
				private readonly ToolStripMenuItem _mainMenu;
				private readonly IReadOnlyDictionary<Command, ToolStripItem> _supportedMenuItems;
				private readonly List<ISeparatorMenuBundle> _separatorMenuBundles;
				private Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> _globalCommands = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();
				private Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> _commandsForArea;
				private Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> _commandsForTool;
				private readonly List<Tuple<UserControl, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>> _commandsForUserControls = new List<Tuple<UserControl, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>>();
				private readonly Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> _combinedCommands = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();

				internal MainMenuController(ToolStripMenuItem mainMenu, IReadOnlyDictionary<Command, ToolStripItem> supportedMenuItems, List<ISeparatorMenuBundle> separatorMenuBundles)
				{
					_mainMenu = mainMenu;
					_mainMenu.DropDownOpening += MainMenu_DropDownOpening;
					_mainMenu.DropDownOpened += MainMenu_DropDownOpened;
					_supportedMenuItems = supportedMenuItems;
					_separatorMenuBundles = separatorMenuBundles;
					foreach (var item in _supportedMenuItems.Values)
					{
						var itemAsToolStripDropDownItem = item as ToolStripDropDownItem;
						if (itemAsToolStripDropDownItem != null && itemAsToolStripDropDownItem.HasDropDownItems)
						{
							((ToolStripDropDownItem)item).DropDownOpened += MainMenu_DropDownOpened;
						}
					}
				}

				internal void AddGlobalHandlers(Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands)
				{
					AddCombinedCommands(commands, true);
				}

				internal void AddAreaHandlers(Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands)
				{
					_commandsForArea = commands;
					AddCombinedCommands(_commandsForArea);
				}

				internal void AddToolHandlers(Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands)
				{
					_commandsForTool = commands;
					AddCombinedCommands(_commandsForTool);
				}

				internal void AddUserControlHandlers(UserControl userControl, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands)
				{
					_commandsForUserControls.Add(new Tuple<UserControl, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>(userControl, commands));
					AddCombinedCommands(commands);
				}

				private void AddCombinedCommands(Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands, bool addToGlobal = false)
				{
					foreach (var commandKvp in commands)
					{
						var commandKey = commandKvp.Key;
						var commandValue = commandKvp.Value;
						if (addToGlobal)
						{
							_globalCommands.Add(commandKey, commandValue);
						}
						_combinedCommands.Add(commandKey, commandValue);
						var currentMenuItem = _supportedMenuItems[commandKey];
						if (commandValue.Item1 != null)
						{
							// Add supplied click event handler to menu item.
							currentMenuItem.Click += commandValue.Item1;
						}
					}
				}

				internal void RemoveAreaHandlers()
				{
					if (_commandsForArea == null)
					{
						return;
					}
					RemoveCombinedCommands(_commandsForArea);
				}

				internal void RemoveToolHandlers()
				{
					if (_commandsForTool == null)
					{
						return;
					}
					RemoveCombinedCommands(_commandsForTool);
				}

				internal void RemoveUserControlHandlers(UserControl userControl)
				{
					var currentUserControlCommands = _commandsForUserControls.FirstOrDefault(tuple => tuple.Item1 == userControl);
					if (currentUserControlCommands == null)
					{
						return;
					}
					_commandsForUserControls.Remove(currentUserControlCommands);
					RemoveCombinedCommands(currentUserControlCommands.Item2);
				}

				private void RemoveCombinedCommands(Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands)
				{
					foreach (var commandKvp in commands)
					{
						var commandKey = commandKvp.Key;
						var commandValue = commandKvp.Value;
						_combinedCommands.Remove(commandKey);
						var currentSubmenuOfMainMenu = _supportedMenuItems[commandKey];
						if (commandValue.Item1 != null)
						{
							// Remove supplied click event handler from menu item.
							currentSubmenuOfMainMenu.Click -= commandValue.Item1;
						}
						currentSubmenuOfMainMenu.Visible = false;
						currentSubmenuOfMainMenu.Enabled = false;
						currentSubmenuOfMainMenu.Tag = null;
					}
				}

				private void MainMenu_DropDownOpening(object sender, EventArgs e)
				{
					// Make relevant menus visible and deal with enabling for the visible ones.
					foreach (var commandKvp in _combinedCommands)
					{
						var canDo = commandKvp.Value.Item2.Invoke();
						var currentMenuItem = _supportedMenuItems[commandKvp.Key];
						currentMenuItem.Visible = canDo.Item1;
						currentMenuItem.Enabled = canDo.Item2;
					}
				}

				private void MainMenu_DropDownOpened(object sender, EventArgs e)
				{
					// Make separators visible, if there are any visible (even if disabled) menus prior to the given separator.
					if (!_separatorMenuBundles.Any())
					{
						// Nothing to do.
						return;
					}
					foreach (var separatorMenuBundle in _separatorMenuBundles)
					{
						var hasPrecedingVisible = false;
						foreach (var precedingItem in separatorMenuBundle.PrecedingItems)
						{
							if (precedingItem.Visible)
							{
								hasPrecedingVisible = true;
								break;
							}
						}
						var hasFollowingVisible = false;
						foreach (var followingItem in separatorMenuBundle.FollowingItems)
						{
							if (followingItem.Visible)
							{
								hasFollowingVisible = true;
								break;
							}
						}
						separatorMenuBundle.Separator.Visible = hasPrecedingVisible && hasFollowingVisible;
					}
				}
			}
		}

		private sealed class ToolBarsController
		{
			private readonly IReadOnlyDictionary<ToolBar, Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>>> _supportedToolBarItems;
			private readonly IReadOnlyDictionary<ToolBar, ToolBarController> _toolBarControllers;

			internal ToolBarsController(MainToolBarsParameterObject mainToolBarsParameterObject)
			{
				_supportedToolBarItems = mainToolBarsParameterObject.SupportedToolBarItems;
				_toolBarControllers = mainToolBarsParameterObject.MainToolBar.ToDictionary(mainToolBarKvp => mainToolBarKvp.Key, mainToolBarKvp => new ToolBarController(_supportedToolBarItems[mainToolBarKvp.Key].Item1, _supportedToolBarItems[mainToolBarKvp.Key].Item2));
			}

			internal IReadOnlyDictionary<Command, ToolStripItem> GetToolBarCommands(ToolBar toolBar)
			{
				return _supportedToolBarItems[toolBar].Item1;
			}

			internal void AddGlobalHandlers(IReadOnlyDictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> globalToolBarItems)
			{
				foreach (var toolBarControllerKvp in globalToolBarItems)
				{
					_toolBarControllers[toolBarControllerKvp.Key].AddGlobalHandlers(toolBarControllerKvp.Value);
				}
			}

			internal void AddAreaHandlers(IReadOnlyDictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> toolBarItemsForArea)
			{
				foreach (var toolBarControllerKvp in toolBarItemsForArea)
				{
					_toolBarControllers[toolBarControllerKvp.Key].AddAreaHandlers(toolBarControllerKvp.Value);
				}
			}

			internal void AddToolHandlers(IReadOnlyDictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> toolBarItemsForTool)
			{
				foreach (var toolBarControllerKvp in toolBarItemsForTool)
				{
					_toolBarControllers[toolBarControllerKvp.Key].AddToolHandlers(toolBarControllerKvp.Value);
				}
			}

			internal void AddUserControlHandlers(UserControl userControl, Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> toolBarItemsForUserControl)
			{
				foreach (var toolBarControllerKvp in toolBarItemsForUserControl)
				{
					_toolBarControllers[toolBarControllerKvp.Key].AddUserControlHandlers(userControl, toolBarControllerKvp.Value);
				}
			}

			internal void RemoveAreaHandlers()
			{
				foreach (var toolBarController in _toolBarControllers.Values)
				{
					toolBarController.RemoveAreaHandlers();
				}
			}

			internal void RemoveToolHandlers()
			{
				foreach (var toolBarController in _toolBarControllers.Values)
				{
					toolBarController.RemoveToolHandlers();
				}
			}

			internal void RemoveUserControlHandlers(UserControl userControl)
			{
				foreach (var toolBarController in _toolBarControllers.Values)
				{
					toolBarController.RemoveUserControlHandlers(userControl);
				}
			}

			private sealed class ToolBarController
			{
				private readonly IReadOnlyDictionary<Command, ToolStripItem> _supportedToolBarItems;
				private readonly List<ISeparatorToolStripBundle> _separatorToolStripBundles;
				private Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> _globalCommands = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();
				private Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> _commandsForArea;
				private Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> _commandsForTool;
				private readonly List<Tuple<UserControl, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>> _commandsForUserControls = new List<Tuple<UserControl, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>>();
				private readonly Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> _combinedCommands = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();

				internal ToolBarController(IReadOnlyDictionary<Command, ToolStripItem> supportedToolBarItems, List<ISeparatorToolStripBundle> separatorToolStripBundles)
				{
					_supportedToolBarItems = supportedToolBarItems;
					_separatorToolStripBundles = separatorToolStripBundles;
				}

				internal void AddGlobalHandlers(Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands)
				{
					AddCombinedCommands(commands, true);
				}

				internal void AddAreaHandlers(Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands)
				{
					_commandsForArea = commands;
					AddCombinedCommands(commands);
					Application.Idle += Application_Idle;
				}

				internal void AddToolHandlers(Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands)
				{
					_commandsForTool = commands;
					AddCombinedCommands(commands);
				}

				internal void AddUserControlHandlers(UserControl userControl, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands)
				{
					_commandsForUserControls.Add(new Tuple<UserControl, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>(userControl, commands));
					AddCombinedCommands(commands);
				}

				private void AddCombinedCommands(Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands, bool addToGlobal = false)
				{
					foreach (var commandKvp in commands)
					{
						var commandKey = commandKvp.Key;
						var commandValue = commandKvp.Value;
						if (addToGlobal)
						{
							_globalCommands.Add(commandKey, commandValue);
						}
						_combinedCommands.Add(commandKey, commandValue);
						if (commandValue.Item1 != null)
						{
							// Add supplied click event handler to menu item.
							_supportedToolBarItems[commandKey].Click += commandValue.Item1;
						}
					}
				}

				internal void RemoveAreaHandlers()
				{
					Application.Idle -= Application_Idle;
					if (_commandsForArea == null)
					{
						return;
					}
					RemoveCombinedCommands(_commandsForArea);
				}

				internal void RemoveToolHandlers()
				{
					if (_commandsForTool == null)
					{
						return;
					}
					RemoveCombinedCommands(_commandsForTool);
				}

				internal void RemoveUserControlHandlers(UserControl userControl)
				{
					var currentUserControlCommands = _commandsForUserControls.FirstOrDefault(tuple => tuple.Item1 == userControl);
					if (currentUserControlCommands == null)
					{
						return;
					}
					_commandsForUserControls.Remove(currentUserControlCommands);
					RemoveCombinedCommands(currentUserControlCommands.Item2);
				}

				private void RemoveCombinedCommands(Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> commands)
				{
					foreach (var commandKvp in commands)
					{
						var commandKey = commandKvp.Key;
						var commandValue = commandKvp.Value;
						_combinedCommands.Remove(commandKey);
						var currentToolStripItem = _supportedToolBarItems[commandKey];
						if (commandValue.Item1 != null)
						{
							// Remove supplied click event handler to tool bar button item.
							currentToolStripItem.Click -= commandValue.Item1;
						}
						currentToolStripItem.Visible = false;
						currentToolStripItem.Enabled = false;
						currentToolStripItem.Tag = null;
					}
				}

				private void Application_Idle(object sender, EventArgs e)
				{
					// Deal with all of the tool bar buttons (visibility and enabling) for my tool bar.
					foreach (var commandKvp in _combinedCommands)
					{
						var canDo = commandKvp.Value.Item2.Invoke();
						var currentToolStripItem = _supportedToolBarItems[commandKvp.Key];
						currentToolStripItem.Visible = canDo.Item1;
						currentToolStripItem.Enabled = canDo.Item2;
					}
					// Make separators visible, if there are any visible (even if disabled) buttons prior to the given separator.
					if (!_separatorToolStripBundles.Any())
					{
						// Nothing to do.
						return;
					}
					foreach (var separatorMenuBundle in _separatorToolStripBundles)
					{
						var hasPrecedingVisible = false;
						foreach (var precedingItem in separatorMenuBundle.PrecedingItems)
						{
							if (precedingItem.Visible)
							{
								hasPrecedingVisible = true;
								break;
							}
						}
						var hasFollowingVisible = false;
						foreach (var followingItem in separatorMenuBundle.FollowingItems)
						{
							if (followingItem.Visible)
							{
								hasFollowingVisible = true;
								break;
							}
						}
						separatorMenuBundle.Separator.Visible = hasPrecedingVisible && hasFollowingVisible;
					}
				}
			}
		}


#if RANDYTODO
		// TODO: Do I really want to go down this path and create them all at once, and just set handlers, visibility and enabled state, as is done for main menus and tool bar buttons?
#endif
		private sealed class ContextMenusController
		{
			private sealed class ContextMenuController
			{
			}
		}

		#endregion Private controller classes that do the real work
	}
}