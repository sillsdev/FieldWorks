// PerlExpressionEncConverter.cpp : Implementation of CPerlExpressionEncConverter

#include "stdafx.h"

#include "PerlExpressionEncConverter.h"

/////////////////////////////////////////////////////////////////////////////
// CPerlExpressionEncConverter
#define PerlInactivityWarningTimeOut 60000   // 60 seconds of inactivity means clean up resources

LPCTSTR clpszPerlExpressionProgID = _T("SilEncConverters31.PerlExpressionEncConverter.5100b");
LPCTSTR clpszPerlExpressionImplType = _T("SIL.PerlExpression");

CPerlExpressionEncConverter::CPerlExpressionEncConverter()
  : CEncConverter(clpszPerlExpressionProgID,clpszPerlExpressionImplType)  // from PerlExpressionEncConverter.rgs)
  , m_pScript(0)
{
	m_eStringDataTypeIn = m_eStringDataTypeOut = UTF8_off;
}

STDMETHODIMP CPerlExpressionEncConverter::Initialize
(
	BSTR            ConverterName,
	BSTR            converterSpec,
	BSTR*           LhsEncodingId,
	BSTR*           RhsEncodingId,
	ConvType*       peConversionType,
	long*           plProcessTypeFlags,
	long            CodePageInput,
	long            CodePageOutput,
	VARIANT_BOOL    bAdding // if adding, then check whether transliterators exist (otherwise
							// wait until needed.
)
{
	// it might fail if the user has hard-coded the .tec file extension, but we haven't
	//  compiled it yet.
	HRESULT hr = CEncConverter::Initialize(ConverterName, converterSpec, LhsEncodingId, RhsEncodingId, peConversionType, plProcessTypeFlags, CodePageInput, CodePageOutput, bAdding);

	if( bAdding )
	{
		// if the user doesn't tell us, then at least set the Encoding IDs as "UNICODE" if we can
		//  determine this from the ConvType
		if( m_eConversionType != ConvType_Unknown )
		{
			if( NormalizeLhsConversionType(m_eConversionType) == NormConversionType_eUnicode )
			{
				m_eStringDataTypeIn = UTF8_on;
				if(     (LhsEncodingId != 0)
					&&  (IsEmpty(*LhsEncodingId)) )
				{
					m_strLhsEncodingID = _T("UNICODE");
					get_LeftEncodingID(LhsEncodingId);  // use this to get a copy
				}
			}
			else
				m_eStringDataTypeIn = UTF8_off;

			if( NormalizeRhsConversionType(m_eConversionType) == NormConversionType_eUnicode )
			{
				m_eStringDataTypeOut = UTF8_on;
				if(     (RhsEncodingId != 0)
					&&  (IsEmpty(*RhsEncodingId)) )
				{
					m_strRhsEncodingID = _T("UNICODE");
					get_RightEncodingID(RhsEncodingId);  // use this to get a copy
				}
			}
			else
				m_eStringDataTypeOut = UTF8_off;
		}

		// make sure, it has the correct process type
		m_lProcessType |= ProcessTypeFlags_PerlExpression;
		if( plProcessTypeFlags != 0 )
			*plProcessTypeFlags |= ProcessTypeFlags_PerlExpression;

		// do the load at this point; not that we need it, but for checking that everything's okay.
		hr = Load(CString(m_strConverterID));
		if( FAILED(hr) )
			return hr;
	}

	return hr;
}

HRESULT CPerlExpressionEncConverter::GetAttributeKeys(CComSafeArray<BSTR>& rSa)
{
	// do the load at this point.
	HRESULT hr = Load(CString(m_strConverterID));
	if( SUCCEEDED(hr) )
	{
		/*
		CComBSTR strValue = Py_GetVersion();
		WriteAttributeDefault(rSa, _T("Perl Version"), strValue);
		strValue = Py_GetPlatform();
		WriteAttributeDefault(rSa, _T("Platform"), strValue);
		strValue = Py_GetCopyright();
		WriteAttributeDefault(rSa, _T("Copyright"), strValue);
		strValue = Py_GetCompiler();
		WriteAttributeDefault(rSa, _T("Compiler"), strValue);
		strValue = Py_GetBuildInfo();
		WriteAttributeDefault(rSa, _T("Build Info"), strValue);
		*/
	}
	return hr;
}

#define STR_IN_OUT      _T("strInOut")
#define STR_INPUT       _T("strIn")
#define STR_OUTPUT      _T("strOut")
#define BUILD_SCALAR(s) _T("$") ## s
#define INIT_SCALAR(s)  interp.GetScalar(*m_pScript, s)

