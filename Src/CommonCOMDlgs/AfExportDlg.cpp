/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfExportDlg.cpp
Responsibility: Steve McConnel
Last reviewed: never.

Description:
	Implementation of the File / Export dialog classes for FieldWorks.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include <direct.h>
#include <io.h>
#include "xmlparse.h"
#include <msxml2.h>

#undef THIS_FILE
DEFINE_THIS_FILE

#undef DUMP_VIEW_DEFINITION
//#define DUMP_VIEW_DEFINITION 1

#define READ_SIZE 16384

//:>********************************************************************************************
//:>	AfExportDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfExportDlg::AfExportDlg()
{
	m_rid = kridExportDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/File/Export/Export.htm");
	m_flidSubItems = 0;
	m_iess = 0;
	m_nErrorLine = 0;
	m_nErrorCode = 0;
	m_fCancelled = false;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool AfExportDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the default output style and options, and disable any inappropriate output styles or
	// options.

	AssertPtr(m_plpi);
	AfDbInfo * pdbi = m_plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetLgWritingSystemFactory(&m_qwsf);
	AssertPtr(m_qwsf);

	UserViewSpecVec & vuvs = pdbi->GetUserViewSpecs();
	Assert(vuvs.Size() >= 1);
	m_puvs = NULL;
	for (int iuvs = 0; iuvs < vuvs.Size(); ++iuvs)
	{
		if (vuvs[iuvs]->m_vwt == m_vwt)
		{
			m_puvs = vuvs[iuvs];
			break;
		}
	}
	AssertPtr(m_puvs);
	GetExportOptions();
	if (!m_vess.Size())
	{
		StrApp strCaption(kstidExportMsgCaption);
		StrApp strMsg(kstidExportErrorFilesGone);
		::MessageBox(m_hwnd, strMsg.Chars(), strCaption.Chars(), MB_OK | MB_ICONINFORMATION);
		OnCancel();
		return false;
	}

	m_fSelectionOnly = false;
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidExportSelectionOnly);
	if (hwnd)
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
		// For Version 1, disable and hide this checkbox control.
		::EnableWindow(hwnd, false);
		::ShowWindow(hwnd, SW_HIDE);
	}

	m_fOpenWhenDone = true;
	::SendMessage(::GetDlgItem(m_hwnd, kctidExportOpenWhenDone), BM_SETCHECK, BST_CHECKED, 0);

	achar szFile[MAX_PATH+1];
	achar szDir[MAX_PATH+1];
	::ZeroMemory(szFile, MAX_PATH+1);
	::ZeroMemory(szDir, MAX_PATH+1);
	GetDefaultFileAndDir(szFile, MAX_PATH+1, szDir, MAX_PATH+1);
	::SetDlgItemText(m_hwnd, kcidExportFolder, szDir);
	::SetDlgItemText(m_hwnd, kctidExportFilename, szFile);

	::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), m_strPathname.Length() != 0);

	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	m_iess = 0;
	StrApp strFilt;
	if (m_fws.GetString(NULL, _T("LatestExportFilter"), strFilt))
	{
		int iess;
		for (iess = 0; iess < m_vess.Size(); ++iess)
		{
			if (strFilt == m_vess[iess].m_strTitle)
			{
				m_iess = iess;
				break;
			}
		}
	}
	AdjustFilename();
	::SendMessage(::GetDlgItem(m_hwnd, kctidExportType), CB_SETCURSEL, m_iess, 0);
	return true;
}

/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes made in the dialog are accepted.
	The default OnApply closes the dialog.

	@param fClose not used here

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool AfExportDlg::OnApply(bool fClose)
{
	StrApp strCaption(kstidExportMsgCaption);
	StrApp strMsg(kstidExportReallySlow);
	int nRet = ::MessageBox(m_hwnd, strMsg.Chars(), strCaption.Chars(),
		MB_YESNO | MB_ICONQUESTION);
	if (nRet != IDYES)
		return false;

	// Create path from dialog values
	achar szFile[MAX_PATH+1];
	achar szDir[MAX_PATH+1];
	::ZeroMemory(szFile, MAX_PATH+1);
	::ZeroMemory(szDir, MAX_PATH+1);
	::GetDlgItemText(m_hwnd, kcidExportFolder, szDir, MAX_PATH);
	::GetDlgItemText(m_hwnd, kctidExportFilename, szFile, MAX_PATH);
	if (szFile[0] == '\\' || szFile[1] == ':')
		m_strPathname.Assign(szFile);
	else
		m_strPathname.Format(_T("%s%s"), szDir, szFile);


	// Add the extension if needed.
	int ichFile = m_strPathname.ReverseFindCh('\\');
	int ichExt = m_strPathname.ReverseFindCh('.');
	if (ichExt <= ichFile)
	{
		StrApp & strExt = m_vess[m_iess].m_strOutputExt;
		m_strPathname.FormatAppend(_T(".%s"), strExt.Chars());
	}
	HANDLE hFile = ::CreateFile(m_strPathname.Chars(), GENERIC_READ, FILE_SHARE_READ, NULL,
		OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile != INVALID_HANDLE_VALUE)
	{
		::CloseHandle(hFile);
		// Ask if the user wants to overwrite this file.  If not, return to the dialog.
		StrApp strFmt(kstidExportFileAlreadyFmt);
		achar szFile[MAX_PATH+1];
		::GetDlgItemText(m_hwnd, kctidExportFilename, szFile, isizeof(szFile) / isizeof(achar));
		strMsg.Format(strFmt.Chars(), szFile);
		nRet = ::MessageBox(m_hwnd, strMsg.Chars(), strCaption.Chars(),
			MB_YESNO | MB_ICONQUESTION);
		if (nRet != IDYES)
			return false;
	}
	else
	{
		// Check whether the file exists -- if so, it must be opened by another application, so
		// we can't use it.
		WIN32_FILE_ATTRIBUTE_DATA wfad;
		if (::GetFileAttributesEx(m_strPathname.Chars(), GetFileExInfoStandard, &wfad))
		{
			// "The file %<0>s is already open.%n"
			// "Please close it if you want to overwrite this file.%n"
			// "Otherwise, use another filename."
			StrApp strFmt(kstidExportFileAlreadyOpenFmt);
			StrApp strMsg;
			achar szFile[MAX_PATH+1];
			::GetDlgItemText(m_hwnd, kctidExportFilename, szFile,
				isizeof(szFile) / isizeof(achar));
			strMsg.Format(strFmt.Chars(), szFile);
			::MessageBox(m_hwnd, strMsg.Chars(), strCaption.Chars(),
				MB_OK | MB_ICONINFORMATION);
			return false;
		}
	}
	m_fws.SetString(NULL, _T("LatestExportFile"), m_strPathname);
	m_fws.SetString(NULL, _T("LatestExportFilter"), m_vess[m_iess].m_strTitle);
	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool AfExportDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	StrApp strCaption(kstidExportMsgCaption);
	HWND hwndFrom = ::GetDlgItem(m_hwnd, ctidFrom);
	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidExportSelectionOnly)
		{
			m_fSelectionOnly = !m_fSelectionOnly;
			if (m_fSelectionOnly)
				::SendMessage(hwndFrom, BM_SETCHECK, BST_CHECKED, 0);
			else
				::SendMessage(hwndFrom, BM_SETCHECK, BST_UNCHECKED, 0);
		}
		else if (ctidFrom == kctidExportOpenWhenDone)
		{
			m_fOpenWhenDone = !m_fOpenWhenDone;
			if (m_fOpenWhenDone)
				::SendMessage(hwndFrom, BM_SETCHECK, BST_CHECKED, 0);
			else
				::SendMessage(hwndFrom, BM_SETCHECK, BST_UNCHECKED, 0);
		}
		else if (ctidFrom == kctidExportBrowse)
		{
			// Open file dialog.
			achar szFile[MAX_PATH+1];
			achar szDir[MAX_PATH+1];
			::ZeroMemory(szFile, MAX_PATH+1);
			::ZeroMemory(szDir, MAX_PATH+1);
			GetDefaultFileAndDir(szFile, MAX_PATH+1, szDir, MAX_PATH+1);
			OPENFILENAME ofn;
			::ZeroMemory(&ofn, isizeof(OPENFILENAME));
			// the constant below is required for compatibility with Windows 95/98 (and maybe
			// NT4)
			ofn.lStructSize = OPENFILENAME_SIZE_VERSION_400;
			ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_HIDEREADONLY;
			ofn.hwndOwner = m_hwnd;
			ofn.lpstrFilter = m_strFilter.Chars();
			ofn.lpstrFile = szFile;
			ofn.nMaxFile = MAX_PATH;
			ofn.lpstrInitialDir = szDir;
			ofn.nFilterIndex = m_iess + 1;		// Go from 0-based to 1-based index.
			if (::GetSaveFileName(&ofn) == IDOK && ofn.lpstrFile[0])
			{
				m_strPathname = ofn.lpstrFile;
				GetDefaultFileAndDir(szFile, MAX_PATH+1, szDir, MAX_PATH+1);
				::SetDlgItemText(m_hwnd, kcidExportFolder, szDir);
				::SetDlgItemText(m_hwnd, kctidExportFilename, szFile);
				::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), TRUE);
				m_iess = ofn.nFilterIndex - 1;	// Go from 1-based to 0-based index.
				::SendMessage(::GetDlgItem(m_hwnd, kctidExportType), CB_SETCURSEL, m_iess, 0);
				AdjustFilename();
			}
		}
		break;
	case CBN_SELCHANGE:
		if (ctidFrom == kctidExportType)
		{
			int iType = ::SendMessage(hwndFrom, CB_GETCURSEL, 0, 0);
			if (iType != CB_ERR && iType != m_iess)
			{
				m_iess = iType;
				AdjustFilename();
			}
		}
		break;
	case EN_CHANGE:
		if (ctidFrom == kctidExportFilename)
		{
			achar szFile[MAX_PATH+1];
			::ZeroMemory(szFile, MAX_PATH+1);
			UINT cch = ::GetDlgItemText(m_hwnd, kctidExportFilename, szFile, MAX_PATH);
			if (cch)
				::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), TRUE);
			else
				::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), FALSE);
		}
		break;
	default:
		break;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog data values.

	@param plpi Pointer to the language project information object.
	@param flidSubItems Id of the field that contains hierarchical subrecords, or 0 if none.
	@param pszProgHelpFile help file (.chm file) that this dialog box uses
	@param pszHelpTopic help topic in the help file that this dialog box uses
----------------------------------------------------------------------------------------------*/
void AfExportDlg::Initialize(AfLpInfo * plpi, IVwStylesheet * pvss, IFwCustomExport * pfcex,
	const achar * pszRegProgName, const achar * pszProgHelpFile, const achar * pszHelpTopic,
	int vwt, int flidSubItems, int crec, int * rghvoRec, int * rgclidRec)
{
	AssertPtr(plpi);
	AssertPtr(pvss);
	AssertPtrN(pfcex);

	m_plpi = plpi;
	m_qvss = pvss;
	m_qfcex = pfcex;
	m_fws.SetRoot(pszRegProgName);
	m_strHelpFilename.Assign(pszProgHelpFile);
	m_pszHelpUrl = pszHelpTopic;

	if (!pszProgHelpFile)
	{
		// Disable the help button (hide it?).
	}
	m_vwt = vwt;
	m_flidSubItems = flidSubItems;
	m_crec = crec;
	m_rghvoRec = rghvoRec;
	m_rgclidRec = rgclidRec;
}

/*----------------------------------------------------------------------------------------------
	Export the designated data in the designated format.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::ExportData(HWND hwndParent)
{
	AssertPtr(m_plpi);

	m_hwndParent = hwndParent;		// Save for use in error message dialogs.

	WaitCursor wc;
	m_qprog.Create();
	AssertPtr(m_qprog.Ptr());
	m_qprog->SetCanceledFlag(&m_fCancelled);	// Provide Cancel button on progress dialog.
	m_qprog->SetCursor(IDC_WAIT);
	m_qprog->DoModeless(hwndParent);
	StrApp strCaption(kstidExportMsgCaption);
	m_qprog->SetTitle(strCaption.Chars());

	// Write the initial XML export file.
	AfDbInfo * pdbi = m_plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetFwMetaDataCache(&m_qmdc);
	AssertPtr(m_qmdc);
	pdbi->GetDbAccess(&m_qode);
	Export();
	if (m_fCancelled)
	{
		m_qprog->DestroyHwnd();
		m_qprog.Clear();
		return;
	}

	// Give CustomExport chance to perform post-processing on XML file before doing transform
	if (m_qfcex)
	{
		BSTR bstrInputFile(NULL);
		BSTR bstrOutputFile(NULL);
		m_staTmpFile.GetBstr(&bstrInputFile);
		m_qfcex->PostProcessFile(bstrInputFile, &bstrOutputFile);
		m_staTmpFile.Assign(bstrOutputFile);
	}

	// Transform the XML export file into the desired format.
	try
	{
		StrApp strMsg(kstidExportFormattingData);
		int csec = 4;
		if (m_vess[m_iess].m_vstrChain.Size())
			csec += m_vess[m_iess].m_vstrChain.Size() * 4;
		Transform();
	}
	catch (...)
	{
		m_qprog->DestroyHwnd();
		m_qprog.Clear();
		StrApp strTitle(kstidExportErrorTitle);
		StrApp strMsg;
		if (m_sbstrError.Length())
		{
			StrApp strError(m_sbstrError.Chars());
			StrApp strFmt;
			if (m_nErrorLine)
				strFmt.Load(kstidExportErrorXmlSyntax);
			else
				strFmt.Load(kstidExportErrorXmlProcess);
			strMsg.Format(strFmt.Chars(), strError.Chars(), m_nErrorCode, m_nErrorLine);
		}
		else if (m_stuErrorProcessStep.Length())
		{
			strMsg.Assign(m_stuErrorProcessStep);
		}
		else
		{
			// No information, but report that something bad happened!
			strMsg.Load(kstidExportErrorTransforming);
		}
		::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
		return;
	}
	m_qprog->DestroyHwnd();
	m_qprog.Clear();
	if (m_fOpenWhenDone)
	{
		int ridError = AfUtil::Execute(NULL, _T("open"), m_strPathname.Chars(), NULL, NULL,
			SW_SHOW);
		if (ridError != 0)
		{
			StrApp strFmt(kstidExportLaunchErrorMsg);
			StrApp str(ridError);
			StrApp strTitle(kstidExportErrorTitle);
			StrApp strMsg;
			strMsg.Format(strFmt.Chars(), m_strPathname.Chars(), str.Chars());
			::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
		}
	}
}

#ifdef DUMP_VIEW_DEFINITION
/*----------------------------------------------------------------------------------------------
	Write the user view field specification information to the output as an XML comment.  This
	is useful only for debugging and development.

	@param pstrm Pointer to the IStream object for output.
	@param pfsp Pointer to the current user view field specification.
	@param indent Number of spaces to indent each output line.
----------------------------------------------------------------------------------------------*/
void WriteFieldSpec(IStream * pstrm, FldSpec * pfsp, int indent)
{
	AssertPtr(pstrm);
	AssertPtr(pfsp);
	Assert(indent >= 0);

	Vector<char> vchIndent;
	vchIndent.Resize(indent + 1);
	memset(vchIndent.Begin(), ' ', indent);
	vchIndent[indent] = 0;
	StrAnsi sta;
	ULONG cbOut;
	switch (pfsp->m_eVisibility)
	{
	case kFTVisNever:
		sta.Format("%n%sNever Visible:   ", vchIndent.Begin());
		break;
	case kFTVisIfData:
		sta.Format("%n%sVisible If Data: ", vchIndent.Begin());
		break;
	default:
		sta.Format("%n%sAlways Visible:  ", vchIndent.Begin());
		break;
	}
	CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
	sta.Format("flid = %d, FldName = '%S', ClsName = '%S'%n",
		pfsp->m_flid,					// Field id in the database.
		// Information not stored in the database.
		pfsp->m_stuFldName.Chars(),		// Name of field in database (computed)
		pfsp->m_stuClsName.Chars());	// Name of class the field belongs to (computed)
	CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
	if (pfsp->m_qtssLabel)
	{
		if (pfsp->m_fHideLabel) // true to hide label in Document view.
			sta.Format("%s    [hide]", vchIndent.Begin());
		else
			sta.Format("%s    [show]", vchIndent.Begin());
		CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
		pfsp->m_qtssLabel->WriteAsXml(pstrm, 0, 0, FALSE);	// Tree label.
	}
	sta.Format("%s    ft = ", vchIndent.Begin());
	switch (pfsp->m_ft)
	{
	case kftString:				// TsString string.
		sta.Append("String");
		break;
		// These types only specify that we are dealing with a MultiString or MultiText
		// field.  The actual writing system(s) can be a real writing system or a magic writing
		// system (in common.h) and is specified in another spot, such as m_ws in FldSpec.
	case kftMsa:				// Multi string alternative.
		sta.Append("Msa");
		break;
	case kftMta:				// MultiText alternative.
		sta.Append("Mta");
		break;
	case kftRefAtomic:		// Atomic reference to a PossibilityItem set via the poss.
		sta.Append("RefAtomic");
		break;					//  chooser.
	case kftRefCombo:			// Atomic reference to a PossibilityItem set via a combo
		sta.Append("RefCombo");
		break;					//  box.
	case kftRefSeq:			// Seq of refs to PossibilityItem set via the poss. chooser.
		sta.Append("RefSeq");
		break;
	case kftEnum:			// An enumeration of possible values set via a combo box.
		sta.Append("Enum");
		break;
		// The next two are not (yet?) supported for display in the dialog, nor in
		// resulting views. They are for the tertiary use of CustViewDa as a way of
		// accessing database contents.
	case kftUnicode:			// A plain Unicode string property.
		sta.Append("Unicode");
		break;
	case kftTtp:				// A binary column in the database interpreted as a
		sta.Append("Ttp");
		break;					//  TsTextProps.
	case kftStText:				// Structured text (Block only).
		sta.Append("StText");
		break;
	case kftDummy:				// Fake field, no data to load.
		sta.Append("Dummy");
		break;					// (Usually a subfield of a kftTitleGroup.)
	case kftLimEmbedLabel:		// On blocks, smaller FTs mean embed label in para.
		sta.Append("LimEmbedLabel");
		break;
	case kftGroup:				// Group (Block only: use fields in FldVec).
		sta.Append("Group");
		break;
	case kftGroupOnePerLine:	// Like group, but each field uses a new indented line
		sta.Append("GroupOnePerLine");
		break;
	case kftTitleGroup:		// Special group of a main text field with a small subfield
		sta.Append("TitleGroup");
		break;					//  right aligned.
	case kftDateRO:				// Standard read-only date/time as stored in MSDE.
		sta.Append("DateRO");
		break;
	case kftDate:				// Standard editable date/time as stored in MSDE.
		sta.Append("Date");
		break;
	case kftGenDate:			// Generic date (covers BC/AD and fuzzy dates).
		sta.Append("GenDate");
		break;
	case kftSubItems:			// Sub items (Hierarchical field type).
		sta.Append("SubItems");
		break;
	case kftObjRefAtomic:		// Atomic reference to non-PossibilityItem.
		sta.Append("ObjRefAtomic");
		break;
	case kftObjRefSeq:			// Seq of refs to non-PossibilityItems.
		sta.Append("ObjRefSeq");
		break;
	case kftInteger:			// Integer property.
		sta.Append("Integer");
		break;
	case kftBackRefAtomic:		// BackRef from atomic ref (no editor type for this yet)
		sta.Append("BackRefAtomic");
		break;
	case kftExpandable:			// example:  Participants
		sta.Append("Expandable");
		break;
	default:
		sta.FormatAppend("???? (%d)", pfsp->m_ft);
		break;
	}
	CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
	sta.Format("; req = %s, sty = '%S', ws = %d",
		pfsp->m_fRequired ? "T" : "F",	// field Required or not
		pfsp->m_stuSty.Chars(), // The default style for this field.
		pfsp->m_ws); // Writing system
	CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));

	// Information used for hierarchical fields (e.g., subrecords).
	if (pfsp->m_fExpand) // Always expand tree nodes.
		sta.Format("; Expand subrecords%n");
	else
		sta.Format("; Hide subrecords%n");
	CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));

	sta.Format("%s    %s Custom; hvoPssl = %d; pnt = %d; fHier = %s; fVert = %s",
		vchIndent.Begin(), pfsp->m_fCustFld ? "   " : "Not", pfsp->m_hvoPssl, pfsp->m_pnt,
		pfsp->m_fHier ? "T" : "F", pfsp->m_fVert ? "T" : "F");
	CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));

