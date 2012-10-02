// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: KeyTermMatchBuilderTests.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rhino.Mocks;

namespace SILUBS.PhraseTranslationHelper
{
	[TestFixture]
	public class KeyTermMatchBuilderTests
	{
		#region Sanitized data Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the simple case of a key term consisting of
		/// a single required word with no optional parts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SingleWordKeyTerm()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("fun"));
			Assert.AreEqual(1, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "fun");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term consisting of
		/// two required words with no optional parts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TwoWordKeyTerm()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("fun stuff"));
			Assert.AreEqual(1, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "fun", "stuff");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term consisting of
		/// a verb with the implictly optional word "to".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TwoWordKeyTermWithImplicitOptionalInfinitiveMarker()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("to cry"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "cry");
			VerifyKeyTermMatch(bldr, 1, "to", "cry");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term with an optional
		/// leading word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OptionalLeadingWord()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("(fun) stuff"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "stuff");
			VerifyKeyTermMatch(bldr, 1, "fun", "stuff");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term with an optional
		/// middle word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OptionalMiddleWord()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("really (fun) stuff"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "really", "stuff");
			VerifyKeyTermMatch(bldr, 1, "really", "fun", "stuff");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term with an optional
		/// trailing word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OptionalTrailingWord()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("morning (star)"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "morning");
			VerifyKeyTermMatch(bldr, 1, "morning", "star");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term with an optional
		/// leading phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OptionalPhrase()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("(things of this) life"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "life");
			VerifyKeyTermMatch(bldr, 1, "things", "of", "this", "life");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term with a single required
		/// word with an optional initial part.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OptionalInitialPart()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("(loving)kindness"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "kindness");
			VerifyKeyTermMatch(bldr, 1, "lovingkindness");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term with a single required
		/// word with an optional middle part. (It's unlikely that there is any actual data like
		/// this.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OptionalMiddlePart()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("anti(dis)establishment"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "antiestablishment");
			VerifyKeyTermMatch(bldr, 1, "antidisestablishment");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term with a single required
		/// word with an optional final part.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OptionalFinalPart()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("kind(ness)"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "kind");
			VerifyKeyTermMatch(bldr, 1, "kindness");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term with a single required
		/// word with an optional final part.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OptionalFinal()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("kind(ness)"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "kind");
			VerifyKeyTermMatch(bldr, 1, "kindness");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a complex multi-word key term
		/// consisting of weird optional parts and phrases.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NastyBeyondBelief()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("to (have) beg(ged) for (loving)kindness (and mercy)"));
			Assert.AreEqual(32, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "beg", "for", "kindness");
			VerifyKeyTermMatch(bldr, 1, "to", "beg", "for", "kindness");
			VerifyKeyTermMatch(bldr, 2, "have", "beg", "for", "kindness");
			VerifyKeyTermMatch(bldr, 3, "to", "have", "beg", "for", "kindness");
			VerifyKeyTermMatch(bldr, 4, "begged", "for", "kindness");
			VerifyKeyTermMatch(bldr, 5, "to", "begged", "for", "kindness");
			VerifyKeyTermMatch(bldr, 6, "have", "begged", "for", "kindness");
			VerifyKeyTermMatch(bldr, 7, "to", "have", "begged", "for", "kindness");
			VerifyKeyTermMatch(bldr, 8, "beg", "for", "lovingkindness");
			VerifyKeyTermMatch(bldr, 9, "to", "beg", "for", "lovingkindness");
			VerifyKeyTermMatch(bldr, 10, "have", "beg", "for", "lovingkindness");
			VerifyKeyTermMatch(bldr, 11, "to", "have", "beg", "for", "lovingkindness");
			VerifyKeyTermMatch(bldr, 12, "begged", "for", "lovingkindness");
			VerifyKeyTermMatch(bldr, 13, "to", "begged", "for", "lovingkindness");
			VerifyKeyTermMatch(bldr, 14, "have", "begged", "for", "lovingkindness");
			VerifyKeyTermMatch(bldr, 15, "to", "have", "begged", "for", "lovingkindness");
			VerifyKeyTermMatch(bldr, 16, "beg", "for", "kindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 17, "to", "beg", "for", "kindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 18, "have", "beg", "for", "kindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 19, "to", "have", "beg", "for", "kindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 20, "begged", "for", "kindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 21, "to", "begged", "for", "kindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 22, "have", "begged", "for", "kindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 23, "to", "have", "begged", "for", "kindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 24, "beg", "for", "lovingkindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 25, "to", "beg", "for", "lovingkindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 26, "have", "beg", "for", "lovingkindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 27, "to", "have", "beg", "for", "lovingkindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 28, "begged", "for", "lovingkindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 29, "to", "begged", "for", "lovingkindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 30, "have", "begged", "for", "lovingkindness", "and", "mercy");
			VerifyKeyTermMatch(bldr, 31, "to", "have", "begged", "for", "lovingkindness", "and", "mercy");
		}
		#endregion

