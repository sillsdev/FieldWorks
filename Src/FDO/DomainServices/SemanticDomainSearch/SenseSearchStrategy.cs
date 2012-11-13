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
		private readonly string[] m_searchKeys;
		private readonly ILexSense m_sense;

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

		private const int MAX_WORDS_IN_USEABLE_DEFINITION = 2;

		private string[] GetSearchKeysFromSense()
		{
			var results = new HashSet<string>();
			AddKeysFrom(m_sense.Gloss.AnalysisDefaultWritingSystem.Text, results);
			var definition = m_sense.Definition.AnalysisDefaultWritingSystem.Text;
			if (definition != null && definition.Split(' ').Length <= MAX_WORDS_IN_USEABLE_DEFINITION)
				AddKeysFrom(definition, results);
			var reversalEntries = m_sense.ReversalEntriesRC;
			foreach (IReversalIndexEntry reversalIndexEntry in reversalEntries)
				AddKeysFrom(reversalIndexEntry.ReversalForm.AnalysisDefaultWritingSystem.Text, results);

			// Enhance (GJM): Eventually add Translation Equivalent when implemented?
			return results.ToArray();
		}

		/// <summary>
		/// Add words parsed from the wordSource string to the keyCollector set.
		/// Handles null wordSource string by returning immediately.
		/// </summary>
		/// <param name="wordSource"></param>
		/// <param name="keyCollector"></param>
		private void AddKeysFrom(string wordSource, ISet<string> keyCollector)
		{
			if (wordSource == null)
				return;
			foreach (Match word in WordParsingRegex.Matches(wordSource))
				keyCollector.Add(word.Value);
		}

		/// <summary>
		/// A list of semantic domains where one of the search keys matched the first part of
		/// one of the words in the Name or Example Words fields.
		/// </summary>
		public IEnumerable<ICmSemanticDomain> PartialMatches { get { return SearchResults.SortedBucketX(1); } }

		/// <summary>
		/// A list of semantic domains where one of the search keys completely matched
		/// one of the words in the Name or Example Words fields.
		/// </summary>
		public IEnumerable<ICmSemanticDomain> FindResults { get { return SearchResults.SortedBucketX(0); } }

		/// <summary>
		/// Looks through a list of words and searches with multiple keys
		/// to find the best matching result between keys and words.
		/// </summary>
		/// <param name="wordsToLookIn"> </param>
		/// <returns></returns>
		protected override MatchingResult DoesInputMatchWord(IEnumerable<string> wordsToLookIn)
		{
			var bestMatch = MatchingResult.NoMatch;
			foreach (var key in m_searchKeys)
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
			return domain.Name.AnalysisDefaultWritingSystem.Text;
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
			return questionObject.ExampleWords.AnalysisDefaultWritingSystem.Text;
		}
	}
}
