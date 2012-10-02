/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpDa.cpp
Responsibility: Sharon Correll
Last reviewed: never

Description:
	This class provides an implementation of ISilDataAccess for WorldPad, where the underlying
	data is a simple XML representation of a document consisting of a sequence of styled
	paragraphs.

	The object representing the whole database is always an StText with HVO 1.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore

static DummyFactory g_fact(_T("SIL.WorldPad.DataAccess")); // For END_COM_METHOD macros

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
WpDa::WpDa()
{
	m_fls = kflsOkay;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
WpDa::~WpDa()
{
}

void WpDa::OnReleasePtr()
{
	m_qacth.Clear();
}

//:>********************************************************************************************
//:>	ISilDataAccess methods overridden.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Return the writing system factory for this database (or the registry, as the case may be).

	@param ppwsf Address of the pointer for returning the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WpDa::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppwsf);

	SetUpWsFactory();

	*ppwsf = m_qwsf;
	(*ppwsf)->AddRef();

	END_COM_METHOD(g_fact, IID_ISilDataAccess)
}

/*----------------------------------------------------------------------------------------------
	Set the writing system factory for this database (or the registry, as the case may be).

	@param pwsf Pointer to the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WpDa::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pwsf);

	if (pwsf == m_qwsf)
		return S_OK; // normal case

	if (!m_qwsf)
	{
		m_qwsf = pwsf;
		WpApp::MainApp()->SetWsFactory(m_qwsf);
		return S_OK;
	}
	return E_FAIL;

	END_COM_METHOD(g_fact, IID_ISilDataAccess)
}

/*----------------------------------------------------------------------------------------------
	Return the encodings of interest within the database (specifically this is currently
	used to set up the Styles dialog). Here return all the encodings (or as many as we have
	room for).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WpDa::get_WritingSystemsOfInterest(int cwsMax, int * prgenc, int * pcws)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgenc, cwsMax);
	ChkComOutPtr(pcws);

	SetUpWsFactory();

	int cws;
	CheckHr(m_qwsf->get_NumberOfWs(&cws));
	/* JohnT: delete this, it doesn't return the count. To get an accurate count,
	we need to go through the loop.
	if (cwsMax == 0)
		return S_OK; // just give number
	*/
	int * prgencT = NewObj int[cws]; // AFTER testing cwsMax! Else memory leak..
	CheckHr(m_qwsf->GetWritingSystems(prgencT, cws));

	*pcws = 0;
	for (int iws = 0; iws < cws; iws++)
	{
		if (cwsMax > 0 && *pcws >= cwsMax)
			return E_INVALIDARG;

		if (prgencT[iws] != 0) // ignore the "unknown" writing system
		{
			if (cwsMax > 0)
				*(prgenc + *pcws) = prgencT[iws];
			(*pcws)++;
		}
	}

	delete[] prgencT;

	END_COM_METHOD(g_fact, IID_ISilDataAccess)
}


//:>********************************************************************************************
//:>	Other methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize the data from the given file, or create a new empty string if no file name is
	supplied. This method is called while we are in the process of opening the window.
----------------------------------------------------------------------------------------------*/
void WpDa::InitNew(StrAnsi staFileName, int * pfiet, WpMainWnd * pwpwnd, WpChildWnd * pwcwnd)
{
	WaitCursor wc; // creates a wait cursor and makes it active until the end of the method.

	m_pwpwnd = pwpwnd;

	WpMainWndPtr qwpwndLauncher = pwpwnd->LauncherWindow();
	if (!qwpwndLauncher)
		qwpwndLauncher = m_pwpwnd;

	if (staFileName == "")
	{
		InitNewEmpty();
		return;
	}

	OLECHAR * pswzData;
	int cbFileLen = 0;
	int fls;
	int cchwInFile;
	GetTextFromFile(staFileName, pfiet, &cbFileLen, &pswzData, &cchwInFile, &fls);
	if (fls == kflsAborted)
	{
		// We're half-way through the process of opening the window. Finish, and set a
		// flag to just close it when we're done.
		m_fls = fls;
		InitNewEmpty();
		return;
	}

	if (*pfiet == kfietXml || *pfiet == kfietTemplate)
	{
		//	XML import.
		StrUni stu(staFileName);
		LoadXml(stu.Bstr(), pwpwnd, qwpwndLauncher, &m_fls);
		int ctss;
		CheckHr(get_VecSize(khvoText, kflidStText_Paragraphs, &ctss));
//		pwcw->UpdateView(ctss); -- don't need to do this, since it is a brand new window
		ClearChanges();
		return;
	}

	//	Read from a flat text file.

	Vector<StrUni> vstu;
	ReadStringsFromBuffer(pswzData, cchwInFile, vstu);

	HVO * prghvoPara = NewObj HVO[vstu.Size()];
	for (int istu = 0; istu < vstu.Size(); istu++)
		prghvoPara[istu] = khvoParaMin + istu;
	CacheVecProp(khvoText, kflidStText_Paragraphs, prghvoPara, vstu.Size());
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	ILgWritingSystemFactoryPtr qwsf = WpApp::MainApp()->GetWsFactory();
	int wsDefault;
	CheckHr(qwsf->get_UserWs(&wsDefault));
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	StrUni stuNormal = g_pszwStyleNormal;
	CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, stuNormal.Bstr()));
	ITsTextPropsPtr qttp;
	CheckHr(qtpb->GetTextProps(&qttp));

	for (int istu = 0; istu < vstu.Size(); istu++)
	{
		StrUni stuPara = vstu[istu];
		ITsStringPtr qtss;
		CheckHr(qtsf->MakeStringRgch(stuPara.Chars(), stuPara.Length(), wsDefault, &qtss));
		CacheStringProp(khvoParaMin + istu, kflidStTxtPara_Contents, qtss);

		CacheUnknown(khvoParaMin + istu, kflidStPara_StyleRules, qttp);
	}

	delete[] prghvoPara;

	ClearChanges();
}

