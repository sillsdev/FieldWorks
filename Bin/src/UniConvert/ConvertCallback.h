/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: ConvertCallback.h
Responsibility: Darrell Zook
Last reviewed:

	Header file for the CsCallback classes.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef _CONVERTCALLBACK_H_
#define _CONVERTCALLBACK_H_

/*----------------------------------------------------------------------------------------------
	This class is the callback for converting a string from 8-bit to 16-bit.
----------------------------------------------------------------------------------------------*/
class CsCallbackAToW : public ICsCallbackAToW
{
public:
	static HRESULT Create(ConvertProcess * pcp, ICsCallbackAToW ** ppccaw);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ICsCallbackAToW methods.
	STDMETHOD(HaveText)(OLECHAR * prgch, int cch, int cbCompleted);
	STDMETHOD(InitError)(InitErrorCode kierr, int iInvalidLine, BSTR bstrInvalidLine, BOOL * pfContinue);
	STDMETHOD(ProcessError)(int ichInput, BOOL * pfContinue);

protected:
	CsCallbackAToW(ConvertProcess * pcp);
	~CsCallbackAToW();

	long m_cref;
	ConvertProcess * m_pcp;
};


/*----------------------------------------------------------------------------------------------
	This class is the callback for converting a string from 16-bit to 8-bit.
----------------------------------------------------------------------------------------------*/
class CsCallbackWToA : public ICsCallbackWToA
{
public:
	static HRESULT Create(ConvertProcess * pcp, ICsCallbackWToA ** ppccwa);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ICsCallbackWToA methods.
	STDMETHOD(HaveText)(byte * prgch, int cch, int cchCompleted);
	STDMETHOD(InitError)(InitErrorCode kierr, int iInvalidLine, BSTR bstrInvalidLine, BOOL * pfContinue);
	STDMETHOD(ProcessError)(int ichInput, BOOL * pfContinue);

protected:
	CsCallbackWToA(ConvertProcess * pcp);
	~CsCallbackWToA();

	long m_cref;
	ConvertProcess * m_pcp;
};

#endif // !_CONVERTCALLBACK_H_