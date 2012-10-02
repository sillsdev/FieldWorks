/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeSt.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	A superclass for client windows that consist entirely of a single view.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


//:>********************************************************************************************
//:>	Constructor/Destructor methods.
//:>********************************************************************************************

AfDeFeSt::AfDeFeSt(int ws)
{
}

AfDeFeSt::~AfDeFeSt()
{
}

/*----------------------------------------------------------------------------------------------
	Finish initialization.
----------------------------------------------------------------------------------------------*/
void AfDeFeSt::Init(CustViewDa * pcvd, HVO hvoText)
{
	AssertPtr(pcvd);
	Assert(m_hvoObj); // Initialize should have been called prior to this.
	AssertPtr(m_qadsc);

	// Get the current default writing system for this field and convert any magic number.
	AfLpInfo * plpi = GetLpInfo();
	AssertPtr(plpi);
	m_qcvd = pcvd;
	m_hvoText = hvoText;
	ILgWritingSystemFactoryPtr qwsf;
	plpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	m_qstvc.Attach(NewObj StVc(m_qfsp->m_stuSty.Chars(), m_ws, m_chrp.clrBack, qwsf));
}


//:>********************************************************************************************
//:>	Utility methods
//:>********************************************************************************************

void AfDeFeSt::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb)
{
	AssertPtrN(pwsf);
	*pprootb = NULL;

	// Do we have a real text object? If not, make a fake one.
	int cpara = 0;
	ISilDataAccessPtr qsda = m_qcvd;
	if (m_hvoText)
		CheckHr(qsda->get_VecSize(m_hvoText, kflidStText_Paragraphs, &cpara));
	if (!cpara)
	{
		// We make a dummy text that is complete enough to edit, in a separate data access
		// object. On loss of focus, if this data access is dirty, we make corresponding
		// real objects.
		if (!m_hvoText)
			m_hvoText = -1;
		HVO hvoPara = -2;
		m_qvcdMissing.CreateInstance(CLSID_VwCacheDa);
		CheckHr(m_qvcdMissing->QueryInterface(IID_ISilDataAccess, (void **)&qsda));
		m_qvcdMissing->CacheVecProp(m_hvoText, kflidStText_Paragraphs, &hvoPara,1);
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		ITsStringPtr qtssMissing;
		CheckHr(qtsf->MakeStringRgch(L"", 0, m_ws, &qtssMissing));
		m_qvcdMissing->CacheStringProp(hvoPara, kflidStTxtPara_Contents, qtssMissing);
	}

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this)); // pass root site
	int frag = kfrText;
	IVwViewConstructor * pvvc = m_qstvc;
	if (pwsf)
		CheckHr(qsda->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qsda));
	CheckHr(qrootb->SetRootObjects(&m_hvoText, &pvvc, &frag,
		GetLpInfo()->GetAfStylesheet(), 1));
	*pprootb = qrootb;
	(*pprootb)->AddRef();

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (prmw)
		prmw->RegisterRootBox(qrootb);
}

//:>********************************************************************************************
//:>	Edit support overrides
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Check whether the content of this edit field has changed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeSt::IsDirty()
{
	if (!m_fDirty)
	{
		if (!SuperClass::IsDirty() && m_qvcdMissing)
		{
			ComBool fDirty;
			ISilDataAccessPtr qsda;
			CheckHr(m_qvcdMissing->QueryInterface(IID_ISilDataAccess, (void **)&qsda));
			qsda->IsDirty(&fDirty);
			if (fDirty)
				m_fDirty = true;
		}
	}
	return m_fDirty;
}


