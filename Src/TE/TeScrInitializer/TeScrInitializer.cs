// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeScrInitializer.cs

using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;
using System;
using SIL.Utils;

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
		protected readonly FdoCache m_cache;
		/// <summary>Scripture of the language project we are working on</summary>
		protected IScripture m_scr;
		#endregion

		#region Constructor
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected TeScrInitializer(FdoCache cache)
		{
			m_cache = cache;
			m_scr = cache.LangProject.TranslatedScriptureOA;
		}
		#endregion

		#region Public static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs any needed tasks to ensure that the project is valid and has any objects
		/// required by this version of the application.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="helpTopicProvider">The Help topic provider.</param>
		/// <param name="existingProgressDlg">The existing progress dialog, if any.</param>
		/// ------------------------------------------------------------------------------------
		public static void EnsureProjectValid(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			IProgress existingProgressDlg)
		{
			TeScrInitializer scrInitializer = new TeScrInitializer(cache);
			scrInitializer.RemoveRtlMarksFromScrProperties();
			scrInitializer.EnsureScriptureTextsValid();
			List<string> issuesToReport = scrInitializer.FixOrcsWithoutProps();
			if (issuesToReport != null)
			{
				using (FixedOrphanFootnoteReportDlg dlg = new FixedOrphanFootnoteReportDlg(
					issuesToReport, cache.ProjectId.UiName, helpTopicProvider))
				{
					dlg.ShowDialog();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs minimal initialization of Scripture needed by FLEx for importing from
		/// Paratext.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void EnsureMinimalScriptureInitialization(FdoCache cache, IThreadedProgress progressDlg,
			IHelpTopicProvider helpTopicProvider)
		{
			TeScrInitializer scrInitializer = new TeScrInitializer(cache);
			if (scrInitializer.m_scr == null)
			{
				progressDlg.RunTask((progDlg, args) =>
				{
					scrInitializer.InitializeScriptureAndStyles(progDlg);
					return null;
				});
			}
			else
				TeStylesXmlAccessor.EnsureCurrentStylesheet(cache, progressDlg, helpTopicProvider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Ensures all of the project components are valid.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public static void EnsureProjectComponentsValid(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			IThreadedProgress existingProgressDlg)
		{
			EnsureProjectValid(cache, helpTopicProvider, existingProgressDlg);

			ILangProject lp = cache.LangProject;

			TeScrBookRefsInit.EnsureFactoryScrBookRefs(cache, existingProgressDlg);
			TeStylesXmlAccessor.EnsureCurrentStylesheet(cache, existingProgressDlg, helpTopicProvider);
			TeScrNoteCategoriesInit.EnsureCurrentScrNoteCategories(lp, existingProgressDlg);
		}
		#endregion

		#region OrcLocation class
		private class OrcLocation
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="OrcLocation"/> class.
			/// </summary>
			/// <param name="para">The paragraph.</param>
			/// <param name="ich">The index of the ORC character.</param>
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

			/// <summary>Footnote object to which this ORC is hooked up</summary>
			internal IStFootnote Footnote = null;

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
		#endregion

		#region FootnoteOrcLocation class
		private class FootnoteOrcLocation
		{
			/// <summary>Footnote</summary>
			internal readonly IStFootnote footnote;
			/// <summary>Location of the corresponding ORC character, if any</summary>
			internal OrcLocation location;

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
		}
		#endregion

		#region Initialization
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Perform one-time initialization of a new Scripture project
		/// </summary>
		/// <returns>true if data loaded successfully; false, otherwise</returns>
		/// -------------------------------------------------------------------------------------
		protected bool Initialize(IThreadedProgress progressDlg)
		{
			if (m_scr != null)
			{
				// Preload all book, section and paragraphs if we already have Scripture
				m_cache.ServiceLocator.DataSetup.LoadDomainAsync(BackendBulkLoadDomain.Scripture);

				ILangProject lp = m_cache.LanguageProject;
				if (m_scr.BookAnnotationsOS.Count != 0 &&
					m_scr.PublicationsOC.Count != 0 &&
					lp.KeyTermsList.PossibilitiesOS.Count >= 1 &&
					m_scr.NoteCategoriesOA != null && m_scr.NoteCategoriesOA.PossibilitiesOS.Count > 0)
				{
					return true;
				}
			}

			try
			{
				progressDlg.RunTask(InitializeScriptureProject);
			}
			catch (WorkerThreadException e)
			{
				while (m_cache.DomainDataByFlid.GetActionHandler().CanUndo())
				{
					UndoResult ures = m_cache.DomainDataByFlid.GetActionHandler().Undo();
					// Enhance JohnT: make use of ures?
				}
				MessageBox.Show(Form.ActiveForm, e.InnerException.Message, FwUtils.ksFlexAppName,
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the scripture project.
		/// </summary>
		/// <param name="progressDialog">The progress dialog.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private object InitializeScriptureProject(IThreadedProgress progressDialog, object[] parameters)
		{
			ILangProject lp = m_cache.LanguageProject;
			if (m_scr == null)
				InitializeScriptureAndStyles(progressDialog);

			// REVIEW: Since all the version-based initialization will be taken care of by a
			// subsequent call to EnsureProjectComponentsValid, we could probably get rid of this and/or
			// put it in the above block as we do with the stylesheet initialization.

			//Initialize the annotation categories
			if (m_scr.NoteCategoriesOA == null || m_scr.NoteCategoriesOA.PossibilitiesOS.Count == 0)
				TeScrNoteCategoriesInit.CreateFactoryScrNoteCategories(progressDialog, m_scr);

			// For good measure, on the off-chance the user notices.
			progressDialog.Position = progressDialog.Maximum;

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the Scripture object, along with styles, book names and abbreviations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeScriptureAndStyles(IProgress progressDlg)
		{
			m_scr = m_cache.LanguageProject.TranslatedScriptureOA = m_cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();

			//Initialize factory styles
			TeStylesXmlAccessor.CreateFactoryScrStyles(progressDlg, m_scr);

			//Initialize Scripture Book Ref info
			TeScrBookRefsInit.SetNamesAndAbbreviations(progressDlg, m_cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sanity check to ensure the scripture texts are valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void EnsureScriptureTextsValid()
		{
			foreach (IScrBook book in m_scr.ScriptureBooksOS)
			{
				if (book.SectionsOS.Count == 0)
					continue;

				ContextValues sectionContext;
				foreach (IScrSection section in book.SectionsOS)
				{
					// Check the heading paragraphs.
					if (section.HeadingOA == null)
						section.HeadingOA = m_cache.ServiceLocator.GetInstance<IStTextFactory>().Create();

					if (section.HeadingOA.ParagraphsOS.Count == 0)
					{
						IStTxtPara para = m_cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
							section.HeadingOA, ScrStyleNames.SectionHead);
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
						section.ContentOA = m_cache.ServiceLocator.GetInstance<IStTextFactory>().Create();

					if (section.ContentOA.ParagraphsOS.Count == 0)
					{
						IStTxtPara para = m_cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
							section.ContentOA, TeStylesXmlAccessor.GetDefaultStyleForContext(sectionContext, false));
					}
				}
			}
		}
		#endregion

		#region Private helper methods
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
		#endregion

		#region FixOrcsWithoutProps stuff
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
			if (m_scr.FixedOrphanedFootnotes)
				return null;

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

			m_scr.FixedOrphanedFootnotes = true;
			return issuesToReport;
		}

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
		/// <param name="parasWithOrcs">List of paragraphs and ORC positions (excluding known
		/// picture ORCs).</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static bool FindOrcsWithoutPropsInText(IStText text, string textLocationInfo,
			ref BCVRef startRef, ref BCVRef endRef,
			List<FootnoteOrcLocation> footnotes, List<OrcLocation> parasWithOrcs)
		{
			bool foundOrphan = false;
			foreach (IStTxtPara para in text.ParagraphsOS)
			{
				ITsString tssContents = para.Contents;
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
							if (sContents[ich] != StringUtils.kChObject)
								continue;

							OrcLocation orcLocation = new OrcLocation(para, ich, startRef, endRef, textLocationInfo);

							ITsTextProps props = tssContents.get_PropertiesAt(ich);
							string objData = props.GetStrPropValue((int)FwTextPropType.ktptObjData);

							if (objData == null)
								foundOrphan = true;
							else
							{
								// first char. of strData is type code - GUID will follow it.
								Guid objGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));

								ICmObject obj;
								if (!text.Cache.ServiceLocator.ObjectRepository.TryGetObject(objGuid, out obj))
								{
									foundOrphan = true;
								}
								else if (obj.ClassID == ScrFootnoteTags.kClassId)
								{
									foreach (FootnoteOrcLocation footnote in footnotes)
									{
										if (footnote.footnote.Guid == objGuid)
										{
											orcLocation.Footnote = footnote.footnote;
											footnote.location = orcLocation;
											break;
										}
									}
								}
								else
								{
									Debug.Assert(obj.ClassID == CmPictureTags.kClassId, "Unknown class id in embedded object: " + obj.ClassID);
									continue;
								}
							}
							parasWithOrcs.Add(orcLocation);
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
				if (orc.Footnote != null)
				{
					while (iFootnote < footnotes.Count && footnotes[iFootnote++].footnote != orc.Footnote)
					{
						issuesToReport.Add(String.Format("{0} - Footnote {1} has no corresponding marker",
							book.BookId, iFootnote));
					}
					continue;
				}

				IStTxtPara para = orc.para;
				ITsStrBldr bldr = para.Contents.GetBldr();

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

					byte[] footnoteObjData = TsStringUtils.GetObjData(footnote.Guid, (byte)FwObjDataTypes.kodtNameGuidHot);
					propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData, footnoteObjData, footnoteObjData.Length);

					bldr.SetProperties(orc.ich, orc.ich + 1, propsBldr.GetTextProps());
					currentFootnote.location = orc; // No longer an orphan :-)
					iFootnote++; // We're now using this one

					issuesToReport.Add(String.Format(Properties.Resources.kstidConnectedFootnoteToMarker, orc.ToString(m_scr)));
				}

				para.Contents = bldr.GetString();
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
