#ifndef MOCKLGWRITINGSYSTEM_H_INCLUDED
#define MOCKLGWRITINGSYSTEM_H_INCLUDED

#pragma once

class MockLgWritingSystem : public ILgWritingSystem
{
public:
	MockLgWritingSystem(ILgWritingSystemFactory* pwsf, BSTR bstrId, int handle)
	{
		m_qwsf = pwsf;
		StrUni stu(bstrId, BstrLen(bstrId));
		m_id.Assign(stu);
		m_handle = handle;
		// COM object behavior
		m_cref = 1;
		ModuleEntry::ModuleAddRef();
	}

	virtual ~MockLgWritingSystem()
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
		else if (riid == IID_ILgWritingSystem)
			*ppv = static_cast<ILgWritingSystem *>(this);
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

	STDMETHOD(get_Id)(BSTR * pbstr)
	{
		m_id.GetBstr(pbstr);
		return S_OK;
	}

	STDMETHOD(get_Handle)(int * phandle)
	{
		*phandle = m_handle;
		return S_OK;
	}

	STDMETHOD(get_LanguageName)(BSTR * pbstr)
	{
		return E_NOTIMPL;
	}

	STDMETHOD(get_SpellCheckingId)(BSTR * pbstr)
	{
		if (m_stuSpellCheckDictionary.Length())
			m_stuSpellCheckDictionary.GetBstr(pbstr);
		else if (m_id.Length())
			m_id.GetBstr(pbstr);
		else
			*pbstr = NULL;
		return S_OK;
	}

	STDMETHOD(put_SpellCheckingId)(BSTR bstr)
	{
		StrUni stu(bstr, BstrLen(bstr));
		if (m_stuSpellCheckDictionary != stu)
			m_stuSpellCheckDictionary.Assign(stu);
		return S_OK;
	}

	STDMETHOD(get_RightToLeftScript)(ComBool * pfRightToLeftScript)
	{
		*pfRightToLeftScript = m_fRightToLeft;
		return S_OK;
	}

	STDMETHOD(put_RightToLeftScript)(ComBool fRightToLeftScript)
	{
		bool fRTL = (bool)fRightToLeftScript;
		if (m_fRightToLeft != fRTL)
			m_fRightToLeft = fRTL;
		return S_OK;
	}

	STDMETHOD(get_Renderer)(IVwGraphics * pvg, IRenderEngine ** ppreneng)
	{
		if (!m_qrenengUni)
		{
#if WIN32
			m_qrenengUni.CreateInstance(CLSID_UniscribeEngine);
#else
			m_qrenengUni.CreateInstance(CLSID_RomRenderEngine);
#endif //WIN32
			if (!m_qrenengUni)
				return E_UNEXPECTED;
			m_qrenengUni->putref_WritingSystemFactory(m_qwsf);
		}
		*ppreneng = m_qrenengUni.Ptr();
		if (*ppreneng)
			(*ppreneng)->AddRef();
		return S_OK;
	}

	STDMETHOD(get_DefaultFontFeatures)(BSTR * pbstr)
	{
		if (m_stuDefFontFeats.Length())
			m_stuDefFontFeats.GetBstr(pbstr);
		else
			*pbstr = NULL;
		return S_OK;
	}

	STDMETHOD(put_DefaultFontFeatures)(BSTR bstr)
	{
		StrUni stu(bstr, BstrLen(bstr));
		if (m_stuDefFontFeats != stu)
			m_stuDefFontFeats.Assign(stu);
		return S_OK;
	}

	STDMETHOD(get_DefaultFontName)(BSTR * pbstr)
	{
		if (m_stuDefFont.Length())
			m_stuDefFont.GetBstr(pbstr);
		else
			*pbstr = NULL;
		return S_OK;
	}

	STDMETHOD(put_DefaultFontName)(BSTR bstr)
	{
		StrUni stu(bstr, BstrLen(bstr));
		if (m_stuDefFont != stu)
			m_stuDefFont.Assign(stu);
		return S_OK;
	}

	STDMETHOD(get_CharPropEngine)(ILgCharacterPropertyEngine ** pppropeng)
	{
		if (!m_qcpe)
		{
			SmartBstr sbstrLanguage;
			SmartBstr sbstrScript;
			SmartBstr sbstrCountry;
			SmartBstr sbstrVariant;
			m_id.GetBstr(&sbstrLanguage);
			ILgIcuCharPropEnginePtr qzcpe;
			qzcpe.CreateInstance(CLSID_LgIcuCharPropEngine);
			qzcpe->Initialize(sbstrLanguage, sbstrScript, sbstrCountry, sbstrVariant);
			qzcpe->QueryInterface(IID_ILgCharacterPropertyEngine, (void **)&m_qcpe);
		}
		*pppropeng = m_qcpe.Ptr();
		if (*pppropeng)
			(*pppropeng)->AddRef();

		return S_OK;
	}

	STDMETHOD(InterpretChrp)(LgCharRenderProps * pchrp)
	{
		ReplaceChrpFontName(pchrp);
		return S_OK;
	}

private:
	bool ReplaceChrpFontName(LgCharRenderProps * pchrp)
	{
		pchrp->szFaceName[31] = 0;	// ensure NUL termination.
		OleStringLiteral defaultFontText = L"<default font>";
		if (wcscmp(pchrp->szFaceName, defaultFontText) == 0)
		{
			wcsncpy_s(pchrp->szFaceName, 32, m_stuDefFont, _TRUNCATE);
			return true;
		}
		return false;
	}

	long m_cref;

	ILgWritingSystemFactoryPtr m_qwsf;
	int m_handle;
	StrUni m_id;
	IRenderEnginePtr m_qrenengUni;
	ILgCharacterPropertyEnginePtr m_qcpe;
	StrUni m_stuDefFont;
	StrUni m_stuDefHeadFont;
	StrUni m_stuDefPubFont;
	StrUni m_stuDefFontFeats;
	StrUni m_stuDefHeadFontFeats;
	StrUni m_stuDefPubFontFeats;
	bool m_fRightToLeft;
	StrUni m_stuSpellCheckDictionary;
};

DEFINE_COM_PTR(MockLgWritingSystem);

#endif
