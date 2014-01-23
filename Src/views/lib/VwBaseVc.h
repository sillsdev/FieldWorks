/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwBaseVc.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This provides a base class for implementing IVwViewConstructor.
	It implements IUnknown, and default (return E_NOTIMPL) implementations of all the methods.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWBASEVC_INCLUDED
#define VWBASEVC_INCLUDED 1

class VwBaseVc : public IVwViewConstructor
{
public:
	VwBaseVc();
	virtual ~VwBaseVc();
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
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
	STDMETHOD(Display)(IVwEnv* pvwenv, HVO vwobj, int frag);
	STDMETHOD(DisplayVec)(IVwEnv * pvwenv, HVO hvo, int tag, int frag);
	STDMETHOD(DisplayPicture)(IVwEnv * pvwenv,  int hvo, int tag, int val, int frag,
		IPicture ** ppPict);
	STDMETHOD(DisplayVariant)(IVwEnv * pvwenv, int tag, int frag, ITsString ** pptss);
	STDMETHOD(UpdateProp)(IVwSelection * pvwsel, HVO vwobj, int tag, int frag, ITsString * ptssVal,
		ITsString ** pptssRepVal);
	STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight);
	STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
		int tag, int frag, int ihvoMin);
	STDMETHOD(GetStrForGuid)(BSTR bstrGuid, ITsString ** pptss);
	STDMETHOD(DoHotLinkAction)(BSTR bstrData, HVO hvoOwner, PropTag tag, ITsString * ptss,
		int ichObj);
	STDMETHOD(GetIdFromGuid)(ISilDataAccess * psda, GUID * puid, HVO * phvo);
	STDMETHOD(DisplayEmbeddedObject)(IVwEnv * pvwenv, HVO hvo);
	STDMETHOD(UpdateRootBoxTextProps)(ITsTextProps * pttp, ITsTextProps ** ppttp);

protected:
	long m_cref;
};

#endif //VWBASEVC_INCLUDED
