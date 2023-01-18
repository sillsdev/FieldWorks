// Copyright (C) 2008-2022 SIL International.
// Copyright (C) 2007 Star Vega.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Impls.SilSidePane;
using NUnit.Framework;

namespace LanguageExplorerTests.Impls.SilSidePane
{
	/// <summary />
	[TestFixture]
	public sealed class SidePaneTests
	{
		private SidePane _sidePane;
		private Panel _parent;
		private bool _tabClickedHappened;
		private void TabClickedHandler(Tab tabClicked)
		{
			_tabClickedHappened = true;
		}
		private bool _itemClickedHappened;
		private void ItemClickedHandler(Item itemClicked)
		{
			_itemClickedHappened = true;
		}

		/// <summary>Runs before each test</summary>
		[SetUp]
		public void SetUp()
		{
			_parent = new Panel();
			_sidePane = new SidePane();
			_parent.Controls.Add(_sidePane);
		}

		/// <summary>Runs after each test</summary>
		[TearDown]
		public void TearDown()
		{
			_sidePane.Dispose();
			_parent.Dispose();

			_parent = null;
			_sidePane = null;
		}

		private static OutlookBarButton GetUnderlyingButtonCorrespondingToTab(Tab tab)
		{
			return TestUtilities.GetPrivatePropertyOfType<OutlookBarButton>(tab, "UnderlyingWidget");
		}

		[Test]
		[TestCase(SidePaneItemAreaStyle.Buttons, typeof(ToolStrip))]
		[TestCase(SidePaneItemAreaStyle.List, typeof(ListView))]
		[TestCase(SidePaneItemAreaStyle.StripList, typeof(ToolStrip))]
		public void HasCorrectItemArea(SidePaneItemAreaStyle expectedValue, Type expectedType)
		{
			// The default for a new SidePane is SidePaneItemAreaStyle.Buttons.
			Assert.AreEqual(_sidePane.ItemAreaStyle, SidePaneItemAreaStyle.Buttons);

			_parent.Controls.Remove(_sidePane);
			_sidePane.Dispose();
			_sidePane = new SidePane
			{
				ItemAreaStyle = expectedValue
			};
			_parent.Controls.Add(_sidePane);

			Assert.AreEqual(_sidePane.ItemAreaStyle, expectedValue);

			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);

			var itemAreas = TestUtilities.GetPrivateField(_sidePane, "_itemAreas") as Dictionary<Tab, IItemArea>;
			Assert.IsNotNull(itemAreas);
			foreach (var area in itemAreas.Values)
	{
				Assert.IsInstanceOf(expectedType, area);
		}
		}

		[Test]
		public void ContainingControlTest()
		{
			var containingControl = _sidePane.Parent;
			Assert.IsNotNull(containingControl);
			Assert.AreSame(containingControl, _parent);
		}

