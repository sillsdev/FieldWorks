/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwOleDbDa.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file contains class declarations for the following class:
		VwOleDbDa
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwOleDbDa_INCLUDED
#define VwOleDbDa_INCLUDED

// Constants duplicated from TlsOptViewRes.h
#define kstidDocument                   28470
#define kstidBrowse                     28471
#define kstidDataEntry                  28472
#define kstidConcordance				23904
#define kstidDraft						23905

// Types collected from various places in AppCore so FldSpec and UserViewSpec can be used
// independent of AppCore.
typedef enum
{
	// Code in PossChsrDlg::LoadDlgSettings assumes these
	kpntName = 0,
	kpntNameAndAbbrev = 1,
	kpntAbbreviation = 2,
	kpntLim,
} PossNameType;

struct HvoClsid // Hungarian: hc.
{
	HVO hvo;
	int clsid;
};
typedef Vector<HvoClsid> HvoClsidVec; // Hungarian vhc.

// This lists the possible types of user views. Applications may use subsets of these.
// If an application needs a new type of view, it should be added to this list, as well
// as a definition for the string in the resources (e.g., kstidBrowse).
typedef enum UserViewType
{
	kvwtBrowse = 0,	// Browse View
	kvwtDE,			// Data Entry View
	kvwtDoc,		// Document View
	kvwtConc,		// Concordance View
	kvwtDraft,		// Scripture Draft View

	kvwtLim			// Count of VwTypes
};

enum FldVis
{
	kFTVisAlways = 0,	// Field is Always visible.
	kFTVisIfData,		// Field is visible if it has data.
	kFTVisNever,		// Field is never visible.
	kFTVisLim			// Count of FldVis.
};

typedef enum FldReq
{
	kFTReqNotReq = 0,	//Field is Not Required
	kFTReqWs,		//Field is Encouraged
	kFTReqReq,		//Field is Required
	kFTReqLim		//Count of FldReq
};

typedef enum OutlineNumSty
{
	konsNone = 0, // No numbering.
	konsNum = 1, // 1, 1.1, 1.1.1
	konsNumDot = 2, // 1., 1.1., 1.1.1.
	konsLim,
} OutlineNumSty;


/*----------------------------------------------------------------------------------------------
	Specifies one field in a bsp.
	Hungarian: fsp.
----------------------------------------------------------------------------------------------*/
class FldSpec : public GenRefObj
{
public:
	FldSpec()
	{
	}

	enum
	{
		kdxpDefBrowseColumn = 100,
	};

	FldSpec(ITsString * ptss, ITsString * ptssHelp, int flid, FldType ft, FldVis vis,
		FldReq req, LPCOLESTR pszSty, int ws, bool fCustFld, HVO hvoPssl)
	{
		AssertPtr(ptss);
		AssertPtrN(ptssHelp);

		m_qtssLabel = ptss;
		m_qtssHelp = ptssHelp;
		m_flid = flid;
		m_ft = ft;
		m_eVisibility = vis; // Field visibility.
		m_fRequired = req;	// field Required or not
		m_stuSty = pszSty;
		m_dxpColumn = kdxpDefBrowseColumn;
		m_ws = ws;
		m_fCustFld = fCustFld;
		m_hvoPssl = hvoPssl;
	}

	void Init(bool fIsDocView, int stidLabel, int stidHelp, FldType ft, FldVis vis, FldReq req,
		LPCOLESTR pszSty, int flid, int ws, bool fCustFld, bool fHideLabel,
		ILgWritingSystemFactory * pwsf);
	void InitPssl(HVO hvoPssl, PossNameType pnt, bool fHier, bool fVert);
	void InitHier(OutlineNumSty ons, bool fExpand);

	bool NewCopy(FldSpec ** ppfsp);
	void SetHideLabel(bool f)
	{
		m_fHideLabel = f;
	}
	HVO Save(IOleDbEncap * pode, HVO hvoOwn, int ws, HVO hvoBsp, bool fForceNewObj = false);
	void SaveDetails(ISilDataAccess * pda);

	ITsStringPtr m_qtssLabel; // Tree label.
	ITsStringPtr m_qtssHelp; // Help string that specifies what the field contains.
	FldType m_ft; // Field type.
	FldVis m_eVisibility; // Field visibility.
	FldReq m_fRequired;	// field Required or not
	StrUni m_stuSty; // The default style for this field.
	int m_flid; // Field id in the database.
	int	m_ws; // Writing system
	bool m_fCustFld; // True if this is a custom field.

