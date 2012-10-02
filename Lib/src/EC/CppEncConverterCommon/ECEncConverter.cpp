// ECEncConverter.cpp
//	This file contains implementations of some of the bigger functions in ECEncConverter.h
#include "stdafx.h"
#include "ECEncConverter.h"
#include "AutoConfigSheet.h"

#define v22_AllowEmptyReturn    // turn off the code that disallowed empty returns

CECEncConverter<IEncConverter>::CECEncConverter(LPCTSTR lpszProgramID, LPCTSTR lpszImplementType)	// sub-class gives its progid
  : m_strProgramID(lpszProgramID)
  , m_strImplementType(lpszImplementType)
  , m_lProcessType(ProcessTypeFlags_DontKnow)
  , m_eConversionType(ConvType_Unknown)
  , m_bForward(true)
  , m_eEncodingInput(EncodingForm_Unspecified)
  , m_eEncodingOutput(EncodingForm_Unspecified)
  , m_eNormalizeOutput(NormalizeFlags_None)
  , m_bDebugDisplayMode(VARIANT_FALSE)
  , m_nCodePageInput(0)
  , m_nCodePageOutput(0)
  , m_bInitialized(false)
  , m_bIsInRepository(VARIANT_FALSE)
{
}

HRESULT CEncConverter::get_AttributeKeys(SAFEARRAY* *pVal)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	if( pVal == NULL )
		return E_POINTER;

	// we keep track of the key value pairs for subsequent calls to getAttributeValue
	//  (in the map, m_mapProperties), so first clear it out in case it was previously
	//  filled
	m_mapProperties.clear();

	// next create the safearray that the subclass will fill and ask the subclass to
	//  fill it.
	CComSafeArray<BSTR>* pSa = new CComSafeArray<BSTR>();
	HRESULT hr = GetAttributeKeys(*pSa);

	if( SUCCEEDED(hr) && ((LPSAFEARRAY)(*pSa) != 0) )
		*pVal = pSa->Detach();
	else
	{
		*pVal = 0;
		delete pSa;
	}

	return hr;
}

HRESULT CEncConverter::AttributeValue(BSTR sKey, BSTR* pVal)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	if( pVal == NULL )
		return E_POINTER;

	// maybe the user didn't do "get_AttributeKeys" if they know what the keys are
	if( m_mapProperties.empty() )
	{
		// create a dummy safe array that the subclass will fill and ask the subclass to
		//  fill it (so it'll also initialize the map of attribute values.
		CComSafeArray<BSTR> rSa;
		GetAttributeKeys(rSa);
	}

	// we should have filled the m_mapProperties map with the attribute values during
	//  GetAttributeKeys above, so now just look them up
	CComBSTR strValue = m_mapProperties[CComBSTR(sKey)];
	*pVal = strValue.Copy();
	return S_OK;
}

HRESULT CEncConverter::Initialize(BSTR ConverterName, BSTR ConverterIdentifier, BSTR* LhsEncodingID, BSTR* RhsEncodingID, ConvType* peConversionType, long* ProcessTypeFlags, long CodePageInput, long CodePageOutput, VARIANT_BOOL bAdding)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	m_bInitialized = true;
	m_strFriendlyName = ConverterName;
	m_strConverterID = ConverterIdentifier;
	m_strLhsEncodingID = (LhsEncodingID) ? *LhsEncodingID : _T("");
	m_strRhsEncodingID = (RhsEncodingID) ? *RhsEncodingID : _T("");
	m_eConversionType = (peConversionType) ? *peConversionType : ConvType_Unknown;
	m_lProcessType = (ProcessTypeFlags) ? *ProcessTypeFlags : ProcessTypeFlags_DontKnow;
	m_nCodePageInput = CodePageInput;
	m_nCodePageOutput = CodePageOutput;

	// some specs have bad things in them (as far as the .Net File classes are concerned)
	if( FindSubStr(m_strConverterID,_T("file::///")) != -1 )
		m_strConverterID = &ConverterIdentifier[9];

	return S_OK;
}

HRESULT CEncConverter::get_Configurator(IEncConverterConfig* *pECConfig)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	if( pECConfig == NULL )
		return E_POINTER;

	IEncConverterConfig* pConfigurator = 0;
	HRESULT hr = GetConfigurator(&pConfigurator);
	if( SUCCEEDED(hr) )
	{
		PtrIEncConverterConfig rConfigurator(pConfigurator);

		// whether it's initialized or not, we still need to set the parent EncConverter reference
		rConfigurator->putref_ParentEncConverter(this);

		if( m_bInitialized )
		{
			rConfigurator->put_ConverterFriendlyName(m_strFriendlyName);
			rConfigurator->put_ConverterIdentifier(m_strConverterID);
			rConfigurator->put_LeftEncodingID(m_strLhsEncodingID);
			rConfigurator->put_RightEncodingID(m_strRhsEncodingID);
			rConfigurator->put_ConversionType(m_eConversionType);
			rConfigurator->put_ProcessType(m_lProcessType);
			rConfigurator->put_IsInRepository(m_bIsInRepository);
		}

		*pECConfig = rConfigurator.Detach();
	}

	return hr;
};

HRESULT CEncConverter::ConvertToUnicode(/*[in]*/ SAFEARRAY* pbaInput, /*[out]*/ BSTR* sOutput)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	if(     (m_eConversionType != ConvType_Legacy_to_from_Unicode)
		&&  (m_eConversionType != ConvType_Unicode_to_from_Legacy)
		&&  (m_eConversionType != ConvType_Legacy_to_Unicode)
		)
	{
		ReturnError(ErrStatus_InvalidConversionType);
	}

	BOOL bForward = !(m_eConversionType == ConvType_Unicode_to_from_Legacy);

	// since 'InternalConvert' is expecting a BSTR, convert the given safearray
	//  to a BSTR and set the input encoding form as LegacyBytes.
	//  (not as efficent as adding a new InternalConvertToUnicode which takes a
	//  safearray instead, but a) that would require a lot of changes which I'm
	//  afraid would break something, and b) this is far more maintainable).
	CComSafeArray<byte> sa(pbaInput);
	if( sa.GetType() != VT_UI1 )
		return COR_E_SAFEARRAYTYPEMISMATCH;

	int nLen = sa.GetCount();

	// now put it in the BSTR for the call to InternalConvert
	long ciOutput = 0;
	CComBSTR sInput((nLen+1)/2,(LPCOLESTR)(LPCSTR)((LPSAFEARRAY)sa)->pvData);
	return InternalConvertEx(EncodingForm_LegacyBytes, sInput, nLen, EncodingForm_UTF16, m_eNormalizeOutput, sOutput, &ciOutput, bForward);
}

