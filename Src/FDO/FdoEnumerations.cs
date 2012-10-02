// --------------------------------------------------------------------------------------------
// Copyright (C) 2002 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: FdoEnumerations.cs
// Responsibility: Randy Regnier
// Last reviewed: never
//
//
// <remarks>
// Implementation of various enumerations used in FDO.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.FDO
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// An agent can have no opinion, approve, or disapprove of analysis.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum Opinions
	{
		/// <summary></summary>
		approves,
		/// <summary></summary>
		disapproves,
		/// <summary></summary>
		noopinion
	};

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// ResolutionStatus values
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum NoteStatus
	{
		/// <summary>Open</summary>
		Open,
		/// <summary>Closed</summary>
		Closed,
	}

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

	#region FieldType enum
	/// <summary>
	/// Types of fields and corresponding IDs in database. This is partial, and not generated.
	/// More can be found at CellarModuleDefns
	/// </summary>
	public enum FieldType
	{
		/// <summary> </summary>
		kcptNil = 0,
		/// <summary> </summary>
		kcptBoolean = 1,
		/// <summary> </summary>
		kcptInteger = 2,
		/// <summary> </summary>
		kcptNumeric = 3,
		/// <summary> </summary>
		kcptFloat = 4,
		/// <summary> </summary>
		kcptTime = 5,
		/// <summary> </summary>
		kcptGuid = 6,
		/// <summary> </summary>
		kcptImage = 7,
		/// <summary> </summary>
		kcptGenDate = 8,
		/// <summary> </summary>
		kcptBinary = 9,

		/// <summary> </summary>
		kcptString = 13,
		/// <summary> </summary>
		kcptMultiString = 14,
		/// <summary> </summary>
		kcptUnicode = 15,
		/// <summary> </summary>
		kcptMultiUnicode = 16,
		/// <summary> </summary>
		kcptBigString = 17,
		/// <summary> </summary>
		kcptMultiBigString = 18,
		/// <summary> </summary>
		kcptBigUnicode = 19,
		/// <summary> </summary>
		kcptMultiBigUnicode = 20,

		/// <summary>The first object type (non-basic)</summary>
		kcptMinObj = 23,

		/// <summary> </summary>
		kcptOwningAtom = 23,
		/// <summary> </summary>
		kcptReferenceAtom = 24,
		/// <summary> </summary>
		kcptOwningCollection = 25,
		/// <summary> </summary>
		kcptReferenceCollection = 26,
		/// <summary> </summary>
		kcptOwningSequence = 27,
		/// <summary> </summary>
		kcptReferenceSequence = 28,


		// if the above values are changed these values need to be recalculated; these values
		// are 1 shifted to the left the number of bits equal to the corresponding property
		// type constant; ex. kfcptOwningAtom = 1 << kcptOwningAtom
		/// <summary> </summary>
		kfcptOwningAtom = 8388608,
		/// <summary> </summary>
		kfcptReferenceAtom = 16777216,
		/// <summary> </summary>
		kfcptOwningCollection = 33554432,
		/// <summary> </summary>
		kfcptReferenceCollection = 67108864,
		/// <summary> </summary>
		kfcptOwningSequence = 134217728,
		/// <summary> </summary>
		kfcptReferenceSequence = 268435456,
		/// <summary> </summary>
		kgrfcptOwning = 176160768,
		/// <summary> </summary>
		kgrfcptReference = 352321536,
		/// <summary> </summary>
		kgrfcptAll = 528482304,
		/// <summary> </summary>
		kfcptMultiString = 1 << kcptMultiString,
		/// <summary> </summary>
		kfcptMultiUnicode = 1 << kcptMultiUnicode,
		/// <summary> </summary>
		kfcptMultiBigString = 1 << kcptMultiBigString,
		/// <summary> </summary>
		kfcptMultiBigUnicode = 1 << kcptMultiBigUnicode,

		/// <summary> </summary>
		kfcptString = 1 << kcptString,
		/// <summary> </summary>
		kfcptUnicode = 1 << kcptUnicode,
		/// <summary> </summary>
		kfcptBigString = 1 << kcptBigString,
		/// <summary> </summary>
		kfcptBigUnicode = 1 << kcptBigUnicode,

		/// <summary> All multilingual string types</summary>
		kgrfcptMulti = kfcptMultiString | kfcptMultiUnicode | kfcptMultiBigString | kfcptMultiBigUnicode,
		/// <summary> All non-multilingual string types</summary>
		kgrfcptSimpleString = kfcptString | kfcptUnicode | kfcptBigString | kfcptBigUnicode,
		/// <summary> All string types, plain and multilingual</summary>
		kgrfcptString = kgrfcptMulti | kgrfcptSimpleString,

		/// <summary>special virtual bits</summary>
		kcptVirtualBit = 0xe0,
		/// <summary>virtual bit mask.</summary>
		kcptVirtualMask = 0x1f,
	}
	#endregion

	#region LinkedObjectType enum
	/// <summary>
	///
	/// </summary>
	public enum LinkedObjectType
	{
		/// <summary> </summary>
		Owning = 176160768,
		/// <summary> </summary>
		Reference = 352321536,
		/// <summary> </summary>
		OwningAndReference = 528482304
	}
	#endregion

	#region ReferenceDirection enum
	/// <summary>
	///
	/// </summary>
	public enum ReferenceDirection
	{
		// (0=both, 1=referenced by this/these object(s), -1 reference this/these objects)
		/// <summary> </summary>
		InboundAndOutbound = 0,
		/// <summary> </summary>
		Outbound = 1,
		/// <summary> </summary>
		Inbound = -1
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

	#region OLEDB enums
	/// <summary>
	///
	/// </summary>
	public enum DBPARAMFLAGSENUM
	{
		/// <summary> </summary>
		DBPARAMFLAGS_ISINPUT	= 0x1,
		/// <summary> </summary>
		DBPARAMFLAGS_ISOUTPUT	= 0x2,
		/// <summary> </summary>
		DBPARAMFLAGS_ISSIGNED	= 0x10,
		/// <summary> </summary>
		DBPARAMFLAGS_ISNULLABLE	= 0x40,
		/// <summary> </summary>
		DBPARAMFLAGS_ISLONG	= 0x80
	}

	/// <summary>
	///
	/// </summary>
	public enum DBTYPEENUM
	{
		/// <summary> </summary>
		DBTYPE_EMPTY	= 0,
		/// <summary> </summary>
		DBTYPE_NULL	= 1,
		/// <summary> </summary>
		DBTYPE_I2	= 2,
		/// <summary> </summary>
		DBTYPE_I4	= 3,
		/// <summary> </summary>
		DBTYPE_R4	= 4,
		/// <summary> </summary>
		DBTYPE_R8	= 5,
		/// <summary> </summary>
		DBTYPE_CY	= 6,
		/// <summary> </summary>
		DBTYPE_DATE	= 7,
		/// <summary> </summary>
		DBTYPE_BSTR	= 8,
		/// <summary> </summary>
		DBTYPE_IDISPATCH	= 9,
		/// <summary> </summary>
		DBTYPE_ERROR	= 10,
		/// <summary> </summary>
		DBTYPE_BOOL	= 11,
		/// <summary> </summary>
		DBTYPE_VARIANT	= 12,
		/// <summary> </summary>
		DBTYPE_IUNKNOWN	= 13,
		/// <summary> </summary>
		DBTYPE_DECIMAL	= 14,
		/// <summary> </summary>
		DBTYPE_UI1	= 17,
		/// <summary> </summary>
		DBTYPE_ARRAY	= 0x2000,
		/// <summary> </summary>
		DBTYPE_BYREF	= 0x4000,
		/// <summary> </summary>
		DBTYPE_I1	= 16,
		/// <summary> </summary>
		DBTYPE_UI2	= 18,
		/// <summary> </summary>
		DBTYPE_UI4	= 19,
		/// <summary> </summary>
		DBTYPE_I8	= 20,
		/// <summary> </summary>
		DBTYPE_UI8	= 21,
		/// <summary> </summary>
		DBTYPE_GUID	= 72,
		/// <summary> </summary>
		DBTYPE_VECTOR	= 0x1000,
		/// <summary> </summary>
		DBTYPE_RESERVED	= 0x8000,
		/// <summary> </summary>
		DBTYPE_BYTES	= 128,
		/// <summary> </summary>
		DBTYPE_STR	= 129,
		/// <summary> </summary>
		DBTYPE_WSTR	= 130,
		/// <summary> </summary>
		DBTYPE_NUMERIC	= 131,
		/// <summary> </summary>
		DBTYPE_UDT	= 132,
		/// <summary> </summary>
		DBTYPE_DBDATE	= 133,
		/// <summary> </summary>
		DBTYPE_DBTIME	= 134,
		/// <summary> </summary>
		DBTYPE_DBTIMESTAMP	= 135
	}
	#endregion	// OLEDB enums

	#region UserViewType enum
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// This lists the possible types of user views. Applications may use subsets of these.
	/// If an application needs a new type of view, it should be added to this list, as well
	/// as a definition for the string in the resources (e.g., kstidBrowse).
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public enum UserViewType
	{
		/// <summary>Browse View</summary>
		kvwtBrowse = 0,
		/// <summary>Data Entry View </summary>
		kvwtDE,
		/// <summary>Document View </summary>
		kvwtDoc,
		/// <summary>Concordance View </summary>
		kvwtConc,
		/// <summary>Draft View </summary>
		kvwtDraft,
		/// <summary>Print Layout View </summary>
		kvwtPrintLayout,
		/// <summary>Back Translation page layout view</summary>
		kvwtBackTransPrintLayout,
		/// <summary>Back Translation page layout view with 2 BTs side by side</summary>
		kvwtBackTransPrintLayoutSideBySide,
		/// <summary>Back Translation/draft split View </summary>
		kvwtBackTransDraftSplit,
		/// <summary>Back Translation draft view (no vern)</summary>
		kvwtBackTransReview,
		/// <summary>Key terms Checking View </summary>
		kvwtKeyTerms,
		/// <summary>Trial Publication View </summary>
		kvwtTrialPublication,
		/// <summary>Correction Layout View </summary>
		kvwtCorrectionLayout,
		/// <summary>Vertical version of a simplified scripture draft view.</summary>
		kvwtVertical,
		/// <summary>Editorial Checks View </summary>
		kvwtEditorialChecks,
		//kvwtLim			// Count of VwTypes
	}
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

	#region FldVis enum
	/// <summary>
	/// Visibility of fields
	/// </summary>
	public enum FldVis
	{
		/// <summary>Field is Always visible.</summary>
		kFTVisAlways = 0,
		/// <summary>Field is visible if it has data.</summary>
		kFTVisIfData,
		/// <summary>Field is never visible.</summary>
		kFTVisNever,
		//kFTVisLim			// Count of FldVis.
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
}