//	pfsp->m_fNewFld; // Flag used internally by custom field dialog.  Never saved.
//	pfsp->m_ons; // Way to show outline numbers (konsNone/konsNum/konsNumDot).
}
#endif /*DUMP_VIEW_DEFINITION*/

/*----------------------------------------------------------------------------------------------
	OnHelp shows the help page for the dialog (if there is one).
----------------------------------------------------------------------------------------------*/
bool AfExportDlg::OnHelp()
{
	AssertPsz(m_pszHelpUrl);
	if (m_strHelpFilename.Length())
	{
		StrAppBufPath strbpHelp;
		strbpHelp.Format(_T("%s::/%s"), m_strHelpFilename.Chars(), m_pszHelpUrl);
		HtmlHelp(::GetDesktopWindow(), strbpHelp.Chars(), HH_DISPLAY_TOPIC, NULL);
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Show help information for the requested control.
----------------------------------------------------------------------------------------------*/
bool AfExportDlg::OnHelpInfo(HELPINFO * phi)
{
	AssertPtr(phi);

	// Get the coordinates of the control and center the tooltip underneath the control.
	Rect rc;
	::GetWindowRect((HWND)phi->hItemHandle, &rc);
	phi->MousePos.x = rc.left + (rc.Width() / 2);
	phi->MousePos.y = rc.bottom + 1;

	AfContextHelpWndPtr qchw;
	qchw.Attach(NewObj AfContextHelpWnd);
	if (m_qwsf)
		qchw->SetLgWritingSystemFactory(m_qwsf);
	qchw->Create(m_hwnd, phi);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Export the designated data in FwExport XML (xml).
----------------------------------------------------------------------------------------------*/
void AfExportDlg::Export()
{
	AssertPtr(m_plpi);
	AssertPtr(m_puvs);
	AssertPtr(m_qwsf);

	IStreamPtr qfist;
	FileStream::Create(m_staTmpFile.Chars(), kfstgmWrite | kfstgmCreate, &qfist);
	if (!qfist)
		return;

	StrAnsi sta;
	ULONG cbOut;
	if (m_qprog)
	{
		StrApp strMsg(kstidExportingData);
		m_qprog->SetRange(0, m_crec + 2);
		m_qprog->SetPos(0);
		m_qprog->SetStep(1);
		m_qprog->SetMessage(strMsg.Chars());
	}

	try
	{
		// Write the XML header.
		sta.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?>%n"
			// Put this back in if we can figure out how to load Export.xml with it.
			// The following doesn't work:
			// CheckHr(qDOMInput->put_validateOnParse(VARIANT_FALSE));
			//"<!DOCTYPE WpDoc SYSTEM \"FwExport.dtd\">%n"
			"<WpDoc xmlVersion=\"1.0\">%n%n");
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));

		// Write the language information for each language.
		// First, get the set of language encodings, and then get the necessary information for
		// each writing system, writing it to the output file.
		Set<int> setws;
		int wsUI;
		CheckHr(m_qwsf->get_UserWs(&wsUI));
		setws.Insert(wsUI);
		Vector<int> & vwsVern = m_plpi->AllVernWss();
		int iws;
		for (iws = 0; iws < vwsVern.Size(); ++iws)
			setws.Insert(vwsVern[iws]);
		Vector<int> & vwsAnal = m_plpi->AllAnalWss();
		for (iws = 0; iws < vwsAnal.Size(); ++iws)
			setws.Insert(vwsAnal[iws]);

		FormatToStream(qfist, "<Languages defaultAnal=\"");
		SmartBstr sbstrAnalWs;
		CheckHr(m_qwsf->GetStrFromWs(vwsAnal[0], &sbstrAnalWs));
		WriteXmlUnicode(qfist, sbstrAnalWs.Chars(), sbstrAnalWs.Length());
		FormatToStream(qfist, "\" defaultVern=\"");
		SmartBstr sbstrVernWs;
		CheckHr(m_qwsf->GetStrFromWs(vwsVern[0], &sbstrVernWs));
		WriteXmlUnicode(qfist, sbstrVernWs.Chars(), sbstrVernWs.Length());
		FormatToStream(qfist, "\">%n");

		IWritingSystemPtr qws;
		ComBool fRTL;
		Set<int>::iterator it;
		for (it = setws.Begin(); it != setws.End(); ++it)
		{
			CheckHr(m_qwsf->get_EngineOrNull(it->GetValue(), &qws));
			AssertPtr(qws);
			if (qws)
				CheckHr(qws->WriteAsXml(qfist, 0));
		}
		qws.Clear();
		sta.Format("</Languages>%n%n");
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));

		// Write the style information for each style.
		// NOTE: This must be kept in sync with WpXml::WriteXmlStyles().

		sta.Format("<Styles>%n");
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		Vector<HVO> vhvoStyles;
		// Get the total number of styles in the style sheet.
		int csty;
		CheckHr(m_qvss->get_CStyles(&csty));
		for (int isty = 0; isty < csty; ++isty)
		{
			HVO hvo;
			CheckHr(m_qvss->get_NthStyle(isty, &hvo));
			vhvoStyles.Push(hvo);
		}

		Vector<SmartBstr> vsbstrName;
		vsbstrName.Resize(csty);
		Vector<int> vnStyleType;
		vnStyleType.Resize(csty);
		ISilDataAccessPtr qsda;
		m_qvss->get_DataAccess(&qsda);
		HashMap<HVO, int> hmhvoihc;
		int nTmp;
		int ihc;
		int iTmp;
		HVO hvo;
		ITsTextPropsPtr qttp;
		for (ihc = 0; ihc < csty; ++ihc)
		{
			CheckHr(qsda->get_UnicodeProp(vhvoStyles[ihc], kflidStStyle_Name,
				&vsbstrName[ihc]));
			hmhvoihc.Insert(vhvoStyles[ihc], ihc);
		}
		for (ihc = 0; ihc < csty; ++ihc)
		{
			sta.Format("<StStyle>%n"
				"<Name17><Uni>");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			WriteXmlUnicode(qfist, vsbstrName[ihc].Chars(), vsbstrName[ihc].Length());
			CheckHr(qsda->get_IntProp(vhvoStyles[ihc], kflidStStyle_Type, &nTmp));
			sta.Format("</Uni></Name17>%n"
				"<Type17><Integer val=\"%d\"/></Type17>%n"
				"<BasedOn17><Uni>", nTmp);
			vnStyleType[ihc] = nTmp;
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			CheckHr(qsda->get_IntProp(vhvoStyles[ihc], kflidStStyle_BasedOn, &nTmp));
			hvo = nTmp;
			if (hmhvoihc.Retrieve(hvo, &iTmp))
				WriteXmlUnicode(qfist, vsbstrName[iTmp].Chars(),
					vsbstrName[iTmp].Length());
			sta.Format("</Uni></BasedOn17>%n"
				"<Next17><Uni>");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			CheckHr(qsda->get_IntProp(vhvoStyles[ihc], kflidStStyle_Next, &nTmp));
			hvo = nTmp;
			if (hmhvoihc.Retrieve(hvo, &iTmp))
				WriteXmlUnicode(qfist, vsbstrName[iTmp].Chars(),
					vsbstrName[iTmp].Length());
			sta.Format("</Uni></Next17>%n"
				"<Rules17>%n");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			IUnknownPtr qunkTtp;
			CheckHr(qsda->get_UnknownProp(vhvoStyles[ihc], kflidStStyle_Rules, &qunkTtp));
			CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
			if (qttp)
				qttp->WriteAsXml(qfist, m_qwsf, 0);
			sta.Format("</Rules17>%n"
				"</StStyle>%n");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		}
		m_stuLabelFormat.Assign(L"Label");
		m_stuSubLabelFormat.Assign(L"SubLabel");
		// Determine which "Heading N" styles already exist, and whether an style named "Label"
		// exists.
		bool fHeadings[knMaxLevels];
		int i;
		for (i = 0; i < knMaxLevels; ++i)
		{
			m_rgstuHeadings[i].Format(L"Heading %d", i + 1);
			fHeadings[i] = false;
		}
		for (ihc = 0; ihc < vsbstrName.Size(); ++ihc)
		{
			if (m_stuLabelFormat == vsbstrName[ihc].Chars())
			{
				m_stuLabelFormat.Append("0");
				ihc = -1;
				continue;
			}
			if (m_stuSubLabelFormat == vsbstrName[ihc].Chars())
			{
				m_stuSubLabelFormat.Append("0");
				ihc = -1;
				continue;
			}
			for (i = 0; i < knMaxLevels; ++i)
			{
				if (fHeadings[i])
					continue;
				if (m_rgstuHeadings[i] == vsbstrName[ihc].Chars())
				{
					if (vnStyleType[ihc] == kstParagraph)
					{
						fHeadings[i] = true;
						break;
					}
					else
					{
						// We need a Paragraph Style!
						m_rgstuHeadings[i].Append(L"A");
						ihc = -1;
						break;
					}
				}
			}
		}
		if (m_qfcex)
		{
			CheckHr(m_qfcex->SetLabelStyles(m_stuLabelFormat.Bstr(),
				m_stuSubLabelFormat.Bstr()));
		}
		Set<StrUni, HashStrUni, EqlStrUni> setstuNewStyles;
		// Add a "Label" character style.
		sta.Format("<StStyle>%n"
			"<Name17><Uni>");
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		WriteXmlUnicode(qfist, m_stuLabelFormat.Chars(), m_stuLabelFormat.Length());
		sta.Format("</Uni></Name17>%n"
			"<Type17><Integer val=\"%d\"/></Type17>%n"
			"<BasedOn17><Uni>Default Paragraph Characters</Uni></BasedOn17>%n"
			"<Next17><Uni></Uni></Next17>%n"
			"<Rules17>%n"
			"<Prop bold=\"on\"/>%n"
			"</Rules17>%n"
			"</StStyle>%n", kstCharacter);
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		setstuNewStyles.Insert(m_stuLabelFormat);
		// Add a "SubLabel" character style.
		sta.Format("<StStyle>%n"
			"<Name17><Uni>");
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		WriteXmlUnicode(qfist, m_stuSubLabelFormat.Chars(), m_stuSubLabelFormat.Length());
		sta.Format("</Uni></Name17>%n"
			"<Type17><Integer val=\"%d\"/></Type17>%n"
			"<BasedOn17><Uni>Default Paragraph Characters</Uni></BasedOn17>%n"
			"<Next17><Uni></Uni></Next17>%n"
			"<Rules17>%n"
			"<Prop bold=\"on\" italic=\"on\"/>%n"
			"</Rules17>%n"
			"</StStyle>%n", kstCharacter);
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		setstuNewStyles.Insert(m_stuSubLabelFormat);
		// Add any needed "Heading N" paragraph styles.
		for (i = 0; i < knMaxLevels; ++i)
		{
			if (fHeadings[i])
				continue;
			sta.Format("<StStyle>%n"
				"<Name17><Uni>");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			WriteXmlUnicode(qfist, m_rgstuHeadings[i].Chars(),
				m_rgstuHeadings[i].Length());
			sta.Format("</Uni></Name17>%n"
				"<Type17><Integer val=\"%d\"/></Type17>%n"
				"<BasedOn17><Uni>Normal</Uni></BasedOn17>%n"
				"<Next17><Uni></Uni></Next17>%n"
				"<Rules17>%n"
				"<Prop italic=\"on\"/>%n"	// dummy value
				"</Rules17>%n"
				"</StStyle>%n", kstParagraph);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			setstuNewStyles.Insert(m_rgstuHeadings[i]);
		}
		// Add character styles as needed for the various group entities.
		ClevRspMap::iterator itcrm;
		for (itcrm = m_puvs->m_hmclevrsp.Begin(); itcrm != m_puvs->m_hmclevrsp.End(); ++itcrm)
		{
			RecordSpecPtr qrsp = itcrm->GetValue();
			int cbsp = qrsp->m_vqbsp.Size();
			for (int ibsp = 0; ibsp < cbsp; ++ibsp)
			{
				BlockSpec * pbsp = qrsp->m_vqbsp[ibsp];
				if (pbsp->m_ft == kftGroup)
				{
					for (int ifsp = 0; ifsp < pbsp->m_vqfsp.Size(); ++ifsp)
					{
						FldSpec * pfsp = pbsp->m_vqfsp[ifsp];
						if (pfsp->m_eVisibility == kFTVisNever)
							continue;
						if (pfsp->m_stuSty.Length() == 0)
						{
							// Need a character style for use within a paragraph.
							if (pfsp->m_qtssLabel)
							{
								SmartBstr sbstr;
								CheckHr(pfsp->m_qtssLabel->get_Text(&sbstr));
								StrUni stu(sbstr.Chars());
								m_hmflidstuCharStyle.Insert(pfsp->m_flid, stu, true);
								if (m_qfcex)
								{
									CheckHr(m_qfcex->AddFlidCharStyleMapping(pfsp->m_flid,
										sbstr));
								}
							}
							else if (pfsp->m_stuFldName.Length())
							{
								m_hmflidstuCharStyle.Insert(pfsp->m_flid, pfsp->m_stuFldName,
									true);
								if (m_qfcex)
								{
									CheckHr(m_qfcex->AddFlidCharStyleMapping(pfsp->m_flid,
										pfsp->m_stuFldName.Bstr()));
								}
							}
						}
					}
				}
				else if (pbsp->m_eVisibility != kFTVisNever && pbsp->m_flid)
				{
					// Add paragraph style.
					if (pbsp->m_qtssLabel)
					{
						SmartBstr sbstr;
						CheckHr(pbsp->m_qtssLabel->get_Text(&sbstr));
						StrUni stu(sbstr.Chars());
						m_hmflidstuParaStyle.Insert(pbsp->m_flid, stu, true);
					}
					else if (pbsp->m_stuFldName.Length())
					{
						m_hmflidstuParaStyle.Insert(pbsp->m_flid, pbsp->m_stuFldName, true);
					}
				}
			}
		}
		HashMap<int, StrUni>::iterator ithm;
		Set<StrUni, HashStrUni, EqlStrUni> setstuNewCharStyles;
		for (ithm = m_hmflidstuCharStyle.Begin(); ithm != m_hmflidstuCharStyle.End(); ++ithm)
		{
			StrUni & stu = ithm->GetValue();
			while (setstuNewStyles.IsMember(stu))
				stu.Append(L"C");		// Style exists -- adjust name and try again.
			bool fFound = false;
			for (ihc = 0; ihc < vsbstrName.Size(); ++ihc)
			{
				if (stu == vsbstrName[ihc].Chars())
				{
					if (vnStyleType[ihc] == kstCharacter)
					{
						// Character Style already exists.
						fFound = true;
						break;
					}
					else
					{
						// Paragraph Style exists -- adjust name and try again.
						stu.Append(L"C");
						ihc = -1;
						continue;
					}
				}
			}
			if (!fFound)
				setstuNewCharStyles.Insert(stu);		// Using a set eliminates duplicates.
		}
		Set<StrUni, HashStrUni, EqlStrUni>::iterator itset;
		for (itset = setstuNewCharStyles.Begin(); itset != setstuNewCharStyles.End(); ++itset)
		{
			sta.Format("<StStyle>%n"
				"<Name17><Uni>");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			StrUni & stu = itset->GetValue();
			WriteXmlUnicode(qfist, stu.Chars(), stu.Length());
			sta.Format("</Uni></Name17>%n"
				"<Type17><Integer val=\"%d\"/></Type17>%n"
				"<BasedOn17><Uni>Default Paragraph Characters</Uni></BasedOn17>%n"
				"<Next17><Uni></Uni></Next17>%n"
				"<Rules17>%n"
				"<Prop bold=\"off\" italic=\"off\"/>%n"		// Dummy value.
				"</Rules17>%n"
				"</StStyle>%n", kstCharacter);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		}

		Set<StrUni, HashStrUni, EqlStrUni> setstuNewParaStyles;
		for (ithm = m_hmflidstuParaStyle.Begin(); ithm != m_hmflidstuParaStyle.End(); ++ithm)
		{
			StrUni & stu = ithm->GetValue();
			while (setstuNewStyles.IsMember(stu))
				stu.Append(L"P");		// Style exists -- adjust name and try again.
			while (setstuNewCharStyles.IsMember(stu))
				stu.Append(L"P");		// Style exists -- adjust name and try again.
			bool fFound = false;
			for (ihc = 0; ihc < vsbstrName.Size(); ++ihc)
			{
				if (stu == vsbstrName[ihc].Chars())
				{
					if (vnStyleType[ihc] == kstParagraph)
					{
						// Paragraph Style already exists.
						fFound = true;
						break;
					}
					else
					{
						// Character Style exists -- adjust name and try again.
						stu.Append(L"P");
						ihc = -1;
						continue;
					}
				}
			}
			if (!fFound)
				setstuNewParaStyles.Insert(stu);		// Using a set eliminates duplicates.
		}
		for (itset = setstuNewParaStyles.Begin(); itset != setstuNewParaStyles.End(); ++itset)
		{
			sta.Format("<StStyle>%n"
				"<Name17><Uni>");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			StrUni & stu = itset->GetValue();
			WriteXmlUnicode(qfist, stu.Chars(), stu.Length());
			sta.Format("</Uni></Name17>%n"
				"<Type17><Integer val=\"%d\"/></Type17>%n"
				"<BasedOn17><Uni>Normal</Uni></BasedOn17>%n"
				"<Next17><Uni></Uni></Next17>%n"
				"<Rules17>%n"
				"<Prop bold=\"off\" italic=\"off\"/>%n"		// Dummy value.
				"</Rules17>%n"
				"</StStyle>%n", kstParagraph);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		}

		sta.Format("</Styles>%n%n");
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		if (m_qprog)
			m_qprog->StepIt();
		if (m_fCancelled)
			return;
