/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FilPgSetDlg.cpp
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Implementation of the Paragraph PageSetUp Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	FilPgSetTssEdit methods
***********************************************************************************************/

void FilPgSetTssEdit::SetParent(FilPgSetDlg * pfpsdlg)
{
	m_pfpsdlg = pfpsdlg;
}

bool FilPgSetTssEdit::OnChange()
{
//todo	m_pfpsdlg->EditBoxChanged(this);
	return false;
}

void FilPgSetTssEdit::HandleSelectionChange(IVwSelection * pvwsel)
{
//todo	m_pfpsdlg->EditBoxChanged(this); // enable/disable the Find button
	SuperClass::HandleSelectionChange(pvwsel);
}

bool FilPgSetTssEdit::OnSetFocus(HWND hwndOld, bool fTbControl)
{
	SuperClass::OnSetFocus(hwndOld, fTbControl);
	m_pfpsdlg->EditBoxFocus(this);

	// Make this edit box be considered the current one, so the overlay palettes affect it.
	AfMainWnd * pafw = MainWindow();
	AssertPtrN(pafw);
	if (pafw)
		pafw->SetActiveRootBox(m_qrootb);

	return false;
}


/***********************************************************************************************
	Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
FilPgSetDlg::FilPgSetDlg(void)
{
	m_rid = kridFilPgSetDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/File/Page_Setup.htm");
	m_hndFilPgSetFont = NULL;
	m_hndFilPgSetPgNum = NULL;
	m_hndFilPgSetTotPg = NULL;
	m_hndFilPgSetDate = NULL;
	m_hndFilPgSetTime = NULL;
	m_hndFilPgSetTitle = NULL;
	m_hfontNumber = NULL;
}

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
FilPgSetDlg::~FilPgSetDlg()
{
	if (m_hfontNumber)
	{
		AfGdi::DeleteObjectFont(m_hfontNumber);
		m_hfontNumber = NULL;
	}
	if (m_hndFilPgSetFont)
	{
		AfGdi::DeleteObjectBitmap(m_hndFilPgSetFont);
		m_hndFilPgSetFont = NULL;
	}
	if (m_hndFilPgSetPgNum)
	{
		AfGdi::DeleteObjectBitmap(m_hndFilPgSetPgNum);
		m_hndFilPgSetPgNum = NULL;
	}
	if (m_hndFilPgSetTotPg)
	{
		AfGdi::DeleteObjectBitmap(m_hndFilPgSetTotPg);
		m_hndFilPgSetTotPg = NULL;
	}
	if (m_hndFilPgSetDate)
	{
		AfGdi::DeleteObjectBitmap(m_hndFilPgSetDate);
		m_hndFilPgSetDate = NULL;
	}
	if (m_hndFilPgSetTime)
	{
		AfGdi::DeleteObjectBitmap(m_hndFilPgSetTime);
		m_hndFilPgSetTime = NULL;
	}
	if (m_hndFilPgSetTitle)
	{
		AfGdi::DeleteObjectBitmap(m_hndFilPgSetTitle);
		m_hndFilPgSetTitle = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.
	A value that is out of range will be brought in range without complaint.
----------------------------------------------------------------------------------------------*/
void FilPgSetDlg::SetDialogValues(int nLMarg, int nRMarg, int nTMarg, int nBMarg, int nHEdge,
	int nFEdge, int nOrient, ITsString * ptssHeader, ITsString * ptssFooter,
	bool fHeaderOnFirstPage, int nPgH, int nPgW, PgSizeType sPgSize, MsrSysType nMsrSys,
	ITsString * ptssTitle)
{
	Assert(nOrient == kPort || nOrient == kLands);
	m_nMsrSys = nMsrSys;
	m_sPgSize = sPgSize;
	m_nOrient = nOrient;

	// The controls can handle TsString directly, just pass them.
	if (ptssHeader)
		m_qtssHeader = ptssHeader;
	else
		m_qtssHeader.Clear();

	if (ptssFooter)
		m_qtssFooter = ptssFooter;
	else
		m_qtssFooter.Clear();

	if (ptssTitle)
		m_qtssTitle = ptssTitle;
	else
		m_qtssTitle.Clear();

	m_nPgW = nPgW;
	m_nPgH = nPgH;
	m_nLMarg = nLMarg;
	m_nRMarg = nRMarg;
	m_nTMarg = nTMarg;
	m_nBMarg = nBMarg;
	m_nHEdge = nHEdge;
	m_nFEdge = nFEdge;
	m_fHeaderOnFirstPage = fHeaderOnFirstPage;
	m_bLastEditFt = false;
	return;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog.
----------------------------------------------------------------------------------------------*/
void FilPgSetDlg::GetDialogValues(int * pLMarg, int * pRMarg, int * pTMarg, int * pBMarg,
	int * pHEdge, int * pFEdge, POrientType * pOrient,
	ITsString ** pptssHeader, ITsString ** pptssFooter, bool * pfHeaderOnFirstPage,
	int * pPgH, int * pPgW, PgSizeType * pPgSize)
{
	AssertPtr(pLMarg);
	AssertPtr(pRMarg);
	AssertPtr(pTMarg);
	AssertPtr(pBMarg);
	AssertPtr(pHEdge);
	AssertPtr(pFEdge);
	AssertPtr(pOrient);
	AssertPtr(pptssHeader);
	AssertPtr(pptssFooter);
	AssertPtr(pPgH);
	AssertPtr(pPgW);
	AssertPtr(pPgSize);

	*pLMarg = m_nLMarg;
	*pRMarg = m_nRMarg;
	*pTMarg = m_nTMarg;
	*pBMarg = m_nBMarg;
	*pHEdge = m_nHEdge;
	*pFEdge = m_nFEdge;
	*pOrient = (POrientType) m_nOrient;

	// The controls handle TsStrings directly, just retrieve them.
	*pptssHeader = m_qtssHeader;
	AddRefObj(*pptssHeader);
	*pptssFooter = m_qtssFooter;
	AddRefObj(*pptssFooter);

	*pfHeaderOnFirstPage = m_fHeaderOnFirstPage;

	*pPgH = m_nPgH;
	*pPgW = m_nPgW;
	*pPgSize = m_sPgSize;
	return;
}

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)  This is also called to update the spin controls in the dialog.
----------------------------------------------------------------------------------------------*/
bool FilPgSetDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	StrAppBuf strb;
	// Load all bitmaps.
	HMODULE hModule = GetModuleHandle(NULL);
	HWND hButton;

	AfMainWnd * pafw = MainWindow();
	if (pafw && pafw->GetLpInfo())
	{
		AfDbInfo * pdbi = pafw->GetLpInfo()->GetDbInfo();
		AssertPtr(pdbi);
		pdbi->GetLgWritingSystemFactory(&m_qwsf);
	}
	else
	{
		// Get the registry-based factory.
		m_qwsf.CreateInstance(CLSID_LgWritingSystemFactory);
	}
	AssertPtr(m_qwsf);

	hButton = ::GetDlgItem(m_hwnd, kcidFilPgSetFont);
	m_hndFilPgSetFont = AfGdi::LoadImageBitmap(hModule, (LPCTSTR)kridFilPgSetFont,
		IMAGE_BITMAP, 0, 0, LR_LOADMAP3DCOLORS);
	::SendMessage(hButton, BM_SETIMAGE, (WPARAM)IMAGE_BITMAP, (LPARAM)m_hndFilPgSetFont);

	hButton = ::GetDlgItem(m_hwnd, kcidFilPgSetPgNum);
	m_hndFilPgSetPgNum = AfGdi::LoadImageBitmap(hModule, (LPCTSTR)kridFilPgSetPgNum, IMAGE_BITMAP, 0, 0,
		LR_LOADMAP3DCOLORS);
	::SendMessage(hButton, BM_SETIMAGE, (WPARAM)IMAGE_BITMAP, (LPARAM)m_hndFilPgSetPgNum);

	hButton = ::GetDlgItem(m_hwnd, kcidFilPgSetTotPg);
	m_hndFilPgSetTotPg = AfGdi::LoadImageBitmap(hModule, (LPCTSTR)kridFilPgSetTotPg, IMAGE_BITMAP, 0, 0,
		LR_LOADMAP3DCOLORS);
	::SendMessage(hButton, BM_SETIMAGE, (WPARAM)IMAGE_BITMAP, (LPARAM)m_hndFilPgSetTotPg);

	hButton = ::GetDlgItem(m_hwnd, kcidFilPgSetDate);
	m_hndFilPgSetDate = AfGdi::LoadImageBitmap(hModule, (LPCTSTR)kridFilPgSetDate, IMAGE_BITMAP, 0, 0,
		LR_LOADMAP3DCOLORS);
	::SendMessage(hButton, BM_SETIMAGE, (WPARAM)IMAGE_BITMAP, (LPARAM)m_hndFilPgSetDate);

	hButton = ::GetDlgItem(m_hwnd, kcidFilPgSetTime);
	m_hndFilPgSetTime = AfGdi::LoadImageBitmap(hModule, (LPCTSTR)kridFilPgSetTime, IMAGE_BITMAP, 0, 0,
		LR_LOADMAP3DCOLORS);
	::SendMessage(hButton, BM_SETIMAGE, (WPARAM)IMAGE_BITMAP, (LPARAM)m_hndFilPgSetTime);

	hButton = ::GetDlgItem(m_hwnd, kcidFilPgSetTitle);
	m_hndFilPgSetTitle = AfGdi::LoadImageBitmap(hModule, (LPCTSTR)kridFilPgSetTitle, IMAGE_BITMAP, 0, 0,
		LR_LOADMAP3DCOLORS);
	::SendMessage(hButton, BM_SETIMAGE, (WPARAM)IMAGE_BITMAP, (LPARAM)m_hndFilPgSetTitle);


	// Initialize values for the Page Size combo box.
	HWND hwndSize = ::GetDlgItem(m_hwnd, kcidFilPgSetSize);

	strb.Load(kstidFilPgSetDlgPgLtrStr);
	::SendMessage(hwndSize, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFilPgSetDlgPgLglStr);
	::SendMessage(hwndSize, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFilPgSetDlgPgA4Str);
	::SendMessage(hwndSize, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFilPgSetDlgPgCuStr);
	::SendMessage(hwndSize, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	// Initialize the spin controls.
	UDACCEL udAccel;
	udAccel.nSec = 0;

	switch (m_nMsrSys)
	{
	case kninches:
		udAccel.nInc = kSpnStpIn;
		break;
	case knmm:
		udAccel.nInc = kSpnStpMm;
		break;
	case kncm:
		udAccel.nInc = kSpnStpCm;
		break;
	//case knpt:
	//	udAccel.nInc = kSpnStpPt;
	//	break;
	default:
		Assert(false);	// We should never reach this.
	}

	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetMLS), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetMRS), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetMTS), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetMBS), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetEHS), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetEFS), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetWS), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetHS), UDM_SETACCEL, 1, (long)&udAccel);

	// Use Arial for the font, if nothing was passed in.
