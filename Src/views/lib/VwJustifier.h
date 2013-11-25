/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwJustifier.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWJUSTIFIER_INCLUDED
#define VWJUSTIFIER_INCLUDED

// Trick GUID for getting the actual implementation of the VwJustifier object.
// SC: Do we need this?
//class __declspec(uuid("06FB56D4-D126-4c81-A475-3998D09763B3")) VwJustifier;
//#define CLID_VWJUSTIFIER_IMPL __uuidof(VwJustifier)


/*----------------------------------------------------------------------------------------------
Class: VwJustifier
Description:
----------------------------------------------------------------------------------------------*/
class VwJustifier : public IVwJustifier, public GrJustifier
{
	friend class FwGrJustifier;

public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	VwJustifier();
	virtual ~VwJustifier();

	// Member variable access

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// The actual IVwJustifier methods.
	STDMETHOD(AdjustGlyphWidths)(IJustifyingRenderer * pjrend, int iGlyphMin, int iGlyphLim,
		float dxCurrWidth, float dxDesiredWidth);
	//STDMETHOD(SuggestShrinkAndBreak)(IJustifyingRenderer * pjrend,
	//	int iGlyphMin, int iGlyphLim, float dxsWidth, LgLineBreak lbPref, LgLineBreak lbMax,
	//	float * pdxShrink, LgLineBreak * plbToTry);

	// IGrJustifier methods - inherited
	//GrResult adjustGlyphWidths(void * pvGrEng, int iGlyphMin, iGlyphLim,
	//	float dxCurrWidth, float dxDesiredWidth);
	// etc.
};

DEFINE_COM_PTR(VwJustifier);

#endif  // VWJUSTIFIER_INCLUDED
