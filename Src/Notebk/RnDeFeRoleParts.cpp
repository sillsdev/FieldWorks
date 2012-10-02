/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2000, SIL International. All rights reserved.

File: RnDeFeRoleParts.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	Implements the RnDeFeRoleParts class for displaying date and time.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnDeFeRoleParts::RnDeFeRoleParts()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
RnDeFeRoleParts::~RnDeFeRoleParts()
{
}


/*----------------------------------------------------------------------------------------------
	Complete initialization of the property by storing the appropriate items, names, and lists.
	@param pssl The id for the People possibility list we are showing in this field.
	@param hvoOwner The owner of the RnRoledPartic we are displaying in this field.
	@param flid The flid in hvoOwner owning the RnRoledPartic we are displaying.
	@param pssRole The id of the CmPossibility from the Role list that we are using to display
		the label. (May be NULL for the unspecified item).
	@param fHier True if item names include parent items in the hierarchy.
	@param pnt Indicates whether we show name, abbr, or both for each item.
----------------------------------------------------------------------------------------------*/
void RnDeFeRoleParts::Init(HVO pssl, HVO hvoOwner, int flid, HVO pssRole, bool fHier,
	PossNameType pnt)
{
	Assert(m_flid); // Don't call this until Initialize has been called.
	Assert(pssl);
	Assert(hvoOwner);
	Assert(flid);
	Assert((uint)pnt < (uint)kpntLim);

	// Save the list id.
	m_vpssl.Push(pssl);
	m_fHier = fHier;
	m_pnt = pnt;
	m_hvoOwner = hvoOwner;
	m_flidOwner = flid;
	m_pssRole = pssRole;

	// Set the proper m_wsMagic value for the People possibility list.
	m_wsMagic = kwsVernAnals;

	// Set up a notifier for the people list.
	PossListInfoPtr qpli;
	GetLpInfo()->LoadPossList(pssl, m_wsMagic, &qpli);
	AssertPtr(qpli);
	qpli->AddNotify(this);
	// Set up a notifier for the role list.
	if (pssRole)
	{
		GetLpInfo()->LoadPossListForItem(pssRole, m_wsMagic, &qpli);
		AssertPtr(qpli);
		qpli->AddNotify(this);
		m_psslRole = qpli->GetPsslId();
	}

	// Create the display cache.
	m_qvcd.CreateInstance(CLSID_VwCacheDa);
	// Create the view constructor.
	m_qtgvc.Attach(NewObj TagsVc(m_qfsp->m_stuSty.Chars(), m_chrp.clrBack));

	// Set the writing system info (especially for Right-To-Left handling).
	ILgWritingSystemFactoryPtr qwsf;
	GetLpInfo()->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	m_qtgvc->SetWritingSystemInfo(m_ws, qwsf);

	// Add information to the display cache.
	FillDisplayCache();
}


/*----------------------------------------------------------------------------------------------
	Release all smart pointers.
----------------------------------------------------------------------------------------------*/
void RnDeFeRoleParts::OnReleasePtr()
{
	// Remove the notifier for the role list.
	if (m_psslRole)
	{
		PossListInfoPtr qpli;
		// Note: We need to use GetPossList here rather than LoadPossList since the
		// latter will try to reload the list if not present. During shutdown, the
		// possibility lists have already been cleared from AfLpInfo before we reach
		// this, so we want to quitely shut down. Using LoadPossList here produces a
		// massive memory leak.
		if (GetLpInfo()->GetPossList(m_psslRole, m_wsMagic, &qpli))
		{
			AssertPtr(qpli);
			qpli->RemoveNotify(this);
		}
		m_psslRole = 0;
	}

	SuperClass::OnReleasePtr();
}


