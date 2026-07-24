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
	/// The hybrid companion-strip MECHANISM (winforms-free-lexeme-editor.md D1's second
	/// resolution slot): the composer carries legacy custom-editor identities (class/assembly,
	/// keyed by the placeholder row's StableId) instead of dropping them, designated-class
	/// selection picks slices for WinForms promotion, and the model filter removes exactly the
	/// promoted rows. The designated set is EMPTY — no shipped class needs WinForms promotion —
	/// so the mechanism is exercised here with an empty plugin
	/// registry (to reach the placeholder path at all) and a fake designated class; the strip
	/// itself stays hidden in the product. The mechanism remains the documented coexistence route for
	/// future tools' WinForms-only custom slices (xml-retirement-blockers.md B11).
	/// </summary>
	[TestFixture]
	public class FullEntryRegionMessagesCompanionTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
			});
		}

		/// <summary>An empty registry keeps every custom class in the placeholder/companion path.</summary>
		private ComposedEntryRegion ComposeWithoutPlugins()
			=> FullEntryRegionComposer.Compose(m_entry, Cache, plugins: new RegionEditorPluginRegistry());

		[Test]
		public void Compose_WithoutAPluginClaim_CarriesTheMessagesSliceIdentity_KeyedToItsPlaceholderRow()
		{
			var composed = ComposeWithoutPlugins();
			Assert.That(composed, Is.Not.Null, "the shipped layouts must compose");

			var messages = composed.CustomEditorFields
				.Where(f => f.ClassName == AvaloniaCompanionSlices.MessageSliceClassName)
				.ToList();
			Assert.That(messages.Count, Is.EqualTo(1),
				"LexEntry/Normal reaches the Messages part (LexEntry-Detail-Messages) exactly once");

			var binding = messages[0];
			Assert.That(binding.AssemblyPath, Is.EqualTo("LexEdDll.dll"),
				"the layout's assemblyPath rides along for DynamicLoader");
			Assert.That(binding.ObjectHvo, Is.EqualTo(m_entry.Hvo),
				"the Messages slice binds the entry itself (field='Self')");

			// StableId coordination: the binding keys exactly one row in the composed model — the
			// placeholder rendering the companion strip replaces (the slice's field='Self' resolves
			// to a reference-atomic flid, so the composer renders a read-only row, never an editor).
			var rows = composed.Model.Fields.Where(f => f.StableId == binding.FieldStableId).ToList();
			Assert.That(rows.Count, Is.EqualTo(1), "the binding's StableId addresses exactly one row");
			Assert.That(rows[0].IsEditable, Is.False, "the placeholder row is never an editor");
			Assert.That(rows[0].EditorClassification, Is.EqualTo(EditorClassification.Dynamic),
				"editor='Custom' classifies as a dynamically loaded editor");
		}

		[Test]
		public void Compose_WithTheDefaultRegistry_TheMessagesSliceIsUnclaimed_NotCompanionMaterial()
		{
			// The Chorus notes bar is not migrated in this PR, so no plugin claims MessageSlice: the
			// Messages node stays in the composer's placeholder path (CustomEditorFields), same as any
			// other unclaimed dynamically loaded slice. With the companion-designated set also empty,
			// the companion strip stays hidden either way.
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);

			Assert.That(composed.CustomEditorFields.Select(f => f.ClassName),
				Has.Member(AvaloniaCompanionSlices.MessageSliceClassName),
				"with no notes-bar plugin, the Messages slice is unclaimed and keeps its placeholder identity");
			var custom = composed.Model.Fields.Where(f => f.Kind == RegionFieldKind.Custom
				&& f.Label == "Messages").ToList();
			Assert.That(custom, Is.Empty,
				"an unclaimed class never composes as a plugin's Custom row (D1 resolution order)");

			Assert.That(AvaloniaCompanionSlices.SelectPromotions(composed.CustomEditorFields), Is.Empty,
				"nothing promotes: the designated set is empty, so RecordEditView's "
				+ "companion strip never shows");
		}

		[Test]
		public void SelectPromotions_PicksOnlyDesignatedCompanionClasses()
		{
			// The selection mechanism, exercised with a fake designated class (the product set is
			// empty since wave 2; the mechanism remains for future tools).
			const string fakeDesignated = "Fake.Tool.WinFormsOnlySlice";
			var designated = new ComposedCustomEditorField("id1", fakeDesignated, "FakeDll.dll", "Fake", 17);
			var other = new ComposedCustomEditorField("id2",
				"SIL.FieldWorks.XWorks.LexEd.GhostLexRefSlice", "LexEdDll.dll", "Components", 17);

			var promotions = AvaloniaCompanionSlices.SelectPromotions(
				new[] { other, designated, null },
				new HashSet<string> { fakeDesignated });

			Assert.That(promotions.Select(p => p.FieldStableId), Is.EqualTo(new[] { "id1" }),
				"only the designated companion classes promote; other dynamic editors keep their unsupported row");
			Assert.That(AvaloniaCompanionSlices.SelectPromotions(null), Is.Empty);
			Assert.That(AvaloniaCompanionSlices.SelectPromotions(new[] { designated, other }), Is.Empty,
				"the product designated set is empty since wave 2 (D2)");
		}

		[Test]
		public void RemovePromotedFields_RemovesExactlyThePromotedRows()
		{
			// Promotion removal mechanism over the real composed entry: pick the Messages
			// placeholder's StableId directly (composing without plugins), as a stand-in for a
			// future designated class.
			var composed = ComposeWithoutPlugins();
			var binding = composed.CustomEditorFields
				.Single(f => f.ClassName == AvaloniaCompanionSlices.MessageSliceClassName);
			var promotedIds = new[] { binding.FieldStableId };

			var filtered = AvaloniaCompanionSlices.RemovePromotedFields(composed.Model, promotedIds);

			Assert.That(filtered.Fields.Count, Is.EqualTo(composed.Model.Fields.Count - 1),
				"exactly the promoted row disappears; everything else survives");
			Assert.That(filtered.Fields.Any(f => f.StableId == binding.FieldStableId), Is.False,
				"the placeholder row for the promoted slice is gone");
			Assert.That(filtered.Fields.Select(f => f.StableId),
				Is.EqualTo(composed.Model.Fields.Where(f => f.StableId != binding.FieldStableId)
					.Select(f => f.StableId)),
				"row order is preserved");
			Assert.That(filtered.ClassName, Is.EqualTo(composed.Model.ClassName));
			Assert.That(filtered.LayoutName, Is.EqualTo(composed.Model.LayoutName));
			Assert.That(filtered.Diagnostics, Is.SameAs(composed.Model.Diagnostics),
				"diagnostics ride the filtered model unchanged");
		}

		[Test]
		public void RemovePromotedFields_WithNothingToRemove_ReturnsTheSameModelInstance()
		{
			var composed = ComposeWithoutPlugins();

			Assert.That(AvaloniaCompanionSlices.RemovePromotedFields(composed.Model, null),
				Is.SameAs(composed.Model));
			Assert.That(AvaloniaCompanionSlices.RemovePromotedFields(composed.Model,
					new[] { "no-such-row" }),
				Is.SameAs(composed.Model), "unknown ids must not rebuild the model");
		}
	}
}
