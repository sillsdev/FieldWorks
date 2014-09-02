using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// WordBreakGuesser inserts word breaks into a text paragraph, based on the current
	/// wordform inventory.
	/// </summary>
	public class WordBreakGuesser
	{
		Set<string> m_words = new Set<string>(); // From text of word to dummy.
		int m_maxChars; // length of longest word
		private int m_minChars = int.MaxValue; // length of shortest word
		ISilDataAccess m_sda;
		int m_vernWs = 0;
		FdoCache m_cache;
		public WordBreakGuesser(FdoCache cache, int hvoParaStart)
		{
			m_cache = cache;
			m_sda = cache.MainCacheAccessor;
			Setup(hvoParaStart);
		}
		/// <summary>
		/// Parameterless constructor, for testing purposes only
		/// </summary>
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
			int vernWs = TsStringUtils.GetWsAtOffset(para.Contents, 0);
			if (vernWs == m_vernWs)
				return;	// already setup.
			m_vernWs = vernWs;
			//Add all the words from the Wordform Repository
			foreach (var wf in m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances())
			{
				string word = wf.Form.get_String(m_vernWs).Text;
				if (String.IsNullOrEmpty(word))
					continue;
				if (wf.SpellingStatus == (int)SpellingStatusStates.incorrect)
					continue; // if it's known to be incorrect don't use it to split things up!
				m_words.Add(word);
				m_maxChars = Math.Max(m_maxChars, word.Length);
				m_minChars = Math.Min(m_minChars, word.Length);
			}

			//Add all the stems from the Lexicon including all their allomorphs (as I understand this it will capture phrases as well -naylor Aug 2011)
			foreach(var stem in m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances())
			{
				foreach (var morphtype in stem.MorphTypes.Where(morphtype => morphtype.IsStemType)) //for every stem
				{
					var allAllomorphs = stem.AllAllomorphs;
					foreach(var allomorph in allAllomorphs.Where(allomorph => (allomorph.MorphTypeRA != null && !allomorph.MorphTypeRA.IsBoundType))) //for every allomorph of the stem
					{
						var allomorphString = allomorph.Form.get_String(vernWs); //get the string for this form in the desired writing system
						if(allomorphString != null && allomorphString.Length > 0) //if it is a valid word
						{
							var word = allomorphString.Text;
							m_maxChars = Math.Max(m_maxChars, word.Length); //increase max word length if necessary
							m_minChars = Math.Min(m_minChars, word.Length);
							m_words.Add(word); //add word to set
						}
					}
				}
			}
		}

		/// <summary>
		/// Insert breaks for the text in the indicated range of the indicate StTxtPara
		/// </summary>
		/// <param name="start"></param>
		/// <param name="limit"></param>
		/// <param name="hvoPara"></param>
		public void Guess(int start, int limit, int hvoPara)
		{
			Setup(hvoPara);
			ITsString tss = m_sda.get_StringProp(hvoPara, StTxtParaTags.kflidContents);
			ITsStrBldr bldr = tss.GetBldr();

			//Guessing wordbreaks in a string that isn't there is not a good idea.
			if(tss.Text == null)
			{
				return;
			}
			string txt = tss.Text.Substring(start, limit > start && limit < tss.Length ? limit - start : tss.Length - start);
			int offset = 0; // offset in tsb caused by previously introduced spaces.

			//Attempt to handle the problem of invalid words being introduced accounting for punctuation
			//replace every kind of punctuation character in the writing system with ., then split on .
			//
			//just grab the system from the first run, seems unlikely you'll be guessing wordbreaks on strings with runs in different writing systems
			var wsID = tss.get_WritingSystem(0);
			//get the writing system from the cache
			IWritingSystem ws = (IWritingSystem)m_cache.WritingSystemFactory.get_EngineOrNull(wsID);
			//get the ValidCharacters for the writing system.
			ValidCharacters vc = ws != null ? ValidCharacters.Load(ws, e => { }, FwDirectoryFinder.CodeDirectory) : null;
			//split the text on everything found in the OtherCharacters section
			string[] distinctPhrases = vc != null ? txt.Split(vc.OtherCharacters.ToArray(), StringSplitOptions.None) //ws info was good, use it
												  : Regex.Replace(txt, "\\p{P}", ".").Split('.'); //bad ws info, replace all punct with . and split on .
			Set<WordLoc> allWords = new Set<WordLoc>();
			int adjustment = 0;
			foreach (var distinctPhrase in distinctPhrases)
			{
				if(distinctPhrase.Length > 0) //split will give us an empty string wherever there was a punctuation
				{
					Set<WordLoc> foundWords = FindAllMatches(0, distinctPhrase.Length, distinctPhrase);
					foreach (var foundWord in foundWords)
					{
						foundWord.Start += adjustment;
					}
					allWords.AddRange(foundWords);
					adjustment += distinctPhrase.Length;
				}
				++adjustment; //rather than just adding 1 to the adjustment above adjust here. This will handle oddities like ,, or ".
			}

			List<WordLoc> bestWords = BestMatches(txt, allWords);

			foreach(var word in bestWords) //for each word in our list of the best result
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
		/// <param name="txt"></param>
		/// <param name="allWords"></param>
		/// <returns></returns>
		protected static List<WordLoc> BestMatches(string txt, Set<WordLoc> allWords)
		{
			//initialize the matches to an empty list, and the score to the maximum integer
			List<WordLoc> bestMatches = new List<WordLoc>();
			int bestScore = int.MaxValue;
			Dictionary<int, Partition> bestPartitions = new Dictionary<int, Partition>();
			//for each character in the given sentence(txt)
			for (int index = 0; index <= txt.Length; ++index)
			{
				//if we have an existing partition at this index
				if(bestPartitions.ContainsKey(index))
				{
					//pull it out of the dictionary
					Partition node = bestPartitions[index];
					bestPartitions.Remove(index);
					//generate any partitions that could spring from it
					SearchPartition(bestPartitions, node, allWords, txt);
					//if it was the best we've seen so far store its result
					if(node.Score < bestScore)
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
		private static void SearchPartition(Dictionary<int, Partition> partitionList, Partition node, Set<WordLoc> allWords, string txt)
		{
			//for every word in the word list that starts at the index of the current node, create a partition with that word added
			//and the index moved to the end of that word
			foreach (var word in allWords)
			{
				if(word.Start == node.Index)
				{
					var matchedWords = new List<WordLoc>(node.Words) {word};
					Partition newPartition = new Partition(node.Index + word.Length, matchedWords, txt.Length);
					if(!partitionList.ContainsKey(newPartition.Index) ||
						partitionList[newPartition.Index].ScoreBeforePartition > newPartition.ScoreBeforePartition)
					{
						partitionList[newPartition.Index] = newPartition;
					}
				}
			}
			if (!partitionList.ContainsKey(node.Index + 1) ||
						partitionList[node.Index + 1].ScoreBeforePartition > node.ScoreBeforePartition)
			{
				partitionList[node.Index + 1] = new Partition(node.Index + 1, node.Words, txt.Length);
			}
		}

		/// <summary>
		/// Class for storing a step in our breadth first iteration of BestMatches
		/// </summary>
		private sealed class Partition
		{
			private readonly int index;
			private readonly int sentenceLength;
			private readonly List<WordLoc> words;
			public Partition(int index, List<WordLoc> words, int sentenceLength)
			{
				this.index = index;
				this.words = words;
				this.sentenceLength = sentenceLength;
			}

			public int Index
			{
				get { return index; }
			}

			public int Score
			{
				get { return CalculateScore(words, sentenceLength); }
			}

			public int ScoreBeforePartition
			{
				get { return CalculateScore(words, index); }
			}

			public List<WordLoc> Words
			{
				get { return words; }
			}
		}

		/// <summary>
		/// The score is the number of characters in each matched word subtracted from the number of
		/// characters in the sentence.
		/// </summary>
		/// <param name="wordList">words in solution</param>
		/// <param name="worstPossible">characters in sentence</param>
		/// <returns></returns>
		private static int CalculateScore(List<WordLoc> wordList, int worstPossible)
		{
			int score = worstPossible;
			if(wordList == null)
			{
				return score;
			}
			return wordList.Aggregate(score, (current, wordLoc) => current - wordLoc.Length);
		}

		static bool SpaceAt(ITsStrBldr bldr, int ich)
		{
			if(ich >= bldr.Length || ich < 0)
				return false;
			return bldr.Text[ich] == ' ' || bldr.Text[ich] == '\u200B'; // optimize: do some trick with FetchChars
		}

		static void InsertSpace(ITsStrBldr bldr, int ich)
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
		protected Set<WordLoc> FindAllMatches(int start, int limit, string txt)
		{
			if(limit == txt.Length)
				--limit;
			Set<WordLoc> matches = new Set<WordLoc>();
			int max = Math.Min(limit - start, m_maxChars);
			for (int index = 0; index < txt.Length; ++index)
			{
				for(int wordtry = m_minChars; wordtry <= max && wordtry + index <= txt.Length; ++wordtry)
				{
					if (m_words.Contains(txt.Substring(index, wordtry)))
						matches.Add(new WordLoc(index, txt.Substring(index, wordtry)));
				}
			}
			return matches;
		}

		protected sealed class WordLoc : IComparable<WordLoc>
		{
			private int start;
			private string word;

			public WordLoc(int start, string word)
			{
				this.start = start;
				this.word = word;
			}

			public int Start
			{
				get { return start; }
				set { start = value; }
			}

			public int Length
			{
				get { return word.Length; }
			}

			public int CompareTo(WordLoc other)
			{
				return start.CompareTo(other.start) == 0 ? word.CompareTo(other.word) :  start.CompareTo(other.start);
			}

			public bool DoesIntersect(WordLoc other)
			{
				//the start of the other word is not within our range
				return other.start + other.word.Length < start || other.start > start + word.Length;
			}
		}
	}
}
