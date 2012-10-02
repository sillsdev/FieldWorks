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
// File: ScrObjWrapper.cs
// Responsibility: TE Team
//
// <remarks>
// Implementation of ScrOjbWrapper.
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Paratext;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;
using ScrVers = SILUBS.SharedScrUtils.ScrVers;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class exists to allow a single interface for the TeImporter to call to read the
	/// scripture import data. Currently it can access the TESO for reading general standard
	/// format files, and P6SO for reading Paratext 6 projects.
	/// Certain functionality is normalized to what TE requires. E.g. this wrapper must
	/// provide a scr ref with verse of zero for introductory material, unlike what P6SO
	/// might do.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrObjWrapper
	{
		#region Member data variables
		/// <summary>
		/// Settings object that represent the Paratext or SF settings for importing
		/// </summary>
		protected IScrImportSet m_settings;

		/// <summary>the Paratext project</summary>
		protected ScrText m_ptProjectText;
		/// <summary>the Paratext text parser</summary>
		protected ScrParser m_ptParser;
		/// <summary>The current state of the Paratext parser.</summary>
		protected ScrParserState m_ptParserState;
		/// <summary>The current verse token that is being processed for Paratext import</summary>
		protected int m_ptCurrentToken;
		/// <summary>List of tokens that make up the current Paratext book </summary>
		protected List<UsfmToken> m_ptBookTokens;
		/// <summary>The current reference that is being processed (currently only Book granularity)</summary>
		protected VerseRef m_ptCurrBook;

		/// <summary>the TE scripture object</summary>
		protected ISCScriptureText m_scSfmText;
		/// <summary>TESO lib text enum object</summary>
		protected ISCTextEnum m_scTextEnum;
		/// <summary>Text segment, gets advanced through the text.</summary>
		protected ISCTextSegment m_scTextSegment;
		/// <summary></summary>
		protected TypeOfImport m_ImportType;
		/// <summary>Current domain being processed</summary>
		protected ImportDomain m_currentDomain = ImportDomain.Main;
		#endregion

		#region Construction, Initilization, Cleanup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the scripture project and enumerator, preparing us to read the data files.
		/// </summary>
		/// <param name="settings">Import settings object (filled in by wizard)</param>
		/// ------------------------------------------------------------------------------------
		public void LoadScriptureProject(IScrImportSet settings)
		{
			m_settings = settings;
			m_ImportType = settings.ImportTypeEnum;

			// Load ScriptureText object
			switch (TypeOfImport)
			{
				case TypeOfImport.Paratext6:
					if ((!settings.ImportTranslation && !settings.ImportAnnotations) || !LoadParatextVernacularProject())
						if (!LoadParatextBackTranslationProject())
							if (!LoadParatextNotesProject())
								throw new InvalidOperationException("There was nothing worth loading.");
					break;
				case TypeOfImport.Other:
				case TypeOfImport.Paratext5:
					ScrVers versification = m_settings.Cache.LangProject.TranslatedScriptureOA.Versification;
					m_settings.CheckForOverlappingFilesInRange(
						new ScrReference(m_settings.StartRef, versification),
						new ScrReference(m_settings.EndRef, versification));

					m_scSfmText = new SCScriptureText(settings, ImportDomain.Main);

					// Now initialize the TextEnum with the range of scripture text we want
					m_scTextEnum = m_scSfmText.TextEnum(m_settings.StartRef, m_settings.EndRef);
					break;
				default:
					Debug.Assert(false, "bogus TypeOfImport");
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the Paratext project and enumerator, preparing us to read the data files.
		/// </summary>
		/// <param name="paratextProjectId">3-letter Paratext project ID</param>
		/// <returns>true if the project was loaded, else false</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadParatextProject(string paratextProjectId)
		{
			try
			{
				m_ptProjectText = new ScrText(paratextProjectId);
			}
			catch (Exception e)
			{
				// Can't load Paratext project if Paratext is not installed.
				throw new ParatextLoadException(
					TeResourceHelper.GetResourceString("kstidCheckParatextInstallation"), e);
			}

			try
			{
				// create ref objs of the Paratext lib
				Logger.WriteEvent("Loading Paratext project " + paratextProjectId + " to import from " +
					m_settings.StartRef.AsString + " to " + m_settings.EndRef.AsString);

				// Now initialize the TextEnum with the range of Scripture text we want
				m_ptParser = m_ptProjectText.Parser();
				m_ptCurrBook = new VerseRef(m_settings.StartRef.Book, 0, 0);
				ResetParatextState();
			}
			catch (Exception e)
			{
				string msg = string.Format(
					TeResourceHelper.GetResourceString("kstidParatextProjectLoadFailure"),
					paratextProjectId);
				throw new ParatextLoadException(msg, e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform any necessary cleanup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Cleanup()
		{
			if (m_scTextEnum != null)
				m_scTextEnum.Cleanup();
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Advance the scripture text object enumerator to the next segment.
		/// </summary>
		/// <remarks>Virtual to support testing</remarks>
		/// <param name="sText">Set to the text of the current segment</param>
		/// <param name="sMarker">Set to the marker of the current segment tag</param>
		/// <param name="domain">Set to the domain of the stream being processed</param>
		/// <returns>True if successful. False if there are no more segments.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool GetNextSegment(out string sText, out string sMarker,
			out ImportDomain domain)
		{
			domain = m_currentDomain;

			if (TypeOfImport == TypeOfImport.Paratext6)
			{
				GetNextParatextSegment(out sText, out sMarker);
				if (sMarker != null)
					return true;

				// We didn't get any segment so assume that we hit the end of the data for the book
				// ENHANCE: When we support partial book imports, this needs to change.
				m_ptCurrBook.BookNum++;
				if (m_ptCurrBook.BookNum <= m_settings.EndRef.Book)
				{
					ResetParatextState();
					return GetNextSegment(out sText, out sMarker, out domain);
				}

				// We hit the end of our import range so try the next import domain
				m_ptCurrBook.BookNum = m_settings.StartRef.Book;
				switch (m_currentDomain)
				{
					case ImportDomain.Main:
					{
						if (LoadParatextBackTranslationProject())
							return GetNextSegment(out sText, out sMarker, out domain);
						goto case ImportDomain.BackTrans;
					}
					case ImportDomain.BackTrans:
					{
						if (LoadParatextNotesProject())
							return GetNextSegment(out sText, out sMarker, out domain);
						break;
					}
				}
			}
			else if (TypeOfImport == TypeOfImport.Other || TypeOfImport == TypeOfImport.Paratext5)
			{
				m_scTextSegment = m_scTextEnum.Next();
				if (m_scTextSegment != null)
				{
					sText = m_scTextSegment.Text;
					sMarker = m_scTextSegment.Marker;
					return true;
				}
				switch (m_currentDomain)
				{
					case ImportDomain.Main:
					{
						m_currentDomain = ImportDomain.BackTrans;
						m_scSfmText = new SCScriptureText(m_settings, ImportDomain.BackTrans);
						// Now initialize the TextEnum with the range of scripture text we want
						m_scTextEnum = m_scSfmText.TextEnum(m_settings.StartRef, m_settings.EndRef);
						return GetNextSegment(out sText, out sMarker, out domain);
					}
					case ImportDomain.BackTrans:
					{
						m_currentDomain = ImportDomain.Annotations;
						m_scSfmText = new SCScriptureText(m_settings, ImportDomain.Annotations);
						// Now initialize the TextEnum with the range of scripture text we want
						m_scTextEnum = m_scSfmText.TextEnum(m_settings.StartRef, m_settings.EndRef);
						return GetNextSegment(out sText, out sMarker, out domain);
					}
				}
			}
			else
				throw new Exception("GetNextSegment has an invalid import type");

			sText = null;
			sMarker = null;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current writing system
		/// </summary>
		/// <param name="defaultWs">The default to use (if this is a Paratext import)</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual int CurrentWs(int defaultWs)
		{
			// For Paratext, we don't know about Writing Systems, so just return the default
			// passed by the caller.
			if (TypeOfImport == TypeOfImport.Paratext6)
				return defaultWs;
			int hvoWs = m_scTextEnum.CurrentWs;
			return hvoWs == -1 ? defaultWs : hvoWs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the annotation type for the current import stream
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int CurrentAnnotationType
		{
			get
			{
				return (m_scTextEnum != null) ? m_scTextEnum.CurrentNoteTypeHvo : 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the ImportedCheckSum as appropriate for the given book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void SetBookCheckSum(IScrBook scrBook)
		{
			// Really don't do anything unless importing from Paratext 7.1 or later
			if (TypeOfImport == TypeOfImport.Paratext6 && m_ptProjectText != null)
			{
				if (m_currentDomain == ImportDomain.Main)
					scrBook.ImportedCheckSum = m_ptProjectText.GetBookCheckSum(scrBook.CanonicalNum);
				else if (m_currentDomain == ImportDomain.BackTrans)
				{
					// For now, we only support importing Paratext BTs into the default
					// analysis writing system.
					scrBook.ImportedBtCheckSum.set_String(scrBook.Cache.DefaultAnalWs,
						m_ptProjectText.GetBookCheckSum(scrBook.CanonicalNum));
				}

			}
		}
		#endregion

		#region Miscellaneous public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the first reference of the current segment.
		/// </summary>
		/// <remarks>Virtual to support testing</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual BCVRef SegmentFirstRef
		{
			get
			{
				if (TypeOfImport == TypeOfImport.Paratext6)
					return MakeBCVRef(m_ptParserState.VerseRef);
				if (TypeOfImport == TypeOfImport.Other || TypeOfImport == TypeOfImport.Paratext5)
					return m_scTextSegment.FirstReference;
				Debug.Assert(false, "bogus TypeOfImport");
				return new BCVRef();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the last reference of the current segment.
		/// </summary>
		/// <remarks>Virtual to support testing</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual BCVRef SegmentLastRef
		{
			get
			{
				if (TypeOfImport == TypeOfImport.Paratext6)
					return MakeBCVRef(m_ptParserState.VerseRef.AllVerses().Last());
				if (TypeOfImport == TypeOfImport.Other || TypeOfImport == TypeOfImport.Paratext5)
					return m_scTextSegment.LastReference;
				Debug.Assert(false, "bogus TypeOfImport");
				return new BCVRef();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the filename from which the current segment was read.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string CurrentFileName
		{
			get
			{
				if (TypeOfImport == TypeOfImport.Other || TypeOfImport == TypeOfImport.Paratext5)
					return m_scTextSegment.CurrentFileName;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the line number from which the current segment was read
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int CurrentLineNumber
		{
			get
			{
				if (TypeOfImport == TypeOfImport.Other || TypeOfImport == TypeOfImport.Paratext5)
					return m_scTextSegment.CurrentLineNumber;
				return 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the BooksPresent property of the proper scripture text.
		/// </summary>
		/// <exception cref="NotSupportedException">If project is not a support type</exception>
		/// ------------------------------------------------------------------------------------
		public List<int> BooksPresent
		{
			get { return m_settings.BooksForProject; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets wheter project includes a separate stream with a back translation. If project
		/// has no BT or the BT is interleaved, this returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">If project is not a support type</exception>
		/// ------------------------------------------------------------------------------------
		public bool HasNonInterleavedBT
		{
			get { return m_settings.HasNonInterleavedBT; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether project includes a separate stream with annotations, or notes. If
		/// project has no annotations or they are interleaved, this returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">If project is not a support type</exception>
		/// ------------------------------------------------------------------------------------
		public bool HasNonInterleavedNotes
		{
			get { return m_settings.HasNonInterleavedNotes; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book marker
		/// </summary>
		/// <exception cref="NotSupportedException">If project is not a support type</exception>
		/// ------------------------------------------------------------------------------------
		public string BookMarker
		{
			get
			{
				switch (m_settings.ImportTypeEnum)
				{
					case TypeOfImport.Paratext6:
					{
						try
						{
							ScrTag bookTag = m_ptProjectText.DefaultStylesheet.Tags.FirstOrDefault(t => (t.TextProperties & TextProperties.scBook) != 0);
							return bookTag != null ? @"\" + bookTag.Marker : @"\id";
						}
						catch
						{
							return @"\id";
						}
					}
					case TypeOfImport.Other:
						return @"\id";
					default:
						throw new NotSupportedException("Unexpected type of Import Project");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of folders to search when importing a picture that does not have a full
		/// path specified. If the picture is not found in any of these folders, the first one
		/// will be used to build a full path (the picture will display in TE as missing, but
		/// will show that path as the expected location).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual List<string> ExternalPictureFolders
		{
			get
			{
				List<string> externalPaths = new List<string>();

				// If the Paratext Scripture Object is defined and the import type is Paratext 6,
				if (m_ptProjectText != null && m_ImportType == TypeOfImport.Paratext6)
				{
					// construct the directory using Paratext settings. For example, with a short
					// name of KAL, the directory would be: "C:\My Paratext Projects\KAL\Figures"
					externalPaths.Add(Path.Combine(m_ptProjectText.SettingsDirectory,
						Path.Combine(m_ptProjectText.Name, "Figures")));
				}
				externalPaths.Add(m_settings.Cache.LangProject.LinkedFilesRootDir);
				externalPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

				return externalPaths;
			}
		}
		#endregion

		#region Non-public
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a BCVRef from a VerseRef.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static BCVRef MakeBCVRef(VerseRef verseRef)
		{
			int segment = string.IsNullOrEmpty(verseRef.Segment) ? 0 : verseRef.Segment[0] - 'a';
			if (segment < 0 || segment > 2)
				segment = 0;
			return new BCVRef(verseRef.BookNum, verseRef.ChapterNum, verseRef.VerseNum, segment);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the state of the paratext import for the beginning of a new book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ResetParatextState()
		{
			m_ptParserState = new ScrParserState(m_ptProjectText, m_ptCurrBook);
			m_ptBookTokens = m_ptParser.GetUsfmTokens(m_ptCurrBook, false, true);
			m_ptCurrentToken = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the next paratext segment for import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GetNextParatextSegment(out string sText, out string sMarker)
		{
			sMarker = null;
			sText = string.Empty;
			while (m_ptCurrentToken < m_ptBookTokens.Count)
			{
				UsfmToken token = m_ptBookTokens[m_ptCurrentToken];
				if (token.Type == UsfmTokenType.Text)
					sText += token.Text;
				else
				{
					if (sMarker != null)
						break; // Found another marker so we got all the text for this one
					sMarker = @"\" + token.Marker; // We expect the marker to have the slash
				}
				m_ptParserState.UpdateState(m_ptBookTokens, m_ptCurrentToken);

				if (!ScrImportFileInfo.IsValidMarker(sMarker))
				{
					throw new ScriptureUtilsException(SUE_ErrorCode.InvalidCharacterInMarker, null, 0,
						sMarker + sText, SegmentFirstRef);
				}
				m_ptCurrentToken++;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Type of import - a convenient local property.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected virtual TypeOfImport TypeOfImport
		{
			get {return m_ImportType;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the P6 vernacular project if it is specified and is to be included
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool LoadParatextVernacularProject()
		{
			if (m_settings.ParatextScrProj == null)
				return false;
			m_currentDomain = ImportDomain.Main;
			LoadParatextProject(m_settings.ParatextScrProj);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the P6 Back translation project if it is specified and is to be included
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool LoadParatextBackTranslationProject()
		{
			if (m_settings.ParatextBTProj == null ||
				!(m_settings.ImportBackTranslation || m_settings.ImportAnnotations))
				return false;
			m_currentDomain = ImportDomain.BackTrans;
			LoadParatextProject(m_settings.ParatextBTProj);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the P6 notes project if it is specified and is to be included
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool LoadParatextNotesProject()
		{
			if (m_settings.ParatextNotesProj == null || !m_settings.ImportAnnotations)
				return false;
			m_currentDomain = ImportDomain.Annotations;
			LoadParatextProject(m_settings.ParatextNotesProj);
			return true;
		}
		#endregion
	}
}
