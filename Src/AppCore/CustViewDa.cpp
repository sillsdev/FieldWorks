/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: CustViewDa.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a customiseable view constructor for document views.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

#undef DEBUG_THIS_FILE
//#define DEBUG_THIS_FILE 1

DummyFactory g_fact(_T("SIL.AppCore.CustViewDa"));

/***********************************************************************************************
	ISilDataAccess methods overridden.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Return the writing system factory for this database (or the registry, as the case may be).

	@param ppwsf Address of the pointer for returning the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CustViewDa::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppwsf);

	(*ppwsf) = 0;
	if (!m_qwsf)
	{
		AssertPtr(m_qlpi);
		AfDbInfo * pdbi = m_qlpi->GetDbInfo();
		if (pdbi)
		{
			pdbi->GetLgWritingSystemFactory(&m_qwsf);
		}
	}
	if (m_qwsf)	// Note that when quitting the program we may not be able to get the factory.
	{
		(*ppwsf) = m_qwsf;
		(*ppwsf)->AddRef();
	}

	END_COM_METHOD(g_fact, IID_ISilDataAccess)
}

/*----------------------------------------------------------------------------------------------
	Set the writing system factory for this database (or the registry, as the case may be).

	@param pwsf Pointer to the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CustViewDa::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_fact, IID_ISilDataAccess)
}

/*----------------------------------------------------------------------------------------------
	Return the encodings of interest within the database (specifically this is currently
	used to set up the Styles dialog). Here we ask the language project.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CustViewDa::get_WritingSystemsOfInterest(int cwsMax, int * pws, int * pcws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcws);
	ChkComArrayArg(pws, cwsMax);

	// Get a pointer to the language project information, and then get references to the
	// vectors of "active" encodings.
	AfLpInfo * plpi = GetLpInfo();
	AssertPtr(plpi);
	Vector<int> & vwsAnal = plpi->AllAnalWss();
	Vector<int> & vwsVern = plpi->AllVernWss();
	Set<int> setws;
	int iws;
	for (iws = 0; iws < vwsAnal.Size(); ++iws)
		setws.Insert(vwsAnal[iws]);
	for (iws = 0; iws < vwsVern.Size(); ++iws)
		setws.Insert(vwsVern[iws]);
	if (cwsMax == 0)
	{
		*pcws = setws.Size();
		return S_OK;
	}
	if (cwsMax < setws.Size())
		return E_INVALIDARG;
	*pcws = min(cwsMax, setws.Size());
	Set<int>::iterator it;
	for (it = setws.Begin(), iws = 0; it != setws.End(); ++it, iws++)
		*(pws + iws) = it->GetValue();


//	Vector<int> vwsProj;
//	plpi->ProjectWritingSystems(vwsProj);
//	if (cwsMax == 0)
//	{
//		*pcws = vwsProj.Size();
//		return S_OK;
//	}
//	*pcws = min(cwsMax, vwsProj.Size());
//	for (int iws = 0; iws < *pcws; iws++)
//	{
//		*(pws + iws) = vwsProj[iws];
//	}

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/***********************************************************************************************
	CustViewDa methods.
***********************************************************************************************/

CustViewDa::CustViewDa()
{
}

CustViewDa::~CustViewDa()
{
}

