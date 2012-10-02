/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilTime.cpp
Responsibility: Steve McConnel
Last reviewed: 8/17/99

	This file provides the implementation of SilTime functions.
	It also provides time/date related functions that don't belong anywhere else, but which are
	generally useful.

	Throughout this code, T0 represents the zero time: the beginning of 1 January 1601.

	Hungarian:

		ymon: The month number within a year from 1 to 12 (as in the YearMonth enum).
		zmon: The number of months since the beginning of some base year. 0 means January of
			the base year.

		mday: The day number within a month from 1 to 31.
		yday: The day number within a year from 0 to 365.
		zday: The number of days since the beginning of some base month. 0 means the first
			day of the base month.
		day: The number of days since T0.

		ymin: The minute within the year.

		msp: The number of milliseconds relative to the beginning of a 400 year period.

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


// VARIANT equivalent of January 1, 1601
const double kvtimJanuary1st1601 = -109205.0;

// VARIANT equivalent of .5 seconds
const double kvtimHalfSecond = 0.5 / 86400.0;

// The day numbers for the months of a non-leap year, followed by those of a leap year.
// For convenience, list is terminated with the number of days in the year.
static const int g_rgyday[2][kmonPerYear + 1] =
{
	{ 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 },
	{ 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 },
};


/*----------------------------------------------------------------------------------------------
	Return the total number of days in the given month, or zero if the month is invalid.

	@param nMonth Month of the year (1-12).
	@param nYear Year in the A.D. (or C.E.) calandar.
----------------------------------------------------------------------------------------------*/
int GetDaysInMonth(int nMonth, int nYear)
{
	if ((nMonth < 1) || (nMonth > 12))
		return 0;
	const static int m[13] = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
	if (nYear == 1752 && nMonth == 9)
		return 19;			// The month the calendar changed.
	else if (nMonth == 2)
		return !(nYear % 4) && (nYear <= 1752 || (nYear % 100) || !(nYear % 400)) ? 29 : 28;
	else
		return m[nMonth - 1];
}


// Date qualifiers:
const int g_rgridDateQual1[] = { kstidDateQualBefore,
								 kstidDateQualOn,
								 kstidDateQualAbout,
								 kstidDateQualAfter,
								 kstidDateQualAbt};
const int g_rgridDateQual2[] = { kstidDateQualBC, kstidDateQualAD };
const int g_rgridDateBlank[] = { kstidDateBlank, kstidDateBlankM, kstidDateBlankD };

//:> REVIEW: The number of characters allowed for these strings was guessed. The strings come
//:> from resources which may be translated into other languages and come out longer.
const int knPrecLen = 25;
const int knAdBcLen = 10;
const int knBlankLen = 15;

// Storage location for date qualifier wide strings:
static wchar g_rgchwPrecFull[5][knPrecLen];
static wchar g_rgchwBC_ADFull[2][knAdBcLen];
static wchar g_rgchwBlankFull[3][knBlankLen];
// Storage location for date qualifier 8-bit strings:
static char g_rgchPrecFull[5][knPrecLen];
static char g_rgchBC_ADFull[2][knAdBcLen];
static char g_rgchBlankFull[3][knBlankLen];

// Array of pointers to strings (As used before code was modified to read resource strings):
extern const wchar * g_rgchwDatePrec[] = { g_rgchwPrecFull[0], g_rgchwPrecFull[1],
										   g_rgchwPrecFull[2], g_rgchwPrecFull[3],
										   g_rgchwPrecFull[4], NULL };
extern const wchar * g_rgchwDateBC_AD[] = { g_rgchwBC_ADFull[0], g_rgchwBC_ADFull[1], NULL };
extern const wchar * g_rgchwDateBlank[] = { g_rgchwBlankFull[0], g_rgchwBlankFull[1],
											g_rgchwBlankFull[2] };

extern const char * g_rgchsDatePrec[] = { g_rgchPrecFull[0], g_rgchPrecFull[1],
										  g_rgchPrecFull[2], g_rgchPrecFull[3],
										  g_rgchPrecFull[4], g_rgchPrecFull[5], NULL };
