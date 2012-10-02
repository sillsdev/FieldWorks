// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2007' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeImportParatext6Tests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

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
			// For these tests, we want to simulate the end-marker mapping for \fr that
			// we would get from the normal USFM.sty Paratext style sheet.
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\fr", @"\fr*",
				MarkerDomain.Footnote, ScrStyleNames.FootnoteTargetRef, null, null));
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = mark.FootnotesOS[0];
			ITsString tss = ((IStTxtPara)footnote.ParagraphsOS[0]).Contents;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "This is a footnote", null, Cache.DefaultVernWs);

			// verify the intro section content text
			IScrTxtPara para = (IScrTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("intro" + StringUtils.kChObject + " paragraph", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = mark.FootnotesOS[0];
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "is a footnote", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents);
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kChObject + " one",
				para.Contents.Text);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = mark.FootnotesOS[0];
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "A big footnote issue", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents);
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kChObject + " one",
				para.Contents.Text);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = mark.FootnotesOS[0];
			ITsString tss = ((IStTxtPara)footnote.ParagraphsOS[0]).Contents;
			Assert.AreEqual(2, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "This is a ", null, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(tss, 1, "footnote", "Emphasis", Cache.DefaultVernWs);
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kChObject + " one",
				para.Contents.Text);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = (IStFootnote)mark.FootnotesOS[0];
			ITsString tss = ((IStTxtPara)footnote.ParagraphsOS[0]).Contents;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "footnote", null, Cache.DefaultVernWs);
			Assert.AreNotEqual("footnote", m_scr.GeneralFootnoteMarker);
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kChObject,
				para.Contents.Text);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = (IStFootnote)mark.FootnotesOS[0];
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "This is a footnote", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents);
			Assert.AreEqual(FootnoteMarkerTypes.SymbolicFootnoteMarker, m_scr.FootnoteMarkerType);
			Assert.AreEqual("q", m_scr.GeneralFootnoteMarker);

			// verify the section content text
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kChObject + " one",
				para.Contents.Text);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = (IStFootnote)mark.FootnotesOS[0];
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "I wish This is a footnote", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents);
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kChObject + " one",
				para.Contents.Text);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = (IStFootnote)mark.FootnotesOS[0];
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "footnote", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.Replace(0, 0, "a ", StyleUtils.CharStyleTextProps("Quoted Text", Cache.DefaultVernWs));
			bldr.Replace(0, 0, "is ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.Replace(0, 0, "This ", StyleUtils.CharStyleTextProps("Quoted Text", Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents);

			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kChObject + " one",
				para.Contents.Text);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the section content text
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			ITsString tssPara = para.Contents;
			Assert.AreEqual(5, tssPara.RunCount);
			AssertEx.RunIsCorrect(tssPara, 0, "1", ScrStyleNames.ChapterNumber, m_wsVern);
			AssertEx.RunIsCorrect(tssPara, 1, "1", ScrStyleNames.VerseNumber, m_wsVern);
			AssertEx.RunIsCorrect(tssPara, 2, "paragraph", null, m_wsVern);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);
			AssertEx.RunIsCorrect(tssPara, 4, " one", null, m_wsVern);
			VerifyFootnote(mark.FootnotesOS[0], para, 11);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = mark.FootnotesOS[0];
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "a footnote", StyleUtils.CharStyleTextProps("Quoted Text", Cache.DefaultVernWs));
			bldr.Replace(0, 0, "This is ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents);

			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kChObject + " one",
				para.Contents.Text);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			IScrSection section = mark.SectionsOS[0];

			// verify the footnote text
			IStFootnote footnote = mark.FootnotesOS[0];
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "footnote", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.Replace(0, 0, "a ", StyleUtils.CharStyleTextProps("Quoted Text", Cache.DefaultVernWs));
			bldr.Replace(0, 0, "This is ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents);

			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker, m_scr.FootnoteMarkerType);

			// verify the section content text
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11paragraph" + StringUtils.kChObject + " one",
				para.Contents.Text);

			// verify the section head text
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("section", para.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void HandleUSFMStyleFootnotes_FirstOneHasCallerOmitted()
		{
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
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Verse 1 start..." + StringUtils.kChObject + " ...verse 1 end. " +
				"2Verse 2 start..." + StringUtils.kChObject + " ...verse 2 end. " +
				"3Verse 3 start..." + StringUtils.kChObject + " ...verse 3 end. " +
				"4Verse 4 start..." + StringUtils.kChObject + " ...verse 4 end.",
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void HandleUSFMStyleFootnotes_FirstOneHasSequence()
		{
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
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Verse 1 start..." + StringUtils.kChObject + " ...verse 1 end. " +
				"2Verse 2 start..." + StringUtils.kChObject + " ...verse 2 end. " +
				"3Verse 3 start..." + StringUtils.kChObject + " ...verse 3 end. " +
				"4Verse 4 start..." + StringUtils.kChObject + " ...verse 4 end.",
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void HandleUSFMStyleFootnotes_FirstOneHasLiteralCaller()
		{
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
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Verse 1 start..." + StringUtils.kChObject + " ...verse 1 end. " +
				"2Verse 2 start..." + StringUtils.kChObject + " ...verse 2 end. " +
				"3Verse 3 start..." + StringUtils.kChObject + " ...verse 3 end. " +
				"4Verse 4 start..." + StringUtils.kChObject + " ...verse 4 end.",
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void HandleUSFMStyleFootnotes_StripAndIgnoreCallers()
		{
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
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Verse 1 start..." + StringUtils.kChObject + " ...verse 1 end. " +
				"2Verse 2 start..." + StringUtils.kChObject + " ...verse 2 end. " +
				"3Verse 3 start..." + StringUtils.kChObject + " ...verse 3 end. " +
				"4Verse 4 start..." + StringUtils.kChObject + " ...verse 4 end.",
				para.Contents.Text);
			VerifySimpleFootnote(0, "Footnote 1 text", "a");
			VerifySimpleFootnote(1, "Footnote 2 text", "a");
			VerifySimpleFootnote(2, "Footnote 3 text", "a");
			VerifySimpleFootnote(3, "Footnote 4 text", "a");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		/// \c 1
		/// \p
		/// \v 31 ...
		/// \c 13
		/// \s Blah \f v \fr 13:1 \ft footnote text \f*
		/// \p
		/// \v 1 ...
		/// Jira number is TE-9219
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void HandleUSFMStyleFootnotes_FootnoteInSectionHeadAfterChapterNum()
		{
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a footnote, no end marker, terminated by another footnote
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment(" ", @"\p");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 31);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 31);
			m_importer.ProcessSegment("Verse thirty-one", @"\v");

			m_importer.TextSegment.FirstReference = new BCVRef(2, 13, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 13, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("This is a foot-washing ceremony like you've never seen before", @"\s");

			// Add footnote to section head
			m_importer.ProcessSegment("v ", @"\f");
			m_importer.ProcessSegment("13:1 ", @"\fr");
			m_importer.ProcessSegment("footnote text", @"\ft");
			m_importer.ProcessSegment(" ", @"\f*");

			m_importer.ProcessSegment(" ", @"\p");
			// Verse 13:1
			m_importer.TextSegment.FirstReference = new BCVRef(2, 13, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 13, 1);
			m_importer.ProcessSegment("Verse one ", @"\v");

			m_importer.FinalizeImport();

			IScrBook exodus = m_importer.UndoInfo.ImportedSavedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[1];
			IStTxtPara paraHead = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			IStTxtPara paraContents = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("This is a foot-washing ceremony like you've never seen before" + StringUtils.kChObject,
				paraHead.Contents.Text);
			VerifySimpleFootnote(0, "footnote text", "v");
			Assert.AreEqual("131Verse one", paraContents.Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void AnnotationNonInterleaved_Simple()
		{
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
			IFdoOwningSequence<IScrScriptureNote> notes = m_scr.BookAnnotationsOS[0].NotesOS;
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
			Assert.AreEqual("Primera Seccion", ((IStTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para11 = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo 2Segundo versiculo", para11.Contents.Text);
			IStTxtPara para12 = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("3Tercer versiculo", para12.Contents.Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Segunda Seccion", ((IStTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
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
				Assert.AreEqual(annotation.BeginObjectRA, annotation.EndObjectRA);
				// REVIEW: Should we try to find the actual offset of the annotated verse in the para?
				Assert.AreEqual(0, annotation.BeginOffset);
				Assert.AreEqual(annotation.BeginOffset, annotation.EndOffset);
				Assert.AreEqual(annotation.BeginRef, annotation.EndRef);
			}
			IScrScriptureNote note = notes[0];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Note before Scripture text", m_wsAnal);
			Assert.AreEqual(genesis, note.BeginObjectRA);
			Assert.AreEqual(1001000, note.BeginRef);

			note = notes[1];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Note for verse 1", m_wsAnal);
			Assert.AreEqual(para11, note.BeginObjectRA);
			Assert.AreEqual(1001001, note.BeginRef);

			note = notes[2];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "First note for verse 2", m_wsAnal);
			Assert.AreEqual(para11, note.BeginObjectRA);
			Assert.AreEqual(1001002, note.BeginRef);

			note = notes[3];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Second note for verse 2", m_wsAnal);
			Assert.AreEqual(para11, note.BeginObjectRA);
			Assert.AreEqual(1001002, note.BeginRef);

			note = notes[4];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Note for verse 4", m_wsAnal);
			Assert.AreEqual(para21, note.BeginObjectRA);
			Assert.AreEqual(1001004, note.BeginRef);

			note = notes[5];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Note for verse 5", m_wsAnal);
			Assert.AreEqual(para22, note.BeginObjectRA);
			Assert.AreEqual(1001005, note.BeginRef);

			note = notes[6];
			m_importer.VerifyAnnotationText(note.DiscussionOA, "Discussion", "Note for verse 6", m_wsAnal);
			Assert.AreEqual(para22, note.BeginObjectRA);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void AnnotationNonInterleaved_StartWithCharacterMapping()
		{
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
			IFdoOwningSequence<IScrScriptureNote> notes = m_scr.BookAnnotationsOS[0].NotesOS;
			Assert.AreEqual(2, notes.Count);

			IScrScriptureNote annotation = notes[0];
			Assert.AreEqual(NoteType.Translator, annotation.AnnotationType);
			Assert.IsNull(annotation.BeginObjectRA);
			Assert.IsNull(annotation.EndObjectRA);
			Assert.AreEqual(0, annotation.BeginOffset);
			Assert.AreEqual(0, annotation.EndOffset);
			Assert.AreEqual(1001001, annotation.BeginRef);
			Assert.AreEqual(1001001, annotation.EndRef);
			Assert.AreEqual(1, annotation.DiscussionOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)annotation.DiscussionOA.ParagraphsOS[0];
			ITsString tssDiscussionP1 = para.Contents;
			Assert.AreEqual(2, tssDiscussionP1.RunCount);
			AssertEx.RunIsCorrect(tssDiscussionP1, 0, "Emphatically first note", "Emphasis", m_wsAnal);
			AssertEx.RunIsCorrect(tssDiscussionP1, 1, " remaining text", null, m_wsAnal);

			annotation = notes[1];
			Assert.AreEqual(NoteType.Translator, annotation.AnnotationType);
			Assert.IsNull(annotation.BeginObjectRA);
			Assert.IsNull(annotation.EndObjectRA);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void AnnotationInterleaved_DontImportScripture()
		{
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
			IFdoOwningSequence<IScrScriptureNote> notes = m_scr.BookAnnotationsOS[0].NotesOS;
			Assert.AreEqual(1, notes.Count);

			IScrScriptureNote annotation = notes[0];
			Assert.AreEqual(NoteType.Translator, annotation.AnnotationType);
			Assert.IsNull(annotation.BeginObjectRA);
			Assert.IsNull(annotation.EndObjectRA);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_Simple()
		{
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
				titleTranslation.Translation.get_String(m_wsAnal).Text);

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("First Section",
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo 2Segundo versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse 2Second verse",
				translation.Translation.get_String(m_wsAnal).Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("3Tercer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("3Third verse",
				translation.Translation.get_String(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second Section",
				translation.Translation.get_String(m_wsAnal).Text);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[1];
			Assert.AreEqual("(Algunos manuscritos no conienen este pasaje.)", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("(Some manuscripts don't have this passage.)",
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("4Cuarto versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("4Fourth verse",
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		[ExpectedException(typeof(ScriptureUtilsException),
		   ExpectedMessage = "Back translation not part of a paragraph:(\\r)?\\n" +
			"\\tThis is default paragraph characters (\\r)?\\n" +
			"\\t\\(Style: Default Paragraph Characters\\)(\\r)?\\n" +
			"Attempting to read GEN",
			MatchType = MessageMatch.Regex)]
		public void BackTranslationNonInterleaved_DefaultParaCharsStart()
		{
			m_importer.Settings.SetMapping(MappingSet.Main,
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
			ICmTranslation titleTranslation = titlePara.TranslationsOC.ToArray()[0];
			Assert.AreEqual("Title",
				titleTranslation.Translation.get_String(m_wsAnal).Text);

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("First Section",
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo 2Segundo versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("11First verse 2Second verse",
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
			ICmTranslation titleTranslation = titlePara.TranslationsOC.ToArray()[0];
			Assert.AreEqual("Title",
				titleTranslation.Translation.get_String(m_wsAnal).Text);

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("First Section",
				translation.Translation.get_String(m_wsAnal).Text);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[1];
			Assert.AreEqual("(Lc. 3.23-38)", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("(Lc. 3.23-38)",
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("11First verse",
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_ParallelPassage_BtOnly()
		{
			// Setup book
			IScrBook genesis = (IScrBook)AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(genesis, "Genesis");
			IScrSection section1 = AddSectionToMockedBook(genesis);
			AddSectionHeadParaToSection(section1, "Primera Seccion", ScrStyleNames.SectionHead);
			AddSectionHeadParaToSection(section1, "(Lc. 3.23-38)", "Parallel Passage Reference");
			IStTxtPara para11 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para11, "Primer versiculo", null);

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
			ICmTranslation titleTranslation = titlePara.TranslationsOC.ToArray()[0];
			Assert.AreEqual("Title",
				titleTranslation.Translation.get_String(m_wsAnal).Text);

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("First Section",
				translation.Translation.get_String(m_wsAnal).Text);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[1];
			Assert.AreEqual("(Lc. 3.23-38)", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("(Lc. 3.23-38)",
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("11First verse",
				translation.Translation.get_String(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation NOT interleaved with the scripture
		/// text. This tests when there is no scripture book for the back translation
		///    BT: id
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		[ExpectedException(typeof(ScriptureUtilsException),
			ExpectedMessage = "No corresponding vernacular book for back translation.(\\r)?\\nAttempting to read GEN",
			MatchType = MessageMatch.Regex)]
		public void BackTranslationNonInterleaved_NoCorrespondingBook()
		{
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_RepeatedChapterNum()
		{
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
			Assert.AreEqual(1, para.TranslationsOC.Count);
			Assert.IsNull(para.TranslationsOC.ToArray()[0].Translation.VernacularDefaultWritingSystem.Text);

			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("1Primer versiculo 2Segundo versiculo",
				para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("1First verse 2Second verse",
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_NoParaMarker()
		{
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
			Assert.AreEqual(1, para.TranslationsOC.Count);
			Assert.IsNull(para.TranslationsOC.ToArray()[0].Translation.VernacularDefaultWritingSystem.Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo 2Segundo versiculo 3Tercer versiculo 4Cuarto versiculo",
				para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("11First verse 2Second verse 3Third verse 4Fourth verse",
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_TwoBooks()
		{
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
			Assert.AreEqual(1, para.TranslationsOC.Count);
			Assert.IsNull(para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("1Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			Assert.AreEqual("1First verse", translation.Translation.get_String(m_wsAnal).Text);

			// Check III John
			Assert.AreEqual(1, john3.SectionsOS.Count);
			section = john3.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(1, para.TranslationsOC.Count);
			Assert.IsNull(para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("1Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("1First verse", translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_Intros()
		{
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
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("Que bueno que decidiste leer este libro de la Biblia.", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("How good that you decided to read this book of the Bible.",
				translation.Translation.get_String(m_wsAnal).Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("A mi me gusta este libro tambien.", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("I like this book, too.",
				translation.Translation.get_String(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second Section",
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse",
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_ScrParaWithNoVerseNumber()
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
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse",
				translation.Translation.get_String(m_wsAnal).Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("Segunda estrofa", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second stanza",
				translation.Translation.get_String(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second Section",
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_VerseInMultipleParagraphs()
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
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(5, section.ContentOA.ParagraphsOS.Count);
			// paragraph 1
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 2
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("2Segunda versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("2Second verse",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 3
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[2];
			Assert.AreEqual("Segunda estrofa", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second stanza",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 4
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[3];
			Assert.AreEqual("Dritte Strophe", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("next part of verse",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 5
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[4];
			Assert.AreEqual("Vierte Strophe", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("last part of verse",
				translation.Translation.get_String(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second Section",
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_EmptyLastPara()
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
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(3, section.ContentOA.ParagraphsOS.Count);

			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse",
				translation.Translation.get_String(m_wsAnal).Text);

			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("Segunda estrofa", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second stanza",
				translation.Translation.get_String(m_wsAnal).Text);

			para = (IStTxtPara)section.ContentOA.ParagraphsOS[2];
			Assert.AreEqual("2Segunda versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("2Second verse",
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_Footnotes()
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
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo" + StringUtils.kChObject +
				" 2Segundo versiculo" + StringUtils.kChObject, para.Contents.Text);
			VerifyFootnoteWithTranslation(0, "Primer pata nota", "First footnote", string.Empty,
				ScrStyleNames.NormalFootnoteParagraph);
			VerifyFootnoteWithTranslation(1, "Segunda pata nota", "Second footnote", string.Empty,
				ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse" + StringUtils.kChObject +
				" 2Second verse" + StringUtils.kChObject,
				translation.Translation.get_String(m_wsAnal).Text);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("3Tercer versiculo" + StringUtils.kChObject, para.Contents.Text);
			VerifyFootnoteWithTranslation(2, "Gal 3:2", null, string.Empty,
				"Note Cross-Reference Paragraph");
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("3Third verse",
				translation.Translation.get_String(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("Second Section", translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("4Cuarto versiculo" + StringUtils.kChObject, para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("4Fourth verse" + StringUtils.kChObject,
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BtFootnoteWhenNotImportingVernacular()
		{
			// Set up Scripture to correspond with the back translation to be imported.
			IScrBook exodus = AddBookToMockedScripture(2, "Exodus");
			IScrSection section = AddSectionToMockedBook(exodus);
			AddSectionHeadParaToSection(section, "Section 1", ScrStyleNames.SectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "verse one text", null);
			// Add a footnote with a character style.
			int ichFootnote = para.Contents.Length - 5;
			IStFootnote noteOne = AddFootnote(exodus, para, ichFootnote,
				"vernacular text for footnote"); // footnote after "one" in verse 1
			ICmTranslation noteOneTrans = ((IStTxtPara)noteOne.ParagraphsOS[0]).GetOrCreateBT();

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
			IScrBook book = m_importer.ScrBook;
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
			ITsString tssBt = trans1.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(5, tssBt.RunCount);
			AssertEx.RunIsCorrect(tssBt, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBt, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBt, 2, "verse one BT text", null, m_wsAnal);

			Guid guid1 = StringUtils.GetGuidFromRun(tssBt, 3);
			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IStFootnoteRepository>().GetObject(guid1);
			Assert.AreEqual(noteOneTrans.Owner, footnote.ParagraphsOS[0],
				"The first imported BT footnote should be owned by paragraph in the first footnote but isn't");

			VerifyFootnoteWithTranslation(0, "vernacular text for footnote",
				"BT text for footnote one.", "a", ScrStyleNames.NormalFootnoteParagraph);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BtFootnoteWhenNotImportingVernacular_CharStyleUsedTwice()
		{
			// Set up Scripture to correspond with the back translation to be imported.
			IScrBook exodus = AddBookToMockedScripture(2, "Exodus");
			IScrSection introSection = AddSectionToMockedBook(exodus, true);
			AddSectionHeadParaToSection(introSection, "Intro Section", ScrStyleNames.IntroSectionHead);
			AddParaToMockedSectionContent(introSection, ScrStyleNames.IntroParagraph);
			IScrSection section = AddSectionToMockedBook(exodus);
			AddSectionHeadParaToSection(section, "Section 1", ScrStyleNames.SectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "verse one text", null);

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
			IScrBook book = m_importer.ScrBook;
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
			ITsString tssBt = trans1.Translation.AnalysisDefaultWritingSystem;
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_WithInterleavedAnnotation()
		{
			// Set up the vernacular Scripture
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(genesis, "Genesis");
			IScrSection section = AddSectionToMockedBook(genesis);
			AddSectionHeadParaToSection(section, "My head hurts", ScrStyleNames.SectionHead);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "This is verse one.");
			AddVerse(para, 0, 2, "This is verse two.");

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
			ITsString tssTrans = translation.Translation.get_String(m_wsAnal);
			Assert.AreEqual(5, tssTrans.RunCount);
			AssertEx.RunIsCorrect(tssTrans, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 2, "In the beginning ", null, m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 3, "2", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 4, "Then came the end", null, m_wsAnal);

			Assert.AreEqual(1, m_scr.BookAnnotationsOS[0].NotesOS.Count);
			IFdoOwningSequence<IStPara> discParas =
				m_scr.BookAnnotationsOS[0].NotesOS[0].DiscussionOA.ParagraphsOS;
			Assert.AreEqual(1, discParas.Count);
			Assert.AreEqual("This is my discussion of the first verse.", ((IStTxtPara)discParas[0]).Contents.Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_Pictures()
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
					translation.Translation.get_String(m_wsAnal).Text);
				Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
				para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
				Assert.AreEqual("11Primer versiculo" + StringUtils.kChObject +
					"2Segundo versiculo" + StringUtils.kChObject, para.Contents.Text);
				VerifyPictureWithTranslation(para, 0, "Primer subtitulo para junk1.jpg",
					Path.Combine(Path.GetTempPath(), "BT for first photo"));
				VerifyPictureWithTranslation(para, 1, "Segunda subtitulo para junk1.jpg",
					Path.Combine(Path.GetTempPath(), "BT for second photo"));
				Assert.AreEqual(1, para.TranslationsOC.Count);
				translation = para.GetBT();
				Assert.AreEqual("11First verse" + " 2Second verse",
					translation.Translation.get_String(m_wsAnal).Text);
				para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
				Assert.AreEqual("3Tercer versiculo" + StringUtils.kChObject, para.Contents.Text);
				VerifyPictureWithTranslation(para, 0, "Tercer subtitulo para junk1.jpg",
					Path.Combine(Path.GetTempPath(), "BT for third photo"));
				Assert.AreEqual(1, para.TranslationsOC.Count);
				translation = para.GetBT();
				Assert.AreEqual("3Third verse",
					translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_MissingPicture()
		{
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
					m_importer.ProcessSegment("", @"\id");
					// no text provided in segment, just the refs
					m_importer.ProcessSegment("Primera Seccion ", @"\s");
					m_importer.ProcessSegment("", @"\p");
					m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
					m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
					m_importer.ProcessSegment("", @"\c");
					m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
					m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
					m_importer.ProcessSegment("Primer versiculo", @"\v");
					IScrBook genesis = m_importer.ScrBook;
					Assert.AreEqual(1, genesis.SectionsOS.Count);
					// minor sanity check

					// Now test the missing picture in a non-interleaved BT
					m_importer.CurrentImportDomain = ImportDomain.BackTrans;
					m_importer.ProcessSegment("", @"\id");
					// no text provided in segment, just the refs
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
				// REVIEW (EberhardB): where exactly do we expect it to throw? Consider using
				// Assert.Throws instead of the catch block (http://www.nunit.org/index.php?p=exceptionAsserts&r=2.5.9)
				Assert.AreEqual(string.Format("Back translation does not correspond to a vernacular picture.{1}" +
						"A back translation picture must correspond to a picture in the corresponding vernacular paragraph." +
						"{1}{1}\\fig {0}{1}Attempting to read GEN  Chapter: 1  Verse: 1",
						Path.Combine(Path.GetTempPath(), "BT for first photo"), Environment.NewLine), e.Message);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_EmptyBTParaFootnote()
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
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo" + StringUtils.kChObject, para.Contents.Text);
			VerifyFootnoteWithTranslation(0, "Primer pata nota", string.Empty, string.Empty,
				ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.GetBT();
			Assert.AreEqual("11First verse" + StringUtils.kChObject,
				translation.Translation.get_String(m_wsAnal).Text);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BackTranslationNonInterleaved_BTFootnoteBeginsPara()
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
			Assert.AreEqual("Primera Seccion" + StringUtils.kChObject, para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.GetBT();
			ITsString tss = translation.Translation.get_String(m_wsAnal);
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
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
}
