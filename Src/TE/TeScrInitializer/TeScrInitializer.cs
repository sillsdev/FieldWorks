// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeScrInitializer.cs
// Responsibility: TE Team
//
// <remarks>

// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class providing static method that TE calls to perform one-time initialization of a new
	/// Scripture project
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeScrInitializer
	{
		#region Member variables
		/// <summary>FdoCache of the language project we are working on</summary>
		protected FdoCache m_cache;
		/// <summary>Scripture of the language project we are working on</summary>
		protected IScripture m_scr;
		/// <summary>Fixed GUID used to identify the CmResource used to indicate that orphaned
		/// footnotes have already been fixed in a FW project</summary>
		public static readonly Guid kguidFixedOrphanedFootnotes = new Guid("35E2F9E2-AF55-48c4-A8A1-4C2722386C85");
		/// <summary>Name used to identify the CmResource used to indicate that orphaned
		/// footnotes have already been fixed in a FW project</summary>
		public static string ksFixedOrphanedFootnotes = "FixedOrphanedFootnotes";

		#endregion

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected TeScrInitializer(FdoCache cache)
		{
			m_cache = cache;
			m_scr = cache.LangProject.TranslatedScriptureOA;
		}

		#region Public static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform one-time initialization of a new Scripture project
		/// </summary>
		/// <param name="cache">The database cache</param>
		/// <param name="splashScreen">The splash screen (can be null).</param>
		/// <returns>
		/// true if data loaded successfully; false, otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool Initialize(FdoCache cache, IFwSplashScreen splashScreen)
		{
			TeScrInitializer scrInitializer = new TeScrInitializer(cache);
			return scrInitializer.Initialize(splashScreen);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs any needed tasks to ensure that the project is valid and has any objects
		/// required by this version of the application.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="existingProgressDlg">The existing progress dialog, if any.</param>
		/// ------------------------------------------------------------------------------------
		public static void EnsureProjectValid(FdoCache cache, IAdvInd4 existingProgressDlg)
		{
			TeScrInitializer scrInitializer = new TeScrInitializer(cache);
			scrInitializer.RemoveRtlMarksFromScrProperties();
			scrInitializer.EnsureScriptureTextsValid();
			List<string> issuesToReport = scrInitializer.FixOrcsWithoutProps();
			if (issuesToReport != null)
			{
				using (FixedOrphanFootnoteReportDlg dlg = new FixedOrphanFootnoteReportDlg(
					issuesToReport, cache.ProjectName(), FwApp.App))
				{
					dlg.ShowDialog();
				}
			}
		}

		private class OrcLocation
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="OrcLocation"/> class.
			/// </summary>
			/// <param name="para">The paragraph.</param>
			/// <param name="ich">The indexc of the ORC character.</param>
			/// <param name="startRef">The starting Scripture reference at the location of the
			/// ORC.</param>
			/// <param name="endRef">The ending Scripture reference at the location of the
			/// ORC (same as startRef except in the case of a verse bridge or ORC in a section
			/// head).</param>
			/// <param name="moreInfo">More information about the location (i.e., context) of
			/// the paragraph containing the ORC.</param>
			/// --------------------------------------------------------------------------------
			internal OrcLocation(IStTxtPara para, int ich, BCVRef startRef, BCVRef endRef,
				string moreInfo)
			{
				this.para = para;
				this.ich = ich;
				m_startRef = new BCVRef(startRef);
				m_endRef = new BCVRef(endRef);
				m_moreInfo = moreInfo;
			}

			/// <summary>Paragraph containing a bogus ORC</summary>
			internal readonly IStTxtPara para;
			/// <summary>Offset to the character position of the ORC</summary>
			internal int ich;
			private  readonly BCVRef m_startRef;
			private readonly BCVRef m_endRef;
			private readonly string m_moreInfo;

			/// <summary>Footnote object to which this ORC is hooked up, if a footnote;
			/// otherwise just the class ID of the object</summary>
			internal object Object = null;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Returns a string representation of the ORC location.
			/// </summary>
			/// <param name="scr">The SCR.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			internal object ToString(IScripture scr)
			{
				return BCVRef.MakeReferenceString(m_startRef, m_endRef, scr.ChapterVerseSepr, scr.Bridge, "Title", "Intro") +
					(m_moreInfo == null ? String.Empty : " " + m_moreInfo);
			}
		}

		private class FootnoteOrcLocation
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="FootnoteOrcLocation"/> class.
			/// </summary>
			/// <param name="footnote">The footnote.</param>
			/// --------------------------------------------------------------------------------
			internal FootnoteOrcLocation(IStFootnote footnote)
			{
				this.footnote = footnote;
			}

			/// <summary>Footnote</summary>
			internal readonly IStFootnote footnote;
			/// <summary>Location of the corresponding ORC character, if any</summary>
			internal OrcLocation location;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixes any orphaned footnotes or pictures caused by TE-8674/TE-8719.
		/// </summary>
		/// <returns>List of descriptions of any problems fix or any remaining problems that the
		/// user might still need to fix</returns>
		/// ------------------------------------------------------------------------------------
		private List<string> FixOrcsWithoutProps()
		{
			// If we've already checked this project out and fixed any orphans, we're done.
			foreach (ICmResource res in m_scr.ResourcesOC)
			{
				if (res.Name == ksFixedOrphanedFootnotes && res.Version == kguidFixedOrphanedFootnotes)
					return null;
			}

			List<string> issuesToReport = null;

			// Do the cleanup.
			foreach (IScrBook book in m_scr.ScriptureBooksOS)
			{
				List<FootnoteOrcLocation> footnotes = new List<FootnoteOrcLocation>(book.FootnotesOS.Count);
				foreach (IStFootnote footnote in book.FootnotesOS)
					footnotes.Add(new FootnoteOrcLocation(footnote));

				List<OrcLocation> parasWithOrcs = new List<OrcLocation>(book.FootnotesOS.Count);

				BCVRef startRef = new BCVRef(book.CanonicalNum, 0, 0);
				BCVRef endRef = new BCVRef(startRef);

				bool foundProblem = FindOrcsWithoutPropsInText(book.TitleOA, null, ref startRef, ref endRef, footnotes, parasWithOrcs);

				int sectionNumber = 1;
				foreach (IScrSection section in book.SectionsOS)
				{
					startRef = new BCVRef(section.VerseRefStart);
					endRef = new BCVRef(section.VerseRefEnd);
					string sLocationFmt = (section.IsIntro) ? "Section {1}, {0}" : "Section {0}";
					string sLocation = String.Format(sLocationFmt, "Heading", sectionNumber);

					foundProblem |= FindOrcsWithoutPropsInText(section.HeadingOA, sLocation, ref startRef, ref endRef, footnotes, parasWithOrcs);
					sLocation = (section.IsIntro) ? String.Format(sLocationFmt, "Contents", sectionNumber) : null;

					foundProblem |= FindOrcsWithoutPropsInText(section.ContentOA, sLocation, ref startRef, ref endRef, footnotes, parasWithOrcs);
					sectionNumber++;
				}

				if (!foundProblem)
				{
					// Might still have orphaned footnotes, so look through list to check
					foreach (FootnoteOrcLocation fn in footnotes)
					{
						if (fn.location == null)
						{
							foundProblem = true;
							break;
						}
					}
				}

				if (foundProblem)
				{
					if (issuesToReport == null)
						issuesToReport = new List<string>();
					FixOrcsWithoutProps(book, parasWithOrcs, footnotes, issuesToReport);
				}
			}

			// Make an entry in the "Resources" so that we know this project has been fixed up.
			ICmResource resourceFixed = new CmResource();
			m_scr.ResourcesOC.Add(resourceFixed);
			resourceFixed.Name = ksFixedOrphanedFootnotes;
			resourceFixed.Version = kguidFixedOrphanedFootnotes;
			return issuesToReport;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures all of the project components are valid.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="existingProgressDlg">The existing progress dialog.</param>
		/// ------------------------------------------------------------------------------------
		public static void EnsureProjectComponentsValid(FdoCache cache, IAdvInd4 existingProgressDlg)
		{
			EnsureProjectValid(cache, existingProgressDlg);

			// TE-8621: Don't try to upgrade the database unless we're the project server and no one is connected
			if (!MiscUtils.IsServerLocal(cache.ServerName) || cache.GetNumberOfRemoteClients() > 0)
				return;

			ILangProject lp = cache.LangProject;

			TePublicationsInit.EnsureFactoryPublications(lp, existingProgressDlg);
			TeStylesXmlAccessor.EnsureCurrentStylesheet(lp, existingProgressDlg);
			TeScrNoteCategoriesInit.EnsureCurrentScrNoteCategories(lp, existingProgressDlg);
			TeKeyTermsInit.EnsureCurrentKeyTerms(lp, existingProgressDlg);
			cache.Save();
		}
		#endregion

		#region Initialization
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Perform one-time initialization of a new Scripture project
		/// </summary>
		/// <returns>true if data loaded successfully; false, otherwise</returns>
		/// -------------------------------------------------------------------------------------
		protected bool Initialize(IFwSplashScreen splashScreen)
		{
			ILangProject lp = m_cache.LangProject;
			if (m_scr != null)
			{
				// Preload all book, section and paragraphs if we already have Scripture
				PreloadData(m_cache, splashScreen);

				if (m_scr.BookAnnotationsOS.Count != 0 &&
					m_cache.ScriptureReferenceSystem.BooksOS.Count != 0 && m_scr.PublicationsOC.Count != 0 &&
					lp.KeyTermsList.PossibilitiesOS.Count >= 1 &&
					m_scr.NoteCategoriesOA != null && m_scr.NoteCategoriesOA.PossibilitiesOS.Count > 0)
				{
					return true;
				}
			}

			IAdvInd4 existingProgressDlg = null;
			if (splashScreen != null)
				existingProgressDlg = splashScreen.ProgressBar as IAdvInd4;

			using (ProgressDialogWithTask dlg = new ProgressDialogWithTask(Form.ActiveForm))
			{
				try
				{
					dlg.RunTask(existingProgressDlg, true, new BackgroundTaskInvoker(InitializeScriptureProject));
				}
				catch (WorkerThreadException e)
				{
					UndoResult ures;
					while (m_cache.Undo(out ures)) ; // Enhance JohnT: make use of ures?
					MessageBox.Show(Form.ActiveForm, e.InnerException.Message,
						TeResourceHelper.GetResourceString("kstidApplicationName"),
						MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Preloads the data used by TE.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="splashScreen">The splash screen (can be null).</param>
		/// ------------------------------------------------------------------------------------
		public static void PreloadData(FdoCache cache, IFwSplashScreen splashScreen)
		{
			if (splashScreen != null)
			{
				splashScreen.ProgressBar.SetRange(0, 21); // we call 21 x UpdateProgress() below
				splashScreen.ProgressBar.StepSize = 1;
				splashScreen.ProgressBar.Position = 0;
			}

			// Preload all vectors for scripture
			// NOTE: splitting up the loading of flids off of the same object type (e.g. StText)
			// lets us create a smoother progress bar and doesn't seem to produce any noticable
			// performance hit!
			cache.PreloadIfMissing(cache.LangProject.TranslatedScriptureOAHvo,
				(int)Scripture.ScriptureTags.kflidScriptureBooks, 0);
			UpdateProgress(splashScreen);
			cache.LoadAllOfAnOwningVectorProp((int)ScrBook.ScrBookTags.kflidSections, "ScrBook");
			UpdateProgress(splashScreen);
			cache.LoadAllOfAnOwningVectorProp((int)Scripture.ScriptureTags.kflidStyles, "Scripture");
			UpdateProgress(splashScreen);
			cache.LoadAllOfAnOwningAtomicProp((int)ScrSection.ScrSectionTags.kflidHeading, "ScrSection");
			UpdateProgress(splashScreen);
			cache.LoadAllOfAnOwningAtomicProp((int)ScrSection.ScrSectionTags.kflidContent, "ScrSection");
			UpdateProgress(splashScreen);
			cache.LoadAllOfAnOwningVectorProp((int)StText.StTextTags.kflidParagraphs, "StText",
				new int[] { (int)ScrBook.ScrBookTags.kflidFootnotes });
			UpdateProgress(splashScreen);
			cache.LoadAllOfAnOwningVectorProp((int)StText.StTextTags.kflidParagraphs, "StText",
				new int[] { (int)ScrBook.ScrBookTags.kflidTitle });
			UpdateProgress(splashScreen);
			cache.LoadAllOfAnOwningVectorProp((int)StText.StTextTags.kflidParagraphs, "StText",
				new int[] { (int)ScrSection.ScrSectionTags.kflidHeading });
			UpdateProgress(splashScreen);
			cache.LoadAllOfAnOwningVectorProp((int)StText.StTextTags.kflidParagraphs, "StText",
				new int[] { (int)ScrSection.ScrSectionTags.kflidContent });
			UpdateProgress(splashScreen);
			cache.LoadAllOfAnOwningVectorProp((int)StTxtPara.StTxtParaTags.kflidTranslations, "StTxtPara");
			UpdateProgress(splashScreen);

			// also preload all scripture, sections, paragraphs and footnotes
			CmObject.LoadDataForFlids(typeof(Scripture), cache, null,
				LangProject.FullViewName);
			UpdateProgress(splashScreen);
			CmObject.LoadDataForFlids(typeof(StStyle), cache,
				new int[] { (int)LangProject.LangProjectTags.kflidTranslatedScripture },
				Scripture.FullViewName);
			UpdateProgress(splashScreen);
			CmObject.LoadDataForFlids(typeof(ScrSection), cache,
				new int[] { (int)Scripture.ScriptureTags.kflidScriptureBooks },
				ScrBook.FullViewName);
			UpdateProgress(splashScreen);
			CmObject.LoadDataForFlids(typeof(StFootnote), cache,
				new int[] { (int)Scripture.ScriptureTags.kflidScriptureBooks },
				ScrBook.FullViewName);
			UpdateProgress(splashScreen);
			CmObject.LoadDataForFlids(typeof(StTxtPara), cache,
				new int[] { (int)ScrBook.ScrBookTags.kflidFootnotes }, StText.FullViewName);
			UpdateProgress(splashScreen);
			CmObject.LoadDataForFlids(typeof(StTxtPara), cache,
				new int[] { (int)ScrBook.ScrBookTags.kflidTitle }, StText.FullViewName);
			UpdateProgress(splashScreen);
			CmObject.LoadDataForFlids(typeof(StTxtPara), cache,
				new int[] { (int)ScrSection.ScrSectionTags.kflidHeading }, StText.FullViewName);
			UpdateProgress(splashScreen);
			CmObject.LoadDataForFlids(typeof(StTxtPara), cache,
				new int[] { (int)ScrSection.ScrSectionTags.kflidContent }, StText.FullViewName);
			UpdateProgress(splashScreen);
			CmObject.LoadDataForFlids(typeof(CmTranslation), cache,
				new int[] { (int)CmTranslation.CmTranslationTags.kflidStatus }, CmTranslation.FullViewName);
			UpdateProgress(splashScreen);
			CmObject.LoadDataForFlids(typeof(CmTranslation), cache,
				new int[] { (int)CmTranslation.CmTranslationTags.kflidTranslation }, CmTranslation.FullViewName);
			UpdateProgress(splashScreen);
			CmObject.LoadDataForFlids(typeof(CmTranslation), cache,
				new int[] { (int)CmTranslation.CmTranslationTags.kflidType }, CmTranslation.FullViewName);
			UpdateProgress(splashScreen);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the progress on the splash screen's progress bar.
		/// </summary>
		/// <param name="splashScreen">The splash screen.</param>
		/// ------------------------------------------------------------------------------------
		private static void UpdateProgress(IFwSplashScreen splashScreen)
		{
			if (splashScreen != null)
				splashScreen.ProgressBar.Step(1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the scripture project.
		/// </summary>
		/// <param name="progressDialog">The progress dialog.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private object InitializeScriptureProject(IAdvInd4 progressDialog, params object[] parameters)
		{
			ILangProject lp = m_cache.LangProject;

			if (m_scr == null)
			{
				m_scr = lp.TranslatedScriptureOA = new Scripture();

				//Initialize factory styles
				TeStylesXmlAccessor.CreateFactoryScrStyles(progressDialog, m_scr);

				// Initialize the separator and bridge strings.
				m_scr.ChapterVerseSepr = ":";
				m_scr.Bridge = "-";
				m_scr.RefSepr = ";";
				m_scr.VerseSepr = ",";

				//Initialize misc
				m_scr.RestartFootnoteSequence = true;
				m_scr.CrossRefsCombinedWithFootnotes = false;

				m_scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;
				m_scr.FootnoteMarkerSymbol = Scripture.kDefaultFootnoteMarkerSymbol;
				m_scr.DisplayFootnoteReference = false;

				m_scr.CrossRefMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
				m_scr.CrossRefMarkerSymbol = Scripture.kDefaultFootnoteMarkerSymbol;
				m_scr.DisplayCrossRefReference = true;
			}

			// Initialize Scripture book annotations
			if (m_scr.BookAnnotationsOS.Count == 0)
				CreateScrBookAnnotations();

			//Initialize Scripture Book Ref info
			if (m_cache.ScriptureReferenceSystem.BooksOS.Count == 0)
				CreateScrBookRefs(progressDialog);

			//Initialize factory publications
			if (m_scr.PublicationsOC.Count == 0)
				TePublicationsInit.CreatePublicationInfo(progressDialog, m_scr);

			//Initialize the key terms
			ICmPossibilityList keyTermsList = lp.KeyTermsList;
			if (keyTermsList.PossibilitiesOS.Count < 1)
				TeKeyTermsInit.CreateKeyTerms(progressDialog, keyTermsList);

			//Initialize the note categories
			if (m_scr.NoteCategoriesOA == null || m_scr.NoteCategoriesOA.PossibilitiesOS.Count == 0)
				TeScrNoteCategoriesInit.CreateFactoryScrNoteCategories(progressDialog, m_scr);

			m_cache.Save();

			// For good measure, on the off-chance the user notices.
			int nMin, nMax;
			progressDialog.GetRange(out nMin, out nMax);
			progressDialog.Position = nMax;

			return null;
		}
		#endregion

		#region Create ScrBookRefs
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Create all of the ScrBookRef objects for each book of Scripture
		/// </summary>
		/// <param name="progressDlg">Progress dialog so the user can cancel</param>
		/// -------------------------------------------------------------------------------------
		protected void CreateScrBookRefs(IAdvInd4 progressDlg)
		{
			IScrRefSystem scr = m_cache.ScriptureReferenceSystem;

			// If there are books existing, then delete them first.
			for (int i = scr.BooksOS.Count - 1; i >= 0; i--)
				scr.BooksOS.RemoveAt(i);

			XmlDocument doc = new XmlDocument();
			doc.Load(DirectoryFinder.FWCodeDirectory + @"\Translation Editor\ScrBookRef.xml");
			ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;

			//Select and display the value of all the ISBN attributes.
			XmlNodeList tagList = doc.SelectNodes("/ScrBookRef/writingsystem");
			progressDlg.SetRange(0, tagList.Count * ScrReference.LastBook);
			progressDlg.Position = 0;
			progressDlg.Title = TeResourceHelper.GetResourceString("kstidCreatingBookNames");

			foreach (XmlNode writingSystem in tagList)
			{
				XmlAttributeCollection attributes = writingSystem.Attributes;
				string sLocale = attributes.GetNamedItem("iculocale").Value;
				int ws = m_cache.LanguageEncodings.GetWsFromIcuLocale(sLocale);
				if (ws == 0)
				{
					// It is possible that the XML file contains more languages than the
					// database. If so, just ignore this writing system.
					continue;
				}

				short iBook = 0;
				XmlNodeList WSBooks = writingSystem.SelectNodes("book");
				foreach (XmlNode book in WSBooks)
				{
					XmlAttributeCollection bookAttributes = book.Attributes;
					string sSilBookId = bookAttributes.GetNamedItem("SILBookId").Value;
					Debug.Assert(sSilBookId != null);
					// Make sure books are coming in canonical order.
					Debug.Assert(ScrReference.BookToNumber(sSilBookId) == iBook + 1);

					string sName = bookAttributes.GetNamedItem("Name").Value;
					string sAbbrev = bookAttributes.GetNamedItem("Abbreviation").Value;
					string sAltName = bookAttributes.GetNamedItem("AlternateName").Value;
					progressDlg.Message = string.Format(
						TeResourceHelper.GetResourceString("kstidCreatingBookNamesStatusMsg"), sName);
					progressDlg.Step(0);

					// check for the book id
					ScrBookRef bookRef = null;
					if (scr.BooksOS.Count > iBook)
					{
						bookRef = (ScrBookRef)scr.BooksOS[iBook];
						Debug.Assert(bookRef != null);
					}
					else
					{
						// add this book to the list
						bookRef = new ScrBookRef();
						scr.BooksOS.Append(bookRef);
					}
					if (sName != null)
						bookRef.BookName.SetAlternative(sName, ws);
					if (sAbbrev != null)
						bookRef.BookAbbrev.SetAlternative(sAbbrev, ws);
					if (sAltName != null)
						bookRef.BookNameAlt.SetAlternative(sAltName, ws);
					iBook++;
				}
			}
		}
		#endregion

		#region Create ScrBookAnnotations
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Create all of the ScrBookAnnotations objects for each book of Scripture
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected void CreateScrBookAnnotations()
		{
			Debug.Assert(m_scr.BookAnnotationsOS.Count == 0);

			for (int iBookNum = 0; iBookNum < 66; iBookNum++)
				m_scr.BookAnnotationsOS.Append(new ScrBookAnnotations());
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sanity check to ensure the scripture texts are valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void EnsureScriptureTextsValid()
		{
			foreach (ScrBook book in m_scr.ScriptureBooksOS)
			{
				int[] sectionHvos = book.SectionsOS.HvoArray;
				// Don't crash if we don't have any sections (TE-5380)
				if (sectionHvos.Length == 0)
					continue;

				ContextValues sectionContext;
				foreach (ScrSection section in book.SectionsOS)
				{
					// Check the heading paragraphs.
					if (section.HeadingOA == null)
					{
						m_cache.CreateObject(StText.kClassId, section.Hvo,
							(int)ScrSection.ScrSectionTags.kflidHeading, 0);
					}
					if (section.HeadingOA.ParagraphsOS.Count == 0)
					{
						StTxtPara para = new StTxtPara();
						section.HeadingOA.ParagraphsOS.Append(para);
						para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead);
						sectionContext = ContextValues.Text;
					}
					else
					{
						IStStyle style = m_scr.FindStyle(section.HeadingOA.ParagraphsOS[0].StyleRules);
						// style could be null. set default context if possible
						sectionContext = style == null ? ContextValues.Text : style.Context;
					}

					// Check the content paragraphs.
					if (section.ContentOA == null)
					{
						m_cache.CreateObject(StText.kClassId, section.Hvo,
							(int)ScrSection.ScrSectionTags.kflidContent, 0);
					}
					if (section.ContentOA.ParagraphsOS.Count == 0)
					{
						StTxtPara para = new StTxtPara();
						section.ContentOA.ParagraphsOS.Append(para);
						para.StyleRules = StyleUtils.ParaStyleTextProps(
							TeEditingHelper.GetDefaultStyleForContext(sectionContext, false));
						section.AdjustReferences();
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the RTL marks from Scripture properties. This is needed because in the past
		/// we used to stick the rtl mark on either side of all these properties if the default
		/// vernacular WS was right-to-left. But now we think that was stupid. We're just going
		/// to include them when needed, since most places in code don't.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RemoveRtlMarksFromScrProperties()
		{
			m_scr.RefSepr = m_scr.RefSepr.Replace("\u200f", String.Empty);
			m_scr.ChapterVerseSepr = m_scr.ChapterVerseSepr.Replace("\u200f", String.Empty);
			m_scr.VerseSepr = m_scr.VerseSepr.Replace("\u200f", String.Empty);
			m_scr.Bridge = m_scr.Bridge.Replace("\u200f", String.Empty);
		}

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds all ORCs in the given text and notes any orphaned footnotes or pictures.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="textLocationInfo">Additional information about the location of the
		/// text that can help the user find it.</param>
		/// <param name="startRef">Caller should pass in the initial reference to use as the
		/// basis for any references found in the course of parsing the text. Returned value
		/// will be the final reference found, which can be used as the basis for the subsequent
		/// text</param>
		/// <param name="endRef">Same as startRef, except in the case of verse bridges or
		/// section headings</param>
		/// <param name="footnotes">List of footnotes owned by the book that owns the
		/// given text. As footnotes are found, their locations will be set.</param>
		/// <param name="parasWithOrcs">List of paragraphs and ORC positions.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static bool FindOrcsWithoutPropsInText(IStText text, string textLocationInfo,
			ref BCVRef startRef, ref BCVRef endRef,
			List<FootnoteOrcLocation> footnotes, List<OrcLocation> parasWithOrcs)
		{
			bool foundOrphan = false;
			foreach (IStTxtPara para in text.ParagraphsOS)
			{
				ITsString tssContents = para.Contents.UnderlyingTsString;
				string sContents = tssContents.Text;
				if (sContents == null)
					continue;

				int nRun = tssContents.RunCount;
				for (int i = 0; i < nRun; i++)
				{
					TsRunInfo runInfo;
					ITsTextProps tprops = tssContents.FetchRunInfo(i, out runInfo);
					string styleName = tprops.GetStrPropValue(
						(int)FwTextPropType.ktptNamedStyle);

					// When a verse number is encountered, save the number into
					// the reference.
					if (styleName == ScrStyleNames.VerseNumber)
					{
						string sVerseNum = sContents.Substring(runInfo.ichMin,
							runInfo.ichLim - runInfo.ichMin);
						int nVerseStart, nVerseEnd;
						ScrReference.VerseToInt(sVerseNum, out nVerseStart, out nVerseEnd);
						startRef.Verse = nVerseStart;
						endRef.Verse = nVerseEnd;
					}

					// If a chapter number is encountered then save the number into
					// the reference and start the verse number back at 1.
					else if (styleName == ScrStyleNames.ChapterNumber)
					{
						try
						{
							string sChapterNum = sContents.Substring(runInfo.ichMin,
								runInfo.ichLim - runInfo.ichMin);
							startRef.Chapter = endRef.Chapter = ScrReference.ChapterToInt(sChapterNum);
							startRef.Verse = endRef.Verse = 1;
						}
						catch (ArgumentException)
						{
							// ignore runs with invalid Chapter numbers
						}
					}
					else
					{
						// search contents for ORCs
						for (int ich = runInfo.ichMin; ich < runInfo.ichLim; ich++)
						{
							if (sContents[ich] != StringUtils.kchObject)
								continue;

							OrcLocation orcLocation = new OrcLocation(para, ich, startRef, endRef, textLocationInfo);
							parasWithOrcs.Add(orcLocation);

							ITsTextProps props = tssContents.get_PropertiesAt(ich);
							string objData = props.GetStrPropValue((int)FwTextPropType.ktptObjData);

							if (objData == null)
								foundOrphan = true;
							else
							{
								// first char. of strData is type code - GUID will follow it.
								Guid objGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));

								int hvo = text.Cache.GetIdFromGuid(objGuid);
								int classId = (hvo == 0) ? 0 : text.Cache.GetClassOfObject(hvo);
								if (classId == StFootnote.kClassId)
								{
									foreach (FootnoteOrcLocation footnote in footnotes)
									{
										if (footnote.footnote.Hvo == hvo)
										{
											orcLocation.Object = footnote.footnote;
											footnote.location = orcLocation;
											break;
										}
									}
								}
								else if (classId == 0)
								{
									foundOrphan = true;
								}
								else
								{
									Debug.Assert(classId == CmPicture.kClassId, "Unknown class id in embedded object: " + classId);
									orcLocation.Object = classId;
								}
							}
						}
					}
				}
			}
			return foundOrphan;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixes all orphaned footnotes or pictures in the given list.
		/// </summary>
		/// <param name="book">The book being fixed (needed in case we have to re-order
		/// footnotes.</param>
		/// <param name="orcLocations">List of locations of ORCs.</param>
		/// <param name="footnotes">List of footnotes and their associated ORC locations if
		/// they were found.</param>
		/// <param name="issuesToReport">List of descriptions of any problems fix or any
		/// remaining problems that the user might still need to fix</param>
		/// ------------------------------------------------------------------------------------
		private void FixOrcsWithoutProps(IScrBook book, List<OrcLocation> orcLocations,
			List<FootnoteOrcLocation> footnotes, List<string> issuesToReport)
		{
			int iFootnote = 0;
			OrcLocation orc;
			for (int i = 0; i < orcLocations.Count; i++)
			{
				orc = orcLocations[i];
				if (orc.Object != null)
				{
					while (iFootnote < footnotes.Count && footnotes[iFootnote++].footnote != orc.Object)
					{
						issuesToReport.Add(String.Format("{0} - Footnote {1} has no corresponding marker",
							book.BookId, iFootnote));
					}
					continue;
				}

				IStTxtPara para = orc.para;
				ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();

				ITsPropsBldr propsBldr = bldr.get_PropertiesAt(orc.ich).GetBldr();
				FootnoteOrcLocation currentFootnote = iFootnote < footnotes.Count ? footnotes[iFootnote] : null;

				if (currentFootnote == null || currentFootnote.location != null)
				{
					// Remove the ORC and report it.
					bldr.Replace(orc.ich, orc.ich + 1, null, null);
					issuesToReport.Add(String.Format(Properties.Resources.kstidCorruptedOrcRemoved,
						orc.ToString(m_scr)));

					// Adjust ORC offsets for any other ORCs in this same para.
					OrcLocation adjOrc;
					for (int j = i + 1; j < orcLocations.Count; j++)
					{
						adjOrc = orcLocations[j];
						if (adjOrc.para == orc.para)
							adjOrc.ich--;
						else
							break;
					}
				}
				else
				{
					IStFootnote footnote = currentFootnote.footnote;

					byte[] footnoteObjData = MiscUtils.GetObjData(footnote.Guid, (byte)FwObjDataTypes.kodtNameGuidHot);
					propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData, footnoteObjData, footnoteObjData.Length);

					bldr.SetProperties(orc.ich, orc.ich + 1, propsBldr.GetTextProps());
					currentFootnote.location = orc; // No longer an orphan :-)
					iFootnote++; // We're now using this one

					issuesToReport.Add(String.Format(Properties.Resources.kstidConnectedFootnoteToMarker, orc.ToString(m_scr)));
				}

				para.Contents.UnderlyingTsString = bldr.GetString();
			}

			while (iFootnote < footnotes.Count)
			{
				Debug.Assert(footnotes[iFootnote].location == null);
				issuesToReport.Add(String.Format(Properties.Resources.kstidNoMarkerForFootnote,
					book.BookId, ++iFootnote));
			}
		}
		#endregion
	}
}
