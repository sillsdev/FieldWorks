#include "stdafx.h"
#include "TextRange.h"

TextRange::TextRange()
{
	start = 0;
	end = 0;
}
TextRange::TextRange(BSTR bstr)
{
	start = 0;
	end = 0;
	text = bstr;
}
TextRange::~TextRange()
{
}

STDMETHODIMP TextRange::CanEdit(long *pbCanEdit)
{
	*pbCanEdit = editable;
	return S_OK;
}
STDMETHODIMP TextRange::CanPaste(VARIANT *pVar, long Format, long *pb)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::ChangeCase(long Type)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::Collapse(long bstart)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextRange::Copy(VARIANT *pVar)
{	return E_NOTIMPL;
}
STDMETHODIMP TextRange::Cut(VARIANT *pVar)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::Delete(long Unit,long Count,long *pDelta)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextRange::EndOf(long Unit,long Extend,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::Expand(long Unit,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::FindText(BSTR bstr,long Count,long Flags,long *pLength)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::FindTextEnd(BSTR bstr,long Count,long Flags,long *pLength)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::FindTextStart(BSTR bstr,long Count,long Flags,long *pLength)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetChar(long *pChar)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextRange::GetDuplicate(ITextRange **ppRange)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetEmbeddedObject(IUnknown **ppObject)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetEnd(long *pcpLim)
{
	*pcpLim = end;
	return S_OK;
}

STDMETHODIMP TextRange::GetFont(ITextFont **ppFont)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetFormattedText(ITextRange **ppRange)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetIndex(long Unit,long *pIndex)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetPara(ITextPara **ppPara)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetPoint(long Type,long *px,long *py)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetStart(long *pcpFirst)
{
	*pcpFirst = start;
	return S_OK;
}

STDMETHODIMP TextRange::GetStoryLength(long *pCount)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetStoryType(long *pValue)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetText(BSTR *pbstr)
{
	*pbstr = text;
	return S_OK;
}
STDMETHODIMP TextRange::InRange(ITextRange *pRange,long *pB)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::InStory(ITextRange *pRange,long *pB)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::IsEqual(ITextRange *pRange,long *pB)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::Move(long Unit,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::MoveEnd(long Unit,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::MoveEndUntil(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::MoveEndWhile(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::MoveStart(long Unit,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::MoveStartUntil(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::MoveStartWhile(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::MoveUntil(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::MoveWhile(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::Paste(VARIANT *pVar,long Format)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::ScrollIntoView(long bstart)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::Select(VOID)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::SetChar(long Char)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::SetEnd(long cp)
{
	end = cp;
	return S_OK;
}
STDMETHODIMP TextRange::SetFont(ITextFont *pFont)
{
	font = pFont;
	return S_OK;
}
STDMETHODIMP TextRange::SetFormattedText(ITextRange *pRange)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::SetIndex(long Unit,long Index,long Extend)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::SetPara(ITextPara *pPara)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::SetPoint(long x,long y,long Type,long Extend)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::SetRange(long cpActive, long cpOther)
{
	if(cpActive < cpOther)
	{
		start = cpActive;
		end = cpOther;
	}
	else
	{
		start = cpOther;
		end = cpActive;
	}

	return S_OK;
}
STDMETHODIMP TextRange::SetStart(long cp)
{
	start = cp;
	return S_OK;
}
STDMETHODIMP TextRange::SetText(BSTR bstr)
{
	text = bstr;
	return S_OK;
}
STDMETHODIMP TextRange::StartOf(long Unit,long Extend,long *pDelta)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextRange::QueryInterface(REFIID riid,void** ppvObject)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextRange::GetTypeInfoCount(UINT *pctinfo)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextRange::GetTypeInfo(UINT itinfo,LCID lcid,ITypeInfo ** pptinfo)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextRange::GetIDsOfNames(const IID &,LPOLESTR *, UINT,LCID,DISPID*)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextRange::Invoke(DISPID,const IID &,LCID,WORD,DISPPARAMS *,VARIANT *,EXCEPINFO *,UINT *)
{
	return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE TextRange::AddRef( void)
{
	return 0;
}

ULONG STDMETHODCALLTYPE TextRange::Release( void)
{
	return 0;
}
