/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UndoAction.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	This file contains the implementation for UndoAction and its subclasses.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


//:>********************************************************************************************
//:>	UndoAction methods.
//:>********************************************************************************************

static DummyFactory g_fact(_T("SIL.Views.lib.VwUndoAction"));
static DummyFactory g_factDa(_T("SIL.Views.lib.VwUndoDa"));
static DummyFactory g_factDelAct(_T("SIL.Views.lib.VwUndoDeleteAction"));
static DummyFactory g_factInsAct(_T("SIL.Views.lib.VwUndoInsertAction"));
static DummyFactory g_factRepAct(_T("SIL.Views.lib.VwUndoReplaceAction"));
static DummyFactory g_factSetBinAct(_T("SIL.Views.lib.VwUndoSetBinaryAction"));
static DummyFactory g_factSetIntAct(_T("SIL.Views.lib.VwUndoSetIntAction"));
static DummyFactory g_factSetTimeAct(_T("SIL.Views.lib.VwUndoSetTimeAction"));
static DummyFactory g_factSetGuidAct(_T("SIL.Views.lib.VwUndoSetGuidAction"));
static DummyFactory g_factSetStrAct(_T("SIL.Views.lib.VwUndoSetStringAction"));
static DummyFactory g_factSetUniAct(_T("SIL.Views.lib.VwUndoSetUnicodeAction"));
static DummyFactory g_factSetUnkAct(_T("SIL.Views.lib.VwUndoSetUnknownAction"));
static DummyFactory g_factStyleAct(_T("SIL.Views.lib.VwUndoStylesheetAction"));
#ifndef VIEWSDLL
static DummyFactory g_factSelAct(_T("SIL.Views.lib.VwUndoSelectionAction"));
#endif

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance of VwUndoDa with CoCreateInstance.
//:>********************************************************************************************
// The generic factory instance is in VwRootBox.cpp (rather arbitrarily) so that other
// things that include this file don't get registered as servers for VwUndoDa.

