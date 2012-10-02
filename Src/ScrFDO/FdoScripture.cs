// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoScripture.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.Scripture
{
	#region RefRunType enum
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// What kind of reference a run is
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum RefRunType
	{
		/// <summary>Run is neither chapter nor verse number</summary>
		None,
		/// <summary>Run is a chapter number</summary>
		Chapter,
		/// <summary>Run is a verse number</summary>
		Verse,
		/// <summary>A chapter run immediatly followed by a verse run</summary>
		ChapterAndVerse
	}
	#endregion

	#region Invalid chapter and verse exceptions
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class InvalidScriptureNumberException : ApplicationException
	{
		private string m_badNumber;
		private TsRunInfo m_runInfo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="badNumber">String with the invalid chapter or verse number</param>
		/// <param name="runInfo">Info about the run</param>
		/// ------------------------------------------------------------------------------------
		public InvalidScriptureNumberException(string badNumber, TsRunInfo runInfo)
			: base()
		{
			m_badNumber = badNumber;
			m_runInfo = runInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the invalid chapter or verse number string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InvalidNumber
		{
			get { return m_badNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the info about the run
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TsRunInfo RunInfo
		{
			get { return m_runInfo; }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InvalidChapterException : InvalidScriptureNumberException
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="badChapterNumber">String with the invalid chapter number</param>
		/// <param name="runInfo">Info about the run</param>
		/// ------------------------------------------------------------------------------------
		public InvalidChapterException(string badChapterNumber, TsRunInfo runInfo)
			: base(badChapterNumber, runInfo)
		{
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InvalidVerseException : InvalidScriptureNumberException
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="badVerseNumber">String with the invalid verse number</param>
		/// <param name="runInfo">Info about the run</param>
		/// ------------------------------------------------------------------------------------
		public InvalidVerseException(string badVerseNumber, TsRunInfo runInfo)
			: base(badVerseNumber, runInfo)
		{
		}
	}
	#endregion

	#region NullStyleRulesException
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Special exception to allow us to catch the condition where StyleRules is null in a
	/// common way.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NullStyleRulesException : NullReferenceException
	{
	}
	#endregion

	#region class Scripture
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Scripture represents a vernacular translation (one per language project).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class Scripture : IScrProjMetaDataProvider, IPictureLocationBridge
	{
		/// <summary>Accessor for getting at the generated resource manager from other
		/// assemblies</summary>
		public static System.Resources.ResourceManager Resources = ScrFdoResources.ResourceManager;

		#region Public constants
		/// <summary>One-based index of first book in old testament</summary>
		public const short kiOtMin = 1;
		/// <summary>One-based index of last book in old testament</summary>
		public const short kiOtMax = 39;
		/// <summary>One-based index of first book in new testament</summary>
		public const short kiNtMin = 40;
		/// <summary>One-based index of last book in new testament</summary>
		public const short kiNtMax = 66;
		/// <summary>Footnote marker used for initial value of auto generated
		/// footnote markers.</summary>
		public const string kDefaultAutoFootnoteMarker = "a";
		/// <summary>Footnote marker used for initial value of auto generated
		/// footnote markers.</summary>
		public const string kDefaultFootnoteMarkerSymbol = "*";

		/// <summary>Name for the default import settings</summary>
		public const string kDefaultImportSettingsName = "Default";
		/// <summary>Default name for the Paratext 5 import settings</summary>
		public const string kParatext5ImportSettingsName = "Paratext5";
		/// <summary>Default name for the Paratext 6 import settings</summary>
		public const string kParatext6ImportSettingsName = "Paratext6";
		/// <summary>Default name for the other import settings</summary>
		public const string kOtherImportSettingsName = "Other";
		#endregion

		#region Scripture Properties

		// REVIEW TomB: Do we want the default values for the following four properties to come
		// from a resource file so they can be localized?

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a string (usually one character) that separates complete references or
		/// chapter/verse references in a list. In the U.S.A., this is traditionally a
		/// semi-colon (;), e.g., Mat 24:16; Rev 1:2;4:5.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string RefSepr
		{
			get
			{
				string s = RefSepr_Generated;
				return (s == null ? ";" : s);
			}
			set
			{
				RefSepr_Generated = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// gets or sets a string (usually one character) that separates the chapter number
		/// from the verse number in a reference. In the U.S.A., this is a traditionally
		/// colon (:), e.g., Mat 24:16.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ChapterVerseSepr
		{
			get
			{
				string s = ChapterVerseSepr_Generated;
				return (s == null ? ":" : s);
			}
			set
			{
				ChapterVerseSepr_Generated = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a string (usually one character) that separates non-contiguous verse
		/// numbers in a list. In the U.S.A., this is traditionally a comma (,), e.g.,
		/// Mat 24:16,25.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string VerseSepr
		{
			get
			{
				string s = VerseSepr_Generated;
				return (s == null ? "," : s);
			}
			set
			{
				VerseSepr_Generated = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a string (usually one character) that bridges contiguous chapter
		/// and/or verse ranges in a reference. In the U.S.A., this is traditionally a dash
		/// (-), e.g., Mat 24:16-25; Rev 1:7-2:9.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Bridge
		{
			get
			{
				string s = Bridge_Generated;
				return (s == null ? "-" : s);
			}
			set
			{
				Bridge_Generated = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the callout option for cross-references.
		/// Getter takes CrossRefsCombinedWithFootnotes into consideration.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FootnoteMarkerTypes CrossRefMarkerType
		{
			get
			{
				return CrossRefsCombinedWithFootnotes ? FootnoteMarkerType :
					CrossRefMarkerType_Generated;
			}
			set { CrossRefMarkerType_Generated = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the footnote settings are all set to the default values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasDefaultFootnoteSettings
		{
			get
			{
				return FootnoteMarkerType == FootnoteMarkerTypes.AutoFootnoteMarker &&
					FootnoteMarkerSymbol == Scripture.kDefaultFootnoteMarkerSymbol &&
					DisplayFootnoteReference == false &&
					CrossRefsCombinedWithFootnotes == false &&
					CrossRefMarkerType == FootnoteMarkerTypes.NoFootnoteMarker &&
					CrossRefMarkerSymbol == Scripture.kDefaultFootnoteMarkerSymbol &&
					DisplayCrossRefReference == true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden because getter takes CrossRefsCombinedWithFootnotes into consideration
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CrossRefMarkerSymbol
		{
			get
			{
				return CrossRefsCombinedWithFootnotes ? FootnoteMarkerSymbol : CrossRefMarkerSymbol_Generated;
			}
			set
			{
				CrossRefMarkerSymbol_Generated = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the nominal cross-reference marker given the CrossRefMarkerType and
		/// CrossRefMarkerSymbol
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CrossRefMarker
		{
			get
			{
				switch (CrossRefMarkerType)
				{
					case FootnoteMarkerTypes.NoFootnoteMarker:
						return null;
					case FootnoteMarkerTypes.SymbolicFootnoteMarker:
						return CrossRefMarkerSymbol;
					case FootnoteMarkerTypes.AutoFootnoteMarker:
					default:
						return kDefaultAutoFootnoteMarker;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the nominal footnote marker based on the FootnoteMarkerType and
		/// FootnoteMarkerSymbol
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string GeneralFootnoteMarker
		{
			get
			{
				switch (FootnoteMarkerType)
				{
					case FootnoteMarkerTypes.NoFootnoteMarker:
						return null;
					case FootnoteMarkerTypes.SymbolicFootnoteMarker:
						return FootnoteMarkerSymbol;
					case FootnoteMarkerTypes.AutoFootnoteMarker:
					default:
						return kDefaultAutoFootnoteMarker;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the current versification scheme
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Paratext.ScrVers Versification
		{
			get
			{
				Paratext.ScrVers vers = Versification_Generated;
				// ENHANCE (TE-6620): Rather than blindly defaulting to English, we could:
				// a) Get a default from a local XML file or something to allow
				// branch defaults.
				// b) Make sure the .vrs file is present and choose another if not.
				return vers == Paratext.ScrVers.Unknown ? Paratext.ScrVers.English : vers;
			}
			set
			{
				Versification_Generated = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden because getter takes CrossRefsCombinedWithFootnotes into consideration
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DisplayCrossRefReference
		{
			get
			{
				return CrossRefsCombinedWithFootnotes ? DisplayFootnoteReference :
					DisplayCrossRefReference_Generated;
			}
			set
			{
				DisplayCrossRefReference_Generated = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to display the general footnote marker in the footnote pane/stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DisplayFootnoteMarker
		{
			get
			{
				return FootnoteMarkerType == FootnoteMarkerTypes.AutoFootnoteMarker || DisplaySymbolInFootnote;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether to display the cross-reference marker in the footnote pane/stream.
		/// Takes CrossRefsCombinedWithFootnotes into consideration
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DisplayCrossRefMarker
		{
			get
			{
				return CrossRefsCombinedWithFootnotes ? DisplayFootnoteMarker :
					CrossRefMarkerType == FootnoteMarkerTypes.AutoFootnoteMarker || DisplaySymbolInCrossRef;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the last-used import settings for this project, if any.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int DefaultImportSettingsHvo_internal
		{
			get
			{
				foreach (int hvoImportSetting in ImportSettingsOC.HvoArray)
				{
					if (Cache.GetMultiUnicodeAlt(hvoImportSetting, (int)ScrImportSet.ScrImportSetTags.kflidName,
						Cache.DefaultUserWs, null) == kDefaultImportSettingsName)
					{
						return hvoImportSetting;
					}
				}
				return 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the import settings named Default, if any, or the first available
		/// one (which is probably the only one), or returns 0 if no settings are available.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DefaultImportSettingsHvo
		{
			get
			{
				int hvo = DefaultImportSettingsHvo_internal;
				if (hvo > 0)
					return hvo;

				if (ImportSettingsOC.Count > 0)
					return ImportSettingsOC.HvoArray[0];
				else
				{
					return Cache.CreateObject(ScrImportSet.kClassId, Hvo,
						(int)ScriptureTags.kflidImportSettings, 0);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets default import settings. Default settings can be for Paratext 5, Paratext 6,
		/// and Other USFM. Setting the import settings to a null value, clears the settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrImportSet DefaultImportSettings
		{
			get
			{
				if (ImportSettingsOC.Count == 0)
					return null;

				foreach (IScrImportSet settings in ImportSettingsOC)
				{
					if (settings.Name.UserDefaultWritingSystem == kDefaultImportSettingsName)
						return settings;
				}

				// We didn't find settings with the name "Default", so set the first
				// set in the collection to default and return it.
				ScrImportSet firstSettings = new ScrImportSet(m_cache, ImportSettingsOC.HvoArray[0]);
				firstSettings.Name.UserDefaultWritingSystem = kDefaultImportSettingsName;
				return firstSettings;
			}

			set
			{
				int hvoExistingSettings = DefaultImportSettingsHvo_internal;
				ScrImportSet existingSettings;

				if (hvoExistingSettings != 0)
				{
					// We have default import settings specified.
					existingSettings = new ScrImportSet(m_cache, hvoExistingSettings);
				}
				else
				{
					// If no settings have been set as the default, we attempt to find settings of the same type.
					// Currently, we maintain settings for each type of import type.
					// If the new value has an import type, then we attempt to find these settings.
					if (value != null)
						existingSettings = (ScrImportSet)FindImportSettings(value.ImportTypeEnum);
				}

				if (value == null && hvoExistingSettings != 0)
				{
					// We are clearing the existing default settings.
					ImportSettingsOC.Remove(hvoExistingSettings);
				}
				else if (value != null)
				{
					// Determine if anything else was set as the default previously.
					// If so, change the name from Default to the name of the type of import.
					foreach (IScrImportSet importSet in ImportSettingsOC)
					{
						bool fSetToDefault = (importSet.Name.UserDefaultWritingSystem == kDefaultImportSettingsName);

						// if the import set is set to "default" and it is not the type of
						// import settings that we are setting as the default...
						if (fSetToDefault && value.ImportTypeEnum != importSet.ImportTypeEnum)
						{
							// set it back to the name of this type of import.
							switch (importSet.ImportTypeEnum)
							{
								case TypeOfImport.Other:
									importSet.Name.UserDefaultWritingSystem = kOtherImportSettingsName;
									break;
								case TypeOfImport.Paratext5:
									importSet.Name.UserDefaultWritingSystem = kParatext5ImportSettingsName;
									break;
								case TypeOfImport.Paratext6:
									importSet.Name.UserDefaultWritingSystem = kParatext6ImportSettingsName;
									break;
								default:
									throw new InvalidOperationException("Invalid import settings");
							}
						}
						// or, if it not set to "default" but is the type of import settings
						// that we are setting as the default...
						else if (!fSetToDefault && value.ImportTypeEnum == importSet.ImportTypeEnum)
						{
							// set it as the "default".
							importSet.Name.UserDefaultWritingSystem = kDefaultImportSettingsName;
						}
					}

					if (!ImportSettingsOC.Contains(value.Hvo))
						ImportSettingsOC.Add(value);
				}
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the referenced paragraph for annotations with introduction verse references
		/// so that the annotations point to paragraphs in current books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AdjustAnnotationReferences()
		{
			foreach (IScrBookAnnotations annotations in BookAnnotationsOS)
			{
				ScrBook book = (ScrBook) FindBook(annotations.OwnOrd);
				if (book == null)
					continue;

				foreach (IScrScriptureNote note in annotations.NotesOS)
				{
					if (BCVRef.GetVerseFromBcv(note.BeginRef) != 0)
					{
						// Annotations are in verse order, can stop looking at first
						// annotation that references an actual verse number.
						break;
					}

					AttachAnnotatedObjects(book, note);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attaches the annotated objects, if they're not already attached.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AttachAnnotatedObjects(int bookNum, IScrScriptureNote note)
		{
			ScrBook book = FindBook(bookNum) as ScrBook;
			if (book != null)
				AttachAnnotatedObjects(book, note);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attaches the annotated objects, if they're not already attached.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AttachAnnotatedObjects(ScrBook book, IScrScriptureNote note)
		{
			if (note.BeginObjectRA == null)
				FindMissingReference(book, note);
			else if (Cache.GetOwnerOfObjectOfClass(note.BeginObjectRAHvo, (int)ScrBook.kclsidScrBook) != book.Hvo)
			{
				note.BeginObjectRA = note.EndObjectRA = FindCorrespondingParagraph(book, note.BeginObjectRA);
				if (note.BeginObjectRA == null)
					FindMissingReference(book, note);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds replacement reference in current book for missing object reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FindMissingReference(ScrBook book, IScrScriptureNote note)
		{
			if (!((StJournalText)note.QuoteOA).IsEmpty)
				note.BeginObjectRA = note.EndObjectRA = FindQuoteInTitleOrIntro(book, note);

			if (note.BeginObjectRA == null)
				note.BeginObjectRA = note.EndObjectRA = FirstIntroParagraph(book);

			if (note.BeginObjectRA == null)
				note.BeginObjectRA = note.EndObjectRA = FirstTitleParagraph(book);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds paragraph int the title or introduction of the passed book that contains the
		/// passed quote.
		/// </summary>
		/// <param name="book">Current version of book</param>
		/// <param name="note">Note containing quote</param>
		/// <returns>
		/// Paragraph in title or intro that contains quote if quote is found;
		/// otherwise <c>null</c> will be returned.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private ICmObject FindQuoteInTitleOrIntro(ScrBook book, IScrScriptureNote note)
		{
			string quote = ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text;
			ICmObject result = SearchStTextForQuote(book.TitleOA, note, quote);
			if (result == null)
			{
				ScrSection section = (ScrSection)book.SectionsOS[0];
				int sectionIndex = 0;
				while (section.IsIntro)
				{
					result = SearchStTextForQuote(section.HeadingOA, note, quote);
					if (result != null)
						break;
					result = SearchStTextForQuote(section.ContentOA, note, quote);
					if (result != null)
						break;
					sectionIndex++;
					if (sectionIndex >= book.SectionsOS.Count)
						break;
					section = (ScrSection)book.SectionsOS[sectionIndex];
				}
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches given StText for quoted text.
		/// </summary>
		/// <param name="text">Text to be searche</param>
		/// <param name="note">Note containing quote</param>
		/// <param name="quote">Quote</param>
		/// <returns>
		/// Paragraph that contains quote if quote is found; otherwise <c>null</c> will be returned.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private ICmObject SearchStTextForQuote(IStText text, IScrScriptureNote note, string quote)
		{
			List<string> stylesToRemove = new List<string>();
			stylesToRemove.Add(ScrStyleNames.ChapterNumber);
			stylesToRemove.Add(ScrStyleNames.VerseNumber);
			foreach (StTxtPara para in text.ParagraphsOS)
			{
				if (para.Contents.Length == 0)
					continue;
				// Remove chapter/verse numbers and footnotes from paragraph contents - this is done when
				// quote is created for the note.
				ITsString tssQuote = StringUtils.RemoveORCsAndStylesFromTSS(para.Contents.UnderlyingTsString,
					stylesToRemove, false, m_cache.LanguageWritingSystemFactoryAccessor);

				if (tssQuote == null || tssQuote.Length == 0)
					return null;

				string paraContent = tssQuote.Text;
				int offset = paraContent.IndexOf(quote);
				if (offset >= 0)
				{
					// offsets may be off slightly because of removed characters
					note.BeginOffset = offset;
					note.EndOffset = offset + quote.Length;
					return para;
				}
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds paragraph in the passed book that is at a similar structural to the given
		/// paragraph's location in its book.
		/// </summary>
		/// <param name="book">Current version of book</param>
		/// <param name="savedPara">Paragraph in saved archive of the book</param>
		/// <returns>
		/// Paragraph that is in a similar location to the passed paragraph if it exists;
		/// otherwise <c>null</c> will be returned.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private ICmObject FindCorrespondingParagraph(ScrBook book, ICmObject savedPara)
		{
			// make sure we really did find a paragraph
			if (!(savedPara is IStTxtPara))
				return null;

			int savedBook = Cache.GetOwnerOfObjectOfClass(savedPara.Hvo, ScrBook.kclsidScrBook);
			if (savedBook > 0)
			{
				int owningText = Cache.GetOwnerOfObject(savedPara.Hvo);
				int paraIndex = Cache.GetObjIndex(owningText, (int)StText.StTextTags.kflidParagraphs, savedPara.Hvo);
				IStText curText = null;
				int owningFlid = Cache.GetOwningFlidOfObject(owningText);
				if (owningFlid == (int)ScrBook.ScrBookTags.kflidTitle)
					curText = book.TitleOA;
				else if (owningFlid == (int) ScrSection.ScrSectionTags.kflidHeading ||
					owningFlid == (int) ScrSection.ScrSectionTags.kflidContent)
				{
					int owningSection = Cache.GetOwnerOfObject(owningText);
					int sectionIndex = Cache.GetObjIndex(savedBook, (int)ScrBook.ScrBookTags.kflidSections, owningSection);
					sectionIndex = Math.Min(sectionIndex, book.SectionsOS.Count - 1);
					IScrSection curSection = book.SectionsOS[sectionIndex];
					while (!curSection.IsIntro)
					{
						sectionIndex--;
						if (sectionIndex < 0)
							return null;
						curSection = book.SectionsOS[sectionIndex];
					}
					curText = (owningFlid == (int) ScrSection.ScrSectionTags.kflidHeading)
						? curSection.HeadingOA : curSection.ContentOA;
				}
				else
					return null;

				paraIndex = Math.Min(paraIndex, curText.ParagraphsOS.Count - 1);
				return curText.ParagraphsOS[paraIndex];
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds first paragraph of first introduction section (if it exists).
		/// </summary>
		/// <param name="book">Book to be searched</param>
		/// <returns>
		/// Paragraph if it exists; otherwise <c>null</c> will be returned.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private IStTxtPara FirstIntroParagraph(ScrBook book)
		{
			return book.FirstSection.IsIntro ? book.FirstSection.FirstContentParagraph : null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds first paragraph of the book title (has to exist, even if empty).
		/// </summary>
		/// <param name="book">Book to be searched</param>
		/// <returns>
		/// First paragraph of title.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private IStTxtPara FirstTitleParagraph(ScrBook book)
		{
			return (IStTxtPara)book.TitleOA.ParagraphsOS[0];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the import settings of the specified type. If the requested type is not found
		/// but import settings of unknown type are found, the unknown settings will be returned.
		/// </summary>
		/// <param name="importType">Type of the import.</param>
		/// <returns>
		/// The import set of the specified type, or an unknown type as a fallback;
		/// otherwise <c>null</c> will be returned.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IScrImportSet FindImportSettings(TypeOfImport importType)
		{
			IScrImportSet unknownSet = null;
			foreach (IScrImportSet importSet in ImportSettingsOC)
			{
				if (importSet.ImportTypeEnum == importType)
					return importSet;
				else if (importSet.ImportTypeEnum == TypeOfImport.Unknown)
					unknownSet = importSet;
			}

			// If we didn't find an import set of the type we were looking for but we found
			// unknown import settings...
			if (unknownSet != null)
			{
				// then the unknown settings have not been completely defined. They may be used
				// as the requested importType.
				unknownSet.ImportTypeEnum = importType;
				return unknownSet;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the specified book in the database
		/// </summary>
		/// <param name="sSilAbbrev">The 3-letter SIL abbreviation (all-caps) for the book
		/// </param>
		/// <returns>The specified book if it exists; otherwise, null</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook FindBook(string sSilAbbrev)
		{
			foreach (IScrBook book in ScriptureBooksOS)
			{
				if (book.BookId == sSilAbbrev)
					return book;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the specified book in the database based from the canonical order
		/// </summary>
		/// <param name="bookOrd">The canonical order</param>
		/// <returns>The specified book if it exists, otherwise null.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook FindBook(int bookOrd)
		{
			// TimS believes this is a lot faster than the
			// commented out loop below. So, we'll go with it.
			foreach (int bookHvo in ScriptureBooksOS.HvoArray)
			{
				if (Cache.GetIntProperty(bookHvo, (int)ScrBook.ScrBookTags.kflidCanonicalNum) == bookOrd)
					return new ScrBook(Cache, bookHvo);
			}

			//foreach (IScrBook book in ScriptureBooksOS)
			//{
			//    if (book.CanonicalNum == bookOrd)
			//        return book;
			//}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a header footer set by name
		/// </summary>
		/// <param name="name">Name of the desired HF set</param>
		/// <returns>The set if found; otherwise <c>null</c></returns>
		/// ------------------------------------------------------------------------------------
		public IPubHFSet FindNamedHfSet(string name)
		{
			foreach (IPubHFSet hfSet in HeaderFooterSetsOC)
			{
				if (hfSet.Name == name)
					return hfSet;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain a style object from a style name.
		/// </summary>
		/// <param name="styleName">Name of style to find</param>
		/// ------------------------------------------------------------------------------------
		public IStStyle FindStyle(string styleName)
		{
			foreach (int hvo in StylesOC.HvoArray)
			{
				// We assume the HVO of the style is valid if we find it in the HVO array
				IStStyle style = new StStyle(m_cache, hvo, false, false) as IStStyle;
				if (style.Name == styleName)
					return style;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain a style object from character properties.
		/// </summary>
		/// <param name="ttp">Properties containing a named style</param>
		/// ------------------------------------------------------------------------------------
		public IStStyle FindStyle(ITsTextProps ttp)
		{
			if (ttp == null)
				return null;
			string styleName =
				ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle);
			return FindStyle(styleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a saved version without adding books. Use AddBookToSavedVersion to add books.
		/// </summary>
		/// <remarks>Note that saved versions are called ScrDrafts in the database and used to
		/// called "archives" in the UI.</remarks>
		/// <param name="description">Description for the saved version</param>
		/// <returns>The new saved version</returns>
		/// ------------------------------------------------------------------------------------
		public IScrDraft CreateSavedVersion(string description)
		{
			IScrDraft draft = ArchivedDraftsOC.Add(new ScrDraft());
			draft.Description = description;
			return draft;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy a book (typically from an archive) to the current version. Caller should
		/// first ensure there is no book with that ID.
		/// </summary>
		/// <param name="book">The book to copy.</param>
		/// <exception cref="InvalidOperationException">Attempt to copy book to current version
		/// when that book already exists in the current version</exception>
		/// <returns>HVO of the copied bok</returns>
		/// ------------------------------------------------------------------------------------
		public int CopyBookToCurrent(IScrBook book)
		{
			if (FindBook(book.CanonicalNum) != null)
				throw new InvalidOperationException("Attempt to copy book to current version when that book already exists in the current version");
			// Find the first book whose canonical number is greater than the one we're copying
			// in. The copied book should be inserted before that one.
			int hvoDstStart = GetBookFollowing(ScriptureBooksOS, book.CanonicalNum);
			Logger.WriteEvent("Copying book " + book.CanonicalNum + " to current");
			int hvoCopiedBook = m_cache.CopyObject(book.Hvo, this.Hvo,
				(int)Scripture.ScriptureTags.kflidScriptureBooks, hvoDstStart);
			Logger.WriteEvent("Adjusting objects in " + book.CanonicalNum);
			AdjustObjectsInArchivedBook(book.Hvo, hvoCopiedBook);
			Logger.WriteEvent("Copying Free Translation for " + book.CanonicalNum);
			CopyFreeTranslations(book, CmObject.CreateFromDBObject(book.Cache, hvoCopiedBook) as ScrBook);
			Logger.WriteEvent("Copying book complete for " + book.CanonicalNum);

			return hvoCopiedBook;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book in the given sequence which follows the given book (identified by its
		/// canonical number).
		/// </summary>
		/// <param name="booksOs">The sequence of books in which to look.</param>
		/// <param name="nCanonicalBookNum">The n canonical book num.</param>
		/// <returns>The HVO of the following book; or -1 if no following book is found</returns>
		/// ------------------------------------------------------------------------------------
		private int GetBookFollowing(FdoOwningSequence<IScrBook> booksOs, int nCanonicalBookNum)
		{
			foreach (IScrBook existingBook in booksOs)
			{
				if (existingBook.CanonicalNum > nCanonicalBookNum)
				{
					return existingBook.Hvo;
				}
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add book to the specified saved version.
		/// </summary>
		/// <param name="version">The saved version</param>
		/// <param name="hvoBook">HVO of book to add</param>
		/// <returns>The HVO of the saved version of the book</returns>
		/// ------------------------------------------------------------------------------------
		public int AddBookToSavedVersion(IScrDraft version, int hvoBook)
		{
			return AddBookToSavedVersion(version, new ScrBook(Cache, hvoBook));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add book to the specified saved version.
		/// </summary>
		/// <param name="version">The saved version</param>
		/// <param name="book">book to add</param>
		/// <exception cref="InvalidOperationException">Saved version already contains a copy of
		/// the specified book</exception>
		/// <returns>The HVO of the saved version of the book</returns>
		/// ------------------------------------------------------------------------------------
		public int AddBookToSavedVersion(IScrDraft version, IScrBook book)
		{
			Debug.Assert(version != null);
			if (version.FindBook(book.CanonicalNum) != null)
			{
				throw new InvalidOperationException("Saved version already contains a copy of " +
					book.BookId);
			}
			// Find the first book whose canonical number is greater than the one we're copying
			// in. The copied book should be inserted before that one.
			int hvoDstStart = GetBookFollowing(version.BooksOS, book.CanonicalNum);
			Logger.WriteEvent("Copying book " + book.CanonicalNum + " to saved version");
			int hvoArchivedBook = m_cache.CopyObject(book.Hvo, version.Hvo,
				(int)ScrDraft.ScrDraftTags.kflidBooks, hvoDstStart);

			Logger.WriteEvent("Adjusting objects in book " + book.CanonicalNum);
			AdjustObjectsInArchivedBook(book.Hvo, hvoArchivedBook);
			Logger.WriteEvent("Copying Free Translation for book " + book.CanonicalNum);
			CopyFreeTranslations(book, CmObject.CreateFromDBObject(book.Cache, hvoArchivedBook) as ScrBook);
			Logger.WriteEvent("Completed copy for " + book.CanonicalNum);
			return hvoArchivedBook;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the footnotes in the archived book.
		/// </summary>
		/// <param name="hvoBook"></param>
		/// <param name="hvoArchivedBook"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void AdjustObjectsInArchivedBook(int hvoBook, int hvoArchivedBook)
		{
			if (m_cache.DatabaseAccessor == null)
				return;	// Can happen in tests

			// Now we need to re-hook up the footnotes. Oh, joy!
			bool fIsNull;
			uint cbSpaceTaken;

			// First we need to retrieve the mapping info to be able to go from existing footnote
			// GUIDs in the active book to archived footnote GUIDs.
			string sSql;
			List<Guid> bookFootnotes = new List<Guid>();
			List<Guid> revFootnotes = new List<Guid>();
			IOleDbCommand odc = null;

			try
			{
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				sSql = string.Format("exec GetNewFootnoteGuids {0}, {1}", hvoBook, hvoArchivedBook);
				odc.ExecCommand(sSql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);

				while (fMoreRows)
				{
					using (ArrayPtr rgGuid = MarshalEx.ArrayToNative(1, typeof(Guid)))
					{
						odc.GetColValue(1, rgGuid, rgGuid.Size, out cbSpaceTaken, out fIsNull, 0);
						if (fIsNull)
							throw new Exception("Unable to archive draft -- Null guid for footnote");
						Guid[] guids = (Guid[])MarshalEx.NativeToArray(rgGuid, 1, typeof(Guid));
						bookFootnotes.Add(guids[0]);
						odc.GetColValue(2, rgGuid, rgGuid.Size, out cbSpaceTaken, out fIsNull, 0);
						if (fIsNull)
							throw new Exception("Unable to archive draft -- Null guid for footnote");
						guids = (Guid[])MarshalEx.NativeToArray(rgGuid, 1, typeof(Guid));
						revFootnotes.Add(guids[0]);
						odc.NextRow(out fMoreRows);
					}
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc); // Has to be done, before the call to DbOps.ReadIntsFromCommand;
			}

			// Now we get a list of all the archived paragraphs that have ORCs in them, and
			// hunt for the footnotes and pictures.
			sSql = string.Format("exec GetParasWithORCs {0}", hvoArchivedBook);
			List<int> rghvoRevParas = DbOps.ReadIntsFromCommand(m_cache, sSql, null);

			ITsString tss;
			ITsStrBldr strBldr;
			ITsTextProps ttp;
			ITsPropsBldr propsBldr;
			int iFootnote = 0;
			foreach (int hvoRevPara in rghvoRevParas)
			{
				//TODO: TE- 5082 Duplicate code! The following should call or use common code with
				// StTxtPara.CreateOwnedObjects()
				bool fChanged = false;
				StTxtPara revPara = new StTxtPara(m_cache, hvoRevPara);
				tss = revPara.Contents.UnderlyingTsString;
				strBldr = tss.GetBldr();
				int cRun = tss.RunCount;
				TsRunInfo tri;
				byte[] objData;
				for (int iRun = 0; iRun < cRun; iRun++)
				{
					// Examine this run to see if it is an owned ORC
					FwObjDataTypes odt;
					Guid guid = StringUtils.GetOwnedGuidFromRun(tss, iRun, out odt, out tri, out ttp);
					if (guid != Guid.Empty)
					{
						switch (odt)
						{
							case FwObjDataTypes.kodtOwnNameGuidHot:
								// Probably a footnote. Make sure it's the next one in the
								// original sequence.
								if (guid == bookFootnotes[iFootnote])
								{
									// adjust the owned ORC
									objData = MiscUtils.GetObjData(revFootnotes[iFootnote],
										(byte)FwObjDataTypes.kodtOwnNameGuidHot);
									propsBldr = ttp.GetBldr();
									propsBldr.SetStrPropValueRgch(
										(int)FwTextPropType.ktptObjData,
										objData, objData.Length);
									strBldr.SetProperties(tri.ichMin, tri.ichLim,
										propsBldr.GetTextProps());
									// Look thru back translation and adjust ref ORCs for this footnote there
									AdjustBtFootnoteInArchivedBook(bookFootnotes[iFootnote],
										revFootnotes[iFootnote], revPara);
									fChanged = true;
									iFootnote++;
								}
								else if (bookFootnotes.Contains(guid))
								{
									// This footnote is out of order.
									Debug.Assert(false, "Fix GetParasWithORCs to return the paras in order");
								}
								else
								{
									// This footnote is not in the database
									Debug.Assert(false, "No footnote object owned by this book with guid: " +
										guid.ToString());
								}
								break;
							case FwObjDataTypes.kodtGuidMoveableObjDisp:
								// Get the original picture info
								int picHvo = m_cache.GetIdFromGuid(guid);
								CmPicture picture = new CmPicture(m_cache, picHvo);

								// Copy the picture and the file
								int newPicHvo = m_cache.CopyObject(picHvo, 0, 0);
								int newFileHvo = m_cache.CopyObject(picture.PictureFileRAHvo,
									m_cache.LangProject.Hvo,
									(int)LangProject.LangProjectTags.kflidPictures);

								//update the new picture info
								CmPicture newPicture = new CmPicture(m_cache, newPicHvo);
								newPicture.PictureFileRAHvo = newFileHvo;

								// update the ORC in the revision to point to the new picture
								Guid newPicGuid = m_cache.GetGuidFromId(newPicHvo);
								objData = MiscUtils.GetObjData(newPicGuid,
									(byte)FwObjDataTypes.kodtGuidMoveableObjDisp);
								propsBldr = ttp.GetBldr();
								propsBldr.SetStrPropValueRgch(
									(int)FwTextPropType.ktptObjData,
									objData, objData.Length);
								strBldr.SetProperties(tri.ichMin, tri.ichLim,
									propsBldr.GetTextProps());
								fChanged = true;
								break;
							default:
								throw new Exception("Found an unexpected kind of ORC");
						}
					}
				}
				if (fChanged)
				{
					// Save the new TS string
					// (but prevent side effects from ChangeWatchers)
					using (new IgnorePropChanged(m_cache, PropChangedHandling.SuppressChangeWatcher))
					{
						revPara.Contents.UnderlyingTsString = strBldr.GetString();
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the back translation of footnotes to reference the newly-created guid in the
		/// archive.
		/// </summary>
		/// <param name="origGuid">guid for footnote in original book</param>
		/// <param name="newGuid">guid for footnote in archive</param>
		/// <param name="para">paragraph (potentially containing a translation)</param>
		/// ------------------------------------------------------------------------------------
		private void AdjustBtFootnoteInArchivedBook(Guid origGuid, Guid newGuid, StTxtPara para)
		{
			//TODO: TE- 5082 Duplicate code! Should call StTxtPara.UpdateOrcsInTranslations(), or common code
			ICmTranslation trans = para.GetBT();

			if (trans != null)
			{
				// Check the back translation for each writing system.
				foreach (ILgWritingSystem ws in m_cache.LangProject.AnalysisWssRC)
				{
					ITsString btTss = trans.Translation.GetAlternative(ws.Hvo).UnderlyingTsString;
					if (btTss.RunCount > 0)
					{
						// Scan through runs searching for a reference guid with the original guid.
						int cRuns = btTss.RunCount;
						TsRunInfo tri;
						ITsTextProps ttp;
						for (int iRun = 0; iRun < cRuns; iRun++)
						{
							Guid guid = StringUtils.GetGuidFromRun(btTss, iRun, out tri, out ttp);

							if (guid != Guid.Empty && guid == origGuid)
							{
								// Guid mapping back to orignal draft found. Update it.
								byte[] objData;
								objData = MiscUtils.GetObjData(newGuid,
									(byte)FwObjDataTypes.kodtNameGuidHot);
								ITsPropsBldr propsBldr;
								propsBldr = ttp.GetBldr();
								propsBldr.SetStrPropValueRgch(
									(int)FwTextPropType.ktptObjData,
									objData, objData.Length);
								ITsStrBldr btTssBldr = btTss.GetBldr();
								btTssBldr.SetProperties(tri.ichMin, tri.ichLim,
									propsBldr.GetTextProps());
								// Set the translation alt string to the new value
								// (but prevent side effects from ChangeWatchers)
								using (new IgnorePropChanged(m_cache, PropChangedHandling.SuppressChangeWatcher))
								{
									trans.Translation.SetAlternative(btTssBldr.GetString(), ws.Hvo);
								}
								break;
							}
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a saved version,adding the list of books as specified.
		/// </summary>
		/// <remarks>Note that saved versions are called ScrDrafts in the database and used to
		/// called "archives" in the UI.</remarks>
		/// <param name="description">Description for the saved version</param>
		/// <param name="hvoBooks">Books that are copied to the saved version</param>
		/// <returns>The new saved version</returns>
		/// ------------------------------------------------------------------------------------
		public IScrDraft CreateSavedVersion(string description, int[] hvoBooks)
		{
			IScrDraft draft = ArchivedDraftsOC.Add(new ScrDraft());
			draft.Description = description;

			foreach (int hvoBook in hvoBooks)
			{
				AddBookToSavedVersion(draft, hvoBook);
			}
			return draft;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts an integer into a string of digits using the zero digit defined for
		/// Scripture.
		/// </summary>
		/// <param name="nValue">Value to be converted</param>
		/// <returns>Converted string for value</returns>
		/// ------------------------------------------------------------------------------------
		public string ConvertToString(int nValue)
		{
			return ConvertToString(nValue, UseScriptDigits);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts an integer into a string of digits using the zero digit defined for
		/// Scripture.
		/// </summary>
		/// <param name="nValue">Value to be converted</param>
		/// <param name="fUseScriptDigits">true use the current script digits in scripture to
		/// convert the digits to a script digits, false otherwise.</param>
		/// <returns>Converted string for value</returns>
		/// ------------------------------------------------------------------------------------
		public string ConvertToString(int nValue, bool fUseScriptDigits)
		{
			if (fUseScriptDigits)
				return ConvertToString(nValue, (char)ScriptDigitZero);
			return ConvertToString(nValue, '0');
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a Scripture reference to a string (like this: 16:3).  It will also
		/// handle script digits if necessary.
		/// </summary>
		/// <param name="reference">A ScrReference that holds the chapter and verse to format</param>
		/// <returns>A formatted chapter/verse reference</returns>
		/// <remarks>See TeHeaderFooterVc for an example of how to format the full reference,
		/// including book name. The method BookName in that class could be moved somewhere
		/// more appropriate if it's ever needed.</remarks>
		/// ------------------------------------------------------------------------------------
		public string ChapterVerseRefAsString(BCVRef reference)
		{
			return ChapterVerseRefAsString(reference, m_cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a Scripture reference to a string (perhaps like this: 16:3, depending on the
		/// chapterVerseSeparator).  It will also handle script digits if necessary.
		/// </summary>
		/// <param name="reference">A ScrReference that holds the chapter and verse to format</param>
		/// <param name="hvoWs">The HVO of the writing system in which we are displaying this
		/// reference (used to determine whether to use script digits and whether to include
		/// right-to-left marks)</param>
		/// <returns>A formatted chapter/verse reference</returns>
		/// <remarks>See TeHeaderFooterVc for an example of how to format the full reference,
		/// including book name. The method BookName in that class could be moved somewhere
		/// more appropriate if it's ever needed.</remarks>
		/// ------------------------------------------------------------------------------------
		public string ChapterVerseRefAsString(BCVRef reference, int hvoWs)
		{
			bool fUseScriptDigits = (UseScriptDigits && hvoWs == Cache.DefaultVernWs);
			string sVerseRef = (reference.Verse > 0) ?
				ConvertToString(reference.Verse, fUseScriptDigits) : String.Empty;

			if (reference.Chapter > 0 && reference.Verse > 0)
			{
				return ConvertToString(reference.Chapter, fUseScriptDigits) +
					ChapterVerseSeparatorForWs(hvoWs) + sVerseRef;
			}
			return sVerseRef;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the given StText belongs to scripture or not.
		/// </summary>
		/// <param name="stText"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public bool IsResponsibleFor(StText stText)
		{
			if (stText == null)
				return false;
			int owningFlid = stText.OwningFlid;
			return IsScriptureTextFlid(owningFlid);
		}

		/// <summary>
		/// Given the flid that owns an StText, answer true if it is part of Scripture.
		/// </summary>
		/// <param name="owningFlid"></param>
		/// <returns></returns>
		public static bool IsScriptureTextFlid(int owningFlid)
		{
			return owningFlid == (int)ScrSection.ScrSectionTags.kflidHeading ||
				   owningFlid == (int)ScrBook.ScrBookTags.kflidFootnotes ||
				   owningFlid == (int)ScrSection.ScrSectionTags.kflidContent ||
				   owningFlid == (int)FDO.Scripture.ScrBook.ScrBookTags.kflidTitle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the chapter verse bridge for a given section, formatted appropriately for the UI
		/// writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ChapterVerseBridgeAsString(IScrSection section)
		{
			if (section.IsIntro)
			{
				return String.Format(ResourceHelper.GetResourceString("kstidScriptureSectionIntroduction"), section.OwnOrd);
			}
			ScrReference startRef = new ScrReference(section.VerseRefStart, Versification);
			ScrReference endRef = new ScrReference(section.VerseRefEnd, Versification);
			if (startRef.Chapter != endRef.Chapter)
			{
				return MakeBridgeAsString(ChapterVerseRefAsString(startRef, m_cache.DefaultUserWs),
					ChapterVerseRefAsString(endRef, m_cache.DefaultUserWs), m_cache.DefaultUserWs);
			}
			else
				return ChapterVerseBridgeAsString(startRef, endRef, m_cache.DefaultUserWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Chapter Verse Reference representation for a footnote (ie. sectionCV-CV Footnote(footnoteCV))
		/// Or Title representation (ie. Title Footnote(OwnOrd))
		/// </summary>
		/// <param name="footnote"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string ContainingRefAsString(ScrFootnote footnote)
		{
			footnote.IgnoreDisplaySettings = true; // so we can access GetReference.
			string parentContext = "";
			string footnoteRef = "";
			int hvoContainingObj;
			if (footnote.TryGetContainingSectionHvo(out hvoContainingObj))
			{
				ScrSection section = new ScrSection(Cache, hvoContainingObj);
				parentContext = ChapterVerseBridgeAsString(section);
				footnoteRef = footnote.GetReference(m_cache.DefaultUserWs).Trim();
			}
			else if (footnote.TryGetContainingTitle(out hvoContainingObj))
			{
				parentContext = ResourceHelper.GetResourceString("kstidScriptureTitle");
				footnoteRef = footnote.OwnOrd.ToString();
			}
			return String.Format("{0} {1}({2})", parentContext,
				ResourceHelper.GetResourceString("kstidScriptureFootnote"), footnoteRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get full reference (BCV-CV) of section.
		/// </summary>
		/// <param name="section"></param>
		/// <param name="ws">writing system of the book</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString BookChapterVerseBridgeAsTss(IScrSection section, int ws)
		{
			ITsString tssBookName = GetBookName(section, ws);
			if (tssBookName.Length > 0)
			{
				ITsStrBldr bldr = tssBookName.GetBldr();
				int cch = bldr.Length;
				bldr.Replace(cch, cch, " " + ChapterVerseBridgeAsString(section), null);
				return bldr.GetString();
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the book.
		/// </summary>
		/// <param name="section">The section.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static ITsString GetBookName(IScrSection section, int ws)
		{
			ScrBook book = (section as ScrSection).OwningBook;
			ITsString tssBookName = book.Name.GetAlternativeTss(ws);
			return tssBookName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get full reference (BCV-CV) of the given StText. Returns an empty string if we can't find a book name
		/// with the given 'ws'.
		/// </summary>
		/// <param name="stText"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString BookChapterVerseBridgeAsTss(StText stText, int ws)
		{
			ITsString tssTitle = null;
			Debug.Assert(Scripture.IsResponsibleFor(stText), "given StText should belong to scripture.");
			int owningFlid = stText.OwningFlid;
			if (owningFlid == (int)ScrBook.ScrBookTags.kflidFootnotes)
			{
				ScrFootnote footnote = new ScrFootnote(Cache, stText.Hvo);
				string footnoteRefStr = ContainingRefAsString(footnote);
				ScrBook book = footnote.Owner as ScrBook;
				ITsString tssBook = book.Name.GetAlternativeTss(ws);
				if (tssBook.Length > 0)
				{
					ITsStrBldr bldr = tssBook.GetBldr();
					int cch = bldr.Length;
					bldr.Replace(cch, cch, " " + footnoteRefStr, null);
					tssTitle = bldr.GetString();
				}
			}
			else if (owningFlid == (int)ScrSection.ScrSectionTags.kflidContent ||
				owningFlid == (int)ScrSection.ScrSectionTags.kflidHeading)
			{
				ScrSection section = stText.Owner as ScrSection;

				ITsString tssBookChapterVerseBridge = this.BookChapterVerseBridgeAsTss(section, ws);
				tssTitle = tssBookChapterVerseBridge;
			}
			else if (owningFlid == (int)FDO.Scripture.ScrBook.ScrBookTags.kflidTitle)
			{
				ScrBook book = stText.Owner as ScrBook;
				ITsString tssBookName = book.Name.GetAlternativeTss(ws);
				if (tssBookName.Length > 0)
				{
					ITsStrBldr bldr = tssBookName.GetBldr();
					int cch = bldr.Length;
					bldr.Replace(cch, cch, String.Format(" ({0})",
						ResourceHelper.GetResourceString("kstidScriptureTitle")), null);
					tssTitle = bldr.GetString();
				}
			}
			else
			{
				// throw.
			}
			if (tssTitle == null)
			{
				// return an empty string.
				ITsStrFactory isf = TsStrFactoryClass.Create();
				tssTitle = isf.MakeString("", ws);
			}
			return tssTitle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert two Scripture references to a string (perhaps like this: 16:3-6), depending
		/// on the chapter/verse separator. It will also handle script digits if necessary.
		/// </summary>
		/// <param name="start">A BCVRef that holds the chapter and beginning verse to format
		/// </param>
		/// <param name="end">A BCVRef that holds the ending reference (if book and chapter
		/// don't match book and chapter of start ref, an argument exception is hurled</param>
		/// <param name="hvoWs">HVO of the writing system for which we are formatting this
		/// reference.</param>
		/// <returns>A formatted chapter/verse reference</returns>
		/// ------------------------------------------------------------------------------------
		public string ChapterVerseBridgeAsString(BCVRef start, BCVRef end, int hvoWs)
		{
			if (start.Book != end.Book)
				throw new ArgumentException("Books are different");
			if (start.Chapter != end.Chapter)
				throw new ArgumentException("Chapters are different");
			string sResult = ChapterVerseRefAsString(start, hvoWs);

			if (start.Verse != end.Verse)
			{
				bool fUseScriptDigits = (UseScriptDigits && hvoWs == Cache.DefaultVernWs);
				sResult = MakeBridgeAsString(sResult, ConvertToString(end.Verse, fUseScriptDigits), hvoWs);
			}

			return sResult;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Puts the bridge marker in between the two given strings. handles right to left markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string MakeBridgeAsString(string sStart, string sEnd, int hvoWs)
		{
			return sStart + BridgeForWs(hvoWs) + sEnd;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a paragraph with the specified style id, containing the specified verse number,
		/// if specified, and having the correct sequence number. Sets all appropriate state
		/// variables before returning.
		/// </summary>
		/// <param name="targetStyle">style of paragraph to find</param>
		/// <param name="targetRef">Reference to seek</param>
		/// <param name="iPara">0-based index of paragraph</param>
		/// <param name="iVernSection">0-based index of the section the corresponding
		/// vernacular paragraph is in. This will be 0 if no corresponding paragraph can be
		/// found.</param>
		/// <returns>The corresponding StTxtPara, or null if no matching para is found</returns>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara FindCorrespondingVernParaForSegment(IStStyle targetStyle,
			BCVRef targetRef, int iPara, ref int iVernSection)
		{
			Debug.Assert(targetRef.BookIsValid, "Invalid book number");
			IScrBook book = FindBook(targetRef.Book);
			if (book == null)
				return null;

			return (book as ScrBook).FindCorrespondingVernParaForSegment(targetStyle, targetRef,
				iPara, ref iVernSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loop through the runs of the given string until a verse or chapter number is
		/// found. Update the start and end reference with the found number.
		/// </summary>
		/// <param name="iStart">Index of run to start search</param>
		/// <param name="iLim">Index of run to end search.
		/// One exception: if the run at iLim-1 is a chapter, we will go one run further (at the
		/// iLim) to check for an adjacent verse number.</param>
		/// <param name="tss">The string</param>
		/// <param name="fIgnoreErrors"><c>true</c> to ignore runs with errors, <c>false</c>
		/// to throw an exception if we encounter a invalid chapter or verse number.</param>
		/// <param name="refStart">Start reference</param>
		/// <param name="refEnd">End reference</param>
		/// <param name="iNext">Index of run after last one processed (i.e. iCurr+1)</param>
		/// <exception cref="System.ArgumentException">Invalid chapter number</exception>
		/// ------------------------------------------------------------------------------------
		public static RefRunType GetNextRef(int iStart, int iLim, ITsString tss,
			bool fIgnoreErrors, ref ScrReference refStart, ref ScrReference refEnd, out int iNext)
		{
			Debug.Assert(iStart >= 0 && iStart < iLim);
			Debug.Assert(iLim <= tss.RunCount);

			// look at all of the text runs in this paragraph starting at
			int iRun = iStart;
			try
			{
				for (; iRun < iLim; iRun++)
				{
					TsRunInfo runInfo;
					ITsTextProps props = tss.FetchRunInfo(iRun, out runInfo);
					if (StStyle.IsStyle(props, ScrStyleNames.VerseNumber))
					{
						// for verse number runs, get the verse range and save it
						int startVerse, endVerse;
						ScrReference.VerseToInt(tss.get_RunText(iRun), out startVerse, out endVerse);
						if (startVerse <= 0)
						{
							if (fIgnoreErrors)
								continue;
							throw new InvalidVerseException(tss.get_RunText(iRun), runInfo);
						}
						else if (startVerse <= refStart.LastVerse)
						{
							refStart.Verse = startVerse;
							refEnd.Verse = Math.Min(refEnd.LastVerse, endVerse);
						}

						iNext = iRun + 1;
						return RefRunType.Verse;
					}
					else if (StStyle.IsStyle(props, ScrStyleNames.ChapterNumber))
					{
						int chapter = -1;
						try
						{
							// for chapter number runs, get the chapter number and save it
							chapter = ScrReference.ChapterToInt(tss.get_RunText(iRun));
						}
						catch (ArgumentException)
						{
							if (fIgnoreErrors)
								continue;
							throw new InvalidChapterException(tss.get_RunText(iRun), runInfo);
						}

						Debug.Assert(chapter > 0); // should have thrown exception in ScrReference.ChapterToInt
						// if chapter is valid for this book...
						if (chapter <= refStart.LastChapter)
						{
							refStart.Chapter = refEnd.Chapter = chapter;
							refStart.Verse = refEnd.Verse = 1; // implicit default
						}
						iNext = iRun + 1; // increment just beyond the chapter

						// Because we've found a chapter, check the very next run (if we can)
						// to see if it is a verse.
						if (iNext < tss.RunCount && iNext < iLim + 1) // it's ok to check at iLim in this special case
						{
							int dummy;
							ScrReference startTemp = new ScrReference(refStart);
							ScrReference endTemp = new ScrReference(refEnd);
							RefRunType nextItemType = GetNextRef(iNext, iNext + 1, tss, fIgnoreErrors,
								ref startTemp, ref endTemp, out dummy);
							// if it is a verse, update refStart/refEnd
							if (nextItemType == RefRunType.Verse)
							{
								refStart = startTemp;
								refEnd = endTemp;
								iNext++;
								return RefRunType.ChapterAndVerse;
							}
						}
						// Otherwise, since a verse didn't immediatly follow, verse 1 is implicit
						// so use it as the refStart/RefEnd
						return RefRunType.Chapter;
					}
				}

				iNext = iLim;
				return RefRunType.None;
			}
			catch
			{
				// Set iNext in case caller handles the exception. We don't want to deal
				// with same run again.
				iNext = iRun + 1;
				throw;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets whether the footnote reference should be displayed for footnotes of the
		/// given style.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <param name="value"><c>true</c> if the reference should be displayed; <c>false</c>
		/// otherwise</param>
		/// ------------------------------------------------------------------------------------
		public void SetDisplayFootnoteReference(string styleId, bool value)
		{
			CheckStyleIdParam(styleId);
			if (styleId == ScrStyleNames.CrossRefFootnoteParagraph)
				DisplayCrossRefReference = value;
			else
				DisplayFootnoteReference = value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the footnote reference should be displayed for footnotes of the
		/// given style.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <returns><c>true</c> if the reference should be displayed; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool GetDisplayFootnoteReference(string styleId)
		{
			CheckStyleIdParam(styleId);
			return (styleId == ScrStyleNames.CrossRefFootnoteParagraph) ?
				DisplayCrossRefReference : DisplayFootnoteReference;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the footnote marker should be displayed in the footnote pane for
		/// footnotes of the given style.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph)</param>
		/// <returns><c>true</c> if the marker should be displayed; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool GetDisplayFootnoteMarker(string styleId)
		{
			CheckStyleIdParam(styleId);
			return (styleId == ScrStyleNames.CrossRefFootnoteParagraph) ?
				DisplayCrossRefMarker : DisplayFootnoteMarker;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote marker for footnotes of the given style.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <param name="footnoteMarker">TS String for the actual footnote whose marker is being
		/// computed. This will be used if this type of footnote is supposed to
		/// be a sequence.</param>
		/// <returns>A TsString with the marker to display</returns>
		/// ------------------------------------------------------------------------------------
		public ITsString GetFootnoteMarker(string styleId, ITsString footnoteMarker)
		{
			if (DetermineFootnoteMarkerType(styleId) == FootnoteMarkerTypes.AutoFootnoteMarker)
				return footnoteMarker;
			ITsStrBldr bldr = footnoteMarker.GetBldr();
			bldr.Replace(0, bldr.Length, GetFootnoteMarker(styleId), null);
			return bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote marker for footnotes of the given style. (Note that for
		/// auto-sequence footnotes, this will just be an "a", not the correct marker from the
		/// sequence.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <returns>A string with the marker</returns>
		/// ------------------------------------------------------------------------------------
		public string GetFootnoteMarker(string styleId)
		{
			return (styleId == ScrStyleNames.CrossRefFootnoteParagraph) ? CrossRefMarker : GeneralFootnoteMarker;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines footnote marker symbol to use for footnotes of the given style. Does not
		/// try to take into account whether the symbolic footnote type is in use.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <returns>The footnote marker symbol to be used for the given style</returns>
		/// ------------------------------------------------------------------------------------
		public string DetermineFootnoteMarkerSymbol(string styleId)
		{
			CheckStyleIdParam(styleId);
			return (styleId == ScrStyleNames.CrossRefFootnoteParagraph) ?
				CrossRefMarkerSymbol : FootnoteMarkerSymbol;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines type of footnote marker to use for footnotes of the given style.
		/// </summary>
		/// <param name="styleId">Name of style (typically either Note General Paragraph or
		/// Cross-Reference Paragraph</param>
		/// <returns>The type of footnote marker to be used for the given style</returns>
		/// ------------------------------------------------------------------------------------
		public FootnoteMarkerTypes DetermineFootnoteMarkerType(string styleId)
		{
			CheckStyleIdParam(styleId);
			return (styleId == ScrStyleNames.CrossRefFootnoteParagraph) ?
				CrossRefMarkerType : FootnoteMarkerType;
		}
	   /// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert from a vernacular verse or chapter number to the kind that should be
		/// displayed in back translations, as configured for this Scripture object.
		/// </summary>
		/// <param name="vernVerseChapterText">Verse or chapter number text. This may be
		/// <c>null</c>.</param>
		/// <returns>The converted verse or chapter number, or empty string if
		/// <paramref name="vernVerseChapterText"/> is <c>null</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public string ConvertVerseChapterNumForBT(string vernVerseChapterText)
		{
			if (vernVerseChapterText == null)
				return string.Empty; // empty verse/chapter number run.

			StringBuilder btVerseChapterText = new StringBuilder(vernVerseChapterText.Length);
			char baseChar = UseScriptDigits ? (char)ScriptDigitZero : '0';
			for (int i = 0; i < vernVerseChapterText.Length; i++)
			{
				if (char.IsDigit(vernVerseChapterText[i]))
				{
					btVerseChapterText.Append((char)('0' + (vernVerseChapterText[i] - baseChar)));
				}
				else
				{
					btVerseChapterText.Append(vernVerseChapterText[i]);
				}
			}
			return btVerseChapterText.ToString();
		}

		/// <summary>
		/// Return a TsString in which any CV numbers have been replaced by their BT equivalents
		/// (in the specified writing system). Other properties, including style, are copied
		/// from the input numbers.
		/// </summary>
		public ITsString ConvertCVNumbersInStringForBT(ITsString input, int wsTrans)
		{
			ITsStrBldr bldr = null;
			// reverse order so we don't mess up offsets if new numbers differ in length.
			for (int iRun = input.RunCount - 1; iRun >= 0; iRun--)
			{
				string styleName = input.get_Properties(iRun).GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle);

				if (styleName == ScrStyleNames.ChapterNumber ||
					styleName == ScrStyleNames.VerseNumber)
				{
					string number = ConvertVerseChapterNumForBT(input.get_RunText(iRun));
					if (number == null)
						continue; // pathologically an empty string has verse number style??
					if (bldr == null)
						bldr = input.GetBldr(); // we have an actual change.
					int ichMin, ichLim;
					input.GetBoundsOfRun(iRun, out ichMin, out ichLim);
					bldr.SetIntPropValues(ichMin, ichLim, (int)FwTextPropType.ktptWs,
						(int)FwTextPropVar.ktpvDefault, wsTrans);
					bldr.Replace(ichMin, ichLim, number, null);
				}
			}
			if (bldr != null)
				return bldr.GetString();
			return input;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string that can be used to format a verse bridge for the given writing
		/// system, including the right-to-left marks if needed.
		/// </summary>
		/// <param name="hvoWs">HVO of the writing system. If 0 or -1, the default vernacular
		/// will be used.</param>
		/// ------------------------------------------------------------------------------------
		public string BridgeForWs(int hvoWs)
		{
			return FormatReferencePunctForWs(Bridge, hvoWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string that can be used to format a chapter-verse separator for the given
		/// writing system, including the right-to-left marks if needed.
		/// </summary>
		/// <param name="hvoWs">HVO of the writing system. If 0 or -1, the default vernacular
		/// will be used.</param>
		/// ------------------------------------------------------------------------------------
		public string ChapterVerseSeparatorForWs(int hvoWs)
		{
			return FormatReferencePunctForWs(ChapterVerseSepr, hvoWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string that can be used to format a verse separator for the given
		/// writing system, including the right-to-left marks if needed.
		/// </summary>
		/// <param name="hvoWs">HVO of the writing system. If 0 or -1, the default vernacular
		/// will be used.</param>
		/// ------------------------------------------------------------------------------------
		public string VerseSeparatorForWs(int hvoWs)
		{
			return FormatReferencePunctForWs(VerseSepr, hvoWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string that can be used to format the given Scripture-reference punctuation
		/// for the given writing system by wrapping it in right-to-left marks if needed.
		/// </summary>
		/// <param name="punct">The Scripture-reference punctuation mark, which is usually a
		/// single character.</param>
		/// <param name="hvoWs">HVO of the writing system. If 0 or -1, the default vernacular
		/// will be used.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string FormatReferencePunctForWs(string punct, int hvoWs)
		{
			if (hvoWs <= 0)
				hvoWs = Cache.DefaultVernWs;
			LgWritingSystem lgws = new LgWritingSystem(m_cache, hvoWs);
			return (lgws.RightToLeft) ? "\u200f" + punct + "\u200f" : punct;
		}
		#endregion

		#region private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts an integer into a string of digits using the given base as the zero digit.
		/// </summary>
		/// <param name="nValue">Value to be converted</param>
		/// <param name="baseDigit">Equivalent of "0" for number system</param>
		/// <returns>Converted string for value</returns>
		/// ------------------------------------------------------------------------------------
		private string ConvertToString(int nValue, char baseDigit)
		{
			if (nValue == 0)
				return new String(baseDigit, 1);

			StringBuilder tempString = new StringBuilder();
			int remainder = nValue;
			while (remainder > 0)
			{
				int digit = remainder % 10;
				remainder /= 10;
				tempString.Insert(0, (char)(baseDigit + digit));
			}

			return tempString.ToString();
		}

		/// <summary>
		/// Copy the free translations from the source to the destination. This is designed to be used on the
		/// output of CopyObject. (It will not remove or merge any existing Segments or FTs, and assumes
		/// the structure of the two books matches exactly.)
		/// Enhance JohnT: we could fairly easily copy literal translations, notes, Wfics as well.
		/// 1. sql1 should allow the additional annotation type(s).
		/// 2. for Wfic, sql1 should retrieve instanceOf
		/// 3. for Wfic, Code should copy InstanceOf as well as other stuff to new annotations.
		/// 4. For literal translations and notes, sql2 and 3 need to include additional annotation types.
		/// For Wfics, a complication is that the instanceOf target for a saved version Wfic may possibly
		/// not be guaranteed to be preserved. We should probably not copy any Wfic which has no instanceOf.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="dest"></param>
		void CopyFreeTranslations(IScrBook source, IScrBook dest)
		{
			FdoCache cache = source.Cache;
			int segmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnTextSegment);
			int ftSegmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
			if (segmentDefn == 0 || ftSegmentDefn == 0)
				return; // Running tests without enough structure to do this.

			// Build a dictionary mapping corresponding paragraphs in the original and copy.
			Dictionary<int, int> paraMap = new Dictionary<int, int>();
			for (int isec = 0; isec < source.SectionsOS.Count; isec++)
			{
				IScrSection sourceSec = source.SectionsOS[isec];
				IScrSection destSec = dest.SectionsOS[isec];
				MapText(sourceSec.ContentOA, destSec.ContentOA, paraMap);
				MapText(sourceSec.HeadingOA, destSec.HeadingOA, paraMap);
			}
			for (int ifn = 0; ifn < source.FootnotesOS.Count; ++ifn)
			{
				IStFootnote sourceFn = source.FootnotesOS[ifn];
				IStFootnote destFn = dest.FootnotesOS[ifn];
				MapText(sourceFn, destFn, paraMap);
			}
			MapText(source.TitleOA, dest.TitleOA, paraMap);

			// For every segment pointing to a title or footnote paragraph that matches, the query returns
			// 2 * the number of sections identical results.  Thus, DISTINCT is needed to create the proper
			// set of copies of the segments.
			string sql1 =
				@"select DISTINCT seg.id, seg.BeginOffset, seg.EndOffset, seg.BeginObject, seg.WritingSystem
				from ScrBook book
				join CmObject sec on sec.Owner$ = book.id
				join CmObject txt_fn on txt_fn.Owner$ = sec.id or txt_fn.Owner$ = book.id
				join CmObject para on para.Owner$ = txt_fn.id
				join CmBaseAnnotation_ seg on seg.BeginObject = para.id and seg.AnnotationType = " + segmentDefn + @"
				where book.id = " + source.Hvo;
			List<int[]> segInfo = DbOps.ReadIntArray(cache, sql1, null, 5);
			// Map the original segment HVOs onto the copies.
			Dictionary<int, int> segMap = new Dictionary<int, int>(segInfo.Count);

			Logger.WriteEvent("Creating base annotations");
			foreach (int[] info in segInfo)
			{
				int hvoPara = paraMap[info[3]];
				// REVIEW (SteveMiller): Don't we want the CmBaseAnnotation.Flid, too?
				string sql4 =
					@"DECLARE @id INT;
					INSERT INTO CmObject (Class$) VALUES (37);
					SET @Id = @@IDENTITY;
					INSERT INTO CmAnnotation (Id, AnnotationType) VALUES (@Id, " +
					segmentDefn + @");
					INSERT INTO CmBaseAnnotation
						(Id, BeginOffset, EndOffset, BeginObject, EndObject, WritingSystem)
						VALUES (@Id, " + info[1] + @", " + info[2] + @", " +
									   hvoPara + @", " + hvoPara + @", " + (info[4] == 0 ? "null" : info[4].ToString()) + @");
					SELECT @id;";
				int hvo;
				if (DbOps.ReadOneIntFromCommand(cache, sql4, null, out hvo))
					segMap[info[0]] = hvo;
			}
			/*
			 * JT: "In revision 12, you moved the actual copy loop into SQL. Unfortunately, in the
			 * process, the new code simply copies the BeginObject from the old segment to the new.
			 * It is supposed to point the new segment to the corresponding paragraph in
			 * the copy, as looked up in paraMap. The result is to duplicate the segments on the
			 * old book's paragraphs instead of copying them to the new.
			string sql4 =
				@"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE -- so we're the only ones inserting
				DECLARE @Id INT, @BeginOffset INT, @Flid INT, @EndOffset INT,
					@BeginObject INT, @EndObject INT, @WritingSystem INT, @ClassId INT;
				SELECT @ClassId = Id FROM Class$ WHERE Name = 'CmBaseAnnotation';
				DECLARE curBA CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
					select DISTINCT seg.BeginOffset, seg.Flid, seg.EndOffset,
						seg.BeginObject, seg.EndObject, seg.WritingSystem
					from ScrBook book
					join CmObject sec on sec.Owner$ = book.id
					join CmObject txt_fn on txt_fn.Owner$ = sec.id or txt_fn.Owner$ = book.id
					join CmObject para on para.Owner$ = txt_fn.id
					join CmBaseAnnotation_ seg on seg.BeginObject = para.id
						and seg.AnnotationType = " + segmentDefn + @"
					where book.id = " + source.Hvo + @"
				OPEN curBA;
				FETCH curBA INTO @BeginOffset, @Flid, @EndOffset, @BeginObject, @EndObject, @WritingSystem;
				WHILE @@FETCH_STATUS = 0 BEGIN
					INSERT INTO CmObject (Class$) VALUES (@ClassId);
					SET @Id = @@IDENTITY;
					INSERT INTO CmAnnotation (Id, AnnotationType)
						VALUES (@Id, " + segmentDefn + @");
					INSERT INTO CmBaseAnnotation
						(Id, BeginOffset, Flid, EndOffset, BeginObject, EndObject, WritingSystem)
						VALUES
						(@Id, @BeginOffset, @Flid, @EndOffset, @BeginObject, @EndObject, @WritingSystem);
					FETCH curBA INTO @BeginOffset, @Flid, @EndOffset, @BeginObject, @EndObject, @WritingSystem;
				END
				CLOSE curBA;
				DEALLOCATE curBA;
				SET TRANSACTION ISOLATION LEVEL READ COMMITTED";
			DbOps.ExecuteStatementNoResults(cache, sql4, null);
			*/

			Logger.WriteEvent("Getting WS in use in BT's");
			// Get a list of all the writing systems we care about (all the ones that have any data for the interesting FT annotations).
			string sql2 =
				@"select DISTINCT com.ws
				from ScrBook book
				join CmObject sec on sec.Owner$ = book.id
				join CmObject txt_fn on txt_fn.Owner$ = sec.id or txt_fn.Owner$ = book.id
				join CmObject para on para.Owner$ = txt_fn.id
				join CmBaseAnnotation_ seg on seg.BeginObject = para.id and seg.AnnotationType = " + segmentDefn + @"
				join CmIndirectAnnotation_AppliesTo ft_at on ft_at.dst = seg.id
				join CmIndirectAnnotation_ ft on ft.id = ft_at.src and ft.AnnotationType = " + ftSegmentDefn + @"
				join CmAnnotation_Comment com on ft.id = com.obj
				where book.id = " + source.Hvo + @"
				group by com.ws";

			int[] wss = DbOps.ReadIntArrayFromCommand(cache, sql2, null);
			const int kflidComment = (int)CmAnnotation.CmAnnotationTags.kflidComment;

			Dictionary<int, int> ftMap = new Dictionary<int, int>();
			foreach (int ws in wss)
			{
				Logger.WriteEvent("Starting to copy for WS = " + ws);
				// For performance, and to populate the free translation virtual property, preload all the
				// free translations of the source text.
				// DISTINCT would be useful here to minimize db access traffic and duplicated insertions
				// into the cache, but isn't allowed because com.txt is NTEXT and com.fmt is IMAGE.
				// An alternative would be to split it into two queries, one for the sections and the
				// other for the title and footnotes, but then we'd have two round trips to the database,
				// which might be slower overall. (UNION won't work to combine the two queries because it
				// implies DISTINCT!)
				// The duplicate results don't cause errors here, because they're all written to the cache,
				// which replaces one copy with another without complaining.
				string sql3 =
					@"select seg.id, ft.id, com.txt, com.fmt
				from ScrBook book
				join CmObject sec on sec.Owner$ = book.id
				join CmObject txt_fn on txt_fn.Owner$ = sec.id or txt_fn.Owner$ = book.id
				join CmObject para on para.Owner$ = txt_fn.id
				join CmBaseAnnotation_ seg on seg.BeginObject = para.id and seg.AnnotationType = " + segmentDefn + @"
				join CmIndirectAnnotation_AppliesTo ft_at on ft_at.dst = seg.id
				join CmIndirectAnnotation_ ft on ft.id = ft_at.src and ft.AnnotationType = " + ftSegmentDefn + @"
				left outer join CmAnnotation_Comment com on ft.id = com.obj and com.ws = " + ws + @"
				where book.id = " + source.Hvo;

				int kflidFT = StTxtPara.SegmentFreeTranslationFlid(cache);
				IDbColSpec dcs = DbColSpecClass.Create();
				dcs.Push((int)DbColType.koctBaseId, 0, 0, 0); // segment ID column is base.
				dcs.Push((int)DbColType.koctObj, 1, kflidFT, 0); // ft annotation is property kflidFT of the segment
				dcs.Push((int)DbColType.koctMlsAlt, 2, kflidComment, ws); // then comes the text of the comment
				dcs.Push((int)DbColType.koctFmt, 2, kflidComment, ws); // and the string format information of the comment.
				Logger.WriteEvent("Loading annotations for WS = " + ws);
				cache.VwOleDbDaAccessor.Load(sql3, dcs, 0, 0, null, false);

				// Now we've preloaded the old FTs and comments, copy to new.
				Logger.WriteEvent("Processing annotations for WS = " + ws);
				foreach (KeyValuePair<int, int> pair in segMap)
				{
					int ftSrc = cache.GetObjProperty(pair.Key, kflidFT);
					if (ftSrc == 0)
						continue; // label segment
					int ftDst = m_cache.GetObjProperty(pair.Value, kflidFT);
					ICmIndirectAnnotation ft;
					if (ftDst == 0)
					{
						ft = CmIndirectAnnotation.CreateUnownedIndirectAnnotation(cache);
						ft.AppliesToRS.Append(pair.Value);
						ft.AnnotationTypeRAHvo = ftSegmentDefn;
						Cache.VwCacheDaAccessor.CacheObjProp(pair.Value, kflidFT, ft.Hvo);
					}
					else
					{
						// Already exists: presumably from a different iteration with a different WS.
						ft = new CmIndirectAnnotation(m_cache, ftDst);
					}
					cache.SetMultiStringAlt(ft.Hvo, kflidComment, ws,
						cache.GetMultiStringAlt(ftSrc, kflidComment, ws));
					ftMap[ftSrc] = ft.Hvo;
				}
				Logger.WriteEvent("Copying complete for WS = " + ws);
			}

			// Update any ScrScriptureNote with old FT hvo in its BeginObject or EndObject, or an
			// old para hvo in its BeginObject or EndObject.
			Logger.WriteEvent("Updating Scripture Annotation Refereneces");
			foreach (IScrScriptureNote note in this.BookAnnotationsOS[dest.CanonicalNum - 1].NotesOS)
			{
				if (ftMap.ContainsKey(note.BeginObjectRAHvo))
					note.BeginObjectRAHvo = ftMap[note.BeginObjectRAHvo];
				if (ftMap.ContainsKey(note.EndObjectRAHvo))
					note.EndObjectRAHvo = ftMap[note.EndObjectRAHvo];
				if (paraMap.ContainsKey(note.BeginObjectRAHvo))
					note.BeginObjectRAHvo = paraMap[note.BeginObjectRAHvo];
				if (paraMap.ContainsKey(note.EndObjectRAHvo))
					note.EndObjectRAHvo = paraMap[note.EndObjectRAHvo];
			}
			Logger.WriteEvent("CopyFreeTranslations complete");
		}

		private void MapText(IStText sourceText, IStText destText, Dictionary<int, int> paraMap)
		{
			for(int ipara = 0; ipara < sourceText.ParagraphsOS.Count; ipara++)
			{
				paraMap[sourceText.ParagraphsOS.HvoArray[ipara]] = destText.ParagraphsOS.HvoArray[ipara];
			}
		}
		#endregion

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Utility method to make sure passed in style-id is valid (not null or empty)
		/// </summary>
		/// <param name="styleId"></param>
		/// ------------------------------------------------------------------------------------
		private void CheckStyleIdParam(string styleId)
		{
			if (styleId == null)
				throw new ArgumentNullException("styleId");
			if (styleId == string.Empty)
				throw new ArgumentException("Empty string is not a valid value", "styleId");
		}
		#endregion

		#region IPictureLocationParser Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses a picture location range string.
		/// </summary>
		/// <param name="s">The string representation of a picture location range.</param>
		/// <param name="anchorLocation">The anchor location.</param>
		/// <param name="locType">The type of the location range. The incoming value tells us
		/// the assumed type for parsing. The out value can be set to a different type if we
		/// discover that the actual value is another type.</param>
		/// <param name="locationMin">The location min.</param>
		/// <param name="locationMax">The location max.</param>
		/// <remarks>This is implemented on Scripture because CmPicture's implementation does
		/// not handle Scripture reference ranges.</remarks>
		/// ------------------------------------------------------------------------------------
		public void ParsePictureLoc(string s, int anchorLocation,
			ref PictureLocationRangeType locType, out int locationMin, out int locationMax)
		{
			locationMin = locationMax = 0;
			if (BCVRef.GetChapterFromBcv(anchorLocation) == 1 &&
				BCVRef.GetVerseFromBcv(anchorLocation) == 0)
			{
				string[] pieces = s.Split(new char[] { '-' }, 2,
					StringSplitOptions.RemoveEmptyEntries);

				if (pieces.Length == 2 &&
					Int32.TryParse(pieces[0], out locationMin) &&
					Int32.TryParse(pieces[1], out locationMax))
				{
					locType = PictureLocationRangeType.ParagraphRange;
					return;
				}
				locType = PictureLocationRangeType.AfterAnchor;
				return;
			}
			BCVRef startRef = new BCVRef(anchorLocation);
			BCVRef endRef = new BCVRef(anchorLocation);
			if (ScrReference.ParseRefRange(s, ref startRef, ref endRef, Versification) &&
				startRef <= anchorLocation && endRef >= anchorLocation)
			{
				locationMin = startRef.BBCCCVVV;
				locationMax = endRef.BBCCCVVV;
				locType = PictureLocationRangeType.ReferenceRange;
			}
			else
				locType = PictureLocationRangeType.AfterAnchor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the picture location.
		/// </summary>
		/// <param name="picture">The picture.</param>
		/// ------------------------------------------------------------------------------------
		public string GetPictureLocAsString(ICmPicture picture)
		{
			if (picture.LocationRangeType == PictureLocationRangeType.ReferenceRange)
			{
				return (new BCVRef(picture.LocationMin)).AsString + "-" +
					(new BCVRef(picture.LocationMax)).AsString;
			}
			return picture.GetPictureLocAsString(picture);
		}
		#endregion
	}
	#endregion

	#region class ScrBook
	public partial class ScrBook
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a paragraph with the specified style id, containing the specified verse number,
		/// if specified, and having the correct sequence number. Sets all appropriate state
		/// variables before returning. Assumes the reference is within this book.
		/// </summary>
		/// <param name="targetStyle">style of paragraph to find</param>
		/// <param name="targetRef">Reference to seek</param>
		/// <param name="iPara">0-based index of paragraph</param>
		/// <param name="iVernSection">0-based index of the section the corresponding
		/// vernacular paragraph is in. This will be 0 if no corresponding paragraph can be
		/// found.</param>
		/// <returns>The corresponding StTxtPara, or null if no matching para is found</returns>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara FindCorrespondingVernParaForSegment(IStStyle targetStyle,
			BCVRef targetRef, int iPara, ref int iVernSection)
		{
			Debug.Assert(iPara >= 0);
			try
			{
				if (targetStyle != null)
				{
					if (targetStyle.Context == ContextValues.Title)
					{
						if (iPara < TitleOA.ParagraphsOS.Count)
						{
							ScrTxtPara para = new ScrTxtPara(Cache, TitleOA.ParagraphsOS.HvoArray[iPara]);
							if (para.StyleName == targetStyle.Name)
								return para as IStTxtPara;
						}
						return null;
					}

					if (iVernSection >= 0 && iVernSection < SectionsOS.Count)
					{
						IScrSection section = SectionsOS[iVernSection];
						if (targetStyle.Structure == StructureValues.Heading)
						{
							if (iPara < section.HeadingOA.ParagraphsOS.Count)
							{
								ScrTxtPara para = new ScrTxtPara(Cache, section.HeadingOA.ParagraphsOS.HvoArray[iPara]);
								if (para.StyleName == targetStyle.Name)
									return para as IStTxtPara;
							}
						}
						else
						{
							if (iPara < section.ContentOA.ParagraphsOS.Count &&
								(targetRef.Verse == 0 ||
								(section.VerseRefMin <= targetRef && targetRef <= section.VerseRefMax)))
							{
								ScrTxtPara para = new ScrTxtPara(Cache, section.ContentOA.ParagraphsOS.HvoArray[iPara]);
								if (para.StyleName == targetStyle.Name)
									return para as IStTxtPara;
							}
						}
					}
				}

				Debug.Assert(targetRef.Valid);
				iVernSection = -1;
				ScrReference refToTest = new ScrReference(targetRef, Versification);
				refToTest.Verse = Math.Min(refToTest.Verse, refToTest.LastVerse);
				foreach (IScrSection section in SectionsOS)
				{
					iVernSection++;

					// Since section.VerseRefMax can never be greater than what the versification
					// scheme allows, but the section can contain verses greater than that max.,
					// make sure to find the section based on the versification's max.
					if (section.VerseRefMin <= refToTest && refToTest <= section.VerseRefMax)
					{
						if (targetStyle != null && targetStyle.Structure == StructureValues.Heading)
						{
							if (iPara < section.HeadingOA.ParagraphsOS.Count)
							{
								ScrTxtPara para = new ScrTxtPara(Cache, section.HeadingOA.ParagraphsOS.HvoArray[iPara]);
								if (para.StyleName == targetStyle.Name)
									return para as IStTxtPara;
							}
							return null;
						}
						else
						{
							BCVRef refStart, refEnd;
							foreach (IStTxtPara stPara in section.ContentOA.ParagraphsOS)
							{
								ScrTxtPara para = stPara as ScrTxtPara;
								if (para == null)
								{
									Debug.WriteLine("Book.FindCorrespondingVernParaForSegment() requires that " +
										"StTxtPara is mapped to ScrTxtPara (see TeApp.InitCache)");
									return null;
								}

								if (targetStyle == null || para.StyleName == targetStyle.Name)
								{
									para.GetBCVRefAtPosition(para.Contents.Length - 1, out refStart, out refEnd);
									if (refEnd < targetRef)
										continue;
									para.GetBCVRefAtPosition(0, out refStart, out refEnd);
									if (refStart > targetRef)
										continue;

									return para;
									//ITsString tss = para.Contents.UnderlyingTsString;
									//int iLim = tss.RunCount;
									//for (int iRun = 0; iRun < iLim; )
									//{
									//    Scripture.GetNextRef(iRun, iLim, tss, true, ref refStart,
									//        ref refEnd, out iRun);
									//    if (refStart <= targetRef && refEnd >= targetRef)
									//        return para;
									//}
								}
							}
						}
					}
				}
			}
			catch (NullStyleRulesException)
			{
			}
			iVernSection = 0;
			return null;
		}
	}
	#endregion

	#region class ScrDraft
	public partial class ScrDraft
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the specified book in the database based from the canonical order
		/// </summary>
		/// <param name="canonicalBookNum">The canonical book number</param>
		/// <returns>The specified book if it exists, otherwise null.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook FindBook(int canonicalBookNum)
		{
			foreach (IScrBook book in BooksOS)
			{
				if (book.CanonicalNum == canonicalBookNum)
					return book;
			}
			return null;
		}
	}
	#endregion
}