/*----------------------------------------------------------------------------------------------
	Initialize an empty document. It has a document object (always ID 1!) and one paragraph
	containing (implicitly) an empty string. Style is set to Normal.
	Review SharonC(JohnT): what writing system should the empty string have??
----------------------------------------------------------------------------------------------*/
void WpDa::InitNewEmpty()
{
	HVO hvoPara = khvoParaMin;
	CacheVecProp(khvoText, kflidStText_Paragraphs, &hvoPara, 1);
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtss;
	int wsDefault;
	SetUpWsFactory();
	CheckHr(m_qwsf->get_UserWs(&wsDefault));
	CheckHr(qtsf->MakeStringRgch(L"", 0, wsDefault, &qtss));
	CacheStringProp(hvoPara, kflidStTxtPara_Contents, qtss);

	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	StrUni stuNormal = g_pszwStyleNormal;
	CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, stuNormal.Bstr()));
	ITsTextPropsPtr qttp;
	CheckHr(qtpb->GetTextProps(&qttp));
	CacheUnknown(hvoPara, kflidStPara_StyleRules, qttp);

	int cst;
	CheckHr(get_VecSize(khvoText, kflidStText_Styles, &cst));
	Assert(cst == 1); // one style, "Normal"

	ClearChanges();
}

/*----------------------------------------------------------------------------------------------
	Load the data from the given file into an empty window.

	@param staFileName	- file to load from
	@param pttp			- properties from the current selection, to use when loading into an
							empty window
----------------------------------------------------------------------------------------------*/
void WpDa::LoadIntoEmpty(StrAnsi staFileName, int * pfiet, WpMainWnd * pwpwnd,
	WpChildWnd * pwcw, ITsTextProps * pttp, int * pfls)
{
	WaitCursor wc; // creates a wait cursor and makes it active until the end of the method.

	OLECHAR * pswzData;
	int cbFileLen = 0;
	int cchwInFile;
	GetTextFromFile(staFileName, pfiet, &cbFileLen, &pswzData, &cchwInFile, pfls);
	if (pfls && *pfls == kflsAborted)
		return;

	if (*pfiet == kfietXml || *pfiet == kfietTemplate)
	{
		StrUni stu(staFileName);
		LoadXml(stu.Bstr(), pwpwnd, pwpwnd, pfls);
		int nRtl = DocRightToLeft();
		if (pwcw->ViewConstructor())
			pwcw->ViewConstructor()->SetRightToLeft((bool)nRtl);
		int ctss;
		CheckHr(get_VecSize(khvoText, kflidStText_Paragraphs, &ctss));
		pwcw->UpdateView(ctss);
		ClearChanges();
		return;
	}

	Assert(staFileName != "");
	int ctss;
	CheckHr(get_VecSize(1, kflidStText_Paragraphs, &ctss));
	Assert(ctss <= 1);

	Vector<StrUni> vstu;
	ReadStringsFromBuffer(pswzData, cchwInFile, vstu);

	HVO * prghvoPara = NewObj HVO[vstu.Size()];
	for (int istu = 0; istu < vstu.Size(); istu++)
		prghvoPara[istu] = khvoParaMin + istu;
	CacheVecProp(khvoText, kflidStText_Paragraphs, prghvoPara, vstu.Size());
//	CacheIntProp(khvoText, kflidStText_RightToLeft, 0);
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	for (int istu = 0; istu < vstu.Size(); istu++)
	{
		StrUni stuPara = vstu[istu];
		ITsStringPtr qtss;
		ITsStrBldrPtr qtsb;
		CheckHr(qtsf->GetBldr(&qtsb));
		CheckHr(qtsb->ReplaceRgch(0, 0, stuPara.Chars(), stuPara.Length(), pttp));
		CheckHr(qtsb->GetString(&qtss));
///		CheckHr(qtsf->MakeStringRgch(stuPara.Chars(), stuPara.Length(), ws, &qtss));

		CacheStringProp(khvoParaMin + istu, kflidStTxtPara_Contents, qtss);
	}

	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	StrUni stuNormal = g_pszwStyleNormal;
	CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, stuNormal.Bstr()));

	delete[] prghvoPara;

	pwcw->UpdateView(vstu.Size());
	ClearChanges();
}

/*----------------------------------------------------------------------------------------------
	Return true if the text buffer is empty--that is, containing only one empty string.
----------------------------------------------------------------------------------------------*/
bool WpDa::IsEmpty()
{
	int ctss;
	CheckHr(get_VecSize(khvoText, kflidStText_Paragraphs, &ctss));

	if (ctss == 0)
		return true;
	if (ctss > 1)
		return false;

	ITsStringPtr qtss;
	CheckHr(get_StringProp(khvoParaMin, kflidStTxtPara_Contents, &qtss));
	int cchw;
	CheckHr(qtss->get_Length(&cchw));
	return (cchw == 0);
}