HRESULT CEncConverter::ConvertFromUnicode(/*[in]*/ BSTR sInput, /*[out]*/ SAFEARRAY* *pbaOutput)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	*pbaOutput = 0;

	if(     (m_eConversionType != ConvType_Legacy_to_from_Unicode)
		&&  (m_eConversionType != ConvType_Unicode_to_from_Legacy)
		&&  (m_eConversionType != ConvType_Unicode_to_Legacy)
		)
	{
		ReturnError(ErrStatus_InvalidConversionType);
	}

	BOOL bForward = !(m_eConversionType == ConvType_Legacy_to_from_Unicode);

	// similarly as above, use the normal 'InternalConvert' which is expecting to
	//  return a string, and then convert it to a byte [].
	long ciOutput = 0;
	CComBSTR sOutput;
	HRESULT hr = InternalConvertEx(EncodingForm_UTF16, sInput, 0, EncodingForm_LegacyBytes, m_eNormalizeOutput, &sOutput, &ciOutput, bForward);
	if( SUCCEEDED(hr) )
	{
		CComSafeArray<byte>* pSa = new CComSafeArray<byte>(ciOutput);
		ATLASSERT(pSa->GetType() == VT_UI1);
		LPSAFEARRAY lpsa = (LPSAFEARRAY)(*pSa);
		if( lpsa != 0 )
		{
			LPCSTR lpData = (LPCSTR)(LPCOLESTR)sOutput;
			memcpy(lpsa->pvData,lpData,ciOutput);
			*pbaOutput = pSa->Detach();
		}
		else
			delete pSa;
	}

	return hr;
}

HRESULT CEncConverter::Equals(VARIANT rhs, VARIANT_BOOL *bEqual)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	if( bEqual == NULL )
		return E_POINTER;

	// I'm not sure if this is what they are getting at by the function in .Net, but
	//  it seems to me that these can be equal only if they have the same name.
	CComVariant var(rhs);
	if( ((var.vt == VT_DISPATCH) || (var.vt == VT_UNKNOWN)) && (*var.ppdispVal != NULL) )
	{
		CComPtr<IEncConverter> pIE;
		(*var.ppdispVal)->QueryInterface(__uuidof(IEncConverter),(LPVOID*)&pIE);
		if( !!pIE )
		{
			CComBSTR strRhsName;
			pIE->get_Name(&strRhsName);
			if( strRhsName == m_strFriendlyName )
			{
				*bEqual = -1;   // true
				return S_OK;
			}
		}
	}

	*bEqual = 0;    // false
	return S_OK;
}

HRESULT CEncConverter::FinishDebugDisplay(LPCTSTR lpszBufOutput, LPCTSTR lpszCaption)
{
	int nBufLenBytes = (int)(_tcslen(lpszBufOutput) + 1) * sizeof(TCHAR);

	// use dynamic memory, because certain clients (e.g. Word) don't have enough stack space for us to us alloca
	int nAllocLen = nBufLenBytes + 100;
	LPTSTR lpszMsgBuf = new TCHAR[nAllocLen + 1];
	_stprintf_s(lpszMsgBuf, nAllocLen, _T("Character values: %s\n\nCopy to the Clipboard?"), lpszBufOutput);

	HWND hWnd = GetForegroundWindow();
	int nRet = ::MessageBox(hWnd, lpszMsgBuf, lpszCaption, MB_YESNOCANCEL | MB_SYSTEMMODAL);
	delete lpszMsgBuf;

	if( nRet == IDYES )
	{
		// Open the clipboard, and empty it.
		if( ::OpenClipboard(hWnd) )
		{
			EmptyClipboard();
			HGLOBAL hGlobal = GlobalAlloc(GMEM_MOVEABLE, nBufLenBytes + sizeof(TCHAR));
			if( hGlobal != 0 )
			{
				_tcscpy_s((LPTSTR)GlobalLock(hGlobal), nBufLenBytes / 2, lpszBufOutput);
				GlobalUnlock(hGlobal);
				SetClipboardData(CF_UNICODETEXT, hGlobal);
			}
			CloseClipboard();
		}
		else
			return S_FALSE;
	}
	else if( nRet == IDCANCEL )
	{
		m_bDebugDisplayMode = false;
	}

	return S_OK;
}

HRESULT CEncConverter::ReturnUCharValues(LPCWSTR szInputString, int nLengthWords, LPCTSTR lpszCaption)
{
	int nLen = lstrlenW(szInputString);

	// use dynamic memory, because certain clients (e.g. Word) don't have enough stack space for us to us alloca
	int nAllocLen = (nLen+1)*20*sizeof(TCHAR);
	LPTSTR lpszBufOutput = new TCHAR[nAllocLen + 1];
	lpszBufOutput[0] = 0;

	const int nBufLen = 100;
	TCHAR szBuf[nBufLen + 1];
	nAllocLen /= 2; // size in words for the following usage
	for( int i = 0; i < nLen; i++ )
	{
		if( szInputString[i] == 0 )
		{
			_tcscat_s( lpszBufOutput, nAllocLen, _T("nul (u0000)  ") );
		}
		else
		{
			_stprintf_s( szBuf, _T("%c (u%04x) "), szInputString[i], szInputString[i]);
			_tcscat_s( lpszBufOutput, nAllocLen, szBuf );
		}
	}

	HRESULT hr = FinishDebugDisplay(lpszBufOutput, lpszCaption);
	delete lpszBufOutput;
	return hr;
}

HRESULT CEncConverter::ReturnCharValues(LPCSTR lpszInputString, int nLengthBytes, LPCTSTR lpszCaption)
{
	int nLen = (int)strlen(lpszInputString);

	// use dynamic memory, because certain clients (e.g. Word) don't have enough stack space for us to us alloca
	int nAllocLen = (nLen+1)*5*sizeof(TCHAR);
	LPTSTR lpszBufOutput = (LPTSTR)new char[nAllocLen + 1];
	lpszBufOutput[0] = 0;

	TCHAR szBuf[10];
	nAllocLen /= 2; // size in words for the following usage
	for( int i = 0; i < nLen; i++ )
	{
		_stprintf_s( szBuf, _T("d%d "), lpszInputString[i] & 0xFF );
		_tcscat_s( lpszBufOutput, nAllocLen, szBuf );
	}

	HRESULT hr = FinishDebugDisplay(lpszBufOutput, lpszCaption);
	delete lpszBufOutput;
	return hr;
}

HRESULT CEncConverter::ReturnUCharValuesFromUTF8(LPCSTR lpszInputString, int nLengthBytes, LPCTSTR lpszCaption)
{
	USES_CONVERSION;
	LPCWSTR lpDestStr = MyU82CW(lpszInputString, nLengthBytes);
	if( lpDestStr )
	{
		return ReturnUCharValues(lpDestStr,nLengthBytes,lpszCaption);
	}
	else
	{
		HWND hWnd = GetForegroundWindow();
		::MessageBox(hWnd, _T("Out of memory!"), lpszCaption, MB_OK | MB_ICONEXCLAMATION | MB_SYSTEMMODAL );
	}

	return S_FALSE;
}