/*----------------------------------------------------------------------------------------------
	Load the root items.

	@param hvoRoot Database id of the root object
	@param vhcItems Reference to output vector of database and class ids
	@param pasi Optional pointer to default sort method info (may be NULL)
	@param vskhItems Optional pointer to output vector of sort key object ids (may be NULL).
----------------------------------------------------------------------------------------------*/
void CustViewDa::LoadMainItems(HVO hvoRoot, HvoClsidVec & vhcItems, AppSortInfo * pasi,
	SortKeyHvosVec * pvskhItems)
{
	AssertPtr(m_qlpi);
	Assert(m_tagRootItems);

	IFwMetaDataCachePtr qmdc;
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	SmartBstr sbstrClassName;
	SmartBstr sbstrDstClsName;
	SmartBstr sbstrFieldName;
	SmartBstr sbstrEntryClassName;
	SmartBstr sbstrEntrySortField;
	vhcItems.Clear();

	try
	{
		// Determine whether we have a sort method defined.
		int cSortKeys = 0;
		if (pasi)
		{
			if (pasi->m_stuTertiaryField.Length())
				cSortKeys = 3;
			else if (pasi->m_stuSecondaryField.Length())
				cSortKeys = 2;
			else if (pasi->m_stuPrimaryField.Length())
				cSortKeys = 1;
		}
		//  Obtain pointer to IOleDbEncap interface and execute the given SQL command.
		qode = m_qode;
		qmdc = m_qmdc;

		CheckHr(qmdc->GetOwnClsName(m_tagRootItems, &sbstrClassName));
		CheckHr(qmdc->GetDstClsName(m_tagRootItems, &sbstrDstClsName));
		CheckHr(qmdc->GetFieldName(m_tagRootItems, &sbstrFieldName));

		/*  Construct a query to retrieve the main items.
			Example query:
				select rgrr.Dst rgrid, Class$ clsid
				from RnResearchNbk_Records rgrr
				join CmObject co on co.id = rgrr.dst
				join RnGenericRec rgr on rgr.id = rgrr.dst
				where rgrr.Src = 1628
				order by rgr.DateCreated
			Example results:
				rgrid       clsid
				----------- -----------
				1529        4006
				1536        4006
				1580        4006
				1584        4014
		*/
		StrUni stuSqlStmt;
		StrUni stuTable;
		StrUni stuSelect;
		StrUni stuFrom;
		StrUni stuJoin;
		StrUni stuWhere;
		StrUni stuOrder;

		stuSelect.Assign(L"SELECT rgrr.Dst, co.Class$");
		stuFrom.Format(L"FROM %s_%s rgrr%n"
			L"JOIN CmObject co ON co.id = rgrr.dst%n",
			sbstrClassName.Chars(), sbstrFieldName.Chars());
		stuWhere.Format(L"WHERE rgrr.Src = %d%n", hvoRoot);
		if (cSortKeys)
		{
			// Initialize these under the assumption that we need to use collations.
			stuTable.Format(
				L"    ObjId int,%n"
				L"    ClsId int");
			stuOrder.Format(L"SELECT ObjId, ClsId");

			StrUni stuAddSel;
			SortMethodUtil::BuildSqlPieces(pasi, L"rgrr.dst", m_qmdc, m_qwsf, NULL, NULL,
				stuTable, stuAddSel, stuJoin, stuOrder);

			if (stuAddSel.Length())
			{
				Assert(stuTable.Length());
				Assert(stuJoin.Length());
				Assert(stuOrder.Length());
				stuSelect.Append(stuAddSel);
			}
			else
			{
				Assert(!stuTable.Length());
				cSortKeys = 0;		// Just in case we had an error.
			}
		}
		if (!stuJoin.Length() && m_tagItemSort)
		{
			// Either no default sort method, or it crashed somehow: use this simple backup.
			CheckHr(qmdc->GetOwnClsName(m_tagItemSort, &sbstrEntryClassName));
			CheckHr(qmdc->GetFieldName(m_tagItemSort, &sbstrEntrySortField));
			stuJoin.Format(L"join %s rgr on rgr.id = rgrr.dst%n",
				sbstrEntryClassName.Chars());
			stuOrder.Format(L"ORDER BY rgr.%s", sbstrEntrySortField.Chars());
		}
		stuSqlStmt.Format(L"%s%s%n%s%s%s%s",
			stuTable.Chars(), stuSelect.Chars(), stuFrom.Chars(), stuJoin.Chars(),
			stuWhere.Chars(), stuOrder.Chars());
#ifdef DEBUG_THIS_FILE
		StrAnsi staT(stuSqlStmt);
		::OutputDebugString(staT.Chars());
#endif

		CheckHr(qode->CreateCommand(&qodc));
		if (stuTable.Length())
			CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtStoredProcedure));
		else
			CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));

		//  Get items from the cache and push them on the vhcItems vector.
		// OPTIMIZE JohnT: we should either take advantage of the fact we are getting the items
		// into the cache, by not reloading later (but how about staleness?), or not
		// bother to load the cache here, just read them directly. But this is easier.
		Vector<HVO> vhvo;
		while (fMoreRows)
		{
			HvoClsid hc;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hc.hvo),
				isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hc.clsid),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			vhcItems.Push(hc);
			vhvo.Push(hc.hvo);
			CacheIntProp(hc.hvo, kflidCmObject_Class, hc.clsid);
			// Save the owner of the objects as well.
			CacheObjProp(hc.hvo, kflidCmObject_Owner, hvoRoot);
			if (pvskhItems && cSortKeys > 0)
			{
				SortKeyHvos skh;
				CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&skh.m_hvoPrimary),
					isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
				if (cSortKeys >= 2)
				{
					CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&skh.m_hvoSecondary),
						isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
				}
				else
				{
					skh.m_hvoSecondary = 0;
				}
				if (cSortKeys >= 3)
				{
					CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&skh.m_hvoTertiary),
						isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
				}
				else
				{
					skh.m_hvoTertiary = 0;
				}
				pvskhItems->Push(skh);
			}
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		if (vhvo.Size())
			CacheVecProp(hvoRoot, m_tagRootItems, vhvo.Begin(), vhvo.Size());
	}
	catch (...)	// Was empty.
	{
		throw;	// For now we have nothing to add, so pass it on up.
	}
}
