// TestEC.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "TestEC.h"
#ifdef _DEBUG
#define new DEBUG_NEW
#endif

/*
#using <mscorlib.dll>
using namespace System; // Only include to access the .NET Framework classes
#using "EncCnvtrs.dll"
using namespace EncCnvtrs;
*/

#import "EncCnvtrs.tlb" raw_interfaces_only // must have raw_* switch or the CCom* stuff doesn't work
using namespace EncCnvtrs;
// The one and only application object

CWinApp theApp;

// using namespace std;


void DoTesting(const CString& strModulePath);
int _tmain(int argc, TCHAR* argv[], TCHAR* envp[])
{
	int nRetCode = 0;

	// initialize MFC and print and error on failure
	HMODULE hModule = ::GetModuleHandle(NULL);
	if (!AfxWinInit(hModule, NULL, ::GetCommandLine(), 0))
	{
		// TODO: change error code to suit your needs
		_tprintf(_T("Fatal Error: MFC initialization failed\n"));
		nRetCode = 1;
	}
	else
	{
		TCHAR lpszModulePath[_MAX_PATH+1];
		int nLen = GetModuleFileName(hModule,lpszModulePath,_MAX_PATH);
		CString strModulePath = CString(lpszModulePath).Left(nLen -
#ifdef	_DEBUG
			16);
#else
			18);
#endif

		CoInitialize(NULL);

		try
		{
			DoTesting(strModulePath);
		}
		catch(...)
		{
		}

		CoUninitialize();
	}

	return nRetCode;
}

#define	TECKIT_PROGID	L"EncCnvtrs.TecFormEncConverter"	// who does special conversions for other engines
#define	TECKIT_EFCREQ	L"EncodingFormConversionRequest"	// use as ConverterIdentifier of Initialize

// from ECEncConverter.h (modified)
typedef CComPtr<IEncConverters> IECs;
typedef CComPtr<IEncConverter>  IEC;

#define eUTF8   65001

#define	USE_PRINTF	// turn it off for now.
#ifndef	USE_PRINTF
#ifdef	_DEBUG
#define MyVERIFY(f)          (void) ((f) || printf("File: (%s), Line: (%d)\n", THIS_FILE, __LINE__) )
#else
#define	MyVERIFY(f)
#endif
#else	// USE_PRINTF
#define	MyVERIFY(f)			VERIFY((hr = f) == S_OK)
#endif	// USE_PRINTF

HRESULT HaveTECConvert(IECs& pECs, const CComBSTR& sInput, EncodingForm eFormInput, long ciInput, EncodingForm eFormOutput, CComBSTR& sInputUTF16, long& nNumItems);

CString m_strOutput;

// get a bunch of variables ready for checking the results with.
CString m_strBookLegacyBytes = "�L��"; // Annapurna for 'book'
CComBSTR m_strBookLegacyString = m_strBookLegacyBytes.AllocSysString();
CString	m_strBookUTF8Bytes = "किताब";
CComBSTR m_strBookUTF8String = m_strBookUTF8Bytes.AllocSysString();
CComBSTR m_strBookUTF16 = CA2W("किताब",eUTF8);
CComBSTR m_strBookU16BE, m_strBookU32, m_strBookU32BE;
long m_NumItemU16BE, m_NumItemU32, m_NumItemU32BE;

void ToUnicode(IEC& pEC,const CComBSTR& sInput,int nLen, EncodingForm eFromForm, BOOL bForward);
void ToLegacy(IEC& pEC,const CComBSTR& sInput,int nLen,EncodingForm eFromForm,BOOL bForward);