// Since each sub-class has to do basic input/output encoding format processing, they
//	should all mostly come thru this and the next functions.
HRESULT CEncConverter::InternalConvert
(
	EncodingForm    eInEncodingForm,
	BSTR			sInput,
	EncodingForm    eOutEncodingForm,
	NormalizeFlags  eNormalizeOutput,
	BSTR*           sOutput,
	BOOL            bForward
)
{
	// this routine is only called by one of the 'implicit' methods (e.g.
	//	ConvertToUnicode). For these "COM standard" methods, the length of the string
	//	is specified by the BSTR itself and always/only supports UTF-16-like (i.e. wide)
	//	data. So, pass 0 so that the function will determine the length from the BSTR
	//	itself (just in case the user happens to have a value of 0 in the data (i.e.
	//	it won't necessarily be null terminated...
	HRESULT hr = InternalConvertEx
					(
						eInEncodingForm,
						sInput,
						0,
						eOutEncodingForm,
						eNormalizeOutput,
						sOutput,
						0,
						bForward
					);

	return hr;
}

// keep a minimum of 16 pages to handle overflow exceptions
const long STACK_RESERVED_SPACE = 4096 * 16;

static bool CheckForStackSpace(long bytes)
{
	MEMORY_BASIC_INFORMATION stackInfo;
	int* currentAddr = (int*)&stackInfo - 4096;
	VirtualQuery(currentAddr, &stackInfo, sizeof(stackInfo));

	return (currentAddr - (int*)stackInfo.AllocationBase) > (bytes + STACK_RESERVED_SPACE);
}

