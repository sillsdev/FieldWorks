// ECEncConverter.h
//	This file contains a template class that implements a great deal of the code needed to
//	support an Encoding Converter's plug-in. You can derive a class from this class and
//	implement a few methods to get started developing a plug-in. Some instructions for
//	doing this are in the "Making a new Plug-in ReadMe.txt" in the Developers folder of the
//	Encoding Converters install set.
#pragma once

#include "ECConv.h"     // for the conversion macros (we specialized them)
#include "atlsafe.h"    // for CComSafeArray
#include <map>          // use a 'map' stl class to contain the converter pointers.
#include "ECResource.h"
#include <atlcom.h>

// import the type library from the repository DLL (to serve as the interface definition rather
//  than the .idl file we were using). This will make sure that we're (i.e. CPP plug-ins and such)
//  are on the same page as what the repository is expecting.
#import "ECInterfaces.tlb" raw_interfaces_only
using namespace ECInterfaces;

typedef CAdapt<CComBSTR>            ACComBSTR;

// container for this' attributes
typedef std::map<ACComBSTR, ACComBSTR>  AttrContainerType;

// the following defines, functions and definitions are used very frequently by sub-classes
//	so I'm putting them here so other plug-ins can benefit as well...
typedef CComPtr<IEncConverters>         PtrIEncConverters;
typedef CComPtr<IEncConverter>          PtrIEncConverter;
typedef CComPtr<IEncConverterConfig>    PtrIEncConverterConfig;
typedef CAdapt<PtrIEncConverter>        APtrIEncConverter;

// The code below uses the TECkit COM interface to do some specialized conversions between
//	more unusual Unicode encodings (e.g. UTF32, UTF16BE, etc.)
#define	TECKIT_PROGID	L"SilEncConverters40.TecFormEncConverter"
#define	TECKIT_EFCREQ	L"EncodingFormConversionRequest"	// use as ConverterName of Initialize

#define CCUnicode8      30  // to cause the UTF16 input to be converted to UTF8 for CC DLL.
#define COR_E_SAFEARRAYTYPEMISMATCH 0x80131533  // from MSDN

template <class T, const IID* piid = &__uuidof(T), const GUID* plibid = &CAtlModule::m_libid,
	WORD wMajor = 3, WORD wMinor = 1>
