/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: ConvertString.h
Responsibility: Darrell Zook
Last reviewed:

	Main header file for ConvertString.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef _CONVERTSTRING_H_
#define _CONVERTSTRING_H_

typedef enum {
	kctAToW,
	kctWToA,
	kctWToW
} CallbackType;

typedef enum
{
	kctConvert,
	kctConvertArray,
	kctConvertCB,
} ConvertType;


/*----------------------------------------------------------------------------------------------
	This class converts a string from 8-bit to 16-bit.
----------------------------------------------------------------------------------------------*/
class CsConvertAToW : public ICsConvertAToW
{
public:
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ICsConvertAToW methods.
	STDMETHOD(Initialize)(BSTR bstrTable, BOOL fFileName, ICsCallbackAToW * pccaw);
	STDMETHOD(Convert)(byte * prgchSrc, int cchSrc, BSTR * pbstrDst);
	STDMETHOD(ConvertRgch)(byte * prgchSrc, int cchSrc, OLECHAR * prgchDst, int cchDst,
		ICsCallbackAToW * pccaw, int * pcchNeed);
	STDMETHOD(ConvertCallback)(byte * prgchSrc, int cchSrc, int cchChunk,
		ICsCallbackAToW * pccaw);

protected:
	CsConvertAToW();
	~CsConvertAToW();

	HRESULT ConvertCore(byte * prgchSrc, int cchSrc, OLECHAR ** pprgchDst, int cchDst,
		int * pcchNeed, ConvertType ct, ICsCallbackAToW * pccaw);
	bool ConvertChunk(byte * prgchSrc, int * pcchSrc, OLECHAR * prgchDst, int * pcchDst,
		int cchSrcProcessed, ICsCallbackAToW * pccaw);

	TrieLevel * m_ptl;
	long m_cref;
};


/*----------------------------------------------------------------------------------------------
	This class converts a string from 16-bit to 8-bit.
----------------------------------------------------------------------------------------------*/
class CsConvertWToA : public ICsConvertWToA
{
public:
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ICsConvertWToA methods.
	STDMETHOD(Initialize)(BSTR bstrTable, BOOL fFileName, ICsCallbackWToA * pccwa);
	STDMETHOD(Convert)(BSTR bstrSrc, byte ** pprgchDst, int * pcchNeed);
	STDMETHOD(ConvertRgch)(OLECHAR * prgchSrc, int cchSrc, byte * prgchDst, int cchDst,
		ICsCallbackWToA * pccwa, int * pcchNeed);
	STDMETHOD(ConvertCallback)(OLECHAR * prgchSrc, int cchSrc, int cchChunk,
		ICsCallbackWToA * pccwa);

protected:
	CsConvertWToA();
	~CsConvertWToA();

	HRESULT ConvertCore(OLECHAR * prgchSrc, int cchSrc, byte ** pprgchDst, int cchDst,
		int * pcchNeed, ConvertType ct, ICsCallbackWToA * pccwa);
	bool ConvertChunk(OLECHAR * prgchSrc, int * pcchSrc, byte * prgchDst, int * pcchDst,
		int cchSrcProcessed, ICsCallbackWToA * pccwa);

	TrieLevel * m_ptl;
	long m_cref;
};


/*----------------------------------------------------------------------------------------------
	This class converts a string from 16-bit to 16-bit.
----------------------------------------------------------------------------------------------*/
class CsConvertWToW : public ICsConvertWToW
{
public:
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ICsConvertWToW methods.
	STDMETHOD(Initialize)(BSTR bstrTable, BOOL fFileName, ICsCallbackWToW * pccww);
	STDMETHOD(Convert)(BSTR bstrSrc, BSTR * pbstrDst);
	STDMETHOD(ConvertRgch)(OLECHAR * prgchSrc, int cchSrc, OLECHAR * prgchDst, int cchDst,
		ICsCallbackWToW * pccww, int * pcchNeed);
	STDMETHOD(ConvertCallback)(OLECHAR * prgchSrc, int cchSrc, int cchChunk,
		ICsCallbackWToW * pccww);

protected:
	CsConvertWToW();
	~CsConvertWToW();

	HRESULT ConvertCore(OLECHAR * prgchSrc, int cchSrc, OLECHAR ** pprgchDst, int cchDst,
		int * pcchNeed, ConvertType ct, ICsCallbackWToW * pccww);
	bool ConvertChunk(OLECHAR * prgchSrc, int * pcchSrc, OLECHAR * prgchDst, int * pcchDst,
		int cchSrcProcessed, ICsCallbackWToW * pccww);

	TrieLevel * m_ptl;
	long m_cref;
};

#endif // _CONVERTSTRING_H_