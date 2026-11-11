// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Tests that InterlinMaster's own InterlinearTab sets move the tab control themselves.
	/// </summary>
	[TestFixture]
	public class InterlinMasterTabTests : MemoryOnlyBackendProviderTestBase
	{
		private IStText m_emptyText;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private TestInterlinMaster m_master;

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_emptyText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				Cache.ServiceLocator.GetInstance<ITextFactory>().Create().ContentsOA = m_emptyText;
				m_emptyText.AddNewTextPara(null).Contents =
					TsStringUtils.MakeString(string.Empty, Cache.DefaultVernWs);
			});
		}

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_mediator = new Mediator();
			m_mediator.AddColleague(new RawTextPaneTests.ListeningColleague());
			m_propertyTable = new PropertyTable(m_mediator);
			m_propertyTable.SetProperty("cache", Cache, false);
			m_master = new TestInterlinMaster(m_mediator, m_propertyTable, m_emptyText);
		}

		[TearDown]
		public override void TestTearDown()
		{
			var clerk = m_master.Clerk;
			m_master.Dispose();
			clerk.Dispose();
			m_propertyTable.Dispose();
			m_mediator.Dispose();
			base.TestTearDown();
		}

		[Test]
		public void TabClick_StoresTheTabWithoutBroadcast()
		{
			m_master.StartOnTab(InterlinMaster.TabPageSelection.Info);

			// SUT
			m_master.ClickTab(InterlinMaster.TabPageSelection.RawText);

			Assert.That(m_master.InterlinearTab, Is.EqualTo(InterlinMaster.TabPageSelection.RawText));
			Assert.That(m_master.SelectedTabIndex, Is.EqualTo((int)InterlinMaster.TabPageSelection.RawText));
			Assert.That(m_mediator.JobItems, Is.Zero, "InterlinearTab must change without a broadcast");
		}

		[TestCase("")]
		[TestCase("NoSuchTab")]
		public void Getter_UnparsableStoredValue_StoresRawTextWithoutBroadcast(string stored)
		{
			m_propertyTable.SetProperty("InterlinearTab", stored, false);

			// SUT
			var tab = m_master.InterlinearTab;

			Assert.That(tab, Is.EqualTo(InterlinMaster.TabPageSelection.RawText));
			Assert.That(m_propertyTable.GetStringProperty("InterlinearTab", null), Is.EqualTo("RawText"));
			Assert.That(m_mediator.JobItems, Is.Zero, "InterlinearTab must change without a broadcast");
		}

		[Test]
		public void ShowRecord_EmptyTextOnAnalysisTab_MovesToRawTextWithoutBroadcast()
		{
			m_master.StartOnTab(InterlinMaster.TabPageSelection.Gloss);

			// SUT
			m_master.TestShowRecord();

			Assert.That(m_master.InterlinearTab, Is.EqualTo(InterlinMaster.TabPageSelection.RawText));
			Assert.That(m_master.SelectedTabIndex, Is.EqualTo((int)InterlinMaster.TabPageSelection.RawText),
				"ShowRecord must move the tab control itself");
			Assert.That(m_mediator.JobItems, Is.Zero, "InterlinearTab must change without a broadcast");
		}

		/// <summary>
		/// An InterlinMaster on a test clerk whose current text is <paramref name="stText"/>,
		/// with the configuration the Baseline pane's Init reads.
		/// </summary>
		private sealed class TestInterlinMaster : InterlinMaster
		{
			private readonly IStText m_stText;

			internal TestInterlinMaster(Mediator mediator, PropertyTable propertyTable, IStText stText)
			{
				m_mediator = mediator;
				m_propertyTable = propertyTable;
				m_stText = stText;
				// The Baseline pane's Init looks the clerk id up in the document's <clerks> list.
				var configuration = new XmlDocument();
				configuration.LoadXml("<root><clerks><clerk id=\"interlinearTexts\"/></clerks>"
					+ "<parameters clerk=\"interlinearTexts\"/></root>");
				m_configurationParameters = configuration.SelectSingleNode("root/parameters");
				Clerk = new TestClerk(mediator, propertyTable, stText);
				// The clerk has no record list, so a tab change must not try to save.
				Clerk.SuppressSaveOnChangeRecord = true;
			}

			protected internal override IStText RootStText => m_stText;

			internal int SelectedTabIndex => m_tabCtrl.SelectedIndex;

			/// <summary>
			/// Puts the view on <paramref name="tab"/> as a persisted setting would, before the
			/// tab control has a handle, so no tab event fires; then creates the handles.
			/// </summary>
			internal void StartOnTab(TabPageSelection tab)
			{
				m_propertyTable.SetProperty("InterlinearTab", tab.ToString(), false);
				m_tabCtrl.SelectedIndex = (int)tab;
				CreateControl();
			}

			/// <summary>
			/// Changes the tab control's selection as a click does, raising its tab events.
			/// </summary>
			internal void ClickTab(TabPageSelection tab)
			{
				m_tabCtrl.SelectedIndex = (int)tab;
			}

			internal void TestShowRecord()
			{
				ShowRecord();
			}
		}

		private sealed class TestClerk : InterlinearTextsRecordClerk
		{
			private readonly IStText m_stText;

			internal TestClerk(Mediator mediator, PropertyTable propertyTable, IStText stText)
			{
				m_mediator = mediator;
				m_propertyTable = propertyTable;
				m_stText = stText;
			}

			public override int CurrentObjectHvo => m_stText.Hvo;

			/// <summary>The base releases a record list this clerk never had.</summary>
			protected override void Dispose(bool disposing)
			{
				if (m_list != null)
					base.Dispose(disposing);
			}
		}
	}
}
