/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgCPWordTokenizer.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LGCPWORDTOKENIZER_INCLUDED
#define LGCPWORDTOKENIZER_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: LgCPWordTokenizer
Description: A Tokenizer that finds word breaks by looking for sequences of word-forming
			 tokens.  A word-forming token is a letter, capital or lowercase (as recognized
			 by the built-in function isalpha.
Hungarian: cpwt
----------------------------------------------------------------------------------------------*/
class LgCPWordTokenizer :
	public ILgTokenizer,
	public ISimpleInit
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgCPWordTokenizer();
	virtual ~LgCPWordTokenizer();

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

	// ISimpleInit Methods
	STDMETHOD(InitNew)(const BYTE * prgb, int cb);
	STDMETHOD(get_InitializationData)(BSTR * pbstr);

	// ILgTokenizer methods
	STDMETHOD(GetToken)(OLECHAR * prgchInput, int cch, int * pichMin, int * pichLim);
	STDMETHOD(get_TokenStart)(BSTR bstrInput, int ichFirst, int *pichMin);
	STDMETHOD(get_TokenEnd)(BSTR bstrInput, int ichFirst, int *pichLim);

	// Member variable access

	// Other public methods

protected:
	// Member variables
	long m_cref;				// standard COM ref count

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
	bool IsWordforming(OLECHAR ch)
	{
		return isalpha(ch);
	}
};
#endif  //LGCPWORDTOKENIZER_INCLUDED