// This function is the meat of the conversion process. It is really long, which
//	normally wouldn't be a virtue (especially as an "in-line" function), but in an
//	effort to save memory fragmentation by using stack memory to buffer the input
//	and output data, I'm using the alloca memory allocation function. Because of this
//	it can't be allocated in some subroutine and returned to a calling program (or the
//	stack will have erased them), so it has to be one big fat long function...
//	The basic structure is:
//
//	o	Check Input Data
//	o	Give the sub-class (via PreConvert) the opportunity to load tables and do
//		any special preprocessing it needs to ahead of the actual conversion
//	o	Possibly call the TECkit COM interface to convert Unicode flavors that the
//		engine (for this conversion) might not support (indicated via PreConvert)
//	o	Normalize the input data to a byte array based on it's input EncodingForm
//	o		Allocate (on the stack) a buffer for the output data (min 10000 bytes)
//	o		Call the subclass (via DoConvert) to do the actual conversion.
//	o	Normalize the output data to match the requested output EncodingForm (including
//		possibly calling the TECkit COM interface).
//	o	Return the resultant BSTR and size of items to the output pointer variables.
//
template<> HRESULT CEncConverter::InternalConvertEx
(
	EncodingForm    eInEncodingForm,
	BSTR			sInput,
	long            ciInput,
	EncodingForm    eOutEncodingForm,
	NormalizeFlags  eNormalizeOutput,
	BSTR*           sOutput,
	long*           pciOutput,
	BOOL            bForward
)
{
	if( !sInput )
		return ReturnError(ErrStatus_IncompleteChar);

	// if the user hasn't specified, then take the default case for the ConversionType:
	//  if L/RHS == eLegacy, then LegacyString
	//  if L/RHS == eUnicode, then UTF16
	HRESULT hr = CheckInitEncForms
					(
						bForward,
						eInEncodingForm,
						eOutEncodingForm
					);
	if( FAILED(hr) )
		return hr;

	// allow the converter engine's (and/or its COM wrapper) to do some preprocessing.
	EncodingForm eFormEngineIn, eFormEngineOut;
	hr = PreConvert
			(
				eInEncodingForm,	// [in] form in the BSTR
				eFormEngineIn,		// [out] form the conversion engine wants, etc.
				eOutEncodingForm,
				eFormEngineOut,
				eNormalizeOutput,
				bForward
			);

	if( hr != S_OK )
		return ReturnError(hr);

	// make a copy so that we can muck with it (e.g. CC needs to pad with spaces)
	int			nLenInput = SysStringLen(sInput);
	CComBSTR	bstrInput(nLenInput,sInput);
	LPOLESTR	lpszInput = bstrInput;

	// check to see if the engine can handle the given input form. If not, then ask
	//	TEC to do the conversion for us so that all engines can handle all possible
	//	input encoding forms (except if it's legacy, which gets handled below)
	if( (eInEncodingForm != eFormEngineIn) && !bIsLegacyFormat(eInEncodingForm) )
	{
		// we can do some of the conversions ourself. For example, if the input form
		//	is UTF16 and the desired form is UTF8, then simply use CCUnicode8 below
		if( (eInEncodingForm == EncodingForm_UTF16) && (eFormEngineIn == EncodingForm_UTF8Bytes) )
		{
			eInEncodingForm = (EncodingForm)CCUnicode8;
		}
		// we can also do the following one
		else if( (eInEncodingForm == EncodingForm_UTF8String) && (eFormEngineIn == EncodingForm_UTF8Bytes) )
		{
			; // i.e. don't have TECkit do this one...
		}
		else if( SUCCEEDED(HaveTECConvert(bstrInput,eInEncodingForm,ciInput,eFormEngineIn,eNormalizeOutput,bstrInput,ciInput)) )
		{
			lpszInput = bstrInput;
			nLenInput = bstrInput.Length();
			eInEncodingForm = eFormEngineIn;
		}
	}

	// convert the input data to a byte string (for Legacy) or a just dereference
	//	for Unicode forms).
	USES_CONVERSION;
	LPBYTE  lpInBuffer = 0;
	UINT    nInLen = 0;

	// when we were initialized, the 'input' code page corresponded to the code page used
	//  by the font on the lhs. This will be backwards if we running the table in reverse:
	int nCodePageInput = ((bForward) ? m_nCodePageInput : m_nCodePageOutput);
	int nCodePageOutput = ((bForward) ? m_nCodePageOutput : m_nCodePageInput);
	try
	{
		// Since we use stack space for the memory allocations to do the conversion, see if we have a reasonable
		//  chance of having stack space enough to do this.
		// The '10' comes from the roughly 4x that could come from one of the convert macros below (e.g. T2A)
		//  and 6x for the possible output. But otherwise, this is just a guess.
		// If some client absolutely must do this with larger units, then we'll have to switch to the new
		//  conversion macros (e.g. CW2A), which will allocate dynamic memory... but I just don't think it's worth
		//  it at this point.
		if (!CheckForStackSpace(nLenInput * 10))
			AtlThrow( E_OUTOFMEMORY );

		if( (eInEncodingForm == EncodingForm_LegacyBytes) || (eInEncodingForm == EncodingForm_UTF8Bytes) )
		{
			// these forms are for C++ apps that want to use the BSTR to transfer bytes rather
			//  than OLECHARs.
			lpInBuffer = (LPBYTE)lpszInput;

			if( ciInput != 0 )
			{
				nInLen = ciInput; // item count should be the number of bytes directly.
			}
			else
			{
				// if the user didn't give the length (i.e. via ConvertEx), then get it
				//	from the BSTR length. nInLen will be the # of bytes.
				nInLen = nLenInput * sizeof(OLECHAR);

				// dilemma: it's possible that the user didn't make it an even number of
				//	bytes, in which case, this is actually one more than the actual
				//	length. otoh, apparently it is possible for this data to have '00'
				//	as a legitimate value (don't ask). At the very least check if the
				//	last byte is zero and if so, then reduce the count by one...
				if( (lpInBuffer[nInLen-1] == 0) && (lpInBuffer[nInLen-2] != 0) )
				{
					// only the last byte was zero, so assume it was an odd count
					nInLen--;
				}
				// otherwise, either the data has zeros in it, or the user screwed up and I have
				//  no choice but to go with what I've got.
			}

			if( m_bDebugDisplayMode )
			{
				if( eInEncodingForm == EncodingForm_LegacyBytes )
					ReturnCharValues((LPCSTR)lpInBuffer, nInLen, _T("Received (LegacyBytes) from client and sending to Converter/DLL..."));
				else
					ReturnUCharValuesFromUTF8((LPCSTR)lpInBuffer, nInLen, _T("Received (UTF8Bytes) from client and sending to Converter/DLL..."));
			}
		}
		else if( eInEncodingForm == EncodingForm_LegacyString )
		{
			if( ciInput != 0 )
			{
				nInLen = ciInput;   // item count should be the number of bytes directly (after conversion below).
			}
			else
			{
				nInLen = nLenInput;
			}

			if( m_bDebugDisplayMode )
				ReturnUCharValues(lpszInput, nInLen, _T("Received (LegacyString) from client..."));

			// first check if it's a symbol font (sometimes the user incorrectly sends
			//  few spaces first, so check the first couple of bytes.
			if(     (   (nCodePageInput == CP_ACP)
					||  (nCodePageInput == CP_THREAD_ACP)
					)
				&&  (
						((lpszInput[0] & 0xF000) == 0xF000)
					||  ((nInLen > 1) && ((lpszInput[1] & 0xF000) == 0xF000))
					||  ((nInLen > 2) && ((lpszInput[2] & 0xF000) == 0xF000))
					)
				)
			{
				nCodePageInput = CP_SYMBOL;
			}

			// if it's a symbol or iso-8859 encoding, then we can handle just
			//  taking the low byte (i.e. the catch case)
			const int CP_ISO_8859 = 28591;
			if(     (nCodePageInput == CP_ISO_8859)
				||  (nCodePageInput == CP_SYMBOL)
				)
			{
				_acp = nCodePageInput;
				lpInBuffer = (LPBYTE)MyW2S(lpszInput, nInLen);

				// on Win9x, this might not work, so if it didn't, then convert
				//	it manually
				if( strlen((LPCSTR)lpInBuffer) == 0 )
				{
					unsigned int i = 0;
					for( ; i < nInLen; i++ )
						lpInBuffer[i] = (char)lpszInput[i];

					// terminate it
					lpInBuffer[i] = 0;
				}
			}
			else
			{
				// otherwise, simply use CP_ACP (or the default code page) to
				//	narrowize it.
				_acp = nCodePageInput;
				lpInBuffer = (LPBYTE)MyOLE2A(lpszInput,nInLen);
			}

			if( m_bDebugDisplayMode )
				ReturnCharValues((LPCSTR)lpInBuffer, nInLen, _T("Sending (LegacyBytes) to Converter/DLL..."));
		}
		else if( eInEncodingForm == EncodingForm_UTF8String )
		{
			if( ciInput != 0 )
			{
				nInLen = ciInput;   // item count should be the number of bytes directly (after conversion below).
			}
			else
			{
				nInLen = nLenInput;
			}

			if( m_bDebugDisplayMode )
				ReturnUCharValues(lpszInput, nInLen, _T("Received (UTF8String) from client..."));

			// this is UTF8 narrow data in a wide BSTR, so use the CP_ACP to narrowize it
			lpInBuffer = (LPBYTE)MyOLE2A(lpszInput, nInLen);

			if( m_bDebugDisplayMode )
				ReturnUCharValuesFromUTF8((LPCSTR)lpInBuffer, nInLen, _T("Sending (UTF8Bytes) to Converter/DLL..."));
		}
		// this is a special case for CC where the input was actually UTF16, but the
		//	CC DLL is expecting (usually) UTF8, so convert from UTF16->UTF8 narrow
		else if( eInEncodingForm == CCUnicode8 )
		{
			if( ciInput != 0 )
			{
				nInLen = ciInput;   // item count should be the number of bytes directly (after conversion below).
			}
			else
			{
				nInLen = nLenInput;
			}

			if( m_bDebugDisplayMode )
				ReturnUCharValues(lpszInput, nInLen, _T("Received (UTF16) from client..."));

#ifndef	OldCCU8Method
			// this way helps us determine the length of the resultant string
			int rnOutLen = (nInLen+1)*4;
			lpInBuffer = (LPBYTE)alloca(rnOutLen);
			nInLen = WideCharToMultiByte(CP_UTF8, 0, (LPCWSTR)lpszInput, nInLen, (LPSTR)lpInBuffer, rnOutLen, NULL, NULL);
#else   // OldCCU8Method
			// but this way works so define "OldCCU8Method" above the #ifndef to revert.
			lpInBuffer = (LPBYTE)W2U8(lpszInput, nInLen);

			// the length now must be recalculated. Since this is for the cc case, and
			//	cc can't handle 0 in the middle of a string anyway, we don't have to
			//	worry about having 0's in the data stream.
			nInLen = lstrlenA((LPCSTR)lpInBuffer);
#endif	// OldCCU8Method

			if( m_bDebugDisplayMode )
				ReturnUCharValuesFromUTF8((LPCSTR)lpInBuffer, nInLen, _T("Sending (UTF8Bytes) to Converter/DLL..."));
		}
		else if( eInEncodingForm == EncodingForm_UTF16 )
		{
			if( ciInput != 0 )
			{
				nInLen = ciInput;   // item count should be the number of 16-bit words directly
			}
			else
			{
				nInLen = nLenInput;
			}

			if( m_bDebugDisplayMode )
				ReturnUCharValues(lpszInput, nInLen, _T("Received (UTF16) from client and sending to Converter/DLL..."));

			// OLE2W is a noop, but in case it changes...
			lpInBuffer = (LPBYTE)OLE2W(lpszInput, nInLen);

			// but TECkit, et al., is expecting the number of 8-bit bytes.
			nInLen *= sizeof(WCHAR);
		}
		else if(    (eInEncodingForm == EncodingForm_UTF16BE)
				||  (eInEncodingForm == EncodingForm_UTF32)
				||  (eInEncodingForm == EncodingForm_UTF32BE)
		)
		{
			if( ciInput != 0 )
			{
				nInLen = ciInput; // item count is the number of Uni chars

				// for UTF32, the converter's actually expecting the length to be twice
				//	this much again.
				if( eInEncodingForm != EncodingForm_UTF16BE )
					nInLen *= sizeof(WCHAR);
			}
			else
			{
				nInLen = nLenInput;
			}

			// for these, just assume they've cast'ed it as a BSTR (i.e. wide).
			if( m_bDebugDisplayMode )
				ReturnUCharValues(lpszInput, nInLen, _T("Received (UTF16BE/32/32BE) from client/Sending to Converter/DLL..."));

			// these forms are for C++ apps that want to use the BSTR to transfer other
			//	than OLECHARs.
			lpInBuffer = (LPBYTE)lpszInput;

			// TECkit is actually expecting the number of bytes.
			nInLen *= sizeof(WCHAR);
		}
		else
			return ReturnError(ErrStatus_InEncFormNotSupported);

		if( !lpInBuffer )
			return ReturnError(ErrStatus_IncompleteChar);

// since this is allocated on the stack, don't muck around; get 10000 bytes for it.
#define cnECMinConvertBufLen 10000
		UINT    nOutLen = max(cnECMinConvertBufLen,nInLen*6);
		LPBYTE  lpOutBuffer = (LPBYTE)alloca(nOutLen);
		memset(lpOutBuffer,0,sizeof(long));	// clear the output in case it fails.

		// call the wrapper sub-classes' DoConvert to let them do it.
		hr = DoConvert(lpInBuffer,nInLen,lpOutBuffer,nOutLen);

		if( FAILED(hr) )
			return hr;

#ifndef v22_AllowEmptyReturn
		// there's no reason this shouldn't be considered a legitimate conversion...
		else if( nOutLen == 0 )
			return ReturnError(ErrStatus_NoReturnData);
#endif

		// null terminate the output.
		memset(&lpOutBuffer[nOutLen],0,sizeof(long));

		// check to see if the engine handled the given output form. If not, then see
		//	if it's a conversion we can easily do (otherwise we'll ask TEC to do the
		//	conversion for us (later) so that all engines can handle all possible
		//	output encoding forms.
		if( eOutEncodingForm != eFormEngineOut )
		{
			if( bIsLegacyFormat(eOutEncodingForm) )
			{
				if( (eFormEngineOut == EncodingForm_LegacyBytes) && (eOutEncodingForm == EncodingForm_LegacyString) )
				{
					// in this case, just *pretend* the engine outputs LegacyString
					// (the LegacyString case below really means "convert LegacyBytes
					//  to LegacyString)
					eFormEngineOut = eOutEncodingForm;
				}
			}
			else    // unicode output forms
			{
				// if the client wants UTF16, but the engine gives UTF8...
				if( (eOutEncodingForm == EncodingForm_UTF16) && (eFormEngineOut == EncodingForm_UTF8Bytes) )
				{
					// use the special form to convert it below
					eOutEncodingForm = eFormEngineOut = (EncodingForm)CCUnicode8;
				}
				// or vise versa
				else if( (eFormEngineOut == EncodingForm_UTF16)
					&& ( (eOutEncodingForm == EncodingForm_UTF8Bytes) || (eOutEncodingForm == EncodingForm_UTF8String) ) )
				{
					// engine gave UTF16, but user wants a UTF8 flavor.
					int nOutLen2 = (nOutLen+1)*4;
					LPSTR lpOutBuffer2 = (LPSTR)alloca(nOutLen2);
					nOutLen = WideCharToMultiByte(CP_UTF8, 0, (LPCWSTR)lpOutBuffer, nOutLen / 2, lpOutBuffer2, nOutLen2, NULL, NULL);
					memcpy(lpOutBuffer,lpOutBuffer2,nOutLen);
					memset(&lpOutBuffer[nOutLen],0,sizeof(long));	// terminate the new output.
					eFormEngineOut = eOutEncodingForm;
				}
				// these conversions we can do ourself
				else if((eOutEncodingForm == EncodingForm_UTF8String)
					||	(eOutEncodingForm == EncodingForm_UTF16))
				{
					eFormEngineOut = eOutEncodingForm;
				}
			}
		}

		long nItems = 0, nBSTRlen = 0;
		LPCWSTR str = 0;
		if( (eFormEngineOut == EncodingForm_LegacyBytes) || (eFormEngineOut == EncodingForm_UTF8Bytes) )
		{
			if( m_bDebugDisplayMode )
			{
				if( eFormEngineOut == EncodingForm_LegacyBytes )
					ReturnCharValues((LPCSTR)lpOutBuffer, nOutLen, _T("Received (LegacyBytes) back from Converter/DLL (returning as LegacyBytes)..."));
				else
					ReturnUCharValuesFromUTF8((LPCSTR)lpOutBuffer, nOutLen, _T("Received (UTF8Bytes) back from Converter/DLL (returning as UTF8Bytes)..."));
			}

			// stuff the returned 'bytes' into the BSTR as narrow characters rather than
			//	converting to wide
			str = (LPCWSTR)lpOutBuffer;
			nItems = nOutLen;
			nBSTRlen = (nOutLen + 1) / 2;
		}
		else if( eFormEngineOut == EncodingForm_LegacyString )
		{
			if( m_bDebugDisplayMode )
				ReturnCharValues((LPCSTR)lpOutBuffer, nOutLen, _T("Received (LegacyBytes) back from Converter/DLL (returning as LegacyString)..."));

			nBSTRlen = nItems = nOutLen;
			_acp = nCodePageOutput;
			str = MyA2W((LPCSTR)lpOutBuffer,nOutLen);
		}
		else if( eFormEngineOut == EncodingForm_UTF16 )
		{
			nBSTRlen = nItems = (nOutLen / sizeof(WCHAR));
			if( m_bDebugDisplayMode )
				ReturnUCharValues((LPCWSTR)lpOutBuffer, nItems, _T("Received (UTF16) back from Converter/DLL (returning as UTF16)..."));

			str = W2OLE((LPWSTR)lpOutBuffer); // W2OLE is a no-op
		}
		else if( eFormEngineOut == EncodingForm_UTF8String )
		{
			if( m_bDebugDisplayMode )
				ReturnUCharValuesFromUTF8((LPCSTR)lpOutBuffer, nOutLen, _T("Received (UTF8Bytes) back from Converter/DLL (returning as UTF8String)..."));

			str = MyA2W((LPSTR)lpOutBuffer,nOutLen);
			nBSTRlen = nItems = nOutLen;
		}
		else if( eFormEngineOut == CCUnicode8 )
		{
			if( m_bDebugDisplayMode )
				ReturnUCharValuesFromUTF8((LPCSTR)lpOutBuffer, nOutLen, _T("Received (UTF8Bytes) back from Converter/DLL (returning as UTF16)..."));

#ifndef	OldCCU8Method
			// this new way is more friendly to calculating true length
			int rnOutLen = (nOutLen+1)*3;
			lpInBuffer = (LPBYTE)alloca(rnOutLen);
			nBSTRlen = nItems = MultiByteToWideChar(CP_UTF8, 0, (LPCSTR)lpOutBuffer, nOutLen, (LPWSTR)lpInBuffer, rnOutLen/2);
			str = (LPCWSTR)lpInBuffer;
#else	// OldCCU8Method
			// but this way works so define "OldCCU8Method" above the #ifndef to revert.
			str = U82W((LPSTR)lpOutBuffer,nOutLen);

			// the length now must be recalculated. Since this is for the cc case, and
			//	cc can't handle 0 anyway, we don't have to worry about having 0's in
			//	the data stream.
			nBSTRlen = nItems = lstrlenW(str);
#endif	// OldCCU8Method
		}
		else if(	(eFormEngineOut == EncodingForm_UTF16BE)
				||	(eFormEngineOut == EncodingForm_UTF32)
				||	(eFormEngineOut == EncodingForm_UTF32BE)
		)
		{
			nBSTRlen = nItems = nOutLen / sizeof(WCHAR);

			// just assume they're expecting it to be cast'ed as wide
			if( m_bDebugDisplayMode )
				ReturnUCharValues((LPCWSTR)lpOutBuffer, nItems, _T("Received (UTF16BE/32/32BE) back from Converter/DLL..."));

			str = (LPCOLESTR)lpOutBuffer;

			// for UTF32, it is half again as little in the item count.
			if( eFormEngineOut != EncodingForm_UTF16BE )
				nItems /= sizeof(WCHAR);
		}
		else
			return ReturnError(ErrStatus_OutEncFormNotSupported);

#ifndef v22_AllowEmptyReturn
		if( nBSTRlen <= 0 )
			return ReturnError(ErrStatus_NoReturnDataBadOutForm);
#endif

		// check to see if the engine handled the given output form. If not, then ask
		//	TEC to do the conversion for us so that all engines can handle all possible
		//	output encoding forms (e.g. caller requested utf32, but above CC could only
		//  give us utf16/8)
		// Also, if the caller wanted something other than "None" for the eNormalizeOutput,
		//  then we also have to call TEC for that as well (but I think this only makes
		//  sense if the output is utf16(be) or utf32(be))
		// p.s. if this had been a TEC converter, then the eNormalizeOutput flag would
		//  ahready have been reset to None (by this point), since we would have directly
		//  requested that normalized form when we created the converter--see
		//  TecEncConverter.PreConvert)
		CComBSTR strOutput(nBSTRlen,(LPOLESTR)str);
		if(     (eFormEngineOut != eOutEncodingForm)
			||  (eNormalizeOutput != NormalizeFlags_None) )
		{
			hr = HaveTECConvert(strOutput,eFormEngineOut,nItems,eOutEncodingForm,eNormalizeOutput,strOutput,nItems);
		}

		strOutput.CopyTo(sOutput);

		if( pciOutput )
			*pciOutput = nItems;

		if( m_bDebugDisplayMode )
		{
			ReturnUCharValues( *sOutput, (int)lstrlenW(*sOutput), _T("Returning back to client...") );
		}
	}
	catch(...)
	{
		// most likely error is memory exception
		hr = ReturnError(ErrStatus_OutOfMemory);
	}

	return hr;
}

