/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestTsTextProps.h
Responsibility:
Last reviewed:

	Unit tests for the TsTextProps class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTTSTEXTPROPS_H_INCLUDED
#define TESTTSTEXTPROPS_H_INCLUDED

#pragma once

#include "testFwKernel.h"

namespace TestFwKernel
{
	class TestTsTextProps : public unitpp::suite
	{
		TsTextProps * m_pzttp1;
		TsTextProps * m_pzttp2;
		ITsTextProps * m_pttp1;

		void testFirst()
		{
			TsTextProps * pzttp = dynamic_cast<TsTextProps *>(m_pttp1);
			unitpp::assert_eq("m_pzttp1", (long)pzttp,(long)m_pzttp1);
		}
		void testEmpty()
		{
			if (m_pzttp1)
			{
				int cref = m_pzttp1->Release();
				unitpp::assert_eq("m_pzttp1 release", 1, cref);
				m_pzttp1 = 0;
			}
			if (m_pzttp2)
			{
				int cref = m_pzttp2->Release();
				unitpp::assert_eq("m_pzttp2 release", 1, cref);
				m_pzttp2 = 0;
			}
			if (m_pttp1)
			{
				int cref = m_pttp1->Release();
				unitpp::assert_eq("m_pttp1 release", 0, cref);
				m_pttp1 = 0;
			}
		}
	public:
		TestTsTextProps();

		virtual void Setup()
		{
			m_pzttp1 = 0;
			m_pzttp2 = 0;
			m_pttp1 = 0;

			TsIntProp g_rgtip[3];
			TsStrProp g_rgtsp[1];
			g_rgtip[0].m_tpt = ktptWs;
			g_rgtip[0].m_nVar = 0;
			g_rgtip[0].m_nVal = kwsENG;
			g_rgtip[1].m_tpt = ktptBold;
			g_rgtip[1].m_nVar = ktpvEnum;
			g_rgtip[1].m_nVal = kttvForceOn;
			g_rgtip[2].m_tpt = ktptBackColor;
			g_rgtip[2].m_nVar = ktpvEnum;
			g_rgtip[2].m_nVal = kclrYellow;
			TsTextProps::Create(g_rgtip, 3, g_rgtsp, 0, &m_pzttp1);
			TsTextProps::Create(g_rgtip, 3, g_rgtsp, 0, &m_pttp1);
		}
		virtual void Teardown()
		{
			if (m_pzttp1)
			{
				m_pzttp1->Release();
				m_pzttp1 = 0;
			}
			if (m_pzttp2)
			{
				m_pzttp2->Release();
				m_pzttp2 = 0;
			}
			if (m_pttp1)
			{
				m_pttp1->Release();
				m_pttp1 = 0;
			}
		}
	};
}

#endif /*TESTTSTEXTPROPS_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkfwk-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
