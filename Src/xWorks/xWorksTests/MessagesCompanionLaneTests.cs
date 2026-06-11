// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
	/// The hybrid companion lane for the LexEntry "Messages" slice (Chorus Send/Receive notes bar):
	/// the composer carries the legacy custom-editor identity (class/assembly, keyed by the
	/// placeholder row's StableId) instead of dropping it, the designated-class selection picks the
	/// Messages slice for promotion, and the model filter removes exactly the promoted rows so the
	/// Avalonia region no longer shows the grey unsupported placeholder. The WinForms/Chorus half
	/// (PocWinFormsHostControl.SetCompanionControls + the real MessageSlice) is manual-verification
	/// territory — headless UI for the Chorus notes bar is impractical.
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

		[Test]
		public void Compose_CarriesTheMessagesSliceCustomEditorIdentity_KeyedToItsPlaceholderRow()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
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
			// placeholder rendering the companion lane replaces (the slice's field='Self' resolves
			// to a reference-atomic flid, so the composer renders a read-only row, never an editor).
			var rows = composed.Model.Fields.Where(f => f.StableId == binding.FieldStableId).ToList();
			Assert.That(rows.Count, Is.EqualTo(1), "the binding's StableId addresses exactly one row");
			Assert.That(rows[0].IsEditable, Is.False, "the placeholder row is never an editor");
			Assert.That(rows[0].EditorClassification, Is.EqualTo(EditorClassification.Dynamic),
				"editor='Custom' classifies as a dynamically loaded editor");
		}

		[Test]
		public void SelectPromotions_PicksOnlyDesignatedCompanionClasses()
		{
			var messages = new ComposedCustomEditorField("id1",
				AvaloniaCompanionSlices.MessageSliceClassName, "LexEdDll.dll", "Messages", 17);
			var other = new ComposedCustomEditorField("id2",
				"SIL.FieldWorks.XWorks.LexEd.GhostLexRefSlice", "LexEdDll.dll", "Components", 17);

			var promotions = AvaloniaCompanionSlices.SelectPromotions(
				new[] { other, messages, null });

			Assert.That(promotions.Select(p => p.FieldStableId), Is.EqualTo(new[] { "id1" }),
				"only the designated companion classes promote; other dynamic editors keep their unsupported row");
			Assert.That(AvaloniaCompanionSlices.SelectPromotions(null), Is.Empty);
		}

		[Test]
		public void SelectPromotions_OnTheRealComposedEntry_PromotesExactlyTheMessagesSlice()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var promotions = AvaloniaCompanionSlices.SelectPromotions(composed.CustomEditorFields);

			Assert.That(promotions.Select(p => p.ClassName),
				Is.EqualTo(new[] { AvaloniaCompanionSlices.MessageSliceClassName }));
		}

		[Test]
		public void RemovePromotedFields_RemovesExactlyThePromotedRows()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var promotions = AvaloniaCompanionSlices.SelectPromotions(composed.CustomEditorFields);
			var promotedIds = promotions.Select(p => p.FieldStableId).ToList();

			var filtered = AvaloniaCompanionSlices.RemovePromotedFields(composed.Model, promotedIds);

			Assert.That(filtered.Fields.Count, Is.EqualTo(composed.Model.Fields.Count - promotedIds.Count),
				"exactly the promoted rows disappear; everything else survives");
			Assert.That(filtered.Fields.Any(f => promotedIds.Contains(f.StableId)), Is.False,
				"the grey unsupported row for the promoted slice is gone");
			Assert.That(filtered.Fields.Select(f => f.StableId),
				Is.EqualTo(composed.Model.Fields.Where(f => !promotedIds.Contains(f.StableId))
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
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);

			Assert.That(AvaloniaCompanionSlices.RemovePromotedFields(composed.Model, null),
				Is.SameAs(composed.Model));
			Assert.That(AvaloniaCompanionSlices.RemovePromotedFields(composed.Model,
					new[] { "no-such-row" }),
				Is.SameAs(composed.Model), "unknown ids must not rebuild the model");
		}
	}
}
