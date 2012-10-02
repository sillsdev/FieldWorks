/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgWritingSystemFactory.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)
#include <oledb.h>

#undef THIS_FILE
DEFINE_THIS_FILE

#undef TRACE_REFCOUNTS
//#define TRACE_REFCOUNTS

typedef long HVO;

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
// This class factory originally had ThreadingModel=Both, but with that we're getting problems
// when running C# tests in Nunit-GUI.
static GenericFactory g_fact(
	_T("SIL.Language.WritingSystemFactory"),
	&CLSID_LgWritingSystemFactory,
	_T("SIL language writing system factory"),
	_T("Apartment"),
	&LgWritingSystemFactory::CreateCom);

// TODO 1438 (KenZ): Should be thread-local if we go multithreaded.
// Store only one LgWritingSystemFactory per database: this should simplify interactions between
// different parts of the program.
ComVector<ILgWritingSystemFactory> LgWritingSystemFactory::g_vqwsf;

/*----------------------------------------------------------------------------------------------
	Called by the GenericFactory to "create" an ILgWritingSystemFactory.  It returns the global
	one associated with the registry.
----------------------------------------------------------------------------------------------*/
void LgWritingSystemFactory::CreateCom(IUnknown * punkCtl, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	StrUni stuNull;
	int iwsf;
	for (iwsf = 0; iwsf < g_vqwsf.Size(); ++iwsf)
	{
		LgWritingSystemFactory * pzwsf =
			dynamic_cast<LgWritingSystemFactory *>(g_vqwsf[iwsf].Ptr());
		AssertPtr(pzwsf);
		if (pzwsf->m_stuServer == stuNull && pzwsf->m_stuDatabase == stuNull)
		{
			CheckHr(g_vqwsf[iwsf]->QueryInterface(iid, ppv));
			return;
		}
	}
	// Not found, create a new one.
	LgWritingSystemFactoryPtr qzwsf;
	qzwsf.Attach(NewObj LgWritingSystemFactory);
	ILgWritingSystemFactory * pwsf = dynamic_cast<ILgWritingSystemFactory *>(qzwsf.Ptr());
	AssertPtr(pwsf);
	g_vqwsf.Push(pwsf);
	CheckHr(pwsf->QueryInterface(iid, ppv));
	// During Shutdown on WritingSystem, we normalize strings when we save new
	// encodings, so the ICU data directory must be initialized for that to work. Without
	// this here, TestLgWritingSystem::testIcuLocale failed on the build machine when
	// running the tests in release mode. I don't understand why it happened, but this
	// seems like a reasonable solution to make sure it will always work.
	StrUtil::InitIcuDataDir();
}

/*----------------------------------------------------------------------------------------------
	Called to "create" an ILgWritingSystemFactory associated with the given database.  It may
	return one that has already been created.
----------------------------------------------------------------------------------------------*/
void LgWritingSystemFactory::Create(IOleDbEncap * pode, IStream * pfistLog,
	ILgWritingSystemFactory ** ppwsf)
{
	AssertPtr(pode);
	AssertPtrN(pfistLog);
	AssertPtr(ppwsf);
	Assert(!*ppwsf);

	SmartBstr sbstrServer;
	SmartBstr sbstrDatabase;
	CheckHr(pode->get_Server(&sbstrServer));
	CheckHr(pode->get_Database(&sbstrDatabase));

	int cchServer = sbstrServer.Length();
	int cchDatabase = sbstrDatabase.Length();
	int iwsf;
	for (iwsf = 0; iwsf < g_vqwsf.Size(); ++iwsf)
	{
		LgWritingSystemFactory * pzwsf =
			dynamic_cast<LgWritingSystemFactory *>(g_vqwsf[iwsf].Ptr());
		AssertPtr(pzwsf);
		if (pzwsf->m_stuServer.EqualsCI(sbstrServer, cchServer) &&
			pzwsf->m_stuDatabase.EqualsCI(sbstrDatabase, cchDatabase))
		{
			*ppwsf = g_vqwsf[iwsf].Ptr();
			AssertPtr(*ppwsf);
			pzwsf->m_nOdeShareCount++;
			(*ppwsf)->AddRef();
			return;
		}
	}
	// Not found, create a new one.
	LgWritingSystemFactoryPtr qzwsf;
	qzwsf.Attach(NewObj LgWritingSystemFactory);
	CheckHr(qzwsf->UseDatabase(pode, pfistLog));
	*ppwsf = dynamic_cast<ILgWritingSystemFactory *>(qzwsf.Ptr());
	AssertPtr(*ppwsf);
	g_vqwsf.Push(*ppwsf);
	qzwsf->m_nOdeShareCount++;
	(*ppwsf)->AddRef();
	// During Shutdown on WritingSystem, we normalize strings when we save new
	// encodings, so the ICU data directory must be initialized for that to work. Without
	// this here, TestLgWritingSystem::testIcuLocale failed on the build machine when
	// running the tests in release mode. I don't understand why it happened, but this
	// seems like a reasonable solution to make sure it will always work.
	StrUtil::InitIcuDataDir();
}

/*----------------------------------------------------------------------------------------------
	Called to create an ILgWritingSystemFactory from the data in the given IStorage.  The
	created factory should be treated as read-only, with no relation to any database or the
	registry.
----------------------------------------------------------------------------------------------*/
void LgWritingSystemFactory::Deserialize(IStorage * pstg, ILgWritingSystemFactory ** ppwsf)
{
	AssertPtr(pstg);
	AssertPtr(ppwsf);
	Assert(!*ppwsf);

	LgWritingSystemFactoryPtr qzwsf;
	qzwsf.Attach(NewObj LgWritingSystemFactory);
	qzwsf->m_stuServer.Assign(L"Deserialized");

	ILgWritingSystemFactoryPtr qwsf;
	qwsf.Attach(qzwsf.Detach());
	// This writing system factory is only used to deserialize a writing system factory
	// and should not be used with InstallLanguage (which should only be used with the
	// target writing system factory.)
	qwsf->put_BypassInstall(TRUE);

	IStreamPtr qstrmWss;
	CheckHr(pstg->OpenStream(L"Wss", NULL, STGM_READ | STGM_SHARE_EXCLUSIVE, 0,
		&qstrmWss));
	if (!qstrmWss)
		ThrowHr(E_UNEXPECTED);
	int cws;
	Vector<int> vws;
	Vector<StrUni> vstuWs;
	Vector<OLECHAR> vch;
	int cch;
	StrUni stu;
	int ws;
	int iws;
	ULONG cb = isizeof(cws);
	ULONG cbRead;

	// 1. Read the number of writing systems stored.
	CheckHr(qstrmWss->Read(&cws, cb, &cbRead));
	if (cb != cbRead)
		ThrowHr(E_UNEXPECTED);

	// 2. For each writing system, read its original id code and its ICU Locale name.
	for (iws = 0; iws < cws; ++iws)
	{
		cb = isizeof(int);
		CheckHr(qstrmWss->Read(&ws, cb, &cbRead));
		if (cb != cbRead)
			ThrowHr(E_UNEXPECTED);
		CheckHr(qstrmWss->Read(&cch, cb, &cbRead));
		if (cb != cbRead)
			ThrowHr(E_UNEXPECTED);
		vch.Resize(cch);
		cb = cch * isizeof(OLECHAR);
		CheckHr(qstrmWss->Read(vch.Begin(), cb, &cbRead));
		if (cb != cbRead)
			ThrowHr(E_UNEXPECTED);
		stu.Assign(vch.Begin(), vch.Size());

		vws.Push(ws);
		vstuWs.Push(stu);
	}

	// 3. For each writing system, deserialize its IStorage object, and add it to the new
	//    factory.
	IStoragePtr qstgEnc;
	for (iws = 0; iws < cws; ++iws)
	{
		stu.Format(L"%s.%d", vstuWs[iws].Chars(), vws[iws]);
		CheckHr(pstg->OpenStorage(stu.Chars(), NULL, STGM_READ | STGM_SHARE_EXCLUSIVE, NULL, 0,
			&qstgEnc));
		if (!qstgEnc)
			ThrowHr(E_UNEXPECTED);
		WritingSystemPtr qzws;
		WritingSystem::Create(qwsf, &qzws);
		CheckHr(qzws->Deserialize(qstgEnc));
		CheckHr(qzws->put_Dirty(FALSE));
		CheckHr(qwsf->AddEngine(qzws));
	}

	g_vqwsf.Push(qwsf);
	*ppwsf = qwsf.Detach();
	AssertPtr(*ppwsf);
}

/*----------------------------------------------------------------------------------------------
	This static method is called by the shutdown notifier object.
----------------------------------------------------------------------------------------------*/
void LgWritingSystemFactory::ShutdownIfActive()
{
	while (g_vqwsf.Size())
	{
		dynamic_cast<LgWritingSystemFactory *>(g_vqwsf[0].Ptr())->m_nOdeShareCount = 1; // force Shutdown to really happen.
		g_vqwsf[0]->Shutdown();	// This will save to the database or registry if needed.
		g_vqwsf.Delete(0);
	}
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgWritingSystemFactory::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgWritingSystemFactory *>(this));
	else if (riid == IID_ILgWritingSystemFactory)
		*ppv = static_cast<ILgWritingSystemFactory *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ILgWritingSystemFactory);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Add a reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) LgWritingSystemFactory::AddRef()
{
	ulong cref = ::InterlockedIncrement(&m_cref);
#ifdef TRACE_REFCOUNTS
	StrApp str;
	StrApp strFmt = "AddRef LgWritingSystemFactory: %d, cref=%d\n";
	str.Format(strFmt, (int) this, cref);
	OutputDebugString(str.Chars());
#endif
	return cref;
}

