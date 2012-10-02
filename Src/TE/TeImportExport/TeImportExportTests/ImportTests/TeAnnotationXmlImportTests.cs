// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeAnnotationXmlImport.cs
// Responsibility: DavidO
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.TE.ImportTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeAnnotationXmlImportTests : ScrInMemoryFdoTestBase
	{
		#region DummyXmlScrAnnotationsList
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DummyXmlScrAnnotationsList : XmlScrAnnotationsList
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Calls the write to cache.
			/// </summary>
			/// <param name="cache">The cache.</param>
			/// <param name="styleSheet">The style sheet.</param>
			/// --------------------------------------------------------------------------------
			public void CallWriteToCache(FdoCache cache, FwStyleSheet styleSheet)
			{
				WriteToCache(cache, styleSheet, null);
			}
		}
		#endregion

		private FwStyleSheet m_stylesheet;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();

			m_scrInMemoryCache.InitializeWritingSystemEncodings();
			m_scrInMemoryCache.InitializeAnnotationDefs();
			m_scrInMemoryCache.InitializeScrAnnotationCategories();
			m_stylesheet = new FwStyleSheet();
			m_stylesheet.Init(Cache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);

			StyleProxyListManager.Initialize(m_stylesheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Exit()
		{
			base.Exit();
			StyleProxyListManager.Cleanup();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving annotations to the cache
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SavingDeserializedAnnotationsToCache()
		{
			DateTime now = DateTime.Now;
			DateTime utcNow = now.ToUniversalTime();

			XmlScrNote ann = CreateNote();
			ann.BeginScrRef = "GEN 2:8";
			ann.ResolutionStatus = NoteStatus.Closed;
			ann.AnnotationTypeGuid = LangProject.kguidAnnTranslatorNote.ToString();
			ann.DateTimeCreated = utcNow.ToString();
			ann.DateTimeModified = utcNow.AddDays(1).ToString();
			ann.DateTimeResolved = utcNow.AddDays(2).ToString();
			AddParasTo(ann.Discussion, "This is my", "discussion");
			AddParasTo(ann.Resolution, "This is my", "resolution for", "the note");
			AddParasTo(ann.Quote, "This is the", "quoted text");
			AddParasTo(ann.Suggestion, "This is", "my", "suggestion");

			DummyXmlScrAnnotationsList list = new DummyXmlScrAnnotationsList();
			list.Annotations.Add(ann);

			list.CallWriteToCache(Cache, m_stylesheet);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[0];
			Assert.AreEqual(1, annotations.NotesOS.Count);

			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual(NoteType.Translator, note.AnnotationType);
			Assert.IsTrue(AreDateTimesClose(now, note.DateCreated));
			Assert.IsTrue(AreDateTimesClose(now.AddDays(1), note.DateModified));
			Assert.IsTrue(AreDateTimesClose(now.AddDays(2), note.DateResolved));

			TestAnnotationField(note.QuoteOA, "This is the", "quoted text");
			TestAnnotationField(note.DiscussionOA, "This is my", "discussion");
			TestAnnotationField(note.ResolutionOA, "This is my", "resolution for", "the note");
			TestAnnotationField(note.RecommendationOA, "This is", "my", "suggestion");

			Assert.AreEqual(0, note.ResponsesOS.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving annotations to the cache when a paragraph in the suggestion of the
		/// annotation contains a hyperlink
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SavingDeserializedAnnotationsToCache_WithHyperlink()
		{
			DateTime now = DateTime.Now;
			DateTime utcNow = now.ToUniversalTime();

			XmlScrNote ann = CreateNote();
			ann.BeginScrRef = "GEN 2:8";
			ann.ResolutionStatus = NoteStatus.Closed;
			ann.AnnotationTypeGuid = LangProject.kguidAnnTranslatorNote.ToString();
			ann.DateTimeCreated = utcNow.ToString();
			ann.DateTimeModified = utcNow.AddDays(1).ToString();
			ann.DateTimeResolved = utcNow.AddDays(2).ToString();
			AddParasTo(ann.Discussion, "This is my", "discussion");
			AddParasTo(ann.Resolution, "This is my", "resolution for", "the note");
			AddParasTo(ann.Quote, "This is the", "quoted text");
			AddParasTo(ann.Suggestion, "This is", "my", "suggestion");
			AddHyperTo(ann.Suggestion[1], "http://www.tim.david.com/cooldudes.html");

			DummyXmlScrAnnotationsList list = new DummyXmlScrAnnotationsList();
			list.Annotations.Add(ann);

			list.CallWriteToCache(Cache, m_stylesheet);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[0];
			Assert.AreEqual(1, annotations.NotesOS.Count);

			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual(NoteType.Translator, note.AnnotationType);
			Assert.IsTrue(AreDateTimesClose(now, note.DateCreated));
			Assert.IsTrue(AreDateTimesClose(now.AddDays(1), note.DateModified));
			Assert.IsTrue(AreDateTimesClose(now.AddDays(2), note.DateResolved));

			TestAnnotationField(note.QuoteOA, "This is the", "quoted text");
			TestAnnotationField(note.DiscussionOA, "This is my", "discussion");
			TestAnnotationField(note.ResolutionOA, "This is my", "resolution for", "the note");
			TestAnnotationField(note.RecommendationOA, "This is", "mymy link", "suggestion");

			// Check the hyperlink location
			ITsTextProps props = ((IStTxtPara)note.RecommendationOA.ParagraphsOS[1]).Contents.UnderlyingTsString.get_Properties(1);
			string href = StringUtils.GetURL(props.GetStrPropValue((int)FwTextPropType.ktptObjData));
			Assert.AreEqual("http://www.tim.david.com/cooldudes.html", href);

			Assert.AreEqual(0, note.ResponsesOS.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving annotations to the cache when the annotation contains a number of
		/// categories
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SavingDeserializedAnnotationsToCache_WithCategories()
		{
			DateTime now = DateTime.Now;
			DateTime utcNow = now.ToUniversalTime();

			XmlScrNote ann = CreateNote();
			ann.BeginScrRef = "LEV 2:8";
			ann.ResolutionStatus = NoteStatus.Open;
			ann.AnnotationTypeGuid = LangProject.kguidAnnTranslatorNote.ToString();
			ann.DateTimeCreated = utcNow.ToString();
			ann.DateTimeModified = utcNow.AddDays(1).ToString();
			AddParasTo(ann.Discussion, "This is my discussion");
			AddParasTo(ann.Resolution, "This is my resolution for the note");
			AddParasTo(ann.Quote, "This is the quoted text");
			AddParasTo(ann.Suggestion, "This is my suggestion");
			XmlNoteCategory category1 = new XmlNoteCategory();
			category1.CategoryName = "Monkey";
			category1.IcuLocale = "en";
			ann.Categories.Add(category1);
			XmlNoteCategory category2 = new XmlNoteCategory();
			category2.CategoryName = "Discourse";
			category2.IcuLocale = "en";
			ann.Categories.Add(category2);

			DummyXmlScrAnnotationsList list = new DummyXmlScrAnnotationsList();
			list.Annotations.Add(ann);

			// Make sure 'Monkey' is not in the DB yet
			foreach (ICmPossibility poss in m_scr.NoteCategoriesOA.PossibilitiesOS)
				Assert.AreNotEqual("Monkey", poss.Name.GetAlternative(InMemoryFdoCache.s_wsHvos.En));

			list.CallWriteToCache(Cache, m_stylesheet);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[2];
			Assert.AreEqual(1, annotations.NotesOS.Count);

			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual(NoteType.Translator, note.AnnotationType);
			Assert.IsTrue(AreDateTimesClose(now, note.DateCreated));
			Assert.IsTrue(AreDateTimesClose(now.AddDays(1), note.DateModified));
			Assert.AreEqual(DateTime.MinValue, note.DateResolved);

			TestAnnotationField(note.QuoteOA, "This is the quoted text");
			TestAnnotationField(note.DiscussionOA, "This is my discussion");
			TestAnnotationField(note.ResolutionOA, "This is my resolution for the note");
			TestAnnotationField(note.RecommendationOA, "This is my suggestion");

			Assert.AreEqual(0, note.ResponsesOS.Count);

			bool foundMonkey = false;
			foreach (ICmPossibility poss in m_scr.NoteCategoriesOA.PossibilitiesOS)
				foundMonkey |= (poss.Name.GetAlternative(InMemoryFdoCache.s_wsHvos.En) == "Monkey");
			Assert.IsTrue(foundMonkey, "Monkey should have been added to the DB");

			Assert.AreEqual(2, note.CategoriesRS.Count);
			Assert.AreEqual("Monkey",
				note.CategoriesRS[0].Name.GetAlternative(InMemoryFdoCache.s_wsHvos.En));
			Assert.AreEqual("Discourse",
				note.CategoriesRS[1].Name.GetAlternative(InMemoryFdoCache.s_wsHvos.En));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving annotations to the cache when the annotation contains responses
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SavingDeserializedAnnotationsToCache_WithResponses()
		{
			DateTime now = DateTime.Now;
			DateTime utcNow = now.ToUniversalTime();

			XmlScrNote ann = CreateNote();
			ann.BeginScrRef = "EXO 2:8";
			ann.ResolutionStatus = NoteStatus.Open;
			ann.AnnotationTypeGuid = LangProject.kguidAnnTranslatorNote.ToString();
			ann.DateTimeCreated = utcNow.ToString();
			ann.DateTimeModified = utcNow.AddDays(1).ToString();
			AddParasTo(ann.Discussion, "This is my discussion");
			AddParasTo(ann.Resolution, "This is my resolution for the note");
			AddParasTo(ann.Quote, "This is the quoted text");
			AddParasTo(ann.Suggestion, "This is my suggestion");
			XmlNoteResponse firstResponse = new XmlNoteResponse();
			AddParasTo(firstResponse.Paragraphs, "This is", "my", "first", "response");
			ann.Responses.Add(firstResponse);
			XmlNoteResponse secondResponse = new XmlNoteResponse();
			AddParasTo(secondResponse.Paragraphs, "This is", "my second response");
			ann.Responses.Add(secondResponse);

			DummyXmlScrAnnotationsList list = new DummyXmlScrAnnotationsList();
			list.Annotations.Add(ann);

			list.CallWriteToCache(Cache, m_stylesheet);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[1];
			Assert.AreEqual(1, annotations.NotesOS.Count);

			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual(NoteType.Translator, note.AnnotationType);
			Assert.IsTrue(AreDateTimesClose(now, note.DateCreated));
			Assert.IsTrue(AreDateTimesClose(now.AddDays(1), note.DateModified));
			Assert.AreEqual(DateTime.MinValue, note.DateResolved);

			TestAnnotationField(note.QuoteOA, "This is the quoted text");
			TestAnnotationField(note.DiscussionOA, "This is my discussion");
			TestAnnotationField(note.ResolutionOA, "This is my resolution for the note");
			TestAnnotationField(note.RecommendationOA, "This is my suggestion");

			FdoOwningSequence<IStJournalText> responses = note.ResponsesOS;
			Assert.AreEqual(2, responses.Count);
			TestAnnotationField(responses[0], "This is", "my", "first", "response");
			TestAnnotationField(responses[1], "This is", "my second response");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving annotations to the cache when an annotation has an invalid date for the
		/// resolved date. In this case it should default to using the last modified date.
		/// (TE-8594)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SavingDeserializedAnnotationsToCache_InvalidResolvedDate()
		{
			DateTime now = DateTime.Now;
			DateTime utcNow = now.ToUniversalTime();

			XmlScrNote ann = CreateNote();
			ann.BeginScrRef = "GEN 2:8";
			ann.ResolutionStatus = NoteStatus.Closed;
			ann.AnnotationTypeGuid = LangProject.kguidAnnTranslatorNote.ToString();
			ann.DateTimeCreated = utcNow.ToString();
			ann.DateTimeModified = utcNow.AddDays(1).ToString();
			ann.DateTimeResolved = "0003-01-01 20:00:00.00";
			AddParasTo(ann.Discussion, "This is my discussion");
			AddParasTo(ann.Resolution, "This is my resolution for the note");
			AddParasTo(ann.Quote, "This is the quoted text");
			AddParasTo(ann.Suggestion, "This is my suggestion");

			DummyXmlScrAnnotationsList list = new DummyXmlScrAnnotationsList();
			list.Annotations.Add(ann);

			list.CallWriteToCache(Cache, m_stylesheet);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[0];
			Assert.AreEqual(1, annotations.NotesOS.Count);

			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual(NoteType.Translator, note.AnnotationType);
			Assert.IsTrue(AreDateTimesClose(now, note.DateCreated));
			Assert.IsTrue(AreDateTimesClose(now.AddDays(1), note.DateModified));
			Assert.IsTrue(AreDateTimesClose(note.DateModified, note.DateResolved));

			TestAnnotationField(note.QuoteOA, "This is the quoted text");
			TestAnnotationField(note.DiscussionOA, "This is my discussion");
			TestAnnotationField(note.ResolutionOA, "This is my resolution for the note");
			TestAnnotationField(note.RecommendationOA, "This is my suggestion");

			Assert.AreEqual(0, note.ResponsesOS.Count);
		}


		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving annotations to the cache when an annotation has an invalid date for the
		/// resolved date and the modified date is also invalid. In this case it should
		/// default to using the minimum file date. (TE-8594)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SavingDeserializedAnnotationsToCache_InvalidResolvedAndModifiedDate()
		{
			DateTime now = DateTime.Now;
			DateTime utcNow = now.ToUniversalTime();

			XmlScrNote ann = CreateNote();
			ann.BeginScrRef = "GEN 2:8";
			ann.ResolutionStatus = NoteStatus.Closed;
			ann.AnnotationTypeGuid = LangProject.kguidAnnTranslatorNote.ToString();
			ann.DateTimeCreated = utcNow.ToString();
			ann.DateTimeModified = "0009-05-02 20:00:00.00";
			ann.DateTimeResolved = "0003-01-01 20:00:00.00";
			AddParasTo(ann.Discussion, "This is my discussion");
			AddParasTo(ann.Resolution, "This is my resolution for the note");
			AddParasTo(ann.Quote, "This is the quoted text");
			AddParasTo(ann.Suggestion, "This is my suggestion");

			DummyXmlScrAnnotationsList list = new DummyXmlScrAnnotationsList();
			list.Annotations.Add(ann);

			list.CallWriteToCache(Cache, m_stylesheet);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[0];
			Assert.AreEqual(1, annotations.NotesOS.Count);

			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual(NoteType.Translator, note.AnnotationType);
			Assert.IsTrue(AreDateTimesClose(now, note.DateCreated));
			Assert.IsTrue(AreDateTimesClose(DateTime.FromFileTime(0), note.DateModified));
			Assert.IsTrue(AreDateTimesClose(DateTime.FromFileTime(0), note.DateModified));

			TestAnnotationField(note.QuoteOA, "This is the quoted text");
			TestAnnotationField(note.DiscussionOA, "This is my discussion");
			TestAnnotationField(note.ResolutionOA, "This is my resolution for the note");
			TestAnnotationField(note.RecommendationOA, "This is my suggestion");

			Assert.AreEqual(0, note.ResponsesOS.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving annotations to the cache when the annotation contains responses. This
		/// makes sure annotation responses are not added if they already exist. (TE-8271)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ExistingAnnotation_WithResponses()
		{
			IScrScriptureNote existingAnn =
				m_scrInMemoryCache.AddAnnotation(null, 02002008, NoteType.Translator, "This is my discussion");
			existingAnn.ResolutionStatus = NoteStatus.Open;

			IStJournalText exisingResponse1 = existingAnn.ResponsesOS.Append(new StJournalText());
			AddParasTo(exisingResponse1, "This is my first response");

			IStJournalText exisingResponse2 = existingAnn.ResponsesOS.Append(new StJournalText());
			AddParasTo(exisingResponse2, "This is my second response");

			DateTime now = DateTime.Now;
			DateTime utcNow = now.ToUniversalTime();

			XmlScrNote ann = CreateNote();
			ann.BeginScrRef = "EXO 2:8";
			ann.ResolutionStatus = NoteStatus.Open;
			ann.AnnotationTypeGuid = LangProject.kguidAnnTranslatorNote.ToString();
			ann.DateTimeCreated = utcNow.ToString();
			ann.DateTimeModified = utcNow.AddDays(1).ToString();
			AddParasTo(ann.Discussion, "This is my discussion");
			XmlNoteResponse firstResponse = new XmlNoteResponse();
			AddParasTo(firstResponse.Paragraphs, "This is my first response");
			ann.Responses.Add(firstResponse);
			XmlNoteResponse secondResponse = new XmlNoteResponse();
			AddParasTo(secondResponse.Paragraphs, "This is my second response");
			ann.Responses.Add(secondResponse);

			DummyXmlScrAnnotationsList list = new DummyXmlScrAnnotationsList();
			list.Annotations.Add(ann);

			list.CallWriteToCache(Cache, m_stylesheet);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[1];
			Assert.AreEqual(1, annotations.NotesOS.Count);

			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual(NoteType.Translator, note.AnnotationType);
			Assert.IsTrue(AreDateTimesClose(now, note.DateCreated));
			Assert.IsTrue(AreDateTimesClose(now.AddDays(1), note.DateModified));
			Assert.AreEqual(DateTime.MinValue, note.DateResolved);

			FdoOwningSequence<IStJournalText> responses = note.ResponsesOS;
			Assert.AreEqual(2, responses.Count);
			TestAnnotationField(responses[0], "This is my first response");
			TestAnnotationField(responses[1], "This is my second response");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving annotations to the cache when the annotation contains responses. This
		/// makes sure annotation responses are correctly added if some exist but there are some
		/// new responses as well. (TE-8271)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ExistingAnnotation_WithNewResponse()
		{
			IScrScriptureNote existingAnn =
				m_scrInMemoryCache.AddAnnotation(null, 02002008, NoteType.Translator, "This is my discussion");
			existingAnn.ResolutionStatus = NoteStatus.Open;

			IStJournalText exisingResponse1 = existingAnn.ResponsesOS.Append(new StJournalText());
			AddParasTo(exisingResponse1, "This is my first response");

			IStJournalText exisingResponse2 = existingAnn.ResponsesOS.Append(new StJournalText());
			AddParasTo(exisingResponse2, "This is my second response");

			DateTime now = DateTime.Now;
			DateTime utcNow = now.ToUniversalTime();

			XmlScrNote ann = CreateNote();
			ann.BeginScrRef = "EXO 2:8";
			ann.ResolutionStatus = NoteStatus.Open;
			ann.AnnotationTypeGuid = LangProject.kguidAnnTranslatorNote.ToString();
			ann.DateTimeCreated = utcNow.ToString();
			ann.DateTimeModified = utcNow.AddDays(1).ToString();
			AddParasTo(ann.Discussion, "This is my discussion");
			XmlNoteResponse firstResponse = new XmlNoteResponse();
			AddParasTo(firstResponse.Paragraphs, "This is my first response");
			ann.Responses.Add(firstResponse);
			XmlNoteResponse secondResponse = new XmlNoteResponse();
			AddParasTo(secondResponse.Paragraphs, "This is a new response");
			ann.Responses.Add(secondResponse);
			XmlNoteResponse thirdResponse = new XmlNoteResponse();
			AddParasTo(thirdResponse.Paragraphs, "This is my second response");
			ann.Responses.Add(thirdResponse);

			DummyXmlScrAnnotationsList list = new DummyXmlScrAnnotationsList();
			list.Annotations.Add(ann);

			list.CallWriteToCache(Cache, m_stylesheet);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[1];
			Assert.AreEqual(1, annotations.NotesOS.Count);

			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual(NoteType.Translator, note.AnnotationType);
			Assert.IsTrue(AreDateTimesClose(now, note.DateCreated));
			Assert.IsTrue(AreDateTimesClose(now.AddDays(1), note.DateModified));
			Assert.AreEqual(DateTime.MinValue, note.DateResolved);

			FdoOwningSequence<IStJournalText> responses = note.ResponsesOS;
			Assert.AreEqual(3, responses.Count);
			TestAnnotationField(responses[0], "This is my first response");
			TestAnnotationField(responses[1], "This is my second response");
			TestAnnotationField(responses[2], "This is a new response");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the specified DateTimes close to each other by checking only
		/// down to the seconds.
		/// </summary>
		/// <param name="first">The first.</param>
		/// <param name="second">The second.</param>
		/// <returns>True if the specified DateTimes are close to each other, false otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool AreDateTimesClose(DateTime first, DateTime second)
		{
			return first.ToString("yyMMddHHmmss") == second.ToString("yyMMddHHmmss");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the annotation field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void TestAnnotationField(IStJournalText jtext, params string[] expected)
		{
			Assert.AreEqual(expected.Length, jtext.ParagraphsOS.Count);
			for (int i = 0; i < expected.Length; i++)
			{
				Assert.IsNotNull(jtext.ParagraphsOS[i].StyleRules);
				Assert.AreEqual(ScrStyleNames.Remark,
					jtext.ParagraphsOS[i].StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				Assert.AreEqual(expected[i],
					((IStTxtPara)jtext.ParagraphsOS[i]).Contents.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified text paragraphs to the specified IStJournalText with a writing
		/// system of the default analysis language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddParasTo(IStJournalText stjt, params string[] paras)
		{
			foreach (string paraText in paras)
			{
				StTxtPara para = (StTxtPara)stjt.ParagraphsOS.Append(new StTxtPara());
				para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.Remark);
				ITsStrFactory fact = TsStrFactoryClass.Create();
				para.Contents.UnderlyingTsString = fact.MakeString(paraText, Cache.DefaultAnalWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified text to a new paragraph (where each string is the text of a new
		/// paragraph)
		/// </summary>
		/// <param name="paraList">The list of paragraphs to add to</param>
		/// <param name="paras">The text of the paragraphs to add</param>
		/// ------------------------------------------------------------------------------------
		private void AddParasTo(List<XmlNotePara> paraList, params string[] paras)
		{
			foreach (string paraText in paras)
			{
				XmlNotePara newPara = new XmlNotePara();
				XmlTextRun run = new XmlTextRun();
				run.Text = paraText;
				newPara.Runs.Add(run);
				paraList.Add(newPara);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified text as a new hyperlink run with the text 'my link' to the
		/// specified paragraph
		/// </summary>
		/// <param name="para">The paragraph to add to</param>
		/// <param name="href">The href.</param>
		/// ------------------------------------------------------------------------------------
		private void AddHyperTo(XmlNotePara para, string href)
		{
			XmlHyperlinkRun run = new XmlHyperlinkRun();
			run.Text = "my link";
			run.Href = href;
			para.Runs.Add(run);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new XmlScrNote and initializes some fields that the deserializer would
		/// initialize.
		/// </summary>
		/// <returns>The new XmlScrNote</returns>
		/// ------------------------------------------------------------------------------------
		private XmlScrNote CreateNote()
		{
			XmlScrNote ann = new XmlScrNote();
			ann.Categories = new List<XmlNoteCategory>();
			ann.Discussion = new List<XmlNotePara>();
			ann.Resolution = new List<XmlNotePara>();
			ann.Suggestion = new List<XmlNotePara>();
			ann.Responses = new List<XmlNoteResponse>();
			ann.Quote = new List<XmlNotePara>();
			return ann;
		}
	}
}
