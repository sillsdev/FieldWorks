/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgKeymanHandler.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LgKeymanHandler_INCLUDED
#define LgKeymanHandler_INCLUDED


#if !WIN32
#include "ViewsTlb.h"
DEFINE_COM_PTR(IIMEKeyboardSwitcher);
class KeyboardSwitcher;
#define CLSID_KeyboardSwitcher __uuidof(KeyboardSwitcher)
#endif

/*----------------------------------------------------------------------------------------------
Class: LgKeymanHandler
Description: A class that manages Keyman, being able to invoke a keyboard, find out which
	one is active, find out which keyboards are available, return the windows message that
	Keyman sends when a keyboard is selected, and so forth.
Hungarian: lkh
----------------------------------------------------------------------------------------------*/
class LgKeymanHandler :
	public ILgKeymanHandler
{
public:

	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgKeymanHandler();
	virtual ~LgKeymanHandler();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	STDMETHOD(Init)(ComBool fForce);
	STDMETHOD(Close)();
	STDMETHOD(get_NLayout)(int * pclayout);
	STDMETHOD(get_Name)(int ilayout, BSTR * pbstrName);
	STDMETHOD(get_ActiveKeyboardName)(BSTR * pbstrName);
	STDMETHOD(put_ActiveKeyboardName)(BSTR bstrName);
	STDMETHOD(get_KeymanWindowsMessage)(int * pwm);

	// Other public methods
protected:
	// Member variables
	long m_cref; // standard COM ref count

	bool InitInternal();
	void ThrowErrorWithInfo(HRESULT hrErr, int stidDescription);

#if !WIN32
	// C# COM object that switches keyboards.
	IIMEKeyboardSwitcherPtr m_qkbs;
#endif
};
#endif  //LgKeymanHandler_INCLUDED