		[Test]
		public void AddTab_null()
		{
			Assert.That(() => {	_sidePane.AddTab(null); }, Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void AddTab_basic()
		{
			var tab1 = new Tab("first tab");
			_sidePane.AddTab(tab1);

			var tab2 = new Tab("another tab");
			_sidePane.AddTab(tab2);
		}

		[Test]
		public void AddTab_ofSameIdentity()
		{
			var tab = new Tab("mytab");
			_sidePane.AddTab(tab);
			Assert.That(() => { _sidePane.AddTab(tab); }, Throws.TypeOf<ArgumentException>());
		}

		[Test]
		public void AddTab_ofSameName()
		{
			var tab1 = new Tab("mytab");
			var tab2 = new Tab("mytab");
			_sidePane.AddTab(tab1);
			Assert.That(() => { _sidePane.AddTab(tab2); }, Throws.TypeOf<ArgumentException>());
		}

		[Test]
		public void AddTab_setsUnderlyingButtonNameAndText()
		{
			var tab = new Tab("tabname")
			{
				Text = "tabtext"
			};
			_sidePane.AddTab(tab);
			using (var button = GetUnderlyingButtonCorrespondingToTab(tab))
			{
				Assert.AreEqual(tab.Name, button.Name, "Tab Name and underlying button Name should be the same.");
				Assert.AreEqual(tab.Text, button.Text, "Tab Text and underlying button Text should be the same.");
			}
		}

		[Test]
		public void AddTab_setsIconInUnderlyingButton()
		{
			var tab = new Tab("tabname")
			{
				Text = "tabtext",
				Icon = Image.FromFile("./whitepixel.bmp")
			};
			_sidePane.AddTab(tab);
			using (var button = GetUnderlyingButtonCorrespondingToTab(tab))
			{
				Assert.AreSame(tab.Icon, button.Image, "Tab Icon and underlying button Image should be the same.");
			}
		}

		[Test]
		public void AddItem_null1()
		{
			Assert.That(() => { _sidePane.AddItem(null, null); }, Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void AddItem_null2()
		{
			Assert.That(() => { _sidePane.AddItem(null, new Item("itemname")); }, Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void AddItem_null3()
		{
			Assert.That(() => { _sidePane.AddItem(new Tab("tabname"), null); }, Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void AddItem_toNonExistentTab()
		{
			Assert.That(() => { _sidePane.AddItem(new Tab("tabname"), new Item("itemname")); }, Throws.TypeOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void AddItem_basic()
		{
			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, new Item("itemname"));
		}

		[Test]
		public void AddItem_ofSameIdentity_onSameTab()
		{
			var tab = new Tab("tabname");
			var item = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item);
			Assert.That(() => { _sidePane.AddItem(tab, item); }, Throws.TypeOf<ArgumentException>());
		}

		[Test]
		public void AddItem_ofSameIdentity_onDifferentTab()
		{
			var tab1 = new Tab("tab1");
			var tab2 = new Tab("tab2");
			var item = new Item("itemname");
			_sidePane.AddTab(tab1);
			_sidePane.AddTab(tab2);
			_sidePane.AddItem(tab1, item);
			_sidePane.AddItem(tab2, item);
		}

		[Test]
		public void AddItem_ofSameName_onSameTab_forNullName()
		{
			var tab = new Tab("tab");
			var item1 = new Item("itemname");
			var item2 = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item1);
			Assert.That(() => { _sidePane.AddItem(tab, item2); }, Throws.TypeOf<ArgumentException>());
		}

		[Test]
		public void AddItem_ofSameName_onSameTab_forNonNullName()
		{
			var tab = new Tab("tab");
			var item1 = new Item("item");
			var item2 = new Item("item");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item1);
			Assert.That(() => { _sidePane.AddItem(tab, item2); }, Throws.TypeOf<ArgumentException>());
		}

		[Test]
		public void AddItem_ofSameName_onDifferentTab_forNullName()
		{
			var tab1 = new Tab("tab1");
			var tab2 = new Tab("tab2");
			var item1 = new Item("itemname");
			var item2 = new Item("itemname");
			_sidePane.AddTab(tab1);
			_sidePane.AddTab(tab2);
			_sidePane.AddItem(tab1, item1);
			_sidePane.AddItem(tab2, item2);
		}

		[Test]
		public void AddItem_ofSameName_onDifferentTab_forNonNullName()
		{
			var tab1 = new Tab("tab1");
			var tab2 = new Tab("tab2");
			var item1 = new Item("item");
			var item2 = new Item("item");
			_sidePane.AddTab(tab1);
			_sidePane.AddTab(tab2);
			_sidePane.AddItem(tab1, item1);
			_sidePane.AddItem(tab2, item2);
		}

		[Test]
		public void SelectTab_basic()
		{
			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			var successful1 = _sidePane.SelectTab(tab);
			Assert.IsTrue(successful1);
			_sidePane.SelectTab(tab, true);
			var successful2 = _sidePane.SelectTab(tab, false);
			Assert.IsTrue(successful2);
		}

		[Test]
		public void SelectTab_havingText()
		{
			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			var successful = _sidePane.SelectTab(tab);
			Assert.IsTrue(successful);
		}

		[Test]
		public void SelectTab_null()
		{
			Assert.That(() => { _sidePane.SelectTab(null); }, Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void SelectTab_thatDoesntExist()
		{
			Assert.That(() => { _sidePane.SelectTab(new Tab("tabname")); }, Throws.TypeOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SelectItem_null1()
		{
			Assert.That(() => { _sidePane.SelectItem((Tab)null, null); }, Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void SelectItem_null2()
		{
			Assert.That(() => { _sidePane.SelectItem((Tab)null, "itemname"); }, Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void SelectItem_null3()
		{
			Assert.That(() => { _sidePane.SelectItem(new Tab("tabname"), null); }, Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void SelectItem_onNonexistentTab()
		{
			Assert.That(() => { _sidePane.SelectItem(new Tab("tabname"), "itemName"); }, Throws.TypeOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SelectItem_thatDoesNotExist()
		{
			var tab = new Tab("tabname");
			const string itemName = "non-existent itemname";
			_sidePane.AddTab(tab);
			Assert.IsFalse(_sidePane.SelectItem(tab, itemName));
		}

		[Test]
		public void SelectItem_basic()
		{
			var tab = new Tab("tabname");
			var item = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item);
			Assert.IsTrue(_sidePane.SelectItem(tab, item.Name));
		}

		[Test]
		public void CurrentTab()
		{
			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			_sidePane.SelectTab(tab);
			Assert.AreSame(tab, _sidePane.CurrentTab);
		}

		[Test]
		public void CurrentTab_whenNoneSelected()
		{
			Assert.IsNull(_sidePane.CurrentTab);
		}

		[Test]
		public void CurrentItem()
		{
			var tab = new Tab("tabname");
			var item = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item);
			_sidePane.SelectItem(tab, item.Name);
			Assert.AreSame(item, _sidePane.CurrentItem);
		}

		[Test]
		public void GetTabByName()
		{
			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			Assert.AreSame(tab, _sidePane.GetTabByName(tab.Name));
		}

		[Test]
		public void GetTabByName_null()
		{
			Assert.That(() => { _sidePane.GetTabByName(null); }, Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void GetTabByName_nonexistentTab()
		{
			Assert.IsNull(_sidePane.GetTabByName("nonexistentTabName"));
		}

		[Test]
		public void ItemClickEvent_basic()
		{
			_sidePane.ItemClicked += ItemClickedHandler;
			var tab = new Tab("tabname");
			var item = new Item("itemname");
			_sidePane.AddTab(tab);
			_sidePane.AddItem(tab, item);
			Assert.IsFalse(_itemClickedHappened);
			_sidePane.SelectItem(tab, item.Name);
			Assert.IsTrue(_itemClickedHappened);
		}

		[Test]
		public void TabClickEvent_basic()
		{
			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			_sidePane.TabClicked += TabClickedHandler;
			Assert.IsFalse(_tabClickedHappened);
			_sidePane.SelectTab(tab);
			Assert.IsTrue(_tabClickedHappened);
		}

		[Test]
		public void CanDisableTab()
		{
			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			tab.Enabled = false;
			Assert.IsFalse(_sidePane.SelectTab(tab));
			Assert.AreNotSame(tab, _sidePane.CurrentTab);
		}

		[Test]
		public void TrySelectingDisabledTabThatDoesNotExist()
		{
			var tab = new Tab("tabname");
			tab.Enabled = false;
			Assert.That(() => { _sidePane.SelectTab(tab); }, Throws.TypeOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void DisablingTabDisablesUnderlyingOutlookBarButton()
		{
			var tab = new Tab("tabname");
			_sidePane.AddTab(tab);
			using (var underlyingButton = GetUnderlyingButtonCorrespondingToTab(tab))
			{
				Assert.IsTrue(underlyingButton.Enabled);
				tab.Enabled = false;
				Assert.IsFalse(underlyingButton.Enabled);
			}
		}

		/// <summary>
		/// Previously would crash when drawing non-square icons (eg DropDown2003) from a
		/// stream due to mono bug https://bugzilla.novell.com/show_bug.cgi?id=581400
		/// </summary>
		[Test]
		public void MakeSidePaneWithManyItems()
		{
			// Put sidepane on a window
			using (var window = new Form())
			{
				window.Height = 600;
				window.Width = 600;
				var container = new SplitContainer
				{
					Dock = DockStyle.Fill,
					SplitterWidth = 100
				};
				window.Controls.Add(container);
				using (var sidepane = new SidePane())
				{
					container.Panel1.Controls.Add(sidepane);
					// Add a tab and a lot of items
					var tab = new Tab("tabname");
					sidepane.AddTab(tab);
					for (int i = 0; i < 50; ++i)
					{
						sidepane.AddItem(tab, new Item("item" + i));
					}
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
	}
}