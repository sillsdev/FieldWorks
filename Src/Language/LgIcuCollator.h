/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgIcuCollator.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LGICUCOLLATOR_H
#define LGICUCOLLATOR_H

class LgIcuCollator;
/*----------------------------------------------------------------------------------------------
	Implmenentation of IIcuCleanupCallback to allow us to be notified of calls to IcuCleanup.
	Note that we do not implement this interface as part of LgIcuCollator, because then the collator
	itself would be registered, and would have a reference count from the manager, and would
	never get deleted. By making it a separate object, the reference count of the engine is
	not increased. The pointer from the object back to the engine is safe, because the engine
	always unregisters and destroys its callback when going away.
	Hungarian cicc
----------------------------------------------------------------------------------------------*/
class CollatorIcuCleanupCallback : public IIcuCleanupCallback
{
	friend class LgIcuCharPropEngine;
public:
	CollatorIcuCleanupCallback(LgIcuCollator * pico);
	virtual ~CollatorIcuCleanupCallback();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	STDMETHOD(DoneCleanup)();
protected:
	long m_cref;
	LgIcuCollator * m_pico;
};
DEFINE_COM_PTR(CollatorIcuCleanupCallback);

/*----------------------------------------------------------------------------------------------
Class: LgIcuCollator
Description:
	This class wraps the ICU collation class.
Hungarian: ico
----------------------------------------------------------------------------------------------*/
class LgIcuCollator : public ILgCollatingEngine
{
public:
	// Static methods
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

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

	//ILgCollatingEngine Methods
	STDMETHOD(get_SortKey)(BSTR bstrValue, LgCollatingOptions colopt, BSTR * pbstrKey);
	STDMETHOD(SortKeyRgch)(const OLECHAR * pch, int cchIn, LgCollatingOptions colopt,
		int cchMaxOut, OLECHAR * pchKey, int * pcchOut);
	STDMETHOD(Compare)(BSTR bstrValue1, BSTR bstrValue2, LgCollatingOptions colopt,
		int * pnVal);
	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** pwsf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);
	STDMETHOD(get_SortKeyVariant)(BSTR bstrValue, LgCollatingOptions colopt,
		VARIANT * psaKey);
	STDMETHOD(CompareVariant)(VARIANT saValue1, VARIANT saValue2, LgCollatingOptions colopt,
		int * pnVal);
	STDMETHOD(Open)(BSTR bstrLocale);
	STDMETHOD(Close)();
	// IIcuCleanupCallback
	void DoneCleanup();

	// Member variable access

	// Other public methods

protected:
	// Member variables
	long m_cref;
	ILgWritingSystemFactoryPtr m_qwsf;
	StrUni m_stuLocale;
	Collator * m_pCollator;
	CollatorIcuCleanupCallbackPtr m_qcicc;

	// Static methods

	// Constructors/destructors/etc.
	LgIcuCollator();
	virtual ~LgIcuCollator();

	// Other protected methods
	static const int keySize = 1024;
	byte * GetSortKey(BSTR bstrValue, byte * prgbKey, int32_t * pcbKey);
	byte * GetSortKey(BSTR bstrValue, byte * prgbKey, int32_t * pcbKey, Vector<byte> & vbKey);
	void EnsureCollator();
};

DEFINE_COM_PTR(LgIcuCollator);
#endif  //LGICUCOLLATOR_H
