#include<tom.h>
#include "TextSelection.h"
#include "TextRange.h"
#include <afxtempl.h>	// For CList

class TextDocument: public ITextDocument
{
public:
	TextDocument();
	~TextDocument();
	STDMETHODIMP GetName(BSTR *pName) ;
	STDMETHODIMP GetSaved(long *pValue);
	STDMETHODIMP GetSelection(ITextSelection **ppSel);
	STDMETHODIMP New();
	STDMETHODIMP Open(VARIANT *pVar, long Flags, long CodePage);
	STDMETHODIMP Range(long cp1,long cp2, ITextRange **ppRange);
	STDMETHODIMP RangeFromPoint(long x, long y, ITextRange **ppRange);
	STDMETHODIMP Redo(long Count, long *prop);
	STDMETHODIMP Save(VARIANT *pVar, long Flags, long CodePage);
	STDMETHODIMP SetDefaultTabStop(float Value);
	STDMETHODIMP SetSaved(long Value);
	STDMETHODIMP Undo(long Count, long *prop);
	STDMETHODIMP Unfreeze(long *pCount);
	STDMETHODIMP GetStoryLength(long *pCount);
	STDMETHODIMP GetStoryType(long *pValue);
	STDMETHODIMP GetTypeInfoCount(UINT *pctinfo);
	STDMETHODIMP GetTypeInfo(UINT,LCID,ITypeInfo ** );
	STDMETHODIMP GetIDsOfNames(const IID &,LPOLESTR *, UINT,LCID,DISPID*);
	STDMETHODIMP Invoke(DISPID,const IID &,LCID,WORD,DISPPARAMS *,VARIANT *,EXCEPINFO *,UINT *);
	STDMETHODIMP QueryInterface(REFIID riid,void** ppvObject);
	ULONG STDMETHODCALLTYPE AddRef( void);
	ULONG STDMETHODCALLTYPE Release( void);
	STDMETHODIMP GetStoryCount(long *);
	STDMETHODIMP GetStoryRanges(ITextStoryRanges ** );
	STDMETHODIMP GetDefaultTabStop(float *);
	STDMETHODIMP Freeze(long *);
	STDMETHODIMP BeginEditCollection(void);
	STDMETHODIMP EndEditCollection(void);
	STDMETHODIMP SetRange(long cp1,long cp2, TextRange newRange); //custom-made
	STDMETHODIMP SetSelection(TextSelection newSelect);

public:
	BSTR fileName;
	//ITextSelection *select;
	//ITextRange *range;
	TextRange range;
	TextSelection select;
	CList<TextRange,TextRange> rngList;
	long Flags;
	long saved;
	long start;
	long end;
};
