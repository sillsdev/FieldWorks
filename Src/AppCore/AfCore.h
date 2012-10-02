/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfCore.h
Responsibility: Shon Katzenberger
Last reviewed:

	Main header file for the application framework.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AfCore_H
#define AfCore_H 1


// Stolen from MFC's Afxres.h.
#define RT_TOOLBAR  MAKEINTRESOURCE(241)

#define kdzptInch 72
#define kdzmpInch 72000

// Define the current database version:
#include "DbVersion.h"

// Record to store prop values.
// For dimensions, initially we store values that are millipoints, as expected by MergeIntProp
// and stored in the ttp.
// Then, we convert to the unit stored used by the dialog, so we can compare new and old, and
// see whether it changed.
// Then, if it changed, we convert back to millipoints to save in the new ttp.
struct ParaPropRec
{
	int nRtl;	// bool, or knConflicting
	int atAlignType;
	int mpLeadInd;
	int mpTrailInd;
	int nLineHeightVar; // Used to store the variation of line height.
	int mpLineHeightVal; // Used to store value of line height: may be mp, thin, or 100th %.
	int mpSpacBef;
	int mpSpacAft;
	int mpSpIndBy;
	int clrBkgdColor;
	int spindSpIndent;
	int lnspSpaceType;

	// Are the above values explicit, inherited, or conflicting?
	int xRtl;
	int xAlign;
	int xLeadInd;
	int xTrailInd;
	int xLnSp;
	int xSpacBef;
	int xSpacAft;
	int xSpInd;
	int xBkgdColor;

}; // Hungarian xpr.

struct TagFlids
{
	bool fRecurse;
	int clid;			// Class id for root object owner. May be 0. (Input 'param')
	int flidRoot;		// flid for the root object owner. (output 'param')
	int flidMain;		// flid for the main property. (output 'param')
	int flidCreated;	// flid for the created property, or 0, if none. (output 'param')
	int flidModified;	// flid for the modified property, or 0, if none. (output 'param')
	int flidSubitems;	// flid for subitems, or 0, if none. (output 'param')
}; // Hungarian tf.


// JT: took this from AfApp.h to avoid a circular dependency.
typedef enum
{
	kninches = 0,
	knmm = 1,
	kncm = 2,
	knpt = 3,
} MsrSysType;

// This enum is used for menu items and toolbar buttons. Multiple strings for each command are
// stored together in the same string resource. See AfUtil::GetResourceStr for more information.
// SMc: took this from AfApp.h to allow use in AfUtil.h/cpp
typedef enum
{
	krstHoverEnabled,
	krstHoverDisabled,
	krstStatusEnabled,
	krstStatusDisabled,
	krstWhatsThisEnabled,
	krstWhatsThisDisabled,
	krstItem,
} ResourceStringType;


// UserViewType defn moved to VwOleDbDa.h

// Enumerates fragment types for use by VwCustomVc and it subclasses.
enum
{
	kfrcdRoot = 0, // the main root object.
	kfrcdMainItem, // a top level item in the list.
	kfrcdSubItem, // a sub item in the list. Similar, but requires item index.
	kfrcdPliName, // a possibility list item shown by name (may wrap).
	kfrcdPliNameOne, // a possibility list item shown by name on one line
	kfrcdTagSeq, // a sequence of CmPossibility items shown by name.
	kfrcdObjRefSeq, // A reference to a sequence of non-CmPossibility objects.
	kfrcdObjName, // A non-possibility list item shown by name (may wrap).
	kfrcdObjNameOne, // A non-possibility list item shown by name on one line.
	kfrcdMlaVern,	// A multi-lingual vernacular alternative.
	kfrcdMlaAnal,	// A multi-lingual analysis alternative.
	// RN items
	kfrcdEvnParticipants, // The RnEvent_Participants sequence.
	kfrcdRpsParticipants, // The RnRoledPartic_Participants sequence.
	kfrcdPliRole, // Used for RnRoledPartic_Role to avoid switching between name and abbr.
	// CLE items
	kfrcdAffixSlots,	// a sequence of AffixSlot objects shown by name.
	kfrcdAffixTemplates,	// a sequence of AffixTemplate objects shown by name.
	// LexEd items
	kfrcdAllomorphs,	// a sequence of MoForm objects, as in LexEntry_Allomorphs.
	kfrcdPliNameOnePrimary, // like kfrcdPliNameOne, but ones not the primary key are grey
	// Add others here.
};

