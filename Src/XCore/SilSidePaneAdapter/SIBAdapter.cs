// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008-2010, SIL International. All Rights Reserved.
// <copyright from='2008' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SIBAdapter.cs
// Responsibility:
// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Linq;
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
		{
			ParentChanged += InfoBarPanel_ParentChanged;
		}

		private void InfoBarPanel_ParentChanged(Object sender, EventArgs e)
		{
			if (Parent == null)
				return;

			Width = Parent.Width;
			Height = Parent.Height;
			BackColor = Color.Gray;
			Paint += HandlePaint;
		}

		protected void HandlePaint(object sender, PaintEventArgs e)
		{
			using (StringFormat textFormat = new StringFormat())
			{
			textFormat.LineAlignment = StringAlignment.Center;
			textFormat.Alignment = StringAlignment.Near;

			e.Graphics.DrawString(Text, Font, Brushes.White,
				new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), textFormat);
		}
	}
	}

	/// <summary>
	/// Sidebar/Information Bar Adapter originally was an ISIBInterface adapter to DotNetBar.
	/// Now it adapts to SilSidePane.
	/// </summary>
	public class SIBAdapter : ISIBInterface, IxCoreColleague, IDisposable
	{
		private Mediator m_mediator;
		private ImageList m_largeItemImages;
		private ImageList m_smallItemImages;
		private ImageList m_tabImageList;

		private SidePane m_sidePane; // side bar to hold item buttons, and their tabs

		private InfoBarPanel m_infoBar;

		private List<SBTabProperties> m_tabProps = new List<SBTabProperties>(); // tabs (categories)
		private List<SBTabItemProperties> m_tabItemProps = new List<SBTabItemProperties>(); // all buttons on all tabs
		private string m_currentlySelectedTabName;
		// Dictionary from tab name to item name, gives the current selection if any.
		private Dictionary<string, string> m_selectedTabItems = new Dictionary<string, string>();

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~SIBAdapter()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		#region IDisposable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
				if (m_sidePane != null)
					m_sidePane.Dispose();
			}
			m_mediator = null;
			m_sidePane = null;
			IsDisposed = true;
		}
		#endregion

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
				m_infoBar.Text = (value ?? String.Empty).Replace(Environment.NewLine, " ").Replace('\n', ' ');
				m_infoBar.Invalidate();
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
				if (null == m_currentlySelectedTabName)
					return null;
				string tabItemName;
				if (!m_selectedTabItems.TryGetValue(m_currentlySelectedTabName, out tabItemName) || tabItemName == null)
					return null;
				return GetTabItemProperties(tabItemName);
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
			m_sidePane.ItemClicked += HandleSidePaneItemClick;
			m_sidePane.TabClicked += (HandleTabClicked);

			sbContainer.Resize += HandleParentContainerResize;
			UpdateSidebarLayout();
		}

		void HandleTabClicked(Tab tabClicked)
		{
			m_currentlySelectedTabName = tabClicked.Name;
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
			itemProps.OwningTabName = tabName;
			m_tabItemProps.Add(itemProps);
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
			Tab tab = m_sidePane.GetTabByName(tabName);
			m_sidePane.SelectTab(tab);
			m_currentlySelectedTabName = tabName;
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
			TMAdapter silAdapter = adapter as TMAdapter;
			foreach (SBTabProperties tab in m_tabProps)
			{
				ToolStripMenuItem viewTab = new ToolStripMenuItem();
				viewTab.Text = tab.Text;
				viewTab.Enabled = tab.Enabled;

				foreach (SBTabItemProperties item in m_tabItemProps)
				{

					if (item.OwningTabName == tab.Name)
					{

						ToolStripMenuItem menuItem = new ToolStripMenuItem();

						 // just one space if any version of newline is present
						menuItem.Text = item.Text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
						menuItem.Image = this.m_smallItemImages.Images[item.ImageIndex];
						menuItem.Tag = item;
						//menuItem.Image = this.m_smallItemImages[];
						viewTab.DropDown.Items.Add(menuItem);

						menuItem.Click += SideBarItemOnMenuClick;
					}
				}
				if (tab.ConfigureMenuVisible && !String.IsNullOrEmpty(tab.ConfigureMessage) && !String.IsNullOrEmpty(tab.ConfigureMenuText))
				{
					var menuItem = new ToolStripMenuItem();
					menuItem.Text = tab.ConfigureMenuText;
					menuItem.Tag = tab;
					menuItem.Click += new EventHandler(ConfigureItem_Click);
					viewTab.DropDown.Items.Add(new ToolStripSeparator());
					viewTab.DropDown.Items.Add(menuItem);
				}
				viewTab.DropDownOpened += new EventHandler(TabMenu_DropDownOpened);
				silAdapter.InsertMenuItem(viewTab, insertBeforeItem);
			}
			adapter.MessageMediator.AddColleague(this);
		}

		void TabMenu_DropDownOpened(object sender, EventArgs e)
		{
			var menu = sender as ToolStripMenuItem;
			if (menu == null)
				return;
			var configItem = menu.DropDown.Items[menu.DropDown.Items.Count -1] as ToolStripMenuItem;
			if (configItem == null)
				return;
			var tabProps = configItem.Tag as SBTabProperties;
			if (tabProps == null)
				return;
			if (!m_mediator.SendMessage("Update" + tabProps.ConfigureMessage, tabProps))
				configItem.Enabled = m_mediator.HasReceiver(tabProps.ConfigureMessage);
			else
			{
				configItem.Enabled = tabProps.ConfigureMenuEnabled;
				configItem.Visible = tabProps.ConfigureMenuVisible;
				// Also control visibility of previous separator.
				menu.DropDown.Items[menu.DropDown.Items.Count - 2].Visible = tabProps.ConfigureMenuVisible;
			}
		}

		void ConfigureItem_Click(object sender, EventArgs e)
		{
			var configItem = sender as ToolStripMenuItem;
			if (configItem == null)
				return;
			var tabProps = configItem.Tag as SBTabProperties;
			if (tabProps == null)
				return;
			m_mediator.SendMessage(tabProps.ConfigureMessage, tabProps);
		}

		void SideBarItemOnMenuClick(object sender, EventArgs e)
		{
			var m = sender as ToolStripMenuItem;
			var props = m.Tag as SBTabItemProperties;
			SetCurrentTabItem(props.OwningTabName, props.Name, true);
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
			m_selectedTabItems[itemProperties.OwningTabName] = itemProperties.Name;
			m_mediator.SendMessage(itemProperties.Message, itemProperties);
		}

		/// <summary>
		/// Returns tab object of specified name, or null if not found.
		/// TODO: The perceived need for this method leads me to believe that something else isn't
		/// designed correctly. eg the way we store tabs maybe should be in a different data structure.
		/// </summary>
		private SBTabProperties GetTabByName(string tabName)
		{
			return m_tabProps.FirstOrDefault(tab => tab.Name == tabName);
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

		/// <summary>
		/// Message handling priority.
		/// </summary>
		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
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
	}
}