void DoTesting(const CString& strModulePath)
{
	HRESULT hr = S_OK;
	USES_CONVERSION;
	IECs    pECs;
	MyVERIFY( pECs.CoCreateInstance(L"EncCnvtrs.EncConverters") );
	if( !!pECs )
	{
		printf("have the repository...\n");
		CString strFriendlyName = _T("Schmaboogle");

		// Ask TECkit for the EncodingForm_UTF16BE, EncodingForm_UTF32, and EncodingForm_UTF32BE forms:
		{
			MyVERIFY(HaveTECConvert(pECs, m_strBookUTF16, EncodingForm_UTF16, 0, EncodingForm_UTF16BE, m_strBookU16BE, m_NumItemU16BE));
			MyVERIFY(HaveTECConvert(pECs, m_strBookUTF16, EncodingForm_UTF16, 0, EncodingForm_UTF32, m_strBookU32, m_NumItemU32));
			MyVERIFY(HaveTECConvert(pECs, m_strBookUTF16, EncodingForm_UTF16, 0, EncodingForm_UTF32BE, m_strBookU32BE, m_NumItemU32BE));
		}

		// just in case it exists from a previous (failed) test.
		pECs->Remove(CComVariant(strFriendlyName));

		// start with a bidirectional legacy<>Unicode teckit map
		printf("bidi legacy<>Unicode TEC test...");
		{
			CString strPath = strModulePath + _T("Annapurna.map");
			MyVERIFY(pECs->Add(strFriendlyName.AllocSysString(),
				strPath.AllocSysString(), ConvType_Legacy_to_from_Unicode, L"asdg", L"asdg",
				ProcessTypeFlags_UnicodeEncodingConversion));

			IEC pEC;
			CComVariant strName(strFriendlyName);
			MyVERIFY(pECs->get_Item(strName, &pEC));
			VERIFY( !!pEC );
			{
				CComBSTR sInput;
				int nLen;

				MessageBox(0,_T("Turning on EncConverters Debug mode for testing purposes.\nClick 'Cancel' (in the following series of dialogs) to turn it off"),_T("Testing EncConverters in Debug mode"),MB_OK);
				pEC->put_Debug(-1);

				// First do the ToUnicode permutations.
				nLen = m_strBookLegacyBytes.GetLength();
				sInput = CComBSTR((nLen+1)/2,(LPCOLESTR)(LPCSTR)m_strBookLegacyBytes);
				ToUnicode(pEC,sInput,nLen,EncodingForm_LegacyBytes,-1);

				sInput = m_strBookLegacyString;
				nLen = sInput.Length();
				ToUnicode(pEC,sInput,nLen,EncodingForm_LegacyString,-1);

				// Then do the ToLegacy permutations.
				nLen = m_strBookUTF8Bytes.GetLength();
				sInput = CComBSTR((nLen + 1)/2,(LPCOLESTR)(LPCSTR)m_strBookUTF8Bytes);
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF8Bytes,false);

				sInput = m_strBookUTF8String;
				nLen = m_strBookUTF8String.Length();
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF8String,false);

				sInput = m_strBookUTF16;
				nLen = m_strBookUTF16.Length();
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF16,false);

				sInput = m_strBookU16BE;
				nLen = m_NumItemU16BE;
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF16BE,false);

				sInput = m_strBookU32;
				nLen = m_NumItemU32;
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF32,false);

				sInput = m_strBookU32BE;
				nLen = m_NumItemU32BE;
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF32BE,false);

				// now remove that converter...
				pECs->Remove(strName);
			}
		}

		printf("...complete\nlegacy>Unicode CC test...");
		{
			// try a cctable... first do the legacy to Unicode flavor
			CString strPath = strModulePath + _T("ann2unicode.cct");
			MyVERIFY(pECs->Add(strFriendlyName.AllocSysString(),
				strPath.AllocSysString(),
				ConvType_Legacy_to_Unicode, L"asdg", L"asdg",
				ProcessTypeFlags_UnicodeEncodingConversion));

			IEC pEC;
			CComVariant strName(strFriendlyName);
			MyVERIFY(pECs->get_Item(strName, &pEC));
			VERIFY( !!pEC );
			{
				CComBSTR sInput;
				int nLen;

				MessageBox(0,_T("Now testing Legacy_to_Unicode CcEncConverters.\nClick 'Cancel' (in the following series of dialogs) to turn it off"),_T("Testing EncConverters in Debug mode"),MB_OK);
				pEC->put_Debug(-1);

				// First do the ToUnicode permutations.
				nLen = m_strBookLegacyBytes.GetLength();
				sInput = CComBSTR((nLen+1)/2,(LPCOLESTR)(LPCSTR)m_strBookLegacyBytes);
				ToUnicode(pEC,sInput,nLen,EncodingForm_LegacyBytes,-1);

				sInput = m_strBookLegacyString;
				nLen = sInput.Length();
				ToUnicode(pEC,sInput,nLen,EncodingForm_LegacyString,-1);

				pECs->Remove(strName);
			}
		}
		printf("...complete\nUnicode>legacy CC test...");
		{
			// try a cctable... next do the Unicode to legacy flavor
			CString strPath = strModulePath + _T("unicode2ann.cct");
			MyVERIFY(pECs->Add(strFriendlyName.AllocSysString(),
				strPath.AllocSysString(),
				ConvType_Unicode_to_Legacy, L"asdg", L"asdg",
				ProcessTypeFlags_UnicodeEncodingConversion));

			IEC pEC;
			CComVariant strName(strFriendlyName);
			MyVERIFY(pECs->get_Item(strName, &pEC));
			VERIFY( !!pEC );
			{
				CComBSTR sInput;
				int nLen;

				MessageBox(0,_T("Now testing Unicode_to_Legacy CcEncConverters.\nClick 'Cancel' (in the following series of dialogs) to turn it off"),_T("Testing EncConverters in Debug mode"),MB_OK);
				pEC->put_Debug(-1);

				nLen = m_strBookUTF8Bytes.GetLength();
				sInput = CComBSTR((nLen + 1)/2,(LPCOLESTR)(LPCSTR)m_strBookUTF8Bytes);
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF8Bytes,-1);

				sInput = m_strBookUTF8String;
				nLen = m_strBookUTF8String.Length();
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF8String,-1);

				sInput = m_strBookUTF16;
				nLen = m_strBookUTF16.Length();
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF16,-1);

				sInput = m_strBookU16BE;
				nLen = m_NumItemU16BE;
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF16BE,-1);

				sInput = m_strBookU32;
				nLen = m_NumItemU32;
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF32,-1);

				sInput = m_strBookU32BE;
				nLen = m_NumItemU32BE;
				ToLegacy(pEC,sInput,nLen,EncodingForm_UTF32BE,-1);

				pECs->Remove(strName);
			}
		}

		printf("...complete\nbidi Unicode<>Unicode Code Page test...");
		{
			// try the utf8<>EncodingForm_UTF16 converter
			CString strPath = _T("65001");	// code page converter
			MyVERIFY(pECs->Add(strFriendlyName.AllocSysString(),
				strPath.AllocSysString(),
				ConvType_Unicode_to_from_Unicode, L"asdg", L"asdg",
				(ProcessTypeFlags)(ProcessTypeFlags_UnicodeEncodingConversion | ProcessTypeFlags_CodePageConversion)));

			IEC pEC;
			CComVariant strName(strFriendlyName);
			MyVERIFY(pECs->get_Item(strName, &pEC));
			VERIFY( !!pEC );
			{
				CComBSTR sInput;
				int nLen;

				MessageBox(0,_T("Now testing Unicode_to_from_Unicode CpEncConverters.\nClick 'Cancel' (in the following series of dialogs) to turn it off"),_T("Testing EncConverters in Debug mode"),MB_OK);
				pEC->put_Debug(-1);

				// First do the ToUnicode permutations (since UTF8 is behaving as
				//	Legacy here).
				nLen = m_strBookUTF8Bytes.GetLength();
				sInput = CComBSTR((nLen+1)/2,(LPCOLESTR)(LPCSTR)m_strBookUTF8Bytes);
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF8Bytes,-1);

				sInput = m_strBookUTF8String;
				nLen = sInput.Length();
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF8String,-1);

				sInput = m_strBookUTF16;
				nLen = m_strBookUTF16.Length();
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF16,-1);

				sInput = m_strBookU16BE;
				nLen = m_NumItemU16BE;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF16BE,-1);

				sInput = m_strBookU32;
				nLen = m_NumItemU32;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF32,-1);

				sInput = m_strBookU32BE;
				nLen = m_NumItemU32BE;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF32BE,-1);

				// now turn it reverse and see if it works the other way with all the same
				//	permutations.
				nLen = m_strBookUTF8Bytes.GetLength();
				sInput = CComBSTR((nLen+1)/2,(LPCOLESTR)(LPCSTR)m_strBookUTF8Bytes);
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF8Bytes,false);

				sInput = m_strBookUTF8String;
				nLen = sInput.Length();
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF8String,false);

				sInput = m_strBookUTF16;
				nLen = m_strBookUTF16.Length();
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF16,false);

				sInput = m_strBookU16BE;
				nLen = m_NumItemU16BE;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF16BE,false);

				sInput = m_strBookU32;
				nLen = m_NumItemU32;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF32,false);

				sInput = m_strBookU32BE;
				nLen = m_NumItemU32BE;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF32BE,false);

				pECs->Remove(strName);
			}
		}
		printf("...complete\nLegacy>Legacy CC test with compound converters...");
		{
			// try a legacy>legacy cctable...
			// but do it with two reversable tables daisy-chained together (i.e. so the
			//	returned result will be the same as the original input).
			CString strPath = strModulePath + _T("MyAnn2TrmR.cct");
			MyVERIFY(pECs->Add(CComBSTR(L"MyAnn2TrmR"), strPath.AllocSysString(),
				ConvType_Legacy_to_Legacy, L"asdg", L"asdg", ProcessTypeFlags_Transliteration));

			strPath = strModulePath + _T("MyTrm2AnnR.cct");
			MyVERIFY(pECs->Add(CComBSTR(L"MyTrm2AnnR"), strPath.AllocSysString(),
				ConvType_Legacy_to_Legacy, L"asdg", L"asdg", ProcessTypeFlags_Transliteration));

			MyVERIFY(pECs->AddCompoundConverterStep(strFriendlyName.AllocSysString(),
							CComBSTR(L"MyAnn2TrmR"), true, NormalizeFlags_None));

			MyVERIFY(pECs->AddCompoundConverterStep(strFriendlyName.AllocSysString(),
							CComBSTR(L"MyTrm2AnnR"), true, NormalizeFlags_None));

			IEC pEC;
			CComVariant strName(strFriendlyName);
			MyVERIFY(pECs->get_Item(strName, &pEC));
			VERIFY( !!pEC );
			{
				CComBSTR sInput;
				int nLen;

				MessageBox(0,_T("Now testing Legacy_to_Legacy CmpdEncConverters.\nClick 'Cancel' (in the following series of dialogs) to turn it off"),_T("Testing EncConverters in Debug mode"),MB_OK);
				pEC->put_Debug(-1);

				// Do the permutations.
				nLen = m_strBookLegacyBytes.GetLength();
				sInput = CComBSTR((nLen+1)/2,(LPCOLESTR)(LPCSTR)m_strBookLegacyBytes);
				ToLegacy(pEC,sInput,nLen,EncodingForm_LegacyBytes,-1);

				sInput = m_strBookLegacyString;
				nLen = sInput.Length();
				ToLegacy(pEC,sInput,nLen,EncodingForm_LegacyString,-1);

				pECs->Remove(strName);
				strName = "MyAnn2TrmR";	pECs->Remove(strName);
				strName = "MyTrm2AnnR";	pECs->Remove(strName);
			}
		}
		printf("...complete\nbidi Unicode<>Unicode ICU Converter test...");
		{
			// try the utf8<>EncodingForm_UTF16 converter
			CString strPath = _T("UTF-8");	// code page converter
			HRESULT hr = S_OK;
			MyVERIFY(pECs->Add(strFriendlyName.AllocSysString(),
				strPath.AllocSysString(),
				ConvType_Unicode_to_from_Unicode, L"asdg", L"asdg",
				ProcessTypeFlags_ICUConverter));

			IEC pEC;
			CComVariant strName(strFriendlyName);
			MyVERIFY(pECs->get_Item(strName, &pEC));
			VERIFY( !!pEC );
			{
				CComBSTR sInput;
				int nLen;

				MessageBox(0,_T("Now testing Unicode_to_from_Unicode IcuConvEncConverters.\nClick 'Cancel' (in the following series of dialogs) to turn it off"),_T("Testing EncConverters in Debug mode"),MB_OK);
				pEC->put_Debug(-1);

				// First do the ToUnicode permutations (since UTF8 is behaving as
				//	Legacy here).
				nLen = m_strBookUTF8Bytes.GetLength();
				sInput = CComBSTR((nLen+1)/2,(LPCOLESTR)(LPCSTR)m_strBookUTF8Bytes);
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF8Bytes,-1);

				sInput = m_strBookUTF8String;
				nLen = sInput.Length();
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF8String,-1);

				sInput = m_strBookUTF16;
				nLen = m_strBookUTF16.Length();
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF16,-1);

				sInput = m_strBookU16BE;
				nLen = m_NumItemU16BE;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF16BE,-1);

				sInput = m_strBookU32;
				nLen = m_NumItemU32;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF32,-1);

				sInput = m_strBookU32BE;
				nLen = m_NumItemU32BE;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF32BE,-1);

				// now turn it reverse and see if it works the other way with all the same
				//	permutations.
				nLen = m_strBookUTF8Bytes.GetLength();
				sInput = CComBSTR((nLen+1)/2,(LPCOLESTR)(LPCSTR)m_strBookUTF8Bytes);
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF8Bytes,false);

				sInput = m_strBookUTF8String;
				nLen = sInput.Length();
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF8String,false);

				sInput = m_strBookUTF16;
				nLen = m_strBookUTF16.Length();
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF16,false);

				sInput = m_strBookU16BE;
				nLen = m_NumItemU16BE;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF16BE,false);

				sInput = m_strBookU32;
				nLen = m_NumItemU32;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF32,false);

				sInput = m_strBookU32BE;
				nLen = m_NumItemU32BE;
				ToUnicode(pEC,sInput,nLen,EncodingForm_UTF32BE,false);

				pECs->Remove(strName);
			}
		}
		printf("...complete\n");
	}
}