/*----------------------------------------------------------------------------------------------
	Subtract a reference count, deleting the object when we get to zero.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) LgWritingSystemFactory::Release()
{
	ulong cref = ::InterlockedDecrement(&m_cref);
#ifdef TRACE_REFCOUNTS
	StrApp str;
	StrApp strFmt = "Release LgWritingSystemFactory: %d, cref=%d\n";
	str.Format(strFmt, (int) this, cref);
	OutputDebugString(str.Chars());
#endif
	if (!cref)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}

//:>********************************************************************************************
//:>	   ILgWritingSystemFactory Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get the actual writing system engine object for a given code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_Engine(BSTR bstrIcuLocale, IWritingSystem ** ppwseng)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrIcuLocale);
	ChkComOutPtr(ppwseng);

	StrUni stu(bstrIcuLocale);
	int ws;
	if (m_hmwsId.Retrieve(stu, &ws))
		return GetEngine(ws, ppwseng);
	// Someone (another program) could have changed the writing systems behind our back, so
	// check with the database for the current state and try again if necessary.  See LT-8718.
	if (RefreshMapsIfNeeded() && m_hmwsId.Retrieve(stu, &ws))
		return GetEngine(ws, ppwseng);
	else
		return CreateEngine(bstrIcuLocale, ppwseng);

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Get the actual writing system engine object for a given code.  Return NULL if such a
	'writing system' does not exist.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_EngineOrNull(int ws, IWritingSystem ** ppwsobj)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppwsobj);

	return GetEngine(ws, ppwsobj);

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	These next 2 functions should go in a utility library somewhere.
----------------------------------------------------------------------------------------------*/
void ReadTsString(IOleDbCommand * podc, int icol, int wsDef, ITsString ** pptss)
{
	Vector<OLECHAR> vchTxt;
	Vector<byte> vbFmt;
	int cbFmt = 0;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	vchTxt.Resize(512);
	CheckHr(podc->GetColValue(icol, reinterpret_cast <BYTE *>(vchTxt.Begin()),
		sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken, &fIsNull, 0));
	if (!fIsNull)
	{
		int cchTxt = cbSpaceTaken / isizeof(OLECHAR);
		if (cchTxt > vchTxt.Size())
		{
			vchTxt.Resize(cchTxt, true);
			CheckHr(podc->GetColValue(icol, reinterpret_cast <BYTE *>(vchTxt.Begin()),
				sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken, &fIsNull, 0));
		}
		vbFmt.Resize(512);
		CheckHr(podc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(vbFmt.Begin()),
			vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
		if (!fIsNull)
		{
			cbFmt = cbSpaceTaken;
			if (cbFmt > vbFmt.Size())
			{
				vbFmt.Resize(cbFmt, true);
				CheckHr(podc->GetColValue(icol + 1,
					reinterpret_cast <BYTE *>(vbFmt.Begin()), vbFmt.Size(),
					&cbSpaceTaken, &fIsNull, 0));
			}
		}
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		AssertPtr(qtsf);
		if (cbFmt)
		{
			CheckHr(qtsf->DeserializeStringRgch(vchTxt.Begin(), &cchTxt,
						vbFmt.Begin(), &cbFmt, pptss));
		}
		else
		{
			CheckHr(qtsf->MakeStringRgch(vchTxt.Begin(), cchTxt, wsDef, pptss));
		}
	}
}



/*----------------------------------------------------------------------------------------------
	Read a Unicode type value from the database query column into the given StrUni.
----------------------------------------------------------------------------------------------*/
void ReadUnicodeString(IOleDbCommand * podc, int icol, StrUni & stu)
{
	Vector<OLECHAR> vchTxt;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	vchTxt.Resize(512);
	CheckHr(podc->GetColValue(icol, reinterpret_cast <BYTE *>(vchTxt.Begin()),
		sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken, &fIsNull, 0));
	if (!fIsNull)
	{
		int cchTxt = cbSpaceTaken / isizeof(OLECHAR);
		if (cchTxt > vchTxt.Size())
		{
			vchTxt.Resize(cchTxt, true);
			CheckHr(podc->GetColValue(icol, reinterpret_cast <BYTE *>(vchTxt.Begin()),
				sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken, &fIsNull, 0));
		}
		stu.Assign(vchTxt.Begin(), cchTxt);
	}
	else
	{
		stu.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Implementation method for get_Engine, get_EngineOrNull, and AddWritingSystem.
----------------------------------------------------------------------------------------------*/
HRESULT LgWritingSystemFactory::GetEngine(int ws, IWritingSystem ** ppwsobj)
{
	AssertPtr(ppwsobj);
	*ppwsobj = NULL;

	IWritingSystemPtr qwsobj;
	if (m_hmnwsobj.Retrieve(ws, qwsobj))
	{
		*ppwsobj = qwsobj.Detach();
		return S_OK;
	}

	if (m_stuDatabase.Length())
	{
		AssertPtr(m_qode.Ptr());

		// Try to read stored information for this writing system from the database.
		bool fWsExists = false;
		bool fHaveModTime = false;
		WritingSystemInfo wsi;
		ComBool fIsNull;
		IOleDbCommandPtr qodc;
		try
		{
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			StrUni stuCmd;

			CheckHr(m_qode->CreateCommand(&qodc));

			// First, get the most basic information about the desired writing system.
			// Ignoring these fields for now:
			//	  KeyboardType (Unicode)
			stuCmd.Format(L"select Locale, DefaultMonospace, DefaultSansSerif, DefaultBodyFont, DefaultSerif,"
				L" FontVariation, SansFontVariation, BodyFontFeatures, RightToLeft, ICULocale, SpellCheckDictionary,"
				L" LegacyMapping, ValidChars, MatchedPairs, PunctuationPatterns, CapitalizationInfo, "
				L" QuotationMarks, KeymanKeyboard, LastModified"
				L" from LgWritingSystem where [Id] = %d",
				ws);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fMoreRows)
			{
				fWsExists = true;
				wsi.m_hvo = ws;
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&wsi.m_nLocale),
					isizeof(wsi.m_nLocale), &cbSpaceTaken, &fIsNull, 0));
				if (fIsNull)
					wsi.m_nLocale = 0;
				ReadUnicodeString(qodc, 2, wsi.m_stuDefMono);
				ReadUnicodeString(qodc, 3, wsi.m_stuDefSS);
				ReadUnicodeString(qodc, 4, wsi.m_stuDefBodyFont);
				ReadUnicodeString(qodc, 5, wsi.m_stuDefSerif);
				ReadUnicodeString(qodc, 6, wsi.m_stuFontVar);
				ReadUnicodeString(qodc, 7, wsi.m_stuSansFontVar);
				ReadUnicodeString(qodc, 8, wsi.m_stuBodyFontFeatures);
				wsi.m_fRightToLeft = false;
				CheckHr(qodc->GetColValue(9, reinterpret_cast <BYTE *>(&wsi.m_fRightToLeft),
					isizeof(wsi.m_fRightToLeft), &cbSpaceTaken, &fIsNull, 0));
				if (fIsNull)
					wsi.m_fRightToLeft = false;
				ReadUnicodeString(qodc, 10, wsi.m_stuIcuLocale);
				ReadUnicodeString(qodc, 11, wsi.m_stuSpellCheckDictionary);
				ReadUnicodeString(qodc, 12, wsi.m_stuLegacyMapping);
				ReadUnicodeString(qodc, 13, wsi.m_stuValidChars);
				ReadUnicodeString(qodc, 14, wsi.m_stuMatchedPairs);
				ReadUnicodeString(qodc, 15, wsi.m_stuPunctuationPatterns);
				ReadUnicodeString(qodc, 16, wsi.m_stuCapitalizationInfo);
				ReadUnicodeString(qodc, 17, wsi.m_stuQuotationMarks);
				ReadUnicodeString(qodc, 18, wsi.m_stuKeymanKeyboard);
				DBTIMESTAMP dbts;
				CheckHr(qodc->GetColValue(19, reinterpret_cast <BYTE *>(&dbts), sizeof(dbts),
					&cbSpaceTaken, &fIsNull, 0));
				if (fIsNull)
				{
					memset(&dbts, sizeof(dbts), 0);
					fHaveModTime = false;
				}
				else
				{
					wsi.m_stModified.wYear = dbts.year;
					wsi.m_stModified.wMonth = dbts.month;
					wsi.m_stModified.wDayOfWeek = 0;
					wsi.m_stModified.wDay = dbts.day;
					wsi.m_stModified.wHour = dbts.hour;
					wsi.m_stModified.wMinute = dbts.minute;
					wsi.m_stModified.wSecond = dbts.second;
					wsi.m_stModified.wMilliseconds = 0;
					fHaveModTime = true;
				}
			}
			if (fWsExists)
			{
				// Get multilingual Name, Abbr, and Description info.
				stuCmd.Format(L"SELECT "
						L"wsn.Ws, "
						L"wsn.Txt AS WsName, "
						L"wsa.Txt AS WsAbbr, "
						L"ms.Txt AS WsDesc, "
						L"ms.Fmt AS WsDescFmt "
					L"FROM LgWritingSystem ws "
					L"LEFT OUTER JOIN LgWritingSystem_Name wsn ON "
						L"wsn.Obj = ws.[Id] "
					L"LEFT OUTER JOIN LgWritingSystem_Abbr wsa ON "
						L"wsa.Obj = ws.[Id] AND wsa.Ws = wsn.Ws "
					L"LEFT OUTER JOIN MultiStr$ ms ON "
						L"ms.Obj = ws.[Id] AND ms.Ws = wsn.Ws AND ms.WS = wsa.WS AND ms.Flid = %d "
					L"WHERE ws.[Id] = %d ",
					kflidLgWritingSystem_Description, wsi.m_hvo);

				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));

				int wsT;
				StrUni stuT;
				StrUni stuName;
				StrUni stuAbbr;

				while (fMoreRows)
				{
					CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&wsT),
						isizeof(wsT), &cbSpaceTaken, &fIsNull, 0));
					if (fIsNull || wsT == 0)
						break;

					ReadUnicodeString(qodc, 2, stuName);
					if (stuName.Length())
						wsi.m_hmwsstuName.Insert(wsT, stuName, true);

					ReadUnicodeString(qodc, 3, stuAbbr);
					if (stuAbbr.Length())
						wsi.m_hmwsstuAbbr.Insert(wsT, stuAbbr, true);

					ITsStringPtr qtss;
					ReadTsString(qodc, 4, wsT, &qtss);
					if (qtss)
						wsi.m_hmwsqtssDesc.Insert(wsT, qtss, true);
					CheckHr(qodc->NextRow(&fMoreRows));
				}

				// Get the basic collation information for the writing system.

				stuCmd.Format(L"SELECT c.Id,c.WinLCID,c.WinCollation,"
					L"c.IcuResourceName,c.IcuResourceText,c.ICURules%n"
					L"from LgCollation c%n"
					L"join LgWritingSystem_Collations wc on wc.Dst = c.Id%n"
					L"where wc.Src = %d%n"
					L"order by wc.Ord",
					wsi.m_hvo);
				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				while (fMoreRows)
				{
					CollationInfo ci;
					CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&ci.m_hvo),
						isizeof(ci.m_hvo), &cbSpaceTaken, &fIsNull, 0));
					if (fIsNull)
						ci.m_hvo = 0;
					CheckHr(qodc->GetColValue(2,
						reinterpret_cast <BYTE *>(&ci.m_nWinLCID),
						isizeof(ci.m_nWinLCID), &cbSpaceTaken, &fIsNull, 0));
					if (fIsNull)
						ci.m_nWinLCID = 0;
					ReadUnicodeString(qodc, 3, ci.m_stuWinCollation);
					ReadUnicodeString(qodc, 4, ci.m_stuIcuResourceName);
					ReadUnicodeString(qodc, 5, ci.m_stuIcuResourceText);
					ReadUnicodeString(qodc, 6, ci.m_stuIcuRules);
					if (ci.m_hvo)
						wsi.m_vci.Push(ci);
					CheckHr(qodc->NextRow(&fMoreRows));
				}

				// Get the multilingual names for the collations.

				stuCmd.Format(L"SELECT Obj, Ws, Txt%n"
					L"FROM LgCollation_Name cn%n"
					L"JOIN LgWritingSystem_Collations wsc ON wsc.Dst = cn.Obj%n"
					L"WHERE Src = %d", wsi.m_hvo);

				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				while (fMoreRows)
				{
					HVO hvoT;
					CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoT),
						isizeof(hvoT), &cbSpaceTaken, &fIsNull, 0));
					bool fFound = false;
					for (int ici = 0; ici < wsi.m_vci.Size(); ++ici)
					{
						if (wsi.m_vci[ici].m_hvo == hvoT)
						{
							CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(&wsT),
								isizeof(wsT), &cbSpaceTaken, &fIsNull, 0));
							ReadUnicodeString(qodc, 3, stuT);
							if (!fIsNull && wsT && stuT.Length())
							{
								wsi.m_vci[ici].m_hmwsstuName.Insert(wsT,
									stuT, true);
							}
							fFound = true;
							break;
						}
					}
					CheckHr(qodc->NextRow(&fMoreRows));
				}
			}
		}
		catch (...)
		{
			// An error probably just means we failed to read the writing system, possibly
			// because it doesn't exist.
			fWsExists = false;
		}
		qodc.Clear();
		if (fWsExists)
		{
			// Check whether we have an empty definition (just the ICU locale and hvo).
			bool fUndefined = true;
			if (wsi.m_nLocale)
				fUndefined = false;
			else if (wsi.m_fRightToLeft)
				fUndefined = false;
			else if (wsi.m_hmwsstuName.Size())
				fUndefined = false;
			else if (wsi.m_hmwsstuAbbr.Size())
				fUndefined = false;
			else if (wsi.m_hmwsqtssDesc.Size())
				fUndefined = false;
			else if (wsi.m_vci.Size())
				fUndefined = false;
			else if (wsi.m_stuDefSerif.Length())
				fUndefined = false;
			else if (wsi.m_stuDefSS.Length())
				fUndefined = false;
			else if (wsi.m_stuDefMono.Length())
				fUndefined = false;
			else if (wsi.m_stuFontVar.Length())
				fUndefined = false;
			else if (wsi.m_stuSansFontVar.Length())
				fUndefined = false;
			else if (wsi.m_stModified.wYear != 0)
				fUndefined = false;

			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			AssertPtr(qtsf);
			ITsStringPtr qtss;
			// Create the WritingSystem object and set its writing system.
			WritingSystemPtr qzws;
			WritingSystem::Create(this, &qzws);
			qzws->SetHvo(ws);
			// Set its ICU locale.
			Assert(wsi.m_stuIcuLocale.Length());
			qzws->put_IcuLocale(wsi.m_stuIcuLocale.Bstr());
			qzws->m_fHaveModTime = fHaveModTime;
			qzws->m_stModified = wsi.m_stModified;
			qzws->m_fNewFile = true;

			// TE-8606 Determine if we are currently on the local machine using SQL Server. If not,
			// don't update the writing systems in the database.
			bool fIsRemote = DetermineRemoteUser();

			// Before going any further, check the modified time against an existing XML file in
			// the Languages directory.  This also takes care of initializing writing systems
			// that have been created without a prior database definition, since they will have
			// a null "last modified" time.
			StrUni stuFile;
			SYSTEMTIME stFileMod;
			::GetSystemTime(&stFileMod);
			Assert(qzws->m_stuIcuLocale.Length());
			qzws->GetLanguageFileName(stuFile);
			HANDLE hFile = ::CreateFileW(stuFile.Chars(), GENERIC_READ, FILE_SHARE_READ, NULL,
				OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
			if (hFile != INVALID_HANDLE_VALUE)
			{
				FILETIME ftModified;
				BOOL fT;
				fT = ::GetFileTime(hFile, NULL, NULL, &ftModified);
				Assert(fT);
				INT fTimeConverted;
				fTimeConverted = ::FileTimeToSystemTime(&ftModified, &stFileMod);
				Assert(fTimeConverted);
				::CloseHandle(hFile);
				stFileMod.wMilliseconds = 0; // to match : wsi.m_stModified.wMilliseconds = 0;

				bool fUseFile = false;
				// Allow times to differ by 15 seconds.
				fUseFile = !SilUtil::CompareTimesWithinXXSeconds(stFileMod, wsi.m_stModified, 15);
				if (fUseFile && !fIsRemote)
				{
					qzws->m_stModified = stFileMod;
					if (LoadWsFromFile(qzws))		// Should always succeed.
					{
						*ppwsobj = qzws.Detach();
						return S_OK;
					}
					qzws->m_fHaveModTime = true;
				}
				qzws->m_fNewFile = false;
			}
			else if (fUndefined)
			{
				stuFile.Assign(DirectoryFinder::FwRootCodeDir());
				stuFile.FormatAppend(L"\\Templates\\%s.xml", wsi.m_stuIcuLocale.Chars());
				hFile = ::CreateFileW(stuFile.Chars(), GENERIC_READ, FILE_SHARE_READ, NULL,
					OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
				if (hFile != INVALID_HANDLE_VALUE)
				{
					::CloseHandle(hFile);
					if (LoadWsFromFile(qzws))	// will find the alternate location by itself.
					{
						*ppwsobj = qzws.Detach();
						return S_OK;
					}
				}
			}

			if (wsi.m_stuKeymanKeyboard)
				qzws->put_KeymanKbdName(wsi.m_stuKeymanKeyboard.Bstr());
			// Set its locale (if defined).
			if (wsi.m_nLocale)
				qzws->put_Locale(wsi.m_nLocale);

			// Set its Legacy Mapping (if defined).
			if (wsi.m_stuLegacyMapping)
				qzws->put_LegacyMapping(wsi.m_stuLegacyMapping.Bstr());

			// Set the Right-To-Left flag.
			if (wsi.m_fRightToLeft)
			{
				ComBool fRTL = wsi.m_fRightToLeft;
				qzws->put_RightToLeft(fRTL);
			}

			// Set the multilingual name(s) (if any have been defined).
			if (wsi.m_hmwsstuName.Size())
			{
				HashMap<int, StrUni>::iterator it;
				for (it = wsi.m_hmwsstuName.Begin(); it != wsi.m_hmwsstuName.End(); ++it)
					qzws->put_Name(it.GetKey(), it.GetValue().Bstr());
			}
			// Set the multilingual abbreviation(s) (if any have been defined).
			if (wsi.m_hmwsstuAbbr.Size())
			{
				HashMap<int, StrUni>::iterator it;
				for (it = wsi.m_hmwsstuAbbr.Begin(); it != wsi.m_hmwsstuAbbr.End(); ++it)
					qzws->put_Abbr(it.GetKey(), it.GetValue().Bstr());
			}
			// Set the multilingual description(s) (if any have been defined).
			if (wsi.m_hmwsqtssDesc.Size())
			{
				ComHashMap<int, ITsString>::iterator it;
				for (it = wsi.m_hmwsqtssDesc.Begin(); it != wsi.m_hmwsqtssDesc.End(); ++it)
					qzws->put_Description(it.GetKey(), it.GetValue());
			}
			if (wsi.m_vci.Size())
			{
				// Set the defined collations.
				for (int ici = 0; ici < wsi.m_vci.Size(); ++ici)
				{
					CollationInfo & ci = wsi.m_vci[ici];
					CollationPtr qzcoll;
					qzcoll.Attach(NewObj Collation);
					if (!qzcoll)
						ThrowHr(WarnHr(E_OUTOFMEMORY));
					qzcoll->SetHvo(ci.m_hvo);
					if (ci.m_nWinLCID)
						CheckHr(qzcoll->put_WinLCID(ci.m_nWinLCID));
					if (ci.m_stuWinCollation.Length())
						CheckHr(qzcoll->put_WinCollation(ci.m_stuWinCollation.Bstr()));
					if (ci.m_stuIcuResourceName.Length())
						CheckHr(
							qzcoll->put_IcuResourceName(ci.m_stuIcuResourceName.Bstr()));
					if (ci.m_stuIcuResourceText.Length())
						CheckHr(
							qzcoll->put_IcuResourceText(ci.m_stuIcuResourceText.Bstr()));
					if (ci.m_stuIcuRules.Length())
						CheckHr(
							qzcoll->put_IcuRules(ci.m_stuIcuRules.Bstr()));
					// Set the multilingual name(s) (if any have been defined).
					HashMap<int, StrUni>::iterator it;
					for (it = ci.m_hmwsstuName.Begin();
							it != ci.m_hmwsstuName.End();
							++it)
					{
						qzcoll->put_Name(it.GetKey(), it.GetValue().Bstr());
					}
					CheckHr(qzcoll->put_Dirty(FALSE));
					// Now, add the collation object to the writing system object!
					CheckHr(qzws->putref_Collation(ici, qzcoll));
				}
			}

			// Set Valid Characters, Matched Pairs and Punctuation Patterns,
			// capitalization info. and quotation marks.
			qzws->put_ValidChars(wsi.m_stuValidChars.Bstr());
			qzws->put_MatchedPairs(wsi.m_stuMatchedPairs.Bstr());
			qzws->put_PunctuationPatterns(wsi.m_stuPunctuationPatterns.Bstr());
			qzws->put_CapitalizationInfo(wsi.m_stuCapitalizationInfo.Bstr());
			qzws->put_QuotationMarks(wsi.m_stuQuotationMarks.Bstr());

			// Set font info.
			qzws->put_DefaultSerif(wsi.m_stuDefSerif.Bstr());
			qzws->put_DefaultSansSerif(wsi.m_stuDefSS.Bstr());
			qzws->put_DefaultBodyFont(wsi.m_stuDefBodyFont.Bstr());
			qzws->put_DefaultMonospace(wsi.m_stuDefMono.Bstr());
			qzws->put_FontVariation(wsi.m_stuFontVar.Bstr());
			qzws->put_SansFontVariation(wsi.m_stuSansFontVar.Bstr());
			qzws->put_BodyFontFeatures(wsi.m_stuBodyFontFeatures.Bstr());
			qzws->put_SpellCheckDictionary(wsi.m_stuSpellCheckDictionary.Bstr());

			// Install the writing system, in case anyone else wants it!
			// TE-8606 Make the writing system dirty to force writing out to the XML file
			// when it's older than the database to ensure we get the server version
			bool fOutOfDate = !SilUtil::CompareTimesWithinXXSeconds(stFileMod, wsi.m_stModified, 15);
			qzws->put_Dirty(fOutOfDate && fIsRemote);
			HRESULT hr;
			IgnoreHr(hr = AddEngine(qzws));
			if (FAILED(hr))
			{
				// Tell the user gently that we have problems, rather than the standard
				// Assert/Green dialog stack dump message.  This can happen when a user
				// with low privileges tries to open a database with writing systems that
				// are not already installed on the system.  (See LT-4095.)
				StrApp strFmt(kstidLangDefXmlMsg015);
				StrApp strMsg;
				StrApp strTitle(kstidLangDefXmlMsg014);
				SmartBstr sbstrWs;
				int wsUi;
				get_UserWs(&wsUi);
				qzws->get_UiName(wsUi, &sbstrWs);
				StrApp strUiName(sbstrWs.Chars());
				strMsg.Format(strFmt.Chars(), strUiName.Chars());
				::MessageBox(NULL, strMsg.Chars(), strTitle.Chars(), MB_ICONINFORMATION);
			}
			// See above, write the dirty file out.
			qzws->SaveIfDirty(m_qode);
			*ppwsobj = qzws.Detach();
			return S_OK;
		}
		else
		{
		}
	}
	else
	{
		// Non-database based factory: we shouldn't get here.
	}

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Implementation method for get_Engine.
----------------------------------------------------------------------------------------------*/
HRESULT LgWritingSystemFactory::CreateEngine(BSTR bstrIcuLocale, IWritingSystem ** ppwsobj)
{
	// Create a default writing system
	WritingSystemPtr qzws;
	WritingSystem::Create(this, &qzws);

	// Add this writing system to the database, so that we have its HVO.
	int hvoWs = GetNewHvoWs();
	if (!hvoWs)
		return E_FAIL;
	// Set its writing system and ICU Locale.
	qzws->SetHvo(hvoWs);
	CheckHr(qzws->put_IcuLocale(bstrIcuLocale));
	bool fInstalled = false;
	try
	{
		fInstalled = LoadWsFromFile(qzws);
	}
	catch (...)
	{
	}
	if (!fInstalled)
	{
		// LoadWsFromFile failed -- use what we have.  It should be in a usable state.
		bool fSave = m_fBypassInstall;
		m_fBypassInstall = true;
		CheckHr(qzws->put_Dirty(TRUE));

		qzws->m_fHaveModTime = false;
		qzws->m_fNewFile = true;

		CheckHr(AddEngine(qzws));
		m_fBypassInstall = fSave;
	}
	*ppwsobj = qzws.Detach();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Put (make available) a new writing system (it will be registered under its own
	proper writing system code).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::AddEngine(IWritingSystem * pwseng)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pwseng);

	int ws;
	SmartBstr sbstrLocale;
	CheckHr(pwseng->get_WritingSystem(&ws));
	CheckHr(pwseng->get_IcuLocale(&sbstrLocale));

	// Perform some sanity checks on ws and locale.
	if (!sbstrLocale.Length())
		ThrowHr(WarnHr(E_INVALIDARG));

	if (!ws)
	{
		ws = GetNewHvoWs();
		if (!ws)
			return E_FAIL;
		WritingSystemPtr qzws;
		CheckHr(pwseng->QueryInterface(CLSID_WritingSystem, (void **)&qzws));
		qzws->SetHvo(ws);
	}
	Assert(ws);

	StrUni stuLocale(sbstrLocale.Chars());
	IWritingSystemPtr qwsOld;
	StrUni stuOld;
	int wsOld;
	bool fHasLocale = m_hmwsId.Retrieve(stuLocale, &wsOld);
	if (fHasLocale && ws != wsOld)
	{
		ThrowHr(WarnHr(E_INVALIDARG));
	}
	bool fHasOldObj = m_hmnwsobj.Retrieve(ws, qwsOld);
	bool fHasOldLocale = m_hmwsLocale.Retrieve(ws, &stuOld);
	bool fHasOldId = m_hmwsId.Retrieve(stuOld, &wsOld);
	if (m_setws.IsMember(ws))
	{
		if (!fHasOldLocale)
		{
			Assert(fHasOldLocale);				// Confused internal state!
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		if (!fHasOldId)
		{
			Assert(fHasOldId);					// Confused internal state!
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		if (wsOld != ws)
		{
			Assert(ws == wsOld);				// Confused internal state!
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		if (stuOld != stuLocale)
		{
			Assert(stuOld == stuLocale);		// Confused internal state!
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		if (pwseng == qwsOld.Ptr())
		{
			// Hash maps all return the proper values, so nothing to do.
			return S_OK;
		}
	}
	else
	{
		if (fHasOldObj)
		{
			Assert(!fHasOldObj);				// Confused internal state!
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		if (fHasOldLocale)
		{
			Assert(!fHasOldLocale);				// Confused internal state!
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		if (fHasOldId)
		{
			Assert(!fHasOldId);					// Confused internal state!
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
	}

	// Get the lcid to use in default collations/collating engines.  Use US English if we can't
	// figure out anything else.
	int lcid;
	CheckHr(pwseng->get_Locale(&lcid));
	if (!lcid)
	{
		// Get the lcid from ICU if we can.
		StrUtil::InitIcuDataDir();		// just in case...
		StrAnsi staIcuLocale(sbstrLocale.Chars());
		Locale loc(staIcuLocale.Chars());
		lcid = loc.getLCID();
		if (!lcid)
		{
			// Ugh.  Use US English and hope for the best.
			lcid = LANG_ENGLISH;
		}
		if (lcid < 0x400)
		{
			// It's a 'primary language ID', a very common case we get from any
			// language name that doesn't have an underscore, like "en"!
			// Windows won't accept this as a langid or lcid, so fix it into a valid one.
			lcid = MAKELCID(MAKELANGID(lcid, SUBLANG_DEFAULT), SORT_DEFAULT);
		}
		CheckHr(pwseng->put_Locale(lcid));

		// The next lines are needed in case another program using ICU wants to update a
		// language definition.  For example, starting up WorldPad with nothing in the
		// {FW}/Languages directory, opening Welcome.wpx, and then futilely trying to start up
		// Data Notebook on Kalaba (TestLangProj).
		// Get rid of any memory-mapping of files by ICU, and reinitialize ICU.
		IIcuCleanupManagerPtr qicln;
		qicln.CreateInstance(CLSID_IcuCleanupManager);
		CheckHr(qicln->Cleanup());
	}

	// Install a default collation if none have been defined.
	int ccoll;
	CheckHr(pwseng->get_CollationCount(&ccoll));
	if (!ccoll)
	{
		// Install a default Windows style collation.
		CollationPtr qzcoll;
		CreateLgCollation(&qzcoll, pwseng);
		CheckHr(qzcoll->put_WinLCID(lcid));
		StrUni stu(L"Latin1_General_CI_AI");
		CheckHr(qzcoll->put_WinCollation(stu.Bstr()));
		stu.Load(kstidLangDefaultCollation);
		int ws = GetDefaultWsCode();
		CheckHr(qzcoll->put_Name(ws, stu.Bstr()));
		// qzcoll is automatically set DIRTY by the methods used above.
		CheckHr(pwseng->put_Dirty(TRUE));
		// Now, add the collation object to the writing system object!
		CheckHr(pwseng->putref_Collation(0, qzcoll));
	}

	// Make a "collating engine" (not to be confused with a "collation") and install it in the
	// writing system.
	// TODO: should we base this on the first collation instead of building an ICU Collator based on the locale?
	StrUtil::InitIcuDataDir();
	ILgCollatingEnginePtr qcol;
	LgIcuCollator::CreateCom(NULL, IID_ILgCollatingEngine, (void **)&qcol);
	if (!qcol)
		ThrowHr(WarnHr(E_UNEXPECTED));
	CheckHr(qcol->Open(sbstrLocale));
	CheckHr(qcol->putref_WritingSystemFactory(this));
	WritingSystem * pzws = dynamic_cast<WritingSystem *>(pwseng);
	AssertPtr(pzws);
	pzws->SetCollater(qcol);

	// Nb do this BEFORE InstallLanguage...various components need to know the
	// writing system factory before possibly writing themselves out as part of
	// InstallLanguage.
	pwseng->putref_WritingSystemFactory(this);

	// Create a language definition file for this writing system if it doesn't exist.
	CheckHr(pwseng->InstallLanguage(false));

	// Register the writing system. It is permitted to replace an existing one.
	m_hmnwsobj.Insert(ws, pwseng, true);
	m_setws.Insert(ws);
	m_hmwsLocale.Insert(ws, stuLocale, true);
	m_hmwsId.Insert(stuLocale, ws, true);
	if (fHasOldLocale && stuOld != stuLocale)
		m_hmwsId.Delete(stuOld);		// Remove obsolete cruft.

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Remove any runs in the given ws from the description strings of pws.
----------------------------------------------------------------------------------------------*/
void LgWritingSystemFactory::RemoveWsRunsForDescriptions(IWritingSystem * pws, int wsDel)
{
	int cws;
	CheckHr(pws->get_DescriptionWsCount(&cws));
	if (cws)
	{
		int iws;
		Vector<int> vws;
		vws.Resize(cws);
		CheckHr(pws->get_DescriptionWss(cws, vws.Begin()));
		for (iws = 0; iws < cws; ++iws)
		{
			ITsStringPtr qtss;
			CheckHr(pws->get_Description(vws[iws], &qtss));
			int crun;
			CheckHr(qtss->get_RunCount(&crun));
			bool fChange = false;
			TsRunInfo tri;
			ITsTextPropsPtr qttp;
			int nVar;
			int wsRun;
			// Build a new description string minus any offending runs with ktptWs == wsDel.
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			AssertPtr(qtsf);
			ITsStrBldrPtr qtsb;
			SmartBstr sbstr;
			CheckHr(qtsf->GetBldr(&qtsb));
			int cch = 0;
			for (int irun = 0; irun < crun; ++irun)
			{
				CheckHr(qtss->FetchRunInfo(irun, &tri, &qttp));
				CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &wsRun));
				if (wsRun == wsDel)
				{
					fChange = true;
					continue;
				}
				CheckHr(qtss->get_RunText(irun, &sbstr));
				CheckHr(qtsb->Replace(cch, cch, sbstr, qttp));
				CheckHr(qtsb->get_Length(&cch));
			}
			if (fChange)
			{
				ITsStringPtr qtssNew;
				if (cch)
					CheckHr(qtsb->GetString(&qtssNew));
				CheckHr(pws->put_Description(vws[iws], qtssNew));
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Unregister a writing system. This should be done before closing the document.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::RemoveEngine(int ws)
{
	BEGIN_COM_METHOD;
	// Unregister the writing system. Note that it is not an error not to find it.
	// Occasionally we may shut down a writing system that never got registered here.
	m_hmnwsobj.Delete(ws);
	m_setws.Delete(ws);
	StrUni stuLocale;
	bool fHasLocale = m_hmwsLocale.Retrieve(ws, &stuLocale);
	m_hmwsLocale.Delete(ws);
	if (fHasLocale)
	{
		m_hmwsId.Delete(stuLocale);
		// Now for the fun part: uninstalling the language from ICU.
		// Eliminate any internal use of the deleted ws.
		MapIntEncoding::iterator it;
		for (it = m_hmnwsobj.Begin(); it != m_hmnwsobj.End(); ++it)
		{
			IWritingSystem * pws = it->GetValue();
			pws->put_Name(ws, NULL);
			pws->put_Abbr(ws, NULL);
			pws->put_Description(ws, NULL);
			pws->put_Dirty(FALSE);		// We update the XML files ourselves.
			int ccoll;
			pws->get_CollationCount(&ccoll);
			for (int icoll = 0; icoll < ccoll; ++icoll)
			{
				ICollationPtr qcoll;
				pws->get_Collation(icoll, &qcoll);
				qcoll->put_Name(ws, NULL);
				qcoll->put_Dirty(FALSE);
			}
			// Now, fix any Description strings to remove any internal runs in ws.
			RemoveWsRunsForDescriptions(pws, ws);
		}
		// Scan through the XML language data files for uses of the writing system we're trying
		// to eliminate.
		LanguageDefinitionFactoryPtr qldf;
		qldf.Create();
		qldf->RemoveLanguage(stuLocale.Bstr());

		// Also, if we're connected to a database, delete it here: this is really nasty
		// potentially.
		if (m_qode)
		{
			return E_NOTIMPL;
		}
	}
	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}



/*----------------------------------------------------------------------------------------------
	Get a writing system code from a string which represents an ICU Locale.
	This validates that the writing system is known to the system.

	@param bstr ICU Locale string
	@param pwsId Pointer to an integer for returning the writing system code (database id).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::GetWsFromStr(BSTR bstr, int * pwsId)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstr);
	ChkComOutPtr(pwsId);

	StrUni stu(bstr);
	if (m_hmwsId.Retrieve(stu, pwsId))
		return S_OK;
	// Someone (another program) could have changed the writing systems behind our back, so
	// check with the database for the current state and try again if necessary.  See LT-8718.
	if (RefreshMapsIfNeeded() && m_hmwsId.Retrieve(stu, pwsId))
		return S_OK;
	else
		return S_FALSE;

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}


/*----------------------------------------------------------------------------------------------
	Similarly the full writing system identifying string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::GetStrFromWs(int ws, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	StrUni stu;
	if (m_hmwsLocale.Retrieve(ws, &stu))
	{
		stu.GetBstr(pbstr);
		return S_OK;
	}
	else
	{
		return S_FALSE;
	}

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Return the number of writing systems that exist.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_NumberOfWs(int * pcws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcws);

	*pcws = m_setws.Size();

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Fill in the given array with a list of all the writing system numbers.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::GetWritingSystems(int * rgws, int cws)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rgws, cws);

	int * pws = rgws;
	int iws = 0;
	for (Set<int>::iterator it = m_setws.Begin(); it != m_setws.End(); ++it)
	{
		if (iws < cws)
		{
			*pws++ = it.GetValue();
		}
		++iws;
	}
	for ( ; iws < cws; ++iws)
		rgws[iws] = 0;

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Return a singleton character property engine for pure Unicode character manipulation.
----------------------------------------------------------------------------------------------*/
HRESULT LgWritingSystemFactory::GetUnicodeCharProps(ILgCharacterPropertyEngine ** pplcpe)
{
	AssertPtr(pplcpe);

	ILgCharacterPropertyEnginePtr qlcpeUnicode;
	StrUtil::InitIcuDataDir();
	ISimpleInitPtr qsimi;
	// Make an instance; initially get the interface we need to initialize it.
	LgIcuCharPropEngine::CreateCom(NULL, IID_ISimpleInit, (void **)&qsimi);
	if (!qsimi)
		ThrowHr(WarnHr(E_UNEXPECTED));
	// This engine does not need any init data.
	CheckHr(qsimi->InitNew(NULL, 0));
	// If initialization succeeds, get the requested interface.
	CheckHr(qsimi->QueryInterface(IID_ILgCharacterPropertyEngine, (void **)&qlcpeUnicode));
	AssertPtr(qlcpeUnicode);
	*pplcpe = qlcpeUnicode.Detach();

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Serialize a vector of writing systems as a group.  The output is deserialized as a factory
	by LgWritingSystemFactory::Deserialize().
----------------------------------------------------------------------------------------------*/
void LgWritingSystemFactory::SerializeVector(IStorage * pstg,
	ComVector<IWritingSystem> & vqws)
{
	AssertPtr(pstg);

	IStreamPtr qstrmWss;
	CheckHr(pstg->CreateStream(L"Wss", STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0,
		&qstrmWss));
	int cws = vqws.Size();
	int ws;
	int iws;
	ULONG cb;
	ULONG cbWritten;
	// 1. Write the number of writing systems.
	cb = isizeof(int);
	CheckHr(qstrmWss->Write(&cws, cb, &cbWritten));

	// 2. For each writing system, write its id code and ICU Locale string.
	SmartBstr sbstr;
	int cch;
	for (iws = 0; iws < cws; ++ iws)
	{
		CheckHr(vqws[iws]->get_WritingSystem(&ws));
		cb = isizeof(int);
		CheckHr(qstrmWss->Write(&ws, cb, &cbWritten));
		CheckHr(vqws[iws]->get_IcuLocale(&sbstr));
		cch = sbstr.Length();
		CheckHr(qstrmWss->Write(&cch, cb, &cbWritten));
		cb = sbstr.ByteLen();
		CheckHr(qstrmWss->Write(sbstr.Chars(), cb, &cbWritten));
	}
	CheckHr(qstrmWss->Commit(STGC_DEFAULT));

	// 3. For each writing system, create a storage and serialize it.
	IStoragePtr qstgEnc;
	for (iws = 0; iws < cws; ++iws)
	{
		CheckHr(vqws[iws]->get_WritingSystem(&ws));
		SmartBstr sbstrWs;
		CheckHr(vqws[iws]->get_IcuLocale(&sbstrWs));
		StrUni stu;
		stu.Format(L"%s.%d", sbstrWs.Chars(), ws);
		CheckHr(pstg->CreateStorage(stu.Chars(), STGM_READWRITE | STGM_SHARE_EXCLUSIVE,
			0, 0, &qstgEnc));
		CheckHr(vqws[iws]->Serialize(qstgEnc));
		CheckHr(qstgEnc->Commit(STGC_DEFAULT));
	}
}


/*----------------------------------------------------------------------------------------------
	Holds a singleton character property engine for pure Unicode character manipulation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_UnicodeCharProps(ILgCharacterPropertyEngine ** pplcpe)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pplcpe);
	GetUnicodeCharProps(pplcpe);
	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Get the default collater for the specified writing system.
	ENHANCE: in some later Milestone, when we have user preferences figured out, we may take a
	different approach to finding the default.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_DefaultCollater(int ws, ILgCollatingEngine ** ppcoleng)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppcoleng);

	IWritingSystemPtr qws;
	HRESULT hr;
	IgnoreHr(hr = get_EngineOrNull(ws, &qws));
	if (FAILED(hr))
		return hr; // probably not WarnHr, this could be somewhat legitimate
	if (qws)
		return qws->get_CollatingEngine(ppcoleng);
	else
		ThrowHr(WarnHr(E_INVALIDARG));

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Shortcut for the char prop engine for a particular WS
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_CharPropEngine(int ws,
	ILgCharacterPropertyEngine ** ppcpe)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppcpe);

	IWritingSystemPtr qws;
	CheckHr(get_EngineOrNull(ws, &qws));
	if (qws)
	{
		CheckHr(qws->get_CharPropEngine(ppcpe));
	}
	else
	{
		ThrowHr(WarnHr(E_INVALIDARG));
	}
	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------


	@param ws
	@param ows
	@param ppre
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_Renderer(int ws, IVwGraphics * pvg,
	IRenderEngine ** ppre)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppre);
	IWritingSystemPtr qws;
	CheckHr(get_EngineOrNull(ws, &qws));
	if (qws)
		CheckHr(qws->get_Renderer(pvg, ppre));
	else
		ThrowHr(WarnHr(E_INVALIDARG));
	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Get the renderer for a particular Chrp
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_RendererFromChrp(LgCharRenderProps * pchrp,
	IRenderEngine ** ppre)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pchrp);
	ChkComOutPtr(ppre);

	HRESULT hr = S_OK;
	// Unfortunately we need a DC before we can call SetupGraphics.
	HDC hdc = ::CreateDC(TEXT("DISPLAY"),NULL,NULL,NULL);
	try
	{
		IVwGraphicsWin32Ptr qvgw;
		qvgw.CreateInstance(CLSID_VwGraphicsWin32);
		CheckHr(qvgw->Initialize(hdc));
		CheckHr(qvgw->SetupGraphics(pchrp));
		hr = get_Renderer(pchrp->ws, qvgw, ppre);
		qvgw.Clear();
	}
	catch(...)
	{
		::DeleteDC(hdc);
		throw;
	}
	::DeleteDC(hdc);
	return hr;

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Call this when your program no longer wants to use writing systems, typically
	as the program shuts down. Not using it will cause some apparent memory
	leaks. Using it before that will mean that new writing system and related object
	instances get made, replacing any already created.
	If the WSF was created using an OleDbEncap, it must be called exactly once per call
	to LgWritingSystemFactoryBuilder::GetWritingSystemFactory or
	LgWritingSystemFactoryBuilder::GetWritingSystemFactoryNew.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::Shutdown()
{
	BEGIN_COM_METHOD

	if (m_stuDatabase.Length())
	{
		m_nOdeShareCount--;
		if (m_nOdeShareCount != 0)
			return S_OK;

		 IgnoreHr(SaveWritingSystems());		// Ignore errors -- what could we do anyway?
	}
	// JohnT (8-16-04): Let's not do this. The method comment says it is harmless, just wasteful,
	// to Shutdown prematurely...but if we do this it isn't, the program crashes if it later
	// tries to get a writing system. And in the preferred usage, SaveWritingSystems() does
	// very little if there are no writing systems, and not much even if there are unless
	// they have not been saved. And if someone wakes it up again and creates one and modifies
	// it, we probably want to save the results!
	m_stuDatabase.Clear();		// We don't want to try saving again via another path!
	m_stuServer.Assign(L"Shutdown already");	// Prevent the factory from being reused.
	m_qode.Clear();
	m_qode = NULL;

	MapIntEncoding::iterator it;
	for (it = m_hmnwsobj.Begin(); it != m_hmnwsobj.End(); ++it)
	{
		dynamic_cast<WritingSystem *>(it.GetValue().Ptr())->Close();
	}
	m_hmnwsobj.Clear();
	m_setws.Clear();
	m_hmwsLocale.Clear();
	m_hmwsId.Clear();

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Clears enough of the writing system factory that it will reload writing systems as needed.
	This is used during FullRefresh.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::Clear()
{
	BEGIN_COM_METHOD

	MapIntEncoding::iterator it;
	for (it = m_hmnwsobj.Begin(); it != m_hmnwsobj.End(); ++it)
	{
		CheckHr(it.GetValue()->putref_WritingSystemFactory(NULL));
	}
	m_hmnwsobj.Clear();
	m_setws.Clear();
	m_hmwsLocale.Clear();
	m_hmwsId.Clear();

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Call this when your program wants to save the current set of writing systems to the
	database.  This commits any open transaction on the database (if a database is in use).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::SaveWritingSystems()
{
	BEGIN_COM_METHOD

	ComBool fTransWasOpen;
	if (m_qode)
	{
		CheckHr(m_qode->IsTransactionOpen(&fTransWasOpen));
		if (fTransWasOpen)
			CheckHr(m_qode->CommitTrans());		// Just in case.
		CheckHr(m_qode->BeginTrans());
	}

	MapIntEncoding::iterator it;
	for (it = m_hmnwsobj.Begin(); it != m_hmnwsobj.End(); ++it)
	{
		if (it.GetKey())
			CheckHr(it.GetValue()->SaveIfDirty(m_qode));
	}

	if (m_qode)
	{
		ComBool f;
		CheckHr(m_qode->IsTransactionOpen(&f));
		if (f)
			CheckHr(m_qode->CommitTrans());

		if (fTransWasOpen)
			CheckHr(m_qode->BeginTrans());
	}

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Write the current set of writing systems to the IStorage object to allow a read-only copy of
	this factory to be reconstituted later.

	@param pstg Pointer to the IStorage object used to store the factory's data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::Serialize(IStorage * pstg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstg);

	ComVector<IWritingSystem> vqws;
	MapIntEncoding::iterator it;
	for (it = m_hmnwsobj.Begin(); it != m_hmnwsobj.End(); ++it)
		vqws.Push(it->GetValue());

	SerializeVector(pstg, vqws);
	CheckHr(pstg->Commit(STGC_DEFAULT));

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Return the ws (~database id) of the writing system used for the user interface of this app.
	This can fail (with E_FAIL) if the writing system is not yet defined.

	@param pws Pointer to an integer for returning the ws of the user interface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::put_UserWs(int ws)
{
	BEGIN_COM_METHOD;
	m_wsUser = ws;
	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Return the ws (~database id) of the writing system used for the user interface of this app.
	This can fail (with E_FAIL) if the writing system is not yet defined.

	@param pws Pointer to an integer for returning the ws of the user interface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_UserWs(int * pws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pws);

	if (!m_wsUser)
	{
		// TODO (SteveMc):  (EVENTUALLY) derive the user interface ws from calling
		// ::GetUserDefaultUILanguage or ::GetUserDefaultLCID, and then determining the best
		// match to available localized resources.  This functionality probably belongs in
		// Generic because it will be needed by all C++ apps and DLLs.  C# code then could get
		// at it by calling this method.
		StrUni stuWs(kstidUserWs);
		if (!stuWs.Length())
			stuWs.Assign(L"en");
		if (!m_hmwsId.Retrieve(stuWs, &m_wsUser))
		{
			if (m_hmwsId.Size())
			{
				ThrowHr(WarnHr(E_FAIL));
			}
			else
			{
				IWritingSystemPtr qws;
				CheckHr(get_Engine(stuWs.Bstr(), &qws));
				CheckHr(qws->get_WritingSystem(&m_wsUser));
				// REVIEW: Do we want to finish defining the default ws properties?
			}
		}
	}
	*pws= m_wsUser;

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	This little class and its one static instance exists to get the Shutdown method
	called when the module is unloading. This prevents spurious memory leak reports.
	It works because any instance of ModuleEntry receives this message automatically
	when the process is being detached.
----------------------------------------------------------------------------------------------*/
class ShutdownNotifier : public ModuleEntry
{
	virtual void ProcessDetach(void)
	{
		LgWritingSystemFactory::ShutdownIfActive();
	}
};

static ShutdownNotifier sn; // existence of an instance gets the processDetach called.


/*----------------------------------------------------------------------------------------------
	Called from LgWritingSystemFactoryBuilder::GetWritingSystemFactory() to establish a link to
	a database for persistence.
	Following a call to this method, get_Engine() reads the data from that database, and
	Shutdown() writes data to that database.  Feeding empty strings (or NULL) as the arguments
	breaks the connection without saving the current state.

	@param pode Connection to a database containing the WritingSystem information.
----------------------------------------------------------------------------------------------*/
HRESULT LgWritingSystemFactory::UseDatabase(IOleDbEncap * pode, IStream * pfistLog)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pode);
	ChkComArgPtrN(pfistLog);

	SmartBstr sbstrServer;
	SmartBstr sbstrDatabase;
	CheckHr(pode->get_Server(&sbstrServer));
	CheckHr(pode->get_Database(&sbstrDatabase));
	m_stuServer = sbstrServer.Chars();
	m_stuDatabase = sbstrDatabase.Chars();
	m_qode = pode;
	m_qfistLog = pfistLog;	// Logging stream.

	Assert(!m_setws.Size());
	Assert(!m_hmnwsobj.Size());

	// Query the database for the set of writing systems that it knows about.
	Assert(m_stuDatabase.Length());

	LoadMapsFromDatabase(m_setws, m_hmwsLocale, m_hmwsId);

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}


/*----------------------------------------------------------------------------------------------
	Loads the provided set and hashmaps from the database.

	@param setws Reference to a set of writing system database object ids.
	@param hmwsLocale Reference to a map from database object id to ICU Locale.
	@param hmwsId Reference to a map from ICU Locale to database object id.
----------------------------------------------------------------------------------------------*/
void LgWritingSystemFactory::LoadMapsFromDatabase(Set<int> & setws,
	HashMap<int, StrUni> & hmwsLocale, HashMapStrUni<int> & hmwsId)
{
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fIsNull2;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	StrUni stuCmd;
	int hvoWs;
	StrUni stuLocale;
	const int kcchBuffer = MAX_PATH;
	OLECHAR rgchLocale[kcchBuffer];
	CheckHr(m_qode->CreateCommand(&qodc));
	stuCmd.Format(L"SELECT [id],[ICULocale] FROM LgWritingSystem");
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	bool fFixNulls = false;
	StrUni stuIds;
	while (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoWs),
			isizeof(hvoWs), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgchLocale),
			kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull2, 2));
		if (!fIsNull && !fIsNull2)
		{
			setws.Insert(hvoWs);
			stuLocale = rgchLocale;
			hmwsLocale.Insert(hvoWs, stuLocale, true);
			hmwsId.Insert(stuLocale, hvoWs, true);
		}
		else
		{
			if (stuIds.Length() > 0)
				stuIds.Append(L",");
			stuIds.FormatAppend(L"%d", hvoWs);
			fFixNulls = true;
		}
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	if (fFixNulls)
	{
		// Remove any botched writing systems from the factory.
		stuCmd.Format(L"exec DeleteObjects '%s'",stuIds.Chars());
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	}
	qodc.Clear();
}


/*----------------------------------------------------------------------------------------------
	Get the writing system code (hvo) for a default writing system (English if possible).
----------------------------------------------------------------------------------------------*/
int LgWritingSystemFactory::GetDefaultWsCode()
{
	// Return code for English if it's available.
	int ws = 0;
	StrUni stu(L"en");
	if (m_hmwsId.Retrieve(stu, &ws))
		return ws;
	// Otherwise, just return something if any writing systems exist.
	if (m_setws.Size())
	{
		Set<int>::iterator it = m_setws.Begin();
		return it.GetValue();
	}
	// REVIEW: What do we use for a ws value here if nothing exists yet?
	// FIXME!!
	return ws;
}


/*----------------------------------------------------------------------------------------------
	Get the flag to bypass installation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_BypassInstall(ComBool * pfBypass)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfBypass);

	if (m_fBypassInstall)
	{
		*pfBypass = TRUE;
		return S_OK;
	}

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}


/*----------------------------------------------------------------------------------------------
	Set the flag to bypass installation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::put_BypassInstall(ComBool fBypass)
{
	BEGIN_COM_METHOD;

	m_fBypassInstall = (bool)fBypass;

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Get the flag whether or not we are a remote user (TE-8606)
----------------------------------------------------------------------------------------------*/
bool LgWritingSystemFactory::DetermineRemoteUser()
{
	if (!m_qode)
		return false;

	StrUni stuSql;
	IOleDbCommandPtr qodc;
	// Note the collations on a Canadian machine were different between the two strings,
	// so we need to make sure we force the same collation.
	stuSql = "SELECT CASE WHEN Convert(nvarchar(200),SERVERPROPERTY('MachineName')) collate SQL_Latin1_General_CP1_CI_AS "
		"= HOST_NAME() collate SQL_Latin1_General_CP1_CI_AS THEN 0 ELSE 1 END";
	CheckHr(m_qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	CheckHr(qodc->NextRow(&fMoreRows));
	int fIsRemote = 0;
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&fIsRemote),
			isizeof(fIsRemote), &cbSpaceTaken, &fIsNull, 0));
	}
	return fIsRemote;
}

/*----------------------------------------------------------------------------------------------
	Add a newly created writing system to the factory.  The writing system and its default
	collation should already have been created in the database when this method is called.

	@param ws Database id of the new writing system
	@param bstrIcuLocale ICU locale name for the new writing system
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::AddWritingSystem(int ws, BSTR bstrIcuLocale)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrIcuLocale);

	StrUni stuIcuLocale(bstrIcuLocale);
	if (!m_setws.IsMember(ws))
	{
		m_setws.Insert(ws);
	}
	StrUni stuOld;
	if (m_hmwsLocale.Retrieve(ws, &stuOld))
	{
		if (stuOld != stuIcuLocale)
			ThrowHr(WarnHr(E_INVALIDARG));
	}
	else
	{
		m_hmwsLocale.Insert(ws, stuIcuLocale);
	}
	int wsOld;
	if (m_hmwsId.Retrieve(stuIcuLocale, &wsOld))
	{
		if (ws != wsOld)
			ThrowHr(WarnHr(E_INVALIDARG));
	}
	else
	{
		m_hmwsId.Insert(stuIcuLocale, ws);
	}
	IWritingSystemPtr qws;
	return GetEngine(ws, &qws);

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Get the flag to bypass installation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactory::get_IsShutdown(ComBool * pfIsShutdown)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfIsShutdown);

	if (!m_qode &&
		m_stuDatabase.Length() == 0 &&
		m_stuServer.Equals(L"Shutdown already"))
	{
		*pfIsShutdown = TRUE;
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_ILgWritingSystemFactory);
}

/*----------------------------------------------------------------------------------------------
	Get the list of current LgWritingSystem objects from the database, and update the maps as
	necessary.  See LT-8718 for motivation.  (Think multiple FW programs running at once.)

	@returns true if the list of writing systems has changed (and hence the map contents)
----------------------------------------------------------------------------------------------*/
bool LgWritingSystemFactory::RefreshMapsIfNeeded()
{
	if (!m_qode)
		return false;
	// Query the database for the set of writing systems that it knows about.
	bool fChanged = false;
	// The set of defined writing systems in the database.
	Set<int>setws;
	// ws ICULocale, looked up by ws identifiers.
	HashMap<int, StrUni> hmwsLocale;
	// ws identifiers, looked up by ws ICULocale.
	HashMapStrUni<int> hmwsId;

	LoadMapsFromDatabase(setws, hmwsLocale, hmwsId);

	if (hmwsLocale.Size() != hmwsId.Size() || hmwsLocale.Size() != setws.Size())
	{
		// warn the user that the world is excessively unstable?
	}

	int hvoWs;
	StrUni stuLocale;

	// Add new writing systems that have been added behind our back.
	for (Set<int>::iterator it = setws.Begin(); it != setws.End(); ++it)
	{
		hvoWs = it->GetValue();
		if (!m_setws.IsMember(hvoWs))
		{
			hmwsLocale.Retrieve(hvoWs, &stuLocale);
			m_setws.Insert(hvoWs);
			m_hmwsLocale.Insert(hvoWs, stuLocale, true);
			m_hmwsId.Insert(stuLocale, hvoWs, true);
			fChanged = true;
		}
	}
	// Remove old writing systems that have been removed behind our back.
	for (Set<int>::iterator it = m_setws.Begin(); it != m_setws.End(); ++it)
	{
		hvoWs = it->GetValue();
		if (!setws.IsMember(hvoWs))
		{
			m_setws.Delete(hvoWs);
			m_hmwsLocale.Retrieve(hvoWs, &stuLocale);
			m_hmwsLocale.Delete(hvoWs);
			m_hmwsId.Delete(stuLocale);
			fChanged = true;
		}
	}
	return fChanged;
}

//:>********************************************************************************************
//:>	Methods for the LgWritingSystemFactoryBuilder class.
//:>********************************************************************************************

// This class factory originally had ThreadingModel=Both, but with that we're getting problems
// when running C# tests in Nunit-GUI.
static GenericFactory g_factBuilder(
	_T("SIL.Language.WritingSystemFactoryBuilder"),
	&CLSID_LgWritingSystemFactoryBuilder,
	_T("SIL language writing system factory builder"),
	_T("Apartment"),
	&LgWritingSystemFactoryBuilder::CreateCom);

/*----------------------------------------------------------------------------------------------
	Called by the GenericFactory to "create" an ILgWritingSystemFactory.  It returns the global
	one associated with the registry.
----------------------------------------------------------------------------------------------*/
void LgWritingSystemFactoryBuilder::CreateCom(IUnknown * punkCtl, REFIID iid, void ** ppv)
{
	AssertPtrN(punkCtl);
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	LgWritingSystemFactoryBuilderPtr qzwsfb;
	qzwsfb.Attach(NewObj LgWritingSystemFactoryBuilder);
	CheckHr(qzwsfb->QueryInterface(iid, ppv));
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgWritingSystemFactoryBuilder::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgWritingSystemFactoryBuilder *>(this));
	else if (riid == IID_ILgWritingSystemFactoryBuilder)
		*ppv = static_cast<ILgWritingSystemFactoryBuilder *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ILgWritingSystemFactoryBuilder);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

STDMETHODIMP_(ULONG) LgWritingSystemFactoryBuilder::AddRef()
{
	::InterlockedIncrement(&m_cref);
	return m_cref;
}

STDMETHODIMP_(ULONG) LgWritingSystemFactoryBuilder::Release()
{
	ulong cref = ::InterlockedDecrement(&m_cref);
	if (!cref)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}

//:>********************************************************************************************
//:>	ILgWritingSystemFactoryBuilder Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get a writing system factory for a given database.
	@param pode Pointer to an open database connection.
	@param pfistLog Pointer to a stream for logging errors.
	@param ppwsf Address of a pointer for returning a factory which is linked to the
		given database.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactoryBuilder::GetWritingSystemFactory(IOleDbEncap * pode,
	IStream * pfistLog, ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pode);
	ChkComArgPtrN(pfistLog);
	ChkComOutPtr(ppwsf);

	LgWritingSystemFactory::Create(pode, pfistLog, ppwsf);

	END_COM_METHOD(g_factBuilder, IID_ILgWritingSystemFactoryBuilder);
}


/*----------------------------------------------------------------------------------------------
	Get a writing system factory for a given database.
	@param bstrServer Name of the machine running the database server.
	@param bstrDatabase Name of the database containing the WritingSystem information.
	@param pfistLog Pointer to a stream for logging errors.
	@param ppwsf Address of a pointer for returning a factory which is linked to the
		given database.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactoryBuilder::GetWritingSystemFactoryNew(BSTR bstrServer,
	BSTR bstrDatabase, IStream * pfistLog, ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD
	ChkComBstrArg(bstrServer);
	ChkComBstrArg(bstrDatabase);
	ChkComArgPtrN(pfistLog);
	ChkComOutPtr(ppwsf);

	IOleDbEncapPtr qode;
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(bstrServer, bstrDatabase, pfistLog, koltMsgBox, koltvForever));
	LgWritingSystemFactory::Create(qode, pfistLog, ppwsf);

	END_COM_METHOD(g_factBuilder, IID_ILgWritingSystemFactoryBuilder);
}


/*----------------------------------------------------------------------------------------------
	Write the current set of writing systems to the IStorage object to allow a read-only copy of
	this factory to be reconstituted later.

	@param pstg Pointer to the IStorage object used to store the factory's data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactoryBuilder::Deserialize(IStorage * pstg,
	ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstg);
	ChkComOutPtr(ppwsf);

	LgWritingSystemFactory::Deserialize(pstg, ppwsf);

	END_COM_METHOD(g_factBuilder, IID_ILgWritingSystemFactoryBuilder);
}


/*----------------------------------------------------------------------------------------------
	Call this when program is shutting down, and you want to ensure that all factories are shut
	down properly to release any memory they consumed.  Note that all writing system factories
	are unusable after this, although pointers to them may still be valid.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgWritingSystemFactoryBuilder::ShutdownAllFactories()
{
	BEGIN_COM_METHOD;

	LgWritingSystemFactory::ShutdownIfActive();

	END_COM_METHOD(g_factBuilder, IID_ILgWritingSystemFactoryBuilder);
}

/*----------------------------------------------------------------------------------------------
	Create a Collation object both in memory and in the database, and link it to its owning
	writing system.
----------------------------------------------------------------------------------------------*/
void LgWritingSystemFactory::CreateLgCollation(Collation ** ppcoll, IWritingSystem * pws)
{
	AssertPtr(ppcoll);
	if (!ppcoll)
		ThrowHr(WarnHr(E_INVALIDARG));
	*ppcoll = NULL;
	AssertPtr(pws);
	if (!pws)
		ThrowHr(WarnHr(E_INVALIDARG));

	CollationPtr qzcoll;
	qzcoll.Attach(NewObj Collation);
	if (!qzcoll)
		ThrowHr(WarnHr(E_UNEXPECTED));

	// Create it in the database if we're connected to a database.
	if (m_qode)
	{
		int hvoWs;
		CheckHr(pws->get_WritingSystem(&hvoWs));
		// Commit any previous database transactions.
		ComBool f;
		m_qode->IsTransactionOpen(&f);
		// Must NOT close a transaction if one was already open! Maybe part of larger Undo task
		// (e.g., running the WS dialog.)
		int hvo;
		try
		{
			if (!f)
				m_qode->BeginTrans();

			IOleDbCommandPtr qodc;
			CheckHr(m_qode->CreateCommand(&qodc));
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *)&hvo,
				isizeof(hvo)));
			StrUni stuCmd;
			ComBool fIsNull;
			stuCmd.Format(L"EXEC CreateOwnedObject$ %d, ? output, null, %d, %d, %d",
				kclidLgCollation, hvoWs, kflidLgWritingSystem_Collations, kcptOwningSequence);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtStoredProcedure));
			CheckHr(qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvo), isizeof(hvo), &fIsNull));
			if (fIsNull)
				ThrowHr(WarnHr(E_FAIL));			// Something went wrong.
			qodc.Clear();

			//// Do we need this test of the transaction?
			//m_qode->IsTransactionOpen(&f);
			//if (f)
			//	m_qode->CommitTrans();

			if (!f)
				CheckHr(m_qode->CommitTrans());
		}
		catch(...)
		{
			if (!f)
				m_qode->RollbackTrans();
			throw;	// For now we have nothing to add, so pass it on up.
		}

		// Store the HVO for future reference.
		qzcoll->SetHvo(hvo);
	}

	// Link it to the writing system object, and add the factory link.
	int ccoll;
	CheckHr(pws->get_CollationCount(&ccoll));
	CheckHr(pws->putref_Collation(ccoll, qzcoll));

	CheckHr(qzcoll->putref_WritingSystemFactory(this));

	*ppcoll = qzcoll.Detach();
}


