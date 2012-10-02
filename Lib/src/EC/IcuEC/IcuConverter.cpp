// IcuConverter.cpp : Implementation of CIcuConverter

#include "stdafx.h"
#include "IcuConverter.h"

/////////////////////////////////////////////////////////////////////////////
// CIcuConverter
LPCTSTR clpszIcuConvImplType   = _T("ICU.conv");
LPCTSTR clpszIcuConvProgId     = _T("SilEncConverters31.IcuECConverter.40");

CIcuConverter::CIcuConverter()
  : CEncConverter(clpszIcuConvProgId,clpszIcuConvImplType)  // from IcuConverter.rgs)
  , m_pConverter(0)
  , m_bToWide(false)
{
}

STDMETHODIMP CIcuConverter::Initialize
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
	HRESULT hr = CEncConverter::Initialize(ConverterName, converterSpec, LhsEncodingId, RhsEncodingId, peConversionType, plProcessTypeFlags, CodePageInput, CodePageOutput, bAdding);

	m_strConverterID.ToLower();	// so we can do caseless compares.

	if( bAdding )
	{
		hr = Load(m_strConverterID);
		if( hr != S_OK )
			return ReturnError(hr);

		if(     (m_strStandardName == _T("utf-8"))
			||  (m_strStandardName == _T("utf-16be"))
			||  (m_strStandardName == _T("utf-32be"))
			||  (m_strStandardName == _T("utf-32"))
			)
		{
			// This only works if we consider UTF8 to be unicode; not legacy
			m_eConversionType = ConvType_Unicode_to_from_Unicode;
			if( peConversionType != 0 )
				*peConversionType = m_eConversionType;
		}

		if( IsEmpty(m_strLhsEncodingID) )
		{
			// if the user doesn't give us one, then use the standard name as the default!
			m_strLhsEncodingID = m_strStandardName;
			if( LhsEncodingId != 0 )
				get_LeftEncodingID(LhsEncodingId);
		}

		if( IsEmpty(m_strRhsEncodingID) )
		{
			// I'm not sure this is valid, but if the user doesn't give us one,
			//  then just make it "UNICODE"
			m_strRhsEncodingID = _T("UNICODE");
			if( RhsEncodingId != 0 )
				get_RightEncodingID(RhsEncodingId);
		}

		// make sure, it has the correct process type
		m_lProcessType |= ProcessTypeFlags_ICUConverter;
		if( plProcessTypeFlags != 0 )
			*plProcessTypeFlags = m_lProcessType;

		// I think these are all "Unicode Encoding Conversions" as well (but since the UTFX
		//  flavors aren't, then there may be others that aren't as well, so just leave it
		//  upto the user.
	}

	return hr;
}

HRESULT CIcuConverter::GetAttributeKeys(CComSafeArray<BSTR>& rSa)
{
	// the attributes for this come from ICU, so load it if it isn't already
	HRESULT hr = Load(m_strConverterID);
	if( hr != S_OK )
		return ReturnError(hr);

	WriteAttributeDefault(rSa, _T("ICU Standard Name"), m_strStandardName);

	return S_OK;
}

STDMETHODIMP CIcuConverter::get_ConverterNameEnum(SAFEARRAY* *pVal)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	if (pVal == NULL)
		return E_POINTER;
	*pVal = NULL;

	CComSafeArray<BSTR>* pSa = new CComSafeArray<BSTR>();

	USES_CONVERSION;
	UErrorCode status = U_ZERO_ERROR;
	int nSize = ucnv_countAvailable();
	pSa->Create(nSize);

	for(int i = 0; i < nSize; i++ )
	{
		LPCSTR lpszName = ucnv_getAvailableName(i);
		CComBSTR strName = lpszName;
		int nSizeAliases = ucnv_countAliases(lpszName, &status);

		if( nSizeAliases > 1 )
			strName += _T(" (aliases: ");

		for( int j = 1; U_SUCCESS(status) && (j < nSizeAliases); j++ )
		{
			LPCSTR lpszAlias = ucnv_getAlias(lpszName, (uint16_t)j, &status);
			if( U_SUCCESS(status) )
			{
				strName += lpszAlias;
				strName += _T(" OR ");
			}
		}

		if( nSizeAliases > 1 )
		{
			// first chop off the last " OR "
			CComBSTR str = CComBSTR(strName.Length() - 4,strName);
			strName = str;
			strName += _T(")");
		}

		pSa->SetAt(i,strName);
	}

	*pVal = pSa->Detach();

	return S_OK;
}

