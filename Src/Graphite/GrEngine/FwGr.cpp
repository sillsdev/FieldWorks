/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: FwGr.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Methods from the FwGr classes that cannot be put in the header.

TODO: figure out if there is a way to put this in the Views Lib stuff.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#include "IcuCommon.h"

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	FwGrTxtSrc
//:>********************************************************************************************

size_t FwGrTxtSrc::fetch(toffset ichMin, size_t cch, gr::utf16 * prgchwBuffer)
{
	HRESULT hr;
	int cchLen;
	IgnoreHr(hr = (gr::GrResult)m_qts->get_Length(&cchLen));
	if (FAILED(hr))
		throw;

	if (!m_useNFC)
	{
		int cchGet = min(cch, cchLen - ichMin);
		IgnoreHr(hr = (gr::GrResult)m_qts->Fetch(ichMin, ichMin + cchGet, (OLECHAR*)prgchwBuffer));
		if (FAILED(hr))
			throw;
		return cchGet;
	}
	else
	{
		int vwIchMin = GrToVwOffset(ichMin);
		int len = cchLen - vwIchMin;
		wchar_t* pchNfd;
		StrUni stuText;
		stuText.SetSize(len + 1, &pchNfd);
		IgnoreHr(hr = (gr::GrResult)m_qts->Fetch(vwIchMin, vwIchMin + len, pchNfd));
		if (FAILED(hr))
			throw;
		pchNfd[len] = '\0';
		stuText.SetSize(len, &pchNfd);
		StrUtil::NormalizeStrUni(stuText, UNORM_NFC);
		int cchGet = min((int) cch, stuText.Length());
		wcsncpy((wchar_t*) prgchwBuffer, stuText.Chars(), cchGet);
		return cchGet;
	}
}

/*----------------------------------------------------------------------------------------------
	Return true if the text uses a right-to-left writing system.
----------------------------------------------------------------------------------------------*/
bool FwGrTxtSrc::getRightToLeft(toffset ich)
{
	LgCharRenderProps lgchrp;
	int ichMinBogus, ichLimBogus;
	GrResult res = (GrResult)m_qts->GetCharProps(GrToVwOffset(ich), &lgchrp, &ichMinBogus, &ichLimBogus);

	return lgchrp.fWsRtl;
}

/*----------------------------------------------------------------------------------------------
	Return true if the text uses a right-to-left writing system.
----------------------------------------------------------------------------------------------*/
unsigned int FwGrTxtSrc::getDirectionDepth(toffset ich)
{
	LgCharRenderProps lgchrp;
	int ichMinBogus, ichLimBogus;
	GrResult res = (GrResult)m_qts->GetCharProps(GrToVwOffset(ich), &lgchrp, &ichMinBogus, &ichLimBogus);

	return lgchrp.nDirDepth;
}

/*----------------------------------------------------------------------------------------------
	Return the size of the text, in points.
----------------------------------------------------------------------------------------------*/
float FwGrTxtSrc::getVerticalOffset(toffset ich)
{
	LgCharRenderProps lgchrp;
	int ichMinBogus, ichLimBogus;
	GrResult res = (GrResult)m_qts->GetCharProps(GrToVwOffset(ich), &lgchrp, &ichMinBogus, &ichLimBogus);

	int dympOffset = lgchrp.dympOffset;

	if(lgchrp.ssv != kssvOff)
	{
		// psuedo device context works since we are getting proportions which are invariant
		HDC hdc = ::CreateDC(TEXT("DISPLAY"),NULL,NULL,NULL);

		IVwGraphicsWin32Ptr qvgW;
		qvgW.CreateInstance(CLSID_VwGraphicsWin32);
		qvgW->Initialize(hdc);

		IVwGraphicsPtr qvg;
		CheckHr(qvgW->QueryInterface(IID_IVwGraphics, (void **) &qvg));
		CheckHr(qvg->SetupGraphics(&lgchrp));

		if (lgchrp.ssv == kssvSuper)
		{
			int dSuperscriptYOffsetNumerator;
			int dSuperscriptYOffsetDenominator;
			CheckHr(qvg->GetSuperscriptYOffsetRatio(&dSuperscriptYOffsetNumerator, &dSuperscriptYOffsetDenominator));
			dympOffset += gr::GrEngine::GrIntMulDiv(lgchrp.dympHeight, dSuperscriptYOffsetNumerator, dSuperscriptYOffsetDenominator);
		}
		else if (lgchrp.ssv == kssvSub)
		{
			int dSubscriptYOffsetNumerator;
			int dSubscriptYOffsetDenominator;
			CheckHr(qvg->GetSubscriptYOffsetRatio(&dSubscriptYOffsetNumerator, &dSubscriptYOffsetDenominator));
			dympOffset -= gr::GrEngine::GrIntMulDiv(lgchrp.dympHeight, dSubscriptYOffsetNumerator, dSubscriptYOffsetDenominator);
		}
		qvgW.Clear();
		qvg.Clear();
		::DeleteDC(hdc);
	}

	return (float)(dympOffset / 1000);
}