/*----------------------------------------------------------------------------------------------
	We are changing the IcuLocale on a writing system, so we need to update the
	hashmaps used to convert between ws and IcuLocale.
----------------------------------------------------------------------------------------------*/
void LgWritingSystemFactory::ChangingIcuLocale(int ws, const OLECHAR * pchOld,
	const OLECHAR * pchNew)
{
	StrUni stuNew(pchNew);
	StrUni stuOld;
#ifdef DEBUG
	bool f = m_hmwsLocale.Retrieve(ws, &stuOld);
	Assert(f && stuOld.Equals(pchOld)); // Verify that we have the old value.
#endif
	m_hmwsLocale.Insert(ws, stuNew, true); // Replace old value with new.

	stuOld.Assign(pchOld);
#ifdef DEBUG
	int wsT;
	f = m_hmwsId.Retrieve(stuOld, &wsT);
	Assert(f && wsT == ws); // Verify that we have the old value.
#endif
	m_hmwsId.Delete(stuOld); // Remove old value.
	m_hmwsId.Insert(stuNew, ws, true); // Add new value.
}


/*----------------------------------------------------------------------------------------------
	Generate a new writing system HVO value.
----------------------------------------------------------------------------------------------*/
int LgWritingSystemFactory::GetNewHvoWs()
{
	int hvoWs = 0;
	if (m_qode)
	{
		// Commit any previous database transactions.
		ComBool f;
		m_qode->IsTransactionOpen(&f);
		// Must NOT close a transaction if one was already open! Maybe part of larger Undo task
		// (e.g., running the WS dialog.)
		try
		{
			if (!f)
				m_qode->BeginTrans();
			IOleDbCommandPtr qodc;
			CheckHr(m_qode->CreateCommand(&qodc));
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4,
						(ULONG *)&hvoWs, isizeof(HVO)));
			StrUni stuCmd;
			ComBool fIsNull;
			stuCmd.Format(L"EXEC CreateObject$ %d, ? output, null", kclidLgWritingSystem);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtStoredProcedure));
			CheckHr(qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoWs),
				isizeof(HVO), &fIsNull));
			if (fIsNull)
				return 0;		// Something went wrong.
			qodc.Clear();

			// Do we need this test of transaction?
			//m_qode->IsTransactionOpen(&f);
			//if (f)
			//	m_qode->CommitTrans();

			if (!f)
				CheckHr(m_qode->CommitTrans());
		}
		catch(...)
		{
			if (!f)
				m_qode->RollbackTrans();
			throw;	// For now we have nothing to add, so pass it on up.
		}
	}
	else
	{
		Assert(!m_qode.Ptr());
		// Generate a pseudo-HVO for the registry-based factory.
		if (m_setws.Size())
		{
			Set<int>::iterator it;
			for (it = m_setws.Begin(); it != m_setws.End(); ++it)
			{
				if (hvoWs <= it->GetValue())
					hvoWs = it->GetValue() + 1;
			}
		}
		else
		{
			hvoWs = 1;
		}
	}
	return hvoWs;
}