/*----------------------------------------------------------------------------------------------
	Get the text to render from the file being opened, and convert it to Unicode if necessary.
	Return a pointer to the buffer and to the beginning of the data in the buffer,
	along with an indication of the data type (text writing system). Caller is responsible to
	delete the buffer.

	In the case of an XML format, just delete the buffers, because the XML import routines
	will do their own thing.
----------------------------------------------------------------------------------------------*/
void WpDa::GetTextFromFile(StrAnsi staFileName, int * pfiet,
	int * pcbFileLen, OLECHAR ** ppswzData, int * pcchw, int * pfls)
{
	FILE * f;

	if (fopen_s(&f, staFileName.Chars(), "rb"))
	{
		// No such file.
		if (pfls)
		{
			*pfls = kflsAborted;
			return;
		}
		else
		{
			ThrowHr(WarnHr(E_FAIL));
		}
	}

	if (pfls)
		*pfls = kflsOkay;

	//	Get length of file.
	fseek(f, 0, SEEK_END);
	*pcbFileLen = ftell(f);
	fseek(f, 0, SEEK_SET);

	++(*pcbFileLen); // null termination requires another character.

	OLECHAR * pswzData = NewObj OLECHAR[*pcbFileLen];
	memset(pswzData, 0, *pcbFileLen);
	char * pszData = NewObj char[*pcbFileLen];
	memset(pszData, 0, *pcbFileLen);

	fread(pszData, 1, *pcbFileLen, f);
	fclose(f);

	char * pch = pszData;
	OLECHAR * pchw;
	bool fUtf8 = false;
	if (WorldPadXmlFormat(pszData, *pcbFileLen, &fUtf8))
	{
		//	XML format
		if (*pfiet != kfietXml && *pfiet != kfietTemplate)
			*pfiet = kfietXml;
		delete[] pswzData;
		delete[] pszData;
		return;
	}

	if ((*pfiet == kfietUnknown || *pfiet == kfietUtf16) &&
		*pch == 0xFFFFFFFF && *(pch + 1) == 0xFFFFFFFE)
	{
		//	UTF-16, little-endian
		*pfiet = kfietUtf16;
		memcpy(pswzData, pszData + 2, *pcbFileLen - 2);
	}
	else if ((*pfiet == kfietUnknown && *pch == 0xFFFFFFFE && *(pch + 1) == 0xFFFFFFFF) ||
		*pfiet == kfietUtf16)
	{
		//	UTF-16 with the byte order reversed, or with no byte-order mark (the Unicode
		//	standard specifies big-endian as the default)
		*pfiet = kfietUtf16;
		pch += 2;
		pchw = pswzData;
		while (pch < pszData + *pcbFileLen && (*pch != 0 || *(pch + 1) != 0))
		{
			OLECHAR chwTmp1 = (OLECHAR)((*pch) << 8);
			OLECHAR chwTmp2 = (OLECHAR)(*(pch + 1) & 0x00FF);
			*pchw = (OLECHAR)(chwTmp1 | chwTmp2);
			pch += 2;
			pchw++;
		}
	}
	else
	{
		if (*pch == 0xFFFFFFEF && *(pch + 1) == 0xFFFFFFBB && *(pch + 2) == 0xFFFFFFBF)
		{
			*pfiet = kfietUtf8;
			// Skip UTF-8 markers
			*pcchw = SetUtf16FromUtf8(pswzData, *pcbFileLen - 3, pszData + 3);
		}
		else if (fUtf8)
		{
			*pfiet = kfietUtf8;
			int cch = SetUtf16FromUtf8(pswzData, *pcbFileLen, pszData);
			*pcchw = cch;
		}
		else
		{
			WpSavePlainTextDlgPtr qdlg;
			qdlg.Create();
			qdlg->Init(false, false);
			if (qdlg->DoModal(m_pwpwnd->Hwnd()) == kctidOk)
			{
				int i = qdlg->SelectedEncoding();	// UTF-8 or ANSI
				*pfiet = (i == 0) ? kfietUtf8 : kfietAnsi;
			}
			else
			{
				// User cancelled.
				if (pfls)
					*pfls = kflsAborted;
				delete[] pswzData;
				delete[] pszData;
				return;
			}
			WaitCursor wc; // create a wait cursor
			if (*pfiet == kfietUtf8)
				*pcchw = SetUtf16FromUtf8(pswzData, *pcbFileLen, pszData);
			else
				*pcchw = ::MultiByteToWideChar(CP_ACP, 0, pszData, *pcbFileLen,
					pswzData, *pcbFileLen);
		}
	}

	delete[] pszData;

	*ppswzData = pswzData;
}

