/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: CSupportErrorInfo.h
Responsibility: John Thomson
Last reviewed:

Description:
	Implement the ISupportErrorInfo interface as a tear-off interface for COM classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef CSupportErrorInfo_H
#define CSupportErrorInfo_H

/*----------------------------------------------------------------------------------------------
	This class implements the ISupportErrorInfo interface for classes that implement only one
	COM interface in addition to IUnknown.  It provides a "tear-off" interface.

	Hungarian: sei
----------------------------------------------------------------------------------------------*/
class CSupportErrorInfo : public ISupportErrorInfo
{
public:
	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv)
	{
		return m_punkObject->QueryInterface(riid, ppv);
	}

	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		m_cref++;
		return m_punkObject->AddRef();
	}

	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = m_punkObject->Release();
		m_cref--;
		if (m_cref == 0)
			delete this; // done with the tear-off, even if not with the object.
		return cref;
	}

	//:> ISupportErrorInfo methods
	STDMETHOD(InterfaceSupportsErrorInfo)(REFIID riid)
	{
		return (riid == m_iid) ? NOERROR : S_FALSE;
	}

	CSupportErrorInfo(IUnknown * punkObject, REFIID riid)
	{
		m_punkObject = punkObject;
		AddRefObj(punkObject); // give it one extra, to match the one we have.
		m_iid = riid;
		m_cref = 1;
	}

private:
	IUnknown * m_punkObject;	// IUnknown of Object that implements this interface
	GUID m_iid;
	long m_cref;
};

/*----------------------------------------------------------------------------------------------
	This class implements the ISupportErrorInfo interface for classes that implement two
	COM interfaces in addition to IUnknown.  It provides a "tear-off" interface.

	Hungarian: sei2
----------------------------------------------------------------------------------------------*/
class CSupportErrorInfo2 : public ISupportErrorInfo
{
public:
	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv)
	{
		return m_punkObject->QueryInterface(riid, ppv);
	}

	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		m_cref++;
		return m_punkObject->AddRef();
	}

	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = m_punkObject->Release();
		m_cref--;
		if (m_cref == 0)
			delete this; // done with the tear-off, even if not with the object.
		return cref;
	}

	//:> ISupportErrorInfo methods
	STDMETHOD(InterfaceSupportsErrorInfo)(REFIID riid)
	{
		return (riid == m_iid1 || riid == m_iid2) ? NOERROR : S_FALSE;
	}

	CSupportErrorInfo2(IUnknown * punkObject, REFIID riid1, REFIID riid2)
	{
		m_punkObject = punkObject;
		AddRefObj(punkObject);
		m_iid1 = riid1;
		m_iid2 = riid2;
		m_cref = 1;
	}

private:
	IUnknown * m_punkObject;	// IUnknown of Object that implements this interface
	GUID m_iid1;
	GUID m_iid2;
	long m_cref;
};

/*----------------------------------------------------------------------------------------------
	This class implements the ISupportErrorInfo interface for classes that implement three
	COM interfaces in addition to IUnknown.  It provides a "tear-off" interface.

	Hungarian: sei3
----------------------------------------------------------------------------------------------*/
class CSupportErrorInfo3 : public ISupportErrorInfo
{
public:
	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv)
	{
		return m_punkObject->QueryInterface(riid, ppv);
	}

	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		m_cref++;
		return m_punkObject->AddRef();
	}

	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = m_punkObject->Release();
		m_cref--;
		if (m_cref == 0)
			delete this; // done with the tear-off, even if not with the object.
		return cref;
	}

	//:> ISupportErrorInfo methods
	STDMETHOD(InterfaceSupportsErrorInfo)(REFIID riid)
	{
		return (riid == m_iid1 || riid == m_iid2|| riid == m_iid3) ? NOERROR : S_FALSE;
	}

	CSupportErrorInfo3(IUnknown * punkObject, REFIID riid1, REFIID riid2, REFIID riid3)
	{
		m_punkObject = punkObject;
		AddRefObj(punkObject);
		m_iid1 = riid1;
		m_iid2 = riid2;
		m_iid3 = riid3;
		m_cref = 1;
	}

private:
	IUnknown * m_punkObject;	// IUnknown of Object that implements this interface
	GUID m_iid1;
	GUID m_iid2;
	GUID m_iid3;
	long m_cref;
};

/*----------------------------------------------------------------------------------------------
	This class implements the ISupportErrorInfo interface for classes that implement more than
	two COM interfaces in addition to IUnknown.  It provides a "tear-off" interface.

	Note: I haven't tried this one yet, I'm not sure exactly how to set up the array of
	constant IIDs that it needs. Consider this a first draft.

	Hungarian: sein
----------------------------------------------------------------------------------------------*/
class CSupportErrorInfoN : public ISupportErrorInfo
{
public:
	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv)
	{
		return m_punkObject->QueryInterface(riid, ppv);
	}

	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		m_cref++;
		return m_punkObject->AddRef();
	}

	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = m_punkObject->Release();
		m_cref--;
		if (m_cref == 0)
			delete this; // done with the tear-off, even if not with the object.
		return cref;
	}

	//:> ISupportErrorInfo methods
	STDMETHOD(InterfaceSupportsErrorInfo)(REFIID riid)
	{
		for (IID * priid = m_prgriid; priid < m_prgriid + m_ciid; priid++)
			if (riid == *priid)
				return NOERROR;
		return S_FALSE;
	}

	CSupportErrorInfoN(IUnknown * punkObject, IID * prgriid, int ciid)
	{
		m_punkObject = punkObject;
		AddRefObj(punkObject);
		m_prgriid = prgriid;
		m_ciid = ciid;
		m_cref = 1;
	}

private:
	IUnknown * m_punkObject;	// IUnknown of Object that implements this interface
	IID * m_prgriid;
	int m_ciid;
	long m_cref;
};


#endif /*CSupportErrorInfo_H*/
