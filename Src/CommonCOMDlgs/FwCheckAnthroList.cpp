/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2005 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwCheckAnthroList.cpp
Responsibility:
Last reviewed: never

Description:
	The standard definition for the IFwCheckAnthroList interface.
-------------------------------------------------------------------------------*//*:End Ignore*/

#pragma hdrstop
#include "main.h"
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	IMPLEMENTATION OF FwCheckAnthroList.
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.FW.FwCheckAnthroList"),
	&CLSID_FwCheckAnthroList,
	_T("SIL FieldWorks FwCheckAnthroList"),
	_T("Apartment"),
	&FwCheckAnthroList::CreateCom);


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwCheckAnthroList::FwCheckAnthroList()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwCheckAnthroList::~FwCheckAnthroList()
{
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Static method to create an instance of FwCheckAnthroList.
----------------------------------------------------------------------------------------------*/
void FwCheckAnthroList::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwCheckAnthroList> qzfwst;
	qzfwst.Attach(NewObj FwCheckAnthroList());	// ref count initially 1
	CheckHr(qzfwst->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IUnknown methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IFwCheckAnthroList are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCheckAnthroList::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IFwCheckAnthroList *>(this));
	else if (iid == IID_IFwCheckAnthroList)
		*ppv = static_cast<IFwCheckAnthroList *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IFwCheckAnthroList);
		return S_OK;
	}
	else
	{
		return E_NOINTERFACE;
	}
	reinterpret_cast<IUnknown *>(*ppv)->AddRef();

	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Standard COM AddRef method.

	@return The reference count after incrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) FwCheckAnthroList::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Standard COM Release method.

	@return The reference count after decrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) FwCheckAnthroList::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}

//:>********************************************************************************************
//:>	IFwCheckAnthroList methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Determine whether the given XML file in the Templates directory contains an anthropology
	list.
----------------------------------------------------------------------------------------------*/
static bool IsAnthroList(const achar * pszFile)
{
	StrAnsi staFile(DirectoryFinder::FwTemplateDir());
	staFile.Append("\\");
	staFile.Append(pszFile);
	bool fRet = false;
	FILE * fp;
	if (!fopen_s(&fp, staFile.Chars(), "r"))
	{
		char rgchBuf[512];
		// Look in the first few lines for an identifying string.
		for (int i = 0; i < 4; ++i)
		{
			if (!fgets(rgchBuf, isizeof(rgchBuf), fp))
				break;
			if (_stricmp(rgchBuf, "<!DOCTYPE AnthroList6001 SYSTEM \"FwDatabase.dtd\">\n") == 0)
			{
				fRet =  true;
				break;
			}
			if (strcmp(rgchBuf, "<AnthroList6001>\n") == 0)
			{
				fRet = true;
				break;
			}
		}
		fclose(fp);
	}
	return fRet;
}

/*
	DWORD GetWindowThreadProcessId(          HWND hWnd,
		LPDWORD lpdwProcessId
	);
 */

static DWORD s_procId;


static ATOM s_atomType;
static BOOL CALLBACK EnumFunc(HWND hwnd, LPARAM lParam)
{
	WINDOWINFO wi;
	::GetWindowInfo(hwnd, &wi);
	DWORD procId;
	::GetWindowThreadProcessId(hwnd, &procId);
	if (wi.atomWindowType == s_atomType && procId == s_procId)
	{
		::EnableWindow(hwnd, lParam);
#if 99
		StrAnsi sta;
		sta.Format("EnumFunc(hwnd = 0x%x, lParam = %d): atom = %d, procId = %d%n", hwnd, lParam, wi.atomWindowType, procId);
		::OutputDebugStringA(sta.Chars());
#endif
	}
	return true;
}

static void EnableRelatedWindows(HWND hwnd, bool fEnable)
{
	WINDOWINFO wi;
	::GetWindowInfo(hwnd, &wi);
	s_atomType = wi.atomWindowType;
	::GetWindowThreadProcessId(hwnd, &s_procId);
#if 99
		StrAnsi sta;
		sta.Format("EnableRelatedWindows(hwnd = 0x%x, fEnable = %d): atom = %d, procId = %d%n", hwnd, fEnable, wi.atomWindowType, s_procId);
		::OutputDebugStringA(sta.Chars());
#endif
	::EnumWindows(EnumFunc, fEnable);
}