// the incoming expression should be one or more a simple Perl statements
//  that anticipate getting the input via $strIn and return the result via a
//  variable named $strOut. e.g.
//
//      $strOut = reverse($strIn);
//
HRESULT CPerlExpressionEncConverter::Load(const CString& strExpression)
{
	TRACE(_T("PerlEncConverter entering Load\n"));
	HRESULT hr = S_OK;

	SetDataEncodingMode(m_eStringDataTypeIn);

	// dealing with the scalar input/output variables in the expression.
	// there are two options:
	//  1)  input and output are the same scalar variable (must be $strInOut)
	//  2)  input ($strIn) and output ($strOut) scalar variables
	//  3)  input ($strIn) and output is print(f) stmt
	m_bUseInOut = false;
	if( strExpression.Find(BUILD_SCALAR(STR_IN_OUT)) != -1 )
	{
		// the user is using only a single scalar for both input and output
		m_bUseInOut = true;
	}

	else if( strExpression.Find(BUILD_SCALAR(STR_INPUT)) == -1 )
	{
		CString strError;
		strError.Format(_T("PerlEncConverter: The Perl expression:\n\n'%s'\n\ndoesn't contain the required reference to the input data string '$strIn ' or '$strInOut '\n\n(e.g. '$strOut = reverse($strIn);')"), strExpression);
		hr = Error(strError, __uuidof(IEncConverter), ErrStatus_CompilationFailed);
	}
	// the output either has to be to $strOut or standard output (i.e. 'print' or 'printf')
	else if((strExpression.Find(BUILD_SCALAR(STR_OUTPUT)) == -1)
		&&  (strExpression.Find(_T("print ")) == -1)
		&&  (strExpression.Find(_T("printf ")) == -1)
		)
	{
		CString strError;
		strError.Format(_T("PerlEncConverter: The Perl expression\n\n'%s'\n\ndoesn't contain the required assignment to the output data string '$strOut'\n\n(e.g. '$strOut = reverse($strIn);')"), strExpression);
		hr = Error(strError, __uuidof(IEncConverter), ErrStatus_CompilationFailed);
	}

	if( SUCCEEDED(hr) )
	{
		// load the interpreter if it isn't already loaded.
		if( !interp.IsLoaded() )
		{
			InitPerl();
			VERIFY(interp.Load());
		}

		// load the expression if it isn't already loaded or if it's changed
		BOOL bReparse = false;
		if( m_pScript == 0 )
			m_pScript = new CScript();

		if( !m_pScript->IsLoaded() )
		{
			m_pScript->Load(strExpression);
			bReparse = true;
		}
		else if( m_pScript->GetScript() != strExpression )
		{
			*m_pScript = strExpression;
			bReparse = true;
		}

		// if something significant has changed (or the first time), parse the expression
		//  and initialize the appropriate scalar variables
		if( bReparse )
		{
			if( !interp.Parse(*m_pScript) )
			{
				hr = CheckForStdError();
			}

			// sometimes, the parse fails, but there's no error... if that happens, just go for it
			//  and initialize the scalars.
			if( SUCCEEDED(hr) )
			{
				if( m_bUseInOut )
				{
					m_sInput = INIT_SCALAR(STR_IN_OUT);
				}
				else
				{
					m_sInput = INIT_SCALAR(STR_INPUT);
					m_sOutput = INIT_SCALAR(STR_OUTPUT);
				}
			}
		}
	}

	TRACE(_T("PerlEncConverter exiting Load\n"));
	return hr;
}

// call to clean up resources when we've been inactive for some time.
void CPerlExpressionEncConverter::InactivityWarning()
{
	TRACE(_T("CPerlExpressionEncConverter::InactivityWarning\n"));
	FinalRelease();
}

HRESULT CPerlExpressionEncConverter::PreConvert
(
	EncodingForm    eInEncodingForm,
	EncodingForm&	eInFormEngine,
	EncodingForm    eOutEncodingForm,
	EncodingForm&	eOutFormEngine,
	NormalizeFlags& eNormalizeOutput,
	BOOL            bForward,
	UINT            nInactivityWarningTimeOut
)
{
	HRESULT hr = CEncConverter::PreConvert(eInEncodingForm, eInFormEngine,
									eOutEncodingForm, eOutFormEngine,
									eNormalizeOutput, bForward, PerlInactivityWarningTimeOut);

	if( SUCCEEDED(hr) )
	{
		// I don't think Perl does "bi-directional", so this code ignores the
		//  the direction.
		switch(eInEncodingForm)
		{
			case EncodingForm_LegacyBytes:
			case EncodingForm_LegacyString:
				eInFormEngine = EncodingForm_LegacyBytes;
				m_eStringDataTypeIn = UTF8_off;
				break;

			case EncodingForm_UTF8Bytes:
			case EncodingForm_UTF8String:
			case EncodingForm_UTF16BE:
			case EncodingForm_UTF16:
			case EncodingForm_UTF32BE:
			case EncodingForm_UTF32:
				eInFormEngine = EncodingForm_UTF16;
				m_eStringDataTypeIn = UTF8_on;
				break;
		};

		switch(eOutEncodingForm)
		{
			case EncodingForm_LegacyBytes:
			case EncodingForm_LegacyString:
				eOutFormEngine = EncodingForm_LegacyBytes;
				m_eStringDataTypeOut = UTF8_off;
				break;

			case EncodingForm_UTF8Bytes:
			case EncodingForm_UTF8String:
			case EncodingForm_UTF16BE:
			case EncodingForm_UTF16:
			case EncodingForm_UTF32BE:
			case EncodingForm_UTF32:
				eOutFormEngine = EncodingForm_UTF16;
				m_eStringDataTypeOut = UTF8_on;
				break;
		};

		// do the load at this point.
		hr = Load(CString(m_strConverterID));
	}

	return hr;
}

