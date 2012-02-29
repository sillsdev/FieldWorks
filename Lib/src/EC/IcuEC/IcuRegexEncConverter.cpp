// IcuRegexEncConverter.cpp : Implementation of CIcuRegexEncConverter

#include "stdafx.h"
#include "IcuRegexEncConverter.h"

/////////////////////////////////////////////////////////////////////////////
// CIcuRegexEncConverter
LPCTSTR clpszIcuRegexImplType   = _T("ICU.regex");
LPCTSTR clpszIcuRegexProgId     = _T("SilEncConverters40.IcuECRegex.40");

CIcuRegexEncConverter::CIcuRegexEncConverter()
  : CEncConverter(clpszIcuRegexProgId, clpszIcuRegexImplType)  // from IcuRegex.rgs)
  , m_pMatcher(0)
{
}

// e.g. "Devanagari-Latin"
STDMETHODIMP CIcuRegexEncConverter::Initialize
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
		hr = Load(m_strConverterID);
		if( hr != S_OK )    // can't use "FAILED" since the hrs are ICU codes which don't have the high 'error' bit set.
			return ReturnError(hr);

		if( IsEmpty(m_strLhsEncodingID) )
		{
			m_strLhsEncodingID = _T("UNICODE");
			if( LhsEncodingId != 0 )
				get_LeftEncodingID(LhsEncodingId);
		}

		if( IsEmpty(m_strRhsEncodingID) )
		{
			m_strRhsEncodingID = _T("UNICODE");
			if( RhsEncodingId != 0 )
				get_RightEncodingID(RhsEncodingId);
		}

		// by definition, unidirectional and with a particular process type
		m_eConversionType = ConvType_Unicode_to_Unicode;
		m_lProcessType |= ProcessTypeFlags_ICURegularExpression;

		if( peConversionType != 0 )
			*peConversionType = m_eConversionType;

		// make sure, it has the correct process type
		if( plProcessTypeFlags != 0 )
			*plProcessTypeFlags = m_lProcessType;
	}

	return hr;
}

