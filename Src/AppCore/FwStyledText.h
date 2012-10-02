/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwStyledText.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:

----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef UTILWSSTYLE_H
#define UTILWSSTYLE_H 1

// Externally defined (effectively forward declaration).
interface ITsTextProps;
class ChrpInheritance;
class WsStyleInfo;
/*:End Ignore*/


enum
{
	kxConflicting = -1,
	kxInherited = 0,
	kxExplicit = 1,

	// These names make more sense for the hard-formatting dialog:
	kxSoft = kxInherited,
	kxHard = kxExplicit,
};

/*----------------------------------------------------------------------------------------------
	This namespace provides a place for these utility functions to live without having to be
	global functions.
----------------------------------------------------------------------------------------------*/
namespace FwStyledText
{
	enum
	{
		knUnspecified = knNinch,
		knConflicting = knNinch + 1,
	};

	void ComputeInheritance(ITsTextProps * pttpBase, ITsTextProps * pttpOverride,
		ITsTextProps ** ppttpEffect);
	void ComputeWsStyleInheritance(BSTR bstrBase, BSTR bstrOver, SmartBstr & sbstrComp);
	int WsStylesPropList(const int ** pprgtpt);
	void MergeIntProp(int tpt, int nVarBase, int nValBase, int &nVar, int &nVal);
	void ZapWsStyle(StrUni & stuWsStyle, int tpt, int nVar, int nVal);

	void DecodeFontPropsString(BSTR bstr, bool fExplicit, Vector<WsStyleInfo> & vesiI,
		Vector<WsStyleInfo> & vesiE, Vector<ChrpInheritance> & vchrpi, Vector<int> & vwsSoFar,
		Vector<int> & vwsExtra);
	void DecodeFontPropsString(BSTR bstr, Vector<WsStyleInfo> & vesi,
		Vector<int> & vwsSoFar);
	int FindOrAddWsInfo(int ws, Vector<WsStyleInfo> & vesiI, Vector<WsStyleInfo> & vesiE,
		Vector<ChrpInheritance> & vchrpi, Vector<int> & vwsSoFar);
	StrUni EncodeFontPropsString(Vector<WsStyleInfo> & vesi, bool fForPara);
	void ConvertDefaultFontInput(StrUni & stuFont);
// currently not used anywhere; REQUIRES Render.idh
//	void DecodeFontPropsForEnc(int wsToFind, SmartBstr sbstr, LgCharRenderProps & chrp,
//		StrUni & stuFF, StrUni & stuFontVar, ChrpInheritance & chrpi);

	StrUni FontStringMarkupToUi(bool f1DefaultFont, StrUni stuMarkup);
	StrUni FontStringUiToMarkup(StrUni stuUi);
	void FontUiStrings(bool f1DefaultFont, Vector<StrUni> & vstu);
	StrUni FontDefaultMarkup();
	StrUni FontDefaultUi(bool f1DefaultFont);
	bool MatchesDefaultSerifMarkup(StrUni str);
	bool MatchesDefaultSansMarkup(StrUni str);
	bool MatchesDefaultBodyFontMarkup(StrUni str);
	bool MatchesDefaultMonoMarkup(StrUni str);
	StrUni FontMarkupToFontName(StrUni str);
	StrUni RemoveSpuriousOverrides(StrUni stuWsStyle, ITsPropsBldr * ptpb);
};

/*----------------------------------------------------------------------------------------------
	This class stores the information about whether style values are explicit (true)
	or inherited (false).
	Hungarian: chrpi
----------------------------------------------------------------------------------------------*/
class ChrpInheritance
{
public:
	// Are the properties explicit, inherited, or some combination (conflicting)?
	int xFont;
	int xSize;
	int xItalic;
	int xBold;
	int xSs;
	int xOffset;
	int xFore;
	int xBack;
	int xUnder;		// underline color
	int xUnderT;	// underline type
	int xFontVar;

	// not included: height, wsRtl

	void Init()
	{
		xFont = kxConflicting;
		xSize = kxConflicting;
		xItalic = kxConflicting;
		xBold = kxConflicting;
		xSs = kxConflicting;
		xOffset = kxConflicting;
		xFore = kxConflicting;
		xBack = kxConflicting;
		xUnder = kxConflicting;
		xUnderT = kxConflicting;
		xFontVar = kxConflicting;
	}

	void InitToInherited()
	{
		xFont = kxInherited;
		xSize = kxInherited;
		xItalic = kxInherited;
		xBold = kxInherited;
		xSs = kxInherited;
		xOffset = kxInherited;
		xFore = kxInherited;
		xBack = kxInherited;
		xUnder = kxInherited;
		xUnderT = kxInherited;
		xFontVar = kxInherited;
	}
	void InitToSoft()
	{
		xFont = kxSoft;
		xSize = kxSoft;
		xItalic = kxSoft;
		xBold = kxSoft;
		xSs = kxSoft;
		xOffset = kxSoft;
		xFore = kxSoft;
		xBack = kxSoft;
		xUnder = kxSoft;
		xUnderT = kxSoft;
		xFontVar = kxSoft;
	}
	void CopyFrom(ChrpInheritance & chrpi)
	{
		xFont = chrpi.xFont;
		xSize = chrpi.xSize;
		xItalic = chrpi.xItalic;
		xBold = chrpi.xBold;
		xSs = chrpi.xSs;
		xOffset = chrpi.xOffset;
		xFore = chrpi.xFore;
		xBack = chrpi.xBack;
		xUnder = chrpi.xUnder;
		xUnderT = chrpi.xUnderT;
		xFontVar = chrpi.xFontVar;
	}

};

typedef Vector<ChrpInheritance> ChrpInherVec;	// Hungarian: vchrpi

/*----------------------------------------------------------------------------------------------
	This class stores the information about one style displayed in the AfStyleFntDlg.
	Hungarian: esi.
----------------------------------------------------------------------------------------------*/
class WsStyleInfo
{
public:
	int m_ws;
	StrUni m_stuFontFamily;
	StrUni m_stuFontVar;
	int m_mpSize;
	COLORREF m_clrFore;
	COLORREF m_clrBack;
	COLORREF m_clrUnder;
	int m_unt;
	int m_fBold;  // all these flags can also take the value conflicting
	int m_fItalic;
	int m_ssv;
	int m_mpOffset;
	bool m_fSelected;

	void Init()
	{
		// Default initial state is all conflicting (including the font family,
		// represented by an empty string).
		m_stuFontFamily = L"";
		m_stuFontVar = L"";
		m_clrBack = (COLORREF)knNinch;
		m_clrFore = (COLORREF)knNinch;
		m_clrUnder = (COLORREF)knNinch;
		m_unt = knNinch;
		m_fBold = knNinch;
		m_fItalic = knNinch;
		m_mpOffset = knNinch;
		m_mpSize = knNinch;
		m_ssv = knNinch;
		// Arbitrarily init this to true, since newly created ones are in the vector
		// and initially selected.
		m_fSelected = true;
	}
	WsStyleInfo()
	{
		Init();
	}
};

typedef Vector<WsStyleInfo> WsStyleVec;		// Hungarian: vesi

// The next three lines are useful for Steve McConnel's editing with Emacs.
// Local Variables:
// mode:C++
// End:

#endif //!UTILWSSTYLE_H
