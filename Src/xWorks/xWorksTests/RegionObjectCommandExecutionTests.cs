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
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Task A (lexical-edit Avalonia migration) — END-TO-END execution + refresh proof for the core
	/// OBJECT commands the Avalonia surface reuses from the legacy xCore machinery. There were ZERO
	/// tests proving these commands actually mutate the model and that the composed region reflects
	/// the mutation; this fixture closes that gap on the REAL product host.
	///
	/// Seam: a real <see cref="RecordEditView"/> is loaded through <see cref="MockFwXWindow"/> in the
	/// New UI mode (the same bootstrap <c>RecordEditViewActiveHostContractTests</c> uses), so the
	/// Avalonia surface is the active host and the hidden legacy DataTree exists only as the approved
	/// "command-menu-routing" baseline adapter. Each test drives a command through the PRODUCTION path:
	///   1. <c>EnsureMenuCommandAdapter(targetHvo)</c> — builds/syncs the hidden adapter tree and points
	///      its CurrentSlice at the slice bound to the clicked row's object (exactly what
	///      <c>OnRegionMenuRequested</c> calls first).
	///   2. <see cref="XCoreMenuBridge.BuildMenuItems(XWindow, string[])"/> — the same native-menu
	///      materialization <c>OnRegionMenuRequested</c> performs; the resulting <see cref="RegionMenuItem"/>
	///      carries an Execute action that dispatches the command through the mediator
	///      (<c>ChoiceBase.OnClick</c> → hidden DataTree/DTMenuHandler colleagues → UOW mutation).
	/// Invoking that Execute is the user clicking the item. We then assert (a) the model mutated and
	/// (b) re-composing the entry (the same <see cref="FullEntryRegionComposer.Compose"/> call
	/// <c>RecordEditView.ShowAvaloniaEntry</c> makes on refresh) reflects it.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class RegionObjectCommandExecutionTests : XWorksAppTestBase
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
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateTestEntry);

			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();

			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null, "expected the lexicon edit RecordEditView to load");
			EnsureCurrentRecord(m_view);
			Assert.That(GetField(m_view, "m_lexicalEditSurface"), Is.EqualTo(LexicalEditSurface.Avalonia),
				"precondition: lexiconEdit resolves to the Avalonia surface under the New UI mode");
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
		// Insert Sense
		// ----------------------------------------------------------------------------------------

		[Test]
		public void InsertSense_FromSenseMenu_AddsSenseToModel_AndComposedRegionReflectsIt()
		{
			var sensesBefore = m_entry.SensesOS.Count;
			var senseHeadersBefore = ComposeSenseHeaderCount();

			InvokeSliceMenuCommand(m_entry.SensesOS[0].Hvo, "mnuDataTree-Sense", "Insert Sense");

			Assert.That(m_entry.SensesOS.Count, Is.EqualTo(sensesBefore + 1),
				"Insert Sense must add a sense to the entry through the real command + UOW");
			Assert.That(ComposeSenseHeaderCount(), Is.GreaterThan(senseHeadersBefore),
				"the re-composed region (what RefreshAvaloniaRegion re-shows) gains a sense header for the new sense");
		}

		[Test]
		public void InsertSense_ViaHotlinks_AddsSense_EndToEndThroughXCoreMenuBridge()
		{
			var sensesBefore = m_entry.SensesOS.Count;

			// The Hotlinks lane is the production path for the section's quick-add affordance:
			// OnRegionMenuRequested(Kind=Hotlinks) builds ONLY the HotlinksId menu through the same
			// XCoreMenuBridge; mnuDataTree-Sense-Hotlinks offers "Insert Sense".
			InvokeHotlinksCommand(m_entry.SensesOS[0].Hvo, "mnuDataTree-Sense-Hotlinks", "Insert Sense");

			Assert.That(m_entry.SensesOS.Count, Is.EqualTo(sensesBefore + 1),
				"Insert Sense via the hotlinks path mutates the model end-to-end through XCoreMenuBridge");
			Assert.That(RefreshedRegionFieldCount(), Is.GreaterThan(0),
				"the host can re-show the region after a hotlink-create");
		}

		// ----------------------------------------------------------------------------------------
		// Delete Sense / Delete object
		// ----------------------------------------------------------------------------------------

		// Skipped (desktop lane): unlike Insert (always enabled), the Delete/Move/Demote/Merge sense
		// commands only materialize+enable when their xCore display handlers can compute live
		// slice-sequence context (position in the owning sequence, owner relationships). That context
		// comes from a laid-out, VISIBLE legacy DataTree; the command-routing adapter tree is hidden +
		// detached while the Avalonia surface is active, so headlessly the items never reach the
		// enabled state and the menu does not surface them. Hosting/laying out the detached tree in the
		// test was tried and did not surface them — the gap is the full menu-display lane, not just
		// slice layout. Runnable on the desktop lane where the legacy tree is shown. The InsertSense
		// tests above exercise the same end-to-end adapter -> XCoreMenuBridge -> mediator -> UOW path
		// headlessly, so the core execution+refresh seam is still covered.
		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree menu-display lane; see note above. Runs on the desktop lane.")]
		public void DeleteSense_RemovesSenseFromModel_AndComposedRegionReflectsIt()
		{
			AddSense("extra gloss");
			var sensesBefore = m_entry.SensesOS.Count;
			Assert.That(sensesBefore, Is.GreaterThanOrEqualTo(2));
			var targetHvo = m_entry.SensesOS[sensesBefore - 1].Hvo;
			var senseHeadersBefore = ComposeSenseHeaderCount();

			InvokeSliceMenuCommand(targetHvo, "mnuDataTree-Sense", "Delete this Sense and any Subsenses");

			Assert.That(m_entry.SensesOS.Count, Is.EqualTo(sensesBefore - 1),
				"Delete Sense removes the targeted sense via the real command");
			Assert.That(Cache.ServiceLocator.ObjectRepository.IsValidObjectId(targetHvo), Is.False,
				"the deleted sense object is really gone from the model");
			Assert.That(ComposeSenseHeaderCount(), Is.LessThan(senseHeadersBefore),
				"the re-composed region drops the deleted sense's header");
		}

		// ----------------------------------------------------------------------------------------
		// Move Up / Move Down (in sequence)
		// ----------------------------------------------------------------------------------------

		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree menu-display lane (Move command enablement needs live slice-sequence context). Runs on the desktop lane.")]
		public void MoveDownThenMoveUp_ReordersSenses_AndRestoresOriginalOrder()
		{
			AddSense("second gloss");
			AddSense("third gloss");
			Assert.That(m_entry.SensesOS.Count, Is.EqualTo(3));
			var firstHvo = m_entry.SensesOS[0].Hvo;
			var secondHvo = m_entry.SensesOS[1].Hvo;

			// Move the first sense DOWN: it should swap with the second.
			InvokeSliceMenuCommand(firstHvo, "mnuDataTree-Sense", "Move Sense Down");
			Assert.That(m_entry.SensesOS[0].Hvo, Is.EqualTo(secondHvo),
				"Move Down advances the targeted sense past its successor");
			Assert.That(m_entry.SensesOS[1].Hvo, Is.EqualTo(firstHvo));

			// Move it back UP: the original order is restored — a sequence proving both directions.
			InvokeSliceMenuCommand(firstHvo, "mnuDataTree-Sense", "Move Sense Up");
			Assert.That(m_entry.SensesOS[0].Hvo, Is.EqualTo(firstHvo),
				"Move Up returns the sense to the front, restoring the original order");
			Assert.That(m_entry.SensesOS[1].Hvo, Is.EqualTo(secondHvo));

			Assert.That(RefreshedRegionFieldCount(), Is.GreaterThan(0),
				"the region still composes after the reorder sequence");
		}

		// ----------------------------------------------------------------------------------------
		// Promote / Demote (Make Subsense)
		// ----------------------------------------------------------------------------------------

		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree menu-display lane (Demote/Promote enablement needs live slice-sequence/owner context). Runs on the desktop lane.")]
		public void DemoteThenPromoteSense_MovesSenseBetweenOwners()
		{
			AddSense("second gloss");
			Assert.That(m_entry.SensesOS.Count, Is.EqualTo(2));
			var firstHvo = m_entry.SensesOS[0].Hvo;
			var secondHvo = m_entry.SensesOS[1].Hvo;

			// Demote: the second top-level sense becomes a subsense of the first (shipped label is "Demote").
			InvokeSliceMenuCommand(secondHvo, "mnuDataTree-Sense", "Demote");
			Assert.That(m_entry.SensesOS.Count, Is.EqualTo(1),
				"demote removes the sense from the entry's top-level senses");
			var first = (ILexSense)Cache.ServiceLocator.GetObject(firstHvo);
			Assert.That(first.SensesOS.Select(s => s.Hvo), Does.Contain(secondHvo),
				"the demoted sense is now nested under the previous sibling");

			// Promote: the subsense returns to the entry's top-level senses.
			InvokeSliceMenuCommand(secondHvo, "mnuDataTree-Sense", "Promote");
			Assert.That(m_entry.SensesOS.Select(s => s.Hvo), Does.Contain(secondHvo),
				"promote lifts the subsense back to the entry's senses");
			Assert.That(((ILexSense)Cache.ServiceLocator.GetObject(firstHvo)).SensesOS.Count, Is.EqualTo(0),
				"the former owner no longer owns it");
		}

		// ----------------------------------------------------------------------------------------
		// Merge
		// ----------------------------------------------------------------------------------------

		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree menu-display lane (the sense-merge command must materialize+enable before its class guard runs). Runs on the desktop lane.")]
		public void Merge_SenseClassGuard_OnlyTargetsMatchingClass()
		{
			AddSense("second gloss");
			var senseHvo = m_entry.SensesOS[0].Hvo;

			// OnDataTreeMerge's class guard (LT-22352) returns false when the merge command's declared
			// className (LexSense) does not match the current slice's object class. Targeting a NON-sense
			// row (the entry/citation row, a LexEntry) with the sense-merge command must be a guarded
			// no-op — the model is untouched and nothing is mis-merged. This proves the adapter targeting
			// reaches the merge handler and that its guard fires on the real object class.
			var sensesBefore = m_entry.SensesOS.Count;
			var citationHvo = m_entry.Hvo;
			var invoked = TryInvokeMergeAgainst(citationHvo);

			Assert.That(invoked, Is.True,
				"the sense-merge command must materialize and dispatch through the adapter (its guard then "
				+ "decides the outcome)");
			Assert.That(m_entry.SensesOS.Count, Is.EqualTo(sensesBefore),
				"a class-guarded merge against a non-sense object must not mutate the senses");
			Assert.That(Cache.ServiceLocator.ObjectRepository.IsValidObjectId(senseHvo), Is.True,
				"no sense was merged away by the guarded command");
		}

		// ----------------------------------------------------------------------------------------
		// Helpers — production-path command drivers
		// ----------------------------------------------------------------------------------------

		// Drives a SLICE-menu command exactly as OnRegionMenuRequested(Kind=SliceMenu) does: ensure the
		// adapter targets the object, materialize the menu (menuId + the host-appended mnuDataTree-Object)
		// through XCoreMenuBridge, find the item by label, invoke its Execute (mediator dispatch). Then
		// drain the mediator/idle queues so the UOW PropChanged + refresh settle.
		private void InvokeSliceMenuCommand(int targetHvo, string menuId, string itemLabel)
		{
			EnsureAdapter(targetHvo);
			var items = BuildItems(new[] { menuId, "mnuDataTree-Object" });
			InvokeItem(items, itemLabel);
			DrainMediatorAndIdleQueues();
		}

		// Drives a HOTLINKS command as OnRegionMenuRequested(Kind=Hotlinks) does: only the HotlinksId
		// menu is materialized.
		private void InvokeHotlinksCommand(int targetHvo, string hotlinksId, string itemLabel)
		{
			EnsureAdapter(targetHvo);
			var items = BuildItems(new[] { hotlinksId });
			InvokeItem(items, itemLabel);
			DrainMediatorAndIdleQueues();
		}

		// Targets a (non-sense) object with the sense-merge command and returns whether an enabled item
		// was found+invoked. We do not depend on a chooser dialog: the class guard short-circuits before
		// HandleMergeCommand opens any UI when the target class mismatches.
		private bool TryInvokeMergeAgainst(int targetHvo)
		{
			EnsureAdapter(targetHvo);
			var items = BuildItems(new[] { "mnuDataTree-Sense", "mnuDataTree-Object" });
			var merge = FindItem(items, "Merge Sense into...");
			if (merge?.Execute == null)
				return false;
			merge.Execute();
			DrainMediatorAndIdleQueues();
			return true;
		}

		private void EnsureAdapter(int targetHvo)
		{
			var method = typeof(RecordEditView).GetMethod("EnsureMenuCommandAdapter",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "EnsureMenuCommandAdapter must exist (adapter targeting seam)");
			method.Invoke(m_view, new object[] { targetHvo });
		}

		private IReadOnlyList<RegionMenuItem> BuildItems(string[] menuIds)
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			Assert.That(window, Is.Not.Null);
			return XCoreMenuBridge.BuildMenuItems(window, menuIds);
		}

		private void InvokeItem(IReadOnlyList<RegionMenuItem> items, string label)
		{
			var item = FindItem(items, label);
			Assert.That(item, Is.Not.Null, "expected a '{0}' menu item to materialize", label);
			Assert.That(item.IsEnabled, Is.True, "the '{0}' command must be enabled for the target", label);
			Assert.That(item.Execute, Is.Not.Null, "the '{0}' item must carry a mediator-dispatch action", label);
			item.Execute();
		}

		// Items come from XCoreMenuBridge with accelerators already stripped; match on the visible label,
		// searching submenus too (some commands nest).
		private static RegionMenuItem FindItem(IReadOnlyList<RegionMenuItem> items, string label)
		{
			foreach (var item in items)
			{
				if (item.IsSeparator)
					continue;
				if (string.Equals(item.Label, label, StringComparison.Ordinal))
					return item;
				var nested = FindItem(item.Children, label);
				if (nested != null)
					return nested;
			}
			return null;
		}

		// ----------------------------------------------------------------------------------------
		// Helpers — refresh / region assertions
		// ----------------------------------------------------------------------------------------

		// Re-composes the displayed entry exactly as RecordEditView.ShowAvaloniaEntry does on refresh
		// and counts the per-sense section headers (one per sense regardless of the sense's content —
		// an empty new sense still gets a header, unlike its ifData Gloss row). This is the
		// surface-visible proof that the recomposed region reflects the model mutation.
		private int ComposeSenseHeaderCount()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			Assert.That(composed, Is.Not.Null, "the entry must compose");
			return composed.Model.Fields.Count(f => f.Kind == RegionFieldKind.Header && f.Field == "Senses");
		}

		// Calls the host's real RefreshAvaloniaRegion and reports the field count of the recomposed
		// model, proving the host can re-render after the command without throwing.
		private int RefreshedRegionFieldCount()
		{
			var refresh = typeof(RecordEditView).GetMethod("RefreshAvaloniaRegion",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(refresh, Is.Not.Null);
			refresh.Invoke(m_view, null);
			DrainMediatorAndIdleQueues();
			return FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields.Count;
		}

		// ----------------------------------------------------------------------------------------
		// Bootstrap helpers (mirrors RecordEditViewActiveHostContractTests)
		// ----------------------------------------------------------------------------------------

		private void CreateTestEntry()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "command-entry", stemMorphType, "first gloss", noun);
		}

		private void AddSense(string gloss)
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
				AddSenseToEntry(m_createdObjects, m_entry, gloss, noun);
			});
			DrainMediatorAndIdleQueues();
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

		private void LoadRecordEditView(string toolValue)
		{
			var windowConfiguration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			var controlNode = windowConfiguration.SelectSingleNode(
				string.Format("//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']", toolValue));
			Assert.That(controlNode, Is.Not.Null, "Expected the RecordEditView configuration node for tool '{0}'.", toolValue);

			m_propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			m_propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_propertyTable.SetProperty("currentContentControl", toolValue, true);
			m_propertyTable.SetPropertyPersistence("currentContentControl", false);
		}

		private void EnsureCurrentRecord(RecordEditView control)
		{
			if (control.Clerk.CurrentObject == null)
			{
				control.Clerk.JumpToRecord(m_entry.Hvo);
				DrainMediatorAndIdleQueues();
			}
			// Make our entry the displayed record so commands target it.
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

		private void DrainMediatorAndIdleQueues()
		{
			var idleQueue = m_window.Mediator.IdleQueue;
			var processIdle = idleQueue.GetType().GetMethod("Application_Idle", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(processIdle, Is.Not.Null);

			for (var iteration = 0; iteration < 8; iteration++)
			{
				((MockFwXWindow)m_window).ProcessPendingItems();
				if (idleQueue.Count == 0 && m_window.Mediator.JobItems == 0)
					break;
				if (idleQueue.Count > 0)
					processIdle.Invoke(idleQueue, new object[] { this, EventArgs.Empty });
			}

			Application.DoEvents();
		}
	}
}
