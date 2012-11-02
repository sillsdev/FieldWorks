using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	internal partial class CmSemanticDomainRepository
	{
		#region Member Variables

		private Regex m_regexPattern;
		private CultureInfo m_appropriateCulture;

		#endregion

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
		///
		/// N.B.: This method looks for matches in the UserWs.
		/// As a result, if the user interface is in a language that does not yet have a translation
		/// of the semantic domain list, this method will return no values.
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

			if (Char.IsDigit(searchString1[0]))
				return SearchInAbbreviationForMatches(searchString1);

			return SearchInNameAndWordsForMatches(searchString1);
		}

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
		/// N.B.: This method looks for matches in the UserWs.
		/// As a result, if the user interface is in a language that does not yet have a translation
		/// of the semantic domain list, this method will return no values.
		/// </summary>
		/// <param name="searchString"></param>
		/// <param name="group3">out var that returns group 3 matches</param>
		/// <returns>groups 1 and 2 above</returns>
		public IEnumerable<ICmSemanticDomain> FindMoreDomainsThatMatch(string searchString, out IEnumerable<ICmSemanticDomain> group3)
		{
			group3 = new List<ICmSemanticDomain>(); // out var needs setting before we return
			if (string.IsNullOrEmpty(searchString))
				return new List<ICmSemanticDomain>();

			var searchString1 = searchString.Trim();
			if (string.IsNullOrEmpty(searchString1))
				return new List<ICmSemanticDomain>();

			if (Char.IsDigit(searchString1[0]))
				return SearchInAbbreviationForMatches(searchString1);

			return SearchInNameAndWordsForMatches(searchString1, out group3);
		}

		private IEnumerable<ICmSemanticDomain> SearchInNameAndWordsForMatches(string searchString)
		{
			IEnumerable<ICmSemanticDomain> dummy;

			return SearchInNameAndWordsForMatches(searchString, out dummy);
		}

		private IEnumerable<ICmSemanticDomain> SearchInNameAndWordsForMatches(string searchString, out IEnumerable<ICmSemanticDomain> group3)
		{
			m_appropriateCulture = GetAppropriateCultureInfo();
			// Look in Name and Example Words for matches
			var group1 = new HashSet<ICmSemanticDomain>(); // is bucket for search matches beginning of Name field
			var group2 = new HashSet<ICmSemanticDomain>(); // is bucket for whole word matches in Name or Example Words
			// group3 (param) is bucket for start-of-word matches in Name or Example Words
			var internalGroup3 = new HashSet<ICmSemanticDomain>(); // for collecting unsorted group3 results
			if (m_regexPattern == null)
				m_regexPattern = new Regex(RegexString, RegexOptions.IgnoreCase);

			foreach (var domain in AllInstances())
			{
				// Check to see if this domain is a group1 match, boolean 'true' ignores case
				if (domain.Name.UserDefaultWritingSystem.Text.StartsWith(searchString, true, m_appropriateCulture))
				{
					group1.Add(domain);
					continue;
				}
				CollectPossibleGroup2Or3Match(searchString, internalGroup3, domain, group2);
			}
			// Sort results and return
			group3 = internalGroup3.OrderBy(dom3 => dom3.Abbreviation.UserDefaultWritingSystem.Text);
			return group1.OrderBy(dom => dom.Abbreviation.UserDefaultWritingSystem.Text).Concat(
				group2.OrderBy(dom2 => dom2.Abbreviation.UserDefaultWritingSystem.Text));
		}

		private void CollectPossibleGroup2Or3Match(string searchString, HashSet<ICmSemanticDomain> internalGroup3,
							ICmSemanticDomain domain, HashSet<ICmSemanticDomain> group2)
		{
			// Check for group2 and group3 matches in Name first
			if (!LookForMatchIn(searchString, domain.Name.UserDefaultWritingSystem.Text,
				domain, group2, internalGroup3))
			{
				LookForMatchIn(searchString, GetExampleWordsString(domain.QuestionsOS),
					domain, group2, internalGroup3);
			}
		}

		private bool LookForMatchIn(string searchString, string searchIn, ICmSemanticDomain domain,
			HashSet<ICmSemanticDomain> group2, HashSet<ICmSemanticDomain> internalGroup3)
		{
			switch (DoesInputMatchWordInString(searchString, searchIn))
			{
				case MatchingResult.WholeWordMatch:
					group2.Add(domain);
					internalGroup3.Remove(domain);
					return true;
				case MatchingResult.StartOfWordMatch:
					internalGroup3.Add(domain);
					break;
				case MatchingResult.NoMatch:
					// do nothing
					break;
				default:
					throw new ApplicationException("Unknown MatchingResult");
			}
			return false;
		}

		private CultureInfo GetAppropriateCultureInfo()
		{
			CultureInfo ci;
			try
			{
				ci = new CultureInfo(Cache.WritingSystemFactory.GetStrFromWs(Cache.DefaultUserWs));
			}
			catch (Exception)
			{
				ci = CultureInfo.InvariantCulture;
			}
			return ci;
		}

		private string GetExampleWordsString(IEnumerable<ICmDomainQ> questions)
		{
			return (from domainQuest in questions where domainQuest.ExampleWords != null
						  select domainQuest.ExampleWords.UserDefaultWritingSystem.Text into text
						  where text.Length > 0 select text).Aggregate(
						  string.Empty, (current, text) => current + (" " + text));
		}

		private IEnumerable<ICmSemanticDomain> SearchInAbbreviationForMatches(string searchString)
		{
			var searchableWs = Cache.DefaultUserWs;
			var matches = new HashSet<ICmSemanticDomain>();
			foreach (var domain in AllInstances())
			{
				if (domain.Abbreviation.UserDefaultWritingSystem.Text.StartsWith(searchString))
					matches.Add(domain);
			}
			return matches.OrderBy(x => x.Abbreviation.UserDefaultWritingSystem.Text);
		}

		// starting after whitespace or the beginning of a line,
		// find a run of either letters or singlequote or hyphen
		// which is followed by either whitespace or the end of a line.
		// (the first look-ahead, (?=\w), means that single quote and hyphen are only included
		// if there is an adjacent following letter, that is, if word-medial)
		private const string RegexString = @"(?<=(^|\s))(\w|['-](?=\w))*(?=(\s|,|$))";

		private MatchingResult DoesInputMatchWordInString(string lookForMe, string lookInMe)
		{
			// starting after whitespace or the beginning of a line,
			// find a run of either letters or singlequote or hyphen
			// which is followed by either whitespace or the end of a line.
			var matches = m_regexPattern.Matches(lookInMe);
			for (var i = 0; i < matches.Count; i++)
			{
				if (string.Compare(matches[i].Value, lookForMe, true, m_appropriateCulture) == 0)
					return MatchingResult.WholeWordMatch;
				if (matches[i].Value.StartsWith(lookForMe, true, m_appropriateCulture))
					return MatchingResult.StartOfWordMatch;
			}
			return MatchingResult.NoMatch;
		}

		private enum MatchingResult
		{
			WholeWordMatch,
			StartOfWordMatch,
			NoMatch
		}

	}
}
