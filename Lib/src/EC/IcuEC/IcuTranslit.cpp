// IcuTranslit.cpp : Implementation of CIcuTranslit CP_SYMBOL

#include "stdafx.h"
#include "IcuTranslit.h"

/////////////////////////////////////////////////////////////////////////////
// CIcuTranslit
LPCTSTR clpszIcuTransImplType   = _T("ICU.trans");
LPCTSTR clpszIcuTransProgId     = _T("SilEncConverters40.IcuECTransliterator.40");

CIcuTranslit::CIcuTranslit()
  : CEncConverter(clpszIcuTransProgId, clpszIcuTransImplType)  // from IcuTranslit.rgs)
  , m_bLTR(false)
  , m_pTForwards(0)
  , m_pTBackwards(0)
{
}

// forward declaration
BOOL IsScriptTransliteratorID
(
	BSTR        TransliteratorID,
	CComBSTR&   strLhsID,
	CComBSTR&   strRhsID
);

// e.g. "Devanagari-Latin"
STDMETHODIMP CIcuTranslit::Initialize
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
		if( FAILED(hr) )
			return hr;
		else if( hr != S_OK )
			return ReturnError(hr);

		// if this is a script transliterator, fill in the unicode range values as well.
		CComBSTR strLhsID, strRhsID;
		if( IsScriptTransliteratorID(m_strConverterID, strLhsID, strRhsID) )
		{
			if( IsEmpty(m_strLhsEncodingID) )
			{
				strLhsID.ToUpper();
				if( strLhsID == _T("ANY") )     // 'any' means Unicode
					strLhsID = _T("UNICODE");
				m_strLhsEncodingID = strLhsID;
				if( LhsEncodingId != 0 )
					get_LeftEncodingID(LhsEncodingId);
			}

			if( IsEmpty(m_strRhsEncodingID) )
			{
				strRhsID.ToUpper();
				if( strRhsID == _T("ANY") )     // 'any' means Unicode
					strRhsID = _T("UNICODE");
				m_strRhsEncodingID = strRhsID;
				if( RhsEncodingId != 0 )
					get_RightEncodingID(RhsEncodingId);
			}
		}

		// make sure, it has the correct process type
		m_lProcessType |= (ProcessTypeFlags_ICUTransliteration | ProcessTypeFlags_Transliteration);
		if( plProcessTypeFlags != 0 )
			*plProcessTypeFlags = m_lProcessType;

		// Load might have adjusted the m_eConversionType
		if( peConversionType != 0 )
			*peConversionType = m_eConversionType;
	}
	return hr;
}

