// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoEnumerations.cs
// Responsibility: Randy Regnier
// Last reviewed: never
//
//
// <remarks>
// Implementation of various enumerations used in FDO.
// </remarks>

using System;

namespace SIL.FieldWorks.FDO
{
	#region ClassOwnershipStatus enum
	/// <summary>
	/// Class ownership options
	/// </summary>
	public enum ClassOwnershipStatus
	{
		/// <summary>An owner is required for all instances of the class of object.</summary>
		kOwnerRequired,
		/// <summary>An owner is optional for all instances of the class of object.</summary>
		kOwnerOptional,
		/// <summary>An owner is prohibitied for all instances of the class of object.</summary>
		kOwnerProhibited
	};
	#endregion

	#region MappingTypes enum
	/// <summary>
	///
	/// </summary>
	public enum MappingTypes
	{
		/// <summary></summary>
		kmtSenseCollection = 0,
		/// <summary></summary>
		kmtSensePair = 1,
		/// <summary>Sense Pair with different Forward/Reverse names</summary>
		kmtSenseAsymmetricPair = 2,
		/// <summary></summary>
		kmtSenseTree = 3,
		/// <summary></summary>
		kmtSenseSequence = 4,
		/// <summary></summary>
		kmtEntryCollection = 5,
		/// <summary></summary>
		kmtEntryPair = 6,
		/// <summary>Entry Pair with different Forward/Reverse names</summary>
		kmtEntryAsymmetricPair = 7,
		/// <summary></summary>
		kmtEntryTree = 8,
		/// <summary></summary>
		kmtEntrySequence = 9,
		/// <summary></summary>
		kmtEntryOrSenseCollection = 10,
		/// <summary></summary>
		kmtEntryOrSensePair = 11,
		/// <summary></summary>
		kmtEntryOrSenseAsymmetricPair = 12,
		/// <summary></summary>
		kmtEntryOrSenseTree = 13,
		/// <summary></summary>
		kmtEntryOrSenseSequence = 14
	};
	#endregion

