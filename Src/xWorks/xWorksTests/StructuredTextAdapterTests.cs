// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// §19a — the LCModel-aware StText edit-context adapter, exercised against a REAL in-memory cache.
	/// An StText field becomes an editable <see cref="RegionFieldKind.StructuredText"/> row whose
	/// paragraph CRUD (text / style / insert / delete) mutates the LCModel StText inside ONE fenced
	/// <see cref="LcmRegionEditSession"/> — one step on the global undo stack legacy surfaces share, the
	/// same undo-granularity rule the rest of the region follows. These tests build the composed
	/// edit-context the way <see cref="FullEntryRegionComposer"/> does (the same
	/// <see cref="ComposedRegionEditContext"/> + paragraph setters), so they cover the real production
	/// write path, not a stand-in. An ORC/lossy paragraph stays read-only/preserved (§19c.3).
	/// </summary>
	[TestFixture]
	public class StructuredTextAdapterTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private IStText m_stText;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));

				// An OWNED StText with two paragraphs — the structured-text the StTextSlice edits. Owned
				// under a notebook record's Discussion (a real OwningAtomic-StText field) so the StText is a
				// valid, persisted object with the same lifetime an lexeme StText custom field would have.
				if (Cache.LangProject.ResearchNotebookOA == null)
					Cache.LangProject.ResearchNotebookOA =
						Cache.ServiceLocator.GetInstance<IRnResearchNbkFactory>().Create();
				var record = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
				Cache.LangProject.ResearchNotebookOA.RecordsOC.Add(record);
				record.DiscussionOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				m_stText = record.DiscussionOA;
				var p0 = m_stText.AddNewTextPara(null);
				p0.Contents = TsStringUtils.MakeString("First paragraph.", Cache.DefaultAnalWs);
				var p1 = m_stText.AddNewTextPara(null);
				p1.Contents = TsStringUtils.MakeString("Second paragraph.", Cache.DefaultAnalWs);
			});
		}

		private string ParaText(int i) => ((IStTxtPara)m_stText.ParagraphsOS[i]).Contents?.Text;
		private string ParaStyle(int i) => ((IStTxtPara)m_stText.ParagraphsOS[i]).StyleName;
		private int ParaCount => m_stText.ParagraphsOS.Count;

		// A StructuredText region field + a composed edit context whose paragraph setters mutate m_stText
		// exactly as FullEntryRegionComposer.AddStructuredText wires them. This is the production seam:
		// ComposedRegionEditContext routes each gesture through the shared fenced Stage().
		private (LexicalEditRegionField Field, ComposedRegionEditContext Context) Build()
		{
			const string stableId = "LexEntry/Bibliography@" + "x";
			var field = new LexicalEditRegionField(stableId, "Discussion", "Discussion", null,
				RegionFieldKind.StructuredText,
				SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.EditorClassification.Known, null, null,
				SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.SurfaceRouting.Product, null, null, null,
				isEditable: true);

			var defaultWs = Cache.DefaultAnalWs;
			var wsf = Cache.WritingSystemFactory;

			var textSetters = new System.Collections.Generic.Dictionary<string, System.Func<int, RegionRichTextValue, bool>>
			{
				[stableId] = (index, value) =>
				{
					if (value == null || index < 0)
						return false;
					while (m_stText.ParagraphsOS.Count <= index)
						m_stText.InsertNewTextPara(m_stText.ParagraphsOS.Count, null);
					((IStTxtPara)m_stText.ParagraphsOS[index]).Contents =
						RegionRichTextAdapter.ToTsString(value, wsf, defaultWs);
					return true;
				}
			};
			var styleSetters = new System.Collections.Generic.Dictionary<string, System.Func<int, string, bool>>
			{
				[stableId] = (index, style) =>
				{
					if (index < 0 || index >= m_stText.ParagraphsOS.Count)
						return false;
					((IStTxtPara)m_stText.ParagraphsOS[index]).StyleName = string.IsNullOrEmpty(style)
						? SIL.LCModel.DomainServices.StyleServices.NormalStyleName
						: style;
					return true;
				}
			};
			var insertSetters = new System.Collections.Generic.Dictionary<string, System.Func<int, bool>>
			{
				[stableId] = afterIndex =>
				{
					var pos = afterIndex < 0 ? 0 : System.Math.Min(afterIndex + 1, m_stText.ParagraphsOS.Count);
					m_stText.InsertNewTextPara(pos, null);
					return true;
				}
			};
			var deleteSetters = new System.Collections.Generic.Dictionary<string, System.Func<int, bool>>
			{
				[stableId] = index =>
				{
					if (index < 0 || index >= m_stText.ParagraphsOS.Count || m_stText.ParagraphsOS.Count <= 1)
						return false;
					m_stText.ParagraphsOS.RemoveAt(index);
					return true;
				}
			};

			var context = new ComposedRegionEditContext(Cache, m_entry,
				new System.Collections.Generic.Dictionary<string, System.Func<string, string, bool>>(),
				new System.Collections.Generic.Dictionary<string, System.Func<string, bool>>(),
				paragraphTextSetters: textSetters, paragraphStyleSetters: styleSetters,
				paragraphInsertSetters: insertSetters, paragraphDeleteSetters: deleteSetters);
			return (field, context);
		}

		private static RegionRichTextValue Rich(string text)
			=> RegionRichTextEditAlgorithms.FromRuns(text,
				new[] { new RegionTextRun(text) });

		[Test]
		public void ParagraphTextEdit_RoundTripsToLcm_AsOneUndoStep()
		{
			var (field, context) = Build();

			Assert.That(context.TrySetParagraphText(field, 0, Rich("First paragraph, edited.")), Is.True);
			Assert.That(context.IsOpen, Is.True, "the fenced session opens on the first staged edit");
			context.Commit();

			Assert.That(ParaText(0), Is.EqualTo("First paragraph, edited."));
			Assert.That(ParaText(1), Is.EqualTo("Second paragraph."), "other paragraphs are untouched");

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(ParaText(0), Is.EqualTo("First paragraph."), "one undo reverts the paragraph edit");
			Cache.ActionHandlerAccessor.Redo();
			Assert.That(ParaText(0), Is.EqualTo("First paragraph, edited."));
		}

		[Test]
		public void InsertParagraph_AddsAnEmptyParagraphAfterTheIndex_OneUndoStep()
		{
			var (field, context) = Build();
			Assert.That(ParaCount, Is.EqualTo(2));

			Assert.That(context.TryInsertParagraph(field, 0), Is.True);
			context.Commit();

			Assert.That(ParaCount, Is.EqualTo(3), "a paragraph was inserted");
			Assert.That(ParaText(0), Is.EqualTo("First paragraph."), "inserted AFTER paragraph 0");
			Assert.That(string.IsNullOrEmpty(ParaText(1)), Is.True, "the new paragraph is empty");
			Assert.That(ParaText(2), Is.EqualTo("Second paragraph."));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(ParaCount, Is.EqualTo(2), "one undo removes the inserted paragraph");
		}

		[Test]
		public void DeleteParagraph_RemovesIt_OneUndoStep_ButNeverTheLastOne()
		{
			var (field, context) = Build();

			Assert.That(context.TryDeleteParagraph(field, 0), Is.True);
			context.Commit();
			Assert.That(ParaCount, Is.EqualTo(1));
			Assert.That(ParaText(0), Is.EqualTo("Second paragraph."), "paragraph 0 was deleted");

			// The StText always keeps at least one paragraph (like the legacy editor): deleting the last
			// one is rejected without opening a session.
			var canUndoBeforeReject = Cache.ActionHandlerAccessor.CanUndo();
			Assert.That(context.TryDeleteParagraph(field, 0), Is.False, "the only paragraph cannot be deleted");
			Assert.That(context.IsOpen, Is.False, "a rejected delete opens no session");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(canUndoBeforeReject));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(ParaCount, Is.EqualTo(2), "one undo restores the deleted paragraph");
		}

		[Test]
		public void ParagraphStyle_AppliedAndCleared_OneUndoStep()
		{
			var (field, context) = Build();

			Assert.That(context.TrySetParagraphStyle(field, 0, "Block Quote"), Is.True);
			context.Commit();
			Assert.That(ParaStyle(0), Is.EqualTo("Block Quote"));

			Assert.That(context.TrySetParagraphStyle(field, 0, null), Is.True);
			context.Commit();
			// "Clear" reverts to the default paragraph style (LCModel forbids a null/empty StyleName).
			Assert.That(ParaStyle(0), Is.EqualTo(SIL.LCModel.DomainServices.StyleServices.NormalStyleName),
				"clearing reverts the paragraph to the default (Normal) style");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(ParaStyle(0), Is.EqualTo("Block Quote"), "undo restores the cleared style");
		}

		[Test]
		public void Cancel_RollsBackEveryStagedParagraphEdit()
		{
			var (field, context) = Build();

			context.TrySetParagraphText(field, 0, Rich("changed"));
			context.TryInsertParagraph(field, 1);
			context.Cancel();

			Assert.That(ParaCount, Is.EqualTo(2), "cancel rolls back the insert");
			Assert.That(ParaText(0), Is.EqualTo("First paragraph."), "cancel rolls back the text edit");
		}

		[Test]
		public void MultiParagraphRoundTrip_ProjectsEveryParagraphFromLcm()
		{
			// FromTsString projection mirrors what AddStructuredText builds for the region model.
			var paragraphs = m_stText.ParagraphsOS.OfType<IStTxtPara>()
				.Select(p => new RegionParagraph(
					RegionRichTextAdapter.FromTsString(p.Contents, Cache.WritingSystemFactory), p.StyleName))
				.ToList();

			Assert.That(paragraphs, Has.Count.EqualTo(2));
			Assert.That(paragraphs[0].Text.PlainText, Is.EqualTo("First paragraph."));
			Assert.That(paragraphs[1].Text.PlainText, Is.EqualTo("Second paragraph."));
			Assert.That(paragraphs.All(p => p.CanEditText), Is.True, "plain paragraphs round-trip and are editable");
		}
	}
}
