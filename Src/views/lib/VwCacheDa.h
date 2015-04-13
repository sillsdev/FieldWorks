/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwCacheDa.h
Responsibility: John Thomson
Last reviewed: never

Description:
	Provides an implementation of the ISilDataAccess interface based on the client pre-loading
	values for all object properties that will be needed (via the Cache* methods).  In essence,
	this is a data cache that stores object properties.

	This file contains class declarations for the following classes:
		ObjPropEncRec
		ObjPropRec
		ObjSeq
		VwCacheDa
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwCacheDa_INCLUDED
#define VwCacheDa_INCLUDED
namespace TestViews
{
	class TestVwTextStore;
};
//#include <hash_set>
#include <set>
/*----------------------------------------------------------------------------------------------
	A storage structure that uniquely identifies an object property with an writing system (eg. a
	MultiString).

	@h3{Hungarian: oper}
----------------------------------------------------------------------------------------------*/
class ObjPropEncRec
{
public:
	HVO m_hvo;
	PropTag m_tag;
	int m_ws;
	ObjPropEncRec() // needs default constructor to be a key
	{
		// see below
		memset(this, 0, sizeof(ObjPropEncRec));

		m_hvo = 0;
		m_tag = 0;
		m_ws = 0;
	}

	ObjPropEncRec(HVO hvo, PropTag tag, int ws)
	{
		// The next line seems to be very strange. However, because we're using this class to
		// calculate a hash value, the entire memory this class occupies has to be initialized to
		// 0. Without doing this it wasn't the case on 64-bit Linux where HVO is 8 bytes and
		// PropTag is 4 bytes, but the size of ObjPropRec is 16 byte to keep classes aligned in
		// 8 byte chunks.
		memset(this, 0, sizeof(ObjPropEncRec));

		m_hvo = hvo;
		m_tag = tag;
		m_ws = ws;
	}
};


/*----------------------------------------------------------------------------------------------
	A storage structure that uniquely identify an object property.

	@h3{Hungarian: opr}
----------------------------------------------------------------------------------------------*/
class ObjPropRec
{
public:
	HVO m_hvo;
	PropTag m_tag;

	ObjPropRec() // needs default constructor to be a key
	{
		// see below
		memset(this, 0, sizeof(ObjPropRec));

		m_hvo = 0;
		m_tag = 0;
	}

	ObjPropRec(HVO hvo, PropTag tag)
	{
		// The next line seems to be very strange. However, because we're using this class to
		// calculate a hash value, the entire memory this class occupies has to be initialized to
		// 0. Without doing this it wasn't the case on 64-bit Linux where HVO is 8 bytes and
		// PropTag is 4 bytes, but the size of ObjPropRec is 16 byte to keep classes aligned in
		// 8 byte chunks.
		memset(this, 0, sizeof(ObjPropRec));

		m_hvo = hvo;
		m_tag = tag;
	}
};


/*----------------------------------------------------------------------------------------------
	A storage structure for a sequence (or collection) of object Ids (ie. HVO's).

	@h3{Hungarian: os}
----------------------------------------------------------------------------------------------*/
class ObjSeq
{
public:
	int m_cobj;
	HVO * m_prghvo;

	ObjSeq()
	{
		m_cobj = 0;
		m_prghvo = NULL;
	}
};


/*----------------------------------------------------------------------------------------------
	A storage structure for a sequence (or collection) of Unicode strings.

	@h3{Hungarian: sx}
----------------------------------------------------------------------------------------------*/
class SeqExtra
{
public:
	int m_cstu;
	StrUni * m_prgstu;

	SeqExtra()
	{
		m_cstu = 0;
		m_prgstu = NULL;
	}
};

// Information about PropChanged calls
struct PropChangedInfo
{
public:
	PropChangedInfo(IVwNotifyChange * _pnchng, int _pct, HVO _hvo, int _tag, int _ivMin,
		int _cvIns, int _cvDel): pnchng(_pnchng), pct(_pct), hvo(_hvo), tag(_tag), ivMin(_ivMin),
		cvIns(_cvIns), cvDel(_cvDel)
	{
	}

	IVwNotifyChange * pnchng;
	int pct;
	HVO hvo;
	int tag;
	int ivMin;
	int cvIns;
	int cvDel;
};