void ToUnicode(IEC& pEC,const CComBSTR& sInput,int nLen, EncodingForm eFromForm, BOOL bForward)
{
	HRESULT hr = S_OK;
	CComBSTR sOutput;

	// output EncodingForm_UTF8Bytes
	long ciOutput = 0;
	MyVERIFY(pEC->ConvertEx(sInput,eFromForm,nLen,EncodingForm_UTF8Bytes,&ciOutput,NormalizeFlags_None,bForward,&sOutput));
	m_strOutput = CString((LPCSTR)(LPCOLESTR)sOutput,ciOutput);
	VERIFY(m_strOutput == m_strBookUTF8Bytes);

	// do the corresponding Convert method
	MyVERIFY(pEC->put_EncodingIn(eFromForm));
	MyVERIFY(pEC->put_EncodingOut(EncodingForm_UTF8Bytes));
	MyVERIFY(pEC->put_DirectionForward(bForward));
	MyVERIFY(pEC->Convert(sInput,&sOutput));
	m_strOutput = CString((LPCSTR)(LPCOLESTR)sOutput,ciOutput);
	VERIFY(m_strOutput == m_strBookUTF8Bytes);

	// EncodingForm_UTF8String
	ciOutput = 0;
	MyVERIFY(pEC->ConvertEx(sInput,eFromForm,nLen,EncodingForm_UTF8String,&ciOutput,NormalizeFlags_None,bForward,&sOutput));
	VERIFY(sOutput == m_strBookUTF8String);

	MyVERIFY(pEC->put_EncodingOut(EncodingForm_UTF8String));
	MyVERIFY(pEC->Convert(sInput,&sOutput));
	VERIFY(sOutput == m_strBookUTF8String);

	// EncodingForm_UTF16
	ciOutput = 0;
	MyVERIFY(pEC->ConvertEx(sInput,eFromForm,nLen,EncodingForm_UTF16,&ciOutput,NormalizeFlags_None,bForward,&sOutput));
	VERIFY(sOutput == m_strBookUTF16);

	MyVERIFY(pEC->put_EncodingOut(EncodingForm_UTF16));
	MyVERIFY(pEC->Convert(sInput,&sOutput));
	VERIFY(sOutput == m_strBookUTF16);

	// do the implicit one here if LegacyForm is EncodingForm_LegacyString
	if( eFromForm == EncodingForm_LegacyString )
	{
		MyVERIFY(pEC->ConvertToUnicode(sInput,&sOutput));
		VERIFY(sOutput == m_strBookUTF16);
	}

	// EncodingForm_UTF16BE
	ciOutput = 0;
	MyVERIFY(pEC->ConvertEx(sInput,eFromForm,nLen,EncodingForm_UTF16BE,&ciOutput,NormalizeFlags_None,bForward,&sOutput));
	VERIFY(sOutput == m_strBookU16BE);

	MyVERIFY(pEC->put_EncodingOut(EncodingForm_UTF16BE));
	MyVERIFY(pEC->Convert(sInput,&sOutput));
	VERIFY(sOutput == m_strBookU16BE);

	// EncodingForm_UTF32
	ciOutput = 0;
	MyVERIFY(pEC->ConvertEx(sInput,eFromForm,nLen,EncodingForm_UTF32,&ciOutput,NormalizeFlags_None,bForward,&sOutput));
	VERIFY(sOutput == m_strBookU32);

	MyVERIFY(pEC->put_EncodingOut(EncodingForm_UTF32));
	MyVERIFY(pEC->Convert(sInput,&sOutput));
	VERIFY(sOutput == m_strBookU32);

	// EncodingForm_UTF32BE
	ciOutput = 0;
	MyVERIFY(pEC->ConvertEx(sInput,eFromForm,nLen,EncodingForm_UTF32BE,&ciOutput,NormalizeFlags_None,bForward,&sOutput));
	VERIFY(sOutput == m_strBookU32BE);

	MyVERIFY(pEC->put_EncodingOut(EncodingForm_UTF32BE));
	MyVERIFY(pEC->Convert(sInput,&sOutput));
	VERIFY(sOutput == m_strBookU32BE);
}

