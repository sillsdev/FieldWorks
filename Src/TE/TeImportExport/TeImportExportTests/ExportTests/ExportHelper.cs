// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2006' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportHelper.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using System;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;
using System.Diagnostics;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE.ExportTests
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
		/// <param name="testBase">in-memory test base</param>
		/// <param name="book">The book</param>param>
		/// <param name="sTitle">The text of the title. Can be a simple string or a format
		/// string. </param>
		/// ------------------------------------------------------------------------------------
		internal static void SetTitle(ScrInMemoryFdoTestBase testBase, IScrBook book, string sTitle)
		{
			book.TitleOA = testBase.Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();

			if (sTitle[0] != '\\')
			{
				testBase.AddTitleToMockedBook(book, sTitle);
			}
			else
			{
				// Create a more complex title from the given format string
				// insert a new para in the title
				IScrTxtPara para = testBase.Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
					book.TitleOA, ScrStyleNames.MainBookTitle);
				// set the para's fields
				testBase.AddFormatTextToMockedPara(book, para, sTitle, testBase.Cache.DefaultVernWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new section to the given book, having the specified text as the section
		/// head. The general section head paragraph style is used.
		/// </summary>
		/// <param name="testBase">in-memory test base class</param>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		internal static IScrSection CreateSection(ScrInMemoryFdoTestBase testBase, IScrBook book,
			string sSectionHead)
		{
			IScrSection section = CreateSection(testBase, book, sSectionHead, ScrStyleNames.SectionHead);
			// this should be a scripture section and not an intro section
			bool isIntro = false;
			section.VerseRefEnd = book.CanonicalNum * 1000000 + 1000 + ((isIntro) ? 0 : 1);
			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new section to the given book, having the specified text as the section
		/// head. The new section will have an empty content text created also.
		/// </summary>
		/// <param name="testBase">in-memory test base to use for testing</param>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head. Can be a simple string
		/// or a format string. (See CreateText for the definition of the format string)</param>
		/// <param name="paraStyleName">paragraph style to apply to the section head</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		internal static IScrSection CreateSection(ScrInMemoryFdoTestBase testBase, IScrBook book,
			string sSectionHead, string paraStyleName)
		{
			// Create a section
			bool fIsIntro = (paraStyleName.Equals(ScrStyleNames.IntroSectionHead));
			IScrSection section = testBase.AddSectionToMockedBook(book, fIsIntro);

			// Create a heading paragraph with text
			string styleName = !string.IsNullOrEmpty(paraStyleName) ? paraStyleName :
				ScrStyleNames.SectionHead;
			IStTxtPara para = section.HeadingOA.AddNewTextPara(styleName);

			if (!string.IsNullOrEmpty(sSectionHead) && sSectionHead[0] == '\\')
			{
				// Text is formatted so add it as a formatted string.
				testBase.AddFormatTextToMockedPara(book, para, sSectionHead, testBase.Cache.DefaultVernWs);
			}
			else
				testBase.AddRunToMockedPara(para, sSectionHead, testBase.Cache.DefaultVernWs);

			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a paragraph from a format string, and append it to the given section's
		/// heading.
		/// </summary>
		/// <param name="testBase">in-memory cache to use for testing</param>
		/// <param name="book">book to use</param>
		/// <param name="section">section to append to</param>
		/// <param name="format">(See CreateText for the definition of the format string)</param>
		/// <param name="ws">writing system to use</param>
		/// <param name="paraStyle">paragraph style name</param>
		/// <returns>the new paragraph</returns>
		/// ------------------------------------------------------------------------------------
		internal static IScrTxtPara AppendParagraphToSectionHead(ScrInMemoryFdoTestBase testBase,
			IScrBook book, IScrSection section, string format, int ws, string paraStyle)
		{
			// insert a new para in the section content
			IScrTxtPara para = testBase.Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				section.HeadingOA, paraStyle);

			// set the para's fields
			testBase.AddFormatTextToMockedPara(book, para, format, ws);

			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a paragraph from a format string, and append it to the given section's
		/// content.
		/// </summary>
		/// <param name="testBase">in-memory test base</param>
		/// <param name="book">book to use</param>
		/// <param name="section">section to append to</param>
		/// <param name="format">(See CreateText for the definition of the format string)</param>
		/// <param name="ws">writing system to use</param>
		/// <param name="paraStyle">paragraph style name</param>
		/// <returns>the new paragraph</returns>
		/// ------------------------------------------------------------------------------------
		internal static IScrTxtPara AppendParagraph(ScrInMemoryFdoTestBase testBase, IScrBook book,
			IScrSection section, string format, int ws, string paraStyle)
		{
			// insert a new para in the section content
			IScrTxtPara para = testBase.Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				section.ContentOA, paraStyle);

			// set the para's fields
			testBase.AddFormatTextToMockedPara(book, para, format, ws);

			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a back translation from a format string, and attach it to the
		/// given paragraph.
		/// </summary>
		/// <param name="testBase">in-memory test base</param>
		/// <param name="book">book to use</param>
		/// <param name="para">the paragraph that will own this back translation</param>
		/// <param name="format">(See CreateText for the definition of the format string)</param>
		/// <param name="ws">writing system of the back translation</param>
		/// ------------------------------------------------------------------------------------
		internal static void AddBackTranslation(ScrInMemoryFdoTestBase testBase, IScrBook book,
			IScrTxtPara para, string format, int ws)
		{
			ICmTranslation cmTrans = para.GetOrCreateBT();
			// Set the translation string for the given WS
			cmTrans.Translation.set_String(ws, testBase.CreateFormatText(book, null, format, ws));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the first translator note annotation definition.
		/// </summary>
		/// <returns>type of annotation</returns>
		/// ------------------------------------------------------------------------------------
		private static ICmAnnotationDefn StandardNoteType(FdoCache cache)
		{
			foreach (ICmPossibility possibility in cache.LangProject.AnnotationDefsOA.PossibilitiesOS)
				if (possibility.Guid == CmAnnotationDefnTags.kguidAnnNote)
					return possibility.SubPossibilitiesOS[0] as ICmAnnotationDefn;
			Debug.Fail("Could not find annotation note type");
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an annotation to a language project that applies to a single verse reference and
		/// a single paragraph.
		/// </summary>
		/// <param name="testBase">in-memory test base</param>
		/// <param name="noteText">text to include in annotation</param>
		/// <param name="reference">The reference.</param>
		/// <param name="para">IStTxtPara to annotate</param>
		/// <returns>a new annotation</returns>
		/// ------------------------------------------------------------------------------------
		internal static IScrScriptureNote AddAnnotation(ScrInMemoryFdoTestBase testBase, string noteText,
			ScrReference reference, ICmObject para)
		{
			return AddAnnotation(testBase, noteText, reference, reference, para, para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an annotation to a language project.
		/// </summary>
		/// <param name="testBase">in-memory cache to use for testing</param>
		/// <param name="noteText">text to include in discussion</param>
		/// <param name="startRef"></param>
		/// <param name="endRef"></param>
		/// <param name="topPara">Begin IStTxtPara to annotate</param>
		/// <param name="bottomPara">End IStTxtPara to annotate</param>
		/// <returns>a new annotation</returns>
		/// ------------------------------------------------------------------------------------
		internal static IScrScriptureNote AddAnnotation(ScrInMemoryFdoTestBase testBase, string noteText,
			ScrReference startRef, ScrReference endRef, ICmObject topPara, ICmObject bottomPara)
		{
			ILangProject lp = testBase.Cache.LangProject;
			IScrBookAnnotations annotations = lp.TranslatedScriptureOA.BookAnnotationsOS[startRef.Book - 1];
			IScrScriptureNote note = annotations.InsertNote(startRef, endRef, topPara, bottomPara,
				StandardNoteType(testBase.Cache).Guid);

			IStTxtPara discussionPara = (IStTxtPara)note.DiscussionOA.ParagraphsOS[0];
			testBase.AddRunToMockedPara(discussionPara, noteText, testBase.Cache.DefaultAnalWs);

			note.DateCreated = new DateTime(s_Ticks++);

			return note;
		}
	}
}
