/*-------------------------------------------------------------------*//*:Ignore these comments.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestUndoStack.h
Responsibility: John Thomson
Last reviewed:

	Unit tests for the ActionHandler class (which I hope to one day rename UndoStack).
----------------------------------------------------------------------------------------------*/
#ifndef TESTUNDOSTACK_H_INCLUDED
#define TESTUNDOSTACK_H_INCLUDED

#pragma once

#include "testFwKernel.h"

namespace TestFwKernel
{
	// Allows access to protected variables
	class DummyActionHandler : public ActionHandler
	{
	public:
		// Gets the last location of the mark
		int DummyActionHandler::GetLastMarkLocation()
		{
			return m_viMarks[m_viMarks.Size() - 1];
		}
	};
	DEFINE_COM_PTR(DummyActionHandler);

	// To test ActionHandler (=UndoStack), we need some UndoActions to insert into it.
	// This is our stub class. The methods just keep track of whether it is done or undone.
	class StubUndoAction : public IUndoAction
	{
	public:
		bool m_fDone;
		bool m_fCommitted;
		bool m_fForceFailure;
		bool m_fForceError;
		bool m_fRefreshPending;
		ulong m_cref;

		StubUndoAction()
		{
			m_cref = 1;
			m_fDone = true;
			m_fCommitted = false;
			m_fForceFailure = false;
			m_fForceError = false;
			m_fRefreshPending = false;
		}

		STDMETHOD(QueryInterface)(REFIID iid, void ** ppv)
		{
			*ppv = NULL;

			if (iid == IID_IUnknown)
				*ppv = static_cast<IUnknown *>(static_cast<IUndoAction *>(this));
			else if (iid == IID_IUndoAction)
				*ppv = static_cast<IUndoAction *>(this);
			else
				return E_NOINTERFACE;

			reinterpret_cast<IUnknown *>(*ppv)->AddRef();
			return S_OK;
		}

		STDMETHOD_(ULONG, AddRef)(void)
		{
			Assert(m_cref > 0);
			return ++m_cref;
		}

		STDMETHOD_(ULONG, Release)(void)
		{
			Assert(m_cref > 0);
			if (--m_cref > 0)
				return m_cref;

			m_cref = 1;
			delete this;
			return 0;
		}

		// Reverses (or "un-does") an action.
		STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess)
		{
			AssertPtr(pfSuccess);
			Assert(!m_fCommitted);
			Assert(m_fDone);
			*pfSuccess = m_fDone && !m_fCommitted;
			m_fRefreshPending = fRefreshPending;
			if (!m_fForceFailure && !m_fForceError)
				m_fDone = false;
			if (m_fForceFailure)
				*pfSuccess = false;
			if (m_fForceError)
				return E_FAIL;
			return S_OK;
		}

