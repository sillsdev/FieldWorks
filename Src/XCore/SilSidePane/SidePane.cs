// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// SidePane is the main class for clients to use of the SilSidePane library.
	/// SidePane is intended to be placed on the side of an application, and
	/// allows buttons to be grouped by category, which are shown by clicking different
	/// tabs. It is similar to the Navigation Pane in Outlook.
	///
	/// An example of its usage is in SilSidePaneTestApp.
	/// </summary>
	public class SidePane : Panel
	{
		// SidePane holds an OutlookBar and multiple IItemAreas.
		// It allows tabs and items to be added.
		// Terminology + hierarchy of what makes up a SidePane:
		//   SidePane
		//     banner showing name of currently-selected tab
		//     item area container
		//       IItemAreas (only one of which is shown at a time, depending on what tab is active)
		//         items (conceptually, which correspond to item widgets (eg ToolStripButton))
		//     tab area
		//       tabs (conceptually, which correspond to tab widgets (OutlookBarButton))

		private Control _containingControl;
		private OutlookBar _tabArea; // Bottom area containing the tabs
		// Contains the item areas. Needed since later dynamically adding an OutlookButtonPanel
		// with DockStyle.Fill directly to the parent container doesn't properly layout. So
		// they are added to this container instead, and this container is added to _containingControl
		// in the required order.
		private Panel _itemAreaContainer;
		private Dictionary<Tab, IItemArea> _itemAreas; // Areas containing items. Areas correspond to tabs.
		private Banner _banner; // Header banner at top

		// If an item should be automatically selected when a tab is selected
		private bool _generateItemEvents;

		/// <summary>
		/// Notifies listeners when an item is clicked.
		/// </summary>
		public event ItemClickedEventHandler ItemClicked;

		/// <summary>
		/// Delegate that listeners to ItemClicked must implement.
		/// </summary>
		public delegate void ItemClickedEventHandler(Item itemClicked);

		/// <summary>
		/// Notifies listeners when a tab is clicked.
		/// </summary>
		public event TabClickedEventHandler TabClicked;

		/// <summary>
		/// Delegate that listeners to TabClicked must implement.
		/// </summary>
		public delegate void TabClickedEventHandler(Tab tabClicked);

		/// <summary>
		/// Control containing this SidePane
		/// </summary>
		public Control ContainingControl
		{
			get { return _containingControl; }
		}

		/// <summary>
		/// Style of the item area
		/// </summary>
		public SidePaneItemAreaStyle ItemAreaStyle
		{
			get;
			private set;
		}

		/// <summary>
		/// Constructor. containingControl is the control, such as a SplitContainer.Panel1, upon which
		/// the tabs and items of this SidePane will be shown.
		/// Defaults to ItemAreaStyle of Buttons.
		/// </summary>
		public SidePane(Control containingControl)
		{
			Init(containingControl);
		}

		/// <param name="itemAreaStyle">
		/// SidePaneItemAreaStyle to use for this sidepane's item area
		/// </param>
		public SidePane(Control containingControl, SidePaneItemAreaStyle itemAreaStyle)
			: this(containingControl)
		{
			ItemAreaStyle = itemAreaStyle;
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "***** Missing Dispose() call for " + GetType() + ". *******");
			if (disposing)
			{

			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Set up this SidePane for use, adding its components to containingControl.
		/// </summary>
		private void Init(Control containingControl)
		{
			_containingControl = containingControl;

			_banner = new Banner
				{
					Text = "",
					Dock = DockStyle.Top,
					//Padding = new Padding(0), // TODO not magic number
					Font = new System.Drawing.Font("Tahoma",13F, System.Drawing.FontStyle.Bold,
						System.Drawing.GraphicsUnit.Point, ((byte)(0))),
					Height = 24, // TODO not magic number
				};

			_itemAreaContainer = new Panel
				{
					Dock = DockStyle.Fill,
				};

			_itemAreas = new Dictionary<Tab, IItemArea>();

			_tabArea = new OutlookBar
				{
					Dock = DockStyle.Bottom,
					Font = new System.Drawing.Font("Tahoma", 13F, System.Drawing.FontStyle.Bold,
						System.Drawing.GraphicsUnit.World),
					Name = "outlookBar",
				};
			_tabArea.Size = _tabArea.MinimumSize;
			_tabArea.ButtonClicked += new OutlookBar.ButtonClickedEventHandler(HandleTabAreaButtonClicked);

			// Controls must be added in the right order to lay out properly
			_containingControl.Controls.Add(_itemAreaContainer);
			_containingControl.Controls.Add(_banner);
			_containingControl.Controls.Add(_tabArea);
		}

		/// <summary>
		/// Handles a click on an OutlookBarButton widget representing a tab
		/// </summary>
		private void HandleTabAreaButtonClicked(object sender, OutlookBarButton tabButton)
		{
			Tab tab = GetTabByName(tabButton.Name);
			ShowOnlyCertainItemArea(tab);
			_banner.UseMnemonic = false;
			_banner.Text = tab.Text;
			InvokeTabClicked(tab);

			if (_generateItemEvents)
			{
				// Upon changing tab, the active item is also changed (to an item in the
				// now-current item area). Tell client about this.

				var currentItem = _itemAreas[tab].CurrentItem;

				// If user clicks a tab that doesn't have a previously-selected item,
				// then select the first item in the item area.
				if (currentItem == null)
				{
					var areaItems = _itemAreas[tab].Items;
					if (areaItems.Count > 0 && areaItems[0] != null)
						SelectItem(tab, areaItems[0].Name);
				}
				else
				{
					// User clicked a tab that does have a previously-selected item. Select it.
					InvokeItemClicked(currentItem);
				}
			}
		}

		/// <summary>
		/// Handles when an item in an item area is clicked. An item area calls this method.
		/// We notify the client which is using SidePane that an Item was clicked.
		/// </summary>
		private void HandleClickFromItemArea(Item itemClicked)
		{
			InvokeItemClicked(itemClicked);
		}

		/// <summary>
		/// Show a specific Tab's item area, and hide others.
		/// </summary>
		private void ShowOnlyCertainItemArea(Tab tab)
		{
			if (tab == null)
				throw new ArgumentNullException("tab");

			foreach (var area in _itemAreas.Values)
				area.Visible = false;
			_itemAreas[tab].Visible = true;
		}

		/// <remarks>Cannot add the same tab more than once. Cannot add a tab with the same name as
		/// an existing tab.</remarks>
		public void AddTab(Tab tab)
		{
			if (tab == null)
				throw new ArgumentNullException("tab");
			if (_itemAreas.Keys.Where(existingTab => existingTab.Name == tab.Name).Count() > 0)
				throw new ArgumentException("cannot add a tab with the same name as an existing tab");

			var tabButton = new OutlookBarButton
				{
			Name = tab.Name,
			Text = tab.Text,
			Image = tab.Icon,
			Enabled = tab.Enabled,
			Tag = tab
				};
			_tabArea.Buttons.Add(tabButton);
			tab.UnderlyingWidget = tabButton;

			IItemArea itemArea;
			switch (ItemAreaStyle)
			{
				case SidePaneItemAreaStyle.StripList:
					itemArea = new StripListItemArea();
					break;

				case SidePaneItemAreaStyle.List:
					itemArea = new ListViewItemArea();
					break;

				case SidePaneItemAreaStyle.Buttons:
				default:
					itemArea = new OutlookButtonPanelItemArea
						{
							Dock = DockStyle.Fill
						};
					break;
			}
			itemArea.ItemClicked += HandleClickFromItemArea;
			_itemAreas.Add(tab, itemArea);
			_itemAreaContainer.Controls.Add(itemArea.AsControl());

			// Expand tab area to show this tab
			_tabArea.ShowAnotherButton();
		}

		/// <summary>
		/// Add item to targetTab
		/// </summary>
		public void AddItem(Tab targetTab, Item item)
		{
			if (targetTab == null)
				throw new ArgumentNullException("targetTab");
			if (item == null)
				throw new ArgumentNullException("item");
			if (!_itemAreas.ContainsKey(targetTab))
				throw new ArgumentOutOfRangeException("targetTab", targetTab, "targetTab is not a tab on this SidePane");
			if (TabContainsItem(targetTab, item))
				throw new ArgumentException("targetTab already contains item");
			if (TabContainsItemWithName(targetTab, item.Name))
				throw new ArgumentException("targetTab already contains an item of the same name");

			_itemAreas[targetTab].Add(item);
		}

		/// <summary>Select a tab and an item on that tab</summary>
		/// <returns>true upon success. false if tab is disabled (and exists in this sidepane).</returns>
		public bool SelectTab(Tab tab)
		{
			return SelectTab(tab, true);
		}

		/// <summary>Select a tab.</summary>
		/// <returns>true upon success. false if tab is disabled (and exists in this sidepane).</returns>
		public bool SelectTab(Tab tab, bool andSelectAnItemOnThatTab)
		{
			if (tab == null)
				throw new ArgumentNullException("tab");
			if (!ContainsTab(tab))
				throw new ArgumentOutOfRangeException("tab", tab, "sidepane does not contain tab");
			if (tab.Enabled == false)
				return false;

			_generateItemEvents = andSelectAnItemOnThatTab; // Optionally suppress selecting an item

			_tabArea.SetSelectionChanged(tab.UnderlyingWidget);
			InvokeTabClicked(tab);

			_generateItemEvents = true;
			return true;
		}

		/// <summary>
		/// Select item on tab, by item name
		/// </summary>
		public bool SelectItem(Tab tab, string itemName)
		{
			if (null == tab)
				throw new ArgumentNullException("tab");
			if (null == itemName)
				throw new ArgumentNullException("itemName");
			if (!ContainsTab(tab))
				throw new ArgumentOutOfRangeException("tab", tab, "sidepane does not contain tab");

			SelectTab(tab, false); // Switch to tab, but don't let it auto-select an item on that tab
			var item = _itemAreas[tab].Items.Find(someItem => someItem.Name == itemName);
			if (item == null)
			{
				//throw new ArgumentOutOfRangeException("itemName", itemName, "tab does not contain item of name itemName");
				// FWR-2895 (and another) are situations where and item was deleted
				// so we shouldn't be so dogmatic about finding the submitted itemName
				SelectTab(tab, true);
				return false; // but we can tell the caller that we weren't successful
			}

			_itemAreas[tab].SelectItem(item);
			return true;
		}

		/// <summary>
		/// Gets the currently selected tab, or null if there is no tab selected.
		/// </summary>
		public Tab CurrentTab
		{
			get
			{
				OutlookBarButton currentTabWidget = _tabArea.SelectedButton;
				if (currentTabWidget == null)
					return null;
				Tab currentTab = currentTabWidget.Tag as Tab;
				return currentTab;
			}
		}

		/// <summary>
		/// Gets the currently selected item on the current tab, or null if there is no such item
		/// </summary>
		public Item CurrentItem
		{
			get
			{
				return _itemAreas[CurrentTab].CurrentItem;
			}
		}

		/// <returns>null if not found</returns>
		public Tab GetTabByName(string tabName)
		{
			if (null == tabName)
				throw new ArgumentNullException("tabName");

			return _itemAreas.Keys.FirstOrDefault(tab => tab.Name == tabName);
		}

		/// <remarks>tab must be a tab in this sidepane</remarks>
		private bool TabContainsItem(Tab tab, Item item)
		{
			if (tab == null)
				throw new ArgumentNullException("tab");
			if (item == null)
				throw new ArgumentNullException("item");
			if (!ContainsTab(tab))
				throw new ArgumentOutOfRangeException("tab", tab, "tab is not a tab in this sidepane.");

			return _itemAreas[tab].Items.Contains(item);
		}

		/// <remarks>tab must be a tab in this sidepane</remarks>
		private bool TabContainsItemWithName(Tab tab, string itemName)
		{
			if (tab == null)
				throw new ArgumentNullException("tab");
			if (itemName == null)
				throw new ArgumentNullException("itemName");
			if (!ContainsTab(tab))
				throw new ArgumentOutOfRangeException("tab", tab, "tab is not a tab in this sidepane.");

			if (_itemAreas[tab].Items.Find(item => item.Name == itemName) != null)
				return true;
			return false;
		}

		/// <summary>
		/// Whether this sidepane contains tab.
		/// </summary>
		private bool ContainsTab(Tab tab)
		{
			return _itemAreas.ContainsKey(tab);
		}

		/// <summary>
		/// Notify clients that an item was selected.
		/// </summary>
		protected void InvokeItemClicked(Item itemClicked)
		{
			if (ItemClicked != null)
				ItemClicked.Invoke(itemClicked);
		}

		/// <summary>
		/// Notify clients that a tab was selected.
		/// </summary>
		protected void InvokeTabClicked(Tab tabClicked)
		{
			if (TabClicked != null)
				TabClicked.Invoke(tabClicked);
		}
	}
}
