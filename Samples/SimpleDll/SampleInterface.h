/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: SampleInterface.h
Responsibility: John Thomson
Last reviewed: never

Description:
	Contains the implementation of ISampleInterface
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef SampleInterface_INCLUDED
#define SampleInterface_INCLUDED

/*----------------------------------------------------------------------------------------------
This class implements a trivial interface as a demonstration
@H3{Hungarian: xsi}
----------------------------------------------------------------------------------------------*/
class SampleInterface : public ISampleInterface
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	SampleInterface();
	virtual ~SampleInterface();

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

	// ISampleInterface methods
	STDMETHOD(get_HelloWorldString)(BSTR * pbstr);

protected:
	// Member variables
	long m_cref;
};
DEFINE_COM_PTR(SampleInterface);

#endif  // SampleInterface_INCLUDED
