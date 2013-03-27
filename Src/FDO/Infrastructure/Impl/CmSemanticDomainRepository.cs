using System.Collections.Generic;
using SIL.FieldWorks.FDO.DomainServices.SemanticDomainSearch;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	internal partial class CmSemanticDomainRepository
	{
		/// <summary>
		/// Finds all the semantic domains that contain 'searchString' in their text fields.
		/// Semantic Domains typically have:
		///   Abbreviation (a hierarchical number, e.g. "8.3.3")
		///   Name (e.g. "Light")
		///   Description (e.g. "Use this domain for words related to light.")
		///   OCM codes and Louw and Nida codes
		///   Questions (e.g. "(1) What words refer to light?")
		///   Example Words (e.g. "light, sunshine, gleam (n), glare (n), glow (n), radiance,")
		/// Search strings beginning with numbers will search Abbreviation only and only match at the beginning.
		///   (so searching for "3.3" won't return "8.3.3")
		/// Search strings beginning with alphabetic chars will search Name and Example Words.
		/// For alphabetic searches, hits will be returned in the following order:
		///   1) Name begins with search string
		///   2) Name or Example Words contain words (bounded by whitespace) that match the search string
		///   3) Name or Example Words contain words that begin with the search string
		///
		/// N.B.: This method looks for matches in the BestAnalysisAlternative writing system.
		/// This ought to match what is displayed in the UI, so if the UI doesn't use
		/// BestAnalysisAlternative one of them needs to be changed.
		/// </summary>
		/// <param name="searchString"></param>
		/// <returns></returns>
		public IEnumerable<ICmSemanticDomain> FindDomainsThatMatch(string searchString)
		{
			if (string.IsNullOrEmpty(searchString))
				return new List<ICmSemanticDomain>();

			var searchString1 = searchString.Trim();
			if (string.IsNullOrEmpty(searchString1))
				return new List<ICmSemanticDomain>();

			var strategy = new PartialNameMatchPrioritySearchStrategy(Cache, searchString1);
			var engine = new SemDomSearchEngine(Cache);
			engine.WalkDomains(strategy);

			return strategy.FindResults;
		}

		/// <summary>
		/// Takes the gloss, a short definition (if only one or two words), and reversal from a LexSense
		/// and uses those words as search keys to find Semantic Domains that have one of those words in
		/// their Name or ExampleWords fields.
		///
		/// N.B.: This method looks for matches in the AnalysisDefaultWritingSystem.
		/// This ought to help ensure that we are searching in the Semantic Domain fields in the same
		/// writing system as the one the search keys in the Sense came from.
		/// </summary>
		/// <param name="sense"></param>
		/// <returns></returns>
		public IEnumerable<ICmSemanticDomain> FindDomainsThatMatchWordsIn(ILexSense sense)
		{
			IEnumerable<ICmSemanticDomain> dummy;
			return FindDomainsThatMatchWordsIn(sense, out dummy);
		}

		/// <summary>
		/// Takes the gloss, a short definition (if only one or two words), and reversal from a LexSense
		/// and uses those words as search keys to find Semantic Domains that have one of those words in
		/// their Name or Example Words fields.
		/// In addition, this method returns additional partial matches in the 'out' parameter where one
		/// of the search keys matches the beginning of one of the words in the domain's Name or Example
		/// Words fields.
		///
		/// N.B.: This method looks for matches in the AnalysisDefaultWritingSystem.
		/// This ought to help ensure that we are searching in the Semantic Domain fields in the same
		/// writing system as the one the search keys in the Sense came from.
		/// </summary>
		/// <param name="sense">A LexSense</param>
		/// <param name="partialMatches">extra partial matches</param>
		/// <returns></returns>
		public IEnumerable<ICmSemanticDomain> FindDomainsThatMatchWordsIn(ILexSense sense,
			out IEnumerable<ICmSemanticDomain> partialMatches)
		{
			var strategy = new SenseSearchStrategy(Cache, sense);
			new SemDomSearchEngine(Cache).WalkDomains(strategy);

			partialMatches = strategy.PartialMatches;

			return strategy.FindResults;
		}
	}
}
