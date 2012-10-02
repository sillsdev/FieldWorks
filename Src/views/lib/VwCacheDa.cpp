/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwCacheDa.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file contains class definitions for the following class:
		VwCacheDa

	Provides an implementation of the ISilDataAccess interface based on the client pre-loading
	values for all object properties that will be needed (via the Cache* methods).  In essence,
	this is a data cache that stores object properties.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

#define TRY_HASH_SET

static DummyFactory g_fact(_T("SIL.Views.lib.VwCacheDa"));

void ClearObjSeq(ObjSeq &os)
{
	if (os.m_prghvo)
	{
		delete[] os.m_prghvo;
		os.m_prghvo = NULL;
		os.m_cobj = 0; // just for paranoia
	}
}

void VwCacheDa::ClearCriticalMaps()
{
	// Free all the object sequence data
	ObjPropSeqMap::iterator it;
	for (it = m_hmoprsobj.Begin(); it != m_hmoprsobj.End(); ++it)
	{
		ObjSeq & os = it.GetValue();
		ClearObjSeq(os);
	}
	m_hmoprsobj.Clear();

	ObjPropExtraMap::iterator itsx;
	for (itsx = m_hmoprsx.Begin(); itsx != m_hmoprsx.End(); ++itsx)
	{
		SeqExtra & sx = itsx.GetValue();
		if (sx.m_prgstu)
		{
			delete[] sx.m_prgstu;
			sx.m_prgstu = NULL;
			sx.m_cstu = 0; // just for paranoia
		}
	}
	m_hmoprsx.Clear();
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwCacheDa::~VwCacheDa()
{
	ClearCriticalMaps();
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwCacheDa::VwCacheDa()
{
	// This gives us very big numbers for new Ids. If that is not good enough a subclass should
	// override.
	m_hvoNext = 100000000;
	m_tagNextVp = ktagMinVp;
	m_vPropChangeds.Clear();
	m_nSuppressPropChangesLevel = 0;
}



//:>********************************************************************************************
//:>    IUnknown Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ISilDataAccess *>(this));
	else if (riid == IID_ISilDataAccess)
		*ppv = static_cast<ISilDataAccess *>(this);
	else if (riid == IID_IVwCacheDa)
		*ppv = static_cast<IVwCacheDa *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<ISilDataAccess *>(this),
			IID_IVwCacheDa, IID_ISilDataAccess);
		return NOERROR;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

/*----------------------------------------------------------------------------------------------
	Method to support using GenericFactory to create an instance. An actual generic factory
	instance is not made in this file, because it is included in many places. Instead, currently
	one generic factory exists in VwRootBox.cpp.
----------------------------------------------------------------------------------------------*/
void VwCacheDa::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwCacheDa> qcda;
	qcda.Attach(NewObj VwCacheDa());		// ref count initialy 1
	CheckHr(qcda->QueryInterface(riid, ppv));
}



//:>/*******************************************************************************************
//:>	Methods used for initially loading the cache with object REFERENCE information.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheObjProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheObjProp(HVO hvo, PropTag tag, HVO val)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	m_hmoprobj.Insert(oprKey, val, true); // allow overwrites
	// REVIEW JohnT (TomB): Do we always want to store the owner of the "val" object in the
	// cache? I was doing this only when calling this function from DoInsert, but I
	// discovered that I also needed it when ReplaceAux was called from other places, so
	// I decided to do it both here and in ReplaceAux no matter where they are being called
	// from. This should cover all situations where an object property is set, but I don't know
	// if it's needed/desired for all those situations. Need a review of all places where this
	// method is used to see if this is what we want.
	// If "val" object is not null, set its owner and owning field.
	if (val != 0 && tag != kflidCmObject_Owner) // prevent nasty recursion
	{
		if (IsOwningField(tag))
		{
			CacheObjProp(val, kflidCmObject_Owner, hvo); // "val" is owned by "hvo"
			CacheIntProp(val, kflidCmObject_OwnFlid, tag);	// in the "tag" field
		}
	}

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheVecProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheVecProp(HVO hvo, PropTag tag, HVO rghvo[], const int chvo)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rghvo, chvo);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	ObjSeq os;
	os.m_cobj = chvo;
	os.m_prghvo = NewObj HVO[chvo];
	CopyItems(rghvo, os.m_prghvo, chvo);

	ObjSeq osOld;
	if (m_hmoprsobj.Retrieve(oprKey, &osOld))
	{
		// A simple overwrite would cause a memory leak. Delete the old array.
		delete[] osOld.m_prghvo;
		// Now it is safe to overwrite.
	}
	m_hmoprsobj.Insert(oprKey, os, true);
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

//:>********************************************************************************************
//:>	Methods used for initially loading the cache with object PROPERTY information (excluding
//:>	reference information).  Note that after loading the cache, these methods should NOT be
//:>	used but rather the Set* methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheBinaryProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheBinaryProp(HVO hvo, PropTag tag, byte * prgb, int cb)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgb, cb);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	StrAnsi sta(reinterpret_cast<const char *>(prgb), cb);
	m_hmoprsta.Insert(oprKey, sta, true); // allow overwrites

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheGuidProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheGuidProp(HVO hvo, PropTag tag, GUID uid)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	m_hmoprguid.Insert(oprKey, uid, true); // allow overwrites

	if (tag == kflidCmObject_Guid)
		m_hmoguidobj.Insert(uid, hvo, true);

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheInt64Prop}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheInt64Prop(HVO hvo, PropTag tag, int64 val)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	m_hmoprlln.Insert(oprKey, val, true); // allow overwrites

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheIntProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheIntProp(HVO hvo, PropTag tag, int val)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	m_hmoprn.Insert(oprKey, val, true); // allow overwrites

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheBooleanProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheBooleanProp(HVO hvo, PropTag tag, ComBool f)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	int val = f;
	m_hmoprn.Insert(oprKey, val, true); // allow overwrites

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheStringAlt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheStringAlt(HVO hvo, PropTag tag, int ws, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptss);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropEncRec opreKey(hvo, tag, ws);
	m_hmopertss.Insert(opreKey, ptss, true);

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheStringFields}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheStringFields(HVO hvo, PropTag tag,
	const OLECHAR * prgchTxt, int cchTxt, const byte * prgbFmt, int cbFmt)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchTxt, cchTxt);
	ChkComArrayArg(prgbFmt, cbFmt);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	if (!m_qtsf)
		m_qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtss;
	CheckHr(m_qtsf->DeserializeStringRgch(prgchTxt, &cchTxt, prgbFmt, &cbFmt, &qtss));
	CacheStringProp(hvo, tag, qtss);

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheStringProp}
	Enhance JohnT: why is ptss allowed to be null? I don't think it should be...some day, test
	whether anything legitimate sets it to that, and if not, make the argument validation
	stronger.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheStringProp(HVO hvo, PropTag tag, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptss);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	m_hmoprtss.Insert(oprKey, ptss, true); // allow overwrites

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheTimeProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheTimeProp(HVO hvo, PropTag tag, SilTime val)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	int64 val1 = val.AsInt64();
	m_hmoprlln.Insert(oprKey, val1, true); // allow overwrites

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheUnicodeProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheUnicodeProp(HVO obj, PropTag tag, OLECHAR * prgch, int cch)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgch, cch);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(obj != 0);
	ObjPropRec oprKey(obj, tag);
	StrUni stu(prgch, cch);
	m_hmoprstu.Insert(oprKey, stu, true); // allow overwrites

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#CacheUnknown}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheUnknown(HVO hvo, PropTag tag, IUnknown * punk)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(punk);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
//	if (!punk)	// No, nulls are a reasonable value!
//		ThrowHr(WarnHr(E_POINTER));
	ObjPropRec oprKey(hvo, tag);
	// Note: for now it would be valid to delete the key from the hash instead
	// of storing a NULL, but we are moving in the direction of wanting to know
	// explicitly what properties are cached, even if null.
	m_hmoprunk.Insert(oprKey, punk, true); // allow overwrites
	//_RPT4(_CRT_WARN, "m_hmoprunk.Insert: hvo=%x, tag=%x, punk=%x, new size: %d\n", hvo, tag, punk, m_hmoprunk.Size());

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


//:>********************************************************************************************
//:>	Methods used to retrieve object information.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#ObjectProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_ObjectProp(HVO hvo, PropTag tag, HVO * phvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);

	ObjPropRec oprKey(hvo, tag);
	if (m_hmoprobj.Retrieve(oprKey, phvo))
		return S_OK;
	switch(TryVirtual(hvo, tag))
	{
	case kvhrNotVirtual:
		return S_FALSE; // Not in cache, nor virtual, but may just be empty or not loaded.
	case kvhrUseAndRemove:
		if (m_hmoprobj.Retrieve(oprKey, phvo)) // Whatever's in the cache is it (0 if not found)
			m_hmoprobj.Delete(oprKey); // But if it is found we have to remove it.
		return S_OK;
	case kvhrUse:
		m_hmoprobj.Retrieve(oprKey, phvo); // Whatever is now in the cache is it (0 if not found)
		return S_OK;
	}
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	This method is an internal version of get_ObjectProp. In this class, there is no
	important difference. However, in subclasses which know about a database and attempt to
	retrieve a property value, before calling the database a check should be made that the
	property is of the right type. Otherwise, just answer zero.
----------------------------------------------------------------------------------------------*/
HVO VwCacheDa::GetObjPropCheckType(HVO hvo, PropTag tag)
{
	ObjPropRec oprKey(hvo, tag);
	HVO hvoT;
	if (m_hmoprobj.Retrieve(oprKey, &hvoT))
		return hvoT;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#VecItem}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_VecItem(HVO hvo, PropTag tag, int index, HVO * phvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);

	ObjPropRec oprKey(hvo, tag);
	ObjSeq os;
	if (!m_hmoprsobj.Retrieve(oprKey, &os))
	{
		switch(TryVirtual(hvo, tag))
		{
		case kvhrNotVirtual:
			return E_FAIL;
		case kvhrUseAndRemove:
			if (!m_hmoprsobj.Retrieve(oprKey, &os))
				return E_INVALIDARG; // empty virtual property, can't retrieve indexth item.
			if ((uint) index >= (uint) (os.m_cobj)) // Have to repeat this to delete after.
				ThrowInternalError(E_INVALIDARG);
			*phvo = os.m_prghvo[index];
			ClearObjSeq(os);
			m_hmoprsobj.Delete(oprKey); // This is really inefficient, hope doesn't happen much!
			return S_OK;
		case kvhrUse:
			m_hmoprsobj.Retrieve(oprKey, &os);	// should be in cache now.
			break; // Carry on as if we'd found in the first place.
		}
	}
	if ((uint) index >= (uint) (os.m_cobj))
		ThrowInternalError(E_INVALIDARG);
	*phvo = os.m_prghvo[index];

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#VecSize}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_VecSize(HVO hvo, PropTag tag, int * pchvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pchvo);

	ObjPropRec oprKey(hvo, tag);
	ObjSeq os;
	if (!m_hmoprsobj.Retrieve(oprKey, &os))
	{
		switch(TryVirtual(hvo, tag))
		{
		case kvhrNotVirtual:
			return S_FALSE; // treat as empty vector
		case kvhrUseAndRemove:
			if (!m_hmoprsobj.Retrieve(oprKey, &os))
				return S_OK; // empty virtual property, length 0 (but treat as present).
			*pchvo = os.m_cobj;
			ClearObjSeq(os);
			m_hmoprsobj.Delete(oprKey); // This is really inefficient, hope doesn't happen much!
			return S_OK;
		case kvhrUse:
			m_hmoprsobj.Retrieve(oprKey, &os);	// should be in cache now.
			break; // Carry on as if we'd found in the first place.
		}
	}
	*pchvo = os.m_cobj;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}