HRESULT CIcuTranslit::ReturnError(long status)
{
	// give the base class first crack at it.
	HRESULT hr = CEncConverter::ReturnError(status);

	// if it couldn't decide what to do, then check our error codes.
	if( SUCCEEDED(hr) )
	{
		switch( status )
		{
		// make the status negative so that it propigates as an error (positive #s are just warnings)
		case U_MALFORMED_RULE:
			hr = Error(_T("ICU returned: U_MALFORMED_RULE"), __uuidof(IEncConverter), -status);
			break;
		case U_MALFORMED_SET:
			hr = Error(_T("ICU returned: U_MALFORMED_SET"), __uuidof(IEncConverter), -status);
			break;
		case U_MALFORMED_SYMBOL_REFERENCE:
			hr = Error(_T("ICU returned: U_MALFORMED_SYMBOL_REFERENCE"), __uuidof(IEncConverter), -status);
			break;
		case U_MALFORMED_UNICODE_ESCAPE:
			hr = Error(_T("ICU returned: U_MALFORMED_UNICODE_ESCAPE"), __uuidof(IEncConverter), -status);
			break;
		case U_MALFORMED_VARIABLE_DEFINITION:
			hr = Error(_T("ICU returned: U_MALFORMED_VARIABLE_DEFINITION"), __uuidof(IEncConverter), -status);
			break;
		case U_MALFORMED_VARIABLE_REFERENCE:
			hr = Error(_T("ICU returned: U_MALFORMED_VARIABLE_REFERENCE"), __uuidof(IEncConverter), -status);
			break;
		case U_MISMATCHED_SEGMENT_DELIMITERS:
			hr = Error(_T("ICU returned: U_MISMATCHED_SEGMENT_DELIMITERS"), __uuidof(IEncConverter), -status);
			break;
		case U_MISPLACED_ANCHOR_START:
			hr = Error(_T("ICU returned: U_MISPLACED_ANCHOR_START"), __uuidof(IEncConverter), -status);
			break;
		case U_MISPLACED_CURSOR_OFFSET:
			hr = Error(_T("ICU returned: U_MISPLACED_CURSOR_OFFSET"), __uuidof(IEncConverter), -status);
			break;
		case U_MISPLACED_QUANTIFIER:
			hr = Error(_T("ICU returned: U_MISPLACED_QUANTIFIER"), __uuidof(IEncConverter), -status);
			break;
		case U_MISSING_OPERATOR:
			hr = Error(_T("ICU returned: U_MISSING_OPERATOR"), __uuidof(IEncConverter), -status);
			break;
		case U_MISSING_SEGMENT_CLOSE:
			hr = Error(_T("ICU returned: U_MISSING_SEGMENT_CLOSE"), __uuidof(IEncConverter), -status);
			break;
		case U_MULTIPLE_ANTE_CONTEXTS:
			hr = Error(_T("ICU returned: U_MULTIPLE_ANTE_CONTEXTS"), __uuidof(IEncConverter), -status);
			break;
		case U_MULTIPLE_CURSORS:
			hr = Error(_T("ICU returned: U_MULTIPLE_CURSORS"), __uuidof(IEncConverter), -status);
			break;
		case U_MULTIPLE_POST_CONTEXTS:
			hr = Error(_T("ICU returned: U_MULTIPLE_POST_CONTEXTS"), __uuidof(IEncConverter), -status);
			break;
		case U_TRAILING_BACKSLASH:
			hr = Error(_T("ICU returned: U_TRAILING_BACKSLASH"), __uuidof(IEncConverter), -status);
			break;
		case U_UNDEFINED_SEGMENT_REFERENCE:
			hr = Error(_T("ICU returned: U_UNDEFINED_SEGMENT_REFERENCE"), __uuidof(IEncConverter), -status);
			break;
		case U_UNDEFINED_VARIABLE:
			hr = Error(_T("ICU returned: U_UNDEFINED_VARIABLE"), __uuidof(IEncConverter), -status);
			break;
		case U_UNQUOTED_SPECIAL:
			hr = Error(_T("ICU returned: U_UNQUOTED_SPECIAL"), __uuidof(IEncConverter), -status);
			break;
		case U_UNTERMINATED_QUOTE:
			hr = Error(_T("ICU returned: U_UNTERMINATED_QUOTE"), __uuidof(IEncConverter), -status);
			break;
		case U_RULE_MASK_ERROR:
			hr = Error(_T("ICU returned: U_RULE_MASK_ERROR"), __uuidof(IEncConverter), -status);
			break;
		case U_MISPLACED_COMPOUND_FILTER:
			hr = Error(_T("ICU returned: U_MISPLACED_COMPOUND_FILTER"), __uuidof(IEncConverter), -status);
			break;
		case U_MULTIPLE_COMPOUND_FILTERS:
			hr = Error(_T("ICU returned: U_MULTIPLE_COMPOUND_FILTERS"), __uuidof(IEncConverter), -status);
			break;
		case U_INVALID_RBT_SYNTAX:
			hr = Error(_T("ICU returned: U_INVALID_RBT_SYNTAX"), __uuidof(IEncConverter), -status);
			break;
		case U_INVALID_PROPERTY_PATTERN:
			hr = Error(_T("ICU returned: U_INVALID_PROPERTY_PATTERN"), __uuidof(IEncConverter), -status);
			break;
		case U_MALFORMED_PRAGMA:
			hr = Error(_T("ICU returned: U_MALFORMED_PRAGMA"), __uuidof(IEncConverter), -status);
			break;
		case U_UNCLOSED_SEGMENT:
			hr = Error(_T("ICU returned: U_UNCLOSED_SEGMENT"), __uuidof(IEncConverter), -status);
			break;
		case U_ILLEGAL_CHAR_IN_SEGMENT:
			hr = Error(_T("ICU returned: U_ILLEGAL_CHAR_IN_SEGMENT"), __uuidof(IEncConverter), -status);
			break;
		case U_VARIABLE_RANGE_EXHAUSTED:
			hr = Error(_T("ICU returned: U_VARIABLE_RANGE_EXHAUSTED"), __uuidof(IEncConverter), -status);
			break;
		case U_VARIABLE_RANGE_OVERLAP:
			hr = Error(_T("ICU returned: U_VARIABLE_RANGE_OVERLAP"), __uuidof(IEncConverter), -status);
			break;
		case U_ILLEGAL_CHARACTER:
			hr = Error(_T("ICU returned: U_ILLEGAL_CHARACTER"), __uuidof(IEncConverter), -status);
			break;
		case U_INTERNAL_TRANSLITERATOR_ERROR:
			hr = Error(_T("ICU returned: U_INTERNAL_TRANSLITERATOR_ERROR"), __uuidof(IEncConverter), -status);
			break;
		case U_INVALID_ID:
			hr = Error(_T("ICU returned: U_INVALID_ID"), __uuidof(IEncConverter), -status);
			break;
		case U_INVALID_FUNCTION:
			hr = Error(_T("ICU returned: U_INVALID_FUNCTION"), __uuidof(IEncConverter), -status);
			break;
		case U_PARSE_ERROR_LIMIT:
			hr = Error(_T("ICU returned: U_PARSE_ERROR_LIMIT"), __uuidof(IEncConverter), -status);
			break;
		default:
			hr = Error(IDS_NoErrorCode, __uuidof(IEncConverter), -status);
			break;
		};
	}

	return hr;
}