void VwUndoDa::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwUndoDa> qsda;
	qsda.Attach(NewObj VwUndoDa());		// ref count initialy 1
	CheckHr(qsda->QueryInterface(riid, ppv));
}
/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwUndoAction::VwUndoAction(VwUndoDa * puda, HVO hvoObj, PropTag tag)
{
	m_puda = puda;
	m_hvoObj = hvoObj;
	m_tag = tag;
	m_fStateUndone = false;

	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwUndoAction::~VwUndoAction()
{
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoAction::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IUndoAction *>(this));
	else if (iid == IID_IUndoAction)
		*ppv = static_cast<IUndoAction *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IUndoAction);
		return NOERROR;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) VwUndoAction::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) VwUndoAction::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Undo an action. Should only be called for subclasses.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoAction::Undo(ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	Redo an action. Should only be called for subclasses.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoAction::Redo(ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	Commit an action so that it can no longer be undone.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoAction::Commit()
{
	BEGIN_COM_METHOD;

	return S_OK;

	END_COM_METHOD(g_fact, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	Return true if this is the kind of action that represents a real change to the
	data; false if it is just cleaning up the user interface (eg, replacing the selection).
	Most actions make real changes, so here we answer true by default.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoAction::get_IsDataChange(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);

	*pfRet = true;

	END_COM_METHOD(g_fact, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#IsRedoable}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoAction::get_IsRedoable(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);

	*pfRet = true;

	END_COM_METHOD(g_fact, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#SuppressNotification}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoAction::put_SuppressNotification(ComBool fSuppress)
{
	BEGIN_COM_METHOD;

	END_COM_METHOD(g_fact, IID_IUndoAction);
}

//:>********************************************************************************************
//:>	General VwUndoDa methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwUndoDa::VwUndoDa()
{
	m_qacth.CreateInstance(CLSID_ActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwUndoDa::~VwUndoDa()
{
	m_qacth.Clear();
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BeginUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoDa::BeginUndoTask(BSTR bstrUndo, BSTR bstrRedo)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrUndo);
	ChkComBstrArgN(bstrRedo);

	return m_qacth->BeginUndoTask(bstrUndo, bstrRedo);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#EndUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoDa::EndUndoTask()
{
	BEGIN_COM_METHOD;

	return m_qacth->EndUndoTask();

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#ContinueUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoDa::ContinueUndoTask()
{
	BEGIN_COM_METHOD;

	return m_qacth->ContinueUndoTask();

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#EndOuterUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoDa::EndOuterUndoTask()
{
	BEGIN_COM_METHOD;

	return m_qacth->EndOuterUndoTask();

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BreakUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoDa::BreakUndoTask(BSTR bstrUndo, BSTR bstrRedo)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrUndo);
	ChkComBstrArgN(bstrRedo);

	return m_qacth->BreakUndoTask(bstrUndo, bstrRedo);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#EndOuterUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoDa::Rollback()
{
	BEGIN_COM_METHOD;

	return m_qacth->Rollback(0);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GetActionHandler}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoDa::GetActionHandler(IActionHandler ** ppacth)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppacth);

	if (!m_qacth) // may the the case for the very first modification
		m_qacth.CreateInstance(CLSID_ActionHandler);
	*ppacth = m_qacth;
	m_qacth.Ptr()->AddRef();

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetActionHandler}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoDa::SetActionHandler(IActionHandler * pacth)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pacth);

	m_qacth = pacth;

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}


//:>********************************************************************************************
//:>	VwUndoDa and corresponding VwUndoAction methods
//:>********************************************************************************************

//:>--------------------------------------------------------------------------------------------
//:>	DeleteObj, DeleteObjOwner, and VwUndoDeleteAction.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::DeleteObj(HVO hvoObj)
{
	BEGIN_COM_METHOD;

	VwUndoActionPtr quact;
	quact.Attach(NewObj VwUndoDeleteAction(this, 0, 0, hvoObj, -1));
	RecordUndoAction(quact);

	return SuperDeleteObj(hvoObj);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

STDMETHODIMP VwUndoDa::DeleteObjOwner(HVO hvoOwner, HVO hvoObj, PropTag tag, int ihvo)
{
	BEGIN_COM_METHOD;

	// Set up an undo-action with the object to restore.
	VwUndoActionPtr quact = NULL;
	ObjPropRec oprKey(hvoOwner, tag);
	if (ihvo == -2)
	{
		// atomic property
		quact.Attach(NewObj VwUndoDeleteAction(this, hvoOwner, tag, hvoObj, ihvo));
	}
	else
	{
		ObjSeq osOld;
		if (m_hmoprsobj.Retrieve(oprKey, &osOld))
		{
			if (ihvo < 0)
			{
				// need to search for it
				for (ihvo = 0; ihvo < osOld.m_cobj && osOld.m_prghvo[ihvo] != hvoObj; ihvo++)
					;
				Assert(ihvo < osOld.m_cobj);
			}
			Assert(ihvo >= 0);
			quact.Attach(NewObj VwUndoDeleteAction(this, hvoOwner, tag, hvoObj, ihvo));
		}
	}
	RecordUndoAction(quact);

	return SuperDeleteObjOwner(hvoOwner, hvoObj, tag, ihvo);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoDeleteAction::VwUndoDeleteAction(VwUndoDa * puda, HVO hvoOwner, PropTag tag,
	HVO hvoObj, int ihvo)
	: VwUndoAction(puda, hvoOwner, tag)
{
	m_hvoDeleted = hvoObj;
	m_ihvo = ihvo;
}

STDMETHODIMP VwUndoDeleteAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);

	m_puda->m_shvoDeleted.Delete(m_hvoDeleted);
	HRESULT hr = S_OK;
	if (m_ihvo == -1)
	{
		// no previous owner
		Assert(m_hvoObj == 0);
		Assert(m_tag == 0);
	}
	else if (m_ihvo == -2)
	{
		// atomic property
		ObjPropRec oprKey(m_hvoObj, m_tag);
		m_puda->m_hmoprobj.Insert(oprKey, m_hvoDeleted, true);
		if (!fRefreshPending)
			CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, 0, 0, 0));
	}
	else
	{
		hr = m_puda->ReplaceAux(m_hvoObj, m_tag, m_ihvo, m_ihvo, &m_hvoDeleted, 1);
		if (!fRefreshPending)
			CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, m_ihvo, 1, 0));
	}
	m_fStateUndone = true;
	*pfSuccess = true;
	return hr;

	END_COM_METHOD(g_factDelAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoDeleteAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);

	HRESULT hr;
	if (m_ihvo == -1)
	{
		// no previous owner
		Assert(m_hvoObj == 0);
		Assert(m_tag == 0);
		hr = m_puda->SuperDeleteObj(m_hvoDeleted);
	}
	else
	{
		hr = m_puda->SuperDeleteObjOwner(m_hvoObj, m_hvoDeleted, m_tag, m_ihvo);
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, m_ihvo, 0, 1));
	}
	m_fStateUndone = false;
	*pfSuccess = true;
	return hr;

	END_COM_METHOD(g_factDelAct, IID_IUndoAction);
}

//:>--------------------------------------------------------------------------------------------
//:>	InsertNew and VwUndoInsertAction.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::InsertNew(HVO hvoObj, PropTag tag, int ihvoPreMin, int chvo,
	IVwStylesheet * pss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pss);

	// Set up an undo-action with the previous values.
	VwUndoInsertActionPtr quact;
	quact.Attach(NewObj VwUndoInsertAction(this, hvoObj, tag, ihvoPreMin + 1, chvo, pss));
	RecordUndoAction(quact);

	HRESULT hr = SuperClass::InsertNew(hvoObj, tag, ihvoPreMin, chvo, pss);

	// Get the HVOs of the new objects and store them in the undo-action.
	HVO * prghvo = NewObj HVO[chvo];
	for (int ihvo = 0; ihvo < chvo; ihvo++)
		CheckHr(get_VecItem(hvoObj, tag, ihvoPreMin + 1 + ihvo, prghvo + ihvo));
	quact->m_prghvoNew = prghvo;

	return hr;

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoInsertAction::VwUndoInsertAction(VwUndoDa * puda, HVO hvoOwner, PropTag tag,
	int ihvo, int chvo, IVwStylesheet * pss)
	: VwUndoAction(puda, hvoOwner, tag)
{
	m_ihvoMinIns = ihvo;
	m_chvo = chvo;
	m_pss = pss;
	m_prghvoNew = NULL;
}