	// The following information is stored in the details field in the database.
	// Different field types interpret the data in different ways.

		// Information used for possibility list items.
	HVO	m_hvoPssl; // The possibility list we are using for references.
	PossNameType m_pnt; // Name format (kpntName/kpntNameAndAbbrev/kpntAbbreviation).
	bool m_fHier; // Show names using hierarchy (e.g., noun:common).
	bool m_fVert; // List multiple items vertically instead of in a paragraph.

		// Information used for hierarchical fields (e.g., subrecords).
	bool m_fExpand; // Always expand tree nodes.
	OutlineNumSty m_ons; // Way to show outline numbers (konsNone/konsNum/konsNumDot).

		// Information for document view.
	bool m_fHideLabel; // true to hide label in Document view.

	// Data members that are never set, while creating a RecordSpec.
	// Information used for browse view.
	int m_dxpColumn;

	// Information not stored in the database.
	StrUni m_stuFldName; // Name of field in database (computed).
	StrUni m_stuClsName; // Name of class the field belongs to (computed).
	HVO m_hvo; // The database id for this object when it was read.
	bool m_fNewFld; // Flag used internally by custom field dialog.  This flag is never saved.

	// Used for getting writing system codes and such.
	ILgWritingSystemFactoryPtr	m_qwsf;
};

typedef GenSmartPtr<FldSpec> FldSpecPtr;
typedef Vector<FldSpecPtr> FldVec; // Hungarian vfsp.

/*----------------------------------------------------------------------------------------------
	Specifies one "block" in a customizable view.
	A block may be
		- a structured text: if tssLabel is not null, it occupies a paragraph before the text.
		- a single field: tssLabel appears in front of the field.
		- a list of fields: the block label, if any, occupies a previous paragraph,
			otherwise the individual field labels are used.
	BlockSpecs are stored as UserViewFields in the database. The difference between a BlockSpec
	and a FldSpec is determined by the presense of SubFieldOf for FldSpecs. This property
	points to the owning BlockSpec.
	Hungarian: bsp.
----------------------------------------------------------------------------------------------*/
class BlockSpec : public FldSpec
{
public:
	BlockSpec()
	{
	}
	BlockSpec(ITsString * ptss, ITsString * ptssHelp, int tag, FldType ft, FldVis vis,
		FldReq req, LPCOLESTR pszSty, int ws, bool fCustFld, HVO listid)
		:FldSpec(ptss, ptssHelp, tag, ft, vis, req, pszSty, ws, fCustFld, listid)
	{
	}

	bool NewCopy(BlockSpec ** ppbsp);

	FldVec m_vqfsp;
};

// Key used in an array used to track properties recently loaded for all of class.
struct AutoloadKey
{
	PropTag tag; // the property we recently loaded
	int ws; // the writing system we recently loaded (zero for non-ML-strings)
	int clsid; // the class for which we loaded this property (0 if all)
};

typedef GenSmartPtr<BlockSpec> BlockSpecPtr;
typedef Vector<BlockSpecPtr> BlockVec; // Hungarian vbsp.

class UserViewSpec; // forward.

/*----------------------------------------------------------------------------------------------
	Specifies one record in a customizable view. We can specify different views for classes
	depending on whether they are major records or nested within another record.
	Hungarian: esp.
----------------------------------------------------------------------------------------------*/
class RecordSpec : public GenRefObj
{
public:
	RecordSpec()
	{
	}

	RecordSpec(int clsid, int nLevel)
	{
		m_clsid = clsid;
		m_nLevel = nLevel;
	}

	void Init(UserViewSpec * puvs, int clid, int iLevel, UserViewType vwt,
		ILgWritingSystemFactory * pwsf);

	FldSpec * AddField(bool fTopLevel, int stidLabel, int flid, FldType ft, int ws = kwsAnal,
		int stidHelp = 0, FldVis vis = kFTVisAlways, FldReq req = kFTReqNotReq,
		LPCOLESTR pszSty = L"", bool fCustFld = false, bool fHideLabel = false);

	void AddPossField(bool fTopLevel, int stidLabel, int flid, FldType ft, int stidHelp,
		HVO hvoPssl, PossNameType pnt = kpntName, bool fHier = false, bool fVert = false,
		int ws = kwsAnal, FldVis vis = kFTVisAlways, FldReq req = kFTReqNotReq,
		LPCOLESTR pszSty = L"", bool fCustFld = false, bool fHideLabel = false);