class CECEncConverter :
	public CComObjectRootEx<CComSingleThreadModel>
  , public ISupportErrorInfo
  , public IDispatchImpl<T, piid, plibid, wMajor, wMinor>
{
protected:
	CComBSTR        m_strProgramID;     // indicates the Program ID from the registry (e.g. "SilEncConverters40.TecEncConverter")
	CComBSTR        m_strImplementType; // eg. "SIL.tec" rather than the program ID
	CComBSTR        m_strPersistKey;    // Key to the persistance for this instance (e.g. Reg key)
	CComBSTR        m_strLhsEncodingID; // something unique/unchangable (at least from TK) (e.g. SIL-UNICODE_DEVANAGRI-2002<>SIL-UNICODE_IPA-2002)
	CComBSTR        m_strRhsEncodingID; // something unique/unchangable (at least from TK) (e.g. SIL-UNICODE_DEVANAGRI-2002<>SIL-UNICODE_IPA-2002)
	CComBSTR        m_strConverterID;   // file spec to the map (or some plug-in specific identifier, such as "Devanagari-Latin" (for ICU) or "65001" (for code page UTF8)
	long            m_lProcessType;     // process type (see .idl)
	ConvType        m_eConversionType;  // conversion type (see .idl)
	BOOL            m_bForward;         // default direction of conversion (for bidirectional conversions; e.g. *not* CC)
	EncodingForm    m_eEncodingInput;   // encoding form of input (see .idl)
	EncodingForm    m_eEncodingOutput;  // encoding form of output (see .idl)
	NormalizeFlags  m_eNormalizeOutput; // should we normalize the output?
	BOOL            m_bDebugDisplayMode;// should we display debug information?
	int             m_nCodePageInput;
	int             m_nCodePageOutput;

	AttrContainerType   m_mapProperties;    // map of all attributes (filled during get_AttributeKeys)
	BOOL            m_bInitialized;
	BOOL            m_bIsInRepository;  // indicates whether this converter is in the static repository (true) or not (false)

public:
	// make it public so the teckit errFunc can access it.
	CComBSTR        m_strFriendlyName;          // something nice and friendly (e.g. "Annapurna<>Unicode")

	CECEncConverter(LPCTSTR lpszProgramID, LPCTSTR lpszImplementType);	// sub-class gives its progid

// IEncConverter
public:
	STDMETHOD(get_ProcessType)(long *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_lProcessType;
		return S_OK;
	}
	STDMETHOD(put_ProcessType)(long lVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_lProcessType = lVal;
		return S_OK;
	}
	STDMETHOD(get_CodePageInput)(long *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_nCodePageInput;
		return S_OK;
	}
	STDMETHOD(put_CodePageInput)(long lVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_nCodePageInput = lVal;
		return S_OK;
	}
	STDMETHOD(get_CodePageOutput)(long *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_nCodePageOutput;
		return S_OK;
	}
	STDMETHOD(put_CodePageOutput)(long lVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_nCodePageOutput = lVal;
		return S_OK;
	}
	STDMETHOD(get_ProgramID)(/*[out, retval]*/ BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strProgramID.Copy();
		return S_OK;
	}
	STDMETHOD(get_ImplementType)(/*[out, retval]*/ BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strImplementType.Copy();
		return S_OK;
	}
	STDMETHOD(get_LeftEncodingID)(BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strLhsEncodingID.Copy();
		return S_OK;
	}
	STDMETHOD(get_RightEncodingID)(BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strRhsEncodingID.Copy();
		return S_OK;
	}
	STDMETHOD(put_RightEncodingID)(BSTR newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_strRhsEncodingID = newVal;
		return S_OK;
	}
	STDMETHOD(get_NormalizeOutput)(/*[out, retval]*/ NormalizeFlags *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_eNormalizeOutput;
		return S_OK;
	}
	STDMETHOD(put_NormalizeOutput)(/*[in]*/ NormalizeFlags newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_eNormalizeOutput = newVal;
		return S_OK;
	}
	// sub-classes fill in an array of strings with the 'keys' for the attributes they
	//  support
	virtual HRESULT GetAttributeKeys(CComSafeArray<BSTR>& rSa)
	{
		return S_OK;
	}
	STDMETHOD(get_AttributeKeys)(SAFEARRAY* *pVal);
	STDMETHOD(AttributeValue)(BSTR sKey, BSTR* pVal);
	STDMETHOD(get_Debug)(/*[out, retval]*/ VARIANT_BOOL *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = (VARIANT_BOOL)m_bDebugDisplayMode;
		return S_OK;
	}
	STDMETHOD(put_Debug)(/*[in]*/ VARIANT_BOOL newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_bDebugDisplayMode = newVal;
		return S_OK;
	}
	STDMETHOD(get_EncodingOut)(/*[out, retval]*/ EncodingForm *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_eEncodingOutput;
		return S_OK;
	}
	STDMETHOD(put_EncodingOut)(/*[in]*/ EncodingForm newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_eEncodingOutput = newVal;
		return S_OK;
	}
	STDMETHOD(get_EncodingIn)(/*[out, retval]*/ EncodingForm *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_eEncodingInput;
		return S_OK;
	}
	STDMETHOD(put_EncodingIn)(/*[in]*/ EncodingForm newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_eEncodingInput = newVal;
		return S_OK;
	}
	STDMETHOD(get_DirectionForward)(/*[out, retval]*/ VARIANT_BOOL *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = (VARIANT_BOOL)m_bForward;
		return S_OK;
	}
	STDMETHOD(put_DirectionForward)(/*[in]*/ VARIANT_BOOL newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_bForward = newVal;
		return S_OK;
	}
	STDMETHOD(get_ConverterIdentifier)(/*[out, retval]*/ BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strConverterID.Copy();
		return S_OK;
	}
	STDMETHOD(get_ConversionType)(/*[out, retval]*/ ConvType *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_eConversionType;
		return S_OK;
	}
	STDMETHOD(get_Name)(/*[out, retval]*/ BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strFriendlyName.Copy();
		return S_OK;
	}
	STDMETHOD(put_Name)(BSTR val)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_strFriendlyName = val;
		return S_OK;
	}
	// this is for non-file based converters (e.g. ICU) to tell of all the converters
	//	that are available.
	STDMETHOD(get_ConverterNameEnum)(SAFEARRAY* *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = 0;
		return E_NOTIMPL;
	}

	// This is called by the repository (EncConverters) to give us the information about
	//	what this converter is. If the 'bAdding' flag is set, it means that the converter
	//	is being added for the first time during this call (otherwise, it only means that
	//	the repository is being instantiated and the 'actual converters' are being added
	//	to the collection.
	STDMETHOD(Initialize)(BSTR ConverterName, BSTR ConverterIdentifier, BSTR* LhsEncodingID, BSTR* RhsEncodingID, ConvType* eConversionType, long* ProcessTypeFlags, long CodePageInput, long CodePageOutput, VARIANT_BOOL bAdding);
	STDMETHOD(get_Configurator)(IEncConverterConfig* *pECConfig);

	// sub-classes must implement this to return a new configurator of the right type.
	virtual HRESULT GetConfigurator(IEncConverterConfig* *pConfigurator) = 0;

	STDMETHOD(get_IsInRepository)(/*[out, retval]*/ VARIANT_BOOL *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = (VARIANT_BOOL)m_bIsInRepository;
		return S_OK;
	}
	STDMETHOD(put_IsInRepository)(/*[in]*/ VARIANT_BOOL newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_bIsInRepository = newVal;
		return S_OK;
	}

	// Default implementations of the "Convert" methods, good for most sub-classes
	//	The meat of what the sub-class implements, then, is in the PreConvert and
	//	DoConvert methods, which are ultimately called by the "InternalConvert" methods
	STDMETHOD(Convert)(/*[in]*/ BSTR sInput, /*[out]*/ BSTR* sOutput);
	STDMETHOD(ConvertEx)(/*[in]*/ BSTR sInput, /*[in]*/ EncodingForm inEnc, /*[in]*/ long ciInput, /*[in]*/ EncodingForm outEnc, /*[out]*/ long* ciOutput, /*[in]*/ NormalizeFlags eNormalizeOutput, /*[in]*/ VARIANT_BOOL bForward, /*[out,retval]*/ BSTR* sOutput);
	STDMETHOD(ConvertToUnicode)(/*[in]*/ SAFEARRAY* pbaInput, /*[out]*/ BSTR* sOutput);
	STDMETHOD(ConvertFromUnicode)(/*[in]*/ BSTR sInput, /*[out]*/ SAFEARRAY* *pbaOutput);

	// these following methods/properties are additions that .Net gives all classes. Since
	//  it is anticipated that this whole thing might one day be incorporated into .Net, i'll
	//  add them here as well (so that clients won't break) and more importantly, if the user
	//  has an unmanaged EncConverter object and calls one of these (since the interface
	//  is defined in .Net, these methods will be exposed), it won't fatal except because
	//  one doesn't exist here.
	STDMETHOD(get_ToString)(BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		// give something useful, for example, for a tooltip.
		CString str = _T("Converter Details:");

		// indicate whether it's temporary or not!
		if( !m_bIsInRepository )
			str = _T("Temporary ") + str;

		str += FormatTabbedTip(_T("Name"), (LPCTSTR)m_strFriendlyName);
		str += FormatTabbedTip(_T("Identifier"), (LPCTSTR)m_strConverterID);
		str += FormatTabbedTip(_T("Implementation Type"), (LPCTSTR)m_strImplementType);
		str += FormatTabbedTip(_T("Conversion Type"), strConvType(m_eConversionType));

		if( m_lProcessType != ProcessTypeFlags_DontKnow )
			str += FormatTabbedTip(_T("Process Type"), strProcessType(m_lProcessType));
		if( !IsEmpty(m_strLhsEncodingID) )
			str += FormatTabbedTip(_T("Left side Encoding ID"), (LPCTSTR)m_strLhsEncodingID);
		if( !IsEmpty(m_strRhsEncodingID) )
			str += FormatTabbedTip(_T("Right side Encoding ID"), (LPCTSTR)m_strRhsEncodingID);

		// also include the current conversion option values
		str += _T("\r\n\r\nCurrent Conversion Options:");
		str += FormatTabbedTip(_T("Direction"), (m_bForward) ? _T("Forward") : _T("Reverse"));
		str += FormatTabbedTip(_T("Normalize Output"), strNormalizeOutputType(m_eNormalizeOutput));
		str += FormatTabbedTip(_T("Debug"), (m_bDebugDisplayMode) ? _T("True") : _T("False"));

		if (IsLhsLegacy())
		{
			CString strCodePage;
			strCodePage.Format(_T("%d"), m_nCodePageInput);
			str += FormatTabbedTip(_T("Input Code Page"), strCodePage);
		}

		if (IsRhsLegacy())
		{
			CString strCodePage;
			strCodePage.Format(_T("%d"), m_nCodePageOutput);
			str += FormatTabbedTip(_T("Outputput Code Page"), strCodePage);
		}

		*pVal = str.AllocSysString();
		return S_OK;
	}

	BOOL IsLhsLegacy()
	{
		if (m_bForward)
			return (NormalizeLhsConversionType(m_eConversionType) == NormConversionType_eLegacy);
		else
			return (NormalizeRhsConversionType(m_eConversionType) == NormConversionType_eLegacy);
	}

	BOOL IsRhsLegacy()
	{
		if (m_bForward)
			return (NormalizeRhsConversionType(m_eConversionType) == NormConversionType_eLegacy);
		else
			return (NormalizeLhsConversionType(m_eConversionType) == NormConversionType_eLegacy);
	}

	CString strProcessType(long lProcessType)
	{
		CString str;
		if( lProcessType & ProcessTypeFlags_UnicodeEncodingConversion )
			str += _T("UnicodeEncodingConversion, ");
		if( lProcessType & ProcessTypeFlags_Transliteration )
			str += _T("Transliteration, ");
		if( lProcessType & ProcessTypeFlags_ICUTransliteration )
			str += _T("ICUTransliteration, ");
		if( lProcessType & ProcessTypeFlags_ICUConverter )
			str += _T("ICUConverter, ");
		if( lProcessType & ProcessTypeFlags_ICURegularExpression )
			str += _T("ICURegularExpression, ");
		if( lProcessType & ProcessTypeFlags_CodePageConversion )
			str += _T("CodePageConversion, ");
		if( lProcessType & ProcessTypeFlags_NonUnicodeEncodingConversion )
			str += _T("NonUnicodeEncodingConversion, ");
		if( lProcessType & ProcessTypeFlags_SpellingFixerProject )
			str += _T("SpellingFixerProject, ");
		if( lProcessType & ProcessTypeFlags_PythonScript )
			str += _T("PythonScript, ");
		if( lProcessType & ProcessTypeFlags_PerlExpression )
			str += _T("PerlExpression, ");
		if( lProcessType & ProcessTypeFlags_UserDefinedSpare1 )
			str += _T("UserDefinedSpare #1, ");
		if( lProcessType & ProcessTypeFlags_UserDefinedSpare2 )
			str += _T("UserDefinedSpare #2, ");

		// strip off the final ", "
		if( !str.IsEmpty() )
			str = str.Left(str.GetLength() - 2);

		return str;
	}

	LPCTSTR strConvType(ConvType eType)
	{
		switch(eType)
		{
		case ConvType_Legacy_to_from_Unicode:
			return _T("Legacy_to_from_Unicode");
			break;
		case ConvType_Legacy_to_from_Legacy:
			return _T("Legacy_to_from_Legacy");
			break;
		case ConvType_Unicode_to_from_Legacy:
			return _T("Unicode_to_from_Legacy");
			break;
		case ConvType_Unicode_to_from_Unicode:
			return _T("Unicode_to_from_Unicode");
			break;
		case ConvType_Legacy_to_Unicode:
			return _T("Legacy_to_Unicode");
			break;
		case ConvType_Legacy_to_Legacy:
			return _T("Legacy_to_Legacy");
			break;
		case ConvType_Unicode_to_Legacy:
			return _T("Unicode_to_Legacy");
			break;
		case ConvType_Unicode_to_Unicode:
			return _T("Unicode_to_Unicode");
			break;

		case ConvType_Unknown:
		default:
			return _T("Unknown");
			break;
		}
	}

	LPCTSTR strNormalizeOutputType(NormalizeFlags eNormalFlags)
	{
		switch(eNormalFlags)
		{
		case NormalizeFlags_FullyDecomposed:
			return _T("FullyDecomposed");
			break;
		case NormalizeFlags_FullyComposed:
			return _T("FullyComposed");
			break;
		default:
		case NormalizeFlags_None:
			return _T("None");
			break;
		}
	}

	CString FormatTabbedTip(const CString& strName, const CString& strValue)
	{
		CString str;
		str.Format(_T("\r\n    %s: '%s'"), strName, strValue);
		return str;
	}

	STDMETHOD(GetHashCode)(long *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = (long)(this && 0xFFFF); // get rid of compiler warning...
		return S_OK;
	}

	// this is for .Net, but we really can't deal with the Type class here
	// (basically, I don't know which tlb file it is in to try to use #import)
