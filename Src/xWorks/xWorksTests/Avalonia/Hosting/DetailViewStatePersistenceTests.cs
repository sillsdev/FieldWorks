// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// View state the Avalonia detail view keeps across sessions in PropertyTable local
	/// settings: the label-column width, per tool, and each section's expansion state.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class DetailViewStatePersistenceTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private RecordEditView m_view;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
		}

		[SetUp]
		public void SetUpWindow()
		{
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_propertyTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateEntry);

			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			DetailMenuTestSupport.LoadRecordEditView(m_propertyTable, "lexiconEdit");
			DrainMediatorAndIdleQueues();

			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null);
			EnsureCurrentRecord(m_view);
		}

		[TearDown]
		public void TearDownWindow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DestroyTestData);
			m_createdObjects = null;
			m_entry = null;
			m_view = null;
			m_propertyTable?.RemoveLocalAndGlobalSettings();
			m_propertyTable = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		[Test]
		public void LabelColumnWidth_PersistsToLocalSetting_AndRoundTrips()
		{
			var persist = typeof(RecordEditView).GetMethod("PersistLabelColumnWidth",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var read = typeof(RecordEditView).GetMethod("GetPersistedLabelColumnWidth",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(persist, Is.Not.Null, "splitter persistence setter must exist");
			Assert.That(read, Is.Not.Null, "splitter persistence getter must exist");

			// Nothing persisted yet -> null, so the view falls back to the density default.
			Assert.That(read.Invoke(m_view, null), Is.Null,
				"with no stored setting the getter returns null (density default applies)");

			persist.Invoke(m_view, new object[] { 173.5 });

			Assert.That((double?)read.Invoke(m_view, null), Is.EqualTo(173.5).Within(0.001),
				"the persisted width round-trips through the getter");

			// It really landed in the PropertyTable local settings keyed by the current tool,
			// marked persistent (so it survives across sessions, unlike a process-only field).
			var key = "LexEditLabelColumnWidth:lexiconEdit";
			var stored = m_propertyTable.GetStringProperty(key, null, PropertyTable.SettingsGroup.LocalSettings);
			Assert.That(stored, Is.Not.Null.And.Not.Empty,
				"the width is stored in a PropertyTable LocalSetting keyed by tool");
		}

		[Test]
		public void LabelColumnWidth_IgnoresNonPositiveWidths()
		{
			var persist = typeof(RecordEditView).GetMethod("PersistLabelColumnWidth",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var read = typeof(RecordEditView).GetMethod("GetPersistedLabelColumnWidth",
				BindingFlags.Instance | BindingFlags.NonPublic);

			persist.Invoke(m_view, new object[] { 0.0 });
			Assert.That(read.Invoke(m_view, null), Is.Null, "a zero width must not be persisted");
			persist.Invoke(m_view, new object[] { -5.0 });
			Assert.That(read.Invoke(m_view, null), Is.Null, "a negative width must not be persisted");
		}

		// LT-22689: the in-session dictionary is cleared to stand in for a restart, so the read
		// must come from the PropertyTable local setting.
		[Test]
		public void ExpansionState_PersistsToLocalSetting_AndIsReadBackInANewSession()
		{
			const string key = "LexEntry/Normal/#12@0123456789abcdef0123456789abcdef";
			Assert.That(m_view.GetPersistedExpansionState(key), Is.Null, "nothing recorded yet");

			m_view.PersistExpansionState(key, false);

			Assert.That(m_propertyTable.GetStringProperty(RecordEditView.ExpansionSettingPrefix + key,
				null, PropertyTable.SettingsGroup.LocalSettings), Is.EqualTo("0"));
			((Dictionary<string, bool>)GetField(m_view, "m_expansionStates")).Clear();
			Assert.That(m_view.GetPersistedExpansionState(key), Is.False);
		}

		private void CreateEntry()
		{
			var stem = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "persistence-entry", stem, "gloss", noun);
		}

		private void DestroyTestData()
		{
			if (m_createdObjects == null)
				return;
			foreach (var obj in m_createdObjects)
			{
				if (obj.IsValidObject && obj is ILexEntry)
					obj.Delete();
			}
		}

		private void EnsureCurrentRecord(RecordEditView control)
		{
			if (control.Clerk.CurrentObject?.Hvo != m_entry.Hvo)
			{
				control.Clerk.JumpToRecord(m_entry.Hvo);
				DrainMediatorAndIdleQueues();
			}
			Assert.That(control.Clerk.CurrentObject, Is.Not.Null);
		}

		private static object GetField(object target, string fieldName)
		{
			var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
			return field.GetValue(target);
		}

		// DrainMediatorAndIdleQueues is inherited from XWorksAppTestBase.
	}
}
