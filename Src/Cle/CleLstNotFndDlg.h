/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: CleLstNotFndDlg.h
Responsibility: John Landon
Last reviewed: Not yet.

Description:
	Header file for the CleLstNotFnd Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef CLELSTNOTFNDDLG_H_INCLUDED
#define CLELSTNOTFNDDLG_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the List Not Found Dialog.

	Hungarian: pnf.
----------------------------------------------------------------------------------------------*/

class CleLstNotFndDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	CleLstNotFndDlg();

	~CleLstNotFndDlg()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

	void SetList(const achar * pszList)
	{
		m_strList = pszList;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	StrApp m_strList;

	// Handle to a font (Height 16 lu, Bold) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;
};

typedef GenSmartPtr<CleLstNotFndDlg> CleLstNotFndDlgPtr;

#endif  // !CLELSTNOTFNDDLG_H