/*------------------------------------------------------------------------------------------
	Structure of RT_TOOLBAR resource.
	Hungarian: tbd.
------------------------------------------------------------------------------------------*/
struct ToolBarData
{
	ushort suVer; // Version # should be 1.
	ushort dxsBmp; // Width of one bitmap.
	ushort dysBmp; // Height of one bitmap.
	ushort ccid; // Number of command ids.
	ushort rgcid[1]; // Array of command ids, actual size is ccid.
};

#include <Oledb.h>
#include <Msdasc.h>
#include <Oledberr.h>

// OLE DB interface pointer typedefs.
typedef ComSmartPtr<IDBInitialize> IDBInitializePtr;
typedef ComSmartPtr<IDataInitialize> IDataInitializePtr;
typedef ComSmartPtr<IDBProperties> IDBPropertiesPtr;
typedef ComSmartPtr<IDBCreateCommand> IDBCreateCommandPtr;
typedef	ComSmartPtr<IDBCreateSession> IDBCreateSessionPtr;
typedef	ComSmartPtr<IDBCreateCommand> IDBCreateCommandPtr;
typedef	ComSmartPtr<ICommandText> ICommandTextPtr;
typedef	ComSmartPtr<ICommand> ICommandPtr;
typedef	ComSmartPtr<IRowset> IRowsetPtr;
typedef	ComSmartPtr<IAccessor> IAccessorPtr;

class StVc;
typedef GenSmartPtr<StVc> StVcPtr;

// These are needed for ActiveX container stuff to work.
#define _ATL_APARTMENT_THREADED
#undef _ATL_FREE_THREADED
#include <atlbase.h>
extern CComModule _Module;
#include <atlwin.h>

// HTML web interfaces
#include <Exdisp.h>
#include <HtmlHelp.h>
typedef ComSmartPtr<IWebBrowser2> IWebBrowser2Ptr;

// Windows shell COM stuff:
#include <shlobj.h>


const achar * const kpszCleProgId = _T("SIL.ListEditor");


/***********************************************************************************************
	Additional view library headers.
***********************************************************************************************/
// From Notebk/main.h.
#define HVO long

typedef Vector<HVO> HvoVec;

// defn of HvoClsid & HvoClsidVec moved to VwOleDbDa.h because needed indep of AppCore.

/*----------------------------------------------------------------------------------------------
	This data structure stores the database ids (HVOs) for the sort key of one sorted record.
	Hungarian: skh
----------------------------------------------------------------------------------------------*/
struct SortKeyHvos
{
	HVO m_hvoPrimary;
	HVO m_hvoSecondary;		// May be zero (null).
	HVO m_hvoTertiary;		// May be zero (null).
};
typedef Vector<SortKeyHvos> SortKeyHvosVec;

#include "FwKernelTlb.h"
//#include "DbAccessTlb.h"		// subsumed by FwKernelTlb.h
//#include "DbServicesTlb.h"		// subsumed by FwKernelTlb.h
//#include "LanguageTlb.h"		// subsumed by FwKernelTlb.h
//#include "MigrateDataTlb.h"	// subsumed by ViewsTlb.h

typedef ComSmartPtr<IOleDbEncap> IOleDbEncapPtr;
typedef ComSmartPtr<IOleDbCommand> IOleDbCommandPtr;

// These defines are in ViewTlb.idl, but for some reason not copied to ViewTlb.h
// IVwObject is obsolete. Gradually we are replacing it with HVO.
#define HVO long // Hungarian hvo (Handle to Viewable Object).
#define PropTag int

#include "ViewsTlb.h"
#include "VwBaseDataAccess.h"
#include "VwCacheDa.h"
#include "VwBaseVc.h"

// CustViewDa references kflidCmPossibility_Name.
// GeneralPropDlg references kflidCmMajorObject_Date(Created,Modified)
// AfApp references kflidCmMajorObject_xxxx, kflidCmProject_Name, kflidCmPossibilityList_xxxx,
//      and kflidCmPossibility_xxxx
// other basic Cellar field ids are also referenced by the application framework.
#include "FwCellarTlb.h"

