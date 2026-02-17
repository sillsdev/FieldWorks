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
			Assert.That(choices.EnabledLineSpecs[0].Flid, Is.EqualTo(InterlinLineChoices.kflidWord));
			Assert.That(choices.EnabledLineSpecs[1].Flid, Is.EqualTo(InterlinLineChoices.kflidLexEntries));
			// This gets reordered to keep the interlinears together.
			Assert.That(choices.EnabledLineSpecs[2].Flid, Is.EqualTo(InterlinLineChoices.kflidLexPos));
			// reordered past ff and word level
			Assert.That(choices.EnabledLineSpecs[3].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(choices.EnabledLineSpecs[4].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			// inserted third, but other things push past it.
			Assert.That(choices.EnabledLineSpecs[5].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			// reordered past a freeform.
			Assert.That(choices.EnabledLineSpecs[6].Flid, Is.EqualTo(InterlinLineChoices.kflidWordPos));
			Assert.That(choices.EnabledLineSpecs[7].Flid, Is.EqualTo(InterlinLineChoices.kflidFreeTrans));

			// Check writing systems assigned by default.
			Assert.That(choices.EnabledLineSpecs[0].WritingSystem, Is.EqualTo(kwsVernInPara));
			Assert.That(choices.EnabledLineSpecs[3].WritingSystem, Is.EqualTo(WritingSystemServices.kwsFirstAnal));
			Assert.That(choices.EnabledLineSpecs[4].WritingSystem, Is.EqualTo(3003));

			// Check field levels
			Assert.That(choices.EnabledLineSpecs[0].WordLevel, Is.True);
			Assert.That(choices.EnabledLineSpecs[1].WordLevel, Is.True);
			Assert.That(choices.EnabledLineSpecs[7].WordLevel, Is.False);

			Assert.That(choices.EnabledLineSpecs[0].MorphemeLevel, Is.False);
			Assert.That(choices.EnabledLineSpecs[1].MorphemeLevel, Is.True);
			Assert.That(choices.EnabledLineSpecs[6].MorphemeLevel, Is.False);
			Assert.That(choices.FirstEnabledMorphemeIndex, Is.EqualTo(1));
			Assert.That(choices.FirstEnabledLexEntryIndex, Is.EqualTo(1));

			Assert.That(choices.EnabledLineSpecs[1].LexEntryLevel, Is.True);   // lex entries
			Assert.That(choices.EnabledLineSpecs[2].LexEntryLevel, Is.True);   // lex pos
			Assert.That(choices.EnabledLineSpecs[3].LexEntryLevel, Is.True);	// lex gloss
			Assert.That(choices.EnabledLineSpecs[4].LexEntryLevel, Is.True);	// lex gloss
			Assert.That(choices.EnabledLineSpecs[0].LexEntryLevel, Is.False);	// word
			Assert.That(choices.EnabledLineSpecs[5].LexEntryLevel, Is.False);	// word gloss
			Assert.That(choices.EnabledLineSpecs[6].LexEntryLevel, Is.False);	// word pos
			Assert.That(choices.EnabledLineSpecs[7].LexEntryLevel, Is.False);	// free trans

			choices.Add(InterlinLineChoices.kflidMorphemes);
			Assert.That(choices.EnabledLineSpecs[5].Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
			Assert.That(choices.FirstEnabledMorphemeIndex, Is.EqualTo(1));	// first morpheme group line
			Assert.That(choices.FirstEnabledLexEntryIndex, Is.EqualTo(1));	// lex entry
			Assert.That(choices.EnabledLineSpecs[5].LexEntryLevel, Is.False);	// morphemes
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

			Assert.That(choices.EnabledLineSpecs[0].Flid, Is.EqualTo(InterlinLineChoices.kflidWord));
			Assert.That(choices.EnabledLineSpecs[1].Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
			// reordered past ff and word level
			Assert.That(choices.EnabledLineSpecs[2].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(choices.EnabledLineSpecs[3].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			// reordered past a freeform.
			Assert.That(choices.EnabledLineSpecs[4].Flid, Is.EqualTo(InterlinLineChoices.kflidWordPos));
			Assert.That(choices.EnabledLineSpecs[5].Flid, Is.EqualTo(InterlinLineChoices.kflidFreeTrans));

			// We can't remove the Word line.
			string msg;
			Assert.That(choices.OkToRemove(choices.EnabledLineSpecs[0], out msg), Is.False);
			Assert.That(msg, Is.Not.Null);

			// Cannot add duplicates.
			var beforeCount = choices.AllLineSpecs.Count;
			choices.Add(InterlinLineChoices.kflidWord);
			Assert.That(choices.AllLineSpecs.Count, Is.EqualTo(beforeCount));

			// Other fields can be removed freely
			Assert.That(choices.OkToRemove(choices.EnabledLineSpecs[1], out msg), Is.True);
			Assert.That(msg, Is.Null);
			Assert.That(choices.OkToRemove(choices.EnabledLineSpecs[2], out msg), Is.True);
			Assert.That(msg, Is.Null);
			Assert.That(choices.OkToRemove(choices.EnabledLineSpecs[3], out msg), Is.True);
			Assert.That(msg, Is.Null);
			Assert.That(choices.OkToRemove(choices.EnabledLineSpecs[4], out msg), Is.True);
			Assert.That(msg, Is.Null);
			Assert.That(choices.OkToRemove(choices.EnabledLineSpecs[5], out msg), Is.True);
			Assert.That(msg, Is.Null);

			// Check what goes along with the morphemes line: morpheme line should be independent (LT-6043).
			choices.Remove(choices.EnabledLineSpecs[1]);
			Assert.That(choices.EnabledLineSpecs[0].Flid, Is.EqualTo(InterlinLineChoices.kflidWord));
			// reordered past ff and word level
			Assert.That(choices.EnabledLineSpecs[1].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(choices.EnabledLineSpecs[2].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			// reordered past a freeform.
			Assert.That(choices.EnabledLineSpecs[3].Flid, Is.EqualTo(InterlinLineChoices.kflidWordPos));
			Assert.That(choices.EnabledLineSpecs[4].Flid, Is.EqualTo(InterlinLineChoices.kflidFreeTrans));
			Assert.That(choices.EnabledCount, Is.EqualTo(5));

			// Add Morphemes and Lexentries lines at the end of the other morpheme group rows.
			choices.Add(InterlinLineChoices.kflidLexEntries); // bring entries back in
			choices.Add(InterlinLineChoices.kflidMorphemes); // bring entries and morphemes back in
			Assert.That(choices.EnabledCount, Is.EqualTo(7));
			// in 9.1 we have removed the restrictions that the Morphemes and LexEntries lines be at the top
			Assert.That(choices.EnabledLineSpecs[2].Flid, Is.EqualTo(InterlinLineChoices.kflidLexEntries));
			Assert.That(choices.EnabledLineSpecs[3].Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
			choices.Remove(choices.EnabledLineSpecs[2]); // and get rid of the entries
			Assert.That(choices.EnabledCount, Is.EqualTo(6));
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

			choices = new EditableInterlinLineChoices(m_lp, 0, wsEng);
			MakeStandardState(choices);
			choices.Add(InterlinLineChoices.kflidLexGloss, wsGer);
			Assert.That(choices.EnabledLineSpecs[0].Flid, Is.EqualTo(InterlinLineChoices.kflidWord));
			Assert.That(choices.EnabledLineSpecs[1].Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
			Assert.That(choices.EnabledLineSpecs[2].Flid, Is.EqualTo(InterlinLineChoices.kflidLexEntries));
			Assert.That(choices.EnabledLineSpecs[3].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(choices.EnabledLineSpecs[4].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(choices.EnabledLineSpecs[5].Flid, Is.EqualTo(InterlinLineChoices.kflidLexPos));
			Assert.That(choices.EnabledLineSpecs[6].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			Assert.That(choices.EnabledLineSpecs[7].Flid, Is.EqualTo(InterlinLineChoices.kflidWordPos));
			Assert.That(choices.EnabledLineSpecs[8].Flid, Is.EqualTo(InterlinLineChoices.kflidFreeTrans));
			Assert.That(choices.EnabledLineSpecs[9].Flid, Is.EqualTo(InterlinLineChoices.kflidLitTrans));

			Assert.That(choices.EnabledLineSpecs[4].WritingSystem, Is.EqualTo(wsGer));
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
			Assert.That(choices.AllLineSpecs.Count, Is.EqualTo(9));
			Assert.That(choices.EnabledLineSpecs.Count, Is.EqualTo(7));
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
			Assert.That(choices.AllLineSpecs[2].Enabled, Is.EqualTo(false));
			Assert.That(choices.AllLineSpecs[8].Enabled, Is.EqualTo(false));

			string persist = choices.Persist(wsManager);
			choices = InterlinLineChoices.Restore(persist, wsManager, m_lp, wsFrn, wsEng);

			Assert.That(choices.AllLineSpecs[2].Enabled, Is.EqualTo(false));
			Assert.That(choices.AllLineSpecs[8].Enabled, Is.EqualTo(false));
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
			Assert.That(choices.AllLineSpecs.Count, Is.EqualTo(11));
			Assert.That(choices.AllLineSpecs[0].Flid, Is.EqualTo(InterlinLineChoices.kflidWord));
			Assert.That(choices.AllLineSpecs[1].Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
			Assert.That(choices.AllLineSpecs[2].Flid, Is.EqualTo(InterlinLineChoices.kflidLexEntries));
			Assert.That(choices.AllLineSpecs[3].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(choices.AllLineSpecs[4].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(choices.AllLineSpecs[5].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(choices.AllLineSpecs[6].Flid, Is.EqualTo(InterlinLineChoices.kflidLexPos));
			Assert.That(choices.AllLineSpecs[7].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			Assert.That(choices.AllLineSpecs[8].Flid, Is.EqualTo(InterlinLineChoices.kflidWordPos));
			Assert.That(choices.AllLineSpecs[9].Flid, Is.EqualTo(InterlinLineChoices.kflidLitTrans));
			Assert.That(choices.AllLineSpecs[10].Flid, Is.EqualTo(InterlinLineChoices.kflidFreeTrans));

			Assert.That(choices.AllLineSpecs[3].WritingSystem, Is.EqualTo(wsEng));
			Assert.That(choices.AllLineSpecs[4].WritingSystem, Is.EqualTo(wsFrn));
			Assert.That(choices.AllLineSpecs[5].WritingSystem, Is.EqualTo(wsGer));

			ReadOnlyCollection<LineOption> configLineOptions = choices.ConfigurationLineOptions;

			// Post-checks
			Assert.That(configLineOptions.Count, Is.EqualTo(10)); // 9 + 1 for kflidNote.
			Assert.That(configLineOptions[0].Flid, Is.EqualTo(InterlinLineChoices.kflidWord));
			Assert.That(configLineOptions[1].Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
			Assert.That(configLineOptions[2].Flid, Is.EqualTo(InterlinLineChoices.kflidLexEntries));
			Assert.That(configLineOptions[3].Flid, Is.EqualTo(InterlinLineChoices.kflidLexGloss));
			Assert.That(configLineOptions[4].Flid, Is.EqualTo(InterlinLineChoices.kflidLexPos));
			Assert.That(configLineOptions[5].Flid, Is.EqualTo(InterlinLineChoices.kflidWordGloss));
			Assert.That(configLineOptions[6].Flid, Is.EqualTo(InterlinLineChoices.kflidWordPos));
			Assert.That(configLineOptions[7].Flid, Is.EqualTo(InterlinLineChoices.kflidLitTrans));
			Assert.That(configLineOptions[8].Flid, Is.EqualTo(InterlinLineChoices.kflidFreeTrans));
			// kflidNote is one of the required options so it was added.
			Assert.That(configLineOptions[9].Flid, Is.EqualTo(InterlinLineChoices.kflidNote));
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
			Assert.That(spec.Flid, Is.EqualTo(InterlinLineChoices.kflidMorphemes));
			// The StringFlid for this line spec always corresponds to a MoForm
			Assert.That(spec.StringFlid, Is.EqualTo(MoFormTags.kflidForm));

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
			Assert.That(spec.Flid == InterlinLineChoices.kflidMorphemes, Is.True);
			int wmbWs = spec.GetActualWs(Cache, wmb.Hvo, spec.WritingSystem);
			int mfWs = spec.GetActualWs(Cache, wmb.MorphRA.Hvo, spec.WritingSystem);
			Assert.That(wmbWs == spec.WritingSystem, Is.True);
			Assert.That(mfWs == spec.WritingSystem, Is.True);
		}
	}
}