typedef enum {
	kvhrNotVirtual,
	kvhrUse,
	kvhrUseAndRemove,
} VhResult;

typedef enum {
	kwvNotVirtual, // Not a virtual property, write normally
	kwvCache, // Virtual property, has been written, now cache.
	kwvDone, // Virtual ComputeEveryTime property, do no more.
} WriteVirtualResult;

//:>********************************************************************************************
//:>	Three types of hash maps that are used to store REFERENCES from one object to another
//:>	object (or several objects).
//:>********************************************************************************************
// A map from an <object cookie, property tag> pair to hvo
typedef HashMap<ObjPropRec, HVO> ObjPropObjMap; // Hungarian hmoprobj
// A map from <object cookie, property tag> pair to obj sequence
typedef HashMap<ObjPropRec, ObjSeq> ObjPropSeqMap; // Hungarian hmoprsobj
// A map from <object cookie, property tag> pair to obj sequence with Extra info
typedef HashMap<ObjPropRec, SeqExtra> ObjPropExtraMap; // Hungarian hmoprsx


//:>********************************************************************************************
//:>	Types of hash maps that are used to store object PROPERTY INFORMATION (excluding
//:>	references to other objects).
//:>********************************************************************************************
// A map from <object cookie, property tag, ws > to TsString, for multi string alts
typedef ComHashMap<ObjPropEncRec, ITsString> ObjPropEncTssMap; // Hungarian hmopertss
// A map from an <object cookie, property tag> pair to GUID
typedef HashMap<ObjPropRec, GUID> ObjPropGuidMap; // Hungarian hmoprguid
// A map from a GUID to an object cookie.
typedef HashMap<GUID, HVO> GuidObjMap; // Hungarian hmoguidobj
// A map from an <object cookie, property tag> pair to int
typedef HashMap<ObjPropRec, int> ObjPropIntMap; // Hungarian hmoprn
// A map from an <object cookie, property tag> pair to int64
typedef HashMap<ObjPropRec, int64> ObjPropInt64Map; // Hungarian hmoprlln
// A map from an <object cookie, property tag> pair to StrAnsi (for binary fields)
typedef HashMap<ObjPropRec, StrAnsi> ObjPropStaMap; // Hungarian hmoprsta
// A map from <object cookie, property tag> to StrUni, for Unicode props
typedef HashMap<ObjPropRec, StrUni> ObjPropStrMap; // Hungarian hmoprstu
// A map from <object cookie, property tag> pair to TsString
typedef ComHashMap<ObjPropRec, ITsString> ObjPropTssMap; // Hungarian hmoprtss
// A map from <object cookie, property tag> pair to IUnknown
typedef ComHashMap<ObjPropRec, IUnknown> ObjPropUnkMap; // Hungarian hmoprunk

// a special type used to store virtual property information
typedef ComHashMap<PropTag, IVwVirtualHandler> TagVhMap; // Hungarian hmtagvh
typedef ComHashMapStrUni<IVwVirtualHandler> StrVhMap; // Hungarian hmstuvh


//:>********************************************************************************************
//:>	Types of sets that are used to indicate which object properties that have changed (or
//:>	have been deleted) that are stored in the types of hash maps above.
//:>********************************************************************************************
typedef Set<HVO> HvoSet; // Hungarian shvo
typedef Set<ObjPropRec> ObjPropSet; // Hungarian sopr
typedef Set<ObjPropEncRec> ObjPropEncSet; // Hungarian soper

/*----------------------------------------------------------------------------------------------
	A data cache that can be used for storing and retrieving object property information.

	Cross-Reference: ${IVwCacheDa}

	@h3{Hungarian: cda}
----------------------------------------------------------------------------------------------*/
class VwCacheDa : public VwBaseDataAccess, public IVwCacheDa
{
	friend class TestViews::TestVwTextStore;
public:
	typedef VwBaseDataAccess SuperClass;

