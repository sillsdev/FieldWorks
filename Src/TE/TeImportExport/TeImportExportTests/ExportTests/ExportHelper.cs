// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides several helper methods for the export tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ExportHelper
	{
		/// <summary>Used to simulate the time changes for adding annotations. Tests are too
		/// fast so that multiple annotations would get same creation date.</summary>
		private static long s_Ticks = DateTime.Now.Ticks;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a title paragrpaph for the given book.
		/// </summary>
		/// <param name="scrInMemoryCache">in-memory cache to use for testing</param>
		/// <param name="book">The book</param>param>
		/// <param name="sTitle">The text of the title. Can be a simple string or a format
		/// string. (See InMemoryFdoCache.CreateFormatText for the definition of the
		/// format string)</param>
		/// ------------------------------------------------------------------------------------
		internal static void SetTitle(ScrInMemoryFdoCache scrInMemoryCache, IScrBook book, string sTitle)
		{
			book.TitleOA = new StText();

			if (sTitle[0] != '\\')
			{
				scrInMemoryCache.AddTitleToMockedBook(book.Hvo, sTitle);
			}
			else
			{
				// Create a more complex title from the given format string
				// insert a new para in the title
				StTxtPara para = new StTxtPara();
				book.TitleOA.ParagraphsOS.Append(para);
				// set the para's fields
				scrInMemoryCache.AddFormatTextToMockedPara(book, para, sTitle, scrInMemoryCache.Cache.DefaultVernWs);
				para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new section to the given book, having the specified text as the section
		/// head. The general section head paragraph style is used.
		/// </summary>
		/// <param name="scrInMemoryCache">in-memory cache to use for testing</param>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		internal static ScrSection CreateSection(ScrInMemoryFdoCache scrInMemoryCache, IScrBook book,
			string sSectionHead)
		{
			ScrSection section = CreateSection(scrInMemoryCache, book, sSectionHead, ScrStyleNames.SectionHead);
			// this should be a scripture section and not an intro section
			bool isIntro = false;
			section.VerseRefEnd = book.CanonicalNum * 1000000 + 1000 + ((isIntro) ? 0 : 1);
			section.AdjustReferences();
			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new section to the given book, having the specified text as the section
		/// head. The new section will have an empty content text created also.
		/// </summary>
		/// <param name="scrInMemoryCache">in-memory cache to use for testing</param>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head. Can be a simple string
		/// or a format string. (See CreateText for the definition of the format string)</param>
		/// <param name="paraStyleName">paragraph style to apply to the section head</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		internal static ScrSection CreateSection(ScrInMemoryFdoCache scrInMemoryCache, IScrBook book,
			string sSectionHead, string paraStyleName)
		{
			// Create a section
			ScrSection section = new ScrSection();
			book.SectionsOS.Append(section);

			// Create a section head for this section
			section.HeadingOA = new StText();

			if (sSectionHead.Length == 0 || sSectionHead[0] != '\\')
			{
				// create a simple section head with no character style
				StTxtParaBldr paraBldr = new StTxtParaBldr(scrInMemoryCache.Cache);
				paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(paraStyleName);
				paraBldr.AppendRun(sSectionHead,
					StyleUtils.CharStyleTextProps(null, scrInMemoryCache.Cache.DefaultVernWs));
				paraBldr.CreateParagraph(section.HeadingOAHvo);
			}
			else
			{
				// Create a more complex section head from the given format string
				// insert a new para in the title
				StTxtPara para = new StTxtPara();
				section.HeadingOA.ParagraphsOS.Append(para);
				// set the para's fields
				scrInMemoryCache.AddFormatTextToMockedPara(book, para, sSectionHead, scrInMemoryCache.Cache.DefaultVernWs);
				para.StyleRules = StyleUtils.ParaStyleTextProps(paraStyleName);
			}

			section.ContentOA = new StText();
			section.AdjustReferences();
			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a paragraph from a format string, and append it to the given section's
		/// heading.
		/// </summary>
		/// <param name="scrInMemoryCache">in-memory cache to use for testing</param>
		/// <param name="book">book to use</param>
		/// <param name="section">section to append to</param>
		/// <param name="format">(See CreateText for the definition of the format string)</param>
		/// <param name="ws">writing system to use</param>
		/// <param name="paraStyle">paragraph style name</param>
		/// <returns>the new paragraph</returns>
		/// ------------------------------------------------------------------------------------
		internal static StTxtPara AppendParagraphToSectionHead(ScrInMemoryFdoCache scrInMemoryCache,
			IScrBook book, IScrSection section, string format, int ws, string paraStyle)
		{
			// insert a new para in the section content
			StTxtPara para = new StTxtPara();
			section.HeadingOA.ParagraphsOS.Append(para);

			// set the para's fields
			scrInMemoryCache.AddFormatTextToMockedPara(book as ScrBook, para, format, ws);
			para.StyleRules = StyleUtils.ParaStyleTextProps(paraStyle);

			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a paragraph from a format string, and append it to the given section's
		/// content.
		/// </summary>
		/// <param name="scrInMemoryCache">in-memory cache to use for testing</param>
		/// <param name="book">book to use</param>
		/// <param name="section">section to append to</param>
		/// <param name="format">(See CreateText for the definition of the format string)</param>
		/// <param name="ws">writing system to use</param>
		/// <param name="paraStyle">paragraph style name</param>
		/// <returns>the new paragraph</returns>
		/// ------------------------------------------------------------------------------------
		internal static StTxtPara AppendParagraph(ScrInMemoryFdoCache scrInMemoryCache, IScrBook book,
			IScrSection section, string format, int ws, string paraStyle)
		{
			// insert a new para in the section content
			StTxtPara para = new StTxtPara();
			section.ContentOA.ParagraphsOS.Append(para);

			// set the para's fields
			scrInMemoryCache.AddFormatTextToMockedPara(book as ScrBook, para, format, ws);
			para.StyleRules = StyleUtils.ParaStyleTextProps(paraStyle);

			section.AdjustReferences();
			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a back translation from a format string, and attach it to the
		/// given paragraph.
		/// </summary>
		/// <param name="scrInMemoryCache">in-memory cache to use for testing</param>
		/// <param name="book">book to use</param>
		/// <param name="para">the paragraph that will own this back translation</param>
		/// <param name="format">(See CreateText for the definition of the format string)</param>
		/// <param name="ws">writing system of the back translation</param>
		/// ------------------------------------------------------------------------------------
		internal static void AddBackTranslation(ScrInMemoryFdoCache scrInMemoryCache, IScrBook book,
			StTxtPara para, string format, int ws)
		{
			ICmTranslation cmTrans = para.GetOrCreateBT();
			// Set the translation string for the given WS
			cmTrans.Translation.GetAlternative(ws).UnderlyingTsString =
				ScrInMemoryFdoCache.CreateFormatText(book, null, format, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the first translator note annotation definition.
		/// </summary>
		/// <returns>type of annotation</returns>
		/// ------------------------------------------------------------------------------------
		private static ICmAnnotationDefn StandardNoteType(ScrInMemoryFdoCache scrInMemoryCache)
		{
			ICmPossibility possibility =
				scrInMemoryCache.Cache.LangProject.AnnotationDefsOA.PossibilitiesOS[0];

			return possibility.SubPossibilitiesOS[0] as ICmAnnotationDefn;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an annotation to a language project that applies to a single verse reference and
		/// a single paragraph.
		/// </summary>
		/// <param name="scrInMemoryCache">in-memory cache to use for testing</param>
		/// <param name="noteText">text to include in annotation</param>
		/// <param name="reference">The reference.</param>
		/// <param name="para">StTxtPara to annotate</param>
		/// <returns>a new annotation</returns>
		/// ------------------------------------------------------------------------------------
		internal static IScrScriptureNote AddAnnotation(ScrInMemoryFdoCache scrInMemoryCache, string noteText,
			ScrReference reference, ICmObject para)
		{
			return AddAnnotation(scrInMemoryCache, noteText, reference, reference, para, para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an annotation to a language project.
		/// </summary>
		/// <param name="scrInMemoryCache">in-memory cache to use for testing</param>
		/// <param name="noteText">text to include in discussion</param>
		/// <param name="startRef"></param>
		/// <param name="endRef"></param>
		/// <param name="topPara">Begin StTxtPara to annotate</param>
		/// <param name="bottomPara">End StTxtPara to annotate</param>
		/// <returns>a new annotation</returns>
		/// ------------------------------------------------------------------------------------
		internal static IScrScriptureNote AddAnnotation(ScrInMemoryFdoCache scrInMemoryCache, string noteText,
			ScrReference startRef, ScrReference endRef, ICmObject topPara, ICmObject bottomPara)
		{
			ILangProject lp = scrInMemoryCache.Cache.LangProject;
			ScrBookAnnotations annotations = (ScrBookAnnotations)lp.TranslatedScriptureOA.BookAnnotationsOS[startRef.Book - 1];
			IScrScriptureNote note = annotations.InsertNote(startRef, endRef, topPara, bottomPara,
				StandardNoteType(scrInMemoryCache).Guid);

			StTxtPara discussionPara = (StTxtPara)note.DiscussionOA.ParagraphsOS[0];
			scrInMemoryCache.AddRunToMockedPara(discussionPara, noteText, scrInMemoryCache.Cache.DefaultAnalWs);

			note.DateCreated = new DateTime(s_Ticks++);

			return note;
		}
	}
}
