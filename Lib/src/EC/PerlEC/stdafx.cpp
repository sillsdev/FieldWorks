// stdafx.cpp : source file that includes just the standard includes
// PerlEC.pch will be the pre-compiled header
// stdafx.obj will contain the pre-compiled type information

#include "stdafx.h"

CPerlInterpreter interp;
CString m_strPerlStdErrorInfo;
CString m_strPerlStdOutputInfo;

void UnLoadPerl()
{
	if( interp.IsLoaded() )
		interp.Unload();
	PXIORedirect::Uninitialize(true);
}

CString GetPXPerlWrapStdError()
{
	// clear it out when it's read
	CString str = m_strPerlStdErrorInfo;
	m_strPerlStdErrorInfo.Empty();
	return str;
}

CString GetPXPerlWrapStdOutput()
{
	// clear it out when it's read
	CString str = m_strPerlStdOutputInfo;
	m_strPerlStdOutputInfo.Empty();
	return str;
}

// last one used
UTF8Mode m_dataModeLast = UTF8_off;

void SetDataEncodingMode(UTF8Mode eUtf8Mode)
{
	m_dataModeLast = eUtf8Mode;
	PXSetUTF8(eUtf8Mode);
}

void CALLBACK funcIOCallback(DWORD dwStream, LPSTR sData, UINT nSize)
{
	if (dwStream == PXPW_REDIR_OUTPUT)
	{
		// I'm not sure, but I don't think we want to treat "StdOutput" as an error...
		if( m_dataModeLast == UTF8_off )
			m_strPerlStdOutputInfo = CA2T((LPCSTR)sData);
		else
			m_strPerlStdOutputInfo = CA2T((LPCSTR)sData, 65001);

		TRACE(_T("PerlEncConverter Output: '%s'\n"), m_strPerlStdOutputInfo);
	}
	else if( dwStream == PXPW_REDIR_ERRORS )
	{
		m_strPerlStdErrorInfo = CA2T((LPCSTR)sData);
		TRACE(_T("PerlEncConverter Error: '%s'"), m_strPerlStdErrorInfo);
	}
	else
		ASSERT(false);
}

void WriteRegKeys(LPCTSTR lpszSubKey, LPCTSTR lpszDefaultString, const CStringArray& astrKeys)
{
	CRegKey keyReg;
	if( keyReg.Create(HKEY_CURRENT_USER, PERLEXPR_REG_ROOT) == ERROR_SUCCESS )
	{
		keyReg.RecurseDeleteKey(lpszSubKey);

		CRegKey keySub;
		if( keySub.Create(keyReg, lpszSubKey) == ERROR_SUCCESS )
		{
			keySub.SetStringValue(_T(""),lpszDefaultString);
			for( int i = 0; i < astrKeys.GetCount(); i++ )
			{
				keySub.SetStringValue(astrKeys[i],_T(""));
			}
		}
	}
}

void EnumRegKeys(LPCTSTR lpszRegKey, LPCTSTR lpszPrefix, CStringArray& astrKeys)
{
	CRegKey keyReg;
	if( keyReg.Open(HKEY_CURRENT_USER, lpszRegKey) == ERROR_SUCCESS )
	{
		DWORD dwIndex = 0;
		BOOL bStop = false;
		do
		{
			DWORD dwValueType = 0, cbName = _MAX_PATH;
			TCHAR lpName[_MAX_PATH];    lpName[0] = 0;
			LONG lVal = RegEnumValue(keyReg,dwIndex++,lpName,&cbName,0,&dwValueType,0,0);
			if( (lVal == ERROR_SUCCESS) || (lVal == ERROR_MORE_DATA) )
			{
				// skip the default value
				if( _tcslen(lpName) > 0 )
				{
					TRACE(_T("Found: (%s)\n"), lpName);
					CString str = lpszPrefix;
					str += lpName;
					astrKeys.Add(str);
				}
			}
			else
				bStop = true;
		} while( !bStop );
	}
}

