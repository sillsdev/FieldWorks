// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Tests that a clerk's filter change adjusts the Filters menu state itself, for the
	/// clerk's own property and for its filter provider.
	/// </summary>
	[TestFixture]
	public class RecordClerkFilterSelectionTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private const string FilterProperty = "currentFilterForRecordClerk_entries";

		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private TestClerk m_clerk;

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_mediator = new Mediator();
			m_mediator.AddColleague(new ListeningColleague());
			m_propertyTable = new PropertyTable(m_mediator);
			m_propertyTable.SetProperty("cache", Cache, false);
			m_propertyTable.SetPropertyDispose("cache", false);
			m_clerk = new TestClerk(m_mediator, m_propertyTable, Cache);
		}

		[TearDown]
		public override void TestTearDown()
		{
			m_clerk.Dispose();
			m_propertyTable.Dispose();
			m_mediator.Dispose();
			base.TestTearDown();
		}

		[Test]
		public void OnChangeFilter_ColumnFilter_UnchecksTheMenuWithoutQueueing()
		{
			// SUT
			m_clerk.OnChangeFilter(new FilterChangeEventArgs(new AcceptAllFilter(), null));

			Assert.That(m_propertyTable.GetStringProperty(FilterProperty, null, PropertyTable.SettingsGroup.LocalSettings),
				Is.EqualTo(new UncheckAll().Name), "a filter the menu does not offer unchecks every menu item");
			Assert.That(m_mediator.JobItems, Is.Zero, "the menu state must change without a queued message");
		}

		[Test]
		public void OnChangeFilter_NoFilter_ChecksNoFilterWithoutQueueing()
		{
			m_propertyTable.SetProperty(FilterProperty, "Test set", PropertyTable.SettingsGroup.LocalSettings, false);

			// SUT
			m_clerk.OnChangeFilter(new FilterChangeEventArgs(new NoFilters(), null));

			Assert.That(m_propertyTable.GetStringProperty(FilterProperty, null, PropertyTable.SettingsGroup.LocalSettings),
				Is.EqualTo(new NoFilters().Name), "clearing the filter checks No Filter");
			Assert.That(m_mediator.JobItems, Is.Zero, "the menu state must change without a queued message");
		}

		[Test]
		public void OnChangeFilter_WordSetFilter_AddsTheProvidersClearItemWithoutQueueing()
		{
			var wordSet = Cache.ServiceLocator.GetInstance<IWfiWordSetFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.TestSetsOC.Add(wordSet);
			var provider = new WfiRecordFilterListProvider();
			provider.Init(m_mediator, m_propertyTable, null);
			var wordSetFilter = (RecordFilter)provider.Filters[0];
			m_clerk.UseProvider(provider);
			m_clerk.ChooseMenuFilter(wordSetFilter);
			m_propertyTable.SetProperty(FilterProperty, wordSetFilter.id, PropertyTable.SettingsGroup.LocalSettings, false);

			// SUT
			m_clerk.OnChangeFilter(new FilterChangeEventArgs(wordSetFilter, null));

			Assert.That(provider.Filters[0], Is.InstanceOf<WordSetNullFilter>(),
				"the provider offers its clear item as soon as its filter is applied");
			Assert.That(m_propertyTable.GetStringProperty(FilterProperty, null, PropertyTable.SettingsGroup.LocalSettings),
				Is.EqualTo(wordSetFilter.id), "the chosen menu item stays checked");
			Assert.That(m_mediator.JobItems, Is.Zero, "the menu state must change without a queued message");
		}

		[Test]
		public void OnChangeFilter_OnAnotherClerk_LeavesTheProvidersClearItemAlone()
		{
			var wordSet = Cache.ServiceLocator.GetInstance<IWfiWordSetFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.TestSetsOC.Add(wordSet);
			var provider = new WfiRecordFilterListProvider();
			provider.Init(m_mediator, m_propertyTable, null);
			var wordSetFilter = (RecordFilter)provider.Filters[0];
			m_clerk.UseProvider(provider);
			m_clerk.ChooseMenuFilter(wordSetFilter);
			m_clerk.OnChangeFilter(new FilterChangeEventArgs(wordSetFilter, null));
			Assume.That(provider.Filters[0], Is.InstanceOf<WordSetNullFilter>());

			// SUT
			using (var otherClerk = new TestClerk(m_mediator, m_propertyTable, Cache))
				otherClerk.OnChangeFilter(new FilterChangeEventArgs(new AcceptAllFilter(), null));

			Assert.That(provider.Filters[0], Is.InstanceOf<WordSetNullFilter>(),
				"only the provider's own clerk may take its clear item away");
			Assert.That(m_mediator.JobItems, Is.Zero, "the menu state must change without a queued message");
		}

		[Test]
		public void Init_PersistedWordSetFilter_ChecksItAndOffersTheClearItem()
		{
			var wordSet = Cache.ServiceLocator.GetInstance<IWfiWordSetFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.TestSetsOC.Add(wordSet);
			wordSet.Name.SetAnalysisDefaultWritingSystem("Test set");
			// A set with no cases persists an empty word list, which does not restore.
			wordSet.CasesRC.Add(Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create());
			var persisted = DynamicLoader.PersistObject(new WordSetFilter(wordSet), "filter");
			m_propertyTable.SetProperty("LexDb.Entries_filter", persisted, PropertyTable.SettingsGroup.LocalSettings, false);
			var configuration = new XmlDocument();
			configuration.LoadXml(ClerkWithProviderXml);
			var parameters = configuration.SelectSingleNode("//parameters");

			// SUT
			using (var clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, parameters, false, true))
			{
				var display = new UIListDisplayProperties(new XCore.List(null));
				clerk.OnDisplayFiltersList(null, ref display);

				Assert.That(display.List.Cast<ListItem>().Select(item => item.label),
					Is.EqualTo(new[] { new NoFilters().Name, new WordSetNullFilter().Name, wordSet.Name.AnalysisDefaultWritingSystem.Text }),
					"the menu offers the clear item as soon as the restored filter is a word set");
				Assert.That(m_propertyTable.GetStringProperty(FilterProperty, null, PropertyTable.SettingsGroup.LocalSettings),
					Is.EqualTo(wordSet.Hvo.ToString()), "the restored word set is the checked menu item");
				Assert.That(m_mediator.JobItems, Is.Zero, "the menu state must change without a queued message");
			}
		}

		/// <summary>The entries clerk with the word-set filter provider, as the Words area
		/// configures it.</summary>
		private const string ClerkWithProviderXml = @"<root>
			<clerks>
				<clerk id='entries'>
					<recordList owner='LexDb' property='Entries'/>
					<recordFilterListProvider assemblyPath='Filters.dll' class='SIL.FieldWorks.Filters.WfiRecordFilterListProvider'/>
				</clerk>
			</clerks>
			<tools><tool><control><parameters clerk='entries'/></control></tool></tools>
		</root>";

		/// <summary>
		/// A colleague registered before the change, so a queued message would have had a
		/// receiver; the job count must still be zero with one present.
		/// </summary>
		private sealed class ListeningColleague : IxCoreColleague
		{
			public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
			public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
			public bool ShouldNotCall => false;
			public int Priority => (int)ColleaguePriority.High;
		}

		/// <summary>A filter the Filters menu does not offer, as a browse column filter
		/// is.</summary>
		private sealed class AcceptAllFilter : RecordFilter
		{
			public override bool Accept(IManyOnePathSortItem item) => true;
		}

		/// <summary>A clerk on the lexicon's entries list, outside any window.</summary>
		private sealed class TestClerk : RecordClerk
		{
			internal TestClerk(Mediator mediator, PropertyTable propertyTable, LcmCache cache)
			{
				m_mediator = mediator;
				m_propertyTable = propertyTable;
				m_id = "entries";
				var configuration = new XmlDocument();
				configuration.LoadXml("<recordList owner=\"LexDb\" property=\"Entries\"/>");
				m_list = RecordList.Create(cache, mediator, propertyTable, configuration.DocumentElement);
				m_list.Clerk = this;
			}

			internal void UseProvider(RecordFilterListProvider provider)
			{
				m_filterProvider = provider;
			}

			/// <summary>Records the menu filter as a Filters menu click does before the
			/// change.</summary>
			internal void ChooseMenuFilter(RecordFilter filter)
			{
				m_activeMenuBarFilter = filter;
			}
		}
	}
}
