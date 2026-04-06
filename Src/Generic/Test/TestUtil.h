/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2026 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestUtil.h
Responsibility:
Last reviewed:

	Unit tests for the functions/classes from Generic/Util.cpp
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTUTIL_H_INCLUDED
#define TESTUTIL_H_INCLUDED

#pragma once

#include <sys/stat.h>
#include "testGenericLib.h"

namespace TestGenericLib
{
	class TestUtil : public unitpp::suite
	{
		void testFailure()
		{
			unitpp::assert_fail("Oh, what fun it is to type in a one-line failing test, hey!");
		}
		void testGetPrimeNear()
		{
			uint u;
			u = GetPrimeNear(0);
			unitpp::assert_eq("GetPrimeNear(0)", (uint)3, u);
			u = GetPrimeNear(1024);
			unitpp::assert_eq("GetPrimeNear(1024)", (uint)1021, u);
			u = GetPrimeNear(1048576);
			unitpp::assert_eq("GetPrimeNear(1048576)", (uint)1048573, u);
			u = GetPrimeNear(4294967295U);
			unitpp::assert_eq("GetPrimeNear(4294967295)", 4294967291U, u);
		}
		void testGetLargerPrime()
		{
			uint u;
			u = GetLargerPrime(0);
			unitpp::assert_eq("GetLargerPrime(0)", (uint)3, u);
			u = GetLargerPrime(1024);
			unitpp::assert_eq("GetLargerPrime(1024)", (uint)2039, u);
			u = GetLargerPrime(1048576);
			unitpp::assert_eq("GetLargerPrime(1048576)", (uint)2097143, u);
			u = GetLargerPrime(4294967295U);
			unitpp::assert_eq("GetLargerPrime(4294967295)", 4294967291U, u);
		}
		void testGetSmallerPrime()
		{
			uint u;
			u = GetSmallerPrime(0);
			unitpp::assert_eq("GetSmallerPrime(0)", (uint)3, u);
			u = GetSmallerPrime(1024);
			unitpp::assert_eq("GetSmallerPrime(1024)", (uint)1021, u);
			u = GetSmallerPrime(1048576);
			unitpp::assert_eq("GetSmallerPrime(1048576)", (uint)1048573, u);
			u = GetSmallerPrime(4294967295U);
			unitpp::assert_eq("GetSmallerPrime(4294967295)", 4294967291U, u);
		}
		void testGetGcdU()
		{
			uint u;
			u = GetGcdU((uint)1, (uint)12345);
			unitpp::assert_eq("GetGcdU(1, 12345)", (uint)1, u);
			u = GetGcdU((uint)12345, (uint)12345);
			unitpp::assert_eq("GetGcdU(12345, 12345)", (uint)12345, u);
			u = GetGcdU((uint)12345, (uint)1);
			unitpp::assert_eq("GetGcdU(12345, 1)", (uint)1, u);
			u = GetGcdU((uint)7500, (uint)1000);
			unitpp::assert_eq("GetGcdU(7500, 1000)", (uint)500, u);
		}

		void testDirectoryFinder()
		{
			// FwRootCodeDir.
			StrUni stuRoot = DirectoryFinder::FwRootCodeDir();
			StrUni stuDummy = L"Some silly dummy file xhkjasoiy";
			unitpp::assert_true("Unexpected file allegedly found", ::GetFileAttributes(stuDummy.Chars()) == INVALID_FILE_ATTRIBUTES );
			StrUni stuFwZip = stuRoot;
			stuFwZip += L"/XceedZip.dll";
			unitpp::assert_true("Expected file found in FW root directory", ::GetFileAttributes(stuFwZip.Chars()) !=INVALID_FILE_ATTRIBUTES );

			//FwTemplateDir();
			StrUni stuTemplate = DirectoryFinder::FwTemplateDir();
			StrUni stuTemplateFile = stuTemplate;
			// BlankLangProj.mdf is not available on the build machine in this directory.
			stuTemplateFile += L"/NewLangProj.fwdata";
			unitpp::assert_true("Expected file found in FW template directory", ::GetFileAttributes(stuTemplateFile.Chars()) !=INVALID_FILE_ATTRIBUTES );
		}
		//ComputeHashRgb
		//CaseSensitiveComputeHash
		//CaseSensitiveComputeHashCch
		//CaseInsensitiveComputeHash
		//CaseInsensitiveComputeHashCch
		//SameObject
		//ReadString(IStream * pstrm, StrBase<wchar> & stb);
		//ReadString(IStream * pstrm, StrBase<schar> & stb);
		//WriteString(IStream * pstrm, StrBase<wchar> & stb);
		//WriteString(IStream * pstrm, StrBase<schar> & stb);
		//GetFullPathName(const achar * psz, StrAnsi & staPath)
		// ...

	public:
		TestUtil();
	};
}

#endif /*TESTUTIL_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkGenLib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
