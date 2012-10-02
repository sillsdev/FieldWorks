// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2007' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrInMemoryFdoTestBase.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;
using System.Text;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An in-memory (FDOBackendProviderType.kMemoryOnly) base class
	/// that supports Scripture tests. This class has methods to populate various Scripture-
	/// related objects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrInMemoryFdoTestBase : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary></summary>
		public const string kParagraphText =
			"This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.";

		/// <summary></summary>
		protected IScripture m_scr;
		/// <summary></summary>
		protected ILangProject m_lp;
		/// <summary></summary>
		protected IScrBookRepository m_repoScrBook;
		/// <summary></summary>
		protected IScrSectionRepository m_repoScrSection;
		/// <summary></summary>
		protected IScrTxtParaRepository m_repoStTxtPara;
		/// <summary></summary>
		protected int m_wsEn;
		/// <summary></summary>
		protected int m_wsDe;

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to TestSetup to clear book filters between tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().Clear();

			base.TestSetup();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes Scripture for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			ScrReferenceTests.InitializeScrReferenceForTests();
			m_lp = Cache.LanguageProject;
			m_repoScrBook = Cache.ServiceLocator.GetInstance<IScrBookRepository>();
			m_repoScrSection = Cache.ServiceLocator.GetInstance<IScrSectionRepository>();
			m_repoStTxtPara = Cache.ServiceLocator.GetInstance<IScrTxtParaRepository>();
			IWritingSystem temp;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en", out temp);
			m_wsEn = temp.Handle;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out temp);
			m_wsDe = temp.Handle;

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				// The following has the side-effect of creating and initializing the ScrRefSystem
				m_scr = m_lp.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
				CreateStandardScriptureStyles();
				InitializeScrAnnotationCategories();
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			// This causes a massive deletion of all objects in the database tree that fall under
			// scripture. Since this shouldn't have to be set to null since we are about to
			// kill the cache anyways, there is no point in doing the extra work of deleting
			// all of those objects.
			//m_lp.TranslatedScriptureOA = null;
			m_scr = null;
			m_lp = null;

			base.FixtureTeardown();
		}
		#endregion

		#region Initialization methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes scripture annotation categories
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeScrAnnotationCategories()
		{
			// Initialize the annotation category possibility list.
			m_scr.NoteCategoriesOA =
				Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();

			// Add an annotation category (for Discourse)
			ICmPossibility category =
				Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			m_scr.NoteCategoriesOA.PossibilitiesOS.Add(category);
			category.Name.set_String(m_wsEn, TsStringUtils.MakeTss("Discourse", m_wsEn));

			// Add an annotation category (for Grammar)
			category = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			m_scr.NoteCategoriesOA.PossibilitiesOS.Add(category);
			category.Name.set_String(m_wsEn, TsStringUtils.MakeTss("Grammar", m_wsEn));

			// add a sub-annotation category (for "Pronominal reference")
			ICmPossibility subCategory =
				Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			category.SubPossibilitiesOS.Add(subCategory);
			subCategory.Name.set_String(m_wsEn, TsStringUtils.MakeTss("Pronominal reference", m_wsEn));

			// add a sub-sub-annotation category (for "Extended use")
			subCategory = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			category.SubPossibilitiesOS.Add(subCategory);
			subCategory.Name.set_String(m_wsEn, TsStringUtils.MakeTss("Extended use", m_wsEn));

			// Add an annotation category (for Gnarly)
			category = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			m_scr.NoteCategoriesOA.PossibilitiesOS.Add(category);
			category.Name.set_String(m_wsEn, TsStringUtils.MakeTss("Gnarly", m_wsEn));
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a Scripture section to the specified book in the mock fdocache
		/// </summary>
		/// <param name="book">the book</param>
		/// ------------------------------------------------------------------------------------
		public IScrSection AddSectionToMockedBook(IScrBook book)
		{
			return AddSectionToMockedBook(book, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a section (Scripture or Intro) to the sepecified book in the mock fdocache
		/// </summary>
		/// <param name="book">the book</param>
		/// <param name="introSection">true for an intro section</param>
		/// ------------------------------------------------------------------------------------
		public IScrSection AddSectionToMockedBook(IScrBook book, bool introSection)
		{
			var section = Cache.ServiceLocator.GetInstance<IScrSectionFactory>().Create();
			book.SectionsOS.Add(section);

			// setup the new section
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			section.ContentOA = stTextFactory.Create();
			section.HeadingOA = stTextFactory.Create();

			section.VerseRefEnd = book.CanonicalNum * 1000000 + 1 * 1000 + (introSection ? 0 : 1);

			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book and sticks two simple sections into it.
		/// </summary>
		/// <param name="nBook">The one-based canonnical book number</param>
		/// <param name="bookName">The English name of the book</param>
		/// <returns>Newly created book</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook AddBookWithTwoSections(int nBook, string bookName)
		{
			var book = AddBookToMockedScripture(nBook, bookName);
			AddTitleToMockedBook(book, bookName);
			var section1 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section1, "Head1", ScrStyleNames.SectionHead);
			AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			var section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Head2", ScrStyleNames.SectionHead);
			AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new section to the given book, having the specified text as the section
		/// head.
		/// </summary>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head</param>
		/// <param name="startRef">The BBCCCVVV reference for the start/min</param>
		/// <param name="endRef">The BBCCCVVV reference for the end/max</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrSection CreateSection(IScrBook book, string sSectionHead, int startRef, int endRef)
		{
			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, sSectionHead, ScrStyleNames.SectionHead);
			//note: the InMemoryCache has created the StTexts owned by this section
			section.VerseRefMin = section.VerseRefStart = startRef;
			section.VerseRefMax = section.VerseRefEnd = endRef;
			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new section to the given book, having the specified text as the section
		/// head. The new section will have an empty content text created also.
		/// </summary>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrSection CreateSection(IScrBook book, string sSectionHead)
		{
			return CreateSection(book, sSectionHead,
				new ScrReference(book.CanonicalNum, 1, 1, m_scr.Versification),
				new ScrReference(book.CanonicalNum, 1, 1, m_scr.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new intro section to the given book, having the specified text as the section
		/// head. The new section will have an empty content text created also.
		/// </summary>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrSection CreateIntroSection(IScrBook book, string sSectionHead)
		{
			return CreateSection(book, sSectionHead,
				new ScrReference(book.CanonicalNum, 1, 0, m_scr.Versification),
				new ScrReference(book.CanonicalNum, 1, 0, m_scr.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a content paragraph to the specified section in the mock fdocache
		/// </summary>
		/// <param name="section">the hvo of the section</param>
		/// <param name="paraStyleName">the paragraph style name</param>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara AddParaToMockedSectionContent(IScrSection section, string paraStyleName)
		{
			return (IScrTxtPara)AddParaToMockedText(section.ContentOA, paraStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a section head paragraph to the sepecified book and section in the mock
		/// fdocache
		/// </summary>
		/// <param name="section">the hvo of the section</param>
		/// <param name="headingText">The text for the section head</param>
		/// <param name="headingStyleName">The style for the section head paragraph</param>
		/// <returns>The newly created section head paragraph</returns>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara AddSectionHeadParaToSection(IScrSection section, string headingText,
			string headingStyleName)
		{
			var frWs = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var para = section.HeadingOA.AddNewTextPara(headingStyleName);
			para.Contents = Cache.TsStrFactory.MakeString(headingText, frWs);
			return (IScrTxtPara)para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function to add an empty normal paragraph to the given section.
		/// </summary>
		/// <param name="section">The section where the empty paragraph will be added.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara AddEmptyPara(IScrSection section)
		{
			return AddEmptyPara(section, ScrStyleNames.NormalParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function to add an empty noraml paragraph to the given section.
		/// </summary>
		/// <param name="section">The section where the empty paragraph will be added.</param>
		/// <param name="style">The style for the added paragraph.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara AddEmptyPara(IScrSection section, string style)
		{
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = style;
			paraBldr.AppendRun(String.Empty, StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			return (IScrTxtPara)paraBldr.CreateParagraph(section.ContentOA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a mindless footnote (i.e., it's marker, paragraph style, etc. won't be set)
		/// to a paragraph in a book.
		/// </summary>
		/// <param name="book">Book to insert footnote into</param>
		/// <param name="para">the paragarph in which to insert the footnote ORC</param>
		/// <param name="ichPos">the zero-based character offset at which to insert the footnote
		/// ORC into the paragraph</param>
		/// <returns>the new footnote</returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote AddFootnote(IScrBook book, IStTxtPara para, int ichPos)
		{
			// Create the footnote
			IScrFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			book.FootnotesOS.Add(footnote);
			ITsStrBldr tsStrBldr = para.Contents.GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote.Guid, FwObjDataTypes.kodtOwnNameGuidHot,
				tsStrBldr, ichPos, ichPos,
				Cache.DefaultVernWs);
			para.Contents = tsStrBldr.GetString();

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a footnote to a paragraph in a book. This overload also sets the footnote text
		/// and footnote style.
		/// </summary>
		/// <param name="book"></param>
		/// <param name="para">the paragarph in which to insert the footnote ORC</param>
		/// <param name="ichPos">the zero-based character offset at which to insert the footnote
		/// ORC into the paragraph</param>
		/// <param name="footnoteText">text for the footnote</param>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote AddFootnote(IScrBook book, IStTxtPara para, int ichPos, string footnoteText)
		{
			IScrFootnote footnote = AddFootnote(book, para, ichPos);

			// Create the footnote paragraph with the given footnoteText
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalFootnoteParagraph;
			paraBldr.AppendRun(footnoteText, StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs));
			paraBldr.CreateParagraph(footnote);
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create several standard TE scripture styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateStandardScriptureStyles()
		{
			AddScrStyle("Normal", ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose, false);
			AddScrStyle("Paragraph", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false);
			AddScrStyle("Section Head", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Intro Paragraph", ContextValues.Intro, StructureValues.Body, FunctionValues.Prose, false, 2);
			AddScrStyle("Intro Section Head", ContextValues.Intro, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Intro List Item1", ContextValues.Intro, StructureValues.Body, FunctionValues.Line, false);
			AddScrStyle("Intro List Item2", ContextValues.Intro, StructureValues.Body, FunctionValues.Line, false);
			AddScrStyle("Intro List Item3", ContextValues.Intro, StructureValues.Body, FunctionValues.Line, false);
			AddScrStyle("Chapter Number", ContextValues.Text, StructureValues.Body, FunctionValues.Chapter, true);
			AddScrStyle("Verse Number", ContextValues.Text, StructureValues.Body, FunctionValues.Verse, true);
			AddScrStyle("Title Main", ContextValues.Title, StructureValues.Body, FunctionValues.Prose, false);
			AddScrStyle("Note General Paragraph", ContextValues.Note, StructureValues.Undefined, FunctionValues.Prose, false);
			AddScrStyle("Note Marker", ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Note Target Reference", ContextValues.InternalMappable, StructureValues.Undefined, FunctionValues.Footnote, true);
			AddScrStyle("Note Cross-Reference Paragraph", ContextValues.Note, StructureValues.Undefined, FunctionValues.Prose, false);
			AddScrStyle("Line1", ContextValues.Text, StructureValues.Body, FunctionValues.Line, false);
			AddScrStyle("Line2", ContextValues.Text, StructureValues.Body, FunctionValues.Line, false);
			AddScrStyle("Line3", ContextValues.Text, StructureValues.Body, FunctionValues.Line, false, 2);
			AddScrStyle("Section Range Paragraph", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Section Head Major", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Section Head Minor", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Parallel Passage Reference", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Title Secondary", ContextValues.Title, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Title Tertiary", ContextValues.Title, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Remark", ContextValues.Annotation, StructureValues.Undefined, FunctionValues.Prose, false);
			AddScrStyle("Hyperlink", ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Emphasis", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Key Word", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Gloss", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Untranslated Word", ContextValues.BackTranslation, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Quoted Text", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Doxology", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true, 3);
			AddScrStyle("List Item1", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false, 3);
			AddScrStyle("List Item2", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false, 4);
			AddScrStyle("List Item3", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false, 4);
			AddScrStyle("Verse Number In Note", ContextValues.Note, StructureValues.Undefined, FunctionValues.Footnote, true, 3);
			AddScrStyle("Alternate Reading", ContextValues.Note, StructureValues.Undefined, FunctionValues.Footnote, true, 2);
			AddScrStyle("Stanza Break", ContextValues.Text, StructureValues.Body, FunctionValues.StanzaBreak, false, 1);
			// Note: Other "standard" Scr styles could be created here, but they aren't needed for tests
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style.
		/// </summary>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <param name="userLevel">User level</param>
		/// ------------------------------------------------------------------------------------
		protected void AddScrStyle(string name, ContextValues context, StructureValues structure,
			FunctionValues function, bool isCharStyle, int userLevel)
		{
			AddStyle(m_lp.TranslatedScriptureOA.StylesOC, name, context, structure, function,
				isCharStyle, userLevel, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style, with user level 0.
		/// </summary>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// ------------------------------------------------------------------------------------
		protected void AddScrStyle(string name, ContextValues context, StructureValues structure,
			FunctionValues function, bool isCharStyle)
		{
			AddScrStyle(name, context, structure, function, isCharStyle, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a title StText to the specified book in the mock fdocache
		/// </summary>
		/// <param name="book">the book hvo</param>
		/// <param name="titleText">The text for the title of the book</param>
		/// ------------------------------------------------------------------------------------
		public IStText AddTitleToMockedBook(IScrBook book, string titleText)
		{
			return AddTitleToMockedBook(book, titleText, Cache.WritingSystemFactory.GetWsFromStr("fr"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a title StText to the specified book in the fdocache
		/// </summary>
		/// <param name="book">The book.</param>
		/// <param name="titleText">The title text.</param>
		/// <param name="ws">The writing system to use.</param>
		/// <returns>The StText of the title</returns>
		/// ------------------------------------------------------------------------------------
		public IStText AddTitleToMockedBook(IScrBook book, string titleText, int ws)
		{
			ITsString titleTss = Cache.TsStrFactory.MakeString(titleText, ws);
			return AddTitleToMockedBook(book, titleTss, ScrStyleNames.MainBookTitle);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Adds a title StText to the specified book in the fdocache
		/// </summary>
		/// <param name="book">the book</param>
		/// <param name="contents">Optional paragraph content for title. If this parameter
		/// is <c>null</c>, no paragraphs are added to the StText.</param>
		/// <param name="styleName">Style name for paragraph content</param>
		/// ------------------------------------------------------------------------------------
		public IStText AddTitleToMockedBook(IScrBook book, ITsString contents, string styleName)
		{
			IStText title = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			book.TitleOA = title;

			if (contents != null)
			{
				// Add paragraph to title.
				IStTxtPara para = title.AddNewTextPara(styleName);
				// Setup the new paragraph.
				para.Contents = contents;
			}

			return title;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a book to the mock fdocache
		/// </summary>
		/// <param name="nBookNumber">the one-based canonical book number, eg 2 for Exodus</param>
		/// <param name="bookName">the English name of the book</param>
		/// <returns>A new ScrBook.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IScrBook AddBookToMockedScripture(int nBookNumber, string bookName)
		{
			Debug.Assert(nBookNumber > 0 && nBookNumber <= BCVRef.LastBook);

			IFdoServiceLocator servloc = Cache.ServiceLocator;
			IScrBook book = servloc.GetInstance<IScrBookFactory>().Create(
				m_lp.TranslatedScriptureOA.ScriptureBooksOS, nBookNumber);

			int frWs = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("fr");
			ITsString tss = Cache.TsStrFactory.MakeString(bookName, frWs);
			book.Name.set_String(frWs, tss);
			book.BookIdRA.BookName.set_String(frWs, tss);

			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a book to the system as an archive (ie the book is by itself in its own
		/// archive)
		/// </summary>
		/// <param name="nBookNumber">the one-based canonical book number, eg 2 for Exodus</param>
		/// <param name="bookName">the English name of the book</param>
		/// ------------------------------------------------------------------------------------
		protected IScrBook AddArchiveBookToMockedScripture(int nBookNumber, string bookName)
		{
			// Set up the draft
			IScrDraft version = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create("test version");
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(version.BooksOS, nBookNumber);

			// Set up the book
			int enWs = Cache.WritingSystemFactory.GetWsFromStr("en");
			book.Name.set_String(enWs, Cache.TsStrFactory.MakeString(bookName, enWs));

			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a publication and adds it to a collection of Publications on the
		/// CmMajorObject AnnotationDefs.
		/// </summary>
		/// <param name="pageHeight">Height of the page.</param>
		/// <param name="pageWidth">Width of the page.</param>
		/// <param name="fIsLandscape">if set to <c>true</c> the publication is landscape.</param>
		/// <param name="name">The name of the publication.</param>
		/// <param name="gutterMargin">The gutter margin.</param>
		/// <param name="bindingSide">The side on which the publication will be bound (i.e., the
		/// gutter location).</param>
		/// <param name="footnoteSepWidth">Width of the footnote seperator.</param>
		/// <returns>the new publication</returns>
		/// <remarks>Adds the publication to AnnotationDefs because we need a
		/// CmMajorObject where we can attach the Publication and Scripture is not visible here.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public IPublication CreatePublication(int pageHeight, int pageWidth, bool fIsLandscape,
			string name, int gutterMargin, BindingSide bindingSide, int footnoteSepWidth)
		{
			Debug.Assert(Cache.LangProject != null, "The language project is null");
			Debug.Assert(Cache.LangProject.AnnotationDefsOA != null,
				"The annotation definitions are null.");

			IPublication pub = Cache.ServiceLocator.GetInstance<IPublicationFactory>().Create();
			Cache.LangProject.AnnotationDefsOA.PublicationsOC.Add(pub);
			pub.PageHeight = pageHeight;
			pub.PageWidth = pageWidth;
			pub.IsLandscape = fIsLandscape;
			pub.Name = name;
			pub.GutterMargin = gutterMargin;
			pub.BindingEdge = bindingSide;
			pub.FootnoteSepWidth = footnoteSepWidth;

			return pub;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the division to the publication.
		/// </summary>
		/// <param name="pub">The publication where the division will be added.</param>
		/// <param name="fDifferentFirstHF">if set to <c>true</c> publication has a different
		/// first header/footer].</param>
		/// <param name="fDifferentEvenHF">if set to <c>true</c> publication has a different even
		/// header/footer.</param>
		/// <param name="startAt">Enumeration of options for where the content of the division
		/// begins</param>
		/// <returns>the new division</returns>
		/// ------------------------------------------------------------------------------------
		public IPubDivision AddDivisionToPub(IPublication pub, bool fDifferentFirstHF,
			bool fDifferentEvenHF, DivisionStartOption startAt)
		{
			IPubDivision div = Cache.ServiceLocator.GetInstance<IPubDivisionFactory>().Create();
			pub.DivisionsOS.Add(div);
			div.DifferentFirstHF = fDifferentFirstHF;
			div.DifferentEvenHF = fDifferentEvenHF;
			div.StartAt = startAt;
			return div;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new paragraph
		/// </summary>
		/// <param name="section">The section to which the paragraph will be added</param>
		/// <param name="chapterNumber">the chapter number to create or <c>null</c> if no
		/// chapter number is desired.</param>
		/// <param name="verseNumber">the chapter number to create or <c>null</c> if no
		/// verse number is desired.</param>
		/// <returns>The newly created paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrTxtPara SetupParagraph(IScrSection section, string chapterNumber, string verseNumber)
		{
			IScrTxtPara para = AddParaToMockedSectionContent(section, "Paragraph");

			if (chapterNumber != null)
				AddRunToMockedPara(para, chapterNumber, "Chapter Number");
			if (verseNumber != null)
				AddRunToMockedPara(para, verseNumber, "Verse Number");
			AddRunToMockedPara(para, kParagraphText, null);

			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a mindless footnote (i.e., it's marker, paragraph style, etc. won't be set).
		/// </summary>
		/// <param name="book">Book to insert footnote into</param>
		/// <param name="para">Paragraph to insert footnote into</param>
		/// <param name="iFootnotePos">The 0-based index of the new footnote in the collection
		/// of footnotes owned by the book</param>
		/// <param name="ichPos">The 0-based character offset into the paragraph</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected IStFootnote InsertTestFootnote(IScrBook book, IStTxtPara para,
			int iFootnotePos, int ichPos)
		{
			// Create the footnote
			IScrFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			book.FootnotesOS.Insert(iFootnotePos, footnote);

			// Update the paragraph contents to include the footnote marker
			ITsStrBldr tsStrBldr = para.Contents.GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote.Guid, FwObjDataTypes.kodtOwnNameGuidHot,
				tsStrBldr, ichPos, ichPos, book.Cache.DefaultVernWs);
			para.Contents = tsStrBldr.GetString();

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure footnote exists and is referred to properly in the paragraph contents
		/// </summary>
		/// <param name="footnote">The expected footnote</param>
		/// <param name="para">The paragraph whose contents should contain an ORC for the footnote</param>
		/// <param name="ich">Character position where ORC should be</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyFootnote(IStFootnote footnote, IStTxtPara para, int ich)
		{
			Guid guid = footnote.Guid;
			ITsString tss = para.Contents;
			int iRun = tss.get_RunAt(ich);
			ITsTextProps orcPropsParaFootnote = tss.get_Properties(iRun);
			string objData = orcPropsParaFootnote.GetStrPropValue(
				(int)FwTextPropType.ktptObjData);
			Assert.IsNotNull(objData, "Footnote not found at character offset " + ich);
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtOwnNameGuidHot, objData[0]);
			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid newFootnoteGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
			Assert.AreEqual(guid, newFootnoteGuid);
			string sOrc = tss.get_RunText(iRun);
			Assert.AreEqual(StringUtils.kChObject, sOrc[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an annotation for the given paragraph and Scripture reference.
		/// </summary>
		/// <param name="cmObject">The object where we want to add the annotation</param>
		/// <param name="scrRef">The Scripture reference for the annotation.</param>
		/// <param name="noteType">Type of the note: NoteType.Consultant or NoteType.Translator.</param>
		/// <param name="sDiscussion">The text to put in the discussion field.</param>
		/// <returns>the created annotation</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote AddAnnotation(ICmObject cmObject, BCVRef scrRef, NoteType noteType,
			string sDiscussion)
		{
			IScrScriptureNote annotation = AddAnnotation(cmObject, scrRef, noteType);
			if (!String.IsNullOrEmpty(sDiscussion))
			{
				StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
				ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, Cache.DefaultAnalWs);
				paraBldr.AppendRun(sDiscussion, propsBldr.GetTextProps());
				annotation.DiscussionOA = Cache.ServiceLocator.GetInstance<IStJournalTextFactory>().Create();
				paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
				InitializeText(paraBldr, annotation.DiscussionOA);
			}
			return annotation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the text for the paragraph with the specified builder, or create an
		/// empty paragraph if the builder is null.
		/// </summary>
		/// <param name="bldr">paragraph builder</param>
		/// <param name="text">StText</param>
		/// ------------------------------------------------------------------------------------
		private void InitializeText(StTxtParaBldr bldr, IStText text)
		{
			if (bldr == null)
			{
				IStTxtPara para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
				text.ParagraphsOS.Add(para);
				para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.Remark);
			}
			else
			{
				bldr.CreateParagraph(text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an annotation for the given paragraph and Scripture reference.
		/// </summary>
		/// <param name="cmObject">The object where we want to add the annotation</param>
		/// <param name="scrRef">The Scripture reference for the annotation.</param>
		/// <param name="noteType">Type of the note: NoteType.Consultant or NoteType.Translator.
		/// </param>
		/// <returns>the created annotation</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote AddAnnotation(ICmObject cmObject, BCVRef scrRef, NoteType noteType)
		{
			Debug.Assert(scrRef.Book > 0);
			Debug.Assert(m_lp != null);
			Debug.Assert(m_lp.TranslatedScriptureOA != null);
			IScripture scr = m_lp.TranslatedScriptureOA;
			IScrBookAnnotations annotations = scr.BookAnnotationsOS[scrRef.Book - 1];
			int iPos;
			IScrScriptureNote annotation = annotations.InsertNote(scrRef, scrRef, cmObject, cmObject,
				CmAnnotationDefnTags.kguidAnnConsultantNote, out iPos);
			Guid noteTypeGuid = Guid.Empty;
			switch (noteType)
			{
				case NoteType.Consultant:
					noteTypeGuid = CmAnnotationDefnTags.kguidAnnConsultantNote;
					break;

				case NoteType.Translator:
					noteTypeGuid = CmAnnotationDefnTags.kguidAnnTranslatorNote;
					break;

				case NoteType.CheckingError:
					noteTypeGuid = CmAnnotationDefnTags.kguidAnnCheckingError;
					break;
			}
			annotation.AnnotationTypeRA =
				Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().GetObject(noteTypeGuid);
			// ENHANCE: Set source to either human or computer agent, depending on NoteType
			annotation.SourceRA = null;
			annotation.DateCreated = DateTime.Now;

			return annotation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book with 3 sections with the following layout:
		///
		///		  (sBookName)
		///		   Heading 1
		///	Intro text
		///		   Heading 2
		///	(1)1Verse one. 2Verse two.
		///	3Verse three.
		///	4Verse four. 5Verse five.
		///		   Heading 3
		///	6Verse six. 7Verse seven.
		///
		///	(1) = chapter number 1
		/// </summary>
		/// <param name="bookNum">The book num.</param>
		/// <param name="sBookName">Name of the s book.</param>
		/// <returns>A book with verses</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateBookData(int bookNum, string sBookName)
		{
			IScrBook book = AddBookToMockedScripture(bookNum, sBookName);
			AddTitleToMockedBook(book, sBookName);

			IScrSection section1 = AddSectionToMockedBook(book, true);
			AddSectionHeadParaToSection(section1, "Heading 1", ScrStyleNames.IntroSectionHead);
			IScrTxtPara para11 = AddParaToMockedSectionContent(section1, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para11, "Intro text. We need lots of stuff here so that our footnote tests will work.", null);

			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Heading 2", ScrStyleNames.SectionHead);
			IScrTxtPara para21 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para21, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para21, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse one. ", null);
			AddRunToMockedPara(para21, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse two.", null);

			IScrTxtPara para22 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para22, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para22, "Verse three.", null);

			IScrTxtPara para23 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para23, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para23, "Verse four. ", null);
			AddRunToMockedPara(para23, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para23, "Verse five.", null);

			IScrSection section3 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section3, "Heading 3", ScrStyleNames.SectionHead);
			IScrTxtPara para31 = AddParaToMockedSectionContent(section3, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para31, "6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para31, "Verse six. ", null);
			AddRunToMockedPara(para31, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para31, "Verse seven.", null);

			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book (Exodus) with 3 sections with the following layout:
		///
		///			(Exodus)
		///		   Heading 1
		///	Intro text
		///		   Heading 2
		///	(1)1Verse one. 2Verse two.
		///	3Verse three.
		///	4Verse four. 5Verse five.
		///		   Heading 3
		///	6Verse six. 7Verse seven.
		///
		///	(1) = chapter number 1
		/// </summary>
		/// <returns>the book of Exodus for testing</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateExodusData()
		{
			return CreateBookData(2, "Exodus");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function to add a verse to the given paragraph: an optional chapter number,
		/// optional verse number, and then one run of verse text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AddVerse(IScrTxtPara para, int chapter, int verse, string verseText)
		{
			AddVerse(para, chapter, (verse != 0) ? verse.ToString() : string.Empty, verseText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function to add a verse to the given paragraph: an optional chapter number,
		/// optional verse number string (string.empty for none), and then one run of verse text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AddVerse(IScrTxtPara para, int chapter, string verseNum, string verseText)
		{
			if (chapter > 0)
				AddRunToMockedPara(para, chapter.ToString(), ScrStyleNames.ChapterNumber);
			if (verseNum != string.Empty)
				AddRunToMockedPara(para, verseNum, ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, verseText, null);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an ITsString from a format string.
		/// </summary>
		/// <param name="book">The book for inserting footnotes into (can be null if there are
		/// no footnotes)</param>
		/// <param name="para">The paragraph to insert the formatted text into.</param>
		/// <param name="format">format string, which may include these special "commands":
		/// \c - insert a chapter number run
		/// \v - insert a verse number run
		/// \* - insert a simple text run
		/// \*(char style name) - insert a text run with a character style.
		/// \i - insert a picture (text must be the text rep of the picture (see pic code))
		/// \f - insert a footnote</param>
		/// \^ - end of the current footnote (required for every footnote)
		/// <param name="ws">writing system for each run of text</param>
		/// ------------------------------------------------------------------------------------
		public void AddFormatTextToMockedPara(IScrBook book, IStTxtPara para, string format, int ws)
		{
			para.Contents = CreateFormatText(book, para.Contents.GetBldr(), format, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an ITsString from a format string.
		/// </summary>
		/// <param name="book">The book for inserting footnotes into (can be null if there are
		/// no footnotes)</param>
		/// <param name="strBldr">The ITsStrBldr to add the text to (if null a new one will be
		/// created)</param>
		/// <param name="format">format string, which may include these special "commands":
		/// \c - insert a chapter number run
		/// \v - insert a verse number run
		/// \* - insert a simple text run
		/// \*(char style name) - insert a text run with a character style.
		/// \i - insert a picture (text must be the text rep of the picture (see pic code))
		/// \f - insert a footnote</param>
		/// \^ - end of the current footnote (required for every footnote)
		/// <param name="ws">writing system for each run of text</param>
		/// <returns>the completed ITsString</returns>
		/// ------------------------------------------------------------------------------------
		public ITsString CreateFormatText(IScrBook book, ITsStrBldr strBldr,
			string format, int ws)
		{
			if (strBldr == null)
				strBldr = TsStrBldrClass.Create();

			if (string.IsNullOrEmpty(format))
				strBldr.Replace(0, 0, string.Empty, StyleUtils.CharStyleTextProps(null, ws));

			int nChapter = 1, nVerse = 1;

			for (int i = 0; i < format.Length; )
			{
				// skip the backslash (verify that it is there first)
				if (format[i] != '\\')
					Debug.Assert(false, @"Format string must start every text run with \*: {0}" +
						format.Substring(i));
				if (++i >= format.Length)
					break;

				// save the field type character
				char field = format[i];
				if (++i >= format.Length)
					break;

				// determine the endmarker we'll look for
				string endMarker = (field == 'f' || field == 'i') ? @"\^" : @"\";

				// extract the data for the field
				int lim = format.IndexOf(endMarker, i);
				if (lim == -1 && field == 'f')
					Debug.Assert(false, @"Format string must have a \^ to end footnote!");
				else if (lim == -1)
					lim = format.Length;
				string fieldData = format.Substring(i, lim - i);

				// remember pos of next backslash, or the end of the format string
				i = lim;
				//skip empty commands, such as \^
				if (fieldData.Length == 0)
					continue;

				// what kind of command is this?
				switch (field)
				{
					case 'c':
						Int32.TryParse(fieldData, out nChapter);
						AddRunToStrBldr(strBldr, fieldData, ws, "Chapter Number");
						break;
					case 'v':
						IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
						string sBridge = Cache.LangProject.TranslatedScriptureOA.Bridge;
						string[] pieces = fieldData.Split(new string[] { sBridge }, 2, StringSplitOptions.RemoveEmptyEntries);
						StringBuilder strb = new StringBuilder();
						string sLastVerse = pieces[pieces.Length - 1];
						Int32.TryParse(fieldData, out nVerse);
						if (vernWs.RightToLeftScript && pieces.Length == 2)
						{
							// The verse number run has a bridge and is in a right-to-left
							// writing system. Construct a verse bridge with right-to-left
							// characters adjacent to the bridge character.
							strb.Append(pieces[0] + '\u200f' + sBridge + '\u200f' + pieces[1]);
						}
						else
							strb.Append(fieldData);

						AddRunToStrBldr(strBldr, strb.ToString(), ws, "Verse Number");
						break;
					case 'f':
						IScrFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
						book.FootnotesOS.Add(footnote);
						TsStringUtils.InsertOrcIntoPara(footnote.Guid, FwObjDataTypes.kodtOwnNameGuidHot, strBldr, strBldr.Length, strBldr.Length, ws);
						footnote.FootnoteMarker = TsStringHelper.MakeTSS("a", ws); // auto-generate
						if (fieldData.IndexOf(@"\f") != -1)
							Debug.Assert(false, @"Format string must not nest \f within another \f..\^");
						IScrTxtPara para = AppendParagraph(footnote, fieldData, ws); //recursively calls CreateText to process any char styles
						para.StyleRules = StyleUtils.ParaStyleTextProps("Note General Paragraph");
						//TODO: add multiple paragraphs for a footnote
						break;
					case 'i':
						ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create(
							fieldData, CmFolderTags.LocalPictures,
							new BCVRef(book.CanonicalNum, nChapter, nVerse),
							book.Cache.LangProject.TranslatedScriptureOA as IPictureLocationBridge);
						picture.InsertORCAt(strBldr, strBldr.Length);
						break;
					case '*':
						{
							int wsRun = ws;
							string charStyleName = null;
							// if we have an optional character style in parens, process it
							if (fieldData[0] == '(')
							{
								int endParen = fieldData.IndexOf(")", 0);
								Debug.Assert(endParen > 1); // caller must provide something within parens
								if (endParen != -1)
								{
									charStyleName = fieldData.Substring(1, endParen - 1);
									fieldData = fieldData.Substring(endParen + 1);
								}
							}
							// if we have an optional writing system specifier, process it
							if (fieldData[0] == '|' && fieldData.Length > 3 && fieldData[3] == '|')
							{
								wsRun = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr(fieldData.Substring(1, 2));
								if (wsRun > 0)
									fieldData = fieldData.Substring(4);
								else
									wsRun = ws;
							}
							AddRunToStrBldr(strBldr, fieldData, wsRun, charStyleName);
							break;
						}
				}
			}
			return strBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function: Append a run to the given string builder
		/// </summary>
		/// <param name="strBldr"></param>
		/// <param name="text"></param>
		/// <param name="ws"></param>
		/// <param name="charStyle"></param>
		/// ------------------------------------------------------------------------------------
		private static void AddRunToStrBldr(ITsStrBldr strBldr, string text, int ws, string charStyle)
		{
			strBldr.Replace(strBldr.Length, strBldr.Length, text,
				StyleUtils.CharStyleTextProps(charStyle, ws));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a paragraph with the given text, and append it to the given footnote's
		/// content.
		/// </summary>
		/// <param name="footnote">footnote to append to</param>
		/// <param name="format">(See CreateText for the definition of the format string)</param>
		/// <param name="ws">writing system for the paragraph text</param>
		/// <returns>the new paragraph</returns>
		/// ------------------------------------------------------------------------------------
		private IScrTxtPara AppendParagraph(IScrFootnote footnote, string format, int ws)
		{
			// insert a new para in the footnote
			IScrTxtPara para = (IScrTxtPara)AddParaToMockedText(footnote, "Note General Paragraph");
			para.Contents = CreateFormatText(null, para.Contents.GetBldr(), format, ws);
			return para;
		}

		#endregion
	}
}
