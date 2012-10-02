/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpDocDlg.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Manages the document dialog.
	THIS CLASS IS CURRENTLY NOT BEING USED.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef DOC_DLG_INCLUDED
#define DOC_DLG_INCLUDED 1

class WpDocDlg;
typedef GenSmartPtr<WpDocDlg> WpDocDlgPtr;
class WpDa;
typedef GenSmartPtr<WpDa> WpDaPtr;

class WpApp;

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Document dialog.
	Currently the only thing this manages is the document direction.
----------------------------------------------------------------------------------------------*/
class WpDocDlg : public AfDialog
{
	typedef AfDialog SuperClass;

public:
	WpDocDlg();
	~WpDocDlg();

	void SetDataAccess(WpDa * pda)
	{
		m_qda = pda;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp); // init controls
	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);

protected:
	//	Complex script options:
	bool m_fRtl;
	WpDaPtr m_qda;
};


#endif // !DOC_DLG_INCLUDED