	#region MarkerDomain enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Marker domains to indicate where markers are imported to
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Flags]
	public enum MarkerDomain
	{
		/// <summary>Default domain, based on context</summary>
		Default = 0x00,
		/// <summary>Don't use this for anything except blob conversion and interpreting old
		/// data when loading mapping lists from Db into memory - use Default</summary>
		DeprecatedScripture = 0x01, //
		/// <summary>back translation domain</summary>
		BackTrans = 0x02,
		/// <summary>Scripture annotations domain</summary>
		Note = 0x04,
		/// <summary>footnote domain</summary>
		Footnote = 0x08,
	}
	#endregion

	#region MappingTargetType enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is used both in defining mapping properties for this class AND also for the
	/// ImportStyleProxy.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum MappingTargetType
	{
		/// <summary>the typical and default case</summary>
		TEStyle = 0,
		/// <summary>is a USFM-style picture/graphic mapping (vertical bars separate parameters)</summary>
		Figure = 1,
		/// <summary>The vernacular translation of "Chapter"</summary>
		ChapterLabel = 2,
		/// <summary>Short name of the book, suitable for displaying on page headers</summary>
		TitleShort = 3,
		/// <summary>Used for the default paragraph characters. This is only used
		/// when the mapping is saved to the database. In the future, this may
		/// also be used in code.</summary>
		DefaultParaChars = 4,
		/// <summary>Caption of a picture</summary>
		FigureCaption,
		/// <summary>Copyright line for a picture</summary>
		FigureCopyright,
		/// <summary>Non-publishable description for a picture</summary>
		FigureDescription,
		/// <summary>Filename of a picture</summary>
		FigureFilename,
		/// <summary>Indication of where/how picture should be laid out (col, span, right,
		/// left, fill-col?, fill-span?, full-page?)</summary>
		FigureLayoutPosition,
		/// <summary>Reference range to which a picture applies (e.g., MRK 1--2,
		/// JHN 3:16-19)</summary>
		FigureRefRange,
		/// <summary>Scale factor for a picture (an integral percentage)</summary>
		FigureScale
	}
	#endregion

	#region LexEntryTypes enum
	/// <summary>
	/// The possible values of the Type field.
	/// </summary>
	public enum LexEntryTypes
	{
		/// <summary>
		/// A main entry is a main top level entry
		/// </summary>
		kMainEntry = 0,
		/// <summary>
		/// A minor entry is a separate entry in the dictionary which usually just refers to a main one
		/// </summary>
		kMinorEntry = 1,
		/// <summary>
		/// A subentry is one that is printed as part of the related main entry, typically an indented paragraph.
		/// </summary>
		kSubentry = 2
	}
	#endregion

	#region NoteType enum
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// NoteType values
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum NoteType
	{
		/// <summary>Unknown note type</summary>
		Unknown = -1,
		/// <summary>Note written by a Consultant</summary>
		Consultant,
		/// <summary>Note written by a Translator</summary>
		Translator,
		/// <summary>Error created by Scripture Check</summary>
		CheckingError,
	}
	#endregion

	#region BackTranslationStatus enum
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Status information for back translation paragraphs
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum BackTranslationStatus
	{
		/// <summary>
		/// The vernacular paragraph has been edited since the back translation or the
		/// back translation has been edited but the translator has not marked it as
		/// finished yet.
		/// </summary>
		Unfinished,
		/// <summary>
		/// The translator marks each back translation paragraph as finished when
		///	they are done.
		/// </summary>
		Finished,
		/// <summary>
		/// The paragraph is finished and has been consultant checked.
		/// </summary>
		Checked
	}
	#endregion

	#region PictureLayoutPosition enum
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// CmPicture.LayoutPos values
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum PictureLayoutPosition
	{
		/// <summary>
		/// No text wrapping around picture, shrinks picture proportionately if wider than
		/// the column, caption can occupy full column width
		/// </summary>
		CenterInColumn,
		/// <summary>
		///  Top or bottom of page only, no text wrapping around picture, shrinks picture
		///  proportionately if wider than the column, caption can occupy full page width
		/// </summary>
		CenterOnPage,
		/// <summary>
		/// Text wraps to the left of picture, caption can occupy same width as picture
		/// </summary>
		RightAlignInColumn,
		/// <summary>
		/// Text wraps to the right of picture, caption can occupy same width as picture
		/// </summary>
		LeftAlignInColumn,
		/// <summary>
		/// Grows picture if necessary to fill column width, caption can occupy full column width
		/// </summary>
		FillColumnWidth,
		/// <summary>
		/// Top or bottom of page only, grows picture if necessary to fill page width,
		/// caption can occupy full page width
		/// </summary>
		FillPageWidth,
		/// <summary>
		/// Picture occupies full page, grows/shrinks picture if necessary
		/// </summary>
		FullPage,
	}
	#endregion

	#region PictureLocationRangeType enum
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// CmPicture.LayoutPos values (indicates the type of data contained in LocationMin and
	/// LocationMax)
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum PictureLocationRangeType
	{
		/// <summary>
		/// LocationMin and LocationMax are ignored, and the picture must lay out on the
		/// line following the ORC, even if this results in a gap
		/// </summary>
		AfterAnchor,
		/// <summary>
		/// LocationMin and LocationMax contain references having the format
		/// BBCCCVVV, which encodes book, chapter, and verse
		/// </summary>
		ReferenceRange,
		/// <summary>
		/// LocationMin and LocationMax represent a number of paragraphs before or after
		/// the paragraph containing the ORC
		/// </summary>
		ParagraphRange,
	}
	#endregion

	#region TypeOfImport enum
	/// -----------------------------------------------------------------------------------
	/// <summary>
	/// Supported Scripture import types
	/// </summary>
	/// -----------------------------------------------------------------------------------
	public enum TypeOfImport
	{
		/// <summary>Undefined</summary>
		Unknown = 0,
		/// <summary>Settings describe how to import from Paratext 6</summary>
		Paratext6,
		/// <summary>Settings describe how to import from a non-Paratext Standard Format
		/// project</summary>
		Other,
		/// <summary>Settings describe how to import from Paratext 5 (or Paratext files
		/// when Paratext is not installed</summary>
		Paratext5,
	}
	#endregion

	#region OverwriteType enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Options for whether/how a book can be overwritten with a saved version
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum OverwriteType
	{
		/// <summary>Full overwrite is possible without losing data</summary>
		FullNoDataLoss,
		/// <summary>Overwrite would lose data</summary>
		DataLoss,
		/// <summary>Partial overwrite is possible</summary>
		Partial,
	}
	#endregion

	#region DiffType enumeration
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// ScrDraft.Type values
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum ScrDraftType
	{
		/// <summary>Normal saved version</summary>
		SavedVersion,
		/// <summary>Saved version created to store imported books</summary>
		ImportedVersion,
	};
	#endregion

	#region EntryType enum
	/// <summary>
	/// Entry type enumeration.
	/// </summary>
	/// <remarks>
	/// In the database, these correspond to different subclasses of LexEntry,
	/// except for ketUnspecified.
	/// </remarks>
	public enum EntryType
	{
		/// <summary>User may want to create a major entry.</summary>
		ketMajorEntry,
		/// <summary>User may want to create a subentry.</summary>
		ketSubentry,
		/// <summary>User may want to create a minor entry.</summary>
		ketMinorEntry
	}
	#endregion

	#region MsaType enum
	/// <summary>
	/// An enumeration of types of MSAs.
	/// </summary>
	public enum MsaType
	{
		// Note: These just help some code.
		/// <summary></summary>
		kNotSet,
		/// <summary></summary>
		kMixed,
		/// <summary></summary>
		kRoot,

		// Note: These correspond to class types.
		/// <summary></summary>
		kStem,
		/// <summary></summary>
		kDeriv,
		/// <summary></summary>
		kInfl,
		/// <summary></summary>
		kUnclassified
	}
	#endregion

	#region SpecialWritingSystemCodes enum
	/// <summary></summary>
	public enum SpecialWritingSystemCodes
	{
		/// <summary></summary>
		DefaultAnalysis = -1000,
		/// <summary></summary>
		DefaultVernacular = -1001,
		/// <summary></summary>
		BestAnalysis = -1002,
		/// <summary></summary>
		BestVernacular = -1003,
		/// <summary></summary>
		BestAnalysisOrVernacular = -1004,
		/// <summary></summary>
		BestVernacularOrAnalysis = -1005
	}
	#endregion

	#region FldReq enum
	/// <summary>
	/// Required field possibilities
	/// </summary>
	public enum FldReq
	{
		/// <summary>Field is Not Required</summary>
		kFTReqNotReq = 0,
		/// <summary>Field is Encouraged</summary>
		kFTReqWs,
		/// <summary>Field is Required</summary>
		kFTReqReq,
		//kFTReqLim		//Count of FldReq
	}
	#endregion

	#region SpellingStatusStates enum
	/// <summary>
	/// Values for spelling status of WfiWordform.
	/// </summary>
	public enum SpellingStatusStates
	{
		/// <summary>
		/// dunno
		/// </summary>
		undecided,
		/// <summary>
		/// well-spelled
		/// </summary>
		correct,
		/// <summary>
		/// no good
		/// </summary>
		incorrect
	}
	#endregion

	#region Sync Messages
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Enum listing types of change being made for synchronization purposes. These messages are
	/// used to indicate the type of change that was made.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum SyncMsg
	{
		/// <summary>
		/// Writing system change. Practically everything needs to be reloaded to reflect new
		/// writing system.
		/// </summary>
		ksyncWs,
		/// <summary>
		/// Add/Modify/Delete a style. hvo and flid are unused.
		/// </summary>
		ksyncStyle,
		/// <summary>
		/// Refresh everything. hvo and flid are unused.
		/// </summary>
		ksyncFullRefresh,
		/// <summary>
		/// We have issued an undo/redo. hvo and flid are unused. At some point this
		/// should be made more powerful so it only does what is necessary.
		/// </summary>
		ksyncUndoRedo,
	};

	#endregion

	#region SpecialHVOValues
	/// <summary>
	/// Defines HVOs with special meanings
	/// </summary>
	public enum SpecialHVOValues
	{
		/// <summary>No Hvo has been set</summary>
		kHvoUninitializedObject = -1,
		/// <summary>Underlying object was deleted</summary>
		kHvoObjectDeleted = -2
	};
	#endregion

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
	};
	#endregion

	#region ImportDomain enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Used to indicate the source of import data.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum ImportDomain
	{
		/// <summary>
		/// Indicates the primary Scripture stream. Use this if Scripture is interleaved with
		/// BT and/or annotations.
		/// </summary>
		Main,
		/// <summary>Back translation</summary>
		BackTrans,
		/// <summary>Annotations, such as consultant notes</summary>
		Annotations,
	}
	#endregion

	#region FileFormatType enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// valid values for FileFormat
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum FileFormatType
	{
		/// <summary>non-Paratext SF markup (e.g., from Toolbox)</summary>
		Other = 0,
		/// <summary>Paratext markup</summary>
		Paratext
	}
	#endregion

	#region MappingSet enum

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Enumeration for the different groups of mappings owned by a ScrImportSet object
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum MappingSet
	{
		/// <summary>List of mappings used for vernacular Scripture and BT</summary>
		Main,
		/// <summary>List of mappings used for Annotations</summary>
		Notes
	}

	#endregion MappingSet enum

	#region ContextValues
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum ContextValues
	{
		/// <summary></summary>
		General,
		/// <summary></summary>
		Annotation,
		/// <summary></summary>
		BackMatter,
		/// <summary></summary>
		BackTranslation,
		/// <summary></summary>
		Book,
		/// <summary>This MUST match the value of knContextInternal in FmtGenDlg.cpp</summary>
		Internal,
		/// <summary>This MUST match the value of knContextInternalMappable in FmtGenDlg.cpp</summary>
		InternalMappable,
		/// <summary></summary>
		Intro,
		/// <summary></summary>
		Note,
		/// <summary></summary>
		Publication,
		/// <summary></summary>
		Text,
		/// <summary></summary>
		Title,
		/// <summary>Not a real Context!</summary>
		EndMarker,
		/// <summary></summary>
		IntroTitle,
		/// <summary>
		/// This one is currently used by Flex, not TE. It is a style which the user may select,
		/// but only for purposes of configuring a view (e.g., Dictionary view), not for general
		/// use within a string. This allows us to define these styles externally and in
		/// parallel with a particular default version of the view, without having to do
		/// data migration.
		/// </summary>
		InternalConfigureView,
		/// <summary>Not a real style at all (used to support export settings)</summary>
		PsuedoStyle,
	}
	#endregion

	#region StructureValues enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum StructureValues
	{
		/// <summary></summary>
		Undefined,
		/// <summary></summary>
		Heading,
		/// <summary></summary>
		Body,
	}
	#endregion

	#region FunctionValues enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum FunctionValues
	{
		/// <summary></summary>
		Prose,
		/// <summary></summary>
		Line,
		/// <summary></summary>
		List,
		/// <summary></summary>
		Table,
		/// <summary></summary>
		Chapter,
		/// <summary></summary>
		Verse,
		/// <summary>This is needed to qualify internalMappable styles that are only valid for
		/// use in footnotes</summary>
		Footnote,
		/// <summary></summary>
		StanzaBreak,
	}
	#endregion

	#region KeyTermRenderingStatus enum
	/// --------------------------------------------------------------------------------
	/// <summary>
	/// These values are tied to the Status field of ChkRef objects.  See TE-6475.
	/// </summary>
	/// --------------------------------------------------------------------------------
	public enum KeyTermRenderingStatus
	{
		/// <summary>A rendering has not been assigned yet</summary>
		Unassigned = 0,
		/// <summary>A rendering has been assigned to the key term</summary>
		Assigned = 1,
		/// <summary>No rendering exists, and it has been explicitly ignored</summary>
		Ignored = 2,
		/// <summary>A rendering was automatically assigned as the result of assigning
		/// a rendering to another instance of the key term.</summary>
		AutoAssigned = 3,
		/// <summary>A rendering that was previously assigned but now missing
		/// from the verse</summary>
		Missing = 4,
	}
	#endregion

	#region NoteStatus enum
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// NoteStatus values
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum NoteStatus
	{
		/// <summary>Open</summary>
		Open,
		/// <summary>Closed</summary>
		Closed,
	}
	#endregion

	#region ScrCheckRunResult enum
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// ScrCheckRun.Result values
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum ScrCheckRunResult
	{
		/// <summary>Check found no inconsistencies</summary>
		NoInconsistencies,
		/// <summary>Check found only ignored inconsistencies</summary>
		IgnoredInconsistencies,
		/// <summary>Check found inconsistencies</summary>
		Inconsistencies
	}
	#endregion

	#region enum FootnoteMarkerTypes
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Values to classify types of footnote markers.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum FootnoteMarkerTypes
	{
		/// <summary>Auto generated footnote marker</summary>
		AutoFootnoteMarker,
		/// <summary>Symbolic footnote marker</summary>
		SymbolicFootnoteMarker,
		/// <summary>No footnote marker</summary>
		NoFootnoteMarker
	}
	#endregion

	#region BindingSide enum
	/// <summary>
	/// Enumeration indicating the binding edge of a publication
	/// </summary>
	public enum BindingSide
	{
		/// <summary>Left-bound.</summary>
		Left = 0,
		/// <summary>Right-bound.</summary>
		Right = 1,
		/// <summary>Top-bound.</summary>
		Top = 2
	};
	#endregion

	#region MultiPageLayout enum
	/// <summary>
	/// Enumeration indicating how to lay out multiple pages
	/// </summary>
	public enum MultiPageLayout
	{
		/// <summary>single-sided publication.</summary>
		Simplex = 0,
		/// <summary>double-sided publication</summary>
		Duplex = 1,
		/// <summary>booklet publication</summary>
		Booklet = 2
	};
	#endregion

	#region DivisionStartOption
	/// <summary>
	/// Enumeration of options for where the content of the division begins
	/// </summary>
	public enum DivisionStartOption
	{
		/// <summary>Division continues on remainder of page of previous division, if any</summary>
		Continuous,
		/// <summary>Division starts on new page</summary>
		NewPage,
		/// <summary>Division starts on a new odd page (forces blank page if necessary)</summary>
		OddPage,
	}
	#endregion

	#region Opinions enum
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// An agent can have no opinion, approve, or disapprove of analysis.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum Opinions
	{
		/// <summary></summary>
		disapproves = 0,
		/// <summary></summary>
		approves = 1,
		/// <summary></summary>
		noopinion = 2
	};
	#endregion

	#region OutlineNumSty enum
	/// <summary>
	/// Outline numbering styles
	/// </summary>
	public enum OutlineNumSty
	{
		/// <summary>No numbering.</summary>
		konsNone = 0,
		/// <summary>Numbers only (1, 1.1, 1.1.1)</summary>
		konsNum = 1,
		/// <summary>Numbers with dot at the end (1., 1.1., 1.1.1.)</summary>
		konsNumDot = 2,
		//konsLim,
	}
	#endregion

	#region PossNameType enum
	/// <summary>
	///
	/// </summary>
	public enum PossNameType
	{
		// Code in PossChsrDlg::LoadDlgSettings assumes these
		/// <summary> </summary>
		kpntName = 0,
		/// <summary> </summary>
		kpntNameAndAbbrev = 1,
		/// <summary> </summary>
		kpntAbbreviation = 2,
		//kpntLim,
	}
	#endregion

	#region ComparisionTypes enum
	/// <summary>The comparison options for cells of a filter</summary>
	public enum ComparisonTypes
	{
		/// <summary> </summary>
		kUndefined,
		/// <summary> </summary>
		kEquals,
		/// <summary> </summary>
		kGreaterThanEqual,
		/// <summary> </summary>
		kLessThanEqual,
		/// <summary> </summary>
		kMatches,
		/// <summary> </summary>
		kEmpty,
	}
	#endregion

	#region ClauseTypes enum
	/// <summary>
	/// Types of clauses in a discourse chart.
	/// </summary>
	public enum ClauseTypes
	{
		/// <summary/>
		Normal = 0,
		/// <summary/>
		Dependent = 1,
		/// <summary/>
		Song = 2,
		/// <summary/>
		Speech = 3
	}
	#endregion
}
