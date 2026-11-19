// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Filters;
using SIL.IO;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The reversal clerk's persisted filter and sorter across the export loop, which switches
	/// the one reversal clerk through every reversal index.
	/// </summary>
	[TestFixture]
	public class DictionaryExportServiceReversalFilterTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private const string ClerkId = "AllReversalEntries";
		private const string LayoutProperty = "ReversalIndexPublicationLayout";
		private const string GuidProperty = "ReversalIndexGuid";

		// A filter and a reverse sorter on the Reversal Form column, as a settings file persists
		// them; the matcher's writing-system handle is substituted at run time.
		private const string FilterXml =
			@"<filter assemblyPath=""Filters.dll"" class=""SIL.FieldWorks.Filters.FilterBarCellFilter"" name=""Unknown""><finder assemblyPath=""XMLViews.dll"" class=""SIL.FieldWorks.Common.Controls.LayoutFinder"" layout=""""><column label=""Reversal Form"" width=""20%"" ws=""$ws=reversal"" transduce=""ReversalIndexEntry.ReversalForm"" cansortbylength=""true""><span><properties><editable value=""true"" /></properties><string field=""ReversalForm"" ws=""$ws=reversal"" /></span></column></finder><matcher assemblyPath=""Filters.dll"" class=""SIL.FieldWorks.Filters.RegExpMatcher"" label=""&lt;Str&gt;&#xD;&#xA;&lt;Run ws=&quot;en&quot;&gt;^[^z]&lt;/Run&gt;&#xD;&#xA;&lt;/Str&gt;"" pattern=""^[^z]"" ws=""WSHANDLE"" matchCase=""False"" matchDiacritics=""False"" /></filter>";
		private const string ReverseSorterXml =
			@"<sorter assemblyPath=""Filters.dll"" class=""SIL.FieldWorks.Filters.GenRecordSorter""><comparer assemblyPath=""Filters.dll"" class=""SIL.FieldWorks.Filters.StringFinderCompare""><finder assemblyPath=""XMLViews.dll"" class=""SIL.FieldWorks.Common.Controls.LayoutFinder"" layout=""""><column label=""Reversal Form"" width=""20%"" ws=""$ws=reversal"" transduce=""ReversalIndexEntry.ReversalForm"" cansortbylength=""true""><span><properties><editable value=""true"" /></properties><string field=""ReversalForm"" ws=""$ws=reversal"" /></span></column></finder><comparer assemblyPath=""Filters.dll"" class=""SIL.FieldWorks.Filters.ReverseComparer""><comparer assemblyPath=""Filters.dll"" class=""SIL.FieldWorks.Filters.WritingSystemComparer"" ws=""en"" /></comparer></comparer></sorter>";

		private PropertyTable m_propertyTable;
		private Mediator m_mediator;
		private FwXApp m_application;
		private FwXWindow m_window;
		private readonly List<string> m_settingNames = new List<string>();

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_mediator = m_window.Mediator;
			m_window.LoadUI(configFilePath);
			var layoutInventory = new Inventory("*.fwlayout", "/LayoutInventory/*", null, "test", "nowhere");
			Inventory.SetInventory("layouts", Cache.ProjectId.Name, layoutInventory);
			var partInventory = new Inventory("*Parts.xml", "/PartInventory/bin/*", null, "test", "nowhere");
			Inventory.SetInventory("parts", Cache.ProjectId.Name, partInventory);
		}

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			FwRegistrySettings.Release();
			m_application?.Dispose();
			m_window?.Dispose();
			m_propertyTable?.Dispose();
		}

		/// <summary>
		/// Each test starts with no reversal clerk and no reversal settings, so the clerk it
		/// creates restores only what the test persisted.
		/// </summary>
		[TearDown]
		public void TearDownTest()
		{
			var clerk = RecordClerk.FindClerk(m_propertyTable, ClerkId);
			m_propertyTable.RemoveProperty(RecordClerk.GetCorrespondingPropertyName(ClerkId));
			clerk?.Dispose();
			foreach (var wsId in new[] { "en", "fr" })
				foreach (var kind in new[] { "filter", "sorter", "sortName" })
					m_settingNames.Add(WsKey(wsId, kind));
			foreach (var name in m_settingNames)
				m_propertyTable.RemoveProperty(name, PropertyTable.SettingsGroup.LocalSettings);
			m_settingNames.Clear();
			m_propertyTable.RemoveProperty(LayoutProperty);
			m_propertyTable.RemoveProperty(GuidProperty);
		}

		private void SetLocalSetting(string name, string value)
		{
			m_settingNames.Add(name);
			m_propertyTable.SetProperty(name, value, PropertyTable.SettingsGroup.LocalSettings, false);
		}

		private static string WsKey(string wsId, string kind)
		{
			return "ReversalIndex.AllEntries:" + wsId + "_" + kind;
		}

		private static string PathKey(string configPath, string kind)
		{
			return "ReversalIndex.AllEntries-" + configPath.Substring(configPath.IndexOf('-') + 1) + "_" + kind;
		}

		private string ProjectConfigurationPath(string wsId)
		{
			return Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder),
				ReversalIndexServices.RevIndexDir, wsId + DictionaryConfigurationModel.FileExtension);
		}

		private void CreateIndexes(out IReversalIndex en, out IReversalIndex fr, out int wsEn)
		{
			wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			CoreWritingSystemDefinition wsFrDef;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out wsFrDef);
			Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsFrDef);
			var repo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			en = repo.FindOrCreateIndexForWs(wsEn);
			fr = repo.FindOrCreateIndexForWs(wsFrDef.Handle);
			en.FindOrCreateReversalEntry("apple");
			en.FindOrCreateReversalEntry("swing");
			en.FindOrCreateReversalEntry("zoo");
			fr.FindOrCreateReversalEntry("pomme");
		}

		private static DictionaryConfigurationModel MakeReversalConfig(string wsId)
		{
			return new DictionaryConfigurationModel { WritingSystem = wsId };
		}

		private static XmlNode ReversalFormColumn()
		{
			var path = Path.Combine(FwDirectoryFinder.CodeDirectory, FwDirectoryFinder.ksFlexFolderName,
				"Configuration", "Lexicon", "ReversalEntriesBulkEdit", "toolConfiguration.xml");
			var doc = new XmlDocument();
			doc.Load(path);
			return doc.SelectSingleNode("//column[@label='Reversal Form']");
		}

		/// <summary>
		/// A filter on the Reversal Form column, built as the filter bar's dialog builds it.
		/// </summary>
		private FilterBarCellFilter MakeRegexFilter(string regex, int ws)
		{
			var vc = new XmlBrowseViewBaseVc { SuppressPictures = true, Cache = Cache };
			var finder = LayoutFinder.CreateFinder(Cache, ReversalFormColumn(), vc, m_propertyTable.GetValue<IApp>("App"));
			var pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.Pattern = TsStringUtils.MakeString(regex, ws);
			SimpleStringMatcher.SetupPatternCollating(pattern, Cache);
			return new FilterBarCellFilter(finder, new RegExpMatcher(pattern));
		}

		private string Forms(int[] hvos)
		{
			var names = new List<string>();
			foreach (var hvo in hvos)
				names.Add(((IReversalIndexEntry)Cache.ServiceLocator.GetObject(hvo)).ShortName);
			return string.Join(", ", names);
		}

		[Test]
		public void GetReversalFilteredAndSortedEntries_KeepsFilterSetInView_AfterVisitingAnotherIndex()
		{
			CreateIndexes(out var en, out var fr, out var wsEn);
			var enConfig = MakeReversalConfig("en");
			var frConfig = MakeReversalConfig("fr");
			m_propertyTable.SetProperty(GuidProperty, en.Guid.ToString(), false);
			m_settingNames.Add(WsKey("en", "filter"));
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);
			var clerk = exportService.GetReversalClerk();
			clerk.ChangeOwningObject(en.Guid, false, enConfig);

			// Set the filter as the bulk edit view does.
			clerk.OnChangeFilter(new FilterChangeEventArgs(MakeRegexFilter("^[^z]", wsEn), null));
			Assert.That(m_propertyTable.GetStringProperty(WsKey("en", "filter"), null, PropertyTable.SettingsGroup.LocalSettings),
				Is.Not.Null, "the filter is persisted under the writing-system key");

			var decorator = ConfiguredLcmGenerator.CurrentDecorator(m_propertyTable, Cache, clerk);
			exportService.GetReversalFilteredAndSortedEntries(fr.Guid, decorator, frConfig, clerk);
			var enEntries = exportService.GetReversalFilteredAndSortedEntries(en.Guid, decorator, enConfig, clerk);

			Assert.That(Forms(enEntries), Does.Contain("apple").And.Contain("swing").And.Not.Contain("zoo"));
			Assert.That(clerk.Filter, Is.Not.Null, "the clerk keeps its filter for the view");
		}

		[Test]
		public void GetReversalFilteredAndSortedEntries_RestoresSettingsPersistedUnderEmptyLayout()
		{
			CreateIndexes(out var en, out var fr, out var wsEn);
			var enConfig = MakeReversalConfig("en");
			var frConfig = MakeReversalConfig("fr");
			// A settings file whose layout property is empty keys the filter and sorter with no
			// writing system; the index they belong to is the one ReversalIndexGuid names.
			m_propertyTable.SetProperty(LayoutProperty, "", false);
			m_propertyTable.SetProperty(GuidProperty, en.Guid.ToString(), false);
			SetLocalSetting("ReversalIndex.AllEntries-_filter", FilterXml.Replace("WSHANDLE", wsEn.ToString()));
			SetLocalSetting("ReversalIndex.AllEntries-_sorter", ReverseSorterXml);
			SetLocalSetting("ReversalIndex.AllEntries-_sortName", "Reversal Form");

			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);
			var clerk = exportService.GetReversalClerk();
			Assert.That(clerk.Filter, Is.Not.Null, "restored when the clerk is created");
			var decorator = ConfiguredLcmGenerator.CurrentDecorator(m_propertyTable, Cache, clerk);
			Assert.That(Forms(exportService.GetReversalFilteredAndSortedEntries(en.Guid, decorator, enConfig, clerk)),
				Is.EqualTo("swing, apple"), "filtered and reverse sorted before any switch");

			var frEntries = exportService.GetReversalFilteredAndSortedEntries(fr.Guid, decorator, frConfig, clerk);
			var enEntries = exportService.GetReversalFilteredAndSortedEntries(en.Guid, decorator, enConfig, clerk);

			Assert.That(Forms(frEntries), Is.EqualTo("pomme"), "the English settings do not apply to French");
			Assert.That(Forms(enEntries), Is.EqualTo("swing, apple"), "filtered and reverse sorted after visiting French");
			Assert.That(clerk.Filter, Is.Not.Null, "the clerk keeps its filter for the view");
		}

		[Test]
		public void GetReversalFilteredAndSortedEntries_RestoresSettingsPersistedUnderConfigurationPath()
		{
			CreateIndexes(out var en, out var fr, out var wsEn);
			var enConfig = MakeReversalConfig("en");
			var frConfig = MakeReversalConfig("fr");
			var configPath = ProjectConfigurationPath("en");
			m_propertyTable.SetProperty(LayoutProperty, configPath, false);
			m_propertyTable.SetProperty(GuidProperty, en.Guid.ToString(), false);
			SetLocalSetting(PathKey(configPath, "filter"), FilterXml.Replace("WSHANDLE", wsEn.ToString()));
			SetLocalSetting(PathKey(configPath, "sorter"), ReverseSorterXml);
			SetLocalSetting(PathKey(configPath, "sortName"), "Reversal Form");

			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);
			var clerk = exportService.GetReversalClerk();
			Assert.That(clerk.Filter, Is.Not.Null, "restored when the clerk is created");
			var decorator = ConfiguredLcmGenerator.CurrentDecorator(m_propertyTable, Cache, clerk);

			exportService.GetReversalFilteredAndSortedEntries(fr.Guid, decorator, frConfig, clerk);
			var enEntries = exportService.GetReversalFilteredAndSortedEntries(en.Guid, decorator, enConfig, clerk);

			Assert.That(Forms(enEntries), Is.EqualTo("swing, apple"), "filtered and reverse sorted after visiting French");
		}

		[Test]
		public void GetReversalFilteredAndSortedEntries_RestoresSettingsPersistedUnderCopiedConfigurationPath()
		{
			CreateIndexes(out var en, out var fr, out var wsEn);
			var enConfig = MakeReversalConfig("en");
			var frConfig = MakeReversalConfig("fr");
			// A copy made in Configure Dictionary is named after its label and declares the
			// writing system of the configuration it copies.
			var configDir = Path.GetDirectoryName(ProjectConfigurationPath("en"));
			Directory.CreateDirectory(configDir);
			using (var copy = TempFile.WithFilename(Path.Combine(configDir, "Copy of English" + DictionaryConfigurationModel.FileExtension)))
			{
				File.WriteAllText(copy.Path,
					"<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='Copy of English' writingSystem='en'/>");
				m_propertyTable.SetProperty(LayoutProperty, copy.Path, false);
				m_propertyTable.SetProperty(GuidProperty, en.Guid.ToString(), false);
				SetLocalSetting(PathKey(copy.Path, "filter"), FilterXml.Replace("WSHANDLE", wsEn.ToString()));
				SetLocalSetting(PathKey(copy.Path, "sorter"), ReverseSorterXml);
				SetLocalSetting(PathKey(copy.Path, "sortName"), "Reversal Form");

				var exportService = new DictionaryExportService(m_propertyTable, m_mediator);
				var clerk = exportService.GetReversalClerk();
				Assert.That(clerk.Filter, Is.Not.Null, "restored when the clerk is created");
				var decorator = ConfiguredLcmGenerator.CurrentDecorator(m_propertyTable, Cache, clerk);

				exportService.GetReversalFilteredAndSortedEntries(fr.Guid, decorator, frConfig, clerk);
				var enEntries = exportService.GetReversalFilteredAndSortedEntries(en.Guid, decorator, enConfig, clerk);

				Assert.That(Forms(enEntries), Is.EqualTo("swing, apple"), "filtered and reverse sorted after visiting French");
			}
		}

		[Test]
		public void OnChangeFilter_WithoutCurrentReversalIndex_InstallsTheFilterWithoutPersistingIt()
		{
			CreateIndexes(out var en, out _, out var wsEn);
			m_propertyTable.RemoveProperty(GuidProperty);
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);
			var clerk = exportService.GetReversalClerk();
			clerk.ChangeOwningObject(en.Guid, false, MakeReversalConfig("en"));

			clerk.OnChangeFilter(new FilterChangeEventArgs(MakeRegexFilter("^[^z]", wsEn), null));

			Assert.That(clerk.Filter, Is.Not.Null);
			Assert.That(m_propertyTable.PropertyExists(WsKey("en", "filter"), PropertyTable.SettingsGroup.LocalSettings), Is.False,
				"no reversal index is current, so there is no key to persist under");
		}

		[Test]
		public void GetReversalFilteredAndSortedEntries_KeepsAClearedFilterCleared_WhenAnOlderKeyHoldsOne()
		{
			CreateIndexes(out var en, out var fr, out var wsEn);
			m_propertyTable.SetProperty(GuidProperty, en.Guid.ToString(), false);
			// Cleared in the view, while the named configuration's key still holds a filter.
			SetLocalSetting(WsKey("en", "filter"), null);
			SetLocalSetting(PathKey(ProjectConfigurationPath("en"), "filter"), FilterXml.Replace("WSHANDLE", wsEn.ToString()));

			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);
			var clerk = exportService.GetReversalClerk();
			Assert.That(clerk.Filter, Is.Null, "nothing restored when the clerk is created");
			var decorator = ConfiguredLcmGenerator.CurrentDecorator(m_propertyTable, Cache, clerk);

			exportService.GetReversalFilteredAndSortedEntries(fr.Guid, decorator, MakeReversalConfig("fr"), clerk);
			var enEntries = exportService.GetReversalFilteredAndSortedEntries(en.Guid, decorator, MakeReversalConfig("en"), clerk);

			Assert.That(Forms(enEntries), Does.Contain("zoo"), "the English export is not filtered");
			Assert.That(clerk.Filter, Is.Null, "the view still has no filter");
		}
	}
}