/*----------------------------------------------------------------------------------------------
	Return true if the text is in WorldPad XML format. Also return a flag indicating whether
	we know for sure that this is UTF-8.
----------------------------------------------------------------------------------------------*/
bool WpDa::WorldPadXmlFormat(char * pszData, int pcbFileLen, bool * pfUtf8)
{
	bool fBom = (*pszData == 0xFFFFFFEF && *(pszData + 1) == 0xFFFFFFBB &&
			*(pszData + 2) == 0xFFFFFFBF);

	char * pch;
	if (fBom)
	{
		if (pcbFileLen <= 8)
			return false;
		pch = pszData + 3;
	}
	else
	{
		if (pcbFileLen <= 5)
			return false;
		pch = pszData;
	}

	StrAnsi staHeader(pch, 5);
	if (staHeader != "<?xml")
		return false;

	pch += 5;
	char * pchLim = pszData + pcbFileLen;
	if (pch >= pchLim)
		return false;
	StrAnsi sta2(pch, 9);
	if (sta2 != " version=")
		return false;

	pch += 9;
	if (pch >= pchLim)
		return false;
	if (*pch != '"')
		return false;
	pch++;
	while (*pch != '"')
	{
		pch++;
		if (pch >= pchLim)
			return false;
	}
	pch++;

	StrAnsi sta3(pch, 17);
	if (sta3 != " encoding=\"UTF-8\"")
		return false;

	*pfUtf8 = true;

	pch += 17;
	if (pch >= pchLim)
		return false;
	while (*pch != '?')
	{
		pch++;
		if (pch >= pchLim)
			return false;
	}
	StrAnsi sta4(pch, 2);
	if (sta4 != "?>")
		return false;

	pch += 2;
	if (pch >= pchLim)
		return false;
	while (*pch != '<')
	{
		pch++;
		if (pch >= pchLim)
			return false;
	}
	StrAnsi sta5(pch, 6);

	if (sta5 != "<WpDoc")
	{
		// Allow for the proper DOCTYPE definition.
		StrAnsiBuf stab("<!DOCTYPE WpDoc SYSTEM \"WorldPad.dtd\">");
		if (pch + stab.Length() >= pchLim || strncmp(pch, stab.Chars(), stab.Length()) != 0)
			return false;
		pch += stab.Length();
		while (*pch != '<')
		{
			pch++;
			if (pch >= pchLim)
				return false;
		}
		sta5.Assign(pch, 6);
	}

	if (sta5 != "<WpDoc")
		return false;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Make a list of strings from the text buffer (that was read from a file). Also delete
	the buffer.
----------------------------------------------------------------------------------------------*/
void WpDa::ReadStringsFromBuffer(OLECHAR * pswzData, int cchw, Vector<StrUni> & vstu)
{
	OLECHAR * pchw = pswzData;
	OLECHAR * pchwStrMin = pchw;
	OLECHAR * pchwLim = pswzData + cchw;
	while (pchw < pchwLim && *pchw != 0)
	{
		if (*pchw == 13 && *(pchw + 1) == 10)	// CRLF
		{
			//	Make a new string;
			StrUni stu(pchwStrMin, pchw - pchwStrMin);
			vstu.Push(stu);
			pchw += 2;	// skip CRLF
			pchwStrMin = pchw;
		}
		else if (*pchw == 13 || *pchw == 10)	// CR or LF
		{
			StrUni stu(pchwStrMin, pchw - pchwStrMin);
			vstu.Push(stu);
			pchw++;
			pchwStrMin = pchw;
		}
		else
			pchw++;
	}
	StrUni stu(pchwStrMin, pchw - pchwStrMin);
	vstu.Push(stu);

	// Replace any tab characters with spaces.
	for (int istu = 0; istu < vstu.Size(); istu++)
	{
		int ichTab = vstu[istu].FindCh(9, 0);
		while (ichTab > -1)
		{
			vstu[istu].Replace(ichTab, ichTab + 1, L" ", 1);
			ichTab = vstu[istu].FindCh(9, ichTab + 1);
		}
	}

	delete[] pswzData;
}

/*----------------------------------------------------------------------------------------------
	Save the text to a file.
----------------------------------------------------------------------------------------------*/
bool WpDa::SaveToFile(StrAnsi staFileName, int fiet, WpMainWnd * pwpwnd)
{
	WaitCursor wc; // creates a wait cursor and makes it active until the end of the method.
	StrUni stu(staFileName);

	// If the file exists rename it to a backup.
	if (SilUtil::FileExists(stu))
	{
		StrUni stuBackName(stu);
		int index = stuBackName.ReverseFindCh(L'.');
		if (index == -1)
			index = stuBackName.Length();
		stuBackName.Replace(index, stuBackName.Length(), L".bak");
		if (SilUtil::FileExists(stuBackName))
		{
			::DeleteFile(stuBackName.Chars());
		}
		::MoveFile(stu.Chars(), stuBackName.Chars());
	}

	StrApp strWp(kstidAppName);

	if ((fiet == kfietUnknown &&
			(staFileName.Right(4).EqualsCI(".wpx") || staFileName.Right(4).EqualsCI(".wpt")))
		|| fiet == kfietXml || fiet == kfietTemplate)
	{
		HRESULT hr;
		IgnoreHr(hr = SaveXml(stu.Bstr(), pwpwnd));
		if (FAILED(hr))
			return false;
		//	Clear out the indicators of what's changed since the last save.
		ClearChanges();
		return true;
	}

	IStreamPtr qstrm;
	try
	{
		FileStream::Create(staFileName.Chars(), kfstgmWrite | kfstgmCreate, &qstrm);
	}
	catch (Throwable & thr)
	{
		StrApp strRes(kstidCantSaveFile);
		StrApp strMsg;
		StrApp strDiag(thr.Message());
		if (strDiag == "")
			strDiag.Load(kstidFileErrUnknown);
		strMsg.Format(strRes, strDiag.Chars());
		::MessageBox(pwpwnd->Hwnd(), strMsg.Chars(), strWp.Chars(), MB_ICONEXCLAMATION);
		return false;
	}
	catch (...)
	{
		StrApp strRes(kstidCantSaveFile);
		StrApp strDiag(kstidFileErrUnknown);
		StrApp strMsg;
		strMsg.Format(strRes, strDiag.Chars());
		::MessageBox(pwpwnd->Hwnd(), strMsg.Chars(), strWp.Chars(), MB_ICONEXCLAMATION);
		return false;
	}

	ULONG cbWritten;
	byte b;
	bool fGaveErrorMsg = false;

	if (fiet == kfietAnsi)
	{
		//	No special markers
	}
	else if (fiet == kfietUtf8)
	{
		b = 0xEF;
		CheckHr(qstrm->Write(&b, 1, &cbWritten));
		b = 0xBB;
		CheckHr(qstrm->Write(&b, 1, &cbWritten));
		b = 0xBF;
		CheckHr(qstrm->Write(&b, 1, &cbWritten));
	}
	else
	{
		//	Default: UTF-16. Write byte-order mark.
		b = 0xFF;
		CheckHr(qstrm->Write(&b, 1, &cbWritten));
		b = 0xFE;
		CheckHr(qstrm->Write(&b, 1, &cbWritten));
	}

	int ctss;
	CheckHr(get_VecSize(khvoText, kflidStText_Paragraphs, &ctss));

	for (int itss = 0; itss < ctss; itss++)
	{
		HVO hvoPara;
		CheckHr(get_VecItem(khvoText, kflidStText_Paragraphs, itss, &hvoPara));
		ITsStringPtr qtss;
		CheckHr(get_StringProp(hvoPara, kflidStTxtPara_Contents, &qtss));
		BSTR bstr = NULL;
		int cchw;
		CheckHr(qtss->get_Length(&cchw));
		qtss->GetChars(0, cchw, &bstr);

		//	Write the string to the file.
		if (cchw > 0)
		{
			Assert(bstr);
			if (fiet == kfietUtf8)
			{
				int cchs = CountUtf8FromUtf16(bstr, cchw);
				char * rgchs = NewObj char[cchs];
				ConvertUtf16ToUtf8(rgchs, cchs, bstr, cchw);
				CheckHr(qstrm->Write(rgchs, cchs, &cbWritten));
				delete[] rgchs;
			}
			else if (fiet == kfietAnsi)
			{
				char * rgchs = NewObj char[cchw * 2];
				int fBadChars = 0;
				int cchs = ::WideCharToMultiByte(CP_ACP, 0, bstr, cchw, rgchs, cchw * 2, 0,
					&fBadChars);	// MSDN says the return value byte count includes
									// terminating null, but it doesn't seem to
				if (cchs == 0)
				{
					ThrowInternalError(E_FAIL);
				}
				CheckHr(qstrm->Write(rgchs, cchs, &cbWritten));
				if (fBadChars && !fGaveErrorMsg)
				{
					StrApp strWp(kstidAppName);
					StrApp strRes(kstidCantFormatAnsi);
					MessageBox(m_pwpwnd->Hwnd(), strRes.Chars(), strWp.Chars(),
						MB_OK | MB_ICONINFORMATION);
					fGaveErrorMsg = true;
				}
				delete[] rgchs;
			}
			else // UTF-16
			{
				CheckHr(qstrm->Write(bstr, cchw * isizeof(wchar), &cbWritten));
			}
		}

		if (itss < ctss - 1)
		{
			if (fiet == kfietUtf8 || fiet == kfietAnsi)
			{
				//	CRLF--two 8-bit chars
				b = 13;
				CheckHr(qstrm->Write(&b, 1, &cbWritten));
				b = 10;
				CheckHr(qstrm->Write(&b, 1, &cbWritten));
			}
			else
			{
				//	CRLF--little-endian
				b = 13;
				CheckHr(qstrm->Write(&b, 1, &cbWritten));
				b = 0;
				CheckHr(qstrm->Write(&b, 1, &cbWritten));
				b = 10;
				CheckHr(qstrm->Write(&b, 1, &cbWritten));
				b = 0;
				CheckHr(qstrm->Write(&b, 1, &cbWritten));
			}
		}

		ReleaseBstr(bstr);
	}

	//	Clear out the indicators of what's changed since the last save.
	ClearChanges();

	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the overall direction of the document. Now this is just the direction of the
	"Normal" paragraph style.

	Eventually we may need to store this as a property of the document itself.
----------------------------------------------------------------------------------------------*/
int WpDa::DocRightToLeft()
{
	AfStylesheetPtr qsts = m_pwpwnd->GetStylesheet();
	ITsTextPropsPtr qttp;
	// Get a non-const version of the "Normal" string:
	// REVIEW: Should the GetStyleRgch method be made to accept const strings?
	Vector<wchar> vchwNormal;
	int cchNormal = wcslen(g_pszwStyleNormal);
	vchwNormal.Resize(cchNormal + 1);
	wcscpy_s(vchwNormal.Begin(), cchNormal + 1, g_pszwStyleNormal);
	CheckHr(qsts->GetStyleRgch(cchNormal, vchwNormal.Begin(), &qttp));
	int nVar, nVal;
	CheckHr(qttp->GetIntPropValues(ktptRightToLeft, &nVar, &nVal));
	if (nVal == 1)
		return 1;
	else
		return 0;	// including -1

	// Eventually, maybe:
	//CheckHr(get_IntProp(khvoText, kflidStText_RightToLeft, &nDocRtl));
}

/*----------------------------------------------------------------------------------------------
	Open the main registry key that has the list of encodings inside of it, and return
	a handle to it.
----------------------------------------------------------------------------------------------*/
bool WpDa::OpenRegWsKey(int at, HKEY * phkey)
{
	StrApp str("Software\\SIL\\FieldWorks\\WorldPad\\WritingSystems");
	if (at == katRead)
		return (::RegOpenKeyEx(HKEY_CURRENT_USER, str, 0, at, phkey) == ERROR_SUCCESS);

	return (::RegCreateKeyEx(HKEY_CURRENT_USER, str, 0, NULL, 0, at, NULL, phkey, NULL)
		== ERROR_SUCCESS);
}

/*----------------------------------------------------------------------------------------------
	Delete the main registry key that has the list of encodings inside of it.
----------------------------------------------------------------------------------------------*/
bool WpDa::DeleteRegWsKey()
{
	HKEY hkey;
	StrApp str("Software\\SIL\\FieldWorks\\WorldPad");
	if (::RegOpenKeyEx(HKEY_CURRENT_USER, str, 0, katBoth, &hkey) != ERROR_SUCCESS)
		return false;

	return (::RegDeleteKey(hkey, _T("WritingSystems")) == ERROR_SUCCESS);
}

/*----------------------------------------------------------------------------------------------
	Fill in the vector with the ids of all the WorldPad encodings in the registry.
----------------------------------------------------------------------------------------------*/
bool WpDa::GetAllWsFromRegistry(Vector<StrApp> & vstr)
{
	RegKey hkey;
	if (!OpenRegWsKey(katRead, &hkey))
		return false;

	DWORD dwIndex;
	for (dwIndex = 0; ; ++dwIndex)
	{
		achar rgch[256];
		DWORD cb = isizeof(rgch);
		LONG l = ::RegEnumValue(hkey, dwIndex, rgch, &cb, NULL, NULL, NULL, NULL);
		if (l == ERROR_NO_MORE_ITEMS)
			return true;
		else if (l != ERROR_SUCCESS)
			return false;

		StrApp strTmp(rgch);
		vstr.Push(strTmp);
	}
	Assert(false);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the XML description of an writing system, which is the string value of the
	writing system in the registry.
----------------------------------------------------------------------------------------------*/
bool WpDa::GetWsDescription(const achar * pszWsKey, StrApp & str)
{
	AssertPsz(pszWsKey);

	RegKey hkey;
	if (OpenRegWsKey(katRead, &hkey))
	{
		Vector<achar> vch;
		vch.Resize(1024);
		DWORD cb = vch.Size() * isizeof(achar);
		DWORD dwT;
		LONG nRet = ::RegQueryValueEx(hkey, pszWsKey, NULL, &dwT, (BYTE *)vch.Begin(), &cb);
		vch.Resize(cb / isizeof(achar));
		if (nRet == ERROR_MORE_DATA)
		{
			nRet = ::RegQueryValueEx(hkey, pszWsKey, NULL, &dwT, (BYTE *)vch.Begin(), &cb);
		}
		if (nRet == ERROR_SUCCESS)
		{
			Assert(dwT == REG_SZ);
			str.Assign(vch.Begin(), vch.Size() - 1);	// Don't include terminating NUL.
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Delete an writing system from the registry.
----------------------------------------------------------------------------------------------*/
bool WpDa::DeleteWsFromRegistry(int ws)
{
	if (!m_qwsf.Ptr())
	{
		AssertPtr(m_qwsf);
		return false;
	}

	//	Generate the writing system ID string.
	SmartBstr sbstr;
	HRESULT hr;
	CheckHr(hr = m_qwsf->GetStrFromWs(ws, &sbstr));
	if (hr == S_FALSE || !sbstr.Length())
		return false;

	//	Open main writing system key and delete it.
	StrApp strWs(sbstr.Chars(), sbstr.Length());
	RegKey hkey;
	if (!OpenRegWsKey(katBoth, &hkey))
		return false;
	if (::RegDeleteValue(hkey, strWs.Chars()) == ERROR_SUCCESS)
		return true;

	return false;
}


/*----------------------------------------------------------------------------------------------
	Initialize the writing system factory with the writing systems stored in Language Definition
	files.
----------------------------------------------------------------------------------------------*/
void WpDa::InitWithPersistedWs(HWND hwndMain)
{
	// First, replace any registry entries with corresponding Language Definition files.
	InitWithWsInRegistry(hwndMain);
	// Next, read all existing Language Definition files.  This may be redundant if registry
	// entries existed, but it's only a one-time occurrence.
	StrApp strLangDir(DirectoryFinder::FwRootDataDir());
	strLangDir.Append(_T("\\Languages"));
	StrApp strFilePattern;
	strFilePattern.Format(_T("%s\\*.xml"), strLangDir.Chars());
	// We may try twice to read the writing system files. This becomes necessary if, for example,
	// a writing system has a name or definition in multiple languages, and gets loaded before
	// we load the WS for one of the alternatives. This is very rare, so we recover by just
	// reading again...but this time we should have the WS we created last time. If there really
	// isn't a definition for the needed WS, on the second pass we will report a problem.
	bool fSkippedStringAlt = false;
	for (int attempt = 0; attempt < 2; attempt++)
	{
		WIN32_FIND_DATA wfd;
		HANDLE hFind = ::FindFirstFile(strFilePattern.Chars(), &wfd);
		if (hFind != INVALID_HANDLE_VALUE)
		{
			StrApp strFile;
			do
			{
				strFile.Format(_T("%s\\%s"), strLangDir.Chars(), wfd.cFileName);
				int ich = strFile.ReverseFindStrCI(_T(".xml"));
				Assert(ich >= 0);
				if (strFile.Length() != ich + 4)
					continue;		// ignore, for example "xyz.xml~"
				fSkippedStringAlt |= ParseLanguageDefinitionFile(strFile, attempt != 0);
			} while (::FindNextFile(hFind, &wfd));
			::FindClose(hFind);
		}
		if (!fSkippedStringAlt)
			break; // no need to repeat.
	}
	// Finally, mark all languages as "clean" since they're all freshly loaded.
	int cws;
	CheckHr(m_qwsf->get_NumberOfWs(&cws));
	Vector<int> vws;
	vws.Resize(cws);
	CheckHr(m_qwsf->GetWritingSystems(vws.Begin(), cws));
	HRESULT hr;
	for (int iws = 0; iws < cws; iws++)
	{
		if (vws[iws] <= 0)
			continue;
		IWritingSystemPtr qws;
		CheckHr(hr = m_qwsf->get_EngineOrNull(vws[iws], &qws));
		if (hr == S_FALSE || !qws.Ptr())
			continue;
		CheckHr(qws->put_Dirty(FALSE));
	}
}


/*----------------------------------------------------------------------------------------------
	Initialize the writing system factory with the writing systems in the registry.
----------------------------------------------------------------------------------------------*/
void WpDa::InitWithWsInRegistry(HWND hwndMain)
{
	// First, bootstrap with the user interface writing system from the resources.  This will be
	// overridden from the registry settings if the user has tweaked that writing system.
	StrApp strUserWsXml(kstidUserWsXml);
	ParseXmlForWritingSystem(strUserWsXml);

	SetUpWsFactory();

	Vector<StrApp> vstrEncs;
	GetAllWsFromRegistry(vstrEncs);
	if (!vstrEncs.Size())
		return;				// Nothing more to do.

	// Create all the engines so that their writing system ids are available.
	for (int istr = 0; istr < vstrEncs.Size(); istr++)
	{
		// Ignore the obsolete writing system with an id code of 0.
		if (_tcscmp(vstrEncs[istr].Chars(), _T("___")) == 0)
			continue;
		StrUni stuOld(vstrEncs[istr]);
		StrUni stuISO = SilUtil::ConvertEthnologueToISO(stuOld.Chars());
		IWritingSystemPtr qws;
		CheckHr(m_qwsf->get_Engine(stuISO.Bstr(), &qws));
	}
	// Now, read the XML values for each encoding, and flesh out the barebones writing systems
	// created above.
	for (int istr = 0; istr < vstrEncs.Size(); istr++)
	{
		// Ignore the obsolete writing system with an id code of 0.
		if (_tcscmp(vstrEncs[istr].Chars(), _T("___")) == 0)
			continue;
		StrApp strDescr;
		GetWsDescription(vstrEncs[istr].Chars(), strDescr);
		ParseXmlForWritingSystem(strDescr);
	}

	DeleteRegWsKey();			// Delete all the old stuff, replace it with the new.
	PersistAllWs(hwndMain);
}

/*----------------------------------------------------------------------------------------------
	Set or update the descriptions of all the encodings in the registry.
----------------------------------------------------------------------------------------------*/
void WpDa::PersistAllWs(HWND hwndMain)
{
	SetUpWsFactory();
	int cws;
	CheckHr(m_qwsf->get_NumberOfWs(&cws));
	Vector<int> vws;
	vws.Resize(cws);
	CheckHr(m_qwsf->GetWritingSystems(vws.Begin(), cws));
	HRESULT hr;

	WaitCursor wc;

	AfProgressDlgPtr qprog;
	qprog.Create();
	qprog->DoModeless(hwndMain);
	StrApp strMsg(kstidSavingWsMsg);
	qprog->SetMessage(strMsg.Chars());
	StrApp strTitle(kstidSavingWsTitle);
	qprog->SetTitle(strTitle.Chars());
	qprog->SetRange(0, cws);
	qprog->SetStep(1);

	for (int iws = 0; iws < cws; iws++)
	{
		if (vws[iws] <= 0)
			continue;
		// Create or modify the Language Definition file if needed.
		IWritingSystemPtr qws;
		CheckHr(hr = m_qwsf->get_EngineOrNull(vws[iws], &qws));
		if (hr == S_FALSE || !qws.Ptr())
			continue;
		CheckHr(qws->SaveIfDirty(NULL));

		qprog->StepIt();
	}

	qprog->DestroyHwnd();
	qprog.Clear();
}

/*----------------------------------------------------------------------------------------------
	Rename and/or delete the given styles in all the strings in the data.
----------------------------------------------------------------------------------------------*/
void WpDa::RenameAndDeleteStyles(Vector<StrUni> & vstuOldNames, Vector<StrUni> & vstuNewNames,
	Vector<StrUni> & vstuDelNames)
{
	int ctss;
	CheckHr(get_VecSize(khvoText, kflidStText_Paragraphs, &ctss));
	for (int itss = 0; itss < ctss; itss++)
	{
		HVO hvoPara;
		CheckHr(get_VecItem(khvoText, kflidStText_Paragraphs, itss, &hvoPara));
		ITsTextPropsPtr qttp = NULL;
		IUnknownPtr qunkTtp;
		CheckHr(get_UnknownProp(hvoPara, kflidStPara_StyleRules, &qunkTtp));
		CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
		if (qttp)
		{
			ITsPropsBldrPtr qtpb;
			SmartBstr sbstrNamedStyle;
			CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrNamedStyle));
			StrUni stuOld(sbstrNamedStyle.Chars());
			StrUni stuNew;
			if (Delete(vstuDelNames, stuOld))
			{
				CheckHr(qttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, NULL));
			}
			else if (Rename(vstuOldNames, vstuNewNames, stuOld, stuNew))
			{
				CheckHr(qttp->GetBldr(&qtpb));
				SmartBstr sbstrNew(stuNew.Chars());
				CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, sbstrNew));
			}
			if (qtpb)
			{
				ITsTextPropsPtr qttpNew;
				CheckHr(qtpb->GetTextProps(&qttpNew));
				SetUnknown(hvoPara, kflidStPara_StyleRules, qttpNew);
			}
		}

		ITsStringPtr qtss;
		CheckHr(get_StringProp(hvoPara, kflidStTxtPara_Contents, &qtss));
		ITsStrBldrPtr qtsb = NULL;
		int crun;
		CheckHr(qtss->get_RunCount(&crun));
		for (int irun = 0; irun < crun; irun++)
		{
			TsRunInfo tri;
			CheckHr(qtss->FetchRunInfo(irun, &tri, &qttp));
			if (!qttp)
				continue;

			SmartBstr sbstrNamedStyle;
			CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrNamedStyle));
			StrUni stuOld(sbstrNamedStyle.Chars());
			StrUni stuNew;
			if (Delete(vstuDelNames, stuOld))
			{
				if (!qtsb)
					CheckHr(qtss->GetBldr(&qtsb));
				CheckHr(qtsb->SetStrPropValue(tri.ichMin, tri.ichLim, ktptNamedStyle,
					NULL));
			}
			else if (Rename(vstuOldNames, vstuNewNames, stuOld, stuNew))
			{
				if (!qtsb)
					CheckHr(qtss->GetBldr(&qtsb));
				CheckHr(qtsb->SetStrPropValue(tri.ichMin, tri.ichLim, ktptNamedStyle,
					stuNew.Bstr()));
			}

		}
		if (qtsb)
		{
			ITsStringPtr qtssNew;
			CheckHr(qtsb->GetString(&qtssNew));
			SetString(hvoPara, kflidStTxtPara_Contents, qtssNew);
		}
	}
}


