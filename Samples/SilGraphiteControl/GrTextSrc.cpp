#include "stdafx.h"
#include "GrClient.h"
#include "GrTextSrc.h"

GrTextSrc::GrTextSrc(OLECHAR * pszText, LgCharRenderProps & chrp,
		OLECHAR * prgchFontVar, int ichFontVar)
{
	m_cchLength = wcslen(pszText);
	m_prgchText = new OLECHAR[m_cchLength + 1];
	memcpy(m_prgchText, pszText, isizeof(OLECHAR) * m_cchLength);
	m_prgchText[m_cchLength] = L'\0'; // zero-terminate

	memcpy(&m_chrp, &chrp, isizeof(LgCharRenderProps));
	memset(m_rgchFontVar, 0, isizeof(m_rgchFontVar));
	memcpy(m_rgchFontVar, prgchFontVar, min(ichFontVar, isizeof(m_rgchFontVar)));
}

GrTextSrc::~GrTextSrc()
{
	delete [] m_prgchText;
}

GrResult GrTextSrc::Fetch(int ichMin, int ichLim, OLECHAR * prgchBuf)
{
	if (ichLim > m_cchLength)
		return kresInvalidArg;

	memcpy(prgchBuf, m_prgchText + ichMin, isizeof(OLECHAR) * (ichLim - ichMin));
	return kresOk;
}

GrResult GrTextSrc::get_Length(int * pcch)
{
	*pcch = m_cchLength;
	return kresOk;
}

GrResult GrTextSrc::GetCharProps(int ich, LgCharRenderProps * pchrp,
int * pichMin, int * pichLim)
{
	memcpy(pchrp, &m_chrp, isizeof(LgCharRenderProps));
	*pichMin = 0;
	*pichLim = m_cchLength;
	return kresOk;
}

GrResult GrTextSrc::GetParaProps(int ich, LgParaRenderProps * pchrp,
int * pichMin, int * pichLim)
{
	memset(pchrp, 0, isizeof(LgParaRenderProps));
	return kresNotImpl;
}

GrResult GrTextSrc::GetFontVariations(int ich,
		OLECHAR * prgchFontVar, int ichMax, int * pich,
		int * pichMin, int * pichLim)
{
	*pich = isizeof(m_rgchFontVar);
	if (ichMax < *pich)
		return kresFalse;

	memcpy(prgchFontVar, m_rgchFontVar, isizeof(m_rgchFontVar));

	*pichMin = 0;
	*pichLim = m_cchLength;

	return kresOk;
}

// Return a Graphite-compatible text source that can be stored in a
//  Graphite segment.
void GrTextSrc::TextSrcObject(IGrTextSource ** ppgts)
{
		*ppgts = this;
}

// When a segment is being destroyed, there is nothing to do,
//  since this is a pointer to an object that is allocated elsewhere.
void GrTextSrc::DeleteTextSrcPtr()
{
}