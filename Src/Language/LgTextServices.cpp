/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgTextServices.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
//:> Any other headers (not precompiled).

#if WIN32
DEFINE_COM_PTR(ITfInputProcessorProfiles);
DEFINE_COM_PTR(IEnumTfLanguageProfiles);
#endif

#undef THIS_FILE
DEFINE_THIS_FILE

#undef ENABLE_TSF
#define ENABLE_TSF

#undef Tracing_KeybdSelection
//#define Tracing_KeybdSelection

#undef TRACING_KEYMAN
//#define TRACING_KEYMAN

#ifndef WIN32
static const GUID KeyboardSwitcher_guid("4ED1E8bC-DAdE-11DE-B350-0019DBf4566E");

#define CLSID_KeyboardSwitcher KeyboardSwitcher_guid
#endif

//:>********************************************************************************************
//:>	Forward declarations.
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables.
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Constructor/Destructor.
//:>********************************************************************************************


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
// Generic factory used to create an instance of LgTextServices with CoCreateInstance.
static GenericFactory g_fact(
	_T("SIL.Language1.TextServices"),
	&CLSID_LgTextServices,
	_T("SIL Text Services"),
	_T("Apartment"),
	&LgTextServices::CreateCom);

/*----------------------------------------------------------------------------------------------
	Called by the GenericFactory to "create" an ILgTextServices; it just returns the global one.
----------------------------------------------------------------------------------------------*/
void LgTextServices::CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	CheckHr(LanguageGlobals::g_lts.QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IUnknown Methods.
//:>********************************************************************************************
// Get a pointer to the interface identified as iid.
STDMETHODIMP LgTextServices::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgTextServices *>(this));
	else if (riid == IID_ILgTextServices)
		*ppv = static_cast<ILgTextServices *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this,IID_ILgTextServices);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	ILgTextServices methods.
//:>********************************************************************************************

#if WIN32
const CLSID kclsidKMTipTextService = { 0x7ba04432, 0x8609, 0x4fe6, {0xbf,
	0xf7, 0x97, 0x10, 0x91, 0xde, 0x09, 0x33} };
#else
int textServicesArray4[] = {0xbf, 0xf7, 0x97, 0x10, 0x91, 0xde, 0x09, 0x33};
int textServicesArray[] = {0x7ba04432, 0x8609, 0x4fe6, *textServicesArray4};
const CLSID kclsidKMTipTextService(*textServicesArray);
#endif

#if WIN32
static INT8 s_WinVersion = -1;

/*----------------------------------------------------------------------------------------------
	Return true if we're running on Windows 2000 or newer; return false if we're running on
	a Windows version older than Windows 2000 or on a different platform.
----------------------------------------------------------------------------------------------*/
bool IsWin2kOrHigher()
{
	if (s_WinVersion < 0)
	{
		// Try calling GetVersionEx using the OSVERSIONINFOEX structure,
		// If that fails, we're too old.
		OSVERSIONINFOEX osvi;
		ZeroMemory(&osvi, sizeof(OSVERSIONINFOEX));
		osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);

		if (::GetVersionEx((OSVERSIONINFO *) &osvi) &&
			osvi.dwPlatformId == VER_PLATFORM_WIN32_NT)
		{
			Assert(osvi.dwMajorVersion < 128);
			s_WinVersion = (INT8)osvi.dwMajorVersion;
		}
		else
			s_WinVersion = 0;
	}

	return s_WinVersion >= 5;
}
#else
	// TODO-Linux: does this need porting?
#endif

/*----------------------------------------------------------------------------------------------
	Returns true if the current keyman keyboard is different from the desired keyman keyboard.
----------------------------------------------------------------------------------------------*/
bool IsKeyboardDifferent(BSTR bstrDesiredKeymanKbd, BSTR bstrActiveKeymanKbd)
{
#if WIN32
	return wcscmp(bstrActiveKeymanKbd ? bstrActiveKeymanKbd : L"",
		bstrDesiredKeymanKbd ? bstrDesiredKeymanKbd : L"") != 0;
#else
	// TODO-Linux: does this need porting?
	return false;
#endif
}

