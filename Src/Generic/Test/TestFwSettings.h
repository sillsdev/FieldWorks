/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2006-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestFwSettings.h
Responsibility:
Last reviewed:

	Unit tests for the functions/classes from Generic/FwSettings.cpp
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTFWSETTINGS_H_INCLUDED
#define TESTFWSETTINGS_H_INCLUDED

#pragma once

#include "testGenericLib.h"

namespace TestGenericLib
{
	class TestFwSettings : public unitpp::suite
	{
		const achar * m_CompleteRoot;
		// Tests getting and setting a DWORD with subkey specified
		void testStaticDwordWithSubkey()
		{
#ifdef WIN32
			m_CompleteRoot = _T("GenericTests\\SetDword");
			FwSettings::SetDword(_T("Software\\SIL\\Fieldworks\\GenericTests"),
				_T("SetDword"), _T("DwordValue"), 4711);

			DWORD dword = 0;
			bool fRet = FwSettings::GetDword(_T("Software\\SIL\\Fieldworks\\GenericTests"),
				_T("SetDword"), _T("DwordValue"), &dword);
			unitpp::assert_true("GetDword with subkey returned false", fRet);
			unitpp::assert_eq("GetDword with subkey returned wrong value", (DWORD)4711, dword);
#else
			// TODO-Linux: port
#endif
		}

		// Tests getting and setting a DWORD with no subkey specified
		void testStaticDwordNoSubkey()
		{
#ifdef WIN32
			FwSettings settings;
			m_CompleteRoot = _T("GenericTests");
			settings.SetRoot(_T("GenericTests"));
			FwSettings::SetDword(_T("Software\\SIL\\Fieldworks\\GenericTests"),
				NULL, _T("DwordValue"), 4711);

			DWORD dword = 0;
			bool fRet = FwSettings::GetDword(_T("Software\\SIL\\Fieldworks\\GenericTests"),
				NULL, _T("DwordValue"), &dword);
			unitpp::assert_true("GetDword no subkey returned false", fRet);
			unitpp::assert_eq("GetDword no subkey returned wrong value", (DWORD)4711, dword);
#else
			// TODO-Linux: port
#endif
		}

		// Tests getting and setting a string with subkey specified
		void testStaticStringWithSubkey()
		{
#ifdef WIN32
			m_CompleteRoot = _T("GenericTests\\SetString");
			StrApp origValue(_T("bla"));
			FwSettings::SetString(_T("Software\\SIL\\Fieldworks\\GenericTests"),
				_T("SetString"), _T("StringValue"), origValue);

			StrApp value;
			bool fRet = FwSettings::GetString(_T("Software\\SIL\\Fieldworks\\GenericTests"),
				_T("SetString"), _T("StringValue"), value);
			unitpp::assert_true("GetString with subkey returned false", fRet);
			unitpp::assert_eq("GetString with subkey returned wrong value", 0, value.Compare(_T("bla")));
#else
			// TODO-Linux: port
#endif
		}

		// Tests getting and setting a Bool value with subkey specified
		void testStaticBoolWithSubkey()
		{
#ifdef WIN32
			m_CompleteRoot = _T("GenericTests\\SetBool");

			// Test setting true
			FwSettings::SetBool(_T("Software\\SIL\\Fieldworks\\GenericTests"),
				_T("SetBool"), _T("BoolValue"), true);

			bool fValue = false;
			bool fRet = FwSettings::GetBool(_T("Software\\SIL\\Fieldworks\\GenericTests"),
				_T("SetBool"), _T("BoolValue"), &fValue);
			unitpp::assert_true("1. GetBool with subkey returned false", fRet);
			unitpp::assert_true("1. GetBool with subkey returned wrong value", fValue);

			// Test setting false
			FwSettings::SetBool(_T("Software\\SIL\\Fieldworks\\GenericTests"),
				_T("SetBool"), _T("BoolValue"), false);

			fValue = true;
			fRet = FwSettings::GetBool(_T("Software\\SIL\\Fieldworks\\GenericTests"),
				_T("SetBool"), _T("BoolValue"), &fValue);
			unitpp::assert_true("2. GetBool with subkey returned false", fRet);
			unitpp::assert_true("2. GetBool with subkey returned wrong value", !fValue);
#else
			// TODO-Linux: port
#endif
		}
	public:
		TestFwSettings();

		virtual void Teardown()
		{
#ifdef WIN32
			// TODO-Linux: enable when above tests are ported
			FwSettings settings;
			settings.SetRoot(m_CompleteRoot);
			settings.RemoveAll();
#endif
		}
	};
}

#endif /*TESTFWSETTINGS_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkGenLib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
