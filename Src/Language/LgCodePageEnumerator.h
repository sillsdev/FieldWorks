/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgCodePageEnumerator.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LgCodePageEnumerator_INCLUDED
#define LgCodePageEnumerator_INCLUDED

DEFINE_COM_PTR(IEnumCodePage);
DEFINE_COM_PTR(IMultiLanguage2);

/*----------------------------------------------------------------------------------------------
Class: LgCodePageEnumerator
Description:
Hungarian: lcpe
----------------------------------------------------------------------------------------------*/
class LgCodePageEnumerator : ILgCodePageEnumerator
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgCodePageEnumerator();
	virtual ~LgCodePageEnumerator();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// ILgCodePageEnumerator Methods
	STDMETHOD(Init)();
	STDMETHOD(Next)(int * pnId, BSTR * pbstrName);

protected:
	// Member variables
	long m_cref;

	IEnumCodePagePtr m_qecp;
};

DEFINE_COM_PTR(ITfInputProcessorProfiles);
/*----------------------------------------------------------------------------------------------
Class: LgLanguageEnumerator
Description:
Hungarian: lcpe
----------------------------------------------------------------------------------------------*/
class LgLanguageEnumerator : ILgLanguageEnumerator
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgLanguageEnumerator();
	virtual ~LgLanguageEnumerator();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// ILgLanguageEnumerator Methods
	STDMETHOD(Init)();
	STDMETHOD(Next)(int * pnId, BSTR * pbstrName);

protected:
	// Member variables
	long m_cref;

	LANGID  *m_prgLangIds; // Array set up by init and freed by destructor
	ULONG   m_ulCount; // Count of langids in m_prgLangIds
	ULONG m_iLangId; // Supports enumeration by identifying current position.
};
#endif  //LgCodePageEnumerator_INCLUDED