extern const char * g_rgchsDateBC_AD[] = { g_rgchBC_ADFull[0], g_rgchBC_ADFull[1], NULL };
extern const char * g_rgchsDateBlank[] = { g_rgchBlankFull[0], g_rgchBlankFull[1],
										   g_rgchBlankFull[2] };

// Global flag to tell if strings have been loaded in from resources:
extern bool g_fDoneDateQual = false;
// Routine to load date qualifier strings from resources. Do not call before AfApp has m_hinst.
void LoadDateQualifiers()
{
	if (g_fDoneDateQual)
		return;

	int i;
	for (i = 0; i < 5; i++)
	{
		StrUni stu(g_rgridDateQual1[i]);
		Assert(stu.Length() < knPrecLen);
		wcscpy_s(g_rgchwPrecFull[i], SizeOfArray(g_rgchwPrecFull[i]), stu.Chars());
		StrAnsi sta(g_rgridDateQual1[i]);
		Assert(sta.Length() < knPrecLen);
		strcpy_s(g_rgchPrecFull[i], sta.Chars());
	}
	for (i = 0; i < 2; i++)
	{
		StrUni stu(g_rgridDateQual2[i]);
		Assert(stu.Length() < knAdBcLen);
		wcscpy_s(g_rgchwBC_ADFull[i], SizeOfArray(g_rgchwBC_ADFull[i]), stu.Chars());
		StrAnsi sta(g_rgridDateQual2[i]);
		Assert(stu.Length() < knAdBcLen);
		strcpy_s(g_rgchBC_ADFull[i], sta.Chars());
	}
	for (i = 0; i < 3; i++)
	{
		StrUni stu(g_rgridDateBlank[i]);
		Assert(stu.Length() < knBlankLen);
		wcscpy_s(g_rgchwBlankFull[i], SizeOfArray(g_rgchwBlankFull[i]), stu.Chars());
		StrAnsi sta(g_rgridDateBlank[i]);
		Assert(stu.Length() < knBlankLen);
		strcpy_s(g_rgchBlankFull[i], sta.Chars());
	}
	g_fDoneDateQual = true;
}



/*----------------------------------------------------------------------------------------------
	Given a year relative to the start of a 400 year period, return the day number.
----------------------------------------------------------------------------------------------*/
inline int DayFromYearInPeriod(int dyear)
{
	Assert((uint)dyear < (uint)kyearPerPeriod);
	return 365 * dyear + dyear / 4 - dyear / 100 + dyear / 400;
}


/*----------------------------------------------------------------------------------------------
	Given a day within a 400 year period, this calculates the year within that 400 year
	period and the day within that year.

	To do this, we add on a day for every non-leap year so in the end we can divide by 366 to
	get the year.
----------------------------------------------------------------------------------------------*/
inline int YearFromDayInPeriod(int day, int * pyday)
{
	Assert((uint)day < (uint)kdayPerPeriod);
	AssertPtr(pyday);

	int dayT;
	int dyear;

	// Add 1 for each year divisible by 100 but not divisible by 400. Since day < kdayPerPeriod
	// we don't have to worry about the year being divisible by 400.
	dayT = day + (4 * day + 3) / kdayPerPeriod;

	// Add 1 for all years not divisible by 4. Note that 1461 is the number of days
	// in 4 years.
	dayT = dayT + (4 * dayT + 3) / 1461 - dayT / 1461;

	// Now we can just divide by 366 to get the year and mod by 366 to get the day within
	// the year.
	dyear = dayT / 366;
	*pyday = dayT - 366 * dyear;

	Assert(0 <= dyear && dyear < 400);
	Assert(0 <= *pyday && (*pyday < 365 ||
		*pyday == 365 && SilTime::IsLeapYear(dyear + kyearBase)));

	return dyear;
}


/*----------------------------------------------------------------------------------------------
	This class handles mapping between UTC and local time.
----------------------------------------------------------------------------------------------*/
class TimeMapper
{
public:
	TimeMapper(void)
	{
		Init();
	}

	void Init(void);

	// Map the time to local time (from Utc).
	int64 ToLcl(int64 msecUtc)
	{
		if (m_dminTz1 == m_dminTz2)
			return msecUtc + Mul(m_dminTz1, kmsecPerMin);
		return MapMsec(msecUtc, false);
	}