template<> HRESULT CEncConverter::CheckInitEncForms
(
	BOOL            bForward,
	EncodingForm&   eInEncodingForm,
	EncodingForm&   eOutEncodingForm
)
{
	HRESULT hr = S_OK;

	// if the user hasn't specified, then take the default case for the ConversionType:
	//  if L/RHS == eLegacy, then LegacyString
	//  if L/RHS == eUnicode, then UTF16
	if( eInEncodingForm == EncodingForm_Unspecified )
	{
		// this isn't optional
		if( m_eConversionType == ConvType_Unknown )
		{
			return ReturnError(ErrStatus_EncodingConvTypeNotSpecified);
		}
		else
		{
			NormConversionType eType;
			if( bForward )
				eType = NormalizeLhsConversionType(m_eConversionType);
			else
				eType = NormalizeRhsConversionType(m_eConversionType);

			if( eType == NormConversionType_eLegacy )
				eInEncodingForm = EncodingForm_LegacyString;
			else // eUnicode
				eInEncodingForm = DefaultUnicodeEncForm(bForward,true);
		}
	}

	// do the same for the output form
	if( eOutEncodingForm == EncodingForm_Unspecified )
	{
		// this isn't optional
		if( m_eConversionType == ConvType_Unknown )
		{
			return ReturnError(ErrStatus_EncodingConvTypeNotSpecified);
		}
		else
		{
			NormConversionType eType;
			if( bForward )
				eType = NormalizeRhsConversionType(m_eConversionType);
			else
				eType = NormalizeLhsConversionType(m_eConversionType);

			if( eType == NormConversionType_eLegacy )
				eOutEncodingForm = EncodingForm_LegacyString;
			else // eUnicode
				eOutEncodingForm = DefaultUnicodeEncForm(bForward,false);
		}

		hr = CheckForBadForm
				(
					bForward,
					eInEncodingForm,
					eOutEncodingForm
				);
	}

	return hr;
}

