/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsOptDlg.cpp
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Implementation of the Tools Options Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

BEGIN_CMD_MAP(TlsOptDlgVwD)
	ON_CID_GEN(kcidTlsOptVwModAlways, &TlsOptDlgVwD::CmdModVis, NULL)
	ON_CID_GEN(kcidTlsOptVwModIfData, &TlsOptDlgVwD::CmdModVis, NULL)
	ON_CID_GEN(kcidTlsOptVwModNVis, &TlsOptDlgVwD::CmdModVis, NULL)
	ON_CID_GEN(kcidTlsOptVwDocIfData, &TlsOptDlgVwD::CmdModVis, NULL)
	ON_CID_GEN(kcidTlsOptVwDocNVis, &TlsOptDlgVwD::CmdModVis, NULL)
END_CMD_MAP_NIL()

BEGIN_CMD_MAP(TlsOptDlgVw)
	ON_CID_CHILD(kcidTlsOptAddBrView, &TlsOptDlgVw::CmdViewAddMenu,&TlsOptDlgVw::CmsViewAddMenu)
	ON_CID_CHILD(kcidTlsOptAddDEView, &TlsOptDlgVw::CmdViewAddMenu,&TlsOptDlgVw::CmsViewAddMenu)
	ON_CID_CHILD(kcidTlsOptAddDocView,&TlsOptDlgVw::CmdViewAddMenu,&TlsOptDlgVw::CmsViewAddMenu)
END_CMD_MAP_NIL()


//:>********************************************************************************************
//:>Browse View Dialog Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
	@param ptod pointer to the main TlsOptDlg.
----------------------------------------------------------------------------------------------*/
TlsOptDlgVwBr::TlsOptDlgVwBr(TlsOptDlg * ptod)
{
	AssertPtr(ptod);
	m_ptod = ptod;
	m_rid = kridTlsOptDlgVwBr;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_Views_tab.htm");
	m_himl = NULL;
	m_wsUser = ptod->MainWnd()->UserWs();
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param ptod himl image list for the dialog
	@param ptod pvuvs user views to be used in the dialog
	@param ptod psiwndClientDel delete list that will be updated to hold all deleted or
	modified views.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgVwBr::SetDialogValues(HIMAGELIST himl, UserViewSpecVec * pvuvs,
	Set<int> * psiwndClientDel)
{
	AssertPtr(pvuvs);
	AssertPtr(psiwndClientDel);
	m_himl = himl;
	m_pvuvs = pvuvs;
	m_psiwndClientDel = psiwndClientDel;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.

	@param wm windows message
	@param wp WPARAM
	@param lp LPARAM
	@param lnRet Value to be returned to the windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwBr::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ERASEBKGND)
	{
		// this is required to prevent the listview from not painting when selected then covered
		// by another window, then uncovered.  without this the listview will not repaint.
		RedrawWindow(m_hwndHideList, NULL , NULL, RDW_ERASE | RDW_FRAME | RDW_INTERNALPAINT |
			RDW_INVALIDATE);
		RedrawWindow(m_hwndDispList, NULL , NULL, RDW_ERASE | RDW_FRAME | RDW_INTERNALPAINT |
			RDW_INVALIDATE);
	}
return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwBr::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
//	AfApp::Papp()->AddCmdHandler(this,1);

	m_hwndDispList = ::GetDlgItem(m_hwnd, kcidTlsOptDlgVwDFlds);
	m_hwndHideList = ::GetDlgItem(m_hwnd, kcidTlsOptDlgVwHFlds);
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kcidTlsOptDlgVwDwn, kbtImage, m_himl,
		TlsOptDlgVw::kimagDownArrow);
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kcidTlsOptDlgVwUp, kbtImage, m_himl,
		TlsOptDlgVw::kimagUpArrow);

	// Subclass the Displayed fields listview controls as TssListViews
	TssListViewPtr qtlv;
	qtlv.Create();
	qtlv->SubclassListView(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwDFlds), m_wsUser);
	Assert(m_hwndDispList == qtlv->Hwnd());
	ListView_SetExtendedListViewStyle(m_hwndDispList,LVS_EX_FULLROWSELECT);

	// setup columns for Displayed fields listview
	FW_LVCOLUMN flvc;
	flvc.mask = LVCF_TEXT | LVCF_WIDTH;
	flvc.cx = 163;
	AfUtil::GetResourceTss(kstidTlsOptBrDspFld, m_wsUser, &flvc.qtss);
	Fw_ListView_InsertColumn(m_hwndDispList, 0, &flvc);

	// Subclass the Hidden fields listview controls as TssListViews
	qtlv.Create();
	qtlv->SubclassListView(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwHFlds), m_wsUser);
	Assert(m_hwndHideList == qtlv->Hwnd());
	ListView_SetExtendedListViewStyle(m_hwndHideList,LVS_EX_FULLROWSELECT);

	// setup columns for Hidden field listview
	flvc.mask = LVCF_TEXT | LVCF_WIDTH;
	flvc.cx = 120;
	AfUtil::GetResourceTss(kstidTlsOptBrHidFld, m_wsUser, &flvc.qtss);
	Fw_ListView_InsertColumn(m_hwndHideList, 0, &flvc);

	// Initialize the spin control.
	UDACCEL udAccel;
	udAccel.nSec = 0;
	udAccel.nInc = 1;
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwSLine), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwSLine), UDM_SETRANGE32, 1, 100);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handles a click on a spin control.

	@param pnmh Windows command that is being passed.
	@param lnRet Out Return value that will be returned to windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwBr::OnDeltaSpin(NMHDR * pnmh, long & lnRet)
{
	// If the edit box has changed and is out of synch with the spin control, this
	// will update the spin's position to correspond to the edit box.
	StrAppBuf strb;
	strb.SetLength(strb.kcchMaxStr);

	HWND hwndEdit = ::GetDlgItem(m_hwnd, kcidTlsOptDlgVwELine);

	// Get the text from the edit box and convert it to a number.
	int cch = ::SendMessage(hwndEdit, WM_GETTEXT, strb.kcchMaxStr, (LPARAM)strb.Chars());
	strb.SetLength(cch);
	m_nSpinValue = _tstoi(strb.Chars());

	UpdateEditBox();

	if (pnmh->code == UDN_DELTAPOS)
	{
		// If nValue is not already a whole increment of nDelta, then we only increment it
		// enough to make it a whole increment. If already a whole increment, then we go ahead
		// and increment it the entire amount. Thus if the increment is 1 and the original
		// value was 1.5, the first click on the arrow will bring it to 2.
		int nDelta = ((NMUPDOWN *)pnmh)->iDelta;
		int nPartialIncrement = m_nSpinValue % nDelta;
		if (nPartialIncrement && nDelta > 0)
			m_nSpinValue += (nDelta - nPartialIncrement);
		else if (nPartialIncrement && nDelta < 0)
			m_nSpinValue -= nPartialIncrement;
		else
			m_nSpinValue += nDelta;
	}

	(*m_pvuvs)[m_ivw]->m_nMaxLines = m_nSpinValue;

	// Add the window to the delete list which will update it when OK is pressed.
	int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
	if (iwndClient >= 0)
		m_psiwndClientDel->Insert(iwndClient);

	// Update dialog controls.
	UpdateEditBox();

	lnRet = 0;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Update the value in the Edit Box.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgVwBr::UpdateEditBox()
{
	StrAppBuf strb;
//	strb.SetLength(strb.kcchMaxStr);

	// Don't exceed the minimum or maximum values in the spin control.
	m_nSpinValue = NBound(m_nSpinValue, 1, 100);

	strb.Format(_T("%d"), m_nSpinValue);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwELine), WM_SETTEXT, 0,
		(LPARAM)strb.Chars());
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
bool TlsOptDlgVwBr::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	switch (pnmh->code)
	{
	case BN_CLICKED:
		{
			// Add the window to the delete list which will update it when OK is pressed.
			int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
			if (iwndClient >= 0)
				m_psiwndClientDel->Insert(iwndClient);

			switch (pnmh->idFrom)
			{
			case kcidTlsOptDlgVwIgn:
				if (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgVwIgn) == BST_CHECKED)
					(*m_pvuvs)[m_ivw]->m_fIgnorHier = true;
				else
					(*m_pvuvs)[m_ivw]->m_fIgnorHier = false;
				return false;

			case kcidTlsOptDlgVwAll:
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwELine), false);
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwSLine), false);
				if (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgVwAll) == BST_CHECKED)
				{
					(*m_pvuvs)[m_ivw]->m_nMaxLines = 0;
					CheckDlgButton(m_hwnd, kcidTlsOptDlgVwOnly, BST_UNCHECKED);
				}
				return false;

			case kcidTlsOptDlgVwOnly:
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwELine), true);
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwSLine), true);
				if (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgVwOnly) == BST_CHECKED)
				{
					(*m_pvuvs)[m_ivw]->m_nMaxLines = m_nSpinValue;
					CheckDlgButton(m_hwnd, kcidTlsOptDlgVwAll, BST_UNCHECKED);
				}
				return false;

			case kcidTlsOptDlgVwShow:
				{
					// Get the selected hidden item then delete it.
					int iItem = ListView_GetNextItem(m_hwndHideList, -1, LVNI_SELECTED);
					if (iItem < 0)
					{
						// Do nothing if there is no Field selected.
						return true;
					}

					FW_LVITEM fwlvi;
					fwlvi.mask = LVIF_TEXT | LVIF_PARAM;
					fwlvi.iItem = iItem;
					fwlvi.iSubItem = 0;
					Fw_ListView_GetItem(m_hwndHideList, &fwlvi);
					// Retrieve information encoded in lParam.
					int ibsp = fwlvi.lParam % 1000;
					int clsid = fwlvi.lParam / 1000;

					// Determine the view type that is used for the master list (normally DE).
					UserViewType vwt = kvwtDE;
					Vector<TlsView> & vtv = m_ptod->ViewVec();
					for (int itv = 0; itv < vtv.Size(); ++itv)
					{
						if (vtv[itv].m_fMaster)
						{
							vwt = vtv[itv].m_vwt;
							break;
						}
					}

					// Find the RecordSpec in the first view with the specified view type
					// that matches the stored clsid.
					RecordSpecPtr qrsp;
					for (int ivuvs = 0; ivuvs < m_pvuvs->Size(); ++ivuvs)
					{
						UserViewSpecPtr quvs = (*m_pvuvs)[ivuvs];
						AssertPtr(quvs);
						if (quvs->m_vwt == vwt)
						{
							// Go through the RecordSpecs, checking top-level views.
							ClevRspMap::iterator ithmclevrspLim = quvs->m_hmclevrsp.End();
							for (ClevRspMap::iterator it = quvs->m_hmclevrsp.Begin();
								it != ithmclevrspLim; ++it)
							{
								ClsLevel clev = it.GetKey();
								qrsp = it.GetValue();
								AssertPtr(qrsp);
								if (clev.m_nLevel == 0 && clev.m_clsid == clsid)
									break;
							}
							break;
						}
					}
					AssertPtr(qrsp);
					// Make a copy of the source BlockSpec so that we can clear m_hvo.
					// This is necessary so that the save method will actually save
					// this new BlockSpec to the database.
					BlockSpecPtr qbsp;
					qrsp->m_vqbsp[ibsp]->NewCopy(&qbsp);
					qbsp->m_hvo = 0;

					// We add it to the dummy RecordSpec in the Browse view at m_ivw.
					m_ptod->GetBlockVec(*m_pvuvs, m_ivw, 0, &qrsp);
					BlockVec & vpbsp = qrsp->m_vqbsp;
					qrsp->m_fDirty = true;

					// copy the blockspec from one vector to the other
					int cItems = ListView_GetItemCount(m_hwndDispList);
					int iItemD = ListView_GetNextItem(m_hwndDispList, -1, LVNI_SELECTED);
					if (iItemD < 0)
						iItemD = 0;
					vpbsp.Insert(iItemD, qbsp);

					// Select another item in the hidden list
					if (cItems > 0)
					{
						if (iItem == cItems)
							--iItem;
					}

					UpdateFldList(iItem);
					UpdateDisp(iItemD);
					return true;
				}

			case kcidTlsOptDlgVwHide:
				{
					// Get the selected displayed item
					int iItem = ListView_GetNextItem(m_hwndDispList, -1, LVNI_SELECTED);
					if (iItem < 0)
						return true; // Do nothing if there is no Field selected.

					FW_LVITEM fwlvi;
					fwlvi.mask = LVIF_TEXT | LVIF_PARAM;
					fwlvi.iItem = iItem;
					fwlvi.iSubItem = 0;
					Fw_ListView_GetItem(m_hwndDispList, &fwlvi);
					// Retrieve information encoded in lParam.
					int ibsp = fwlvi.lParam % 1000;
					//int clsid = fwlvi.lParam / 1000;

					// Delete the block spec from the browse view
					RecordSpecPtr qrsp;
					// Get the dummy RecordSpec in the Browse view at m_ivw.
					m_ptod->GetBlockVec(*m_pvuvs, m_ivw, 0, &qrsp);
					BlockVec & vpbsp = qrsp->m_vqbsp;
					vpbsp.Delete(ibsp);
					qrsp->m_fDirty = true;

					// Get another item in the displayed list to select then update fields.
					int cItems = ListView_GetItemCount(m_hwndDispList);
					if (cItems > 0)
					{
						if (iItem == cItems -1)
							--iItem;
					}
					else
						iItem = -1;
					int iHItem = ListView_GetNextItem(m_hwndHideList, -1, LVNI_SELECTED);
					UpdateFldList(iHItem);
					UpdateDisp(iItem);
					return true;
				}

			case kcidTlsOptDlgVwMod:
				ModFldSet();
				return true;

			case kcidTlsOptDlgVwUp:
				MoveFld(TlsOptDlgVw::kMoveUp);
				return true;

			case kcidTlsOptDlgVwDwn:
				MoveFld(TlsOptDlgVw::kMoveDwn);
				return true;
			}
			break;
		}
	case UDN_DELTAPOS: // Spin control is activated.
		return OnDeltaSpin(pnmh, lnRet);

	case EN_SETFOCUS: // Edit control modified.
		{
			if (pnmh->idFrom == kcidTlsOptDlgVwELine)
				::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwELine), EM_SETSEL, (WPARAM)0,
					(LPARAM)-1);
			return true;
		}
	case EN_KILLFOCUS: // Edit control modified.
		if (pnmh->idFrom == kcidTlsOptDlgVwELine)
			return OnDeltaSpin(pnmh, lnRet);
		return false;
	case LVN_ITEMCHANGED:
		if (pnmh->idFrom == kcidTlsOptDlgVwHFlds)
		{
			bool fEnable = -1 < ListView_GetNextItem(m_hwndHideList, -1, LVNI_SELECTED);
			::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwShow), fEnable);
		}
		else if (pnmh->idFrom == kcidTlsOptDlgVwDFlds)
		{
			int iSel = ListView_GetNextItem(m_hwndDispList, -1, LVNI_SELECTED);
			if (iSel == -1)
			{
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwMod), false);
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwHide), false);
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwUp), false);
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwDwn), false);
			}
			else
			{
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwMod), true);
				int cList = ListView_GetItemCount(m_hwndDispList);
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwHide), cList > 1);
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwUp), iSel > 0);
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwDwn), iSel < cList - 1);
			}
		}
		break;
	case NM_DBLCLK:
		if (pnmh->idFrom == kcidTlsOptDlgVwDFlds)
			if (-1 < ListView_GetNextItem(m_hwndDispList, -1, LVNI_SELECTED))
				ModFldSet();
		break;
	//	Default is do nothing.
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Bring up the Modify Field Settings dialog.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwBr::ModFldSet()
{
	int iItem = ListView_GetNextItem(m_hwndDispList, -1, LVNI_SELECTED);
	// Get the lParam, because it is the index to the fields of the block spec.
	FW_LVITEM fwlvi;
	fwlvi.mask = LVIF_PARAM;
	fwlvi.iItem = iItem;
	fwlvi.iSubItem = 0;
	Fw_ListView_GetItem(m_hwndDispList, &fwlvi);
	RecordSpecPtr qrsp;
	m_ptod->GetBlockVec(*m_pvuvs, m_ivw, m_ptod->CurObjVecIndex(), &qrsp);
	BlockVec & vpbsp = qrsp->m_vqbsp;
	FldSpecPtr qfsp;
	StrUni stuSty;

	int ilParam = fwlvi.lParam;

	FldVis vis;
	FldType fldtyp;
		iItem = ilParam;
		vis = vpbsp[ilParam]->m_eVisibility;
		qfsp = vpbsp[ilParam];
		fldtyp = qfsp->m_ft;
		stuSty = qfsp->m_stuSty;

	StrUni stuDefParaChars = L""; // Dummy style name for "no character style at all"
	stuDefParaChars.Load(kstidDefParaChars);

	switch (fldtyp)
	{
	case kftString:
	case kftMsa:
	case kftMta:
	case kftEnum:
	case kftUnicode:
	case kftTtp:
	case kftLimEmbedLabel:
	case kftStText:
	case kftGroup:
	case kftDateRO:
	case kftDate:
	case kftGenDate:
	case kftTitleGroup:
	case kftDummy:
	case kftObjRefAtomic:
	case kftObjRefSeq:
	case kftInteger:
		{
			// It is a text field type
			ModBrFldSetDlgTPtr qmfsdt;
			qmfsdt.Create();
			qmfsdt->SetDialogValues((UserViewType)(*m_pvuvs)[m_ivw]->m_vwt, stuSty);
			if (qmfsdt->DoModal(m_hwnd) == kctidOk)
			{
				qmfsdt->GetDialogValues(stuSty);
				if (stuSty.Equals(stuDefParaChars))
					qfsp->m_stuSty.Clear();
				else
					qfsp->m_stuSty = stuSty;
				// Add the window to the delete list which will update it when OK is pressed.
				int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
				if (iwndClient >= 0)
					m_psiwndClientDel->Insert(iwndClient);
				qrsp->m_fDirty = true;
			}
			break;
		}
	case kftRefAtomic:
	case kftRefCombo:
	case kftRefSeq:
		{
			// it is a Choices List field type
			PossNameType pnt = qfsp->m_pnt;
			bool fHier = qfsp->m_fHier;
			bool fVert = qfsp->m_fVert;
			ModBrFldSetDlgCLPtr qmfsdcl;
			qmfsdcl.Create();
			qmfsdcl->SetDialogValues((UserViewType)(*m_pvuvs)[m_ivw]->m_vwt, stuSty, pnt,
				fHier, fVert);
			if (qmfsdcl->DoModal(m_hwnd) == kctidOk)
			{
				qmfsdcl->GetDialogValues(stuSty, &pnt, &fHier, &fVert);
				if (stuSty.Equals(stuDefParaChars))
					qfsp->m_stuSty.Clear();
				else
					qfsp->m_stuSty = stuSty;
				qfsp->m_pnt = pnt;
				qfsp->m_fHier = fHier;
				qfsp->m_fVert = fVert;

				// Add the window to the delete list which will update it when OK is pressed.
				int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
				if (iwndClient >= 0)
					m_psiwndClientDel->Insert(iwndClient);
				qrsp->m_fDirty = true;
			}
			break;
		}
	case kftExpandable:
		{
			// it is a expandable field type
			PossNameType pnt = qfsp->m_pnt;
			bool fHier = qfsp->m_fHier;
			bool fVert = qfsp->m_fVert;
			bool fExpand = qfsp->m_fExpand;
			ModBrFldSetDlgExpPtr qmbfsde;
			qmbfsde.Create();
			qmbfsde->SetDialogValues((UserViewType)(*m_pvuvs)[m_ivw]->m_vwt, stuSty, pnt,
				fHier, fVert, fExpand,(*m_pvuvs)[m_ivw]->m_vwt ==	kvwtDoc);
			if (qmbfsde->DoModal(m_hwnd) == kctidOk)
			{
				qmbfsde->GetDialogValues(stuSty, &pnt, &fHier, &fVert, &fExpand);
				if (stuSty.Equals(stuDefParaChars))
					qfsp->m_stuSty.Clear();
				else
					qfsp->m_stuSty = stuSty;
				qfsp->m_pnt = pnt;
				qfsp->m_fHier = fHier;
				qfsp->m_fVert = fVert;
				qfsp->m_fExpand = fExpand;

				// Add the window to the delete list which will update it when OK is pressed.
				int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
				if (iwndClient >= 0)
					m_psiwndClientDel->Insert(iwndClient);
				qrsp->m_fDirty = true;
			}
			break;
		}
	case kftSubItems:
		{
/*			// it is a Hierarchical field type
			OutlineNumSty ons = qfsp->m_ons;
			bool fExpand = qfsp->m_fExpand;
			ModFldSetDlgHiPtr qmfsdh;
			qmfsdh.Create();
			qmfsdh->SetDialogValues(vis, (UserViewType)(*m_pvuvs)[m_ivw]->m_vwt, stuSty, ons,
				fExpand);
			if (qmfsdh->DoModal(m_hwnd) == kctidOk)
			{
				qmfsdh->GetDialogValues(&vis, stuSty, &ons, &fExpand);
				qfsp->m_eVisibility = vis;
				if (stuSty.Equals(stuDefParaChars))
					qfsp->m_stuSty.Clear();
				else
					qfsp->m_stuSty = stuSty;
				qfsp->m_ons = ons;
				qfsp->m_fExpand = fExpand;
				fwlvi.mask = LVIF_TEXT;
				fwlvi.iSubItem = 1;
				Fw_ListView_GetItem(m_hwndFldList, &fwlvi);
				fwlvi.qtss = m_rgqFldVis[(int) vis];
				Fw_ListView_SetItem(m_hwndFldList, &fwlvi);

				// Add the window to the delete list which will update it when OK is pressed.
				int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
				if (iwndClient >= 0)
					m_psiwndClientDel->Insert(iwndClient);
				qrsp->m_fDirty = true;
				UpdateFldList(fwlvi.iItem);
			}
*/			break;
		}
	default:
		Assert(false);
		break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Updates the Line number and Ignore Hierarchy controls for the dialog.  This is called any
	time a new view is	selected.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgVwBr::UpdateLineCtrls()
{
	m_nSpinValue = (*m_pvuvs)[m_ivw]->m_nMaxLines;

	if (m_nSpinValue == 0)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwELine), false);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwSLine), false);
		CheckDlgButton(m_hwnd, kcidTlsOptDlgVwAll, BST_CHECKED);
		CheckDlgButton(m_hwnd, kcidTlsOptDlgVwOnly, BST_UNCHECKED);
	}
	else
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwELine), true);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwSLine), true);
		CheckDlgButton(m_hwnd, kcidTlsOptDlgVwOnly, BST_CHECKED);
		CheckDlgButton(m_hwnd, kcidTlsOptDlgVwAll, BST_UNCHECKED);
	}

	if ((*m_pvuvs)[m_ivw]->m_fIgnorHier)
		CheckDlgButton(m_hwnd, kcidTlsOptDlgVwIgn, BST_CHECKED);
	else
		CheckDlgButton(m_hwnd, kcidTlsOptDlgVwIgn, BST_UNCHECKED);
	UpdateEditBox();
}

