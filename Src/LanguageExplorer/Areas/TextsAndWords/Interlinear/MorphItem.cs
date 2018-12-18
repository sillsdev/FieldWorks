// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal struct MorphItem : IComparable
	{
		/// <summary>
		/// hvo of the morph form of an entry
		/// </summary>
		public int m_hvoMorph;
		/// <summary>
		/// typically derived from the variant component lexeme
		/// </summary>
		public int m_hvoMainEntryOfVariant;
		public int m_hvoSense;
		public int m_hvoMsa;
		public ILexEntryInflType m_inflType;
		public ILexEntryRef m_entryRef;
		public ITsString m_name;
		public string m_nameSense;
		public string m_nameMsa;

		public MorphItem(MorphItemOptions options)
			: this(options.HvoMoForm, options.HvoEntry, options.TssName, options.HvoSense, options.SenseName, options.HvoMsa, options.MsaName)
		{
			m_inflType = options.InflType;
			m_entryRef = options.EntryRef;
			if (m_entryRef != null)
			{
				var entry = IhMissingEntry.GetMainEntryOfVariant(m_entryRef);
				m_hvoMainEntryOfVariant = entry.Hvo;
			}
		}

		public MorphItem(int hvoMorph, ITsString tssName)
			: this(hvoMorph, 0, tssName)
		{
		}

		public MorphItem(int hvoMorph, int hvoMainEntryOfVariant, ITsString tssName)
			: this(hvoMorph, hvoMainEntryOfVariant, tssName, 0, null, 0, null)
		{
		}

		public MorphItem(int hvoMorph, ITsString tssName, int hvoSense, string nameSense, int hvoMsa, string nameMsa)
			: this(hvoMorph, 0, tssName, hvoSense, nameSense, hvoMsa, nameMsa)
		{
		}

		/// <summary />
		/// <param name="hvoMorph">IMoForm (e.g. wmb.MorphRA)</param>
		/// <param name="hvoMainEntryOfVariant">for variant specs, this is hvoMorph's Entry.VariantEntryRef.ComponentLexeme target, 0 otherwise</param>
		/// <param name="tssName"></param>
		/// <param name="hvoSense">ILexSense (e.g. wmb.SensaRA)</param>
		/// <param name="nameSense"></param>
		/// <param name="hvoMsa">IMoMorphSynAnalysis (e.g. wmb.MsaRA)</param>
		/// <param name="nameMsa"></param>
		public MorphItem(int hvoMorph, int hvoMainEntryOfVariant, ITsString tssName, int hvoSense, string nameSense, int hvoMsa, string nameMsa)
		{
			m_hvoMorph = hvoMorph;
			m_hvoMainEntryOfVariant = hvoMainEntryOfVariant;
			m_name = tssName;
			m_hvoSense = hvoSense;
			m_nameSense = nameSense;
			m_hvoMsa = hvoMsa;
			m_nameMsa = nameMsa;
			m_inflType = null;
			m_entryRef = null;
		}

		/// <summary>
		/// for variant relationships, return the primary entry
		/// (of which this morph is a variant). Otherwise,
		/// return the owning entry of the morph.
		/// </summary>
		public ILexEntry GetPrimaryOrOwningEntry(LcmCache cache)
		{
			var repository = cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			ILexEntry morphEntryReal;
			if (m_hvoMainEntryOfVariant != 0)
			{
				// for variant relationships, we want to allow trying to create a
				// new sense on the entry of which we are a variant.
				morphEntryReal = repository.GetObject(m_hvoMainEntryOfVariant) as ILexEntry;
			}
			else
			{
				var morph = repository.GetObject(m_hvoMorph);
				morphEntryReal = morph.Owner as ILexEntry;
			}
			return morphEntryReal;
		}

		#region IComparer Members

		/// <summary>
		/// make sure SetupCombo groups morph items according to lex name, sense,
		/// and msa names in that order. (LT-5848).
		/// </summary>
		public int Compare(object x, object y)
		{
			var miX = (MorphItem)x;
			var miY = (MorphItem)y;
			// first compare the lex and sense names.
			if (miX.m_name == null || miY.m_name == null) //handle sort under null conditions
			{
				if (miY.m_name != null)
				{
					return -1;
				}
				if (miX.m_name != null)
				{
					return 1;
				}
			}
			else
			{
				var compareLexNames = string.CompareOrdinal(miX.m_name.Text, miY.m_name.Text);
				if (compareLexNames != 0)
				{
					return compareLexNames;
				}
			}
			// otherwise if the hvo's are the same, then we want the ones with senses to be higher.
			// when m_hvoSense equals '0' we want to insert "Add New Sense" for that lexEntry,
			// following all the other senses for that lexEntry.
			if (miX.m_hvoMorph == miY.m_hvoMorph)
			{
				if (miX.m_hvoSense == 0)
				{
					return 1;
				}
				if (miY.m_hvoSense == 0)
				{
					return -1;
				}
			}
			// only compare sense names for the same morph
			if (miX.m_hvoMorph == miY.m_hvoMorph)
			{
				var compareSenseNames = string.CompareOrdinal(miX.m_nameSense, miY.m_nameSense);
				if (compareSenseNames != 0)
				{
					// if we have inflectional affix information, order them according to their order in LexEntryRef.VariantEntryTypes.
					if (miX.m_entryRef == null || miY.m_entryRef == null || miX.m_entryRef.Hvo != miY.m_entryRef.Hvo)
					{
						return compareSenseNames;
					}
					var commonVariantEntryTypesRs = miX.m_entryRef.VariantEntryTypesRS;
					if (miX.m_inflType == null || miY.m_inflType == null) //handle sort under null conditions
					{
						if (miY.m_inflType != null)
						{
							return -1;
						}
						if (miX.m_inflType != null)
						{
							return 1;
						}
					}
					else
					{
						var iX = commonVariantEntryTypesRs.IndexOf(miX.m_inflType);
						var iY = commonVariantEntryTypesRs.IndexOf(miY.m_inflType);
						if (iX > iY)
						{
							return 1;
						}
						if (iX < iY)
						{
							return -1;
						}
					}
					return compareSenseNames;
				}
				var msaCompare = string.CompareOrdinal(miX.m_nameMsa, miY.m_nameMsa);
				if (msaCompare != 0)
				{
					return msaCompare;
				}
			}
			// otherwise, try to regroup common lex morphs together.
			return miX.m_hvoMorph.CompareTo(miY.m_hvoMorph);
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			return Compare(this, obj);
		}

		#endregion
	}
}