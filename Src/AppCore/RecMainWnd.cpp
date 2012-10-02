/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: RecMainWnd.cpp
Responsibility: Ken Zook
Last reviewed:

Description:
	This file contains class definitions for the following classes:
		RecMainWnd : RecMainWnd
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include <direct.h>
#include <io.h>
#include "xmlparse.h"

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

#undef LOG_FILTER_SQL
//#define LOG_FILTER_SQL 1
#ifdef LOG_FILTER_SQL
// Making this a static global avoids conditional method argument.
static FILE * fp = NULL;
#endif
//:End Ignore

#define READ_SIZE 16384

const achar * kpszWndPositionValue = _T("Position");


/*----------------------------------------------------------------------------------------------
	Data used for parsing XSL transform files in determining which views are valid to export.
	Hungarian: ed.
----------------------------------------------------------------------------------------------*/
struct ExportData
{
	StrAnsi m_staViews;
	bool m_fDone;
};

/*----------------------------------------------------------------------------------------------
	Return a pointer to the character string containing the value of the given XML attribute,
	or NULL if that attribute is not defined for this XML element.

	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszName Name to search for in prgpszAtts.
----------------------------------------------------------------------------------------------*/
static const XML_Char * GetAttributeValue(const XML_Char ** prgpszAtts,
	const XML_Char * pszName)
{
	if (!prgpszAtts)
		return NULL;
	for (int i = 0; prgpszAtts[i]; i += 2)
	{
		if (strcmp(prgpszAtts[i], pszName) == 0)
			return prgpszAtts[i+1];
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements.

	This static method is passed to the expat XML parser as a callback function.  See the
	comments in xmlparse.h for the XML_StartElementHandler typedef for the documentation
	such as it is.

	@param pvUser Pointer to generic user data (always an ExportData object in this case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
static void HandleStartTag(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsi staElem(pszName);
	if (staElem == "silfw:file")
	{
		ExportData * ped = reinterpret_cast<ExportData *>(pvUser);
		AssertPtr(ped);
		ped->m_staViews.Assign(GetAttributeValue(prgpszAtts, "views"));
		ped->m_fDone = true;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML end elements.

	This static method is passed to the expat XML parser as a callback function.

	@param pvUser Pointer to generic user data (always an ExportData object in this case).
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
static void HandleEndTag(void * pvUser, const XML_Char * pszName)
{
	// This is a trivial parse.  We don't need to do anything with the end tags.
}

/***********************************************************************************************
	RecMainWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor for main window.
----------------------------------------------------------------------------------------------*/
RecMainWnd::RecMainWnd()
{
	m_rghiml[0] = m_rghiml[1] = NULL;
	m_fResizing = false;
	m_dxpLeft = 90; // Default view bar width if nothing comes from the registry.
	m_vwt = kvwtDE;
	m_fCheckRequired = true;
	m_fFilterCancelled = false;
	m_ivbltMax = kvbltLim;
	for (int i = 0; i < kvbltLim; ++i)
		m_ivblt[i] = kvbltNoValue;
	// Set up registry location shared by all of FieldWorks. FwSettings::SetRoot already
	// adds the FieldWorks root to whatever is passed.
	m_allFws = NewObj FwSettings();
	m_allFws->SetRoot(_T(""));
}


/*----------------------------------------------------------------------------------------------
	Destructor for main window.
----------------------------------------------------------------------------------------------*/
RecMainWnd::~RecMainWnd()
{
	ClearViewBarImageLists();
}

AfSplitChild * RecMainWnd::CreateNewSplitChild()
{
	switch (m_vwt)
	{
	default:
		Assert(false);
		break;
	// case kvwtDE:	Handled by subclasses.
	case kvwtBrowse:
		// To get m_fHScrollEnabled in AfVwRootSite (base class) set to true. Note
		// that AfVwScrollWndBase (sub-class of AfVwRootSite) handles scroll bar adjustments.
		// Note also that there are three member variables having to do with enabling horizontal
		// scroll bars (on 31 March 2003): AfVwRootSite::m_fHScrollEnabled;
		// AfSplitterClientWnd::m_fScrollHoriz; AfSplitFrame::m_fScrollHoriz.
		return NewObj AfBrowseSplitChild(true);
		break;
	case kvwtDoc:
		return NewObj AfDocSplitChild();
		break;
	}
	return NULL;
}

void RecMainWnd::JumpTo(HVO hvo)
{
	Vector<HVO> vhvo;
	Vector<int> vflid;
	TagFlids tf;
	GetTagFlids(tf);

	// At some point we will want to distinguish between opening in a new window and
	// opening in the same window. But first, some way needs to be specified and
	// implemented to allow the user to get back from where they came. So until that
	// day, we simply open a new window.
	vhvo.Push(hvo);
	// Add owning records, if there are any, until we reach the main record.
	HVO hvoOwn;
	while (hvo)
	{
		CheckHr(m_qcvd->get_ObjOwner(hvo, &hvoOwn));
		hvo = hvoOwn;
		if (hvo == GetRootObj() || !hvo)
			break;
		vhvo.Insert(0, hvo);
		vflid.Insert(0, tf.flidSubitems);
	}

	// Get the current view.
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltView], sisel);
	Assert(sisel.Size() == 1);

	MakeJumpWindow(vhvo, vflid, *sisel.Begin());
}

/*----------------------------------------------------------------------------------------------
	This method applies the selected filter and sort method.
	It sets fCancel to true when it has to set the filter back to the previous one.

	@param iflt index to the requested filter (negative means "no filter").
	@param fCancel In: true to disable prompting.  Out: true when filter has to be set back to
					the previous filter.
	@param isrt index to the requested sort (negative means "default sort").
	@param pfxref Pointer to utility class with functions for handling cross reference fields.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::ApplyFilterAndSort(int iflt, bool & fCancel, int isrt,
	FwFilterXrefUtil * pfxref)
{
	WaitCursor wc;
	HVO hvoCur = 0;
	if (m_vhcFilteredRecords.Size())
	{
		// Some changes, e.g., undo promote while on last record can make this
		// illegal, so we should make sure we have a legal value.
		m_ihvoCurr = Min(m_ihvoCurr, m_vhcFilteredRecords.Size() - 1);
		hvoCur = m_vhcFilteredRecords[m_ihvoCurr].hvo;
	}

	bool fCancelPrev = fCancel;
	fCancel = false;

	HvoClsidVec vhcOld = m_vhcFilteredRecords;
	HVO hvoRootObjId = GetRootObj();
	AppSortInfo * pasi = NULL;

	if (iflt < 0 && isrt < 0)
	{
		m_vhcFilteredRecords = m_vhcRecords;
	}
	else
	{
		AfDbInfo * pdbi = m_qlpi->GetDbInfo();
		AssertPtr(pdbi);

		AppSortInfo * pasiXref = NULL;
		AppSortInfo * pasiDef = NULL;
		if (isrt < 0)
		{
			pasi = &m_asiDefault;
		}
		else
		{
			pasi = &pdbi->GetSortInfo(isrt);
			// pasiXref may be NULL if there are no cross references.
			pasiXref = GetCrossReferenceSortInfo();
			pasiDef = &m_asiDefault;
		}
		bool fSuccess = false;
		HvoClsidVec vhcNew;
		SortKeyHvosVec vskhNew;
		int cSortKeys = 0;
		bool fMultiSecond = false;
		bool fMultiThird = false;
		StrUni stuQuery;
#ifdef DEBUG
		StrAnsi staT;
		if (iflt >= 0)
		{
			AppFilterInfo & afi = pdbi->GetFilterInfo(iflt);
			staT.Format("******** Filter/Sort Query **** %S [%c %d] ********\n"
				"-- %S\n",
				afi.m_stuName.Chars(), afi.m_fSimple ? 'B' : 'A', afi.m_hvo,
				afi.m_stuColInfo.Chars());
		}
		else
		{
			staT.Format("******** Filter/Sort Query **** [no filter] ********\n"
				"--\n");
		}
		staT.FormatAppend("//////// %S [%S%s]",
			pasi->m_stuName.Chars(), pasi->m_stuPrimaryField.Chars(),
			pasi->m_fPrimaryReverse ? " Rev" : "");
		if (pasi->m_stuSecondaryField.Length())
			staT.FormatAppend(" [%S%s]",
				pasi->m_stuName.Chars(), pasi->m_stuSecondaryField.Chars(),
				pasi->m_fSecondaryReverse ? " Rev" : "");
		if (pasi->m_stuTertiaryField.Length())
			staT.FormatAppend(" [%S%s]",
				pasi->m_stuName.Chars(), pasi->m_stuTertiaryField.Chars(),
				pasi->m_fTertiaryReverse ? " Rev" : "");
		staT.Append("\n--------\n");
		::OutputDebugStringA(staT.Chars());
#ifdef LOG_FILTER_SQL
		FILE * fp = fopen("c:/FW/DebugOutput.txt", "a");
		if (fp)
		{
			fprintf(fp, "\n\
===============================================================================\n");
			time_t nTime;
			time(&nTime);
			fprintf(fp,
				"DEBUG RecMainWnd::ApplyFilterAndSort(iflt = %d, f = %c, isrt = %d) at %s",
				iflt, fCancelPrev ? 'T' : 'F', isrt, ctime(&nTime));
			fputs(staT.Chars(), fp);
		}
#endif
#endif

		bool fAdvFilter = false;
		TagFlids tf;
		tf.clid = GetRecordClid();
		GetTagFlids(tf);
		try
		{
			m_qstbr->StepProgressBar();
			if (iflt >= 0)
			{
				AppFilterInfo & afi = pdbi->GetFilterInfo(iflt);
				fAdvFilter = !afi.m_fSimple;
				if (!FilterUtil::GetFilterQuery(m_qlpi, iflt, m_hwnd, stuQuery, hvoRootObjId,
					tf.flidRoot, tf.flidSubitems, this, pasi, pasiXref, fCancelPrev, pfxref))
				{
					// The user has selected a simple filter that requires a prompt, and they
					// chose Cancel from the prompt dialog, so go back to the previously
					// selected filter.
					fCancel = true;
					return;
				}
			}
			else
			{
				SortMethodUtil::GetSortQuery(m_qlpi, pasi, hvoRootObjId,
					tf.flidRoot, tf.flidSubitems, pasiXref, pasiDef, stuQuery);
			}

			HRESULT hr = S_OK;
			try
			{
#ifdef DEBUG
				staT.Format("%S\n********\n", stuQuery.Chars());
				::OutputDebugStringA(staT.Chars());
#ifdef LOG_FILTER_SQL
				if (fp)
					fputs(staT.Chars(), fp);
#endif
#endif
				IOleDbEncapPtr qode;
				IOleDbCommandPtr qodc;
				ComBool fIsNull;
				ComBool fMoreRows;
				ULONG cbSpaceTaken;

				//	Obtain pointer to IOleDbEncap interface.
				pdbi->GetDbAccess(&qode);

				m_qstbr->StepProgressBar();
				int nStep;
				if (m_vhcFilteredRecords.Size())
				{
					nStep = 1000 / m_vhcFilteredRecords.Size();
					if (!nStep)
						nStep = 1;
				}
				else
				{
					nStep = 50;
				}
				// Determine the number of sort keys.
				if (pasi)
				{
					bool fIgnoreTertiary = false;
					bool fIgnoreSecondary = false;
					if (pasi->m_stuTertiaryField.Length())
					{
						cSortKeys = 3;
					}
					else if (pasi->m_stuSecondaryField.Length())
					{
						cSortKeys = 2;
						fIgnoreTertiary = true;
					}
					else if (pasi->m_stuPrimaryField.Length())
					{
						cSortKeys = 1;
						fIgnoreTertiary = true;
						fIgnoreSecondary = true;
					}
					// Check for essentially redundant sort fields.
					if (cSortKeys == 3)
					{
						if (pasi->m_stuTertiaryField == pasi->m_stuSecondaryField &&
							pasi->m_wsTertiary == pasi->m_wsSecondary &&
							pasi->m_collTertiary == pasi->m_collSecondary)
						{
							--cSortKeys;
							fIgnoreTertiary = true;
						}
						if (pasi->m_stuTertiaryField == pasi->m_stuPrimaryField &&
							pasi->m_wsTertiary == pasi->m_wsPrimary &&
							pasi->m_collTertiary == pasi->m_collPrimary)
						{
							--cSortKeys;
							if (fIgnoreTertiary)
								fIgnoreSecondary = true;
							else
								fIgnoreTertiary = true;
						}
					}
					if (cSortKeys == 2)
					{
						if (pasi->m_stuSecondaryField == pasi->m_stuPrimaryField &&
							pasi->m_wsSecondary == pasi->m_wsPrimary &&
							pasi->m_collSecondary == pasi->m_collPrimary)
						{
							--cSortKeys;
							fIgnoreSecondary = true;
						}
					}
					if (cSortKeys > 1)
					{
						IFwMetaDataCachePtr qmdc;
						pdbi->GetFwMetaDataCache(&qmdc);
						AssertPtr(qmdc);
						Vector<int> vflid;
						int iType;
						if (!fIgnoreSecondary)
						{
							SortMethodUtil::ParseFieldPath(pasi->m_stuSecondaryField, vflid);
							for (int i = 1; i < vflid.Size(); ++i)
							{
								CheckHr(qmdc->GetFieldType(vflid[i], &iType));
								if (iType == kcptOwningCollection ||
									iType == kcptReferenceCollection ||
									iType == kcptOwningSequence ||
									iType == kcptReferenceSequence)
								{
									fMultiSecond = true;
									break;
								}
							}
						}
						if (!fIgnoreTertiary)
						{
							SortMethodUtil::ParseFieldPath(pasi->m_stuTertiaryField, vflid);
							for (int i = 1; i < vflid.Size(); ++i)
							{
								CheckHr(qmdc->GetFieldType(vflid[i], &iType));
								if (iType == kcptOwningCollection ||
									iType == kcptReferenceCollection ||
									iType == kcptOwningSequence ||
									iType == kcptReferenceSequence)
								{
									if (fIgnoreSecondary)
										fMultiSecond = true;
									else
										fMultiThird = true;
									break;
								}
							}
						}
					}
				}
				// Run the query to get the IDs of the records that match the filter, in the
				// order determined by the sort method.
				CheckHr(hr = qode->CreateCommand(&qodc));
				CheckHr(hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
				CheckHr(hr = qodc->GetRowset(0));
				CheckHr(hr = qodc->NextRow(&fMoreRows));
				HvoClsid hc;

				while (fMoreRows)
				{
					CheckHr(hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hc.hvo),
						isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull && hc.hvo != hvoRootObjId)
					{
						CheckHr(hr = qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hc.clsid),
							isizeof(int), &cbSpaceTaken, &fIsNull, 0));

						vhcNew.Push(hc);

						HVO hvoOwner;
						CheckHr(hr = qodc->GetColValue(3, reinterpret_cast<BYTE *>(&hvoOwner),
							isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));

						m_qcvd->CacheIntProp(hc.hvo, kflidCmObject_Class, hc.clsid);
						m_qcvd->CacheObjProp(hc.hvo, kflidCmObject_Owner, hvoOwner);
						if (cSortKeys > 0)
						{
							SortKeyHvos skh;
							CheckHr(hr = qodc->GetColValue(4,
								reinterpret_cast<BYTE *>(&skh.m_hvoPrimary), isizeof(HVO),
								&cbSpaceTaken, &fIsNull, 0));
							if (cSortKeys >= 2)
							{
								CheckHr(hr = qodc->GetColValue(5,
									reinterpret_cast<BYTE *>(&skh.m_hvoSecondary),
									isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
							}
							else
							{
								skh.m_hvoSecondary = 0;
							}
							if (cSortKeys >= 3)
							{
								CheckHr(hr = qodc->GetColValue(6,
									reinterpret_cast<BYTE *>(&skh.m_hvoTertiary),
									isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
							}
							else
							{
								skh.m_hvoTertiary = 0;
							}
							vskhNew.Push(skh);
						}
					}
					CheckHr(hr = qodc->NextRow(&fMoreRows));
					m_qstbr->StepProgressBar(nStep);
				}
			}
			catch (...)
			{
				// TODO JohnT(DarrellZ): Do something more intelligent here.
				if (iflt >= 0 && isrt < 0)
				{
					StrApp strTitle(kstidFltrError);
					StrApp str;
					if (fAdvFilter)
						str.Load(kstidFltrImproperAdv);
					else
						str.Load(kstidFltrImproper);
					::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
#ifdef LOG_FILTER_SQL
					if (fp)
						fprintf(fp,
						"The SQL text for the selected filter was not generated correctly.\n"
							"hr = %s\n", AsciiHresult(hr));
#endif
				}
				else if (iflt < 0 && isrt >= 0)
				{
					StrApp strTitle(kstidSortError);
					StrApp str(kstidSortImproper);
					::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
#ifdef LOG_FILTER_SQL
					if (fp)
						fprintf(fp,
					  "The SQL text for the selected sort method was not generated correctly.\n"
							"hr = %s\n", AsciiHresult(hr));
#endif
				}
				else
				{
					Assert(iflt >= 0 && isrt >= 0);
					StrApp strTitle(kstidFltrOrSortError);
					StrApp str;
					if (fAdvFilter)
						str.Load(kstidFltrOrSortImproperAdv);
					else
						str.Load(kstidFltrOrSortImproper);
					::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
#ifdef LOG_FILTER_SQL
					if (fp)
						fprintf(fp,
		   "The SQL text for the selected filter and sort method was not generated correctly.\n"
							"hr = %s\n", AsciiHresult(hr));
#endif
				}
			}
		}
		catch (Throwable & thr)
		{
			if (thr.Error() != E_ABORT)
			{
				if (iflt >= 0 && isrt < 0)
				{
					StrApp strTitle(kstidFltrError);
					StrApp str;
					if (fAdvFilter)
						str.Load(kstidFltrImproperAdv);
					else
						str.Load(kstidFltrImproper);
					::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
				}
				else if (iflt < 0 && isrt >= 0)
				{
					StrApp strTitle(kstidSortError);
					StrApp str(kstidSortImproper);
					::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
				}
				else
				{
					Assert(iflt >= 0 && isrt >= 0);
					StrApp strTitle(kstidFltrOrSortError);
					StrApp str;
					if (fAdvFilter)
						str.Load(kstidFltrOrSortImproperAdv);
					else
						str.Load(kstidFltrOrSortImproper);
					::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
				}
			}
		}
		catch (...)
		{
			// TODO JohnT (DarrellZ): Do something more intelligent here.
			if (iflt >= 0 && isrt < 0)
			{
				StrApp strTitle(kstidFltrError);
				StrApp str;
				if (fAdvFilter)
					str.Load(kstidFltrImproperAdv);
				else
					str.Load(kstidFltrImproper);
				::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
			}
			else if (iflt < 0 && isrt >= 0)
			{
				StrApp strTitle(kstidSortError);
				StrApp str(kstidSortImproper);
				::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
			}
			else
			{
				Assert(iflt >= 0 && isrt >= 0);
				StrApp strTitle(kstidFltrOrSortError);
				StrApp str;
				if (fAdvFilter)
					str.Load(kstidFltrOrSortImproperAdv);
				else
					str.Load(kstidFltrOrSortImproper);
				::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
			}
		}
		if (vhcNew.Size() > 0 && (fMultiSecond || fMultiThird))
		{
			// The spec says to take the first value only of secondary and tertiary sort keys
			// that are multivalued, so weed out multiple results that aren't wanted.
#ifdef LOG_FILTER_SQL
			if (fp)
			{
				fprintf(fp,
					"vhcNew.Size() = %d, fMultiSecond = %d, fMultiThird = %d\n",
					vhcNew.Size(), fMultiSecond, fMultiThird);
#if 99
				fprintf(fp,
				"################################ DEBUG ################################\n");
				for (int i = 0; i < vhcNew.Size(); ++i)
				{
					fprintf(fp, "[%3d] hvo = %4d, clsid = %4d; Sort Key hvos = %4d, %4d, %4d\n",
						i, vhcNew[i].hvo, vhcNew[i].clsid, vskhNew[i].m_hvoPrimary,
						vskhNew[i].m_hvoSecondary, vskhNew[i].m_hvoTertiary);
				}
				fprintf(fp,
				"################################ DEBUG ################################\n");
#endif
			}
#endif
			if (fMultiSecond)
			{
				for (int i = 0; i < vhcNew.Size(); ++i)
				{
					for (int i1 = i + 1; i1 < vhcNew.Size(); ++i1)
					{
						if (vhcNew[i].hvo == vhcNew[i1].hvo &&
							vhcNew[i].clsid == vhcNew[i1].clsid &&
							vskhNew[i].m_hvoPrimary == vskhNew[i1].m_hvoPrimary &&
							vskhNew[i].m_hvoTertiary == vskhNew[i1].m_hvoTertiary)
						{
							vhcNew.Delete(i1);
							vskhNew.Delete(i1);
							--i1;
						}
					}
				}
			}
			if (fMultiThird)
			{
				for (int i = 0; i < vhcNew.Size(); ++i)
				{
					for (int i1 = i + 1; i1 < vhcNew.Size(); ++i1)
					{
						if (vhcNew[i].hvo == vhcNew[i1].hvo &&
							vhcNew[i].clsid == vhcNew[i1].clsid &&
							vskhNew[i].m_hvoPrimary == vskhNew[i1].m_hvoPrimary &&
							vskhNew[i].m_hvoSecondary == vskhNew[i1].m_hvoSecondary)
						{
							vhcNew.Delete(i1);
							vskhNew.Delete(i1);
							--i1;
						}
					}
				}
			}
#ifdef LOG_FILTER_SQL
#if 99
			if (fp)
			{
				for (int i = 0; i < vhcNew.Size(); ++i)
				{
					fprintf(fp, "[%3d] hvo = %4d, clsid = %4d; Sort Key hvos = %4d, %4d, %4d\n",
						i, vhcNew[i].hvo, vhcNew[i].clsid, vskhNew[i].m_hvoPrimary,
						vskhNew[i].m_hvoSecondary, vskhNew[i].m_hvoTertiary);
				}
				fprintf(fp,
				"################################ DEBUG ################################\n");
			}
#endif
#endif
			Assert(vhcNew.Size() == vskhNew.Size());
		}
#ifdef LOG_FILTER_SQL
		if (fp)
		{
#if 99
			fprintf(fp,
				"################################ DEBUG ################################\n");
			for (int i = 0; i < vhcNew.Size(); ++i)
			{
				fprintf(fp, "[%3d] hvo = %4d, clsid = %4d; Sort Key hvos = %4d, %4d, %4d\n",
					i, vhcNew[i].hvo, vhcNew[i].clsid, vskhNew[i].m_hvoPrimary,
					vskhNew[i].m_hvoSecondary, vskhNew[i].m_hvoTertiary);
			}
			fprintf(fp,
				"################################ DEBUG ################################\n");
#endif
			fprintf(fp, "========  The filter and sort method yield %d records.  ========\n",
				vhcNew.Size());
			fclose(fp);
			fp = NULL;
		}
#endif

		if (vhcNew.Size() > 0)
		{
			m_vhcFilteredRecords = vhcNew;
			m_vskhSortKeys = vskhNew;
			fSuccess = true;
		}

		if (!fSuccess)
		{
			if (iflt >= 0)
			{
				// The filter failed (either from user error or SQL generation error) when we
				// tried to run the query, or the query didn't return any results.
				// ENHANCE JohnT(DarrellZ): Eventually we want to do something a little more
				// intelligent in each of these cases.

				UpdateStatusBar();		// This turns the filter status pane on.
				StrUni stuNoMatch;
				stuNoMatch.Load(kstidStBar_NoFilterMatch);
				m_qstbr->SetRecordInfo(NULL, stuNoMatch.Chars(), NULL);
				m_qstbr->SetLocationStatus(0, 0);

				// There are not any records that matched the new filter, so let the user
				// make a choice as to what they want to do.

				FwFilterNoMatchDlgPtr qfltnm;
				qfltnm.Attach(CreateFilterNoMatchDlg());
				qfltnm->SetDialogValues(iflt, this);
				int ifltNew = 0;
				if (qfltnm->DoModal(m_hwnd) == kctidOk)
				{
					qfltnm->GetDialogValues(ifltNew);
					// Since the first filter is actually the No Filter, we have to add one
					// to the number retrieved from the dialog.
					ifltNew++;
					// Moved from TlsOptDlg::SaveFilterValues() to prevent massive use of
					// "GDI Object" and "User Object" resources.
					Vector<int> vifltNew = qfltnm->GetNewFilterIndexes();
					Vector<AfViewBarShell *> vpvwbrs = qfltnm->GetFilterViewBars();
					Assert(vifltNew.Size() == vpvwbrs.Size());
					qfltnm.Clear();
					if (vifltNew.Size())
					{
						Set<int> sisel;
						int cwnd = vifltNew.Size();
						for (int iwnd = 0; iwnd < cwnd; iwnd++)
						{
							if (vpvwbrs[iwnd])
							{
								sisel.Clear();
								sisel.Insert(vifltNew[iwnd]);
								vpvwbrs[iwnd]->SetSelection(m_ivblt[kvbltFilter], sisel);
							}
						}
					}
				}
				else
				{
					qfltnm.Clear();
				}
				if (ifltNew == -1)
				{
					// The user modified the current filter, which already selected the current
					// filter, so we don't want to select it again.
					return;
				}

				Set<int> sisel;
				sisel.Insert(ifltNew);
				m_qvwbrs->SetSelection(m_ivblt[kvbltFilter], sisel);
				return; // We already set the new icon, so don't reset it.
			}
			else
			{
				Assert(isrt >= 0);
				// The sort failed from an SQL generation error, so turn it off.
				int isrtNew = 0;
				Set<int> sisel;
				sisel.Insert(isrtNew);
				m_qvwbrs->SetSelection(m_ivblt[kvbltSort], sisel);
				return;
			}
		}
	}

	int crecNew = m_vhcFilteredRecords.Size();
	if (crecNew)
	{
		// Cache the dummy vector property that the browse and document views will look for when
		// constructing their views. Basically, it creates a dummy object using a negative
		// number unique to this main window as the object ID and
		// kflidStartDummyFlids as the tag.
		Vector<HVO> vhvo;
		vhvo.Resize(crecNew);
		for (int irecNew = 0; irecNew < crecNew; irecNew++)
			vhvo[irecNew] = m_vhcFilteredRecords[irecNew].hvo;
		m_qcvd->CacheVecProp(m_hvoFilterVec, m_flidFilteredRecords, vhvo.Begin(),
			vhvo.Size());
		// Set the "Class Id" of the dummy property so that m_hvoFilterVec will be recognized as
		// a valid object id when reconstructing the view.  See DN-718 for what happens without
		// this.  (Any nonzero value will do in all likelihood.)
		m_qcvd->CacheIntProp(m_hvoFilterVec, kflidCmObject_Class,
			MAKECLIDFROMFLID(m_flidFilteredRecords));
	}

	Vector<int> vflidPrimary;
	int flidPrimaryKey = 0;
	if (pasi)
	{
		SortMethodUtil::ParseFieldPath(pasi->m_stuPrimaryField, vflidPrimary);
		if (vflidPrimary.Size() > 0)
			flidPrimaryKey = vflidPrimary[1]; // 1st element is clid, 2nd is flid
	}

	if (vhcOld.Size() == m_vhcFilteredRecords.Size())
	{
		// Compare the two vectors, and if they are identical, return without doing
		// anything else.
		if (memcmp(vhcOld.Begin(), m_vhcFilteredRecords.Begin(),
			isizeof(HvoClsid) * vhcOld.Size()) == 0)
		{
			return;
		}
	}

	// Find out the index to set the client windows to.
	m_ihvoCurr = 0;
	for (int irecNew = 0; irecNew < crecNew; irecNew++)
	{
		if (m_vhcFilteredRecords[irecNew].hvo == hvoCur)
		{
			SetCurRecIndex(irecNew);
			break;
		}
	}

	if (!crecNew)
	{
		// JohnL has discussed this with KenZ and we think that you can't get to this point
		// in the code now. So Assert(false). Also, this is the only time that the dialog
		// RnNoEntriesDlg was used. That dialog is thought to be obsolete and superseded
		// by RnEmptyNotebookDlg. Hence we use that dialog here now (in case this point can
		// in fact be reached), allowing us to remove RnNoEntriesDlg altogether.
		Assert(false);
/*
	NOTE: If the assert goes away, then this will have to be redone in a generic way
	Right now, it came from RN, and uses its stuff.

		m_qstbr->StepProgressBar();

		Assert(!m_vhcRecords.Size());

		RnEmptyNotebookDlgPtr qremp;
		qremp.Create();
		StrApp strName(m_qlpi->PrjName());
		qremp->SetProject(strName.Chars());
		int ctid = qremp->DoModal(m_hwnd);
		int clid = kclidRnAnalysis;
		switch (ctid)
		{
		case kctidEmptyNewEvent:
			clid = kclidRnEvent;
			// Fall through.
		case kctidEmptyNewAnalysis:
			try
			{
				RnLpInfo * plpi = dynamic_cast<RnLpInfo *>(m_qlpi.Ptr());
				AssertPtr(plpi);
				HVO hvoRnb = plpi->GetRnId();
				HVO hvoNew;
				// Have database create a new record and set hvoNew to the new ID.
				// -1 indicates property is a collection.
				CheckHr(m_qcvd->MakeNewObject(clid, hvoRnb,
					kflidRnResearchNbk_Records, -1,
					&hvoNew));
				HvoClsid hc;
				hc.hvo = hvoNew;
				hc.clsid = clid;
				m_vhcRecords.Insert(0, hc);
				m_vhcFilteredRecords = m_vhcRecords;
				crecNew = 1;
			}
			catch (...)
			{
				Assert(false);
				return;
			}
			break;
		case kctidEmptyImport:
			break;
		case kctidCancel:
			break;
		}
*/
	}

	if (crecNew)
	{
		m_qstbr->StepProgressBar();

		int crncw = m_qmdic->GetChildCount();
		for (int irncw = 0; irncw < crncw; irncw++)
		{
			AfClientRecWnd * pafcrw = dynamic_cast<AfClientRecWnd *>(
				m_qmdic->GetChildFromIndex(irncw));
			AssertPtr(pafcrw);
			pafcrw->SetPrimarySortKeyFlid(flidPrimaryKey);

			pafcrw->ReloadRootObjects();
		}
	}
	RefreshTreeView(hvoCur);
}



/*----------------------------------------------------------------------------------------------
	Get the drag text for this object. This will be used to follow the mouse during a drag
	and also will be pasted into a text if dropped into a text location.

	This should be overridden to produce something more meaningful. Here we just return a
	string with the underlying class name and the hvo.
	@param hvo The object we plan to drag.
	@param clid The class of the object we plan to drag.
	@param pptss Pointer in which to return the drag text.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::GetDragText(HVO hvo, int clid, ITsString ** pptss)
{
	IFwMetaDataCachePtr qmdc;
	m_qlpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	SmartBstr sbstr;
	CheckHr(qmdc->GetClassName(clid, &sbstr));
	StrUni stu(sbstr.Chars());
	stu.FormatAppend(L": %d", hvo);
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), UserWs(), pptss);
}

/*----------------------------------------------------------------------------------------------
	Enable/Disable First, Last, Next, Previous record.

	@param cms menu command state

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsRecSelUpdate(CmdState & cms)
{
	if (!m_qmdic)
	{
		// If for some reason we don't have an mdi window we certainly can't do any of these.
		cms.Enable(false);
		return true;
	}

	AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(m_qmdic->GetCurChild());
	if (!qafcrw)
	{
		// If for some reason we don't have a client window we certainly can't do any of these.
		cms.Enable(false);
		return true;
	}

	// For all cases that are fully implemented, we can do the command provided the desired
	// record exists and is different.
	int chvo = m_vhcFilteredRecords.Size();
	bool fEnable = false;
	switch (cms.Cid())
	{
		case kcidDataPrev:
		case kcidDataFirst:
			fEnable = m_ihvoCurr > 0;
			break;
		case kcidDataLast:
		case kcidDataNext:
			fEnable = m_ihvoCurr < chvo - 1 && chvo > 0;
			break;
		default:
			Assert(false);
			break;
	}
	cms.Enable(fEnable);
	return true; // indicates we have handled it.
}


/*----------------------------------------------------------------------------------------------
	Handle First, Last, Next, Previous record.

	@param pcmd Ptr to menu command

	@return true if successfully handled
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdRecSel(Cmd * pcmd)
{
	AssertObj(pcmd);

	if (!m_qmdic)
		return false;

	AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(m_qmdic->GetCurChild());
	// Can't change records if window isn't ready to close.
	if (!qafcrw || !qafcrw->IsOkToChange(true))
		return false;
	// For now we save data whenever we go to next/previous in order to clear the undo stack.
	// Otherwise users can undo things that do not show up on the screen which is very
	// confusing. Of course, there are various other ways this can still happen such as
	// clicking in another FW window, switching views, etc. This is only a partial solution
	// until Undo can be enhanced to include full cursor location information.
	SaveData();

	// Synchronize with any changes from other apps.
	AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	AssertPtr(papp);
	papp->DbSynchronize(m_qlpi);

	m_fCheckRequired = false;

	// Clear out cursor information so it doesn't interfere with the new record.
	m_vflidPath.Clear();
	m_vhvoPath.Clear();
	m_ichCur = 0;

	switch (pcmd->m_cid)
	{
		case kcidDataFirst:
			qafcrw->DispCurRec(DBBMK_FIRST, 0);
			break;
		case kcidDataLast:
			qafcrw->DispCurRec(DBBMK_LAST, 0);
			break;
		case kcidDataNext:
			if (m_ihvoCurr < m_vhcFilteredRecords.Size() - 1)
				qafcrw->DispCurRec(NULL, 1);
			break;
		case kcidDataPrev:
			if (m_ihvoCurr > 0)
				qafcrw->DispCurRec(NULL, -1);
			break;
		default:
			m_fCheckRequired = true;
			return false;	// Button ID not recognized.
	}

	m_fCheckRequired = true;
	return true;
}


/*----------------------------------------------------------------------------------------------
	A new top-level record has been inserted, so update the vector of filtered records that is
	stored in the cache (used by document and browse views).
	This method does not update the real (i.e. what gets put in the database) cache vector
	property. Since this method always updates the vector in memory (m_vhcRecords), the real
	cache property must be updated when this method is called to keep the two lists
	synchronized. Since it is not easily possible to tell whether a new record matches a filter,
	it is not shown in windows that are filtered. If the current main window is not filtered,
	the fake (i.e. the vector of filtered records) cache property is updated as well as the
	vector of filtered records in memory (m_vhcFilteredRecords).
	Returns the index of the new record in the raw (unfiltered) vector.

	@param clid class id
	@param hvoNew new object id

	@return index of the new record inserted
----------------------------------------------------------------------------------------------*/
int RecMainWnd::OnInsertRecord(int clid, HVO hvoNew)
{
	// This is called for all open windows. Some may have filters activated, so at this point
	// it is valid to have filters enabled.
	int ihvo = AddMainRecord(hvoNew, clid);
	// Update the current record index only if this is the active window. (DN-522)
	if (m_fActiveWindow)
		SetCurRecIndex(ihvo);
	UpdateStatusBar();
	return ihvo;
}


/*----------------------------------------------------------------------------------------------
	This adds a main record to the appropriate vectors (m_vhcRecords, m_vhcFilteredRecords,
	and the cache vector represented by m_hvoFilterVec). This does not create the object. It
	just provides a consistent way of updating these vectors. If you are adding a new blank
	entry, you should disable filters prior to calling this method. See the comments below.
	@param hvo The id of the object we want to add.
	@param clid The class id of hvo.
	@return the index in current list where the item was placed.
----------------------------------------------------------------------------------------------*/
int RecMainWnd::AddMainRecord(HVO hvo, int clid)
{
	// Insert the new record at the end of the list for now.
	// Since it is newly created, the end of the list is the logical place when sorted by the
	// default which is creation date. If any other sort is active, we will have nothing else
	// to sort on at this point, so putting it at the end is still reasonable.
	HvoClsid hc;
	hc.hvo = hvo;
	hc.clsid = clid;
	int ihvo = m_vhcRecords.Size();
	// Add it to the master list of records.
	m_vhcRecords.Insert(ihvo, hc);

	// If we have an active filter, stop at this point. If we are really inserting a new
	// main record and filters are enabled, the new record would almost certainly not match
	// the filter, so it would not show up. Or alternatively, we stuff the record in even
	// though it doesn't match the filter, which leaves the user in a confusing state.
	// The analysts decided that we should silently disable filters when a new record is
	// inserted rather than raising a dialog or inserting a new entry in a filtered list.
	// In the case of promoting a subentry, it would already be in the filtered list if the
	// subrecord passed the filter, so we don't need to go any further.
	if (IsFilterActive() || IsSortMethodActive())
		return ihvo;

	// Add it to the filtered list of records.
	m_vhcFilteredRecords.Insert(ihvo, hc);
	// Since a filter isn't active, the master and filtered lists should be identical.
	Assert(m_vhcRecords.Size() == m_vhcFilteredRecords.Size());
	Assert(memcmp(m_vhcRecords.Begin(), m_vhcFilteredRecords.Begin(),
		m_vhcRecords.Size()) == 0);

	// Update the dummy vector property that the browse and document views will look for when
	// constructing their views.
	if (m_hvoFilterVec)
	{
		m_qcvd->CacheReplace(m_hvoFilterVec, m_flidFilteredRecords, ihvo, ihvo,
			&hvo, 1);
		CheckHr(m_qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoFilterVec,
			m_flidFilteredRecords, ihvo, 1, 0));
	}
	return ihvo;
}