// This is needed before CustViewDa.h, which currently knows about some specific
// conceptual models. At least the Notebk.sqh should be possible to remove eventually.
// AfStyleSheet references kflidLangProject_Styles
// ENHANCE JohnT: clean up further.
typedef enum NotebookModuleDefns
{
	#define CMCG_SQL_ENUM 1
	#include "LangProj.sqh" // So AfStyleSheet can know about kflidLangProject_Styles.
	#include "Notebk.sqh"
	#undef CMCG_SQL_ENUM
} NotebookModuleDefns;


// Enum listing types of change being made for synchronization purposes. These messages are
// used in SyncInfo.msg to indicate the type of change that was made. The usage of SyncInfo.hvo
// and SyncInfo.flid varies depending on the message.
typedef enum
{
	ksyncNothing = 0, // Used to indicate nothing needs synchronization.
	ksyncWs, // Writing system change. Practically everything needs to be reloaded to reflect new
				// writing system. hvo and flid are unused.
	ksyncPossList, // A possibility list was changed (name/abbr, ordering) and should be
				// reloaded. hvo is list id, and flid is unused.
	ksyncAddPss, // A new possibility item was added. hvo is list id, flid really the hvo
				// of the added item.
	ksyncDelPss, // A possibility item was deleted. hvo is list id, flid is really the
				// hvo of the deleted item.
	ksyncMergePss, // Two possibility items were merged. hvo is list id, flid is unused.
	ksyncSimpleEdit, // Edited string. hvo is string owner, flid is string property.
	ksyncAddEntry, // Add new major/subentry. hvo is root object of window, flid is really
				// the hvo of the new object.
	ksyncDelEntry, // Delete major/subentry. hvo is root object of window, flid is really
				// the hvo of the new object.
	ksyncMoveEntry, // A major/subentry was moved. hvo is root object of window, flid is really
				// the hvo of the moved object.
	ksyncPromoteEntry, // A subentry was promoted. hvo is the major object of the tool (e.g.,
				// Notebook and flid is unused.
	ksyncStyle, // Add/Modify/Delete a style. hvo and flid are unused.
	ksyncCustomField, // Add/Modify/Delete a custom field. hvo and flid are unused.
	ksyncUserViews, // Add/Modify/Delete a user view. hvo and flid are unused.
	ksyncPageSetup, // Modified Page Setup information. Each window needs to have page headings
				// reloaded. hvo = root object for window. flid is unused.
	ksyncOverlays, // Modified overlays. hvo is ??? and flid is ???
	ksyncHeadingChg, // A Language project or major object name changed. All headings need
				// need to be reloaded. hvo and flid are unused.
	ksyncFullRefresh, // Refresh everything. hvo and flid are unused.
	ksyncUndoRedo, // We have issued an undo/redo. hvo and flid are unused. At some point this
				// should be made more powerful so it only does what is necessary.
	ksyncLim,
} SyncMsg;

// Structure used when accessing data from Synch$ table in database as well as used for
// updating various windows and caches in the current application.
// Hungarian: sync.
typedef struct SyncInfo
{
	SyncInfo()
	{
		msg = 0;
		hvo = 0;
		flid = 0;
	}
	SyncInfo(int msgIn, HVO hvoIn, int flidIn)
	{
		msg = msgIn;
		hvo = hvoIn;
		flid = flidIn;
	}
	int msg; // Message indicating type of change.
	HVO hvo; // Item being changed.
	int flid; // Field being changed.
} SyncInfo;

// Page orientation types.
enum POrientType {kPort=0, kLands};

// A page size may be any one of three possible standard sizes.
enum PgSizeType
{
	kSzLtr = 0,			//letter page size
	kSzLgl,				//legal page size
	kSzA4,				//A4 page size
	kSzCust				//custom page size
};

using namespace fwutil;	// Rect and Point classes

