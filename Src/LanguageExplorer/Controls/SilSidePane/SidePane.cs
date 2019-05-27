// SilSidePane, Copyright 2008-2019 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.SilSidePane
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
		// Bottom area containing the tabs
		private OutlookBar _tabArea;
		/// <summary>
		/// Contains the item areas. Needed since later dynamically adding an OutlookButtonPanel
		/// with DockStyle.Fill directly to the parent container doesn't properly layout. So
		/// they are added to this container instead, and this container is added to _containingControl
		/// in the required order.
		/// </summary>
		private Panel _itemAreaContainer;
		private Dictionary<Tab, IItemArea> _itemAreas; // Areas containing items. Areas correspond to tabs.
		private Banner _banner; // Header banner at top
		// If an item should be automatically selected when a tab is selected
		private bool _generateItemEvents;

		/// <summary>
		/// Notifies listeners when an item is clicked.
		/// </summary>
		internal event ItemClickedEventHandler ItemClicked;

		/// <summary>
		/// Delegate that listeners to ItemClicked must implement.
		/// </summary>
		internal delegate void ItemClickedEventHandler(Item itemClicked);

		/// <summary>
		/// Notifies listeners when a tab is clicked.
		/// </summary>
		internal event TabClickedEventHandler TabClicked;

		/// <summary>
		/// Delegate that listeners to TabClicked must implement.
		/// </summary>
		internal delegate void TabClickedEventHandler(Tab tabClicked);

		/// <summary>
		/// Style of the item area
		/// </summary>
		internal SidePaneItemAreaStyle ItemAreaStyle { get; set; }

		/// <summary />
		public SidePane()
		{
			Init();
			ItemAreaStyle = SidePaneItemAreaStyle.Buttons;
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "******* Missing Dispose() call for " + GetType() + ". *******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Set up this SidePane for use, adding its components to containingControl.
		/// </summary>
		private void Init()
		{
			_banner = new Banner
			{
				Text = string.Empty,
				Dock = DockStyle.Top,
				//Padding = new Padding(0), // TODO not magic number
				Font = new System.Drawing.Font("Tahoma", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0),
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
				Font = new System.Drawing.Font("Tahoma", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.World),
				Name = "outlookBar",
			};
			_tabArea.Size = _tabArea.MinimumSize;
			_tabArea.ButtonClicked += HandleTabAreaButtonClicked;
			// Controls must be added in the right order to lay out properly
			Controls.Add(_itemAreaContainer);
			Controls.Add(_banner);
			Controls.Add(_tabArea);
			Dock = DockStyle.Fill;
		}

		/// <summary>
		/// Handles a click on an OutlookBarButton widget representing a tab
		/// </summary>
		private void HandleTabAreaButtonClicked(object sender, OutlookBarButton tabButton)
		{
			var tab = GetTabByName(tabButton.Name);
			ShowOnlyCertainItemArea(tab);
			_banner.UseMnemonic = false;
			_banner.Text = tab.Text;
			InvokeTabClicked(tab);
			if (!_generateItemEvents)
			{
				return;
			}
			// Upon changing tab, the active item is also changed (to an item in the
			// now-current item area). Tell client about this.
			var currentItem = _itemAreas[tab].CurrentItem;
			// If user clicks a tab that doesn't have a previously-selected item,
			// then select the first item in the item area.
			if (currentItem == null)
			{
				var areaItems = _itemAreas[tab].Items;
				if (areaItems.Count > 0 && areaItems[0] != null)
				{
					SelectItem(tab, areaItems[0].Name);
				}
			}
			else
			{
				// User clicked a tab that does have a previously-selected item. Select it.
				InvokeItemClicked(currentItem);
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
			Guard.AgainstNull(tab, nameof(tab));

			foreach (var area in _itemAreas.Values)
			{
				area.Visible = false;
			}
			_itemAreas[tab].Visible = true;
		}


		/// <summary>
		/// Set up area tabs, tool items, and View Area/Tool menus.
		/// </summary>
		internal void Initalize(IAreaRepository areaRepository, ToolStripMenuItem viewToolStripMenuItem, EventHandler view_Area_Tool_Clicked)
		{
			Guard.AgainstNull(areaRepository, nameof(areaRepository));

			TabStop = true;
			TabIndex = 0;
			ItemAreaStyle = SidePaneItemAreaStyle.List;
			// Add areas and tools.
			var currentAreaMenuIdx = 2;
			foreach (var area in areaRepository.AllAreasInOrder)
			{
				var localizedAreaName = StringTable.Table.LocalizeLiteralValue(area.UiName);
				var currentAreaMenu = (ToolStripMenuItem)viewToolStripMenuItem.DropDownItems[currentAreaMenuIdx++];
				currentAreaMenu.Text = localizedAreaName;
				currentAreaMenu.Image = area.Icon;
				var tab = new Tab(localizedAreaName)
				{
					Icon = area.Icon,
					Tag = area,
					Name = area.MachineName
				};
				if (area.MachineName == AreaServices.ListsAreaMachineName)
				{
					((IListArea)area).ListAreaTab = tab;
				}
				AddTab(tab);
				// Add tools for area.
				foreach (var tool in area.AllToolsInOrder)
				{
					var localizedToolName = StringTable.Table.LocalizeLiteralValue(tool.UiName);
					var item = new Item(localizedToolName)
					{
						Icon = tool.Icon,
						Tag = tool,
						Name = tool.MachineName
					};
					var currentToolMenu = new ToolStripMenuItem(localizedToolName, tool.Icon, view_Area_Tool_Clicked)
					{
						Tag = new Tuple<Tab, ITool>(tab, tool)
					};
					currentAreaMenu.DropDownItems.Add(currentToolMenu);
					AddItem(tab, item);
				}
			}
		}

		/// <remarks>Cannot add the same tab more than once. Cannot add a tab with the same name as
		/// an existing tab.</remarks>
		internal void AddTab(Tab tab)
		{
			Guard.AgainstNull(tab, nameof(tab));
			if (_itemAreas.Keys.Any(existingTab => existingTab.Name == tab.Name))
			{
				throw new ArgumentException("cannot add a tab with the same name as an existing tab");
			}
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
		internal void AddItem(Tab targetTab, Item item)
		{
			Guard.AgainstNull(targetTab, nameof(targetTab));
			Guard.AgainstNull(item, nameof(item));

			if (!_itemAreas.ContainsKey(targetTab))
			{
				throw new ArgumentOutOfRangeException(nameof(targetTab), targetTab, "targetTab is not a tab on this SidePane");
			}
			if (TabContainsItem(targetTab, item))
			{
				throw new ArgumentException("targetTab already contains item");
			}
			if (TabContainsItemWithName(targetTab, item.Name))
			{
				throw new ArgumentException("targetTab already contains an item of the same name");
			}
			_itemAreas[targetTab].Add(item);
		}

		/// <summary>
		/// Remove item from targetTab that has itemTag as its Tag.
		/// </summary>
		internal void RemoveItem(Tab targetTab, object itemTag)
		{
			Guard.AgainstNull(targetTab, nameof(targetTab));
			Guard.AgainstNull(itemTag, nameof(itemTag));

			if (!_itemAreas.ContainsKey(targetTab))
			{
				throw new ArgumentOutOfRangeException(nameof(targetTab), targetTab, "targetTab is not a tab on this SidePane");
			}
			var itemArea = _itemAreas[targetTab];
			foreach (var item in itemArea.Items)
			{
				if (ReferenceEquals(item.Tag, itemTag))
				{
					itemArea.Items.Remove(item);
					break;
				}
			}
		}

		/// <summary>
		/// Rename item in targetTab that has itemTag as its Tag.
		/// </summary>
		internal void RenameItem(Tab targetTab, object itemTag, string newText)
		{
			Guard.AgainstNull(targetTab, nameof(targetTab));
			Guard.AgainstNull(itemTag, nameof(itemTag));

			if (!_itemAreas.ContainsKey(targetTab))
			{
				throw new ArgumentOutOfRangeException(nameof(targetTab), targetTab, "targetTab is not a tab on this SidePane");
			}
			var itemArea = _itemAreas[targetTab];
			foreach (var item in itemArea.Items)
			{
				if (ReferenceEquals(item.Tag, itemTag) && item.Text != newText)
				{
					item.Text = newText;
					var widget = item.UnderlyingWidget as ListViewItem;
					if (widget != null)
					{
						widget.Text = newText;
					}
					break;
				}
			}
		}

		/// <summary>Select a tab and an item on that tab</summary>
		/// <returns>true upon success. false if tab is disabled (and exists in this sidepane).</returns>
		internal bool SelectTab(Tab tab)
		{
			return SelectTab(tab, true);
		}

		/// <summary>Select a tab.</summary>
		/// <returns>true upon success. false if tab is disabled (and exists in this sidepane).</returns>
		internal bool SelectTab(Tab tab, bool andSelectAnItemOnThatTab)
		{
			Guard.AgainstNull(tab, nameof(tab));

			if (!ContainsTab(tab))
			{
				throw new ArgumentOutOfRangeException(nameof(tab), tab, "sidepane does not contain tab");
			}
			if (tab.Enabled == false)
			{
				return false;
			}
			_generateItemEvents = andSelectAnItemOnThatTab; // Optionally suppress selecting an item
			_tabArea.SetSelectionChanged(tab.UnderlyingWidget);
			InvokeTabClicked(tab);
			_generateItemEvents = true;
			return true;
		}

		/// <summary>
		/// Select item on tab, by item name
		/// </summary>
		internal bool SelectItem(Tab tab, string itemName)
		{
			Guard.AgainstNull(tab, nameof(tab));
			Guard.AgainstNullOrEmptyString(itemName, nameof(itemName));

			if (!ContainsTab(tab))
			{
				throw new ArgumentOutOfRangeException(nameof(tab), tab, "sidepane does not contain tab");
			}
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
		internal Tab CurrentTab
		{
			get
			{
				var currentTabWidget = _tabArea.SelectedButton;
				return currentTabWidget?.Tag as Tab;
			}
		}

		/// <summary>
		/// Gets the currently selected item on the current tab, or null if there is no such item
		/// </summary>
		internal Item CurrentItem => _itemAreas[CurrentTab].CurrentItem;

		/// <returns>null if not found</returns>
		internal Tab GetTabByName(string tabName)
		{
			Guard.AgainstNullOrEmptyString(tabName, nameof(tabName));

			return _itemAreas.Keys.FirstOrDefault(tab => tab.Name == tabName);
		}

		/// <remarks>tab must be a tab in this sidepane</remarks>
		private bool TabContainsItem(Tab tab, Item item)
		{
			Guard.AgainstNull(tab, nameof(tab));
			Guard.AgainstNull(item, nameof(item));

			if (!ContainsTab(tab))
			{
				throw new ArgumentOutOfRangeException(nameof(tab), tab, "tab is not a tab in this sidepane.");
			}
			return _itemAreas[tab].Items.Contains(item);
		}

		/// <remarks>tab must be a tab in this sidepane</remarks>
		internal bool TabContainsItemWithName(Tab tab, string itemName)
		{
			Guard.AgainstNull(tab, nameof(tab));
			Guard.AgainstNull(itemName, nameof(itemName));

			if (!ContainsTab(tab))
			{
				throw new ArgumentOutOfRangeException(nameof(tab), tab, "tab is not a tab in this sidepane.");
			}
			return _itemAreas[tab].Items.Find(item => item.Name == itemName) != null;
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
		private void InvokeItemClicked(Item itemClicked)
		{
			ItemClicked?.Invoke(itemClicked);
		}

		/// <summary>
		/// Notify clients that a tab was selected.
		/// </summary>
		private void InvokeTabClicked(Tab tabClicked)
		{
			TabClicked?.Invoke(tabClicked);
		}
	}
}