// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	public class InterlinLineChoicesTests : MemoryOnlyBackendProviderTestBase
	{
		private int kwsVernInPara;
		private int kwsAnalysis;
		private ILangProject m_lp;

		public override void FixtureSetup()
		{
			base.FixtureSetup();

			kwsVernInPara = WritingSystemServices.kwsVernInParagraph;
			kwsAnalysis = WritingSystemServices.kwsAnal;
			m_lp = Cache.LangProject;
		}

		[Test]
		public void AddFields()
		{
			InterlinLineChoices choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			choices.Add(InterlinLineChoices.kflidWord);
			choices.Add(InterlinLineChoices.kflidLexEntries);
			choices.Add(InterlinLineChoices.kflidWordGloss);
			choices.Add(InterlinLineChoices.kflidLexPos);
			choices.Add(InterlinLineChoices.kflidFreeTrans);
			choices.Add(InterlinLineChoices.kflidWordPos);
			choices.Add(InterlinLineChoices.kflidLexGloss);
			choices.Add(InterlinLineChoices.kflidLexGloss, 3003);

			// Check order inserted.
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices[0].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[1].Flid);
			// This gets reordered to keep the interlinears together.
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices[2].Flid);
			// reordered past ff and word level
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[4].Flid);
			// inserted third, but other things push past it.
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[5].Flid);
			// reordered past a freeform.
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices[6].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices[7].Flid);

			// Check writing systems assigned by default.
			Assert.AreEqual(kwsVernInPara, choices[0].WritingSystem);
			Assert.AreEqual(WritingSystemServices.kwsFirstAnal, choices[3].WritingSystem);
			Assert.AreEqual(3003, choices[4].WritingSystem);

			// Check field levels
			Assert.IsTrue(choices[0].WordLevel);
			Assert.IsTrue(choices[1].WordLevel);
			Assert.IsFalse(choices[7].WordLevel);

			Assert.IsFalse(choices[0].MorphemeLevel);
			Assert.IsTrue(choices[1].MorphemeLevel);
			Assert.IsFalse(choices[6].MorphemeLevel);
			Assert.AreEqual(1, choices.FirstMorphemeIndex);
			Assert.AreEqual(1, choices.FirstLexEntryIndex);

			Assert.IsTrue(choices[1].LexEntryLevel);	// lex entries
			Assert.IsTrue(choices[2].LexEntryLevel);	// lex pos
			Assert.IsTrue(choices[3].LexEntryLevel);	// lex gloss
			Assert.IsTrue(choices[4].LexEntryLevel);	// lex gloss
			Assert.IsFalse(choices[0].LexEntryLevel);	// word
			Assert.IsFalse(choices[5].LexEntryLevel);	// word gloss
			Assert.IsFalse(choices[6].LexEntryLevel);	// word pos
			Assert.IsFalse(choices[7].LexEntryLevel);	// free trans

			choices.Add(InterlinLineChoices.kflidMorphemes);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[5].Flid);
			Assert.AreEqual(1, choices.FirstMorphemeIndex);	// first morpheme group line
			Assert.AreEqual(1, choices.FirstLexEntryIndex);	// lex entry
			Assert.IsFalse(choices[5].LexEntryLevel);	// morphemes
		}
		[Test]
		public void AddRemoveEditFields()
		{
			InterlinLineChoices choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			choices.Add(InterlinLineChoices.kflidWord);
			choices.Add(InterlinLineChoices.kflidMorphemes);
			choices.Add(InterlinLineChoices.kflidWordGloss);
			choices.Add(InterlinLineChoices.kflidFreeTrans);
			choices.Add(InterlinLineChoices.kflidWordPos);
			choices.Add(InterlinLineChoices.kflidLexGloss);

			Assert.AreEqual(InterlinLineChoices.kflidWord, choices[0].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[1].Flid);
			// reordered past ff and word level
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[3].Flid);
			// reordered past a freeform.
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices[4].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices[5].Flid);

			// We can't remove the Word line.
			string msg;
			Assert.IsFalse(choices.OkToRemove(choices[0], out msg));
			Assert.IsNotNull(msg);
			// Add another word line and make sure we can remove one of them.
			choices.Add(InterlinLineChoices.kflidWord);
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices[1].Flid);
			Assert.IsTrue(choices.OkToRemove(choices[0], out msg));
			Assert.IsNull(msg);
			choices.Remove(choices[0]);
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices[0].Flid);

			// Other fields can be removed freely
			Assert.IsTrue(choices.OkToRemove(choices[1], out msg));
			Assert.IsNull(msg);
			Assert.IsTrue(choices.OkToRemove(choices[2], out msg));
			Assert.IsNull(msg);
			Assert.IsTrue(choices.OkToRemove(choices[3], out msg));
			Assert.IsNull(msg);
			Assert.IsTrue(choices.OkToRemove(choices[4], out msg));
			Assert.IsNull(msg);
			Assert.IsTrue(choices.OkToRemove(choices[5], out msg));
			Assert.IsNull(msg);

			// Check what goes along with the morphemes line: morpheme line should be independent (LT-6043).
			choices.Remove(choices[1]);
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices[0].Flid);
			// reordered past ff and word level
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[2].Flid);
			// reordered past a freeform.
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices[4].Flid);
			Assert.AreEqual(5, choices.Count);

			// Add Morphemes and Lexentries lines at the end of the other morpheme group rows.
			choices.Add(InterlinLineChoices.kflidLexEntries); // bring entries back in
			choices.Add(InterlinLineChoices.kflidMorphemes); // bring entries and morphemes back in
			Assert.AreEqual(7, choices.Count);
			// in 9.1 we have removed the restrictions that the Morphemes and LexEntries lines be at the top
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[3].Flid);
			choices.Remove(choices[2]); // and get rid of the entries
			Assert.AreEqual(6, choices.Count);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void MoveUp(bool editable)
		{
			var choices = editable
				? new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis)
				: new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			// To make it less confusing, here we add them in an order that does not produce
			// reordering.
			MakeStandardState(choices);

			// Nothing can move because there are no rows with multiple writing systems
			Assert.That(choices.OkToMoveUp(0), Is.False);
			Assert.That(choices.OkToMoveUp(1), Is.False);
			Assert.That(choices.OkToMoveUp(2), Is.False);
			Assert.That(choices.OkToMoveUp(3), Is.False);
			Assert.That(choices.OkToMoveUp(4), Is.False);
			Assert.That(choices.OkToMoveUp(5), Is.False);
			Assert.That(choices.OkToMoveUp(6), Is.False);
			Assert.That(choices.OkToMoveUp(7), Is.False);
			Assert.That(choices.OkToMoveUp(8), Is.False);
		}

		[Test]
		public void MoveWsRowsUp()
		{
			const int fakeSecondWs = 90008;
			InterlinLineChoices choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			choices.Add(InterlinLineChoices.kflidWord); // 0
			choices.Add(InterlinLineChoices.kflidWordPos); // 1
			choices.Add(InterlinLineChoices.kflidWordGloss, kwsAnalysis); // 2
			choices.Add(InterlinLineChoices.kflidWordGloss, fakeSecondWs); // 3

			Assert.That(choices.OkToMoveUp(0), Is.False); // words line already at top
			Assert.That(choices.OkToMoveUp(1), Is.False);
			Assert.That(choices.OkToMoveUp(2), Is.False);
			Assert.That(choices.OkToMoveUp(3), Is.True);

			choices.MoveUp(3);
			// Second ws moved above first
			Assert.That(choices[2].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			Assert.That(choices[2].WritingSystem, Is.EqualTo(fakeSecondWs));
		}

		[Test]
		public void MoveWsRowsDown()
		{
			const int fakeSecondWs = 90008;
			InterlinLineChoices choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			choices.Add(InterlinLineChoices.kflidWord); // 0
			choices.Add(InterlinLineChoices.kflidWordGloss, kwsAnalysis); // 1
			choices.Add(InterlinLineChoices.kflidWordGloss, fakeSecondWs); // 2
			choices.Add(InterlinLineChoices.kflidWordPos); // 3

			Assert.That(choices.OkToMoveDown(0), Is.False);
			Assert.That(choices.OkToMoveDown(1), Is.True);
			Assert.That(choices.OkToMoveDown(2), Is.False);
			Assert.That(choices.OkToMoveDown(3), Is.False);

			choices.MoveDown(1);
			// First ws row moved below second
			Assert.That(choices[2].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			Assert.That(choices[2].WritingSystem, Is.EqualTo(kwsAnalysis));
		}

		private void MakeStandardState(InterlinLineChoices choices)
		{
			choices.Add(InterlinLineChoices.kflidWord); // 0
			choices.Add(InterlinLineChoices.kflidMorphemes); // 1
			choices.Add(InterlinLineChoices.kflidLexEntries); //2
			choices.Add(InterlinLineChoices.kflidLexGloss); //3
			choices.Add(InterlinLineChoices.kflidLexPos); //4
			choices.Add(InterlinLineChoices.kflidWordGloss); //5
			choices.Add(InterlinLineChoices.kflidWordPos); //6
			choices.Add(InterlinLineChoices.kflidFreeTrans); //7
			choices.Add(InterlinLineChoices.kflidLitTrans); //8
		}

		[TestCase(false)]
		[TestCase(true)]
		public void MoveDown(bool editable)
		{
			var choices = editable
				? new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis)
				: new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			// To make it less confusing, here we add them in an order that does not produce
			// reordering.
			MakeStandardState(choices);

			// Nothing can move because there are no rows with multiple writing systems
			Assert.That(choices.OkToMoveDown(0), Is.False);
			Assert.That(choices.OkToMoveDown(1), Is.False);
			Assert.That(choices.OkToMoveDown(2), Is.False);
			Assert.That(choices.OkToMoveDown(3), Is.False);
			Assert.That(choices.OkToMoveDown(4), Is.False);
			Assert.That(choices.OkToMoveDown(5), Is.False);
			Assert.That(choices.OkToMoveDown(6), Is.False);
			Assert.That(choices.OkToMoveDown(7), Is.False);
			Assert.That(choices.OkToMoveDown(8), Is.False);
		}

		[Test]
		public void Persistence()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			wsManager.GetOrSet("en", out enWs);
			int wsEng = enWs.Handle;

			CoreWritingSystemDefinition frWs;
			wsManager.GetOrSet("fr", out frWs);
			int wsFrn = frWs.Handle;

			CoreWritingSystemDefinition deWs;
			wsManager.GetOrSet("de", out deWs);
			int wsGer = deWs.Handle;

			InterlinLineChoices choices = new InterlinLineChoices(m_lp, wsFrn, wsEng);
			MakeStandardState(choices);
			string persist = choices.Persist(wsManager);
			choices = InterlinLineChoices.Restore(persist, wsManager, m_lp, wsFrn, wsEng);

			Assert.AreEqual(InterlinLineChoices.kflidWord, choices[0].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices[4].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices[6].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices[7].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, choices[8].Flid);

			// Check writing systems assigned by default.
			Assert.AreEqual(wsFrn, choices[0].WritingSystem);
			Assert.AreEqual(wsEng, choices[5].WritingSystem);
			Assert.AreEqual(wsFrn, choices[2].WritingSystem);

			choices = new EditableInterlinLineChoices(m_lp, 0, wsEng);
			MakeStandardState(choices);
			choices.Add(InterlinLineChoices.kflidLexGloss, wsGer);
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices[0].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[4].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[6].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices[7].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices[8].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, choices[9].Flid);

			Assert.AreEqual(wsGer, choices[4].WritingSystem);
		}

		[Test]
		public void NewCustomFieldAddedAfterRestore()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			wsManager.GetOrSet("en", out enWs);
			var wsEng = enWs.Handle;

			CoreWritingSystemDefinition frWs;
			wsManager.GetOrSet("fr", out frWs);
			var wsFrn = frWs.Handle;

			CoreWritingSystemDefinition deWs;
			wsManager.GetOrSet("de", out deWs);
			var wsGer = deWs.Handle;

			var choices = new InterlinLineChoices(m_lp, wsFrn, wsEng);
			MakeStandardState(choices);
			var persist = choices.Persist(wsManager);
			var choicesCountBeforeCustomField = choices.AllLineOptions.Count;
			using (var cf = new CustomFieldForTest(Cache,
				"Candy Apple Red",
				Cache.MetaDataCacheAccessor.GetClassId("Segment"),
				WritingSystemServices.kwsAnal,
				CellarPropertyType.String,
				Guid.Empty))
			{
				choices = InterlinLineChoices.Restore(persist, wsManager, m_lp, wsFrn, wsEng);

				Assert.That(choices.AllLineOptions.Count, Is.EqualTo(choicesCountBeforeCustomField + 1));
				Assert.That(choices.AllLineOptions[choicesCountBeforeCustomField].Flid, Is.EqualTo(cf.Flid));

				// Verify that the choices are not disturbed by the new custom field
				Assert.That(choices[0].Flid, Is.EqualTo(InterlinLineChoices.kflidWord));
				Assert.That(choices[1].Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
				Assert.That(choices[2].Flid, Is.EqualTo(InterlinLineChoices.kflidLexEntries));
				Assert.That(choices[3].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
				Assert.That(choices[4].Flid, Is.EqualTo(InterlinLineChoices.kflidLexPos));
				Assert.That(choices[5].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
				Assert.That(choices[6].Flid, Is.EqualTo(InterlinLineChoices.kflidWordPos));
				Assert.That(choices[7].Flid, Is.EqualTo(InterlinLineChoices.kflidFreeTrans));
				Assert.That(choices[8].Flid, Is.EqualTo(InterlinLineChoices.kflidLitTrans));
			}
		}

		[Test]
		public void DeletedCustomFieldRemovedAfterRestore()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			wsManager.GetOrSet("en", out enWs);
			var wsEng = enWs.Handle;

			CoreWritingSystemDefinition frWs;
			wsManager.GetOrSet("fr", out frWs);
			var wsFrn = frWs.Handle;

			CoreWritingSystemDefinition deWs;
			wsManager.GetOrSet("de", out deWs);
			var wsGer = deWs.Handle;

			InterlinLineChoices choices;
			int choicesCountWithCustomField;
			string persist;
			using (var cf = new CustomFieldForTest(Cache,
				"Candy Apple Red",
				Cache.MetaDataCacheAccessor.GetClassId("Segment"),
				WritingSystemServices.kwsAnal,
				CellarPropertyType.String,
				Guid.Empty))
			{
				choices = new InterlinLineChoices(m_lp, wsFrn, wsEng);
				MakeStandardState(choices);
				choices.Add(cf.Flid);
				persist = choices.Persist(wsManager);
				choicesCountWithCustomField = choices.Count;
			}

			choices = InterlinLineChoices.Restore(persist, wsManager, m_lp, wsFrn, wsEng);

			Assert.That(choices.Count, Is.EqualTo(choicesCountWithCustomField - 1));

			Assert.That(choices[0].Flid, Is.EqualTo(InterlinLineChoices.kflidWord));
			Assert.That(choices[1].Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
			Assert.That(choices[2].Flid, Is.EqualTo(InterlinLineChoices.kflidLexEntries));
			Assert.That(choices[3].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(choices[4].Flid, Is.EqualTo(InterlinLineChoices.kflidLexPos));
			Assert.That(choices[5].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			Assert.That(choices[6].Flid, Is.EqualTo(InterlinLineChoices.kflidWordPos));
			Assert.That(choices[7].Flid, Is.EqualTo(InterlinLineChoices.kflidFreeTrans));
			Assert.That(choices[8].Flid, Is.EqualTo(InterlinLineChoices.kflidLitTrans));

			// Check writing systems assigned by default.
			Assert.That(choices[0].WritingSystem, Is.EqualTo(wsFrn));
			Assert.That(choices[5].WritingSystem, Is.EqualTo(wsEng));
			Assert.That(choices[2].WritingSystem, Is.EqualTo(wsFrn));
		}

		[Test]
		public void GetActualWs_MorphBundleBehavesLikeMoForm()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			wsManager.GetOrSet("en", out enWs);
			int wsEng = enWs.Handle;

			CoreWritingSystemDefinition frWs;
			wsManager.GetOrSet("fr", out frWs);
			int wsFrn = frWs.Handle;

			var choices = new InterlinLineChoices(m_lp, wsFrn, wsEng);
			MakeStandardState(choices);
			InterlinLineSpec spec = choices[1];
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, spec.Flid);
			// The StringFlid for this line spec always corresponds to a MoForm
			Assert.AreEqual(MoFormTags.kflidForm, spec.StringFlid);

			IWfiWordform wf;
			IWfiAnalysis wag;
			ITsString str = TsStringUtils.MakeString("WordForm", spec.WritingSystem);
			IWfiMorphBundle wmb = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create(str);
					wag = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(wf);
					wag.MorphBundlesOS.Add(wmb);
					ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
					IMoForm moForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
					entry.AlternateFormsOS.Add(moForm);
					moForm.Form.set_String(spec.WritingSystem, "Morph");
					wmb.MorphRA = moForm;
				});
			// The line spec for displaying the Morpheme must be able to handle getting the ws from both
			// MorphBundles or MoForms
			Assert.True(spec.Flid == InterlinLineChoices.kflidMorphemes);
			int wmbWs = spec.GetActualWs(Cache, wmb.Hvo, spec.WritingSystem);
			int mfWs = spec.GetActualWs(Cache, wmb.MorphRA.Hvo, spec.WritingSystem);
			Assert.True(wmbWs == spec.WritingSystem);
			Assert.True(mfWs == spec.WritingSystem);
		}
	}
}
