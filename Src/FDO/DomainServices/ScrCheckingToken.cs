// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrCheckingToken.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of ITextToken to enable basic Scripture checking
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrCheckingToken : ITextToken
	{
		#region Data Members
		/// <summary></summary>
		internal protected BCVRef m_startRef;
		/// <summary></summary>
		internal protected BCVRef m_endRef;
		/// <summary></summary>
		internal protected bool m_fNoteStart = false;
		/// <summary></summary>
		internal protected bool m_fParagraphStart = true;
		/// <summary></summary>
		internal protected string m_paraStyleName = string.Empty;
		/// <summary></summary>
		internal protected string m_charStyleName = string.Empty;
		/// <summary></summary>
		internal protected string m_sText = null;
		/// <summary></summary>
		internal protected TextType m_textType = TextType.Other;
		/// <summary></summary>
		internal protected string m_icuLocale = null;
		/// <summary></summary>
		internal protected ICmObject m_object = null;
		/// <summary></summary>
		private int m_ws;
		/// <summary></summary>
		internal protected int m_flid;
		/// <summary>The offset of this token in the paragraph</summary>
		internal protected int m_paraOffset;
		/// <summary></summary>
		internal protected string m_scrRefString;
		/// <summary></summary>
		internal protected BCVRef m_missingStartRef;
		/// <summary></summary>
		internal protected BCVRef m_missingEndRef;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies this instance.
		/// </summary>
		/// <returns>A copy of this token</returns>
		/// ------------------------------------------------------------------------------------
		public ScrCheckingToken Copy()
		{
			ScrCheckingToken newToken = new ScrCheckingToken();
			newToken.m_endRef = new BCVRef(m_endRef);
			newToken.m_flid = m_flid;
			newToken.m_fNoteStart = m_fNoteStart;
			newToken.m_fParagraphStart = m_fParagraphStart;
			newToken.m_icuLocale = m_icuLocale;
			newToken.m_object = m_object;
			newToken.m_startRef = new BCVRef(m_startRef);
			newToken.m_sText = m_sText;
			newToken.m_paraStyleName = m_paraStyleName;
			newToken.m_charStyleName = m_charStyleName;
			newToken.m_textType = m_textType;
			newToken.m_ws = m_ws;
			newToken.m_paraOffset = m_paraOffset;
			newToken.m_scrRefString = m_scrRefString;
			newToken.m_missingStartRef = m_missingStartRef;
			newToken.m_missingEndRef = m_missingEndRef;
			return newToken;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a deep copy of this text token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITextToken Clone()
		{
			return Copy();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// First Scripture reference containing this token. Same as EndRef except when the
		/// token is text contained in a verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef StartRef
		{
			get { return new BCVRef(m_startRef); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Last Scripture reference containing this token. Same as StartRef except when the
		/// token is text contained in a verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef EndRef
		{
			get { return new BCVRef(m_endRef); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Flid (usually StTxtPara.Contents) from which token was created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Flid
		{
			get { return m_flid; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the object (usually an StTxtPara) from which this token was created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmObject Object
		{
			get { return m_object; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the offset in the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ParaOffset
		{
			get { return m_paraOffset; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Ws
		{
			get { return m_ws; }
			internal protected set { m_ws = value; }
		}

		#region ITextToken Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this token starts a new note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsNoteStart
		{
			get { return m_fNoteStart; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is paragraph start.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsParagraphStart
		{
			get { return m_fParagraphStart; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the paragraph style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ParaStyleName
		{
			get { return m_paraStyleName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CharStyleName
		{
			get { return m_charStyleName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text of the token. Verse and chapter numbers are NOT included in this
		/// field. NOTE: to keep checks from crashing, this will never return a null value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Text
		{
			get { return (m_sText != null) ? m_sText : string.Empty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ICU Locale of the token. This is null if this is the default locale for
		/// this text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Locale
		{
			get { return m_icuLocale; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the text contained in token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TextType TextType
		{
			get { return m_textType; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Scripture reference as a string, suitable for displaying in the UI
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ScrRefString
		{
			get
			{
				if (m_scrRefString == null)
				{
					IScripture scr = m_object.Cache.LangProject.TranslatedScriptureOA;
					m_scrRefString = BCVRef.MakeReferenceString(m_startRef, m_endRef,
						scr.ChapterVerseSepr, scr.Bridge);
				}

				return m_scrRefString;
			}
			set { m_scrRefString = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the beginning reference of a missing chapter or verse range. This
		/// property is primarily for the chapter/verse check and allows the check to set the
		/// beginning reference of a missing chapter or verse range. If the missing verse is
		/// not part of a range, then this just contains the missing verse reference and the
		/// MissingEndRef is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef MissingStartRef
		{
			get { return m_missingStartRef; }
			set { m_missingStartRef = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ending reference of a missing chapter or verse range. This
		/// property is primarily for the chapter/verse check and allows the check to set the
		/// ending reference of a missing chapter or verse range. If the missing verse is not
		/// part of a range, then this is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef MissingEndRef
		{
			get { return m_missingEndRef; }
			set { m_missingEndRef = value; }
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Text + "; " + TextType + "; " + ScrRefString;
		}
	}
}
