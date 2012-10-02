/*
 *	Filename
 *
 *	Description
 *
 *	Neil Mayhew - 06 Sep 2006
 *
 */

#include "common.h"

class TestGenericFactory : public IUnknown
{
public:
	virtual UCOMINT32 AddRef();
	virtual UCOMINT32 Release();
	virtual HRESULT QueryInterface(REFIID riid, void** ppv);
protected:
	TestGenericFactory();
	virtual ~TestGenericFactory();
private:
	ULONG m_cref;
public:
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
};

#define CLSID_TestGenericFactory __uuidof(TestGenericFactory)

template<> const GUID CLSID_TestGenericFactory("b670f879-f7c3-44c4-94aa-572c955a5320");

static GenericFactory g_fact(
	_T("SIL.Generic.TestGenericFactory"),
	&CLSID_TestGenericFactory,
	_T("Test Generic Factory"),
	_T("Apartment"),
	&TestGenericFactory::CreateCom);

void TestGenericFactory::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	TestGenericFactory* pTest = new TestGenericFactory(); // ref count initialy 1

	if (pTest == NULL)
		ThrowHr(WarnHr(E_OUTOFMEMORY));

	// Get the requested interface.
	HRESULT hr = pTest->QueryInterface(riid, ppv);

	// Release the IUnknown pointer.
	// (If QueryInterface failed, component will delete itself.)
	pTest->Release();

	CheckHr(WarnHr(hr));
}

TestGenericFactory::TestGenericFactory()
	: m_cref(1)
{
}

TestGenericFactory::~TestGenericFactory()
{
}

UCOMINT32 TestGenericFactory::AddRef()
{
	//Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}

UCOMINT32 TestGenericFactory::Release()
{
	//Assert(m_cref > 0);
	UCOMINT32 cref = ::InterlockedDecrement(&m_cref);
	if (!cref)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}

HRESULT TestGenericFactory::QueryInterface(REFIID iid, void** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == __uuidof(IUnknown))
	{
		*ppv = static_cast<IUnknown*>(this);
	}
	else
	{
		return E_NOINTERFACE;
	}
	reinterpret_cast<IUnknown*>(*ppv)->AddRef();
	return S_OK;
}

extern "C" BOOL WINAPI DllMain(HMODULE hmod, DWORD dwReason, PVOID pvReserved);
STDAPI DllGetClassObject(REFCLSID clsid, REFIID iid, VOID ** ppv);

#include <iostream>

int main(int argc, char** argv)
{
	try
	{
		DllMain(0, DLL_PROCESS_ATTACH, 0);

		IClassFactory* pFact = 0;
		CheckHr(DllGetClassObject(CLSID_TestGenericFactory, IID_IClassFactory, (void**)&pFact));

		IUnknown* pTest = 0;
		CheckHr(pFact->CreateInstance(0, IID_IUnknown, (void**)&pTest));

		pFact->Release();
		pTest->Release();

		return DllMain(0, DLL_PROCESS_DETACH, 0) ? 0 : 1;
	}
	catch (Throwable& thr)
	{
		std::cerr << "Failed HRESULT: " << thr.Error() << "\n";
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception: " << e.what() << "\n";
	}
	catch (...)
	{
		std::cerr << "Unknown Exception:\n";
	}

	return 2;
}
