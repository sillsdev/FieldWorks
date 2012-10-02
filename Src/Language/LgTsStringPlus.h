/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgTsStringPlus.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef __LgTsStringPlus_H_
#define __LgTsStringPlus_H_

/*----------------------------------------------------------------------------------------------
	Hungarian: ztssencs
----------------------------------------------------------------------------------------------*/
class LgTsStringPlusWss : public ILgTsStringPlusWss
{
public:
	// Static Methods
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// IUnknown Methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHODIMP_(ULONG) AddRef(void);
	STDMETHODIMP_(ULONG) Release(void);

	// ITsStringPlus Methods.
	STDMETHOD(get_String)(ILgWritingSystemFactory * pwsf, ITsString ** pptss);
	STDMETHOD(get_StringUsingWs)(int newWs, ITsString ** pptss);
	STDMETHOD(putref_String)(ILgWritingSystemFactory * pwsf, ITsString * ptss);
	STDMETHOD(get_Text)(BSTR * pbstr);
	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** ppwsf);
	STDMETHOD(Serialize)(IStorage * pstg);
	STDMETHOD(Deserialize)(IStorage * pstg);

protected:
	// Member variables
	long m_cref;

	ITsStringPtr m_qtss;
	ComVector<IWritingSystem> m_vqws;
	ILgWritingSystemFactoryPtr m_qwsfSrc;

	bool m_fNeedToMap;
	HashMap<int, int> m_hmwsOldwsNew;

	void AddStringWritingSystems(ITsString * ptss, Set<int> & setws,
		ILgWritingSystemFactory * pwsf);
	void AddWritingSystemIfMissing(int ws, Set<int> & setws, ILgWritingSystemFactory * pwsf);
	void MapInternalWss(ITsString * ptss, ITsString ** pptssNew);

	LgTsStringPlusWss();
	~LgTsStringPlusWss();
};
DEFINE_COM_PTR(LgTsStringPlusWss);

// Local Variables:
// mode:C++
// End: (These 3 lines are useful to Steve McConnel.)

#endif	// __TsStringPlus_H_
