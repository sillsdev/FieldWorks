/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestGetNormalizedClipboardData.h
Responsibility:
Last reviewed:

	Unit tests for the functions/classes from AppCore/CmDataObject normalization
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTGETNORMALIZEDCLIPBOARDDATA_H_INCLUDED
#define TESTGETNORMALIZEDCLIPBOARDDATA_H_INCLUDED

#pragma once

#include "testAfLib.h"

namespace TestAfLib
{
	class TestNFDClipboardOutput : public unitpp::suite
	{
		void GetClipboardFormat(CLIPFORMAT cf, StrUni stuNFC)
		{
			FORMATETC format;
			STGMEDIUM medium;

			IDataObjectPtr qdobj;
			CheckHr(::OleGetClipboard(&qdobj));

			format.cfFormat = static_cast<unsigned short>(cf);
			format.ptd = NULL;
			format.dwAspect = DVASPECT_CONTENT;
			format.lindex = -1;
			format.tymed = TYMED_HGLOBAL;

			HRESULT hr = qdobj->GetData(&format, &medium);
			if (hr == S_OK)
			{
				StrUni stu;
				if (medium.tymed == TYMED_HGLOBAL && medium.hGlobal)
				{
					const char * pszClip;
					const wchar * pwszClip;
					switch(cf)
					{
						case CF_OEMTEXT:
						case CF_TEXT:
							pszClip = (const char *)::GlobalLock(medium.hGlobal);
							stu = pszClip;
							break;

						case CF_UNICODETEXT:
						default:
							pwszClip = (const wchar *)::GlobalLock(medium.hGlobal);
							stu = pwszClip;
					}
					::GlobalUnlock(medium.hGlobal);
				}
				::ReleaseStgMedium(&medium);
				unitpp::assert_true("Clipboard data normalized correctly in some format",
					stu.Equals(stuNFC.Chars(), stuNFC.Length()));
			}
			else
				unitpp::assert_true("GetData failed in GetClipboardFormat", 0);
		}

//		This now requires a database connection, or more thinking than i want to do.
//		void test GetDataNormalized()
//		{
//			StrUni stuIn = L"Te\x0301sting";
//			StrUni stuNFC = L"T\x00e9sting";
//			IDataObjectPtr qdobj;
//
//			ITsStringPtr qtssIn;
//			ITsStringPtr qtssOut;
//			ILgWritingSystemFactoryPtr qwsf;
//			int ws;
//			SmartBstr sbstrEng(L"en");
//			qwsf->GetWsFromStr(sbstrEng, &ws);
//			ITsStrFactoryPtr qtsf;
//			qtsf.CreateInstance(CLSID_TsStrFactory);
//			qtsf->MakeStringRgch(stuIn.Chars(), stuIn.Length(), ws, &qtssIn);
//
//			CmDataObject::Create( L"lalala", L"hehehe", (HVO)1, 2, qtssIn, 2, &qdobj );
//
//			if (::OleSetClipboard(qdobj) == S_OK)
//			{
//				ModuleEntry::SetClipboard(qdobj);
//			}
//
//			GetClipboardFormat(CF_UNICODETEXT, stuNFC);
//			GetClipboardFormat(CF_OEMTEXT, stuNFC);
//			GetClipboardFormat(CF_TEXT, stuNFC);
//
//			::OleSetClipboard(NULL);		// reset the clipboard {release the object}
//		}


	public:
		TestNFDClipboardOutput();
	};
}

#endif /*TESTGETNORMALIZEDCLIPBOARDDATA_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkaflib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