/*----------------------------------------------------------------------------------------------
	Fill in the writing system values from the given language definition file.  Note that the
	HVO, ICU Locale, and LastModified values have already been set.  Everything else needs to be
	loaded from the file.  We do this the easy way with the class that's already been
	implemented for this purpose.

	Returns true if successful, false (or throws) if an error occurs.
----------------------------------------------------------------------------------------------*/
bool LgWritingSystemFactory::LoadWsFromFile(WritingSystem * pzws)
{
	LanguageDefinitionFactoryPtr qldf;
	qldf.Create();
	LanguageDefinitionPtr qld;
	bool fNewWs = false;
	int hvoWs = (int)pzws->m_hvo;
	if (hvoWs && !m_setws.IsMember(hvoWs))
	{
		fNewWs = true;
		m_setws.Insert(hvoWs);		// enable a ws to have name/abbr/descr in itself.
		m_hmwsLocale.Insert(hvoWs, pzws->m_stuIcuLocale);
	}
	HRESULT hr = qldf->InitializeFromXml(this, pzws->m_stuIcuLocale.Bstr(), &qld);
	if (fNewWs)
	{
		m_setws.Delete(hvoWs);
		m_hmwsLocale.Delete(hvoWs);
	}
	if (hr != S_OK)
		return false;
	AssertPtr(qld.Ptr());

	// Copy all the loaded values to the object.
	IWritingSystemPtr qwsXml;
	CheckHr(qld->get_WritingSystem(&qwsXml));

	int cws;
	Vector<int> vws;
	SmartBstr sbstr;
	int nT;
	ComBool fT;

	CheckHr(qwsXml->get_Locale(&nT));
	CheckHr(pzws->put_Locale(nT));

	CheckHr(qwsXml->get_RightToLeft(&fT));
	CheckHr(pzws->put_RightToLeft(fT));

	CheckHr(qwsXml->get_FontVariation(&sbstr));
	CheckHr(pzws->put_FontVariation(sbstr));

	CheckHr(qwsXml->get_SansFontVariation(&sbstr));
	CheckHr(pzws->put_SansFontVariation(sbstr));

	CheckHr(qwsXml->get_BodyFontFeatures(&sbstr));
	CheckHr(pzws->put_BodyFontFeatures(sbstr));

	CheckHr(qwsXml->get_DefaultSerif(&sbstr));
	CheckHr(pzws->put_DefaultSerif(sbstr));

	CheckHr(qwsXml->get_DefaultSansSerif(&sbstr));
	CheckHr(pzws->put_DefaultSansSerif(sbstr));

	CheckHr(qwsXml->get_DefaultBodyFont(&sbstr));
	CheckHr(pzws->put_DefaultBodyFont(sbstr));

	CheckHr(qwsXml->get_DefaultMonospace(&sbstr));
	CheckHr(pzws->put_DefaultMonospace(sbstr));

	CheckHr(qwsXml->get_KeyMan(&fT));
	CheckHr(pzws->put_KeyMan(fT));

	CheckHr(qwsXml->get_LegacyMapping(&sbstr));
	CheckHr(pzws->put_LegacyMapping(sbstr));

	CheckHr(qwsXml->get_KeymanKbdName(&sbstr));
	CheckHr(pzws->put_KeymanKbdName(sbstr));

	CheckHr(qwsXml->get_ValidChars(&sbstr));
	CheckHr(pzws->put_ValidChars(sbstr));

	CheckHr(qwsXml->get_SpellCheckDictionary(&sbstr));
	CheckHr(pzws->put_SpellCheckDictionary(sbstr));

	CheckHr(qwsXml->get_CapitalizationInfo(&sbstr));
	CheckHr(pzws->put_CapitalizationInfo(sbstr));

	CheckHr(qwsXml->get_MatchedPairs(&sbstr));
	CheckHr(pzws->put_MatchedPairs(sbstr));

	CheckHr(qwsXml->get_PunctuationPatterns(&sbstr));
	CheckHr(pzws->put_PunctuationPatterns(sbstr));

	CheckHr(qwsXml->get_QuotationMarks(&sbstr));
	CheckHr(pzws->put_QuotationMarks(sbstr));

	CheckHr(qwsXml->get_NameWsCount(&cws));
	if (cws)
	{	// Copy the multilingual name values.
		vws.Resize(cws);
		CheckHr(qwsXml->get_NameWss(cws, vws.Begin()));
		for (int iws = 0; iws < cws; ++iws)
		{
			CheckHr(qwsXml->get_Name(vws[iws], &sbstr));
			CheckHr(pzws->put_Name(vws[iws], sbstr));
		}
	}

	CheckHr(qwsXml->get_AbbrWsCount(&cws));
	if (cws)
	{	// Copy the multilingual abbreviation values.
		vws.Resize(cws);
		CheckHr(qwsXml->get_AbbrWss(cws, vws.Begin()));
		for (int iws = 0; iws < cws; ++iws)
		{
			CheckHr(qwsXml->get_Abbr(vws[iws], &sbstr));
			CheckHr(pzws->put_Abbr(vws[iws], sbstr));
		}
	}

	CheckHr(qwsXml->get_DescriptionWsCount(&cws));
	if (cws)
	{	// Copy the multilingual description values.
		vws.Resize(cws);
		CheckHr(qwsXml->get_DescriptionWss(cws, vws.Begin()));
		for (int iws = 0; iws < cws; ++iws)
		{
			ITsStringPtr qtss;
			CheckHr(qwsXml->get_Description(vws[iws], &qtss));
			CheckHr(pzws->put_Description(vws[iws], qtss));
		}
	}

	// Copy any/all collations.
	int ccoll;
	CheckHr(qwsXml->get_CollationCount(&ccoll));
	for (int icoll = 0; icoll < ccoll; ++icoll)
	{
		ICollationPtr qcoll;
		CheckHr(qwsXml->get_Collation(icoll, &qcoll));
		CheckHr(pzws->putref_Collation(icoll, qcoll));
	}

	// Install the writing system into the factory.
	// Force the ws to write to the DB, but we don't want to run InstallLanguage unless we
	// need to.
	CheckHr(AddEngine(pzws));
	// We used to do this (with some other trickery involving m_fBypassInstall):
	//pzws->m_fDirty = true;
	//CheckHr(pzws->SaveIfDirty(m_qode));
	// But that rewrites part of the file we just read! And that's REALLY bad if WorldPad is
	// in the middle of its approach to reading it.
	pzws->SaveToDatabase(m_qode); // So just make sure the database if any is consistent...
	// And if it's not 'installed' already ICU needs to know.
	if (!IsLocaleInstalled(pzws->m_stuIcuLocale.Bstr()))
		pzws->IcuInstallLanguage(false);

	pzws->m_fDirty = false;
	// Set LastModified to the timestamp of the file so it won't be overwritten when we close.
	pzws->SetLastModifiedTime();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return true if the given ICU Locale has already been installed.  If it's a custom locale,
	also check whether the XML file has been updated since the installation, and return false
	if so.
----------------------------------------------------------------------------------------------*/
bool LgWritingSystemFactory::IsLocaleInstalled(const wchar * pszIcuLocale)
{
	StrUni stuXmlFile;
	stuXmlFile.Format(L"%s\\Languages\\%s.xml",
		DirectoryFinder::FwRootDataDir().Chars(), pszIcuLocale);
	StrUni stuResFile;
	stuResFile.Format(L"%s\\%s.res", DirectoryFinder::IcuDataDir().Chars(), pszIcuLocale);
	WIN32_FILE_ATTRIBUTE_DATA fadXml;
	BOOL fOk = ::GetFileAttributesExW(stuXmlFile.Chars(), GetFileExInfoStandard, &fadXml);
	DWORD dwErr;
	if (!fOk)
	{
		dwErr = ::GetLastError();
		return false;
	}
	WIN32_FILE_ATTRIBUTE_DATA fadRes;
	fOk = ::GetFileAttributesExW(stuResFile.Chars(), GetFileExInfoStandard, &fadRes);
	if (!fOk)
	{
		dwErr = ::GetLastError();
		return false;
	}

	if (!IsCustomLocale(pszIcuLocale))
		return true;

	ULARGE_INTEGER uliXml;
	uliXml.LowPart = fadXml.ftLastWriteTime.dwLowDateTime;
	uliXml.HighPart = fadXml.ftLastWriteTime.dwHighDateTime;
	ULARGE_INTEGER uliRes;
	uliRes.LowPart = fadRes.ftLastWriteTime.dwLowDateTime;
	uliRes.HighPart = fadRes.ftLastWriteTime.dwHighDateTime;
	return (uliRes.QuadPart >= uliXml.QuadPart);
}

/*----------------------------------------------------------------------------------------------
	Return true if the given ICU Locale is a custom locale defined by a user.
----------------------------------------------------------------------------------------------*/
bool LgWritingSystemFactory::IsCustomLocale(const wchar * pszIcuLocale)
{
	bool fRet = false;
	StrUtil::InitIcuDataDir();
	UErrorCode uerr = U_ZERO_ERROR;
	UResourceBundle * prbRoot = ures_open(NULL, "", &uerr);
	if (U_SUCCESS(uerr) && prbRoot != NULL)
	{
		UResourceBundle * prbCustom = ures_getByKey(prbRoot, "Custom", NULL, &uerr);
		if (U_SUCCESS(uerr) && prbCustom != NULL)
		{
			UResourceBundle * prbLocales = ures_getByKey(prbCustom,"LocalesAdded", NULL, &uerr);
			if (U_SUCCESS(uerr) && prbLocales != NULL)
			{
				while (ures_hasNext(prbLocales))
				{
					Assert(sizeof(wchar) == sizeof(UChar));
					int len;
					const char * pszKey;
					const UChar * pszVal = ures_getNextString(prbLocales, &len, &pszKey, &uerr);
					if (!wcscmp(pszIcuLocale, pszVal))
					{
						fRet = true;
						break;
					}
				}
				ures_close(prbLocales);
			}
			ures_close(prbCustom);
		}
		ures_close(prbRoot);
	}
	// Get rid of any memory-mapping of files by ICU, and reinitialize ICU.
	IIcuCleanupManagerPtr qicln;
	qicln.CreateInstance(CLSID_IcuCleanupManager);
	CheckHr(qicln->Cleanup());
	return fRet;
}


// Allow template method instantiation.
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
#include "ComHashMap_i.cpp"
#include "Set_i.cpp"