#ifdef DUMP_VIEW_DEFINITION
		// Dump out the view definition for development/debugging.
		for (itcrm = m_puvs->m_hmclevrsp.Begin(); itcrm != m_puvs->m_hmclevrsp.End(); ++itcrm)
		{
			RecordSpecPtr qrsp = itcrm->GetValue();
			ClsLevel clev = itcrm->GetKey();
			if (qrsp)
			{
				SmartBstr sbstrClass;
				m_qmdc->GetClassName(clev.m_clsid, &sbstrClass);
				sta.Format("<!-- [%S (%d), %d]",
					sbstrClass.Chars(), clev.m_clsid, clev.m_nLevel);
				CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
				int cbsp = qrsp->m_vqbsp.Size();
				for (int ibsp = 0; ibsp < cbsp; ++ibsp)
				{
					BlockSpec * pbsp = qrsp->m_vqbsp[ibsp];
					WriteFieldSpec(qfist, dynamic_cast<FldSpec *>(pbsp), 0);
					for (int ifsp = 0; ifsp < pbsp->m_vqfsp.Size(); ++ifsp)
						WriteFieldSpec(qfist, pbsp->m_vqfsp[ifsp], 8);
				}
				sta.Format("%n-->%n");
				CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			}
		}
#endif /*DUMP_VIEW_DEFINITION*/

		// Write the text for each paragraph.
		fRTL = false;			// TODO: Get real value when we fully support Graphite and RTL.
		sta.Format("<Body docRightToLeft=\"%s\">%n", fRTL ? "true" : "false");
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		for (ihc = 0; ihc < m_crec; ++ihc)
		{
			HvoClsid hc = { m_rghvoRec[ihc], m_rgclidRec[ihc] };
			WriteRecord(qfist, hc, 0);
			if (m_qprog)
				m_qprog->StepIt();
			if (m_fCancelled)
				return;
		}
		sta.Format("</Body>%n%n");
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));

		// Write the page layout information.
		if (m_qfcex)
		{
			int nPot;
			int nPst;
			int dxmpLeftMargin;
			int dxmpRightMargin;
			int dympTopMargin;
			int dympBottomMargin;
			int dympHeaderMargin;
			int dympFooterMargin;
			int dxmpPageWidth;
			int dympPageHeight;
			ITsStringPtr qtssHeader;
			ITsStringPtr qtssFooter;
			const char * pszT;
			CheckHr(m_qfcex->GetPageSetupInfo(&nPot, &nPst, &dxmpLeftMargin, &dxmpRightMargin,
				&dympTopMargin, &dympBottomMargin, &dympHeaderMargin, &dympFooterMargin,
				&dxmpPageWidth, &dympPageHeight, &qtssHeader, &qtssFooter));
			sta.Format("<PageSetup>%n");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<PageInfo>%n");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<TopMargin9999><Integer val=\"%d\"/></TopMargin9999>%n", dympTopMargin);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<BottomMargin9999><Integer val=\"%d\"/></BottomMargin9999>%n",
				dympBottomMargin);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<LeftMargin9999><Integer val=\"%d\"/></LeftMargin9999>%n",
				dxmpLeftMargin);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<RightMargin9999><Integer val=\"%d\"/></RightMargin9999>%n",
				dxmpRightMargin);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<HeaderMargin9999><Integer val=\"%d\"/></HeaderMargin9999>%n",
				dympHeaderMargin);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<FooterMargin9999><Integer val=\"36000\"/></FooterMargin9999>%n",
				dympFooterMargin);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			switch (nPst)
			{
			case kSzLtr:	pszT = "Letter";	break;
			case kSzLgl:	pszT = "Legal";		break;
			case kSzA4:		pszT = "A4";		break;
			case kSzCust:	pszT = "Custom";	break;
			default:		pszT = "???";		break;
			}
			sta.Format("<PageSize9999><Integer val=\"%d\"/><!-- %s --></PageSize9999>%n",
				nPst, pszT);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<PageHeight9999><Integer val=\"%d\"/></PageHeight9999>%n",
				dympPageHeight);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<PageWidth9999><Integer val=\"%d\"/></PageWidth9999>%n",
				dxmpPageWidth);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			switch (nPot)
			{
			case kPort:		pszT = "Portrait";		break;
			case kLands:	pszT = "Landscape";		break;
			default:		pszT = "???";			break;
			}
			sta.Format(
				"<PageOrientation9999><Integer val=\"%d\"/><!-- %s --></PageOrientation9999>%n",
				nPot, pszT);
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<Header9999>%n");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			if (qtssHeader)
				qtssHeader->WriteAsXml(qfist, m_qwsf, 0, 0, FALSE);
			sta.Format("</Header9999>%n");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("<Footer9999>%n");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			if (qtssFooter)
				qtssFooter->WriteAsXml(qfist, m_qwsf, 0, 0, FALSE);
			sta.Format("</Footer9999>%n");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("</PageInfo>%n");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
			sta.Format("</PageSetup>%n%n");
			CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		}

		// Write the XML footer.
		sta.Format("</WpDoc>%n");
		CheckHr(qfist->Write(sta.Chars(), sta.Length(), &cbOut));
		if (m_qprog)
			m_qprog->StepIt();
		if (m_fCancelled)
			return;
	}
	catch (...)
	{
	}
}

/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the formatted possibility name
	for the given field.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildRefAtomicString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	AssertPtr(pptss);
	Assert(hvoRec);

	*pptss = NULL;
	HVO hvo = 0;
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		StrUni stuQuery;
		ComBool fMoreRows;
		ComBool fIsNull;
		ULONG cbSpaceTaken;
		stuQuery.Format(L"SELECT [%s] FROM [%s] WHERE [Id]=%d",
			pfsp->m_stuFldName.Chars(), pfsp->m_stuClsName.Chars(), hvoRec);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvo), sizeof(hvo),
				&cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvo = 0;
		}
	}
	catch (...)
	{
		hvo = 0;
	}
	if (hvo || pfsp->m_eVisibility == kFTVisAlways)
	{
		ITsIncStrBldrPtr qtisb;
		ITsStringPtr qtss;
		if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
		{
			ITsStrBldrPtr qtsb;
			CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
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
		PossListInfoPtr qpli;
		StrUni stu;
		if (hvo && m_plpi->LoadPossList(pfsp->m_hvoPssl, pfsp->m_ws, &qpli))
		{
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, pfsp->m_stuFldName.Bstr()));
			int ipii = qpli->GetIndexFromId(hvo);
			if (ipii >= 0)
			{
				PossItemInfo * ppii = qpli->GetPssFromIndex(ipii);
				AssertPtr(ppii);
				CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ppii->GetWs()));
				if (pfsp->m_fHier)
					ppii->GetHierName(qpli, stu, pfsp->m_pnt);
				else
					ppii->GetName(stu, pfsp->m_pnt);
				bool fHaveStyle = false;
				StrUni stuSty;
				if (pfsp->m_stuSty.Length())
				{
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, pfsp->m_stuSty.Bstr()));
					fHaveStyle = true;
				}
				else if (m_hmflidstuCharStyle.Retrieve(pfsp->m_flid, &stuSty))
				{
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
					fHaveStyle = true;
				}
				CheckHr(qtisb->Append(stu.Bstr()));
				if (fHaveStyle)
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
			}
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
		}
		CheckHr(qtisb->GetString(pptss));
	}
}

/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the sequence of formatted
	possibility names for the given field.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildRefSeqString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	AssertPtr(pptss);
	Assert(hvoRec);

	*pptss = NULL;
	Vector<HVO> vhvoPss;
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		StrUni stuQuery;
		ComBool fMoreRows;
		ComBool fIsNull;
		ULONG cbSpaceTaken;
		stuQuery.Format(L"SELECT [Dst] FROM [%s_%s] WHERE [Src]=%d",
			pfsp->m_stuClsName.Chars(), pfsp->m_stuFldName.Chars(), hvoRec);
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
	}
	catch (...)
	{
		vhvoPss.Clear();
	}
	if (vhvoPss.Size() || pfsp->m_eVisibility == kFTVisAlways)
	{
		ITsIncStrBldrPtr qtisb;
		ITsStringPtr qtss;
		if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
		{
			ITsStrBldrPtr qtsb;
			CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
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
		PossListInfoPtr qpli;
		if (m_plpi->LoadPossList(pfsp->m_hvoPssl, pfsp->m_ws, &qpli))
		{
			StrUni stu;
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, pfsp->m_stuFldName.Bstr()));
			int cItemsOut = 0;
			int ihvo;
			for (ihvo = 0; ihvo < vhvoPss.Size(); ++ihvo)
			{
				if (cItemsOut)
					CheckHr(qtisb->AppendRgch(L", ", 2));
				int ipii = qpli->GetIndexFromId(vhvoPss[ihvo]);
				if (ipii >= 0)
				{
					PossItemInfo * ppii = qpli->GetPssFromIndex(ipii);
					AssertPtr(ppii);
					CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ppii->GetWs()));
					if (pfsp->m_fHier)
						ppii->GetHierName(qpli, stu, pfsp->m_pnt);
					else
						ppii->GetName(stu, pfsp->m_pnt);
					bool fHaveStyle = false;
					StrUni stuSty;
					if (pfsp->m_stuSty.Length())
					{
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, pfsp->m_stuSty.Bstr()));
						fHaveStyle = true;
					}
					else if (m_hmflidstuCharStyle.Retrieve(pfsp->m_flid, &stuSty))
					{
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
						fHaveStyle = true;
					}
					CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvForceOn));
					CheckHr(qtisb->Append(stu.Bstr()));
					if (fHaveStyle)
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
					CheckHr(qtisb->SetIntPropValues(ktptMarkItem, ktpvEnum, kttvOff));
					++cItemsOut;
				}
			}
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
		}
		CheckHr(qtisb->GetString(pptss));
	}
}

/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the value of a 'Generic Date'
	for the given field.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildGenDateString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	AssertPtr(pptss);
	Assert(hvoRec);

	*pptss = NULL;
	int gdat = 0;
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		StrUni stuQuery;
		ComBool fMoreRows;
		ComBool fIsNull;
		ULONG cbSpaceTaken;
		stuQuery.Format(L"SELECT [%s] FROM [%s] WHERE [Id]=%d",
			pfsp->m_stuFldName.Chars(), pfsp->m_stuClsName.Chars(), hvoRec);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&gdat),
				sizeof(gdat), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				gdat = 0;
		}
	}
	catch (...)
	{
		gdat = 0;
	}
	if (gdat || pfsp->m_eVisibility == kFTVisAlways)
	{
		ITsIncStrBldrPtr qtisb;
		ITsStringPtr qtss;
		if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
		{
			ITsStrBldrPtr qtsb;
			CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
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
		if (gdat)
		{
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, pfsp->m_stuFldName.Bstr()));
			StrUni stu;
			stu.Format(L"%D", gdat);
			bool fHaveStyle = false;
			StrUni stuSty;
			if (pfsp->m_stuSty.Length())
			{
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, pfsp->m_stuSty.Bstr()));
				fHaveStyle = true;
			}
			else if (m_hmflidstuCharStyle.Retrieve(pfsp->m_flid, &stuSty))
			{
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
				fHaveStyle = true;
			}
			CheckHr(qtisb->Append(stu.Bstr()));
			if (fHaveStyle)
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
		}
		CheckHr(qtisb->GetString(pptss));
	}
}