VwUndoInsertAction::~VwUndoInsertAction()
{
	if (m_prghvoNew)
		delete[] m_prghvoNew;
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#Undo}
	Undo the action: delete the objects.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoInsertAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == false);

	HRESULT hr = S_OK;
	for (int ihvo = m_chvo; --ihvo >= 0; )
	{
		CheckHr(hr = m_puda->SuperDeleteObjOwner(m_hvoObj, m_prghvoNew[ihvo], m_tag,
			m_ihvoMinIns + ihvo));
	}
	if (!fRefreshPending)
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, m_ihvoMinIns, 0, m_chvo));

	m_fStateUndone = true;
	*pfSuccess = true;
	return hr;

	END_COM_METHOD(g_factInsAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoInsertAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == true);

	for (int ihvo = 0; ihvo < m_chvo; ihvo++)
		m_puda->m_shvoDeleted.Delete(m_prghvoNew[ihvo]);

	HRESULT hr = m_puda->ReplaceAux(m_hvoObj, m_tag,
		m_ihvoMinIns, m_ihvoMinIns, m_prghvoNew, m_chvo);
	CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, m_ihvoMinIns, m_chvo, 0));

	m_fStateUndone = false;
	*pfSuccess = true;
	return hr;

	END_COM_METHOD(g_factInsAct, IID_IUndoAction);
}


//:>--------------------------------------------------------------------------------------------
//:>	MakeNewObject and VwUndoMakeNewObjAction.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::MakeNewObject(int clid, HVO hvoOwner, PropTag tag, int ord,
	HVO * phvoNew)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(phvoNew);
	Assert(ord >= 0 || ord == -2); // seq or atomic; don't know how to do collection yet.
	HVO hvoOld = 0;
	if (ord == -2)
		CheckHr(get_ObjectProp(hvoOwner, tag, &hvoOld));

	CheckHr(SuperClass::MakeNewObject(clid, hvoOwner, tag, ord, phvoNew));
	// Set up an undo-action with the previous values.
	VwUndoMakeNewObjectActionPtr quact;
	quact.Attach(NewObj VwUndoMakeNewObjectAction(this, hvoOwner, tag, ord, *phvoNew, hvoOld));
	RecordUndoAction(quact);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoMakeNewObjectAction::VwUndoMakeNewObjectAction(VwUndoDa * puda, HVO hvoOwner, PropTag tag,
	int ihvo, HVO hvo, HVO hvoOld)
	: VwUndoAction(puda, hvoOwner, tag)
{
	m_hvo = hvo;
	m_ihvo = ihvo;
	m_hvoOld = hvoOld;
}

VwUndoMakeNewObjectAction::~VwUndoMakeNewObjectAction()
{
}


STDMETHODIMP VwUndoMakeNewObjectAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == false);

	CheckHr(m_puda->SuperDeleteObjOwner(m_hvoObj, m_hvo, m_tag, m_ihvo));
	if (m_hvoOld)
	{
		// Reinstate the old object, deleted as a side effect. Must be atomic, because we
		// make it zero for sequences.
		m_puda->m_shvoDeleted.Delete(m_hvoOld);
		CheckHr(m_puda->CacheObjProp(m_hvoObj, m_tag, m_hvoOld));
	}
	if (!fRefreshPending)
	{
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag,
			(m_ihvo >= 0 ? m_ihvo : 0), (m_hvoOld == 0 ? 0 : 1), 1));
	}

	m_fStateUndone = true;
	*pfSuccess = true;

	END_COM_METHOD(g_factInsAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoMakeNewObjectAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == true);

	m_puda->m_shvoDeleted.Delete(m_hvo);
	// Arguably, if m_hvoOld is non-zero, we should do something to get it into the list of deleted objects.
	// But, it can't have existed before the last Save, so it shouldn't need to be deleted.

	if (m_ihvo >= 0)
	{
		CheckHr(m_puda->ReplaceAux(m_hvoObj, m_tag, m_ihvo, m_ihvo, &m_hvo, 1));
	}
	else // must be -2, atomic.
	{
		Assert(m_ihvo == -2);
		CheckHr(m_puda->CacheObjProp(m_hvoObj, m_tag, m_hvo));
	}
	CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, (m_ihvo >= 0 ? m_ihvo : 0),
		1, (m_hvoOld == 0 ? 0 : 1)));

	m_fStateUndone = false;
	*pfSuccess = true;

	END_COM_METHOD(g_factInsAct, IID_IUndoAction);
}