	void AddHierField(bool fTopLevel, int stidLabel, int flid, int ws = kwsAnal,
		int stidHelp = 0, OutlineNumSty ons = konsNone, bool fExpand = false,
		FldVis vis = kFTVisAlways, FldReq req = kFTReqNotReq, LPCOLESTR pszSty = L"",
		bool fCustFld = false, bool fHideLabel = false);

	void AddCollectionField(bool fTopLevel, int stidLabel, int flid, int ws = kwsAnal,
		int stidHelp = 0, OutlineNumSty ons = konsNone, bool fExpand = false,
		FldVis vis = kFTVisAlways, FldReq req = kFTReqNotReq, LPCOLESTR pszSty = L"",
		bool fCustFld = false, bool fHideLabel = false);

	void AddSequenceField(bool fTopLevel, int stidLabel, int flid, int ws = kwsAnal,
		int stidHelp = 0, OutlineNumSty ons = konsNone, bool fExpand = false,
		FldVis vis = kFTVisAlways, FldReq req = kFTReqNotReq, LPCOLESTR pszSty = L"",
		bool fCustFld = false, bool fHideLabel = false);

	bool NewCopy(RecordSpec ** pprsp);

	void SetMetaNames(IFwMetaDataCache * pmdc);

	// A vector of block spec pointers that specify the fields to display for this entry.
	Vector<BlockSpecPtr> m_vqbsp;
	// The class identification for this entry (e.g., kclidRnEvent, kclidRnAnalysis, etc.).
	int m_clsid;
	// The level of display. 0 = main record in window. 1 = subrecord. We currently only
	// support two levels, but at some point in the future, we might need more, so we are
	// using int instead of bool.
	int m_nLevel;
	HVO m_hvo; // The database id for this object.

	// The database has a details field that can store additional information, but we have
	// nothing to store at this point. If this is ever used for more than one purpose, it
	// will add complexity to doing generic load and save.

	// Not stored in the database.
	// The browse (and perhaps other) UserViewSpec has one RecordSpec for views and other
	// RecordSpecs for loading data. This is necessary because loads require valid fields
	// for each class, while the browse view shows columns for multiple classes--some columns
	// are not valid for a given class and are simply ignored by the views code. In this case,
	// the RecordSpec for views is stored in the database, but the RecordSpecs for loading
	// data are created when the UserViewSpec is initialized. These RecordSpecs are not saved,
	// thus have this flag set.
	bool m_fNoSave;
	// Flag set during TlsOptDlg modifications to indicate it needs to be changed along with
	// the fields. This is not set or used when new RecordSpecs are created, and it is not
	// copied when copies are made.
	bool m_fDirty;
	// View type that helps sort out BlockSpec.
	UserViewType m_vwt;

	// Used for getting writing system codes and such.
	ILgWritingSystemFactoryPtr	m_qwsf;
};

typedef GenSmartPtr<RecordSpec> RecordSpecPtr;


/*----------------------------------------------------------------------------------------------
	Used in a HashMap to find an entry for a given class and level (major/subentry)
	Hungarian: clev.
----------------------------------------------------------------------------------------------*/
class ClsLevel
{
public:
	int m_clsid;
	int m_nLevel;

	ClsLevel() // needs  default constructor to be a key
	{
	}

	ClsLevel(int clsid, int nLevel)
	{
		m_clsid = clsid;
		m_nLevel = nLevel;
	}
};

typedef GpHashMap<ClsLevel, RecordSpec> ClevRspMap; // Hungarian hmclevrsp.

/*----------------------------------------------------------------------------------------------
	An item in the top-level list of user-customizeable views.
	Hungarian: uvs.
----------------------------------------------------------------------------------------------*/
class UserViewSpec : public GenRefObj
{
public:
	UserViewSpec()
	{
	}
	UserViewSpec(const UserViewSpec & uvs);
	bool NewCopy(UserViewSpec ** ppuvs);
	bool Save(IOleDbEncap * pode, bool fForceNewObj = false);

	// A map of RecordSpecs that define the blocks to show for each type of record.
	ClevRspMap m_hmclevrsp;

