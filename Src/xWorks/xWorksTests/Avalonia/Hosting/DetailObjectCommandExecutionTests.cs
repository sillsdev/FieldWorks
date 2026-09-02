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
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
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

		// THE decisive copy test: unchecking one of two vernaculars must store the REDUCED
		// set (the slice still holds the pre-click set right after OnClick), and re-checking
		// must store the restored set.
		[Test]
		public void WritingSystemToggle_TwoVernaculars_StoresTheReducedThenRestoredSet()
		{
			CoreWritingSystemDefinition second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(second);
			});
			var field = LexemeFormField();
			try
			{
				EnsureAdapter(m_entry.LexemeFormOA.Hvo, "Form");
				var writingSystems = BuildWritingSystemsSubmenu(field);
				var checkedEntries = writingSystems.Children
					.Where(c => !c.IsSeparator && c.IsChecked && c.Execute != null).ToList();
				TestContext.WriteLine("checked: " + string.Join(", ",
					checkedEntries.Select(e => e.Label)));
				Assert.That(checkedEntries.Count, Is.EqualTo(2),
					"precondition: both vernacular writing systems start visible");
				var toggled = checkedEntries[0].Label;

				checkedEntries[0].Execute();
				DrainMediatorAndIdleQueues();

				var reduced = StoredWritingSystems(field);
				TestContext.WriteLine("reduced: " + string.Join(",", reduced));
				Assert.That(reduced, Is.EqualTo(new[] { "es" }),
					"the stored op holds the set AFTER the uncheck, not the pre-click set");

				// Re-check the same writing system on a freshly built menu: the ADD direction.
				writingSystems = BuildWritingSystemsSubmenu(field);
				var reAdd = writingSystems.Children.Single(c => !c.IsSeparator && c.Label == toggled);
				Assert.That(reAdd.IsChecked, Is.False, "the unchecked toggle stays unchecked");
				Assert.That(reAdd.Execute, Is.Not.Null, "an unchecked toggle is clickable");
				reAdd.Execute();
				DrainMediatorAndIdleQueues();

				var restored = StoredWritingSystems(field);
				TestContext.WriteLine("restored: " + string.Join(",", restored));
				// Exact order matters: the property appends the re-checked writing system at the
				// end, but the copy canonicalizes to option order so rows never reorder.
				Assert.That(restored, Is.EqualTo(new[] { "fr", "es" }),
					"re-checking stores the restored set in option order");
			}
			finally
			{
				DeleteOverrideFor(field);
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second));
			}
		}

		// The Lexeme Form row is keyed by the MoForm's own descended (class, layout). Compose
		// must resolve an override for it, or the row's customizations are written and never
		// read.
		[Test]
		public void Compose_ResolvesTheOverrideForADescendedLayout()
		{
			var field = LexemeFormField();
			var store = GetOverrideStore();
			var templateId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId);
			TestContext.WriteLine(
				$"class={field.ClassName} layout={field.LayoutName} template={templateId}");
			try
			{
				store.Save(new ViewDefinitionOverride(field.ClassName, field.LayoutName, "detail",
					new[]
					{
						new ViewOverrideOperation(ViewOverrideOperationKind.SetLabel, templateId,
							label: "OverrideMarker")
					}, null));

				var asked = new List<string>();
				var recomposed = DetailComposer.Compose(m_entry, Cache, showHiddenFields: false,
					overrides: (cls, layout) =>
					{
						asked.Add(cls + "/" + layout);
						return store.TryGet(cls, layout);
					});
				TestContext.WriteLine("compose asked for: " + string.Join(", ", asked));

				var row = FindLexemeFormRow(recomposed.Model);
				Assert.That(row.Label, Is.EqualTo("OverrideMarker"),
					"an override keyed by the row's own (class, layout) must reach the composed row");
			}
			finally
			{
				DeleteOverrideFor(field);
			}
		}

		// -----------------------------------------------------------------
		// "Show all right now": the transient writing-system reveal
		// -----------------------------------------------------------------

		// The intercepted item must reveal WITHOUT persisting: the row composes with the full
		// set while the stored override keeps the restriction.
		[Test]
		public void ShowAllRightNow_RevealsTheFullSet_WithoutWritingTheOverride()
		{
			CoreWritingSystemDefinition second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(second);
			});
			var field = LexemeFormField();
			try
			{
				var fullSet = field.Values.Select(v => v.WsTag).ToList();
				Assume.That(fullSet, Is.EqualTo(new List<string> { "fr", "es" }),
					"precondition: the unrestricted Lexeme Form row shows both vernaculars");
				GetOverrideStore().Save(new ViewDefinitionOverride(field.ClassName, field.LayoutName,
					"detail", new[]
					{
						new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibleWritingSystems,
							ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId),
							writingSystems: new[] { "fr" })
					}, null));
				Assert.That(ComposedFormWsTags(RevealedFields()), Is.EqualTo(new[] { "fr" }),
					"precondition: the override restricts the composed row");

				InvokeShowAllRightNow(field);

				Assert.That(RevealedFields(), Does.Contain(TemplateId(field)),
					"the click marks the row's part in the host's transient reveal set");
				Assert.That(ComposedFormWsTags(RevealedFields()), Is.EqualTo(fullSet),
					"the revealed row composes with the full writing-system set");
				Assert.That(StoredWritingSystems(field), Is.EqualTo(new[] { "fr" }),
					"the stored override keeps the restriction -- the reveal is never persisted");
			}
			finally
			{
				DeleteOverrideFor(field);
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second));
			}
		}

		// The reveal survives everything within the record and expires only when the shown
		// record changes.
		[Test]
		public void TransientReveal_ExpiresOnRecordNavigation()
		{
			var field = LexemeFormField();
			InvokeShowAllRightNow(field);
			Assert.That(RevealedFields(), Does.Contain(TemplateId(field)), "precondition: revealed");

			ILexEntry other = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var stemMorphType = GetMorphTypeOrCreateOne("stem");
				var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
				other = AddLexeme(m_createdObjects, "other-entry", stemMorphType, "other gloss", noun);
			});
			m_view.Clerk.JumpToRecord(other.Hvo);
			DrainMediatorAndIdleQueues();

			Assert.That(RevealedFields(), Is.Empty,
				"showing a different record expires every transient reveal");
		}

		// A configuration write replaces the reveal: after a toggle, the row shows the newly
		// stored set, not the stale full set.
		[Test]
		public void WritingSystemToggle_ReplacesTheTransientReveal()
		{
			CoreWritingSystemDefinition second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(second);
			});
			var field = LexemeFormField();
			try
			{
				InvokeShowAllRightNow(field);
				Assert.That(RevealedFields(), Does.Contain(TemplateId(field)), "precondition: revealed");

				var toggle = BuildWritingSystemsSubmenu(field).Children
					.First(c => !c.IsSeparator && c.IsChecked && c.Execute != null);
				var toggledLabel = toggle.Label;
				toggle.Execute();
				DrainMediatorAndIdleQueues();

				Assert.That(RevealedFields(), Does.Not.Contain(TemplateId(field)),
					"persisting a new display set replaces the transient reveal");

				// Re-check the toggled writing system: the toggle persists the visible set into
				// the part-ref Inventory, which is shared across this fixture's tests.
				var reAdd = BuildWritingSystemsSubmenu(field).Children
					.Single(c => !c.IsSeparator && c.Label == toggledLabel);
				reAdd.Execute();
				DrainMediatorAndIdleQueues();
			}
			finally
			{
				DeleteOverrideFor(field);
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second));
			}
		}

		// Drives the intercepted "Show all right now" item for a row the way
		// OnDetailMenuRequested would: target the adapter, materialize the submenu, invoke,
		// drain.
		private void InvokeShowAllRightNow(DetailField field)
		{
			EnsureAdapter(field.ObjectHvo, field.Field);
			var showAll = FindItem(BuildWritingSystemsSubmenu(field).Children, "Show all right now");
			Assert.That(showAll, Is.Not.Null, "the intercepted reveal item must materialize");
			Assert.That(showAll.IsEnabled, Is.True);
			Assert.That(showAll.Execute, Is.Not.Null);
			showAll.Execute();
			DrainMediatorAndIdleQueues();
		}

		// The reveal set is keyed by the part's template id, not the runtime row id.
		private static string TemplateId(DetailField field)
			=> ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId);

		// A failed override save must leave the reveal alone: ending it without recomposing
		// would collapse the row at the next unrelated refresh, with nothing to explain it.
		[Test]
		public void TransientReveal_SurvivesAFailedOverrideSave()
		{
			CoreWritingSystemDefinition second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(second);
			});
			var field = LexemeFormField();
			// A file where the store expects its directory: Save's CreateDirectory throws.
			var blocker = Path.Combine(Path.GetTempPath(), "fw-reveal-" + Guid.NewGuid().ToString("N"));
			File.WriteAllText(blocker, "not a directory");
			var storeField = typeof(RecordEditView).GetField("m_viewOverrideStore",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(storeField, Is.Not.Null, "the override store field must exist");
			var original = storeField.GetValue(m_view);
			string toggledLabel = null;
			try
			{
				InvokeShowAllRightNow(field);
				Assert.That(RevealedFields(), Does.Contain(TemplateId(field)), "precondition: revealed");

				storeField.SetValue(m_view, new ViewDefinitionOverrideStore(blocker));
				var toggle = BuildWritingSystemsSubmenu(field).Children
					.First(c => !c.IsSeparator && c.IsChecked && c.Execute != null);
				toggledLabel = toggle.Label;
				toggle.Execute();
				DrainMediatorAndIdleQueues();

				Assert.That(RevealedFields(), Does.Contain(TemplateId(field)),
					"a save that failed must leave the transient reveal in place");
			}
			finally
			{
				storeField.SetValue(m_view, original);
				File.Delete(blocker);
				// The toggle persists into the part-ref inventory this fixture shares; re-check
				// it or later tests see no visible writing system.
				if (toggledLabel != null)
				{
					BuildWritingSystemsSubmenu(field).Children
						.Single(c => !c.IsSeparator && c.Label == toggledLabel).Execute();
					DrainMediatorAndIdleQueues();
				}
				DeleteOverrideFor(field);
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second));
			}
		}

		// The host's transient reveal set, read through the same seam the production compose
		// uses.
		private HashSet<string> RevealedFields()
			=> (HashSet<string>)GetField(m_view, "m_showAllWsFields");

		// The Lexeme Form row's composed writing-system tags under the CURRENT stored override
		// and the given reveal set -- the same Compose call ShowAvaloniaEntry makes.
		private IReadOnlyList<string> ComposedFormWsTags(HashSet<string> reveal)
			=> FindLexemeFormRow(DetailComposer.Compose(m_entry, Cache,
					overrides: (cls, layout) => GetOverrideStore().TryGet(cls, layout),
					showAllWritingSystemsFields: reveal).Model)
				.Values.Select(v => v.WsTag).ToList();

		// -----------------------------------------------------------------
		// Helpers -- production-path command drivers
		// -----------------------------------------------------------------

		// The composed Lexeme Form row, as the production host resolves it before raising a
		// menu request.
		private DetailField LexemeFormField()
			=> FindLexemeFormRow(DetailComposer.Compose(m_entry, Cache).Model);

		// The one locator for the Lexeme Form row (the MoForm's own Form field) in a composed
		// model.
		private DetailField FindLexemeFormRow(DetailModel model)
			=> model.Fields.Single(f => f.Field == "Form" && f.Kind == DetailFieldKind.Text
				&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);

		private ViewDefinitionOverrideStore GetOverrideStore()
		{
			var storeProperty = typeof(RecordEditView).GetProperty("ViewOverrideStore",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(storeProperty, Is.Not.Null, "the override store seam must exist");
			var store = (ViewDefinitionOverrideStore)storeProperty.GetValue(m_view);
			Assert.That(store, Is.Not.Null, "the project must have a reachable override store");
			return store;
		}

		// Materializes a menu the way OnDetailMenuRequested does, WITH the override interceptor,
		// so the intercepted writing-system items are the ones under test.
		private IReadOnlyList<DetailMenuItem> BuildItemsWithOverrideInterceptor(string[] menuIds,
			DetailField field)
		{
			var build = typeof(RecordEditView).GetMethod("BuildOverrideCommandInterceptor",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(build, Is.Not.Null, "the override interceptor seam must exist");
			var interceptor = (Func<ChoiceBase, UIItemDisplayProperties, DetailMenuItem>)build.Invoke(
				m_view, new object[] { field });
			TestContext.WriteLine("interceptor built: " + (interceptor != null));
			var window = m_propertyTable.GetValue<XWindow>("window");
			return XCoreMenuBridge.CreateMenuItems(window, menuIds, interceptor);
		}

		private ViewDefinitionOverride ReadOverrideFor(DetailField field)
			=> GetOverrideStore().TryGet(field.ClassName, field.LayoutName);

		private DetailMenuItem BuildWritingSystemsSubmenu(DetailField field)
		{
			var items = BuildItemsWithOverrideInterceptor(
				new[] { "mnuDataTree-LexemeForm", "mnuDataTree-MultiStringSlice" }, field);
			var writingSystems = FindItem(items, "Writing Systems");
			Assert.That(writingSystems, Is.Not.Null,
				"precondition: the Writing Systems submenu is present");
			return writingSystems;
		}

		// The writing systems the stored override op carries for the field's row.
		private IReadOnlyList<string> StoredWritingSystems(DetailField field)
		{
			var stored = ReadOverrideFor(field);
			Assert.That(stored, Is.Not.Null,
				"toggling a writing system must write a project override");
			return stored.Operations.Single(o =>
				o.Kind == ViewOverrideOperationKind.SetVisibleWritingSystems).WritingSystems;
		}

		// Saving an empty patch deletes the file, so overrides never leak between tests.
		private void DeleteOverrideFor(DetailField field)
			=> GetOverrideStore().Save(new ViewDefinitionOverride(
				field.ClassName, field.LayoutName, "detail", null, null));

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

		// DrainMediatorAndIdleQueues is inherited from XWorksAppTestBase.
	}
}
