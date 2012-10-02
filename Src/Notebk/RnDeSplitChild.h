/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright 2002, SIL International. All rights reserved.

File: RnDeSplitChild.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	This provides the data entry Research Notebook functions.
	RnDeSplitChild does most of the interesting. There is also a view constructor.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RN_DE_SPLIT_CHILD_WND_INCLUDED
#define RN_DE_SPLIT_CHILD_WND_INCLUDED 1

class RnDeSplitChild;
typedef GenSmartPtr<RnDeSplitChild> RnDeSplitChildPtr;

/*----------------------------------------------------------------------------------------------
	This class specializes a basic data entry window to handle RnGenericRecs.

	@h3{Hungarian: rnadsc}
----------------------------------------------------------------------------------------------*/
class RnDeSplitChild : public AfDeRecSplitChild
{
public:
	typedef AfDeRecSplitChild SuperClass;

	RnDeSplitChild();
	~RnDeSplitChild();

	virtual void UpdateField(HVO hvoOwn, int flid, HVO hvoNode);
	virtual void SetTreeHeader(AfDeFeNode * pden);
	virtual void JumpItem(HVO hvo);
	virtual void LaunchItem(HVO hvo);
	virtual void ToggleExpansion(int idfe);
	virtual void BeginEdit(AfDeFieldEditor * pdfe);
	virtual bool RnDeSplitChild::CmdDelete(Cmd * pcmd);

	bool CmdFindInDictionary(Cmd * pcmd);
	bool CmsFindInDictionary(CmdState & cms);
	virtual void AddExtraContextMenuItems(HMENU hmenuPopup);

protected:
	virtual bool CmdEditRoles(Cmd * pcmd);
	virtual void AddField(HVO hvoRoot, int clid, int nLev, FldSpec * pfsp,
		CustViewDa * pcvd, int & idfe, int nInd = 0, bool fAlwaysVisible = false);
	virtual bool CmdInsertSubentry(Cmd * pcmd);
	virtual bool CmsInsertSubentry(CmdState & cms);
	virtual int GetDstFlidAndLabel(HVO hvoDstOwner, int clid, ITsString ** pptss);
	virtual void AddContextInsertItems(HMENU & hmenu);
	virtual void AddContextShowItems(HMENU & hmenu);
	virtual RecordSpec * GetRecordSpec(int idfe, FldSpec ** ppfsp = NULL);
	virtual bool CmdExpContextMenu(Cmd * pcmd);
	virtual bool IsSubitemFlid(int flid)
	{
		return flid == kflidRnGenericRec_SubRecords;
	}
	virtual int GetOwnerModifiedFlid(int clid);
	virtual void PromoteSetup(HVO hvo);

	// member variables

	// Stores current participants labels when processing insert participants context menu.
	Vector<StrUni> m_vstuParticipants;
	// Stores the Role hvoPss for each item showing in the popup participants menu.
	// We could just get the name from the menu item, but we have to go to an ANSI string
	// for the menu item. This approach allows us to use a Unicode string for the label.
	Vector<HVO> m_hvoMenuRoles;
	CMD_MAP_DEC(RnDeSplitChild);
};


/*----------------------------------------------------------------------------------------------
	A view constructor that displays a single item in a reference view. It may also be used
	to provide views in list choosers, etc. This defines a generic view of any object.
	It should be subclassed to provide more meaningful views of most objects.

	Depending on fLoadData, the view constructor needs to load anything it uses, since the
	desired information may not be in the cache.

	@h3{Hungarian: rrvc}
----------------------------------------------------------------------------------------------*/
class RnRecVc : public ObjVc
{
public:
	typedef ObjVc SuperClass;

	RnRecVc(bool fLoadData = true);
	virtual ~RnRecVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(GetStrForGuid)(BSTR bstrGuid, ITsString ** pptss);
	STDMETHOD(DoHotLinkAction)(BSTR bstrData, HVO hvoOwner, PropTag tag, ITsString * ptss,
		int ichObj);
};
typedef GenSmartPtr<RnRecVc> RnRecVcPtr;


#endif // RN_DE_SPLIT_CHILD_WND_INCLUDED