/***********************************************************************************************
	Application Framework headers.
***********************************************************************************************/
#include "AfDef.h"
#include "AfGfx.h"
#include "AfCmd.h"
#include "AfMenuMgr.h"
#include "AfWnd.h" // Before AfTagOverlay.
#include "FwStyledText.h" // Before AfDialog.
#include "AfDialog.h" // Before AfTagOverlay.
#include "AfSplitter.h" // Before AfVwWnd.h.
#include "UtilView.h" // Before AfVwWnd.h"
#include "OrientationManager.h" // before AfVwWnd.h"
#include "AfVwWnd.h" // Before AfTagOverlay.
#include "TssWidgets.h" // Before AfBars.
#include "FilPgSetDlgRes.h" // Before FilPgSetDlg.
#include "FilPgSetDlg.h" // Before AfMainWnd.
#include "TlsListsDlgRes.h" // Before TlsListsDlg.
#include "TlsListsDlg.h" // Before AfMainWnd.
#include "TlsStatsDlgRes.h" // Before AfMainWnd.
#include "TlsStatsDlg.h" // Before AfMainWnd.


#include "AfUtil.h" // Before AfApp.h
#include "AfDbInfo.h" // Before AfApp.h, #includes VwOleDbDa.h
#include "DelObjUndoAction.h" // used by VwOleDbDa.cpp
#include "AfApp.h"
#include "DbStringCrawler.h" // Before CustViewDa
#include "FwDbChangeOverlayTags.h" // Before CustViewDa
#include "CustViewDa.h" // Before AfStylesheet.

#include "AfBars.h"

#include "AfStylesheet.h" // Before AfStylesDlg and VwUndo
#include "VwUndo.h"
#include "FmtGenDlgRes.h" // Before AfStylesDlg.
#include "AfColorTable.h" // Before UiColor.
#include "UiColor.h" // Before FmtFntDlg and FmtParaDlg.
#include "FmtBdrDlgRes.h" // Before FmtBdrDlg.
#include "FmtBdrDlg.h" // Before AfMainWnd.
#include "FmtFntDlgRes.h" // Before FmtFntDlg.
#include "FmtFntDlg.h" // Before AfStylesDlg.
#include "FmtParaDlgRes.h" // Before FmtParaDlg.
#include "FmtParaDlg.h" // Before AfStylesDlg.
#include "FmtGenDlg.h" // Before AfStylesDlg.
#include "AfStylesDlgRes.h" // Before AfStylesDlg.
#include "AfStylesDlg.h" // Before AfMainWnd.
#include "FmtBulNumDlgRes.h" // Before FmtBulNumDlg.
#include "FmtBulNumDlg.h" // Before AfMainWnd.
#include "AfMainWnd.h"
#include "AfContextHelp.h"
#include "AfAppRes.h"
#include "TreeDragDrop.h" // Before PossChsrDlg
#include "PossChsrDlg.h" // Needed by AfTagOverlay.h.
#include "PossChsrDlgRes.h"
#include "AfTagOverlay.h"
#include "AfTagOverlayRes.h"
#include "RecMainWndRes.h"
#include "RecMainWndSupportWnds.h"
#include "AfHeaderWnd.h"
#include "AfWizardDlg.h"
#include "AfWizardDlgRes.h"
#include "FwFilterRes.h"
#include "FwFilterDlg.h"
#include "AfSortMethodRes.h"
#include "TlsOptDlgRes.h"
#include "TlsOptCustRes.h"
#include "TlsOptGenRes.h"
#include "TlsOptViewRes.h"
#include "TlsOptDlg.h"
#include "TlsOptCust.h"
#include "TlsOptGen.h"
#include "TlsOptView.h"
#include "RecMainWnd.h"
#include "ClientWindows.h"
#include "IconCombo.h"
#include "AfSysLangList.h"
#include "AfSortMethod.h"
#include "FmtWrtSysRes.h"	// Before FmtWrtSysDlg.h
#include "FmtWrtSysDlg.h"
#include "AfFindDlgRes.h"
#include "AfFindDlg.h"
#include "CmDataObject.h"
#include "AfAnimateCtrl.h"
#include "GeneralPropDlgRes.h"
#include "GeneralPropDlg.h"
#include "ListsPropDlg.h"
#include "ListsPropDlgRes.h"
#include "AfProgressDlgRes.h"
#include "AfProgressDlg.h"
#include "AfChangeWatcher.h"
#include "AfPrjNotFndDlgRes.h"
#include "AfPrjNotFndDlg.h"
#include "WriteXml.h"
#include "AfCustomExport.h"
#include "MiscDlgsRes.h"
#include "MiscDlgs.h"
#include "DlgProps.h"

#endif // !AfCore_H
