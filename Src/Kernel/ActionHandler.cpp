/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ActionHandler.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file contains class definitions for the ActionHandler class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

// Enable the following line for debugging purposes. Output goes to Output window.
//#define DEBUG_ACTION_HANDLER


//:>********************************************************************************************
//:>	ActionHandler methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
ActionHandler::ActionHandler()
{
	m_cref = 1;
	m_nDepth = 0;
	m_iuactCurr = -1;
	m_iCurrSeq = -1;
	m_fStartedNext = false;
	ModuleEntry::ModuleAddRef();
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
ActionHandler::~ActionHandler()
{
	ModuleEntry::ModuleRelease();
}


//:>********************************************************************************************
//:>	ActionHandler - Generic factory stuff to allow creating an instance w/ CoCreateInstance.
//:>********************************************************************************************

static GenericFactory g_factActh(
	_T("SIL.Views.ActionHandler"),
	&CLSID_ActionHandler,
	_T("SIL Action Handler"),
	_T("Apartment"),
	&ActionHandler::CreateCom);


void ActionHandler::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<ActionHandler> qzacth;
	// Ref count initially 1
	qzacth.Attach(NewObj ActionHandler());
	CheckHr(qzacth->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	ActionHandler - IUnknown Methods
//:>********************************************************************************************

STDMETHODIMP ActionHandler::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IActionHandler *>(this));
	else if (iid == IID_IActionHandler)
		*ppv = static_cast<IActionHandler *>(this);
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

STDMETHODIMP_(ULONG) ActionHandler::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}

STDMETHODIMP_(ULONG) ActionHandler::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}

//:>********************************************************************************************
//:>	ActionHandler - IActionHandler Methods
//:>********************************************************************************************

STDMETHODIMP ActionHandler::get_IsUndoOrRedoInProgress(ComBool * pfInProgress)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfInProgress);
	*pfInProgress = m_fUndoOrRedoInProgress;
	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#BeginUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::BeginUndoTask(BSTR bstrUndo, BSTR bstrRedo)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrUndo);
	ChkComBstrArgN(bstrRedo);

	if (m_fUndoOrRedoInProgress)
		return S_OK;

	StrUni stuUndo(bstrUndo);
	StrUni stuRedo(bstrRedo);

	if (m_nDepth == 0 && !m_fStartedNext)
	{
		// Outer task.

		CleanUpEmptyTasks();

		// Don't clear the redo stack until we're sure we have at least one action
		// for this task. Instead, just remember the labels and set a flag.
		// Clearing the variables here causes the values from stuUndo to be copied below.
		m_stuNextUndo.Clear();
		m_stuNextRedo.Clear();
		m_fStartedNext = true;
		m_fDataChangeAction = false;

		//::OutputDebugStringA("Begin Undo Task - started next\n");
		//StrAnsi sta;
		//sta.Format("depth = %d; m_fStartedNext = %d; action %d of %d; seq %d of %d; marks %d%n",
		//	m_nDepth, m_fStartedNext, m_iuactCurr, m_vquact.Size(), m_iCurrSeq, m_viSeqStart.Size(), m_viMarks.Size());
		//::OutputDebugStringA(sta.Chars());
	}
	// Otherwise, this task will simply be embedded in the outer task.

	++m_nDepth;

	// However, if this task has labels and the outer one didn't, make use of these labels.
	// This can happen, for example, when ContinueUndoTask has to start a new transaction
	// because of a prior Un/Redo.
	if (m_stuNextUndo.Length() == 0)
	{
		// TODO JohnT(SharonC): temporary code; remove it when we get this issue straightened out.
		if (stuUndo == L"-")
			m_stuNextUndo = L"Undo";
		else
			m_stuNextUndo = stuUndo;
	}
	if (m_stuNextRedo.Length() == 0)
	{
		if (stuRedo == L"-")
			m_stuNextRedo = L"Redo";
		else
			m_stuNextRedo = stuRedo;
	}

#ifdef DEBUG_ACTION_HANDLER
	StrAnsi sta(bstrUndo);
	if (sta.Length()) // Skip calls without string (eliminates idle loop calls)
	{
		sta.Replace(0, 0, "BeginUndoTask: ");
		sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
			m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
		::OutputDebugStringA(sta.Chars());
	}
