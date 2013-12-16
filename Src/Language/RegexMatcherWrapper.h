/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2002-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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
