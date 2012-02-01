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
// File: TranslatablePhrase.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SIL.Utils;

namespace SILUBS.PhraseTranslationHelper
{
	public enum TypeOfPhrase : byte
	{
		Unknown,
		Question,
		StatementOrImperative,
		NoEnglishVersion,
	}

	public sealed class TranslatablePhrase : IComparable<TranslatablePhrase>
	{
		#region Data Members
		private readonly string m_sReference;
		private readonly string m_sOrigPhrase;
		private string m_sModifiedPhrase;
		private HashSet<string> m_alternateForms;
		private bool m_fExclude;
		private readonly int m_category;
		internal readonly List<IPhrasePart> m_parts = new List<IPhrasePart>();
		private readonly TypeOfPhrase m_type;
		private readonly int m_startRef;
		private readonly int m_endRef;
		private readonly float m_seqNumber;
		private readonly Question m_questionInfo;
		private string m_sTranslation;
		private bool m_fHasUserTranslation;
		private bool m_allTermsMatch;
		internal static PhraseTranslationHelper s_helper;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TranslatablePhrase"/> class.
		/// </summary>
		/// <param name="questionlInfo">Information about the original question</param>
		/// <param name="category">The category (e.g. Overview vs. Detail question).</param>
		/// <param name="reference">The displayable "reference" that tells what this phrase
		/// pertains to.</param>
		/// <param name="startRef">The (numeric) start reference, for sorting and comparing.</param>
		/// <param name="endRef">The (numeric) end reference, for sorting and comparing.</param>
		/// <param name="seqNumber">The sequence number (used to sort and/or uniquely identify
		/// a phrase within a particular category and reference).</param>
		/// ------------------------------------------------------------------------------------
		public TranslatablePhrase(Question questionlInfo, int category, string reference,
			int startRef, int endRef, float seqNumber)
			: this(questionlInfo.Text, category, reference, startRef, endRef, seqNumber)
		{
			m_questionInfo = questionlInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TranslatablePhrase"/> class.
		/// </summary>
		/// <param name="phrase">The original phrase.</param>
		/// <param name="category">The category (e.g. Overview vs. Detail question).</param>
		/// <param name="reference">The displayable "reference" that tells what this phrase
		/// pertains to.</param>
		/// <param name="startRef">The (numeric) start reference, for sorting and comparing.</param>
		/// <param name="endRef">The (numeric) end reference, for sorting and comparing.</param>
		/// <param name="seqNumber">The sequence number (used to sort and/or uniquely identify
		/// a phrase within a particular category and reference).</param>
		/// ------------------------------------------------------------------------------------
		public TranslatablePhrase(string phrase, int category, string reference,
			int startRef, int endRef, float seqNumber) : this(phrase)
		{
			m_category = category;
			m_sReference = reference;
			m_endRef = endRef;
			m_startRef = startRef;
			m_seqNumber = seqNumber;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TranslatablePhrase"/> class.
		/// </summary>
		/// <param name="phrase">The original phrase.</param>
		/// ------------------------------------------------------------------------------------
		public TranslatablePhrase(string phrase)
		{
			m_sOrigPhrase = phrase.Normalize(NormalizationForm.FormD);
			if (!String.IsNullOrEmpty(m_sOrigPhrase))
			{
				switch (m_sOrigPhrase[m_sOrigPhrase.Length - 1])
				{
					case '?': m_type = TypeOfPhrase.Question; break;
					case '.': m_type = TypeOfPhrase.StatementOrImperative; break;
					default: m_type = TypeOfPhrase.Unknown; break;
				}
				if (m_type == TypeOfPhrase.Unknown && m_sOrigPhrase.StartsWith(Question.kGuidPrefix))
				{
					m_sOrigPhrase = string.Empty;
					m_type = TypeOfPhrase.NoEnglishVersion;
				}
			}
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the category of this phrase (used to group phrases having the same reference).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Category
		{
			get { return m_category; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the "reference" that tells what this phrase pertains to.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Reference
		{
			get { return m_sReference; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the original phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string OriginalPhrase
		{
			get { return m_sOrigPhrase; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the phrase to use for processing & comparison purposes (either the original\
		/// phrase or a modified form of it).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string PhraseInUse
		{
			get { return m_sModifiedPhrase ?? m_sOrigPhrase; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the phrase as it is being presented to the user (the original phrase, a
		/// modified form of it, or a special UI string indicating a user-added question with
		/// no English equivalent).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string PhraseToDisplayInUI
		{
			get
			{
				return (m_type == TypeOfPhrase.NoEnglishVersion) ? Properties.Resources.kstidUserAddedEmptyPhrase :
					m_sModifiedPhrase ?? m_sOrigPhrase;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the phrase to use when saving or attempting to look up a specific translation
		/// (either the original phrase, a modified form of it, or the underlying GUID-based key
		/// to use for user-added questions that have no english version).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string PhraseKey
		{
			get
			{
				return (m_type == TypeOfPhrase.NoEnglishVersion) ? QuestionInfo.Text :
					m_sModifiedPhrase ?? m_sOrigPhrase;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a modified version of the phrase to use in place of the original.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ModifiedPhrase
		{
			get { return m_sModifiedPhrase; }
			internal set
			{
				m_sModifiedPhrase = value.Normalize(NormalizationForm.FormD);
				if (IsUserAdded)
					QuestionInfo.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets information about a question that is inserted ahead of this phrase in
		/// the list (when sorted in text order).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Question InsertedPhraseBefore { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets information about a question that is added after this phrase in
		/// the list (when sorted in text order).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Question AddedPhraseAfter { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this phrase is excluded (not available for
		/// translation).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsExcluded
		{
			get { return m_fExclude; }
			internal set { m_fExclude = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is customized (i.e, modified, deleted
		/// but not user-supplied).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IsCustomized
		{
			get { return (m_fExclude || m_sModifiedPhrase != null) && !IsUserAdded; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is user-supplied.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IsUserAdded
		{
			get
			{
				return SequenceNumber != Math.Round(SequenceNumber);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the user translation with any initial and final punctuation removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UserTransSansOuterPunctuation
		{
			get
			{
				Debug.Assert(m_fHasUserTranslation);

				StringBuilder bldr = new StringBuilder(m_sTranslation);
				while (bldr.Length > 0 && Char.IsPunctuation(bldr[0]))
					bldr.Remove(0, 1);
				while (bldr.Length > 0 && Char.IsPunctuation(bldr[bldr.Length - 1]))
					bldr.Length--;
				return bldr.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Translation
		{
			get
			{
				if (m_fHasUserTranslation)
					return m_sTranslation;
				if (!string.IsNullOrEmpty(m_sTranslation))
					return String.Format(m_sTranslation, KeyTermRenderings);
				return s_helper.InitialPunctuationForType(TypeOfPhrase) +
					m_parts.ToString(true, " ", p => p.GetBestRenderingInContext(this)) +
					s_helper.FinalPunctuationForType(TypeOfPhrase);
			}
			set
			{
				if (IsExcluded)
					throw new InvalidOperationException("Translation can not be set for an excluded phrase.");
				m_fHasUserTranslation = !string.IsNullOrEmpty(value);
				SetTranslationInternal(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the provisional translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void SetProvisionalTranslation(string value)
		{
			m_sTranslation = value;
			m_fHasUserTranslation = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this phrase has a translation that was
		/// supplied by the user).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasUserTranslation
		{
			get { return m_fHasUserTranslation; }
			set
			{
				if (value)
					Translation = Translation; // This looks weird, but we want the side effects.
				else
				{
					m_sTranslation = null;
					m_fHasUserTranslation = false;

					Part firstPart = TranslatableParts.FirstOrDefault();
					if (firstPart != null)
					{
						foreach (TranslatablePhrase similarPhrase in firstPart.OwningPhrases.Where(phrase => phrase.HasUserTranslation && phrase.PartPatternMatches(this)))
						{
							if (similarPhrase.PhraseInUse == PhraseInUse)
							{
								m_sTranslation = similarPhrase.Translation;
								return;
							}
							if (similarPhrase.m_allTermsMatch)
							{
								SetProvisionalTranslation(similarPhrase.GetTranslationTemplate());
								return;
							}
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the user-supplied translation contains a known
		/// rendering for each of the key terms in the original phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllTermsMatch
		{
			get
			{
				Debug.Assert(m_fHasUserTranslation);
				return m_allTermsMatch;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string that shows the phrase broken into parts (for debugging purposes).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Parts
		{
			get
			{
				return m_parts.ToString(" | ", (part, bldr) =>
				{
					bldr.Append(part);
					bldr.Append(" (");
					bldr.Append(part.DebugInfo);
					bldr.Append(")");
				});
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the translatable Parts (i.e., the parts that are not Key Terms or leading/
		/// trailing punctuation).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<Part> TranslatableParts
		{
			get { return m_parts.OfType<Part>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an an array of the key term renderings (i.e., tranlsations), ordered by their
		/// occurrence in the phrase.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object[] KeyTermRenderings
		{
			get
			{
				object[] retArray = new object[m_parts.OfType<KeyTermMatch>().Count()];
				int i = 0;
				foreach (KeyTermMatch kt in m_parts.OfType<KeyTermMatch>())
					retArray[i++] = kt.GetBestRenderingInContext(this);
				return retArray;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a numeric representation of the start reference in the form BBBCCCVVV.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int StartRef
		{
			get { return m_startRef; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a numeric representation of the end reference in the form BBBCCCVVV.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int EndRef
		{
			get { return m_endRef; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sequence number of this phrase (uniquely identifies this phrase within a
		/// given category and for a particular reference).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public float SequenceNumber
		{
			get { return m_seqNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the translated name of the requested category; if not translated, use the
		/// English name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CategoryName
		{
			get { return s_helper.GetCategoryName(Category); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets any additional information the creator of this phrase wanted to associate with
		/// it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Question QuestionInfo
		{
			get { return m_questionInfo; }
		}

		public TypeOfPhrase TypeOfPhrase
		{
			get { return (IsUserAdded && m_sModifiedPhrase == string.Empty) ? TypeOfPhrase.NoEnglishVersion : m_type; }
		}

		public IEnumerable<string> AlternateForms
		{
			get { return m_alternateForms; }
		}
		#endregion

		#region Public/internal methods (and the indexer which is really more like a property, but Tim wants it in this region)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Reference + "-" + PhraseKey;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all the parts, including the key terms (maybe never needed?).
		/// </summary>
		/// <remarks>This is a method rather than a property to prevent it from being displayed
		/// in the data grid.</remarks>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<IPhrasePart> GetParts()
		{
			return m_parts;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the part at the specified <paramref name="index"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IPhrasePart this[int index]
		{
			get { return m_parts[index]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two phrases based on the following:
		/// 1) Compare the translatable parts with the fewest number of owning phrases. The
		/// phrase whose least prolific part has the most owning phrases sorts first.
		/// 2) Fewest translatable parts
		/// 3) Translatable part with the maximum number of owning phrases.
		/// 4) Reference (alphabetically)
		/// 5) Alphabetically by original phrase
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings:
		/// Value
		/// Meaning
		/// Less than zero
		/// This object is less than the <paramref name="other"/> parameter.
		/// Zero
		/// This object is equal to <paramref name="other"/>.
		/// Greater than zero
		/// This object is greater than <paramref name="other"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int CompareTo(TranslatablePhrase other)
		{
			int compare;
			// 1)
			// ENHANCE: Idea for a possible future optimization: 	compare = (TranslatableParts.Any() ? (-1) : m_parts[0] ) + (other.TranslatableParts.Any() ? 1 : -2);
			if (!other.TranslatableParts.Any())
				return TranslatableParts.Any() ? -1 : 0;
			if (!TranslatableParts.Any())
				return 1;
			compare = other.TranslatableParts.Min(p => p.OwningPhrases.Count()) * 100 / other.TranslatableParts.Count() - TranslatableParts.Min(p => p.OwningPhrases.Count()) * 100 / TranslatableParts.Count();
			if (compare != 0)
				return compare;
			// 2)
			//compare = TranslatableParts.Count() - other.TranslatableParts.Count();
			//if (compare != 0)
			//    return compare;
			// 3)
			compare = other.TranslatableParts.Max(p => p.OwningPhrases.Count()) - TranslatableParts.Max(p => p.OwningPhrases.Count());
			if (compare != 0)
				return compare;
			// 4)
			compare = (m_sReference == null) ? (other.m_sReference == null? 0 : -1) : m_sReference.CompareTo(other.m_sReference);
			if (compare != 0)
				return compare;
			// 5)
			return PhraseInUse.CompareTo(other.PhraseInUse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether this phrase's parts matches those of the given phrase. A "match"
		/// is when the Translatable Parts are in the exact same sequence and the number and
		/// sequence of key terms is also the same (but not necessarily the same key terms).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool PartPatternMatches(TranslatablePhrase tp)
		{
			if (m_parts.Count != tp.m_parts.Count)
				return false;
			for (int i = 0; i < m_parts.Count; i++)
			{
				if ((m_parts[i] as Part) != tp.m_parts[i] as Part)
					return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether this phrase matches the criteria specified by the key term
		/// filter option.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool MatchesKeyTermFilter(PhraseTranslationHelper.KeyTermFilterType ktFilter)
		{
			switch (ktFilter)
			{
				case PhraseTranslationHelper.KeyTermFilterType.WithRenderings:
					return m_parts.OfType<KeyTermMatch>().All(kt => !string.IsNullOrEmpty(kt.Translation));
				case PhraseTranslationHelper.KeyTermFilterType.WithoutRenderings:
					bool temp = m_parts.OfType<KeyTermMatch>().Any(kt => string.IsNullOrEmpty(kt.Translation));
					return temp;
				default: // PhraseTranslationHelper.KeyTermFilterType.All
					return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the translation template with placeholders for each of the key terms for which
		/// a matching rendering is found in the translation. As a side-effect,this also sets
		/// m_allTermsMatch.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string GetTranslationTemplate()
		{
			Debug.Assert(m_fHasUserTranslation);
			int iKeyTerm = 0;
			string translation = Translation;
			m_allTermsMatch = true;
			foreach (KeyTermMatch term in GetParts().Where(p => p is KeyTermMatch))
			{
				int ich = -1;
				foreach (string ktTrans in term.Renderings.OrderBy(r => r, new StrLengthComparer(false)))
				{
					ich = translation.IndexOf(ktTrans, StringComparison.Ordinal);
					if (ich >= 0)
					{
						translation = translation.Remove(ich, ktTrans.Length);
						translation = translation.Insert(ich, "{" + iKeyTerm++ + "}");
						break;
					}
				}
				if (ich == -1)
					m_allTermsMatch = false;
			}
			return translation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the term rendering (from the known ones in the renderingInfo) in use in
		/// the current translation.
		/// </summary>
		/// <param name="renderingInfo">The information about a single occurrence of a key
		/// biblical term and its rendering in a string in the target language.</param>
		/// <returns>An object that indicates where in the translation string the match was
		/// found (offset and length)</returns>
		/// ------------------------------------------------------------------------------------
		public SubstringDescriptor FindTermRenderingInUse(ITermRenderingInfo renderingInfo)
		{
			// This will almost always be 0, but if a term occurs more than once, this
			// will be the character offset following the occurrence of the rendering of
			// the preceding term in the translation.
			int ichStart = renderingInfo.EndOffsetOfRenderingOfPreviousOccurrenceOfThisTerm;
			int indexOfMatch = Int32.MaxValue;
			int lengthOfMatch = 0;
			foreach (string rendering in renderingInfo.Renderings)
			{
				int ich = Translation.IndexOf(rendering, ichStart, StringComparison.Ordinal);
				if (ich >= 0 && (ich < indexOfMatch || (ich == indexOfMatch && rendering.Length > lengthOfMatch)))
				{
					// Found an earlier or longer match.
					indexOfMatch = ich;
					lengthOfMatch = rendering.Length;
				}
			}
			if (lengthOfMatch == 0)
				return null;

			return new SubstringDescriptor(indexOfMatch, lengthOfMatch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces the key term.
		/// </summary>
		/// <param name="sd">object that indicates what part of the translation represents the
		/// original key term rendering to replace.</param>
		/// <param name="newRendering">To string.</param>
		/// ------------------------------------------------------------------------------------
		internal void ReplaceKeyTermRendering(SubstringDescriptor sd, string newRendering)
		{
			if (sd == null)
			{
				// Caller probably couldn't find an existing rendering to replace,
				// so we'll just stick it at the end.
				sd = new SubstringDescriptor(Translation.Length, 0);
			}
			SetTranslationInternal(Translation.Remove(sd.Offset, sd.Length).Insert(sd.Offset, newRendering));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an alternate form of the phrase/question.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void AddAlternateForm(string form)
		{
			if (m_alternateForms == null)
				m_alternateForms = new HashSet<string>();
			m_alternateForms.Add(form);
		}
		#endregion

		#region Private Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the translation and processes it if it is a user translation.
		/// </summary>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		private void SetTranslationInternal(string value)
		{
			m_sTranslation = (value == null) ? null : value.Normalize(NormalizationForm.FormD);
			if (m_fHasUserTranslation && m_type != TypeOfPhrase.NoEnglishVersion)
			{
				m_allTermsMatch = false; // This will usually get updated in ProcessTranslation
				s_helper.ProcessTranslation(this);
			}

		}
		#endregion
	}
}
