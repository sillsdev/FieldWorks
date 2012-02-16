// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2011' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PhraseTranslationHelperTests.cs
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.Utils;

namespace SILUBS.PhraseTranslationHelper
{
	[TestFixture]
	public class PhraseTranslationHelperTests
	{
		private List<IKeyTerm> m_dummyKtList;
		private KeyTermRules m_keyTermRules;

		[SetUp]
		public void Setup()
		{
			m_dummyKtList = new List<IKeyTerm>();
			m_keyTermRules = null;
		}

		#region List sorting tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting phrases, where the phrases that have parts that are used by lots of
		/// other phrases sort first in the list. Specifically, based on the number of owning
		/// phrases of the part that has the fewest. If they have the same min, then the phrase
		/// with the fewest parts should sort first. If they are still equal, the one with a
		/// part that has a maximum number of owning phrases should sort first. Otherwise, sort
		/// by reference. (Hard to explain)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesSortedByDefault()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", 1, 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),       // what would (1)     | me to (3)       | with respect to (3)
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),   // what is (3)        | asking (1)      | me to (3)           | with respect to (3) | that dog (4)
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),                                                  // that dog (4)
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // is it okay for (1) | me to (3)       | talk (1)            | with respect to (3) | today (1)
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0),          // that dog (4)       | wishes this (1) | and (1)             | what is (3)         | radish (1)
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},                                        // what is (3)        | that dog (4)
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			Assert.AreEqual("C", pth[0].Reference);
			Assert.AreEqual("F", pth[1].Reference);
			Assert.AreEqual("A", pth[2].Reference);
			Assert.AreEqual("B", pth[3].Reference);
			Assert.AreEqual("E", pth[4].Reference);
			Assert.AreEqual("D", pth[5].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting phrases sorted alphabetically by the English original.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesSortedByOriginalPhrase()
		{
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0),
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			pth.Sort(PhraseTranslationHelper.SortBy.EnglishPhrase, true);

			Assert.AreEqual("D", pth[0].Reference);
			Assert.AreEqual("C", pth[1].Reference);
			Assert.AreEqual("E", pth[2].Reference);
			Assert.AreEqual("B", pth[3].Reference);
			Assert.AreEqual("F", pth[4].Reference);
			Assert.AreEqual("A", pth[5].Reference);

			pth.Sort(PhraseTranslationHelper.SortBy.EnglishPhrase, false);

			Assert.AreEqual("A", pth[0].Reference);
			Assert.AreEqual("F", pth[1].Reference);
			Assert.AreEqual("B", pth[2].Reference);
			Assert.AreEqual("E", pth[3].Reference);
			Assert.AreEqual("C", pth[4].Reference);
			Assert.AreEqual("D", pth[5].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting phrases sorted by reference (all unique, all in the same category)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesSortedByReference()
		{
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0),
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			pth.Sort(PhraseTranslationHelper.SortBy.Reference, true);

			Assert.AreEqual("A", pth[0].Reference);
			Assert.AreEqual("B", pth[1].Reference);
			Assert.AreEqual("C", pth[2].Reference);
			Assert.AreEqual("D", pth[3].Reference);
			Assert.AreEqual("E", pth[4].Reference);
			Assert.AreEqual("F", pth[5].Reference);

			pth.Sort(PhraseTranslationHelper.SortBy.Reference, false);

			Assert.AreEqual("F", pth[0].Reference);
			Assert.AreEqual("E", pth[1].Reference);
			Assert.AreEqual("D", pth[2].Reference);
			Assert.AreEqual("C", pth[3].Reference);
			Assert.AreEqual("B", pth[4].Reference);
			Assert.AreEqual("A", pth[5].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting phrases sorted by category, reference and sequence number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesSortedByReferenceCategoryAndSequenceNum()
		{
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What is the meaning of life?", "A-D", 1, 4), 0, 0),
				new TranslatablePhrase(new TestQ("Why am I here?", "A-D", 1, 4), 0, 1),
				new TranslatablePhrase(new TestQ("What would God do?", "A", 1, 1), 1, 0),
				new TranslatablePhrase(new TestQ("What is Paul asking that man?", "A", 1, 1), 1, 1),
				new TranslatablePhrase(new TestQ("When is the best time for ice cream?", "C", 3, 3), 1, 0),
				new TranslatablePhrase(new TestQ("Is it okay for Paul to talk to God today?", "D", 4, 4), 1, 0),
				new TranslatablePhrase(new TestQ("Is a dog man's best friend?", "D", 4, 4), 1, 1),
				new TranslatablePhrase(new TestQ("Why is there evil?", "E-G", 5, 6), 0, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "E", 5, 5), 1, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "E-F", 5, 6), 1, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "E-G", 5, 7), 1, 0)},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			pth.Sort(PhraseTranslationHelper.SortBy.Reference, true);

			Assert.AreEqual("A-D", pth[0].Reference);
			Assert.AreEqual(0, pth[0].SequenceNumber);
			Assert.AreEqual("A-D", pth[1].Reference);
			Assert.AreEqual(1, pth[1].SequenceNumber);
			Assert.AreEqual("A", pth[2].Reference);
			Assert.AreEqual(0, pth[2].SequenceNumber);
			Assert.AreEqual("A", pth[3].Reference);
			Assert.AreEqual(1, pth[3].SequenceNumber);
			Assert.AreEqual("C", pth[4].Reference);
			Assert.AreEqual("D", pth[5].Reference);
			Assert.AreEqual(0, pth[5].SequenceNumber);
			Assert.AreEqual("D", pth[6].Reference);
			Assert.AreEqual(1, pth[6].SequenceNumber);
			Assert.AreEqual("E-G", pth[7].Reference);
			Assert.AreEqual(0, pth[7].Category);
			Assert.AreEqual("E", pth[8].Reference);
			Assert.AreEqual("E-F", pth[9].Reference);
			Assert.AreEqual("E-G", pth[10].Reference);
			Assert.AreEqual(1, pth[10].Category);

			pth.Sort(PhraseTranslationHelper.SortBy.Reference, false);

			Assert.AreEqual("E-G", pth[0].Reference);
			Assert.AreEqual(1, pth[0].Category);
			Assert.AreEqual("E-F", pth[1].Reference);
			Assert.AreEqual("E", pth[2].Reference);
			Assert.AreEqual("E-G", pth[3].Reference);
			Assert.AreEqual(0, pth[3].Category);
			Assert.AreEqual("D", pth[4].Reference);
			Assert.AreEqual(1, pth[4].SequenceNumber);
			Assert.AreEqual("D", pth[5].Reference);
			Assert.AreEqual(0, pth[5].SequenceNumber);
			Assert.AreEqual("C", pth[6].Reference);
			Assert.AreEqual("A", pth[7].Reference);
			Assert.AreEqual(1, pth[7].SequenceNumber);
			Assert.AreEqual("A", pth[8].Reference);
			Assert.AreEqual(0, pth[8].SequenceNumber);
			Assert.AreEqual("A-D", pth[9].Reference);
			Assert.AreEqual(1, pth[9].SequenceNumber);
			Assert.AreEqual("A-D", pth[10].Reference);
			Assert.AreEqual(0, pth[10].SequenceNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting phrases sorted alphabetically by the translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesSortedByTranslation()
		{
			TranslatablePhrase tp1 = new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0);
			TranslatablePhrase tp2 = new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0);
			TranslatablePhrase tp3 = new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0);
			TranslatablePhrase tp4 = new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0);
			TranslatablePhrase tp5 = new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0);
			TranslatablePhrase tp6 = new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { tp1, tp2, tp3, tp4, tp5, tp6 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());

			tp1.Translation = "Z";
			tp2.Translation = "B";
			tp3.Translation = "alligator";
			tp4.Translation = "D";
			tp5.Translation = "e";
			tp6.Translation = "E";

			pth.Sort(PhraseTranslationHelper.SortBy.Translation, true);

			Assert.AreEqual(tp3, pth[0]);
			Assert.AreEqual(tp2, pth[1]);
			Assert.AreEqual(tp4, pth[2]);
			Assert.AreEqual(tp5, pth[3]);
			Assert.AreEqual(tp6, pth[4]);
			Assert.AreEqual(tp1, pth[5]);

			pth.Sort(PhraseTranslationHelper.SortBy.Translation, false);

			Assert.AreEqual(tp1, pth[0]);
			Assert.AreEqual(tp6, pth[1]);
			Assert.AreEqual(tp5, pth[2]);
			Assert.AreEqual(tp4, pth[3]);
			Assert.AreEqual(tp2, pth[4]);
			Assert.AreEqual(tp3, pth[5]);
		}
		#endregion

		#region List filtering tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering phrases textually with the whole-word-match option
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredTextually_WholeWordMatch()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", 1, 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),       // what would (1)     | me to (3)       | with respect to (3)
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),   // what is (3)        | asking (1)      | me to (3)           | with respect to (3) | that dog (4)
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),                                                  // that dog (4)
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // is it okay for (1) | me to (3)       | talk (1)            | with respect to (3) | today (1)
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0),          // that dog (4)       | wishes this (1) | and (1)             | what is (3)         | radish (1)
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},                                        // what is (3)        | that dog (4)
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter("what is", true, PhraseTranslationHelper.KeyTermFilterType.All, null, false);
			Assert.AreEqual(3, pth.Phrases.Count(), "Wrong number of phrases in helper");

			Assert.AreEqual("F", pth[0].Reference);
			Assert.AreEqual("B", pth[1].Reference);
			Assert.AreEqual("E", pth[2].Reference);

			pth.Filter("what is Pau", true, PhraseTranslationHelper.KeyTermFilterType.All, null, false);
			Assert.AreEqual(0, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter("what is Paul", true, PhraseTranslationHelper.KeyTermFilterType.All, null, false);
			Assert.AreEqual(1, pth.Phrases.Count(), "Wrong number of phrases in helper");
			Assert.AreEqual("B", pth[0].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering phrases textually with the whole-word-match option
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredTextually_WholeWordMatch_IncludeExcluded()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", 1, 2, 5);

			TranslatablePhrase tp1 = new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0);       // what would (1)     | me to (3)       | with respect to (3)
			TranslatablePhrase tp2 = new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0);   // what is (3)        | asking (1)      | me to (3)           | with respect to (3) | that dog (4)
			TranslatablePhrase tp3 = new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0);                                                  // that dog (4)
			TranslatablePhrase tp4 = new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0); // is it okay for (1) | me to (3)       | talk (1)            | with respect to (3) | today (1)
			TranslatablePhrase tp5 = new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0);          // that dog (4)       | wishes this (1) | and (1)             | what is (3)         | radish (1)
			TranslatablePhrase tp6 = new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0);                                         // what is (3)        | that dog (4)
			tp2.IsExcluded = true;
			tp6.IsExcluded = true;

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { tp1, tp2, tp3, tp4, tp5, tp6},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter("what is", true, PhraseTranslationHelper.KeyTermFilterType.All, null, false);
			Assert.AreEqual(1, pth.Phrases.Count(), "Wrong number of phrases in helper");

			Assert.AreEqual(tp5, pth[0]);

			pth.Filter("what is", true, PhraseTranslationHelper.KeyTermFilterType.All, null, true);
			Assert.AreEqual(3, pth.Phrases.Count(), "Wrong number of phrases in helper");

			Assert.AreEqual(tp5, pth[0]);
			Assert.AreEqual(tp2, pth[1]);
			Assert.AreEqual(tp6, pth[2]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering phrases textually without the whole-word-match option
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredTextually_PartialWordMatch()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", 1, 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("This would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),       // what would (1)     | me to (3)       | with respect to (3)
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),   // what is (2)        | asking (1)      | me to (3)           | with respect to (3) | that dog (4)
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),                                                  // that dog (4)
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // is it okay for (1) | me to (3)       | talk (1)            | with respect to (3) | today (1)
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and say radish", "E", 5, 5), 1, 0),				  // that dog (4)       | wishes this (1) | and (1)             | radish (1)
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},                                        // what is (2)        | that dog (4)
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter("is", false, PhraseTranslationHelper.KeyTermFilterType.All, null, false);
			Assert.AreEqual(5, pth.Phrases.Count(), "Wrong number of phrases in helper");
			pth.Sort(PhraseTranslationHelper.SortBy.Reference, true);

			Assert.AreEqual("A", pth[0].Reference);
			Assert.AreEqual("B", pth[1].Reference);
			Assert.AreEqual("D", pth[2].Reference);
			Assert.AreEqual("E", pth[3].Reference);
			Assert.AreEqual("F", pth[4].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering phrases textually using a string that would result in a bogus
		/// regular expression if not properly escaped.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredTextually_PreventBogusRegExCrash()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", 1, 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("This would God have me [to] say with respect to Paul?", "A", 1, 1), 1, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "B", 6, 6), 1, 0)},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(2, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter("[", false, PhraseTranslationHelper.KeyTermFilterType.All, null, false);
			Assert.AreEqual(1, pth.Phrases.Count(), "Wrong number of phrases in helper");
			Assert.AreEqual("A", pth[0].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering by part using a string that doesn't even come close to matching any part
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredTextually_NoMatches()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", 1, 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),       // what would (1)     | me to (3)       | with respect to (3)
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),   // what is (3)        | asking (1)      | me to (3)           | with respect to (3) | that dog (4)
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),                                                  // that dog (4)
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // is it okay for (1) | me to (3)       | talk (1)            | with respect to (3) | today (1)
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0),          // that dog (4)       | wishes this (1) | and (1)             | what is (3)         | radish (1)
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},                                        // what is (3)        | that dog (4)
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter("finkelsteins", true, PhraseTranslationHelper.KeyTermFilterType.All, null, false);
			Assert.AreEqual(0, pth.Phrases.Count(), "Wrong number of phrases in helper");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering by ref
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredOnlyByRef()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", 1, 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),       // what would (1)     | me to (3)       | with respect to (3)
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),   // what is (3)        | asking (1)      | me to (3)           | with respect to (3) | that dog (4)
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),                                                  // that dog (4)
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // is it okay for (1) | me to (3)       | talk (1)            | with respect to (3) | today (1)
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0),          // that dog (4)       | wishes this (1) | and (1)             | what is (3)         | radish (1)
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},                                        // what is (3)        | that dog (4)
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter(null, false, PhraseTranslationHelper.KeyTermFilterType.All,
				((start, end, sref) => start >= 2 && end <= 5 && sref != "C"), false);
			pth.Sort(PhraseTranslationHelper.SortBy.Reference, true);
			Assert.AreEqual(3, pth.Phrases.Count(), "Wrong number of phrases in helper");
			Assert.AreEqual("B", pth[0].Reference);
			Assert.AreEqual("D", pth[1].Reference);
			Assert.AreEqual("E", pth[2].Reference);

			// Now remove the ref filter
			pth.Filter(null, false, PhraseTranslationHelper.KeyTermFilterType.All, null, false);
			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering by ref
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredOnlyByRef_IncludeExcluded()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", 1, 2, 5);

			TranslatablePhrase tp1 = new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0);       // what would (1)     | me to (3)       | with respect to (3)
			TranslatablePhrase tp2 = new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0);   // what is (3)        | asking (1)      | me to (3)           | with respect to (3) | that dog (4)
			TranslatablePhrase tp3 = new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0);                                                  // that dog (4)
			TranslatablePhrase tp4 = new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0); // is it okay for (1) | me to (3)       | talk (1)            | with respect to (3) | today (1)
			TranslatablePhrase tp5 = new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0);          // that dog (4)       | wishes this (1) | and (1)             | what is (3)         | radish (1)
			TranslatablePhrase tp6 = new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0);                                         // what is (3)        | that dog (4)
			tp4.IsExcluded = true;
			tp5.IsExcluded = true;

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { tp1, tp2, tp3, tp4, tp5, tp6 },
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter(null, false, PhraseTranslationHelper.KeyTermFilterType.All,
				((start, end, sref) => start >= 2 && end <= 5 && sref != "C"), false);
			pth.Sort(PhraseTranslationHelper.SortBy.Reference, true);
			Assert.AreEqual(1, pth.Phrases.Count(), "Wrong number of phrases in filtered list");
			Assert.AreEqual(tp2, pth[0]);

			pth.Filter(null, false, PhraseTranslationHelper.KeyTermFilterType.All,
				((start, end, sref) => start >= 2 && end <= 5 && sref != "C"), true);
			Assert.AreEqual(3, pth.Phrases.Count(), "Wrong number of phrases in filtered list");
			Assert.AreEqual(tp2, pth[0]);
			Assert.AreEqual(tp4, pth[1]);
			Assert.AreEqual(tp5, pth[2]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering for phrases where all key terms have renderings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredByKeyTerms_WithRenderings()
		{
			AddMockedKeyTerm("God", (string)null);
			AddMockedKeyTerm("Paul");
			AddMockedKeyTerm("have", (string)null);
			AddMockedKeyTerm("say");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),       // God & have don't have renderings
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // God doesn't have a rendering
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter(null, false, PhraseTranslationHelper.KeyTermFilterType.WithRenderings, null, false);
			pth.Sort(PhraseTranslationHelper.SortBy.Reference, true);
			Assert.AreEqual(4, pth.Phrases.Count(), "Wrong number of phrases in helper");

			Assert.AreEqual("B", pth[0].Reference);
			Assert.AreEqual("C", pth[1].Reference);
			Assert.AreEqual("E", pth[2].Reference);
			Assert.AreEqual("F", pth[3].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering for phrases where all key terms have renderings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredByKeyTerms_WithRenderings_IncludeExcluded()
		{
			AddMockedKeyTerm("God", (string)null);
			AddMockedKeyTerm("Paul");
			AddMockedKeyTerm("have", (string)null);
			AddMockedKeyTerm("say");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0) { IsExcluded = true },       // God & have don't have renderings
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0) { IsExcluded = true },
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // God doesn't have a rendering
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0) { IsExcluded = true },
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter(null, false, PhraseTranslationHelper.KeyTermFilterType.WithRenderings, null, false);
			pth.Sort(PhraseTranslationHelper.SortBy.Reference, true);
			Assert.AreEqual(2, pth.Phrases.Count(), "Wrong number of phrases in helper");

			Assert.AreEqual("C", pth[0].Reference);
			Assert.AreEqual("F", pth[1].Reference);

			pth.Filter(null, false, PhraseTranslationHelper.KeyTermFilterType.WithRenderings, null, true);
			Assert.AreEqual(5, pth.Phrases.Count(), "Wrong number of phrases in helper");

			Assert.AreEqual("A", pth[0].Reference); // Even though there is an unrendered term, this phrase gets included because we don't actually parse excluded phrases to look for key term matches.
			Assert.AreEqual("B", pth[1].Reference);
			Assert.AreEqual("C", pth[2].Reference);
			Assert.AreEqual("E", pth[3].Reference);
			Assert.AreEqual("F", pth[4].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering for phrases where any key terms don't have renderings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredByKeyTerms_WithoutRenderings()
		{
			AddMockedKeyTerm("God", (string)null);
			AddMockedKeyTerm("Paul");
			AddMockedKeyTerm("have", (string)null);
			AddMockedKeyTerm("say");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),       // God & have don't have renderings
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // God doesn't have a rendering
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter(null, false, PhraseTranslationHelper.KeyTermFilterType.WithoutRenderings, null, false);
			pth.Sort(PhraseTranslationHelper.SortBy.Reference, true);
			Assert.AreEqual(2, pth.Phrases.Count(), "Wrong number of phrases in helper");

			Assert.AreEqual("A", pth[0].Reference);
			Assert.AreEqual("D", pth[1].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting phrases filtered by part, retrieving only phrases whose key terms have
		/// renderings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredTextually_WholeWordMatch_KeyTermsWithRenderings()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", null, 1, 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),       // what would (1)     | me to (3)       | with respect to (3)
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),   // what is (3)        | asking (1)      | me to (3)           | with respect to (3) | that dog (4) **** "say" isn't rendered
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),                                                  // that dog (4)
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // is it okay for (1) | me to (3)       | talk (1)            | with respect to (3) | today (1)
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is have radish", "E", 5, 5), 1, 0),          // that dog (4)       | wishes this (1) | and (1)             | what is (3)         | radish (1)
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},                                        // what is (3)        | that dog (4)
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter("what is", true, PhraseTranslationHelper.KeyTermFilterType.WithRenderings, null, false);
			Assert.AreEqual(2, pth.Phrases.Count(), "Wrong number of phrases in helper");

			Assert.AreEqual("F", pth[0].Reference);
			Assert.AreEqual("E", pth[1].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting phrases filtered by part and reference, retrieving only phrases whose
		/// key terms have renderings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredTextuallyAndRef_WholeWordMatch_KeyTermsWithRenderings()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", null, 1, 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),       // what would (1)     | me to (3)       | with respect to (3)
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),   // what is (3)        | asking (1)      | me to (3)           | with respect to (3) | that dog (4) **** "say" isn't rendered
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),                                                  // that dog (4)
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // is it okay for (1) | me to (3)       | talk (1)            | with respect to (3) | today (1)
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is have radish", "E", 5, 5), 1, 0),          // that dog (4)       | wishes this (1) | and (1)             | what is (3)         | radish (1)
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},                                        // what is (3)        | that dog (4)
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter("what is", true, PhraseTranslationHelper.KeyTermFilterType.WithRenderings,
				((start, end, sref) => start < 6), false);
			Assert.AreEqual(1, pth.Phrases.Count(), "Wrong number of phrases in helper");

			Assert.AreEqual("E", pth[0].Reference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests filtering by part doing a partial match, retrieving only phrases whose key
		/// terms do NOT have renderings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPhrasesFilteredTextually_PartialMatchWithoutRenderings()
		{
			AddMockedKeyTerm("God", 1, 4);
			AddMockedKeyTerm("Paul", 1, 2, 5);
			AddMockedKeyTerm("have", 1, 2, 3, 4, 5, 6);
			AddMockedKeyTerm("say", null, 1, 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("This would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),       // what would (1)     | me to (3)       | with respect to (3)
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),   // what is (3)        | asking (1)      | me to (3)           | with respect to (3) | that dog (4)
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),                                                  // that dog (4)
				new TranslatablePhrase(new TestQ("Is it okay for Paul me to talk with respect to God today?", "D", 4, 4), 1, 0), // is it okay for (1) | me to (3)       | talk (1)            | with respect to (3) | today (1)
				new TranslatablePhrase(new TestQ("that dog wishes this Paul and what is say radish", "E", 5, 5), 1, 0),          // that dog (4)       | wishes this (1) | and (1)             | what is (3)         | radish (1)
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)},                                        // what is (3)        | that dog (4)
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			pth.Filter("is", false, PhraseTranslationHelper.KeyTermFilterType.WithoutRenderings, null, false);
			Assert.AreEqual(3, pth.Phrases.Count(), "Wrong number of phrases in helper");
			pth.Sort(PhraseTranslationHelper.SortBy.Reference, true);

			Assert.AreEqual("A", pth[0].Reference);
			Assert.AreEqual("B", pth[1].Reference);
			Assert.AreEqual("E", pth[2].Reference);
		}
		#endregion

		#region GetParts tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where an empty key terms list is supplied
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_EmptyKeyTermsList_NoPhraseSubstitutions()
		{
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new [] {
				new TranslatablePhrase(" What do  you think?"),
				new TranslatablePhrase("What  do you think it means to forgive?"),
				new TranslatablePhrase(string.Empty),
				new TranslatablePhrase("-OR-"),
				new TranslatablePhrase("How could I have forgotten the question mark"),
				new TranslatablePhrase("What do you think  it means to bless someone? "),
				new TranslatablePhrase("What is this? ")}, m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, " What do  you think?", "what do you think", 3);
			VerifyTranslatablePhrase(pth, "What  do you think it means to forgive?", "what do you think", 3, "it means to forgive", 1);
			VerifyTranslatablePhrase(pth, "-OR-", "-or-", 1);
			VerifyTranslatablePhrase(pth, "How could I have forgotten the question mark", "how could i have forgotten the question mark", 1);
			VerifyTranslatablePhrase(pth, "What do you think  it means to bless someone? ", "what do you think", 3, "it means to bless someone", 1);
			VerifyTranslatablePhrase(pth, "What is this? ", "what is this", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where a non-empty key terms list is supplied
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_KeyTermsList_NoPhraseSubstitutions()
		{
			AddMockedKeyTerm("John"); // The apostle
			AddMockedKeyTerm("John"); // The Baptist
			AddMockedKeyTerm("Paul");
			AddMockedKeyTerm("Mary");
			AddMockedKeyTerm("temple");
			AddMockedKeyTerm("forgive");
			AddMockedKeyTerm("bless");
			AddMockedKeyTerm("God");
			AddMockedKeyTerm("Jesus");
			AddMockedKeyTerm("sin");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("Who was John?"),
				new TranslatablePhrase("Who was Paul?"),
				new TranslatablePhrase("Who was Mary?"),
				new TranslatablePhrase("Who went to the well?"),
				new TranslatablePhrase("Who went to the temple?"),
				new TranslatablePhrase("What do you think it means to forgive?"),
				new TranslatablePhrase("What do you think it means to bless someone?"),
				new TranslatablePhrase("What do you think God wants you to do?"),
				new TranslatablePhrase("Why do you think God created man?"),
				new TranslatablePhrase("Why do you think God  sent Jesus to earth?"),
				new TranslatablePhrase("Who went to the well with Jesus?"),
				new TranslatablePhrase("Do you think God could forgive someone who sins?"),
				new TranslatablePhrase("What do you think it means to serve two masters?")},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(13, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "Who was John?", "who was", 3);
			VerifyTranslatablePhrase(pth, "Who was Paul?", "who was", 3);
			VerifyTranslatablePhrase(pth, "Who was Mary?", "who was", 3);
			VerifyTranslatablePhrase(pth, "Who went to the well?", "who went to the well", 2);
			VerifyTranslatablePhrase(pth, "Who went to the temple?", "who went to the", 1);
			VerifyTranslatablePhrase(pth, "What do you think it means to forgive?", "what do you think it means to", 3);
			VerifyTranslatablePhrase(pth, "What do you think it means to bless someone?", "what do you think it means to", 3, "someone", 1);
			VerifyTranslatablePhrase(pth, "What do you think God wants you to do?", "what", 1, "do you think", 2, "wants you to do", 1);
			VerifyTranslatablePhrase(pth, "Why do you think God created man?", "why do you think", 2, "created man", 1);
			VerifyTranslatablePhrase(pth, "Why do you think God  sent Jesus to earth?", "why do you think", 2, "sent", 1, "to earth", 1);
			VerifyTranslatablePhrase(pth, "Who went to the well with Jesus?", "who went to the well", 2, "with", 1);
			VerifyTranslatablePhrase(pth, "Do you think God could forgive someone who sins?", "do you think", 2, "could", 1, "someone who", 1);
			VerifyTranslatablePhrase(pth, "What do you think it means to serve two masters?", "what do you think it means to", 3, "serve two masters", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where an empty key terms list is supplied and
		/// there are phrases ignored/substituted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_EmptyKeyTermsList_PhraseSubstitutions()
		{
			List<Substitution> ignored = new List<Substitution>();
			ignored.Add(new Substitution("What do you think it means", "What means", false, false));
			ignored.Add(new Substitution("tHe", null, false, false));
			ignored.Add(new Substitution("do", string.Empty, false, false));

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("What do you think it means?"),
				new TranslatablePhrase("What do you think it means to forgive?"),
				new TranslatablePhrase(string.Empty),
				new TranslatablePhrase("-OR-"),
				new TranslatablePhrase("How could I have forgotten the question mark"),
				new TranslatablePhrase("What do you think it means to bless someone? "),
				new TranslatablePhrase("What means of support do disciples have?")},
				m_dummyKtList, m_keyTermRules, ignored);

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "What do you think it means?", "what means", 4);
			VerifyTranslatablePhrase(pth, "What do you think it means to forgive?", "what means", 4, "to forgive", 1);
			VerifyTranslatablePhrase(pth, "-OR-", "-or-", 1);
			VerifyTranslatablePhrase(pth, "How could I have forgotten the question mark", "how could i have forgotten question mark", 1);
			VerifyTranslatablePhrase(pth, "What do you think it means to bless someone? ", "what means", 4, "to bless someone", 1);
			VerifyTranslatablePhrase(pth, "What means of support do disciples have?", "what means", 4, "of support disciples have", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where a non-empty key terms list is supplied
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_KeyTermsList_Apostrophes()
		{
			AddMockedKeyTerm("(God described as the) Most High");
			AddMockedKeyTerm("(God of) hosts");
			AddMockedKeyTerm("God");
			AddMockedKeyTerm("heavenly creature, symbol of God's majesty and associated with his presence");
			AddMockedKeyTerm("(one's own) burial-place");
			AddMockedKeyTerm("to make straight (one's way)");
			AddMockedKeyTerm("love for one's fellow believer");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("If one gives one's word and doesn't keep it, is it sin?"),
				new TranslatablePhrase("Is there one way to heaven?"),
				new TranslatablePhrase("Is the Bible God's Word?"),
				new TranslatablePhrase("Who is God's one and only Son?"),
				new TranslatablePhrase("Can the God of hosts also be called the Most High God?"),
				new TranslatablePhrase("Should one be buried in one's own burial-place?"),
				new TranslatablePhrase("Do wise people make straight paths for themselves?"),
				new TranslatablePhrase("How can you tell if one has love for one's fellow believer?"),
				new TranslatablePhrase("Is the earth God's?")},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(9, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "If one gives one's word and doesn't keep it, is it sin?", "if one gives one's word and doesn't keep it is it sin", 1);
			VerifyTranslatablePhrase(pth, "Is there one way to heaven?", "is there one way to heaven", 1);
			VerifyTranslatablePhrase(pth, "Is the Bible God's Word?", "is the bible", 1, "word", 1);
			VerifyTranslatablePhrase(pth, "Who is God's one and only Son?", "who is", 1, "one and only son", 1);
			VerifyTranslatablePhrase(pth, "Can the God of hosts also be called the Most High God?", "can the", 1, "also be called the", 1);
			VerifyTranslatablePhrase(pth, "Should one be buried in one's own burial-place?", "should one be buried in", 1);
			VerifyTranslatablePhrase(pth, "Do wise people make straight paths for themselves?", "do wise people", 1, "paths for themselves", 1);
			VerifyTranslatablePhrase(pth, "How can you tell if one has love for one's fellow believer?", "how can you tell if one has", 1);
			VerifyTranslatablePhrase(pth, "Is the earth God's?", "is the earth", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where a non-empty key terms list is supplied
		/// and there are substitutions defined with regular expressions and case-sensitive
		/// matching.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_KeyTermsList_SimplePhraseSubstitutions()
		{
			AddMockedKeyTerm("John"); // The apostle
			AddMockedKeyTerm("John"); // The Baptist
			AddMockedKeyTerm("Paul");
			AddMockedKeyTerm("Mary");
			AddMockedKeyTerm("temple");
			AddMockedKeyTerm("forgive");
			AddMockedKeyTerm("bless");
			AddMockedKeyTerm("God");
			AddMockedKeyTerm("Jesus");
			AddMockedKeyTerm("sin");

			List<Substitution> ignored = new List<Substitution>();
			ignored.Add(new Substitution("What do you think it means", "What means", false, false));
			ignored.Add(new Substitution("the", null, false, false));
			ignored.Add(new Substitution("do", string.Empty, false, false));

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("Who was John?"),
				new TranslatablePhrase("Who was Paul?"),
				new TranslatablePhrase("Who was Mary?"),
				new TranslatablePhrase("Who went to the well?"),
				new TranslatablePhrase("Who went to the temple?"),
				new TranslatablePhrase("What do you think it means to forgive?"),
				new TranslatablePhrase("What do you think it means to bless someone?"),
				new TranslatablePhrase("What do you think God wants you to do?"),
				new TranslatablePhrase("Why do you think God created man?"),
				new TranslatablePhrase("Why do you think God  sent Jesus to the earth?"),
				new TranslatablePhrase("Who went to the well with Jesus?"),
				new TranslatablePhrase("Do you think God could forgive someone who sins?"),
				new TranslatablePhrase("What do you think it means to serve two masters?")},
				m_dummyKtList, m_keyTermRules, ignored);

			Assert.AreEqual(13, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "Who was John?", "who was", 3);
			VerifyTranslatablePhrase(pth, "Who was Paul?", "who was", 3);
			VerifyTranslatablePhrase(pth, "Who was Mary?", "who was", 3);
			VerifyTranslatablePhrase(pth, "Who went to the well?", "who went to well", 2);
			VerifyTranslatablePhrase(pth, "Who went to the temple?", "who went to", 1);
			VerifyTranslatablePhrase(pth, "What do you think it means to forgive?", "what means to", 3);
			VerifyTranslatablePhrase(pth, "What do you think it means to bless someone?", "what means to", 3, "someone", 1);
			VerifyTranslatablePhrase(pth, "What do you think God wants you to do?", "what", 1, "you think", 2, "wants you to", 1);
			VerifyTranslatablePhrase(pth, "Why do you think God created man?", "why you think", 2, "created man", 1);
			VerifyTranslatablePhrase(pth, "Why do you think God  sent Jesus to the earth?", "why you think", 2, "sent", 1, "to earth", 1);
			VerifyTranslatablePhrase(pth, "Who went to the well with Jesus?", "who went to well", 2, "with", 1);
			VerifyTranslatablePhrase(pth, "Do you think God could forgive someone who sins?", "you think", 2, "could", 1, "someone who", 1);
			VerifyTranslatablePhrase(pth, "What do you think it means to serve two masters?", "what means to", 3, "serve two masters", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where a non-empty key terms list is supplied
		/// and there are phrases ignored/substituted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_KeyTermsList_ComplexPhraseSubstitutions()
		{
			AddMockedKeyTerm("John"); // The apostle
			AddMockedKeyTerm("John"); // The Baptist
			AddMockedKeyTerm("Paul");
			AddMockedKeyTerm("Mary");
			AddMockedKeyTerm("altar");
			AddMockedKeyTerm("forgive");
			AddMockedKeyTerm("bless");
			AddMockedKeyTerm("God");
			AddMockedKeyTerm("Jesus");
			AddMockedKeyTerm("sin");

			List<Substitution> ignored = new List<Substitution>();
			ignored.Add(new Substitution("what do you think it means", "what means", false, true));
			ignored.Add(new Substitution(@"\ban\b", "a", true, false));
			ignored.Add(new Substitution(@"did (\S+) do", @"did $1", true, true));
			ignored.Add(new Substitution(@"ed\b", null, true, true));

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("What did John do?"),
				new TranslatablePhrase("What did Paul do?"),
				new TranslatablePhrase("Who was Mary?"),
				new TranslatablePhrase("Who walked on a wall?"),
				new TranslatablePhrase("Who walked on an altar?"),
				new TranslatablePhrase("What do you think it means to forgive?"),
				new TranslatablePhrase("what do you think it means to bless someone?"),
				new TranslatablePhrase("Did Mary do the right thing?")},
				m_dummyKtList, m_keyTermRules, ignored);

			Assert.AreEqual(8, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "What did John do?", "what did", 2);
			VerifyTranslatablePhrase(pth, "What did Paul do?", "what did", 2);
			VerifyTranslatablePhrase(pth, "Who was Mary?", "who was", 1);
			VerifyTranslatablePhrase(pth, "Who walked on a wall?", "who walk on a", 2, "wall", 1);
			VerifyTranslatablePhrase(pth, "Who walked on an altar?", "who walk on a", 2);
			VerifyTranslatablePhrase(pth, "What do you think it means to forgive?", "what do you think it means to", 1);
			VerifyTranslatablePhrase(pth, "what do you think it means to bless someone?", "what means to", 1, "someone", 1);
			VerifyTranslatablePhrase(pth, "Did Mary do the right thing?", "did", 1, "do the right thing", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where the key terms list contains some terms
		/// consisting of more than one word
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_MultiWordKeyTermsList()
		{
			AddMockedKeyTerm("to forgive (flamboyantly)");
			AddMockedKeyTerm("to forgive always and forever");
			AddMockedKeyTerm("high priest");
			AddMockedKeyTerm("God");
			AddMockedKeyTerm("sentence that is seven words long");
			AddMockedKeyTerm("sentence");
			AddMockedKeyTerm("seven");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("What do you think it means to forgive?"),
				new TranslatablePhrase("Bla bla bla to forgive always?"),
				new TranslatablePhrase("Please forgive!"),
				new TranslatablePhrase("Who do you think God wants you to forgive and why?"),
				new TranslatablePhrase("Can you say a sentence that is seven words long?"),
				new TranslatablePhrase("high priest"),
				new TranslatablePhrase("If the high priest wants you to forgive God, can he ask you using a sentence that is seven words long or not?"),
				new TranslatablePhrase("Is this sentence that is seven dwarves?")},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(8, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "What do you think it means to forgive?", "what do you think it means", 1);
			VerifyTranslatablePhrase(pth, "Bla bla bla to forgive always?", "bla bla bla", 1, "always", 1);
			VerifyTranslatablePhrase(pth, "Please forgive!", "please", 1);
			VerifyTranslatablePhrase(pth, "Who do you think God wants you to forgive and why?", "who do you think", 1, "wants you", 2, "and why", 1);
			VerifyTranslatablePhrase(pth, "Can you say a sentence that is seven words long?", "can you say a", 1);
			VerifyTranslatablePhrase(pth, "high priest");
			VerifyTranslatablePhrase(pth, "If the high priest wants you to forgive God, can he ask you using a sentence that is seven words long or not?",
				"if the", 1, "wants you", 2, "can he ask you using a", 1, "or not", 1);
			VerifyTranslatablePhrase(pth, "Is this sentence that is seven dwarves?", "is this", 1, "that is", 1, "dwarves", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where the phrase contains the word "Pharisees"
		/// This deals with a weakness in the original (v1) Porter Stemmer algortihm. (TXL-52)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_MatchPluralFormOfMultiSyllabicWordEndingInDoubleE()
		{
			AddMockedKeyTerm("pharisee");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("What did the pharisee want?"),
				new TranslatablePhrase("What did the pharisees eat?")},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(2, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "What did the pharisee want?", "what did the", 2, "want", 1);
			VerifyTranslatablePhrase(pth, "What did the pharisees eat?", "what did the", 2, "eat", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where the key terms list contains a term
		/// consisting of more than one word where there is a partial match that fails at the
		/// end of the phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_MultiWordKeyTermsList_AvoidFalseMatchAtEnd()
		{
			AddMockedKeyTerm("John");
			AddMockedKeyTerm("tell the good news");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("What did John tell the Christians?"),
				new TranslatablePhrase("Why should you tell the good news?")},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(2, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "What did John tell the Christians?", "what did", 1, "tell the christians", 1);
			VerifyTranslatablePhrase(pth, "Why should you tell the good news?", "why should you", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where a two consectutive key terms appear in a
		/// phrase
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_TwoConsecutiveKeyTerms()
		{
			AddMockedKeyTerm("John");
			AddMockedKeyTerm("sin");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("Did John sin when he told Herod that it was unlawful to marry Herodius?")},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(1, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "Did John sin when he told Herod that it was unlawful to marry Herodius?", "did", 1, "when he told herod that it was unlawful to marry herodius", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests breaking up phrases into parts where a terms list is supplied that contains
		/// words or morphemes that are optional (either explicitly indicated using parentheses
		/// or implicitly optional words, such as the word "to" followed by a verb.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_KeyTermsWithOptionalWords()
		{
			AddMockedKeyTerm("ask for (earnestly)");
			AddMockedKeyTerm("to sin");
			AddMockedKeyTerm("(things of) this life");
			AddMockedKeyTerm("(loving)kindness");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("Did Herod ask for John's head because he wanted to sin?"),
				new TranslatablePhrase("Did Jambres sin when he clung to the things of this life?"),
				new TranslatablePhrase("Whose lovingkindness is everlasting?"),
				new TranslatablePhrase("What did John ask for earnestly?"),
				new TranslatablePhrase("Is showing kindness in this life a way to earn salvation?")},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(5, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "Did Herod ask for John's head because he wanted to sin?", "did herod", 1, "john's head because he wanted", 1);
			VerifyTranslatablePhrase(pth, "Did Jambres sin when he clung to the things of this life?", "did jambres", 1, "when he clung to the", 1);
			VerifyTranslatablePhrase(pth, "Whose lovingkindness is everlasting?", "whose", 1, "is everlasting", 1);
			VerifyTranslatablePhrase(pth, "What did John ask for earnestly?", "what did john", 1);
			VerifyTranslatablePhrase(pth, "Is showing kindness in this life a way to earn salvation?", "is showing", 1, "in", 1, "a way to earn salvation", 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests sub-part matching logic in case where breaking a phrase into smaller subparts
		/// causes a remainder which is an existing part (in use in another phrase).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_SubPartBreakingCausesRemainderWhichIsAnExistingPart()
		{
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("Who was the man who went to the store?"),
				new TranslatablePhrase("Who was the man?"),
				new TranslatablePhrase("Who went to the store?"),
				new TranslatablePhrase("Who was the man with the goatee who went to the store?")},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(4, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "Who was the man who went to the store?", "who was the man", 3, "who went to the store", 3);
			VerifyTranslatablePhrase(pth, "Who was the man?", "who was the man", 3);
			VerifyTranslatablePhrase(pth, "Who went to the store?", "who went to the store", 3);
			VerifyTranslatablePhrase(pth, "Who was the man with the goatee who went to the store?", "who was the man", 3, "with the goatee", 1, "who went to the store", 3);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests sub-part matching logic in case where breaking a phrase into smaller subparts
		/// causes both a preceeding and a following remainder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_SubPartMatchInTheMiddle()
		{
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("Are you the one who knows the man who ate the monkey?"),
				new TranslatablePhrase("Who knows the man?")},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(2, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "Are you the one who knows the man who ate the monkey?",
				"are you the one", 1, "who knows the man", 2, "who ate the monkey", 1);
			VerifyTranslatablePhrase(pth, "Who knows the man?", "who knows the man", 2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests sub-part matching logic in case where a phrase could theoretically match a
		/// sub-phrase  on smoething other than a word boundary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParts_PreventMatchOfPartialWords()
		{
			AddMockedKeyTerm("think");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase("Was a man happy?"),
				new TranslatablePhrase("As a man thinks in his heart, how is he?")},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(2, pth.Phrases.Count(), "Wrong number of phrases in helper");

			VerifyTranslatablePhrase(pth, "Was a man happy?", "was a man happy", 1);
			VerifyTranslatablePhrase(pth, "As a man thinks in his heart, how is he?", "as a man", 1, "in his heart how is he", 1);
		}
		#endregion

		#region PartPatternMatches tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the PartPatternMatches method with phrases that consist of a single part.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PartPatternMatches_SinglePart()
		{
			TranslatablePhrase tp1 = new TranslatablePhrase("Wuzzee?");
			TranslatablePhrase tp2 = new TranslatablePhrase("Wuzzee!");
			TranslatablePhrase tp3 = new TranslatablePhrase("As a man thinks in his heart, how is he?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				tp1,
				tp2,
				tp3},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.IsTrue(tp1.PartPatternMatches(tp2));
			Assert.IsTrue(tp2.PartPatternMatches(tp1));
			Assert.IsTrue(tp2.PartPatternMatches(tp2));
			Assert.IsFalse(tp3.PartPatternMatches(tp2));
			Assert.IsFalse(tp3.PartPatternMatches(tp1));
			Assert.IsFalse(tp1.PartPatternMatches(tp3));
			Assert.IsFalse(tp2.PartPatternMatches(tp3));
			Assert.IsTrue(tp3.PartPatternMatches(tp3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the PartPatternMatches method with phrases that consist of a single part.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PartPatternMatches_OneTranslatablePartOneKeyTerm()
		{
			AddMockedKeyTerm("wunkers");
			AddMockedKeyTerm("punkers");

			TranslatablePhrase tp1 = new TranslatablePhrase("Wuzzee wunkers?");
			TranslatablePhrase tp2 = new TranslatablePhrase("Wuzzee punkers.");
			TranslatablePhrase tp3 = new TranslatablePhrase("Wuzzee wunkers!");
			TranslatablePhrase tp4 = new TranslatablePhrase("Wunkers wuzzee!");
			TranslatablePhrase tp5 = new TranslatablePhrase("A dude named punkers?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				tp1,
				tp2,
				tp3,
				tp4,
				tp5},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.IsTrue(tp1.PartPatternMatches(tp2));
			Assert.IsTrue(tp1.PartPatternMatches(tp3));
			Assert.IsFalse(tp1.PartPatternMatches(tp4));
			Assert.IsFalse(tp1.PartPatternMatches(tp5));

			Assert.IsTrue(tp2.PartPatternMatches(tp3));
			Assert.IsFalse(tp2.PartPatternMatches(tp4));
			Assert.IsFalse(tp2.PartPatternMatches(tp5));

			Assert.IsFalse(tp3.PartPatternMatches(tp4));
			Assert.IsFalse(tp3.PartPatternMatches(tp5));

			Assert.IsFalse(tp4.PartPatternMatches(tp5));
		}
		#endregion

		#region Translation tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation to null for a phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetNewTranslation_Null()
		{
			TranslatablePhrase phrase = new TranslatablePhrase(new TestQ("Who was the man?", "A", 1, 1), 1, 0);
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase}, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			phrase.Translation = null;

			Assert.AreEqual(0, phrase.Translation.Length);
			Assert.IsFalse(phrase.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a phrase when that whole phrase matches part of
		/// another phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetNewTranslation_AutoAcceptTranslationForAllIdenticalPhrases()
		{
			TranslatablePhrase phrase1 = new TranslatablePhrase(new TestQ("Who was the man?", "A", 1, 1), 1, 0);
			TranslatablePhrase phrase2 = new TranslatablePhrase(new TestQ("Where was the woman?", "A", 1, 1), 1, 0);
			TranslatablePhrase phrase3 = new TranslatablePhrase(new TestQ("Who was the man?", "B", 2, 2), 1, 0);
			TranslatablePhrase phrase4 = new TranslatablePhrase(new TestQ("Where was the woman?", "C", 3, 3), 1, 0);
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2, phrase3, phrase4 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			phrase1.Translation = "\u00BFQuie\u0301n era el hombre?";
			phrase2.Translation = "\u00BFDo\u0301nde estaba la mujer?";

			Assert.AreEqual(phrase1.Translation, phrase3.Translation);
			Assert.IsTrue(phrase3.HasUserTranslation);
			Assert.AreEqual(phrase2.Translation, phrase4.Translation);
			Assert.IsTrue(phrase4.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a phrase when that whole phrase matches part of
		/// another phrase. TXL-108
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetNewTranslation_AcceptTranslationConsistingOnlyOfInitialAndFinalPunctuation()
		{
			AddMockedKeyTerm("Paul", (string)null);
			AddMockedKeyTerm("Judas", (string)null);
			TranslatablePhrase phrase1 = new TranslatablePhrase(new TestQ("Who was Paul talking to?", "A", 1, 1), 1, 0);
			TranslatablePhrase phrase2 = new TranslatablePhrase(new TestQ("Who was Judas kissing?", "A", 1, 1), 1, 0);
			TranslatablePhrase phrase3 = new TranslatablePhrase(new TestQ("Why was Judas talking to Paul?", "B", 2, 2), 1, 0);
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2, phrase3 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			phrase1.Translation = "\u00BFCon quie\u0301n estaba hablando Pablo?";
			phrase2.HasUserTranslation = true;

			Assert.AreEqual("\u00BF?", phrase2.Translation);
			Assert.IsTrue(phrase2.HasUserTranslation);
			Assert.AreEqual(phrase2.Translation, phrase3.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a phrase when that whole phrase matches part of
		/// another phrase, even if it has an untranslated key term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetNewTranslation_AutoAcceptTranslationForAllIdenticalPhrases_WithUntranslatedKeyTerm()
		{
			AddMockedKeyTerm("man", (string)null);

			TranslatablePhrase phrase1 = new TranslatablePhrase(new TestQ("Who was the man?", "A", 1, 1), 1, 0);
			TranslatablePhrase phrase2 = new TranslatablePhrase(new TestQ("Who was the man?", "B", 2, 2), 1, 0);
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			phrase1.Translation = "\u00BFQuie\u0301n era el hombre?";

			Assert.AreEqual(phrase1.Translation, phrase2.Translation);
			Assert.IsTrue(phrase2.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a phrase when that whole phrase matches part of
		/// another phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetNewTranslation_WholePhraseMatchesPartOfAnotherPhrase()
		{
			TranslatablePhrase shortPhrase = new TranslatablePhrase("Who was the man?");
			TranslatablePhrase longPhrase = new TranslatablePhrase("Who was the man with the jar?");
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				shortPhrase, longPhrase }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(1, shortPhrase.TranslatableParts.Count());
			Assert.AreEqual(2, longPhrase.TranslatableParts.Count());

			string partTrans = "Quie\u0301n era el hombre";
			shortPhrase.Translation = partTrans + "?";

			Assert.AreEqual(partTrans + "?", shortPhrase.Translation);
			Assert.AreEqual(partTrans, shortPhrase[0].Translation);
			Assert.AreEqual(partTrans + "?", longPhrase.Translation);
			Assert.AreEqual(partTrans, longPhrase[0].Translation);
			Assert.AreEqual(0, longPhrase[1].Translation.Length);
			Assert.IsTrue(shortPhrase.HasUserTranslation);
			Assert.IsFalse(longPhrase.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a phrase when that whole phrase matches part of
		/// another phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeTranslation_WholePhraseMatchesPartOfAnotherPhrase()
		{
			TranslatablePhrase shortPhrase = new TranslatablePhrase("Who was the man?");
			TranslatablePhrase longPhrase = new TranslatablePhrase("Who was the man with the jar?");
			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				shortPhrase, longPhrase }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(1, shortPhrase.TranslatableParts.Count());
			Assert.AreEqual(2, longPhrase.TranslatableParts.Count());

			shortPhrase.Translation = "Quiem fue el hambre?";
			string partTrans = "Quie\u0301n era el hombre";
			string trans = "\u00BF" + partTrans + "?";
			shortPhrase.Translation = trans;

			Assert.AreEqual(trans, shortPhrase.Translation);
			Assert.AreEqual(partTrans, shortPhrase[0].Translation);
			Assert.AreEqual(trans, longPhrase.Translation);
			Assert.AreEqual(partTrans, longPhrase[0].Translation);
			Assert.AreEqual(0, longPhrase[1].Translation.Length);
			Assert.IsTrue(shortPhrase.HasUserTranslation);
			Assert.IsFalse(longPhrase.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for two phrases that have a common part and verify
		/// that a third phrase that has that part shows the translation of the translated part.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_GuessAtPhraseTranslationBasedOnTriangulation()
		{
			AddMockedKeyTerm("Jesus");
			AddMockedKeyTerm("lion");
			AddMockedKeyTerm("jar");

			TranslatablePhrase phrase1 = new TranslatablePhrase("Who was the man in the lion's den?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Who was the man with the jar?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("Who was the man Jesus healed?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				phrase1, phrase2, phrase3 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(3, phrase1.TranslatableParts.Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());
			Assert.AreEqual(2, phrase3.TranslatableParts.Count());

			string transPart = "Quie\u0301n era el hombre";
			string transCommon = "\u00BF" + transPart;
			phrase1.Translation = transCommon + " en la fosa de leones?";
			phrase2.Translation = transCommon + " con el jarro?";

			Assert.AreEqual(transCommon + " en la fosa de leones?", phrase1.Translation);
			Assert.AreEqual(transCommon + " con el jarro?", phrase2.Translation);
			Assert.AreEqual(transCommon + " JESUS?", phrase3.Translation);
			Assert.AreEqual(transPart, phrase3[0].Translation);
			Assert.IsTrue(phrase1.HasUserTranslation);
			Assert.IsTrue(phrase2.HasUserTranslation);
			Assert.IsFalse(phrase3.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for two phrases that have a common part and verify
		/// that a third phrase that has that part shows the translation of the translated part.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_FindKeyTermRenderingWhenKtHasMultiplesTranslations()
		{
			AddMockedKeyTerm("arrow", "flecha");
			AddMockedKeyTerm("arrow", "dardo");
			AddMockedKeyTerm("arrow", "dardos");
			AddMockedKeyTerm("lion", "leo\u0301n");
			AddMockedKeyTerm("boat", "nave");
			AddMockedKeyTerm("boat", "barco");
			AddMockedKeyTerm("boat", "barca");

			TranslatablePhrase phrase1 = new TranslatablePhrase("I shot the lion with the arrow.");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Who put the lion in the boat?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("Does the boat belong to the boat?");
			TranslatablePhrase phrase4 = new TranslatablePhrase("I shot the boat with the arrow.");
			TranslatablePhrase phrase5 = new TranslatablePhrase("Who put the arrow in the boat?");
			TranslatablePhrase phrase6 = new TranslatablePhrase("Who put the arrow in the lion?");
			TranslatablePhrase phrase7 = new TranslatablePhrase("I shot the arrow with the lion.");
			TranslatablePhrase phrase8 = new TranslatablePhrase("Does the arrow belong to the lion?");

			PhraseTranslationHelper helper = new PhraseTranslationHelper(
				new[] { phrase1, phrase2, phrase3, phrase4, phrase5, phrase6, phrase7, phrase8 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(helper, "m_justGettingStarted", false);

			foreach (TranslatablePhrase phrase in helper.Phrases)
			{
				Assert.AreEqual(4, phrase.GetParts().Count(), "Wrong number of parts for phrase: " + phrase.OriginalPhrase);
				Assert.AreEqual(2, phrase.TranslatableParts.Count(), "Wrong number of translatable parts for phrase: " + phrase.OriginalPhrase);
			}

			phrase1.Translation = "Yo le pegue\u0301 un tiro al noil con un dardo.";
			Assert.AreEqual("Yo le pegue\u0301 un tiro al nave con un flecha.", phrase4.Translation);
			Assert.AreEqual("Yo le pegue\u0301 un tiro al flecha con un leo\u0301n.", phrase7.Translation);

			phrase2.Translation = "\u00BFQuie\u0301n puso el leo\u0301n en la barca?";
			Assert.AreEqual("\u00BFQuie\u0301n puso el flecha en la nave?", phrase5.Translation);
			Assert.AreEqual("\u00BFQuie\u0301n puso el flecha en la leo\u0301n?", phrase6.Translation);

			phrase3.Translation = "\u00BFEl taob le pertenece al barco?";
			// This is a bizarre special case where the original question has the same key term twice and
			// the user has translated it differently. Internal details of the logic (specifically, it finds
			// the longer rendering first) dictate the order in which the key terms are considered to have
			// been found. For the purposes of this test case, we don't care in which order the terms of the
			// untranslated question get substituted.
			Assert.IsTrue("\u00BFEl leo\u0301n le pertenece al flecha?" == phrase8.Translation ||
				"\u00BFEl flecha le pertenece al leo\u0301n?" == phrase8.Translation);

			Assert.IsTrue(phrase1.HasUserTranslation);
			Assert.IsTrue(phrase2.HasUserTranslation);
			Assert.IsTrue(phrase3.HasUserTranslation);
			Assert.IsFalse(phrase4.HasUserTranslation);
			Assert.IsFalse(phrase5.HasUserTranslation);
			Assert.IsFalse(phrase6.HasUserTranslation);
			Assert.IsFalse(phrase7.HasUserTranslation);
			Assert.IsFalse(phrase8.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for one phrase and then repeatedly accepting and
		/// un-accepting the generated translation for the other phrase that differs only by
		/// key terms. (TXL-51)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_PreventAddedSpacesWhenAcceptingGeneratedTranslation()
		{
			AddMockedKeyTerm("Jesus", "Jesu\u0301s");
			AddMockedKeyTerm("Phillip", "Felipe");
			AddMockedKeyTerm("Matthew", "Mateo");

			TranslatablePhrase phrase0 = new TranslatablePhrase("What asked Jesus Matthew?");
			TranslatablePhrase phrase1 = new TranslatablePhrase("What asked Jesus Phillip?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("What asked Phillip Matthew?");

			PhraseTranslationHelper helper = new PhraseTranslationHelper(
				new[] { phrase0, phrase1, phrase2 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(helper, "m_justGettingStarted", false);


			phrase0.Translation = "\u00BFQue\u0301 le pregunto\u0301 Jesu\u0301s Mateo?";
			phrase1.Translation = "\u00BFQue\u0301 le pregunto\u0301 Jesu\u0301s a Felipe?";
			Assert.AreEqual("\u00BFQue\u0301 le pregunto\u0301 Felipe a Mateo?", phrase2.Translation);
			Assert.IsFalse(phrase2.HasUserTranslation);

			phrase2.HasUserTranslation = true;
			Assert.AreEqual("\u00BFQue\u0301 le pregunto\u0301 Felipe a Mateo?", phrase2.Translation);
			Assert.IsTrue(phrase2.HasUserTranslation);

			phrase2.HasUserTranslation = false;
			Assert.AreEqual("\u00BFQue\u0301 le pregunto\u0301 Felipe Mateo?", phrase2.Translation);
			Assert.IsFalse(phrase2.HasUserTranslation);

			phrase2.HasUserTranslation = true;
			Assert.AreEqual("\u00BFQue\u0301 le pregunto\u0301 Felipe Mateo?", phrase2.Translation);
			Assert.IsTrue(phrase2.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that unconfirming a user translation will go back to a template-based
		/// translation if one is available. Specifically, it will revert to the template of the
		/// first translated question it finds.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_ClearingUserTranslationFlagRestoresTemplateBasedTranslation()
		{
			AddMockedKeyTerm("Jesus", "Jesu\u0301s");
			AddMockedKeyTerm("Phillip", "Felipe");
			AddMockedKeyTerm("Matthew", "Mateo");

			TranslatablePhrase phrase0 = new TranslatablePhrase("What asked Jesus Matthew?");
			TranslatablePhrase phrase1 = new TranslatablePhrase("What asked Jesus Phillip?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("What asked Phillip Matthew?");

			PhraseTranslationHelper helper = new PhraseTranslationHelper(
				new[] { phrase0, phrase1, phrase2 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(helper, "m_justGettingStarted", false);


			phrase0.Translation = "\u00BFQue\u0301 le pregunto\u0301 Jesu\u0301s a Mateo?";
			phrase1.Translation = "\u00BFQue\u0301 le pregunto\u0301 Jesu\u0301s Felipe?";
			Assert.AreEqual("\u00BFQue\u0301 le pregunto\u0301 Felipe Mateo?", phrase2.Translation);
			Assert.IsFalse(phrase2.HasUserTranslation);

			phrase2.HasUserTranslation = true;
			Assert.AreEqual("\u00BFQue\u0301 le pregunto\u0301 Felipe Mateo?", phrase2.Translation);
			Assert.IsTrue(phrase2.HasUserTranslation);

			phrase2.HasUserTranslation = false;
			Assert.AreEqual("\u00BFQue\u0301 le pregunto\u0301 Felipe a Mateo?", phrase2.Translation);
			Assert.IsFalse(phrase2.HasUserTranslation);

			phrase2.HasUserTranslation = true;
			Assert.AreEqual("\u00BFQue\u0301 le pregunto\u0301 Felipe a Mateo?", phrase2.Translation);
			Assert.IsTrue(phrase2.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for two phrases that have a common part and verify
		/// that a third phrase that has that part shows the translation of the translated part.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeTranslation_GuessAtPhraseTranslationBasedOnTriangulation()
		{
			AddMockedKeyTerm("Jesus");
			AddMockedKeyTerm("lion");
			AddMockedKeyTerm("jar");

			TranslatablePhrase phrase1 = new TranslatablePhrase("Who was the man in the lions' den?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Who was the man with the jar?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("Who was the man Jesus healed?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				phrase1, phrase2, phrase3 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(3, phrase1.TranslatableParts.Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());
			Assert.AreEqual(2, phrase3.TranslatableParts.Count());

			string partTrans = "Quie\u0301n era el hombre";
			string transCommon = "\u00BF" + partTrans;
			phrase1.Translation = "Quien fue lo hambre en la fosa de leones?";
			phrase2.Translation = transCommon + " con el jarro?";
			Assert.AreEqual("\u00BFmbre JESUS?", phrase3.Translation);
			Assert.AreEqual("mbre", phrase3[0].Translation);

			phrase1.Translation = transCommon + " en la fosa de leones?";

			Assert.AreEqual(transCommon + " en la fosa de leones?", phrase1.Translation);
			Assert.AreEqual(transCommon + " con el jarro?", phrase2.Translation);
			Assert.AreEqual(transCommon + " JESUS?", phrase3.Translation);
			Assert.AreEqual(partTrans, phrase3[0].Translation);
			Assert.IsTrue(phrase1.HasUserTranslation);
			Assert.IsTrue(phrase2.HasUserTranslation);
			Assert.IsFalse(phrase3.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a phrase with only one translatable part when
		/// another phrase differs only by a key term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_GuessAtOnePartPhraseThatDiffersBySingleKeyTerm()
		{
			AddMockedKeyTerm("Timothy", "Timoteo");
			AddMockedKeyTerm("Euticus", "Eutico");

			TranslatablePhrase phrase1 = new TranslatablePhrase("Who was Timothy?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Who was Euticus?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				phrase1, phrase2 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(1, phrase1.TranslatableParts.Count());
			Assert.AreEqual(2, phrase1.GetParts().Count());
			Assert.AreEqual(1, phrase2.TranslatableParts.Count());
			Assert.AreEqual(2, phrase2.GetParts().Count());

			const string frame = "\u00BFQuie\u0301n era {0}?";
			phrase1.Translation = string.Format(frame, "Timoteo");

			Assert.AreEqual(string.Format(frame, "Timoteo"), phrase1.Translation);
			Assert.AreEqual(string.Format(frame, "Eutico"), phrase2.Translation);
			Assert.IsTrue(phrase1.HasUserTranslation);
			Assert.IsFalse(phrase2.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for two phrases that have a common part and verify
		/// that a third phrase that has that part shows the translation of the translated part.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_GuessAtTwoPartPhraseThatDiffersBySingleKeyTerm()
		{
			AddMockedKeyTerm("Jacob", "Jacobo");
			AddMockedKeyTerm("Matthew", "Mateo");

			TranslatablePhrase phrase1 = new TranslatablePhrase("Was Jacob one of the disciples?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Was Matthew one of the disciples?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				phrase1, phrase2 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(2, phrase1.TranslatableParts.Count());
			Assert.AreEqual(3, phrase1.GetParts().Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());
			Assert.AreEqual(3, phrase2.GetParts().Count());

			const string frame = "\u00BFFue {0} uno de los discipulos?";
			phrase1.Translation = string.Format(frame, "Jacobo");

			Assert.AreEqual(string.Format(frame, "Jacobo"), phrase1.Translation);
			Assert.AreEqual(string.Format(frame, "Mateo"), phrase2.Translation);
			Assert.IsTrue(phrase1.HasUserTranslation);
			Assert.IsFalse(phrase2.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for multiple phrases. Possible part translations
		/// should be assigned to parts according to length and numbers of occurrences, but no
		/// portion of a translation should be used as the translation for two parts of the same
		/// owning phrase
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_PreventTranslationFromBeingUsedForMultipleParts()
		{
			AddMockedKeyTerm("Jacob", "Jacob");
			AddMockedKeyTerm("John", "Juan");
			AddMockedKeyTerm("Jesus", "Jesu\u0301s");
			AddMockedKeyTerm("Mary", "Mari\u0301a");
			AddMockedKeyTerm("Moses", "Moise\u0301s");

			TranslatablePhrase phrase1 = new TranslatablePhrase("So what did Jacob do?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("So what did Jesus do?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("What did Jacob do?");
			TranslatablePhrase phrase4 = new TranslatablePhrase("What did Moses ask?");
			TranslatablePhrase phrase5 = new TranslatablePhrase("So what did John ask?");
			TranslatablePhrase phrase6 = new TranslatablePhrase("So what did Mary want?");
			TranslatablePhrase phrase7 = new TranslatablePhrase("What did Moses do?");
			TranslatablePhrase phrase8 = new TranslatablePhrase("Did Moses ask, \"What did Jacob do?\"");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2, phrase3,
				phrase4, phrase5, phrase6, phrase7, phrase8 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(2, phrase1.TranslatableParts.Count());
			Assert.AreEqual(3, phrase1.GetParts().Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());
			Assert.AreEqual(3, phrase2.GetParts().Count());
			Assert.AreEqual(2, phrase3.TranslatableParts.Count());
			Assert.AreEqual(3, phrase3.GetParts().Count());
			Assert.AreEqual(2, phrase4.TranslatableParts.Count());
			Assert.AreEqual(3, phrase4.GetParts().Count());
			Assert.AreEqual(2, phrase5.TranslatableParts.Count());
			Assert.AreEqual(3, phrase5.GetParts().Count());
			Assert.AreEqual(2, phrase6.TranslatableParts.Count());
			Assert.AreEqual(3, phrase6.GetParts().Count());
			Assert.AreEqual(2, phrase7.TranslatableParts.Count());
			Assert.AreEqual(3, phrase7.GetParts().Count());
			Assert.AreEqual(4, phrase8.TranslatableParts.Count());
			Assert.AreEqual(6, phrase8.GetParts().Count());

			phrase1.Translation = "\u00BFEntonces que\u0301 hizo Jacob?";
			phrase2.Translation = "\u00BFEntonces que\u0301 hizo Jesu\u0301s?";
			phrase3.Translation = "\u00BFQue\u0301 hizo Jacob?";
			phrase4.Translation = "\u00BFQue\u0301 pregunto\u0301 Moise\u0301s?";
			phrase5.Translation = "\u00BFEntonces que\u0301 pregunto\u0301 Juan?";

			Assert.AreEqual("\u00BFEntonces que\u0301 Mari\u0301a?", phrase6.Translation);
			Assert.AreEqual("\u00BFQue\u0301 hizo Moise\u0301s?", phrase7.Translation);
			Assert.AreEqual("Moise\u0301s pregunto\u0301 Que\u0301 Jacob hizo", phrase8.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for multiple phrases. Possible part translations
		/// should be assigned to parts according to length and numbers of occurrences, but no
		/// portion of a translation should be used as the translation for two parts of the same
		/// owning phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_UseStatsAndConfidenceToDeterminePartTranslations()
		{
			AddMockedKeyTerm("ask");
			AddMockedKeyTerm("give");
			AddMockedKeyTerm("want");
			AddMockedKeyTerm("whatever");
			AddMockedKeyTerm("thing");

			TranslatablePhrase phrase1 = new TranslatablePhrase("ABC ask DEF");
			TranslatablePhrase phrase2 = new TranslatablePhrase("ABC give XYZ");
			TranslatablePhrase phrase3 = new TranslatablePhrase("XYZ want ABC whatever EFG");
			TranslatablePhrase phrase4 = new TranslatablePhrase("EFG thing ABC");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2,
				phrase3, phrase4 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(2, phrase1.TranslatableParts.Count());
			Assert.AreEqual(3, phrase1.GetParts().Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());
			Assert.AreEqual(3, phrase2.GetParts().Count());
			Assert.AreEqual(3, phrase3.TranslatableParts.Count());
			Assert.AreEqual(5, phrase3.GetParts().Count());
			Assert.AreEqual(2, phrase4.TranslatableParts.Count());
			Assert.AreEqual(3, phrase4.GetParts().Count());

			phrase1.Translation = "def ASK abc";
			phrase2.Translation = "abc xyz GIVE";
			phrase3.Translation = "WANT xyz abc WHATEVER efg";

			Assert.AreEqual("efg THING abc", phrase4.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the code to determine the best translation for a part of a phrase will
		/// not take a substring common to all phrases if it would mean selecting less than a
		/// whole word instead of a statistically viable substring that consists of whole
		/// words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_UseStatisticalBestPartTranslations()
		{
			AddMockedKeyTerm("Isaac", "Isaac");
			AddMockedKeyTerm("Paul", "Pablo");

			TranslatablePhrase phraseBreakerA = new TranslatablePhrase("Now what?");
			TranslatablePhrase phraseBreakerB = new TranslatablePhrase("What did Isaac say?");
			TranslatablePhrase phrase1 = new TranslatablePhrase("So now what did those two brothers do?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("So what did they do about the problem?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("So what did he do?");
			TranslatablePhrase phrase4 = new TranslatablePhrase("So now what was Isaac complaining about?");
			TranslatablePhrase phrase5 = new TranslatablePhrase("So what did the Apostle Paul say about that?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phraseBreakerA, phraseBreakerB,
				phrase1, phrase2, phrase3, phrase4, phrase5 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(3, phrase1.TranslatableParts.Count());
			Assert.AreEqual(3, phrase1.GetParts().Count());
			Assert.AreEqual(3, phrase2.TranslatableParts.Count());
			Assert.AreEqual(3, phrase2.GetParts().Count());
			Assert.AreEqual(3, phrase3.TranslatableParts.Count());
			Assert.AreEqual(3, phrase3.GetParts().Count());
			Assert.AreEqual(4, phrase4.TranslatableParts.Count());
			Assert.AreEqual(5, phrase4.GetParts().Count());
			Assert.AreEqual(4, phrase5.TranslatableParts.Count());
			Assert.AreEqual(5, phrase5.GetParts().Count());

			phrase1.Translation = "\u00BFEntonces ahora que\u0301 hicieron esos dos hermanos?";
			phrase2.Translation = "\u00BFEntonces que\u0301 hicieron acerca del problema?";
			phrase3.Translation = "\u00BFEntonces que\u0301 hizo?";
			phrase5.Translation = "\u00BFEntonces que\u0301 dijo Pablo acerca de eso?";

			Assert.AreEqual("\u00BFEntonces Isaac?", phrase4.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the code to determine the best translation for a part of a phrase will
		/// not take a substring common to all phrases if it would mean selecting less than a
		/// whole word instead of a statistically viable substring that consists of whole
		/// words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_UseStatisticalBestPartTranslationsRatherThanCommonPartialWord()
		{
			AddMockedKeyTerm("Isaac", "Isaac");
			AddMockedKeyTerm("Paul", "Pablo");

			TranslatablePhrase phraseBreakerA = new TranslatablePhrase("Now what?");
			TranslatablePhrase phraseBreakerB = new TranslatablePhrase("What did Isaac say?");
			TranslatablePhrase phraseBreakerC = new TranslatablePhrase("What could Isaac say?");
			TranslatablePhrase phrase1 = new TranslatablePhrase("So now what did those two brothers do?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("So what could they do about the problem?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("So what did he do?");
			TranslatablePhrase phrase4 = new TranslatablePhrase("So now what was Isaac complaining about?");
			TranslatablePhrase phrase5 = new TranslatablePhrase("So what did the Apostle Paul say about that?");
			TranslatablePhrase phrase6 = new TranslatablePhrase("Why did they treat the Apostle Paul so?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phraseBreakerA, phraseBreakerB, phraseBreakerC,
				phrase1, phrase2, phrase3, phrase4, phrase5, phrase6 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(3, phrase1.TranslatableParts.Count());
			Assert.AreEqual(3, phrase1.GetParts().Count());
			Assert.AreEqual(3, phrase2.TranslatableParts.Count());
			Assert.AreEqual(3, phrase2.GetParts().Count());
			Assert.AreEqual(3, phrase3.TranslatableParts.Count());
			Assert.AreEqual(3, phrase3.GetParts().Count());
			Assert.AreEqual(4, phrase4.TranslatableParts.Count());
			Assert.AreEqual(5, phrase4.GetParts().Count());
			Assert.AreEqual(4, phrase5.TranslatableParts.Count());
			Assert.AreEqual(5, phrase5.GetParts().Count());
			Assert.AreEqual(2, phrase6.TranslatableParts.Count());
			Assert.AreEqual(3, phrase6.GetParts().Count());

			phrase1.Translation = "Entonces AB Zuxelopitmyfor CD EF GH";
			phrase2.Translation = "Entonces Vuxelopitmyfor IJ KL MN OP QR";
			phrase3.Translation = "Entonces Wuxelopitmyfor ST";
			phrase5.Translation = "Entonces Xuxelopitmyfor dijo Pablo UV WX YZ";
			phrase6.Translation = "BG LP Yuxelopitmyfor DW MR Pablo";

			Assert.AreEqual("Entonces Isaac", phrase4.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a group of phrases such that the only common
		/// character for a part they have in common is a punctuation character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetTranslation_DoNotTreatNormalLeadingPuncAsOpeningQuestionMark()
		{
			AddMockedKeyTerm("Isaiah", "Isai\u0301as");
			AddMockedKeyTerm("Paul", "Pablo");
			AddMockedKeyTerm("Silas", "Silas");

			TranslatablePhrase phrase1 = new TranslatablePhrase("What did Paul and Silas do in jail?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Were Isaiah and Paul prophets?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2 },
				m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(3, phrase1.TranslatableParts.Count());
			Assert.AreEqual(3, phrase2.TranslatableParts.Count());

			phrase1.Translation = "*\u00BFQue\u0301 hicieron Pablo y Silas en la carcel?";
			Assert.AreEqual("Isai\u0301as Pablo?", phrase2.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a group of phrases such that the only common
		/// character for a part they have in common is a punctuation character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeTranslation_PreventPartTranslationFromBeingSetToPunctuation()
		{
			AddMockedKeyTerm("Isaiah", "Isai\u0301as");
			AddMockedKeyTerm("Paul", "Pablo");
			AddMockedKeyTerm("Silas", "Silas");

			TranslatablePhrase phrase1 = new TranslatablePhrase("What did Paul and Silas do in jail?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Were Isaiah and Paul prophets?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("Did Paul and Silas run away?");
			TranslatablePhrase phrase4 = new TranslatablePhrase("And what did Paul do next?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2,
				phrase3, phrase4 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(3, phrase1.TranslatableParts.Count());
			Assert.AreEqual(3, phrase2.TranslatableParts.Count());
			Assert.AreEqual(3, phrase3.TranslatableParts.Count());
			Assert.AreEqual(3, phrase4.TranslatableParts.Count());

			phrase1.Translation = "\u00BFQue\u0301 hicieron Pablo y Silas en la carcel?";
			phrase2.Translation = "\u00BFEran profetas Pablo e Isai\u0301as?";
			phrase3.Translation = "\u00BFSe corrieron Pablo y Silas?";
			Assert.AreEqual("\u00BFy Pablo?", phrase4.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a phrase that has parts that are also in another
		/// phrase that does not have a user translation but does have parts that do have a
		/// translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeTranslation_PreventTrashingPartTranslationsWhenReCalculating()
		{
			AddMockedKeyTerm("Mary", "Mari\u0301a");
			AddMockedKeyTerm("Jesus");

			TranslatablePhrase phrase1 = new TranslatablePhrase("When?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Where did Mary find Jesus?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("Where did Jesus find a rock?");
			TranslatablePhrase phrase4 = new TranslatablePhrase("Where did Mary eat?");
			TranslatablePhrase phrase5 = new TranslatablePhrase("When Mary went to the tomb, where did Jesus meet her?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2,
				phrase3, phrase4, phrase5 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(1, phrase1.TranslatableParts.Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());
			Assert.AreEqual(2, phrase3.TranslatableParts.Count());
			Assert.AreEqual(2, phrase4.TranslatableParts.Count());
			Assert.AreEqual(4, phrase5.TranslatableParts.Count());

			phrase1.Translation = "\u00BFCua\u0301ndo?";
			phrase2.Translation = "\u00BFDo\u0301nde encontro\u0301 Mari\u0301a a JESUS?";
			phrase3.Translation = "\u00BFDo\u0301nde encontro\u0301 JESUS una piedra?";
			phrase4.Translation = "\u00BFDo\u0301nde comio\u0301 Mari\u0301a?";
			Assert.AreEqual("\u00BFCua\u0301ndo Mari\u0301a Do\u0301nde JESUS?", phrase5.Translation);

			Assert.IsTrue(phrase1.HasUserTranslation);
			Assert.IsTrue(phrase2.HasUserTranslation);
			Assert.IsTrue(phrase3.HasUserTranslation);
			Assert.IsFalse(phrase5.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a group of phrases that have a common part such
		/// that phrases A & B have a common substring that is longer than the substring that
		/// all three share in common.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeTranslation_FallbackToSmallerCommonSubstring()
		{
			AddMockedKeyTerm("the squirrel", "la ardilla");
			AddMockedKeyTerm("donkey", "asno");
			AddMockedKeyTerm("Balaam", "Balaam");

			TranslatablePhrase phrase1 = new TranslatablePhrase("When did the donkey and the squirrel fight?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("What did the donkey and Balaam do?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("Where are Balaam and the squirrel?");
			TranslatablePhrase phrase4 = new TranslatablePhrase("and?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2,
				phrase3, phrase4 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(3, phrase1.TranslatableParts.Count());
			Assert.AreEqual(3, phrase2.TranslatableParts.Count());
			Assert.AreEqual(2, phrase3.TranslatableParts.Count());
			Assert.AreEqual(1, phrase4.TranslatableParts.Count());

			phrase1.Translation = "\u00BFCua\u0301ndo pelearon el asno y la ardilla?";
			phrase2.Translation = "\u00BFQue\u0301 hicieron el asno y Balaam?";
			phrase3.Translation = "\u00BFDo\u0301nde esta\u0301n Balaam y la ardilla?";
			Assert.AreEqual("\u00BFy?", phrase4.Translation);

			Assert.IsTrue(phrase1.HasUserTranslation);
			Assert.IsTrue(phrase2.HasUserTranslation);
			Assert.IsTrue(phrase3.HasUserTranslation);
			Assert.IsFalse(phrase4.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a group of phrases that have a common part such
		/// that phrases A & B have a common substring that is longer than the substring that
		/// all three share in common.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeTranslation_FallbackToSmallerCommonSubstring_EndingInLargerSubstring()
		{
			AddMockedKeyTerm("the squirrel", "ardilla");
			AddMockedKeyTerm("donkey", "asno");
			AddMockedKeyTerm("Balaam", "Balaam");

			TranslatablePhrase phrase1 = new TranslatablePhrase("When did the donkey and the squirrel fight?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Where are Balaam and the squirrel?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("What did the donkey and Balaam do?");
			TranslatablePhrase phrase4 = new TranslatablePhrase("and?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2,
				phrase3, phrase4 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(3, phrase1.TranslatableParts.Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());
			Assert.AreEqual(3, phrase3.TranslatableParts.Count());
			Assert.AreEqual(1, phrase4.TranslatableParts.Count());

			phrase1.Translation = "\u00BFCua\u0301ndo pelearon el asno loco y ardilla?";
			phrase2.Translation = "\u00BFDo\u0301nde esta\u0301n Balaam loco y ardilla?";
			phrase3.Translation = "\u00BFQue\u0301 hicieron el asno y Balaam?";
			Assert.AreEqual("\u00BFy?", phrase4.Translation);

			Assert.IsTrue(phrase1.HasUserTranslation);
			Assert.IsTrue(phrase2.HasUserTranslation);
			Assert.IsTrue(phrase3.HasUserTranslation);
			Assert.IsFalse(phrase4.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a group of phrases that have a common part such
		/// that phrases A & B have a common substring that is longer than the substring that
		/// all three share in common.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeTranslation_FallbackToSmallerCommonSubstring_StartingInLargerSubstring()
		{
			AddMockedKeyTerm("the squirrel", "ardilla");
			AddMockedKeyTerm("donkey", "asno");
			AddMockedKeyTerm("Balaam", "Balaam");

			TranslatablePhrase phrase1 = new TranslatablePhrase("When did the donkey and the squirrel fight?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Where are Balaam and the squirrel?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("What did the donkey and Balaam do?");
			TranslatablePhrase phrase4 = new TranslatablePhrase("and?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2, phrase3, phrase4 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(3, phrase1.TranslatableParts.Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());
			Assert.AreEqual(3, phrase3.TranslatableParts.Count());
			Assert.AreEqual(1, phrase4.TranslatableParts.Count());

			phrase1.Translation = "\u00BFCua\u0301ndo pelearon el asno y la ardilla?";
			phrase2.Translation = "\u00BFDo\u0301nde esta\u0301n Balaam y la ardilla?";
			phrase3.Translation = "\u00BFQue\u0301 hicieron el asno y Balaam?";
			Assert.AreEqual("\u00BFy?", phrase4.Translation);

			Assert.IsTrue(phrase1.HasUserTranslation);
			Assert.IsTrue(phrase2.HasUserTranslation);
			Assert.IsTrue(phrase3.HasUserTranslation);
			Assert.IsFalse(phrase4.HasUserTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the translation for a phrase such that there is a single part whose
		/// rendering does not match the statistically best rendering for that part. The
		/// statistically best part should win.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeTranslation_PreventUpdatedTranslationFromChangingGoodPartTranslation()
		{
			AddMockedKeyTerm("donkey", "asno");
			AddMockedKeyTerm("Balaam", "Balaam");

			TranslatablePhrase phrase1 = new TranslatablePhrase("When?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("When Balaam eats donkey.");
			TranslatablePhrase phrase3 = new TranslatablePhrase("What donkey eats?");
			TranslatablePhrase phrase4 = new TranslatablePhrase("What Balaam eats?");
			TranslatablePhrase phrase5 = new TranslatablePhrase("Donkey eats?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] { phrase1, phrase2, phrase3, phrase4, phrase5 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(1, phrase1.TranslatableParts.Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());
			Assert.AreEqual(2, phrase3.TranslatableParts.Count());
			Assert.AreEqual(2, phrase4.TranslatableParts.Count());
			Assert.AreEqual(1, phrase5.TranslatableParts.Count());

			phrase1.Translation = "\u00BFCua\u0301ndo?";
			phrase2.Translation = "\u00BFCua\u0301ndo come Balaam al asno.";
			phrase3.Translation = "\u00BFQue\u0301 come el asno?";
			phrase4.Translation = "\u00BFQue\u0301 ingiere Balaam?";
			Assert.AreEqual("\u00BFasno come?", phrase5.Translation);

			Assert.IsTrue(phrase1.HasUserTranslation);
			Assert.IsTrue(phrase2.HasUserTranslation);
			Assert.IsTrue(phrase3.HasUserTranslation);
			Assert.IsTrue(phrase4.HasUserTranslation);
			Assert.IsFalse(phrase5.HasUserTranslation);
		}
		#endregion

		#region Constrain Key Terms to References tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests constraining the use of key terms to only the applicable "verses"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConstrainByRef_Simple()
		{
			AddMockedKeyTerm("God", 4);
			AddMockedKeyTerm("Paul", 1, 5);
			AddMockedKeyTerm("have", 99);
			AddMockedKeyTerm("say", 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A", 1, 1), 1, 0),
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),
				new TranslatablePhrase(new TestQ("Is it okay for Paul to talk with respect to God today?", "D", 4, 4), 1, 0),
				new TranslatablePhrase(new TestQ("that dog wishes this Paul what is say radish", "E", 5, 5), 1, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "F", 6, 6), 1, 0)}, m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");
			VerifyTranslatablePhrase(pth, "What would God have me to say with respect to Paul?", "what would god have me to say with respect to", 1);
			VerifyTranslatablePhrase(pth, "What is Paul asking me to say with respect to that dog?", "what is", 3, "paul asking me to", 1, "with respect to", 1, "that dog", 4);
			VerifyTranslatablePhrase(pth, "that dog", "that dog", 4);
			VerifyTranslatablePhrase(pth, "Is it okay for Paul to talk with respect to God today?", "is it okay for paul to talk with respect to", 1, "today", 1);
			VerifyTranslatablePhrase(pth, "that dog wishes this Paul what is say radish", "that dog", 4, "wishes this", 1, "what is", 3, "radish", 1);
			VerifyTranslatablePhrase(pth, "What is that dog?", "what is", 3, "that dog", 4);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests constraining the use of key terms to only the applicable "verses"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConstrainByRef_RefRanges()
		{
			AddMockedKeyTerm("God", 4);
			AddMockedKeyTerm("Paul", 1, 3, 5);
			AddMockedKeyTerm("have", 99);
			AddMockedKeyTerm("say", 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A-D", 1, 4), 0, 0),
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),
				new TranslatablePhrase(new TestQ("Is it okay for Paul to talk with respect to God today?", "B-D", 2, 4), 0, 0),
				new TranslatablePhrase(new TestQ("that dog wishes this Paul what is say radish", "E", 5, 5), 1, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "E-F", 5, 6), 0, 0)}, m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");
			VerifyTranslatablePhrase(pth, "What would God have me to say with respect to Paul?", "what would", 1, "have me to", 1, "with respect to", 3);
			VerifyTranslatablePhrase(pth, "What is Paul asking me to say with respect to that dog?", "what is", 3, "paul asking me to", 1, "with respect to", 3, "that dog", 4);
			VerifyTranslatablePhrase(pth, "that dog", "that dog", 4);
			VerifyTranslatablePhrase(pth, "Is it okay for Paul to talk with respect to God today?", "is it okay for", 1, "to talk", 1, "with respect to", 3, "today", 1);
			VerifyTranslatablePhrase(pth, "that dog wishes this Paul what is say radish", "that dog", 4, "wishes this", 1, "what is", 3, "radish", 1);
			VerifyTranslatablePhrase(pth, "What is that dog?", "what is", 3, "that dog", 4);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests constraining the use of key terms to only the applicable "verses"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConstrainByRef_GodMatchesAnywhere()
		{
			AddMockedKeyTerm("God");
			AddMockedKeyTerm("Paul", 1, 3, 5);
			AddMockedKeyTerm("have", 99);
			AddMockedKeyTerm("say", 2, 5);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("What would God have me to say with respect to Paul?", "A-D", 1, 4), 0, 0),
				new TranslatablePhrase(new TestQ("What is Paul asking me to say with respect to that dog?", "B", 2, 2), 1, 0),
				new TranslatablePhrase(new TestQ("that dog", "C", 3, 3), 1, 0),
				new TranslatablePhrase(new TestQ("Is it okay for Paul to talk with respect to God today?", "B-D", 2, 4), 0, 0),
				new TranslatablePhrase(new TestQ("that dog wishes this Paul what is say radish", "E", 5, 5), 1, 0),
				new TranslatablePhrase(new TestQ("What is that dog?", "E-F", 5, 6), 0, 0)}, m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");
			VerifyTranslatablePhrase(pth, "What would God have me to say with respect to Paul?", "what would", 1, "have me to", 1, "with respect to", 3);
			VerifyTranslatablePhrase(pth, "What is Paul asking me to say with respect to that dog?", "what is", 3, "paul asking me to", 1, "with respect to", 3, "that dog", 4);
			VerifyTranslatablePhrase(pth, "that dog", "that dog", 4);
			VerifyTranslatablePhrase(pth, "Is it okay for Paul to talk with respect to God today?", "is it okay for", 1, "to talk", 1, "with respect to", 3, "today", 1);
			VerifyTranslatablePhrase(pth, "that dog wishes this Paul what is say radish", "that dog", 4, "wishes this", 1, "what is", 3, "radish", 1);
			VerifyTranslatablePhrase(pth, "What is that dog?", "what is", 3, "that dog", 4);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests constraining the use of key terms to only the applicable "verses"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConstrainByRef_ComplexKeyTerms()
		{
			AddMockedKeyTerm("high priest", 1);
			AddMockedKeyTerm("high", 1, 2);
			AddMockedKeyTerm("radish", 1, 2);
			AddMockedKeyTerm("(to have) eaten or drunk", 2, 3);
			AddMockedKeyTerm("high or drunk sailor", 2, 4);

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				new TranslatablePhrase(new TestQ("Was the high priest on his high horse?", "A", 1, 1), 1, 0),
				new TranslatablePhrase(new TestQ("Who was the high priest?", "B", 2, 2), 1, 0),
				new TranslatablePhrase(new TestQ("I have eaten the horse.", "A", 1, 1), 1, 0),
				new TranslatablePhrase(new TestQ("How high is this?", "C", 3, 3), 1, 0),
				new TranslatablePhrase(new TestQ("That drunk sailor has eaten a radish", "C-D", 3, 4), 0, 0),
				new TranslatablePhrase(new TestQ("That high sailor was to have drunk some radish juice", "A-B", 1, 2), 0, 0)}, m_dummyKtList, m_keyTermRules, new List<Substitution>());

			Assert.AreEqual(6, pth.Phrases.Count(), "Wrong number of phrases in helper");
			VerifyTranslatablePhrase(pth, "Was the high priest on his high horse?", "was the", 2, "on his", 1, "horse", 1);
			VerifyTranslatablePhrase(pth, "Who was the high priest?", "who", 1, "was the", 2, "priest", 1);
			VerifyTranslatablePhrase(pth, "I have eaten the horse.", "i have eaten the horse", 1);
			VerifyTranslatablePhrase(pth, "How high is this?", "how high is this", 1);
			VerifyTranslatablePhrase(pth, "That drunk sailor has eaten a radish", "that", 2, "has", 1, "a radish", 1);
			VerifyTranslatablePhrase(pth, "That high sailor was to have drunk some radish juice", "that", 2, "was", 1, "some", 1, "juice", 1);
		}
		#endregion

		#region TranslatablePhrase.FindTermRenderingInUse tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding renderings of key terms when there is exactly one occurrence of each
		/// term in a particular phrase and one of the renderings is present in the translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestFindTermRenderingInUse_Present()
		{
			IKeyTerm ktGod = AddMockedKeyTerm("God", "Dios", 1, 4);
			IKeyTerm ktPaul = AddMockedKeyTerm("Paul", "Pablo", 1, 2, 5);
			IKeyTerm ktHave = AddMockedKeyTerm("have", "tenemos", 1, 2, 3, 4, 5, 6);
			IKeyTerm ktSay = AddMockedKeyTerm("say", "dice", 1, 2, 5);

			TranslatablePhrase phrase1 = new TranslatablePhrase("What did God tell Paul?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("What does Paul say we have to give to God?");

			new PhraseTranslationHelper(new[] {phrase1, phrase2},
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			phrase1.Translation = "\u00BFQue\u0301 le dijo Dios a Pablo?";
			phrase2.Translation = "\u00BFQue\u0301 dice luaP que tenemos que dar a Dios?";

			SubstringDescriptor sd;

			sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, 0));
			Assert.AreEqual(14, sd.Offset);
			Assert.AreEqual(4, sd.Length);

			sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, 0));
			Assert.AreEqual(21, sd.Offset);
			Assert.AreEqual(5, sd.Length);

			sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, 0));
			Assert.AreEqual(11, sd.Offset);
			Assert.AreEqual(4, sd.Length);

			sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktSay, 0));
			Assert.AreEqual(6, sd.Offset);
			Assert.AreEqual(4, sd.Length);

			sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktHave, 0));
			Assert.AreEqual(20, sd.Offset);
			Assert.AreEqual(7, sd.Length);

			sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, 0));
			Assert.AreEqual(38, sd.Offset);
			Assert.AreEqual(4, sd.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding renderings of key terms when there is exactly one occurrence of each
		/// term in a particular phrase and one of the renderings is present in the translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestFindTermRenderingInUse_SomeMissing()
		{
			IKeyTerm ktGod = AddMockedKeyTerm("God", "Dios", 1, 4);
			IKeyTerm ktPaul = AddMockedKeyTerm("Paul", "Pablo", 1, 2, 5);
			IKeyTerm ktHave = AddMockedKeyTerm("have", "tenemos", 1, 2, 3, 4, 5, 6);
			IKeyTerm ktSay = AddMockedKeyTerm("say", "dice", 1, 2, 5);

			TranslatablePhrase phrase1 = new TranslatablePhrase("What did God tell Paul?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("What does Paul say we have to give to God?");

			new PhraseTranslationHelper(new[] { phrase1, phrase2 },
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			phrase1.Translation = "\u00BFQue\u0301 le dijo Jehovah a Pablo?";
			phrase2.Translation = "Pi\u0301dale ayuda a Bill.";

			SubstringDescriptor sd;

			sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, 0));
			Assert.IsNull(sd);

			sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, 0));
			Assert.AreEqual(24, sd.Offset);
			Assert.AreEqual(5, sd.Length);

			sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, 0));
			Assert.IsNull(sd);

			sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktSay, 0));
			Assert.IsNull(sd);

			sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktHave, 0));
			Assert.IsNull(sd);

			sd = phrase2.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, 0));
			Assert.IsNull(sd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding renderings of key terms when there is exactly one occurrence of each
		/// term in a particular phrase and one of the renderings is present in the translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestFindTermRenderingInUse_RepeatedTerms()
		{
			IKeyTerm ktGod = AddMockedKeyTerm("God", "Dios", 1, 4);
			IKeyTerm ktPaul = AddMockedKeyTerm("Paul", "Pablo", 1, 2, 5);

			TranslatablePhrase phrase1 = new TranslatablePhrase("What did God tell Paul?/What was Paul told by God");

			new PhraseTranslationHelper(new[] { phrase1 },
				m_dummyKtList, m_keyTermRules, new List<Substitution>());

			phrase1.Translation = "\u00BFQue\u0301 le dijo Dios a Pablo?/\u00BFQue\u0301 le fue dicho a Pablo por Dios?";

			SubstringDescriptor sd;

			sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, 0));
			Assert.AreEqual(14, sd.Offset);
			Assert.AreEqual(4, sd.Length);
			int endOfLastOccurrenceOfGod = sd.EndOffset;

			sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, 0));
			Assert.AreEqual(21, sd.Offset);
			Assert.AreEqual(5, sd.Length);
			int endOfLastOccurrenceOfPaul = sd.EndOffset;

			sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktGod, endOfLastOccurrenceOfGod));
			Assert.AreEqual(59, sd.Offset);
			Assert.AreEqual(4, sd.Length);

			sd = phrase1.FindTermRenderingInUse(new DummyKeyTermRenderingInfo(ktPaul, endOfLastOccurrenceOfPaul));
			Assert.AreEqual(49, sd.Offset);
			Assert.AreEqual(5, sd.Length);
		}
		#endregion

		#region Rendering Selection Rules tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that adding a rendering selection rule based on the preceding (English) word
		/// causes the correct vernacular rendering to be selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectCorrectTermRendering_NoPartsTranslated_BasedOnPrecedingWordRule()
		{
			AddMockedKeyTerm("Jesus", "Cristo", new [] {"Jesucristo", "Jesus", "Cristo Jesus"});

			TranslatablePhrase phrase1 = new TranslatablePhrase("Who was the man Jesus healed?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				phrase1 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(2, phrase1.TranslatableParts.Count());

			Assert.IsFalse(phrase1.HasUserTranslation);
			Assert.AreEqual("Cristo", phrase1.Translation);

			pth.TermRenderingSelectionRules = new List<RenderingSelectionRule>(new [] {new RenderingSelectionRule(@"\bman {0}", @"ucristo\b") });

			Assert.AreEqual("Jesucristo", phrase1.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that adding a rendering selection rule based on the following (English) word
		/// causes the correct vernacular rendering to be inserted into the partial translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectCorrectTermRendering_SomePartsTranslated_BasedOnFollowingWordRule()
		{
			AddMockedKeyTerm("Stephen", "Esteban");
			AddMockedKeyTerm("Mary", "Mari\u0301a");
			AddMockedKeyTerm("look", "mirar", new[] { "pareci\u0301a", "buscaba", "busca", "busco" });

			TranslatablePhrase phrase1 = new TranslatablePhrase("What did Stephen look like to the priests and elders and other people present?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("What did Stephen do?");
			TranslatablePhrase phrase3 = new TranslatablePhrase("What did Mary look for?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				phrase1, phrase2, phrase3 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(2, phrase1.TranslatableParts.Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());
			Assert.AreEqual(2, phrase3.TranslatableParts.Count());

			phrase1.Translation = "\u00BFCo\u0301mo pareci\u0301a Esteban a los sacerdotes y ancianos y a los dema\u0301s?";
			phrase2.Translation = "\u00BFCo\u0301mo hizo Esteban?";

			Assert.IsFalse(phrase3.HasUserTranslation);
			Assert.AreEqual("\u00BFCo\u0301mo Mari\u0301a mirar?", phrase3.Translation);

			pth.TermRenderingSelectionRules = new List<RenderingSelectionRule>(new[] {
				new RenderingSelectionRule(@"{0} like\b", @"\bparec"),
				new RenderingSelectionRule(@"{0} for\b", @"\bbusc")});

			Assert.AreEqual("\u00BFCo\u0301mo Mari\u0301a buscaba?", phrase3.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that adding a rendering selection rule based on the (English) suffix causes
		/// the correct vernacular rendering to be inserted into the translation template.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectCorrectTermRendering_FillInTemplate_BasedOnSuffixRule()
		{
			AddMockedKeyTerm("magician", "mago", new[] { "brujo" });
			AddMockedKeyTerm("servant", "criado", new[] { "siervo" });
			AddMockedKeyTerm("heal", "sanar", new[] { "curada", "sanada", "sanara\u0301", "sanas", "curan", "cura", "sana", "sanado" });

			TranslatablePhrase phrase1 = new TranslatablePhrase("Was the servant healed?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Was the magician healed?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				phrase1, phrase2 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(1, phrase1.TranslatableParts.Count());
			Assert.AreEqual(1, phrase2.TranslatableParts.Count());

			phrase1.Translation = "\u00BFFue sanado el siervo?";

			Assert.IsFalse(phrase2.HasUserTranslation);
			Assert.AreEqual("\u00BFFue sanar el mago?", phrase2.Translation);

			pth.TermRenderingSelectionRules = new List<RenderingSelectionRule>(new[] {
				new RenderingSelectionRule(@"{0}\w*ed\b", @"o$")});

			Assert.AreEqual("\u00BFFue sanado el mago?", phrase2.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that adding a rendering selection rule based on the (English) suffix causes
		/// the correct vernacular rendering to be inserted into the translation template. In
		/// this test, there are multiple renderings which conform to the rendering selection
		/// rule -- we want the one that is also the default rendering for the term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectCorrectTermRendering_FillInTemplate_BasedOnSuffixRule_PreferDefault()
		{
			AddMockedKeyTerm("magician", "mago", new[] { "brujo" });
			AddMockedKeyTerm("servant", "criado", new[] { "siervo" });
			AddMockedKeyTerm("heal", "sanara\u0301", new[] { "curada", "sanada", "sanar", "curara\u0301", "sanas", "curan", "cura", "sana", "sanado" });

			TranslatablePhrase phrase1 = new TranslatablePhrase("Will the servant be healed?");
			TranslatablePhrase phrase2 = new TranslatablePhrase("Will the magician be healed?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				phrase1, phrase2 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(2, phrase1.TranslatableParts.Count());
			Assert.AreEqual(2, phrase2.TranslatableParts.Count());

			phrase1.Translation = "\u00BFSe curara\u0301 el siervo?";

			Assert.IsFalse(phrase2.HasUserTranslation);
			Assert.AreEqual("\u00BFSe sanara\u0301 el mago?", phrase2.Translation);

			pth.TermRenderingSelectionRules = new List<RenderingSelectionRule>(new[] {
				new RenderingSelectionRule(@"Will .* {0}\w*\b", "ra\u0301$")});

			Dictionary<Word, List<KeyTermMatch>> keyTermsTable =
				(Dictionary<Word, List<KeyTermMatch>>)ReflectionHelper.GetField(pth, "m_keyTermsTable");

			keyTermsTable["heal"].First().BestRendering = "curara\u0301";

			Assert.AreEqual("\u00BFSe curara\u0301 el mago?", phrase2.Translation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that adding a disabled rendering selection rule does not change the resulting
		/// translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SelectCorrectTermRendering_RuleDisabled()
		{
			AddMockedKeyTerm("Jesus", "Cristo", new[] { "Jesucristo", "Jesus", "Cristo Jesus" });

			TranslatablePhrase phrase1 = new TranslatablePhrase("Who was the man Jesus healed?");

			PhraseTranslationHelper pth = new PhraseTranslationHelper(new[] {
				phrase1 }, m_dummyKtList, m_keyTermRules, new List<Substitution>());
			ReflectionHelper.SetField(pth, "m_justGettingStarted", false);

			Assert.AreEqual(2, phrase1.TranslatableParts.Count());

			Assert.IsFalse(phrase1.HasUserTranslation);
			Assert.AreEqual("Cristo", phrase1.Translation);

			pth.TermRenderingSelectionRules = new List<RenderingSelectionRule>(new[] { new RenderingSelectionRule(@"\bman {0}", @"ucristo\b") });
			pth.TermRenderingSelectionRules[0].Disabled = true;

			Assert.AreEqual("Cristo", phrase1.Translation);
		}
		#endregion

		#region private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the mocked key term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddMockedKeyTerm(string term, params int[] occurences)
		{
			AddMockedKeyTerm(term, term.ToUpper(), occurences);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the mocked key term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IKeyTerm AddMockedKeyTerm(string term, string bestRendering, params int[] occurences)
		{
			return AddMockedKeyTerm(term, bestRendering,
				(bestRendering != null) ? new[] { new string(term.Reverse().ToArray()) } : new string[0],
				occurences);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the mocked key term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IKeyTerm AddMockedKeyTerm(string term, string bestRendering, string[] otherRenderings,
			params int[] occurences)
		{
			IKeyTerm mockedKt = KeyTermMatchBuilderTests.AddMockedKeyTerm(term, occurences);
			if (bestRendering != null)
			{
				otherRenderings = (otherRenderings == null) ? new [] { bestRendering } :
					(new [] { bestRendering }).Concat(otherRenderings).ToArray();
				mockedKt.Stub(kt => kt.BestRendering).Return(bestRendering);
			}
			mockedKt.Stub(kt => kt.Renderings).Return(otherRenderings);

			if (occurences.Length > 0)
			{
				if (m_keyTermRules == null)
				{
					m_keyTermRules = new KeyTermRules();
					m_keyTermRules.Items = new List<KeyTermRule>();
				}
				KeyTermRule rule = new KeyTermRule();
				rule.id = term;
				rule.Rule = KeyTermRule.RuleType.MatchForRefOnly;
				m_keyTermRules.Items.Add(rule);
			}
			m_dummyKtList.Add(mockedKt);
			return mockedKt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the translatable phrase.
		/// </summary>
		/// <param name="pth">The PhraseTranslationHelper.</param>
		/// <param name="phrase">The phrase.</param>
		/// <param name="parts">Parts information, with alternating sub-phrases and their
		/// occurrence counts (across all phrases in the test).</param>
		/// ------------------------------------------------------------------------------------
		private static void VerifyTranslatablePhrase(PhraseTranslationHelper pth, string phrase,
			params object[] parts)
		{
			TranslatablePhrase phr = pth.UnfilteredPhrases.FirstOrDefault(x => x.OriginalPhrase == phrase.Normalize(NormalizationForm.FormD));
			Assert.IsNotNull(phr);
			Assert.AreEqual(parts.Length / 2, phr.TranslatableParts.Count(), "Phrase \"" + phrase +
				"\" was split into the wrong number of parts.");
			int i = 0;
			foreach (Part part in phr.TranslatableParts)
			{
				Assert.AreEqual(parts[i++], part.Text, "Unexpected part");
				Assert.AreEqual(GetWordCount(part.Text), part.Words.Count(),
					"Unexpected word count for part \"" + part.Text + "\"");
				Assert.AreEqual(parts[i++], part.OwningPhrases.Count(),
					"Unexpected number of owning phrases for part \"" + part.Text + "\"");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the word count of the specified text.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static int GetWordCount(IEnumerable<char> text)
		{
			return 1 + text.Count(c => c == ' ');
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy class so we don't have to use a real list box
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyKeyTermRenderingInfo : ITermRenderingInfo
	{
		#region Implementation of ITermRenderingInfo
		public IKeyTerm Term { get; private set; }

		public IEnumerable<string> Renderings
		{
			get { return Term.Renderings; }
		}

		public int EndOffsetOfRenderingOfPreviousOccurrenceOfThisTerm { get; set; }

		public DummyKeyTermRenderingInfo(IKeyTerm keyTerm, int endOffsetOfPrev)
		{
			Term = keyTerm;
			EndOffsetOfRenderingOfPreviousOccurrenceOfThisTerm = endOffsetOfPrev;
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Trivial implementation of QuestionKey for test questions (not real Scripture references)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TestQ : QuestionKey
	{
		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TestQ(string text, string sRef, int startRef, int endRef)
		{
			Text = text;
			ScriptureReference = sRef;
			StartRef = startRef;
			EndRef = endRef;
		}
		#endregion

		#region Overrides of QuestionKey
		public override string ScriptureReference { get; set; }
		public override int StartRef { get; set; }
		public override int EndRef { get; set; }
		#endregion
	}
}