//:>--------------------------------------------------------------------------------------------
//:>	MoveOwnSeq and VwUndoMoveAction.
//:>	ENHANCE SharonC/JohnT(?): implement properly. (This is not currently used by any
//:>	code that uses this class, so we haven't implemented the Undo capability
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::MoveOwnSeq(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart, int ihvoEnd,
	HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart)
{
	BEGIN_COM_METHOD;
	Assert(false);	// not needed for WorldPad

	// Something we don't know how to undo yet.
	if (m_qacth)
		m_qacth->Commit();
	return SuperClass::MoveOwnSeq(hvoSrcOwner, tagSrc, ihvoStart, ihvoEnd,
		hvoDstOwner, tagDst, ihvoDstStart);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

//:>--------------------------------------------------------------------------------------------
//:>	Replace and VwUndoReplaceAction.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::Replace(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
	HVO * prghvo, int chvoIns)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prghvo, chvoIns);

	// Set up an undo-action with the old and new values.
	int chvoDel = ihvoLim - ihvoMin;
	HVO * prghvoDel = NULL;
	if (chvoDel)
	{
		prghvoDel = NewObj HVO[chvoDel];
		for (int ihvoTmp = 0; ihvoTmp < chvoDel; ihvoTmp++)
			CheckHr(get_VecItem(hvoObj, tag, ihvoMin + ihvoTmp, prghvoDel + ihvoTmp));
	}
	HVO * prghvoIns = NULL;
	if (chvoIns)
	{
		prghvoIns = NewObj HVO[chvoIns];
		for (int ihvoTmp = 0; ihvoTmp < chvoIns; ihvoTmp++)
			*(prghvoIns + ihvoTmp) = *(prghvo + ihvoTmp);
	}

	VwUndoReplaceActionPtr quact;
	quact.Attach(NewObj VwUndoReplaceAction(this, hvoObj, tag,
		chvoIns, chvoDel, ihvoMin, prghvoIns, prghvoDel));
	RecordUndoAction(quact);

	return SuperReplace(hvoObj, tag, ihvoMin, ihvoLim, prghvo, chvoIns);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoReplaceAction::VwUndoReplaceAction(VwUndoDa * puda, HVO hvoOwner, PropTag tag,
	int chvoIns, int chvoDel, int ihvoMin, HVO * prghvoIns, HVO * prghvoDel)
	: VwUndoAction(puda, hvoOwner, tag)
{
	m_chvoIns = chvoIns;
	m_chvoDel = chvoDel;
	m_ihvoMin = ihvoMin;
	m_prghvoIns = prghvoIns;
	m_prghvoDel = prghvoDel;
}

VwUndoReplaceAction::~VwUndoReplaceAction()
{
	if (m_prghvoIns)
		delete[] m_prghvoIns;
	if (m_prghvoDel)
		delete[] m_prghvoDel;
}

STDMETHODIMP VwUndoReplaceAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == false);

	HRESULT hr = m_puda->SuperReplace(m_hvoObj, m_tag, m_ihvoMin, m_ihvoMin + m_chvoIns,
		m_prghvoDel, m_chvoDel);
	if (!fRefreshPending)
	{
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, m_ihvoMin,
			m_chvoDel, m_chvoIns));
	}

	m_fStateUndone = true;
	*pfSuccess = true;
	return hr;

	END_COM_METHOD(g_factRepAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoReplaceAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == true);

	HRESULT hr = m_puda->SuperReplace(m_hvoObj, m_tag, m_ihvoMin, m_ihvoMin + m_chvoDel,
		m_prghvoIns, m_chvoIns);
	CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, m_ihvoMin,
		m_chvoIns, m_chvoDel));

	m_fStateUndone = false;
	*pfSuccess = true;
	return hr;

	END_COM_METHOD(g_factRepAct, IID_IUndoAction);
}


//:>--------------------------------------------------------------------------------------------
//:>	SetBinary and VwUndoSetBinaryAction.
//:>	ENHANCE SharonC(?): this code has not been tested at all!  It isn't used by anything
//:>	that uses this class, so for now it doesn't matter.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::SetBinary(HVO hvo, PropTag tag, byte * prgb, int cb)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgb, cb);

	// Set up an undo-action with the previous value.
	int cbOld;
	CheckHr(BinaryPropRgb(hvo, tag, NULL, 0, &cbOld));
	byte * prgbOld = NewObj byte[cbOld];
	CheckHr(BinaryPropRgb(hvo, tag, prgbOld, cbOld, &cbOld));
	VwUndoActionPtr quact;
	quact.Attach(NewObj VwUndoSetBinaryAction(this, hvo, tag, prgbOld, cbOld));
	RecordUndoAction(quact);

	return SuperSetBinary(hvo, tag, prgb, cb);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoSetBinaryAction::VwUndoSetBinaryAction(VwUndoDa * puda, HVO hvo, PropTag tag,
	byte * prgb, int cb)
	: VwUndoAction(puda, hvo, tag)
{
	m_prgbOther = prgb;
	m_cbOther = cb;
}

STDMETHODIMP VwUndoSetBinaryAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(true, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetBinAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoSetBinaryAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(false, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetBinAct, IID_IUndoAction);
}