/*----------------------------------------------------------------------------------------------
	COM method to conditionally show the dialog and process its results.

	@return S_OK or appropriate error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCheckAnthroList::CheckAnthroList(IOleDbEncap * pode, DWORD hwndParent,
	BSTR bstrProjName, int wsDefault)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pode);
	ChkComBstrArg(bstrProjName);

	HWND hwnd = (HWND)hwndParent;

	// 1. Determine whether or not the Anthropology List has been initialized.

	bool fListInitialized = false;
	ComBool fMoreRows;
	StrUni stuQuery;
	IOleDbCommandPtr qodc;
	ULONG cbSpaceTaken;
	ComBool fIsNull;
	try
	{
		stuQuery.Format(L"select ItemClsId from CmPossibilityList"
			L" where [id] = (select Dst from LangProject_AnthroList)");
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			int cpt = 0;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cpt),
				isizeof(cpt), &cbSpaceTaken, &fIsNull, 0));
			fListInitialized = !fIsNull && (cpt != 0);
		}
	}
	catch (...)
	{
		// If an error throws, just assume that the list hasn't been initialized.
	}
	qodc.Clear();
	if (fListInitialized)
		return S_OK;		// The Anthropology List may still be empty, but it's initialized!

	// 2. Figure out what lists are available (in {FW}/Templates/*.xml).

	StrApp strFilePattern(DirectoryFinder::FwTemplateDir());
	strFilePattern.Append(_T("\\*.xml"));

	bool fHaveOCM = false;
	bool fHaveFRAME = false;
	Vector<StrApp> vstrXmlFiles;
	const achar kszOCM[] = _T("OCM.xml");
	const achar kszFRAME[] = _T("OCM-Frame.xml");
	WIN32_FIND_DATA wfd;
	HANDLE hfind = ::FindFirstFile(strFilePattern.Chars(), &wfd);
	if (hfind != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (_tcsicmp(wfd.cFileName, kszOCM) == 0)
			{
				fHaveOCM = true;
			}
			else if (_tcsicmp(wfd.cFileName, kszFRAME) == 0)
			{
				fHaveFRAME = true;
			}
			else if (_tcsicmp(wfd.cFileName, _T("NewLangProj.xml")) != 0) // Ignore basic data.
			{
				StrApp str(wfd.cFileName);
				vstrXmlFiles.Push(str);
			}
		} while (::FindNextFile(hfind, &wfd));
		::FindClose(hfind);
		for (int istr = 0; istr < vstrXmlFiles.Size(); ++istr)
		{
			if (!IsAnthroList(vstrXmlFiles[istr].Chars()))
			{
				vstrXmlFiles.Delete(istr);
				--istr;
			}
		}
	}

	// 3. display a dialog for the user to select a list.

	StrApp strFile;
	if (fHaveOCM || fHaveFRAME || vstrXmlFiles.Size())
	{
		RnAnthroListDlgPtr qrald;
		qrald.Create();
		qrald->SetValues(fHaveOCM, fHaveFRAME, vstrXmlFiles, m_strHelpFilename.Chars());
		if (m_sbstrDescription.Length())
			qrald->SetDescription(m_sbstrDescription);
		EnableRelatedWindows(hwnd, false);
		int ctid = qrald->DoModal(hwnd);
		EnableRelatedWindows(hwnd, true);
		if (ctid == kctidOk)
		{
			int nChoice = qrald->GetChoice();
			switch (nChoice)
			{
			case RnAnthroListDlg::kralUserDef:
				strFile.Clear();	// Initialize directly with SQL rather than an XML file.
				break;
			case RnAnthroListDlg::kralOCM:
				strFile = kszOCM;
				break;
			case RnAnthroListDlg::kralFRAME:
				strFile = kszFRAME;
				break;
			default:
				Assert((unsigned)nChoice < (unsigned)vstrXmlFiles.Size());
				strFile = vstrXmlFiles[nChoice];
				break;
			}
		}
		else
		{
			Assert(ctid == kctidOk);
			ThrowHr(E_UNEXPECTED);
		}
	}

	// 4. Load the selected list, or initialize properly for a User-defined (empty) list.

	int hvoListOwner = 0;
	int hvoList = 0;
	// We need the list id for a user-defined list, and we need the list owner id for a list
	// loaded from an XML file.
	stuQuery.Format(L"SELECT Owner$,[Id] from CmObject where OwnFlid$ = %d",
		kflidLangProject_AnthroList);
	CheckHr(pode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoListOwner),
			isizeof(hvoListOwner), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hvoList),
			isizeof(hvoList), &cbSpaceTaken, &fIsNull, 0));
	}
	else
	{
		Assert(fMoreRows);
		ThrowHr(E_UNEXPECTED);
	}
	qodc.Clear();

	if (strFile.Length())
	{
		WaitCursor wc;

		// Import a predefined list.
		StrUni stuFile(DirectoryFinder::FwTemplateDir());
		stuFile.Append(L"\\");
		stuFile.Append(strFile);
		IFwXmlDataPtr qfwxd;
		qfwxd.CreateInstance(CLSID_FwXmlData, CLSCTX_INPROC_SERVER);
		IFwXmlData2Ptr qfwxd2;
		CheckHr(qfwxd->QueryInterface(IID_IFwXmlData2, (void **)&qfwxd2));
		SmartBstr sbstrServer;
		SmartBstr sbstrDbName;
		CheckHr(pode->get_Server(&sbstrServer));
		CheckHr(pode->get_Database(&sbstrDbName));
		CheckHr(qfwxd2->Open(sbstrServer, sbstrDbName));

		AfProgressDlgPtr qprog;
		qprog.Create();
		qprog->DoModeless(hwnd);
		StrApp strMsg(kstidRnAnthroImportListProg);
		qprog->SetMessage(strMsg.Chars());
		StrApp strFmt(kstidRnAnthroImportListTitleFmt);
		StrApp strProj(bstrProjName);
		strMsg.Format(strFmt.Chars(), strProj.Chars());
		qprog->SetTitle(strMsg.Chars());
		qprog->SetRange(0, 100);
		IAdvIndPtr qadvi;
		qprog->QueryInterface(IID_IAdvInd, (void **)&qadvi);

		CheckHr(qfwxd2->ImportXmlObject(stuFile.Bstr(), hvoListOwner,
			kflidLangProject_AnthroList, qadvi));

		qadvi.Clear();
		qprog->DestroyHwnd();
		qprog.Clear();
	}
	else
	{
		// Finish initializing an empty list.
		StrUni stuListName(kstidRnAnthroListName);	// "Anthropology Categories"
		StrUni stuListAbbr(kstidRnAnthroListAbbr);	// "Anth"
		stuQuery.Format(L"UPDATE CmPossibilityList"
			L" SET Depth = 127, DisplayOption = %d, ItemClsid = %d, WsSelector = %d"
			L" WHERE [Id] = %d;%n"
			L"INSERT INTO CmMajorObject_Name (Obj, Ws, Txt) VALUES (%d, %d, N'%s');%n"
			L"INSERT INTO CmPossibilityList_Abbreviation (Obj, Ws, Txt) VALUES (%d, %d, N'%s')",
			kpntNameAndAbbrev, kclidCmAnthroItem, kwsAnals,
			hvoList,
			hvoList, wsDefault, stuListName.Chars(),
			hvoList, wsDefault, stuListAbbr.Chars());
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		qodc.Clear();
	}

	// 5. create the corresponding overlays if the list is not empty.

	int hvoOverlay = 0;
	stuQuery.Format(L"SELECT [id] FROM CmOverlay WHERE PossList = %d", hvoList);
	CheckHr(pode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoOverlay),
			isizeof(hvoOverlay), &cbSpaceTaken, &fIsNull, 0));
	}
	else
	{
		Assert(fMoreRows);
		ThrowHr(E_UNEXPECTED);
	}
	Assert(hvoOverlay);

	Vector<int> vhvo;
	vhvo.Push(hvoList);
	while (vhvo.Size())
	{
		stuQuery.Format(L"SELECT [Id] FROM CmAnthroItem_ WHERE Owner$ in (%d", vhvo[0]);
		int ihvo;
		for (ihvo = 1; ihvo < vhvo.Size(); ++ihvo)
			stuQuery.FormatAppend(L",%d", vhvo[ihvo]);
		stuQuery.Append(L")");

		vhvo.Clear();

		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			int hvo = 0;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull && hvo)
				vhvo.Push(hvo);
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		if (vhvo.Size())
		{
			stuQuery.Format(L"INSERT INTO CmOverlay_PossItems (Src,Dst) VALUES (%d, ?)",
				hvoOverlay);
			CheckHr(pode->CreateCommand(&qodc));
			for (ihvo = 0; ihvo < vhvo.Size(); ++ihvo)
			{
				CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
					reinterpret_cast<ULONG *>(&vhvo[ihvo]), isizeof(int)));
				CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
			}
			qodc.Clear();
		}
	}

	END_COM_METHOD(g_fact, IID_IFwCheckAnthroList);
}

/*----------------------------------------------------------------------------------------------
	COM method to set the description string for the dialog.

	@return S_OK or appropriate error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCheckAnthroList::put_Description(BSTR bstrDescription)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrDescription);

	m_sbstrDescription = bstrDescription;

	END_COM_METHOD(g_fact, IID_IFwCheckAnthroList);
}

/*----------------------------------------------------------------------------------------------
	COM method to set the help filename string for the dialog.

	@return S_OK or appropriate error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCheckAnthroList::put_HelpFilename(BSTR bstrHelpFilename)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrHelpFilename);

	m_strHelpFilename = bstrHelpFilename;

	END_COM_METHOD(g_fact, IID_IFwCheckAnthroList);
}
