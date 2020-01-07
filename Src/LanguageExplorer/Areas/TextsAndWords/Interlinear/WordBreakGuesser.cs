// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// WordBreakGuesser inserts word breaks into a text paragraph, based on the current
	/// wordform inventory.
	/// </summary>
	public class WordBreakGuesser
	{
		private readonly HashSet<string> m_words = new HashSet<string>(); // From text of word to dummy.
		private int m_maxChars; // length of longest word
		private int m_minChars = int.MaxValue; // length of shortest word
		private ISilDataAccess m_sda;
		private int m_vernWs;
		private LcmCache m_cache;

		public WordBreakGuesser(LcmCache cache, int hvoParaStart)
		{
			m_cache = cache;
			m_sda = cache.MainCacheAccessor;
			Setup(hvoParaStart);
		}

		/// <summary />
		/// <remarks>Do not use except in test code.</remarks>
		public WordBreakGuesser()
		{
			//this method intentionally left blank.
		}

		/// <summary>
		/// Testing Setup method
		/// </summary>
		/// <remarks>to be used only by test code.</remarks>
		protected void Setup(IEnumerable<string> wordList)
		{
			foreach (var word in wordList)
			{
				m_words.Add(word);
				m_maxChars = Math.Max(m_maxChars, word.Length);
				m_minChars = Math.Min(m_minChars, word.Length);
			}
		}

		private void Setup(int hvoPara)
		{
			var para = (IStTxtPara)m_cache.ServiceLocator.GetObject(hvoPara);
			var vernWs = TsStringUtils.GetWsAtOffset(para.Contents, 0);
			if (vernWs == m_vernWs)
			{
				return; // already setup.
			}
			m_vernWs = vernWs;
			//Add all the words from the Wordform Repository
			foreach (var wf in m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances())
			{
				var word = wf.Form.get_String(m_vernWs).Text;
				if (string.IsNullOrEmpty(word))
				{
					continue;
				}
				if (wf.SpellingStatus == (int)SpellingStatusStates.incorrect)
				{
					continue; // if it's known to be incorrect don't use it to split things up!
				}
				m_words.Add(word);
				m_maxChars = Math.Max(m_maxChars, word.Length);
				m_minChars = Math.Min(m_minChars, word.Length);
			}

			//Add all the stems from the Lexicon including all their allomorphs (as I understand this it will capture phrases as well -naylor Aug 2011)
			foreach (var stem in m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances())
			{
				foreach (var allomorph in stem.AllAllomorphs.Where(allomorph => allomorph.MorphTypeRA != null && !allomorph.MorphTypeRA.IsBoundType))
				{
					var allomorphString = allomorph.Form.get_String(vernWs); //get the string for this form in the desired writing system
					if (allomorphString == null || allomorphString.Length <= 0)
					{
						continue;
					}
					// It is a valid word.
					var word = allomorphString.Text;
					m_maxChars = Math.Max(m_maxChars, word.Length); //increase max word length if necessary
					m_minChars = Math.Min(m_minChars, word.Length);
					m_words.Add(word); //add word to set
				}
			}
		}

		/// <summary>
		/// Insert breaks for the text in the indicated range of the indicate StTxtPara
		/// </summary>
		public void Guess(int start, int limit, int hvoPara)
		{
			Setup(hvoPara);
			var tss = m_sda.get_StringProp(hvoPara, StTxtParaTags.kflidContents);
			var bldr = tss.GetBldr();
			//Guessing wordbreaks in a string that isn't there is not a good idea.
			if (tss.Text == null)
			{
				return;
			}
			var txt = tss.Text.Substring(start, limit > start && limit < tss.Length ? limit - start : tss.Length - start);
			var offset = 0; // offset in tsb caused by previously introduced spaces.
			//Attempt to handle the problem of invalid words being introduced accounting for punctuation
			//replace every kind of punctuation character in the writing system with ., then split on .
			//
			//just grab the system from the first run, seems unlikely you'll be guessing wordbreaks on strings with runs in different writing systems
			var wsID = tss.get_WritingSystem(0);
			//get the writing system from the cache
			var ws = (CoreWritingSystemDefinition)m_cache.WritingSystemFactory.get_EngineOrNull(wsID);
			//get the ValidCharacters for the writing system.
			var vc = ws != null ? ValidCharacters.Load(ws) : null;
			//split the text on everything found in the OtherCharacters section
			var distinctPhrases = vc != null ? txt.Split(vc.OtherCharacters.ToArray(), StringSplitOptions.None) //ws info was good, use it
												  : Regex.Replace(txt, "\\p{P}", ".").Split('.'); //bad ws info, replace all punct with . and split on .
			var allWords = new HashSet<WordLoc>();
			var adjustment = 0;
			foreach (var distinctPhrase in distinctPhrases)
			{
				if (distinctPhrase.Length > 0) //split will give us an empty string wherever there was a punctuation
				{
					var foundWords = FindAllMatches(0, distinctPhrase.Length, distinctPhrase);
					foreach (var foundWord in foundWords)
					{
						foundWord.Start += adjustment;
					}
					allWords.UnionWith(foundWords);
					adjustment += distinctPhrase.Length;
				}
				++adjustment; //rather than just adding 1 to the adjustment above adjust here. This will handle oddities like ,, or ".
			}
			var bestWords = BestMatches(txt, allWords);
			foreach (var word in bestWords) //for each word in our list of the best result
			{
				//unless the word starts at the beginning of the input or the word starts right after the end of the last word we found
				//insert a space before the word.
				if (word.Start != 0)
				{
					//insert a space before the word.
					if (!SpaceAt(bldr, start + word.Start + offset - 1)) //if there isn't already a space
					{
						InsertSpace(bldr, start + word.Start + offset);
						++offset;
					}
				}
				if (word.Start + word.Length != txt.Length) //unless the word ends at the end of our input
				{

					if (!SpaceAt(bldr, start + word.Start + word.Length + offset)) //if there isn't already a space
					{
						InsertSpace(bldr, start + word.Start + word.Length + offset); //insert a space at the end of the word
						++offset;
					}
				}
			}
			if (offset > 0)
			{
				UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoGuessWordBreaks, ITextStrings.ksRedoGuessWordBreaks, m_cache.ActionHandlerAccessor,
				() => m_sda.SetString(hvoPara, StTxtParaTags.kflidContents, bldr.GetString()));
			}
		}

		/// <summary>
		/// This method drives a breadth first iteration of the possible matches from allWords in the txt string.
		/// </summary>
		protected static List<WordLoc> BestMatches(string txt, ISet<WordLoc> allWords)
		{
			//initialize the matches to an empty list, and the score to the maximum integer
			var bestMatches = new List<WordLoc>();
			var bestScore = int.MaxValue;
			var bestPartitions = new Dictionary<int, Partition>();
			//for each character in the given sentence(txt)
			for (var index = 0; index <= txt.Length; ++index)
			{
				//if we have an existing partition at this index
				if (bestPartitions.ContainsKey(index))
				{
					//pull it out of the dictionary
					var node = bestPartitions[index];
					bestPartitions.Remove(index);
					//generate any partitions that could spring from it
					SearchPartition(bestPartitions, node, allWords, txt);
					//if it was the best we've seen so far store its result
					if (node.Score < bestScore)
					{
						bestScore = node.Score;
						bestMatches = node.Words;
					}
				}
				else //we haven't generated any partition that starts at this index, generate one with no previously found words
				{
					SearchPartition(bestPartitions, new Partition(index, new List<WordLoc>(), txt.Length), allWords, txt);
				}
			}
			return bestMatches;
		}

		/// <summary>
		/// This method is used to extend our search from the given node.
		/// </summary>
		/// <param name="partitionList">List to populate with all possible next steps</param>
		/// <param name="node">The current node to traverse</param>
		/// <param name="allWords">The list of possible words in the string</param>
		/// <param name="txt">The sentence to search</param>
		private static void SearchPartition(Dictionary<int, Partition> partitionList, Partition node, ISet<WordLoc> allWords, string txt)
		{
			//for every word in the word list that starts at the index of the current node, create a partition with that word added
			//and the index moved to the end of that word
			foreach (var word in allWords)
			{
				if (word.Start != node.Index)
				{
					continue;
				}
				var matchedWords = new List<WordLoc>(node.Words) { word };
				var newPartition = new Partition(node.Index + word.Length, matchedWords, txt.Length);
				if (!partitionList.ContainsKey(newPartition.Index) || partitionList[newPartition.Index].ScoreBeforePartition > newPartition.ScoreBeforePartition)
				{
					partitionList[newPartition.Index] = newPartition;
				}
			}
			if (!partitionList.ContainsKey(node.Index + 1) || partitionList[node.Index + 1].ScoreBeforePartition > node.ScoreBeforePartition)
			{
				partitionList[node.Index + 1] = new Partition(node.Index + 1, node.Words, txt.Length);
			}
		}

		/// <summary>
		/// The score is the number of characters in each matched word subtracted from the number of
		/// characters in the sentence.
		/// </summary>
		/// <param name="wordList">words in solution</param>
		/// <param name="worstPossible">characters in sentence</param>
		private static int CalculateScore(List<WordLoc> wordList, int worstPossible)
		{
			var score = worstPossible;
			return wordList?.Aggregate(score, (current, wordLoc) => current - wordLoc.Length) ?? score;
		}

		private static bool SpaceAt(ITsStrBldr bldr, int ich)
		{
			if (ich >= bldr.Length || ich < 0)
			{
				return false;
			}
			return bldr.Text[ich] == ' ' || bldr.Text[ich] == '\u200B'; // optimize: do some trick with FetchChars
		}

		private static void InsertSpace(ITsStrBldr bldr, int ich)
		{
			bldr.Replace(ich, ich, "\u200B", null);
		}

		/// <summary>
		/// Find the longest match possible starting at start up to but not including limit
		/// </summary>
		/// <param name="start"></param>
		/// <param name="limit">this limit should be one less than the text length to avoid matching false whole sentence wordforms</param>
		/// <param name="txt"></param>
		/// <returns></returns>
		protected ISet<WordLoc> FindAllMatches(int start, int limit, string txt)
		{
			if (limit == txt.Length)
			{
				--limit;
			}
			var matches = new HashSet<WordLoc>();
			var max = Math.Min(limit - start, m_maxChars);
			for (var index = 0; index < txt.Length; ++index)
			{
				for (var wordtry = m_minChars; wordtry <= max && wordtry + index <= txt.Length; ++wordtry)
				{
					if (m_words.Contains(txt.Substring(index, wordtry)))
					{
						matches.Add(new WordLoc(index, txt.Substring(index, wordtry)));
					}
				}
			}
			return matches;
		}

		/// <summary />
		/// <remarks>
		/// This is only protected, because tests want to use it.
		/// </remarks>
		protected sealed class WordLoc : IComparable<WordLoc>
		{
			public WordLoc(int start, string word)
			{
				Start = start;
				Word = word;
			}

			private string Word { get; }

			public int Start { get; set; }

			public int Length => Word.Length;

			public int CompareTo(WordLoc other)
			{
				return Start.CompareTo(other.Start) == 0 ? Word.CompareTo(other.Word) : Start.CompareTo(other.Start);
			}

			public bool DoesIntersect(WordLoc other)
			{
				//the start of the other word is not within our range
				return other.Start + other.Word.Length < Start || other.Start > Start + Word.Length;
			}
		}

		/// <summary>
		/// Class for storing a step in our breadth first iteration of BestMatches
		/// </summary>
		private sealed class Partition
		{
			private int SentenceLength { get; }

			public Partition(int index, List<WordLoc> words, int sentenceLength)
			{
				Index = index;
				Words = words;
				SentenceLength = sentenceLength;
			}

			public int Index { get; }

			public int Score => CalculateScore(Words, SentenceLength);

			public int ScoreBeforePartition => CalculateScore(Words, Index);

			public List<WordLoc> Words { get; }
		}
	}
}