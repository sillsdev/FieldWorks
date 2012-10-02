// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrVerse.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;
using System.Collections.Generic;

namespace SIL.FieldWorks.FDO.Scripture
{
	#region ScrVerseSet enumerator
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents the set of verses within a paragraph of Scripture.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrVerseSet : IEnumerator, IEnumerable
	{
		private ScrTxtPara m_para;
		private ScrTxtPara m_prevPara = null;
		private ParaNodeMap m_address;
		private int m_ich = 0;	// current index into the paragraph
		private int m_ichVerseStart = 0;
		private int m_ichTextStart = 0;
		private BCVRef m_startRef;
		private BCVRef m_endRef;
		private ITsString m_tssParaContents = null;
		private int m_paraLength = 0;
		private bool m_inChapterNum = false;
		private bool m_inVerseNum = false;
		private bool m_isCompletePara = false;
		//private bool m_isEmptyPara = false;
		private bool m_isStanzaBreak = false;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrVerseSet"/> class, for the given
		/// paragraph.
		/// </summary>
		/// <param name="para">given paragraph</param>
		/// -----------------------------------------------------------------------------------
		public ScrVerseSet(ScrTxtPara para)
		{
			if(para == null)
				throw new ArgumentNullException("para");

			m_para = para;
			m_address = new ParaNodeMap(para);
			m_para.GetBCVRefAtPosition(0, out m_startRef, out m_endRef);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the TS String contents for the paragraph. This is virtual so the
		/// back translation version can get the BT text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected virtual ITsString ParaTssContent
		{
			get
			{
				return m_para.Contents.UnderlyingTsString;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Paragraph of content
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected ScrTxtPara Para
		{
			get
			{
				return m_para;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether this is the first time that the iterator was in an empty stanza break.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private bool FirstTimeAtStanzaBreak
		{
			get
			{
				if (m_paraLength > 0)
					return false; // not an empty paragraph

				return (m_prevPara == null || m_prevPara.Hvo != m_para.Hvo) && m_isStanzaBreak;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initialize Paragraph Contents
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeParaContents()
		{
			if (m_tssParaContents == null)
			{
				m_tssParaContents = ParaTssContent;
				m_paraLength = m_tssParaContents.Length;
			}
			else if (!m_tssParaContents.Equals(ParaTssContent))
				throw new InvalidOperationException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current ScrVerse in the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object Current
		{
			get
			{
				int ichEnd = UpdateEndOfVerse(); // m_ich;

				if (m_ich > m_paraLength)
					throw new InvalidOperationException();

				// Get the substring of the paragraph that has the verse text, including the
				// verse number.
				ITsStrBldr bldr = m_tssParaContents.GetBldr();
				bldr.Replace(0, m_ichVerseStart, string.Empty, null);
				bldr.Replace(ichEnd - m_ichVerseStart, bldr.Length, string.Empty, null);
				ScrVerse curVerse = new ScrVerse(m_startRef, m_endRef, bldr.GetString(),
					m_address, m_para.Hvo, ((StTxtPara)m_para).OwnerHVO, m_ichVerseStart, m_ichTextStart,
					m_inChapterNum, m_inVerseNum, m_isCompletePara, m_isStanzaBreak);
				return curVerse;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the end of verse to include any adjacent, duplicate verse numbers.
		/// For any adjacent and duplicate verses in the paragraph, we want to include them in
		/// the text of the same ScrVerse.(TE-7137)
		/// </summary>
		/// <returns>ich for the end of the ScrVerse updated (perhaps) to include the verse
		/// number and text of duplicate and adjacent verses</returns>
		/// ------------------------------------------------------------------------------------
		private int UpdateEndOfVerse()
		{
			if (m_inChapterNum)
				return m_ich;

			bool fFoundChapterNumber;
			int iRun = FindNextVerseRun(m_ich, out fFoundChapterNumber);

			// If we found another chapter or verse number run...
			if (iRun > 0)
			{
				if (fFoundChapterNumber)
				{
					m_ich = m_tssParaContents.get_LimOfRun(iRun - 1); // update the end of the verse
					return m_ich;
				}

				// Scan through the remainder of the paragraph (or until a verse number run
				// is not found, in which case, the iRun will be set to -1)
				for (; iRun > 0 && iRun < m_tssParaContents.RunCount; )
				{
					int verseStart, verseEnd;
					ScrReference.VerseToInt(m_tssParaContents.get_RunText(iRun), out verseStart,
						out verseEnd);

					// If the starting reference of the next verse number run is the same as the
					// current starting reference...
					if (verseStart == m_startRef.Verse)
					{
						// we have a duplicate verse and we need to find either the next verse number
						// run or the end of the paragraph to get the remainder of the ScrVerse.
						iRun = FindNextVerseRun(m_tssParaContents.get_LimOfRun(iRun), out fFoundChapterNumber);
						m_ich = iRun < 0 ? m_tssParaContents.Text.Length :
							m_tssParaContents.get_LimOfRun(iRun - 1); // update the end of the verse
					}
					else
					{
						m_ich = m_tssParaContents.get_MinOfRun(iRun);
						break; // no more duplicate, adjacent verses. We're finished scanning the para.
					}
				}
			}

			// No more verse numbers found. Set end of ScrVerse if we are not in a chapter number run.
			else
			{
				m_ich = m_tssParaContents.Text != null ? m_tssParaContents.Text.Length : 0;
			}

			return m_ich;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next verse number run.
		/// </summary>
		/// <param name="FromIch">The character index from which to scan in the ITsString.</param>
		/// <param name="fFoundChapterNumber">[out] if set to <c>true</c> the returned run index is
		/// a chapter number run.</param>
		/// <returns>
		/// index of the run containing the next verse number, or -1 if the paragraph
		/// has no more verse number runs after
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private int FindNextVerseRun(int FromIch, out bool fFoundChapterNumber)
		{
			fFoundChapterNumber = false;
			if (m_tssParaContents.Text == null || FromIch == m_tssParaContents.Text.Length)
				return -1; // we're at the end of the paragraph

			int iRun = m_tssParaContents.get_RunAt(FromIch);

			// Scan through the paragraph until we find another chapter or verse number run.
			for (; iRun < m_tssParaContents.RunCount; iRun++)
			{
				string styleName = m_tssParaContents.get_Properties(iRun).GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle);

				if (styleName == ScrStyleNames.VerseNumber)
					return iRun;

				else if (styleName == ScrStyleNames.ChapterNumber)
				{
					fFoundChapterNumber = true;
					return iRun;
				}
			}

			return -1;
		}

		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Advances the enumerator to the next verse in the paragraph.
		/// </summary>
		/// <returns>True if we successfully moved to the next ScrVerse; False if we reached
		/// the end of the paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		public bool MoveNext()
		{
			InitializeParaContents();

			if (m_ich > m_paraLength)
				return false;

			m_ichVerseStart = m_ichTextStart = m_ich;
			TsRunInfo tsi;
			ITsTextProps ttpRun;
			string sPara = m_tssParaContents.Text;
			int nChapter = -1;  // This is used to see if we found a chapter later.

			m_inVerseNum = false;
			m_inChapterNum = false;
			while (m_ich < m_paraLength)
			{
				ttpRun = m_tssParaContents.FetchRunInfoAt(m_ich, out tsi);

				// If this run is our verse number style
				if (StStyle.IsStyle(ttpRun, ScrStyleNames.VerseNumber))
				{
					// If there is already a verse in process, a new verse number run will terminate it.
					if (m_ichVerseStart != m_ich)
						break;

					// Assume the whole run is the verse number
					string sVerseNum = sPara.Substring(m_ich, tsi.ichLim - tsi.ichMin);
					int nVerseStart, nVerseEnd;
					ScrReference.VerseToInt(sVerseNum, out nVerseStart, out nVerseEnd);
					m_startRef.Verse = nVerseStart;
					m_endRef.Verse = nVerseEnd;
					m_ichVerseStart = m_ich; //set VerseStart at beg of verse number
					m_ich += sVerseNum.Length;
					m_ichTextStart = m_ich;
					m_inVerseNum = true;

				}
				// If this run is our chapter number style
				else if (StStyle.IsStyle(ttpRun, ScrStyleNames.ChapterNumber))
				{
					// If there is already a verse being processed, then the chapter number
					// run will end it
					if (m_ichVerseStart != m_ich)
						break;

					try
					{
						// Assume the whole run is the chapter number
						string sChapterNum = sPara.Substring(m_ich, tsi.ichLim - tsi.ichMin);
						nChapter = ScrReference.ChapterToInt(sChapterNum);
						m_startRef.Chapter = m_endRef.Chapter = nChapter;
						// Set the verse number to 1, since the first verse number after a
						// chapter is optional. If we happen to get a verse number in the
						// next run, this '1' will be overridden (though it will probably
						// still be a 1).
						m_startRef.Verse = m_endRef.Verse = 1;
						m_ichVerseStart = m_ich; //set VerseStart at beg of chapter number
						m_ich += sChapterNum.Length;
						m_ichTextStart = m_ich;
						m_inChapterNum = true;
					}
					catch (ArgumentException)
					{
						// ignore runs with invalid Chapter numbers
						m_ich += tsi.ichLim - tsi.ichMin;
					}
				}

				else // Process a text run.
				{
					// If it comes after a chapter number, then just return the
					// chapter number without adding the text.
					if ( nChapter > 0 )
						break;

					// skip to the next run
					m_ich += tsi.ichLim - tsi.ichMin;
				}
			}

			// determine if this verse is a complete paragraph, an empty para and/or a stanza break.
			m_isCompletePara = (m_ichVerseStart == 0 && m_ich == m_paraLength);
			if (string.IsNullOrEmpty(sPara))
			{
				//m_isEmptyPara = true;
				m_isStanzaBreak = string.Equals(ScrStyleNames.StanzaBreak,
					ScrStyleNames.GetStyleName(m_para.Hvo, m_para.Cache));
			}

			try
			{
				return (m_ich > m_ichVerseStart) || FirstTimeAtStanzaBreak;
			}
			finally
			{
				// Update the previous paragraph for the next time (but we do the update
				// in a 'finally' so that we can compare the current to the previous for
				// the return value).
				m_prevPara = m_para;
			}
		}

		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the enumerator to its initial position, which is before the first verse in
		/// the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Reset()
		{
			InitializeParaContents();
			m_ich = m_ichVerseStart = 0;
		}

		#region IEnumerable Members
		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an enumerator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerator GetEnumerator()
		{
			return this;
		}

		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents the set of verses within a BT paragraph of Scripture.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrVerseSetBT : ScrVerseSet
	{
		private int m_BackTranslationWS;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a Verse iterator from the back translation paragraph
		/// </summary>
		/// <param name="para">The paragraph with the back translation</param>
		/// <param name="BTWritingSystem">The writing system of the back translation</param>
		/// ------------------------------------------------------------------------------------
		public ScrVerseSetBT(ScrTxtPara para, int BTWritingSystem) : base(para)
		{
			m_BackTranslationWS = BTWritingSystem;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// How we get the appropriate content
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override ITsString ParaTssContent
		{
			get
			{
				ICmTranslation trans = Para.GetOrCreateBT();
				return trans.Translation.GetAlternative(m_BackTranslationWS).UnderlyingTsString;
			}
		}
	}

	#endregion

	#region ScrVerseList class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a list of verses within a paragraph.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrVerseList : List<ScrVerse>
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrVerseList(ScrTxtPara para)
		{
			foreach (ScrVerse verse in new ScrVerseSet(para))
				this.Add(verse);
		}
	}

	#endregion

	#region ScrVerse
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a verse of Scripture (as much as is contained wholly within a single
	/// paragraph) or a paragraph in a Scripture title, section head or intro paragraph.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrVerse
	{
		#region Member Data
		// member variables
		private BCVRef m_startRef;
		private BCVRef m_endRef;
		private ITsString m_text;
		private ParaNodeMap m_address;
		private int m_hvoPara;
		private int m_hvoParaOwner;
		private int m_ichMinVerse;
		private int m_ichMinText;
		private bool m_chapterNumberRun;
		private bool m_verseNumberRun;
		private bool m_completePara;
		private bool m_isStanzaBreak;
		#endregion

		#region Construction
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrVerse"/> class, when we don't
		/// care about the complete para property
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ScrVerse(BCVRef start, BCVRef end, ITsString text, ParaNodeMap map,
			int hvoPara, int hvoParaOwner, int ichMinVerse, int ichMinText,
			bool isChapterNumberRun, bool isVerseNumberRun) :
			this(start, end, text, map, hvoPara, hvoParaOwner, ichMinVerse, ichMinText,
			isChapterNumberRun, isVerseNumberRun, false, false)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrVerse"/> class for an empty paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ScrVerse(BCVRef start, BCVRef end, ITsString text, ParaNodeMap map,
			int hvoPara, int hvoParaOwner, bool isStanzaBreak) :
			this(start, end, text, map, hvoPara, hvoParaOwner, 0, 0, false, false, true, isStanzaBreak)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrVerse"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ScrVerse(BCVRef start, BCVRef end, ITsString text, ParaNodeMap map,
			int hvoPara, int hvoParaOwner, int ichMinVerse, int ichMinText,
			bool isChapterNumberRun, bool isVerseNumberRun, bool isCompletePara, bool isStanzaBreak)
		{
			m_startRef = new BCVRef(start);
			m_endRef = new BCVRef(end);
			m_text = text;
			m_address = map;
			m_hvoPara = hvoPara;
			m_hvoParaOwner = hvoParaOwner;
			m_ichMinVerse = ichMinVerse;
			m_ichMinText = ichMinText;
			m_chapterNumberRun = isChapterNumberRun;
			m_verseNumberRun = isVerseNumberRun;
			m_completePara = isCompletePara;
			m_isStanzaBreak = isStanzaBreak;
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this instance.
		/// This is a shallow copy, using references to this ScrVerse's owned objects.
		/// </summary>
		/// <returns>a new instance of this ScrVerse</returns>
		/// ------------------------------------------------------------------------------------
		public ScrVerse Clone()
		{
			return new ScrVerse(this.StartRef, this.EndRef, this.Text, this.ParaNodeMap,
				this.HvoPara, this.HvoParaOwner, this.m_ichMinVerse, this.m_ichMinText,
				this.m_chapterNumberRun, this.VerseNumberRun, this.IsCompleteParagraph,
				this.IsStanzaBreak);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ParaNodeMap for this ScrVerse
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ParaNodeMap ParaNodeMap
		{
			get { return m_address; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start reference for the verse
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef StartRef
		{
			get { return new BCVRef(m_startRef); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the end reference for the verse
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef EndRef
		{
			get { return new BCVRef(m_endRef); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text of the verse as a TsString.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString Text
		{
			get { return m_text; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the length of the text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int TextLength
		{
			get { return (Text != null) ? Text.Length : 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the paragraph containing the verse
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HvoPara
		{
			get { return m_hvoPara; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the StText containing the verse's paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HvoParaOwner
		{
			get { return m_hvoParaOwner; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character index where the verse number starts in the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int VerseStartIndex
		{
			get { return m_ichMinVerse; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character index where the verse text starts in the paragraph - after
		/// the verse number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int TextStartIndex
		{
			get { return m_ichMinText; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an indicator to tell if this ScrVerse is for a chapter number run. It will be
		/// false for verse number runs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ChapterNumberRun
		{
			get { return m_chapterNumberRun; }
			set { m_chapterNumberRun = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an indicator to tell if this ScrVerse is for a verse number run. It will be
		/// false for chapter number runs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool VerseNumberRun
		{
			get { return m_verseNumberRun; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance has verse number run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasVerseNumberRun
		{
			get { return VerseStartIndex != TextStartIndex; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the indicator that tells if the ScrVerse covers a complete paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsCompleteParagraph
		{
			get { return m_completePara; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether this ScrVerse is a Stanza Break.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsStanzaBreak
		{
			get { return m_isStanzaBreak; }
		}
		#endregion
	}
	#endregion
}
