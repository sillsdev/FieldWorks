/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfUtil.cpp
Responsibility: Steve McConnel
Last reviewed:

	This file contains method definitions for the AfUtil namespace.
	It also contains class definitions for the following classes:
		FwSettings
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

/*----------------------------------------------------------------------------------------------
	Convert the string to a int value in mPoints.  The string's value is determined by seeing
	it contains " , mm , cm , pt.  If none of these are in the string then the value is assumed
	to be the default measurement system (msrSys).

	@param pstrb Pointer to a string object.
	@param msrSys Specify the default measurement system, if not encoded in the string.
	@param pnValue Pointer to an integer for returning the value.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfUtil::GetStrMsrValue(StrAppBuf * pstrb, MsrSysType msrSys, int * pnValue)
{
	AssertPtr(pstrb);
	AssertPtr(pnValue);

	// load inches, mm, cm, pt strings from resource file
	StrAppBuf strb1 = (*pstrb).Chars();
	StrAppBuf strInch;
	StrAppBuf strIn;
	StrAppBuf strMm;
	StrAppBuf strCm;
	StrAppBuf strPt;

	strIn.Load(kstidIn);
	strInch.Load(kstidInches);
	strMm.Load(kstidMm);
	strCm.Load(kstidCm);
	strPt.Load(kstidPt);

	int64 nVal;


	if (_tcsstr(*pstrb, strIn.Chars()))
	{
		nVal = (int64) (_tstof(strb1.Chars()) * 72000);
	}
	else if (_tcsstr(*pstrb, strInch.Chars()))
	{
		nVal = (int64) (_tstof(strb1.Chars()) * 72000);
	}
	else if (_tcsstr(*pstrb, strMm.Chars()))
	{
		nVal = (int64) (_tstof(strb1.Chars()) * 2835);
	}
	else if (_tcsstr(*pstrb, strCm.Chars()))
	{
		nVal = (int64) (_tstof(strb1.Chars()) * 28347);
	}
	else if (_tcsstr(*pstrb, strPt.Chars()))
	{
		nVal = (int64) (_tstof(strb1.Chars()) * 1000);
	}
	else
	{
		switch (msrSys)
		{
		case kninches:
			nVal = (int64) (_tstof(strb1.Chars()) * 72000);
			break;
		case knmm:
			nVal = (int64) (_tstof(strb1.Chars()) * 2835);
			break;
		case kncm:
			nVal = (int64) (_tstof(strb1.Chars()) * 28347);
			break;
		case knpt:
			nVal = (int64) (_tstof(strb1.Chars()) * 1000);
			break;
		default:
			Assert(false);	// We should never reach this.
		}
	}
	if (nVal > 2000000000)
		nVal = 2000000000;

	if (nVal < -2000000000)
		nVal = -2000000000;
	*pnValue = (int)nVal;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Convert a int value in mPoints to a string.  The output string is a value followed by
	" , mm , cm , pt.  The measurement system is determined by (msrSys).

	@param nValue Value to encode.
	@param msrSys Specify the measurement system.
	@param pstrb Pointer to a string object for returning the encoded value.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfUtil::MakeMsrStr(int nValue, MsrSysType msrSys, StrAppBuf * pstrb)
{
	AssertPtr(pstrb);

	if (nValue == knConflicting)
	{
		*pstrb = "";
		return true;
	}
	// load inches, mm, cm strings from resource file
	StrAppBuf strb1;
	StrAppBuf strInch;
	StrAppBuf strMm;
	StrAppBuf strCm;
	StrAppBuf strPt;

	strInch.Load(kstidInches);
	strMm.Load(kstidMm);
	strCm.Load(kstidCm);
	strPt.Load(kstidPt);

	int nVal;
	switch (msrSys)
	{
	case kninches:
		nVal = nValue / 720;
		strb1.Format(_T("%d.%02d%s"), nVal / 100, nVal % 100, strInch.Chars());
		break;
	case knmm:
		strb1.Format(_T("%d%s%s"), (nValue + 1417) / 2835, " ",strMm.Chars());
		break;
	case kncm:
		nVal = (int)((nValue + 1417) / 2834.7);
		strb1.Format(_T("%d.%01d%s%s"), nVal / 10, nVal % 10, " ", strCm.Chars());
		break;
	case knpt:
		strb1.Format(_T("%d%s%s"), nValue / 1000, " ", strPt.Chars());
		break;
	default:
		Assert(false);	// We should never reach this.
	}

	*pstrb = strb1;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Creates a TsString from a resource ID.

	@param rid String resource id.
	@param pptss Address of a pointer for returning the TsString.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfUtil::GetResourceTss(int rid, int ws, ITsString ** pptss)
{
	Assert(ws);
	AssertPtr(pptss);
	Assert(!*pptss);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(rid);
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, pptss);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Returns the string that corresponds to the type of text that is requested from the given
	resource ID. This is used because the text that shows when the user hovers over a toolbar
	button, the text on the status bar, and the What's This text are all stored in one string
	in the resource file. For some popup menus the menu item text is stored as a fourth string.

	@param rst Type of text desired from the resource string.
	@param rid String resource id.
	@param str Reference to a string object for returning

	@return True if successful, false if an error occurs or an empty string results.
----------------------------------------------------------------------------------------------*/
bool AfUtil::GetResourceStr(ResourceStringType rst, int rid, StrApp & str)
{
	StrApp strT(rid);
	if (!strT.Length())
		return false;

	int ichMin;
	int ichLim;
	switch (rst)
	{
	case krstItem:
		{
			// The actual menu item (used in building popups): the fourth string,
			// or if that is missing the second.
			// Find the 2nd section.
			int ichMin2nd = strT.FindCh('\n') + 1;
			if (ichMin2nd == 0)
				return false;
			int ichLim2nd = strT.FindCh('\n', ichMin2nd);
			if (ichLim2nd == -1)
			{
				ichLim2nd = strT.Length();
				str = strT.Mid(ichMin2nd, ichLim2nd - ichMin2nd);
				return true;
			}
			int ichMin4th = strT.FindCh('\n', ichLim2nd + 1) + 1;
			if (ichMin4th == 0)
			{
				// No fourth, use 2nd
				str = strT.Mid(ichMin2nd, ichLim2nd - ichMin2nd);
				return true;
			}
			int ichLim4th = strT.FindCh('\n', ichMin4th + 1);
			if (ichLim4th == -1)
				ichLim4th = strT.Length();
			str = strT.Mid(ichMin4th, ichLim4th - ichMin4th);
		}
		return true;
	case krstHoverEnabled: // Fall through.
	case krstHoverDisabled:
		// Find the 2nd section.
		ichMin = strT.FindCh('\n') + 1;
		if (ichMin == 0)
			return false;
		ichLim = strT.FindCh('\n', ichMin);
		if (ichLim == -1)
			ichLim = strT.Length();
		str = strT.Mid(ichMin, ichLim - ichMin);
		return true;

	case krstStatusEnabled: // Fall through.
	case krstStatusDisabled:
		// Find the 1st section.
		ichLim = strT.FindCh('\n');
		if (ichLim != -1)
			str = strT.Left(ichLim);
		else
			str = strT;
		return true;

	case krstWhatsThisEnabled: // Fall through.
	case krstWhatsThisDisabled:
		// Find the 3rd section. If there isn't one, use the first part.
		ichLim = strT.FindCh('\n');
		if (ichLim == -1)
		{
			str = strT;
		}
		else
		{
			ichMin = strT.FindCh('\n', ichLim + 1) + 1;
			if (ichMin != 0)
			{
				// There is a third part.
				int ichLimT = strT.FindCh('\n', ichMin);
				if (ichLimT != -1) // There is a fourth part.
					str = strT.Mid(ichMin, ichLimT - ichMin);
				else
					str = strT.Right(strT.Length() - ichMin);
			}
			if (str.Length() == 0)
			{
				// There is not a third part, so use the first part.
				str = strT.Left(ichLim);
			}
		}
		return str.Length() > 0;

	default:
		Assert(false); // This should never happen.
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Show the help file for the application. If pszPage is not NULL, we want to open a
	specific page within the help file.

	@param pszHelp String giving relative pathname to the help file.
	@param pszPage String identifying the specific page desired, or NULL.

	@return True if successful, false if the help file does not exist.
----------------------------------------------------------------------------------------------*/
bool AfUtil::ShowHelpFile(const achar * pszHelp, const achar * pszPage)
{
	AssertPsz(pszHelp);
	AssertPszN(pszPage);
	if (!pszHelp)
		return false;

	StrAppBufPath strbpHelp(pszHelp);
	if (pszPage)
	{
		strbpHelp.Append(_T("::/"));
		strbpHelp.Append(pszPage);
	}
	::HtmlHelp(::GetDesktopWindow(), strbpHelp.Chars(), HH_DISPLAY_TOPIC, NULL);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Calls ::ShellExecute() with the given parameters, and returns the resource id of the
	appropriate error message, or zero if it went OK.
----------------------------------------------------------------------------------------------*/
int AfUtil::Execute(HWND hwnd, LPCTSTR pszOperation, LPCTSTR pszFile, LPCTSTR pszParameters,
	LPCTSTR pszDirectory, int nShowCmd)
{
	int nRes = (int)::ShellExecute(hwnd, pszOperation, pszFile, pszParameters, pszDirectory,
		nShowCmd);

	if (nRes > 32)
		return 0;

	switch (nRes)
	{
	case 0:
		return kstidErrorOutOfMemOrResource;
	case ERROR_FILE_NOT_FOUND:
		return kstidErrorFileNotFound;
	case ERROR_PATH_NOT_FOUND:
		return kstidErrorPathNotFound;
	case ERROR_BAD_FORMAT:
		return kstidErrorBadFormatExe;
	case SE_ERR_ACCESSDENIED:
		return kstidErrorAccessDenied;
	case SE_ERR_ASSOCINCOMPLETE:
		return kstidErrorAssocIncomplete;
	case SE_ERR_DDEBUSY:
		return kstidErrorDDEBusy;
	case SE_ERR_DDEFAIL:
		return kstidErrorDDEFail;
	case SE_ERR_DDETIMEOUT:
		return kstidErrorDDETimeOut;
	case SE_ERR_DLLNOTFOUND:
		return kstidErrorDLLNotFound;
//	case SE_ERR_FNF:
//		return kstidErrorFNF;
	case SE_ERR_NOASSOC:
		return kstidErrorNoAssoc;
	case SE_ERR_OOM:
		return kstidErrorOutOfMemory;
//	case SE_ERR_PNF:
//		return kstidErrorPNF;
	case SE_ERR_SHARE:
		return kstidErrorShare;
	default:
		return kstidErrorUnrecognized;
	}

	Assert(false); // Should not get to here!
	return 0;
}