//	if (!m_hfontNumber) TODO why was this commented out?
	// TODO should destroy the olf font before changing m_hfontNumber
		m_hfontNumber = AfGdi::CreateFont(15, 0, 0, 0, FW_DONTCARE, 0, 0, 0, ANSI_CHARSET,
			OUT_TT_PRECIS, CLIP_TT_ALWAYS, DEFAULT_QUALITY, VARIABLE_PITCH | TMPF_TRUETYPE,
			_T("Arial"));
//	HWND hwndBullet = ::GetDlgItem(m_hwnd, kctidFbnCbBullet);
//	::SendMessage(hwndBullet, WM_SETFONT, (WPARAM)m_hfontBullet, 0);

	// set range of spin controls
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetMLS), UDM_SETRANGE32, kMarMin, kMarMax);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetMRS), UDM_SETRANGE32, kMarMin, kMarMax);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetMTS), UDM_SETRANGE32, kMarMin, kMarMax);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetMBS), UDM_SETRANGE32, kMarMin, kMarMax);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetEHS), UDM_SETRANGE32, kMarMin, kMarMax);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetEFS), UDM_SETRANGE32, kMarMin, kMarMax);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetWS), UDM_SETRANGE32, kPgMin, kPgMax);
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetHS), UDM_SETRANGE32, kPgMin, kPgMax);
	UpdateCtrls();

	/*------------------------------------------------------------------------------------------
		Initialize the header and footer Ts editbox controls.
	------------------------------------------------------------------------------------------*/

	m_qteHeader.Create();
	m_qteHeader->SetParent(this);
	int wsUser;
	CheckHr(m_qwsf->get_UserWs(&wsUser));
	m_qteHeader->SubclassEdit(m_hwnd, kcidFilPgSetHdE, m_qwsf, wsUser, WS_EX_CLIENTEDGE);

	m_qteLastFocus = m_qteHeader; // Defaults header as first text box "in focus".

	m_qteFooter.Create();
	m_qteFooter->SetParent(this);
	m_qteFooter->SubclassEdit(m_hwnd, kcidFilPgSetFtE, m_qwsf, wsUser, WS_EX_CLIENTEDGE);

	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetHdE), FW_EM_SETTEXT, 0,
		(LPARAM)m_qtssHeader.Ptr());
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetFtE), FW_EM_SETTEXT, 0,
		(LPARAM)m_qtssFooter.Ptr());

	return AfDialog::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Check Custom Page Size.  Returns True if Custom size.
