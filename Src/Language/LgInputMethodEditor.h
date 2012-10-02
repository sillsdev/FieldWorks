/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgInputMethodEditor.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LgInputMethodEditor_INCLUDED
#define LgInputMethodEditor_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: LgInputMethodEditor
Description:
Hungarian: ime
----------------------------------------------------------------------------------------------*/
class LgInputMethodEditor :
	public ILgInputMethodEditor,
	public ISimpleInit
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgInputMethodEditor();
	virtual ~LgInputMethodEditor();

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

	// IRenderEngine methods
	STDMETHOD(Setup)();

	STDMETHOD(Replace)(BSTR bstrInput, ITsTextProps* pttpInput, ITsStrBldr* ptsbOld,
		int ichMin, int ichLim, int* pichModMin, int* pichModLim, int* pichIP);

	STDMETHOD(Backspace)(int pichStart, int cactBackspace, ITsStrBldr* ptsbOld,
		int* pichModMin, int* pichModLim, int* pichIP, int* pcactBsRemaining);

	STDMETHOD(DeleteForward)(int pichStart, int cactDelForward, ITsStrBldr* ptsbOld,
		int* pichModMin, int* pichModLim, int* pichIP, int* pcactDfRemaining);

	STDMETHOD(IsValidInsertionPoint)(int ich, ITsString* ptss, BOOL* pfValid);

protected:
	// Member variables
	long m_cref;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
//	void AdjustEndForWidth(IVwGraphics * pvg);
	void FindLineBreak(const byte * prglbs, const int ichMin, const int ichLim,
								const LgLineBreak lbrkRequired, int& ichBreak, int& ichDim);
};
DEFINE_COM_PTR(LgInputMethodEditor);

#endif  //LgInputMethodEditor_INCLUDED
