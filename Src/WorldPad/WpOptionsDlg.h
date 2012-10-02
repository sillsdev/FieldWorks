/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpOptionsDlg.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Manages the options dialog.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef OPTIONS_DLG_INCLUDED
#define OPTIONS_DLG_INCLUDED 1

class WpOptionsDlg;
typedef GenSmartPtr<WpOptionsDlg> WpOptionsDlgPtr;

class WpApp;

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Options dialog.
	ENHANCE: make this a tabbed dialog when we add more stuff to it.
----------------------------------------------------------------------------------------------*/
class WpOptionsDlg : public AfDialog
{
	typedef AfDialog SuperClass;

public:
	WpOptionsDlg();
	~WpOptionsDlg();

	void ModifyAppFlags(WpApp * pwpapp);

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp); // init controls
	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);

protected:
	//	Complex script options:
	bool m_fLogicalArrow;			// true if arrow keys move logically
//	bool m_fLogicalShiftArrow;
//	bool m_fLogicalHomeEnd;
	bool m_fGraphiteLog;		// output log of Graphite transduction for debugging
};


#endif // !OPTIONS_DLG_INCLUDED