#endif DEBUG_ACTION_HANDLER

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#EndUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::EndUndoTask()
{
	BEGIN_COM_METHOD;

	if (m_fUndoOrRedoInProgress)
		return S_OK;

	if (m_nDepth <= 0)
	{
		return E_UNEXPECTED;
	}
	else
	{
		--m_nDepth;
		if (m_nDepth == 0)
		{
			// cleans no-op undo tasks
			EndOuterUndoTask();
		}
	}

#ifdef DEBUG_ACTION_HANDLER
	StrAnsi sta;
	sta.Format("EndUndoTask:");
	sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
		m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
	::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#ContinueUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::ContinueUndoTask()
{
	BEGIN_COM_METHOD;

	if (m_fUndoOrRedoInProgress)
		return S_OK;

	if (!m_fCanContinueTask)
	{
		// No task available to continue; start one. This is true initially, when the stack
		// is empty, or after an Undo or Redo, when the task on the top of the stack is not
		// the most recent thing the user did.
		BeginUndoTask(NULL, NULL);
	}
	else
	{
		++m_nDepth;
	}

/* Probably don't want this because it is called in RecMainWnd::OnIdle
#ifdef DEBUG_ACTION_HANDLER
	StrAnsi sta;
	sta.Format("ContinueUndoTask:");
	sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
		m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
	::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER*/

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#EndOuterUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::EndOuterUndoTask()
{
	BEGIN_COM_METHOD;

	if (m_fUndoOrRedoInProgress)
		return S_OK;

	CleanUpEmptyTasks();

	m_nDepth = 0;
	m_fStartedNext = false;

/* Probably don't want this because it is called in RecMainWnd::OnIdle
#ifdef DEBUG_ACTION_HANDLER
	StrAnsi sta;
	sta.Format("EndOuterUndoTask:");
	sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
		m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
	::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER*/

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#BreakUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::BreakUndoTask(BSTR bstrUndo, BSTR bstrRedo)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrUndo);
	ChkComBstrArg(bstrRedo);

	if (m_fUndoOrRedoInProgress)
		return S_OK;

	int nSaveDepth = m_nDepth;

	EndOuterUndoTask();

	BeginUndoTask(bstrUndo, bstrRedo);

	m_nDepth = nSaveDepth;

#ifdef DEBUG_ACTION_HANDLER
	StrAnsi sta;
	sta.Format("BreakUndoTask:");
	sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
		m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
	::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#StartSeq}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::StartSeq(BSTR bstrUndo, BSTR bstrRedo, IUndoAction * puact)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrUndo);
	ChkComBstrArg(bstrRedo);
	ChkComArgPtr(puact);

	if (m_fUndoOrRedoInProgress)
		return S_OK;

	BeginUndoTask(bstrUndo, bstrRedo);
	// Add the current action.
	AddActionAux(puact);
	EndUndoTask();

#ifdef DEBUG_ACTION_HANDLER
	StrAnsi sta(bstrUndo);
	sta.Replace(0, 0, "StartSeq:");
	sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
		m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
	::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#AddAction}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::AddAction(IUndoAction * puact)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(puact);

	if (m_fUndoOrRedoInProgress)
		return S_OK;

	if(m_fCreateMarkIfNeeded && m_viMarks.Size() == 0)
	{
		// m_fCreateMarkIfNeeded probably didn't get reset (TE-4856)
		Assert(m_nDepth <= 0 || m_fStartedNext);

		int hmark;
		CheckHr(Mark(&hmark));
		Assert(hmark == 1); // Should only have 1 mark!
	}
	ContinueUndoTask();
	AddActionAux(puact);
	EndUndoTask();

#ifdef DEBUG_ACTION_HANDLER
	StrAnsi sta;
	sta.Format("AddAction:");
	sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
		m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
	::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Add an action to the list.
----------------------------------------------------------------------------------------------*/
void ActionHandler::AddActionAux(IUndoAction * puact)
{
	CleanUpRedoActions(false);
	if (m_fStartedNext)
	{
		m_vstuUndo.Push(m_stuNextUndo);
		m_vstuRedo.Push(m_stuNextRedo);
		m_viSeqStart.Push(m_vquact.Size()); // new sequence where the action will go
		m_iCurrSeq = m_viSeqStart.Size() - 1;
		m_fStartedNext = false;
	}

	// The current action should be the last one in the stack since we just cleared all
	// the redo actions.
	Assert(m_iuactCurr == m_vquact.Size() - 1);

	// Marks shouldn't straddle tasks; the most recent mark should correspond to the beginning
	// of some task. We test for this here because it is only here that we know we have a
	// legitimate task with actions inside.
// The test framework starts an outer level of undo, which invalidates this sanity check.
// JohnT doesn't remember this check ever catching a real bug anyway!
//#if _DEBUG
//	if (m_viMarks.Size())
//	{
//		int iuactTopMark = m_viMarks[m_viMarks.Size() - 1];
//		int iSeq;
//		for (iSeq = 0; iSeq < m_viSeqStart.Size(); iSeq++)
//		{
//			if (m_viSeqStart[iSeq] == iuactTopMark)
//				break;
//		}
//		Assert(iSeq < m_viSeqStart.Size());	// die if we didn't find such a sequence
//	}
//#endif // _DEBUG

	// Add an action to the stack
	m_vquact.Push(puact);
	m_iuactCurr = m_vquact.Size() - 1;
	ComBool f;
	CheckHr(puact->IsDataChange(&f));
	if (f)
	{
		// We've added something real to the current action; that makes it one that
		// can be continued, if something wants to after closing it.
		m_fCanContinueTask = true;
		m_fDataChangeAction = true;
	}

	//::OutputDebugStringA("After AddActionAux\n");
	//StrAnsi sta;
	//sta.Format("depth = %d; m_fStartedNext = %d; action %d of %d; seq %d of %d; marks %d%n",
	//	m_nDepth, m_fStartedNext, m_iuactCurr, m_vquact.Size(), m_iCurrSeq, m_viSeqStart.Size(), m_viMarks.Size());
	//::OutputDebugStringA(sta.Chars());
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#GetUndoText}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::GetUndoText(BSTR * pbstrUndo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrUndo);

	if (m_iCurrSeq < 0)
	{
		// Can't Undo
		*pbstrUndo = NULL;
	}
	else
	{
		m_vstuUndo[m_iCurrSeq].GetBstr(pbstrUndo);
	}

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#GetUndoText}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::GetUndoTextN(int iAct, BSTR * pbstrUndo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrUndo);

	if (iAct < 0 || iAct > m_vstuUndo.Size())
		return E_INVALIDARG;

	m_vstuUndo[iAct].GetBstr(pbstrUndo);

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#GetRedoText}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::GetRedoText(BSTR * pbstrRedo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrRedo);

	if (m_iCurrSeq >= m_viSeqStart.Size() - 1)
	{
		// Can't Redo
		*pbstrRedo = NULL;
	}
	else
	{
		m_vstuRedo[m_iCurrSeq + 1].GetBstr(pbstrRedo);
	}

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#GetRedoText}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::GetRedoTextN(int iAct, BSTR * pbstrRedo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrRedo);

	int iReal = m_iCurrSeq + 1 + iAct;

	if (iReal < 0 || iReal > m_vstuRedo.Size())
		return E_INVALIDARG;

	m_vstuRedo[iReal].GetBstr(pbstrRedo);

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#CanUndo}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::CanUndo(ComBool * pfCanUndo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfCanUndo);

	*pfCanUndo = (m_iuactCurr > -1);

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#CanRedo}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::CanRedo(ComBool * pfCanRedo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfCanRedo);

	*pfCanRedo = (m_iuactCurr < m_vquact.Size() - 1);

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#Undo}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::Undo(UndoResult * pures)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pures);

	*pures = kuresSuccess;

	Assert(m_iCurrSeq >= 0);
	Assert(m_vquact.Size() > 0);

	HRESULT hr = E_FAIL;
	HRESULT hrFinal = S_OK;

	m_fCanContinueTask = false; // Can't 'continue' the previous task from before the Undo.
	m_fUndoOrRedoInProgress = true;

	try // to ensure in progress gets cleared.
	{
		int handle;
		// Start a 'transaction' if possible to group together all the stuff we're going to Undo.
		if (m_qundg)
		{
			m_qundg->BeginGroup(&handle);
		}

		// Loop through all of the actions that are about to be undone and see if any of them
		// require a refresh.
		Vector<long> vhvoCreatedObjects;
		bool fNeedsRefresh = false;
		for (int i = m_iuactCurr; (m_iCurrSeq >= 0) && (i >= m_viSeqStart[m_iCurrSeq]); i--)
		{
			CheckHr(m_vquact[i]->put_SuppressNotification(true));

			ComBool fThisNeedsRefresh;
			CheckHr(m_vquact[i]->RequiresRefresh(&fThisNeedsRefresh));
			if (fThisNeedsRefresh)
			{
				fNeedsRefresh = true;
				break;
			}
		}

		if (fNeedsRefresh)
			*pures = kuresRefresh;

		// Undo all actions from the current action back to the first action of the current action
		// sequence.
		bool fRedoable = true;
		IgnoreHr(hrFinal = CallUndo(pures, fRedoable, true, fNeedsRefresh));

		// Now do all the PropChangeds
		for (int i = m_iuactCurr; (m_iCurrSeq >= 0) && (i >= m_viSeqStart[m_iCurrSeq]); i--)
		{
			CheckHr(m_vquact[i]->put_SuppressNotification(false));
		}

		// REVIEW (TimS/EberhardB): What do we do if the call to CallUndo above failed somewhere?
		// Should we do this at all? Or just until the failed action? Or everything?
		UndoResult uresTmp;
		IgnoreHr(hr = CallUndo(&uresTmp, fRedoable, false, fNeedsRefresh));
		if (FAILED(hr))
		{
			hrFinal = hr;
			*pures = uresTmp;
		}
		else if (uresTmp == kuresFailed)
		{
			*pures = uresTmp;
		}

		// dlh testing
		if (*pures == kuresError || * pures == kuresFailed)
		{
			if (m_qundg)
				m_qundg->CancelGroup(handle);
			EmptyStack();
		}
		else // normal success case
		{
			// Set the current action and start of the current action sequence.
			if (m_iCurrSeq >= 0)
			{
				m_iuactCurr = m_viSeqStart[m_iCurrSeq] - 1;
				m_iCurrSeq = m_iCurrSeq - 1;
			}
			if (m_qundg)
				m_qundg->EndGroup(handle);
		}

		// Delete any marks that now point after current undo action
		CleanUpMarks();

		if (!fRedoable)
			CleanUpRedoActions(true);

#ifdef DEBUG_ACTION_HANDLER
		StrAnsi sta;
		sta.Format("Undo:");
		sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
			m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
		::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER
	}
	catch(...)
	{
		CleanUpMarks();
		m_fUndoOrRedoInProgress = false;
		throw;
	}
	m_fUndoOrRedoInProgress = false;

	return hrFinal;

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Deletes any marks that point after the current action
----------------------------------------------------------------------------------------------*/
void ActionHandler::CleanUpMarks()
{
	for (int i = m_viMarks.Size() - 1; i >= 0; i--)
	{
		// Mark gets set to m_iuactCurr + 1, so we better check for that
		if (m_viMarks[i] <= m_iuactCurr + 1)
			break;
		m_viMarks.Delete(i);
	}
}

/*----------------------------------------------------------------------------------------------
	Calls the Undo method on the action handlers. If fForDataChange is true, this will be done
	on all action handlers that change the data, if fForDataChange is false this will be done
	on all action handlers that don't change the data.
----------------------------------------------------------------------------------------------*/
HRESULT ActionHandler::CallUndo(UndoResult * pures, bool & fRedoable, bool fForDataChange,
	bool fNeedsRefresh)
{
	HRESULT hr = S_OK;

	for (int i = m_iuactCurr; (m_iCurrSeq >= 0) && (i >= m_viSeqStart[m_iCurrSeq]); i--)
	{
		ComBool fDataChange;
		ComBool fThisIsRedoable;
		IUndoActionPtr quact = m_vquact[i];
		CheckHr(quact->IsDataChange(&fDataChange));
		CheckHr(quact->IsRedoable(&fThisIsRedoable));
		fRedoable &= fThisIsRedoable;
		if (fDataChange == fForDataChange)
		{
			ComBool fSuccess = false;
			IgnoreHr(hr = quact->Undo(fNeedsRefresh, &fSuccess));
			if (FAILED(hr))
			{
				// Undo() failed. We no longer want to continue the chain of actions as one failed
				// catastrophically.
				*pures = kuresError;
				return hr;
			}
			if (!fSuccess)
			{
				// we no longer want to continue the chain of actions as one failed (typically because
				// something else changed the data it was trying to fix).
				*pures = kuresFailed;
				break;
			}
		}
	}

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#Redo}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::Redo(UndoResult * pures)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pures);

	*pures = kuresSuccess;
	// Do not allow actions to be redone if they have not been undone.
	Assert(m_viSeqStart.Size() > 0);
	Assert(m_iCurrSeq <= m_viSeqStart.Size() - 1);

	m_fCanContinueTask = false; // Can't 'continue' the previous task from before the Undo.
	m_fUndoOrRedoInProgress = true;
	HRESULT hrFinal = S_OK;
	int iSeqToRedo = m_iCurrSeq + 1;

	try // to ensure in progress gets cleared.
	{
		int handle;
		// Start a 'transaction' if possible to group together all the stuff we're going to Undo.
		if (m_qundg)
		{
			m_qundg->BeginGroup(&handle);
		}

		// Determine the last action to be redone; The last action to redo is the action
		// before the next redo sequence. Or the last existing action if there is not a
		// redo sequence after the sequence we are about to redo.
		int iLastRedoAct = m_vquact.Size() - 1;
		if (iSeqToRedo <= m_viSeqStart.Size() - 2)
			iLastRedoAct = m_viSeqStart[iSeqToRedo + 1] - 1;

		// Loop through all of the actions that are about to be redone and see if any of them
		// require a refresh.
		bool fNeedsRefresh = false;
		for (int i = m_viSeqStart[iSeqToRedo]; i <= iLastRedoAct; i++)
		{
			CheckHr(m_vquact[i]->put_SuppressNotification(true));

			ComBool fThisNeedsRefresh;
			CheckHr(m_vquact[i]->RequiresRefresh(&fThisNeedsRefresh));
			fNeedsRefresh |= (bool)fThisNeedsRefresh;
		}

		if (fNeedsRefresh)
			*pures = kuresRefresh;

		// Redo all the actions in the next action sequence that change data.
		hrFinal = CallRedo(pures, true, fNeedsRefresh, iSeqToRedo, iLastRedoAct);

		// Now do all the PropChangeds
		for (int i = m_viSeqStart[iSeqToRedo]; i <= iLastRedoAct; i++)
		{
			CheckHr(m_vquact[i]->put_SuppressNotification(false));
		}

		// REVIEW (TimS/EberhardB): What do we do if the call to CallRedo above failed somewhere?
		// Should we do this at all? Or just until the failed action? Or everything?
		UndoResult uresTmp = kuresSuccess;
		// Redo all the actions in the next action sequence that don't change data.
		HRESULT hr;
		IgnoreHr(hr = CallRedo(&uresTmp, false, fNeedsRefresh, iSeqToRedo, iLastRedoAct));
		if (FAILED(hr))
		{
			hrFinal = hr;
			*pures = uresTmp;
		}
		else if (uresTmp == kuresFailed)
		{
			*pures = uresTmp;
		}

		// dlh testing
		if (*pures == kuresError || * pures == kuresFailed)
		{
			if (m_qundg)
				m_qundg->CancelGroup(handle);
			EmptyStack();
		}
		else // normal success case
		{
			// Set the current sequence and action.
			m_iCurrSeq++;
			m_iuactCurr = m_vquact.Size() - 1;
			if (m_iCurrSeq < m_viSeqStart.Size() - 1)
			{
				m_iuactCurr = m_viSeqStart[m_iCurrSeq + 1] - 1;
			}
			if (m_qundg)
				m_qundg->EndGroup(handle);
		}


#ifdef DEBUG_ACTION_HANDLER
		StrAnsi sta;
		sta.Format("Redo:");
		sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
			m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
		::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER
	}
	catch(...)
	{
		m_fUndoOrRedoInProgress = false;
		throw;
	}
	m_fUndoOrRedoInProgress = false;

	return hrFinal;

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Calls the Redo method on the action handlers. If fForDataChange is true, this will be done
	on all action handlers that change the data, if fForDataChange is false this will be done
	on all action handlers that don't change the data.
----------------------------------------------------------------------------------------------*/
HRESULT ActionHandler::CallRedo(UndoResult * pures, bool fForDataChange, bool fNeedsRefresh,
	int iSeqToRedo, int iLastRedoAct)
{
	HRESULT hr = S_OK;

	for (int i = m_viSeqStart[iSeqToRedo]; i <= iLastRedoAct; i++)
	{
		ComBool f;
		CheckHr(m_vquact[i]->IsDataChange(&f));
		if (f == fForDataChange)
		{
			ComBool fSuccess = false;
			IgnoreHr(hr = m_vquact[i]->Redo(fNeedsRefresh, &fSuccess));
			if (FAILED(hr))
			{
				// we no longer want to continue the chain of actions as one failed catastrophically
				*pures = kuresError;
				return hr;
			}
			if (!fSuccess)
			{
				// we no longer want to continue the chain of actions as one failed (typically because
				// something else changed the data it was trying to fix).
				*pures = kuresFailed;
				break;
			}
		}
	}
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#Rollback}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::Rollback(int nDepth)
{
	BEGIN_COM_METHOD;
	if (m_nDepth > 0)
	{
		CheckHr(EndUndoTask());
		m_nDepth = nDepth;
		// make sure we have something to undo
		if (m_iCurrSeq >= 0 && m_fDataChangeAction)
		{
			UndoResult ures;
			CheckHr(Undo(&ures));
			// REVIEW (TimS): What should we do if the undo fails?
			if (ures == kuresError || ures == kuresFailed)
			{
				return E_FAIL;
			}
			CleanUpRedoActions(true);
		}
	}
	return S_OK;
	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#get_CurrentDepth}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::get_CurrentDepth(int * pnDepth)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnDepth);

	*pnDepth = m_nDepth;

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#Commit}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::Commit()
{
	BEGIN_COM_METHOD;

	HRESULT hr = E_FAIL;
	HRESULT hrFinal = S_OK;
	if (m_vquact.Size())
	{
		// JohnT 10/22/01: this can happen, typically if the only thing in the sequence is a
		// position mark. We could try to check for that but it doesn't seem worth it. Nothing
		// actually depends on having at least one sequence start here.
		//Assert(m_viSeqStart.Size() > 0);

		// Commit all actions.
		for (int i = 0; i <= m_iuactCurr; i++)
		{
			// If for some reason, we cannot commit an action that is part of a sequence, it's
			// just too bad.  We can't undo a commited action so we might as well just amble on
			// and hope for the best.
			IgnoreHr(hr = m_vquact[i]->Commit());
			if (FAILED(hr))
			{
				hrFinal = hr;
			}
		}

		EmptyStack();
	}

#ifdef DEBUG_ACTION_HANDLER
	StrAnsi sta;
	sta.Format("Commit:");
	sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
		m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
	::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER

	return hrFinal;

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

// Empty the stack, either on Commit or some error, and put it back in the state
// where there is nothing that can be undone or redone, and we are ready for a
// BeginUndoTask.
void ActionHandler::EmptyStack()
{
	// Delete the committed actions
	for (int i = m_vquact.Size() -1; i >= 0; i--)
	{
		m_vquact.Delete(i);
	}

	// Delete the items in the action sequence vector and undo/redo text vectors that refer
	// to committed actions.
	for (int i = m_viSeqStart.Size() - 1; i >= 0; i--)
	{
		m_viSeqStart.Delete(i);
		m_vstuUndo.Delete(i);
		m_vstuRedo.Delete(i);
	}

	// Set the current action sequence and action values.
	m_iCurrSeq = -1;
	m_iuactCurr = -1;
	m_fCanContinueTask = false; // nothing on stack to continue.
}

/*----------------------------------------------------------------------------------------------
	${IActionHandler#Close}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::Close()
{
	BEGIN_COM_METHOD;

	for (int i = m_vquact.Size() - 1; i >= 0; i--)
	{
		m_vquact.Delete(i);
	}

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	When adding new actions to the list, drop any "undone" actions--they can no longer
	be redone.
	@param fForce True if redo actions should be cleared even if our depth is
				  zero or if a task is not started (e.g., as for Rollback).
----------------------------------------------------------------------------------------------*/
void ActionHandler::CleanUpRedoActions(bool fForce)
{
	//::OutputDebugStringA("Entering CleanUpRedoActions\n");
	//StrAnsi sta;
	//sta.Format("depth = %d; m_fStartedNext = %d; action %d of %d; seq %d of %d; marks %d%n",
	//	m_nDepth, m_fStartedNext, m_iuactCurr, m_vquact.Size(), m_iCurrSeq, m_viSeqStart.Size(), m_viMarks.Size());
	//::OutputDebugStringA(sta.Chars());

	if (m_fStartedNext || fForce)
	{
		if ((m_viSeqStart.Size()) && (m_iCurrSeq < m_viSeqStart.Size() - 1))
		{
			for (int i = m_vquact.Size() - 1; i >= m_viSeqStart[m_iCurrSeq + 1]; i--)
			{
				m_vquact.Delete(i);
			}

			// Drop any action sequences and their corresponding undo/redo texts
			// that have been undone.
			for (int i = m_viSeqStart.Size() - 1; i > m_iCurrSeq; i--)
			{
				m_viSeqStart.Delete(i);
				m_vstuUndo.Delete(i);
				m_vstuRedo.Delete(i);
			}

			// Drop any marks that are within the range we've deleted. Shouldn't really
			// need this, because the editing mechanism should take care of it,
			// but just in case.
			for (int i = m_viMarks.Size() - 1; i >= 0; i--)
			{
				if (m_viMarks[i] <= m_vquact.Size())
					break;
				m_viMarks.Delete(i);
				Assert(false);
			}
		}

		Assert(m_nDepth > 0 || fForce);
//		m_vstuUndo.Push(m_stuNextUndo);
//		m_vstuRedo.Push(m_stuNextRedo);
//
//		m_viSeqStart.Push(m_vquact.Size()); // where the next action will go
		m_iCurrSeq = m_viSeqStart.Size() - 1;

//		m_fStartedNext = false;

		//::OutputDebugStringA("After CleanUpRedoActions\n");
		//StrAnsi sta;
		//sta.Format("depth = %d; m_fStartedNext = %d; action %d of %d; seq %d of %d; marks %d%n",
		//	m_nDepth, m_fStartedNext, m_iuactCurr, m_vquact.Size(), m_iCurrSeq, m_viSeqStart.Size(), m_viMarks.Size());
		//::OutputDebugStringA(sta.Chars());
	}
	else
	{
		//::OutputDebugStringA("redo stuff already cleared\n");

		// Redo stuff should have already been cleared.
		Assert(m_viSeqStart.Size() == 0 || (m_iCurrSeq >= m_viSeqStart.Size() - 1));
	}
}

/*----------------------------------------------------------------------------------------------
	Remove the task from the top of the list if it has no actions inside of it, or if the
	actions have no real effect on the data.
----------------------------------------------------------------------------------------------*/
void ActionHandler::CleanUpEmptyTasks()
{
	if (m_viSeqStart.Size() == 0)
	{
		m_fCanContinueTask = false;
		return;
	}

	//::OutputDebugStringA("Entering CleanUpEmptyTasks\n");
	//StrAnsi sta;
	//sta.Format("depth = %d; m_fStartedNext = %d; action %d of %d; seq %d of %d; marks %d%n",
	//	m_nDepth, m_fStartedNext, m_iuactCurr, m_vquact.Size(), m_iCurrSeq, m_viSeqStart.Size(), m_viMarks.Size());
	//::OutputDebugStringA(sta.Chars());

	// Look for any actions that are just affecting the user interface, such as replacing
	// the selection. If that's all we have, or we don't have any actions at all in the,
	// top task, we have a bogus undo task that needs to be deleted.
	for (int iuact = *(m_viSeqStart.Top()); iuact < m_vquact.Size(); iuact++)
	{
		ComBool f;
		CheckHr(m_vquact[iuact]->IsDataChange(&f));
		if (f)
			return;	// found a real legitimate change
	}

	while (m_vquact.Size() - 1 >= *(m_viSeqStart.Top()))
	{
		Assert(m_iuactCurr == m_vquact.Size() - 1);
		// Remove no-effect action.
		m_vquact.Pop();
		m_iuactCurr--;
	}

	// Current task does not have any actions in it.
	Assert(m_vstuUndo.Size() == m_viSeqStart.Size());
	Assert(m_vstuRedo.Size() == m_viSeqStart.Size());
	m_vstuUndo.Pop();
	m_vstuRedo.Pop();
	m_viSeqStart.Pop();
	if (m_iCurrSeq >= m_viSeqStart.Size())
		m_iCurrSeq = m_viSeqStart.Size() - 1;
	// See if we had a mark pointing after what we just deleted. If so adjust it to reflect the
	// new next action.
	if (m_viMarks.Size() && *(m_viMarks.Top()) > m_iuactCurr + 1)
	{
		*(m_viMarks.Top()) = m_iuactCurr + 1;
	}
	// After deleting a null task, we can go ahead and add to the previous one...
	// but only if there is one!
	if (m_viSeqStart.Size() == 0)
		m_fCanContinueTask = false;

	//::OutputDebugStringA("After CleanUpEmptyTasks\n");
	//sta.Format("depth = %d; m_fStartedNext = %d; action %d of %d; seq %d of %d; marks %d%n",
	//	m_nDepth, m_fStartedNext, m_iuactCurr, m_vquact.Size(), m_iCurrSeq, m_viSeqStart.Size(), m_viMarks.Size());
	//::OutputDebugStringA(sta.Chars());
}

/*----------------------------------------------------------------------------------------------
	Sets a flag which makes the ActionHandler create a mark if there is no mark and
	another action is added to the stack.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::CreateMarkIfNeeded(ComBool fCreateMark)
{
	BEGIN_COM_METHOD;

	// It's only valid to place a mark while no action sequence is in progress, so making the
	// same check here as will be made in the Mark method that is called when an undo task
	// is started.
	if (fCreateMark && m_viMarks.Size() == 0 && m_nDepth > 0 && !m_fStartedNext)
		ThrowHr(WarnHr(E_UNEXPECTED));

	m_fCreateMarkIfNeeded = fCreateMark;

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Inserts a mark and returns a handle that can be used later to discard all Undo items
	back to the mark. Handle will never be zero.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::Mark(int * phMark)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phMark);

	// It's only valid to place a mark while no action sequence is in progress.
	if (m_nDepth > 0 && !m_fStartedNext)
		ThrowHr(WarnHr(E_UNEXPECTED));

	m_fCanContinueTask = false; // Can't merge another action into the current task.

	// For now, we are only supporting one mark at a time.
	Assert(m_viMarks.Size() == 0);

	m_viMarks.Push(m_iuactCurr + 1); // index of next action to be added
	*phMark = m_viMarks.Size();

	//StrAnsi sta;
	//sta.Format("mark location: %d\n", m_iuactCurr + 1);
	//::OutputDebugStringA(sta.Chars());

	//::OutputDebugStringA("MARK\n");
	//StrAnsi sta;
	//sta.Format("depth = %d; m_fStartedNext = %d; action %d of %d; seq %d of %d; marks %d%n",
	//	m_nDepth, m_fStartedNext, m_iuactCurr, m_vquact.Size(), m_iCurrSeq, m_viSeqStart.Size(), m_viMarks.Size());
	//::OutputDebugStringA(sta.Chars());

#ifdef DEBUG_ACTION_HANDLER
	StrAnsi sta;
	sta.Format("Mark:%d", *phMark);
	sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
		m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
	::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Collapses all Undo items back to a specified mark and creates a single Undo task for
	all of them. Also discards the mark. If there are no sequences following the mark, then
	simply discard the mark.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::CollapseToMark(int hMark, BSTR bstrUndo, BSTR bstrRedo)
{
	BEGIN_COM_METHOD;

	// It's only valid to collapse to a mark while no action sequence is in progress.
	if (m_nDepth > 0)
	{
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	m_fCanContinueTask = false; // Can't merge another action into the current task.

	// The handle hMark is 1 + the position of the mark in the mark stack.
	int iMarkGoal = (hMark == 0) ? m_viMarks.Size() - 1 : hMark - 1;

	if (iMarkGoal >= m_viMarks.Size() || iMarkGoal < 0)
		ThrowHr(E_INVALIDARG, L"hMark value too great. Don't have that many marks available.");

	CleanUpRedoActions(true);

	bool fSeqFoundAfterMark = false;

	// Find the task the mark points to and delete tasks following the mark.
	for (int iuSeq = m_viSeqStart.Size() - 1;
		iuSeq >= 0 && m_viSeqStart[iuSeq] >= m_viMarks[iMarkGoal]; iuSeq--)
	{
		m_viSeqStart.Delete(iuSeq);
		m_vstuUndo.Delete(iuSeq);
		m_vstuRedo.Delete(iuSeq);
		fSeqFoundAfterMark = true;
	}

	if (fSeqFoundAfterMark)
	{
		// Now create a new task that includes all the actions following the mark.
		m_viSeqStart.Push(m_viMarks[iMarkGoal]);
		m_vstuUndo.Push(bstrUndo);
		m_vstuRedo.Push(bstrRedo);

		// Make sure the current task pointer points to a valid task.
		if (m_iCurrSeq >= m_viSeqStart.Size())
			m_iCurrSeq = m_viSeqStart.Size() - 1;
		m_fCanContinueTask = true; // At this point we DO have an existing task we could append to.

	}

	//// Delete all actions that are already undone - after collapsing they can't be
	//// redone any more.
	//for (int iuAct = m_vquact.Size() - 1; iuAct >= 0 && m_iuactCurr < iuAct; iuAct--)
	//	m_vquact.Delete(iuAct);

	// Delete the mark (and any that follow) we're collapsing to.
	for (int iMarkTmp = m_viMarks.Size() - 1; iMarkTmp >= iMarkGoal; iMarkTmp--)
		m_viMarks.Delete(iMarkTmp);

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Discard all Undo items back to the specified mark (or the most recent mark, if any,
	if handle is zero).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::DiscardToMark(int hMark)
{
	BEGIN_COM_METHOD;
	// It's only valid to discard while no action sequence is in progress.
	if (m_nDepth > 0)
	{
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	m_fCanContinueTask = false; // Can't merge another action into the current task.

	// The handle hMark is 1 + the position of the mark in the mark stack.
	int iMarkGoal = (hMark == 0) ? m_viMarks.Size() - 1 : hMark - 1;

	if (iMarkGoal >= m_viMarks.Size() || iMarkGoal < 0)
		return E_INVALIDARG;

	//::OutputDebugStringA("Entering DiscardToMark\n");
	//StrAnsi sta;
	//sta.Format("depth = %d; m_fStartedNext = %d; action %d of %d; seq %d of %d; marks %d%n",
	//	m_nDepth, m_fStartedNext, m_iuactCurr, m_vquact.Size(), m_iCurrSeq, m_viSeqStart.Size(), m_viMarks.Size());
	//::OutputDebugStringA(sta.Chars());

	int iuactMark = m_viMarks[iMarkGoal];
	int iuact;
	for (iuact = m_vquact.Size() - 1; iuact >= iuactMark; iuact--)
	{
		m_vquact.Delete(iuact);
	}
	m_iuactCurr = min(m_iuactCurr, m_vquact.Size() - 1);

	for (iuact = m_viSeqStart.Size() - 1; iuact >= 0; iuact--)
	{
		if (m_viSeqStart[iuact] < m_vquact.Size())
			break;
		m_viSeqStart.Delete(iuact);
		m_vstuUndo.Delete(iuact);
		m_vstuRedo.Delete(iuact);
	}
	m_iCurrSeq = min(m_iCurrSeq, m_viSeqStart.Size() - 1);

	// Delete the mark(s).
	for (int iMarkTmp = m_viMarks.Size() - 1; iMarkTmp >= iMarkGoal; iMarkTmp--)
	{
		m_viMarks.Delete(iMarkTmp);
	}

	//::OutputDebugStringA("After DiscardToMark\n");
	//sta.Format("depth = %d; m_fStartedNext = %d; action %d of %d; seq %d of %d; marks %d%n",
	//	m_nDepth, m_fStartedNext, m_iuactCurr, m_vquact.Size(), m_iCurrSeq, m_viSeqStart.Size(), m_viMarks.Size());
	//::OutputDebugStringA(sta.Chars());

	// For now, we are only supporting one mark at a time:
	Assert(m_viMarks.Size() == 0);

	// If the depth is greater than zero, we're in the middle of recording a task, so
	// get back in the state where we can close it out.
	if (m_nDepth > 0)
	{
		Assert(m_viSeqStart.Size() == m_vstuUndo.Size());
		Assert(m_viSeqStart.Size() == m_vstuRedo.Size());

		m_fStartedNext = true;

		//::OutputDebugStringA("Leaving DiscardToMark\n");
		//sta.Format("depth = %d; m_fStartedNext = %d; action %d of %d; seq %d of %d; marks %d%n",
		//	m_nDepth, m_fStartedNext, m_iuactCurr, m_vquact.Size(), m_iCurrSeq, m_viSeqStart.Size(), m_viMarks.Size());
		//::OutputDebugStringA(sta.Chars());
	}

#ifdef DEBUG_ACTION_HANDLER
	StrAnsi sta;
	sta.Format("DiscardToMark:%d", hMark);
	sta.FormatAppend(" m_iCurrSeq=%d, m_viSeqStart=%d, m_iuactCurr=%d, m_vquact=%d, m_viMarks=%d, m_nDepth=%d\n",
		m_iCurrSeq, m_viSeqStart.Size(), m_iuactCurr, m_vquact.Size(), m_viMarks.Size(), m_nDepth);
	::OutputDebugStringA(sta.Chars());
#endif DEBUG_ACTION_HANDLER

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}


/*----------------------------------------------------------------------------------------------
	Get the handle to the top mark. If there are no marks on the undo stack, return 0.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::get_TopMarkHandle(int * phMark)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phMark);

	*phMark = m_viMarks.Size();

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Return true if there is anything undoable after the top mark (and if there is at
	least one mark).
	If fUndo is true, we need an action to undo, if false, it doesn't matter (since any
	action to redo will certainly be within the scope of the mark).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::get_TasksSinceMark(ComBool fUndo, ComBool * pf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pf);
	*pf = false; // default answer.

	if (m_viMarks.Size() == 0)
		return S_OK; // no mark, return false

	int iuactTopMark = m_viMarks[m_viMarks.Size() - 1];

	if (fUndo)
	{
		// The next action to undo is before the top mark.
		if (iuactTopMark > m_iuactCurr)
			return S_OK; // return false;
	}
	else
	{
		// The next action to redo is before the top mark.
		if (iuactTopMark > m_iuactCurr + 1)
			return S_OK; // return false;
	}

	int iuactStart = (fUndo) ? iuactTopMark : m_iuactCurr + 1;
	int iuactEnd = (fUndo) ? m_iuactCurr + 1 : m_vquact.Size();
	for (int iuact = iuactStart; iuact < iuactEnd; iuact++)
	{
		ComBool fIsDataChange;
		CheckHr(m_vquact[iuact]->IsDataChange(&fIsDataChange));
		if (fIsDataChange)
		{
			*pf = true;
			return S_OK;
		}
	}
	// No real changes within the marks, return false.

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Return the number of outstanding Undoable actions.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::get_UndoableActionCount(int * pcAct)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcAct);
	*pcAct = m_iuactCurr + 1;
	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Return the number of outstanding Undoable sequences. This is the number of
	times the user could issue the Undo command.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::get_UndoableSequenceCount(int * pcAct)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcAct);
	*pcAct = m_iCurrSeq + 1;
	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	Return the number of outstanding Redoable sequences. This is the number of
	times the user could issue the Redo command.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::get_RedoableSequenceCount(int * pcAct)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcAct);
	*pcAct = m_viSeqStart.Size() - m_iCurrSeq - 1;
	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
	This will return the current UndoGrouper for the AH - if one exists, otherwise returns null.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::get_UndoGrouper(IUndoGrouper ** ppundg)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppundg);

	*ppundg = m_qundg;
	AddRefObj(*ppundg);

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}

/*----------------------------------------------------------------------------------------------
// This will set the UndoGrouper for this AH.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ActionHandler::put_UndoGrouper(IUndoGrouper * pundg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pundg);

	m_qundg = pundg;

	END_COM_METHOD(g_factActh, IID_IActionHandler);
}



// Explicit instantiation
#include <Vector_i.cpp>
template Vector<IUndoActionPtr>;
template Vector<SmartBstr>;
