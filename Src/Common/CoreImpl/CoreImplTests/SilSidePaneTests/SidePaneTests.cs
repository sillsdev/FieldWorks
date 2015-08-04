// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;
using SIL.CoreImpl.SilSidePane;

namespace SIL.CoreImpl.SilSidePaneTests
{
	/// <summary></summary>
	[TestFixture]
	public class SidePaneTests_Buttons : SidePaneTests
	{
		protected override SidePaneItemAreaStyle ItemAreaStyle
		{
			get { return SidePaneItemAreaStyle.Buttons; }
		}

		#region ButtonItemArea
		[Test]
		public void IsButtonItemArea()
		{
			Assert.AreEqual(_sidePane.ItemAreaStyle, SidePaneItemAreaStyle.Buttons);

			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);

			var itemAreas = TestUtilities.GetPrivateField(_sidePane, "_itemAreas") as Dictionary<Tab, IItemArea>;
			Assert.IsNotNull(itemAreas);
			foreach (var area in itemAreas.Values)
				Assert.IsInstanceOf<ToolStrip>(area);
		}
		#endregion ButtonItemArea
	}

	/// <summary></summary>
	[TestFixture]
	public class SidePaneTests_List : SidePaneTests
	{
		protected override SidePaneItemAreaStyle ItemAreaStyle
		{
			get { return SidePaneItemAreaStyle.List; }
		}

		#region ListItemArea
		[Test]
		public void IsListItemArea()
		{
			Assert.AreEqual(_sidePane.ItemAreaStyle, SidePaneItemAreaStyle.List);

			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);

			var itemAreas = TestUtilities.GetPrivateField(_sidePane, "_itemAreas") as Dictionary<Tab, IItemArea>;
			Assert.IsNotNull(itemAreas);
			foreach (var area in itemAreas.Values)
				Assert.IsInstanceOf<ListView>(area);
		}
		#endregion ListItemArea
	}

	/// <summary></summary>
	[TestFixture]
	public class SidePaneTests_StripList : SidePaneTests
	{
		protected override SidePaneItemAreaStyle ItemAreaStyle
		{
			get { return SidePaneItemAreaStyle.StripList; }
		}

		#region StripListItemArea
		[Test]
		public void IsStripListItemArea()
		{
			Assert.AreEqual(_sidePane.ItemAreaStyle, SidePaneItemAreaStyle.StripList);

			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);

			var itemAreas = TestUtilities.GetPrivateField(_sidePane, "_itemAreas") as Dictionary<Tab, IItemArea>;
			Assert.IsNotNull(itemAreas);
			foreach (var area in itemAreas.Values)
				Assert.IsInstanceOf<ToolStrip>(area);
		}
		#endregion StripListItemArea
	}

	/// <summary>
	/// For tests that are item area style independent, or should be run when
	/// the single-argument SidePane constructor is used.
	/// </summary>
	[TestFixture]
	public class SidePaneTests_UnspecifiedItemAreaStyle
	{
		private SidePane _sidePane;
		private Panel _parent;

		#region SetUpDown
		/// <summary>Runs before each test</summary>
		[SetUp]
		public void SetUp()
		{
			_parent = new Panel();
			_sidePane = new SidePane(_parent);
		}

		/// <summary>Runs after each test</summary>
		[TearDown]
		public void TearDown()
		{
			_sidePane.Dispose();
			_parent.Dispose();
		}
		#endregion

		[Test]
		public void IsButtonItemAreaByDefault()
		{
			Assert.AreEqual(_sidePane.ItemAreaStyle, SidePaneItemAreaStyle.Buttons);

			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);

			var itemAreas = TestUtilities.GetPrivateField(_sidePane, "_itemAreas") as Dictionary<Tab, IItemArea>;
			Assert.IsNotNull(itemAreas);
			foreach (var area in itemAreas.Values)
				Assert.IsInstanceOf<ToolStrip>(area);
		}
	}

	/// <summary>
	/// To be subclassed by classes that implement the ItemAreaStyle property,
	/// so that the unit tests run for each style of item area.
	/// </summary>
	public abstract class SidePaneTests
	{
		protected SidePane _sidePane;
		protected Panel _parent;
		protected abstract SidePaneItemAreaStyle ItemAreaStyle { get; }

		#region SetUpDown
		/// <summary>Runs before each test</summary>
		[SetUp]
		public void SetUp()
		{
			_parent = new Panel();
			_sidePane = new SidePane(_parent, ItemAreaStyle);
		}

		/// <summary>Runs after each test</summary>
		[TearDown]
		public void TearDown()
		{
			_sidePane.Dispose();
			_parent.Dispose();
		}
		#endregion

		#region ContainingControl
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "containingControl is a reference")]
		public void ContainingControlTest()
		{
			Control containingControl = _sidePane.ContainingControl;
			Assert.IsNotNull(containingControl);
			Assert.AreSame(containingControl, _parent);
		}
		#endregion ContainingControl

		#region AddTab
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddTab_null()
		{
			_sidePane.AddTab(null);
		}

		[Test]
		public void AddTab_basic()
		{
			Tab tab1 = new Tab("first tab");
			_sidePane.AddTab(tab1);

			Tab tab2 = new Tab("another tab");
			_sidePane.AddTab(tab2);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddTab_ofSameIdentity()
		{
			Tab tab = new Tab("mytab");
			_sidePane.AddTab(tab);
			_sidePane.AddTab(tab);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddTab_ofSameName()
		{
			Tab tab1 = new Tab("mytab");
			Tab tab2 = new Tab("mytab");
			_sidePane.AddTab(tab1);
			_sidePane.AddTab(tab2);
		}

		[Test]
		public void AddTab_setsUnderlyingButtonNameAndText()
		{
			Tab tab = new Tab("tabname");
			tab.Text = "tabtext";
			_sidePane.AddTab(tab);
			using (var button = TestUtilities.GetUnderlyingButtonCorrespondingToTab(tab))
			{
				Assert.AreEqual(tab.Name, button.Name, "Tab Name and underlying button Name should be the same.");
				Assert.AreEqual(tab.Text, button.Text, "Tab Text and underlying button Text should be the same.");
			}
		}

		[Test]
		public void AddTab_setsIconInUnderlyingButton()
		{
			Tab tab = new Tab("tabname");
			tab.Text = "tabtext";
			tab.Icon = Image.FromFile("./SilSidePaneTests/whitepixel.bmp");
			_sidePane.AddTab(tab);
			using (var button = TestUtilities.GetUnderlyingButtonCorrespondingToTab(tab))
			{
				Assert.AreSame(tab.Icon, button.Image, "Tab Icon and underlying button Image should be the same.");
			}
		}
		#endregion AddTab

		#region AddItem
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddItem_null1()
		{
			_sidePane.AddItem(null, null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddItem_null2()
		{
			Item item = new Item("itemname");
			_sidePane.AddItem(null, item);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddItem_null3()
		{
			Tab tab = new Tab("tabname");
			_sidePane.AddItem(tab, null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void AddItem_toNonExistentTab()
		{
			Tab tab = new Tab("tabname");
			Item item = new Item("itemname");
			_sidePane.AddItem(tab, item);
		}

		[Test]
		public void AddItem_basic()
		{
			Tab tab = new Tab("tabname");
			Item item = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddItem_ofSameIdentity_onSameTab()
		{
			Tab tab = new Tab("tabname");
			Item item = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item);
			_sidePane.AddItem(tab, item);
		}

		[Test]
		public void AddItem_ofSameIdentity_onDifferentTab()
		{
			Tab tab1 = new Tab("tab1");
			Tab tab2 = new Tab("tab2");
			Item item = new Item("itemname");
			_sidePane.AddTab(tab1);
			_sidePane.AddTab(tab2);
			_sidePane.AddItem(tab1, item);
			_sidePane.AddItem(tab2, item);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddItem_ofSameName_onSameTab_forNullName()
		{
			Tab tab = new Tab("tab");
			Item item1 = new Item("itemname");
			Item item2 = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item1);
			_sidePane.AddItem(tab, item2);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddItem_ofSameName_onSameTab_forNonNullName()
		{
			Tab tab = new Tab("tab");
			Item item1 = new Item("item");
			Item item2 = new Item("item");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item1);
			_sidePane.AddItem(tab, item2);
		}

		[Test]
		public void AddItem_ofSameName_onDifferentTab_forNullName()
		{
			Tab tab1 = new Tab("tab1");
			Tab tab2 = new Tab("tab2");
			Item item1 = new Item("itemname");
			Item item2 = new Item("itemname");
			_sidePane.AddTab(tab1);
			_sidePane.AddTab(tab2);
			_sidePane.AddItem(tab1, item1);
			_sidePane.AddItem(tab2, item2);
		}

		[Test]
		public void AddItem_ofSameName_onDifferentTab_forNonNullName()
		{
			Tab tab1 = new Tab("tab1");
			Tab tab2 = new Tab("tab2");
			Item item1 = new Item("item");
			Item item2 = new Item("item");
			_sidePane.AddTab(tab1);
			_sidePane.AddTab(tab2);
			_sidePane.AddItem(tab1, item1);
			_sidePane.AddItem(tab2, item2);
		}
		#endregion AddItem

		#region SelectTab
		[Test]
		public void SelectTab_basic()
		{
			Tab tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			bool successful1 = _sidePane.SelectTab(tab);
			Assert.IsTrue(successful1);
			_sidePane.SelectTab(tab, true);
			bool successful2 = _sidePane.SelectTab(tab, false);
			Assert.IsTrue(successful2);
		}

		[Test]
		public void SelectTab_havingText()
		{
			Tab tab = new Tab("tabname");
			tab.Text = "tabtext";
			_sidePane.AddTab(tab);
			bool successful = _sidePane.SelectTab(tab);
			Assert.IsTrue(successful);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SelectTab_null()
		{
			_sidePane.SelectTab(null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void SelectTab_thatDoesntExist()
		{
			Tab tab = new Tab("tabname");
			_sidePane.SelectTab(tab);
		}
		#endregion SelectTab

		#region SelectItem
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SelectItem_null1()
		{
			_sidePane.SelectItem(null, null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SelectItem_null2()
		{
			_sidePane.SelectItem(null, "itemname");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SelectItem_null3()
		{
			Tab tab = new Tab("tabname");
			_sidePane.SelectItem(tab, null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void SelectItem_onNonexistentTab()
		{
			Tab tab = new Tab("tabname");
			string itemName = "itemName";
			_sidePane.SelectItem(tab, itemName);
		}

		[Test]
		public void SelectItem_thatDoesNotExist()
		{
			Tab tab = new Tab("tabname");
			string itemName = "non-existent itemname";
			_sidePane.AddTab(tab);
			var result = _sidePane.SelectItem(tab, itemName);
			Assert.IsFalse(result);
		}

		[Test]
		public void SelectItem_basic()
		{
			Tab tab = new Tab("tabname");
			Item item = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item);
			var result = _sidePane.SelectItem(tab, item.Name);
			Assert.IsTrue(result);
		}
		#endregion SelectItem

		#region CurrentTab
		[Test]
		public void CurrentTab()
		{
			Tab tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			_sidePane.SelectTab(tab);
			Tab result = _sidePane.CurrentTab;
			Assert.AreSame(tab, result);
		}

		[Test]
		public void CurrentTab_whenNoneSelected()
		{
			Tab currentTab = _sidePane.CurrentTab;
			Assert.IsNull(currentTab);
		}
		#endregion

		#region CurrentItem
		[Test]
		public void CurrentItem()
		{
			Tab tab = new Tab("tabname");
			Item item = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item);
			_sidePane.SelectItem(tab, item.Name);
			Item currentItem = _sidePane.CurrentItem;
			Assert.AreSame(item, currentItem);
		}
		#endregion

		#region GetTabByName
		[Test]
		public void GetTabByName()
		{
			Tab tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			Tab result = _sidePane.GetTabByName(tab.Name);
			Assert.AreSame(tab, result);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetTabByName_null()
		{
			_sidePane.GetTabByName(null);
		}

		[Test]
		public void GetTabByName_nonexistentTab()
		{
			Tab tab = _sidePane.GetTabByName("nonexistentTabName");
			Assert.IsNull(tab);
		}
		#endregion GetTabByName

		#region ItemClickEvent
		private bool _itemClickedHappened = false;
		private void ItemClickedHandler(Item itemClicked)
		{
			_itemClickedHappened = true;
		}

		[Test]
		public void ItemClickEvent_basic()
		{
			_sidePane.ItemClicked += ItemClickedHandler;
			Tab tab = new Tab("tabname");
			Item item = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item);
			Assert.IsFalse(_itemClickedHappened);
			_sidePane.SelectItem(tab, item.Name);
			Assert.IsTrue(_itemClickedHappened);
		}
		#endregion ItemClickEvent

		#region TabClickEvent
		private bool _tabClickedHappened = false;
		private void TabClickedHandler(Tab tabClicked)
		{
			_tabClickedHappened = true;
		}

		[Test]
		public void TabClickEvent_basic()
		{
			Tab tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			_sidePane.TabClicked += TabClickedHandler;
			Assert.IsFalse(_tabClickedHappened);
			_sidePane.SelectTab(tab);
			Assert.IsTrue(_tabClickedHappened);
		}
		#endregion TabClickEvent

		#region DisableTab
		[Test]
		public void CanDisableTab()
		{
			Tab tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			tab.Enabled = false;
			bool success = _sidePane.SelectTab(tab);
			Assert.IsFalse(success);
			Tab currentTab = _sidePane.CurrentTab;
			Assert.AreNotSame(tab, currentTab);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TrySelectingDisabledTabThatDoesNotExist()
		{
			Tab tab = new Tab("tabname");
			tab.Enabled = false;
			_sidePane.SelectTab(tab);
		}

		[Test]
		public void DisablingTabDisablesUnderlyingOutlookBarButton()
		{
			Tab tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			using (var underlyingButton = TestUtilities.GetUnderlyingButtonCorrespondingToTab(tab))
			{
				Assert.IsTrue(underlyingButton.Enabled);
				tab.Enabled = false;
				Assert.IsFalse(underlyingButton.Enabled);
			}
		}
		#endregion

		#region SidePaneWithManyItems
		/// <summary>
		/// Previously would crash when drawing non-square icons (eg DropDown2003) from a
		/// stream due to mono bug https://bugzilla.novell.com/show_bug.cgi?id=581400
		/// </summary>
		[Test]
		public void MakeSidePaneWithManyItems()
		{
			// Put sidepane on a window
			using (Form window = new Form())
			{
				window.Height = 600;
				window.Width = 600;
				SplitContainer container = new SplitContainer();
				container.Dock = DockStyle.Fill;
				container.SplitterWidth = 100;
				window.Controls.Add(container);
				using (SidePane sidepane = new SidePane(container.Panel1, ItemAreaStyle))
				{
					// Add a tab and a lot of items
					Tab tab = new Tab("tabname");
					sidepane.AddTab(tab);
					for (int i = 0; i < 50; ++i)
						sidepane.AddItem(tab, new Item("item" + i.ToString()));

					try
					{
						// Display the window and its contents
						window.Show();
						Application.DoEvents();
						Assert.IsTrue(window.Visible);
					}
					finally
					{
						window.Hide();
						Application.DoEvents();
					}
				}
			}
		}
		#endregion SidePaneWithManyItems
	}
}
