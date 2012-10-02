/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwAccessRoot.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

// CO_E_OBJECTNOTCONNECTED REturn if target object destroyed.
ROLE_SYSTEM_CELL
ROLE_SYSTEM_GRAPHIC
ROLE_SYSTEM_GROUPING
ROLE_SYSTEM_PANE
ROLE_SYSTEM_ROW
ROLE_SYSTEM_SEPARATOR
ROLE_SYSTEM_TABLE
ROLE_SYSTEM_TEXT

STATE_SYSTEM_FOCUSABLE
STATE_SYSTEM_FOCUSED
STATE_SYSTEM_INVISIBLE
STATE_SYSTEM_OFFSCREEN
STATE_SYSTEM_READONLY
STATE_SYSTEM_SELECTABLE
STATE_SYSTEM_SELECTED
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

#ifndef CO_E_OBJECTNOTCONNECTED
#define CO_E_OBJECTNOTCONNECTED E_FAIL
#endif

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

static DummyFactory g_fact(
	_T("SIL.Views.VwAccessRoot"));

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************

VwAccessRoot::VwAccessRoot(VwBox * pbox)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_pbox = pbox;
	m_iaccCurrent = 0; // start at beginning if used as iterator.
	ViewsGlobals::m_hmboxacc->Insert(pbox, (IAccessible *) this);
}

// Used to make clones.
VwAccessRoot::VwAccessRoot(VwAccessRoot * pacc)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_pbox = pacc->m_pbox;
	m_iaccCurrent = pacc->m_iaccCurrent;
}


VwAccessRoot::~VwAccessRoot()
{
	if (m_pbox)
		ViewsGlobals::m_hmboxacc->Delete(m_pbox);
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP VwAccessRoot::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IAccessible *>(this));
	else if (riid == IID_IAccessible)
		*ppv = static_cast<IAccessible *>(this);
	else if (riid == IID_IServiceProvider)
		*ppv = static_cast<IServiceProvider *>(this);
	else if (riid == IID_IDispatch)
		*ppv = static_cast<IDispatch *>(this);
	else if (riid == IID_IEnumVARIANT)
		*ppv = static_cast<IEnumVARIANT *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo3(static_cast<IAccessible *>(this),
			IID_IAccessible, IID_IServiceProvider, IID_IEnumVARIANT);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	IAccessibility methods
//:>********************************************************************************************
STDMETHODIMP VwAccessRoot::accNavigate(long navDir, VARIANT varStart, VARIANT * pvarEnd)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvarEnd);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;
	VwGroupBox * pgboxContainer = m_pbox->Container();
	VwGroupBox * pgboxThis = dynamic_cast<VwGroupBox *>(m_pbox);
	VwBox * pboxRes = NULL;
	switch(navDir)
	{
	case NAVDIR_DOWN:
		if (pgboxContainer)
			pboxRes = pgboxContainer->NavDown(m_pbox);
		break;
	case NAVDIR_FIRSTCHILD:
		if (pgboxThis)
			pboxRes = pgboxThis->FirstBox();
		break;
	case NAVDIR_LASTCHILD:
		if (pgboxThis)
			pboxRes = pgboxThis->LastBox();
		break;
	case NAVDIR_LEFT:
		if (pgboxContainer)
			pboxRes = pgboxContainer->NavLeft(m_pbox);
		break;
	case NAVDIR_NEXT:
		if (pgboxContainer)
			pboxRes = m_pbox ->NextOrLazy();
		break;
	case NAVDIR_PREVIOUS:
		if (pgboxContainer)
			pboxRes = pgboxContainer->BoxBefore(m_pbox);
		break;
	case NAVDIR_RIGHT:
		if (pgboxContainer)
			pboxRes = pgboxContainer->NavRight(m_pbox);
		break;
	case NAVDIR_UP:
		if (pgboxContainer)
			pboxRes = pgboxContainer->NavUp(m_pbox);
		break;
	// default: leave pboxRes null, return VT_EMPTY/EUNEXPECTED.
	}
	if (!pboxRes)
	{
		pvarEnd->vt = VT_EMPTY;
		return E_UNEXPECTED;
	}
	pvarEnd->vt = VT_DISPATCH;
	GetAccessFor(pboxRes, &(pvarEnd->pdispVal));

	END_COM_METHOD(g_fact, IID_IAccessible);
}

