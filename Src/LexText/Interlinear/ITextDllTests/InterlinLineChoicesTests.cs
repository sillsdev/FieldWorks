// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices.EnabledLineSpecs[0].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices.EnabledLineSpecs[1].Flid);
			// This gets reordered to keep the interlinears together.
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices.EnabledLineSpecs[2].Flid);
			// reordered past ff and word level
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.EnabledLineSpecs[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.EnabledLineSpecs[4].Flid);
			// inserted third, but other things push past it.
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices.EnabledLineSpecs[5].Flid);
			// reordered past a freeform.
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices.EnabledLineSpecs[6].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices.EnabledLineSpecs[7].Flid);

			// Check writing systems assigned by default.
			Assert.AreEqual(kwsVernInPara, choices.EnabledLineSpecs[0].WritingSystem);
			Assert.AreEqual(WritingSystemServices.kwsFirstAnal, choices.EnabledLineSpecs[3].WritingSystem);
			Assert.AreEqual(3003, choices.EnabledLineSpecs[4].WritingSystem);

			// Check field levels
			Assert.IsTrue(choices.EnabledLineSpecs[0].WordLevel);
			Assert.IsTrue(choices.EnabledLineSpecs[1].WordLevel);
			Assert.IsFalse(choices.EnabledLineSpecs[7].WordLevel);

			Assert.IsFalse(choices.EnabledLineSpecs[0].MorphemeLevel);
			Assert.IsTrue(choices.EnabledLineSpecs[1].MorphemeLevel);
			Assert.IsFalse(choices.EnabledLineSpecs[6].MorphemeLevel);
			Assert.AreEqual(1, choices.FirstEnabledMorphemeIndex);
			Assert.AreEqual(1, choices.FirstEnabledLexEntryIndex);

			Assert.IsTrue(choices.EnabledLineSpecs[1].LexEntryLevel);   // lex entries
			Assert.IsTrue(choices.EnabledLineSpecs[2].LexEntryLevel);   // lex pos
			Assert.IsTrue(choices.EnabledLineSpecs[3].LexEntryLevel);	// lex gloss
			Assert.IsTrue(choices.EnabledLineSpecs[4].LexEntryLevel);	// lex gloss
			Assert.IsFalse(choices.EnabledLineSpecs[0].LexEntryLevel);	// word
			Assert.IsFalse(choices.EnabledLineSpecs[5].LexEntryLevel);	// word gloss
			Assert.IsFalse(choices.EnabledLineSpecs[6].LexEntryLevel);	// word pos
			Assert.IsFalse(choices.EnabledLineSpecs[7].LexEntryLevel);	// free trans

			choices.Add(InterlinLineChoices.kflidMorphemes);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices.EnabledLineSpecs[5].Flid);
			Assert.AreEqual(1, choices.FirstEnabledMorphemeIndex);	// first morpheme group line
			Assert.AreEqual(1, choices.FirstEnabledLexEntryIndex);	// lex entry
			Assert.IsFalse(choices.EnabledLineSpecs[5].LexEntryLevel);	// morphemes
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

			Assert.AreEqual(InterlinLineChoices.kflidWord, choices.EnabledLineSpecs[0].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices.EnabledLineSpecs[1].Flid);
			// reordered past ff and word level
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.EnabledLineSpecs[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices.EnabledLineSpecs[3].Flid);
			// reordered past a freeform.
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices.EnabledLineSpecs[4].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices.EnabledLineSpecs[5].Flid);

			// We can't remove the Word line.
			string msg;
			Assert.IsFalse(choices.OkToRemove(choices.EnabledLineSpecs[0], out msg));
			Assert.That(msg, Is.Not.Null);

			// Cannot add duplicates.
			var beforeCount = choices.AllLineSpecs.Count;
			choices.Add(InterlinLineChoices.kflidWord);
			Assert.AreEqual(beforeCount, choices.AllLineSpecs.Count);

			// Other fields can be removed freely
			Assert.IsTrue(choices.OkToRemove(choices.EnabledLineSpecs[1], out msg));
			Assert.That(msg, Is.Null);
			Assert.IsTrue(choices.OkToRemove(choices.EnabledLineSpecs[2], out msg));
			Assert.That(msg, Is.Null);
			Assert.IsTrue(choices.OkToRemove(choices.EnabledLineSpecs[3], out msg));
			Assert.That(msg, Is.Null);
			Assert.IsTrue(choices.OkToRemove(choices.EnabledLineSpecs[4], out msg));
			Assert.That(msg, Is.Null);
			Assert.IsTrue(choices.OkToRemove(choices.EnabledLineSpecs[5], out msg));
			Assert.That(msg, Is.Null);

			// Check what goes along with the morphemes line: morpheme line should be independent (LT-6043).
			choices.Remove(choices.EnabledLineSpecs[1]);
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices.EnabledLineSpecs[0].Flid);
			// reordered past ff and word level
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.EnabledLineSpecs[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices.EnabledLineSpecs[2].Flid);
			// reordered past a freeform.
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices.EnabledLineSpecs[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices.EnabledLineSpecs[4].Flid);
			Assert.AreEqual(5, choices.EnabledCount);

			// Add Morphemes and Lexentries lines at the end of the other morpheme group rows.
			choices.Add(InterlinLineChoices.kflidLexEntries); // bring entries back in
			choices.Add(InterlinLineChoices.kflidMorphemes); // bring entries and morphemes back in
			Assert.AreEqual(7, choices.EnabledCount);
			// in 9.1 we have removed the restrictions that the Morphemes and LexEntries lines be at the top
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices.EnabledLineSpecs[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices.EnabledLineSpecs[3].Flid);
			choices.Remove(choices.EnabledLineSpecs[2]); // and get rid of the entries
			Assert.AreEqual(6, choices.EnabledCount);
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
			Assert.That(choices.EnabledLineSpecs[2].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			Assert.That(choices.EnabledLineSpecs[2].WritingSystem, Is.EqualTo(fakeSecondWs));
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
			Assert.That(choices.EnabledLineSpecs[2].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			Assert.That(choices.EnabledLineSpecs[2].WritingSystem, Is.EqualTo(kwsAnalysis));
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

			Assert.AreEqual(InterlinLineChoices.kflidWord, choices.EnabledLineSpecs[0].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices.EnabledLineSpecs[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices.EnabledLineSpecs[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.EnabledLineSpecs[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices.EnabledLineSpecs[4].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices.EnabledLineSpecs[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices.EnabledLineSpecs[6].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices.EnabledLineSpecs[7].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, choices.EnabledLineSpecs[8].Flid);

			// Check writing systems assigned by default.
			Assert.AreEqual(wsFrn, choices.EnabledLineSpecs[0].WritingSystem);
			Assert.AreEqual(wsEng, choices.EnabledLineSpecs[5].WritingSystem);
			Assert.AreEqual(wsFrn, choices.EnabledLineSpecs[2].WritingSystem);

			choices = new EditableInterlinLineChoices(m_lp, 0, wsEng);
			MakeStandardState(choices);
			choices.Add(InterlinLineChoices.kflidLexGloss, wsGer);
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices.EnabledLineSpecs[0].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices.EnabledLineSpecs[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices.EnabledLineSpecs[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.EnabledLineSpecs[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.EnabledLineSpecs[4].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices.EnabledLineSpecs[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices.EnabledLineSpecs[6].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices.EnabledLineSpecs[7].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices.EnabledLineSpecs[8].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, choices.EnabledLineSpecs[9].Flid);

			Assert.AreEqual(wsGer, choices.EnabledLineSpecs[4].WritingSystem);
		}

		[Test]
		// Confirm that only 'enabled' specs are returned.
		public void EnabledLineSpecs()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			wsManager.GetOrSet("en", out enWs);
			int wsEng = enWs.Handle;

			CoreWritingSystemDefinition frWs;
			wsManager.GetOrSet("fr", out frWs);
			int wsFrn = frWs.Handle;

			InterlinLineChoices choices = new InterlinLineChoices(m_lp, wsFrn, wsEng);
			MakeStandardState(choices);
			choices.AllLineSpecs[2].Enabled = false;
			choices.AllLineSpecs[8].Enabled = false;
			Assert.AreEqual(9, choices.AllLineSpecs.Count);
			Assert.AreEqual(7, choices.EnabledLineSpecs.Count);
		}

		[Test]
		// Confirm that the 'enabled' flags are persisted and restored.
		public void PersistEnabled()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			wsManager.GetOrSet("en", out enWs);
			int wsEng = enWs.Handle;

			CoreWritingSystemDefinition frWs;
			wsManager.GetOrSet("fr", out frWs);
			int wsFrn = frWs.Handle;

			InterlinLineChoices choices = new InterlinLineChoices(m_lp, wsFrn, wsEng);
			MakeStandardState(choices);
			choices.AllLineSpecs[2].Enabled = false;
			choices.AllLineSpecs[8].Enabled = false;
			Assert.AreEqual(false, choices.AllLineSpecs[2].Enabled);
			Assert.AreEqual(false, choices.AllLineSpecs[8].Enabled);

			string persist = choices.Persist(wsManager);
			choices = InterlinLineChoices.Restore(persist, wsManager, m_lp, wsFrn, wsEng);

			Assert.AreEqual(false, choices.AllLineSpecs[2].Enabled);
			Assert.AreEqual(false, choices.AllLineSpecs[8].Enabled);
		}

		[Test]
		// Confirm that ConfigurationLineOptions returns the required list (one of each Flid) in the preserved order.
		public void ConfigurationLineOptions()
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
			choices.Add(InterlinLineChoices.kflidWord, wsEng, true); // 0
			choices.Add(InterlinLineChoices.kflidMorphemes, wsEng, true); // 1
			choices.Add(InterlinLineChoices.kflidLexEntries, wsEng, true); //2
			choices.Add(InterlinLineChoices.kflidLexGloss, wsEng, true); //3
			choices.Add(InterlinLineChoices.kflidLexPos, wsEng, true); //4
			choices.Add(InterlinLineChoices.kflidWordGloss, wsEng, true); //5
			choices.Add(InterlinLineChoices.kflidWordPos, wsEng, true); //6
			choices.Add(InterlinLineChoices.kflidLitTrans, wsEng, true); //7
			choices.Add(InterlinLineChoices.kflidFreeTrans, wsEng, true); //8

			choices.Add(InterlinLineChoices.kflidLexGloss, wsFrn, true);
			choices.Add(InterlinLineChoices.kflidLexGloss, wsGer, true);

			// Pre-checks
			Assert.AreEqual(11, choices.AllLineSpecs.Count);
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices.AllLineSpecs[0].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices.AllLineSpecs[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices.AllLineSpecs[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.AllLineSpecs[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.AllLineSpecs[4].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices.AllLineSpecs[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices.AllLineSpecs[6].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices.AllLineSpecs[7].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices.AllLineSpecs[8].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, choices.AllLineSpecs[9].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices.AllLineSpecs[10].Flid);

			Assert.AreEqual(wsEng, choices.AllLineSpecs[3].WritingSystem);
			Assert.AreEqual(wsFrn, choices.AllLineSpecs[4].WritingSystem);
			Assert.AreEqual(wsGer, choices.AllLineSpecs[5].WritingSystem);

			ReadOnlyCollection<LineOption> configLineOptions = choices.ConfigurationLineOptions;

			// Post-checks
			Assert.AreEqual(10, configLineOptions.Count); // 9 + 1 for kflidNote.
			Assert.AreEqual(InterlinLineChoices.kflidWord, configLineOptions[0].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, configLineOptions[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, configLineOptions[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, configLineOptions[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, configLineOptions[4].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, configLineOptions[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, configLineOptions[6].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, configLineOptions[7].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, configLineOptions[8].Flid);
			// kflidNote is one of the required options so it was added.
			Assert.AreEqual(InterlinLineChoices.kflidNote, configLineOptions[9].Flid);
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
			var choicesCountBeforeCustomField = choices.ConfigurationLineOptions.Count;
			using (var cf = new CustomFieldForTest(Cache,
				"Candy Apple Red",
				Cache.MetaDataCacheAccessor.GetClassId("Segment"),
				WritingSystemServices.kwsAnal,
				CellarPropertyType.String,
				Guid.Empty))
			{
				choices = InterlinLineChoices.Restore(persist, wsManager, m_lp, wsFrn, wsEng);

				Assert.That(choices.ConfigurationLineOptions.Count, Is.EqualTo(choicesCountBeforeCustomField + 1));
				Assert.That(choices.ConfigurationLineOptions[choicesCountBeforeCustomField].Flid, Is.EqualTo(cf.Flid));

				// Verify that the choices are not disturbed by the new custom field
				Assert.That(choices.EnabledLineSpecs[0].Flid, Is.EqualTo(InterlinLineChoices.kflidWord));
				Assert.That(choices.EnabledLineSpecs[1].Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
				Assert.That(choices.EnabledLineSpecs[2].Flid, Is.EqualTo(InterlinLineChoices.kflidLexEntries));
				Assert.That(choices.EnabledLineSpecs[3].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
				Assert.That(choices.EnabledLineSpecs[4].Flid, Is.EqualTo(InterlinLineChoices.kflidLexPos));
				Assert.That(choices.EnabledLineSpecs[5].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
				Assert.That(choices.EnabledLineSpecs[6].Flid, Is.EqualTo(InterlinLineChoices.kflidWordPos));
				Assert.That(choices.EnabledLineSpecs[7].Flid, Is.EqualTo(InterlinLineChoices.kflidFreeTrans));
				Assert.That(choices.EnabledLineSpecs[8].Flid, Is.EqualTo(InterlinLineChoices.kflidLitTrans));
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
				choicesCountWithCustomField = choices.EnabledCount;
			}

			choices = InterlinLineChoices.Restore(persist, wsManager, m_lp, wsFrn, wsEng);

			Assert.That(choices.EnabledCount, Is.EqualTo(choicesCountWithCustomField - 1));

			Assert.That(choices.EnabledLineSpecs[0].Flid, Is.EqualTo(InterlinLineChoices.kflidWord));
			Assert.That(choices.EnabledLineSpecs[1].Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
			Assert.That(choices.EnabledLineSpecs[2].Flid, Is.EqualTo(InterlinLineChoices.kflidLexEntries));
			Assert.That(choices.EnabledLineSpecs[3].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(choices.EnabledLineSpecs[4].Flid, Is.EqualTo(InterlinLineChoices.kflidLexPos));
			Assert.That(choices.EnabledLineSpecs[5].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			Assert.That(choices.EnabledLineSpecs[6].Flid, Is.EqualTo(InterlinLineChoices.kflidWordPos));
			Assert.That(choices.EnabledLineSpecs[7].Flid, Is.EqualTo(InterlinLineChoices.kflidFreeTrans));
			Assert.That(choices.EnabledLineSpecs[8].Flid, Is.EqualTo(InterlinLineChoices.kflidLitTrans));

			// Check writing systems assigned by default.
			Assert.That(choices.EnabledLineSpecs[0].WritingSystem, Is.EqualTo(wsFrn));
			Assert.That(choices.EnabledLineSpecs[5].WritingSystem, Is.EqualTo(wsEng));
			Assert.That(choices.EnabledLineSpecs[2].WritingSystem, Is.EqualTo(wsFrn));
		}

		[Test]
		public void AddCustomSpecsForAnalAndVern()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			wsManager.GetOrSet("en", out enWs);
			var wsEng = enWs.Handle;

			CoreWritingSystemDefinition frWs;
			wsManager.GetOrSet("fr", out frWs);
			var wsFrn = frWs.Handle;

			using (var cFirstAnal = new CustomFieldForTest(Cache,
				"Candy Apple Red Anal",
				Cache.MetaDataCacheAccessor.GetClassId("Segment"),
				WritingSystemServices.kwsAnal,
				CellarPropertyType.String,
				Guid.Empty))
			using (var cFirstVern = new CustomFieldForTest(Cache,
				"Candy Apple Red Vern",
				Cache.MetaDataCacheAccessor.GetClassId("Segment"),
				WritingSystemServices.kwsVern,
				CellarPropertyType.String,
				Guid.Empty))
			{
				InterlinLineChoices choices = new InterlinLineChoices(m_lp, wsFrn, wsEng);
				choices.Add(cFirstAnal.Flid);
				choices.Add(cFirstVern.Flid);
				Assert.That(choices.EnabledCount, Is.EqualTo(2));
				Assert.That(choices.EnabledLineSpecs[0].WritingSystem, Is.EqualTo(Cache.LangProject.DefaultAnalysisWritingSystem.Handle));
				Assert.That(choices.EnabledLineSpecs[1].WritingSystem, Is.EqualTo(Cache.LangProject.DefaultVernacularWritingSystem.Handle));
			}
		}

		[Test]
		public void CreateSpecForCustomAlwaysUsesDefaultWS()
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

			using (var cFirstAnal = new CustomFieldForTest(Cache,
				"Candy Apple Red Anal",
				Cache.MetaDataCacheAccessor.GetClassId("Segment"),
				WritingSystemServices.kwsAnal,
				CellarPropertyType.String,
				Guid.Empty))
			using (var cFirstVern = new CustomFieldForTest(Cache,
				"Candy Apple Red Vern",
				Cache.MetaDataCacheAccessor.GetClassId("Segment"),
				WritingSystemServices.kwsVern,
				CellarPropertyType.String,
				Guid.Empty))
			{
				InterlinLineChoices choices = new InterlinLineChoices(m_lp, wsFrn, wsEng);
				InterlinLineSpec analChoice = choices.CreateSpec(cFirstAnal.Flid, wsGer);
				InterlinLineSpec vernChoice = choices.CreateSpec(cFirstVern.Flid, wsGer);

				Assert.That(analChoice.WritingSystem, Is.EqualTo(Cache.LangProject.DefaultAnalysisWritingSystem.Handle));
				Assert.That(vernChoice.WritingSystem, Is.EqualTo(Cache.LangProject.DefaultVernacularWritingSystem.Handle));
			}
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
			InterlinLineSpec spec = choices.EnabledLineSpecs[1];
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
