// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using NUnit.Framework;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
	[TestFixture]
	public class WordBreakGuesserTests
	{
		/// <summary>
		/// Test to make sure that the best match is found, not just the first or shortest word
		/// </summary>
		[Test]
		public void BestFoundNotPartial()
		{
			var tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "the", "there", "is" });
			var breakLocs = tester.BreakResults("thereis");
			Assert.True(breakLocs.Length == 2); //we should have found 2 words
			Assert.True(breakLocs[0] == 0 && breakLocs[1] == 5);//there at index 0, and is at index 5
		}

		/// <summary>
		/// Test to make sure the best is found, not just the longest word matched
		/// </summary>
		[Test]
		public void BestFoundNotLongest()
		{
			var tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "the", "there", "is", "rest", "easy" });
			var breakLocs = tester.BreakResults("therestiseasy");
			Assert.True(breakLocs.Length == 4);
			Assert.True(breakLocs[0] == 0 && breakLocs[1] == 3 && breakLocs[2] == 7 && breakLocs[3] == 9);
		}

		/// <summary>
		/// Test to make sure the best is found when there are a number of options
		/// </summary>
		[Test]
		public void BestFoundMoreRobust()
		{
			var tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "the", "he", "here", "a", "there", "is", "rest", "easy" });
			var breakLocs = tester.BreakResults("therestiseasy");
			Assert.True(breakLocs.Length == 4);
			Assert.True(breakLocs[0] == 0 && breakLocs[1] == 3 && breakLocs[2] == 7 && breakLocs[3] == 9);
		}

		/// <summary>
		/// Test to make sure the best is found when there are a number of options
		/// </summary>
		[Test]
		public void BestFoundPunctuationTest()
		{
			var tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "there", "isn't", "a", "problem", "is", "this", "fail" });
			var breakLocs = tester.BreakResults("thereisn'tapunctuationproblem,isthere?thisshould'tfailifthereis");
			Assert.True(breakLocs.Length == 11);
			Assert.True(breakLocs[0] == 0 && breakLocs[1] == 5 && breakLocs[2] == 10 && breakLocs[10] == 61);
		}

		[Test]
		public void DontDieOnLongData()
		{
			var tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "the", "a", "is", "rest", "easy", "that", "for", "there", "on" });
			var breakLocs = tester.BreakResults("fourscoreandsevenyearsagoourforefathersbroughtforthonthiscontinentanewnationconcievedinliberty" +
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
			Assert.True(breakLocs.Length == 165);
		}

		/// <summary>
		/// In the practical use of guessing word breaks, the whole sentence is likely to have been added
		/// to the word list, we don't want to match that temporary fake word, this test ensures that behavior.
		/// </summary>
		[Test]
		public void SkipFalseWholeSentenceWord()
		{
			var tester = new WordBreakGuesserTester();
			tester.Init(new List<string> { "thisisnotaword" });
			var breakLocs = tester.BreakResults("thisisnotaword");
			Assert.True(breakLocs.Length == 0);
		}

		private sealed class WordBreakGuesserTester : WordBreakGuesser
		{
			public void Init(IEnumerable<string> wordList)
			{
				Setup(wordList);
			}

			//returns an integer array of the starting index for every word in the best match.
			public int[] BreakResults(string txt)
			{
				var matches = FindAllMatches(0, txt.Length - 1, txt);
				var results = BestMatches(txt, matches);
				var wordBreaks = new int[results.Count];
				for (var i = 0; i < results.Count; i++)
				{
					wordBreaks[i] = results[i].Start;
				}
				return wordBreaks;
			}
		}
	}
}