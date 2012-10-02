/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilTime.h
Responsibility: Darrell Zook
Last reviewed: 8/17/99

	Abbreviations used:
		UTC stands for Universal Coordinated Time, which is the same as Greenwich Mean Time.
		DST stands for Daylight Savings Time.

	SilTime stores the number of milliseconds that have elapsed since January 1, 1601.
	It can store values into the year 292,278,625 AD and back to -292,275,424 which is
	292,275,425 BC.

	An SilTime value is always interpreted as a UTC time/date as opposed to local time.
	int64 is used for values that are not interpreted as UTC, and SilTime is used for values
		that should be interpreted as UTC time.

	NOTE: These time functions do not take into account any time changes throughout history.
		It takes into account daylight savings time, but any changes in how time was kept
		(i.e. adding or subtracting days/seconds/etc. and other calendar changes) are ignored.
		If these are required for display purposes, the display algorithm must take these into
		account. For example, displaying a year of 0 should show 1 BC instead of 0.

	There are two functions to convert to and from the date that is stored in a VARIANT
		(VT_DATE). A VARIANT date is a double and is interpreted as follows:
		1)	The whole number part of the value is the number of days that have passed since
			December 30, 1899. It can be both positive (after 12/30/1899) and negative (before
			12/30/1899).
		2)	The fraction part of the value is the portion of the day that has passed. 6 AM
			would be 0.25, noon would be 0.5, 9 PM would be 0.875, etc.
		Examples:
			After 12/30/1899
				Date                        Value
				--------------------------  -----
				30 December 1899, midnight  0.00
				1 January 1900, midnight    2.00
				4 January 1900, midnight    5.00
				4 January 1900, 6 A.M.      5.25
				4 January 1900, noon        5.50
				4 January 1900, 9 P.M.      5.875

			After 12/30/1899
				Date                        Value
				--------------------------  -----
				30 December 1899, midnight  0.00
				29 December 1899, midnight  -1.00
				18 December 1899, midnight  -12.00
				18 December 1899, 6 A.M.    -12.25
				18 December 1899, noon      -12.50
				18 December 1899, 6 P.M.    -12.75
				19 December 1899, midnight  -11.00
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UtilTime_H
#define UtilTime_H 1


// This is so headers from IDL files see SilTime as a struct.
#define SILTIME_IS_STRUCT 1


// The year to which SilTime is relative.
const int kyearBase = 1601;


enum YearMonth
{
	kymonJanuary = 1,
	kymonFebruary,
	kymonMarch,
	kymonApril,
	kymonMay,
	kymonJune,
	kymonJuly,
	kymonAugust,
	kymonSeptember,
	kymonOctober,
	kymonNovember,
	kymonDecember
};


enum WeekDay
{
	kwdaySunday = 0,
	kwdayMonday,
	kwdayTuesday,
	kwdayWednesday,
	kwdayThursday,
	kwdayFriday,
	kwdaySaturday
};


/*----------------------------------------------------------------------------------------------
	The type of the year is indicated by whether it's a leap year and by the day of the week
	that Jan 1 falls on.
	Hungarian: yt
----------------------------------------------------------------------------------------------*/
enum YearType
{
	kytSunday = 0,
	kytMonday,
	kytTuesday,
	kytWednesday,
	kytThursday,
	kytFriday,
	kytSaturday,
	kytSundayLeap,
	kytMondayLeap,
	kytTuesdayLeap,
	kytWednesdayLeap,
	kytThursdayLeap,
	kytFridayLeap,
	kytSaturdayLeap,

	kytLim
};


// Constant strings for specifying "BC", "AD", "Before", "After" etc.
// Wide:
extern const wchar * g_rgchwDatePrec[];
extern const wchar * g_rgchwDateBC_AD[];
extern const wchar * g_rgchwDateBlank[];
// 8-bit:
extern const char * g_rgchsDatePrec[];
extern const char * g_rgchsDateBC_AD[];
extern const char * g_rgchsDateBlank[];

#ifdef UNICODE
#define g_rgchDatePrec  g_rgchwDatePrec
#define g_rgchDateBC_AD g_rgchwDateBC_AD
#define g_rgchDateBlank g_rgchwDateBlank
#else
#define g_rgchDatePrec  g_rgchsDatePrec
#define g_rgchDateBC_AD g_rgchsDateBC_AD
#define g_rgchDateBlank g_rgchsDateBlank
#endif

// Call this function and assert this flag before trying to use any of the above globals:
extern void LoadDateQualifiers();
extern bool g_fDoneDateQual;



const int kmsecPerDay =  86400000;
const int kmsecPerHour = 3600000;
const int kmsecPerMin =  60000;
const int kmsecPerSec =  1000;
const int ksecPerMin =   60;
const int kminPerDay =   1440;
const int kminPerHour =  60;
const int khourPerDay =  24;
const int kdayPerWeek =  7;
const int kmonPerYear =  12;

