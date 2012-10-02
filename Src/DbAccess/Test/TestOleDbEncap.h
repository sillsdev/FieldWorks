/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestOleDbEncap.h
Responsibility:
Last reviewed:

	Unit tests for the OleDbEncap class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTOLEDBENCAP_H_INCLUDED
#define TESTOLEDBENCAP_H_INCLUDED

#pragma once

#include "testDbAccess.h"

namespace TestDbAccess
{
	class TestOleDbEncap : public unitpp::suite
	{
		class OleDbEncapDouble: public OleDbEncap
		{
		protected:
			class RemoteConnectionMonitorDouble: public RemoteConnectionMonitor
			{
			public:
				bool IsRemoteConnection()
				{
					return m_fRemote;
				}
			};

			RemoteConnectionMonitorDouble m_rmcmdbl;
		public:
			bool NewConnection_IsRemoteConnection(BSTR bstrServer)
			{
				m_rmcmdbl.NewConnection(bstrServer);
				bool fRemote = m_rmcmdbl.IsRemoteConnection();
				m_rmcmdbl.TerminatingConnection();
				return fRemote;
			}
		};

		IOleDbEncapPtr m_qode;

		void testNullArgs()
		{
			unitpp::assert_true("Non-null m_qode after setup", m_qode.Ptr() != 0);
			HRESULT hr;
#ifndef _DEBUG
			IgnoreHr(hr = m_qode->QueryInterface(IID_NULL, NULL));
			unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, hr);
#endif
			IgnoreHr(hr = m_qode->CreateCommand(NULL));
			unitpp::assert_eq("CreateCommand(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qode->IsTransactionOpen(NULL));
			unitpp::assert_eq("IsTransactionOpen(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qode->SetSavePoint(NULL));
			unitpp::assert_eq("SetSavePoint(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qode->SetSavePointOrBeginTrans(NULL));
			unitpp::assert_eq("SetSavePointOrBeginTrans(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qode->get_Server(NULL));
			unitpp::assert_eq("get_Server(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qode->get_Database(NULL));
			unitpp::assert_eq("get_Database(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qode->Init(NULL, NULL, NULL, koltNone, 0));
			unitpp::assert_eq("Init(NULL, NULL, NULL, koltNone, 0) HRESULT", E_FAIL, hr);
		}

		// LT-9098: .\\SILFW should be equal to <hostname>\\SILFW and not considered a remote connection
		void testRemoteConnectionMonitor_NewLocalConnection()
		{
			OleDbEncapDouble ode;
			unitpp::assert_true(".\\SILFW should not be a remote connection",
				!ode.NewConnection_IsRemoteConnection(L".\\SILFW"));
		}

		void testRemoteConnectionMonitor_NewRemoteConnection()
		{
			OleDbEncapDouble ode;
			unitpp::assert_true("SomeRemoteHost\\SILFW should be a remote connection",
				ode.NewConnection_IsRemoteConnection(L"SomeRemoteHost\\SILFW"));
		}

		// REVIEW: What about something like localhost\\SILFW? This is also a local connection,
		// but currently we don't detect that.

	public:
		TestOleDbEncap();

		virtual void Setup()
		{
			OleDbEncap::CreateCom(NULL, IID_IOleDbEncap, (void **)&m_qode);
		}
		virtual void Teardown()
		{
			m_qode.Clear();
		}
	};
}

#endif /*TESTOLEDBENCAP_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkdba-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
