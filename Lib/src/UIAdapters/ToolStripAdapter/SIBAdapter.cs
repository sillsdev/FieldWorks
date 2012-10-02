// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SIBAdapter.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;
using DevComponents.DotNetBar;
using XCore;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.UIAdapters
{
	/// <summary>
	/// Summary description for SIBAdapter.
	/// </summary>
	public class SIBAdapter : ISIBInterface, IxCoreColleague, IDisposable
	{
		private const string kLargeIconModeItem = "LargeIcons";
		private const string kSmallIconModeItem = "SmallIcons";
		private const string kHideSideBar = "HideSideBar";
		private bool m_supressAutoEvent = false;
		private ButtonItem m_contextMenu;
		private PanelEx m_infoBar;
		private Mediator m_mediator;
		private Hashtable m_htInfoBarButtons = new Hashtable();
		private ImageList m_largeItemImages = null;
		private ImageList m_smallItemImages = null;
		private System.Windows.Forms.ToolTip m_tooltip = new System.Windows.Forms.ToolTip();
		internal NavigationPane m_navPane;
		internal DotNetBarManager m_dnbMngr;

		#region ISIBInterface Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the visible state of the sidebar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SideBarVisible
		{
			get {return m_navPane.Parent.Visible;}
			set {m_navPane.Parent.Visible = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the visible state of the info. bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool InformationBarVisible
		{
			get {return m_infoBar.Parent.Visible;}
			set {m_infoBar.Parent.Visible = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text on the information bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InformationBarText
		{
			get {return (m_infoBar == null ? null : m_infoBar.Text);}
			set
			{
				if (m_infoBar == null)
					SetupInfoBar(null);

				m_infoBar.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the array of tabs contained in the sidebar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SBTabProperties[] Tabs
		{
			get
			{
				List<SBTabProperties> tabProps = new List<SBTabProperties>();

				foreach (SideBarTab tab in m_navPane.Items)
					tabProps.Add(tab.Properties);

				return tabProps.ToArray();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the image list for small icon mode for the individual items on side
		/// bar tabs. There is one image list for small icon mode that's used for all tabs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImageList ItemImageListSmall
		{
			get	{return m_smallItemImages;}
			set
			{
				m_smallItemImages = value;
				foreach (SideBarTab tab in m_navPane.Items)
					tab.ItemImageListSmall = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the image list for large icon mode for the individual items on side
		/// bar tabs. There is one image list for large icon mode that's used for all tabs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImageList ItemImageListLarge
		{
			get	{return m_largeItemImages;}
			set
			{
				m_largeItemImages = value;
				foreach (SideBarTab tab in m_navPane.Items)
					tab.ItemImageListLarge = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the image list for side bar tab icons that appear on the tab buttons
		/// (i.e. next to the tab's text) as well as the icons that appear on the information
		/// bar buttons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImageList TabImageList
		{
			get {return m_navPane.NavigationBar.Images;}
			set
			{
				m_navPane.NavigationBar.Images = value;
				foreach (SideBarTab tab in m_navPane.Items)
					tab.UpdateInfoBarButtonImage();

				PositionInfoBarButtons();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the image used for the context menu item that allows the
		/// user to switch to large icons on the side bar. It is assumed the image for those
		/// menu items is found in the same image list that's specified for the TabImageList
		/// property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int LargeIconModeImageIndex
		{
			get
			{
				SetupSideBarsContextMenu();
				return ((ButtonItem)m_contextMenu.SubItems[kLargeIconModeItem]).ImageIndex;
			}
			set
			{
				SetupSideBarsContextMenu();
				((ButtonItem)m_contextMenu.SubItems[kLargeIconModeItem]).ImageIndex = value;
				SetContextMenuImages();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the image used for the context menu item that allows the
		/// user to switch to small icons on the side bar. It is assumed the image for those
		/// menu items is found in the same image list that's specified for the TabImageList
		/// property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SmallIconModeImageIndex
		{
			get
			{
				SetupSideBarsContextMenu();
				return ((ButtonItem)m_contextMenu.SubItems[kSmallIconModeItem]).ImageIndex;
			}
			set
			{
				SetupSideBarsContextMenu();
				((ButtonItem)m_contextMenu.SubItems[kSmallIconModeItem]).ImageIndex = value;
				SetContextMenuImages();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current tab's properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SBTabProperties CurrentTabProperties
		{
			get
			{
				SideBarTab tab = m_navPane.CheckedButton as SideBarTab;
				return (tab == null ? null : tab.Properties);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties of the current item on the current tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SBTabItemProperties CurrentTabItemProperties
		{
			get
			{
				SideBarTab tab = m_navPane.CheckedButton as SideBarTab;
				return (tab == null ? null : tab.CurrentItemProps);
			}
		}

		#endregion

		#region Internal Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal SideBarTab CurrentTab
		{
			get	{return m_navPane.CheckedButton as SideBarTab;}
		}

		#endregion

		#region ISIBInterface Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sbContainer">The control that contains the sidebar control.</param>
		/// <param name="ibContainer">The control that contains the information bar.</param>
		/// <param name="mediator">XCore message mediator through which messages are sent
		/// for tab and tab item clicks.</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(Control sbContainer, Control ibContainer, Mediator mediator)
		{
			if (sbContainer == null)
				return;

			m_mediator = mediator;
			m_navPane = new NavigationPane();
			m_navPane.Dock = DockStyle.Fill;

			//Setup context menu to allow switching between large and small icon mode.
			SetupSideBarsContextMenu();

			SetupInfoBar(ibContainer);

			m_navPane.AllowDrop = false;
			m_navPane.AutoSizeButtonImage = true;
			m_navPane.ImageSizeSummaryLine = new Size(16, 16);
			m_navPane.ConfigureAddRemoveVisible = false;
			m_navPane.ConfigureShowHideVisible = false;
			m_navPane.ConfigureNavOptionsVisible = false;
			m_navPane.Load += new EventHandler(m_navPane_Load);
			m_navPane.PanelChanging += m_navPane_PanelChanging;

			m_navPane.TitlePanel.Dock = DockStyle.Top;
			m_navPane.TitlePanel.Font = Font.FromLogFont(Win32Api.NonClientMetrics.lfCaptionFont);
			m_navPane.TitlePanel.Size = new Size(1, ibContainer == null ? 28 : ibContainer.Height);
			m_navPane.TitlePanel.Style.Border = eBorderType.SingleLine;
			m_navPane.TitlePanel.Style.MarginLeft = 4;
			m_navPane.TitlePanel.Style.Alignment = StringAlignment.Near;
			m_navPane.TitlePanel.MouseEnter += TitlePanel_MouseEnter;

			if (m_dnbMngr.IsThemeActive)
				m_navPane.TitlePanel.ColorSchemeStyle = eDotNetBarStyle.Office2003;
			else
				m_navPane.TitlePanel.Paint += PaintNonThemedPanel;

			sbContainer.Controls.Add(m_navPane);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="tabProps"></param>
		/// <param name="infoBarButton"></param>
		/// ------------------------------------------------------------------------------------
		public void AddTab(SBTabProperties tabProps)
		{
			if (tabProps == null)
				return;

			SideBarTab tab = new SideBarTab(this, m_mediator, tabProps);
			tab.ItemImageListSmall = ItemImageListSmall;
			tab.ItemImageListLarge = ItemImageListLarge;
			tab.InfoBarButton.MouseEnter += new EventHandler(InfoBarButton_MouseEnter);
			m_dnbMngr.ContextMenus.Add(tab.Menu);
			m_infoBar.Controls.Add(tab.InfoBarButton);
			PositionInfoBarButtons();

			// Add the new tab's configure menu item to the side bar's context menu.
			ButtonItem tabCfgMenuItem = (ButtonItem)tab.ConfigureMenuItem.Copy();
			tabCfgMenuItem.Visible = true;
			m_contextMenu.SubItems.Add(tabCfgMenuItem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an item to the specified tab.
		/// </summary>
		/// <param name="tabIndex"></param>
		/// <param name="itemProps"></param>
		/// ------------------------------------------------------------------------------------
		public void AddTabItem(int tabIndex, SBTabItemProperties itemProps)
		{
			try
			{
				AddTabItem(m_navPane.Items[tabIndex].Name, itemProps);
			}
			catch
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an item to the specified tab.
		/// </summary>
		/// <param name="tabName"></param>
		/// <param name="itemProps"></param>
		/// ------------------------------------------------------------------------------------
		public void AddTabItem(string tabName, SBTabItemProperties itemProps)
		{
			SideBarTab tab = m_navPane.Items[tabName] as SideBarTab;
			if (tab != null)
				tab.AddTabItem(itemProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current sidebar tab.
		/// </summary>
		/// <param name="tabIndex">index of tab to make current.</param>
		/// <param name="generateEvents"><c>true</c> to cause events to be fired when setting
		/// the current sidebar tab (like when the user clicks on an item). Otherwise,
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		public void SetCurrentTab(int tabIndex, bool generateEvents)
		{
			if (tabIndex >= 0 && tabIndex < m_navPane.Items.Count &&
				m_navPane.Items[tabIndex] != m_navPane.CheckedButton)
			{
				SetCurrentTab(m_navPane.Items[tabIndex].Name, generateEvents);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current sidebar tab.
		/// </summary>
		/// <param name="tabName"></param>
		/// <param name="generateEvents"><c>true</c> to cause events to be fired when setting
		/// the current sidebar tab (like when the user clicks on an item). Otherwise,
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		public void SetCurrentTab(string tabName, bool generateEvents)
		{
			try
			{
				m_supressAutoEvent = !generateEvents;
				((ButtonItem)m_navPane.Items[tabName]).Checked = true;
				m_supressAutoEvent = false;

				//if (generateEvents)
				//    m_navPane.CheckedButton.RaiseClick();
			}
			catch
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current sidebar tab item.
		/// </summary>
		/// <param name="tabIndex">index of tab containing the tab item to make current.</param>
		/// <param name="itemName">Name of tab item to make current.</param>
		/// <param name="generateEvents"><c>true</c> to cause events to be fired when setting
		/// the current sidebar tab item (like when the user clicks on an item). Otherwise,
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		public void SetCurrentTabItem(int tabIndex, string itemName, bool generateEvents)
		{
			if (tabIndex < 0 || tabIndex >= m_navPane.Items.Count)
				return;

			SetCurrentTabItem(m_navPane.Items[tabIndex].Name, itemName, generateEvents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current sidebar tab item.
		/// </summary>
		/// <param name="tabName">Name of tab containing the tab item to make current.</param>
		/// <param name="itemName">Name of tab item to make current.</param>
		/// <param name="generateEvents"><c>true</c> to cause events to be fired when setting
		/// the current sidebar tab item (like when the user clicks on an item). Otherwise,
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		public void SetCurrentTabItem(string tabName, string itemName, bool generateEvents)
		{
			if (m_navPane.Items.Contains(tabName))
			{
				SideBarTab tab = m_navPane.Items[tabName] as SideBarTab;
				if (tab != null)
					tab.SetCurrentTabItem(itemName, generateEvents);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the properties for the specified item on the specified tab.
		/// </summary>
		/// <param name="tabName">Tab containing the item.</param>
		/// <param name="itemName">Item whose properties are being requested.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public SBTabItemProperties GetTabItemProperties(string tabName, string itemName)
		{
			SideBarTab tab = m_navPane.Items[tabName] as SideBarTab;
			if (tab == null)
				return null;

			return tab.GetItemProps(itemName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tells the side bar adapter to setup it's menus so they show up on the application's
		/// view menu. This method should be called after all the tabs and tab items have been
		/// created.
		/// </summary>
		/// <param name="menuAdapter">Menu adapter used by the application.</param>
		/// <param name="insertBeforeItem">Name of the menu item before which the sidebar
		/// menus will be added.</param>
		/// ------------------------------------------------------------------------------------
		public void SetupViewMenuForSideBarTabs(ITMAdapter adapter, string insertBeforeItem)
		{
			Debug.Assert(adapter != null);
			adapter.MessageMediator.AddColleague(this);

			foreach (SideBarTab tab in m_navPane.Items)
			{
				// Add the menu for the view tab.
				TMItemProperties props = tab.MenuItemProperties;
				props.Message = "SideBarViewTabMenuHandler";
				adapter.AddMenuItem(props, null, insertBeforeItem);

				// Add menus for the view tab items.
				foreach (TMItemProperties itemProps in tab.SubMenuItemProperties)
				{
					itemProps.Text = itemProps.Text.Replace(Environment.NewLine, " ");
					itemProps.Message = "SideBarViewTabItemMenuHandler";
					adapter.AddMenuItem(itemProps, tab.MenuItemProperties.Name, null);
				}

				// Add the menu item for the view tab's configure menu.
				SBTabProperties tabProps = tab.Properties;
				props = new TMItemProperties();
				props.Name = tab.Name + "Config";
				props.Text = tabProps.ConfigureMenuText;
				props.Message = tabProps.ConfigureMessage;
				props.BeginGroup = true;
				props.Tag = tabProps;
				adapter.AddMenuItem(props, tab.MenuItemProperties.Name, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called from the TMAdapter when one of the view menus needs to be updated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateSideBarViewTabMenuHandler(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;

			itemProps.Enabled = m_navPane.Items[itemProps.Name].Enabled;
			itemProps.Update = true;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called from the TMAdapter when one of the view menu items is clicked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnSideBarViewTabItemMenuHandler(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;

			SBTabItemProperties tabProps = itemProps.Tag as SBTabItemProperties;
			if (tabProps == null)
				return false;

			string tabName = tabProps.OwningTabName;
			string itemName = tabProps.Name;

			SetCurrentTabItem(tabName, itemName, true);
			if (!((ButtonItem)m_navPane.Items[tabName]).Checked)
				SetCurrentTab(tabName, true);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called from the TMAdapter when one of the view menu items needs to be updated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateSideBarViewTabItemMenuHandler(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;

			SBTabItemProperties tabProps = itemProps.Tag as SBTabItemProperties;
			if (tabProps == null)
				return false;

			SideBarTab tab = m_navPane.Items[tabProps.OwningTabName] as SideBarTab;
			if (tab == null)
				return false;

			itemProps.Checked = (tab == CurrentTab && tab.CurrentItemProps.Name == itemProps.Name);
			itemProps.Update = true;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tells the adapter to load it's settings from the specified keys.
		/// </summary>
		/// <param name="key"></param>
		/// ------------------------------------------------------------------------------------
		public void LoadSettings(Microsoft.Win32.RegistryKey key)
		{
			// Set the sidebar's width
			if (m_navPane.Parent != null)
				m_navPane.Parent.Width = (int)key.GetValue("SideBarWidth", m_navPane.Parent.Width);

			// Restore the active tab and active tab item.
			string tabName = key.GetValue("ActiveTab") as string;
			if (tabName != null)
			{
				// First set the current tab without causing events.
				SetCurrentTab(tabName, false);

				string itemName = key.GetValue("ActiveTabItem") as string;
				if (itemName != null)
					SetCurrentTabItem(tabName, itemName, true);
				else
				{
					// At this point, the registry didn't contain an active tab item, so
					// we'll set the current tab again, but this time generate events on
					// the assumption that when the application gets the event, it will
					// set a default tab item for the tab.
					SetCurrentTab(tabName, true);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tells the adapter to save it's settings to the specified keys.
		/// </summary>
		/// <param name="key"></param>
		/// ------------------------------------------------------------------------------------
		public void SaveSettings(Microsoft.Win32.RegistryKey key)
		{
			// Save the sidebar's width
			if (m_navPane.Parent != null)
				key.SetValue("SideBarWidth", m_navPane.Parent.Width);

			// Save the active tab and active tab item.
			if (CurrentTab != null)
			{
				key.SetValue("ActiveTab", CurrentTab.Name);

				SBTabItemProperties itemProps = CurrentTab.CurrentItemProps;
				if (itemProps != null)
					key.SetValue("ActiveTabItem", itemProps.Name);
			}
		}

		#endregion

		#region Misc. private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupSideBarsContextMenu()
		{
			// Do nothing if the context menu has already been created.
			if (m_contextMenu != null)
				return;

			m_dnbMngr = new DotNetBarManager();

			m_contextMenu = new ButtonItem("sbcontextmenu");

			m_contextMenu.PopupShowing += new EventHandler(HandleSideBarContextMenuShowing);
			m_contextMenu.PopupOpen +=
				new DotNetBarManager.PopupOpenEventHandler(HandleSideBarContextMenuPopup);

			ButtonItem item = new ButtonItem(kLargeIconModeItem, "Large Icons");
			item.OptionGroup = m_contextMenu.Name;
			item.Click += new EventHandler(HandleSideBarContextMenuClick);
			m_contextMenu.SubItems.Add(item);

			item = new ButtonItem(kSmallIconModeItem, "Small Icons");
			item.OptionGroup = m_contextMenu.Name;
			item.Click += new EventHandler(HandleSideBarContextMenuClick);
			m_contextMenu.SubItems.Add(item);

			item = new ButtonItem(kHideSideBar, "Hide Side Bar");
			item.BeginGroup = true;
			item.Click += new EventHandler(HandleSideBarContextMenuClick);
			m_contextMenu.SubItems.Add(item);

			SetContextMenuImages();

			m_dnbMngr.ContextMenus.Add(m_contextMenu);
			m_dnbMngr.SetContextMenuEx(m_navPane, "sbcontextmenu");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the images for the icon mode items for the sidebar's context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetContextMenuImages()
		{
			ButtonItem item = m_contextMenu.SubItems[kLargeIconModeItem] as ButtonItem;

			// If the image can be found in the sidebar tab's image list, then use that
			// image, otherwise, use the default one embedded in this assembly.
			if (TabImageList != null && item.ImageIndex >= 0 &&
				item.ImageIndex < TabImageList.Images.Count)
			{
				item.Image = TabImageList.Images[item.ImageIndex];
			}
			else
			{
				Bitmap bmp = new Bitmap(GetType(), "LargeIconMode.bmp");
				bmp.MakeTransparent(Color.Magenta);
				item.Image = bmp;
			}

			item = m_contextMenu.SubItems[kSmallIconModeItem] as ButtonItem;

			// If the image can be found in the sidebar tab's image list, then use that
			// image, otherwise, use the default one embedded in this assembly.
			if (TabImageList != null && item.ImageIndex >= 0 &&
				item.ImageIndex < TabImageList.Images.Count)
			{
				item.Image = TabImageList.Images[item.ImageIndex];
			}
			else
			{
				Bitmap bmp = new Bitmap(GetType(), "SmallIconMode.bmp");
				bmp.MakeTransparent(Color.Magenta);
				item.Image = bmp;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create and information bar and place it in the specified container control.
		/// </summary>
		/// <param name="ibContainer"></param>
		/// ------------------------------------------------------------------------------------
		private void SetupInfoBar(Control ibContainer)
		{
			if (m_infoBar == null)
			{
				m_infoBar = new PanelEx();
				m_infoBar.Dock = DockStyle.Fill;
				m_infoBar.Font = Font.FromLogFont(Win32Api.NonClientMetrics.lfCaptionFont);
				m_infoBar.Style.MarginLeft = 4;
				m_infoBar.Style.Alignment = m_navPane.TitlePanel.Style.Alignment;
				m_infoBar.Style.Border = m_navPane.TitlePanel.Style.Border;
				m_infoBar.Style.BorderColor.Color = m_navPane.TitlePanel.Style.BorderColor.Color;


				if (!m_dnbMngr.IsThemeActive)
					m_infoBar.Paint += new PaintEventHandler(PaintNonThemedPanel);
				else
				{
					m_infoBar.ColorSchemeStyle = m_navPane.TitlePanel.ColorSchemeStyle;
					m_infoBar.Style.BackColor1.ColorSchemePart = m_navPane.TitlePanel.Style.BackColor1.ColorSchemePart;
					m_infoBar.Style.BackColor2.ColorSchemePart = m_navPane.TitlePanel.Style.BackColor2.ColorSchemePart;
					m_infoBar.Style.ForeColor.ColorSchemePart = m_navPane.TitlePanel.Style.ForeColor.ColorSchemePart;
					m_infoBar.Style.GradientAngle = m_navPane.TitlePanel.Style.GradientAngle;
				}
			}

			if (ibContainer != null)
				ibContainer.Controls.Add(m_infoBar);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PositionInfoBarButtons()
		{
			m_infoBar.SuspendLayout();

			int left = m_infoBar.ClientRectangle.Width - 5;

			for (int i = m_navPane.Items.Count - 1; i >= 0; i--)
			{
				SideBarTab tab = m_navPane.Items[i] as SideBarTab;
				if (tab == null)
					continue;

				left -= (tab.InfoBarButton.Width);
				tab.InfoBarButton.Location =
					new Point(left, (m_infoBar.Height - tab.InfoBarButton.Height) / 2);
			}

			m_infoBar.ResumeLayout();
		}

		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the current tab changing.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_navPane_PanelChanging(object sender, PanelChangingEventArgs e)
		{
			if (m_supressAutoEvent)
				return;

			SideBarTab tab = e.NewPanel.ParentItem as SideBarTab;

			// Post a message to click a button in the new tab. The post will allow the panel
			// to redraw before dispatching the click.
			if (m_mediator != null && tab != null && tab.Message != null)
				m_mediator.PostMessage(tab.Message, tab.Properties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the ContextShowing event - make sure that it will popup in the right location
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="?"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleSideBarContextMenuShowing(object sender, EventArgs e)
		{
			ButtonItem contextMenu = sender as ButtonItem;
			if (contextMenu == null || contextMenu.SubItems.Count == 0)
				return;

			// figure out which screen the cursor is on and determine a rect for the
			// popup at the cursor location
			Point mousePos = Cursor.Position;
			Screen currentScreen = Screen.FromPoint(mousePos);
			Rectangle currentScreenRect = currentScreen.Bounds;
			Rectangle rect = new Rectangle(mousePos, contextMenu.PopupControl.Size);

			// Make sure the popup rect is completely contained in the desired screen
			if (rect.Right > currentScreenRect.Right)
				rect.Offset(currentScreenRect.Right - rect.Right, 0);
			if (rect.Bottom > currentScreenRect.Bottom)
				rect.Offset(0, currentScreenRect.Bottom - rect.Bottom);

			// move the popup
			contextMenu.PopupLocation = new Point(rect.X, rect.Y);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the side bar's context menu pops-up, we need to make sure the correct icon
		/// mode menu item is checked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleSideBarContextMenuPopup(object sender, PopupOpenEventArgs e)
		{
			ButtonItem contextMenu = sender as ButtonItem;
			if (contextMenu == null || contextMenu.SubItems.Count == 0)
				return;

			SideBarTab tab = m_navPane.CheckedButton as SideBarTab;
			if (tab == null)
				return;

			// Setting the first item in the context menu will automatically adjust the
			// other item appropriately since they're both in the same option group.
			if (m_contextMenu.SubItems[0].Name == kLargeIconModeItem)
				((ButtonItem)m_contextMenu.SubItems[0]).Checked = !tab.SmallIconMode;
			else if (m_contextMenu.SubItems[0].Name == kSmallIconModeItem)
				((ButtonItem)m_contextMenu.SubItems[0]).Checked = tab.SmallIconMode;

			// Make only one configure menu visible. (i.e. the one corresponding to the
			// current tab. When the proper configure menu is found, then call its
			// update handler.
			for (int i = 3; i < m_contextMenu.SubItems.Count; i++)
			{
				m_contextMenu.SubItems[i].Visible =
					(m_contextMenu.SubItems[i].Name == CurrentTab.ConfigureMenuItem.Name);

				if (m_contextMenu.SubItems[i].Visible)
					CurrentTab.HandleConfigureMenuPopup(m_contextMenu.SubItems[i] as ButtonItem);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle choices from the sidebar's context menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleSideBarContextMenuClick(object sender, EventArgs e)
		{
			ButtonItem item = sender as ButtonItem;
			if (item == null)
				return;

			SideBarTab tab = m_navPane.CheckedButton as SideBarTab;
			if (tab == null)
				return;

			if (item.Name == kLargeIconModeItem)
				tab.SmallIconMode = false;
			else if (item.Name == kSmallIconModeItem)
				tab.SmallIconMode = true;
			else if (item.Name == kHideSideBar)
				m_mediator.SendMessage("SideBar", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_navPane_Load(object sender, EventArgs e)
		{
			PositionInfoBarButtons();
			// Need to do this here since we should have all the tabs added by now.
			m_navPane.NavigationBarHeight = 400;
			m_navPane.RecalcLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the tooltip for the info. bar buttons are up-to-date.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void InfoBarButton_MouseEnter(object sender, EventArgs e)
		{
			PanelEx infoBarButton = sender as PanelEx;
			if (infoBarButton == null)
				return;

			SideBarTab tab = infoBarButton.Tag as SideBarTab;
			if (tab == null)
				return;

			m_tooltip.SetToolTip(infoBarButton, tab.InfoBarButtonToolTipText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws a panel to look a lot like the Outlook 2000 information bar for when the OS
		/// is not supporting themes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		internal void PaintNonThemedPanel(object sender, PaintEventArgs e)
		{
			PanelEx panel = sender as PanelEx;
			if (panel == null)
				return;

			Rectangle rc = panel.ClientRectangle;

			// Draw the background
			e.Graphics.FillRectangle(SystemBrushes.Control, rc);
			rc.Inflate(-3, -3);
			e.Graphics.FillRectangle(SystemBrushes.ControlDark, rc);

			// Draw the text
			StringFormat sf = (StringFormat)StringFormat.GenericDefault.Clone();
			sf.Alignment = StringAlignment.Near;
			sf.LineAlignment = StringAlignment.Center;
			sf.Trimming = StringTrimming.EllipsisCharacter;
			sf.FormatFlags = StringFormatFlags.NoWrap;
			rc.X += 2;
			e.Graphics.DrawString(panel.Text, panel.Font, new SolidBrush(Color.White), rc, sf);

			// Draw the border.
			rc = panel.ClientRectangle;
			rc.Width--;
			rc.Height--;
			e.Graphics.DrawRectangle(SystemPens.ControlDark, rc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the text in the title panel gets clipped, then use a tooltip to show the
		/// panel's full string.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void TitlePanel_MouseEnter(object sender, EventArgs e)
		{
			PanelEx titlePanel = m_navPane.TitlePanel;
			Graphics graphics = titlePanel.CreateGraphics();

			Rectangle rc = titlePanel.ClientRectangle;
			rc.Inflate(-3, -3);
			rc.X += 2;

			if (graphics.MeasureString(titlePanel.Text, titlePanel.Font).Width >= rc.Width)
				m_tooltip.SetToolTip(titlePanel, titlePanel.Text);
			else
				m_tooltip.SetToolTip(titlePanel, null);

			graphics.Dispose();
		}

		#endregion

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the message targets.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the specified mediator.
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configurationParameters">The configuration parameters.</param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			// Not Used
		}

		#endregion

		#region IDisposable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			if (m_mediator != null)
				m_mediator.RemoveColleague(this);
		}

		#endregion
	}
}