/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfUtil.h
Responsibility: Steve McConnel
Last reviewed:

Description:
	This file contains Utility methods (gathered in the AfUtil namespace).  Some of these were
	static methods of AfApp and other classes.
	It also contains class declarations for the following classes and structs:
		class WaitCursor
		struct AfExportStyleSheet
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AfUtil_H_INCLUDED
#define AfUtil_H_INCLUDED 1

namespace AfUtil
{
	bool GetStrMsrValue(StrAppBuf * pstrb, MsrSysType msrSys, int * nValue);
	bool MakeMsrStr(int nValue, MsrSysType msrSys, StrAppBuf * pstrb);
	bool GetResourceTss(int rid, int wsUser, ITsString ** pptss);
	bool GetResourceStr(ResourceStringType rst, int rid, StrApp & str);
	bool ShowHelpFile(const achar * pszHelp, const achar * pszPage);
	int Execute(HWND hwnd, LPCTSTR pszOperation, LPCTSTR pszFile, LPCTSTR pszParameters,
		LPCTSTR pszDirectory, int nShowCmd);
}

/*----------------------------------------------------------------------------------------------
	Instantiating this class sets the mouse to the hourglass wait cursor. The wait cursor stays
	in effect until the WaitCursor object goes out of scope, or until RestoreCursor() is called.

	@h3{Hungarian: wc)
----------------------------------------------------------------------------------------------*/
class WaitCursor
{
public:
	// Constructor.
	WaitCursor()
	{
		HCURSOR hcurWait = ::LoadCursor(NULL, IDC_WAIT);
		m_hcurOld = (hcurWait != NULL) ? ::SetCursor(hcurWait) : NULL;
	}
	// Constructor that selects a cursor.
	WaitCursor(LPCTSTR lpCursorName)
	{
		HCURSOR hcurWait = ::LoadCursor(NULL, lpCursorName);
		m_hcurOld = (hcurWait != NULL) ? ::SetCursor(hcurWait) : NULL;
	}

	// Destructor.
	~WaitCursor()
	{
		RestoreCursor();
	}
	/*------------------------------------------------------------------------------------------
		Restore the former cursor.
	------------------------------------------------------------------------------------------*/
	void RestoreCursor()
	{
		// This used to only set cursor if m_hcurOld was set. However, this allowed
		// a wait cursor to remain. For example, in List Chooser, Keyword combo box.
		// If you typed a keyword and pressed Enter, the wait cursor remained until the
		// mouse was moved.
		::SetCursor(m_hcurOld);
		m_hcurOld = NULL;
	}

protected:
	HCURSOR m_hcurOld;		// Handle to the former cursor.
};



/*----------------------------------------------------------------------------------------------
	Store the relevant information for each export XSL Transform file.

	@h3{Hungarian: ess)
----------------------------------------------------------------------------------------------*/
struct AfExportStyleSheet
{
	StrApp m_strFile;
	StrApp m_strTitle;
	StrApp m_strOutputExt;
	StrApp m_strDescription;
	Vector<StrApp> m_vstrChain;
	StrApp m_strViews;		// Not used by WorldPad.
};


/*------------------------------------------------------------------------------------------
	Writing System data.
	Hungarian: wsd
------------------------------------------------------------------------------------------*/
struct WrtSysData
{
	int m_ws;						// subsumes the old m_hvo field.
	StrUni m_stuIcuLocale;			// replaces the Alphabetic representation of the old m_ws
	StrUni m_stuName;
	int m_lcid;
	StrUni m_stuNormalFont;
	StrUni m_stuHeadingFont;
	StrUni m_stuBodyFont;
	StrUni m_stuEncodingConverter;	// From LegacyMapping field of WritingSystem.
	StrUni m_stuEncodingConvDesc;	// From somewhere or other???

	/*--------------------------------------------------------------------------------------
		Compare two WrtSysData objects for equality.

		@param wsd Reference to the other WrtSysData object.
	--------------------------------------------------------------------------------------*/
	bool operator == (const WrtSysData & wsd)
	{
		return m_ws == wsd.m_ws &&
			m_lcid == wsd.m_lcid &&
			m_stuName == wsd.m_stuName &&
			m_stuNormalFont == wsd.m_stuNormalFont &&
			m_stuHeadingFont == wsd.m_stuHeadingFont &&
			m_stuEncodingConverter == wsd.m_stuEncodingConverter &&
			m_stuEncodingConvDesc == wsd.m_stuEncodingConvDesc;
	}
};


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\MkCustomNb.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*AfUtil_H_INCLUDED*/
