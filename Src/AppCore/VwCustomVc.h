/*----------------------------------------------------------------------------------------------
Copyright 2000, 2002, SIL International. All rights reserved.

File: VwCustomVc.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file contains class declarations for the following classes:
		VwCustomVc
			VwCustBrowseVc : VwCustomVc
			VwCustDocVc : VwCustomVc
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef AF_CUSTOM_VC_INCLUDED
#define AF_CUSTOM_VC_INCLUDED 1

typedef Vector<int> IntVec;
typedef Set<HVO> HvoSet;

/*----------------------------------------------------------------------------------------------
	The main customizeable view constructor base class.
	Hungarian: vcvc.
----------------------------------------------------------------------------------------------*/
class VwCustomVc : public VwBaseVc
{
public:
	typedef VwBaseVc SuperClass;

	VwCustomVc(UserViewSpec * puvs, AfLpInfo * plpi,
		int tagTitle, int tagSubItems, int tagRootItems = kflidStartDummyFlids);
	~VwCustomVc();

	void SetDa(CustViewDa * pcda, AfStatusBar * pstbr, UserViewSpec * puvs = NULL);
	virtual bool FullRefresh();
	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(DisplayVec)(IVwEnv * pvwenv, HVO hvo, int tag, int frag);
	STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
		int tag, int frag, int ihvoMin);
	STDMETHOD(GetStrForGuid)(BSTR bstrGuid, ITsString ** pptss);
	STDMETHOD(DoHotLinkAction)(BSTR bstrData, HVO hvoOwner, PropTag tag, ITsString * ptss,
		int ichObj);
	virtual void SetPrimarySortKeyFlid(int flid) {}

protected:
	virtual void BodyOfRecord(IVwEnv * pvwenv, HVO hvo, ITsTextProps * pttpDiv, int nLevel = 0);
	virtual bool GetLoadRecursively()
	{ return true; }
	void FixEmptyStrings(HVO * prghvo, int chvo, PropTag tagStrProp, int wsExpected);
	AfLpInfoPtr m_qlpi;
	int m_wsUser;		// user interface writing system id.
	CustViewDaPtr m_qcda; // DA into which to load data when LoadDataFor is called.
	AfStatusBarPtr m_qstbr; // To report progress in loading data.
	UserViewSpecPtr m_quvs; // used for loading data.
	int m_tagRootItems; // prop of root object that contains doc/browse contents.
	int m_tagTitle; // prop of root object that contains title string.
	int m_tagSubItems; // prop of main item that contains more items.
	int m_flidTemp;	// A temporary holding space for a flid.
	HvoSet m_shvoLoaded; // Set of root objects already loaded.
	IVwViewConstructorPtr m_qRecVc;
	ITsStringPtr m_qtssMissing; // String to insert for empty vector.
	ITsStringPtr m_qtssListSep; // comma-space between items in list
	ITsTextPropsPtr m_qttpMain; // TsTextProps for main entries (creates underline between)
	ITsTextPropsPtr m_qttpSub; // for subentries (indents them)
};

/*----------------------------------------------------------------------------------------------
This class implements VwCustDocVc
@h3{Hungarian: vcdvc}
----------------------------------------------------------------------------------------------*/
class VwCustDocVc : public VwCustomVc
{
public:
	typedef VwCustomVc SuperClass;

	VwCustDocVc(UserViewSpec * puvs, AfLpInfo * plpi,
		int tagTitle, int tagSubItems, int tagRootItems = kflidStartDummyFlids);
	virtual ~VwCustDocVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(DisplayVariant)(IVwEnv * pvwenv, int tag, VARIANT v, int frag,
		ITsString ** pptss);
	STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight);

protected:
	virtual bool DisplayFields(FldSpecPtr * prgqfsp, int cfsp, IVwEnv * pvwenv, HVO hvo,
		bool fOnePerLine, HvoVec & hvoNoData, IntVec & vtagNoData, bool fTest = false);
	virtual void FirstFldCheckPre(IVwEnv * pvwenv, int cVisFld, ITsTextProps * pttpDiv);
	virtual void FirstFldCheckPost(IVwEnv * pvwenv, int & cVisFld, ITsTextProps * pttpDiv);
	virtual void NoteFldNoData(HVO hvo, PropTag tag, HvoVec & vhvoNoData, IntVec & vtagNoData)
	{
		vtagNoData.Push(tag);
		vhvoNoData.Push(hvo);
	}
	virtual void BodyOfRecord(IVwEnv * pvwenv, HVO hvo, ITsTextProps * pttpDiv, int nLevel = 0);
	virtual int GetSubItemFlid()
	{
		Assert(false);	// must be overridden.
		return 0;
	}
	virtual ITsStringPtr GetTypeForClsid(int clsid)
	{
		return NULL;
	}
	virtual void SetSubItems(IVwEnv * pvwenv, int flid)
	{
		Assert(false);	// Subclasses must override this.
	}

	FldSpecPtr m_qfsp;
	ITsStringPtr m_qtssFldSep; // dot-space between fields
	ITsStringPtr m_qtssBlockEnd; // dot at end of block
	ITsStringPtr m_qtssColon; // colon-space between label and value
	OutlineNumSty m_ons; // The last subrecord numbering option.
};

/*----------------------------------------------------------------------------------------------
	The main customizeable browse view constructor class.
	Hungarian: vcbvc.
----------------------------------------------------------------------------------------------*/
class VwCustBrowseVc : public VwCustomVc
{
public:
	typedef VwCustomVc SuperClass;

	VwCustBrowseVc(UserViewSpec * puvs, AfLpInfo * plpi,
		int dypHeader, int nMaxLines, HVO hvoRootObjId,
		int tagTitle, int tagSubItems, int tagRootItems = kflidStartDummyFlids);
	~VwCustBrowseVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight);

	void SetSortKeyInfo(Vector<SortKeyHvos> * pvskhSortKeys)
	{
		m_pvskhSortKeys = pvskhSortKeys;
	}

	void SetPrimarySortKeyFlid(int flid)
	{
		m_flidPrimarySort = flid;
	}

protected:
	void CreateBitmap(HIMAGELIST himl, int iimage, COLORREF clrBkg, IPicture ** ppict);

	int m_nMaxLines; // max lines per entry
	StVcPtr m_qstvc; // Structured Text view constructor
	int m_dypHeader;
	bool m_fIgnoreHier; // If ture then show subrecords as well as top level records.
	HVO m_hvoRootObjId;	// HVO for the root object
	RecordSpecPtr m_qrsp; // RecordSpec for Browse view combining several subclasses.
	// This is a pointer to a vector giving information about sort keys. Currently it is used
	// when RecMainWnd sets up a browse view. It allows us to identify the sort key in
	// displaying a record.
	Vector<SortKeyHvos> * m_pvskhSortKeys;
	int m_flidPrimarySort; // The field that contains the primary sort key (if any).
};

#endif // AF_CUSTOM_VC_INCLUDED
