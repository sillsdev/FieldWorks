// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