/*----------------------------------------------------------------------------------------------
	Return the ISO-639-3 language code, cast to an unsigned int.
----------------------------------------------------------------------------------------------*/
isocode FwGrTxtSrc::getLanguage(toffset ich)
{
	LgCharRenderProps lgchrp;
	int ichMinBogus, ichLimBogus;
	GrResult res = (GrResult)m_qts->GetCharProps(GrToVwOffset(ich), &lgchrp, &ichMinBogus, &ichLimBogus);

	int ws = lgchrp.ws;

	ILgWritingSystemFactoryPtr qwsf;
	ILgWritingSystemPtr qws;
	CheckHr(m_qts->GetWsFactory(&qwsf));
	AssertPtr(qwsf);
	CheckHr(qwsf->get_EngineOrNull(ws, &qws));

	isocode code;
	if (!qws)
	{
		memset(code.rgch, 0, sizeof(char) * 4);
		return code;
	}

	SmartBstr bstrLang;
	CheckHr(qws->get_ISO3(&bstrLang));

	for (int i = 0; i < 4; i++)
		code.rgch[i] = (char)(i >= bstrLang.Length() ? 0 : bstrLang[i]);
	return code;
}

/*----------------------------------------------------------------------------------------------
	Return the indices of a range of characters that all share the same properties.
----------------------------------------------------------------------------------------------*/
std::pair<toffset, toffset> FwGrTxtSrc::propertyRange(toffset ich)
{
	LgCharRenderProps lgchrp;
	int ichMin, ichLim;
	GrResult res = (GrResult)m_qts->GetCharProps(GrToVwOffset(ich), &lgchrp, &ichMin, &ichLim);

	// for testing UTF-8:
	//if (ichMin != 0)
	//{
	//	Assert(false);
	//}
	//gr::utf16 prgchw[1000];
	//fetch(0, ichLim, prgchw);
	//ichLim = CountUtf8FromUtf16(prgchw, *pichLim);
	////////////////////

	std::pair<toffset, toffset> pairRet;
	pairRet.first = VwToGrOffset(ichMin);
	pairRet.second = VwToGrOffset(ichLim);
	return pairRet;
}

/*----------------------------------------------------------------------------------------------
	Return a list of font feature settings for the given character.
----------------------------------------------------------------------------------------------*/
size_t FwGrTxtSrc::getFontFeatures(toffset ich, FeatureSetting * prgfset)
{
	int ichMinBogus, ichLimBogus;

	int cfset = 0;

	int vwIch = GrToVwOffset(ich);
	// The first batch of features comes from the style setting.
	LgCharRenderProps lgchrp;
	GrResult res = (GrResult)m_qts->GetCharProps(vwIch, &lgchrp, &ichMinBogus, &ichLimBogus);
	if (ResultFailed(res))
		return cfset;
#if WIN32
	std::wstring stuFromStyle(lgchrp.szFontVar);
#else
	std::wstring stuFromStyle = StrUtil::wstring(lgchrp.szFontVar);
#endif
	ParseFeatureString(stuFromStyle, kMaxFeatures, prgfset, &cfset);

	// The second batch comes from the hard-formatting on the text.
	// Because of the way the strings are stored this step is probably not necessary
	// (the first string is a superset of the second), but just to be on the safe side...
	SmartBstr sbstrString;
	res = (GrResult)m_qts->GetCharStringProp(vwIch, ktptFontVariations, &sbstrString,
		&ichMinBogus, &ichLimBogus);
	if (ResultFailed(res))
		return cfset;

	if (sbstrString)
	{
		std::wstring stuFromString(sbstrString);
		if (stuFromString != stuFromStyle)
			ParseFeatureString(stuFromString, kMaxFeatures, prgfset, &cfset);
	}

	Assert(cfset < kMaxFeatures); // otherwise memory has been clobbered

	//for (int ifeat = 0; ifeat < min(*pcfset, cMax); ifeat++)
	//{
	//	prgfset[ifeat].id = rgFeatIDs[ifeat];
	//	prgfset[ifeat].value = rgValues[ifeat];
	//}

	return cfset;
}