	// Type of view (kvwtBrowse/kvwtDE/kvwtDoc, etc.).
	// This is defined by the application, so it can be expanded to include type of
	// CmPossibility for the chooser list, etc.
	UserViewType m_vwt;
	ITsStringPtr m_qtssName; // Name of the user view.
	int m_hvo; // This is the object ID stored in the database.
	GUID m_guid; // Unique id used to identify the application using this user view.
	bool m_fv; // FactoryView if true, AddedView if false.
	// SubType. In list editor this is used to hold the clsid of the items in a list.
	// In other applications this can be used for anything else that is needed.
	int m_nst;

	// Fields stored in binary details field in database.
	// This is a bit of a problem since we want to be able to save and load in AppCore
	// without knowing the view type, which is defined at the application level. For the
	// moment we can handle this since there is only one type of field that is currently
	// storing info in details. Once we go beyond this, we will need to pass details back
	// to the application to parse.
	int m_nMaxLines; // max line per record in Browse view

	// Variables not stored in the database.
	int m_ws; // Alternative writing system for MultiUnicode fields anywhere in ownership tree.
	int m_iwndClient; // Index of the child window in the MDI client corresponding to the view.
	// NOTE: Do NOT use m_idWnd. It needs to be deleted once the CLE stuff is
	// fixed to use m_iwndClient instead.
	int m_idWnd; // Id of the window
	int m_fIgnorHier; // Ignore Hierarchy. Used in Browse view.

protected:
	// Disallow use of assignment for now.
	UserViewSpec & operator = (const UserViewSpec & usrvw)
	{
	}
	// Member variables.
};

typedef GenSmartPtr<UserViewSpec> UserViewSpecPtr;
typedef Vector<UserViewSpecPtr> UserViewSpecVec; // Hungarian vuvs

//:>  Hungarian: hmostamp
typedef HashMap<HVO, StrAnsi> ObjPropTimeStampMap;

/*----------------------------------------------------------------------------------------------
	VwOleDbDa subclasses VwCacheDa and provides mechanisms for loading the data from the
	database to the cache using SQL "select" statements.  As well, it provides a means for
	storing data back to the database.  When a client inserts, deletes, or updates an object
	(by using the Set*, Delete*, MakeNewObject, Replace*, MoveOwnSeq, and other methods), it
	affects the database immediately.  No buffering occurs.

	This class also provides a mechanism for clients to be notified when a given object
	property changes.

	Cross-Reference: ${IVwOleDbDa}

	@h3{Hungarian: odde}
----------------------------------------------------------------------------------------------*/
class VwOleDbDa : public VwCacheDa, public IVwOleDbDa, public ISetupVwOleDbDa
{
	typedef VwCacheDa SuperClass;
//public:
protected:
	VwOleDbDa();
	~VwOleDbDa();

	IOleDbEncapPtr m_qode;
	IActionHandlerPtr m_qacth;
	// (Binary) time stamps
	ObjPropTimeStampMap m_hmostamp;
	// This variable stores the next negative dummy ID.
	HVO m_hvoNextDummy;
	int m_wsUser;		// User interface writing system id for this database.
	int m_nUndoLevel; // Number of BeginUndoActions called without matching EndUndoActions.

	Vector<int> m_vwsAnal; // Current analysis writing system.
	Vector<int> m_vwsVern; // Current vernacular writing systems.
	Vector<int> m_vwsVernAnal; // Current vernacular then analysis writing systems.
	Vector<int> m_vwsAnalVern; // Current analysis then vernacular writing systems.
	bool m_fLoadedWsInfo; // true if LoadWritingSystems has been called.
	// What to do internally when a property is missing.
	// (kalpLoadAllOfClassIncludingAllVirtuals becomes kalpLoadForAllOfObjectClass)
	AutoloadPolicies m_alpAutoloadPolicy;
	// Original policy visible externally, including kalpLoadAllOfClassIncludingAllVirtuals.
	AutoloadPolicies m_alpFullAutoloadPolicy;
	static const int kcRecentAutoloads = 20;
	AutoloadKey m_rgalkRecentAutoLoads[kcRecentAutoloads]; // Array used as circular buffer
	int m_ialkNext; // next slot in recent autoloads to fill.
	Set<AutoloadKey> m_salkLoadedProps;
	HvoSet m_shvoDeleted;		// Set of db object ids removed by ClearInfoAbout