HRESULT InitPerl()
{
	// since we may have been installed without an actual distribution, query the user
	//  for its location (if we don't already know it)
	//  _T("SOFTWARE\\SIL\\SilEncConverters31\\ConvertersSupported\\SIL.PerlExpression\\PerlPaths")
	CRegKey keyReg;
	if( keyReg.Open(HKEY_CURRENT_USER, PERLEXPR_PATHS_KEY) != ERROR_SUCCESS )
	{
		// couldn't find our initial reg key telling us where the Perl distribution(s) are located,
		//  so ask the user
		CStringArray astrPaths;
		astrPaths.Add(_T("C:\\Perl"));
		astrPaths.Add(_T("C:\\Perl\\lib"));
		astrPaths.Add(_T("C:\\Perl\\site\\lib"));
		WritePerlDistroPaths(astrPaths);
	}

	// same for the default modules to load
	if( keyReg.Open(HKEY_CURRENT_USER, PERLEXPR_MODULES_KEY) != ERROR_SUCCESS )
	{
		// couldn't find our initial reg key telling us where the Perl distribution(s) are located,
		//  so ask the user
		CStringArray astrModules;
		astrModules.Add(_T("Win32"));
		WritePerlModulePaths(astrModules);
	}

	PXIORedirect::Initialize();
	PXIORedirect::SetDestination(PXPW_REDIR_ERRORS | PXPW_REDIR_OUTPUT, PXIORedirect::DestCallback, (LPVOID)funcIOCallback);

	XSItem items[] = {
		// XSItem("PerlEC::boot_TestDlg", SWIG::boot_TestDlg),
		XSItem() // tells it's the end of the list
	};
	PXPerlWrap::PXSetXS(items);

	PXPerlWrap::PXSetUTF8(UTF8_off);

	// get the path to the Perl installation and the default modules to load from the registry
	// (see PerlExpressionEncConverter.rgs for where the default module is added, the Setup program
	//  must set the paths, since there doesn't appear to be anything in the registry that tells where
	//  a perl installation is located).
	CStringArray arCmd;
	arCmd.Add(_T("-w"));
	EnumRegKeys(PERLEXPR_PATHS_KEY, _T("-I"), arCmd);
	// arCmd.SetAtGrow(i++, _T("-IC:\\PXPerl\\lib"));
	// arCmd.SetAtGrow(i++, _T("-IC:\\PXPerl\\site\\lib"));
	PXPerlWrap::PXSetCommandLineOptions(&arCmd);

	// i = 0;
	CStringArray arMod;
	EnumRegKeys(PERLEXPR_MODULES_KEY, _T(""), arMod);
	// arMod.SetSize(1);
	//arMod.SetAtGrow(i++, _T("LWP::UserAgent"));
	//arMod.SetAtGrow(i++, _T("Graphics::Magick"));
	// arMod.SetAtGrow(i++, _T("Win32"));
	PXPerlWrap::PXSetDefaultModules(&arMod);

	return S_OK;
}

#include "BrowseForStrings.h"

void WritePerlDistroPaths(CStringArray& astrPaths)
{
	CBrowseForStrings dlg(_T("Indicate the paths to your Perl distribution and library root folders"), astrPaths, true);
	if( dlg.DoModal() == IDOK )
	{
		CFileStatus fstat;
		if ((astrPaths.GetCount() > 0) && !CFile::GetStatus(astrPaths[0], fstat))
		{
			CString strErrorMsg;
			strErrorMsg.Format(_T("The '%s' folder does not exist! You have to have a Perl Distribution installed to use this converter"), astrPaths[0] );
			AfxMessageBox(strErrorMsg);
		}
		else
			WriteRegKeys(_T("PerlPaths"), _T("Paths to the Perl installation library folders"), astrPaths);
	}
}

void WritePerlModulePaths(CStringArray& astrModules)
{
	CBrowseForStrings dlg(_T("Indicate the Perl modules to automatically load for expression evaluation"), astrModules, false);
	if( dlg.DoModal() == IDOK )
		WriteRegKeys(_T("DefaultModules"), _T("Default modules to load for expressions"), astrModules);
}
