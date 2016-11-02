#ifndef MOCKRENDERENGINEFACTOR_H_INCLUDED
#define MOCKRENDERENGINEFACTOR_H_INCLUDED

#pragma once

class MockRenderEngineFactory : public IRenderEngineFactory
{
public:
	MockRenderEngineFactory()
	{
		// COM object behavior
		m_cref = 1;
		ModuleEntry::ModuleAddRef();
	}

	virtual ~MockRenderEngineFactory()
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
		else if (riid == IID_IRenderEngineFactory)
			*ppv = static_cast<IRenderEngineFactory *>(this);
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

	STDMETHOD(get_Renderer)(ILgWritingSystem * ws, IVwGraphics * pvg, IRenderEngine ** ppreneng)
	{
		if (!m_qrenengUni)
		{
#ifdef WIN32
			m_qrenengUni.CreateInstance(CLSID_UniscribeEngine);
#else
			m_qrenengUni.CreateInstance(CLSID_RomRenderEngine);
#endif //WIN32
			if (!m_qrenengUni)
				return E_UNEXPECTED;
			ILgWritingSystemFactoryPtr qwsf;
			ws->get_WritingSystemFactory(&qwsf);
			m_qrenengUni->putref_WritingSystemFactory(qwsf);
			m_qrenengUni->putref_RenderEngineFactory(this);
		}
		*ppreneng = m_qrenengUni.Ptr();
		if (*ppreneng)
			(*ppreneng)->AddRef();
		return S_OK;
	}

private:
	long m_cref;
	IRenderEnginePtr m_qrenengUni;
};

DEFINE_COM_PTR(MockRenderEngineFactory);

#endif