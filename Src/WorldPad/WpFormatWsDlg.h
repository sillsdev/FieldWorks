/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpFormatWsDlg.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Manages a dialog to allow selecting of a old writing system and applying it to the current
	text.

	TODO: Delete this file, as it has been made obsolete by moving the functionality to
	FmtWrtSysDlg.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef FORMATWS_DLG_INCLUDED
#define FORMATWS_DLG_INCLUDED 1

class WpFormatWsDlg;
typedef GenSmartPtr<WpFormatWsDlg> WpFormatWsDlgPtr;

class WpDa;

//:End Ignore

#if 0  // replaced by FmtWrtSysDlg in AppCore

/*----------------------------------------------------------------------------------------------
	Format-Writing Systems dialog.
----------------------------------------------------------------------------------------------*/
class WpFormatWsDlg : public AfDialog
{
	typedef AfDialog SuperClass;

public:
	WpFormatWsDlg();
	~WpFormatWsDlg();

	// Return the selected writing system to apply to the text selection.
	void SetInitEnc(int ws)
	{
		m_wsInit = ws;
	}

	int SelectedWritingSystem()
	{
		return m_wsSel;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp); // init controls
	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
//	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);

	void InitEncList();

protected:

	//	member variables
	int m_wsInit;	// initially selected
	int m_iwsInit;
	int m_wsSel;	// to apply
	Vector<int> m_vws;
	Vector<StrApp> m_vstr;
};

#endif // 0

#endif // !FORMATWS_DLG_INCLUDED