#include "stdafx.h"
#include "TextDocument.h"

//default constructor
TextDocument::TextDocument()
{
	//select = new TextSelection;
	//range = new TextRange;
	saved = tomTrue;
	start = 0;
	end = 0;
	range.SetRange(start,end);
	rngList.AddHead(range);
}
//destructor
TextDocument::~TextDocument()
{
	//delete select;
	//delete range;
	rngList.RemoveAll();
}
//get the name file of current document
STDMETHODIMP TextDocument::GetName(BSTR *pName)
{
	*pName = fileName;
	return S_OK;
}
//to get the status whether the document has been changed since the last time it's saved
STDMETHODIMP TextDocument::GetSaved(long *pValue)
{
	*pValue = saved;
	return S_OK;
}
// get the active selection
STDMETHODIMP TextDocument::GetSelection(ITextSelection **ppSel)
{
	//*ppSel = select;
	*ppSel = &select;
	return S_OK;
}
// new text document
STDMETHODIMP TextDocument::New()
{
	rngList.RemoveAll();
	fileName = L"Untitled";
	//select = new TextSelection;
	//range = new TextRange;
	range.SetRange(0,0);
	rngList.AddHead(range);
	saved = tomTrue;
	return S_OK;
}
//open a file with different types and code page
STDMETHODIMP TextDocument::Open(VARIANT *pVar, long Flags, long CodePage)
{
	return E_NOTIMPL;
}
// get active range
STDMETHODIMP TextDocument::Range(long cp1,long cp2, ITextRange **ppRange)
{
	if(cp1 <= cp2)
	{
		start = cp1;
		end = cp1+cp2;
	}
	else
	{
		start = cp2;
		end = cp1+cp2;
	}

	for(int i = 0; i < rngList.GetCount(); i++)
	{
		long startPos, endPos;
		range = rngList.GetAt(rngList.FindIndex(i));
		range.GetStart(&startPos);
		range.GetEnd(&endPos);
		if(startPos == start && endPos == end)
			break;
		if(i == (rngList.GetCount() - 1) && startPos != start)
			range = rngList.GetHead();
	}

	*ppRange = &range;
	return S_OK;
}
// retrieve the active range nearest from a point pt(x,y)
STDMETHODIMP TextDocument::RangeFromPoint(long x, long y, ITextRange **ppRange)
{
	return E_NOTIMPL;
}
//multiple redo
STDMETHODIMP TextDocument::Redo(long Count, long *prop)
{
	return E_NOTIMPL;
}

//save to a file
STDMETHODIMP TextDocument::Save(VARIANT *pVar, long Flags, long CodePage)
{
	return E_NOTIMPL;
}

//used when no tab exists beyond the current display position
STDMETHODIMP TextDocument::SetDefaultTabStop(float Value)
{
	return E_NOTIMPL;
}

//set the document when it is changed
STDMETHODIMP TextDocument::SetSaved(long Value)
{
	saved = Value;
	return S_OK;
}
//multiple undo
STDMETHODIMP TextDocument::Undo(long Count, long *prop)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextDocument::GetStoryCount(long *)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextDocument::GetStoryRanges(ITextStoryRanges ** )
{
	return E_NOTIMPL;
}
STDMETHODIMP TextDocument::GetDefaultTabStop(float *)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextDocument::Freeze(long *)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextDocument::BeginEditCollection(void)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextDocument::EndEditCollection(void)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextDocument::Unfreeze(long *pCount)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextDocument::GetStoryLength(long *pCount)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextDocument::GetStoryType(long *pValue)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextDocument::GetTypeInfoCount(UINT *pctinfo)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextDocument::GetTypeInfo(UINT,LCID,ITypeInfo ** )
{
	return E_NOTIMPL;
}

STDMETHODIMP TextDocument::GetIDsOfNames(const IID &,LPOLESTR *, UINT,LCID,DISPID*)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextDocument::Invoke(DISPID,const IID &,LCID,WORD,DISPPARAMS *,VARIANT *,EXCEPINFO *,UINT *)
{
	return E_NOTIMPL;
}
STDMETHODIMP TextDocument::QueryInterface(REFIID riid,void** ppvObject)
{
	return S_OK;
}
ULONG STDMETHODCALLTYPE TextDocument::AddRef( void)
{
	return 0;
}
ULONG STDMETHODCALLTYPE TextDocument::Release( void)
{
	return 0;
}

STDMETHODIMP TextDocument::SetRange(long cp1,long cp2, TextRange newRange)
{
	if(cp1 <= cp2)
	{
		start = cp1;
		end = cp1+cp2;
	}
	else
	{
		start = cp2;
		end = cp1+cp2;
	}
	for(int i = 0; i < rngList.GetCount(); i++)
	{
		long startPos, endPos;
		range = rngList.GetAt(rngList.FindIndex(i));
		range.GetStart(&startPos);
		range.GetEnd(&endPos);
		if(startPos == start && endPos == end)
			rngList.SetAt(rngList.FindIndex(i),newRange);
		else
			rngList.AddTail(range);
	}
	return S_OK;
}

STDMETHODIMP TextDocument::SetSelection(TextSelection newSelect)
{
	select = newSelect;
	return S_OK;
}