/*----------------------------------------------------------------------------------------------
	Saves changes but keeps the editing window open. In this method we need to convert dummy
	items into real items if there are any people present. We also need to delete
	RnRoledPartic and convert the field into a dummy if all of the people have been
	deleted. Otherwise, there is no way for the user to get rid of RnRoledPartic.
	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnDeFeRoleParts::SaveEdit()
{
	// See if any items are present.
	int cpss;
	HVO hvopssl = m_vpssl[0];
	ISilDataAccessPtr qsdaTemp;
	HRESULT hr = m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp);
	if (FAILED(hr))
		ThrowInternalError(E_INVALIDARG);
	CheckHr(qsdaTemp->get_VecSize(hvopssl, kflidPssIds, &cpss));
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);

	if (cpss > 1) // Don't count the dummy at the end.
	{
		if (m_hvoObj == khvoDummyRnRoledPartic)
		{
			// We have a dummy RnRoledParticipant with at least one item, so we need to
			// turn the dummy into a real object.
			HVO hvo;
			BeginChangesToLabel();
			CheckHr(qcvd->MakeNewObject(kclidRnRoledPartic, m_hvoOwner, kflidRnEvent_Participants,
				-1, &hvo));
			Assert(hvo);
			m_hvoObj = hvo;

			// If someone deleted the role we are interested in while we are displaying
			// this field, we can't set the object prop or we'll get an error from the
			// database. So before doing a save, make sure our role hasn't been deleted.
			if (m_pssRole)
				CheckHr(qcvd->SetObjProp(hvo, kflidRnRoledPartic_Role, m_pssRole));

			SuperClass::SaveEdit();
			CheckHr(qcvd->EndUndoTask());
			// Need to notify this property after the participants were added in the superclass
			// for this to properly update document views.
			CheckHr(qcvd->PropChanged(m_qrootb, kpctNotifyAllButMe, m_hvoOwner,
				kflidRnEvent_Participants, 1, 1, 0));
			return true;
		}
		else
		{
			// We have a real RnRoledParticipant with at least one item, so handle it as usual.
			bool fDirty = m_fDirty;
			if (!SuperClass::SaveEdit())
				return false;
			if (fDirty)
			{
				// Database triggers/procs do not update the entry, so we must do it here.
				qcvd->SetTimeStamp(m_hvoOwner);
				qcvd->CacheCurrTimeStamp(m_hvoOwner);
			}
			return true;
		}
	}
	else
	{
		if (m_hvoObj == khvoDummyRnRoledPartic)
		{
			// We have a dummy RnRoledParticipant without any people. Do nothing so the
			// field will go away when we move to the next record.
			return true;
		}
		else
		{
			// We have a real RnRoledParticipant with no items, so we need to change this
			// into a dummy RnRoledParticipant and remove it from the database.
			CustViewDaPtr qcvd;
			GetDataAccess(&qcvd);
			AssertPtr(qcvd);
			int ihvo;
			BeginChangesToLabel();
			CheckHr(qcvd->GetObjIndex(m_hvoOwner, kflidRnEvent_Participants, m_hvoObj, &ihvo));
			CheckHr(qcvd->DeleteObjOwner(m_hvoOwner, m_hvoObj, kflidRnEvent_Participants, ihvo));
			CheckHr(qcvd->EndUndoTask());
			m_hvoObj = khvoDummyRnRoledPartic;
			CheckHr(qcvd->PropChanged(m_qrootb, kpctNotifyAllButMe, m_hvoOwner,
				kflidRnEvent_Participants, 1, 0, 1));
			return true;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	This method saves the current cursor information in RecMainWnd. Normally it just
	stores the cursor index in RecMainWnd::m_ichCur. For structured texts, however,
	it also inserts the appropriate hvos and flids for the StText classes in
	m_vhvoPath and m_vflidPath. Other editors may need to do other things.
----------------------------------------------------------------------------------------------*/
void RnDeFeRoleParts::SaveCursorInfo()
{
	SuperClass::SaveCursorInfo();
}


/*----------------------------------------------------------------------------------------------
	This attempts to place the cursor as defined in RecMainWnd m_vhvoPath, m_vflidPath,
	and m_ichCur.
	@param vhvo Vector of ids inside the field.
	@param vflid Vector of flids inside the field.
	@param ichCur Character offset in the final field for the cursor.
----------------------------------------------------------------------------------------------*/
void RnDeFeRoleParts::RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur)
{
	// Store the current record/subrecord and field info.
	//::SendMessage(m_hwnd, EM_SETSEL, ichCur, ichCur);
}


