/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestSqlUndoAction.h
Responsibility:
Last reviewed:

	Unit tests for the SqlUndoAction class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTSQLUNDOACTION_H_INCLUDED
#define TESTSQLUNDOACTION_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
	class TestSqlUndoAction : public unitpp::suite
	{
		ISqlUndoActionPtr m_qsqlua;

		void testNullArgs()
		{
			unitpp::assert_true("Non-null m_qsqlua after setup", m_qsqlua.Ptr() != 0);
			HRESULT hr;
#ifndef _DEBUG
			try{
				CheckHr(hr = m_qsqlua->QueryInterface(IID_NULL, NULL));
				unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable & thr){
				unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
#endif
			try{
				CheckHr(hr = m_qsqlua->AddRedoCommand(NULL, NULL, NULL));
				unitpp::assert_eq("AddRedoCommand(NULL, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("AddRedoCommand(NULL, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qsqlua->AddRedoReloadInfo(NULL, NULL, NULL, 0, 0, NULL));
			unitpp::assert_eq("AddRedoReloadInfo(NULL, NULL, NULL, 0, 0, NULL) HRESULT",
				S_OK, hr);
			CheckHr(hr = m_qsqlua->AddUndoReloadInfo(NULL, NULL, NULL, 0, 0, NULL));
			unitpp::assert_eq("AddUndoReloadInfo(NULL, NULL, NULL, 0, 0, NULL) HRESULT",
				S_OK, hr);
			IUndoActionPtr qua;
			CheckHr(hr = m_qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
			unitpp::assert_eq("QueryInterface(IID_IUndoAction, (void **)&qua) HRESULT",
				S_OK, hr);
			try{
				CheckHr(hr = qua->IsDataChange(NULL));
				unitpp::assert_eq("IsDataChange(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("IsDataChange(NULL) HRESULT", E_POINTER, thr.Result());
			}
		}
	public:
		TestSqlUndoAction();

		virtual void Setup()
		{
			SqlUndoAction::CreateCom(NULL, IID_ISqlUndoAction, (void **)&m_qsqlua);
		}
		virtual void Teardown()
		{
			m_qsqlua.Clear();
		}
	};
}

#endif /*TESTSQLUNDOACTION_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