/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the value of a 'Read-Only Date'
	for the given field.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildDateROString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	Assert(hvoRec);
	AssertPtr(pptss);

	*pptss = NULL;

	// Get the data from the database.
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		StrUni stuQuery;
		ComBool fMoreRows;
		ComBool fIsNull;
		ULONG cbSpaceTaken;
		stuQuery.Format(L"SELECT [%s] FROM [%s] WHERE [Id]=%d",
			pfsp->m_stuFldName.Chars(), pfsp->m_stuClsName.Chars(), hvoRec);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			DBTIMESTAMP tim;
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&tim),
						sizeof(DBTIMESTAMP), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull || pfsp->m_eVisibility == kFTVisAlways)
			{
				ITsIncStrBldrPtr qtisb;
				if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
				{
					ITsStrBldrPtr qtsb;
					CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
					int cch;
					CheckHr(qtsb->get_Length(&cch));
					CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle,
						m_stuLabelFormat.Bstr()));
					CheckHr(qtsb->ReplaceRgch(cch, cch, L":", 1, NULL));
					ITsStringPtr qtss;
					CheckHr(qtsb->GetString(&qtss));
					CheckHr(qtss->GetIncBldr(&qtisb));
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
					CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
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
				if (!fIsNull)
				{
					CheckHr(qtisb->SetStrPropValue(ktptFieldName, pfsp->m_stuFldName.Bstr()));
					SYSTEMTIME systim;
					memset(&systim, 0, sizeof(systim));
					systim.wYear = tim.year;
					systim.wMonth = tim.month;
					systim.wDay = tim.day;
					systim.wHour = tim.hour;
					systim.wMinute = tim.minute;
					systim.wSecond = tim.second;
					wchar rgchDate[81];
					wchar rgchTime[81];
					int cch = ::GetDateFormatW(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &systim,
						NULL, rgchDate, 80);
					if (!cch)
						ThrowHr(E_FAIL);
					else if (cch <= 80)
						rgchDate[cch] = 0;
					cch = ::GetTimeFormatW(LOCALE_USER_DEFAULT, LOCALE_NOUSEROVERRIDE, &systim,
						NULL, rgchTime, 80);
					if (!cch)
						ThrowHr(E_FAIL);
					else if (cch <= 80)
						rgchTime[cch] = 0;
					StrUni stu;
					stu.Format(L"%s %s", rgchDate, rgchTime);
					bool fHaveStyle = false;
					StrUni stuSty;
					if (pfsp->m_stuSty.Length())
					{
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, pfsp->m_stuSty.Bstr()));
						fHaveStyle = true;
					}
					else if (m_hmflidstuCharStyle.Retrieve(pfsp->m_flid, &stuSty))
					{
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
						fHaveStyle = true;
					}
					CheckHr(qtisb->AppendRgch(stu.Chars(), stu.Length()));
					if (fHaveStyle)
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
					CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
				}
				CheckHr(qtisb->GetString(pptss));
			}
		}
	}
	catch (...)
	{
	}
}

/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the enumeration value for the
	current record.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildEnumString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	AssertPtr(pptss);
	Assert(hvoRec);

	*pptss = NULL;

	// Get the data from the database.
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		StrUni stuQuery;
		ComBool fMoreRows;
		ComBool fIsNull;
		ULONG cbSpaceTaken;
		stuQuery.Format(L"SELECT [%s] FROM [%s] WHERE [Id]=%d",
			pfsp->m_stuFldName.Chars(), pfsp->m_stuClsName.Chars(), hvoRec);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			int itss = 0;
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&itss), sizeof(itss),
				&cbSpaceTaken, &fIsNull, 0));
			// Collapse boolean values to 0 (false) and 1 (true).  ComBool defines "True"
			// as 65535 (16-bit value of -1).
			int eType;
			CheckHr(m_qmdc->GetFieldType(pfsp->m_flid, &eType));
			if (eType == kcptBoolean)
			{
				if (itss)
					itss = 1;
			}
			if (!fIsNull || pfsp->m_eVisibility == kFTVisAlways)
			{
				ITsIncStrBldrPtr qtisb;
				if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
				{
					ITsStrBldrPtr qtsb;
					CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
					int cch;
					CheckHr(qtsb->get_Length(&cch));
					CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle,
						m_stuLabelFormat.Bstr()));
					CheckHr(qtsb->ReplaceRgch(cch, cch, L":", 1, NULL));
					ITsStringPtr qtss;
					CheckHr(qtsb->GetString(&qtss));
					CheckHr(qtss->GetIncBldr(&qtisb));
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
					CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
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
				if (!fIsNull)
				{
					CheckHr(qtisb->SetStrPropValue(ktptFieldName, pfsp->m_stuFldName.Bstr()));
					StrUni stu;
					if (m_qfcex)
					{
						SmartBstr sbstr;
						CheckHr(m_qfcex->GetEnumString(pfsp->m_flid, itss, &sbstr));
						stu = sbstr.Chars();
					}
					if (!stu.Length())
					{
						// Fall back behavior if we can't find a string: use the integer value.
						stu.Format(L"%d", itss);
					}
					bool fHaveStyle = false;
					StrUni stuSty;
					if (pfsp->m_stuSty.Length())
					{
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, pfsp->m_stuSty.Bstr()));
						fHaveStyle = true;
					}
					else if (m_hmflidstuCharStyle.Retrieve(pfsp->m_flid, &stuSty))
					{
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
						fHaveStyle = true;
					}
					CheckHr(qtisb->AppendRgch(stu.Chars(), stu.Length()));
					if (fHaveStyle)
						CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
					CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
				}
				CheckHr(qtisb->GetString(pptss));
			}
		}
	}
	catch (...)
	{
	}
}


