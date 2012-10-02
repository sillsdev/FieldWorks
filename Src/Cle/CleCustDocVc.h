/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: CleCustDocVc.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a customizeable view constructor for document views.
	A standard document view has the following:
		1. A heading, consisting of a property of the main object, plus a literal type field
			These are arranged as a table, heading left, type right justified.
		2. A sequence of blocks
			- Structured text blocks contain an optional literal heading paragraph,
				then a structured text
			- Composite blocks contain a single paragraph, a sequence of literal/field pairs.
				- Fields may be an atomic string, an atomic reference, or a sequence of
					references. Appropriate list punctuation is added to sequences.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef RN_CUST_DOC_VC_INCLUDED
#define RN_CUST_DOC_VC_INCLUDED 1

/*----------------------------------------------------------------------------------------------
	A view constructor that displays a single item in a reference view. It may also be used
	to provide views in list choosers, etc. This defines a generic view of any object.
	It should be subclassed to provide more meaningful views of most objects.

	Depending on fLoadData, the view constructor needs to load anything it uses, since the
	desired information may not be in the cache.

	@h3{Hungarian: rrvc}
----------------------------------------------------------------------------------------------*/
class CleRecVc : public ObjVc
{
public:
	typedef ObjVc SuperClass;

	CleRecVc(bool fLoadData = true);
	virtual ~CleRecVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(GetStrForGuid)(BSTR bstrGuid, ITsString ** pptss);
	STDMETHOD(DoHotLinkAction)(BSTR bstrData, HVO hvoOwner, PropTag tag, ITsString * ptss,
		int ichObj);
};
typedef GenSmartPtr<CleRecVc> CleRecVcPtr;


/*----------------------------------------------------------------------------------------------
	The main customizeable document view constructor class.
	Hungarian: ccdvc.
----------------------------------------------------------------------------------------------*/
class CleCustDocVc : public VwCustDocVc
{
public:
	typedef VwCustDocVc SuperClass;

	CleCustDocVc(UserViewSpec * puvs, AfLpInfo * plpi, CleMainWnd * pcmw);
	~CleCustDocVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(DisplayVec)(IVwEnv * pvwenv, HVO hvo, int tag, int frag);
	STDMETHOD(DisplayVariant)(IVwEnv * pvwenv, int tag, VARIANT v, int frag, ITsString ** pptss);

protected:
	virtual int GetSubItemFlid()
	{
		return kflidCmPossibilityList_Possibilities;
	}
	virtual void SetSubItems(IVwEnv * pvwenv, int flid);
	virtual bool GetLoadRecursively()
	{ return false; }

	ITsTextPropsPtr m_qttpMainFirst;
	ITsTextPropsPtr m_qttpMainLast;
	ITsTextPropsPtr m_qttpMainFlat;
	ITsTextPropsPtr m_qttpSubLast;
	HVO m_hvoPssl; // Id of the poss list we are showing.
	CleMainWndPtr m_qcmw;
};

#endif // RN_CUST_DOC_VC_INCLUDED