	VwCacheDa();
	~VwCacheDa();
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);
	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return SuperClass::AddRef();
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = SuperClass::Release();
		return cref;
	}


	//:>****************************************************************************************
	//:>	Methods used for initially loading the cache with object REFERENCE information.
	//:>	CacheObjProp should be used for "atomic references" (ie. a single object pointing
	//:>	to another object) and CacheVecProp should be used for "collection" and "sequence"
	//:>	references (ie. a single object linked with several objects).  Note that once this
	//:>	information is loaded in the cache, the SetObjProp and Replace methods should be
	//:>	used to affect changes to the references.
	//:>****************************************************************************************
	STDMETHOD(CacheObjProp)(HVO obj, PropTag tag, HVO val);
	STDMETHOD(CacheVecProp)(HVO hvo, PropTag tag, HVO rghvo[], const int chvo);

	/*------------------------------------------------------------------------------------------
		This method is used to replace items in a vector only in the cache. Since this class is
		subclassed by another class that overrides the Replace method to instantly make changes
		in a database, this provides a way to make a change to a vector in the cache that does
		not affect the database. This is useful when using dummy IDs to temporarily store a
		vector of IDs.
	------------------------------------------------------------------------------------------*/
	STDMETHOD(CacheReplace)(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
		HVO rghvo[], int chvo);

	//:>****************************************************************************************
	//:>	Methods used for initially loading the cache with object PROPERTY information
	//:>	(excluding reference information).  Note that after loading the cache, these
	//:>	methods should NOT be used but rather the Set* methods.
	//:>****************************************************************************************
	STDMETHOD(CacheBinaryProp)(HVO obj, PropTag tag, byte * prgb, int cb);
	STDMETHOD(CacheGuidProp)(HVO obj, PropTag tag, GUID uid);
	STDMETHOD(CacheInt64Prop)(HVO obj, PropTag tag, int64 val);
	STDMETHOD(CacheIntProp)(HVO obj, PropTag tag, int val);
	STDMETHOD(CacheBooleanProp)(HVO obj, PropTag tag, ComBool val);
	STDMETHOD(CacheStringAlt)(HVO obj, PropTag tag, int ws, ITsString * ptss);
	STDMETHOD(CacheStringProp)(HVO obj, PropTag tag, ITsString * ptss);
	STDMETHOD(CacheTimeProp)(HVO hvo, PropTag tag, SilTime val);
	STDMETHOD(CacheUnicodeProp)(HVO obj, PropTag tag, OLECHAR * prgch, int cch);
	STDMETHOD(CacheUnknown)(HVO obj, PropTag tag, IUnknown * punk);


	//:>****************************************************************************************
	//:>	Methods used to retrieve object REFERENCE information.
	//:>****************************************************************************************
	STDMETHOD(get_ObjectProp)(HVO hvo, PropTag tag, HVO * phvo);
	STDMETHOD(get_VecItem)(HVO hvo, PropTag tag, int index, HVO * phvo);
	STDMETHOD(get_VecSize)(HVO hvo, PropTag tag, int * pchvo);
	STDMETHOD(get_VecSizeAssumeCached)(HVO hvo, PropTag tag, int * pchvo);
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
	STDMETHOD(get_BooleanProp)(HVO hvo, PropTag tag, ComBool * pn);
	STDMETHOD(get_MultiStringAlt)(HVO hvo, PropTag tag, int ws, ITsString ** pptss);
	STDMETHOD(get_StringProp)(HVO hvo, PropTag tag, ITsString ** pptss);
	STDMETHOD(get_Prop)(HVO hvo, PropTag tag, VARIANT * pvar);
	STDMETHOD(get_TimeProp)(HVO hvo, PropTag tag, int64 * ptim);
	STDMETHOD(get_UnknownProp)(HVO hvo, PropTag tag, IUnknown ** ppunk);
	bool UnicodeProp(HVO obj, PropTag tag, StrUni & stu);
	STDMETHOD(get_UnicodeProp)(HVO obj, PropTag tag, BSTR * pbstr);
	STDMETHOD(put_UnicodeProp)(HVO obj, PropTag tag, BSTR bstr);
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
	//:>	both of these actions (in the case of MoveOwnSeq).  These are the only methods
	//:>	that actually change the OWNERSHIP RELATIONSHIPS of objects.
	//:>****************************************************************************************
	STDMETHOD(DeleteObj)(HVO hvoObj);
	STDMETHOD(DeleteObjOwner)(HVO hvoOwner, HVO hvoObj, PropTag tag, int ihvo);
	STDMETHOD(InsertNew)(HVO hvoObj, PropTag tag, int ihvo, int chvo, IVwStylesheet * pss);
	STDMETHOD(MakeNewObject)(int clid, HVO hvoOwner, PropTag tag, int ord, HVO * phvoNew);
	STDMETHOD(MoveOwnSeq)(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart, int ihvoEnd,
		HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart);
	STDMETHOD(MoveOwn)(HVO hvoSrcOwner, PropTag tagSrc, HVO hvo, HVO hvoDstOwner, PropTag tagDst,
		int ihvoDstStart);
	void DoInsert(HVO hvoOwner, HVO hvoNew, PropTag tag, int ord);

	//:>****************************************************************************************
	//:>	SetObjProp changes the value of an atomic REFERENCES and Replace changes the values
	//:>	of collection/sequence references. RemoveObj removes any reference to an object.
	//:>****************************************************************************************
	STDMETHOD(Replace)(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
		HVO * prghvo, int chvo);
	STDMETHOD(SetObjProp)(HVO hvo, PropTag tag, HVO hvoObj);
	STDMETHOD(RemoveObjRefs)(HVO hvo);


	//:>****************************************************************************************
	//:>	Methods used to change object PROPERTY information (except reference properties).
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
	STDMETHOD(SetUnknown)(HVO hvoObj, PropTag tag, IUnknown * punk);


	//:>****************************************************************************************
	//:>	A method that indicates if the cache has changed since it was first loaded by means
	//:>	of Cache* methods.  Basically what this means is that client code has called one
	//:>	of the property modification methods (eg. "Set" methods, MakeNewObject, DeleteObject*,
	//:>	MoveOwnSeq, or Replace methods).
	//:>****************************************************************************************
	STDMETHOD(IsDirty)(ComBool * pf);
	STDMETHOD(ClearDirty)();
	STDMETHOD(get_IsPropInCache)(HVO hvo, PropTag tag, int cpt, int ws, ComBool * pfCached);
	STDMETHOD(get_IsValidObject)(HVO hvo, ComBool * pfValid);
	STDMETHOD(get_IsDummyId)(HVO hvo, ComBool * pfDummy);

	//:>****************************************************************************************
	//:>	Methods used for sending notifications to subscribers when a designated object
	//:>	property value (in the cache) has changed.
	//:>****************************************************************************************
	STDMETHOD(AddNotification)(IVwNotifyChange * pnchng);
	STDMETHOD(PropChanged)(IVwNotifyChange * pnchng, int pct, HVO hvo, int tag, int ivMin,
		int cvIns, int cvDel);
	STDMETHOD(RemoveNotification)(IVwNotifyChange * pnchng);
	STDMETHOD(GetDisplayIndex)(HVO hvoOwn, int flid, int ihvo, int * ihvoDisp);

	// Return the index of hvo in the flid vector of hvoOwn.
	STDMETHOD(GetObjIndex)(HVO hvoOwn, int flid, HVO hvo, int * ihvo);

	// Return an outline number for the given hvo. It should be recursively owned in the
	// flid property. if fFinPer is true, a final period is attached. The final string
	// is placed in stu. It returns false if something went wrong.
	STDMETHOD(GetOutlineNumber)(HVO hvo, int flid, ComBool fFinPer, BSTR * pbstr);

	//:>****************************************************************************************
	//:>	Methods to set and retrieve extra info for collection/sequence references.
	//:>****************************************************************************************
	STDMETHOD(InsertRelExtra)(HVO hvoSrc, PropTag tag, int ihvo, HVO hvoDst, BSTR bstrExtra);
	STDMETHOD(UpdateRelExtra)(HVO hvoSrc, PropTag tag, int ihvo, BSTR bstrExtra);
	STDMETHOD(GetRelExtra)(HVO hvoSrc, PropTag tag, int ihvo, BSTR * pbstrExtra);

	//:>****************************************************************************************
	//:>	Methods to implement IStructuredTextDataAccess.
	//:>****************************************************************************************
	STDMETHOD(get_ParaContentsFlid)(PropTag * paraContentsFlid);
	STDMETHOD(get_ParaPropertiesFlid)(PropTag * paraPropertiesFlid);
	STDMETHOD(get_TextParagraphsFlid)(PropTag * textParagraphsFlid);

	//:>****************************************************************************************
	//:>	Other methods
	//:>****************************************************************************************
	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** ppencf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);
	STDMETHOD(get_WritingSystemsOfInterest)(int cwsMax, int * pws, int * pcws);
	STDMETHOD(ClearInfoAbout)(HVO hvo, VwClearInfoAction cia);
	STDMETHOD(ClearInfoAboutAll)(HVO * prghvo, int chvo, VwClearInfoAction cia);
	STDMETHOD(get_CachedIntProp)(HVO hvo, PropTag tag, ComBool * pf, int * pn);
	STDMETHOD(InstallVirtual)(IVwVirtualHandler * pvh);
	STDMETHOD(GetVirtualHandlerId)(PropTag tag, IVwVirtualHandler ** ppvh);
	STDMETHOD(GetVirtualHandlerName)(BSTR bstrClass, BSTR bstrField,
		IVwVirtualHandler ** ppvh);
	STDMETHOD(ClearAllData)();
	STDMETHOD(ClearVirtualProperties)();
	STDMETHOD(get_MetaDataCache)(IFwMetaDataCache ** ppmdc);
	STDMETHOD(putref_MetaDataCache)(IFwMetaDataCache * pmdc);
	STDMETHOD(MoveString)(int hvoSource, PropTag flidSrc, int wsSrc, int ichMin,
		int ichLim, HVO hvoDst, PropTag flidDst, int wsDst, int ichDest, ComBool fDstIsNew);

