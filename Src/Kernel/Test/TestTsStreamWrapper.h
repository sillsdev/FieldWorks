/*-------------------------------------------------------------------*//*:Ignore these comments.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestTsStreamWrapper.h
Responsibility:
Last reviewed:

	Unit tests for the TsString classes (ITsString interface, TsStrSingle, and TsStrMulti).
----------------------------------------------------------------------------------------------*/
#ifndef TESTTSSTREAMWRAPPER_H_INCLUDED
#define TESTTSSTREAMWRAPPER_H_INCLUDED

#pragma once

#include "testFwKernel.h"
//#include "LanguageTlb.h"
#include <stdio.h>

namespace TestFwKernel
{
	class TestTsStreamWrapper : public unitpp::suite
	{
		ITsStringPtr m_qtssEmpty;
		ITsStringPtr m_qtssOneRun; // "This is a test!"
		ITsStringPtr m_qtssTwoRuns; // "This is<bold> a test!</bold>"
		ITsStrFactoryPtr m_qtsf;
		ILgWritingSystemFactoryPtr m_qwsf;
		int m_wsEng;
		int m_wsStk;
		ITsStreamWrapperPtr m_qstfWrapper;

		/*--------------------------------------------------------------------------------------
			Test the COM methods that read and write TsStrings to XML.
		--------------------------------------------------------------------------------------*/
		void testTsStringToFromXml()
		{
			ITsStreamWrapperPtr qsftWrapRead;
			ITsStringPtr qtss;
			ComBool fEqual;

			qsftWrapRead.CreateInstance(CLSID_TsStreamWrapper);
			SmartBstr sbstrEmpty;
			m_qstfWrapper->WriteTssAsXml(m_qtssEmpty, m_qwsf, 0, 0, false);
			m_qstfWrapper->get_Contents(&sbstrEmpty);
			qsftWrapRead->put_Contents(sbstrEmpty);
			qsftWrapRead->ReadTssFromXml(m_qwsf, &qtss);
			qtss->Equals(m_qtssEmpty, &fEqual);
			unitpp::assert_true("Empty string recovered", fEqual);

			m_qstfWrapper->put_Contents(NULL);
			SmartBstr sbstrOneRun;
			m_qstfWrapper->WriteTssAsXml(m_qtssOneRun, m_qwsf, 0, 0, false);
			m_qstfWrapper->get_Contents(&sbstrOneRun);
			qsftWrapRead->put_Contents(sbstrOneRun);
			qsftWrapRead->ReadTssFromXml(m_qwsf, &qtss);
			qtss->Equals(m_qtssOneRun, &fEqual);
			unitpp::assert_true("OneRun string recovered", fEqual);

			m_qstfWrapper->put_Contents(NULL);
			SmartBstr sbstrTwoRuns;
			m_qstfWrapper->WriteTssAsXml(m_qtssTwoRuns, m_qwsf, 0, 0, false);
			m_qstfWrapper->get_Contents(&sbstrTwoRuns);
			qsftWrapRead->put_Contents(sbstrTwoRuns);
			qsftWrapRead->ReadTssFromXml(m_qwsf, &qtss);
			qtss->Equals(m_qtssTwoRuns, &fEqual);
			unitpp::assert_true("TwoRuns string recovered", fEqual);
		}

	public:
		TestTsStreamWrapper();

		/*--------------------------------------------------------------------------------------
			Create three objects: one empty, one with one run, and one with the same character
			data, but two runs.
		--------------------------------------------------------------------------------------*/
		virtual void Setup()
		{
			// Make sure to create everything through the interfaces.
			// If we use the copies compiled into this test program,
			// equality of TsTextProps is messed up, because this code and
			// the real DLL have different sets of unique props.
			m_qtsf.CreateInstance(CLSID_TsStrFactory);
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptWs, 0, m_wsEng);
			ITsTextPropsPtr qttp1;
			qtpb->GetTextProps(&qttp1);

			qtpb->SetIntPropValues(ktptBold, ktpvEnum, kttvForceOn);
			ITsTextPropsPtr qttp2;
			qtpb->GetTextProps(&qttp2);

			m_qtsf->MakeStringRgch(L"", 0, m_wsEng, &m_qtssEmpty);
			m_qtsf->MakeStringRgch(g_pszTest, g_cchTest, m_wsEng, &m_qtssOneRun);

			ITsStrBldrPtr qtsb;
			m_qtssOneRun->GetBldr(&qtsb);
			qtsb->SetIntPropValues(g_cchTest/2, g_cchTest, ktptBold,
				ktpvEnum, kttvForceOn);
			qtsb->GetString(&m_qtssTwoRuns);

			m_qstfWrapper.CreateInstance(CLSID_TsStreamWrapper);
		}

		/*--------------------------------------------------------------------------------------
			Delete the objects created in Setup().
		--------------------------------------------------------------------------------------*/
		virtual void Teardown()
		{
			m_qtssEmpty.Clear();
			m_qtssOneRun.Clear();
			m_qtssTwoRuns.Clear();
			m_qtsf.Clear();
			m_qstfWrapper.Clear();
		}

		/*--------------------------------------------------------------------------------------
			Create a WritingSystem factory, and populate it with writing systems for "en" and
			"xstk".
		--------------------------------------------------------------------------------------*/
		virtual void SuiteSetup()
		{
			try
			{
				m_qwsf.CreateInstance(CLSID_LgWritingSystemFactory);
				// We don't want to create ICU files during this test.
				m_qwsf->put_BypassInstall(TRUE);
				IWritingSystemPtr qws;
				SmartBstr sbstr;

				sbstr.Assign(L"en");
				m_qwsf->get_Engine(sbstr, &qws);
				qws->get_WritingSystem(&m_wsEng);

				sbstr.Assign(L"xstk");
				m_qwsf->get_Engine(sbstr, &qws);
				qws->get_WritingSystem(&m_wsStk);
			}
			catch (...)
			{
			}
		}

		/*--------------------------------------------------------------------------------------
			Destroy the WritingSystem factory.
		--------------------------------------------------------------------------------------*/
		virtual void SuiteTeardown()
		{
			m_qwsf->Shutdown();
			m_qwsf.Clear();
		}
	};
}

#endif /*TESTTSSTREAMWRAPPER_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkfwk-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
/*:End Ignore*/