STDMETHODIMP VwAccessRoot::get_accChild(VARIANT varChildID, IDispatch ** ppdispChild)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppdispChild);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;
	if (varChildID.vt != VT_I4)
		return E_UNEXPECTED;
	int ichild = varChildID.intVal;

	// Get the appropriate child.
	if (ichild < 0)
		return S_FALSE;
	GetAccessFor(m_pbox->ChildAt(ichild), ppdispChild);

	END_COM_METHOD(g_fact, IID_IAccessible);
}
STDMETHODIMP VwAccessRoot::get_accChildCount(long * pcountChildren)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcountChildren);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;
	*pcountChildren = m_pbox->ChildCount();
	END_COM_METHOD(g_fact, IID_IAccessible);
}
STDMETHODIMP VwAccessRoot::get_accParent(IDispatch ** ppdispParent)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppdispParent);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;
	// It's really a group box, but here we don't care, and the Retrieve function does.
	GetAccessFor(m_pbox->Container(), ppdispParent);

	END_COM_METHOD(g_fact, IID_IAccessible);
}

// We haven't figured a sensible default action for boxes.
STDMETHODIMP VwAccessRoot::accDoDefaultAction(VARIANT varID)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IAccessible);
}

// No accelerators for boxes.
STDMETHODIMP VwAccessRoot::get_accDefaultAction(VARIANT varID, BSTR * pszDefaultAction)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pszDefaultAction);
	END_COM_METHOD(g_fact, IID_IAccessible);
}

STDMETHODIMP VwAccessRoot::get_accDescription(VARIANT varID, BSTR * pszDescription)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pszDescription);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;

	StrUni stuDesc = m_pbox->Description();
	stuDesc.GetBstr(pszDescription);

	END_COM_METHOD(g_fact, IID_IAccessible);
}

STDMETHODIMP VwAccessRoot::get_accHelp(VARIANT varID, BSTR * pszHelp)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IAccessible);
}

STDMETHODIMP VwAccessRoot::get_accHelpTopic(BSTR * pszHelpFile, VARIANT varChild,
	long * pidTopic)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IAccessible);
}

STDMETHODIMP VwAccessRoot::get_accKeyboardShortcut(VARIANT varID, BSTR * pszKeyboardShortcut)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IAccessible);
}

STDMETHODIMP VwAccessRoot::get_accName(VARIANT varID, BSTR * pszName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pszName);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;
	// Review JohnT: should we handle varIDs other than SELF?
	SmartBstr sbstrName = m_pbox->Name();
	*pszName = sbstrName.Detach();

	END_COM_METHOD(g_fact, IID_IAccessible);
}

STDMETHODIMP VwAccessRoot::get_accRole(VARIANT varID, VARIANT * pvarRole)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvarRole);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;
	pvarRole->vt = VT_I4;
	pvarRole->intVal = m_pbox->Role();
	END_COM_METHOD(g_fact, IID_IAccessible);
}

STDMETHODIMP VwAccessRoot::get_accState(VARIANT varID, VARIANT * pvarState)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvarState);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;
	pvarState->vt = VT_I4;
	pvarState->intVal = m_pbox->State();
	END_COM_METHOD(g_fact, IID_IAccessible);
}
STDMETHODIMP VwAccessRoot::get_accValue(VARIANT varID, BSTR * pszValue)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pszValue);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;

	StrUni stuDesc = m_pbox->Description();
	stuDesc.GetBstr(pszValue);

	END_COM_METHOD(g_fact, IID_IAccessible);
}