/*----------------------------------------------------------------------------------------------
	Update Field listView Control.

	@param iSel Index to the item to be reselected after updating. (-1 for no selection)
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwBr::UpdateFldList(int iSel)
{
	// Determine the view type that is used for the master list (normally DE).
	UserViewType vwt = kvwtDE;
	Vector<TlsView> & vtv = m_ptod->ViewVec();
	for (int itv = 0; itv < vtv.Size(); ++itv)
	{
		if (vtv[itv].m_fMaster)
		{
			vwt = vtv[itv].m_vwt;
			break;
		}
	}

	FW_LVITEM fwlvi;
	FW_LVFINDINFO flvfi;
	flvfi.flags = LVFI_STRING;

	// Now find the first view of the correct type and build a list of block specs from
	// that view, merging fields from all top-level classes.
	::SendMessage(m_hwndHideList, LVM_DELETEALLITEMS, 0, 0);
	for (int ivuvs = 0; ivuvs < m_pvuvs->Size(); ++ivuvs)
	{
		UserViewSpecPtr quvs = (*m_pvuvs)[ivuvs];
		AssertPtr(quvs);
		if (quvs->m_vwt == vwt)
		{
			// Go through the RecordSpecs, checking top-level views.
			ClevRspMap::iterator ithmclevrspLim = quvs->m_hmclevrsp.End();
			for (ClevRspMap::iterator it = quvs->m_hmclevrsp.Begin();
				it != ithmclevrspLim; ++it)
			{
				ClsLevel clev = it.GetKey();
				RecordSpecPtr qrsp = it.GetValue();
				AssertPtr(qrsp);
				if (clev.m_clsid == kclidRnRoledPartic)
					continue;
				if (clev.m_nLevel > 0)
					continue;
				int cbsp = qrsp->m_vqbsp.Size();
				// We store the clsid and index in the lparam.
				Assert(cbsp < 1000);
				int clsidMul = clev.m_clsid * 1000;
				int ilvi = 0;
				ITsStringPtr qtssLabel;
				AfUtil::GetResourceTss(kstidTlsOptSubentries, m_wsUser, &qtssLabel);
				for (int ibsp = 0; ibsp < cbsp; ++ibsp)
				{
					ComBool fEqual;
					qtssLabel->Equals(qrsp->m_vqbsp[ibsp]->m_qtssLabel, &fEqual);
					if (fEqual)
						continue;
					// See if we already have the item in the Hide listview.
					flvfi.qtss = qrsp->m_vqbsp[ibsp]->m_qtssLabel;
					int ilviT = Fw_ListView_FindItem(m_hwndHideList, -1, &flvfi);
					if (ilviT >= 0)
						continue; // We already have a BlockSpec with that name.
					// It wasn't found, so add a new item.
					fwlvi.qtss = qrsp->m_vqbsp[ibsp]->m_qtssLabel;
					fwlvi.mask = LVIF_TEXT | LVIF_PARAM;
					fwlvi.iItem = ilvi;
					fwlvi.iSubItem = 0;
					// Add the clsid to the ibsp and store in the LPARAM.
					fwlvi.lParam = (LPARAM)(ibsp + clsidMul);
					Fw_ListView_InsertItem(m_hwndHideList, &fwlvi);
					ilvi++;
				}
			}
			break;
		}
	}

	if (iSel > -1)
	{
		fwlvi.mask = LVIF_STATE;
		fwlvi.iItem = iSel;
		fwlvi.iSubItem = 0;
		fwlvi.state = LVIS_SELECTED | LVIS_FOCUSED;
		fwlvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
		Fw_ListView_SetItem(m_hwndHideList, &fwlvi);
		ListView_EnsureVisible(m_hwndHideList, iSel, false);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Update the displayed list view control.

	@param iSel Index to the item to be reselected after updating. (-1 for no selection)
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwBr::UpdateDisp(int iSel)
{
	FW_LVITEM fwlvi;
	int ifld;
	int iLvItem;
	RecordSpecPtr qrsp;
	::SendMessage(m_hwndDispList, LVM_DELETEALLITEMS, 0, 0);

	m_ptod->GetBlockVec(*m_pvuvs, m_ivw, 0, &qrsp);
	BlockVec & vpbsp = qrsp->m_vqbsp;

	ITsStringPtr qtssAbc;
	iLvItem = 0;
	for (ifld = 0; ifld < vpbsp.Size(); ++ifld)
	{
		// add to the Display listview
		fwlvi.mask = LVIF_TEXT | LVIF_PARAM;
		fwlvi.iItem = iLvItem;
		fwlvi.iSubItem = 0;
		fwlvi.qtss = vpbsp[ifld]->m_qtssLabel;
		fwlvi.lParam = (LPARAM) ifld;
		Fw_ListView_InsertItem(m_hwndDispList, &fwlvi);
		iLvItem++;

		// find the item in the Hide listview
		int iStart = -1;
		FW_LVFINDINFO flvfi;
		flvfi.flags = LVFI_STRING;
		flvfi.qtss = vpbsp[ifld]->m_qtssLabel;
		int iItem = Fw_ListView_FindItem(m_hwndHideList, iStart, &flvfi);

		if (iItem > -1)
		{
			// delete the item from the Hide listview if it is in there
			ListView_DeleteItem(m_hwndHideList, iItem);
		}

		// if the selected item was deleted then select another one
		iItem = ListView_GetNextItem(m_hwndHideList, -1, LVNI_SELECTED);
		if (iItem < 0)
		{
			// Select the first item
			fwlvi.mask = LVIF_STATE;
			fwlvi.iItem = 0;
			fwlvi.iSubItem = 0;
			fwlvi.state = LVIS_SELECTED | LVIS_FOCUSED;
			fwlvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
			Fw_ListView_SetItem(m_hwndHideList, &fwlvi);
			ListView_EnsureVisible(m_hwndHideList, iSel, false);
		}
	}

	if (iSel > -1)
	{
		fwlvi.mask = LVIF_STATE;
		fwlvi.iItem = iSel;
		fwlvi.iSubItem = 0;
		fwlvi.state = LVIS_SELECTED | LVIS_FOCUSED;
		fwlvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
		Fw_ListView_SetItem(m_hwndDispList, &fwlvi);
		ListView_EnsureVisible(m_hwndDispList, iSel, false);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Move field up or down in the displayed list view control.

	@param updwn move the field up (kMoveUp) or down (kMoveDwn)
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwBr::MoveFld(int updwn)
{
	ITsStringPtr qtssSel;
	int iItem = ListView_GetNextItem(m_hwndDispList, -1, LVNI_SELECTED);
	if (iItem < 0)
	{
		// Do nothing if there is no Field selected.
		return true;
	}

	// Add the window to the delete list which will update it when OK is pressed.
	int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
	if (iwndClient >= 0)
		m_psiwndClientDel->Insert(iwndClient);

	//get selected field, save name so we can select it later, after the move.
	FW_LVITEM fwlvi;
	fwlvi.mask = LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM;
	fwlvi.iItem = iItem;
	fwlvi.iSubItem = 0;
	Fw_ListView_GetItem(m_hwndDispList, &fwlvi);
	int iFld = fwlvi.lParam;
	qtssSel = fwlvi.qtss;

	RecordSpecPtr qrsp;
	m_ptod->GetBlockVec(*m_pvuvs, m_ivw, 0, &qrsp);
	BlockVec & vpbsp = qrsp->m_vqbsp;

	BlockSpecPtr qbsp = vpbsp[iFld];
	if (updwn == TlsOptDlgVw::kMoveUp)
	{
		// Move field up one positon
		if (iFld < 1)
		{
			::SetFocus(m_hwndDispList);
			return true;
		}
		vpbsp.Delete(iFld);
		vpbsp.Insert(iFld - 1, qbsp);
	}
	else
	{
		// Move field down one positon
		if (iFld == vpbsp.Size() - 1)
		{
			::SetFocus(m_hwndDispList);
			return true;
		}
		vpbsp.Delete(iFld);
		vpbsp.Insert(iFld + 1, qbsp);
	}

	qrsp->m_fDirty = true;
	UpdateDisp(-1);

	// Reselect the item now
	int iStart = -1;
	FW_LVFINDINFO flvfi;
	flvfi.flags = LVFI_STRING;
	flvfi.qtss = qtssSel;
	iItem = Fw_ListView_FindItem(m_hwndDispList, iStart, &flvfi);

	fwlvi.mask = LVIF_STATE;
	fwlvi.iItem = iItem;
	fwlvi.iSubItem = 0;
	fwlvi.state = LVIS_SELECTED | LVIS_FOCUSED;
	fwlvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
	Fw_ListView_SetItem(m_hwndDispList, &fwlvi);
	ListView_EnsureVisible(m_hwndDispList, iItem, false);
	::SetFocus(m_hwndDispList);
	return true;
}


//:>********************************************************************************************
//:>Data Entry or Document View Dialog Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
	@param ptod pointer to the main TlsOptDlg.
----------------------------------------------------------------------------------------------*/
TlsOptDlgVwD::TlsOptDlgVwD(TlsOptDlg * ptod)
{
	AssertPtr(ptod);
	m_ptod = ptod;
	m_rid = kridTlsOptDlgVwD;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_Views_tab.htm");
	m_himl = NULL;
	m_wsUser = ptod->MainWnd()->UserWs();
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param ptod himl image list for the dialog
	@param ptod pvuvs user views to be used in the dialog
	@param ptod psiwndClientDel delete list that will be updated to hold all deleted or
	modified views.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgVwD::SetDialogValues(HIMAGELIST himl, UserViewSpecVec * pvuvs,
	Set<int> * psiwndClientDel)
{
	AssertPtr(pvuvs);
	AssertPtr(psiwndClientDel);
	m_himl = himl;
	m_pvuvs = pvuvs;
	m_psiwndClientDel = psiwndClientDel;
}


/*----------------------------------------------------------------------------------------------
	Need to clean up.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgVwD::OnReleasePtr()
{
	SuperClass::OnReleasePtr();
	AfApp::Papp()->RemoveCmdHandler(this, 1);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.

	@param wm windows message
	@param wp WPARAM
	@param lp LPARAM
	@param lnRet Value to be returned to the windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwD::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ERASEBKGND)
		// this is required to prevent the listview from not painting when selected then covered
		// by another window, then uncovered.  without this the listview will not repaint.
		RedrawWindow(m_hwndFldList, NULL , NULL, RDW_ERASE | RDW_FRAME | RDW_INTERNALPAINT |
			RDW_INVALIDATE);

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwD::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AfApp::Papp()->AddCmdHandler(this,1);

	m_hwndFldList = ::GetDlgItem(m_hwnd, kcidTlsOptDlgVwFlds);

	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kcidTlsOptDlgVwDwn, kbtImage, m_himl,
		TlsOptDlgVw::kimagDownArrow);
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kcidTlsOptDlgVwUp, kbtImage, m_himl,
		TlsOptDlgVw::kimagUpArrow);

	// Subclass the fields listview control as a TssListView
	TssListViewPtr qtlv;
	qtlv.Create();
	qtlv->SubclassListView(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwFlds), m_wsUser);
	Assert(m_hwndFldList == qtlv->Hwnd());
	ListView_SetExtendedListViewStyle(m_hwndFldList,LVS_EX_FULLROWSELECT);

	// setup columns for fields listview
	FW_LVCOLUMN flvc;
	flvc.mask = LVCF_TEXT | LVCF_WIDTH;

	flvc.cx = 100;
	AfUtil::GetResourceTss(kstidTlsOptFld, m_wsUser, &flvc.qtss);
	Fw_ListView_InsertColumn(m_hwndFldList, 0, &flvc);

	flvc.cx = 90;
	AfUtil::GetResourceTss(kstidTlsOptVis, m_wsUser, &flvc.qtss);
	Fw_ListView_InsertColumn(m_hwndFldList, 1, &flvc);

//	flvc.cx = 50;
	flvc.cx = 140;
	AfUtil::GetResourceTss(kstidTlsOptVwStyle, m_wsUser, &flvc.qtss);
	Fw_ListView_InsertColumn(m_hwndFldList, 2, &flvc);

//	flvc.cx = 90;
//	AfUtil::GetResourceTss(kstidTlsOptVwDisp, m_wsUser, &flvc.qtss);
//	Fw_ListView_InsertColumn(m_hwndFldList, 3, &flvc);

	// Read in and setup the visiblity text
	AfUtil::GetResourceTss(kstidTlsOptAl, m_wsUser, &m_rgqFldVis[kFTVisAlways]);
	AfUtil::GetResourceTss(kstidTlsOptIfDP, m_wsUser, &m_rgqFldVis[kFTVisIfData]);
	AfUtil::GetResourceTss(kstidTlsOptNV, m_wsUser, &m_rgqFldVis[kFTVisNever]);

	// Make the indent string used for subitems in doc views
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->MakeStringRgch(L"    ", 4, m_wsUser, &m_qtssIndent));

	// Load "Fields in:" Combo Box
	Vector<TlsObject> & vto = m_ptod->ObjectVec();
	Assert((uint)m_ptod->CurObjVecIndex() < (uint)vto.Size());
	HWND hwndFIn = ::GetDlgItem(m_hwnd, kcidTlsOptDlgVwFIn);
	for (int i = 0; i < vto.Size(); ++i)
	{
		::SendMessage(hwndFIn, CB_ADDSTRING, 0, (LPARAM)vto[i].m_strName.Chars());
	}

	// Only show the Fields In combo if we should.
	if (!m_ptod->GetfShowFInCbo())
	{
		::ShowWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwFInCap), SW_HIDE);
		::ShowWindow(hwndFIn, SW_HIDE);
		Rect rcCbo;
		::GetWindowRect(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwFInCap), &rcCbo);
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rcCbo, 2);

		Rect rcLst;
		::GetWindowRect(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwFlds), &rcLst);
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rcLst, 2);

		::MoveWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwFlds), rcLst.left, rcCbo.top,
			rcLst.Width(), rcLst.Height() + (rcLst.top - rcCbo.top), true);
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
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
bool TlsOptDlgVwD::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	Rect rc;
	Rect rc1;
	ITsStringPtr qtss;
	FW_LVITEM fwlvi;

	switch (pnmh->code)
	{
	case CBN_SELCHANGE: // Combo box item changed.
		if (pnmh->idFrom == kcidTlsOptDlgVwFIn)
			return OnComboChange(pnmh, lnRet);
		break;

	case BN_CLICKED:
		switch (pnmh->idFrom)
		{
		case kcidTlsOptDlgVwMod:
			ModFldSet();
			return true;

		case kcidTlsOptDlgVwUp:
			MoveFld(TlsOptDlgVw::kMoveUp);
			return true;

		case kcidTlsOptDlgVwDwn:
			MoveFld(TlsOptDlgVw::kMoveDwn);
			return true;
		}
		break;

	case NM_RCLICK:
		{
			if (pnmh->idFrom == kcidTlsOptDlgVwFlds)
			{
				LVHITTESTINFO hti;
				::GetCursorPos(&hti.pt);
				::ScreenToClient(m_hwndFldList, &hti.pt);
				ListView_SubItemHitTest(m_hwndFldList, &hti);
				if (hti.iItem != -1 && hti.iSubItem == 1)
				{
					ListView_GetSubItemRect(m_hwndFldList, hti.iItem, hti.iSubItem, LVIR_BOUNDS,
						&rc);
					::ClientToScreen(m_hwndFldList,(POINT *)&rc);
					int rid;
					if ((*m_pvuvs)[m_ivw]->m_vwt == kvwtDoc)
					{
						// If the click was on a group title, ignore.
						FldSpecPtr qfsp;
						RecordSpecPtr qrsp;
						FindFieldSpec(&qfsp, &qrsp);
						if (qfsp->m_ft == kftGroup || qfsp->m_ft == kftGroupOnePerLine)
							return true;

						// We now want to allow Always as an option for the Document
						// view, so use the regular menu.
						//rid = kcidTlsOptVwVisDocMnu;
						rid = kcidTlsOptVwVisMnu;
					}
					else
						rid = kcidTlsOptVwVisMnu;
					HMENU hmenu = ::LoadMenu(ModuleEntry::GetModuleHandle(),
						MAKEINTRESOURCE(rid));
					HMENU hmenuPopup = ::GetSubMenu(hmenu, 0);
					::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left,
						rc.top, 0, m_hwnd, NULL);
					::DestroyMenu(hmenu);
				}
			}
			break;
		}
	case NM_DBLCLK:
		if (pnmh->idFrom == kcidTlsOptDlgVwFlds)
			ModFldSet();
		break;

	//	Default is do nothing.
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Return the FieldSpec for the current item.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgVwD::FindFieldSpec(FldSpec ** ppfsp, RecordSpec ** pprsp)
{
	int iItem = ListView_GetNextItem(m_hwndFldList, -1, LVNI_SELECTED);
	// Get the lParam, because it is the index to the fields of the block spec.
	FW_LVITEM fwlvi;
	fwlvi.mask = LVIF_PARAM;
	fwlvi.iItem = iItem;
	fwlvi.iSubItem = 0;
	Fw_ListView_GetItem(m_hwndFldList, &fwlvi);
	RecordSpecPtr qrsp;
	m_ptod->GetBlockVec(*m_pvuvs, m_ivw, m_ptod->CurObjVecIndex(), &qrsp);
	BlockVec & vpbsp = qrsp->m_vqbsp;
	FldSpecPtr qfsp;

	int ilParam = fwlvi.lParam;

	if (ilParam < 1000)
	{
		// Outer level.
		iItem = ilParam;
		qfsp = vpbsp[ilParam];
	}
	else
	{
		// Inside a group.
		int iGroupItem;
		iItem = ilParam % 1000;
		iGroupItem = ((ilParam - 1000) - iItem) / 1000;
		qfsp = vpbsp[iItem]->m_vqfsp[iGroupItem];
	}
	SmartBstr sbstr;
	CheckHr(qfsp->m_qtssLabel->get_Text(&sbstr));

	*ppfsp = qfsp.Detach();
	*pprsp = qrsp.Detach();
}

/*----------------------------------------------------------------------------------------------
	Bring up the Modify Field Settings dialog.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwD::ModFldSet()
{
	StrUni stuDefParaChars = L""; // Dummy style name for "no character style at all"
	stuDefParaChars.Load(kstidDefParaChars);

	FldSpecPtr qfsp;
	RecordSpecPtr qrsp;
	FindFieldSpec(&qfsp, &qrsp);
	FldVis vis = qfsp->m_eVisibility;
	FldType fldtyp = qfsp->m_ft;
	StrUni stuSty = qfsp->m_stuSty;

	// Set up structure for updating the table below.
	int iItem = ListView_GetNextItem(m_hwndFldList, -1, LVNI_SELECTED);
	FW_LVITEM fwlvi;
	fwlvi.iItem = iItem;

	switch (fldtyp)
	{
	case kftString:
	case kftMsa:
	case kftMta:
	case kftEnum:
	case kftUnicode:
	case kftTtp:
	case kftLimEmbedLabel:
	case kftStText:
	case kftDateRO:
	case kftTitleGroup:
	case kftDate:
	case kftGenDate:
	case kftDummy:
	case kftInteger:
	case kftObjRefAtomic:
	case kftObjRefSeq:
		{
			// It is a text field type
			ModFldSetDlgTPtr qmfsdt;
			qmfsdt.Create();
			qmfsdt->SetDialogValues(vis, (UserViewType)(*m_pvuvs)[m_ivw]->m_vwt, stuSty);
			if (qmfsdt->DoModal(m_hwnd) == kctidOk)
			{
				qmfsdt->GetDialogValues(&vis, stuSty);
				qfsp->m_eVisibility = vis;
				if (stuSty.Equals(stuDefParaChars))
					qfsp->m_stuSty.Clear();
				else
					qfsp->m_stuSty = stuSty;
				fwlvi.mask = LVIF_TEXT;
				fwlvi.iSubItem = 1;
				Fw_ListView_GetItem(m_hwndFldList, &fwlvi);
				fwlvi.qtss = m_rgqFldVis[(int) vis];
				Fw_ListView_SetItem(m_hwndFldList, &fwlvi);

				// Add the window to the delete list which will update it when OK is pressed.
				int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
				if (iwndClient >= 0)
					m_psiwndClientDel->Insert(iwndClient);
				qrsp->m_fDirty = true;
				UpdateFldList(fwlvi.iItem);
			}
			break;
		}
	case kftGroupOnePerLine:
	case kftGroup:
		{
			// It is a Group header. Do nothing.
			/**
			ModFldSetDlgGPtr qmfsdg;
			qmfsdg.Create();
			if (qmfsdg->DoModal(m_hwnd) == kctidOk)
			{
				qmfsdg->GetDialogValues(&vis);
				fwlvi.mask = LVIF_TEXT;
				fwlvi.iSubItem = 1;
				for (int ifld = 0; ifld < vpbsp[iItem]->m_vqfsp.Size(); ++ifld)
					vpbsp[iItem]->m_vqfsp[ifld]->m_eVisibility = vis;

				// Add the window to the delete list which will update it when OK is pressed.
				int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
				if (iwndClient >= 0)
					m_psiwndClientDel->Insert(iwndClient);
				qrsp->m_fDirty = true;
				UpdateFldList(fwlvi.iItem);
			}
			**/
			break;
		}
	case kftRefAtomic:
	case kftRefCombo:
	case kftRefSeq:
		{
			// it is a Choices List field type
			PossNameType pnt = qfsp->m_pnt;
			bool fHier = qfsp->m_fHier;
			bool fVert = qfsp->m_fVert;
			ModFldSetDlgCLPtr qmfsdcl;
			qmfsdcl.Create();
			qmfsdcl->SetDialogValues(vis, (UserViewType)(*m_pvuvs)[m_ivw]->m_vwt, stuSty, pnt,
				fHier, fVert);
			if (qmfsdcl->DoModal(m_hwnd) == kctidOk)
			{
				qmfsdcl->GetDialogValues(&vis, stuSty, &pnt, &fHier, &fVert);
				qfsp->m_eVisibility = vis;
				if (stuSty.Equals(stuDefParaChars))
					qfsp->m_stuSty.Clear();
				else
					qfsp->m_stuSty = stuSty;
				qfsp->m_pnt = pnt;
				qfsp->m_fHier = fHier;
				qfsp->m_fVert = fVert;
				fwlvi.mask = LVIF_TEXT;
				fwlvi.iSubItem = 1;
				Fw_ListView_GetItem(m_hwndFldList, &fwlvi);
				fwlvi.qtss = m_rgqFldVis[(int) vis];
				Fw_ListView_SetItem(m_hwndFldList, &fwlvi);

				// Add the window to the delete list which will update it when OK is pressed.
				int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
				if (iwndClient >= 0)
					m_psiwndClientDel->Insert(iwndClient);
				qrsp->m_fDirty = true;
				UpdateFldList(fwlvi.iItem);
			}
			break;
		}
	case kftExpandable:
		{
			// it is a expandable field type
			PossNameType pnt = qfsp->m_pnt;
			bool fHier = qfsp->m_fHier;
			bool fVert = qfsp->m_fVert;
			bool fExpand = qfsp->m_fExpand;
			ModFldSetDlgExpPtr qmfsde;
			qmfsde.Create();
			qmfsde->SetDialogValues(vis, (UserViewType)(*m_pvuvs)[m_ivw]->m_vwt, stuSty, pnt,
				fHier, fVert, fExpand,(*m_pvuvs)[m_ivw]->m_vwt ==	kvwtDoc);
			if (qmfsde->DoModal(m_hwnd) == kctidOk)
			{
				qmfsde->GetDialogValues(&vis, stuSty, &pnt, &fHier, &fVert, &fExpand);
				qfsp->m_eVisibility = vis;
				if (stuSty.Equals(stuDefParaChars))
					qfsp->m_stuSty.Clear();
				else
					qfsp->m_stuSty = stuSty;
				qfsp->m_pnt = pnt;
				qfsp->m_fHier = fHier;
				qfsp->m_fVert = fVert;
				qfsp->m_fExpand = fExpand;
				fwlvi.mask = LVIF_TEXT;
				fwlvi.iSubItem = 1;
				Fw_ListView_GetItem(m_hwndFldList, &fwlvi);
				fwlvi.qtss = m_rgqFldVis[(int) vis];
				Fw_ListView_SetItem(m_hwndFldList, &fwlvi);

				// Add the window to the delete list which will update it when OK is pressed.
				int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
				if (iwndClient >= 0)
					m_psiwndClientDel->Insert(iwndClient);
				qrsp->m_fDirty = true;
				UpdateFldList(fwlvi.iItem);
			}
			break;
		}
	case kftSubItems:
		{
			// it is a Hierarchical field type
			OutlineNumSty ons = qfsp->m_ons;
			bool fExpand = qfsp->m_fExpand;
			ModFldSetDlgHiPtr qmfsdh;
			qmfsdh.Create();
			qmfsdh->SetDialogValues(vis, (UserViewType)(*m_pvuvs)[m_ivw]->m_vwt, stuSty, ons,
				fExpand,(*m_pvuvs)[m_ivw]->m_vwt ==	kvwtDoc);
			if (qmfsdh->DoModal(m_hwnd) == kctidOk)
			{
				qmfsdh->GetDialogValues(&vis, stuSty, &ons, &fExpand);
				qfsp->m_eVisibility = vis;
				if (stuSty.Equals(stuDefParaChars))
					qfsp->m_stuSty.Clear();
				else
					qfsp->m_stuSty = stuSty;
				qfsp->m_ons = ons;
				qfsp->m_fExpand = fExpand;
				fwlvi.mask = LVIF_TEXT;
				fwlvi.iSubItem = 1;
				Fw_ListView_GetItem(m_hwndFldList, &fwlvi);
				fwlvi.qtss = m_rgqFldVis[(int) vis];
				Fw_ListView_SetItem(m_hwndFldList, &fwlvi);

				// Add the window to the delete list which will update it when OK is pressed.
				int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
				if (iwndClient >= 0)
					m_psiwndClientDel->Insert(iwndClient);
				qrsp->m_fDirty = true;
				UpdateFldList(fwlvi.iItem);
			}
			break;
		}
	default:
		Assert(false); // The new constant should be added to the enum.
		break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Modify Visibility field command.

	@param pcmd Menu command that was clicked
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwD::CmdModVis(Cmd * pcmd)
{
	AssertObj(pcmd);
	FldVis nCurSel;
	switch (pcmd->m_cid)
	{
	case kcidTlsOptVwModAlways:
		nCurSel = kFTVisAlways;
		break;
	case kcidTlsOptVwModIfData:
		nCurSel = kFTVisIfData;
		break;
	case kcidTlsOptVwModNVis:
		nCurSel = kFTVisNever;
		break;
	case kcidTlsOptVwDocIfData:
		nCurSel = kFTVisIfData;
		break;
	case kcidTlsOptVwDocNVis:
		nCurSel = kFTVisNever;
		break;
	default:
		Assert(false);
		return true;
	}
	int iItem = ListView_GetNextItem(m_hwndFldList, -1, LVNI_SELECTED);

	// Get the lParam, because it is the index to the fields of the block spec.
	FW_LVITEM fwlvi;
	fwlvi.mask = LVIF_PARAM;
	fwlvi.iItem = iItem;
	fwlvi.iSubItem = 0;
	int iClsfn;
	Fw_ListView_GetItem(m_hwndFldList, &fwlvi);
	RecordSpecPtr qrsp;
	m_ptod->GetBlockVec(*m_pvuvs, m_ivw, m_ptod->CurObjVecIndex(), &qrsp);
	BlockVec & vpbsp = qrsp->m_vqbsp;

	int ilParam = fwlvi.lParam;

	if (ilParam < 1000)
	{
		// It is NOT a Document view Classification
		iItem = ilParam;
		if ((vpbsp[iItem]->m_fRequired == kFTReqReq) & ((*m_pvuvs)[m_ivw]->m_vwt ==
			kvwtDE) & (nCurSel != kFTVisAlways))
		{
			// It is a required DE View field and user is trying to make field not visible.
			ModFldSetNoticePtr qmfsn;
			qmfsn.Create();
			qmfsn->DoModal(m_hwnd);
			UpdateFldList(iItem);
			return true;
		}
		else
			vpbsp[ilParam]->m_eVisibility = nCurSel;
	}
	else
	{
		// It is a Document view Classification
		iItem = ilParam % 1000;
		iClsfn = ((ilParam - 1000) - iItem) / 1000;
		vpbsp[iItem]->m_vqfsp[iClsfn]->m_eVisibility = nCurSel;
	}

	fwlvi.mask = LVIF_TEXT;
	fwlvi.iSubItem = 1;
	Fw_ListView_GetItem(m_hwndFldList, &fwlvi);
	fwlvi.qtss = m_rgqFldVis[nCurSel];
	Fw_ListView_SetItem(m_hwndFldList, &fwlvi);
	qrsp->m_fDirty = true;

	// Add the window to the delete list which will update it when OK is pressed.
	int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
	if (iwndClient >= 0)
		m_psiwndClientDel->Insert(iwndClient);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Move field up or down in the filed list view control.

	@param updwn move the field up (kMoveUp) or down (kMoveDwn)
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwD::MoveFld(int updwn)
{
	ITsStringPtr qtssSel;

	int iItem = ListView_GetNextItem(m_hwndFldList, -1, LVNI_SELECTED);
	if (iItem < 0)
	{
		// Do nothing if there is no Field selected.
		return true;
	}

	// Add the window to the delete list which will update it when OK is pressed.
	int iwndClient = (*m_pvuvs)[m_ivw]->m_iwndClient;
	if (iwndClient >= 0)
		m_psiwndClientDel->Insert(iwndClient);

	//get selected field, save name so we can select it later, after the move.
	FW_LVITEM fwlvi;
	fwlvi.mask = LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM;
	fwlvi.iItem = iItem;
	fwlvi.iSubItem = 0;
	Fw_ListView_GetItem(m_hwndFldList, &fwlvi);
	int iFld = fwlvi.lParam;
	qtssSel = fwlvi.qtss;

	RecordSpecPtr qrsp;
	m_ptod->GetBlockVec(*m_pvuvs, m_ivw, m_ptod->CurObjVecIndex(), &qrsp);
	BlockVec & vpbsp = qrsp->m_vqbsp;
	int iClsfn;
	if (iFld < 1000)
	{
		//selected item is not a "Classifications"
		BlockSpecPtr qbsp = vpbsp[iFld];
		if (updwn == TlsOptDlgVw::kMoveUp)
		{
			// Move field up one positon
			if (iFld < 1)
			{
				::SetFocus(m_hwndFldList);
				return true;
			}
			vpbsp.Delete(iFld);
			vpbsp.Insert(iFld - 1, qbsp);
		}
		else
		{
			// Move field down one positon
			if (iFld == vpbsp.Size() - 1)
			{
				::SetFocus(m_hwndFldList);
				return true;
			}
			vpbsp.Delete(iFld);
			vpbsp.Insert(iFld + 1, qbsp);
		}
	}
	else
	{
		// Selected item is a "Classifications"
		iItem = iFld % 1000;
		iClsfn = ((iFld - 1000) - iItem) / 1000;
		BlockSpecPtr qbsp = vpbsp[iItem];
		FldSpecPtr qfsp = qbsp->m_vqfsp[iClsfn];

		if (updwn == TlsOptDlgVw::kMoveUp)
		{
			// Move field up one positon
			if (iClsfn < 1)
			{
				::SetFocus(m_hwndFldList);
				return true;
			}
			qbsp->m_vqfsp.Delete(iClsfn);
			qbsp->m_vqfsp.Insert(iClsfn - 1, qfsp);
		}
		else
		{
			// Move field Down one positon
			int iTopItem = vpbsp[iItem]->m_vqfsp.Size() - 1;
			if (iClsfn == iTopItem)
			{
				::SetFocus(m_hwndFldList);
				return true;
			}
			qbsp->m_vqfsp.Delete(iClsfn);
			qbsp->m_vqfsp.Insert(iClsfn + 1, qfsp);
		}
	}

	qrsp->m_fDirty = true;
	UpdateFldList(-1);

	// Reselect the item now
	int iStart = -1;
	FW_LVFINDINFO flvfi;
	flvfi.flags = LVFI_STRING;
	flvfi.qtss = qtssSel;
	iItem = Fw_ListView_FindItem(m_hwndFldList, iStart, &flvfi);

	fwlvi.mask = LVIF_STATE;
	fwlvi.iItem = iItem;
	fwlvi.iSubItem = 0;
	fwlvi.state = LVIS_SELECTED | LVIS_FOCUSED;
	fwlvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
	Fw_ListView_SetItem(m_hwndFldList, &fwlvi);
	ListView_EnsureVisible(m_hwndFldList, iItem, false);
	::SetFocus(m_hwndFldList);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle a change in a combo box.

	@param pnmh Windows command that is being passed.
	@param lnRet Out Return value that will be returned to windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwD::OnComboChange(NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	// Get the current index from the combo box.
	int icb = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);

	m_ptod->SetCurObjVecIndex(icb);

	// Update dialog controls.
	UpdateFldList(0);

	lnRet = 0;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Update Field listView Control.

	@param iSel Index to the item to be reselected after updating. (-1 for no selection)
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVwD::UpdateFldList(int iSel)
{
	::SendMessage(m_hwndFldList, LVM_DELETEALLITEMS, 0, 0);

	if (m_pvuvs->Size() == 0)
		return true;

	FW_LVITEM fwlvi;
	RecordSpecPtr qrsp;
	m_ptod->GetBlockVec(*m_pvuvs, m_ivw, m_ptod->CurObjVecIndex(), &qrsp);
	BlockVec & vpbsp = qrsp->m_vqbsp;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu;

	int iLvItem = 0;
	int ifld;
	for (ifld = 0; ifld < vpbsp.Size(); ++ifld)
	{
		fwlvi.mask = LVIF_TEXT | LVIF_PARAM;
		fwlvi.iItem = iLvItem;
		fwlvi.iSubItem = 0;
		fwlvi.qtss = vpbsp[ifld]->m_qtssLabel;
		fwlvi.lParam = (LPARAM) ifld;
		Fw_ListView_InsertItem(m_hwndFldList, &fwlvi);
		fwlvi.mask = LVIF_TEXT;
		fwlvi.iSubItem = 1;

		if (vpbsp[ifld]->m_ft == kftGroup || vpbsp[ifld]->m_ft == kftGroupOnePerLine)
		{
			stu = "";
			qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &fwlvi.qtss);
			Fw_ListView_SetItem(m_hwndFldList, &fwlvi);
			iLvItem = UpdateClassifications(iLvItem, ifld);
		}
		else
		{
			fwlvi.qtss = m_rgqFldVis[vpbsp[ifld]->m_eVisibility];
			Fw_ListView_SetItem(m_hwndFldList, &fwlvi);
			// JT: non-groups may now also have "classifications", that is, subfields.
			// Review JohnT: does anything else in the code need to change?
			if (vpbsp[ifld]->m_vqfsp.Size() > 0)
				iLvItem = UpdateClassifications(iLvItem, ifld);
		}

		stu = vpbsp[ifld]->m_stuSty;
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &fwlvi.qtss);
		fwlvi.iSubItem = 2;
		Fw_ListView_SetItem(m_hwndFldList, &fwlvi);

		iLvItem++;
	}

	if (iSel > -1)
	{
		fwlvi.mask = LVIF_STATE;
		fwlvi.iItem = iSel;
		fwlvi.iSubItem = 0;
		fwlvi.state = LVIS_SELECTED | LVIS_FOCUSED;
		fwlvi.stateMask = LVIS_SELECTED | LVIS_FOCUSED;
		Fw_ListView_SetItem(m_hwndFldList, &fwlvi);
		ListView_EnsureVisible(m_hwndFldList, iSel, false);
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the selected Field index from the listView.

	@return index of selection, or if nothing is selected return 0.
----------------------------------------------------------------------------------------------*/
int TlsOptDlgVwD::GetSelFldIdx()
{
	int iItem = ListView_GetNextItem(m_hwndFldList, -1, LVNI_SELECTED);
	if (iItem < 0)
	{
		iItem = 0;
	}
	return iItem;
}

/*----------------------------------------------------------------------------------------------
	Update Field listView Classifications.

	@param iLvItem The top listview item index.
	@param ifld Index to which blockspec to update.
	@return true
----------------------------------------------------------------------------------------------*/
int TlsOptDlgVwD::UpdateClassifications(int iLvItem, int ifld)
{
	FW_LVITEM lvi(LVIF_TEXT);
	RecordSpecPtr qrsp;
	m_ptod->GetBlockVec(*m_pvuvs, m_ivw, m_ptod->CurObjVecIndex(), &qrsp);
	BlockVec & vpbsp = qrsp->m_vqbsp;

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu;
	int icfld;
	for (icfld = 0; icfld < vpbsp[ifld]->m_vqfsp.Size(); ++icfld)
	{
		lvi.iItem = ++iLvItem;
		lvi.iSubItem = 0;
		lvi.mask = LVIF_TEXT | LVIF_PARAM;
		lvi.lParam = (LPARAM) (icfld * 1000) + 1000 + ifld; // Classification lParam
		ITsStrBldrPtr qtsb;
		CheckHr(vpbsp[ifld]->m_vqfsp[icfld]->m_qtssLabel->GetBldr(&qtsb));
		CheckHr(qtsb->ReplaceTsString(0, 0, m_qtssIndent));
		CheckHr(qtsb->GetString(&lvi.qtss));
		Fw_ListView_InsertItem(m_hwndFldList, &lvi);
		lvi.iSubItem = 1;
		lvi.mask = LVIF_TEXT;
		lvi.qtss = m_rgqFldVis[vpbsp[ifld]->m_vqfsp[icfld]->m_eVisibility];
		Fw_ListView_SetItem(m_hwndFldList, &lvi);


		stu = vpbsp[ifld]->m_vqfsp[icfld]->m_stuSty;
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &lvi.qtss);
		lvi.iSubItem = 2;
		Fw_ListView_SetItem(m_hwndFldList, &lvi);
	}

	return iLvItem;
}


//:>********************************************************************************************
//:>View Dialog Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
	@param ptod pointer to the main TlsOptDlg.
----------------------------------------------------------------------------------------------*/
TlsOptDlgVw::TlsOptDlgVw(TlsOptDlg * ptod)
{
	AssertPtr(ptod);
	m_ptod = ptod;
	m_rid = kridTlsOptDlgVw;
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_Views_tab.htm");
	m_himl = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TlsOptDlgVw::~TlsOptDlgVw()
{
	if (m_himl)
	{
		AfGdi::ImageList_Destroy(m_himl);
		m_himl = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Called when the dialog becomes active.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVw::SetActive()
{
	ShowDDlg();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param ptod vuvs user views to be used in the dialog
	@param ptod psiwndClientDel delete list that will be updated to hold all deleted or
	@param ivwInitial index to the initial view to be selected.
	modified views.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgVw::SetDialogValues(UserViewSpecVec & vuvs, Set<int> * psiwndClientDel,
	int ivwInitial)
{
	AssertPtr(psiwndClientDel);
	m_psiwndClientDel = psiwndClientDel;
	m_ivw = ivwInitial;
}


/*----------------------------------------------------------------------------------------------
	Fix the sort order of the internal vector of view information.  Make vector order match
	the ListView order.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVw::Apply()
{
	// Fix the sort order of the internal vector of view information.
	LVITEM lvi = { LVIF_PARAM };
	UserViewSpecVec vuvs;
	int citem = ListView_GetItemCount(m_hwndVwList);
	for (int iitem = 0; iitem < citem; iitem++)
	{
		lvi.iItem = iitem;
		if (!ListView_GetItem(m_hwndVwList, &lvi))
			ThrowHr(E_UNEXPECTED);
		Assert((uint)lvi.lParam < (uint)m_ptod->m_vuvs.Size());
		vuvs.Push(m_ptod->m_vuvs[lvi.lParam]);
	}
	Assert(vuvs.Size() == citem);
	m_ptod->m_vuvs = vuvs;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Show the Data Entry, Browse, or Document child Dialog.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVw::ShowDDlg()
{
	if (m_ptod->m_vuvs[m_ivw]->m_vwt == kvwtBrowse)
	{
		if (!m_qtodvB->Hwnd())
		{
			m_qtodvB->DoModeless(m_hwnd);
			::SetWindowPos(m_qtodvB->Hwnd(), NULL, m_dxsClient, m_dysClient, 0, 0,
				SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
			::SetFocus(m_hwndVwList);
		}

		::ShowWindow(m_qtodvB->Hwnd(), SW_SHOW);
		if (m_qtodvD->Hwnd())
			::ShowWindow(m_qtodvD->Hwnd(), SW_HIDE);
		m_qtodvB->SetViewIndex(m_ivw);
		m_qtodvB->UpdateFldList(0);
		m_qtodvB->UpdateDisp(0);
		m_qtodvB->UpdateLineCtrls();
	}
	else
	{
		if (!m_qtodvD->Hwnd())
		{
			m_qtodvD->DoModeless(m_hwnd);
			::SetWindowPos(m_qtodvD->Hwnd(), NULL, m_dxsClient, m_dysClient, 0, 0,
				SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
			::SetFocus(m_hwndVwList);
		}

		::ShowWindow(m_qtodvD->Hwnd(), SW_SHOW);
		if (m_qtodvB->Hwnd())
			::ShowWindow(m_qtodvB->Hwnd(), SW_HIDE);
		m_qtodvD->SetViewIndex(m_ivw);

		if (m_fEditLabel)
		{
			m_qtodvD->UpdateFldList(m_qtodvD->GetSelFldIdx());
			m_fEditLabel = false;
		}
		else
		{
			m_qtodvD->UpdateFldList(0);
			HWND hwndFIn = ::GetDlgItem(m_qtodvD->Hwnd(), kcidTlsOptDlgVwFIn);
			::SendMessage(hwndFIn, CB_SETCURSEL, m_ptod->CurObjVecIndex(), 0);
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Copy command.  Copy a view to a new view.

	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVw::CmdCopy()
{
	Assert(m_ivw != -1);
	LVFINDINFO lvfi = { LVFI_PARAM };
	lvfi.lParam = m_ivw;
	int iitem = ListView_FindItem(m_hwndVwList, -1, &lvfi);
	Assert(iitem != -1);

	achar rgchBuf[1024];
	LVITEM lvi = { LVIF_TEXT, iitem };
	lvi.pszText = rgchBuf;
	lvi.cchTextMax = isizeof(rgchBuf);
	ListView_GetItem(m_hwndVwList, &lvi);

	UserViewSpecPtr quvs;
	m_ptod->m_vuvs[m_ivw]->NewCopy(&quvs);
	// Clear hvo to indicate a new UserViewSpec that hasn't been saved to the database.
	quvs->m_hvo = 0;

	StrApp strName(rgchBuf);
	m_ptod->FixName(strName, m_hwndVwList, true);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu = strName.Chars();
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_ptod->MainWnd()->UserWs(),
		&quvs->m_qtssName);
	quvs->m_fv = false;
	quvs->m_iwndClient = -1; // We have no window client index yet.

	// Add the view to m_vuvs and the listview
	lvi.mask = LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM;
	lvi.iImage = (int)quvs->m_vwt;
	lvi.pszText = const_cast<achar *>(strName.Chars());
	lvi.iItem = 0; // Order doesn't matter because it's sorted automatically.
	lvi.iSubItem = 0;
	lvi.lParam = m_ptod->m_vuvs.Size();
	int iItem = ListView_InsertItem(m_hwndVwList, &lvi);
	m_ptod->m_vuvs.Push(quvs);

	ListView_SetItemState(m_hwndVwList, iItem, LVIS_FOCUSED | LVIS_SELECTED,
		LVIS_FOCUSED | LVIS_SELECTED);
	ListView_EnsureVisible(m_hwndVwList, iItem, false);
	::SendMessage(m_hwnd, WM_NEXTDLGCTL, (WPARAM)m_hwndVwList, true);
	ListView_EditLabel(m_hwndVwList, iItem);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Delete command.  Delete the view whose index is m_ivw in the vector m_vuvs.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgVw::DeleteView()
{
	Assert((uint)m_ivw < (uint)m_ptod->m_vuvs.Size());

	StrApp strTitle(kstidDeleteView);
	StrApp strPrompt(kstidTlsOptVwDMsg);

	const achar * pszHelpUrl = m_pszHelpUrl;
	m_pszHelpUrl = _T("Advanced_Tasks/Customizing_Views/Delete_a_view.htm");

	ConfirmDeleteDlgPtr qcdd;
	qcdd.Create();
	qcdd->SetTitle(strTitle.Chars());
	qcdd->SetPrompt(strPrompt.Chars());
	qcdd->SetHelpUrl(m_pszHelpUrl);
	// Make sure the user really wants to delete the view.
	if (qcdd->DoModal(m_hwnd) == kctidOk)
	{
		WaitCursor wc;

		// Add the window to the delete list which will update it when OK is pressed.
		int iwndClient = m_ptod->m_vuvs[m_ivw]->m_iwndClient;
		if (iwndClient >= 0)
			m_psiwndClientDel->Insert(iwndClient);

		// Delete it from the listview
		m_ptod->m_vuvs.Delete(m_ivw, m_ivw + 1);

		// m_ivw will get updated properly in UpdateVwList.
		UpdateVwList();
	}
	m_pszHelpUrl = pszHelpUrl;
}


/*----------------------------------------------------------------------------------------------
	Need to clean up.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgVw::OnReleasePtr()
{
	SuperClass::OnReleasePtr();
	AfApp::Papp()->RemoveCmdHandler(this, 1);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.

	@param wm windows message
	@param wp WPARAM
	@param lp LPARAM
	@param lnRet Value to be returned to the windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVw::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ERASEBKGND)
		// this is required to prevent the listview from not painting when selected then covered
		// by another window, then uncovered.  without this the listview will not repaint.
		::RedrawWindow(m_hwndVwList, NULL , NULL, RDW_ERASE | RDW_FRAME | RDW_INTERNALPAINT |
			RDW_INVALIDATE);
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVw::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_fEditLabel = false;

	// If any of these asserts fail, the bitmap containing the images for this dialog needs
	// to be modified and these enum values need to be changed.
	Assert(kimagBrowse == kvwtBrowse);
	Assert(kimagDataEntry == kvwtDE);
	Assert(kimagDocument == kvwtDoc);

	m_fEnableUpdate = false;

	AfApp::Papp()->AddCmdHandler(this,1);

	// Creates image list for a list view.
	m_hwndVwList = ::GetDlgItem(m_hwnd, kcidTlsOptDlgVwVws);

	if (!m_himl)
		m_himl = AfGdi::ImageList_Create(16, 16, ILC_COLORDDB | ILC_MASK, 0, 0);
	if (!m_himl)
		ThrowHr(WarnHr(E_FAIL));
	HBITMAP hbmp = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
		MAKEINTRESOURCE(kridTlsOptVwBmp));
	if (!hbmp || ::ImageList_AddMasked(m_himl, hbmp, kclrPink) == -1)
		ThrowHr(WarnHr(E_FAIL));
	AfGdi::DeleteObjectBitmap(hbmp);

	// Assign the image lists to the list view control and add text.
	HIMAGELIST himlOld = ListView_SetImageList(m_hwndVwList, m_himl, LVSIL_SMALL);
	if (himlOld)
		if (himlOld != m_himl)
			AfGdi::ImageList_Destroy(himlOld);

	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	Rect rc;
	::GetClientRect(m_hwndVwList, &rc);
	lvc.cx = rc.Width();
	ListView_InsertColumn(m_hwndVwList, 0, &lvc);

	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kcidTlsOptDlgVwAdd, kbtPopMenu, NULL, 0);

	// get the location of where the child dialogs will go.
	RECT rcList;
	::GetWindowRect(m_hwndVwList, &rcList);
	TabCtrl_AdjustRect(m_hwndVwList, false, &rcList);
	POINT pt = { rcList.right, rcList.top };
	::ScreenToClient(m_hwnd, &pt);
	m_dxsClient = pt.x;
	m_dysClient = 0; //pt.y;

	// create the child dialog objects.
	m_qtodvD.Attach(NewObj TlsOptDlgVwD(m_ptod));
	m_qtodvD->SetDialogValues(m_himl, &m_ptod->m_vuvs, m_psiwndClientDel);
	m_qtodvB.Attach(NewObj TlsOptDlgVwBr(m_ptod));
	m_qtodvB->SetDialogValues(m_himl, &m_ptod->m_vuvs, m_psiwndClientDel);

	UpdateVwList();

	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwDel), !m_ptod->m_vuvs[m_ivw]->m_fv);

	ShowDDlg();

	m_fEnableUpdate = true;

	return SuperClass::OnInitDlg(hwndCtrl, lp);
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
bool TlsOptDlgVw::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	NMLISTVIEW * pnmlv = (NMLISTVIEW *)pnmh;
	Rect rc;
	Rect rc1;
	HMENU hmenuPopup;
	ITsStringPtr qtss;
	FW_LVITEM fwlvi;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (pnmh->idFrom)
		{

		case kcidTlsOptDlgVwCpy:
			CmdCopy();
			return true;

		case kcidTlsOptDlgVwAdd:
			{
				hmenuPopup = ::CreatePopupMenu();
				StrAppBuf strb;
				Vector<TlsView> & vtv = m_ptod->ViewVec();
				for (int itv = 0; itv < vtv.Size(); ++itv)
				{
					switch (vtv[itv].m_vwt)
					{
					case kvwtBrowse:
						strb.Load(kstidBrowse);
						::AppendMenu(hmenuPopup, MF_STRING, kcidTlsOptAddBrView, strb.Chars());
						break;
					case kvwtDE:
						strb.Load(kstidDataEntry);
						::AppendMenu(hmenuPopup, MF_STRING, kcidTlsOptAddDEView, strb.Chars());
						break;
					case kvwtDoc:
						strb.Load(kstidDocument);
						::AppendMenu(hmenuPopup, MF_STRING, kcidTlsOptAddDocView, strb.Chars());
						break;
					}
				}

				::GetWindowRect(pnmh->hwndFrom, &rc);
				::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left,
					rc.bottom, 0, m_hwnd, NULL);
				::DestroyMenu(hmenuPopup);
				return true;
			}
		case kcidTlsOptDlgVwDel:
			Assert(!m_ptod->m_vuvs[m_ivw]->m_fv);
			DeleteView();
			break;
		}
		break;

	case LVN_ITEMCHANGED:
		if (pnmh->idFrom == kcidTlsOptDlgVwVws && m_fEnableUpdate == true)
		{
			if (pnmlv->uNewState & LVIS_SELECTED)
			{
				m_ivw = pnmlv->lParam;

				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgVwDel), !m_ptod->m_vuvs[m_ivw]->m_fv);

				ShowDDlg();
			}
		}
		break;

	case LVN_ITEMCHANGING:
		{
			// If the user clicked on an empty part of the list view, keep the selection
			// on the current item.
			NMLISTVIEW * pnmlv = (NMLISTVIEW *)pnmh;
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

	case LVN_BEGINLABELEDIT:
		if (m_ptod->m_vuvs[m_ivw]->m_fv)
			lnRet = true;
		m_fEditLabel = true;
		return true;

	case LVN_ENDLABELEDIT:
		return OnEndLabelEdit((NMLVDISPINFO *)pnmh, lnRet);

	case LVN_KEYDOWN:
		{
			Assert(pnmh->hwndFrom == m_hwndVwList);
			NMLVKEYDOWN * pnmlvkd = (NMLVKEYDOWN *)pnmh;
			if (pnmlvkd->wVKey == VK_DELETE && !m_ptod->m_vuvs[m_ivw]->m_fv)
				DeleteView();
			else if (pnmlvkd->wVKey == VK_F2)
			{
				int iitem = ListView_GetNextItem(pnmh->hwndFrom, -1, LVNI_SELECTED);
				if (iitem != -1)
					ListView_EditLabel(pnmh->hwndFrom, iitem);
			}
			break;
		}

	default:
		//	Default is do nothing.
		break;
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	This is called upon ending an label editing operation.  The label is checked to make sure
	it is valid, then the view name is updated.

	@param plvdi Contains information about an the notification message.
	@param lnRet Value to be returned to the windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVw::OnEndLabelEdit(NMLVDISPINFO * plvdi, long & lnRet)
{
	AssertPtr(plvdi);

	if (plvdi->item.pszText)
	{
		AssertPsz(plvdi->item.pszText);
		Assert(plvdi->item.lParam == m_ivw);

		// Strip off blank characters at the front and end of the name.
		StrApp strLabel;
		StrUtil::TrimWhiteSpace(plvdi->item.pszText, strLabel);

		if (strLabel.Length() == 0)
		{
			// The item is empty, so show a message complaining about it.
			StrApp strMessage(kstidViewRenEmptyMsg);
			StrApp strView(kstidTlsOptView);
			::MessageBox(m_hwnd, strMessage.Chars(), strView.Chars(),
				MB_OK | MB_ICONINFORMATION);
			::PostMessage(plvdi->hdr.hwndFrom, LVM_EDITLABEL, plvdi->item.iItem, 0);
			return true;
		}

		// See if there is already an item with the same name.
		LVFINDINFO lvfi = { LVFI_STRING };
		lvfi.psz = strLabel.Chars();
		int iitem = ListView_FindItem(m_hwndVwList, -1, &lvfi);
		int ivw = -1;
		if (iitem != -1)
		{
			LVITEM lvi = { LVIF_PARAM, iitem };
			ListView_GetItem(m_hwndVwList, &lvi);
			ivw = lvi.lParam;
		}
		// If they didn't change the name, we're done.
		if (ivw == m_ivw)
			return true;
		if (ivw != -1)
		{
			StrApp strMessage(kstidViewRenViewMsg);
			StrApp strView(kstidTlsOptView);
			::MessageBox(m_hwnd, strMessage.Chars(), strView.Chars(),
				MB_OK | MB_ICONINFORMATION);
			::PostMessage(plvdi->hdr.hwndFrom, LVM_EDITLABEL, plvdi->item.iItem, 0);
			return true;
		}

		// Update the name of the selected view.
		UserViewSpec * puvs = m_ptod->m_vuvs[m_ivw];
		AssertPtr(puvs);

		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		StrUni stu(strLabel);
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_ptod->MainWnd()->UserWs(),
			&puvs->m_qtssName);

		// Add the window to the delete list which will update it when OK is pressed.
		int iwndClient = m_ptod->m_vuvs[m_ivw]->m_iwndClient;
		if (iwndClient >= 0)
			m_psiwndClientDel->Insert(iwndClient);

		UpdateVwList();
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Update View ListView Control.
----------------------------------------------------------------------------------------------*/
void TlsOptDlgVw::UpdateVwList()
{
	int iitemOld = 0;
	if (m_ivw != -1)
	{
		LVFINDINFO lvfi = { LVFI_PARAM };
		lvfi.lParam = m_ivw;
		iitemOld = ListView_FindItem(m_hwndVwList, -1, &lvfi);
	}

	::SendMessage(m_hwndVwList, WM_SETREDRAW, false, 0);
	ListView_DeleteAllItems(m_hwndVwList);

	StrApp str;
	const OLECHAR * pwrgch;
	int cch;
	LVITEM lvi = { LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM };
	int cvw = m_ptod->m_vuvs.Size();
	for (int ivw = 0; ivw < cvw; ++ivw)
	{
		lvi.iItem = ivw;
		lvi.iImage = (int)m_ptod->m_vuvs[ivw]->m_vwt;

		CheckHr(m_ptod->m_vuvs[ivw]->m_qtssName->LockText(&pwrgch, &cch));
		str.Assign(pwrgch, cch);
		m_ptod->m_vuvs[ivw]->m_qtssName->UnlockText(pwrgch);

		lvi.pszText = const_cast<achar *>(str.Chars());
		lvi.lParam = ivw;
		ListView_InsertItem(m_hwndVwList, &lvi);
	}

	if (m_ivw != -1)
	{
		// Find the index of the item that was previously selected.
		LVFINDINFO lvfi = { LVFI_PARAM };
		lvfi.lParam = m_ivw;
		int iitemNew = ListView_FindItem(m_hwndVwList, -1, &lvfi);

		if (iitemNew == -1)
		{
			iitemNew = iitemOld;

			// The old current selection is not in the list, so determine which item to select.
			int citem = ListView_GetItemCount(m_hwndVwList);
			if ((uint)iitemNew >= (uint)citem)
				iitemNew = citem - 1;
		}
		Assert(iitemNew != -1 || ListView_GetItemCount(m_hwndVwList) == 0);
		if (iitemNew != -1)
		{
			ListView_SetItemState(m_hwndVwList, iitemNew, LVIS_FOCUSED | LVIS_SELECTED,
				LVIS_FOCUSED | LVIS_SELECTED);
			ListView_EnsureVisible(m_hwndVwList, iitemNew, false);
		}
	}
#ifdef DEBUG
	else
	{
		// There should never be no views.
		Assert(false);
	}
#endif

	::SendMessage(m_hwndVwList, WM_SETREDRAW, true, 0);
	::InvalidateRect(m_hwndVwList, NULL, true);
}


/*----------------------------------------------------------------------------------------------
	Makes Default fields for a view

	@param vwt The type of view to be made.
	@param puvs UserViewSpec that the new view is to be added to.
	@return Index of Listview where the new view was inserted.
----------------------------------------------------------------------------------------------*/
int TlsOptDlgVw::MakeNewView(UserViewType vwt, UserViewSpec * puvs)
{
	AssertPtr(puvs);
	AssertPtr(m_ptod);

	RecMainWndPtr qrmw = m_ptod->MainWnd();
	qrmw->MakeNewView(vwt, qrmw->GetLpInfo(), puvs, & m_ptod->m_vuvs);

	StrApp strName;
	LVITEM lvi = { LVIF_TEXT };
	lvi.mask = LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM;
	switch (vwt)
	{
	case kvwtBrowse:
		strName.Load(kstidBrowse);
		break;
	case kvwtDE:
		strName.Load(kstidDataEntry);
		break;
	case kvwtDoc:
		strName.Load(kstidDocument);
		break;
	}
	m_ptod->FixName(strName, m_hwndVwList, false);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu = strName.Chars();
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_ptod->MainWnd()->UserWs(),
		&puvs->m_qtssName);

	puvs->m_fv = false;
	puvs->m_iwndClient = -1; // We have no window client index yet.
	puvs->m_nMaxLines = 5;
	puvs->m_fIgnorHier = false;
	m_ptod->m_vuvs.Push(puvs);

	int iuvsOld = 0;
	ITsStringPtr qtssNew;
	ITsStringPtr qtssOld;
	int cuvs = m_ptod->m_vuvs.Size();
	for (iuvsOld = 0; iuvsOld < cuvs; ++iuvsOld)
	{
		if (m_ptod->m_vuvs[iuvsOld]->m_vwt == kvwtDE)
			break;
	}
	Assert(iuvsOld < cuvs);
	int iuvsNew = m_ptod->m_vuvs.Size() - 1;

	// We now have the index of a data entry view, so ues it to correct all the
	// "required" and "visibility" fields for the uvs we just added.
	RecordSpecPtr qrspNew;
	RecordSpecPtr qrspOld;
	Vector<TlsObject> & vto = m_ptod->ObjectVec();
	for (int ito = 0; ito < vto.Size(); ++ito)
	{
		m_ptod->GetBlockVec(m_ptod->m_vuvs, iuvsOld, ito, &qrspOld);
		BlockVec & vpbspOld = qrspOld->m_vqbsp;
		m_ptod->GetBlockVec(m_ptod->m_vuvs, iuvsNew, ito, &qrspNew);
		BlockVec & vpbspNew = qrspNew->m_vqbsp;

		int ifldNew;
		for (ifldNew = 0; ifldNew < vpbspNew.Size(); ++ifldNew)
		{
			qtssNew = vpbspNew[ifldNew]->m_qtssLabel;
			int ifldOld;
			for (ifldOld = 0; ifldOld < vpbspOld.Size(); ++ifldOld)
			{
				ComBool fEqual;
				qtssNew->Equals(vpbspOld[ifldOld]->m_qtssLabel, &fEqual);
				if (fEqual)
				{
					vpbspNew[ifldNew]->m_fRequired = vpbspOld[ifldOld]->m_fRequired;
					if (vpbspNew[ifldNew]->m_fRequired == kFTReqReq)
						vpbspNew[ifldNew]->m_eVisibility = kFTVisAlways;
					break;
				}
			}
		}
	}
	lvi.iImage = (int)puvs->m_vwt;
	lvi.pszText = const_cast<achar *>(strName.Chars());
	lvi.lParam = m_ptod->m_vuvs.Size() - 1;
	lvi.iItem = 0; // Order doesn't matter because it's sorted automatically.
	lvi.iSubItem = 0;
	return ListView_InsertItem(m_hwndVwList, &lvi);
}


/*----------------------------------------------------------------------------------------------
	Set the state for an expanded menu item.
	cms.GetExpMenuItemIndex() returns the index of the item to set the state for.
	To get the menu handle and the old ID of the dummy item that was replaced, call
	AfMenuMgr::GetLastExpMenuInfo.

	@param cms menu command state
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVw::CmsViewAddMenu(CmdState & cms)
{
	cms.Enable(true);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Adds a new view.  This is called from the Add dropdown menu.
	@param pcmd Ptr to menu command
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlgVw::CmdViewAddMenu(Cmd * pcmd)
{
	AssertPtr(pcmd);
	UserViewSpecPtr quvs;
	quvs.Create();
	UserViewType uvwt;
	switch (pcmd->m_cid)
	{
	case kcidTlsOptAddBrView:
		uvwt = kvwtBrowse;
		break;
	case kcidTlsOptAddDEView:
		uvwt = kvwtDE;
		break;
	case kcidTlsOptAddDocView:
		uvwt = kvwtDoc;
		break;
	}

	int iItem = MakeNewView(uvwt, quvs);
	if (iItem != -1)
	{
		ListView_SetItemState(m_hwndVwList, iItem, LVIS_FOCUSED | LVIS_SELECTED,
			LVIS_FOCUSED | LVIS_SELECTED);
		ListView_EnsureVisible(m_hwndVwList, iItem, false);
		::SetFocus(m_hwndVwList);
		ListView_EditLabel(m_hwndVwList, iItem);
	}
	return true;
}

//:>********************************************************************************************
//:>ModFldSetNotice methods. (Notification that that user is attempting to make a required field
//:>not visible in a data entry view.  This is not allowed!
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModFldSetNotice::ModFldSetNotice(void)
{
	m_rid = kridModFldSetNotice;
	m_pszHelpUrl = _T("Advanced_Tasks/Customizing_Views/Modify_Field_Settings.htm");
}



//:>********************************************************************************************
//:>ModFldSetDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModFldSetDlg::ModFldSetDlg(void)
{
	// This is checked later to make sure it is set properly.
	// The Help URL for this class (and subclasses) depends on this being set.
	m_vwt = (UserViewType)-1;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModFldSetDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_fDisableEnChange = false;

	Assert(m_vwt != (UserViewType)-1);
	switch (m_vwt)
	{
	case kvwtDE:
		m_pszHelpUrl = _T("Advanced_Tasks/Customizing_Views/Modify_Field_Settings.htm");
		break;
	case kvwtDoc:
		m_pszHelpUrl = _T("Advanced_Tasks/Customizing_Views/Modify_Field_Settings.htm");
		// if doc view then hide "always visible" and move remaining radio buttons up.
		{ // block
			/* No, don't do this because Document views can now also offer "Always".
			RECT rc;
			HWND hwndCtrl = ::GetDlgItem(m_hwnd, kcidTlsOptDlgModAVis);
			ShowWindow(hwndCtrl, SW_HIDE);
			::GetWindowRect(hwndCtrl, &rc);
			int top = rc.top;
			hwndCtrl = ::GetDlgItem(m_hwnd, kcidTlsOptDlgModIDVis);
			::GetWindowRect(hwndCtrl, &rc);
			int offset = rc.top - top;

			MoveCtrl(kcidTlsOptDlgModIDVis, offset);
			MoveCtrl(kcidTlsOptDlgModNVis, offset);
			*/
		}
		break;
	case kvwtBrowse:
		{
			m_pszHelpUrl = _T("Advanced_Tasks/Customizing_Views/Modify_Field_Settings.htm");
		}
		break;
	default:
		{
			Assert(false);
			m_pszHelpUrl = NULL;
		}
		break;
	}

	// Initialize values for the Character Style combo box.
	SetCharStylesCombo();

	// Subclass the Style and help buttons.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	SetVis(m_nVis);
	return AfDialog::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Loads the Character Styles combo box with character styles from the cached styles for
	the language project. If m_stuSty is set, it sets the current item to m_stuSty.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlg::SetCharStylesCombo()
{
	HWND hwndCharSty = ::GetDlgItem(m_hwnd, kcidTlsOptCharSty);
	if (!hwndCharSty)
		hwndCharSty = ::GetDlgItem(m_hwnd, kcidTlsOptCharStyForText);
	Assert(hwndCharSty);

	// Empty the combo-box.
	::SendMessage(hwndCharSty, CB_RESETCONTENT , 0, 0);

	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	CustViewDaPtr qcvd;
	plpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	HVO hvo = plpi->GetLpId();
	int chvoSty;
	StrAppBuf strbCur; // The currently selected string.
	StrApp strNone(kstidTlsOptVwStNone);
	::SendMessage(hwndCharSty, CB_ADDSTRING, 0, (LPARAM)strNone.Chars());
	CheckHr(qcvd->get_VecSize(hvo, kflidLangProject_Styles, &chvoSty));
	for (int ihvo = 0; ihvo < chvoSty; ++ihvo)
	{
		HVO hvoSty;
		int nType;
		SmartBstr sbstr;
		StrAppBuf strb;
		CheckHr(qcvd->get_VecItem(hvo, kflidLangProject_Styles, ihvo, &hvoSty));
		CheckHr(qcvd->get_IntProp(hvoSty, kflidStStyle_Type, &nType));
		if (nType != kstCharacter)
			continue;
		CheckHr(qcvd->get_UnicodeProp(hvoSty, kflidStStyle_Name, &sbstr));
		strb = sbstr.Chars();
		// Add the string to the combo-box.
		::SendMessage(hwndCharSty, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}

	StrUni stuDefParaChars = L""; // Dummy style name for "no character style at all"
	stuDefParaChars.Load(kstidDefParaChars);

	StrApp str = m_stuSty.Length() ? m_stuSty : stuDefParaChars;
	int icb = ::SendMessage(hwndCharSty, CB_FINDSTRINGEXACT, (WPARAM)-1, (LPARAM)str.Chars());
	::SendMessage(hwndCharSty, CB_SETCURSEL, icb, 0);
}

/*----------------------------------------------------------------------------------------------
	Open Styles Dialog. Put in a separate function so that a derived class can provide
	its own slightly different dialog (e.g. for TE)

	@h3{Parameters}
	@code{
		hwnd -- window handle
		past -- pointer to the IVwStylesheet for a particular language project.
		vqttpPara -- vector of TsTextProps for paragraph properties.
		cttpChar -- count of TsTextProps for character properties.
		vqttpChar -- pointer to range of TsTextProps for character properties.
		pstuStyleName -- name of selected style, when AdjustTsTextProps returns.
		fStylesChanged -- true if any of the styles have been changed, when AdjustTsTextProps
			returns.
		fApply -- true if the Apply button was clicked
		fReloadDb -- true if the views data needs to be reloaded from the DB; this is needed
			when styles were renamed.
	}
----------------------------------------------------------------------------------------------*/
bool ModFldSetDlg::OpenFormatStylesDialog(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
	IVwStylesheet * past, TtpVec & vqttpPara, TtpVec & vqttpChar, bool fCanFormatChar,
	StrUni * pstuStyleName, bool & fStylesChanged, bool & fApply, bool & fReloadDb)
{
	Assert(sizeof(ITsTextPropsPtr) == sizeof(ITsTextProps *));
	IFwCppStylesDlgPtr qfwst;
	qfwst.CreateInstance(CLSID_FwCppStylesDlg);
	StrUni stuHelpFile(AfApp::Papp()->GetHelpFile());
	ILgWritingSystemFactoryPtr qwsf;
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	pafw->GetLgWritingSystemFactory(&qwsf);
	if (!qwsf)
	{
		AfVwWnd * pavw = dynamic_cast<AfVwWnd *>(MainWindow());
		if (pavw)
			pavw->GetLgWritingSystemFactory(&qwsf);
	}
	AssertPtr(qwsf);
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	int nResult;
	ComBool fStylesChangedT;
	ComBool fApplyT;
	ComBool fReloadDbT;
	ComBool fResultT;
	SmartBstr sbstrStyleName;
	int hvoRoot = 0;
	// Get the writing systems "of interest" to the user.
	Vector<int> vws;
	if (pafw)
	{
		AfLpInfo * plpi = pafw->GetLpInfo();
		AssertPtr(plpi);
		Vector<int> & vwsAnal = plpi->AllAnalWss();
		Vector<int> & vwsVern = plpi->AllVernWss();
		Set<int> setws;
		int iws;
		for (iws = 0; iws < vwsAnal.Size(); ++iws)
			setws.Insert(vwsAnal[iws]);
		for (iws = 0; iws < vwsVern.Size(); ++iws)
			setws.Insert(vwsVern[iws]);
		Set<int>::iterator it;
		for (it = setws.Begin(), iws = 0; it != setws.End(); ++it, iws++)
			vws.Push(it->GetValue());

		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(pafw);
		AssertPtr(prmw);
		hvoRoot = prmw->GetRootObj();
	}
	// get the log file stream
	IStreamPtr qstrm;
	CheckHr(AfApp::Papp()->GetLogPointer(&qstrm));

	CheckHr(qfwst->put_DlgType(ksdtStandard));
	CheckHr(qfwst->put_ShowAll(false));
	CheckHr(qfwst->put_SysMsrUnit(AfApp::Papp()->GetMsrSys()));
	CheckHr(qfwst->put_UserWs(wsUser));
	CheckHr(qfwst->put_HelpFile(stuHelpFile.Bstr()));
	CheckHr(qfwst->putref_WritingSystemFactory(qwsf));
	CheckHr(qfwst->put_ParentHwnd(reinterpret_cast<DWORD>(hwnd)));
	CheckHr(qfwst->put_CanDoRtl(fCanDoRtl));
	CheckHr(qfwst->put_OuterRtl(fOuterRtl));
	CheckHr(qfwst->put_FontFeatures(false));
	CheckHr(qfwst->putref_Stylesheet(past));
	CheckHr(qfwst->put_CanFormatChar(fCanFormatChar));
	CheckHr(qfwst->put_OnlyCharStyles(true));
	CheckHr(qfwst->put_StyleName(pstuStyleName->Bstr()));
	CheckHr(qfwst->SetTextProps(
		reinterpret_cast<ITsTextProps **>(vqttpPara.Begin()), vqttpPara.Size(),
		reinterpret_cast<ITsTextProps **>(vqttpChar.Begin()), vqttpChar.Size()));
	CheckHr(qfwst->put_RootObjectId(hvoRoot));
	CheckHr(qfwst->SetWritingSystemsOfInterest(vws.Begin(), vws.Size()));
	CheckHr(qfwst->putref_LogFile(qstrm));
	CLSID clsidApp = GUID_NULL;
	if (AfApp::Papp())
		clsidApp = *AfApp::Papp()->GetAppClsid();
	CheckHr(qfwst->put_AppClsid(clsidApp));
	IHelpTopicProviderPtr qhtprov = new HelpTopicProvider(AfApp::Papp()->GetHelpBaseName());
	CheckHr(qfwst->putref_HelpTopicProvider(qhtprov));

	CheckHr(qfwst->ShowModal(&nResult));
	CheckHr(qfwst->GetResults(&sbstrStyleName, &fStylesChangedT, &fApplyT,
		&fReloadDbT, &fResultT));

	pstuStyleName->Assign(sbstrStyleName.Chars());
	fStylesChanged = bool(fStylesChangedT);
	fApply = bool(fApplyT);
	fReloadDb = bool(fReloadDbT);
	return bool(fResultT);
}




/*----------------------------------------------------------------------------------------------
	Called when the user clicks the Modify Styles button.

	@param pnmh Windows command that is being passed.
	@param lnRet Out Return value that will be returned to windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool ModFldSetDlg::OnModifyStyles(NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	ISilDataAccessPtr qsda;
	VwPropsVec vqvpsPara;
	TtpVec vqttpPara;
	TtpVec vqttpChar;
	VwPropsVec vqvpsChar;
	IVwStylesheetPtr qasts;
	AfMainWnd * pafw = MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	qasts = plpi->GetAfStylesheet();

	// Variables set by AdjustTsTextProps.
	StrUni stuStyleToApply = L""; // If AdjustTsTextProps sets this, apply it to the selection.
	bool fDefnChanged = false;	// If AdjustTsTextProps makes this true, ask the main windows
								// to redraw.
	ComBool fCanFormatChar = true;
	bool fApply;
	bool fReloadDb;

	// get the current style
	HWND hwndCharSty = ::GetDlgItem(m_hwnd, kcidTlsOptCharSty);
	if (!hwndCharSty)
		hwndCharSty = ::GetDlgItem(m_hwnd, kcidTlsOptCharStyForText);
	Assert(hwndCharSty);

	int ics = ::SendMessage(hwndCharSty, CB_GETCURSEL , 0, 0);
	achar rgch[MAX_PATH];
	Vector<achar> vch;
	achar * pszT;
	int cch = ::SendMessage(hwndCharSty, CB_GETLBTEXTLEN, ics, (LPARAM)0);
	if (cch < MAX_PATH)
	{
		pszT = rgch;
	}
	else
	{
		vch.Resize(cch + 1);
		pszT = vch.Begin();
	}
	cch = ::SendMessage(hwndCharSty, CB_GETLBTEXT, ics, (LPARAM)pszT);
	if (cch < 0)
		pszT = _T("");
	stuStyleToApply = pszT;

	if (OpenFormatStylesDialog(m_hwnd, false /*m_fCanDoRtl*/, false /*OuterRightToLeft()*/, qasts,
			vqttpPara, vqttpChar, fCanFormatChar,
			&stuStyleToApply, fDefnChanged, fApply, fReloadDb))
	{
		// If OpenFormatStylesDialog returns true, than something changed.
		SetCharStylesCombo(); // Load new values in case they changed.
		int icombobox = ::SendMessage(hwndCharSty, CB_FINDSTRING, (WPARAM)-1,
			(LPARAM)stuStyleToApply.Chars());
		::SendMessage(hwndCharSty, CB_SETCURSEL, icombobox, 0);
		ChangeCharSty();
	}
	if (fReloadDb)
	{
		if (AfApp::Papp())
			AfApp::Papp()->OnStyleNameChange(NULL, NULL);
	}
	lnRet = 0;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Sets the visibity setting

	@param nvis The new field Visibility setting.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlg::SetVis(FldVis nvis)
{
	// set disable flag to prevent endless loop
	m_fDisableEnChange = true;

	// Set the new type.
	Assert(kFTVisAlways == nvis || kFTVisIfData == nvis || kFTVisNever == nvis);

	m_nVis = nvis;

	// Determine current status.
	bool fAlways = (kFTVisAlways == m_nVis);
	bool fIfData = (kFTVisIfData == m_nVis);
	bool fNever = (kFTVisNever == m_nVis);

	// Set the correct radio button and clear the others.
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAVis), BM_SETCHECK,
		fAlways ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModIDVis), BM_SETCHECK,
		fIfData ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModNVis), BM_SETCHECK,
		fNever ? BST_CHECKED : BST_UNCHECKED, 0);

	m_fDisableEnChange = false;
}