/*----------------------------------------------------------------------------------------------
	Save changes that have been made to the current editor.
	which should happen when we use File...Save, but for some reason we aren't getting there.
	There is a lot of overlap with EndEdit that should be eliminated.
----------------------------------------------------------------------------------------------*/
bool AfDeFeSt::SaveEdit()
{
	if (!SuperClass::SaveEdit())
		return false;
	SaveFullCursorInfo();
	if (m_qvcdMissing)
	{
		ComBool fDirty;
		ISilDataAccessPtr qsdaTemp;
		CheckHr(m_qvcdMissing->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
		qsdaTemp->IsDirty(&fDirty);
		if (!fDirty)
			return true;
		// The text got edited. So, make a real text and copy the relevant info.
		// First get the paragraph list
		HvoVec vhvoParas;
		int chvoParas;
		CheckHr(qsdaTemp->get_VecSize(m_hvoText, kflidStText_Paragraphs, &chvoParas));
		int ihvo; // used twice
		for (ihvo = 0; ihvo < chvoParas; ihvo++)
		{
			HVO hvoPara;
			CheckHr(qsdaTemp->get_VecItem(m_hvoText, kflidStText_Paragraphs, ihvo,
				&hvoPara));
			vhvoParas.Push(hvoPara);
		}
		// Actually, we may have had a real text...if so, skip this step
		if (m_hvoText < 0)
		{
			m_qcvd->MakeNewObject(kclidStText, m_hvoObj, m_flid, -2, &m_hvoText);
		}
		else
		{
			// Check if the record has been edited by someone else since we first loaded data.
			HRESULT hrTemp;
			if ((hrTemp = m_qcvd->CheckTimeStamp(m_hvoText)) != S_OK)
			{
				// If it was changed and the user does not want to overwrite it, perform a
				// refresh so the displayed field will revert to it's original value.
				// REVIEW KenZ (PaulP):  There may need to be a refresh call made here.  It's
				// difficult to know, however, since I haven't tracked down when this method
				// actually gets called.
				m_fDirty = false;
				return true;
			}
		}

		// Now make real paragraph objects and set their properties.
		for (ihvo = 0; ihvo < chvoParas; ihvo++)
		{
			HVO hvoPara = vhvoParas[ihvo];
			ITsStringPtr qtss;
			ITsTextPropsPtr qttp;
			IUnknownPtr qunkTtp;
			CheckHr(qsdaTemp->get_UnknownProp(hvoPara, kflidStStyle_Rules, &qunkTtp));
			CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
			CheckHr(qsdaTemp->get_StringProp(hvoPara, kflidStTxtPara_Contents,
				&qtss));
			CheckHr(m_qcvd->MakeNewObject(kclidStTxtPara, m_hvoText, kflidStText_Paragraphs,
				ihvo, &hvoPara));
			if (qttp)
				CheckHr(m_qcvd->SetUnknown(hvoPara, kflidStPara_StyleRules, qttp));
			CheckHr(m_qcvd->SetString(hvoPara, kflidStTxtPara_Contents, qtss));
		}
		m_qvcdMissing.Clear();
		// Update the root object to point at the real text we just made.
		int frag = kfrText;
		IVwViewConstructor * pvvc = m_qstvc;
		CheckHr(m_qrootb->putref_DataAccess(m_qcvd));
		CheckHr(m_qrootb->SetRootObjects(&m_hvoText, &pvvc, &frag,
			GetLpInfo()->GetAfStylesheet(), 1));
		CheckHr(m_qcvd->PropChanged(m_qrootb, kpctNotifyAllButMe, m_hvoObj, m_flid, 0,
					1, 1));
	}
	m_fDirty = false;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Close the current editor, saving changes that were made. hwnd is the editor hwnd.
	@param fForce True if we want to force the editor closed without making any
		validity checks or saving any changes.
----------------------------------------------------------------------------------------------*/
void AfDeFeSt::EndEdit(bool fForce)
{
	if (m_qvcdMissing && !fForce)
	{
		ComBool fDirty;
		ISilDataAccessPtr qsdaTemp;
		CheckHr(m_qvcdMissing->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
		qsdaTemp->IsDirty(&fDirty);
		if (fDirty)
		{
			// The text got edited. So, make a real text and copy the relevant info.
			// First get the paragraph list
			HvoVec vhvoParas;
			int chvoParas;

			// If we already had a text, we will notify it of new paragraphs.
			bool fNotifyText = m_hvoText > 0;

			CheckHr(qsdaTemp->get_VecSize(m_hvoText, kflidStText_Paragraphs, &chvoParas));
			int ihvo; // used twice
			for (ihvo = 0; ihvo < chvoParas; ihvo++)
			{
				HVO hvoPara;
				CheckHr(qsdaTemp->get_VecItem(m_hvoText, kflidStText_Paragraphs, ihvo,
					&hvoPara));
				vhvoParas.Push(hvoPara);
			}
			// Actually, we may have had a real text...if so, skip this step
			if (m_hvoText < 0)
			{
				m_qcvd->MakeNewObject(kclidStText, m_hvoObj, m_flid, -2, &m_hvoText);
			}
			else
			{
				// Check if the record has been edited by someone else since we first loaded
				// the data.
				HRESULT hrTemp;
				if ((hrTemp = m_qcvd->CheckTimeStamp(m_hvoText)) != S_OK)
				{
					// If it was changed and the user does not want to overwrite it, perform a
					// refresh so the displayed field will revert to it's original value.
					SuperClass::EndEdit(fForce);
					// REVIEW KenZ (PaulP):  There may need to be a refresh call made here.
					// It's difficult to know, however, since I haven't tracked down when
					// this method actually gets called.
					m_fDirty = false;
					return;
				}
			}

			// Now make real paragraph objects and set their properties.
			for (ihvo = 0; ihvo < chvoParas; ihvo++)
			{
				HVO hvoPara = vhvoParas[ihvo];
				ITsStringPtr qtss;
				ITsTextPropsPtr qttp;
				IUnknownPtr qunkTtp;
				CheckHr(qsdaTemp->get_UnknownProp(hvoPara, kflidStStyle_Rules, &qunkTtp));
				CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
				CheckHr(qsdaTemp->get_StringProp(hvoPara, kflidStTxtPara_Contents,
						&qtss));
				CheckHr(m_qcvd->MakeNewObject(kclidStTxtPara, m_hvoText, kflidStText_Paragraphs,
					ihvo, &hvoPara));
				if (qttp)
					CheckHr(m_qcvd->SetUnknown(hvoPara, kflidStPara_StyleRules, qttp));
				CheckHr(m_qcvd->SetString(hvoPara, kflidStTxtPara_Contents, qtss));
			}
			m_qvcdMissing.Clear();
			// Update the root object to point at the real text we just made.
			int frag = kfrText;
			IVwViewConstructor * pvvc = m_qstvc;
			CheckHr(m_qrootb->putref_DataAccess(m_qcvd));
			CheckHr(m_qrootb->SetRootObjects(&m_hvoText, &pvvc, &frag,
				GetLpInfo()->GetAfStylesheet(), 1));
			m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
			if (fNotifyText)
			{
				CheckHr(m_qcvd->PropChanged(m_qrootb, kpctNotifyAllButMe, m_hvoText,
					kflidStText_Paragraphs, 0,
							chvoParas, chvoParas));
			}
			else
			{
				CheckHr(m_qcvd->PropChanged(m_qrootb, kpctNotifyAllButMe, m_hvoObj, m_flid, 0,
							1, 1));
			}
		}
	}
	// Do this after the changes above, otherwise, our hwnd is no longer the child of a main
	// window, and we can't find the style sheet for the SetRootObjects call.
	SuperClass::EndEdit(fForce);
	m_fDirty = false;
}


/*----------------------------------------------------------------------------------------------
	Refresh the field from the data cache.
	We only need to do something if a new text has been edited.
----------------------------------------------------------------------------------------------*/
void AfDeFeSt::UpdateField()
{
	// See if we have a dummy cache filling a missing text.
	if (!m_qvcdMissing)
		return; // If not, the view code updates things automatically from the real cache.

	HVO hvoText;
	CheckHr(m_qcvd->get_ObjectProp(m_hvoObj, m_flid, &hvoText));
	if (!hvoText)
		return; // The main cache doesn't have anything yet.

	// We now have a text, but we still have a dummy data cache, so
	// we need to throw out the dummy data cache and use the real cache.
	m_hvoText = hvoText;
	m_qvcdMissing.Clear();
	// Update the root object to point at the real text in the main cache.
	int frag = kfrText;
	IVwViewConstructor * pvvc = m_qstvc;
	CheckHr(m_qrootb->putref_DataAccess(m_qcvd));
	CheckHr(m_qrootb->SetRootObjects(&m_hvoText, &pvvc, &frag,
		GetLpInfo()->GetAfStylesheet(), 1));
	if (m_hwnd)
	{
		// If we have an editing window open, make it redraw.
		::InvalidateRect(m_hwnd, NULL, true);
	}
}


/*----------------------------------------------------------------------------------------------
	Check the requirments of the FldSpec, and verify that data in the field meets the
	requirement. It returns:
		kFTReqNotReq if the all requirements are met.
		kFTReqWs if data is missing, but it is encouraged.
		kFTReqReq if data is missing, but it is required.
----------------------------------------------------------------------------------------------*/
FldReq AfDeFeSt::HasRequiredData()
{
	if (m_qfsp->m_fRequired == kFTReqNotReq)
		return kFTReqNotReq;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	bool fEmpty = true;
	ITsStringPtr qtss;
	int cch = 0;
	HVO hvoPara;
	int iPara;

	if (!m_hvoText)
		goto LExit;

	int cPara;
	CheckHr(qcvd->get_VecSize(m_hvoText, kflidStText_Paragraphs, &cPara));
	if (!cPara)
		goto LExit;

	// Make sure at least some paragraph has data.
	for (iPara = 0; iPara < cPara; ++iPara)
	{
		CheckHr(qcvd->get_VecItem(m_hvoText, kflidStText_Paragraphs, iPara, &hvoPara));
		// At some point we may need to handle something other than StTxtPara.
		CheckHr(qcvd->get_StringProp(hvoPara, kflidStTxtPara_Contents, &qtss));
		if (qtss)
			CheckHr(qtss->get_Length(&cch));
		if (cch)
			break; // Have a valid string
	}

	fEmpty = iPara == cPara;

LExit:
	if (fEmpty)
		return m_qfsp->m_fRequired;
	else
		return kFTReqNotReq;
}

/*----------------------------------------------------------------------------------------------
	Process mouse movements when the editor isn't active.
	See ${AfDeFieldEditor#OnMouseMove} for parameter descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeFeSt::OnMouseMove(uint grfmk, int xp, int yp)
{
	// See if the mouse is over text.
	RECT rcSrcRoot;
	RECT rcDstRoot;

	InitGraphics();
	GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
	UninitGraphics();

	ComBool fInObject;
	int odt;
	CheckHr(m_qrootb->get_IsClickInObject(xp, yp, rcSrcRoot, rcDstRoot, &odt, &fInObject));

	// Change the cursor to a pointing finger if over a hot link.
	if (fInObject && (odt == kodtNameGuidHot || odt == kodtExternalPathName
		|| odt == kodtOwnNameGuidHot))
	{
		::SetCursor(::LoadCursor(NULL, IDC_HAND));
	}
	else
		::SetCursor(::LoadCursor(NULL, IDC_IBEAM));
	return true;
}


/*----------------------------------------------------------------------------------------------
	This method saves the current cursor information in RecMainWnd. Normally it just
	stores the cursor index in RecMainWnd::m_ichCur. For structured texts, however,
	it also inserts the appropriate hvos and flids for the StText classes in
	m_vhvoPath and m_vflidPath. Other editors may need to do other things.
----------------------------------------------------------------------------------------------*/
void AfDeFeSt::SaveCursorInfo()
{
	SuperClass::SaveCursorInfo();
	// Store the StText info and field info.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (!prmw)
		return;
	Vector<int> & vflid = prmw->GetFlidPath();
	Vector<HVO> & vhvo = prmw->GetHvoPath();
	vhvo.Insert(0, m_hvoText);
	vflid.Insert(0, kflidStText_Paragraphs);
}


/*----------------------------------------------------------------------------------------------
	This attempts to place the cursor as defined in RecMainWnd m_vhvoPath, m_vflidPath,
	and m_ichCur.
	@param vhvo Vector of ids inside the field.
	@param vflid Vector of flids inside the field.
	@param ichCur Character offset in the final field for the cursor.
----------------------------------------------------------------------------------------------*/
void AfDeFeSt::RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur)
{
	// Store the current record/subrecord and field info.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (!prmw)
		return;
	CustViewDaPtr qcvd = prmw->MainDa();
	AssertPtr(qcvd);
	IVwSelectionPtr qsel;
	if (m_qrootb)
	{
		// Get the index for the paragraph.
		// We are assuming at this point that we have something like
		// vhvo = 2007 (RnGenericRec), 2014 (StText), 2017 (StTxtPara)
		// vflid = 4006001, 14001 (StText_Paragraphs), 16002 (kflidStTxtPara_Contents)
		bool fFullSpec = vflid.Size() > 1 && vhvo.Size() > 2 && vflid[1] == kflidStText_Paragraphs;
		if (fFullSpec)
		{
			int ihvo;
			VwSelLevInfo rgvsli[1];
			CheckHr(qcvd->GetObjIndex(vhvo[1], vflid[1], vhvo[2], &ihvo));
			rgvsli[0].tag = kflidStText_Paragraphs; // Set up the index for the ids.
			rgvsli[0].cpropPrevious = 0;
			rgvsli[0].ihvo = ihvo;
			m_qrootb->MakeTextSelection(
				0, // int ihvoRoot
				1, // int cvlsi,
				rgvsli, // VwSelLevInfo * prgvsli
				kflidStTxtPara_Contents, // int tagTextProp,
				0, // int cpropPrevious,
				ichCur, // int ichAnchor,
				ichCur, // int ichEnd,
				0, // int ws,
				true, // ComBool fAssocPrev,
				-1, // int ihvoEnd,
				NULL, // ITsTextProps * pttpIns,
				true, // ComBool fInstall,
				&qsel); // IVwSelection ** ppsel
		}
		// If we didn't get a text selection, try getting a selection somewhere close.
		if (!qsel)
		{
			m_qrootb->MakeTextSelInObj(
				0,  // index of the one and only root object in this view
				0, // the object we want is one level down
				NULL, // and here's how to find it there
				0,
				NULL, // don't worry about the endpoint
				true, // select at the start of it
				true, // Find an editable field
				false, // and don't select a range.
				// Making this true, allows the whole record to scroll into view when we launch
				// a new window by clicking on a reference to an entry, but we don't get an insertion
				// point. Using false gives an insertion point, but the top of the record is typically
				// at the bottom of the screen, which isn't good.
				false, // don't select the whole object
				true, // but do install it as the current selection
				NULL); // and don't bother returning it to here.
		}
	}
}
