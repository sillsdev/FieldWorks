/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: BasicVc.h
Responsibility:
Last reviewed:

	A class like VwBaseVc, but even simpler and with no dependencies on AfApp.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef BasicVc_H_INCLUDED
#define BasicVc_H_INCLUDED

#pragma once

class BasicVc : public IVwViewConstructor
{
public:
	BasicVc()
	{
		// COM object behavior
		m_cref = 1;
		ModuleEntry::ModuleAddRef();
	}

	virtual ~BasicVc()
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
		else if (riid == IID_IVwViewConstructor)
			*ppv = static_cast<IVwViewConstructor *>(this);
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
	STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
	{
		Assert(false);
		return E_NOTIMPL;
	}
	STDMETHOD(DisplayVec)(IVwEnv * pvwenv, HVO hvo, int tag, int frag)
	{
		Assert(false);
		return E_NOTIMPL;
	}
	STDMETHOD(DisplayVariant)(IVwEnv * pvwenv, int tag, int frag, ITsString ** pptss)
	{
		Assert(false);
		return E_NOTIMPL;
	}
	STDMETHOD(DisplayPicture)(IVwEnv * pvwenv,  int hvo, int tag, int val, int frag,
		IPicture ** ppPict)
	{
		*ppPict = NULL;
		Assert(false);
		return E_NOTIMPL;
	}

	STDMETHOD(UpdateProp)(IVwSelection * pvwsel, HVO vwobj, int tag, int frag, ITsString * ptssVal,
		ITsString ** pptssRepVal)
	{
		Assert(false);
		return E_NOTIMPL;
	}
	STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
	{
		*pdyHeight = 15 + hvo * 2; // just give any arbitrary number
		return S_OK;
	}
	STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
		int tag, int frag, int ihvoMin)
	{
		return S_OK;
	}
	STDMETHOD(GetStrForGuid)(BSTR bstrGuid, ITsString ** pptss)
	{
		Assert(false);
		return E_NOTIMPL;
	}
	STDMETHOD(DoHotLinkAction)(BSTR bstrData, ISilDataAccess * psda)
	{
		Assert(false);
		return E_NOTIMPL;
	}
	STDMETHOD(GetIdFromGuid)(ISilDataAccess * psda, GUID * pguid, HVO * phvo)
	{
		Assert(false);
		return E_NOTIMPL;
	}
	STDMETHOD(DisplayEmbeddedObject)(IVwEnv * pvwenv, HVO hvo)
	{
		Assert(false);
		return E_NOTIMPL;
	}
	STDMETHOD(UpdateRootBoxTextProps)(ITsTextProps * pttp, ITsTextProps ** ppttp)
	{
		*ppttp = NULL;
		return S_OK;
	}

protected:
	long m_cref;
};

#endif /*BasicVc_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
