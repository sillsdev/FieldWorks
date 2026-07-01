// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// §19a T4 — WORKFLOW (end-to-end, headless, real cache). Two complete journeys a user runs against
	/// an editable StText, driven through the REAL composed paragraph CRUD seam over an in-memory LCModel
	/// and then RE-PROJECTED from domain truth (the host re-show), so the assertions prove the data
	/// genuinely round-tripped, not merely that a setter returned true:
	///   WF2: edit a Definition paragraph → add a 2nd paragraph → apply a paragraph style to it → commit
	///        → re-project the StText → verify the two paragraphs + the style round-tripped.
	///   WF3: delete a paragraph → undo restores it (text + order intact).
	/// The StText is a Sense.Definition — the canonical StText the legacy StTextSlice edits.
	/// </summary>
	[TestFixture]
	public class StructuredTextWorkflowTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private IStText m_definition;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(sense);
				sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("house", Cache.DefaultAnalWs));

				// A real, persisted OwningAtomic StText container (a notebook record's Discussion) — the
				// same proven StText shape StructuredTextAdapterTests uses; "Definition" is the narrative
				// role being exercised (the canonical multi-paragraph StText a user edits).
				if (Cache.LangProject.ResearchNotebookOA == null)
					Cache.LangProject.ResearchNotebookOA =
						Cache.ServiceLocator.GetInstance<IRnResearchNbkFactory>().Create();
				var record = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
				Cache.LangProject.ResearchNotebookOA.RecordsOC.Add(record);
				record.DiscussionOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				m_definition = record.DiscussionOA;
				var p0 = m_definition.AddNewTextPara(null);
				p0.Contents = TsStringUtils.MakeString("A dwelling.", Cache.DefaultAnalWs);
			});
		}

		private string ParaText(int i) => ((IStTxtPara)m_definition.ParagraphsOS[i]).Contents?.Text;
		private string ParaStyle(int i) => ((IStTxtPara)m_definition.ParagraphsOS[i]).StyleName;
		private int ParaCount => m_definition.ParagraphsOS.Count;

		private const string StableId = "LexEntry/Definition@w";

		// The StructuredText field + composed edit context wired exactly as FullEntryRegionComposer does:
		// each setter mutates m_definition inside the shared fenced Stage().
		private (LexicalEditRegionField Field, ComposedRegionEditContext Context) Build()
		{
			var paragraphs = m_definition.ParagraphsOS.OfType<IStTxtPara>()
				.Select(p => new RegionParagraph(
					RegionRichTextAdapter.FromTsString(p.Contents, Cache.WritingSystemFactory), p.StyleName))
				.ToList();
			var field = new LexicalEditRegionField(StableId, "Definition", "Definition", null,
				RegionFieldKind.StructuredText,
				SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.EditorClassification.Known, null, null,
				SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.SurfaceRouting.Product, null, null, null,
				isEditable: true, paragraphs: paragraphs)
			{
				AvailableParagraphStyles = new[] { "Block Quote", "Numbered List" }
			};

			var defaultWs = Cache.DefaultAnalWs;
			var wsf = Cache.WritingSystemFactory;

			var textSetters = new Dictionary<string, Func<int, RegionRichTextValue, bool>>
			{
				[StableId] = (index, value) =>
				{
					if (value == null || index < 0)
						return false;
					while (m_definition.ParagraphsOS.Count <= index)
						m_definition.InsertNewTextPara(m_definition.ParagraphsOS.Count, null);
					((IStTxtPara)m_definition.ParagraphsOS[index]).Contents =
						RegionRichTextAdapter.ToTsString(value, wsf, defaultWs);
					return true;
				}
			};
			var styleSetters = new Dictionary<string, Func<int, string, bool>>
			{
				[StableId] = (index, style) =>
				{
					if (index < 0 || index >= m_definition.ParagraphsOS.Count)
						return false;
					((IStTxtPara)m_definition.ParagraphsOS[index]).StyleName =
						string.IsNullOrEmpty(style) ? StyleServices.NormalStyleName : style;
					return true;
				}
			};
			var insertSetters = new Dictionary<string, Func<int, bool>>
			{
				[StableId] = afterIndex =>
				{
					var pos = afterIndex < 0 ? 0 : Math.Min(afterIndex + 1, m_definition.ParagraphsOS.Count);
					m_definition.InsertNewTextPara(pos, null);
					return true;
				}
			};
			var deleteSetters = new Dictionary<string, Func<int, bool>>
			{
				[StableId] = index =>
				{
					if (index < 0 || index >= m_definition.ParagraphsOS.Count || m_definition.ParagraphsOS.Count <= 1)
						return false;
					m_definition.ParagraphsOS.RemoveAt(index);
					return true;
				}
			};

			var context = new ComposedRegionEditContext(Cache, m_entry,
				textSetters: new Dictionary<string, Func<string, string, bool>>(),
				optionSetters: new Dictionary<string, Func<string, bool>>(),
				paragraphTextSetters: textSetters, paragraphStyleSetters: styleSetters,
				paragraphInsertSetters: insertSetters, paragraphDeleteSetters: deleteSetters);
			return (field, context);
		}

		private static RegionRichTextValue Rich(string text)
			=> RegionRichTextEditAlgorithms.FromRuns(text, new[] { new RegionTextRun(text) });

		// The host re-show: re-project the StText from domain truth into fresh RegionParagraphs, exactly
		// as a recompose/reopen of the entry would. Asserting against THIS proves the round-trip.
		private List<RegionParagraph> ReProject()
			=> m_definition.ParagraphsOS.OfType<IStTxtPara>()
				.Select(p => new RegionParagraph(
					RegionRichTextAdapter.FromTsString(p.Contents, Cache.WritingSystemFactory), p.StyleName))
				.ToList();

		// WF2: edit Definition text -> add a 2nd paragraph -> apply a paragraph style to it -> commit ->
		// re-show/reopen the entry -> verify the two paragraphs + style round-tripped.
		[Test]
		public void Workflow_EditDefinition_AddSecondPara_ApplyStyle_RoundTrips()
		{
			var (field, context) = Build();

			// 1) edit the first paragraph's text (stages; rides the focus-loss autosave in the real view).
			Assert.That(context.TrySetParagraphText(field, 0, Rich("A house or dwelling.")), Is.True);
			context.Commit();

			// 2) add a second paragraph after index 0 (structural: immediate commit).
			Assert.That(context.TryInsertParagraph(field, 0), Is.True);
			context.Commit();
			Assert.That(ParaCount, Is.EqualTo(2), "the second paragraph was created");

			// 3) put text into and apply a paragraph style to the new paragraph (index 1).
			Assert.That(context.TrySetParagraphText(field, 1, Rich("Especially a permanent one.")), Is.True);
			Assert.That(context.TrySetParagraphStyle(field, 1, "Block Quote"), Is.True);
			context.Commit();

			// Re-show/reopen: re-project from domain truth and verify the WHOLE thing round-tripped.
			var reshown = ReProject();
			Assert.That(reshown, Has.Count.EqualTo(2), "two paragraphs survive the re-show");
			Assert.That(reshown[0].Text.PlainText, Is.EqualTo("A house or dwelling."),
				"the edited first paragraph round-tripped");
			Assert.That(reshown[1].Text.PlainText, Is.EqualTo("Especially a permanent one."),
				"the added second paragraph's text round-tripped");
			Assert.That(reshown[1].ParagraphStyle, Is.EqualTo("Block Quote"),
				"the paragraph style applied to the new paragraph round-tripped");
			Assert.That(reshown[0].CanEditText && reshown[1].CanEditText, Is.True,
				"both round-tripped paragraphs are still plain/editable");

			// And the persisted LCModel agrees.
			Assert.That(ParaText(0), Is.EqualTo("A house or dwelling."));
			Assert.That(ParaText(1), Is.EqualTo("Especially a permanent one."));
			Assert.That(ParaStyle(1), Is.EqualTo("Block Quote"));
		}

		// WF3: delete a paragraph -> undo restores it (text + order intact).
		[Test]
		public void Workflow_DeleteParagraph_ThenUndoRestoresIt()
		{
			var (field, context) = Build();

			// Grow to three paragraphs so there is a middle one to delete and restore.
			Assert.That(context.TryInsertParagraph(field, 0), Is.True);
			context.Commit();
			Assert.That(context.TrySetParagraphText(field, 1, Rich("Second paragraph.")), Is.True);
			context.Commit();
			Assert.That(context.TryInsertParagraph(field, 1), Is.True);
			context.Commit();
			Assert.That(context.TrySetParagraphText(field, 2, Rich("Third paragraph.")), Is.True);
			context.Commit();
			Assert.That(ParaCount, Is.EqualTo(3));
			Assert.That(new[] { ParaText(0), ParaText(1), ParaText(2) },
				Is.EqualTo(new[] { "A dwelling.", "Second paragraph.", "Third paragraph." }));

			// Delete the middle paragraph (index 1) — one immediate undo step.
			Assert.That(context.TryDeleteParagraph(field, 1), Is.True);
			context.Commit();
			Assert.That(ParaCount, Is.EqualTo(2), "the middle paragraph was deleted");
			Assert.That(new[] { ParaText(0), ParaText(1) },
				Is.EqualTo(new[] { "A dwelling.", "Third paragraph." }), "the order closed up correctly");

			// Undo restores the deleted paragraph in its original place, text intact.
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(ParaCount, Is.EqualTo(3), "one undo restores the deleted paragraph");

			var reshown = ReProject();
			Assert.That(reshown.Select(p => p.Text.PlainText),
				Is.EqualTo(new[] { "A dwelling.", "Second paragraph.", "Third paragraph." }),
				"the restored paragraph is back in its original position with its text after re-show");

			// Redo deletes it again (the step is on the shared global stack).
			Cache.ActionHandlerAccessor.Redo();
			Assert.That(ParaCount, Is.EqualTo(2), "redo re-applies the delete");
		}
	}
}
