// Set of useful string manipulation functions.

#include <stdarg.h>

#include "StringFunctions.h"

// Replaces the deprecated strdup function.
// Caller must delete[] the result when finished.
_TCHAR * my_strdup(const _TCHAR * pszOriginal)
{
	int cch = (int)_tcslen(pszOriginal);
	_TCHAR * pszResult = new _TCHAR [1 + cch];
	_tcscpy_s(pszResult, 1 + cch, pszOriginal);
	return pszResult;
}

// Acts the same way as new_sprintf() below, except the variable arguments have already been
// collected.
// Caller must delete[] the return value.
_TCHAR * new_vsprintf(const _TCHAR * pszFormat, const va_list arglist)
{
	int cchWksp = 100; // First guess at size needed.
	_TCHAR * szWksp = new _TCHAR [1 + cchWksp];

	// Format it with variable arguments, repeating until Wksp is big enough:
	int cch = _vsntprintf_s(szWksp, 1 + cchWksp, cchWksp, pszFormat, arglist);
	// If the reported number of _TCHARacters written is the same as the size of our buffer, then
	// the terminating zero will have been missed off!
	while (cch == -1 || cch == cchWksp)
	{
		delete[] szWksp;
		cchWksp *= 2;
		szWksp = new _TCHAR [1 + cchWksp];
		cch = _vsntprintf_s(szWksp, 1 + cchWksp, cchWksp, pszFormat, arglist);
	}
	return szWksp;
}

// Acts the same way as sprintf(), but creates a buffer to hold the text that is at least
// as big as the minimum needed.
// Caller must delete[] the return value.
_TCHAR * new_sprintf(const _TCHAR * pszFormat, ...)
{
	// We will be passing on the variable arguments to the new_vsprintf() function:
	va_list arglist;
	va_start(arglist, pszFormat);

	_TCHAR * pszResult = new_vsprintf(pszFormat, arglist);
	return pszResult;
}

// Acts the same as new_vsprintf() above, only it appends the formatted string to rpszMain,
// having inserted ctInsertNewline newlines.
void new_vsprintf_concat(_TCHAR *& rpszMain, int ctInsertNewline, const _TCHAR * pszAddendumFmt,
						 const va_list arglist)
{
	_TCHAR * pszWksp;

	for (int n = 0; n < ctInsertNewline; n++)
	{
		pszWksp = new_sprintf(_T("%s\r\n"), rpszMain ? rpszMain : _T(""));
		delete[] rpszMain;
		rpszMain = pszWksp;
	}
	pszWksp = new_vsprintf(pszAddendumFmt, arglist);
	_TCHAR * pszWksp2 = new_sprintf(_T("%s%s"), rpszMain ? rpszMain : _T(""), pszWksp);
	delete[] pszWksp;
	pszWksp = NULL;

	delete[] rpszMain;
	rpszMain = pszWksp2;
}

// Acts the same as new_sprintf() above, only it appends the formatted string to rpszMain,
// having inserted ctInsertNewline newlines.
void new_sprintf_concat(_TCHAR *& rpszMain, int ctInsertNewline, const _TCHAR * pszAddendumFmt, ...)
{
	// We will be passing on the variable arguments to the new_vsprintf_concat() function:
	va_list arglist;
	va_start(arglist, pszAddendumFmt);

	new_vsprintf_concat(rpszMain, ctInsertNewline, pszAddendumFmt, arglist);
}

// Write nIndent spaces at the start of the returned string, then treat the rest of the
// arguments as in a new_sprintf() call.
// Caller must delete[] the return value;
_TCHAR * new_ind_sprintf(int nIndent, const _TCHAR * pszFormat, ...)
{
	_TCHAR * pszWksp = NULL;

	for (int n = 0; n < nIndent; n++)
		new_sprintf_concat(pszWksp, 0, _T(" "));

	// We will be passing on the variable arguments to the new_vsprintf_concat() function:
	va_list arglist;
	va_start(arglist, pszFormat);

	new_vsprintf_concat(pszWksp, 0, pszFormat, arglist);

	return pszWksp;
}