/*----------------------------------------------------------------------------------------------
	Moves a control whose id is "cid" up by the amount of the offset.

	@param cid ID of control to be moved.
	@param offset How far to move it.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlg::MoveCtrl(int cid, int offset)
{
		RECT rc;
		HWND hwndCtrl = ::GetDlgItem(m_hwnd, cid);
		::GetWindowRect(hwndCtrl, &rc);
		rc.top = rc.top - offset;
		rc.bottom = rc.bottom - offset;
		::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);
		::MoveWindow(hwndCtrl, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top, true);
}


/*----------------------------------------------------------------------------------------------
	Handle a change in a combo box.

	@param pnmh Windows command that is being passed.
	@param lnRet Out Return value that will be returned to windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool ModFldSetDlg::OnChangeCharSty(NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	Assert(pnmh->idFrom == kcidTlsOptCharSty || pnmh->idFrom == kcidTlsOptCharStyForText);
//	if (pnmh->idFrom != kcidTlsOptCharSty)
//		return false;
	ChangeCharSty();
	lnRet = 0;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle a change in a combo box.

	@param pnmh Windows command that is being passed.
	@param lnRet Out Return value that will be returned to windows.
	@return true
----------------------------------------------------------------------------------------------*/
void ModFldSetDlg::ChangeCharSty()
{
	HWND hwndCharSty = ::GetDlgItem(m_hwnd, kcidTlsOptCharSty);
	if (!hwndCharSty)
		hwndCharSty = ::GetDlgItem(m_hwnd, kcidTlsOptCharStyForText);
	Assert(hwndCharSty);

	// Get the current index from the combo box.
	int icb;
	icb = ::SendMessage(hwndCharSty, CB_GETCURSEL, 0, 0);
	SmartBstr sbstrSty;
	achar rgch[MAX_PATH];
	Vector<achar> vch;
	achar * pszT;
	int cch = ::SendMessage(hwndCharSty, CB_GETLBTEXTLEN, icb, (LPARAM)0);
	if (cch < MAX_PATH)
	{
		pszT = rgch;
	}
	else
	{
		vch.Resize(cch + 1);
		pszT = vch.Begin();
	}
	::SendMessage(hwndCharSty, CB_GETLBTEXT, icb, (LPARAM)pszT);
	StrUniBuf stub = pszT;

	StrUniBuf stubNone(kstidTlsOptVwStNone);
	if (stub.Equals (stubNone))
	{
		m_stuSty.Assign("");
		::SendMessage(hwndCharSty, CB_SETCURSEL, (WPARAM)-1, 0);
		return;
	}

	sbstrSty.Assign(stub.Chars(), stub.Length());

	// Find the corresponding StStyle id and save it.
	AfMainWnd * pafw = MainWindow();
	AssertObj(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	CustViewDaPtr qcvd;
	plpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	HVO hvo = plpi->GetLpId();
	int chvoSty;
	CheckHr(qcvd->get_VecSize(hvo, kflidLangProject_Styles, &chvoSty));
	for (int ihvo = 0; ihvo < chvoSty; ++ihvo)
	{
		HVO hvoSty;
		int nType;
		SmartBstr sbstr;
		StrAppBuf strb;
		CheckHr(qcvd->get_VecItem(hvo, kflidLangProject_Styles, ihvo, &hvoSty));
		CheckHr(qcvd->get_IntProp(hvoSty, kflidStStyle_Type, &nType));
		if (nType != kstCharacter)
			continue;
		CheckHr(qcvd->get_UnicodeProp(hvoSty, kflidStStyle_Name, &sbstr));

		if (sbstr.Equals(sbstrSty))
		{
			// Save the new character style.
			m_stuSty = sbstrSty.Chars();
			break;
		}
	}
}


//:>********************************************************************************************
//:>ModBrFldSetDlgT methods. (Modify Field text visibility Dialog for Browse View)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModBrFldSetDlgT::ModBrFldSetDlgT(void)
{
	m_rid = kridModBrFldSetDlgT;
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param vis Visibility of the field.
	@param vwt Type of view
	@param pszSty Text style
----------------------------------------------------------------------------------------------*/
void ModBrFldSetDlgT::SetDialogValues(UserViewType vwt, LPCOLESTR pszSty)
{
	m_vwt = vwt;
	m_stuSty = pszSty;
}


/*----------------------------------------------------------------------------------------------
	Gets the values from the dialog.

	@param pvis Out Visibility of the field.
	@param stuSty Text style
----------------------------------------------------------------------------------------------*/
void ModBrFldSetDlgT::GetDialogValues(StrUni & stuSty)
{
	stuSty = m_stuSty;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModBrFldSetDlgT::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	return ModFldSetDlg::OnInitDlg(hwndCtrl, lp);
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
bool ModBrFldSetDlgT::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fDisableEnChange == false)
	{
		switch (pnmh->code)
		{
		case BN_CLICKED:
			switch (pnmh->idFrom)
			{
			case kcidTlsOptBrModSty:
				return OnModifyStyles(pnmh, lnRet);
				break;
			}
		case CBN_SELCHANGE: // Combo box item changed.
			switch (pnmh->idFrom)
			{
			case kcidTlsOptCharStyForText:
				return OnChangeCharSty(pnmh, lnRet);
				break;
			}
		}
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}


//:>********************************************************************************************
//:>ModFldSetDlgT methods. (Modify Field text visibility Dialog)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModFldSetDlgT::ModFldSetDlgT(void)
{
	m_rid = kridModFldSetDlgT;
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param vis Visibility of the field.
	@param vwt Type of view
	@param pszSty Text style
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgT::SetDialogValues(FldVis vis, UserViewType vwt, LPCOLESTR pszSty)
{
	Assert((uint)vis < (uint)kFTVisLim);

	m_nVis = vis;
	m_vwt = vwt;
	m_stuSty = pszSty;
}


/*----------------------------------------------------------------------------------------------
	Gets the values from the dialog.

	@param pvis Out Visibility of the field.
	@param stuSty Text style
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgT::GetDialogValues(FldVis * pvis, StrUni & stuSty)
{
	AssertPtr(pvis);

	*pvis = m_nVis;
	stuSty = m_stuSty;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModFldSetDlgT::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	return ModFldSetDlg::OnInitDlg(hwndCtrl, lp);
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
bool ModFldSetDlgT::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fDisableEnChange == false)
	{
		switch (pnmh->code)
		{
		case BN_CLICKED:
			switch (pnmh->idFrom)
			{
			case kcidTlsOptModSty:
				return OnModifyStyles(pnmh, lnRet);
				break;
			case kcidTlsOptDlgModAVis:
				SetVis(kFTVisAlways);
				break;
			case kcidTlsOptDlgModIDVis:
				SetVis(kFTVisIfData);
				break;
			case kcidTlsOptDlgModNVis:
				SetVis(kFTVisNever);
				break;
			}
		case CBN_SELCHANGE: // Combo box item changed.
			switch (pnmh->idFrom)
			{
			case kcidTlsOptCharStyForText:
				return OnChangeCharSty(pnmh, lnRet);
				break;
			}
		}
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}


//:>********************************************************************************************
//:>	ModFldSetDlgG methods. (Modify Field group visibility Dialog)
//:>
//:>	Note: this class is obsolete, since we no longer allow them to modify settings on
//:>	groups.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModFldSetDlgG::ModFldSetDlgG(void)
{
	m_rid = kridModFldSetDlgGrp;
}


/*----------------------------------------------------------------------------------------------
	Gets the values from the dialog.

	@param pvis Out Visibility of the field.
	@param stuSty Text style
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgG::GetDialogValues(FldVis * pvis)
{
	AssertPtr(pvis);

	*pvis = m_nVis;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModFldSetDlgG::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_pszHelpUrl = _T("User_Interface/Menus/Tools/Options/Options_Views_tab.htm");
//	return ModFldSetDlg::OnInitDlg(hwndCtrl, lp);
	SetVis(kFTVisIfData);
	m_nVis = kFTVisIfData;
	return true;
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
bool ModFldSetDlgG::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fDisableEnChange == false)
	{
		switch (pnmh->code)
		{
		case BN_CLICKED:
			switch (pnmh->idFrom)
			{
			case kcidTlsOptDlgModIDVis:
				SetVis(kFTVisIfData);
				break;
			case kcidTlsOptDlgModNVis:
				SetVis(kFTVisNever);
				break;
			}
		}
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}


//:>********************************************************************************************
//:>ModBrFldSetDlgCL methods. (Modify Field Choices List Dialog for Browse View)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModBrFldSetDlgCL::ModBrFldSetDlgCL(void)
{
	m_rid = kridModBrFldSetDlgCL;
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param vwt Type of view
	@param pszSty Text style
	@param pnt PossChsr Name type (kpntName, kpntNameAndAbbrev,	kpntAbbreviation)
	@param fHier Flag that tells whether to show hierarchy or not.
	@param fVert Flag that tells whether to List items in this field vertically or not.
----------------------------------------------------------------------------------------------*/
void ModBrFldSetDlgCL::SetDialogValues(UserViewType vwt, LPCOLESTR pszSty,
	PossNameType pnt, bool fHier, bool fVert)
{
	Assert((uint)pnt < (uint)kpntLim);

	m_vwt = vwt;
	m_stuSty = pszSty;
	m_fShowNa = pnt != kpntAbbreviation;
	m_fShowAb = pnt != kpntName;
	m_fShowHi = fHier;
	m_fListI = fVert;
}


/*----------------------------------------------------------------------------------------------
	Gets the values from the dialog.

	@param stuSty Text style
	@param ppnt PossChsr Name type (kpntName, kpntNameAndAbbrev,	kpntAbbreviation)
	@param pfHier Flag that tells whether to show hierarchy or not.
	@param pfVert Flag that tells whether to List items in this field vertically or not.
----------------------------------------------------------------------------------------------*/
void ModBrFldSetDlgCL::GetDialogValues(StrUni & stuSty, PossNameType * ppnt,
	bool * pfHier, bool * pfVert)
{
	AssertPtr(ppnt);
	AssertPtr(pfHier);
	AssertPtr(pfVert);

	stuSty = m_stuSty;
	if (m_fShowNa)
	{
		if (m_fShowAb)
			*ppnt = kpntNameAndAbbrev;
		else
			*ppnt = kpntName;
	}
	else
		*ppnt = kpntAbbreviation;
	*pfHier = m_fShowHi;
	*pfVert = m_fListI;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModBrFldSetDlgCL::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Preset the buttons.
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSAb), BM_SETCHECK,
		m_fShowAb ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSNa), BM_SETCHECK,
		m_fShowNa ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSHi), BM_SETCHECK,
		m_fShowHi ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSLI), BM_SETCHECK,
		m_fShowHi ? BST_CHECKED : BST_UNCHECKED, 0);
	// Disable until we support listing fields vertically.
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSLI), 0);

	UpdatePrev();
	return ModFldSetDlg::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Updates the contents of the preview box with dummy data.
