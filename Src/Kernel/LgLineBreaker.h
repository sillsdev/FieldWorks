/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2002-2016 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LGLINEBREAKER_INCLUDED
#define LGLINEBREAKER_INCLUDED

class LgLineBreaker :
	public ILgLineBreaker,
	public ISimpleInit
{
public:
	//:> Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	//:> Constructors/destructors/etc.
	LgLineBreaker();
	LgLineBreaker(BSTR bstrLocale);
	virtual ~LgLineBreaker();

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

	//:> ISimpleInit Methods
	STDMETHOD(InitNew)(const BYTE * prgb, int cb);
	STDMETHOD(get_InitializationData)(BSTR * pbstr);

	//:> ILgLineBreaker
	STDMETHOD(Initialize)(BSTR bstrLocale);
	STDMETHOD(GetLineBreakProps)(const OLECHAR * prgchIn, int cchIn, byte * prglbpOut);
	STDMETHOD(GetLineBreakInfo)(const OLECHAR * prgchIn, int cchIn, int ichMin,
		int ichLim, byte * prglbsOut, int * pichBreak);
	STDMETHOD(put_LineBreakText)(OLECHAR * prgchIn, int cchMax);
	STDMETHOD(GetLineBreakText)(int cchMax, OLECHAR * prgchOut, int * pcchOut);
	STDMETHOD(LineBreakBefore)(int ichIn, int * pichOut, LgLineBreak * plbWeight);
	STDMETHOD(LineBreakAfter)(int ichIn, int * pichOut, LgLineBreak * plbWeight);

protected:
	//:> Member variables
	long m_cref;
	Locale * m_pLocale;
	BreakIterator * m_pBrkit;
	UnicodeString m_usBrkIt;	// the string that BreakIterator operates on.

	int m_cchBrkMax;  // Measures the size of the text in the BreakIterator.

	//:> Static members
	static const byte s_rglbs[32][32]; // Look-up table for GetLineBreakStatus.

	//:> Constructors/destructors/etc.

	void CleanupBreakIterator();
	void SetupBreakIterator();
};

#endif  // LGLINEBREAKER_INCLUDED