HRESULT CIcuRegexEncConverter::ReturnError(long status)
{
	// give the base class first crack at it.
	HRESULT hr = CEncConverter::ReturnError(status);

	// if it couldn't decide what to do, then check our error codes.
	if( SUCCEEDED(hr) )
	{
		switch( status )
		{
		// make the status negative so that it propigates as an error (positive #s are just warnings)
		//case U_REGEX_ERROR_START:
		//    hr = Error(_T("ICU returned: U_REGEX_ERROR_START: Start of codes indicating Regexp failures"), __uuidof(IEncConverter), -status);
		//    break;
		case U_ILLEGAL_ARGUMENT_ERROR:
			hr = Error(_T("ICU returned: U_ILLEGAL_ARGUMENT_ERROR"), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_INTERNAL_ERROR:
			hr = Error(_T("ICU returned: U_REGEX_INTERNAL_ERROR: An internal error (bug) was detected."), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_RULE_SYNTAX:
			hr = Error(_T("ICU returned: U_REGEX_RULE_SYNTAX: Syntax error in regexp pattern."), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_INVALID_STATE:
			hr = Error(_T("ICU returned: U_REGEX_INVALID_STATE: RegexMatcher in invalid state for requested operation"), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_BAD_ESCAPE_SEQUENCE:
			hr = Error(_T("ICU returned: U_REGEX_BAD_ESCAPE_SEQUENCE: Unrecognized backslash escape sequence in pattern"), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_PROPERTY_SYNTAX:
			hr = Error(_T("ICU returned: U_REGEX_PROPERTY_SYNTAX: Incorrect Unicode property"), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_UNIMPLEMENTED:
			hr = Error(_T("ICU returned: U_REGEX_UNIMPLEMENTED: Use of regexp feature that is not yet implemented."), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_MISMATCHED_PAREN:
			hr = Error(_T("ICU returned: U_REGEX_MISMATCHED_PAREN: Incorrectly nested parentheses in regexp pattern."), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_NUMBER_TOO_BIG:
			hr = Error(_T("ICU returned: U_REGEX_NUMBER_TOO_BIG: Decimal number is too large."), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_BAD_INTERVAL:
			hr = Error(_T("ICU returned: U_REGEX_BAD_INTERVAL: Error in {min,max} interval"), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_MAX_LT_MIN:
			hr = Error(_T("ICU returned: U_REGEX_MAX_LT_MIN: In {min,max}, max is less than min."), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_INVALID_BACK_REF:
			hr = Error(_T("ICU returned: U_REGEX_INVALID_BACK_REF: Back-reference to a non-existent capture group."), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_INVALID_FLAG:
			hr = Error(_T("ICU returned: U_REGEX_INVALID_FLAG: Invalid value for match mode flags."), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_LOOK_BEHIND_LIMIT:
			hr = Error(_T("ICU returned: U_REGEX_LOOK_BEHIND_LIMIT: Look-Behind pattern matches must have a bounded maximum length."), __uuidof(IEncConverter), -status);
			break;
		case U_REGEX_SET_CONTAINS_STRING:
			hr = Error(_T("ICU returned: U_REGEX_SET_CONTAINS_STRING: Regexps cannot have UnicodeSets containing strings."), __uuidof(IEncConverter), -status);
			break;
		default:
			hr = Error(IDS_NoErrorCode, __uuidof(IEncConverter), -status);
			break;
		};
	}

	return hr;
}

/*
HRESULT CIcuRegexEncConverter::GetAttributeKeys(CComSafeArray<BSTR>& rSa)
{
	// the attributes for this come from ICU, so load it if it isn't already
	HRESULT hr = Load(m_strConverterID);
	if( hr != S_OK )    // can't use "FAILED" since the hrs are ICU codes which don't have the high 'error' bit set.
		return ReturnError(hr);

	// first, add what the ICU people call it (the lhs/rhs might get overwritten below if
	//  this happens to be a script-based transliterator).
	// UnicodeString result;
	if( m_pMatcher )
	{
		// WriteAttributeDefault(rSa,_T("ICU Forward Rules"),strRhsID);
	}

	return S_OK;
}
*/

// replace non printable unicode chars with escape chars
CComBSTR prettify(const UnicodeString &source, UBool parseBackslash);

/*
void CIcuRegexEncConverter::DisplayErrorMsgBox(UParseError parseError, LPCSTR lpID, const CString& strFunc)
{
	// but not if they haven't initialized the parseerror info.
	if( parseError.line >= 0 )
	{
		TCHAR szBuffer[22];
		CString err = _T("ICU FAILURE: ");
		err += strFunc;
		err += _T("() => bad rules, line ");
		err += _itot(parseError.line,szBuffer, 10);
		err += _T(", offset ");
		err += _itot(parseError.offset,szBuffer, 10);
		err += _T(", context ");
		err += prettify(parseError.preContext, TRUE);
		err += _T(", rule(s): ");
		err += prettify(lpID, TRUE);

		CString strCaption = _T("Compilation feedback from ICU for the '");
		strCaption += m_strFriendlyName;
		strCaption += _T("' transliterator");

		MessageBox(GetForegroundWindow(), OLE2T(err), OLE2T(strCaption), MB_ICONEXCLAMATION);
	}
}
*/

HRESULT CIcuRegexEncConverter::Load(const CComBSTR& strConverterSpec)
{
	HRESULT hr = S_OK;

	// but not if it's already loaded.
	if( IsMatcherLoaded() )
		return  hr;

	BOOL    bIgnoreCase = false;
	CString strFind, strReplace;
	if( DeconstructConverterSpec(CString(strConverterSpec),strFind,strReplace,bIgnoreCase) )
	{
		uint32_t flags = 0; // by default
		if( bIgnoreCase )
			flags |= UREGEX_CASE_INSENSITIVE;

		m_strFind.setTo((LPCWSTR)strFind,strFind.GetLength());
		m_strReplace.setTo((LPCWSTR)strReplace,strReplace.GetLength());
		UErrorCode status = U_ZERO_ERROR;
		m_pMatcher = new RegexMatcher(m_strFind, flags, status);

		if( U_FAILURE(status) )
		{
			// the base class does ReturnError and if we do it also, then it inverts the error
			//  code twice
			hr = status;
		}
	}
	else
	{
		CString strError;
		strError.Format(_T("IcuRegex: The converter identifier '%s' is invalid!\n\nUsage: <Find>-><Replace> (<Flags>)*\n\n\twhere <Flags> can be '/i'"), strConverterSpec);
		hr = Error(strError, __uuidof(IEncConverter), ErrStatus_CompilationFailed);
	}

	return hr;
}

// the format of the converter spec for ICU regular expressions is:
//  {Find string}->{Replace} ({flags})
// e.g.
//  "[aeiou]->V /i"
LPCTSTR clpszFindReplaceDelimiter   = _T("->");
LPCTSTR clpszCaseInsensitiveFlag    = _T(" /i");

BOOL DeconstructConverterSpec
(
	const CString&  strConverterSpec,
	CString&        strFind,
	CString&        strReplace,
	BOOL&           bIgnoreCase
)
{
	// split up the converter spec into the pieces:
	//  <Find>-><Replace> (<Flags>)*
	// BUT BEWARE, is there any reason that they couldn't have spaces in there? So instead of using
	//  RegexMatcher::split, do it more carefully
	int nIndex = strConverterSpec.Find(clpszFindReplaceDelimiter);
	if( nIndex == -1 )
		return false;

	// else, strFind is everything before it (including possible spaces)
	strFind = strConverterSpec.Left(nIndex);

	// and strReplace is everything after it (up to a possible final flag)
	nIndex += (int)_tcslen(clpszFindReplaceDelimiter);
	int nLengthStrReplace = strConverterSpec.GetLength() - nIndex;
	if( strConverterSpec.Right((int)_tcslen(clpszCaseInsensitiveFlag)) == clpszCaseInsensitiveFlag )
	{
		bIgnoreCase = true;
		nLengthStrReplace -= (int)_tcslen(clpszCaseInsensitiveFlag);
	}

	strReplace = strConverterSpec.Mid(nIndex,nLengthStrReplace);
	return true;
}

// call to clean up resources when we've been inactive for some time.
void CIcuRegexEncConverter::InactivityWarning()
{
	TRACE(_T("CIcuRegexEncConverter::InactivityWarning\n"));
	FinalRelease();
}

HRESULT CIcuRegexEncConverter::PreConvert
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
	// let the base class do something if it wants.
	HRESULT hr = CEncConverter::PreConvert(eInEncodingForm, eInFormEngine,
									eOutEncodingForm, eOutFormEngine,
									eNormalizeOutput, bForward, IcuInactivityWarningTimeOut);

	// but basically, all transliterators are UTF16;
	eInFormEngine = eOutFormEngine = EncodingForm_UTF16;

	// load the transliterator if it isn't already
	if( !IsMatcherLoaded() )
		hr = Load(m_strConverterID);

	return hr;
}

HRESULT CIcuRegexEncConverter::DoConvert
(
	LPBYTE  lpInBuffer,
	UINT    nInLen,
	LPBYTE  lpOutBuffer,
	UINT&   rnOutLen
)
{
	HRESULT hr = S_OK;
	UnicodeString sInput;
	sInput.setTo((LPCWSTR)lpInBuffer,nInLen / 2);
	m_pMatcher->reset(sInput);

	UErrorCode status = U_ZERO_ERROR;
	UnicodeString sOutput = m_pMatcher->replaceAll(m_strReplace, status);

	if( U_FAILURE(status) )
	{
		// the base class does ReturnError and if we do it also, then it inverts the error
		//  code twice
		hr = status;
	}

	else    //  if( SUCCEEDED(hr) )
	{
		int nLength = sOutput.length() * sizeof(TCHAR);
		if( nLength > (int)rnOutLen )
			hr = Error((LPCTSTR)sOutput.getBuffer(), __uuidof(IEncConverter), ErrStatus_OutputBufferFull);
		else
		{
			rnOutLen = nLength;
			memcpy(lpOutBuffer,sOutput.getBuffer(),rnOutLen);
		}
	}

	return hr;
}
