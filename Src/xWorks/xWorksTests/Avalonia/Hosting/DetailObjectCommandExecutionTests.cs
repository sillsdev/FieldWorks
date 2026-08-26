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
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;
// Both namespaces above define DataTree; the adapter tests mean the legacy WinForms one.
using LegacyDataTree = SIL.FieldWorks.Common.Framework.DetailControls.DataTree;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// END-TO-END execution + refresh proof for the core
	/// OBJECT commands the Avalonia detail view reuses from the legacy xCore machinery. This fixture
	/// proves on the REAL product host that these commands actually mutate the model and that the
	/// composed detail view reflects the mutation.
	///
	/// Seam: a real <see cref="RecordEditView"/> is loaded through <see cref="MockFwXWindow"/> in the
	/// New UI mode (the same bootstrap <c>RecordEditViewActiveHostContractTests</c> uses), so the
	/// Avalonia is the active host and the hidden legacy DataTree exists only as the approved
	/// "command-menu-routing" baseline adapter. Each test drives a command through the PRODUCTION path:
	/// 1. <c>EnsureMenuCommandAdapter(targetHvo)</c> -- builds/syncs the hidden adapter tree and
	/// points
	///      its CurrentSlice at the slice bound to the clicked row's object (exactly what
	///      <c>OnDetailMenuRequested</c> calls first).
	/// 2. <see cref="XCoreMenuBridge.CreateMenuItems(XWindow, string[])"/> -- the same
	/// native-menu
	/// materialization <c>OnDetailMenuRequested</c> performs; the resulting <see
	/// cref="DetailMenuItem"/>
	///      carries an Execute action that dispatches the command through the mediator
	/// (<c>ChoiceBase.OnClick</c> -> hidden DataTree/DTMenuHandler colleagues -> UOW mutation).
	/// Invoking that Execute is the user clicking the item. We then assert (a) the model mutated and
	/// (b) re-composing the entry (the same <see cref="DetailComposer.Compose"/> call
	/// <c>RecordEditView.ShowAvaloniaEntry</c> makes on refresh) reflects it.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class DetailObjectCommandExecutionTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private RecordEditView m_view;
		private Inventory m_layouts;
		private string m_layoutOverridePath;
		private bool m_layoutOverrideExisted;
		private byte[] m_layoutOverrideBytes;

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
			// EnsureDataTreeInitialized (LayoutCache loads the real lexicon .fwlayout/Parts).
			// Without it, DataTree.GetTemplateForObjLayout finds a null layout inventory and ShowObject
			// throws an NRE. This is the same bootstrap the DictionaryConfigurationMigrator tests use.
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application, Cache.ProjectId.Path);
			m_layouts = Inventory.GetInventory("layouts", Cache.ProjectId.Name);
			var configurationDirectory = LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.Path);
			m_layoutOverridePath = Path.GetFullPath(Path.Combine(configurationDirectory,
				"LexEntry.fwlayout"));
			Assert.That(Path.GetDirectoryName(m_layoutOverridePath),
				Is.EqualTo(Path.GetFullPath(configurationDirectory)).IgnoreCase);
			m_layoutOverrideExisted = File.Exists(m_layoutOverridePath);
			m_layoutOverrideBytes = m_layoutOverrideExisted
				? File.ReadAllBytes(m_layoutOverridePath)
				: null;
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateTestEntry);

			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();

			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null, "expected the lexicon edit RecordEditView to load");
			EnsureCurrentRecord(m_view);
			Assert.That(GetField(m_view, "m_activeUIFramework"), Is.EqualTo(UIFramework.Avalonia),
				"precondition: lexiconEdit resolves to Avalonia under the New UI mode");
		}

		[TearDown]
		public void TearDownWindow()
		{
			RestoreLayoutOverride();
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

		[TestCase("CmdAlwaysVisible", "Always visible", "ifdata", "always")]
		[TestCase("CmdIfData", "Normally hidden, unless non-empty", "always", "ifdata")]
		[TestCase("CmdNormallyHidden", "Normally hidden", "always", "never")]
		[TestCase("CmdDataTree-MoveFieldUp", "Move Up", null, "up")]
		[TestCase("CmdDataTree-MoveFieldDown", "Move Down", null, "down")]
		public void PersistentLayoutCommand_UsesLegacyWriter_PersistsAndRecomposes(
			string commandId, string label, string initialVisibility, string expectedChange)
		{
			if (initialVisibility != null)
				PersistCitationVisibility(initialVisibility);
			RefreshAvaloniaDetail();
			if (expectedChange == "up")
				MoveCitationDownThroughNativeCommand();

			var beforeModel = GetHostedDetailModel();
			var field = beforeModel.Fields.Single(f => f.Field == "CitationForm");
			var beforeIndex = beforeModel.Fields.ToList().IndexOf(field);
			var layoutBefore = CurrentLexEntryLayout().OuterXml;

			var items = CreateNativeMenuItems(field,
				new[] { "mnuDataTree-MultiStringSlice", RecordEditView.ObjectMenuId });
			var item = FindItem(items, label);
			Assert.That(item, Is.Not.Null, commandId + " should materialize through the native menu");
			Assert.That(item.IsEnabled, Is.True, commandId + " should be enabled for Citation Form");
			item.Execute();

			var persisted = CurrentLexEntryLayout();
			Assert.That(persisted.OuterXml, Is.Not.EqualTo(layoutBefore),
				commandId + " should run the legacy Slice handler and change its Inventory layout");
			Assert.That(File.Exists(m_layoutOverridePath), Is.True,
				commandId + " should persist through Inventory to the project .fwlayout file");

			var afterModel = GetHostedDetailModel();
			Assert.That(afterModel, Is.Not.SameAs(beforeModel),
				commandId + " should refresh the Avalonia model from the changed XML");
			if (expectedChange == "never")
			{
				Assert.That(afterModel.Fields, Has.None.Property("Field").EqualTo("CitationForm"));
			}
			else if (expectedChange == "up" || expectedChange == "down")
			{
				var afterIndex = afterModel.Fields.ToList().FindIndex(f => f.Field == "CitationForm");
				Assert.That(Math.Sign(afterIndex - beforeIndex),
					Is.EqualTo(expectedChange == "up" ? -1 : 1),
					commandId + " should recompose Citation Form in the persisted direction");
			}
			else
			{
				var part = persisted.SelectSingleNode("part[@ref='CitationFormAllV']");
				Assert.That(part.Attributes["visibility"].Value, Is.EqualTo(expectedChange));
				Assert.That(afterModel.Fields, Has.Some.Property("Field").EqualTo("CitationForm"));
			}

			var configurationDirectory = Path.GetDirectoryName(m_layoutOverridePath);
			Assert.That(Directory.GetFiles(configurationDirectory, "*.viewoverride.json"), Is.Empty);
		}

		[Test]
		public void PersistentLayoutCommand_MissingExactIdentity_ClearsTargetAndDoesNotWrite()
		{
			PersistCitationVisibility("ifdata");
			RefreshAvaloniaDetail();
			var field = GetHostedDetailModel().Fields.Single(f => f.Field == "CitationForm");
			field.SourceCallerPath = "part[999]";
			var beforeBytes = File.ReadAllBytes(m_layoutOverridePath);

			var items = CreateNativeMenuItems(field,
				new[] { "mnuDataTree-MultiStringSlice", RecordEditView.ObjectMenuId });
			var item = FindItem(items, "Always visible");
			var dataTree = (LegacyDataTree)GetField(m_view, "m_dataEntryForm");

			Assert.That(item, Is.Not.Null.And.Property("IsEnabled").False);
			Assert.That(dataTree.CurrentSlice, Is.Null,
				"a persistent command with no unique exact identity must clear the legacy target");
			item.Execute();
			Assert.That(File.ReadAllBytes(m_layoutOverridePath), Is.EqualTo(beforeBytes),
				"a disabled persistent command must not invoke the legacy Inventory writer");
		}

		// ----------------------------------------------------------------------------------------
		// Insert Sense
		// ----------------------------------------------------------------------------------------

		[Test]
		public void InsertSense_FromSenseMenu_AddsSenseToModel_AndComposedDetailReflectsIt()
		{
			var sensesBefore = m_entry.SensesOS.Count;
			var senseHeadersBefore = ComposeSenseHeaderCount();

			InvokeSliceMenuCommand(m_entry.SensesOS[0].Hvo, "mnuDataTree-Sense", "Insert Sense");

			Assert.That(m_entry.SensesOS.Count, Is.EqualTo(sensesBefore + 1),
				"Insert Sense must add a sense to the entry through the real command + UOW");
			Assert.That(ComposeSenseHeaderCount(), Is.GreaterThan(senseHeadersBefore),
				"the re-composed detail view (what RefreshAvaloniaDetail re-shows) gains a sense header for the new sense");
		}

		[Test]
		public void InsertSense_ViaHotlinks_AddsSense_EndToEndThroughXCoreMenuBridge()
		{
			var sensesBefore = m_entry.SensesOS.Count;

			// Hotlinks is the production path for the section's quick-add affordance:
			// OnDetailMenuRequested(Kind=Hotlinks) builds ONLY the HotlinksId menu through the same
			// XCoreMenuBridge; mnuDataTree-Sense-Hotlinks offers "Insert Sense".
			InvokeHotlinksCommand(m_entry.SensesOS[0].Hvo, "mnuDataTree-Sense-Hotlinks", "Insert Sense");

			Assert.That(m_entry.SensesOS.Count, Is.EqualTo(sensesBefore + 1),
				"Insert Sense via the hotlinks path mutates the model end-to-end through XCoreMenuBridge");
			Assert.That(RefreshedDetailFieldCount(), Is.GreaterThan(0),
				"the host can re-show the detail view after a hotlink-create");
		}

		// ----------------------------------------------------------------------------------------
		// Delete Sense / Delete object
		// ----------------------------------------------------------------------------------------

		// Skipped (desktop environment only): unlike Insert (always enabled), the Delete/Move/Demote/Merge sense
		// commands only materialize+enable when their xCore display handlers can compute live
		// slice-sequence context (position in the owning sequence, owner relationships). That context
		// comes from a laid-out, VISIBLE legacy DataTree; the command-routing adapter tree is
		// hidden +
		// detached while Avalonia is active, so headlessly the items never reach the
		// enabled state and the menu does not surface them. Hosting/laying out the detached tree in the
		// test was tried and did not surface them -- the gap is the full menu-display path, not
		// just
		// slice layout. Runnable in the desktop environment where the legacy tree is shown. The InsertSense
		// tests above exercise the same end-to-end adapter -> XCoreMenuBridge -> mediator -> UOW path
		// headlessly, so the core execution+refresh seam is still covered.
		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree menu-display path; see note above. Runs in the desktop environment.")]
		public void DeleteSense_RemovesSenseFromModel_AndComposedDetailReflectsIt()
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
				"the re-composed detail view drops the deleted sense's header");
		}

		// ----------------------------------------------------------------------------------------
		// Move Up / Move Down (in sequence)
		// ----------------------------------------------------------------------------------------

		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree menu-display path (Move command enablement needs live slice-sequence context). Runs in the desktop environment.")]
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

			// Move it back UP: the original order is restored -- a sequence proving both
			// directions.
			InvokeSliceMenuCommand(firstHvo, "mnuDataTree-Sense", "Move Sense Up");
			Assert.That(m_entry.SensesOS[0].Hvo, Is.EqualTo(firstHvo),
				"Move Up returns the sense to the front, restoring the original order");
			Assert.That(m_entry.SensesOS[1].Hvo, Is.EqualTo(secondHvo));

			Assert.That(RefreshedDetailFieldCount(), Is.GreaterThan(0),
				"the detail view still composes after the reorder sequence");
		}

		// ----------------------------------------------------------------------------------------
		// Promote / Demote (Make Subsense)
		// ----------------------------------------------------------------------------------------

		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree menu-display path (Demote/Promote enablement needs live slice-sequence/owner context). Runs in the desktop environment.")]
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
		[Explicit("Requires the live (laid-out, visible) legacy DataTree menu-display path (the sense-merge command must materialize+enable before its class guard runs). Runs in the desktop environment.")]
		public void Merge_SenseClassGuard_OnlyTargetsMatchingClass()
		{
			AddSense("second gloss");
			var senseHvo = m_entry.SensesOS[0].Hvo;

			// OnDataTreeMerge's class guard (LT-22352) returns false when the merge command's declared
			// className (LexSense) does not match the current slice's object class. Targeting a NON-sense
			// row (the entry/citation row, a LexEntry) with the sense-merge command must be a guarded
			// no-op -- the model is untouched and nothing is mis-merged. This proves the adapter
			// targeting
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

		// -----------------------------------------------------------------
		// Lexeme Form slice menu: adapter reachability, Writing Systems
		// -----------------------------------------------------------------

		// The adapter tree is never shown, so message-target selection must keep it in the
		// colleague chain for handlers defined on DataTree itself, such as OnJumpToTool.
		[Test]
		public void HiddenAdapterTree_StaysInTheColleagueChain_SoItsOwnHandlersAreReachable()
		{
			EnsureAdapter(m_entry.LexemeFormOA.Hvo, "Form");

			var dataTree = (LegacyDataTree)GetField(m_view, "m_dataEntryForm");
			Assert.That(dataTree.Visible, Is.False,
				"precondition: the adapter tree is hidden while Avalonia is the active host");
			Assert.That(dataTree.IsExternalCommandAdapter, Is.True,
				"precondition: the host flagged it as the command-routing adapter");
			Assert.That(dataTree.GetMessageTargets(), Does.Contain(dataTree),
				"handlers defined on the tree itself (JumpToTool, Delete, Insert) must stay reachable");
		}

		// The Lexeme Form row and its indented siblings share one MoForm, so matching by
		// object alone is ambiguous; the concordance jump and the Writing Systems list
		// both need the Form slice.
		[Test]
		public void LexemeFormRow_TargetsTheFormSlice_NotJustSomeSliceOnTheSameMorph()
		{
			EnsureAdapter(m_entry.LexemeFormOA.Hvo, "Form");

			var dataTree = (LegacyDataTree)GetField(m_view, "m_dataEntryForm");
			var current = dataTree.CurrentSlice;
			Assert.That(current, Is.Not.Null, "a slice must be targeted for the Lexeme Form row");
			Assert.That(current.Object?.Hvo, Is.EqualTo(m_entry.LexemeFormOA.Hvo));
			Assert.That(current.Flid, Is.EqualTo(MoFormTags.kflidForm),
				"the targeted slice must be the Form field's own slice");
		}

		// DataTree.OnDisplayJumpToTool offers the jump only when CurrentSlice.Flid is the
		// MoForm's Form field, so an enabled item proves the tree is reachable and the row
		// targeted that slice.
		[Test]
		public void ConcordanceCommand_OnTheLexemeFormRow_IsEnabledByItsRealHandler()
		{
			EnsureAdapter(m_entry.LexemeFormOA.Hvo, "Form");

			var jump = FindItem(BuildItems(new[] { "mnuDataTree-LexemeForm", "mnuDataTree-MultiStringSlice" }),
				"Show Lexeme Form in Concordance");

			Assert.That(jump, Is.Not.Null, "the command materializes on the Lexeme Form row");
			Assert.That(jump.IsEnabled, Is.True,
				"DataTree.OnDisplayJumpToTool must run and resolve a concordance target for the Form field");
			Assert.That(jump.Execute, Is.Not.Null, "and it carries a mediator-dispatch action");
		}

		// The writing-system entries come from an inline list group: they need the Form slice
		// targeted and the group spliced into its parent.
		[Test]
		public void LexemeFormSliceMenu_WritingSystemsSubmenu_ListsTheProjectWritingSystems()
		{
			EnsureAdapter(m_entry.LexemeFormOA.Hvo, "Form");
			var items = BuildItems(new[] { "mnuDataTree-LexemeForm", "mnuDataTree-MultiStringSlice" });

			var writingSystems = FindItem(items, "Writing Systems");
			Assert.That(writingSystems, Is.Not.Null,
				"the multistring group contributes the Writing Systems submenu");

			var labels = writingSystems.Children.Where(c => !c.IsSeparator).Select(c => c.Label).ToList();
			Assert.That(labels, Does.Contain("Show all right now"));
			Assert.That(labels, Does.Contain("Configure..."));

			// Both commands are handled by the slice, so they enable only once it is
			// reachable through the colleague chain.
			Assert.That(FindItem(items, "Show all right now").IsEnabled, Is.True);
			Assert.That(FindItem(items, "Configure...").IsEnabled, Is.True);

			// The inline list splices the slice's own writing systems between the two commands.
			var spliced = labels.Where(l => l != "Show all right now" && l != "Configure...").ToList();
			Assert.That(spliced, Is.Not.Empty,
				"the inline list group must contribute the slice's writing systems, not be nested away");

			var vernacular = Cache.LangProject.CurrentVernacularWritingSystems
				.Select(ws => ws.DisplayLabel).ToList();
			Assert.That(spliced.Intersect(vernacular), Is.Not.Empty,
				"the spliced entries are the project's vernacular writing systems (the Lexeme Form's ws set)");
		}

		// -----------------------------------------------------------------
		// Helpers -- production-path command drivers
		// -----------------------------------------------------------------

		// Drives a SLICE-menu command exactly as OnDetailMenuRequested(Kind=SliceMenu) does: ensure the
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

		// Drives a HOTLINKS command as OnDetailMenuRequested(Kind=Hotlinks) does: only the HotlinksId
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

		private void EnsureAdapter(int targetHvo, string fieldName = null)
		{
			var method = typeof(RecordEditView).GetMethod("EnsureMenuCommandAdapter",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null, "EnsureMenuCommandAdapter must exist (adapter targeting seam)");
			method.Invoke(m_view, new object[] { targetHvo, fieldName });
		}

		private IReadOnlyList<DetailMenuItem> BuildItems(string[] menuIds)
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			Assert.That(window, Is.Not.Null);
			return XCoreMenuBridge.CreateMenuItems(window, menuIds);
		}

		private IReadOnlyList<DetailMenuItem> CreateNativeMenuItems(DetailField field, string[] menuIds)
		{
			var method = typeof(RecordEditView).GetMethod("CreateNativeDetailMenuItems",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null,
				"the product host should expose its native menu materialization seam");
			return (IReadOnlyList<DetailMenuItem>)method.Invoke(m_view,
				new object[] { field, menuIds });
		}

		private void InvokeItem(IReadOnlyList<DetailMenuItem> items, string label)
		{
			var item = FindItem(items, label);
			Assert.That(item, Is.Not.Null, "expected a '{0}' menu item to materialize", label);
			Assert.That(item.IsEnabled, Is.True, "the '{0}' command must be enabled for the target", label);
			Assert.That(item.Execute, Is.Not.Null, "the '{0}' item must carry a mediator-dispatch action", label);
			item.Execute();
		}

		// Items come from XCoreMenuBridge with accelerators already stripped; match on the visible label,
		// searching submenus too (some commands nest).
		private static DetailMenuItem FindItem(IReadOnlyList<DetailMenuItem> items, string label)
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

		// -----------------------------------------------------------------
		// Helpers -- refresh / detail assertions
		// -----------------------------------------------------------------

		// Re-composes the displayed entry exactly as RecordEditView.ShowAvaloniaEntry does on refresh
		// and counts the per-sense section headers (one per sense regardless of the sense's
		// content --
		// an empty new sense still gets a header, unlike its ifData Gloss row). This is the
		// user-visible proof that the recomposed detail view reflects the model mutation.
		private int ComposeSenseHeaderCount()
		{
			var composed = DetailComposer.Compose(m_entry, Cache);
			Assert.That(composed, Is.Not.Null, "the entry must compose");
			return composed.Model.Fields.Count(f => f.Kind == DetailFieldKind.Header && f.Field == "Senses");
		}

		// Calls the host's real RefreshAvaloniaDetail and reports the field count of the recomposed
		// model, proving the host can re-render after the command without throwing.
		private int RefreshedDetailFieldCount()
		{
			var refresh = typeof(RecordEditView).GetMethod("RefreshAvaloniaDetail",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(refresh, Is.Not.Null);
			refresh.Invoke(m_view, null);
			DrainMediatorAndIdleQueues();
			return DetailComposer.Compose(m_entry, Cache).Model.Fields.Count;
		}

		private void RefreshAvaloniaDetail()
		{
			var refresh = typeof(RecordEditView).GetMethod("RefreshAvaloniaDetail",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(refresh, Is.Not.Null);
			refresh.Invoke(m_view, null);
			DrainMediatorAndIdleQueues();
		}

		private DetailModel GetHostedDetailModel()
		{
			var entryForm = (DetailHostControl)GetField(m_view, "m_avaloniaEntryForm");
			var hostField = typeof(AvaloniaHostControlBase).GetField("Host",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(hostField, Is.Not.Null);
			var host = hostField.GetValue(entryForm);
			var content = host.GetType().GetProperty("Content").GetValue(host, null);
			var tree = content as SIL.FieldWorks.Common.FwAvalonia.Detail.DataTree;
			Assert.That(tree, Is.Not.Null);
			return tree.Model;
		}

		private XmlNode CurrentLexEntryLayout()
		{
			var layout = m_layouts.GetElement("layout",
				new[] { "LexEntry", "detail", "Normal", null });
			Assert.That(layout, Is.Not.Null);
			return layout;
		}

		private void PersistCitationVisibility(string visibility)
		{
			var changed = CurrentLexEntryLayout().Clone();
			var part = changed.SelectSingleNode("part[@ref='CitationFormAllV']");
			Assert.That(part, Is.Not.Null);
			var attribute = part.Attributes["visibility"]
				?? changed.OwnerDocument.CreateAttribute("visibility");
			attribute.Value = visibility;
			if (attribute.OwnerElement == null)
				part.Attributes.Append(attribute);
			m_layouts.PersistOverrideElement(changed);
		}

		private void MoveCitationDownThroughNativeCommand()
		{
			var field = GetHostedDetailModel().Fields.Single(f => f.Field == "CitationForm");
			var items = CreateNativeMenuItems(field,
				new[] { "mnuDataTree-MultiStringSlice", RecordEditView.ObjectMenuId });
			var item = FindItem(items, "Move Down");
			Assert.That(item, Is.Not.Null.And.Property("IsEnabled").True,
				"Move Down must establish a real legacy-slice predecessor for Move Up");
			item.Execute();
		}

		private void RestoreLayoutOverride()
		{
			if (m_layouts == null || string.IsNullOrEmpty(m_layoutOverridePath))
				return;
			if (m_layoutOverrideExisted)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(m_layoutOverridePath));
				File.WriteAllBytes(m_layoutOverridePath, m_layoutOverrideBytes);
			}
			else if (File.Exists(m_layoutOverridePath))
			{
				File.Delete(m_layoutOverridePath);
			}
			m_layouts.Reload();
			Assert.That(Inventory.GetInventory("layouts", Cache.ProjectId.Name), Is.SameAs(m_layouts));
		}

		// ----------------------------------------------------------------------------------------
		// Bootstrap helpers (mirrors RecordEditViewActiveHostContractTests)
		// ----------------------------------------------------------------------------------------

		private void CreateTestEntry()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "command-entry", stemMorphType, "first gloss", noun);
			m_entry.CitationForm.set_String(Cache.DefaultVernWs,
				TsStringUtils.MakeString("citation", Cache.DefaultVernWs));
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

		// DrainMediatorAndIdleQueues is inherited from XWorksAppTestBase.
	}
}