	// This function overrides default base class implementation which doesn't know about
	// owning and non-owning fields.
	virtual bool IsOwningField(PropTag tag);

public:
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	//:>****************************************************************************************
	//:>    ISetupVwOleDbDa Methods
	//:>****************************************************************************************
	STDMETHOD(Init)(IUnknown * pode /* IOleDbEncap*/, IUnknown * pmdc /* IFwMetaDataCache */,
		IUnknown * pwsf /* ILgWritingSystemFactory */, IActionHandler * pacth);
	// Provide access to the internal IOleDbEncap interface pointer.
	STDMETHOD(GetOleDbEncap)(IUnknown ** ppode);

	//:>****************************************************************************************
	//:>    IUnknown Methods
	//:>****************************************************************************************
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return SuperClass::AddRef();
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = SuperClass::Release();
		return cref;
	}


	//:>****************************************************************************************
	//:>    Miscellaneous Methods
	//:>****************************************************************************************
	STDMETHOD(CreateDummyID)(HVO * phvo)
	{
		// If this logic changes at all, be sure to check get_IsValidObject and IsDummyId()!
		*phvo = m_hvoNextDummy--;
		return S_OK;
	}
	STDMETHOD(ClearInfoAbout)(HVO hvo, VwClearInfoAction cia);
	STDMETHOD(ClearInfoAboutAll)(HVO * prghvo, int chvo, VwClearInfoAction cia);

	STDMETHOD(Close)();

	STDMETHOD(get_AutoloadPolicy)(AutoloadPolicies * palp);
	STDMETHOD(put_AutoloadPolicy)(AutoloadPolicies alp);

