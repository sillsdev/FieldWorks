// stdafx.cpp : source file that includes just the standard includes
// CppEncConverterCommon.pch will be the pre-compiled header
// stdafx.obj will contain the pre-compiled type information

#include "stdafx.h"
#include <comdef.h> // _com_issue_errorex

#define strCaption  _T("EncConverters Error")

BOOL ProcessHResult(HRESULT hr, IUnknown* pEC)
{
	if( hr == S_OK )
		return true;

	// otherwise, throw a _com_issue_errorex and catch it (so we can use it to get
	//  the error description out of it for us.
	try
	{
		_com_issue_errorex(hr, pEC,__uuidof(IEncConverter));
	}
	catch(_com_error & er)
	{
		if( er.Description().length() > 0)
		{
			::MessageBox(NULL, er.Description(), strCaption, MB_OK);
		}
		else
		{
			::MessageBox(NULL, er.ErrorMessage(), strCaption, MB_OK);
		}
	}

	return false;
}
