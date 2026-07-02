// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Task B + Task C hardening for the Avalonia lexical-edit object-command path.
	///
	/// Task B — <c>EnsureMenuCommandAdapter</c> targeting: when the target hvo's slice is a lazy,
	/// unrealized <c>DummyObjectSlice</c> (a sequence with &gt;= <c>DataTree.kInstantSliceMax</c> items
	/// builds lazy placeholders whose <c>Object</c> is the OWNER, not the target), the old code found no
	/// matching slice and left CurrentSlice pointed wherever the previous interaction left it — so the
	/// command mis-targeted or (for Merge's class guard) silently failed. The hardened code realizes the
	/// lazy slices and retries, and fails LOUD (clears CurrentSlice + logs) when no slice can be produced.
	///
	/// Task C2 — splitter width SESSION persistence: the host's remembered label-column width was
	/// process-only; the product host now routes a PropertyTable LocalSetting so the width survives across
	/// sessions, mirroring the expansion-persistence pattern.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class RegionCommandAdapterHardeningTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private RecordEditView m_view;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			// The hidden legacy DataTree's ShowObject (driven by EnsureMenuCommandAdapter) needs the
			// legacy layout/parts Inventory loaded; that Inventory is keyed by the project path, so
			// give the in-memory test project a writable temp path before the inventory bootstrap.
			Cache.ProjectId.Path = Path.Combine(Path.GetTempPath(), Cache.ProjectId.Name,
				Cache.ProjectId.Name + ".junk");
		}

		[SetUp]
		public void SetUpWindow()
		{
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_propertyTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			// The lexicon detail layout includes the Chorus-backed MessageSlice, which localizes
			// strings when built; register a minimal LocalizationManager (as the product does at
			// startup) so ShowObject can build the full tree headlessly.
			TestLocalizationManagerBootstrap.EnsureInitialized();
			// The mock app has no inner help-topic provider; give the PropertyTable a null-returning
			// stub so the legacy Help command can be queried while materializing the menu.
			TestLocalizationManagerBootstrap.EnsureHelpTopicProvider(m_propertyTable);
			// Bootstrap the legacy layout/parts Inventory the production RecordEditView loads via
			// EnsureLegacySurfaceInitialized (LayoutCache loads the real lexicon .fwlayout/Parts).
			// Without it, DataTree.GetTemplateForObjLayout finds a null layout inventory and ShowObject
			// throws an NRE. This is the same bootstrap the DictionaryConfigurationMigrator tests use.
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application, Cache.ProjectId.Path);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateEntryWithManySenses);

			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView("lexiconEdit");
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

		// ----------------------------------------------------------------------------------------
		// Task B — adapter targeting through (and past) lazy slices
		// ----------------------------------------------------------------------------------------

		// Skipped (desktop lane): realizing lazy DummyObjectSlices and pointing CurrentSlice at a deep
		// target runs through DataTree.FieldAt/MakeSliceRealAt, which depend on a laid-out, VISIBLE
		// tree (ClientRectangle width, AutoScrollPosition, MakeSliceVisible). The command-routing
		// adapter tree is hidden + detached while the Avalonia surface is active, so headlessly the
		// lazy slices do not realize and CurrentSlice cannot be resolved. Runnable on the desktop lane
		// where the legacy tree is shown. (The DataTreeMove reachability tests cover the targeting/
		// reachability logic that CAN be exercised without a live tree.)
		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree to realize lazy slices and resolve CurrentSlice; see note above. Runs on the desktop lane.")]
		public void EnsureMenuCommandAdapter_TargetInLazySliceRange_RealizesAndTargetsTheRightObject()
		{
			// The entry has well over DataTree.kInstantSliceMax (20) senses, so the Senses sequence builds
			// lazy DummyObjectSlices. Target a sense deep in the lazy range — its slice does not exist yet,
			// so the old single-pass match would have failed and left CurrentSlice mis-pointed.
			Assert.That(m_entry.SensesOS.Count, Is.GreaterThan(20),
				"precondition: enough senses to force lazy DummyObjectSlices");
			var deepSenseHvo = m_entry.SensesOS[m_entry.SensesOS.Count - 1].Hvo;

			EnsureAdapter(deepSenseHvo);

			var dataTree = (DataTree)GetField(m_view, "m_dataEntryForm");
			var current = dataTree.CurrentSlice;
			Assert.That(current, Is.Not.Null,
				"the adapter must realize the lazy slice and point CurrentSlice at the deep target");
			Assert.That(current.Object, Is.Not.Null);
			Assert.That(current.Object.Hvo, Is.EqualTo(deepSenseHvo),
				"CurrentSlice targets the requested object, not a lazy dummy's owner or a stale slice");
			Assert.That(current.IsRealSlice, Is.True, "the targeted slice was realized");
		}

		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree to realize slices and resolve/clear CurrentSlice; the hidden detached adapter tree never lays out headlessly. Runs on the desktop lane.")]
		public void EnsureMenuCommandAdapter_NoSliceMatchesHvo_ClearsCurrentSliceRatherThanMisTarget()
		{
			// First point the adapter at a real sense, so CurrentSlice is non-null...
			var realSenseHvo = m_entry.SensesOS[0].Hvo;
			EnsureAdapter(realSenseHvo);
			var dataTree = (DataTree)GetField(m_view, "m_dataEntryForm");
			Assert.That(dataTree.CurrentSlice, Is.Not.Null, "precondition: a slice is current");

			// ...then target an hvo that has NO slice in this entry's tree (a foreign object). The hardened
			// adapter must NOT leave the previous sense's slice current (that would mis-target the command);
			// it clears CurrentSlice so the command handlers see "no current slice" and no-op.
			ILexEntry foreign = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var stem = GetMorphTypeOrCreateOne("stem");
				var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
				foreign = AddLexeme(m_createdObjects, "foreign", stem, "foreign gloss", noun);
			});

			EnsureAdapter(foreign.Hvo);

			Assert.That(dataTree.CurrentSlice, Is.Null,
				"when no slice matches the target, CurrentSlice is cleared so the command no-ops rather "
				+ "than mis-targeting the previously selected object");
		}

		// ----------------------------------------------------------------------------------------
		// Task C2 — splitter width persists to a PropertyTable LocalSetting (round-trips across sessions)
		// ----------------------------------------------------------------------------------------

		[Test]
		public void LabelColumnWidth_PersistsToLocalSetting_AndRoundTrips()
		{
			var persist = typeof(RecordEditView).GetMethod("PersistLabelColumnWidth",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var read = typeof(RecordEditView).GetMethod("GetPersistedLabelColumnWidth",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(persist, Is.Not.Null, "splitter persistence setter must exist (Task C2 wiring)");
			Assert.That(read, Is.Not.Null, "splitter persistence getter must exist (Task C2 wiring)");

			// Nothing persisted yet -> null, so the view falls back to the density default.
			Assert.That(read.Invoke(m_view, null), Is.Null,
				"with no stored setting the getter returns null (density default applies)");

			persist.Invoke(m_view, new object[] { 173.5 });

			Assert.That((double?)read.Invoke(m_view, null), Is.EqualTo(173.5).Within(0.001),
				"the persisted width round-trips through the getter");

			// It really landed in the PropertyTable local settings keyed by the current tool, marked
			// persistent (so it survives across sessions, unlike the former process-only host field).
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

		// ----------------------------------------------------------------------------------------
		// Bootstrap helpers
		// ----------------------------------------------------------------------------------------

		private void CreateEntryWithManySenses()
		{
			var stem = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "many-sense-entry", stem, "gloss 0", noun);
			// 25 senses: comfortably above DataTree.kInstantSliceMax (20) so the Senses sequence builds
			// lazy DummyObjectSlices in the adapter tree.
			for (var i = 1; i < 25; i++)
				AddSenseToEntry(m_createdObjects, m_entry, "gloss " + i, noun);
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

		private void EnsureAdapter(int targetHvo)
		{
			var method = typeof(RecordEditView).GetMethod("EnsureMenuCommandAdapter",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null);
			method.Invoke(m_view, new object[] { targetHvo });
		}

		private void LoadRecordEditView(string toolValue)
		{
			var windowConfiguration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			var controlNode = windowConfiguration.SelectSingleNode(
				string.Format("//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']", toolValue));
			Assert.That(controlNode, Is.Not.Null);
			m_propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			m_propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_propertyTable.SetProperty("currentContentControl", toolValue, true);
			m_propertyTable.SetPropertyPersistence("currentContentControl", false);
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