----------------------------------------------------------------------------------------------*/
bool FilPgSetDlg::ChkCstSize()
{
	if (m_nOrient == kPort)
	{
		switch (m_sPgSize)
		{
		case kSzLtr:
			if ((m_nPgW == kPgWLtr) && (m_nPgH == kPgHLtr))
				return false;
			break;
		case kSzLgl:
			if ((m_nPgW == kPgWLgl) && (m_nPgH == kPgHLgl))
				return false;
			break;
		case kSzA4:
			if ((m_nPgW == kPgWA4) && (m_nPgH == kPgHA4))
				return false;
			break;
		default:
			break;
		}
	}
	else
	{
		switch (m_sPgSize)
		{
		case kSzLtr:
			if ((m_nPgW == kPgHLtr) && (m_nPgH == kPgWLtr))
				return false;
			break;
		case kSzLgl:
			if ((m_nPgW == kPgHLgl) && (m_nPgH == kPgWLgl))
				return false;
			break;
		case kSzA4:
			if ((m_nPgW == kPgHA4) && (m_nPgH == kPgWA4))
				return false;
			break;
		default:
			break;
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Update Dialog changes.
----------------------------------------------------------------------------------------------*/
bool FilPgSetDlg::UpdateCtrls()
{
	if (m_nOrient == kPort)
	{
		CheckDlgButton(m_hwnd, kcidFilPgSetPort, BST_CHECKED);
		CheckDlgButton(m_hwnd, kcidFilPgSetLand, BST_UNCHECKED);

		// Force page size contrls into range
		switch (m_sPgSize)
		{
		case kSzLtr:
			m_nPgW = kPgWLtr;
			m_nPgH = kPgHLtr;
			break;
		case kSzLgl:
			m_nPgW = kPgWLgl;
			m_nPgH = kPgHLgl;
			break;
		case kSzA4:
			m_nPgW = kPgWA4;
			m_nPgH = kPgHA4;
			break;
		default:
			m_sPgSize = kSzCust;
			m_nPgW = NBound(m_nPgW, kPgMin, kPgMax);
			m_nPgH = NBound(m_nPgH, kPgMin, kPgMax);
		}
	}
	else
	{
		CheckDlgButton(m_hwnd, kcidFilPgSetLand, BST_CHECKED);
		CheckDlgButton(m_hwnd, kcidFilPgSetPort, BST_UNCHECKED);
		// Force page size contrls into range
		switch (m_sPgSize)
		{
		case kSzLtr:
			m_nPgW = kPgHLtr;
			m_nPgH = kPgWLtr;
			break;
		case kSzLgl:
			m_nPgW = kPgHLgl;
			m_nPgH = kPgWLgl;
			break;
		case kSzA4:
			m_nPgW = kPgHA4;
			m_nPgH = kPgWA4;
			break;
		default:
			m_sPgSize = kSzCust;
			m_nPgW = NBound(m_nPgW, kPgMin, kPgMax);
			m_nPgH = NBound(m_nPgH, kPgMin, kPgMax);
		}
	}


	// Select the correct page size in combo box
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetSize), CB_SETCURSEL, m_sPgSize, 0);

	// Force these values into range.
	m_nLMarg = NBound(m_nLMarg, kMarMin, kMarMax);
	m_nRMarg = NBound(m_nRMarg, kMarMin, kMarMax);
	m_nTMarg = NBound(m_nTMarg, kMarMin, kMarMax);
	m_nBMarg = NBound(m_nBMarg, kMarMin, kMarMax);
	m_nHEdge = NBound(m_nHEdge, kEdgeMin, kMarMax);
	m_nFEdge = NBound(m_nFEdge, kEdgeMin, kMarMax);


	// set values in spin edit controls
	HWND hwndEdit;
	HWND hwndSpin;

	hwndEdit = ::GetDlgItem(m_hwnd, kcidFilPgSetMLE);
	hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetMLS);
	UpdateEditBox(hwndSpin, hwndEdit, m_nLMarg);

	hwndEdit = ::GetDlgItem(m_hwnd, kcidFilPgSetMRE);
	hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetMRS);
	UpdateEditBox(hwndSpin, hwndEdit, m_nRMarg);

	hwndEdit = ::GetDlgItem(m_hwnd, kcidFilPgSetMTE);
	hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetMTS);
	UpdateEditBox(hwndSpin, hwndEdit, m_nTMarg);

	hwndEdit = ::GetDlgItem(m_hwnd, kcidFilPgSetMBE);
	hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetMBS);
	UpdateEditBox(hwndSpin, hwndEdit, m_nBMarg);

	hwndEdit = ::GetDlgItem(m_hwnd, kcidFilPgSetEHE);
	hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetEHS);
	UpdateEditBox(hwndSpin, hwndEdit, m_nHEdge);

	hwndEdit = ::GetDlgItem(m_hwnd, kcidFilPgSetEFE);
	hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetEFS);
	UpdateEditBox(hwndSpin, hwndEdit, m_nFEdge);

	hwndEdit = ::GetDlgItem(m_hwnd, kcidFilPgSetWE);
	hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetWS);
	UpdateEditBox(hwndSpin, hwndEdit, m_nPgW);

	hwndEdit = ::GetDlgItem(m_hwnd, kcidFilPgSetHE);
	hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetHS);
	UpdateEditBox(hwndSpin, hwndEdit, m_nPgH);

	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetHdE), FW_EM_SETTEXT, 0,
		(LPARAM)m_qtssHeader.Ptr());
	::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetFtE), FW_EM_SETTEXT, 0,
		(LPARAM)m_qtssFooter.Ptr());

	::CheckDlgButton(m_hwnd, kcidFilPgSetShowHdr,
		m_fHeaderOnFirstPage ? BST_CHECKED : BST_UNCHECKED);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle a change in a combo box.
