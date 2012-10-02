/*----------------------------------------------------------------------------------------------
Copyright 2000, 2002, SIL International. All rights reserved.

File: RnCustBrowseVc.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a customizeable view constructor for browse views.
	A browse view is a table, with one column for each field of interest.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef RN_CUST_BROWSE_VC_INCLUDED
#define RN_CUST_BROWSE_VC_INCLUDED 1

enum RnEntryTypes
{
	kentypEvent = 0,
	kentypAnal,
	kentypSubEvent,
	kentypSubAnal,
	kentypLim
};

/*----------------------------------------------------------------------------------------------
	The main customizeable browse view constructor class.
	Hungarian: rcbvc.
----------------------------------------------------------------------------------------------*/
class RnCustBrowseVc : public VwCustBrowseVc
{
public:
	typedef VwCustBrowseVc SuperClass;

	RnCustBrowseVc(UserViewSpec * puvs, AfLpInfo * plpi, int dypHeader, int nMaxLines,
		HVO hvoRootObjId);
	~RnCustBrowseVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(DisplayVec)(IVwEnv * pvwenv, HVO hvo, int tag, int frag);
	STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
		int tag, int frag, int ihvoMin);

protected:
	virtual void BodyOfRecord(IVwEnv * pvwenv, HVO hvo, ITsTextProps * pttpDiv, int nLevel = 0);
	BlockSpec * GetFldSpecOutOne(IVwEnv * pvwenv);

	IPicturePtr m_qpicRnEntryType[kentypLim];
};

#endif // RN_CUST_BROWSE_VC_INCLUDED
