// Original author or copyright holder unknown.

#if USE_THIS
using System;
using System.Windows.Forms;
using System.Drawing;
using SidebarLibrary.Collections;
using SidebarLibrary.Menus;
using SidebarLibrary.WinControls;

namespace SidebarLibrary.CommandBars
{
	/// <summary>
	/// Summary description for ToolBarItemMenu.
	/// </summary>
	public class ChevronMenu : PopupMenu
	{
		ToolBarItemCollection items = new ToolBarItemCollection();

		public ToolBarItemCollection Items
		{
			set { items = value; Attach(); }
			get { return items; }
		}

		private void AddSubMenu(MenuCommand parentMenuCommand, MenuItem[] items)
		{
			for ( int i = 0; i < items.Length; i++ )
			{
				// I know these menu items are actually MenuItemExs
				MenuItemEx item = (MenuItemEx)items[i];
				MenuCommand currentMenuCommand = new MenuCommand(item.Text, (Bitmap)item.Icon,
					(Shortcut)item.Shortcut, item.ClickHandler, item);
				parentMenuCommand.MenuCommands.Add(currentMenuCommand);
				if ( item.MenuItems.Count > 0 )
					AddSubMenu(currentMenuCommand, item.MenuItems);
			}
		}

		private void AddSubMenu(MenuCommand parentMenuCommand, Menu.MenuItemCollection items)
		{
			for ( int i = 0; i < items.Count; i++ )
			{
				// I know these menu items are actually MenuItemExs
				MenuItemEx item = (MenuItemEx)items[i];
				MenuCommand currentMenuCommand = new MenuCommand(item.Text, (Bitmap)item.Icon,
					(Shortcut)item.Shortcut, item.ClickHandler, item);
				parentMenuCommand.MenuCommands.Add(currentMenuCommand);
				if ( item.MenuItems.Count > 0 )
					AddSubMenu(currentMenuCommand, item.MenuItems);
			}
		}

		void Attach()
		{
			// Cleanup previous menus
			MenuCommands.Clear();

			foreach (ToolBarItem item in items)
			{
				string text = item.Text;
				if ( text == string.Empty || text == null )
					text = item.ToolTip;
				if ( item.Style == ToolBarItemStyle.Separator )
					text = "-";

				// If this is a combobox
				if ( item.ComboBox != null )
				{
					MenuCommands.Add(new MenuCommand(item.ComboBox));
					item.ComboBox.Visible = true;
					// I know where this combobox comes from
					ComboBoxBase cbb = (ComboBoxBase)item.ComboBox;
					cbb.ToolBarUse = false;
					continue;
				}

				MenuCommand currentMenuCommand = new MenuCommand(text, (Bitmap)item.Image,
					(Shortcut)item.Shortcut, item.ClickHandler, item);
				MenuCommands.Add(currentMenuCommand);

				// If we have a menubar
				if ( item.MenuItems != null)
					AddSubMenu(currentMenuCommand, item.MenuItems);

			}
		}
	}


}
#endif