/*
	STDMETHOD(GetType)(_Type* *pVal)
	{
		*pVal = NULL;
		return E_NOTIMPL;
	}
*/

	STDMETHOD(Equals)(VARIANT rhs, VARIANT_BOOL *bEqual);

	// sub-classes can implement this function if they want to be informed when they haven't been used
	//  (to convert something) in 1 minute (e.g. to release resources)
	virtual void InactivityWarning();

protected:
// ISupportsErrorInfo
	STDMETHOD(InterfaceSupportsErrorInfo)(REFIID riid)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		static const IID* arr[] =
		{
			piid
		};
		for (int i=0; i < sizeof(arr) / sizeof(arr[0]); i++)
		{
			if (InlineIsEqualGUID(*arr[i],riid))
				return S_OK;
		}
		return S_FALSE;
	}

	// have a special com pointer to the TECkit wrapper object so we can ask TEC to do
	//	special conversions from time to time (e.g. UTF16BE, UTF32, etc) for the other
	//	engines that don't otherwise support those flavors.
	virtual HRESULT HaveTECConvert(const CComBSTR& sInput, EncodingForm eFormInput, long ciInput, EncodingForm eFormOutput, NormalizeFlags eNormalizeOutput, CComBSTR& sInputUTF16, long& nNumItems);

	// each subclass which is a 'CoClass' (i.e. derives from CComCoClass<>) must implement
	//  the "Error" method to ReturnError can work correctly.
	virtual HRESULT Error(UINT nID, const IID& iid, HRESULT hRes) = 0;

	// the following three protected members are for 'Debug' mode
	HRESULT ReturnUCharValues(LPCWSTR szInputString, int nLengthWords, LPCTSTR lpszCaption);
	HRESULT ReturnCharValues(LPCSTR lpszInputString, int nLengthBytes, LPCTSTR lpszCaption);
	HRESULT ReturnUCharValuesFromUTF8(LPCSTR lpszInputString, int nLengthBytes, LPCTSTR lpszCaption);
	HRESULT FinishDebugDisplay(LPCTSTR lpszBufOutput, LPCTSTR lpszCaption);

	// Since each sub-class has to do basic input/output encoding format processing, they
	//	should all mostly come thru this and the next functions.
	virtual HRESULT InternalConvert
	(
		EncodingForm    eInEncodingForm,
		BSTR			sInput,
		EncodingForm    eOutEncodingForm,
		NormalizeFlags  eNormalizeOutput,
		BSTR*           sOutput,
		BOOL            bForward
	);

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
	virtual HRESULT InternalConvertEx
	(
		EncodingForm    eInEncodingForm,
		BSTR			sInput,
		long            ciInput,
		EncodingForm    eOutEncodingForm,
		NormalizeFlags  eNormalizeOutput,
		BSTR*           sOutput,
		long*           pciOutput,
		BOOL            bForward
	);
	virtual HRESULT CheckInitEncForms
	(
		BOOL            bForward,
		EncodingForm&   eInEncodingForm,
		EncodingForm&   eOutEncodingForm
	);
	virtual HRESULT CheckForBadForm
	(
		BOOL            bForward,
		EncodingForm    eInEncodingForm,
		EncodingForm    eOutEncodingForm
	);
	virtual HRESULT ReturnError(long status);

	// must override to get InactivityWarning
	virtual HRESULT PreConvert
	(
		EncodingForm    eInEncodingForm,
		EncodingForm&	eInFormEngine,
		EncodingForm    eOutEncodingForm,
		EncodingForm&   eOutFormEngine,
		NormalizeFlags& eNormalizeOutput,   // if your converter can do output normalization directly (like TEC can), then clear this out before returning (so the post conversion code won't try to do it again)
		BOOL            bForward,
		UINT            nInactivityWarningTimeOut = 0   // 0-no timer; otherwise, # of ms
	);

	// this is where the sub-classes do the actual work... i.e. override this one for sure.
	virtual HRESULT DoConvert
	(
		LPBYTE  lpInBuffer,
		UINT    nInLen,
		LPBYTE  lpOutBuffer,
		UINT&   rnOutLen
	) = 0;

	BOOL    bIsLegacyFormat(EncodingForm eForm)
	{
		return  (   (eForm == EncodingForm_LegacyString)
				||  (eForm == EncodingForm_LegacyBytes) );
	}

	// some converters (e.g. cc) use a different Unicode form as basic
	virtual EncodingForm    DefaultUnicodeEncForm(BOOL bForward, BOOL bLHS)
	{ return EncodingForm_UTF16; };

	virtual void WriteAttributeDefault
	(
		CComSafeArray<BSTR>&    rSa,
		LPCTSTR                 strNameKey,
		const CComBSTR&         strValue
	);
};

