// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: ButtonsMenusLinker.cs
// Responsibility: TeTeam
// Last reviewed:
//
// <remarks>
// Implements code so the "View" sub-menus (i.e. Scripture, Back Translation, Checking, etc)
// and their sub-menus (i.e. Draft, Print Layout, Key Terms, etc) and the InformationBar
// buttons and their context menus are dynamically created and removed based on the existing
// sideBar tabs and the adding and removal of sideBar tab buttons.
//
// You use the ButtonsMenusLinker by setting ViewMenu, SideBar, and InformationBar with
// their respective setter functions.  For example, the FwMainWnd.cs constructor uses
// this code to use the ButtonsMenusLinker:
//
//		buttonsMenusLinker1.ViewMenu = m_mnuView;
//		buttonsMenusLinker1.SideBar = sideBarFw;
//		buttonsMenusLinker1.InformationBar = informationBar;
//
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Dynamically creates InfoBar buttons, their context menus, and View sub-menus and their
	/// sub-menus and links their behavior together.
	/// </summary>
	public class ButtonsMenusLinker
	{
		#region Member variables
		/// <summary>MenuItem to which items will be added for the sidebar buttons.</summary>
		protected MenuItem m_mnuView;

		/// <summary>Menu extender used on the form on which m_mnuView is found.</summary>
		protected MenuExtender m_menuExtender;

		/// <summary>Sidebar</summary>
		protected SideBar sideBarFw;

		/// <summary>InfomationBar</summary>
		protected InformationBar informationBar;

		private ContextMenu m_cmnuInfoBarButton = null;
		private MenuItem[] m_mnuViewCollection = null;

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the menu extender on the form containing menu used to set the ViewMenu
		/// property.
		/// <value>The <c>MenuExtender</c> that sub-menus are added to the bottom of.</value>
		/// </summary>
		/// <remarks>This property should be set before the ViewMenu property.</remarks>
		/// ------------------------------------------------------------------------------------
		public MenuExtender MenuExtender
		{
			get {return m_menuExtender;}
			set {m_menuExtender = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the main menu item (i.e. View) that will contain the new tab button
		/// menus.
		/// <value>The <c>ViewMenu</c> that sub-menus are added to the bottom of.</value>
		/// </summary>
		/// <remarks>This will usually be the 'View' menu. Note: the MenuExtender should
		/// be set before setting this property.)</remarks>
		/// ------------------------------------------------------------------------------------
		public MenuItem ViewMenu
		{
			get {return m_mnuView;}
			set
			{
				m_mnuView = value;
				BuildInfoBarButtons();
				BuildViewMenu();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The side bar that contains tabs and tab buttons.
		/// </summary>
		/// <value>The <c>SideBar</c> that contains tabs and tab buttons.</value>
		/// -----------------------------------------------------------------------------------
		public SideBar SideBar
		{
			get {return sideBarFw;}
			set
			{
				sideBarFw = value;
				BuildInfoBarButtons();
				BuildViewMenu();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The InformationBar that icons and context clone menus will be added to.
		/// </summary>
		/// <value>The <c>InformationBar</c> that icons and context menus will be added
		///  to.</value>
		/// -----------------------------------------------------------------------------------
		public InformationBar InformationBar
		{
			get {return informationBar;}
			set
			{
				informationBar = value;
				BuildInfoBarButtons();
			}
		}

		#endregion

		#region Miscellaneous methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Build information bar buttons based on the sideBar tabs.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void BuildInfoBarButtons()
		{
			if (m_mnuView == null || informationBar == null ||
				sideBarFw == null || sideBarFw.Tabs.Count == 0)
			{
				return;
			}

			// First, clear all the buttons from the info bar..
			informationBar.Buttons.Clear();

			foreach (SideBarTab tab in sideBarFw.Tabs)
			{
				// Add event handlers for adding and removing tab buttons
				tab.Buttons.AfterInsert +=
					new SideBarButtonCollection.CollectionChange(tabButton_AddOrRemove);
				tab.Buttons.BeforeRemove +=
					new SideBarButtonCollection.CollectionChange(tabButton_AddOrRemove);

				// Add a 'Button Click' event to each tab
				tab.ButtonClickEvent += new EventHandler(tabButton_Click);

				// Create an information button based on the tab and add it to the info bar
				InformationBarButton infoButton = new InformationBarButton();
				infoButton.Text = tab.Title;
				infoButton.Tag = tab;
				infoButton.ImageList = sideBarFw.ImageListSmall;
				informationBar.Buttons.Add(infoButton);
				infoButton.Click += new EventHandler(infoButton_Click);
				infoButton.MouseHover += new EventHandler(infoButton_MouseHover);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh each info bar button's icon to reflect the current selection within that
		/// button's associated sidebar tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshInfoBarButtons()
		{
			foreach (InformationBarButton button in informationBar.Buttons)
			{
				SideBarTab tab = button.Tag as SideBarTab;
				if (tab != null && tab.IntSelection.Length > 0)
					button.ImageIndex = tab.Buttons[tab.IntSelection[0]].ImageIndex; // use the first selection
				else
					button.ImageIndex = sideBarFw.ImageListSmall.Images.Count - 1; // default to last image
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a menu for each tab and adds them to the spcecified view menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void BuildViewMenu()
		{
			if (ViewMenu == null || sideBarFw == null || sideBarFw.Tabs.Count == 0 ||
				MenuExtender == null)
			{
				return;
			}

			// If necessary, remove existing sidebar menu items from the view menu.
			if (m_mnuViewCollection != null)
				RemoveOurViewMenuItems();

			List<MenuItem> mnuList = new List<MenuItem>();

			// Go through each tab and add a menu with sub menus off the specified view menu.
			int position = 0;
			foreach (SideBarTab tab in sideBarFw.Tabs)
			{
				MenuItem menuItem = new MenuItem(tab.Title);
				mnuList.Add(menuItem);

				menuItem.MenuItems.AddRange(BuildMenusForSideBarTab(tab));
				ViewMenu.MenuItems.Add(position, menuItem);
				MenuExtender.AddMenuItem(menuItem);
				position++;
			}

			// Save the collection of meun items we added to the view menu so we can remove
			// them later if the number of buttons or tabs in the sidebar changes.
			if (mnuList.Count > 0)
				m_mnuViewCollection = mnuList.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove all the items from the view menu that correspond to the sidebar tabs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RemoveOurViewMenuItems()
		{
			// Iterate through the collection of menu items we've previously added
			// to the view menu.
			foreach (MenuItem menuItem in m_mnuViewCollection)
			{
				// Find the menu item in the view menu's menu item collection and
				// remove that menu item from the view menu.
				foreach (MenuItem mnuTabItem in ViewMenu.MenuItems)
				{
					// Have we found one of our items in the view menu?
					if (menuItem == mnuTabItem)
					{
						// Does our view menu item have sub items?
						if (mnuTabItem.MenuItems.Count > 0)
						{
							// Clear the sub items from the menu extender.
							foreach (MenuItem mnuButtonItem in mnuTabItem.MenuItems)
								MenuExtender.ClearMenuItem(mnuButtonItem);
						}

						// Remove our view menu item from the view menu.
						ViewMenu.MenuItems.Remove(mnuTabItem);
						break;
					}
				}
			}

			m_mnuViewCollection = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds a menu collection for the specified sidebar tab. The collection contains a
		/// menu item for each button plus a configure item at the end.
		/// </summary>
		/// <param name="tab">given sidebar tab</param>
		/// <returns>array of menu items from sidebar tab</returns>
		/// ------------------------------------------------------------------------------------
		public MenuItem[] BuildMenusForSideBarTab(SideBarTab tab)
		{
			List<MenuItem> mnuList = new List<MenuItem>();

			// Create a menu item for each button on the sidebar tab.
			foreach (SideBarButton button in tab.Buttons)
			{
				MenuItem menuItem = new MenuItem(button.Text);
				MenuExtender.AddMenuItem(menuItem);
				MenuExtender.SetTag(menuItem, button);
				MenuExtender.SetCommandId(menuItem, "SideBarButtonMenu");
				mnuList.Add(menuItem);
			}

			// Create the separator
			MenuItem mnuSepr = new MenuItem("-");
			MenuExtender.AddMenuItem(mnuSepr);
			MenuExtender.SetCommandId(mnuSepr, tab.ConfigureMenuCommandId);
			MenuExtender.SetTag(mnuSepr, tab);
			mnuList.Add(mnuSepr);

			// Create the configure menu item.
			MenuItem cfgMenuItem = new MenuItem(tab.ConfigureMenuText);
			MenuExtender.AddMenuItem(cfgMenuItem);
			MenuExtender.SetCommandId(cfgMenuItem, tab.ConfigureMenuCommandId);
			MenuExtender.SetImageList(cfgMenuItem, tab.ConfigureMenuImageList);
			MenuExtender.SetImageIndex(cfgMenuItem, tab.ConfigureMenuImageIndex);
			MenuExtender.SetTag(cfgMenuItem, tab);
			mnuList.Add(cfgMenuItem);

			return mnuList.ToArray();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Press the given tab button, and update the menu and info bar appropriately.
		/// </summary>
		/// <param name='tabButtonName'>The .Name of the button to press (not the button's text).
		/// </param>
		/// -----------------------------------------------------------------------------------
		public void PressTabButton(string tabButtonName)
		{
			foreach (SideBarTab tab in sideBarFw.Tabs)
			{
				// Loop through the tab's buttons looking for the desired name
				foreach (SideBarButton button in tab.Buttons)
				{
					if (tabButtonName == button.Name)
					{
						// We have located the given sidebar button. Now process all the buttons
						// under this tab: press the given button and unpress all its siblings.
						foreach (SideBarButton findButton in tab.Buttons)
							findButton.PressButton(button.Name == findButton.Name);

						// update the menu and infobar
						tabButton_Click(button, null);
						break; // all done
					}
				}
			}
		}

		#endregion

		#region Information Bar Buttons Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This will build a tooltip for a info. bar button that's made up of the title of
		/// the button's corresponding sidebar tab (i.e. task tab) and -- if the tab is the
		/// active sidebar tab -- the tab's current button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void infoButton_MouseHover(object sender, EventArgs e)
		{
			InformationBarButton button = sender as InformationBarButton;

			if (button == null || button.Tag == null)
				return;

			SideBarTab tab = button.Tag as SideBarTab;

			if (tab == null)
				return;

			if (tab == sideBarFw.ActiveTab)
			{
				button.TooltipText = String.Format(FrameworkStrings.ksInfoBarBtnToolTipFmt,
					tab.Title, tab.Buttons[tab.IntSelection[0]].Text);
			}
			else
			{
				button.TooltipText = tab.Title;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This will trap clicks on the info. bar buttons and build a context menu containing
		/// one item for each side bar button in the corresponding sidebar tab. It will also
		/// add a title item with the corresponding tab's title.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void infoButton_Click(object sender, EventArgs e)
		{
			InformationBarButton button = sender as InformationBarButton;

			if (button == null || button.Tag == null)
				return;

			SideBarTab tab = button.Tag as SideBarTab;

			if (tab == null)
				return;

			if (m_cmnuInfoBarButton == null)
				m_cmnuInfoBarButton = new ContextMenu();
			else
			{
				// Clear the menus items so that we start fresh the next time the
				// user clicks on the information bar button.
				foreach (MenuItem menuItem in m_cmnuInfoBarButton.MenuItems)
					MenuExtender.ClearMenuItem(menuItem);

				m_cmnuInfoBarButton.MenuItems.Clear();
			}

			// Load the context menu
			m_cmnuInfoBarButton.MenuItems.Add(new LabelMenuItem(tab.Title));
			m_cmnuInfoBarButton.MenuItems.AddRange(BuildMenusForSideBarTab(tab));

			MenuExtender.AddContextMenu(m_cmnuInfoBarButton);

			// Popup the loaded ContextMenu just below the button.
			m_cmnuInfoBarButton.Show(button, new Point(0, button.Bottom));
		}

		#endregion

		#region Tab Button Event handlers
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Add a new menu item for the new SideBar tab button.
		/// </summary>
		///
		/// <param name="index">The zero-based index at which <c>sender</c> can be found.</param>
		/// <param name='sender'>The tab button that was added.</param>
		/// -----------------------------------------------------------------------------------
		private void tabButton_AddOrRemove(int index, object sender)
		{
			BuildViewMenu();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// When a sidebar button is clicked update the info bar text and info. button for
		/// the new current sidebar button.
		/// </summary>
		///
		/// <param name='sender'>The SideBar tab button that was clicked.</param>
		/// <param name='e'>The event that caused this function to be called.</param>
		/// -----------------------------------------------------------------------------------
		private void tabButton_Click(object sender, System.EventArgs e)
		{
			SideBarButton sideBarButton = (SideBarButton)sender;
			SideBarTab sideBarTab = (SideBarTab)sideBarButton.Parent.Parent;

			// Update the info bar caption
			informationBar.InfoBarLabel.Text = sideBarTab.Title + ' ' + sideBarButton.Text;

			// find the info bar button associated with the sidebar tab and set the
			// button's icon to the sidebar button's icon.
			foreach (InformationBarButton button in informationBar.Buttons)
			{
				// Update the info bar button's image
				if ((SideBarTab)button.Tag == sideBarTab)
					button.ImageIndex = sideBarButton.ImageIndex;
			}
		}

		#endregion
	}
}
