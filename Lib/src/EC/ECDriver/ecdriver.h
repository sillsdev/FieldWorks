// This DLL interface can be used to get access to EncConverter's convert capability
//	and is primarily intended for C and C++ clients.
// To use this, you have to include this 'h' file in the client app, and link with the
//	ECDriver.lib library. And include the ECDriver.DLL AND MSVCRT80.dll in your tool
//	(the later of which has to be done via a merge module).
// Contact silconverters_support@sil.org if you need other things in this interface.

#pragma once
#ifdef ECDRIVER_EXPORTS
#define ECDRIVER_API __declspec(dllexport)
#else
#define ECDRIVER_API __declspec(dllimport)
#endif

#if defined(_STDCALL_SUPPORTED)
#define STDCALL __stdcall    // Declare our calling convention.
#else
#define STDCALL
#endif // STDCALL_SUPPORTED

#ifdef __cplusplus
extern "C" {
#endif

	// this function returns whether a workable version of EncConverters is installed
	//	(it might be from FW, SA, PA, or SILConverters)
	ECDRIVER_API BOOL STDCALL IsEcInstalled();

	// this function can be used to acquire a converter name and other run-time parameters
	//	using the EC auto-select interfaces (i.e. each transducer provides it's own UI, so you
	//	don't have to). If it returns S_OK, then the name to use in the subsequent calls, the
	//	direction flag, and the normalize output flag will be filled in automatically the
	//	parameters. (see snippet below)
	ECDRIVER_API HRESULT STDCALL EncConverterSelectConverterA(LPSTR lpszConverterName, BOOL& bDirectionForward, int& eNormOutputForm);
	ECDRIVER_API HRESULT STDCALL EncConverterSelectConverterW(LPWSTR lpszConverterName, BOOL& bDirectionForward, int& eNormOutputForm);

	// this function can be used to initialize a converter by its name and set its run-time
	//	parameters if you already have configuration information (e.g. from a previous call
	//	to *SelectConverter, but on a subsequent invocation). You must do either this or
	//	*SelectConverter before calling either of the *ConvertString calls below.
	ECDRIVER_API HRESULT STDCALL EncConverterInitializeConverterA(LPCSTR lpszConverterName, BOOL bDirectionForward, int eNormOutputForm);
	ECDRIVER_API HRESULT STDCALL EncConverterInitializeConverterW(LPCWSTR lpszConverterName, BOOL bDirectionForward, int eNormOutputForm);

	// this function can be used to convert a string of narrow bytes using the named converter
	//	this string of narrow bytes ought to be UTF8 if the input to the conversion is Unicode.
	//  You should pass the length of the buffer for the converted result.
	ECDRIVER_API HRESULT STDCALL EncConverterConvertStringA(LPCSTR lpszConverterName, LPCSTR lpszInput, LPSTR lpszOutput, int nOutputLen);

	// this function can be used to convert a string of wide bytes using the named converter
	//	This string of wide bytes ought to be UTF16 if the input to the conversion is Unicode.
	//	It can also be 'hacked-UTF16' if it is, in fact, legacy-encoded data that has been
	//	widened by some code page (IEncConverter->CodePageInput).
	//  You should pass the length of the buffer for the converted result.
	ECDRIVER_API HRESULT STDCALL EncConverterConvertStringW(LPCWSTR lpszConverterName, LPCWSTR lpszInput, LPWSTR lpszOutput, int nOutputLen);

	// this function can be used to get a description from the given converter name
	ECDRIVER_API HRESULT STDCALL EncConverterConverterDescriptionA(LPCSTR lpszConverterName, LPSTR lpszDescription, int nDescriptionLen);
	ECDRIVER_API HRESULT STDCALL EncConverterConverterDescriptionW(LPCWSTR lpszConverterName, LPWSTR lpszDescription, int nDescriptionLen);

	// you can use EncConverterConvertString if your app is compiled for both Unicode and MBCS
	//	support. Note that all four of the following permutations are possible:
	//
	//			Legacy					Unicode
	//	MBCS	narrow byte string		UTF-8
	//	UNICODE	hacked-UTF16			UTF-16

#ifdef UNICODE
#define EncConverterConvertString			EncConverterConvertStringW
#define EncConverterInitializeConverter		EncConverterInitializeConverterW
#define	EncConverterSelectConverter			EncConverterSelectConverterW
#define	EncConverterConverterDescription	EncConverterConverterDescriptionW
#else
#define EncConverterConvertString			EncConverterConvertStringA
#define	EncConverterInitializeConverter		EncConverterInitializeConverterA
#define	EncConverterSelectConverter			EncConverterSelectConverterA
#define	EncConverterConverterDescription	EncConverterConverterDescriptionA
#endif // !UNICODE

#ifdef __cplusplus
}	// Close the extern C.

