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
// File: SIBAdapter-simple.cs
// Responsibility:
// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;
using XCore;
using System.Collections.Generic;
using SIL.SilSidePane;

namespace SIL.FieldWorks.Common.UIAdapters
{
	// A really basic implementation of InfoBarPanel
	public class InfoBarPanel : Panel
	{
		public InfoBarPanel()
			: base()
		{
			this.ParentChanged += new EventHandler(InfoBarPanel_ParentChanged);
		}

		private void InfoBarPanel_ParentChanged(System.Object sender, System.EventArgs e)
		{
			if (Parent == null)
				return;

			this.Width = Parent.Width;
			this.Height = Parent.Height;
			this.BackColor = Color.Gray;
			this.Paint += HandlePaint;
		}

		protected void HandlePaint(object sender, PaintEventArgs e)
		{
			StringFormat textFormat = new StringFormat();
			textFormat.LineAlignment = StringAlignment.Center;
			textFormat.Alignment = StringAlignment.Near;

			e.Graphics.DrawString(Text, this.Font,
			Brushes.White, new RectangleF(this.Bounds.X,
			this.Bounds.Y, this.Bounds.Width, this.Bounds.Height), textFormat);

		}
	}

	/// <summary>
	/// Sidebar/Information Bar Adapter originally was an ISIBInterface adapter to DotNetBar.
	/// Now it adapts to SilSidePane.
	/// </summary>
	public class SIBAdapter : ISIBInterface, IxCoreColleague, IDisposable
	{
		private const string kLargeIconModeItem = "LargeIcons";
		private const string kSmallIconModeItem = "SmallIcons";
		private const string kHideSideBar = "HideSideBar";
		private bool m_supressAutoEvent = false;
		private Mediator m_mediator;
		private Hashtable m_htInfoBarButtons = new Hashtable();
		private ImageList m_largeItemImages = null;
		private ImageList m_smallItemImages = null;
		private ImageList m_tabImageList = null;
		private System.Windows.Forms.ToolTip m_tooltip = new System.Windows.Forms.ToolTip();

		private SidePane m_sidePane = null; // side bar to hold item buttons, and their tabs

		private InfoBarPanel m_infoBar = null;

		private List<SBTabProperties> m_tabProps = new List<SBTabProperties>(); // tabs (categories)
		private List<SBTabItemProperties> m_tabItemProps = new List<SBTabItemProperties>(); // all buttons on all tabs
		private string m_currentlySelectedTabName = null;

