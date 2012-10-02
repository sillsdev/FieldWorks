using System.Collections.Generic;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;

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
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[1].Flid);
			Assert.AreEqual(1, choices.FirstMorphemeIndex);	// morpheme line
			Assert.AreEqual(2, choices.FirstLexEntryIndex);	// lex entry
			Assert.IsFalse(choices[1].LexEntryLevel);	// morphemes
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

			// Add Morphemes and Lexentries lines above other MorphemeLevel lines by default.
			choices.Add(InterlinLineChoices.kflidLexEntries); // bring entries back in
			choices.Add(InterlinLineChoices.kflidMorphemes); // bring entries and morphemes back in
			Assert.AreEqual(7, choices.Count);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[2].Flid);
			choices.Remove(choices[2]); // and get rid of the entries
			Assert.AreEqual(6, choices.Count);
		}

		[Test]
		public void MoveUp()
		{
			InterlinLineChoices choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			// To make it less confusing, here we add them in an order that does not produce
			// reordering.
			MakeStandardState(choices);

			// lit trans can move up
			Assert.IsFalse(choices.OkToMoveUp(0)); // words line already at toop
			Assert.IsTrue(choices.OkToMoveUp(1)); // non-edit, morphemes can move up, group goes too.
			Assert.IsTrue(choices.OkToMoveUp(2));
			Assert.IsTrue(choices.OkToMoveUp(3));
			Assert.IsTrue(choices.OkToMoveUp(4));
			Assert.IsTrue(choices.OkToMoveUp(5)); // will move past whole morph bundle
			Assert.IsTrue(choices.OkToMoveUp(6));
			Assert.IsFalse(choices.OkToMoveUp(7)); // free can't go anywhere
			Assert.IsTrue(choices.OkToMoveUp(8));

			choices.MoveUp(1);
			// morphemes is now top
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[0].Flid);
			// Word moved down to position 4
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices[4].Flid);
			// Lex Gloss (to pick just one) also moved up
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[2].Flid);

			choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveUp(2);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[2].Flid);

			choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveUp(3);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[3].Flid);


			choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveUp(5);
			// Moves past whole morpheme bundle
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[3].Flid);

			choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveUp(6);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[6].Flid);

			choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveUp(8);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, choices[7].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices[8].Flid);
		}

		[Test]
		public void EditMoveUp()
		{
			InterlinLineChoices choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			// To make it less confusing, here we add them in an order that does not produce
			// reordering.
			MakeStandardState(choices);
			//choices.Add(InterlinLineChoices.kflidWord); // 0
			//choices.Add(InterlinLineChoices.kflidMorphemes); // 1
			//choices.Add(InterlinLineChoices.kflidLexEntries); //2
			//choices.Add(InterlinLineChoices.kflidLexGloss); //3
			//choices.Add(InterlinLineChoices.kflidLexPos); //4
			//choices.Add(InterlinLineChoices.kflidWordGloss); //5
			//choices.Add(InterlinLineChoices.kflidWordPos); //6
			//choices.Add(InterlinLineChoices.kflidFreeTrans); //7
			//choices.Add(InterlinLineChoices.kflidLitTrans); //8

			Assert.IsFalse(choices.OkToMoveUp(0)); // words line already at top
			Assert.IsTrue(choices.OkToMoveUp(1)); // morphemes
			Assert.IsTrue(choices.OkToMoveUp(2)); // lex entries
			Assert.IsTrue(choices.OkToMoveUp(3)); // lex gloss
			Assert.IsTrue(choices.OkToMoveUp(4)); // lex pos
			Assert.IsTrue(choices.OkToMoveUp(5)); // will move past whole morph bundle
			Assert.IsTrue(choices.OkToMoveUp(6));
			Assert.IsFalse(choices.OkToMoveUp(7)); // free can't go anywhere
			Assert.IsTrue(choices.OkToMoveUp(8));

			choices.MoveUp(4);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[4].Flid);

			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveUp(5);
			// Moves past whole morpheme bundle
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[3].Flid);

			// Now, the morphemes line can move up, to move the whole bundle.
			Assert.IsTrue(choices.OkToMoveUp(2)); // morphemes line now below word gloss.
			choices.MoveUp(2); // whole bundle moves.
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[2].Flid);

			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveUp(6);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[6].Flid);

			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveUp(8);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, choices[7].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices[8].Flid);

			// Try finding some default ws values.
			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			Assert.AreEqual(0, choices.Count);
			List<int> wsList = choices.WritingSystemsForFlid(InterlinLineChoices.kflidWord);
			Assert.AreEqual(0, wsList.Count);
			wsList = choices.WritingSystemsForFlid(InterlinLineChoices.kflidWord, true);
			Assert.AreEqual(1, wsList.Count);
			Assert.AreEqual(kwsVernInPara, wsList[0]);
			wsList = choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss);
			Assert.AreEqual(0, wsList.Count);
			wsList = choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss, true);
			Assert.AreEqual(1, wsList.Count);
			Assert.AreEqual(Cache.DefaultAnalWs, wsList[0]);

			// Try one with another WS.
			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			wsList = choices.WritingSystemsForFlid(InterlinLineChoices.kflidLexGloss);
			Assert.AreEqual(1, wsList.Count);
			Assert.AreEqual(WritingSystemServices.kwsFirstAnal, wsList[0]);
			choices.Add(InterlinLineChoices.kflidLexGloss, 3004); //becomes 4 (by default, after existing field with same flid)
			wsList = choices.WritingSystemsForFlid(InterlinLineChoices.kflidLexGloss);
			Assert.AreEqual(2, wsList.Count);
			Assert.AreEqual(WritingSystemServices.kwsFirstAnal, wsList[0]);
			Assert.AreEqual(3004, wsList[1]);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[4].Flid);
			choices.MoveDown(4);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[5].Flid);
			Assert.IsTrue(choices.OkToMoveUp(5));
			choices.MoveUp(5);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[4].Flid);
			Assert.AreEqual(3004, choices[4].WritingSystem);
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices[5].Flid);
			Assert.IsTrue(choices.OkToMoveUp(5));

			Assert.IsTrue(choices.OkToMoveUp(4));
			choices.MoveUp(4);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[3].Flid);
			Assert.AreEqual(3004, choices[3].WritingSystem);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[4].Flid);
			Assert.AreEqual(WritingSystemServices.kwsFirstAnal, choices[4].WritingSystem);
			Assert.IsTrue(choices.OkToMoveUp(3));

			// Another ws of entries can move up just as far (not past the primary
			// one that has the controls).
			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			wsList = choices.WritingSystemsForFlid(InterlinLineChoices.kflidLexEntries);
			Assert.AreEqual(1, wsList.Count);
			Assert.AreEqual(kwsVernInPara, wsList[0]);
			choices.Add(InterlinLineChoices.kflidLexEntries, 3005); //becomes 3
			wsList = choices.WritingSystemsForFlid(InterlinLineChoices.kflidLexEntries);
			Assert.AreEqual(2, wsList.Count);
			Assert.AreEqual(kwsVernInPara, wsList[0]);
			Assert.AreEqual(3005, wsList[1]);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[3].Flid);
			choices.MoveDown(3);
			choices.MoveDown(4);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[5].Flid);
			Assert.IsTrue(choices.OkToMoveUp(5));
			choices.MoveUp(5);
			Assert.IsTrue(choices.OkToMoveUp(4));
			choices.MoveUp(4);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[3].Flid);
			Assert.AreEqual(3005, choices[3].WritingSystem);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[4].Flid);
			Assert.AreEqual(WritingSystemServices.kwsFirstAnal, choices[4].WritingSystem);
			Assert.IsTrue(choices.OkToMoveUp(3)); // not past primary LexEntry.

			// Another ws of morphemes can move up just under the primary one (not past the primary
			// one that has the controls).
			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.Add(InterlinLineChoices.kflidMorphemes, 3006); //becomes 2
			wsList = choices.WritingSystemsForFlid(InterlinLineChoices.kflidMorphemes);
			Assert.AreEqual(2, wsList.Count);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[2].Flid);
			choices.MoveDown(2);
			choices.MoveDown(3);
			choices.MoveDown(4);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[5].Flid);
			Assert.IsTrue(choices.OkToMoveUp(5));
			choices.MoveUp(5);
			Assert.IsTrue(choices.OkToMoveUp(4));
			choices.MoveUp(4);
			Assert.IsTrue(choices.OkToMoveUp(3));
			choices.MoveUp(3);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[2].Flid);
			Assert.AreEqual(3006, choices[2].WritingSystem);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[3].Flid);
			Assert.IsTrue(choices.OkToMoveUp(2)); // not past primary morphemes.

			// Another ws of word starts out right after it.
			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.Add(InterlinLineChoices.kflidWord, 3007); //becomes 1
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices[1].Flid);
			wsList = choices.WritingSystemsForFlid(InterlinLineChoices.kflidWord);
			Assert.AreEqual(2, wsList.Count);
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
		[Test]
		public void MoveDown()
		{
			InterlinLineChoices choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			// To make it less confusing, here we add them in an order that does not produce
			// reordering.
			MakeStandardState(choices);

			// lit trans can move up
			Assert.IsTrue(choices.OkToMoveDown(0)); // most moves are OK in non-edit mode
			Assert.IsTrue(choices.OkToMoveDown(1));
			Assert.IsTrue(choices.OkToMoveDown(2));
			Assert.IsTrue(choices.OkToMoveDown(3));
			Assert.IsTrue(choices.OkToMoveDown(4)); // whole morph bundle will move down
			Assert.IsTrue(choices.OkToMoveDown(5));
			Assert.IsFalse(choices.OkToMoveDown(6)); // Would put FF out of order
			Assert.IsTrue(choices.OkToMoveDown(7));
			Assert.IsFalse(choices.OkToMoveDown(8));

			choices.MoveDown(0);
			// morphemes is now top
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[0].Flid);
			// Word moved down to position 4
			Assert.AreEqual(InterlinLineChoices.kflidWord, choices[4].Flid);
			// Lex Gloss (to pick just one) also moved up
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[2].Flid);

			choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveDown(1);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[2].Flid);

			choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveDown(4);
			// Moves past whole morpheme bundle
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[3].Flid);

			choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveDown(5);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[6].Flid);

			choices = new InterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveDown(7);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, choices[7].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices[8].Flid);
		}
		[Test]
		public void EditMoveDown()
		{
			InterlinLineChoices choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			// To make it less confusing, here we add them in an order that does not produce
			// reordering.
			MakeStandardState(choices);

			//choices.Add(InterlinLineChoices.kflidWord); // 0
			//choices.Add(InterlinLineChoices.kflidMorphemes); // 1
			//choices.Add(InterlinLineChoices.kflidLexEntries); //2
			//choices.Add(InterlinLineChoices.kflidLexGloss); //3
			//choices.Add(InterlinLineChoices.kflidLexPos); //4
			//choices.Add(InterlinLineChoices.kflidWordGloss); //5
			//choices.Add(InterlinLineChoices.kflidWordPos); //6
			//choices.Add(InterlinLineChoices.kflidFreeTrans); //7
			//choices.Add(InterlinLineChoices.kflidLitTrans); //8

			Assert.IsTrue(choices.OkToMoveDown(0)); // word line
			Assert.IsTrue(choices.OkToMoveDown(1)); // morphemes
			Assert.IsTrue(choices.OkToMoveDown(2)); // lex entries
			Assert.IsTrue(choices.OkToMoveDown(3));  // lex gloss
			Assert.IsTrue(choices.OkToMoveDown(4));	 // lex pos -- whole morph bundle moves down
			Assert.IsTrue(choices.OkToMoveDown(5));  // Word gloss
			Assert.IsFalse(choices.OkToMoveDown(6)); // Word pos can't go past free
			Assert.IsTrue(choices.OkToMoveDown(7));	 // free trans
			Assert.IsFalse(choices.OkToMoveDown(8)); // lit trans -- last can't go down

			choices.MoveDown(3);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidLexPos, choices[3].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexGloss, choices[4].Flid);

			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveDown(4);
			// Moves past whole morpheme bundle
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[2].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[3].Flid);

			// Now, the morphemes line can move up, to move the whole bundle.
			Assert.IsTrue(choices.OkToMoveDown(1)); // morphemes line now below word gloss.
			choices.MoveDown(1); // whole bundle moves.
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidMorphemes, choices[1].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidLexEntries, choices[2].Flid);

			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveDown(5);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidWordPos, choices[5].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidWordGloss, choices[6].Flid);

			choices = new EditableInterlinLineChoices(m_lp, kwsVernInPara, kwsAnalysis);
			MakeStandardState(choices);
			choices.MoveDown(7);
			// nothing complicated, two items changed place.
			Assert.AreEqual(InterlinLineChoices.kflidLitTrans, choices[7].Flid);
			Assert.AreEqual(InterlinLineChoices.kflidFreeTrans, choices[8].Flid);

			// Left out a lot of the complicated cases, since move down is just implemented
			// as an inverse MoveUp.
		}

		[Test]
		public void Persistence()
		{
			var wsManager = new PalasoWritingSystemManager();
			IWritingSystem enWs;
			wsManager.GetOrSet("en", out enWs);
			int wsEng = enWs.Handle;

			IWritingSystem frWs;
			wsManager.GetOrSet("fr", out frWs);
			int wsFrn = frWs.Handle;

			IWritingSystem deWs;
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
	}
}
