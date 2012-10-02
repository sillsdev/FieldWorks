/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwFilterDlg.cpp
Responsibility: Steve McConnel (was Darrell Zook)
Last reviewed: Not yet.

Description:
	Implementation of the Tools/Options filter dialog and supporting dialogs for it.

	This file contains class definitions for the following classes:
		FilterUtil
		KeywordLookup
		DateKeywordLookup
		FwFilterDlg : AfDialogView
		FwFilterSimpleShellDlg : AfDialogView
		FwFilterBuilderShellDlg : AfDialog
		FwFilterSimpleDlg : AfDialogView
		FwFilterFullDlg : AfDialogView
		FwFilterFullShellDlg : AfDialog
		FwFilterHeader : AfWnd
		FwFilterPromptDlg : AfDialog
		FwFilterTipsDlg : AfDialog
		FwFilterNoMatchDlg : AfDialog
		FwFilterTurnOffDlg : AfDialog
		FwFilterLaunchBtn : AfWnd
		FwFilterPssEdit : AfWnd
		FwFilterStatic : AfWnd
		FwFilterButton : AfButton
		FilterVc : VwBaseVc
		FilterWnd : AfVwScrollWnd
		FromQueryBuilder
		WhereQueryBuilder

	NOTE: Any dialog design changes to the FwFilterSimpleDlg dialog need to also be made in the
		FwFilterPromptDlg dialog because they basically both have to show the same thing. The
		main difference is that an FwFilterSimpleDlg dialog has an extra combo box that lets
		the user change the filter condition.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
//:End Ignore

#undef VERSION2FILTER /* Marks items which are not implemented in Version 1 of Data Notebook. */
#define INCOMPLETE_GetCellSQL 1		// FLAG THAT THERE IS STILL WORK TO DO!!

uint s_wmConditionChanged = ::RegisterWindowMessage(_T("FwFilterSimpleDlg Condition Changed"));
uint s_wmBldrShellActivate = ::RegisterWindowMessage(_T("FwFilterBuilderShellDlg Activate"));

// Forward declaration of local function.
static bool ParseDate(StrUni & stuDate, int iselScope, int & nYear, int & nMonth, int & nDay,
	bool fSkipErrorMsg = false, HWND hwndParent = NULL);

//:>--------------------------------------------------------------------------------------------
//:>	Command maps.
//:>--------------------------------------------------------------------------------------------

BEGIN_CMD_MAP(FwFilterDlg)
	ON_CID_ME(kcidAddSimpleFilter, &FwFilterDlg::CmdAddFilter, NULL)
	ON_CID_ME(kcidAddFullFilter, &FwFilterDlg::CmdAddFilter, NULL)
	ON_CID_ALL(kcidFltrSpcExpand, &FwFilterDlg::CmdFltrSpcExpand, NULL)
	ON_CID_ALL(kcidFltrFmtFont, &FwFilterDlg::CmdFltrFormat, &FwFilterDlg::CmsFltrFormat)
	ON_CID_ALL(kcidFltrFmtStyle, &FwFilterDlg::CmdFltrFormat, &FwFilterDlg::CmsFltrFormat)
	ON_CID_ALL(kcidFltrFmtNone, &FwFilterDlg::CmdFltrFormat,
		&FwFilterDlg::CmsFltrFormat)
	ON_CID_ALL(kcidExpFindFmtWs, &FwFilterDlg::CmdFltrFmtWrtSys,
		&FwFilterDlg::CmsFltrFormat)
END_CMD_MAP_NIL()

BEGIN_CMD_MAP(FwFilterFullDlg)
	ON_CID_CHILD(kcidFullFilterPopupMenu, &FwFilterFullDlg::CmdFieldPopup, NULL)
	ON_CID_CHILD(kcidFullFilterDelCol, &FwFilterFullDlg::CmdDeleteCol,
		&FwFilterFullDlg::CmsDeleteCol)
END_CMD_MAP_NIL()

BEGIN_CMD_MAP(FwFilterSimpleShellDlg)
	ON_CID_CHILD(kcidSimpleFilterPopupMenu, &FwFilterSimpleShellDlg::CmdFieldPopup, NULL)
END_CMD_MAP_NIL()

BEGIN_CMD_MAP(FilterWnd)
	ON_CID_ME(kcidEditCut, &FilterWnd::CmdEditCut, &FilterWnd::CmsEditCut)
	ON_CID_ME(kcidEditPaste, &FilterWnd::CmdEditPaste, &FilterWnd::CmsEditPaste)
END_CMD_MAP_NIL()


//:>--------------------------------------------------------------------------------------------
//:>	Global variables and constants.
//:>--------------------------------------------------------------------------------------------

static FilterUtil g_fu;

StrUni KeywordLookup::s_stuKeywords;
StrUni DateKeywordLookup::s_stuKeywords;

int FwFilterHeader::s_icolContext;

enum
{
	kiselExactDate = 0,
	kiselMonthYear,
	kiselYear,
#ifdef VERSION2FILTER
	kiselMonth,
	kiselDay,
#endif /*VERSION2FILTER*/
	kcselDateNeeded
};

//:>********************************************************************************************
//:>	FwFilterXrefUtil methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Process a cross reference field for the From clause of a filter's constructed SQL query.

	@param flid Field id of the cross reference target.
	@param fJoin Flag whether this needs a new join clause.
	@param ialias Reference to the alias counter for the From clause under construction.
	@param ialiasLastClass The alias index for the most recent class encountered in the filter.
	@param pmdc Pointer to the database meta-data cache object.
	@param stuFromClause Reference to the from clause being constructed.
	@param stuWJoin Reference to the subordinate where/from/join clause used for negations.
	@param sbstrClass Reference to the class name of the cross reference target.
	@param sbstrField Reference to the field name of the cross reference target.
	@param stuAliasText Reference to an SQL alias string which is modified by this method.
	@param stuAliasId Reference to an SQL alias string which is modified by this method.

	@return True if cross references are valid, false if they are invalid.
----------------------------------------------------------------------------------------------*/
bool FwFilterXrefUtil::ProcessCrossRefColumn(int flid, bool fJoin, int & ialias,
	int ialiasLastClass, IFwMetaDataCache * pmdc, StrUni & stuFromClause, StrUni & stuWJoin,
	SmartBstr & sbstrClass, SmartBstr & sbstrField, StrUni & stuAliasText, StrUni & stuAliasId)
{
	return false;
}

/*----------------------------------------------------------------------------------------------
	Process a cross reference list field for the From clause of a filter's constructed SQL
	query.

	@param flid Field id of the cross reference target.
	@param ialias Reference to the alias counter for the From clause under construction.
	@param ialiasLastClass The alias index for the most recent class encountered in the filter.
	@param pmdc Pointer to the database meta-data cache object.
	@param stuFromClause Reference to the from clause being constructed.
	@param stuWJoin Reference to the subordinate where/from/join clause used for negations.
	@param sbstrClass Reference to the class name of the cross reference target.
	@param sbstrField Reference to the field name of the cross reference target.
	@param stuAliasText Reference to an SQL alias string which is modified by this method.
	@param stuAliasId Reference to an SQL alias string which is modified by this method.

	@return True if cross references are valid, false if they are invalid.
----------------------------------------------------------------------------------------------*/
bool FwFilterXrefUtil::ProcessCrossRefListColumn(int flid, int & ialias, int ialiasLastClass,
	IFwMetaDataCache * pmdc, StrUni & stuFromClause, StrUni & stuWJoin, SmartBstr & sbstrClass,
	SmartBstr & sbstrField, StrUni & stuAliasText, StrUni & stuAliasId)
{
	return false;
}

/*----------------------------------------------------------------------------------------------
	Fix the 'alias text' value for a cross reference field in the Where clause of a filter's
	constructed SQL Query.

	@param stuAliasText Reference to the SQL reference string which is modified by this method.
	@param flid Field id of the target of this filter.

	@return True if cross references are valid, false if they are invalid.
----------------------------------------------------------------------------------------------*/
bool FwFilterXrefUtil::FixCrossRefTitle(StrUni & stuAliasText, int flid)
{
	return false;
}


//:>********************************************************************************************
//:>	FilterUtil methods.
//:>********************************************************************************************

//:>--------------------------------------------------------------------------------------------
//:>	The following methods are all static methods on FilterUtil.
//:>--------------------------------------------------------------------------------------------

/*----------------------------------------------------------------------------------------------
	Load all filter information from the database for this application.

	@param pdbi Pointer to the application database information.
	@param pguidApp Pointer to application's GUID.

	@return True if successful, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FilterUtil::LoadFilters(AfDbInfo * pdbi, const GUID * pguidApp)
{
	AssertPtr(pdbi);

	// If we've already loaded the filters from the database, don't do it again.
	if (pdbi->GetFilterCount() != 0)
		return true;

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuQuery;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;

	//  Obtain pointer to IOleDbEncap interface.
	pdbi->GetDbAccess(&qode);

	try
	{
		CheckHr(qode->CreateCommand(&qodc));
		stuQuery.Format(L"SELECT id, name, type, columninfo, showprompt, prompttext, ClassId%n"
			L" FROM CmFilter WHERE app = '%g' ORDER BY name",
			pguidApp);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));

		// Read the filter information from the filter table in the database.
		while (fMoreRows)
		{
			int fltId;
			wchar rgchName[MAX_PATH];
			char fltType;
			wchar rgchColInfo[MAX_PATH];
			char fPrompt;
			wchar rgchPrompt[MAX_PATH];
			int clid;

			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&fltId),
				isizeof(fltId), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(rgchName),
				isizeof(rgchName), &cbSpaceTaken, &fIsNull, 2));
			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&fltType),
				isizeof(fltType), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(rgchColInfo),
				isizeof(rgchColInfo), &cbSpaceTaken, &fIsNull, 2));
			CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&fPrompt),
				isizeof(fltType), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(rgchPrompt),
				isizeof(rgchPrompt), &cbSpaceTaken, &fIsNull, 2));
			CheckHr(qodc->GetColValue(7, reinterpret_cast<BYTE *>(&clid),
				isizeof(clid), &cbSpaceTaken, &fIsNull, 0));

			pdbi->AddFilter(rgchName, fltType == 0, fltId, rgchColInfo, fPrompt != 0,
				rgchPrompt, clid);
			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)
	{
		return false;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Build the vector of columns representing a filter from the string representation of them.
	This is used when loading a filter from the database to know what field each of the columns
	refers to.  Columns have the following representation in the database:

		<class1>,<flid1>|<class2>,<flid2a>,<flid2b>[ws]|<class3>,<flid3>(<hvo3>),<offset3>

	The first number in each column is the clsid (kclid...).
	The numbers following the class give the path from the class down to the attribute the
	column is filtered on (kflid...).
	Since artificial subentries have been created for tags, the last number gives the offset
	in the group of tags.
	The flid numbers before the leaf node may be followed by an HVO enclosed in parenthesis.
	This allows a menu column to have multiple occurrences of the same flid, with the target hvo
	value given to distinguish among them.  The "Roled Participants" field of the Data Notebook
	filter menu inspired this hack.  (Note that -1 for the hvo means "don't care", and 0 means
	"is null".)
	The leaf node may be followed by an writing system value enclosed by brackets ([]).  This is
	only for multilingual text fields.

	@param plpi Pointer to the application language project information.
	@param stuColumns Reference to the input columns string.
	@param vvfmnColumns Reference to the output vector of filter column definition vectors,
					used to return a complete filter specification.
----------------------------------------------------------------------------------------------*/
bool FilterUtil::BuildColumnsVector(AfLpInfo * plpi, StrUni & stuColumns,
	Vector<FilterMenuNodeVec> & vvfmnColumns, AfMainWnd * pafw)
{
	AssertPtr(plpi);

	AssertPtr(pafw);
	FilterMenuNodeVec * pvfmnRoot = pafw->GetFilterMenuNodes(plpi);
	AssertPtr(pvfmnRoot);

	vvfmnColumns.Clear();

	wchar * prgch = (wchar *)stuColumns.Chars();
	wchar * prgchLim = prgch + stuColumns.Length() + 1;
	if (prgchLim == prgch + 1)
		return false; // Empty columns string.

	int flid = 0;
	int encT = 0;
	bool fComplex = false;
	int hvoT;
	FilterMenuNode * pfmn = NULL;
	FilterMenuNodeVec vfmn; // This is used to store the different numbers for one column.

	while (prgch < prgchLim)
	{
		if (iswdigit(*prgch))
		{
			// Build up the number.
			flid = flid * 10 + (*prgch - '0');
		}
		else if (flid && *prgch == '(')
		{
			hvoT = wcstol(prgch + 1, &prgch, 10);
			fComplex = true;
			Assert(prgch < prgchLim && *prgch == ')');
		}
		else
		{
			Assert(*prgch == ',' || *prgch == '|' || *prgch == 0);
			// pvfmnT points to the parent node we are searching through.
			FilterMenuNodeVec * pvfmnT;
			if (pfmn)
				pvfmnT = &pfmn->m_vfmnSubItems;
			else
				pvfmnT = pvfmnRoot;

			// Find the item that matches this number in the list of every possible node.
			int cfmn = pvfmnT->Size();
			int ifmn;
			for (ifmn = 0; ifmn < cfmn; ifmn++)
			{
				if (((*pvfmnT)[ifmn]->m_flid == flid) &&
					(!fComplex || (*pvfmnT)[ifmn]->m_hvo == hvoT))
				{
					break;
				}
			}
			if (ifmn >= cfmn)
			{
				vvfmnColumns.Clear();
				return false;
			}
			pfmn = (*pvfmnT)[ifmn];
			vfmn.Push(pfmn);

			flid = 0;
			hvoT = 0;
			fComplex = false;

			if (*prgch == '|' || *prgch == 0)
			{
				// We are at the end of a column.
				vvfmnColumns.Push(vfmn);
				vfmn.Clear();
				if (encT)
				{
					// Save the writing system somewhere, somehow!
					encT = 0;
				}
				pfmn = NULL;
			}
		}
		++prgch;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Build a string that identifies the columns in a table representing a filter. This is used
	when saving a filter to the database. See the description for BuildColumnsVector to see how
	columns are represented in the database.

	@param vvfmnColumns Reference to an input vector of filter column definition vectors.
	@param stuColumns Reference to the output string.
----------------------------------------------------------------------------------------------*/
void FilterUtil::BuildColumnsString(Vector<FilterMenuNodeVec> & vvfmnColumns,
	StrUni & stuColumns)
{
	stuColumns.Clear();

	StrUni stuColumn;
	int cvfmn = vvfmnColumns.Size();
	for (int ivfmn = 0; ivfmn < cvfmn; ivfmn++)
	{
		// Loop through each column.
		stuColumn.Clear();
		FilterMenuNodeVec & vfmn = vvfmnColumns[ivfmn];
		int cfmn = vfmn.Size();
		for (int ifmn = 0; ifmn < cfmn; ifmn++)
		{
			// Loop through the path from the class down to the attribute for this column.
			if (ifmn > 0)
				stuColumn.Append(",");
			stuColumn.FormatAppend(L"%d", vfmn[ifmn]->m_flid);
			if (vfmn[ifmn]->m_proptype == kfptRoledParticipant)
			{
				stuColumn.FormatAppend(L"(%d)", vfmn[ifmn]->m_hvo);	// Store desired Role HVO.
			}
		}
		if (ivfmn > 0)
			stuColumns.Append(L"|");
		stuColumns.Append(stuColumn.Chars());
	}
}


/*----------------------------------------------------------------------------------------------
	This method returns the SQL string for the requested filter (iflt). If the requested filter
	is a simple filter and the user has requested a prompt for it, the prompt dialog will be
	shown and the new value will be used in the query. If the user cancels the prompt dialog,
	this method will return false. If there was a problem constructing the query, this method
	will throw an exception.

	@param plpi Pointer to the application language project information.
	@param iflt Index of the filter in the database.
	@param hwnd Handle to a window for querying user for information to embed in the filter.
	@param stuQuery Reference to the output SQL query string.
	@param hvoTopLevel Database object id for the top level object we are filtering on.
	@param flidTop Field id for the top level object we are filtering on.
	@param flidSub Field id for subsidiary objects belonging to the top level object.
	@param pafw Pointer to the frame window of the application, which contains the filter
				menu node information.
	@param pasi Pointer to the currently selected sort method information.
	@param pasiXref Pointer to the additional sort method information used by cross reference
				fields (may be NULL).
	@param fCancelPrev Flag whether a previous prompt dialog was cancelled, so that we don't
				need (or want) to prompt if the pre-existing filter normally wants to do so.
	@param pfxref Pointer to utility class with functions for handling cross reference fields.

	@return True if the SQL string is successfully built, false if the user cancels a prompt
				dialog, preventing the SQL query from being completed.

	@exception E_ABORT is thrown if an error has been detected, and an appropriate message
				already displayed on the screen.  E_FAIL is thrown for errors that do not
				have a specific error message defined.
----------------------------------------------------------------------------------------------*/
bool FilterUtil::GetFilterQuery(AfLpInfo * plpi, int iflt, HWND hwnd, StrUni & stuQuery,
	HVO hvoTopLevel, int flidTop, int flidSub, AfMainWnd * pafw, AppSortInfo * pasi,
	AppSortInfo * pasiXref, bool fCancelPrev, FwFilterXrefUtil * pfxref
)
{
	AssertPtr(plpi);
	Assert(hwnd);
	AssertPtr(pafw);
	AssertPtrN(pasiXref);

	stuQuery.Clear();

	FilterMenuNodeVec * pvfmn = pafw->GetFilterMenuNodes(plpi);
	AssertPtr(pvfmn);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	Assert((uint)iflt < (uint)pdbi->GetFilterCount());

	Vector<FilterMenuNodeVec> vvfmnColumns;
	AppFilterInfo & afi = pdbi->GetFilterInfo(iflt);
	FilterUtil::BuildColumnsVector(plpi, afi.m_stuColInfo, vvfmnColumns, pafw);
#ifdef DEBUG
	StrAnsi stab;
	StrAnsi stabTitle;
#endif
	if (vvfmnColumns.Size() == 0)
		ThrowHr(E_FAIL);

	// Show the prompt dialog if we need to for this filter.
	if (afi.m_fShowPrompt && !fCancelPrev)
	{
		Assert(afi.m_fSimple);
		Assert(vvfmnColumns.Size() == 1);

		// Show the dialog.
		FwFilterPromptDlgPtr qfltp;
		qfltp.Create();
		qfltp->SetDialogValues((wchar *)afi.m_stuPrompt.Chars(), plpi, afi.m_hvo,
			vvfmnColumns[0]);
		if (qfltp->DoModal(hwnd) != kctidOk)
			return false;

		// Get the new text from the dialog and update the database.
		ITsStringPtr qtss;
		qfltp->GetDialogValues(&qtss);
		if (qtss)
		{
			// First, make sure that the string is normalized.  This may be paranoid overkill.
			ITsStringPtr qtssNFD;
			CheckHr(qtss->get_NormalizedForm(knmNFD, &qtssNFD));
			// This outlines an approach that could be taken if we want this to be Undoable.
			// However, Undoing a change produced by running a prompt filter is dubious.
			// Do we 'undo' to the previous choice?
			// But if Refresh re-constructs the filter it will prompt again rather than using
			// the previous value; and the state before the user invoked the filter may anyway
			// have been a quite different filter, or none, rather than an earlier state of this filter.
			// This is probably why we made this not undoable.

			// Get aCustViewDa from pdbi.
			// Begin an Undo task
			// qcda->get_VecItem(afi.m_hvo, kflidCmFilter_Rows, 0, &hvoRow
			// get_VecItem(hvoRow, kflidCmFilterRow_Cells, 0, &hvoCell)
			// SetString(hvoCell, kflidCmCell_Contents, qtssNFD
			// EndUndoTask

			// As written this code updates the contents of all CmCells in the filter;
			// but currently we only generate prompts for basic filters that have only a single
			// cell, so only that one gets updated. If we allow prompts in multi-cell filters,
			// we will need to enhance this so only the relevant cell gets updated.
			StrUni stuQuery;
			stuQuery.Format(L"update CmCell set contents=?, contents_fmt=? "
				L"where id in (select id from CmCell cell "
				L"  left outer join CmRow_Cells as row on cell.id = row.dst "
				L"  left outer join CmFilter_Rows as fltr "
				L"on row.src = fltr.dst where fltr.src = %d)", afi.m_hvo);
			SmartBstr sbstr;
			CheckHr(qtssNFD->get_Text(&sbstr));

			IOleDbEncapPtr qode;
			IOleDbCommandPtr qodc;

			pdbi->GetDbAccess(&qode);
			CheckHr(qode->CreateCommand(&qodc));

			//  Copy format information of the TsString to byte array rgbFormat.
			BYTE rgbFormat[8200];
			int cbNeeded;
			CheckHr(qtssNFD->SerializeFmtRgb(rgbFormat, isizeof(rgbFormat), &cbNeeded));
			CheckHr(qodc->SetParameter(1,
				DBPARAMFLAGS_ISINPUT,
				NULL, // param name; we are not using; hope OK
				DBTYPE_WSTR,
				(ULONG *)(sbstr.Chars()),
				sbstr.Length() * 2));
			CheckHr(qodc->SetParameter(2,
				DBPARAMFLAGS_ISINPUT,
				NULL, // param name; we are not using; hope OK
				DBTYPE_BYTES,
				(ULONG *)rgbFormat,
				cbNeeded));

			try
			{
				qode->BeginTrans();
				CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
				qode->CommitTrans();
			}
			catch(...)
			{
				qode->RollbackTrans();
				throw;	// For now we have nothing to add, so pass it on up.
			}
		}
	}

	WaitCursor wc;

	IFwMetaDataCachePtr qmdc;
	pdbi->GetFwMetaDataCache(&qmdc);

	// Add each column to the from builder to generate aliases for each column. This also
	// generates all the join statements that are needed.
	StrUni stuFromClause;
	StrUni stuWhereClause;
	Vector<StrUni> vstuAlias;
	ULONG clidDst;
	qmdc->GetDstClsId(flidTop, &clidDst);
	bool fSingleClass = true;
	int clid = 0;
	for (int ivfmn = 0; ivfmn < vvfmnColumns.Size(); ++ivfmn)
	{
		FilterMenuNodeVec & vfmn = vvfmnColumns[ivfmn];
		for (int ifmn = 0; ifmn < vfmn.Size(); ++ifmn)
		{
			if (vfmn[ifmn]->m_fmnt == kfmntClass)
			{
				if (clid)
				{
					fSingleClass = vfmn[ifmn]->m_clid == clid;
					break;
				}
				else
				{
					clid = vfmn[ifmn]->m_clid;
					break;
				}
			}
		}
		if (!fSingleClass)
			break;
	}
	FromQueryBuilder fqb(pdbi->UserWs(), pdbi, pvfmn, clidDst, fSingleClass, pfxref);
	PossNameType pnt;
	Vector<PossNameType> vpnt;

	for (int ivfmn = 0; ivfmn < vvfmnColumns.Size(); ivfmn++)
	{
		pnt = kpntLim;
		FilterMenuNodeVec & vfmn = vvfmnColumns[ivfmn];
		if ((*vfmn.Top())->m_proptype == kfptPossList)
		{
			PossListInfoPtr qpli;
			if (plpi->LoadPossList((*vfmn.Top())->m_hvo,
					plpi->GetPsslWsFromDb((*vfmn.Top())->m_hvo), &qpli))
			{
				pnt = qpli->GetDisplayOption();
			}
		}
		vpnt.Push(pnt);
		if (!fqb.AddColumn(vfmn, pnt))
		{
#ifdef DEBUG
			stab.Format("fqb.AddColumn(vvfmnColumns[%d]) failed", ivfmn);
			stabTitle.Format("DEBUG FilterUtil::GetFilterQuery() -- line %d", __LINE__);
			::MessageBoxA(NULL, stab.Chars(), stabTitle.Chars(), MB_OK | MB_TASKMODAL);
#endif
			ThrowHr(E_FAIL);
		}
	}
	fqb.GetFromClause(stuFromClause, vstuAlias);
	Vector<int> vhvoTagPossList;
	fqb.GetTagLists(vhvoTagPossList);

	// Add the cells from each row to the where builder to generate the where statement.
	WhereQueryBuilder wqb(vstuAlias, &vvfmnColumns, vpnt, plpi, clidDst, pfxref);
	if (!wqb.BuildWhereClause(afi.m_hvo))
	{
#ifdef DEBUG
		stab.Format("wqb.BuildWhereClause(afi.m_hvo = %d) failed", afi.m_hvo);
		stabTitle.Format("DEBUG FilterUtil::GetFilterQuery() -- line %d", __LINE__);
		::MessageBoxA(NULL, stab.Chars(), stabTitle.Chars(), MB_OK | MB_TASKMODAL);
#endif
		ThrowHr(E_FAIL);
	}
	wqb.GetWhereClause(stuWhereClause);

	// Create the SQL string that will be passed to the database to perform the filter.
	stuQuery.Format(L"declare @uid as uniqueidentifier;%n"
		L"exec GetSubObjects$ @uid output, %d, %d;%n"
		L"exec GetSubObjects$ @uid output, null, %d;%n",
		hvoTopLevel, flidTop, flidSub);

	int ihvo;
	for (ihvo = 0; ihvo < vhvoTagPossList.Size(); ++ihvo)
	{
		stuQuery.FormatAppend(L"declare @uid%d as uniqueidentifier;%n"
			L"exec GetSubObjects$ @uid%d output, %d, %d;%n"
			L"exec GetSubObjects$ @uid%d output, null, %d;%n",
			ihvo,
			ihvo, vhvoTagPossList[ihvo], kflidCmPossibilityList_Possibilities,
			ihvo, kflidCmPossibility_SubPossibilities);
	}

	StrUni stuTable;
	StrUni stuSelAdd;
	StrUni stuJoin;
	StrUni stuOrder;

	if (pasi && pasi->m_stuPrimaryField.Length())
	{
		stuTable.Format(
			L"    ObjId int,%n"
			L"    ClsId int,%n"
			L"    OwnId int");
		stuOrder.Format(L"SELECT ObjId, ClsId, OwnId");
		ILgWritingSystemFactoryPtr qwsf;
		pdbi->GetLgWritingSystemFactory(&qwsf);
		AppSortInfo * pasiDef = pafw->GetDefaultSortMethod();
		if (pasiDef == pasi)
			pasiDef = NULL;
		if (fSingleClass)
			SortMethodUtil::BuildSqlPieces(pasi, L"t1.id", qmdc, qwsf, pasiXref,
				pasiDef, stuTable, stuSelAdd, stuJoin, stuOrder);
		else
			SortMethodUtil::BuildSqlPieces(pasi, L"t0.id", qmdc, qwsf, pasiXref,
				pasiDef, stuTable, stuSelAdd, stuJoin, stuOrder);
#ifdef DEBUG
		if (stuTable.Length())
		{
			Assert(stuSelAdd.Length());
			Assert(stuJoin.Length());
			Assert(stuOrder.Length());
		}
#endif
	}

	stuQuery.Append(stuTable);

	if (fSingleClass)
		stuQuery.FormatAppend(L"SELECT DISTINCT t1.id, t1.class$, t1.owner$");
	else
		stuQuery.FormatAppend(L"SELECT DISTINCT t0.id, t0.class$, t0.owner$");
	stuQuery.Append(stuSelAdd);
	stuQuery.FormatAppend(L"%n  FROM%n%s", stuFromClause.Chars());
	stuQuery.Append(stuJoin);
	stuQuery.Append(stuWhereClause);
	if (stuOrder.Length())
		stuQuery.FormatAppend(L"%s;%n", stuOrder.Chars());
	else
		stuQuery.FormatAppend(L"ORDER BY t1.id;%n");
	stuQuery.FormatAppend(L"exec CleanObjInfoTbl$ @uid");
	for (ihvo = 0; ihvo < vhvoTagPossList.Size(); ++ihvo)
		stuQuery.FormatAppend(L";%nexec CleanObjInfoTbl$ @uid%d", ihvo);

	return true;
}


//:>--------------------------------------------------------------------------------------------
//:>	The following methods are all non-static methods on FilterUtil.
//:>--------------------------------------------------------------------------------------------

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FilterUtil::FilterUtil()
{
	m_himl = NULL;
	m_qteSpecial = NULL;
	m_pwsSpecial = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FilterUtil::~FilterUtil()
{
	if (m_himl)
	{
		AfGdi::ImageList_Destroy(m_himl);
		m_himl = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Create a hierarchical popup menu based on the filter menu node structure returned
	from the application.
	Since this menu is generated at run-time, we don't know how many items will be in the menu.
	So we start adding menu items in the range used by expandable menus. Since we also store a
	flat list of all possible nodes, we can use these IDs as indexes into the flat list. So
	when a user chooses an item from this menu, we can easily get back to the node the menu
	item corresponds to. This 'trick' seemed to be the most efficient way to create a
	hierarchical menu at runtime and still be able to figure out what the user clicked on.

	@param plpi Pointer to the application language project information.

	@return Handle to the newly created hierarchical popup menu.
----------------------------------------------------------------------------------------------*/
HMENU FilterUtil::CreatePopupMenu(AfLpInfo * plpi, AfMainWnd * pafw)
{
	AssertPtr(plpi);

	HMENU hmenuPopup = ::CreatePopupMenu();
	if (!hmenuPopup)
		ThrowHr(E_FAIL);
	int cid = kcidMenuItemDynMin;
	AssertPtr(pafw);
	FilterMenuNodeVec * pvfmn = pafw->GetFilterMenuNodes(plpi);
	AssertPtr(pvfmn);
	FilterMenuNodeVec & vfmnFlat = pafw->FlatFilterMenuNodeVec();
	bool fPushNodes = vfmnFlat.Size() == 0;
	int cfmn = pvfmn->Size();
	if (cfmn == 1)
	{
		// We need this top menu node internally: just don't show it to users because they
		// would consider it repetitively redundant.
		if (fPushNodes)
			vfmnFlat.Push((*pvfmn)[0]);
		++cid;
		pvfmn = &(*pvfmn)[0]->m_vfmnSubItems;
		cfmn = pvfmn->Size();
	}
	for (int ifmn = 0; ifmn < cfmn; ifmn++)
		InsertMenuNode(hmenuPopup, (*pvfmn)[ifmn], cid, vfmnFlat, fPushNodes);
	return hmenuPopup;
}


/*----------------------------------------------------------------------------------------------
	This recursive method first gets called from CreatePopupMenu and adds one node to the menu.
	It calls itself recursively if the node pops up another submenu. It is used to generate
	a hierarchical popup menu based on the filter menu node structure returned from the
	application.

	@param hmenu Handle to a popup menu.
	@param pfmn Pointer to a filter menu node.
	@param cid Command id for the next new menu node to add to hmenu.
	@param fPushNodes Flag whether to push pfmn onto the class's flat list of filter menu nodes.
----------------------------------------------------------------------------------------------*/
void FilterUtil::InsertMenuNode(HMENU hmenu, FilterMenuNode * pfmn, int & cid,
	FilterMenuNodeVec & vfmnFlat, bool fPushNodes)
{
	AssertPtr(pfmn);
	StrApp str(pfmn->m_stuText);
	if (fPushNodes)
		vfmnFlat.Push(pfmn);

	if (pfmn->m_fmnt == kfmntLeaf)
	{
		if (pfmn->m_flid == 0 && pfmn->m_proptype == kcptNil && !str.Length())
			::AppendMenu(hmenu, MF_SEPARATOR, cid++, NULL);
		else
			::AppendMenu(hmenu, MF_STRING, cid++, str.Chars());
	}
	else
	{
		// Create a popup menu and insert the nodes for it.
		cid++;
		HMENU hmenuPopup = ::CreatePopupMenu();
		int cfmn = pfmn->m_vfmnSubItems.Size();
		for (int ifmn = 0; ifmn < cfmn; ifmn++)
			InsertMenuNode(hmenuPopup, pfmn->m_vfmnSubItems[ifmn], cid, vfmnFlat, fPushNodes);
		::AppendMenu(hmenu, MF_POPUP, (uint)hmenuPopup, str.Chars());
	}
}


/*----------------------------------------------------------------------------------------------
	This recursive method creates a vector of ownership FilterMenuNode structures based on the
	index of a selected FilterMenuNode in the flat list. It is used to generate the vector when
	the user selects an item from the popup menu generated using CreatePopupMenu.
	When this returns, the first item in vfmn will be the root node. The last item will be the
	node given by ifmnFlat.

	@param ifmnFlat Index into the class's flat list of filter menu nodes.
	@param vfmn Reference to the output column of filter menu nodes.
	@param fClear Flag whether to clear vfmn at the beginning of the function.
----------------------------------------------------------------------------------------------*/
void FilterUtil::BuildColumnVector(FilterMenuNodeVec & vfmnFlat, int ifmnFlat,
	FilterMenuNodeVec & vfmn, bool fClear)
{
	FilterMenuNode * pfmn = vfmnFlat[ifmnFlat];
	AssertPtr(pfmn);

	// fClear should only be true the first time this is called.
	if (fClear)
		vfmn.Clear();

	vfmn.Insert(0, pfmn);

	// Look through the flat list to find out which item points to the one given by ifmnFlat.
	// It must be before the index given by ifmnFlat.
	for (int ifmn = 0; ifmn < ifmnFlat; ifmn++)
	{
		FilterMenuNodeVec & vfmnSub = vfmnFlat[ifmn]->m_vfmnSubItems;
		int cfmnSub = vfmnSub.Size();
		for (int ifmnSub = 0; ifmnSub < cfmnSub; ifmnSub++)
		{
			if (vfmnSub[ifmnSub] == pfmn)
			{
				// We've found the parent so call this method again passing the parent index.
				BuildColumnVector(vfmnFlat, ifmn, vfmn, false);
				return;
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	This method returns the column name of a column given the vector of FilterMenuNodes that
	represent the column. If fPath is true, all nodes in the vector are used to construct the
	name; otherwise only the last (leaf) node is used.
	If fPath is true, the string has the form: "<node>: <node>: <node>".

	@param vfmn Reference to the input column of filter menu nodes.
	@param fPath Flag whether to include all nodes in the column in the output name.
	@param str Reference to the output column name string.
----------------------------------------------------------------------------------------------*/
void FilterUtil::GetColumnName(FilterMenuNodeVec & vfmn, bool fPath, StrApp & str)
{
	str.Clear();

	StrApp strT;
	int cfmn = vfmn.Size();
	Assert(cfmn > 0);
	if (fPath)
	{
		for (int ifmn = 0; ifmn < cfmn - 1; ifmn++)
		{
			strT = vfmn[ifmn]->m_stuText;
			str.FormatAppend(_T("%s: "), strT.Chars());
		}
	}

	str.Append(vfmn[cfmn - 1]->m_stuText.Chars());
}


/*----------------------------------------------------------------------------------------------
	Return the handle to the image list that holds icons used by the filter dialogs.
----------------------------------------------------------------------------------------------*/
HIMAGELIST FilterUtil::GetImageList()
{
	if (!m_himl)
	{
		// The image list hasn't been created yet, so create it now.
		m_himl = AfGdi::ImageList_Create(16, 16, ILC_COLORDDB | ILC_MASK, 0, 0);
		HBITMAP hbmp = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
			MAKEINTRESOURCE(kridFilterButtonImages));
		if (!hbmp)
			ThrowHr(WarnHr(E_FAIL));
		if (::ImageList_AddMasked(m_himl, hbmp, kclrPink) == -1)
			ThrowHr(WarnHr(E_FAIL));
		AfGdi::DeleteObjectBitmap(hbmp);
	}

	return m_himl;
}


/*----------------------------------------------------------------------------------------------
	Get the default prompt used to show the user when they select a simple filter and have
	asked to be prompted, but have not entered their own prompt string. pfft and/or pfkt can
	be NULL if their value is not desired.

	If prgchCell is NULL and pfkt is not NULL, the input value of pfkt will be used instead
	of using the default keyword type (unless pfkt is kfktError). If prgchCell is not NULL, any
	value stored in pfkt is ignored (because it will be retrieved from prgchCell).
	If prgchCell is NULL, strCondition will not be set to anything.

	The prompt will have the form: "The %s field %s the following %s:". The first string is
	the (hierarchical) type of field the filter is based on. The second string is the
	comparision that is used. The third string is the type of value stored in that field.
	This is a sample prompt: "The Event: Title field contains the following text:".

	@param prgchCell Pointer to a NUL-terminated Unicode string containing the text of the
					filter cell (may be NULL).
	@param vfmn Reference to the vector of filter menu nodes that define the item being
					filtered on.
	@param strPrompt Reference to a string for storing the output prompt.
	@param strCondition Reference to a string for storing the text of the filter cell (cleared
					if prgchCell is NULL)
	@param pfkt Pointer to the type of filter keyword; used for input if prgchCell is NULL, and
					otherwise used for output.
	@param pfft Pointer to the type of filter field; used for output.
	@param pfSubitems Pointer to a flag whether this filter matches on hierarchical list
					subitems.
	@param pws Pointer to the writing system for a multilingual text operation.
----------------------------------------------------------------------------------------------*/
void FilterUtil::GetSimpleFilterPrompt(const wchar * prgchCell, ILgWritingSystemFactory * pwsf,
	FilterMenuNodeVec & vfmn, StrApp & strPrompt, StrApp & strCondition,
	FilterKeywordType * pfkt, FilterFieldType * pfft, bool * pfSubitems, int * pws)
{
	AssertPszN(prgchCell);
	AssertPtrN(pfft);
	AssertPtrN(pfkt);
	Assert(vfmn.Size() > 0);

	strPrompt.Clear();
	strCondition.Clear();

	KeywordLookup kl;
	FilterKeywordType fkt = kfktError;
	bool fSubitems = false;
	if (prgchCell)
	{
		// Get the actual cell text that gets shown (without the condition and quotes).
		const wchar * psz = const_cast<wchar *>(prgchCell);
		SkipWhiteSpace(psz);
		fkt = kl.GetTypeFromStr(psz);
		// Extract any writing system value.
		int ws;
		ws = FilterUtil::ParseWritingSystem(psz, pwsf);
		if (pws)
			*pws = ws;

		const wchar * pszMin = psz;
		if (*pszMin == '"')
			pszMin++;
		const wchar * pszLim = pszMin;
		while (*pszLim != 0 && *pszLim != '"')
		{
			if (*pszLim == '\\')
			{
				if (pszLim[1] == '\\' || pszLim[1] == '"')
					++pszLim;
			}
			++pszLim;
		}
		strCondition.Assign(pszMin, pszLim - pszMin);
		int ich = strCondition.FindCh('\\');
		while (ich >= 0)
		{
			int ch = strCondition.GetAt(ich+1);
			if (ch == '\\' || ch == '"')
				strCondition.Replace(ich, ich+1, L"");
			ich = strCondition.FindCh('\\', ich+1);
		}
		int ichSub = strCondition.FindStr(L" +subitems");
		if (ichSub > 0 && strCondition.Length() == ichSub + 10)
			fSubitems = true;
	}
	else if (pfkt)
	{
		fkt = *pfkt;
	}

	StrApp strFormat(kstidFltrDefaultPrompt);

	StrApp strColumn;
	GetColumnName(vfmn, true, strColumn);

	StrAppBufPath strbpType;
	FilterFieldType fft = kfftNone;
	int tag = vfmn[vfmn.Size() - 1]->m_proptype;
	switch (tag)
	{
	case kcptTime:
	case kcptGenDate:
		strbpType.Load(kstidFilterDate);
		fft = kfftDate;
		if (fkt == kfktError)
			fkt = kfktGTE; // Use default value.
		break;
	case kcptBoolean:
	case kfptBoolean:
		strbpType.Load(kstidFilterBoolean);
		strFormat.Load(kstidFltrDefaultBooleanPrompt);
		fft = kfftBoolean;
		if (fkt == kfktError)
			fkt = kfktYes;	// Use default value.
		break;
	case kcptInteger:
	case kcptNumeric:
	case kcptFloat:
		strbpType.Load(kstidFilterNumber);
		fft = kfftNumber;
		if (fkt == kfktError)
			fkt = kfktEqual; // Use default value.
		break;
	case kcptString:
	case kcptUnicode:
	case kcptBigString:
	case kcptBigUnicode:
	case kcptMultiString:
	case kcptMultiUnicode:
	case kcptMultiBigString:
	case kcptMultiBigUnicode:
	case kfptStText:
		strbpType.Load(kstidFilterText);
		fft = kfftText;
		if (fkt == kfktError)
			fkt = kfktContains; // Use default value.
		break;
	case kcptOwningAtom:
	case kcptReferenceAtom:
	case kcptOwningCollection:
	case kcptReferenceCollection:
	case kcptOwningSequence:
	case kcptReferenceSequence:
	case kfptPossList:
	case kfptTagList:
	case kfptRoledParticipant:
		strbpType.Load(tag == kfptTagList ? kstidFilterTag : kstidFilterReference);
		fft = kfftRef;
		if (fkt == kfktContains || fkt == kfktDoesNotContain)
			fft = kfftRefText;
		break;
	case kfptEnumList:
		strbpType.Load(kstidFilterEnum);
		fft = kfftEnum;
		if (fkt == kfktError)
			fkt = kfktEqual; // Use default value.
		break;
	case kfptEnumListReq:
		strbpType.Load(kstidFilterEnum);
		fft = kfftEnumReq;
		if (fkt == kfktError)
			fkt = kfktEqual; // Use default value.
		break;
	case kfptCrossRef:
	case kfptCrossRefList:
		fft = kfftCrossRef;
		if (fkt == kfktError)
			fkt = kfktNotEmpty;	// Use default value.
		break;
	default:
		fft = kfftNone;
	}

	if (fkt == kfktError)
	{
		Assert(fft == kfftRef || fft == kfftRefText);
		fkt = fft == kfftRef ? kfktMatches : kfktContains;
	}

	StrAppBufPath strbpCondition;
	strbpCondition = kl.GetStrFromType(fkt);
	strbpCondition.SetAt(0, (char)tolower(strbpCondition.GetAt(0)));

	StrAppBufPath strbpSubitems;
	if (fSubitems)
		strbpSubitems.Load(kstidFltrAndSubitems);

	strPrompt.Format(strFormat.Chars(), strColumn.Chars(), strbpCondition.Chars(),
		strbpType.Chars(), strbpSubitems.Chars());
	if (pfft)
		*pfft = fft;
	if (pfkt)
		*pfkt = fkt;
	if (pfSubitems)
		*pfSubitems = fSubitems;
}


/*----------------------------------------------------------------------------------------------
	Launch a choices list chooser on the appropriate list for the column, and associate the
	chosen guid, if any, with the specified character.

	@param hwndPar Handle to the parent window for the choices list chooser dialog box.
	@param pdbi Pointer to the application database information.
	@param vfmn Reference to  the vector of filter menu nodes that define the possibility list
					or tag list being used.
	@param pvcd Pointer to the data cache containing the filter information.
	@param ptss Pointer to an ITsString COM object that contains the filter cell text.
	@param prootb Pointer to the IVwRootBox COM object used for displaying the filter cell.
	@param hvoOwner The database ID of the owning object.
	@param tag Identifier used to select one particular property of the owning object.
	@param ichObj Offset into the string contained in ptss.
	@param fReplace Flag whether to replace the single character at ichObj, or insert the link
					at that location.
	@param hvoPss The database ID of the list item to select initially, or zero.

	@return True if successful, false if an error occurs or the operation is canceled.
----------------------------------------------------------------------------------------------*/
bool FilterUtil::InsertHotLink(HWND hwndPar, AfDbInfo * pdbi, FilterMenuNodeVec & vfmn,
	IVwCacheDa * pvcd, ITsString * ptss, IVwRootBox * prootb, HVO hvoOwner, PropTag tag,
	int ichObj, bool fReplace, HVO hvoPss, FilterVc * pfvc)
{
	Assert(vfmn.Size() > 0);
	FilterMenuNodePtr qfmn = *vfmn.Top();
	Assert(qfmn->m_proptype == kfptPossList || qfmn->m_proptype == kfptTagList);
	AssertPtr(pdbi);
	AssertPtr(pvcd);
	AssertPtr(ptss);
	AssertPtr(prootb);
	Assert(hvoOwner);

	// Get the ID of the choices list the column is based on.
	HVO hvoPssl = qfmn->m_hvo;
	Assert(hvoPssl);

	// We need the list's preferred writing system (may be selector value).
	Vector<AfLpInfoPtr> & vqlpi = pdbi->GetLpiVec();
	Assert(vqlpi.Size() > 0);
	int wsList = vqlpi[0]->GetPsslWsFromDb(hvoPssl);

	// Show the chooser dialog and return if the user cancels out of it.
	PossChsrDlgPtr qplc;
	qplc.Create();
	qplc->SetDialogValues(hvoPssl, wsList, hvoPss);
	if (qplc->DoModal(hwndPar) != kctidOk)
		return false;

	// Get the guid of the possibility that was chosen.
	HVO hvoPssNew;
	qplc->GetDialogValues(hvoPssNew);
	GUID uid;
	if (!pdbi->GetGuidFromId(hvoPssNew, uid))
		return false;

	try
	{
		// Update the string with the new object.
		StrUni stuData;
		OLECHAR * prgchData;
		// Make large enough for a guid plus the type character at the start.
		stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
		*prgchData = kodtNameGuidHot;
		memmove(prgchData + 1, &uid, isizeof(uid));

		ITsStrBldrPtr qtsb;
		ITsTextPropsPtr qttp;
		ITsPropsBldrPtr qtpb;
		ITsStringPtr qtss;
		CheckHr(ptss->GetBldr(&qtsb));
		CheckHr(qtsb->get_PropertiesAt(ichObj, &qttp));
		CheckHr(qttp->GetBldr(&qtpb));
		CheckHr(qtpb->SetStrPropValue(ktptObjData, stuData.Bstr()));
		CheckHr(qtpb->GetTextProps(&qttp));
		OLECHAR chObj = kchObject;
		CheckHr(qtsb->ReplaceRgch(ichObj, ichObj + (fReplace == true), &chObj, 1, qttp));
		CheckHr(qtsb->GetString(&qtss));
		// The possibility name string in the database may have changed due to qplc->DoModal(),
		// so we need to flush the cache which may be storing the old string value.
		if (pfvc)
			pfvc->FlushHotLinkCache(uid);
		ISilDataAccessPtr qsdaTemp;
		HRESULT hr;
		IgnoreHr(hr = pvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
		if (FAILED(hr))
			ThrowInternalError(E_INVALIDARG);
		CheckHr(qsdaTemp->SetString(hvoOwner, tag, qtss));
		CheckHr(qsdaTemp->PropChanged(prootb, kpctNotifyAll, hvoOwner, tag, ichObj, 1,
			fReplace == true));
	}
	catch (...)
	{
		return false;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Return a vector giving the user-definable language variables.
----------------------------------------------------------------------------------------------*/
//:>	TODO SteveMc (DarrellZ): This whole method is going to have to be rewritten to load the
//:>	actual data from somewhere once that part gets finished.  Right now we just hard code
//:>	default English values.
Vector<FilterPatternInfo> & FilterUtil::GetLanguageVariables()
{
	static Vector<FilterPatternInfo> s_vfpi;

	if (s_vfpi.Size() == 0)
	{
		wchar * rgpsz[][3] = {
			{ L"Digit", L"D", L"[0-9]" },
			{ L"Nasal", L"N", L"[MmNn—Ò]" },
			{ L"Consonants", L"C", L"[BbCcDdFfGgHhJjKkLlMmNnPpQqRrSsTtVvWwXxYyZz«Á˝ˇ]" },
			{ L"Vowels", L"V",
							 L"[AaEeIiOoUu¿¡¬√ƒ≈∆»… ÀÃÕŒœ“”‘’÷Ÿ⁄€‹›‡·‚„‰ÂÊËÈÍÎÏÌÓÔÚÛÙıˆ˘˙˚¸]" },
			{ L"Punctuation",   L".", L"[.,?:;!<>{}[\\]()\"''`ø]" },
			{ L"Word boundary", L"#", L"[ .,?:;!<>{}[\\]()\"''`ø]" },
			// TODO SteveMc: What is the Unicode character for morph boundaries?
			//{ L"Morph boundary", L"=", L"" },
			{ L"One character", L"a", L"_" },
			{ L"One alpha character", L"@", L"[AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWw"
							 L"XxYyZz«Á˝ˇ¿¡¬√ƒ≈∆»… ÀÃÕŒœ“”‘’÷Ÿ⁄€‹›‡·‚„‰ÂÊËÈÍÎÏÌÓÔÚÛÙıˆ˘˙˚¸]" },
			{ L"One or more characters", L"+", L"_%" },
			{ L"Zero or more characters", L"*", L"%" },
			// TODO SteveMc: What is the SQL for zero or one character?  Can it be done?
			//{ L"Zero or one character", L"?", L"" },
		};
		int cfpi = SizeOfArray(rgpsz);
		s_vfpi.Resize(cfpi);
		for (int ifpi = 0; ifpi < cfpi; ifpi++)
		{
			FilterPatternInfo & fpi = s_vfpi[ifpi];
			fpi.m_stuName = rgpsz[ifpi][0];
			fpi.m_stuAbbrev = rgpsz[ifpi][1];
			fpi.m_stuReplace = rgpsz[ifpi][2];
		}
	}
	return s_vfpi;
}


/*----------------------------------------------------------------------------------------------
	Return the font family and size of the default system font used to draw menus and items
	in dialogs. These are used when creating the full filter table.

	@param stuFont Reference to a string for returning the default system font name.
	@param dympFont Reference to an integer for returning the default system font height in
					millipoints.
----------------------------------------------------------------------------------------------*/
void FilterUtil::GetSystemFont(StrUni & stuFont, int & dympFont)
{
	if (m_stuFont.Length() == 0)
	{
		// This is the first time this has been called, so get the system font information
		// now and store it for the next time we get called.
		HFONT hfont = (HFONT)::GetStockObject(DEFAULT_GUI_FONT);
		LOGFONT lf;
		if (!::GetObject(hfont, isizeof(lf), &lf))
			ThrowHr(E_FAIL);
		m_stuFont = lf.lfFaceName;

		HWND hwndDesk = ::GetDesktopWindow();
		HDC hdc = ::GetDC(hwndDesk);
		int ypLogPixels = ::GetDeviceCaps(hdc, LOGPIXELSY);
		int iSuccess;
		iSuccess = ::ReleaseDC(hwndDesk, hdc);
		Assert(iSuccess);
		m_dympFont = -MulDiv(lf.lfHeight, kdzmpInch, ypLogPixels);
	}

	stuFont = m_stuFont;
	dympFont = m_dympFont;
}


/*----------------------------------------------------------------------------------------------
	Return the keyword type given by the field type and the dialog containing the Condition
	combobox (this is the FwFilterSimpleDlg window). The type is based on the current selection
	in the combobox.

	@param fft Filter field type (text, ref, ref text, date, ...)
	@param hwndSimpleDlg Handle to the simple dialog window (single condition).
----------------------------------------------------------------------------------------------*/
FilterKeywordType FilterUtil::GetKeywordType(FilterFieldType fft, HWND hwndSimpleDlg)
{
	FilterKeywordType fkt = kfktError;

	int isel = ::SendMessage(::GetDlgItem(hwndSimpleDlg, kctidCondition), CB_GETCURSEL, 0, 0);

	FilterKeywordType rgfktText[] = { kfktEmpty, kfktNotEmpty, kfktContains,
		kfktDoesNotContain, kfktEqual, kfktNotEqual, kfktGT, kfktLT, kfktGTE, kfktLTE };
	FilterKeywordType rgfktRef[] = { kfktEmpty, kfktNotEmpty, kfktContains,
		kfktDoesNotContain, kfktMatches, kfktDoesNotMatch };
	FilterKeywordType rgfktDefault[] = { kfktEmpty, kfktNotEmpty, kfktEqual, kfktNotEqual,
		kfktGT, kfktLT, kfktGTE, kfktLTE };
	FilterKeywordType rgfktEnumReq[] = { kfktEqual, kfktNotEqual, kfktGT, kfktLT, kfktGTE,
		kfktLTE };
	FilterKeywordType rgfktBoolean[] = { kfktYes, kfktNo };
	FilterKeywordType rgfktCrossRef[] = { kfktEmpty, kfktNotEmpty, kfktContains,
		kfktDoesNotContain };
	FilterKeywordType rgfktNumber[] = { kfktEqual, kfktNotEqual, kfktGT, kfktLT, kfktGTE,
		kfktLTE };

	switch (fft)
	{
	case kfftText:
		Assert((uint)isel < (uint)(isizeof(rgfktText) / isizeof(FilterKeywordType)));
		fkt = rgfktText[isel];
		break;
	case kfftRef:
		Assert((uint)isel < (uint)(isizeof(rgfktRef) / isizeof(FilterKeywordType)));
		fkt = rgfktRef[isel];
		break;
	case kfftRefText:
		Assert((uint)isel < (uint)(isizeof(rgfktRef) / isizeof(FilterKeywordType)));
		fkt = rgfktRef[isel];
		break;
	case kfftDate:
	case kfftEnum:
		Assert((uint)isel < (uint)(isizeof(rgfktDefault) / isizeof(FilterKeywordType)));
		fkt = rgfktDefault[isel];
		break;
	case kfftEnumReq:
		Assert((uint)isel < (uint)(isizeof(rgfktEnumReq) / isizeof(FilterKeywordType)));
		fkt = rgfktEnumReq[isel];
		break;
	case kfftBoolean:
		Assert((uint)isel < (uint)(isizeof(rgfktBoolean) / isizeof(FilterKeywordType)));
		fkt = rgfktBoolean[isel];
		break;
	case kfftCrossRef:
		Assert((uint)isel < (uint)(isizeof(rgfktCrossRef) / isizeof(FilterKeywordType)));
		fkt = rgfktCrossRef[isel];
		break;
	case kfftNumber:
		Assert((uint)isel < (uint)(isizeof(rgfktNumber) / isizeof(FilterKeywordType)));
		fkt = rgfktNumber[isel];
		break;
	}

	return fkt;
}


/*----------------------------------------------------------------------------------------------
	Initialize the given date scope combobox with the appropriate set of strings.

	@param hwndCombo Handle to the date scope combobox control.
----------------------------------------------------------------------------------------------*/
void FilterUtil::FillDateScopeCombo(HWND hwndCombo, bool fExpanded)
{
	Assert(hwndCombo);

	int csel = ::SendMessage(hwndCombo, CB_GETCOUNT, 0, 0);
	StrApp str;
	if (csel == 0)
	{
		str.Load(kstidFltrExactDate);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		str.Load(kstidFltrMonthYear);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		str.Load(kstidFltrYear);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
	if (fExpanded && (csel == 0 || csel == 3))
	{
#ifdef VERSION2FILTER
		str.Load(kstidFltrMonth);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		str.Load(kstidFltrDay);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
#endif /*VERSION2FILTER*/
		str.Load(kstidFltrToday);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		str.Load(kstidFltrLastWeek);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		str.Load(kstidFltrLastMonth);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		str.Load(kstidFltrLastYear);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
#ifdef VERSION2FILTER
		str.Load(kstidFltrLast7Days);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		str.Load(kstidFltrLast30Days);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		str.Load(kstidFltrLast365Days);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
#endif /*VERSION2FILTER*/
		return;
	}
	if (!fExpanded && csel > 3)
	{
		int isel = ::SendMessage(hwndCombo, CB_GETCURSEL, 0, 0);
		if (isel > kiselYear)
			::SendMessage(hwndCombo, CB_SETCURSEL, kiselMonthYear, 0);
		while (csel > 3)
			csel = ::SendMessage(hwndCombo, CB_DELETESTRING, 3, 0);
	}
}


/*----------------------------------------------------------------------------------------------
	If ptss is NULL or the string is not defined correctly, hvoPss will be set to zero.
	Otherwise hvoPss will be set to the possibility pointed to be the string.

	@param ptss Pointer to an ITsString COM object that contains the filter cell text.
	@param vfmn Reference to  the vector of filter menu nodes that define the possibility list
					or tag list being used.
	@param pdbi Pointer to the application database information.
	@param hvoPssl Reference to the database ID of the current possibility list (output).
	@param hvoPss Reference to the database ID of the currently chosen possibility from the
					list (output).
----------------------------------------------------------------------------------------------*/
void FilterUtil::StringToReference(ITsString * ptss, FilterMenuNodeVec & vfmn, AfDbInfo * pdbi,
	HVO & hvoPssl, HVO & hvoPss)
{
	AssertPtrN(ptss);
	Assert(vfmn.Size() > 0);
	FilterMenuNodePtr qfmn = *vfmn.Top();
	Assert(qfmn->m_proptype == kfptPossList || qfmn->m_proptype == kfptTagList);
	AssertPtr(pdbi);

	hvoPssl = qfmn->m_hvo;
	hvoPss = 0;
	if (ptss)
	{
		int crun;
		CheckHr(ptss->get_RunCount(&crun));
		// The first run is the condition, the second run should be the object.
		if (crun == 2 || crun == 3)
		{
			ITsTextPropsPtr qttp;
			SmartBstr sbstr;
			CheckHr(ptss->get_Properties(1, &qttp));
			CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstr));
			wchar * prgchData = (wchar *)sbstr.Chars();
			if (*prgchData == kodtNameGuidHot)
			{
				GUID * puid = (GUID *)(prgchData + 1);
				hvoPss = pdbi->GetIdFromGuid(puid);
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Add the strings embedded inside the given enum string to the combobox. If pszValue matches
	any of the strings, select it, otherwise select the first enum value.

	@param hwndCombo Handle to a combobox control.
	@param stid Resource string ID number to a specially formated enumeration string.
					Individual substrings are separated by newline ('\n') characters.
	@param pszValue String that matches initial enum selection for the combobox.
----------------------------------------------------------------------------------------------*/
void FilterUtil::AddEnumToCombo(HWND hwndCombo, int stid, const achar * pszValue)
{
	AssertPsz(pszValue);

	int iselEnum = 0; // Default to the first enum value.
	StrUni stuValue(pszValue);
	StrUni stuEnum(stid);
	Assert(stuEnum.Length());
	::SendMessage(hwndCombo, CB_RESETCONTENT, 0, 0);
	const wchar * pszEnum = stuEnum.Chars();
	const wchar * pszEnumTotalLim = stuEnum.Chars() + stuEnum.Length();
	int cselEnum = 0;
	while (pszEnum < pszEnumTotalLim)
	{
		const wchar * pszEnumLim = wcschr(pszEnum, '\n');
		if (!pszEnumLim)
			pszEnumLim = pszEnumTotalLim;
		StrApp str(pszEnum, pszEnumLim - pszEnum);
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		if (stuValue.Length() && _wcsnicmp(pszEnum, stuValue.Chars(), pszEnumLim - pszEnum) == 0)
			iselEnum = cselEnum;
		pszEnum = pszEnumLim + 1;
		cselEnum++;
	}
	::SendMessage(hwndCombo, CB_SETCURSEL, iselEnum, 0);
}


/*----------------------------------------------------------------------------------------------
	If the actual field name (without the path) is too big to fit in the allocated space,
	draw as much as possible with '...' after it (i.e. right-truncated).
	Otherwise, draw '...' followed by as much of the right side of the field name (with the
	path) as will fit in the allocated space (i.e. left-truncated).

	This method does not actually do the drawing, it merely computes the maximal caption that
	can be drawn.

	@param hdc Handle to a device context for drawing the caption.
	@param rc Rectangular coordinates of the available space for drawing the caption.
	@param pfltdlg Pointer to the filter dialog pane object.
	@param icol Index into the vector of filter columns.
	@param strCaption Reference to a string for returning the field caption.
	@param fShowHotKey Flag whether the "Choose a field" caption should contain a hot key.  This
					is relevant only when icol does not indicate a valid column.
----------------------------------------------------------------------------------------------*/
void FilterUtil::GetFieldCaption(HDC hdc, const Rect & rc, FwFilterDlg * pfltdlg, int icol,
	StrApp & strCaption, bool fShowHotKey)
{
	AssertPtr(pfltdlg);
	Vector<FilterMenuNodeVec> vvfmnColumns;
	pfltdlg->LoadCurrentFilter(vvfmnColumns);
	Assert((uint)icol <= (uint)vvfmnColumns.Size());

	if (icol == vvfmnColumns.Size())
	{
		strCaption.Load(fShowHotKey ? kstidFltrChooseField : kstidFltrChooseFieldNHK);
	}
	else
	{
		FilterMenuNodeVec & vfmn = vvfmnColumns[icol];
		// See if the field name (with the path) fits in the allocated space.
		GetColumnName(vfmn, true, strCaption);
		SIZE size;
		::GetTextExtentPoint32(hdc, strCaption.Chars(), strCaption.Length(), &size);
		if (size.cx > rc.Width())
		{
			// The actual field name (with the path) will not fit in the allocated space.
			StrApp strPath(strCaption);
			// See if the field name (without the path) fits in the allocated space. If it
			// doesn't, we return the field name without the path.
			GetColumnName(vfmn, false, strCaption);
			::GetTextExtentPoint32(hdc, strCaption.Chars(), strCaption.Length(), &size);
			if (size.cx < rc.Width())
			{
				// Find out how much of the path we can draw.
				SIZE sizeDots;
				::GetTextExtentPoint32(hdc, _T("..."), 3, &sizeDots);
				int cxPath = rc.Width() - size.cx - sizeDots.cx;
				if (cxPath > 0)
				{
					SIZE sizePath;
					int cch = strCaption.Length();
					int ich = strPath.Length() - cch - 1;
					::GetTextExtentPoint32(hdc, strPath.Chars() + ich,
						strPath.Length() - cch - ich, &sizePath);
					while (ich >= 0 && sizePath.cx < cxPath)
					{
						ich--;
						::GetTextExtentPoint32(hdc, strPath.Chars() + ich,
							strPath.Length() - cch - ich, &sizePath);
					}
					Assert(ich >= 0);
					strCaption.Format(_T("...%s"), strPath.Chars() + ich + 1);
				}
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	If the string contains a writing system (ICU Locale name) embedded in brackets (like
	" [en]"), parse the writing system and return it, advancing the pointer past the closing
	bracket.

	@param prgch Reference to a pointer into a filter cell string.

	@return Writing system value, or zero if none found.
----------------------------------------------------------------------------------------------*/
int FilterUtil::ParseWritingSystem(const wchar *& prgch, ILgWritingSystemFactory * pwsf)
{
	AssertPtr(pwsf);
	FilterUtil::SkipWhiteSpace(prgch);
	if (*prgch == '[')
	{
		++prgch;
		const wchar * prgchMin = prgch;
		while (*prgch != 0 && *prgch != ']')
			++prgch;
		const wchar * prgchLim = prgch;
		int cch = prgchLim - prgchMin;
		int ws = 0;
		if (cch)
		{
			StrUni stu(prgchMin, cch);
			CheckHr(pwsf->GetWsFromStr(stu.Bstr(), &ws));
		}
		if (*prgch == ']')
			++prgch;
		return ws;
	}
	return 0;
}


//:>********************************************************************************************
//:>	KeywordLookup methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
KeywordLookup::KeywordLookup()
{
	static KeywordEntry s_rgke[] = {
		{ kstidFltrEmpty,               kfktEmpty },
		{ kstidFltrNotEmpty,            kfktNotEmpty },
		{ kstidFltrContains,            kfktContains },
		{ kstidFltrDoesNotContain,      kfktDoesNotContain },
		{ kstidFltrMatches,             kfktMatches },
		{ kstidFltrDoesNotMatch,        kfktDoesNotMatch },
		{ kstidFltrYes,                 kfktYes },
		{ kstidFltrNo,                  kfktNo },
		/*--------------------------------------------------------------------------------------
			These were commented out because "Greater than" and "Greater than or equal to" were
			conflicting, and PM decided we could just use the symbols instead of the words.
			If we want these, we could probably just change the GetTypeFromStr method to use
			the longest match. The symbols should be placed in front of these though, so the
			Criteria Builder will insert the symbols instead of the words.
		----------------------------------------------------------------------------------------
		{ kstidFltrEqualTo,             kfktEqual },
		{ kstidFltrNotEqualTo,          kfktNotEqual },
		{ kstidFltrGreaterThan,         kfktGT },
		{ kstidFltrLessThan,            kfktLT },
		{ kstidFltrGreaterThanEqual,    kfktGTE },
		{ kstidFltrLessThanEqual,       kfktLTE },
		{ kstidFltrOn,                  kfktEqual },
		{ kstidFltrNotOn,               kfktNotEqual },
		{ kstidFltrAfter,               kfktGT },
		{ kstidFltrBefore,              kfktLT },
		{ kstidFltrOnAfter,             kfktGTE },
		{ kstidFltrOnBefore,            kfktLTE },
		--------------------------------------------------------------------------------------*/
		{ kstidFltrAnd,                 kfktAnd },
		{ kstidFltrOr,                  kfktOr },
		{ 0,                            kfktNotEqual,   L"<>" },
		{ 0,                            kfktNotEqual,   L"><" },
		{ 0,                            kfktLTE,        L"<=" },
		{ 0,                            kfktLTE,        L"=<" },
		{ 0,                            kfktGTE,        L">=" },
		{ 0,                            kfktGTE,        L"=>" },
		{ 0,                            kfktLT,         L"<" },
		{ 0,                            kfktGT,         L">" },
		{ 0,                            kfktEqual,      L"=" },
		{ 0,                            kfktOpenParen,  L"(" },
		{ 0,                            kfktCloseParen, L")" },
	};

	m_prgke = s_rgke;
	m_cke = SizeOfArray(s_rgke);

	if (s_stuKeywords.Length() == 0)
	{
		// Load the keywords from the resource file the first time they are needed.
		for (int ike = 0; ike < m_cke; ike++)
		{
			int stid = m_prgke[ike].m_stid;
			if (stid != 0)
				s_stuKeywords.FormatAppend(L"%r^", stid);
		}
		wchar * prgch = (wchar *)s_stuKeywords.Chars();
		for (int ike = 0; ike < m_cke; ike++)
		{
			KeywordEntry & ke = m_prgke[ike];
			if (ke.m_stid != 0)
			{
				ke.m_psz = prgch;
				while (*++prgch != '^')
					;
				ke.m_cch = prgch - ke.m_psz;
				// This is OK to do because it doesn't change the length of the string.
				*prgch++ = 0;
			}
			else
			{
				AssertPsz(ke.m_psz);
				ke.m_cch = StrLen(ke.m_psz);
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Return the type of the keyword in the list of condition keywords or kfktError if pszKeyword
	does not match any of the condition keywords. If it does match a keyword, pszKeyword will be
	advanced to the character following the matched keyword.
	The keyword must be followed by a space or be at the end of the string for it to be a match.

	@param pszKeyword Reference to a pointer to a string that supposedly begins with a known
					filter keyword.
----------------------------------------------------------------------------------------------*/
FilterKeywordType KeywordLookup::GetTypeFromStr(const wchar *& pszKeyword)
{
	for (int ike = 0; ike < m_cke; ike++)
	{
		if (_wcsnicmp(pszKeyword, m_prgke[ike].m_psz, m_prgke[ike].m_cch) == 0)
		{
			pszKeyword += m_prgke[ike].m_cch;
			// Return an error code if:
			//   we're not at the end of the string, and
			//   the next letter is not a white space, and
			//   the keyword does not start with a punctuation character
			if (*pszKeyword != 0 && !iswspace(*pszKeyword) && !iswpunct(*m_prgke[ike].m_psz))
				return kfktError;
			return m_prgke[ike].m_fkt;
		}
	}

	return kfktError;
}


/*----------------------------------------------------------------------------------------------
	Return the string representation of the keyword type.

	@param fkt Filter keyword type code.
----------------------------------------------------------------------------------------------*/
const wchar * KeywordLookup::GetStrFromType(FilterKeywordType fkt)
{
	for (int ike = 0; ike < m_cke; ike++)
	{
		if (m_prgke[ike].m_fkt == fkt)
			return m_prgke[ike].m_psz;
	}
	return NULL;
}


//:>********************************************************************************************
//:>	DateKeywordLookup methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
DateKeywordLookup::DateKeywordLookup()
{
	static DateKeywordEntry s_rgdke[] = {
		{ kstidFltrExactDate   },
		{ kstidFltrMonthYear   },
		{ kstidFltrYear        },
#ifdef VERSION2FILTER
		{ kstidFltrMonth       },
		{ kstidFltrDay         },
#endif /*VERSION2FILTER*/
		{ kstidFltrToday       },
		{ kstidFltrLastWeek    },
		{ kstidFltrLastMonth   },
		{ kstidFltrLastYear    },
#ifdef VERSION2FILTER
		{ kstidFltrLast7Days   },
		{ kstidFltrLast30Days  },
		{ kstidFltrLast365Days },
#endif /*VERSION2FILTER*/
	};

	m_prgdke = s_rgdke;
	m_cdke = SizeOfArray(s_rgdke);

	if (s_stuKeywords.Length() == 0)
	{
		// Load the keywords from the resource file the first time they are needed.
		for (int idke = 0; idke < m_cdke; idke++)
			s_stuKeywords.FormatAppend(L"%r^", m_prgdke[idke].m_stid);

		wchar * prgch = (wchar *)s_stuKeywords.Chars();
		for (int idke = 0; idke < m_cdke; idke++)
		{
			m_prgdke[idke].m_psz = prgch;
			while (*++prgch != '^')
				;
			m_prgdke[idke].m_cch = prgch - m_prgdke[idke].m_psz;
			// This is OK to do because it doesn't change the length of the string.
			*prgch++ = 0;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Return the index of the keyword in the list of date keywords or -1 if pszKeyword does not
	match any of the date keywords.  If it does match a keyword, pszKeyword will be advanced to
	the character following the matched keyword.  The best match (most characters) will be used
	to determine which index to return.

	@param pszKeyword Reference to a pointer to a string that supposedly begins with a known
					date keyword.
----------------------------------------------------------------------------------------------*/
int DateKeywordLookup::GetIndexFromStr(const wchar *& pszKeyword)
{
	int idkeBestMatch = -1;
	int cchMatched = -1;
	for (int idke = 0; idke < m_cdke; idke++)
	{
		if (_wcsnicmp(pszKeyword, m_prgdke[idke].m_psz, m_prgdke[idke].m_cch) == 0 &&
			m_prgdke[idke].m_cch > cchMatched)
		{
			idkeBestMatch = idke;
			cchMatched = m_prgdke[idke].m_cch;
		}
	}

	if (cchMatched != -1)
		pszKeyword += cchMatched;

	return idkeBestMatch;
}


//:>********************************************************************************************
//:>	FwFilterDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.

	@param ptod Pointer to the enclosing Tools/Options dialog box.
----------------------------------------------------------------------------------------------*/
FwFilterDlg::FwFilterDlg(TlsOptDlg * ptod)
{
	AssertPtr(ptod);

	m_ptod = ptod;
	m_rid = kridTlsOptDlgFltr;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_Filters_tab.htm");
	m_ifltInitial = 0;
	m_ifltCurrent = -1;
	m_prmwMain = NULL;
	m_pdlgvLast = NULL;
	m_fModified = false;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwFilterDlg::~FwFilterDlg()
{
}

AfLpInfo * FwFilterDlg::GetLpInfo()
{
	AssertPtr(m_prmwMain);
	return m_prmwMain->GetLpInfo();
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog.

	@param prmwMain Pointer to the application's main window.
	@param ifltInitial Index into the application's table of filters for choosing an initial
					filter to edit.
----------------------------------------------------------------------------------------------*/
void FwFilterDlg::SetDialogValues(RecMainWnd * prmwMain, int ifltInitial)
{
	AssertPtr(prmwMain);

	m_prmwMain = prmwMain;
	m_ifltInitial = ifltInitial;
}


/*----------------------------------------------------------------------------------------------
	Gets the final values for the dialog controls, after the dialog has been closed.

	@param vfi Reference to a vector of internal FilterInfo objects that describe the set of
					defined filters.
----------------------------------------------------------------------------------------------*/
void FwFilterDlg::GetDialogValues(Vector<FwFilterDlg::FilterInfo> & vfi)
{
	vfi = m_vfi;
}


/*----------------------------------------------------------------------------------------------
	Called by AFDialog when another tab is pressed.
	Returns

	@param qct QueryCloseType
	@return True if it is OK to move off this tabb.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::QueryClose(QueryCloseType qct)
{
	if (m_pdlgvLast)
		return m_pdlgvLast->Apply();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AfLpInfo * plpi = m_prmwMain->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);

	// Create a temporary cache for loading/saving from/to the database.
	m_qodde.CreateInstance(CLSID_VwOleDbDa);
	IOleDbEncapPtr qode;
	IFwMetaDataCachePtr qmdc;
	pdbi->GetDbAccess(&qode);
	pdbi->GetFwMetaDataCache(&qmdc);
	pdbi->GetLgWritingSystemFactory(&m_qwsf);
	ISetupVwOleDbDaPtr qsodde;
	CheckHr(m_qodde->QueryInterface(IID_ISetupVwOleDbDa, (void**)&qsodde));
	Assert(qsodde);
	qsodde->Init(qode, qmdc, m_qwsf, NULL);

	// Create a private dummy cache.
	m_qvcd.CreateInstance(CLSID_VwCacheDa);

	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidAddFilter, kbtPopMenu, NULL, 0);

	HWND hwndList = ::GetDlgItem(m_hwnd, kctidFilterList);
	HIMAGELIST himlOld = ListView_SetImageList(hwndList, g_fu.GetImageList(), LVSIL_SMALL);
	if (himlOld)
		if (himlOld != g_fu.GetImageList())
			AfGdi::ImageList_Destroy(himlOld);

	// Insert a column in the list view control.
	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	Rect rc;
	::GetClientRect(hwndList, &rc);
	lvc.cx = rc.Width();
	ListView_InsertColumn(hwndList, 0, &lvc);

	// Load the filters into our internal filter vector.
	int clidRec = m_prmwMain->GetRecordClid();
	int cflt = pdbi->GetFilterCount();
	for (int iflt = 0; iflt < cflt; iflt++)
	{
		AppFilterInfo & afi = pdbi->GetFilterInfo(iflt);
		if (afi.m_clidRec == clidRec)
		{
			FilterInfo fi;
			fi.m_stuName = afi.m_stuName;
			fi.m_fSimple = afi.m_fSimple;
			fi.m_hvoOld = afi.m_hvo;
			fi.m_stuColInfo = afi.m_stuColInfo;
			fi.m_fShowPrompt = afi.m_fShowPrompt;
			fi.m_stuPrompt = afi.m_stuPrompt;
			m_vfi.Push(fi);
		}
	}
	// If a filter is activated on a custom field, and it is the last one in the list and
	// the user deletes the custom field, we get into a state where m_ifltInitial ends up
	// equaling cflt. So in this case, we can just set it to zero since the viewbar also
	// sets it to no filter. I'm (KenZ) not sure this is the correct answer, but it does
	// seem to work without any ill effects and the case of it happening is extremely rare.
	if (m_ifltInitial >= cflt)
		m_ifltInitial = 0;
	Assert((uint)m_ifltInitial < (uint)cflt || cflt == 0);
	m_ifltCurrent = m_ifltInitial;
	UpdateFilterList();

	AfApp::Papp()->AddCmdHandler(this, 1, kgrfcmmAll);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Apply changes to the dialog. This is the point where all filter changes get copied from
	our dummy cache back to the temporary cache (which writes it out to the database). This
	methods also reorders the vector of filter information so that it is sorted by the filter
	name.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::Apply()
{
	Assert(m_hwnd);

	WaitCursor wc;
	if (m_pdlgvLast)
	{
		if (!m_pdlgvLast->Apply())
			return false;
	}

	AfLpInfo * plpi = m_prmwMain->GetLpInfo();
	AssertPtr(plpi);

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	plpi->GetDbInfo()->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));

	// Get interfaces needed in this method.
	ISilDataAccessPtr qsda_oddeTemp;
	HRESULT hr = m_qodde->QueryInterface(IID_ISilDataAccess, (void **)&qsda_oddeTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);
	IVwCacheDaPtr qvcd_oddeTemp;
	hr = m_qodde->QueryInterface(IID_IVwCacheDa, (void**)&qvcd_oddeTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);

	HVO hvoLpId = plpi->GetLpId();

	int iflt;
	int cflt = m_vfi.Size();
	// Check for any filter not actually being defined.
	StrUni stuUndefined;
	int cfltUndef = 0;
	for (iflt = 0; iflt < cflt; ++iflt)
	{
		FilterInfo & fi = m_vfi[iflt];
		if (fi.m_fs == kfsInserted && !fi.m_stuColInfo.Length())
		{
			if (stuUndefined.Length())
				stuUndefined.Append(L", ");
			stuUndefined.Append(fi.m_stuName);
			++cfltUndef;
		}
	}
	if (cfltUndef)
	{
		// Alert the user, ask what to do.
		StrApp strUndefined(stuUndefined);
		StrApp strFmt;
		if (cfltUndef == 1)
			strFmt.Load(kstidFltrUndefinedFilter);
		else
			strFmt.Load(kstidFltrUndefinedFilters);
		StrApp str;
		str.Format(strFmt.Chars(), strUndefined.Chars());
		StrApp strTitle(kstidFltrCannotCreate);
		if (::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(),
			MB_OKCANCEL | MB_ICONQUESTION | MB_APPLMODAL) == IDCANCEL)
		{
			return false;
		}
	}

	// For all the filters that were updated, copy their information into the temporary cache,
	// which will update the database.
	for (iflt = 0; iflt < cflt; ++iflt)
	{
		FilterInfo & fi = m_vfi[iflt];
		if (fi.m_fs == kfsDeleted)
		{
			// If fi.m_hvoOld is NULL, the filter hasn't been put into the database yet,
			// so we don't have to delete it.
			if (fi.m_hvoOld)
			{
				m_fModified = true;
				CheckHr(qsda_oddeTemp->DeleteObjOwner(hvoLpId, fi.m_hvoOld,
					kflidLangProject_Filters, -1));
			}
		}

		if (fi.m_fs == kfsInserted && !fi.m_stuColInfo.Length())
			continue;

		if (fi.m_fs == kfsInserted || fi.m_fs == kfsModified)
		{
			m_fModified = true;
			if (fi.m_hvoOld)
			{
				Assert(fi.m_fs == kfsModified);

				// Delete the old rows (and the cells in them) for this filter.
				// This needs to go backwards so the index doesn't get messed up.
				int crow;
				CheckHr(qsda_oddeTemp->get_VecSize(fi.m_hvoOld, kflidCmFilter_Rows, &crow));
				for (int irow = crow; --irow >= 0; )
				{
					HVO hvoRow;
					CheckHr(qsda_oddeTemp->get_VecItem(fi.m_hvoOld, kflidCmFilter_Rows, irow,
						&hvoRow));
					// Must NOT just use DeleteObj, we NEED to update the cache, since
					// we use it to make later inserts work.
					CheckHr(qsda_oddeTemp->DeleteObjOwner(fi.m_hvoOld, hvoRow,
						kflidCmFilter_Rows, irow));
				}
			}
			else
			{
				Assert(fi.m_fs == kfsInserted);

				// Create the new filter.
				qsda_oddeTemp->MakeNewObject(kclidCmFilter, hvoLpId, kflidLangProject_Filters,
					-1, &(fi.m_hvoOld));
			}

			const CLSID * pclsid = AfApp::Papp()->GetAppClsid();
			StrUni stuQuery;
			stuQuery.Format(L"UPDATE CmFilter"
				L" SET Name = ?, App = '%g', ClassId = %d, Type = %d, ColumnInfo = ?,"
				L" ShowPrompt = %d, PromptText = ?"
				L" WHERE id = %d%n",
				pclsid, m_prmwMain->GetRecordClid(),
				!fi.m_fSimple, fi.m_fShowPrompt, fi.m_hvoOld);
			StrUtil::NormalizeStrUni(fi.m_stuName, UNORM_NFD);
			StrUtil::NormalizeStrUni(fi.m_stuColInfo, UNORM_NFD);
			StrUtil::NormalizeStrUni(fi.m_stuPrompt, UNORM_NFD);
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)(fi.m_stuName.Chars()), fi.m_stuName.Length() * 2));
			CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)(fi.m_stuColInfo.Chars()), fi.m_stuColInfo.Length() * 2));
			CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)(fi.m_stuPrompt.Chars()), fi.m_stuPrompt.Length() * 2));
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));

			_CopyFilterRows(fi.m_hvo, fi.m_hvoOld, m_qvcd, qvcd_oddeTemp, fi.m_fSimple);
		}
	}

	// Reorder the internal vector of filters by name, and remove deleted (or undefined)
	// filters.
	Vector<FilterInfo> vfi;
	for (iflt = 0; iflt < cflt; ++iflt)
	{
		FilterInfo & fi = m_vfi[iflt];
		if (fi.m_fs == kfsDeleted || (fi.m_fs == kfsInserted && !fi.m_stuColInfo.Length()))
			continue;
		int iv;
		int ivLim;
		for (iv = 0, ivLim = vfi.Size(); iv < ivLim; )
		{
			int ivMid = (iv + ivLim) / 2;
			if (vfi[ivMid].m_stuName < fi.m_stuName)
				iv = ivMid + 1;
			else
				ivLim = ivMid;
		}
		Assert(iv <= vfi.Size());
		vfi.Insert(iv, fi);
	}
	m_vfi = vfi;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only WM_ACTIVATE, WM_ERASEBKGND and WM_DESTROY are processed, and even then the message is
	passed on to the superclass's FWndProc method.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True or false: whatever the superclass's FWndProc method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ERASEBKGND)
	{
		// This is needed because of a bug in the list view control that causes
		// it not to be redrawn sometimes.
		::RedrawWindow(::GetDlgItem(m_hwnd, kctidFilterList), NULL, NULL,
			RDW_ERASE | RDW_FRAME | RDW_INVALIDATE);
	}
	else if (wm == WM_DESTROY)
		AfApp::Papp()->RemoveCmdHandler(this, 1);
	else if (wm == WM_ACTIVATE)
	{
		if (LOWORD(wp) == WA_INACTIVE)
		{
			// Remove our special accelerator table.
			AfApp::Papp()->RemoveAccelTable(m_atid);
		}
		else
		{
			// We load this basic accelerator table so that these commands can be directed to
			// this window.  This allows the embedded Views to see the commands.  Otherwise, if
			// they are translated by the main window, the main window is the 'target', and the
			// command handlers on AfVwRootSite don't work, because the root site is not a child
			// window of the main one.
			// I'm creating and destroying in Activate/Deactivate partly because I copied the
			// code from AfFindDialog, but also just to make sure this accel table can't be
			// accidentally used for other windows.
			m_atid = AfApp::Papp()->LoadAccelTable(kridAccelBasic, 0, m_hwnd);
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case LVN_ITEMCHANGED:
		{
			NMLISTVIEW * pnmlv = reinterpret_cast<NMLISTVIEW *>(pnmh);
			if (pnmlv->uChanged & LVIF_STATE)
			{
				Assert(ctidFrom == kctidFilterList);

				if (pnmlv->uNewState & LVIS_SELECTED)
				{
					// Save the HWND of the window currently in focus. This will usually be the
					// List View but it may be the Filter tab.
					HWND hwndSaved = ::GetFocus();
					// Show the new selected filter on the right side of the dialog.
					::EnableWindow(::GetDlgItem(m_hwnd, kctidDeleteFilter), true);
					bool fT = ShowFilterSubDlg(pnmlv->lParam);
					::SetFocus(hwndSaved);	// Restore the focus to where it was before.
					return fT;
				}
			}
			break;
		}

	case LVN_ITEMCHANGING:
		{
			// If the user clicked on an empty part of the list view, keep the selection
			// on the current item.
			NMLISTVIEW * pnmlv = reinterpret_cast<NMLISTVIEW *>(pnmh);
			if (pnmlv->uChanged & LVIF_STATE && !(pnmlv->uNewState & LVIS_SELECTED))
			{
				// NOTE: This can also be called when the keyboard is used to select a different
				// item. In this case, we don't want to cancel the new selection.
				if (::GetKeyState(VK_LBUTTON) < 0 || ::GetKeyState(VK_RBUTTON) < 0)
				{
					LVHITTESTINFO lvhti;
					::GetCursorPos(&lvhti.pt);
					::ScreenToClient(pnmh->hwndFrom, &lvhti.pt);
					if (ListView_HitTest(pnmh->hwndFrom, &lvhti) == -1)
					{
						lnRet = true;
						return true;
					}
				}
			}
		}
		break;

	case LVN_KEYDOWN:
		{
			NMLVKEYDOWN * pnmlvkd = reinterpret_cast<NMLVKEYDOWN *>(pnmh);
			if (pnmlvkd->wVKey == VK_DELETE)
				DeleteFilter();
			else if (pnmlvkd->wVKey == VK_F2)
			{
				int iitem = ListView_GetNextItem(pnmh->hwndFrom, -1, LVNI_SELECTED);
				if (iitem != -1)
					ListView_EditLabel(pnmh->hwndFrom, iitem);
			}
			break;
		}

	case LVN_ENDLABELEDIT:
		return OnEndLabelEdit(reinterpret_cast<NMLVDISPINFO *>(pnmh), lnRet);

	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidAddFilter:
			{
				// Create and show the popup menu that allows a user to create a new filter.
				HMENU hmenuPopup = ::CreatePopupMenu();
				if (!hmenuPopup)
					ThrowHr(WarnHr(E_FAIL));

				Rect rc;
				::GetWindowRect(::GetDlgItem(m_hwnd, kctidAddFilter), &rc);

				StrApp str(kcidAddSimpleFilter);
				::AppendMenu(hmenuPopup, MF_STRING, kcidAddSimpleFilter, str.Chars());
				str.Load(kcidAddFullFilter);
				::AppendMenu(hmenuPopup, MF_STRING, kcidAddFullFilter, str.Chars());

				::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left,
					rc.bottom, 0, m_hwnd, NULL);

				::DestroyMenu(hmenuPopup);
			}
			break;

		case kctidCopyFilter:
			{
				Assert((uint)m_ifltCurrent < (uint)m_vfi.Size());
				FilterInfo & fiOld = m_vfi[m_ifltCurrent];
				InsertFilter(fiOld.m_stuName.Chars(), fiOld.m_fSimple, &fiOld);
			}
			break;

		case kctidDeleteFilter:
			DeleteFilter();
			break;
		}
		break;

	default:
		break;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Handle an LVN_ENDLABELEDIT notification message by changing the name of the item if the new
	name is a unique, non-empty string.

	@param plvdi Pointer to the data for an LVN_ENDLABELEDIT notification message.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::OnEndLabelEdit(NMLVDISPINFO * plvdi, long & lnRet)
{
	AssertPtr(plvdi);

	if (plvdi->item.pszText)
	{
		AssertPsz(plvdi->item.pszText);

		// Strip off blank characters at the front and end of the name.
		StrApp strLabel;
		StrUtil::TrimWhiteSpace(plvdi->item.pszText, strLabel);

		if (strLabel.Length() == 0)
		{
			// The item is empty, so show a message complaining about it.
			StrApp strMessage(kstidFltrRenEmptyMsg);
			StrApp strFilter(kstidTlsOptFltr);
			::MessageBox(m_hwnd, strMessage.Chars(), strFilter.Chars(),
				MB_OK | MB_ICONINFORMATION | MB_SYSTEMMODAL);
			::PostMessage(plvdi->hdr.hwndFrom, LVM_EDITLABEL, plvdi->item.iItem, 0);
			return true;
		}

		HWND hwndList = ::GetDlgItem(m_hwnd, kctidFilterList);

		// See if there is already an item with the same name.
		LVFINDINFO lvfi = { LVFI_STRING };
		lvfi.psz = strLabel.Chars();
		int iitem = ListView_FindItem(hwndList, -1, &lvfi);
		int iflt = -1;
		if (iitem != -1)
		{
			LVITEM lvi = { LVIF_PARAM, iitem };
			ListView_GetItem(hwndList, &lvi);
			iflt = lvi.lParam;
		}
		// If they didn't change the name, we're done.
		if (iflt == m_ifltCurrent)
			return true;
		if (iflt != -1)
		{
			StrApp strMessage(kstidFltrRenFilterMsg);
			StrApp strFilter(kstidTlsOptFltr);
			::MessageBox(m_hwnd, strMessage.Chars(), strFilter.Chars(),
				MB_OK | MB_ICONINFORMATION | MB_SYSTEMMODAL);
			::PostMessage(plvdi->hdr.hwndFrom, LVM_EDITLABEL, plvdi->item.iItem, 0);
			return true;
		}

		// Update the name of the selected filter.
		FilterInfo & fi = m_vfi[m_ifltCurrent];
		Assert(fi.m_fs != kfsDeleted);
		fi.m_stuName = strLabel;
		if (fi.m_fs == kfsNormal)
			fi.m_fs = kfsModified;

		// This is necessary to reorder the list if needed due to the name change.
		UpdateFilterList();
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Create a new filter.

	@param pcmd Pointer to the command information for an "Add filter" command.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::CmdAddFilter(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_cid == kcidAddSimpleFilter || pcmd->m_cid == kcidAddFullFilter);

	StrUniBufPath stubp;
	bool fSimple;

	if (pcmd->m_cid == kcidAddSimpleFilter)
	{
		stubp.Load(kstidFltrNewSimple);
		fSimple = true;
	}
	else
	{
		stubp.Load(kstidFltrNewFull);
		fSimple = false;
	}

	InsertFilter(stubp.Chars(), fSimple, NULL);
	return true;
}


/*----------------------------------------------------------------------------------------------
	The user selected an item from the format popup menu.  (This method has not yet been fully
	implemented.)

	@param pcmd Pointer to the command information from the format popup menu.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::CmdFltrFormat(Cmd * pcmd)
{
	AssertPtr(pcmd);

	switch (pcmd->m_cid)
	{
	case kcidFltrFmtFont:
		// TODO DarrellZ: Implement this for Version 2.
		break;
	case kcidFltrFmtStyle:
		// TODO DarrellZ: Implement this for Version 2.
		break;
	case kcidFltrFmtNone:
		g_fu.SetSpecialEnc(0);
		// TODO DarrellZ: Implement this more when the Font and Style menu commands are
		// implemented.
		break;
	default:
		Assert(false); // This should never happen.
		break;
	}
	TssEditPtr qte = g_fu.GetTssEdit();
	if (qte)
	{
		::SetFocus(qte->Hwnd());
		g_fu.SetTssEdit(NULL);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	This method is used for three purposes.
@line	1) If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to expand the
			dummy item by adding new items.
@line	2) If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the
			status bar string for an expanded item.
@line	3) If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
			selected an expandable menu item.

	Expanding items:
@line	pcmd->m_rgn[1] -> Contains the handle to the menu (HMENU) to add items to.
@line	pcmd->m_rgn[2] -> Contains the index in the menu where you should start inserting items.
@line	pcmd->m_rgn[3] -> This value must be set to the number of items that you inserted.

	The expanded items will automatically be deleted when the menu is closed. The dummy
	menu item will be deleted for you, so don't do anything with it here.

	Getting the status bar text:
@line   pcmd->m_rgn[1] -> Contains the index of the expanded/inserted item to get text for.
@line   pcmd->m_rgn[2] -> Contains a pointer (StrApp *) to the text for the inserted item.

	If the menu item does not have any text to show on the status bar, return false.

	Performing the command:
@line   pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
@line   pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to get text for.

	@param pcmd Pointer to the command information.

	@return True if the command is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::CmdFltrSpcExpand(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_cid == kcidFltrSpcExpand);

	// This is used to handle the user-definable items on the Special popup menu. This menu is
	// shown in the FwFilterSimpleDlg window, both in the simple filter pane and the criteria
	// builder. It is also shown in the context menu for the view window in a full filter.

	int ma = pcmd->m_rgn[0];
	if (ma == AfMenuMgr::kmaExpandItem)
	{
		// We need to expand the dummy menu item.
		HMENU hmenu = reinterpret_cast<HMENU>(pcmd->m_rgn[1]);
		int imni = pcmd->m_rgn[2];
		int & cmniAdded = pcmd->m_rgn[3];

		StrAppBufPath strbp;
		Vector<FilterPatternInfo> & vfpi = g_fu.GetLanguageVariables();
		int cfpi = vfpi.Size();
		StrApp strName;
		StrApp strAbbr;
		for (int ifpi = 0; ifpi < cfpi; ifpi++)
		{
			strName.Assign(vfpi[ifpi].m_stuName.Chars());
			strAbbr.Assign(vfpi[ifpi].m_stuAbbrev.Chars());
			strbp.Format(_T("%s\t[%s]"), strName.Chars(), strAbbr.Chars());
			::InsertMenu(hmenu, imni + ifpi, MF_BYPOSITION, kcidMenuItemDynMin + ifpi,
				strbp.Chars());
		}
		cmniAdded = cfpi;
		return true;
	}
	else if (ma == AfMenuMgr::kmaGetStatusText)
	{
		Assert(false); // This shouldn't be called because we don't have a status bar to set.
	}
	else if (ma == AfMenuMgr::kmaDoCommand)
	{
		// The user selected an expanded menu item, so perform the command now.
		//    m_rgn[1] holds the menu handle.
		//    m_rgn[2] holds the index of the selected item.

		int ifpi = pcmd->m_rgn[2];
		Vector<FilterPatternInfo> & vfpi = g_fu.GetLanguageVariables();
		StrUni stu;
		stu.Format(L"[%s]", vfpi[ifpi].m_stuAbbrev.Chars());
		ITsStrFactoryPtr qtsf;
		ITsStringPtr qtss;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_prmwMain->UserWs(), &qtss);
		TssEditPtr qte = g_fu.GetTssEdit();
		if (qte)
		{
			qte->ReplaceSel(qtss);
			::SetFocus(qte->Hwnd());
			g_fu.SetTssEdit(NULL);
		}

		return true;
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Switch between showing the simple or full dialogs.

	@param ifltr Index into the internal table of filters.

	@return True if successful (the subdialog can be made active); otherwise, false.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::ShowFilterSubDlg(int ifltr)
{
	const int kdxCriteriaOffset = 7;
	const int kdyCriteriaOffset = 17;

	Assert((uint)ifltr < (uint)m_vfi.Size());
	FilterInfo & fi = m_vfi[ifltr];

	if (m_pdlgvLast)
	{
		// Save the state of the previous filter, and hide its subdialog.
		if (!m_pdlgvLast->Apply())
			return false;
		::ShowWindow(m_pdlgvLast->Hwnd(), SW_HIDE);
	}

	m_ifltCurrent = ifltr;

	if (fi.m_fSimple)
	{
		if (m_qfltss)
		{
			// The dialog has already been created.
			m_qfltss->RefreshFilterDisplay();
		}
		else
		{
			// The dialog has not already been created, so create it now.
			Rect rc;
			HWND hwndT = ::GetDlgItem(m_hwnd, kctidFilterCriteria);
			::GetWindowRect(hwndT, &rc);
			::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);

			m_qfltss.Create();
			m_qfltss->SetDialogValues(this, m_qvcd);
			m_qfltss->DoModeless(m_hwnd);
			::SetWindowPos(m_qfltss->Hwnd(), NULL, rc.left + kdxCriteriaOffset,
				rc.top + kdyCriteriaOffset, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
		}

		m_pdlgvLast = m_qfltss;
	}
	else
	{
		if (m_qfltf)
		{
			// The dialog has already been created.
			m_qfltf->RefreshFilterDisplay();
		}
		else
		{
			// The dialog has not already been created, so create it now.
			Rect rc;
			HWND hwndT = ::GetDlgItem(m_hwnd, kctidFilterCriteria);
			::GetWindowRect(hwndT, &rc);
			::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);

			m_qfltf.Create();
			m_qfltf->SetDialogValues(this, m_qvcd, NULL);
			m_qfltf->DoModeless(m_hwnd);
			::SetWindowPos(m_qfltf->Hwnd(), NULL, rc.left + kdxCriteriaOffset,
				rc.top + kdyCriteriaOffset, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
		}

		m_pdlgvLast = m_qfltf;
	}

	if (!m_pdlgvLast->SetActive())
		return false;

	::ShowWindow(m_pdlgvLast->Hwnd(), SW_SHOW);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Update the list view with the filter information.
----------------------------------------------------------------------------------------------*/
void FwFilterDlg::UpdateFilterList()
{
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidFilterList);

	int iitemOld = 0;
	if (m_ifltCurrent != -1)
	{
		LVFINDINFO lvfi = { LVFI_PARAM };
		lvfi.lParam = m_ifltCurrent;
		iitemOld = ListView_FindItem(hwndList, -1, &lvfi);
	}

	::SendMessage(hwndList, WM_SETREDRAW, false, 0);
	ListView_DeleteAllItems(hwndList);

	// Insert items in the list view for all non-deleted filters.
	LVITEM lvi = { LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM };
	int cflt = m_vfi.Size();
	for (int iflt = 0; iflt < cflt; iflt++)
	{
		lvi.iItem = iflt;
		FilterInfo & fi = m_vfi[iflt];
		if (fi.m_fs == kfsDeleted)
			continue;
		StrApp str(fi.m_stuName.Chars());
		lvi.pszText = const_cast<achar *>(str.Chars());
		lvi.iImage = fi.m_fSimple ? 0 : 1;
		lvi.lParam = iflt;
		ListView_InsertItem(hwndList, &lvi);
	}

	if (m_ifltCurrent != -1)
	{
		// Find the index of the item that was previously selected.
		LVFINDINFO lvfi = { LVFI_PARAM };
		lvfi.lParam = m_ifltCurrent;
		int iitemNew = ListView_FindItem(hwndList, -1, &lvfi);

		if (iitemNew == -1)
		{
			iitemNew = iitemOld;

			// The old current selection is not in the list, so determine which item to select.
			int citem = ListView_GetItemCount(hwndList);
			if ((uint)iitemNew >= (uint)citem)
				iitemNew = citem - 1;
		}
		Assert(iitemNew != -1 || ListView_GetItemCount(hwndList) == 0);
		if (iitemNew != -1)
		{
			ListView_SetItemState(hwndList, iitemNew, LVIS_FOCUSED | LVIS_SELECTED,
				LVIS_FOCUSED | LVIS_SELECTED);
			ListView_EnsureVisible(hwndList, iitemNew, false);
		}
	}

	// If there aren't any filters, hide the subdialogs on the right side and disable
	// the Copy Filter and Delete Filter buttons.
	int citem = ListView_GetItemCount(hwndList);
	if (citem == 0)
	{
		if (m_qfltss)
			::ShowWindow(m_qfltss->Hwnd(), SW_HIDE);
		if (m_qfltf)
			::ShowWindow(m_qfltf->Hwnd(), SW_HIDE);
	}
	bool fEnable = citem > 0;
	::EnableWindow(::GetDlgItem(m_hwnd, kctidCopyFilter), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidDeleteFilter), fEnable);

	::SendMessage(hwndList, WM_SETREDRAW, true, 0);
	::InvalidateRect(hwndList, NULL, true);
}


/*----------------------------------------------------------------------------------------------
	This method loads the information for the current filter into our dummy cache if it hasn't
	already been loaded.

	@param vvfmnColumns Reference to a vector of filter column definition vectors, used to
					return a complete filter specification.

	@return The columns and the filter information.
----------------------------------------------------------------------------------------------*/
FwFilterDlg::FilterInfo & FwFilterDlg::LoadCurrentFilter(
	Vector<FilterMenuNodeVec> & vvfmnColumns)
{
	AssertPtr(m_qvcd);
	IDbColSpecPtr qdcs;

	FilterInfo & fi = GetCurrentFilterInfo();
	FilterUtil::BuildColumnsVector(GetLpInfo(), fi.m_stuColInfo, vvfmnColumns, MainWindow());

	if (!fi.m_hvo)
	{
		// The filter has not been loaded into our dummy cache yet, so load it now.
		// We have to load it from the database into a VwOleDbDa cache, then copy everything
		// in the filter to our dummy cache (IVwCacheDa). This is required so we can cancel
		// out of the dialog if the user decides not to keep their changes.

		// Load the filter row information into the temporary cache.
		StrUni stuSql;
		stuSql.Format(L"select fr.dst from CmFilter_Rows as fr "
			L"where fr.src = %d order by fr.ord", fi.m_hvoOld);
		qdcs.CreateInstance(CLSID_DbColSpec);
		qdcs->Push(koctObjVec, 0, kflidCmFilter_Rows, 0);

		// Execute the query and store results in the temporary cache.
		m_qodde->Load(stuSql.Bstr(), qdcs, fi.m_hvoOld, 0, NULL, NULL);

		// Load the filter rows into the temporary cache.
		stuSql.Format(L"select fr.dst, frc.dst, fc.Contents, fc.Contents_Fmt "
			L"from CmFilter_Rows as fr "
			L"	left outer join CmRow_Cells as frc on frc.src = fr.dst "
			L"	left outer join CmCell as fc on fc.id = frc.dst "
			L"where fr.src = %d order by fr.dst, frc.ord",
			fi.m_hvoOld);
		qdcs->Clear();
		qdcs->Push(koctBaseId, 0, 0, 0);
		qdcs->Push(koctObjVec, 1, kflidCmRow_Cells, 0);
		qdcs->Push(koctString, 2, kflidCmCell_Contents, 0);
		qdcs->Push(koctFmt, 2, kflidCmCell_Contents, 0);

		// Execute the query and store results in the temporary cache.
		m_qodde->Load(stuSql.Bstr(), qdcs, fi.m_hvoOld, 0, NULL, NULL);

		// Copy all the filter information into our dummy cache.
		ISilDataAccessPtr qsda;
		CheckHr(m_qodde->QueryInterface(IID_ISilDataAccess, (void**)&qsda));
		Assert(qsda);
		// If this filter is not changed, the code leaks an empty CmFilter object into
		// the database every time through here.
		qsda->MakeNewObject(kclidCmFilter, m_prmwMain->GetLpInfo()->GetLpId(),
			kflidLangProject_Filters, -1, &(fi.m_hvo));
		IVwCacheDaPtr qvcd_oddeTemp;
		CheckHr(m_qodde->QueryInterface(IID_IVwCacheDa, (void**)&qvcd_oddeTemp));
		Assert(qvcd_oddeTemp);
		_CopyFilterRows(fi.m_hvoOld, fi.m_hvo, qvcd_oddeTemp, m_qvcd, fi.m_fSimple);
	}

	return fi;
}


/*----------------------------------------------------------------------------------------------
	Delete the currently selected filter in the list view.
----------------------------------------------------------------------------------------------*/
void FwFilterDlg::DeleteFilter()
{
	Assert((uint)m_ifltCurrent < (uint)m_vfi.Size());

	StrApp strTitle(kstidDeleteFilter);
	StrApp strPrompt(kstidFltrDelFilterMsg);

	const achar * pszHelpUrl = m_pszHelpUrl;
	m_pszHelpUrl = _T("Basic_Tasks/Filtering/Delete_a_filter.htm");

	ConfirmDeleteDlgPtr qcdd;
	qcdd.Create();
	qcdd->SetTitle(strTitle.Chars());
	qcdd->SetPrompt(strPrompt.Chars());
	qcdd->SetHelpUrl(m_pszHelpUrl);
	// Make sure the user really wants to delete the filter.
	if (qcdd->DoModal(m_hwnd) != kctidOk)
	{
		m_pszHelpUrl = pszHelpUrl;
		return;
	}

	WaitCursor wc;

	m_pszHelpUrl = pszHelpUrl;

	FilterInfo & fi = m_vfi[m_ifltCurrent];
	Assert(fi.m_fs != kfsDeleted);
	fi.m_fs = kfsDeleted;

	if (m_pdlgvLast)
	{
		ShowChildren(false);
		::ShowWindow(m_pdlgvLast->Hwnd(), SW_HIDE);
	}
	m_pdlgvLast = NULL;

	UpdateFilterList();
}


/*----------------------------------------------------------------------------------------------
	Create a new filter and add it to the database. If pfiCopy is not NULL, the new filter
	will be copied from it.
	Simple filters should always have one row and one column, even if we're creating an empty
	one.

	@param pszName Name of the new filter to add to the database.
	@param fSimple Flag whether this is a simple (one column/one row) filter.
	@param pfiCopy Pointer to the information for an existing filter to copy (may be NULL).
----------------------------------------------------------------------------------------------*/
void FwFilterDlg::InsertFilter(const wchar * pszName, bool fSimple, FilterInfo * pfiCopy)
{
	AssertPsz(pszName);
	AssertPtrN(pfiCopy);

	if (m_pdlgvLast)
	{
		// Save the state of the old filter, and hide its subdialog.
		if (!m_pdlgvLast->Apply())
			return;
		::ShowWindow(m_pdlgvLast->Hwnd(), SW_HIDE);
		m_pdlgvLast = NULL;		// Prevent copying old filter's value to the new filter.
	}
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidFilterList);

	StrApp strName(pszName);
	m_ptod->FixName(strName, hwndList, pfiCopy != NULL);
	StrUni stuName(strName);

	FilterInfo fi;
	fi.m_fs = kfsInserted;
	ISilDataAccessPtr qsda;
	CheckHr(m_qodde->QueryInterface(IID_ISilDataAccess, (void**)&qsda));
	Assert(qsda);
	qsda->MakeNewObject(kclidCmFilter, m_prmwMain->GetLpInfo()->GetLpId(),
		kflidLangProject_Filters, -1, &(fi.m_hvo));
	fi.m_fSimple = fSimple;
	fi.m_stuName = stuName;

	if (pfiCopy)
	{
		fi.m_fShowPrompt = pfiCopy->m_fShowPrompt;
		fi.m_stuPrompt = pfiCopy->m_stuPrompt;

		// Copy the column information.
		fi.m_stuColInfo = pfiCopy->m_stuColInfo;

		// Copy the rows and cells from the old filter to the new one.
		_CopyFilterRows(pfiCopy->m_hvo, fi.m_hvo, m_qvcd, m_qvcd, pfiCopy->m_fSimple);
	}
	else if (fSimple)
	{
		// For simple filters, we automatically create one row and one cell.
		HVO hvoFilter = fi.m_hvo;
		HVO hvoRow;
		HVO hvoDud;
		qsda->MakeNewObject(kclidCmRow, hvoFilter, kflidCmFilter_Rows, 0, &hvoRow);
		qsda->MakeNewObject(kclidCmCell, hvoRow, kflidCmRow_Cells, 0, &hvoDud);
		IVwCacheDaPtr qvcd_oddeTemp;
		CheckHr(m_qodde->QueryInterface(IID_IVwCacheDa, (void**)&qvcd_oddeTemp));
		Assert(qvcd_oddeTemp);
		_CopyFilterRows(fi.m_hvo, fi.m_hvo, qvcd_oddeTemp, m_qvcd, fi.m_fSimple);
	}

	m_vfi.Push(fi);
	m_ifltCurrent = -1;
	UpdateFilterList();
	m_ifltCurrent = m_vfi.Size() - 1;

	// Select the new filter in the list.
	LVFINDINFO lvfi = { LVFI_PARAM };
	lvfi.lParam = m_ifltCurrent;
	int iitem = ListView_FindItem(hwndList, -1, &lvfi);
	Assert(iitem != -1);
	ListView_SetItemState(hwndList, iitem, LVIS_FOCUSED | LVIS_SELECTED,
		LVIS_FOCUSED | LVIS_SELECTED);
	ListView_EnsureVisible(hwndList, iitem, false);
	::SendMessage(m_hwnd, WM_NEXTDLGCTL, (WPARAM)hwndList, true);
	ListView_EditLabel(hwndList, iitem);
}


/*----------------------------------------------------------------------------------------------
	Copy the row and column/cell information from a filter in one cache to another filter
	in another (or the same--pvcdNew and pvcdOld can be the same) cache.

	@param hvoFilterOld Database ID for the filter stored in pvcdOld.
	@param hvoFilterNew Database ID for a filter stored in pvcdNew.
	@param pvcdOld Pointer to a data cache containing filter information.
	@param pvcdNew Pointer to another (or the same) data cache.
	@param fSimple Flag whether this is a simple (one column/one row) filter.
----------------------------------------------------------------------------------------------*/
void FwFilterDlg::_CopyFilterRows(HVO hvoFilterOld, HVO hvoFilterNew, IVwCacheDa * pvcdOld,
	IVwCacheDa * pvcdNew, bool fSimple)
{
	Assert(hvoFilterOld && hvoFilterNew);
	// If the caches are the same, we better not be trying to copy over the same filter.
	Assert(!(pvcdOld == pvcdNew && hvoFilterOld == hvoFilterNew));
	AssertPtr(pvcdOld);
	AssertPtr(pvcdNew);

	HVO hvoRowOld;
	HVO hvoRow;
	HVO hvoCellOld;
	HVO hvoCell;
	int crow;
	int ccol;
	ITsStringPtr qtss;
	ISilDataAccessPtr qsdaOld;
	HRESULT hr = pvcdOld->QueryInterface(IID_ISilDataAccess, (void **)&qsdaOld);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);
	ISilDataAccessPtr qsdaNew;
	hr = pvcdNew->QueryInterface(IID_ISilDataAccess, (void**)&qsdaNew);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);

	CheckHr(qsdaOld->get_VecSize(hvoFilterOld, kflidCmFilter_Rows, &crow));
	// If we are copying a simple filter, make sure we only have one row.
	Assert(!fSimple || crow == 1);
	for (int irow = 0; irow < crow; irow++)
	{
		// Make a copy of the row.
		CheckHr(qsdaOld->get_VecItem(hvoFilterOld, kflidCmFilter_Rows, irow, &hvoRowOld));
		if (irow == 0)
		{
			CheckHr(qsdaOld->get_VecSize(hvoRowOld, kflidCmRow_Cells, &ccol));
			// If we are copying a simple filter, make sure we only have one column.
			Assert(!fSimple || ccol == 1);
		}

		qsdaNew->MakeNewObject(kclidCmRow, hvoFilterNew, kflidCmFilter_Rows, irow, &hvoRow);

		for (int icol = 0; icol < ccol; icol++)
		{
			// Make a copy of the cell.
			CheckHr(qsdaOld->get_VecItem(hvoRowOld, kflidCmRow_Cells, icol, &hvoCellOld));
			CheckHr(qsdaOld->get_StringProp(hvoCellOld, kflidCmCell_Contents, &qtss));

			qsdaNew->MakeNewObject(kclidCmCell, hvoRow, kflidCmRow_Cells, icol, &hvoCell);
			CheckHr(qsdaNew->SetString(hvoCell, kflidCmCell_Contents, qtss));
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Show or hide child windows belonging to the filter dialog tab.

	@param fShow Flag whether to show or to hide the child windows.
----------------------------------------------------------------------------------------------*/
void FwFilterDlg::ShowChildren(bool fShow)
{
	if (m_qfltf && dynamic_cast <FwFilterFullDlg *> (m_pdlgvLast))
		m_qfltf->ShowChildren(fShow);	// Show only if current filter is a full filter.
}

/*----------------------------------------------------------------------------------------------
	Called when the dialog becomes active.

	@return true
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::SetActive()
{
	UpdateFilterList();
/*	HWND hwndFIn = ::GetDlgItem(m_hwnd, kcidTlsOptDlgGenFIn);
	::SendMessage(hwndFIn, CB_SETCURSEL, m_ptod->CurObjVecIndex(), 0);
	UpdateFlds();
*/
	return true;
}


//:>********************************************************************************************
//:>	FwFilterSimpleShellDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterSimpleShellDlg::FwFilterSimpleShellDlg()
{
	m_rid = kridFilterSimpleShellDlg;
	m_hmenuPopup = NULL;
	m_fEditedPrompt = false;
	m_hwndToolTip = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwFilterSimpleShellDlg::~FwFilterSimpleShellDlg()
{
	if (m_hmenuPopup)
	{
		::DestroyMenu(m_hmenuPopup);
		m_hmenuPopup = NULL;
	}
#ifdef TimP_2002_10_Invalid
	// It appears tooltip "windows" should not be "DestroyWindow"ed.
	// This DestroyWindow call cause an error that GetLastError reports as "1400".
	if (m_hwndToolTip)
	{
		BOOL flag  = ::DestroyWindow(m_hwndToolTip);
		if (!flag)
		{
			CHAR szBuf[80];
			DWORD dw = GetLastError();

			sprintf(szBuf, "%s failed: GetLastError returned %u.\n",
				"DestroyWindow", dw);

			MessageBox(NULL, szBuf, "Error", MB_OK);
		}
		m_hwndToolTip = NULL;
	}
#endif
}


/*----------------------------------------------------------------------------------------------
	Store initial dialog values.

	@param pfltdlg Pointer to the filter dialog pane object.
	@param pvcd Pointer to the data cache containing the filter information.
----------------------------------------------------------------------------------------------*/
void FwFilterSimpleShellDlg::SetDialogValues(FwFilterDlg * pfltdlg, IVwCacheDa * pvcd)
{
	AssertPtr(pfltdlg);
	AssertPtr(pvcd);

	m_pfltdlg = pfltdlg;
	m_qvcd = pvcd;
}


/*----------------------------------------------------------------------------------------------
	The user has selected a different filter.
----------------------------------------------------------------------------------------------*/
void FwFilterSimpleShellDlg::RefreshFilterDisplay()
{
	AssertPtr(m_pfltdlg);

	m_fEditedPrompt = false;

	ISilDataAccessPtr qsda_vcdTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);

	Vector<FilterMenuNodeVec> vvfmnColumns;
	FwFilterDlg::FilterInfo & fi = m_pfltdlg->LoadCurrentFilter(vvfmnColumns);
	Assert(vvfmnColumns.Size() <= 1);
	Assert(fi.m_fSimple == true);
	Assert(fi.m_hvo);

	::CheckDlgButton(m_hwnd, kctidCriteria, fi.m_fShowPrompt ? BST_CHECKED : BST_UNCHECKED);

	// Set the button text and tooltip to the appropriate strings.
	FilterMenuNodeVec * pvfmn = NULL;
	StrApp str;
	StrApp strTip;
	if (vvfmnColumns.Size() == 1)
	{
		pvfmn = &vvfmnColumns[0];
		FilterUtil::GetColumnName(vvfmnColumns[0], false, str);
		FilterUtil::GetColumnName(vvfmnColumns[0], true, strTip);
	}
	else
	{
		str.Load(kstidFltrChooseFieldNHK);
		strTip = str;
		fi.m_fShowPrompt = false;
	}
	::SetDlgItemText(m_hwnd, kctidField, str.Chars());

	TOOLINFO ti = { isizeof(ti) };
	ti.hwnd = m_hwnd;
	ti.uId = (uint)::GetDlgItem(m_hwnd, kctidField);
	ti.lpszText = const_cast<achar *>(strTip.Chars());
	::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, 0, (LPARAM)&ti);

	StrApp strDefPrompt;
	StrApp strPrompt;
	StrApp strCondition;
	ITsStringPtr qtss;

	if (pvfmn)
	{
		// Get the cell text for this simple filter.
		HVO hvoRow0;
		HVO hvoCell0;
#ifdef DEBUG
		{
			// Simple filters should always have exactly one row.
			int crow;
			CheckHr(qsda_vcdTemp->get_VecSize(fi.m_hvo, kflidCmFilter_Rows, &crow));
			Assert(crow == 1);
		}
#endif
		CheckHr(qsda_vcdTemp->get_VecItem(fi.m_hvo, kflidCmFilter_Rows, 0, &hvoRow0));
#ifdef DEBUG
		{
			// Simple filters should always have exactly one column.
			int ccol;
			CheckHr(qsda_vcdTemp->get_VecSize(hvoRow0, kflidCmRow_Cells, &ccol));
			Assert(ccol == 1);
		}
#endif
		CheckHr(qsda_vcdTemp->get_VecItem(hvoRow0, kflidCmRow_Cells, 0, &hvoCell0));
		CheckHr(qsda_vcdTemp->get_StringProp(hvoCell0, kflidCmCell_Contents, &qtss));
		const OLECHAR * prgwch;
		int cch;
		CheckHr(qtss->LockText(&prgwch, &cch));
		FilterUtil::GetSimpleFilterPrompt(prgwch, m_pfltdlg->WritingSystemFactory(),
			*pvfmn, strDefPrompt, strCondition);
		qtss->UnlockText(prgwch);

		if (fi.m_stuPrompt.Length() == 0)
		{
			strPrompt = strDefPrompt;
		}
		else
		{
			strPrompt = fi.m_stuPrompt;
			m_fEditedPrompt = true;
		}
	}

	// Since setting the prompt text causes m_fEditedPrompt to be set to true, we store
	// what it is now, and reset it after setting the prompt text.
	bool fOldEditedPrompt = m_fEditedPrompt;
	::SetDlgItemText(m_hwnd, kctidPrompt, strPrompt.Chars());
	m_fEditedPrompt = fOldEditedPrompt;

	if (!m_qflts)
	{
		// The subdialog has not been created, so create it now.
		Rect rc;
		::GetWindowRect(::GetDlgItem(m_hwnd, kctidField), &rc);
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);

		// Vertical space between the field button and the simple filter dialog.
		const int kdypMargin = 4;

		m_qflts.Create();
		m_qflts->SetDialogValues(m_pfltdlg->GetLpInfo(), pvfmn, qtss);
		m_qflts->DoModeless(m_hwnd);
		::SetWindowPos(m_qflts->Hwnd(), ::GetDlgItem(m_hwnd, kctidField), 0,
			rc.bottom + kdypMargin, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
	}
	else
	{
		m_qflts->RefreshFilterDisplay(pvfmn, qtss);
	}

	m_qflts->SetActive();
	::ShowWindow(m_qflts->Hwnd(), SW_SHOW);
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param hwndCtrl Handle passed on to the superclass method.
	@param lp Long parameter passed on the superclass method.

	@return True or false: whatever the superclass's OnInitDlg method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterSimpleShellDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Create the popup menu from the menu node structures.
	m_hmenuPopup = FilterUtil::CreatePopupMenu(m_pfltdlg->GetLpInfo(), MainWindow());

	FwFilterButtonPtr qfbtn;
	qfbtn.Create();
	qfbtn->Create(m_hwnd, kctidField, m_pfltdlg);

	FwFilterStaticPtr qffs;
	qffs.Create();
	qffs->SubclassStatic(::GetDlgItem(m_hwnd, kctidFltrFieldLabel));
	StrApp str(kctidFltrFieldLabel);
	::SetDlgItemText(m_hwnd, kctidFltrFieldLabel, str.Chars());

	m_hwndToolTip = ::CreateWindowEx(0, TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP,
		0, 0, 0, 0, m_hwnd, 0, ModuleEntry::GetModuleHandle(), NULL);
	TOOLINFO ti = { isizeof(ti), TTF_SUBCLASS | TTF_IDISHWND };
	ti.hwnd = m_hwnd;
	ti.uId = (uint)qfbtn->Hwnd();
	ti.lpszText = _T(""); // This gets changed later (in RefreshFilterDisplay).
	::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);

	RefreshFilterDisplay();

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Get the cell value from the subdialog (FwFilterSimpleDlg) and save it in our dummy cache.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterSimpleShellDlg::Apply()
{
	AssertPtr(m_pfltdlg);

	ISilDataAccessPtr qsda_vcdTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);

	// Update the information for this filter.
	FwFilterDlg::FilterInfo & fi = m_pfltdlg->GetCurrentFilterInfo();
	Assert(fi.m_fs != FwFilterDlg::kfsDeleted);

	if (!m_qflts)
		RefreshFilterDisplay();

	if (!m_qflts->Apply())
		return false;

	// Store the cell value in our dummy cache.
	ITsStringPtr qtss;
	m_qflts->GetDialogValues(&qtss);

	if (qtss)
	{
		HVO hvoRow0;
		HVO hvoCell0;
#ifdef DEBUG
		{
			// Simple filters should always have exactly one row.
			int crow;
			CheckHr(qsda_vcdTemp->get_VecSize(fi.m_hvo, kflidCmFilter_Rows, &crow));
			Assert(crow == 1);
		}
#endif
		CheckHr(qsda_vcdTemp->get_VecItem(fi.m_hvo, kflidCmFilter_Rows, 0, &hvoRow0));
#ifdef DEBUG
		{
			// Simple filters should always have exactly one column.
			int ccol;
			CheckHr(qsda_vcdTemp->get_VecSize(hvoRow0, kflidCmRow_Cells, &ccol));
			Assert(ccol == 1);
		}
#endif
		CheckHr(qsda_vcdTemp->get_VecItem(hvoRow0, kflidCmRow_Cells, 0, &hvoCell0));
		CheckHr(qsda_vcdTemp->SetString(hvoCell0, kflidCmCell_Contents, qtss));
	}

	fi.m_fShowPrompt = ::IsWindowEnabled(::GetDlgItem(m_hwnd, kctidCriteria)) &&
		::IsDlgButtonChecked(m_hwnd, kctidCriteria) == BST_CHECKED;

	if (m_fEditedPrompt)
	{
		achar rgchBuffer[MAX_PATH];
		::GetDlgItemText(m_hwnd, kctidPrompt, rgchBuffer, isizeof(rgchBuffer) / isizeof(achar));
		fi.m_stuPrompt = rgchBuffer;
	}

	// TODO DarrellZ: Remove this if we find a better way of finding out when a filter has
	// been modified.
	if (fi.m_fs == FwFilterDlg::kfsNormal)
		fi.m_fs = FwFilterDlg::kfsModified;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool FwFilterSimpleShellDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidField:
			{
				Rect rc;
				::GetWindowRect(::GetDlgItem(m_hwnd, kctidField), &rc);

				// Show the popup-menu
				AfApp::GetMenuMgr()->SetMenuHandler(kcidSimpleFilterPopupMenu);
				::TrackPopupMenu(m_hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left,
					rc.bottom, 0, m_hwnd, NULL);
				return true;
			}

		case kctidCriteria:
			{
				bool fEnable = ::IsDlgButtonChecked(m_hwnd, kctidCriteria) == BST_CHECKED;
				HWND hwndPrompt = ::GetDlgItem(m_hwnd, kctidPrompt);
				::EnableWindow(hwndPrompt, fEnable);
				::EnableWindow(::GetDlgItem(m_hwnd, kctidPromptLabel), fEnable);
				if (fEnable)
				{
					::SetFocus(hwndPrompt);
					::SendMessage(hwndPrompt, EM_SETSEL, 0, -1);
				}
				return true;
			}
		}
		break;

	case EN_UPDATE:
		m_fEditedPrompt = true;
		break;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only the private s_wmConditionChanged message is processed, and even then the message is
	passed on to the superclass's FWndProc method.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True or false: whatever the superclass's FWndProc method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterSimpleShellDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == s_wmConditionChanged)
	{
		// If the new condition is 'Empty' or 'Not empty', we need to disable the prompt
		// part of the dialog.
		FilterKeywordType fkt = (FilterKeywordType)lp;
		if (fkt == kfktError)
		{
			FilterFieldType fft = (FilterFieldType)wp;
			fkt = FilterUtil::GetKeywordType(fft, m_qflts->Hwnd());
		}
		bool fEnable = fkt != kfktEmpty && fkt != kfktNotEmpty && fkt != kfktError;
		if (fEnable)
			fEnable = m_qflts->IsCriteriaPromptAllowed();
		::EnableWindow(::GetDlgItem(m_hwnd, kctidCriteria), fEnable);
		bool fChecked = ::IsDlgButtonChecked(m_hwnd, kctidCriteria);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidPromptLabel), fEnable && fChecked);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidPrompt), fEnable && fChecked);

		if (!m_fEditedPrompt || ::GetWindowTextLength(::GetDlgItem(m_hwnd, kctidPrompt)) == 0)
		{
			// Change the automatic prompt to reflect the change.
			StrApp strPrompt;
			Vector<FilterMenuNodeVec> vvfmnColumns;
			FwFilterDlg::FilterInfo & fi = m_pfltdlg->LoadCurrentFilter(vvfmnColumns);
			if (fEnable)
			{
				Assert(vvfmnColumns.Size() <= 1);

				if (vvfmnColumns.Size() == 1)
				{
					StrApp strCondition;
					FilterUtil::GetSimpleFilterPrompt(NULL, m_pfltdlg->WritingSystemFactory(),
						vvfmnColumns[0], strPrompt, strCondition, &fkt);
				}
			}
			::SetWindowText(::GetDlgItem(m_hwnd, kctidPrompt), strPrompt.Chars());
			// Clear the prompt string for this filter so the default is used next time.
			fi.m_stuPrompt.Clear();
			// Since the above line changed m_fEditedPrompt to true, set it back to false.
			m_fEditedPrompt = false;
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	This method is usually used for three purposes, but in this case only the last case will
	ever get called.
@line	1) If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to expand the dummy
			item by adding new items.
@line	2) If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the
			status bar string for an expanded item.
@line	3) If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
			selected an expandable menu item.

	Performing the command:
@line   pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
@line   pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to get text for.

	We are using this method to take advantage of the expandable menus functionality.
	See ${FilterUtil#CreatePopupMenu} for more information on why we're doing it this way.

	@param pcmd Pointer to the command information.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterSimpleShellDlg::CmdFieldPopup(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand);

	// The user selected an expanded menu item, so perform the command now.
	//    m_rgn[1] holds the menu handle.
	//    m_rgn[2] holds the index of the selected item.
	FilterMenuNodeVec vfmn;
	FilterMenuNodeVec & vfmnFlat = MainWindow()->FlatFilterMenuNodeVec();
	FilterUtil::BuildColumnVector(vfmnFlat, pcmd->m_rgn[2], vfmn);

	// Update the column information in the filter structure.
	AssertPtr(m_pfltdlg);
	FwFilterDlg::FilterInfo & fi = m_pfltdlg->GetCurrentFilterInfo();
	Assert(fi.m_fs != FwFilterDlg::kfsDeleted);
	Vector<FilterMenuNodeVec> vvfmnColumns;
	vvfmnColumns.Push(vfmn);
	FilterUtil::BuildColumnsString(vvfmnColumns, fi.m_stuColInfo);

	// Update the button text.
	StrApp str;
	FilterUtil::GetColumnName(vfmn, false, str);
	::SetDlgItemText(m_hwnd, kctidField, str.Chars());

	// Update the tooltip text.
	FilterUtil::GetColumnName(vfmn, true, str);
	TOOLINFO ti = { isizeof(ti) };
	ti.hwnd = m_hwnd;
	ti.uId = (uint)::GetDlgItem(m_hwnd, kctidField);
	ti.lpszText = const_cast<achar *>(str.Chars());
	::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, 0, (LPARAM)&ti);

	if (!m_fEditedPrompt)
	{
		// Since the field was just changed, we have to regenerate the default prompt.
		StrApp strDefPrompt;
		StrApp strCondition;
		FilterUtil::GetSimpleFilterPrompt(NULL, m_pfltdlg->WritingSystemFactory(),
			vfmn, strDefPrompt, strCondition);
		::SetDlgItemText(m_hwnd, kctidPrompt, strDefPrompt.Chars());
		// Since m_fEditedPrompt was set to true by the above line, set it back to false.
		m_fEditedPrompt = false;
	}

	AssertPtr(m_qflts);
	m_qflts->RefreshFilterDisplay(&vfmn, NULL);
	return true;
}


//:>********************************************************************************************
//:>	FwFilterBuilderShellDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterBuilderShellDlg::FwFilterBuilderShellDlg()
{
	m_rid = kridFilterBuilderShellDlg;
	m_hvoFilter = 0;
	m_pfltdlg = NULL;
	m_pfltf = NULL;
	m_fValidCriterion = false;
	m_fValidInsertionPt = false;
	m_st = kstNone;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_Filters_tab.htm");
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwFilterBuilderShellDlg::~FwFilterBuilderShellDlg()
{
}


/*----------------------------------------------------------------------------------------------
	Store initial dialog values.

	@param pfltdlg Pointer to the filter dialog pane object.
	@param hvoFilter Database ID of the current filter.
	@param pvcd Pointer to the data cache containing the filter information.
	@param pfltf Pointer to the full filter dialog connected to this builder shell dialog.
	@param icol Column index of the current filter cell.
	@param irow Row index of the current filter cell.
	@param ichAnchor End point of the selection in the current filter cell string.
	@param ichEnd The other end point of the selection in the cell string.
----------------------------------------------------------------------------------------------*/
void FwFilterBuilderShellDlg::SetDialogValues(FwFilterDlg * pfltdlg, HVO hvoFilter,
	IVwCacheDa * pvcd, FwFilterFullDlg * pfltf, int icol, int irow, int ichAnchor, int ichEnd)
{
	AssertPtr(pfltdlg);
	Assert(hvoFilter);
	AssertPtr(pvcd);
	AssertPtr(pfltf);

	m_pfltdlg = pfltdlg;
	m_hvoFilter = hvoFilter;
	m_qvcd = pvcd;
	m_pfltf = pfltf;
	m_icol = icol;
	m_irow = irow;
	m_ichAnchor = ichAnchor;
	m_ichEnd = ichEnd;
}


/*----------------------------------------------------------------------------------------------
	The user has selected a different filter.  Update the dialog display accordingly.

	@param hvoFilter Database ID of the new filter.
	@param icol Column index of the current cell in the new filter.
	@param irow Row index of the current cell in the new filter.
	@param ichAnchor End point of the selection in the current cell string of the new filter.
	@param ichEnd The other end point of the selection in the cell string.
----------------------------------------------------------------------------------------------*/
void FwFilterBuilderShellDlg::RefreshFilterDisplay(HVO hvoFilter, int icol, int irow,
	int ichAnchor, int ichEnd)
{
	Assert(hvoFilter);
	m_hvoFilter = hvoFilter;

	// icol can be -1 when the criteria builder is showing for a filter that does not have
	// any columns yet. So we have to handle that case.
	FilterMenuNodeVec * pvfmn = NULL;
	if (icol != -1)
	{
		FilterMenuNodeVec & vfmn = m_pfltf->GetColumnVector(icol);
		pvfmn = &vfmn;
	}

	if (!m_qflts)
	{
		// The subdialog has not been created, so create it now.
		const int kdxpCriteriaOffset = 7;
		const int kdypCriteriaOffset = 17;

		Rect rc;
		HWND hwndT = ::GetDlgItem(m_hwnd, kctidFilterCriteria);
		::GetWindowRect(hwndT, &rc);
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);

		m_qflts.Create();
		m_qflts->SetDialogValues(m_pfltdlg->GetLpInfo(), pvfmn, NULL);
		m_qflts->DoModeless(m_hwnd);
		::SetWindowPos(m_qflts->Hwnd(), ::GetDlgItem(m_hwnd, kctidFilterCriteria),
			rc.left + kdxpCriteriaOffset, rc.top + kdypCriteriaOffset, 0, 0,
			SWP_NOSIZE | SWP_NOACTIVATE);
	}
	else
	{
		m_qflts->RefreshFilterDisplay(pvfmn, NULL);
	}

	m_qflts->SetActive();
	::ShowWindow(m_qflts->Hwnd(), SW_SHOW);

	OnEditSelChange(icol, irow, ichAnchor, ichEnd, true);
}


/*----------------------------------------------------------------------------------------------
	The user has changed the caret position in the full filter view window. This means we
	need to update what the simple filter window is showing.
	We also need to determine whether or not the Insert button should be enabled.

	@param icol Column index of the new cell in the current filter.
	@param irow Row index of the new cell in the current filter.
	@param ichAnchor End point of the selection in the new cell string of the current filter.
	@param ichEnd The other end point of the selection in the cell string.
	@param fForceRefresh Flag whether to force refresh even if the other input values seem not
					to have changed.
----------------------------------------------------------------------------------------------*/
void FwFilterBuilderShellDlg::OnEditSelChange(int icol, int irow, int ichAnchor, int ichEnd,
	bool fForceRefresh)
{

	ISilDataAccessPtr qsda_vcdTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);

	if (m_icol != icol || fForceRefresh)
	{
		// We are in a new/modified column, so make sure the simple filter dialog is showing
		// the right choices/controls for this column.
		AssertPtr(m_qflts);
		FilterMenuNodeVec * pvfmn = NULL;
		if (icol != -1)
		{
			FilterMenuNodeVec & vfmn = m_pfltf->GetColumnVector(icol);
			pvfmn = &vfmn;
		}
		m_qflts->RefreshFilterDisplay(pvfmn, NULL);
	}

	m_icol = icol;
	m_irow = irow;
	m_ichAnchor = ichAnchor;
	m_ichEnd = ichEnd;
	m_st = kstNone;

	if (m_icol != -1)
	{
		m_fValidCriterion = false;
		m_fValidInsertionPt = false;
		// Get to the cell text.
		HVO hvoRow;
		HVO hvoCell;
		ITsStringPtr qtss;
		CheckHr(qsda_vcdTemp->get_VecItem(m_hvoFilter, kflidCmFilter_Rows, irow, &hvoRow));
		CheckHr(qsda_vcdTemp->get_VecItem(hvoRow, kflidCmRow_Cells, icol, &hvoCell));
		CheckHr(qsda_vcdTemp->get_StringProp(hvoCell, kflidCmCell_Contents, &qtss));

		const OLECHAR * prgwch;
		int cch;
		CheckHr(qtss->LockText(&prgwch, &cch));
		Assert((uint)ichAnchor <= (uint)cch);

		// We can insert a new condition if we are at the beginning, the end, or right before
		// a keyword.
		// Look at the previous non-space character to see if we need a separator (and/or).
		// The only time we don't need a separator is when the string is empty, or we are right
		// after an open paranthesis, or the entire string is selected.
		int ich = Min(ichAnchor, ichEnd);
		for (int iv = 0; iv < 2; iv++)
		{
			bool fValidInsert = false;
			const wchar * prgwchPrev = const_cast<wchar *>(prgwch + ich - 1);
			while (prgwchPrev >= prgwch && iswspace(*prgwchPrev))
				prgwchPrev--;
			const wchar * prgwchNext = const_cast<wchar *>(prgwch + ich);
			FilterUtil::SkipWhiteSpace(prgwchNext);
			if (prgwchPrev < prgwch || *prgwchPrev == '(')
			{
				fValidInsert = true;
				if (iv == 0 && *prgwchNext != 0)
					m_st = kstAfter;
			}
			else if (*prgwchNext == 0 || *prgwchNext == ')')
			{
				// We are at the end of the string or right before a close paranthesis.
				fValidInsert = true;
				if (iv == 0)
					m_st = kstBefore;
			}
			else
			{
				// Look at the next word to see if we can insert a new condition here.
				KeywordLookup kl;
				switch (kl.GetTypeFromStr(prgwchNext))
				{
				case kfktEmpty:
				case kfktNotEmpty:
				case kfktContains:
				case kfktDoesNotContain:
				case kfktMatches:
				case kfktDoesNotMatch:
				case kfktEqual:
				case kfktNotEqual:
				case kfktGT:
				case kfktLT:
				case kfktGTE:
				case kfktLTE:
					fValidInsert = true;
					m_st = kstAfter;
					break;
				case kfktOr:
				case kfktAnd:
					fValidInsert = true;

					if (iv == 0)
					{
						m_st = kstBefore;

						// If we have two separators in a row, we don't need to insert another
						// one. We already have a separator after the current, so look at the
						// previous word to see if it matches a separator.
						const wchar * pszKey = kl.GetStrFromType(kfktOr);
						int cch = StrLen(pszKey);
						if (prgwchPrev >= prgwch + cch - 1)
						{
							if (_wcsnicmp(prgwchPrev - cch + 1, pszKey, cch) == 0)
								m_st = kstNone;
						}
						if (m_st == kstBefore)
						{
							pszKey = kl.GetStrFromType(kfktAnd);
							cch = StrLen(pszKey);
							if (prgwchPrev >= prgwch + cch - 1)
							{
								if (_wcsnicmp(prgwchPrev - cch + 1, pszKey, cch) == 0)
									m_st = kstNone;
							}
						}
					}
				}
			}
			if (iv == 0)
			{
				m_fValidInsertionPt = fValidInsert;
				if (ichAnchor == ichEnd)
					break;
				ich = Max(ichAnchor, ichEnd);
			}
			else if (!fValidInsert)
			{
				m_fValidInsertionPt = false;
			}
		}
		// No separator wanted if replacing the entire cell contents.
		if (Min(ichAnchor, ichEnd) == 0 && Max(ichAnchor, ichEnd) == cch)
			m_st = kstNone;

		qtss->UnlockText(prgwch);
	}
	achar rgch[MAX_PATH] = { 0 };
	if (::IsWindowEnabled(::GetDlgItem(m_qflts->Hwnd(), kctidFilterText)))
	{
		if (::GetDlgItemText(m_qflts->Hwnd(), kctidFilterText, rgch, MAX_PATH))
			m_fValidCriterion = true;
	}
	else if (::IsWindowEnabled(::GetDlgItem(m_qflts->Hwnd(), kctidFilterDate)))
	{
		int cch = ::GetDlgItemText(m_qflts->Hwnd(), kctidFilterDate, rgch, MAX_PATH);
		if (cch)
		{
			SilTime stim;
			int cchUsed;
			m_fValidCriterion = StrUtil::ParseDateTime(rgch, cch, &stim, &cchUsed);
		}
	}
	else if (::IsWindowEnabled(::GetDlgItem(m_qflts->Hwnd(), kctidFilterEnum)))
	{
		if (::GetDlgItemText(m_qflts->Hwnd(), kctidFilterEnum, rgch, MAX_PATH))
			m_fValidCriterion = true;
	}
	else if (::IsWindowEnabled(::GetDlgItem(m_qflts->Hwnd(), kctidFilterRef)))
	{
		if (::GetDlgItemText(m_qflts->Hwnd(), kctidFilterRef, rgch, MAX_PATH))
			m_fValidCriterion = true;
	}
	else if (::IsWindowVisible(::GetDlgItem(m_qflts->Hwnd(), kctidCondition)))
	{
		m_fValidCriterion = true;
	}
	::EnableWindow(::GetDlgItem(m_hwnd, kctidInsert), m_fValidCriterion && m_fValidInsertionPt);
	BOOL fEnableAndOr = m_fValidCriterion && m_fValidInsertionPt && m_st != kstNone;
	::EnableWindow(::GetDlgItem(m_hwnd, kctidAnd), fEnableAndOr);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidOr), fEnableAndOr);
}


/*----------------------------------------------------------------------------------------------
	Enable or disable the Insert button, and possibly the And/Or radio buttons.

	@param fValid Flag whether the criterion is valid for inserting into the filter cell.
----------------------------------------------------------------------------------------------*/
void FwFilterBuilderShellDlg::EnableInsertBtn(bool fValid)
{
	if (m_fValidCriterion == fValid)
		return;
	m_fValidCriterion = fValid;
	::EnableWindow(::GetDlgItem(m_hwnd, kctidInsert), m_fValidCriterion && m_fValidInsertionPt);
	BOOL fEnableAndOr = m_fValidCriterion && m_fValidInsertionPt && m_st != kstNone;
	::EnableWindow(::GetDlgItem(m_hwnd, kctidAnd), fEnableAndOr);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidOr), fEnableAndOr);
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterBuilderShellDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	::CheckDlgButton(m_hwnd, kctidAnd, BST_CHECKED);

	RefreshFilterDisplay(m_hvoFilter, m_icol, m_irow, m_ichAnchor, m_ichEnd);

	// In case we don't have a position saved, put this in a reasonable location.
	Rect rcProg;
	Rect rcParent;
	Rect rcScreen;
	Rect rc;
	HWND hwndPar = ::GetParent(m_hwnd);
	::SystemParametersInfo(SPI_GETWORKAREA, 0, &rcScreen, 0);
	if (::GetWindowRect(hwndPar, &rcParent) &&
		::GetWindowRect(::GetParent(hwndPar), &rcProg) &&
		::GetWindowRect(m_hwnd, &rc))
	{
		int dy = rcParent.bottom - rcParent.top;
		int dx = rcParent.right - rcParent.left;
		rcParent.top = (rcProg.top + rcProg.bottom - dy) / 2;
		rcParent.bottom = rcParent.top + dy;
		rcParent.left = (rcProg.left + rcProg.right - dx) / 2;
		rcParent.right = rcParent.left + dx;
		dy = rc.bottom - rc.top;
		dx = rc.right - rc.left;
		rc.bottom = rcParent.top;
		rc.top = rc.bottom - dy;
		rc.left = (rcParent.left + rcParent.right - dx) / 2;
		rc.right = rc.left + dx;
		AfGfx::EnsureVisibleRect(rc);
		WINDOWPLACEMENT wp = { isizeof(wp) };
		wp.rcNormalPosition = rc;
		::SetWindowPlacement(m_hwnd, &wp);
	}
	// Now, try to retrieve a stored position.
	LoadWindowPosition(kpszFilterSubKey, _T("CB Position"));

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool FwFilterBuilderShellDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidInsert)
		{
			Assert(m_fValidCriterion);
			AssertPtr(m_qflts);

			// Get the cell text to be inserted from the subdialog.
			m_qflts->Apply();
			ITsStringPtr qtssCell;
			m_qflts->GetDialogValues(&qtssCell);
			int cch = 0;
			if (qtssCell)
				CheckHr(qtssCell->get_Length(&cch));
			if (cch > 0)
			{
				// Add and/or if required with correct spacing at the proper position in
				// the text to be inserted into the cell.
				ITsStringPtr qtssInsert;
				if (m_st == kstNone)
				{
					qtssInsert = qtssCell;
				}
				else
				{
					Assert(m_st == kstBefore || m_st == kstAfter);
					ITsStrBldrPtr qtsb;
					qtsb.CreateInstance(CLSID_TsStrBldr);
					CheckHr(qtsb->ReplaceTsString(0, 0, qtssCell));
					wchar * pszLogOp;
					bool fAnd = ::IsDlgButtonChecked(m_hwnd, kctidAnd);
					if (m_st == kstBefore)
					{
						pszLogOp = fAnd ? L"and " : L"or ";
						CheckHr(qtsb->ReplaceRgch(0, 0, pszLogOp, StrLen(pszLogOp), NULL));
					}
					else
					{
						pszLogOp = fAnd ? L" and" : L" or";
						CheckHr(qtsb->get_Length(&cch));
						CheckHr(qtsb->ReplaceRgch(cch, cch, pszLogOp, StrLen(pszLogOp), NULL));
					}
					CheckHr(qtsb->GetString(&qtssInsert));
				}
				m_pfltf->InsertIntoCell(qtssInsert);
			}
		}
		return true;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Close the dialog.  Also let the parent window know that we are closing.

	@return True or false: whatever the superclass's OnCancel method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterBuilderShellDlg::OnCancel()
{
//	SaveWindowPosition(kpszFilterSubKey, "CB Position");
	m_pfltf->ShowBuilder(false);
	return SuperClass::OnCancel();
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only the WM_SHOWWINDOW, WM_ACTIVATE, and private s_wmBldrShellActivate messages are
	processed, and even then the message is passed on to the superclass's FWndProc method.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True or false: whatever the superclass's FWndProc method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterBuilderShellDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_SHOWWINDOW)
	{
		if (!m_fValidInsertionPt)
		{
			// Move to a known good insertion point.
			IVwRootBox * prootb = m_pfltf->GetFilterWnd()->GetRootBox();
			AssertPtr(prootb);
			prootb->OnExtendedKey(VK_END, kfssControl, 0);
			m_pfltf->GetFilterWnd()->ForwardEditSelChange();
		}
	}
	else if (wm == WM_ACTIVATE)
	{
		// If we are being activated, add a message at the end of the queue to activate the
		// view window. It is posted to the queue because the view window deactivates itself
		// when it receives the WM_KILLFOCUS message, so we want to (re)activate it after that
		// message has been received.
		// If we are being deactivated, we want to deactivate the view window immediately. If
		// we post the message, it could be received after the view window receives the
		// WM_SETFOCUS message (which activates it), which would be bad because this would
		// deactivate the view window again.
		bool fActivate = LOWORD(wp) != WA_INACTIVE;
		if (fActivate)
		{
			::PostMessage(m_hwnd, s_wmBldrShellActivate, 0, true);
			m_pfltf->GetFilterWnd()->ForwardEditSelChange();
			if (!m_fValidInsertionPt)
			{
				// Move to a known good insertion point.
				IVwRootBox * prootb = m_pfltf->GetFilterWnd()->GetRootBox();
				AssertPtr(prootb);
				prootb->OnExtendedKey(VK_END, kfssControl, 0);
				m_pfltf->GetFilterWnd()->ForwardEditSelChange();
			}
		}
		else
		{
			wm = s_wmBldrShellActivate;
			lp = 0;
			// Fall through to the s_wmBldrShellActivate handler below.
		}
	}
	if (wm == s_wmBldrShellActivate)
	{
		// NOTE: This block must come after the WM_ACTIVATE block.
		AssertPtr(m_pfltf);
		FilterWnd * pfwnd = m_pfltf->GetFilterWnd();
		AssertPtr(pfwnd);
		pfwnd->Activate(lp ? vssEnabled : vssDisabled);
	}
	else if (wm == WM_DESTROY)
	{
		SaveWindowPosition(kpszFilterSubKey, _T("CB Position"));
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Refresh the possibility hot link string in the filter table to reflect the new
	choice for how to display possibilities in this list.

	@param pnt Specifies how the possibility is to be displayed.
----------------------------------------------------------------------------------------------*/
void FwFilterBuilderShellDlg::RefreshPossibilityColumn(PossNameType pnt)
{
	if (m_pfltf)
		m_pfltf->RefreshPossibilityColumn(pnt);
}


//:>********************************************************************************************
//:>	FwFilterSimpleDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterSimpleDlg::FwFilterSimpleDlg()
{
	m_rid = kridFilterSimpleDlg;
	m_fft = (FilterFieldType)(kfftNone - 1);
	m_fIgnoreCurSel = true;
	m_fAllowCriteriaPrompt = true;
	m_ws = 0;
	m_fWideCondition = false;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwFilterSimpleDlg::~FwFilterSimpleDlg()
{
}


/*----------------------------------------------------------------------------------------------
	Store initial dialog values.

	@param plpi Pointer to the application's language project information.
	@param pvfmn Pointer to a vector of filter menu nodes that define what the filter operates
					on.
	@param ptss Pointer to an ITsString COM object that stores the filter's operator and value.
----------------------------------------------------------------------------------------------*/
void FwFilterSimpleDlg::SetDialogValues(AfLpInfo * plpi, FilterMenuNodeVec * pvfmn,
	ITsString * ptss)
{
	AssertPtr(plpi);
	AssertPtrN(pvfmn);
	AssertPtrN(ptss);

	m_qlpi = plpi;
	if (pvfmn)
		m_vfmn = *pvfmn;
	m_qtss = ptss;
}


/*----------------------------------------------------------------------------------------------
	Retrieve dialog values after the user has closed the dialog.

	@param pptss Address of an ITsString COM pointer, used to store return value.
----------------------------------------------------------------------------------------------*/
void FwFilterSimpleDlg::GetDialogValues(ITsString ** pptss)
{
	AssertPtr(pptss);
	*pptss = m_qtss;
	AddRefObj(*pptss);
}


/*----------------------------------------------------------------------------------------------
	The user has selected a different filter.

	@param pvfmn Pointer to the column of filter menu nodes.
	@param ptss Pointer to an ITsString COM object that stores the filter text.
----------------------------------------------------------------------------------------------*/
void FwFilterSimpleDlg::RefreshFilterDisplay(FilterMenuNodeVec * pvfmn, ITsString * ptss)
{
	AssertPtrN(pvfmn);
	AssertPtrN(ptss);

	int fptOld = kcptNil;
	if (m_vfmn.Size())
		fptOld = m_vfmn[m_vfmn.Size() - 1]->m_proptype;

	// Make sure it's done in this order, because pvfmn could be pointing to m_vfmn.
	if (pvfmn)
		m_vfmn = *pvfmn;
	else
		m_vfmn.Clear();
	m_qtss = ptss;

	// Clear out existing values.
	m_qte->SetText(NULL);
	m_qte->SelectAll(); // This is a hack to make sure the caret appears.
	::SetWindowText(::GetDlgItem(m_hwnd, kctidFilterDate), _T(""));
	::SetWindowText(::GetDlgItem(m_hwnd, kctidFilterNumber), _T(""));
	if (m_vfmn.Size())
	{
		FilterMenuNodePtr qfmn = *m_vfmn.Top();
		if (qfmn->m_proptype == kfptPossList)
		{
			m_qfpe->SetPssl(qfmn->m_hvo);
		}
		else if (qfmn->m_proptype == kfptTagList)
		{
			AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(qfmn->m_flid);
			Assert(qfmn->m_hvo == aoi.m_hvoPssl);
			if (!aoi.m_qvo)
			{
				IVwOverlayPtr qvo;
				m_qlpi->GetOverlay(qfmn->m_flid, &qvo);
				aoi.m_qvo = qvo;
			}
			m_qfpe->SetPssl(qfmn->m_hvo, NULL, &aoi);
		}
	}

	m_fIgnoreCurSel = true;
	ShowType(fptOld);
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param hwndCtrl Handle passed on to the superclass method.
	@param lp Long parameter passed on the superclass method.

	@return True or false: whatever the superclass's OnInitDlg method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterSimpleDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidFilterSpecial, kbtPopMenu, NULL, 0);
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidFilterFormat, kbtPopMenu, NULL, 0);

	FwFilterLaunchBtnPtr qflb;
	qflb.Create();
	qflb->SubclassButton(::GetDlgItem(m_hwnd, kctidFilterChooseItem));
	qflb.Create();
	qflb->SubclassButton(::GetDlgItem(m_hwnd, kctidFilterChooseDate));

	m_qfpe.Create();
	m_qfpe->SubclassEdit(::GetDlgItem(m_hwnd, kctidFilterRef), m_qlpi);

	ILgWritingSystemFactoryPtr qwsf;
	m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	m_qte.Create();
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	m_qte->SubclassEdit(m_hwnd, kctidFilterText, qwsf, wsUser, WS_EX_CLIENTEDGE);

	HWND hwndCombo = ::GetDlgItem(m_hwnd, kctidFilterScope);
	FilterUtil::FillDateScopeCombo(hwndCombo, true);
	::SendMessage(hwndCombo, CB_SETCURSEL, 1, 0); // Month and Year.

	RefreshFilterDisplay(&m_vfmn, m_qtss);

	if (::SendMessage(::GetDlgItem(m_hwnd, kctidCondition), CB_GETCURSEL, 0,0) > 3)
		FilterUtil::FillDateScopeCombo(hwndCombo, false);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Check whether the string contains a valid number (and only the number).  Versions for both
	ANSI and Unicode characters are provided.

	@return True if successful, false if the string contains non-numeric data.
----------------------------------------------------------------------------------------------*/
static bool ValidateNumber(const char * pszString, int cch, StrApp & strNumber)
{
	if (!cch)
		return false;
	char * psz;
	errno = 0;
	long n = strtol(pszString, &psz, 10);
	Assert(sizeof(int) == sizeof(long));
	if (errno == ERANGE)
		strNumber.Format(_T("%d"), n);
	else
		strNumber.Clear();
	return (psz - pszString) == cch;
}
static bool ValidateNumber(const wchar * pszString, int cch, StrApp & strNumber)
{
	if (!cch)
		return false;
	wchar * psz;
	errno = 0;
	long n = wcstol(pszString, &psz, 10);
	Assert(sizeof(int) == sizeof(long));
	if (errno == ERANGE)
		strNumber.Format(_T("%d"), n);
	else
		strNumber.Clear();
	return (psz - pszString) == cch;
}

/*----------------------------------------------------------------------------------------------
	Create the cell text based on the condition and text/item the user has selected.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool FwFilterSimpleDlg::Apply()
{
	m_qtss.Clear();

	achar rgch[MAX_PATH] = { 0 };
	int cch = 0;
	ITsStringPtr qtssText;
	HVO hvo;
	StrApp strNumber;

	FilterKeywordType fkt = FilterUtil::GetKeywordType(m_fft, m_hwnd);

	// Get the text from the correct control.
	switch (m_fft)
	{
	case kfftText:
		m_qte->GetText(&qtssText);
		break;
	case kfftRef:
		hvo = m_qfpe->GetPss();
		break;
	case kfftRefText:
		m_qte->GetText(&qtssText);
		break;
	case kfftDate:
		cch = ::GetDlgItemText(m_hwnd, kctidFilterDate, rgch, MAX_PATH);
		break;
	case kfftEnum:
		// Do nothing.
		break;
	case kfftEnumReq:
		// Do nothing.
		break;
	case kfftBoolean:
		// Do nothing.
		break;
	case kfftCrossRef:
		m_qte->GetText(&qtssText);
		break;
	case kfftNumber:
		cch = ::GetDlgItemText(m_hwnd, kctidFilterNumber, rgch, MAX_PATH);
		// Verify that it's a valid number.
		if (!ValidateNumber(rgch, cch, strNumber))
		{
			StrApp strTitle(kstidFltrNumberCap);
			StrApp strMsg(kstidFltrNumberError);
			::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_TASKMODAL);
			return false;
		}
		if (strNumber.Length())
		{
			StrApp strTitle(kstidFltrNumberCap);
			StrApp strMsg(kstidFltrMaxIntMsg);
			::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_TASKMODAL);
			::SetDlgItemText(m_hwnd, kctidFilterNumber, strNumber.Chars());
			return false;
		}
		break;
	}

	KeywordLookup kl;
	const wchar * pszKeyword = kl.GetStrFromType(fkt);
	if (!pszKeyword)
		return true;

	StrUni stuCell;

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	if (m_fft == kfftText)
	{
		Assert(m_vfmn.Size());
		const FilterMenuNode * pfmn = m_vfmn[m_vfmn.Size() - 1];
		Assert(pfmn->m_fmnt == kfmntLeaf);
		if (pfmn->m_proptype != kcptMultiString &&
			pfmn->m_proptype != kcptMultiUnicode &&
			pfmn->m_proptype != kcptMultiBigString &&
			pfmn->m_proptype != kcptMultiBigUnicode)
		{
			m_ws = 0;
		}
	}
	else
	{
		m_ws = 0;
	}

	SmartBstr sbstrWs;
	if (m_ws)
	{
		ILgWritingSystemFactoryPtr qwsf;
		m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
		AssertPtr(qwsf);
		CheckHr(qwsf->GetStrFromWs(m_ws, &sbstrWs));
	}

	int wsUser = m_qlpi->GetDbInfo()->UserWs();

	// Update m_qtss.
	if (fkt == kfktEmpty || fkt == kfktNotEmpty)
	{
		// All we need is the keyword for these cases, unless there is an writing system to worry
		// about.
		if (m_ws)
		{
			stuCell.Format(L"%s [%s]", pszKeyword, sbstrWs.Chars());
			qtsf->MakeStringRgch(stuCell.Chars(), stuCell.Length(), wsUser, &m_qtss);
		}
		else
		{
			qtsf->MakeStringRgch(pszKeyword, StrLen(pszKeyword), wsUser, &m_qtss);
		}
	}
	else
	{
		switch (m_fft)
		{
		case kfftText:
		case kfftRefText:
		case kfftCrossRef:
			{
				// The string's format is: <keyword> "<text>" or <keyword> [ENC]"<text>"
				// If any double quotes (or backslashes) are found in the cell text, insert \ in
				// front of them if there isn't one already.
				if (m_ws)
					stuCell.Format(L"%s [%s]\"", pszKeyword, sbstrWs.Chars());
				else
					stuCell.Format(L"%s \"", pszKeyword);
				Assert(qtssText);
				ITsIncStrBldrPtr qtisb;
				qtsf->GetIncBldr(&qtisb);
				qtisb->SetIntPropValues(ktptWs, ktpvDefault, wsUser);
				qtisb->Append(stuCell.Bstr());
				Vector<int> vichFix;
				SmartBstr sbstr;
				const OLECHAR * prgchText;
				int cchText;
				int ich;
				CheckHr(qtssText->LockText(&prgchText, &cchText));
				for (ich = 0; ich < cchText; ++ich)
				{
					if (prgchText[ich] == '"' || prgchText[ich] == '\\')
					{
						vichFix.Push(ich);
					}
				}
				CheckHr(qtssText->UnlockText(prgchText));
				if (vichFix.Size())
				{
					ITsTextPropsPtr qttp;
					ITsStrBldrPtr qtsb;
					qtssText->GetBldr(&qtsb);
					while (vichFix.Size())
					{
						vichFix.Pop(&ich);
						qtsb->get_PropertiesAt(ich, &qttp);
						qtsb->ReplaceRgch(ich, ich, L"\\", 1, qttp);
					}
					qtsb->GetString(&qtssText);
				}
				qtisb->AppendTsString(qtssText);
				qtisb->SetIntPropValues(ktptWs, ktpvDefault, wsUser);
				qtisb->AppendRgch(L"\"", 1);
				qtisb->GetString(&m_qtss);
			}
			break;
		case kfftRef:
			{
				// The string's format is: <keyword> <object>
				// The object character has the ktptObjData string property set on it, which
				// gives the guid of the possibility.
				if (!hvo)
				{
					// Message about empty list?
					StrApp strTitle(kstidFltrCannotCreate);
					StrApp str(kstidFltrCannotEmptyList);
					FwFilterErrorMsgDlgPtr qfmsg;
					qfmsg.Create();
					StrApp strHelp;
					qfmsg->Initialize(strTitle.Chars(), str.Chars(), strHelp.Chars());
					qfmsg->DoModal(m_hwnd);
					return false;
				}
				stuCell.Format(L"%s %c", pszKeyword, kchObject);
				int ichObj = stuCell.Length() - 1;
				// Get the guid of the possibility from the HVO.
				GUID uid;
				if (!m_qlpi->GetDbInfo()->GetGuidFromId(hvo, uid))
					return false;

				StrUni stuData;
				OLECHAR * prgchData;
				// Make large enough for a guid plus the type character at the start.
				stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
				*prgchData = kodtNameGuidHot;
				memmove(prgchData + 1, &uid, isizeof(uid));
				if (::SendMessage(::GetDlgItem(m_hwnd, kctidFilterSubitems), BM_GETCHECK, 0, 0)
					== BST_CHECKED)
				{
					stuCell.Append(L" +subitems");
				}
				ITsStrBldrPtr qtsb;
				CheckHr(qtsf->GetBldr(&qtsb));
				CheckHr(qtsb->Replace(0, 0, stuCell.Bstr(), NULL));
				CheckHr(qtsb->SetIntPropValues(0, stuCell.Length(), ktptWs, 0, wsUser));
				CheckHr(qtsb->SetStrPropValue(ichObj, ichObj + 1, ktptObjData, stuData.Bstr()));
				CheckHr(qtsb->GetString(&m_qtss));
			}
			break;
		case kfftDate:
			{
				// The string's format is: <keyword> <scope>(<text>)
				int isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidFilterScope), CB_GETCURSEL,
					0, 0);
				StrUni stuDate(rgch);
				if (isel < kcselDateNeeded)
				{
					// Need a valid date string.
					int nYear;
					int nMonth;
					int nDay;
					if (!ParseDate(stuDate, isel, nYear, nMonth, nDay, false, m_hwnd))
						return false;
				}
				DateKeywordLookup dkl;
				const wchar * pszScope = dkl.GetStrFromIndex(isel);
				AssertPsz(pszScope);
				stuCell.Format(L"%s %s", pszKeyword, pszScope);
				if (isel < kcselDateNeeded)
					stuCell.FormatAppend(L"(%s)", stuDate.Chars());
				qtsf->MakeStringRgch(stuCell.Chars(), stuCell.Length(), wsUser, &m_qtss);
			}
			break;
		case kfftEnum:
		case kfftEnumReq:
			{
				// The string's format is: <keyword> <enum text>
				int isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidFilterEnum), CB_GETCURSEL,
					0, 0);
				Assert(m_vfmn.Size());
				FilterMenuNode * pfmn = m_vfmn[m_vfmn.Size() - 1];
#ifdef DEBUG
				if (m_fft == kfftEnum)
					Assert(pfmn->m_proptype == kfptEnumList);
				else
					Assert(pfmn->m_proptype == kfptEnumListReq);
#endif
				Assert(pfmn->m_stid);
				StrUni stuEnumTotal(pfmn->m_stid);
				const wchar * pszEnum = stuEnumTotal.Chars();
				while (isel-- > 0 && pszEnum != (wchar *)1)
					pszEnum = wcschr(pszEnum, '\n') + 1;
				if (isel == -1)
				{
					AssertPsz(pszEnum);
					const wchar * pszEnumLim = wcschr(pszEnum, '\n');
					if (!pszEnumLim)
						pszEnumLim = stuEnumTotal.Chars() + stuEnumTotal.Length();
					StrUni stuEnum(pszEnum, pszEnumLim - pszEnum);
					stuCell.Format(L"%s %s", pszKeyword, stuEnum.Chars());
					qtsf->MakeStringRgch(stuCell.Chars(), stuCell.Length(), wsUser, &m_qtss);
				}
			}
			break;
		case kfftBoolean:
			// The string's format is: "<keyword>"
			stuCell.Format(L"%s", pszKeyword);
			qtsf->MakeStringRgch(stuCell.Chars(), stuCell.Length(), wsUser, &m_qtss);
			break;
		case kfftNumber:
			// The string's format is: "<keyword> <number>"
			{
				StrUni stuNumber(rgch);
				stuCell.Format(L"%s %s", pszKeyword, stuNumber.Chars());
				qtsf->MakeStringRgch(stuCell.Chars(), stuCell.Length(), wsUser, &m_qtss);
			}
			break;
		}
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Format the date stored in psystime according to the scope defined by isel.

	@param isel Index of the current selection in the Scope combobox.
	@param psystime Pointer to a date/time data structure.
	@param rgchDate Pointer to the output buffer.
	@param cchDateMax Maximum number of characters that can be stored in rgchDate.

	@return The number of characters consumed in the formatted output.
----------------------------------------------------------------------------------------------*/
static int FormatDate(int isel, CONST SYSTEMTIME * psystime, achar * rgchDate, int cchDateMax)
{
	StrApp strFmt;
	switch (isel)
	{
	case kiselExactDate:
		return ::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, psystime, NULL, rgchDate,
			cchDateMax);
		break;
	case kiselMonthYear:
		return ::GetDateFormat(LOCALE_USER_DEFAULT, DATE_YEARMONTH, psystime, NULL, rgchDate,
			cchDateMax);
		break;
	case kiselYear:
		strFmt.Assign("yyyy");
		return ::GetDateFormat(LOCALE_USER_DEFAULT, 0, psystime, strFmt.Chars(), rgchDate,
			cchDateMax);
		break;
#ifdef VERSION2FILTER
	case kiselMonth:
		strFmt.Assign("MMMM");
		return ::GetDateFormat(LOCALE_USER_DEFAULT, 0, psystime, strFmt.Chars(), rgchDate,
			cchDateMax);
		break;
	case kiselDay:
		strFmt.Assign("d");
		return ::GetDateFormat(LOCALE_USER_DEFAULT, 0, psystime, strFmt.Chars(), rgchDate,
			cchDateMax);
		break;
#endif /*VERSION2FILTER*/
	default:
		return 0;
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Parse the date string, extracting the year, month, and day.

	@param stuDate Date value.
	@param iselScope Index selecting the desired date scope.
	@param nYear Reference to an integer for returning the year.
	@param nMonth Reference to an integer for returning the month of the year (1-12).
	@param nDay Reference to an integer for returning the day of the month (1-31).
	@para, fSkipErrorMsg if true it will not show the error message if there is a invalid date

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
static bool ParseDate(StrUni & stuDate, int iselScope, int & nYear, int & nMonth, int & nDay,
	bool fSkipErrorMsg, HWND hwndParent)
{
	SilTime stim;
	int cch;
	if (StrUtil::ParseDateTime(stuDate.Chars(), stuDate.Length(), &stim, &cch))
	{
		// Convert SilTime to SYSTEMTIME (only the date fields).
		nYear = stim.Year();
		nMonth = stim.Month();
		nDay = stim.Date();
		return cch == stuDate.Length();
	}
	else
	{
		// Default to the current date for unparsed values.
		SYSTEMTIME systime;
		::GetLocalTime(&systime);
		nYear = systime.wYear;
		nMonth = systime.wMonth;
		nDay = systime.wDay;
#ifdef VERSION2FILTER
		bool fError = false;
#endif /*VERSION2FILTER*/
		char * pch;
		switch (iselScope)
		{
		case kiselYear:
			{
				StrAnsiBuf stab(stuDate.Chars());
				nYear = static_cast<int>(strtoul(stab.Chars(), &pch, 10));
				if (pch == stab.Chars())
					goto LBadDate;
				if (nYear < 100)
				{
					nYear += 2000;
					if (nYear > systime.wYear)
						nYear -= 100;
				}
			}
			break;
		case kiselMonthYear:
			{
				StrAnsiBuf stab(stuDate.Chars());
				nYear = static_cast<int>(strtoul(stab.Chars(), &pch, 10));
				if (pch == stab.Chars())
				{
					nYear = systime.wYear;
					goto LBadDate;
				}
				if (nYear < 100)
				{
					nYear += 2000;
					if (nYear > systime.wYear)
						nYear -= 100;
				}
				nMonth = 1;
				nDay = 1;
				goto LBadDate;
			}
			break;
#ifdef VERSION2FILTER
		case kiselMonth:
			{
				StrAnsiBuf stab(stuDate.Chars());
				nMonth = static_cast<int>(strtoul(stab.Chars(), &pch, 10));
				if (nMonth < 1)
				{
					for (int i = 1; i <= 12; ++i)
					{
						StrUni * pstu = StrUtil::GetMonthStr(i, true);
						if (!_wcsicmp(stuDate.Chars(), pstu->Chars()))
						{
							nMonth = i;
							break;
						}
						pstu = StrUtil::GetMonthStr(i, false);
						if (!_wcsicmp(stuDate.Chars(), pstu->Chars()))
						{
							nMonth = i;
							break;
						}
					}
					if (nMonth < 1)
					{
						fError = true;
						nMonth = 1;
					}
				}
				else if (nMonth > 12)
				{
					fError = true;
					nMonth = 12;
				}
			}
			if (fError)
				goto LBadDate;
			break;
		case kiselDay:
			{
				StrAnsiBuf stab(stuDate.Chars());
				nDay = static_cast<int>(strtoul(stab.Chars(), &pch, 10));
				if (nDay < 1)
				{
					fError = true;
					nDay = 1;
				}
				else if (nDay > 31)
				{
					fError = true;
					nDay = 31;
				}
				if (fError)
					goto LBadDate;
			}
			break;
#endif /*VERSION2FILTER*/
		default:
			// Tell the user that it's an invalid date string.
			{
LBadDate:
				if (!fSkipErrorMsg)
				{
					StrApp strHelpUrl(AfApp::Papp()->GetHelpFile());
					strHelpUrl.Append(_T("::/"));
					strHelpUrl.Append(_T("DialogFilterError.htm"));
					AfMainWndPtr qafwTop = AfApp::Papp()->GetCurMainWnd();
					qafwTop->SetFullHelpUrl(strHelpUrl.Chars());

					StrAppBuf strDate(stuDate.Chars());
					StrAppBuf strFmt(kstidFltrDateProb);
					StrApp strTitle(kstidFltrDateProbCap);

					StrApp strScope("");
					switch (iselScope)
					{
					case kiselExactDate:
						strScope.Load(kstidFltrExactDate);
						break;
					case kiselMonthYear:
						strScope.Load(kstidFltrMonthYear);
						break;
					case kiselYear:
						strScope.Load(kstidFltrYear);
						break;
#ifdef VERSION2FILTER
					case kiselMonth:
						strScope.Load(kstidFltrMonth);
						break;
					case kiselDay:
						strScope.Load(kstidFltrDay);
						break;
#endif /*VERSION2FILTER*/
					}

					// No Help button needed (see DN-591), so a simple message box will do.
					StrApp str;
					str.Format(strFmt.Chars(), strScope.Chars(), strDate.Chars());
					::MessageBox(hwndParent, str.Chars(), strTitle.Chars(),
						MB_OK | MB_ICONINFORMATION | MB_TASKMODAL);

					// Clear the stored Url to prevent inadvertent re-use.
					qafwTop->ClearFullHelpUrl();
					if (hwndParent)
						::SetFocus(hwndParent);
				}
			}
			return false;
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Check whether the given date scope and value is valid.

	@return True if the combination of scope and date is valid, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwFilterSimpleDlg::IsValidDate()
{
	achar rgchDate[MAX_PATH];
	int cch = ::GetDlgItemText(m_hwnd, kctidFilterDate, rgchDate, MAX_PATH);
	rgchDate[cch] = 0;		// Paranoia - is this needed?
	int isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidFilterScope), CB_GETCURSEL, 0, 0);
	if (isel < kcselDateNeeded && !cch)
		return false;
	int cchUsed;
	SilTime stim;
	StrAppBufSmall strbsFmt;

	switch (isel)
	{
	case kiselExactDate:
	case kiselMonthYear:
		return StrUtil::ParseDateTime(rgchDate, cch, &stim, &cchUsed);


	case kiselYear:
		strbsFmt.Assign("yyyy");
		cchUsed = StrUtil::ParseDateWithFormat(rgchDate, strbsFmt.Chars(), &stim);
		if (cchUsed == cch)
			return true;
		strbsFmt.Assign("yyyy gg");
		cchUsed = StrUtil::ParseDateWithFormat(rgchDate, strbsFmt.Chars(), &stim);
		if (cchUsed == cch)
			return true;
		strbsFmt.Assign("gg yyyy");
		cchUsed = StrUtil::ParseDateWithFormat(rgchDate, strbsFmt.Chars(), &stim);
		if (cchUsed == cch)
			return true;
		strbsFmt.Assign("yy");
		cchUsed = StrUtil::ParseDateWithFormat(rgchDate, strbsFmt.Chars(), &stim);
		return cchUsed == cch;

#ifdef VERSION2FILTER
	case kiselMonth:
		strbsFmt.Assign("MMMM");
		cchUsed = StrUtil::ParseDateWithFormat(rgchDate, strbsFmt.Chars(), &stim);
		if (cchUsed == cch)
			return true;
		strbsFmt.Assign("MMM");
		cchUsed = StrUtil::ParseDateWithFormat(rgchDate, strbsFmt.Chars(), &stim);
		if (cchUsed == cch)
			return true;
		strbsFmt.Assign("MM");
		cchUsed = StrUtil::ParseDateWithFormat(rgchDate, strbsFmt.Chars(), &stim);
		if (cchUsed == cch)
			return true;
		strbsFmt.Assign("M");
		cchUsed = StrUtil::ParseDateWithFormat(rgchDate, strbsFmt.Chars(), &stim);
		return cchUsed == cch;
	case kiselDay:
		strbsFmt.Assign("dd");
		cchUsed = StrUtil::ParseDateWithFormat(rgchDate, strbsFmt.Chars(), &stim);
		if (cchUsed == cch)
			return true;
		cchUsed = StrUtil::ParseDateWithFormat(rgchDate, strbsFmt.Chars(), &stim);
		strbsFmt.Assign("d");
		return cchUsed == cch;
#endif /*VERSION2FILTER*/

	default:
		Assert(isel >= kcselDateNeeded);
		return true;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool FwFilterSimpleDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidFilterSpecial:
			{
				// Show the special popup menu.
				Rect rc;
				::GetWindowRect(::GetDlgItem(m_hwnd, ctidFrom), &rc);
				HMENU hmenuPopup = ::LoadMenu(ModuleEntry::GetModuleHandle(),
					MAKEINTRESOURCE(kridFltrPopups));
				g_fu.SetTssEdit(m_qte);
				::TrackPopupMenu(::GetSubMenu(hmenuPopup, kfpmSpecial),
					TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left, rc.bottom, 0, m_hwnd, NULL);
				::DestroyMenu(hmenuPopup);
			}
			break;

		case kctidFilterFormat:
			{
				// Show the special popup menu.
				Rect rc;
				::GetWindowRect(::GetDlgItem(m_hwnd, ctidFrom), &rc);
				HMENU hmenuPopup = ::LoadMenu(ModuleEntry::GetModuleHandle(),
					MAKEINTRESOURCE(kridFltrPopups));
				HMENU hmenu = ::GetSubMenu(hmenuPopup, kfpmFormat);

				// Remove the next two lines when those menu entries are implemented.
				::DeleteMenu(hmenu, kcidFltrFmtFont, MF_BYCOMMAND);
				::DeleteMenu(hmenu, kcidFltrFmtStyle, MF_BYCOMMAND);

				g_fu.SetEncAddr(&m_ws);
				g_fu.SetTssEdit(m_qte);
				::TrackPopupMenu(hmenu,
					TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left, rc.bottom, 0, m_hwnd, NULL);
				::DestroyMenu(hmenuPopup);
			}
			break;

		case kctidFilterChooseItem:
			{
				AssertPtr(m_qfpe);

				// Get the HVO of the possibility list to show.
				Assert(m_vfmn.Size() > 0);
				FilterMenuNodePtr qfmn = *m_vfmn.Top();
				Assert(qfmn->m_proptype == kfptPossList || qfmn->m_proptype == kfptTagList);
				// Launch the chooser dialog and update the edit box if they chose a new
				// selection.
				PossChsrDlgPtr qplc;
				qplc.Create();
				// We need the list's preferred writing system (may be selector value).
				int wsList = m_qlpi->GetPsslWsFromDb(qfmn->m_hvo);
				if (qfmn->m_proptype == kfptPossList)
				{
					qplc->SetDialogValues(qfmn->m_hvo, wsList, m_qfpe->GetPss());
				}
				else if (qfmn->m_proptype == kfptTagList)
				{
					AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(qfmn->m_flid);
					Assert(qfmn->m_hvo == aoi.m_hvoPssl);
					if (!aoi.m_qvo)
					{
						IVwOverlayPtr qvo;
						m_qlpi->GetOverlay(qfmn->m_flid, &qvo);
						aoi.m_qvo = qvo;
					}
					qplc->SetDialogValues(qfmn->m_hvo, wsList, m_qfpe->GetPss(), &aoi);
				}
				if (qplc->DoModal(m_hwnd) == kctidOk)
				{
					HVO hvoPss;
					qplc->GetDialogValues(hvoPss);
					if (hvoPss)
					{
						m_qfpe->SetPss(hvoPss);
					}
					else
					{
						// The list must be empty.  I don't think we need an error message.
					}
					// Check whether we are in the Criteria Builder dialog box.
					HWND hwndParent = reinterpret_cast<HWND>(
						::GetWindowLongPtr(m_hwnd, GWLP_HWNDPARENT));
					AfWnd * pwnd = AfWnd::GetAfWnd(hwndParent);
					FwFilterBuilderShellDlg * pfltblds = dynamic_cast<
						FwFilterBuilderShellDlg *>(pwnd);
					if (pfltblds)
						pfltblds->RefreshPossibilityColumn(m_qfpe->GetPossNameType());
				}
			}
			break;

		case kctidFilterChooseDate:
			// Launch the date picker here.
			{
				// NOTE:  See identical code in FwFilterPromptDlg::OnNotifyChild().
				GenSmartPtr<DatePickDlg> qfdpk;
				qfdpk.Create();
				AfDialogShellPtr qdlgShell;
				qdlgShell.Create();
				achar rgchDate[MAX_PATH] = { 0 };
				int cchDate = ::GetDlgItemText(m_hwnd, kctidFilterDate, rgchDate, MAX_PATH);
				int isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidFilterScope), CB_GETCURSEL,
					0, 0);
				if (cchDate)
				{
					StrUni stu(rgchDate, cchDate);
					int nYear;
					int nMonth;
					int nDay;
					ParseDate(stu, isel, nYear, nMonth, nDay, true, m_hwnd);
					qfdpk->m_systime.wYear = static_cast<unsigned short>(nYear);
					qfdpk->m_systime.wMonth = static_cast<unsigned short>(nMonth);
					qfdpk->m_systime.wDay = static_cast<unsigned short>(nDay);
				}
				else
				{
					::GetLocalTime(&qfdpk->m_systime);
				}
				StrApp str(kstidFilterDateTtl);
				if (qdlgShell->CreateNoHelpDlgShell(qfdpk, str.Chars(), m_hwnd) == kctidOk)
				{
					m_systime = qfdpk->m_systime;
					int cch = FormatDate(isel, &m_systime, rgchDate, MAX_PATH);
					rgchDate[cch] = 0;		// It's good to be paranoid!
					::SetWindowText(::GetDlgItem(m_hwnd, kctidFilterDate), rgchDate);
				}
			}
			break;
		}
		break;

	case CBN_SELENDOK:
		if (ctidFrom == kctidCondition)
		{
			// The condition has changed, so make sure the correct controls for the new
			// condition are visible.
			m_qtss.Clear();

			int encOld = m_ws;

			ShowType();

			if (!m_ws && encOld)
				m_ws = encOld;
			// Check whether we are in the Criteria Builder dialog box, and if so, update the
			// Insert button validity.
			HWND hwndParent = reinterpret_cast<HWND>(
				::GetWindowLongPtr(m_hwnd, GWLP_HWNDPARENT));
			AfWnd * pwnd = AfWnd::GetAfWnd(hwndParent);
			FwFilterBuilderShellDlg * pfltblds = dynamic_cast<FwFilterBuilderShellDlg *>(pwnd);
			FilterKeywordType fkt = FilterUtil::GetKeywordType(m_fft, m_hwnd);
			if (pfltblds)
			{
				if (fkt == kfktEmpty || fkt == kfktNotEmpty || fkt == kfktYes || fkt == kfktNo)
				{
					pfltblds->EnableInsertBtn(true);
				}
				else
				{
					// Check whether we should enable the Insert button.
					achar rgchItem[MAX_PATH];
					int cch;
					bool fNumber;
					StrApp strNumber;
					switch (m_fft)
					{
					case kfftText:
					case kfftRefText:
						// Just need some text, any text.
						cch = m_qte->GetTextLength();
						pfltblds->EnableInsertBtn(cch != 0);
						break;
					case kfftDate:
						pfltblds->EnableInsertBtn(IsValidDate());
						break;
					case kfftEnum:
					case kfftEnumReq:
						// Just need some text, any text.
						cch = ::GetDlgItemText(m_hwnd, kctidFilterEnum, rgchItem, MAX_PATH);
						pfltblds->EnableInsertBtn(cch != 0);
						break;
					case kfftRef:
						// Just need some text, any text.
						cch = ::GetDlgItemText(m_hwnd, kctidFilterRef, rgchItem, MAX_PATH);
						pfltblds->EnableInsertBtn(cch != 0);
						break;
					case kfftBoolean:
						// Nothing to do for Boolean.
						break;
					case kfftCrossRef:
						cch = m_qte->GetTextLength();
						pfltblds->EnableInsertBtn(cch != 0);
						break;
					case kfftNumber:
						// Verify that it's a valid number.
						cch = ::GetDlgItemText(m_hwnd, kctidFilterNumber, rgchItem, MAX_PATH);
						fNumber = ValidateNumber(rgchItem, cch, strNumber);
						if (strNumber.Length())
						{
							StrApp strTitle(kstidFltrNumberCap);
							StrApp strMsg(kstidFltrMaxIntMsg);
							::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(),
								MB_OK | MB_TASKMODAL);
							::SetDlgItemText(m_hwnd, kctidFilterNumber, strNumber.Chars());
						}
						pfltblds->EnableInsertBtn(fNumber);
						break;
					}
				}
			}
			// Adjust the date scope combobox if necessary.
			if (m_fft == kfftDate)
			{
				HWND hwndCombo = ::GetDlgItem(m_hwnd, kctidFilterScope);
				bool fExpanded = fkt == kfktEmpty || fkt == kfktNotEmpty || fkt == kfktEqual ||
					fkt == kfktNotEqual;
				FilterUtil::FillDateScopeCombo(hwndCombo, fExpanded);
			}
		}
		else if (ctidFrom == kctidFilterScope)
		{
			// The date scope has changed, so make sure the correct controls for the new scope
			// are enabled/disabled.
			Assert(m_fft == kfftDate);
			int isel = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);
			bool fEnable = isel < kcselDateNeeded;
			::EnableWindow(::GetDlgItem(m_hwnd, kctidFilterDate), fEnable);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidFilterChooseDate), fEnable);
			achar rgchDate[MAX_PATH+1] = { 0 };
			int cch = FormatDate(isel, &m_systime, rgchDate, MAX_PATH);
			rgchDate[cch] = 0;
			::SetWindowText(::GetDlgItem(m_hwnd, kctidFilterDate), rgchDate);
			HWND hwndPar = ::GetParent(m_hwnd);
			::EnableWindow(::GetDlgItem(hwndPar, kctidCriteria), true);
			bool fChecked = ::IsDlgButtonChecked(hwndPar, kctidCriteria);
			::EnableWindow(::GetDlgItem(hwndPar, kctidPromptLabel), fChecked);
			::EnableWindow(::GetDlgItem(hwndPar, kctidPrompt), fChecked);
			m_fAllowCriteriaPrompt = true;
			// Check whether we are in the Criteria Builder dialog box, and if so, update the
			// Insert button validity.
			HWND hwndParent = reinterpret_cast<HWND>(
				::GetWindowLongPtr(m_hwnd, GWLP_HWNDPARENT));
			AfWnd * pwnd = AfWnd::GetAfWnd(hwndParent);
			FwFilterBuilderShellDlg * pfltblds = dynamic_cast<FwFilterBuilderShellDlg *>(pwnd);
			if (pfltblds)
				pfltblds->EnableInsertBtn(IsValidDate());
		}
		break;

	case EN_CHANGE:
	case EN_KILLFOCUS:
		if (ctidFrom == kctidFilterDate || ctidFrom == kctidFilterEnum ||
			ctidFrom == kctidFilterRef)
		{
			// Check whether we are in the Criteria Builder dialog box.
			HWND hwndParent = reinterpret_cast<HWND>(
				::GetWindowLongPtr(m_hwnd, GWLP_HWNDPARENT));
			FwFilterBuilderShellDlg * pfltblds =
				dynamic_cast<FwFilterBuilderShellDlg *>(AfWnd::GetAfWnd(hwndParent));
			if (pfltblds && ::IsWindowEnabled(::GetDlgItem(m_hwnd, ctidFrom)))
			{
				// Check whether we should enable the Insert button.
				achar rgchItem[MAX_PATH];
				int cch = ::GetDlgItemText(m_hwnd, ctidFrom, rgchItem, MAX_PATH);
				rgchItem[cch] = 0;		// Paranoia - is this needed?
				if (ctidFrom == kctidFilterDate)
				{
					// Need a valid date string.
					pfltblds->EnableInsertBtn(IsValidDate());
				}
				else
				{
					// Just need some text.
					pfltblds->EnableInsertBtn(cch != 0);
				}
			}
		}
		else if (ctidFrom == kctidFilterText)
		{
			// Check whether we are in the Criteria Builder dialog box.
			HWND hwndParent = reinterpret_cast<HWND>(
				::GetWindowLongPtr(m_hwnd, GWLP_HWNDPARENT));
			FwFilterBuilderShellDlg * pfltblds =
				dynamic_cast<FwFilterBuilderShellDlg *>(AfWnd::GetAfWnd(hwndParent));
			if (pfltblds && ::IsWindowEnabled(m_qte->Hwnd()))
			{
				// Check whether we should enable the Insert button.  (Just need some text.)
				pfltblds->EnableInsertBtn(m_qte->GetTextLength() != 0);
			}
		}
		else if (ctidFrom == kctidFilterNumber)
		{
			HWND hwndParent = reinterpret_cast<HWND>(
				::GetWindowLongPtr(m_hwnd, GWLP_HWNDPARENT));
			FwFilterBuilderShellDlg * pfltblds =
				dynamic_cast<FwFilterBuilderShellDlg *>(AfWnd::GetAfWnd(hwndParent));
			if (pfltblds && ::IsWindowEnabled(::GetDlgItem(m_hwnd, ctidFrom)))
			{
				// Verify that it's a valid number.
				achar rgchItem[MAX_PATH];
				int cch = ::GetDlgItemText(m_hwnd, kctidFilterNumber, rgchItem, MAX_PATH);
				StrApp strNumber;
				bool fNumber = ValidateNumber(rgchItem, cch, strNumber);
				if (strNumber.Length() && pnmh->code == EN_CHANGE)
				{
					StrApp strTitle(kstidFltrNumberCap);
					StrApp strMsg(kstidFltrMaxIntMsg);
					::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(),
						MB_OK | MB_TASKMODAL);		// produces EN_KILLFOCUS
					::SetDlgItemText(m_hwnd, kctidFilterNumber, strNumber.Chars());
				}
				pfltblds->EnableInsertBtn(fNumber);
			}
		}
		break;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Update the simple filter dialog to show the new type of field.

	@param fptOld The specific type formerly associated with this field.
----------------------------------------------------------------------------------------------*/
void FwFilterSimpleDlg::ShowType(int fptOld)
{
	int rgcidText[] = { kctidTextLabel, kctidFilterText, kctidFilterMatchCase,
		kctidFilterMatchDiac, kctidFilterSpecial, kctidFilterFormat };
	int rgcidRef[] = { kctidRefLabel, kctidFilterRef, kctidFilterChooseItem,
		kctidFilterSubitems };
	int rgcidRefText[] = { kctidTextLabel, kctidFilterText, kctidFilterMatchCase,
		kctidFilterMatchDiac, kctidFilterSpecial };
	int rgcidDate[] = { kctidScopeLabel, kctidFilterScope, kctidDateLabel,
		kctidFilterDate, kctidFilterChooseDate };
	int rgcidEnum[] = { kctidEnumLabel, kctidFilterEnum };
	int rgcidEnumReq[] = { kctidEnumLabel, kctidFilterEnum };
	int rgcidCrossRef[] = { kctidTextLabel, kctidFilterText, kctidFilterMatchCase,
		kctidFilterMatchDiac, kctidFilterSpecial };
	int rgcidNumber[] = { kctidNumberLabel, kctidFilterNumber };

	// NOTE: If you change the order in prgcidAll, change the order in rgccid as well.
	int * prgcidAll[] = { rgcidText, rgcidRef, rgcidRefText, rgcidDate, rgcidEnum, rgcidEnumReq,
		NULL,	// Boolean has only a Yes/No operator with no additional value field.
		rgcidCrossRef, rgcidNumber,
	};
	int rgccid[] = { SizeOfArray(rgcidText), SizeOfArray(rgcidRef), SizeOfArray(rgcidRefText),
		SizeOfArray(rgcidDate), SizeOfArray(rgcidEnum), SizeOfArray(rgcidEnumReq),
		0,		// Boolean has only a Yes/No operator with no additional value field.
		SizeOfArray(rgcidCrossRef), SizeOfArray(rgcidNumber),
	};

	// These checks should assert if something gets changed in one place but doesn't get
	// updated here too.
	Assert(kfftLim == 9);
	Assert(SizeOfArray(prgcidAll) == kfftLim);
	Assert(SizeOfArray(rgccid) == kfftLim);

	KeywordLookup kl;
	FilterFieldType fft = kfftNone;
	FilterKeywordType fkt = kfktError;
	int fpt = kcptNil;
	int isel = -1;
	StrApp strCondition;
	bool fEmpty = false;
	bool fSubitems = false;
	int wsUser = m_qlpi->GetDbInfo()->UserWs();
	m_ws = 0;
	if (m_qtss)
	{
		// We are initializing the controls with an existing simple filter.
		const OLECHAR * prgchMin;
		int cch;
		CheckHr(m_qtss->LockText(&prgchMin, &cch));
		StrUni stuCell;
		stuCell.Assign(prgchMin, cch);
		m_qtss->UnlockText(prgchMin);

		const wchar * prgch = const_cast<wchar *>(stuCell.Chars());
		FilterUtil::SkipWhiteSpace(prgch);
		fkt = kl.GetTypeFromStr(prgch);
		if (fkt == kfktEmpty || fkt == kfktNotEmpty)
		{
			fEmpty = true;
		}

		// Extract any writing system value.
		ILgWritingSystemFactoryPtr qwsf;
		m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
		m_ws = FilterUtil::ParseWritingSystem(prgch, qwsf);
		if (*prgch == '"')
			prgch++;
		const wchar * prgchLim = prgch;
		while (*prgchLim != 0 && *prgchLim != '"')
		{
			if (*prgchLim == '\\')
			{
				if (prgchLim[1] == '\\' || prgchLim[1] == '"')
					++prgchLim;
			}
			++prgchLim;
		}
		strCondition.Assign(prgch, prgchLim - prgch);
		int ich = strCondition.FindCh('\\');
		while (ich >= 0)
		{
			int ch = strCondition.GetAt(ich+1);
			if (ch == '\\' || ch == '"')
				strCondition.Replace(ich, ich+1, L"");
			ich = strCondition.FindCh('\\', ich+1);
		}
		int ichSub = stuCell.FindStr(L" +subitems");
		if (ichSub > 3 && stuCell.Length() == ichSub + 10)
			fSubitems = true;
	}
	if (m_vfmn.Size() > 0)
	{
		FilterMenuNode * pfmn = *m_vfmn.Top();
		AssertPtr(pfmn);
		const int kiselContains = 2;
		// Get the field type from the column information.
		isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidCondition), CB_GETCURSEL, 0, 0);
		if (!m_fIgnoreCurSel && isel < kiselContains &&
			pfmn->m_proptype != kfptEnumListReq && pfmn->m_proptype != kfptBoolean &&
			pfmn->m_proptype != kcptBoolean && pfmn->m_proptype != kcptInteger &&
			pfmn->m_proptype != kcptNumeric && pfmn->m_proptype != kcptFloat)
		{
			Assert(isel != -1);
			fEmpty = true;
		}
		fpt = pfmn->m_proptype;
		switch (fpt)
		{
		case kcptTime:
		case kcptGenDate:
			fft = kfftDate;
			break;

		case kcptBoolean:
		case kfptBoolean:
			fft = kfftBoolean;
			break;

		case kcptInteger:
		case kcptNumeric:
		case kcptFloat:
			fft = kfftNumber;
			break;

		case kcptString:
		case kcptUnicode:
		case kcptBigString:
		case kcptBigUnicode:
		case kcptMultiString:
		case kcptMultiUnicode:
		case kcptMultiBigString:
		case kcptMultiBigUnicode:
		case kfptStText:
			fft = kfftText;
			break;
		case kcptOwningAtom:
		case kcptReferenceAtom:
		case kcptOwningCollection:
		case kcptReferenceCollection:
		case kcptOwningSequence:
		case kcptReferenceSequence:
		case kfptPossList:
		case kfptTagList:
		case kfptRoledParticipant:
			if (fkt == kfktContains || fkt == kfktDoesNotContain)
			{
				fft = kfftRefText;
			}
			else
			{
				const int kiselMatches = 4;
				fft = kfftRef;
				if (!m_fIgnoreCurSel && !fEmpty && isel < kiselMatches)
				{
					Assert(isel != -1);
					fft = kfftRefText;
				}
			}
			break;
		case kfptEnumList:
			fft = kfftEnum;
			break;
		case kfptEnumListReq:
			fft = kfftEnumReq;
			break;
		case kfptCrossRef:
		case kfptCrossRefList:
			fft = kfftCrossRef;
			if (m_fIgnoreCurSel && fkt != kfktContains && fkt != kfktDoesNotContain)
				fEmpty = true;
			break;
		}
	}

	int nCmdShow = (fft == kfftNone && !fEmpty) ? SW_HIDE : SW_SHOW;
	::ShowWindow(::GetDlgItem(m_hwnd, kctidConditionLabel), nCmdShow);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidCondition), nCmdShow);

	HDWP hdwp;
	// Hide the old group of windows.
	if (m_fft > kfftNone)
	{
		Assert((uint)m_fft < (uint)(isizeof(rgccid) / isizeof(int)));
		hdwp = ::BeginDeferWindowPos(rgccid[m_fft]);
		for (int iid = 0; iid < rgccid[m_fft]; iid++)
		{
			HWND hwnd = ::GetDlgItem(m_hwnd, prgcidAll[m_fft][iid]);
			::DeferWindowPos(hdwp, hwnd, NULL, 0, 0, 0, 0,
				SWP_NOMOVE | SWP_NOSIZE | SWP_HIDEWINDOW | SWP_NOZORDER);
			::EnableWindow(hwnd, FALSE);
		}
		::EndDeferWindowPos(hdwp);
	}
	// Show the new group of windows.
	if (fft > kfftNone)
	{
		Assert((uint)fft < (uint)(isizeof(rgccid) / isizeof(int)));
		hdwp = ::BeginDeferWindowPos(rgccid[fft]);
		for (int iid = 0; iid < rgccid[fft]; iid++)
		{
			int ctid = prgcidAll[fft][iid];
			HWND hwnd = ::GetDlgItem(m_hwnd, ctid);
			BOOL fEnable = !fEmpty;
			if (ctid == kctidFilterFormat)
			{
				if (m_vfmn.Size())
				{
					fEnable = FALSE;
					FilterMenuNodePtr qfmn = *m_vfmn.Top();
					if (qfmn->m_proptype == kcptMultiUnicode ||
						qfmn->m_proptype == kcptMultiString ||
						qfmn->m_proptype == kcptMultiBigUnicode ||
						qfmn->m_proptype == kcptMultiBigString)
					{
						fEnable = TRUE;
					}
				}
			}
			::DeferWindowPos(hdwp, hwnd, NULL, 0, 0, 0, 0, fEnable ?
				SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOZORDER :
				SWP_NOMOVE | SWP_NOSIZE | SWP_HIDEWINDOW | SWP_NOZORDER);
			::EnableWindow(hwnd, fEnable);
		}
		::EndDeferWindowPos(hdwp);
	}

	/*----------------------------------------------------------------------------------------*/
	// TODO DarrellZ: Since these aren't implemented yet, they are being disabled and hidden
	// here.  These lines must be deleted when these controls get implemented.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFilterMatchCase), FALSE);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidFilterMatchCase), SW_HIDE);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFilterMatchDiac), FALSE);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidFilterMatchDiac), SW_HIDE);
	/*----------------------------------------------------------------------------------------*/

	// Update the Condition combo box and the right side of the dialog with the proper
	// values based on the field type and contents.
	HWND hwndCombo = ::GetDlgItem(m_hwnd, kctidCondition);
	if (fft != kfftNone)
	{
		bool fWideCondition = false;
		int ctidLabel = 0;
		int ctidValue = 0;
		// Update the combobox items if:
		//   - we are changing a type of field that is being shown, and
		//   - we are not switching between kfftRefText and kfftRef
		bool fUpdateCombo = !((m_fft == fft) || (m_fft == kfftRef && fft == kfftRefText) ||
				(m_fft == kfftRefText && fft == kfftRef));
		if (!fUpdateCombo && fptOld != kcptNil && fpt != fptOld)
		{
			bool fOldBig = (fptOld == kfptStText || fptOld == kcptBigString ||
				fptOld == kcptBigUnicode || fptOld == kcptMultiBigString ||
				fptOld == kcptMultiBigUnicode);
			bool fNewBig = (fpt == kfptStText || fpt == kcptBigString ||
				fpt == kcptBigUnicode || fpt == kcptMultiBigString ||
				fpt == kcptMultiBigUnicode);
			fUpdateCombo = (fOldBig != fNewBig);
		}

		// Update the combo box and other controls based on the cell text.
		StrApp str;
		if (fUpdateCombo)
		{
			::SendMessage(hwndCombo, CB_RESETCONTENT, 0, 0);
			if (fft != kfftEnumReq && fft != kfftBoolean && fft != kfftNumber)
			{
				str.Load(kstidFltrEmpty);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrNotEmpty);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
			}
		}

		// NOTE: m_fIgnoreCurSel will be set to true the first time a cell is being shown. This
		// usually means that the contents of the other controls that are visible should be
		// refreshed to the new string/item/date/... The reason we need this is because we
		// don't want to reset the other controls every time the user changes a condition,
		// because they might type values in the other controls, and then decide just to change
		// the condition from 'Equals' to 'Does not Equal'. The other controls should not
		// be reset in this case.
		switch (fft)
		{
		case kfftText:
			if (fUpdateCombo)
			{
				// Add the combobox items for a text field.
				str.Load(kstidFltrContains);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrDoesNotContain);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrEqualTo);
				str.Append(" (=)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrNotEqualTo);
				str.Append(" (<>)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				if (fpt != kfptStText && fpt != kcptBigString && fpt != kcptBigUnicode &&
					fpt != kcptMultiBigString && fpt != kcptMultiBigUnicode)
				{
					str.Load(kstidFltrGreaterThan);
					str.Append(" (>)");
					::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
					str.Load(kstidFltrLessThan);
					str.Append(" (<)");
					::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
					str.Load(kstidFltrGreaterThanEqual);
					str.Append(" (>=)");
					::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
					str.Load(kstidFltrLessThanEqual);
					str.Append(" (<=)");
					::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
					fWideCondition = true;
					ctidLabel = kctidTextLabel;
					ctidValue = kctidFilterText;
				}
			}
			// Figure out which item should be selected in the condition combobox.
			switch (fkt)
			{
			case kfktEmpty:				isel = 0; break;
			case kfktNotEmpty:			isel = 1; break;
			case kfktContains:			isel = 2; break;
			case kfktDoesNotContain:	isel = 3; break;
			case kfktEqual:				isel = 4; break;
			case kfktNotEqual:			isel = 5; break;
			case kfktGT:				isel = 6; break;
			case kfktLT:				isel = 7; break;
			case kfktGTE:				isel = 8; break;
			case kfktLTE:				isel = 9; break;
			default:
				if (m_fIgnoreCurSel)
				{
					isel = 2;	// Choose the default value: "Contains".
					::SetFocus(m_qte->Hwnd());
				}
				break;
			}
			if (m_fIgnoreCurSel)
			{
				StrUni stu(strCondition);
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				ITsStringPtr qtss;
				qtsf->MakeStringRgch(stu.Chars(), stu.Length(), wsUser, &qtss);
				m_qte->SetText(qtss);
			}
			break;
		case kfftRef:
			{
				HVO hvoPssl;
				HVO hvoPss;
				FilterUtil::StringToReference(m_qtss, m_vfmn, m_qlpi->GetDbInfo(), hvoPssl,
					hvoPss);
				PossListInfoPtr qpli;
				AfMainWnd * pafw = MainWindow();
				AssertPtr(pafw);
				AfLpInfo * plpi = pafw->GetLpInfo();
				AssertPtr(plpi);
				plpi->LoadPossList(hvoPssl, plpi->GetPsslWsFromDb(hvoPssl), &qpli);
				HWND hwndSub = ::GetDlgItem(m_hwnd, kctidFilterSubitems);
				if (qpli->GetDepth() == 1)
				{
					::EnableWindow(hwndSub, FALSE);
					::ShowWindow(hwndSub, SW_HIDE);
				}
				else
				{
					::SendMessage(hwndSub, BM_SETCHECK,
						fSubitems ? BST_CHECKED : BST_UNCHECKED, 0);
				}
			}
			// Fall through.
		case kfftRefText:
			if (fUpdateCombo)
			{
				// Add the combobox items for a reference field.
				str.Load(kstidFltrContains);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrDoesNotContain);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrMatches);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrDoesNotMatch);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
			}
			// Figure out which item should be selected in the condition combobox.
			switch (fkt)
			{
			case kfktEmpty:				isel = 0; break;
			case kfktNotEmpty:			isel = 1; break;
			case kfktContains:			isel = 2; break;
			case kfktDoesNotContain:	isel = 3; break;
			case kfktMatches:			isel = 4; break;
			case kfktDoesNotMatch:		isel = 5; break;
			default:
				if (m_fIgnoreCurSel)
				{
					if (fft == kfftRef)
					{
						isel = 4;	// Choose the default value: "Matches".
						::SetFocus(m_qfpe->Hwnd());
					}
					else
					{
						isel = 2;	// Choose the default value: "Contains".
						::SetFocus(m_qte->Hwnd());
					}
				}
				break;
			}
			if (m_fIgnoreCurSel)
			{
				if (fft == kfftRef)
				{
					HVO hvoPssl;
					HVO hvoPss;
					FilterUtil::StringToReference(m_qtss, m_vfmn, m_qlpi->GetDbInfo(), hvoPssl,
						hvoPss);
					Assert(m_vfmn.Size());
					FilterMenuNode * pfmn = *m_vfmn.Top();
					AssertPtr(pfmn);
					if (pfmn->m_proptype == kfptTagList)
					{
						AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(pfmn->m_flid);
						Assert(pfmn->m_hvo == aoi.m_hvoPssl);
						if (!aoi.m_qvo)
						{
							IVwOverlayPtr qvo;
							m_qlpi->GetOverlay(pfmn->m_flid, &qvo);
							aoi.m_qvo = qvo;
						}
						m_qfpe->SetPssl(hvoPssl, hvoPss, &aoi);
					}
					else
					{
						m_qfpe->SetPssl(hvoPssl, hvoPss);
					}
				}
				else
				{
					StrUni stu(strCondition);
					ITsStrFactoryPtr qtsf;
					qtsf.CreateInstance(CLSID_TsStrFactory);
					ITsStringPtr qtss;
					qtsf->MakeStringRgch(stu.Chars(), stu.Length(), wsUser, &qtss);
					m_qte->SetText(qtss);
				}
			}
			break;
		case kfftDate:
			{
				if (fUpdateCombo)
				{
					// Add the combobox items for a date field.
					str.Load(kstidFltrOn);
					str.Append(" (=)");
					::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
					str.Load(kstidFltrNotOn);
					str.Append(" (<>)");
					::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
					str.Load(kstidFltrAfter);
					str.Append(" (>)");
					::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
					str.Load(kstidFltrBefore);
					str.Append(" (<)");
					::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
					str.Load(kstidFltrOnAfter);
					str.Append(" (>=)");
					::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
					str.Load(kstidFltrOnBefore);
					str.Append(" (<=)");
					::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				}
				// Figure out which item should be selected in the condition combobox.
				switch (fkt)
				{
				case kfktEmpty:		isel = 0; break;
				case kfktNotEmpty:	isel = 1; break;
				case kfktEqual:		isel = 2; break;
				case kfktNotEqual:	isel = 3; break;
				case kfktGT:		isel = 4; break;
				case kfktLT:		isel = 5; break;
				case kfktGTE:		isel = 6; break;
				case kfktLTE:		isel = 7; break;
				default:
					if (m_fIgnoreCurSel)
					{
						isel = 6;	// Choose the default value: "On or after".
						::SetFocus(::GetDlgItem(m_hwnd, kctidFilterDate));
					}
					break;
				}
				if (m_fIgnoreCurSel)
				{
					int iselScope = -1;
					if (strCondition.Length())
					{
						StrUni stuCondition(strCondition);
						const wchar * prgch = const_cast<wchar *>(stuCondition.Chars());
						DateKeywordLookup dkl;
						int iselT = dkl.GetIndexFromStr(prgch);
						if (iselT > -1)
						{
							FilterUtil::SkipWhiteSpace(prgch);
							if (*prgch == '(')
							{
								const wchar * prgchLim = ++prgch + 1;
								while (*prgchLim && *prgchLim != ')')
									prgchLim++;
								strCondition.Assign(prgch, prgchLim - prgch);
								iselScope = iselT;
							}
							else if (*prgch == 0)
							{
								iselScope = iselT;
								strCondition.Clear();
							}
						}
					}
					if (iselScope == -1)
					{
						iselScope = kiselMonthYear; // Default is Month and Year.
						strCondition.Clear();
					}
					::SendMessage(::GetDlgItem(m_hwnd, kctidFilterScope), CB_SETCURSEL,
						iselScope, 0);
					if (iselScope < kcselDateNeeded)
					{
						if (strCondition.Length())
						{
							StrUni stu(strCondition);
							int nYear;
							int nMonth;
							int nDay;
							ParseDate(stu, iselScope, nYear, nMonth, nDay, true, m_hwnd);
							m_systime.wYear = static_cast<unsigned short>(nYear);
							m_systime.wMonth = static_cast<unsigned short>(nMonth);
							m_systime.wDay = static_cast<unsigned short>(nDay);
						}
						else
						{
							::GetLocalTime(&m_systime);
						}
						achar rgchDate[MAX_PATH+1] = { 0 };
						int cch = FormatDate(iselScope, &m_systime, rgchDate, MAX_PATH);
						strCondition.Assign(rgchDate, cch);
						::SetDlgItemText(m_hwnd, kctidFilterDate, strCondition.Chars());
						m_fAllowCriteriaPrompt = true;
					}
					else
					{
						::EnableWindow(::GetDlgItem(m_hwnd, kctidFilterDate), false);
						::EnableWindow(::GetDlgItem(m_hwnd, kctidFilterChooseDate), false);
						HWND hwndPar = ::GetParent(m_hwnd);
						::EnableWindow(::GetDlgItem(hwndPar, kctidCriteria), false);
						::EnableWindow(::GetDlgItem(hwndPar, kctidPromptLabel), false);
						::EnableWindow(::GetDlgItem(hwndPar, kctidPrompt), false);
						m_fAllowCriteriaPrompt = false;
					}
				}
				else
				{
					int iselScope = ::SendMessage(::GetDlgItem(m_hwnd, kctidFilterScope),
						CB_GETCURSEL, 0, 0);
					if (iselScope >= kcselDateNeeded)
					{
						// Keep these disabled.
						::EnableWindow(::GetDlgItem(m_hwnd, kctidFilterDate), false);
						::EnableWindow(::GetDlgItem(m_hwnd, kctidFilterChooseDate), false);
					}
				}
			}
			break;
		case kfftEnum:
			if (fUpdateCombo)
			{
				// Add the combobox items for an enum field.
				str.Load(kstidFltrEqualTo);
				str.Append(" (=)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrNotEqualTo);
				str.Append(" (<>)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrGreaterThan);
				str.Append(" (>)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrLessThan);
				str.Append(" (<)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrGreaterThanEqual);
				str.Append(" (>=)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrLessThanEqual);
				str.Append(" (<=)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				fWideCondition = true;
				ctidLabel = kctidEnumLabel;
				ctidValue = kctidFilterEnum;
			}
			// Figure out which item should be selected in the condition combobox.
			switch (fkt)
			{
			case kfktEmpty:		isel = 0; break;
			case kfktNotEmpty:	isel = 1; break;
			case kfktEqual:		isel = 2; break;
			case kfktNotEqual:	isel = 3; break;
			case kfktGT:		isel = 4; break;
			case kfktLT:		isel = 5; break;
			case kfktGTE:		isel = 6; break;
			case kfktLTE:		isel = 7; break;
			default:
				if (m_fIgnoreCurSel)
				{
					isel = 2;	// Choose the default value: "Equal".
					::SetFocus(::GetDlgItem(m_hwnd, kctidFilterEnum));
				}
				break;
			}
			if (m_fIgnoreCurSel)
			{
				FilterMenuNode * pfmn = m_vfmn[m_vfmn.Size() - 1];
				AssertPtr(pfmn);
				Assert(pfmn->m_stid);
				FilterUtil::AddEnumToCombo(::GetDlgItem(m_hwnd, kctidFilterEnum), pfmn->m_stid,
					strCondition.Chars());
			}
			break;
		case kfftEnumReq:
			if (fUpdateCombo)
			{
				// Add the combobox items for an enum field.
				str.Load(kstidFltrEqualTo);
				str.Append(" (=)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrNotEqualTo);
				str.Append(" (<>)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrGreaterThan);
				str.Append(" (>)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrLessThan);
				str.Append(" (<)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrGreaterThanEqual);
				str.Append(" (>=)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrLessThanEqual);
				str.Append(" (<=)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				fWideCondition = true;
				ctidLabel = kctidEnumLabel;
				ctidValue = kctidFilterEnum;
			}
			// Figure out which item should be selected in the condition combobox.
			switch (fkt)
			{
			case kfktEqual:		isel = 0; break;
			case kfktNotEqual:	isel = 1; break;
			case kfktGT:		isel = 2; break;
			case kfktLT:		isel = 3; break;
			case kfktGTE:		isel = 4; break;
			case kfktLTE:		isel = 5; break;
			default:
				if (m_fIgnoreCurSel)
				{
					isel = 0;	// Choose the default value: "Equal".
					::SetFocus(::GetDlgItem(m_hwnd, kctidFilterEnum));
				}
				break;
			}
			if (m_fIgnoreCurSel)
			{
				FilterMenuNode * pfmn = m_vfmn[m_vfmn.Size() - 1];
				AssertPtr(pfmn);
				Assert(pfmn->m_stid);
				FilterUtil::AddEnumToCombo(::GetDlgItem(m_hwnd, kctidFilterEnum), pfmn->m_stid,
					strCondition.Chars());
			}
			break;
		case kfftBoolean:
			if (fUpdateCombo)
			{
				// Add the combobox items for a Boolean field.
				str.Load(kstidFltrYes);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrNo);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
			}
			// Figure out which item should be selected in the condition combobox.
			switch (fkt)
			{
			case kfktYes:	isel = 0; break;
			case kfktNo:	isel = 1; break;
			default:
				if (m_fIgnoreCurSel)
					isel = 0;	// Choose the default value: "Yes".
				break;
			}
			break;
		case kfftCrossRef:
			if (fUpdateCombo)
			{
				// Have already added Empty and NotEmpty.
				// Add the combobox items for a cross reference field.
				str.Load(kstidFltrContains);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrDoesNotContain);
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
			}
			switch (fkt)
			{
			case kfktEmpty:				isel = 0; break;
			case kfktNotEmpty:			isel = 1; break;
			case kfktContains:			isel = 2; break;
			case kfktDoesNotContain:	isel = 3; break;
			default:
				if (m_fIgnoreCurSel)
				{
					isel = 1;		// Choose the default value: "Not Empty".
				}
				break;
			}
			if (m_fIgnoreCurSel)
			{
				StrUni stu(strCondition);
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				ITsStringPtr qtss;
				qtsf->MakeStringRgch(stu.Chars(), stu.Length(), wsUser, &qtss);
				m_qte->SetText(qtss);
			}
			break;
		case kfftNumber:
			if (fUpdateCombo)
			{
				// Add the combobox items for a Number field.
				str.Load(kstidFltrEqualTo);
				str.Append(" (=)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrNotEqualTo);
				str.Append(" (<>)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrGreaterThan);
				str.Append(" (>)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrLessThan);
				str.Append(" (<)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrGreaterThanEqual);
				str.Append(" (>=)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				str.Load(kstidFltrLessThanEqual);
				str.Append(" (<=)");
				::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)str.Chars());
				fWideCondition = true;
				ctidLabel = kctidNumberLabel;
				ctidValue = kctidFilterNumber;
			}
			// Figure out which item should be selected in the condition combobox.
			switch (fkt)
			{
			case kfktEqual:		isel = 0; break;
			case kfktNotEqual:	isel = 1; break;
			case kfktGT:		isel = 2; break;
			case kfktLT:		isel = 3; break;
			case kfktGTE:		isel = 4; break;
			case kfktLTE:		isel = 5; break;
			default:
				if (m_fIgnoreCurSel)
				{
					isel = 0;	// Choose the default value: "Equal".
					::SetFocus(::GetDlgItem(m_hwnd, kctidFilterNumber));
				}
				break;
			}
			if (m_fIgnoreCurSel)
				::SetDlgItemText(m_hwnd, kctidFilterNumber, strCondition.Chars());
			break;
		default:
			// Remove all the items from the combo box.
			::SendMessage(hwndCombo, CB_RESETCONTENT, 0, 0);
			break;
		}

		if (isel != -1)
			::SendMessage(hwndCombo, CB_SETCURSEL, isel, 0);

		if (fUpdateCombo && fWideCondition != m_fWideCondition)
		{
			int rgctidAdjust[] = { kctidTextLabel, kctidFilterText,
				kctidEnumLabel, kctidFilterEnum, kctidNumberLabel, kctidFilterNumber };
			const int kcctidAdjust = isizeof(rgctidAdjust) / isizeof(int);
			const int kxAdjust = 30;
			int ictid;
			HWND hwndParent = ::GetParent(hwndCombo);
			RECT rcParent;
			RECT rcCombo;
			RECT rcValue;
			::GetWindowRect(hwndParent, &rcParent);
			::GetWindowRect(hwndCombo, &rcCombo);
			if (fWideCondition)
			{
				// Widen the combobox window and narrow the associated value windows.
				Assert(!m_fWideCondition);
				::MoveWindow(hwndCombo,
					rcCombo.left - rcParent.left, rcCombo.top - rcParent.top,
					rcCombo.right - rcCombo.left + kxAdjust, rcCombo.bottom - rcCombo.top,
					TRUE);
				for (ictid = 0; ictid < kcctidAdjust; ++ictid)
				{
					int ctid = rgctidAdjust[ictid];
					HWND hwndValue = ::GetDlgItem(m_hwnd, ctid);
					Assert(hwndValue);
					Assert(::GetParent(hwndValue) == hwndParent);
					::GetWindowRect(hwndValue, &rcValue);
					::MoveWindow(hwndValue,
						rcValue.left - rcParent.left + kxAdjust, rcValue.top - rcParent.top,
						rcValue.right - rcValue.left - kxAdjust, rcValue.bottom - rcValue.top,
						ctid == ctidLabel || ctid == ctidValue);
				}
			}
			else
			{
				// Narrow the combobox window and widen the associated value windows.
				Assert(m_fWideCondition);
				::MoveWindow(hwndCombo,
					rcCombo.left - rcParent.left, rcCombo.top - rcParent.top,
					rcCombo.right - rcCombo.left - kxAdjust, rcCombo.bottom - rcCombo.top,
					TRUE);
				for (ictid = 0; ictid < kcctidAdjust; ++ictid)
				{
					int ctid = rgctidAdjust[ictid];
					HWND hwndValue = ::GetDlgItem(m_hwnd, ctid);
					Assert(hwndValue);
					Assert(::GetParent(hwndValue) == hwndParent);
					::GetWindowRect(hwndValue, &rcValue);
					::MoveWindow(hwndValue,
						rcValue.left - rcParent.left - kxAdjust, rcValue.top - rcParent.top,
						rcValue.right - rcValue.left + kxAdjust, rcValue.bottom - rcValue.top,
						ctid == ctidLabel || ctid == ctidValue);
				}
			}
			m_fWideCondition = fWideCondition;
			// This appears to be necessary, unfortunately.
			::RedrawWindow(m_hwnd, NULL, NULL, RDW_ERASE | RDW_FRAME | RDW_INVALIDATE);
		}
	}

	if (fft != kfftNone || m_vfmn.Size() == 0)
		m_fft = fft;
	m_fIgnoreCurSel = false;

	::SendMessage(::GetParent(m_hwnd), s_wmConditionChanged, fft, fkt);
}

/*----------------------------------------------------------------------------------------------
	Refresh the possibility edit box to reflect the new choice for how to display
	possibilities in this list.

	@param hvo Database id of the possibility item.
	@param pnt Specifies how the possibility is to be displayed.
----------------------------------------------------------------------------------------------*/
void FwFilterSimpleDlg::RefreshPossibility(HVO hvo, PossNameType pnt)
{
	if (m_qfpe)
		m_qfpe->Refresh(hvo, pnt);
}

/*----------------------------------------------------------------------------------------------
	This method implements the expandable items for old writing systems in the popup menu that
	appears when the user clicks the "Format" button in the filter dialog.  It has three
	responsibilities.
	1)	If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to replace the dummy
		item by adding new items. It generates an item for each currently used writing system,
		by stealing the items from the Formatting toolbar.
	2)	If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the status
		bar string for an expanded item. Nothing is available at present, so return false.
	3)	If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
		selected an expandable menu item. Figure which writing system it is and apply it to the
		filter definition.

	Expanding items:
		pcmd->m_rgn[1] -> Contains the handle to the menu (HMENU) to add items to.
		pcmd->m_rgn[2] -> Contains the index in the menu where you should start inserting items.
		pcmd->m_rgn[3] -> This value must be set to the number of items that you inserted.
	The expanded items will automatically be deleted when the menu is closed. The dummy
	menu item will be deleted for you, so don't do anything with it here.

	Getting the status bar text:
		pcmd->m_rgn[1] -> Contains the index of the expanded/inserted item to get text for.
		pcmd->m_rgn[2] -> Contains a pointer (StrApp *) to the text for the inserted item.
	If the menu item does not have any text to show on the status bar, return false.

	Performing the command:
		pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
		pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to execute.

	@param pcmd Pointer to the command information.

	@return True if the command is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::CmdFltrFmtWrtSys(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_cid == kcidExpFindFmtWs);

	AfLpInfo * plpi = m_prmwMain->GetLpInfo();
	AssertPtr(plpi);
	ILgWritingSystemFactoryPtr qwsf;
	plpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);

	int ma = pcmd->m_rgn[0];
	if (ma == AfMenuMgr::kmaExpandItem)
	{
		HMENU hmenu = (HMENU)pcmd->m_rgn[1];
		int imni = pcmd->m_rgn[2];
		int & cmniAdded = pcmd->m_rgn[3];
		int cws;
		CustViewDaPtr qcvd;
		plpi->GetDataAccess(&qcvd);
		AssertPtr(qcvd);
		qcvd->get_WritingSystemsOfInterest(0, NULL, &cws);
		Vector<int> vwsT;
		vwsT.Resize(cws, true);
		qcvd->get_WritingSystemsOfInterest(cws, vwsT.Begin(), &cws);
		// Compare this code with WpMainWnd::OnToolBarButtonAdded, case kcidFmttbWrtgSys.
		// Todo 1350 (JohnT): allow code to be shared, or update both when we support
		// multiple old writing systems.
		Vector<StrApp> vstrbNames;
		m_vws.Clear();
		for (int iws = 0; iws < cws; iws++)
		{
			StrApp strb;
			IWritingSystemPtr qws;
			CheckHr(qwsf->get_EngineOrNull(vwsT[iws], &qws));
			if (qws)
			{
				SmartBstr sbstr;
				CheckHr(qws->get_UiName(m_prmwMain->UserWs(), &sbstr));
				strb.Assign(sbstr.Chars(), sbstr.Length());
			}
			int istrbTmp;
			for (istrbTmp = 0; istrbTmp < vstrbNames.Size(); istrbTmp++)
			{
				if (vstrbNames[istrbTmp] > strb)
					break;
			}
			vstrbNames.Insert(istrbTmp, strb);
			m_vws.Insert(istrbTmp, vwsT[iws]);
		}
		int encSelected = g_fu.GetSpecialEnc();
		for (int istrb = 0; istrb < vstrbNames.Size(); istrb++)
		{
			int cid = kcidMenuItemDynMin + istrb;
			::InsertMenu(hmenu, imni + istrb, MF_BYPOSITION, cid, vstrbNames[istrb].Chars());
			if (m_vws[istrb] == encSelected)
				::CheckMenuItem(hmenu, cid, MF_CHECKED);
		}

		// Give the overall number of items.
		Assert(cws == vstrbNames.Size());
		cmniAdded = cws;
		return true;
	}
	else if (ma == AfMenuMgr::kmaGetStatusText)
	{
		return false; // don't have any useful status text to show.
	}
	else if (ma == AfMenuMgr::kmaDoCommand)
	{
		// The user selected an expanded menu item, so perform the command now.
		//    m_rgn[1] holds the menu handle.
		//    m_rgn[2] holds the index of the selected item.
		g_fu.SetSpecialEnc(m_vws[pcmd->m_rgn[2]]);
		TssEditPtr qte = g_fu.GetTssEdit();
		if (qte)
		{
			::SetFocus(qte->Hwnd());
			g_fu.SetTssEdit(NULL);
		}
		return true;
	}
	Assert(false);
	return false;
}

/*----------------------------------------------------------------------------------------------
	Enable the Writing System submenu.

	@param cms Reference to the command state data structure controlling the Writing System
					submenu.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterDlg::CmsFltrFormat(CmdState & cms)
{
	switch (cms.Cid())
	{
	case kcidExpFindFmtWs:
		cms.Enable(true);
		break;
	case kcidFltrFmtFont:
		cms.Enable(false);
		break;
	case kcidFltrFmtStyle:
		cms.Enable(false);
		break;
	case kcidFltrFmtNone:
		cms.Enable(true);
		break;
	default:
		cms.Enable(false);
		break;
	}
	return true;
}


//:>********************************************************************************************
//:>	FwFilterFullDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterFullDlg::FwFilterFullDlg()
{
	m_rid = kridFilterFullDlg;
	m_pfltfCopy = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwFilterFullDlg::~FwFilterFullDlg()
{
	if (m_hmenuPopup)
	{
		::DestroyMenu(m_hmenuPopup);
		m_hmenuPopup = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Store initial dialog values.

	@param pfltdlg Pointer to the filter dialog pane object.
	@param pvcd Pointer to the data cache containing the filter information.
	@param pfltfCopy Pointer to the FwFilterFullDlg embedded in the filter dialog pane, or NULL
					if this is that object.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::SetDialogValues(FwFilterDlg * pfltdlg, IVwCacheDa * pvcd,
	FwFilterFullDlg * pfltfCopy)
{
	AssertPtr(pfltdlg);
	AssertPtr(pvcd);
	AssertPtrN(pfltfCopy);

	m_pfltdlg = pfltdlg;
	m_qvcd = pvcd;
	m_pfltfCopy = pfltfCopy;
}


/*----------------------------------------------------------------------------------------------
	The user has selected a different filter.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::RefreshFilterDisplay()
{
	FwFilterDlg::FilterInfo & fi = m_pfltdlg->LoadCurrentFilter(m_vvfmnColumns);
	m_hvoFilter = fi.m_hvo;
	Assert(m_hvoFilter);

	ISilDataAccessPtr qsda_vcdTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);

	HWND hwndOld = NULL;
	if (m_qfltvw)
		hwndOld = m_qfltvw->Hwnd();

	// Create the filter window.
	m_qfltvw.Attach(NewObj FilterWnd);
	m_qfltvw->Create(m_hwnd, kwidFltrTable, m_hvoFilter, m_qvcd, m_hmenuPopup, this, m_pfltdlg);
	::SetWindowPos(m_qfltvw->Hwnd(), ::GetDlgItem(m_hwnd, kctidFilterCriteria), 0, 0, 0, 0,
		SWP_NOMOVE | SWP_NOSIZE);

	// Add columns to the header control.
	HWND hwndHeader = m_qfltvw->GetHeader()->Hwnd();
	HDITEM hdi = { HDI_FORMAT | HDI_WIDTH };
	hdi.fmt = HDF_OWNERDRAW;

	// Find out how many columns are in the table and add the columns to the header control.
	// If there aren't any columns, start an empty table.
	int ccol = 0;
	HVO hvoRow0;
	if (SUCCEEDED(qsda_vcdTemp->get_VecItem(m_hvoFilter, kflidCmFilter_Rows, 0, &hvoRow0)))
		CheckHr(qsda_vcdTemp->get_VecSize(hvoRow0, kflidCmRow_Cells, &ccol));

	// Get the width of each of the columns for this filter.
	FwSettings * pfs = AfApp::GetSettings();
	AssertPtr(pfs);
	StrApp strColWidth;
	StrAppBufPath strbpSubKey(kpszFilterSubKey);
	// Use GUID instead of HVO for registry key to ensure uniqueness.
	GUID guid;
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	if (pdbi->GetGuidFromId(fi.m_hvoOld, guid))
	{
		strbpSubKey.FormatAppend(_T("\\%g"), &guid);
		pfs->GetString(strbpSubKey.Chars(), _T("ColumnWidth"), strColWidth);
	}
	const achar * psz = strColWidth.Chars();

	int dxpCol;
	int cchRead;
	for (int icol = 0; icol < ccol; icol++)
	{
		StrUtil::ParseInt(psz, StrLen(psz), &dxpCol, &cchRead);
		if (cchRead)
			psz += cchRead + 1;
		else
			dxpCol = kdxpFilterDefColumn;
		hdi.cxy = dxpCol;
		::SendMessage(hwndHeader, HDM_INSERTITEM, icol, (LPARAM)&hdi);
	}

	// Add the extra column at the end.
	hdi.cxy = kdxpFilterDefColumn;
	::SendMessage(hwndHeader, HDM_INSERTITEM, ccol, (LPARAM)&hdi);

	m_qfltvw->GetHeader()->RecalcToolTip();

	// We have to destroy the old window before we resize so that the new window (with the
	// same window ID) will be resized correctly.
	if (hwndOld)
		::DestroyWindow(hwndOld);

	Rect rcClient;
	GetClientRect(rcClient);
	OnSize(kwstRestored, rcClient.Width(), rcClient.Height());

	// Reshow the Criteria Builder dialog if it needs to be shown.
	if (m_qfltblds)
	{
		int icol = 0;
		if (ccol == 0)
			icol = -1;
		m_qfltblds->RefreshFilterDisplay(m_hvoFilter, icol, 0, 0, 0);
		::ShowWindow(m_qfltblds->Hwnd(), SW_SHOW);
	}
	// Reshow the Tips dialog if it needs to be shown.
	if (m_qfltt)
		::ShowWindow(m_qfltt->Hwnd(), SW_SHOW);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only the WM_SETFOCUS message is handled.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if message handled, otherwise whatever the superclass's FWndProc method
					returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (WM_SETFOCUS == wm)
	{
		::SetFocus(m_qfltvw->Hwnd());
		return true;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param hwndCtrl Handle passed on to the superclass method.
	@param lp Long parameter passed on the superclass method.

	@return True or false: whatever the superclass's OnInitDlg method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	Rect rcClient;
	GetClientRect(rcClient);

	// Store dialog offset values needed when we are resized later. This method is used instead
	// of hardcoding offsets because Windows expands the size of dialogs when Large Fonts is
	// selected, so hardcoded offsets will be incorrect. This dialog can be resized when it is
	// a part of the FwFilterFullShellDlg window.
	Rect rc;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidFilterField), &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	m_xpTable = rc.right + kdzpBorder;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidExpand), &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	m_xpExpandTable = rc.left;
	m_dypExpandTable = rcClient.Height() - rc.top;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidShowTips), &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	m_dxpShowTips = rcClient.right - rc.left;
	m_dypShowTips = rcClient.Height() - rc.top;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidShowBuilder), &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	m_dxpShowBuilder = rcClient.right - rc.left;

	// Create the popup menu from the menu node structures.
	Assert(m_hmenuPopup == NULL);
	m_hmenuPopup = FilterUtil::CreatePopupMenu(m_pfltdlg->GetLpInfo(), MainWindow());
	::AppendMenu(m_hmenuPopup, MF_SEPARATOR, 0, NULL);
	StrApp strRemove(kstidFltrRemoveField);
	::AppendMenu(m_hmenuPopup, MF_STRING, kcidFullFilterDelCol, strRemove.Chars());

	RefreshFilterDisplay();

	if (m_pfltfCopy)
	{
		UpdateChildWindows(m_pfltfCopy, this);
	}
	else
	{
		::ShowWindow(::GetDlgItem(m_hwnd, kctidExpand), SW_SHOW);

		FwSettings * pfs = AfApp::GetSettings();
		AssertPtr(pfs);
		DWORD dwT;

		// Load the criteria builder settings.
		if (!pfs->GetDword(kpszFilterSubKey, _T("Show CB"), &dwT))
			dwT = 1;
		ShowBuilder(dwT != 0);

		// Load the tips settings.
		if (!pfs->GetDword(kpszFilterSubKey, _T("Show Tips"), &dwT))
			dwT = 1;
		ShowTips(dwT != 0);
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	The dialog is being closed or changed to a different filter, so make sure all the changes
	get stored in the top level filter dialog.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullDlg::Apply()
{
	AssertPtr(m_pfltdlg);

	// Update the columns string for this filter.
	FwFilterDlg::FilterInfo & fi = m_pfltdlg->GetCurrentFilterInfo();
	Assert(fi.m_fs != FwFilterDlg::kfsDeleted);
	FilterUtil::BuildColumnsString(m_vvfmnColumns, fi.m_stuColInfo);

	// TODO DarrellZ: Remove this if we find a better way of finding out when a filter has
	// been modified.
	if (fi.m_fs == FwFilterDlg::kfsNormal)
		fi.m_fs = FwFilterDlg::kfsModified;

	// Save the tips settings.
	FwSettings * pfs = AfApp::GetSettings();
	AssertPtr(pfs);
	DWORD dwT = ::IsDlgButtonChecked(m_hwnd, kctidShowTips) == BST_CHECKED;
	pfs->SetDword(kpszFilterSubKey, _T("Show Tips"), dwT);

	// Save the criteria builder settings.
	dwT = ::IsDlgButtonChecked(m_hwnd, kctidShowBuilder) == BST_CHECKED;
	pfs->SetDword(kpszFilterSubKey, _T("Show CB"), dwT);

	// Hide the Criteria Builder and Tip modeless dialogs if they're visible.
	if (m_qfltblds)
		::ShowWindow(m_qfltblds->Hwnd(), SW_HIDE);
	if (m_qfltt)
		::ShowWindow(m_qfltt->Hwnd(), SW_HIDE);

	SaveColumnWidths();

	return true;
}


/*----------------------------------------------------------------------------------------------
	This method shows or hides the 'Full Filter Tips' dialog.

	@param fShow Flag whether to show the 'Full Filter Tips' dialog.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::ShowTips(bool fShow)
{
	// Record of whether the dialog is showing or not. It seems that this method can be called
	// recursively with fshow == false when closing. To avoid an infinite loop m_qfltt
	// was being cleared (to indicate that the dialog was already being closed) before calling
	// Close(). However, this was discarding a reference to the tips dialog before the dialog
	// was finished with, leading to its premature deletion (JohnL).
	static bool bShowing = false;
	if (m_pfltfCopy)
	{
		::CheckDlgButton(m_pfltfCopy->Hwnd(), kctidShowTips,
			fShow ? BST_CHECKED : BST_UNCHECKED);
	}

	if (fShow)
	{
		// Create the tip dialog.
		::CheckDlgButton(m_hwnd, kctidShowTips, BST_CHECKED);
		Assert(!m_qfltt);
		m_qfltt.Create();
		m_qfltt->SetDialogValues(this);
		m_qfltt->DoModeless(m_hwnd);
		::ShowWindow(m_qfltt->Hwnd(), SW_SHOW);
		bShowing = true;
	}
	else
	{
		if (bShowing)
		{
			bShowing = false;
			m_qfltt->Close();
			m_qfltt.Clear();
		}
		::CheckDlgButton(m_hwnd, kctidShowTips, BST_UNCHECKED);
	}
}


/*----------------------------------------------------------------------------------------------
	This method shows or hides the 'Criteria Builder' dialog.

	@param fShow Flag whether to show the 'Criteria Builder' dialog.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::ShowBuilder(bool fShow)
{
	static bool bShowing = false;	// See comment in ShowTips, above.
	if (m_pfltfCopy)
	{
		::CheckDlgButton(m_pfltfCopy->Hwnd(), kctidShowBuilder,
			fShow ? BST_CHECKED : BST_UNCHECKED);
	}

	if (fShow)
	{
		// Create the criteria builder dialog.
		AssertPtr(m_qfltvw);
		int icol = 0;
		int irow = 0;
		int ichAnchor = 0;
		int ichEnd = 0;
		// Find the column, row, and character information from the selection.
		IVwRootBox * prootb = m_qfltvw->GetRootBox();
		AssertPtr(prootb);
		IVwSelectionPtr qvwsel;
		CheckHr(prootb->get_Selection(&qvwsel));
		if (qvwsel)
		{
#ifdef DEBUG
			int cvsli;
			CheckHr(qvwsel->CLevels(true, &cvsli));
			Assert(cvsli == 3);
#endif

			VwSelLevInfo rgvsli[2];
			int ihvoRoot;
			PropTag tagTextProp;
			int cpropPrevious;
			int ws;
			ComBool fAssocPrev;
			int ihvoEnd;

			CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, 2, rgvsli, &tagTextProp,
				&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
			icol = rgvsli[0].ihvo;
			irow = rgvsli[1].ihvo;
		}

		if (m_vvfmnColumns.Size() == 0)
			icol = -1;
		::CheckDlgButton(m_hwnd, kctidShowBuilder, BST_CHECKED);
		Assert(!m_qfltblds);
		m_qfltblds.Create();
		m_qfltblds->SetDialogValues(m_pfltdlg, m_hvoFilter, m_qvcd, this, icol, irow, ichAnchor,
			ichEnd);
		m_qfltblds->DoModeless(m_hwnd);
		::ShowWindow(m_qfltblds->Hwnd(), SW_SHOW);
		bShowing = true;
	}
	else
	{
		if (bShowing)
		{
			bShowing = false;
			m_qfltblds->Close();
			m_qfltblds.Clear();
		}
		::CheckDlgButton(m_hwnd, kctidShowBuilder, BST_UNCHECKED);
	}
}


/*----------------------------------------------------------------------------------------------
	This gets called by the filter view window whenever the selection changes.
	Let the builder shell (if it exists) know about the new caret position.

	@param icol Index to the new filter column.
	@param irow Index to the new cell in that column.
	@param ichMin Character index to the beginning of the selection in the new cell string.
	@param ichEnd Character index to the end of the selection in the new cell string.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::OnEditSelChange(int icol, int irow, int ichMin, int ichEnd)
{
	AssertPtr(m_qfltvw);

	if (m_qfltblds)
	{
		m_qfltblds->OnEditSelChange(icol, irow, ichMin, ichEnd);

		// Since the builder shell might change the focus, set it back to the view window.
		::SetFocus(m_qfltvw->Hwnd());
	}
	m_qfltvw->SetEditColumn(icol);
}


/*----------------------------------------------------------------------------------------------
	This gets called by the filter builder.
	Insert/replace text at the current selection.  Pass the message on to the view window.

	@param ptss Pointer to the ITsString COM object whose contents are to replace the current
					selection in the current filter cell.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::InsertIntoCell(ITsString * ptss)
{
	AssertPtr(m_qfltvw);
	m_qfltvw->InsertIntoCell(ptss);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidExpand:
			{
				// Create and show the full filter shell dialog.
				SaveColumnWidths();
				FwFilterFullShellDlgPtr qfltfs;
				qfltfs.Create();
				FwFilterFullDlgPtr qfltf;
				qfltf.Create();
				qfltf->SetDialogValues(m_pfltdlg, m_qvcd, this);
				qfltfs->SetDialogValues(m_pfltdlg);
				qfltfs->CreateDlgShell(qfltf, m_hwnd, m_qfltvw);
				::SetFocus(m_qfltvw->Hwnd());
				return true;
			}
		case kctidShowTips:
			ShowTips(::IsDlgButtonChecked(m_hwnd, kctidShowTips) == BST_UNCHECKED);
			return true;
		case kctidShowBuilder:
			ShowBuilder(::IsDlgButtonChecked(m_hwnd, kctidShowBuilder) == BST_UNCHECKED);
			return true;
		}
		break;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Process the WM_SIZE message by moving the child controls to their new positions, and then
	letting the superclass method do the rest of the work.

	@param wst Type of sizing requested.
	@param dxp New width of the client area in pixels.
	@param dyp New height of the client area in pixels.

	@return True if the resizing message is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullDlg::OnSize(int wst, int dxp, int dyp)
{
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidExpand), NULL, m_xpExpandTable,
		dyp - m_dypExpandTable, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidShowTips), NULL, dxp - m_dxpShowTips,
		dyp - m_dypShowTips, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidShowBuilder), NULL, dxp - m_dxpShowBuilder,
		dyp - m_dypShowTips, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
	::SetWindowPos(::GetDlgItem(m_hwnd, kwidFltrTable), NULL, m_xpTable,
		0, dxp - m_xpTable, dyp - m_dypExpandTable - kdzpBorder, SWP_NOZORDER);

	return SuperClass::OnSize(wst, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------
	Add or replace a column to the filter table.

	@param icol Index of the column to add or replace in the filter table.
	@param ifmn Index into the filter's flat list of filter menu nodes, used to determine what
					field the new column refers to.
	@param fCopy Flag that two FwFilterFullDlg dialogs are open, the one embedded in the filter
					dialog pane and the resizable expanded dialog.  Since both dialogs share the
					same cache and the same filter information structure, changes in these only
					need to be made once.  These changes will not be made when fCopy is set to
					true.  But other changes (like the header control) need to be made twice.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::AddColumn(int icol, int ifmn, bool fCopy)
{
	HWND hwndHeader = m_qfltvw->GetHeader()->Hwnd();

	ISilDataAccessPtr qsda_vcdTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp));
	Assert(qsda_vcdTemp);

	int ccol = m_vvfmnColumns.Size();
	bool fInsertNewColumn = false;
	if (icol == ccol)
	{
		// We need to insert a new column.

		// First create the column in the header control.
		HDITEM hdi = { HDI_FORMAT | HDI_WIDTH };
		hdi.cxy = kdxpFilterDefColumn;
		hdi.fmt = HDF_OWNERDRAW;
		::SendMessage(hwndHeader, HDM_INSERTITEM, icol, (LPARAM)&hdi);

		// Insert the column in our vector of columns.
		FilterMenuNodeVec vfmnT;
		m_vvfmnColumns.Insert(icol, vfmnT);

		fInsertNewColumn = true;
	}

	if (!fCopy)
	{
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		ITsStringPtr qtss;
		qtsf->MakeStringRgch(NULL, 0, MainWindow()->UserWs(), &qtss);

		// Update the data cache so the table/view is updated.
		int crow;
		HVO hvoRow;
		HVO hvoCell;
		CheckHr(qsda_vcdTemp->get_VecSize(m_hvoFilter, kflidCmFilter_Rows, &crow));
		if (crow == 0)
		{
			// The table is currently empty, so create one dummy row and one column.
			Assert(ccol == 0);
			Assert(icol == 0);
			Assert(fInsertNewColumn);
			qsda_vcdTemp->MakeNewObject(kclidCmRow, m_hvoFilter, kflidCmFilter_Rows, 0, &hvoRow);
			crow = 1;
		}

		for (int irow = 0; irow < crow; irow++)
		{
			CheckHr(qsda_vcdTemp->get_VecItem(m_hvoFilter, kflidCmFilter_Rows, irow, &hvoRow));
			if (fInsertNewColumn)
				qsda_vcdTemp->MakeNewObject(kclidCmCell, hvoRow, kflidCmRow_Cells, icol, &hvoCell);
			else
				CheckHr(qsda_vcdTemp->get_VecItem(hvoRow, kflidCmRow_Cells, icol, &hvoCell));
			CheckHr(qsda_vcdTemp->SetString(hvoCell, kflidCmCell_Contents, qtss));
		}

		// If we replaced a column, we might have one or more empty rows, because the column's
		// contents were all emptied out.
		if (!fInsertNewColumn)
			m_qfltvw->RemoveEmptyRows();
	}

	FilterMenuNodeVec & vfmnFlat = MainWindow()->FlatFilterMenuNodeVec();
	FilterUtil::BuildColumnVector(vfmnFlat, ifmn, m_vvfmnColumns[icol]);
	if (!fCopy)
	{
		FwFilterDlg::FilterInfo & fi = m_pfltdlg->GetCurrentFilterInfo();
		FilterUtil::BuildColumnsString(m_vvfmnColumns, fi.m_stuColInfo);
	}

	m_qfltvw->Reconstruct();

	// This has to be done after m_vvfmnColumns gets modified.
	m_qfltvw->GetHeader()->RecalcToolTip();

	// Change the header control text for this column.
	HDITEM hdi = { HDI_TEXT };
	StrApp str;
	FilterUtil::GetColumnName(m_vvfmnColumns[icol], false, str);
	hdi.pszText = const_cast<achar *>(str.Chars());
	Header_SetItem(hwndHeader, icol, &hdi);

	if (!fCopy)
	{
		// Put the cursor in the first row in the modified/new column.
		::SetFocus(m_qfltvw->Hwnd());
		VwSelLevInfo rgvsli[2] = { { kflidCmRow_Cells, 0, icol },
			{ kflidCmFilter_Rows, 0, 0 } };
		CheckHr(m_qfltvw->GetRootBox()->MakeTextSelection(0,
			isizeof(rgvsli) / isizeof(VwSelLevInfo), rgvsli, kflidCmCell_Contents, 0, 0, 0, 0,
			0, -1, NULL, true, NULL));

		if (m_qfltblds)
			m_qfltblds->OnEditSelChange(icol, 0, 0, 0, true);
		m_qfltvw->SetEditColumn(icol);
	}

	// If we are in the expanded table, call the non-expanded AddColumn method with the fCopy
	// parameter set to true so it doesn't duplicate unneeded operations.
	if (m_pfltfCopy)
		m_pfltfCopy->AddColumn(icol, ifmn, true);
}


/*----------------------------------------------------------------------------------------------
	Remove a column from the filter table.

	@param icol Index of the column to remove from the filter table.
	@param fCopy Flag that two FwFilterFullDlg dialogs are open, the one embedded in the filter
					dialog pane and the resizable expanded dialog.  Since both dialogs share the
					same cache and the same filter information structure, changes in these only
					need to be made once.  These changes will not be made when fCopy is set to
					true.  But other changes (like the header control) need to be made twice.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::RemoveColumn(int icol, bool fCopy)
{
	ISilDataAccessPtr qsda_vcdTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);

	HWND hwndHeader = m_qfltvw->GetHeader()->Hwnd();

	Assert((uint)icol < (uint)m_vvfmnColumns.Size());

	// Remove the column from the header control.
	Header_DeleteItem(hwndHeader, icol);

	// Remove the column from our vector of columns.
	m_vvfmnColumns.Delete(icol);

	// NOTE: This has to be done after m_vvfmnColumns gets modified.
	m_qfltvw->GetHeader()->RecalcToolTip();

	if (!fCopy)
	{
		// Update the data cache so the table/view is updated.
		int crow;
		int ccol = -1;
		HVO hvoRow;
		HVO hvoCell;
		CheckHr(qsda_vcdTemp->get_VecSize(m_hvoFilter, kflidCmFilter_Rows, &crow));
		// We are going backwards, because there is a chance that we are deleting the
		// rows as we go through the table (if we're deleting the only column).
		for (int irow = crow; --irow >= 0; )
		{
			CheckHr(qsda_vcdTemp->get_VecItem(m_hvoFilter, kflidCmFilter_Rows, irow, &hvoRow));
			// Get the number of columns the first time through the loop.
			if (ccol == -1)
				CheckHr(qsda_vcdTemp->get_VecSize(hvoRow, kflidCmRow_Cells, &ccol));
			if (ccol == 1)
			{
				CheckHr(qsda_vcdTemp->DeleteObjOwner(m_hvoFilter, hvoRow, kflidCmFilter_Rows,
					irow));
			}
			else
			{
				CheckHr(qsda_vcdTemp->get_VecItem(hvoRow, kflidCmRow_Cells, icol, &hvoCell));
				CheckHr(qsda_vcdTemp->DeleteObjOwner(hvoRow, hvoCell, kflidCmRow_Cells,
					icol));
			}
		}
	}

	// Make sure we don't have any empty rows.
	m_qfltvw->RemoveEmptyRows();

	FwFilterDlg::FilterInfo & fi = m_pfltdlg->GetCurrentFilterInfo();
	FilterUtil::BuildColumnsString(m_vvfmnColumns, fi.m_stuColInfo);

	m_qfltvw->Reconstruct();

	// If we are in the expanded table, call the non-expanded RemoveColumn method with the fCopy
	// parameter set to true so it doesn't duplicate unneeded operations.
	if (m_pfltfCopy)
		m_pfltfCopy->RemoveColumn(icol, true);

	if (icol >= m_vvfmnColumns.Size())
		icol = m_vvfmnColumns.Size() - 1;
	if (m_qfltblds)
		m_qfltblds->OnEditSelChange(icol, 0, 0, true);
	m_qfltvw->SetEditColumn(icol);
}


/*----------------------------------------------------------------------------------------------
	Save the column widths for this filter.  These get stored in a registry key created under
	the Filters key based on the ID of the filter.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::SaveColumnWidths()
{
	FwFilterDlg::FilterInfo & fi = m_pfltdlg->GetCurrentFilterInfo();

	FwSettings * pfs = AfApp::GetSettings();
	AssertPtr(pfs);
	StrApp strColWidth;
	Rect rc;
	HWND hwndHeader = m_qfltvw->GetHeader()->Hwnd();
	int ccol = m_vvfmnColumns.Size();
	for (int icol = 0; icol < ccol; icol++)
	{
		::SendMessage(hwndHeader, HDM_GETITEMRECT, icol, (LPARAM)&rc);
		strColWidth.FormatAppend(_T("%d "), rc.Width());
	}
	StrAppBufPath strbpSubKey(kpszFilterSubKey);
	// Use GUID instead of HVO for registry key to ensure uniqueness.
	GUID guid;
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	if (pdbi->GetGuidFromId(fi.m_hvoOld, guid))
	{
		strbpSubKey.FormatAppend(_T("\\%g"), &guid);
		pfs->SetString(strbpSubKey.Chars(), _T("ColumnWidth"), strColWidth);
	}
}


/*----------------------------------------------------------------------------------------------
	Hide the child windows (tip window and builder shell) tied to the old full filter dialog
	and show those tied to the new full filter dialog.  One of these dialogs is the one embedded
	in the filter dialog pane, and the other dialog is the resizable expanded dialog.

	@param pfltfOld Pointer to the old filter dialog.
	@param pfltfNew Pointer to the new filter dialog.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::UpdateChildWindows(FwFilterFullDlg * pfltfOld, FwFilterFullDlg * pfltfNew)
{
	if (!pfltfOld || !pfltfNew)
		return;

	bool fShow = ::IsDlgButtonChecked(pfltfOld->Hwnd(), kctidShowTips) == BST_CHECKED;
	if (fShow)
	{
		pfltfOld->ShowTips(false);
		pfltfNew->ShowTips(fShow);
	}
	fShow = ::IsDlgButtonChecked(pfltfOld->Hwnd(), kctidShowBuilder) == BST_CHECKED;
	if (fShow)
	{
		pfltfOld->ShowBuilder(false);
		pfltfNew->ShowBuilder(fShow);
	}
}


/*----------------------------------------------------------------------------------------------
	This method is usually used for three purposes, but in this case only the last case will
	ever get called.
@line	1) If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to expand the dummy
			item by adding new items.
@line	2) If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the
			status bar string for an expanded item.
@line	3) If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
			selected an expandable menu item.

	Performing the command:
@line   pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
@line   pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to get text for.

	We are using this method to take advantage of the expandable menus functionality.
	See ${FilterUtil#CreatePopupMenu} for more information on why we're doing it this way.

	@param pcmd Pointer to the command information.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullDlg::CmdFieldPopup(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand);

	// The user selected an expanded menu item, so perform the command now.
	//    m_rgn[1] holds the menu handle.
	//    m_rgn[2] holds the index of the selected item.
	AddColumn(m_qfltvw->GetHeader()->GetContextColumn(), pcmd->m_rgn[2]);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Command handler for the Delete column command. If the user confirms deleting the column,
	then go ahead and delete it.

	@param pcmd Pointer to the command information.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullDlg::CmdDeleteCol(Cmd * pcmd)
{
	StrApp strMessage(kstidFltrDelFieldMsg);
	StrApp strFilter(kstidTlsOptFltr);
	int nT = ::MessageBox(m_hwnd, strMessage.Chars(), strFilter.Chars(),
		MB_YESNO | MB_ICONQUESTION);
	if (nT == IDYES)
		RemoveColumn(m_qfltvw->GetHeader()->GetContextColumn());
	return true;
}


/*----------------------------------------------------------------------------------------------
	Don't allow the user to delete the last (fake) column in the table.

	@param cms Reference to the command state data structure controlling whether a column can
					be deleted.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullDlg::CmsDeleteCol(CmdState & cms)
{
	int ccol = Header_GetItemCount(m_qfltvw->GetHeader()->Hwnd());
	int icol = m_qfltvw->GetHeader()->GetContextColumn();
	cms.Enable(icol < ccol - 1);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Get the type of display desired for the possibility list item / link in the given filter
	column.

	@param icol Index of the column in the filter table.

	@return Type of display desired for possibility list items.
----------------------------------------------------------------------------------------------*/
PossNameType FwFilterFullDlg::GetColumnLinkNameType(int icol)
{
	FilterMenuNode * pfmn = GetColumnVector(icol).Top()->Ptr();
	AssertPtr(pfmn);
	if (pfmn->m_proptype != kfptPossList)
		return kpntName;		// The value doesn't really matter here, but this is safe.
	// Load the possibility list from the database, if not already cached.
	PossListInfoPtr qpli;
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	plpi->LoadPossList(pfmn->m_hvo, plpi->GetPsslWsFromDb(pfmn->m_hvo), &qpli);
	return qpli->GetDisplayOption();
}

/*----------------------------------------------------------------------------------------------
	Refresh the possibility hot link string in the filter table to reflect the new
	choice for how to display possibilities in this list.

	@param pnt Specifies how the possibility is to be displayed.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::RefreshPossibilityColumn(PossNameType pnt)
{
	if (m_qfltvw)
		m_qfltvw->RefreshPossibilityColumn(pnt);
}

/*----------------------------------------------------------------------------------------------
	Show or hide child windows belonging to the full filter dialog.

	@param fShow Flag whether to show or to hide the child windows.
----------------------------------------------------------------------------------------------*/
void FwFilterFullDlg::ShowChildren(bool fShow)
{
	int nCmd = fShow ? SW_SHOW : SW_HIDE;
	if (m_qfltt)
		::ShowWindow(m_qfltt->Hwnd(), nCmd);
	if (m_qfltblds)
		::ShowWindow(m_qfltblds->Hwnd(), nCmd);
}

//:>********************************************************************************************
//:>	FwFilterFullShellDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterFullShellDlg::FwFilterFullShellDlg()
{
	m_rid = kridFilterFullShellDlg;
	m_pfltdlg = NULL;
	m_pfltvw = NULL;
	m_hwndGrip = NULL;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_Filters_tab.htm");
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwFilterFullShellDlg::~FwFilterFullShellDlg()
{
	// Child windows do not need to be destroyed:  m_hwndGrip.
}


/*----------------------------------------------------------------------------------------------
	Store initial dialog values.

	@param pfltdlg Pointer to the filter dialog pane object.
----------------------------------------------------------------------------------------------*/
void FwFilterFullShellDlg::SetDialogValues(FwFilterDlg * pfltdlg)
{
	AssertPtr(pfltdlg);

	m_pfltdlg = pfltdlg;
}


/*----------------------------------------------------------------------------------------------
	Create the dialog.

	@param pfltf Pointer to a full filter dialog to embed inside this shell dialog.
	@param hwndPar Handle to the parent window (the full filter dialog embedded inside the
					filter dialog pane).
	@param pfltvw Pointer to the filter table view window embedded inside the full filter
					dialog.

	@return Zero if hwndPar is invalid, -1 if any other error occurs, or a value indicating how
					the dialog was closed (kctidOk, kctidCancel, ...).
----------------------------------------------------------------------------------------------*/
int FwFilterFullShellDlg::CreateDlgShell(FwFilterFullDlg * pfltf, HWND hwndPar,
	FilterWnd * pfltvw)
{
	AssertPtr(pfltf);
	Assert(pfltf->GetResourceId());
	AssertPtr(pfltvw);

	m_qfltf = pfltf;
	m_pfltvw = pfltvw;

	return DoModal(hwndPar);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only the WM_SETFOCUS message is handled.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if message handled, otherwise whatever the superclass's FWndProc method
					returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullShellDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (WM_SETFOCUS == wm)
	{
		::SetFocus(m_qfltf->Hwnd());
		return true;
	}
	if (wm == WM_ACTIVATE)
	{
		if (LOWORD(wp) == WA_INACTIVE)
		{
			// Remove our special accelerator table.
			AfApp::Papp()->RemoveAccelTable(m_atid);
		}
		else
		{
			// We load this basic accelerator table so that these commands can be directed to
			// this window.  This allows the embedded Views to see the commands. Otherwise, if
			// they are translated by the main window, the main window is the 'target', and the
			// command handlers on AfVwRootSite don't work, because the root site is not a child
			// window of the main one.
			// I'm creating and destroying in Activate/Deactivate partly because I copied the
			// code from AfFindDialog, but also just to make sure this accel table can't be
			// accidentally used for other windows.
			m_atid = AfApp::Papp()->LoadAccelTable(kridAccelBasic, 0, m_hwnd);
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param hwndCtrl Handle passed on to the superclass method.
	@param lp Long parameter passed on the superclass method.

	@return False if an error occurs, or whatever the superclass's OnInitDlg method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullShellDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AssertPtr(m_pfltdlg);

	FwFilterDlg::FilterInfo & fi = m_pfltdlg->GetCurrentFilterInfo();
	StrApp str = fi.m_stuName;
	str.Append(" - ");
	str.AppendLoad(kstidFltrFullBldrTitle);

	::SetWindowText(m_hwnd, str.Chars());

	HICON hicon = ImageList_ExtractIcon(NULL, g_fu.GetImageList(), 1);
	::SendMessage(m_hwnd, WM_SETICON, ICON_SMALL, (LPARAM)hicon);
	BOOL fSuccess;
	fSuccess = ::DestroyIcon(hicon);
	Assert(fSuccess);

	// Create the gripper control.
	m_hwndGrip = ::CreateWindow(_T("SCROLLBAR"), NULL,
		WS_CHILD | WS_VISIBLE | SBS_SIZEGRIP | SBS_SIZEBOX | SBS_SIZEBOXBOTTOMRIGHTALIGN,
		0, 0, 0, 0, m_hwnd, NULL, NULL, NULL);

	// Create the embedded FwFilterFullDlg.
	m_qfltf->DoModeless(m_hwnd);
	::SetWindowPos(m_qfltf->Hwnd(), HWND_TOP, 0, 0, 0, 0,
		SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	if (!m_qfltf->SetActive())
		return false;

	FilterWnd * pfltvw = m_qfltf->GetFilterWnd();
	AssertPtr(pfltvw);
	pfltvw->CopySelection(m_pfltvw);

	Rect rc;
	GetClientRect(rc);
	OnSize(kwstRestored, rc.Width(), rc.Height());

	::SetFocus(pfltvw->Hwnd());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	The user has decided to close the shell dialog.

	@param fClose Flag whether to close the dialog: essentially ignored in this case.

	@return True if the control values are successfully saved into member variables, otherwise
					false.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullShellDlg::OnApply(bool fClose)
{
	AssertObj(m_qfltf);
	m_qfltf->PreCloseCopy();
	return SuperClass::OnApply(fClose);
}


/*----------------------------------------------------------------------------------------------
	The user has decided to close the shell dialog.

	@return Always true (if you trace it out).
----------------------------------------------------------------------------------------------*/
bool FwFilterFullShellDlg::OnCancel()
{
	AssertObj(m_qfltf);
	m_qfltf->PreCloseCopy();
	return SuperClass::OnCancel();
}


/*----------------------------------------------------------------------------------------------
	Resize/move all the child windows in response to the  WM_SIZE message.

	@param wst Flag specifying the type of resizing requested (not used in this method).
	@param dxp New width of the client area in pixesl.
	@param dyp New height of the client area in pixels.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterFullShellDlg::OnSize(int wst, int dxp, int dyp)
{
	Rect rc;

	::GetWindowRect(m_hwndGrip, &rc);
	int dxpGripper = rc.Width();
	::MoveWindow(m_hwndGrip, dxp - dxpGripper, dyp - rc.Height(), dxpGripper, rc.Height(),
		true);
	::InvalidateRect(m_hwndGrip, NULL, true);

	HWND hwndHelp = ::GetDlgItem(m_hwnd, kctidHelp);
	::GetWindowRect(hwndHelp, &rc);
	int xpHelp = dxp - rc.Width() - kdzpMargin - dxpGripper;
	int ypButton = dyp - rc.Height() - kdzpMargin;
	::SetWindowPos(hwndHelp, NULL, xpHelp, ypButton, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
	::InvalidateRect(hwndHelp, NULL, true);
	HWND hwndClose = ::GetDlgItem(m_hwnd, kctidOk);
	int xpClose = xpHelp - rc.Width() - kdzpMargin;
	::SetWindowPos(hwndClose, NULL, xpClose, ypButton, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
	::InvalidateRect(hwndClose, NULL, true);

	int xpCriteria = kdzpMargin;
	int ypCriteria = kdzpMargin;
	int dxpCriteria = dxp - (kdzpMargin * 2);
	int dypCriteria = ypButton - (kdzpMargin * 2);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidFilterCriteria), NULL, xpCriteria, ypCriteria,
		dxpCriteria, dypCriteria, SWP_NOZORDER);

	const int kdyCriteriaOffset = 10;
	int ypFilterWnd = ypCriteria + kdyCriteriaOffset + kdzpMargin;
	int dypFilterWnd = dypCriteria - kdyCriteriaOffset - (kdzpMargin * 2);
	::SetWindowPos(m_qfltf->Hwnd(), NULL, xpCriteria + kdzpMargin, ypFilterWnd,
		dxpCriteria - (kdzpMargin * 2) - 2, dypFilterWnd, SWP_NOZORDER);

	return true;
}


//:>********************************************************************************************
//:>	FwFilterHeader methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterHeader::FwFilterHeader()
{
	m_hmenuPopup = NULL;
	m_hwndToolTip = NULL;
	m_pfltf = NULL;
	m_pfltdlg = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwFilterHeader::~FwFilterHeader()
{
#ifdef TimP_2002_10_Invalid
	// It appears tooltip "windows" should not be "DestroyWindow"ed.
	// This DestroyWindow call cause an error that GetLastError reports as "1400".
	if (m_hwndToolTip)
	{
		::DestroyWindow(m_hwndToolTip);
		m_hwndToolTip = NULL;
	}
#endif
}


/*----------------------------------------------------------------------------------------------
	Create the header that will be embedded in the view window (FilterWnd).

	@param hwndPar Handle to the parent window.
	@param wid Child window identifier to use for the header.
	@param rc Coordinates of a rectangle that the header control will occupy.
	@param hmenuPopup Handle to the popup menu that allows choosing what a column of the filter
					operates on.
	@param pfltdlg Pointer to the filter dialog pane object.
----------------------------------------------------------------------------------------------*/
void FwFilterHeader::Create(HWND hwndPar, int wid, Rect & rc, HMENU hmenuPopup,
	FwFilterDlg * pfltdlg)
{
	AssertPtr(pfltdlg);

	INITCOMMONCONTROLSEX iccex = { sizeof(iccex), ICC_LISTVIEW_CLASSES };
	::InitCommonControlsEx(&iccex);

	// We use this as the name of the header window so that the hotkey will set focus to
	// the header window. When this happens, we show the popup menu.
	HWND hwndT = ::CreateWindowEx(0, WC_HEADER, NULL, WS_CHILD | HDS_BUTTONS | HDS_FULLDRAG |
		HDS_HORZ, 0, 0, 0, 0, hwndPar, (HMENU)wid, 0, 0);
	SubclassHwnd(hwndT);
	Assert(m_hwnd == hwndT);

	::SendMessage(m_hwnd, WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT), true);

	// Set the size, position, and visibility of the header control.
	HDLAYOUT hdl;
	WINDOWPOS wp;
	hdl.prc = &rc;
	hdl.pwpos = &wp;
	::SendMessage(m_hwnd, HDM_LAYOUT, 0, (LPARAM)&hdl);
	::SetWindowPos(m_hwnd, wp.hwndInsertAfter, wp.x, wp.y, wp.cx, wp.cy,
		wp.flags | SWP_SHOWWINDOW);

	m_hmenuPopup = hmenuPopup;

	// Create a tooltip control.  This will be initialized in RecalcToolTip.
	m_hwndToolTip = ::CreateWindowEx(0, TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP | TTS_NOPREFIX,
		0, 0, 0, 0, m_hwnd, 0, ModuleEntry::GetModuleHandle(), NULL);

	m_pfltdlg = pfltdlg;
	m_pfltf = dynamic_cast<FwFilterFullDlg *>(AfWnd::GetAfWnd(::GetParent(hwndPar)));
	AssertPtr(m_pfltf);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	If appropriate, messages are forwarded to the the tooltip window as well.  Only the
	WM_LBUTTONDOWN message is handled, and then only when the user clicked on one of the header
	buttons.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if message handled, otherwise whatever the superclass's FWndProc method
					returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterHeader::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (m_hwndToolTip)
	{
		// Forward messages to the tooltip window.
		MSG msg = { m_hwnd, wm, wp, lp };
		::SendMessage(m_hwndToolTip, TTM_RELAYEVENT, 0, (LPARAM)&msg);
	}

	if (wm == WM_LBUTTONDOWN)
	{
		s_icolContext = -1;

		HDHITTESTINFO hdhti;
		hdhti.pt = MakePoint(lp);
		::SendMessage(m_hwnd, HDM_HITTEST, 0, (LPARAM)&hdhti);

		if (hdhti.flags & HHT_ONHEADER)
		{
			// The user clicked on one of the header buttons, so show the field popup menu.
			ShowPopupMenu(hdhti.iItem);
			return true;
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	The header uses owner draw techniques to draw each item.

	@param pdis Pointer to the information needed to paint an owner-drawn control or menu item.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterHeader::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	HDC hdc = pdis->hDC;
	Rect rcText(pdis->rcItem);
	AfGfx::FillSolidRect(hdc, rcText, ::GetSysColor(COLOR_3DFACE));

	// Draw the border.
	if (pdis->itemState & ODS_SELECTED)
	{
		::DrawEdge(hdc, &pdis->rcItem, EDGE_SUNKEN, BF_RECT);
		rcText.left++;
		rcText.top++;
	}
	else
	{
		::DrawEdge(hdc, &pdis->rcItem, EDGE_RAISED, BF_RECT);
	}

	// Draw the text.
	rcText.left += kdxpMargin;
	rcText.right -= kdxpMargin * 2 + 9; // The 9 is the width of the wedge plus spacing.
	StrApp strCaption;
	FilterUtil::GetFieldCaption(hdc, rcText, m_pfltdlg, pdis->itemID, strCaption, true);
	::DrawText(hdc, strCaption.Chars(), strCaption.Length(), &rcText,
		DT_LEFT | DT_VCENTER | DT_SINGLELINE | DT_END_ELLIPSIS);

	// Draw the down arrow.
	{
		PenWrap xpwr(PS_SOLID, 0, ::GetSysColor(COLOR_BTNTEXT), hdc);
		int xpWedge = rcText.right + kdxpMargin;
		int ypWedge = rcText.top + (rcText.Height() - 4) / 2;
		::MoveToEx(hdc, xpWedge, ypWedge, NULL);
		::LineTo(hdc, xpWedge + 7, ypWedge);
		::MoveToEx(hdc, ++xpWedge, ++ypWedge, NULL);
		::LineTo(hdc, xpWedge + 5, ypWedge);
		::MoveToEx(hdc, ++xpWedge, ++ypWedge, NULL);
		::LineTo(hdc, xpWedge + 3, ypWedge);
		::MoveToEx(hdc, ++xpWedge, ++ypWedge, NULL);
		::LineTo(hdc, xpWedge + 1, ypWedge);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Recalculate the size and location of each of the tools that are stored in the tooltip
	attached to the header control. This is necessary whenever any of the column widths have
	changed. Right now it deletes all the old tools and recreates new tools based on the new
	column widths. If this gets too slow, it could be optimized by changing the size and
	position of the tools including and following the column the user resized.
----------------------------------------------------------------------------------------------*/
void FwFilterHeader::RecalcToolTip()
{
	Rect rc;
	TOOLINFO ti = { isizeof(ti) };
	ti.hwnd = m_hwnd;
	ti.hinst = ModuleEntry::GetModuleHandle();

	// Clean out all the old tools.
	int ctools = ::SendMessage(m_hwndToolTip, TTM_GETTOOLCOUNT, 0, 0);
	for (int itool = 0; itool < ctools; itool++)
	{
		ti.uId = itool;
		::SendMessage(m_hwndToolTip, TTM_DELTOOL, 0, (LPARAM)&ti);
	}

	// Add the new tools.
	StrApp str;
	int ccol = Header_GetItemCount(m_hwnd);
	for (int icol = 0; icol < ccol; icol++)
	{
		Header_GetItemRect(m_hwnd, icol, &rc);
		if (icol < ccol - 1)
			FilterUtil::GetColumnName(m_pfltf->GetColumnVector(icol), true, str);
		else
			str.Load(kstidFltrChooseFieldNHK);
		ti.lpszText = const_cast<achar *>(str.Chars());
		ti.rect = rc;
		ti.uId = icol;
		::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);
	}
}


/*----------------------------------------------------------------------------------------------
	Show the popup menu at the given column position.  If icol < 0, the popup menu will appear
	underneath the 'Choose a field' column.

	@param icol Index of the column where the user wants to select a field with the popup menu.
----------------------------------------------------------------------------------------------*/
void FwFilterHeader::ShowPopupMenu(int icol)
{
	AssertPtr(m_pfltf);

	if (icol < 0)
		icol = Header_GetItemCount(m_hwnd) - 1;
	Assert(icol >= 0);

	Rect rcItem;
	Header_GetItemRect(m_hwnd, icol, &rcItem);
	Point pt(rcItem.left, rcItem.bottom);
	::ClientToScreen(m_hwnd, &pt);

	// Make sure the menu shows within the visible part of the header control. This is in case
	// the user clicks on an item whose left side is not visible. We get the parent's rectangle
	// because the header control is moved around when the client area of the parent is
	// scrolled, so it's left coordinate will always be left of pt.x. We add SM_CXEDGE because
	// the parent window has the WS_EX_CLIENTEDGE extended style on it, and we want the menu
	// to show up inside of the border.
	Rect rc;
	::GetWindowRect(::GetParent(m_hwnd), &rc);
	pt.x = Max((int)pt.x, (int)(rc.left + ::GetSystemMetrics(SM_CXEDGE)));

	// Show the popup-menu. Send the WM_COMMAND message to the full filter dialog.
	s_icolContext = icol;
	// Calling this method adds our command handler for expanded menu items. The reason for
	// this is discussed up in FilterUtil::CreatePopupMenu.
	AfApp::GetMenuMgr()->SetMenuHandler(kcidFullFilterPopupMenu);
	::TrackPopupMenu(m_hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0,
		m_pfltf->Hwnd(), NULL);
	::SetFocus(::GetParent(m_hwnd));
}


//:>********************************************************************************************
//:>	FwFilterPromptDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterPromptDlg::FwFilterPromptDlg()
{
	m_rid = kridFilterPromptDlg;
	m_pszHelpUrl = _T("Basic_Tasks/Filtering/Generalized_Filter.htm");
	m_hvoFilter = NULL;
	m_fft = kfftNone;
	m_fSubitems = false;
	m_ws = 0;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwFilterPromptDlg::~FwFilterPromptDlg()
{
}


/*----------------------------------------------------------------------------------------------
	Store initial dialog values.

	@param pszPrompt Pointer to the prompt string.
	@param plpi Pointer to the application language project information.
	@param hvoFilter Database ID of the filter.
	@param vfmn Reference to the vector of filter menu nodes that define the item being
					filtered on.
----------------------------------------------------------------------------------------------*/
void FwFilterPromptDlg::SetDialogValues(wchar * pszPrompt, AfLpInfo * plpi, HVO hvoFilter,
	FilterMenuNodeVec & vfmn)
{
	AssertPsz(pszPrompt);
	AssertPtr(plpi);
	Assert(hvoFilter);

	m_stuPrompt = pszPrompt;
	m_qlpi = plpi;
	m_hvoFilter = hvoFilter;
	m_vfmn = vfmn;
}


/*----------------------------------------------------------------------------------------------
	Retrieve dialog values after the user has closed the dialog.

	@param pptss Address of a pointer to an ITsString COM object that contains the value the
					user wants to filter on.
----------------------------------------------------------------------------------------------*/
void FwFilterPromptDlg::GetDialogValues(ITsString ** pptss)
{
	AssertPtr(pptss);
	*pptss = m_qtss;
	AddRefObj(*pptss);
}


/*----------------------------------------------------------------------------------------------
	Initialize the prompt dialog in response to the WM_INITDIALOG message.
	The dialog will be closed before the user sees anything if the condition of the simple
	filter is 'Empty' or 'Not empty' because there is nothing for the user to enter in those
	two cases.

	@param hwndCtrl Handle passed on to the superclass method.
	@param lp Long parameter passed on the superclass method.

	@return True if the dialog closes before the user sees anything, false if an error occurs,
					or whatever the superclass's OnInitDlg method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterPromptDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	// Set the caption of the dialog.
	StrApp strTitle;
	FilterUtil::GetColumnName(m_vfmn, false, strTitle);
	strTitle.Append(" - ");
	strTitle.AppendLoad(kstidTlsOptFltr);
	::SetWindowText(m_hwnd, strTitle.Chars());

	ILgWritingSystemFactoryPtr qwsf;
	m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	m_qte.Create();
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	m_qte->SubclassEdit(m_hwnd, kctidFilterText, qwsf, wsUser, WS_EX_CLIENTEDGE);

	// Get the current cell text for the simple filter.
	wchar rgchText[8200];
	BYTE rgbFormat[8200];
	StrAnsi strText;
	try
	{
		IOleDbEncapPtr qode;
		IOleDbCommandPtr qodc;
		StrUni stuQuery;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		m_qlpi->GetDbInfo()->GetDbAccess(&qode);
		CheckHr(qode->CreateCommand(&qodc));

		stuQuery.Format(L"select ce.contents, ce.contents_fmt "
			L"from CmFilter_Rows as row "
			L"left outer join CmRow_Cells as cell on cell.src = row.dst "
			L"left outer join CmCell as ce on ce.id = cell.dst "
			L"where row.src = %d", m_hvoFilter);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (!fMoreRows)
			ThrowHr(E_FAIL);

		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(rgchText),
			isizeof(rgchText), &cbSpaceTaken, &fIsNull, 2));
		int cchText = (int)cbSpaceTaken / isizeof(wchar);
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(rgbFormat),
			isizeof(rgbFormat), &cbSpaceTaken, &fIsNull, 0));
		int cbFormat = (int)cbSpaceTaken;

		// Create a TsString from the cell text.
		if (cchText)
		{
			strText.Assign(rgchText, cchText);
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			CheckHr(qtsf->DeserializeStringRgch(rgchText, &cchText, rgbFormat, &cbFormat,
				&m_qtss));
		}
	}
	catch (...)
	{
		return false;
	}

	// Generate a default prompt if we don't have a user-defined one.
	StrApp strDefPrompt;
	StrApp strPrompt;
	StrApp strCondition;
	FilterUtil::GetSimpleFilterPrompt(rgchText, qwsf, m_vfmn, strDefPrompt, strCondition,
		&m_fkt, &m_fft, &m_fSubitems, &m_ws);
	if (m_fkt == kfktEmpty || m_fkt == kfktNotEmpty)
	{
		// Close the dialog. If the condition of the current filter is Empty or Not Empty,
		// there is nothing to ask the user to change on this dialog. So we just pretend the
		// user closed the dialog with the OK button before it even gets shown.
		::EndDialog(m_hwnd, 1);
		return true;
	}

	if (m_stuPrompt.Length() > 0)
		strPrompt = m_stuPrompt;
	else
		strPrompt = strDefPrompt;
	::SetDlgItemText(m_hwnd, kctidPrompt, strPrompt.Chars());

	Assert(m_vfmn.Size());
	FilterMenuNode * pfmn = *m_vfmn.Top();
	AssertPtr(pfmn);
	switch (m_fft)
	{
	case kfftDate:
		{
			HWND hwndCombo = ::GetDlgItem(m_hwnd, kctidFilterScope);
			bool fExpanded = m_fkt == kfktEmpty || m_fkt == kfktNotEmpty ||
				m_fkt == kfktEqual || m_fkt == kfktNotEqual;
			FilterUtil::FillDateScopeCombo(hwndCombo, fExpanded);
			::ShowWindow(hwndCombo, SW_SHOW);
			int iselScope = -1;
			if (strCondition.Length())
			{
				StrUni stuCondition(strCondition);
				const wchar * prgch = const_cast<wchar *>(stuCondition.Chars());
				DateKeywordLookup dkl;
				int iselT = dkl.GetIndexFromStr(prgch);
				if (iselT > -1)
				{
					FilterUtil::SkipWhiteSpace(prgch);
					if (*prgch == '(')
					{
						const wchar * prgchLim = ++prgch + 1;
						while (*prgchLim && *prgchLim != ')')
							prgchLim++;
						strCondition.Assign(prgch, prgchLim - prgch);
						iselScope = iselT;
					}
					else if (*prgch == 0)
					{
						iselScope = iselT;
						strCondition.Clear();
					}
				}
			}
			if (iselScope == -1)
			{
				iselScope = kiselMonthYear; // Default is Month and Year.
				strCondition.Clear();
			}
			::SendMessage(hwndCombo, CB_SETCURSEL, iselScope, 0);

			HWND hwndChoose = ::GetDlgItem(m_hwnd, kctidFilterChooseDate);
			FwFilterLaunchBtnPtr qflb;
			qflb.Create();
			qflb->SubclassButton(hwndChoose);

			HWND hwndEdit = ::GetDlgItem(m_hwnd, kctidFilterDate);
			if (strCondition.Length())
			{
				StrUni stu(strCondition);
				int nYear;
				int nMonth;
				int nDay;
				ParseDate(stu, iselScope, nYear, nMonth, nDay, true, m_hwnd);
				m_systime.wYear = static_cast<unsigned short>(nYear);
				m_systime.wMonth = static_cast<unsigned short>(nMonth);
				m_systime.wDay = static_cast<unsigned short>(nDay);
			}
			else
			{
				::GetLocalTime(&m_systime);
			}
			if (iselScope < kcselDateNeeded)
			{
				::ShowWindow(qflb->Hwnd(), SW_SHOW);
				::ShowWindow(hwndEdit, SW_SHOW);
				::SetFocus(hwndEdit);
				::SendMessage(hwndEdit, EM_SETSEL, 0, -1);
				achar rgchDate[MAX_PATH+1] = { 0 };
				int cch = FormatDate(iselScope, &m_systime, rgchDate, MAX_PATH);
				strCondition.Assign(rgchDate, cch);
				::SetDlgItemText(m_hwnd, kctidFilterDate, strCondition.Chars());
			}
			else
			{
				::EnableWindow(hwndEdit, false);
				::EnableWindow(::GetDlgItem(m_hwnd, kctidFilterChooseDate), false);
				::ShowWindow(qflb->Hwnd(), SW_HIDE);
				::ShowWindow(hwndEdit, SW_HIDE);
			}
		}
		break;
	case kfftText:
	case kfftRefText:
	case kfftCrossRef:
		{
			// Display the condition in the text field.
			StrUni stu(strCondition);
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ITsStringPtr qtss;
			qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_qlpi->GetDbInfo()->UserWs(),
				&qtss);
			m_qte->SetText(qtss);
			::ShowWindow(m_qte->Hwnd(), SW_SHOW);
			::SetFocus(m_qte->Hwnd());
		}
		break;
	case kfftRef:
		{
			// Display the possibility item in the subclassed FwFilterPssEdit edit window.
			FwFilterLaunchBtnPtr qflb;
			qflb.Create();
			qflb->SubclassButton(::GetDlgItem(m_hwnd, kctidFilterChooseItem));
			::ShowWindow(qflb->Hwnd(), SW_SHOW);

			HWND hwndEdit = ::GetDlgItem(m_hwnd, kctidFilterRef);
			::ShowWindow(hwndEdit, SW_SHOW);
			m_qfpe.Create();
			m_qfpe->SubclassEdit(hwndEdit, m_qlpi);

			HVO hvoPssl;
			HVO hvoPss;
			FilterUtil::StringToReference(m_qtss, m_vfmn, m_qlpi->GetDbInfo(), hvoPssl, hvoPss);
			if (pfmn->m_proptype == kfptTagList)
			{
				AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(pfmn->m_flid);
				Assert(pfmn->m_hvo == aoi.m_hvoPssl);
				if (!aoi.m_qvo)
				{
					IVwOverlayPtr qvo;
					m_qlpi->GetOverlay(pfmn->m_flid, &qvo);
					aoi.m_qvo = qvo;
				}
				m_qfpe->SetPssl(hvoPssl, hvoPss, &aoi);
			}
			else
			{
				m_qfpe->SetPssl(hvoPssl, hvoPss);
			}
			::SetFocus(hwndEdit);
			::SendMessage(hwndEdit, EM_SETSEL, 0, -1);
		}
		break;
	case kfftEnum:
		{
			// Add the enum values to the combobox and select the correct one.
			AssertPtr(pfmn);
			Assert(pfmn->m_stid);
			HWND hwndCombo = ::GetDlgItem(m_hwnd, kctidFilterEnum);
			FilterUtil::AddEnumToCombo(hwndCombo, pfmn->m_stid, strCondition.Chars());
			::ShowWindow(hwndCombo, SW_SHOW);
			::SetFocus(hwndCombo);
		}
		break;
	case kfftEnumReq:
		{
			// Add the enum values to the combobox and select the correct one.
			AssertPtr(pfmn);
			Assert(pfmn->m_stid);
			HWND hwndCombo = ::GetDlgItem(m_hwnd, kctidFilterEnum);
			FilterUtil::AddEnumToCombo(hwndCombo, pfmn->m_stid, strCondition.Chars());
			::ShowWindow(hwndCombo, SW_SHOW);
			::SetFocus(hwndCombo);
		}
		break;
	case kfftBoolean:
		{
			HWND hwndCombo = ::GetDlgItem(m_hwnd, kctidFilterEnum);
			// Add the combobox items for a Boolean field.
			StrAnsi strYes(kstidFltrYes);
			::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)strYes.Chars());
			StrAnsi strNo(kstidFltrNo);
			::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)strNo.Chars());
			if (strText.FindStrCI(strYes) == 0)
				::SendMessage(hwndCombo, CB_SETCURSEL, 0, 0);
			else
				::SendMessage(hwndCombo, CB_SETCURSEL, 1, 0);
			::ShowWindow(hwndCombo, SW_SHOW);
			::SetFocus(hwndCombo);
		}
		break;
	case kfftNumber:
		{
			// Display the condition in the number field.
			HWND hwndEdit = ::GetDlgItem(m_hwnd, kctidFilterNumber);
			::ShowWindow(hwndEdit, SW_SHOW);
			::SetDlgItemText(m_hwnd, kctidFilterNumber, strCondition.Chars());
			::SetFocus(hwndEdit);
			::SendMessage(hwndEdit, EM_SETSEL, 0, -1);
		}
		break;
	}

	// Find out how many lines it will take to show the prompt.
	HWND hwndLabel = ::GetDlgItem(m_hwnd, kctidPrompt);
	Rect rcLabel;
	Rect rc;
	::GetWindowRect(hwndLabel, &rcLabel);
	::GetWindowRect(m_qte->Hwnd(), &rc);
	int ypTextTop = rc.top - rcLabel.bottom;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidOk), &rc);
	int ypButtonTop = rc.top - rcLabel.bottom;
	::GetWindowRect(m_hwnd, &rc);
	int ypDialog = rc.Height();

	HDC hdc = ::GetDC(hwndLabel);
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
	Rect rcLabelBefore(rcLabel);
	::DrawText(hdc, strPrompt.Chars(), -1, &rcLabel, DT_CALCRECT | DT_WORDBREAK);
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	int iSuccess;
	iSuccess = ::ReleaseDC(hwndLabel, hdc);
	Assert(iSuccess);
	::SetWindowPos(hwndLabel, NULL, 0, 0, rcLabel.Width(), rcLabel.Height(),
		SWP_NOMOVE | SWP_NOZORDER);

	int dyp = rcLabel.Height() - rcLabelBefore.Height();
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rcLabel, 2);
	ypTextTop += rcLabel.bottom;
	ypButtonTop += rcLabel.bottom;
	ypDialog += dyp;

	// Update the window position of all the child controls.
	::GetWindowRect(m_qte->Hwnd(), &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	::SetWindowPos(m_qte->Hwnd(), NULL, rc.left, ypTextTop, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidFilterRef), NULL, rc.left, ypTextTop, 0, 0,
		SWP_NOSIZE | SWP_NOZORDER);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidFilterEnum), NULL, rc.left, ypTextTop, 0, 0,
		SWP_NOSIZE | SWP_NOZORDER);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidFilterScope), NULL, rc.left, ypTextTop, 0, 0,
		SWP_NOSIZE | SWP_NOZORDER);
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidFilterDate), &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidFilterDate), NULL, rc.left, ypTextTop, 0, 0,
		SWP_NOSIZE | SWP_NOZORDER);
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidFilterChooseItem), &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidFilterChooseItem), NULL, rc.left, ypTextTop, 0, 0,
		SWP_NOSIZE | SWP_NOZORDER);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidFilterChooseDate), NULL, rc.left, ypTextTop, 0, 0,
		SWP_NOSIZE | SWP_NOZORDER);

	::GetWindowRect(::GetDlgItem(m_hwnd, kctidOk), &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidOk), NULL, rc.left, ypButtonTop, 0, 0,
		SWP_NOSIZE | SWP_NOZORDER);
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidCancel), &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidCancel), NULL, rc.left, ypButtonTop, 0, 0,
		SWP_NOSIZE | SWP_NOZORDER);
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidHelp), &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
	::SetWindowPos(::GetDlgItem(m_hwnd, kctidHelp), NULL, rc.left, ypButtonTop, 0, 0,
		SWP_NOSIZE | SWP_NOZORDER);

	::GetWindowRect(m_hwnd, &rc);
	::SetWindowPos(m_hwnd, NULL, 0, 0, rc.Width(), ypDialog, SWP_NOMOVE | SWP_NOZORDER);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	The OK button was pushed, so recalculate the TsString based on the user's choices.

	@param fClose Flag whether to close the dialog: essentially ignored in this case.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool FwFilterPromptDlg::OnApply(bool fClose)
{
	m_qtss.Clear();

	ITsIncStrBldrPtr qtisb;
	qtisb.CreateInstance(CLSID_TsIncStrBldr);

	// First, set the default property for the string.
	ILgWritingSystemFactoryPtr qwsf;
	m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, wsUser));

	KeywordLookup kl;
	StrUni stuKeyword(kl.GetStrFromType(m_fkt));
	stuKeyword.Append(L" ");
	CheckHr(qtisb->Append(stuKeyword.Bstr()));

	achar rgch[MAX_PATH];

	switch (m_fft)
	{
	case kfftDate:
		{
			StrUni stuCondition;
			::GetDlgItemText(m_hwnd, kctidFilterScope, rgch, MAX_PATH);
			stuCondition.Assign(L" ");
			stuCondition.Append(rgch);
			int iselScope = ::SendMessage(::GetDlgItem(m_hwnd, kctidFilterScope), CB_GETCURSEL,
				0, 0);
			if (iselScope < kcselDateNeeded)
			{
				stuCondition.Append(L" (");
				::GetDlgItemText(m_hwnd, kctidFilterDate, rgch, MAX_PATH);
				stuCondition.Append(rgch);
				stuCondition.Append(L")");
			}
			CheckHr(qtisb->Append(stuCondition.Bstr()));
		}
		break;
	case kfftText:
	case kfftRefText:
	case kfftCrossRef:
		{
			// If any double quotes (or backslashes) are found in the cell text, insert \ in
			// front of them if there isn't one already.
			ITsStringPtr qtss;
			m_qte->GetText(&qtss);
			Vector<int> vichFix;
			SmartBstr sbstr;
			const OLECHAR * prgchText;
			int cchText;
			int ich;
			CheckHr(qtss->LockText(&prgchText, &cchText));
			for (ich = 0; ich < cchText; ++ich)
			{
				if (prgchText[ich] == '"' || prgchText[ich] == '\\')
				{
					vichFix.Push(ich);
				}
			}
			CheckHr(qtss->UnlockText(prgchText));
			if (vichFix.Size())
			{
				ITsTextPropsPtr qttp;
				ITsStrBldrPtr qtsb;
				qtss->GetBldr(&qtsb);
				while (vichFix.Size())
				{
					vichFix.Pop(&ich);
					qtsb->get_PropertiesAt(ich, &qttp);
					qtsb->ReplaceRgch(ich, ich, L"\\", 1, qttp);
				}
				qtsb->GetString(&qtss);
			}
			if (m_ws)
			{
				ILgWritingSystemFactoryPtr qwsf;
				m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
				AssertPtr(qwsf);
				SmartBstr sbstrWs;
				CheckHr(qwsf->GetStrFromWs(m_ws, &sbstrWs));
				StrUni stuWs;
				stuWs.Format(L"[%s]", sbstrWs.Chars());
				qtisb->AppendRgch(stuWs.Chars(), stuWs.Length());
			}
			qtisb->AppendRgch(L"\"", 1);
			qtisb->AppendTsString(qtss);
			qtisb->AppendRgch(L"\"", 1);
		}
		break;
	case kfftRef:
		{
			HVO hvoPss = m_qfpe->GetPss();
			GUID uid;
			if (!m_qlpi->GetDbInfo()->GetGuidFromId(hvoPss, uid))
				return false;

			// Update the string with the new object.
			StrUni stuData;
			OLECHAR * prgchData;
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtNameGuidHot;
			memmove(prgchData + 1, &uid, isizeof(uid));

			CheckHr(qtisb->SetStrPropValue(ktptObjData, stuData.Bstr()));
			wchar chObj = kchObject;
			CheckHr(qtisb->AppendRgch(&chObj, 1));
		}
		break;
	case kfftEnum:
	case kfftEnumReq:
		{
			int isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidFilterEnum), CB_GETCURSEL, 0, 0);
			FilterMenuNode * pfmn = m_vfmn[m_vfmn.Size() - 1];
#ifdef DEBUG
			if (m_fft == kfftEnum)
				Assert(pfmn->m_proptype == kfptEnumList);
			else
				Assert(pfmn->m_proptype == kfptEnumListReq);
#endif
			Assert(pfmn->m_stid);
			StrUni stuEnumTotal(pfmn->m_stid);
			const wchar * pszEnum = stuEnumTotal.Chars();
			while (isel-- > 0 && pszEnum != (wchar *)1)
				pszEnum = wcschr(pszEnum, '\n') + 1;
			if (isel != -1)
				return false;

			AssertPsz(pszEnum);
			const wchar * pszEnumLim = wcschr(pszEnum, '\n');
			if (!pszEnumLim)
				pszEnumLim = stuEnumTotal.Chars() + stuEnumTotal.Length();
			StrUni stuEnum(pszEnum, pszEnumLim - pszEnum);
			CheckHr(qtisb->Append(stuEnum.Bstr()));
		}
		break;
	case kfftBoolean:
		{
			// Replace the old keyword with the selected one.
			UINT cch = ::GetDlgItemText(m_hwnd, kctidFilterEnum, rgch, MAX_PATH);
			StrUni stuBool;
			stuBool.Assign(rgch, cch);
			stuBool.Append(L" ");
			CheckHr(qtisb->Clear());
			CheckHr(qtisb->Append(stuBool.Bstr()));
		}
		break;
	case kfftNumber:
		{
			int cch = ::GetDlgItemText(m_hwnd, kctidFilterNumber, rgch, MAX_PATH);
			// Verify that it's a valid number.
			StrApp strNumber;
			if (!ValidateNumber(rgch, cch, strNumber))
			{
				StrApp strTitle(kstidFltrNumberCap);
				StrApp strMsg(kstidFltrNumberError);
				::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_TASKMODAL);
				return false;
			}
			if (strNumber.Length())
			{
				StrApp strTitle(kstidFltrNumberCap);
				StrApp strMsg(kstidFltrMaxIntMsg);
				::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_TASKMODAL);
				::SetDlgItemText(m_hwnd, kctidFilterNumber, strNumber.Chars());
				return false;
			}
			StrUni stuCondition(rgch);
			CheckHr(qtisb->Append(stuCondition.Bstr()));
		}
		break;
	}
	if (m_fSubitems)
	{
		StrUni stuSub(L" +subitems");
		CheckHr(qtisb->Append(stuSub.Bstr()));
	}
	CheckHr(qtisb->GetString(&m_qtss));

	return SuperClass::OnApply(fClose);
}


/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool FwFilterPromptDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidFilterChooseItem)
		{
			AssertPtr(m_qfpe);

			// Get the HVO of the possibility list to show.
			Assert(m_vfmn.Size() > 0);
			FilterMenuNodePtr qfmn = *m_vfmn.Top();
			Assert(qfmn->m_proptype == kfptPossList || qfmn->m_proptype == kfptTagList);
			// Launch the chooser dialog and update the edit box if they chose a new
			// selection.
			PossChsrDlgPtr qplc;
			qplc.Create();
			// We need the list's preferred writing system (may be selector value).
			int wsList = m_qlpi->GetPsslWsFromDb(qfmn->m_hvo);
			if (qfmn->m_proptype == kfptPossList)
			{
				qplc->SetDialogValues(qfmn->m_hvo, wsList, m_qfpe->GetPss());
			}
			else if (qfmn->m_proptype == kfptTagList)
			{
				AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(qfmn->m_flid);
				Assert(qfmn->m_hvo == aoi.m_hvoPssl);
				if (!aoi.m_qvo)
				{
					IVwOverlayPtr qvo;
					m_qlpi->GetOverlay(qfmn->m_flid, &qvo);
					aoi.m_qvo = qvo;
				}
				qplc->SetDialogValues(qfmn->m_hvo, wsList, m_qfpe->GetPss(), &aoi);
			}
			if (qplc->DoModal(m_hwnd) == kctidOk)
			{
				HVO hvoPss;
				qplc->GetDialogValues(hvoPss);
				m_qfpe->SetPss(hvoPss);
			}
		}
		else if (ctidFrom == kctidFilterChooseDate)
		{
			// NOTE:  See identical code in FwFilterSimpleDlg::OnNotifyChild().
			GenSmartPtr<DatePickDlg> qfdpk;
			qfdpk.Create();
			AfDialogShellPtr qdlgShell;
			qdlgShell.Create();
			achar rgchDate[MAX_PATH] = { 0 };
			int cchDate = ::GetDlgItemText(m_hwnd, kctidFilterDate, rgchDate, MAX_PATH);
			int isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidFilterScope), CB_GETCURSEL,
				0, 0);
			if (cchDate)
			{
				StrUni stu(rgchDate, cchDate);
				int nYear;
				int nMonth;
				int nDay;
				ParseDate(stu, isel, nYear, nMonth, nDay, false, m_hwnd);
				qfdpk->m_systime.wYear = static_cast<unsigned short>(nYear);
				qfdpk->m_systime.wMonth = static_cast<unsigned short>(nMonth);
				qfdpk->m_systime.wDay = static_cast<unsigned short>(nDay);
			}
			else
			{
				::GetLocalTime(&qfdpk->m_systime);
			}
			StrApp str(kstidFilterDateTtl);
			if (qdlgShell->CreateNoHelpDlgShell(qfdpk, str.Chars(), m_hwnd) == kctidOk)
			{
				m_systime = qfdpk->m_systime;
				int cch = FormatDate(isel, &m_systime, rgchDate, MAX_PATH);
				rgchDate[cch] = 0;		// It's good to be paranoid!
				::SetWindowText(::GetDlgItem(m_hwnd, kctidFilterDate), rgchDate);
			}
		}
		break;

	case CBN_SELENDOK:
		if (ctidFrom == kctidFilterScope)
		{
			// The date scope has changed, so make sure the correct controls for the new scope
			// are enabled/disabled.
			Assert(m_fft == kfftDate);
			int iselScope = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);
			bool fEnable = iselScope < kcselDateNeeded;
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidFilterDate);
			::EnableWindow(hwnd, fEnable);
			::ShowWindow(hwnd, fEnable ? SW_SHOW : SW_HIDE);
			hwnd = ::GetDlgItem(m_hwnd, kctidFilterChooseDate);
			::EnableWindow(hwnd, fEnable);
			::ShowWindow(hwnd, fEnable ? SW_SHOW : SW_HIDE);
			if (fEnable)
			{
				// Ensure we have a correctly formatted date in the edit box.
				achar rgchDate[MAX_PATH+1] = { 0 };
				int cch = FormatDate(iselScope, &m_systime, rgchDate, MAX_PATH);
				rgchDate[cch] = 0;		// I'm still paranoid!
				::SetDlgItemText(m_hwnd, kctidFilterDate, rgchDate);
			}
		}
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only WM_ACTIVATE is processed, and even then the message is
	passed on to the superclass's FWndProc method.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True or false: whatever the superclass's FWndProc method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterPromptDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ACTIVATE)
	{
		if (LOWORD(wp) == WA_INACTIVE)
		{
			// Remove our special accelerator table.
			AfApp::Papp()->RemoveAccelTable(m_atid);
		}
		else
		{
			// We load this basic accelerator table so that these commands can be directed to
			// this window.  This allows the embedded Views to see the commands. Otherwise, if
			// they are translated by the main window, the main window is the 'target', and the
			// command handlers on AfVwRootSite don't work, because the root site is not a child
			// window of the main one.
			// I'm creating and destroying in Activate/Deactivate partly because I copied the
			// code from AfFindDialog, but also just to make sure this accel table can't be
			// accidentally used for other windows.
			m_atid = AfApp::Papp()->LoadAccelTable(kridAccelBasic, 0, m_hwnd);
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


//:>********************************************************************************************
//:>	FwFilterTipsDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only the WM_INITDIALOG message is handled, although the WM_DESTROY message is also partially
	processed.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if message handled, otherwise whatever the superclass's FWndProc method
					returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterTipsDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (WM_INITDIALOG == wm)
	{
		// In case we don't have a position saved, put this in a reasonable location.
		Rect rcProg;
		Rect rcParent;
		Rect rcScreen;
		Rect rc;
		HWND hwndPar = ::GetParent(m_hwnd);
		::SystemParametersInfo(SPI_GETWORKAREA, 0, &rcScreen, 0);
		if (::GetWindowRect(hwndPar, &rcParent) &&
			::GetWindowRect(::GetParent(hwndPar), &rcProg) &&
			::GetWindowRect(m_hwnd, &rc))
		{
			int dy = rcParent.bottom - rcParent.top;
			int dx = rcParent.right - rcParent.left;
			rcParent.top = (rcProg.top + rcProg.bottom - dy) / 2;
			rcParent.bottom = rcParent.top + dy;
			rcParent.left = (rcProg.left + rcProg.right - dx) / 2;
			rcParent.right = rcParent.left + dx;
			dy = rc.bottom - rc.top;
			dx = rc.right - rc.left;
			rc.bottom = rcParent.top - 160;		// Allow space for the Criteria Builder dialog.
			rc.top = rc.bottom - dy;
			rc.left = (rcParent.left + rcParent.right - dx) / 2;
			rc.right = rc.left + dx;
			AfGfx::EnsureVisibleRect(rc);
			WINDOWPLACEMENT wp = { isizeof(wp) };
			wp.rcNormalPosition = rc;
			::SetWindowPlacement(m_hwnd, &wp);
		}
		// Now, try to retrieve a stored position.
		LoadWindowPosition(kpszFilterSubKey, _T("Tips Position"));
		return true;
	}
	else if (WM_DESTROY == wm)
	{
		SaveWindowPosition(kpszFilterSubKey, _T("Tips Position"));
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


//:>********************************************************************************************
//:>	FwFilterNoMatchDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterNoMatchDlg::FwFilterNoMatchDlg()
{
	m_rid = kridFilterNoMatchDlg;
	m_prmwMain = NULL;
	m_hfontLarge = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwFilterNoMatchDlg::~FwFilterNoMatchDlg()
{
	if (m_hfontLarge)
	{
		AfGdi::DeleteObjectFont(m_hfontLarge);
		m_hfontLarge = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Store initial dialog values.

	@param iflt Index of the currently chosen filter, the one causing all this trouble.
	@param prmwMain Pointer to the application's main window, used for getting the language
					project information or the handle of the main window.
----------------------------------------------------------------------------------------------*/
void FwFilterNoMatchDlg::SetDialogValues(int iflt, RecMainWnd * prmwMain)
{
	AssertPtr(prmwMain);

	m_iflt = iflt;
	m_prmwMain = prmwMain;
}


/*----------------------------------------------------------------------------------------------
	Retrieve dialog values after the user has closed the dialog.
	The following explains how in interpret the value GetDialogValues returns:
@line	1) If it returns -2, the user opened the Tools\Options dialog and then pressed OK.  This
			causes the current filter to be selected again.
@line	2) If it returns -1, the user decided to turn off all filters.
@line	3) If it returns a value >= 0, this is the index of the filter that the user selected.

	@param iflt Reference to the new filter index, or a negative value as described above.
----------------------------------------------------------------------------------------------*/
void FwFilterNoMatchDlg::GetDialogValues(int & iflt)
{
	iflt = m_iflt;
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param hwndCtrl Handle passed on to the superclass method.
	@param lp Long parameter passed on the superclass method.

	@return True or false: whatever the superclass's OnInitDlg method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterNoMatchDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kridFltrSelectNew, kbtPopMenu, NULL, 0);

	AppFilterInfo & afi = m_prmwMain->GetLpInfo()->GetDbInfo()->GetFilterInfo(m_iflt);
	StrApp str = afi.m_stuName;
	::SetWindowText(m_hwnd, str.Chars());

	HICON hicon = ImageList_ExtractIcon(NULL, g_fu.GetImageList(), afi.m_fSimple ? 0 : 1);
	::SendMessage(m_hwnd, WM_SETICON, ICON_SMALL, (LPARAM)hicon);
	BOOL fSuccess;
	fSuccess = ::DestroyIcon(hicon);
	Assert(fSuccess);

	HWND hwndLabel = ::GetDlgItem(m_hwnd, kcidFltrNoEntries);
	str.Clear();
	str.Load(kstidFltrNoEntries);
	if (str.Length())
		::SetWindowText(hwndLabel, str.Chars());
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(hwndLabel, WM_SETFONT, (WPARAM)m_hfontLarge, false);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool FwFilterNoMatchDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	ClearNewFilterIndexes();
	ClearFilterViewBars();

	switch (ctidFrom)
	{
	case kridFltrTurnOff:
		m_iflt = -1;
		break;

	case kridFltrModifyFilter:
		{
			TlsOptDlgPtr qtod;
			GetTlsOptDlg(&qtod);
			::ShowWindow(m_hwnd, SW_HIDE);
			if (qtod->DoModal(m_hwnd) == kctidOk)
			{
				qtod->ClearNewFilterIndexes();
				qtod->ClearFilterViewBars();
				qtod->SaveDialogValues();
				// Saving the dialog values will update the filter, so we don't want to
				// select the same filter again when we return.
				m_iflt = -2;
				m_vifltNew = qtod->GetNewFilterIndexes();
				m_vpvwbrsFlt = qtod->GetFilterViewBars();
				Assert(m_vifltNew.Size() == m_vpvwbrsFlt.Size());
			}
		}
		break;

	case kridFltrSelectNew:
		{
			HMENU hmenuPopup = ::CreatePopupMenu();
			if (!hmenuPopup)
				ThrowHr(WarnHr(E_FAIL));

			Rect rc;
			::GetWindowRect(::GetDlgItem(m_hwnd, kridFltrSelectNew), &rc);

			SelectNewMenu(hmenuPopup);
			int nCmd = ::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON |
				TPM_RETURNCMD, rc.left, rc.bottom, 0, m_prmwMain->Hwnd(), NULL);

			::DestroyMenu(hmenuPopup);

			// If the user selected an item, we want to close this dialog.
			if (nCmd != 0)
			{
				m_iflt = nCmd - kcidMenuItemDynMin - 1;
				break;
			}
		}
		// Fall through

	default:
		return false;
	}

	// We want to close the dialog if they click any of the buttons.
	return OnApply(true);
}


//:>********************************************************************************************
//:>	FwFilterTurnOffDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterTurnOffDlg::FwFilterTurnOffDlg()
{
	m_rid = kridFilterTurnOffDlg;
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param hwndCtrl Handle passed on to the superclass method.
	@param lp Long parameter passed on the superclass method.

	@return True or false: whatever the superclass's OnInitDlg method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterTurnOffDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the icon to the system information icon.
	HICON hicon = ::LoadIcon(NULL, IDI_EXCLAMATION);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFltrInfoIcon), STM_SETICON, (WPARAM)hicon, 0);

	StrApp str;
	str.Load(kstidFltrTurnOffInfo);
	if (str.Length())
		::SetWindowText(::GetDlgItem(m_hwnd, kcidFltrTurnOffInfo), str.Chars());
	str.Clear();
	str.Load(kstidFltrTurnOffQuestion);
	if (str.Length())
		::SetWindowText(::GetDlgItem(m_hwnd, kcidFltrTurnOffQuestion), str.Chars());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


//:>********************************************************************************************
//:>	FwFilterErrorMsgDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterErrorMsgDlg::FwFilterErrorMsgDlg()
{
	m_rid = kridFilterErrorMsgDlg;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param pszCaption Dialog window caption string.
	@param pszMessage Error message string.
	@param pszHelpUrl URL of help dialog.
----------------------------------------------------------------------------------------------*/
void FwFilterErrorMsgDlg::Initialize(const achar * pszCaption, const achar * pszMessage,
	const achar * pszHelpUrl)
{
	m_strCaption = pszCaption;
	m_strMessage = pszMessage;
	m_pszHelpUrl = pszHelpUrl;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param hwndCtrl Handle passed on to the superclass method.
	@param lp Long parameter passed on the superclass method.

	@return True or false: whatever the superclass's OnInitDlg method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterErrorMsgDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the icon to the system information icon.
	HICON hicon = ::LoadIcon(NULL, IDI_INFORMATION);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFltrInfoIcon), STM_SETICON, (WPARAM)hicon, 0);

	::SetWindowText(m_hwnd, m_strCaption.Chars());
	::SetWindowText(::GetDlgItem(m_hwnd, kcidFltrErrorMsg), m_strMessage.Chars());

	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


//:>********************************************************************************************
//:>	FwFilterLaunchBtn methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Subclass an existing button window.

	@param hwndButton Handle to an existing button window.
----------------------------------------------------------------------------------------------*/
void FwFilterLaunchBtn::SubclassButton(HWND hwndButton)
{
	Assert(hwndButton);

	DWORD dwStyle = ::GetWindowLong(hwndButton, GWL_STYLE);
	::SendMessage(hwndButton, BM_SETSTYLE, dwStyle | BS_OWNERDRAW, true);

	SubclassHwnd(hwndButton);
}


/*----------------------------------------------------------------------------------------------
	Handle window painting (WM_PAINT).

	@param pdis Pointer to the information needed to paint an owner-drawn control or menu item.

	@return True.
----------------------------------------------------------------------------------------------*/
bool FwFilterLaunchBtn::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	// Note: Using a Rectangle over an existing background didn't work on one monitor.
	// Also, using a standard button didn't work properly when it was clicked.
	AssertObj(this);
	AssertPtr(pdis);
	HDC hdc = pdis->hDC;
	// Draw the button.
	Rect rc(pdis->rcItem);
	Rect rcDot;
	Rect rcT;
	AfGfx::FillSolidRect(hdc, rc, ::GetSysColor(COLOR_3DFACE));
	if (pdis->itemState & ODS_SELECTED)
	{
		::DrawEdge(hdc, &rc, EDGE_SUNKEN, BF_RECT);
		rcDot.left = rc.Width() / 2 - 4;
		rcDot.top = rc.bottom - 5;
	}
	else
	{
		::DrawEdge(hdc, &rc, EDGE_RAISED, BF_RECT);
		rcDot.left = rc.Width() / 2 - 5;
		rcDot.top = rc.bottom - 6;
	}

	// Draw the dots.
	COLORREF clr;
	rcDot.right = rcDot.left + 2;
	rcDot.bottom = rcDot.top + 2;
	if (pdis->itemState & ODS_DISABLED)
	{
		clr = ::GetSysColor(COLOR_3DHILIGHT);
		Rect rcDotT(rcDot);
		rcDotT.Offset(1, 1);
		AfGfx::FillSolidRect(hdc, rcDotT, clr);
		rcDotT.Offset(4, 0);
		AfGfx::FillSolidRect(hdc, rcDotT, clr);
		rcDotT.Offset(4, 0);
		AfGfx::FillSolidRect(hdc, rcDotT, clr);
		clr = ::GetSysColor(COLOR_GRAYTEXT);
	}
	else
	{
		clr = ::GetSysColor(COLOR_BTNTEXT);
	}
	AfGfx::FillSolidRect(hdc, rcDot, clr);
	rcDot.Offset(4, 0);
	AfGfx::FillSolidRect(hdc, rcDot, clr);
	rcDot.Offset(4, 0);
	AfGfx::FillSolidRect(hdc, rcDot, clr);

	return true;
}


//:>********************************************************************************************
//:>	FwFilterPssEdit methods.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFilterPssEdit::FwFilterPssEdit()
{
	m_hvoPss = NULL;
	m_fIgnoreChange = false;
	m_fExtraBackspace = false;
	m_pnt = kpntAbbreviation;
}


/*----------------------------------------------------------------------------------------------
	Set the possibility list and possibility that the edit control shows. If hvoPss is NULL,
	the first item in the list is selected.

	@param hvoPssl Database ID of the current possibility list.
	@param hvoPss Database ID of the chosen possibility from the list, or zero if none have
					yet been chosen.
----------------------------------------------------------------------------------------------*/
void FwFilterPssEdit::SetPssl(HVO hvoPssl, HVO hvoPss, AppOverlayInfo * paoi)
{
	m_qlpi->LoadPossList(hvoPssl, m_qlpi->GetPsslWsFromDb(hvoPssl), &m_qpli);
	if (m_qpli->GetCount())
	{
		SetPss(hvoPss, 0, paoi);
	}
	else
	{
		// The list must be empty.  I don't think we need an error message.
	}
	m_pnt = m_qpli->GetDisplayOption();
}


/*----------------------------------------------------------------------------------------------
	Set the possibility that the edit control shows. If hvoPss is NULL, the first item in the
	current possibility list is selected.

	@param hvoPss Database ID of the chosen possibility from the list, or zero if none have
					yet been chosen.
	@param ichMin Character index of the selection point in the edit control.
----------------------------------------------------------------------------------------------*/
void FwFilterPssEdit::SetPss(HVO hvoPss, int ichMin, AppOverlayInfo * paoi)
{
	Assert(m_qpli);

	int ipss = 0;
	if (!hvoPss && paoi && paoi->m_qvo)
	{
		int ctag;
		CheckHr(paoi->m_qvo->get_CTags(&ctag));
		if (ctag)
		{
			COLORREF clrFore;
			COLORREF clrBack;
			COLORREF clrUnder;
			int unt;
			ComBool fHidden;
			OLECHAR rgchGuid[isizeof(GUID)];
			CheckHr(paoi->m_qvo->GetDbTagInfo(0, &hvoPss, &clrFore, &clrBack, &clrUnder, &unt,
				&fHidden, rgchGuid));
		}
	}
	if (hvoPss)
	{
		PossListInfo * ppli = NULL;
		ipss = m_qpli->GetIndexFromId(hvoPss, &ppli);	// No Ref Count.
		if (ppli && ppli != m_qpli.Ptr())
		{
			m_qpli = ppli;
			m_pnt = m_qpli->GetDisplayOption();
		}
	}
	SetPssFromIndex(ipss, ichMin);
}


/*----------------------------------------------------------------------------------------------
	Set the possibility that the edit control shows.

	@param ipss Index of the possibility in the possibility list.
	@param ichMin Character index of the selection point in the edit control.
----------------------------------------------------------------------------------------------*/
void FwFilterPssEdit::SetPssFromIndex(int ipss, int ichMin)
{
	PossItemInfo * pii = m_qpli->GetPssFromIndex(ipss);
	if (!pii)
		return;

	m_hvoPss = pii->GetPssId();

	StrUni stu;
	pii->GetName(stu, m_pnt);
	StrApp str(stu);
	m_fIgnoreChange = true;
	::SetWindowText(m_hwnd, str.Chars());
	m_fIgnoreChange = false;
	::SendMessage(m_hwnd, EM_SETSEL, ichMin, -1);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only the WM_KEYDOWN and WM_SETFOCUS messages are processed, and even then the messages are
	passed on to the superclass's FWndProc method.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True or false: whatever the superclass's FWndProc method returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterPssEdit::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_KEYDOWN)
	{
		DWORD ichStart;
		DWORD ichStop;
		::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)&ichStart, (LPARAM)&ichStop);
		m_fExtraBackspace = ichStart != ichStop && wp == VK_BACK;
	}
	else if (wm == WM_SETFOCUS)
	{
		::SendMessage(m_hwnd, EM_SETSEL, 0, -1);
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.
	This virtual method is called from AfWnd::OnNotifyChild, applying to the window object
	that generated the message originally.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool FwFilterPssEdit::OnNotifyThis(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyThis(ctidFrom, pnmh, lnRet))
		return true;

	if (pnmh->code == EN_UPDATE && !m_fIgnoreChange)
	{
		// The user has modified the edit box, so set the string to the closest match in
		// the possibility list.
		achar rgch[MAX_PATH];
		::GetWindowText(m_hwnd, rgch, MAX_PATH);
		StrUni stu(rgch);

		AssertPtr(m_qpli);
		int ipss;
		Locale loc = m_qlpi->GetLocale(m_qlpi->ActualWs(m_qpli->GetWs()));
		if (!m_qpli->FindPss(stu.Chars(), loc, m_pnt, &ipss))
		{
			ipss = 0;
			stu.Clear();
		}
		SetPssFromIndex(ipss, stu.Length() - m_fExtraBackspace);
		return true;
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Refresh the possibility edit box to reflect the new choice for how to display
	possibilities in this list.

	@param hvo Database id of the possibility item.
	@param pnt Specifies how the possibility is to be displayed.
----------------------------------------------------------------------------------------------*/
void FwFilterPssEdit::Refresh(HVO hvo, PossNameType pnt)
{
	if (hvo != m_hvoPss || pnt == m_pnt || !m_qpli)
		return;
	m_pnt = pnt;
	PossListInfo * ppli;
	int ipss = m_qpli->GetIndexFromId(m_hvoPss, &ppli);	// No Ref Count.
	if (ppli && ppli != m_qpli.Ptr())
	{
		m_qpli = ppli;
		Assert(m_pnt == m_qpli->GetDisplayOption());
	}
	DWORD ichStart;
	DWORD ichStop;
	::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)&ichStart, (LPARAM)&ichStop);
	SetPssFromIndex(ipss, ichStart);
}


//:>********************************************************************************************
//:>	FwFilterStatic methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only the WM_GETTEXT message is handled.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if message handled, otherwise whatever the superclass's FWndProc method
					returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterStatic::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_GETTEXT)
	{
		// We don't want to return the text that is currently showing for the static control
		// because we don't want it to respond to the hotkey assigned to it. We return true
		// here so it doesn't call the default window procedure.
		return true;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


//:>********************************************************************************************
//:>	FwFilterButton methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Create the button that shows up on the FwFilterSimpleShellDlg dialog.

	@param hwndPar Handle of the parent window.
	@param wid Child window identifier to use for the button.
	@param pfltdlg Pointer to the filter dialog pane object.
----------------------------------------------------------------------------------------------*/
void FwFilterButton::Create(HWND hwndPar, int wid, FwFilterDlg * pfltdlg)
{
	AssertPtr(pfltdlg);
	m_pfltdlg = pfltdlg;

	SubclassButton(hwndPar, wid, kbtPopMenu, NULL, 0);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only the WM_GETTEXT message is handled.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if message handled, otherwise whatever the superclass's FWndProc method
					returns.
----------------------------------------------------------------------------------------------*/
bool FwFilterButton::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_GETTEXT)
	{
		// To make this button respond to a hot key that is different from the text that is
		// visible on the control, we have to override this message and return true so the
		// default window procedure doesn't get called.
		StrApp str(kctidFltrFieldLabel);
		achar * psz = reinterpret_cast<achar *>(lp);
		_tcsncpy_s(psz, wp, str.Chars(), wp - 1);
		lnRet = StrLen(psz);
		return true;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	We have some convulated way of coming up the string to display on the button.
	See ${FilterUtil#GetFieldCaption} for more information.

	@param hdc Handle to a device context for drawing the caption.
	@param rc Rectangular coordinates of the available space for drawing the caption.
	@param strCaption Reference to an output string for returning the caption.
----------------------------------------------------------------------------------------------*/
void FwFilterButton::GetCaption(HDC hdc, const Rect & rc, StrApp & strCaption)
{
	FilterUtil::GetFieldCaption(hdc, rc, m_pfltdlg, 0, strCaption, false);
}


//:>********************************************************************************************
//:>	FilterVc methods.
//:>********************************************************************************************

static DummyFactory g_fact(_T("SIL.AppCore.FilterVc"));

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FilterVc::FilterVc()
{
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FilterVc::~FilterVc()
{
}

/*----------------------------------------------------------------------------------------------
	Initialize the filter table view constructor.

	@param pdbi Pointer to the application's database information.
	@param pfltf Pointer to the complex ("full") filter editing dialog that contains the filter
					table view window.
	@param hwndHeader Handle to the window containing the header control for the filter table.
	@param dypHeader Height of the window containing the header control for the filter table.
	@param pvcd Pointer to the data cache containing the filter information.
	@param pfltvw Pointer to the filter table window used for displaying complex ("full")
					filters in the controlling filter dialog pane's embedded FwFilterFullDlg.
					The view embedded in this window is what this view constructor constructs.
----------------------------------------------------------------------------------------------*/
void FilterVc::Init(AfDbInfo * pdbi, FwFilterFullDlg * pfltf, HWND hwndHeader, int dypHeader,
	IVwCacheDa * pvcd, FilterWnd * pfltvw)
{
	AssertPtr(pdbi);
	AssertPtr(pfltf);
	Assert(hwndHeader);
	AssertPtr(pvcd);
	AssertPtr(pfltvw);

	m_hwndHeader = hwndHeader;
	m_dypHeader = dypHeader;
	m_qdbi = pdbi;
	m_pfltf = pfltf;
	m_qvcd = pvcd;
	m_pfltvw = pfltvw;
}


/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	Here a Filter is displayed by displaying its rows; and a row is displayed by displaying its
	cells; and a cell is displayed by displaying its contents string.

	@param pvwenv Pointer to a view environment COM object, used to access the filter data.
	@param hvo Database/cache ID for the filter fragment.
	@param frag Selects what fragment of a filter to display.

	@return S_OK, E_FAIL, or another appropriate error COM error value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FilterVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	switch (frag)
	{
	case kfrfiFilter:
		{// BLOCK for var skip warnings
			// This is the top level, which corresponds to an entire filter table.

			// Get the number of columns.
			// (Assumes all rows are the same, or at least the first is longest)
			int ccols = 0;
			ISilDataAccessPtr qcda;
			CheckHr(pvwenv->get_DataAccess(&qcda));
			HVO hvoRow0;
			if (SUCCEEDED(qcda->get_VecItem(hvo, kflidCmFilter_Rows, 0, &hvoRow0)))
				CheckHr(qcda->get_VecSize(hvoRow0, kflidCmRow_Cells, &ccols));

			// Get the width of the table (based on the header column widths).
			int dxpTable = 1;
			Rect rc;
			for (int icol = 0; icol < ccols; icol++)
			{
				::SendMessage(m_hwndHeader, HDM_GETITEMRECT, icol, (LPARAM)&rc);
				dxpTable += rc.Width();
			}

			HWND hwndDesk = ::GetDesktopWindow();
			HDC hdc = ::GetDC(hwndDesk);
			int ypLogPixels = ::GetDeviceCaps(hdc, LOGPIXELSY);
			int iSuccess;
			iSuccess = ::ReleaseDC(hwndDesk, hdc);
			Assert(iSuccess);

			// Set up environment properties.
			int mp = m_dypHeader * kdzmpInch / ypLogPixels;
			int mpMargin = kdxpFilterDefColumn * kdzmpInch / ypLogPixels;
			CheckHr(pvwenv->put_IntProperty(ktptMarginTop, ktpvMilliPoint, mp));
			CheckHr(pvwenv->put_IntProperty(ktptMarginTrailing, ktpvMilliPoint, mpMargin));
			CheckHr(pvwenv->put_IntProperty(ktptBackColor, ktpvDefault, kclrWhite));
			StrUni stuFont;
			int dympFont;
			g_fu.GetSystemFont(stuFont, dympFont);
			CheckHr(pvwenv->put_StringProperty(ktptFontFamily, stuFont.Bstr()));
			CheckHr(pvwenv->put_IntProperty(ktptFontSize, ktpvMilliPoint, dympFont));

			// The table uses combined width of the header cells.
			VwLength vlTab = {
				dxpTable * kdzmpInch / ypLogPixels + mpMargin, kunPoint1000 };
			CheckHr(pvwenv->OpenTable(ccols,
				vlTab,
				kdzmpInch / ypLogPixels, // border thickness about a pixel
				kvaLeft, // default alignment
				(VwFramePosition)(kvfpBelow | kvfpRhs), // border on bottom and right sides
				kvrlAll, // rules between rows and columns
				0, // no forced space between cells
				kdzmpInch * 2 / ypLogPixels, // 2 pixels padding inside cells
				false));
			// Specify column widths. The first argument is #cols, not col index.
			// The tag column only occurs at all if its width is non-zero.
			for (int icol = 0; icol < ccols; icol++)
			{
				::SendMessage(m_hwndHeader, HDM_GETITEMRECT, icol, (LPARAM)&rc);
				// The first column has to be one pixel smaller than the others for some
				// reason, probably the left border (???).
				VwLength vl = { (rc.Width() - (icol == 0)) * kdzmpInch / ypLogPixels,
								kunPoint1000 };
				CheckHr(pvwenv->MakeColumns(1, vl));
			}

			CheckHr(pvwenv->OpenTableBody());

			// Now all the contents of the table.
			CheckHr(pvwenv->AddObjVecItems(kflidCmFilter_Rows, this, kfrfiRow));

			CheckHr(pvwenv->CloseTableBody());
			CheckHr(pvwenv->CloseTable());
		}
		break;

	case kfrfiRow:
		// Add the cells in the row.
		CheckHr(pvwenv->OpenTableRow());
		CheckHr(pvwenv->AddObjVecItems(kflidCmRow_Cells, this, kfrfiCell));
		CheckHr(pvwenv->CloseTableRow());
		break;

	case kfrfiCell:
		// Add the contents of the cell.
		CheckHr(pvwenv->OpenTableCell(1,1));
		CheckHr(pvwenv->OpenMappedPara());
		CheckHr(pvwenv->AddStringProp(kflidCmCell_Contents, this));
		CheckHr(pvwenv->CloseParagraph());
		CheckHr(pvwenv->CloseTableCell());
		break;
	}

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	Return the text string that gets shown to the user when this object needs to be displayed.

	@param pguid Pointer to a database object's assigned GUID.
	@param pptss Address of a pointer to an ITsString COM object used for returning the text
					string.

	@return S_OK, E_POINTER, or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FilterVc::GetStrForGuid(BSTR bstrGuid, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrGuid);
	ChkComOutPtr(pptss);
	if (BstrLen(bstrGuid) != 8)
		ReturnHr(E_INVALIDARG);

	HVO hvo = m_qdbi->GetIdFromGuid((GUID *)bstrGuid);
	AfMainWnd * pafw = m_pfltf->MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	int wsUser = plpi->GetDbInfo()->UserWs();

	PossItemInfo * ppii = NULL;
	PossListInfoPtr qpli;
	plpi->GetPossListAndItem(hvo, wsUser, &ppii, &qpli);
	AssertPtr(qpli);
	PossNameType pnt = qpli->GetDisplayOption();
	// We really want the list's native writing system (which may be a selector rather than an
	// actual writing system).
	int wsList = plpi->GetPsslWsFromDb(qpli->GetPsslId());
	if (wsList != wsUser)
		plpi->GetPossListAndItem(hvo, wsList, &ppii, &qpli);
	AssertPtr(ppii);

	// Propagate THE PossNameType to the related FwFilterBuilderShellDlg's embedded
	// FwFilterSimpleDlg's embedded FwFilterPssEdit.
	m_pfltf->RefreshCriteriaBuilderHotLink(hvo, pnt);

	HotLinkInfo hli;
	bool fCached = m_hmhli.Retrieve(*((GUID *)bstrGuid), &hli);
	if (!fCached || pnt != hli.m_pnt)
	{
		// We have not cached this hot link before, so create the text string for the given
		// guid (which has already been translated into a PossListItem), and add it to the
		// hashmap for quick future retrieval.
		StrUni stuName;
		int wsReal = ppii->GetName(stuName, pnt);
		Assert(wsReal > 0);		// it shouldn't be a magic value any more.

		// Create the string and make it look like a hyperlink.
		ITsPropsBldrPtr qtpb;
		qtpb.CreateInstance(CLSID_TsPropsBldr);
		CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, wsReal));
		CheckHr(qtpb->SetIntPropValues(ktptUnderline, ktpvEnum, kuntSingle));
		CheckHr(qtpb->SetIntPropValues(ktptForeColor, 0, (int)RGB(0, 0, 255)));
		CheckHr(qtpb->SetIntPropValues(ktptUnderColor, 0, (int)RGB(0, 0, 255)));
		ITsTextPropsPtr qttp;
		CheckHr(qtpb->GetTextProps(&qttp));
		ITsStrBldrPtr qtsb;
		qtsb.CreateInstance(CLSID_TsStrBldr);
		CheckHr(qtsb->ReplaceRgch(0, 0, stuName.Chars(), stuName.Length(), qttp));
		CheckHr(qtsb->GetString(&hli.m_qtss));
		hli.m_pnt = pnt;

		m_hmhli.Insert(*((GUID *)bstrGuid), hli, true);		// Allow replacement.
	}

	*pptss = hli.m_qtss;
	AddRefObj(*pptss);

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	The user clicked on the object, so open up a chooser on it.

	@param pguid Pointer to a database object's assigned GUID.
	@param hvoOwner The database ID of the object.
	@param tag Identifier used to select one particular property of the object.
	@param ptss Pointer to an ITsString COM object containing a string that embeds a link to the
					object.
	@param ichObj Offset in the string to the pseudo-character that represents the object link.

	@return S_OK, E_POINTER, E_INVALIDARG, or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FilterVc::DoHotLinkAction(BSTR bstrData, HVO hvoOwner, PropTag tag,
	ITsString * ptss, int ichObj)
{
	BEGIN_COM_METHOD
	ChkComBstrArg(bstrData);
	ChkComArgPtr(ptss);
	if (!hvoOwner)
		ThrowHr(E_INVALIDARG);

	if (BstrLen(bstrData) == 9 && bstrData[0] == kodtNameGuidHot)
	{
		// This is essential since this class only sees selection changes after the event,
		// but clicking the hot link may have changed it in the middle. If we have changed
		// columns, it is vital to get the view constructor looking at the right column
		// before we call GetColumnVector below.
		m_pfltvw->ForwardEditSelChange();

		// The link should be in our cache by now because it gets put there whenever the link is
		// drawn on the screen.
		HotLinkInfo hli;
		// We skip the first character because it is the kodtNameGuidHot character.
		// The other eight characters make up the GUID.
		if (!m_hmhli.Retrieve(*((GUID *)(bstrData + 1)), &hli))
			ThrowHr(E_FAIL);

		AssertPtr(m_pfltvw);
		FilterUtil::InsertHotLink(m_pfltvw->Hwnd(), m_qdbi, m_pfltf->GetColumnVector(m_icol),
			m_qvcd, ptss, m_pfltvw->GetRootBox(), hvoOwner, tag, ichObj, true, hli.m_hvo);
		return S_OK;
	}

	CheckHr(SuperClass::DoHotLinkAction(bstrData, hvoOwner, tag, ptss, ichObj));

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}


//:>********************************************************************************************
//:>	FilterWnd methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FilterWnd::FilterWnd()
{
	m_hvoFilter = NULL;
	m_hmenuPopup = NULL;
	m_pfltf = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FilterWnd::~FilterWnd()
{
}


/*----------------------------------------------------------------------------------------------
	Create the view window.

	@param hwndPar Handle to the parent window.
	@param wid Child window identifier to use for the view window.
	@param hvoFilter Database ID of the filter.
	@param pvcd Pointer to the data cache containing the filter information.
	@param hmenuPopup Handle to the popup menu that allows choosing what a column of the filter
					operates on.
	@param pfltf Pointer to the complex ("full") filter editing dialog that contains the filter
					table view window.
	@param pfltdlg Pointer to the filter dialog pane object.
----------------------------------------------------------------------------------------------*/
void FilterWnd::Create(HWND hwndPar, int wid, HVO hvoFilter, IVwCacheDa * pvcd,
	HMENU hmenuPopup, FwFilterFullDlg * pfltf, FwFilterDlg * pfltdlg)
{
Assert(hvoFilter);
	AssertPtr(pvcd);
	AssertPtr(pfltf);
	AssertPtr(pfltdlg);

	m_qvcd = pvcd;
	m_hvoFilter = hvoFilter;
	m_hmenuPopup = hmenuPopup;
	m_pfltf = pfltf;
	m_pfltdlg = pfltdlg;
	m_qdbi = pfltdlg->GetLpInfo()->GetDbInfo();

	WndCreateStruct wcs;
	wcs.InitChild(_T("AfVwWnd"), hwndPar, wid);
	wcs.style |= WS_VISIBLE | WS_TABSTOP;
	wcs.dwExStyle = WS_EX_CLIENTEDGE;

	// Store the virtual key that should be used as the hot key for this window.
	StrApp str(kstidFltrChooseField);
	int ich = str.FindCh('&');
	m_ch = ToUpper(str[ich + 1]); // Virtual keys are uppercase.
	wcs.lpszName = str.Chars();

	CreateHwnd(wcs);
}


/*----------------------------------------------------------------------------------------------
	Set the selection in the new window to the selection in the old window.

	@param pfltvw Pointer to the old filter table view window.
----------------------------------------------------------------------------------------------*/
void FilterWnd::CopySelection(FilterWnd * pfltvw)
{
	// This is used when the filter is expanded.
	AssertPtr(pfltvw);

	m_qfltvwCopy = pfltvw;

	IVwSelectionPtr qvwsel;
	AssertPtr(pfltvw->m_qrootb);
	CheckHr(pfltvw->m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return;

#ifdef DEBUG
	int cvsli;
	CheckHr(qvwsel->CLevels(true, &cvsli));
	Assert(cvsli == 3);
#endif

	VwSelLevInfo rgvsli[2];
	int ihvoRoot;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;

	try
	{
		// Get the selection info from the selection in the other window, and use it to
		// set the selection in this window.
		CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, 2, rgvsli, &tagTextProp,
			&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
		CheckHr(m_qrootb->MakeTextSelection(ihvoRoot, 2, rgvsli, tagTextProp,
			cpropPrevious, ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, NULL, true, NULL));
		AssertPtr(m_pfltf);
		m_pfltf->OnEditSelChange(rgvsli[0].ihvo, rgvsli[1].ihvo, ichAnchor, ichEnd);
	}
	catch (...)
	{
	}
}


/*----------------------------------------------------------------------------------------------
	Go through the table and remove empty rows. If irow = -1, every row that is empty will be
	removed. Otherwise, only the row specified by irow will be removed if it is empty.

	@param irow Index into the vector of rows, or -1 to signal that all empty rows should be
					removed.

	@return True if any rows are deleted, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FilterWnd::RemoveEmptyRows(int irow)
{
	int crow;
	int ccol = -1;
	HVO hvoRow;
	HVO hvoCell;
	ITsStringPtr qtss;
	int cch;
	bool fEmptyRow;
	bool fCheckOnlyOneRow = false;
	bool fReconstruct = false;

	ISilDataAccessPtr qsda_vcdTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);

	if (irow != -1)
	{
		fCheckOnlyOneRow = true;
		crow = irow + 1;
	}
	else
	{
		CheckHr(qsda_vcdTemp->get_VecSize(m_hvoFilter, kflidCmFilter_Rows, &crow));
		crow--; // We want to skip the last (empty) row.
	}

	// Go backwards through the rows because the index will be messed up when we delete a row
	// if we go forwards.
	for (irow = crow; --irow >= 0; )
	{
		CheckHr(qsda_vcdTemp->get_VecItem(m_hvoFilter, kflidCmFilter_Rows, irow, &hvoRow));
		// Get the number of columns the first time through the loop.
		if (ccol == -1)
			CheckHr(qsda_vcdTemp->get_VecSize(hvoRow, kflidCmRow_Cells, &ccol));

		// If any of the cells in this row have a string that has a length greater than 0, the
		// row cannot be deleted.
		fEmptyRow = true;
		for (int icol = 0; icol < ccol; icol++)
		{
			CheckHr(qsda_vcdTemp->get_VecItem(hvoRow, kflidCmRow_Cells, icol, &hvoCell));
			CheckHr(qsda_vcdTemp->get_StringProp(hvoCell, kflidCmCell_Contents, &qtss));
			CheckHr(qtss->get_Length(&cch));
			if (cch > 0)
			{
				fEmptyRow = false;
				break;
			}
		}

		if (fEmptyRow)
		{
			// This row can be deleted.
			CheckHr(qsda_vcdTemp->DeleteObjOwner(m_hvoFilter, hvoRow, kflidCmFilter_Rows,
				irow));
			fReconstruct = true;
		}

		if (fCheckOnlyOneRow)
			break;
	}

	return fReconstruct;
}


/*----------------------------------------------------------------------------------------------
	Insert/replace text at the current selection.

	@param ptss Pointer to an ITsString COM object that contains the new text (or object link).
----------------------------------------------------------------------------------------------*/
void FilterWnd::InsertIntoCell(ITsString * ptss)
{
	AssertPtr(ptss);
	int cch;
	CheckHr(ptss->get_Length(&cch));
	if (cch == 0)
		return;

	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return;

	// We need to add spaces as necessary to the left and right of the inserted text, so
	// find the current selection and see which sides already have white space.

#ifdef DEBUG
	int cvsli;
	CheckHr(qvwsel->CLevels(true, &cvsli));
	Assert(cvsli == 3);
#endif

	VwSelLevInfo rgvsli[2];
	int ihvoRoot;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;

	ISilDataAccessPtr qsda_vcdTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);

	CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, 2, rgvsli, &tagTextProp,
		&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

	int icol = rgvsli[0].ihvo;
	int irow = rgvsli[1].ihvo;
	int ichSelMin = ichAnchor;
	int ichSelMax = ichEnd;
	if (ichAnchor > ichEnd)
	{
		ichSelMin = ichEnd;
		ichSelMax = ichAnchor;
	}
	// Get the current string in the current cell.
	HVO hvoRow;
	HVO hvoCell;
	ITsStringPtr qtss;
	CheckHr(qsda_vcdTemp->get_VecItem(m_hvoFilter, kflidCmFilter_Rows, irow, &hvoRow));
	CheckHr(qsda_vcdTemp->get_VecItem(hvoRow, kflidCmRow_Cells, icol, &hvoCell));
	CheckHr(qsda_vcdTemp->get_StringProp(hvoCell, kflidCmCell_Contents, &qtss));
	const OLECHAR * prgchMin;
	CheckHr(qtss->LockText(&prgchMin, &cch));

	ITsStrBldrPtr qtsb;
	qtsb.CreateInstance(CLSID_TsStrBldr);
	CheckHr(qtsb->ReplaceRgch(0, 0, L"  ", 2, NULL));
	int wsUser = m_qdbi->UserWs();					// Ensure valid ws for any inserted spaces.
	CheckHr(qtsb->SetIntPropValues(0, 2, ktptWs, 0, wsUser));
	int ichMin = 0;
	int ichLim = 2;

	// See if we need a space before the inserted text.
	if (ichSelMin > 0 && !iswspace(prgchMin[ichSelMin - 1]))
		ichMin++;

	// See if we need a space after the inserted text.
	if (ichSelMax < cch && !iswspace(prgchMin[ichSelMax]))
		ichLim--;

	qtss->UnlockText(prgchMin);

	CheckHr(qtsb->ReplaceTsString(ichMin, ichLim, ptss));

	CheckHr(qtsb->GetString(&qtss));
	CheckHr(qvwsel->ReplaceWithTsString(qtss));

	OnCellChanged();
}


/*----------------------------------------------------------------------------------------------
	The view window has been created. Create the embedded header control.
----------------------------------------------------------------------------------------------*/
void FilterWnd::PostAttach()
{
	Rect rc;
	AfWnd::GetClientRect(rc);
	// 10000 seems to be the limit on W9x machines. The header control didn't show up when
	// I (DarrellZ) used values >= SHRT_MAX.
	rc.right = 10000;

	m_qflthdr.Create();
	m_qflthdr->Create(m_hwnd, kwidFltrTableHeader, rc, m_hmenuPopup, m_pfltdlg);

	::GetWindowRect(m_qflthdr->Hwnd(), &rc);
	// Store the height of this header control for use in superclasses.
	m_dyHeader = rc.Height();

	SuperClass::PostAttach();
}


/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only the WM_GETDLGCODE message is handled, although the WM_SETFOCUS, WM_SYSKEYDOWN, and
	WM_CONTEXTMENU messages are (partially?) processed as well.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if message handled, otherwise whatever the superclass's FWndProc method
					returns.
----------------------------------------------------------------------------------------------*/
bool FilterWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_GETDLGCODE:
		lnRet = DLGC_WANTALLKEYS;
		return true;

	case WM_SETFOCUS:
		if (::GetAsyncKeyState(m_ch) & (SHRT_MAX + 1))
		{
			AssertPtr(m_qflthdr);
			// We only get this message when the user hits the hotkey for the 'Choose a field'
			// button on the header control, so when we get it, show the popup menu.
			m_qflthdr->ShowPopupMenu(-1);
		}
		else
		{
			AssertPtr(m_qrootb);
			IVwSelectionPtr qvwsel;
			CheckHr(m_qrootb->get_Selection(&qvwsel));
			if (!qvwsel)
				m_qrootb->MakeSimpleSel(true, true, false, true, NULL);
		}
		break;

	case WM_SYSKEYDOWN:
		if (::GetAsyncKeyState(m_ch) & (SHRT_MAX + 1))
		{
			AssertPtr(m_qflthdr);
			// We only get this message when the user hits the hotkey for the 'Choose a field'
			// button on the header control, so when we get it, show the popup menu.
			m_qflthdr->ShowPopupMenu(-1);
		}
		break;

	case WM_CONTEXTMENU:
		if (m_qrootb)
		{
			IVwSelectionPtr qvwsel;
			CheckHr(m_qrootb->get_Selection(&qvwsel));
			if (qvwsel)
			{
				// Show the popup menu for the view window.
				Point pt = MakePoint(lp);
				if (pt.x == -1 && pt.y == -1)
				{
					// Coordinates at -1,-1 is the indication that the user triggered the menu
					// with the keyboard, and not the mouse:
					pt.Set(0, 0);
					::ClientToScreen(m_hwnd, &pt);
				}
				HMENU hmenuPopup = ::LoadMenu(ModuleEntry::GetModuleHandle(),
					MAKEINTRESOURCE(kridFltrPopups));
				::TrackPopupMenu(::GetSubMenu(hmenuPopup, kfpmContext),
					TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y, 0, m_hwnd, NULL);
				::DestroyMenu(hmenuPopup);
			}
		}
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool FilterWnd::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	// The reason we need both of these is because Windows sends the last HDN_ITEMCHANGED
	// message after it send the HDN_ENDTRACK message. We need both of these variables to make
	// sure we catch the last size change.
	// NOTE: Windows has a setting that lets you determine whether a window is redrawn every
	// time it is resized. It seems the behavior of the header control is different depending
	// on whether this flag is set or not. So if you change the code in this method, make
	// sure you check that it works both ways. If I (DarrellZ) remember correctly, the
	// HDN_BEGINTRACK, HDN_ENDTRACK, and HDN_ITEMCHANGED messages get sent in one case, and the
	// HDN_TRACK message gets sent in the other case. That is why both ways of changing the
	// column width are included here.
	static bool s_fTracking = false;
	static bool s_fAllowTrack = false;

	const int kdxyMinHeader = 20;

	switch (pnmh->code)
	{
	case HDN_BEGINTRACK:
		if ((reinterpret_cast<NMHEADER *>(pnmh))->iItem ==
			Header_GetItemCount(pnmh->hwndFrom) - 1)
		{
			// Don't allow the last column to be resized.
			lnRet = 1;
			return true;
		}
		Assert(!s_fTracking);
		Assert(!s_fAllowTrack);
		s_fTracking = true;
		s_fAllowTrack = true;
		return true;
	case HDN_ENDTRACK:
		s_fTracking = false;
		return true;
	case HDN_ITEMCHANGING:
		if (s_fAllowTrack)
		{
			NMHEADER * pnhdr = reinterpret_cast<NMHEADER *>(pnmh);
			if (pnhdr->pitem->cxy < kdxyMinHeader)
			{
				lnRet = true;
				s_fAllowTrack = s_fTracking;
			}
		}
		return true;
	case HDN_ITEMCHANGED:
		if (s_fAllowTrack)
			OnHeaderTrack(reinterpret_cast<NMHEADER *>(pnmh));
		s_fAllowTrack = s_fTracking;
		return true;

	case HDN_TRACK:
		OnHeaderTrack(reinterpret_cast<NMHEADER *>(pnmh));
		break;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Resize the column widths of the embedded table in response to HDN_ITEMCHANGED or HDN_TRACK
	notifications.

	@param pnmh Pointer to the notification message data.
----------------------------------------------------------------------------------------------*/
void FilterWnd::OnHeaderTrack(NMHEADER * pnmh)
{
	AssertPtr(pnmh);
	AssertPtr(pnmh->pitem);
	AssertPtr(m_qrootb);
	AssertPtr(m_qflthdr);

	m_qflthdr->RecalcToolTip();

	// The header control has one more column than the table does. This is because of the
	// 'Choose a field' column.
	HWND hwndHeader = m_qflthdr->Hwnd();
	int ccol = Header_GetItemCount(hwndHeader) - 1;
	if (pnmh->iItem >= ccol)
		return;

	HDC hdc = ::GetDC(m_hwnd);
	int ypLogPixels = ::GetDeviceCaps(hdc, LOGPIXELSY);
	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);

	// Go through each column and set up the table column widths vector with the new widths.
	Rect rc;
	Vector<VwLength> vvlen;
	vvlen.Resize(ccol);
	for (int icol = 0; icol < ccol; icol++)
	{
		Header_GetItemRect(hwndHeader, icol, &rc);
		vvlen[icol].unit = kunPoint1000;
		vvlen[icol].nVal = rc.Width() * kdzmpInch / ypLogPixels;
	}

	CheckHr(m_qrootb->SetTableColWidths(vvlen.Begin(), vvlen.Size()));
	::UpdateWindow(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	Make the root box.

	@param pvg Pointer to an IVwGraphics COM object: not used by this function.
	@param pprootb Address of a pointer to the root box, used to return the newly created root
					box.
----------------------------------------------------------------------------------------------*/
void FilterWnd::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb)
{
	AssertPtrN(pwsf);
	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this));
	HVO hvo = m_hvoFilter;
	int frag = kfrfiFilter;

	// Set up a new view constructor.
	m_qfvc.Attach(NewObj FilterVc);
	Rect rc;
	::GetWindowRect(m_qflthdr->Hwnd(), &rc);
	m_qfvc->Init(m_qdbi, m_pfltf, m_qflthdr->Hwnd(), rc.Height(), m_qvcd, this);

	ISilDataAccessPtr qsda_vcdTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);

	if (pwsf)
		CheckHr(qsda_vcdTemp->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qsda_vcdTemp));

	IVwViewConstructor * pvvc = m_qfvc;
	// TODO DarrellZ: Use the stylesheet once styles can be attached. When I tried this before,
	// the ktptFontFamily and ktptFontSize properties seemed to be ignored and the boxes
	// seemed to be the wrong size, so make sure that is fixed when this code is used.
#undef STYLE_SHEET_WORKS
#ifdef STYLE_SHEET_WORKS
	AfLpInfo * plpi = m_pfltdlg->GetLpInfo();
	AssertPtr(plpi);
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, plpi->GetAfStylesheet(), 1));
#endif
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, NULL, 1));
	*pprootb = qrootb;
	(*pprootb)->AddRef();
}


/*----------------------------------------------------------------------------------------------
	Close things down.
----------------------------------------------------------------------------------------------*/
void FilterWnd::OnReleasePtr()
{
	m_qrootb->Close();
	SuperClass::OnReleasePtr();
}

/*----------------------------------------------------------------------------------------------
	Process commands. Return true if processed.
	See ${AfWnd#OnCommand} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool FilterWnd::OnCommand(int cid, int nc, HWND hctl)
{
	if (!nc && !hctl && cid >= kcidMenuItemDynMin)
	{
		Vector<FilterPatternInfo> & vfpi = g_fu.GetLanguageVariables();
		int cfpi = vfpi.Size();
		int ifpi = cid - kcidMenuItemDynMin;
		if (ifpi < cfpi)
		{
			StrUniBuf stub;
			stub.Format(L"[%s]", vfpi[ifpi].m_stuAbbrev.Chars());
			IVwSelectionPtr qvwsel;
			AssertPtr(m_qrootb);
			CheckHr(m_qrootb->get_Selection(&qvwsel));
			if (qvwsel)
			{
				int cttp;
				ComVector<ITsTextProps> vqttp;
				ComVector<IVwPropertyStore> vqvps;
				CheckHr(qvwsel->GetSelectionProps(0, NULL, NULL, &cttp));
				vqttp.Resize(cttp);
				vqvps.Resize(cttp);
				CheckHr(qvwsel->GetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin(),
					(IVwPropertyStore **)vqvps.Begin(), &cttp));
				ITsStringPtr qtss;
				ITsStrBldrPtr qtsb;
				qtsb.CreateInstance(CLSID_TsStrBldr);
				CheckHr(qtsb->ReplaceRgch(0, 0, stub.Chars(), stub.Length(),
					cttp ? vqttp[0] : NULL));
				CheckHr(qtsb->GetString(&qtss));
				qvwsel->ReplaceWithTsString(qtss);
			}
			else
			{
				for (int ich = 0; ich < stub.Length(); ++ich)
					OnChar(stub.GetAt(ich), 1, 0);
			}
			return true;
		}
	}
	return SuperClass::OnCommand(cid, nc, hctl);
}


/*----------------------------------------------------------------------------------------------
	Trap a character press to add or remove rows as needed.

	@param nChar Code of the key that has been pressed.
	@param nRepCnt Repeat count for how many consecutive times that key has been pressed.
	@param nFlags Additional flags that came in bits 16-31 of the WM_CHAR message lparam.
----------------------------------------------------------------------------------------------*/
void FilterWnd::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	if (nChar == VK_RETURN)
	{
		// Since we are trapping every keystroke, we have to pass the Enter key on to the top
		// dialog so the dialog gets closed properly.
		AssertPtr(m_pfltf);
		AfDialog * pdlg = m_pfltf->GetTopDialog();
		AssertPtr(pdlg);
		::PostMessage(pdlg->Hwnd(), WM_KEYDOWN, nChar, MAKELPARAM(1, nFlags));
		return;
	}
	else if (nChar == VK_ESCAPE)
	{
		// Since we are trapping every keystroke, we have to pass the Escape key on to the top
		// dialog so the dialog gets closed properly.
		AssertPtr(m_pfltf);
		AfDialog * pdlg = m_pfltf->GetTopDialog();
		AssertPtr(pdlg);
		::PostMessage(pdlg->Hwnd(), WM_KEYDOWN, nChar, MAKELPARAM(1, nFlags));
		return;
	}
	else if (nChar == VK_TAB)
	{
		// Go to the next/previous cell and select its contents. If we are already at the
		// boundary of the table, switch focus to the next/previous control in the tab order.
		bool fPrev = ::GetKeyState(VK_SHIFT) < 0;
		IVwSelectionPtr qvwsel;
		AssertPtr(m_qrootb);
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (qvwsel)
		{
#ifdef DEBUG
			int cvsli;
			CheckHr(qvwsel->CLevels(true, &cvsli));
			Assert(cvsli == 3);
#endif

			VwSelLevInfo rgvsli[2];
			int ihvoRoot;
			PropTag tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			ComBool fAssocPrev;
			int ihvoEnd;

			ISilDataAccessPtr qsda_vcdTemp;
			HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp);
			if (FAILED(hr))
				ThrowInternalError(E_INVALIDARG);

			try
			{
				CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, 2, rgvsli, &tagTextProp,
					&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
				int & icol = rgvsli[0].ihvo;
				int & irow = rgvsli[1].ihvo;

				int crow;
				int ccol;
				HVO hvoRow0;
				CheckHr(qsda_vcdTemp->get_VecSize(m_hvoFilter, kflidCmFilter_Rows, &crow));
				Assert(crow > 0);
				CheckHr(qsda_vcdTemp->get_VecItem(m_hvoFilter, kflidCmFilter_Rows, 0,
					&hvoRow0));
				CheckHr(qsda_vcdTemp->get_VecSize(hvoRow0, kflidCmRow_Cells, &ccol));
				Assert(ccol > 0);

				if (fPrev)
					icol--;
				else
					icol++;
				if (icol < 0)
				{
					irow--;
					icol = ccol - 1;
				}
				else if (icol >= ccol)
				{
					irow++;
					icol = 0;
				}
				if (irow >= 0 && irow < crow)
				{
					// We are still within the table, so set the focus to the specified cell
					// and select its contents.
					Assert(icol >= 0 && icol < ccol);

					// Get the number of characters in the new cell. Note that the row may have
					// changed, so we have to get the HVO of the correct row.
					HVO hvoRow;
					HVO hvoCell;
					ITsStringPtr qtss;
					CheckHr(qsda_vcdTemp->get_VecItem(m_hvoFilter, kflidCmFilter_Rows, irow,
						&hvoRow));
					CheckHr(qsda_vcdTemp->get_VecItem(hvoRow, kflidCmRow_Cells, icol,
						&hvoCell));
					CheckHr(qsda_vcdTemp->get_StringProp(hvoCell, kflidCmCell_Contents, &qtss));
					int cch = 0;
					if (qtss)
						CheckHr(qtss->get_Length(&cch));

					CheckHr(m_qrootb->MakeTextSelection(ihvoRoot, 2, rgvsli, tagTextProp,
						cpropPrevious, 0, cch, ws, fAssocPrev, -1, NULL, true, NULL));
					ScrollSelectionIntoView(NULL, kssoDefault);
					return;
				}
			}
			catch (...)
			{
			}
		}

		// We either didn't have a selection or we are at the limit of the table, so select
		// the next control in the tab order.
		HWND hwndNext;
		if (fPrev)
		{
			// Since the view window is the first control in the parent dialog, find the
			// control that is before the dialog containing the view window.
			// REVIEW DarrellZ: Why doesn't this work?!?
			//HWND hwndDlgPar = ::GetParent(m_hwnd);
			//hwndNext = ::GetNextDlgTabItem(::GetParent(hwndDlgPar), hwndDlgPar, true);
			// This is a hack to get the handle of the Delete button. If the Assert below ever
			// fires, this will have to be modified.
			hwndNext = ::GetDlgItem(::GetParent(::GetParent(m_hwnd)), kctidDeleteFilter);
			Assert(hwndNext);
		}
		else
		{
			// Get the next control in the parent dialog.
			hwndNext = ::GetNextDlgTabItem(::GetParent(m_hwnd), m_hwnd, false);
		}
		::SendMessage(::GetParent(m_hwnd), WM_NEXTDLGCTL, (WPARAM)hwndNext, true);
		return;
	}

	SuperClass::OnChar(nChar, nRepCnt, nFlags);

	// OnCellChanged
	OnCellChanged(nChar);
}


/*----------------------------------------------------------------------------------------------
	Trap a change in the selection and notify the appropriate windows of the change.

	@param xp Horizontal mouse position.
	@param yp Vertical mouse position.
	@param rcSrcRoot Coordinates of the box where the user clicked. (?)
	@param rcDstRoot Transformed coordinates of the box where the user clicked. (?)
----------------------------------------------------------------------------------------------*/
void FilterWnd::CallMouseUp(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot)
{
	SuperClass::CallMouseUp(xp, yp, rcSrcRoot, rcDstRoot);
	ForwardEditSelChange();
}


/*----------------------------------------------------------------------------------------------
	Trap a change in the selection and notify the appropriate windows of the change.

	@param chw Character that was pressed.
	@param ss Shift key status: None, Shift, Control, ShiftControl

	@return True.
----------------------------------------------------------------------------------------------*/
bool FilterWnd::CallOnExtendedKey(int chw, VwShiftStatus ss)
{
	SuperClass::CallOnExtendedKey(chw, ss);
	ForwardEditSelChange();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Figure out which column, row, and character index we are currently at and let the
	necessary windows know about it.
----------------------------------------------------------------------------------------------*/
void FilterWnd::ForwardEditSelChange()
{
	AssertPtr(m_qrootb);
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return;
	ComBool fOk;
	CheckHr(qvwsel->Commit(&fOk));

#ifdef DEBUG
	int cvsli;
	CheckHr(qvwsel->CLevels(true, &cvsli));
	Assert(cvsli == 3);
#endif

	VwSelLevInfo rgvsli[2];
	int ihvoRoot;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;

	CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, 2, rgvsli, &tagTextProp,
		&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

	int icol = rgvsli[0].ihvo;
	int irow = rgvsli[1].ihvo;

	m_pfltf->OnEditSelChange(icol, irow, ichAnchor, ichEnd);
	m_qfvc->OnEditSelChange(icol, irow, ichAnchor);
}


/*----------------------------------------------------------------------------------------------
	The cell's contents have been changed. If the cell that was changed is on the last row, we
	need to create another empty row at the bottom of the table. If the cell that was changed
	results in an empty row, that row needs to be deleted.

	@param ch Optional character that caused the changed.  Only VK_DELETE, VK_BACK, and
					kscDelForward are significant.
----------------------------------------------------------------------------------------------*/
void FilterWnd::OnCellChanged(UINT ch)
{
	int irow;
	int icol;
	int crow;
	IVwSelectionPtr qvwsel;

	VwSelLevInfo rgvsli[2];
	int ihvoRoot;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;

	ISilDataAccessPtr qsda_vcdTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda_vcdTemp));
	Assert(qsda_vcdTemp);

	try
	{
		AssertPtr(m_qrootb);
		ComBool fOk;
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
			return;
		CheckHr(qvwsel->Commit(&fOk));

#ifdef DEBUG
		int cvsli;
		CheckHr(qvwsel->CLevels(true, &cvsli));
		Assert(cvsli == 3);
#endif

		CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, 2, rgvsli, &tagTextProp,
			&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

		icol = rgvsli[0].ihvo;
		irow = rgvsli[1].ihvo;

		CheckHr(qsda_vcdTemp->get_VecSize(m_hvoFilter, kflidCmFilter_Rows, &crow));
		Assert(crow > 0);
	}
	catch (...)
	{
		return;
	}

	bool fReconstruct = false;

	Assert((uint)irow < (uint)crow);
	if (ch == kscDelForward || ch == VK_DELETE || ch == VK_BACK)
	{
		// Since we deleted something, we might have an empty row now. It doesn't matter if
		// we delete something on the last line, because it should already be empty.
		if (irow < crow - 1)
			fReconstruct = RemoveEmptyRows(irow);
	}
	else if (irow == crow - 1)
	{
		// Get the number of columns in the first row.
		HVO hvoRow0;
		int ccol;
		CheckHr(qsda_vcdTemp->get_VecItem(m_hvoFilter, kflidCmFilter_Rows, 0, &hvoRow0));
		CheckHr(qsda_vcdTemp->get_VecSize(hvoRow0, kflidCmRow_Cells, &ccol));

		// The user just typed a character in the last row, so create a new one.
		HVO hvoRow;
		qsda_vcdTemp->MakeNewObject(kclidCmRow, m_hvoFilter, kflidCmFilter_Rows, crow, &hvoRow);

		// Create cells for each column and add them to the row.
		HVO hvoDud;
		for (int icol = 0; icol < ccol; icol++)
		{
			qsda_vcdTemp->MakeNewObject(kclidCmCell, hvoRow, kflidCmRow_Cells, icol, &hvoDud);
		}

		// NOTE: revert to setting fReconstruct if this doesn't really work.
		//	ISilDataAccessPtr qsda;
		//	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda);
		//	if (FAILED(hr))
		//		ThrowInternalError(E_INVALIDARG);
		//	CheckHr(qsda->PropChanged(NULL, kpctNotifyAll, m_hvoFilter, kflidCmFilter_Rows,
		//		crow, 1, false));
		// NOTE: the lines above don't quite work correctly.
		fReconstruct = true;
	}

	if (fReconstruct)
	{
		try
		{
			Reconstruct();
			if (m_qfltvwCopy)
				m_qfltvwCopy->Reconstruct();
			CheckHr(m_qrootb->MakeTextSelection(ihvoRoot, 2, rgvsli, tagTextProp,
				cpropPrevious, ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, NULL, true, NULL));
		}
		catch (...)
		{
		}
	}

	// When we hit the space key in a reference field, we need to check to see if the previous
	// word is a 'Matches' or 'Does not match' keyword. If it is, then we throw up a chooser
	// dialog that lets the user select which possibility they want to insert there.
	if (isascii(ch) && isspace(ch))
	{
		// See if we are in a reference field.
		bool fReference = false;
		FilterMenuNodeVec & vfmn = m_pfltf->GetColumnVector(icol);
		FilterMenuNode * pfmn = vfmn[vfmn.Size() - 1];
		AssertPtr(pfmn);
		switch (pfmn->m_proptype)
		{
		case kcptOwningAtom:
		case kcptReferenceAtom:
		case kcptOwningCollection:
		case kcptReferenceCollection:
		case kcptOwningSequence:
		case kcptReferenceSequence:
		case kfptPossList:
		case kfptTagList:
		case kfptRoledParticipant:
			fReference = true;
			break;
		}

		if (fReference)
		{
			// Look at the previous word to see if it is one of the 'matches' keywords.
			// We have to also make sure that we are not enclosed in a " pair.
			HVO hvoRow;
			HVO hvoCell;
			ITsStringPtr qtss;
			CheckHr(qsda_vcdTemp->get_VecItem(m_hvoFilter, kflidCmFilter_Rows, irow, &hvoRow));
			CheckHr(qsda_vcdTemp->get_VecItem(hvoRow, kflidCmRow_Cells, icol, &hvoCell));
			CheckHr(qsda_vcdTemp->get_StringProp(hvoCell, kflidCmCell_Contents, &qtss));
			const OLECHAR * prgchMin;
			int cch;
			CheckHr(qtss->LockText(&prgchMin, &cch));

			const wchar * prgch = const_cast<wchar *>(prgchMin);
			const wchar * prgchLim = prgch + ichAnchor - 1;

			KeywordLookup kl;
			FilterKeywordType fkt;
			wchar ch;
			bool fInQuotes = false;
			FilterUtil::SkipWhiteSpace(prgch);
			while (prgch < prgchLim)
			{
				ch = *prgch;
				if (ch == '"')
				{
					// Find the close of the " pair.
					while (++prgch < prgchLim && *prgch != ch)
						;
					if (++prgch >= prgchLim)
					{
						fInQuotes = true;
						break;
					}
				}
				else
				{
					fkt = kl.GetTypeFromStr(prgch);
					if (fkt == kfktError)
					{
						while (prgch < prgchLim && !iswspace(*prgch))
							prgch++;
					}
				}
				FilterUtil::SkipWhiteSpace(prgch);
			}

			if (!fInQuotes)
			{
				// See if the last word is a 'matches' word.
				if (fkt == kfktMatches || fkt == kfktDoesNotMatch)
				{
					// Insert the link character.
					FilterUtil::InsertHotLink(m_hwnd, m_qdbi, m_pfltf->GetColumnVector(icol),
						m_qvcd, qtss, m_qrootb, hvoCell, kflidCmCell_Contents, ichAnchor,
						false);
				}
			}

			qtss->UnlockText(prgchMin);
		}
	}

	m_pfltf->OnEditSelChange(icol, irow, ichAnchor, ichEnd);
	m_qfvc->OnEditSelChange(icol, irow, ichAnchor);
}


/*----------------------------------------------------------------------------------------------
	This should keep the header control from scrolling vertically out of sight when the view
	window needs to be scrolled.

	@param dx Change in horizontal position.
	@param dy Change in vertical position.
	@param rc Reference to a coordinate rectangle that gets updated for the change in position.
----------------------------------------------------------------------------------------------*/
void FilterWnd::GetScrollRect(int dx, int dy, Rect & rc)
{
	AfWnd::GetClientRect(rc);
	if (dy)
	{
		// This will get a little more complicated if we are scrolling in both directions.
		// Basically if we are scrolling horizontally, we want the header control to scroll
		// as well. If we are scrolling vertically, we don't want it to scroll.
		Assert(dx == 0);
		Rect rcHeader;
		::GetWindowRect(m_qflthdr->Hwnd(), &rcHeader);
		rc.top += rcHeader.Height();
	}
}


/*----------------------------------------------------------------------------------------------
	Refresh the possibility hot link string in the filter table to reflect the new
	choice for how to display possibilities in this list.

	@param pnt Specifies how the possibility is to be displayed.
----------------------------------------------------------------------------------------------*/
void FilterWnd::RefreshPossibilityColumn(PossNameType pnt)
{
	try
	{
		// This may seem like overkill, and doesn't even need the input parameter, but it seems
		// to work.
		// 1. Save the current selection.
		// 2. Reconstruct the entire view (this destroys the selection).
		// 3. Restore the previous selection.
		IVwSelectionPtr qvwsel;
		VwSelLevInfo rgvsli[2];
		int ihvoRoot;
		PropTag tagTextProp;
		int cpropPrevious;
		int ichAnchor;
		int ichEnd;
		int ws;
		ComBool fAssocPrev;
		int ihvoEnd;

		AssertPtr(m_qrootb);
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
			return;
#ifdef DEBUG
		int cvsli;
		CheckHr(qvwsel->CLevels(true, &cvsli));
		Assert(cvsli == 3);
#endif
		CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, 2, rgvsli, &tagTextProp,
			&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
		Reconstruct();
		if (m_qfltvwCopy)
			m_qfltvwCopy->Reconstruct();
		CheckHr(m_qrootb->MakeTextSelection(ihvoRoot, 2, rgvsli, tagTextProp,
			cpropPrevious, ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, NULL, true, NULL));
	}
	catch (...)
	{
	}
}


/*----------------------------------------------------------------------------------------------
	Make a selection that includes all the text in the current cell.
	CURRENTLY, THIS IS THE SAME AS THE DEFAULT METHOD OF AfRootSite.  I'M LEAVING IT HERE AS A
	PLACEHOLDER FOR FUTURE WORK, SINCE THIS SEEMS LIKE A GOOD ENHANCEMENT.  SteveMc
----------------------------------------------------------------------------------------------*/
void FilterWnd::SelectAll()
{
	if (!m_qrootb)
		return;

	WaitCursor wc; // creates a wait cursor and makes it active until the end of the method.

	CheckHr(m_qrootb->MakeSimpleSel(true, false, false, true, NULL));
	// Simulate a Ctrl-Shift-End keypress using logical arrow key behavior
	CheckHr(m_qrootb->OnExtendedKey(VK_END, kgrfssShiftControl, 1));
}


//:>********************************************************************************************
//:>	FromQueryBuilder methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.

	@param ws The desired language writing system for multilingual strings.
	@param pdbi Pointer to the application database information.
	@param pvfmn Pointer to the column of filter menu nodes.
	@param clidTarget Base class id of the filtered objects.
	@param fSingleClass Flag whether this is a simple (basic or monotype advanced) filter.
	@param pfxref Pointer to utility class with functions for handling cross reference fields.
----------------------------------------------------------------------------------------------*/
FromQueryBuilder::FromQueryBuilder(int ws, AfDbInfo * pdbi, FilterMenuNodeVec * pvfmn,
	int clidTarget, bool fSingleClass, FwFilterXrefUtil * pfxref)
{
	AssertPtr(pdbi);
	AssertPtr(pvfmn);

	pdbi->GetFwMetaDataCache(&m_qmdc);

	m_vstuAlias.Clear();
	m_ialias = 0;
	m_ialiasLastClass = -1;
	m_ws = ws;
	m_clidTarget = clidTarget;
	m_fSingleClass = fSingleClass;
	ClearFilterMenuNodes(pvfmn);
	m_pfxref = pfxref;
	m_qdbi = pdbi;
}


/*----------------------------------------------------------------------------------------------
	Utility function to compute desired id alias to join on.

	@param cWJoin counter/flag to choose alias id in where clause subquery
	@param stuAliasId alias of id field to join on, but starting with "t" instead of "w"

	@return pointer to string containing all but the first character of the alias id to join on
----------------------------------------------------------------------------------------------*/
static inline const wchar * GetWJoinId(int & cWJoin, StrUni & stuAliasId)
{
	return cWJoin++ ? stuAliasId.Chars() + 1 : L"0.id";
}


static int WsForPossList(AfLpInfo * plpi, int hvoPssl)
{
	int ws = 0;
	if (hvoPssl)
	{
		int wsList = plpi->GetPsslWsFromDb(hvoPssl);
		switch (wsList)
		{
		case kwsAnal:
		case kwsAnals:
		case kwsAnalVerns:
			ws = plpi->AnalWs();
			break;
		case kwsVern:
		case kwsVerns:
		case kwsVernAnals:
			ws = plpi->VernWs();
			break;
		default:
			ws = wsList;
			break;
		}
	}
	else
	{
		ws = plpi->AnalWs();
	}
	return ws;
}

static bool IsPossListRef(AfLpInfo * plpi, int flid)
{
	if (flid == 0)
		return false;
	AfDbInfo * pdbi = plpi->GetDbInfo();
	IFwMetaDataCachePtr qmdc;
	pdbi->GetFwMetaDataCache(&qmdc);
	int nType;
	CheckHr(qmdc->GetFieldType((ULONG)flid, &nType));
	if (nType != kcptReferenceAtom &&
		nType != kcptReferenceCollection &&
		nType != kcptReferenceSequence)
	{
		return false;
	}
	ULONG clidDst;
	CheckHr(qmdc->GetDstClsId((ULONG)flid, &clidDst));
	if (clidDst == kclidCmPossibility)
		return true;
	ULONG clidBase;
	do
	{
		CheckHr(qmdc->GetBaseClsId(clidDst, &clidBase));
		if (clidBase == kclidCmPossibility)
			return true;
	} while (clidBase != 0);

	return false;
}

static int WsForColumn(FilterMenuNodeVec & vfmn, AfLpInfo * plpi)
{
	int ws = 0;
	int cfmn = vfmn.Size();
	if (vfmn[cfmn - 1]->m_hvo && IsPossListRef(plpi, vfmn[cfmn - 1]->m_flid))
	{
		ws = WsForPossList(plpi, vfmn[cfmn - 1]->m_hvo);
	}
	else if (vfmn[cfmn - 2]->m_hvo && cfmn > 2 && IsPossListRef(plpi, vfmn[cfmn - 2]->m_flid))
	{
		ws = WsForPossList(plpi, vfmn[cfmn - 2]->m_hvo);
	}

	if (ws == 0)
		ws = plpi->AnalWs();
	return ws;
}

/*----------------------------------------------------------------------------------------------
	Add a new column. This method generates the following three aliases for the column:
	1) The ID of the last class object.
	2) The ID of the object we're filtering on.
	3) The name of the field we're filtering on in the object given by #2.

	@param vfmn Reference to the vector of filter menu nodes that specify what field this
					column uses.

	@return True unless an internal error occurs that should never happen, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FromQueryBuilder::AddColumn(FilterMenuNodeVec & vfmn, PossNameType pnt)
{
	SmartBstr sbstrField;
	SmartBstr sbstrClass;
	StrUni stuAliasClass;
	StrUni stuAliasId;
	StrUni stuAliasText;
	StrUni stuFullRows;
	Vector<StrUni> vstuFieldTable;
	Vector<StrUni> vstuFieldTag;
	Vector<StrUni> vstuFieldTag2;
	Vector<StrUni> vstuAliasText;
	Vector<StrUni> vstuJoinField;
	Vector<StrUni> vstuWJoinField;
	Vector<StrUni> vstuRole;			// used for kfptRoledParticipant
	StrUni stuT;
	StrUni stuT2;
	StrUni stuT3;
	int clidLast = 0;
	int flid;
	int cWJoin = 0;			// counter/flag to choose alias id in where clause subquery.

	// This loops through the hierarchy of FilterMenuNodes. The first node should be a class
	// node, followed by any number of field nodes, and the last node should be a leaf node.

	int cfmn = vfmn.Size();
	int ws = WsForColumn(vfmn, m_qdbi->GetLpiVec()[0]);
	bool fNeedFullRows = false;
	for (int ifmn = 0; ifmn < cfmn; ifmn++)
	{
		stuFullRows.Clear();
		stuAliasId = stuAliasText;

		FilterMenuNodePtr qfmn = vfmn[ifmn];
		FilterMenuNodeType fmnt = qfmn->m_fmnt;

		if (qfmn->m_stuAlias.Length() > 0)
		{
			// This node already has a join clause for it so copy the alias.
			if (fmnt == kfmntClass)
			{
				stuAliasText = qfmn->m_stuAlias;
				m_ialiasLastClass = qfmn->m_ialiasLastClass;
				stuAliasClass = stuAliasText;
				continue;
			}
		}

		m_ialias++;
		if (fmnt == kfmntClass)
		{
			vstuFieldTable.Clear();
			vstuFieldTag.Clear();
			vstuFieldTag2.Clear();
			vstuAliasText.Clear();
			vstuJoinField.Clear();
			vstuWJoinField.Clear();
			vstuRole.Clear();

			clidLast = qfmn->m_clid;
			if (m_fSingleClass)
			{
				if (!m_stuFromClause.Length())
				{
					CheckHr(m_qmdc->GetClassName(clidLast, &sbstrClass));
					m_stuBaseRows.Format(L""
						L"select * from %s_ a where exists (%n"
						L"        select * from [ObjInfoTbl$] b%n"
						L"        where a.id = b.objid AND b.uid = @uid)",
						sbstrClass.Chars());
					m_stuFromClause.Format(L"  (%s) t%d%n", m_stuBaseRows.Chars(), m_ialias);
				}
				else
				{
					CheckHr(m_qmdc->GetClassName(clidLast, &sbstrClass));
					m_stuFromClause.FormatAppend(L"  left outer join %<0>s_ t%<1>d "
						L" on t%<1>d.Id = t0.Id%n",
						sbstrClass.Chars(), m_ialias);
					StrUni stuWT;
					stuWT.Format(L"  left outer join %<0>s_ w%<1>d "
						L"on w%<1>d.Id = w0.Id%n",
						sbstrClass.Chars(), m_ialias);
					Assert(vstuWJoinField.Size() == 0);
					vstuWJoinField.Push(stuWT);
				}
			}
			else
			{
				if (!m_stuFromClause.Length())
				{
					CheckHr(m_qmdc->GetClassName(m_clidTarget, &sbstrClass));
					m_stuBaseRows.Format(L""
						L"select * from %s_ a where exists (%n"
						L"        select * from [ObjInfoTbl$] b%n"
						L"        where a.id = b.objid AND b.uid = @uid)",
					sbstrClass.Chars());
					m_stuFromClause.Format(L"  (%s) t0%n", m_stuBaseRows.Chars());
				}
				CheckHr(m_qmdc->GetClassName(clidLast, &sbstrClass));
				m_stuFromClause.FormatAppend(L""
					L"  left outer join %<0>s_ t%<1>d on t%<1>d.Id = t0.Id%n",
					sbstrClass.Chars(), m_ialias);
				StrUni stuWT;
				stuWT.Format(L""
					L"  left outer join %<0>s_ w%<1>d on w%<1>d.Id = w0.Id%n",
					sbstrClass.Chars(), m_ialias);
				Assert(vstuWJoinField.Size() == 0);
				vstuWJoinField.Push(stuWT);
			}
			stuAliasText.Format(L"t%d.id", m_ialias);
			stuAliasClass = stuAliasText;
			m_ialiasLastClass = m_ialias;
			qfmn->m_ialiasLastClass = m_ialiasLastClass;
		}
		else if (fmnt == kfmntField)
		{
			flid = qfmn->m_flid;
			CheckHr(m_qmdc->GetOwnClsName(flid, &sbstrClass));
			CheckHr(m_qmdc->GetFieldName(flid, &sbstrField));
			stuT.Format(L"%s_%s", sbstrClass.Chars(), sbstrField.Chars());
			vstuFieldTable.Push(stuT);
			stuT.Format(L"t%d", m_ialias);
			vstuFieldTag.Push(stuT);
			stuT.Format(L"w%d", m_ialias);
			vstuFieldTag2.Push(stuT);
			vstuAliasText.Push(stuAliasText);
			stuT.Format(L"  left outer join %<0>s_%<1>s as t%<2>d "
				L"on t%<2>d.src = %<3>s%n",
				sbstrClass.Chars(), sbstrField.Chars(), m_ialias, stuAliasText.Chars());
			stuT2.Format(L"  join %<0>s_%<1>s as w%<2>d"
				L" on w%<2>d.src = w%<3>s%n",
				sbstrClass.Chars(), sbstrField.Chars(), m_ialias,
				GetWJoinId(cWJoin, stuAliasText));
			int proptype = qfmn->m_proptype;
			if (proptype == kfptRoledParticipant)
			{
				if (qfmn->m_hvo == 0)
				{
					stuT.FormatAppend(L"  left outer join %<0>s as r%<1>d "
						L"on r%<1>d.id = %<2>s%n",
						sbstrClass.Chars(), m_ialias, stuAliasText.Chars());
					stuT2.FormatAppend(L"  join %<0>s as rw%<1>d "
						L"on rw%<1>d.id = w%<2>s AND rw%<1>d.Role is null%n",
						sbstrClass.Chars(), m_ialias, GetWJoinId(cWJoin, stuAliasText));
					stuT3.Format(L"r%d.Role is null", m_ialias);
					vstuRole.Push(stuT3);
				}
				else if (qfmn->m_hvo > 0)
				{
					stuT.FormatAppend(L"  left outer join %<0>s as r%<1>d "
						L"on r%<1>d.id = %<2>s%n",
						sbstrClass.Chars(), m_ialias, stuAliasText.Chars());
					stuT2.FormatAppend(L"  join %<0>s as rw%<1>d "
						L"on rw%<1>d.id = w%<2>s AND rw%<1>d.Role = %<3>d%n",
						sbstrClass.Chars(), m_ialias, GetWJoinId(cWJoin, stuAliasText),
						qfmn->m_hvo);
					stuT3.Format(L"r%d.Role = %d", m_ialias, qfmn->m_hvo);
					vstuRole.Push(stuT3);
				}
			}
			else if (proptype == 0)
			{
				m_qmdc->GetFieldType(flid, &proptype);
				if (proptype == kcptOwningCollection || proptype == kcptReferenceCollection ||
					proptype == kcptOwningSequence || proptype == kcptReferenceSequence)
				{
					fNeedFullRows = true;
				}
			}
			vstuJoinField.Push(stuT);
			vstuWJoinField.Push(stuT2);
			stuAliasText.Format(L"t%d.dst", m_ialias);
		}
		else
		{
			Assert(fmnt == kfmntLeaf);
			Assert(ifmn >= 1);
			Assert(vfmn.Size() == ifmn + 1);
			int istu;
			for (istu = 0; istu < vstuFieldTable.Size(); ++istu)
				m_stuFromClause.Append(vstuJoinField[istu].Chars());
			StrUni stuWJoin;
			if (qfmn->m_proptype == kfptTagList)
			{
				flid = vfmn[ifmn-1]->m_flid;
			}
			else
			{
				flid = qfmn->m_flid;
				if (!flid)
				{
					// flid == 0 means we rely on the previous column's value rather than adding
					// to it.  This handles the "Person" menu item at the top of the CmPerson
					// submenu.
					Assert(qfmn->m_proptype == kfptPossList);
					if (vfmn[ifmn-1]->m_proptype != kfptRoledParticipant)
					{
						// But we may need the name and/or abbreviation for a "Contains" or
						// "Does not contain" operator
						Assert(pnt==kpntName||pnt==kpntNameAndAbbrev||pnt==kpntAbbreviation);
						switch (pnt)
						{
						case kpntName:
							m_stuFromClause.FormatAppend(L""
								L"  left outer join CmPossibility_Name as t%<0>d "
								L"on t%<0>d.obj = %<1>s AND t%<0>d.ws = %<2>d%n",
								m_ialias, stuAliasId.Chars(), ws);
							stuWJoin.Format(L"  join CmPossibility_Name as w%<0>d "
								L"on w%<0>d.obj = w%<1>s AND w%<0>d.ws = %<2>d%n",
								m_ialias, GetWJoinId(cWJoin, stuAliasId), ws);
							stuAliasText.Format(L"t%d.txt", m_ialias);
							break;
						case kpntAbbreviation:
							m_stuFromClause.FormatAppend(L""
								L"  left outer join CmPossibility_Abbreviation as t%<0>d "
								L"on t%<0>d.obj = %<1>s AND t%<0>d.ws = %<2>d%n",
								m_ialias, stuAliasId.Chars(), ws);
							stuWJoin.Format(L"  join CmPossibility_Abbreviation as w%<0>d "
								L"on w%<0>d.obj = w%<1>s AND w%<0>d.ws = %<2>d%n",
								m_ialias, GetWJoinId(cWJoin, stuAliasId), ws);
							stuAliasText.Format(L"t%d.txt", m_ialias);
							break;
						case kpntNameAndAbbrev:
							m_stuFromClause.FormatAppend(L""
								L"  left outer join CmPossibility_Name as t%<0>d "
								L"on t%<0>d.obj = %<1>s AND t%<0>d.ws = %<2>d%n"
								L"  left outer join CmPossibility_Abbreviation as ta%<0>d "
								L"on ta%<0>d.obj = %<1>s AND ta%<0>d.ws = %<2>d%n",
								m_ialias, stuAliasId.Chars(), ws);
							stuWJoin.Format(L""
								L"  join CmPossibility_Name as w%<0>d "
								L"on w%<0>d.obj = w%<1>s AND w%<0>d.ws = %<2>d%n"
								L"  join CmPossibility_Abbreviation as wa%<0>d "
								L"on wa%<0>d.obj = w%<1>s AND wa%<0>d.ws = %<2>d%n",
								m_ialias, GetWJoinId(cWJoin, stuAliasId), ws);
							stuAliasText.Format(L"(ta%<0>d.txt+' - '+t%<0>d.txt)", m_ialias);
							break;
						default:
							stuWJoin.Clear();
							break;
						}
						qfmn->m_stuAlias = stuAliasText;
						if (m_fSingleClass)
						{
							stuFullRows.Format(L"select * from%n    (%s) w0%n",
								m_stuBaseRows.Chars());
							for (istu = 0; istu < vstuWJoinField.Size(); ++istu)
								stuFullRows.Append(vstuWJoinField[istu].Chars());
							stuFullRows.Append(stuWJoin.Chars());
							stuFullRows.Append(L"  where w0.id = t1.id");
						}
						else
						{
							stuFullRows.Format(L"select * from%n    (%s) w0%n",
								m_stuBaseRows.Chars());
							for (istu = 0; istu < vstuWJoinField.Size(); ++istu)
								stuFullRows.Append(vstuWJoinField[istu].Chars());
							stuFullRows.Append(stuWJoin.Chars());
							stuFullRows.Append(L"  where w0.id = t0.id");
						}
						fNeedFullRows = false;
						continue;
					}
				}
			}
			int proptypeLeaf;
			if (flid)
			{
				CheckHr(m_qmdc->GetFieldType(flid, &proptypeLeaf));
				CheckHr(m_qmdc->GetOwnClsName(flid, &sbstrClass));
				CheckHr(m_qmdc->GetFieldName(flid, &sbstrField));
			}
			else
			{
				proptypeLeaf = kcptNil;
				sbstrClass.Clear();
				sbstrField.Clear();
			}
			switch (qfmn->m_proptype)
			{
			case kcptBoolean:
			case kcptInteger:
			case kcptNumeric:
			case kcptFloat:
			case kcptTime:
			case kcptGenDate:
			case kcptString:
			case kcptUnicode:
			case kcptBigString:
			case kcptBigUnicode:
			case kfptEnumList:
			case kfptEnumListReq:
			case kfptBoolean:
				Assert(flid);
				if (vstuFieldTable.Size() || clidLast != MAKECLIDFROMFLID(flid))
				{
					m_stuFromClause.FormatAppend(L"  left outer join %<0>s as t%<1>d "
						L"on t%<1>d.id = %<2>s%n",
						sbstrClass.Chars(), m_ialias, stuAliasId.Chars());
					stuWJoin.Format(L""
						L"  join %<0>s as w%<1>d on w%<1>d.id = w%<2>s%n",
						sbstrClass.Chars(), m_ialias, GetWJoinId(cWJoin, stuAliasText));
					stuAliasText.Format(L"t%d.%s", m_ialias, sbstrField.Chars());
				}
				else
				{
					Assert(m_ialiasLastClass != -1);
					stuAliasText.Format(L"t%d.%s", m_ialiasLastClass, sbstrField.Chars());
				}
				break;
			case kcptMultiString:
				m_stuFromClause.FormatAppend(L"  left outer join MultiStr$ as t%<0>d "
					L"on t%<0>d.obj = %<1>s and t%<0>d.flid = %<2>d%n",
					m_ialias, stuAliasId.Chars(), flid);
				stuWJoin.Format(L"  join MultiStr$ as w%<0>d "
					L"on w%<0>d.obj = w%<1>s and w%<0>d.flid = %<2>d%n",
					m_ialias, GetWJoinId(cWJoin, stuAliasId), flid);
				stuAliasText.Format(L"t%d.txt", m_ialias);
				fNeedFullRows = true;
				break;
			case kcptMultiUnicode:
				m_stuFromClause.FormatAppend(L"  left outer join %<0>s_%<1>s as t%<2>d "
					L"on t%<2>d.obj = %<3>s %n",
					sbstrClass.Chars(), sbstrField.Chars(), m_ialias, stuAliasId.Chars());
				stuWJoin.Format(L"  join %<0>s_%<1>s as w%<2>d "
					L"on w%<2>d.obj = w%<3>s %n",
					sbstrClass.Chars(), sbstrField.Chars(), m_ialias,
					GetWJoinId(cWJoin, stuAliasId));
				stuAliasText.Format(L"t%d.txt", m_ialias);
				fNeedFullRows = true;
				break;
			case kcptMultiBigString:
				m_stuFromClause.FormatAppend(L"  left outer join MultiBigStr$ as t%<0>d "
					L"on t%<0>d.obj = %<1>s and t%<0>d.flid = %<2>d%n",
					m_ialias, stuAliasId.Chars(), flid);
				stuWJoin.Format(L"  join MultiBigStr$ as w%<0>d "
					L"on w%<0>d.obj = w%<1>s and w%<0>d.flid = %<2>d%n",
					m_ialias, GetWJoinId(cWJoin, stuAliasId), flid);
				stuAliasText.Format(L"t%d.txt", m_ialias);
				fNeedFullRows = true;
				break;
			case kcptMultiBigUnicode:
				m_stuFromClause.FormatAppend(L"  left outer join MultiBigTxt$ as t%<0>d "
					L"on t%<0>d.obj = %<1>s and t%<0>d.flid = %<2>d%n",
					m_ialias, stuAliasId.Chars(), flid);
				stuWJoin.Format(L"  join MultiBigTxt$ as w%<0>d "
					L"on w%<0>d.obj = w%<1>s and w%<0>d.flid = %<2>d%n",
					m_ialias, GetWJoinId(cWJoin, stuAliasId), flid);
				stuAliasText.Format(L"t%d.txt", m_ialias);
				fNeedFullRows = true;
				break;
			case kcptOwningAtom:
			case kcptReferenceAtom:
				Assert(flid);
				stuAliasId.Format(L"t%d.%s", m_ialias, sbstrField.Chars());
				stuAliasText.Format(L"t%d.%s", m_ialias, sbstrField.Chars());
				break;
			case kfptCrossRef:
				Assert(flid);
				if (!m_pfxref->ProcessCrossRefColumn(flid,
						(vstuFieldTable.Size() || clidLast != MAKECLIDFROMFLID(flid)),
						m_ialias, m_ialiasLastClass, m_qmdc, m_stuFromClause,
						stuWJoin, sbstrClass, sbstrField, stuAliasText, stuAliasId))
				{
					Assert(qfmn->m_proptype != kfptCrossRef);
					return false;
				}
				break;
			case kcptOwningCollection:
			case kcptReferenceCollection:
			case kcptOwningSequence:
			case kcptReferenceSequence:
				Assert(flid);
				m_stuFromClause.FormatAppend(L"  left outer join %<0>s_%<1>s as t%<2>d "
					L"on t%<2>d.src = %<3>s%n",
					sbstrClass.Chars(), sbstrField.Chars(), m_ialias, stuAliasText.Chars());
				stuWJoin.Format(L""
					L"  join %<0>s_%<1>s as w%<2>d on w%<2>d.src = w%<3>s%n",
					sbstrClass.Chars(), sbstrField.Chars(), m_ialias,
					GetWJoinId(cWJoin, stuAliasText));
				fNeedFullRows = true;
				stuAliasId.Format(L"t%d.dst", m_ialias);
				stuAliasText.Format(L"t%d.dst", m_ialias);
				break;
			case kfptCrossRefList:
				Assert(flid);
				if (!m_pfxref->ProcessCrossRefListColumn(flid, m_ialias, m_ialiasLastClass,
					m_qmdc, m_stuFromClause, stuWJoin, sbstrClass, sbstrField, stuAliasText,
					stuAliasId))
				{
					Assert(qfmn->m_proptype != kfptCrossRefList);
					return false;
				}
				fNeedFullRows = true;
				break;
			case kfptStText:
				Assert(flid);
				m_stuFromClause.FormatAppend(L"  left outer join %<0>s_%<1>s as t%<2>d "
					L"on t%<2>d.src = %<3>s%n",
					sbstrClass.Chars(), sbstrField.Chars(), m_ialias, stuAliasText.Chars());
				stuWJoin.Format(L"  join %<0>s_%<1>s as w%<2>d "
					L"on w%<2>d.src = w%<3>s%n",
					sbstrClass.Chars(), sbstrField.Chars(), m_ialias,
					GetWJoinId(cWJoin, stuAliasText));
				m_ialias++;
				m_stuFromClause.FormatAppend(L""
					L"  left outer join StText_Paragraphs as t%<0>d "
					L"on t%<0>d.src = t%<1>d.dst%n", m_ialias, m_ialias - 1);
				stuWJoin.FormatAppend(L""
					L"  join StText_Paragraphs as w%<0>d "
					L"on w%<0>d.src = w%<1>d.dst%n", m_ialias, m_ialias - 1);
				m_ialias++;
				m_stuFromClause.FormatAppend(L"  left outer join StTxtPara as t%<0>d "
					L"on t%<0>d.id = t%<1>d.dst%n",
					m_ialias, m_ialias - 1);
				stuWJoin.FormatAppend(L"  join StTxtPara as w%<0>d "
					L"on w%<0>d.id = w%<1>d.dst%n",
					m_ialias, m_ialias - 1);
				fNeedFullRows = true;
				stuAliasId = stuAliasText;
				stuAliasText.Format(L"t%d.contents", m_ialias);
				break;
			case kfptPossList:
				if (proptypeLeaf == kcptOwningAtom || proptypeLeaf == kcptReferenceAtom)
				{
					if (clidLast != MAKECLIDFROMFLID(flid))
					{
						m_stuFromClause.FormatAppend(L"  left outer join %<0>s as t%<1>d "
							L"on t%<1>d.id = %<2>s%n",
							sbstrClass.Chars(), m_ialias, stuAliasId.Chars());
						stuAliasId.Format(L"t%d.%s", m_ialias, sbstrField.Chars());
						m_ialias++;
					}
					else
					{
						stuAliasId.Format(L"t%d.%s", m_ialiasLastClass, sbstrField.Chars());
					}
					switch (pnt)
					{
					case kpntName:
						m_stuFromClause.FormatAppend(L""
							L"  left outer join CmPossibility_Name as t%<0>d "
							L"on t%<0>d.obj = %<1>s and t%<0>d.ws = %<2>d%n",
							m_ialias, stuAliasId.Chars(), ws);
						stuAliasText.Format(L"t%d.txt", m_ialias);
						break;
					case kpntAbbreviation:
						m_stuFromClause.FormatAppend(L""
							L"  left outer join CmPossibility_Abbreviation as t%<0>d "
							L"on t%<0>d.obj = %<1>s and t%<0>d.ws = %<2>d%n",
							m_ialias, stuAliasId.Chars(), ws);
						stuAliasText.Format(L"t%d.txt", m_ialias);
						break;
					case kpntNameAndAbbrev:
						m_stuFromClause.FormatAppend(L""
							L"  left outer join CmPossibility_Name as t%<0>d "
							L"on t%<0>d.obj = %<1>s and t%<0>d.ws = %<2>d%n"
							L"  left outer join CmPossibility_Abbreviation as ta<0>%d "
							L"on ta%<0>d.obj = %<1>s and ta%<0>d.ws = %<2>d%n",
							m_ialias, stuAliasId.Chars(), ws);
						stuAliasText.Format(L"(ta%<0>d.txt+' - '+t%<0>d.txt)", m_ialias);
						break;
					default:
						Assert(pnt==kpntName||pnt==kpntNameAndAbbrev||pnt==kpntAbbreviation);
						break;
					}
				}
				else
				{
					fNeedFullRows = true;
					if (flid)
					{
						m_stuFromClause.FormatAppend(L"  left outer join %<0>s_%<1>s as t%<2>d "
							L"on t%<2>d.src = t1.id%n",
							sbstrClass.Chars(), sbstrField.Chars(), m_ialias);
						stuWJoin.Format(L"  join %<0>s_%<1>s as w%<2>d "
							L"on w%<2>d.src = w%<3>s%n",
							sbstrClass.Chars(), sbstrField.Chars(), m_ialias,
							GetWJoinId(cWJoin, stuAliasText));
						stuAliasId.Format(L"t%d.dst", m_ialias);
						m_ialias++;
					}
					else
					{
						// must be kfptRoledParticipant
						stuWJoin.Clear();
						stuAliasId.Format(L"t%d.dst", m_ialias - 1);
					}
					switch (pnt)
					{
					case kpntName:
						m_stuFromClause.FormatAppend(L""
							L"  left outer join CmPossibility_Name as t%<0>d "
							L"on t%<0>d.obj = %<1>s and t%<0>d.ws = %<2>d%n",
							m_ialias, stuAliasId.Chars(), ws);
						stuWJoin.FormatAppend(L""
							L"  join CmPossibility_Name as w%<0>d "
							L"on w%<0>d.obj = w%<1>s and w%<0>d.ws = %<2>d%n",
							m_ialias, GetWJoinId(cWJoin, stuAliasId), ws);
						stuAliasText.Format(L"t%d.txt", m_ialias);
						break;
					case kpntAbbreviation:
						m_stuFromClause.FormatAppend(L""
							L"  left outer join CmPossibility_Abbreviation as t%<0>d "
							L"on t%<0>d.obj = %<1>s and t%<0>d.ws = %<2>d%n",
							m_ialias, stuAliasId.Chars(), ws);
						stuWJoin.FormatAppend(L""
							L"  join CmPossibility_Abbreviation as w%<0>d "
							L"on w%<0>d.obj = w%<1>s and w%<0>d.ws = %<2>d%n",
							m_ialias, GetWJoinId(cWJoin, stuAliasId), ws);
						stuAliasText.Format(L"t%d.txt", m_ialias);
						break;
					case kpntNameAndAbbrev:
						m_stuFromClause.FormatAppend(L""
							L"  left outer join CmPossibility_Name as t%<0>d "
							L"on t%<0>d.obj = %<1>s and t%<0>d.ws = %<2>d%n"
							L"  left outer join CmPossibility_Abbreviation as ta%<0>d "
							L"on ta%<0>d.obj = %<1>s and ta%<0>d.ws = %<2>d%n",
							m_ialias, stuAliasId.Chars(), ws);
						stuWJoin.FormatAppend(L""
							L"  join CmPossibility_Name as w%<0>d "
							L"on w%<0>d.obj = w%<1>s and w%<0>d.ws = %<2>d%n"
							L"  join CmPossibility_Abbreviation as wa%<0>d "
							L"on wa%<0>d.obj = w%<1>s and wa%<0>d.ws = %<2>d%n",
							m_ialias, GetWJoinId(cWJoin, stuAliasId), ws);
						stuAliasText.Format(L"(ta%<0>d.txt+' - '+t%<0>d.txt)", m_ialias);
						break;
					default:
						Assert(pnt==kpntName||pnt==kpntNameAndAbbrev||pnt==kpntAbbreviation);
						break;
					}
				}
				break;
			case kfptTagList:
				m_stuFromClause.FormatAppend(L""
					L"  left outer join (select * from CmPossibility_Name "
					L"where ws = %<0>d) t%<1>d%n"
					L"      on t%<1>d.obj = %<2>s%n",
					ws, m_ialias, stuAliasText.Chars());
				stuWJoin.Format(L""
					L"  join (select * from CmPossibility_Name "
					L"where ws = %<0>d) w%<1>d%n"
					L"      on w%<1>d.obj = w%<2>s%n",
					ws, m_ialias, GetWJoinId(cWJoin, stuAliasText));
				fNeedFullRows = true;
				stuAliasId.Format(L"t%d.obj", m_ialias);
				stuAliasText.Format(L"t%d.txt", m_ialias);
				m_vhvoTagPossList.Push(qfmn->m_hvo);
				break;
			default:
				Assert(false); // This should never happen.
				return false;
			}
			if (fNeedFullRows)
			{
				stuFullRows.Format(L"select * from%n    (%s) w0%n",
					m_stuBaseRows.Chars());
				for (istu = 0; istu < vstuWJoinField.Size(); ++istu)
					stuFullRows.Append(vstuWJoinField[istu].Chars());
				stuFullRows.Append(stuWJoin.Chars());
				stuFullRows.Append(L"  where w0.id = t1.id");
			}
		}

		qfmn->m_stuAlias = stuAliasText;
	}

	m_vstuAlias.Push(stuAliasClass);
	m_vstuAlias.Push(stuAliasId);
	m_vstuAlias.Push(stuAliasText);
	m_vstuAlias.Push(stuFullRows);
	stuT.Clear();
	if (vstuRole.Size())
	{
		if (vstuRole.Size() > 1)
		{
			stuT.Assign(L"(");
			for (int istu = 0; istu < vstuRole.Size(); ++istu)
			{
				if (istu > 0)
					stuT.Append(L" AND ");
				stuT.Append(vstuRole[istu].Chars());
			}
			stuT.Append(L")");
		}
		else
		{
			stuT.Assign(vstuRole[0].Chars());
		}
	}
	m_vstuAlias.Push(stuT);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Find a simple text field for the given class.

	@return The field id of a simple text field, or 0 if none found.
----------------------------------------------------------------------------------------------*/
int FromQueryBuilder::FindSimpleTextField(int clidOwner)
{
	if (clidOwner == kclidRnGenericRec || clidOwner == kclidRnAnalysis ||
		clidOwner == kclidRnEvent)
	{
		return kflidRnGenericRec_Title;
	}
	Vector<ULONG> vflid;
	int cflid;
	CheckHr(m_qmdc->GetFields(clidOwner, TRUE,
		(1 << kcptString) | (1 << kcptUnicode) | (1 << kcptBigString) | (1 << kcptBigUnicode),
		vflid.Size(), vflid.Begin(), &cflid));
	if (cflid)
	{
		vflid.Resize(cflid);
		CheckHr(m_qmdc->GetFields(clidOwner, TRUE,
		  (1 << kcptString) | (1 << kcptUnicode) | (1 << kcptBigString) | (1 << kcptBigUnicode),
			vflid.Size(), vflid.Begin(), &cflid));
		return vflid[0];
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Find a structured text field for the given class.

	@return The field id of a structured text field, or 0 if none found.
----------------------------------------------------------------------------------------------*/
int FromQueryBuilder::FindStructuredTextField(int clidOwner)
{
	Vector<ULONG> vflid;
	int cflid;
	CheckHr(m_qmdc->GetFields(clidOwner, TRUE, kfcptOwningAtom, vflid.Size(), vflid.Begin(),
		&cflid));
	if (cflid)
	{
		vflid.Resize(cflid);
		CheckHr(m_qmdc->GetFields(clidOwner, TRUE, kfcptOwningAtom, vflid.Size(), vflid.Begin(),
			&cflid));
		int clid;
		ULONG clidDst;
		for (int iflid = 0; iflid < cflid; ++iflid)
		{
			clid = MAKECLIDFROMFLID(vflid[iflid]);
			if (clid == clidOwner)
			{
				CheckHr(m_qmdc->GetDstClsId(vflid[iflid], &clidDst));
				if (clidDst == kclidStText)
				{
					return vflid[iflid];
				}
			}
		}
	}
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Retrieve the from clause after all the columns have been added.

	@param stuFromClause Reference to a string used to return the from clause.
	@param vstuAlias Reference to a vector used to return all the aliases set up by the from
					clause.
----------------------------------------------------------------------------------------------*/
void FromQueryBuilder::GetFromClause(StrUni & stuFromClause, Vector<StrUni> & vstuAlias)
{
	stuFromClause = m_stuFromClause;
	vstuAlias = m_vstuAlias;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the tag related possibility lists after all the columns have been added.

	@param vhvoTagPossList Reference to a vector used to return all the database/cache IDs of
							possibility lists used in the from clause.
----------------------------------------------------------------------------------------------*/
void FromQueryBuilder::GetTagLists(Vector<int> & vhvoTagPossList)
{
	vhvoTagPossList = m_vhvoTagPossList;
}


/*----------------------------------------------------------------------------------------------
	Loop through the FilterMenuNode structures and clear out the alias used in each one. This
	gets called when constructing a new query.

	@param pvfmn Pointer to the column of filter menu nodes.
----------------------------------------------------------------------------------------------*/
void FromQueryBuilder::ClearFilterMenuNodes(FilterMenuNodeVec * pvfmn)
{
	int cfmn = pvfmn->Size();
	for (int ifmn = 0; ifmn < cfmn; ifmn++)
	{
		(*pvfmn)[ifmn]->m_stuAlias.Clear();
		ClearFilterMenuNodes(&(*pvfmn)[ifmn]->m_vfmnSubItems);
	}
}


//:>********************************************************************************************
//:>	WhereQueryBuilder methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.

	@param vstuAlias Reference to the vector of aliases set up by the from clause.
	@param pvvfmnColumns Pointer to a vector of vectors of filter menu nodes that specify this
					filter.
	@param plpi Pointer to the application language project information.
	@param clidTarget
	@param pfxref Pointer to utility class with functions for handling cross reference fields.
----------------------------------------------------------------------------------------------*/
WhereQueryBuilder::WhereQueryBuilder(Vector<StrUni> & vstuAlias,
	Vector<FilterMenuNodeVec> * pvvfmnColumns, Vector<PossNameType> & vpnt, AfLpInfo * plpi,
	int clidTarget, FwFilterXrefUtil * pfxref)
	: m_vpnt(vpnt)
{
	AssertPtr(pvvfmnColumns);
	AssertPtr(plpi);

	m_vstuAlias = vstuAlias;
	m_pvvfmnColumns = pvvfmnColumns;
	m_qlpi = plpi;
	m_clidTarget = clidTarget;
	m_pfxref = pfxref;
}


/*----------------------------------------------------------------------------------------------
	Loop through all the rows and cells in the table and generate the SQL code necessary for
	the where clause. This method returns false if it cannot create a proper where clause.

	@param hvoFilter Database ID of the filter.

	@return True if successful, false if an error occurs.

	@exception E_ABORT if an error message has already been displayed.
----------------------------------------------------------------------------------------------*/
bool WhereQueryBuilder::BuildWhereClause(HVO hvoFilter)
{
	Assert(hvoFilter);

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuQuery;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	StrUni stuTable;
	StrUni stuRow;
	StrUni stuCell;

	try
	{
		m_qlpi->GetDbInfo()->GetDbAccess(&qode);
		CheckHr(qode->CreateCommand(&qodc));

		ITsStrFactoryPtr qtsf;
		ITsStringPtr qtss;
		qtsf.CreateInstance(CLSID_TsStrFactory);

		stuQuery.Format(L"select cell.src, ce.contents, ce.contents_fmt "
			L"from CmFilter_Rows as row "
			L"left outer join CmRow_Cells as cell on cell.src = row.dst "
			L"left outer join CmCell as ce on ce.id = cell.dst "
			L"where row.src = %d order by row.ord", hvoFilter);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		wchar rgchText[8200];
		BYTE rgbFormat[8200];
		HVO hvoRow;
		HVO hvoRowCur = NULL;
		int icol;
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoRow),
				isizeof(hvoRow), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(rgchText),
				isizeof(rgchText), &cbSpaceTaken, &fIsNull, 2));
			int cchText = (int)cbSpaceTaken / isizeof(wchar);
			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(rgbFormat),
				isizeof(rgbFormat), &cbSpaceTaken, &fIsNull, 0));
			int cbFormat = (int)cbSpaceTaken;

			if (hvoRow != hvoRowCur)
			{
				// We have started a new row, so add the current row string to the table
				// string and clear the row string out.
				if (hvoRowCur != NULL && stuRow.Length() > 0)
				{
					if (stuTable.Length() > 0)
						stuTable.FormatAppend(L"%n  or%n ");
					stuTable.FormatAppend(L"(%s)", stuRow.Chars());
				}

				stuRow.Clear();
				hvoRowCur = hvoRow;
				icol = 0;
			}

			// Only add text to the row string if the cell is not empty.
			if (cchText)
			{
				CheckHr(qtsf->DeserializeStringRgch(rgchText, &cchText, rgbFormat, &cbFormat,
					&qtss));

				// Get the SQL text for the current cell.
				if (!GetCellSQL(qtss, icol, stuCell))
				{
#ifdef DEBUG
					::MessageBoxA(NULL,
						"call to GetCellSQL() failed in WhereQueryBuilder::BuildWhereClause()",
						"DEBUG", MB_OK | MB_TASKMODAL);
#endif
					return false;
				}

				// Add the SQL text to the row string.
				if (stuCell.Length() > 0)
				{
					if (stuRow.Length() > 0)
						stuRow.FormatAppend(L"%n  and%n  ");
					stuRow.FormatAppend(L"(%s)", stuCell.Chars());
				}
			}

			icol++;

			CheckHr(qodc->NextRow(&fMoreRows));
		}

		// We have finished going through the rows, so add the last row string to the end of
		// the table string.
		if (hvoRowCur != NULL && stuRow.Length() > 0)
		{
			if (stuTable.Length() > 0)
				stuTable.FormatAppend(L"%n  or%n  ");
			stuTable.FormatAppend(L"(%s)", stuRow.Chars());
		}
	}
	catch (Throwable & thr)
	{
		ThrowHr(thr.Error());
	}
	catch (...)
	{
#ifdef DEBUG
		::MessageBoxA(NULL, "ERROR caught in WhereQueryBuilder::BuildWhereClause()",
			"DEBUG", MB_OK | MB_TASKMODAL);
#endif
		return false;
	}

	m_stuWhereClause = stuTable;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Get the SQL text for the current cell. This method returns false if it cannot create
	proper SQL for the cell.

	@param ptss Pointer to an ITsString containing the filter cell text.
	@param icol Index of the column in the filter.
	@param stuCell Reference to a string for returning the SQL generated for the cell.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool WhereQueryBuilder::GetCellSQL(ITsString * ptss, int icol, StrUni & stuCell)
{
	AssertPtr(ptss);

	stuCell.Clear();

	const OLECHAR * prgwch;
	int cch;
	CheckHr(ptss->LockText(&prgwch, &cch));
	StrUni stu(prgwch, cch);
	ptss->UnlockText(prgwch);

#ifdef INCOMPLETE_GetCellSQL
	StrAnsi stab;
	StrAnsi stabTitle;
	stabTitle.Format("NOT YET IMPLEMENTED - WhereQueryBuilder::GetCellSQL(\"%S\", %d)",
		stu.Chars(), icol);
#endif

	Assert((uint)(icol * 5 + 4) < (uint)m_vstuAlias.Size());
#ifdef DEBUG
	StrUni & stuAliasClass = m_vstuAlias[icol * 5];
#endif
	StrUni & stuAliasId = m_vstuAlias[icol * 5 + 1];
	StrUni & stuAliasText = m_vstuAlias[icol * 5 + 2];
	StrUni & stuFullRows = m_vstuAlias[icol * 5 + 3];
	StrUni & stuRole = m_vstuAlias[icol * 5 + 4];
	bool fIgnoreRole = false;
	// get where clause aliases, changing from something like "t4.txt" to "w4.txt"
	StrUni stuAliasId2(stuAliasId);
	stuAliasId2.Replace(0, 1, L"w");
	StrUni stuAliasText2(stuAliasText);
	int ich = stuAliasText2.FindCh(L't');
	Assert(ich > -1 && ich < 2);		// "either t4.txt" or "(ta4.txt+' - '+t4.txt)"
	stuAliasText2.Replace(ich, ich+1, L"w");
	ich = stuAliasText2.FindStr(L"+' - '+");
	if (ich > -1)
	{
		ich = stuAliasText2.FindCh(L't', ich + 7);
		Assert(ich > -1);
		stuAliasText2.Replace(ich, ich+1, L"w");
	}
	StrUni stuText;
	int ws = 0;
	wchar * pszSymbol;
	wchar * pszSymbol2;
	wchar * pszStrict;
	wchar * pszStrict2;

	// This may be needed, so go ahead and get it this once.
	SYSTEMTIME systimeNow;
	::GetLocalTime(&systimeNow);

	Assert((uint)icol < (uint)m_pvvfmnColumns->Size());
	FilterMenuNodeVec & vfmn = (*m_pvvfmnColumns)[icol];
	Assert(vfmn.Size() > 0);
	// pfmn is the pointer to the leaf FilterMenuNode.
	FilterMenuNode * pfmn = vfmn[vfmn.Size() - 1];
	AssertPtr(pfmn);
	int nType = pfmn->m_proptype;

	// A cell can contain multiple conditions, so loop through until we get the end of the cell.
	HVO hvo;
	HVO hvoOverlay = 0;
	int nT;
	const wchar * prgchMin = const_cast<wchar *>(stu.Chars());
	const wchar * prgch = prgchMin;
	FilterUtil::SkipWhiteSpace(prgch);
	bool fMatchBegin;
	bool fMatchEnd;
	const wchar * prgchBegin;
	StrUni stuAliasWs;
	StrUni stuAliasEnc2;
	if (nType == kcptMultiUnicode || nType == kcptMultiString || nType == kcptMultiBigUnicode ||
		nType == kcptMultiBigString)
	{
		stuAliasWs.Assign(stuAliasText);
		int ich = stuAliasWs.FindCh('.');
		Assert(ich > 0);
		stuAliasWs.Replace(ich, stuAliasWs.Length(), L".Ws");
		stuAliasEnc2.Assign(stuAliasText2);
		ich = stuAliasEnc2.FindCh('.');
		Assert(ich > 0);
		stuAliasEnc2.Replace(ich, stuAliasEnc2.Length(), L".Ws");
	}
	if (nType == kfptTagList)
	{
		AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(pfmn->m_flid);
		hvoOverlay = aoi.m_hvo;
	}
	ILgWritingSystemFactoryPtr qwsf;
	m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	while (*prgch)
	{
		FilterKeywordType fkt = m_kl.GetTypeFromStr(prgch);
		switch (fkt)
		{
		case kfktEmpty:
			if (stuFullRows.Length())
			{
				stuCell.FormatAppend(L"NOT EXISTS (%s AND ", stuFullRows.Chars());
				fIgnoreRole = true;
				switch (nType)
				{
				case kcptGenDate:
					stuCell.FormatAppend(L"%s IS NOT NULL AND %s <> 0",
						stuAliasText2.Chars(), stuAliasText2.Chars());
					break;
				case kcptInteger:
				case kcptNumeric:
				case kcptFloat:
				case kcptTime:
				case kfptEnumList:
				case kfptEnumListReq:
					stuCell.FormatAppend(L"%s IS NOT NULL", stuAliasText2.Chars());
					break;
				case kcptString:
				case kcptUnicode:
					stuCell.FormatAppend(L"%s IS NOT NULL AND %s NOT LIKE ''",
						stuAliasText2.Chars(), stuAliasText2.Chars());
					break;
				case kcptBigString:
				case kcptBigUnicode:
				case kfptStText:
					stuCell.FormatAppend(L""
						L"SUBSTRING(%s,1,10) IS NOT NULL AND SUBSTRING(%s,1,10) NOT LIKE ''",
						stuAliasText2.Chars(), stuAliasText2.Chars());
					break;
				case kcptMultiString:
				case kcptMultiUnicode:
					ws = FilterUtil::ParseWritingSystem(prgch, qwsf);
					if (!ws)
						ws = WsForColumn(vfmn, m_qlpi);
					stuCell.FormatAppend(L"%s = %d AND %s IS NOT NULL AND %s NOT LIKE ''",
						stuAliasEnc2.Chars(), ws, stuAliasText2.Chars(),
						stuAliasText2.Chars());
					break;
				case kcptMultiBigString:
				case kcptMultiBigUnicode:
					ws = FilterUtil::ParseWritingSystem(prgch, qwsf);
					if (!ws)
						ws = WsForColumn(vfmn, m_qlpi);
					stuCell.FormatAppend(L"%s = %d AND "
						L"SUBSTRING(%s,1,10) IS NOT NULL AND SUBSTRING(%s,1,10) NOT LIKE ''",
						stuAliasEnc2.Chars(), ws, stuAliasText2.Chars(),
						stuAliasText2.Chars());
					break;
				case kfptTagList:
					Assert(hvoOverlay);
					stuCell.FormatAppend(L"%s IN (SELECT Dst FROM CmOverlay_PossItems "
						L"WHERE Src = %d)",
						stuAliasId2.Chars(), hvoOverlay);
					break;
				default:
					stuCell.FormatAppend(L"%s IS NOT NULL", stuAliasId2.Chars());
					break;
				}
				stuCell.Append(L")");
			}
			else
			{
				switch (nType)
				{
				case kcptGenDate:
					stuCell.FormatAppend(L"(%s IS NULL OR %s = 0)",
						stuAliasText.Chars(), stuAliasText.Chars());
					break;
				case kcptInteger:
				case kcptNumeric:
				case kcptFloat:
				case kcptTime:
				case kfptEnumList:
				case kfptEnumListReq:
					stuCell.FormatAppend(L"%s IS NULL", stuAliasText.Chars());
					break;
				case kcptString:
				case kcptUnicode:
					stuCell.FormatAppend(L"(%s IS NULL OR %s LIKE '')",
						stuAliasText.Chars(), stuAliasText.Chars());
					break;
				case kcptBigString:
				case kcptBigUnicode:
				case kfptStText:
					stuCell.FormatAppend(L""
						L"(SUBSTRING(%s,1,10) IS NULL OR SUBSTRING(%s,1,10) LIKE '')",
						stuAliasText.Chars(), stuAliasText.Chars());
					break;
				case kcptMultiString:
				case kcptMultiUnicode:
					ws = FilterUtil::ParseWritingSystem(prgch, qwsf);
					if (!ws)
						ws = WsForColumn(vfmn, m_qlpi);
					stuCell.FormatAppend(L""
						L"(NOT (%s = %d AND %s IS NOT NULL AND %s NOT LIKE ''))",
						stuAliasWs.Chars(), ws, stuAliasText.Chars(), stuAliasText.Chars());
					break;
				case kcptMultiBigString:
				case kcptMultiBigUnicode:
					ws = FilterUtil::ParseWritingSystem(prgch, qwsf);
					if (!ws)
						ws = WsForColumn(vfmn, m_qlpi);
					stuCell.FormatAppend(L"(NOT (%s = %d AND "
						L"SUBSTRING(%s,1,10) IS NOT NULL AND SUBSTRING(%s,1,10) NOT LIKE ''))",
						stuAliasWs.Chars(), ws, stuAliasText.Chars(), stuAliasText.Chars());
					break;
				case kfptTagList:
					Assert(hvoOverlay);
					stuCell.FormatAppend(L"(%s NOT IN (SELECT Dst FROM CmOverlay_PossItems "
						L"WHERE Src = %d)) OR (%s IS NULL)",
						stuAliasId2.Chars(), hvoOverlay, stuAliasId2.Chars());
					break;
				default:
					stuCell.FormatAppend(L"%s IS NULL", stuAliasId.Chars());
					break;
				}
			}
			break;
		////////////////////////////////////////////////////////////////////////////////////////
		case kfktNotEmpty:
			switch (nType)
			{
			case kcptGenDate:
				stuCell.FormatAppend(L"%s IS NOT NULL AND %s <> 0",
					stuAliasText.Chars(), stuAliasText.Chars());
				break;
			case kcptInteger:
			case kcptNumeric:
			case kcptFloat:
			case kcptTime:
			case kfptEnumList:
			case kfptEnumListReq:
				stuCell.FormatAppend(L"%s IS NOT NULL", stuAliasText.Chars());
				break;
			case kcptString:
			case kcptUnicode:
				stuCell.FormatAppend(L"%s IS NOT NULL AND %s NOT LIKE ''",
					stuAliasText.Chars(), stuAliasText.Chars());
				break;
			case kcptBigString:
			case kcptBigUnicode:
			case kfptStText:
				stuCell.FormatAppend(L""
					L"SUBSTRING(%s,1,10) IS NOT NULL AND SUBSTRING(%s,1,10) NOT LIKE ''",
					stuAliasText.Chars(), stuAliasText.Chars());
				break;
			case kcptMultiString:
			case kcptMultiUnicode:
				ws = FilterUtil::ParseWritingSystem(prgch, qwsf);
				if (!ws)
					ws = WsForColumn(vfmn, m_qlpi);
				stuCell.FormatAppend(L"%s = %d AND %s IS NOT NULL AND %s NOT LIKE ''",
					stuAliasWs.Chars(), ws, stuAliasText.Chars(), stuAliasText.Chars());
				break;
			case kcptMultiBigString:
			case kcptMultiBigUnicode:
				ws = FilterUtil::ParseWritingSystem(prgch, qwsf);
				if (!ws)
					ws = WsForColumn(vfmn, m_qlpi);
				stuCell.FormatAppend(L"%s = %d AND "
					L"SUBSTRING(%s,1,10) IS NOT NULL AND SUBSTRING(%s,1,10) NOT LIKE ''",
					stuAliasWs.Chars(), ws, stuAliasText.Chars(), stuAliasText.Chars());
				break;
			case kfptTagList:
				Assert(hvoOverlay);
				stuCell.FormatAppend(L"%s IS NOT NULL AND"
					L" %s IN (SELECT Dst FROM CmOverlay_PossItems"
					L" WHERE Src = %d)",
					stuAliasId.Chars(), stuAliasId.Chars(), hvoOverlay);
				break;
			default:
				stuCell.FormatAppend(L"%s IS NOT NULL", stuAliasId.Chars());
				break;
			}
			break;
		////////////////////////////////////////////////////////////////////////////////////////
		case kfktContains:
			switch (nType)
			{
			case kcptString:
			case kcptUnicode:
			case kcptBigString:
			case kcptBigUnicode:
			case kcptMultiString:
			case kcptMultiUnicode:
			case kcptMultiBigString:
			case kcptMultiBigUnicode:
			case kfptStText:
			case kfptTagList:
			case kfptCrossRef:
			case kfptCrossRefList:
			case kfptPossList:
				prgchBegin = prgch;
				if (!ParseConditionText(prgch, stuText, fMatchBegin, fMatchEnd, ws))
					return false;
				if (nType == kcptMultiUnicode || nType == kcptMultiString ||
					nType == kcptMultiBigUnicode || nType == kcptMultiBigString)
				{
					if (!ws)
						ws = WsForColumn(vfmn, m_qlpi);
				}
				else if (nType == kfptTagList)
				{
					Assert(hvoOverlay);
					stuCell.FormatAppend(L"(%s IN (SELECT Dst FROM CmOverlay_PossItems"
						L" WHERE Src = %d)) AND ",
						stuAliasId.Chars(), hvoOverlay);
				}
				else if (nType == kfptCrossRef || nType == kfptCrossRefList)
				{
					/*
					  TODO Enhancement:
					  For kfptCrossRef and kfptCrossRefList, the following illustrates the SQL
					  code that should be generated rather than a simple

					t3.Title LIKE N'%Event%Jan%' ESCAPE '\'.

					CASE
						WHEN t3.Class$ = 4005 AND t3.OwnFlid$ = 4001001 THEN 'Analysis'
						WHEN t3.Class$ = 4005 AND t3.OwnFlid$ = 4004009 THEN 'Subanalysis'
						WHEN t3.Class$ = 4006 AND t3.OwnFlid$ = 4001001 THEN 'Event'
						ELSE 'Subevent'
					END+' - '+ISNULL(t3.Title,'')+' - '+
					DATENAME(dd,t3.DateCreated)+'-'+
					LEFT(DATENAME(mm,t3.DateCreated),3)+'-'+
					DATENAME(yy,t3.DateCreated) LIKE N'%Event%Jan%' ESCAPE '\'

					  This is, of course, program dependent (pulling strings from resources),
					  and varies according to the system setting of the 'short date format'.
					*/
					m_pfxref->FixCrossRefTitle(stuAliasText, pfmn->m_flid);
					m_pfxref->FixCrossRefTitle(stuAliasText2, pfmn->m_flid);
				}
				if (stuText.Length())
				{
					if (ws)
						stuCell.FormatAppend(L"(%s = %d AND ", stuAliasWs.Chars(), ws);
					stuCell.FormatAppend(L"(%s LIKE N'%%%s%%' ESCAPE '\\'",
						stuAliasText.Chars(), stuText.Chars());
					if (fMatchBegin)
					{
						ReparseConditionText(prgchBegin, stuText, true, false);
						if (stuText.Length())
							stuCell.FormatAppend(L" OR %s LIKE N'%s%%' ESCAPE '\\'",
								stuAliasText.Chars(), stuText.Chars());
					}
					if (fMatchEnd)
					{
						ReparseConditionText(prgchBegin, stuText, false, true);
						if (stuText.Length())
							stuCell.FormatAppend(L" OR %s LIKE N'%%%s' ESCAPE '\\'",
								stuAliasText.Chars(), stuText.Chars());
					}
					if (fMatchBegin && fMatchEnd)
					{
						ReparseConditionText(prgchBegin, stuText, true, true);
						if (stuText.Length())
							stuCell.FormatAppend(L" OR %s LIKE N'%s' ESCAPE '\\'",
								stuAliasText.Chars(), stuText.Chars());
					}
					stuCell.Append(L")");
					if (ws)
						stuCell.Append(L")");
				}
				else
				{
					// "Contains ''" is equivalent to "Empty" for naive users (is there any
					// other kind?)
					StrUni stuVal;
					if (stuFullRows.Length())
					{
						if (nType == kcptBigString || nType == kcptBigUnicode ||
							nType == kfptStText || nType == kcptMultiBigString ||
							nType == kcptMultiBigUnicode)
						{
							stuVal.Format(L"SUBSTRING(%s,1,10)", stuAliasText2.Chars());
						}
						else
						{
							stuVal.Assign(stuAliasText2);
						}
						stuCell.FormatAppend(L"NOT EXISTS (%s AND ", stuFullRows.Chars());
						fIgnoreRole = true;
						if (ws)
							stuCell.FormatAppend(L"%s = %d AND ", stuAliasEnc2.Chars(), ws);
						stuCell.FormatAppend(L"%s IS NOT NULL AND %s NOT LIKE '')",
							stuVal.Chars(), stuVal.Chars());
					}
					else
					{
						if (nType == kcptBigString || nType == kcptBigUnicode ||
							nType == kfptStText || nType == kcptMultiBigString ||
							nType == kcptMultiBigUnicode)
						{
							stuVal.Format(L"SUBSTRING(%s,1,10)", stuAliasText.Chars());
						}
						else
						{
							stuVal.Assign(stuAliasText);
						}
						if (ws)
						{
							stuCell.FormatAppend(L""
								L"(NOT (%s = %d AND %s IS NOT NULL AND %s NOT LIKE ''))",
								stuAliasWs.Chars(), ws, stuVal.Chars(), stuVal.Chars());
						}
						else
						{
							stuCell.FormatAppend(L"(%s IS NULL OR %s LIKE '')",
								stuVal.Chars(), stuVal.Chars());
						}
					}
				}
				break;
			default:
				return false;
			}
			break;
		////////////////////////////////////////////////////////////////////////////////////////
		case kfktDoesNotContain:
			switch (nType)
			{
			case kcptString:
			case kcptUnicode:
			case kcptBigString:
			case kcptBigUnicode:
			case kcptMultiString:
			case kcptMultiUnicode:
			case kcptMultiBigString:
			case kcptMultiBigUnicode:
			case kfptStText:
			case kfptTagList:
			case kfptCrossRef:
			case kfptCrossRefList:
			case kfptPossList:
				prgchBegin = prgch;
				if (!ParseConditionText(prgch, stuText, fMatchBegin, fMatchEnd, ws))
					return false;
				if (nType == kcptMultiUnicode || nType == kcptMultiString ||
					nType == kcptMultiBigUnicode || nType == kcptMultiBigString)
				{
					if (!ws)
						ws = WsForColumn(vfmn, m_qlpi);
				}
				if (nType == kfptTagList)
				{
					Assert(hvoOverlay);
					stuCell.FormatAppend(L"(%s IN (SELECT Dst FROM CmOverlay_PossItems"
						L" WHERE Src = %d)) AND ",
						stuAliasId.Chars(), hvoOverlay);
				}
				if (stuText.Length())
				{
					if (stuFullRows.Length())
					{
						stuCell.FormatAppend(L"NOT EXISTS (%s AND ", stuFullRows.Chars());
						fIgnoreRole = true;
						if (ws)
							stuCell.FormatAppend(L"(%s = %d AND ", stuAliasEnc2.Chars(), ws);
						stuCell.FormatAppend(L"(%s LIKE N'%%%s%%' ESCAPE '\\'",
							stuAliasText2.Chars(), stuText.Chars());
						if (fMatchBegin)
						{
							ReparseConditionText(prgchBegin, stuText, true, false);
							if (stuText.Length())
								stuCell.FormatAppend(L" OR %s LIKE N'%s%%' ESCAPE '\\'",
									stuAliasText2.Chars(), stuText.Chars());
						}
						if (fMatchEnd)
						{
							ReparseConditionText(prgchBegin, stuText, false, true);
							if (stuText.Length())
								stuCell.FormatAppend(L" OR %s LIKE N'%%%s' ESCAPE '\\'",
									stuAliasText2.Chars(), stuText.Chars());
						}
						if (fMatchBegin && fMatchEnd)
						{
							ReparseConditionText(prgchBegin, stuText, true, true);
							if (stuText.Length())
								stuCell.FormatAppend(L" OR %s LIKE N'%s' ESCAPE '\\'",
									stuAliasText2.Chars(), stuText.Chars());
						}
					}
					else
					{
						stuCell.FormatAppend(L"(%s IS NULL OR NOT ", stuAliasText.Chars());
						if (ws)
							stuCell.FormatAppend(L"(%s = %d AND ", stuAliasWs.Chars(), ws);
						stuCell.FormatAppend(L"(%s LIKE N'%%%s%%' ESCAPE '\\'",
							stuAliasText.Chars(), stuText.Chars());
						if (fMatchBegin)
						{
							ReparseConditionText(prgchBegin, stuText, true, false);
							if (stuText.Length())
								stuCell.FormatAppend(L" OR %s LIKE N'%s%%' ESCAPE '\\'",
									stuAliasText.Chars(), stuText.Chars());
						}
						if (fMatchEnd)
						{
							ReparseConditionText(prgchBegin, stuText, false, true);
							if (stuText.Length())
								stuCell.FormatAppend(L" OR %s LIKE N'%%%s' ESCAPE '\\'",
									stuAliasText.Chars(), stuText.Chars());
						}
						if (fMatchBegin && fMatchEnd)
						{
							ReparseConditionText(prgchBegin, stuText, true, true);
							if (stuText.Length())
								stuCell.FormatAppend(L" OR %s LIKE N'%s' ESCAPE '\\'",
									stuAliasText.Chars(), stuText.Chars());
						}
					}
					stuCell.Append(L")");
					if (ws)
						stuCell.Append(L")");
					stuCell.Append(L")");
				}
				else
				{
					// "Does Not Contain ''" is equivalent to "Not Empty" for naive users
					// (is there any other kind?)
					StrUni stuVal;
					if (nType == kcptBigString || nType == kcptBigUnicode ||
						nType == kfptStText || nType == kcptMultiBigString ||
						nType == kcptMultiBigUnicode)
					{
						stuVal.Format(L"SUBSTRING(%s,1,10)", stuAliasText.Chars());
					}
					else
					{
						stuVal.Assign(stuAliasText);
					}
					if (ws)
						stuCell.FormatAppend(L"%s = %d AND ", stuAliasWs.Chars(), ws);
					stuCell.FormatAppend(L"%s IS NOT NULL AND %s NOT LIKE ''",
						stuVal.Chars(), stuVal.Chars());
				}
				break;
			default:
				return false;
			}
			break;
		////////////////////////////////////////////////////////////////////////////////////////
		case kfktEqual:
			switch (nType)
			{
			case kcptInteger:
				if (!ParseIntegerText(prgch, &nT))
					return false;
				stuCell.FormatAppend(L"%s = %d", stuAliasText.Chars(), nT);
				break;
			case kcptNumeric:				// TODO
#ifdef INCOMPLETE_GetCellSQL
				stab.Format("op is '=', type is Numeric");
#ifdef DEBUG
				stab.FormatAppend("; Class = \"%S\", Id = \"%S\", Text = \"%S\"",
					stuAliasClass.Chars(), stuAliasId.Chars(), stuAliasText.Chars());
#endif
				::MessageBoxA(NULL, stab.Chars(), stabTitle.Chars(),
					MB_OK | MB_ICONWARNING | MB_TASKMODAL);
				ThrowHr(E_ABORT);
#endif
				break;
			case kcptFloat:				// TODO
#ifdef INCOMPLETE_GetCellSQL
				stab.Format("op is '=', type is Float");
#ifdef DEBUG
				stab.FormatAppend("; Class = \"%S\", Id = \"%S\", Text = \"%S\"",
					stuAliasClass.Chars(), stuAliasId.Chars(), stuAliasText.Chars());
#endif
				::MessageBoxA(NULL, stab.Chars(), stabTitle.Chars(),
					MB_OK | MB_ICONWARNING | MB_TASKMODAL);
				ThrowHr(E_ABORT);
#endif
				break;
			case kcptTime:
				{
					int nScope;
					int nYear;
					int nMonth;
					int nDay;
					int nDayMax;
					if (!ParseDateText(prgch, nScope, nYear, nMonth, nDay))
						return false;
					switch (nScope)
					{
					case kstidFltrExactDate:
						stuCell.FormatAppend(L"DATEPART(YEAR, %s) = %d AND "
							L"DATEPART(MONTH, %s) = %d AND DATEPART(DAY, %s) = %d",
							stuAliasText.Chars(), nYear, stuAliasText.Chars(), nMonth,
							stuAliasText.Chars(), nDay);
						break;
#ifdef VERSION2FILTER
					case kstidFltrMonth:
						stuCell.FormatAppend(L"DATEPART(MONTH, %s) = %d",
							stuAliasText.Chars(), nMonth);
						break;
#endif /*VERSION2FILTER*/
					case kstidFltrMonthYear:
						stuCell.FormatAppend(L"DATEPART(YEAR, %s) = %d AND "
							L"DATEPART(MONTH, %s) = %d",
							stuAliasText.Chars(), nYear, stuAliasText.Chars(), nMonth);
						break;
					case kstidFltrYear:
						stuCell.FormatAppend(L"DATEPART(YEAR, %s) = %d",
							stuAliasText.Chars(), nYear);
						break;
#ifdef VERSION2FILTER
					case kstidFltrDay:
						stuCell.FormatAppend(L"DATEPART(DAY, %s) = %d",
							stuAliasText.Chars(), nDay);
						break;
#endif /*VERSION2FILTER*/
					case kstidFltrToday:
						stuCell.FormatAppend(L"DATEPART(YEAR, %s) = %d AND "
							L"DATEPART(MONTH, %s) = %d AND DATEPART(DAY, %s) = %d",
							stuAliasText.Chars(), systimeNow.wYear,
							stuAliasText.Chars(), systimeNow.wMonth,
							stuAliasText.Chars(), systimeNow.wDay);
						break;
					case kstidFltrLastWeek:
					case kstidFltrLastMonth:
					case kstidFltrLastYear:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLastWeek)
						{
							// How does this differ from Last7Days?
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLastMonth)
						{
							if (--nMonth == 0)
							{
								nMonth = 12;
								if (--nYear == 0)
									nYear = -1;		// 1 BC immediately precedes 1 AD
							}
						}
						else
						{
							Assert(nScope == kstidFltrLastYear);
							if (--nYear == 0)
								nYear = -1;		// 1 BC immediately precedes 1 AD
						}
						// Convert, for example, Feb 30 to Feb 28.
						nDayMax = GetDaysInMonth(nMonth, nYear);
						if (nDay > nDayMax)
							nDay = nDayMax;
						{
							// Equals Last Week means "within the last week"
							// Equals Last month means "within the last month"
							// Equals Last Year means "within the last year"
							Assert(!stuFullRows.Length());
							StrUni stuYear;
							StrUni stuMonth;
							StrUni stuDay;
							stuYear.Format(L"DATEPART(YEAR, %s)", stuAliasText.Chars());
							stuMonth.Format(L"DATEPART(MONTH, %s)", stuAliasText.Chars());
							stuDay.Format(L"DATEPART(DAY, %s)", stuAliasText.Chars());
							stuCell.FormatAppend(L"(%s > %d OR (%s = %d AND "
								L"(%s > %d OR (%s = %d AND %s >= %d))))",
								stuYear.Chars(), nYear, stuYear.Chars(), nYear,
								stuMonth.Chars(), nMonth, stuMonth.Chars(), nMonth,
								stuDay.Chars(), nDay);
						}
						break;
#ifdef VERSION2FILTER
					case kstidFltrLast7Days:
					case kstidFltrLast30Days:
					case kstidFltrLast365Days:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLast7Days)
						{
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLast30Days)
						{
							// Adjust for varying length months.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one month ago today", which would simply be "--nMonth;"?
							nDay -= 30;
							while (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else
						{
							// If leap year, adjust by one day if necessary.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one year ago today", which would simply be "--nYear;"?
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth > 2)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									nDay = 1;
									if (++nMonth > 12)
										nMonth = 1;
								}
								else
								{
									++nDay;
								}
							}
							--nYear;
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth < 3)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									++nMonth;
									nDay = 1;
								}
								else
								{
									++nDay;
								}
							}
						}
						goto LTimeEqual;
#endif /*VERSION2FILTER*/
					default:
						return false;
					}
				}
				break;
			case kcptGenDate:
				{
					int nScope;
					int nYear;
					int nMonth;
					int nDay;
					int nDayMax;
					if (!ParseDateText(prgch, nScope, nYear, nMonth, nDay))
						return false;
					int nDate;
					switch (nScope)
					{
					case kstidFltrExactDate:
LGenDateEqual:
						if (nYear < 0)
						{
							// June 1, 1000 BC => -10000731
							nDate = 10000 * nYear - 100 * (13 - nMonth) - (32 - nDay);
						}
						else
						{
							// June 1, 1000 AD => 10000601
							nDate = nDay + 100 * nMonth + 10000 * nYear;
						}
						stuCell.FormatAppend(L"(%s / 10) = %d", stuAliasText.Chars(), nDate);
						break;
					case kstidFltrMonthYear:
						if (nYear < 0)
						{
							// June, 1000 BC => -100007
							nDate = 100 * nYear - (13 - nMonth);
						}
						else
						{
							// June, 1000 AD => 100006
							nDate = 100 * nYear + nMonth;
						}
						stuCell.FormatAppend(L"(%s / 1000) = %d",
							stuAliasText.Chars(), nDate);
						break;

					case kstidFltrYear:
						// 1000 BC => -1000
						// 1000 AD => 1000
						stuCell.FormatAppend(L"(%s / 100000) = %d",
							stuAliasText.Chars(), nYear);
						break;

					case kstidFltrToday:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						goto LGenDateEqual;

#ifdef VERSION2FILTER
					case kstidFltrMonth:
					case kstidFltrDay:
#ifdef INCOMPLETE_GetCellSQL
						stab.Format("op is '=', type is GenDate");
#ifdef DEBUG
						stab.FormatAppend("; Class = \"%S\", Id = \"%S\", Text = \"%S\"",
							stuAliasClass.Chars(), stuAliasId.Chars(), stuAliasText.Chars());
#endif
						::MessageBox(NULL, stab.Chars(), stabTitle.Chars(),
							MB_OK | MB_ICONWARNING | MB_TASKMODAL);
						ThrowHr(E_ABORT);
#endif
						break;
#endif /*VERSION2FILTER*/
					case kstidFltrLastWeek:
					case kstidFltrLastMonth:
					case kstidFltrLastYear:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLastWeek)
						{
							// How does this differ from Last7Days?
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLastMonth)
						{
							if (--nMonth == 0)
							{
								nMonth = 12;
								if (--nYear == 0)
									nYear = -1;		// 1 BC immediately precedes 1 AD
							}
						}
						else
						{
							Assert(nScope == kstidFltrLastYear);
							if (--nYear == 0)
								nYear = -1;		// 1 BC immediately precedes 1 AD
						}
						// Convert, for example, Feb 30 to Feb 28.
						nDayMax = GetDaysInMonth(nMonth, nYear);
						if (nDay > nDayMax)
							nDay = nDayMax;
						if (nYear < 0)
						{
							// June 1, 1000 BC => -10000731
							nDate = 10000 * nYear - 100 * (13 - nMonth) - (32 - nDay);
						}
						else
						{
							// June 1, 1000 AD => 10000601
							nDate = nDay + 100 * nMonth + 10000 * nYear;
						}
						// Equals Last Week means "within the last week"
						// Equals Last month means "within the last month"
						// Equals Last Year means "within the last year"
						stuCell.FormatAppend(L"(%s / 10) >= %d", stuAliasText.Chars(), nDate);
						break;
#ifdef VERSION2FILTER
					case kstidFltrLast7Days:
					case kstidFltrLast30Days:
					case kstidFltrLast365Days:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLast7Days)
						{
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLast30Days)
						{
							// Adjust for varying length months.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one month ago today", which would simply be "--nMonth;"?
							nDay -= 30;
							while (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else
						{
							// If leap year, adjust by one day if necessary.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one year ago today", which would simply be "--nYear;"?
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth > 2)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									nDay = 1;
									if (++nMonth > 12)
										nMonth = 1;
								}
								else
								{
									++nDay;
								}
							}
							--nYear;
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth < 3)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									++nMonth;
									nDay = 1;
								}
								else
								{
									++nDay;
								}
							}
						}
						goto LGenDateEqual;
#endif /*VERSION2FILTER*/
					default:
						return false;
					}
				}
				break;
			case kcptString:
			case kcptUnicode:
			case kcptBigString:
			case kcptBigUnicode:
			case kcptMultiString:
			case kcptMultiUnicode:
			case kcptMultiBigString:
			case kcptMultiBigUnicode:
			case kfptStText:
				prgchBegin = prgch;
				if (!ParseConditionText(prgch, stuText, fMatchBegin, fMatchEnd, ws))
					return false;
				if (nType == kcptMultiUnicode || nType == kcptMultiString ||
					nType == kcptMultiBigUnicode || nType == kcptMultiBigString)
				{
					if (!ws)
						ws = WsForColumn(vfmn, m_qlpi);
				}
				if (stuText.Length())
				{
					if (ws)
						stuCell.FormatAppend(L"(%s = %d AND ", stuAliasWs.Chars(), ws);
					stuCell.FormatAppend(L"(%s LIKE N'%s' ESCAPE '\\'",
						stuAliasText.Chars(), stuText.Chars());
					StrUni stu1;
					if (fMatchBegin)
					{
						ReparseConditionText(prgchBegin, stu1, true, false);
						if (stu1.Length() && stu1 != stuText)
							stuCell.FormatAppend(L" OR %s LIKE N'%s' ESCAPE '\\'",
								stuAliasText.Chars(), stu1.Chars());
					}
					StrUni stu2;
					if (fMatchEnd)
					{
						ReparseConditionText(prgchBegin, stu2, false, true);
						if (stu2.Length() && stu2 != stuText && stu2 != stu1)
							stuCell.FormatAppend(L" OR %s LIKE N'%s' ESCAPE '\\'",
								stuAliasText.Chars(), stu2.Chars());
					}
					StrUni stu3;
					if (fMatchBegin && fMatchEnd)
					{
						ReparseConditionText(prgchBegin, stu3, true, true);
						if (stu3.Length() && stu3 != stuText && stu3 != stu1 && stu3 != stu2)
							stuCell.FormatAppend(L" OR %s LIKE N'%s' ESCAPE '\\'",
								stuAliasText.Chars(), stu3.Chars());
					}
					stuCell.Append(L")");
					if (ws)
						stuCell.Append(L")");
				}
				else
				{
					// "Equal to ''" is equivalent to "Empty" for naive users (is there any
					// other kind?)
					StrUni stuVal;
					if (stuFullRows.Length())
					{
						if (nType == kcptBigString || nType == kcptBigUnicode ||
							nType == kfptStText || nType == kcptMultiBigString ||
							nType == kcptMultiBigUnicode)
						{
							stuVal.Format(L"SUBSTRING(%s,1,10)", stuAliasText2.Chars());
						}
						else
						{
							stuVal.Assign(stuAliasText2);
						}
						stuCell.FormatAppend(L"NOT EXISTS (%s AND ", stuFullRows.Chars());
						fIgnoreRole = true;
						if (ws)
							stuCell.FormatAppend(L"%s = %d AND ", stuAliasEnc2.Chars(), ws);
						stuCell.FormatAppend(L"%s IS NOT NULL AND %s NOT LIKE '')",
							stuVal.Chars(), stuVal.Chars());
					}
					else
					{
						if (nType == kcptBigString || nType == kcptBigUnicode ||
							nType == kfptStText || nType == kcptMultiBigString ||
							nType == kcptMultiBigUnicode)
						{
							stuVal.Format(L"SUBSTRING(%s,1,10)", stuAliasText.Chars());
						}
						else
						{
							stuVal.Assign(stuAliasText);
						}
						stuCell.FormatAppend(L"(%s IS NULL OR ", stuVal.Chars());
						if (ws)
							stuCell.FormatAppend(L"(%s = %d AND %s LIKE ''))",
								stuAliasWs.Chars(), ws, stuVal.Chars());
						else
							stuCell.FormatAppend(L"%s LIKE '')", stuVal.Chars());
					}
				}
				break;
			case kfptPossList:
			case kfptRoledParticipant:
				hvo = GetNextObject(ptss, prgchMin, prgch);
				if (!hvo)
					return false;
				stuCell.FormatAppend(L"%s = %d", stuAliasId.Chars(), hvo);
				break;
			case kfptTagList:
				Assert(false);		// Undefined: should never happen.
				break;
			case kfptEnumList:
			case kfptEnumListReq:
				if (!ParseEnumText(pfmn, prgch, &nT))
					return false;
				stuCell.FormatAppend(L"%s = %d", stuAliasText.Chars(), nT);
				break;
			default:
				return false;
			}
			break;
		////////////////////////////////////////////////////////////////////////////////////////
		case kfktNotEqual:
			switch (nType)
			{
			case kcptInteger:
				if (!ParseIntegerText(prgch, &nT))
					return false;
				if (stuFullRows.Length())
				{
					stuCell.FormatAppend(L"NOT EXISTS (%s AND %s = %d)",
						stuFullRows.Chars(), stuAliasText2.Chars(), nT);
					fIgnoreRole = true;
				}
				else
				{
					stuCell.FormatAppend(L"(%s IS NULL OR %s <> %d)",
						stuAliasText.Chars(), stuAliasText.Chars(), nT);
				}
				break;
			case kcptNumeric:			// TODO
#ifdef INCOMPLETE_GetCellSQL
				stab.Format("op is '<>', type is Numeric");
#ifdef DEBUG
				stab.FormatAppend("; Class = \"%S\", Id = \"%S\", Text = \"%S\"",
					stuAliasClass.Chars(), stuAliasId.Chars(), stuAliasText.Chars());
#endif
				::MessageBoxA(NULL, stab.Chars(), stabTitle.Chars(),
					MB_OK | MB_ICONWARNING | MB_TASKMODAL);
				ThrowHr(E_ABORT);
#endif
				break;
			case kcptFloat:			// TODO
#ifdef INCOMPLETE_GetCellSQL
				stab.Format("op is '<>', type is Float");
#ifdef DEBUG
				stab.FormatAppend("; Class = \"%S\", Id = \"%S\", Text = \"%S\"",
					stuAliasClass.Chars(), stuAliasId.Chars(), stuAliasText.Chars());
#endif
				::MessageBoxA(NULL, stab.Chars(), stabTitle.Chars(),
					MB_OK | MB_ICONWARNING | MB_TASKMODAL);
				ThrowHr(E_ABORT);
#endif
				break;
			case kcptTime:
				if (stuFullRows.Length())
				{
					int nScope;
					int nYear;
					int nMonth;
					int nDay;
					int nDayMax;
					if (!ParseDateText(prgch, nScope, nYear, nMonth, nDay))
						return false;
					stuCell.FormatAppend(L"NOT EXISTS (%s AND ", stuFullRows.Chars());
					fIgnoreRole = true;
					switch (nScope)
					{
					case kstidFltrExactDate:
						stuCell.FormatAppend(L"DATEPART(YEAR, %s) = %d AND "
							L"DATEPART(MONTH, %s) = %d AND DATEPART(DAY, %s) = %d",
							stuAliasText2.Chars(), nYear, stuAliasText2.Chars(), nMonth,
							stuAliasText2.Chars(), nDay);
						break;
#ifdef VERSION2FILTER
					case kstidFltrMonth:
						stuCell.FormatAppend(L"DATEPART(MONTH, %s) = %d",
							stuAliasText2.Chars(), nMonth);
						break;
#endif /*VERSION2FILTER*/
					case kstidFltrMonthYear:
						stuCell.FormatAppend(L"DATEPART(YEAR, %s) = %d AND "
							L"DATEPART(MONTH, %s) = %d",
							stuAliasText2.Chars(), nYear, stuAliasText2.Chars(), nMonth);
						break;
					case kstidFltrYear:
						stuCell.FormatAppend(L"DATEPART(YEAR, %s) = %d",
							stuAliasText2.Chars(), nYear);
						break;
#ifdef VERSION2FILTER
					case kstidFltrDay:
						stuCell.FormatAppend(L"DATEPART(DAY, %s) = %d",
							stuAliasText2.Chars(), nDay);
						break;
#endif /*VERSION2FILTER*/
					case kstidFltrToday:
						stuCell.FormatAppend(L"DATEPART(YEAR, %s) = %d AND "
							L"DATEPART(MONTH, %s) = %d AND DATEPART(DAY, %s) = %d",
							stuAliasText2.Chars(), systimeNow.wYear,
							stuAliasText2.Chars(), systimeNow.wMonth,
							stuAliasText2.Chars(), systimeNow.wDay);
						break;
					case kstidFltrLastWeek:
					case kstidFltrLastMonth:
					case kstidFltrLastYear:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLastWeek)
						{
							// How does this differ from Last7Days?
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLastMonth)
						{
							if (--nMonth == 0)
							{
								nMonth = 12;
								if (--nYear == 0)
									nYear = -1;		// 1 BC immediately precedes 1 AD
							}
						}
						else
						{
							Assert(nScope == kstidFltrLastYear);
							if (--nYear == 0)
								nYear = -1;		// 1 BC immediately precedes 1 AD
						}
						// Convert, for example, Feb 30 to Feb 28.
						nDayMax = GetDaysInMonth(nMonth, nYear);
						if (nDay > nDayMax)
							nDay = nDayMax;
						{
							// Does Not Equal Last Week means "before the last week"
							// Does Not Equal Last month means "before the last month"
							// Does Not Equal Last Year means "before the last year"
							Assert(!stuFullRows.Length());
							StrUni stuYear;
							StrUni stuMonth;
							StrUni stuDay;
							stuYear.Format(L"DATEPART(YEAR, %s)", stuAliasText2.Chars());
							stuMonth.Format(L"DATEPART(MONTH, %s)", stuAliasText2.Chars());
							stuDay.Format(L"DATEPART(DAY, %s)", stuAliasText2.Chars());
							stuCell.FormatAppend(L"(%s > %d OR (%s = %d AND "
								L"(%s > %d OR (%s = %d AND %s >= %d))))",
								stuYear.Chars(), nYear, stuYear.Chars(), nYear,
								stuMonth.Chars(), nMonth, stuMonth.Chars(), nMonth,
								stuDay.Chars(), nDay);
						}
						break;

#ifdef VERSION2FILTER
					case kstidFltrLast7Days:
					case kstidFltrLast30Days:
					case kstidFltrLast365Days:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLast7Days)
						{
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLast30Days)
						{
							// Adjust for varying length months.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one month ago today", which would simply be "--nMonth;"?
							nDay -= 30;
							while (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else
						{
							// If leap year, adjust by one day if necessary.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one year ago today", which would simply be "--nYear;"?
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth > 2)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									nDay = 1;
									if (++nMonth > 12)
										nMonth = 1;
								}
								else
								{
									++nDay;
								}
							}
							--nYear;
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth < 3)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									++nMonth;
									nDay = 1;
								}
								else
								{
									++nDay;
								}
							}
						}
						goto LMultiTimeNotEqual;
#endif /*VERSION2FILTER*/
					default:
						return false;
					}
					stuCell.Append(L")");
				}
				else
				{
					int nScope;
					int nYear;
					int nMonth;
					int nDay;
					int nDayMax;
					if (!ParseDateText(prgch, nScope, nYear, nMonth, nDay))
						return false;
					switch (nScope)
					{
					case kstidFltrExactDate:
						stuCell.FormatAppend(L"%s IS NULL OR DATEPART(YEAR, %s) <> %d OR "
							L"DATEPART(MONTH, %s) <> %d OR DATEPART(DAY, %s) <> %d",
							stuAliasText.Chars(), stuAliasText.Chars(), nYear,
							stuAliasText.Chars(), nMonth, stuAliasText.Chars(), nDay);
						break;
#ifdef VERSION2FILTER
					case kstidFltrMonth:
						stuCell.FormatAppend(L"%s IS NULL OR DATEPART(MONTH, %s) <> %d",
							stuAliasText.Chars(), stuAliasText.Chars(), nMonth);
						break;
#endif /*VERSION2FILTER*/
					case kstidFltrMonthYear:
						stuCell.FormatAppend(L"%s IS NULL OR DATEPART(YEAR, %s) <> %d OR "
							L"DATEPART(MONTH, %s) <> %d",
							stuAliasText.Chars(),
							stuAliasText.Chars(), nYear, stuAliasText.Chars(), nMonth);
						break;
					case kstidFltrYear:
						stuCell.FormatAppend(L"%s IS NULL OR DATEPART(YEAR, %s) <> %d",
							stuAliasText.Chars(), stuAliasText.Chars(), nYear);
						break;
#ifdef VERSION2FILTER
					case kstidFltrDay:
						stuCell.FormatAppend(L"%s IS NULL OR DATEPART(DAY, %s) <> %d",
							stuAliasText.Chars(), stuAliasText.Chars(), nDay);
						break;
#endif /*VERSION2FILTER*/
					case kstidFltrToday:
						stuCell.FormatAppend(L"%s IS NULL OR DATEPART(YEAR, %s) <> %d OR "
							L"DATEPART(MONTH, %s) <> %d OR DATEPART(DAY, %s) <> %d",
							stuAliasText.Chars(), stuAliasText.Chars(), systimeNow.wYear,
							stuAliasText.Chars(), systimeNow.wMonth, stuAliasText.Chars(),
							systimeNow.wDay);
						break;
					case kstidFltrLastWeek:
					case kstidFltrLastMonth:
					case kstidFltrLastYear:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLastWeek)
						{
							// How does this differ from Last7Days?
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLastMonth)
						{
							if (--nMonth == 0)
							{
								nMonth = 12;
								if (--nYear == 0)
									nYear = -1;		// 1 BC immediately precedes 1 AD
							}
						}
						else
						{
							Assert(nScope == kstidFltrLastYear);
							if (--nYear == 0)
								nYear = -1;		// 1 BC immediately precedes 1 AD
						}
						// Convert, for example, Feb 30 to Feb 28.
						nDayMax = GetDaysInMonth(nMonth, nYear);
						if (nDay > nDayMax)
							nDay = nDayMax;
						{
							// Does Not Equal Last Week means "before the last week"
							// Does Not Equal Last month means "before the last month"
							// Does Not Equal Last Year means "before the last year"
							StrUni stuYear;
							StrUni stuMonth;
							StrUni stuDay;
							stuYear.Format(L"DATEPART(YEAR, %s)", stuAliasText.Chars());
							stuMonth.Format(L"DATEPART(MONTH, %s)", stuAliasText.Chars());
							stuDay.Format(L"DATEPART(DAY, %s)", stuAliasText.Chars());
							stuCell.FormatAppend(L"(%s < %d OR (%s = %d AND "
								L"(%s < %d OR (%s = %d AND %s < %d))))",
								stuYear.Chars(), nYear, stuYear.Chars(), nYear,
								stuMonth.Chars(), nMonth, stuMonth.Chars(), nMonth,
								stuDay.Chars(), nDay);
						}
						break;
#ifdef VERSION2FILTER
					case kstidFltrLast7Days:
					case kstidFltrLast30Days:
					case kstidFltrLast365Days:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLast7Days)
						{
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLast30Days)
						{
							// Adjust for varying length months.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one month ago today", which would simply be "--nMonth;"?
							nDay -= 30;
							while (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else
						{
							// If leap year, adjust by one day if necessary.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one year ago today", which would simply be "--nYear;"?
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth > 2)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									nDay = 1;
									if (++nMonth > 12)
										nMonth = 1;
								}
								else
								{
									++nDay;
								}
							}
							--nYear;
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth < 3)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									++nMonth;
									nDay = 1;
								}
								else
								{
									++nDay;
								}
							}
						}
						goto LTimeNotEqual;
#endif /*VERSION2FILTER*/
					default:
						return false;
					}
				}
				break;
			case kcptGenDate:
				if (stuFullRows.Length())
				{
					int nScope;
					int nYear;
					int nMonth;
					int nDay;
					int nDayMax;
					if (!ParseDateText(prgch, nScope, nYear, nMonth, nDay))
						return false;
					int nDate;
					stuCell.FormatAppend(L"NOT EXISTS (%s AND ", stuFullRows.Chars());
					fIgnoreRole = true;
					switch (nScope)
					{
					case kstidFltrExactDate:
LMultiGenDateNotEqual:
						if (nYear < 0)
						{
							// June 1, 1000 BC => -10000731
							nDate = 10000 * nYear - 100 * (13 - nMonth) - (32 - nDay);
						}
						else
						{
							// June 1, 1000 AD => 10000601
							nDate = nDay + 100 * nMonth + 10000 * nYear;
						}
						stuCell.FormatAppend(L"(%s / 10) = %d", stuAliasText2.Chars(), nDate);
						break;
					case kstidFltrMonthYear:
						if (nYear < 0)
						{
							// June, 1000 BC => -100007
							nDate = 100 * nYear - (13 - nMonth);
						}
						else
						{
							// June, 1000 AD => 100006
							nDate = 100 * nYear + nMonth;
						}
						stuCell.FormatAppend(L"(%s / 1000) = %d", stuAliasText2.Chars(), nDate);
						break;

					case kstidFltrYear:
						// 1000 BC => -1000
						// 1000 AD => 1000
						stuCell.FormatAppend(L"(%s / 100000) = %d",
							stuAliasText2.Chars(), nYear);
						break;

					case kstidFltrToday:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						goto LMultiGenDateNotEqual;

#ifdef VERSION2FILTER
					case kstidFltrMonth:
					case kstidFltrDay:
#ifdef INCOMPLETE_GetCellSQL
						stab.Format("op is '<>', type is GenDate");
#ifdef DEBUG
						stab.FormatAppend("; Class = \"%S\", Id = \"%S\", Text = \"%S\"",
							stuAliasClass.Chars(), stuAliasId.Chars(), stuAliasText.Chars());
#endif
						::MessageBox(NULL, stab.Chars(), stabTitle.Chars(),
							MB_OK | MB_ICONWARNING | MB_TASKMODAL);
						ThrowHr(E_ABORT);
#endif
						break;
#endif /*VERSION2FILTER*/
					case kstidFltrLastWeek:
					case kstidFltrLastMonth:
					case kstidFltrLastYear:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLastWeek)
						{
							// How does this differ from Last7Days?
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLastMonth)
						{
							if (--nMonth == 0)
							{
								nMonth = 12;
								if (--nYear == 0)
									nYear = -1;		// 1 BC immediately precedes 1 AD
							}
						}
						else
						{
							Assert(nScope == kstidFltrLastYear);
							if (--nYear == 0)
								nYear = -1;		// 1 BC immediately precedes 1 AD
						}
						// Convert, for example, Feb 30 to Feb 28.
						nDayMax = GetDaysInMonth(nMonth, nYear);
						if (nDay > nDayMax)
							nDay = nDayMax;
						if (nYear < 0)
						{
							// June 1, 1000 BC => -10000731
							nDate = 10000 * nYear - 100 * (13 - nMonth) - (32 - nDay);
						}
						else
						{
							// June 1, 1000 AD => 10000601
							nDate = nDay + 100 * nMonth + 10000 * nYear;
						}
						// Does Not Equal Last Week means "before the last week"
						// Does Not Equal Last month means "before the last month"
						// Does Not Equal Last Year means "before the last year"
						stuCell.FormatAppend(L"(%s / 10) >= %d", stuAliasText2.Chars(), nDate);
						break;
#ifdef VERSION2FILTER
					case kstidFltrLast7Days:
					case kstidFltrLast30Days:
					case kstidFltrLast365Days:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLast7Days)
						{
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLast30Days)
						{
							// Adjust for varying length months.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one month ago today", which would simply be "--nMonth;"?
							nDay -= 30;
							while (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else
						{
							// If leap year, adjust by one day if necessary.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one year ago today", which would simply be "--nYear;"?
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth > 2)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									nDay = 1;
									if (++nMonth > 12)
										nMonth = 1;
								}
								else
								{
									++nDay;
								}
							}
							--nYear;
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth < 3)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									++nMonth;
									nDay = 1;
								}
								else
								{
									++nDay;
								}
							}
						}
						goto LMultiGenDateNotEqual;
#endif /*VERSION2FILTER*/
					default:
						return false;
					}
					stuCell.Append(L")");
				}
				else
				{
					int nScope;
					int nYear;
					int nMonth;
					int nDay;
					int nDayMax;
					if (!ParseDateText(prgch, nScope, nYear, nMonth, nDay))
						return false;
					int nDate;
					switch (nScope)
					{
					case kstidFltrExactDate:
LGenDateNotEqual:
						if (nYear < 0)
						{
							// June 1, 1000 BC => -10000731
							nDate = 10000 * nYear - 100 * (13 - nMonth) - (32 - nDay);
						}
						else
						{
							// June 1, 1000 AD => 10000601
							nDate = nDay + 100 * nMonth + 10000 * nYear;
						}
						stuCell.FormatAppend(L"%s is NULL OR (%s / 10) <> %d",
							stuAliasText.Chars(), stuAliasText.Chars(), nDate);
						break;
					case kstidFltrMonthYear:
						if (nYear < 0)
						{
							// June, 1000 BC => -100007
							nDate = 100 * nYear - (13 - nMonth);
						}
						else
						{
							// June, 1000 AD => 100006
							nDate = 100 * nYear + nMonth;
						}
						stuCell.FormatAppend(L"%s is NULL OR (%s / 1000) <> %d",
							stuAliasText.Chars(), stuAliasText.Chars(), nDate);
						break;

					case kstidFltrYear:
						// 1000 BC => -1000
						// 1000 AD => 1000
						stuCell.FormatAppend(L"%s is NULL OR (%s / 100000) <> %d",
							stuAliasText.Chars(), stuAliasText.Chars(), nYear);
						break;

					case kstidFltrToday:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						goto LGenDateNotEqual;

#ifdef VERSION2FILTER
					case kstidFltrMonth:
					case kstidFltrDay:
#ifdef INCOMPLETE_GetCellSQL
						stab.Format("op is '<>', type is GenDate");
#ifdef DEBUG
						stab.FormatAppend("; Class = \"%S\", Id = \"%S\", Text = \"%S\"",
							stuAliasClass.Chars(), stuAliasId.Chars(), stuAliasText.Chars());
#endif
						::MessageBox(NULL, stab.Chars(), stabTitle.Chars(),
							MB_OK | MB_ICONWARNING | MB_TASKMODAL);
						ThrowHr(E_ABORT);
#endif
						break;
#endif /*VERSION2FILTER*/
					case kstidFltrLastWeek:
					case kstidFltrLastMonth:
					case kstidFltrLastYear:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLastWeek)
						{
							// How does this differ from Last7Days?
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLastMonth)
						{
							if (--nMonth == 0)
							{
								nMonth = 12;
								if (--nYear == 0)
									nYear = -1;		// 1 BC immediately precedes 1 AD
							}
						}
						else
						{
							Assert(nScope == kstidFltrLastYear);
							if (--nYear == 0)
								nYear = -1;		// 1 BC immediately precedes 1 AD
						}
						// Convert, for example, Feb 30 to Feb 28.
						nDayMax = GetDaysInMonth(nMonth, nYear);
						if (nDay > nDayMax)
							nDay = nDayMax;
						if (nYear < 0)
						{
							// June 1, 1000 BC => -10000731
							nDate = 10000 * nYear - 100 * (13 - nMonth) - (32 - nDay);
						}
						else
						{
							// June 1, 1000 AD => 10000601
							nDate = nDay + 100 * nMonth + 10000 * nYear;
						}
						// Does Not Equal Last Week means "before the last week"
						// Does Not Equal Last month means "before the last month"
						// Does Not Equal Last Year means "before the last year"
						stuCell.FormatAppend(L"%s is NULL OR (%s / 10) < %d",
							stuAliasText.Chars(), stuAliasText.Chars(), nDate);
						break;
#ifdef VERSION2FILTER
					case kstidFltrLast7Days:
					case kstidFltrLast30Days:
					case kstidFltrLast365Days:
						nYear = systimeNow.wYear;
						nMonth = systimeNow.wMonth;
						nDay = systimeNow.wDay;
						if (nScope == kstidFltrLast7Days)
						{
							nDay -= 7;
							if (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else if (nScope == kstidFltrLast30Days)
						{
							// Adjust for varying length months.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one month ago today", which would simply be "--nMonth;"?
							nDay -= 30;
							while (nDay <= 0)
							{
								if (--nMonth == 0)
								{
									nMonth = 12;
									if (--nYear == 0)
										nYear = -1;		// 1 BC immediately precedes 1 AD
								}
								nDay += GetDaysInMonth(nMonth, nYear);
							}
						}
						else
						{
							// If leap year, adjust by one day if necessary.
							// REVIEW SteveMc: Is this wanted, or do they really mean
							// "one year ago today", which would simply be "--nYear;"?
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth > 2)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									nDay = 1;
									if (++nMonth > 12)
										nMonth = 1;
								}
								else
								{
									++nDay;
								}
							}
							--nYear;
							if (!(nYear % 4) && (!(nYear % 400) || (nYear % 100)) &&
								nMonth < 3)
							{
								if (nDay >= GetDaysInMonth(nMonth, nYear))
								{
									++nMonth;
									nDay = 1;
								}
								else
								{
									++nDay;
								}
							}
						}
						goto LGenDateNotEqual;
#endif /*VERSION2FILTER*/
					default:
						return false;
					}
				}
				break;
			case kcptString:
			case kcptUnicode:
			case kcptBigString:
			case kcptBigUnicode:
			case kcptMultiString:
			case kcptMultiUnicode:
			case kcptMultiBigString:
			case kcptMultiBigUnicode:
			case kfptStText:
				prgchBegin = prgch;
				if (!ParseConditionText(prgch, stuText, fMatchBegin, fMatchEnd, ws))
					return false;
				if (nType == kcptMultiUnicode || nType == kcptMultiString ||
					nType == kcptMultiBigUnicode || nType == kcptMultiBigString)
				{
					if (!ws)
						ws = WsForColumn(vfmn, m_qlpi);
				}
				if (stuText.Length())
				{
					StrUni stuVal;
					StrUni stuWs;
					if (stuFullRows.Length())
					{
						if (nType == kcptBigString || nType == kcptBigUnicode ||
							nType == kfptStText || nType == kcptMultiBigString ||
							nType == kcptMultiBigUnicode)
						{
							stuVal.Format(L"SUBSTRING(%s,1,10)", stuAliasText2.Chars());
						}
						else
						{
							stuVal.Assign(stuAliasText2);
						}
						stuWs = stuAliasEnc2;
						stuCell.FormatAppend(L"NOT EXISTS (%s AND ", stuFullRows.Chars());
						fIgnoreRole = true;
					}
					else
					{
						if (nType == kcptBigString || nType == kcptBigUnicode ||
							nType == kfptStText || nType == kcptMultiBigString ||
							nType == kcptMultiBigUnicode)
						{
							stuVal.Format(L"SUBSTRING(%s,1,10)", stuAliasText.Chars());
						}
						else
						{
							stuVal.Assign(stuAliasText);
						}
						stuWs = stuAliasWs;
						stuCell.FormatAppend(L"(%s IS NULL OR NOT ", stuVal.Chars());
					}
					if (ws)
						stuCell.FormatAppend(L"(%s = %d AND ", stuWs.Chars(), ws);
					stuCell.FormatAppend(L"(%s LIKE N'%s' ESCAPE '\\'",
						stuVal.Chars(), stuText.Chars());
					StrUni stu1;
					if (fMatchBegin)
					{
						ReparseConditionText(prgchBegin, stu1, true, false);
						if (stu1.Length() && stu1 != stuText)
							stuCell.FormatAppend(L" OR %s LIKE N'%s' ESCAPE '\\'",
								stuVal.Chars(), stu1.Chars());
					}
					StrUni stu2;
					if (fMatchEnd)
					{
						ReparseConditionText(prgchBegin, stu2, false, true);
						if (stu2.Length() && stu2 != stuText && stu2 != stu1)
							stuCell.FormatAppend(L" OR %s LIKE N'%s' ESCAPE '\\'",
								stuVal.Chars(), stu2.Chars());
					}
					StrUni stu3;
					if (fMatchBegin && fMatchEnd)
					{
						ReparseConditionText(prgchBegin, stu3, true, true);
						if (stu3.Length() && stu3 != stuText && stu3 != stu1 && stu3 != stu2)
							stuCell.FormatAppend(L" OR %s LIKE N'%s' ESCAPE '\\'",
								stuVal.Chars(), stu3.Chars());
					}
					stuCell.Append(L")");
					if (ws)
						stuCell.Append(L")");
					stuCell.FormatAppend(L")");
				}
				else
				{
					// "Not Equal To ''" is equivalent to "Not Empty" for naive users
					// (is there any other kind?)
					StrUni stuVal;
					if (nType == kcptBigString || nType == kcptBigUnicode ||
						nType == kfptStText || nType == kcptMultiBigString ||
						nType == kcptMultiBigUnicode)
					{
						stuVal.Format(L"SUBSTRING(%s,1,10)", stuAliasText.Chars());
					}
					else
					{
						stuVal.Assign(stuAliasText);
					}
					if (ws)
						stuCell.FormatAppend(L"%s = %d AND ", stuAliasWs.Chars(), ws);
					stuCell.FormatAppend(L"%s IS NOT NULL AND %s NOT LIKE ''",
						stuVal.Chars(), stuVal.Chars());
				}
				break;
			case kfptPossList:
			case kfptRoledParticipant:
				hvo = GetNextObject(ptss, prgchMin, prgch);
				if (!hvo)
					return false;
				if (stuFullRows.Length())
				{
					stuCell.FormatAppend(L"NOT EXISTS (%s AND %s = %d)",
						stuFullRows.Chars(), stuAliasId2.Chars(), hvo);
					fIgnoreRole = true;
				}
				else
				{
					stuCell.FormatAppend(L"%s IS NULL OR %s <> %d",
						stuAliasId.Chars(), stuAliasId.Chars(), hvo);
				}
				break;
			case kfptTagList:
				Assert(false);		// Undefined: should never happen.
				break;
			case kfptEnumList:
			case kfptEnumListReq:
				if (!ParseEnumText(pfmn, prgch, &nT))
					return false;
				if (stuFullRows.Length())
				{
					stuCell.FormatAppend(L"NOT EXISTS (%s AND %s = %d)",
						stuFullRows.Chars(), stuAliasId2.Chars(), nT);
					fIgnoreRole = true;
				}
				else
				{
					stuCell.FormatAppend(L"%s IS NULL OR %s <> %d",
						stuAliasText.Chars(), stuAliasText.Chars(), nT);
				}
				break;
			default:
				return false;
			}
			break;
		////////////////////////////////////////////////////////////////////////////////////////
		case kfktGT:
		case kfktLT:
		case kfktGTE:
		case kfktLTE:
			pszSymbol = LookupSymbol(fkt);
			Assert(pszSymbol);
			if (stuFullRows.Length())
			{
				switch (fkt)
				{
				case kfktGT:
					pszSymbol2 = LookupSymbol(kfktLTE);
					pszStrict2 = LookupSymbol(kfktLT);
					break;
				case kfktLT:
					pszSymbol2 = LookupSymbol(kfktGTE);
					pszStrict2 = LookupSymbol(kfktGT);
					break;
				case kfktGTE:
					pszSymbol2 = LookupSymbol(kfktLT);
					pszStrict2 = LookupSymbol(kfktLT);
					break;
				case kfktLTE:
					pszSymbol2 = LookupSymbol(kfktGT);
					pszStrict2 = LookupSymbol(kfktGT);
					break;
				}
				AssertPtr(pszSymbol2);
				AssertPtr(pszStrict2);
				stuCell.FormatAppend(L"NOT EXISTS (%s AND ", stuFullRows.Chars());
				fIgnoreRole = true;
			}
			else
			{
				switch (fkt)
				{
				case kfktGT:
					pszStrict = pszSymbol;
					break;
				case kfktLT:
					pszStrict = pszSymbol;
					break;
				case kfktGTE:
					pszStrict = LookupSymbol(kfktGT);
					break;
				case kfktLTE:
					pszStrict = LookupSymbol(kfktLT);
					break;
				}
				AssertPtr(pszStrict);
			}
			switch (nType)
			{
			case kcptInteger:
				if (!ParseIntegerText(prgch, &nT))
					return false;
				if (stuFullRows.Length())
					stuCell.FormatAppend(L"%s %s %d", stuAliasText2.Chars(), pszSymbol2, nT);
				else
					stuCell.FormatAppend(L"%s %s %d", stuAliasText.Chars(), pszSymbol, nT);
				break;
			case kcptNumeric:			// TODO
#ifdef INCOMPLETE_GetCellSQL
				stab.Format("op is '%S', type is Numeric", pszSymbol);
#ifdef DEBUG
				stab.FormatAppend("; Class = \"%S\", Id = \"%S\", Text = \"%S\"",
					stuAliasClass.Chars(), stuAliasId.Chars(), stuAliasText.Chars());
#endif
				::MessageBoxA(NULL, stab.Chars(), stabTitle.Chars(),
					MB_OK | MB_ICONWARNING | MB_TASKMODAL);
				ThrowHr(E_ABORT);
#endif
				break;
			case kcptFloat:				// TODO
#ifdef INCOMPLETE_GetCellSQL
				stab.Format("op is '%S', type is Float", pszSymbol);
#ifdef DEBUG
				stab.FormatAppend("; Class = \"%S\", Id = \"%S\", Text = \"%S\"",
					stuAliasClass.Chars(), stuAliasId.Chars(), stuAliasText.Chars());
#endif
				::MessageBoxA(NULL, stab.Chars(), stabTitle.Chars(),
					MB_OK | MB_ICONWARNING | MB_TASKMODAL);
				ThrowHr(E_ABORT);
#endif
				break;
			case kcptTime:
				{
					int nScope;
					int nYear;
					int nMonth;
					int nDay;
					if (!ParseDateText(prgch, nScope, nYear, nMonth, nDay))
						return false;
					StrUni stuYear;
					StrUni stuMonth;
					StrUni stuDay;
					if (stuFullRows.Length())
					{
						stuYear.Format(L"DATEPART(YEAR, %s)", stuAliasText2.Chars());
						stuMonth.Format(L"DATEPART(MONTH, %s)", stuAliasText2.Chars());
						stuDay.Format(L"DATEPART(DAY, %s)", stuAliasText2.Chars());
					}
					else
					{
						stuYear.Format(L"DATEPART(YEAR, %s)", stuAliasText.Chars());
						stuMonth.Format(L"DATEPART(MONTH, %s)", stuAliasText.Chars());
						stuDay.Format(L"DATEPART(DAY, %s)", stuAliasText.Chars());
					}
					switch (nScope)
					{
					case kstidFltrExactDate:
						if (stuFullRows.Length())
						{
							stuCell.FormatAppend(L"(%s %s %d OR (%s = %d AND "
								L"(%s %s %d OR (%s = %d AND %s %s %d))))",
								stuYear.Chars(), pszStrict2, nYear, stuYear.Chars(), nYear,
								stuMonth.Chars(), pszStrict2, nMonth, stuMonth.Chars(), nMonth,
								stuDay.Chars(), pszSymbol2, nDay);
						}
						else
						{
							stuCell.FormatAppend(L"(%s %s %d OR (%s = %d AND "
								L"(%s %s %d OR (%s = %d AND %s %s %d))))",
								stuYear.Chars(), pszStrict, nYear, stuYear.Chars(), nYear,
								stuMonth.Chars(), pszStrict, nMonth, stuMonth.Chars(), nMonth,
								stuDay.Chars(), pszSymbol, nDay);
						}
						break;
#ifdef VERSION2FILTER
					case kstidFltrMonth:
						if (stuFullRows.Length())
						{
							stuCell.FormatAppend(L"(%s %s %d)",
								stuMonth.Chars(), pszSymbol2, nMonth);
						}
						else
						{
							stuCell.FormatAppend(L"%s %s %d",
								stuMonth.Chars(), pszSymbol, nMonth);
						}
						break;
#endif /*VERSION2FILTER*/
					case kstidFltrMonthYear:
						if (stuFullRows.Length())
						{
							stuCell.FormatAppend(L"(%s %s %d OR (%s = %d AND %s %s %d))",
								stuYear.Chars(), pszStrict2, nYear, stuYear.Chars(), nYear,
								stuMonth.Chars(), pszSymbol2, nMonth);
						}
						else
						{
							stuCell.FormatAppend(L"(%s %s %d OR (%s = %d AND %s %s %d))",
								stuYear.Chars(), pszStrict, nYear, stuYear.Chars(), nYear,
								stuMonth.Chars(), pszSymbol, nMonth);
						}
						break;
					case kstidFltrYear:
						if (stuFullRows.Length())
						{
							stuCell.FormatAppend(L"(%s %s %d)",
								stuYear.Chars(), pszSymbol2, nYear);
						}
						else
						{
							stuCell.FormatAppend(L"(%s %s %d)",
								stuYear.Chars(), pszSymbol, nYear);
						}
						break;
#ifdef VERSION2FILTER
					case kstidFltrDay:
						if (stuFullRows.Length())
						{
							stuCell.FormatAppend(L"(%s %s %d)",
								stuDay.Chars(), pszSymbol2, nDay);
						}
						else
						{
							stuCell.FormatAppend(L"(%s %s %d)",
								stuDay.Chars(), pszSymbol, nDay);
						}
						break;
#endif /*VERSION2FILTER*/
					default:
						return false;
					}
				}
				break;
			case kcptGenDate:
				{
					int nScope;
					int nYear;
					int nMonth;
					int nDay;
					if (!ParseDateText(prgch, nScope, nYear, nMonth, nDay))
						return false;
					int nDate;
					switch (nScope)
					{
					case kstidFltrExactDate:
						if (nYear < 0)
						{
							// June 1, 1000 BC => -10000731
							nDate = 10000 * nYear - 100 * (13 - nMonth) - (32 - nDay);
						}
						else
						{
							// June 1, 1000 AD => 10000601
							nDate = nDay + 100 * nMonth + 10000 * nYear;
						}
						stuCell.FormatAppend(L"(%s / 10) %s %d",
							stuAliasText.Chars(), pszSymbol, nDate);
						break;

					case kstidFltrMonthYear:
						if (nYear < 0)
						{
							// June, 1000 BC => -100007
							nDate = 100 * nYear - (13 - nMonth);
						}
						else
						{
							// June, 1000 AD => 100006
							nDate = 100 * nYear + nMonth;
						}
						if (stuFullRows.Length())
						{
							stuCell.FormatAppend(L"((%s / 1000) %s %d)",
								stuAliasText.Chars(), pszSymbol2, nDate);
						}
						else
						{
							stuCell.FormatAppend(L"((%s / 1000) %s %d)",
								stuAliasText.Chars(), pszSymbol, nDate);
						}
						break;

					case kstidFltrYear:
						// 1000 BC => -1000
						// 1000 AD => 1000
						if (stuFullRows.Length())
						{
							stuCell.FormatAppend(L"((%s / 100000) %s %d)",
								stuAliasText.Chars(), pszSymbol2, nYear);
						}
						else
						{
							stuCell.FormatAppend(L"((%s / 100000) %s %d)",
								stuAliasText.Chars(), pszSymbol, nYear);
						}
						break;

					default:
						return false;
					}
				}
				break;
			case kcptString:
			case kcptUnicode:
			case kcptMultiString:
			case kcptMultiUnicode:
				prgchBegin = prgch;
				if (!ParseConditionText(prgch, stuText, fMatchBegin, fMatchEnd, ws))
					return false;
				if (nType == kcptMultiUnicode || nType == kcptMultiString)
				{
					if (!ws)
						ws = WsForColumn(vfmn, m_qlpi);
				}
				if (stuFullRows.Length())
				{
					if (ws)
						stuCell.FormatAppend(L"%s = %d AND (", stuAliasWs.Chars(), ws);
					stuCell.FormatAppend(L"%s %s '%s'",
						stuAliasText.Chars(), pszSymbol2, stuText.Chars());
					if (ws)
						stuCell.Append(L")");
				}
				else
				{
					if (ws)
						stuCell.FormatAppend(L"%s = %d AND (", stuAliasWs.Chars(), ws);
					stuCell.FormatAppend(L"%s %s '%s'",
						stuAliasText.Chars(), pszSymbol, stuText.Chars());
					if (ws)
						stuCell.Append(L")");
				}
				break;
			case kcptBigString:
			case kcptBigUnicode:
			case kcptMultiBigString:
			case kcptMultiBigUnicode:
			case kfptStText:
				{
					// 'Big' strings have to be treated differently because SQL cannot
					// handle comparisons when using that type.
					StrAppBuf str(kstidFltrBadBigStringCompare);
					StrApp strTitle(kstidFltrError);
					::MessageBox(NULL, str.Chars(), strTitle.Chars(),
						MB_OK | MB_ICONWARNING | MB_TASKMODAL);
					ThrowHr(E_ABORT);
				}
				break;
			case kfptPossList:
			case kfptTagList:
			case kfptRoledParticipant:
				Assert(false);		// Undefined: should never happen.
				// REVIEW SteveMc: THIS doesn't make sense as an operation for this data type!
				hvo = GetNextObject(ptss, prgchMin, prgch);
				if (!hvo)
					return false;
				stuCell.FormatAppend(L"%s in (", stuAliasId.Chars());
				AddItemsToString(stuCell, pfmn->m_hvo, fkt, hvo);
				stuCell.Append(L")");
				return false;
				break;
			case kfptEnumList:
			case kfptEnumListReq:
				if (!ParseEnumText(pfmn, prgch, &nT))
					return false;
				if (stuFullRows.Length())
				{
					stuCell.FormatAppend(L"%s %s %d", stuAliasText.Chars(), pszSymbol2, nT);
				}
				else
				{
					stuCell.FormatAppend(L"%s %s %d", stuAliasText.Chars(), pszSymbol, nT);
				}
				break;
			default:
				return false;
			}
			if (stuFullRows.Length())
				stuCell.Append(L")");
			break;
		////////////////////////////////////////////////////////////////////////////////////////
		case kfktMatches:
		case kfktDoesNotMatch:
			switch (nType)
			{
			case kfptPossList:
			case kfptRoledParticipant:
				hvo = GetNextObject(ptss, prgchMin, prgch);
				if (!hvo)
					return false;
				FilterUtil::SkipWhiteSpace(prgch);
				if (!wcsncmp(prgch, L"+subitems", 9))
				{
					prgch += 9;
					wchar * pszEnd;
					if (fkt == kfktMatches)
					{
						stuCell.FormatAppend(L"%s in (%d", stuAliasId.Chars(), hvo);
						pszEnd = L")";
					}
					else if (stuFullRows.Length())
					{
						stuCell.FormatAppend(L"NOT EXISTS (%s AND %s IN (%d",
							stuFullRows.Chars(), stuAliasId2.Chars(), hvo);
						fIgnoreRole = true;
						pszEnd = L"))";
					}
					else
					{
						stuCell.FormatAppend(L"((%s IS NULL) OR (%s NOT IN (%d",
							stuAliasId.Chars(), stuAliasId.Chars(), hvo);
						pszEnd = L")))";
					}
					// Get the subitems and add them to the list.
					Vector<HVO> vhvoSub;
					GetSubitems(vhvoSub, pfmn->m_hvo, hvo);
					for (int iv = 0; iv < vhvoSub.Size(); ++iv)
						stuCell.FormatAppend(L", %d", vhvoSub[iv]);
					stuCell.Append(pszEnd);
				}
				else if (fkt == kfktMatches)
				{
					stuCell.FormatAppend(L"%s = %d", stuAliasId.Chars(), hvo);
				}
				else if (stuFullRows.Length())
				{
					stuCell.FormatAppend(L"NOT EXISTS (%s AND %s = %d)",
						stuFullRows.Chars(), stuAliasId2.Chars(), hvo);
					fIgnoreRole = true;
				}
				else
				{
					stuCell.FormatAppend(L"%s <> %d OR %s IS NULL",
						stuAliasId.Chars(), hvo, stuAliasId.Chars());
				}
				break;
			case kfptTagList:
				hvo = GetNextObject(ptss, prgchMin, prgch);
				if (!hvo)
					return false;
				FilterUtil::SkipWhiteSpace(prgch);
				if (fkt == kfktDoesNotMatch)
				{
					Assert(hvoOverlay);
					stuCell.FormatAppend(L"(%s IN (SELECT Dst FROM CmOverlay_PossItems"
						L" WHERE Src = %d)) AND ",
						stuAliasId.Chars(), hvoOverlay);
				}
				if (!wcsncmp(prgch, L"+subitems", 9))
				{
					prgch += 9;
					wchar * pszEnd;
					if (fkt == kfktMatches)
					{
						stuCell.FormatAppend(L"%s in (%d", stuAliasId.Chars(), hvo);
						pszEnd = L")";
					}
					else if (stuFullRows.Length())
					{
						stuCell.FormatAppend(L"NOT EXISTS (%s AND %s IN (%d",
							stuFullRows.Chars(), stuAliasId2.Chars(), hvo);
						fIgnoreRole = true;
						pszEnd = L"))";
					}
					else
					{
						stuCell.FormatAppend(L"(%s NOT IN (%d", stuAliasId.Chars(), hvo);
						pszEnd = L"))";
					}
					// Get the subitems and add them to the list.
					Vector<HVO> vhvoSub;
					GetSubitems(vhvoSub, pfmn->m_hvo, hvo);
					for (int iv = 0; iv < vhvoSub.Size(); ++iv)
						stuCell.FormatAppend(L", %d", vhvoSub[iv]);
					stuCell.Append(pszEnd);
				}
				else if (fkt == kfktMatches)
				{
					stuCell.FormatAppend(L"%s = %d", stuAliasId.Chars(), hvo);
				}
				else if (stuFullRows.Length())
				{
					stuCell.FormatAppend(L"NOT EXISTS (%s AND %s = %d)",
						stuFullRows.Chars(), stuAliasId2.Chars(), hvo);
					fIgnoreRole = true;
				}
				else
				{
					stuCell.FormatAppend(L"(%s <> %d)", stuAliasId.Chars(), hvo);
				}
				break;
			default:
				return false;
			}
			break;
		////////////////////////////////////////////////////////////////////////////////////////
		case kfktYes:
			Assert(nType == kcptBoolean || nType == kfptBoolean);
			if (stuFullRows.Length())
			{
				stuCell.FormatAppend(L"NOT EXISTS (%s AND (%s = 0 OR %s IS NULL))",
					stuFullRows.Chars(), stuAliasText.Chars(), stuAliasText.Chars());
				fIgnoreRole = true;
			}
			else
			{
				stuCell.FormatAppend(L"%s = 1", stuAliasText.Chars());
			}
			break;
		case kfktNo:
			Assert(nType == kcptBoolean || nType == kfptBoolean);
			if (stuFullRows.Length())
			{
				stuCell.FormatAppend(L"NOT EXISTS (%s AND %s = 1)",
					stuFullRows.Chars(), stuAliasText.Chars());
				fIgnoreRole = true;
			}
			else
			{
				stuCell.FormatAppend(L"%s = 0 or %s is null",
					stuAliasText.Chars(), stuAliasText.Chars());
			}
			break;
		case kfktAnd:
			stuCell.FormatAppend(L"%n  AND%n  ");
			break;
		case kfktOr:
			stuCell.FormatAppend(L"%n  OR%n  ");
			break;
		case kfktOpenParen:
			stuCell.Append("(");
			break;
		case kfktCloseParen:
			stuCell.Append(")");
			break;
		default:
			return false;
		}

		ws = 0;

		FilterUtil::SkipWhiteSpace(prgch);
	}
	if (!fIgnoreRole && stuRole.Length() && stuCell.Length())
	{
		stuCell.Replace(0, 0, L"(");
		stuCell.FormatAppend(L") AND %s", stuRole.Chars());
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Look through the condition text and do appropriate things for user variables and other
	pattern matching type things, including eventually text formatting (How do we do that?!?).
	This method returns false if there is an error while parsing the text.

	@param prgchKey Reference to a pointer to some filter text from a cell that may contain
					SQL pattern matching wildcard characters.
	@param stuText Reference to a string for returning the modified filter text.
	@param fMatchBegin Reference to a flag that we must match the beginning of the field, minus
		an initial [#] wildcard reference.
	@param fMatchEnd Reference to a flag that we must match the end of the field, minus a
		trailing [#] wildcard reference.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool WhereQueryBuilder::ParseConditionText(const wchar *& prgchKey, StrUni & stuText,
	bool & fMatchBegin, bool & fMatchEnd, int & ws)
{
	AssertPtr(prgchKey);
	stuText.Clear();

	StrApp str;
	StrApp strTitle(kstidFltrError);

	// Extract any writing system value.
	ILgWritingSystemFactoryPtr qwsf;
	m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	ws = FilterUtil::ParseWritingSystem(prgchKey, qwsf);

	// The next non-space character must be a double quote.
	FilterUtil::SkipWhiteSpace(prgchKey);
	if (*prgchKey != '"')
		return false;

	// Find the matching double quote. If there isn't one, return false.
	const wchar * prgchLim = prgchKey + 1;

	while (*prgchLim != 0 && *prgchLim != '"')
	{
		if (*prgchLim == '\\')
		{
			if (prgchLim[1] == '\\' || prgchLim[1] == '"')
				++prgchLim;
		}
		++prgchLim;
	}
	if (*prgchLim != '"')
		return false;

	Vector<FilterPatternInfo> & vfpi = g_fu.GetLanguageVariables();
	int cfpi = vfpi.Size();

	Assert(prgchKey + 1 <= prgchLim);
	const wchar * prgch = prgchKey + 1;
	const wchar * prgchRun = prgch;
	wchar ch;
	fMatchBegin = false;
	fMatchEnd = false;
	while (prgch < prgchLim)
	{
		ch = *prgch;
		// Take care of special patterns.
		if (ch == '[')
		{
			stuText.Append(prgchRun, prgch - prgchRun);

			const wchar * prgchPatLim = prgch + 1;
			while (*prgchPatLim && *prgchPatLim != ']')
				prgchPatLim++;
			if (*prgchPatLim != ']')
			{
				stuText.Append(L"[[]");
				prgchRun = prgch + 1;
			}
			else
			{
				// Lookup the abbreviation and see what the replacement text for it is.
				int cchPat = prgchPatLim - prgch - 1;
				int ifpi;
				for (ifpi = 0; ifpi < cfpi; ifpi++)
				{
					if (wcsncmp(vfpi[ifpi].m_stuAbbrev, prgch + 1, cchPat) == 0)
						break;
				}
				if (ifpi < cfpi)
				{
					if (cchPat == 1 && prgch[1] == '#')
					{
						if (prgch == prgchKey + 1)
							fMatchBegin = true;
						if (prgchPatLim == prgchLim - 1)
							fMatchEnd = true;
					}
					// Since ' characters need to be doubled, the maximum possible length is
					// double the length of the abbreviation.
					wchar * prgchDst;
					StrUni stuReplace;
					stuReplace.SetSize(vfpi[ifpi].m_stuReplace.Length() * 2, &prgchDst);
					const wchar * prgchSrc = vfpi[ifpi].m_stuReplace.Chars();
					while (*prgchSrc)
					{
						if (*prgchSrc == '\'')
							*prgchDst++ = '\'';
						*prgchDst++ = *prgchSrc++;
					}
					stuReplace.SetSize(prgchDst - stuReplace.Chars(), &prgchDst);

					stuText.Append(stuReplace);
					prgch = prgchPatLim;
					prgchRun = prgchPatLim + 1;
				}
				else
				{
					// ENHANCE: AT SOME POINT, WE WILL WANT TO ASK THE USER WHETHER THIS IS
					// MEANT TO BE LITERAL, OR IS A MISSPELLING.  (PREFERABLY AT DEFINITION
					// TIME, NOT INVOCATION TIME.)
					stuText.Append(L"[[]");
					prgchRun = prgch + 1;
				}
			}
		}
		else if (ch == '_')
		{
			// SQL treats _ as a special character, so we have to put brackets around it.
			stuText.Append(prgchRun, prgch - prgchRun);
			stuText.Append(L"[_]");
			prgchRun = prgch + 1;
		}
		else if (ch == '%')
		{
			// SQL treats % as a special character, so we have to put brackets around it.
			stuText.Append(prgchRun, prgch - prgchRun);
			stuText.Append(L"[%]");
			prgchRun = prgch + 1;
		}
		else if (ch == '\'')
		{
			// Since the string is enclosed with quotes, we have to put two quotes together
			// so SQL treats it properly.
			stuText.Append(prgchRun, prgch - prgchRun);
			stuText.Append(L"''");
			prgchRun = prgch + 1;
		}
		else if (ch == '\\' && prgch < prgchLim - 1)
		{
			wchar chT = prgch[1];
			if (chT == '"')
			{
				// Convert \" combinations back to just a plain ".
				stuText.Append(prgchRun, prgch - prgchRun);
				prgchRun = prgch++ + 1; // Keep the " in prgchRun.
			}
			else if (chT == '\\')
			{
				// SQL will treat \ as a special character, so leave it doubled.
				stuText.Append(prgchRun, prgch - prgchRun);
				stuText.Append(L"\\\\");
				prgchRun = ++prgch + 1;
			}
		}
		prgch++;
	}
	stuText.Append(prgchRun, prgch - prgchRun);
	StrUtil::NormalizeStrUni(stuText, UNORM_NFD);

	prgchKey = prgchLim + 1;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Look through the condition text and do appropriate things for user variables and other
	pattern matching type things, including eventually text formatting (How do we do that?!?).

	@param prgchKey Pointer to some filter text from a cell that contains SQL pattern matching
					wildcard characters.
	@param stuText Reference to a string for returning the modified filter text.
	@param fBegin Flag whether to skip an initial [#] wildcard reference.
	@param fEnd Flag whether to skip a trailing [#] wildcard reference.
----------------------------------------------------------------------------------------------*/
void WhereQueryBuilder::ReparseConditionText(const wchar * prgchKey, StrUni & stuText, bool fBegin,
	bool fEnd)
{
	AssertPtr(prgchKey);
	stuText.Clear();

	StrApp str;
	StrApp strTitle(kstidFltrError);

	// Skip over any writing system value.
	FilterUtil::SkipWhiteSpace(prgchKey);
	if (*prgchKey == '[')
	{
		++prgchKey;
		while (*prgchKey != 0 && *prgchKey != ']')
			++prgchKey;
		if (*prgchKey == ']')
			++prgchKey;
	}
	// The next non-space character must be a double quote.
	FilterUtil::SkipWhiteSpace(prgchKey);
	if (*prgchKey != '"')
		return;

	// Find the matching double quote. If there isn't one, return false.
	const wchar * prgchLim = prgchKey + 1;

	while (*prgchLim != 0 && *prgchLim != '"')
	{
		if (*prgchLim == '\\')
		{
			if (prgchLim[1] == '\\' || prgchLim[1] == '"')
				++prgchLim;
		}
		++prgchLim;
	}
	if (*prgchLim != '"')
		return;

	Vector<FilterPatternInfo> & vfpi = g_fu.GetLanguageVariables();
	int cfpi = vfpi.Size();

	Assert(prgchKey + 1 <= prgchLim);
	const wchar * prgch = prgchKey + 1;
	const wchar * prgchRun = prgch;
	wchar ch;
	if (fBegin)
	{
		if (wcsncmp(prgch, L"[#]", 3) != 0)
			return;
		prgch += 3;
		prgchRun += 3;
	}
	if (fEnd)
	{
		if (wcsncmp(prgchLim - 3, L"[#]", 3) != 0)
			return;
		prgchLim -= 3;
	}
	while (prgch < prgchLim)
	{
		ch = *prgch;
		// Take care of special patterns.
		if (ch == '[')
		{
			stuText.Append(prgchRun, prgch - prgchRun);

			const wchar * prgchPatLim = prgch + 1;
			while (*prgchPatLim && *prgchPatLim != ']')
				prgchPatLim++;
			if (*prgchPatLim != ']')
			{
				stuText.Append(L"[[]");
				prgchRun = prgch + 1;
			}
			else
			{
				// Lookup the abbreviation and see what the replacement text for it is.
				int cchPat = prgchPatLim - prgch - 1;
				int ifpi;
				for (ifpi = 0; ifpi < cfpi; ifpi++)
				{
					if (wcsncmp(vfpi[ifpi].m_stuAbbrev, prgch + 1, cchPat) == 0)
						break;
				}
				if (ifpi < cfpi)
				{
					// Since ' characters need to be doubled, the maximum possible length is
					// double the length of the abbreviation.
					wchar * prgchDst;
					StrUni stuReplace;
					stuReplace.SetSize(vfpi[ifpi].m_stuReplace.Length() * 2, &prgchDst);
					const wchar * prgchSrc = vfpi[ifpi].m_stuReplace.Chars();
					while (*prgchSrc)
					{
						if (*prgchSrc == '\'')
							*prgchDst++ = '\'';
						*prgchDst++ = *prgchSrc++;
					}
					stuReplace.SetSize(prgchDst - stuReplace.Chars(), &prgchDst);

					stuText.Append(stuReplace);
					prgch = prgchPatLim;
					prgchRun = prgchPatLim + 1;
				}
				else
				{
					// ENHANCE: AT SOME POINT, WE WILL WANT TO ASK THE USER WHETHER THIS IS
					// MEANT TO BE LITERAL, OR IS A MISSPELLING.  (PREFERABLY AT DEFINITION
					// TIME, NOT INVOCATION TIME.)
					stuText.Append(L"[[]");
					prgchRun = prgch + 1;
				}
			}
		}
		else if (ch == '_')
		{
			// SQL treats _ as a special character, so we have to put brackets around it.
			stuText.Append(prgchRun, prgch - prgchRun);
			stuText.Append(L"[_]");
			prgchRun = prgch + 1;
		}
		else if (ch == '%')
		{
			// SQL treats % as a special character, so we have to put brackets around it.
			stuText.Append(prgchRun, prgch - prgchRun);
			stuText.Append(L"[%]");
			prgchRun = prgch + 1;
		}
		else if (ch == '\'')
		{
			// Since the string is enclosed with quotes, we have to put two quotes together
			// so SQL treats it properly.
			stuText.Append(prgchRun, prgch - prgchRun);
			stuText.Append(L"''");
			prgchRun = prgch + 1;
		}
		else if (ch == '\\' && prgch < prgchLim - 1)
		{
			wchar chT = prgch[1];
			if (chT == '"')
			{
				// Convert \" combinations back to just a plain ".
				stuText.Append(prgchRun, prgch - prgchRun);
				prgchRun = prgch++ + 1; // Keep the " in prgchRun.
			}
			else if (chT == '\\')
			{
				// SQL will treat \ as a special character, so leave it doubled.
				stuText.Append(prgchRun, prgch - prgchRun);
				stuText.Append(L"\\\\");
				prgchRun = ++prgch + 1;
			}
		}
		prgch++;
	}
	stuText.Append(prgchRun, prgch - prgchRun);
}

/*----------------------------------------------------------------------------------------------
	Get the index of the enum that matches the text. Returns true if a match is found.

	@param pfmn Pointer to a filter menu node that specifies an enumeration valued field.
	@param prgchKey Reference to a pointer to some filter text from a cell that should start
					with an enumeration value.
	@param pisel Pointer to an integer for returning the enum index.

	@return True if successful, false if the text does not match any of the enumeration values.
----------------------------------------------------------------------------------------------*/
bool WhereQueryBuilder::ParseEnumText(FilterMenuNode * pfmn, const wchar *& prgchKey, int * pisel)
{
	AssertPtr(pfmn);
	AssertPsz(prgchKey);
	AssertPtr(pisel);
	Assert(pfmn->m_stid);

	StrUni stuEnum(pfmn->m_stid);
	if (!stuEnum.Length()) // Make sure the enum is a valid string.
		return false;

	FilterUtil::SkipWhiteSpace(prgchKey);

	const wchar * prgchEnum = stuEnum.Chars();
	const wchar * prgchLim = prgchEnum + stuEnum.Length();
	int isel = 0;
	while (prgchEnum < prgchLim)
	{
		// Loop through each of the strings and see if it matches prgchKey.
		const wchar * prgchEnumLim = wcschr(prgchEnum, '\n');
		if (!prgchEnumLim)
			prgchEnumLim = stuEnum.Chars() + stuEnum.Length();
		if (_wcsnicmp(prgchKey, prgchEnum, prgchEnumLim - prgchEnum) == 0)
		{
			// We found a match. Make sure we are at the end of the string or the next
			// character is a space character.
			wchar chT = prgchKey[prgchEnumLim - prgchEnum];
			if (chT == 0 || iswspace(chT))
			{
				// We definitely found a match, so move prgchKey forward and return the index
				// of the match in the enum.
				prgchKey += (prgchEnumLim - prgchEnum);
				*pisel = isel;
				return true;
			}
		}
		prgchEnum = prgchEnumLim + 1;
		isel++;
	}

	// There wasn't a match.
	return false;
}


/*----------------------------------------------------------------------------------------------
	Get the date values from the input string, which looks something like
	" Exact date(2000 / 12 / 28)"

	@param prgchKey Reference to a pointer to some filter text from a cell that should start
					with a date scope and value.
	@param nScope Reference to an integer for returning the scope code.
	@param nYear Reference to an integer for returning the year.
	@param nMonth Reference to an integer for returning the month of the year (1-12).
	@param nDay Reference to an integer for returning the day of the month (1-31).

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool WhereQueryBuilder::ParseDateText(const wchar *& prgchKey, int & nScope, int & nYear,
	int & nMonth, int & nDay)
{
	AssertPtr(prgchKey);

	FilterUtil::SkipWhiteSpace(prgchKey);
	DateKeywordLookup dkl;
	int idke = dkl.GetIndexFromStr(prgchKey);
	if (idke < 0)
		return false;
	nScope = dkl.GetStidFromIndex(idke);

	FilterUtil::SkipWhiteSpace(prgchKey);
	if (*prgchKey == 0)
	{
		nYear = 0;		// These values will be replaced, but let's make them deterministic.
		nMonth = 0;
		nDay = 0;
		return true;
	}
	if (*prgchKey == L'(')
		++prgchKey;
	FilterUtil::SkipWhiteSpace(prgchKey);

	StrUniBuf stubDate(prgchKey);
	int cchTerm = stubDate.ReverseFindCh(')');
	if (cchTerm >= 0)
	{
		stubDate.SetLength(cchTerm);		// Remove the trailing ) from the string.
		prgchKey += cchTerm + 1;
	}
	else
	{
		prgchKey += wcslen(prgchKey);
	}
	StrUni stu(stubDate.Chars(), stubDate.Length());
	if (!ParseDate(stu, idke, nYear, nMonth, nDay, false))
		ThrowHr(E_ABORT);		// Signals that we've already displayed error message.
	return true;
}

/*----------------------------------------------------------------------------------------------
	Parse the integer string, setting *pn to the value.
	This method returns false if there is an error while parsing the text.

	@param prgchKey Reference to a pointer to some filter text from a cell that contains a
					number.
	@param pn Pointer to an integer for returning the parsed text.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool WhereQueryBuilder::ParseIntegerText(const wchar *& prgchKey, int * pn)
{
	AssertPsz(prgchKey);
	AssertPtr(pn);

	wchar * psz;
	long n = wcstol(prgchKey, &psz, 10);
	if (n == 0)
	{
		prgchKey += wcsspn(prgchKey, L" \t");
		if (prgchKey[0] == '+' || prgchKey[0] == '-')
			++prgchKey;
		if (prgchKey[0] != '0')
			return false;
	}
	prgchKey = psz;
	Assert(sizeof(int) == sizeof(long));
	*pn = n;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Look at the first non-space character following our current position and get the object out
	of it.

	@param ptss Pointer to the ITsString COM object that contains the filter cell text,
					including the embedded object link we are looking for.
	@param prgchMin Pointer to the beginning of the text from ptss.
	@param prgchKey Reference to a pointer to our current location in the text from ptss.

	@return Database ID of the object stored in the next non-space character, or zero if the
					next non-space character is not an object.
----------------------------------------------------------------------------------------------*/
HVO WhereQueryBuilder::GetNextObject(ITsString * ptss, const wchar * prgchMin, const wchar *& prgchKey)
{
	AssertPtr(ptss);
	AssertPsz(prgchMin);
	AssertPtr(prgchKey);

	// The first non-space character must be kchObject.
	FilterUtil::SkipWhiteSpace(prgchKey);
	if (*prgchKey != kchObject)
		return 0;

	try
	{
		// The ktptObjData string property contains the guid of the object that we're looking
		// for.
		int ich = prgchKey - prgchMin;
		ITsTextPropsPtr qttp;
		SmartBstr sbstr;
		ITsStringPtr qtss;
		CheckHr(ptss->get_PropertiesAt(ich, &qttp));
		CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstr));
		wchar * prgchData = (wchar *)sbstr.Chars();
		if (*prgchData == kodtNameGuidHot)
		{
			// The string was formatted correctly, so move prgchKey forward one character
			// and return the ID of the object given by the GUID.
			prgchKey++;
			return m_qlpi->GetDbInfo()->GetIdFromGuid((GUID *)(prgchData + 1));
		}
	}
	catch (...)
	{
	}
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Go through the possibility list given by hvoPssl and add all the IDs with the given
	relation to the possibility given by hvoPss.

	@param stuCell Reference to a string used to return the set of database IDs for all
					matching possibity items.
	@param hvoPssl Database ID of the current possibility list.
	@param fkt Comparative relationship: kfktGT, kfktGTE, kfktLT, or kfktLTE
	@param hvoPss Database ID of the chosen possibility from the list, or zero if none have
					yet been chosen.
----------------------------------------------------------------------------------------------*/
void WhereQueryBuilder::AddItemsToString(StrUni & stuCell, HVO hvoPssl, FilterKeywordType fkt,
	HVO hvoPss)
{
	Assert(fkt == kfktGT || fkt == kfktLT || fkt == kfktGTE || fkt == kfktLTE);

	PossListInfoPtr qpli;
	bool fAddedOne = false;
	if (m_qlpi->LoadPossList(hvoPssl, m_qlpi->GetPsslWsFromDb(hvoPssl), &qpli))
	{
		int ipss = qpli->GetIndexFromId(hvoPss);
		Assert(ipss != -1);

		int ipssMin;
		int ipssLim;
		if (fkt == kfktGT || fkt == kfktGTE)
		{
			ipssMin = ipss + (fkt == kfktGT);
			ipssLim = qpli->GetCount();
		}
		else
		{
			ipssMin = 0;
			ipssLim = ipss - (fkt == kfktLT);
		}
		// We have retrieved the minimum and limit index values, so loop through these and
		// add each possibility ID to the string.
		for (int ipssT = ipssMin; ipssT < ipssLim; ipssT++)
		{
			PossItemInfo * pii = qpli->GetPssFromIndex(ipssT);
			AssertPtr(pii);
			stuCell.FormatAppend(fAddedOne ? L", %d" : L"%d", pii->GetPssId());
			fAddedOne = true;
		}
	}

	// We need to add at least one item, so if we didn't find any matches, add 0 because
	// 0 should never be used as an ID.
	if (!fAddedOne)
		stuCell.Append(L"0");
}

/*----------------------------------------------------------------------------------------------
	Go through the possibility list given by hvoPssl and add all the IDs which are subitems of
	the possibility given by hvoPss.

	@param vhvoSub Reference to a vector used to return the set of database IDs of all subitems.
	@param hvoPssl Database ID of the current possibility list.
	@param hvoPss Database ID of the chosen possibility from the list.
----------------------------------------------------------------------------------------------*/
void WhereQueryBuilder::GetSubitems(Vector<HVO> & vhvoSub, HVO hvoPssl, HVO hvoPss)
{
	PossListInfoPtr qpli;
	if (m_qlpi->LoadPossList(hvoPssl, m_qlpi->GetPsslWsFromDb(hvoPssl), &qpli))
	{
		if (qpli->GetDepth() == 1)
			return;
		int cpss = qpli->GetCount();
		int ipss = qpli->GetIndexFromId(hvoPss);
		Assert(ipss != -1);
		int nHierPss = qpli->GetPssFromIndex(ipss)->GetHierLevel();
		int nHier;
		PossItemInfo * ppss;
		while (++ipss < cpss)
		{
			ppss = qpli->GetPssFromIndex(ipss);
			AssertPtr(ppss);
			nHier = ppss->GetHierLevel();
			if (nHier <= nHierPss)
				break;
			vhvoSub.Push(ppss->GetPssId());
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Return the where part of the SQL query.

	@param stuWhereClause Reference to a string for returning the constructed where clause of
					an SQL query.
----------------------------------------------------------------------------------------------*/
void WhereQueryBuilder::GetWhereClause(StrUni & stuWhereClause)
{
	stuWhereClause.Clear();
	if (m_stuWhereClause.Length())
		stuWhereClause.Format(L"where %s%n", m_stuWhereClause.Chars());
}


/*----------------------------------------------------------------------------------------------
	Return the text representation of a FilterKeywordType.

	@param fkt Filter keyword type code.

	@return String representation of the keyword type code, or NULL for invalid input.
----------------------------------------------------------------------------------------------*/
wchar * WhereQueryBuilder::LookupSymbol(FilterKeywordType fkt)
{
	switch (fkt)
	{
	case kfktEqual:
	case kfktMatches:
		return L"=";
	case kfktNotEqual:
	case kfktDoesNotMatch:
		return L"<>";
	case kfktGT:
		return L">";
	case kfktLT:
		return L"<";
	case kfktGTE:
		return L">=";
	case kfktLTE:
		return L"<=";
	}
	return NULL;
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\MkCustomNb.bat"
// End: (These 4 lines are useful to Steve McConnel.)
