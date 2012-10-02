/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Disconnect.h
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	Header file for the interface IDisconnectDb, which enables users to disconnect others from
	their database.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef DISCONNECT_H_INCLUDED
#define DISCONNECT_H_INCLUDED
/*:End Ignore*/

#include "backup.h" // so we can get BackupDialogBase

/*----------------------------------------------------------------------------------------------
	Class to handle forced disconnection of all users from database.
	@h3{Hungarian: dscdb}
----------------------------------------------------------------------------------------------*/
class DisconnectDb : public IDisconnectDb, public BackupDialogBase
{
	typedef BackupDialogBase SuperClass;

public:
	enum // Return values for Init()
	{
		kError,
		kNobodyConnected,
		kOnlyMeConnected,
		kOnlyOutsidersConnected,
		kMeAndOutsidersConnected,
	};
	enum
	{
		knTimeOut = 180
	};
	DisconnectDb();
	~DisconnectDb();

	STDMETHOD_(ULONG, AddRef)(void);
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	STDMETHOD(Init)(BSTR bstrDatabase, BSTR bstrServer, BSTR bstrReason,
		BSTR bstrExternalReason, ComBool fConfirmCancel, BSTR bstrCancelQuestion,
		int hwndParent);
	STDMETHOD(CheckConnections)(int * pnResponse);
	STDMETHOD(DisconnectAll)(ComBool * pfResult);
	STDMETHOD(ForceDisconnectAll)();
	virtual SmartBstr GetHelpTopic();

protected:
	long m_cref;

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	virtual bool OnCancel();
	void OnTimer(UINT nIDEvent);
	void FillListView();
	void NotifyAndCountDown();
	void NotificationOption(bool fFlag);

	struct ConnectionData
	{
		StrUni m_stuHostName;
		StrUni m_stuNTDomainUser;
		int m_nNumConnections;
		IRemoteDbWarnPtr m_qzrdbw;
		bool m_fWarnAttempted;
		bool m_fWarned;
		ConnectionData() : m_nNumConnections(0), m_fWarnAttempted(false),
			m_fWarned(false) { }
	};
	Vector<ConnectionData> m_vcondat; // Details of connected users.
	bool m_fOfferNotify; // True if user will have option of notifying other computers.
	int m_nNumOwnConnections; // Number of connections this user has.
	StrUni m_stuDatabase; // Name of database in question.
	HFONT m_hfntLargeNumberFont; // Font for displaying countdown time.
	HWND m_hwndParent; // Handle of owning window.
	IOleDbEncapPtr m_qode; // Access to the master database on specified server.
	IOleDbCommandPtr m_qodc; // Command interface to master database on specified server.
	StrApp m_strReason; // The reason to give to user to explain disconnection.
	StrUni m_stuHostName; // Name of local machine.
	StrUni m_stuWarning; // Warning message to send to remote users.
	int m_nTimeRemaining; // Number of seconds left until forced disconnection.
	int64 m_nEndTime; // Time at which forced disconnection will occur.
	HIMAGELIST m_himlNetwork; // Useful icon images.
	bool m_fConfirmCancel; // True if user has to confirm abort.
	StrApp m_strCancelQuestion; // Question to ask if user must confirm abort.
};

#endif //:> DISCONNECT_H_INCLUDED
