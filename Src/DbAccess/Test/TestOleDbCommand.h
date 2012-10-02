/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2006 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestOleDbCommand.h
Responsibility:
Last reviewed:

	Unit tests for the OleDbCommand class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTOLEDBCOMMAND_H_INCLUDED
#define TESTOLEDBCOMMAND_H_INCLUDED

#pragma once

#include "testDbAccess.h"

namespace TestDbAccess
{
	static DummyFactory g_fact(_T("SIL.TestErrorHandling")); // For END_COM_METHOD macros

	class TestOleDbCommand : public unitpp::suite
	{
		// Derived OleDbCommand class so that we can access FullErrorCheck method.
		class DummyOleDbCommand: OleDbCommand
		{
		public:
			// Our simulated COM method.
			HRESULT ComMethod(HRESULT in)
			{
				BEGIN_COM_METHOD
					return FullErrorCheck(in, this, IID_IOleDbCommand);
				END_COM_METHOD(g_fact, IID_IOleDbCommand)
			}
		};

		// Helper method
		HRESULT Method(HRESULT in)
		{
			DummyOleDbCommand * pdodc = NewObj DummyOleDbCommand;
			HRESULT hr = pdodc->ComMethod(in);
			delete pdodc;
			return hr;
		}

		// Tests that return S_OK doesn't set up error info
		void testOkReturn()
		{
			HRESULT hr = Method(S_OK);
			unitpp::assert_eq("Got wrong return value for S_OK", S_OK, hr);

			IErrorInfoPtr qerrinfo;
			::GetErrorInfo(0, &qerrinfo);
			unitpp::assert_true("S_OK set up error info", !qerrinfo);
		}

		// Tests that a programming error in a directly called COM method sets up the error info
		// object
		void testProgrammingErrorInCalledMethod()
		{
			HRESULT hr = Method(E_UNEXPECTED);
			unitpp::assert_eq("Got wrong return value for E_UNEXPECTED", E_UNEXPECTED, hr);

			IErrorInfoPtr qerrinfo;
			::GetErrorInfo(0, &qerrinfo);
			unitpp::assert_true("E_UNEXPECTED didn't set up error info", qerrinfo);
			SmartBstr descr;
			qerrinfo->GetDescription(&descr);
			StrUni strDescr(descr.Bstr());
			unitpp::assert_true("E_UNEXPECTED set wrong description",
				strDescr.FindStr(L"\r\n\r\nFurther details:\r\nA problem occurred executing the SQL code") == 0);
		}

		// Tests that an error in a directly called COM method sets up the error info
		// object
		void testOtherErrorInCalledMethod()
		{
			HRESULT hr = Method(E_FAIL);
			unitpp::assert_eq("Got wrong return value for E_FAIL", E_FAIL, hr);

			IErrorInfoPtr qerrinfo;
			::GetErrorInfo(0, &qerrinfo);
			unitpp::assert_true("E_FAIL didn't set up error info", qerrinfo);
			SmartBstr descr;
			qerrinfo->GetDescription(&descr);
			StrUni strDescr(descr.Bstr());
			unitpp::assert_true("E_FAIL set wrong description",
				strDescr.FindStr(L"\r\n\r\nFurther details:\r\nA problem occurred executing the SQL code") == 0);
		}

	public:
		TestOleDbCommand();
	};
}

#endif /*TESTOLEDBCOMMAND_H_INCLUDED*/