----------------------------------------------------------------------------------------------*/
void ModBrFldSetDlgCL::UpdatePrev()
{
	StrApp str;
	StrApp strAN;
	StrApp strH;
	HWND hwndPrev = ::GetDlgItem(m_hwnd, kcidTlsOptDlgModPrvw);
	SendMessage(hwndPrev, LB_RESETCONTENT , 0, 0);
	if (m_fShowHi)
	{
		if (m_fShowAb && !m_fShowNa)
			strH.Load(kstridTlsOptModHeirA);
		if (!m_fShowAb && m_fShowNa)
			strH.Load(kstridTlsOptModHeirN);
		if (m_fShowAb && m_fShowNa)
			strH.Load(kstridTlsOptModHeirAN);
	}
	else
		strH = _T("");

	if (m_fShowAb && !m_fShowNa)
	{
		strAN.Load(kstridTlsOptModAbr1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbr2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbr3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
	if (!m_fShowAb && m_fShowNa)
	{
		strAN.Load(kstridTlsOptModNam1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModNam2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModNam3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
	if (m_fShowAb && m_fShowNa)
	{
		strAN.Load(kstridTlsOptModAbrNam1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbrNam2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbrNam3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
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
bool ModBrFldSetDlgCL::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fDisableEnChange == false)
	{
		switch (pnmh->code)
		{
		case BN_CLICKED:
			switch (pnmh->idFrom)
			{
			case kcidTlsOptModSty:
				return OnModifyStyles(pnmh, lnRet);
				break;
			case kcidTlsOptDlgModSAb:
				// We can't allow both name and abbreviation to be turned off.
				m_fShowAb = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSAb) == BST_CHECKED);
				if (!m_fShowAb)
					::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSNa), BM_SETCHECK, 1, 0);
				m_fShowNa = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSNa) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSNa:
				// We can't allow both name and abbreviation to be turned off.
				m_fShowNa = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSNa) == BST_CHECKED);
				if (!m_fShowNa)
					::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSAb), BM_SETCHECK, 1, 0);
				m_fShowAb = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSAb) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSHi:
				m_fShowHi = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSHi) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSLI:
				m_fListI = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSLI) == BST_CHECKED);
				break;
			}
		case CBN_SELCHANGE: // Combo box item changed.
			switch (pnmh->idFrom)
			{
			case kcidTlsOptCharSty:
				return OnChangeCharSty(pnmh, lnRet);
				break;
			}
		}
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}



