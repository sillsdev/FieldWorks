/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2004, SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: RnCustomExport.cpp
Responsibility: Steve McConnel
Last reviewed: never.

Description:
	Implementation of the File / Export dialog classes for the Data Notebook.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

static DummyFactory g_fact(_T("Sil.Notebk.RnCustomExport"));

//:>********************************************************************************************
//:>	RnCustomExport methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor: just call the base class constructor.
----------------------------------------------------------------------------------------------*/
RnCustomExport::RnCustomExport(AfLpInfo * plpi, AfMainWnd * pafw)
	: AfCustomExport(plpi, pafw)
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
RnCustomExport::~RnCustomExport()
{
}


//:>********************************************************************************************
//:>	IUnknown methods: use the inherited implementations.
//:>********************************************************************************************


//:>********************************************************************************************
//:>	IFwCustomExport methods.
//:>	Use the inherited implementations for the following methods:
//:>
//:>	HRESULT SetLabelStyles(BSTR bstrLabel, BSTR bstrSubLabel)
//:>	HRESULT AddFlidCharStyleMapping(int flid, BSTR bstrStyle)
//:>	HRESULT GetActualLevel(int nLevel, int hvoRec, int ws, int * pnActualLevel)
//:>	HRESULT GetPageSetupInfo(int * pnOrientation, int * pnPaperSize, ...)
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the sequence of certain
	subitems owned by the current record.

	@param pffsp Pointer to the current user view field specification COM object.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustomExport::BuildSubItemsString(IFwFldSpec * pffsp, int hvoRec, int ws,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pffsp);
	ChkComOutPtr(pptss);
	if (!hvoRec || !ws)
		ThrowInternalError(E_INVALIDARG);
	AssertPtr(m_qode);
	AssertPtr(m_qmdc);

	// Extract the needed values from the COM field spec object.
	FldSpecPtr qfsp;
	qfsp.Create();
	ExtractFldSpec(pffsp, qfsp);

	Vector<HVO> vhvoPss;
	HVO hvoPssl = 0;
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		ULONG nDstCls;
		CheckHr(m_qmdc->GetDstClsId(qfsp->m_flid, &nDstCls));
		if (nDstCls == kclidRnRoledPartic)
		{
			SmartBstr sbstrClsName;
			SmartBstr sbstrFldName;
			CheckHr(m_qmdc->GetOwnClsName(kflidRnRoledPartic_Participants,
				&sbstrClsName));
			CheckHr(m_qmdc->GetFieldName(kflidRnRoledPartic_Participants, &sbstrFldName));
			StrUni stuQuery;
			ComBool fMoreRows;
			ComBool fIsNull;
			ULONG cbSpaceTaken;
			stuQuery.Format(L"SELECT [Dst] FROM [%s_%s]%n"
				L"WHERE [Src] IN (SELECT [Dst] FROM [%s_%s] WHERE [Src]=%d)",
				sbstrClsName.Chars(), sbstrFldName.Chars(),
				qfsp->m_stuClsName.Chars(), qfsp->m_stuFldName.Chars(), hvoRec);
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			while (fMoreRows)
			{
				HVO hvo;
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvo),
					sizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
					vhvoPss.Push(hvo);
				CheckHr(qodc->NextRow(&fMoreRows));
			}
			if (vhvoPss.Size())
			{
				stuQuery.Format(
					L"SELECT [Owner$] FROM [CmObject] WHERE [Id] = %d",
					vhvoPss[0]);
				CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				if (fMoreRows)
				{
					CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoPssl),
						sizeof(hvoPssl), &cbSpaceTaken, &fIsNull, 0));
					if (fIsNull)
						hvoPssl = 0;
				}
			}
		}
	}
	catch (...)
	{
		vhvoPss.Clear();
		hvoPssl = 0;
	}
	if (hvoPssl || qfsp->m_eVisibility == kFTVisAlways)
	{
		ITsIncStrBldrPtr qtisb;
		ITsStringPtr qtss;
		if (!qfsp->m_fHideLabel && qfsp->m_qtssLabel)
		{
			ITsStrBldrPtr qtsb;
			CheckHr(qfsp->m_qtssLabel->GetBldr(&qtsb));
			int cch;
			CheckHr(qtsb->get_Length(&cch));
			CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle, m_stuLabelFormat.Bstr()));
			CheckHr(qtsb->ReplaceRgch(cch, cch, L":", 1, NULL));
			CheckHr(qtsb->GetString(&qtss));
			CheckHr(qtss->GetIncBldr(&qtisb));
			CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
			CheckHr(qtisb->AppendRgch(L" ", 1));
		}
		else
		{
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			AssertPtr(qtsf);
			CheckHr(qtsf->GetIncBldr(&qtisb));
			CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
		}
		StrUni stu;
		PossListInfoPtr qpli;
		if (hvoPssl && m_plpi->LoadPossList(hvoPssl, ws, &qpli))
		{
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, qfsp->m_stuFldName.Bstr()));
			PossNameType pnt = qpli->GetDisplayOption();
			int cItemsOut = 0;
			int ihvo;
			for (ihvo = 0; ihvo < vhvoPss.Size(); ++ihvo)
			{
				if (cItemsOut)
				{
					stu.Assign(", ");
					CheckHr(qtisb->Append(stu.Bstr()));
				}
				int ipii = qpli->GetIndexFromId(vhvoPss[ihvo]);
				if (ipii >= 0)
				{
					CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvForceOn));
					PossItemInfo * ppii = qpli->GetPssFromIndex(ipii);
					AssertPtr(ppii);
					ppii->GetName(stu, pnt);
					bool fHaveStyle = false;
					if (qfsp->m_stuSty.Length())
					{
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
						fHaveStyle = true;
					}
					else
					{
						StrUni stuSty;
						if (m_hmflidstuCharStyle.Retrieve(qfsp->m_flid, &stuSty))
						{
							CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
							fHaveStyle = true;
						}
					}
					CheckHr(qtisb->Append(stu.Bstr()));
					if (fHaveStyle)
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
					CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvOff));
					++cItemsOut;
				}
			}
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
		}
		stu.Assign(L".");
		CheckHr(qtisb->Append(stu.Bstr()));
		CheckHr(qtisb->GetString(pptss));
	}

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the object reference contained
	by the current record.

	@param pffsp Pointer to the current user view field specification COM object.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustomExport::BuildObjRefSeqString(IFwFldSpec * pffsp, int hvoRec, int ws,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pffsp);
	ChkComOutPtr(pptss);
	if (!hvoRec || !ws)
		ThrowInternalError(E_INVALIDARG);
	AssertPtr(m_qode);
	AssertPtr(m_qmdc);

	// Extract the needed values from the COM field spec object.
	FldSpecPtr qfsp;
	qfsp.Create();
	ExtractFldSpec(pffsp, qfsp);

	ComVector<ITsString> vqtssObjRefs;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	AssertPtr(qtsf);
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		ULONG nDstCls;
		SmartBstr sbstrDstCls;
		CheckHr(m_qmdc->GetDstClsId(qfsp->m_flid, &nDstCls));
		CheckHr(m_qmdc->GetClassName(nDstCls, &sbstrDstCls));
		if (nDstCls == kclidRnGenericRec || nDstCls == kclidRnEvent ||
			nDstCls == kclidRnAnalysis)
		{
			StrUni stuEvent(kstidEvent);
			StrUni stuAnalysis(kstidAnalysis);
			StrUni stuSubevent(kstidSubevent);
			StrUni stuSubanalysis(kstidSubanalysis);
			StrUni stuQuery;
			ComBool fMoreRows;
			ComBool fIsNull;
			ULONG cbSpaceTaken;
			stuQuery.Format(
				L"SELECT co.Class$, co.OwnFlid$, xx.Title, xx.Title_Fmt, xx.DateCreated%n"
				L"    FROM %s_ xx%n"
				L"    JOIN CmObject co ON co.[Id] = xx.[Id]%n"
				L"WHERE xx.[Id] IN (SELECT [Dst] FROM %s_%s where [Src] = %d)",
				sbstrDstCls.Chars(),
				qfsp->m_stuClsName.Chars(), qfsp->m_stuFldName.Chars(), hvoRec);
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			int clidObj;
			int flidOwner;
			Vector<OLECHAR> vchName;
			Vector<byte> vbNameFmt;
			vchName.Resize(512);
			vbNameFmt.Resize(512);
			int cchName;
			int cbNameFmt;
			DBTIMESTAMP tim;
			while (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&clidObj),
					sizeof(clidObj), &cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
				{
					CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&flidOwner),
						sizeof(flidOwner), &cbSpaceTaken, &fIsNull, 0));
				}
				if (!fIsNull)
				{
					CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(vchName.Begin()),
						vchName.Size() * sizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull)
					{
						cchName = cbSpaceTaken / sizeof(OLECHAR);
						if (cchName > vchName.Size())
						{
							vchName.Resize(cchName, true);
							CheckHr(qodc->GetColValue(3,
								reinterpret_cast<BYTE *>(vchName.Begin()),
								vchName.Size() * sizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 0));
						}
						CheckHr(qodc->GetColValue(4,
							reinterpret_cast<BYTE *>(vbNameFmt.Begin()), vbNameFmt.Size(),
							&cbSpaceTaken, &fIsNull, 0));
						if (!fIsNull)
						{
							cbNameFmt = cbSpaceTaken;
							if (cbNameFmt > vbNameFmt.Size())
							{
								vbNameFmt.Resize(cbNameFmt, true);
								CheckHr(qodc->GetColValue(4,
									reinterpret_cast<BYTE *>(vbNameFmt.Begin()),
									vbNameFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
							}
						}
						else
						{
							cbNameFmt = 0;
						}
					}
					else
					{
						cchName = 0;
					}
					// Build a string for this reference.
					ITsIncStrBldrPtr qtisb;
					ITsStringPtr qtss;
					CheckHr(qtsf->GetIncBldr(&qtisb));
					CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
					CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvForceOn));
					StrUni stu;
					if (flidOwner == kflidRnResearchNbk_Records)
					{
						if (clidObj == kclidRnEvent)
							stu = stuEvent;
						else
							stu = stuAnalysis;
					}
					else
					{
						if (clidObj == kclidRnEvent)
							stu = stuSubevent;
						else
							stu = stuSubanalysis;
					}
					stu.Append(L" - ");
					CheckHr(qtisb->Append(stu.Bstr()));
					if (cchName)
					{
						if (cbNameFmt)
						{
							CheckHr(qtsf->DeserializeStringRgch(vchName.Begin(), &cchName,
								vbNameFmt.Begin(), &cbNameFmt, &qtss));
							if (qtss)
							{
								ITsStrBldrPtr qtsb;
								CheckHr(qtss->GetBldr(&qtsb));
								int cch;
								CheckHr(qtsb->get_Length(&cch));
								CheckHr(qtsb->SetIntPropValues(0, cch, ktptMarkItem,
									ktpvEnum, kttvForceOn));
								CheckHr(qtsb->GetString(&qtss));
								CheckHr(qtisb->AppendTsString(qtss));
							}
							CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
						}
						else
						{
							stu.Assign(vchName.Begin(), cchName);
							CheckHr(qtisb->Append(stu.Bstr()));
						}
					}
					stu.Assign(L" - ");
					CheckHr(qtisb->Append(stu.Bstr()));
					CheckHr(qodc->GetColValue(5, reinterpret_cast <BYTE *>(&tim),
						sizeof(DBTIMESTAMP), &cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull)
					{
						achar rgchFmt[81];
						int cchFmt = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SSHORTDATE,
							rgchFmt, 80);
						rgchFmt[cchFmt] = 0;
						achar rgchDate[81];
						SYSTEMTIME systim;
						memset(&systim, 0, sizeof(systim));
						systim.wYear = tim.year;
						systim.wMonth = tim.month;
						systim.wDay = tim.day;
						int cch = ::GetDateFormat(NULL, 0, &systim, rgchFmt, rgchDate, 80);
						rgchFmt[cch] = 0;
						stu.Assign(rgchDate);
						CheckHr(qtisb->Append(stu.Bstr()));
					}
					CheckHr(qtisb->GetString(&qtss));
					StrUni stuSty;
					if (qfsp->m_stuSty.Length())
					{
						stuSty = qfsp->m_stuSty;
					}
					else if (!m_hmflidstuCharStyle.Retrieve(qfsp->m_flid, &stuSty))
					{
						stuSty.Clear();		// Probably paranoid.
					}
					if (stuSty.Length())
					{
						ITsStrBldrPtr qtsb;
						CheckHr(qtss->GetBldr(&qtsb));
						int cch;
						CheckHr(qtsb->get_Length(&cch));
						CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle, stuSty.Bstr()));
						CheckHr(qtsb->GetString(&qtss));
					}
					vqtssObjRefs.Push(qtss);
				}
				CheckHr(qodc->NextRow(&fMoreRows));
			}
		}
	}
	catch (...)
	{
		vqtssObjRefs.Clear();
	}
	if (vqtssObjRefs.Size() || qfsp->m_eVisibility == kFTVisAlways)
	{
		ITsIncStrBldrPtr qtisb;
		ITsStringPtr qtss;
		if (!qfsp->m_fHideLabel && qfsp->m_qtssLabel)
		{
			ITsStrBldrPtr qtsb;
			CheckHr(qfsp->m_qtssLabel->GetBldr(&qtsb));
			int cch;
			CheckHr(qtsb->get_Length(&cch));
			CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle, m_stuLabelFormat.Bstr()));
			CheckHr(qtsb->ReplaceRgch(cch, cch, L":", 1, NULL));
			CheckHr(qtsb->GetString(&qtss));
			CheckHr(qtss->GetIncBldr(&qtisb));
			CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
			CheckHr(qtisb->AppendRgch(L" ", 1));
		}
		else
		{
			CheckHr(qtsf->GetIncBldr(&qtisb));
			CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
		}
		StrUni stu(L", ");
		int itss;
		CheckHr(qtisb->SetStrPropValue(ktptFieldName, qfsp->m_stuFldName.Bstr()));
		for (itss = 0; itss < vqtssObjRefs.Size(); ++itss)
		{
			if (itss)
			{
				CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
				CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvOff));
				CheckHr(qtisb->Append(stu.Bstr()));
			}
			if (vqtssObjRefs[itss])
			{
				ITsStrBldrPtr qtsb;
				CheckHr(vqtssObjRefs[itss]->GetBldr(&qtsb));
				int cch;
				CheckHr(qtsb->get_Length(&cch));
				CheckHr(qtsb->SetStrPropValue(0, cch, ktptFieldName,
					qfsp->m_stuFldName.Bstr()));
				ITsStringPtr qtss;
				CheckHr(qtsb->GetString(&qtss));
				CheckHr(qtisb->AppendTsString(qtss));
				CheckHr(qtisb->SetStrPropValue(ktptFieldName, qfsp->m_stuFldName.Bstr()));
			}
		}
		CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
		CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
		CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
		stu.Assign(L".");
		CheckHr(qtisb->Append(stu.Bstr()));
		CheckHr(qtisb->GetString(pptss));
	}

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the object reference contained
	by the current record.

	@param pffsp Pointer to the current user view field specification COM object.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustomExport::BuildObjRefAtomicString(IFwFldSpec * pffsp, int hvoRec, int ws,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pffsp);
	ChkComOutPtr(pptss);
	if (!hvoRec || !ws)
		ThrowInternalError(E_INVALIDARG);
	AssertPtr(m_qode);
	AssertPtr(m_qmdc);

	// Extract the needed values from the COM field spec object.
	FldSpecPtr qfsp;
	qfsp.Create();
	ExtractFldSpec(pffsp, qfsp);

	ITsStringPtr qtssObjRef;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	AssertPtr(qtsf);
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		ULONG nDstCls;
		SmartBstr sbstrDstCls;
		CheckHr(m_qmdc->GetDstClsId(qfsp->m_flid, &nDstCls));
		CheckHr(m_qmdc->GetClassName(nDstCls, &sbstrDstCls));
		if (nDstCls == kclidRnGenericRec || nDstCls == kclidRnEvent ||
			nDstCls == kclidRnAnalysis)
		{
			StrUni stuEvent(kstidEvent);
			StrUni stuAnalysis(kstidAnalysis);
			StrUni stuSubevent(kstidSubevent);
			StrUni stuSubanalysis(kstidSubanalysis);
			StrUni stuQuery;
			ComBool fMoreRows;
			ComBool fIsNull;
			ULONG cbSpaceTaken;
			stuQuery.Format(
				L"SELECT co.Class$, co.OwnFlid$, xx.Title, xx.Title_Fmt, xx.DateCreated%n"
				L"    FROM %s_ xx%n"
				L"    JOIN CmObject co ON co.[Id] = xx.[Id]%n"
				L"WHERE xx.[Id] IN (SELECT [%s] FROM [%s] where [Id] = %d)",
				sbstrDstCls.Chars(),
				qfsp->m_stuFldName.Chars(), qfsp->m_stuClsName.Chars(), hvoRec);
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			int clidObj;
			int flidOwner;
			Vector<OLECHAR> vchName;
			Vector<byte> vbNameFmt;
			vchName.Resize(512);
			vbNameFmt.Resize(512);
			int cchName;
			int cbNameFmt;
			DBTIMESTAMP tim;
			if (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&clidObj),
					sizeof(clidObj), &cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
				{
					CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&flidOwner),
						sizeof(flidOwner), &cbSpaceTaken, &fIsNull, 0));
				}
				if (!fIsNull)
				{
					CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(vchName.Begin()),
						vchName.Size() * sizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull)
					{
						cchName = cbSpaceTaken / sizeof(OLECHAR);
						if (cchName > vchName.Size())
						{
							vchName.Resize(cchName, true);
							CheckHr(qodc->GetColValue(3,
								reinterpret_cast<BYTE *>(vchName.Begin()),
								vchName.Size() * sizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 0));
						}
						CheckHr(qodc->GetColValue(4,
							reinterpret_cast<BYTE *>(vbNameFmt.Begin()), vbNameFmt.Size(),
							&cbSpaceTaken, &fIsNull, 0));
						if (!fIsNull)
						{
							cbNameFmt = cbSpaceTaken;
							if (cbNameFmt > vbNameFmt.Size())
							{
								vbNameFmt.Resize(cbNameFmt, true);
								CheckHr(qodc->GetColValue(4,
									reinterpret_cast<BYTE *>(vbNameFmt.Begin()),
									vbNameFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
							}
						}
						else
						{
							cbNameFmt = 0;
						}
					}
					else
					{
						cchName = 0;
					}
					// Build a string for this reference.
					ITsIncStrBldrPtr qtisb;
					ITsStringPtr qtss;
					CheckHr(qtsf->GetIncBldr(&qtisb));
					CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
					CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvForceOn));
					StrUni stu;
					if (flidOwner == kflidRnResearchNbk_Records)
					{
						if (clidObj == kclidRnEvent)
							stu = stuEvent;
						else
							stu = stuAnalysis;
					}
					else
					{
						if (clidObj == kclidRnEvent)
							stu = stuSubevent;
						else
							stu = stuSubanalysis;
					}
					stu.Append(L" - ");
					CheckHr(qtisb->Append(stu.Bstr()));
					if (cchName)
					{
						if (cbNameFmt)
						{
							CheckHr(qtsf->DeserializeStringRgch(vchName.Begin(), &cchName,
								vbNameFmt.Begin(), &cbNameFmt, &qtss));
							if (qtss)
							{
								ITsStrBldrPtr qtsb;
								CheckHr(qtss->GetBldr(&qtsb));
								int cch;
								CheckHr(qtsb->get_Length(&cch));
								CheckHr(qtsb->SetIntPropValues(0, cch, ktptMarkItem,
									ktpvEnum, kttvForceOn));
								CheckHr(qtsb->GetString(&qtss));
								CheckHr(qtisb->AppendTsString(qtss));
							}
							CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
						}
						else
						{
							stu.Assign(vchName.Begin(), cchName);
							CheckHr(qtisb->Append(stu.Bstr()));
						}
					}
					stu.Assign(L" - ");
					CheckHr(qtisb->Append(stu.Bstr()));
					CheckHr(qodc->GetColValue(5, reinterpret_cast <BYTE *>(&tim),
						sizeof(DBTIMESTAMP), &cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull)
					{
						achar rgchFmt[81];
						int cchFmt = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SSHORTDATE,
							rgchFmt, 80);
						rgchFmt[cchFmt] = 0;
						achar rgchDate[81];
						SYSTEMTIME systim;
						memset(&systim, 0, sizeof(systim));
						systim.wYear = tim.year;
						systim.wMonth = tim.month;
						systim.wDay = tim.day;
						int cch = ::GetDateFormat(NULL, 0, &systim, rgchFmt, rgchDate, 80);
						rgchFmt[cch] = 0;
						stu.Assign(rgchDate);
						CheckHr(qtisb->Append(stu.Bstr()));
					}
					CheckHr(qtisb->GetString(&qtss));
					StrUni stuSty;
					if (qfsp->m_stuSty.Length())
					{
						stuSty = qfsp->m_stuSty;
					}
					else if (!m_hmflidstuCharStyle.Retrieve(qfsp->m_flid, &stuSty))
					{
						stuSty.Clear();		// Probably paranoid.
					}
					if (stuSty.Length())
					{
						ITsStrBldrPtr qtsb;
						CheckHr(qtss->GetBldr(&qtsb));
						int cch;
						CheckHr(qtsb->get_Length(&cch));
						CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle, stuSty.Bstr()));
						CheckHr(qtsb->GetString(&qtss));
					}
					qtssObjRef = qtss;
				}
			}
		}
		if (qtssObjRef || qfsp->m_eVisibility == kFTVisAlways)
		{
			ITsIncStrBldrPtr qtisb;
			ITsStringPtr qtss;
			if (!qfsp->m_fHideLabel && qfsp->m_qtssLabel)
			{
				ITsStrBldrPtr qtsb;
				CheckHr(qfsp->m_qtssLabel->GetBldr(&qtsb));
				int cch;
				CheckHr(qtsb->get_Length(&cch));
				CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle, m_stuLabelFormat.Bstr()));
				CheckHr(qtsb->ReplaceRgch(cch, cch, L":", 1, NULL));
				CheckHr(qtsb->GetString(&qtss));
				CheckHr(qtss->GetIncBldr(&qtisb));
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
				CheckHr(qtisb->AppendRgch(L" ", 1));
			}
			else
			{
				CheckHr(qtsf->GetIncBldr(&qtisb));
				CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
			}
			if (qtssObjRef)
			{
				ITsStrBldrPtr qtsb;
				CheckHr(qtssObjRef->GetBldr(&qtsb));
				int cch;
				CheckHr(qtsb->get_Length(&cch));
				CheckHr(qtsb->SetStrPropValue(0, cch, ktptFieldName,
					qfsp->m_stuFldName.Bstr()));
				ITsStringPtr qtss;
				CheckHr(qtsb->GetString(&qtss));
				CheckHr(qtisb->AppendTsString(qtss));
				CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
			}
			CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
			CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
			StrUni stu(L".");
			CheckHr(qtisb->Append(stu.Bstr()));
			CheckHr(qtisb->GetString(pptss));
		}
	}
	catch (...)
	{
	}
	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the "expandable" information
	contained by the current record.

	@param pffsp Pointer to the current user view field specification COM object.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustomExport::BuildExpandableString(IFwFldSpec * pffsp, int hvoRec, int ws,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pffsp);
	ChkComOutPtr(pptss);
	if (!hvoRec || !ws)
		ThrowInternalError(E_INVALIDARG);
	AssertPtr(m_qode);
	AssertPtr(m_qmdc);

	// Extract the needed values from the COM field spec object.
	FldSpecPtr qfsp;
	qfsp.Create();
	ExtractFldSpec(pffsp, qfsp);

	Set<HVO> sethvoRole;
	MultiMap<HVO,HVO> mmhvoRolehvoPerson;
	HVO hvoPsslRole = 0;
	HVO hvoPsslPerson = 0;
	HVO hvoRole = 0;
	HVO hvoPerson = 0;
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		ULONG nDstCls;
		CheckHr(m_qmdc->GetDstClsId(qfsp->m_flid, &nDstCls));
		if (nDstCls == kclidRnRoledPartic)
		{
			SmartBstr sbstrClass;
			SmartBstr sbstrPartField;
			SmartBstr sbstrRoleField;
			CheckHr(m_qmdc->GetClassName(kclidRnRoledPartic, &sbstrClass));
			CheckHr(m_qmdc->GetFieldName(kflidRnRoledPartic_Participants,
				&sbstrPartField));
			CheckHr(m_qmdc->GetFieldName(kflidRnRoledPartic_Role, &sbstrRoleField));
			StrUni stuQuery;
			ComBool fMoreRows;
			ComBool fIsNull;
			ULONG cbSpaceTaken;
/*
	select rp.Role,rpp.Dst
	from RnEvent_Participants ep
	left outer join RnRoledPartic rp on rp.id = ep.Dst
	left outer join RnRoledPartic_Participants rpp on rpp.Src = rp.id
	where ep.Src = 2022
*/
			stuQuery.Format(L"SELECT rp.[%s],rpp.[Dst]%n"
				L"FROM [%s_%s] ep%n"
				L"LEFT OUTER JOIN [%s] rp ON rp.[id] = ep.[Dst]%n"
				L"LEFT OUTER JOIN [%s_%s] rpp ON rpp.[Src] = ep.[Dst]%n"
				L"WHERE ep.[Src] = %d",
				sbstrRoleField.Chars(),
				qfsp->m_stuClsName.Chars(), qfsp->m_stuFldName.Chars(),
				sbstrClass.Chars(),
				sbstrClass.Chars(), sbstrPartField.Chars(),
				hvoRec);
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			while (fMoreRows)
			{
				HVO hvo1;
				HVO hvo2;
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvo1),
					sizeof(hvo1), &cbSpaceTaken, &fIsNull, 0));
				if (fIsNull)
					hvo1 = 0;
				CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(&hvo2),
					sizeof(hvo2), &cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
				{
					sethvoRole.Insert(hvo1);
					mmhvoRolehvoPerson.Insert(hvo1, hvo2);
					if (hvo1)
						hvoRole = hvo1;
					hvoPerson = hvo2;
				}
				CheckHr(qodc->NextRow(&fMoreRows));
			}
			if (hvoRole)
			{
				stuQuery.Format(
					L"SELECT [Owner$] FROM [CmObject] WHERE [Id] = %d",
					hvoRole);
				CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				if (fMoreRows)
				{
					CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoPsslRole),
						sizeof(hvoPsslRole), &cbSpaceTaken, &fIsNull, 0));
					if (fIsNull)
						hvoPsslRole = 0;
				}
			}
			if (hvoPerson)
			{
				stuQuery.Format(
					L"SELECT [Owner$] FROM [CmObject] WHERE [Id] = %d",
					hvoPerson);
				CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				if (fMoreRows)
				{
					CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoPsslPerson),
						sizeof(hvoPsslPerson), &cbSpaceTaken, &fIsNull, 0));
					if (fIsNull)
						hvoPsslPerson = 0;
				}
			}
		}
	}
	catch (...)
	{
		hvoPsslRole = 0;
		hvoPsslPerson = 0;
	}
	if (hvoPsslPerson || qfsp->m_eVisibility == kFTVisAlways)
	{
		ITsIncStrBldrPtr qtisb;
		ITsStringPtr qtss;
		if (!qfsp->m_fHideLabel && qfsp->m_qtssLabel)
		{
			ITsStrBldrPtr qtsb;
			CheckHr(qfsp->m_qtssLabel->GetBldr(&qtsb));
			int cch;
			CheckHr(qtsb->get_Length(&cch));
			CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle, m_stuLabelFormat.Bstr()));
			CheckHr(qtsb->ReplaceRgch(cch, cch, L":", 1, NULL));
			CheckHr(qtsb->GetString(&qtss));
			CheckHr(qtss->GetIncBldr(&qtisb));
			CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
			CheckHr(qtisb->AppendRgch(L" ", 1));
		}
		else
		{
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			AssertPtr(qtsf);
			CheckHr(qtsf->GetIncBldr(&qtisb));
			CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
		}
		StrUni stu;
		PossListInfoPtr qpliRole;
		PossNameType pntRole = kpntName;
		bool fRoleOk = true;
		if (hvoPsslRole)
		{
			if (!m_plpi->LoadPossList(hvoPsslRole, ws, &qpliRole))
			{
				qpliRole = NULL;
				fRoleOk = false;
			}
			else
			{
				pntRole = qpliRole->GetDisplayOption();
			}
		}
		PossListInfoPtr qpli;
		if (fRoleOk && hvoPsslPerson && m_plpi->LoadPossList(hvoPsslPerson, ws, &qpli))
		{
			PossNameType pnt = qpli->GetDisplayOption();
			// First, check for any unroled participants.
			hvoRole = 0;
			Vector<StrUni> vstuNames;
			StrUni stu;
			MultiMap<HVO,HVO>::iterator mmit;
			MultiMap<HVO,HVO>::iterator mmitLim;
			if (sethvoRole.IsMember(hvoRole))
			{
				if (mmhvoRolehvoPerson.Retrieve(hvoRole, &mmit, &mmitLim))
				{
					for (; mmit != mmitLim; ++mmit)
					{
						hvoPerson = mmit->GetValue();
						int ipii = qpli->GetIndexFromId(hvoPerson);
						if (ipii >= 0)
						{
							PossItemInfo * ppii = qpli->GetPssFromIndex(ipii);
							AssertPtr(ppii);
							ppii->GetName(stu, pnt);
							int iv;
							int ivLim;
							for (iv = 0, ivLim = vstuNames.Size(); iv < ivLim; )
							{
								int ivMid = (iv + ivLim) / 2;
								if (_wcsicmp(vstuNames[ivMid].Chars(), stu.Chars()) < 0)
									iv = ivMid + 1;
								else
									ivLim = ivMid;
							}
							vstuNames.Insert(iv, stu);
						}
					}
				}
				CheckHr(qtisb->SetStrPropValue(ktptFieldName, qfsp->m_stuFldName.Bstr()));
				for (int istu = 0; istu < vstuNames.Size(); ++istu)
				{
					if (istu)
						CheckHr(qtisb->AppendRgch(L", ", 2));
					CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvForceOn));
					CheckHr(qtisb->AppendRgch(vstuNames[istu].Chars(),
						vstuNames[istu].Length()));
					CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvOff));
				}
				CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
				sethvoRole.Delete(hvoRole);
				if (sethvoRole.Size())
					CheckHr(qtisb->AppendRgch(L"  ", 2));
			}
			Vector<StrUni> vstuRoles;
			Vector<HVO> vhvoRoles;
			Set<HVO>::iterator sit;
			// Sort the roles by name.
			for (sit = sethvoRole.Begin(); sit != sethvoRole.End(); ++sit)
			{
				hvoRole = sit->GetValue();
				int ipii = qpliRole->GetIndexFromId(hvoRole);
				if (ipii >= 0)
				{
					PossItemInfo * ppii = qpliRole->GetPssFromIndex(ipii);
					AssertPtr(ppii);
					ppii->GetName(stu, pntRole);
					int iv;
					int ivLim;
					for (iv = 0, ivLim = vstuRoles.Size(); iv < ivLim; )
					{
						int ivMid = (iv + ivLim) / 2;
						if (_wcsicmp(vstuRoles[ivMid].Chars(), stu.Chars()) < 0)
							iv = ivMid + 1;
						else
							ivLim = ivMid;
					}
					vstuRoles.Insert(iv, stu);
					vhvoRoles.Insert(iv, hvoRole);
				}
			}
			// Write the participants for each role.
			for (int irole = 0; irole < vstuRoles.Size(); ++irole)
			{
				hvoRole = vhvoRoles[irole];
				if (mmhvoRolehvoPerson.Retrieve(hvoRole, &mmit, &mmitLim))
				{
					if (irole)
						CheckHr(qtisb->AppendRgch(L"  ", 2));
					stu.Format(L"%s:%s", qfsp->m_stuFldName.Chars(), vstuRoles[irole].Chars());
					CheckHr(qtisb->SetStrPropValue(ktptFieldName, stu.Bstr()));
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, m_stuSubLabelFormat.Bstr()));
					CheckHr(qtisb->AppendRgch(vstuRoles[irole].Chars(),
						vstuRoles[irole].Length()));
					CheckHr(qtisb->AppendRgch(L":", 1));
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
					CheckHr(qtisb->AppendRgch(L" ", 1));
					for (vstuNames.Clear(); mmit != mmitLim; ++mmit)
					{
						hvoPerson = mmit->GetValue();
						int ipii = qpli->GetIndexFromId(hvoPerson);
						if (ipii >= 0)
						{
							PossItemInfo * ppii = qpli->GetPssFromIndex(ipii);
							AssertPtr(ppii);
							ppii->GetName(stu, pnt);
							int iv;
							int ivLim;
							for (iv = 0, ivLim = vstuNames.Size(); iv < ivLim; )
							{
								int ivMid = (iv + ivLim) / 2;
								if (_wcsicmp(vstuNames[ivMid].Chars(), stu.Chars()) < 0)
									iv = ivMid + 1;
								else
									ivLim = ivMid;
							}
							vstuNames.Insert(iv, stu);
						}
					}
					for (int istu = 0; istu < vstuNames.Size(); ++istu)
					{
						if (istu)
							CheckHr(qtisb->AppendRgch(L", ", 2));
						CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvForceOn));
						CheckHr(qtisb->AppendRgch(vstuNames[istu].Chars(),
							vstuNames[istu].Length()));
						CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvOff));
					}
					CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
				}
			}
		}
		stu.Assign(L".");
		CheckHr(qtisb->Append(stu.Bstr()));
		CheckHr(qtisb->GetString(pptss));
	}

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Obtain the string for the enumeration value stored by the given field.  This method
	should be overridden for the specific type of export.

	@param flid Id of field containing an enumeration value.
	@param itss Index of enumeration value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustomExport::GetEnumString(int flid, int itss, BSTR * pbstrName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrName);
	if (!flid || itss < 0)
		ThrowInternalError(E_INVALIDARG);

	StrUni stuEnum;
	switch (flid)
	{
	case kflidCmPerson_Gender:
		stuEnum.Load(kstidEnumGender);
		break;
	case kflidCmPerson_IsResearcher:
		stuEnum.Load(kstidEnumNoYes);
		break;
	default:
		return S_FALSE;
	}
	const wchar * pszEnum = stuEnum.Chars();
	const wchar * pszEnumLim = stuEnum.Chars() + stuEnum.Length();
	int itssTry = 0;
	while (pszEnum < pszEnumLim && itssTry <= itss)
	{
		const wchar * pszEnumNl = wcschr(pszEnum, '\n');
		if (!pszEnumNl)
			pszEnumNl = pszEnumLim;
		if (itss == itssTry)
		{
			StrUni stu(pszEnum, pszEnumNl - pszEnum);
			stu.GetBstr(pbstrName);
			return S_OK;
		}
		itssTry++;
		pszEnum = pszEnumNl + 1;
	}

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Build the start and end tags for the current record.  This method may return empty
	strings if the default of <Entry level="0">, <Entry level="1">, etc. is acceptable.

	@param nLevel (Indentation) level of the record (0 means top level, >0 means subrecord)
	@param hvo Database id of the current record (object).
	@param clid Database class id of the current record (object).
	@param pbstrStartTag Pointer to the output BSTR start tag.
	@param pbstrEndTag Pointer to the output BSTR end tag.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustomExport::BuildRecordTags(int nLevel, int hvo, int clid,
	BSTR * pbstrStartTag, BSTR * pbstrEndTag)
{
	BEGIN_COM_METHOD;

	StrUni stuStartTag;
	stuStartTag.Format(L"<Entry level=\"%d\"", nLevel);

	Assert(clid == kclidRnEvent || clid == kclidRnAnalysis);
	if (clid == kclidRnEvent)
		stuStartTag.FormatAppend(L" type=\"Event\"");
	else if (clid == kclidRnAnalysis)
		stuStartTag.FormatAppend(L" type=\"Analysis\"");

	StrUni stuDate;
	BuildDateCreatedString(kflidRnGenericRec_DateCreated, hvo, stuDate);
	if (stuDate.Length())
		stuStartTag.FormatAppend(L" dateCreated=\"%s\"", stuDate.Chars());

	stuStartTag.FormatAppend(L">%n");
	stuStartTag.GetBstr(pbstrStartTag);
	*pbstrEndTag = NULL;

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


// Handle explicit instantiation specific to this file.
#include "Set_i.cpp"
#include "MultiMap_i.cpp"

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mknb.bat"
// End: (These 4 lines are useful to Steve McConnel.)
