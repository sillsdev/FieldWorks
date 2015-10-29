// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.IText
{
	class WordBreakGuesserTests
	{
		/// <summary>
		/// Test to make sure that the best match is found, not just the first or shortest word
		/// </summary>
		[Test]
		public void BestFoundNotPartial()
		{
			WordBreakGuesserTester tester = new WordBreakGuesserTester();
			tester.Init(new List<string> {"the", "there","is"});
			int[] breakLocs = tester.BreakResults("thereis");
			Assert.True(breakLocs.Length == 2); //we should have found 2 words
			Assert.True(breakLocs[0] == 0 && breakLocs[1] == 5);//there at index 0, and is at index 5
		}

		/// <summary>
		/// Test to make sure the best is found, not just the longest word matched
		/// </summary>
		[Test]
		public void BestFoundNotLongest()
		{
			WordBreakGuesserTester tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "the", "there", "is", "rest", "easy"});
			int[] breakLocs = tester.BreakResults("therestiseasy");
			Assert.True(breakLocs.Length == 4); //we should have found four words
			//the at index 0, rest at 3, is at 7, easy at 9
			Assert.True(breakLocs[0] == 0 && breakLocs[1] == 3 && breakLocs[2] == 7 && breakLocs[3] == 9);
		}

		/// <summary>
		/// Test to make sure the best is found when there are a number of options
		/// </summary>
		[Test]
		public void BestFoundMoreRobust()
		{
			WordBreakGuesserTester tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "the", "he", "here", "a", "there", "is", "rest", "easy" });
			int[] breakLocs = tester.BreakResults("therestiseasy");
			Assert.True(breakLocs.Length == 4); //we should have found four words
			//the at index 0, rest at 3, is at 7, easy at 9
			Assert.True(breakLocs[0] == 0 && breakLocs[1] == 3 && breakLocs[2] == 7 && breakLocs[3] == 9);
		}

		/// <summary>
		/// Test to make sure the best is found when there are a number of options
		/// </summary>
		[Test]
		public void BestFoundPunctuationTest()
		{
			WordBreakGuesserTester tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "there", "isn't", "a", "problem", "is", "this", "fail" });
			int[] breakLocs = tester.BreakResults("thereisn'tapunctuationproblem,isthere?thisshould'tfailifthereis");
			Assert.True(breakLocs.Length == 11); //we should have found thirteen words
			//there at index 0, isn't at 5, a at 10, is at 61
			Assert.True(breakLocs[0] == 0 && breakLocs[1] == 5 && breakLocs[2] == 10 && breakLocs[10] == 61);
		}

		[Test]
		public void DontDieOnLongData()
		{
			WordBreakGuesserTester tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "the", "a", "is", "rest", "easy", "that", "for", "there", "on"});
			int[] breakLocs = tester.BreakResults("fourscoreandsevenyearsagoourforefathersbroughtforthonthiscontinentanewnationconcievedinliberty" +
												  "anddedicatedtothepropsitionthatallmenarecreatedequalnowweareengagedinagreatcivilwartestingwhether" +
												  "thatnationoranynationsoconceivedandsodedicatedcanlongendourewearemetonagreatbattlefieldofthatwar" +
												  "wehavecometodedicateaportionofthatfieldasafinalrestingplaceforthosewhoheregavetheirlivesthatthat" +
												  "nationmightliveitisaltogetherfittingandproperthatweshoulddothisbutinalargersensewecannotdedicate" +
												  "wecannotconsecratewecannothallowthisgroundthebravemenlivinganddeadwhostruggledherehaveconsecrated" +
												  "itfaraboveourpoorpowertoaddordetracttheworldwilllittlenotenorlongrememberwhatwesayherebutitcannever" +
												  "forgetwhattheydidhereitisforusthelivingrathertobededicatedheeretotheunfinishedworkwhichtheywhofought" +
												  "herehavethusfarsonoblyadvanceditisratherforustobeherededicatedtothegreattaskremainingbeforeusthatfrom" +
												  "thesehonoreddeadwetakeincreaseddevotiontothatcauseforwhichtheygavethelastfullmeasureofdevotion" +
												  "thatweherehighlyresolvethatthesedeadshallnothavediedinvainthatthisnationunderGodshallhaveanewbirth" +
												  "offreedomandthatgovernmentofthepeoplebythepeopleandforthepeopleshallnotperishfromtheearth");
			Assert.True(breakLocs.Length == 165); //we should have found 165 words
		}

		/// <summary>
		/// In the practical use of guessing word breaks, the whole sentence is likely to have been added
		/// to the word list, we don't want to match that temporary fake word, this test ensures that behavior.
		/// </summary>
		[Test]
		public void SkipFalseWholeSentenceWord()
		{
			WordBreakGuesserTester tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "thisisnotaword" });
			int[] breakLocs = tester.BreakResults("thisisnotaword");
			Assert.True(breakLocs.Length == 0);
		}

		sealed class WordBreakGuesserTester : WordBreakGuesser
		{
			public void Init(IEnumerable<string> wordList)
			{
				Setup(wordList);
			}
			//returns an integer array of the starting index for every word in the best match.
			public int[] BreakResults(string txt)
			{
				SIL.Utils.Set<WordLoc> matches = FindAllMatches(0, txt.Length - 1, txt);
				List<WordLoc> results = BestMatches(txt, matches);
				int[] wordBreaks = new int[results.Count];
				for (int i = 0; i < results.Count; i++)
				{
					wordBreaks[i] = results[i].Start;
				}
				return wordBreaks;
			}
		}
	}
}
