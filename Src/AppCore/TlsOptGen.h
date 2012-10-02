/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsOptGen.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Header file for the General tab in the Tools Options Dialog class.
	NOTE: this general tab uses the first DataEntry UserViewSpec to initialize the list of
	fields. If a particular application does not provide a DataEntry view, then this version
	of the tab should not be used.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TLSOPTGEN_H_INCLUDED
#define TLSOPTGEN_H_INCLUDED

class TlsOptDlg;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the General tab for paragraphs.
	@h3{Hungarian: brdp}
----------------------------------------------------------------------------------------------*/
class TlsOptDlgGen : public AfDialogView
{
typedef AfDialogView SuperClass;
public:
	TlsOptDlgGen(TlsOptDlg * ptod);
	~TlsOptDlgGen();

	void OnReleasePtr();
	void SetDialogValues(UserViewSpecVec & vuvs, Set<int> * psiwndClientDel);
	bool SetActive();

protected:
	// Methods
	virtual void PostAttach(void);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	bool CmdMod(Cmd * pcmd);
	bool UpdateFlds();
	bool ModReq();
	bool NowRequired(ITsString * ptssFldName);
	bool CheckAlwaysVisible(ITsString * ptssFldName);
	bool SaveReqChange(FldReq nreq);

	ITsStringPtr m_rgqFldReq[3];	//Field Required
	TlsOptDlg * m_ptod;
	HWND m_hwndGenFlds;
	int m_iItemGenFlds;
	int m_comboWidth;
	Set<int> * m_psiwndClientDel;
	int m_wsUser;		// user interface writing system id.

	CMD_MAP_DEC(TlsOptDlgGen);
};

typedef GenSmartPtr<TlsOptDlgGen> TlsOptDlgGenPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the Views tab for paragraphs.
	@h3{Hungarian: brdp}
----------------------------------------------------------------------------------------------*/
class ModReqDlg : public AfDialogView
{

typedef AfDialogView SuperClass;
public:
	ModReqDlg();
	~ModReqDlg()
	{
	}

	void SetDialogValues(FldReq req);
	void GetDialogValues(FldReq * preq);

protected:
	// Member variables.
	HFONT m_hfontNumber;
	bool m_fDisableEnChange;
	FldReq m_nReq;
	HWND m_hwndReq;

	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	void SetReq(FldReq nreq);
};

typedef GenSmartPtr<ModReqDlg> ModReqDlgPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Data Requirements Notice that is opened
	when someone changes the Required property to Always visible..
	@h3{Hungarian: mfsnd}
----------------------------------------------------------------------------------------------*/
class ModFldSetNtcDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;

public:
	ModFldSetNtcDlg();

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
};

typedef GenSmartPtr<ModFldSetNtcDlg> ModFldSetNtcDlgPtr;


#endif  // !TLSOPTGEN_H_INCLUDED
