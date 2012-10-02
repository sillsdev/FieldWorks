/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2006 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestErrorHandling.h
Responsibility:
Last reviewed:

	Unit tests for error handling and the methods in StackDumper.cpp, HandleThrowable, CheckHr...
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTERRORHANDLING_H_INCLUDED
#define TESTERRORHANDLING_H_INCLUDED

#pragma once

#include "testGenericLib.h"

namespace TestGenericLib
{
	static DummyFactory g_fact(_T("SIL.TestErrorHandling")); // For END_COM_METHOD macros
	class TestErrorHandling : public unitpp::suite
	{
		// Our simulated COM method.
		HRESULT ComMethod(HRESULT in)
		{
			BEGIN_COM_METHOD
				CheckHr(in);
				return in;
			END_COM_METHOD(g_fact, IID_IAccessible)
		}

		// Simulated COM method that calls another method
		HRESULT OuterComMethod(HRESULT in)
		{
			BEGIN_COM_METHOD
				CheckHr(ComMethod(in));
				return in;
			END_COM_METHOD(g_fact, IID_IAccessible)
		}

		// Tests that return S_OK doesn't set up error info
		void testOkReturn()
		{
			HRESULT hr = ComMethod(S_OK);
			unitpp::assert_eq("Got wrong return value for S_OK", S_OK, hr);

			IErrorInfoPtr qerrinfo;
			::GetErrorInfo(0, &qerrinfo);
			unitpp::assert_true("S_OK set up error info", !qerrinfo);
		}

		// Tests that a programming error in a directly called COM method sets up the error info
		// object
		void testProgrammingErrorInCalledMethod()
		{
#ifdef WIN32
			HRESULT hr = ComMethod(E_UNEXPECTED);
			unitpp::assert_eq("Got wrong return value for E_UNEXPECTED", E_UNEXPECTED, hr);

			IErrorInfoPtr qerrinfo;
			::GetErrorInfo(0, &qerrinfo);
			unitpp::assert_true("E_UNEXPECTED didn't set up error info", qerrinfo);
			SmartBstr descr;
			qerrinfo->GetDescription(&descr);
			StrUni strDescr(descr.Bstr());
			unitpp::assert_true("E_UNEXPECTED set wrong description",
				strDescr.FindStr(L"\n---***More***---\nStack Dump:") == 0);
#else
			// TODO-Linux: port
#endif
		}

		// Tests that a programming error in a method called by a COM method sets up the error
		// info object
		void testProgrammingErrorInSubMethod()
		{
#ifdef WIN32
			HRESULT hr = OuterComMethod(E_INVALIDARG);
			unitpp::assert_eq("Got wrong return value for E_INVALIDARG", E_INVALIDARG, hr);

			IErrorInfoPtr qerrinfo;
			::GetErrorInfo(0, &qerrinfo);
			unitpp::assert_true("E_INVALIDARG didn't set up error info", qerrinfo);
			SmartBstr descr;
			qerrinfo->GetDescription(&descr);
			StrUni strDescr(descr.Bstr());
			unitpp::assert_true("E_INVALIDARG set wrong description",
				strDescr.FindStr(L"\n---***More***---\nStack Dump:") == 0);
#else
			// TODO-Linux: port
#endif
		}

		// Tests that an error in a directly called COM method sets up the error info
		// object
		void testOtherErrorInCalledMethod()
		{
#ifdef WIN32
			HRESULT hr = ComMethod(E_FAIL);
			unitpp::assert_eq("Got wrong return value for E_FAIL", E_FAIL, hr);

			IErrorInfoPtr qerrinfo;
			::GetErrorInfo(0, &qerrinfo);
			unitpp::assert_true("E_FAIL didn't set up error info", qerrinfo);
			SmartBstr descr;
			qerrinfo->GetDescription(&descr);
			StrUni strDescr(descr.Bstr());
			unitpp::assert_true("E_FAIL set wrong description",
				strDescr.FindStr(L"\n---***More***---\nStack Dump:") == 0);
#else
			// TODO-Linux: port
#endif
		}

		// Tests that an error in a method called by a COM method sets up the error
		// info object
		void testOtherErrorInSubMethod()
		{
#ifdef WIN32
			HRESULT hr = OuterComMethod(E_ABORT);
			unitpp::assert_eq("Got wrong return value for E_ABORT", E_ABORT, hr);

			IErrorInfoPtr qerrinfo;
			::GetErrorInfo(0, &qerrinfo);
			unitpp::assert_true("E_ABORT didn't set up error info", qerrinfo);
			SmartBstr descr;
			qerrinfo->GetDescription(&descr);
			StrUni strDescr(descr.Bstr());
			unitpp::assert_true("E_ABORT set wrong description",
				strDescr.FindStr(L"\n---***More***---\nStack Dump:") == 0);
#else
			// TODO-Linux: port
#endif
		}

		// Tests that CheckHr clears the error info and that error info is included in the exception
		// instead
		void testCheckHr()
		{
#ifdef WIN32
			try
			{
				CheckHr(ComMethod(E_ACCESSDENIED));
				unitpp::assert_fail("CheckHr didn't throw an exception");
			}
			catch(Throwable& thr)
			{
				IErrorInfoPtr qerrinfo;
				::GetErrorInfo(0, &qerrinfo);
				unitpp::assert_true("CheckHr didn't clean error info", !qerrinfo);
				unitpp::assert_true("No error info in exception", thr.GetErrorInfo());
			}
#else
			// TODO-Linux: port
#endif
		}

	public:
		TestErrorHandling();
	};
}

#endif /*TESTERRORHANDLING_H_INCLUDED*/
