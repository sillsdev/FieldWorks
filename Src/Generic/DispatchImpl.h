#if WIN32
// TODO-Linux: Implement later

class SilComTypeInfoHolder : public ModuleEntry
{
// Should be 'protected' but can cause compiler to generate fat code.
public:
	const GUID* m_pguid;
	const GUID* m_plibid;
	WORD m_wMajor;
	WORD m_wMinor;

	ITypeInfo* m_pInfo;
	long m_dwRef;
	struct stringdispid
	{
		SmartBstr bstr;
		int nLen;
		DISPID id;
	};
	stringdispid* m_pMap;
	int m_nCount;

public:
	SilComTypeInfoHolder(const IID * piid, const GUID * plibid, WORD wMajor, WORD wMinor)
	{
		m_pguid = piid;
		m_plibid = plibid;
		m_wMajor = wMajor;
		m_wMinor = wMinor;
		m_pInfo = NULL;
		m_dwRef = 0;
		m_pMap = NULL;
		m_nCount = 0;
	}
	HRESULT GetTI(LCID lcid, ITypeInfo** ppInfo)
	{
		HRESULT hr = S_OK;
		if (m_pInfo == NULL)
			hr = GetTI(lcid);
		*ppInfo = m_pInfo;
		if (m_pInfo != NULL)
		{
			m_pInfo->AddRef();
			hr = S_OK;
		}
		return hr;
	}
	HRESULT GetTI(LCID lcid);
	HRESULT EnsureTI(LCID lcid)
	{
		HRESULT hr = S_OK;
		if (m_pInfo == NULL)
			hr = GetTI(lcid);
		return hr;
	}

	// This function is called by the module on exit
	virtual void ProcessDetach(void)
	{
		SilComTypeInfoHolder* p = this;
		if (p->m_pInfo != NULL)
			p->m_pInfo->Release();
		p->m_pInfo = NULL;
		if (p->m_pMap)
			delete [] p->m_pMap;
		p->m_pMap = NULL;
	}

	HRESULT GetTypeInfo(UINT /* itinfo */, LCID lcid, ITypeInfo** pptinfo)
	{
		HRESULT hRes = E_POINTER;
		if (pptinfo != NULL)
			hRes = GetTI(lcid, pptinfo);
		return hRes;
	}
	HRESULT GetIDsOfNames(REFIID /* riid */, LPOLESTR* rgszNames, UINT cNames,
		LCID lcid, DISPID* rgdispid)
	{
		HRESULT hRes = EnsureTI(lcid);
		if (m_pInfo != NULL)
		{
			for (int i=0; i<(int)cNames; i++)
			{
				int n = wcslen(rgszNames[i]);
				int j;
				for (j = m_nCount-1; j >= 0; j--)
				{
					if ((n == m_pMap[j].nLen) &&
						(memcmp(m_pMap[j].bstr, rgszNames[i], m_pMap[j].nLen * sizeof(OLECHAR)) == 0))
					{
						rgdispid[i] = m_pMap[j].id;
						break;
					}
				}
				if (j < 0)
				{
					hRes = m_pInfo->GetIDsOfNames(rgszNames + i, 1, &rgdispid[i]);
					if (FAILED(hRes))
						break;
				}
			}
		}
		return hRes;
	}

	HRESULT Invoke(IDispatch* p, DISPID dispidMember, REFIID /* riid */,
		LCID lcid, WORD wFlags, DISPPARAMS* pdispparams, VARIANT* pvarResult,
		EXCEPINFO* pexcepinfo, UINT* puArgErr)
	{
		HRESULT hRes = EnsureTI(lcid);
		if (m_pInfo != NULL)
			hRes = m_pInfo->Invoke(p, dispidMember, wFlags, pdispparams, pvarResult, pexcepinfo, puArgErr);
		return hRes;
	}
	HRESULT LoadNameCache(ITypeInfo* pTypeInfo)
	{
		TYPEATTR* pta;
		HRESULT hr = pTypeInfo->GetTypeAttr(&pta);
		if (SUCCEEDED(hr))
		{
			m_nCount = pta->cFuncs;
			m_pMap = m_nCount == 0 ? 0 : new stringdispid[m_nCount];
			for (int i=0; i<m_nCount; i++)
			{
				FUNCDESC* pfd;
				if (SUCCEEDED(pTypeInfo->GetFuncDesc(i, &pfd)))
				{
					SmartBstr bstrName;
					if (SUCCEEDED(pTypeInfo->GetDocumentation(pfd->memid, &bstrName, NULL, NULL, NULL)))
					{
						m_pMap[i].bstr.Attach(bstrName.Detach());
						m_pMap[i].nLen = SysStringLen(m_pMap[i].bstr);
						m_pMap[i].id = pfd->memid;
					}
#ifdef DEBUG
					MEMBERID idCheck;
					OLECHAR * pchName = m_pMap[i].bstr; // don't use &bstr, nulls it!
					pTypeInfo->GetIDsOfNames(&pchName, 1, &idCheck);
					Assert(idCheck == pfd->memid);
#endif
					pTypeInfo->ReleaseFuncDesc(pfd);
				}
			}
			pTypeInfo->ReleaseTypeAttr(pta);
		}
		return S_OK;
	}
};


