using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.SemanticDomainSearch
{
	/// <summary>
	/// An abstract class to be instantiated by a class that knows how to sort
	/// semantic domains into buckets.
	/// </summary>
	public abstract class SemDomSearchStrategy
	{
		/// <summary>
		/// Cache for retrieving wss, etc.
		/// </summary>
		protected FdoCache Cache { get; private set; }

		/// <summary>
		/// Search Results can be read from here.
		/// </summary>
		internal SemDomSearchResults SearchResults { get; private set; }

		/// <summary>
		/// Constructor for new search strategy.
		/// </summary>
		/// <param name="cache"></param>
		protected SemDomSearchStrategy(FdoCache cache)
		{
			Cache = cache;
			SearchResults = new SemDomSearchResults();
		}

		internal abstract void DetermineBucket(ICmSemanticDomain domain);

		/// <summary>
		/// Search Results Object (sro) for accumulating SemDomSearchEngine search results
		/// from a particular SemDomSearchStrategy.
		/// </summary>
		internal class SemDomSearchResults
		{
			/// <summary>
			/// A results bucket for sorting domains into. The semantics of what a bucket
			/// means is determined by the strategy being employed.
			/// </summary>
			internal HashSet<ICmSemanticDomain> Bucket1 { get; set; }
			/// <summary>
			/// A results bucket for sorting domains into. The semantics of what a bucket
			/// means is determined by the strategy being employed.
			/// </summary>
			internal HashSet<ICmSemanticDomain> Bucket2 { get; set; }
			/// <summary>
			/// A results bucket for sorting domains into. The semantics of what a bucket
			/// means is determined by the strategy being employed.
			/// </summary>
			internal HashSet<ICmSemanticDomain> Bucket3 { get; set; }

			/// <summary>
			/// Create new (empty) buckets for collecting results
			/// The semantics behind each bucket are determined by the SearchStrategy using the sro
			/// </summary>
			public SemDomSearchResults()
			{
				Bucket1 = new HashSet<ICmSemanticDomain>();
				Bucket2 = new HashSet<ICmSemanticDomain>();
				Bucket3 = new HashSet<ICmSemanticDomain>();
			}

			/// <summary>
			/// Returns the contents of Bucket1 sorted by hierarchical code (e.g. 1.1.8)
			/// </summary>
			internal IEnumerable<ICmSemanticDomain> SortedBucket1
			{
				get { return SortBucket(Bucket1); }
			}

			/// <summary>
			/// Returns the contents of Bucket2 sorted by hierarchical code (e.g. 1.1.8)
			/// </summary>
			internal IEnumerable<ICmSemanticDomain> SortedBucket2
			{
				get { return SortBucket(Bucket2); }
			}

			/// <summary>
			/// Returns the contents of Bucket3 sorted by hierarchical code (e.g. 1.1.8)
			/// </summary>
			internal IEnumerable<ICmSemanticDomain> SortedBucket3
			{
				get { return SortBucket(Bucket3); }
			}

			private IEnumerable<ICmSemanticDomain> SortBucket(IEnumerable<ICmSemanticDomain> bucket)
			{
				return bucket.OrderBy(dom => dom.Abbreviation.BestAnalysisAlternative.Text);
			}
		}
	}
}
