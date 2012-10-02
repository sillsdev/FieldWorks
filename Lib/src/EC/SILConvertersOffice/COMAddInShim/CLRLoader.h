#pragma once

#include <memory>

class CCLRLoader
{
public:
	static CCLRLoader *TheInstance();
	HRESULT CreateInstance(LPCWSTR szAssemblyName, LPCWSTR szClassName, const IID &riid, void ** ppvObject);

protected:
	CCLRLoader(void);
	virtual ~CCLRLoader(void);

	HRESULT LoadCLR();
	HRESULT CreateLocalAppDomain();

private:
	friend std::auto_ptr<CCLRLoader>;
	static std::auto_ptr<CCLRLoader> m_pInstance;

	ICorRuntimeHost   *m_pHost;
	mscorlib::_AppDomain *m_pLocalDomain;
};