/*----------------------------------------------------------------------------------------------
	 ${ISilDataAccess#get_VecSizeAssumeCached}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_VecSizeAssumeCached(HVO hvo, PropTag tag, int * pchvo)
{
	// Don't want the overridden version in vwoledbda, which tries to load the cache if missing
	return VwCacheDa::get_VecSize(hvo, tag, pchvo);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#VecProp}; Get the full contents of the specified sequence in one go.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::VecProp(HVO hvo, PropTag tag, int chvoMax, int * pchvo, HVO * prghvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pchvo);
	ChkComArrayArg(prghvo, chvoMax);
	ObjPropRec oprKey(hvo, tag);
	ObjSeq os;
	HvoVec vhvoItems;
	if (!m_hmoprsobj.Retrieve(oprKey, &os))
	{
		switch(TryVirtual(hvo, tag))
		{
		case kvhrNotVirtual:
			return S_FALSE; // Not in cache, nor virtual, but may just be empty or not loaded.
		case kvhrUseAndRemove:
			if (!m_hmoprsobj.Retrieve(oprKey, &os))
				return S_OK; // empty virtual property, just leave size zero.
			if (os.m_cobj > chvoMax)
				return E_INVALIDARG;
			CopyItems(os.m_prghvo, prghvo, os.m_cobj);
			*pchvo = os.m_cobj;
			ClearObjSeq(os);
			m_hmoprsobj.Delete(oprKey); // This is really inefficient, hope doesn't happen much!
			return S_OK;
		case kvhrUse:
			break; // Carry on as if we'd found in the first place.
		}
	}
	if (os.m_cobj > chvoMax)
		return E_INVALIDARG;
	CopyItems(os.m_prghvo, prghvo, os.m_cobj);
	*pchvo = os.m_cobj;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#get_IsPropInCache}; See if prop has been cached.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_IsPropInCache(HVO hvo, PropTag tag, int cpt, int ws,
	ComBool * pfCached)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfCached);
	ObjPropRec oprKey(hvo, tag);
	switch(cpt)
	{
	case kcptBoolean: // ??
	case kcptNumeric: // ??
	case kcptInteger:
		{
			int n;
			*pfCached = m_hmoprn.Retrieve(oprKey, &n);
		}
		break;
	case kcptFloat: // Never cached so far
		break;
	case kcptTime:
		{
			int64 lln;
			*pfCached = m_hmoprlln.Retrieve(oprKey, &lln);
		}
		break;
	case kcptGuid:
		{
			GUID uid;
			*pfCached = m_hmoprguid.Retrieve(oprKey, &uid);
		}
		break;
	case kcptImage: // never cached so far
	case kcptGenDate: // ??
		break;
	case kcptBinary:
		{
			StrAnsi sta;
			*pfCached = m_hmoprsta.Retrieve(oprKey, &sta);
		}
		break;

	case kcptMultiUnicode:
	case kcptMultiString:
	case kcptMultiBigString:
	case kcptMultiBigUnicode:
		{
			ObjPropEncRec opreKey(hvo, tag, ws);
			ITsStringPtr qtss;
			*pfCached = m_hmopertss.Retrieve(opreKey, qtss);
		}
		break;
	case kcptString:
	case kcptBigString:
		{
			ITsStringPtr qtss;
			*pfCached = m_hmoprtss.Retrieve(oprKey, qtss);
		}
		break;
	case kcptUnicode:
	case kcptBigUnicode:
		{
			StrUni stu;
			*pfCached = m_hmoprstu.Retrieve(oprKey, &stu);
		}
		break;
	case kcptOwningAtom:
	case kcptReferenceAtom:
		{
			HVO hvoVal;
			*pfCached = m_hmoprobj.Retrieve(oprKey, &hvoVal);
		}
		break;
	case kcptOwningCollection:
	case kcptReferenceCollection:
	case kcptOwningSequence:
	case kcptReferenceSequence:
		{
			ObjSeq os;
			HvoVec vhvoItems;
			*pfCached = m_hmoprsobj.Retrieve(oprKey, &os);
		}
		break;

	default:
		ThrowInternalError(E_INVALIDARG, L"Not a valid property type");
	}
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

//:>********************************************************************************************
//:>	Methods used to retrieve object PROPERTY information from the cache (except references).
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BinaryPropRgb}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::BinaryPropRgb(HVO obj, PropTag tag, byte * prgb, int cbMax, int * pcb)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcb);
	ChkComArrayArg(prgb, cbMax);

	ObjPropRec oprKey(obj, tag);
	StrAnsi sta;
	if (m_hmoprsta.Retrieve(oprKey, &sta))
	{
		*pcb = sta.Length();
		if (!cbMax)
			return S_OK;
		if (cbMax < sta.Length())
			return E_FAIL;
		::memcpy(prgb, sta.Chars(), sta.Length() * isizeof(byte));
		return S_OK;
	}
	*pcb = 0;
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GuidProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_GuidProp(HVO hvo, PropTag tag, GUID * puid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(puid);

	ObjPropRec oprKey(hvo, tag);
	if (m_hmoprguid.Retrieve(oprKey, puid))
		return S_OK;
	*puid = GUID_NULL;
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#ObjFromGuid}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_ObjFromGuid(GUID uid, HVO * pHvo)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pHvo);

	if (m_hmoguidobj.Retrieve(uid, pHvo))
		return S_OK;
	*pHvo = 0;
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#Int64Prop}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_Int64Prop(HVO hvo, PropTag tag, int64 * plln)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(plln);

	ObjPropRec oprKey(hvo, tag);
	if (m_hmoprlln.Retrieve(oprKey, plln))
		return S_OK;
	switch(TryVirtual(hvo, tag))
	{
	case kvhrNotVirtual:
		return S_FALSE; // Not in cache, nor virtual, but may just be empty or not loaded.
	case kvhrUseAndRemove:
		if (m_hmoprlln.Retrieve(oprKey, plln)) // Whatever's in the cache is it (0 if not found)
			m_hmoprlln.Delete(oprKey); // But if it is found we have to remove it.
		return S_OK;
	case kvhrUse:
		m_hmoprlln.Retrieve(oprKey, plln); // Whatever is now in the cache is it (0 if not found)
		return S_OK;
	}

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#IntProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_IntProp(HVO hvo, PropTag tag, int * pn)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pn);

	ObjPropRec oprKey(hvo, tag);
	if (m_hmoprn.Retrieve(oprKey, pn))
		return S_OK; // Got it!
	switch(TryVirtual(hvo, tag))
	{
	case kvhrNotVirtual:
		return S_FALSE; // Not in cache, nor virtual, but may just be empty or not loaded.
	case kvhrUseAndRemove:
		if (m_hmoprn.Retrieve(oprKey, pn)) // Whatever's in the cache is it (0 if not found)
			m_hmoprn.Delete(oprKey); // But if it is found we have to remove it.
		return S_OK;
	case kvhrUse:
		m_hmoprn.Retrieve(oprKey, pn); // Whatever is now in the cache is it (0 if not found)
		return S_OK;
	}
	return S_FALSE; // Neither in cache nor virtual.

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BooleanProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_BooleanProp(HVO hvo, PropTag tag, ComBool * pn)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pn);

	int nVal = 0;
	HRESULT hr = get_IntProp(hvo, tag, &nVal);
	*pn = (nVal != 0);

	return hr;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#MultiStringAlt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_MultiStringAlt(HVO hvo, PropTag tag, int ws, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pptss);

	ObjPropEncRec opreKey(hvo, tag, ws);
	ITsStringPtr qtss;
	HRESULT hrRet = S_OK;
	if (!m_hmopertss.Retrieve(opreKey, qtss))
	{
		switch(TryVirtual(hvo, tag, ws))
		{
		case kvhrNotVirtual:
			hrRet = S_FALSE; // Return value marks it as missing, non-virtual.
			break;
		case kvhrUseAndRemove:
			if (m_hmopertss.Retrieve(opreKey, qtss)) // Whatever's in the cache is it
				m_hmopertss.Delete(opreKey); // But if it is found we have to remove it.
			break;
		case kvhrUse:
			m_hmopertss.Retrieve(opreKey, qtss); // Whatever is now in the cache is it
			break;
		}
	}
	if (qtss)
	{
		*pptss = qtss.Detach();
	}
	else
	{
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		CheckHr(qtsf->EmptyString(ws, pptss));
		return hrRet;
	}

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#StringProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_StringProp(HVO hvo, PropTag tag, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);

	ObjPropRec oprKey(hvo, tag);
	// Try to retrieve a string from the cache, or, if there is nothing in the cache,
	// by treating it as a virtual property.
	ITsStringPtr qtss;
	HRESULT hrRet = S_OK; // default, unless neither in cache nor virtual.
	if (!m_hmoprtss.Retrieve(oprKey, qtss))
	{
		switch(TryVirtual(hvo, tag))
		{
		case kvhrNotVirtual:
			hrRet = S_FALSE; // Return value marks it as missing, non-virtual.
			break;
		case kvhrUseAndRemove:
			if (m_hmoprtss.Retrieve(oprKey, qtss)) // Whatever's in the cache is it
				m_hmoprtss.Delete(oprKey); // But if it is found we have to remove it.
			break;
		case kvhrUse:
			m_hmoprtss.Retrieve(oprKey, qtss); // Whatever is now in the cache is it
			break;
		}
	}
	// JohnT: it seems unreasonable for qtss to be retrieved as null. We definitely don't want
	// this routine to return null. However, CacheStringProp allows the input argument to
	// be null, so we adjust for it here. We may at some point want to change not to allow
	// nulls at all, in which case we could fix this. Basic idea is that if any of the above
	// paths produced a null, we now make an empty string.
	if (qtss)
	{
		*pptss = qtss.Detach();
		return S_OK;
	}
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	int wsUser;
	AssertPtr(m_qwsf);
	CheckHr(m_qwsf->get_UserWs(&wsUser));
	CheckHr(qtsf->EmptyString(wsUser, pptss));
	return hrRet;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#Prop}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_Prop(HVO hvo, PropTag tag, VARIANT * pvar)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvar);

	ITsStringPtr qtss;
	ObjPropRec oprKey(hvo, tag);

	int64 tim;
	int n;
	pvar->vt = VT_EMPTY; // default
	if (m_hmoprlln.Retrieve(oprKey, &tim))
	{
		// property is a time (or other int64).
		// pvar->vt = VT_CY;
		pvar->vt = VT_I8; // availabe now, fits our use better then VT_CY
		pvar->cyVal.int64 = tim;
		return S_OK;
	}
	else if (m_hmoprn.Retrieve(oprKey, &n))
	{
		// property is an int
		pvar->vt = VT_I4;
		pvar->lVal = n;
		return S_OK;
	}
	else if (m_hmoprtss.Retrieve(oprKey, qtss))
	{
		pvar->vt = VT_UNKNOWN;
		pvar->punkVal = qtss.Detach();
		return S_OK;
	}
	return S_FALSE; // Not a catastrophic error, but variant is empty.

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#TimeProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_TimeProp(HVO hvo, PropTag tag, int64 * ptim)
{
	// Must be explicit about which get_Int64Prop: don't want the VwOleDbDa override,
	// for example.
	return VwCacheDa::get_Int64Prop(hvo, tag, ptim);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#UnknownProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_UnknownProp(HVO hvo, PropTag tag, IUnknown ** ppunk)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppunk);

	ObjPropRec oprKey(hvo, tag);
	*ppunk = NULL;
	IUnknownPtr qunk;
	if (m_hmoprunk.Retrieve(oprKey, qunk) && qunk)
	{
		*ppunk = qunk.Detach();
		return S_OK;
	}
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	DO NOT USE THIS METHOD OUTSIDE THIS CLASS.  Subclasses in the same DLL can get it directly
	(and much more efficiently)	as an StrUni.
----------------------------------------------------------------------------------------------*/
bool VwCacheDa::UnicodeProp(HVO obj, PropTag tag, StrUni & stu)
{
	ObjPropRec oprKey(obj, tag);
	return m_hmoprstu.Retrieve(oprKey, &stu);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#UnicodeProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_UnicodeProp(HVO hvo, PropTag tag, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	ObjPropRec oprKey(hvo, tag);
	*pbstr = NULL;
	StrUni stu;
	if (m_hmoprstu.Retrieve(oprKey, &stu))
	{
		stu.GetBstr(pbstr);
		return S_OK;
	}
	switch(TryVirtual(hvo, tag))
	{
	case kvhrNotVirtual:
		return S_FALSE; // Return value marks it as missing, non-virtual.
		break;
	case kvhrUseAndRemove:
		if (m_hmoprstu.Retrieve(oprKey, &stu)) // Whatever's in the cache is it
			m_hmoprstu.Delete(oprKey); // But if it is found we have to remove it.
		break;
	case kvhrUse:
		m_hmoprstu.Retrieve(oprKey, &stu); // Whatever is now in the cache is it
		break;
	}
	stu.GetBstr(pbstr);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#UnicodeProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::put_UnicodeProp(HVO obj, PropTag tag, BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	return SetUnicode(obj, tag, (OLECHAR *)bstr, BstrLen(bstr));

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#UnicodePropRgch}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::UnicodePropRgch(HVO obj, PropTag tag, OLECHAR * prgch, int cchMax,
	int * pcch)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgch, cchMax);
	ChkComOutPtr(pcch);

	ObjPropRec oprKey(obj, tag);
	StrUni stu;
	if (m_hmoprstu.Retrieve(oprKey, &stu))
	{
		*pcch = stu.Length();
		if (!cchMax)
			return S_OK;
		if (cchMax < stu.Length())
			return E_FAIL;
		::memset(prgch, 0, cchMax * isizeof(OLECHAR));
		::memcpy(prgch, stu.Chars(), stu.Length() * isizeof(OLECHAR));
		return S_OK;
	}
	*pcch = 0;
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}




//:>********************************************************************************************
//:>	Methods to manage the undo/redo mechanism.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BeginUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::BeginUndoTask(BSTR bstrUndo, BSTR bstrRedo)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrUndo);
	ChkComBstrArgN(bstrRedo);

	return S_OK;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#EndUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::EndUndoTask()
{
	BEGIN_COM_METHOD;
	return S_OK;
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#ContinueUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::ContinueUndoTask()
{
	BEGIN_COM_METHOD;
	return S_OK;
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#EndOuterUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::EndOuterUndoTask()
{
	BEGIN_COM_METHOD;
	return S_OK;
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BreakUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::BreakUndoTask(BSTR bstrUndo, BSTR bstrRedo)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrUndo);
	ChkComBstrArgN(bstrRedo);

	return S_OK;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#Rollback}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::Rollback()
{
	BEGIN_COM_METHOD;
	return S_OK;
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GetActionHandler}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::GetActionHandler(IActionHandler ** ppacth)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppacth);

	*ppacth = NULL;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetActionHandler}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetActionHandler(IActionHandler * pacth)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pacth);

	return S_OK;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