STDMETHODIMP VwAccessRoot::accSelect(long flagsSelect, VARIANT varID)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IAccessible);
}
// Because selections don't correspond well to boxes (a selection may be part of a box,
// or cross several boxes), we currently don't support this. We might enhance it later,
// at least for special cases like a selection entirely within one paragraph.
STDMETHODIMP VwAccessRoot::get_accFocus(VARIANT * pvarID)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvarID);
	pvarID->vt = VT_EMPTY;
	END_COM_METHOD(g_fact, IID_IAccessible);
}
STDMETHODIMP VwAccessRoot::get_accSelection(VARIANT * pvarChildren)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IAccessible);
}
STDMETHODIMP VwAccessRoot::accLocation(long * pxLeft, long * pyTop, long * pcxWidth,
	long * pcyHeight, VARIANT varID)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pxLeft);
	ChkComArgPtr(pyTop);
	ChkComArgPtr(pcxWidth);
	ChkComArgPtr(pcyHeight);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;
	VwRootBox * prootb = m_pbox->Root();
	IVwRootSite * psite = prootb->Site();
	// Rather arbitrarily, use the coordinate transformation at the top left of the box.
	Point pt(m_pbox->LeftToLeftOfDocument(), m_pbox->TopToTopOfDocument());
	HoldGraphicsAtSrc hg(prootb, pt);
	Rect bounds = m_pbox->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
	Point ptTl = bounds.TopLeft();
	psite->ClientToScreen(prootb, &ptTl);
	*pxLeft = ptTl.x;
	*pyTop = ptTl.y;
	*pcxWidth = bounds.Width();
	*pcyHeight = bounds.Height();

	END_COM_METHOD(g_fact, IID_IAccessible);
}

STDMETHODIMP VwAccessRoot::accHitTest(long xLeft, long yTop, VARIANT * pvarID)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvarID);
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;
	VwRootBox * prootb = m_pbox->Root();
	IVwRootSite * psite = prootb->Site();
	Point ptTarget;
	ptTarget.x = xLeft;
	ptTarget.y = yTop;
	psite->ScreenToClient(prootb, &ptTarget);
	HoldGraphicsAtDst hg(prootb, ptTarget);
	VwBox * pboxRes = m_pbox->FindBoxContaining(ptTarget, hg.m_qvg, hg.m_rcSrcRoot,
		hg.m_rcDstRoot);
	if (pboxRes == m_pbox)
	{
		// Doc says to return I4 value as follows if not within a child
		pvarID->vt= VT_I4;
		pvarID->intVal = CHILDID_SELF;
		return S_OK;
	}
	if (!pboxRes)
	{
		pvarID->vt = VT_EMPTY;
		return S_OK;
	}

	pvarID->vt = VT_DISPATCH;
	GetAccessFor(pboxRes, &(pvarID->pdispVal));

	END_COM_METHOD(g_fact, IID_IAccessible);
}

// The compiler insists on this but it isn't in the doc for IAccessible. Leave unimplemented.
STDMETHODIMP VwAccessRoot::put_accName(VARIANT, BSTR bstrName)
{
	BEGIN_COM_METHOD;
	m_pbox->SetAccessibleName(bstrName);
	END_COM_METHOD(g_fact, IID_IAccessible);
}

// The compiler insists on this but it isn't in the doc for IAccessible. Leave unimplemented.
STDMETHODIMP VwAccessRoot::put_accValue(VARIANT,BSTR)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IAccessible);
}

//:>********************************************************************************************
//:>	IServiceProvider methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Rather like QueryInterface, but can provide access to interfaces on other objects. The
	only one it can currently provide is the root box.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwAccessRoot::QueryService(REFGUID guidService, REFIID riid, void ** ppv)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppv);
	if (riid != IID_IVwRootBox)
		return E_NOINTERFACE;
	if (!m_pbox)
		return CO_E_OBJECTNOTCONNECTED;
	*ppv = static_cast<IVwRootBox *>(m_pbox->Root());
	m_pbox->Root()->AddRef();
	END_COM_METHOD(g_fact, IID_IServiceProvider);
}