----------------------------------------------------------------------------------------------*/
bool FilPgSetDlg::OnComboChange(NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	// Get the current index from the combo box.
	int icb = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);

	m_sPgSize = (PgSizeType)icb;

	// Update dialog controls.
	UpdateCtrls();

	lnRet = 0;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handles a click on a spin control.
----------------------------------------------------------------------------------------------*/
bool FilPgSetDlg::OnDeltaSpin(NMHDR * pnmh, long & lnRet)
{
	// If the edit box has changed and is out of synch with the spin control, this
	// will update the spin's position to correspond to the edit box.
	StrAppBuf strb;
	strb.SetLength(strb.kcchMaxStr);
	HWND hwndEdit;
	HWND hwndSpin;

	// Get handle for the edit and spin controls.
	if (pnmh->code == UDN_DELTAPOS)
	{
		// Called from a spin control.
		hwndSpin = pnmh->hwndFrom;
		hwndEdit = (HWND)::SendMessage(hwndSpin, UDM_GETBUDDY, 0, 0);
	}
	else
	{
		// Called from an edit control.
		hwndEdit = pnmh->hwndFrom;
		switch (pnmh->idFrom)
		{
		case kcidFilPgSetMLE:
			hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetMLS);
			break;
		case kcidFilPgSetMRE:
			hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetMRS);
			break;
		case kcidFilPgSetMTE:
			hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetMTS);
			break;
		case kcidFilPgSetMBE:
			hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetMBS);
			break;
		case kcidFilPgSetEHE:
			hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetEHS);
			break;
		case kcidFilPgSetEFE:
			hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetEFS);
			break;
		case kcidFilPgSetWE:
			hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetWS);
			break;
		case kcidFilPgSetHE:
			hwndSpin = ::GetDlgItem(m_hwnd, kcidFilPgSetHS);
			break;
		default:
			Assert(false);
		}
	}

	// Get the text from the edit box and convert it to a number.
	int cch = ::SendMessage(hwndEdit, WM_GETTEXT, strb.kcchMaxStr, (LPARAM)strb.Chars());
	strb.SetLength(cch);
	int nValue = 0;
	AfUtil::GetStrMsrValue(&strb, m_nMsrSys, &nValue);

	if (pnmh->code == UDN_DELTAPOS)
	{
		if (m_nMsrSys == kninches)
		{
			// If nValue is not already a whole increment of nDelta, then we only increment it
			// enough to make it a whole increment. If already a whole increment, then we go
			// ahead and increment it the entire amount. Thus if the increment is 0.25" and the
			// original value was 0.15", the first click on the arrow will bring it to 0.25; the
			// next click to 0.50".
			int nDelta = ((NMUPDOWN *)pnmh)->iDelta;
			int nPartialIncrement = nValue % nDelta;
			if (nPartialIncrement && nDelta > 0)
				nValue += (nDelta - nPartialIncrement);
			else if (nPartialIncrement && nDelta < 0)
				nValue -= nPartialIncrement;
			else
				nValue += nDelta;
		}
		else
			nValue += ((NMUPDOWN *)pnmh)->iDelta;
	}


	// Update the appropriate member variable.
	switch (pnmh->idFrom)
	{
	case kcidFilPgSetMLE:
	case kcidFilPgSetMLS:
		m_nLMarg = nValue;
		break;
	case kcidFilPgSetMRE:
	case kcidFilPgSetMRS:
		m_nRMarg = nValue;
		break;
	case kcidFilPgSetMTE:
	case kcidFilPgSetMTS:
		m_nTMarg = nValue;
		break;
	case kcidFilPgSetMBE:
	case kcidFilPgSetMBS:
		m_nBMarg = nValue;
		break;
	case kcidFilPgSetEHE:
	case kcidFilPgSetEHS:
		m_nHEdge = nValue;
		break;
	case kcidFilPgSetEFE:
	case kcidFilPgSetEFS:
		m_nFEdge = nValue;
		break;
	case kcidFilPgSetWE:
	case kcidFilPgSetWS:
		m_nPgW = nValue;
		if (ChkCstSize())
			m_sPgSize = kSzCust;
		break;
	case kcidFilPgSetHE:
	case kcidFilPgSetHS:
		m_nPgH = nValue;
		if (ChkCstSize())
			m_sPgSize = kSzCust;
		break;
	default:
		Assert(false);	// We should never reach this.
	}

	// Update dialog controls.
	UpdateCtrls();

	lnRet = 0;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Update the value in the Edit Box.