//:>********************************************************************************************
//:>	Methods used to create new objects, delete existing objects, or a combination of both
//:>	of these actions (in the case of MoveOwnSeq).  These are the only methods that actually
//:>	change the OWNERSHIP RELATIONSHIPS of objects.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#DeleteObj}

	Delete the specified object.
	For the moment we just record that it is deleted. This is enough to handle things like
	deleting paragraphs of structured text, where we just need to make sure we don't try
	to write out its basic properties if it has been deleted.
	ENHANCE JohnT: eventually, we need to consider implications of deleting things that particpate
	in reference relationships. Ideally, the client code has ensured that the object is
	removed from all such relationships (except other deleted objects) before deleting the
	object itself. But what if we have a property in the cache that includes a backreference
	to it? Or if there were references, but the user said "go ahead and delete"? How do we
	ensure the cache gets cleaned up? Maybe such deletions require another method that checks
	every property in the cache?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::DeleteObj(HVO hvoObj)
{
	BEGIN_COM_METHOD;

	m_shvoDeleted.Insert(hvoObj);
	InformNowDirty();
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#DeleteObjOwner}

	More often use this. This will clean up the owning property, assuming you
	have the arguments right. Pass ihvo = -2 for atomic properties.
	For collections or sequences, if you know the position, pass it; otherwise, pass -1
	If the owning property is not cached at all, the object is simply deleted.
	This is not an error.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::DeleteObjOwner(HVO hvoOwner, HVO hvoObj, PropTag tag, int ihvo)
{
	BEGIN_COM_METHOD;

	m_shvoDeleted.Insert(hvoObj); // causes the actual deletion, on next save
	DeleteObjOwnerCore(hvoOwner, hvoObj, tag, ihvo);
	if (ihvo != -2)
		InformNowDirty();

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#InsertNew}

	Typically used when splitting the paragraph at ihvo. New objects are inserted
	after the one at ihvo.
	The new objects should generally be similar to the one at ihvo, except that
	the main text property that forms the paragraph body should be empty.
	If the object has a paragraph style property, the new objects should have
	the same style as the one at ihvo, except that, if a stylesheet is passed,
	each successive paragraph inserted should have the appropriate next style
	for the one named in the previous paragraph.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::InsertNew(HVO hvoObj, PropTag tag, int ihvo, int chvo,
	IVwStylesheet * pss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pss);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvoObj != 0);
	if (ihvo < 0)
		ThrowInternalError(E_INVALIDARG);

	HVO * prghvoNew;

	try
	{
		// For now we only know how to do this trick for one object type.
		if (tag != kflidStText_Paragraphs)
			return E_NOTIMPL;
		HVO hvoBase;
		CheckHr(get_VecItem(hvoObj, tag, ihvo, &hvoBase));
		ITsTextPropsPtr qttp;
		IUnknownPtr qunkTtp;
		CheckHr(get_UnknownProp(hvoBase, kflidStPara_StyleRules, &qunkTtp));
		if (qunkTtp)
			CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **)&qttp));
		prghvoNew = NewObj HVO[chvo];
		// Assign IDs for the new objects.
		for (int i2 = 0; i2 < chvo; i2++)
		{
			MakeNewObject(kclidStTxtPara, hvoObj, tag, ihvo + i2 + 1, &(prghvoNew[i2]));
		}
		for (int i = 0; i < chvo; i++)
		{
			if (pss)
			{
				SmartBstr sbstr;
				if (qttp)
				{
					CheckHr(qttp->GetStrPropValue(kspNamedStyle, &sbstr));
					SmartBstr sbstrNew;
					CheckHr(pss->GetNextStyle(sbstr, &sbstrNew));
					if (sbstrNew != sbstr && sbstrNew.Length())
					{
						ITsPropsBldrPtr qtpb;
						CheckHr(qttp->GetBldr(&qtpb));
						CheckHr(qtpb->SetStrPropValue(kspNamedStyle, sbstrNew));
						CheckHr(qtpb->GetTextProps(&qttp));
					}
					// else if empty string, default to same style
				}
			}
			if (qttp)
			{
#ifdef JohnT_10_9_2001_NeedToAvoidDuplicateStartAt
				// We enhanced other code so that all paragraphs in a sequence can have the same
				// "start at" value and only the first is used.
				// Now, the following code is a nuisance, because it interferes with paragraph
				// borders recognizing that all the paragraphs have the same properties.

				// Make sure that the new paragraph does not have a "start at" numbering
				// property, which it may have copied from the previous paragraph, and which
				// would result in a numbered list with duplicated numbers.
				ITsPropsBldrPtr qtpb;
				CheckHr(qttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptBulNumStartAt, -1, -1));
				CheckHr(qtpb->GetTextProps(&qttp));
#endif

				CheckHr(SetUnknown(prghvoNew[i], kflidStPara_StyleRules, qttp));
			}
		}
		if (prghvoNew)
			delete[] prghvoNew;
	}
	catch (Throwable & thr)
	{
		if (prghvoNew)
			delete[] prghvoNew;
		return thr.Error();
	}
	catch (...)
	{
		if (prghvoNew)
			delete[] prghvoNew;
		ThrowHr(WarnHr(E_FAIL));
	}

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#MakeNewObject}

	Make a new object owned in a particular position. The object is created immediately.
	(Actually in the database, in database implementations; this will roll back if changes
	are not saved.)
	If ord is >= 0, the object is inserted in the appropriate place in the (presumed sequence)
	property, both in the database itself and in the data access object's internal cache, if
	that property is cached.
	If ord is < 0, it is entered as a null into the database, which is appropriate for
	collection and atomic properties.
	Specifically, use -2 for an atomic property, and -1 for a collection; this will ensure
	that the cache is updated. You may use -3 if you know the property is not currently cached.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::MakeNewObject(int clid, HVO hvoOwner, PropTag tag, int ord,
	HVO * phvoNew)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(phvoNew);

	NewObject(clid, hvoOwner, tag, ord, phvoNew);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	This method does all the real work of moving an object from one owner to another. Called
	by MoveOwnSeq and MoveOwn.
----------------------------------------------------------------------------------------------*/

