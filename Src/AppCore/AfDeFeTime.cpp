/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeTime.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	Implements the AfDeFeTime class for displaying date and time.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeTime::AfDeFeTime()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeTime::~AfDeFeTime()
{
}


/*----------------------------------------------------------------------------------------------
	Initialize the font after superclass initialization is done.
----------------------------------------------------------------------------------------------*/
void AfDeFeTime::Init()
{
	Assert(m_hvoObj); // Initialize should have been called first.
	CreateFont();
}


/*----------------------------------------------------------------------------------------------
	Get the date from the cache and draw it to the given clip rectangle according to the
	current user locale.
	@param hdc The device context of the AfDeSplitChild.
	@param rcClip The rectangle available for drawing (AfDeSplitChild client coordinates)
----------------------------------------------------------------------------------------------*/
void AfDeFeTime::Draw(HDC hdc, const Rect & rcpClip)
{
	Assert(hdc);

	// Get the time information from the cache..
	StrAppBuf strb;
	SilTime tim;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	int64 nTime;
	CheckHr(qcvd->get_TimeProp(m_hvoObj, m_flid, &nTime));
	tim = nTime;

	// Leave the field blank if a date doesn't exist.
	if (nTime)
	{
		// Convert the date to a system date.
		SYSTEMTIME stim;
		stim.wYear = (unsigned short) tim.Year();
		stim.wMonth = (unsigned short) tim.Month();
		stim.wDayOfWeek = (unsigned short) tim.WeekDay();
		stim.wDay = (unsigned short) tim.Date();
		stim.wHour = (unsigned short) tim.Hour();
		stim.wMinute = (unsigned short) tim.Minute();
		stim.wSecond = (unsigned short) tim.Second();
		stim.wMilliseconds = (unsigned short)(tim.MilliSecond());

		// Then format it to a time based on the current user locale.
		achar rgchDate[50]; // Tuesday, August 15, 2000		mardi 15 août 2000
		achar rgchTime[50]; // 10:17:09 PM					22:20:08
		::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL, rgchDate, 50);
		::GetTimeFormat(LOCALE_USER_DEFAULT, NULL, &stim, NULL, rgchTime, 50);
		strb.Format(_T("%s %s"), rgchDate, rgchTime);
	}

	COLORREF clrFgOld = AfGfx::SetTextColor(hdc, m_chrp.clrFore);
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, m_hfont);

	AfGfx::FillSolidRect(hdc, rcpClip, ::GetSysColor(COLOR_3DFACE));
	COLORREF clrBgOld = AfGfx::SetBkColor(hdc, ::GetSysColor(COLOR_3DFACE));
	//HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
	::TextOut(hdc, rcpClip.left + 2, rcpClip.top + 1, strb.Chars(), strb.Length());

	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	AfGfx::SetBkColor(hdc, clrBgOld);
	AfGfx::SetTextColor(hdc, clrFgOld);
}


/*----------------------------------------------------------------------------------------------
	Set the height for the specified width, and return it.
	@param dxpWidth The width in pixels of the data part of the AfDeSplitChild.
	@return The height of the data in pixels.
----------------------------------------------------------------------------------------------*/
int AfDeFeTime::SetHeightAt(int dxpWidth)
{
	Assert(dxpWidth > 0);
	if (dxpWidth != m_dxpWidth)
	{
		// The height is set when the field is initialized and doesn't change here.
		m_dxpWidth = dxpWidth;
	}
	return m_dypHeight;
}


/*----------------------------------------------------------------------------------------------
	The field has changed, so make sure it is updated.
----------------------------------------------------------------------------------------------*/
void AfDeFeTime::UpdateField()
{
	// Nothing is needed here since the Draw method does everything.
}