	// Map the time to Utc (from local).
	int64 ToUtc(int64 msecLcl)
	{
		if (m_dminTz1 == m_dminTz2)
			return msecLcl - Mul(m_dminTz1, kmsecPerMin);
		return MapMsec(msecLcl, true);
	}

protected:
	// These are the biases between local time and UTC for the two time periods. The first is
	// for the time period consisting of the beginning and end of the year (STD time in US,
	// DST in Australia). The second is for the middle time period (DST in US, STD time in
	// Australia).
	int m_dminTz1;
	int m_dminTz2;

	// These indicate the minute at which the transitions happen for the different year types.
	int m_rgyminMin[kytLim];
	int m_rgyminLim[kytLim];

	int64 MapMsec(int64 msecUtc, bool fToUtc);
	bool CalcTransitions(int dminBias, SYSTEMTIME & st, const int * prgyday, int * prgymin);
};


static TimeMapper g_tmm;


/*----------------------------------------------------------------------------------------------
	Initialize with the time zone information. This can be called multiple times, although
	it's not protected for multi-threaded access.
	REVIEW ShonK: Should we protect this with a critical section?
----------------------------------------------------------------------------------------------*/
void TimeMapper::Init(void)
{
	TIME_ZONE_INFORMATION tzi;
	GetTimeZoneInformation(&tzi);

	ClearItems(m_rgyminMin, kytLim);
	ClearItems(m_rgyminLim, kytLim);
	m_dminTz1 = m_dminTz2 = -(tzi.Bias + tzi.StandardBias);

	// Note: Win98 gives bogus information for tzi.DaylightBias when there is no daylight
	// savings time. In this case, tzi.StandardDate.wMonth is 0.
	if (!tzi.StandardDate.wMonth || tzi.StandardBias == tzi.DaylightBias)
		return;

	if (tzi.StandardDate.wYear || tzi.DaylightDate.wYear)
	{
		// REVIEW ShonK: Can this ever happen? MSDN seems to indicate it can, but I don't
		// understand what the info means.
		Assert(!"Don't know how to handle this DST conversion.");
		return;
	}

	if (tzi.StandardDate.wMonth == tzi.DaylightDate.wMonth)
	{
		Assert(!"Bad months in the time zone info.");
		return;
	}

	int dminT = -(tzi.Bias + tzi.DaylightBias);
	if (tzi.StandardDate.wMonth > tzi.DaylightDate.wMonth)
	{
		// Jan 1 is STD time.
		if (!CalcTransitions(tzi.Bias, tzi.StandardDate, g_rgyday[0], m_rgyminLim) ||
			!CalcTransitions(tzi.Bias, tzi.StandardDate, g_rgyday[1],
				m_rgyminLim + kytSundayLeap) ||
			!CalcTransitions(tzi.Bias, tzi.DaylightDate, g_rgyday[0], m_rgyminMin) ||
			!CalcTransitions(tzi.Bias, tzi.DaylightDate, g_rgyday[1],
				m_rgyminMin + kytSundayLeap))
		{
			Assert(!"Calculating transitions failed.");
			return;
		}
		m_dminTz2 = dminT;
	}
	else
	{
		// Jan 1 is DST.
		if (!CalcTransitions(tzi.Bias, tzi.StandardDate, g_rgyday[0], m_rgyminMin) ||
			!CalcTransitions(tzi.Bias, tzi.StandardDate, g_rgyday[1],
				m_rgyminMin + kytSundayLeap) ||
			!CalcTransitions(tzi.Bias, tzi.DaylightDate, g_rgyday[0], m_rgyminLim) ||
			!CalcTransitions(tzi.Bias, tzi.DaylightDate, g_rgyday[1],
				m_rgyminLim + kytSundayLeap))
		{
			Assert(!"Calculating transitions failed.");
			return;
		}
		m_dminTz1 = dminT;
	}
}