#ifndef NO_DATABASE_SUPPORT

	//:> ISilDataAccess methods are inherited unchanged

	//:>****************************************************************************************
	//:>	Methods used to load/save/unload the database.
	//:>****************************************************************************************
	STDMETHOD(Load)(BSTR bstrSqlStmt, IDbColSpec * pdcs, HVO hvoBase, int nrowMax,
		IAdvInd * padvi, ComBool fNotifyChange);
	STDMETHOD(Save)();
	STDMETHOD(ClearAllData)();

	//:>****************************************************************************************
	//:>	Method used to determine the editability of a record.
	//:>****************************************************************************************
	STDMETHOD(CheckTimeStamp)(HVO hvo);
	STDMETHOD(SetTimeStamp)(HVO hvo);
	STDMETHOD(CacheCurrTimeStamp)(HVO hvo);


	//:>****************************************************************************************
	//:>	Methods used to retrieve object REFERENCE information.
	//:>****************************************************************************************
	STDMETHOD(get_ObjectProp)(HVO hvo, PropTag tag, HVO * phvo);
	STDMETHOD(get_VecItem)(HVO hvo, PropTag tag, int index, HVO * phvo);
	STDMETHOD(get_VecSize)(HVO hvo, PropTag tag, int * pchvo);
	STDMETHOD(VecProp)(HVO hvo, PropTag tag, int chvoMax, int * pchvo, HVO * prghvo);


	//:>****************************************************************************************
	//:>	Methods used to retrieve object PROPERTY information from the cache (except
	//:>	references).
	//:>****************************************************************************************
	STDMETHOD(BinaryPropRgb)(HVO obj, PropTag tag, byte * prgb, int cbMax, int * pcb);
	STDMETHOD(get_GuidProp)(HVO hvo, PropTag tag, GUID * puid);
	STDMETHOD(get_ObjFromGuid)(GUID uid, HVO * pHvo);
	STDMETHOD(get_Int64Prop)(HVO hvo, PropTag tag, int64 * plln);
	STDMETHOD(get_IntProp)(HVO hvo, PropTag tag, int * pn);
	STDMETHOD(get_MultiStringAlt)(HVO hvo, PropTag tag, int ws, ITsString ** pptss);
	STDMETHOD(get_StringProp)(HVO hvo, PropTag tag, ITsString ** pptss);
	STDMETHOD(get_Prop)(HVO hvo, PropTag tag, VARIANT * pvar);
	STDMETHOD(get_TimeProp)(HVO hvo, PropTag tag, int64 * ptim);
	STDMETHOD(get_UnknownProp)(HVO hvo, PropTag tag, IUnknown ** ppunk);
	bool UnicodeProp(HVO obj, PropTag tag, StrUni & stu);
	STDMETHOD(get_UnicodeProp)(HVO obj, PropTag tag, BSTR * pbstr);
	STDMETHOD(UnicodePropRgch)(HVO obj, PropTag tag, OLECHAR * prgch, int cchMax, int * pcch);


	//:>****************************************************************************************
	//:>	Methods to manage the undo/redo mechanism.
	//:>****************************************************************************************
	STDMETHOD(BeginUndoTask)(BSTR bstrUndo, BSTR bstrRedo);
	STDMETHOD(EndUndoTask)();
	STDMETHOD(ContinueUndoTask)();
	STDMETHOD(EndOuterUndoTask)();
	STDMETHOD(BreakUndoTask)(BSTR bstrUndo, BSTR bstrRedo);
	STDMETHOD(Rollback)();
	STDMETHOD(GetActionHandler)(IActionHandler ** ppacth);
	STDMETHOD(SetActionHandler)(IActionHandler * pacth);


	//:>****************************************************************************************
	//:>	Methods used to create new objects, delete existing objects, or a combination of
	//:>	both of these actions (in the case of MoveOwnSeq).  These are the only methods that
	//:>	actually change the OWNERSHIP RELATIONSHIPS of objects.
	//:>****************************************************************************************
	STDMETHOD(DeleteObj)(HVO hvoObj);
	STDMETHOD(DeleteObjOwner)(HVO hvoOwner, HVO hvoObj, PropTag tag, int ihvo);
	STDMETHOD(InsertNew)(HVO hvoObj, PropTag tag, int ihvo, int chvo, IVwStylesheet * pss);
	STDMETHOD(MoveOwnSeq)(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart, int ihvoEnd,
		HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart);
	STDMETHOD(MoveOwn)(HVO hvoSrcOwner, PropTag tagSrc, HVO hvo, HVO hvoDstOwner, PropTag tagDst,
		int ihvoDstStart);
	STDMETHOD(MakeNewObject)(int clid, HVO hvoOwner, PropTag tag, int ord, HVO * phvoNew);


	//:>****************************************************************************************
	//:>	The "SetObjProp" method changes the values of atomic REFERENCES and the "Replace"
	//:>	method changes the values of collection/sequence references.
	//:>****************************************************************************************
	STDMETHOD(Replace)(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim, HVO * prghvo,
		int chvo);
	STDMETHOD(SetObjProp)(HVO hvo, PropTag tag, HVO hvoObj);


	//:>****************************************************************************************
	//:>	Methods used to change object PROPERTY information (outside of reference
	//:>	properties).
	//:>****************************************************************************************
	STDMETHOD(SetBinary)(HVO hvo, PropTag tag, byte * prgb, int cb);
	STDMETHOD(SetGuid)(HVO hvo, PropTag tag, GUID uid);
	STDMETHOD(SetInt)(HVO hvo, PropTag tag, int n);
	STDMETHOD(SetInt64)(HVO hvo, PropTag tag, int64 lln);
	STDMETHOD(SetBoolean)(HVO hvo, PropTag tag, ComBool n);
	STDMETHOD(SetMultiStringAlt)(HVO hvo, PropTag tag, int ws, ITsString * ptss);
	STDMETHOD(SetString)(HVO hvo, PropTag tag, ITsString * ptss);
	STDMETHOD(SetTime)(HVO hvo, PropTag tag, int64 tim);
	STDMETHOD(SetUnicode)(HVO hvo, PropTag tag, OLECHAR * prgch, int cch);
	STDMETHOD(SetUnknown)(HVO hvo, PropTag tag, IUnknown * punk);

	//:>****************************************************************************************
	//:>	Methods to set and retrieve extra info for collection/sequence references.
	//:>****************************************************************************************
	STDMETHOD(InsertRelExtra)(HVO hvoSrc, PropTag tag, int ihvo, HVO hvoDst, BSTR bstrExtra);
	STDMETHOD(UpdateRelExtra)(HVO hvoSrc, PropTag tag, int ihvo, BSTR bstrExtra);

	//:>****************************************************************************************
	//:>	Methods loading and getting information from CmObject table that is frequently used.
	//:>****************************************************************************************
	STDMETHOD(get_ObjOwner)(HVO hvo, HVO * phvoOwn);
	STDMETHOD(get_ObjClid)(HVO hvo, int * pclid);
	STDMETHOD(get_ObjOwnFlid)(HVO hvo, int * pflidOwn);
	STDMETHOD(LoadObjInfo)(HVO hvo);
	STDMETHOD(LoadData)(HVO * prghvo, int * prgclsid, int chvo, IVwDataSpec *pdts,
		IAdvInd * padvi, ComBool fIncludeOwnedObjects);
	STDMETHOD(UpdatePropIfCached)(HVO hvo, PropTag tag, int cpt, int ws);
	STDMETHOD(GetIdFromGuid)(GUID * puid, HVO * phvo);
	STDMETHOD(get_IsValidObject)(HVO hvo, ComBool * pfValid);
	STDMETHOD(get_IsDummyId)(HVO hvo, ComBool * pfDummy);