HRESULT CIcuConverter::Load(const CComBSTR& strTablePath)
{
	HRESULT hr = S_OK;

	// but not if it's already loaded.
	if( IsFileLoaded() )
		return  hr;

	UErrorCode status = U_ZERO_ERROR;

	// the syntax we use for custom converters is: "filename.dat:<custom converter>"
	//  (e.g. "./testdata.dat:customConverter"). If we get this syntax, then use the
	//  package interface.
	int nIndex = ReverseFind(strTablePath, ':');
	if( nIndex != -1 )
	{
		CComBSTR str, str2;
		str = CComBSTR(nIndex, strTablePath);
		str2 = strTablePath+nIndex+1;
		USES_CONVERSION;
		m_pConverter = ucnv_openPackage(MyOLE2A(str,str.Length()), MyOLE2A(str2,str2.Length()), &status);
	}
	else
		m_pConverter = ucnv_openU(strTablePath, &status);

	if( U_FAILURE(status) )
	{
		hr = status;
	}
	else
	{
		// get a version of the 'standard name' so we can use it in the DefaultUnicodeEncForm
		//  (just in case the user used one of the aliases for the converter identifier)
		LPCSTR pszName = ucnv_getName (m_pConverter, &status);
		USES_CONVERSION;
		m_strStandardName = MyA2OLE(pszName,(int)strlen(pszName));
		m_strStandardName.ToLower();
	}

	return hr;
}

