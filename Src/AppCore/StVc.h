/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: StVc.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a view constructor for a standard view of a structured text
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RN_ST_VC_INCLUDED
#define RN_ST_VC_INCLUDED 1

// Enumeration to provide fragment identifiers
enum
{
	kfrText, // The whole text
	kfrPara, // one paragraph
	kftBody, // The string that is the body of a paragraph
};

/*----------------------------------------------------------------------------------------------
	The main customizeable document ("structured text") view constructor class.
	Hungarian: stvc.
----------------------------------------------------------------------------------------------*/
class StVc : public VwBaseVc
{
public:
	typedef VwBaseVc SuperClass;

	StVc(int wsDefault);
	StVc(LPCOLESTR pszSty, int wsDefault, COLORREF clrBkg = -1);
	StVc(LPCOLESTR pszSty, int wsDefault, COLORREF clrBkg, ILgWritingSystemFactory * pwsf);
	~StVc();
	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight);
	void SetStyle(LPCOLESTR pszSty)
	{
		AssertPszN(pszSty);
		m_stuSty = pszSty;
	}
	void SetClrBkg(COLORREF clr)
	{
		m_clrBkg = clr;
	}
	void SetLabel(ITsString * ptssLabel)
	{
		m_qtssLabel = ptssLabel;
	}
	void SetRightToLeft(bool f)
	{
		m_fRtl = f;
	}
	void BeLazy(bool fLazy = true) {m_fLazy = fLazy;}
	static HVO MakeEmptyStText(ISilDataAccess * psda, HVO hvoOwner, PropTag tag, int ws);

protected:
	// A TsTextProps that invokes the named style "Normal." Created when first needed.
	ITsTextPropsPtr m_qttpNormal;
	ITsTextProps * NormalStyle();
	StrUni m_stuSty; // Text properties.
	COLORREF m_clrBkg; // Color for paragraph background.
	ITsStringPtr m_qtssLabel; // Label to be stuck at start of first paragraph.
	bool m_fRtl;	// overall document direction
	bool m_fLazy;  // paragraphs displayed lazily
	int m_wsDefault;	// Default writing system for empty fields.
};

#endif // RN_ST_VC_INCLUDED
