#ifndef GRTEXTSOURCE_INCLUDED
#define GRTEXTSOURCE_INCLUDED

#include "IGrTextSource.h"

class GrTextSrc : public IGrTextSource
{
public:
	GrTextSrc(OLECHAR * pszText, LgCharRenderProps & chrp,
		OLECHAR * prgchFontVar, int ichFontVar);
	~GrTextSrc();

	virtual GrResult Fetch(int ichMin, int ichLim, OLECHAR * prgchBuf);
	virtual GrResult get_Length(int * pcch);
	virtual GrResult GetCharProps(int ich, LgCharRenderProps * pchrp,
		int * pichMin, int * pichLim);
	virtual GrResult GetParaProps(int ich, LgParaRenderProps * pchrp,
		int * pichMin, int * pichLim);
	virtual GrResult GetFontVariations(int ich,
		OLECHAR * prgchFontVar, int ichMax, int * pich,
		int * pichMin, int * pichLim);
	virtual void TextSrcObject(IGrTextSource ** ppgts);
	virtual void DeleteTextSrcPtr();

protected:
	OLECHAR * m_prgchText;
	int m_cchLength;
	OLECHAR m_rgchFontVar[64];
	LgCharRenderProps m_chrp;
};

#endif