protected:
	HVO m_hvoNext;
	ITsStrFactoryPtr m_qtsf;
	ILgWritingSystemFactoryPtr m_qwsf;
	IActionHandlerPtr m_qacth;

	void SuppressPropChanges();
	void ResumePropChanges();

	//:>****************************************************************************************
	//:>   The following 3 hash maps store atomic and collection/sequence REFERENCE information.
	//:>****************************************************************************************
	// Map from <object, tag> to hvo, used to cache atomic object properties
	ObjPropObjMap m_hmoprobj;
	// Map from <object, tag> to ObjSeq, used to cache "collection" and "sequence" properties
	ObjPropSeqMap m_hmoprsobj;
	// Map from <object, tag> to SeqExtra, used to cache extra info about "collections" and
	// "sequences"
	ObjPropExtraMap m_hmoprsx;


	//:>****************************************************************************************
	//:>	These hash maps store all object PROPERTY information except reference info.
	//:>****************************************************************************************
	// Map from <object, tag> to GUID, used to cache uniqueidentifier properties
	ObjPropGuidMap m_hmoprguid;
	// Map from GUID to object
	GuidObjMap m_hmoguidobj;
	// Map from <object, tag> to int, used to cache integer properties
	ObjPropIntMap m_hmoprn;
	// Map from <object, tag> to int64, used to cache double integer properties (and SilTimes)
	ObjPropInt64Map m_hmoprlln;
	// Map from <object, tag> to ITsString, used to cache string properties
	ObjPropUnkMap m_hmoprunk;
	// Map from <object, tag, ws> to ITsString, used to cache multistring alternatives
	ObjPropEncTssMap m_hmopertss;
	// Map from <object, tag> to StrAnsi used to cache binary properties
	ObjPropStaMap m_hmoprsta;
	// Map from <object, tag> to StrUni, for Unicode props
	ObjPropTssMap m_hmoprtss;
	// A map from <object cookie, property tag> pair to IUnknown, caches arbitrary objects
	ObjPropStrMap m_hmoprstu;

	//:>****************************************************************************************
	//:>	These 3 "Sets" indicate which objects have been DELETED and which object properties
	//:>	have been CHANGED.
	//:>****************************************************************************************
	// A set of objects that have been deleted.
	HvoSet m_shvoDeleted;
	// A set indicating which props have changed (excluding MSA)
	ObjPropSet m_soprMods;
	// A set indicating which MSA's have changed.
	ObjPropEncSet m_soperMods;

	//:>****************************************************************************************
	//:>	Used for object property change notification.
	//:>****************************************************************************************
