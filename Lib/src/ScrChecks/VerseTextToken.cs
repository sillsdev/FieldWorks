// ---------------------------------------------------------------------------------------------
// Copyright (c) 2009-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TextTokenSkipChapVerse.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// -------------------------------------------------------------------------------------------
	/// <summary>
	/// The purpose of this class is to wrap the ITextTokens for the checks that are concerned
	/// with verse text only in order to be able to deal with situations in which the
	/// IsParagraphStart should be true but the tokenizer has set it to false. That happens for
	/// tokens containing normal text that appears at the beginning of a paragraph but is
	/// preceded in the paragraph by a chapter or verse number (or a combination of these).
	/// If either of these token types exist at the beginning of a paragraph before the actual
	/// body text, then we need to override the IsParagraphStart property to return true for
	/// the first body text token. Where this was needed, it would have been great to just set
	/// that property to true for the base token, but ITextToken doesn't define a setting for
	/// that property and someone with whom I consulted on this, thought this solution may be a
	/// better idea than to modify the interface to require one. --DDO (May 29, 2009 - TE-8050).
	/// </summary>
	/// -------------------------------------------------------------------------------------------
	public class VerseTextToken : ITextToken
	{
		private ITextToken m_token;
		private bool m_fIsParagraphStart;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the base token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ITextToken Token
		{
			get { return m_token; }
			set
			{
				ITextToken prevToken = m_token;
				m_token = value;

				if (prevToken != null)
				{
					if (prevToken.TextType == TextType.ChapterNumber ||
						prevToken.TextType == TextType.VerseNumber)
					{
						m_fIsParagraphStart = (prevToken.IsParagraphStart || m_fIsParagraphStart);
					}
					else
						m_fIsParagraphStart = false;
				}
			}
		}

		#region ITextToken Members
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public bool IsParagraphStart
		{
			get
			{
				return (m_fIsParagraphStart || m_token.IsParagraphStart);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public string CharStyleName
		{
			get { return m_token.CharStyleName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public bool IsNoteStart
		{
			get { return m_token.IsNoteStart; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public string Locale
		{
			get { return m_token.Locale; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef MissingEndRef
		{
			get { return m_token.MissingEndRef; }
			set { m_token.MissingEndRef = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef MissingStartRef
		{
			get { return m_token.MissingStartRef; }
			set { m_token.MissingStartRef = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public string ParaStyleName
		{
			get { return m_token.ParaStyleName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public string ScrRefString
		{
			get { return m_token.ScrRefString; }
			set { m_token.ScrRefString = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public string Text
		{
			get { return m_token.Text; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public TextType TextType
		{
			get { return m_token.TextType; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public ITextToken Clone()
		{
			VerseTextToken verseToken = new VerseTextToken();
			verseToken.m_fIsParagraphStart = m_fIsParagraphStart;
			verseToken.m_token = m_token;
			return verseToken;
		}
		#endregion
	}
}
