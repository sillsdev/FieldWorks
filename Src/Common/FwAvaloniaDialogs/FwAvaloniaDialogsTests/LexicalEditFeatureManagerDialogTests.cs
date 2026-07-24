// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using Avalonia.Headless.NUnit;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The Avalonia "Manage Individual Features" dialog (PR #964 review follow-up; replaces the WinForms
	/// <c>LexicalEditFeatureManagerDlg</c>, whose absolute-positioned <see cref="System.Windows.Forms.FlowLayoutPanel"/>
	/// rows corrupted their own layout on a checkbox click — reported as "everything disappears" when clicking a
	/// feature). Covers grouping, search filtering, per-group select/deselect-all (visible rows only), and that
	/// edits land on the state's <see cref="FeatureOption"/> rows via their two-way checkbox binding.
	/// </summary>
	[TestFixture]
	public class LexicalEditFeatureManagerDialogTests
	{
		private static LexicalEditFeatureManagerState State(
			params (string tool, string name, string desc, string group, bool enabled)[] rows)
		{
			var groups = rows
				.GroupBy(r => r.group)
				.Select(g => new FeatureGroupOption(g.Key,
					g.Select(r => new FeatureOption(r.tool, r.name, r.desc, r.enabled)).ToList()))
				.ToList();
			return new LexicalEditFeatureManagerState { Groups = groups };
		}

		private static LexicalEditFeatureManagerState DefaultState() => State(
			("lexiconEdit", "Lexicon Edit", "The main entry-editing surface.", "Dialogs (lexical entry)", true),
			("lexiconEditPopup", "Lexicon Edit (popup)", "The popup variant of entry editing.", "Dialogs (lexical entry)", true),
			("notebookEdit", "Notebook", "Notebook (RnGenericRec) entries.", "Other record types", true),
			("posEdit", "Grammar / Part of Speech", "The Part of Speech editor.", "Other record types", false));

		private static (LexicalEditFeatureManagerDialogView view, LexicalEditFeatureManagerDialogViewModel vm) Show(
			LexicalEditFeatureManagerState state, string stageName = "FeatureManager-01-initial")
		{
			var vm = new LexicalEditFeatureManagerDialogViewModel(state);
			var view = new LexicalEditFeatureManagerDialogView { DataContext = vm };
			AvaloniaDialogTestHarness.Realize(view, 460, 440, stageName, forceRenderTick: true);
			return (view, vm);
		}

		[AvaloniaTest]
		public void Renders_GroupedRows_WithInitialCheckedStateFromTheState()
		{
			var (_, vm) = Show(DefaultState());

			Assert.That(vm.Groups.Select(g => g.Name), Is.EqualTo(new[] { "Dialogs (lexical entry)", "Other record types" }));
			var posEdit = vm.Groups.SelectMany(g => g.Features).Single(f => f.ToolName == "posEdit");
			Assert.That(posEdit.Enabled, Is.False, "posEdit was seeded as disabled");
			var lexiconEdit = vm.Groups.SelectMany(g => g.Features).Single(f => f.ToolName == "lexiconEdit");
			Assert.That(lexiconEdit.Enabled, Is.True);
		}

		[AvaloniaTest]
		public void SearchText_FiltersRowsAcrossGroups_AndHidesEmptyGroups()
		{
			var (_, vm) = Show(DefaultState());

			vm.SearchText = "notebook";

			var lexicalGroup = vm.Groups.Single(g => g.Name == "Dialogs (lexical entry)");
			var otherGroup = vm.Groups.Single(g => g.Name == "Other record types");
			Assert.That(lexicalGroup.IsVisible, Is.False, "no lexical-entry row matches 'notebook'");
			Assert.That(otherGroup.IsVisible, Is.True);
			Assert.That(otherGroup.Features.Single(f => f.ToolName == "notebookEdit").IsVisible, Is.True);
			Assert.That(otherGroup.Features.Single(f => f.ToolName == "posEdit").IsVisible, Is.False);
		}

		[AvaloniaTest]
		public void SearchText_Cleared_RestoresAllRows()
		{
			var (_, vm) = Show(DefaultState());

			vm.SearchText = "notebook";
			vm.SearchText = string.Empty;

			Assert.That(vm.Groups.SelectMany(g => g.Features).All(f => f.IsVisible), Is.True);
			Assert.That(vm.Groups.All(g => g.IsVisible), Is.True);
		}

		[AvaloniaTest]
		public void DeselectAll_OnlyTouchesCurrentlyVisibleRows()
		{
			var (_, vm) = Show(DefaultState());
			var otherGroup = vm.Groups.Single(g => g.Name == "Other record types");

			vm.SearchText = "notebook"; // hides posEdit within the group
			otherGroup.DeselectAllCommand.Execute(null);

			Assert.That(otherGroup.Features.Single(f => f.ToolName == "notebookEdit").Enabled, Is.False,
				"the visible row was deselected");
			Assert.That(otherGroup.Features.Single(f => f.ToolName == "posEdit").Enabled, Is.False,
				"posEdit started disabled and stays untouched (hidden by the filter)");
		}

		[AvaloniaTest]
		public void SelectAll_ChecksOnlyVisibleRows()
		{
			var (_, vm) = Show(DefaultState());
			var otherGroup = vm.Groups.Single(g => g.Name == "Other record types");

			vm.SearchText = "notebook"; // hides posEdit within the group
			otherGroup.SelectAllCommand.Execute(null);

			Assert.That(otherGroup.Features.Single(f => f.ToolName == "notebookEdit").Enabled, Is.True);
			Assert.That(otherGroup.Features.Single(f => f.ToolName == "posEdit").Enabled, Is.False,
				"posEdit is hidden by the filter, so Select All does not touch it");
		}

		[AvaloniaTest]
		public void OkCommand_LeavesEditsOnTheStateRows_ViaTheirTwoWayCheckboxBinding()
		{
			var state = DefaultState();
			var (_, vm) = Show(state);

			vm.Groups.SelectMany(g => g.Features).Single(f => f.ToolName == "lexiconEdit").Enabled = false;
			vm.OkCommand.Execute(null);

			Assert.That(vm.Accepted, Is.True);
			Assert.That(state.Groups.SelectMany(g => g.Features).Single(f => f.ToolName == "lexiconEdit").Enabled, Is.False);
			Assert.That(state.Groups.SelectMany(g => g.Features).Single(f => f.ToolName == "posEdit").Enabled, Is.False);
		}

		[AvaloniaTest]
		public void CancelCommand_SetsAcceptedFalse()
		{
			var (_, vm) = Show(DefaultState());
			vm.CancelCommand.Execute(null);
			Assert.That(vm.Accepted, Is.False);
		}

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.FeatureManagerTitle, Is.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.FeatureManagerSearchWatermark, Is.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.FeatureManagerSelectAll, Is.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.FeatureManagerDeselectAll, Is.Not.Empty);
		}

		// --- BuildGroups / ExtractDisabledToolNames: the catalog<->CSV bridging Show() itself does, extracted
		// so it is testable without a real modal window (mirrors AvaloniaDialogHost.ApplySizing/ResolveEffectiveOwner).
		// Uses the REAL LexicalEditFeatureCatalog, unlike the tests above (which build a synthetic State() to
		// isolate the ViewModel), so this is the one place Show()'s actual product wiring is exercised end-to-end. ---

		[Test]
		public void BuildGroups_NullFeatures_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => LexicalEditFeatureManagerDialog.BuildGroups(null, null));
		}

		[Test]
		public void BuildGroups_NullDisabledNames_EverythingEnabled()
		{
			var groups = LexicalEditFeatureManagerDialog.BuildGroups(LexicalEditFeatureCatalog.Features, null);

			Assert.That(groups.SelectMany(g => g.Features).All(f => f.Enabled), Is.True,
				"the master UIMode=New switch defaults every catalog tool on");
		}

		[Test]
		public void BuildGroups_GroupsMatchTheCatalogsDeclaredGroupNames_InFirstSeenOrder()
		{
			var groups = LexicalEditFeatureManagerDialog.BuildGroups(LexicalEditFeatureCatalog.Features, null);

			var expectedGroupOrder = LexicalEditFeatureCatalog.Features
				.Select(f => f.GroupName)
				.Distinct()
				.ToArray();
			Assert.That(groups.Select(g => g.Name), Is.EqualTo(expectedGroupOrder));

			// Every catalog tool must land in exactly one row, under its own declared group.
			var allRows = groups.SelectMany(g => g.Features).ToList();
			Assert.That(allRows.Select(f => f.ToolName), Is.EquivalentTo(LexicalEditFeatureCatalog.ToolNames));
		}

		[Test]
		public void BuildGroups_DisablesExactlyTheNamedTools_CaseInsensitively()
		{
			var groups = LexicalEditFeatureManagerDialog.BuildGroups(
				LexicalEditFeatureCatalog.Features, new[] { "POSEDIT" }); // catalog declares it as "posEdit"

			var rows = groups.SelectMany(g => g.Features).ToList();
			Assert.That(rows.Single(f => f.ToolName == "posEdit").Enabled, Is.False);
			Assert.That(rows.Where(f => f.ToolName != "posEdit").All(f => f.Enabled), Is.True);
		}

		[Test]
		public void BuildGroups_UnknownDisabledToolName_SeedsNoPhantomRow()
		{
			// A stale/renamed/removed tool name in the persisted CSV must not create a row that doesn't
			// correspond to any real catalog entry.
			var groups = LexicalEditFeatureManagerDialog.BuildGroups(
				LexicalEditFeatureCatalog.Features, new[] { "someRetiredToolFromAnOldVersion" });

			var rows = groups.SelectMany(g => g.Features).ToList();
			Assert.That(rows.Select(f => f.ToolName), Is.EquivalentTo(LexicalEditFeatureCatalog.ToolNames));
			Assert.That(rows.All(f => f.Enabled), Is.True, "the unknown name matched nothing, so nothing is disabled");
		}

		[Test]
		public void ExtractDisabledToolNames_NullOrEmptyGroups_ReturnsEmpty()
		{
			Assert.That(LexicalEditFeatureManagerDialog.ExtractDisabledToolNames(null), Is.Empty);
			Assert.That(LexicalEditFeatureManagerDialog.ExtractDisabledToolNames(Array.Empty<FeatureGroupOption>()), Is.Empty);
		}

		[Test]
		public void ExtractDisabledToolNames_ReturnsOnlyUncheckedRows_InGroupAndCatalogOrder()
		{
			var groups = LexicalEditFeatureManagerDialog.BuildGroups(
				LexicalEditFeatureCatalog.Features, new[] { "posEdit", "lexiconEdit" });

			var extracted = LexicalEditFeatureManagerDialog.ExtractDisabledToolNames(groups);

			// Catalog order is lexiconEdit, lexiconEditPopup, notebookEdit, posEdit -- so the two disabled
			// names must come back in that order, not the order they were passed in to BuildGroups.
			Assert.That(extracted, Is.EqualTo(new[] { "lexiconEdit", "posEdit" }));
		}

		[Test]
		public void BuildGroups_ThenExtractDisabledToolNames_RoundTripsTheDisabledSet_WhenUntouched()
		{
			// The full bridge Show() relies on: seed from a CSV-derived set, then (without any user edits)
			// extract back out. This is the "reopen Options and Manage Features still reflects what you saved"
			// guarantee for every catalog tool.
			var originalDisabled = LexicalEditSurfaceResolver.ParseDisabledTools("lexiconEditPopup,notebookEdit");

			var groups = LexicalEditFeatureManagerDialog.BuildGroups(LexicalEditFeatureCatalog.Features, originalDisabled);
			var roundTripped = LexicalEditFeatureManagerDialog.ExtractDisabledToolNames(groups);

			Assert.That(roundTripped, Is.EquivalentTo(originalDisabled));
		}
	}
}
