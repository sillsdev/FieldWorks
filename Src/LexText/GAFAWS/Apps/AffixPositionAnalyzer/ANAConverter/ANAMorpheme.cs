// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2007' company='SIL International'>
//    Copyright (c) 2007, SIL International. All Rights Reserved.
// </copyright>
//
// File: ANAMorpheme.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implementation of ANAMorpheme, ANAAffix, ANAPrefix, ANASuffix, and ANAStem classes.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.WordWorks.GAFAWS;

namespace SIL.WordWorks.GAFAWS.ANAConverter
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Abstract class of morphemes.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal abstract class ANAMorpheme : ANAObject
	{
		protected string m_morphname;
		protected string m_decomposition;
		protected string m_underlyingForm;
		protected string m_category;
		protected DataLayerMorpheme m_dataLayerMorpheme;
		protected Morpheme m_morpheme;
		protected MorphemeType m_type;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="morphname">The morphname of the morpheme.</param>
		/// -----------------------------------------------------------------------------------
		internal ANAMorpheme(string morphname)
		{
			m_morphname = morphname;
		}

		protected WordRecord LastRecord
		{
			get
			{
				return s_gd.WordRecords[s_gd.WordRecords.Count - 1];
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Adds content from various fields in the ANA record.
		/// </summary>
		/// <param name="type">The type of field being processed.</param>
		/// <param name="form">The individual form being added.</param>
		/// -----------------------------------------------------------------------------------
		internal virtual void AddContent(LineType type, string form)
		{
			switch (type)
			{
				case LineType.kUnderlyingForm:
				{
					m_underlyingForm = form;
					break;
				}
				case LineType.kCategory:
				{
					m_category = form;
					break;
				}
				case LineType.kDecomposition:
				{
					m_decomposition = form;
					break;
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// An abstract method used to convert a morpheme.
		/// Subclasses must override this method to do an appropriate conversion.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal virtual void Convert()
		{
			foreach (Morpheme m in s_gd.Morphemes)
			{
				if (m.MID == m_morphname)
				{
					m_morpheme = m;
					break;
				}
			}
			if (m_morpheme == null)
			{
				m_morpheme = new Morpheme(m_type, m_morphname);
				s_gd.Morphemes.Add(m_morpheme);
			}
			m_dataLayerMorpheme.MIDREF = m_morpheme.MID;
			if (m_underlyingForm != null)
			{
				string xml = "<ANAInfo underlyingForm=\'" + m_underlyingForm + "\' />";
				m_morpheme.Other = new Other(xml);
			}
			if (m_category != null
				|| m_decomposition != null)
			{
				string xml = "<ANAInfo";
				if (m_category != null)
					xml += " category=\'" + m_category + "\'";
				if (m_decomposition != null)
					xml += " decomposition=\'" + m_decomposition + "\'";
				xml += " />";
				m_dataLayerMorpheme.Other = new Other(xml);
			}

		}
	}

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// A stem morpheme.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal class ANAStem : ANAMorpheme
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="morphname">The morphname of the stem.</param>
		/// -----------------------------------------------------------------------------------
		internal ANAStem(string morphname)
			: base(morphname.Trim().Replace(" ", "_"))
		{
			m_type = MorphemeType.stem;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Convert the stem to the data layer.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal override void Convert()
		{
			LastRecord.Stem = new Stem();
			m_dataLayerMorpheme = LastRecord.Stem;
			base.Convert();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the count of roots in the stem.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal int RootCount
		{
			get { return  m_morphname.Split('_').Length/2; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Adds content from various fields in the ANA record.
		/// </summary>
		/// <param name="type">The type of field being processed.</param>
		/// <param name="form">The individual form being added.</param>
		/// -----------------------------------------------------------------------------------
		internal override void AddContent(LineType type, string form)
		{
			switch (type)
			{
				case LineType.kUnderlyingForm:
				{
					if (m_underlyingForm == null)
						m_underlyingForm = form;
					else
						m_underlyingForm += "_" + form;
					break;
				}
				case LineType.kCategory:
				{
					if (m_category == null)
						m_category = form;
					else
						m_category += "_" + form;
					break;
				}
				case LineType.kDecomposition:
				{
					if (m_decomposition == null)
						m_decomposition = form;
					else
						m_decomposition += "_" + form;
					break;
				}
			}
		}
	}

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// A prefix or suffix morpheme.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal abstract class ANAAffix : ANAMorpheme
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="morphname">The morphname of the affix.</param>
		/// -----------------------------------------------------------------------------------
		internal ANAAffix(string morphname)
			: base(morphname)
		{
		}
	}

	internal class ANAPrefix : ANAAffix
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="morphname">The morphname of the affix.</param>
		/// -----------------------------------------------------------------------------------
		internal ANAPrefix(string morphname)
			: base(morphname)
		{
			m_type = MorphemeType.prefix;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Convert the affix to the data layer.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal override void Convert()
		{
			Affix afx = new Affix();
			m_dataLayerMorpheme = afx;
			LastRecord.Prefixes.Add(afx);
			base.Convert();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tokenize the given input, and create affixes.
		/// </summary>
		/// <param name="affixes">One or more affixes to process.</param>
		/// <returns>An array of affixes that have been created.</returns>
		/// -----------------------------------------------------------------------------------
		internal static List<ANAPrefix> TokenizeAffixes(string affixes)
		{
			List<ANAPrefix> list = null;
			if (affixes.Length > 0)
			{
				list = new List<ANAPrefix>();
				string[] afxes = affixes.Trim().Split();
				for (int i = 0; i < afxes.Length; ++i)
				{
					list.Add(new ANAPrefix(afxes[i]));
				}
			}
			return list;
		}
	}

	internal class ANASuffix : ANAAffix
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="morphname">The morphname of the affix.</param>
		/// -----------------------------------------------------------------------------------
		internal ANASuffix(string morphname)
			: base(morphname)
		{
			m_type = MorphemeType.suffix;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Convert the affix to the data layer.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal override void Convert()
		{
			Affix afx = new Affix();
			LastRecord.Suffixes.Add(afx);
			m_dataLayerMorpheme = afx;
			base.Convert();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tokenize the given input, and create affixes.
		/// </summary>
		/// <param name="affixes">One or more affixes to process.</param>
		/// <returns>An array of affixes that have been created.</returns>
		/// -----------------------------------------------------------------------------------
		internal static List<ANASuffix> TokenizeAffixes(string affixes)
		{
			List<ANASuffix> list = null;
			if (affixes.Length > 0)
			{
				list = new List<ANASuffix>();
				string[] afxes = affixes.Trim().Split();
				for (int i = 0; i < afxes.Length; ++i)
				{
					list.Add(new ANASuffix(afxes[i]));
				}
			}
			return list;
		}
	}

}