//:>********************************************************************************************
//:>ModFldSetDlgCL methods. (Modify Field Choices List visibility Dialog)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModFldSetDlgCL::ModFldSetDlgCL(void)
{
	m_rid = kridModFldSetDlgCL;
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param vis Visibility of the field.
	@param vwt Type of view
	@param pszSty Text style
	@param pnt PossChsr Name type (kpntName, kpntNameAndAbbrev,	kpntAbbreviation)
	@param fHier Flag that tells whether to show hierarchy or not.
	@param fVert Flag that tells whether to List items in this field vertically or not.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgCL::SetDialogValues(FldVis vis, UserViewType vwt, LPCOLESTR pszSty,
	PossNameType pnt, bool fHier, bool fVert)
{
	Assert((uint)vis < (uint)kFTVisLim);
	Assert((uint)pnt < (uint)kpntLim);

	m_nVis = vis;
	m_vwt = vwt;
	m_stuSty = pszSty;
	m_fShowNa = pnt != kpntAbbreviation;
	m_fShowAb = pnt != kpntName;
	m_fShowHi = fHier;
	m_fListI = fVert;
}


/*----------------------------------------------------------------------------------------------
	Gets the values from the dialog.

	@param pvis Out Visibility of the field.
	@param stuSty Text style
	@param ppnt PossChsr Name type (kpntName, kpntNameAndAbbrev,	kpntAbbreviation)
	@param pfHier Flag that tells whether to show hierarchy or not.
	@param pfVert Flag that tells whether to List items in this field vertically or not.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgCL::GetDialogValues(FldVis * pvis, StrUni & stuSty, PossNameType * ppnt,
	bool * pfHier, bool * pfVert)
{
	AssertPtr(pvis);
	AssertPtr(ppnt);
	AssertPtr(pfHier);
	AssertPtr(pfVert);

	*pvis = m_nVis;
	stuSty = m_stuSty;
	if (m_fShowNa)
	{
		if (m_fShowAb)
			*ppnt = kpntNameAndAbbrev;
		else
			*ppnt = kpntName;
	}
	else
		*ppnt = kpntAbbreviation;
	*pfHier = m_fShowHi;
	*pfVert = m_fListI;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModFldSetDlgCL::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Preset the buttons.
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSAb), BM_SETCHECK,
		m_fShowAb ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSNa), BM_SETCHECK,
		m_fShowNa ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSHi), BM_SETCHECK,
		m_fShowHi ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSLI), BM_SETCHECK,
		m_fShowHi ? BST_CHECKED : BST_UNCHECKED, 0);
	// Disable until we support listing fields vertically.
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSLI), 0);

	UpdatePrev();
	return ModFldSetDlg::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Updates the contents of the preview box with dummy data.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgCL::UpdatePrev()
{
	StrApp str;
	StrApp strAN;
	StrApp strH;
	HWND hwndPrev = ::GetDlgItem(m_hwnd, kcidTlsOptDlgModPrvw);
	SendMessage(hwndPrev, LB_RESETCONTENT , 0, 0);
	if (m_fShowHi)
	{
		if (m_fShowAb && !m_fShowNa)
			strH.Load(kstridTlsOptModHeirA);
		if (!m_fShowAb && m_fShowNa)
			strH.Load(kstridTlsOptModHeirN);
		if (m_fShowAb && m_fShowNa)
			strH.Load(kstridTlsOptModHeirAN);
	}
	else
		strH = _T("");

	if (m_fShowAb && !m_fShowNa)
	{
		strAN.Load(kstridTlsOptModAbr1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbr2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbr3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
	if (!m_fShowAb && m_fShowNa)
	{
		strAN.Load(kstridTlsOptModNam1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModNam2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModNam3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
	if (m_fShowAb && m_fShowNa)
	{
		strAN.Load(kstridTlsOptModAbrNam1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbrNam2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbrNam3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
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
bool ModFldSetDlgCL::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fDisableEnChange == false)
	{
		switch (pnmh->code)
		{
		case BN_CLICKED:
			switch (pnmh->idFrom)
			{
			case kcidTlsOptModSty:
				return OnModifyStyles(pnmh, lnRet);
				break;
			case kcidTlsOptDlgModAVis:
				SetVis(kFTVisAlways);
				break;
			case kcidTlsOptDlgModIDVis:
				SetVis(kFTVisIfData);
				break;
			case kcidTlsOptDlgModNVis:
				SetVis(kFTVisNever);
				break;
			case kcidTlsOptDlgModSAb:
				// We can't allow both name and abbreviation to be turned off.
				m_fShowAb = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSAb) == BST_CHECKED);
				if (!m_fShowAb)
					::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSNa), BM_SETCHECK, 1, 0);
				m_fShowNa = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSNa) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSNa:
				// We can't allow both name and abbreviation to be turned off.
				m_fShowNa = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSNa) == BST_CHECKED);
				if (!m_fShowNa)
					::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSAb), BM_SETCHECK, 1, 0);
				m_fShowAb = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSAb) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSHi:
				m_fShowHi = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSHi) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSLI:
				m_fListI = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSLI) == BST_CHECKED);
				break;
			}
		case CBN_SELCHANGE: // Combo box item changed.
			switch (pnmh->idFrom)
			{
			case kcidTlsOptCharSty:
				return OnChangeCharSty(pnmh, lnRet);
				break;
			}
		}
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}


//:>********************************************************************************************
//:>ModBrFldSetDlgExp methods. (Modify Field Choices List visibility Dialog)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModBrFldSetDlgExp::ModBrFldSetDlgExp(void)
{
	m_rid = kridModBrFldSetDlgExp;
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param vwt Type of view
	@param pszSty Text style
	@param pnt PossChsr Name type (kpntName, kpntNameAndAbbrev,	kpntAbbreviation)
	@param fHier Flag that tells whether to show hierarchy or not.
	@param fVert Flag that tells whether to List items in this field vertically or not.
----------------------------------------------------------------------------------------------*/
void ModBrFldSetDlgExp::SetDialogValues(UserViewType vwt, LPCOLESTR pszSty,
	PossNameType pnt, bool fHier, bool fVert, bool fExpand, bool fIsDocVw)
{
	Assert((uint)pnt < (uint)kpntLim);

	// from ModFldSetDlgCL
	m_vwt = vwt;
	m_stuSty = pszSty;
	m_fShowNa = pnt != kpntAbbreviation;
	m_fShowAb = pnt != kpntName;
	m_fShowHi = fHier;
	m_fListI = fVert;

	// from ModFldSetDlgHi
	m_fAlEx = fExpand;
	m_fIsDocVw = fIsDocVw;
}


/*----------------------------------------------------------------------------------------------
	Gets the values from the dialog.

	@param stuSty Text style
	@param ppnt PossChsr Name type (kpntName, kpntNameAndAbbrev,	kpntAbbreviation)
	@param pfHier Flag that tells whether to show hierarchy or not.
	@param pfVert Flag that tells whether to List items in this field vertically or not.
----------------------------------------------------------------------------------------------*/
void ModBrFldSetDlgExp::GetDialogValues(StrUni & stuSty, PossNameType * ppnt,
	bool * pfHier, bool * pfVert, bool * pfExpand)
{
	AssertPtr(ppnt);
	AssertPtr(pfHier);
	AssertPtr(pfVert);

	AssertPtr(pfExpand);

	stuSty = m_stuSty;
	if (m_fShowNa)
	{
		if (m_fShowAb)
			*ppnt = kpntNameAndAbbrev;
		else
			*ppnt = kpntName;
	}
	else
		*ppnt = kpntAbbreviation;
	*pfHier = m_fShowHi;
	*pfVert = m_fListI;

	*pfExpand = m_fAlEx;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModBrFldSetDlgExp::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
//	if (m_fIsDocVw)
//	{
		::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAEx), BM_SETCHECK, BST_CHECKED, 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAEx), false);