HRESULT VwUndoSetBinaryAction::UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending)
{
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == !fUndo);

	byte * prgbNext = m_prgbOther;
	int cbNext = m_cbOther;
	CheckHr(m_puda->BinaryPropRgb(m_hvoObj, m_tag, NULL, 0, &m_cbOther));
	m_prgbOther = NewObj byte[m_cbOther];
	CheckHr(m_puda->BinaryPropRgb(m_hvoObj, m_tag, m_prgbOther, m_cbOther, &m_cbOther));

	HRESULT hr = m_puda->SuperSetBinary(m_hvoObj, m_tag, prgbNext, cbNext);
	if (!fRefreshPending)
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, 0, 0, 0));

	if (prgbNext)
		delete[] prgbNext;

	m_fStateUndone = fUndo;
	*pfSuccess = true;
	return hr;
}

//:>--------------------------------------------------------------------------------------------
//:>	SetObjProp, SetInt, and VwUndoSetIntAction.
//:>	ENHANCE SharonC(?): this code has not been tested at all!  It isn't used by anything
//:>	that uses this class, so for now it doesn't matter.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::SetObjProp(HVO hvo, PropTag tag, HVO hvoVal)
{
	BEGIN_COM_METHOD;

	// Set up an undo-action with the previous value.
	HVO hvoOld;
	CheckHr(this->get_ObjectProp(hvo, tag, &hvoOld));
	VwUndoActionPtr quact;
	quact.Attach(NewObj VwUndoSetIntAction(this, hvo, tag, (int)hvoOld, true));
	RecordUndoAction(quact);

	return SuperSetObjProp(hvo, tag, hvoVal);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

STDMETHODIMP VwUndoDa::SetInt(HVO hvo, PropTag tag, int n)
{
	BEGIN_COM_METHOD;

	// Set up an undo-action with the previous value.
	int nOld;
	CheckHr(get_IntProp(hvo, tag, &nOld));
	VwUndoActionPtr quact;
	quact.Attach(NewObj VwUndoSetIntAction(this, hvo, tag, nOld, false));
	RecordUndoAction(quact);

	return SuperSetInt(hvo, tag, n);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoSetIntAction::VwUndoSetIntAction(VwUndoDa * puda, HVO hvoObj, PropTag tag,
	int n, bool fObj)
	: VwUndoAction(puda, hvoObj, tag)
{
	m_nOther = n;
	m_fObj = fObj;
}

STDMETHODIMP VwUndoSetIntAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(true, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetIntAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoSetIntAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(false, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetIntAct, IID_IUndoAction);
}

HRESULT VwUndoSetIntAction::UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending)
{
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == !fUndo);

	int nNext = m_nOther;

	HRESULT hr;
	if (m_fObj)
	{
		HVO hvoOther;
		CheckHr(m_puda->get_ObjectProp(m_hvoObj, m_tag, &hvoOther));
		m_nOther = (int)hvoOther;
		hr = m_puda->SuperSetObjProp(m_hvoObj, m_tag, (HVO)nNext);
	}
	else
	{
		CheckHr(m_puda->get_IntProp(m_hvoObj, m_tag, &m_nOther));
		hr = m_puda->SuperSetInt(m_hvoObj, m_tag, nNext);
	}

	if (!fRefreshPending)
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, 0, 0, 0));

	m_fStateUndone = fUndo;
	*pfSuccess = true;
	return hr;
}


//:>--------------------------------------------------------------------------------------------
//:>	SetTime, SetInt64, and VwUndoSetTimeAction.
//:>	ENHANCE SharonC(?): this code has not been tested at all!  It isn't used by anything
//:>	that uses this class, so for now it doesn't matter.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::SetTime(HVO hvo, PropTag tag, int64 tim)
{
	BEGIN_COM_METHOD;

	// Set up an undo-action with the previous value.
	int64 timOld;
	CheckHr(this->get_TimeProp(hvo, tag, &timOld));
	VwUndoActionPtr quact;
	quact.Attach(NewObj VwUndoSetTimeAction(this, hvo, tag, (int)timOld, true));
	RecordUndoAction(quact);

	return SuperSetTime(hvo, tag, tim);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

STDMETHODIMP VwUndoDa::SetInt64(HVO hvo, PropTag tag, int64 lln)
{
	BEGIN_COM_METHOD;

	// Set up an undo-action with the previous value.
	int64 llnOld;
	CheckHr(get_Int64Prop(hvo, tag, &llnOld));
	VwUndoActionPtr quact;
	quact.Attach(NewObj VwUndoSetTimeAction(this, hvo, tag, llnOld, false));
	RecordUndoAction(quact);

	return SuperSetInt64(hvo, tag, lln);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoSetTimeAction::VwUndoSetTimeAction(VwUndoDa * puda, HVO hvoObj, PropTag tag,
	int64 lln, bool fTime)
	: VwUndoAction(puda, hvoObj, tag)
{
	m_llnOther = lln;
	m_fTime = fTime;
}

STDMETHODIMP VwUndoSetTimeAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(true, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetTimeAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoSetTimeAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(false, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetTimeAct, IID_IUndoAction);
}

HRESULT VwUndoSetTimeAction::UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending)
{
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == !fUndo);

	int64 llnNext = m_llnOther;

	HRESULT hr;
	if (m_fTime)
	{
		CheckHr(m_puda->get_TimeProp(m_hvoObj, m_tag, &m_llnOther));
		hr = m_puda->SuperSetTime(m_hvoObj, m_tag, llnNext);
	}
	else
	{
		CheckHr(m_puda->get_Int64Prop(m_hvoObj, m_tag, &m_llnOther));
		hr = m_puda->SuperSetInt64(m_hvoObj, m_tag, llnNext);
	}
	if (!fRefreshPending)
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, 0, 0, 0));

	m_fStateUndone = fUndo;
	*pfSuccess = true;
	return hr;
}