		// Re-applies (or "re-does") an action.
		STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess)
		{
			AssertPtr(pfSuccess);
			Assert(!m_fCommitted);
			Assert(!m_fDone);
			*pfSuccess = !m_fDone && !m_fCommitted;
			m_fRefreshPending = fRefreshPending;
			if (!m_fForceFailure && !m_fForceError)
				m_fDone = true;
			if (m_fForceFailure)
				*pfSuccess = false;
			if (m_fForceError)
				return E_FAIL;
			return S_OK;
		}

		// Irreversibly commits an action.
		STDMETHOD(Commit)()
		{
			Assert(!m_fCommitted);
			m_fCommitted = true;
			return S_OK;
		}

		// True for most actions, which make changes to data; false for actions that represent
		// updates to the user interface, like replacing the selection.
		STDMETHOD(IsDataChange)(ComBool * pfRet)
		{
			*pfRet = true;
			return S_OK;
		}

		// Returns true because this can be redone.
		STDMETHOD(IsRedoable)(ComBool * pfRet)
		{
			*pfRet = true;
			return S_OK;
		}

		/*----------------------------------------------------------------------------------------------
			${IUndoAction#RequiresRefresh}
		----------------------------------------------------------------------------------------------*/
		STDMETHOD(RequiresRefresh)(ComBool * pfRet)
		{
			*pfRet = true;
			return S_OK;
		}

		/*----------------------------------------------------------------------------------------------
			${IUndoAction#SuppressNotification}
		----------------------------------------------------------------------------------------------*/
		STDMETHOD(put_SuppressNotification)(ComBool fSuppress)
		{
			return S_OK;
		}
	};
	DEFINE_COM_PTR(StubUndoAction);

	// To test ActionHandler (=UndoStack), we need some UndoActions to insert into it.
	// This is our stub class. The methods just keep track of whether it is done or undone.
	// This class returns false for RequiresRefresh.
	class StubUndoActionNoRefreshNeeded : public StubUndoAction
	{
		/*----------------------------------------------------------------------------------------------
			${IUndoAction#RequiresRefresh}
		----------------------------------------------------------------------------------------------*/
		STDMETHOD(RequiresRefresh)(ComBool * pfRet)
		{
			*pfRet = false;
			return S_OK;
		}
	};
	DEFINE_COM_PTR(StubUndoActionNoRefreshNeeded);

	// To test ActionHandler (=UndoStack), we need some UndoActions to insert into it.
	// This is our stub class. The methods just keep track of whether it is done or undone.
	// This class returns false for IsDataChange.
	class StubUndoActionNoDataChange : public StubUndoAction
	{
		// True for most actions, which make changes to data; false for actions that represent
		// updates to the user interface, like replacing the selection.
		STDMETHOD(IsDataChange)(ComBool * pfRet)
		{
			*pfRet = false;
			return S_OK;
		}
	};
	DEFINE_COM_PTR(StubUndoActionNoDataChange);

	// To test ActionHandler (=UndoStack), we need some UndoActions to insert into it.
	// This is our stub class. This class makes a call to the action handler to set a mark.
	class StubUndoActionMakeMark : public StubUndoAction
	{
	private:
		IActionHandler * m_pacth;

	public:
		// intializes this class
		void Initialize(IActionHandler * pacth)
		{
			m_pacth = pacth;
		}

		// Reverses (or "un-does") an action.
		STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess)
		{
			int hmark;
			CheckHr(m_pacth->Mark(&hmark));
			*pfSuccess = true;
			return S_OK;
		}

		// Re-applies (or "re-does") an action.
		STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess)
		{
			int hmark;
			CheckHr(m_pacth->Mark(&hmark));
			*pfSuccess = true;
			return S_OK;
		}
	};
	DEFINE_COM_PTR(StubUndoActionMakeMark);


	// Stub implementation of IUndoGrouper, just notes calls.

	class StubGrouper : public IUndoGrouper
	{
	public:
		int m_cBegin, m_cEnd, m_cCancel;
		ulong m_cref;

		StubGrouper()
		{
			m_cref = 1;
			Reset();
		}

		STDMETHOD(QueryInterface)(REFIID iid, void ** ppv)
		{
			*ppv = NULL;

			if (iid == IID_IUnknown)
				*ppv = static_cast<IUnknown *>(static_cast<IUndoGrouper *>(this));
			else if (iid == IID_IUndoGrouper)
				*ppv = static_cast<IUndoGrouper *>(this);
			else
				return E_NOINTERFACE;

			reinterpret_cast<IUnknown *>(*ppv)->AddRef();
			return S_OK;
		}

		STDMETHOD_(ULONG, AddRef)(void)
		{
			Assert(m_cref > 0);
			return ++m_cref;
		}

		STDMETHOD_(ULONG, Release)(void)
		{
			Assert(m_cref > 0);
			if (--m_cref > 0)
				return m_cref;

			m_cref = 1;
			delete this;
			return 0;
		}


		void Reset()
		{
			m_cBegin = m_cEnd = m_cCancel = 0;
		}
		// Called before the seq of undo actions is undone or redone. (Begin Transaction)
		STDMETHOD(BeginGroup)(int * phandle)
		{
			m_cBegin++;
			return S_OK;
		}

		// Called to end a seq of undo actions. (End Transaction)
		STDMETHOD(EndGroup)(int handle)
		{
			m_cEnd++;
			return S_OK;
		}

		// Call following and error condition to return things to the state where it was when the
		// BeginGroup was called. (Rollback)
		STDMETHOD(CancelGroup)(int handle)
		{
			m_cCancel++;
			return S_OK;
		}

	};
	DEFINE_COM_PTR(StubGrouper);

	class TestUndoStack : public unitpp::suite
	{
		DummyActionHandlerPtr m_qacth;
		StubUndoActionPtr m_qsua1, m_qsua2, m_qsua3;
		StubGrouperPtr m_qsg;

		/*--------------------------------------------------------------------------------------
			Test the method that tells us how many individual Undoable actions are on the undo
			stack.
			This also tests several other methods
		--------------------------------------------------------------------------------------*/
		void testActionCount()
		{
			unitpp::assert_true("Non-NULL m_qacth after setup", m_qacth.Ptr() != 0 );

			HRESULT hr;
			int cAct;
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Action count HRESULT", S_OK , hr);
			unitpp::assert_eq("Initial action count zero", 0 , cAct);

			StrUni stuUndo = L"Undo dummy action";
			StrUni stuRedo = L"Redo dummy action";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			StubUndoActionPtr qsua;
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Initial action count zero", 1 , cAct);

			StubUndoActionPtr qsua2;
			qsua2.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua2);
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Count two items in same seq", 2 , cAct);

			hr = m_qacth->EndUndoTask();

			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Still have two actions when group closed.", 2 , cAct);

			UndoResult ures;
			hr = m_qacth->Undo(&ures);
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Undo removed items from count", 0 , cAct);

			unitpp::assert_eq("Undo called Undo on items", false , qsua->m_fDone);
			unitpp::assert_eq("Undo called Undo on items", false , qsua2->m_fDone);

			hr = m_qacth->Redo(&ures);
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Redo put items back in count", 2 , cAct);

			unitpp::assert_eq("Redo called Redo on items", true , qsua->m_fDone);
			unitpp::assert_eq("Redo called Redo on items", true , qsua2->m_fDone);

			StrUni stuUndo2 = L"Undo dummy action2";
			StrUni stuRedo2 = L"Redo dummy action2";
			hr = m_qacth->BeginUndoTask(stuUndo2.Bstr(), stuRedo2.Bstr());

			StubUndoActionPtr qsua3;
			qsua3.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua3.Ptr());
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("3 actions in 2 groups", 3 , cAct);

			hr = m_qacth->EndUndoTask();

			hr = m_qacth->Undo(&ures);
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Undo removed 1 item from count", 2 , cAct);

			unitpp::assert_eq("Undo called Undo on items", false , qsua3->m_fDone);
			unitpp::assert_eq("Undo called Undo only on correct items", true , qsua2->m_fDone);

			hr = m_qacth->Redo(&ures);
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Redo put items back in count", 3 , cAct);

			unitpp::assert_eq("Redo called Redo on items", true , qsua3->m_fDone);
		}


		void MakeSimpleThreeActionTask()
		{
			StrUni stuUndo = L"Undo dummy action";
			StrUni stuRedo = L"Redo dummy action";
			HRESULT hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			m_qsua1.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(m_qsua1.Ptr());

			m_qsua2.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(m_qsua2.Ptr());

			m_qsua3.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(m_qsua3.Ptr());

			hr = m_qacth->EndUndoTask();
		}

		/*--------------------------------------------------------------------------------------
			This method will test an Undo / Redo result and reset the group flags.
		--------------------------------------------------------------------------------------*/
		void VerifyUndoGroupActions(int cBeginExpected, int cEndExpected, int cCancelExpected,
			UndoResult uresExpected, UndoResult ures,
			bool fdone1Expected, bool fdone2Expected, bool fdone3Expected)
		{
			unitpp::assert_eq("Begin Group calls", cBeginExpected, m_qsg->m_cBegin);
			unitpp::assert_eq("End Group calls", cEndExpected, m_qsg->m_cEnd);
			unitpp::assert_eq("Cancel Group calls", cCancelExpected, m_qsg->m_cCancel);
			unitpp::assert_eq("Undo Grouper Enum", uresExpected, ures);
			unitpp::assert_eq("1st action done state", fdone1Expected, m_qsua1->m_fDone);
			unitpp::assert_eq("2nd action done state", fdone2Expected, m_qsua2->m_fDone);
			unitpp::assert_eq("3rd action done state", fdone3Expected, m_qsua3->m_fDone);
			//m_qsg->Reset();

			if (ures == kuresError || ures == kuresFailed)
			{
				int cAct;
				HRESULT hr = m_qacth->get_UndoableActionCount(&cAct);
				unitpp::assert_eq("Failed undo/redo didn't clear the undo stack", 0 , cAct);
				hr = m_qacth->get_RedoableSequenceCount(&cAct);
				unitpp::assert_eq("Failed undo/redo didn't clear the undo stack", 0 , cAct);
			}
		}

		/*--------------------------------------------------------------------------------------
			Test the UndoGrouper and the AH to make sure it's calling the UndoGrouper.
		--------------------------------------------------------------------------------------*/
		void testUndoGrouping()
		{
			m_qsg.Attach(NewObj StubGrouper());

			HRESULT hr = m_qacth->put_UndoGrouper(m_qsg);
			IUndoGrouperPtr qundg;
			hr = m_qacth->get_UndoGrouper(&qundg);
			unitpp::assert_eq("ActionHandler failed to save IUndoGrouper", (IUndoGrouper*)m_qsg.Ptr(), qundg.Ptr());

			MakeSimpleThreeActionTask();

			UndoResult ures;
			hr = m_qacth->Undo(&ures);
			VerifyUndoGroupActions(1,1,0,kuresRefresh,ures,false,false,false);
			m_qsg->Reset();

			hr = m_qacth->Redo(&ures);
			VerifyUndoGroupActions(1,1,0,kuresRefresh,ures,true,true,true);

			// add another undo action to the AH so we can verify stack clearing
			m_qsg->Reset();
			MakeSimpleThreeActionTask();

			// Undo error
			m_qsua2->m_fForceError = true;
			hr = m_qacth->Undo(&ures);
			VerifyUndoGroupActions(1,0,1,kuresError,ures,true,true,false);

			// Undo failure
			m_qsg->Reset();
			MakeSimpleThreeActionTask();
			MakeSimpleThreeActionTask();
			m_qsua2->m_fForceFailure = true;
			hr = m_qacth->Undo(&ures);
			VerifyUndoGroupActions(1,0,1,kuresFailed,ures,true,true,false);

			// Redo error
			MakeSimpleThreeActionTask();
			MakeSimpleThreeActionTask();
			hr = m_qacth->Undo(&ures);
			m_qsua2->m_fForceError = true;
			m_qsg->Reset();
			hr = m_qacth->Redo(&ures);
			VerifyUndoGroupActions(1,0,1,kuresError,ures,true,false,false);

			// Redo failure
			MakeSimpleThreeActionTask();
			MakeSimpleThreeActionTask();
			hr = m_qacth->Undo(&ures);
			m_qsua2->m_fForceFailure = true;
			m_qsg->Reset();
			hr = m_qacth->Redo(&ures);
			VerifyUndoGroupActions(1,0,1,kuresFailed,ures,true,false,false);

		}

		/*--------------------------------------------------------------------------------------
			Test the COM methods for handling NULL pointer arguments properly.
		--------------------------------------------------------------------------------------*/
		void testNullArgs()
		{
			unitpp::assert_true("Non-NULL m_qacth after setup", m_qacth.Ptr() != 0 );

			HRESULT hr;
			try
			{
				CheckHr(hr = m_qacth->get_UndoableActionCount(NULL));
				unitpp::assert_eq("get_UndoableActionCount(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable &thr)
			{
				unitpp::assert_eq("get_UndoableActionCount(NULL) HRESULT", E_POINTER, thr.Result());
			}
			// Enhance JohnT: add tests for other methods...
		}

		/*--------------------------------------------------------------------------------------
			Test the CollapseToMark method.
		--------------------------------------------------------------------------------------*/
		void testCollapseToMark()
		{
			HRESULT hr;
			int hMark;
			int cAct;
			int cSeq;
			StrUni stuUndo;
			StrUni stuRedo;
			StubUndoActionPtr qsua;

			// Make sure there are no undo actions.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Initial action count zero", 0 , cAct);

			// Put a mark before task 1.
			hr = m_qacth->Mark(&hMark);
			unitpp::assert_eq("No mark after calling Mark()", 1, hMark);

			// Start undo task 1.
			stuUndo = L"Undo Task 1";
			stuRedo = L"Redo Task 1";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			// Add a couple of actions task 1
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();

			// Start undo task 2.
			stuUndo = L"Undo Task 2";
			stuRedo = L"Redo Task 2";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			// Add 3 actions to task 2
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();

			// Make sure there are 5 undo actions and 2 undo tasks.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Should have 5 actions.", 5 , cAct);
			hr = m_qacth->get_UndoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 2 tasks.", 2 , cSeq);

			// Collapse tasks 1 and 2 into a single undo task.
			stuUndo = L"Collapsed Undo Task";
			stuRedo = L"Collapsed Redo Task";
			hr = m_qacth->CollapseToMark(hMark, stuUndo.Bstr(), stuRedo.Bstr());

			// Now make sure there are 5 undo actions and 1 undo task.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Should have 5 actions.", 5 , cAct);
			hr = m_qacth->get_UndoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 1 tasks.", 1 , cSeq);

			SmartBstr sbstr;
			// Check that our task's undo text is correct.
			hr = m_qacth->GetUndoText(&sbstr);
			unitpp::assert_true("Undo Text wrong.",	sbstr == (LPCOLESTR)L"Collapsed Undo Task");

			// Do a single undo and make sure the collapsed task's redo text is correct.
			UndoResult ures;
			hr = m_qacth->Undo(&ures);
			hr = m_qacth->GetRedoText(&sbstr);
			unitpp::assert_true("Redo Text wrong.", sbstr == (LPCOLESTR)L"Collapsed Redo Task");
		}

		/*--------------------------------------------------------------------------------------
			Test the CollapseToMark method when there are no sequences created since the mark.
			Should not create an undo task at all.
		--------------------------------------------------------------------------------------*/
		void testCollapseToMark_NoSequences()
		{
			HRESULT hr;
			int hMark;
			int cAct;
			int cSeq;
			StrUni stuUndo;
			StrUni stuRedo;
			StubUndoActionPtr qsua;

			// Make sure there are no undo actions.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Initial action count zero", 0 , cAct);

			// Put a mark before task 1.
			hr = m_qacth->Mark(&hMark);
			unitpp::assert_eq("No mark after calling Mark()", 1, hMark);

			// Start undo task 1 and end it without adding any actions.
			stuUndo = L"Undo Task 1";
			stuRedo = L"Redo Task 1";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			m_qacth->EndUndoTask();

			// Collapse to mark (shouldn't create a task at all).
			stuUndo = L"This Undo Task should not get created";
			stuRedo = L"This Redo Task should not get created";
			hr = m_qacth->CollapseToMark(hMark, stuUndo.Bstr(), stuRedo.Bstr());

			// Now make sure there are 0 undo actions and 0 undo tasks.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Should have 0 actions.", 0 , cAct);
			hr = m_qacth->get_UndoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 0 undoable sequences.", 0, cSeq);
			hr = m_qacth->get_RedoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 0 redoable sequences", 0, cSeq);
		}

		/*--------------------------------------------------------------------------------------
			Test the CollapseToMark method when the only sequence created since the mark has
			already been undone. Should not create an undo task at all but should clear the
			redo stack.
		--------------------------------------------------------------------------------------*/
		void testCollapseToMark_SequenceAlreadyUndone()
		{
			HRESULT hr;
			int hMark;
			int cAct;
			int cSeq;
			StrUni stuUndo;
			StrUni stuRedo;
			StubUndoActionPtr qsua;

			// Make sure there are no undo actions.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Initial action count zero", 0 , cAct);

			// Put a mark before task 1.
			hr = m_qacth->Mark(&hMark);
			unitpp::assert_eq("No mark after calling Mark()", 1, hMark);

			// Start undo task 1.
			stuUndo = L"Undo Task 1";
			stuRedo = L"Redo Task 1";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			// Add 2 actions to task 1
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();

			// Now, undo task 1.
			UndoResult undoRes;
			hr = m_qacth->Undo(&undoRes);
			unitpp::assert_eq("Undo should succeed", kuresRefresh, undoRes);

			// Collapse to mark (shouldn't create a task at all).
			stuUndo = L"This Undo Task should not get created";
			stuRedo = L"This Redo Task should not get created";
			hr = m_qacth->CollapseToMark(hMark, stuUndo.Bstr(), stuRedo.Bstr());

			// Now make sure there are 0 undo actions and 0 undo tasks.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Should have 0 actions.", 0 , cAct);
			hr = m_qacth->get_UndoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 0 undoable sequences.", 0, cSeq);
			hr = m_qacth->get_RedoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 0 redoable sequences", 0, cSeq);
		}

		/*--------------------------------------------------------------------------------------
			Test the CollapseToMark method when the tasks are already partially undone.
		--------------------------------------------------------------------------------------*/
		void testCollapseToMark_PartialUndone()
		{
			HRESULT hr;
			int hMark;
			int cAct;
			int cSeq;
			StrUni stuUndo;
			StrUni stuRedo;
			StubUndoActionPtr qsua;

			// Put a mark before task 1.
			hr = m_qacth->Mark(&hMark);
			unitpp::assert_eq("No mark after calling Mark()", 1, hMark);

			// Start undo task 1.
			stuUndo = L"Undo Task 1";
			stuRedo = L"Redo Task 1";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			// Add a couple of actions task 1
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();

			// Start undo task 2.
			stuUndo = L"Undo Task 2";
			stuRedo = L"Redo Task 2";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			// Add 3 actions to task 2
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();

			// Make sure there are 5 undo actions and 2 undo tasks.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Should have 5 actions.", 5 , cAct);
			hr = m_qacth->get_UndoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 2 tasks.", 2 , cSeq);

			UndoResult undoRes;
			hr = m_qacth->Undo(&undoRes);
			unitpp::assert_eq("Undo should succeed", kuresRefresh, undoRes);

			// Collapse tasks 1 and 2 into a single undo task.
			stuUndo = L"Collapsed Undo Task";
			stuRedo = L"Collapsed Redo Task";
			hr = m_qacth->CollapseToMark(hMark, stuUndo.Bstr(), stuRedo.Bstr());

			// Now make sure there are 2 undo actions and 1 undo task (since task 2
			// was undone, we don't want that in the new task).
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Should have 2 actions.", 2 , cAct);
			hr = m_qacth->get_UndoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 1 tasks.", 1 , cSeq);

			// Now make sure there are no redoable actions and no redo tasks
			ComBool fCanRedo;
			hr = m_qacth->CanRedo(&fCanRedo);
			unitpp::assert_true("Shouldn't be able to redo", !fCanRedo);
			hr = m_qacth->get_RedoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 0 tasks.", 0 , cSeq);

			SmartBstr sbstr;
			// Check that our task's undo text is correct.
			hr = m_qacth->GetUndoText(&sbstr);
			unitpp::assert_true("Undo Text wrong.",	sbstr == (LPCOLESTR)L"Collapsed Undo Task");

			// Do a single undo and make sure the collapsed task's redo text is correct.
			UndoResult ures;
			hr = m_qacth->Undo(&ures);
			hr = m_qacth->GetRedoText(&sbstr);
			unitpp::assert_true("Redo Text wrong.", sbstr == (LPCOLESTR)L"Collapsed Redo Task");
		}

		/*--------------------------------------------------------------------------------------
			Test the DiscardToMark (and some other methods)
		--------------------------------------------------------------------------------------*/
		void testDiscardToMark()
		{
			HRESULT hr;
			int hMark;

			hr = m_qacth->get_TopMarkHandle(&hMark);
			unitpp::assert_eq("got marks after beginning of undo task", 0, hMark);

			// 1. Test: DiscardToMark and pass handle
			hr = m_qacth->Mark(&hMark);
			unitpp::assert_eq("No mark after calling Mark()", 1, hMark);

			int cAct;
			StubUndoActionPtr qsua;
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Initial action count zero", 1 , cAct);

			hr = m_qacth->DiscardToMark(hMark);
			hr = m_qacth->get_TopMarkHandle(&hMark);
			unitpp::assert_eq("got marks after DiscardToMark(hMark)", 0, hMark);
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Actions left after DiscardToMark(hMark)", 0 , cAct);

			// 2. Test: DiscardToMark but pass NULL
			hr = m_qacth->Mark(&hMark);
			unitpp::assert_eq("No mark after calling Mark()", 1, hMark);

			hr = m_qacth->DiscardToMark(0);
			hr = m_qacth->get_TopMarkHandle(&hMark);
			unitpp::assert_eq("got marks after DiscardToMark(0)", 0, hMark);

			// 3. Test: call DiscardToMark without having a mark
			hr = m_qacth->DiscardToMark(0);
			hr = m_qacth->get_TopMarkHandle(&hMark);
			unitpp::assert_eq("got marks after DiscardToMark(0)", 0, hMark);
		}

		/*--------------------------------------------------------------------------------------
			Test the Rollback
		--------------------------------------------------------------------------------------*/
		void testRollback()
		{
			int cAct;
			StubUndoActionPtr qsua;
			int nDepth;

			StrUni stuUndo = L"Undo dummy action";
			StrUni stuRedo = L"Redo dummy action";

			// 1. Test: make sure Rollback can handle not having any actions to undo
			m_qacth->get_CurrentDepth(&nDepth);
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 1: Initial action should be 0", 0, cAct);

			m_qacth->Rollback(nDepth);
			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 1: Action count should still be zero", 0, cAct);

			// 2. Test: Create one task and Rollback.
			m_qacth->get_CurrentDepth(&nDepth);
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			qsua.Attach(NewObj StubUndoAction());
			m_qacth->AddAction(qsua.Ptr());
			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 2: Initial action should be 1", 1, cAct);

			m_qacth->Rollback(nDepth);
			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 2: Action count should be zero", 0, cAct);

			// 3. Test: Create two tasks and Rollback to first task.
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			qsua.Attach(NewObj StubUndoAction());
			m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();
			m_qacth->get_CurrentDepth(&nDepth);
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			qsua.Attach(NewObj StubUndoAction());
			m_qacth->AddAction(qsua.Ptr());

			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 3: Initial action count should be 2", 2, cAct);

			m_qacth->Rollback(nDepth);
			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 3: Action count should be 1", 1, cAct);

			// 4. Test: make sure rollback does nothing if we aren't in the middle of a task
			m_qacth->Rollback(nDepth);
			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 4: Action count should still be 1", 1, cAct);

			// 5. Test: make sure Rollback doesn't undo the previous task if there was
			//          nothing to undo for the current task
			m_qacth->get_CurrentDepth(&nDepth);
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 5: Initial action should be 1", 1, cAct);

			m_qacth->Rollback(nDepth);
			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 5: Action count should still be 1", 1, cAct);

			// 6. Test: make sure Rollback doesn't undo the previous task if there were
			//          no data changes made
			m_qacth->get_CurrentDepth(&nDepth);
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			qsua.Attach(NewObj StubUndoActionNoDataChange());
			m_qacth->AddAction(qsua.Ptr());

			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 6: Initial action should be 2", 2, cAct);

			m_qacth->Rollback(nDepth);
			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Test 6: Action count should still be 1", 1, cAct);
		}

		/*--------------------------------------------------------------------------------------
			Test the Rollback with nested tasks.
		--------------------------------------------------------------------------------------*/
		void testNestedRollback()
		{
			int cAct;
			int nDepth;
			StubUndoActionPtr qsua;
			StrUni stuUndo = L"Undo dummy action";
			StrUni stuRedo = L"Redo dummy action";

			// Test: Create two tasks nested under another task and Rollback.
			//			It should roll back all the tasks.
			m_qacth->get_CurrentDepth(&nDepth);
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			qsua.Attach(NewObj StubUndoAction());
			m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			qsua.Attach(NewObj StubUndoAction());
			m_qacth->AddAction(qsua.Ptr());

			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Initial action count should be 2", 2, cAct);

			m_qacth->Rollback(nDepth);
			m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Action count should be zero", 0, cAct);
		}

		/*--------------------------------------------------------------------------------------
			Test that Undoactions get the correct refreshPending flag passed in. Also the undo
			result should be kuresRefresh.
		--------------------------------------------------------------------------------------*/
		void testRefreshPending_withRefresh()
		{
			StubUndoActionPtr qsua1;
			StubUndoActionPtr qsua2;
			StubUndoActionPtr qsua3;
			StrUni stuUndo = L"Undo dummy action";
			StrUni stuRedo = L"Redo dummy action";

			// Test: Create three actions. Two don't need a refresh and the third does.
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			qsua1.Attach(NewObj StubUndoActionNoRefreshNeeded());
			m_qacth->AddAction(qsua1.Ptr());
			qsua2.Attach(NewObj StubUndoActionNoRefreshNeeded());
			m_qacth->AddAction(qsua2.Ptr());
			qsua3.Attach(NewObj StubUndoAction());
			m_qacth->AddAction(qsua3.Ptr());
			m_qacth->EndUndoTask();

			UndoResult ures;
			m_qacth->Undo(&ures);

			unitpp::assert_eq("Action 1 should have refresh pending", true, qsua1->m_fRefreshPending);
			unitpp::assert_eq("Action 2 should have refresh pending", true, qsua2->m_fRefreshPending);
			unitpp::assert_eq("Action 3 should have refresh pending", true, qsua3->m_fRefreshPending);
			unitpp::assert_eq("Undo result should be kuresRefresh", kuresRefresh, ures);

			m_qacth->Redo(&ures);

			unitpp::assert_eq("Action 1 should have refresh pending", true, qsua1->m_fRefreshPending);
			unitpp::assert_eq("Action 2 should have refresh pending", true, qsua2->m_fRefreshPending);
			unitpp::assert_eq("Action 3 should have refresh pending", true, qsua3->m_fRefreshPending);
			unitpp::assert_eq("Redo result should be kuresRefresh", kuresRefresh, ures);
		}

		/*--------------------------------------------------------------------------------------
			Test that Undoactions get the correct refreshPending flag passed in. Also the undo
			result should be kuresSuccess.
		--------------------------------------------------------------------------------------*/
		void testRefreshPending_withoutRefresh()
		{
			StubUndoActionPtr qsua1;
			StubUndoActionPtr qsua2;
			StubUndoActionPtr qsua3;
			StrUni stuUndo = L"Undo dummy action";
			StrUni stuRedo = L"Redo dummy action";

			// Test: Create three actions. All don't need a refresh.
			m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			qsua1.Attach(NewObj StubUndoActionNoRefreshNeeded());
			m_qacth->AddAction(qsua1.Ptr());
			qsua2.Attach(NewObj StubUndoActionNoRefreshNeeded());
			m_qacth->AddAction(qsua2.Ptr());
			qsua3.Attach(NewObj StubUndoActionNoRefreshNeeded());
			m_qacth->AddAction(qsua3.Ptr());
			m_qacth->EndUndoTask();

			UndoResult ures;
			m_qacth->Undo(&ures);

			unitpp::assert_eq("Action 1 should not have refresh pending", false, qsua1->m_fRefreshPending);
			unitpp::assert_eq("Action 2 should not have refresh pending", false, qsua2->m_fRefreshPending);
			unitpp::assert_eq("Action 3 should not have refresh pending", false, qsua3->m_fRefreshPending);
			unitpp::assert_eq("Undo result should be kuresSuccess", kuresSuccess, ures);

			m_qacth->Redo(&ures);

			unitpp::assert_eq("Action 1 should not have refresh pending", false, qsua1->m_fRefreshPending);
			unitpp::assert_eq("Action 2 should not have refresh pending", false, qsua2->m_fRefreshPending);
			unitpp::assert_eq("Action 3 should not have refresh pending", false, qsua3->m_fRefreshPending);
			unitpp::assert_eq("Redo result should be kuresSuccess", kuresSuccess, ures);
		}

		/*--------------------------------------------------------------------------------------
			Test the TasksSinceMark method.
		--------------------------------------------------------------------------------------*/
		void testTasksSinceMark()
		{
			HRESULT hr;
			int hMark;
			int cAct;
			int cSeq;
			ComBool fTasksUndo;
			ComBool fTasksRedo;
			StrUni stuUndo;
			StrUni stuRedo;
			StubUndoActionPtr qsua;
			UndoResult ures;

			// make sure that there are no tasks
			hr = m_qacth->get_TasksSinceMark(true, &fTasksUndo);
			hr = m_qacth->get_TasksSinceMark(false, &fTasksRedo);
			unitpp::assert_true("Shouldn't have tasks to undo", !fTasksUndo);
			unitpp::assert_true("Shouldn't have tasks to redo", !fTasksRedo);

			// Put a mark before task 1.
			hr = m_qacth->Mark(&hMark);
			unitpp::assert_eq("No mark after calling Mark()", 1, hMark);

			// Start undo task 1.
			stuUndo = L"Undo Task 1";
			stuRedo = L"Redo Task 1";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			// Add a couple of actions task 1
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();

			// Start undo task 2.
			stuUndo = L"Undo Task 2";
			stuRedo = L"Redo Task 2";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			// Add 3 actions to task 2
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();

			// Make sure there are 5 undo actions and 2 undo tasks.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Should have 5 actions.", 5 , cAct);
			hr = m_qacth->get_UndoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 2 tasks.", 2 , cSeq);

			hr = m_qacth->get_TasksSinceMark(true, &fTasksUndo);
			hr = m_qacth->get_TasksSinceMark(false, &fTasksRedo);
			unitpp::assert_true("Should have tasks to undo", fTasksUndo);
			unitpp::assert_true("Shouldn't have tasks to redo", !fTasksRedo);

			m_qacth->Undo(&ures);

			hr = m_qacth->get_TasksSinceMark(true, &fTasksUndo);
			hr = m_qacth->get_TasksSinceMark(false, &fTasksRedo);
			unitpp::assert_true("Should have tasks to undo", fTasksUndo);
			unitpp::assert_true("Should have tasks to redo", fTasksRedo);

			m_qacth->Undo(&ures);

			hr = m_qacth->get_TasksSinceMark(true, &fTasksUndo);
			hr = m_qacth->get_TasksSinceMark(false, &fTasksRedo);
			unitpp::assert_true("Shouldn't have tasks to undo", !fTasksUndo);
			unitpp::assert_true("Should have tasks to redo", fTasksRedo);
		}

		/*--------------------------------------------------------------------------------------
			Test the TasksSinceMark method when the mark is not at the start of the actions
			list.
		--------------------------------------------------------------------------------------*/
		void testTasksSinceMark_NotStartOfActions()
		{
			HRESULT hr;
			int hMark;
			int cAct;
			int cSeq;
			ComBool fTasksUndo;
			ComBool fTasksRedo;
			StrUni stuUndo;
			StrUni stuRedo;
			StubUndoActionPtr qsua;
			UndoResult ures;

			// make sure that there are no tasks
			hr = m_qacth->get_TasksSinceMark(true, &fTasksUndo);
			hr = m_qacth->get_TasksSinceMark(false, &fTasksRedo);
			unitpp::assert_true("Shouldn't have tasks to undo", !fTasksUndo);
			unitpp::assert_true("Shouldn't have tasks to redo", !fTasksRedo);

			// Start undo task 1.
			stuUndo = L"Undo Task 1";
			stuRedo = L"Redo Task 1";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			// Add a couple of actions task 1
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();

			// Put a mark before task 2.
			hr = m_qacth->Mark(&hMark);
			unitpp::assert_eq("No mark after calling Mark()", 1, hMark);

			// Start undo task 2.
			stuUndo = L"Undo Task 2";
			stuRedo = L"Redo Task 2";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			// Add 2 actions to task 2
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();

			// Start undo task 3.
			stuUndo = L"Undo Task 3";
			stuRedo = L"Redo Task 3";
			hr = m_qacth->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());

			// Add 2 actions to task 3
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			qsua.Attach(NewObj StubUndoAction());
			hr = m_qacth->AddAction(qsua.Ptr());
			m_qacth->EndUndoTask();

			// Make sure there are 6 undo actions and 3 undo tasks.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Should have 6 actions.", 6 , cAct);
			hr = m_qacth->get_UndoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 3 tasks.", 3 , cSeq);

			hr = m_qacth->get_TasksSinceMark(true, &fTasksUndo);
			hr = m_qacth->get_TasksSinceMark(false, &fTasksRedo);
			unitpp::assert_true("Should have tasks to undo", fTasksUndo);
			unitpp::assert_true("Shouldn't have tasks to redo", !fTasksRedo);

			m_qacth->Undo(&ures);

			hr = m_qacth->get_TasksSinceMark(true, &fTasksUndo);
			hr = m_qacth->get_TasksSinceMark(false, &fTasksRedo);
			unitpp::assert_true("Should have tasks to undo", fTasksUndo);
			unitpp::assert_true("Should have tasks to redo", fTasksRedo);

			m_qacth->Undo(&ures);

			hr = m_qacth->get_TasksSinceMark(true, &fTasksUndo);
			hr = m_qacth->get_TasksSinceMark(false, &fTasksRedo);
			unitpp::assert_true("Shouldn't have tasks to undo if we hit the mark", !fTasksUndo);
			unitpp::assert_true("Should have tasks to redo", fTasksRedo);
		}

		/*--------------------------------------------------------------------------------------
			Enhance JohnT: add test for the rest of the interface.
		--------------------------------------------------------------------------------------*/


	public:
		TestUndoStack();  // Constructor is declared; awk script generates body.

		/*--------------------------------------------------------------------------------------
			Create three objects: one empty, one with one run, and one with the same character
			data, but two runs.
		--------------------------------------------------------------------------------------*/
		virtual void Setup()
		{
			m_qacth.Attach(NewObj DummyActionHandler());
		}
		/*--------------------------------------------------------------------------------------
			Delete the objects created in Setup().
		--------------------------------------------------------------------------------------*/
		virtual void Teardown()
		{
			m_qacth.Clear();
		}
	};
}

#endif /*TESTUNDOSTACK_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkfwk-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
/*:End Ignore*/