/*----------------------------------------------------------------------------------------------
	Convert a feature string as used by FieldWorks into a set of feature-IDs-plus-values.
	Private.

	TODO: find a way to merge this method with the FwGrEngine version.
----------------------------------------------------------------------------------------------*/
void FwGrTxtSrc::ParseFeatureString(std::wstring stuFeat, int cMax,
	FeatureSetting * prgfset, int * pcFeat)
{
	wchar_t * pchw = const_cast<wchar_t *>(stuFeat.data());
	wchar_t * pchwLim = pchw + stuFeat.size();
	while (pchw < pchwLim)
	{
		int nID = 0;
		int nValue = 0;
		bool fNeg = false;

		//	Read the ID.
		while (*pchw != '=' && *pchw != ' ')
		{
			if (*pchw < '0' || *pchw > '9')
				goto LNext;	// syntax error: skip this setting
			nID = nID * 10 + (*pchw - '0');
			pchw++;
		}
		while (*pchw == ' ')
			pchw++;
		Assert(*pchw == '=');
		pchw++;
		while (*pchw == ' ')
			pchw++;

		//	Read the value.
		if (*pchw == '"')
		{
			//	Language ID string--form an integer out of the first four bytes, ignore
			//	the rest.
			pchw++;	// skip quote
			byte b1 = (*pchw != '"') ? *pchw++ : 0;
			byte b2 = (*pchw != '"') ? *pchw++ : 0;
			byte b3 = (*pchw != '"') ? *pchw++ : 0;
			byte b4 = (*pchw != '"') ? *pchw++ : 0;
			while (pchw < pchwLim  && *pchw != '"')	// skip superfluous chars
				pchw++;
			if (pchw >= pchwLim)
				goto LNext;
			pchw++;	// skip quote
			nValue = (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
		}
		else
		{
			//	Numerical value
			if (*pchw == '-')
			{
				pchw++;
				fNeg = true;
			}
			else if (*pchw == '+')
			{
				pchw++;
				fNeg = false;
			}
			while (*pchw != ',' && *pchw != ' ' && pchw < pchwLim)
			{
				if (*pchw < '0' || *pchw > '9')
					goto LNext;	// syntax error skip this setting
				nValue = nValue * 10 + (*pchw - '0');
				pchw++;
			}
			if (fNeg)
				nValue = nValue * -1;
		}

		//	Set the feature value.
		int ifeat;
		for (ifeat = 0; ifeat < *pcFeat; ifeat++)
		{
			if (prgfset[ifeat].id == nID)
				break;	// already in list; overwrite
		}
		if (ifeat < cMax)
		{
			prgfset[ifeat].id = nID;
			prgfset[ifeat].value = nValue;
		}
		// else: don't write past the end of the available space
		*pcFeat = max(*pcFeat, ifeat + 1);

LNext:
		//	Find the next setting.
		while (pchw < pchwLim && *pchw != ',')
			pchw++;
		while (pchw < pchwLim && (*pchw < '0' || *pchw > '9'))
			pchw++;
	}
}

/*----------------------------------------------------------------------------------------------
	Return true if the two characters represent runs that can be rendered in the same
	segment.
----------------------------------------------------------------------------------------------*/
bool FwGrTxtSrc::sameSegment(toffset ich1, toffset ich2)
{
	int ichMinBogus, ichLimBogus;
	LgCharRenderProps chrp1, chrp2;
	GrResult res = (GrResult)m_qts->GetCharProps(GrToVwOffset(ich1), &chrp1, &ichMinBogus, &ichLimBogus);
	if (ResultFailed(res))
		return false;
	res = (GrResult)m_qts->GetCharProps(GrToVwOffset(ich2), &chrp2, &ichMinBogus, &ichLimBogus);
	if (ResultFailed(res))
		return false;

	if (u_strcmp(chrp1.szFaceName, chrp2.szFaceName) != 0)
		return false;
	if (chrp1.ws != chrp2.ws)
	{
		// Can't compare default fonts for different writing systems.
		StrUni stuFace;
		stuFace.Assign(chrp1.szFaceName, 8);
		if (stuFace == L"<default")
			return false;
	}
	if (chrp1.dympHeight != chrp2.dympHeight)
		return false;
	if (chrp1.ttvBold != chrp2.ttvBold)
		return false;
	if (chrp1.ttvItalic != chrp2.ttvItalic)
		return false;
	if (chrp1.fWsRtl != chrp2.fWsRtl)
		return false;
	if (chrp1.nDirDepth != chrp2.nDirDepth)
		return false;
	if (chrp1.ssv != chrp2.ssv)
		return false;
	if (chrp1.dympOffset != chrp2.dympOffset)
		return false; // eventually improve on this

	return true;
}

/*----------------------------------------------------------------------------------------------
	Temporary--eventually these will be of interest only to SegmentPainter.
----------------------------------------------------------------------------------------------*/
void FwGrTxtSrc::getColors(toffset ich, int * pclrFore, int * pclrBack)
{
	LgCharRenderProps lgchrp;
	int ichMinBogus, ichLimBogus;
	GrResult res = (GrResult)m_qts->GetCharProps(GrToVwOffset(ich), &lgchrp, &ichMinBogus, &ichLimBogus);

	*pclrFore = (int)lgchrp.clrFore;
	*pclrBack = (int)lgchrp.clrBack;
}

/*----------------------------------------------------------------------------------------------
	Get the character properties out of the text source.
	Not part of ITextSource interface; specific to FwGrTxtSrc.
----------------------------------------------------------------------------------------------*/

GrResult FwGrTxtSrc::GetCharProps(int ich, LgCharRenderProps * pchrp,
	int * pichMin, int * pichLim)
{
	return (GrResult)m_qts->GetCharProps(ich, pchrp, pichMin, pichLim);
}

/*----------------------------------------------------------------------------------------------
	Return the size of the text, in points.
	Not part of ITextSource interface; specific to FwGrTxtSrc.
----------------------------------------------------------------------------------------------*/
float FwGrTxtSrc::GetFontSize(int ich)
{
	LgCharRenderProps lgchrp;
	int ichMinBogus, ichLimBogus;
	GrResult res = (GrResult)m_qts->GetCharProps(ich, &lgchrp, &ichMinBogus, &ichLimBogus);

	int dympHeight = lgchrp.dympHeight;
	if(lgchrp.ssv != kssvOff)
	{
		// psuedo device context works since we are getting proportions which are invariant
		HDC hdc = ::CreateDC(TEXT("DISPLAY"),NULL,NULL,NULL);

		IVwGraphicsWin32Ptr qvgW;
		qvgW.CreateInstance(CLSID_VwGraphicsWin32);
		qvgW->Initialize(hdc);

		IVwGraphicsPtr qvg;
		CheckHr(qvgW->QueryInterface(IID_IVwGraphics, (void **) &qvg));
		CheckHr(qvg->SetupGraphics(&lgchrp));

		int dSizeNumerator = 1;
		int dSizeDenominator = 1;
		if (lgchrp.ssv == kssvSuper)
		{
			CheckHr(qvg->GetSuperscriptHeightRatio(&dSizeNumerator, &dSizeDenominator));
		}
		else if (lgchrp.ssv == kssvSub)
		{
			CheckHr(qvg->GetSubscriptHeightRatio(&dSizeNumerator, &dSizeDenominator));
		}
		dympHeight = gr::GrEngine::GrIntMulDiv(dympHeight, dSizeNumerator, dSizeDenominator);

		qvgW.Clear();
		qvg.Clear();
		::DeleteDC(hdc);
	}

	return (float)(dympHeight / 1000);
}

int FwGrTxtSrc::VwToGrOffset(int vwOffset, bool& badOffset)
{
	badOffset = false;
	if (!m_useNFC)
	{
		// the NFD offset is a Graphite offset
		return vwOffset;
	}
	else
	{
		// convert internal NFD offsets to NFC offsets
		if (vwOffset == 0)
			return 0;

		HRESULT hr;
		int cch;
		IgnoreHr(hr = m_qts->get_Length(&cch));
		if (FAILED(hr))
			throw;
		if (vwOffset > cch)
			return vwOffset;

		StrUni stuNfd;
		wchar_t* pchNfd;
		stuNfd.SetSize(cch + 1, &pchNfd);
		IgnoreHr(hr = m_qts->Fetch(0, cch, pchNfd));
		if (FAILED(hr))
			throw;
		pchNfd[cch] = '\0';

		wchar_t szOut[kNFDBufferSize];
		UCharIterator iter;
		uiter_setString(&iter, pchNfd, -1);
		int curGrOffset = 0;
		while (iter.hasNext(&iter))
		{
			int index = iter.getIndex(&iter, UITER_CURRENT);
			UBool neededToNormalize;
			UErrorCode uerr = U_ZERO_ERROR;
			int outLen = unorm_next(&iter, szOut, kNFDBufferSize, UNORM_NFC, 0, TRUE, &neededToNormalize, &uerr);
			Assert(U_SUCCESS(uerr));
			for (int i = 0; i < outLen; i++)
			{
				if (index + i + 1 > vwOffset)
					return curGrOffset;
				curGrOffset++;
			}
			if (neededToNormalize && iter.getIndex(&iter, UITER_CURRENT) > vwOffset)
				badOffset = true;
		}
		return curGrOffset;
	}
}

/*----------------------------------------------------------------------------------------------
	Convert the decomposed NFD character offset (used by views code) to the
	character offset used by Graphite.
	The NFD character offset is the same as the logical character offset.
----------------------------------------------------------------------------------------------*/
int FwGrTxtSrc::VwToGrOffset(int vwOffset)
{
	bool badOffset;
	return VwToGrOffset(vwOffset, badOffset);
}

/*----------------------------------------------------------------------------------------------
	Convert the Graphite character offset to the decomposed NFD
	character offset used internally by views code.
----------------------------------------------------------------------------------------------*/
int FwGrTxtSrc::GrToVwOffset(int grOffset)
{
	if (!m_useNFC)
	{
		// the Graphite offset is a NFD offset
		return grOffset;
	}
	else
	{
		// convert NFC offsets to internal NFD offsets
		if (grOffset == 0)
			return 0;

		HRESULT hr;
		int cch;
		IgnoreHr(hr = m_qts->get_Length(&cch));
		if (FAILED(hr))
			throw;

		if (grOffset > cch)
			// grOffset points beyond the available text, i.e. is invalid.
			return cch + 10; // arbitrary number that is bigger than NFD text

		StrUni stuNfd;
		wchar_t* pchNfd;
		stuNfd.SetSize(cch + 1, &pchNfd);
		IgnoreHr(hr = m_qts->Fetch(0, cch, pchNfd));
		if (FAILED(hr))
			throw;
		pchNfd[cch] = '\0';

		wchar_t szOut[kNFDBufferSize];
		UCharIterator iter;
		uiter_setString(&iter, pchNfd, -1);
		int curGrOffset = 0;
		while (iter.hasNext(&iter))
		{
			int index = iter.getIndex(&iter, UITER_CURRENT);
			if (curGrOffset >= grOffset)
				return index;
			UBool neededToNormalize;
			UErrorCode uerr = U_ZERO_ERROR;
			int outLen = unorm_next(&iter, szOut, kNFDBufferSize, UNORM_NFC, 0, TRUE, &neededToNormalize, &uerr);
			Assert(U_SUCCESS(uerr));
			curGrOffset++;
			for (int i = 1; i < outLen; i++)
			{
				if (curGrOffset >= grOffset)
					return index + i;
				curGrOffset++;
			}
		}
		return iter.getIndex(&iter, UITER_CURRENT);
	}
}

//:>********************************************************************************************
//:>	FwGrJustifier
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Determine how to kern the glyphs to get a justified effect.
	Return kresFalse if we can't achieve the desired width.
----------------------------------------------------------------------------------------------*/
GrResult FwGrJustifier::adjustGlyphWidths(gr::GraphiteProcess * pgproc,
	int iGlyphMin, int iGlyphLim,
	float dxCurrentWidth, float dxDesiredWidth)
{
	gr::GrEngine * pgreng = dynamic_cast<gr::GrEngine *>(pgproc);
	FwGraphiteProcess fgje;
	fgje.SetProcess(pgreng);
	FwGraphiteProcess * pfgje = &fgje;

	IJustifyingRendererPtr qjrend;
	CheckHr(pfgje->QueryInterface(IID_IJustifyingRenderer, (void **)&qjrend));
	return (GrResult)m_qjus->AdjustGlyphWidths(qjrend, iGlyphMin, iGlyphLim,
		dxCurrentWidth, dxDesiredWidth);
}

/*----------------------------------------------------------------------------------------------
	Suggest how much shrinking is possible for low-end justification.
----------------------------------------------------------------------------------------------*/
//GrResult FwGrJustifier::suggestShrinkAndBreak(gr::GraphiteProcess * pgje,
//	int iGlyphMin, int iGlyphLim, float dxsWidth, LgLineBreak lbPref, LgLineBreak lbMax,
//	float * pdxShrink, LgLineBreak * plbToTry)
//{
//	GrEngine * pgreng = dynamic_cast<GrEngine *>(pgproc);
//	FwGraphiteProcess fgje;
//	fgje.SetProcess(pgreng);
//	FwGraphiteProcess * pfgje = &fgje;
//
//	IJustifyingRendererPtr qjrend;
//	CheckHr(pfgje->QueryInterface(IID_IJustifyingRenderer, (void **)&qjrend));
//	return (GrResult)m_qjus->SuggestShrinkAndBreak(qjrend, iGlyphMin, iGlyphLim,
//		dxsWidth, lbPref, lbMax, pdxShrink, plbToTry);
//}
