/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLgTsDataObject.h
Responsibility:
Last reviewed:

	Unit tests for the LgTsDataObject class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGTSDATAOBJECT_H_INCLUDED
#define TESTLGTSDATAOBJECT_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for LgTsDataObject
	 ******************************************************************************************/
	class TestLgTsDataObject : public unitpp::suite
	{
		ILgTsDataObjectPtr m_qtsdo;

		void testNullArgs()
		{
			unitpp::assert_true("m_qtsdo", m_qtsdo.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qtsdo->Init(NULL));
				unitpp::assert_eq("Init(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Init(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtsdo->GetClipboardType(NULL));
				unitpp::assert_eq("GetClipboardType(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetClipboardType(NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

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

		void testGetDataNormalized()
		{
			StrUni stuIn = L"Te\x0301sting";
			StrUni stuNFC = L"T\x00e9sting";

			// Get a copy of the 'stuIn' as a TsString
			ITsStringPtr qtssIn;
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			qtsf->MakeStringRgch(stuIn.Chars(), stuIn.Length(), kwsEng, &qtssIn);

			ILgWritingSystemFactoryPtr qwsf;
			CreateTestWritingSystemFactory(&qwsf);

			ILgTsStringPlusWssPtr qtsswss;
			LgTsStringPlusWss::CreateCom(NULL, IID_ILgTsStringPlusWss, (void **)&qtsswss);

			CheckHr(qtsswss->putref_String(qwsf, qtssIn));
			m_qtsdo->Init(qtsswss);

			IDataObjectPtr qdobj;
			CheckHr(m_qtsdo->QueryInterface(IID_IDataObject, (void **)&qdobj));
			if (::OleSetClipboard(qdobj) == S_OK)
			{
				ModuleEntry::SetClipboard(qdobj);
			}

			GetClipboardFormat(CF_UNICODETEXT, stuNFC);
			GetClipboardFormat(CF_OEMTEXT, stuNFC);
			GetClipboardFormat(CF_TEXT, stuNFC);

			::OleSetClipboard(NULL);		// reset the clipboard {release the object}
			qwsf->Shutdown();
		}

	public:
		TestLgTsDataObject();
		virtual void SuiteSetup()
		{
			LgTsDataObject::CreateCom(NULL, IID_ILgTsDataObject, (void **)&m_qtsdo);
		}
		virtual void SuiteTeardown()
		{
			m_qtsdo.Clear();
		}
	};
}

#endif /*TESTLGTSDATAOBJECT_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
