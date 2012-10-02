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
	public enum TypeOfPhrase
	{
		Unknown,
		Question,
		StatementOrImperative,
	}

	public sealed class TranslatablePhrase : IComparable<TranslatablePhrase>
	{
		#region Data Members
		private readonly string m_sOrigPhrase;
		private readonly string m_sReference;
		private readonly int m_category;
		internal readonly List<IPhrasePart> m_parts = new List<IPhrasePart>();
		private readonly TypeOfPhrase m_type;
		private readonly int m_startRef;
		private readonly int m_endRef;
		private readonly int m_seqNumber;
		private readonly object[] m_additionalInfo;
		private string m_sTranslation;
		private bool m_fHasUserTranslation;
		internal static PhraseTranslationHelper s_helper;
		#endregion

		#region Constructors
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
		/// <param name="additionalInfo">Any additional info that the caller would like to keep
		/// associated with this phrase</param>
		/// ------------------------------------------------------------------------------------
		public TranslatablePhrase(string phrase, int category, string reference,
			int startRef, int endRef, int seqNumber, params object[] additionalInfo) : this(phrase)
		{
			m_category = category;
			m_sReference = reference;
			m_endRef = endRef;
			m_startRef = startRef;
			m_seqNumber = seqNumber;
			m_additionalInfo = additionalInfo;
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
		public string OriginalPhrase
		{
			get { return m_sOrigPhrase; }
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
				bool fOmitKeyTermTranslations = TranslatableParts.Any(p => p.Translation != null && p.Translation.Contains('{'));
				return s_helper.InitialPunctuationForType(TypeOfPhrase) +
					m_parts.ToString(true, " ", p => p.Translation == null || (p is KeyTermMatch && fOmitKeyTermTranslations) ?
					String.Empty : string.Format(p.Translation, KeyTermRenderings)) +
					s_helper.FinalPunctuationForType(TypeOfPhrase);
			}
			set
			{
				m_sTranslation = (value == null) ? null : value.Normalize(NormalizationForm.FormD);
				if (!string.IsNullOrEmpty(m_sTranslation))
				{
					m_fHasUserTranslation = true;
					s_helper.ProcessTranslation(this);
				}
				else
					m_fHasUserTranslation = false;
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
				}
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
			get { return m_parts.OfType<KeyTermMatch>().Select(kt => kt.Translation).ToArray(); }
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
		public int SequenceNumber
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
		public object[] AdditionalInfo
		{
			get { return m_additionalInfo; }
		}

		public TypeOfPhrase TypeOfPhrase
		{
			get { return m_type; }
		}
		#endregion

		#region Public methods (and the indexer which is really more like a property, but Tim wants it in this region)
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
		#endregion

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
			return m_sOrigPhrase.CompareTo(other.m_sOrigPhrase);
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
	}
}