/*----------------------------------------------------------------------------------------------
	This deletes a main record from the appropriate vectors (m_vhcRecords &
	m_vhcFilteredRecords). This does not delete the object. It just provides a consistent way
	of updating these member variables. This only deletes one occurrence of the item from each
	vector. It ignores any vector that doesn't hold hvo. This also puts ihvoCurr in range if
	the final entry is deleted.
	@param hvo The id of the main record we want to delete.
	@param fUpdateCache If true this also updates the dummy cache vector used by Doc and Browse.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::DeleteMainRecord(HVO hvo, bool fUpdateCache)
{
	// Find the index of the deleted item in the filtered record list.
	int ivhc;
	for (ivhc = m_vhcFilteredRecords.Size(); --ivhc >= 0; )
	{
		if (m_vhcFilteredRecords[ivhc].hvo == hvo)
			break;
	}
	if (ivhc >= 0)
	{
		m_vhcFilteredRecords.Delete(ivhc);
		if (fUpdateCache)
		{
			// Remove the record from the dummy vector that the Browse and Document views use
			// to show the filtered and sorted records. This is not needed when RemoveObjRefs
			// has been called prior to calling this since it automatically removes all
			// references and executes PropChanged for all properties that are changed.
			// In that case false should have been passed in fUpdateCache.
			m_qcvd->CacheReplace(m_hvoFilterVec, m_flidFilteredRecords, ivhc, ivhc + 1,
				NULL, 0);
			CheckHr(m_qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoFilterVec,
				m_flidFilteredRecords, ivhc, 0, 1));
		}
	}

	// Find the index of the deleted item in the main top-level record list.
	for (ivhc = m_vhcRecords.Size(); --ivhc >= 0; )
	{
		if (m_vhcRecords[ivhc].hvo == hvo)
			break;
	}
	if (ivhc >= 0)
		m_vhcRecords.Delete(ivhc);

	// If we deleted the last entry, update the current entry pointer.
	if (m_ihvoCurr == m_vhcFilteredRecords.Size())
		--m_ihvoCurr;
}


/*----------------------------------------------------------------------------------------------
	Remove field editors for a subitem.

	@param flid The owning flid for the deleted object.
	@param hvoDel The id for the deleted object.
	@param fCheckEmpty ???.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::OnDeleteSubitem(int flid, HVO hvoDel, bool fCheckEmpty)
{
	int cwnd = m_qmdic->GetChildCount();
	// Go through all DE windows.
	for (int iwnd = 0; iwnd < cwnd; ++iwnd)
	{
		AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(m_qmdic->GetChildFromIndex(iwnd));
		if (!qafcrw || !qafcrw->Hwnd())
			continue; // Not an active data entry window.
		HVO hvoOwn;
		m_qcvd->get_ObjectProp(hvoDel, kflidCmObject_Owner, &hvoOwn);
		Assert(hvoOwn);
		// Make sure it is deleted from both panes.
		AfDeSplitChild * padsc = dynamic_cast<AfDeSplitChild *>(qafcrw->GetPane(0));
		Assert(padsc);
		padsc->DeleteTreeNode(hvoDel, 0);
		padsc = dynamic_cast<AfDeSplitChild *>(qafcrw->GetPane(1));
		if (padsc)
			padsc->DeleteTreeNode(hvoDel, 0);
	}
}


/*----------------------------------------------------------------------------------------------
	Load settings common to this window's subclasses from the registry.

	@param pszRoot The string that is used for this app as the root for most registry entries.
	@param fRecursive	If true, then every child window will be load their settings.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	// load the Measurement System to be used in the App
	FwSettings * pfws = AfApp::GetSettings();
	DWORD dwT;

	// First attempt to load the MeasurementSystem setting from the FieldWorks root.
	if (!pfws->GetDword(m_allFws->GetRoot(), _T(""), _T("MeasurementSystem"), &dwT))
	{
		// If not in FieldWorks, check in the Data Notebook directory.
		if (!pfws->GetDword(pszRoot, _T("MeasurementSystem"), &dwT))
			dwT = 2; // Default to centimeters if no registry entry
	}

	if (dwT > 3)
		dwT = 0;
	AfApp::Papp()->SetMsrSys((MsrSysType)dwT);

	// Load the rest
	SuperClass::LoadSettings(pszRoot, fRecursive);

	// Get window position.
	LoadWindowPosition(pszRoot, kpszWndPositionValue);

	// Load the width of the view bar.
	if (pfws->GetDword(pszRoot, _T("Viewbar Width"), &dwT))
		m_dxpLeft = dwT;

	DWORD dwStatusbarFlags;
	if (!pfws->GetDword(pszRoot, _T("Statusbar Flags"), &dwStatusbarFlags))
		dwStatusbarFlags = 1;
	::ShowWindow(m_qstbr->Hwnd(), dwStatusbarFlags ? SW_SHOW : SW_HIDE);

	// Read the toolbar settings. If the settings aren't there, use default values.
	DWORD dwToolbarFlags;
	if (!pfws->GetDword(pszRoot, _T("Toolbar Flags"), &dwToolbarFlags))
		dwToolbarFlags = (DWORD)-1; // Show all toolbars.
	LoadToolbars(pfws, pszRoot, dwToolbarFlags);
}

/*----------------------------------------------------------------------------------------------
	Save settings common to this window's subclasses to the registry.

	@param pszRoot The string that is used for this app as the root for all registry entries.
	@param fRecursive If true, then every child window will be asked to save their settings.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::SaveSettings(pszRoot, fRecursive);

	SaveWindowPosition(pszRoot, kpszWndPositionValue);

	FwSettings * pfws = AfApp::GetSettings();

	// Store the settings for the viewbar.
	AssertObj(m_qvwbrs);
	DWORD dwViewbarFlags = GetViewbarSaveFlags();
	pfws->SetDword(pszRoot, _T("Viewbar Flags"), dwViewbarFlags);

	// Store the settings for the status bar.
	pfws->SetDword(pszRoot, _T("Statusbar Flags"), ::IsWindowVisible(m_qstbr->Hwnd()));

	// Store the settings for the toolbars.
	SaveToolbars(pfws, pszRoot, _T("Toolbar Flags"));

	// Store which view bar group is currently selected.
	pfws->SetDword(pszRoot, _T("LastViewBarGroup"), m_qvwbrs->GetCurrentList());

	// Store the width of the view bar.
	pfws->SetDword(pszRoot, _T("Viewbar Width"), m_dxpLeft);

	// Store the Measurement System to be used in the App
	pfws->SetDword(m_allFws->GetRoot(), _T(""), _T("MeasurementSystem"), (DWORD)AfApp::Papp()->GetMsrSys());
	// Store settings for three command line options, plus database name.
	if (m_qlpi)
	{
		AfDbInfo * pdbi = m_qlpi->GetDbInfo();
		// Maybe all databases are disconnected, so check for it.
		if (pdbi)
		{
			StrApp str;
			str = pdbi->ServerName();
			pfws->SetString(pszRoot, _T("LatestDatabaseServer"), str);
			str = GetRootObjName();
			pfws->SetString(pszRoot, _T("LatestRootObjectName"), str);
			str = pdbi->DbName();
			pfws->SetString(pszRoot, _T("LatestDatabaseName"), str);
		}
	}

	// Store the last data record that is showing.
	pfws->SetDword(pszRoot, _T("LastDataRecord"), m_ihvoCurr);

	// Store the view that is currently visible.
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltView], sisel);
	Assert(sisel.Size() == 1);
	pfws->SetDword(pszRoot, _T("LastView"), *sisel.Begin());

	// Store the filter that is currently chosen.
	sisel.Clear();
	m_qvwbrs->GetSelection(m_ivblt[kvbltFilter], sisel);
	Assert(sisel.Size() == 1);
	pfws->SetDword(pszRoot, _T("LastFilter"), *sisel.Begin());

	// Store the sort method that is currently chosen.
	sisel.Clear();
	m_qvwbrs->GetSelection(m_ivblt[kvbltSort], sisel);
	Assert(sisel.Size() == 1);
	pfws->SetDword(pszRoot, _T("LastSort"), *sisel.Begin());

	if (m_ivblt[kvbltOverlay] != kvbltNoValue)
	{
		// Store which overlays are currently visible.
		DWORD dwOverlay = 0;
		sisel.Clear();
		m_qvwbrs->GetSelection(m_ivblt[kvbltOverlay], sisel);
		Set<int>::iterator sit = sisel.Begin();
		Set<int>::iterator sitStop = sisel.End();
		while (sit != sitStop)
		{
			dwOverlay |= 1 << *sit;
			++sit;
		}
		pfws->SetDword(pszRoot, _T("Open Overlays"), dwOverlay);
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the class

	@param pdbi Ptr to DataBase Info
	@param plpi Ptr to Language Project Info
----------------------------------------------------------------------------------------------*/
void RecMainWnd::Init(AfLpInfo * plpi)
{
	Assert(!m_qlpi); // Only call this once.
	AssertPtr(plpi);

	m_qlpi = plpi;
	if (!m_qlpi)
		ThrowHr(E_FAIL);
}