typedef CECEncConverter<IEncConverter>  CEncConverter;

class CAutoConfigDlg;   // forward declaration

template <class T, const IID* piid = &__uuidof(T), const GUID* plibid = &CAtlModule::m_libid,
	WORD wMajor = 3, WORD wMinor = 1>
class CECEncConverterConfig :
	public CComObjectRootEx<CComSingleThreadModel>
  , public ISupportErrorInfo
  , public IDispatchImpl<T, piid, plibid, wMajor, wMinor>
{
protected:
	CString             m_strProgramID;         // eg. "SilEncConverters40.PyScriptEncConverter" rather than the implementation type
	CString             m_strDisplayName;       // e.g. "Python Script"
	CString             m_strHtmlFilename;      // filename of the HTML file that becomes the About HTML control (assumed to be in the local dir by installer)
	ProcessTypeFlags    m_eDefiningProcessType; // a process type flag that uniquely defines this configurator type (e.g. ProcessTypeFlags_ICUTransliteration)

	CString             m_strFriendlyName;      // something nice and friendly (e.g. "Annapurna<>Unicode")
	CString             m_strConverterID;       // file spec to the map (or some plug-in specific identifier, such as "Devanagari-Latin" (for ICU) or "65001" (for code page UTF8)
	CString             m_strLhsEncodingID;     // something unique/unchangable (at least from TK) (e.g. SIL-UNICODE_DEVANAGRI-2002<>SIL-UNICODE_IPA-2002)
	CString             m_strRhsEncodingID;     // something unique/unchangable (at least from TK) (e.g. SIL-UNICODE_DEVANAGRI-2002<>SIL-UNICODE_IPA-2002)
	ConvType            m_eConversionType;      // conversion type (see .idl)
	long                m_lProcessType;         // process type (see .idl)
	BOOL                m_bIsInRepository;      // indicates whether this converter is in the static repository (true) or not (false)
	PtrIEncConverter    m_pIECParent;           // reference to the parent EC of which this is the configurator

public:
	CECEncConverterConfig
		(
			LPCTSTR             lpszProgramID,
			LPCTSTR             lpszDisplayName,
			LPCTSTR             lpszHtmlFilename,
			ProcessTypeFlags    eDefiningProcessType = ProcessTypeFlags_DontKnow
		)
		: m_strProgramID(lpszProgramID)
		, m_strDisplayName(lpszDisplayName)
		, m_strHtmlFilename(lpszHtmlFilename)
		, m_eDefiningProcessType(eDefiningProcessType)
		, m_lProcessType(ProcessTypeFlags_DontKnow)
		, m_eConversionType(ConvType_Unknown)
		, m_bIsInRepository(ATL_VARIANT_FALSE)
	{
	};

// IEncConverterConfig
public:
	STDMETHOD(get_ConfiguratorDisplayName)(/*[out, retval]*/ BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strDisplayName.AllocSysString();
		return S_OK;
	}

	STDMETHOD(get_DefiningProcessType)(/*[out, retval]*/ ProcessTypeFlags *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_eDefiningProcessType;
		return S_OK;
	}

	STDMETHOD(get_ConverterFriendlyName)(/*[out, retval]*/ BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strFriendlyName.AllocSysString();
		return S_OK;
	}

	STDMETHOD(put_ConverterFriendlyName)(/*[in]*/ BSTR newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_strFriendlyName = newVal;
		return S_OK;
	}

	STDMETHOD(get_ConverterIdentifier)(/*[out, retval]*/ BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strConverterID.AllocSysString();
		return S_OK;
	}

	STDMETHOD(put_ConverterIdentifier)(/*[in]*/ BSTR newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_strConverterID = newVal;
		return S_OK;
	}

	STDMETHOD(get_LeftEncodingID)(BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strLhsEncodingID.AllocSysString();
		return S_OK;
	}

	STDMETHOD(put_LeftEncodingID)(/*[in]*/ BSTR newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_strLhsEncodingID = newVal;
		return S_OK;
	}

	STDMETHOD(get_RightEncodingID)(BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_strRhsEncodingID.AllocSysString();
		return S_OK;
	}

	STDMETHOD(put_RightEncodingID)(/*[in]*/ BSTR newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_strRhsEncodingID = newVal;
		return S_OK;
	}

	STDMETHOD(get_ConversionType)(/*[out, retval]*/ ConvType *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_eConversionType;
		return S_OK;
	}

	STDMETHOD(put_ConversionType)(/*[in]*/ ConvType newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_eConversionType = newVal;
		return S_OK;
	}

	STDMETHOD(get_ProcessType)(long *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = m_lProcessType;
		return S_OK;
	}

	STDMETHOD(put_ProcessType)(/*[in]*/ long newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_lProcessType = newVal;
		return S_OK;
	}

	STDMETHOD(get_IsInRepository)(/*[out, retval]*/ VARIANT_BOOL *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = (VARIANT_BOOL)m_bIsInRepository;
		return S_OK;
	}
	STDMETHOD(put_IsInRepository)(/*[in]*/ VARIANT_BOOL newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_bIsInRepository = newVal;
		return S_OK;
	}

	STDMETHOD(get_ParentEncConverter)(/*[out, retval]*/ IEncConverter* *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		return m_pIECParent.QueryInterface(pVal);
	}

	STDMETHOD(putref_ParentEncConverter)(/*[in]*/ IEncConverter* newVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		m_pIECParent = newVal;
		return S_OK;
	}

	STDMETHOD(Configure)(IEncConverters* pECs, BSTR strFriendlyName, ConvType eConversionType, BSTR strLhsEncodingID, BSTR strRhsEncodingID, VARIANT_BOOL *bRet) = 0;
	STDMETHOD(DisplayTestPage)(IEncConverters* pECs, BSTR strFriendlyName, BSTR strConverterIdentifier, ConvType eConversionType, BSTR strTestData) = 0;

	// base class implementation that does all the work
	virtual BOOL Configure(CAutoConfigDlg* pPgConfig);
	virtual void DisplayTestPageEx(CAutoConfigDlg* pPgConfig, const CString& strTestData);

	// initialize the parameters to DisplayTestPage from This if they're null
	void InitializeFromThis
	(
		BSTR*       pStrFriendlyName,
		BSTR*       pStrConverterIdentifier,
		ConvType&   eConversionType,
		BSTR*       pStrTestData
	);

	// these following methods/properties are additions that .Net gives all classes. Since
	//  it is anticipated that this whole thing might one day be incorporated into .Net, i'll
	//  add them here as well (so that clients won't break) and more importantly, if the user
	//  has an unmanaged EncConverter object and calls one of these (since the interface
	//  is defined in .Net, these methods will be exposed), it won't fatal except because
	//  one doesn't exist here.
	STDMETHOD(get_ToString)(BSTR *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		*pVal = m_strDisplayName.AllocSysString();
		return S_OK;
	}

	STDMETHOD(GetHashCode)(long *pVal)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		if( pVal == NULL )
			return E_POINTER;

		*pVal = (long)(this && 0xFFFF); // get rid of compiler warning...
		return S_OK;
	}

	// this is for .Net, but we really can't deal with the Type class here
	// (basically, I don't know which tlb file it is in to try to use #import)
