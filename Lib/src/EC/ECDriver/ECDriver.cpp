// ECDriver.cpp : Defines the entry point for the DLL application.
//

#include "stdafx.h"
#include "ECDriver.h"
#include "SilEncConverter.h"

#ifdef _MANAGED
#pragma managed(push, off)
#endif

BOOL APIENTRY DllMain( HMODULE hModule,
					   DWORD  ul_reason_for_call,
					   LPVOID lpReserved
					 )
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

#ifdef _MANAGED
#pragma managed(pop)
#endif

// This is an example of an exported function.
ECDRIVER_API BOOL IsEcInstalled(void)
{
	CRegKey keyRegEC;
	return ((keyRegEC.Open(HKEY_LOCAL_MACHINE, _T("SOFTWARE\\SIL\\SilEncConverters40"), KEY_READ) == ERROR_SUCCESS)
		|| (keyRegEC.Open(HKEY_LOCAL_MACHINE, _T("SOFTWARE\\SIL\\SilEncConverters31"), KEY_READ) == ERROR_SUCCESS)
		|| (keyRegEC.Open(HKEY_LOCAL_MACHINE, _T("SOFTWARE\\SIL\\SilEncConverters30"), KEY_READ) == ERROR_SUCCESS)
		|| (keyRegEC.Open(HKEY_LOCAL_MACHINE, _T("SOFTWARE\\SIL\\SilEncConverters22"), KEY_READ) == ERROR_SUCCESS));
}

CSimpleMap<CStringW,CSilEncConverter*> m_mapECs;

void SetEncConverter(const CStringW& strConverterName, CSilEncConverter* pEC)
{
	int nIndex;
	if ((nIndex = m_mapECs.FindKey(strConverterName)) != -1)
	{
		CSilEncConverter* p = m_mapECs.GetValueAt(nIndex);
		m_mapECs.RemoveAt(nIndex);
		delete p;
	}

	m_mapECs.Add(strConverterName, pEC);
}

CSilEncConverter* GetEncConverter(const CStringW& strConverterName)
{
	CSilEncConverter* pEC = 0;
	int nIndex;
	if ((nIndex = m_mapECs.FindKey(strConverterName)) != -1)
	{
		pEC = m_mapECs.GetValueAt(nIndex);
	}
	else
	{
		pEC = new CSilEncConverter();
		SetEncConverter(strConverterName, pEC);
	}

	ATLASSERT(pEC != 0);
	return pEC;
}

ECDRIVER_API HRESULT EncConverterSelectConverterA(LPSTR lpszConverterName, BOOL& bDirectionForward, int& eNormOutputForm)
{
	CSilEncConverter* pEC = new CSilEncConverter();
	HRESULT hr = pEC->AutoSelect();
	if (hr == S_OK)
	{
		strcpy(lpszConverterName, CW2A(pEC->ConverterName, 65001));
		bDirectionForward = pEC->DirectionForward;
		eNormOutputForm = pEC->NormalizeOutput;
		SetEncConverter(pEC->ConverterName, pEC);
	}
	else
		delete pEC;

	return hr;
}

ECDRIVER_API HRESULT EncConverterSelectConverterW(LPWSTR lpszConverterName, BOOL& bDirectionForward, int& eNormOutputForm)
{
	CSilEncConverter* pEC = new CSilEncConverter();
	HRESULT hr = pEC->AutoSelect();
	if (hr == S_OK)
	{
		wcscpy(lpszConverterName, (LPCWSTR)pEC->ConverterName);
		bDirectionForward = pEC->DirectionForward;
		eNormOutputForm = pEC->NormalizeOutput;
		SetEncConverter(lpszConverterName, pEC);
	}
	else
		delete pEC;

	return hr;
}

ECDRIVER_API HRESULT EncConverterInitializeConverterA(LPCSTR lpszConverterName, BOOL bDirectionForward, int eNormOutputForm)
{
	CStringW strConverterName = CA2W(lpszConverterName, 65001);
	CSilEncConverter* pEC = GetEncConverter(strConverterName);
	ATLASSERT(pEC != 0);
	return pEC->Initialize(strConverterName, bDirectionForward, eNormOutputForm);
}

ECDRIVER_API HRESULT EncConverterInitializeConverterW(LPCWSTR lpszConverterName, BOOL bDirectionForward, int eNormOutputForm)
{
	CSilEncConverter* pEC = GetEncConverter(lpszConverterName);
	ATLASSERT(pEC != 0);
	return pEC->Initialize(lpszConverterName, bDirectionForward, eNormOutputForm);
}

ECDRIVER_API HRESULT EncConverterConvertStringA(LPCSTR lpszConverterName, LPCSTR lpszInput, LPSTR lpszOutput, int nOutputLen)
{
	CStringW strConverterName = CA2W(lpszConverterName, 65001);
	CSilEncConverter* pEC = GetEncConverter(strConverterName);
	if (!(*pEC))
		pEC->Initialize(pEC->ConverterName, pEC->DirectionForward, pEC->NormalizeOutput);

	if (!(*pEC))
		return /*NameNotFound*/ -7;

	int nCodePage = (pEC->IsInputLegacy()) ? pEC->CodePageInput() : /* UTF8 */ 65001;

	// EncConverter's interface is easiest when wide
	CStringW strInput = CA2W(lpszInput, nCodePage);

	CStringW strOutputW = pEC->Convert(strInput);

	nCodePage = (pEC->IsOutputLegacy()) ? pEC->CodePageOutput() : /* UTF8 */ 65001;

	CStringA strOutput = CW2A(strOutputW, nCodePage);

	strncpy(lpszOutput, (LPCSTR)strOutput, nOutputLen);
	return S_OK;
}

ECDRIVER_API HRESULT EncConverterConvertStringW(LPCWSTR lpszConverterName, LPCWSTR lpszInput, LPWSTR lpszOutput, int nOutputLen)
{
	CSilEncConverter* pEC = GetEncConverter(lpszConverterName);
	if (!(*pEC))
		pEC->Initialize(pEC->ConverterName, pEC->DirectionForward, pEC->NormalizeOutput);

	if (!(*pEC))
		return /*NameNotFound*/ -7;

	CStringW strOutput = pEC->Convert(lpszInput);

	wcsncpy(lpszOutput, (LPCWSTR)strOutput, nOutputLen);
	return S_OK;
}

ECDRIVER_API HRESULT EncConverterConverterDescriptionA(LPCSTR lpszConverterName, LPSTR lpszDescription, int nDescriptionLen)
{
	CStringW strConverterName = CA2W(lpszConverterName, 65001);
	CSilEncConverter* pEC = GetEncConverter(strConverterName);
	if (!(*pEC))
		return /*NameNotFound*/ -7;

	// write it out as UTF-8, just in case it has Unicode data in it
	strncpy(lpszDescription, CW2A(pEC->Description(), 65001), nDescriptionLen);
	return S_OK;
}

ECDRIVER_API HRESULT EncConverterConverterDescriptionW(LPCWSTR lpszConverterName, LPWSTR lpszDescription, int nDescriptionLen)
{
	CSilEncConverter* pEC = GetEncConverter(lpszConverterName);
	if (!(*pEC))
		return /*NameNotFound*/ -7;

	wcsncpy(lpszDescription, (LPCWSTR)pEC->Description(), nDescriptionLen);
	return S_OK;
}