// A period is defined to be 400 years.
const int kyearPerPeriod = 400;
const int kdayPerPeriod = 146097;
const int64 kmsecPerPeriod = (int64)kdayPerPeriod * kmsecPerDay;

// Number of FILETIME ticks per millisecond. The FILETIME clock ticks every 100 nanoseconds
// That is, an ftt is 10^-7 seconds, which is 10^-4 milliseconds.
const int kfttPerMsec = 10000;


/*----------------------------------------------------------------------------------------------
	Decomposed SilTime.
	Hungarian: sti
----------------------------------------------------------------------------------------------*/
struct SilTimeInfo
{
	int year; // Year (can be negative).
	int ymon; // Month in year: 1 to 12.
	int mday; // Day in Month: 1 to 31.
	int wday; // Week day: 1 to 7.
	int hour; // 0 to 23.
	int min;  // 0 to 59.
	int sec;  // 0 to 59.
	int msec; // 0 to 999.
};


/*----------------------------------------------------------------------------------------------
	SilTime structure. This must be 64 bits. MIDL views this as a "Currency" so that VB sees
	it as a 64-bit integer.
	Hungarian: stim
----------------------------------------------------------------------------------------------*/
struct SilTime
{
	// Milliseconds since 1 January 1601, in UTC.
	int64 m_msec;

	/*******************************************************************************************
		Static functions.
	*******************************************************************************************/

	// Return an SilTime containing the current time.
	static SilTime CurTime(void)
	{
		int64 ftt;
		::GetSystemTimeAsFileTime((FILETIME *)&ftt);
		return SilTime(ftt / kfttPerMsec);
	}

	static SilTime VarTime(double vtim);

	// Returns true iff the given year is a leap year.
	static bool IsLeapYear(int year)
		{ return !(year & 3) && ((year % 100) || !(year % 400)); }

	/*******************************************************************************************
		Constructors.
	*******************************************************************************************/

	SilTime(void)
		{ }
	SilTime(int64 msec)
		{ m_msec = msec; }
	SilTime(const SilTimeInfo & sti, bool fUtc = false)
		{ SetTimeInfo(sti, fUtc); }
	SilTime(int year, int month = 1, int dayOfMonth = 1, int hour = 0, int min = 0,
		int sec = 0, int msec = 0, bool fUtc = false);

	/*******************************************************************************************
		Operators.
	*******************************************************************************************/

	bool operator == (const SilTime & stim) const
	{
		return m_msec == stim.m_msec;
	}

	bool operator != (const SilTime & stim) const
	{
		return m_msec != stim.m_msec;
	}

	int64 operator - (const SilTime & stim) const
	{
		return m_msec - stim.m_msec;
	}

	SilTime operator - (int64 dmsec) const
	{
		return SilTime(m_msec - dmsec);
	}

	SilTime operator + (int64 dmsec) const
	{
		return SilTime(m_msec + dmsec);
	}

	SilTime & operator -= (int64 dmsec)
	{
		m_msec -= dmsec;
		return *this;
	}

	SilTime & operator += (int64 dmsec)
	{
		m_msec += dmsec;
		return *this;
	}


	/*******************************************************************************************
		Methods to get information.
	*******************************************************************************************/

	void GetTimeInfo(SilTimeInfo * psti, bool fUtc = false) const;
	int TimeZoneOffset(void) const;
	int Year(bool fUtc = false) const
	{
		SilTimeInfo sti;
		GetTimeInfo(&sti, fUtc);
		return sti.year;
	}
	int Month(bool fUtc = false) const
	{
		SilTimeInfo sti;
		GetTimeInfo(&sti, fUtc);
		return sti.ymon;
	}
	int Date(bool fUtc = false) const
	{
		SilTimeInfo sti;
		GetTimeInfo(&sti, fUtc);
		return sti.mday;
	}
	int WeekDay(bool fUtc = false) const
	{
		SilTimeInfo sti;
		GetTimeInfo(&sti, fUtc);
		return sti.wday;
	}
	int Hour(bool fUtc = false) const
	{
		if (fUtc)
			return (int)::ModPos(m_msec, (int64)kmsecPerDay) / kmsecPerHour;
		SilTimeInfo sti;
		GetTimeInfo(&sti, false);
		return sti.hour;
	}
	int Minute(bool fUtc = false) const
	{
		if (fUtc)
			return (int)::ModPos(m_msec, (int64)kmsecPerHour) / kmsecPerMin;
		SilTimeInfo sti;
		GetTimeInfo(&sti, false);
		return sti.min;
	}
	// NOTE: As far as I (ShonK) can tell, the time zone offset is always some number of full
	// minutes, so the second and millisecond values don't depend on whether the time is
	// measured in UTC or local time.
	int Second(void) const
		{ return (int)::ModPos(m_msec, (int64)kmsecPerMin) / kmsecPerSec; }
	int MilliSecond(void) const
		{ return (int)::ModPos(m_msec, (int64)kmsecPerSec); }
	double VarTime(void) const;