BOOL IsScriptTransliteratorID
(
	BSTR        TransliteratorID,
	CComBSTR&   strLhsID,
	CComBSTR&   strRhsID
)
{
	BOOL bRet = false;
	int nIndex = -1;
	if( (nIndex = FindSubStr(TransliteratorID, _T("-"))) != -1 )
	{
		strRhsID = CComBSTR(TransliteratorID + nIndex + 1);
		strLhsID = CComBSTR(nIndex, TransliteratorID);
		bRet = true;
	}

	return bRet;
}

STDMETHODIMP CIcuTranslit::get_ConverterNameEnum(SAFEARRAY* *pVal)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	if (pVal == NULL)
		return E_POINTER;
	*pVal = NULL;

	CComSafeArray<BSTR>* pSa = new CComSafeArray<BSTR>();
	int nSize = Transliterator::countAvailableIDs();
	pSa->Create(nSize);

	USES_CONVERSION;
	for(int i = 0; i < nSize; i++ )
	{
		UnicodeString strID = Transliterator::getAvailableID(i);
		/*
		// according to the ICU webpage, in the new version (3.4), we should be asking for a 'display name'
		//  rather than displaying this 'internal name'. However, there's no way to go from display name back
		//  to internal name (which is what the converter ID must be), so that makes this complicated. So
		//  unless someone complains, continue with showing the internal names (e.g. "Any-Latin" rather than
		//  "Any to Latin").
		UnicodeString strDisplayName;
		Transliterator::getDisplayName(strID, strDisplayName);
		LPCTSTR lpszValue = strDisplayName.getTerminatedBuffer();
		if( _tcslen(lpszValue) == 0 )
			lpszValue = strID.getTerminatedBuffer();
		*/
		CComBSTR strValue = strID.getTerminatedBuffer();
		pSa->SetAt(i,strValue);
	}

	*pVal = pSa->Detach();

	return S_OK;
}

HRESULT CIcuTranslit::GetAttributeKeys(CComSafeArray<BSTR>& rSa)
{
	// the attributes for this come from ICU, so load it if it isn't already
	HRESULT hr = Load(m_strConverterID);
	if( hr != S_OK )    // can't use "FAILED" since the hrs are ICU codes which don't have the high 'error' bit set.
		return ReturnError(hr);

	// first, add what the ICU people call it (the lhs/rhs might get overwritten below if
	//  this happens to be a script-based transliterator).
	CComBSTR strLhsID, strRhsID;
	UnicodeString result;
	if( m_pTForwards )
	{
		m_pTForwards->toRules(result, true);

		// put this *forward* wallah in the *rhs* (so it appears to be the 'destination' of conversion
		strRhsID = result.getTerminatedBuffer();

		WriteAttributeDefault(rSa,_T("ICU Forward Rules"),strRhsID);
	}
	if( m_pTBackwards )
	{
		m_pTBackwards->toRules(result, true);

		// put this *reverse* wallah in the *lhs* (so it appears to be the 'source' of conversion
		strLhsID = result.getTerminatedBuffer();
		WriteAttributeDefault(rSa,_T("ICU Reverse Rules"),strLhsID);
	}

	return S_OK;
}

