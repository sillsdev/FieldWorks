// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrCheckingTokenizer.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An enumerator for parsing a Scripture Book into tokens that can be checked
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class ScrCheckingTokenizer : IEnumerator<ITextToken>
	{
		#region TokenizableText class
		private class TokenizableText
		{
			internal IStText m_text;
			internal ITsString m_paraTss;
			internal ICmObject m_obj;
			internal int m_iPara;
			internal int m_iRun;
			internal int m_flid;
			private string m_paraStyleName;
			private TextType m_defaultTextType;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="TokenizableText"/> class.
			/// </summary>
			/// <param name="text">The StText</param>
			/// <param name="defaultTextType">The default text type of the text</param>
			/// --------------------------------------------------------------------------------
			internal TokenizableText(IStText text, TextType defaultTextType)
			{
				m_text = text;
				m_iPara = 0;
				m_flid = StTxtParaTags.kflidContents;
				InitPara();
				m_defaultTextType = defaultTextType;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="TokenizableText"/> class for a
			/// TSS that is not part of a StText.
			/// </summary>
			/// <param name="tss">The StText</param>
			/// <param name="styleName">Name of the style associated with the TSS.</param>
			/// <param name="defaultTextType">The default text type of the text</param>
			/// <param name="obj">The object.</param>
			/// <param name="flid">The flid</param>
			/// --------------------------------------------------------------------------------
			internal TokenizableText(ITsString tss, string styleName, TextType defaultTextType,
				ICmObject obj, int flid)
			{
				m_text = null;
				m_obj = obj;
				m_iPara = -1;
				m_iRun = -1;
				m_flid = flid;
				m_paraTss = tss;
				m_paraStyleName = styleName;
				m_defaultTextType = defaultTextType;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes the variables related to the current paragraph.
			/// </summary>
			/// --------------------------------------------------------------------------------
			private void InitPara()
			{
				m_iRun = -1;
				IStTxtPara para = (IStTxtPara)m_text.ParagraphsOS[m_iPara];

				m_obj = para;
				m_paraTss = para.Contents;
				m_paraStyleName = para.StyleName;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the default text type of the text that is being tokenized.
			/// </summary>
			/// --------------------------------------------------------------------------------
			internal TextType DefaultTextType
			{
				get { return m_defaultTextType; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Move to the Next paragraph, if any.
			/// </summary>
			/// <returns><c>true</c> if we successfully advance to the next paragraph;
			/// <c>false</c> if there are no more paragraphs in the text.</returns>
			/// --------------------------------------------------------------------------------
			internal bool NextParagraph()
			{
				if (m_text != null && ++m_iPara < m_text.ParagraphsOS.Count)
				{
					InitPara();
					return true;
				}
				return false;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the name of the para style.
			/// </summary>
			/// --------------------------------------------------------------------------------
			internal string ParaStyleName
			{
				get { return m_paraStyleName; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the name of the character style.
			/// </summary>
			/// --------------------------------------------------------------------------------
			internal string CharStyleName
			{
				get { return string.Empty; }
			}
		}
		#endregion

		#region Data members
		private bool m_fOrcWasStartOfPara = false;
		private IScripture m_scr;
		private IScrBook m_book;
		private int m_chapterNum;
		private IScrSection m_currentSection;
		private int m_iCurrentSection;
		private TokenizableText m_outerText; // We could use a stack for this, but it's never more than 1 level deep
		private TokenizableText m_currentScrText;
		private ScrCheckingToken m_internalToken;
		private ScrCheckingToken m_currentToken;
		private bool m_foundStart;
		#endregion

		#region Constructor & Disposal
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrCheckingTokenizer"/> class.
		/// </summary>
		/// <param name="book">The book being parsed.</param>
		/// <param name="chapterNum">The 1-basede canonical chapter number being parse, or 0 to
		/// parse the whole book</param>
		/// ------------------------------------------------------------------------------------
		public ScrCheckingTokenizer(IScrBook book, int chapterNum)
		{
			m_book = book;
			m_scr = (IScripture)book.Owner;
			m_chapterNum = chapterNum;
			Reset();
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~ScrCheckingTokenizer()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		/// <remarks>This method is part of interface IEnumerator&lt;T&gt;</remarks>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
		{
				// dispose managed and unmanaged objects
			}
			m_book = null;
			m_currentSection = null;
			m_internalToken = null;
			m_outerText = null;
			m_currentScrText = null;
			IsDisposed = true;
		}
		#endregion
		#endregion

		#region IEnumerator<ITextToken> Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <value></value>
		/// <returns>The element in the collection at the current position of the enumerator.</returns>
		/// ------------------------------------------------------------------------------------
		public ITextToken Current
		{
			get { return m_currentToken; }
		}
		#endregion

		#region IEnumerator Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <value></value>
		/// <returns>The element in the collection at the current position of the enumerator.</returns>
		/// ------------------------------------------------------------------------------------
		object IEnumerator.Current
		{
			get { return Current; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>
		/// true if the enumerator was successfully advanced to the next element; false if the
		/// enumerator has passed the end of the collection.
		/// </returns>
		/// <exception cref="T:System.InvalidOperationException">The collection was modified
		/// after the enumerator was created. </exception>
		/// ------------------------------------------------------------------------------------
		public bool MoveNext()
		{
			if (m_currentScrText == null)
				return false;

			while (++m_currentScrText.m_iRun < m_currentScrText.m_paraTss.RunCount)
			{
				m_internalToken.m_fParagraphStart = (m_currentScrText.m_iRun == 0);

				ITsTextProps runProps = m_currentScrText.m_paraTss.get_Properties(m_currentScrText.m_iRun);
				string charStyleName = runProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				m_internalToken.m_sText = m_currentScrText.m_paraTss.get_RunText(m_currentScrText.m_iRun);
				m_internalToken.m_paraOffset = m_currentScrText.m_paraTss.get_MinOfRun(m_currentScrText.m_iRun);
				int var;
				int ws = runProps.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				m_internalToken.m_icuLocale = GetLocale(ws);
				m_internalToken.Ws = ws;
				switch (runProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle))
				{
					case ScrStyleNames.VerseNumber:
						if (!m_foundStart || string.IsNullOrEmpty(m_internalToken.m_sText))
							continue;

						m_internalToken.m_textType = TextType.VerseNumber;
						int verse = ScrReference.VerseToIntStart(m_internalToken.m_sText);
						if (verse != 0)
							m_internalToken.m_startRef.Verse = verse;
						verse = ScrReference.VerseToIntEnd(m_internalToken.m_sText);
						if (verse != 0)
							m_internalToken.m_endRef.Verse = verse;

						break;
					case ScrStyleNames.ChapterNumber:
						if (string.IsNullOrEmpty(m_internalToken.m_sText))
							continue;
						int chapter = 0;
						try
						{
							chapter = ScrReference.ChapterToInt(m_internalToken.m_sText);
						}
						catch
						{
							// Ignore exceptions. We'll flag them later as errors.
						}
						if (!m_foundStart)
						{
							if (m_chapterNum != chapter)
								continue;
							m_foundStart = true;
						}
						else if (m_chapterNum > 0 && m_chapterNum != chapter)
						{
							// Stop if we're only getting tokens for a single chapter (unless
							// this is an (erroneous) second occurrence of the same chapter)
							return false;
						}
						m_internalToken.m_textType = TextType.ChapterNumber;
						m_internalToken.m_startRef.Chapter = m_internalToken.m_endRef.Chapter = chapter;
						m_internalToken.m_startRef.Verse = m_internalToken.m_endRef.Verse = 1;
						break;
					default:
						{
							if (!m_foundStart)
								continue;
							// Deal with footnotes and picture captions
							Guid guidObj = TsStringUtils.GetGuidFromRun(m_currentScrText.m_paraTss, m_currentScrText.m_iRun, runProps);
							if (guidObj == Guid.Empty)
							{
								m_internalToken.m_textType = m_currentScrText.DefaultTextType;
							}
							else if (m_outerText != null)
							{
								// It was possible through copy/paste to put ORCs into footnotes or pictures, but that is no
								// longer allowed. This tokenizing code won't handle the nesting correctly, so just ignore
								// the nested ORC. See TE-8609.
								continue;
							}
							else
							{
								m_fOrcWasStartOfPara = m_internalToken.m_fParagraphStart;

								ICmObject obj;
								m_book.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(guidObj, out obj);
								if (obj is IStFootnote)
								{
									m_outerText = m_currentScrText;
									// footnotes are StTexts
									m_currentScrText = new TokenizableText((IStText)obj, TextType.Note);
									return MoveNext();
								}
								if (obj is ICmPicture)
								{
									m_outerText = m_currentScrText;
									ICmPicture pict = (ICmPicture)obj;
									m_currentScrText = new TokenizableText(
										pict.Caption.VernacularDefaultWritingSystem,
										ScrStyleNames.Figure, TextType.PictureCaption, pict,
										CmPictureTags.kflidCaption);
									return MoveNext();
								}
							}
						}
						break;
				}
				m_internalToken.m_fNoteStart = (m_internalToken.m_textType == TextType.Note &&
					m_internalToken.m_fParagraphStart && m_currentScrText.m_iPara == 0);
				m_internalToken.m_paraStyleName = m_currentScrText.ParaStyleName;
				m_internalToken.m_charStyleName = charStyleName != null ? charStyleName : string.Empty;
				m_internalToken.m_object = m_currentScrText.m_obj;
				m_internalToken.m_flid = m_currentScrText.m_flid;

				// We need the current token to be a copy of our internal token so we don't change the
				// internal variables of whatever was returned from the enumerator.
				m_currentToken = m_internalToken.Copy();
				return true;
			}
			// Finished that paragraph and didn't find any more runs; try next para in this text,
			// if any.
			if (!m_currentScrText.NextParagraph())
			{
				if (!m_foundStart)
				{
					Debug.Fail("We should have found the desired chapter wtihin the section we were searching.");
					return false;
				}
				// Finished that text and didn't find any more paragraphs.
				// If we have been processing an inner text (footnote or picture caption), pop back
				// out to the "outer" one.
				if (m_outerText != null)
				{
					m_currentScrText = m_outerText;
					m_outerText = null;
					bool result = MoveNext();
					if (result)
					{
						m_currentToken.m_fParagraphStart |= m_fOrcWasStartOfPara;
						m_fOrcWasStartOfPara = false;
					}
					return result;
				}

				// Otherwise, try next text, if any.
				if (m_currentScrText.m_text.OwningFlid == ScrBookTags.kflidTitle)
				{
					// Get first section head text.
					CurrentSectionIndex = 0;
				}
				else if (m_currentScrText.m_text.OwningFlid == ScrSectionTags.kflidHeading)
				{
					m_currentScrText = new TokenizableText(m_currentSection.ContentOA,
						m_currentSection.IsIntro ? TextType.Other : TextType.Verse);
				}
				else
				{
					Debug.Assert(m_currentScrText.m_text.OwningFlid == ScrSectionTags.kflidContent);
					if (m_iCurrentSection + 1 >= m_book.SectionsOS.Count)
						return false;
					CurrentSectionIndex++;
					if (m_chapterNum > 0 && ScrReference.GetChapterFromBcv(m_currentSection.VerseRefStart) != m_chapterNum)
						return false;
				}

			}
			return MoveNext();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the current section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int CurrentSectionIndex
		{
			get
			{
				return m_iCurrentSection;
			}
			set
			{
				m_iCurrentSection = value;
				if (m_iCurrentSection == -1)
				{
					m_currentSection = null;
					m_currentScrText = null;
					return;
				}
				m_currentSection = m_book.SectionsOS[m_iCurrentSection];
				m_currentScrText = new TokenizableText(m_currentSection.HeadingOA, TextType.Other);
				m_internalToken.m_startRef = m_currentSection.VerseRefStart;
				m_internalToken.m_endRef = new BCVRef(m_internalToken.m_startRef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ICU locale for the given writing system, or null if this is the default
		/// vernacular writing system. We also return null if the given ws cannot be resolved to
		/// a valid locale. Typically this would be the result of a TSString that was created
		/// without getting the writing system set at all (there was an import bug that used to
		/// be able to cause this). ENHANCE: Some day we might want to consider a check to look
		/// for an invalid or un-set WS; then we'd have to return a special locale value to
		/// represent that.
		/// </summary>
		/// <param name="ws">The id of the writing system.</param>
		/// ------------------------------------------------------------------------------------
		private string GetLocale(int ws)
		{
			if (ws == m_book.Cache.DefaultVernWs || ws <= 0 || !m_book.Services.WritingSystemManager.Exists(ws))
				return null;

			return m_book.Services.WritingSystemManager.Get(ws).IcuLocale;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the enumerator to its initial position, which is before the first element in
		/// the collection.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">The collection was modified
		/// after the enumerator was created. </exception>
		/// ------------------------------------------------------------------------------------
		public void Reset()
		{
			m_internalToken = new ScrCheckingToken();
			m_outerText = null;
			if (m_chapterNum == 0)
			{
				m_currentScrText = new TokenizableText(m_book.TitleOA, TextType.Other);
				m_currentSection = null;
				m_iCurrentSection = -1;
				m_internalToken.m_startRef = m_internalToken.m_endRef =
					new ScrReference(m_book.CanonicalNum, 1, 0, m_scr.Versification);
				m_foundStart = true;
			}
			else
			{
				// Set to the first token for the requested chapter
				CurrentSectionIndex = m_book.FindSectionForChapter(m_chapterNum);
				if (ScrReference.GetChapterFromBcv(m_currentSection.VerseRefStart) != m_chapterNum)
				{
					m_currentScrText = new TokenizableText(m_currentSection.ContentOA, TextType.Verse);
					m_foundStart = false;
				}
				else
					m_foundStart = true;
			}
		}
		#endregion
	}
}