/*----------------------------------------------------------------------------------------------
	Make all client windows give up their connection to the database and close project saving
	settings to registry. If this is the final window open on the database, remove the DbInfo
	to release the database connection.
	@return false to close the window, true to abort.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::OnClose()
{
	if (!IsOkToChange())
		return true; // Don't close the window if there are problems.

	// Make all client windows give up their connection to the database.
	int crncw = m_qmdic->GetChildCount();
	int irncw;
	for (irncw = 0; irncw < crncw; irncw++)
	{
		AfClientWnd * pafcw = m_qmdic->GetChildFromIndex(irncw);
		AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(pafcw);
		if (qafcrw && !qafcrw->CloseProj())
			return true;	// Abort shutdown.
	}

	// Determine how many windows are open using this database.
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	int cMainWnds = 0;
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cqafw = vqafw.Size();
	for (int iqafw = 0; iqafw < cqafw; ++iqafw)
	{
		AfMainWnd * pafwLoop = vqafw[iqafw].Ptr();
		AssertPtr(pafwLoop);
		AfLpInfo * plpiLoop = pafwLoop->GetLpInfo();
		AssertPtr(plpiLoop);
		if (plpiLoop->GetDbInfo() == pdbi)
			cMainWnds++;
	}

	// We need to get rid of the old records in m_vhcRecords.
	m_vhcRecords.Clear();
	m_vhcFilteredRecords.Clear();

	// This saves all settings, etc. Do this before closing DbInfo.
	bool freturn = SuperClass::OnClose();

	// If this is the final window on this database, remove DbInfo to clear DB connection.
	// This is required for AfDbApp::CloseDbAndWindows to work properly.
	if (cMainWnds == 1)
	{
		AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
		Assert(pdapp);
		pdbi->CleanUp();
		pdapp->DelDbInfo(pdbi);
		m_qacth.Clear();	// release any hold on the database connection
	}

	return freturn;
}


/*----------------------------------------------------------------------------------------------
	Enable various menu items.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsHaveRecord(CmdState & cms)
{
	cms.Enable(m_vhcFilteredRecords.Size() > 0);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Enable the Insert XXX menu item(s).
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsInsertEntry(CmdState & cms)
{
	cms.Enable(true);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set up the Tools Option dialog. This is called from the Tools/Options menu and also
	from the popup menu on the CaptionBar. Some set-up, and the actual running, are now in
	RecMainWnd::RunTlsOptDlg (below).

	@param pcmd menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdToolsOpts(Cmd * pcmd)
{
	AssertObj(pcmd);
	Assert(m_qmdic);

	TlsOptDlgPtr qtod;
	qtod.Attach(GetTlsOptDlg());
	AssertObj(qtod);
	TlsDlgValue tgv;
	tgv.iv1 = -1;
	tgv.iv2 = 0;
	tgv.itabInitial = qtod->GetInitialTabIndex(pcmd->m_cid);
	RunTlsOptDlg(qtod, tgv);

	// Moved from TlsOptDlg::SaveFilterValues() to prevent massive use of
	// "GDI Object" and "User Object" resources.
	Vector<int> vifltNew = qtod->GetNewFilterIndexes();
	Vector<AfViewBarShell *> vpvwbrs = qtod->GetFilterViewBars();
	Assert(vifltNew.Size() == vpvwbrs.Size());
	qtod.Clear();
	if (vifltNew.Size())
	{
		Set<int> sisel;
		int cwnd = vifltNew.Size();
		for (int iwnd = 0; iwnd < cwnd; iwnd++)
		{
			if (vpvwbrs[iwnd])
			{
				sisel.Clear();
				sisel.Insert(vifltNew[iwnd]);
				vpvwbrs[iwnd]->SetSelection(m_ivblt[kvbltFilter], sisel);
			}
		}
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Run the Tools Option dialog. This is called from CmdTlsOpts (above) and also, for example,
	from CleListBar::CmdToolsOpts. Go through all open windows and copy their settings
	into a vector (vwndSet). Open the tools Option Dialog (or sub-class) with the correct tab
	selected, according to the users selection and the passed parameters. If the dialog was
	exited with the OK button then go through all windows and restore settings that were saved
	earlier.

	@param ptod	Pointer to the Tools Options Dialog class to be run.
	@param tgv	Reference to the TlsDlgValue structure set up by the caller.

	@return true
----------------------------------------------------------------------------------------------*/
void RecMainWnd::RunTlsOptDlg(TlsOptDlg * ptod, TlsDlgValue & tgv)
{
	AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	Assert(pdapp);
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);

	// Don't do anything if there is a bad edit outstanding or some other problem which the
	// user will be made aware of by these next two calls.
	if (!pdapp->AreAllWndsOkToChange(pdbi, false))
		return;
	if (!pdapp->SaveAllWndsEdits(pdbi))
		return;

	// Find out the currently applicable class id (e.g. "location")
	// and level (e.g. 0=regular entry, 1=sub-entry).
	// Do this here for all callers since I (JohnL) think it is always needed and is protected.
	GetCurClsLevel(&tgv.clsid, &tgv.nLevel);
	RecMainWndPtr qrmwCur = dynamic_cast<RecMainWnd *>(AfApp::Papp()->GetCurMainWnd());
	RecMainWndPtr qrmwOther;
	AfClientRecWndPtr qafcrwOther;
	AfMdiClientWndPtr qmdicOther;	// Not m_qmdic.
	Vector<WndSettings> vwndSet;
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int iqafw;
	int cqafw = vqafw.Size();
	// Spin through windows, and do two things to them.
	for (iqafw = 0; iqafw < cqafw; ++iqafw)
	{
		// We must close all editors of all windows or else if the cursor is in a field
		// that the user switches to "if data preset" that field can not be hidden
		// unless the editor is closed.
		qrmwOther = dynamic_cast<RecMainWnd *>(vqafw[iqafw].Ptr());
		AssertObj(qrmwOther);
		qmdicOther = qrmwOther->GetMdiClientWnd();
		Assert(qmdicOther);
		qafcrwOther = dynamic_cast<AfClientRecWnd *>(qmdicOther->GetCurChild());
		Assert(qafcrwOther);
		AfDeSplitChildPtr qadsc = dynamic_cast<AfDeSplitChild *>(qafcrwOther->CurrentPane());
		if (qadsc)
			qadsc->CloseEditor();

		// If the language projects are the same, temp save settings of all windows
		// in vwndSet vector.
		if (qrmwOther->GetLpInfo() == qrmwCur->GetLpInfo())
		{
			qmdicOther = qrmwOther->GetMdiClientWnd();
			AssertPtr(qmdicOther);
			int cwnd = qmdicOther->GetChildCount();
			// Go through all child windows.
			for (int iwnd = 0; iwnd < cwnd; ++iwnd)
			{
				AfClientWndPtr qacw =
					dynamic_cast<AfClientWnd *>(qmdicOther->GetChildFromIndex(iwnd));
				if (!qacw || !qacw->Hwnd())
					continue; // Not an active window.
				qafcrwOther =
					dynamic_cast<AfClientRecWnd *>(qmdicOther->GetChildFromIndex(iwnd));
				AssertPtr(qafcrwOther);
				WndSettings wndSet;
				qafcrwOther->GetVwSpInfo(&wndSet);
				vwndSet.Push(wndSet);
			}
		}
	}

	// Set up dlg, and fire it up.
	ptod->SetDialogValues(tgv);
	if (ptod->DoModal(m_hwnd) == kctidOk)
	{
		ptod->ClearNewFilterIndexes();
		ptod->ClearFilterViewBars();

		ptod->SaveDialogValues();
		SyncInfo sync(ksyncFullRefresh, 0, 0);
		m_qlpi->StoreAndSync(sync);

		if (ptod->GetSync() == ksyncCustomField)
		{
			SyncInfo sync(ksyncCustomField, 0, 0);
			m_qlpi->StoreAndSync(sync);
		}
	}
	return;
}


/*----------------------------------------------------------------------------------------------
	This recursively extends leaf nodes that refer to multilingual strings with nodes that allow
	selecting the specific writing system, old writing system, and collating engine.

	@param plpi
	@param hmflidfd
	@param vsmnLang
	@param psmn
	@param wsDefault Writing System code/selector from the parent node.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::_AddLanguageChoices(AfLpInfo * plpi, HashMap<int,FieldData> & hmflidfd,
	SortMenuNodeVec & vsmnLang, SortMenuNode * psmn, int wsDefault)
{
	AssertPtr(plpi);
	AssertPtr(psmn);

	if (psmn->m_smnt == ksmntClass || psmn->m_smnt == ksmntField)
	{
		// Recurse into the subitems.
		int csmn = psmn->m_vsmnSubItems.Size();
		for (int ismn = 0; ismn < csmn; ismn++)
			_AddLanguageChoices(plpi, hmflidfd, vsmnLang, psmn->m_vsmnSubItems[ismn],
				psmn->m_wsMagic ? psmn->m_wsMagic : wsDefault);
		return;
	}
	else if (psmn->m_smnt != ksmntLeaf)
	{
		return;
	}
	else if (psmn->m_proptype != kcptString && psmn->m_proptype != kcptMultiString &&
		psmn->m_proptype != kcptUnicode && psmn->m_proptype != kcptMultiUnicode &&
		psmn->m_proptype != kfptCrossRef && psmn->m_proptype != kfptCrossRefList)
	{
		return;
	}
	// We need to add language/old writing system/collation submenus to this node.
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	ILgWritingSystemFactoryPtr qwsf;
	pdbi->GetLgWritingSystemFactory(&qwsf);
	int wsUser = UserWs();
	Vector<int> vws;
	int iws;
	int wsMagic = psmn->m_wsMagic ? psmn->m_wsMagic : wsDefault;
	switch (wsMagic)
	{
	case 0:
		{
			// We know nothing, so list everything!
			Vector<int> & vwsAnal = plpi->AnalWss();
			Vector<int> & vwsVern = plpi->VernWss();
			Set<int> setws;
			for (iws = 0; iws < vwsAnal.Size(); ++iws)
				setws.Insert(vwsAnal[iws]);
			for (iws = 0; iws < vwsVern.Size(); ++iws)
				setws.Insert(vwsVern[iws]);
			setws.Insert(wsUser);
			Set<int>::iterator it;
			for (it = setws.Begin(); it != setws.End(); ++it)
				vws.Push(it.GetValue());
		}
		break;
	case kwsAnals:
		vws = plpi->AnalWss();
		break;
	case kwsVerns:
		vws = plpi->VernWss();
		break;
	case kwsAnal:
		vws.Push(plpi->AnalWs());
		break;
	case kwsVern:
		vws.Push(plpi->VernWs());
		break;
	case kwsAnalVerns:
		vws = plpi->AnalVernWss();
		break;
	case kwsVernAnals:
		vws = plpi->VernAnalWss();
		break;
	default:
		vws.Push(psmn->m_wsMagic);
		break;
	}
	int ismn;
	SortMenuNodePtr qsmnCopy;
	for (iws = 0; iws < vws.Size(); ++iws)
	{
		bool fFound = false;
		for (ismn = 0; ismn < vsmnLang.Size(); ++ ismn)
		{
			if (vws[iws] == vsmnLang[ismn]->m_ws)
			{
				_CopyMenuNode(&qsmnCopy, vsmnLang[ismn]);
				psmn->AddSortedSubItem(qsmnCopy);
				fFound = true;
				break;
			}
		}
		if (fFound)
			continue;

		// Need to create a Language selection menu for this writing system.
		IWritingSystemPtr qws;
		CheckHr(qwsf->get_EngineOrNull(vws[iws], &qws));
		AssertPtr(qws.Ptr());
		SmartBstr sbstrEnc;
		CheckHr(qws->get_UiName(wsUser, &sbstrEnc));
		int nLocale;
		CheckHr(qws->get_Locale(&nLocale));
		SortMenuNodePtr qsmnWs;
		qsmnWs.Create();
		Assert(sbstrEnc.Length());
		qsmnWs->m_stuText.Assign(sbstrEnc.Chars());

		qsmnWs->m_smnt = ksmntWs;
		qsmnWs->m_ws = vws[iws];
		Vector<int> vws;
		SortMenuNodePtr qsmnOws;
		// Add the list of collations as a submenu to the old writing system.
		int ccoll;
		int icoll;
		CheckHr(qws->get_CollationCount(&ccoll));
		for (icoll = 0; icoll < ccoll; ++icoll)
		{
			ICollationPtr qcoll;
			CheckHr(qws->get_Collation(icoll, &qcoll));
			SmartBstr sbstrColl;
			CheckHr(qcoll->get_Name(wsUser, &sbstrColl));
			SortMenuNodePtr qsmnColl;
			qsmnColl.Create();
			qsmnColl->m_stuText.Assign(sbstrColl.Chars());
			qsmnColl->m_smnt = ksmntColl;
			qsmnColl->m_coll = icoll;		// store an index value.
			qsmnWs->AddSortedSubItem(qsmnColl);
		}
		vsmnLang.Push(qsmnWs);
		_CopyMenuNode(&qsmnCopy, qsmnWs);
		psmn->AddSortedSubItem(qsmnCopy);
	}
}


/*----------------------------------------------------------------------------------------------
	Synchronize all clients and dialogs with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::Synchronize(SyncInfo & sync)
{
	// Process all child AfDialogs of the main window.
	::EnumChildWindows(m_hwnd, &AfWnd::EnumChildSyncDialogs, (LPARAM)&sync);
	// Process all AfDialogs of the thread (top level).
	if (ModuleEntry::IsExe())
	{
		::EnumThreadWindows(ModuleEntry::MainThreadId(), &AfWnd::EnumChildSyncDialogs,
			(LPARAM)&sync);
	}

	switch(sync.msg)
	{
	case ksyncWs:
		if (!FullRefresh())
			return false;
		return true;

	case ksyncPageSetup:
		LoadPageSetup();
		return true;

	case ksyncHeadingChg:
		UpdateTitleBar();
		return true;

	case ksyncPossList:
		{
			// If an overlay is active for this list, we need to update the overlay
			// to keep the display in sync.
			for (int ioi = m_qlpi->GetOverlayCount(); --ioi >= 0; )
			{
				AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(ioi);
				if (aoi.m_hvoPssl == sync.hvo)
					OnChangeOverlay(ioi);
			}
			return true;
		}

	case ksyncMoveEntry:
	case ksyncPromoteEntry:
	case ksyncUndoRedo:
	case ksyncMergePss:
		// For merging, we know the list. For UndoRedo, we don't.
		if (sync.msg == ksyncMergePss && !m_qlpi->GetPossList(sync.hvo, 0, 0))
			break;
		else
		{
			ClearFilterMenuNodes();
			ClearSortMenuNodes();
			LoadData();
			if (sync.msg == ksyncPromoteEntry || sync.msg == ksyncMoveEntry)
			{
				// The promoted entry needs to be located in case it is no longer in the
				// same location. By using the top item, we should be able to find it even
				// if the subentry is nested. Note, this is done after filters and sorting
				// is done in LoadData().
				HVO hvoRec = m_vhvoPath[0]; // Top-level record
				int ihvo;
				for (ihvo = m_vhcFilteredRecords.Size(); --ihvo >= 0; )
				{
					if (m_vhcFilteredRecords[ihvo].hvo == hvoRec)
					{
						m_ihvoCurr = ihvo;
						break;
					}
				}
				if (ihvo < 0)
					m_ihvoCurr = 0; // Couldn't find record?? Use first record.
			}
			int crncw = m_qmdic->GetChildCount();
			for (int irncw = 0; irncw < crncw; irncw++)
			// Now refresh the windows.
			{
				AfClientWnd * pafcwT = m_qmdic->GetChildFromIndex(irncw);
				AfClientRecWnd * pafcrwT = dynamic_cast<AfClientRecWnd *>(pafcwT);
				if (pafcrwT && !pafcrwT->FullRefresh())
					return false;
			}
			UpdateStatusBar();
			UpdateCaptionBar();
			return true;
		}

	case ksyncDelPss:
		{
			// If an overlay is active for this list, we need to update the overlay
			// to keep the display in sync.
			for (int ioi = m_qlpi->GetOverlayCount(); --ioi >= 0; )
			{
				AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(ioi);
				if (aoi.m_hvoPssl == sync.hvo)
					OnChangeOverlay(ioi);
			}

			// Pass this on to clients.
			int crncw = m_qmdic->GetChildCount();
			for (int irncw = 0; irncw < crncw; irncw++)
			{
				AfClientWnd * pafcwT = m_qmdic->GetChildFromIndex(irncw);
				AfClientRecWnd * pafcrwT = dynamic_cast<AfClientRecWnd *>(pafcwT);
				if (pafcrwT && !pafcrwT->Synchronize(sync))
					return false;
			}
			return true;
		}

	case ksyncStyle:
		{
			OnStylesheetChange();
			return true;
		}

	// Numerous other things in here.

	default:
		break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Get ready to synchronize all clients and dialogs with any changes made in the database.
	This should not be necessary, but things are too intricately tied together. For example,
	LoadViewBar needs to be called to get the viewbar and record list loaded, but it also
	tries to display the client window. But at this point the client window isn't ready to
	display, giving crashes with lazy boxes since we've cleared the	cache. So to get around
	this at the moment, we use this method to clear the lazy load stuff on all view windows
	before calling LoadViewBar.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::PreSynchronize(SyncInfo & sync)
{
	switch(sync.msg)
	{
	// Any sync message that clears the view cache should be included here.
	case ksyncWs:
	case ksyncFullRefresh:
		{
			AfClientWnd * pafcw = m_qmdic->GetCurChild();
			m_wIdOldSel = pafcw->GetWindowId();
			m_stuOldSel = pafcw->GetViewName();

			for (int icwnd = m_qmdic->GetChildCount(); --icwnd >= 0; )
			{
				AfClientWnd * pafcw = m_qmdic->GetChildFromIndex(icwnd);
				AssertObj(pafcw);
				m_qvwbrs->DeleteListItem(m_ivblt[kvbltView], pafcw->GetViewName());
			}
			m_qmdic->DelAllChildWnds();
		}
	case ksyncMoveEntry:
	case ksyncUndoRedo:
	case ksyncMergePss:
	case ksyncPromoteEntry:
		{
			// If we have a field editor open, save the information so we can reopen
			// it after the field editors have been recreated. We need to do this in order
			// to allow the list chooser to call ChooserApplied on the open editor.
			AfDeSplitChild * padsc = CurrentDeWnd();
			if (padsc)
			{
				AfDeFieldEditor * pdfe = padsc->GetActiveFieldEditor();
				if (pdfe)
					pdfe->SaveFullCursorInfo();
			}
			// We want to force all editors closed without any checks or saves.
			// Otherwise we can get into nasty problems. One example is using type-ahead
			// to add an existing item in the notebook researcher field, then before
			// moving out of the field, use a list editor to merge this item with another
			// item. When the sync process tries to update the notebook, it will fail
			// when trying to save the researcher field because it is trying to send a
			// replace to the database where the old value is no longer in the database.
			CloseAllEditors(true);
			//ShowAllOverlays(false);
			// Clear out lazy load stuff.
			int crncw = m_qmdic->GetChildCount();
			int irncw;
			for (irncw = 0; irncw < crncw; irncw++)
			{
				AfClientWnd * pafcw = m_qmdic->GetChildFromIndex(irncw);
				if (pafcw && !pafcw->PreSynchronize(sync))
					return false;
			}
		}
		break;

	default:
		break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Reload data and redisplay all windows including open dialogs.
	This assumes that the AfLpInfo has already been refereshed.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::FullRefresh()
{
	int iViewList = m_ivblt[kvbltView]; //GetViewbarListIndex
	// There shouldn't be any items in the view bar.
	Assert(m_qvwbrs->GetViewBar()->GetList(iViewList)->GetSize() == 0);

	// Re-do initialization of the stylesheet to make sure it is up to date.
	AfDbStylesheet * pdst = dynamic_cast<AfDbStylesheet *>(m_qlpi->GetAfStylesheet());
	pdst->Init(m_qlpi, m_qlpi->GetLpId());

	// Before we refresh, reload our internal cache of the records.
	// (See CLE-76 for a crash that can occur b/c this wasn't happening.)
	ReloadRecordsFromDatabase();

	// Create any new windows and add them to the view bar(s).
	const OLECHAR * pwrgch;
	int cch;
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	UserViewSpecVec & vuvs = pdbi->GetUserViewSpecs();
	int cuvs = vuvs.Size();
	int wid = m_qmdic->GetClientIndexLim();

	// Recreate the child windows that should appear in this main window.
	Vector<AfClientWndPtr> vqafcw;
	for (int iuvs = 0; iuvs < cuvs; ++iuvs, ++wid)
	{
		vuvs[iuvs]->m_qtssName->LockText(&pwrgch, &cch);
		if (wcsncmp(m_stuOldSel.Chars(), pwrgch, cch) == 0)
			m_wIdOldSel = wid;
		vuvs[iuvs]->m_qtssName->UnlockText(pwrgch);
		CreateClient(vuvs[iuvs]->m_vwt, vqafcw, wid);
	}
	Assert(vqafcw.Size() == vuvs.Size());

	m_qmdic->SetClientIndexLim(wid);

	// Add the (now sorted) children to the MDI client window and to the view bar.
	for (int iuvs = 0; iuvs < cuvs; ++iuvs)
	{
		AfClientWnd * pafcw = vqafcw[iuvs];
		AssertPtr(pafcw);
		vuvs[iuvs]->m_iwndClient = iuvs;

		StrApp str;
		const OLECHAR * pwrgch;
		int cch;
		CheckHr(vuvs[iuvs]->m_qtssName->LockText(&pwrgch, &cch));
		str.Assign(pwrgch, cch);
		vuvs[iuvs]->m_qtssName->UnlockText(pwrgch);
		pafcw->SetViewName(str);

		m_qmdic->AddChild(pafcw);
	}

	// Clear filter and sort menus. (They will reload later when needed).
	ClearFilterMenuNodes();
	ClearSortMenuNodes();
	m_vhcFilteredRecords.Clear(); // See CLE-78, it's possible to compute exactly the same set of records and not rebuild the display.
	// Reload viewbar which also loads filter/sort index.
	LoadViewBar();
	int iwndSel = m_qmdic->GetChildIndexFromWid(m_wIdOldSel);
	if (iwndSel == -1)
		iwndSel = 0;

	Set<int> sisel;
	sisel.Insert(iwndSel);
	m_qvwbrs->SetSelection(iViewList, sisel);

	// Load the page setup information.
	LoadPageSetup();
	UpdateTitleBar();
	UpdateToolBarWrtSysControl();

	// Putting this in causes crash when refreshing DE view with Doc view hidden.
	//ShowAllOverlays(true, true);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Actually reload our internal record caches according to the current state of the database.
	See CLE-76 for a crash that can occur b/c this wasn't happening.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::ReloadRecordsFromDatabase()
{
	// 1) Save our current record.
	// (copied from RecMainWnd::UpdateRecordDate())
	int hvoCurrOld = 0;
	if (m_ihvoCurr != -1 && m_vhcFilteredRecords.Size())
		hvoCurrOld = m_vhcFilteredRecords[m_ihvoCurr].hvo;
	// 2) Clear our cache of the records
	// (copied from RecMainWnd::OnClose())
	m_vhcRecords.Clear();
	m_vhcFilteredRecords.Clear();
	// 3) Reload list from database
	// (copied from RecMainWnd::PostAttach)
	LoadMainData(); // reload m_vhcRecords
	// NOTE: ApplyFilterAndSort will get called later if it needs to.
	m_vhcFilteredRecords = m_vhcRecords;
	// 4) Restore current record (if it still exists)
	// (copied from RecMainWnd::ApplyFilterAndSort)
	// Find out the index to set the client windows to.
	m_ihvoCurr = 0;
	int crecNew = m_vhcFilteredRecords.Size();
	bool fFoundRecord = false;
	for (int irecNew = 0; irecNew < crecNew; irecNew++)
	{
		if (m_vhcFilteredRecords[irecNew].hvo == hvoCurrOld)
		{
			SetCurRecIndex(irecNew);
			fFoundRecord = true;
			break;
		}
	}
	if (!fFoundRecord && crecNew > 0)
		SetCurRecIndex(0);
}



/*----------------------------------------------------------------------------------------------
	Turn off filtering and sorting
----------------------------------------------------------------------------------------------*/
void RecMainWnd::DisableFilterAndSort()
{
	Set<int> sisel;
	m_qvwbrs->SetSelection(m_ivblt[kvbltFilter], sisel);
	m_qvwbrs->SetSelection(m_ivblt[kvbltSort], sisel);
	m_vhcFilteredRecords = m_vhcRecords;
}