/*
	STDMETHOD(GetType)(_Type* *pVal)
	{
		*pVal = NULL;
		return E_NOTIMPL;
	}
*/

	STDMETHOD(Equals)(VARIANT rhs, VARIANT_BOOL *bEqual);

protected:
// ISupportsErrorInfo
	STDMETHOD(InterfaceSupportsErrorInfo)(REFIID riid)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		static const IID* arr[] =
		{
			piid
		};
		for (int i=0; i < sizeof(arr) / sizeof(arr[0]); i++)
		{
			if (InlineIsEqualGUID(*arr[i],riid))
				return S_OK;
		}
		return S_FALSE;
	}

	// each subclass which is a 'CoClass' (i.e. derives from CComCoClass<>) must implement
	//  the "Error" method to ReturnError can work correctly.
	virtual HRESULT Error(UINT nID, const IID& iid, HRESULT hRes) = 0;
};

typedef CECEncConverterConfig<IEncConverterConfig>  CEncConverterConfig;

inline BOOL    IsUnidirectional(ConvType eConversionType)
{
	return  (  (eConversionType >= ConvType_Legacy_to_Unicode)
			&& (eConversionType <= ConvType_Unicode_to_Unicode)
			);
}

inline NormConversionType NormalizeLhsConversionType(ConvType type)
{
	NormConversionType eType = NormConversionType_eUnicode;
	switch(type)
	{
	case ConvType_Legacy_to_from_Unicode:
	case ConvType_Legacy_to_from_Legacy:
	case ConvType_Legacy_to_Unicode:
	case ConvType_Legacy_to_Legacy:
		eType = NormConversionType_eLegacy;
		break;

	case ConvType_Unicode_to_from_Legacy:
	case ConvType_Unicode_to_from_Unicode:
	case ConvType_Unicode_to_Legacy:
	case ConvType_Unicode_to_Unicode:
		eType = NormConversionType_eUnicode;
		break;

	default:
		ASSERT(false);
		break;
	};

	return eType;
}

