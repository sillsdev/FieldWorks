// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.FDO.DomainServices.SemanticDomainSearch
{
	/// <summary>
	/// A cache of Semantic Domains by word string and writing system to facilitate
	/// bulk editing of LexSense.SemanticDomains collections by computer-generated
	/// suggestions.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "m_cache is a reference")]
	public class SemDomSearchCache
	{
		// Cached words from Semantic Domain Name and Example Words fields
		private readonly Dictionary<Tuple<string, int>, HashSet<ICmSemanticDomain>> m_semDomCache;
		private readonly ICmSemanticDomainRepository m_semDomRepo;
		private readonly CultureInfo m_appropriateCulture;
		private readonly Regex m_wordParsingRegex;
		private readonly FdoCache m_cache;

		/// <summary>
		/// Create an (uninitialized) cache of Semantic Domains by word string and writing system
		/// integer to provide quick searching for multiple keys (bulk edit of the
		/// LexSense.SemanticDomains collection; Suggest button).
		/// </summary>
		/// <param name="cache">FdoCache</param>
		public SemDomSearchCache(FdoCache cache)
		{
			CacheIsInitialized = false;
			m_cache = cache;
			m_semDomRepo = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			m_semDomCache = new Dictionary<Tuple<string, int>, HashSet<ICmSemanticDomain>>();
			m_appropriateCulture = GetAppropriateCultureInfo();
			// Starting after whitespace or the beginning of a line, finds runs of either letters or
			// singlequote or hyphen followed by either whitespace or the end of a line.
			// The first look-ahead, (?=\w), means that single quote and hyphen are only included
			// if there is an adjacent following letter, that is, if they occur word-medially.
			m_wordParsingRegex = new Regex(@"(?<=(^|\s))(\w|['-](?=\w))*(?=(\s|,|$))");
		}


		/// <summary>
		/// Returns true if InitializeCache() has been run.
		/// </summary>
		public bool CacheIsInitialized { get; set; }

		/// <summary>
		/// Loops through all the Semantic Domains to add words from Name and Example Words
		/// fields to the cache for each available analysis writing system.
		/// </summary>
		public void InitializeCache()
		{
			foreach (var domain in m_semDomRepo.AllInstances())
			{
				CacheDomain(domain);
			}
			CacheIsInitialized = true;
		}

		private void CacheDomain(ICmSemanticDomain domain)
		{
			// Check for whole word matches in Name and ExampleWords
			CacheWordsIn(GetDomainNameString(domain), domain);
			CacheWordsIn(GetExampleWordsString(domain.QuestionsOS), domain);
		}

		private void CacheWordsIn(Tuple<int, string>[] wsStringTupleArray, ICmSemanticDomain domain)
		{
			foreach (var tuple in wsStringTupleArray)
			{
				var ws = tuple.Item1;
				var words = ParseStringIntoWords(tuple.Item2);
				foreach (var word in words)
				{
					CacheWord(ws, word, domain);
				}
			}
		}

		private void CacheWord(int ws, string word, ICmSemanticDomain domain)
		{
			var searchKey = new Tuple<string, int>(word, ws);
			HashSet<ICmSemanticDomain> cachedValues; // out var
			if (m_semDomCache.TryGetValue(searchKey, out cachedValues))
			{
				if (cachedValues.Contains(domain))
					return; // this domain is already cached

				// searchKey is already cached, but we need to add this domain
				cachedValues.Add(domain);
				m_semDomCache.Remove(searchKey);
				m_semDomCache.Add(searchKey, cachedValues);
				return;
			}

			// searchKey was not found, add it with this domain
			m_semDomCache.Add(searchKey, new HashSet<ICmSemanticDomain>() { domain });
		}

		/// <summary>
		/// Get the text from a Semantic Domain's Name field.
		/// </summary>
		/// <param name="domain"></param>
		/// <returns>an array of Tuples, where each Tuple has a ws handle and a name string</returns>
		private static Tuple<int, string>[] GetDomainNameString(ICmSemanticDomain domain)
		{
			var name = domain.Name;
			return name.AvailableWritingSystemIds.Select(ws => new Tuple<int, string>(
				ws, name.get_String(ws).Text)).Where(tuple => !String.IsNullOrEmpty(tuple.Item2)).ToArray();
		}

		/// <summary>
		/// Takes a list of semantic domain question objects and fishes out the example words
		/// and puts them in an array of space-delimited strings for easier searching.
		/// (one string per writing system)
		/// </summary>
		/// <param name="questions"></param>
		/// <returns></returns>
		protected Tuple<int, string>[] GetExampleWordsString(IEnumerable<ICmDomainQ> questions)
		{
			var results = new List<Tuple<int, string>>();
			foreach (var domainQuest in questions.Where(question => question.ExampleWords != null))
			{
				results.AddRange(GetExampleWordTextFromDomainQuestion(domainQuest));
			}
			CondenseResults(results);
			return results.ToArray();
		}

		private static void CondenseResults(List<Tuple<int, string>> results)
		{
			for (var i = 0; i < results.Count - 1; i++)
			{
				var currentTuple = results[i];
				for (var j = i + 1; j < results.Count; j++)
				{
					var candidateTuple = results[j];
					if (currentTuple.Item1 != candidateTuple.Item1)
						continue;
					results[i] = new Tuple<int, string>(currentTuple.Item1, currentTuple.Item2 + " " + candidateTuple.Item2);
					results.RemoveAt(j);
				}
			}
		}

		/// <summary>
		/// For one Semantic Domain question, return the text of the Example Words field.
		/// </summary>
		/// <param name="questionObject"> </param>
		/// <returns>an array of Tuples, where each Tuple has a ws handle and a string of
		/// comma-deliminated (usually) words</returns>
		private static IEnumerable<Tuple<int, string>> GetExampleWordTextFromDomainQuestion(ICmDomainQ questionObject)
		{
			var wordsField = questionObject.ExampleWords;
			return wordsField.AvailableWritingSystemIds.Select(ws => new Tuple<int, string>(
				ws, wordsField.get_String(ws).Text)).Where(tuple => !String.IsNullOrEmpty(tuple.Item2)).ToArray();
		}

		private CultureInfo GetAppropriateCultureInfo()
		{
			CultureInfo ci;
			try
			{
				ci = new CultureInfo(m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultAnalWs));
			}
			catch (Exception)
			{
				ci = CultureInfo.InvariantCulture;
			}
			return ci;
		}

		/// <summary>
		/// Tries to get a predefined culture for the Default Analysis Ws, failing that
		/// we go with the InvariantCulture. Used for finding matches.
		/// </summary>
		protected CultureInfo AppropriateCulture { get { return m_appropriateCulture; } }

		private IEnumerable<string> ParseStringIntoWords(string searchIn)
		{
			return (from Match match in m_wordParsingRegex.Matches(searchIn)
					select match.Value).ToArray();
		}

		/// <summary>
		/// Given a writing system integer and a word (searchKey string), this method
		/// returns all the Semantic Domains that contain that exact word in their Name or
		/// Example Words fields.
		///
		/// N.B. Make sure the cache has been initialized first.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="searchKey"></param>
		/// <returns>Be prepared for null return value, if string/ws combo is not cached</returns>
		/// <exception cref="ApplicationException">if the cache has not been initialized</exception>
		public IEnumerable<ICmSemanticDomain> GetDomainsForCachedString(int ws, string searchKey)
		{
			if (!CacheIsInitialized)
				throw new ApplicationException("Semantic Domain search cache is not initialized!");

			HashSet<ICmSemanticDomain> cachedDomains;
			return !m_semDomCache.TryGetValue(new Tuple<string, int>(searchKey, ws), out cachedDomains) ? null : cachedDomains;
		}
	}
}