void VwCacheDa::MoveOwnedObject(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart,
								int ihvoEnd, HVO hvoDstOwner, PropTag tagDst,
								int ihvoDstStart, HVO* prghvo, int cobj)
{
	int iSrcType;
	m_qmdc->GetFieldType(tagSrc, &iSrcType);
	int iDstType;
	m_qmdc->GetFieldType(tagDst, &iDstType);

	int chvoMoved = ihvoEnd - ihvoStart + 1;

	if (iSrcType != kcptOwningSequence && hvoSrcOwner == hvoDstOwner && tagSrc == tagDst)
	{
		ThrowInternalError(E_INVALIDARG);
	}

	//  Make sure the given parameter's values are OK.
	if (ihvoStart < 0 || ihvoStart > ihvoEnd || (iDstType == kcptOwningAtom && chvoMoved > 1))
	{
		ThrowInternalError(E_INVALIDARG);
	}

	if ((ihvoStart > cobj) || (ihvoEnd > cobj))
	{
		ThrowInternalError(E_INVALIDARG);
	}

	ObjPropRec oprSrcKey(hvoSrcOwner, tagSrc);
	//  Create a new ObjSeq record and copy the records over appropriately.
	if (hvoSrcOwner == hvoDstOwner && tagSrc == tagDst)
	{
		// We are moving items in the same vector.
		ObjSeq osRep;

		osRep.m_cobj = cobj;
		osRep.m_prghvo = NewObj HVO[osRep.m_cobj];
		if (ihvoDstStart < ihvoStart)
		{
			// Move a sequence up higher.
			// A B C D E -> A C D B E
			// where CD is moved before the target B

			// First copy items before the target (A).
			MoveItems(prghvo, osRep.m_prghvo, ihvoDstStart);
			int ihvo = ihvoDstStart;
			// Copy the extracted sequence (CD)
			MoveItems(prghvo + ihvoStart, osRep.m_prghvo + ihvo,
				chvoMoved);
			ihvo += chvoMoved;
			// Copy items between the target and the extracted sequence (B).
			MoveItems(prghvo + ihvoDstStart, osRep.m_prghvo + ihvo,
				ihvoStart - ihvoDstStart);
			ihvo += ihvoStart - ihvoDstStart;
			// Copy items after the extracted sequence (E).
			MoveItems(prghvo + ihvoEnd + 1, osRep.m_prghvo + ihvo,
				cobj - ihvoEnd - 1);
		}
		else
		{
			// Move a sequence down lower.
			// A B C D E -> A D B C E
			// where BC is moved before the target E

			// First copy items before the extracted sequence (A).
			MoveItems(prghvo, osRep.m_prghvo, ihvoStart);
			int ihvo = ihvoStart;
			// Copy items between the extracted sequence and the target (D).
			MoveItems(prghvo + ihvoEnd + 1, osRep.m_prghvo + ihvo,
				ihvoDstStart - ihvoEnd - 1);
			ihvo += ihvoDstStart - ihvoEnd - 1;
			// Copy the extracted sequence (BC)
			MoveItems(prghvo + ihvoStart, osRep.m_prghvo + ihvo,
				chvoMoved);
			ihvo += chvoMoved;
			// Copy items from the target to the end of the vector (E).
			MoveItems(prghvo + ihvoDstStart, osRep.m_prghvo + ihvo,
				cobj - ihvoDstStart);
		}

		//  Delete the old ObjSeq record and replace it with the new one.
		m_hmoprsobj.Insert(oprSrcKey, osRep, true);
	}
	else
	{
		// We may have owners or flids stored in the cache. So we need to make sure any
		// of these that have changed are updated appropriately.
		for (int ihvo = ihvoStart; ihvo <= ihvoEnd; ++ihvo)
		{
			HVO hvo = prghvo[ihvo];
			if (hvoSrcOwner != hvoDstOwner)
			{
				ObjPropRec opr(hvo, kflidCmObject_Owner);
				HVO hvoOwn;
				if (m_hmoprobj.Retrieve(opr, &hvoOwn))
					m_hmoprobj.Insert(opr, hvoDstOwner, true);
			}
			if (tagSrc != tagDst)
			{
				ObjPropRec opr(hvo, kflidCmObject_OwnFlid);
				int flid;
				if (m_hmoprn.Retrieve(opr, &flid))
					m_hmoprn.Insert(opr, tagDst, true);
			}
		}

		ObjPropRec oprDstKey(hvoDstOwner, tagDst);
		if (iDstType == kcptOwningAtom)
		{
			m_hmoprobj.Insert(oprDstKey, prghvo[ihvoStart], true);
		}
		else
		{
			ObjSeq osDst;
			ObjSeq osRep;
			if (m_hmoprsobj.Retrieve(oprDstKey, &osDst))
			{
				//  If there already exists a destination ObjSeq, create a new one and
				//  copy values from one to the other.
				osRep.m_cobj = osDst.m_cobj + chvoMoved;
				osRep.m_prghvo = NewObj HVO[osRep.m_cobj];
				if (ihvoDstStart > 0)
				{
					//  Copy sequences before the insertion point.
					MoveItems(osDst.m_prghvo, osRep.m_prghvo, ihvoDstStart);
				}
				//  Copy the moved sequences.
				MoveItems(prghvo + ihvoStart, osRep.m_prghvo + ihvoDstStart,
					chvoMoved);
				//  Copy the sequences after (and including) the insertion point.
				if (osDst.m_cobj > ihvoDstStart)
				{
					MoveItems(osDst.m_prghvo + ihvoDstStart,
						osRep.m_prghvo + ihvoDstStart + (chvoMoved),
						osDst.m_cobj - ihvoDstStart);
				}
			}
			else
			{
				//  If a sequence does not exist, create a new ObjSeq record for it.
				osRep.m_cobj = chvoMoved;
				osRep.m_prghvo = NewObj HVO[chvoMoved];
				MoveItems(prghvo + ihvoStart, osRep.m_prghvo, chvoMoved);
			}

			//  Delete the old ObjSeq record and replace it with the new one.
			if (osDst.m_prghvo)
			{
				delete[] osDst.m_prghvo;
			}
			m_hmoprsobj.Insert(oprDstKey, osRep, true);
		}

		if (iSrcType == kcptOwningAtom)
		{
			m_hmoprobj.Delete(oprSrcKey);
		}
		else
		{
			if (cobj > chvoMoved)
			{
				//  Close up the gap left in the Src sequence (ie. replace it with a new one)
				ObjSeq osRep2;
				osRep2.m_cobj = cobj - (chvoMoved);
				osRep2.m_prghvo = NewObj HVO[osRep2.m_cobj];
				if (ihvoStart > 0)
				{
					//  Copy sequences before the extracted sequences.
					MoveItems(prghvo, osRep2.m_prghvo, ihvoStart);
				}
				if (ihvoEnd < cobj - 1)
				{
					//  Copy sequences after the extracted sequences.
					MoveItems(prghvo + ihvoEnd + 1, osRep2.m_prghvo + ihvoStart,
						cobj - ihvoEnd - 1);
				}

				//  Delete the old Dst ObjSeq record and replace it with the new one.
				m_hmoprsobj.Insert(oprSrcKey, osRep2, true);
			}
			else
			{
				//  If all the objects were removed from the Src ObjSeq, simply delete it.
				m_hmoprsobj.Delete(oprSrcKey);
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#MoveOwnSeq}

  This method inserts the objects delimited by the indexes ihvoStart and ihvoEnd in another
  owning sequence (or in the same owning sequence at a different position).  Thus, the "ord"
  value of the objects is changed accordingly (starting with the "ord" value of ihvoDst) as
  well as the "OwnOrd$" column (if a new owning object is specified).

  All objects selected by ihvoStart and ihvoEnd will be inserted in the sequence BEFORE the
  position of the object found at ihvoDstStart (which is in the destination owning sequence).

  To append the (ihvoStart to ihvoEnd) objects to the end of the sequence (owned by hvoDstOwner),
  specify an ihvoDstStart greater than the highest value in that sequence.  eg.  if there are
  4 ownSeq objects already in hvoDstOwner (0,1,2,3) set ihvoDstStart = 4 (or higher).

  When moving an owner, we also need to update the objects owner and flid in the cache where
  needed. This method makes these changes as well.

  NOTE!  While the "ord" values of sequences in the database are guaranteed to be sequential,
  they are NOT guaranteed to be contiguous.  That is, there may be gaps in the numbering.
  (eg. 0, 1, 4, 6, 23 rather than 0, 1, 2, 3, 4).  The elements in the ObjSeq record in the
  VwCacheDa database cache, however, IS guaranteed to be both sequential and contiguous.  That
  is, if there are 5 elements in a sequence, there will be only 5 elements in the array and they
  will be indexed 0, 1, 2, 3, 4.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::MoveOwnSeq(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart,
	int ihvoEnd, HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart)
{
	BEGIN_COM_METHOD;

	Assert(hvoSrcOwner != 0);
	Assert(hvoDstOwner != 0);

	//  If the source ObjSeq record is not in the hash map (of the cache) already,
	//  return an error.
	ObjSeq osSrc;
	ObjPropRec oprSrcKey(hvoSrcOwner, tagSrc);
	if (!m_hmoprsobj.Retrieve(oprSrcKey, &osSrc))
	{
		ThrowInternalError(E_FAIL);
	}

	MoveOwnedObject(hvoSrcOwner, tagSrc, ihvoStart, ihvoEnd, hvoDstOwner,
		tagDst, ihvoDstStart, osSrc.m_prghvo, osSrc.m_cobj);
	delete[] osSrc.m_prghvo;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#MoveOwn}

  This method moves an object from one owner to another. The source and destination can be of
  any type. If the destination is a sequence, one can specifiy the location to insert the
  object. The object is inserted in the destination sequence before the object located at
  ihvoDstStart.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::MoveOwn(HVO hvoSrcOwner, PropTag tagSrc, HVO hvo, HVO hvoDstOwner,
								PropTag tagDst, int ihvoDstStart)
{
	BEGIN_COM_METHOD;

	Assert(hvoSrcOwner != 0);
	Assert(hvoDstOwner != 0);

	int iSrcType;
	m_qmdc->GetFieldType(tagSrc, &iSrcType);

	switch (iSrcType)
	{
	case kcptOwningCollection:
	case kcptOwningSequence:
	{
		//  If the source ObjSeq record is not in the hash map (of the cache) already,
		//  return an error.
		ObjSeq osSrc;
		ObjPropRec oprSrcKey(hvoSrcOwner, tagSrc);
		if (!m_hmoprsobj.Retrieve(oprSrcKey, &osSrc))
		{
			ThrowInternalError(E_FAIL);
		}
		// retrieve the index of the HVO in the vector
		int ihvo = 0;
		for (; ihvo < osSrc.m_cobj; ihvo++)
		{
			if (osSrc.m_prghvo[ihvo] == hvo)
				break;
		}
		// if the HVO is not found, return an error
		if (ihvo == osSrc.m_cobj)
		{
			ThrowInternalError(E_INVALIDARG);
		}
		MoveOwnedObject(hvoSrcOwner, tagSrc, ihvo, ihvo, hvoDstOwner,
			tagDst, ihvoDstStart, osSrc.m_prghvo, osSrc.m_cobj);
		delete[] osSrc.m_prghvo;
		break;
	}

	case kcptOwningAtom:
		MoveOwnedObject(hvoSrcOwner, tagSrc, 0, 0, hvoDstOwner, tagDst, ihvoDstStart, &hvo, 1);
	}

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	Create a new object of the specified type.
	Most subclasses should override to do whatever is needed to get a real new object.
	If ord is -2 it is an atomic property, and -1 for a collection; this will ensure
	that the cache is updated. You may use -3 if you know the property is not currently cached.

	NOTE: No longer an interface method. Use ISilDataAccess::MakeNewObject instead.
----------------------------------------------------------------------------------------------*/
void VwCacheDa::NewObject(int clid, HVO hvoOwner, PropTag tag, int ord,
	HVO * phvoNew)
{
	ChkComOutPtr(phvoNew);

	*phvoNew = m_hvoNext++;
	DoInsert(hvoOwner, *phvoNew, tag, ord);
}


/*----------------------------------------------------------------------------------------------
  Update the caches for a MakeNewObject call. Allows this functionality to be shared by callers.
----------------------------------------------------------------------------------------------*/
void VwCacheDa::DoInsert(HVO hvoOwner, HVO hvoNew, PropTag tag, int ord)
{
	ObjPropRec oprKey(hvoOwner, tag);
	if (ord >= -1)
	{
		// Insert into sequence or collection: see if we have it
		ObjSeq osOld;
		osOld.m_cobj = 0;
		// If ord is zero or -1, we might be inserting the very first thing into a prop,
		// and therefore not have anything recorded although we read the property. Safest is
		// to put it in, in this case.
		// (This could obviously produce an invalid value for that property, containing
		// just the one thing we inserted instead of the full value, if we have not
		// yet loaded it. But, if we really want the property, we should load it.
		// We can do this better when we know for sure whether a property has been loaded
		// into the cache.
		if (ord > 0 && !m_hmoprsobj.Retrieve(oprKey, &osOld))
			return;
		if (ord < 0)
			ord = osOld.m_cobj;
		CheckHr(ReplaceAux(hvoOwner, tag, ord, ord, &hvoNew, 1));
		m_soprMods.Insert(oprKey);
		InformNowDirty();
	}
	// Do NOT check whether something is already there! If the property was null before,
	// we may not have cached it on loading from the database; it would have just been
	// a missing row.
	else if (ord == -2)
	{
		// we know about this property, update it
		CacheObjProp(hvoOwner, tag, hvoNew);
	}
	// REVIEW JohnT (TomB): I originally added a little loop to do this in ReplaceAux, but then
	// I discovered that when ord == -2, this wasn't getting done and needs to be. However, just
	// doing it here isn't enough because CacheReplace doesn't use this function. So I'm putting
	// the needed code in both ReplaceAux and CacheObjProp. I hope this will cover all the
	// needed cases and not do too much. There might be some places where CacheObjProp is being
	// used that we might not want this behavior.
//	CacheObjProp(hvoNew, kflidCmObject_Owner, hvoOwner);
//	CacheIntProp(hvoNew, kflidCmObject_OwnFlid, tag);
}



//:>********************************************************************************************
//:>	SetObjProp changes the value of an atomic REFERENCES and Replace changes the values of
//:>	collection/sequence references.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#Replace}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::Replace(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
	HVO * prghvo, int chvoIns)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prghvo, chvoIns);
	if (TryVirtualReplace(hvoObj, tag, ihvoMin, ihvoLim, prghvo, chvoIns) == kwvDone)
		return S_OK;

	ObjPropRec oprKey(hvoObj, tag);
	m_soprMods.Insert(oprKey);
	InformNowDirty();
	return ReplaceAux(hvoObj, tag, ihvoMin, ihvoLim, prghvo, chvoIns);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*------------------------------------------------------------------------------------------
	This method is used to replace items in a vector only in the cache. Since this class is
	subclassed by another class that overrides the Replace method to instantly make changes
	in a database, this provides a way to make a change to a vector in the cache that does
	not affect the database. This is useful when using dummy IDs to temporarily store a
	vector of IDs.
------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::CacheReplace(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
	HVO rghvo[], int chvo)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rghvo, chvo);
	return ReplaceAux(hvoObj, tag, ihvoMin, ihvoLim, rghvo, chvo);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	General replacement function used by various operations. Avoids the undo/redo
	mechanism (in the subclasses); caller is assumed to handle it.
----------------------------------------------------------------------------------------------*/
HRESULT VwCacheDa::ReplaceAux(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
	HVO * prghvo, int chvoIns)
{
	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvoObj != 0);

	ObjPropRec oprKey(hvoObj, tag);
	if (ihvoMin < 0 || ihvoMin > ihvoLim)
		ThrowInternalError(E_INVALIDARG);
	ObjSeq os;
	// If it is not there already the only valid replacement is 0,0
	if (!m_hmoprsobj.Retrieve(oprKey, &os))
	{
		if (ihvoLim)
			return E_FAIL;
		else
			os.m_cobj = 0; // work with empty old list
	}
	if (ihvoLim > os.m_cobj)
		ThrowInternalError(E_INVALIDARG);

	int chvo = os.m_cobj + chvoIns - (ihvoLim - ihvoMin);

	ObjSeq osRep;
	osRep.m_cobj = chvo;
	osRep.m_prghvo = NewObj HVO[chvo];
	MoveItems(os.m_prghvo, osRep.m_prghvo, ihvoMin);
	MoveItems(prghvo, osRep.m_prghvo + ihvoMin, chvoIns);
	MoveItems(os.m_prghvo + ihvoLim, osRep.m_prghvo + ihvoMin + chvoIns, os.m_cobj - ihvoLim);
	m_hmoprsobj.Insert(oprKey, osRep, true);

	// REVIEW JohnT (TomB): I think this is the right thing to do, but I'm not sure if it's
	// going too far. I made a more conservative stab at this by modifying the DoInsert
	// method. However, that didn't cover the case where CacheReplace was called. See further
	// comments in DoInsert and CacheObjProp.
	if (IsOwningField(tag))
	{
		for (int ihvo = 0; ihvo < chvoIns; ihvo++)
		{
			CacheObjProp(prghvo[ihvo], kflidCmObject_Owner, hvoObj); // hvoObj is the owner
			CacheIntProp(prghvo[ihvo], kflidCmObject_OwnFlid, tag);
		}
	}

	if (os.m_prghvo)
		delete[] os.m_prghvo;

	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetObjProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetObjProp(HVO hvo, PropTag tag, HVO hvoObj)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.  (hvoObj == 0 implies exactly this.)
	Assert(hvo != 0);
	if (TryVirtualAtomic(hvo, tag, hvoObj) == kwvDone)
		return S_OK;
	SetObjPropVal(hvo, tag, hvoObj);
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

void VwCacheDa::SetObjPropVal(HVO hvo, PropTag tag, HVO hvoObj)
{
	ObjPropRec oprKey(hvo, tag);
	m_hmoprobj.Insert(oprKey, hvoObj, true); // allow overwrites
	m_soprMods.Insert(oprKey);
	InformNowDirty();
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#RemoveObjRefs}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::RemoveObjRefs(HVO hvo)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all. (hvoObj == 0 implies exactly this.)
	Assert(hvo != 0);

//-	// Remove hvo from any atomic references (m_hmoprobj).
//-	ObjPropObjMap::iterator ito;
//-	for (ito = m_hmoprobj.Begin(); ito != m_hmoprobj.End(); ++ito)
//-	{
//-		if (ito.GetValue() == hvo)
//-		{
//-			ObjPropRec opr = ito.GetKey();
//-			m_hmoprobj.Delete(opr);
//-			CheckHr(PropChanged(NULL, kpctNotifyAll, opr.m_hvo, opr.m_tag, 0, 0, 1));
//-		}
//-	}
//-
//-	// Remove hvo from any sequence references (m_hmoprsobj).
//-	ObjPropSeqMap::iterator its;
//-	for (its = m_hmoprsobj.Begin(); its != m_hmoprsobj.End(); ++its)
//-	{
//-		ObjSeq & os = its.GetValue();
//-		for (int ihvo = os.m_cobj; --ihvo >= 0; )
//-		{
//-			if (os.m_prghvo[ihvo] == hvo)
//-			{
//-				ObjPropRec opr = its.GetKey();
//-				// JohnT: it is tempting to use ReplaceAux here, but the code as written
//-				// is more efficient, and even more important, it doesn't modify the hashmap
//-				// in any way, which may be important for the continued operation of the
//-				// iterator.
//-				MoveItems(os.m_prghvo + ihvo + 1, os.m_prghvo + ihvo, os.m_cobj - ihvo - 1);
//-				os.m_cobj--;
//-				CheckHr(PropChanged(NULL, kpctNotifyAll, opr.m_hvo, opr.m_tag, ihvo, 0, 1));
//-			}
//-		}
//-	}

	RemoveCachedProperties(hvo);

	// We don't need to modify the dirty flag since we don't need to write anything to the
	// cache.

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	Remove the given object from its owner in the cache, and then remove all of the object's
	properties from the cache.
----------------------------------------------------------------------------------------------*/
void VwCacheDa::DeleteObjOwnerCore(HVO hvoOwner, HVO hvoObj, PropTag tag, int ihvo,
	bool fClearIncomingRefs)
{
	ObjPropRec oprKey(hvoOwner, tag);
	if (ihvo == -2)
	{
		// atomic property
		m_hmoprobj.Delete(oprKey);
	}
	else
	{
		ObjSeq osOld;
		if (m_hmoprsobj.Retrieve(oprKey, &osOld))
		{
			if (ihvo < 0)
			{
				// Need to search for it
				for (ihvo = 0; ihvo < osOld.m_cobj && osOld.m_prghvo[ihvo] != hvoObj; ihvo++)
					;
			}
			// ihvo can be >= m_cobj if the object we're deleting is not in the cached
			// copy of the owning property. This can happen if the cache is out of date
			// for some reason. We just skip deleting it.
			// Enhance: would it be better to delete the property from the cache altogether,
			// and reload if it is ever wanted, since it is clearly inaccurate?
			if (ihvo < osOld.m_cobj)
				CheckHr(ReplaceAux(hvoOwner, tag, ihvo, ihvo + 1, NULL, 0));
		}
	}
	// If this has a meta-data cache (as is the case, for exampe if this is a is a VwOleDbDa object,
	// then we can be more efficient by not calling RemoveCachedProperties. Also, if the
	// caller has told us not to bother clearing incoming references to the object being deleted
	// (presumably because they already determined that there are none) the second argument
	// to ClearInfoAbout can be kciaRemoveAllObjectInfo, which is more efficient than
	// kciaRemoveObjectAndOwnedInfo.
	if (m_qmdc)
	{
		//long first = ::GetTickCount();
		CheckHr(ClearInfoAbout(hvoObj, fClearIncomingRefs ? kciaRemoveAllObjectInfo : kciaRemoveObjectAndOwnedInfo));
		//long diff = ::GetTickCount() - first;
		//StrUni stu;
		//stu.Format(L"Elapsed time: %d", diff);
		//::OutputDebugString(stu);
	}
	else
	{
		// Call ClearInfoAbout before we RemoveCachedProperties, because, in particular, it clears the
		// kflidCmObject_Class info, allowing notifiers to detect that the object is gone.
		CheckHr(ClearInfoAbout(hvoObj, kciaRemoveObjectAndOwnedInfo));
		RemoveCachedProperties(hvoObj);
	}
}


/*----------------------------------------------------------------------------------------------
	Run through the cache data looking for references to a deleted object, and removing any that
	are found.  After clearing out the cache, call PropChanged() as needed.  This includes
	deleting any objects owned by hvoDeleted.
----------------------------------------------------------------------------------------------*/
void VwCacheDa::RemoveCachedProperties(HVO hvoDeleted)
{
	if (hvoDeleted == 0)
		return;
	Set<HVO> sethvoDel;				// records objects that have been deleted.
	// records items which need a PropChanged() at the end.
	// A positive or zero index indicates we should do a PropChanged specifying that one
	// object at that index has been deleted.
	// A negative index indicates we should do a PropChanged specifying that -(val) objects
	// have been deleted and the current number inserted. This is used when more than one
	// object is deleted from the same property, to avoid the complexity and error-prone-ness
	// of trying to figure out the exact group that have changed.
	ObjPropIntMap hmoprnChg;

	RemoveCachedProperties(hvoDeleted, sethvoDel, hmoprnChg);
	ObjPropIntMap::iterator it;
	for (it = hmoprnChg.Begin(); it != hmoprnChg.End(); ++it)
	{
		ObjPropRec & opr = it.GetKey();
		if (!sethvoDel.IsMember(opr.m_hvo))
		{
			int ihvo = it.GetValue();
			if (ihvo >= 0)
				CheckHr(PropChanged(NULL, kpctNotifyAll, opr.m_hvo, opr.m_tag, ihvo, 0, 1));
			else
			{
				// negative ihvo is actually the old chvo, that is, minus the number of objects
				// we pretend to delete and replace with the current ones.
				int chvoNew;
				CheckHr(get_VecSize(opr.m_hvo, opr.m_tag, &chvoNew));
				CheckHr(PropChanged(NULL, kpctNotifyAll, opr.m_hvo, opr.m_tag, 0, chvoNew, -ihvo));
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Run through the cache data looking for references to a deleted object, and removing any that
	are found.  This is a recursive function since it deals with owning properties as well as
	all the other types of properties.  Along the way, record any objects that are deleted and
	the information needed to call PropChanged() for properties that have changed on objects
	that have not been deleted.
----------------------------------------------------------------------------------------------*/
void VwCacheDa::RemoveCachedProperties(HVO hvoDeleted, Set<HVO> & sethvoDel,
	ObjPropIntMap & hmoprnChg)
{
	if (hvoDeleted == 0)
		return;

	sethvoDel.Insert(hvoDeleted);

	Vector<ObjPropRec> voprDelAtomic;
	Vector<ObjPropRec> voprDelColl;
	Vector<HVO> vhvoRefDel;

	// Remove references from the cache of atomic object properties.
	if (m_hmoprobj.Size() != 0)
	{
		ObjPropObjMap::iterator it;

		for (it = m_hmoprobj.Begin(); it != m_hmoprobj.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();
			if (oprKey.m_hvo == hvoDeleted || it->GetValue() == hvoDeleted)
			{
				voprDelAtomic.Push(oprKey);
				vhvoRefDel.Push(it->GetValue());
			}
		}
		for (int i = 0; i < voprDelAtomic.Size(); ++i)
		{
			ObjPropRec & opr = voprDelAtomic[i];

			if (vhvoRefDel[i] == hvoDeleted)
			{
				int index = 0;
				hmoprnChg.Insert(opr, index);
			}
			// Recursively delete references for an owned object.
			if (opr.m_hvo == hvoDeleted && IsOwningField(opr.m_tag))
			{
				RemoveCachedProperties(vhvoRefDel[i], sethvoDel, hmoprnChg);
			}
			else
			{
				// Items owned in a sequence or collection may be stored in a virtual property
				// rather than directly as expected in the code in the remove references
				// code in the next major if block ...
				if (vhvoRefDel[i] == hvoDeleted && opr.m_tag == kflidCmObject_Owner)
					RemoveCachedProperties(opr.m_hvo, sethvoDel, hmoprnChg);
			}
		}
	}
	// Remove references from the cache of sequence/collection object properties.
	if (m_hmoprsobj.Size() != 0)
	{
		ObjPropSeqMap::iterator it;
		ObjSeq os;
		IntVec vihvoDelIndexes;

		for (it = m_hmoprsobj.Begin(); it != m_hmoprsobj.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();
			if (oprKey.m_hvo == hvoDeleted)
			{
				voprDelColl.Push(oprKey);
			}
			else
			{
				os = it->GetValue();
				vihvoDelIndexes.Clear();
				// First we have to scan the whole array, because calling ReplaceAux deletes the memory
				// that os.m_prghvo is pointing at.
				for (int ihvo = os.m_cobj; --ihvo >= 0; )
				{
					if (os.m_prghvo[ihvo] == hvoDeleted)
					{
						// Optimize JohnT: commonly we just have one item; we could refrain from using the
						// vector unless we find a second. (But probably the savings would be negligible.)
						vihvoDelIndexes.Push(ihvo);
					}
				}
				int chvoOriginal = os.m_cobj; // note: might have been decreased by deleting some other object earlier.
				for (int iitem = 0; iitem < vihvoDelIndexes.Size(); iitem++)
				{
					int ihvo = vihvoDelIndexes[iitem];
					// Remove the reference from the current sequence/collection.
					CheckHr(ReplaceAux(oprKey.m_hvo, oprKey.m_tag, ihvo, ihvo+1, NULL, 0));
					chvoOriginal--; // the replace has removed one object.
					// Figure what to do about PropChanged
					int ihvoOld;
					if (hmoprnChg.Retrieve(oprKey, &ihvoOld))
					{
						if (ihvoOld >= 0)
						{
							// We have previously deleted exactly one object from this
							// vector, either in a previous iteration of this current loop,
							// or earlier, when scanning for some other deleted object.
							// We just deleted a second. Therefore the original size
							// was two more than the current size.
							int chvoNegated = -(chvoOriginal + 2);
							hmoprnChg.Insert(oprKey, chvoNegated, true);
						}
						// If the value is negative we have already removed at least two and
						// have the information we need for a total replace.
					}
					else
					{
						// Record the position of the one object we have removed.
						hmoprnChg.Insert(oprKey, ihvo);
					}
					// Continue the loop! In a reference property we might find more hits.
				}
			}
		}
		// Recursively as needed, delete properties belonging to the object being deleted.
		for (int i = 0; i < voprDelColl.Size(); ++i)
		{
			if (IsOwningField(voprDelColl[i].m_tag) && m_hmoprsobj.Retrieve(voprDelColl[i], &os))
			{
				for (int ihvo = 0; ihvo < os.m_cobj; ++ihvo)
					RemoveCachedProperties(os.m_prghvo[ihvo], sethvoDel, hmoprnChg);
			}
		}
	}

	// time to delete ...
	// Remove references from the cache of atomic object properties.
	if (m_hmoprobj.Size() != 0)
	{
		for (int i = 0; i < voprDelAtomic.Size(); ++i)
		{
			// Delete the reference stored in the loop above.  (Deleting it inside that loop
			// would invalidate the iterator.)
			m_hmoprobj.Delete(voprDelAtomic[i]);
		}
	}

	// Remove references from the cache of sequence/collection object properties.
	if (m_hmoprsobj.Size() != 0)
	{
		for (int i = 0; i < voprDelColl.Size(); ++i)
		{
			m_hmoprsobj.Delete(voprDelColl[i]);
		}
	}

	// Remove any extra info stuff from the cache of sequence/collection object properties.
	if (m_hmoprsx.Size() != 0)
	{
		ObjPropExtraMap::iterator it;
		Vector<ObjPropRec> voprDel;
		for (it = m_hmoprsx.Begin(); it != m_hmoprsx.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();
			if (oprKey.m_hvo == hvoDeleted)
				voprDel.Push(oprKey);
		}
		for (int i = 0; i < voprDel.Size(); ++i)
			m_hmoprsx.Delete(voprDel[i]);
	}
	// Remove relevant cached uniqueidentifier (GUID) properties.
	if (m_hmoprguid.Size() != 0)
	{
		ObjPropGuidMap::iterator it;
		Vector<ObjPropRec> voprDel;
		for (it = m_hmoprguid.Begin(); it != m_hmoprguid.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();
			if (oprKey.m_hvo == hvoDeleted)
			{
				voprDel.Push(oprKey);

				// delete from GUID->HVO map if the GUID is an object GUID.
				if (oprKey.m_tag == kflidCmObject_Guid)
					m_hmoguidobj.Delete(it->GetValue());
			}
		}
		for (int i = 0; i < voprDel.Size(); ++i)
			m_hmoprguid.Delete(voprDel[i]);
	}
	// Remove relevant cached integer properties.
	if (m_hmoprn.Size() != 0)
	{
		ObjPropIntMap::iterator it;
		Vector<ObjPropRec> voprDel;
		for (it = m_hmoprn.Begin(); it != m_hmoprn.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();
			if (oprKey.m_hvo == hvoDeleted)
				voprDel.Push(oprKey);
		}
		for (int i = 0; i < voprDel.Size(); ++i)
			m_hmoprn.Delete(voprDel[i]);
	}
	// Remove relevant cached int64/SilTime properties.
	if (m_hmoprlln.Size() != 0)
	{
		ObjPropInt64Map::iterator it;
		Vector<ObjPropRec> voprDel;
		for (it = m_hmoprlln.Begin(); it != m_hmoprlln.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();
			if (oprKey.m_hvo == hvoDeleted)
				voprDel.Push(oprKey);
		}
		for (int i = 0; i < voprDel.Size(); ++i)
			m_hmoprlln.Delete(voprDel[i]);
	}
	// Remove relevant cached IUnknown properties.
	if (m_hmoprunk.Size() != 0)
	{
		ObjPropUnkMap::iterator it;
		Vector<ObjPropRec> voprDel;
		for (it = m_hmoprunk.Begin(); it != m_hmoprunk.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();
			if (oprKey.m_hvo == hvoDeleted)
				voprDel.Push(oprKey);
		}
		for (int i = 0; i < voprDel.Size(); ++i)
			m_hmoprunk.Delete(voprDel[i]);
	}
	// Remove relevant cached multilingual ITsString properties.
	if (m_hmopertss.Size() != 0)
	{
		ObjPropEncTssMap::iterator it;
		Vector<ObjPropEncRec> voperDel;
		for (it = m_hmopertss.Begin(); it != m_hmopertss.End(); ++it)
		{
			ObjPropEncRec operKey = it->GetKey();
			if (operKey.m_hvo == hvoDeleted)
				voperDel.Push(operKey);
		}
		for (int i = 0; i < voperDel.Size(); ++i)
			m_hmopertss.Delete(voperDel[i]);
	}
	// Remove relevant cached binary blob properties.
	if (m_hmoprsta.Size() != 0)
	{
		ObjPropStaMap::iterator it;
		Vector<ObjPropRec> voprDel;
		for (it = m_hmoprsta.Begin(); it != m_hmoprsta.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();
			if (oprKey.m_hvo == hvoDeleted)
				voprDel.Push(oprKey);
		}
		for (int i = 0; i < voprDel.Size(); ++i)
			m_hmoprsta.Delete(voprDel[i]);
	}
	// Remove relevant cached ITsString properties.
	if (m_hmoprtss.Size() != 0)
	{
		ObjPropTssMap::iterator it;
		Vector<ObjPropRec> voprDel;
		for (it = m_hmoprtss.Begin(); it != m_hmoprtss.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();
			if (oprKey.m_hvo == hvoDeleted)
				voprDel.Push(oprKey);
		}
		for (int i = 0; i < voprDel.Size(); ++i)
			m_hmoprtss.Delete(voprDel[i]);
	}
	// Remove relevant cached Unicode properties.
	if (m_hmoprstu.Size() != 0)
	{
		ObjPropStrMap::iterator it;
		Vector<ObjPropRec> voprDel;
		for (it = m_hmoprstu.Begin(); it != m_hmoprstu.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();
			if (oprKey.m_hvo == hvoDeleted)
				voprDel.Push(oprKey);
		}
		for (int i = 0; i < voprDel.Size(); ++i)
			m_hmoprstu.Delete(voprDel[i]);
	}
}


//:>********************************************************************************************
//:>	"Set" methods are used to change object PROPERTY information (outside of reference
//:>	properties).
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetBinary}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetBinary(HVO hvo, PropTag tag, byte * prgb, int cb)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgb, cb);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	StrAnsi sta(reinterpret_cast<const char *>(prgb), cb);
	ObjPropRec oprKey(hvo, tag);
	m_soprMods.Insert(oprKey);
	m_hmoprsta.Insert(oprKey, sta, true); // allow overwrites
	InformNowDirty();

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetGuid}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetGuid(HVO hvo, PropTag tag, GUID uid)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	m_soprMods.Insert(oprKey);
	m_hmoprguid.Insert(oprKey, uid, true); // allow overwrites

	if (tag == kflidCmObject_Guid)
		m_hmoguidobj.Insert(uid, hvo, true);

	InformNowDirty();

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetInt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetInt(HVO hvo, PropTag tag, int n)
{
	BEGIN_COM_METHOD;
	if (TryWriteVirtualInt64(hvo, tag, n) == kwvDone)
		return S_OK;
	SetIntVal(hvo, tag, n);
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetBoolean}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetBoolean(HVO hvo, PropTag tag, ComBool f)
{
	BEGIN_COM_METHOD;
	if (TryWriteVirtualInt64(hvo, tag, f) == kwvDone)
		return S_OK;
	SetIntVal(hvo, tag, f);
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

void VwCacheDa::SetIntVal(HVO hvo, PropTag tag, int n)
{
	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	m_soprMods.Insert(oprKey);
	m_hmoprn.Insert(oprKey, n, true); // allow overwrites
	InformNowDirty();
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetInt64}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetInt64(HVO hvo, PropTag tag, int64 lln)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	if (TryWriteVirtualInt64(hvo, tag, lln) == kwvDone)
		return S_OK;
	SetInt64Val(hvo, tag, lln);
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

void VwCacheDa::SetInt64Val(HVO hvo, PropTag tag, int64 lln)
{
	ObjPropRec oprKey(hvo, tag);
	m_soprMods.Insert(oprKey);
	m_hmoprlln.Insert(oprKey, lln, true); // allow overwrites
	InformNowDirty();

}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetMultiStringAlt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetMultiStringAlt(HVO hvo, PropTag tag, int ws, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptss);
	if (TryWriteVirtualObj(hvo, tag, ws, ptss) == kwvDone)
		return S_OK;
	SetMultiStringAltVal(hvo, tag, ws, ptss);
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}
void VwCacheDa::SetMultiStringAltVal(HVO hvo, PropTag tag, int ws, ITsString * ptss)
{
	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropEncRec opreKey(hvo, tag, ws);
	m_soperMods.Insert(opreKey);
	m_hmopertss.Insert(opreKey, ptss, true); // allow overwrites
	InformNowDirty();
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetString}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetString(HVO hvo, PropTag tag, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptss);
	if (TryWriteVirtualObj(hvo, tag, 0, ptss) == kwvDone)
		return S_OK;
	SetStringVal(hvo, tag, ptss);
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}
void VwCacheDa::SetStringVal(HVO hvo, PropTag tag, ITsString * ptss)
{

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	ObjPropRec oprKey(hvo, tag);
	m_soprMods.Insert(oprKey);
	m_hmoprtss.Insert(oprKey, ptss, true); // allow overwrites
	InformNowDirty();

}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetTime}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetTime(HVO hvo, PropTag tag, int64 tim)
{
	BEGIN_COM_METHOD;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	ObjPropRec oprKey(hvo, tag);
	m_soprMods.Insert(oprKey);
	m_hmoprlln.Insert(oprKey, tim, true); // allow overwrites
	InformNowDirty();

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetUnicode}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetUnicode(HVO hvo, PropTag tag, OLECHAR * prgch, int cch)
{
	BEGIN_COM_METHOD;
	if (TryWriteVirtualUnicode(hvo, tag, prgch, cch) == kwvDone)
		return S_OK;
	SetUnicodeVal(hvo, tag, prgch, cch);
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

void VwCacheDa::SetUnicodeVal(HVO hvo, PropTag tag, OLECHAR * prgch, int cch)
{
	ChkComArrayArg(prgch, cch);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	StrUni stu(prgch, cch);
	ObjPropRec oprKey(hvo, tag);
	m_soprMods.Insert(oprKey);
	m_hmoprstu.Insert(oprKey, stu, true); // allow overwrites
	InformNowDirty();
}


/*----------------------------------------------------------------------------------------------
	See ${ISilDataAccess#SetUnknown}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SetUnknown(HVO hvoObj, PropTag tag, IUnknown * punk)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(punk);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvoObj != 0);
	CacheUnknown(hvoObj, tag, punk);
	ObjPropRec oprKey(hvoObj, tag);
	m_soprMods.Insert(oprKey);
	InformNowDirty();

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


//:>********************************************************************************************
//:>	A method that indicates if the cache has changed since it was first loaded by means of
//:>	Set* methods.  Basically what this means is that client code has called one of the
//:>	property modification methods (eg. "Set" methods, MakeNewObject, DeleteObject*, MoveOwnSeq,
//:>	or Replace methods).
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#IsDirty}
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::IsDirty(ComBool * pf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pf);

	*pf = m_soprMods.Size() > 0 || m_soperMods.Size() > 0 || m_shvoDeleted.Size() > 0;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#ClearDirty}
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::ClearDirty()
{
	BEGIN_COM_METHOD;
	m_soprMods.Clear();
	m_soperMods.Clear();
	m_shvoDeleted.Clear();
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


//:>********************************************************************************************
//:>	Methods used for sending notifications to subscribers when a designated object property
//:>	value (in the cache) has changed.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#AddNotification}

	Add a rootbox to the list of rootboxes that should be notified when a cached value changes.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::AddNotification(IVwNotifyChange * pnchng)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pnchng);

#ifdef TRY_HASH_SET

	m_vvncNew_cIter = m_vvncNew.find(pnchng);
	if (m_vvncNew_cIter != m_vvncNew.end())
	{
		Assert(false); // It was already added before.
	}
	else
		m_vvncNew.insert(pnchng);
#else

	// Make sure it hasn't already been added before.
	bool alreadyExists = false;
	int crootb = m_vvnc.Size();
	for (int irootb = 0; irootb < crootb; irootb++)
	{
		if (m_vvnc[irootb] == pnchng)
		{
			Assert(false); // It was already added before.
			alreadyExists = true;
			break;
		}
	}
	if (!alreadyExists)
		m_vvnc.Push(pnchng);
#endif

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


#define END_COM_METHOD_SPECIAL_DLH(factory, iid) \
	}\
	catch (Throwable & thr) \
	{ \
		return HandleThrowable(thr, iid, &factory); \
	} \
	catch (...) \
	{ \
		return HandleDefaultException(iid, &factory); \
	} \
	finally \
	{ \
		DWORD id = GetCurrentThreadId(); OLECHAR buff[64]; \
		wsprintf(buff,L"@@<<< PropChanged: tID = 0x%lX  EXITING\n", id); \
		OutputDebugString(buff); \
	} \
	return S_OK; \

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#PropChanged}

	Notify all the registered rootboxes of the property change.
	prootb can be NULL if pct != kpctNotifyMeThenAll
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::PropChanged(IVwNotifyChange * pnchng, int pct, HVO hvo, int tag,
	int ivMin, int cvIns, int cvDel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pnchng);
	if (!pnchng && kpctNotifyAll != pct)
		ThrowInternalError(E_POINTER);

	if (m_nSuppressPropChangesLevel > 0)
	{
		// We want to queue this call to PropChanged so that it can be processed later
		// but we have to check the already queued calls first to see if we already have an entry
		// with the same hvo and tag and an overlapping range.
		// We also have to see if this object is already owned in a flid of an object covered by an
		// existing prop-changed.
		HVO hvoOwner;
		int flidOwning, iIndexInOwner;
		CheckHr(get_ObjectProp(hvo, kflidCmObject_Owner, &hvoOwner));
		CheckHr(get_IntProp(hvo, kflidCmObject_OwnFlid, &flidOwning));
		if (hvoOwner && flidOwning)
			CheckHr(GetObjIndex(hvoOwner, flidOwning, hvo, &iIndexInOwner));
		else
			iIndexInOwner = -1;

		for (int i = 0; i < m_vPropChangeds.Size(); i++)
		{
			PropChangedInfo pci = m_vPropChangeds[i];
			if (pci.hvo == hvo && pci.tag == tag &&
				ivMin >= pci.ivMin && ivMin <= (pci.ivMin + pci.cvIns))
			{
				// For now we assume that these values are the same. If this shouldn't be the
				// case, we have to enhance our tests and the handling of this.
				Assert(pci.pnchng == pnchng);
				Assert(pci.pct == pct);

				int previouslyInsertedLim = pci.ivMin + pci.cvIns;
				int previouslyInsertedNowDeleted = min(previouslyInsertedLim - ivMin, cvDel);
				pci.cvDel = pci.cvDel + cvDel - previouslyInsertedNowDeleted ;
				pci.cvIns = pci.cvIns - previouslyInsertedNowDeleted + cvIns;
				m_vPropChangeds[i] = pci;
				return S_OK;
			}
			else if (pci.hvo == hvoOwner && pci.tag == flidOwning &&
				iIndexInOwner >= pci.ivMin && iIndexInOwner < (pci.ivMin + pci.cvIns) && m_qmdc)
			{
				// The PropChanged relates to a newly created object...but only if it's an owning SEQUENCE,
				// if it's a collection it's meaningless to try to consider the order to determine
				// whether it is newly created.
				// We can generally suppress PropChanged calls for newly created objects because
				// prior to their creation they didn't exist so nothing can have been displaying them.
				// Suppression happens during Undo/Redo, where we make all the changes before issuing
				// all the PropChanged. So the PropChanged that inserts it will have inserted it
				// in its fully-created state, and we shouldn't need further PropChanged on its
				// own properties.
				// But, we do need to verify that it is not an owning COLLECTION; owning collections
				// typically use ranges of changed values that are wider than what actually got created
				// (e.g., the whole sequence). So if no MDC, we can't assume sequence, so don't suppress.
				int nType;
				CheckHr(m_qmdc->GetFieldType(flidOwning, &nType));
				if (nType != kcptOwningCollection)
				{
					// For now we assume that these values are the same. If this shouldn't be the
					// case, we have to enhance our tests and the handling of this.
					Assert(pci.pnchng == pnchng);
					Assert(pci.pct == pct);
					return S_OK;
				}
			}
		}
		m_vPropChangeds.Push(PropChangedInfo(pnchng, pct, hvo, tag, ivMin, cvIns, cvDel));
		return S_OK;
	}

	if (kpctNotifyMeThenAll == pct)
		CheckHr(pnchng->PropChanged(hvo, tag, ivMin, cvIns, cvDel));

	// Build a vector of the things we want to notify. This ensures we don't miss any
	// because of changes to the vector contents while we're doing the notify.
#ifdef TRY_HASH_SET
	std::set<IVwNotifyChange*> vvncTNewVwRootBoxes;
	std::set<IVwNotifyChange*>::const_iterator vvncTNew_cIter;
	std::set<IVwNotifyChange*> vvncTNewOtherNotifiers;

	for (m_vvncNew_cIter = m_vvncNew.begin(); m_vvncNew_cIter != m_vvncNew.end(); m_vvncNew_cIter++)
	{
		if (*m_vvncNew_cIter != pnchng || kpctNotifyAll == pct)
		{
			IVwRootBoxPtr qrootb;
			(*m_vvncNew_cIter)->QueryInterface(IID_IVwRootBox, (void **)&qrootb);
			if (qrootb)
				vvncTNewVwRootBoxes.insert(*m_vvncNew_cIter);
			else
				vvncTNewOtherNotifiers.insert(*m_vvncNew_cIter);
		}
	}
#else
	Vector<IVwNotifyChange *> vvncT;
	for (int irootb = 0; irootb < m_vvnc.Size(); irootb++)
	{
		// If pct == kpctNotifyMeThenAll and m_vvnc[irootb] == prootb, we already
		//   notified it.
		// If pct == kpctNotifyAllButMe and m_vvnc[irootb] == prootb, don't notify it.
		if (m_vvnc[irootb] != pnchng || kpctNotifyAll == pct)
			vvncT.Push(m_vvnc[irootb]);
	}
#endif

#ifdef TRY_HASH_SET
		// Each one we propose to do must be checked to see whether it is still in
		// the active vector; otherwise there is danger that it has been closed and
		// is no longer in a state to receive notifications.
	for (vvncTNew_cIter = vvncTNewOtherNotifiers.begin(); vvncTNew_cIter != vvncTNewOtherNotifiers.end(); vvncTNew_cIter++)
	{
		m_vvncNew_cIter = m_vvncNew.find(*vvncTNew_cIter);
		if (m_vvncNew_cIter != m_vvncNew.end())
			CheckHr((*m_vvncNew_cIter)->PropChanged(hvo, tag, ivMin, cvIns, cvDel));
#ifdef DEBUG
		else
		{
			OLECHAR buff2[64];
			wsprintf(buff2,L"DDD PropChanged client no longer active\n");
			OutputDebugString(buff2);
		}
#endif
	}
	for (vvncTNew_cIter = vvncTNewVwRootBoxes.begin(); vvncTNew_cIter != vvncTNewVwRootBoxes.end(); vvncTNew_cIter++)
	{
		m_vvncNew_cIter = m_vvncNew.find(*vvncTNew_cIter);
		if (m_vvncNew_cIter != m_vvncNew.end())
			CheckHr((*m_vvncNew_cIter)->PropChanged(hvo, tag, ivMin, cvIns, cvDel));
#ifdef DEBUG
		else
		{
			OLECHAR buff2[64];
			wsprintf(buff2,L"DDD PropChanged client no longer active\n");
			OutputDebugString(buff2);
		}
#endif
	}
#else
	int cNotifiers = vvncT.Size();
	for (int i = 0; i < cNotifiers; i++)
	{
		// Each one we propose to do must be checked to see whether it is still in
		// the active vector; otherwise there is danger that it has been closed and
		// is no longer in a state to receive notifications.
		bool fStillActive = false;
		int cNotifiers2 = m_vvnc.Size(); // Don't move outside the main loop, could change during PropChanged call.
		IVwNotifyChange * pnchngT = vvncT[i];
		for (int j = 0; j < cNotifiers2; j++)
		{
			if (m_vvnc[j] == pnchngT)
			{
				fStillActive = true;
				break;
			}
		}
		if (fStillActive)
			CheckHr(pnchngT->PropChanged(hvo, tag, ivMin, cvIns, cvDel));
	}
#endif
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	Call this to queue any PropChanged. They will be fired when ResumePropChanges gets called.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::SuppressPropChanges()
{
	BEGIN_COM_METHOD;
	m_nSuppressPropChangesLevel++;

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	Resume calls to PropChanged and notify view of any queued PropChanged calls. The method
	checks to see if the object in question is still valid.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::ResumePropChanges()
{
	BEGIN_COM_METHOD;
	if (m_nSuppressPropChangesLevel > 0)
		m_nSuppressPropChangesLevel--;

	if (!m_nSuppressPropChangesLevel)
	{
		for (int i = 0; i < m_vPropChangeds.Size(); i++)
		{
			PropChangedInfo pci = m_vPropChangeds[i];

			ComBool fIsValid;
			CheckHr(get_IsValidObject(pci.hvo, &fIsValid));
			if (!fIsValid)
				continue;

			CheckHr(PropChanged(pci.pnchng, pci.pct, pci.hvo, pci.tag, pci.ivMin, pci.cvIns, pci.cvDel));
		}
		m_vPropChangeds.Clear();
	}

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#RemoveNotification}
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::RemoveNotification(IVwNotifyChange * pnchng)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pnchng);
#ifdef TRY_HASH_SET
	m_vvncNew_cIter = m_vvncNew.find(pnchng);
	if (m_vvncNew_cIter != m_vvncNew.end())
	{
		m_vvncNew.erase(m_vvncNew.find(pnchng));
		return S_OK;
	}
#else
	int crootb = m_vvnc.Size();
	for (int irootb = 0; irootb < crootb; irootb++)
	{
		if (m_vvnc[irootb] == pnchng)
		{
			m_vvnc.Delete(irootb);
			return S_OK;
		}
	}
#endif
	// This can happen during FullRefresh when the cache has been cleared before all windows
	// have been refreshed. During the windows refreshing, they may be removing their
	// notifications which have already been cleared. We'll return S_FALSE in this case
	// in case anyone is really interested.
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GetObjIndex}
	Return the index of an object in a vector (or 0 if the property is atomic).
	@param hvoOwn The object that contains flid.
	@param flid The property on hvoOwn that holds hvo.
	@param hvo The object contained in the flid property of hvoOwn
	@param pihvo Pointer to receive the index of hvo in the flid vector (or 0 if atomic).
	@return S_OK for normal completion. E_POINTER if pihvo is invalid. E_INVALID arg if
		hvoOwn, flid, or hvo are zero.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::GetObjIndex(HVO hvoOwn, int flid, HVO hvo, int * pihvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pihvo);
	if (!hvoOwn || !hvo || !flid)
		ThrowInternalError(E_INVALIDARG);

	// Allow this to work for non-vector flids as well.
	// We must use this routine rather than get_ObjProp because in subclasses the latter will
	// try (and disastrously fail) to load the property from the database even if it isn't
	// an owning atomic property.
	if (hvo == GetObjPropCheckType(hvoOwn, flid))
	{
		*pihvo = 0;
		return S_OK;
	}

	int chvo;
	int ihvo = 0;
	get_VecSize(hvoOwn, flid, &chvo);
	for (; ihvo < chvo; ++ihvo)
	{
		HVO hvoT = 0;
		get_VecItem(hvoOwn, flid, ihvo, &hvoT);
		if (hvoT == hvo)
			break;
	}
	*pihvo = ihvo == chvo ? -1 : ihvo;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GetOutlineNumber}
	Return a string giving an outline number such as 1.2.3 based on position in the owning
	hierarcy.
	@param hvo The object for which we want an outline number.
	@param flid The property on hvo's owner that holds hvo.
	@param fFinPer True if you want a final period appended to the string.
	@param pbstr Pointer to receive the outline string.
	@return S_OK for normal completion. E_POINTER if pbstr is invalid. E_INVALIDARG if hvo
		or flid are zero.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::GetOutlineNumber(HVO hvo, int flid, ComBool fFinPer, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);
	if (!hvo || !flid)
		ThrowInternalError(E_INVALIDARG);

	StrUni stu;
	HVO hvoOwn;
	int ihvo;
	bool fForever = true; // Hack to keep compiler happy.

	do
	{
		// Get the owner of hvo.
		get_ObjectProp(hvo, kflidCmObject_Owner, &hvoOwn);
		if (!hvoOwn)
			break; // A missing owner returns what is generated thus far.
		// See if hvo is owned by hvoOwn in the flid parameter.
		GetObjIndex(hvoOwn, flid, hvo, &ihvo);
		if (ihvo < 0)
			break; // This will stop recursion when hvoSub is owned in a different property.
		if (stu.Length())
			stu.Format(L"%d.%s", ihvo + 1, stu.Chars());
		else
			stu.Format(L"%d", ihvo + 1);
		hvo = hvoOwn;
	} while (fForever);

	if (fFinPer)
		stu.FormatAppend(L".");
	stu.GetBstr(pbstr);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

bool IsVirtual(PropTag tag, IFwMetaDataCache * pmdc)
{
	ULONG tag1 = tag;
	ComBool fVirtual;
	CheckHr(pmdc->get_IsVirtual(tag1, &fVirtual));
	return fVirtual;
}

template<class Map>
void ClearVirtuals(Map & map, IFwMetaDataCache * pmdc)
{
	Map::iterator it;
	Vector<ObjPropRec> vopr;
	for (it = map.Begin(); it != map.End(); ++it)
	{
		if (IsVirtual(it.GetKey().m_tag, pmdc))
		{
			vopr.Push(it.GetKey());
		}
	}
	for (int i = 0; i < vopr.Size(); i++)
	{
		map.Delete(vopr[i]);
	}
}

/*----------------------------------------------------------------------------------------------
	Clear all virtual property data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::ClearVirtualProperties()
{
	BEGIN_COM_METHOD
	if (!m_qmdc)
		return E_FAIL; // can't implement without a metadatacache to tell which are virtual

	ClearVirtuals(m_hmoprn, m_qmdc);
	ClearVirtuals(m_hmoprguid, m_qmdc);
	// ClearVirtuals(m_hmoguidobj, m_qmdc); - can't have any virtuals in it
	ClearVirtuals(m_hmoprlln, m_qmdc);
	ClearVirtuals(m_hmoprunk, m_qmdc);
	ClearVirtuals(m_hmoprsta, m_qmdc);
	ClearVirtuals(m_hmoprtss, m_qmdc);
	ClearVirtuals(m_hmoprstu, m_qmdc);
	ClearVirtuals(m_hmoprobj, m_qmdc);

	// sequences are special, we have to do something about deleting the value.
	ObjPropSeqMap::iterator it;
	Vector<ObjPropRec> vopr;
	for (it = m_hmoprsobj.Begin(); it != m_hmoprsobj.End(); ++it)
	{
		ObjSeq & os = it.GetValue();
		if (IsVirtual(it.GetKey().m_tag, m_qmdc))
		{
			ClearObjSeq(os);
			vopr.Push(it.GetKey());
		}
	}
	for (int i = 0; i < vopr.Size(); i++)
	{
		m_hmoprsobj.Delete(vopr[i]);
	}

	// Multistrings are special, we have a different kind of key
	ObjPropEncTssMap::iterator it2;
	Vector<ObjPropEncRec> vopre;
	for (it2 = m_hmopertss.Begin(); it2 != m_hmopertss.End(); ++it2)
	{
		if (IsVirtual(it2.GetKey().m_tag, m_qmdc))
		{
			vopre.Push(it2.GetKey());
		}
	}
	for (int i = 0; i < vopre.Size(); i++)
	{
		m_hmopertss.Delete(vopre[i]);
	}



	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	Get the meta data cache, if any. Type IUnknown is used to avoid circularity
	between FieldWorks components in type definitions.
	(Arguably these functions would make more sense in IVwCachDa. But they are
	very parallel to the writing system factory methods, which are well-established
	in this interface.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_MetaDataCache(IFwMetaDataCache ** ppmdc)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppmdc);
	*ppmdc = m_qmdc;
	AddRefObj(*ppmdc);
	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	Set the meta data cache.
	(Note that currently this is most commonly done in the Init method of IVwOleDbDa.
	A setter is added here so that non-database caches can have metadata.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::putref_MetaDataCache(IFwMetaDataCache * pmdc)
{
	BEGIN_COM_METHOD
	m_qmdc = pmdc;
	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


//:>********************************************************************************************
//:>	Other methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Return the writing system factory for this database (or the registry, as the case may be).

	@param ppwsf Address of the pointer for returning the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppwsf);

	AssertPtr(m_qwsf);
	*ppwsf = m_qwsf;
	(*ppwsf)->AddRef();

	END_COM_METHOD(g_fact, IID_ISilDataAccess)
}

/*----------------------------------------------------------------------------------------------
	Set the writing system factory for this database (or the registry, as the case may be).

	@param pwsf Pointer to the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_fact, IID_ISilDataAccess)
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#get_WritingSystemsOfInterest}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_WritingSystemsOfInterest(int cwsMax, int * pws, int * pcws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pws);
	ChkComArgPtr(pcws);

	*pcws = 0;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

//:>****************************************************************************************
//:>	Methods to set and retrieve extra info for collection/sequence references.
//:>****************************************************************************************


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#InsertRelExtra}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::InsertRelExtra(HVO hvoSrc, PropTag tag, int ihvo, HVO hvoDst,
	BSTR bstrExtra)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrExtra);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvoSrc != 0);
	CheckHr(ReplaceAux(hvoSrc, tag, ihvo, ihvo, &hvoDst, 1));
	ObjPropRec oprKey(hvoSrc, tag);
	m_soprMods.Insert(oprKey);
	InformNowDirty();
	SeqExtra sx;
	// If it is not there already the only valid insertion is 0,0
	if (!m_hmoprsx.Retrieve(oprKey, &sx))
		sx.m_cstu = 0; // work with empty list
	if (ihvo > sx.m_cstu)
		ThrowHr(WarnHr(E_INVALIDARG)); // Index out of range.

	SeqExtra sxRep;
	sxRep.m_cstu = sx.m_cstu + 1;
	sxRep.m_prgstu = NewObj StrUni[sxRep.m_cstu];
	MoveItems(sx.m_prgstu, sxRep.m_prgstu, ihvo);
	sxRep.m_prgstu[ihvo].Assign(bstrExtra, BstrLen(bstrExtra));
	MoveItems(sx.m_prgstu + ihvo, sxRep.m_prgstu + ihvo + 1, sx.m_cstu - ihvo);

	m_hmoprsx.Insert(oprKey, sxRep, true);
	if (sx.m_prgstu)
		delete[] sx.m_prgstu;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#UpdateRelExtra}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::UpdateRelExtra(HVO hvoSrc, PropTag tag, int ihvo, BSTR bstrExtra)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrExtra);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvoSrc != 0);
	ObjPropRec oprKey(hvoSrc, tag);
	SeqExtra sx;
	// If it is not there already the only valid insertion is 0,0
	if (!m_hmoprsx.Retrieve(oprKey, &sx))
		ThrowInternalError(E_INVALIDARG); // Nothing to update.

	if (ihvo <= sx.m_cstu)
		ThrowInternalError(E_INVALIDARG); // Index out of range.

	sx.m_prgstu[ihvo].Assign(bstrExtra, BstrLen(bstrExtra));

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GetRelExtra}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::GetRelExtra(HVO hvoSrc, PropTag tag, int ihvo, BSTR * pbstrExtra)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrExtra);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvoSrc != 0);
	ObjPropRec oprKey(hvoSrc, tag);
	SeqExtra sx;
	// If it is not there already the only valid insertion is 0,0
	if (!m_hmoprsx.Retrieve(oprKey, &sx))
		ThrowInternalError(E_INVALIDARG); // Nothing to update.

	if (ihvo <= sx.m_cstu)
		ThrowInternalError(E_INVALIDARG); // Index out of range.

	sx.m_prgstu[ihvo].GetBstr(pbstrExtra);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	Remove hvoDel from the (backreference) property tagDst of hvoDst, if it occurs.
	OPTIMIZE JohnT: if we know the prop is sorted, make use of this to find the right
	index more efficiently.
----------------------------------------------------------------------------------------------*/
void VwCacheDa::DelBackRef(HVO hvoDel, HVO hvoDst, PropTag tagDst)
{
	// See if we have the inverse cached.
	ObjPropRec oprKeyBr(hvoDst, tagDst);
	ObjSeq osBr;
	if (m_hmoprsobj.Retrieve(oprKeyBr,&osBr))
	{
		// We do! fix it.
		int ihvo;
		for (ihvo = 0; ihvo < osBr.m_cobj && osBr.m_prghvo[ihvo] != hvoDel; ihvo++)
			;
		if (ihvo >= osBr.m_cobj)
		{
			Warn("missing backref not removed");
			return; // not there for some reason, ignore.
		}
		CheckHr(ReplaceAux(hvoDst, tagDst, ihvo, ihvo + 1, NULL, 0));
		CheckHr(PropChanged(NULL, kpctNotifyAll, hvoDst, tagDst, ihvo, 0, 1));
	}
}

/*----------------------------------------------------------------------------------------------
	Add hvoIns to the (backreference) property tagDst of hvoDst.
	ENHANCE JohnT: if we know how the property is sorted, make sure to insert at the right
	place.
----------------------------------------------------------------------------------------------*/
void VwCacheDa::InsBackRef(HVO hvoIns, HVO hvoDst, PropTag tagDst)
{
	// For now insert at start.
	CheckHr(ReplaceAux(hvoDst, tagDst, 0, 0, &hvoIns, 1));
	CheckHr(PropChanged(NULL, kpctNotifyAll, hvoDst, tagDst, 0, 1, 0));
}

/*----------------------------------------------------------------------------------------------
	Remove from the cache all information about this object and, if the second
	argument is true, everything it owns.

	Note that this is not absolutely guaranteed to work. It tells the system that you
	no longer need this information cached. However, whether it can find the information
	efficiently enough to actually do the deletion depends on whether the implementation
	has a MetaDataCache that can tell it what properties the object has, and in the
	case of owned objects, it will only find children that are accessible through
	properties that are in the cache.

	Note that the property that owns this object is not modified, nor are any
	references or backreferences that point at it. Only outward references from the
	object (and its children) are cleared.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::ClearInfoAbout(HVO hvo, VwClearInfoAction cia)
{
	// This default implementation doesn't do anything useful. This a historical artifact
	// since at one point it didn't have a meta data cache, so we put the real implementation
	// on VwOleDbDa. It should be moved down, with a check for the possibility that the cache
	// is null.
	return S_OK;
}

STDMETHODIMP VwCacheDa::ClearInfoAboutAll(HVO * prghvo, int chvo, VwClearInfoAction cia)
{
	return S_OK;
}
/*----------------------------------------------------------------------------------------------
	Method to retrieve a particular int property if it is in the cache, and return a bool
	to say whether it was or not. Similar to ISilDataAccess::get_IntProp, but this method
	is guaranteed not to do a lazy load of the property and it makes it easier for .Net
	clients to see whether the property was loaded, because this info is not hidden in an
	HRESULT.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_CachedIntProp(HVO hvo, PropTag tag, ComBool * pf, int * pn)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pf);
	ChkComArgPtr(pn);

	ObjPropRec oprKey(hvo, tag);
	if (m_hmoprn.Retrieve(oprKey, pn))
		*pf = true;
	else
	{
		*pn = 0;
		*pf = false;
	}
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#ClearAllData}
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::ClearAllData()
{
	BEGIN_COM_METHOD;

	// We don't want to reset this. This method is called
	// during FullRefresh and some synchronizing. The problem I encountered when it was
	// being cleared was doing a promote in a single window, then opening a second window
	// and dragging the promoted item back to the original location. Since promote calls
	// this method, it was resetting m_hvoNextDummy so that the second window was using
	// the same value as the first window. Then when we tried to delete an item from the
	// cache for both windows, it tried deleting the item twice from the same vector instead\
	// of different vectors.
	//m_hvoNext = 100000000;
	//m_hvoNextDummy = -1000000;

	//	Clear the hash maps that store atomic and collection/sequence REFERENCE information.
	m_hmoprobj.Clear(); // Done

	// Free all the object sequence data
	ClearCriticalMaps();

	// Clear the hash maps that store all object PROPERTY information except reference info.
	m_hmoprguid.Clear(); // Done
	m_hmoguidobj.Clear(); // Done :-)
	m_hmoprn.Clear(); // Done
	m_hmoprlln.Clear(); // Done
	m_hmoprunk.Clear(); // Done
	m_hmopertss.Clear(); // Done
	m_hmoprsta.Clear(); // Done
	m_hmoprtss.Clear(); // Done
	m_hmoprstu.Clear(); // Done

	// Clear the sets that contain records of modified or deleted objects.
	m_soprMods.Clear();
	m_soperMods.Clear();
	m_shvoDeleted.Clear();

	// Do NOT clear the vector of object property change notifications.
	// This method is used in Refresh, and we definitely want the connections to
	// interested observers to survive refresh.
	//m_vvnc.Clear();

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

StrUni MakeVhKey(BSTR bstrClass, BSTR bstrField)
{
	StrUni stuKey;
	OLECHAR * prgch;
	int classLen = BstrLen(bstrClass);
	int keyLen = classLen + BstrLen(bstrField) + 2;
	stuKey.SetSize(keyLen, &prgch);
	wcscpy_s(prgch, keyLen, bstrClass);
	prgch[classLen] = 13;
	wcscpy_s(prgch + classLen + 1, keyLen - classLen - 1, bstrField);
	return stuKey;
}
/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#InstallVirtual}
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::InstallVirtual(IVwVirtualHandler * pvh)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvh);
	int type;
	CheckHr(pvh->get_Type(&type));
	Assert(type >= kcptMin && type < kcptLim );
	int tag = m_tagNextVp++;
	CheckHr(pvh->put_Tag(tag));
	m_hmtagvh.Insert(tag, pvh);
	SmartBstr sbstrClass, sbstrField;
	CheckHr(pvh->get_ClassName(&sbstrClass));
	CheckHr(pvh->get_FieldName(&sbstrField));
	// Deliberately call with fOkToOverwrite false; causes E_INVALIDARG to be thrown
	// if key already present.
	StrUni stuKey = MakeVhKey(sbstrClass, sbstrField);
	m_hmstuvh.Insert(stuKey, pvh);
	if (m_qmdc)
	{
		// Add info to MDC.
		int tag;
		CheckHr(pvh->get_Tag(&tag));
		SmartBstr sbstrClass, sbstrField;
		CheckHr(pvh->get_ClassName(&sbstrClass));
		CheckHr(pvh->get_FieldName(&sbstrField));
		CheckHr(m_qmdc->AddVirtualProp(sbstrClass.Bstr(), sbstrField.Bstr(), (ULONG) tag, type));
	}

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}
/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#InstallVirtual}
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::GetVirtualHandlerId(PropTag tag, IVwVirtualHandler ** ppvh)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvh);
	IVwVirtualHandlerPtr qvh;
	m_hmtagvh.Retrieve(tag, qvh); // No error if not found, just leave zero.
	*ppvh = qvh.Detach();
	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	${IVwCacheDa#InstallVirtual}
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::GetVirtualHandlerName(BSTR bstrClass, BSTR bstrField,
		IVwVirtualHandler ** ppvh)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrClass);
	ChkComBstrArg(bstrField);
	ChkComOutPtr(ppvh);
	StrUni stuKey = MakeVhKey(bstrClass, bstrField);
	IVwVirtualHandlerPtr qvh;
	m_hmstuvh.Retrieve(stuKey, qvh);

	if (!qvh && m_qmdc)
	{
		ULONG clsid;
		CheckHr(m_qmdc->GetClassId(bstrClass, &clsid));

		for( ; ; )
		{
			ULONG baseClsid;
			CheckHr(m_qmdc->GetBaseClsId(clsid, &baseClsid));
			SmartBstr sbstrBaseClass;
			CheckHr(m_qmdc->GetClassName(baseClsid, &sbstrBaseClass));
			stuKey = MakeVhKey(sbstrBaseClass, bstrField);
			m_hmstuvh.Retrieve(stuKey, qvh);
			if (qvh)
				break; // got it!
			if (baseClsid == 0)
				break; // not going to get it, current class is CmObject
			clsid = baseClsid;
		}
	}
	if (qvh)
		*ppvh = qvh.Detach();
	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#IsValidObject}
	Test whether an HVO represents a valid object. For the simple memory cache,
	any HVO is potentially valid, and true will be returned.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_IsValidObject(HVO hvo, ComBool * pfValid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfValid);
	*pfValid = true;
	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#IsValidObject}
	Test whether an HVO is in the range of dummy IDs. The default implementation just answers
	false; all IDs are considered real objects.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCacheDa::get_IsDummyId(HVO hvo, ComBool * pfDummy)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfDummy);

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}
/*----------------------------------------------------------------------------------------------
	Try running a virtual property. Return a result indicating whether we found one and, if
	so, whether it is a 'ComputeEveryTime'.
----------------------------------------------------------------------------------------------*/
VhResult VwCacheDa::TryVirtual(HVO hvo, PropTag tag, int ws)
{
	IVwVirtualHandlerPtr qvh;
	if (!m_hmtagvh.Retrieve(tag, qvh))
		return kvhrNotVirtual;
	CheckHr(qvh->Load(hvo, tag, ws, this));
	ComBool fComputeEveryTime;
	CheckHr(qvh->get_ComputeEveryTime(&fComputeEveryTime));
	return fComputeEveryTime ? kvhrUseAndRemove : kvhrUse;
}

/*----------------------------------------------------------------------------------------------
	Try writing a virtual property. Return a result indicating whether we found one and, if
	so, whether it is a 'ComputeEveryTime'.
----------------------------------------------------------------------------------------------*/
WriteVirtualResult VwCacheDa::TryWriteVirtualInt64(HVO hvo, PropTag tag, int64 val)
{
	IVwVirtualHandlerPtr qvh;
	if (!m_hmtagvh.Retrieve(tag, qvh))
		return kwvNotVirtual;
	CheckHr(qvh->WriteInt64(hvo, tag, val, this));
	ComBool fComputeEveryTime;
	CheckHr(qvh->get_ComputeEveryTime(&fComputeEveryTime));
	return fComputeEveryTime ? kwvDone : kwvCache;
}

/*----------------------------------------------------------------------------------------------
	Try writing a virtual property. Return a result indicating whether we found one and, if
	so, whether it is a 'ComputeEveryTime'.
----------------------------------------------------------------------------------------------*/
WriteVirtualResult VwCacheDa::TryWriteVirtualUnicode(HVO hvo, PropTag tag, OLECHAR * prgch, int cch)
{
	IVwVirtualHandlerPtr qvh;
	if (!m_hmtagvh.Retrieve(tag, qvh))
		return kwvNotVirtual;
	StrUni stuVal(prgch, cch);
	CheckHr(qvh->WriteUnicode(hvo, tag, stuVal.Bstr(), this));
	ComBool fComputeEveryTime;
	CheckHr(qvh->get_ComputeEveryTime(&fComputeEveryTime));
	return fComputeEveryTime ? kwvDone : kwvCache;
}

/*----------------------------------------------------------------------------------------------
	Try writing a virtual property. Return a result indicating whether we found one and, if
	so, whether it is a 'ComputeEveryTime'.
----------------------------------------------------------------------------------------------*/
WriteVirtualResult VwCacheDa::TryWriteVirtualObj(HVO hvo, PropTag tag, int ws, IUnknown * punk)
{
	IVwVirtualHandlerPtr qvh;
	if (!m_hmtagvh.Retrieve(tag, qvh))
		return kwvNotVirtual;
	CheckHr(qvh->WriteObj(hvo, tag, ws, punk, this));
	ComBool fComputeEveryTime;
	CheckHr(qvh->get_ComputeEveryTime(&fComputeEveryTime));
	return fComputeEveryTime ? kwvDone : kwvCache;
}

/*----------------------------------------------------------------------------------------------
	Try writing a virtual property. Return a result indicating whether we found one and, if
	so, whether it is a 'ComputeEveryTime'.
----------------------------------------------------------------------------------------------*/
WriteVirtualResult VwCacheDa::TryVirtualReplace(HVO hvo, PropTag tag, int ihvoMin, int ihvoLim,
	HVO * prghvo, int chvoIns)
{
	IVwVirtualHandlerPtr qvh;
	if (!m_hmtagvh.Retrieve(tag, qvh))
		return kwvNotVirtual;
	CheckHr(qvh->Replace(hvo, tag, ihvoMin, ihvoLim, prghvo, chvoIns, this));
	ComBool fComputeEveryTime;
	CheckHr(qvh->get_ComputeEveryTime(&fComputeEveryTime));
	return fComputeEveryTime ? kwvDone : kwvCache;
}

/*----------------------------------------------------------------------------------------------
	Try writing a virtual property. Return a result indicating whether we found one and, if
	so, whether it is a 'ComputeEveryTime'.
----------------------------------------------------------------------------------------------*/
WriteVirtualResult VwCacheDa::TryVirtualAtomic(HVO hvo, PropTag tag, HVO newVal)
{
	IVwVirtualHandlerPtr qvh;
	if (!m_hmtagvh.Retrieve(tag, qvh))
		return kwvNotVirtual;
	HVO hvoOld = 0;
	ObjPropRec oprKey(hvo, tag);
	m_hmoprobj.Retrieve(oprKey, &hvoOld); // Leaves zero if not present.
	CheckHr(qvh->Replace(hvo, tag, 0, (hvoOld == 0 ? 0 : 1), &newVal, (newVal == 0 ? 0 : 1), this));
	ComBool fComputeEveryTime;
	CheckHr(qvh->get_ComputeEveryTime(&fComputeEveryTime));
	return fComputeEveryTime ? kwvDone : kwvCache;
}
// Explicit instantiation of hashmap classes
#include "HashMap_i.cpp"
#include "ComHashMap_i.cpp"
#include "Set_i.cpp"
#include "Vector_i.cpp"
#include "MultiMap_i.cpp"

template HashMap<ObjPropRec, int>; // ObjPropIntMap; // Hungarian hmoprn
template HashMap<ObjPropRec, HVO>; // ObjPropObjMap; // Hungarian hmoprobj
template ComHashMap<ObjPropRec, ITsString>; // ObjPropTssMap; // Hungarian hmoprtss
template HashMap<ObjPropRec, ObjSeq>; // ObjPropSeqMap; // Hungarian hmoprsobj
template ComHashMap<ObjPropEncRec, ITsString>; // ObjPropEncTssMap; // Hungarian hmopertss
template ComHashMap<ObjPropRec, IUnknown>; // ObjPropUnkMap; // Hungarian hmoprunk
template Set<ObjPropEncRec>; // ObjPropEncSet;
template Set<ObjPropRec>; // ObjPropSet; // Hungarian sopr
template HashMap<ObjPropRec, StrUni>; // ObjPropStrMap; // Hungarian hmoprstu
template Set<HVO>; // HvoSet; // Hungarian shvo
template HashMap<ObjPropRec, SeqExtra>; // ObjPropExtraMap; // Hungarian hmoprsx
template ComHashMap<PropTag, IVwVirtualHandler>; // TagVhMap; // Hungarian hmtagvp
template ComHashMapStrUni<IVwVirtualHandler>; // StrVhMap; // Hungarian hmstuvh