/*----------------------------------------------------------------------------------------------
	Find the available XSL transform files and extract from them the types of views that can be
	exported.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::FindExportableViews()
{
	UserViewType vwt;
	// Get the FieldWorks code root directory:
	StrApp strFwRoot(AfApp::Papp()->GetFwCodePath());

	// Look for all the export option *.xsl files, and extract the necessary information
	// from each of them.
	StrAnsi staDir(strFwRoot.Chars(), strFwRoot.Length());
	_mkdir(staDir.Chars());
	staDir.Append("\\ExportOptions");
	_mkdir(staDir.Chars());
	StrAnsi staFile;
	staFile.Format("%s\\*.xsl*", staDir.Chars());
	struct _finddata_t fileinfo;
	intptr_t hf = _findfirst(staFile.Chars(), &fileinfo);
	if (hf != -1)
	{
		StrAnsi staFile;
		ExportData ed;
		do
		{
			staFile.Format("%s\\%s", staDir.Chars(), fileinfo.name);
			ed.m_staViews.Clear();
			ed.m_fDone = false;
			const XML_Char * pszWritingSystem = NULL;
			XML_Parser parser = XML_ParserCreate(pszWritingSystem);
			if (!XML_SetBase(parser, staFile.Chars()))
			{
				XML_ParserFree(parser);
				continue;
			}
			try
			{
				XML_SetUserData(parser, &ed);
				XML_SetElementHandler(parser, HandleStartTag, HandleEndTag);
				IStreamPtr qstrm;
				FileStream::Create(staFile.Chars(), kfstgmRead, &qstrm);
				for (;;)
				{
					ulong cbRead;
					void * pBuffer = XML_GetBuffer(parser, READ_SIZE);
					if (!pBuffer)
						break;
					CheckHr(qstrm->Read(pBuffer, READ_SIZE, &cbRead));
					if (cbRead == 0)
						break;
					if (!XML_ParseBuffer(parser, cbRead, cbRead == 0))
						break;
					if (ed.m_fDone)
					{
						if (ed.m_staViews.Length())
						{
							int ich = ed.m_staViews.FindStrCI("browse");
							if (ich >= 0)
							{
								vwt = kvwtBrowse;
								m_setvwtExportable.Insert(vwt);
							}
							ich = ed.m_staViews.FindStrCI("dataentry");
							if (ich >= 0)
							{
								vwt = kvwtDE;
								m_setvwtExportable.Insert(vwt);
							}
							ich = ed.m_staViews.FindStrCI("document");
							if (ich >= 0)
							{
								vwt = kvwtDoc;
								m_setvwtExportable.Insert(vwt);
							}
							ich = ed.m_staViews.FindStrCI("concordance");
							if (ich >= 0)
							{
								vwt = kvwtConc;
								m_setvwtExportable.Insert(vwt);
							}
							ich = ed.m_staViews.FindStrCI("draft");
							if (ich >= 0)
							{
								vwt = kvwtDraft;
								m_setvwtExportable.Insert(vwt);
							}
						}
						break;
					}
				}
			}
			catch (...)
			{
				// Ignore any errors in parsing.
			}
			XML_ParserFree(parser);
		} while (!_findnext(hf, &fileinfo));
		_findclose(hf);
	}
	if (!m_setvwtExportable.Size())
	{
		// Make sure the set is non-empty so we won't repeat all this effort.
		vwt = kvwtLim;					// Non-existent view:  count of view types.
		m_setvwtExportable.Insert(vwt);
	}
}

/*----------------------------------------------------------------------------------------------
	Finish initializing the window immediately after it is created before other events
	happen.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::PostAttach(void)
{
	AssertObj(this);
	Assert(m_hwnd != NULL);
	Assert(!m_qstbr);
	AssertPtr(m_qlpi);

	SuperClass::PostAttach();

	// Create the MDI child window.
	WndCreateStruct wcs;
	wcs.InitChild(_T("STATIC"), m_hwnd, kwidMdiClient);
	wcs.style |= WS_VISIBLE;
	wcs.dwExStyle |= WS_EX_CLIENTEDGE;
	wcs.lpCreateParams = this;
	m_qmdic.Create();
	m_qmdic->CreateAndSubclassHwnd(wcs);
	InitMdiClient();

	// Create the pager bar shell.
	m_qvwbrs.Create();
	m_qvwbrs->Create(m_hwnd, kwidViewBar);

	FindExportableViews();

	AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	StrAppBuf strbTemp; // Holds temp string, e.g., strings used as tab captions.
	// Set the default caption text in case the database cannot be opened.
	strbTemp.Load(pdapp->GetAppNameId());
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)strbTemp.Chars());

	UpdateTitleBar();

	// Set data access.
	m_qlpi->GetDataAccess(&m_qcvd);
	//  Set the Action Handler
	m_qlpi->GetActionHandler(&m_qacth);

	LoadMainData();

	m_vhcFilteredRecords = m_vhcRecords;
	m_qcvd->CreateDummyID(&m_hvoFilterVec);
	m_qcvd->CreateDummyID(&m_hvoPrintSelVec);
	// This must be something that FwMetaDataCache::GetFieldType will recognize, but not think
	// is owning, or when we insert a record, VwCacheDa::ReplaceAux will cache information
	// indicating that the dummy property is the owner. Safest is to use a value from the
	// allocated dummy range.
	m_flidFilteredRecords = kflidStartDummyFlids;

	// Create the toolbars.
	const int rgrid[] =
	{
		kridTBarStd,
		kridTBarFmtg,
		kridTBarData,
		kridTBarView,
		kridTBarIns,
		kridTBarTools,
		kridTBarWnd,
		kridTBarInvisible,
	};

	GetMenuMgr()->LoadToolBars(rgrid, SizeOfArray(rgrid));
	GetMenuMgr()->LoadAccelTable(kridAccelStd, 0, m_hwnd);

	// Load the view bar information
	LoadViewBar();

	// Create the menu bar.
	AfMenuBarPtr qmnbr;
	qmnbr.Create();
	strbTemp.Load(kstidMenu);
	qmnbr->Initialize(m_hwnd, kridAppMenu, kridAppMenu, strbTemp.Chars());
	m_vqtlbr.Push(qmnbr.Ptr());

	// Create the toolbars.
	AfToolBarPtr qtlbr;
	ILgWritingSystemFactoryPtr qwsf;
	m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);

	qtlbr.Create();
	strbTemp.Load(kstidTBarStd);
	qtlbr->Initialize(kridTBarStd, kridTBarStd, strbTemp.Chars());
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);

	qtlbr.Create();
	strbTemp.Load(kstidTBarFmtg);
	qtlbr->Initialize(kridTBarFmtg, kridTBarFmtg, strbTemp.Chars());
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);

	qtlbr.Create();
	strbTemp.Load(kstidTBarData);
	qtlbr->Initialize(kridTBarData, kridTBarData, strbTemp.Chars());
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);

	/*qtlbr.Create();
	qtlbr->Initialize(kridTBarView, kridTBarView, "View", qwsf);
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);*/

	qtlbr.Create();
	strbTemp.Load(kstidTBarIns);
	qtlbr->Initialize(kridTBarIns, kridTBarIns, strbTemp.Chars());
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);

	qtlbr.Create();
	strbTemp.Load(kstidTBarTools);
	qtlbr->Initialize(kridTBarTools, kridTBarTools, strbTemp.Chars());
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);

	qtlbr.Create();
	strbTemp.Load(kstidTBarWnd);
	qtlbr->Initialize(kridTBarWnd, kridTBarWnd, strbTemp.Chars());
	qtlbr->SetWritingSystemFactory(qwsf);
	m_vqtlbr.Push(qtlbr);

	pdapp->AddCmdHandler(this, 1);
	pdapp->SetSplashMessage(kridSplashFinishingMessage);

	LoadPageSetup();
	LoadSettings(NULL, false);

	// Update the icons for the formatting toolbar drop-down buttons.
	UpdateToolBarIcon(kridTBarFmtg, kcidFmttbApplyBgrndColor, m_clrBack);
	UpdateToolBarIcon(kridTBarFmtg, kcidFmttbApplyFgrndColor, m_clrFore);
	UpdateToolBarIcon(kridTBarFmtg, kcidFmttbApplyBdr, (COLORREF) m_bpBorderPos);

	// Set the status bar to a safe default state.
	m_qstbr->RestoreStatusText();

	// Let the status bar know the user interface writing system id.
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	m_qstbr->SetUserWs(wsUser);
}