//:>--------------------------------------------------------------------------------------------
//:>	SetGuid and VwUndoSetGuidAction.
//:>	ENHANCE SharonC(?): this code has not been tested at all! How do we handle the situation
//:>	where the previous value was NULL?
//:>	It isn't used by anything
//:>	that uses this class, so for now it doesn't matter.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::SetGuid(HVO hvo, PropTag tag, GUID uid)
{
	BEGIN_COM_METHOD;

	// Set up an undo-action with the previous value.
	GUID uidOld;
	CheckHr(get_GuidProp(hvo, tag, &uidOld));
	VwUndoSetGuidActionPtr quact;
	quact.Attach(NewObj VwUndoSetGuidAction(this, hvo, tag, uidOld));
	RecordUndoAction(quact);

	return SuperSetGuid(hvo, tag, uid);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoSetGuidAction::VwUndoSetGuidAction(VwUndoDa * puda, HVO hvoObj, PropTag tag,
	GUID uid)
	: VwUndoAction(puda, hvoObj, tag)
{
	m_uidOther = uid;
}

STDMETHODIMP VwUndoSetGuidAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(true, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetGuidAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoSetGuidAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(false, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetGuidAct, IID_IUndoAction);
}

HRESULT VwUndoSetGuidAction::UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending)
{
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == !fUndo);

	GUID uidNext = m_uidOther;
	CheckHr(m_puda->get_GuidProp(m_hvoObj, m_tag, &m_uidOther));
	HRESULT hr = m_puda->SuperSetGuid(m_hvoObj, m_tag, uidNext);
	if (!fRefreshPending)
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, 0, 0, 0));

	m_fStateUndone = fUndo;
	*pfSuccess = true;
	return hr;
}


//:>--------------------------------------------------------------------------------------------
//:>	SetString, SetMultiStringAlt, and VwUndoSetStringAction.
//:>	ENHANCE SharonC(?): the multi-string code has not been tested at all!  It isn't used by
//:>	anything that uses this class, so for now it doesn't matter.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::SetString(HVO hvo, PropTag tag, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptss);

	// Set up an undo-action with the previous value.
	ITsStringPtr qtssOld;
	CheckHr(get_StringProp(hvo, tag, &qtssOld));
	VwUndoActionPtr quact;
	quact.Attach(NewObj VwUndoSetStringAction(this, hvo, tag, -1, qtssOld));
	RecordUndoAction(quact);

	return SuperSetString(hvo, tag, ptss);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

STDMETHODIMP VwUndoDa::SetMultiStringAlt(HVO hvo, PropTag tag, int ws, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptss);
	Assert(ws != -1);

	// Set up an undo-action with the previous value.
	ITsStringPtr qtssOld;
	CheckHr(get_MultiStringAlt(hvo, tag, ws, &qtssOld));
	VwUndoActionPtr quact;
	quact.Attach(NewObj VwUndoSetStringAction(this, hvo, tag, ws, qtssOld));
	RecordUndoAction(quact);

	return SuperSetMultiStringAlt(hvo, tag, ws, ptss);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoSetStringAction::VwUndoSetStringAction(VwUndoDa * puda, HVO hvoObj, PropTag tag,
	int ws, ITsString * ptss)
	: VwUndoAction(puda, hvoObj, tag)
{
	m_ws = ws;
	m_qtssOther = ptss;
}

STDMETHODIMP VwUndoSetStringAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(true, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetStrAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoSetStringAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(false, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetStrAct, IID_IUndoAction);
}

HRESULT VwUndoSetStringAction::UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending)
{
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == !fUndo);

	ITsStringPtr qtssNext = m_qtssOther;

	HRESULT hr;
	if (m_ws == -1)
	{
		CheckHr(m_puda->get_StringProp(m_hvoObj, m_tag, &m_qtssOther));
		hr = m_puda->SuperSetString(m_hvoObj, m_tag, qtssNext);
	}
	else
	{
		CheckHr(m_puda->get_MultiStringAlt(m_hvoObj, m_tag, m_ws, &m_qtssOther));
		hr = m_puda->SuperSetMultiStringAlt(m_hvoObj, m_tag, m_ws, qtssNext);
	}
	int cchNext;
	CheckHr(qtssNext->get_Length(&cchNext));
	int cchOther;
	CheckHr(qtssNext->get_Length(&cchOther));
	// Note that for a MS property, specs call for passing the ws in the 'insert' parameter
	// to indicate which alternative changed. Otherwise we pass zero indicating the first
	// character (may have) changed.
	if (!fRefreshPending)
	{
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag,
			(m_ws == -1 ? 0 : m_ws), cchNext, cchOther));
	}

	m_fStateUndone = fUndo;
	*pfSuccess = true;
	return hr;
}