#endif !NO_DATABASE_SUPPORT
	virtual HVO GetObjPropCheckType(HVO hvo, PropTag tag);

	// Moved from earlier class CustViewDa.
	void LoadData(HvoClsidVec & vhcItems, UserViewSpec * puvs, IAdvInd * padvi = NULL,
		bool fRecurse = true);
	void CheckWsLoad()
	{
		if (!m_fLoadedWsInfo)
			LoadWritingSystems();
	}
	/*------------------------------------------------------------------------------------------
		Return the vector of analysis encodings.
	------------------------------------------------------------------------------------------*/
	virtual Vector<int> & AnalWss()
	{
		CheckWsLoad();
		return m_vwsAnal;
	}
	/*------------------------------------------------------------------------------------------
		Return the vector of analysis then vernacular writing systems.
	------------------------------------------------------------------------------------------*/
	virtual Vector<int> & AnalVernWss()
	{
		CheckWsLoad();
		return m_vwsAnalVern;
	}
	/*------------------------------------------------------------------------------------------
		Return the vector of vernacular then analysis writing systems.
	------------------------------------------------------------------------------------------*/
	virtual Vector<int> & VernAnalWss()
	{
		CheckWsLoad();
		return m_vwsVernAnal;
	}
	/*------------------------------------------------------------------------------------------
		Return the first analysis writing system. (LoadWritingSytems makes sure there is one).
	------------------------------------------------------------------------------------------*/
	virtual int AnalWs()
	{
		CheckWsLoad();
		return m_vwsAnal[0];
	}
	/*------------------------------------------------------------------------------------------
		Return the vector of vernacular encodings.
	------------------------------------------------------------------------------------------*/
	virtual Vector<int> & VernWss()
	{
		CheckWsLoad();
		return m_vwsVern;
	}

protected:
	void LoadRefSeq(FldSpec * pfsp, StrUni suItems);
	void LoadAtomicBackRefSeq(FldSpec * pfsp, StrUni stuItems);
	void LoadWritingSystems();
	void CacheCurrTimeStampIfMissing(HVO hvo);
	void LoadVecProp(HVO hvo, PropTag tag);
	void LoadSimpleProp(HVO hvo, PropTag tag, int oct);
	void RecordVectorValue(bool fNotifyChange, HVO hvoVecBase, PropTag tagVec,
		Vector<HVO>& vhvo);
	bool TestAndNoteRecentAutoloads(PropTag tag, int ws, int clsid);
	int TestAndNoteLoadAllForReadOnly(int tag, int ws, int clsid);
	void ClearOwnedInfoAbout(HVO hvo, ComBool fIncludeOwnedObjects);
	void ClearIncomingReferences();
#define khvoFirstDummyId -1000000
	bool IsDummyId(int hvo)
	{
		return hvo <= khvoFirstDummyId && hvo > m_hvoNextDummy;
	}

private:
	void MoveOwnedObject(HVO hvoSrcOwner, PropTag tagSrc, HVO hvoStart, HVO hvoEnd,
		HVO hvoDstOwner, PropTag tagDst, HVO hvoDstStart, ISqlUndoAction* qsqlua, HVO hvoUndoDst,
		const StrUni& stuVerifyUndoable, const StrUni& stuVerifyRedoable);
};
DEFINE_COM_PTR(VwOleDbDa);

class VwDataSpec : public IVwDataSpec
{
public:
	VwDataSpec();
	virtual ~VwDataSpec();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
	// IUnknown
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}
	// IVwDataSpec
	STDMETHOD(AddField)(int clsid, PropTag tag, FldType ft, ILgWritingSystemFactory * pwsf,
		int ws);
	UserViewSpec * ViewSpec() {return m_quvs;}
	void SetMetaNames(IFwMetaDataCache * pmdc);
protected:
	long m_cref;
	bool m_fGotMetaNames;
	UserViewSpecPtr m_quvs;
};
DEFINE_COM_PTR(VwDataSpec);

#endif // VwOleDbDa_INCLUDED