bool WpDa::Delete(Vector<StrUni> & vstuDelNames, StrUni stu)
{
	for (int istu = 0; istu < vstuDelNames.Size(); istu++)
	{
		if (vstuDelNames[istu] == stu)
		{
			return true;
		}
	}
	return false;
}

bool WpDa::Rename(Vector<StrUni> & vstuOldNames, Vector<StrUni> & vstuNewNames,
	StrUni stuOld, StrUni & stuNew)
{
	Assert(vstuOldNames.Size() == vstuNewNames.Size());
	for (int istu = 0; istu < vstuOldNames.Size(); istu++)
	{
		if (vstuOldNames[istu] == stuOld)
		{
			stuNew = vstuNewNames[istu];
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Answer true if there are any strings in the document that use the given writing system.
----------------------------------------------------------------------------------------------*/
bool WpDa::AnyStringWithWs(int ws)
{
	int ctss;
	CheckHr(get_VecSize(1, kflidStText_Paragraphs, &ctss));

	for (int itss = 0; itss < ctss; itss++)
	{
		HVO hvoPara;
		CheckHr(get_VecItem(khvoText, kflidStText_Paragraphs, itss, &hvoPara));

		ITsStringPtr qtss;
		CheckHr(get_StringProp(hvoPara, kflidStTxtPara_Contents, &qtss));
		// Look at each run.
		int crun;
		CheckHr(qtss->get_RunCount(&crun));

		for (int irun = 0; irun < crun; ++irun)
		{
			ITsTextPropsPtr qttp;
			TsRunInfo tri;
			int encTmp, nVar;
			CheckHr(qtss->FetchRunInfo(irun, &tri, &qttp));
			CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &encTmp));

			if (encTmp == ws)
				return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Perform an Undo operation.
----------------------------------------------------------------------------------------------*/
bool WpDa::Undo()
{
	if (!m_qacth)
		return false;
	ComBool f;
	CheckHr(m_qacth->CanUndo(&f));
	if (!f)
		return false;
	UndoResult ures;
	CheckHr(m_qacth->Undo(&ures));
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return true if the Undo menu option should be enabled.
----------------------------------------------------------------------------------------------*/
bool WpDa::CanUndo()
{
	if (!m_qacth)
		return false;
	else
	{
		ComBool f;
		CheckHr(m_qacth->CanUndo(&f));
		return (bool)f;
	}
}

/*----------------------------------------------------------------------------------------------
	Perform a Redo operation.
----------------------------------------------------------------------------------------------*/
bool WpDa::Redo()
{
	if (!m_qacth)
		return false;
	ComBool f;
	CheckHr(m_qacth->CanRedo(&f));
	if (!f)
		return false;
	UndoResult ures;
	CheckHr(m_qacth->Redo(&ures));
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return true if the Redo menu option should be enabled.
----------------------------------------------------------------------------------------------*/
bool WpDa::CanRedo()
{
	if (!m_qacth)
		return false;
	else
	{
		ComBool f;
		CheckHr(m_qacth->CanRedo(&f));
		return (bool)f;
	}
}

/*----------------------------------------------------------------------------------------------
	Return true if the database has been changed since the last save.
----------------------------------------------------------------------------------------------*/
bool WpDa::IsDirty()
{
	return (m_soprMods.Size() > 0 || m_soperMods.Size() > 0 || m_shvoDeleted.Size() > 0);
}

/*----------------------------------------------------------------------------------------------
	Do what is necessary when something has changed.
	Maybe someday: tell the window so it can update its appearance.
----------------------------------------------------------------------------------------------*/
void WpDa::InformNowDirty()
{
}

/*----------------------------------------------------------------------------------------------
	Clear the information about changes since the last save.
----------------------------------------------------------------------------------------------*/
void WpDa::ClearChanges()
{
	ClearUndo();

	m_soprMods.Clear();
	m_soperMods.Clear();
	m_shvoDeleted.Clear();
	// Maybe someday: tell the window so it can update its appearance.
}

/*----------------------------------------------------------------------------------------------
	Set the writing system factory; create one if needed.
----------------------------------------------------------------------------------------------*/
void WpDa::SetUpWsFactory()
{
	if (!m_qwsf)
	{
		m_qwsf = WpApp::MainApp()->GetWsFactory();
		if (!m_qwsf)
		{
			m_qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get memory-based factory.
			WpApp::MainApp()->SetWsFactory(m_qwsf);
		}
	}
	AssertPtr(m_qwsf);
}
