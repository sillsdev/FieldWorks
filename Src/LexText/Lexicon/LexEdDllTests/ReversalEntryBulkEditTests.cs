// Copyright (c) 2015-2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.XWorks.LexEd;
using XCore;

namespace LexEdDllTests
{
	/// <summary>
	/// The settings keys under which the reversal entries list persists and reads its filter,
	/// sorter and sort name.
	/// </summary>
	[TestFixture]
	public class ReversalEntryBulkEditTests : MemoryOnlyBackendProviderTestBase
	{
		const string FieldName = "field";
		private IReversalIndex m_enIndex;

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_enIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(Cache.DefaultAnalWs));
		}

		private PropertyTable MakePropertyTable(Mediator mediator)
		{
			var propertyTable = new PropertyTable(mediator);
			propertyTable.SetProperty("cache", Cache, false);
			return propertyTable;
		}

		private string WsKey(string wsId)
		{
			return "ReversalIndex.AllEntries-" + wsId + "_" + FieldName;
		}

		/// <summary>
		/// The key built from the project's configuration file path for the writing system.
		/// </summary>
		private string ConfigurationPathKey(string wsId)
		{
			var configPath = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder),
				ReversalIndexServices.RevIndexDir, wsId + DictionaryConfigurationModel.FileExtension);
			return "ReversalIndex.AllEntries-" + configPath.Substring(configPath.IndexOf('-') + 1) + "_" + FieldName;
		}

		[Test]
		public void PropertyTableId_UsesWritingSystemOfCurrentReversalIndex()
		{
			using (var mediator = new Mediator())
			using (var propertyTable = MakePropertyTable(mediator))
			using (var recordList = new TestReversalRecordList(Cache, mediator, propertyTable))
			{
				propertyTable.SetProperty("ReversalIndexGuid", m_enIndex.Guid.ToString(), false);

				Assert.That(recordList.GetPropertyTableId(FieldName), Is.EqualTo(WsKey(m_enIndex.WritingSystem)));
			}
		}

		[Test]
		public void PropertyTableIdReturnsNullIfNoActiveReversalIndex()
		{
			using (var mediator = new Mediator())
			using (var propertyTable = MakePropertyTable(mediator))
			using (var recordList = new TestReversalRecordList(Cache, mediator, propertyTable))
			{
				Assert.That(recordList.GetPropertyTableId(FieldName), Is.Null);
				Assert.That(recordList.GetPersisted(FieldName), Is.Null);
			}
		}

		[Test]
		public void GetPersistedSetting_PrefersWritingSystemKey()
		{
			using (var mediator = new Mediator())
			using (var propertyTable = MakePropertyTable(mediator))
			using (var recordList = new TestReversalRecordList(Cache, mediator, propertyTable))
			{
				var wsId = m_enIndex.WritingSystem;
				propertyTable.SetProperty("ReversalIndexGuid", m_enIndex.Guid.ToString(), false);
				propertyTable.SetProperty("ReversalIndexPublicationLayout", "", false);
				propertyTable.SetProperty(WsKey(wsId), "by writing system", PropertyTable.SettingsGroup.LocalSettings, false);
				propertyTable.SetProperty("ReversalIndex.AllEntries-_" + FieldName, "by empty layout", PropertyTable.SettingsGroup.LocalSettings, false);
				propertyTable.SetProperty(ConfigurationPathKey(wsId), "by path", PropertyTable.SettingsGroup.LocalSettings, false);

				Assert.That(recordList.GetPersisted(FieldName), Is.EqualTo("by writing system"));
			}
		}

		[Test]
		public void GetPersistedSetting_ReadsEmptyLayoutKeyForCurrentIndex()
		{
			using (var mediator = new Mediator())
			using (var propertyTable = MakePropertyTable(mediator))
			using (var recordList = new TestReversalRecordList(Cache, mediator, propertyTable))
			{
				var wsId = m_enIndex.WritingSystem;
				var emptyKey = "ReversalIndex.AllEntries-_" + FieldName;
				propertyTable.SetProperty("ReversalIndexGuid", m_enIndex.Guid.ToString(), false);
				propertyTable.SetProperty("ReversalIndexPublicationLayout", "", false);
				propertyTable.SetProperty(emptyKey, "by empty layout", PropertyTable.SettingsGroup.LocalSettings, false);

				Assert.That(recordList.GetPersisted(FieldName), Is.EqualTo("by empty layout"));
				Assert.That(propertyTable.GetStringProperty(WsKey(wsId), null, PropertyTable.SettingsGroup.LocalSettings),
					Is.EqualTo("by empty layout"), "moved to the writing-system key");
				Assert.That(propertyTable.GetStringProperty(emptyKey, null, PropertyTable.SettingsGroup.LocalSettings),
					Is.Null, "cleared from the empty-layout key");
			}
		}

		[Test]
		public void GetPersistedSetting_ReadsConfigurationPathKey()
		{
			using (var mediator = new Mediator())
			using (var propertyTable = MakePropertyTable(mediator))
			using (var recordList = new TestReversalRecordList(Cache, mediator, propertyTable))
			{
				var wsId = m_enIndex.WritingSystem;
				propertyTable.SetProperty("ReversalIndexGuid", m_enIndex.Guid.ToString(), false);
				propertyTable.SetProperty(ConfigurationPathKey(wsId), "by path", PropertyTable.SettingsGroup.LocalSettings, false);

				Assert.That(recordList.GetPersisted(FieldName), Is.EqualTo("by path"));
				Assert.That(propertyTable.GetStringProperty(WsKey(wsId), null, PropertyTable.SettingsGroup.LocalSettings),
					Is.EqualTo("by path"), "moved to the writing-system key");
				Assert.That(propertyTable.GetStringProperty(ConfigurationPathKey(wsId), null, PropertyTable.SettingsGroup.LocalSettings),
					Is.Null, "cleared from the path key");
			}
		}

		[Test]
		public void GetPersistedSetting_ReadsLayoutPathKeyWhenItNamesTheWritingSystem()
		{
			using (var mediator = new Mediator())
			using (var propertyTable = MakePropertyTable(mediator))
			using (var recordList = new TestReversalRecordList(Cache, mediator, propertyTable))
			{
				var wsId = m_enIndex.WritingSystem;
				var layoutPath = Path.Combine(@"C:\Another-Machine\Projects\Example\ConfigurationSettings\ReversalIndex",
					wsId + DictionaryConfigurationModel.FileExtension);
				propertyTable.SetProperty("ReversalIndexGuid", m_enIndex.Guid.ToString(), false);
				propertyTable.SetProperty("ReversalIndexPublicationLayout", layoutPath, false);
				var layoutKey = "ReversalIndex.AllEntries-" + layoutPath.Substring(layoutPath.IndexOf('-') + 1) + "_" + FieldName;
				propertyTable.SetProperty(layoutKey, "by layout path", PropertyTable.SettingsGroup.LocalSettings, false);

				Assert.That(recordList.GetPersisted(FieldName), Is.EqualTo("by layout path"));
				Assert.That(propertyTable.GetStringProperty(WsKey(wsId), null, PropertyTable.SettingsGroup.LocalSettings),
					Is.EqualTo("by layout path"), "moved to the writing-system key");
				Assert.That(propertyTable.GetStringProperty(layoutKey, null, PropertyTable.SettingsGroup.LocalSettings),
					Is.Null, "cleared from the layout path key");
			}
		}

		class TestReversalRecordList : AllReversalEntriesRecordList
		{
			/// <summary>Configured as the AllReversalEntries clerk's record list is.</summary>
			public TestReversalRecordList(LcmCache cache, Mediator mediator, PropertyTable propertyTable)
			{
				var doc = new XmlDocument();
				doc.LoadXml("<recordList owner=\"ReversalIndex\" property=\"AllEntries\"/>");
				Init(cache, mediator, propertyTable, doc.DocumentElement);
			}

			public string GetPropertyTableId(string fieldName, DictionaryConfigurationModel config = null)
			{
				return PropertyTableId(fieldName, config);
			}

			public string GetPersisted(string fieldName, DictionaryConfigurationModel config = null)
			{
				return GetPersistedSetting(fieldName, config);
			}
		}
	}
}