/*----------------------------------------------------------------------------------------------
	Create an IFwFldSpec object based on the FldSpec object.

	@param pfsp Pointer to the C++ user view field specification object.
	@param ppffsp Address of a pointer to a COM user view field specification object.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::GetFwFldSpec(FldSpec * pfsp, IFwFldSpec ** ppffsp)
{
	AssertPtr(pfsp);
	AssertPtr(ppffsp);

	IFwFldSpecPtr qffsp;
	qffsp.CreateInstance(CLSID_FwFldSpec);
	CheckHr(qffsp->put_Visibility(pfsp->m_eVisibility));
	CheckHr(qffsp->put_HideLabel(pfsp->m_fHideLabel));
	CheckHr(qffsp->put_Label(pfsp->m_qtssLabel));
	CheckHr(qffsp->put_FieldId(pfsp->m_flid));
	CheckHr(qffsp->put_ClassName(pfsp->m_stuClsName.Bstr()));
	CheckHr(qffsp->put_FieldName(pfsp->m_stuFldName.Bstr()));
	CheckHr(qffsp->put_Style(pfsp->m_stuSty.Bstr()));
	*ppffsp = qffsp.Detach();
}


/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the labels (if desired) and the values for group of fields in
	the current record, formatted together as a single paragraph.

	@param vqfsp Vector of specifications for the fields that are grouped together.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildGroupString(FldVec & vqfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pptss);
	Assert(hvoRec);

	*pptss = NULL;

	ITsIncStrBldrPtr qtisb;
	StrUni stuSpace(L"  ");
	int ifsp;
	for (ifsp = 0; ifsp < vqfsp.Size(); ++ifsp)
	{
		FldSpec * pfsp = vqfsp[ifsp];
		switch (pfsp->m_ft)
		{
		case kftString:
		case kftMsa:
		case kftUnicode:
		case kftMta:
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsIncStrBldrPtr qtisbLabel;
				ITsStringPtr qtssLabel;
				ITsStringPtr qtss;
				LoadTsString(pfsp, hvoRec, ws, &qtss);
				if (qtss || pfsp->m_eVisibility == kFTVisAlways)
				{
					if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
					{
						ITsStrBldrPtr qtsb;
						CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
						int cch;
						CheckHr(qtsb->get_Length(&cch));
						CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle,
							m_stuLabelFormat.Bstr()));
						CheckHr(qtsb->ReplaceRgch(cch, cch, L":", 1, NULL));
						CheckHr(qtsb->GetString(&qtssLabel));
						CheckHr(qtssLabel->GetIncBldr(&qtisbLabel));
						CheckHr(qtisbLabel->SetStrPropValue(ktptNamedStyle, NULL));
						CheckHr(qtisbLabel->SetIntPropValues(ktptWs, ktpvDefault, ws));
						CheckHr(qtisbLabel->AppendRgch(L" ", 1));
					}
					else
					{
						ITsStrFactoryPtr qtsf;
						qtsf.CreateInstance(CLSID_TsStrFactory);
						AssertPtr(qtsf);
						CheckHr(qtsf->GetIncBldr(&qtisbLabel));
						CheckHr(qtisbLabel->SetIntPropValues(ktptWs, ktpvDefault, ws));
					}
					if (!qtisb)
					{
						qtisb = qtisbLabel;
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisbLabel->GetString(&qtssLabel));
						if (qtssLabel)
							CheckHr(qtisb->AppendTsString(qtssLabel));
					}
					if (qtss)
					{
						ITsStrBldrPtr qtsb;
						CheckHr(qtss->GetBldr(&qtsb));
						int cch;
						CheckHr(qtsb->get_Length(&cch));
						CheckHr(qtsb->SetStrPropValue(0, cch, ktptFieldName,
							pfsp->m_stuFldName.Bstr()));
						CheckHr(qtsb->GetString(&qtss));
						CheckHr(qtisb->AppendTsString(qtss));
						CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
					}
				}
			}
			break;
		case kftRefAtomic:	// Atomic reference to a PossibilityItem set via the poss. chooser.
		case kftRefCombo:	// Atomic reference to a PossibilityItem set via a combo
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsStringPtr qtss;
				BuildRefAtomicString(pfsp, hvoRec, ws, &qtss);
				if (qtss)
				{
					if (!qtisb)
					{
						CheckHr(qtss->GetIncBldr(&qtisb));
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisb->AppendTsString(qtss));
					}
				}
			}
			break;
		case kftRefSeq:		// Seq of refs to PossibilityItem set via the poss. chooser.
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsStringPtr qtss;
				BuildRefSeqString(pfsp, hvoRec, ws, &qtss);
				if (qtss)
				{
					if (!qtisb)
					{
						CheckHr(qtss->GetIncBldr(&qtisb));
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisb->AppendTsString(qtss));
					}
				}
			}
			break;
		case kftEnum:
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsStringPtr qtss;
				BuildEnumString(pfsp, hvoRec, ws, &qtss);
				if (qtss)
				{
					if (!qtisb)
					{
						CheckHr(qtss->GetIncBldr(&qtisb));
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisb->AppendTsString(qtss));
					}
				}
			}
			break;
		case kftTtp:
			Assert(pfsp->m_ft != kftTtp);
			break;
		case kftStText:
			Assert(pfsp->m_ft != kftStText);
			break;
		case kftDummy:
			Assert(pfsp->m_ft != kftDummy);
			break;
		case kftLimEmbedLabel:
			Assert(pfsp->m_ft != kftLimEmbedLabel);
			break;
		case kftGroup:
			Assert(pfsp->m_ft != kftGroup);
			break;
		case kftGroupOnePerLine:
			Assert(pfsp->m_ft != kftGroupOnePerLine);
			break;
		case kftTitleGroup:
			Assert(pfsp->m_ft != kftTitleGroup);
			break;
		case kftDateRO:		// Standard read-only date/time as stored in MSDE.
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsStringPtr qtss;
				BuildDateROString(pfsp, hvoRec, ws, &qtss);
				if (qtss)
				{
					if (!qtisb)
					{
						CheckHr(qtss->GetIncBldr(&qtisb));
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisb->AppendTsString(qtss));
					}
				}
			}
			break;
		case kftDate:
			Assert(pfsp->m_ft != kftDate);
			break;
		case kftGenDate:	// Generic date (covers BC/AD and fuzzy dates).
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsStringPtr qtss;
				BuildGenDateString(pfsp, hvoRec, ws, &qtss);
				if (qtss)
				{
					if (!qtisb)
					{
						CheckHr(qtss->GetIncBldr(&qtisb));
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisb->AppendTsString(qtss));
					}
				}
			}
			break;
		case kftSubItems:	// Sub items (Hierarchical field type).
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsStringPtr qtss;
				if (m_qfcex)
				{
					IFwFldSpecPtr qffsp;
					GetFwFldSpec(pfsp, &qffsp);
					CheckHr(m_qfcex->BuildSubItemsString(qffsp, hvoRec, ws, &qtss));
				}
				if (qtss)
				{
					if (!qtisb)
					{
						CheckHr(qtss->GetIncBldr(&qtisb));
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisb->AppendTsString(qtss));
					}
				}
			}
			break;
		case kftObjRefAtomic:
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsStringPtr qtss;
				// BuildObjRefAtomicString(pfsp, hvoRec, ws, &qtss);
				if (m_qfcex)
				{
					IFwFldSpecPtr qffsp;
					GetFwFldSpec(pfsp, &qffsp);
					CheckHr(m_qfcex->BuildObjRefAtomicString(qffsp, hvoRec, ws, &qtss));
				}
				if (qtss)
				{
					if (!qtisb)
					{
						CheckHr(qtss->GetIncBldr(&qtisb));
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisb->AppendTsString(qtss));
					}
				}
			}
			break;
		case kftObjRefSeq:
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsStringPtr qtss;
				if (m_qfcex)
				{
					IFwFldSpecPtr qffsp;
					GetFwFldSpec(pfsp, &qffsp);
					CheckHr(m_qfcex->BuildObjRefSeqString(qffsp, hvoRec, ws, &qtss));
				}
				if (qtss)
				{
					if (!qtisb)
					{
						CheckHr(qtss->GetIncBldr(&qtisb));
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisb->AppendTsString(qtss));
					}
				}
			}
			break;
		case kftInteger:
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsStringPtr qtss;
				BuildIntegerString(pfsp, hvoRec, ws, &qtss);
				if (qtss)
				{
					if (!qtisb)
					{
						CheckHr(qtss->GetIncBldr(&qtisb));
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisb->AppendTsString(qtss));
					}
				}
			}
			break;
		case kftBackRefAtomic:
			Assert(pfsp->m_ft != kftBackRefAtomic);
			break;
		case kftExpandable:
			if (pfsp->m_eVisibility != kFTVisNever)
			{
				ITsStringPtr qtss;
				if (m_qfcex)
				{
					IFwFldSpecPtr qffsp;
					GetFwFldSpec(pfsp, &qffsp);
					CheckHr(m_qfcex->BuildExpandableString(qffsp, hvoRec, ws, &qtss));
				}
				if (qtss)
				{
					if (!qtisb)
					{
						CheckHr(qtss->GetIncBldr(&qtisb));
					}
					else
					{
						CheckHr(qtisb->Append(stuSpace.Bstr()));
						CheckHr(qtisb->AppendTsString(qtss));
					}
				}
			}
			break;
		}
	}
	if (qtisb)
	{
		CheckHr(qtisb->GetString(pptss));
	}
}

/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the value for a field in the
	current record that is grouped with other fields, one per paragraph.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildGroupLine(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	switch (pfsp->m_ft)
	{
	case kftString:
	case kftMsa:
	case kftUnicode:
	case kftMta:
		BuildTsString(pfsp, hvoRec, ws, pptss);
		return;

	case kftRefAtomic:
	case kftRefCombo:
		BuildRefAtomicString(pfsp, hvoRec, ws, pptss);
		return;

	case kftRefSeq:
		BuildRefSeqString(pfsp, hvoRec, ws, pptss);
		return;

	case kftEnum:
		BuildEnumString(pfsp, hvoRec, ws, pptss);
		return;

	case kftTtp:
		Assert(pfsp->m_ft != kftTtp);
		break;
	case kftStText:
		Assert(pfsp->m_ft != kftStText);
		break;
	case kftDummy:
		Assert(pfsp->m_ft != kftDummy);
		break;
	case kftLimEmbedLabel:
		Assert(pfsp->m_ft != kftLimEmbedLabel);
		break;
	case kftGroup:
		Assert(pfsp->m_ft != kftGroup);
		break;
	case kftGroupOnePerLine:
		Assert(pfsp->m_ft != kftGroupOnePerLine);
		break;
	case kftTitleGroup:
		Assert(pfsp->m_ft != kftTitleGroup);
		break;
	case kftDateRO:		// Standard read-only date/time as stored in MSDE.
		BuildDateROString(pfsp, hvoRec, ws, pptss);
		return;

	case kftDate:
		Assert(pfsp->m_ft != kftDate);
		break;

	case kftGenDate:	// Generic date (covers BC/AD and fuzzy dates).
		BuildGenDateString(pfsp, hvoRec, ws, pptss);
		break;

	case kftSubItems:	// Sub items (Hierarchical field type).
		Assert(pfsp->m_ft != kftSubItems);
		break;
	case kftObjRefAtomic:
		// BuildObjRefAtomicString(pfsp, hvoRec, ws, pptss);
		if (m_qfcex)
		{
			IFwFldSpecPtr qffsp;
			GetFwFldSpec(pfsp, &qffsp);
			CheckHr(m_qfcex->BuildObjRefAtomicString(qffsp, hvoRec, ws, pptss));
		}
		break;
	case kftObjRefSeq:
		// BuildObjRefSeqString(pfsp, hvoRec, ws, pptss));
		if (m_qfcex)
		{
			IFwFldSpecPtr qffsp;
			GetFwFldSpec(pfsp, &qffsp);
			CheckHr(m_qfcex->BuildObjRefSeqString(qffsp, hvoRec, ws, pptss));
		}
		break;
	case kftInteger:
		BuildIntegerString(pfsp, hvoRec, ws, pptss);
		break;
	case kftBackRefAtomic:
		Assert(pfsp->m_ft != kftBackRefAtomic);
		break;
	case kftExpandable:
		if (m_qfcex)
		{
			IFwFldSpecPtr qffsp;
			GetFwFldSpec(pfsp, &qffsp);
			CheckHr(m_qfcex->BuildExpandableString(qffsp, hvoRec, ws, pptss));
		}
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the value for a multilingual
	string type field in the current record.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param vws Language encodings desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildMultiTsString(FldSpec * pfsp, HVO hvoRec, Vector<int> & vws,
	ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	AssertPtr(pptss);
	Assert(vws.Size());

	*pptss = NULL;
	try
	{
		ComVector<ITsString> vqtss;
		bool fHaveData = false;
		for (int iws = 0; iws < vws.Size(); ++iws)
		{
			ITsStringPtr qtss;
			LoadTsString(pfsp, hvoRec, vws[iws], &qtss);
			vqtss.Push(qtss);
			if (qtss)
				fHaveData = true;
		}
		if (fHaveData || pfsp->m_eVisibility == kFTVisAlways)
		{
			ITsIncStrBldrPtr qtisb;
			StrUni stuLangCodeStyle(L"Language Code");
			StrUni stuSty;
			if (!m_hmflidstuCharStyle.Retrieve(pfsp->m_flid, &stuSty))
				stuSty.Clear();		// sheer paranoia.
			if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
			{
				ITsStrBldrPtr qtsb;
				CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
				int cch;
				CheckHr(qtsb->get_Length(&cch));
				if (cch)
				{
					CheckHr(qtsb->ReplaceRgch(cch, cch, L":", 1, NULL));
					CheckHr(qtsb->SetStrPropValue(0, cch + 1, ktptNamedStyle,
								m_stuLabelFormat.Bstr()));
					ITsStringPtr qtssLabel;
					CheckHr(qtsb->GetString(&qtssLabel));
					CheckHr(qtssLabel->GetIncBldr(&qtisb));
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
					CheckHr(qtisb->AppendRgch(L" ", 1));
				}
			}
			bool fField = false;
			SmartBstr sbstr;
			int wsUser;
			CheckHr(m_qwsf->get_UserWs(&wsUser));
			for (int iws = 0; iws < vws.Size(); ++iws)
			{
				// Make sure we have a string builder even if we don't have a label.
				if (!qtisb)
				{
					ITsStrFactoryPtr qtsf;
					qtsf.CreateInstance(CLSID_TsStrFactory);
					AssertPtr(qtsf);
					CheckHr(qtsf->GetIncBldr(&qtisb));
				}
				// The first time, mark the field.  From then on, just add a space
				if (!fField)
				{
					fField = true;
					CheckHr(qtisb->SetStrPropValue(ktptFieldName, pfsp->m_stuFldName.Bstr()));
				}
				else
				{
					CheckHr(qtisb->AppendRgch(L" ", 1));
				}
				// Add the language code.
				CheckHr(m_qwsf->GetStrFromWs(vws[iws], &sbstr));
				CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, wsUser));
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuLangCodeStyle.Bstr()));
				CheckHr(qtisb->AppendRgch(sbstr.Chars(), sbstr.Length()));
				// Add the string, if it exists.
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
				CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, vws[iws]));
				CheckHr(qtisb->AppendRgch(L" ", 1));
				if (vqtss[iws])
				{
					ITsStrBldrPtr qtsb;
					CheckHr(vqtss[iws]->GetBldr(&qtsb));
					int cch;
					CheckHr(qtsb->get_Length(&cch));
					CheckHr(qtsb->SetStrPropValue(0, cch, ktptFieldName,
						pfsp->m_stuFldName.Bstr()));
					ITsStringPtr qtss;
					CheckHr(qtsb->GetString(&qtss));
					CheckHr(qtisb->AppendTsString(qtss));
					CheckHr(qtisb->SetStrPropValue(ktptFieldName, pfsp->m_stuFldName.Bstr()));
				}
				else
				{
					CheckHr(qtisb->AppendRgch(L" ", 1));
				}
			}
			if (qtisb)
			{
				if (fField)
					CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
				CheckHr(qtisb->GetString(pptss));
			}
		}
	}
	catch (...)
	{
	}
}

/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the value for a string type field
	in the current record.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildTsString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	AssertPtr(pptss);

	*pptss = NULL;
	try
	{
		ITsStringPtr qtss;
		LoadTsString(pfsp, hvoRec, ws, &qtss);
		if (qtss || pfsp->m_eVisibility == kFTVisAlways)
		{
			if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
			{
				ITsStrBldrPtr qtsb;
				CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
				int cch;
				CheckHr(qtsb->get_Length(&cch));
				CheckHr(qtsb->ReplaceRgch(cch, cch, L":", 1, NULL));
				CheckHr(qtsb->SetStrPropValue(0, cch + 1, ktptNamedStyle,
					m_stuLabelFormat.Bstr()));
				ITsStringPtr qtssLabel;
				CheckHr(qtsb->GetString(&qtssLabel));
				ITsIncStrBldrPtr qtisb;
				CheckHr(qtssLabel->GetIncBldr(&qtisb));
				StrUni stuSty;
				if (m_hmflidstuCharStyle.Retrieve(pfsp->m_flid, &stuSty))
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
				else
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
				CheckHr(qtisb->AppendRgch(L" ", 1));
				if (qtss)
				{
					ITsStrBldrPtr qtsb;
					CheckHr(qtss->GetBldr(&qtsb));
					int cch;
					CheckHr(qtsb->get_Length(&cch));
					CheckHr(qtsb->SetStrPropValue(0, cch, ktptFieldName,
						pfsp->m_stuFldName.Bstr()));
					CheckHr(qtsb->GetString(&qtss));
					CheckHr(qtisb->AppendTsString(qtss));
				}
				CheckHr(qtisb->GetString(pptss));
			}
			else if (qtss)
			{
				ITsStrBldrPtr qtsb;
				CheckHr(qtss->GetBldr(&qtsb));
				int cch;
				CheckHr(qtsb->get_Length(&cch));
				CheckHr(qtsb->SetStrPropValue(0, cch, ktptFieldName,
					pfsp->m_stuFldName.Bstr()));
				CheckHr(qtsb->GetString(pptss));
			}
		}
	}
	catch (...)
	{
	}
}

/*----------------------------------------------------------------------------------------------
	Create a TsString object from the database data for the given field in the current record.
	The database field may be either formatted text or plain (Unicode) text, may be either
	"big" or normal sized, and may be multi-lingual or monolingual.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::LoadTsString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	AssertPtr(pptss);

	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		*pptss = NULL;
		StrUni stuQuery;
		ComBool fMoreRows;
		ComBool fIsNull;
		ULONG cbSpaceTaken;
		bool fFormatted = false;
		int eType = 0;
		if (pfsp->m_flid)
			CheckHr(m_qmdc->GetFieldType(pfsp->m_flid, &eType));
		SmartBstr sbstrClassName;
		CheckHr(m_qmdc->GetOwnClsName(pfsp->m_flid, &sbstrClassName));
		SmartBstr sbstrFieldName;
		CheckHr(m_qmdc->GetFieldName(pfsp->m_flid, &sbstrFieldName));

		switch (eType)
		{
		case kcptString:
		case kcptBigString:
			stuQuery.Format(L"SELECT [%s],[%s_Fmt] FROM [%s] WHERE [Id]=%d",
				pfsp->m_stuFldName.Chars(), pfsp->m_stuFldName.Chars(),
				pfsp->m_stuClsName.Chars(), hvoRec);
			fFormatted = true;
			break;
		case kcptMultiString:
			stuQuery.Format(L"SELECT [Txt],[Fmt] FROM [MultiStr$]"
				L" WHERE [Flid]=%d AND [Obj]=%d AND [Ws]=%d",
				pfsp->m_flid, hvoRec, ws);
			fFormatted = true;
			break;
		case kcptMultiBigString:
			stuQuery.Format(L"SELECT [Txt],[Fmt] FROM [MultiBigStr$]"
				L" WHERE [Flid]=%d AND [Obj]=%d AND [Ws]=%d",
				pfsp->m_flid, hvoRec, ws);
			fFormatted = true;
			break;
		case kcptUnicode:
		case kcptBigUnicode:
			stuQuery.Format(L"SELECT [%s] FROM [%s] WHERE [Id]=%d",
				pfsp->m_stuFldName.Chars(), pfsp->m_stuClsName.Chars(), hvoRec);
			break;
		case kcptMultiUnicode:
			stuQuery.Format(L"SELECT [Txt] FROM %s_%s itm"
				L" WHERE [Obj]=%d AND [Ws]=%d",
				sbstrClassName.Chars(), sbstrFieldName.Chars(), hvoRec, ws);
			break;
		case kcptMultiBigUnicode:
			stuQuery.Format(L"SELECT [Txt] FROM [MultiBigTxt$]"
				L" WHERE [Flid]=%d AND [Obj]=%d AND [Ws]=%d",
				pfsp->m_flid, hvoRec, ws);
			break;
		default:
			return;
		}
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			Vector<OLECHAR> vchTxt;
			Vector<byte> vbFmt;
			int cbFmt = 0;
			vchTxt.Resize(512);
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(vchTxt.Begin()),
				sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				ITsStringPtr qtss;
				int cchTxt = cbSpaceTaken / isizeof(OLECHAR);
				if (cchTxt > vchTxt.Size())
				{
					vchTxt.Resize(cchTxt, true);
					CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(vchTxt.Begin()),
						sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken, &fIsNull, 0));
				}
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				AssertPtr(qtsf);
				if (fFormatted)
				{
					vbFmt.Resize(512);
					CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(vbFmt.Begin()),
						vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull)
					{
						cbFmt = cbSpaceTaken;
						if (cbFmt > vbFmt.Size())
						{
							vbFmt.Resize(cbFmt, true);
							CheckHr(qodc->GetColValue(2,
								reinterpret_cast <BYTE *>(vbFmt.Begin()), vbFmt.Size(),
								&cbSpaceTaken, &fIsNull, 0));
						}
					}
				}
				qodc.Clear();
				if (cbFmt)
				{
					CheckHr(qtsf->DeserializeStringRgch(vchTxt.Begin(), &cchTxt,
						vbFmt.Begin(), &cbFmt, &qtss));
				}
				else
				{
					CheckHr(qtsf->MakeStringRgch(vchTxt.Begin(), cchTxt, ws, &qtss));
				}
				bool fHaveStyle;
				StrUni stuSty;
				if (pfsp->m_stuSty.Length())
				{
					stuSty = pfsp->m_stuSty;
					fHaveStyle = true;
				}
				else
				{
					fHaveStyle = m_hmflidstuCharStyle.Retrieve(pfsp->m_flid, &stuSty);
				}
				if (fHaveStyle)
				{
					ITsStrBldrPtr qtsb;
					int cch;
					CheckHr(qtss->GetBldr(&qtsb));
					CheckHr(qtsb->get_Length(&cch));
					CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle, stuSty.Bstr()));
					CheckHr(qtsb->GetString(pptss));
				}
				else
				{
					*pptss = qtss.Detach();
				}
				return;
			}
		}
	}
	catch (...)
	{
		*pptss = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the integer value for the
	current record.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildIntegerString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	AssertPtr(pptss);
	Assert(hvoRec);

	*pptss = NULL;
	int nVal = 0;
	ComBool fIsNull;
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		StrUni stuQuery;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		stuQuery.Format(L"SELECT [%s] FROM [%s] WHERE [Id]=%d",
			pfsp->m_stuFldName.Chars(), pfsp->m_stuClsName.Chars(), hvoRec);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&nVal),
				sizeof(nVal), &cbSpaceTaken, &fIsNull, 0));
		}
		else
		{
			fIsNull = TRUE;
		}
	}
	catch (...)
	{
		fIsNull = TRUE;
	}
	if (!fIsNull || pfsp->m_eVisibility == kFTVisAlways)
	{
		ITsIncStrBldrPtr qtisb;
		ITsStringPtr qtss;
		if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
		{
			ITsStrBldrPtr qtsb;
			CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
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
		if (!fIsNull)
		{
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, pfsp->m_stuFldName.Bstr()));
			StrUni stu;
			stu.Format(L"%d", nVal);
			bool fHaveStyle = false;
			StrUni stuSty;
			if (pfsp->m_stuSty.Length())
			{
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, pfsp->m_stuSty.Bstr()));
				fHaveStyle = true;
			}
			else if (m_hmflidstuCharStyle.Retrieve(pfsp->m_flid, &stuSty))
			{
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
				fHaveStyle = true;
			}
			CheckHr(qtisb->Append(stu.Bstr()));
			if (fHaveStyle)
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
		}
		CheckHr(qtisb->GetString(pptss));
	}
}

/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the GUID value for the
	current record.

	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildGuidString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	AssertPtr(pptss);
	Assert(hvoRec);

	*pptss = NULL;
	GUID uid;
	ComBool fIsNull;
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		StrUni stuQuery;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		stuQuery.Format(L"SELECT [Guid$] FROM [%s]_ WHERE [Id]=%d",
			pfsp->m_stuClsName.Chars(), hvoRec);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&uid), isizeof(GUID),
				&cbSpaceTaken, &fIsNull, 0));
		}
		else
		{
			fIsNull = TRUE;
		}
	}
	catch (...)
	{
		fIsNull = TRUE;
	}
	if (!fIsNull || pfsp->m_eVisibility == kFTVisAlways)
	{
		ITsIncStrBldrPtr qtisb;
		ITsStringPtr qtss;
		if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
		{
			ITsStrBldrPtr qtsb;
			CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
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
		if (!fIsNull)
		{
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, pfsp->m_stuFldName.Bstr()));
			StrUni stu;
			stu.Format(L"%g", &uid);
			bool fHaveStyle = false;
			StrUni stuSty;
			if (pfsp->m_stuSty.Length())
			{
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, pfsp->m_stuSty.Bstr()));
				fHaveStyle = true;
			}
			else if (m_hmflidstuCharStyle.Retrieve(pfsp->m_flid, &stuSty))
			{
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
				fHaveStyle = true;
			}
			CheckHr(qtisb->Append(stu.Bstr()));
			if (fHaveStyle)
				CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
			CheckHr(qtisb->SetStrPropValue(ktptFieldName, NULL));
		}
		CheckHr(qtisb->GetString(pptss));
	}
}

/*----------------------------------------------------------------------------------------------
	Write this record out in WorldPad XML format, based on the given user view specification.

	@param pstrm Pointer to the IStream object for output.
	@param hcRec The database id and class id of the record.
	@param nLevel (Indentation) level of the record (0 means top level, >= 1 means subrecord)
----------------------------------------------------------------------------------------------*/
void AfExportDlg::WriteRecord(IStream * pstrm, HvoClsid & hcRec, int nLevel)
{
	AssertPtr(pstrm);
	AssertPtr(m_plpi);
	AssertPtr(m_puvs);

	ClsLevel clev(hcRec.clsid, nLevel);
	RecordSpecPtr qrsp;
	while (!m_puvs->m_hmclevrsp.Retrieve(clev, qrsp))
	{
		--clev.m_nLevel;
		if (clev.m_nLevel < 0)
		{
			// error message?
			return;
		}
	}

	// Get the actual indentation level, which may or may not be the same as nLevel.
	int ws = m_plpi->ActualWs(qrsp->m_vqbsp[0]->m_ws);
	ULONG cbOut;
	StrAnsi sta;
	SmartBstr sbstrStartTag;
	SmartBstr sbstrEndTag;
	int nLevelIndent = nLevel;
	if (m_qfcex)
	{
		CheckHr(m_qfcex->GetActualLevel(nLevel, hcRec.hvo, ws, &nLevelIndent));

		HRESULT hr;
		IgnoreHr(hr = m_qfcex->BuildRecordTags(nLevelIndent, hcRec.hvo, hcRec.clsid,
			&sbstrStartTag, &sbstrEndTag));
		if (FAILED(hr))
		{
			sbstrStartTag.Clear();
			sbstrEndTag.Clear();
		}
	}

	StrAnsi staStartTag;
	if (sbstrStartTag.Length())
		staStartTag.Assign(sbstrStartTag.Chars(), sbstrStartTag.Length());
	else
		staStartTag.Format("<Entry level=\"%d\">%n", nLevelIndent);
	CheckHr(pstrm->Write(staStartTag.Chars(), staStartTag.Length(), &cbOut));

#ifdef DUMP_VIEW_DEFINITION
	SmartBstr sbstrClass;
	m_qmdc->GetClassName(hcRec.clsid, &sbstrClass);
	sta.Format("<!-- [%S (%d), %d]:  hvo = %d, nLevel = %d (%d) -->%n",
		sbstrClass.Chars(), clev.m_clsid, clev.m_nLevel, hcRec.hvo, nLevel, nLevelIndent);
	CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
#endif
	AssertPtr(qrsp);
	int cbsp = qrsp->m_vqbsp.Size();
	for (int ibsp = 0; ibsp < cbsp; ++ibsp)
	{
		BlockSpec * pbsp = qrsp->m_vqbsp[ibsp];

		// Unimplemented types.  If one asserts, implement it!
		Assert(pbsp->m_ft != kftTtp);
		Assert(pbsp->m_ft != kftLimEmbedLabel);
		Assert(pbsp->m_ft != kftDate);
		Assert(pbsp->m_ft != kftBackRefAtomic);

		// Skip fields that are never shown.
		if (pbsp->m_eVisibility == kFTVisNever &&
			pbsp->m_ft != kftGroup && pbsp->m_ft != kftGroupOnePerLine)
		{
			continue;
		}

		ws = m_plpi->ActualWs(pbsp->m_ws);

		ITsStringPtr qtss;
		// Assume that the first item shown is the heading for this record.  (What else could
		// serve this purpose?)
		// ENHANCE (TE-693): Now that this code is used for exporting Scripture, this is no longer a
		// valid assumption. This code should be made optional so we don't get Heading N styles in\
		// our Scripture export.
		StrUni stuStyle;
		if (!ibsp)
		{
			stuStyle = (nLevelIndent < knMaxLevels) ?
				m_rgstuHeadings[nLevelIndent] : m_rgstuHeadings[knMaxLevels-1];
		}

		switch (pbsp->m_ft)
		{
		case kftString:		// TsString string.
		case kftUnicode:	// A plain Unicode string property.
		case kftTitleGroup:			// Special group of a main text field with record type.
			BuildTsString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws, &qtss);
			break;
		case kftMsa:		// Multi string alternative.
		case kftMta:		// MultiText alternative.
			if (pbsp->m_ws == kwsAnals)
			{
				BuildMultiTsString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, m_plpi->AnalWss(),
					&qtss);
			}
			else if (pbsp->m_ws == kwsVerns)
			{
				BuildMultiTsString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, m_plpi->VernWss(),
					&qtss);
			}
			else if (pbsp->m_ws == kwsAnalVerns)
			{
				BuildMultiTsString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo,
					m_plpi->AnalVernWss(), &qtss);
			}
			else if (pbsp->m_ws == kwsVernAnals)
			{
				BuildMultiTsString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo,
					m_plpi->VernAnalWss(), &qtss);
			}
			else
			{
				BuildTsString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws, &qtss);
			}
			break;
		case kftRefAtomic:	// Atomic ref to a PossibilityItem set via the poss. chooser.
		case kftRefCombo:	// Atomic ref to a PossibilityItem set via a combo box.
			BuildRefAtomicString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws, &qtss);
			break;
		case kftRefSeq:		// Seq of refs to PossibilityItem set via the poss. chooser.
			BuildRefSeqString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws, &qtss);
			break;
		case kftEnum:		// An enumeration of possible values set via a combo box.
			BuildEnumString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws, &qtss);
			break;
		case kftTtp:		// A binary column in the database interpreted as a TsTextProps.
			break;

		case kftStTextParas:		// Paragraphs of an Structured Text
		case kftStText:				// Structured text (Block only).
			WriteStructuredText(pstrm, dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws,
				nLevelIndent);
			continue;

		case kftDummy:				// Fake field, no data to load.
			break;					// (Usually a subfield of a kftTitleGroup.)
		case kftLimEmbedLabel:		// On blocks, smaller FTs mean embed label in para.
			break;
		case kftGroup:				// Group (Block only: use fields in FldVec).
			BuildGroupString(pbsp->m_vqfsp, hcRec.hvo, ws, &qtss);
			break;

		case kftGroupOnePerLine:	// Like group, but each field uses a new indented line
			WriteGroupOnePerLine(pstrm, pbsp, hcRec.hvo, ws, nLevelIndent);
			continue;

		case kftDateRO:				// Standard read-only date/time as stored in MSDE.
			BuildDateROString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws, &qtss);
			break;
		case kftDate:				// Standard editable date/time as stored in MSDE.
			break;

		case kftGenDate:			// Generic date (covers BC/AD and fuzzy dates).
			BuildGenDateString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws, &qtss);
			break;

		case kftSubItems:			// Sub items (Hierarchical field type).
			if (pbsp->m_flid == m_flidSubItems)
				WriteSubItems(pstrm, dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws,
					nLevelIndent);
			continue;

		case kftObjOwnCol:			// Owned collection.
		case kftObjOwnSeq:			// Owned sequence.
			WriteSubItems(pstrm, dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws,
				nLevelIndent);
			continue;

		case kftObjRefAtomic:		// Atomic reference to non-PossibilityItem.
			if (m_qfcex)
			{
				IFwFldSpecPtr qffsp;
				GetFwFldSpec(dynamic_cast<FldSpec *>(pbsp), &qffsp);
				CheckHr(m_qfcex->BuildObjRefAtomicString(qffsp, hcRec.hvo, ws, &qtss));
			}
			break;
		case kftObjRefSeq:			// Seq of refs to non-PossibilityItems.
			if (m_qfcex)
			{
				IFwFldSpecPtr qffsp;
				GetFwFldSpec(dynamic_cast<FldSpec *>(pbsp), &qffsp);
				CheckHr(m_qfcex->BuildObjRefSeqString(qffsp, hcRec.hvo, ws, &qtss));
			}
			break;
		case kftInteger:			// Integer property.
			BuildIntegerString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws, &qtss);
			break;
		case kftBackRefAtomic:		// BackRef from atomic ref (no editor type for this yet)
			break;
		case kftExpandable:
			if (m_qfcex)
			{
				IFwFldSpecPtr qffsp;
				GetFwFldSpec(dynamic_cast<FldSpec *>(pbsp), &qffsp);
				CheckHr(m_qfcex->BuildExpandableString(qffsp, hcRec.hvo, ws, &qtss));
			}
			break;
		case kftGuid:
			BuildGuidString(dynamic_cast<FldSpec *>(pbsp), hcRec.hvo, ws, &qtss);
			break;

		default:
			break;
		}
		if (qtss || pbsp->m_eVisibility == kFTVisAlways || stuStyle.Length())
		{
			if (pbsp->m_flid)
			{
				if (stuStyle.Length())
					WriteParagraph(pstrm, qtss, stuStyle, nLevelIndent * knIndentPerLevel,
						pbsp->m_stuFldName, nLevelIndent);
				else
					WriteParagraph(pstrm, qtss, pbsp->m_flid, nLevelIndent,
						pbsp->m_stuFldName);
			}
			else
			{
				// Get StrUni from the group label TsString.
				Assert(pbsp->m_ft == kftGroup);
				SmartBstr sbstr;
				CheckHr(pbsp->m_qtssLabel->get_Text(&sbstr));
				StrUni stuGroup(sbstr.Chars());
				stuGroup.Append(L" (Group)");
				if (stuStyle.Length())
					WriteParagraph(pstrm, qtss, stuStyle, nLevelIndent * knIndentPerLevel,
						stuGroup, nLevelIndent);
				else
					WriteParagraph(pstrm, qtss, pbsp->m_flid, nLevelIndent, stuGroup);
			}
		}
		if (m_qprog && m_qprog->CheckCancel())
			return;
	}
	StrAnsi staEndTag;
	if (sbstrEndTag.Length())
		staEndTag.Assign(sbstrEndTag.Chars(), sbstrEndTag.Length());
	else
		staEndTag.Format("</Entry>%n");
	CheckHr(pstrm->Write(staEndTag.Chars(), staEndTag.Length(), &cbOut));
}