//:>********************************************************************************************
//:>	IDispatch methods. We don't really implement this but IAccessible inherits from
//:>	it so we have to pretend to.
//:>********************************************************************************************

STDMETHODIMP VwAccessRoot::GetTypeInfoCount(UINT *)
{
	return E_NOTIMPL;
}

STDMETHODIMP VwAccessRoot::GetTypeInfo(UINT,LCID,ITypeInfo ** )
{
	return E_NOTIMPL;
}

STDMETHODIMP VwAccessRoot::GetIDsOfNames(const IID &,LPOLESTR * ,UINT,LCID,DISPID *)
{
	return E_NOTIMPL;
}

STDMETHODIMP VwAccessRoot::Invoke(DISPID,const IID &,LCID,WORD,DISPPARAMS *,VARIANT *,EXCEPINFO *,UINT *)
{
	return E_NOTIMPL;
}

// When a box is deleted, make the corresponding VwAccessRoot (if any) invalid.
void VwAccessRoot::BoxDeleted(VwBox * pbox)
{
	IAccessiblePtr qacc;
	if (ViewsGlobals::m_hmboxacc->Retrieve(pbox, qacc))
	{
		ViewsGlobals::m_hmboxacc->Delete(pbox);
		dynamic_cast<VwAccessRoot *>(qacc.Ptr())->m_pbox = NULL;
	}
}

// Get a dispinterface for the IAccessibility object for the specified box.
void VwAccessRoot::GetAccessFor(VwBox * pbox, IDispatch ** ppdisp)
{
	*ppdisp = NULL;
	if (!pbox)
		return;
	IAccessiblePtr qacc;
	// If we already made one for that box, answer it. Otherwise make one.
	if (!ViewsGlobals::m_hmboxacc->Retrieve(pbox, qacc))
		qacc.Attach(NewObj VwAccessRoot(pbox));

	qacc->QueryInterface(IID_IDispatch, (void **)ppdisp);
}
//:>********************************************************************************************
//:>	IEnumVariant Methods
//:>********************************************************************************************

STDMETHODIMP VwAccessRoot::Next(unsigned long celt, VARIANT FAR * prgvar, unsigned long FAR* pceltFetched)
{
	BEGIN_COM_METHOD;
	if (pceltFetched)
		*pceltFetched = 0;
	ChkComArrayArg(prgvar, celt);
	for (uint i=0; i<celt; i++)
		VariantInit(&prgvar[i]);
	uint iaccMin = m_iaccCurrent;
	uint iaccLim = std::min(iaccMin + celt, (unsigned long)m_pbox->ChildCount());
	for (uint iacc = iaccMin; iacc < iaccLim; iacc++)
	{
		prgvar[iacc - m_iaccCurrent].vt = VT_DISPATCH;
		GetAccessFor(m_pbox->ChildAt(iacc), &prgvar[iacc - m_iaccCurrent].pdispVal);
	}
	m_iaccCurrent += iaccLim - iaccMin;
	if (pceltFetched)
		*pceltFetched = iaccLim - iaccMin;
	if (iaccLim - iaccMin < celt)
		return S_FALSE;  // didn't get all asked for.

	END_COM_METHOD(g_fact, IID_IEnumVARIANT);
}
STDMETHODIMP VwAccessRoot::Skip(unsigned long celt)
{
	BEGIN_COM_METHOD;
	m_iaccCurrent += celt;
	END_COM_METHOD(g_fact, IID_IEnumVARIANT);
}
STDMETHODIMP VwAccessRoot::Reset()
{
	m_iaccCurrent =  0;
	return S_OK;
}
STDMETHODIMP VwAccessRoot::Clone(IEnumVARIANT FAR* FAR* ppenum)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppenum);
	*ppenum = new VwAccessRoot(this);
	END_COM_METHOD(g_fact, IID_IEnumVARIANT);
}


#include "ComHashMap_i.cpp"
template class ComHashMap<VwBox *, IAccessible>; // BoxAccessorMap; // Hungarian hmboxacc;