----------------------------------------------------------------------------------------------*/
void FilPgSetDlg::UpdateEditBox(HWND hwndSpin, HWND hwndEdit, int nValue)
{
	StrAppBuf strb;
	strb.SetLength(strb.kcchMaxStr);

	// Don't exceed the minimum or maximum values in the spin control.
	int nRangeMin = 0;
	int nRangeMax = 0;
	::SendMessage(hwndSpin, UDM_GETRANGE32, (long)&nRangeMin, (long)&nRangeMax);
	nValue = NBound(nValue, nRangeMin, nRangeMax);

	AfUtil::MakeMsrStr(nValue, m_nMsrSys, &strb);

	::SendMessage(hwndEdit, WM_SETTEXT, 0, (LPARAM)strb.Chars());
}

void FilPgSetDlg::InsertButtonId(HWND hwndct, int stid)
{
	StrUni stu;
	stu.Load(stid);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);


	int wsUser;
	CheckHr(m_qwsf->get_UserWs(&wsUser));
	ITsStringPtr qtssText;
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), wsUser, &qtssText);

	InsertButtonText(hwndct, qtssText);
}

/*----------------------------------------------------------------------------------------------
	Insert psz text into the header or footer "control".

	Called when one of the insert buttons on the File->Page Setup dialog has been pressed.

	@param hwndct The control's window handle (for the header or footer text box).
	@param ptssText pointer to the ITsString to be inserted.
----------------------------------------------------------------------------------------------*/
void FilPgSetDlg::InsertButtonText(HWND hwndct, ITsString * ptssText)
{
	// Replace selected text with the psz text.
	::SendMessage(hwndct, FW_EM_REPLACESEL, 0, (LPARAM)ptssText);

	if (m_bLastEditFt)
		::SendMessage(hwndct, FW_EM_GETTEXT, 0, (LPARAM)&m_qtssFooter);
	else
		::SendMessage(hwndct, FW_EM_GETTEXT, 0, (LPARAM)&m_qtssHeader);

	int ichSel;
	::SendMessage(hwndct, EM_GETSEL, (WPARAM) &ichSel, NULL);
	::SetFocus(hwndct); // Selects the whole string, unfortunately.
	// Set it back to what it was after the replacement.
	::SendMessage(hwndct, EM_SETSEL, ichSel, ichSel);
}


