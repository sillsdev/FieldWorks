/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfPrjNotFndDlg.h
Responsibility: John Landon
Last reviewed: Not yet.

Description:
	Header file for the AfPrjNotFnd Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFPRJNOTFNDDLG_H_INCLUDED
#define AFPRJNOTFNDDLG_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Project Not Found Dialog.

	Hungarian: pnf.
----------------------------------------------------------------------------------------------*/

class AfPrjNotFndDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	AfPrjNotFndDlg();

	~AfPrjNotFndDlg()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

	void SetProject(const achar * pszProject)
	{
		m_strProj = pszProject;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	StrApp m_strProj;

	// Handle to a font (Height 16 lu, Bold) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;
};

typedef GenSmartPtr<AfPrjNotFndDlg> AfPrjNotFndDlgPtr;

#endif  // !AFPRJNOTFNDDLG_H