HRESULT CPerlExpressionEncConverter::DoConvert
(
	LPBYTE  lpInBuffer,
	UINT    nInLen,
	LPBYTE  lpOutBuffer,
	UINT&   rnOutLen
)
{
	HRESULT hr = S_OK;

	SetDataEncodingMode(m_eStringDataTypeIn);
	switch(m_eStringDataTypeIn)
	{
		case UTF8_off:
			m_sInput = CA2T((LPCSTR)lpInBuffer);
			break;
		case UTF8_on:
			m_sInput = (LPCWSTR)lpInBuffer;
			break;
	}

	// run the expression
	TRACE(_T("PerlEncConverter about to run script\n"));
	bool bRet = interp.Run(*m_pScript);
	TRACE(_T("PerlEncConverter finished running script\n"));

	SetDataEncodingMode(m_eStringDataTypeOut);
	if( SUCCEEDED((hr = CheckForStdError())) )
	{
		CString str;
		if( m_bUseInOut )
			str = m_sInput.String();
		else
			str = m_sOutput.String();

		// otherwise, see if the expression just prints it to std output
		if( str.IsEmpty() )
			str = CheckForStdOutput();

		if( !str.IsEmpty() )
		{
			// if it's legacy output, then narrowize it to get the length
			if( m_eStringDataTypeOut == UTF8_off )
			{
				CStringA strA = CT2A(str);

				// be sure it isn't longer than the output buffer (Perl can return *anything); not
				//  just 3x as with cp converters)
				int nLen = strA.GetLength();
				if( nLen > (int)rnOutLen )
					hr = Error((LPCTSTR)str, __uuidof(IEncConverter), ErrStatus_OutputBufferFull);
				else
				{
					rnOutLen = nLen;
					memcpy(lpOutBuffer, (LPVOID)(LPCSTR)strA, rnOutLen);
				}
			}
			else // if( m_eStringDataTypeOut == UTF8_on )
			{
				// be sure it isn't longer than the output buffer (Perl can return *anything); not
				//  just 3x as with cp converters)
				int nLen = str.GetLength() * sizeof(TCHAR);
				if( nLen > (int)rnOutLen )
					hr = Error((LPCTSTR)str, __uuidof(IEncConverter), ErrStatus_OutputBufferFull);
				else
				{
					rnOutLen = nLen;
					memcpy(lpOutBuffer, (LPVOID)(LPCTSTR)str, rnOutLen);
				}
			}
		}
	}

	return hr;
}

HRESULT CPerlExpressionEncConverter::CheckForStdError()
{
	HRESULT hr = S_OK;

	TRACE(_T("PerlEncConverter about to flush errors\n"));
	PXIORedirect::Flush(PXPW_REDIR_ERRORS);
	TRACE(_T("PerlEncConverter finished flushing errors\n"));

	CString strError = GetPXPerlWrapStdError();
	if( !strError.IsEmpty() )
	{
		CString strErrorHeader;
		strErrorHeader.Format(_T("While executing the following Perl expression:\n\n%s\n\nthe following error occurred:\n\n%s"), m_strConverterID, strError );

		hr = Error(strErrorHeader, __uuidof(IEncConverter), ErrStatus_NoReturnDataBadOutForm);
	}

	TRACE(_T("PerlEncConverter about to resume redirection\n"));
	PXIORedirect::Resume(PXPW_REDIR_ERRORS);
	TRACE(_T("PerlEncConverter finished resuming redirection\n"));

	return hr;
}

CString CPerlExpressionEncConverter::CheckForStdOutput()
{
	PXIORedirect::Flush(PXPW_REDIR_OUTPUT);

	CString strStdOutput = GetPXPerlWrapStdOutput();

	PXIORedirect::Resume(PXPW_REDIR_OUTPUT);

	return strStdOutput;
}