template<> HRESULT CEncConverter::CheckForBadForm
(
	BOOL            bForward,
	EncodingForm    eInEncodingForm,
	EncodingForm    eOutEncodingForm
)
{
	HRESULT hr = S_OK;
	if( IsUnidirectional(m_eConversionType) && !bForward )
	{
		hr = ReturnError(ErrStatus_InvalidConversionType);
	}
	else
	{
		BOOL bLhsUnicode = (NormalizeLhsConversionType(m_eConversionType) == NormConversionType_eUnicode);
		BOOL bRhsUnicode = (NormalizeRhsConversionType(m_eConversionType) == NormConversionType_eUnicode);
		if( bForward )
		{
			if( bLhsUnicode )
			{
				if( bIsLegacyFormat(eInEncodingForm) )
					hr = ReturnError(ErrStatus_InEncFormNotSupported);
			}
			else    // !bLhsUnicode
			{
				if( !bIsLegacyFormat(eInEncodingForm) )
					hr = ReturnError(ErrStatus_InEncFormNotSupported);
			}
			if( bRhsUnicode )
			{
				if( bIsLegacyFormat(eOutEncodingForm) )
					hr = ReturnError(ErrStatus_OutEncFormNotSupported);
			}
			else    // !bRhsUnicode
			{
				if( !bIsLegacyFormat(eOutEncodingForm) )
					hr = ReturnError(ErrStatus_OutEncFormNotSupported);
			}
		}
		else    // reverse
		{
			if( bLhsUnicode )
			{
				if( bIsLegacyFormat(eOutEncodingForm) )
					hr = ReturnError(ErrStatus_OutEncFormNotSupported);
			}
			else    // !bLhsUnicode
			{
				if( !bIsLegacyFormat(eOutEncodingForm) )
					hr = ReturnError(ErrStatus_OutEncFormNotSupported);
			}
			if( bRhsUnicode )
			{
				if( bIsLegacyFormat(eInEncodingForm) )
					hr = ReturnError(ErrStatus_InEncFormNotSupported);
			}
			else    // !bRhsUnicode
			{
				if( !bIsLegacyFormat(eInEncodingForm) )
					hr = ReturnError(ErrStatus_InEncFormNotSupported);
			}
		}
	}

	return hr;
}

