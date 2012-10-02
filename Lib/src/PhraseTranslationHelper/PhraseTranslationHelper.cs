// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PhraseTranslationHelper.cs
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.Utils;
using System;

namespace SILUBS.PhraseTranslationHelper
{
	/// --------------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// --------------------------------------------------------------------------------------------
	public class PhraseTranslationHelper
	{
		#region Events
		public event Action TranslationsChanged;
		#endregion

		#region Data members
		private readonly List<TranslatablePhrase> m_phrases = new List<TranslatablePhrase>();
		private readonly Dictionary<Regex, string> m_phraseSubstitutions;
		/// <summary>A lookup table of the last word of all known English key terms to the
		/// actual key term objects.</summary>
		private readonly Dictionary<Word, List<KeyTermMatch>> m_keyTermsTable;
		/// <summary>A double lookup table of all parts in all phrases managed by this class.
		/// For improved performance, outer lookup is by wordcount.</summary>
		private readonly SortedDictionary<int, Dictionary<Word, List<Part>>> m_partsTable;
		private List<TranslatablePhrase> m_filteredPhrases;
		private readonly Dictionary<int, TranslatablePhrase> m_categories = new Dictionary<int, TranslatablePhrase>(2);
		private readonly Dictionary<TypeOfPhrase, string> m_initialPunct = new Dictionary<TypeOfPhrase, string>();
		private readonly Dictionary<TypeOfPhrase, string> m_finalPunct = new Dictionary<TypeOfPhrase, string>();
		private bool m_justGettingStarted = true;
		private string m_keyTermRenderingRulesFile;
		private List<RenderingSelectionRule> m_termRenderingSelectionRules;
		private SortBy m_listSortCriterion = SortBy.Default;
		private bool m_listSortedAscending = true;
		/// <summary>Indicates whether the filtered list's sorting has been done</summary>
		private bool m_listSorted = false;

		private const int kAscending = 1;
		private const int kDescending = -1;
		#endregion

		#region SortBy enumeration
		public enum SortBy
		{
			Default,
			Reference,
			OriginalPhrase,
			Translation,
			Status,
		}
		#endregion

		#region KeyTermFilterType enumeration
		public enum KeyTermFilterType
		{
			All,
			WithRenderings,
			WithoutRenderings,
		}
		#endregion

		#region SubPhraseMatch class
		private class SubPhraseMatch
		{
			internal readonly int StartIndex;
			internal readonly Part Part;

