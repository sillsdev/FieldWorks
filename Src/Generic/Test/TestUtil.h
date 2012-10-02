/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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

// Clipboard is entirely handled in C# now
//		void GetClipboardFormat(CLIPFORMAT cf, StrUni stuNFC)
//		{
//			// TODO-Linux: Clipboard not supported in C++
//#ifdef WIN32
//			FORMATETC format;
//			STGMEDIUM medium;
//
//			HRESULT hr;
//			IDataObjectPtr qdobj;
//			for (int i = 0; i < 10; i++) // try up to 20 times.
//			{
//				hr = ::OleGetClipboard(&qdobj);
//				if (SUCCEEDED(hr))
//					break;
//				::Sleep(100);
//			}
//			CheckHr(hr);
//
//			format.cfFormat = static_cast<unsigned short>(cf);
//			format.ptd = NULL;
//			format.dwAspect = DVASPECT_CONTENT;
//			format.lindex = -1;
//			format.tymed = TYMED_HGLOBAL;
//
//			hr = qdobj->GetData(&format, &medium);
//			if (hr == S_OK)
//			{
//				StrUni stu;
//				if (medium.tymed == TYMED_HGLOBAL && medium.hGlobal)
//				{
//					const char * pszClip;
//					const wchar * pwszClip;
//					switch(cf)
//					{
//						case CF_OEMTEXT:
//						case CF_TEXT:
//							pszClip = (const char *)::GlobalLock(medium.hGlobal);
//							stu = pszClip;
//							break;
//
//						case CF_UNICODETEXT:
//						default:
//							pwszClip = (const wchar *)::GlobalLock(medium.hGlobal);
//							stu = pwszClip;
//					}
//					::GlobalUnlock(medium.hGlobal);
//				}
//				::ReleaseStgMedium(&medium);
//				unitpp::assert_true("Clipboard data normalized correctly in some format",
//					stu.Equals(stuNFC.Chars(), stuNFC.Length()));
//			}
//			else
//				unitpp::assert_true("GetData failed in GetClipboardFormat", 0);
//#endif
//		}
//
//		void testGetDataNormalized()
//		{
//			// TODO-Linux: Clipboard not supported in C++
//#ifdef WIN32
//			StrUni stuIn = L"Te\x0301sting";
//			StrUni stuNFC = L"T\x00e9sting";
//			IDataObjectPtr qdobj;
//			StringDataObject::Create(const_cast<OLECHAR *>(stuIn.Chars()), &qdobj);
//			if (::OleSetClipboard(qdobj) == S_OK)
//			{
//				ModuleEntry::SetClipboard(qdobj);
//			}
//			qdobj.Clear(); // Let go of our connection to the clipboard; seems to make tests at least more reliable.
//
//			GetClipboardFormat(CF_UNICODETEXT, stuNFC);
//			GetClipboardFormat(CF_OEMTEXT, stuNFC);
//			GetClipboardFormat(CF_TEXT, stuNFC);
//
//			::OleSetClipboard(NULL);		// reset the clipboard {release the object}
//#endif
//		}

	public:
		TestUtil();
	};
}

#endif /*TESTUTIL_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkGenLib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