//:>--------------------------------------------------------------------------------------------
//:>	SetUnicode and VwUndoSetUnicodeAction.
//:>	ENHANCE SharonC(?): this code has not been tested at all!  It isn't used by
//:>	anything that uses this class, so for now it doesn't matter.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::SetUnicode(HVO hvo, PropTag tag, OLECHAR * prgch, int cch)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgch, cch);

	// Set up an undo-action with the previous value.
	int cchOld;
	CheckHr(UnicodePropRgch(hvo, tag, NULL, 0, &cchOld));
	OLECHAR * prgchOld = NewObj OLECHAR[cchOld];
	CheckHr(UnicodePropRgch(hvo, tag, prgchOld, cchOld, &cchOld));
	VwUndoActionPtr quact;
	quact.Attach(NewObj VwUndoSetUnicodeAction(this, hvo, tag, prgchOld, cchOld));
	RecordUndoAction(quact);

	return SuperSetUnicode(hvo, tag, prgch, cch);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoSetUnicodeAction::VwUndoSetUnicodeAction(VwUndoDa * puda, HVO hvo, PropTag tag,
	OLECHAR * prgch, int cch)
	: VwUndoAction(puda, hvo, tag)
{
	m_prgchOther = prgch;
	m_cchOther = cch;
}

STDMETHODIMP VwUndoSetUnicodeAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(true, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetUniAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoSetUnicodeAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(false, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetUniAct, IID_IUndoAction);
}

HRESULT VwUndoSetUnicodeAction::UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending)
{
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == !fUndo);

	OLECHAR * prgchNext = m_prgchOther;
	int cchNext = m_cchOther;
	CheckHr(m_puda->UnicodePropRgch(m_hvoObj, m_tag, NULL, 0, &m_cchOther));
	m_prgchOther = NewObj OLECHAR[m_cchOther];
	CheckHr(m_puda->UnicodePropRgch(m_hvoObj, m_tag, m_prgchOther, m_cchOther, &m_cchOther));

	HRESULT hr = m_puda->SuperSetUnicode(m_hvoObj, m_tag, prgchNext, cchNext);
	if (!fRefreshPending)
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, 0, 0, 0));

	if (prgchNext)
		delete[] prgchNext;

	m_fStateUndone = fUndo;
	*pfSuccess = true;
	return hr;
}

//:>--------------------------------------------------------------------------------------------
//:>	SetUnknown and VwUndoSetUnknownAction.
//:>------------------------------------------------------------------------------------------*/

STDMETHODIMP VwUndoDa::SetUnknown(HVO hvo, PropTag tag, IUnknown * punk)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(punk);

	// Set up an undo-action with the previous value.
	IUnknownPtr qunkOld;
	CheckHr(get_UnknownProp(hvo, tag, &qunkOld));
	VwUndoSetUnknownActionPtr quact;
	quact.Attach(NewObj VwUndoSetUnknownAction(this, hvo, tag, qunkOld));
	RecordUndoAction(quact);

	return SuperSetUnknown(hvo, tag, punk);

	END_COM_METHOD(g_factDa, IID_ISilDataAccess);
}

VwUndoSetUnknownAction::VwUndoSetUnknownAction(VwUndoDa * puda, HVO hvoObj, PropTag tag,
	IUnknown * punk)
	: VwUndoAction(puda, hvoObj, tag)
{
	m_qunkOther = punk;
}

STDMETHODIMP VwUndoSetUnknownAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(true, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetUnkAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoSetUnknownAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(false, pfSuccess, fRefreshPending);

	END_COM_METHOD(g_factSetUnkAct, IID_IUndoAction);
}

HRESULT VwUndoSetUnknownAction::UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending)
{
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == !fUndo);

	IUnknownPtr qunkNext = m_qunkOther;
	CheckHr(m_puda->get_UnknownProp(m_hvoObj, m_tag, &m_qunkOther));
	HRESULT hr = m_puda->SuperSetUnknown(m_hvoObj, m_tag, qunkNext);
	if (!fRefreshPending)
		CheckHr(m_puda->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_tag, 0, 0, 0));

	m_fStateUndone = fUndo;
	*pfSuccess = true;
	return hr;
}

// Stuff that depends on AppCore.
#ifndef VIEWSDLL

//:>--------------------------------------------------------------------------------------------
//:>	VwUndoSetStylesheetAction and VwUndoSetStyleAction - these are created by the
//:>	AfStylesheet class.
//:>------------------------------------------------------------------------------------------*/

VwUndoStylesheetAction::VwUndoStylesheetAction(VwUndoDa * puda, AfStylesheet * pasts,
	bool fForUndo)
	: VwUndoAction(puda, 0, 0)
{
	m_pasts = pasts;
	m_fForUndo = fForUndo;
}

