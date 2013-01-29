using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.SemanticDomainSearch
{

	/// <summary>
	/// Search strategy for when the user types a string and we need to search the Semantic Domain list
	/// for matches of that string.
	/// </summary>
	public class WholeAndPartialNameMatchSearchStrategy : SemDomSearchStrategy
	{
		#region Member Variables

		private readonly bool m_fnumeric;
		private readonly string m_searchString;

		#endregion

		/// <summary>
		/// Semantic Domain Search Strategy for finding:
		///   1) bucket 0 will contain: Partial or complete matches, left to right of Abbreviation (hierarchical number)
		///				Note when matches are found for this then the other buckets should be empty because this is a number vs alphabet
		///
		///	  2a) bucket 0  This is the bucket for an EXACT match found for the DomainNameString
		///   2a) bucket 1   This is the bucket for whole word matches found in the DomainNameString
		///   2b) bucket 2   This is the bucket for whole word matches found in the ExampleWordsString
		///   2c) bucket 3   This is the bucket for partial(StartsWith) word matches found in the DomainNameString
		///   2d) bucket 4   This is the bucket for partial(StartsWith) word matches found in the ExampleWordsString
		///
		/// Note: The order of these buckets is very important due to the behavior found in the method AddResultToBucketX
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="searchKey">This strategy expects to only have one search key string.</param>
		public WholeAndPartialNameMatchSearchStrategy(FdoCache cache, string searchKey)
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
			SearchResults = new SemDomSearchResults(5);
		}

		/// <summary>
		/// Subclass can override to return a different index into the search results bucket list
		/// that specifies where to put whole word matches.
		/// This strategy puts a priority on exact DomainNameString matches and puts them in bucket[0],
		/// so WholeWord matches found somewhere else in the DomainNameString after that go into bucket[1].
		/// </summary>
		protected override int WholeWordBucketIndex { get { return 1; } }

		/// <summary>
		/// This specifies where to put whole word matches found in the ExampleWordsString.
		/// </summary>
		protected override int WholeWordExamplesBucketIndex { get { return 2; } }

		/// <summary>
		/// The results of this strategy are 3 groups of matches concatenated together, each group sorted
		/// by hierarchical number within itself
		/// </summary>
		public IEnumerable<ICmSemanticDomain> FindResults
		{
			get { return SearchResults.SortedBucketX(0).Concat(SearchResults.SortedBucketX(1).Concat(SearchResults.SortedBucketX(2).Concat(SearchResults.SortedBucketX(3).Concat(SearchResults.SortedBucketX(4))))); }
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

			// Check to see if this domain is an exact match
			// Boolean param in Compare() ignores case
			// N.B.: WritingSystem check should be the same as what's displayed in the UI!
			if (String.Compare(domain.Name.BestAnalysisAlternative.Text, m_searchString, true, AppropriateCulture) == 0)
			{
				SearchResults.AddResultToBucketX(0, domain);
				return;
			}

			base.PutDomainInDesiredBucket(domain);
		}

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