//	} else {
//		::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAEx), BM_SETCHECK,
//			m_fAlEx ? BST_CHECKED : BST_UNCHECKED, 0);
//	}

	// Preset the buttons.
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSAb), BM_SETCHECK,
		m_fShowAb ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSNa), BM_SETCHECK,
		m_fShowNa ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSHi), BM_SETCHECK,
		m_fShowHi ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSLI), BM_SETCHECK,
		m_fShowHi ? BST_CHECKED : BST_UNCHECKED, 0);
	// Disable until we support listing fields vertically.
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSLI), 0);

	UpdatePrev();
	return ModFldSetDlg::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Updates the contents of the preview box with dummy data.
----------------------------------------------------------------------------------------------*/
void ModBrFldSetDlgExp::UpdatePrev()
{
	StrApp str;
	StrApp strAN;
	StrApp strH;
	HWND hwndPrev = ::GetDlgItem(m_hwnd, kcidTlsOptDlgModPrvw);
	SendMessage(hwndPrev, LB_RESETCONTENT , 0, 0);
	if (m_fShowHi)
	{
		if (m_fShowAb && !m_fShowNa)
			strH.Load(kstridTlsOptModHeirA);
		if (!m_fShowAb && m_fShowNa)
			strH.Load(kstridTlsOptModHeirN);
		if (m_fShowAb && m_fShowNa)
			strH.Load(kstridTlsOptModHeirAN);
	}
	else
		strH = _T("");

	if (m_fShowAb && !m_fShowNa)
	{
		strAN.Load(kstridTlsOptModAbr1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbr2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbr3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
	if (!m_fShowAb && m_fShowNa)
	{
		strAN.Load(kstridTlsOptModNam1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModNam2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModNam3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
	if (m_fShowAb && m_fShowNa)
	{
		strAN.Load(kstridTlsOptModAbrNam1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbrNam2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbrNam3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
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
bool ModBrFldSetDlgExp::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fDisableEnChange == false)
	{
		switch (pnmh->code)
		{
		case BN_CLICKED:
			switch (pnmh->idFrom)
			{
			case kcidTlsOptModSty:
				return OnModifyStyles(pnmh, lnRet);
				break;
			case kcidTlsOptDlgModAEx:
				m_fAlEx = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModAEx) == BST_CHECKED);
				break;
			case kcidTlsOptDlgModSAb:
				// We can't allow both name and abbreviation to be turned off.
				m_fShowAb = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSAb) == BST_CHECKED);
				if (!m_fShowAb)
					::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSNa), BM_SETCHECK, 1, 0);
				m_fShowNa = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSNa) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSNa:
				// We can't allow both name and abbreviation to be turned off.
				m_fShowNa = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSNa) == BST_CHECKED);
				if (!m_fShowNa)
					::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSAb), BM_SETCHECK, 1, 0);
				m_fShowAb = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSAb) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSHi:
				m_fShowHi = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSHi) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSLI:
				m_fListI = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSLI) == BST_CHECKED);
				break;
			}
		case CBN_SELCHANGE: // Combo box item changed.
			switch (pnmh->idFrom)
			{
			case kcidTlsOptCharSty:
				return OnChangeCharSty(pnmh, lnRet);
				break;
			}
		}
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}