/*----------------------------------------------------------------------------------------------
	Return the size of the client area minus the viewbar if it is visible.

	@param Out rc Rectangle
----------------------------------------------------------------------------------------------*/
void RecMainWnd::GetClientRect(Rect & rc)
{
	AssertPtrN(m_qvwbrs);

	SuperClass::GetClientRect(rc);

	if (m_qvwbrs)
	{
		Rect rcT;
		if (::IsWindowVisible(m_qvwbrs->Hwnd()))
		{
			::GetClientRect(m_qvwbrs->Hwnd(), &rcT);
			rc.left += rcT.Width();
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Make sure the width of the view bar is reasonable.
	Reposition our child windows.

	@return false
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::OnClientSize(void)
{
	// See the full explanation for the need for this under AfMainWnd::OnSize. This is called
	// from AfWnd::FWndProcPre on WM_SIZE and needs to be aborted if the main window is deemed
	// invisible.
	if (!::IsWindowVisible(m_hwnd))
		return true;

	Rect rc;
	// WARNING: Do not call RecMainWnd::GetClientRect because if subtracts the width of the
	// view bar, which is not what we want here.
	::GetClientRect(m_hwnd, &rc);

	// The width is zero when we are being minimized. Don't do anything in this case.
	if (!rc.Width())
		return true;

	m_dxpLeft = Max(kdxpMinViewBar, m_dxpLeft);
	m_dxpLeft = Min(m_dxpLeft, rc.Width() - kdxpSplitter - kdxpMinClient);

	SuperClass::OnClientSize();

	SuperClass::GetClientRect(rc);
	if (m_qvwbrs && ::IsWindowVisible(m_qvwbrs->Hwnd()))
	{
		::MoveWindow(m_qvwbrs->Hwnd(), 0, rc.top, m_dxpLeft, rc.Height(), true);
		::UpdateWindow(m_qvwbrs->Hwnd());
		rc.left += m_dxpLeft + kdxpSplitter; // Allow extra space for the "splitter" bar.
	}

	if (m_qmdic)
	{
		::MoveWindow(m_qmdic->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);
		::UpdateWindow(m_qmdic->Hwnd());
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Start an 'undoable' task. May throw en exception.

	@param hvoOwner Id of the owner of the new object.
	@param flidOwnerModified flid on owner for modified date, or '0' for none.
	@param kstid Id of the string resource to be used for 'Undo/Redo' message.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::StartUndoableTask(HVO hvoOwner, int flidOwnerModified, int kstid)
{
	Assert(m_qcvd);

	StrUni stu(kstid);
	StrUni stuUndoFmt;
	StrUni stuRedoFmt;
	StrUtil::MakeUndoRedoLabels(kstidUndoInsertX, &stuUndoFmt, &stuRedoFmt);
	StrUni stuUndo;
	StrUni stuRedo;
	stuUndo.Format(stuUndoFmt.Chars(), stu.Chars());
	stuRedo.Format(stuRedoFmt.Chars(), stu.Chars());
	CheckHr(m_qcvd->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
	// Set modified for owner.
	if (flidOwnerModified)
	{
		SilTime stim = SilTime::CurTime();
		CheckHr(m_qcvd->SetTime(hvoOwner, flidOwnerModified, stim.AsInt64()));
	}
}


/*----------------------------------------------------------------------------------------------
	This is called when any menu item is expanded, and allows the app to modify the menu. We
	use it here to see if the Split Window menu item needs to be replaced with Remove Split.

	@param hmenu Handle to the menu that is being expanded right now.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::FixMenu(HMENU hmenu)
{
	int cmni = ::GetMenuItemCount(hmenu);
	int imni;

	for (imni = cmni - 1; imni > -1; imni--)
	{
		UINT nmId = GetMenuItemID(hmenu, imni);
		if (nmId == kcidWndSplit)
		{
			// Check whether the window is already split or not:
			AfSplitterClientWnd * pscw =
				dynamic_cast<AfSplitterClientWnd *>(m_qmdic->GetCurChild());
			AssertPtr(pscw);

			if (pscw->GetPane(1) == NULL)
			{
				// Make this menu item say "Split Window":
				StrApp str(kcidWndSplitOn);
				::ModifyMenu(hmenu, kcidWndSplit, MF_BYCOMMAND | MF_STRING, kcidWndSplit,
					str.Chars());
			}
			else
			{
				// Make this menu item say "Remove split":
				StrApp str(kcidWndSplitOff);
				::ModifyMenu(hmenu, kcidWndSplit, MF_BYCOMMAND | MF_STRING, kcidWndSplit,
					str.Chars());
			}
		}
	}
	SuperClass::FixMenu(hmenu);
}



/*----------------------------------------------------------------------------------------------
	Process notifications for this dialog from some event on a control.  This method is called
	by the framework.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.
	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	if (pnmh->code == CBN_DROPDOWN && ctid == kcidFmttbStyle)
		return OnStyleDropDown(pnmh->hwndFrom);

	return false;
}


/*----------------------------------------------------------------------------------------------
	Respond to the File Save menu item.

	@param pcmd Menu command (This parameter is not used in in this method.)
	@return true if sucessful
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdFileSave(Cmd * pcmd)
{
	if (!m_qcvd)
		return true;	// e.g., call before loading first record

	AfSplitterClientWnd * afscw =
		dynamic_cast<AfSplitterClientWnd *>(m_qmdic->GetCurChild());
	AssertPtr(afscw);
	if (!afscw->CurrentPane()->Save())
		return false;
	SaveData();
	m_cActUndoLastChange = 0;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Enable the File Save menu item.

	@param cms Menu command state
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsFileSave(CmdState & cms)
{
//	Assert(m_qacth);
//	ComBool fCanUndo;
//	m_qacth->CanUndo(&fCanUndo);
//	cms.Enable(fCanUndo);

	cms.Enable(true); // always allowed, regardless of whether any changes have been made
	return true;
}

/*----------------------------------------------------------------------------------------------
	Enable or disable the File Export menu item.

	@param cms Menu command state
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsFileExport(CmdState & cms)
{
	// TODO RandyR: Remove this next call, since it is the main window.
	AfClientWnd * pafcw = dynamic_cast<AfClientWnd *>(m_qmdic->GetCurChild());
	AssertPtr(pafcw);
	int iview = m_qmdic->GetChildIndexFromWid(pafcw->GetWindowId());
	UserViewSpecVec & vuvs = m_qlpi->GetDbInfo()->GetUserViewSpecs();

	if (m_setvwtExportable.IsMember(vuvs[iview]->m_vwt))
		cms.Enable(true);
	else
		cms.Enable(false);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Enter What's This help mode.

	@param pcmd menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdHelpMode(Cmd * pcmd)
{
	ToggleHelpMode();
	return true;
}


/*----------------------------------------------------------------------------------------------
	If the current client window is already split, unsplit it. Otherwise split it in half
	horizontally.

	@param pcmd menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdWndSplit(Cmd * pcmd)
{
	AssertPtr(pcmd);
	AfSplitterClientWnd * pscw = dynamic_cast<AfSplitterClientWnd *>(m_qmdic->GetCurChild());
	AssertPtr(pscw);

	if (pscw->GetPane(1) == NULL)
		pscw->SplitWindow(-1);
	else
		pscw->UnsplitWindow(true);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the state of the split window toolbar/menu item. It should be checked when the window
	is split.

	@param cms menu command state

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsWndSplit(CmdState & cms)
{
	AfSplitterClientWnd * pscw = dynamic_cast<AfSplitterClientWnd *>(m_qmdic->GetCurChild());
	// This can be NULL when the main window is first getting created because the
	// child windows have not been created yet.
	AssertPtrN(pscw);
	if (pscw)
		cms.SetCheck(pscw->GetPane(1));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Toggle the visibility of the view bar.

	@param pcmd Ptr to menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdVbToggle(Cmd * pcmd)
{
	AssertObj(pcmd);

	m_qvwbrs->ShowViewBar(!::IsWindowVisible(m_qvwbrs->Hwnd()));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the state of the view bar toolbar/menu item.

	@param cms menu command state

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsVbUpdate(CmdState & cms)
{
	// Disable if we're in full window mode.
	cms.Enable(!m_fFullWindow);

	if (m_fFullWindow)
		cms.SetCheck(m_fOldViewbarVisible);
	else
		cms.SetCheck(::IsWindowVisible(m_qvwbrs->Hwnd()));

	return true;
}


/*----------------------------------------------------------------------------------------------
	Toggle the visibility of the status bar.

	@param pcmd Ptr to menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdSbToggle(Cmd * pcmd)
{
	AssertObj(pcmd);

	if (::IsWindowVisible(m_qstbr->Hwnd()))
	{
		::ShowWindow(m_qstbr->Hwnd(), SW_HIDE);
	}
	else
	{
		::ShowWindow(m_qstbr->Hwnd(), SW_SHOW);
		UpdateStatusBar();
	}

	::SendMessage(m_hwnd, WM_SIZE, kwstRestored, 0);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the state of the status bar toolbar/menu item.

	@param cms menu command state

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsSbUpdate(CmdState & cms)
{
	cms.SetCheck(::IsWindowVisible(m_qstbr->Hwnd()));
	return true;
}


/*----------------------------------------------------------------------------------------------
	The user has changed Window's settings.

	@param pcmd Ptr to menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdSettingChange(Cmd * pcmd)
{
	AssertObj(this);
	AssertObj(pcmd);
	SuperClass::CmdSettingChange(pcmd);

	if (pcmd->m_cid == kcidSettingChange)
	{
		// Update current client area (so dates, etc. will update)
		Rect rc;
		AfWndPtr qwnd = m_qmdic->GetCurChild();
		::InvalidateRect(qwnd->Hwnd(), NULL, false);
		::UpdateWindow(qwnd->Hwnd());
	}

	return true;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdViewFullWindow(Cmd * pcmd)
{
	AssertPtr(pcmd);
	if (!m_fFullWindow)
		SaveToolbars(AfApp::GetSettings(), NULL, _T("Toolbar Flags"));
	SetWindowMode(!m_fFullWindow);
	return true;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsViewFullWindow(CmdState & cms)
{
	cms.SetCheck(m_fFullWindow);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle the File / Language Project Properties command.

	@param pcmd Pointer to the menu command.

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdFileProjProps(Cmd * pcmd)
{
	WaitCursor wc;

	AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	Assert(pdapp);
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);

	if (!pdapp->AreAllWndsOkToChange(pdbi, false)) // Don't check required fields.
		return true;

	if (!pdapp->SaveAllWndsEdits(pdbi))
		return true;

	AssertObj(pcmd);
	StrApp strName(m_qlpi->PrjName());
	StrApp strType(kstidLangProject);

	StrApp strLoc = pdbi->ServerName();
	if (strLoc.Length())
		strLoc.Replace(strLoc.Length() - 6, strLoc.Length(), " "); // Remove /SILFW from server.
	bool fNetworked = false;
	achar pszCompName[MAX_COMPUTERNAME_LENGTH + 1];
	ulong cch = isizeof(pszCompName);
	if (::GetComputerName(pszCompName, &cch))
	{
		if (!strLoc.Left(min(int(_tcslen(pszCompName)), strLoc.Length())).EqualsCI(pszCompName))
			fNetworked = true;
	}
	strLoc.Append(m_qlpi->PrjName());

	StrUni stuOldExtLinkRoot(m_qlpi->GetExtLinkRoot(false));

	HICON hicon;
	int ridIcon = fNetworked ? kridGeneralPropertiesNwIcon : kridGeneralPropertiesObjIcon;
	hicon = ::LoadIcon(pdapp->GetInstance(), MAKEINTRESOURCE(ridIcon));

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	OLECHAR rgch[MAX_PATH];
	pdbi->GetDbAccess(&qode);
	StrUni stuSql;

	StrApp strDescription;
	stuSql.Format(L"exec GetOrderedMultiTxt '%d', %d, 1", m_qlpi->GetLpId(),
		kflidCmProject_Description);
	CheckHr(qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	Assert(fMoreRows); // This proc should always return something.
	CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgch), isizeof(rgch), &cbSpaceTaken,
		&fIsNull, 2));
	if (cbSpaceTaken > isizeof(rgch))
	{
		Vector<OLECHAR> vch;
		vch.Resize(cbSpaceTaken / isizeof(OLECHAR));
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(vch.Begin()),
			vch.Size() * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
		strDescription.Assign(vch.Begin());
	}
	else
	{
		strDescription.Assign(rgch);
	}

	qodc.Clear();

	FwCoreDlgs::IFwProjPropertiesDlgPtr qfwppd;
	qfwppd.CreateInstance("FwCoreDlgs.FwProjPropertiesDlg");
	long nRet;
	pdbi->GetDbAccess(&qode);
	IFwMetaDataCachePtr qmdc;
	pdbi->GetFwMetaDataCache(&qmdc);
	IVwOleDbDaPtr qda;
	m_qcvd->QueryInterface(IID_IVwOleDbDa, (void**)&qda);

	const CLSID * pclsid = AfApp::Papp()->GetAppClsid();
	AssertPtr(pclsid);
	IFwToolPtr qfwt;
	IHelpTopicProviderPtr qhtprov = new HelpTopicProvider(AfApp::Papp()->GetHelpBaseName());
	IRunningObjectTablePtr qrot;
	CheckHr(::GetRunningObjectTable(0, &qrot));
	IMonikerPtr qmnk;
	CheckHr(::CreateClassMoniker(*pclsid, &qmnk));
	IUnknownPtr qunk;
	HRESULT hr;
	IgnoreHr(hr = qrot->GetObject(qmnk, &qunk));
	if (hr == S_OK)
		IgnoreHr(qunk->QueryInterface(IID_IFwTool, (void **)&qfwt));
	IStreamPtr qstrmLog;
	AfApp::Papp()->GetLogPointer(&qstrmLog);

	//We need the style sheet!
	IVwStylesheetPtr qvss;
	AfStylesheet * pasts = GetStylesheet();
	AssertPtr(pasts);
	CheckHr(pasts->QueryInterface(IID_IVwStylesheet, (void **)&qvss));
	CheckHr(qfwppd->Initialize(qode, qmdc, qda, qfwt, qhtprov, qstrmLog, m_qlpi->GetLpId(), m_qlpi->ObjId(),
		UserWs(), qvss));

	// Clear all the smart pointers before actually running the dialog.
	qvss.Clear();
	qode.Clear();
	qmdc.Clear();
	qda.Clear();
	qfwt.Clear();
	qhtprov.Clear();
	qstrmLog.Clear();
	qunk.Clear();
	qmnk.Clear();
	qrot.Clear();

	CheckHr(qfwppd->ShowDlg(&nRet));
	if (nRet != 1) // DialogResult.OK
	{
		qfwppd->DisposeDialog(); // allows it to free IFwTool interface ptr so DN can exit!
		return true;
	}

	VARIANT_BOOL fExtLnkChg;
	VARIANT_BOOL fProjChg;
	VARIANT_BOOL fWsChg;
	CheckHr(qfwppd->ExternalLinkChanged(&fExtLnkChg));
	CheckHr(qfwppd->ProjectNameChanged(&fProjChg));
	CheckHr(qfwppd->WritingSystemsChanged(&fWsChg));
	CheckHr(qfwppd->DisposeDialog()); // allows it to free IFwTool interface ptr so DN can exit!
	qfwppd.Clear();
	if (fExtLnkChg)
	{
		if (stuOldExtLinkRoot.Length() == 0)
			stuOldExtLinkRoot.Assign(DirectoryFinder::FwRootDataDir());
		StrUni stuNewExtLinkRoot(m_qlpi->GetExtLinkRoot(true));
		Vector<MovableFile> vmovFiles;
		CollectMovableFiles(vmovFiles, stuOldExtLinkRoot, stuNewExtLinkRoot);
		if (vmovFiles.Size() > 0)
		{
			IHelpTopicProviderPtr qhtprov =
				new HelpTopicProvider(AfApp::Papp()->GetHelpBaseName());
			FwCoreDlgs::IFwMoveOrCopyFilesDlgPtr qmcf;
			qmcf.CreateInstance("FwCoreDlgs.MoveOrCopyFilesDlg");
			qmcf->Initialize(vmovFiles.Size(), stuOldExtLinkRoot.Bstr(),
				stuNewExtLinkRoot.Bstr(), qhtprov);
			qmcf->ShowDlg(&nRet);
			if (nRet == 1)
			{
				VARIANT_BOOL fCopyFiles;
				VARIANT_BOOL fFixPaths;
				VARIANT_BOOL fMoveFiles;
				qmcf->CopyFiles(&fCopyFiles);
				qmcf->LeaveFiles(&fFixPaths);
				qmcf->MoveFiles(&fMoveFiles);
				qmcf.Clear();
				if (fFixPaths)
				{
					ExpandToFullPath(vmovFiles, stuOldExtLinkRoot);
				}
				else if (fCopyFiles || fMoveFiles)
				{
					StrApp strOld;
					StrApp strNew;
					StrApp strNewDir;
					for (int i = 0; i < vmovFiles.Size(); ++i)
					{
						strOld.Assign(SilUtil::PathCombine(stuOldExtLinkRoot.Chars(),
										  vmovFiles[i].stuInternalPath));
						strNew.Assign(SilUtil::PathCombine(stuNewExtLinkRoot.Chars(),
										  vmovFiles[i].stuInternalPath));
						int cchLast = strNew.ReverseFindCh('\\');
						if (cchLast == -1)
							cchLast = strNew.ReverseFindCh('/');
						if (cchLast != -1)
						{
							strNewDir.Assign(strNew.Chars(), cchLast);
							MakeDir(strNewDir.Chars());
						}
						BOOL fOk;
						if (fMoveFiles)
						{
							fOk = ::MoveFileEx(strOld.Chars(), strNew.Chars(),
								MOVEFILE_COPY_ALLOWED | MOVEFILE_WRITE_THROUGH);
						}
						else
						{
							fOk = ::CopyFile(strOld.Chars(), strNew.Chars(), true);
						}
						if (!fOk)
						{
							achar rgchError[2000];
							DWORD dwError = ::GetLastError();
							::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, dwError,
								LANG_USER_DEFAULT, rgchError, 2000, NULL);
							StrApp strFmt;
							StrApp strMsg;
							if (fMoveFiles)		// "Error trying to move %<0>s to %<1>s: %<2>s"
								strFmt.Load(kstidCannotMoveFile);
							else				// "Error trying to copy %<0>s to %<1>s: %<2>s"
								strFmt.Load(kstidCannotCopyFile);
							StrApp strTitle(kstidMiscError);
							strMsg.Format(strFmt.Chars(),
								strOld.Chars(), strNew.Chars(), rgchError);
							::MessageBox(NULL, strMsg.Chars(), strTitle.Chars(), MB_OK);
						}
					}
				}
			}
		}
	}
	if (fProjChg)
	{
		// Rename the project (database).
		pdbi->GetDbAccess(&qode);
		SmartBstr sbstrServer;
		SmartBstr sbstrDatabase;
		SmartBstr sbstrProject;
		CheckHr(qode->get_Server(&sbstrServer));
		CheckHr(qode->get_Database(&sbstrDatabase));
		m_qlpi->GetCurrentProjectName(&sbstrProject);
		qode.Clear();
		HRESULT hr;
		IBackupDelegates * pbkupd = dynamic_cast<IBackupDelegates *>(AfApp::Papp());
		pbkupd->IncExportedObjects_Bkupd();
		ComBool fWindowClosed;
		IgnoreHr(hr = pbkupd->CloseDbAndWindows_Bkupd(sbstrServer, sbstrDatabase, false,
			&fWindowClosed));
		// Calling the project properties dialog (which is written in C#) leaves some
		// connections open to the database which I haven't been able to track down in
		// 2-3 days of effort.  So we'll use this handy sledgehammer to fix things...
		try
		{
			IDisconnectDbPtr qdscdb;
			qdscdb.CreateInstance(CLSID_FwDisconnect);
			// Set up strings needed for disconnection:
			StrUni stuReason(L"The database is being renamed.");
			DWORD nBufSize = MAX_COMPUTERNAME_LENGTH + 1;
			achar rgchBuffer[MAX_COMPUTERNAME_LENGTH + 1];
			::GetComputerName(rgchBuffer, &nBufSize);
			StrUni stuComputer(rgchBuffer);
			StrUni stuExternal;
			stuExternal.Format(L"The project %<0>s on %<1>s is being renamed to %<2>s.",
				sbstrDatabase.Chars(), stuComputer.Chars(), sbstrProject.Chars());
			IgnoreHr(hr = qdscdb->Init(sbstrDatabase, sbstrServer, stuReason.Bstr(),
				stuExternal.Bstr(), (ComBool)false, NULL, 0));
			IgnoreHr(hr = qdscdb->ForceDisconnectAll());
		}
		catch (...)
		{
		}
		if (SUCCEEDED(hr))
		{
			try
			{
				IStreamPtr qfist;
				AfApp::Papp()->GetLogPointer(&qfist);
				IDbAdminPtr qmda;
				qmda.CreateInstance(CLSID_DbAdmin);
				CheckHr(qmda->SimplyRenameDatabase(sbstrDatabase, sbstrProject));
				if (fWindowClosed)
					IgnoreHr(hr=pbkupd->ReopenDbAndOneWindow_Bkupd(sbstrServer, sbstrProject));
			}
			catch (...)
			{
				if (fWindowClosed)
					IgnoreHr(hr=pbkupd->ReopenDbAndOneWindow_Bkupd(sbstrServer, sbstrDatabase));
			}
		}
		pbkupd->DecExportedObjects_Bkupd();
	}
	else if (fWsChg)
	{
		// If the database was renamed, only this app could be running, so no sync is needed.
		SyncInfo sync(ksyncWs, 0, 0);
		m_qlpi->StoreAndSync(sync);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Find all the files that might be moved (or copied) when the external links root directory
	changes.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::CollectMovableFiles(Vector<MovableFile> & vmovFiles,
	const StrUni & stuOldExtLinkRoot, const StrUni & stuNewExtLinkRoot)
{
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	StrUni stuSql;
	Vector<int> vhvoFolders;
	stuSql.Format(L"SELECT Id FROM CmFolder_ WHERE Owner$=%<0>d AND OwnFlid$ IN (%<1>d,%<2>d)",
		m_qlpi->GetLpId(), kflidLangProject_Pictures, kflidLangProject_Media);
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	pdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	int hvoFolder;
	while (fMoreRows)
	{
		qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoFolder), sizeof(int), &cbSpaceTaken,
			&fIsNull, 0);
		if (!fIsNull)
			vhvoFolders.Push(hvoFolder);
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	qodc.Clear();
	for (int i = 0; i < vhvoFolders.Size(); ++i)
	{
		CollectMovableFilesFromFolder(qode, vhvoFolders[i], vmovFiles,
			stuOldExtLinkRoot, stuNewExtLinkRoot);
	}
	qode.Clear();
}


/*----------------------------------------------------------------------------------------------
	Find all the files in the given CmFolder that might be moved (or copied) when the external
	links root directory changes.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::CollectMovableFilesFromFolder(IOleDbEncap * pode, int hvoFolder,
	Vector<MovableFile> & vmovFiles, const StrUni & stuOldExtLinkRoot,
	const StrUni & stuNewExtLinkRoot)
{
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	int cchPath;
	StrUni stuSql;
	stuSql.Format(
		L"SELECT Id, InternalPath FROM CmFile_ WHERE Owner$ = %<0>d AND OwnFlid$ = %<1>d",
		hvoFolder, kflidCmFolder_Files);
	CheckHr(pode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	int hvoFile;
	OLECHAR rgchPath[4000];
	MovableFile mov;
	while (fMoreRows)
	{
		qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoFile), sizeof(int), &cbSpaceTaken,
			&fIsNull, 0);
		if (!fIsNull)
		{
			mov.hvoCmFile = hvoFile;
			qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgchPath), 4000 * isizeof(OLECHAR),
				&cbSpaceTaken, &fIsNull, isizeof(OLECHAR));
			if (!fIsNull && cbSpaceTaken)
			{
				cchPath = (cbSpaceTaken / isizeof(OLECHAR)) - 1;
				mov.stuInternalPath.Assign(rgchPath, cchPath);
				if (!SilUtil::IsPathRooted(mov.stuInternalPath.Chars()))
				{
					if (SilUtil::FileExists(SilUtil::PathCombine(stuOldExtLinkRoot.Chars(),
												mov.stuInternalPath.Chars())) &&
						!SilUtil::FileExists(SilUtil::PathCombine(stuNewExtLinkRoot.Chars(),
												 mov.stuInternalPath.Chars())))
					{
						vmovFiles.Push(mov);
					}
				}

			}
		}
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	stuSql.Format(L"SELECT Id FROM CmFolder_ WHERE Owner$ = %<0>d AND OwnFlid$ = %<1>d",
		hvoFolder, kflidCmFolder_SubFolders);
	Vector<int> vhvoFolders;
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	int hvoSub;
	while (fMoreRows)
	{
		qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoSub), sizeof(int), &cbSpaceTaken,
			&fIsNull, 0);
		if (!fIsNull)
			vhvoFolders.Push(hvoSub);
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	qodc.Clear();
	for (int i = 0; i < vhvoFolders.Size(); ++i)
	{
		// Recurse!
		CollectMovableFilesFromFolder(pode, vhvoFolders[i], vmovFiles,
			stuOldExtLinkRoot, stuNewExtLinkRoot);
	}
}

/*----------------------------------------------------------------------------------------------
	For all the entries in vmovFiles, change the InternalPath value to an absolute path using
	stuRootDir.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::ExpandToFullPath(Vector<MovableFile> & vmovFiles, const StrUni & stuRootDir)
{
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuSql;
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	pdbi->GetDbAccess(&qode);
	for (int i = 0; i < vmovFiles.Size(); ++i)
	{
		CheckHr(qode->CreateCommand(&qodc));
		stuSql.Format(L"UPDATE CmFile SET InternalPath=? WHERE Id=%<0>d",
			vmovFiles[i].hvoCmFile);
		StrUni stuPath(SilUtil::PathCombine(stuRootDir.Chars(),
			vmovFiles[i].stuInternalPath.Chars()));
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)stuPath.Chars(), stuPath.Length() * isizeof(OLECHAR)));
		qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults);
		qodc.Clear();
	}
	qodc.Clear();
}


/*----------------------------------------------------------------------------------------------
	Bring up the Tools Lists dialog, run it, save the results.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdTlsLists(Cmd * pcmd)
{
	AssertObj(pcmd);
	TlsListsDlgPtr qftlstd;
	qftlstd.Create();

	// Use our window title as default header.
	achar rgchName[MAX_PATH];
	::SendMessage(m_hwnd, WM_GETTEXT, MAX_PATH, (LPARAM)rgchName);

	Vector<HVO> & vhvo = m_qlpi->GetPsslIds();

	qftlstd->SetDialogValues(0, m_qlpi->AnalWs(), false, vhvo);
	qftlstd->DoModal(Hwnd());
	return true;
}


/*----------------------------------------------------------------------------------------------
	Bring up the Statistics report dialog, run it, save the results.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdStats(Cmd * pcmd)
{
	Assert(false); // This should be defined in a subclass (i.e. derived class)
	return true;
}

/*----------------------------------------------------------------------------------------------
	Process External Link menu items (Open, Open With, External Link, Remove Link).

	Overrides AfMainWnd's version to supply m_qlpi to the ExternalLink method.

	@param pcmd Ptr to menu command

	@return True if successful, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdExternalLink(Cmd * pcmd)
{
	AssertObj(pcmd);
	return ExternalLink(pcmd, m_qlpi);
}


/*----------------------------------------------------------------------------------------------
	Merge all the overlays in the given set together and use it as the active overlay.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::MergeOverlays(Set<int> & sisel)
{
	// Merge all the selected overlays.
	IVwOverlayPtr qvo;
	IVwOverlayPtr qvoFinal;
	Set<int>::iterator it = sisel.Begin();
	Set<int>::iterator itStop = sisel.End();
	if (*it != 0)
	{
		while (it != itStop)
		{
			// We subtract 1 because the first item is 'No Overlay'.
			if (!m_qlpi->GetOverlay(*it - 1, &qvo))
				ThrowHr(WarnHr(E_FAIL));
			if (!qvoFinal)
			{
				qvoFinal = qvo;
			}
			else
			{
				IVwOverlayPtr qvoMerged;
				CheckHr(qvoFinal->Merge(qvo, &qvoMerged));
				qvoFinal = qvoMerged;
			}
			++it;
		}
	}
	SetCurrentOverlay(qvoFinal);
}


/*----------------------------------------------------------------------------------------------
	Set a path/view to be used for locating the target object when first starting up the
	window. For example, if subentry 1582 is to be displayed, the following input might be
	used:
	prghvo: 1579, 1581, 1582,  chvo: 3
	prgflid: 4004009, 4004009,  cflid: 2
	1579 is the main record. 1581 is a subentry in the 4004009 flid of 1579. 1582 is a
	subentry in the 4004009 flid of 1581. When the window opens, we try to display the
	cursor in the 1582 record. If a third flid is supplied, we try to position the cursor
	in that field.

	Implementations that use this feature will normally need to customize
	RecMainWnd::LoadSettings that gets called in the main window during PostAttach to set
	the desired record instead of the one saved in the registry. DispCurRec() (or something
	it calls) in the various views also needs to be customized to allow moving down inside
	of the object being displayed.

	@param prghvo Pointer to an array of object ids.
	@param chvo Number of object ids in prghvo.
	@param prgflid Pointer to an array of flids.
	@param cflid Number of flids in prgflid.
	@param ichCur The cursor offset in the selected field.
	@param nView The view to display when showing the first object. -1 is used to choose
		the first data entry view.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::SetStartupInfo(const HVO * prghvo, int chvo, const int * prgflid, int cflid,
	int ichCur, int nView)
{
	AssertArray(prghvo, chvo);
	AssertArray(prgflid, cflid);
	m_vhvoPath.Clear(); // Clear old values when we reuse this in promote, etc.
	m_vflidPath.Clear();
	for (int ihvo = 0; ihvo < chvo; ++ihvo)
		m_vhvoPath.Push(prghvo[ihvo]);
	for (int iflid = 0; iflid < cflid; ++iflid)
		m_vflidPath.Push(prgflid[iflid]);
	m_ichCur = ichCur;
	m_nView = nView;
}

/*----------------------------------------------------------------------------------------------
	Create an 'undoable' object.

	@param hvoOwner Id of the owner of the new object.
	@param flidOwn flid where the new object will be placed.
	@param flidOwnerModified flid on owner for modified date, or '0' for none.
	@param clsid Class id of object to be created.
	@param kstid Id of the string resource to be used for 'Undo/Redo' message.
	@param chvo Count of items in flidOwn, or -1 if a collection.

	@return Id of new object, or 0 if not created.
----------------------------------------------------------------------------------------------*/
HVO RecMainWnd::CreateUndoableObject(HVO hvoOwner, int flidOwn, int flidOwnerModified,
									int clsid, int kstid, int ihvo)
{
	Assert(m_qcvd);
	HVO hvoNew;
	try
	{
		// Have database create a new record and set hvoNew to the new ID.
		StartUndoableTask(hvoOwner, flidOwnerModified, kstid);

		// Call core method, which may be overidden by subclasses to do more than
		// what is specified in this class.
		hvoNew = CreateUndoableObjectCore(hvoOwner, flidOwn, clsid, ihvo);

		CheckHr(m_qcvd->EndUndoTask());
		CheckHr(m_qcvd->PropChanged(NULL, kpctNotifyAll, hvoOwner, flidOwn, ihvo, 1, 0));
	}
	catch (...)
	{
		return 0;
	}
	return hvoNew;
}

/*----------------------------------------------------------------------------------------------
	Create an object, and set class and owner in cache. This method should be overridden,
	if more than the basics of creation, and adding owner and class information is needed.
	(cf. RnMainWnd::CreateUndoableObjectCore override, which also adds create and modified
	times, as well as set other stuff.)

	@param hvoOwner Id of the owner of the new object.
	@param flidOwn flid where the new object will be placed.
	@param clsid Class id of object to be created.

	@return Id of new object, or 0 if not created.
----------------------------------------------------------------------------------------------*/
HVO RecMainWnd::CreateUndoableObjectCore(HVO hvoOwner, int flidOwn, int clsid, int ihvo)
{
	Assert(m_qcvd);
	HVO hvoNew;
	try
	{
		CheckHr(m_qcvd->MakeNewObject(clsid, hvoOwner, flidOwn, ihvo, &hvoNew));
		m_qcvd->CacheIntProp(hvoNew, kflidCmObject_Class, clsid);
		m_qcvd->CacheObjProp(hvoNew, kflidCmObject_Owner, hvoOwner);
	}
	catch (...)
	{
		return 0;
	}
	return hvoNew;
}

/*----------------------------------------------------------------------------------------------
	Delete an 'undoable' object.

	@param hvoOwner Id of the owner of the new object.
	@param flidOwn flid where the new object will be placed.
	@param flidOwnerModified flid on owner for modified date, or '0' for none.
	@param clsid Class id of object to be created.
	@param kstid Id of the string resource to be used for 'Undo/Redo' message.
	@param chvo Count of items in flidOwn, or -1 if a collection.

	@return Id of new object, or 0 if not created.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::DeleteUndoableObject(HVO hvoOwner, int flidOwn, int flidOwnerModified,
	HVO hvoObj, int kstid, int ihvo, bool fHasRefs)
{
	Assert(m_qcvd);
	try
	{
		StartUndoableTask(hvoOwner, flidOwnerModified, kstid);
		// Call core method, which may be overidden by subclasses to do more than
		// what is specified in this class.
		DeleteUndoableObjectCore(hvoOwner, flidOwn, hvoObj, ihvo);
		// RemoveObjRefs has been subsumed by DeleteObjOwner.
//		if (fHasRefs)
//			CheckHr(m_qcvd->RemoveObjRefs(hvoObj));

		CheckHr(m_qcvd->EndUndoTask());
	}
	catch (...)
	{
		return;
	}
	return;
}

/*----------------------------------------------------------------------------------------------
	Delete an object.

	@param hvoOwner Id of the owner of the new object.
	@param flidOwn flid where the new object will be placed.
	@param hvoObj Id of object to be deleted.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::DeleteUndoableObjectCore(HVO hvoOwner, int flidOwn, HVO hvoObj, int ihvo)
{
	Assert(m_qcvd);

	// Delete the object, remove it from the vector, and notify that the prop changed.
	CheckHr(m_qcvd->DeleteObjOwner(hvoOwner, hvoObj, flidOwn, ihvo));
	CheckHr(m_qcvd->PropChanged(NULL, kpctNotifyAll, hvoOwner, flidOwn, ihvo, 0, 1));
	return;
}


/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::OnReleasePtr()
{
	// By contract we must clear all our own smart pointers.
	m_qcvd.Clear();
	// To prevent spurious memory leaks shut down the writing system factory, releasing
	// cached encodings, old writing systems, etc.
	// (But only if we are the last window.)
	if (AfApp::Papp()->GetMainWndCount() == 0) // last one already removed
	{
		ILgWritingSystemFactoryBuilderPtr qwsfb;
		qwsfb.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
		AssertPtr(qwsfb);
		CheckHr(qwsfb->ShutdownAllFactories());
	}
	SuperClass::OnReleasePtr();

	m_qlpi.Clear();
	m_qmdic.Clear();
	m_qvwbrs.Clear();

	AfApp::Papp()->RemoveCmdHandler(this, 1);
}

/*----------------------------------------------------------------------------------------------
	Crawl through the database and rename the given styles.  This functionality is now part of
	AfStylesDlg.

	Note that this method is still needed as a counterpart to WpMainWnd::RenameAndDeleteStyles()
	since AfMainWnd::RenameAndDeleteStyles() is a pure virtual method.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::RenameAndDeleteStyles(Vector<StrUni> & vstuOldNames,
	Vector<StrUni> & vstuNewNames, Vector<StrUni> & vstuDelNames)
{
	::MessageBoxA(m_hwnd,
		"RecMainWnd::RenameAndDeleteStyles() should never be called.\r\n"
		"Please notify the FieldWorks Development Team!\r\n"
		"Your styles will not be renamed or deleted despite your correct efforts.",
		"BUG!!!!!!!!!!!!!!!!!", MB_OK);
}


/*----------------------------------------------------------------------------------------------
	Update the writing system combobox list on the formatting toolbar if it is visible.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::UpdateToolBarWrtSysControl()
{
	AfToolBar * ptlbr = GetToolBar(kridTBarFmtg);
	if (ptlbr && ::IsWindowVisible(ptlbr->Hwnd()))
	{
		HWND hwnd = ::GetDlgItem(ptlbr->Hwnd(), kcidFmttbWrtgSys);
		if (hwnd)
		{
			AfToolBarCombo * ptbc = dynamic_cast<AfToolBarCombo *>(AfWnd::GetAfWnd(hwnd));
			if (ptbc)
			{
				ptbc->ResetContent();
				FillWrtSysButton(ptbc);
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	If the modification date on the main object needs to be updated, it does that, then it
	saves the data, ending the current transaction.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::SaveData()
{
	// If a record has its modified date updated, update the date on the major object.
	// We don't want to set CmMajorObject_DateModified until just prior to saving, because
	// this would lock the entire major object for other users.
	if (m_fIsDateDirty)
	{
		// Set CmMajorObject_DateModified to the current time. (This also updates
		// CmObject_UpdDttm).
		IOleDbEncapPtr qode;
		IOleDbCommandPtr qodc;
		StrUni stu;
		stu.Format(L"update CmMajorObject set DateModified = getdate() where id = %d "
			L"update CmProject set DateModified = getdate() where id = %d",
			GetRootObj(), m_qlpi->GetLpId());
		m_qlpi->GetDbInfo()->GetDbAccess(&qode);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtNoResults));
		m_fIsDateDirty = false;
	}
	// Save cached data and end the transaction.
	CheckHr(m_qcvd->Save());
}


/*----------------------------------------------------------------------------------------------
	This compares CmObject_UpdDttm with pszTable_DateModified and if it is greater, it updates
	both to the current time. It also sets the member variable RecMainWnd::m_fIsDateDirty when
	the dates are updated.
	@param hvo Id of the object that has a DateModified field.
	@param flid The field Id of the DateModified field.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::UpdateDateModified(HVO hvo, int flid)
{
	if (!hvo || !flid)
		return;
//StrApp str;
//str.Format("Update HVO %d, flid %d\n", hvo, flid);
//OutputDebugString(str.Chars());
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stu;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	DBTIMESTAMP tim;
	SmartBstr sbstr;
	IFwMetaDataCachePtr qmdc;

	m_qlpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	CheckHr(qmdc->GetOwnClsName(flid, &sbstr));
	m_qlpi->GetDbInfo()->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));
	// This gets tricky because of differences in precision between UpdDttm and DateModified.
	// UpdDttm will automatically get set to the current time as soon as we update DateModified.
	// But setting DateModified to getdate() directly gives too great a precision, resulting
	// in repeated calls to this method resetting the date.
	stu.Format(
		L" declare @dttm smalldatetime, @dmod datetime, @id int"
		L" set @id = ?"
		L" select @dttm = UpdDttm from CmObject where id = @id"
		L" select @dmod = DateModified from [%s] where id = @id"
		L" if @dttm > cast(@dmod as smalldatetime) begin"
		L"   update [%s] set DateModified = cast(getdate() as smalldatetime) where id = @id"
		L"   select cast(@dttm as datetime)"
		L" end"
		L" else begin"
		L"   select NULL"
		L" end", sbstr.Chars(), sbstr.Chars());
	CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		reinterpret_cast<ULONG *>(&hvo), sizeof(HVO)));
// This didn't work, for some reason.
//	CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
//		reinterpret_cast<ULONG *>((ULONG *)pszTable), wcslen(pszTable) * sizeof(OLECHAR)));
//	CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
//		reinterpret_cast<ULONG *>((ULONG *)pszTable), wcslen(pszTable) * sizeof(OLECHAR)));
	HRESULT hr;
	IgnoreHr(hr = qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
	// The most likely reason we can't update it is that editing took place on another
	// computer. At this point we just ignore the error, but of course don't use
	// the rowset. Also we do NOT modify the flag indicating the major object is modified
	// (it was probably modified on another system, which will do its own update),
	// nor try to update the modify time property (we didn't get the tim info), nor do
	// we update the timestamp (it probably really IS out of date, should not allow the
	// user to modify this object).
	if (FAILED(hr))
		return;
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	Assert(fMoreRows);
	CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&tim), sizeof(DBTIMESTAMP),
		&cbSpaceTaken, &fIsNull, 0));
	if (fIsNull)
		return; // The date wasn't changed.
	// The date was changed.
	m_fIsDateDirty = true; // Set flag to modify the time on the major object.

	// We need to update the cache, if there was already something there.
	// Convert the time to SilTime.
	SilTimeInfo sti;
	sti.year = tim.year;
	sti.ymon = tim.month;
	sti.mday = tim.day;
	sti.hour = tim.hour;
	sti.min = tim.minute;
	sti.sec = tim.second;
	sti.msec = tim.fraction/1000000;
	SilTime stim(sti);
	// If we have the time cached, update the cache with the new value.
	ComBool fInCache;
	CheckHr(m_qcvd->get_IsPropInCache(hvo, flid, kcptTime, 0, &fInCache));
	if (!fInCache)
		return;
	m_qcvd->CacheTimeProp(hvo, flid, stim);
	m_qcvd->PropChanged(NULL, kpctNotifyAll, hvo, flid, 0, 1, 1);
	// Make sure we have the correct time stamp for the current object, since it just
	// changed with our query above. This is significant when we have 2 or more windows open
	// on the same record since moving to another record in one window can cause problems with
	// further edits in the other window.
	CheckHr(m_qcvd->CacheCurrTimeStamp(hvo));
}


/*----------------------------------------------------------------------------------------------
	Retrieves Page Setup information from the database.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::LoadPageSetup()
{
	const int kcBuff = 400; // number of characters (bytes) in the text (fmt) column

	ULONG cbFooter;
	ULONG cbHeader;
	ULONG cchwFooter;
	ULONG cchwHeader;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG luSpaceTaken;
	OLECHAR prgchwFooter[kcBuff];
	OLECHAR prgchwHeader[kcBuff];
	byte rgbFooterFmt[kcBuff];
	byte rgbHeaderFmt[kcBuff];
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	ITsStrFactoryPtr qtsf;
	StrUni stuSql;

	try
	{
		//  Obtain pointer to TsString class factory interface.
		qtsf.CreateInstance(CLSID_TsStrFactory);
		int ws = m_qlpi->AnalWs();

		// Obtain a pointer to the IOleDbEncap object.
		m_qlpi->GetDbInfo()->GetDbAccess(&qode);
		// Form SQL command.
		stuSql.Format(L"select MarginLeft, MarginRight, MarginTop, MarginBottom, "
			L" MarginHeader, MarginFooter, PaperSize, PaperWidth, PaperHeight, Orientation, "
			L" Header, Header_Fmt, Footer, Footer_Fmt, PrintFirstHeader from PageSetup$ "
			L" where Id = %d", GetRootObj());
		qode->CreateCommand(&qodc);
		qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset);
		qodc->GetRowset(1);
		qodc->NextRow(&fMoreRows);
		if (fMoreRows)
		{
			luSpaceTaken = 0;
			qodc->GetColValue(1, reinterpret_cast <BYTE *>(&m_dxmpLeftMargin), sizeof(int),
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != sizeof(int)) || fIsNull)
			{
				m_dxmpLeftMargin = FilPgSetDlg::kDefnLMarg;
			}
			qodc->GetColValue(2, reinterpret_cast <BYTE *>(&m_dxmpRightMargin), sizeof(int),
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != sizeof(int)) || fIsNull)
			{
				m_dxmpRightMargin = FilPgSetDlg::kDefnRMarg;
			}
			qodc->GetColValue(3, reinterpret_cast <BYTE *>(&m_dympTopMargin), sizeof(int),
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != sizeof(int)) || fIsNull)
			{
				m_dympTopMargin = FilPgSetDlg::kDefnTMarg;
			}
			qodc->GetColValue(4, reinterpret_cast <BYTE *>(&m_dympBottomMargin), sizeof(int),
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != sizeof(int)) || fIsNull)
			{
				m_dympBottomMargin = FilPgSetDlg::kDefnBMarg;
			}
			qodc->GetColValue(5, reinterpret_cast <BYTE *>(&m_dympHeaderMargin), sizeof(int),
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != sizeof(int)) || fIsNull)
			{
				m_dympHeaderMargin = FilPgSetDlg::kDefnHEdge;
			}
			qodc->GetColValue(6, reinterpret_cast <BYTE *>(&m_dympFooterMargin), sizeof(int),
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != sizeof(int)) || fIsNull)
			{
				m_dympFooterMargin = FilPgSetDlg::kDefnFEdge;
			}
			qodc->GetColValue(7, reinterpret_cast <BYTE *>(&m_sPgSize), sizeof(int),
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != sizeof(int)) || fIsNull)
			{
				m_sPgSize = kSzLtr;
			}
			qodc->GetColValue(8, reinterpret_cast <BYTE *>(&m_dxmpPageWidth), sizeof(int),
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != sizeof(int)) || fIsNull)
			{
				m_dxmpPageWidth = FilPgSetDlg::kPgWLtr;
			}
			qodc->GetColValue(9, reinterpret_cast <BYTE *>(&m_dympPageHeight), sizeof(int),
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != sizeof(int)) || fIsNull)
			{
				m_dympPageHeight = FilPgSetDlg::kPgHLtr;
			}
			qodc->GetColValue(10, reinterpret_cast <BYTE *>(&m_nOrient), sizeof(int),
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != sizeof(int)) || fIsNull)
			{
				m_nOrient = kPort;
			}
			qodc->GetColValue(11, reinterpret_cast <BYTE *>(prgchwHeader),
				kcBuff * isizeof(OLECHAR), &cchwHeader, &fIsNull, 2);
			if (cchwHeader)
			{
				cchwHeader -= 2;
				cchwHeader /= isizeof(OLECHAR);	// Byte count to character count.
			}
			qodc->GetColValue(12, reinterpret_cast <BYTE *>(rgbHeaderFmt),
				kcBuff * isizeof(byte), &cbHeader, &fIsNull, 0);
			qodc->GetColValue(13, reinterpret_cast <BYTE *>(prgchwFooter),
				kcBuff * isizeof(OLECHAR), &cchwFooter, &fIsNull, 2);
			if (cchwFooter)
			{
				cchwFooter -= 2;
				cchwFooter /= isizeof(OLECHAR);	// Byte count to character count.
			}
			qodc->GetColValue(14, reinterpret_cast <BYTE *>(rgbFooterFmt),
				kcBuff * isizeof(byte), &cbFooter, &fIsNull, 0);
			qodc->GetColValue(15, reinterpret_cast <BYTE *>(&m_fHeaderOnFirstPage), 2,
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != 2) || fIsNull)
			{
				m_fHeaderOnFirstPage = false;
			}

			// Create a TsString for the Header.
			if (cchwHeader == 0)
			{
				// No characters.  Handled below.
			}
			else if (cbHeader == 0)
			{
				// Characters but no formatting.
				qtsf->MakeStringRgch(prgchwHeader, cchwHeader, ws, &m_qtssHeader);
			}
			else
			{
				// Characters and formatting.
				int cchw = cchwHeader;
				int cb = cbHeader;
				qtsf->DeserializeStringRgch(prgchwHeader, &cchw, rgbHeaderFmt, &cb,
					&m_qtssHeader);
			}

			// Create a TsString for the Footer.
			if (cchwFooter == 0)
			{
				// Do not default the footer here
//				// No characters.
//				StrUni stuFoot(kstidFilPgSetDefaultFooter);
//				qtsf->MakeStringRgch(stuFoot.Chars(), stuFoot.Length(), ws, &m_qtssFooter);
			}
			else if (cbFooter == 0)
			{
				// Characters but no formatting.
				qtsf->MakeStringRgch(prgchwFooter, cchwFooter, ws, &m_qtssFooter);
			}
			else
			{
				// Characters and formatting.
				int cchw = cchwFooter;
				int cb = cbFooter;
				qtsf->DeserializeStringRgch(prgchwFooter, &cchw, rgbFooterFmt, &cb,
					&m_qtssFooter);
			}
			m_stuHeaderDefault.Assign(GetRootObjName());
		}
		else
		{
			// Create default values since none have been stored.
			m_dxmpLeftMargin = FilPgSetDlg::kDefnLMarg;
			m_dxmpRightMargin = FilPgSetDlg::kDefnRMarg;
			m_dympTopMargin = FilPgSetDlg::kDefnTMarg;
			m_dympBottomMargin = FilPgSetDlg::kDefnBMarg;
			m_dympHeaderMargin = FilPgSetDlg::kDefnHEdge;
			m_dympFooterMargin = FilPgSetDlg::kDefnFEdge;
			m_sPgSize = kSzLtr;
			m_dympPageHeight = FilPgSetDlg::kPgHLtr;
			m_dxmpPageWidth = FilPgSetDlg::kPgWLtr;
			m_nOrient = kPort;
			m_fHeaderOnFirstPage = false;

////////////////////////////////////////////////////////////////////////////////
// old code ....
/*
			cchwHeader = 0;
			StrUni stuFoot(kstidFilPgSetDefaultFooter);
			qtsf->MakeStringRgch(stuFoot.Chars(), stuFoot.Length(), ws, &m_qtssFooter);

			StrUni stuHeaderFmt(kstidFilPgSetDefaultHeaderFmt);
			StrUni stuApp(AfApp::Papp()->GetAppNameId());
			m_stuHeaderDefault.Format(stuHeaderFmt.Chars(), m_qlpi->PrjName(), stuApp.Chars());
			qtsf->MakeStringRgch(m_stuHeaderDefault.Chars(), m_stuHeaderDefault.Length(),
				ws, &m_qtssHeader);
*/
// ... old code
////////////////////////////////////////////////////////////////////////////////
			m_stuHeaderDefault.Assign(GetRootObjName());
			qtsf->MakeStringRgch(m_stuHeaderDefault.Chars(), m_stuHeaderDefault.Length(), ws,
				&m_qtssHeader);
		}
	}
	catch (...)	// Was empty.
	{
		throw;	// For now we have nothing to add, so pass it on up.
	}
}


/*----------------------------------------------------------------------------------------------
	Saves the Page Setup information to the database.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::SavePageSetup()
{
	const int kcbFmtBufMax = 1024;
	int cbFmtBufSize = kcbFmtBufMax;
	int cbFmtFooterSpaceTaken;
	int cbFmtHeaderSpaceTaken;
	HRESULT hr;
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	// ENHANCE PaulP: Change these to vectors instead of arrays.
	byte * rgbFooterFmt = NewObj byte[kcbFmtBufMax];
	byte * rgbHeaderFmt = NewObj byte[kcbFmtBufMax];
	SmartBstr sbstrFooterText;
	SmartBstr sbstrHeaderText;
	StrUni stuSql;
	// Obtain a pointer to the IOleDbEncap object.
	m_qlpi->GetDbInfo()->GetDbAccess(&qode);

	try
	{
		hr = qode->BeginTrans();
		// Obtain a SmartBstr containing the text of the header and footer TsStrings.
		// First, make sure that the header and footer strings are normalized.
		// This may be paranoid overkill.
		ITsStringPtr qtss;
		ComBool fEqual;
		CheckHr(m_qtssHeader->get_NormalizedForm(knmNFD, &qtss));
		CheckHr(m_qtssHeader->Equals(qtss, &fEqual));
		if (!fEqual)
			m_qtssHeader = qtss;
		CheckHr(m_qtssFooter->get_NormalizedForm(knmNFD, &qtss));
		CheckHr(m_qtssFooter->Equals(qtss, &fEqual));
		if (!fEqual)
			m_qtssFooter = qtss;
		m_qtssHeader->get_Text(&sbstrHeaderText);
		m_qtssFooter->get_Text(&sbstrFooterText);

		// Copy the format information of the header TsString to a byte array.
		hr = m_qtssHeader->SerializeFmtRgb(rgbHeaderFmt, cbFmtBufSize, &cbFmtHeaderSpaceTaken);
		if (hr != S_OK)
		{
			if (hr == S_FALSE)
			{
				//  If the supplied buffer is too small, try it again with
				//  the value that cbFmtSpaceTaken was set to.  If this
				//  fails, throw error.
				delete[] rgbHeaderFmt;
				rgbHeaderFmt = NewObj byte[cbFmtHeaderSpaceTaken];
				cbFmtBufSize = cbFmtHeaderSpaceTaken;
				CheckHr(m_qtssHeader->SerializeFmtRgb(rgbHeaderFmt, cbFmtBufSize,
					&cbFmtHeaderSpaceTaken));
			}
			else
			{
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
		}

		// Copy the format information of the footer TsString to a byte array.
		hr = m_qtssFooter->SerializeFmtRgb(rgbFooterFmt, cbFmtBufSize, &cbFmtFooterSpaceTaken);
		if (hr != S_OK)
		{
			if (hr == S_FALSE)
			{
				//  If the supplied buffer is too small, try it again with
				//  the value that cbFmtSpaceTaken was set to.  If this
				//  fails, throw error.
				delete[] rgbFooterFmt;
				rgbFooterFmt = NewObj byte[cbFmtFooterSpaceTaken];
				cbFmtBufSize = cbFmtFooterSpaceTaken;
				CheckHr(m_qtssFooter->SerializeFmtRgb(rgbFooterFmt, cbFmtBufSize,
					&cbFmtFooterSpaceTaken));
			}
			else
			{
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
		}

		// Form the SQL query.
		hr = qode->CreateCommand(&qodc);
		stuSql.Format(L"delete PageSetup$ where id=%d", GetRootObj());
		qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults);

		stuSql.Format(L"insert into PageSetup$ (Id, MarginLeft, MarginRight, MarginTop, "
			L" MarginBottom, MarginHeader, MarginFooter, PaperSize, PaperWidth, PaperHeight, "
			L" Orientation, Header, Header_Fmt, Footer, Footer_Fmt, PrintFirstHeader) "
			L" values (%d, %d, %d, %d, %d, %d, %d, %d, %d, %d, %d, ?, ?, ?, ?, %d) ",
			GetRootObj(), m_dxmpLeftMargin, m_dxmpRightMargin, m_dympTopMargin,
			m_dympBottomMargin, m_dympHeaderMargin, m_dympFooterMargin, m_sPgSize,
			m_dxmpPageWidth, m_dympPageHeight, m_nOrient, m_fHeaderOnFirstPage);

		// Set SQL statement parameters.
		qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)sbstrHeaderText.Chars(), sbstrHeaderText.Length() * 2);
		if (cbFmtHeaderSpaceTaken)
		{
			qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(rgbHeaderFmt), cbFmtHeaderSpaceTaken);
		}
		else
		{
			qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(NULL), 0);
		}
		qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)sbstrFooterText.Chars(), sbstrFooterText.Length() * 2);
		if (cbFmtFooterSpaceTaken)
		{
			qodc->SetParameter(4, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(rgbFooterFmt), cbFmtFooterSpaceTaken);
		}
		else
		{
			qodc->SetParameter(4, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(NULL), 0);
		}

		// Execute the command.
		qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults);
		qodc.Clear();
		CheckHr(qode->CommitTrans());
	}
	catch (...)	// Was empty.
	{
		qode->RollbackTrans();
		throw;	// For now we have nothing to add, so pass it on up.
	}
}


#if 0
/*----------------------------------------------------------------------------------------------
	If the given overlay is currently showing, remerge all open overlays.
----------------------------------------------------------------------------------------------*
void RecMainWnd::OnChangeOverlay(int iovr)
{
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltOverlay], sisel);
	int isel = iovr + 1; // We add 1 because the first item is 'No Overlay'.
	if (sisel.IsMember(isel))
		MergeOverlays(sisel);
}
*/
#endif


/*----------------------------------------------------------------------------------------------
	Bring up another top-level window with the same view. The new window is a complete duplicate,
	meaning that it displays the same view, scrolled to the same position, with the text cursor
	at the same position, etc. Any active sort orders, filters, an overlays are likewise active
	in the new window.

	@param pcmd menu command

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdWndNew(Cmd * pcmd)
{
	// Set the current registry settings (toolbars, view bar, etc.)
	SaveSettings(NULL);

	// Retrieve the current scroll, split status
	AfClientWndPtr qafcw = m_qmdic->GetCurChild();
	AssertPtr(qafcw);
	WndSettings wndSet;
	qafcw->GetVwSpInfo(&wndSet);

	PrepareNewWindowLocation();

	// Request the appropriate frame window subclass to give us a new window
	RecMainWndPtr qrmwNew;
	NewWindow(&qrmwNew);
	AssertPtr(qrmwNew);

	// Set up the new window's status bar
	AfStatusBarPtr qstbr = qrmwNew->GetStatusBarWnd();
	Assert(qstbr);
	qstbr->InitializePanes();
	qrmwNew->UpdateStatusBar();

	// Set the new window's scroll, split status
	AfMdiClientWndPtr qmdic = qrmwNew->GetMdiClientWnd();
	AssertPtr(qmdic);
	qafcw = qmdic->GetCurChild();
	AssertPtr(qafcw);
	qafcw->RestoreVwInfo(wndSet);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Add the root box to the vector of root boxes. Also set the overlay for the new root box to
	the current overlay.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::RegisterRootBox(IVwRootBox * prootb)
{
	AssertPtr(prootb);

	CheckHr(prootb->putref_Overlay(m_qvo));
	SuperClass::RegisterRootBox(prootb);
}


/*----------------------------------------------------------------------------------------------
	Step through the register root boxes and inform them of the overlay change.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::SetCurrentOverlay(IVwOverlay * pvo)
{
	AssertPtrN(pvo);

	m_qvo = pvo;
	SuperClass::SetCurrentOverlay(pvo);
}


/*----------------------------------------------------------------------------------------------
	Initialize special buttons on the toolbars.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::OnToolBarButtonAdded(AfToolBar * ptlbr, int ibtn, int cid)
{
	AssertPtr(ptlbr);

	switch (cid)
	{
	case kcidFmttbStyle:
		{
			//------------------------------------------------------------------------------
			// The style control.
			AfToolBarComboPtr qtbc;
			ptlbr->SetupComboControl(&qtbc, cid, 100, 210, true);
			int width = 190;
			::SendMessage(qtbc->GetComboControl(), CB_SETDROPPEDWIDTH , (WPARAM)width, 0);
			OnStyleDropDown(qtbc->Hwnd());
		}
		return;

	case kcidFmttbWrtgSys:
		{
			//------------------------------------------------------------------------------
			// The old writing system control.

			AfToolBarComboPtr qtbc;
			ptlbr->SetupComboControl(&qtbc, cid, 80, 200, true);
			int width = 100;
			::SendMessage(qtbc->GetComboControl(), CB_SETDROPPEDWIDTH , (WPARAM)width, 0);

			FillWrtSysButton(qtbc);
		}
		return;

	case kcidFmttbFnt:
		{
			//------------------------------------------------------------------------------
			// The font family control.
			AfToolBarComboPtr qtbc;
			ptlbr->SetupComboControl(&qtbc, cid, 120, 200, true);
			int width = 190;
			::SendMessage(qtbc->GetComboControl(), CB_SETDROPPEDWIDTH , (WPARAM)width, 0);

			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);

			FW_COMBOBOXEXITEM fcbi;
			memset(&fcbi, 0, isizeof(fcbi));
			fcbi.mask = CBEIF_TEXT;
			fcbi.iIndent = 0;

			// Add the three predefined names to the combo box.
			// Review JohnT: If changing, check carefully they are not stored as
			// literals in the database--if so, don't make localizable.
			Vector<StrUni> vstuFontDefaults;
			FwStyledText::FontUiStrings(false, vstuFontDefaults);
			int ifnt;
			for (ifnt = 0; ifnt < vstuFontDefaults.Size(); ifnt++)
			{
				qtsf->MakeStringRgch(vstuFontDefaults[ifnt], vstuFontDefaults[ifnt].Length(),
					UserWs(), &fcbi.qtss);
				fcbi.iItem = ifnt;
				qtbc->InsertItem(&fcbi);
			}
			// Get the currently available fonts via the LgFontManager.
			ILgFontManagerPtr qfm;
			SmartBstr bstrNames;
			qfm.CreateInstance(CLSID_LgFontManager);
			HRESULT hr = qfm->AvailableFonts(&bstrNames);
			if (SUCCEEDED(hr))
			{
				StrUni stuNameList;
				// Convert BSTR to StrUni.
				stuNameList.Assign(bstrNames.Bstr(), BstrLen(bstrNames.Bstr()));
				int cchLength = stuNameList.Length();
				int ichMin = 0; // Index of the beginning of a font name.
				int ichLim = 0; // Index that is one past the end of a font name.
				// Add each font name to the combo box.
				while (ichLim < cchLength)
				{
					ichLim = stuNameList.FindCh(L',', ichMin);
					if (ichLim == -1) // i.e., if not found.
					{
						ichLim = cchLength;
					}
					qtsf->MakeStringRgch(stuNameList.Chars() + ichMin, ichLim - ichMin,
						UserWs(), &fcbi.qtss);
					fcbi.iItem = ifnt++;
					qtbc->InsertItem(&fcbi);
					ichMin = ichLim + 1;
				}
			}
		}
		return;

	case kcidFmttbFntSize:
		{
			//------------------------------------------------------------------------------
			// The font size control.
			AfToolBarComboPtr qtbc;
			// Allow enough space for all the font sizes to show.
			ptlbr->SetupComboControl(&qtbc, cid, 40, 400, false);

			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);

			FW_COMBOBOXEXITEM fcbi;
			memset(&fcbi, 0, isizeof(fcbi));
			fcbi.mask = CBEIF_TEXT;
			fcbi.iIndent = 0;

			const wchar * krgszSizes[] = {
				L"8", L"9", L"10", L"11", L"12", L"14", L"16", L"18", L"20", L"22", L"24",
				L"26", L"28", L"36", L"48", L"72"
			};
			const int kcSizes = isizeof(krgszSizes) / isizeof(wchar *);
			for (int isiz = 0; isiz < kcSizes; isiz++)
			{
				qtsf->MakeStringRgch(krgszSizes[isiz], StrLen(krgszSizes[isiz]),
					UserWs(), &fcbi.qtss);
				fcbi.iItem = isiz;
				qtbc->InsertItem(&fcbi);
			}
		}
		return;

	case kcidEditSrchQuick:
		{
#if 0 // Not needed until we switch to a combo box -- after it works with View code.
			AfToolBarComboPtr qtbc;
			ptlbr->SetupComboControl(&qtbc, cid, 120, 200, false);
			StrApp str(kstidEditSrchQuick);
			::SetWindowText(qtbc->Hwnd(), str.Chars());
#else
			AfToolBarEditPtr qtbe;
			ptlbr->SetupEditControl(&qtbe, cid, 120);
			// The enabling routine will set it to the current pattern, which should
			// be an empty string initially.
			//StrApp str(kstidEditSrchQuick);
			//::SetWindowText(qtbe->Hwnd(), str.Chars());
#endif
		}
		return;
	}

	SuperClass::OnToolBarButtonAdded(ptlbr, ibtn, cid);
}

/*----------------------------------------------------------------------------------------------
	Fill the combobox containing the list of old writing systems / encodings.

	@param ptbc Pointer to the tool bar child containing the old writing system combobox.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::FillWrtSysButton(AfToolBarCombo * ptbc)
{
	AssertPtr(ptbc);

	FW_COMBOBOXEXITEM fcbi;
	memset(&fcbi, 0, isizeof(fcbi));
	fcbi.mask = CBEIF_TEXT;
	fcbi.iIndent = 0;

	ILgWritingSystemFactoryPtr qwsf;
	m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	IWritingSystemPtr qws;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	Vector<int> & vwsAnal = m_qlpi->AllAnalWss();
	Vector<int> & vwsVern = m_qlpi->AllVernWss();
	int iws;
	int iitem = 0;
	StrUni stu;
	for (iws = 0; iws < vwsAnal.Size(); iws++)
	{
		stu.Clear();
		int ws = vwsAnal[iws];
		CheckHr(qwsf->get_EngineOrNull(ws, &qws));
		AssertPtr(qws);
		SmartBstr sbstr;
		CheckHr(qws->get_UiName(UserWs(), &sbstr));
		stu.Assign(sbstr.Chars(), sbstr.Length());
		Assert(stu.Length());
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), UserWs(), &fcbi.qtss);
		fcbi.iItem = iitem++;
		ptbc->InsertItem(&fcbi);
	}
	for (iws = 0; iws < vwsVern.Size(); iws++)
	{
		int ws = vwsVern[iws];
		// Check whether some already covered by analysis list.
		// Review PM (JohnT): should we sort them alphabetically or some other way?
		// Should we make the first two the primary analysis and vernacular ones?
		bool fFound = false;
		for (int i = 0; i < vwsAnal.Size(); i++)
		{
			if (ws == vwsAnal[i])
			{
				fFound = true;
				break;
			}
		}
		if (fFound)
			continue; // don't insert duplicates.
		CheckHr(qwsf->get_EngineOrNull(ws, &qws));
		AssertPtr(qws);
		SmartBstr sbstr;
		CheckHr(qws->get_UiName(UserWs(), &sbstr));
		stu.Assign(sbstr.Chars(), sbstr.Length());
		Assert(stu.Length());
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), UserWs(), &fcbi.qtss);
		fcbi.iItem = iitem++;
		ptbc->InsertItem(&fcbi);
	}
}


/*----------------------------------------------------------------------------------------------
	This should be called before making any changes to the main window (splitting, switching
	records, closing, etc.) If it returns false, it means something (probably a data entry
	window) can't be closed due to illegal data, so you should abort whatever operation the
	user was trying to do. If there is a problem, this call should produce an error message
	notifying the user of the problem.
	@fChkReq True if we should also check Required/Encouraged data. False otherwise.
	@return True if it is OK to close the DE window.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::IsOkToChange(bool fChkReq)
{
	AfClientWndPtr qacw = m_qmdic->GetCurChild();
	if (!qacw)
		return true;
	return qacw->IsOkToChange(fChkReq);
}

/*----------------------------------------------------------------------------------------------
	Return the path extension that allows suitable help to be found for how the application
	saves data. The DataNotebook one is provided as a default.
----------------------------------------------------------------------------------------------*/
achar * RecMainWnd::HowToSaveHelpString()
{
	return _T("HowTheFieldWorksDataNotebookSa.htm");
}

/*----------------------------------------------------------------------------------------------
	Any data changes that are made during the idle loop should be part of the previous
	undo task.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::OnIdle()
{
	if (m_qlpi)
	{
		IOleDbEncapPtr qode;
		AfDbInfoPtr qdbi = m_qlpi->GetDbInfo();
		if (qdbi)
		{
			qdbi->GetDbAccess(&qode);
			if (qode)
			{
				ComBool f;
				qode->IsTransactionOpen(&f);
#if DEBUG // don't break release build
				if (f)
					Warn("transaction open!");
#endif
			}
		}
	}

	// This causes all typing to become part of the previous Undo task, which is too strong.
	//if (m_qcvd)
	//	m_qcvd->ContinueUndoTask();
	SuperClass::OnIdle();
	if (m_qcvd)
		m_qcvd->EndOuterUndoTask();

	// When we are shutting down a window, m_qlpi will be gone at this point.
	if (m_qlpi && this->Hwnd() == ::GetForegroundWindow())
	{
		bool fCancelOp;
		ProcessExcessiveChgs(kSaveWarnTyping, false, &fCancelOp);
	}
}


/*----------------------------------------------------------------------------------------------
	Check to see if we have excessive changes on the undo stack, and if so, warn the user
	and do a save if they agree to save.
	@param psda An ISilDataAccess pointer if we are in the middle of a BeginUndoTask and
		we need to do an EndUndoTask prior to the save.
	@param pfAlwaysSave if true then save is automatic without asking user.
	@param fCancelOp returns true if we get a log full and user said to Cancel (Not save)
	@return true if the user decided to save, or false otherwise.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::ProcessExcessiveChgs(SaveWarningType nWarningType, bool fAlwaysSave,
			bool * pfCancelOp, ISilDataAccess * psda)
{
	*pfCancelOp = false;
	int nKbFree;
	IOleDbEncapPtr qode;
	AfDbInfoPtr qdbi = m_qlpi->GetDbInfo();
	AssertPtr(qdbi);
	qdbi->GetDbAccess(&qode);
	int nReserveDiskSpace = 5000;
	int nReserveLogSpace = 2000;
	int cAct;
	CheckHr(m_qacth->get_UndoableActionCount(&cAct));

	// Don't warn more often than this however low the safety margin.
	const int kcActMinCheckGap = 100;
	if (cAct - m_cActUndoLastChange < kcActMinCheckGap)
		return false;

	m_cActUndoLastChange = cAct;
	qode->GetFreeLogKb(nReserveDiskSpace, &nKbFree);

	StrApp strCaption(kstidSaveWork);
	StrApp strOldHelp(m_strFullHelpUrl);
	StrApp strHelpUrl(AfApp::Papp()->GetHelpFile());
	strHelpUrl.Append(_T("::/"));
	StrApp strPath2 = HowToSaveHelpString();
	strHelpUrl.Append(strPath2);
	m_strFullHelpUrl = strHelpUrl;
	try
	{
		if (nKbFree < nReserveLogSpace)
		{
			if(nWarningType == kSaveWarnTyping)
			{
				StrApp strMsg(kstidSaveWarnTyping);
				// Launch the dialog. As a safety precaution, since both responses can be dangerous,
				// help is the default.
				if (IDYES == ::MessageBox(this->Hwnd(), strMsg.Chars(),
					strCaption.Chars(), MB_YESNO | MB_ICONWARNING | MB_HELP | MB_DEFBUTTON3))
				{
					// If the user chose 'Yes' do a save.
					if (psda)
						CheckHr(psda->EndUndoTask());
					Cmd cmd; // not used by CmdFileSave, but required argument
					CmdFileSave(&cmd);
					m_strFullHelpUrl = strOldHelp;
					return true;
				}
				else
				{
					*pfCancelOp = true;
					m_strFullHelpUrl = strOldHelp;
					return false;
				}
			}
			else if(nWarningType == kSaveWarnReplace)
			{
				if (!fAlwaysSave)
				{
					StrApp strMsg(kstidSaveWarnReplace);
					// Launch the dialog. As a safety precaution, since both responses can be dangerous,
					// help is the default.
					if (IDOK == ::MessageBox(this->Hwnd(), strMsg.Chars(),
							strCaption.Chars(), MB_OKCANCEL | MB_ICONWARNING | MB_HELP | MB_DEFBUTTON3))
						fAlwaysSave = true;
				}

				if (fAlwaysSave)
				{
					// If the user has chose 'OK' do a save.
					if (psda)
						CheckHr(psda->EndUndoTask());
					Cmd cmd; // not used by CmdFileSave, but required argument
					CmdFileSave(&cmd);
					m_strFullHelpUrl = strOldHelp;
					return true;
				}
				else
				{
					// If the user chose 'Cancel' in the MessageBox.
					*pfCancelOp = true;
					m_strFullHelpUrl = strOldHelp;
					return false;
				}
			}
		}
		m_strFullHelpUrl = strOldHelp;
	}
	catch(...)
	{
		m_strFullHelpUrl = strOldHelp;
		throw;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Return the width of the viewbar plus the width of the client area.
----------------------------------------------------------------------------------------------*/
int RecMainWnd::GetMinWidth()
{
	Rect rcWindow;
	Rect rcClient;
	::GetWindowRect(m_hwnd, &rcWindow);
	::GetClientRect(m_hwnd, &rcClient);
	return kdxpMinClient + m_dxpLeft + kdxpSplitter + rcWindow.Width() - rcClient.Width();
}


/*----------------------------------------------------------------------------------------------
	If fFullWindow is true, temporarily hide the viewbar and toolbars.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::SetWindowMode(bool fFullWindow)
{
	// Store the current state of the viewbar.
	// This has to be set before WM_SETREDRAW is called or it always returns false.
	if (fFullWindow)
		m_fOldViewbarVisible = ::IsWindowVisible(m_qvwbrs->Hwnd());

	::SendMessage(m_hwnd, WM_SETREDRAW, false, 0);
	if (fFullWindow)
		m_qvwbrs->ShowViewBar(false);
	else
		m_qvwbrs->ShowViewBar(m_fOldViewbarVisible);

	SuperClass::SetWindowMode(fFullWindow);
}


/*----------------------------------------------------------------------------------------------
	Load the UserViews, create UserViewSpecs for each one and store in vuvs.
	Load the UserViewRecs, create RecordSpecs and store in appropriate UserViewSpecs.
	Load the UserViewFields, create BlockSpecs/FldSpecs and store in appropriate RecordSpecs.
	Then fill in the names for each RecordSpec and find all Browse UserViews and fill in the
	additional RecordSpecs.

	@return true
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::LoadUserViews()
{
	return m_qlpi->GetDbInfo()->LoadUserViews(AfApp::Papp()->GetAppClsid());
}


/*----------------------------------------------------------------------------------------------
	Called when the frame window is gaining or losing activation.
	@param fActivating true if gaining activation, false if losing activation.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::OnPreActivate(bool fActivating)
{
	SuperClass::OnPreActivate(fActivating);
	// Pass the message on to the current child.
	AfClientWnd * pafcw = m_qmdic->GetCurChild();
	if (!pafcw)
		return; // We do not have a client window on startup.
	pafcw->OnPreActivate(fActivating);
	// When activating the window, ask the application to update any of its windows if
	// changes were made to the database by another application.
	if (fActivating)
	{
		AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
		if (papp)
			papp->DbSynchronize(m_qlpi); // Do anything if it fails?
	}
}


/*----------------------------------------------------------------------------------------------
	If we have a data entry window open, return a pointer to it.
	@return Pointer to De window or NULL if none.
----------------------------------------------------------------------------------------------*/
AfDeSplitChild * RecMainWnd::CurrentDeWnd()
{
	AfClientRecDeWnd * pcrdw = dynamic_cast<AfClientRecDeWnd *>(m_qmdic->GetCurChild());
	if (pcrdw && pcrdw->CanGetPane())
		return dynamic_cast<AfDeSplitChild *>(pcrdw->CurrentPane());
	return NULL;
}


/*----------------------------------------------------------------------------------------------
	Close any active editors open in this window.
	@return true if no errors, or false if an editor couldn't be closed.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CloseActiveEditors()
{
	AfClientRecDeWnd * pcrdw = dynamic_cast<AfClientRecDeWnd *>(m_qmdic->GetCurChild());
	bool fOk;
	if (pcrdw)
	{
		AfDeSplitChild * padsc = dynamic_cast<AfDeSplitChild *>(pcrdw->GetPane(0));
		if (padsc)
		{
			fOk = padsc->CloseEditor();
			if (!fOk)
				return false;
		}
		padsc = dynamic_cast<AfDeSplitChild *>(pcrdw->GetPane(1));
		if (padsc)
		{
			fOk = padsc->CloseEditor();
			if (!fOk)
				return false;
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Close (delete) all field editors in this window.
	@param fForce True if we want to force the editors to close without making any
		validity checks or saving any changes.
	@return true if no errors, or false if an editor couldn't be closed.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CloseAllEditors(bool fForce)
{
	AfClientRecDeWnd * pcrdw = dynamic_cast<AfClientRecDeWnd *>(m_qmdic->GetCurChild());
	if (!fForce && !IsOkToChange())
		return false;
	if (pcrdw)
	{
		AfDeSplitChild * padsc = dynamic_cast<AfDeSplitChild *>(pcrdw->GetPane(0));
		if (padsc)
			padsc->CloseAllEditors(fForce);
		padsc = dynamic_cast<AfDeSplitChild *>(pcrdw->GetPane(1));
		if (padsc)
			padsc->CloseAllEditors(fForce);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Return the writing system factory.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::GetLgWritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	AssertPtr(ppwsf);

	*ppwsf = NULL;
	if (m_qlpi)
	{
		AfDbInfo * pdbi = m_qlpi->GetDbInfo();
		if (pdbi)
			pdbi->GetLgWritingSystemFactory(ppwsf);
	}
}

/*----------------------------------------------------------------------------------------------
	Return the user interface writing system id.
----------------------------------------------------------------------------------------------*/
int RecMainWnd::UserWs()
{
	if (!m_wsUser)
	{
		ILgWritingSystemFactoryPtr qwsf;
		if (m_qlpi)
		{
			AfDbInfo * pdbi = m_qlpi->GetDbInfo();
			if (pdbi)
			{
				pdbi->GetLgWritingSystemFactory(&qwsf);
				if (qwsf)
					CheckHr(qwsf->get_UserWs(&m_wsUser));
			}
		}
	}
	Assert(m_wsUser);

	return m_wsUser;
}

/*----------------------------------------------------------------------------------------------
	Make sure we are not filtered.

	@return True if successful, otherwise false.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::EnsureSafeSort()
{
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltSort], sisel);
	Assert(sisel.Size() == 1);
	if (*sisel.Begin() != 0) // 0 means Default Sort (which should be safe).
	{
		AfSortMethodTurnOffDlgPtr qsto;
		qsto.Create();
		if (qsto->DoModal(m_hwnd) != kctidOk)
			return false;

		int iselDefaultSort = 0;
		sisel.Clear();
		sisel.Insert(iselDefaultSort);
		m_qvwbrs->SetSelection(m_ivblt[kvbltSort], sisel);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Destructor for main window.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::ClearViewBarImageLists()
{
	if (m_rghiml[0])
	{
		AfGdi::ImageList_Destroy(m_rghiml[0]);
		m_rghiml[0] = NULL;
	}
	if (m_rghiml[1])
	{
		AfGdi::ImageList_Destroy(m_rghiml[1]);
		m_rghiml[1] = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_FONTCHANGE:
		{
			ILgFontManagerPtr qfm;
			qfm.CreateInstance(CLSID_LgFontManager);
			qfm->RefreshFontList();
			return true;
		}

	case WM_LBUTTONDOWN:
		{
			// Modified by DarrellZ: Somehow, we were getting a left button down message even
			// when the mouse wasn't in the client area but right at the edge of it. This made
			// it possible to click at the very right of the scrollbar in the current client
			// window and cause this message to be called, which resized the viewbar to fill
			// the entire client area.
			Point pt = MakePoint(lp);
			if (Abs(pt.x - m_dxpLeft) <= kdxpSplitter)
			{
				::SetCapture(m_hwnd);
				m_fResizing = true;
			}
		}
		break;

	case WM_MOUSEMOVE:
		{
			// We only want the wedge cursor at the left side. Without the test, it shows up
			// on all 4 sides of the window border.
			Point pt = MakePoint(lp);
			if (m_fResizing)
			{
				m_dxpLeft = pt.x;
				OnClientSize();
			}
			else if (Abs(pt.x - m_dxpLeft) <= kdxpSplitter)
			{
				::SetCursor(::LoadCursor(NULL, IDC_SIZEWE));
			}
			else
			{
				// This keeps the wedge from staying when the user starts at the upper left
				// corner of the window and moves across the top border.
				::SetCursor(::LoadCursor(NULL, IDC_ARROW));
			}
		}
		break;

	case WM_LBUTTONUP:
		::ReleaseCapture();
		m_fResizing = false;
		::SetCursor(::LoadCursor(NULL, IDC_ARROW));
		break;

	case WM_SETFOCUS:
		if (m_qmdic && m_qmdic->Hwnd())
			::SetFocus(m_qmdic->Hwnd());
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	// Switch to a Data Entry view if we aren't already in one.

	@param kimagDataEntryIn Index to the image.
	@return True if successful, otherwise false.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::SwitchToDataEntryView(int kimagDataEntryIn)
{
	bool bRet = false;

	AfClientWnd * pafcw = m_qmdic->GetCurChild();
	// Don't go any further if we can't close the current window.
	if (!IsOkToChange())
		goto LExit;

	// Switch to a Data Entry view if we aren't already in one.
	if (pafcw->GetImageIndex() != kimagDataEntryIn)
	{
		// If we aren't in a data entry view, find the first DE view.
		int cview = m_qmdic->GetChildCount();
		for (int iview = 0; iview < cview; iview++)
		{
			pafcw = m_qmdic->GetChildFromIndex(iview);
			AssertPtr(pafcw);
			if (pafcw->GetImageIndex() == kimagDataEntryIn)
				break;
		}

		// Make sure we found at least one data entry view.
		bool fFoundIndex = pafcw->GetImageIndex() == kimagDataEntryIn;
		Assert(fFoundIndex);	// Only works for Debug build.
		if (!fFoundIndex)
			goto LExit;

		Set<int> sisel;
		int iitem = m_qmdic->GetChildIndexFromWid(pafcw->GetWindowId());
		sisel.Insert(iitem);
		m_qvwbrs->SetSelection(m_ivblt[kvbltView], sisel);
	}
	bRet = true;

LExit:
	return bRet;
}

/*----------------------------------------------------------------------------------------------
	Make sure we are not filtered.

	@return True if successful, otherwise false.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::EnsureNoFilter()
{
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltFilter], sisel);
	Assert(sisel.Size() == 1);
	if (*sisel.Begin() != 0) // 0 means No Filter.
	{
		FwFilterTurnOffDlgPtr qfto;
		qfto.Create();
		if (qfto->DoModal(m_hwnd) != kctidOk)
			return false;

		int iselNoFilter = 0;
		sisel.Clear();
		sisel.Insert(iselNoFilter);
		m_qvwbrs->SetSelection(m_ivblt[kvbltFilter], sisel);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set clsid to the class of the item or subitem in which the cursor is located.
	Sets nLevel to 0 for item, and 1 for subitem.

	@param pclsid [out] Class ID (or -1 if there are no records)
	@param pnLevel [out] 0 for main item in window, or 1 for subitem in window.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::GetCurClsLevel(int * pclsid, int * pnLevel)
{
	AssertPtr(m_qmdic);

	AssertPtr(pclsid);
	AssertPtr(pnLevel);
	AfClientRecWndPtr qrcw = dynamic_cast<AfClientRecWnd *>(m_qmdic->GetCurChild());
	AssertPtr(qrcw);
	qrcw->GetCurClsLevel(pclsid, pnLevel);
}


/*----------------------------------------------------------------------------------------------
	Called when window is gaining or losing activation.
	If we are being activated, show all the overlays, otherwise hide them all.

	@param fActivating true if gaining activation, false if losing activation.
	@param hwnd handle to the window.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::OnActivate(bool fActivating, HWND hwnd)
{
	SuperClass::OnActivate(fActivating, hwnd);

	// If we are losing activation, save the current changes (if any) to the database.

	// REVIEW JohnT (PaulP):	If another person has edited and saved the same record as is
	// currently  being displayed between the time this user loaded the record and now is about
	// to save the record, the timestamp checking mechanism will bring up a message box to ask
	// the user if the record should be overwritten.
	// If this "Save" call is made here, the current actions done so far will be committed
	// just by bringing that message box up, which is not desireable behavior.
	// Thus, there needs to be a better way to deal with this.
//	if (!fActivating && m_qcvd)
//		SaveData();
	ShowAllOverlays(fActivating);
}


/*----------------------------------------------------------------------------------------------
	Update the status bar display.	Identify the current record.  Fill in the each pane of the
	status bar.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::UpdateStatusBar()
{
	if (!::IsWindowVisible(m_qstbr->Hwnd()))
		return;

	// Identify the current record.
	int ihvo = CurRecIndex();
	if (ihvo == -1)
		return; // No current record.

	int chvo = RecordCount();
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(m_qcvd);

	if (m_vhcFilteredRecords.Size())
	{
		TagFlids tf;
		GetTagFlids(tf);
		Assert(m_vhvoPath.Size() >= m_vflidPath.Size());
		HVO hvoCurRec = m_vhcFilteredRecords[0].hvo;
		for (int iflid = 0; iflid < m_vflidPath.Size(); ++iflid)
		{
			if (m_vflidPath[iflid] == tf.flidSubitems)
				continue;
			hvoCurRec = m_vhvoPath[iflid];
			break;
		}

		// Fill in the first pane of the status bar.
		int64 nTime;
		CheckHr(m_qcvd->get_TimeProp(hvoCurRec, tf.flidCreated, &nTime));
		StrUni stuDate;
		StrUni stuDateFmt(kstidStBarDateFmt);
		if (nTime)
		{
			SilTime tim = nTime;
			// Convert the date to a system date.
			SYSTEMTIME stim;
			stim.wYear = (unsigned short) tim.Year();
			stim.wMonth = (unsigned short) tim.Month();
			stim.wDayOfWeek = (unsigned short) tim.WeekDay();
			stim.wDay = (unsigned short) tim.Date();
			stim.wHour = (unsigned short) tim.Hour();
			stim.wMinute = (unsigned short) tim.Minute();
			stim.wSecond = (unsigned short) tim.Second();
			stim.wMilliseconds = (unsigned short)(tim.MilliSecond());
			achar rgchDate[50];
			::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL, rgchDate, 50);
			StrUniBuf stub(rgchDate);
			stuDate.Format(stuDateFmt.Chars(), stub.Chars());
		}
		else
		{
			// TODO SteveMc: not cached -- retrieve from the database?
		}
		SmartBstr sbstrTitle = GetStatusBarPaneOneTitle(hvoCurRec);
		if (!BstrLen(sbstrTitle))
		{
			// TODO SteveMc: not cached -- retrieve from the database?
		}
		// OPTIMIZE SteveMc: cache this tooltip string.
		StrApp strTagA(kstidTlsOptEDateCreated);
		StrApp strTagB(kstidTlsOptETitle);
		StrApp strRecFmt(kstidStBarRecordFmt);		// "%s: %s"
		StrApp strFields;
		strFields.Format(strRecFmt.Chars(), strTagA.Chars(), strTagB.Chars());
		m_qstbr->SetRecordInfo(stuDate.Chars(), sbstrTitle.Chars(), strFields.Chars());
	}
	else
	{
		// No records.
		m_qstbr->SetRecordInfo(L"", L"", _T(""));
	}

	// Fill in the second and third panes of the status bar.
	Set<int> sisel;
	m_qvwbrs->GetSelection(m_ivblt[kvbltSort], sisel);
	int isort = -1;
	if (sisel.Size())
	{
		Assert(sisel.Size() == 1);
		isort = pdbi->ComputeSortIndex(*sisel.Begin(), GetRecordClid());
	}
	if (isort < 0)
	{
		m_qstbr->SetSortingStatus(false, NULL, NULL, NULL);
	}
	else
	{
		AppSortInfo & asi = pdbi->GetSortInfo(isort);
		StrUni stuSortKeyValue;		// content of current sort key
		StrUni stuSortKeyName;		// sort key identification
		if (m_vhcFilteredRecords.Size() == m_vskhSortKeys.Size())
		{
			SortMethodUtil::GenerateStatusStrings(&asi, m_vskhSortKeys[m_ihvoCurr], m_vsmn,
				m_qcvd, m_qlpi, &m_asiXref, stuSortKeyValue, stuSortKeyName);
		}
		else
		{
			SortKeyHvos skh = { 0, 0, 0 };
			SortMethodUtil::GenerateStatusStrings(&asi, skh, m_vsmn, m_qcvd, m_qlpi, &m_asiXref,
				stuSortKeyValue, stuSortKeyName);
		}
		StrApp strMethod(asi.m_stuName);
		StrApp strSortKeyName(stuSortKeyName);
		m_qstbr->SetSortingStatus(true, strMethod.Chars(), stuSortKeyValue.Chars(),
			strSortKeyName.Chars());
	}

	// Fill in the fourth pane of the status bar.
	m_qvwbrs->GetSelection(m_ivblt[kvbltFilter], sisel);
	int iflt = -1;
	if (sisel.Size())
	{
		Assert(sisel.Size() == 1);
		iflt = pdbi->ComputeFilterIndex(*sisel.Begin(), GetRecordClid());
	}
	if (iflt < 0)
	{
		m_qstbr->SetFilteringStatus(false, NULL);
	}
	else
	{
		StrApp strFilter;
		AppFilterInfo & afi = pdbi->GetFilterInfo(iflt);
		strFilter = afi.m_stuName;
		m_qstbr->SetFilteringStatus(true, strFilter.Chars());
	}

	// Fill in the fifth pane of the status bar.
	m_qstbr->SetLocationStatus(ihvo, chvo);
}


/*----------------------------------------------------------------------------------------------
	Update the caption bar display.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::UpdateCaptionBar()
{
	AfClientWnd * pafcw = m_qmdic->GetCurChild();
	if (!pafcw) // This is not always present at startup.
		return;

	if (!m_vhcFilteredRecords.Size() || m_ihvoCurr == -1)
		return; // No records present.

	HVO hvoCurRec = m_vhcFilteredRecords[0].hvo;
	Assert(m_vhvoPath.Size() >= m_vflidPath.Size());
	for (int iflid = 0; iflid < m_vflidPath.Size(); ++iflid)
	{
		if (m_vflidPath[iflid] == kflidRnGenericRec_SubRecords)
			continue;
		hvoCurRec = m_vhvoPath[iflid];
		break;
	}
	Assert(hvoCurRec);

	StrApp str = GetCaptionBarClasslabel(hvoCurRec);
	str.FormatAppend(_T("%s"), pafcw->GetViewName());
	AfCaptionBar * pcpbr = m_qmdic->GetCaptionBar();
	AssertPtr(pcpbr);
	pcpbr->SetCaptionText(str.Chars());
}

void RecMainWnd::UpdateRecordDate(HVO hvo)
{
	TagFlids tf;
	GetTagFlids(tf);
	int flidMod = tf.flidModified;
	HVO hvoMod = 0;
	if (hvo)
		hvoMod = hvo;
	else if (m_ihvoCurr != -1 && m_vhcFilteredRecords.Size())
		hvoMod = m_vhcFilteredRecords[m_ihvoCurr].hvo;
	UpdateDateModified(hvoMod, flidMod);
}


/*----------------------------------------------------------------------------------------------
	Update the title bar.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::UpdateTitleBar()
{
	// Set the caption of the main window.
	AfApp * papp = AfApp::Papp();
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	StrApp str = GetTitlePrefix();
	str.Append(pdbi->DbName());
	StrApp strSvr(pdbi->ServerName());
	if (strSvr.Length() &&
		(!strSvr.EqualsCI(dynamic_cast<AfDbApp *>(papp)->GetLocalServer())))
	{
		str.Append(_T(" - "));
		str.Append(pdbi->ServerName());
		str.Replace(str.Length() - 6, str.Length(), _T(" ")); // Remove /SILFW from server.
	}
	str.FormatAppend(_T(" - %r"), papp->GetAppNameId());
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)str.Chars());
}


/*----------------------------------------------------------------------------------------------
	Handle the undo command.

	Optimize JohnT: most commands could be successfully undone by having the UndoAction objects
	keep track of what properties changed, and broadcasting PropChanged notifications for those
	properties. That would be much faster and more efficient.

	It gets tricky because changes to minor properties could affect whether an object matches
	a filter (however, we don't yet fix that even for the original change, and for performance
	reasons we probably won't any time soon). More significantly, changes to the property on
	which the filtered list is based really should make us re-run the filter. Also, Data Entry
	view has some fields which don't monitor PropChanged notifications, and this needs to be
	added if we are to use this mechanism.

	Probably we need some sort of exception mechanism, to fall back to this "RefreshAll"
	strategy where an unusually complex change must be undone.

	@param pcmd menu command
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdEditUndo(Cmd * pcmd)
{
	AssertPtr(m_qacth);

	HRESULT hr;
	ComBool fCanUndo;
	hr = m_qacth->CanUndo(&fCanUndo);
	if (fCanUndo)
	{
		WaitCursor wc;

		ComBool fPrivate; // Are changes private to a TssEdit field editor or similar?
		CheckHr(m_qacth->get_TasksSinceMark(true, &fPrivate));
		if (!fPrivate)
		{
			// Need to do stuff like ending edits in actions BEFORE we change the data with Undo.
			SyncInfo sync(ksyncUndoRedo, 0, 0);
			AfApp::Papp()->PreSynchronize(sync, m_qlpi);
		}
		UndoResult ures;
		hr = m_qacth->Undo(&ures);
		return HandleUndoRedoResults(hr, ures, fPrivate);
	}
	return true; // Handled, even though nothing done.
}

// Common tail end of Undo and Redo. Decides whether to report a problem, and
// whether a Refresh is needed, and does the Refresh if necessary.
bool RecMainWnd::HandleUndoRedoResults(HRESULT hr, UndoResult ures, bool fPrivate)
{
	if (FAILED(hr))
		ures = kuresError; // don't think this can happen, but treat as error.
	// Enhance: may want to warn user, e.g.
	// kuresFailed: 'The Undo or Redo could not be completed, possibly because the data has
	//	been modified by another user or application'.
	// kuresError: 'Something went wrong while attempting the Undo or Redo'.

	// Treat 'private' Undo/Redos as success not needing refresh, even if the
	// undo stack doesn't know it yet.
	// In all cases except Success, we want to do our best to update the display to whatever
	// the database is now. kuresSuccess indicates that the display has already been updated.
	// If something went drastically wrong, anything could have happened, so clean up as best
	// we can. kuresRefresh indicates that the UndoActions were not able to update the cache,
	// so a refresh is necessary.
	if (!fPrivate || ures != kuresSuccess)
	{
		// Update all interested parties of change. Until we can make undo/redo smarter,
		// we have to refresh everything to make sure we add/delete records, etc.
		SyncInfo sync(ksyncUndoRedo, 0, 0);
		m_qlpi->StoreAndSync(sync);
		ShowAllOverlays(true); // Make sure the overlay windows come back on.
	}

	// Overall, the Undo/Redo succeeded if we got one of these result codes.
	return ures == kuresSuccess || ures == kuresRefresh;
}


/*----------------------------------------------------------------------------------------------
	Enable/disable the undo menu item.

	@param cms menu command state
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsEditUndo(CmdState & cms)
{
	AssertPtr(m_qacth);

	StrApp staLabel = UndoRedoText(false);
	cms.SetText(staLabel, staLabel.Length());

	ComBool fCanUndo = false;
	HRESULT hr;
	IgnoreHr(hr = m_qacth->CanUndo(&fCanUndo));
	cms.Enable(fCanUndo);
	if (FAILED(hr))
	{
		return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the redo command.

	@param pcmd menu command
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmdEditRedo(Cmd * pcmd)
{
	AssertPtr(m_qacth);

	HRESULT hr;
	ComBool fCanRedo;
	hr = m_qacth->CanRedo(&fCanRedo);
	if (fCanRedo)
	{
		WaitCursor wc;

		ComBool fPrivate; // Are changes private to a TssEdit field editor or similar?
		CheckHr(m_qacth->get_TasksSinceMark(false, &fPrivate));
		UndoResult ures;
		hr = m_qacth->Redo(&ures);
		return HandleUndoRedoResults(hr, ures, fPrivate);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Enable/disable the redo menu item.

	@param cms menu command state
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool RecMainWnd::CmsEditRedo(CmdState & cms)
{
	AssertPtr(m_qacth);

	StrApp staLabel = UndoRedoText(true);
	cms.SetText(staLabel, staLabel.Length());

	ComBool fCanRedo = false;
	HRESULT hr;
	IgnoreHr(hr = m_qacth->CanRedo(&fCanRedo));
	cms.Enable(fCanRedo);
	if (FAILED(hr))
	{
		return false;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Create the client windows and attach them to the MDI client.  Initialize the image lists.
	Create and attach our caption bar.
----------------------------------------------------------------------------------------------*/
void RecMainWnd::InitMdiClient(void)
{
	// Initialize the image lists
	ClearViewBarImageLists();

	Assert(m_rghiml[0] == 0);
	m_rghiml[0] = AfGdi::ImageList_Create(32, 32, ILC_COLORDDB | ILC_MASK, 0, 0);
	if (!m_rghiml[0])
		ThrowHr(WarnHr(E_FAIL));
	HBITMAP hbmp = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
		MAKEINTRESOURCE(kridAfVBarLarge));
	if (!hbmp)
		ThrowHr(WarnHr(E_FAIL));
	::ImageList_AddMasked(m_rghiml[0], hbmp, kclrPink);
	AfGdi::DeleteObjectBitmap(hbmp);

	Assert(m_rghiml[1] == 0);
	m_rghiml[1] = AfGdi::ImageList_Create(16, 16, ILC_COLORDDB | ILC_MASK, 0, 0);
	if (!m_rghiml[1])
		ThrowHr(WarnHr(E_FAIL));
	hbmp = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(kridAfVBarSmall));
	if (!hbmp)
		ThrowHr(WarnHr(E_FAIL));
	::ImageList_AddMasked(m_rghiml[1], hbmp, kclrPink);
	AfGdi::DeleteObjectBitmap(hbmp);

	// Create and attach our caption bar.
	// TODO: Have subclass make the caption bar of the right class.
	AfRecCaptionBarPtr qrcpbr;
	qrcpbr.Attach(CreateCaptionBar());
	m_qmdic->SetCaptionBar(qrcpbr);

	// Create the client child window.
	AfClientWndPtr qafcw;

	// Setup "vuvs" which holds all user view specs.
	UserViewSpecVec & vuvs = m_qlpi->GetDbInfo()->GetUserViewSpecs();
	if (vuvs.Size() == 0)
	{
		// Load user views from the database.
		// If we fail, create new ones and save them so we can keep going.
		if (!LoadUserViews())
		{
			UserViewSpecPtr quvs;
			quvs.Create();
			MakeNewView(kvwtBrowse, m_qlpi, quvs);
			IOleDbEncapPtr qode;
			m_qlpi->GetDbInfo()->GetDbAccess(&qode);
			if (quvs->m_vwt == kvwtBrowse)
			{
				quvs->Save(qode, true);
				vuvs.Push(quvs);
			}

			quvs.Create();
			MakeNewView(kvwtDE, m_qlpi, quvs);
			quvs->Save(qode, true);
			vuvs.Push(quvs);

			quvs.Create();
			MakeNewView(kvwtDoc, m_qlpi, quvs);
			quvs->Save(qode, true);
			vuvs.Push(quvs);
		}
		// Save vuvs in app.
		m_qlpi->GetDbInfo()->SetUserViewSpecs(&vuvs);
	}

	StrApp str;
	const OLECHAR * pwrgch;
	int cch;
	int iuvs;

	int wid = kwidChildBase;
	for (iuvs = 0; iuvs < vuvs.Size(); ++iuvs, ++wid)
	{
		vuvs[iuvs]->m_qtssName->LockText(&pwrgch, &cch);
		str.Assign(pwrgch, cch);
		vuvs[iuvs]->m_qtssName->UnlockText(pwrgch);
		switch (vuvs[iuvs]->m_vwt)
		{
		default:
			// we should never get here.
			Assert(false);
			break;

		case kvwtDE:
			qafcw.Attach(NewObj AfClientRecDeWnd);
			qafcw->Create(str.Chars(), kimagDataEntry, wid);
			break;

		case kvwtBrowse:
			{
				qafcw.Attach(NewObj AfClientRecVwWnd);
				qafcw->Create(str.Chars(), kimagBrowse, wid);
				AfClientRecVwWnd * pcrvw = dynamic_cast<AfClientRecVwWnd *>(qafcw.Ptr());
				Assert(pcrvw);
				pcrvw->EnableHScroll();	// Browse view needs horizontal scroll bar.
				break;
			}

		case kvwtDoc:
			qafcw.Attach(NewObj AfClientRecVwWnd);
			qafcw->Create(str.Chars(), kimagDocument, wid);
			break;
		}
		m_qmdic->AddChild(qafcw);
		vuvs[iuvs]->m_iwndClient = iuvs;
	}
	m_qmdic->SetClientIndexLim(wid);

	FilterUtil::LoadFilters(m_qlpi->GetDbInfo(), AfApp::Papp()->GetAppClsid());
	SortMethodUtil::LoadSortMethods(m_qlpi->GetDbInfo(), AfApp::Papp()->GetAppClsid());
	// Load overlays if they aren't already loaded.
	if (!m_qlpi->GetOverlayCount())
		m_qlpi->LoadOverlays();
}


/*----------------------------------------------------------------------------------------------
	Get the application specific flags for the viewbar, which will be used in SaveSettings..

	@return The view bar flags for this class.
----------------------------------------------------------------------------------------------*/
DWORD RecMainWnd::GetViewbarSaveFlags()
{
	AssertObj(m_qvwbrs);
	DWORD dwViewbarFlags = 0;
	if (m_qvwbrs->IsShowingLargeIcons(m_ivblt[kvbltView]))
		dwViewbarFlags |= kmskShowLargeViewIcons;
	if (m_qvwbrs->IsShowingLargeIcons(m_ivblt[kvbltFilter]))
		dwViewbarFlags |= kmskShowLargeFilterIcons;
	if (m_qvwbrs->IsShowingLargeIcons(m_ivblt[kvbltSort]))
		dwViewbarFlags |= kmskShowLargeSortIcons;
	if (m_ivblt[kvbltOverlay] != kvbltNoValue
		&& m_qvwbrs->IsShowingLargeIcons(m_ivblt[kvbltOverlay]))
		dwViewbarFlags |= kmskShowLargeOverlayIcons;
	if ((m_fFullWindow && m_fOldViewbarVisible) || ::IsWindowVisible(m_qvwbrs->Hwnd()))
		dwViewbarFlags |= kmskShowViewBar;
	return dwViewbarFlags;
}

// Check the IsProtected flag on a CmPossibility and return true if it is not set.
// If it is set, display the error message and return false.
bool RecMainWnd::IsPossibilityDeletable(HVO hvoPss, int nMsg)
{
	StrUni stuCmd;
	ComBool fMoreRows;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	pdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));
	stuCmd.Format(L"select IsProtected from CmPossibility where Id = %d", hvoPss);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		// Can't use bool here with GetColValue.
		int fProtected = 0;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&fProtected), sizeof(int),
			&cbSpaceTaken, &fIsNull, 0));
		if (fProtected)
		{
			StrApp str(nMsg);
			::MessageBox(NULL, str.Chars(), _T(""), MB_ICONSTOP | MB_OK);
			return false;
		}
	}
	return true;
}