/*----------------------------------------------------------------------------------------------
	Turn off the keyman keyboard
----------------------------------------------------------------------------------------------*/
void TurnOffKeymanKbd(BSTR * pbstrActiveKeymanKbd)
{
#if WIN32
	// It seems sometimes to be necessary to explicitly turn Keyman off, though it's not
	// supposed to be. One reason is that on loss of focus, C# code loses track of the
	// current keyboard, so *pbstrActiveKeymanKbd cannot be relied on.
	//if (BstrLen(*pbstrActiveKeymanKbd))
	ILgKeymanHandlerPtr qkh;
	qkh.CreateInstance(CLSID_LgKeymanHandler);
	CheckHr(qkh->put_ActiveKeyboardName(NULL));
	if (*pbstrActiveKeymanKbd)
		::SysFreeString(*pbstrActiveKeymanKbd);
	*pbstrActiveKeymanKbd = NULL;
#else
	// TODO-Linux: does this need porting?
#endif
}

/*----------------------------------------------------------------------------------------------
	Set the keyboard the Windows API way
----------------------------------------------------------------------------------------------*/
void SetKeyboard_Windows(int lcid)
{
#if WIN32
#ifdef Tracing_KeybdSelection
	StrAnsi sta;
	sta.Format("LgTextServices::SetKeyboard(%d) [not Keyman]%n", lcid);
	::OutputDebugStringA(sta.Chars());
#endif
	int nLangId = LANGIDFROMLCID(lcid);
	if (nLangId == 0)
		nLangId = ::GetSystemDefaultLangID();
	// JohnT: Damien made the change to the commented line below as an attempt to fix FWR-1939.
	// However it appears to cause the worse problem noted in FWR_3308.
	// Passing 0 as the high word selectst the default keyboard for the language.
	// Windows 7 (at least) does a good job of remembering what keyboard the user last wanted
	// for the language. The only problem would be if the user is using the same system language
	// for two writing systems where he wants to use different keyboards. We can't support that yet.
	//HKL hkl = (HKL)(nLangId << 16 | (nLangId & 0xffff));
	HKL hkl = (HKL)nLangId;

	// If we're not activating a Keyman keyboard, activate the appropriate OS IM.
	// Microsoft says we should only do this if we were not able to do it using
	// ActivateLanguageProfile (private communication to JohnT).
#ifdef Tracing_KeybdSelection
	sta.Format("LgTextServices::SetKeyboard(%d) - "
		"hkl = %x, ::GetKeyboardLayout() = %x%n",
		lcid, hkl, ::GetKeyboardLayout(0));
	::OutputDebugStringA(sta.Chars());
#endif
	if (hkl != ::GetKeyboardLayout(0))
	{
		// We need to work out whether we're using at least W2000, because
		// KLF_SETFORPROCESS is not supported before that.
		UINT flags = 0;
		if (IsWin2kOrHigher())
		{
			//Windows 2000 or better, we can use KLF_SETFORPROCESS.
			flags = KLF_SETFORPROCESS;
		}
#ifdef Tracing_KeybdSelection
		StrAnsi sta;
		sta.Format("LgTextServices::SetKeyboard(%d) - "
			"::ActivateKeyboardLayout(%x, %x) [nLangId = %d]\n",
			lcid, hkl, flags, nLangId);
		::OutputDebugStringA(sta.Chars());
#endif
		::ActivateKeyboardLayout(hkl, flags);
		Assert(sizeof(int) >= sizeof(hkl));
	}
#else
	// TODO-Linux: does this need porting?
#endif
}

