// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using XCore;

namespace XMLViewsTests
{
	internal sealed class TestXmlView : SIL.FieldWorks.Common.Controls.XmlView
	{
		public void InstallRootBoxForTest(IVwRootBox rootBox)
		{
			m_rootb = rootBox;
		}

		public void InstallXmlVcForTest(SIL.FieldWorks.Common.Controls.XmlVc xmlVc)
		{
			m_xmlVc = xmlVc;
		}

		public string LayoutNameForTest => m_layoutName;
	}

	internal sealed class TestXmlSeqView : SIL.FieldWorks.Common.Controls.XmlSeqView
	{
		public void InstallRootBoxForTest(IVwRootBox rootBox)
		{
			m_rootb = rootBox;
		}

		public void InstallXmlVcForTest(SIL.FieldWorks.Common.Controls.XmlVc xmlVc)
		{
			m_xmlVc = xmlVc;
		}

		public void InstallPropertyTableForTest(PropertyTable propertyTable)
		{
			m_propertyTable = propertyTable;
		}
	}

	[TestFixture]
	public class XmlViewRefreshPolicyTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private static void EnsureTestInventoriesLoaded(string databaseName)
		{
			if (Inventory.GetInventory("layouts", databaseName) != null &&
				Inventory.GetInventory("parts", databaseName) != null)
			{
				return;
			}

			var layoutKeyAttributes = new System.Collections.Generic.Dictionary<string, string[]>();
			layoutKeyAttributes["layout"] = new[] { "class", "type", "name", "choiceGuid" };
			layoutKeyAttributes["group"] = new[] { "label" };
			layoutKeyAttributes["part"] = new[] { "ref" };

			var layoutInventory = new Inventory("*.fwlayout", "/LayoutInventory/*", layoutKeyAttributes, "test", "nowhere");
			layoutInventory.LoadElements(Resources.Layouts_xml, 1);
			Inventory.SetInventory("layouts", databaseName, layoutInventory);

			var partKeyAttributes = new System.Collections.Generic.Dictionary<string, string[]>();
			partKeyAttributes["part"] = new[] { "id" };

			var partInventory = new Inventory("*Parts.xml", "/PartInventory/bin/*", partKeyAttributes, "test", "nowhere");
			partInventory.LoadElements(Resources.Parts_xml, 1);
			Inventory.SetInventory("parts", databaseName, partInventory);
		}

		private SIL.FieldWorks.Common.Controls.XmlVc CreateConfiguredXmlVc(SIL.FieldWorks.Common.RootSites.SimpleRootSite rootSite, bool editable)
		{
			EnsureTestInventoriesLoaded(Cache.ProjectId.Name);

			var xmlVc = new SIL.FieldWorks.Common.Controls.XmlVc("root", editable, rootSite, null, Cache.DomainDataByFlid);
			xmlVc.SetCache(Cache);
			xmlVc.DataAccess = Cache.DomainDataByFlid;
			return xmlVc;
		}

		[Test]
		public void XmlViewResetTables_ReconstructsRootBox()
		{
			using (var view = new TestXmlView())
			{
				var rootBox = new FakeXmlBrowseViewBase.FakeRootBox();
				view.InstallRootBoxForTest(rootBox);
				view.InstallXmlVcForTest(CreateConfiguredXmlVc(view, true));

				view.ResetTables();

				Assert.That(rootBox.ReconstructCallCount, Is.EqualTo(1));
			}
		}

		[Test]
		public void XmlViewResetTablesWithNewLayout_ReconstructsRootBoxAndStoresLayoutWithoutVc()
		{
			using (var view = new TestXmlView())
			{
				var rootBox = new FakeXmlBrowseViewBase.FakeRootBox();
				view.InstallRootBoxForTest(rootBox);

				view.ResetTables("updated-layout");

				Assert.That(rootBox.ReconstructCallCount, Is.EqualTo(1));
				Assert.That(view.LayoutNameForTest, Is.EqualTo("updated-layout"));
			}
		}

		[Test]
		public void XmlSeqViewResetTablesWithLayout_ReconstructsRootBoxWithoutVc()
		{
			using (var view = new TestXmlSeqView())
			{
				var rootBox = new FakeXmlBrowseViewBase.FakeRootBox();
				view.InstallRootBoxForTest(rootBox);

				view.ResetTables("updated-layout");

				Assert.That(rootBox.ReconstructCallCount, Is.EqualTo(1));
			}
		}

		[Test]
		public void XmlSeqViewResetRoot_ReassignsRootObjectAndReconstructs()
		{
			using (var view = new TestXmlSeqView())
			{
				var rootBox = new FakeXmlBrowseViewBase.FakeRootBox();
				var xmlVc = new SIL.FieldWorks.Common.Controls.XmlVc();
				view.InstallRootBoxForTest(rootBox);
				view.InstallXmlVcForTest(xmlVc);

				view.ResetRoot(42);

				Assert.That(rootBox.LastSetRootObjectHvo, Is.EqualTo(42));
				Assert.That(rootBox.LastSetRootObjectVc, Is.SameAs(xmlVc));
				Assert.That(rootBox.LastSetRootObjectFrag, Is.EqualTo(view.RootFrag));
				Assert.That(rootBox.ReconstructCallCount, Is.EqualTo(1));
			}
		}

		[Test]
		public void XmlSeqViewOnPropertyChanged_ShowFailingItemsChange_ReconstructsRootBox()
		{
			using (var view = new TestXmlSeqView())
			using (var propertyTable = new PropertyTable(null))
			{
				var rootBox = new FakeXmlBrowseViewBase.FakeRootBox();
				view.InstallRootBoxForTest(rootBox);
				view.InstallXmlVcForTest(new SIL.FieldWorks.Common.Controls.XmlVc("root", false, view, null, (ISilDataAccess)null));
				view.InstallPropertyTableForTest(propertyTable);
				propertyTable.SetProperty("currentContentControl", "tool", false);
				propertyTable.SetProperty("ShowFailingItems-tool", true, false);

				view.OnPropertyChanged("ShowFailingItems-tool");

				Assert.That(rootBox.ReconstructCallCount, Is.EqualTo(1));
			}
		}

		[Test]
		public void XmlSeqViewOnPropertyChanged_ShowFailingItemsUnchanged_DoesNotReconstructRootBox()
		{
			using (var view = new TestXmlSeqView())
			using (var propertyTable = new PropertyTable(null))
			{
				var rootBox = new FakeXmlBrowseViewBase.FakeRootBox();
				view.InstallRootBoxForTest(rootBox);
				view.InstallXmlVcForTest(new SIL.FieldWorks.Common.Controls.XmlVc("root", false, view, null, (ISilDataAccess)null));
				view.InstallPropertyTableForTest(propertyTable);
				propertyTable.SetProperty("currentContentControl", "tool", false);
				propertyTable.SetProperty("ShowFailingItems-tool", false, false);

				view.OnPropertyChanged("ShowFailingItems-tool");

				Assert.That(rootBox.ReconstructCallCount, Is.EqualTo(0));
			}
		}
	}
}