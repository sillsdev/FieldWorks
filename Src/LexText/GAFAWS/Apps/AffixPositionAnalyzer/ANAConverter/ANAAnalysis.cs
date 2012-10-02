// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2007' company='SIL International'>
//    Copyright (c) 2007, SIL International. All Rights Reserved.
// </copyright>
//
// File: ANAAnalysis.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implementation of ANAAnalysis class.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

using SIL.WordWorks.GAFAWS;

namespace SIL.WordWorks.GAFAWS.ANAConverter
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ANAAnalysis.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal class ANAAnalysis : ANAObject
	{
		private List<ANAPrefix> m_prefixes;
		private ANAStem m_stem;
		private string m_sstem;
		private List<ANASuffix> m_suffixes;
		private string m_originalForm;
		private string m_wordCategory;

		static private char s_openRootDelimiter = '<';
		static private char s_closeRootDelimiter = '>';
		static private char s_separatorCharacter = '-';
		static private char s_categorySeparator = '=';
		static private List<Category> s_partsOfSpeech;
		static private int s_idx = 1;

		static new internal void Reset()
		{
			s_openRootDelimiter = '<';
			s_closeRootDelimiter = '>';
			s_separatorCharacter = '-';
			s_categorySeparator = '=';
			s_partsOfSpeech = null;
			s_idx = 1;
		}

		/// <summary>
		/// PartsOfSpeech.
		/// </summary>
		internal static List<Category> PartsOfSpeech
		{
			set { s_partsOfSpeech = value; }
			get { return s_partsOfSpeech; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the open root delimiter property.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal static char OpenRootDelimiter
		{
			set { s_openRootDelimiter = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the close root delimiter property.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal static char CloseRootDelimiter
		{
			set { s_closeRootDelimiter = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the open separator property.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal static char SeparatorCharacter
		{
			set { s_separatorCharacter = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the original form.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal string OriginalForm
		{
			get { return m_originalForm; }
			set { m_originalForm = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process the content from various analysis fields.
		/// </summary>
		/// <param name="type">The type of line being processed.</param>
		/// <param name="form">The form from the ANA field.</param>
		/// -----------------------------------------------------------------------------------
		internal void ProcessContent(LineType type, string form)
		{
			if (m_stem == null)
				return;	// Don't process a failure.

			string[] forms;
			char[] sep = {s_separatorCharacter};
			if (type == LineType.kCategory)
			{
				// \cat %5%N N%ADJ ADJ%N N%V VA/V=VA%V VA/V=VA%
				sep = new char[1];
				sep[0] = ' ';
				forms = TokenizeLine(form, sep);
				m_wordCategory = forms[0];
				if (form.Length == m_wordCategory.Length)
					return;
				form = form.Substring(m_wordCategory.Length);
				sep = new char[1];
				sep[0] = s_categorySeparator;
			}
			forms = TokenizeLine(form, sep);
			if (MorphemeCount != forms.Length)
			{
				// The count may not match for one of two reasons:
				// 1) The separator used to tokenize the string wasn't in the string, or
				// 2) Sentrans or Stamp didn't fix the contents of the field,
				//	when it did some Transfer operation.
				// We can't process the field in this case,
				// but we can continue the overall conversion.
				return;
			}
			int formCnt = 0;
			int i = 0;
			for (i = 0; m_prefixes != null && i < m_prefixes.Count; ++i)
				m_prefixes[i].AddContent(type, forms[formCnt++]);
			for (i = 0; i < m_stem.RootCount; ++i)
				m_stem.AddContent(type, forms[formCnt++]);
			for (i = 0; m_suffixes != null && i < m_suffixes.Count; ++i)
				m_suffixes[i].AddContent(type, forms[formCnt++]);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the count of morphemes.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal int MorphemeCount
		{
			get
			{
				int pfxCnt = (m_prefixes != null) ? m_prefixes.Count : 0;
				int sfxCnt = (m_suffixes != null) ? m_suffixes.Count : 0;
				return pfxCnt + m_stem.RootCount + sfxCnt;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="analysis">An analysis string from the \a field.</param>
		/// -----------------------------------------------------------------------------------
		internal ANAAnalysis(string analysis)
		{
			m_wordCategory = null;
			m_originalForm = null;

			if (analysis == null)
			{
				m_prefixes = null;
				m_stem = null;
				m_suffixes = null;
			}
			else
			{
				char[] seps = {s_openRootDelimiter, s_closeRootDelimiter};
				string[] morphemes = TokenizeLine(analysis, seps);
				if (morphemes.Length != 3)
					throw new ApplicationException("Incorrect delimiters.");
				m_prefixes = ANAPrefix.TokenizeAffixes(morphemes[0]);
				m_stem = new ANAStem(morphemes[1]);
				m_sstem = morphemes[1]; // For cat filter
				m_suffixes = ANASuffix.TokenizeAffixes(morphemes[2]);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Convert the analysis and its morphemes.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal void Convert()
		{
			int i = 0;

			if (m_stem == null || (m_prefixes == null && m_suffixes == null))
				return;	// Don't convert a failure or no affixes.

			// Category Filter
			if ((PartsOfSpeech != null) && (PartsOfSpeech.Count != 0))
			{
				// \cat?
				string[] catCats = {m_wordCategory};
				if (m_wordCategory != null)
					if (ContainsCat(catCats))
						goto label1;
					else
						return;
				// \a?
				string[] stemElements = m_sstem.Split(' ');
				string[] stemCats = new string[(stemElements.Length - 2)/2];
				for (i = 0; i < (stemElements.Length - 2)/2; ++i)
					stemCats[i] = new string(stemElements[(i*2)+1].ToCharArray());

				if (ContainsCat(stemCats))
					goto label1;

				return;
			}

			label1: WordRecord wr = new WordRecord();
			s_gd.WordRecords.Add(wr);
			wr.WRID = "WR" + s_idx++;
			if (m_prefixes != null)
				wr.Prefixes = new List<Affix>();
			if (m_suffixes != null)
				wr.Suffixes = new List<Affix>();

			if ((m_originalForm != null) || (m_wordCategory != null))
			{
				string xml = "<ANAInfo";
				if (m_originalForm != null)
					xml += " form=\'" + m_originalForm + "\'";
				if (m_wordCategory != null)
					xml += " category=\'" + m_wordCategory + "\'";
				xml += " />";
				wr.Other = new Other(xml);
			}

			for (i = 0; m_prefixes != null && i < m_prefixes.Count; ++i)
				m_prefixes[i].Convert();
			m_stem.Convert();
			for (i = 0; m_suffixes != null && i < m_suffixes.Count; ++i)
				m_suffixes[i].Convert();
		}

		/// <summary>
		/// ContainsCats - check to see if analysis fits cat filter.
		/// </summary>
		/// <param name="cats"></param>
		/// <returns></returns>
		private bool ContainsCat(string[] cats)
		{
			int j;
			Category cat;
			for (j = 0; j < cats.Length; ++j)
				for (int i = 0; i < PartsOfSpeech.Count; ++i)
				{
					cat = (Category)PartsOfSpeech[i];
					if ( cat.Cat == cats[j])
						return true;
				}

			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tokenize the given line with the given separators.
		/// </summary>
		/// <param name="line">The line to tokenize.</param>
		/// <param name="seps">The characters used to tokenize the given string.</param>
		/// <returns>An array of token strings.</returns>
		/// -----------------------------------------------------------------------------------
		protected string[] TokenizeLine(string line, char[] seps)
		{
			return line.Trim().Split(seps);
		}

	}
}