#ifdef ENABLE_TSF
/*----------------------------------------------------------------------------------------------
	Set the keyboard through TSF
----------------------------------------------------------------------------------------------*/
bool SetKeyboard_TSF(bool fDoingKeyman, int lcid, int * pnActiveLangId)
{
#if WIN32
#ifdef Tracing_KeybdSelection
	StrAnsi sta;
	sta.Format("LgTextServices::SetKeyboard(%d) [making TSF calls]%n", lcid);
	::OutputDebugStringA(sta.Chars());
	sta.Format("LgTextServices::SetKeyboard(%d) - ::GetKeyboardLayout() = %x%n",
		lcid, ::GetKeyboardLayout(0));
	::OutputDebugStringA(sta.Chars());
#endif
	HRESULT hr = S_OK;
	ITfInputProcessorProfilesPtr qtfipp;
	int nLangId = LANGIDFROMLCID(lcid);
	bool fSetInputLang = false;
	// Don't check the HR or use CreateInstance; I (JohnT) think this may fail if TSF is not
	// fully active.
	::CoCreateInstance(CLSID_TF_InputProcessorProfiles, NULL, CLSCTX_ALL,
		IID_ITfInputProcessorProfiles, (void **)&qtfipp);
	if (!qtfipp)
	{
		Warn("Could not get ITfInputProcessorProfiles to set Keyman text service\n");
	}
	else
	{
		// Have to make the language active before we can set the profile;
		// and want to set the language even if we aren't doing Keyman.
		if (*pnActiveLangId != (LANGID)nLangId)
		{
			// Do NOT do this if the right langID is already current. In some bizarre cases
			// during WM_INPUTLANGCHANGED it can lead to a new input-lang-changed in a
			// DIFFERENT language and an infinite loop.
			*pnActiveLangId = (LANGID)nLangId;
			IgnoreHr(hr = qtfipp->ChangeCurrentLanguage((LANGID)nLangId));
		}
#ifdef Tracing_KeybdSelection
		sta.Format(
			"LgTextServices::SetKeyboard(%d) [qtfipp->ChangeCL(%d) => hr = %x]%n",
			lcid, nLangId, hr);
		::OutputDebugStringA(sta.Chars());
#endif
		if (FAILED(hr))
		{
			Warn("failed to change language\n");
		}
		else if (fDoingKeyman)
		{
			// Make sure the Keyman text service is turned on. For some bizarre reason there is
			// no API to just ask for the service to turn on for the langid, we have to do our
			// own search for the profile that corresponds to this langid and text service.
			IEnumTfLanguageProfilesPtr qenum;
	#ifdef Tracing_KeybdSelection
			sta.Format("LgTextServices::SetKeyboard(%d) [qtfipp->EnumLP(%d)]%n",
				lcid, nLangId);
			::OutputDebugStringA(sta.Chars());
			sta.Format("LgTextServices::SetKeyboard(%d) - ::GetKeyboardLayout() = %x%n",
				lcid, ::GetKeyboardLayout(0));
			::OutputDebugStringA(sta.Chars());
	#endif
			IgnoreHr(hr = qtfipp->EnumLanguageProfiles((LANGID)nLangId, &qenum));
			if (FAILED(hr))
			{
				Warn("Could not get enumerator for language profiles\n");
			}
			else
			{
				// If doing keyman try to turn on Keyman text service.
				TF_LANGUAGEPROFILE profile;
				for ( ; ; )
				{
					ULONG cprofile;
					IgnoreHr(hr = qenum->Next(1, &profile, &cprofile));
					if (FAILED(hr) || cprofile != 1)
					{
						Warn("failed to find language profiled for Keyman\n");
						break;
					}
					if (kclsidKMTipTextService == profile.clsid)
					{
						// got it at last!
#ifdef Tracing_KeybdSelection
						StrAnsi sta;
						sta.Format("LgTextServices::SetKeyboard(%d) - "
							"qtfipp->ActivateLanguageProfile(nLangId = %d)\n",
							lcid, nLangId);
						::OutputDebugStringA(sta.Chars());
#endif
						IgnoreHr(hr = qtfipp->ActivateLanguageProfile(
							kclsidKMTipTextService, (LANGID)nLangId, profile.guidProfile));
						if (FAILED(hr))
						{
							Warn("failed to activate language profile\n");
						}
						else
						{
							fSetInputLang = true;
						}
						break;
					}
				}
			}
#ifdef Tracing_KeybdSelection
			sta.Format("LgTextServices::SetKeyboard(%d) [after qtfipp->ChangeCL(%d)]%n",
				lcid, nLangId);
			::OutputDebugStringA(sta.Chars());
			sta.Format("LgTextServices::SetKeyboard(%d) - ::GetKeyboardLayout() = %x%n",
				lcid, ::GetKeyboardLayout(0));
			::OutputDebugStringA(sta.Chars());
#endif
		}
		else
		{
			// this ensures that we switch to the correct keyboard (not Keyman addin) as well as
			// the correct language
			// TODO (DamienD): we could use the TSF interface ITfInputProcessorProfileMgr to change
			// the keyboard, but it is only available on Vista and higher. Is there a benefit to
			// using that interface?
			SetKeyboard_Windows(lcid);
			fSetInputLang = true;
		}
	}
	return fSetInputLang;
#else
	// TODO-Linux: does this need porting?
	return false;
#endif
}
#endif /*ENABLE_TSF*/

