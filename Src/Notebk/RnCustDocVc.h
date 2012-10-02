/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: RnCustDocVc.h
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
	The main customizeable document view constructor class.
	Hungarian: cdvc.
----------------------------------------------------------------------------------------------*/
class RnCustDocVc : public VwCustDocVc
{
public:
	typedef VwCustDocVc SuperClass;

	RnCustDocVc(UserViewSpec * puvs, AfLpInfo * plpi);
	~RnCustDocVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(DisplayVec)(IVwEnv * pvwenv, HVO hvo, int tag, int frag);
	STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
		int tag, int frag, int ihvoMin);

protected:
	virtual int GetSubItemFlid()
	{
		return kflidRnGenericRec_SubRecords;
	}

	virtual ITsStringPtr GetTypeForClsid(int clsid)
	{
		return (clsid == kclidRnEvent) ? m_qtssEvent : m_qtssAnalysis;
	}
	virtual void SetSubItems(IVwEnv * pvwenv, int flid);

	ITsStringPtr m_qtssPartEnd; // String at end of participants within a RnRoledPartic.
	ITsStringPtr m_qtssEvent; // String "Event" for event records
	ITsStringPtr m_qtssAnalysis; // String "Analysis" for analysis records
};



#endif // RN_CUST_DOC_VC_INCLUDED