VwUndoStyleAction::VwUndoStyleAction(VwUndoDa * puda, AfStylesheet * pasts,
	HVO hvoStyle, StrUni stuName, bool fDeleted)
	: VwUndoStylesheetAction(puda, pasts, false)
{
	m_hvoStyle = hvoStyle;
	m_stuName = stuName;
	m_fDeleted = fDeleted;
}

STDMETHODIMP VwUndoStylesheetAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(true, pfSuccess);

	END_COM_METHOD(g_factStyleAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoStylesheetAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(false, pfSuccess);

	END_COM_METHOD(g_factStyleAct, IID_IUndoAction);
}

HRESULT VwUndoStylesheetAction::UndoRedo(bool fUndo, ComBool * pfSuccess)
{
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == !fUndo);

	if (fUndo == m_fForUndo)
		m_pasts->ComputeDerivedStyles();
	// Ask all views to redraw based on the modified stylesheet.
	if (AfApp::Papp()) // -- just in case used in a control some day
	{
		Vector<AfMainWndPtr> &vqafw = AfApp::Papp()->GetMainWindows();
		for (int i = 0; i < vqafw.Size(); i++)
		{
			vqafw[i]->OnStylesheetChange();
		}
	}

	m_fStateUndone = fUndo;
	*pfSuccess = true;
	return S_OK;
}

HRESULT VwUndoStyleAction::UndoRedo(bool fUndo, ComBool * pfSuccess)
{
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == !fUndo);

	HRESULT hr;
	if (m_fDeleted == fUndo)
	{
		// Insert the style
		hr = m_pasts->UndoDeletedStyle(m_hvoStyle);
	}
	else
	{
		// Delete the style.
//		if (m_stuName == L"")
//		{
//			SmartBstr sbstrName;
//			CheckHr(m_puda->get_UnicodeProp(m_hvoStyle, kflidStStyle_Name, &sbstrName));
//			StrUni stuName(sbstrName.Chars(), sbstrName.Length());
//			m_stuName = stuName;
//		}

		hr = m_pasts->UndoInsertedStyle(m_hvoStyle, m_stuName);
	}

	// Shouldn't need to call ComputeDerivedStyles; there should be VwUndoStylesheetActions
	// to take care of it.

	m_fStateUndone = fUndo;
	*pfSuccess = true;
	return hr;
}
#endif

// The main Views DLL can't use this class, because it depends on stuff from AppCore.
// fortunately it also doesn't need it. This constant is defined in the Views make file.
#ifndef VIEWSDLL

//:>--------------------------------------------------------------------------------------------
//:>	VwUndoSelectionAction - these are created by the AfVwRootSite class.
//:>------------------------------------------------------------------------------------------*/

VwUndoSelectionAction::VwUndoSelectionAction(VwUndoDa * puda, AfVwRootSite * pavrs,
	IVwSelection * psel, bool fForUndo)
	: VwUndoAction(puda, 0, 0)
{
	pavrs->get_RootBox(&m_prootb);
	// Don't preserve a pointer to this object; this creates a circular loop.
	m_prootb->Release();

	// Store the information about the selection.
	m_avsi.LoadVisible(m_prootb);
	// We don't need to preserve this information, and the smart pointer creates a circular
	// loop that causes memory leaks.
	m_avsi.m_qttp = NULL;

	m_fForUndo = fForUndo;
}

STDMETHODIMP VwUndoSelectionAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(true, pfSuccess);

	END_COM_METHOD(g_factSelAct, IID_IUndoAction);
}

STDMETHODIMP VwUndoSelectionAction::Redo(ComBool fRefreshPending, ComBool *  pfSuccess)
{
	BEGIN_COM_METHOD;

	return UndoRedo(false, pfSuccess);

	END_COM_METHOD(g_factSelAct, IID_IUndoAction);
}

HRESULT VwUndoSelectionAction::UndoRedo(bool fUndo, ComBool * pfSuccess)
{
	ChkComOutPtr(pfSuccess);
	Assert(m_fStateUndone == !fUndo);

	bool fSuccess;
	if (fUndo == m_fForUndo)
	{
		// Create a new selection, if possible.
		IVwSelectionPtr qsel;
		fSuccess = m_avsi.MakeBest(m_prootb, true, &qsel);
	}

	m_fStateUndone = fUndo;
	*pfSuccess = true;

	// ENHANCE: should we return some error result if we couldn't create a selection?
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Return true if this is the kind of action that represents a real change to the
	data; false if it is just cleaning up the user interface (eg, replacing the selection).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwUndoSelectionAction::get_IsDataChange(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);

	*pfRet = false;

	END_COM_METHOD(g_fact, IID_IUndoAction);
}
#endif // #ifndef VIEWSDLL


//:>********************************************************************************************
//:>	General VwUndoDa methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Store the given undo-action in the action handler.
----------------------------------------------------------------------------------------------*/
void VwUndoDa::RecordUndoAction(VwUndoAction * puact)
{
	if (!puact)
		return;

	CheckHr(m_qacth->AddAction(puact));
}