/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool FilPgSetDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	int nTemp;
	StrAppBuf strb;
	StrAppBuf strb2;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		{
			HWND hwndct;  // control (ct) handle (hwnd)
			if (m_bLastEditFt)
				hwndct = ::GetDlgItem(m_hwnd, kcidFilPgSetFtE);
			else
				hwndct = ::GetDlgItem(m_hwnd, kcidFilPgSetHdE);

			switch (pnmh->idFrom)
			{
			case kcidFmtFnt:
				return OnFontChange(pnmh, lnRet);

			case kcidFilPgSetFont:
				return OnFontChange(pnmh, lnRet);

			case kcidFilPgSetPgNum:
				InsertButtonId(hwndct, kstidFilPgSetDlgPage);
				return false;

			case kcidFilPgSetTotPg:
				InsertButtonId(hwndct, kstidFilPgSetDlgPages);
				return false;

			case kcidFilPgSetDate:
				InsertButtonId(hwndct, kstidFilPgSetDlgDate);
				return false;

			case kcidFilPgSetTime:
				InsertButtonId(hwndct, kstidFilPgSetDlgTime);
				return false;

			case kcidFilPgSetTitle:
				InsertButtonText(hwndct, m_qtssTitle);
				return false;

			case kcidFilPgSetPort:
				if (IsDlgButtonChecked(m_hwnd, kcidFilPgSetPort) == BST_CHECKED)
				{
					if (m_nOrient != kPort)
					{
						m_nOrient = kPort;
						nTemp = m_nPgH;
						m_nPgH = m_nPgW;
						m_nPgW = nTemp;
						UpdateCtrls();
					}
				}
				return false;

			case kcidFilPgSetLand:
				if (IsDlgButtonChecked(m_hwnd, kcidFilPgSetLand) == BST_CHECKED)
				{
					if (m_nOrient != kLands)
					{
						m_nOrient = kLands;
						nTemp = m_nPgH;
						m_nPgH = m_nPgW;
						m_nPgW = nTemp;
						UpdateCtrls();
					}
				}
				return false;

			case kcidFilPgSetDef:
				strb.Load(kstridFilPgSetDlgDefQ);
				strb2.Load(kstridFilPgSetDlgDefT);
				if (IDYES == MessageBox(m_hwnd,strb.Chars(), strb2.Chars(),
					MB_YESNO | MB_ICONQUESTION | MB_DEFBUTTON2))
				{
					// Set default values
					m_sPgSize = kSzLtr;		// Size of the page.
					m_nLMarg = kDefnLMarg;	// Width of Left Margin in units of 1/72000"
					m_nRMarg = kDefnRMarg;	// Width of Right Margin in units of 1/72000"
					m_nTMarg = kDefnTMarg;	// Height of Top Margin in units of 1/72000"
					m_nBMarg = kDefnBMarg;	// Height of Bottom Margin in units of 1/72000"
					m_nHEdge = kDefnHEdge;	// Height of Header from edge in units of 1/72000"
					m_nFEdge = kDefnFEdge;	// Height of Footer from edge in units of 1/72000"
					m_nOrient = kPort;		// Orientation of page

					m_qtssHeader = m_qtssTitle;

					StrUni stu;
					stu.Load(kstidFilPgSetDefaultFooter);
					ITsStrFactoryPtr qtsf;
					qtsf.CreateInstance(CLSID_TsStrFactory);

					int wsUser;
					CheckHr(m_qwsf->get_UserWs(&wsUser));
					qtsf->MakeStringRgch(stu.Chars(), stu.Length(), wsUser, &m_qtssFooter);

					m_fHeaderOnFirstPage = false;
					UpdateCtrls();
				}

				return false;

			case kcidFilPgSetShowHdr:
				m_fHeaderOnFirstPage = (::IsDlgButtonChecked(m_hwnd, kcidFilPgSetShowHdr) ==
					BST_CHECKED);
				break;

			default:
				return AfWnd::OnNotifyChild(ctid, pnmh, lnRet);
			}
		}
		break;

	case UDN_DELTAPOS: // Spin control is activated.
		return OnDeltaSpin(pnmh, lnRet);

	case EN_SETFOCUS: // Edit control modified.
	{
		SuperClass::OnNotifyChild(ctid, pnmh, lnRet); // turn on default keyboard

		switch (pnmh->idFrom)
		{
		case kcidFilPgSetMLE:
		case kcidFilPgSetMRE:
		case kcidFilPgSetMTE:
		case kcidFilPgSetMBE:
		case kcidFilPgSetEHE:
		case kcidFilPgSetEFE:
		case kcidFilPgSetWE:
		case kcidFilPgSetHE:
		case kcidFilPgSetHdE:
		case kcidFilPgSetFtE:
			::SendMessage(::GetDlgItem(m_hwnd, pnmh->idFrom), EM_SETSEL, (WPARAM)0, (LPARAM)-1);
		}
		break;
	}
	case EN_KILLFOCUS: // Edit control modified.
		switch (pnmh->idFrom)
		{
		case kcidFilPgSetMLE:
		case kcidFilPgSetMRE:
		case kcidFilPgSetMTE:
		case kcidFilPgSetMBE:
		case kcidFilPgSetEHE:
		case kcidFilPgSetEFE:
		case kcidFilPgSetWE:
		case kcidFilPgSetHE:
			return OnDeltaSpin(pnmh, lnRet);

		case kcidFilPgSetHdE:
			{
				m_bLastEditFt = false;
				::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetHdE), FW_EM_GETTEXT, 0,
					(LPARAM)&m_qtssHeader);
			}
			return true;

		case kcidFilPgSetFtE:
			{
				m_bLastEditFt = true;
				::SendMessage(::GetDlgItem(m_hwnd, kcidFilPgSetFtE), FW_EM_GETTEXT, 0,
					(LPARAM)&m_qtssFooter);
			}
			return true;
		}
		return false;

	case CBN_SELCHANGE: // Combo box item changed.
		return OnComboChange(pnmh, lnRet);
	// Default is do nothing.
	}

	return AfWnd::OnNotifyChild(ctid, pnmh, lnRet);
}
/*----------------------------------------------------------------------------------------------
	Supports the user choosing a different font and color for displaying the number label for
	a kltNumber list.

	Calls the FieldWorks font dialog to manipulate the TsTextProps used to control the preview.
----------------------------------------------------------------------------------------------*/
bool FilPgSetDlg::OnFontChange(NMHDR * pnmh, long & lnRet)
{
	lnRet = 0;

	AssertPtr(pnmh);

	IVwSelectionPtr qvwsel;
	TtpVec vqttpSel;
	VwPropsVec vqvpsSel;
	VwPropsVec vqvpsSoft;

	if (!m_qteLastFocus->GetCharacterProps(&qvwsel, vqttpSel, vqvpsSel))
		return false;

	int cttp = vqttpSel.Size();

	vqttpSel.Resize(cttp);
	vqvpsSoft.Resize(cttp);

	CheckHr(qvwsel->GetHardAndSoftCharProps(cttp, (ITsTextProps **)vqttpSel.Begin(),
		(IVwPropertyStore **)vqvpsSoft.Begin(), &cttp));

//todo	ISilDataAccessPtr qsda;
//todo	BeginUndoTask(pcmd->m_cid, &qsda);

	if (FmtFntDlg::AdjustTsTextProps(m_hwnd, vqttpSel, vqvpsSoft, m_qwsf, m_pszHelpFile,
		false, false, false))
	{
		// Some change was made.

		CheckHr(qvwsel->SetSelectionProps(cttp, (ITsTextProps **)vqttpSel.Begin()));
	}

//todo	EndUndoTask(qsda);

	return true;
}
/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool FilPgSetDlg::OnApply(bool fClose)
{
	StrApp strMsg;
	if (m_nPgW < m_nLMarg + kMinTxt + m_nRMarg)
	{
		strMsg.Load(kstridFilPgSetDlgLRMar);
		MessageBox(m_hwnd, strMsg.Chars(), NULL, MB_OK);
		::SetFocus(::GetDlgItem(m_hwnd, kcidFilPgSetMLE));
		return true;
	}

	if (m_nPgH < m_nTMarg + kMinTxt + m_nBMarg)
	{
		strMsg.Load(kstridFilPgSetDlgTBMar);
		MessageBox(m_hwnd, strMsg.Chars(), NULL, MB_OK);
		::SetFocus(::GetDlgItem(m_hwnd, kcidFilPgSetMTE));
		return true;
	}

	StrApp strTitle;
	// 7200 = 0.1 inches ??
	if (m_nHEdge + 7200 > m_nTMarg)
	{
		strMsg.Load(kstridFilPgSetDlgEHMar);
		strTitle.Load(kstidAdjustHeader);
		MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_ICONEXCLAMATION);
		::SetFocus(::GetDlgItem(m_hwnd, kcidFilPgSetEHE));
		return true;
	}

	if (m_nFEdge + 7200 > m_nBMarg)
	{
		strMsg.Load(kstridFilPgSetDlgEFMar);
		strTitle.Load(kstidAdjustFooter);
		MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_ICONEXCLAMATION);
		::SetFocus(::GetDlgItem(m_hwnd, kcidFilPgSetEFE));
		return true;
	}

	return AfDialog::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool FilPgSetDlg::OnCancel()
{
	return AfDialog::OnCancel();
}