/*----------------------------------------------------------------------------------------------
	Set a keyman keyboard
----------------------------------------------------------------------------------------------*/
HRESULT SetKeyboard_Keyman(int lcid, BSTR bstrKeymanKbd, BSTR * pbstrActiveKeymanKbd,
	ComBool * pfSelectLangPending)
{
#if WIN32
#ifdef Tracing_KeybdSelection
	StrAnsi sta;
	sta.Format("LgTextServices::SetKeyboard(%d) [setting Keyman kbd]%n", lcid);
	::OutputDebugStringA(sta.Chars());
#endif
	int nLangId = LANGIDFROMLCID(lcid);
	HRESULT hr = S_OK;
	ILgKeymanHandlerPtr qkh;
	qkh.CreateInstance(CLSID_LgKeymanHandler);
	// Tell Keyman about the particular keyboard (but only if it changed).
	if (IsKeyboardDifferent(bstrKeymanKbd, *pbstrActiveKeymanKbd))
	{
		// Activate the particular layout we want.
		// John Durdin says this next step is necessary.
		//::ActivateKeyboardLayout(::GetKeyboardLayout(0), 0);
		// JohnT: discovered that if we've never set a keyboard before, the current one
		// won't be right, but forcing the right langid into the low word seems to help.
		// Keyman always uses the US English keyboard, which is the magic number we're
		// stuffing into the high word.
		HKL hklDesired = (HKL)(0x04090000 | (nLangId & 0xffff));
#ifdef Tracing_KeybdSelection
		StrAnsi sta;
		sta.Format("LgTextServices::SetKeyboard(%d) - "
			"::ActivateKeyboardLayout(%d [%x], 0) for keyman setup\n",
			lcid, hklDesired, hklDesired);
		::OutputDebugStringA(sta.Chars());
#endif
		::ActivateKeyboardLayout(hklDesired, 0);

		try
		{
			CheckHr(qkh->put_ActiveKeyboardName(bstrKeymanKbd));
#ifdef TRACING_KEYMAN
			StrUni stuMsg;
			stuMsg.Format(L"%b is now the active Keyman keyboard.\n",
				bstrKeymanKbd);
			::OutputDebugStringW(stuMsg.Chars());
#endif
			if (*pbstrActiveKeymanKbd)
				::SysFreeString(*pbstrActiveKeymanKbd);
			CopyBstr(pbstrActiveKeymanKbd, bstrKeymanKbd);
			*pfSelectLangPending = true;
		}
		catch (Throwable& thr)
		{
			hr = thr.Result();
#ifdef TRACING_KEYMAN
			StrAnsi staMsg;
			staMsg.Format("Cannot make %B the active Keyman keyboard!?\n",
				bstrKeymanKbd);
			::OutputDebugStringA(staMsg.Chars());
#endif
			if (BstrLen(*pbstrActiveKeymanKbd))
			{
				// We failed, so ensure it's turned off.
				TurnOffKeymanKbd(pbstrActiveKeymanKbd);
				*pfSelectLangPending = true;
			}
		}
	}
	return hr;
#else
	// TODO-Linux: does this need porting?
	return S_OK;
#endif
}

/*----------------------------------------------------------------------------------------------
	Set the system keyboard and TSF language.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTextServices::SetKeyboard(int lcid, BSTR bstrKeymanKbd, int * pnActiveLangId,
	BSTR * pbstrActiveKeymanKbd, ComBool * pfSelectLangPending)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrKeymanKbd);
	ChkComArgPtr(pnActiveLangId);
	ChkComArgPtr(pbstrActiveKeymanKbd);
	ChkComArgPtr(pfSelectLangPending);
#if WIN32

	HRESULT hr;
	int nLangId = LANGIDFROMLCID(lcid);

	bool fDoingKeyman = BstrLen(bstrKeymanKbd) > 0;
	bool fSetInputLang = false;
#ifdef ENABLE_TSF
	if (IsKeyboardDifferent(bstrKeymanKbd, *pbstrActiveKeymanKbd) ||
		(LANGID)nLangId != (LANGID)*pnActiveLangId)
	{
		fSetInputLang = SetKeyboard_TSF(fDoingKeyman, lcid, pnActiveLangId);
	}
#endif /*ENABLE_TSF*/

	if (fDoingKeyman)
	{
		hr = SetKeyboard_Keyman(lcid, bstrKeymanKbd, pbstrActiveKeymanKbd, pfSelectLangPending);
	}
	else // no keyman keyboard wanted.
	{
		if (!fSetInputLang)
			SetKeyboard_Windows(lcid);

		TurnOffKeymanKbd(pbstrActiveKeymanKbd);
		*pfSelectLangPending = true;
	}
#else
	// C# COM object that switches keyboards.
	IIMEKeyboardSwitcherPtr qkbs;
	qkbs.CreateInstance(CLSID_KeyboardSwitcher);
	qkbs->put_IMEKeyboard(bstrKeymanKbd);
	qkbs->Close();
#endif //WIN32

	END_COM_METHOD(g_fact, IID_ILgTextServices);
}
