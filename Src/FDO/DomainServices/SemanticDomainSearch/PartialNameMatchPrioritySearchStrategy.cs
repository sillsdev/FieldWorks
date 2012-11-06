using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.FDO.DomainServices.SemanticDomainSearch
{

	/// <summary>
	/// Search strategy for when the user types a string and we need to search the Semantic Domain list
	/// for matches of that string.
	/// </summary>
	public class PartialNameMatchPrioritySearchStrategy : SemDomSearchStrategy
	{
		#region Member Variables

		private readonly Regex m_regexPattern;
		private readonly CultureInfo m_appropriateCulture;
		private readonly bool m_fnumeric;
		private readonly string m_searchString;

		#endregion

		// starting after whitespace or the beginning of a line,
		// find a run of either letters or singlequote or hyphen
		// which is followed by either whitespace or the end of a line.
		// (the first look-ahead, (?=\w), means that single quote and hyphen are only included
		// if there is an adjacent following letter, that is, if word-medial)
		private const string RegexString = @"(?<=(^|\s))(\w|['-](?=\w))*(?=(\s|,|$))";

		/// <summary>
		/// Semantic Domain Search Strategy for finding:
		///   1) Partial or complete matches, left to right of Abbreviation (hierarchical number)
		///   2) Partial or complete matches, from the beginning of Name
		///   3) Whole word matches of Name or ExampleWords
		///   4) Partial matches (from the beginning of words) elsewhere in Name and ExampleWords
		/// N.B. 1 and (2,3,4) are mutually exclusive depending on the first character of the search string.
		///   1 and 2 go into bucket1
		///   3 goes into bucket2
		///   4 goes into bucket3
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="searchKey">This strategy expects to only have one search key string.</param>
		public PartialNameMatchPrioritySearchStrategy(FdoCache cache, string searchKey)
			: base(cache)
		{
			m_appropriateCulture = GetAppropriateCultureInfo();
			m_regexPattern = new Regex(RegexString, RegexOptions.IgnoreCase);
			m_fnumeric = Char.IsDigit(searchKey[0]);
			m_searchString = searchKey;
		}

		/// <summary>
		/// The results of this strategy are 3 groups of matches concatenated together, each group sorted
		/// by hierarchical number within itself
		/// </summary>
		public IEnumerable<ICmSemanticDomain> FindResults
		{
			get { return SearchResults.SortedBucket1.Concat(SearchResults.SortedBucket2.Concat(SearchResults.SortedBucket3)); }
		}

		internal override void DetermineBucket(ICmSemanticDomain domain)
		{
			// Look in Name and Example Words for matches
			if (m_fnumeric)
			{
				SearchInAbbreviationForMatches(domain);
				return;
			}

			// Check to see if this domain is a bucket1 match
			// Boolean param in StartsWith() ignores case
			// N.B.: WritingSystem check should be the same as what's displayed in the UI!
			if (domain.Name.BestAnalysisAlternative.Text.StartsWith(m_searchString, true, m_appropriateCulture))
			{
				SearchResults.Bucket1.Add(domain);
				return;
			}
			CollectPossibleGroup2Or3Match(domain);
		}

		private void SearchInAbbreviationForMatches(ICmSemanticDomain domain)
		{
			if (domain.Abbreviation.BestAnalysisAlternative.Text.StartsWith(m_searchString))
				SearchResults.Bucket1.Add(domain);
		}

		private void CollectPossibleGroup2Or3Match(ICmSemanticDomain domain)
		{
			// Check for group2 and group3 matches in Name first
			if (!LookForMatchIn(domain.Name.BestAnalysisAlternative.Text,
				domain))
			{
				LookForMatchIn(GetExampleWordsString(domain.QuestionsOS),
					domain);
			}
		}

		private bool LookForMatchIn(string searchIn, ICmSemanticDomain domain)
		{
			switch (DoesInputMatchWordInString(searchIn))
			{
				case MatchingResult.WholeWordMatch:
					SearchResults.Bucket2.Add(domain);
					SearchResults.Bucket3.Remove(domain);
					return true;
				case MatchingResult.StartOfWordMatch:
					SearchResults.Bucket3.Add(domain);
					break;
				case MatchingResult.NoMatch:
					// do nothing
					break;
				default:
					throw new ApplicationException("Unknown MatchingResult");
			}
			return false;
		}

		private string GetExampleWordsString(IEnumerable<ICmDomainQ> questions)
		{
			return (from domainQuest in questions
					where domainQuest.ExampleWords != null
					select domainQuest.ExampleWords.BestAnalysisAlternative.Text into text
					where text.Length > 0
					select text).Aggregate(
						  string.Empty, (current, text) => current + (" " + text));
		}

		private MatchingResult DoesInputMatchWordInString(string lookInMe)
		{
			// starting after whitespace or the beginning of a line,
			// find a run of either letters or singlequote or hyphen
			// which is followed by either whitespace or the end of a line.
			var matches = m_regexPattern.Matches(lookInMe);
			for (var i = 0; i < matches.Count; i++)
			{
				if (string.Compare(matches[i].Value, m_searchString, true, m_appropriateCulture) == 0)
					return MatchingResult.WholeWordMatch;
				if (matches[i].Value.StartsWith(m_searchString, true, m_appropriateCulture))
					return MatchingResult.StartOfWordMatch;
			}
			return MatchingResult.NoMatch;
		}

		private CultureInfo GetAppropriateCultureInfo()
		{
			CultureInfo ci;
			try
			{
				ci = new CultureInfo(Cache.WritingSystemFactory.GetStrFromWs(Cache.DefaultAnalWs));
			}
			catch (Exception)
			{
				ci = CultureInfo.InvariantCulture;
			}
			return ci;
		}

		private enum MatchingResult
		{
			WholeWordMatch,
			StartOfWordMatch,
			NoMatch
		}
	}
}
