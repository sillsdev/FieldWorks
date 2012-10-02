/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtWrtSysDlg.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Manages a dialog to allow selection of a old writing system for formatting text.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef FMTWRTSYSDLG_H_INCLUDED
#define FMTWRTSYSDLG_H_INCLUDED 1

class FmtWrtSysDlg;
typedef GenSmartPtr<FmtWrtSysDlg> FmtWrtSysDlgPtr;

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Format-Writing Systems dialog.
----------------------------------------------------------------------------------------------*/
class FmtWrtSysDlg : public AfDialog
{
	typedef AfDialog SuperClass;

public:
	FmtWrtSysDlg();
	~FmtWrtSysDlg();

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


// Local Variables:
// mode:C++
// End: (These 3 lines are useful to Steve McConnel.)

#endif // !FMTWRTSYSDLG_H_INCLUDED
