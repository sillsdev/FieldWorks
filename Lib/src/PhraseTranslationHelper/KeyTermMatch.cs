// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2011' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: KeyTermMatch.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Utils;

namespace SILUBS.PhraseTranslationHelper
{
	public class KeyTermMatch : IPhrasePart, IKeyTerm
	{
		#region Events and Delegates
		public delegate void BestRenderingChangedHandler(KeyTermMatch sender);
		public event BestRenderingChangedHandler BestRenderingChanged;
		#endregion

		#region Data members
		internal readonly List<Word> m_words;
		private readonly List<IKeyTerm> m_terms;
		private string m_bestTranslation = null;
		private readonly bool m_matchForRefOnly;
		private HashSet<int> m_occurrences;
		private static string m_keyTermRenderingInfoFile;
		private static List<KeyTermRenderingInfo> m_keyTermRenderingInfo;
		#endregion

		#region Construction & initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermMatch"/> class.
		/// </summary>
		/// <param name="words">The words.</param>
		/// <param name="term">The term.</param>
		/// <param name="matchForRefOnly">if set to <c>true</c> [match for ref only].</param>
		/// ------------------------------------------------------------------------------------
		internal KeyTermMatch(IEnumerable<Word> words, IKeyTerm term, bool matchForRefOnly)
		{
			m_matchForRefOnly = matchForRefOnly;
			m_words = words.ToList();
			m_terms = new List<IKeyTerm>();
			m_terms.Add(term);
			KeyTermRenderingInfo info = m_keyTermRenderingInfo.FirstOrDefault(i => i.TermId == Term);
			if (info != null)
				m_bestTranslation = info.PreferredRendering;
		}

		internal static string RenderingInfoFile
		{
			set
			{
				m_keyTermRenderingInfoFile = value;
				m_keyTermRenderingInfo = XmlSerializationHelper.LoadOrCreateList<KeyTermRenderingInfo>(m_keyTermRenderingInfoFile, true);
			}
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns>
		/// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			if (obj is KeyTermMatch)
			{
				return m_words.SequenceEqual(((KeyTermMatch)obj).m_words);
			}
			if (obj is IEnumerable<Word>)
			{
				return m_words.SequenceEqual((IEnumerable<Word>)obj);
			}
			return base.Equals(obj);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data
		/// structures like a hash table.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Term; //WAS: m_words.ToString(" ");
		}
		#endregion

		#region Public & internal methods
		public bool AppliesTo(int startRef, int endRef)
		{
			if (!m_matchForRefOnly)
				return true;
			if (m_occurrences == null)
				m_occurrences = new HashSet<int>(m_terms.SelectMany(term => term.BcvOccurences));
			return m_occurrences.Any(o => startRef <= o && endRef >= o);
		}

		public void AddTerm(IKeyTerm keyTerm)
		{
			if (keyTerm == null)
				throw new ArgumentNullException("keyTerm");
			m_terms.Add(keyTerm);
		}

		public void AddWord(Word word)
		{
			if (word == null)
				throw new ArgumentNullException("word");
			m_words.Add(word);
		}