		#region Rules tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a term which has a rule to
		/// include both the original term and add an alternate.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RuleToKeepOriginalTermAndAddAnAlternate()
		{
			Dictionary<string, KeyTermRule> rules = new Dictionary<string, KeyTermRule>();
			KeyTermRule rule = new KeyTermRule();
			rule.id = "Jesus";
			rule.Alternates = new [] {new KeyTermRulesKeyTermRuleAlternate()};
			rule.Alternates[0].Name = "Jesus Christ";
			rules[rule.id] = rule;
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("Jesus"), rules);
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "jesus", "christ");
			VerifyKeyTermMatch(bldr, 1, "jesus");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a term which has a rule to
		/// exclude it (no alternates).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RuleToExcludeTermCompletely()
		{
			Dictionary<string, KeyTermRule> rules = new Dictionary<string, KeyTermRule>();
			KeyTermRule rule = new KeyTermRule();
			rule.id = "Jesus";
			rule.Rule = KeyTermRule.RuleType.Exclude;
			rules[rule.id] = rule;
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("Jesus"), rules);
			Assert.AreEqual(0, bldr.Matches.Count());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a term which has a rule to
		/// restrict it to match only to certain references.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RuleToLimitMatchToTermRefs()
		{
			Dictionary<string, KeyTermRule> rules = new Dictionary<string, KeyTermRule>();
			KeyTermRule rule = new KeyTermRule();
			rule.id = "ask";
			rule.Rule = KeyTermRule.RuleType.MatchForRefOnly;
			rules[rule.id] = rule;
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm(rule.id, 34), rules);
			Assert.AreEqual(1, bldr.Matches.Count());
			KeyTermMatch ktm = VerifyKeyTermMatch(bldr, 0, false, "ask");
			Assert.IsFalse(ktm.AppliesTo(30, 33));
			Assert.IsTrue(ktm.AppliesTo(34, 34));
			Assert.IsFalse(ktm.AppliesTo(35, 39));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a term which has a rule to
		/// exclude it, using alternates instead.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RuleToReplaceOriginalTermWithAlternates()
		{
			Dictionary<string, KeyTermRule> rules = new Dictionary<string, KeyTermRule>();
			KeyTermRule rule = new KeyTermRule();
			rule.id = "to lift up (one's hand, heart, or soul) = to worship, pray";
			rule.Rule = KeyTermRule.RuleType.Exclude;
			rule.Alternates = new[] { new KeyTermRulesKeyTermRuleAlternate(), new KeyTermRulesKeyTermRuleAlternate(), new KeyTermRulesKeyTermRuleAlternate() };
			rule.Alternates[0].Name = "worship";
			rule.Alternates[1].Name = "praise exuberantly";
			rule.Alternates[2].Name = "pray";
			rules[rule.id] = rule;
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm(rule.id), rules);
			Assert.AreEqual(3, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "worship");
			VerifyKeyTermMatch(bldr, 1, "praise", "exuberantly");
			VerifyKeyTermMatch(bldr, 2, "pray");
		}
		#endregion

		#region Really hard Real Data tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData1()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("worm, maggot"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "worm");
			VerifyKeyTermMatch(bldr, 1, "maggot");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData2()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("castor oil plant (FF 106, 107)"));
			Assert.LessOrEqual(1, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "castor", "oil", "plant");
			// Ideally, we don't want to get anything for the junk in parentheses, but it
			// shouldn't really hurt anything, so we'll live with it.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData3()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("(loving)kindness, solidarity, joint liability, grace"));
			Assert.AreEqual(5, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "kindness");
			VerifyKeyTermMatch(bldr, 1, "lovingkindness");
			VerifyKeyTermMatch(bldr, 2, "solidarity");
			VerifyKeyTermMatch(bldr, 3, "joint", "liability");
			VerifyKeyTermMatch(bldr, 4, "grace");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData4()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("Canaanean = Zealot"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "canaanean");
			VerifyKeyTermMatch(bldr, 1, "zealot");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData5()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("dreadful event or sight"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "dreadful", "event");
			VerifyKeyTermMatch(bldr, 1, "dreadful", "sight");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData6()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("exempt, free from"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "exempt");
			VerifyKeyTermMatch(bldr, 1, "free", "from");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData7()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("someone who sins against someone else and therefore 'owes' that person"));
			Assert.AreEqual(1, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "someone", "who", "sins", "against", "someone", "else", "and", "therefore", "owes", "that", "person");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData8()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("state of fearing, standing in awe"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "state", "of", "fearing");
			VerifyKeyTermMatch(bldr, 1, "standing", "in", "awe");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData9()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("to be favorably disposed to someone, or to experience an emotion of compassion towards other people"));
			Assert.AreEqual(4, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "be", "favorably", "disposed", "to", "someone");
			VerifyKeyTermMatch(bldr, 1, "to", "be", "favorably", "disposed", "to", "someone");
			VerifyKeyTermMatch(bldr, 2, "experience", "an", "emotion", "of", "compassion", "towards", "other", "people");
			VerifyKeyTermMatch(bldr, 3, "to", "experience", "an", "emotion", "of", "compassion", "towards", "other", "people");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData10()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("to recompense, to reward, to pay"));
			Assert.AreEqual(6, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "recompense");
			VerifyKeyTermMatch(bldr, 1, "to", "recompense");
			VerifyKeyTermMatch(bldr, 2, "reward");
			VerifyKeyTermMatch(bldr, 3, "to", "reward");
			VerifyKeyTermMatch(bldr, 4, "pay");
			VerifyKeyTermMatch(bldr, 5, "to", "pay");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data: missing closing parenthesis for optional phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("We're still trying to figure out what to do with this.")]
		public void RealData11()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("to lift up (one's hand, heart, or soul) = to worship, pray"));
			Assert.AreEqual(7, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "lift", "up");
			VerifyKeyTermMatch(bldr, 1, "to", "lift", "up");
			VerifyKeyTermMatch(bldr, 2, "lift", "up", "one's", "hand", "heart", "soul");
			VerifyKeyTermMatch(bldr, 3, "to", "lift", "up", "one's", "hand", "heart", "soul");
			VerifyKeyTermMatch(bldr, 4, "worship");
			VerifyKeyTermMatch(bldr, 5, "to", "worship");
			VerifyKeyTermMatch(bldr, 6, "pray");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data: missing closing parenthesis for optional phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealData12()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("olive oil (used as food"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "olive", "oil");
			VerifyKeyTermMatch(bldr, 1, "olive", "oil", "used", "as", "food");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data: "or" separating two words
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealDataWithOr1()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("courtyard or sheepfold"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "courtyard");
			VerifyKeyTermMatch(bldr, 1, "sheepfold");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data: "or" separating two two-word phrases, with more text following
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealDataWithOr2()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("give up or lay aside what one possesses"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "give", "up", "what", "one", "possesses");
			VerifyKeyTermMatch(bldr, 1, "lay", "aside", "what", "one", "possesses");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data: "or" separating two three-word phrases, with more text preceeding
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealDataWithOr3()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("to perform the ritual of removing the state of guilt or uncleanness from oneself"));
			Assert.AreEqual(4, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "perform", "the", "ritual", "of", "removing", "the", "state", "of", "guilt");
			VerifyKeyTermMatch(bldr, 1, "to", "perform", "the", "ritual", "of", "removing", "the", "state", "of", "guilt");
			VerifyKeyTermMatch(bldr, 2, "perform", "the", "ritual", "of", "removing", "the", "uncleanness", "from", "oneself");
			VerifyKeyTermMatch(bldr, 3, "to", "perform", "the", "ritual", "of", "removing", "the", "uncleanness", "from", "oneself");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermMatchBuilder class in the case of a key term from the world of
		/// real evil data: "or" separating two three-word phrases, with more text preceeding
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RealDataWithOr4()
		{
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(AddMockedKeyTerm("and the flowers are white or pink. The whole plant gives off an agreeable odour"));
			Assert.AreEqual(2, bldr.Matches.Count());
			VerifyKeyTermMatch(bldr, 0, "and", "the", "flowers", "are", "white", "off", "an", "agreeable", "odour");
			VerifyKeyTermMatch(bldr, 1, "pink.", "the", "whole", "plant", "gives", "off", "an", "agreeable", "odour");
		}
		#endregion

		#region private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the mocked key term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static IKeyTerm AddMockedKeyTerm(string term, params int[] occurences)
		{
			IKeyTerm mockedKt = MockRepository.GenerateStub<IKeyTerm>();
			mockedKt.Stub(kt => kt.Term).Return(term);
			mockedKt.Stub(kt => kt.BcvOccurences).Return(occurences.Length > 0 ? occurences : new[] { 0 });
			return mockedKt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the key term match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void VerifyKeyTermMatch(KeyTermMatchBuilder bldr, int iMatch,
			params string[] words)
		{
			VerifyKeyTermMatch(bldr, iMatch, true, words);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the key term match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static KeyTermMatch VerifyKeyTermMatch(KeyTermMatchBuilder bldr, int iMatch,
			bool matchAnywhere, params string[] words)
		{
			KeyTermMatch ktm = bldr.Matches.ElementAt(iMatch);
			Assert.AreEqual(words.Length, ktm.Words.Count());
			for (int i = 0; i < words.Length; i++)
				Assert.AreEqual(words[i], ktm.Words.ElementAt(i).Text);
			if (matchAnywhere)
			{
				Random r = new Random(DateTime.Now.Millisecond);
				Assert.IsTrue(ktm.AppliesTo(r.Next(), r.Next()));
			}
			return ktm;
		}
		#endregion
	}
}
