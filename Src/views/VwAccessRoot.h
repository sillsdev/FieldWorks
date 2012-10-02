/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwAccessRoot.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwAccessRoot_INCLUDED
#define VwAccessRoot_INCLUDED

typedef ComHashMap<VwBox *, IAccessible> BoxAccessorMap; // Hungarian hmboxacc;

/*----------------------------------------------------------------------------------------------
Class: VwAccessRoot
Description: This class implements IAccessible to provide basic information about roots,
and also IServiceProvider which can be used to get a pointer to the IVwRootBox for more
detailed information.
Hungarian: acc
----------------------------------------------------------------------------------------------*/
class VwAccessRoot :  public IAccessible, public IServiceProvider, public IEnumVARIANT
{
public:
	// Static methods

	// Constructors/destructors/etc.
	VwAccessRoot(VwBox * pbox);
	VwAccessRoot(VwAccessRoot * pacc);
	virtual ~VwAccessRoot();

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

	// IAccessibility methods

	STDMETHOD(accNavigate)(long navDir,	VARIANT varStart, VARIANT * pvarEnd);
	STDMETHOD(get_accChild)(VARIANT varChildID, IDispatch ** ppdispChild);
	STDMETHOD( get_accChildCount)(long * pcountChildren);
	STDMETHOD( get_accParent)(IDispatch ** ppdispParent);
	STDMETHOD( accDoDefaultAction)(VARIANT varID);
	STDMETHOD( get_accDefaultAction)(VARIANT varID,	BSTR * pszDefaultAction);
	STDMETHOD( get_accDescription)(VARIANT varID, BSTR * pszDescription);
	STDMETHOD( get_accHelp)(VARIANT varID, BSTR * pszHelp);
	STDMETHOD( get_accHelpTopic)(BSTR * pszHelpFile, VARIANT varChild, long * pidTopic);
	STDMETHOD( get_accKeyboardShortcut)(VARIANT varID, BSTR * pszKeyboardShortcut);
	STDMETHOD( get_accName)(VARIANT varID, BSTR * pszName);
	STDMETHOD( get_accRole)(VARIANT varID, VARIANT * pvarRole);
	STDMETHOD( get_accState)(VARIANT varID, VARIANT * pvarState);
	STDMETHOD( get_accValue)(VARIANT varID, BSTR * pszValue);
	STDMETHOD( accSelect)(long flagsSelect, VARIANT varID);
	STDMETHOD( get_accFocus)(VARIANT * pvarID);
	STDMETHOD( get_accSelection)(VARIANT * pvarChildren);
	STDMETHOD( accLocation)(long * pxLeft, long * pyTop, long * pcxWidth, long * pcyHeight,
		VARIANT varID);
	STDMETHOD( accHitTest)(long xLeft, long yTop, VARIANT * pvarID);
	// The compiler insists on these but they in the doc for IAccessible. Leave unimplemented.
	STDMETHOD(put_accName)(VARIANT,BSTR);
	STDMETHOD(put_accValue)(VARIANT,BSTR);

	// IServiceProvider methods
	STDMETHOD(QueryService)(REFGUID guidService, REFIID riid, void ** ppv);

	// IDispatch dummy methods
	STDMETHOD(GetTypeInfoCount)(UINT *);
	STDMETHOD(GetTypeInfo)(UINT,LCID,ITypeInfo ** );
	STDMETHOD(GetIDsOfNames)(const IID &,LPOLESTR * ,UINT,LCID,DISPID *);
	STDMETHOD(Invoke)(DISPID,const IID &,LCID,WORD,DISPPARAMS *,VARIANT *,EXCEPINFO *,UINT *);

	// IEnumVariant methods
	STDMETHOD(Next)(unsigned long celt, VARIANT FAR* rgvar, unsigned long FAR* pceltFetched);
	STDMETHOD(Skip)(unsigned long celt);
	STDMETHOD(Reset)();
	STDMETHOD(Clone)(IEnumVARIANT FAR* FAR* ppenum);

	static void BoxDeleted(VwBox * pbox);
	static void GetAccessFor(VwBox * pbox, IDispatch ** ppdisp);

protected:
	long m_cref;
	VwBox * m_pbox;  // The box we are accessing

	// Maintains a current position for when the object is used as an enumeration.
	// Zero-based index into children.
	int m_iaccCurrent;

};
DEFINE_COM_PTR(VwAccessRoot);


#endif  //VwAccessRoot_INCLUDED
