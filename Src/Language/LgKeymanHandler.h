/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: LgKeymanHandler.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LgKeymanHandler_INCLUDED
#define LgKeymanHandler_INCLUDED

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
	STDMETHOD(get_ActiveKeyboardName)(BSTR * pbstrName);

	// Other public methods
protected:
	// Member variables
	long m_cref; // standard COM ref count

	bool InitInternal();
	void ThrowErrorWithInfo(HRESULT hrErr, int stidDescription);
};
#endif  //LgKeymanHandler_INCLUDED
