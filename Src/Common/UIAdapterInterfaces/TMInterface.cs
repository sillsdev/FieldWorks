// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MenuInterface.cs
// Responsibility: David Olson
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using XCore;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.UIAdapters
{
	#region ITMAdapter Interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ITMAdapter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a Toolbar/Menu adapter object.
		/// </summary>
		/// <param name="parentForm">The form owning the toolbars.</param>
		/// <param name="messageMediator">An XCore message mediator used for message routing.
		/// </param>
		/// <param name="definitions">XML strings defining all the menus/toolbars in an
		/// application.</param>
		/// ------------------------------------------------------------------------------------
		void Initialize(Form parentForm, Mediator messageMediator, string[] definitions);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a Toolbar/Menu adapter object.
		/// </summary>
		/// <param name="parentForm">The form owning the toolbars.</param>
		/// <param name="messageMediator">An XCore message mediator used for message routing.
		/// </param>
		/// <param name="appsRegKeyPath">The registry path where the application's settings
		/// are stored. (e.g. "Software\SIL\FieldWorks")</param>
		/// <param name="definitions">XML strings defining all the menus/toolbars in an
		/// application.</param>
		/// ------------------------------------------------------------------------------------
		void Initialize(Form parentForm, Mediator messageMediator, string appsRegKeyPath,
			string[] definitions);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a Toolbar/Menu adapter object.
		/// </summary>
		/// <param name="parentForm">The form owning the toolbars.</param>
		/// <param name="contentPanelContent">The control on parentForm that contains all
		/// the child controls of the form. The adapter will use that control (if it's not
		/// null) to put inside a tool strip docking manager (e.g. ToolStripContainer
		/// control).</param>
		/// <param name="messageMediator">An XCore message mediator used for message routing.
		/// </param>
		/// <param name="definitions">XML strings defining all the menus/toolbars in an
		/// application.</param>
		/// ------------------------------------------------------------------------------------
		void Initialize(Form parentForm, Control contentPanelContent,
			Mediator messageMediator, string[] definitions);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a Toolbar/Menu adapter object.
		/// </summary>
		/// <param name="parentForm">The form owning the toolbars.</param>
		/// <param name="contentPanelContent">The control on parentForm that contains all
		/// the child controls of the form. The adapter will use that control (if it's not
		/// null) to put inside a tool strip docking manager (e.g. ToolStripContainer
		/// control).</param>
		/// <param name="messageMediator">An XCore message mediator used for message routing.
		/// </param>
		/// <param name="appsRegKeyPath">The registry path where the application's settings
		/// are stored. (e.g. "Software\SIL\FieldWorks")</param>
		/// <param name="definitions">XML strings defining all the menus/toolbars in an
		/// application.</param>
		/// ------------------------------------------------------------------------------------
		void Initialize(Form parentForm, Control contentPanelContent,
			Mediator messageMediator, string appsRegKeyPath, string[] definitions);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Dispose();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties of a toolbar/menu item.
		/// </summary>
		/// <param name="name">Name of the toolbar/menu item whose properties are being
		/// requested.
		/// </param>
		/// <returns>The properties of the toolbar/menu item.</returns>
		/// ------------------------------------------------------------------------------------
		TMItemProperties GetItemProperties(string name);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the properties of a toolbar/menu item to those stored in a TMItemProperties
		/// object.
		/// </summary>
		/// <param name="name">Name of the toolbar/menu item to modify.</param>
		/// <param name="itemProps">Properties used to modfy the toolbar/menu item.</param>
		/// ------------------------------------------------------------------------------------
		void SetItemProperties(string name, TMItemProperties itemProps);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new submenu item to the menu specified by parentItemName and inserts it
		/// before the item specified by insertBeforeItem. If insertBeforeItem is null, then
		/// the new submenu item is added to the end of parentItemName's menu collection.
		/// </summary>
		/// <param name="itemProps">Properties of the new menu item.</param>
		/// <param name="parentItemName">Name of the menu item that will be added to.</param>
		/// <param name="insertBeforeItem">Name of the submenu item before which the new
		/// menu item will be added.</param>
		/// ------------------------------------------------------------------------------------
		void AddMenuItem(TMItemProperties itemProps, string parentItemName, string insertBeforeItem);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new menu item to a context menu specified by contextMenuName and inserts it
		/// before the item specified by insertBeforeItem. If insertBeforeItem is null, then
		/// the new menu item is added to the end of parentItemName's menu collection.
		/// </summary>
		/// <param name="itemProps">Properties of the new menu item.</param>
		/// <param name="contextMenuName">Name of the context menu to which the item is added.
		/// </param>
		/// <param name="insertBeforeItem">Name of the context menu item before which the new
		/// menu item will be added.</param>
		/// ------------------------------------------------------------------------------------
		void AddContextMenuItem(TMItemProperties itemProps, string contextMenuName, string insertBeforeItem);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new menu item to a context menu specified by contextMenuName and inserts it
		/// before the item specified by insertBeforeItem. If insertBeforeItem is null, then
		/// the new menu item is added to the end of parentItemName's menu collection. This
		/// overload allows new menu items to be added as submenus to menus at the top level
		/// of the context menu. The parentMenuName can be the name of a menu item at any
		/// level within the hierarchy of the menus on the context menu.
		/// </summary>
		/// <param name="itemProps">Properties of the new menu item.</param>
		/// <param name="contextMenuName">Name of the context menu to which the item is added.
		/// </param>
		/// <param name="parentMenuName">Name of the menu item in the context menu under which
		/// the new item is added.</param>
		/// <param name="insertBeforeItem">Name of the context menu item before which the new
		/// menu item will be added.</param>
		/// ------------------------------------------------------------------------------------
		void AddContextMenuItem(TMItemProperties itemProps, string contextMenuName,
			string parentMenuName, string insertBeforeItem);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes all the subitems of the specified menu.
		/// </summary>
		/// <param name="parentItemName">The name of the item whose subitems will be removed.
		/// </param>
		/// ------------------------------------------------------------------------------------
		void RemoveMenuSubItems(string parentItemName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified item from the specified parent menu.
		/// </summary>
		/// <param name="parentItemName">The name of the item whose subitem will be removed.
		/// </param>
		/// <param name="name">subitem to remove from parent menu.</param>
		/// ------------------------------------------------------------------------------------
		void RemoveMenuItem(string parentItemName, string name);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Pops-up a menu so it shows like a context menu. If the item doesn't have any
		/// sub items, then this command is ignored.
		/// </summary>
		/// <param name="name">The name of the item to popup. The name could be the name of
		/// a menu off the application's menu bar, or one of the context menu's added to the
		/// menu adapter.</param>
		/// <param name="x">The X location (on the screen) where the menu is popped-up.</param>
		/// <param name="y">The Y location (on the screen) where the menu is popped-up.</param>
		/// ------------------------------------------------------------------------------------
		void PopupMenu(string name, int x, int y);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Pops-up a menu so it shows like a context menu. If the item doesn't have any
		/// sub items, then this command is ignored.
		/// </summary>
		/// <param name="name">The name of the item to popup. The name could be the name of
		/// a menu off the application's menu bar, or one of the context menu's added to the
		/// menu adapter.</param>
		/// <param name="x">The X location (on the screen) where the menu is popped-up.</param>
		/// <param name="y">The Y location (on the screen) where the menu is popped-up.</param>
		/// <param name="subItemsToRemoveOnClose">list of items to remove from the menu when
		/// it closes</param>
		/// ------------------------------------------------------------------------------------
		void PopupMenu(string name, int x, int y, List<string> subItemsToRemoveOnClose);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the context menu for a specified control.
		/// </summary>
		/// <param name="ctrl">Control which is being assigned a context menu.</param>
		/// <param name="name">The name of the context menu to assign to the control.</param>
		/// ------------------------------------------------------------------------------------
		void SetContextMenuForControl(Control ctrl, string name);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows an application to force the hiding of a toolbar item's popup control.
		/// </summary>
		/// <param name="name">Name of item whose popup should be hidden.</param>
		/// ------------------------------------------------------------------------------------
		void HideBarItemsPopup(string name);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows an application to hide of a toolbar.
		/// </summary>
		/// <param name="name">Name of toolbar to hide.</param>
		/// ------------------------------------------------------------------------------------
		void HideToolBar(string name);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows an application to show of a toolbar.
		/// </summary>
		/// <param name="name">Name of toolbar to show.</param>
		/// ------------------------------------------------------------------------------------
		void ShowToolBar(string name);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties of a toolbar.
		/// </summary>
		/// <param name="name">Name of the toolbar whose properties are being requested.
		/// </param>
		/// <returns>The properties of the toolbar.</returns>
		/// ------------------------------------------------------------------------------------
		TMBarProperties GetBarProperties(string name);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the properties of a toolbar/menu to those stored in a TMBarProperties
		/// object.
		/// </summary>
		/// <param name="name">Name of the toolbar/menu to modify.</param>
		/// <param name="barProps">Properties used to modfy the toolbar/menu item.</param>
		/// ------------------------------------------------------------------------------------
		void SetBarProperties(string name, TMBarProperties barProps);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the control contained within a control container toolbar item.
		/// </summary>
		/// <param name="name">Name of the control container item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		Control GetBarItemControl(string name);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Causes the adapter to save toolbar settings (e.g. user placement of toolbars).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SaveBarSettings();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Causes the adapter to show it's dialog for customizing toolbars.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ShowCustomizeDialog();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the adapter's parent form. This will not cause docking sites to be added to
		/// the form and DotNetBar manager.
		/// </summary>
		/// <param name="newForm">The new parent form.</param>
		/// <returns>The adapter's previous parent form.</returns>
		/// ------------------------------------------------------------------------------------
		Form SetParentForm(Form newForm);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified Keys is a shortcut for a toolbar or menu item.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="isItemEnabled"><c>true</c> when the specified key is a shortcut key
		/// and its associated toolbar or menu item is enabled.</param>
		/// <returns>
		/// 	<c>true</c> if the specified key is a shortcut for a toolbar or menu item;
		/// 	otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		bool IsShortcutKey(Keys key, ref bool isItemEnabled);

		#region Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event fired when adding toolbar/menu items to a menu, menu bar or toolbar. This
		/// allows delegates of this event to initialize properties of the menu item such as
		/// its text, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		event InitializeItemHandler InitializeItem;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event fired when adding a toolbar to the toolbar container. This allows
		/// delegates of this event to initialize properties of the toolbar such as its text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		event InitializeBarHandler InitializeBar;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event fired when adding a combobox toolbar item. This gives applications a chance
		/// to initialize a combobox item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		event InitializeComboItemHandler InitializeComboItem;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event fired when a control container item requests the control to contain.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		event LoadControlContainerItemHandler LoadControlContainerItem;

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the message mediator used by the menu adapter for message dispatch.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Mediator MessageMediator
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not updates to toolbar/menu items should
		/// take place during the application's idle cycles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool AllowUpdates
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of toolbar property objects an application can use to send to a
		/// menu extender to use for display on a view menu allowing users to toggle the
		/// visibility of each toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		TMBarProperties[] BarInfoForViewMenu
		{
			get;
		}

		#endregion
	}

	#endregion
}