template<> HRESULT CEncConverter::ReturnError(long status)
{
	HRESULT hr = status;
	switch( status )
	{
	case ErrStatus_InEncFormNotSupported:
		hr = Error(IDS_InEncodingFormNotSupported, __uuidof(IEncConverter), status);
		break;
	case ErrStatus_OutEncFormNotSupported:
		hr = Error(IDS_OutEncodingFormNotSupported, __uuidof(IEncConverter), status);
		break;
	case ErrStatus_EncodingConvTypeNotSpecified:
		hr = Error(IDS_EncodingConvTypeNotSpecified, __uuidof(IEncConverter), status);
		break;
	case ErrStatus_InvalidConversionType:
		hr = Error(IDS_InvalidConversionType,__uuidof(IEncConverter),status);
		break;
	case ErrStatus_OutOfMemory:
		hr = Error(IDS_kStatus_OutOfMemory, __uuidof(IEncConverter), status);
		break;
	case ErrStatus_NoReturnData:
		hr = Error(IDS_NoReturnData, __uuidof(IEncConverter), status);
		break;
	case ErrStatus_NoReturnDataBadOutForm:
		hr = Error(IDS_NoReturnDataBadOutForm, __uuidof(IEncConverter), status);
		break;
	case ErrStatus_IncompleteChar:
		hr = Error(IDS_IncompleteChar,__uuidof(IEncConverter),status);
		break;
	default:
		// the base class should handle it (or raise a msg box).
		break;
	};
	return hr;
}

HRESULT CEncConverter::Convert(/*[in]*/ BSTR sInput, /*[out]*/ BSTR* sOutput)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return InternalConvert(m_eEncodingInput,sInput,m_eEncodingOutput,m_eNormalizeOutput,sOutput,m_bForward);
}

HRESULT CEncConverter::ConvertEx(/*[in]*/ BSTR sInput, /*[in]*/ EncodingForm eInEncodingForm, /*[in]*/ long ciInput, /*[in]*/ EncodingForm eOutEncodingForm, /*[out]*/ long* ciOutput, /*[in]*/ NormalizeFlags eNormalizeOutput, /*[in]*/ VARIANT_BOOL bForward, /*[out,retval]*/ BSTR* sOutput)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	return InternalConvertEx(eInEncodingForm, sInput, ciInput, eOutEncodingForm, eNormalizeOutput, sOutput, ciOutput, bForward);
}

// global static map of all CEncConverter's *this* pointers based on inactivity timer IDs (see InactivityWarning)
// and reverse map (for KillTimer ID lookup based on *this* pointer)
std::map<UINT,CEncConverter*>   m_mapTimerIDsToThisPtrs;
std::map<CEncConverter*,UINT>   m_mapThisPtrsToTimerIDs;    // the inverse map (so we can find the appropriate kill timer id)

// Timer Proc.
// global timer proc is used so this control can activate both windowed and windowless.
void CALLBACK TimerProc(HWND /*hwnd*/, UINT /*uMsg*/, UINT idEvent, DWORD /*dwTime*/)
{
	CEncConverter* p = m_mapTimerIDsToThisPtrs[idEvent];
	if (p)
	{
		// stop the timer so it doesn't repeat
		m_mapTimerIDsToThisPtrs.erase(idEvent);
		::KillTimer(0, idEvent);
		p->InactivityWarning();
	}
}

// subclasses who care can override this virtual method to clean up resources
template<> void CEncConverter::InactivityWarning()
{
	TRACE(_T("InactivityWarning\n"));
}

template<> HRESULT CEncConverter::PreConvert
(
	EncodingForm    eInEncodingForm,
	EncodingForm&	eInFormEngine,
	EncodingForm    eOutEncodingForm,
	EncodingForm&   eOutFormEngine,
	NormalizeFlags& eNormalizeOutput,   // if your converter can do output normalization directly (like TEC can), then clear this out before returning (so the post conversion code won't try to do it again)
	BOOL            bForward,
	UINT            nInactivityWarningTimeOut   // 0-no timer; otherwise, # of ms
)
{
	// by default, the form it comes in is okay for the engine (never really true, so
	//	each engine's COM wrapper must override this; but this is here to see what you
	//	must do). For example, for CC, the input must be UTF8Bytes for Unicode, so
	//	you'd set the eInFormEngine to UTF8Bytes.
	eInFormEngine = eInEncodingForm;
	eOutFormEngine = eOutEncodingForm;

	// some conversion engines lock files, which can be bad for other clients. So start a 1 minute timer and if it
	//  expires then call "InactivityWarning" to give the servers an opportunity to release their resources
	//  (but only if requested by certain wrappers (e.g. all ICU wrappers do this).
	if( nInactivityWarningTimeOut > 0 )
	{
		// kill the current timer (if there was one) and remove it from the map
		UINT nTimerID = m_mapThisPtrsToTimerIDs[this];
		if( nTimerID != 0 )
		{
			m_mapTimerIDsToThisPtrs.erase(nTimerID);
			::KillTimer(0, nTimerID);
		}

		// create a new timer for the requested time and initialize our two maps
		nTimerID = (UINT)::SetTimer(0, (UINT_PTR)this, nInactivityWarningTimeOut, TimerProc);
		m_mapTimerIDsToThisPtrs[nTimerID] = this;
		m_mapThisPtrsToTimerIDs[this] = nTimerID;
	}

	return S_OK;
};

