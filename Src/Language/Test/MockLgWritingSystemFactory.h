#ifndef MOCKLGWRITINGSYSTEMFACTORY_H_INCLUDED
#define MOCKLGWRITINGSYSTEMFACTORY_H_INCLUDED

#pragma once

#include "MockLgWritingSystem.h"

class MockLgWritingSystemFactory : public ILgWritingSystemFactory
{
public:
	MockLgWritingSystemFactory()
	{
		m_nextHandle = 999000001;

		// COM object behavior
		m_cref = 1;
		ModuleEntry::ModuleAddRef();
	}

	virtual ~MockLgWritingSystemFactory()
	{
		ModuleEntry::ModuleRelease();
	}

	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv)
	{
		AssertPtr(ppv);
		if (!ppv)
			return WarnHr(E_POINTER);
		*ppv = NULL;

		if (riid == IID_IUnknown)
			*ppv = static_cast<IUnknown *>(this);
		else if (riid == IID_ILgWritingSystemFactory)
			*ppv = static_cast<ILgWritingSystemFactory *>(this);
		else
			return E_NOINTERFACE;

		AddRef();
		return NOERROR;
	}

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

	STDMETHOD(get_Engine)(BSTR bstrId, ILgWritingSystem ** ppwseng)
	{
		StrUni stu(bstrId);
		int ws;
		if (m_hmwsId.Retrieve(stu, &ws))
			return get_EngineOrNull(ws, ppwseng);

		ws = m_nextHandle++;
		ILgWritingSystemPtr qzws;
		qzws.Attach(NewObj MockLgWritingSystem(this, bstrId, ws));
		m_hmnwsobj.Insert(ws, qzws, true);
		m_hmwsLocale.Insert(ws, stu, true);
		m_hmwsId.Insert(stu, ws, true);
		m_setws.Insert(ws);
		*ppwseng = qzws.Detach();
		return S_OK;
	}

	STDMETHOD(get_EngineOrNull)(int ws, ILgWritingSystem ** ppwseng)
	{
		*ppwseng = NULL;

		ILgWritingSystemPtr qwsobj;
		if (m_hmnwsobj.Retrieve(ws, qwsobj))
			*ppwseng = qwsobj.Detach();
		return S_OK;
	}

	STDMETHOD(GetWsFromStr)(BSTR bstr, int * pws)
	{
		StrUni stu(bstr);
		if (m_hmwsId.Retrieve(stu, pws))
			return S_OK;

		return S_FALSE;
	}

	STDMETHOD(GetStrFromWs)(int ws, BSTR * pbstr)
	{
		StrUni stu;
		if (m_hmwsLocale.Retrieve(ws, &stu))
		{
			stu.GetBstr(pbstr);
			return S_OK;
		}

		return S_FALSE;
	}

	STDMETHOD(get_NumberOfWs)(int * pcws)
	{
		*pcws = m_setws.Size();
		return S_OK;
	}

	STDMETHOD(GetWritingSystems)(int * rgws, int cws)
	{
		int iws = 0;
		int * pws = rgws;
		for (Set<int>::iterator it = m_setws.Begin(); it != m_setws.End(); ++it)
		{
			if (iws < cws)
			{
				*pws++ = it.GetValue();
			}
			++iws;
		}
		for ( ; iws < cws; ++iws)
			rgws[iws] = 0;
		return S_OK;
	}

	STDMETHOD(get_CharPropEngine)(int ws, ILgCharacterPropertyEngine ** pplcpe)
	{
		ILgWritingSystemPtr qws;
		CheckHr(get_EngineOrNull(ws, &qws));
		if (qws)
			return qws->get_CharPropEngine(pplcpe);

		return E_INVALIDARG;
	}

	STDMETHOD(get_Renderer)(int ws, IVwGraphics * pvg, IRenderEngine ** ppre)
	{
		ILgWritingSystemPtr qws;
		get_EngineOrNull(ws, &qws);
		if (qws)
			return qws->get_Renderer(pvg, ppre);

		return E_INVALIDARG;
	}

	STDMETHOD(get_RendererFromChrp)(IVwGraphics * pvg, LgCharRenderProps * pchrp, IRenderEngine ** ppre)
	{
		HRESULT hr = S_OK;
#if WIN32
		// Unfortunately we need a DC before we can call SetupGraphics.
		HDC hdc = ::CreateDC(TEXT("DISPLAY"),NULL,NULL,NULL);
#else
		HDC hdc = NULL; // Linux implementation of VwGraphics can handle this.
#endif //WIN32

		IVwGraphicsWin32Ptr qvgw;
		qvgw.CreateInstance(CLSID_VwGraphicsWin32);
		qvgw->Initialize(hdc);
		qvgw->SetupGraphics(pchrp);
		hr = get_Renderer(pchrp->ws, qvgw, ppre);
		qvgw.Clear();

#if WIN32
		::DeleteDC(hdc);
#endif //WIN32

		return hr;
	}

	STDMETHOD(get_UserWs)(int * pws)
	{
		*pws = m_wsUser;
		return S_OK;
	}

	STDMETHOD(put_UserWs)(int ws)
	{
		m_wsUser = ws;
		return S_OK;
	}

private:
	long m_cref;

	int m_nextHandle;
	HashMap<int, StrUni> m_hmwsLocale;
	HashMapStrUni<int> m_hmwsId;
	ComHashMap<int, ILgWritingSystem> m_hmnwsobj;
	Set<int> m_setws;
	int m_wsUser;
};

DEFINE_COM_PTR(MockLgWritingSystemFactory);

// Allow template method instantiation.
#include "HashMap_i.cpp"
#include "ComHashMap_i.cpp"
#include "Set_i.cpp"

#endif