/*----------------------------------------------------------------------------------------------
	Write a structured text field in WorldPad XML format.

	@param pstrm Pointer to the IStream object for output.
	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param nLevel (Indentation) level of the record (0 means top level, >= 1 means subrecord)
----------------------------------------------------------------------------------------------*/
void AfExportDlg::WriteStructuredText(IStream * pstrm, FldSpec * pfsp, HVO hvoRec, int ws,
	int nLevel)
{
	ComVector<ITsString> vqtss;
	ComVector<ITsString> vqtssLabel;
	Vector<StrUni> vstuStyleName;
	ComVector<ITsTextProps> vqttpRules;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	AssertPtr(qtsf);

	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		ULONG cbSpaceTaken;
		ComBool fMoreRows;
		ComBool fIsNull;

		ITsPropsFactoryPtr qtpf;
		qtpf.CreateInstance(CLSID_TsPropsFactory);
		AssertPtr(qtpf);
		StrUni stuQuery;
		if (pfsp->m_ft == kftStText)
		{
			stuQuery.Format(L"SELECT st.Contents, st.Contents_Fmt, "
				L"st.Label, st.Label_Fmt, "
				L"st.StyleName, "
				L"st.StyleRules%n"
				L"FROM %s_%s xx%n"
				L"JOIN StTxtPara_ st ON st.Owner$ = xx.Dst%n"
				L"WHERE xx.Src = %d order by st.OwnOrd$",
				pfsp->m_stuClsName.Chars(), pfsp->m_stuFldName.Chars(), hvoRec);
		}
		else
		{
			stuQuery.Format(L"SELECT st.Contents, st.Contents_Fmt, "
				L"st.Label, st.Label_Fmt, "
				L"st.StyleName, "
				L"st.StyleRules%n"
				L"FROM StTxtPara_ st "
				L"WHERE st.Owner$ = %d order by st.OwnOrd$",
				hvoRec);
		}
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(),
			knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Vector<OLECHAR> vchTxt;
		Vector<byte> vbFmt;
		StrUni stuName;
		vchTxt.Resize(512);
		vbFmt.Resize(512);
		ITsStringPtr qtss;
		ITsTextPropsPtr qttp;
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1,
				reinterpret_cast <BYTE *>(vchTxt.Begin()),
				sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				int cchTxt = cbSpaceTaken / isizeof(OLECHAR);
				if (cchTxt > vchTxt.Size())
				{
					vchTxt.Resize(cchTxt, true);
					CheckHr(qodc->GetColValue(1,
						reinterpret_cast <BYTE *>(vchTxt.Begin()),
						sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken,
						&fIsNull, 0));
				}
				CheckHr(qodc->GetColValue(2,
					reinterpret_cast <BYTE *>(vbFmt.Begin()),
					vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
				{
					// We must have a valid Contents string to have anything.
					int cbFmt = cbSpaceTaken;
					if (cbFmt > vbFmt.Size())
					{
						vbFmt.Resize(cbFmt, true);
						CheckHr(qodc->GetColValue(2,
							reinterpret_cast <BYTE *>(vbFmt.Begin()),
							vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
					}
					CheckHr(qtsf->DeserializeStringRgch(vchTxt.Begin(), &cchTxt,
						vbFmt.Begin(), &cbFmt, &qtss));
					ITsStrBldrPtr qtsb;
					CheckHr(qtss->GetBldr(&qtsb));
					int cch;
					CheckHr(qtsb->get_Length(&cch));
					CheckHr(qtsb->SetStrPropValue(0, cch, ktptFieldName,
						pfsp->m_stuFldName.Bstr()));
					CheckHr(qtsb->GetString(&qtss));
					vqtss.Push(qtss);
					qtss.Clear();
					CheckHr(qodc->GetColValue(3,
						reinterpret_cast <BYTE *>(vchTxt.Begin()),
						sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken,
						&fIsNull, 0));
					if (!fIsNull)
					{
						cchTxt = cbSpaceTaken / isizeof(OLECHAR);
						if (cchTxt > vchTxt.Size())
						{
							vchTxt.Resize(cchTxt, true);
							CheckHr(qodc->GetColValue(3,
								reinterpret_cast <BYTE *>(vchTxt.Begin()),
								sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken,
								&fIsNull, 0));
						}
						CheckHr(qodc->GetColValue(4,
							reinterpret_cast <BYTE *>(vbFmt.Begin()),
							vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
						if (!fIsNull)
						{
							cbFmt = cbSpaceTaken;
							if (cbFmt > vbFmt.Size())
							{
								vbFmt.Resize(cbFmt, true);
								CheckHr(qodc->GetColValue(4,
									reinterpret_cast <BYTE *>(vbFmt.Begin()),
									vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
							}
							CheckHr(qtsf->DeserializeStringRgch(vchTxt.Begin(),
								&cchTxt, vbFmt.Begin(), &cbFmt, &qtss));
						}
					}
					vqtssLabel.Push(qtss);
					CheckHr(qodc->GetColValue(5,
						reinterpret_cast <BYTE *>(vchTxt.Begin()),
						sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken,
						&fIsNull, 0));
					if (!fIsNull)
					{
						cchTxt = cbSpaceTaken / isizeof(OLECHAR);
						if (cchTxt > vchTxt.Size())
						{
							vchTxt.Resize(cchTxt, true);
							CheckHr(qodc->GetColValue(5,
								reinterpret_cast <BYTE *>(vchTxt.Begin()),
								sizeof(OLECHAR) * vchTxt.Size(), &cbSpaceTaken,
								&fIsNull, 0));
						}
						stuName.Assign(vchTxt.Begin(), cchTxt);
					}
					else
					{
						stuName.Clear();
					}
					vstuStyleName.Push(stuName);
					CheckHr(qodc->GetColValue(6,
						reinterpret_cast <BYTE *>(vbFmt.Begin()), vbFmt.Size(),
						&cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull)
					{
						cbFmt = cbSpaceTaken;
						if (cbFmt > vbFmt.Size())
						{
							vbFmt.Resize(cbFmt, true);
							CheckHr(qodc->GetColValue(6,
								reinterpret_cast <BYTE *>(vbFmt.Begin()),
								vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
						}
						CheckHr(qtpf->DeserializePropsRgb(vbFmt.Begin(), &cbFmt,
							&qttp));
					}
					else
					{
						qttp.Clear();
					}
					vqttpRules.Push(qttp);
				}
			}
			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)
	{
		vqtss.Clear();
	}
	int cchPara = 0;
	for (int itss = 0; itss < vqtss.Size(); ++itss)
	{
		CheckHr(vqtss[itss]->get_Length(&cchPara));
		if (cchPara)
			break;
	}
	StrAnsi sta;
	ULONG cbOut;
	if (cchPara || pfsp->m_eVisibility == kFTVisAlways)
	{
		if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
		{
			ITsStrBldrPtr qtsb;
			ITsStringPtr qtss;
			CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
			int cch;
			CheckHr(qtsb->get_Length(&cch));
			CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle,
				m_stuLabelFormat.Bstr()));
			CheckHr(qtsb->ReplaceRgch(cch, cch, L":", 1, NULL));
			CheckHr(qtsb->GetString(&qtss));
			WriteParagraph(pstrm, qtss, 0, nLevel, pfsp->m_stuFldName);
		}
		sta.Format("<Field name=\"%S\" type=\"StText\">%n", pfsp->m_stuFldName.Chars());
		CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
		StrUni stuSty;
		m_hmflidstuParaStyle.Retrieve(pfsp->m_flid, &stuSty);
		for (int itss = 0; itss < vqtss.Size(); ++itss)
		{
			if (vqttpRules[itss])
			{
				sta.Format("<StTxtPara>%n");
				CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
				sta.Format("<StyleRules15>%n");
				CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
				// Check whether the paragraph already has a named style.
				SmartBstr sbstrStyle;
				CheckHr(vqttpRules[itss]->GetStrPropValue(ktptNamedStyle, &sbstrStyle));
				// If no explicit named style, use the named style for this field if it exists.
				bool fSetStyle = !sbstrStyle.Length() && stuSty.Length();
				if (fSetStyle || nLevel > 0)
				{
					ITsPropsBldrPtr qtpb;
					CheckHr(vqttpRules[itss]->GetBldr(&qtpb));
					if (nLevel > 0)
					{
						int nVar;
						int nVal;
						CheckHr(qtpb->GetIntPropValues(ktptLeadingIndent, &nVar,
							&nVal));
						if (nVal == -1 && nVar == -1)
						{
							nVar = ktpvMilliPoint;
							nVal = nLevel * knIndentPerLevel;
						}
						else
						{
							// add to any existing ktptLeadingIndent
							if (nVar == ktpvMilliPoint)
								nVal += nLevel * knIndentPerLevel;
							else
								Assert(nVar == ktpvMilliPoint);
						}
						CheckHr(qtpb->SetIntPropValues(ktptLeadingIndent, nVar,
									nVal));
					}
					if (fSetStyle)
						CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, stuSty.Bstr()));
					CheckHr(qtpb->GetTextProps(&vqttpRules[itss]));
				}
				vqttpRules[itss]->WriteAsXml(pstrm, m_qwsf, 0);
				sta.Format("</StyleRules15>%n"
					"<Contents16>%n");
				CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
				ComBool fWriteObjData = false;
				if (m_qfcex)
					CheckHr(m_qfcex->IncludeObjectData(&fWriteObjData));
				vqtss[itss]->WriteAsXml(pstrm, m_qwsf, 0, 0, fWriteObjData);
				sta.Format("</Contents16>%n"
					"</StTxtPara>%n");
				CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
			}
			else
			{
				WriteParagraph(pstrm, vqtss[itss], pfsp->m_flid, nLevel, pfsp->m_stuFldName);
			}
			if (m_qprog && m_qprog->CheckCancel())
				return;
		}
		if (!vqtss.Size())
		{
			// This results in an empty string with the correct default writing system.
			ITsStrBldrPtr qtsb;
			CheckHr(qtsf->GetBldr(&qtsb));
			CheckHr(qtsb->SetIntPropValues(0, 0, ktptWs, 0, m_plpi->AnalWss()[0]));
			ITsStringPtr qtss;
			CheckHr(qtsb->GetString(&qtss));
			WriteParagraph(pstrm, qtss, pfsp->m_flid, nLevel, pfsp->m_stuFldName);
		}
		sta.Format("</Field>%n");
		CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
	}
}

/*----------------------------------------------------------------------------------------------
	Write a group of related fields in WorldPad XML format, one per paragraph ("line").

	@param pstrm Pointer to the IStream object for output.
	@param pbsp Pointer to the current user view "block" (of fields) specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param nLevel (Indentation) level of the record (0 means top level, >= 1 means subrecord)
----------------------------------------------------------------------------------------------*/
void AfExportDlg::WriteGroupOnePerLine(IStream * pstrm, BlockSpec * pbsp, HVO hvoRec,
	int ws, int nLevel)
{
	bool fHeadingShown = false;
	int ifsp;
	StrUni stuSty;
	for (ifsp = 0; ifsp < pbsp->m_vqfsp.Size(); ++ifsp)
	{
		ITsStringPtr qtss;
		FldSpec * pfsp = pbsp->m_vqfsp[ifsp];
		if (pfsp->m_eVisibility == kFTVisNever)
			continue;
		BuildGroupLine(pfsp, hvoRec, ws, &qtss);
		if (qtss || pfsp->m_eVisibility == kFTVisAlways)
		{
			if (!fHeadingShown)
			{
				if (!pbsp->m_fHideLabel && pbsp->m_qtssLabel)
				{
					ITsStrBldrPtr qtsb;
					ITsStringPtr qtssHeading;
					CheckHr(pbsp->m_qtssLabel->GetBldr(&qtsb));
					int cch;
					CheckHr(qtsb->get_Length(&cch));
					CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle,
						m_stuLabelFormat.Bstr()));
					CheckHr(qtsb->GetString(&qtssHeading));
					// Get StrUni from the group label TsString.
					SmartBstr sbstr;
					CheckHr(pbsp->m_qtssLabel->get_Text(&sbstr));
					StrUni stuGroup(sbstr.Chars());
					stuGroup.Append(L" (Group)");
					WriteParagraph(pstrm, qtssHeading, pbsp->m_flid, nLevel, stuGroup);
				}
				fHeadingShown = true;
			}
			if (!m_hmflidstuParaStyle.Retrieve(pfsp->m_flid, &stuSty))
				stuSty.Clear();		// Sheer paranoia.
			WriteParagraph(pstrm, qtss, stuSty,
				nLevel * knIndentPerLevel + (2 * knIndentPerLevel) / 3, pfsp->m_stuFldName,
				nLevel);
		}
		if (m_qprog && m_qprog->CheckCancel())
			return;
	}
}

/*----------------------------------------------------------------------------------------------
	Write the subitems of the given record in WorldPad XML format.

	@param pstrm Pointer to the IStream object for output.
	@param pfsp Pointer to the current user view field specification.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param nLevel (Indentation) level of the record (0 means top level, >= 1 means subrecord)
----------------------------------------------------------------------------------------------*/
void AfExportDlg::WriteSubItems(IStream * pstrm, FldSpec * pfsp, HVO hvoRec, int ws,
	int nLevel)
{
	AssertPtr(m_qode);
	AssertPtr(pfsp);
	Assert(hvoRec);

	HvoClsidVec vhcSub;
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		HvoClsid hc;
		StrUni stuQuery;
		ComBool fMoreRows;
		ComBool fIsNull;
		ULONG cbSpaceTaken;

		stuQuery.Format(L"SELECT xx.[Dst], co.[Class$] FROM [%s_%s] xx%n"
			L"    JOIN [CmObject] co ON co.[Id] = xx.[Dst]%n"
			L"WHERE xx.[Src]=%d ORDER BY xx.[Ord]",
			pfsp->m_stuClsName.Chars(), pfsp->m_stuFldName.Chars(), hvoRec);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hc.hvo), sizeof(hc.hvo),
				&cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hc.clsid),
					sizeof(hc.clsid), &cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
					vhcSub.Push(hc);
			}
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		qodc.Clear();

		// Ignore (pfsp->m_eVisibility == kFTVisAlways), since an isolated Subitems label looks
		// funny.
		if (vhcSub.Size())
		{
			StrAnsi sta;
			ULONG cbOut;
			if (!pfsp->m_fHideLabel && pfsp->m_qtssLabel)
			{
				sta.Format("<StTxtPara>%n");
				if (nLevel >= 0)
				{
					sta.FormatAppend("<StyleRules15>%n"
						"<Prop leadingIndent=\"%d\"/>%n"
						"</StyleRules15>%n",
						nLevel * knIndentPerLevel + knIndentPerLevel/3);
				}
				sta.FormatAppend("<Contents16>%n");
				CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
				ITsStrBldrPtr qtsb;
				ITsStringPtr qtss;
				CheckHr(pfsp->m_qtssLabel->GetBldr(&qtsb));
				int cch;
				CheckHr(qtsb->get_Length(&cch));
				CheckHr(qtsb->SetStrPropValue(0, cch, ktptNamedStyle,
					m_stuLabelFormat.Bstr()));
				CheckHr(qtsb->GetString(&qtss));
				qtss->WriteAsXml(pstrm, m_qwsf, 0, 0, FALSE);
				sta.Format("</Contents16>%n"
					"</StTxtPara>%n");
				CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
			}
			for (int ihc = 0; ihc < vhcSub.Size(); ++ihc)
			{
				WriteRecord(pstrm, vhcSub[ihc], nLevel + 1);		// Recurse!
				if (m_fCancelled)
					return;
			}
		}
	}
	catch (...)
	{
	}
}