		public void AddWords(IEnumerable<Word> words)
		{
			m_words.AddRange(words);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a rendering.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddRendering(string rendering)
		{
			if (Renderings.Contains(rendering.Normalize(NormalizationForm.FormD)))
				throw new ArgumentException(Properties.Resources.kstidRenderingExists);
			KeyTermRenderingInfo info = RenderingInfo;
			if (info == null)
			{
				info = new KeyTermRenderingInfo(Term, BestRendering);
				m_keyTermRenderingInfo.Add(info);
			}
			info.AddlRenderings.Add(rendering);
			UpdateRenderingInfoFile();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified rendering can be deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanRenderingBeDeleted(string rendering)
		{
			if (rendering == BestRendering)
				return false;
			KeyTermRenderingInfo info = RenderingInfo;
			if (info == null)
				return false;

			return info.AddlRenderings.Contains(rendering.Normalize(NormalizationForm.FormC));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified rendering can be deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeleteRendering(string rendering)
		{
			rendering = rendering.Normalize(NormalizationForm.FormC);
			KeyTermRenderingInfo info = RenderingInfo;
			if (info == null || !info.AddlRenderings.Contains(rendering))
				throw new ArgumentException("Cannot delete non-existent rendering: " + rendering);

			if (info.AddlRenderings.Remove(rendering))
				UpdateRenderingInfoFile();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the best rendering for this term in when used in the context of the given
		/// phrase.
		/// </summary>
		/// <remarks>If this term occurs more than once in the phrase, it is not possible to
		/// know which occurrence is which.</remarks>
		/// ------------------------------------------------------------------------------------
		public string GetBestRenderingInContext(TranslatablePhrase phrase)
		{
			IEnumerable<string> renderings = Renderings;
			if (!renderings.Any())
				return string.Empty;
			if (renderings.Count() == 1 || TranslatablePhrase.s_helper.TermRenderingSelectionRules == null)
				return Translation;

			List<string> renderingsList = null;
			foreach (RenderingSelectionRule rule in TranslatablePhrase.s_helper.TermRenderingSelectionRules.Where(r => !r.Disabled))
			{
				if (renderingsList == null)
				{
					renderingsList = new List<string>();
					foreach (string rendering in renderings)
					{
						if (rendering == Translation)
							renderingsList.Insert(0, rendering);
						else
							renderingsList.Add(rendering);
					}
				}
				string s = rule.ChooseRendering(phrase.PhraseInUse, Words, renderingsList);
				if (!string.IsNullOrEmpty(s))
					return s;
			}
			return Translation;
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enumerates the words that this object matches on.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<Word> Words
		{
			get { return m_words; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the term in the "source" language (i.e., the source of the UNS questions list,
		/// which is in English).
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string Term
		{
			get { return m_terms.First().Term; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Translation
		{
			get
			{
				if (m_bestTranslation == null)
				{
					int max = 0;
					Dictionary<string, int> occurrences = new Dictionary<string, int>();
					foreach (string rendering in m_terms.Select(keyTerm => keyTerm.BestRendering).Where(rendering => rendering != null))
					{
						string normalizedRendering = rendering.Normalize(NormalizationForm.FormD);
						int num;
						occurrences.TryGetValue(normalizedRendering, out num);
						occurrences[normalizedRendering] = ++num;
						if (num > max)
						{
							m_bestTranslation = normalizedRendering;
							max = num;
						}
					}
					if (m_bestTranslation == null)
						m_bestTranslation = string.Empty;
				}
				return m_bestTranslation;
			}
		}

		public string DebugInfo
		{
			get { return "KT: " + Translation; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all (distinct) renderings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> Renderings
		{
			get
			{
				//List<string> renderings = new List<string>();
				//renderings.
				//if (Translation == null)
				// return renderings
				// else
				// renderings[0] = Translation;
				IEnumerable<string> renderings = m_terms.SelectMany(keyTerm => keyTerm.Renderings.Where(r => r != null));
				KeyTermRenderingInfo info = RenderingInfo;
				if (info != null)
					renderings = renderings.Union(info.AddlRenderings.Where(r => r != null));
				return renderings.Select(r => r.Normalize(NormalizationForm.FormD)).Distinct();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rendering info (if any) corresponding to this key term match object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public KeyTermRenderingInfo RenderingInfo
		{
			get { return m_keyTermRenderingInfo.FirstOrDefault(i => i.TermId == Term); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The primary (best) rendering for the term in the target language (equivalent to the
		/// Translation).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BestRendering
		{
			get { return Translation; }
			set
			{
				m_bestTranslation = value;
				KeyTermRenderingInfo info = RenderingInfo;
				if (info == null)
				{
					info = new KeyTermRenderingInfo(Term, m_bestTranslation);
					m_keyTermRenderingInfo.Add(info);
				}
				else
				{
					info.PreferredRendering = m_bestTranslation;
				}
				if (BestRenderingChanged != null)
					BestRenderingChanged(this);
				UpdateRenderingInfoFile();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the references of all occurences of this key term as integers in the form
		/// BBBCCCVVV.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<int> BcvOccurences
		{
			get { return m_terms.SelectMany(keyTerm => keyTerm.BcvOccurences).Distinct(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all the key terms for this match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<IKeyTerm> AllTerms
		{
			get { return m_terms; }
		}
		#endregion

		#region Private helper methods
		private void UpdateRenderingInfoFile()
		{
			if (m_keyTermRenderingInfoFile != null)
				XmlSerializationHelper.SerializeToFile(m_keyTermRenderingInfoFile, m_keyTermRenderingInfo);
		}
		#endregion
	}
}