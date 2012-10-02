/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: RegexMatcherWrapper.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	See the cpp.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RegexMatcherWrapper_INCLUDED
#define RegexMatcherWrapper_INCLUDED

class RegexMatcherWrapper :
	public IRegexMatcher
{
public:
	//:> Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	//:> Constructors/destructors/etc.
	RegexMatcherWrapper();
	virtual ~RegexMatcherWrapper();


	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}


	STDMETHOD(Init)(BSTR bstrPattern, ComBool fMatchCase);
	STDMETHOD(Reset)(BSTR bstrInput);
	STDMETHOD(Find)(int ich, ComBool * pfFound);
	STDMETHOD(get_Start)(int igroup, int * pich);
	STDMETHOD(get_End)(int igroup, int * pich);
	STDMETHOD(get_ErrorMessage)(BSTR * pbstrMsg);

protected:
	//:> Member variables
	long m_cref;
	RegexMatcher * m_pmatcher;
	UnicodeString * m_pusInput;
	UErrorCode m_status;
};

#endif  // RegexMatcherWrapper_INCLUDED
