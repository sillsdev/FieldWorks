/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeGenDate.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	This is a data entry field editor for atomic reference fields.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_GENDATE_INCLUDED
#define AFDEFE_GENDATE_INCLUDED 1

/*----------------------------------------------------------------------------------------------
DatePickDlg Dialog

Before running dialog, either:
	1) Set m_systime (not valid for BC dates), or
	2) Call DecodeGDate() with an SIL date.

After running dialog, answer is in both:
	1) m_systime (not valid for BC dates)
	2) Call EncodeGDate() to get answer as an SIL date.
----------------------------------------------------------------------------------------------*/

#define DPK_ALLOW_PRECISION 1
#define DPK_ALLOW_BC        2

/*----------------------------------------------------------------------------------------------
	Defines a dialog for setting dates.
	Hungarian: dpd.
----------------------------------------------------------------------------------------------*/
class DatePickDlg : public AfDialogView
{
public:
	DatePickDlg (int nMode = 0);
	virtual ~DatePickDlg() {}
	int  EncodeGDate();
	void DecodeGDate (int gdate);
	SYSTEMTIME m_systime; // this also is the answer.
	enum { m_kDateBefore=0, m_kDateOn=1, m_kDateAbout=2, m_kDateAfter=3, m_kDateBlank=-1 };

	// For changing the help page to something specific:
	void SetHelpUrl(achar * psz)
	{
		m_pszHelpUrl = psz;
	}

private:
	HWND m_hCtl;
	int m_nRecurse;
	int m_nPrecision;
	int m_nYear;  // 0 == blank
	int m_nMonth; // 1-based, 0 == blank
	int m_nDay;   // 1-based, 0 == blank
	int m_nDaysInMonth;
	int m_nMinCalYyyyMm;
	int m_nMode;

	enum { m_kMinYear = -6000, m_kMaxYear = 9999 };
	enum { DPK_INIT=1, DPK_UPDATE, DPK_SELCHANGE, DPK_CHANGE, DPK_KILLFOCUS, DPK_DELTAPOS }; // user-defined events
	enum { m_kYearMaxLen = 100 };	// room for things like "January, 2000" in any language

	bool OnInitDlg (HWND hwndCtrl, LPARAM lp);
	void SetSizesPositions();

	bool OnNotifyChild (int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnCommand (int ctrl_id, int event, HWND hctl);
	bool OnKeyDown (UINT nChar, UINT nRepCnt, UINT nFlags);

	bool OnEventPrecision (int event);
	bool OnEventMonth (int event);
	bool OnEventDay (int event);
	bool OnEventYear (int event, UINT nChar);
	bool OnEventYearSpin (int event, NMHDR* pnmhdr);
	bool OnEventCalendar (int event);

	void UpdateCalendar();
	void UpdateControls();
	void UpdateSystime();
	void SetCalendarColors();
	void GetToday();
	void CalcDaysInMonth();
	int  ParseYear(achar y[m_kYearMaxLen]);
	void FormatYear(achar y[m_kYearMaxLen], int year, int month);
};


/*----------------------------------------------------------------------------------------------
	This node is used to provide tree structures in data entry editors.
	Hungarian: degd.
----------------------------------------------------------------------------------------------*/

class AfDeFeGenDate : public AfDeFeEdBoxBut
{
public:
	typedef AfDeFeEdBoxBut SuperClass;

	AfDeFeGenDate();
	~AfDeFeGenDate();

	void InitContents(int gdat);
	virtual bool SaveEdit();
	virtual bool IsDirty();
	virtual void ProcessChooser();
	virtual void UpdateField();
	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true,
		TptEditable tpte = ktptIsEditable);
	virtual bool IsOkToClose(bool fWarn = true);
	virtual FldReq HasRequiredData();

protected:
	int m_gdat; // The current value of the generic date.
};


#endif // AFDEFE_GENDATE_INCLUDED
