/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: VwTestRootSite.cpp
Responsibility: Luke Ulrich
Last reviewed: never

Description:
	Dummy test site for testing views
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

VwTestRootSite::VwTestRootSite()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}

VwTestRootSite::~VwTestRootSite()
{
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTestRootSite::QueryInterface(REFIID riid, void ** ppv)
{
	if (!ppv)
		return WarnHr(E_POINTER);
	AssertPtr(ppv);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwRootSite)
		*ppv = static_cast<IVwRootSite *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}
//----------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------
void VwTestRootSite::SetVgObject(IVwGraphics *pvg)
{
	m_pvg = pvg;
}
//----------------------------------------------------------------------------------------------
void VwTestRootSite::SetSrcRoot(RECT rcSrcRoot)
{
	m_rcSrcRoot = rcSrcRoot;
}
//----------------------------------------------------------------------------------------------
void VwTestRootSite::SetDstRoot(RECT rcDstRoot)
{
	m_rcDstRoot = rcDstRoot;
}
//----------------------------------------------------------------------------------------------
void VwTestRootSite::SetAvailWidth(IVwRootBoxPtr ptb, int iwidth)
{
	m_twWidth = iwidth;
	ptb->Layout(m_pvg, iwidth);
}
//----------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------
/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTestRootSite::InvalidateRect(IVwRootBox * pRoot, int xdLeft, int ydTop,
	int xdWidth, int ydHeight)
{
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTestRootSite::GetGraphics(IVwGraphics ** ppvg, RECT * prcSrcRoot, RECT * prcDstRoot)
{
	*ppvg = m_pvg;
	*prcSrcRoot = m_rcSrcRoot;
	*prcDstRoot = m_rcDstRoot;
	return S_OK;
}
/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTestRootSite::ReleaseGraphics(IVwGraphics * pvg)
{
	return S_OK;
}
/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTestRootSite::GetAvailWidth(int * ptwWidth)
{
	*ptwWidth = m_twWidth;
	return S_OK;
}
/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTestRootSite::SizeChanged()
{
	return S_OK;
}
/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTestRootSite::DoUpdates()
{
	return S_OK;
}
/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTestRootSite::PropChanged(HVO hvo, int tag, int ivMin, int cvIns, int cvDel)
{
	return S_OK;
}
