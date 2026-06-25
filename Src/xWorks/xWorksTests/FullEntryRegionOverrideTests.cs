// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// advanced-entry-view: end-to-end coverage that the per-field gear-menu commands actually change
	/// what the Avalonia surface composes, BY GOING THROUGH the override layer the menu writes — not the
	/// legacy Inventory store. Visibility overrides hide/show rows under the same showHidden semantics
	/// legacy slices use; reorder overrides move sibling rows; both survive a recompose; and applying an
	/// override never poisons the process-wide compiled-model cache (a compose without the patch is
	/// unaffected). The composer is the real product path; the resolver here stands in for the file store
	/// (which has its own round-trip tests in FwAvaloniaTests).
	/// </summary>
	[TestFixture]
	public class FullEntryRegionOverrideTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private IMoStemAllomorph m_morph;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				m_morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = m_morph;
				m_morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(sense);
				sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("house", Cache.DefaultAnalWs));
			});
		}

		// An in-memory resolver standing in for the file-backed ViewDefinitionOverrideStore.
		private static ViewDefinitionOverrideResolver Resolver(params ViewDefinitionOverride[] patches)
		{
			var byKey = patches.ToDictionary(p => (p.ClassName, p.LayoutName));
			return (cls, layout) => byKey.TryGetValue((cls, layout), out var patch) ? patch : null;
		}

		private static ViewDefinitionOverride EntryPatch(params ViewOverrideOperation[] ops)
			=> new ViewDefinitionOverride("LexEntry", "Normal", "detail", ops, null);

		// The template (override-key) StableId of an entry-level field: strip the runtime "@{hvo}" suffix.
		private string EntryFieldTemplateId(string field)
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var row = composed.Model.Fields.First(f => f.Field == field && f.ClassName == "LexEntry");
			return ViewDefinitionOverrideEditor.StripRuntimeSuffix(row.StableId);
		}

		[Test]
		public void Compose_StampsClassAndLayoutOnEntryFields()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);

			var entryRows = composed.Model.Fields.Where(f => f.ObjectHvo == m_entry.Hvo).ToList();
			Assert.That(entryRows, Is.Not.Empty, "the entry must contribute at least one row");
			Assert.That(entryRows, Has.All.Property("ClassName").EqualTo("LexEntry"));
			Assert.That(entryRows, Has.All.Property("LayoutName").EqualTo("Normal"));
		}

		[Test]
		public void Compose_StampsDescendedObjectsLayoutClass_NotTheEntrys()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true);

			// Sense rows are projected from the sense's own compiled layout, so they must carry LexSense.
			var senseRows = composed.Model.Fields
				.Where(f => f.ClassName == "LexSense" && f.LayoutName == "Normal").ToList();
			Assert.That(senseRows, Is.Not.Empty,
				"descended sense rows must be stamped with their own layout class, not the entry's");
		}

		[Test]
		public void Visibility_Never_HidesRow_UnlessShowHidden()
		{
			// Pick a visible entry field, force it to "Normally hidden".
			var baseline = FullEntryRegionComposer.Compose(m_entry, Cache);
			var victim = baseline.Model.Fields.First(f => f.ClassName == "LexEntry"
				&& f.Kind == RegionFieldKind.Text);
			var templateId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(victim.StableId);
			var resolver = Resolver(EntryPatch(new ViewOverrideOperation(
				ViewOverrideOperationKind.SetVisibility, templateId, visibility: ViewVisibility.Never)));

			var hidden = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: false,
				overrides: resolver);
			Assert.That(hidden.Model.Fields.Any(f => f.StableId == victim.StableId), Is.False,
				"a Never field is hidden when showHidden is off");

			var shown = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true,
				overrides: resolver);
			Assert.That(shown.Model.Fields.Any(f => f.StableId == victim.StableId), Is.True,
				"a Never field reappears when showHidden is on");
		}

		[Test]
		public void Visibility_IfData_HidesWhenEmpty_ShowsWhenNonEmpty()
		{
			// CitationForm is empty on this entry; force IfData and confirm the empty row hides, then
			// give it data and confirm it shows.
			var baseline = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true);
			var citation = baseline.Model.Fields.FirstOrDefault(f => f.Field == "CitationForm"
				&& f.ClassName == "LexEntry");
			Assert.That(citation, Is.Not.Null, "the entry layout must offer a CitationForm row");
			var templateId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(citation.StableId);
			var resolver = Resolver(EntryPatch(new ViewOverrideOperation(
				ViewOverrideOperationKind.SetVisibility, templateId, visibility: ViewVisibility.IfData)));

			var emptyHidden = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: false,
				overrides: resolver);
			Assert.That(emptyHidden.Model.Fields.Any(f => f.Field == "CitationForm"), Is.False,
				"an empty IfData field hides when showHidden is off");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_entry.CitationForm.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("casita", Cache.DefaultVernWs)));

			var nowShown = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: false,
				overrides: resolver);
			Assert.That(nowShown.Model.Fields.Any(f => f.Field == "CitationForm"), Is.True,
				"a non-empty IfData field shows even when showHidden is off");
		}

		[Test]
		public void Reorder_SwapsTwoSiblingRows_AndSurvivesRecompose()
		{
			// Find two entry-level sibling fields under a shared parent (via the SAME LocateTarget the
			// menu uses), then reorder them and assert the row order swapped in the composed model.
			var model = FullEntryRegionComposer.CompileForObject(Cache, m_entry, "Normal");
			var siblings = FindSiblingPair(model);
			Assert.That(siblings, Is.Not.Null, "the entry layout must have a parent with two locatable fields");

			var (parentId, firstId, secondId, order) = siblings.Value;
			var moved = order.ToList();
			var idx = moved.IndexOf(secondId);
			moved[idx] = moved[idx - 1];
			moved[idx - 1] = secondId; // move 'second' up one
			var resolver = Resolver(EntryPatch(new ViewOverrideOperation(
				ViewOverrideOperationKind.ReorderChildren, parentId, childOrder: moved)));

			var reordered = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true,
				overrides: resolver);
			var firstPos = RowPosition(reordered.Model, firstId);
			var secondPos = RowPosition(reordered.Model, secondId);
			Assert.That(secondPos, Is.LessThan(firstPos),
				"the reorder override must move the second sibling's row ahead of the first");

			// Survives a fresh recompose with the same resolver (the override is the source of truth).
			var again = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true,
				overrides: resolver);
			Assert.That(RowPosition(again.Model, secondId), Is.LessThan(RowPosition(again.Model, firstId)));
		}

		[Test]
		public void Override_DoesNotPoisonProcessWideCompiledCache()
		{
			var victimId = EntryFieldTemplateId(FullEntryRegionComposer.Compose(m_entry, Cache)
				.Model.Fields.First(f => f.ClassName == "LexEntry" && f.Kind == RegionFieldKind.Text).Field);
			var resolver = Resolver(EntryPatch(new ViewOverrideOperation(
				ViewOverrideOperationKind.SetVisibility, victimId, visibility: ViewVisibility.Never)));

			// Compose WITH the override (mutates nothing but the returned copy).
			FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: false, overrides: resolver);

			// A subsequent compose WITHOUT the override must see the shipped definition unchanged: the
			// cached model was never patched in place.
			var clean = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: false);
			Assert.That(clean.Model.Fields.Any(f => f.StableId.StartsWith(victimId)), Is.True,
				"composing without the patch must see the shipped (unhidden) field — the cache stayed clean");
		}

		[Test]
		public void Override_UnknownStableId_IsNoOp_NotACrash()
		{
			var resolver = Resolver(EntryPatch(new ViewOverrideOperation(
				ViewOverrideOperationKind.SetVisibility, "/#does/#not/#exist", visibility: ViewVisibility.Never)));

			var baseline = FullEntryRegionComposer.Compose(m_entry, Cache);
			var withStale = FullEntryRegionComposer.Compose(m_entry, Cache, overrides: resolver);

			Assert.That(withStale.Model.Fields.Count, Is.EqualTo(baseline.Model.Fields.Count),
				"a stale/unknown override target changes nothing");
			Assert.That(withStale.Model.Diagnostics.Any(d => d.Code == "override-stale-target"), Is.True,
				"the stale target is reported as a diagnostic, not silently dropped");
		}

		private static int RowPosition(LexicalEditRegionModel model, string templateId)
		{
			for (var i = 0; i < model.Fields.Count; i++)
			{
				if (ViewDefinitionOverrideEditor.StripRuntimeSuffix(model.Fields[i].StableId) == templateId)
					return i;
			}

			return -1;
		}

		// Finds a parent node in the compiled model with at least two field children both locatable by id.
		private static (string Parent, string First, string Second, IReadOnlyList<string> Order)?
			FindSiblingPair(ViewDefinitionModel model)
		{
			(string, string, string, IReadOnlyList<string>)? result = null;
			void Visit(ViewNode parent)
			{
				if (result != null) return;
				var fieldChildren = parent.Children.Where(c => c.Kind == ViewNodeKind.Field).ToList();
				if (fieldChildren.Count >= 2)
				{
					result = (parent.StableId, fieldChildren[0].StableId, fieldChildren[1].StableId,
						parent.Children.Select(c => c.StableId).ToList());
					return;
				}

				foreach (var child in parent.Children)
					Visit(child);
			}

			foreach (var root in model.Roots)
				Visit(root);
			return result;
		}
	}
}