inline NormConversionType NormalizeRhsConversionType(ConvType type)
{
	NormConversionType eType = NormConversionType_eUnicode;
	switch(type)
	{
	case ConvType_Legacy_to_from_Legacy:
	case ConvType_Legacy_to_Legacy:
	case ConvType_Unicode_to_from_Legacy:
	case ConvType_Unicode_to_Legacy:
		eType = NormConversionType_eLegacy;
		break;

	case ConvType_Legacy_to_from_Unicode:
	case ConvType_Legacy_to_Unicode:
	case ConvType_Unicode_to_from_Unicode:
	case ConvType_Unicode_to_Unicode:
		eType = NormConversionType_eUnicode;
		break;

	default:
		ASSERT(false);
		break;
	};

	return eType;
}

inline BOOL IsEmpty(const CComBSTR& str)
{
	return !(str && !(str == ""));
}

inline BOOL IsEmpty(const BSTR& str)
{
	return (str == 0);
}

inline int FindSubStr(const BSTR& str, LPCTSTR lpszSub, int nStart = 0)
{
	if( IsEmpty(str) )
		return -1;

	USES_CONVERSION;
	LPCTSTR lpData = OLE2T(str);

	int nLength = (int)_tcslen(lpData);
	if (nStart > nLength)
		return -1;

	// find first matching substring
	LPCTSTR lpsz = _tcsstr(lpData + nStart, lpszSub);

	// return -1 for not found, distance from beginning otherwise
	return (lpsz == NULL) ? -1 : (int)(lpsz - lpData);
}