// replace non printable unicode chars with escape chars
CString prettify(const UnicodeString &source, UBool parseBackslash);

CString CIcuTranslit::DisplayErrorMsgBox(UParseError parseError, LPCSTR lpID, const CString& strFunc)
{
	CString strErrRet;

	// but not if they haven't initialized the parseerror info.
	if( parseError.line >= 0 )
	{
		TCHAR szBuffer[22];
		CString err = _T("ICU FAILURE: ");
		err += strFunc;
		err += _T("() => bad rules, line ");
		_itot_s(parseError.line,szBuffer, 10);
		err += szBuffer;
		err += _T(", offset ");
		_itot_s(parseError.offset,szBuffer, 10);
		err += szBuffer;
		err += _T(", context ");
		err += prettify(parseError.preContext, TRUE);
		err += _T(", rule(s): ");
		err += prettify(lpID, TRUE);

		strErrRet.Format(_T("Compilation feedback from ICU for the '%s' transliterator:\n\n%s"), m_strFriendlyName, err);
	}

	// COM servers *return* errors; not display them...
	// MessageBox(GetForegroundWindow(), err, strCaption, MB_ICONEXCLAMATION);
	return strErrRet;
}

HRESULT CIcuTranslit::Load(const CComBSTR& strConverterSpec)
{
	HRESULT hr = S_OK;

	// but not if it's already loaded.
	if( IsFileLoaded() )
		return  hr;

	USES_CONVERSION;
	UErrorCode status = U_ZERO_ERROR;
	UParseError parseError = { -1, 0, 0, 0};
	LPCSTR lpID = MyOLE2A(strConverterSpec,strConverterSpec.Length());

	// being pessimistic...
	m_eConversionType = ConvType_Unicode_to_Unicode;

	// it's not so clear what is a "rule-based" converter and what isn't. So just try to create it from
	//  the normal approach first and if that fails, then try the createFromRules
	m_pTForwards = Transliterator::createInstance(lpID, UTRANS_FORWARD, parseError, status);

	if( !m_pTForwards ) // IsRuleBased(strConverterSpec) )
	{
		UErrorCode statusFromRules = U_ZERO_ERROR;
		m_pTForwards = Transliterator::createFromRules(MyOLE2A(m_strFriendlyName,m_strFriendlyName.Length()),
											lpID, UTRANS_FORWARD, parseError, statusFromRules);
	}

	// if the forward direction worked, see if there's a reversable one.
	if( m_pTForwards )
	{
		// use a different status variable since we don't *really* care if the reverse is possible
		//  (i.e. even if it fails, we wouldn't want to return an error).
		UErrorCode statusRev = U_ZERO_ERROR;
		m_pTBackwards = m_pTForwards->createInverse(statusRev);

		if( m_pTBackwards )
		{
			m_eConversionType = ConvType_Unicode_to_from_Unicode;
		}
	}
	else
	{
		return Error(DisplayErrorMsgBox(parseError, lpID, _T("createInstance failed (but createFromRules also failed)")), __uuidof(IEncConverter), ErrStatus_CompilationFailed);
	}

	if( U_FAILURE(status) )
	{
		hr = status;
	}

	return hr;
}

// call to clean up resources when we've been inactive for some time.
void CIcuTranslit::InactivityWarning()
{
	TRACE(_T("CIcuTranslit::InactivityWarning\n"));
	FinalRelease();
}

HRESULT CIcuTranslit::PreConvert
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
	// pass it a "give me a warning when I haven't been used in 60 seconds" request
	HRESULT hr = CEncConverter::PreConvert(eInEncodingForm, eInFormEngine,
									eOutEncodingForm, eOutFormEngine,
									eNormalizeOutput, bForward, IcuInactivityWarningTimeOut);

	// but basically, all transliterators are UTF16;
	eInFormEngine = eOutFormEngine = EncodingForm_UTF16;

	// keep track of which direction (can't use m_bForward as this may have come in from
	//	ConvertEx and be different than that).
	m_bLTR = bForward;

	// load the transliterator if it isn't already
	if( !IsFileLoaded() )
		hr = Load(m_strConverterID);

	return hr;
}

