// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The expanded and collapsed header lines of senses, etymologies, extended notes and
	/// notebook records (LT-22689), checked against the WinForms Summary and SummaryCollapsed
	/// layouts they follow, and where the composer places those headers.
	/// </summary>
	[TestFixture]
	public class DetailSummaryLinesTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private ILexSense m_sense;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				m_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(m_sense);
				m_sense.Gloss.set_String(Cache.DefaultAnalWs, Analysis("house"));
			});
		}

		private ITsString Analysis(string text) => TsStringUtils.MakeString(text, Cache.DefaultAnalWs);

		private void Do(System.Action action) => NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => action());

		private (string Expanded, string Collapsed) Lines(ICmObject obj)
		{
			Assert.That(new DetailSummaryLines(Cache).TryGet(obj, out var expanded, out var collapsed), Is.True);
			return (expanded, collapsed);
		}

		private IPartOfSpeech PartOfSpeech(string abbreviation)
		{
			if (Cache.LangProject.PartsOfSpeechOA == null)
				Cache.LangProject.PartsOfSpeechOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			pos.Abbreviation.set_String(Cache.DefaultAnalWs, Analysis(abbreviation));
			return pos;
		}

		private void SetGrammaticalInfo(IMoMorphSynAnalysis msa)
		{
			m_entry.MorphoSyntaxAnalysesOC.Add(msa);
			m_sense.MorphoSyntaxAnalysisRA = msa;
		}

		private string Collapsed(System.Action<ILexEntry, ILexSense> arrange)
		{
			Do(() => arrange(m_entry, m_sense));
			return Lines(m_sense).Collapsed;
		}

		[Test]
		public void Sense_ShowsItsNumber_AndWhileCollapsed_TheGlossAndPartOfSpeech()
		{
			Do(() =>
			{
				var stem = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				SetGrammaticalInfo(stem);
				stem.PartOfSpeechRA = PartOfSpeech("n");
			});

			var lines = Lines(m_sense);

			Assert.That(lines.Expanded, Is.EqualTo("Sense 1"));
			Assert.That(lines.Collapsed, Is.EqualTo("Sense 1 - house - n"));
		}

		[Test]
		public void Sense_WhileCollapsed_ShowsTheDefinitionInPlaceOfTheGloss()
		{
			var collapsed = Collapsed((entry, sense) =>
				sense.Definition.set_String(Cache.DefaultAnalWs, Analysis("a dwelling")));

			Assert.That(collapsed, Is.EqualTo("Sense 1 - a dwelling - "));
		}

		[Test]
		public void GrammaticalInfo_OfADerivationalAffix_ShowsFromAndToPartsOfSpeech()
		{
			var collapsed = Collapsed((entry, sense) =>
			{
				var msa = Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>().Create();
				SetGrammaticalInfo(msa);
				msa.FromPartOfSpeechRA = PartOfSpeech("v");
				msa.ToPartOfSpeechRA = PartOfSpeech("n");
			});

			Assert.That(collapsed, Is.EqualTo("Sense 1 - house - v > n"));
		}

		[Test]
		public void GrammaticalInfo_OfADerivationalAffix_WithNoPartsOfSpeech_IsUnclassified()
		{
			var collapsed = Collapsed((entry, sense) =>
				SetGrammaticalInfo(Cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>().Create()));

			Assert.That(collapsed, Is.EqualTo("Sense 1 - house - Unclassified affix"));
		}

		[Test]
		public void GrammaticalInfo_OfAnInflectionalAffix_ShowsItsPartOfSpeechAndSlots()
		{
			var collapsed = Collapsed((entry, sense) =>
			{
				var pos = PartOfSpeech("v");
				var slot = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
				pos.AffixSlotsOC.Add(slot);
				slot.Name.set_String(Cache.DefaultAnalWs, Analysis("tense"));
				var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
				SetGrammaticalInfo(msa);
				msa.PartOfSpeechRA = pos;
				msa.SlotsRC.Add(slot);
			});

			Assert.That(collapsed, Is.EqualTo("Sense 1 - house - v:tense"));
		}

		[Test]
		public void GrammaticalInfo_OfAnInflectionalAffix_WithNoPartOfSpeech_IsAnUnmarkedSlot()
		{
			var collapsed = Collapsed((entry, sense) =>
				SetGrammaticalInfo(Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create()));

			Assert.That(collapsed, Is.EqualTo("Sense 1 - house - Unmk. infl. afx slot"));
		}

		[Test]
		public void GrammaticalInfo_OfAnUnclassifiedAffix_ShowsItsPartOfSpeech()
		{
			var collapsed = Collapsed((entry, sense) =>
			{
				var msa = Cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create();
				SetGrammaticalInfo(msa);
				msa.PartOfSpeechRA = PartOfSpeech("adj");
			});

			Assert.That(collapsed, Is.EqualTo("Sense 1 - house - adj"));
		}

		[Test]
		public void Etymology_WhileCollapsed_ShowsTheGloss_AndAnyLanguageNotesBeforeIt()
		{
			ILexEtymology etymology = null;
			Do(() =>
			{
				etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
				m_entry.EtymologyOS.Add(etymology);
				etymology.Gloss.set_String(Cache.DefaultAnalWs, Analysis("borrowed"));
			});
			Assert.That(Lines(etymology), Is.EqualTo(("Etymology", "Etymology - borrowed")));

			Do(() => etymology.LanguageNotes.set_String(Cache.DefaultAnalWs, Analysis("from Latin")));
			Assert.That(Lines(etymology).Collapsed, Is.EqualTo("Etymology - from Latin - borrowed"));
		}

		[Test]
		public void ExtendedNote_WhileCollapsed_ShowsItsTypeAndDiscussion()
		{
			ILexExtendedNote note = null;
			Do(() =>
			{
				if (Cache.LangProject.LexDbOA.ExtendedNoteTypesOA == null)
					Cache.LangProject.LexDbOA.ExtendedNoteTypesOA =
						Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				var type = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				Cache.LangProject.LexDbOA.ExtendedNoteTypesOA.PossibilitiesOS.Add(type);
				type.Abbreviation.set_String(Cache.DefaultAnalWs, Analysis("use"));
				note = Cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
				m_sense.ExtendedNoteOS.Add(note);
				note.ExtendedNoteTypeRA = type;
				note.Discussion.set_String(Cache.DefaultAnalWs, Analysis("formal speech"));
			});

			Assert.That(Lines(note), Is.EqualTo(("Extended Note", "Extended Note - use - formal speech")));
		}

		private IRnGenericRec RecordWithSubrecord(out IRnGenericRec subrecord)
		{
			IRnGenericRec record = null, sub = null;
			Do(() =>
			{
				if (Cache.LangProject.ResearchNotebookOA == null)
					Cache.LangProject.ResearchNotebookOA = Cache.ServiceLocator.GetInstance<IRnResearchNbkFactory>().Create();
				var notebook = Cache.LangProject.ResearchNotebookOA;
				if (notebook.RecTypesOA == null)
					notebook.RecTypesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				var type = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				notebook.RecTypesOA.PossibilitiesOS.Add(type);
				type.Name.set_String(Cache.DefaultAnalWs, Analysis("Observation"));
				record = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
				notebook.RecordsOC.Add(record);
				sub = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
				record.SubRecordsOS.Add(sub);
				sub.TypeRA = type;
				sub.Title = Analysis("Market day");
			});
			subrecord = sub;
			return record;
		}

		[Test]
		public void Subrecord_ShowsItsNumberAndType_AndWhileCollapsed_ItsTitle()
		{
			RecordWithSubrecord(out var subrecord);

			var lines = Lines(subrecord);

			Assert.That(lines.Expanded, Does.StartWith("1").And.EndWith(" Observation"));
			Assert.That(lines.Collapsed, Is.EqualTo(lines.Expanded + " - Market day"));
		}

		[Test]
		public void Compose_ASubrecord_HasOneHeaderOfItsOwn_UnderTheSubrecordsSection()
		{
			var record = RecordWithSubrecord(out var subrecord);

			var headers = DetailComposer.Compose(record, Cache).Model.Fields
				.Where(f => f.Kind == DetailFieldKind.Header).ToList();

			var subrecordHeaders = headers.Where(h => h.ObjectHvo == subrecord.Hvo).ToList();
			Assert.That(subrecordHeaders.Select(h => h.Label), Is.EqualTo(new[] { Lines(subrecord).Expanded }),
				"one header for the subrecord, carrying its summary line");
			var section = headers.Single(h => h.Label == "Subrecords");
			Assert.That(subrecordHeaders[0].Indent, Is.EqualTo(section.Indent + 1),
				"the subrecord sits directly under the Subrecords section, with no banner between");
		}

		// The Event record Type: its Normal layout shows Hypothesis and hides an empty Sources,
		// and the layout with no choiceGuid does the reverse.
		private static readonly Guid EventTypeGuid = new Guid("82290763-1633-4998-8317-0EC3F5027FBD");

		private ICmPossibility EventType()
		{
			var types = Cache.LangProject.ResearchNotebookOA.RecTypesOA;
			return types.PossibilitiesOS.FirstOrDefault(p => p.Guid == EventTypeGuid)
				?? Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(EventTypeGuid, types);
		}

		private IRnGenericRec AddSubrecord(IRnGenericRec owner, string title)
		{
			var sub = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
			owner.SubRecordsOS.Add(sub);
			sub.Title = Analysis(title);
			return sub;
		}

		// The non-header rows composed for the object.
		private static List<DetailField> RowsOf(IReadOnlyList<DetailField> fields, ICmObject obj)
			=> fields.Where(f => f.ObjectHvo == obj.Hvo && f.Kind != DetailFieldKind.Header).ToList();

		// LT-22858
		[Test]
		public void Compose_ASubrecord_ShowsItsFields_UnderItsHeader_ChosenByItsType()
		{
			var record = RecordWithSubrecord(out var untyped);
			IRnGenericRec eventSub = null;
			Do(() =>
			{
				untyped.TypeRA = null;
				eventSub = AddSubrecord(record, "Harvest");
				eventSub.TypeRA = EventType();
			});

			var fields = DetailComposer.Compose(record, Cache).Model.Fields;

			foreach (var sub in new[] { untyped, eventSub })
			{
				var header = fields.Single(f => f.Kind == DetailFieldKind.Header && f.ObjectHvo == sub.Hvo);
				var title = RowsOf(fields, sub).FirstOrDefault(f => f.Field == "Title");
				Assert.That(title, Is.Not.Null, "the subrecord's own fields compose under its header");
				Assert.That(title.Indent, Is.EqualTo(header.Indent + 1), "one level under the header");
			}
			var eventFields = RowsOf(fields, eventSub).Select(f => f.Field).ToList();
			var untypedFields = RowsOf(fields, untyped).Select(f => f.Field).ToList();
			Assert.That(eventFields, Does.Contain("Hypothesis").And.Not.Contain("Sources"),
				"an Event subrecord gets the Event layout");
			Assert.That(untypedFields, Does.Contain("Sources").And.Not.Contain("Hypothesis"),
				"a subrecord with no Type gets the layout with no choice");
		}

		[Test]
		public void Edit_ASubrecordsTitle_UnderItsHeader_SavesToTheSubrecord()
		{
			var record = RecordWithSubrecord(out var sub);
			Do(() => record.Title = Analysis("Field trip"));
			var composed = DetailComposer.Compose(record, Cache);
			var title = RowsOf(composed.Model.Fields, sub).Single(f => f.Field == "Title");

			Assert.That(composed.EditContext.TrySetText(title, title.Values[0].WsAbbrev, "Fish market"), Is.True);
			composed.EditContext.Commit();

			Assert.That(sub.Title.Text, Is.EqualTo("Fish market"));
			Assert.That(record.Title.Text, Is.EqualTo("Field trip"), "only the subrecord changed");
		}

		[Test]
		public void Compose_ASubrecordOfASubrecord_ShowsItsFieldsOneLevelFurtherIn()
		{
			var record = RecordWithSubrecord(out var sub);
			IRnGenericRec subSub = null;
			Do(() => subSub = AddSubrecord(sub, "Stall prices"));

			var fields = DetailComposer.Compose(record, Cache).Model.Fields;

			var subTitle = RowsOf(fields, sub).Single(f => f.Field == "Title");
			var subSubHeader = fields.Single(f => f.Kind == DetailFieldKind.Header && f.ObjectHvo == subSub.Hvo);
			var subSubTitle = RowsOf(fields, subSub).SingleOrDefault(f => f.Field == "Title");
			Assert.That(subSubTitle, Is.Not.Null, "the nested subrecord's fields compose too");
			Assert.That(subSubHeader.Indent, Is.GreaterThan(subTitle.Indent), "its header nests inside the subrecord");
			Assert.That(subSubTitle.Indent, Is.EqualTo(subSubHeader.Indent + 1));
		}

		[Test]
		public void Compose_EtymologiesAndExtendedNotes_HaveNoBanner_AndCarryTheirSummaryLines()
		{
			ILexEtymology etymology = null;
			ILexExtendedNote note = null;
			Do(() =>
			{
				etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
				m_entry.EtymologyOS.Add(etymology);
				etymology.Gloss.set_String(Cache.DefaultAnalWs, Analysis("borrowed"));
				note = Cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
				m_sense.ExtendedNoteOS.Add(note);
				note.Discussion.set_String(Cache.DefaultAnalWs, Analysis("formal speech"));
			});

			var headers = DetailComposer.Compose(m_entry, Cache).Model.Fields
				.Where(f => f.Kind == DetailFieldKind.Header).ToList();

			Assert.That(headers.Any(h => h.Field == "Etymology" && h.ObjectHvo == m_entry.Hvo), Is.False,
				"no banner over the etymologies");
			Assert.That(headers.Any(h => h.Field == "ExtendedNote" && h.ObjectHvo == m_sense.Hvo), Is.False,
				"no banner over the extended notes");
			var etymologyHeader = headers.Single(h => h.ObjectHvo == etymology.Hvo);
			Assert.That((etymologyHeader.Label, etymologyHeader.CollapsedLabel), Is.EqualTo(Lines(etymology)));
			Assert.That(etymologyHeader.Indent, Is.EqualTo(0), "an etymology header sits at the entry's level");
			var noteHeader = headers.Single(h => h.ObjectHvo == note.Hvo);
			Assert.That((noteHeader.Label, noteHeader.CollapsedLabel), Is.EqualTo(Lines(note)));
			Assert.That(etymologyHeader.IsNormalWeight && noteHeader.IsNormalWeight, Is.True,
				"etymologies and extended notes open with a NormalSummary row");
			Assert.That(headers.Single(h => h.ObjectHvo == m_sense.Hvo).IsNormalWeight, Is.False,
				"a sense opens with a HeavySummary row");
		}
	}
}
