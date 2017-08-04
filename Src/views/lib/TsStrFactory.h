/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TsStrFactory.h
Responsibility: Jeff Gayle
Last reviewed:

	Implementation of ITsStringFactory.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TsStrFactory_H
#define TsStrFactory_H 1

/*----------------------------------------------------------------------------------------------
	Implements ITsStrFactory.
	Hungarian: tsf / ztsf.
----------------------------------------------------------------------------------------------*/
class TsStrFact : public ITsStrFactory
{
public:
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void);
	STDMETHOD_(UCOMINT32, Release)(void);

	// ITsStrFactory methods.
	STDMETHOD(MakeString)(BSTR bstr, int ws, ITsString ** pptss);
	STDMETHOD(MakeStringRgch)(const OLECHAR * prgch, int cch, int ws, ITsString ** pptss);
	STDMETHOD(MakeStringWithPropsRgch)(const OLECHAR * prgch, int cch, ITsTextProps * pttp,
		ITsString ** pptss);

	// Builder methods.
	STDMETHOD(GetBldr)(ITsStrBldr ** pptsb);
	STDMETHOD(GetIncBldr)(ITsIncStrBldr ** pptisb);

	// Return an empty TsString in the given writing system.
	STDMETHOD(EmptyString)(int ws, ITsString ** pptss);

private:

	friend class ViewsGlobals;

	ComHashMap<int, ITsString> m_hmwsqtssEmptyStrings;
#ifdef WIN32
	IUnknownPtr m_qunkMarshaler;
#endif
	Mutex m_mutex;

	TsStrFact(void)
	{
		// Don't call ModuleAddRef since there is a global singleton TsStrFact. Its
		// AddRef and Release call ModuleAddRef and ModuleRelease.
#ifdef WIN32
		CoCreateFreeThreadedMarshaler(this, &m_qunkMarshaler);
#endif
	}
};


#endif // !TsStrFactory_H