HRESULT CIcuConverter::ReturnError(long status)
{
	HRESULT hr = S_OK;

	switch( status )
	{
	// make the status negative so that it propigates as an error (positive #s are just warnings)
	case U_ILLEGAL_ARGUMENT_ERROR:
		hr = Error(_T("ICU returned: U_ILLEGAL_ARGUMENT_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_MISSING_RESOURCE_ERROR:
		hr = Error(_T("ICU returned: U_MISSING_RESOURCE_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_INVALID_FORMAT_ERROR:
		hr = Error(_T("ICU returned: U_INVALID_FORMAT_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_FILE_ACCESS_ERROR:
		hr = Error(_T("ICU returned: U_FILE_ACCESS_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_INTERNAL_PROGRAM_ERROR:
		hr = Error(_T("ICU returned: U_INTERNAL_PROGRAM_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_MESSAGE_PARSE_ERROR:
		hr = Error(_T("ICU returned: U_MESSAGE_PARSE_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_MEMORY_ALLOCATION_ERROR:
		hr = Error(_T("ICU returned: U_MEMORY_ALLOCATION_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_INDEX_OUTOFBOUNDS_ERROR:
		hr = Error(_T("ICU returned: U_INDEX_OUTOFBOUNDS_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_PARSE_ERROR:
		hr = Error(_T("ICU returned: U_PARSE_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_INVALID_TABLE_FILE:
		hr = Error(_T("ICU returned: U_INVALID_TABLE_FILE"), __uuidof(IEncConverter), -status);
		break;
	case U_BUFFER_OVERFLOW_ERROR:
		hr = Error(_T("ICU returned: U_BUFFER_OVERFLOW_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_UNSUPPORTED_ERROR:
		hr = Error(_T("ICU returned: U_UNSUPPORTED_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_RESOURCE_TYPE_MISMATCH:
		hr = Error(_T("ICU returned: U_RESOURCE_TYPE_MISMATCH"), __uuidof(IEncConverter), -status);
		break;
	case U_ILLEGAL_ESCAPE_SEQUENCE:
		hr = Error(_T("ICU returned: U_ILLEGAL_ESCAPE_SEQUENCE"), __uuidof(IEncConverter), -status);
		break;
	case U_UNSUPPORTED_ESCAPE_SEQUENCE:
		hr = Error(_T("ICU returned: U_UNSUPPORTED_ESCAPE_SEQUENCE"), __uuidof(IEncConverter), -status);
		break;
	case U_NO_SPACE_AVAILABLE:
		hr = Error(_T("ICU returned: U_NO_SPACE_AVAILABLE"), __uuidof(IEncConverter), -status);
		break;
	case U_CE_NOT_FOUND_ERROR:
		hr = Error(_T("ICU returned: U_CE_NOT_FOUND_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_PRIMARY_TOO_LONG_ERROR:
		hr = Error(_T("ICU returned: U_PRIMARY_TOO_LONG_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_STATE_TOO_OLD_ERROR:
		hr = Error(_T("ICU returned: U_STATE_TOO_OLD_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_TOO_MANY_ALIASES_ERROR:
		hr = Error(_T("ICU returned: U_TOO_MANY_ALIASES_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_ENUM_OUT_OF_SYNC_ERROR:
		hr = Error(_T("ICU returned: U_ENUM_OUT_OF_SYNC_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_INVARIANT_CONVERSION_ERROR:
		hr = Error(_T("ICU returned: U_INVARIANT_CONVERSION_ERROR"), __uuidof(IEncConverter), -status);
		break;
	case U_INVALID_CHAR_FOUND:
		hr = Error(IDS_InvalidCharFound, __uuidof(IEncConverter), ErrStatus_InvalidCharFound);
		break;
	case U_TRUNCATED_CHAR_FOUND:
		hr = Error(IDS_TruncatedCharFound, __uuidof(IEncConverter), ErrStatus_TruncatedCharFound);
		break;
	case U_ILLEGAL_CHAR_FOUND:
		hr = Error(IDS_IllegalCharFound, __uuidof(IEncConverter), ErrStatus_IllegalCharFound);
		break;
	case U_INVALID_TABLE_FORMAT:
		hr = Error(IDS_InvalidTableFormat, __uuidof(IEncConverter), ErrStatus_InvalidTableFormat);
		break;
	default:
		// if nothing found, then give the base class a crack at it.
		hr = CEncConverter::ReturnError(status);
		break;
	};

	return hr;
}

HRESULT CIcuConverter::Error(UINT nID, const IID& iid, HRESULT hRes)
{
	return IcuCnvtrComCoClass::Error(nID, iid, hRes);
}

HRESULT CIcuConverter::Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes)
{
	return IcuCnvtrComCoClass::Error(lpszDesc, iid, hRes);
}

EncodingForm CIcuConverter::DefaultUnicodeEncForm(BOOL bForward, BOOL bLHS)
{
	// we can't provide default unicode encoding forms for all possible ICU converters (since
	//  we don't know what they all expect), but we can for the easy ones...
	if( !IsFileLoaded() )
		Load(m_strConverterID);

	EncodingForm eForm = EncodingForm_UTF16;    // default situation
	if( (m_strStandardName == _T("utf-8")) && ( (bLHS && bForward) || !(bLHS || bForward) ) )
		eForm = EncodingForm_UTF8String;
	else if( (m_strStandardName == _T("utf-16be")) && ( (bLHS && bForward) || !(bLHS || bForward) ) )
		eForm = EncodingForm_UTF16BE;
	else if( (m_strStandardName == _T("utf-32be")) && ( (bLHS && bForward) || !(bLHS || bForward) ) )
		eForm = EncodingForm_UTF32BE;
	else if( (m_strStandardName == _T("utf-32")) && ( (bLHS && bForward) || !(bLHS || bForward) ) )
		eForm = EncodingForm_UTF32;
	return eForm;
}

// call to clean up resources when we've been inactive for some time.
void CIcuConverter::InactivityWarning()
{
	TRACE(_T("CIcuConverter::InactivityWarning\n"));
	if( IsFileLoaded() )
		FinalRelease();
}

HRESULT CIcuConverter::PreConvert
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
									eNormalizeOutput, bForward, IcuInactivityWarningTimeOut);

	if( SUCCEEDED(hr) )
	{
		if( !IsFileLoaded() )
			hr = Load(m_strConverterID);

		if( SUCCEEDED(hr) )
		{
			// we need to know whether to go *to* wide or *from* wide for "DoConvert"
			m_bToWide = bForward;
			if( m_bToWide )
			{
				// going "to wide" means the output form required by the engine is UTF16.
				eOutFormEngine = EncodingForm_UTF16;

				// TODO: (TECHNICALLY, we should be doing the following for the other Unicode
				//	formats as well--i.e. if it is the UTF-32 converter, then... what???
				//	is the input UTF16 or is it UTF8? I'm not sure what to do for the other
				//	cases. Maybe we should add additional enums to the 'ConversionType'
				//	enum saying "ConvTypeUTF8_to_from_UTF16" and so on for the 32, etc
				//	flavors... Then we could set the e???FormEngine values correctly.
				if( m_strConverterID == L"utf-8" )
					eInFormEngine = EncodingForm_UTF8Bytes;
			}
			else
			{
				// going "from wide" means the input form required by the engine is UTF16.
				eInFormEngine = EncodingForm_UTF16;

				if( m_strConverterID == L"utf-8" )
					eOutFormEngine = EncodingForm_UTF8Bytes;
			}
		}
	}

	return hr;
}

HRESULT CIcuConverter::DoConvert
(
	LPBYTE  lpInBuffer,
	UINT    nInLen,
	LPBYTE  lpOutBuffer,
	UINT&   rnOutLen
)
{
	UErrorCode  status = U_ZERO_ERROR;
	int32_t     len;

	if( m_bToWide )
	{
		/* Convert from ??? to Unicode */
		len = ucnv_toUChars(m_pConverter, (UChar*)lpOutBuffer, rnOutLen,
					(const char*)lpInBuffer, nInLen, &status);

		rnOutLen = len * sizeof(WCHAR); // calling function expecting # of bytes, but ICU
										// returns # of items
	}
	else
	{
		// ICU is expecting the # of items
		nInLen /= sizeof(WCHAR);

		/* Convert from Unicode to ??? */
		len = ucnv_fromUChars(m_pConverter, (char*)lpOutBuffer, rnOutLen,
					(const UChar*)lpInBuffer, nInLen, &status);

		rnOutLen = len;
	}

	HRESULT hr = S_OK;

	if( U_FAILURE(status) )
	{
		hr = status;
	}

	return hr;
}