//	Here are some snippets for using the above:

/*	for C/C++ with _MBCS (i.e. natively narrow (8-bit) byte strings):
// this code is to ask the user to choose a converter
if (IsEcInstalled())
{
	char szConverterName[1000];
	BOOL bDirectionForward = TRUE;
	int eNormFormOutput = 0;
	if (EncConverterSelectConverter(szConverterName, bDirectionForward, eNormFormOutput) == S_OK)
	{
		// input data is narrow bytes (UTF-8 if Unicode)
		CW2A szInput(L"किताब", 65001);
		char szOutput[1000];
		EncConverterConvertString(szConverterName, szInput, szOutput, 1000);
		// szOutput contains the narrow string result (UTF-8 if Unicode)
	}
}

// this code is to initialize a converter when you've already got the initialization information.
if (IsEcInstalled())
{
	char szConverterName[1000];
	strcpy(szConverterName, CW2A(L"हिन्‍दी to Latin", 65001));	// note it could contain Unicode chars
	BOOL bDirectionForward = TRUE;
	int eNormFormOutput = 0;

	// initialize the converter based on these stored configuration properties (but beware,
	//	it may no longer be installed! So do something in the 'else' case to inform user)
	if (EncConverterInitializeConverter(szConverterName, bDirectionForward, eNormFormOutput) == S_OK)
	{
		// input data is narrow bytes (UTF-8 if Unicode)
		CW2A szInput(L"किताब", 65001);
		char szOutput[1000];
		EncConverterConvertString(szConverterName, szInput, szOutput, 1000);
		// szOutput contains the narrow string result (UTF-8 if Unicode)
	}
}
*/

/*	for C/C++ with _UNICODE (i.e. natively wide UTF-16 (possibly 'hacked') strings):
// this code is to ask the user to choose a converter
if (IsEcInstalled())
{
	wchar_t szConverterName[1000];
	BOOL bDirectionForward = TRUE;
	int eNormFormOutput = 0;
	if (EncConverterSelectConverter(szConverterName, bDirectionForward, eNormFormOutput) == S_OK)
	{
		// input data is wide bytes (UTF-16 or 'hacked-UTF16' if Legacy)
		const wchar_t* lpszInput = L"किताब";
		wchar_t szOutput[1000];
		EncConverterConvertString(szConverterName, lpszInput, szOutput, 1000);
		// szOutput contains the wide string result (UTF-16 if Unicode or 'hacked-UTF16' if Legacy)
	}
}

// this code is to initialize a converter when you've already got the initialization information.
if (IsEcInstalled())
{
	wchar_t szConverterName[1000];
	wcscpy(szConverterName, L"हिन्‍दी to Latin");	// note it could contain Unicode chars
	BOOL bDirectionForward = TRUE;
	int eNormFormOutput = 0;

	// initialize the converter based on these stored configuration properties (but beware,
	//	it may no longer be installed! So do something in the 'else' case to inform user)
	if (EncConverterInitializeConverter(szConverterName, bDirectionForward, eNormFormOutput) == S_OK)
	{
		// input data is wide chars (UTF-16 or 'hacked-UTF16' if Legacy)
		const wchar_t* lpszInput = L"किताब";
		wchar_t szOutput[1000];
		EncConverterConvertString(szConverterName, lpszInput, szOutput, 1000);
		// szOutput contains the wide string result (UTF-16 if Unicode or 'hacked-UTF16' if Legacy)
	}
}
*/
#endif