void ToLegacy
(
	IEC&			pEC,
	const CComBSTR&	sInput,
	int				nLen,
	EncodingForm	eFromForm,
	BOOL			bForward
)
{
	HRESULT hr = S_OK;
	CComBSTR sOutput;

	// EncodingForm_LegacyBytes
	long ciOutput = 0;
	MyVERIFY(pEC->ConvertEx(sInput,eFromForm,nLen,EncodingForm_LegacyBytes,&ciOutput,NormalizeFlags_None,bForward,&sOutput));
	m_strOutput = CString((LPCSTR)(LPCOLESTR)sOutput,ciOutput);
	VERIFY(m_strOutput == m_strBookLegacyBytes);

	// do the corresponding Convert method
	MyVERIFY(pEC->put_DirectionForward(bForward));
	MyVERIFY(pEC->put_EncodingIn(eFromForm));
	MyVERIFY(pEC->put_EncodingOut(EncodingForm_LegacyBytes));
	MyVERIFY(pEC->Convert(sInput,&sOutput));
	m_strOutput = CString((LPCSTR)(LPCOLESTR)sOutput,ciOutput);
	VERIFY(m_strOutput == m_strBookLegacyBytes);

	// EncodingForm_LegacyString
	ciOutput = 0;
	MyVERIFY(pEC->ConvertEx(sInput,eFromForm,nLen,EncodingForm_LegacyString,&ciOutput,NormalizeFlags_None,bForward,&sOutput));
	LPCWSTR lpszOutput = sOutput;
	VERIFY(sOutput == m_strBookLegacyString);

	MyVERIFY(pEC->put_EncodingOut(EncodingForm_LegacyString));
	MyVERIFY(pEC->Convert(sInput,&sOutput));
	VERIFY(sOutput == m_strBookLegacyString);

	// do the implicit one here also if EncodingForm_UTF16
	if( eFromForm == EncodingForm_UTF16 )
	{
		MyVERIFY(pEC->ConvertFromUnicode(sInput, &sOutput));
		VERIFY(sOutput == m_strBookLegacyString);
	}
}

HRESULT HaveTECConvert(IECs& pECs, const CComBSTR& sInput, EncodingForm eFormInput, long ciInput, EncodingForm eFormOutput, CComBSTR& sOutput, long& nNumItems)
{
	return pECs->UnicodeEncodingFormConvert(sInput,eFormInput,ciInput,eFormOutput,&nNumItems,&sOutput);
}