void CEncConverter::WriteAttributeDefault
(
	CComSafeArray<BSTR>&    rSa,
	LPCTSTR                 strNameKey,
	const CComBSTR&         strValue
)
{
	CComBSTR strAttrKey = strNameKey;
	rSa.Add(strAttrKey);

	// put it in the base class' map for subsequent queries
	m_mapProperties[strAttrKey] = CComBSTR(strValue);
};

BOOL CEncConverterConfig::Configure( CAutoConfigDlg* pPgConfig )
{
	// set them here in case the following process doesn't change them (because
	//  they'll be queried by the caller and we should *at least* return what was given to us)
	m_strFriendlyName = pPgConfig->m_strFriendlyName;
	m_eConversionType = pPgConfig->m_eConversionType;
	m_strLhsEncodingID = pPgConfig->m_strLhsEncodingId;
	m_strRhsEncodingID = pPgConfig->m_strRhsEncodingId;

	// construct the sheet to display "our" config sheet as well as the common "About" and "Test" tabs
	CAutoConfigSheet dlg(pPgConfig, m_strDisplayName, m_strHtmlFilename, m_strProgramID);

	// because it's possible that the user already added a converter to the repository and yet still
	//  cancelled, go ahead and update our info whether they clicked "OK" or not.
	INT_PTR retVal = dlg.DoModal();

	// update our internal values from the config tab (but only if the user clicked OK or
	//  if it is already in the repository--either editing or MetaCmpd type)
	if( (retVal == IDOK) || pPgConfig->m_bIsInRepository )
	{
		// but beware of clobbering them with empty values
		if( !pPgConfig->m_strFriendlyName.IsEmpty() )
			m_strFriendlyName = pPgConfig->m_strFriendlyName;

		if( !pPgConfig->m_strConverterIdentifier.IsEmpty() )
			m_strConverterID = pPgConfig->m_strConverterIdentifier;

		if( !pPgConfig->m_strLhsEncodingId.IsEmpty() )
			m_strLhsEncodingID = pPgConfig->m_strLhsEncodingId;

		if( !pPgConfig->m_strRhsEncodingId.IsEmpty() )
			m_strRhsEncodingID = pPgConfig->m_strRhsEncodingId;

		if( pPgConfig->m_eConversionType != ConvType_Unknown )
			m_eConversionType = pPgConfig->m_eConversionType;

		m_lProcessType = pPgConfig->m_lProcessTypeFlags;
		m_bIsInRepository = pPgConfig->m_bIsInRepository;

		// and... if we have the pointer to the parent EC, then go ahead and update that also
		//  (to save *some* clients a step)
		if( !!m_pIECParent )
		{
			CComBSTR strLhsEncodingID = m_strLhsEncodingID.AllocSysString();
			CComBSTR strRhsEncodingID = m_strRhsEncodingID.AllocSysString();

			// in case these were set to something else, don't trash them.
			long cpInput = 0, cpOutput = 0;
			m_pIECParent->get_CodePageInput(&cpInput);
			m_pIECParent->get_CodePageOutput(&cpOutput);

			// initialize it with the details we have.
			m_pIECParent->Initialize(m_strFriendlyName.AllocSysString(), m_strConverterID.AllocSysString(),
				&strLhsEncodingID, &strRhsEncodingID, &m_eConversionType, &m_lProcessType, cpInput, cpOutput, VARIANT_TRUE);

			// and update it's temporariness status
			m_pIECParent->put_IsInRepository( (m_bIsInRepository) ? VARIANT_TRUE : VARIANT_FALSE );
		}
	}

	return ( retVal == IDOK );
}

void CEncConverterConfig::InitializeFromThis
(
	BSTR*               pStrFriendlyName,
	BSTR*               pStrConverterIdentifier,
	ConvType&           eConversionType,
	BSTR*               pStrTestData
)
{
	if( IsEmpty(*pStrFriendlyName) )
		get_ConverterFriendlyName(pStrFriendlyName);
	if( IsEmpty(*pStrConverterIdentifier) )
		get_ConverterIdentifier(pStrConverterIdentifier);
	if( IsEmpty(*pStrTestData) )
		*pStrTestData = CComBSTR("Test Data").Detach();
	if( eConversionType == ConvType_Unknown )
		get_ConversionType(&eConversionType);
}

void CEncConverterConfig::DisplayTestPageEx(CAutoConfigDlg* pPgConfig, const CString& strTestData)
{
	// construct the sheet to display "our" config sheet as well as the common "About" and "Test" tabs
	CString strWindowTitle; strWindowTitle.Format(_T("%s (converter: %s)"), m_strDisplayName, pPgConfig->m_strFriendlyName);
	CAutoConfigSheet dlg(pPgConfig, strWindowTitle, strTestData);
	dlg.DoModal();
}

HRESULT CEncConverterConfig::Equals(VARIANT rhs, VARIANT_BOOL *bEqual)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	if( bEqual == NULL )
		return E_POINTER;

	// I'm not sure if this is what they are getting at by the function in .Net, but
	//  it seems to me that these can be equal only if they have the same name.
	CComVariant var(rhs);
	if( ((var.vt == VT_DISPATCH) || (var.vt == VT_UNKNOWN)) && (*var.ppdispVal != NULL) )
	{
		CComPtr<IEncConverterConfig> pIEcConfig;
		(*var.ppdispVal)->QueryInterface(__uuidof(IEncConverterConfig),(LPVOID*)&pIEcConfig);
		if( !!pIEcConfig )
		{
			CComBSTR strRhsName;
			pIEcConfig->get_ConverterFriendlyName(&strRhsName);
			if( strRhsName == (LPCTSTR)m_strFriendlyName )
			{
				*bEqual = -1;   // true
				return S_OK;
			}
		}
	}

	*bEqual = 0;    // false
	return S_OK;
}

CComPtr<IEncConverter> pTec;
template<> HRESULT CEncConverter::HaveTECConvert(const CComBSTR& sInput, EncodingForm eFormInput, long ciInput, EncodingForm eFormOutput, NormalizeFlags eNormalizeOutput, CComBSTR& sInputUTF16, long& nNumItems)
{
	HRESULT hr = S_OK;

	// get the TECkit COM interface (if we don't have it already)
	if( !pTec )
	{
		hr = pTec.CoCreateInstance(TECKIT_PROGID);
	}

	if( SUCCEEDED(hr) && !!pTec )
	{
		// pass an empty CComBSTR rather than the address of the given one so the
		//	string in the given one won't leak.
		CComBSTR sOutput;
		hr = pTec->ConvertEx(sInput,eFormInput,ciInput,eFormOutput,&nNumItems,
								eNormalizeOutput,true,&sOutput);

		// if it worked, then put the returned value in the callers buffer.
		if( SUCCEEDED(hr) )
			sInputUTF16 = sOutput;
	}

	return hr;
}
