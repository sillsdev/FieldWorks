using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.FDO.DomainServices.SemanticDomainSearch
{
	/// <summary>
	/// This strategy takes a LexSense and finds keywords from the sense's Gloss,
	/// Definition and Reversal Entries and uses them to search (primarily) for
	/// whole word matches in Semantic Domains.
	/// </summary>
	class SenseSearchStrategy : SemDomSearchStrategy
	{
		// Maps from writing system to words that occur in the sense that we should look for in that writing system.
		private readonly Dictionary<int, HashSet<string>> m_searchKeys;
		private readonly ILexSense m_sense;
		private const int MIN_SEARCH_KEY_LENGTH = 3;

		/// <summary>
		/// Create and setup a search for keywords from a LexSense in the Semantic Domain list.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sense"></param>
		public SenseSearchStrategy(FdoCache cache, ILexSense sense) : base(cache)
		{
			m_sense = sense;
			m_searchKeys = GetSearchKeysFromSense();
		}

		internal Dictionary<int, HashSet<string>> GetSearchKeysFromSense()
		{
			var results = new Dictionary<int, HashSet<string>>();
			AddKeysFrom(m_sense.Gloss, results, int.MaxValue);
			AddKeysFrom(m_sense.Definition, results, 2);
			var reversalEntries = m_sense.ReversalEntriesRC;
			foreach (IReversalIndexEntry reversalIndexEntry in reversalEntries)
				AddKeysFrom(reversalIndexEntry.ReversalForm, results, int.MaxValue);

			// Enhance (GJM): Eventually add Translation Equivalent when implemented?

			return results;
		}

		/// <summary>
		/// For each ws in ms, add to the set of strings under that key in results the words from that alternative.
		/// If the alternative contains more than maxWords words, skip it.
		/// </summary>
		/// <param name="ms"></param>
		/// <param name="results"></param>
		/// <param name="maxWords"></param>
		private void AddKeysFrom(IMultiAccessorBase ms, Dictionary<int, HashSet<string>> results, int maxWords)
		{
			foreach (var ws in ms.AvailableWritingSystemIds)
			{
				string wordSource = ms.get_String(ws).Text;
				if (wordSource == null)
					continue;
				var words = (from Match match in WordParsingRegex.Matches(wordSource) select match.Value).ToArray();
				if (words.Length > maxWords || words.Length == 0)
					continue;
				HashSet<string> wordSet;
				if (!results.TryGetValue(ws, out wordSet))
				{
					wordSet = new HashSet<string>();
					results[ws] = wordSet;
				}
				foreach (var word in words.Where(
					word => !String.IsNullOrEmpty(word) && word.Length >= MIN_SEARCH_KEY_LENGTH))
				{
					wordSet.Add(word);
				}
			}
		}

		/// <summary>
		/// A list of semantic domains where one of the search keys matched the first part of
		/// one of the words in the Name or Example Words fields.
		/// </summary>
		public IEnumerable<ICmSemanticDomain> PartialMatches { get { return SearchResults.SortedBucketX(WholeWordBucketIndex + PartialMatchRelativeIndex).Concat(SearchResults.SortedBucketX(WholeWordExamplesBucketIndex + PartialMatchRelativeIndex)); } }

		/// <summary>
		/// A list of semantic domains where one of the search keys completely matched
		/// one of the words in the Name or Example Words fields.
		/// </summary>
		public IEnumerable<ICmSemanticDomain> FindResults { get { return SearchResults.SortedBucketX(WholeWordBucketIndex).Concat(SearchResults.SortedBucketX(WholeWordExamplesBucketIndex)); } }

		/// <summary>
		/// The writing system we currently want to get domain names etc in.
		/// </summary>
		private int CurrentWs { get; set; }

		/// <summary>
		/// The search strings corresponding to that CurrentWs.
		/// </summary>
		private ISet<string> CurrentSearchKeys { get; set; }

		internal override void PutDomainInDesiredBucket(ICmSemanticDomain domain)
		{
			foreach (var kvp in m_searchKeys)
			{
				CurrentWs = kvp.Key;
				CurrentSearchKeys = kvp.Value;
				base.PutDomainInDesiredBucket(domain);
			}
		}
		/// <summary>
		/// Looks through a list of words and searches with multiple keys
		/// to find the best matching result between keys and words.
		/// </summary>
		/// <param name="wordsToLookIn"> </param>
		/// <returns></returns>
		protected override MatchingResult DoesInputMatchWord(IEnumerable<string> wordsToLookIn)
		{
			var bestMatch = MatchingResult.NoMatch;
			foreach (var key in CurrentSearchKeys)
			{
				var result = LookForHitInWordCollection(wordsToLookIn, key);
				switch (result)
				{
					case MatchingResult.NoMatch:
						break;
					case MatchingResult.WholeWordMatch:
						return result;
					case MatchingResult.StartOfWordMatch:
						bestMatch = result;
						break;
				}
			}
			return bestMatch;
		}

		/// <summary>
		/// Get the text from a Semantic Domain's Name field.
		/// This version uses AnalysisDefault ws which may not always have a value
		/// (i.e. be prepared for a null return value).
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
		protected override string GetDomainNameString(ICmSemanticDomain domain)
		{
			return domain.Name.get_String(CurrentWs).Text;
		}

		/// <summary>
		/// For one Semantic Domain question, return the text of the Example Words field.
		/// This subclass version uses AnalysisDefault, which could have a null value
		/// (i.e. be prepared to handle a null return value).
		/// </summary>
		/// <param name="questionObject"> </param>
		/// <returns>A string of comma-deliminated (usually) words</returns>
		protected override string GetExampleWordTextFromDomainQuestion(ICmDomainQ questionObject)
		{
			return questionObject.ExampleWords.get_String(CurrentWs).Text;
		}
	}
}