/*----------------------------------------------------------------------------------------------
	Calculate the transition times for switching between STD time and DST.
----------------------------------------------------------------------------------------------*/
bool TimeMapper::CalcTransitions(int dminBias, SYSTEMTIME & st, const int * prgyday,
	int * prgymin)
{
	AssertArray(prgyday, kmonPerYear);
	AssertArray(prgymin, kdayPerWeek);

	int zmon = st.wMonth - 1;
	int wday = st.wDayOfWeek;
	int week = st.wDay;
	int min = st.wHour * kminPerHour + st.wMinute;

	if ((uint)zmon >= 12 || (uint)wday >= kdayPerWeek || week < 1 || week > 5 ||
		(uint)min >= kminPerDay)
	{
		Assert(!"Bad time zone info");
		return false;
	}

	int ydayLast = prgyday[zmon] + week * kdayPerWeek - 1;
	if (ydayLast >= prgyday[zmon + 1])
		ydayLast = prgyday[zmon + 1] - 1;

	// Find the first day <= ydayLast that is the correct day of the week.
	for (int wdayT = 0; wdayT < kdayPerWeek; wdayT++)
	{
		int wdayLast = (ydayLast + wdayT) % kdayPerWeek;
		int yday = ydayLast - (wdayLast - wday) - (wdayLast < wday) * kdayPerWeek;
		Assert(yday <= ydayLast);
		Assert((yday + wdayT) % kdayPerWeek == wday);
		prgymin[wdayT] = yday * kminPerDay + min + dminBias;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Core routine to map between local and Utc. This assumes that DST doesn't kick in
	near a year boundary so that when the switch happens, the DST and STD times are in the
	same year.
----------------------------------------------------------------------------------------------*/
int64 TimeMapper::MapMsec(int64 msecSrc, bool fToUtc)
{
	int64 msecT;
	int msecDay;
	int day;
	int dayMin;
	int dyear;
	int yday;
	int yt;
	int ymin;

	// First determines the year type and minute within the year.

	// Mod to 400 year period.
	msecT = ModPos(msecSrc, kmsecPerPeriod);

	// Find the day within the 400 year period.
	day = (int)(msecT / kmsecPerDay);
	Assert(0 <= day && day < kdayPerPeriod);

	// Get the time within the day.
	msecDay = (int)(msecT - day * (int64)kmsecPerDay);
	Assert(0 <= msecDay && msecDay < kmsecPerDay);

	// Find the year within the 400 year period (dyear) and the day within that year (yday).
	Assert(0 <= day && day < 146097);
	dyear = YearFromDayInPeriod(day, &yday);
	Assert(0 <= dyear && dyear < 400);
	Assert(0 <= yday && (yday < 365 || yday == 365 && SilTime::IsLeapYear(dyear + kyearBase)));

	// Calculate the day number (within the 400 year period) of the first day of
	// the year (dayMin).
	dayMin = day - yday;
	Assert(dayMin == DayFromYearInPeriod(dyear));

	// Get the year type. kdayMonday is used because T0 is a Monday.
	yt = ModPos(dayMin + kwdayMonday, kdayPerWeek);
	if (SilTime::IsLeapYear(dyear + kyearBase))
		yt += kdayPerWeek;

	// Calculate the minute within the year.
	ymin = yday * kminPerDay + msecDay / kmsecPerMin;

	if (!fToUtc)
	{
		// Mapping from utc to local.
		if (m_rgyminMin[yt] <= ymin && ymin < m_rgyminLim[yt])
			return msecSrc + Mul(m_dminTz2, kmsecPerMin);
		return msecSrc + Mul(m_dminTz1, kmsecPerMin);
	}

	// The overlap is always ambiguous and there's nothing we can do about it.
	ymin -= m_dminTz2;
	if (m_rgyminMin[yt] <= ymin && ymin < m_rgyminLim[yt])
		return msecSrc - Mul(m_dminTz2, kmsecPerMin);
	return msecSrc - Mul(m_dminTz1, kmsecPerMin);
}

/*----------------------------------------------------------------------------------------------
	Constructor for a literal time.
----------------------------------------------------------------------------------------------*/
SilTime::SilTime(int year, int month, int dayOfMonth, int hour, int min, int sec,
	int msec, bool fUtc)
{

	SilTimeInfo sti;
	sti.year = year;
	sti.ymon = month;
	sti.mday = dayOfMonth;
	sti.hour = hour;
	sti.min = min;
	sti.sec = sec;
	sti.msec = msec;
	SetTimeInfo(sti, fUtc);
}
/*----------------------------------------------------------------------------------------------
	Fill in the SilTimeInfo structure from this time.
----------------------------------------------------------------------------------------------*/
void SilTime::GetTimeInfo(SilTimeInfo * psti, bool fUtc) const
{
	AssertPtr(psti);

	int dyear;
	int day;
	int msecDay;
	int yday;
	int64 msec = m_msec;

	if (!fUtc)
		msec = g_tmm.ToLcl(msec);

	// Decompose m_msec into which 400 year period it is in (relative to T0) and
	// the millisecond within the 400 year period.
	psti->year = (int)FloorDiv(msec, kmsecPerPeriod) * kyearPerPeriod + kyearBase;
	msec = ModPos(msec, kmsecPerPeriod);
	Assert(0 <= 0 && msec < kmsecPerPeriod);

	// Find the day within the 400 year period.
	day = (int)(msec / kmsecPerDay);
	Assert(0 <= day && day < kdayPerPeriod);

	// Get the time within the day.
	msecDay = (int)(msec - day * (int64)kmsecPerDay);
	Assert(0 <= msecDay && msecDay < kmsecPerDay);

	// Find the year within the 400 year period (dyear) and the day within that year (yday).
	Assert(0 <= day && day < 146097);
	dyear = YearFromDayInPeriod(day, &yday);
	Assert(0 <= dyear && dyear < 400);
	Assert(0 <= yday && (yday < 365 || yday == 365 && SilTime::IsLeapYear(dyear + kyearBase)));

	// Add dyear into the year.
	psti->year += dyear;

	// Find the month and day in the month.
	const int * prgday = g_rgyday[IsLeapYear(psti->year)];
	for (psti->ymon = 1; yday >= prgday[psti->ymon]; psti->ymon++)
		;
	psti->mday = yday - prgday[psti->ymon - 1] + 1;

	// Calculate the week day.
	// kdayMonday is used because T0 is a Monday. Note that kdayPerPeriod is divisible by 7
	// so we don't have to adjust the day of the week.
	psti->wday = ModPos(day + kwdayMonday, kdayPerWeek);

	// Fill in the time.
	psti->hour = msecDay / kmsecPerHour;
	psti->min = (msecDay % kmsecPerHour) / kmsecPerMin;
	psti->sec = (msecDay % kmsecPerMin) / kmsecPerSec;
	psti->msec = msecDay % kmsecPerSec;
}


/*----------------------------------------------------------------------------------------------
	Set the value of this SilTime according to the information in psti.
----------------------------------------------------------------------------------------------*/
void SilTime::SetTimeInfo(const SilTimeInfo & sti, bool fUtc)
{
	int dyear = sti.year + FloorDiv(sti.ymon - 1, kmonPerYear) - kyearBase;
	int mon = ModPos(sti.ymon - 1, kmonPerYear);

	// Calculate the day number.
	int64 day = (int64)365 * dyear +
		FloorDiv(dyear, 4) - FloorDiv(dyear, 100) + FloorDiv(dyear, 400) +
		g_rgyday[IsLeapYear(dyear + kyearBase)][mon] + sti.mday - 1;

	m_msec = day * kmsecPerDay +
		sti.hour * (int64)kmsecPerHour + sti.min * (int64)kmsecPerMin +
		sti.sec * (int64)kmsecPerSec + sti.msec;

	if (!fUtc)
		m_msec = g_tmm.ToUtc(m_msec);
}


/*----------------------------------------------------------------------------------------------
	Return the timezone offset: local time minus utc.
----------------------------------------------------------------------------------------------*/
int SilTime::TimeZoneOffset(void) const
{
	return (int)((g_tmm.ToLcl(m_msec) - m_msec) / kmsecPerMin);
}


/*----------------------------------------------------------------------------------------------
	Return a variant time from this SilTime.
----------------------------------------------------------------------------------------------*/
double SilTime::VarTime(void) const
{
	double vtim;
	int64 msec;

	// Convert to local time.
	msec = g_tmm.ToLcl(m_msec);

	// Convert to an automation date.
	vtim = (double)msec / kmsecPerDay + kvtimJanuary1st1601;

	// vtim is the actual number of days since 0000h 12/30/1899.
	// Convert this to a true Automation-style date.
	if (vtim < 0.0)
	{
		vtim = 2.0 * floor(vtim) - vtim;
		double vtimT = vtim - floor(vtim);
		if (vtimT <= kvtimHalfSecond && 0.0 < vtimT)
			vtim = ceil(vtim) + 1.0;
	}

	return vtim;
}


/*----------------------------------------------------------------------------------------------
	Set this SilTime to the indicated variant time. Variant time is always in local time.
----------------------------------------------------------------------------------------------*/
void SilTime::SetToVarTime(double vtim)
{
	double dbl = vtim;

	// So that the arithmetic works even for negative dates, convert the
	// date to the _actual number of days_ since 0000h 12/30/1899.
	if (dbl < 0.0)
		dbl = 2.0 * ceil(dbl) - dbl;

	// Get the local time value.
	dbl = (dbl - kvtimJanuary1st1601) * kmsecPerDay;
	m_msec = g_tmm.ToUtc((int64)dbl);
}


/*----------------------------------------------------------------------------------------------
	Parse a string and compute the date.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	static bool FParseSilTime(const XChar * prgch, int cch, XChar chDateSep, XChar chSep,
		XChar chTimeSep, const XChar ** ppchLim, bool fUtc, SilTime * pstim)
{
	// REVIEW SteveMc: Should we do range checking and report errors?
	AssertArray(prgch, cch);
	AssertPtrN(ppchLim);
	AssertPtr(pstim);

	SilTimeInfo sti;
	ClearItems(&sti, 1);

	const XChar * pch = prgch;
	const XChar * pchLim = prgch + cch;

	// Get the year.
	if (pch < pchLim && *pch == '-')
		pch++;
	while (pch < pchLim && '0' <= *pch && *pch <= '9')
		sti.year = sti.year * 10 + (*pch++ - '0');
	if (cch > 0 && *prgch == chDateSep)
		sti.year = -sti.year;

	if (pch < pchLim && *pch == chDateSep)
	{
		pch++;
		while (pch < pchLim && '0' <= *pch && *pch <= '9')
			sti.ymon = sti.ymon * 10 + (*pch++ - '0');
		if (pch < pchLim && *pch == chDateSep)
		{
			pch++;
			while (pch < pchLim && '0' <= *pch && *pch <= '9')
				sti.mday = sti.mday * 10 + (*pch++ - '0');
		}
	}

	if (pch < pchLim && *pch == chSep)
	{
		pch++;
		while (pch < pchLim && '0' <= *pch && *pch <= '9')
			sti.hour = sti.hour * 10 + (*pch++ - '0');
		if (pch < pchLim && *pch == chTimeSep)
		{
			pch++;
			while (pch < pchLim && '0' <= *pch && *pch <= '9')
				sti.min = sti.min * 10 + (*pch++ - '0');
			if (pch < pchLim && *pch == chTimeSep)
			{
				pch++;
				while (pch < pchLim && '0' <= *pch && *pch <= '9')
					sti.sec = sti.sec * 10 + (*pch++ - '0');
				if (pch < pchLim && *pch == '.')
				{
					pch++;
					if ('0' <= *pch && *pch <= '9')
					{
						sti.msec += (*pch++ - '0') * 100;
						if ('0' <= *pch && *pch <= '9')
						{
							sti.msec += (*pch++ - '0') * 10;
							if ('0' <= *pch && *pch <= '9')
								sti.msec += (*pch++ - '0');
						}
					}
				}
			}
		}
	}

	if (ppchLim)
		*ppchLim = pch;

	pstim->SetTimeInfo(sti, true);
	return true;
}


bool SilTime::FParse(const wchar * prgch, int cch,
	wchar chDateSep, wchar chSep, wchar chTimeSep, const wchar ** ppchLim, bool fUtc)
{
	return FParseSilTime(prgch, cch, chDateSep, chSep, chTimeSep, ppchLim, fUtc, this);
}


bool SilTime::FParse(const schar * prgch, int cch,
	schar chDateSep, schar chSep, schar chTimeSep, const schar ** ppchLim, bool fUtc)
{
	return FParseSilTime(prgch, cch, chDateSep, chSep, chTimeSep, ppchLim, fUtc, this);
}