/*----------------------------------------------------------------------------------------------
	Return a pointer to the character string containing the value of the given XML attribute,
	or NULL if that attribute is not defined for this XML element.

	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszName Name to search for in prgpszAtts.
----------------------------------------------------------------------------------------------*/
static void GetAttributeValue(const XML_Char ** prgpszAtts, const XML_Char * pszName,
	StrApp & strOut)
{
	strOut.Clear();
	if (!prgpszAtts)
		return;
	for (int i = 0; prgpszAtts[i]; i += 2)
	{
		if (strcmp(prgpszAtts[i], pszName) == 0)	// Assumes sizeof(XML_Char) == sizeof(char)
		{
			if (sizeof(achar) == sizeof(XML_Char))
			{
				strOut.Assign(prgpszAtts[i+1]);
				return;
			}
			else if (sizeof(achar) < sizeof(XML_Char))
			{
				Assert(sizeof(achar) == sizeof(char) && sizeof(XML_Char) == sizeof(wchar));
				// Convert from UTF-16 to UTF-8
				Vector<char> vch;
				int cch16 = wcslen((const wchar *)prgpszAtts[i+1]);
				int cch8 = CountUtf8FromUtf16((const wchar *)prgpszAtts[i+1], cch16);
				vch.Resize(cch8 + 1);
				int cch = ConvertUtf16ToUtf8(vch.Begin(), cch8, (const wchar *)prgpszAtts[i+1],
					cch16);
				Assert(cch == cch8);
				vch[cch] = 0;
				strOut.Assign(vch.Begin());
			}
			else if (sizeof(achar) > sizeof(XML_Char))
			{
				Assert(sizeof(achar) == sizeof(wchar) && sizeof(XML_Char) == sizeof(char));
				// Convert from UTF-8 to UTF-16
				Vector<wchar> vch;
				int cch16 = CountUtf16FromUtf8((const char *)prgpszAtts[i+1]);
				vch.Resize(cch16 + 1);
				int cch;
				cch = SetUtf16FromUtf8(vch.Begin(), cch16+1, (const char *)prgpszAtts[i+1]);
				Assert(cch == cch16);
				strOut.Assign(vch.Begin());
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements.

	This static method is passed to the expat XML parser as a callback function.  See the
	comments in xmlparse.h for the XML_StartElementHandler typedef for the documentation
	such as it is.

	@param pvUser Pointer to generic user data (always an AfExportStyleSheet object in this
					case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
static void HandleStartTag(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsi staElem(pszName);
	if (staElem == "silfw:file")
	{
		AfExportStyleSheet * pess = reinterpret_cast<AfExportStyleSheet *>(pvUser);
		AssertPtr(pess);
		GetAttributeValue(prgpszAtts, "title", pess->m_strTitle);
		GetAttributeValue(prgpszAtts, "outputext", pess->m_strOutputExt);
		GetAttributeValue(prgpszAtts, "description", pess->m_strDescription);
		StrApp str;
		GetAttributeValue(prgpszAtts, "chain", str);
		if (str.Length())
			pess->m_vstrChain.Push(str);
		GetAttributeValue(prgpszAtts, "views", pess->m_strViews);
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML end elements.

	This static method is passed to the expat XML parser as a callback function.

	@param pvUser Pointer to generic user data (always an AfExportStyleSheet object in this
					case).
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
static void HandleEndTag(void * pvUser, const XML_Char * pszName)
{
	// This is a trivial parse.  We don't need to do anything with the end tags.
}

/*----------------------------------------------------------------------------------------------
	Dynamically generate the list of possible output formats, getting the information from the
	installed XSL files in the [FieldWorks/RootCodeDir]/ExportOptions directory.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::GetExportOptions()
{
	// Get the FieldWorks code root directory.
	StrAppBufPath strbpFwRoot;
	strbpFwRoot.Assign(DirectoryFinder::FwRootCodeDir().Chars());

	// Look for all the export option *.xsl files, and extract the necessary information from
	// each of them.
	StrApp strDir(strbpFwRoot.Chars(), strbpFwRoot.Length());
	_tmkdir(strDir.Chars());
	strDir.Append(_T("\\ExportOptions"));
	_tmkdir(strDir.Chars());
	StrApp strFile;
	strFile.Format(_T("%s\\*.xsl*"), strDir.Chars());
	struct _tfinddata_t fileinfo;
	intptr_t hf = _tfindfirst(const_cast<achar *>(strFile.Chars()), &fileinfo);
	if (hf != -1)
	{
		AfExportStyleSheet ess;
		do
		{
			ess.m_strFile.Format(_T("%s\\%s"), strDir.Chars(), fileinfo.name);
			ess.m_strTitle.Clear();
			ess.m_strOutputExt.Clear();
			ess.m_strDescription.Clear();
			ess.m_vstrChain.Clear();
			ess.m_strViews.Clear();

			const XML_Char * pszWritingSystem = NULL;
			XML_Parser parser = XML_ParserCreate(pszWritingSystem);
			StrAnsi staFile(ess.m_strFile);
			if (!XML_SetBase(parser, staFile.Chars()))
			{
				XML_ParserFree(parser);
				continue;
			}
			try
			{
				XML_SetUserData(parser, &ess);
				XML_SetElementHandler(parser, HandleStartTag, HandleEndTag);
				IStreamPtr qstrm;
				FileStream::Create(ess.m_strFile.Chars(), kfstgmRead, &qstrm);
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
					if (ess.m_strTitle.Length() && ess.m_strOutputExt.Length())
					{
						// Sort the vector by title.
						int iv;
						int ivLim;
						for (iv = 0, ivLim = m_vess.Size(); iv < ivLim; )
						{
							int ivMid = (iv + ivLim) / 2;
							if (m_vess[ivMid].m_strTitle < ess.m_strTitle)
								iv = ivMid + 1;
							else
								ivLim = ivMid;
						}
						m_vess.Insert(iv, ess);
						break;
					}
				}
			}
			catch (...)
			{
				// Ignore any errors in parsing.
			}
			XML_ParserFree(parser);
		} while (!_tfindnext(hf, &fileinfo));
		_findclose(hf);
	}

	// Store the temporary file name for use later.
	strDir.Format(_T("%s\\Temp"), strbpFwRoot.Chars());
	_tmkdir(strDir.Chars());
	StrAnsi staDir(strDir.Chars());
	m_staTmpFile.Format("%s\\Export.xml", staDir.Chars());

	// Eliminate any export options which don't work with this view.
	int iess;
	for (iess = 0; iess < m_vess.Size(); ++iess)
	{
		int ich;
		switch (m_puvs->m_vwt)
		{
		case kvwtBrowse:
			// This shouldn't happen!  Browse view can't be exported!
			Assert(m_puvs->m_vwt != kvwtBrowse);
			ich = m_vess[iess].m_strViews.FindStrCI(_T("browse"));
			break;
		case kvwtDE:
			ich = m_vess[iess].m_strViews.FindStrCI(_T("dataentry"));
			break;
		case kvwtDoc:
			ich = m_vess[iess].m_strViews.FindStrCI(_T("document"));
			break;
		case kvwtConc:
			ich = m_vess[iess].m_strViews.FindStrCI(_T("concordance"));
			break;
		case kvwtDraft:
			ich = m_vess[iess].m_strViews.FindStrCI(_T("draft"));
			break;
		default:
			ich = -1;
			break;
		}
		if (ich < 0)
		{
			m_vess.Delete(iess);
			--iess;
		}
	}

	// Fill in the combobox with the list of available Output XML Transforms.
	HWND hwndCombo = ::GetDlgItem(m_hwnd, kctidExportType);
	m_strFilter.Clear();
	for (iess = 0; iess < m_vess.Size(); ++iess)
	{
		::SendMessage(hwndCombo, CB_ADDSTRING, 0, (LPARAM)m_vess[iess].m_strTitle.Chars());

		m_strFilter.Append(m_vess[iess].m_strTitle.Chars());
		m_strFilter.Append(_T("\0"), 1);
		m_strFilter.FormatAppend(_T("*.%s"), m_vess[iess].m_strOutputExt.Chars());
		m_strFilter.Append(_T("\0"), 1);
		if (m_vess[iess].m_vstrChain.Size())
			BuildXslChain(m_vess[iess]);
	}
}

/*----------------------------------------------------------------------------------------------
	Write a paragraph in WorldPad XML format, basing the paragraph named style on the field the
	underlying data is coming from, and the computing the indentation value from the given
	indentation level.

	@param pstrm Pointer to the IStream object for output.
	@param ptss Pointer to the TsString containing the paragraph contents.
	@param flid Id of field containing the formatted data, or zero if not applicable.
	@param nLevel (Indentation) level of the record (0 means top level, >= 1 means subrecord)
	@param stuField Name of the field (or group) this paragraph came from.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::WriteParagraph(IStream * pstrm, ITsString * ptss, int flid, int nLevel,
	StrUni & stuField)
{
	StrUni stuSty;
	if (m_hmflidstuParaStyle.Retrieve(flid, &stuSty))
	{
		WriteParagraph(pstrm, ptss, stuSty, nLevel * knIndentPerLevel, stuField, nLevel);
	}
	else
	{
		stuSty.Clear();		// Probably paranoid.
		WriteParagraph(pstrm, ptss, stuSty, nLevel * knIndentPerLevel, stuField, nLevel);
	}
}

/*----------------------------------------------------------------------------------------------
	Write a paragraph in WorldPad XML format, using the given paragraph style (if non-NULL) and
	indentation value.

	@param pstrm Pointer to the IStream object for output.
	@param ptss Pointer to the TsString containing the paragraph contents.
	@param stuStyle Paragraph's named style, or NULL.
	@param nIndent Value of paragraph indentation in millipoints.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::WriteParagraph(IStream * pstrm, ITsString * ptss, StrUni & stuStyle,
	int nIndent, StrUni & stuField, int nLevel)
{
	StrAnsi sta;
	ULONG cbOut;
	sta.Format("<StTxtPara>%n");
	if (stuStyle.Length() || nIndent > 0)
	{
		sta.FormatAppend("<StyleRules15>%n"
			"<Prop");
		if (stuStyle.Length())
		{
			sta.FormatAppend(" namedStyle=\"");
			CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
			WriteXmlUnicode(pstrm, stuStyle.Chars(), stuStyle.Length());
			sta.Format("\"");
		}
		if (nIndent > 0)
			sta.FormatAppend(" leadingIndent=\"%d\"", nIndent);
		sta.FormatAppend("/>%n"
			"</StyleRules15>%n");
	}
	sta.FormatAppend("<Contents16>%n");
	CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
	if (ptss)
	{
		ComBool fWriteObjData = false;
		if (m_qfcex)
			CheckHr(m_qfcex->IncludeObjectData(&fWriteObjData));
		ptss->WriteAsXml(pstrm, m_qwsf, 0, 0, fWriteObjData);
	}
	else
	{
		sta.Format("<Str></Str>%n");
		CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
	}
	sta.Format("</Contents16>%n"
		"</StTxtPara>%n");
	CheckHr(pstrm->Write(sta.Chars(), sta.Length(), &cbOut));
}

/*----------------------------------------------------------------------------------------------
	Get the default directory and filename for exporting.

	@param pszFile Buffer for storing the default filename
	@param cchFileMax Size of the filename buffer
	@param pszDir Buffer for storing the default directory
	@param cchDirMax Size of the directory buffer
----------------------------------------------------------------------------------------------*/
void AfExportDlg::GetDefaultFileAndDir(achar * pszFile, int cchFileMax, achar * pszDir,
	int cchDirMax)
{
	StrApp strPath;
	if (m_strPathname)
	{
		strPath = m_strPathname;
	}
	else
	{
		if (!m_fws.GetString(NULL, _T("LatestExportFile"), strPath))
		{
			// no stored information to draw upon:  make something up.
			StrApp strPrj(m_plpi->PrjName());
			int cch = strPrj.Length();
			if (cch >= cchFileMax)
				cch = cchFileMax - 1;
			memcpy(pszFile, strPrj.Chars(), cch * isizeof(achar));

			// Try to get the "My Documents" folder from the registry
			StrApp documentPath;
			DWORD cchDir;
			if (m_fws.GetString(
				_T("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Folders"),
				NULL, _T("Personal"), documentPath))
			{
				Assert((DWORD)documentPath.Length()+1 < (DWORD)cchDirMax);
				memcpy(pszDir, documentPath.Chars(), documentPath.Length() * isizeof(achar));
				cchDir = documentPath.Length();
			}
			else
			{
				// Couldn't find it. Default to the current directory
				cchDir = ::GetCurrentDirectory(cchDirMax, pszDir);
			}
			pszDir[cchDir] = '\\';
			Assert(cchDir+1 < (DWORD)cchDirMax);
			pszDir[cchDir+1] = 0;
			m_strPathname.Format(_T("%s%s.%s"),
				pszDir, pszFile, m_vess[m_iess].m_strOutputExt.Chars());
			return;
		}
		m_strPathname = strPath;
	}
	int cchPath = strPath.Length();
	int ich = strPath.ReverseFindCh('\\') + 1;
	int cch = cchPath - ich;
	if (cch >= cchFileMax)
		cch = cchFileMax - 1;
	memcpy(pszFile, strPath.Chars() + ich, cch * isizeof(achar));
	pszFile[cch] = 0;
	if (ich >= cchDirMax)
		ich = cchDirMax - 1;
	if (ich)
		memcpy(pszDir, strPath.Chars(), ich * isizeof(achar));
	pszDir[ich] = 0;
}

/*----------------------------------------------------------------------------------------------
	Fix the output filename to have the default extension for the selected type.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::AdjustFilename()
{
	if (!m_strPathname.Length())
		return;
	int ichFile = m_strPathname.ReverseFindCh('\\') + 1;
	int ichExt = m_strPathname.ReverseFindCh('.') + 1;
	StrApp & strExt = m_vess[m_iess].m_strOutputExt;
	bool fChange = false;
	if (ichExt > ichFile)
	{
		if (strExt != m_strPathname.Chars() + ichExt)
		{
			m_strPathname.Replace(ichExt, m_strPathname.Length(), strExt.Chars());
			fChange = true;
		}
	}
	else
	{
		m_strPathname.FormatAppend(_T(".%s"), strExt.Chars());
		fChange = true;
	}
	if (fChange)
		::SetDlgItemText(m_hwnd, kctidExportFilename, m_strPathname.Chars() + ichFile);
}

/*----------------------------------------------------------------------------------------------
	Load an XML file (either an intermediate output file or a "stylesheet") for use in applying
	an XSL transformation.

	@param pDOM Pointer to the COM object representing an XML document in DOM form.
	@param bstrFile Pathname of the XML file to load.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::LoadDOM(IXMLDOMDocument * pDOM, BSTR bstrFile)
{
	VARIANT vInput;
	VARIANT_BOOL fSuccess;
	V_VT(&vInput) = VT_BSTR;
	V_BSTR(&vInput) = bstrFile;
	CheckHr(pDOM->put_async(VARIANT_FALSE));
	CheckHr(pDOM->put_preserveWhiteSpace(VARIANT_TRUE));
	CheckHr(pDOM->load(vInput, &fSuccess));
	if (fSuccess == VARIANT_FALSE)
	{
		ComSmartPtr<IXMLDOMParseError> qParseError;
		CheckHr(pDOM->get_parseError(&qParseError));
		CheckHr(qParseError->get_reason(&m_sbstrError));
		CheckHr(qParseError->get_errorCode(&m_nErrorCode));
		CheckHr(qParseError->get_line(&m_nErrorLine));
		ThrowHr(E_FAIL);
	}
}

// The following definitions are cribbed from the latest version of <msxml2.h>.
// The #ifndefs should prevent any conflicts.
#ifndef __XSLTemplate40_FWD_DEFINED__
#define __XSLTemplate40_FWD_DEFINED__
typedef class XSLTemplate40 XSLTemplate40;
EXTERN_C const CLSID CLSID_XSLTemplate40;
class DECLSPEC_UUID("88d969c3-f192-11d4-a65f-0040963251e5") XSLTemplate40;
#endif  /* __XSLTemplate40_FWD_DEFINED__ */
#ifndef __DOMDocument40_FWD_DEFINED__
#define __DOMDocument40_FWD_DEFINED__
typedef class DOMDocument40 DOMDocument40;
EXTERN_C const CLSID CLSID_DOMDocument40;
class DECLSPEC_UUID("88d969c0-f192-11d4-a65f-0040963251e5") DOMDocument40;
#endif  /* __DOMDocument40_FWD_DEFINED__ */
#ifndef __FreeThreadedDOMDocument40_FWD_DEFINED__
#define __FreeThreadedDOMDocument40_FWD_DEFINED__
typedef class FreeThreadedDOMDocument40 FreeThreadedDOMDocument40;
EXTERN_C const CLSID CLSID_FreeThreadedDOMDocument40;
class DECLSPEC_UUID("88d969c1-f192-11d4-a65f-0040963251e5") FreeThreadedDOMDocument40;
#endif  /* __FreeThreadedDOMDocument40_FWD_DEFINED__ */

enum MsXslVersion {VERSION_NONE, VERSION_26, VERSION_30, VERSION_40};
struct MsXslInfo
{
	MsXslVersion m_version;
	const WCHAR * m_pwszVersion;
	const char * m_pszTemplateCLSID;
	const CLSID * m_clsidTemplate;
	const WCHAR * m_pwszTemplateProgID;
	const CLSID * m_clsidDocument;
	const WCHAR * m_pwszDocumentProgID;
	const CLSID * m_clsidFreeDocument;
	const WCHAR * m_pwszFreeDocumentProgID;
};

// These entries are in priority order.
static MsXslInfo g_rgMsXslInfo[] =
{
	{
		VERSION_40,
		L"4.0",
		"{88d969c3-f192-11d4-a65f-0040963251e5}",
		&__uuidof(XSLTemplate40),
		L"MSXML2.XSLTemplate.4.0",
		&__uuidof(DOMDocument40),
		L"MSXML2.DOMDocument.4.0",
		&__uuidof(FreeThreadedDOMDocument40),
		L"MSXML2.FreeThreadedDOMDocument.4.0",
	},
	{
		VERSION_30,
		L"3.0",
		"{f5078f36-c551-11d3-89b9-0000f81fe221}",
		&__uuidof(XSLTemplate30),
		L"MSXML2.XSLTemplate.3.0",
		&__uuidof(DOMDocument30),
		L"MSXML2.DOMDocument.3.0",
		&__uuidof(FreeThreadedDOMDocument30),
		L"MSXML2.FreeThreadedDOMDocument.3.0",
	},
	{
		VERSION_26,
		L"2.6",
		"{f5078f21-c551-11d3-89b9-0000f81fe221}",
		&__uuidof(XSLTemplate26),
		L"MSXML2.XSLTemplate.2.6",
		&__uuidof(DOMDocument26),
		L"MSXML2.DOMDocument.2.6",
		&__uuidof(FreeThreadedDOMDocument26),
		L"MSXML2.FreeThreadedDOMDocument.2.6",
	},
};
static const int g_cMsXslInfo = sizeof(g_rgMsXslInfo) / sizeof(MsXslInfo);

/*----------------------------------------------------------------------------------------------
	Apply one or more XSL transformations to produce the desired output file.  Use the newest
	XML services DLL from Microsoft that you can find, at least through version 4.0.  Multiple
	stages are possible, if an XSL stylesheet chains to another XSL stylesheets.  This chaining
	is limited only by disk space and user patience.  Cycles in the chain are prevented when
	the chain is stored, so it isn't checked here.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::Transform()
{
	m_stuErrorProcessStep.Clear();
	// Get information about the version of MSXML in use
	HKEY hkeyOuter = NULL;
	HKEY hkeyInner = NULL;
	LONG lRegRet;
	::RegOpenKeyExA(HKEY_CLASSES_ROOT, "CLSID", NULL, KEY_READ, &hkeyOuter);
	int iXsl;
	bool fFound = false;
	for (iXsl = 0; iXsl < g_cMsXslInfo; ++iXsl)
	{
		// Lookup XSLTemplate version dependent CLSID in registry
		lRegRet = ::RegOpenKeyExA(hkeyOuter, g_rgMsXslInfo[iXsl].m_pszTemplateCLSID, NULL,
			KEY_READ, &hkeyInner);
		if (lRegRet == NO_ERROR)
		{
			fFound = true;
			break;
		}
	}
	if (!fFound)
	{
		// Error message?
		ThrowHr(E_FAIL);
	}

	int cPass = m_vess[m_iess].m_vstrChain.Size();
	if (m_qprog)
	{
		if (cPass)
		{
			m_qprog->SetRange(0, 4 * cPass);
		}
		else
		{
			m_qprog->SetRange(0, 4);
			StrApp strMsg(kstidExportFormattingData);
			m_qprog->SetMessage(strMsg.Chars());
		}
		m_qprog->SetPos(0);
		m_qprog->SetStep(1);
	}
	StrUni stuInput(m_staTmpFile);
	StrUni stuStylesheet(m_vess[m_iess].m_strFile);
	StrUni stuOutput;
	if (cPass)
	{
		StrApp strFmt;
		if (m_qprog)
			strFmt.Load(kstidExportFormattingDataPass);
		stuOutput.Assign(m_staTmpFile.Chars());
		stuOutput.Append(L"1");
		if (!ProcessXsl(stuInput, stuStylesheet, stuOutput, iXsl))
			return;
		int ista;
		for (ista = 0; ista < cPass; ++ista)
		{
			if (m_qprog)
			{
				StrApp strMsg;
				strMsg.Format(strFmt.Chars(), ista + 1, cPass);
			}
			stuInput.Assign(stuOutput);
			stuStylesheet.Assign(m_vess[m_iess].m_vstrChain[ista].Chars());
			if (ista + 1 < cPass)
				stuOutput.Format(L"%S%d", m_staTmpFile, ista + 2);
			else
				stuOutput.Assign(m_strPathname.Chars());
			if (!ProcessXsl(stuInput, stuStylesheet, stuOutput, iXsl))
				return;
		}
	}
	else
	{
		stuOutput.Assign(m_strPathname.Chars());
		ProcessXsl(stuInput, stuStylesheet, stuOutput, iXsl);
	}
}

/*----------------------------------------------------------------------------------------------
	Apply the XSL stylesheet (transformation) to the input file, producing the output file.  Use
	the indicated version of the Microsoft XML services DLL.

	@param stuInput Pathname of the input file.
	@param stuStylesheet Pathname of the XSL transformation stylesheet.
	@param stuOutput Pathname of the output file (which may be an intermediate file).
	@param iXsl Index into g_rgMsXslInfo of the newest available Microsoft XML services DLL.

	@return True if successful; false if cancelled, or if an error in transforming.
----------------------------------------------------------------------------------------------*/
bool AfExportDlg::ProcessXsl(StrUni & stuInput, StrUni & stuStylesheet, StrUni & stuOutput,
	int iXsl)
{
	StrUni stuFmt(kstidExportProcessStepFmt);
	m_stuErrorProcessStep.Format(stuFmt.Chars(),
		stuStylesheet.Chars(), stuInput.Chars(), stuOutput.Chars());

	// Load the source document.
	ComSmartPtr<IXMLDOMDocument> qxdomInput;
	CheckHr(::CoCreateInstance(*g_rgMsXslInfo[iXsl].m_clsidDocument, NULL, CLSCTX_SERVER,
		__uuidof(IXMLDOMDocument), (void **)&qxdomInput));
	LoadDOM(qxdomInput, stuInput.Bstr());
	if (m_qprog)
		m_qprog->StepIt();
	if (m_fCancelled)
		return false;

	// Load the stylesheet document.
	ComSmartPtr<IXMLDOMDocument> qxdomStylesheet;
	CheckHr(::CoCreateInstance(*g_rgMsXslInfo[iXsl].m_clsidFreeDocument, NULL, CLSCTX_SERVER,
		__uuidof(IXMLDOMDocument), (void **)&qxdomStylesheet));
	LoadDOM(qxdomStylesheet, stuStylesheet.Bstr());
	if (m_qprog)
		m_qprog->StepIt();
	if (m_fCancelled)
		return false;

	// Compile the stylesheet document.
	ComSmartPtr<IXSLTemplate> qxslTemplate;
	CheckHr(::CoCreateInstance(*g_rgMsXslInfo[iXsl].m_clsidTemplate, NULL, CLSCTX_SERVER,
		__uuidof(IXSLTemplate), (void **)&qxslTemplate));
	CheckHr(qxslTemplate->putref_stylesheet(qxdomStylesheet));
	if (m_fCancelled)
		return false;

	// Execute the stylesheet
	ComSmartPtr<IXSLProcessor> qxslProcessor;
	CheckHr(qxslTemplate->createProcessor(&qxslProcessor));
	if (m_fCancelled)
		return false;

	// Set processor's input to input IXMLDOMDocument.
	VARIANT vInput;
	V_VT(&vInput) = VT_UNKNOWN;
	V_UNKNOWN(&vInput) = qxdomInput.Ptr();
	CheckHr(qxslProcessor->put_input(vInput));
	if (m_fCancelled)
		return false;

	// Set processor's output to file output IStream.
	VARIANT vOut;
	IStreamPtr qstrm;
	FileStream::Create(stuOutput.Chars(), kfstgmWrite | kfstgmCreate, &qstrm);
	V_VT(&vOut) = VT_UNKNOWN;
	V_UNKNOWN(&vOut) = qstrm.Ptr();
	CheckHr(qxslProcessor->put_output(vOut));
	if (m_qprog)
		m_qprog->StepIt();
	if (m_fCancelled)
		return false;

	// Get ready state (to help analyze processing failure).
	long nReadyState;
	CheckHr(qxslProcessor->get_readyState(&nReadyState));
	Assert(nReadyState == READYSTATE_LOADED || nReadyState == READYSTATE_COMPLETE);

	// Execute stylesheet
	VARIANT_BOOL fDone = VARIANT_FALSE;
	CheckHr(qxslProcessor->transform(&fDone));

	m_stuErrorProcessStep.Clear();
	if (m_qprog)
		m_qprog->StepIt();
	if (m_fCancelled)
		return false;
	else
		return true;
}

/*----------------------------------------------------------------------------------------------
	Build a vector of stylesheets to represent a chain of XSL transformations that are to be
	applied in order to create the output file.  Cycles are detected, and the chain broken at
	the point a cycle would have occurred.

	@param essComplex Reference to the internal style sheet object which contains the chain of
				XSL transformation stylesheets.
----------------------------------------------------------------------------------------------*/
void AfExportDlg::BuildXslChain(AfExportStyleSheet & essComplex)
{
	StrApp strFile = essComplex.m_strFile;
	StrApp strChain = essComplex.m_vstrChain[0];
	essComplex.m_vstrChain.Clear();
	do
	{
		AfExportStyleSheet ess;
		// Convert the filename to a full pathname if it isn't already.
		if (isascii(strChain.GetAt(0)) && strChain.GetAt(1) == ':')
		{
			ess.m_strFile.Assign(strChain);
		}
		else
		{
			int ich = strFile.ReverseFindCh('\\') + 1;
			ess.m_strFile.Assign(strFile.Chars(), ich);
			ess.m_strFile.Append(strChain.Chars());
		}
		strChain.Clear();
		ess.m_strTitle.Clear();
		ess.m_strOutputExt.Clear();
		ess.m_strDescription.Clear();
		ess.m_vstrChain.Clear();

		IStreamPtr qstrm;
		try
		{
			FileStream::Create(ess.m_strFile.Chars(), kfstgmRead, &qstrm);
		}
		catch (Throwable & thr)
		{
			if (thr.Message())
			{
				StrApp strMsg(thr.Message());
				StrApp strTitle(kstidExportErrorTitle);
				::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
			}
			return;
		}
		Assert(qstrm);

		// Check for a loop.
		for (int ista = 0; ista < essComplex.m_vstrChain.Size(); ++ista)
		{
			if (essComplex.m_vstrChain[ista] == ess.m_strFile)
			{
				// Error message?
				return;
			}
		}

		const XML_Char * pszWritingSystem = NULL;
		XML_Parser parser = XML_ParserCreate(pszWritingSystem);
		StrAnsi staFile(ess.m_strFile);
		if (!XML_SetBase(parser, staFile.Chars()))
		{
			XML_ParserFree(parser);
			continue;
		}
		try
		{
			XML_SetUserData(parser, &ess);
			XML_SetElementHandler(parser, HandleStartTag, HandleEndTag);
			essComplex.m_vstrChain.Push(ess.m_strFile);
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
				if (ess.m_vstrChain.Size())
				{
					strFile = ess.m_strFile;
					strChain = ess.m_vstrChain[0];
					break;
				}
				if (ess.m_strTitle.Length() && ess.m_strOutputExt.Length())
				{
					break;
				}
			}
		}
		catch(...)
		{
			// Ignore any errors in parsing.
		}
		XML_ParserFree(parser);
	} while (strChain.Length());
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)

// Handle explicit instantiation.
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
#include "Set_i.cpp"
