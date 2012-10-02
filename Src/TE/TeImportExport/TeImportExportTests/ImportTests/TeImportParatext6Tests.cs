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
// File: TeImportParatext6Tests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using NUnit.Framework;//using NMock;
//using NMock.Constraints;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.ScrImportComponents;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.FDOTests;
using SILUBS.SharedScrUtils;
using System.IO;

namespace SIL.FieldWorks.TE.ImportTests
{
	#region TE Paratext 6 Import Tests (in-memory cache)
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// TeImportTestsParatext6 tests TeImport for Paratext 6 projects
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class TeImportTestParatext6 : TeImportTestsBase
	{
		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes import settings (mappings and options) for "Paratext 6" type of import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeImportSettings()
		{
			DummyTeImporter.MakeParatextTestSettings(m_settings);
			m_settings.ImportBackTranslation = true;
			m_settings.ImportBookIntros = true;
			m_settings.ImportAnnotations = false;
		}
		#endregion

		#region Footnote tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a simple footnote (in the intro material) that begins with an
		/// asterisk. This should beinterpreted as the custom footnote symbol.
		///		\id MRK
		///		\ip intro \f * This is a footnote \f* paragraph
		///		\p
		///		\c 1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteBeginningWithAsterisk()
		{
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.ProcessSegment("intro ", @"\ip");
			m_importer.ProcessSegment("* This is a footnote ", @"\f");
			m_importer.ProcessSegment("paragraph", @"\f*");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(2, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the footnote text
			StFootnote footnote = (StFootnote)mark.FootnotesOS[0];
			ITsString tss = ((StTxtPara)footnote.ParagraphsOS[0]).Contents.UnderlyingTsString;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "This is a footnote", null, Cache.DefaultVernWs);

			// verify the intro section content text
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("intro" + StringUtils.kchObject + " paragraph",
				para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a footnote that has a single token (word) consisting of multiple
		/// characters followed by an explicitly marked footnote segment mapped to Default Para
		/// Characters, with an end delimiter (this is not standard USFM). The first token
		/// should be thrown away and the explicit footnote properties should not be affected
		/// (This is because we think the user possibly meant it as a custom symbol but we don't
		/// want to interpret it as such automatically since it is unusual to have a
		/// multi-character symbol).
		///		\id MRK
		///		\c 1
		///		\s section
		///		\v 1
		///		\vt paragraph \f This \ft is a \ft* footnote \f* one
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteBeginningWithMultiCharToken()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ft", @"\ft*",
				MarkerDomain.Footnote, "Default Paragraph Characters", null, null));
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("paragraph ", @"\vt");
			m_importer.ProcessSegment("This ", @"\f");
			m_importer.ProcessSegment("is a ", @"\ft");
			m_importer.ProcessSegment("footnote ", @"\ft*");
			m_importer.ProcessSegment("one", @"\f*");
			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the footnote text
			StFootnote footnote = (StFootnote)mark.FootnotesOS[0];
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "is a footnote", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents.UnderlyingTsString);
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kchObject + " one",
				para.Contents.UnderlyingTsString.Text);

			// verify the section head text
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a footnote that has multiple words (not standard USFM) followed by
		/// explicitly marked footnote segments. The words preceding the explicit footnote text
		/// segment should be included as part of the footnote text and the explicit footnote
		/// properties should not be affected.
		///		\id MRK
		///		\c 1
		///		\s new section
		///		\v 1
		///		\vt paragraph \f A big \ft footnote issue \f* one
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteBeginningWithMultipleWords()
		{
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("paragraph ", @"\vt");
			m_importer.ProcessSegment("A big ", @"\f");
			m_importer.ProcessSegment("footnote issue ", @"\ft");
			m_importer.ProcessSegment("one", @"\f*");
			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the footnote text
			StFootnote footnote = (StFootnote)mark.FootnotesOS[0];
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "A big footnote issue", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents.UnderlyingTsString);
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kchObject + " one",
				para.Contents.UnderlyingTsString.Text);

			// verify the section head text
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a footnote that ends with a run of text that is mapped to a character
		/// style.
		///		\id MRK
		///		\c 1
		///		\s new section
		///		\v 1
		///		\vt paragraph \f This is a \em footnote \f* one
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteEndsWithCharStyle()
		{
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("paragraph ", @"\vt");
			m_importer.ProcessSegment("This is a ", @"\f");
			m_importer.ProcessSegment("footnote ", @"\em");
			m_importer.ProcessSegment("one", @"\f*");
			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the footnote text
			StFootnote footnote = (StFootnote)mark.FootnotesOS[0];
			ITsString tss = ((StTxtPara)footnote.ParagraphsOS[0]).Contents.UnderlyingTsString;
			Assert.AreEqual(2, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "This is a ", null, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(tss, 1, "footnote", "Emphasis", Cache.DefaultVernWs);
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kchObject + " one",
				para.Contents.UnderlyingTsString.Text);

			// verify the section head text
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a one-word footnote that is the very last thing in the imported
		/// content and has no explicit end marker.
		///		\id MRK
		///		\c 1
		///		\s new section
		///		\v 1
		///		\vt paragraph \f footnote
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteLastThing()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ft", @"\ft*",
				MarkerDomain.Footnote, "Default Paragraph Characters", null, null));
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("paragraph ", @"\vt");
			m_importer.ProcessSegment("footnote ", @"\f");
			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the footnote text
			StFootnote footnote = (StFootnote)mark.FootnotesOS[0];
			ITsString tss = ((StTxtPara)footnote.ParagraphsOS[0]).Contents.UnderlyingTsString;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "footnote", null, Cache.DefaultVernWs);
			Assert.AreNotEqual("footnote", m_scr.GeneralFootnoteMarker);
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kchObject,
				para.Contents.UnderlyingTsString.Text);

			// verify the section head text
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a footnote that has a single alpha character followed by explicitly
		/// marked footnote segments. That character should be used as the custom footnote
		/// symbol.
		///		\id MRK
		///		\c 1
		///		\s new section
		///		\v 1
		///		\vt paragraph \f q \fr 1.1 \ft This is a footnote \f* one
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteLookahead()
		{
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("paragraph ", @"\vt");
			m_importer.ProcessSegment("q ", @"\f");
			m_importer.ProcessSegment("1.1 ", @"\fr");
			m_importer.ProcessSegment("This is a footnote ", @"\ft");
			m_importer.ProcessSegment("one", @"\f*");
			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the footnote text
			StFootnote footnote = (StFootnote)mark.FootnotesOS[0];
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "This is a footnote", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents.UnderlyingTsString);
			Assert.AreEqual(FootnoteMarkerTypes.SymbolicFootnoteMarker, m_scr.FootnoteMarkerType);
			Assert.AreEqual("q", m_scr.GeneralFootnoteMarker);

			// verify the section content text
			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kchObject + " one",
				para.Contents.UnderlyingTsString.Text);

			// verify the section head text
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a footnote that contains text preceding the footnote reference
		/// segment.
		///		\id MRK
		///		\c 1
		///		\s new section
		///		\v 1
		///		\vt paragraph \f I wish \fr 1.1 \ft This is a footnote \f* one
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteWithTextBeforeReference()
		{
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("paragraph ", @"\vt");
			m_importer.ProcessSegment("I wish ", @"\f");
			m_importer.ProcessSegment("1.1 ", @"\fr");
			m_importer.ProcessSegment("This is a footnote ", @"\ft");
			m_importer.ProcessSegment("one", @"\f*");
			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the footnote text
			StFootnote footnote = (StFootnote)mark.FootnotesOS[0];
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "I wish This is a footnote", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents.UnderlyingTsString);
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kchObject + " one",
				para.Contents.UnderlyingTsString.Text);

			// verify the section head text
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a footnote that contains a run of text that is mapped to default
		/// paragraph characters.
		///		\id MRK
		///		\c 1
		///		\s new section
		///		\v 1
		///		\vt paragraph \f \fq This \ft is \ft* \fq a \fq* footnote \f* one
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteDefaultParaChars1()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ft", @"\ft*",
				MarkerDomain.Footnote, "Default Paragraph Characters", null, null));
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("paragraph ", @"\vt");
			m_importer.ProcessSegment("", @"\f");
			m_importer.ProcessSegment("This ", @"\fq");
			m_importer.ProcessSegment("is ", @"\ft");
			m_importer.ProcessSegment("", @"\ft*");
			m_importer.ProcessSegment("a ", @"\fq");
			m_importer.ProcessSegment("footnote ", @"\fq*");
			m_importer.ProcessSegment("one", @"\f*");
			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the footnote text
			StFootnote footnote = (StFootnote)mark.FootnotesOS[0];
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "footnote", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.Replace(0, 0, "a ", StyleUtils.CharStyleTextProps("Quoted Text", Cache.DefaultVernWs));
			bldr.Replace(0, 0, "is ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.Replace(0, 0, "This ", StyleUtils.CharStyleTextProps("Quoted Text", Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents.UnderlyingTsString);

			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kchObject + " one",
				para.Contents.UnderlyingTsString.Text);

			// verify the section head text
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a footnote that contains a run of text that is mapped to default
		/// paragraph characters.
		///		\id MRK
		///		\c 1
		///		\s section
		///		\v 1
		///		\vt paragraph \ft This is a footnote \ft* one
		/// </summary>
		/// <remarks>Ideally \ft (being mapped to Default Para Chars) would create a footnote
		/// even without a \f because it is mapped to the footnote domain. However, the
		/// current implementation of TE would put too much in the footnote (doesn't switch
		/// domain back at \ft*) so it was decided to ignore the domain for now. This test
		/// tests the current behavior. See TE-5078</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteDefaultParaChars2()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ft", @"\ft*",
				MarkerDomain.Footnote, "Default Paragraph Characters", null, null));
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("paragraph ", @"\vt");
			m_importer.ProcessSegment("This is a footnote ", @"\ft");
			m_importer.ProcessSegment(" one", @"\ft*");
			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the section content text
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			ITsString tssPara = para.Contents.UnderlyingTsString;
			Assert.AreEqual(5, tssPara.RunCount);
			AssertEx.RunIsCorrect(tssPara, 0, "1", ScrStyleNames.ChapterNumber, m_wsVern);
			AssertEx.RunIsCorrect(tssPara, 1, "1", ScrStyleNames.VerseNumber, m_wsVern);
			AssertEx.RunIsCorrect(tssPara, 2, "paragraph", null, m_wsVern);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);
			AssertEx.RunIsCorrect(tssPara, 4, " one", null, m_wsVern);
			VerifyFootnote(mark.FootnotesOS[0], para, 11);

			// verify the section head text
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a footnote that contains a run of text that is mapped to default
		/// paragraph characters.
		///		\id MRK
		///		\c 1
		///		\s new section
		///		\v 1
		///		\vt paragraph \f \fr 1.3 \ft This is \fq a footnote \fq* \f* one
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteDefaultParaChars3()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ft", @"\ft*",
				MarkerDomain.Footnote, "Default Paragraph Characters", null, null));
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("paragraph ", @"\vt");
			m_importer.ProcessSegment("", @"\f");
			m_importer.ProcessSegment("1.3 ", @"\fr");
			m_importer.ProcessSegment("This is ", @"\ft");
			m_importer.ProcessSegment("a footnote ", @"\fq");
			m_importer.ProcessSegment("", @"\fq*");
			m_importer.ProcessSegment("one", @"\f*");
			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the footnote text
			StFootnote footnote = (StFootnote)mark.FootnotesOS[0];
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "a footnote", StyleUtils.CharStyleTextProps("Quoted Text", Cache.DefaultVernWs));
			bldr.Replace(0, 0, "This is ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents.UnderlyingTsString);

			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kchObject + " one",
				para.Contents.UnderlyingTsString.Text);

			// verify the section head text
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test importing a footnote that contains a run of text that is mapped to default
		/// paragraph characters.
		///		\id MRK
		///		\c 1
		///		\s new section
		///		\v 1
		///		\vt paragraph \f \fr 1.3 \ft This is \fq a \fq* footnote \f* one
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteDefaultParaChars4()
		{
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ft", @"\ft*",
				MarkerDomain.Footnote, "Default Paragraph Characters", null, null));
			m_importer.Initialize();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(41, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("section", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(41, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("paragraph ", @"\vt");
			m_importer.ProcessSegment("", @"\f");
			m_importer.ProcessSegment("1.3 ", @"\fr");
			m_importer.ProcessSegment("This is ", @"\ft");
			m_importer.ProcessSegment("a ", @"\fq");
			m_importer.ProcessSegment("footnote ", @"\fq*");
			m_importer.ProcessSegment("one", @"\f*");
			m_importer.FinalizeImport();

			// Verify the imported data
			IScrBook mark = m_importer.UndoInfo.ImportedSavedVersion.FindBook(41);
			Assert.IsNotNull(mark, "Book not created");
			Assert.AreEqual(1, mark.SectionsOS.Count, "section count is not correct");
			Assert.AreEqual(1, mark.FootnotesOS.Count, "Footnote count is not correct");
			ScrSection section = (ScrSection)mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = mark.FootnotesOS[0];
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "footnote", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.Replace(0, 0, "a ", StyleUtils.CharStyleTextProps("Quoted Text", Cache.DefaultVernWs));
			bldr.Replace(0, 0, "This is ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents.UnderlyingTsString);

			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kchObject + " one",
				para.Contents.UnderlyingTsString.Text);

			// verify the section head text
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.UnderlyingTsString.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		/// \v 1 ... \f - \ft ...\vt ...
		/// \v 2 ... \f - \fr 1.2 \ft ...\vt ...
		/// \v 3 ... \f + \fr 1.3 \ft ...\vt ...
		/// \v 4 ... \f - \fr 1.3 \ft ...\vt ...
		/// Jira number is TE-2064 (also part of TE-996)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleUSFMStyleFootnotes_FirstOneHasCallerOmitted()
		{
			CheckDisposed();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a footnote, no end marker, terminated by another footnote
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment(" ", @"\p");
			// Verse 1: footnote with auto-numbered marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("Verse 1 start...", @"\v");
			m_importer.ProcessSegment("- ", @"\f");
			m_importer.ProcessSegment("Footnote 1 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 1 end. ", @"\vt");

			// Verse 2: footnote with custom marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("Verse 2 start...", @"\v");
			m_importer.ProcessSegment("* ", @"\f");
			m_importer.ProcessSegment("1.2 ", @"\fr");
			m_importer.ProcessSegment("Footnote 2 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 2 end. ", @"\vt");

			// Verse 3: footnote with no marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			m_importer.ProcessSegment("Verse 3 start...", @"\v");
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("1.3 ", @"\fr");
			m_importer.ProcessSegment("Footnote 3 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 3 end. ", @"\vt");

			// Verse 4: footnote with missing marker specification should fall back to default
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 4);
			m_importer.ProcessSegment("Verse 4 start...", @"\v");
			m_importer.ProcessSegment("- ", @"\f");
			m_importer.ProcessSegment("1.4 ", @"\fr");
			m_importer.ProcessSegment("Footnote 4 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 4 end. ", @"\vt");

			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedSavedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Verse 1 start..." + StringUtils.kchObject.ToString() + " ...verse 1 end. " +
				"2Verse 2 start..." + StringUtils.kchObject.ToString() + " ...verse 2 end. " +
				"3Verse 3 start..." + StringUtils.kchObject.ToString() + " ...verse 3 end. " +
				"4Verse 4 start..." + StringUtils.kchObject.ToString() + " ...verse 4 end.",
				para.Contents.Text);
			Assert.IsNull(m_scr.GeneralFootnoteMarker);
			VerifySimpleFootnote(0, "Footnote 1 text", string.Empty);
			VerifySimpleFootnote(1, "Footnote 2 text", string.Empty);
			VerifySimpleFootnote(2, "Footnote 3 text", string.Empty);
			VerifySimpleFootnote(3, "Footnote 4 text", string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		/// \v 1 ... \f + \ft ...\vt ...
		/// \v 2 ... \f * \fr 1.2 \ft ...\vt ...
		/// \v 3 ... \f + \fr 1.3 \ft ...\vt ...
		/// \v 4 ... \f + \fr 1.3 \ft ...\vt ...
		/// Jira number is TE-2064 (also part of TE-996)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleUSFMStyleFootnotes_FirstOneHasSequence()
		{
			CheckDisposed();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a footnote, no end marker, terminated by another footnote
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment(" ", @"\p");
			// Verse 1: footnote with auto-numbered marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("Verse 1 start...", @"\v");
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("Footnote 1 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 1 end. ", @"\vt");

			// Verse 2: footnote with custom marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("Verse 2 start...", @"\v");
			m_importer.ProcessSegment("* ", @"\f");
			m_importer.ProcessSegment("1.2 ", @"\fr");
			m_importer.ProcessSegment("Footnote 2 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 2 end. ", @"\vt");

			// Verse 3: footnote with no marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			m_importer.ProcessSegment("Verse 3 start...", @"\v");
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("1.3 ", @"\fr");
			m_importer.ProcessSegment("Footnote 3 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 3 end. ", @"\vt");

			// Verse 4: footnote with missing marker specification should fall back to default
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 4);
			m_importer.ProcessSegment("Verse 4 start...", @"\v");
			m_importer.ProcessSegment("- ", @"\f");
			m_importer.ProcessSegment("1.4 ", @"\fr");
			m_importer.ProcessSegment("Footnote 4 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 4 end. ", @"\vt");

			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedSavedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Verse 1 start..." + StringUtils.kchObject.ToString() + " ...verse 1 end. " +
				"2Verse 2 start..." + StringUtils.kchObject.ToString() + " ...verse 2 end. " +
				"3Verse 3 start..." + StringUtils.kchObject.ToString() + " ...verse 3 end. " +
				"4Verse 4 start..." + StringUtils.kchObject.ToString() + " ...verse 4 end.",
				para.Contents.Text);
			Assert.AreEqual("a", m_scr.GeneralFootnoteMarker);
			VerifySimpleFootnote(0, "Footnote 1 text", "a");
			VerifySimpleFootnote(1, "Footnote 2 text", "a");
			VerifySimpleFootnote(2, "Footnote 3 text", "a");
			VerifySimpleFootnote(3, "Footnote 4 text", "a");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		/// \v 1 ... \f * \ft ...\vt ...
		/// \v 2 ... \f * \fr 1.2 \ft ...\vt ...
		/// \v 3 ... \f + \fr 1.3 \ft ...\vt ...
		/// \v 4 ... \f * \fr 1.3 \ft ...\vt ...
		/// Jira number is TE-2064 (also part of TE-996)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleUSFMStyleFootnotes_FirstOneHasLiteralCaller()
		{
			CheckDisposed();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a footnote, no end marker, terminated by another footnote
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment(" ", @"\p");
			// Verse 1: footnote with auto-numbered marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("Verse 1 start...", @"\v");
			m_importer.ProcessSegment("* ", @"\f");
			m_importer.ProcessSegment("Footnote 1 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 1 end. ", @"\vt");

			// Verse 2: footnote with custom marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("Verse 2 start...", @"\v");
			m_importer.ProcessSegment("- ", @"\f");
			m_importer.ProcessSegment("1.2 ", @"\fr");
			m_importer.ProcessSegment("Footnote 2 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 2 end. ", @"\vt");

			// Verse 3: footnote with no marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			m_importer.ProcessSegment("Verse 3 start...", @"\v");
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("1.3 ", @"\fr");
			m_importer.ProcessSegment("Footnote 3 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 3 end. ", @"\vt");

			// Verse 4: footnote with missing marker specification should fall back to default
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 4);
			m_importer.ProcessSegment("Verse 4 start...", @"\v");
			m_importer.ProcessSegment("^ ", @"\f");
			m_importer.ProcessSegment("1.4 ", @"\fr");
			m_importer.ProcessSegment("Footnote 4 text ", @"\ft");
			m_importer.ProcessSegment(" ...verse 4 end. ", @"\vt");

			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedSavedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Verse 1 start..." + StringUtils.kchObject.ToString() + " ...verse 1 end. " +
				"2Verse 2 start..." + StringUtils.kchObject.ToString() + " ...verse 2 end. " +
				"3Verse 3 start..." + StringUtils.kchObject.ToString() + " ...verse 3 end. " +
				"4Verse 4 start..." + StringUtils.kchObject.ToString() + " ...verse 4 end.",
				para.Contents.Text);
			Assert.AreEqual("*", m_scr.GeneralFootnoteMarker);
			VerifySimpleFootnote(0, "Footnote 1 text", "*");
			VerifySimpleFootnote(1, "Footnote 2 text", "*");
			VerifySimpleFootnote(2, "Footnote 3 text", "*");
			VerifySimpleFootnote(3, "Footnote 4 text", "*");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		/// \v 1 ... \f + \ft ...\f* ...
		/// \v 2 ... \f * \fr 1.2 \ft ...\f* ...
		/// \v 3 ... \f - \fr 1.3 \ft ...\f* ...
		/// \v 4 ... \f \fr 1.3 \ft ...\f* ...
		/// Jira number is TE-2064 (also part of TE-996)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleUSFMStyleFootnotes_StripAndIgnoreCallers()
		{
			CheckDisposed();

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a footnote, no end marker, terminated by another footnote
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment(" ", @"\p");
			// Verse 1: footnote with auto-numbered marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("Verse 1 start...", @"\v");
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("Footnote 1 text", @"\ft");
			m_importer.ProcessSegment(" ...verse 1 end. ", @"\f*");

			// Verse 2: footnote with custom marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("Verse 2 start...", @"\v");
			m_importer.ProcessSegment("* ", @"\f");
			m_importer.ProcessSegment("1.2 ", @"\fr");
			m_importer.ProcessSegment("Footnote 2 text", @"\ft");
			m_importer.ProcessSegment(" ...verse 2 end. ", @"\f*");

			// Verse 3: footnote with no marker
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			m_importer.ProcessSegment("Verse 3 start...", @"\v");
			m_importer.ProcessSegment("- ", @"\f");
			m_importer.ProcessSegment("1.3 ", @"\fr");
			m_importer.ProcessSegment("Footnote 3 text", @"\ft");
			m_importer.ProcessSegment(" ...verse 3 end. ", @"\f*");

			// Verse 4: footnote with missing marker specification should fall back to default
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 4);
			m_importer.ProcessSegment("Verse 4 start...", @"\v");
			m_importer.ProcessSegment(" ", @"\f");
			m_importer.ProcessSegment("1.4 ", @"\fr");
			m_importer.ProcessSegment("Footnote 4 text", @"\ft");
			m_importer.ProcessSegment(" ...verse 4 end.", @"\f*");

			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedSavedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Verse 1 start..." + StringUtils.kchObject.ToString() + " ...verse 1 end. " +
				"2Verse 2 start..." + StringUtils.kchObject.ToString() + " ...verse 2 end. " +
				"3Verse 3 start..." + StringUtils.kchObject.ToString() + " ...verse 3 end. " +
				"4Verse 4 start..." + StringUtils.kchObject.ToString() + " ...verse 4 end.",
				para.Contents.Text);
			VerifySimpleFootnote(0, "Footnote 1 text", "a");
			VerifySimpleFootnote(1, "Footnote 2 text", "a");
			VerifySimpleFootnote(2, "Footnote 3 text", "a");
			VerifySimpleFootnote(3, "Footnote 4 text", "a");
		}
		#endregion

		#region Import Annotations Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import an annotation stream NOT interleaved with the Scripture
		/// text. This imports some notes corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    Vern:  id mt s p c1 v1 v2 p v3 s p v4 p v5
		///    Notes: id rem c1 v1 rem v2 rem rem v4 rem v5 rem
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnnotationNonInterleaved_Simple()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = false;
			m_importer.Settings.ImportBookIntros = true;
			m_importer.Settings.ImportAnnotations = true;

			// Set up the vernacular Scripture
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Titulo ", @"\mt");
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Segundo versiculo ", @"\v");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 3);
			m_importer.ProcessSegment("Tercer versiculo ", @"\v");
			m_importer.ProcessSegment("Segunda Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 4);
			m_importer.ProcessSegment("Cuarto versiculo ", @"\v");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 5);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 5);
			m_importer.ProcessSegment("Quinto versiculo ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 6);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 6);
			m_importer.ProcessSegment("Sexto versiculo ", @"\v");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// make sure there are no notes before we start importing them
			FdoOwningSequence<IScrScriptureNote> notes = m_scr.BookAnnotationsOS[0].NotesOS;
			Assert.AreEqual(0, notes.Count);

			// Now test ability to import a non-interleaved Annotation stream
			m_importer.CurrentImportDomain = ImportDomain.Annotations;
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			Assert.AreEqual(genesis.Hvo, m_importer.UndoInfo.ImportedSavedVersion.BooksOS[0].Hvo,
				"The id line in the notes file should not cause a new ScrBook to get created.");
			Assert.AreEqual(1, m_importer.UndoInfo.ImportedSavedVersion.BooksOS.Count,
				"The id line in the notes file should not cause a new ScrBook to get created.");
			m_importer.ProcessSegment("Note before Scripture text", @"\rem");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Note for verse 1", @"\rem");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("First note for verse 2", @"\rem");
			m_importer.ProcessSegment("Second note for verse 2", @"\rem");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 4);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Note for verse 4", @"\rem");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 5);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 5);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Note for verse 5", @"\rem");

			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 6);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 6);
			m_importer.ProcessSegment("Note for verse 6", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// minor sanity checks
			Assert.AreEqual(2, genesis.SectionsOS.Count);
			Assert.AreEqual(1, genesis.TitleOA.ParagraphsOS.Count);

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Primera Seccion", ((StTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para11 = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo 2Segundo versiculo", para11.Contents.Text);
			IStTxtPara para12 = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("3Tercer versiculo", para12.Contents.Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Segunda Seccion", ((StTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para21 = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("4Cuarto versiculo", para21.Contents.Text);
			IStTxtPara para22 = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("5Quinto versiculo 6Sexto versiculo", para22.Contents.Text);

			// look at the annotations and see if they are associated to the correct paragraphs
			//notes = m_scr.BookAnnotationsOS[0].NotesOS;
			Assert.AreEqual(7, notes.Count);

			// Check stuff that's common to all notes
			foreach (IScrScriptureNote annotation in notes)
			{
				Assert.AreEqual(NoteType.Translator, annotation.AnnotationType);
				Assert.AreEqual(annotation.BeginObjectRAHvo, annotation.EndObjectRAHvo);
				// REVIEW: Should we try to find the actual offset of the annotated verse in the para?
				Assert.AreEqual(0, annotation.BeginOffset);
				Assert.AreEqual(annotation.BeginOffset, annotation.EndOffset);
				Assert.AreEqual(annotation.BeginRef, annotation.EndRef);
			}
			IScrScriptureNote note = notes[0];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Note before Scripture text", m_wsAnal);
			Assert.AreEqual(genesis.Hvo, note.BeginObjectRAHvo);
			Assert.AreEqual(1001000, note.BeginRef);

			note = notes[1];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Note for verse 1", m_wsAnal);
			Assert.AreEqual(para11.Hvo, note.BeginObjectRAHvo);
			Assert.AreEqual(1001001, note.BeginRef);

			note = notes[2];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "First note for verse 2", m_wsAnal);
			Assert.AreEqual(para11.Hvo, note.BeginObjectRAHvo);
			Assert.AreEqual(1001002, note.BeginRef);

			note = notes[3];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Second note for verse 2", m_wsAnal);
			Assert.AreEqual(para11.Hvo, note.BeginObjectRAHvo);
			Assert.AreEqual(1001002, note.BeginRef);

			note = notes[4];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Note for verse 4", m_wsAnal);
			Assert.AreEqual(para21.Hvo, note.BeginObjectRAHvo);
			Assert.AreEqual(1001004, note.BeginRef);

			note = notes[5];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Note for verse 5", m_wsAnal);
			Assert.AreEqual(para22.Hvo, note.BeginObjectRAHvo);
			Assert.AreEqual(1001005, note.BeginRef);

			note = notes[6];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Note for verse 6", m_wsAnal);
			Assert.AreEqual(para22.Hvo, note.BeginObjectRAHvo);
			Assert.AreEqual(1001006, note.BeginRef);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import an annotation stream interleaved with the Scripture
		/// text, without actually importing the Scripture text itself.
		/// This imports some notes for the book of Genesis, even though Genesis does not exist
		/// as a transalted book. We will process this marker sequence:
		///    id c1 em...em* v2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnnotationNonInterleaved_StartWithCharacterMapping()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = false;
			m_importer.Settings.ImportBookIntros = true;    // required for code to process first character mapping
			m_importer.Settings.ImportAnnotations = true;

			// Set up the domain and starting reference
			m_importer.CurrentImportDomain = ImportDomain.Annotations;
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// Since we aren't importing Scripture, we shouldn't have created Genesis.
			Assert.IsNull(m_importer.ScrBook);
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Emphatically first note", @"\em");
			m_importer.ProcessSegment(" remaining text", @"\em*");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Second note", @"\v");
			m_importer.FinalizeImport();
			Assert.IsNull(m_importer.UndoInfo.ImportedSavedVersion);

			// look at the annotation and see if it is associated to the correct Scripture reference
			FdoOwningSequence<IScrScriptureNote> notes = m_scr.BookAnnotationsOS[0].NotesOS;
			Assert.AreEqual(2, notes.Count);

			IScrScriptureNote annotation = notes[0];
			Assert.AreEqual(NoteType.Translator, annotation.AnnotationType);
			Assert.AreEqual(0, annotation.BeginObjectRAHvo);
			Assert.AreEqual(0, annotation.EndObjectRAHvo);
			Assert.AreEqual(0, annotation.BeginOffset);
			Assert.AreEqual(0, annotation.EndOffset);
			Assert.AreEqual(1001001, annotation.BeginRef);
			Assert.AreEqual(1001001, annotation.EndRef);
			Assert.AreEqual(1, annotation.DiscussionOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)annotation.DiscussionOA.ParagraphsOS[0];
			ITsString tssDiscussionP1 = para.Contents.UnderlyingTsString;
			Assert.AreEqual(2, tssDiscussionP1.RunCount);
			AssertEx.RunIsCorrect(tssDiscussionP1, 0, "Emphatically first note", "Emphasis", m_wsAnal);
			AssertEx.RunIsCorrect(tssDiscussionP1, 1, " remaining text", null, m_wsAnal);

			annotation = notes[1];
			Assert.AreEqual(NoteType.Translator, annotation.AnnotationType);
			Assert.AreEqual(0, annotation.BeginObjectRAHvo);
			Assert.AreEqual(0, annotation.EndObjectRAHvo);
			Assert.AreEqual(0, annotation.BeginOffset);
			Assert.AreEqual(0, annotation.EndOffset);
			Assert.AreEqual(1001002, annotation.BeginRef);
			Assert.AreEqual(1001002, annotation.EndRef);
			m_importer.VerifyAnnotationText(annotation.DiscussionOA, "Discussion", "Second note", m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import an annotation where the first annotation begins with a character style rather
		/// than a verse number or paragraph style.
		/// For simplicity, test is built on test that only imported annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnnotationInterleaved_DontImportScripture()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = false;
			m_importer.Settings.ImportBookIntros = false;
			m_importer.Settings.ImportAnnotations = true;

			// Set up the vernacular Scripture
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			// Since we aren't importing Scripture, we shouldn't have created Genesis.
			Assert.IsNull(m_importer.ScrBook);
			m_importer.ProcessSegment("Genesis ", @"\h");
			m_importer.ProcessSegment("Titulo ", @"\mt");
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			m_importer.ProcessSegment("User-supplied picture|junk.jpg|col|GEN 1--1||Primer subtitulo para junk1.jpg| ", @"\fig");
			m_importer.ProcessSegment("Note for verse 1", @"\rem");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Segundo versiculo ", @"\v");
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("Some footnote text ", @"\ft");
			m_importer.ProcessSegment(" ", @"\f*");
			m_importer.FinalizeImport();
			Assert.IsNull(m_importer.UndoInfo.ImportedSavedVersion);

			// look at the annotation and see if it is associated to the correct Scripture reference
			FdoOwningSequence<IScrScriptureNote> notes = m_scr.BookAnnotationsOS[0].NotesOS;
			Assert.AreEqual(1, notes.Count);

			IScrScriptureNote annotation = notes[0];
			Assert.AreEqual(NoteType.Translator, annotation.AnnotationType);
			Assert.AreEqual(0, annotation.BeginObjectRAHvo);
			Assert.AreEqual(0, annotation.EndObjectRAHvo);
			Assert.AreEqual(0, annotation.BeginOffset);
			Assert.AreEqual(0, annotation.EndOffset);
			Assert.AreEqual(1001001, annotation.BeginRef);
			Assert.AreEqual(1001001, annotation.EndRef);
			m_importer.VerifyAnnotationText(annotation.DiscussionOA, "Discussion", "Note for verse 1", m_wsAnal);
		}
		#endregion

		#region Back Translation tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    Vern: id mt s p c1 v1 v2 p v3 s p v4
		///    BT: id mt s p c1 v1 v2 p v3 s p v4
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_Simple()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Titulo ", @"\mt");
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Segundo versiculo ", @"\v");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 3);
			m_importer.ProcessSegment("Tercer versiculo ", @"\v");
			m_importer.ProcessSegment("Segunda Seccion ", @"\s");
			m_importer.ProcessSegment("(Algunos manuscritos no conienen este pasaje.) ", @"\s2");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 4);
			m_importer.ProcessSegment("Cuarto versiculo ", @"\v");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("Title ", @"\mt");
			m_importer.ProcessSegment("First Section ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Second verse ", @"\v");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 3);
			m_importer.ProcessSegment("Third verse ", @"\v");
			m_importer.ProcessSegment("Second Section ", @"\s");
			m_importer.ProcessSegment("(Some manuscripts don't have this passage.) ", @"\s2");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 4);
			m_importer.ProcessSegment("Fourth verse ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			Assert.AreEqual(1, genesis.TitleOA.ParagraphsOS.Count);
			IStTxtPara titlePara = (IStTxtPara)genesis.TitleOA.ParagraphsOS[0];
			Assert.AreEqual(1, titlePara.TranslationsOC.Count);
			ICmTranslation titleTranslation = titlePara.GetBT();
			Assert.AreEqual("Title",
				titleTranslation.Translation.GetAlternative(m_wsAnal).Text);

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("First Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo 2Segundo versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse 2Second verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("3Tercer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("3Third verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[1];
			Assert.AreEqual("(Algunos manuscritos no conienen este pasaje.)", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("(Some manuscripts don't have this passage.)",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("4Cuarto versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("4Fourth verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import simple BT that starts with a marker mapped to default
		/// paragraph characters (after the id line). We will process this marker sequence:
		///    Vern: id mt s p c1 v1 v2
		///    BT: id nt mt s p c1 v1 v2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(ExceptionType=typeof(ScriptureUtilsException),
		   ExpectedMessage = "Back translation not part of a paragraph:\r\n" +
			"\tThis is default paragraph characters \r\n" +
			"\t(Style: Default Paragraph Characters)\r\n" +
			"Attempting to read GEN")]
		public void BackTranslationNonInterleaved_DefaultParaCharsStart()
		{
			CheckDisposed();
			((ScrImportSet)m_importer.Settings).SetMapping(MappingSet.Main,
				new ImportMappingInfo(@"\nt", MarkerDomain.Default,
				"Default Paragraph Characters", null, null));
			m_importer.Initialize();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Titulo ", @"\mt");
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Segundo versiculo ", @"\v");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("This is default paragraph characters ", @"\nt");
			m_importer.ProcessSegment("Title ", @"\mt");
			m_importer.ProcessSegment("First Section ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Second verse ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			Assert.AreEqual(1, genesis.TitleOA.ParagraphsOS.Count);
			IStTxtPara titlePara = (IStTxtPara)genesis.TitleOA.ParagraphsOS[0];
			Assert.AreEqual(1, titlePara.TranslationsOC.Count);
			ICmTranslation titleTranslation =
				new CmTranslation(Cache, titlePara.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("Title",
				titleTranslation.Translation.GetAlternative(m_wsAnal).Text);

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation =
				new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("First Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo 2Segundo versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("11First verse 2Second verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    Vern: id mt c1 s r p v1
		///    BT: id mt c1 s r p v1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_ParallelPassage()
		{
			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Titulo ", @"\mt");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("(Lc. 3.23-38)", @"\r");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("Title ", @"\mt");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("First Section ", @"\s");
			m_importer.ProcessSegment("(Lc. 3.23-38)", @"\r");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			Assert.AreEqual(1, genesis.TitleOA.ParagraphsOS.Count);
			IStTxtPara titlePara = (IStTxtPara)genesis.TitleOA.ParagraphsOS[0];
			Assert.AreEqual(1, titlePara.TranslationsOC.Count);
			ICmTranslation titleTranslation =
				new CmTranslation(Cache, titlePara.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("Title",
				titleTranslation.Translation.GetAlternative(m_wsAnal).Text);

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation =
				new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("First Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[1];
			Assert.AreEqual("(Lc. 3.23-38)", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation =
				new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("(Lc. 3.23-38)",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("11First verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    Vern: id mt c1 s r p v1
		///    BT: id mt c1 s r p v1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_ParallelPassage_BtOnly()
		{
			// Setup book
			IScrBook genesis = (IScrBook)m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(genesis.Hvo, "Genesis");
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo, "Primera Seccion",
				ScrStyleNames.SectionHead);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo, "(Lc. 3.23-38)",
				"Parallel Passage Reference");
			StTxtPara para11 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "Primer versiculo", null);
			section1.AdjustReferences();

			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = true;

			// Set up the vernacular Scripture - this will be ignored
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Titulo ", @"\mt");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("(Lc. 3.23-38)", @"\r");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("Title ", @"\mt");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("First Section ", @"\s");
			m_importer.ProcessSegment("(Lc. 3.23-38)", @"\r");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			Assert.AreEqual(1, genesis.TitleOA.ParagraphsOS.Count);
			IStTxtPara titlePara = (IStTxtPara)genesis.TitleOA.ParagraphsOS[0];
			Assert.AreEqual(1, titlePara.TranslationsOC.Count);
			ICmTranslation titleTranslation =
				new CmTranslation(Cache, titlePara.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("Title",
				titleTranslation.Translation.GetAlternative(m_wsAnal).Text);

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation =
				new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("First Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[1];
			Assert.AreEqual("(Lc. 3.23-38)", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation =
				new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("(Lc. 3.23-38)",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			Assert.AreEqual("11First verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text. This tests when there is no scripture book for the back translation
		///    BT: id
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(ExceptionType = typeof(ScriptureUtilsException),
			ExpectedMessage = "No corresponding vernacular book for back translation.\r\nAttempting to read GEN")]
		public void BackTranslationNonInterleaved_NoCorrespondingBook()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** finalize **************
			m_importer.FinalizeImport();
			// Shouldn't get here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-4427: Test fix for problem where same chapter number is repeated in back
		/// translation during import. This test uses a single back-translation NOT interleaved
		/// with the scripture text. We will process this marker sequence:
		///    Vern: id mt c1 p v2 vt
		///    BT: id mt c1 p v2 vt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_RepeatedChapterNum()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// Set up the vernacular Scripture
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("MAT", @"\id");
			m_importer.ProcessSegment("Matthew", @"\mt");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("1", @"\c");
			m_importer.ProcessSegment("Primer versiculo ", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			//m_importer.ProcessSegment("2", @"\v");
			m_importer.ProcessSegment("Segundo versiculo ", @"\v");
			IScrBook genesis = m_importer.ScrBook;

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("MAT", @"\id");
			m_importer.ProcessSegment("Matthew", @"\mt");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("1", @"\c");
			m_importer.ProcessSegment("First verse ", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			//m_importer.ProcessSegment("2", @"\v");
			m_importer.ProcessSegment("Second verse ", @"\v");
			Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Check section contents
			Assert.AreEqual(1, genesis.SectionsOS.Count);
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(0, para.TranslationsOC.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("1Primer versiculo 2Segundo versiculo",
				para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("1First verse 2Second verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    Vern: id c1 v1 v2 v3 v4
		///    BT: id c1 v1 v2 v3 v4
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_NoParaMarker()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Segundo versiculo ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 3);
			m_importer.ProcessSegment("Tercer versiculo ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 4);
			m_importer.ProcessSegment("Cuarto versiculo ", @"\v");
			IScrBook genesis = m_importer.ScrBook;

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Second verse ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 3);
			m_importer.ProcessSegment("Third verse ", @"\v");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 4);
			m_importer.ProcessSegment("Fourth verse ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Check section contents
			Assert.AreEqual(1, genesis.SectionsOS.Count);
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(0, para.TranslationsOC.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo 2Segundo versiculo 3Tercer versiculo 4Cuarto versiculo",
				para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("11First verse 2Second verse 3Third verse 4Fourth verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import two back-translations NOT interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the books of II and III John. We will process this marker sequence:
		///    Vern: id p v1 id p v1
		///    BT: id p v1 id p v1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_TwoBooks()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// Set up the vernacular Scripture for 2 John
			m_importer.TextSegment.FirstReference = new BCVRef(63, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(63, 1, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.TextSegment.FirstReference = new BCVRef(63, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(63, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			IScrBook john2 = m_importer.ScrBook;

			// Set up the vernacular Scripture for 3 John
			m_importer.TextSegment.FirstReference = new BCVRef(64, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(64, 1, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.TextSegment.FirstReference = new BCVRef(64, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(64, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			IScrBook john3 = m_importer.ScrBook;

			// import a non-interleaved BT for II John
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.TextSegment.FirstReference = new BCVRef(63, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(63, 1, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(john2.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");
			m_importer.TextSegment.FirstReference = new BCVRef(63, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(63, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");

			// import a non-interleaved BT for III John
			m_importer.TextSegment.FirstReference = new BCVRef(64, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(64, 1, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(john3.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");
			m_importer.TextSegment.FirstReference = new BCVRef(64, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(64, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Check II John
			Assert.AreEqual(1, john2.SectionsOS.Count);
			IScrSection section = john2.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(0, para.TranslationsOC.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("1Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("1First verse", translation.Translation.GetAlternative(m_wsAnal).Text);

			// Check III John
			Assert.AreEqual(1, john3.SectionsOS.Count);
			section = john3.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(0, para.TranslationsOC.Count);
			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("1Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("1First verse", translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    Vern: id is ip ip s c1 p v1
		///    BT: id is ip ip s c1 p v1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_Intros()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Primera Seccion ", @"\is");
			m_importer.ProcessSegment("Que bueno que decidiste leer este libro de la Biblia.", @"\ip");
			m_importer.ProcessSegment("A mi me gusta este libro tambien.", @"\ip");
			m_importer.ProcessSegment("Segunda Seccion ", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("First Section ", @"\is");
			m_importer.ProcessSegment("How good that you decided to read this book of the Bible.", @"\ip");
			m_importer.ProcessSegment("I like this book, too.", @"\ip");
			m_importer.ProcessSegment("Second Section ", @"\s");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(2, genesis.SectionsOS.Count); // insanity check?

			Assert.AreEqual(1, genesis.TitleOA.ParagraphsOS.Count);

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("First Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("Que bueno que decidiste leer este libro de la Biblia.", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("How good that you decided to read this book of the Bible.",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("A mi me gusta este libro tambien.", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("I like this book, too.",
				translation.Translation.GetAlternative(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    Vern: id s p c1 v1 q c2 s
		///    BT: id s p c1 v1 q c2 s
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_ScrParaWithNoVerseNumber()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			m_importer.ProcessSegment("Segunda estrofa", @"\q");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 2, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 2, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("Segunda Seccion", @"\s");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("First Section ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");
			m_importer.ProcessSegment("Second stanza", @"\q");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 2, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 2, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("Second Section", @"\s");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("First Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("Segunda estrofa", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second stanza",
				translation.Translation.GetAlternative(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    Vern: id s p c1 v1 p v2 q q2 q2 c2 s
		///    BT: id s p c1 v1 p v2 q q2 q2 c2 s
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_VerseInMultipleParagraphs()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Segunda versiculo ", @"\v");
			m_importer.ProcessSegment("Segunda estrofa", @"\q");
			m_importer.ProcessSegment("", @"\q");
			m_importer.ProcessSegment("", @"\q");
			m_importer.ProcessSegment("Dritte Strophe", @"\q2");
			m_importer.ProcessSegment("Vierte Strophe", @"\q2");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 2, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 2, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("Segunda Seccion", @"\s");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("First Section ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Second verse ", @"\v");
			m_importer.ProcessSegment("Second stanza", @"\q");
			m_importer.ProcessSegment("", @"\q");
			m_importer.ProcessSegment("", @"\q");
			m_importer.ProcessSegment("next part of verse", @"\q2");
			m_importer.ProcessSegment("last part of verse", @"\q2");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 2, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 2, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("Second Section", @"\s");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("First Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(5, section.ContentOA.ParagraphsOS.Count);
			// paragraph 1
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			// paragraph 2
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("2Segunda versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("2Second verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			// paragraph 3
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[2];
			Assert.AreEqual("Segunda estrofa", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second stanza",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			// paragraph 4
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[3];
			Assert.AreEqual("Dritte Strophe", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("next part of verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			// paragraph 5
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[4];
			Assert.AreEqual("Vierte Strophe", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("last part of verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    Vern: id s p c1 v1 q q v2 q
		///    BT: id s p c1 v1 q q v2 q
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_EmptyLastPara()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo ", @"\v");
			m_importer.ProcessSegment("Segunda estrofa ", @"\q");
			m_importer.ProcessSegment("", @"\q");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Segunda versiculo ", @"\v");
			m_importer.ProcessSegment("", @"\q");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("First Section ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");
			m_importer.ProcessSegment("Second stanza ", @"\q");
			m_importer.ProcessSegment("", @"\q");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Second verse ", @"\v");
			m_importer.ProcessSegment("", @"\q");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("First Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(3, section.ContentOA.ParagraphsOS.Count);

			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);

			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("Segunda estrofa", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second stanza",
				translation.Translation.GetAlternative(m_wsAnal).Text);

			para = (IStTxtPara)section.ContentOA.ParagraphsOS[2];
			Assert.AreEqual("2Segunda versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("2Second verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text containing footnotes. This imports some BT material corresponding to the
		/// existing vernacular Scripture in the book of Genesis. We will process this marker
		/// sequence:
		///    Vern: id s p c1 v1 f v2 f p v3 x s p v4 f
		///    BT: id s p c1 v1 f v2 f p v3 s p v4 f
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_Footnotes()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo", @"\v");
			m_importer.ProcessSegment("- Primer pata nota", @"\f");
			m_importer.ProcessSegment(" ", @"\f*");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Segundo versiculo", @"\v");
			m_importer.ProcessSegment("Segunda pata nota", @"\f");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 3);
			m_importer.ProcessSegment("Tercer versiculo", @"\v");
			m_importer.ProcessSegment("Gal 3:2 ", @"\x");
			m_importer.ProcessSegment(" ", @"\x*");
			m_importer.ProcessSegment("Segunda Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 4);
			m_importer.ProcessSegment("Cuarto versiculo", @"\v");
			m_importer.ProcessSegment("Ultima pata nota", @"\f");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("First Section ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");
			m_importer.ProcessSegment("- ", @"\f");
			m_importer.ProcessSegment("1.1 ", @"\fr"); // This should be ignored (TE-3933)
			m_importer.ProcessSegment("First footnote", @"\ft");
			m_importer.ProcessSegment(" ", @"\f*");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Second verse ", @"\v");
			m_importer.ProcessSegment("Second footnote", @"\f");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 3);
			m_importer.ProcessSegment("Third verse ", @"\v");
			m_importer.ProcessSegment("Second Section ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 4);
			m_importer.ProcessSegment("Fourth verse ", @"\v");
			m_importer.ProcessSegment("Last footnote", @"\f");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("First Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo" + StringUtils.kchObject +
				" 2Segundo versiculo" + StringUtils.kchObject, para.Contents.Text);
			VerifyFootnoteWithTranslation(0, "Primer pata nota", "First footnote", string.Empty,
				ScrStyleNames.NormalFootnoteParagraph);
			VerifyFootnoteWithTranslation(1, "Segunda pata nota", "Second footnote", string.Empty,
				ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse" + StringUtils.kchObject +
				" 2Second verse" + StringUtils.kchObject,
				translation.Translation.GetAlternative(m_wsAnal).Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("3Tercer versiculo" + StringUtils.kchObject, para.Contents.Text);
			VerifyFootnoteWithTranslation(2, "Gal 3:2", null, string.Empty,
				"Note Cross-Reference Paragraph");
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("3Third verse",
				translation.Translation.GetAlternative(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second Section", translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("4Cuarto versiculo" + StringUtils.kchObject, para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("4Fourth verse" + StringUtils.kchObject,
				translation.Translation.GetAlternative(m_wsAnal).Text);
			VerifyFootnoteWithTranslation(3, "Ultima pata nota", "Last footnote", string.Empty,
				ScrStyleNames.NormalFootnoteParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case when only the back translation and annotations are imported and there
		/// is a footnote in both the vernacular (which is processed to import any annotations
		/// it might contain) and the BT. TE-7983: failed because of bogus style proxy created
		/// for \f* when processing verncular.
		/// We will process this marker sequence:
		///    id c1 s p v1 f
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BtFootnoteWhenNotImportingVernacular()
		{
			m_scrInMemoryCache.InitializeScrPublications();

			// Set up Scripture to correspond with the back translation to be imported.
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Section 1",
				ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse one text", null);
			// Add a footnote with a character style.
			int ichFootnote = para.Contents.Length - 5;
			StFootnote noteOne = m_scrInMemoryCache.AddFootnote(exodus, para, ichFootnote,
				"vernacular text for footnote"); // footnote after "one" in verse 1
			ICmTranslation noteOneTrans = ((StTxtPara)noteOne.ParagraphsOS[0]).GetOrCreateBT();
			section.AdjustReferences();

			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportAnnotations = true;

			// ************* Start with the vernacular, which should get skipped ****************
			m_importer.CurrentImportDomain = ImportDomain.Main;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = new ScrBook(Cache, m_importer.ScrBook.Hvo);
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a section head *********************
			m_importer.ProcessSegment("Section One for Exodus (skipped)", @"\s");

			// ****** process a new BT paragraph with footnotes **********
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("verse one vernacular text (skipped)", @"\v");
			m_importer.ProcessSegment("+ This is a footnote (skipped)", @"\f");
			m_importer.ProcessSegment(" more text (skipped)", @"\f*");

			// ************* Now process the back translation ****************
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a section head *********************
			m_importer.ProcessSegment("BT of Section One for Exodus", @"\s");

			// ****** process a new BT paragraph with footnotes **********
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("verse one BT text", @"\v");
			m_importer.ProcessSegment("+ BT text for footnote one.", @"\f");
			m_importer.ProcessSegment(" more BT text", @"\f*");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Check the BT of these two paragraphs
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans1 = para.GetBT();
			Assert.IsNotNull(trans1);
			ITsString tssBt = trans1.Translation.AnalysisDefaultWritingSystem.UnderlyingTsString;
			Assert.AreEqual(5, tssBt.RunCount);
			AssertEx.RunIsCorrect(tssBt, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBt, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBt, 2, "verse one BT text", null, m_wsAnal);

			Guid guid1 = StringUtils.GetGuidFromRun(tssBt, 3);
			int hvoFootnote = Cache.GetIdFromGuid(guid1);
			Assert.AreEqual(noteOneTrans.OwnerHVO,
				new StFootnote(Cache, hvoFootnote).ParagraphsOS[0].Hvo,
				"The first imported BT footnote should be owned by paragraph in the first footnote but isn't");

			VerifyFootnoteWithTranslation(0, "vernacular text for footnote",
				"BT text for footnote one.", null, ScrStyleNames.NormalFootnoteParagraph);
			AssertEx.RunIsCorrect(tssBt, 4, " more BT text", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case when only the back translation and annotations are imported.
		/// TE-8102: failed because of bogus style proxy created for \bk* when processing verncular.
		/// We will process this marker sequence:
		///    id is ip bk bk* c1 s p v1 bk bk*
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BtFootnoteWhenNotImportingVernacular_CharStyleUsedTwice()
		{
			m_scrInMemoryCache.InitializeScrPublications();

			// Set up Scripture to correspond with the back translation to be imported.
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			IScrSection introSection = m_scrInMemoryCache.AddIntroSectionToMockedBook(exodus.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(introSection.Hvo, "Intro Section",
				ScrStyleNames.IntroSectionHead);
			m_scrInMemoryCache.AddParaToMockedSectionContent(introSection.Hvo,
				ScrStyleNames.IntroParagraph);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Section 1",
				ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse one text", null);
			section.AdjustReferences();

			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportAnnotations = true;

			// ************* Start with the vernacular, which should get skipped ****************
			m_importer.CurrentImportDomain = ImportDomain.Main;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = new ScrBook(Cache, m_importer.ScrBook.Hvo);
			Assert.AreEqual("EXO", book.BookId);

			// ************** process an intro section *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\is");

			// ************** process an intro paragraph *********************
			m_importer.ProcessSegment("", @"\ip");
			m_importer.ProcessSegment("This is my character style ", @"\bk");
			m_importer.ProcessSegment("After the character style ", @"\bk*");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a section head *********************
			m_importer.ProcessSegment("Section One for Exodus (skipped)", @"\s");

			// ****** process a new BT paragraph with footnotes **********
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("verse one vernacular text (skipped)", @"\v");
			m_importer.ProcessSegment("Character style in main text (skipped)", @"\bk");
			m_importer.ProcessSegment(" more text (skipped)", @"\bk*");

			// ************* Now process the back translation ****************
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);

			// ************** process an intro section *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\is");

			// ************** process an intro paragraph *********************
			m_importer.ProcessSegment("", @"\ip");
			m_importer.ProcessSegment("This is my character style in BT ", @"\bk");
			m_importer.ProcessSegment("After the character style in BT ", @"\bk*");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a section head *********************
			m_importer.ProcessSegment("Section One BT for Exodus", @"\s");

			// ****** process a new BT paragraph with footnotes **********
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("verse one BT text", @"\v");
			m_importer.ProcessSegment("Character style in BT text", @"\bk");
			m_importer.ProcessSegment(" more text in BT", @"\bk*");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Check the BT of these two paragraphs
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans1 = para.GetBT();
			Assert.IsNotNull(trans1);
			ITsString tssBt = trans1.Translation.AnalysisDefaultWritingSystem.UnderlyingTsString;
			Assert.AreEqual(5, tssBt.RunCount);
			AssertEx.RunIsCorrect(tssBt, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBt, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBt, 2, "verse one BT text", null, m_wsAnal);
			AssertEx.RunIsCorrect(tssBt, 3, "Character style in BT text", ScrStyleNames.BookTitleInText, m_wsAnal);
			AssertEx.RunIsCorrect(tssBt, 4, " more text in BT", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case when only the back translation is imported and we have multiple footnotes.
		/// TE-7428 and TE-6327.
		/// We will process this marker sequence:
		///    id mt c1 s p v1 rem v2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_WithInterleavedAnnotation()
		{
			CheckDisposed();

			// Set up the vernacular Scripture
			IScrBook genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(genesis.Hvo, "Genesis");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "My head hurts", ScrStyleNames.SectionHead);
			StTxtPara para = AddPara(section);
			AddVerse(para, 1, 1, "This is verse one.");
			AddVerse(para, 0, 2, "This is verse two.");
			section.AdjustReferences();

			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportAnnotations = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");
			m_importer.ProcessSegment("Beginning ", @"\mt");
			m_importer.ProcessSegment("Div One ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("In the beginning ", @"\v");
			m_importer.ProcessSegment("This is my discussion of the first verse. ", @"\rem");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("Then came the end ", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			// Check BT
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			ITsString tssTrans = translation.Translation.GetAlternative(m_wsAnal).UnderlyingTsString;
			Assert.AreEqual(5, tssTrans.RunCount);
			AssertEx.RunIsCorrect(tssTrans, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 2, "In the beginning ", null, m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 3, "2", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 4, "Then came the end", null, m_wsAnal);

			Assert.AreEqual(1, m_scr.BookAnnotationsOS[0].NotesOS.Count);
			FdoOwningSequence<IStPara> discParas =
				m_scr.BookAnnotationsOS[0].NotesOS[0].DiscussionOA.ParagraphsOS;
			Assert.AreEqual(1, discParas.Count);
			Assert.AreEqual("This is my discussion of the first verse.", ((StTxtPara)discParas[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text containing pictures. This imports some BT material corresponding to the
		/// existing vernacular Scripture in the book of Genesis. We will process this marker
		/// sequence:
		///    Vern: id s p c1 v1 fig v2 fig p v3 fig
		///    BT:   id s p c1 v1 fig v2 fig p v3 fig
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_Pictures()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk1.jpg", true))
			{
				m_importer.Settings.ImportTranslation = true;
				m_importer.Settings.ImportBackTranslation = true;
				m_importer.Settings.ImportBookIntros = false;

				// ************** process a \id segment, test MakeBook() method *********************
				m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
				m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

				// Set up the vernacular Scripture
				m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
				m_importer.ProcessSegment("Primera Seccion ", @"\s");
				m_importer.ProcessSegment("", @"\p");
				m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
				m_importer.ProcessSegment("", @"\c");
				m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
				m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
				m_importer.ProcessSegment("Primer versiculo", @"\v");
				m_importer.ProcessSegment("User-supplied picture|" + filemaker.Filename +
					"|col|EXO 1--1||Primer subtitulo para junk1.jpg| ", @"\fig");
				m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
				m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
				m_importer.ProcessSegment("Segundo versiculo", @"\v");
				m_importer.ProcessSegment("User-supplied picture|" + filemaker.Filename +
					"|col|EXO 1--1||Segunda subtitulo para junk1.jpg| ", @"\fig");
				m_importer.ProcessSegment("", @"\p");
				m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 3);
				m_importer.TextSegment.LastReference = new BCVRef(1, 1, 3);
				m_importer.ProcessSegment("Tercer versiculo", @"\v");
				m_importer.ProcessSegment("User-supplied picture|" + filemaker.Filename +
					"|col|EXO 1--1||Tercer subtitulo para junk1.jpg|", @"\fig");
				IScrBook genesis = m_importer.ScrBook;
				Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

				// Now test ability to import a non-interleaved BT
				m_importer.CurrentImportDomain = ImportDomain.BackTrans;
				m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
				Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
					"The id line in the BT file should not cause a new ScrBook to get created.");
				m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
				m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
				m_importer.ProcessSegment("First Section ", @"\s");
				m_importer.ProcessSegment("", @"\p");
				m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
				m_importer.ProcessSegment("", @"\c");
				m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
				m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
				m_importer.ProcessSegment("First verse ", @"\v");
				m_importer.ProcessSegment("BT for first photo", @"\fig");
				m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
				m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
				m_importer.ProcessSegment("Second verse ", @"\v");
				m_importer.ProcessSegment("BT for second photo", @"\fig");
				m_importer.ProcessSegment("", @"\p");
				m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 3);
				m_importer.TextSegment.LastReference = new BCVRef(1, 1, 3);
				m_importer.ProcessSegment("Third verse ", @"\v");
				m_importer.ProcessSegment("BT for third photo", @"\fig");
				m_importer.ProcessSegment("", @"\p");

				// ************** finalize **************
				m_importer.FinalizeImport();

				Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

				// Check first section
				IScrSection section = genesis.SectionsOS[0];
				Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
				IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
				Assert.AreEqual("Primera Seccion", para.Contents.Text);
				Assert.AreEqual(1, para.TranslationsOC.Count);
				ICmTranslation translation = para.GetBT();
				Assert.AreEqual("First Section",
					translation.Translation.GetAlternative(m_wsAnal).Text);
				Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
				para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
				Assert.AreEqual("11Primer versiculo" + StringUtils.kchObject +
					"2Segundo versiculo" + StringUtils.kchObject, para.Contents.Text);
				VerifyPictureWithTranslation(para, 0, "Primer subtitulo para junk1.jpg",
					Path.Combine(Path.GetTempPath(), "BT for first photo"));
				VerifyPictureWithTranslation(para, 1, "Segunda subtitulo para junk1.jpg",
					Path.Combine(Path.GetTempPath(), "BT for second photo"));
				Assert.AreEqual(1, para.TranslationsOC.Count);
				translation = para.GetBT();
				Assert.AreEqual("11First verse" + " 2Second verse",
					translation.Translation.GetAlternative(m_wsAnal).Text);
				para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
				Assert.AreEqual("3Tercer versiculo" + StringUtils.kchObject, para.Contents.Text);
				VerifyPictureWithTranslation(para, 0, "Tercer subtitulo para junk1.jpg",
					Path.Combine(Path.GetTempPath(), "BT for third photo"));
				Assert.AreEqual(1, para.TranslationsOC.Count);
				translation = para.GetBT();
				Assert.AreEqual("3Third verse",
					translation.Translation.GetAlternative(m_wsAnal).Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text containing pictures. The back translation for the picture caption does not
		/// have a corresponding picture and will throw an exception. We will process this marker
		/// sequence:
		///    Vern: id s p c1 v1
		///    BT:   id s p c1 v1 fig
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_MissingPicture()
		{
			CheckDisposed();

			try
			{
				using (DummyFileMaker filemaker = new DummyFileMaker("junk1.jpg", true))
				{
					m_importer.Settings.ImportTranslation = true;
					m_importer.Settings.ImportBackTranslation = true;
					m_importer.Settings.ImportBookIntros = false;

					// ************** process a \id segment, test MakeBook() method *********************
					m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
					m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

					// Set up the vernacular Scripture
					m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
					m_importer.ProcessSegment("Primera Seccion ", @"\s");
					m_importer.ProcessSegment("", @"\p");
					m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
					m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
					m_importer.ProcessSegment("", @"\c");
					m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
					m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
					m_importer.ProcessSegment("Primer versiculo", @"\v");
					IScrBook genesis = m_importer.ScrBook;
					Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

					// Now test the missing picture in a non-interleaved BT
					m_importer.CurrentImportDomain = ImportDomain.BackTrans;
					m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
					Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
						"The id line in the BT file should not cause a new ScrBook to get created.");
					m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
					m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
					m_importer.ProcessSegment("First Section ", @"\s");
					m_importer.ProcessSegment("", @"\p");
					m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
					m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
					m_importer.ProcessSegment("", @"\c");
					m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
					m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
					m_importer.ProcessSegment("First verse ", @"\v");
					m_importer.ProcessSegment("BT for first photo", @"\fig");
				}

				// ************** finalize **************
				m_importer.FinalizeImport();

				Assert.Fail("We should throw an exception for the bad picture");
			}
			catch (ScriptureUtilsException e)
			{
				// Rather than having the test expect an exception, we had to put this assertion into
				// a catch because the exception contains a variable, which is not allowed in the
				// attribute.
				Assert.AreEqual("Back translation does not correspond to a vernacular picture.\r\n" +
					"A back translation picture must correspond to a picture in the corresponding vernacular paragraph." +
					"\r\n\r\n\\fig " + Path.Combine(Path.GetTempPath(), "BT for first photo") +
					"\r\nAttempting to read GEN  Chapter: 1  Verse: 1", e.Message);
			}
			catch (Exception)
			{
				Assert.Fail("Exception should have been a ScriptureUtilsException");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text containing footnotes. This imports some BT material corresponding to the
		/// existing vernacular Scripture in the book of Genesis, where the BT footnote is empty.
		/// We will process this marker sequence:
		///    Vern: id s p c1 v1 f
		///    BT: id s p c1 v1 f
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_EmptyBTParaFootnote()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("Primer versiculo", @"\v");
			m_importer.ProcessSegment("- Primer pata nota", @"\f");
			m_importer.ProcessSegment(" ", @"\f*");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(genesis.Hvo, m_importer.ScrBook.Hvo,
				"The id line in the BT file should not cause a new ScrBook to get created.");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);
			m_importer.ProcessSegment("First Section ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("First verse ", @"\v");
			m_importer.ProcessSegment("", @"\f");
			m_importer.ProcessSegment(" ", @"\f*");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			// Check section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("First Section",
				translation.Translation.GetAlternative(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo" + StringUtils.kchObject, para.Contents.Text);
			VerifyFootnoteWithTranslation(0, "Primer pata nota", string.Empty, string.Empty,
				ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse" + StringUtils.kchObject,
				translation.Translation.GetAlternative(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text containing footnotes. This imports some BT material corresponding to the
		/// existing vernacular Scripture in the book of Genesis, where the BT footnote is empty.
		/// We will process this marker sequence:
		///    Vern: id s f
		///    BT: id s f
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationNonInterleaved_BTFootnoteBeginsPara()
		{
			CheckDisposed();

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the vernacular Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("- Primer pata nota", @"\f");
			m_importer.ProcessSegment(" ", @"\f*");
			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			// Now test ability to import a non-interleaved BT
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment(" ", @"\s");
			m_importer.ProcessSegment("Hi mom", @"\f");
			m_importer.ProcessSegment(" ", @"\f*");

			// ************** finalize **************
			m_importer.FinalizeImport();

			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			// Check section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion" + StringUtils.kchObject, para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			ITsString tss = translation.Translation.GetAlternative(m_wsAnal).UnderlyingTsString;
			Assert.AreEqual(1, tss.RunCount);
			TeImportTestInMemory.VerifyFootnoteMarkerOrcRun(tss, 0, m_wsAnal, true);
			VerifyFootnoteWithTranslation(0, "Primer pata nota", "Hi mom", string.Empty,
				ScrStyleNames.NormalFootnoteParagraph);
		}
		#endregion

		#region Invalid marker tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to import a Paratext Scripture file that has a chapter number before a book.
		/// (TE-5021)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidScrFile_UnexcludedDataBeforeIdLine()
		{
			try
			{
				m_importer.TextSegment.FirstReference = new BCVRef(41, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(41, 1, 0);
				m_importer.ProcessSegment("", @"\c");

				Assert.Fail("The exception was not detected.");
			}
			catch (ScriptureUtilsException e)
			{
				Assert.AreEqual(SUE_ErrorCode.UnexcludedDataBeforeIdLine, e.ErrorCode);
			}
			catch
			{
				Assert.Fail("Wrong exception detected.");
			}
		}
		#endregion
	}
	#endregion

	#region Paratext 5 Import Tests (in-memory cache) Currently commented out
	//	/// ---------------------------------------------------------------------------------------
	//	/// <summary>
	//	/// TeImportTestsParatext5 tests TeImport for Paratext 5 files
	//	/// </summary>
	//	/// ---------------------------------------------------------------------------------------
	//	[TestFixture]
	//	public class TeImportTestParatext5: TeTestBase
	//	{
	//		#region Member variables
	//		private DummyTeImporter m_importer;
	//		private ScrReference m_titus;
	//		private FwStyleSheet m_styleSheet;
	//		private ScrImportSet m_settings;
	//
	//		private int m_wsVern; // writing system info needed by tests
	//		private int m_wsAnal;
	//		private ITsTextProps m_ttpVernWS; // simple run text props expected by tests
	//		private ITsTextProps m_ttpAnalWS;
	//		#endregion
	//
	//		#region Setup/Teardown
	//		/// ------------------------------------------------------------------------------------
	//		/// <summary>
	//		/// Initialize the importer
	//		/// </summary>
	//		/// ------------------------------------------------------------------------------------
	//		[SetUp]
	//		public override void Initialize()
	//		{
	//			base.Initialize();
	//
	//			m_styleSheet = new FwStyleSheet();
	//			m_styleSheet.Init(Cache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);
	//			InitWsInfo();
	//
	//			m_titus = new ScrReference(56001001);
	//			m_settings = new ScrImportSet();
	//			Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(m_settings);
	//			m_settings.ImportTypeEnum = TypeOfImport.Other;
	//			DummyTeImporter.s_translatorNoteDefn = m_inMemoryCache.m_translatorNoteDefn;
	//			DummyTeImporter.s_consultantNoteDefn = m_inMemoryCache.m_consultantNoteDefn;
	//
	//			m_settings.ImportTypeEnum = TypeOfImport.Paratext5;
	//			// add a bogus file to the project
	//			m_settings.AddFile(DriveUtil.BootDrive + @"IDontExist.txt", ImportDomain.Main, null, 0);
	//			// Set up the mappings
	//			DummyTeImporter.SetUpMappings(m_settings);
	//
	//			m_settings.StartRef = m_titus;
	//			m_settings.EndRef = m_titus;
	//			m_settings.ImportTranslation = true;
	//			m_settings.ImportBackTranslation = false;
	//			m_settings.ImportBookIntros = true;
	//			m_settings.ImportAnnotations = false;
	//
	//			m_importer = new DummyTeImporter(m_settings, Cache, m_styleSheet, m_inMemoryCache);
	//			m_importer.Initialize();
	//		}
	//
	//		#region IDisposable override
	//		/// ------------------------------------------------------------------------------------
	//		/// <summary>
	//		/// Executes in two distinct scenarios.
	//		///
	//		/// 1. If disposing is true, the method has been called directly
	//		/// or indirectly by a user's code via the Dispose method.
	//		/// Both managed and unmanaged resources can be disposed.
	//		///
	//		/// 2. If disposing is false, the method has been called by the
	//		/// runtime from inside the finalizer and you should not reference (access)
	//		/// other managed objects, as they already have been garbage collected.
	//		/// Only unmanaged resources can be disposed.
	//		/// </summary>
	//		/// <param name="disposing"></param>
	//		/// <remarks>
	//		/// If any exceptions are thrown, that is fine.
	//		/// If the method is being done in a finalizer, it will be ignored.
	//		/// If it is thrown by client code calling Dispose,
	//		/// it needs to be handled by fixing the bug.
	//		///
	//		/// If subclasses override this method, they should call the base implementation.
	//		/// </remarks>
	//		/// ------------------------------------------------------------------------------------
	//		protected override void Dispose(bool disposing)
	//		{
	//			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
	//			// Must not be run more than once.
	//			if (IsDisposed)
	//				return;
	//
	//			if (disposing)
	//			{
	//				// Dispose managed resources here.
	//				if (m_importer != null)
	//					m_importer.Dispose();
	//			}
	//
	//			// Dispose unmanaged resources here, whether disposing is true or false.
	//			m_styleSheet = null; // FwStyleSheet should implement IDisposable.
	//			m_settings = null;
	//			m_importer = null; // TeImporter should implement IDisposable.
	//			if (m_ttpVernWS != null)
	//			{
	////				Marshal.ReleaseComObject(m_ttpVernWS);
	//				m_ttpVernWS = null;
	//			}
	//			if (m_ttpAnalWS != null)
	//			{
	////				Marshal.ReleaseComObject(m_ttpAnalWS);
	//				m_ttpAnalWS = null;
	//			}
	//
	//			base.Dispose(disposing);
	//		}
	//		#endregion IDisposable override
	//
	//		/// ------------------------------------------------------------------------------------
	//		/// <summary>
	//		///
	//		/// </summary>
	//		/// ------------------------------------------------------------------------------------
	//		public override void CreateTestData()
	//		{
	//			m_inMemoryCache.InitializeWritingSystemEncodings();
	//			// setup the default vernacular WS
	//			m_inMemoryCache.CacheAccessor.CacheVecProp(Cache.LangProject.Hvo,
	//				(int)LangProject.LangProjectTags.kflidCurVernWss,
	//				new int[]{InMemoryFdoCache.s_wsHvos.XKal}, 1);
	//			Cache.LangProject.CacheDefaultWritingSystems();
	//			m_inMemoryCache.InitializeScrAnnotationDefs();
	//			m_inMemoryCache.InitializeScrAnnotationCategories();
	//		}
	//
	//		/// ------------------------------------------------------------------------------------
	//		/// <summary>
	//		/// Init writing system info and some props needed by some tests.
	//		/// </summary>
	//		/// ------------------------------------------------------------------------------------
	//		private void InitWsInfo()
	//		{
	//			Debug.Assert(Cache != null);
	//
	//			// get writing system info needed by tests
	//			m_wsVern = Cache.DefaultVernWs;
	//			m_wsAnal = Cache.DefaultAnalWs;
	//
	//			// init simple run text props expected by tests
	//			ITsPropsBldr tsPropsBldr = TsPropsBldrClass.Create();
	//			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsVern);
	//			m_ttpVernWS = tsPropsBldr.GetTextProps();
	//			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsAnal);
	//			m_ttpAnalWS = tsPropsBldr.GetTextProps();
	//		}
	//		#endregion
	//
	//		#region Properties
	//		/// ------------------------------------------------------------------------------------
	//		/// <summary>
	//		/// returns the default vernacular writing system
	//		/// </summary>
	//		/// ------------------------------------------------------------------------------------
	//		public int DefaultVernWs
	//		{
	//			get {return m_wsVern;}
	//		}
	//
	//		/// ------------------------------------------------------------------------------------
	//		/// <summary>
	//		/// returns the default analysis writing system
	//		/// </summary>
	//		/// ------------------------------------------------------------------------------------
	//		public int DefaultAnalWs
	//		{
	//			get {return m_wsAnal;}
	//		}
	//		#endregion
	//
	//		#region ProcessSegment - Basic tests
	//		/// ------------------------------------------------------------------------------------
	//		/// <summary>
	//		/// Send a minimal, normal sequence of Scripture text segments through ProcessSegment,
	//		/// and verify the results.
	//		/// We will process this marker sequence:
	//		///    id mt is ip
	//		///    c1 s s r p v1 x x* f f* v2-3 q q2 v4
	//		///    c2 s q v1-10 s p v11
	//		/// </summary>
	//		/// ------------------------------------------------------------------------------------
	//		[Test]
	//		public void ProcessSegmentBasic()
	//		{
	//			// ************** process a \id segment, test MakeBook() method *********************
	//			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
	//			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
	//			m_importer.ProcessSegment("EXO", @"\id");
	//			Assert.AreEqual(2, m_importer.BookNumber);
	//			Assert.AreEqual(1, m_importer.ScrBook.TitleOA.ParagraphsOS.Count);
	//			Assert.AreEqual(0, m_importer.ScrBook.SectionsOS.Count);
	//			Assert.IsTrue(m_importer.HvoTitle > 0);
	//			// verify that a new book was added to the DB
	//			ScrBook book = new ScrBook(Cache, m_importer.ScrBook.Hvo);
	//			Assert.AreEqual(string.Empty, book.IdText);
	//			Assert.IsTrue(book.TitleOA.IsValidObject()); //empty title
	//			Assert.AreEqual(book.TitleOAHvo, m_importer.HvoTitle);
	//			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
	//			Assert.AreEqual(0, book.SectionsOS.Count); // empty seq of sections
	//			Assert.AreEqual("EXO", book.BookId);
	//			//	Assert.AreEqual(2, book.CanonOrd);
	//
	//			// ************** process a main title *********************
	//			m_importer.ProcessSegment("Main Title!", @"\mt");
	//			Assert.AreEqual("Main Title!", m_importer.ScrBook.Name.GetAlternative(
	//				m_wsVern));
	//
	//			// begin first section (intro material)
	//			// ************** process an intro section head, test MakeSection() method ************
	//			m_importer.ProcessSegment("Background Material", @"\is");
	//			Assert.AreEqual(2, m_importer.BookNumber);
	//			Assert.IsNotNull(m_importer.CurrentSection);
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(1, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(0, "Background Material", null);
	//			Assert.AreEqual(19, m_importer.ParaBldrLength);
	//			// verify completed title was added to the DB
	//			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
	//			StTxtPara title = (StTxtPara)book.TitleOA.ParagraphsOS[0];
	//			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle), title.StyleRules);
	//			Assert.AreEqual(1, title.Contents.UnderlyingTsString.get_RunCount());
	//			AssertEx.RunIsCorrect(title.Contents.UnderlyingTsString, 0, "Main Title!", null, DefaultVernWs);
	//			// verify that a new section was added to the DB
	//			book = (ScrBook)ScrBook.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
	//			VerifyNewSectionExists(book, 0);
	//
	//			// ************** process an intro paragraph, test MakeParagraph() method **********
	//			m_importer.ProcessSegment("Intro paragraph text", @"\ip");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(1, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(0, "Intro paragraph text", null);
	//			Assert.AreEqual(20, m_importer.ParaBldrLength);
	//			// verify completed intro section head was added to DB
	//			Assert.AreEqual(1, book.SectionsOS.Count);
	//			Assert.AreEqual(1, book.SectionsOS[0].HeadingOA.ParagraphsOS.Count);
	//			StTxtPara heading = (StTxtPara)book.SectionsOS[0].HeadingOA.ParagraphsOS[0];
	//			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Section Head"),
	//				heading.StyleRules);
	//			Assert.AreEqual(1, heading.Contents.UnderlyingTsString.get_RunCount());
	//			AssertEx.RunIsCorrect(heading.Contents.UnderlyingTsString, 0, "Background Material", null, DefaultVernWs);
	//
	//			// begin second section (scripture text)
	//			// ************** process a chapter *********************
	//			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
	//			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
	//			m_importer.ProcessSegment("", @"\c");
	//			// note: new section and para are established, but chapter number is not put in
	//			//  para now (it's saved for drop-cap location later)
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(0, m_importer.ParaBldrLength);
	//			Assert.AreEqual(1, m_importer.Chapter);
	//			// verify contents of completed paragraph
	//			Assert.AreEqual(1, book.SectionsOS[0].ContentOA.ParagraphsOS.Count);
	//			StTxtPara para = (StTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
	//			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Paragraph"), para.StyleRules);
	//			Assert.AreEqual(1, para.Contents.UnderlyingTsString.get_RunCount());
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Intro paragraph text", null, DefaultVernWs);
	//			// verify refs of completed section
	//			Assert.AreEqual(2001000, book.SectionsOS[0].VerseRefMin);
	//			Assert.AreEqual(2001000, book.SectionsOS[0].VerseRefMax);
	//			// verify that a new section was added to the DB
	//			book = (IScrBook)CmObject.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
	//			VerifyNewSectionExists(book, 1);
	//
	//			// ************** process a section head (for 1:1-4) *********************
	//			m_importer.ProcessSegment("Section Head One", @"\s");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(1, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(0, "Section Head One", null);
	//			Assert.AreEqual(16, m_importer.ParaBldrLength);
	//
	//			// ************** process second line of section head *********************
	//			m_importer.ProcessSegment("Yadda yadda Line two!", @"\s");
	//			// verify state of NormalParaStrBldr
	//			string sBrkChar = new string((char)0x2028, 1);
	//			Assert.AreEqual(1, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(0, "Section Head One" + sBrkChar + "Yadda yadda Line two!", null);
	//			Assert.AreEqual(38, m_importer.ParaBldrLength);
	//
	//			// ************** process a section head reference *********************
	//			m_importer.ProcessSegment("Section Head Ref Line", @"\r");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(1, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(0, "Section Head Ref Line", null);
	//
	//			// in second section (1:1-4), begin first content paragraph
	//			// ************** process a \p paragraph marker *********************
	//			m_importer.ProcessSegment("", @"\p");
	//			// note: chapter number should be inserted now
	//			int expectedBldrLength = 1; // The chapter number takes one character
	//			int expectedRunCount = 1;
	//			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(0, "1", "Chapter Number");
	//			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
	//			// verify completed section head was added to DB (for 1:1-4)
	//			Assert.AreEqual(2, book.SectionsOS.Count);
	//			Assert.AreEqual(2, book.SectionsOS[1].HeadingOA.ParagraphsOS.Count);
	//			// Check 1st heading para
	//			heading = (StTxtPara)book.SectionsOS[1].HeadingOA.ParagraphsOS[0];
	//			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), heading.StyleRules);
	//			Assert.AreEqual(1, heading.Contents.UnderlyingTsString.get_RunCount());
	//			AssertEx.RunIsCorrect(heading.Contents.UnderlyingTsString, 0, "Section Head One" +
	//				sBrkChar + "Yadda yadda Line two!",	null, DefaultVernWs);
	//			// Check 2nd heading para
	//			heading = (StTxtPara)book.SectionsOS[1].HeadingOA.ParagraphsOS[1];
	//			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Parallel Passage Reference"), heading.StyleRules);
	//			Assert.AreEqual(1, heading.Contents.UnderlyingTsString.get_RunCount());
	//			AssertEx.RunIsCorrect(heading.Contents.UnderlyingTsString, 0, "Section Head Ref Line",	null, DefaultVernWs);
	//
	//			// ************** process verse text *********************
	//			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
	//			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
	//			string sSegmentText = "Verse one text";
	//			expectedBldrLength += 1 + sSegmentText.Length; // Length of verse # + length of verse text
	//			expectedRunCount += 2;
	//			m_importer.ProcessSegment(sSegmentText, @"\v");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(0, "1", "Chapter Number");
	//			VerifyBldrRun(1, "1", "Verse Number");
	//			VerifyBldrRun(2, sSegmentText, null);
	//			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
	//
	//			// ************** process verse text with character style *********************
	//			sSegmentText = " text with char style";
	//			expectedBldrLength += sSegmentText.Length;
	//			expectedRunCount++;
	//			m_importer.ProcessSegment(sSegmentText, @"\kw");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(expectedRunCount - 1, sSegmentText, "Key Word");
	//			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
	//
	//			// ************** process text after the character style *********************
	//			sSegmentText = " text after char style";
	//			expectedBldrLength += sSegmentText.Length;
	//			expectedRunCount++;
	//			m_importer.ProcessSegment(sSegmentText, @"\kw*");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(expectedRunCount - 1, sSegmentText, null);
	//			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
	//
	//			// ********** process a footnote (use default Scripture settings) *************
	//			string sFootnoteSegment = "My footnote text";
	//			expectedBldrLength++; // Just adding the footnote marker: "a"
	//			expectedRunCount++;
	//			m_importer.ProcessSegment(sFootnoteSegment, @"\f");
	//			// verify state of FootnoteParaStrBldr
	//			Assert.AreEqual(1, m_importer.FootnoteParaStrBldr.get_RunCount());
	//
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.get_RunCount());
	//			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
	//			// check the ORC (Object Replacement Character)
	//			VerifyBldrFootnoteOrcRun(expectedRunCount - 1, 0);
	//
	//			// ************** process text after a footnote *********************
	//			sSegmentText = " more verse text";
	//			expectedBldrLength += sSegmentText.Length;
	//			expectedRunCount++;
	//			m_importer.ProcessSegment(sSegmentText, @"\vt");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(expectedRunCount - 1, sSegmentText, null);
	//			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
	//			// Verify creation of footnote object
	//			VerifySimpleFootnote(0, sFootnoteSegment);
	//
	//			// ************** process verse two-three text *********************
	//			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
	//			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
	//			sSegmentText = "Verse two-three text";
	//			expectedBldrLength += 3 + sSegmentText.Length;
	//			expectedRunCount += 2;
	//			m_importer.ProcessSegment(sSegmentText, @"\v");
	//			// verify state of NormalParaStrBldr
	//			//TODO: when ready, modify these lines to verify that chapter number was properly added to para
	//			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(expectedRunCount - 2, "2-3", "Verse Number");
	//			VerifyBldrRun(expectedRunCount - 1, sSegmentText, null);
	//			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
	//
	//			// in second section (verse text), begin second paragraph
	//			// ************** process a \q paragraph marker with text *********************
	//			int expectedParaRunCount = expectedRunCount;
	//			sSegmentText = "First line of poetry";
	//			expectedRunCount = 1;
	//			expectedBldrLength = sSegmentText.Length;
	//			m_importer.ProcessSegment(sSegmentText, @"\q");
	//
	//			Assert.AreEqual(2, m_importer.BookNumber);
	//
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(expectedRunCount - 1, sSegmentText, null);
	//			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
	//
	//			// verify that the verse text first paragraph is in the db correctly
	//			book = (ScrBook)ScrBook.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
	//			Assert.AreEqual(2, book.SectionsOS.Count);
	//			Assert.AreEqual(1, book.SectionsOS[1].ContentOA.ParagraphsOS.Count);
	//			para = (StTxtPara)book.SectionsOS[1].ContentOA.ParagraphsOS[0]; //first para
	//			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
	//			Assert.AreEqual(expectedParaRunCount, para.Contents.UnderlyingTsString.get_RunCount());
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "1", "Chapter Number", DefaultVernWs);
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "1", "Verse Number", DefaultVernWs);
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 2, "Verse one text", null, DefaultVernWs);
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 3, " text with char style", "Key Word", DefaultVernWs);
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 4, " text after char style", null, DefaultVernWs);
	//			//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 5,
	//			//				" text after char style{footnote text} more verse text", null, DefaultVernWs);
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 6, " more verse text", null, DefaultVernWs);
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 7, "2-3", "Verse Number", DefaultVernWs);
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 8, "Verse two-three text", null, DefaultVernWs);
	//
	//			// in second section (verse text), begin third paragraph
	//			// ************** process a \q2 paragraph marker (for a new verse) ****************
	//			expectedParaRunCount = expectedRunCount;
	//			m_importer.ProcessSegment("", @"\q2");
	//			Assert.AreEqual(2, m_importer.BookNumber);
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(0, m_importer.ParaBldrLength);
	//			// verify that the verse text second paragraph is in the db correctly
	//			book = (ScrBook)ScrBook.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
	//			Assert.AreEqual(2, book.SectionsOS.Count);
	//			Assert.AreEqual(2, book.SectionsOS[1].ContentOA.ParagraphsOS.Count);
	//			para = (StTxtPara)book.SectionsOS[1].ContentOA.ParagraphsOS[1]; //second para
	//			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
	//			Assert.AreEqual(expectedParaRunCount, para.Contents.UnderlyingTsString.get_RunCount());
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, sSegmentText, null, DefaultVernWs);
	//
	//			// ************** process verse four text *********************
	//			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 4);
	//			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 4);
	//			sSegmentText = "second line of poetry";
	//			expectedBldrLength = sSegmentText.Length + 1;
	//			expectedRunCount = 2;
	//			m_importer.ProcessSegment(sSegmentText, @"\v");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(expectedRunCount, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(expectedRunCount - 2, "4", "Verse Number");
	//			VerifyBldrRun(expectedRunCount - 1, sSegmentText, null);
	//			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
	//
	//			// ************** process a chapter *********************
	//			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 1);
	//			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 1);
	//			m_importer.ProcessSegment("", @"\c");
	//			// note: new para is established, but chapter number is not put in
	//			// para now (it's saved for drop-cap location later)
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength, "nothing should have been added");
	//			Assert.AreEqual(2, m_importer.Chapter);
	//			// verify that we have not yet established a third section
	//			Assert.AreEqual(2, book.SectionsOS.Count);
	//
	//			// begin third section
	//			// ************** process a section head (for 2:1-10) *********************
	//			m_importer.ProcessSegment("Section Head Two", @"\s");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(1, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(0, "Section Head Two", null);
	//			Assert.AreEqual(16, m_importer.ParaBldrLength);
	//
	//			// verify that the second section third paragraph is in the db correctly
	//			book = (ScrBook)ScrBook.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
	//			Assert.AreEqual(3, book.SectionsOS[1].ContentOA.ParagraphsOS.Count);
	//			para = (StTxtPara)book.SectionsOS[1].ContentOA.ParagraphsOS[2]; //third para
	//			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line2"), para.StyleRules);
	//			Assert.AreEqual(2, para.Contents.UnderlyingTsString.get_RunCount());
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "4", "Verse Number", DefaultVernWs);
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "second line of poetry", null, DefaultVernWs);
	//
	//			// verify refs of completed scripture text section (1:1-4)
	//			Assert.AreEqual(2001001, book.SectionsOS[1].VerseRefMin);
	//			Assert.AreEqual(2001004, book.SectionsOS[1].VerseRefMax);
	//			// verify that a new section was added to the DB
	//			book = (IScrBook)CmObject.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
	//			VerifyNewSectionExists(book, 2);
	//
	//			// in third section, begin first paragraph
	//			// ************** process a \q paragraph marker (for a new verse) ****************
	//			m_importer.ProcessSegment("", @"\q");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(1, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(0, "2", "Chapter Number");
	//			Assert.AreEqual(1, m_importer.ParaBldrLength);
	//			// verify completed section head was added to DB
	//			Assert.AreEqual(3, book.SectionsOS.Count);
	//			Assert.AreEqual(1, book.SectionsOS[2].HeadingOA.ParagraphsOS.Count);
	//			heading = (StTxtPara)book.SectionsOS[2].HeadingOA.ParagraphsOS[0];
	//			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), heading.StyleRules);
	//			Assert.AreEqual(1, heading.Contents.UnderlyingTsString.get_RunCount());
	//			AssertEx.RunIsCorrect(heading.Contents.UnderlyingTsString, 0, "Section Head Two", null, DefaultVernWs);
	//
	//			// ************** process verse 5-10 text *********************
	//			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 1);
	//			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 10);
	//			m_importer.ProcessSegment("verse one to ten text", @"\v");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(3, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(1, "1-10", "Verse Number");
	//			VerifyBldrRun(2, "verse one to ten text", null);
	//			Assert.AreEqual(26, m_importer.ParaBldrLength);
	//
	//			// begin fourth section
	//			// ************** process a section head (for 2:11) *********************
	//			m_importer.ProcessSegment("Section Head Four", @"\s");
	//			// verify state of NormalParaStrBldr
	//			Assert.AreEqual(1, m_importer.NormalParaStrBldr.get_RunCount());
	//			VerifyBldrRun(0, "Section Head Four", null);
	//			Assert.AreEqual(17, m_importer.ParaBldrLength);
	//			// verify that the third section first paragraph is in the db correctly
	//			//book = (ScrBook)ScrBook.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
	//			Assert.AreEqual(4, book.SectionsOS.Count);
	//			Assert.AreEqual(1, book.SectionsOS[2].ContentOA.ParagraphsOS.Count);
	//			para = (StTxtPara)book.SectionsOS[2].ContentOA.ParagraphsOS[0]; //first para
	//			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
	//			Assert.AreEqual(3, para.Contents.UnderlyingTsString.get_RunCount());
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "2", "Chapter Number", DefaultVernWs);
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "1-10", "Verse Number", DefaultVernWs);
	//			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 2, "verse one to ten text", null, DefaultVernWs);
	//			// verify refs of completed scripture text section (2:5-10)
	//			Assert.AreEqual(2002001, book.SectionsOS[2].VerseRefMin);
	//			Assert.AreEqual(2002010, book.SectionsOS[2].VerseRefMax);
	//			// verify that a new section was added to the DB
	//			book = (IScrBook)CmObject.CreateFromDBObject(Cache, m_importer.ScrBook.Hvo, true); //refresh cache
	//			VerifyNewSectionExists(book, 3);
	//
	//			// TODO:  p v11
	//
	//			// ************** finalize **************
	//			m_importer.FinalizeImport();
	//			Assert.AreEqual(2, m_importer.BookNumber);
	//		}
	//		#endregion
	//	}
	#endregion
}