			public SubPhraseMatch(int startIndex, Part part)
			{
				StartIndex = startIndex;
				Part = part;
			}
		}
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PhraseTranslationHelper"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PhraseTranslationHelper(IEnumerable<TranslatablePhrase> phrases,
			IEnumerable<IKeyTerm> keyTerms, KeyTermRules keyTermRules,
			IEnumerable<Substitution> phrasesToIgnore)
		{
			TranslatablePhrase.s_helper = this;

			m_keyTermsTable = new Dictionary<Word, List<KeyTermMatch>>(keyTerms.Count());
			PopulateKeyTermsTable(keyTerms, keyTermRules);

			m_phraseSubstitutions = new Dictionary<Regex, string>(phrasesToIgnore.Count());
			foreach (Substitution substitutePhrase in phrasesToIgnore)
				m_phraseSubstitutions[substitutePhrase.RegEx] = substitutePhrase.RegExReplacementString;

			m_partsTable = new SortedDictionary<int, Dictionary<Word, List<Part>>>();
			foreach (TranslatablePhrase phrase in phrases)
			{
				if (!string.IsNullOrEmpty(phrase.OriginalPhrase))
				{
					PhraseParser parser = new PhraseParser(m_keyTermsTable, m_phraseSubstitutions, phrase, GetOrCreatePart);
					foreach (IPhrasePart part in parser.Parse())
						phrase.m_parts.Add(part);
					m_phrases.Add(phrase);
					if (phrase.Category == -1)
					{
						m_categories[phrase.SequenceNumber] = phrase;
					}
				}
			}

			for (int wordCount = m_partsTable.Keys.Max(); wordCount > 1; wordCount--)
			{
				Dictionary<Word, List<Part>> partsTable;
				if (!m_partsTable.TryGetValue(wordCount, out partsTable))
					continue;

				List<Part> partsToDelete = new List<Part>();

				foreach (KeyValuePair<Word, List<Part>> phrasePartPair in partsTable) // REVIEW: problem: won't be able to add a new part that starts with this word
				{
					foreach (Part part in phrasePartPair.Value)
					{
						if (part.OwningPhrases.Count() != 1)
							continue;

						// Look to see if some other part is a sub-phrase of this part.
						SubPhraseMatch match = FindSubPhraseMatch(part);
						if (match != null)
						{
							TranslatablePhrase owningPhraseOfPart = part.OwningPhrases.First();
							int iPart = owningPhraseOfPart.m_parts.IndexOf(part);
							// Deal with any preceding remainder
							if (match.StartIndex > 0)
							{
								Part preceedingPart = GetOrCreatePart(part.GetSubWords(0, match.StartIndex), owningPhraseOfPart, wordCount);
								owningPhraseOfPart.m_parts.Insert(iPart++, preceedingPart);
							}
							match.Part.AddOwningPhrase(owningPhraseOfPart);
							owningPhraseOfPart.m_parts[iPart++] = match.Part;
							// Deal with any following remainder
							// Breaks this part at the given position because an existing part was found to be a
							// substring of this part. Any text before the part being excluded will be broken off
							// as a new part and returned. Any text following the part being excluded will be kept
							// as this part's contents.
							if (match.StartIndex + match.Part.m_words.Count < part.m_words.Count)
							{
								Part followingPart = GetOrCreatePart(part.GetSubWords(match.StartIndex + match.Part.m_words.Count), owningPhraseOfPart, wordCount);
								owningPhraseOfPart.m_parts.Insert(iPart, followingPart);
							}
							partsToDelete.Add(part);
						}
					}
				}
				foreach (Part partToDelete in partsToDelete)
				{
					partsTable[partToDelete.m_words[0]].Remove(partToDelete);
				}
			}
			m_filteredPhrases = m_phrases;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the list of phrases in the specified way.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Sort(SortBy by, bool ascending)
		{
			if (m_listSortCriterion != by)
			{
				m_listSortCriterion = by;
				m_listSortedAscending = ascending;
				m_listSorted = false;
			}
			else if (m_listSortedAscending != ascending)
			{
				if (m_listSorted)
					m_filteredPhrases.Reverse();
				else
					m_listSortedAscending = ascending;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the specified list of phrases in the specified way.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void SortList(List<TranslatablePhrase> phrases, SortBy by, bool ascending)
		{
			if (by == SortBy.Default)
			{
				phrases.Sort();
				if (!ascending)
					phrases.Reverse();
				return;
			}

			Comparison<TranslatablePhrase> how;
			int direction = ascending ? kAscending : kDescending;
			switch (by)
			{
				case SortBy.Reference:
					how = PhraseReferenceComparison(direction);
					break;
				case SortBy.OriginalPhrase:
					how = (a, b) => a.OriginalPhrase.CompareTo(b.OriginalPhrase) * direction;
					break;
				case SortBy.Translation:
					how = (a, b) => a.Translation.CompareTo(b.Translation) * direction;
					break;
				case SortBy.Status:
					how = (a, b) => a.HasUserTranslation.CompareTo(b.HasUserTranslation) * direction;
					break;
				default:
					throw new ArgumentException("Unexpected sorting method", "by");
			}
			phrases.Sort(how);
		}

		private static Comparison<TranslatablePhrase> PhraseReferenceComparison(int direction)
		{
			return (a, b) =>
			{
				int val = a.StartRef.CompareTo(b.StartRef);
				if (val == 0)
				{
					val = a.Category.CompareTo(b.Category);
					if (val == 0)
					{
						val = a.EndRef.CompareTo(b.EndRef);
						if (val == 0)
						{
							val = a.SequenceNumber.CompareTo(b.SequenceNumber);
						}
					}
				}
				return val * direction;
			};
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populates the key terms table.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PopulateKeyTermsTable(IEnumerable<IKeyTerm> keyTerms, KeyTermRules rules)
		{
			Dictionary<string, KeyTermRule> ktRules = new Dictionary<string, KeyTermRule>();
			if (rules != null)
			{
				foreach (KeyTermRule keyTermRule in rules.Items.Where(keyTermRule => !String.IsNullOrEmpty(keyTermRule.id)))
					ktRules[keyTermRule.id] = keyTermRule;
			}

			KeyTermMatchBuilder matchBuilder;

			foreach (IKeyTerm keyTerm in keyTerms)
			{
				matchBuilder = new KeyTermMatchBuilder(keyTerm, ktRules);

				foreach (KeyTermMatch matcher in matchBuilder.Matches)
				{
					if (!matcher.Words.Any())
						continue;

					List<KeyTermMatch> foundMatchers;
					Word firstWord = matcher.Words.First();
					if (!m_keyTermsTable.TryGetValue(firstWord, out foundMatchers))
						m_keyTermsTable[firstWord] = foundMatchers = new List<KeyTermMatch>();

					KeyTermMatch existingMatcher = foundMatchers.FirstOrDefault(m => m.Equals(matcher));
					if (existingMatcher == null)
						foundMatchers.Add(matcher);
					else
						existingMatcher.AddTerm(keyTerm);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or creates a part matching the given sub-phrase.
		/// </summary>
		/// <param name="words">The words of the sub-phrase.</param>
		/// <param name="owningPhraseOfPart">The owning phrase of the part to find or create.</param>
		/// <param name="tempWordCountOfPhraseBeingBroken">The temp word count of phrase being broken.</param>
		/// <returns>the newly created or found part</returns>
		/// ------------------------------------------------------------------------------------
		private Part GetOrCreatePart(IEnumerable<Word> words, TranslatablePhrase owningPhraseOfPart,
			int tempWordCountOfPhraseBeingBroken)
		{
			Debug.Assert(words.Any());
			Part part = null;

			Dictionary<Word, List<Part>> partsTable;
			List<Part> parts = null;
			if (m_partsTable.TryGetValue(words.Count(), out partsTable))
			{
				if (partsTable.TryGetValue(words.First(), out parts))
					part = parts.FirstOrDefault(x => x.Words.SequenceEqual(words));
			}
			else
				m_partsTable[words.Count()] = partsTable = new Dictionary<Word, List<Part>>();

			if (parts == null)
				partsTable[words.First()] = parts = new List<Part>();

			if (part == null)
			{
				Debug.Assert(tempWordCountOfPhraseBeingBroken != words.Count());
				part = new Part(words);
				parts.Add(part);
			}

			part.AddOwningPhrase(owningPhraseOfPart);

			return part;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the longest phrase that is a sub-phrase of the specified part.
		/// </summary>
		/// <param name="part">The part.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private SubPhraseMatch FindSubPhraseMatch(Part part)
		{
			int partWordCount = part.m_words.Count;
			for (int subPhraseWordCount = partWordCount - 1; subPhraseWordCount > 1; subPhraseWordCount--)
			{
				Dictionary<Word, List<Part>> subPhraseTable;
				if (!m_partsTable.TryGetValue(subPhraseWordCount, out subPhraseTable))
					continue;

				for (int iWord = 0; iWord < partWordCount; iWord++)
				{
					Word word = part.m_words[iWord];
					if (iWord + subPhraseWordCount > partWordCount)
						break; // There aren't enough words left in this part to find a match
					List<Part> possibleSubParts;
					if (subPhraseTable.TryGetValue(word, out possibleSubParts))
					{
						foreach (Part possibleSubPart in possibleSubParts)
						{
							int iWordTemp = iWord + 1;
							int isubWord = 1;
							int possiblePartWordCount = possibleSubPart.m_words.Count;
							while (isubWord < possiblePartWordCount && possibleSubPart.m_words[isubWord] == part.m_words[iWordTemp++])
								isubWord++;
							if (isubWord == possiblePartWordCount)
								return new SubPhraseMatch(iWord, possibleSubPart);
						}
					}
				}
			}
			return null;
		}
		#endregion

		#region Public methods and properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the (first) phrase in the collection that matches the given text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TranslatablePhrase GetPhrase(string origPhrase)
		{
			return GetPhrase(null, origPhrase);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the (first) phrase in the collection that matches the given text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TranslatablePhrase GetPhrase(string reference, string origPhrase)
		{
			origPhrase = origPhrase.Normalize(NormalizationForm.FormD);
			return m_phrases.FirstOrDefault(x => (reference == null || x.Reference == reference) && x.OriginalPhrase == origPhrase);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the phrases (filtered and sorted).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<TranslatablePhrase> Phrases
		{
			get { return FilteredSortedPhrases; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the filtered phrases, sorting them first if needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<TranslatablePhrase> FilteredSortedPhrases
		{
			get
			{
				if (!m_listSorted)
				{
					SortList(m_filteredPhrases, m_listSortCriterion, m_listSortedAscending);
					m_listSorted = true;
				}
				return m_filteredPhrases;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the term-rendering selection rules.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<RenderingSelectionRule> TermRenderingSelectionRules
		{
			get { return m_termRenderingSelectionRules; }
			set
			{
				m_termRenderingSelectionRules = value;
				if (m_keyTermRenderingRulesFile != null)
				{
					UNSQuestionsDialog.EnsureDataFolderExists();
					XmlSerializationHelper.SerializeToFile(m_keyTermRenderingRulesFile, m_termRenderingSelectionRules);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the complete list of phrases sorted by reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal IEnumerable<TranslatablePhrase> UnfilteredPhrases
		{
			get
			{
				List<TranslatablePhrase> temp = m_phrases.GetRange(0, m_phrases.Count);
				temp.Sort(PhraseReferenceComparison(kAscending));
				return temp;
			}
		}

		internal string KeyTermRenderingRulesFile
		{
			set
			{
				m_keyTermRenderingRulesFile = value;
				m_termRenderingSelectionRules = XmlSerializationHelper.LoadOrCreateList<RenderingSelectionRule>(m_keyTermRenderingRulesFile, true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of phrases/questions matching the applied filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int FilteredPhraseCount
		{
			get { return m_filteredPhrases.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the total number of phrases/questions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int UnfilteredPhraseCount
		{
			get { return m_phrases.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the translation of the requested category; if not translated, use the English.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string GetCategoryName(int categoryId)
		{
			string catName = m_categories[categoryId].Translation;
			if (string.IsNullOrEmpty(catName))
				catName = m_categories[categoryId].OriginalPhrase;
			return catName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the translatable phrase at the specified <paramref name="index"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TranslatablePhrase this[int index]
		{
			get { return FilteredSortedPhrases[index]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Filters the list of translatable phrases.
		/// </summary>
		/// <param name="partMatchString">String to filter "translatable" parts (not key term
		/// parts).</param>
		/// <param name="wholeWordMatch">If set to <c>true</c> the match string will only match
		/// complete words.</param>
		/// <param name="ktFilter">The type of Key Terms filter to apply.</param>
		/// <param name="refFilter">The reference filter delegate (params are startRef, endRef,
		/// and string representation of reference).</param>
		/// ------------------------------------------------------------------------------------
		public void Filter(string partMatchString, bool wholeWordMatch, KeyTermFilterType ktFilter,
			Func<int, int, string, bool> refFilter)
		{
			Func<int, int, string, bool> filterByRef = refFilter ?? new Func<int, int, string, bool>((start, end, sref) => true);

			m_listSorted = false;

			if (string.IsNullOrEmpty(partMatchString))
			{
				if (ktFilter != KeyTermFilterType.All)
					m_filteredPhrases = m_phrases.Where(phrase => phrase.MatchesKeyTermFilter(ktFilter) &&
						filterByRef(phrase.StartRef, phrase.EndRef, phrase.Reference)).ToList();
				else if (refFilter != null)
					m_filteredPhrases = m_phrases.Where(phrase => filterByRef(phrase.StartRef, phrase.EndRef, phrase.Reference)).ToList();
				else
					m_filteredPhrases = m_phrases;
				return;
			}

			partMatchString = Regex.Escape(partMatchString.Normalize(NormalizationForm.FormD));
			if (wholeWordMatch)
				partMatchString = @"\b" + partMatchString + @"\b";
			Regex regexFilter = new Regex(partMatchString,
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			m_filteredPhrases = m_phrases.Where(phrase => regexFilter.IsMatch(phrase.OriginalPhrase) &&
				phrase.MatchesKeyTermFilter(ktFilter) &&
				filterByRef(phrase.StartRef, phrase.EndRef, phrase.Reference)).ToList();
		}
		#endregion

		#region Private and internal methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes a new translation on a phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ProcessTranslation(TranslatablePhrase tp)
		{
			string initialPunct, finalPunct;

			StringBuilder bldr = new StringBuilder();
			if (tp.Translation.StartsWith("\u00BF"))
				bldr.Append(tp.Translation[0]);
			if (bldr.Length > 0 && bldr.Length < tp.Translation.Length)
				m_initialPunct[tp.TypeOfPhrase] = initialPunct = bldr.ToString();
			else
				initialPunct = InitialPunctuationForType(tp.TypeOfPhrase);

			bldr.Length = 0;
			foreach (char t in tp.Translation.Reverse().TakeWhile(Char.IsPunctuation))
				bldr.Insert(0, t);
			if (bldr.Length > 0 && bldr.Length < tp.Translation.Length)
				m_finalPunct[tp.TypeOfPhrase] = finalPunct = bldr.ToString();
			else
				finalPunct = InitialPunctuationForType(tp.TypeOfPhrase);

			List<Part> tpParts = tp.TranslatableParts.ToList();
			if (tpParts.Count == 0)
				return;

			string translation = tp.GetTranslationTemplate();

			foreach (TranslatablePhrase similarPhrase in tpParts[0].OwningPhrases.Where(phrase => !phrase.HasUserTranslation && phrase.PartPatternMatches(tp)))
			{
				if (similarPhrase.OriginalPhrase == tp.OriginalPhrase)
					similarPhrase.Translation = tp.Translation;
				else if (tp.AllTermsMatch)
					similarPhrase.SetProvisionalTranslation(translation);
			}

			if (tp.AllTermsMatch)
			{
				if (tpParts.Count == 1)
				{
					if (translation.StartsWith(initialPunct))
						translation = translation.Remove(0, initialPunct.Length);
					if (translation.EndsWith(finalPunct))
						translation = translation.Substring(0, translation.Length - finalPunct.Length);

					tpParts[0].Translation = Regex.Replace(translation, @"\{.+\}", string.Empty).Trim();
					if (TranslationsChanged != null)
						TranslationsChanged();
					return;
				}
			}

			if (m_justGettingStarted)
				return;

			if (translation.StartsWith(initialPunct))
				translation = translation.Remove(0, initialPunct.Length);
			if (translation.EndsWith(finalPunct))
				translation = translation.Substring(0, translation.Length - finalPunct.Length);

			List<Part> unTranslatedParts = new List<Part>(tpParts);
			HashSet<Part> partsNeedingUpdating = new HashSet<Part>();
			foreach (Part part in tpParts)
			{
				partsNeedingUpdating.UnionWith(RecalculatePartTranslation(part).Where(p => !tpParts.Contains(p)));
				if (part.Translation.Length > 0)
				{
					int ichMatch = translation.IndexOf(part.Translation, StringComparison.Ordinal);
					if (ichMatch >= 0)
						translation = translation.Remove(ichMatch, part.Translation.Length);
					unTranslatedParts.Remove(part);
				}
			}
			if (unTranslatedParts.Count == 1)
				unTranslatedParts[0].Translation = Regex.Replace(translation, @"\{.+\}", string.Empty).Trim();

			foreach (Part partNeedingUpdating in partsNeedingUpdating)
				RecalculatePartTranslation(partNeedingUpdating);

			if (TranslationsChanged != null)
				TranslationsChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recalculates the part translation by considering all the owning phrases of the part
		/// and a probable translation based on what they have in common.
		/// </summary>
		/// <param name="part">The part.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static IEnumerable<Part> RecalculatePartTranslation(Part part)
		{
			string originalTranslation = part.Translation;

			List<string> userTranslations = new List<string>();
			foreach (TranslatablePhrase phrase in part.OwningPhrases.Where(op => op.HasUserTranslation))
			{
				string toAdd = phrase.UserTransSansOuterPunctuation;
				foreach (IPhrasePart otherPart in phrase.GetParts().Where(otherPart => otherPart != part))
				{
					if (otherPart is KeyTermMatch)
					{
						foreach (string ktTrans in ((KeyTermMatch)otherPart).Renderings)
						{
							int ich = toAdd.IndexOf(ktTrans, StringComparison.Ordinal);
							if (ich >= 0)
							{
								toAdd = toAdd.Remove(ich, ktTrans.Length).Insert(ich, StringUtils.kszObject);
								break;
							}
						}
					}
					else
					{
						if (otherPart.Translation.Length > 0)
						{
							int ichMatch = toAdd.IndexOf(otherPart.Translation, StringComparison.Ordinal);
							if (ichMatch >= 0)
								toAdd = toAdd.Remove(ichMatch, otherPart.Translation.Length).Insert(ichMatch, StringUtils.kszObject);
						}
					}
				}
				if (!string.IsNullOrEmpty(toAdd))
					userTranslations.Add(toAdd);
			}

			string commonTranslation = GetSCommonSubstring(userTranslations);
			if (commonTranslation != null && commonTranslation.Length > 5) // 5 is a "magic number" - We don't want to accept really small words without considering a possibly better match statistically
				part.Translation = commonTranslation;
			else
			{
				Dictionary<string, double> commonSubstrings = new Dictionary<string, double>(userTranslations.Count * 2);
				KeyValuePair<string, double> statisticallyBestSubstring = new KeyValuePair<string, double>(null, -1);
				bool fCommonSubstringIsWholeWord = false;
				bool fBestIsWholeWord = false;
				for (int i = 0; i < userTranslations.Count - 1; i++)
				{
					for (int j = i + 1; j < userTranslations.Count; j++)
					{
						string sCommonSubstring = StringUtils.LongestUsefulCommonSubstring(userTranslations[i], userTranslations[j],
							fCommonSubstringIsWholeWord, out fCommonSubstringIsWholeWord).Trim();
						if (sCommonSubstring.Length > 1 || (sCommonSubstring.Length == 1 && Char.IsLetter(sCommonSubstring[0])))
						{
							double val;
							commonSubstrings.TryGetValue(sCommonSubstring, out val);
							val += Math.Sqrt(sCommonSubstring.Length);
							commonSubstrings[sCommonSubstring] = val;
							// A whole-word match always trumps a partial-word match.
							if (val > statisticallyBestSubstring.Value || (!fBestIsWholeWord && fCommonSubstringIsWholeWord))
							{
								statisticallyBestSubstring = new KeyValuePair<string, double>(sCommonSubstring, val);
								fBestIsWholeWord = fCommonSubstringIsWholeWord;
							}
						}
					}
				}
				int totalComparisons = ((userTranslations.Count * userTranslations.Count) + userTranslations.Count) / 2;
				part.Translation = (string.IsNullOrEmpty(commonTranslation) || statisticallyBestSubstring.Value > totalComparisons) ?
					statisticallyBestSubstring.Key : commonTranslation;
			}
			if (originalTranslation.Length > 0 && (part.Translation.Length == 0 || originalTranslation.Contains(part.Translation)))
			{
				// The translation of the part has shrunk
				return part.OwningPhrases.Where(phr => phr.HasUserTranslation).SelectMany(otherPhrases => otherPhrases.TranslatableParts).Distinct();
			}
			return new Part[0];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the longest substring common to all the given strings. This can't return less
		/// than a whole word.
		/// TODO: Handle agglutinative languages
		/// </summary>
		/// <param name="strings">The strings to consider.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetSCommonSubstring(IList<string> strings)
		{
			string firstOne = strings[0];
			string sCommonSubstring;
			if (strings.Count == 1)
				sCommonSubstring = string.Empty;
			else
			{
				bool fCommonSubstringIsWholeWord;
				sCommonSubstring = StringUtils.LongestUsefulCommonSubstring(firstOne, strings[1],
					true, out fCommonSubstringIsWholeWord);
				for (int i = 2; i < strings.Count; i++)
				{
					string sPossibleCommonSubstring = StringUtils.LongestUsefulCommonSubstring(strings[i], sCommonSubstring,
						true, out fCommonSubstringIsWholeWord);
					if (sPossibleCommonSubstring.Length < sCommonSubstring.Length)
					{
						i = 1;
						int ichCommonPiece = sPossibleCommonSubstring.Length == 0 ? -1 :
							sCommonSubstring.IndexOf(sPossibleCommonSubstring, StringComparison.Ordinal);
						if (ichCommonPiece < 0)
							firstOne = firstOne.Replace(sCommonSubstring, StringUtils.kszObject);
						else
						{
							if (ichCommonPiece > 0)
								firstOne = firstOne.Replace(sCommonSubstring.Substring(0, ichCommonPiece), StringUtils.kszObject);
							if (ichCommonPiece + sPossibleCommonSubstring.Length < sCommonSubstring.Length)
							{
								firstOne = firstOne.Replace(sCommonSubstring.Substring(ichCommonPiece + sPossibleCommonSubstring.Length),
									StringUtils.kszObject);
							}
						}
						sCommonSubstring = StringUtils.LongestUsefulCommonSubstring(firstOne, strings[i],
							true, out fCommonSubstringIsWholeWord);
					}
				}
			}
			return sCommonSubstring;
		}

		internal string InitialPunctuationForType(TypeOfPhrase type)
		{
			string p;
			return m_initialPunct.TryGetValue(type, out p) ? p : string.Empty;
		}

		internal string FinalPunctuationForType(TypeOfPhrase type)
		{
			string p;
			return m_finalPunct.TryGetValue(type, out p) ? p : string.Empty;
		}
		#endregion

		internal void ProcessAllTranslations()
		{
			if (!m_justGettingStarted)
				throw new InvalidOperationException("This method should only be called once, after all the saved translations have been loaded.");

			foreach (Part part in m_partsTable.Values.SelectMany(thing => thing.Values.SelectMany(parts => parts)))
			{
				if (part.OwningPhrases.Where(p => p.HasUserTranslation).Skip(1).Any()) // Must have at least 2 phrases with translations
					RecalculatePartTranslation(part);
			}

			m_justGettingStarted = false;
		}
	}
}
