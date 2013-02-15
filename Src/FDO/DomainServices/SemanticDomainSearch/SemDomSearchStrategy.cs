using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.FDO.DomainServices.SemanticDomainSearch
{
	/// <summary>
	/// An abstract class to be instantiated by a class that knows how to sort
	/// semantic domains into buckets.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Cache is a reference and will be disposed in parent class.")]
	public abstract class SemDomSearchStrategy
	{
		#region Member Variables

		private readonly CultureInfo m_appropriateCulture;

		#endregion

		/// <summary>
		/// Cache for retrieving wss, etc.
		/// </summary>
		protected FdoCache Cache { get; private set; }

		/// <summary>
		/// Search Results can be read from here.
		/// </summary>
		protected SemDomSearchResults SearchResults { get; set; }

		/// <summary>
		/// Constructor for new search strategy.
		/// </summary>
		/// <param name="cache"></param>
		protected SemDomSearchStrategy(FdoCache cache)
		{
			Cache = cache;
			m_appropriateCulture = GetAppropriateCultureInfo();
			// Starting after whitespace or the beginning of a line, finds runs of either letters or
			// singlequote or hyphen followed by either whitespace or the end of a line.
			// The first look-ahead, (?=\w), means that single quote and hyphen are only included
			// if there is an adjacent following letter, that is, if they occur word-medially.
			WordParsingRegex = new Regex(@"(?<=(^|\s))(\w|['-](?=\w))*(?=(\s|,|$))");

			SetupSearchResults();
		}

		/// <summary>
		/// Subclasses can override to set the search results object to have
		/// the correct number of buckets for a different strategy.
		/// Baseclass defaults to two buckets.
		/// </summary>
		protected virtual void SetupSearchResults()
		{
			SearchResults = new SemDomSearchResults(4);
		}

		///
		/// There are four buckets for matches:
		/// bucket 0   This is the bucket for whole word matches found in the DomainNameString
		/// bucket 1   This is the bucket for whole word matches found in the ExampleWordsString
		/// bucket 2   This is the bucket for partial word matches found in the DomainNameString
		/// bucket 3   This is the bucket for partial word matches found in the ExampleWordsString
		/// Note: The order of these buckets is very important due to the behavior found in the method AddResultToBucketX
		///
		/// <summary>
		/// Subclass can override to return a different index into the search results bucket list
		/// that specifies where to put whole word matches found in the DomainNameString.
		/// </summary>
		protected virtual int WholeWordBucketIndex { get { return 0; } }

		/// <summary>
		/// Subclass can override to return a different index into the search results bucket list
		/// that specifies where to put whole word matches found in the ExampleWordsString.
		/// </summary>
		protected virtual int WholeWordExamplesBucketIndex { get { return 1; } }

		/// <summary>
		/// Subclass can override to return a different relative index into the search results bucket list
		/// that specifies where to put partial word matches found in the DomainNameString or ExampleWordsString.
		/// </summary>
		protected virtual int PartialMatchRelativeIndex { get { return 2; } }

		/// <summary>
		/// Subclasses can override this method to change how Semantic Domains are compared
		/// with any search key(s) and the priority types of matches for putting the
		/// domain into an appropriate search results bucket (if any).
		/// N.B. Strategy subclasses should take care not to add a domain to a bucket
		/// if it is already in a higher priority bucket.
		/// </summary>
		/// <param name="domain"></param>
		internal virtual void PutDomainInDesiredBucket(ICmSemanticDomain domain)
		{
			// Check for whole word and partial matches in Name first
			if (!CollectPossibleMatchIn(GetDomainNameString(domain), domain, WholeWordBucketIndex))
			{
				// If no whole word match in Name, look in Example Words too.
				CollectPossibleMatchIn(GetExampleWordsString(domain.QuestionsOS), domain, WholeWordExamplesBucketIndex);
			}
		}

		/// <summary>
		/// Take a string to look in (from a domain's Name or ExampleWords) and try to find
		/// a match for some (strategy-defined) search key(s).
		/// </summary>
		/// <param name="searchIn"></param>
		/// <param name="domain"></param>
		/// <param name="wholeWordBucket"></param>
		/// <returns></returns>
		protected bool CollectPossibleMatchIn(string searchIn, ICmSemanticDomain domain, int wholeWordBucket)
		{
			if (string.IsNullOrEmpty(searchIn))
				return false;
			var wordsToLookIn = ParseStringIntoWords(searchIn);
			switch (DoesInputMatchWord(wordsToLookIn))
			{
				case MatchingResult.WholeWordMatch:
					SearchResults.AddResultToBucketX(wholeWordBucket, domain);
					return true;
				case MatchingResult.StartOfWordMatch:
					SearchResults.AddResultToBucketX(wholeWordBucket + PartialMatchRelativeIndex, domain);
					return true;
				case MatchingResult.NoMatch:
					// do nothing
					break;
				default:
					throw new ApplicationException("Unknown MatchingResult");
			}
			return false;
		}

		private IEnumerable<string> ParseStringIntoWords(string searchIn)
		{
			return (from Match match in WordParsingRegex.Matches(searchIn)
				select match.Value).ToArray();
		}

		/// <summary>
		/// Each strategy defines how to determine whether a string has a match or not.
		/// </summary>
		/// <param name="wordsToLookIn"> </param>
		/// <returns></returns>
		protected abstract MatchingResult DoesInputMatchWord(IEnumerable<string> wordsToLookIn);

		/// <summary>
		/// Tries to get a predefined culture for the Default Analysis Ws, failing that
		/// we go with the InvariantCulture. Used for finding matches.
		/// </summary>
		protected CultureInfo AppropriateCulture { get { return m_appropriateCulture; } }

		/// <summary>
		/// Starting after whitespace or the beginning of a line, finds runs of either letters or
		/// singlequote or hyphen followed by either whitespace or the end of a line.
		/// The first look-ahead, (?=\w), means that single quote and hyphen are only included
		/// if there is an adjacent following letter, that is, if they occur word-medially.
		/// </summary>
		protected Regex WordParsingRegex { get; private set; }

		/// <summary>
		/// For one Semantic Domain question, return the text of the Example Words field.
		/// This baseclass version uses BestAnalysis, which always has a value, but a subclass
		/// might use a more specific writing system which could return a null value
		/// (i.e. be prepared to handle a null return value).
		/// </summary>
		/// <param name="questionObject"> </param>
		/// <returns>A string of comma-deliminated (usually) words</returns>
		protected virtual string GetExampleWordTextFromDomainQuestion(ICmDomainQ questionObject)
		{
			return questionObject.ExampleWords.BestAnalysisAlternative.Text;
		}

		/// <summary>
		/// Get the text from a Semantic Domain's Name field.
		/// This version uses BestAnalysisAlternative which should always have a value,
		/// but a subclass might use a more specific writing system which may not have a value
		/// (i.e. be prepared for a null return value).
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
		protected virtual string GetDomainNameString(ICmSemanticDomain domain)
		{
			return domain.Name.BestAnalysisAlternative.Text;
		}

		/// <summary>
		/// Takes a list of semantic domain question objects and fishes out the example words
		/// and puts them in a space-delimited string for easier searching.
		/// This version uses BestAnalysisAlternative.
		/// </summary>
		/// <param name="questions"></param>
		/// <returns></returns>
		protected string GetExampleWordsString(IEnumerable<ICmDomainQ> questions)
		{
			return (from domainQuest in questions
					where domainQuest.ExampleWords != null
					select GetExampleWordTextFromDomainQuestion(domainQuest) into text
					where !string.IsNullOrEmpty(text)
					select text).Aggregate(
						  string.Empty, (current, text) => current + (" " + text));
		}

		/// <summary>
		/// Search through a list of words (results of Regex match) for a search key.
		/// Return a MatchingResult to inform caller of type of match, if any.
		/// </summary>
		/// <param name="wordsToLookIn"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		protected MatchingResult LookForHitInWordCollection(IEnumerable<string> wordsToLookIn, string key)
		{
			foreach(var word in wordsToLookIn)
			{
				if (string.Compare(word, key, true, AppropriateCulture) == 0)
					return MatchingResult.WholeWordMatch;
				if (word.StartsWith(key, true, AppropriateCulture))
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

		/// <summary>
		/// Search Results object for accumulating SemDomSearchEngine search results
		/// from a particular SemDomSearchStrategy.
		/// </summary>
		protected class SemDomSearchResults
		{
			// A list of buckets to hold search results
			private readonly HashSet<ICmSemanticDomain>[] m_bucketList;

			/// <summary>
			/// Create new (empty) buckets for collecting results
			/// The semantics behind each bucket are determined by the SearchStrategy using the results object
			/// </summary>
			public SemDomSearchResults(int numberOfNeededBuckets)
			{
				m_bucketList = new HashSet<ICmSemanticDomain>[numberOfNeededBuckets];
				for (var i = 0; i < numberOfNeededBuckets; i++)
					m_bucketList[i] = new HashSet<ICmSemanticDomain>();
			}

			/// <summary>
			/// The buckets are ordered in priority so adding a result to one bucket
			/// should remove that result from all lower (higher index) ones.
			/// N.B. Strategy subclasses should take care not to add a domain to a bucket
			/// if it is already in a higher priority bucket.
			/// </summary>
			/// <param name="bucketIndex"></param>
			/// <param name="domain"></param>
			public void AddResultToBucketX(int bucketIndex, ICmSemanticDomain domain)
			{
				m_bucketList[bucketIndex].Add(domain);
				for (var i = bucketIndex + 1; i < m_bucketList.Length; i++)
				{
					m_bucketList[i].Remove(domain);
				}
			}

			/// <summary>
			/// Returns the contents of a Bucket sorted by hierarchical code (e.g. 1.1.8)
			/// </summary>
			public IEnumerable<ICmSemanticDomain> SortedBucketX(int bucketIndex)
			{
				return m_bucketList[bucketIndex].OrderBy(dom => dom.Abbreviation.BestAnalysisAlternative.Text);
			}
		}

		/// <summary>
		/// Defines the different possible results when comparing a search key with a string.
		/// </summary>
		protected enum MatchingResult
		{
			/// <summary>
			/// Search key matches an entire word.
			/// </summary>
			WholeWordMatch,
			/// <summary>
			/// Search key matches part of a word (starting from the beginning of the word).
			/// </summary>
			StartOfWordMatch,
			/// <summary>
			/// Search key doesn't even have a partial match in the relevant string.
			/// </summary>
			NoMatch
		}

	}
}
