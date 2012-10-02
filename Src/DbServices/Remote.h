/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Remote.h
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	Header file for the IRemoteDbWarn Interface.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef __REMOTE_H__
#define __REMOTE_H__

DEFINE_COM_PTR(IRemoteDbWarn);
DEFINE_COM_PTR(IDbWarnSetup);

#include "..\AppCore\AfCore.h"

/*----------------------------------------------------------------------------------------------
	Used by the ${RemoteDbWarn} to warn remotely connected users to log off.
	@h3{Hungarian: ctdd}
----------------------------------------------------------------------------------------------*/
class CountdownDlg : public AfDialog
{
	typedef AfDialog SuperClass;

public:
	CountdownDlg();
	~CountdownDlg();
	void Init(BSTR bstrMessage, int nTimeLeft);
	void Cancel();
	virtual bool OnCancel();

protected:
	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	void OnTimer(UINT nIDEvent);
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	StrApp m_strMessage; // Context-sensitive message to display.
	int m_nTimeRemaining; // Number of seconds left until forced disconnection
	int64 m_nEndTime; // Time at which forced disconnection will occur
	HFONT m_hfntLargeNumberFont; // Font for displaying countdown time
	bool m_fCanceled;
};
typedef GenSmartPtr<CountdownDlg> CountdownDlgPtr;

/*----------------------------------------------------------------------------------------------
	Used to warn remotely connected users to log off.
	@h3{Hungarian: zrdbw}
----------------------------------------------------------------------------------------------*/
class RemoteDbWarn : public IRemoteDbWarn, public IDbWarnSetup
{
protected:
	long m_cref;
	CountdownDlgPtr m_qctdd;
	bool m_fPermissionConfigured;
	static GUID s_guidAppId;

public:
	RemoteDbWarn();
	~RemoteDbWarn();

	STDMETHOD_(ULONG, AddRef)(void);
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	STDMETHOD(WarnSimple)(BSTR bstrMessage, int nFlags, int * pnResponse);
	STDMETHOD(WarnWithTimeout)(BSTR bstrMessage, int nTimeLeft);
	STDMETHOD(Cancel)();

	STDMETHOD(PermitRemoteWarnings)();
	STDMETHOD(RefuseRemoteWarnings)();
};

#endif   // __REMOTE_H__
