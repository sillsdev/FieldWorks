#include "stdafx.h"
#include "TextSelection.h"

TextSelection::TextSelection()
{
	Text = L"";
	Flags = tomSelActive;
	Start = 0;
	End = 0;
}

TextSelection::TextSelection(BSTR text)
{
	Flags = tomSelActive;
	Start = 0;
	End = 0;
	Text = text;
}

TextSelection::~TextSelection()
{

}

STDMETHODIMP TextSelection::TypeText(BSTR bstr)
{
	Flags = tomSelActive;
	Text = bstr;
	return S_OK;
}

STDMETHODIMP TextSelection::GetType(BSTR *pbstr)
{
	*pbstr = Text;
	return S_OK;
}

STDMETHODIMP TextSelection::SetFlags(long Flags)
{
	this->Flags = Flags;
	return S_OK;
}

STDMETHODIMP TextSelection::GetFlags(long *pFlags)
{
	*pFlags = Flags;
	return S_OK;
}

STDMETHODIMP TextSelection::EndKey(long Unit, long Extend, long *pDelta)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::HomeKey(long Unit, long Extend, long *pDelta)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::MoveUp(long Unit, long Count, long Extend, long *pDelta)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::MoveDown(long Unit, long Count, long Extend, long *pDelta)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::MoveRight(long Unit, long Count, long Extend, long *pDelta)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::MoveLeft(long Unit, long Count, long Extend, long *pDelta)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::SetStart(long cpFirst)
{
	Start = cpFirst;
	return S_OK;
}

STDMETHODIMP TextSelection::CanEdit(long *pbCanEdit)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::CanPaste(VARIANT *pVar, long Format, long *pb)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::ChangeCase(long Type)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::Collapse(long bStart)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::Copy(VARIANT *pVar)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::Cut(VARIANT *pVar)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::Delete(long Unit,long Count,long *pDelta)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::EndOf(long Unit,long Extend,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::Expand(long Unit,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::FindText(BSTR bstr,long Count,long Flags,long *pLength)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::FindTextEnd(BSTR bstr,long Count,long Flags,long *pLength)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::FindTextStart(BSTR bstr,long Count,long Flags,long *pLength)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetChar(long *pChar)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::GetDuplicate(ITextRange **ppRange)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetEmbeddedObject(IUnknown **ppObject)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetEnd(long *pcpLim)
{
	*pcpLim = End;
	return S_OK;
}

STDMETHODIMP TextSelection::GetFont(ITextFont **ppFont)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetFormattedText(ITextRange **ppRange)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetIndex(long Unit,long *pIndex)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetPara(ITextPara **ppPara)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetPoint(long Type,long *px,long *py)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetStart(long *pcpFirst)
{
	*pcpFirst = Start;
	return S_OK;
}

STDMETHODIMP TextSelection::GetStoryLength(long *pCount)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetStoryType(long *pValue)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetText(BSTR *pbstr)
{
	*pbstr = Text;
	return S_OK;
}
STDMETHODIMP TextSelection::InRange(ITextRange *pRange,long *pB)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::GetType(long *pType)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::InStory(ITextRange *pRange,long *pB)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::IsEqual(ITextRange *pRange,long *pB)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::Move(long Unit,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::MoveEnd(long Unit,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::MoveEndUntil(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::MoveEndWhile(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::MoveStart(long Unit,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::MoveStartUntil(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::MoveStartWhile(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::MoveUntil(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::MoveWhile(VARIANT *Cset,long Count,long *pDelta)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::Paste(VARIANT *pVar,long Format)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::ScrollIntoView(long bStart)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::Select(VOID)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::SetChar(long Char)
{
	Text[Start] = Char;
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::SetEnd(long cpLim)
{
	End = cpLim;
	return S_OK;
}
STDMETHODIMP TextSelection::SetFont(ITextFont *pFont)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::SetFormattedText(ITextRange *pRange)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::SetIndex(long Unit,long Index,long Extend)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::SetPara(ITextPara *pPara)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::SetPoint(long x,long y,long Type,long Extend)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::SetRange(long cpActive, long cpOther)
{
	Start = cpActive;
	End = cpOther;
	return S_OK;
}
//STDMETHODIMP TextSelection::SetStart(long cp)
//{
//	Start = cp;
//	return S_OK;
//}
STDMETHODIMP TextSelection::SetText(BSTR bstr)
{

	Text = bstr;
	Flags = tomSelActive;

	return S_OK;
}
STDMETHODIMP TextSelection::StartOf(long Unit,long Extend,long *pDelta)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::QueryInterface(REFIID riid,void** ppvObject)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::GetTypeInfoCount(UINT *pctinfo)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextSelection::GetTypeInfo(UINT,LCID,ITypeInfo ** )
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::GetIDsOfNames(const IID &,LPOLESTR *, UINT,LCID,DISPID*)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextSelection::Invoke(DISPID,const IID &,LCID,WORD,DISPPARAMS *,VARIANT *,EXCEPINFO *,UINT *)
{
	return E_NOTIMPL;
}

ULONG STDMETHODCALLTYPE TextSelection::AddRef( void)
{
	return 0;
}

ULONG STDMETHODCALLTYPE TextSelection::Release( void)
{
	return 0;
}