#define TRY_HASH_SET
#ifdef TRY_HASH_SET
	std::set<IVwNotifyChange*> m_vvncNew;
	std::set<IVwNotifyChange*>::const_iterator m_vvncNew_cIter;
#else
	Vector<IVwNotifyChange *> m_vvnc;
#endif
	Vector<PropChangedInfo> m_vPropChangeds; // Queued calls to PropChanged
	HvoSet m_shvoNewObjectsWhileSuppressed; // objects created while propChanged is suppressed; ignore PropChanged for these.

	// Used to store virtual property information
	TagVhMap m_hmtagvh;
	// Next available PropTag for virtual properties
	PropTag m_tagNextVp;
	// Keeps it positive, allows 2^24 virtual properties, keeps clear of real ones.
	// At some point we may make PropTag an unsigned int, then we can use ff.
#define ktagMinVp 0x7f000000
	StrVhMap m_hmstuvh; // map from virtual property name class<cr>field to virtual handler.

	// Typically null for now, except in subclass VwOleDbDa
	IFwMetaDataCachePtr m_qmdc;

	int m_nSuppressPropChangesLevel; // Number of calls to SuppressPropChanges without
									 // matching ResumePropChanges.

	//:>****************************************************************************************
	//:>	Other methods
	//:>****************************************************************************************
	virtual void InformNowDirty()
	{
	}

	// This class is the best place to maintain owning HVO/field properties, but it doesn't
	// actually know about owning/non-owning fields. This method should be overridden by any
	// subclass that needs these properties to be maintained. If this method returns false for
	// a particular tag, setting that tag will not result in the side-effect of caching the
	// owner or owning field for the object property being set.
	virtual bool IsOwningField(PropTag tag)
	{
		return false;
	}

	HRESULT ReplaceAux(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
		HVO * prghvo, int chvo);
	void InsBackRef(HVO hvoIns, HVO hvoDst, PropTag tagDst);
	void DelBackRef(HVO hvoDel, HVO hvoDst, PropTag tagDst);
	virtual HVO GetObjPropCheckType(HVO hvo, PropTag tag);
	void SetIntVal(HVO hvo, PropTag tag, int n);
	void SetUnicodeVal(HVO hvo, PropTag tag, OLECHAR * prgch, int cch);
	void SetStringVal(HVO hvo, PropTag tag, ITsString * ptss);
	void SetMultiStringAltVal(HVO hvo, PropTag tag, int ws, ITsString * ptss);
	void SetObjPropVal(HVO hvo, PropTag tag, HVO hvoObj);
	void SetInt64Val(HVO hvo, PropTag tag, int64 lln);

	VhResult TryVirtual(HVO hvo, PropTag tag, int ws = 0);

	WriteVirtualResult TryVirtualReplace(HVO hvo, PropTag tag, int ihvoMin, int ihvoLim,
		HVO * prghvo, int chvoIns);
	WriteVirtualResult TryVirtualAtomic(HVO hvo, PropTag tag, HVO newVal);

	WriteVirtualResult TryWriteVirtualInt64(HVO hvo, PropTag tag, int64 val);
	WriteVirtualResult TryWriteVirtualUnicode(HVO hvo, PropTag tag, OLECHAR * prgch, int cch);
	WriteVirtualResult TryWriteVirtualObj(HVO hvo, PropTag tag, int ws, IUnknown * punk);

	void DeleteObjOwnerCore(HVO hvoOwner, HVO hvoObj, PropTag tag, int ihvo, bool clearIncomingRefs = true);
	void RemoveCachedProperties(HVO hvoDeleted);
	void RemoveCachedProperties(HVO hvoDeleted, Set<HVO> & sethvoDel,
		ObjPropIntMap & hmoprnChg);
	void ClearCriticalMaps();

private:
	void NewObject(int clid, HVO hvoOwner, PropTag tag, int ord, HVO * phvoNew);
	void MoveOwnedObject(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart,
		int ihvoEnd, HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart,
		HVO* prghvo, int cobj);
};

DEFINE_COM_PTR(VwCacheDa);
#endif // VwCacheDa_INCLUDED