	/*******************************************************************************************
		Methods to set information.
	*******************************************************************************************/

	void SetToCurTime(void)
	{
		int64 ftt;
		::GetSystemTimeAsFileTime((FILETIME *)&ftt);
		m_msec = ftt / kfttPerMsec;
	}
	void SetTimeInfo(const SilTimeInfo & sti, bool fUtc = false);
	void SetYear(int year, bool fUtc = false)
	{
		SilTimeInfo sti;
		GetTimeInfo(&sti, fUtc);
		if (year != sti.year)
		{
			sti.year = year;
			SetTimeInfo(sti, fUtc);
		}
	}
	void SetMonth(int ymon, bool fUtc = false)
	{
		SilTimeInfo sti;
		GetTimeInfo(&sti, fUtc);
		if (ymon != sti.ymon)
		{
			sti.ymon = ymon;
			SetTimeInfo(sti, fUtc);
		}
	}
	void SetDate(int mday, bool fUtc = false)
	{
		SilTimeInfo sti;
		GetTimeInfo(&sti, fUtc);
		if (mday != sti.mday)
		{
			sti.mday = mday;
			SetTimeInfo(sti, fUtc);
		}
	}
	void SetHour(int hour, bool fUtc = false)
	{
		if (fUtc)
		{
			m_msec =
				(FloorDiv(m_msec, (int64)kmsecPerDay) * khourPerDay + hour) * kmsecPerHour +
				ModPos(m_msec, (int64)kmsecPerHour);
		}
		else
		{
			SilTimeInfo sti;
			GetTimeInfo(&sti, false);
			if (hour != sti.hour)
			{
				sti.hour = hour;
				SetTimeInfo(sti, false);
			}
		}
	}
	void SetMinute(int min, bool fUtc = false)
	{
		if (fUtc)
		{
			m_msec =
				(FloorDiv(m_msec, (int64)kmsecPerHour) * kminPerHour + min) * kmsecPerMin +
				ModPos(m_msec, (int64)kmsecPerMin);
		}
		else
		{
			SilTimeInfo sti;
			GetTimeInfo(&sti, false);
			if (min != sti.min)
			{
				sti.min = min;
				SetTimeInfo(sti, false);
			}
		}
	}
	void SetSecond(int sec)
	{
		m_msec =
			(FloorDiv(m_msec, (int64)kmsecPerMin) * ksecPerMin + sec) * kmsecPerSec +
			ModPos(m_msec, (int64)kmsecPerSec);
	}
	void SetMilliSecond(int msec)
	{
		m_msec = FloorDiv(m_msec, (int64)kmsecPerSec) * kmsecPerSec + msec;
	}
	void SetToVarTime(double vtim);

	bool FParse(const wchar * prgch, int cch, wchar chDateSep, wchar chSep, wchar chTimeSep,
		const wchar ** ppchLim, bool fUtc);
	bool FParse(const schar * prgch, int cch, schar chDateSep, schar chSep, schar chTimeSep,
		const schar ** ppchLim, bool fUtc);
	// Since this has to be represented as an Int64, and we can construct one from an int64,
	// it is convenient to be able to do the reverse switch.
	int64 AsInt64()
	{
		return m_msec;
	}
};

/*----------------------------------------------------------------------------------------------
Class: MeasureDuration
Description: This class is used to measure the elapsed time spent performing some operation,
often cumulatively. It is initialized with a reference to some variable. When destructed,
it adds to that variable the number of milliseconds it was in existence.
For example: to measure the total time spent executing a certain method, declare a variable,
say m_msMyMethod. Then, at the start of the method, insert MeasureDuration(m_msMyMethod).
At some convenient point set m_msMyMethod to zero, and later see what it contains.
You can also surround a smaller piece of code with braces and call this at the start.
Hungarian: not needed
----------------------------------------------------------------------------------------------*/
#define MeasureDuration(arg) _MeasureDuration _measure_duration_##__LINE__(&arg)

class _MeasureDuration
{
public:
	_MeasureDuration(int * pms)
	{
		m_pms = pms;
		m_ms = ::GetTickCount();
	}
	~_MeasureDuration()
	{
		*m_pms += ::GetTickCount() - m_ms;
	}
protected:
	int * m_pms;
	DWORD m_ms;
};

int GetDaysInMonth(int nMonth, int nYear);

#endif // !UtilTime_H