inline HRESULT SilComTypeInfoHolder::GetTI(LCID lcid)
{
	//If this assert occurs then most likely didn't initialize properly
	Assert(m_plibid != NULL && m_pguid != NULL);
	Assert(!InlineIsEqualGUID(*m_plibid, GUID_NULL) && "Did you forget to pass the LIBID to CComModule::Init?");

	if (m_pInfo != NULL)
		return S_OK;
	HRESULT hRes = E_FAIL;
	// Review 1438 (JohnT): if multi-threaded we may need this?
	//EnterCriticalSection(&_Module.m_csTypeInfoHolder);
	if (m_pInfo == NULL)
	{
		ITypeLib* pTypeLib;
		hRes = LoadRegTypeLib(*m_plibid, m_wMajor, m_wMinor, lcid, &pTypeLib);
		if (SUCCEEDED(hRes))
		{
			ComSmartPtr<ITypeInfo> spTypeInfo;
			hRes = pTypeLib->GetTypeInfoOfGuid(*m_pguid, &spTypeInfo);
			if (SUCCEEDED(hRes))
			{
				ComSmartPtr<ITypeInfo> spInfo(spTypeInfo);
				ComSmartPtr<ITypeInfo2> spTypeInfo2;
				if (SUCCEEDED(spTypeInfo->QueryInterface(&spTypeInfo2)))
					spInfo = spTypeInfo2;

				LoadNameCache(spInfo);
				m_pInfo = spInfo.Detach();
			}
			pTypeLib->Release();
		}
	}
	// Need if multi-threaded?
	//LeaveCriticalSection(&_Module.m_csTypeInfoHolder);
	// Replaced by subclassing ModuleEntry
	//_Module.AddTermFunc(Cleanup, (DWORD)this);
	return hRes;
}

/////////////////////////////////////////////////////////////////////////////
// IDispatchImpl

template <class T, const IID* piid, const GUID* plibid = &CComModule::m_libid, WORD wMajor = 2,
WORD wMinor = 0, class tihclass = SilComTypeInfoHolder>
class SilDispatchImpl : public T
{
public:
	typedef tihclass _tihclass;
// IDispatch
	STDMETHOD(GetTypeInfoCount)(UINT* pctinfo)
	{
		*pctinfo = 1;
		return S_OK;
	}
	STDMETHOD(GetTypeInfo)(UINT itinfo, LCID lcid, ITypeInfo** pptinfo)
	{
		return _tih.GetTypeInfo(itinfo, lcid, pptinfo);
	}
	STDMETHOD(GetIDsOfNames)(REFIID riid, LPOLESTR* rgszNames, UINT cNames,
		LCID lcid, DISPID* rgdispid)
	{
		return _tih.GetIDsOfNames(riid, rgszNames, cNames, lcid, rgdispid);
	}
	STDMETHOD(Invoke)(DISPID dispidMember, REFIID riid,
		LCID lcid, WORD wFlags, DISPPARAMS* pdispparams, VARIANT* pvarResult,
		EXCEPINFO* pexcepinfo, UINT* puArgErr)
	{
		return _tih.Invoke((IDispatch*)this, dispidMember, riid, lcid,
		wFlags, pdispparams, pvarResult, pexcepinfo, puArgErr);
	}
protected:
	static _tihclass _tih;
	static HRESULT GetTI(LCID lcid, ITypeInfo** ppInfo)
	{
		return _tih.GetTI(lcid, ppInfo);
	}
};
#endif //WIN32

#if 0 // can't get this to work
template <class T, const IID* piid, const GUID* plibid, WORD wMajor, WORD wMinor, class tihclass>
SilDispatchImpl<T, piid, plibid, wMajor, wMinor, tihclass>::_tihclass
SilDispatchImpl<T, piid, plibid, wMajor, wMinor, tihclass>::_tih(
piid, plibid, wMajor, wMinor);
#else
#if WIN32
// REVIEW  We used to have wMajor and wMinor for the last two parameters, but VS .NET 2003 grumbled
// that wMajor/wMinor were undeclared identifiers.
#define IMPLEMENT_SIL_DISPATCH(T, piid, plibid) \
	SilDispatchImpl<T, piid, plibid>::_tihclass \
	SilDispatchImpl<T, piid, plibid>::_tih(piid, plibid, 2, 0);
#else //!WIN32
#define IMPLEMENT_SIL_DISPATCH(T, piid, plibid)
#endif //!WIN32
#endif // !0
