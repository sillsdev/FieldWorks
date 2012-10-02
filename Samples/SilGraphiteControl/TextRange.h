#include <tom.h>

class TextRange : public ITextRange
{
public:
	TextRange();
	TextRange(BSTR bstr);
	~TextRange();
	STDMETHODIMP CanEdit(long *pbCanEdit);
	STDMETHODIMP CanPaste(VARIANT *pVar, long Format, long *pb);
	STDMETHODIMP ChangeCase(long Type);
	STDMETHODIMP Collapse(long bStart);
	STDMETHODIMP Copy(VARIANT *pVar);
	STDMETHODIMP Cut(VARIANT *pVar);
	STDMETHODIMP Delete(long Unit,long Count,long *pDelta);
	STDMETHODIMP EndOf(long Unit,long Extend,long *pDelta);
	STDMETHODIMP Expand(long Unit,long *pDelta);
	STDMETHODIMP FindText(BSTR bstr,long Count,long Flags,long *pLength);
	STDMETHODIMP FindTextEnd(BSTR bstr,long Count,long Flags,long *pLength);
	STDMETHODIMP FindTextStart(BSTR bstr,long Count,long Flags,long *pLength);
	STDMETHODIMP GetChar(long *pChar);
	STDMETHODIMP GetDuplicate(ITextRange **ppRange);
	STDMETHODIMP GetEmbeddedObject(IUnknown **ppObject);
	STDMETHODIMP GetEnd(long *pcpLim);
	STDMETHODIMP GetFont(ITextFont **ppFont);
	STDMETHODIMP GetFormattedText(ITextRange **ppRange);
	STDMETHODIMP GetIndex(long Unit,long *pIndex);
	STDMETHODIMP GetPara(ITextPara **ppPara);
	STDMETHODIMP GetPoint(long Type,long *px,long *py);
	STDMETHODIMP GetStart(long *pcpFirst);
	STDMETHODIMP GetStoryLength(long *pCount);
	STDMETHODIMP GetStoryType(long *pValue);
	STDMETHODIMP GetText(BSTR *pbstr);
	STDMETHODIMP InRange(ITextRange *pRange,long *pB);
	STDMETHODIMP InStory(ITextRange *pRange,long *pB);
	STDMETHODIMP IsEqual(ITextRange *pRange,long *pB);
	STDMETHODIMP Move(long Unit,long Count,long *pDelta);
	STDMETHODIMP MoveEnd(long Unit,long Count,long *pDelta);
	STDMETHODIMP MoveEndUntil(VARIANT *Cset,long Count,long *pDelta);
	STDMETHODIMP MoveEndWhile(VARIANT *Cset,long Count,long *pDelta);
	STDMETHODIMP MoveStart(long Unit,long Count,long *pDelta);
	STDMETHODIMP MoveStartUntil(VARIANT *Cset,long Count,long *pDelta);
	STDMETHODIMP MoveStartWhile(VARIANT *Cset,long Count,long *pDelta);
	STDMETHODIMP MoveUntil(VARIANT *Cset,long Count,long *pDelta);
	STDMETHODIMP MoveWhile(VARIANT *Cset,long Count,long *pDelta);
	STDMETHODIMP Paste(VARIANT *pVar,long Format);
	STDMETHODIMP ScrollIntoView(long bStart);
	STDMETHODIMP Select(VOID);
	STDMETHODIMP SetChar(long Char);
	STDMETHODIMP SetEnd(long cp);
	STDMETHODIMP SetFont(ITextFont *pFont);
	STDMETHODIMP SetFormattedText(ITextRange *pRange);
	STDMETHODIMP SetIndex(long Unit,long Index,long Extend);
	STDMETHODIMP SetPara(ITextPara *pPara);
	STDMETHODIMP SetPoint(long x,long y,long Type,long Extend);
	STDMETHODIMP SetRange(long cp1,long cp2);
	STDMETHODIMP SetStart(long cp);
	STDMETHODIMP SetText(BSTR bstr);
	STDMETHODIMP StartOf(long Unit,long Extend,long *pDelta);
	STDMETHODIMP GetTypeInfoCount(UINT *pctinfo);
	STDMETHODIMP GetTypeInfo(UINT,LCID,ITypeInfo ** );
	STDMETHODIMP GetIDsOfNames(const IID &,LPOLESTR *, UINT,LCID,DISPID*);
	STDMETHODIMP Invoke(DISPID,const IID &,LCID,WORD,DISPPARAMS *,VARIANT *,EXCEPINFO *,UINT *);
	STDMETHODIMP QueryInterface(REFIID riid,void** ppvObject);
	ULONG STDMETHODCALLTYPE AddRef( void);
	ULONG STDMETHODCALLTYPE Release( void);


private:
	ITextFont *font;
	BSTR text;
	bool editable;
	long start;
	long end;
};
