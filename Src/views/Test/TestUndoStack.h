/*-------------------------------------------------------------------*//*:Ignore these comments.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestUndoStack.h
Responsibility: John Thomson
Last reviewed:

	Unit tests for the ActionHandler class (which I hope to one day rename UndoStack).
----------------------------------------------------------------------------------------------*/
#ifndef TESTUNDOSTACK_H_INCLUDED
#define TESTUNDOSTACK_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
	// Allows access to protected variables
	class DummyActionHandler : public ActionHandler
	{
	public:
		// Gets the last location of the mark
		int GetLastMarkLocation()
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
		ulong m_cref;

		StubUndoAction()
		{
			m_cref = 1;
			m_fDone = true;
			m_fCommitted = false;
			m_fForceFailure = false;
			m_fForceError = false;
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

		STDMETHOD_(UCOMINT32, AddRef)(void)
		{
			Assert(m_cref > 0);
			return ++m_cref;
		}

		STDMETHOD_(UCOMINT32, Release)(void)
		{
			Assert(m_cref > 0);
			if (--m_cref > 0)
				return m_cref;

			m_cref = 1;
			delete this;
			return 0;
		}

		// Reverses (or "un-does") an action.
		STDMETHOD(Undo)(ComBool * pfSuccess)
		{
			AssertPtr(pfSuccess);
			Assert(!m_fCommitted);
			Assert(m_fDone);
			*pfSuccess = m_fDone && !m_fCommitted;
			if (!m_fForceFailure && !m_fForceError)
				m_fDone = false;
			if (m_fForceFailure)
				*pfSuccess = false;
			if (m_fForceError)
				return E_FAIL;
			return S_OK;
		}

		// Re-applies (or "re-does") an action.
		STDMETHOD(Redo)(ComBool * pfSuccess)
		{
			AssertPtr(pfSuccess);
			Assert(!m_fCommitted);
			Assert(!m_fDone);
			*pfSuccess = !m_fDone && !m_fCommitted;
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
		STDMETHOD(get_IsDataChange)(ComBool * pfRet)
		{
			*pfRet = true;
			return S_OK;
		}

		// Returns true because this can be redone.
		STDMETHOD(get_IsRedoable)(ComBool * pfRet)
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
		STDMETHOD(Undo)(ComBool * pfSuccess)
		{
			int hmark;
			CheckHr(m_pacth->Mark(&hmark));
			*pfSuccess = true;
			return S_OK;
		}

		// Re-applies (or "re-does") an action.
		STDMETHOD(Redo)(ComBool * pfSuccess)
		{
			int hmark;
			CheckHr(m_pacth->Mark(&hmark));
			*pfSuccess = true;
			return S_OK;
		}
	};
	DEFINE_COM_PTR(StubUndoActionMakeMark);

	class TestUndoStack : public unitpp::suite
	{
		DummyActionHandlerPtr m_qacth;
		StubUndoActionPtr m_qsua1, m_qsua2, m_qsua3;

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
			ComBool fReturnVal;
			hr = m_qacth->CollapseToMark(hMark, stuUndo.Bstr(), stuRedo.Bstr(), &fReturnVal);

			// Now make sure there are 5 undo actions and 1 undo task.
			hr = m_qacth->get_UndoableActionCount(&cAct);
			unitpp::assert_eq("Should have 5 actions.", 5 , cAct);
			hr = m_qacth->get_UndoableSequenceCount(&cSeq);
			unitpp::assert_eq("Should have 1 tasks.", 1 , cSeq);

			SmartBstr sbstr;
			// Check that our task's undo text is correct.
			hr = m_qacth->GetUndoText(&sbstr);
			unitpp::assert_true("Undo Text wrong.",	sbstr == StrUni(L"Collapsed Undo Task").Chars());

			// Do a single undo and make sure the collapsed task's redo text is correct.
			UndoResult ures;
			hr = m_qacth->Undo(&ures);
			hr = m_qacth->GetRedoText(&sbstr);
			unitpp::assert_true("Redo Text wrong.", sbstr == StrUni(L"Collapsed Redo Task").Chars());
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
			ComBool fReturnVal;
			hr = m_qacth->CollapseToMark(hMark, stuUndo.Bstr(), stuRedo.Bstr(), &fReturnVal);

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
			unitpp::assert_eq("Undo should succeed", kuresSuccess, undoRes);

			// Collapse to mark (shouldn't create a task at all).
			stuUndo = L"This Undo Task should not get created";
			stuRedo = L"This Redo Task should not get created";
			ComBool fReturnVal;
			hr = m_qacth->CollapseToMark(hMark, stuUndo.Bstr(), stuRedo.Bstr(), &fReturnVal);

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
			unitpp::assert_eq("Undo should succeed", kuresSuccess, undoRes);

			// Collapse tasks 1 and 2 into a single undo task.
			stuUndo = L"Collapsed Undo Task";
			stuRedo = L"Collapsed Redo Task";
			ComBool fReturnVal;
			hr = m_qacth->CollapseToMark(hMark, stuUndo.Bstr(), stuRedo.Bstr(), &fReturnVal);

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
			unitpp::assert_true("Undo Text wrong.",	sbstr == StrUni(L"Collapsed Undo Task").Chars());

			// Do a single undo and make sure the collapsed task's redo text is correct.
			UndoResult ures;
			hr = m_qacth->Undo(&ures);
			hr = m_qacth->GetRedoText(&sbstr);
			unitpp::assert_true("Redo Text wrong.", sbstr == StrUni(L"Collapsed Redo Task").Chars());
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

		Linux - this test if failing on unitpp::assert_eq("Initial action count should be 2", 2, cAct);
		cAct == 3
		Caused by testRollback finishing with a cAct of 1. Are Tests run in a different order on Windows?
		EB/2009-08-25: The tests work on both Linux and Windows if the Setup()/Teardown()
		methods below are run after each test, i.e. not called SuiteSetup()/SuiteTeardown().
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