/*----------------------------------------------------------------------------------------------
	Note that the specified edit control has the focus.
----------------------------------------------------------------------------------------------*/
void FilPgSetDlg::EditBoxFocus(FilPgSetTssEdit * pte)
{
	// Remember which one has the focus so we can properly apply commands like Format.
	m_qteLastFocus = pte;

	// We wil make the root box in the other pane disabled, otherwise its selection continues to
	// show up if it is a range.
	FilPgSetTssEdit * pteOther;
	if (pte == m_qteHeader)
		pteOther = m_qteFooter;
	else
		pteOther = m_qteHeader;
	IVwRootBoxPtr qrootb;
	pte->get_RootBox(&qrootb);
	ITsStringPtr qtss;
	CheckHr(pte->GetText(&qtss));
	int cch = 0;
	if (qtss)
		CheckHr(qtss->get_Length(&cch));
	if (!cch)
	{
		// Empty: Standard windows dialog stuff does not guarantee to make a selection.
		// If there is one, it must be an IP in the empty string, so don't mess with it
		// (the user might have set some formatting properties on it!).
		// But if there isn't one, make one.
		IVwSelectionPtr qvwsel;
		CheckHr(qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
			qrootb->MakeSimpleSel(true, true, false, true, NULL);
	}
	if (qrootb)
		CheckHr(qrootb->Activate(vssEnabled));
	if (!pteOther) // Can be NULL, during start-up at least.
		return;
	pteOther->get_RootBox(&qrootb);
	if (!qrootb)
		return; // It certainly can't have a selection, then!
	CheckHr(qrootb->Activate(vssDisabled));
}

/*----------------------------------------------------------------------------------------------
	Handle getting focus by passing it to the appropriate edit box.
----------------------------------------------------------------------------------------------*/
bool FilPgSetDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_SETFOCUS && m_qteLastFocus)
	{
		::SetFocus(m_qteLastFocus->Hwnd());
		return true;
	}