HRESULT CIcuTranslit::DoConvert
(
	LPBYTE  lpInBuffer,
	UINT    nInLen,
	LPBYTE  lpOutBuffer,
	UINT&   rnOutLen
)
{
	HRESULT hr = S_OK;
	UnicodeString result;
	result.setTo((LPCWSTR)lpInBuffer,nInLen / 2);

	if( m_bLTR )
	{
		if( m_pTForwards )
		{
			m_pTForwards->transliterate(result);
		}
		else
		{
			hr = ErrStatus_NoAvailableConverters;
		}
	}
	else	// !m_bLTR
	{
		if( m_pTBackwards )
		{
			m_pTBackwards->transliterate(result);
		}
		else
		{
			hr = ErrStatus_NoAvailableConverters;
		}
	}

	if( SUCCEEDED(hr) )
	{
		int nLen = result.length() * 2;
		if( nLen > (int)rnOutLen )
			hr = Error((LPCTSTR)result.getBuffer(), __uuidof(IEncConverter), ErrStatus_OutputBufferFull);
		else
		{
			rnOutLen = nLen;
		memcpy(lpOutBuffer,result.getBuffer(),rnOutLen);
		}
	}

	return hr;
}

HRESULT CIcuTranslit::Error(UINT nID, const IID& iid, HRESULT hRes)
{
	return IcuTranslitComCoClass::Error(nID, iid, hRes);
}

HRESULT CIcuTranslit::Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes)
{
	return IcuTranslitComCoClass::Error(lpszDesc, iid, hRes);
}

// Append a hex string to the target
UnicodeString& appendHex(uint32_t number,
			int32_t digits,
			UnicodeString& target)
{
	static const UChar digitString[] = {
		0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
		0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0
	}; /* "0123456789ABCDEF" */

	switch (digits)
	{
	case 8:
		target += digitString[(number >> 28) & 0xF];
	case 7:
		target += digitString[(number >> 24) & 0xF];
	case 6:
		target += digitString[(number >> 20) & 0xF];
	case 5:
		target += digitString[(number >> 16) & 0xF];
	case 4:
		target += digitString[(number >> 12) & 0xF];
	case 3:
		target += digitString[(number >>  8) & 0xF];
	case 2:
		target += digitString[(number >>  4) & 0xF];
	case 1:
		target += digitString[(number >>  0) & 0xF];
		break;
	default:
		target += "**";
	}
	return target;
}

// Replace nonprintable characters with unicode escapes
CString prettify(const UnicodeString &source, UBool parseBackslash)
{
	int32_t i;
	UnicodeString target;
	target.remove();
	target += "\"";

	for (i = 0; i < source.length();)
	{
		UChar32 ch = source.char32At(i);
		i += UTF_CHAR_LENGTH(ch);

		if (ch < 0x09 || (ch > 0x0A && ch < 0x20)|| ch > 0x7E)
		{
			if (parseBackslash) {
				// If we are preceded by an odd number of backslashes,
				// then this character has already been backslash escaped.
				// Delete a backslash.
				int32_t backslashCount = 0;
				for (int32_t j=target.length()-1; j>=0; --j) {
					if (target.charAt(j) == (UChar)92) {
						++backslashCount;
					} else {
						break;
					}
				}
				if ((backslashCount % 2) == 1) {
					target.truncate(target.length() - 1);
				}
			}
			if (ch <= 0xFFFF) {
				target += "\\u";
				appendHex(ch, 4, target);
			} else {
				target += "\\U";
				appendHex(ch, 8, target);
			}
		}
		else
		{
			target += ch;
		}
	}

	target += "\"";

	return CString(target.getTerminatedBuffer());
}

// rule-based transliterators have either "::" or "<" or ">" OR ";" (I think)
//  or NOT "-"
BOOL IsRuleBased(const CComBSTR& strConverterSpec)
{
	return ((FindSubStr(strConverterSpec, _T("::")) != -1)
		||  (FindOneOf(strConverterSpec, _T("><;")) != -1)
		||  (FindOneOf(strConverterSpec, _T("-")) == -1)    // e.g. "null" apparently is a good rule-based transliterator
		);
}