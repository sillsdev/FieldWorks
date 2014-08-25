// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoScripture.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
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
	internal partial class Scripture : IScrProjMetaDataProvider, IPictureLocationBridge, IPropertyChangeNotifier
	{
		#region Public Events
		public event PropertyChangedHandler BooksChanged;
		#endregion

		/// <summary>Accessor for getting at the generated resource manager from other
		/// assemblies</summary>
		public static System.Resources.ResourceManager Resources = ScrFdoResources.ResourceManager;

		#region Scripture Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this instance of Scripture has had the
		/// BT CmTranslation fix applied. The setter is really only intended to be used
		/// internally. Don't ever set it to false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool FixedParasWithoutBt
		{
			get
			{
				return ResourcesOC.Any(res => res.Name == CmResourceTags.ksFixedParasWithoutBt &&
					res.Version == CmResourceTags.kguidFixedParasWithoutBt);
			}
			internal set
			{
				Debug.Assert(value, "Never set FixedParasWithoutBt to false.");
				ICmResource res = Services.GetInstance<ICmResourceFactory>().Create();
				ResourcesOC.Add(res);
				res.Name = CmResourceTags.ksFixedParasWithoutBt;
				res.Version = CmResourceTags.kguidFixedParasWithoutBt;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this instance of Scripture has had the
		/// segments fix applied. The setter is really only intended to be used
		/// internally. Don't ever set it to false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool FixedParasWithoutSegments
		{
			get
			{
				return ResourcesOC.Any(res => res.Name == CmResourceTags.ksFixedParasWithoutSegments &&
					res.Version == CmResourceTags.kguidFixedParasWithoutSegments);
			}
			internal set
			{
				Debug.Assert(value, "Never set FixedParasWithoutSegments to false.");
				ICmResource res = Services.GetInstance<ICmResourceFactory>().Create();
				ResourcesOC.Add(res);
				res.Name = CmResourceTags.ksFixedParasWithoutSegments;
				res.Version = CmResourceTags.kguidFixedParasWithoutSegments;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the old key terms list with its leftover key
		/// term hierarchical structures has already been removed from a FW language project.
		/// Don't ever set it to false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool RemovedOldKeyTermsList
		{
			get
			{
				return ResourcesOC.Any(res => res.Name == CmResourceTags.ksRemovedOldKeyTermsList &&
					res.Version == CmResourceTags.kguidRemovedOldKeyTermsList);
			}
			internal set
			{
				Debug.Assert(value, "Never set RemovedOldKeyTermsList to false.");
				ICmResource res = Services.GetInstance<ICmResourceFactory>().Create();
				ResourcesOC.Add(res);
				res.Name = CmResourceTags.ksRemovedOldKeyTermsList;
				res.Version = CmResourceTags.kguidRemovedOldKeyTermsList;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether styles referenced in ScrParas have had the fix to
		/// mark these styles as InUse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool FixedStylesInUse
		{
			get
			{
				return ResourcesOC.Any(res => res.Name == CmResourceTags.ksFixedStylesInUse &&
					res.Version == CmResourceTags.kguidFixedStylesInUse);
			}
			internal set
			{
				Debug.Assert(value, "Never set FixedStylesInUse to false.");
				ICmResource res = Services.GetInstance<ICmResourceFactory>().Create();
				ResourcesOC.Add(res);
				res.Name = CmResourceTags.ksFixedStylesInUse;
				res.Version = CmResourceTags.kguidFixedStylesInUse;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this instance of Scripture has had the
		/// orphaned footnote fix applied. The setter is really only intended to be used
		/// internally. Don't ever set it to false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool FixedOrphanedFootnotes
		{
			get
			{
				return ResourcesOC.Any(res => res.Name == CmResourceTags.ksFixedOrphanedFootnotes &&
					res.Version == CmResourceTags.kguidFixedOrphanedFootnotes);
			}
			set
			{
				Debug.Assert(value, "Never set FixedOrphanedFootnotes to false.");
				ICmResource res = Services.GetInstance<ICmResourceFactory>().Create();
				ResourcesOC.Add(res);
				res.Name = CmResourceTags.ksFixedOrphanedFootnotes;
				res.Version = CmResourceTags.kguidFixedOrphanedFootnotes;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this instance of Scripture has had any
		/// paragraphs with ORCS resegmented, following the change to no longer have ORCS cause
		/// segment breaks. The setter is really only intended to be used internally. Don't ever
		/// set it to false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ResegmentedParasWithOrcs
		{
			get
			{
				return ResourcesOC.Any(res => res.Name == CmResourceTags.ksResegmentedParasWithOrcs &&
					res.Version == CmResourceTags.kguidResegmentedParasWithOrcs);
			}
			internal set
			{
				Debug.Assert(value, "Never set ResegmentedParasWithOrcs to false.");
				ICmResource res = Services.GetInstance<ICmResourceFactory>().Create();
				ResourcesOC.Add(res);
				res.Name = CmResourceTags.ksResegmentedParasWithOrcs;
				res.Version = CmResourceTags.kguidResegmentedParasWithOrcs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the callout option for cross-references.
		/// Getter takes CrossRefsCombinedWithFootnotes into consideration.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FootnoteMarkerTypes CrossRefMarkerType_Effective
		{
			get
			{
				return CrossRefsCombinedWithFootnotes ? FootnoteMarkerType : CrossRefMarkerType;
			}
		}

		/// <summary>
		/// All the StTexts owned by this Scripture.
		/// </summary>
		public IEnumerable<IStText> StTexts
		{
			get
			{
				foreach (var book in ScriptureBooksOS)
				{
					if (book.TitleOA != null)
						yield return book.TitleOA;
					foreach (var section in book.SectionsOS)
					{
						if (section.HeadingOA != null)
							yield return section.HeadingOA;
						if (section.ContentOA != null)
							yield return section.ContentOA;
					}
					foreach (var stText in book.FootnotesOS)
						yield return stText;
				}
			}
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
					FootnoteMarkerSymbol == ScriptureTags.kDefaultFootnoteMarkerSymbol &&
					!DisplayFootnoteReference &&
					!CrossRefsCombinedWithFootnotes &&
					CrossRefMarkerType == FootnoteMarkerTypes.NoFootnoteMarker &&
					CrossRefMarkerSymbol == ScriptureTags.kDefaultFootnoteMarkerSymbol &&
					DisplayCrossRefReference;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the effective cross-reference marker that should be used, taking
		/// CrossRefsCombinedWithFootnotes into consideration (if combining cross-reference and
		/// general footnotes, we use the general footnote marker and ignore the cross-reference
		/// marker).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string CrossRefMarkerSymbol_Effective
		{
			get
			{
				return CrossRefsCombinedWithFootnotes ? FootnoteMarkerSymbol : CrossRefMarkerSymbol;
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
				switch (CrossRefMarkerType_Effective)
				{
					case FootnoteMarkerTypes.NoFootnoteMarker:
						return null;
					case FootnoteMarkerTypes.SymbolicFootnoteMarker:
						return CrossRefMarkerSymbol_Effective;
					case FootnoteMarkerTypes.AutoFootnoteMarker:
					default:
						return ScriptureTags.kDefaultAutoFootnoteMarker;
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
						return ScriptureTags.kDefaultAutoFootnoteMarker;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden because getter takes CrossRefsCombinedWithFootnotes into consideration
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool DisplayCrossRefReference_Effective
		{
			get
			{
				return CrossRefsCombinedWithFootnotes ? DisplayFootnoteReference :
					DisplayCrossRefReference;
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
		private IScrImportSet DefaultImportSettings_internal
		{
			get
			{
				string sName = ScriptureTags.kDefaultImportSettingsName;
				foreach (IScrImportSet importSetting in ImportSettingsOC)
				{
					if (importSetting.Name.UserDefaultWritingSystem.Text == sName)
						return importSetting;
				}
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the import settings named Default, if any, or the first available
		/// one (which is probably the only one), or creates new settings if none exist.
		/// </summary>
		/// <param name="importType">type of import type to find</param>
		/// <param name="defaultParaCharsStyleName">The default paragraph characters style name.</param>
		/// <param name="stylesPath"></param>
		/// ------------------------------------------------------------------------------------
		public IScrImportSet FindOrCreateDefaultImportSettings(TypeOfImport importType, string defaultParaCharsStyleName, string stylesPath)
		{
			IScrImportSet settings = DefaultImportSettings_internal;

			// First, attempt to find the default import settings
			if (settings != null && (settings.ImportType == (int)importType || importType == TypeOfImport.Unknown))
				return settings;

			if (ImportSettingsOC.Count > 0)
			{
				// If the specified type is unknown, just return the first set.
				if (importType == TypeOfImport.Unknown)
					return ImportSettingsOC.ToArray()[0];
				else
				{
					// Attempt to find the specified import type.
					foreach (IScrImportSet importSettings in ImportSettingsOC)
					{
						if (importSettings.ImportType == (int)importType)
							return importSettings;
					}
				}
			}

			// Didn't find the specified type of settings, so create a new set.
			IScrImportSet newSettings =
				m_cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create(defaultParaCharsStyleName, stylesPath);
			ImportSettingsOC.Add(newSettings);
			newSettings.ImportType = (int)importType;
			return newSettings;
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
					if (settings.Name.UserDefaultWritingSystem.Text ==
						ScriptureTags.kDefaultImportSettingsName)
					{
						return settings;
					}
				}

				// We didn't find settings with the name "Default", so set the first
				// set in the collection to default and return it.
				IScrImportSet firstSettings = ImportSettingsOC.ToArray()[0];
				int wsHvo = WritingSystemServices.FallbackUserWs(m_cache);
				firstSettings.Name.set_String(wsHvo, ScriptureTags.kDefaultImportSettingsName);
				return firstSettings;
			}

			set
			{
				IScrImportSet existingSettings = DefaultImportSettings_internal;

				if (existingSettings == null)
				{
					// If no settings have been set as the default, we attempt to find settings of the same type.
					// Currently, we maintain settings for each type of import type.
					// If the new value has an import type, then we attempt to find these settings.
					if (value != null)
						existingSettings = FindImportSettings(value.ImportTypeEnum);
				}

				if (value == null && existingSettings != null)
				{
					ImportSettingsOC.Remove(existingSettings);
				}
				else if (value != null)
				{
					int wsHvo = WritingSystemServices.FallbackUserWs(m_cache);

					// Determine if anything else was set as the default previously.
					// If so, change the name from Default to the name of the type of import.
					foreach (IScrImportSet importSet in ImportSettingsOC)
					{
						bool fSetToDefault = importSet.Name.UserDefaultWritingSystem.Text ==
							ScriptureTags.kDefaultImportSettingsName;

						// if the import set is set to "default" and it is not the type of
						// import settings that we are setting as the default...
						if (fSetToDefault && value.ImportTypeEnum != importSet.ImportTypeEnum)
						{
							// set it back to the name of this type of import.
							switch (importSet.ImportTypeEnum)
							{
								case TypeOfImport.Other:
									importSet.Name.set_String(wsHvo, ScriptureTags.kOtherImportSettingsName);
									break;
								case TypeOfImport.Paratext5:
									importSet.Name.set_String(wsHvo, ScriptureTags.kParatext5ImportSettingsName);
									break;
								case TypeOfImport.Paratext6:
									importSet.Name.set_String(wsHvo, ScriptureTags.kParatext6ImportSettingsName);
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
							importSet.Name.set_String(wsHvo, ScriptureTags.kDefaultImportSettingsName);
						}
					}

					if (!ImportSettingsOC.Contains(value))
						ImportSettingsOC.Add(value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the SCR proj meta data provider.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrProjMetaDataProvider ScrProjMetaDataProvider
		{
			get { return this; }
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
				IScrBook book = FindBook(annotations.CanonicalNum);
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
			IScrBook book = FindBook(bookNum);
			if (book != null)
				AttachAnnotatedObjects(book, note);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attaches the annotated objects, if they're not already attached.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AttachAnnotatedObjects(IScrBook book, IScrScriptureNote note)
		{
			if (note.BeginObjectRA == null)
				FindMissingReference(book, note);
			else if (note.BeginObjectRA.OwnerOfClass<IScrBook>() != book)
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
		private void FindMissingReference(IScrBook book, IScrScriptureNote note)
		{
			if (!note.QuoteOA.IsEmpty)
				note.BeginObjectRA = note.EndObjectRA = FindQuoteInTitleOrIntro(book, note);

			if (note.BeginObjectRA == null)
				note.BeginObjectRA = note.EndObjectRA = book.FirstSection.IsIntro ? book.FirstSection.FirstContentParagraph : null;

			if (note.BeginObjectRA == null)
				note.BeginObjectRA = note.EndObjectRA = (IStTxtPara)book.TitleOA.ParagraphsOS[0];
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
		private ICmObject FindQuoteInTitleOrIntro(IScrBook book, IScrScriptureNote note)
		{
			string quote = note.QuoteOA[0].Contents.Text;
			ICmObject result = SearchStTextForQuote(book.TitleOA, note, quote);
			if (result == null)
			{
				IScrSection section = book.SectionsOS[0];
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
					section = book.SectionsOS[sectionIndex];
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
			foreach (IStTxtPara para in text.ParagraphsOS)
			{
				if (para.Contents.Length == 0)
					continue;
				// Remove chapter/verse numbers and footnotes from paragraph contents - this is done when
				// quote is created for the note.
				ITsString tssQuote = TsStringUtils.GetCleanTsString(para.Contents, ScrStyleNames.ChapterAndVerse);

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
		/// Finds paragraph in the passed book that is at a similar structural location to the
		/// given paragraph's location in its book.
		/// </summary>
		/// <param name="book">Current version of book</param>
		/// <param name="savedPara">Paragraph in saved archive of the book</param>
		/// <returns>
		/// Paragraph that is in a similar location to the passed paragraph if it exists;
		/// otherwise <c>null</c> will be returned.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private ICmObject FindCorrespondingParagraph(IScrBook book, ICmObject savedPara)
		{
			// make sure we really did find a paragraph
			if (!(savedPara is IStTxtPara))
				return null;

			IScrBook savedBook = savedPara.OwnerOfClass<IScrBook>();
			if (savedBook != null)
			{
				IStText owningText = (IStText)savedPara.Owner;
				int paraIndex = savedPara.IndexInOwner;
				IStText curText = null;
				int owningFlid = owningText.OwningFlid;
				if (owningFlid == (int)ScrBookTags.kflidTitle)
					curText = book.TitleOA;
				else if (owningFlid == (int)ScrSectionTags.kflidHeading ||
					owningFlid == (int)ScrSectionTags.kflidContent)
				{
					IScrSection owningSection = (IScrSection)owningText.Owner;
					int sectionIndex = owningSection.IndexInOwner;
					sectionIndex = Math.Min(sectionIndex, book.SectionsOS.Count - 1);
					IScrSection curSection = book.SectionsOS[sectionIndex];
					while (!curSection.IsIntro)
					{
						sectionIndex--;
						if (sectionIndex < 0)
							return null;
						curSection = book.SectionsOS[sectionIndex];
					}
					curText = (owningFlid == (int)ScrSectionTags.kflidHeading)
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
		/// Find the specified book
		/// </summary>
		/// <param name="sSilAbbrev">The 3-letter SIL abbreviation (all-caps) for the book
		/// </param>
		/// <returns>The specified book if it exists; otherwise, null</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook FindBook(string sSilAbbrev)
		{
			return ScriptureBooksOS.FirstOrDefault(book => book.BookId == sSilAbbrev);
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
			return ScriptureBooksOS.FirstOrDefault(book => book.CanonicalNum == bookOrd);
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
			return HeaderFooterSetsOC.FirstOrDefault(hfSet => hfSet.Name == name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain a style object from a style name.
		/// </summary>
		/// <param name="styleName">Name of style to find</param>
		/// ------------------------------------------------------------------------------------
		public IStStyle FindStyle(string styleName)
		{
			foreach (IStStyle style in StylesOC)
			{
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
			return FindStyle(ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the publication with a requested name.
		/// </summary>
		/// <param name="publicationName">Name of publication</param>
		/// <returns>found publication or null</returns>
		/// ------------------------------------------------------------------------------------
		public IPublication FindByName(string publicationName)
		{
			foreach (IPublication pub in PublicationsOC)
			{
				if (pub.Name == publicationName)
					return pub;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy a book (typically from an archive) to the current version. Caller should
		/// first ensure there is no book with that ID.
		/// </summary>
		/// <param name="book">The book to copy.</param>
		/// <exception cref="InvalidOperationException">Attempt to copy book to current version
		/// when that book already exists in the current version</exception>
		/// <returns>The copied bok</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook CopyBookToCurrent(IScrBook book)
		{
			if (FindBook(book.CanonicalNum) != null)
				throw new InvalidOperationException("Attempt to copy book to current version when that book already exists in the current version");

			// Find the first book whose canonical number is greater than the one we're copying
			// in. The copied book should be inserted before that one.
			IScrBook dstStart = ScriptureServices.GetBookFollowing(ScriptureBooksOS, book.CanonicalNum);
			int iInsert = (dstStart != null) ? dstStart.IndexInOwner : ScriptureBooksOS.Count;
			IScrBook copiedBook = CopyObject<IScrBook>.CloneFdoObject(book,
				x => ScriptureBooksOS.Insert(iInsert, x));
			ScriptureServices.AdjustObjectsInArchivedBook(book, copiedBook);
			return copiedBook;
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
		/// Get the chapter verse bridge for a given section, formatted appropriately for the UI
		/// writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ChapterVerseBridgeAsString(IScrSection section)
		{
			if (section.IsIntro)
			{
				return String.Format(Strings.ksScriptureSectionIntroduction, section.OwnOrd);
			}
			ScrReference startRef = new ScrReference(section.VerseRefStart, Versification);
			ScrReference endRef = new ScrReference(section.VerseRefEnd, Versification);
			if (startRef.Chapter != endRef.Chapter)
			{
				return MakeBridgeAsString(ChapterVerseRefAsString(startRef, m_cache.DefaultUserWs),
					ChapterVerseRefAsString(endRef, m_cache.DefaultUserWs), m_cache.DefaultUserWs);
			}
			return ChapterVerseBridgeAsString(startRef, endRef, m_cache.DefaultUserWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Chapter Verse Reference representation for a footnote (ie. sectionCV-CV Footnote(footnoteCV))
		/// Or Title representation (ie. Title Footnote(OwnOrd))
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ContainingRefAsString(IScrFootnote footnote)
		{
			footnote.IgnoreDisplaySettings = true; // so we can access GetReference.
			string parentContext = "";
			string footnoteRef = "";
			IScrSection containingSection;
			IStText containingTitle;
			if (footnote.TryGetContainingSection(out containingSection))
			{
				parentContext = ChapterVerseBridgeAsString(containingSection);
				footnoteRef = footnote.GetReference(m_cache.DefaultUserWs).Trim();
			}
			else if (footnote.TryGetContainingTitle(out containingTitle))
			{
				parentContext = Strings.ksScriptureTitle;
				footnoteRef = footnote.OwnOrd.ToString(CultureInfo.InvariantCulture);
			}
			return String.Format("{0} {1}({2})", parentContext, Strings.ksScriptureFootnote, footnoteRef);
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
			ITsString tssBookName = ((IScrBook)section.Owner).Name.get_String(ws);
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
		/// Get full reference (BCV-CV) of the given StText. Returns an empty string if we can't find a book name
		/// with the given 'ws'.
		/// </summary>
		/// <param name="stText"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString BookChapterVerseBridgeAsTss(IStText stText, int ws)
		{
			ITsString tssTitle = null;
			Debug.Assert(stText.OwnerOfClass<IScripture>() != null, "given StText should belong to scripture.");

			int owningFlid = stText.OwningFlid;
			if (owningFlid == ScrBookTags.kflidFootnotes)
			{
				IScrFootnote footnote = Services.GetInstance<IScrFootnoteRepository>().GetObject(stText.Hvo);
				string footnoteRefStr = ContainingRefAsString(footnote);
				ScrBook book = footnote.Owner as ScrBook;
				ITsString tssBook = book.Name.get_String(ws);
				if (tssBook.Length > 0)
				{
					ITsStrBldr bldr = tssBook.GetBldr();
					int cch = bldr.Length;
					bldr.Replace(cch, cch, " " + footnoteRefStr, null);
					tssTitle = bldr.GetString();
				}
			}
			else if (owningFlid == ScrSectionTags.kflidContent || owningFlid == ScrSectionTags.kflidHeading)
			{
				IScrSection section = (IScrSection)stText.Owner;

				ITsString tssBookChapterVerseBridge = BookChapterVerseBridgeAsTss(section, ws);
				tssTitle = tssBookChapterVerseBridge;
			}
			else if (owningFlid == ScrBookTags.kflidTitle)
			{
				IScrBook book = (IScrBook)stText.Owner;
				ITsString tssBookName = book.Name.get_String(ws);
				if (tssBookName.Length > 0)
				{
					ITsStrBldr bldr = tssBookName.GetBldr();
					int cch = bldr.Length;
					bldr.Replace(cch, cch, String.Format(" ({0})", Strings.ksScriptureTitle), null);
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
		/// <param name="start">A BCVRef that holds the chapter and beginning verse to format</param>
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
		/// if specified, and having the correct sequence number.
		/// </summary>
		/// <param name="targetStyle">style of paragraph to find</param>
		/// <param name="targetRef">Reference to seek</param>
		/// <param name="iPara">0-based index of paragraph</param>
		/// <param name="iVernSection">0-based index of the section the corresponding
		/// vernacular paragraph is in. This will be 0 if no corresponding paragraph can be
		/// found.</param>
		/// <returns>The corresponding StTxtPara, or null if no matching para is found</returns>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara FindPara(IStStyle targetStyle, BCVRef targetRef, int iPara,
			ref int iVernSection)
		{
			// REVIEW: In production code, iPara is always passed as 0. Can we eliminate this parameter?
			// Do tests really need to be able to pass 1? Why are we testing something that can never happen in real life?
			Debug.Assert(targetRef.BookIsValid, "Invalid book number");
			IScrBook book = FindBook(targetRef.Book);
			if (book == null)
				return null;

			return book.FindPara(targetStyle, targetRef, iPara, ref iVernSection);
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
		/// <exception cref="T:System.ArgumentException">Invalid chapter number</exception>
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
					string style = tss.FetchRunInfo(iRun, out runInfo).Style();
					if (style == ScrStyleNames.VerseNumber)
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
						if (startVerse <= refStart.LastVerse)
						{
							refStart.Verse = startVerse;
							refEnd.Verse = Math.Min(refEnd.LastVerse, endVerse);
						}

						iNext = iRun + 1;
						return RefRunType.Verse;
					}
					if (style == ScrStyleNames.ChapterNumber)
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
				DisplayCrossRefReference_Effective : DisplayFootnoteReference;
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
				CrossRefMarkerSymbol_Effective : FootnoteMarkerSymbol;
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
				CrossRefMarkerType_Effective : FootnoteMarkerType;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a TsString in which any CV numbers have been replaced by their BT equivalents
		/// (in the specified writing system). Other properties, including style, are copied
		/// from the input numbers.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <param name="wsTrans">The ws trans.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
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
		#endregion

		#region Methods for retrieving WS-specific punctuation for Scripture references
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
		/// ------------------------------------------------------------------------------------
		private string FormatReferencePunctForWs(string punct, int hvoWs)
		{
			IWritingSystem ws = (hvoWs <= 0) ? Services.WritingSystems.DefaultVernacularWritingSystem :
				Services.WritingSystemManager.Get(hvoWs);
			return (ws.RightToLeftScript) ? "\u200f" + punct + "\u200f" : punct;
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

		#region IPictureLocationBridge Members
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
			return ((CmPicture)picture).PictureLocAsString;
		}
		#endregion

		#region IPropertyChangeNotifier Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the UOW is complete so we can notify anyone who cares about a change in
		/// our ScriptureBooks collection.
		/// </summary>
		/// <param name="flid">The flid of the changed property.</param>
		/// ------------------------------------------------------------------------------------
		public void NotifyOfChangedProperty(int flid)
		{
			if (flid == ScriptureTags.kflidScriptureBooks && BooksChanged != null)
				BooksChanged(this);
		}
		#endregion

		partial void ValidateRefSepr(ref string newValue)
		{
			if (String.IsNullOrEmpty(newValue))
				throw new ArgumentException("RefSepr can not be null or empty");
		}

		partial void ValidateChapterVerseSepr(ref string newValue)
		{
			if (String.IsNullOrEmpty(newValue))
				throw new ArgumentException("ChapterVerseSepr can not be null or empty");
		}

		partial void ValidateVerseSepr(ref string newValue)
		{
			if (String.IsNullOrEmpty(newValue))
				throw new ArgumentException("VerseSepr can not be null or empty");
		}

		partial void ValidateBridge(ref string newValue)
		{
			if (String.IsNullOrEmpty(newValue))
				throw new ArgumentException("Bridge can not be null or empty");
		}

		partial void ValidateVersification(ref ScrVers newValue)
		{
			// ENHANCE (TE-6620): Rather than blindly defaulting to English, we could:
			// a) Get a default from a local XML file or something to allow
			// branch defaults.
			// b) Make sure the .vrs file is present and choose another if not.
			if (newValue == ScrVers.Unknown)
				newValue = ScrVers.English;
		}
	}
	#endregion

	#region class ScrDraft
	internal partial class ScrDraft
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
			return BooksOS.FirstOrDefault(book => book.CanonicalNum == canonicalBookNum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Description;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a copy of the specified book to this version.
		/// </summary>
		/// <param name="book">book to copy</param>
		/// <exception cref="InvalidOperationException">Saved version already contains a copy of
		/// the specified book</exception>
		/// <returns>The saved version of the book (copy of original)</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook AddBookCopy(IScrBook book)
		{
			if (FindBook(book.CanonicalNum) != null)
				throw new InvalidOperationException("Saved version already contains a copy of " + book.BookId);

			// Find the first book whose canonical number is greater than the one we're copying
			// in. The copied book should be inserted before that one.
			IScrBook dstStart = ScriptureServices.GetBookFollowing(BooksOS, book.CanonicalNum);
			int iInsert = (dstStart != null) ? dstStart.IndexInOwner : BooksOS.Count;

			IScrBook archivedBook = CopyObject<IScrBook>.CloneFdoObject(book, x => BooksOS.Insert(iInsert, x));

			ScriptureServices.AdjustObjectsInArchivedBook(book, archivedBook);
			return archivedBook;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the specified book to this version
		/// </summary>
		/// <param name="book">book to move</param>
		/// <exception cref="InvalidOperationException">Saved version already contains a copy of
		/// the specified book</exception>
		/// ------------------------------------------------------------------------------------
		public void AddBookMove(IScrBook book)
		{
			if (FindBook(book.CanonicalNum) != null)
				throw new InvalidOperationException("Saved version already contains " + book.BookId);

			// Find the first book whose canonical number is greater than the one we're copying
			// in. The copied book should be inserted before that one.
			IScrBook dstStart = ScriptureServices.GetBookFollowing(BooksOS, book.CanonicalNum);
			int iInsert = (dstStart != null) ? dstStart.IndexInOwner : BooksOS.Count;

			BooksOS.Insert(iInsert, book);
		}
	}

	#endregion
}