/*----------------------------------------------------------------------------------------------
	Something has changed in the possibility list.
----------------------------------------------------------------------------------------------*/
void RnDeFeRoleParts::ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
	int ipssDst)
{
	if (hvoPssl != m_psslRole)
	{
		// Process changes for people lists.
		SuperClass::ListChanged(nAction, hvoPssl, hvoSrc, hvoDst, ipssSrc, ipssDst);
		return;
	}

	// If someone deleted the role we are interested in we need to clean up.
	if (hvoSrc == m_pssRole && nAction == kplnaDelete)
	{
		// Remove the notifier for the role list.
		Assert(m_psslRole);
		PossListInfoPtr qpli;
		GetLpInfo()->LoadPossList(m_psslRole, m_wsMagic, &qpli);
		AssertPtr(qpli);
		qpli->RemoveNotify(this);
		m_psslRole = 0;
		m_pssRole = 0;
	}
	// At this point we are not getting kplnaMerged at all since we are just deleting all
	// fields and recreating new ones. But at some point down the road we may want to switch
	// to where we don't delete and recreate. This would be the right action to take then.
	if (hvoSrc == m_pssRole && nAction == kplnaMerged)
	{
		// Switch to the new role.
		m_pssRole = hvoDst;
	}
	// Now reset the label in case we changed.
	// Use Participants for the unspecified editor label.
	ITsStringPtr qtssName;
	int wsUser = GetLpInfo()->GetDbInfo()->UserWs();
	AfUtil::GetResourceTss(kstidTlsOptParticipants, wsUser, &qtssName);
	ITsStringPtr qtssHelp = m_qfsp->m_qtssHelp;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	if (m_pssRole)
	{
		PossItemInfo * ppii;
		GetLpInfo()->GetPossListAndItem(m_pssRole, wsUser, &ppii, NULL);
		StrUni stuName;  // the Role's name
		ppii->GetName(stuName, m_pnt);
		qtsf->MakeStringRgch(stuName.Chars(), stuName.Length(), wsUser, &qtssName);

		ITsIncStrBldrPtr qtisb;
		qtisb.CreateInstance(CLSID_TsIncStrBldr);

		// Add the first substring.
		qtisb->AppendTsString(m_qfsp->m_qtssHelp);

		// Add the second substring.
		StrUni stuTemp;
		stuTemp.Load(kstidRnRoledPartic_HelpA);
		qtisb->AppendRgch(stuTemp.Chars(), stuTemp.Length());

		// Turn bold on.
		qtisb->SetIntPropValues(ktptBold, ktpvEnum, kttvForceOn);
		// Add the Role's name
		qtisb->AppendRgch(stuName.Chars(), stuName.Length());
		// Turn bold off.
		qtisb->SetIntPropValues(ktptBold, ktpvEnum, kttvOff);

		// Add the third substring.
		stuTemp.Load(kstidRnRoledPartic_HelpB);
		qtisb->AppendRgch(stuTemp.Chars(), stuTemp.Length());

		// Get the completed TsString.
		qtisb->GetString(&qtssHelp);
	}
	m_qtssLabel = qtssName;
	m_qtssHelp = qtssHelp;

	// Force the label to redraw.
	Rect rcLabel;
	::GetClientRect(m_qadsc->Hwnd(),&rcLabel);
	Rect rcEdit(m_rcClip);
	if (m_hwnd)
	{
		// We have an open edit box, so get the correct coordinates for the client window.
		::GetClientRect(m_hwnd, &rcEdit);
		::MapWindowPoints(m_hwnd, m_qadsc->Hwnd(), (POINT *)&rcEdit, 2);
	}
	rcLabel.top = rcEdit.top;
	rcLabel.bottom = rcEdit.bottom;
	rcLabel.right = m_qadsc->GetTreeWidth();
	::InvalidateRect(m_qadsc->Hwnd(), &rcLabel, false);
}