		#region ISIBInterface Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the visible state of the sidebar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SideBarVisible
		{
			get; set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the visible state of the info. bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool InformationBarVisible
		{
			get { return m_infoBar.Parent.Visible; }
			set { m_infoBar.Parent.Visible = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text on the information bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InformationBarText
		{
			get { return m_infoBar.Text; }
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
				return m_tabProps.ToArray();
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
			get { return m_smallItemImages; }
			set
			{
				m_smallItemImages = value;
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
			get { return m_largeItemImages; }
			set
			{
				m_largeItemImages = value;
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
			get
			{
				return m_tabImageList;
			}
			set
			{
				m_tabImageList = value;
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
				return 0;
			}
			set
			{
				SetupSideBarsContextMenu();

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
				return 0;
			}
			set
			{
				SetupSideBarsContextMenu();

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
				return GetTabByName(m_currentlySelectedTabName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties of the current item on the current tab, or null if there is no
		/// current tab or if there is no selected item on the current tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SBTabItemProperties CurrentTabItemProperties
		{
			get
			{
				return null;//TODO for SilSidePane
				/* some previous adapter code was
				if (null == m_currentlySelectedTabName)
					return null;
				string tabItemName = m_selectedTabItems[m_currentlySelectedTabName] as string;
				if (tabItemName == null)
					return null;
				return GetTabItemProperties(tabItemName);
				*/
			}
		}

		#endregion

		#region ISIBInterface Methods
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
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

			// Setup context menu to allow switching between large and small icon mode.
			SetupSideBarsContextMenu();

			SetupInfoBar(ibContainer);

			m_sidePane = new SidePane(sbContainer);
			m_sidePane.ItemClicked += new SidePane.ItemClickedEventHandler(HandleSidePaneItemClick);

			sbContainer.Resize += this.HandleParentContainerResize;
			UpdateSidebarLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add category tab to sidebar
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddTab(SBTabProperties tabProps)
		{
			if (tabProps == null)
				return;

			m_tabProps.Add(tabProps);

			Tab tab = new Tab(tabProps.Name);
			tab.Text = tabProps.Text;
			if (m_tabImageList != null)
				tab.Icon = m_tabImageList.Images[tabProps.ImageIndex];
			tab.Enabled = tabProps.Enabled;
			m_sidePane.AddTab(tab);

			PositionInfoBarButtons();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add item to a category tab.
		/// Silently fail if no such tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddTabItem(int tabIndex, SBTabItemProperties itemProps)
		{
			throw new NotImplementedException(); // TODO for SilSidePane
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add item to a category tab
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddTabItem(string tabName, SBTabItemProperties itemProps)
		{
			Tab tab = m_sidePane.GetTabByName(tabName);
			Item item = new Item(itemProps.Name);
			item.Text = itemProps.Text;
			item.Icon = m_largeItemImages.Images[itemProps.ImageIndex];
			item.Tag = itemProps;
			m_sidePane.AddItem(tab, item);
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
			Debug.WriteLine("Warning: SetCurrentTab(int,) definitely not fully implemented.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current sidebar tab.
		/// Updates what sidebar buttons will be visible, and makes tab visually look selected.
		/// </summary>
		/// <param name="tabName"></param>
		/// <param name="generateEvents"><c>true</c> to cause events to be fired when setting
		/// the current sidebar tab (like when the user clicks on an item). Otherwise,
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		public void SetCurrentTab(string tabName, bool generateEvents)
		{
			m_supressAutoEvent = !generateEvents;

			Tab tab = m_sidePane.GetTabByName(tabName);
			m_sidePane.SelectTab(tab);

			m_supressAutoEvent = false;
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
			Debug.WriteLine("Warning: unimplemented SetCurrentTabItem(int...)");
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
			Tab tab = m_sidePane.GetTabByName(tabName);
			m_sidePane.SelectItem(tab, itemName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the properties for the specified item on the specified tab.
		/// </summary>
		/// <param name="tabName">Tab containing the item. IGNORED.</param>
		/// <param name="itemName">Item whose properties are being requested.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public SBTabItemProperties GetTabItemProperties(string tabName, string itemName)
		{
			return GetTabItemProperties(itemName);
		}

		/// <summary>
		/// Get an item properties object of a given name.
		/// </summary>
		public SBTabItemProperties GetTabItemProperties(string itemName)
		{
			return m_tabItemProps.Find(itemProperties => itemProperties.Name == itemName);
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
			// TODO for SilSidePane: some functionality here about setting items and tabs is irrelevant with SilSidePane, so identify and remove it.

			// Set the sidebar's width
			if (m_sidePane.ContainingControl != null)
				m_sidePane.ContainingControl.Width = (int)key.GetValue("SideBarWidth", m_sidePane.ContainingControl.Width);

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
			else
			{
				// Set to a default tab and tab item.
				// Use the first item on the first tab, rather than kScrSBTabName, kScrDraftViewSBItemName, since
				// this library shouldn't have to know what buttons the application uses.
				var tab = m_tabProps[0];
				if (null != tab)
				{
					SetCurrentTab(tab.Name, true);
					// Setting first item on tab should happen automatically.
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
			if (m_sidePane.ContainingControl != null)
				key.SetValue("SideBarWidth", m_sidePane.ContainingControl.Width);

			// Save the active tab and active tab item.
			if (CurrentTabProperties != null)
			{
				key.SetValue("ActiveTab", CurrentTabProperties.Name);

				SBTabItemProperties itemProps = CurrentTabItemProperties;
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

			SetContextMenuImages();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the images for the icon mode items for the sidebar's context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetContextMenuImages()
		{

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create and information bar and place it in the specified container control.
		/// </summary>
		/// <param name="ibContainer"></param>
		/// ------------------------------------------------------------------------------------
		private void SetupInfoBar(Control ibContainer)
		{
			// TODO-Linux: Create an Info bar and add it to passing Container
			Debug.WriteLine("Warning: SetupInfoBar()) may not be fully implemented.");

			if (m_infoBar == null)
			{
				m_infoBar = new InfoBarPanel();
				m_infoBar.Dock = DockStyle.Fill;
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
		}

		/// <summary>
		/// Handle when the container containing SplitContainer gets resized.
		/// </summary>
		private void HandleParentContainerResize(object sender, EventArgs e)
		{
			UpdateSidebarLayout();
		}

		/// <summary></summary>
		private void ProcessTabItemClick(SBTabItemProperties itemProperties)
		{
			Debug.Assert(null != itemProperties, "Argument itemProperties shouldn't be null");
			Debug.Assert(null != m_mediator, "m_mediator shouldn't be null");

			m_mediator.SendMessage(itemProperties.Message, itemProperties);
		}

		/// <summary>
		/// Returns tab object of specified name, or null if not found.
		/// TODO: The perceived need for this method leads me to believe that something else isn't
		/// designed correctly. eg the way we store tabs maybe should be in a different data structure.
		/// </summary>
		private SBTabProperties GetTabByName(string tabName)
		{
			foreach (SBTabProperties tab in m_tabProps)
				if (tab.Name == tabName)
					return tab;
			return null;
		}

		/// <summary>
		/// Call to update the sidebar layout after the size of the sidebar container has changed
		/// (eg from window resize).
		/// </summary>
		private void UpdateSidebarLayout()
		{
		}

		/// <summary></summary>
		private void HandleSidePaneItemClick(Item itemClicked)
		{
			SBTabItemProperties itemProperties = itemClicked.Tag as SBTabItemProperties;
			ProcessTabItemClick(itemProperties);
		}
		#endregion

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
			Debug.WriteLine("Warning: HandleSideBarContextMenuClick not implemented.");
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
		}

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

		/// <summary>
		/// Does not track disposed state, assume always OK to call.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return false; }
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