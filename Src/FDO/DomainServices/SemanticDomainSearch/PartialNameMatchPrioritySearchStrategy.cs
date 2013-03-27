using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.SemanticDomainSearch
{

	/// <summary>
	/// Search strategy for when the user types a string and we need to search the Semantic Domain list
	/// for matches of that string.
	/// </summary>
	public class PartialNameMatchPrioritySearchStrategy : SemDomSearchStrategy
	{
		#region Member Variables

		private readonly bool m_fnumeric;
		private readonly string m_searchString;

		#endregion

		/// <summary>
		/// Semantic Domain Search Strategy for finding:
		///   1) Partial or complete matches, left to right of Abbreviation (hierarchical number)
		///   2) Partial or complete matches, from the beginning of Name
		///   3) Whole word matches of Name or ExampleWords
		///   4) Partial matches (from the beginning of words) elsewhere in Name and ExampleWords
		/// N.B. 1 and (2,3,4) are mutually exclusive depending on the first character of the search string.
		///   1 and 2 go into first bucket
		///   3 goes into second bucket
		///   4 goes into third bucket
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="searchKey">This strategy expects to only have one search key string.</param>
		public PartialNameMatchPrioritySearchStrategy(FdoCache cache, string searchKey)
			: base(cache)
		{
			m_fnumeric = Char.IsDigit(searchKey[0]);
			m_searchString = searchKey;
		}

		/// <summary>
		/// Subclasses can override to set the search results object to have
		/// the correct number of buckets for a different strategy.
		/// This strategy needs three buckets.
		/// </summary>
		protected override void SetupSearchResults()
		{
			SearchResults = new SemDomSearchResults(3);
		}

		/// <summary>
		/// The results of this strategy are 3 groups of matches concatenated together, each group sorted
		/// by hierarchical number within itself
		/// </summary>
		public IEnumerable<ICmSemanticDomain> FindResults
		{
			get { return SearchResults.SortedBucketX(0).Concat(SearchResults.SortedBucketX(1).Concat(SearchResults.SortedBucketX(2))); }
		}

		/// <summary>
		/// This implementation checks to see of the search key starts with a digit first.
		/// If so, it looks in the Abbreviation field for a search key match in the hierarchical number
		/// (e.g. "8.3.3"; these would go in the first bucket).
		/// Otherwise it checks next for matches where the key corresponds to the beginning
		/// of the Name field (first bucket). Failing that, it checks for whole word (second bucket) or
		/// partial matches (third bucket) between the search key and the contents of a Semantic Domain's
		/// Name and ExampleWords fields.
		/// N.B. Strategy subclasses should take care not to add a domain to a bucket
		/// if it is already in a higher priority bucket.
		/// </summary>
		internal override void PutDomainInDesiredBucket(ICmSemanticDomain domain)
		{
			// Look in Name and Example Words for matches
			if (m_fnumeric)
			{
				SearchInAbbreviationForMatches(domain);
				return;
			}

			// Check to see if this domain is a first bucket match
			// Boolean param in StartsWith() ignores case
			// N.B.: WritingSystem check should be the same as what's displayed in the UI!
			if (domain.Name.BestAnalysisAlternative.Text.StartsWith(m_searchString, true, AppropriateCulture))
			{
				SearchResults.AddResultToBucketX(0, domain);
				return;
			}
			base.PutDomainInDesiredBucket(domain);
		}

		/// <summary>
		/// Subclass can override to return a different index into the search results bucket list
		/// that specifies where to put whole word matches.
		/// This strategy puts a priority on partial Name matches and puts them in bucket[0],
		/// so WholeWord matches after that go into bucket[1].
		/// </summary>
		protected override int WholeWordBucketIndex { get { return 1; } }

		private void SearchInAbbreviationForMatches(ICmSemanticDomain domain)
		{
			if (domain.Abbreviation.BestAnalysisAlternative.Text.StartsWith(m_searchString))
				SearchResults.AddResultToBucketX(0, domain);
		}

		/// <summary>
		/// This strategy only uses one search string (what the user typed),
		/// so this version is pretty simple.
		/// </summary>
		/// <param name="wordsToLookIn"> </param>
		/// <returns></returns>
		protected override MatchingResult DoesInputMatchWord(IEnumerable<string> wordsToLookIn)
		{
			return LookForHitInWordCollection(wordsToLookIn, m_searchString);
		}
	}
}