inline int FindOneOf(const CComBSTR& str, LPCTSTR lpszCharSet)
{
	if( IsEmpty(str) )
		return -1;

	USES_CONVERSION;
	LPCTSTR lpData = OLE2T(str);
	LPCTSTR lpsz = _tcspbrk(lpData, lpszCharSet);
	return (lpsz == NULL) ? -1 : (int)(lpsz - lpData);
}

inline int ReverseFind(const BSTR& str, TCHAR ch)
{
	// make sure that the bstr isn't empty
	if( IsEmpty(str) )
		return -1;

	// find last single character
	USES_CONVERSION;
	LPCTSTR lpData = OLE2T(str);
	LPCTSTR lpsz = _tcsrchr(lpData, (_TUCHAR) ch);

	// return -1 if not found, distance from beginning otherwise
	return (lpsz == NULL) ? -1 : (int)(lpsz - lpData);
}

// if str is "C:\file.doc", then strFilename will be "C:\file" and strExt will be ".doc"
inline BOOL GetFileExtn(const BSTR& str, CComBSTR& strFilename, CComBSTR& strExt)
{
	int nPeriodIndex = ReverseFind(str,'.');
	if( nPeriodIndex == -1 )
		return false;
	strFilename.Append(str,nPeriodIndex);
	strExt = &str[nPeriodIndex];
	return true;
}

inline BOOL IsFileBased(const BSTR& str)
{
	int nPeriodIndex = ReverseFind(str,'.');
	return (nPeriodIndex != -1);
}

// keys from EncCnvtrs/EncConverters.cs, but too hard to get (in CPP) from managed assemblies
extern const CString CNVTRS_ROOT;