// todo
/*	else if (wm == WM_ACTIVATE)
	{
		if (LOWORD(wp) == WA_INACTIVE)
		{
			// Deactivating. Remove this from list of command handlers.
			AfApp::Papp()->RemoveCmdHandler(this, 1);
			// Also remove our special accelerator table.
			AfApp::Papp()->RemoveAccelTable(m_atid);
			// Ideally, we should remove the reference to the last root box, but we keep it
			// around so that bringing up the debugger doesn't confuse things.
			//m_qvrsLast.Clear();
		}
		else
		{
			// Activating. Keep track of the last root site for when we actually do the search.
			// This needs to be done before the focus is set to an edit box, because setting
			// the focus will update the root site.
			AfMainWnd * pafw = MainWindow();
			AssertPtrN(pafw);
			AfVwRootSitePtr qvrs;
			if (pafw)
				pafw->GetActiveViewWindow(&qvrs, NULL, false);
			if (!dynamic_cast<FindTssEdit *>(qvrs.Ptr()))
				m_qvrsLast = qvrs;
			// else: leave as is. This is a safety net for when reactivating the Find
			// dialog after bringing up the debugger.

			// If we previously had an edit box in focus, restore it.
			if (m_qteLastFocus)
				::SetFocus(m_qteLastFocus->Hwnd());
			// Make ourself an active command handler.
			AfApp::Papp()->AddCmdHandler(this, 1, kgrfcmmAll);
			// We load the basic accelerator table so that these commands can be directed to this
			// window. This allows the embedded TssEdit controls to see the commands. Otherwise, if
			// they are translated by the main window, the main window is the 'target', and the
			// command handlers on AfVwRootSite don't work, because the root site is not a child
			// window of the main one.
			// Note that we don't just create it once and use SetAccelHwnd, because the active
			// main window can change while the find dialog is open, and we need to install
			// into the active menu manager.
			if (AfApp::Papp())
				m_atid = AfApp::Papp()->LoadAccelTable(kridAccelBasic, 0, m_hwnd);
		}
	}
*/
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}