//:>********************************************************************************************
//:>ModFldSetDlgExp methods. (Modify Field Choices List visibility Dialog)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModFldSetDlgExp::ModFldSetDlgExp(void)
{
	m_rid = kridModFldSetDlgExp;
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param vis Visibility of the field.
	@param vwt Type of view
	@param pszSty Text style
	@param pnt PossChsr Name type (kpntName, kpntNameAndAbbrev,	kpntAbbreviation)
	@param fHier Flag that tells whether to show hierarchy or not.
	@param fVert Flag that tells whether to List items in this field vertically or not.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgExp::SetDialogValues(FldVis vis, UserViewType vwt, LPCOLESTR pszSty,
	PossNameType pnt, bool fHier, bool fVert, bool fExpand, bool fIsDocVw)
{
	Assert((uint)vis < (uint)kFTVisLim);
	Assert((uint)pnt < (uint)kpntLim);

	// from ModFldSetDlgCL
	m_nVis = vis;
	m_vwt = vwt;
	m_stuSty = pszSty;
	m_fShowNa = pnt != kpntAbbreviation;
	m_fShowAb = pnt != kpntName;
	m_fShowHi = fHier;
	m_fListI = fVert;

	// from ModFldSetDlgHi
	m_fAlEx = fExpand;
	m_fIsDocVw = fIsDocVw;
}


/*----------------------------------------------------------------------------------------------
	Gets the values from the dialog.

	@param pvis Out Visibility of the field.
	@param stuSty Text style
	@param ppnt PossChsr Name type (kpntName, kpntNameAndAbbrev,	kpntAbbreviation)
	@param pfHier Flag that tells whether to show hierarchy or not.
	@param pfVert Flag that tells whether to List items in this field vertically or not.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgExp::GetDialogValues(FldVis * pvis, StrUni & stuSty, PossNameType * ppnt,
	bool * pfHier, bool * pfVert, bool * pfExpand)
{
	AssertPtr(pvis);
	AssertPtr(ppnt);
	AssertPtr(pfHier);
	AssertPtr(pfVert);

	AssertPtr(pfExpand);

	*pvis = m_nVis;
	stuSty = m_stuSty;
	if (m_fShowNa)
	{
		if (m_fShowAb)
			*ppnt = kpntNameAndAbbrev;
		else
			*ppnt = kpntName;
	}
	else
		*ppnt = kpntAbbreviation;
	*pfHier = m_fShowHi;
	*pfVert = m_fListI;

	*pfExpand = m_fAlEx;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModFldSetDlgExp::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	if (m_fIsDocVw)
	{
		::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAEx), BM_SETCHECK, BST_CHECKED, 0);
	} else {
		::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAEx), BM_SETCHECK,
			m_fAlEx ? BST_CHECKED : BST_UNCHECKED, 0);
	}
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAEx), !m_fIsDocVw);

	// Preset the buttons.
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSAb), BM_SETCHECK,
		m_fShowAb ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSNa), BM_SETCHECK,
		m_fShowNa ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSHi), BM_SETCHECK,
		m_fShowHi ? BST_CHECKED : BST_UNCHECKED, 0);
	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSLI), BM_SETCHECK,
		m_fShowHi ? BST_CHECKED : BST_UNCHECKED, 0);
	// Disable until we support listing fields vertically.
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSLI), 0);

	UpdatePrev();
	return ModFldSetDlg::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Updates the contents of the preview box with dummy data.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgExp::UpdatePrev()
{
	StrApp str;
	StrApp strAN;
	StrApp strH;
	HWND hwndPrev = ::GetDlgItem(m_hwnd, kcidTlsOptDlgModPrvw);
	SendMessage(hwndPrev, LB_RESETCONTENT , 0, 0);
	if (m_fShowHi)
	{
		if (m_fShowAb && !m_fShowNa)
			strH.Load(kstridTlsOptModHeirA);
		if (!m_fShowAb && m_fShowNa)
			strH.Load(kstridTlsOptModHeirN);
		if (m_fShowAb && m_fShowNa)
			strH.Load(kstridTlsOptModHeirAN);
	}
	else
		strH = _T("");

	if (m_fShowAb && !m_fShowNa)
	{
		strAN.Load(kstridTlsOptModAbr1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbr2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbr3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
	if (!m_fShowAb && m_fShowNa)
	{
		strAN.Load(kstridTlsOptModNam1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModNam2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModNam3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
	if (m_fShowAb && m_fShowNa)
	{
		strAN.Load(kstridTlsOptModAbrNam1);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbrNam2);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());

		strAN.Load(kstridTlsOptModAbrNam3);
		str.Format(_T("%s%s"), strH.Chars(), strAN.Chars());
		SendMessage(hwndPrev, LB_ADDSTRING, 0, (LPARAM)str.Chars());
	}
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
bool ModFldSetDlgExp::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fDisableEnChange == false)
	{
		switch (pnmh->code)
		{
		case BN_CLICKED:
			switch (pnmh->idFrom)
			{
			case kcidTlsOptModSty:
				return OnModifyStyles(pnmh, lnRet);
				break;
			case kcidTlsOptDlgModAVis:
				SetVis(kFTVisAlways);
				break;
			case kcidTlsOptDlgModIDVis:
				SetVis(kFTVisIfData);
				break;
			case kcidTlsOptDlgModNVis:
				SetVis(kFTVisNever);
				break;
			case kcidTlsOptDlgModAEx:
				::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAEx), BM_SETCHECK, 1, 0);
				m_fAlEx = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModAEx) == BST_CHECKED);
				break;
			case kcidTlsOptDlgModSAb:
				// We can't allow both name and abbreviation to be turned off.
				m_fShowAb = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSAb) == BST_CHECKED);
				if (!m_fShowAb)
					::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSNa), BM_SETCHECK, 1, 0);
				m_fShowNa = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSNa) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSNa:
				// We can't allow both name and abbreviation to be turned off.
				m_fShowNa = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSNa) == BST_CHECKED);
				if (!m_fShowNa)
					::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModSAb), BM_SETCHECK, 1, 0);
				m_fShowAb = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSAb) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSHi:
				m_fShowHi = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSHi) == BST_CHECKED);
				UpdatePrev();
				break;
			case kcidTlsOptDlgModSLI:
				m_fListI = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModSLI) == BST_CHECKED);
				break;
			}
		case CBN_SELCHANGE: // Combo box item changed.
			switch (pnmh->idFrom)
			{
			case kcidTlsOptCharSty:
				return OnChangeCharSty(pnmh, lnRet);
				break;
			}
		}
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}

//:>********************************************************************************************
//:>ModFldSetDlgHi methods. (Modify Field Hierarchical visibility Dialog)
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ModFldSetDlgHi::ModFldSetDlgHi(void)
{
	m_rid = kridModFldSetDlgHi;
}


/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.

	@param vis Out Visibility of the field.
	@param vwt UserView Type (Document, Data Entry, ets.)
	@param pszSty Text style
	@param ons Outline number style
	@param fExpand Flag to Expand the list.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgHi::SetDialogValues(FldVis vis, UserViewType vwt, LPCOLESTR pszSty,
	OutlineNumSty ons, bool fExpand, bool fIsDocVw)
{
	Assert((uint)vis < (uint)kFTVisLim);
	Assert((uint)ons < (uint)konsLim);

	m_nVis = vis;
	m_vwt = vwt;
	m_stuSty = pszSty;
	m_ons = ons;
	m_fNS = ons != konsNone;
	m_fAlEx = fExpand;
	m_fIsDocVw = fIsDocVw;
}


/*----------------------------------------------------------------------------------------------
	Gets the values from the dialog.

	@param pvis Out Visibility of the field.
	@param stuSty Text style
	@param pons Outline number style
	@param pfExpand Flag to Expand the list.
----------------------------------------------------------------------------------------------*/
void ModFldSetDlgHi::GetDialogValues(FldVis * pvis, StrUni & stuSty, OutlineNumSty * pons,
	bool * pfExpand)
{
	AssertPtr(pvis);
	AssertPtr(pons);
	AssertPtr(pfExpand);

	*pvis = m_nVis;
	stuSty = m_stuSty;
	*pfExpand = m_fAlEx;
	*pons = m_fNS ? m_ons : konsNone;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ModFldSetDlgHi::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgModNSC), m_fNS);
	if (m_fIsDocVw)
	{
		::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAEx), BM_SETCHECK, BST_CHECKED, 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAEx), !m_fIsDocVw);
	}
	else
	{
		::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModAEx), BM_SETCHECK,
			m_fAlEx ? BST_CHECKED : BST_UNCHECKED, 0);
	}

	::SendMessage(::GetDlgItem(m_hwnd, kcidTlsOptDlgModNSt), BM_SETCHECK,
		m_fNS ? BST_CHECKED : BST_UNCHECKED, 0);

	// Load Numbering Style combo
	HWND hwndcbo = ::GetDlgItem(m_hwnd, kcidTlsOptDlgModNSC);
	::SendMessage(hwndcbo, CB_ADDSTRING, 0, (LPARAM)_T("1.1.1"));
	::SendMessage(hwndcbo, CB_ADDSTRING, 0, (LPARAM)_T("1.1.1."));
	::SendMessage(hwndcbo, CB_SETCURSEL, m_ons == konsNone ? 0 : (int)m_ons - 1, 0);

	return ModFldSetDlg::OnInitDlg(hwndCtrl, lp);
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
bool ModFldSetDlgHi::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (m_fDisableEnChange == false)
	{
		switch (pnmh->code)
		{
		case BN_CLICKED:
			switch (pnmh->idFrom)
			{
			case kcidTlsOptModSty:
				return OnModifyStyles(pnmh, lnRet);
				break;
			case kcidTlsOptDlgModAVis:
				SetVis(kFTVisAlways);
				break;
			case kcidTlsOptDlgModIDVis:
				SetVis(kFTVisIfData);
				break;
			case kcidTlsOptDlgModNVis:
				SetVis(kFTVisNever);
				break;
			case kcidTlsOptDlgModAEx:
				m_fAlEx = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModAEx) == BST_CHECKED);
				break;
			case kcidTlsOptDlgModNSt:
				{
				bool chk = (IsDlgButtonChecked(m_hwnd, kcidTlsOptDlgModNSt) == BST_CHECKED);
				m_fNS = chk;
				::EnableWindow(::GetDlgItem(m_hwnd, kcidTlsOptDlgModNSC), chk);
				if (chk && m_ons == konsNone)
					// Preset it to an on value so the change in m_fNS will persist.
					m_ons = konsNum;
				break;
				}
			}
		case CBN_SELCHANGE: // Combo box item changed.
			switch (pnmh->idFrom)
			{
			case kcidTlsOptDlgModNSC:
				{
					int icb;
					icb = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);
					m_ons = (OutlineNumSty)(icb + 1); // Offset for konsNone.
					break;
				}
			case kcidTlsOptCharSty:
				return OnChangeCharSty(pnmh, lnRet);
				break;
			}
		}
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}
