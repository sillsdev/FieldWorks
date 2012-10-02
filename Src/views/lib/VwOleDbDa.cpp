/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwOleDbDa.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file contains class definitions for the following classes:
		VwOleDbDa
		FldSpec
		BlockSpec
		RecordSpec
		UserViewSpec
		VwDataSpec
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
//  Note: Including these 2 files will lead to code bloat.
// Include in AfExplicitInstantations.cpp (Aflib build)
// and ExplicitInstantations.cpp (Views build).
//#include "HashMap.h"
#include "HashMap_i.cpp"
#include "Vector_i.cpp"

// Explicit instantiation
// Included in AfExplicitInstantations.cpp (Aflib build)
// and ExplicitInstantations.cpp (Views build).
//template Vector<byte>;
//template Vector<wchar>;
//template Vector<HVO>;

static const int kcbFmtBufMax = 1024;

//:>********************************************************************************************
//:>	VwOleDbDa methods.
//:>********************************************************************************************

static DummyFactory g_fact(_T("SIL.Views.VwOleDbDa"));

// Constants for maximum allowed lengths (in characters) of String, Unicode, MultiString
// and MultiUnicode.
const int kcchMaxString = 1000;
const int kcchMaxUnicode = 1000;
const int kcchMaxMultiString = 1000;
const int kcchMaxMultiUnicode = 1000;

/*----------------------------------------------------------------------------------------------
	Duplicates AfUtil::GetResourceTss, but can't see how to avoid...
----------------------------------------------------------------------------------------------*/
static bool GetResourceTss(ILgWritingSystemFactory * pwsf, int rid, ITsString ** pptss)
{
	AssertPtr(pwsf);
	AssertPtr(pptss);
	Assert(!*pptss);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	int wsUser;
	CheckHr(pwsf->get_UserWs(&wsUser));
	StrUni stu(rid);
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), wsUser, pptss);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwOleDbDa::VwOleDbDa()
{
	m_hvoNext = 100000000;
	m_hvoNextDummy = khvoFirstDummyId;
	m_wsUser = 0;
	StrUtil::InitIcuDataDir();
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwOleDbDa::~VwOleDbDa()
{
}

/*----------------------------------------------------------------------------------------------
	Method to support using GenericFactory to create an instance. An actual generic factory
	instance is not made in this file, because it is included in many places. Instead, currently
	one generic factory exists in VwRootBox.cpp.
----------------------------------------------------------------------------------------------*/
void VwOleDbDa::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwOleDbDa> qodde;
	qodde.Attach(NewObj VwOleDbDa());		// ref count initially 1
	CheckHr(qodde->QueryInterface(riid, ppv));
}

/*----------------------------------------------------------------------------------------------
	If this method returns false for a particular tag, setting that tag will not result in the
	side-effect of caching the owner or owning field for the object property being set.
----------------------------------------------------------------------------------------------*/
bool VwOleDbDa::IsOwningField(PropTag tag)
{
	int nType;
	m_qmdc->GetFieldType(tag, &nType);
	return (nType == kcptOwningCollection ||
		nType == kcptOwningSequence ||
		nType == kcptOwningAtom);
}

/*----------------------------------------------------------------------------------------------
	${IVwOleDbDa#Close}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::Close()
{
	BEGIN_COM_METHOD;

	m_qmdc = NULL;
	m_qode = NULL;
	m_qacth = NULL;
	m_qwsf = NULL;
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the object ID that corresponds to the specified GUID.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::GetIdFromGuid(GUID * puid, HVO * phvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);

	return get_ObjFromGuid(*puid, phvo);
	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}

//:>********************************************************************************************
//:>    VwOleDbDa - ISetupVwOleDbDa Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISetupVwOleDbDa#Init}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::Init(IUnknown * punkOde /* IOleDbEncap*/,
	IUnknown * punkMdc /* IFwMetaDataCache */,
	IUnknown * punkWsf /* ILgWritingSystemFactory */,
	IActionHandler * pacth /* NULL is fine. */)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(punkOde);
	ChkComArgPtr(punkMdc);
	ChkComArgPtr(punkWsf);
	ChkComArgPtrN(pacth);

	HRESULT hr;
	// Don't call twice, and mutually exclusive with other Init.
	Assert(!m_qode);
	hr = punkOde->QueryInterface(IID_IOleDbEncap, (void **)&m_qode);
	if (FAILED(hr))
		return hr;
	hr = punkMdc->QueryInterface(IID_IFwMetaDataCache, (void **)&m_qmdc);
	if (FAILED(hr))
		return hr;
	hr = punkWsf->QueryInterface(IID_ILgWritingSystemFactory, (void **)&m_qwsf);
	if (FAILED(hr))
		return hr;
	CheckHr(m_qwsf->get_UserWs(&m_wsUser));

	StrUtil::InitIcuDataDir();		// Just in case...

	m_qacth = pacth; // It is ok if this is NULL.
	return S_OK;

	END_COM_METHOD(g_fact, IID_ISetupVwOleDbDa);
}

/*----------------------------------------------------------------------------------------------
	${ISetupVwOleDbDa#GetOleDbEncap}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::GetOleDbEncap(IUnknown ** ppode)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppode);

	if (!m_qode)
		return E_FAIL;
	IOleDbEncapPtr qode(m_qode);
	*ppode = qode.Detach();
	return S_OK;

	END_COM_METHOD(g_fact, IID_ISetupVwOleDbDa);
}


//:>********************************************************************************************
//:>    VwOleDbDa - IUnknown Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Standard COM method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::QueryInterface(REFIID riid, void **ppv)
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
	else if (riid == IID_IVwOleDbDa)
		*ppv = static_cast<IVwOleDbDa *>(this);
	else if (riid == IID_ISetupVwOleDbDa)
		*ppv = static_cast<ISetupVwOleDbDa *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(
			static_cast<IUnknown *>(static_cast<ISilDataAccess *>(this)), IID_ISilDataAccess);
//	*ppv = NewObj CSupportErrorInfo(this, IID_IVwCacheDa);
//	*ppv = NewObj CSupportErrorInfo(this, IID_IVwOleDbDa);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	VwOleDbDa methods used to load the database.
//:>********************************************************************************************

void VwOleDbDa::RecordVectorValue(bool fNotifyChange, HVO hvoVecBase, PropTag tagVec,
	Vector<HVO>& vhvo)
{
	int nDels = 0; // size of vector previously stored in cache, use 0 if not cached.
	if (fNotifyChange)
	{
		// Retrieve the old vector length, which we will need for PropChanged,
		// if the property is cached at all. If it is not cached, probably nothing is
		// using it, so nothing will care about PropChanged. It's just possible, though,
		// that something that is updating (e.g.) modify times for an owning object wants
		// to know the property changed, even though it isn't cached. Methods that want
		// to handle such notifications (of changes to uncached properties) should not
		// depend on the 'number deleted' argument.
		// We do NOT use get_VecSize here, since that will attempt to load the property
		// if it is not cached, and that could just conceivably lead to a recursive
		// call and stack overflow, if the VH load method were to pass fNotifyChange true.
		// It would also be rather wasteful to load the old value from the database
		// (though we could suppress that with a special value of auto load policy).
		ObjPropRec oprKey(hvoVecBase, tagVec);
		ObjSeq os;
		if (m_hmoprsobj.Retrieve(oprKey, &os))
			nDels = os.m_cobj;
	}
	CacheVecProp(hvoVecBase, tagVec, vhvo.Begin(), vhvo.Size());
	if (fNotifyChange)
		PropChanged(NULL, kpctNotifyAll, hvoVecBase, tagVec, 0, vhvo.Size(), nDels);
}
/*----------------------------------------------------------------------------------------------
	${IVwOleDbDa#Load}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::Load(BSTR bstrSqlStmt, IDbColSpec * pdcs, HVO hvoBase, int crowMax,
	IAdvInd * padvi, ComBool fNotifyChange)
{
	BEGIN_COM_METHOD;
	//  Ensure that parameters are ok.
	ChkComArgPtr(pdcs);
	ChkComArgPtrN(padvi);
	int cdcs;
	pdcs->Size(&cdcs);
	Assert(cdcs >= 0);
	if (cdcs < 0)
		return E_INVALIDARG;
	Assert(crowMax >= 0);
	if (crowMax < 0)
		return E_INVALIDARG;

	//  Execute the given SQL "select" command.
	IOleDbCommandPtr qodc;
	CheckHr(m_qode->CreateCommand(&qodc));
	if (padvi)
	{
		CheckHr(padvi->Step(0));
	}
	// Enhance SteveMc:  check for error due to server running out of memory, and tell user
	// about it?
	CheckHr(qodc->ExecCommand(bstrSqlStmt, knSqlStmtSelectWithOneRowset));
	if (padvi)
	{
		CheckHr(padvi->Step(0));
	}

	// Starting size of binary data buffer.
	const int kcbMaxData = 1024;
	// Starting size of unicode text buffer.
	const int kcchMaxData = 1024;
	// Number of bytes of the prgbData buffer that holds valid data.
	ULONG cbData;
	// Maximum size of prgbData buffer.
	ULONG cbDataMax = kcbMaxData;
	// Number of bytes of the rgchData buffer that holds valid data.
	ULONG cchData;
	// Maximum size of the rgchData buffer.
	ULONG cchDataMax = kcchMaxData;
	ComBool fIsNull;
	ComBool fMoreRows;
	// Object that is base of vector property.
	HVO hvoVecBase;
	PropTag tagVec; // tag to save vector
	// Index of (one and only) column of type koctObjVec (or koctObjVecExtra)
	int icolVec = -1;
	int nrows = 0;
	// Points to rgbData or vbData.Begin(), as appropriate
	byte * prgbData;
	// Used for unicode text.
	wchar * prgchData;
	byte rgbTimeStamp[8];
	Vector<HVO> vhvoBaseIds;
	vhvoBaseIds.Resize(cdcs);
	ITsStrFactoryPtr qtsf;
	ITsPropsFactoryPtr qtpf;
	// Used to buffer data from binary fields.
	Vector<byte> vbData;
	// Used for unicode text.
	Vector<wchar> vchData;
	// Accumulate objects for sequence property.
	Vector<HVO> vhvo;

	// Obtain pointer to TsString and TsProperty class factory interfaces.
	qtsf.CreateInstance(CLSID_TsStrFactory);
	qtpf.CreateInstance(CLSID_TsPropsFactory);

	// Maximum number of rows to input is undesignated so set it to the max.
	if (crowMax == 0)
	{
		crowMax = INT_MAX;
	}

	// Process each row.
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	prgbData = reinterpret_cast<byte *> (CoTaskMemAlloc(cbDataMax));
	prgchData = reinterpret_cast<wchar *> (CoTaskMemAlloc(cchDataMax * isizeof(OLECHAR)));
	int flid = 0;
	while (fMoreRows)
	{
		// Initialize to user interface ws for atomic strings without any characters or fmt.
		// wsX is the writing system obtained from one column (koctEnc) for use in a subsequent one.
		int wsX = m_wsUser;
		Assert(wsX);
		ITsStringPtr qtssVal;
		HVO hvoCurBase; // object whose property we will read.
		for (int icol = 0; icol < cdcs; icol++)
		{
			int nVal;
			int64 llnVal;
			HVO hvoVal;
			DBTIMESTAMP tim;
			GUID uid;
			PropTag tag;
			int ws; // writing system obtained from pdcs for this column, used if no previous koctEnc.

			//  Determine the current "base" hvo.
			//  This is for "reference sequences" (as described in the FW conceptual model).
			int icolId;
			CheckHr(pdcs->GetBaseCol(icol, &icolId));
			CheckHr(pdcs->GetTag(icol, &tag));
			CheckHr(pdcs->GetWs(icol, &ws));
			if (icolId == 0)
			{
				hvoCurBase = hvoBase;
			}
			else
			{
				//  Must refer to a previous column.
				//  Use <= because m_icolID is 1-based.  Thus, if it equals i, it refers to
				//  the previous column.
				Assert(icolId <= icol);
				hvoCurBase = vhvoBaseIds[icolId - 1];
			}

			//  Put the value of the column in the cache depending on the type.
			int oct;
			int octPrev;
			CheckHr(pdcs->GetDbColType(icol, &oct));
			// If we have no valid base HVO, give up. This can legitimately happen when, for
			// example, one column is following an atomic object property and it is missing,
			// while another column is trying to get a field of that object (if it exists).
			// Of course, a column can still be a base for other data even if it has no source
			// (it may not represent a property of some object at all).
			if (hvoCurBase == 0 && oct != koctBaseId)
			{
				// We're not going to bother reading data from this column, since we have
				// no base object to set the property of. However, vhvoBaseIds[icol]
				// may have a stale value from a previous row. If we leave it, there is
				// a danger that a subsequent column will be read (probably null) and
				// used to overwrite a value we previously read in an earlier row.
				// So we basically suppress reading any information that uses the
				// current column as a base. (On almost all queries of the type used for
				// load, if the base column is zero or null, the value we would be reading
				// for col icol is null anyway.)
				vhvoBaseIds[icol] = 0;
				continue;
			}
			switch (oct)
			{
			case koctObjVecExtra:
				// Need to implement this if we ever want to use it.
				// fall through
			default:
				Assert(false);
				ThrowHr(WarnHr(E_UNEXPECTED));

			case koctGuid:
				//  Guids.
				CheckHr(qodc->GetColValue(icol + 1, (BYTE*)&uid,
				sizeof(GUID), &cbData, &fIsNull, 0));
				if (fIsNull)
					uid = GUID_NULL;
				CheckHr(CacheGuidProp(hvoCurBase, tag, uid));
				if (fNotifyChange)
					CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, 0, 1, 1));
				break;

			case koctInt:
				//  Integers.
				nVal = 0;
				CheckHr(qodc->GetColValue(icol + 1, (BYTE*)&nVal,
					sizeof(int), &cbData, &fIsNull, 0));
				// Check for cases where a value type is TinyInt, or other type that has a
				// size less than Int (4 bytes).
				switch (cbData)
				{
				case 4: // We got the full 4 bytes.
					break;
				case 1: // We got only one byte, sign-extend it.
					nVal = * (signed char *)(&nVal);
					break;
				case 2: // Sign-extend the two bytes we got.
					nVal = * (short *) (&nVal);
					break;
				case 0: // Null--play safe.
					// TODO JohnT(KenZ): We need to do something better than this since we
					// really need to know the difference between 0 and NULL in Owner$, etc.
					// JohnT: why? We never use 0 as an object ID, so NULL and 0 are equivalent
					// for any object property...
					nVal = 0;
					break;
				default:
					Assert(false);
					break;
				}

				// If the value in the database is NULL, zero will be cached for it.  Even
				// though this will take up a bit of extra space in the hash map, it will save
				// us the trouble of querying the database in the get_Int method.
				// TODO 1725 (PaulP): Eventually, we should have a way to represent NULL's in
				// the hash maps.
				CheckHr(CacheIntProp(hvoCurBase, tag, nVal));
				if (fNotifyChange)
					CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, 0, 1, 1));
				break;

			case koctInt64:
				{
					//  64-bit integers.
					llnVal = 0;
					uint64 revVal;
					CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(&revVal),
					sizeof(uint64), &cbData, &fIsNull, 0));

					// Unfortunately this produces a long with the bytes reversed.
					uint64 result = 0;
					for (int i = 0; i < 8; i++)
					{
						uint64 b = (revVal >> i * 8) % 0x100;
						result += b << ((7 - i) * 8);
					}
					llnVal = (int64) result;

					// If the value in the database is NULL, zero will be cached for it.  Even
					// though this will take up a bit of extra space in the hash map, it will save
					// us the trouble of querying the database in the get_Int64 method.
					// TODO 1725 (PaulP): Eventually, we should have a way to represent NULL's in
					// the hash maps.
					CheckHr(CacheInt64Prop(hvoCurBase, tag, llnVal));
					if (fNotifyChange)
						CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, 0, 1, 1));
				}
				break;

			case koctTime:
				//  16 byte DBDATETIME struct.
				CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(&tim),
					sizeof(DBTIMESTAMP), &cbData, &fIsNull, 0));
				if (!fIsNull)
				{
					SilTimeInfo sti;
					sti.year = tim.year;
					sti.ymon = tim.month;
					sti.mday = tim.day;
					sti.hour = tim.hour;
					sti.min = tim.minute;
					sti.sec = tim.second;
					sti.msec = tim.fraction/1000000;
					SilTime stim(sti);
					CheckHr(CacheTimeProp(hvoCurBase, tag, stim));
					if (fNotifyChange)
						CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, 0, 1, 1));
				}
				break;

			case koctUnicode:
			case koctMltAlt:
				//  Unicode strings.
				// NOTE: koctMltAlt here is only used when a single writing system is being
				// processed. For multiple encodings, koctMlaAlt should be used.

				CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(prgchData),
				cchDataMax * isizeof(OLECHAR), &cchData, &fIsNull, 0));
				cchData /= isizeof(OLECHAR);
				//  If buffer was too small, reallocate and try again.
				if ((cchData > cchDataMax) && (!fIsNull))
				{
					cchDataMax = cchData;
					CoTaskMemFree(prgchData);
					prgchData = reinterpret_cast<wchar *>
						(CoTaskMemAlloc(cchDataMax * isizeof(OLECHAR)));
					CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>
						(prgchData), cchDataMax * isizeof(OLECHAR), &cchData, &fIsNull, 0));
					cchData /= isizeof(OLECHAR);
				}

				// If the value in the database is NULL, cache an empty string.  Even though
				// this will take up a bit of extra space in the hash map, it will save us the
				// trouble of querying the database in the get_* method.
				// TODO 1725 (PaulP): Eventually, we should have a way to represent NULL's in
				// the hash maps.
				if (oct == koctUnicode)
				{
					CheckHr(CacheUnicodeProp(hvoCurBase, tag, prgchData, cchData));
					if (fNotifyChange)
						CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, 0, 1, 1));
				}
				else
				{
					Assert(ws); // Need a valid writing system.
					if (cchData == 0)
						CheckHr(qtsf->EmptyString(ws, &qtssVal));
					else
						CheckHr(qtsf->MakeStringRgch(prgchData, cchData, ws, &qtssVal));
					CheckHr(CacheStringAlt(hvoCurBase, tag, ws, qtssVal));
					if (fNotifyChange)
						CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, ws, 1, 1));
				}
				break;

			case koctString:
			case koctMlaAlt:
			case koctMlsAlt:
				// NOTE: koctMlsAlt is only used when a single writing system is being
				// processed. For multiple encodings, koctMlaAlt should be used.
				//  The next column(s) depend on the type of string.
				CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(prgchData),
					cchDataMax * isizeof(OLECHAR), &cchData, &fIsNull, 0));
				cchData /= isizeof(OLECHAR);
				//  If buffer was too small, reallocate and try again.
				if ((cchData > cchDataMax)  && (!fIsNull))
				{
					cchDataMax = cchData;
					CoTaskMemFree(prgchData);
					prgchData = reinterpret_cast<wchar *>
						(CoTaskMemAlloc(cchDataMax * isizeof(OLECHAR)));
					CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>
						(prgchData), cchDataMax * isizeof(OLECHAR), &cchData, &fIsNull, 0));
					cchData /= isizeof(OLECHAR);
				}

#ifdef DEBUG
				if (!fIsNull)
				{
					// For these strings, we leave the data in prgchData buffer (and cchData
					// value), and process it when we get to the koctFmt column.
					// For koctMlaAlt, there is are intervening koctFlid and koctEnc columns.
					int octNext;
					PropTag tagNext;
					int encNext;
					CheckHr(pdcs->GetDbColType(icol + 1, &octNext));
					CheckHr(pdcs->GetTag(icol + 1, &tagNext));
					CheckHr(pdcs->GetWs(icol + 1, &encNext));

					switch (oct)
					{
					case koctString:
						// For Strings we must have the text followed by format
						Assert(icol < cdcs - 1 && octNext == koctFmt);
						Assert(tag == tagNext);
						break;
					case koctMlsAlt:
						// For single MultiStrings we must have the text followed by format
						Assert(icol < cdcs - 1 && octNext == koctFmt);
						Assert(tag == tagNext);
						Assert(ws); // Need a valid writing system.
						break;
					case koctMlaAlt:
						// For multi strings/text we need text, flid, writing system, and
						// format.
						int octNext2;
						int octNext3;
						CheckHr(pdcs->GetDbColType(icol + 2, &octNext2));
						CheckHr(pdcs->GetDbColType(icol + 3, &octNext3));
						int tagNext2;
						int tagNext3;
						CheckHr(pdcs->GetTag(icol + 2, &tagNext2));
						CheckHr(pdcs->GetTag(icol + 3, &tagNext3));
						Assert(icol < cdcs - 3 && octNext == koctFlid);
						Assert(tag == tagNext);
						Assert(octNext2 == koctEnc);
						Assert(tag == tagNext2);
						Assert(octNext3 == koctFmt);
						Assert(tag == tagNext3);
						break;
					default:
						Assert(false);
						break;
					}
				}
#endif
				break;

			case koctFlid:
				// Get the property id into flid, then process the next column.
				CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(&flid),
					sizeof(int), &cbData, &fIsNull, 0));
				Assert(flid);
				break;

			case koctEnc:
				//  The previous column has to be a flid, otherwise the
				//  client code has given us either a bogus SQL statement or bogus ocs's.
				//  Note that in the last iteration, we checked the tag already.
				CheckHr(pdcs->GetDbColType(icol - 1, &octPrev));
				Assert(icol > 0 && octPrev == koctFlid);
				// Get the alternative writing system into ws, then process the next column.
				CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(&wsX),
					sizeof(int), &cbData, &fIsNull, 0));
				Assert(wsX);
				break;

			case koctFmt:
				{
					// The previous column has to be a string, ws, or single multistring,
					// otherwise the client code has given us either a bogus SQL statement or
					// bogus ocs's. Note that in the last iteration, we checked the tag already.
					CheckHr(pdcs->GetDbColType(icol - 1, &octPrev));
					Assert(icol > 0 && (octPrev == koctString ||
						octPrev == koctEnc ||
						octPrev == koctMlsAlt));
					CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(prgbData),
						cbDataMax, &cbData, &fIsNull, 0));
					//  If buffer was too small, reallocate and try again.
					if ((cbData > cbDataMax) && (!fIsNull))
					{
						cbDataMax = cbData;
						CoTaskMemFree(prgbData);
						prgbData = reinterpret_cast<byte *> (CoTaskMemAlloc(cbDataMax));
						CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>
							(prgbData), cbDataMax, &cbData, &fIsNull, 0));
					}

					int cbDataInt;
					int cchDataInt;
					cbDataInt = cbData;
					cchDataInt = cchData;
					int wsToUse = wsX; // default: use ws specified in previous koctEnc column, or default for non-multistrings
					if (octPrev == koctMlsAlt)
					{
						// Get the writing system from koctMlsAlt, not the one from koctFmt,
						// thus koctFmt writing system is ignored.
						CheckHr(pdcs->GetWs(icol - 1, &wsToUse));
						Assert(wsToUse);
					}
					else if (octPrev == koctString)
					{
						// For custom fields, use the designated writing system to create an empty
						// string.  See LT-6650 (secondary complaint).
						int wsField = 0;
						CheckHr(pdcs->GetWs(icol - 1, &wsField));
						switch (wsField)
						{
						case kwsAnal:
							wsToUse =AnalWss()[0];
							break;
						case kwsVern:
							wsToUse = VernWss()[0];
							break;
						default:
							if (wsField > 0)
								wsToUse = wsField;
							break;
						}
					}
					if (cchDataInt == 0 && cbDataInt == 0)
					{
						// No characters or formatting.
						CheckHr(qtsf->EmptyString(wsToUse, &qtssVal));
					}
					else if (cbDataInt == 0)
					{
						// Characters but no formatting.
						CheckHr(qtsf->MakeStringRgch(prgchData, cchDataInt, wsToUse, &qtssVal));
					}
					else
					{
						// Characters and formatting.
						CheckHr(qtsf->DeserializeStringRgch(prgchData, &cchDataInt, prgbData,
							&cbDataInt, &qtssVal));
					}
					if (octPrev == koctEnc) // koctMlaAlt
					{
						CheckHr(CacheStringAlt(hvoCurBase, flid, wsX, qtssVal));
						if (fNotifyChange)
							CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, flid, wsX, 1, 1));
					}
					else if (octPrev == koctString)
					{
						CheckHr(CacheStringProp(hvoCurBase, tag, qtssVal));
						if (fNotifyChange)
							CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, 0, 1, 1));
					}
					else // koctMlsAlt
					{
						CheckHr(CacheStringAlt(hvoCurBase, tag, wsToUse, qtssVal));
						if (fNotifyChange)
							CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, wsToUse, 1, 1));
					}
				}
				break;

			case koctObj:
			case koctBaseId:
			case koctObjOwn:
				//  This is for "atomic references" (which are int columns in database tables)
				//  and for "base id's" (which I believe are things like the "src" column
				//  of "reference sequence or collection" joiner tables).
				CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(&hvoVal),
					sizeof(HVO), &cbData, &fIsNull, 0));
				if (fIsNull)
				{
					// Treat null as zero.
					hvoVal = 0;
				}
				if (oct != koctBaseId) // Obj or ObjOwn
				{
					HVO hvoOrig = 0;
					ComBool fInCache = false;
					CheckHr(get_IsPropInCache(hvoCurBase, tag, kcptReferenceAtom, 0, &fInCache));
					if (fInCache)
					{
						// get the original value.
						CheckHr(get_ObjectProp(hvoCurBase, tag, &hvoOrig));
					}
					// update the value in the cache.
					CheckHr(CacheObjProp(hvoCurBase, tag, hvoVal));
					if (oct == koctObjOwn && hvoVal != 0)
					{
						CheckHr(CacheObjProp(hvoVal, kflidCmObject_Owner, hvoCurBase));
						CheckHr(CacheObjProp(hvoVal, kflidCmObject_OwnFlid, tag));
						if (fNotifyChange)
						{
							CheckHr(PropChanged(NULL, kpctNotifyAll, hvoVal, kflidCmObject_Owner, 0, 1, 1));
							CheckHr(PropChanged(NULL, kpctNotifyAll, hvoVal, kflidCmObject_OwnFlid, 0, 1, 1));
						}
					}
					if (fNotifyChange && hvoCurBase != 0 && hvoVal != hvoOrig)
					{
						// We want to issue a prop changed even if the property wasn't in the cache previously
						// See the comment in RecordVectorValue for details of this policy.
						CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, 0, hvoVal != 0, hvoOrig != 0));
					}
				}
				vhvoBaseIds[icol] = hvoVal;
				break;

			case koctObjVec:
			case koctObjVecOwn:
				CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(&hvoVal),
					sizeof(HVO), &cbData, &fIsNull, 0));
				vhvoBaseIds[icol] = hvoVal;
				int tagVecRow;
				CheckHr(pdcs->GetTag(icol, &tagVecRow));
				if (tagVecRow == 0)
				{
					// tag should come from previous column
					CheckHr(pdcs->GetDbColType(icol - 1, &octPrev));
					Assert(icol > 0 && octPrev == koctFlid);
					tagVecRow = flid; // saved while processing previous column
				}
				//  See if there has been a change in the base column, if so, record value
				//  and start a new one.
				if (icolVec < 0)
				{
					// First iteration, ignore previous object
					icolVec = icol;
					hvoVecBase = hvoCurBase;
					tagVec = tagVecRow;
				}
				else
				{
					// Only one vector column allowed!
					Assert(icolVec == icol);
					if (hvoVecBase != hvoCurBase || tagVec != tagVecRow)
					{
						// Started a new vector! Record the old one
						RecordVectorValue(fNotifyChange, hvoVecBase, tagVec, vhvo);
						// clear the list out and note new base object
						vhvo.Clear();
						hvoVecBase = hvoCurBase;
						tagVec = tagVecRow;
					}
				}
				// We can get null, typically, from a left outer join aimed at making sure there is at least
				// one row for each source object. Using such a query ensures that every source object has a
				// value cached. Anything that somehow returns a zero will be considered not a value and
				// also ignored (except that an empty value will be cached if no other items are encountered).
				if (hvoVal != 0 && !fIsNull)
				{
					vhvo.Push(hvoVal);
					if (oct == koctObjVecOwn)
					{
						CheckHr(CacheObjProp(hvoVal, kflidCmObject_Owner, hvoVecBase));
						CheckHr(CacheObjProp(hvoVal, kflidCmObject_OwnFlid, tagVecRow));
						if (fNotifyChange)
						{
							CheckHr(PropChanged(NULL, kpctNotifyAll, hvoVal, kflidCmObject_Owner, 0, 1, 1));
							CheckHr(PropChanged(NULL, kpctNotifyAll, hvoVal, kflidCmObject_OwnFlid, 0, 1, 1));
						}
					}
				}
				break;

			case koctTtp:
				{ // BLOCK
					CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(prgbData),
						cbDataMax, &cbData, &fIsNull, 0));
					//  If buffer was too small, reallocate and try again.
					if ((cbData > cbDataMax) && (!fIsNull))
					{
						cbDataMax = cbData;
						CoTaskMemFree(prgbData);
						prgbData = reinterpret_cast<byte *> (CoTaskMemAlloc(cbDataMax));
						CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>
							(prgbData), cbDataMax, &cbData, &fIsNull, 0));
					}

					int cbDataInt;
					ITsTextPropsPtr qttp;
					if ((!fIsNull) && (cbData > 0))
					{
						// We got some data, try to interpret it.
						cbDataInt = cbData;
						CheckHr(qtpf->DeserializePropsRgb(prgbData, &cbDataInt, &qttp));
					}
					CheckHr(CacheUnknown(hvoCurBase, tag, qttp));
					if (fNotifyChange)
						CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, 0, 1, 1));
				}
				break;
			case koctBinary: // arbitrary binary data interpreted by the app
				{ // BLOCK
					CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(prgbData),
						cbDataMax, &cbData, &fIsNull, 0));
					//  If buffer was too small, reallocate and try again.
					if ((cbData > cbDataMax) && (!fIsNull))
					{
						cbDataMax = cbData;
						CoTaskMemFree(prgbData);
						prgbData = reinterpret_cast<byte *> (CoTaskMemAlloc(cbDataMax));
						CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>
							(prgbData), cbDataMax, &cbData, &fIsNull, 0));
					}
					CheckHr(CacheBinaryProp(hvoCurBase, tag, prgbData, cbData));
					if (fNotifyChange)
						CheckHr(PropChanged(NULL, kpctNotifyAll, hvoCurBase, tag, 0, 1, 1));
				}
				break;
			case koctTimeStampIfMissing:
				{
					StrAnsi staT;
					if (m_hmostamp.Retrieve(hvoCurBase, &staT))
						break; // already cached, do nothing.
				}
				// FALL TRHOUGH
			case koctTimeStamp:
				CheckHr(qodc->GetColValue(icol + 1, reinterpret_cast <BYTE *>(rgbTimeStamp), 8,
					&cbData, &fIsNull, 0));
				if ((!fIsNull) && (cbData == 8))
				{
					StrAnsi sta(reinterpret_cast<const char *>(rgbTimeStamp), 8);
					m_hmostamp.Insert(hvoCurBase, sta, true);	// Allow overwrites.
				}
				else
				{
					// If the time stamp does not exist or the value is null, the database
					// is corrupted somehow.
					Assert(0);
					m_hmostamp.Delete(hvoCurBase);
				}
				break;
			}
		}
		qodc->NextRow(&fMoreRows);
		if (padvi && !(nrows & 0x7))
			CheckHr(padvi->Step(8));

		// Stop if we have processed the requested number of rows.
		nrows++;
		if (nrows >= crowMax)
			break;
	}

	// If we are processing a vector, we need to fill in the last occurrence
	if (icolVec >= 0)
	{
		RecordVectorValue(fNotifyChange, hvoVecBase, tagVec, vhvo);
	}
	else
	{
		if (hvoBase)
		{
			// It may be that there was a "koctObjVec" type column specified, however, if
			// there are no rows in the rowset, this will get skipped.
			// It may also be that there was a koct
			for (int icol = 0; icol < cdcs; icol++)
			{
				int oct;
				pdcs->GetDbColType(icol, &oct);
				if (oct == koctObjVec)
				{
					// This means that there was a vector specified, but no rows were
					// returned.  We should therefore clear out the vector, so data that
					// is not in the database does not get pulled from the cache.
					int tagVecT;
					pdcs->GetTag(icol, &tagVecT);
					Vector<HVO> vhvoT; // empty one to pass
					RecordVectorValue(fNotifyChange, hvoBase, tagVecT, vhvoT);
				}
			}
		}
	}
	if (hvoBase && nrows == 0)
	{
		// There were no rows at all. This can readily happen, for example, when looking for
		// an atomic property of an object which owns nothing. If there are any object properties
		// expected, set the properties to null
		for (int icol = 0; icol < cdcs; icol++)
		{
			int oct;
			pdcs->GetDbColType(icol, &oct);
			if (oct == koctObj || oct == koctObjOwn)
			{
				int tag;
				pdcs->GetTag(icol, &tag);
				CheckHr(CacheObjProp(hvoBase, tag, 0));
				if (fNotifyChange)
					CheckHr(PropChanged(NULL, kpctNotifyAll, hvoBase, tag, 0, 0, 1));
			}
		}
	}

	CoTaskMemFree(prgbData);
	CoTaskMemFree(prgchData);

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwOleDbDa#Save}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::Save()
{
	BEGIN_COM_METHOD;

	// Commit any actions that have been made so far.
	if (m_qacth)
	{
		m_qacth->Commit();
	}
	// If there is any outstanding database activity that didn't go through the action
	// handler, make sure it is committed as well.
	ComBool fOpen;
	CheckHr(m_qode->IsTransactionOpen(&fOpen));
	if (fOpen)
		m_qode->CommitTrans();

	// Clear the two Sets containing records of modified properties and MBA's, and the set
	// of deleted objects.
	m_soprMods.Clear();
	m_soperMods.Clear();
	m_shvoDeleted.Clear();

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwOleDbDa#Clear}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::ClearAllData()
{
	BEGIN_COM_METHOD;

	// We don't want to reset these: at least m_hvoNextDummy. This method is called
	// during FullRefresh and some synchronizing. The problem I encountered when it was
	// being cleared was doing a promote in a single window, then opening a second window
	// and dragging the promoted item back to the original location. Since promote calls
	// this method, it was resetting m_hvoNextDummy so that the second window was using
	// the same value as the first window. Then when we tried to delete an item from the
	// cache for both windows, it tried deleting the item twice from the same vector instead\
	// of different vectors.
	//m_hvoNext = 100000000;
	//m_hvoNextDummy = -1000000;

	// Clear timestamps.
	m_hmostamp.Clear();

	// Clear everything else.
	CheckHr(SuperClass::ClearAllData());

	// Since we're clearing all the data, new 'load everything' requests are relevant
	// even if we've done them recently.
	for (int i = 0; i < kcRecentAutoloads; i++)
		m_rgalkRecentAutoLoads[i].tag = 0; // enough to ensure nothing matches.

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwOleDbDa#CheckTimeStamp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::CheckTimeStamp(HVO hvo)
{
	BEGIN_COM_METHOD;
	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	Assert(hvo != 0);
	if (!hvo)
		return E_INVALIDARG;

	// HVO's created by the CreateDummyID method should be negative.  Simply return OK for this
	// since the ID value does not exist in the database.
	if (hvo < 0)
		return S_OK;

	byte rgbStampCache[8];
	byte rgbStampDb[8];

	HRESULT hr = E_FAIL;
	StrAnsi sta;
	if (m_hmostamp.Retrieve(hvo, &sta))
	{
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG luSpaceTaken;
		StrUni stuSql;

		// Form SQL command.
		stuSql.Format(L"select [UpdStmp] from [CmObject] where [Id]=%d", hvo);
		IOleDbCommandPtr qodc;
		hr = m_qode->CreateCommand(&qodc);
		hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset);
		hr = qodc->GetRowset(1);
		hr = qodc->NextRow(&fMoreRows);
		if (fMoreRows)
		{
			luSpaceTaken = 0;
			hr = qodc->GetColValue(1, reinterpret_cast <BYTE *>(&rgbStampDb), 8,
				&luSpaceTaken, &fIsNull, 0);
			if ((luSpaceTaken != 8) || fIsNull)
			{
				// If this happens, it means that the time stamp (and thereby, the database)
				// has been corrupted somehow.
				Assert(0);
				return E_FAIL;
			}

			// There shouldn't be any more rows, however, just to be safe, simply exhaust
			// the rowset so it doesn't cause any trouble.
			hr = qodc->NextRow(&fMoreRows);
		}
		else
		{
			// The record has probably been deleted by someone else.
			StrApp strM(kstridRecLockDeleted);
			StrApp strT(kstridRecLockDeletedTitle);
			::MessageBox(NULL, strM.Chars(), strT.Chars(), MB_ICONEXCLAMATION | MB_OK);
			return S_FALSE;
		}

		// Compare the time stamp from the database with the one in the cache.
		::memcpy(&rgbStampCache, sta.Chars(), 8);
		int n = memcmp(rgbStampCache, rgbStampDb, 8);
		if (n == 0)
		{
			return S_OK;
		}

		// Another user (or application) has modified this record since we last cached it.
		StrApp strM(kstridRecLockModified);
		StrApp strT(kstridRecLockModifiedTitle);
		int nReply = ::MessageBox(NULL, strM.Chars(), strT.Chars(),
			MB_ICONEXCLAMATION | MB_YESNO | MB_DEFBUTTON2);
		if (nReply == IDYES)
		{
			return S_OK;
		}
		return S_FALSE;
	}

	// If we do not have a previous time stamp loaded in the cache for a given record, the
	// client code probably forgot to load the TimeStamp along with the other data.
	Assert(0);
	return E_FAIL;

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}


/*----------------------------------------------------------------------------------------------
	${IVwOleDbDa#SetTimeStamp}
	This is a special case. Undo and Redo don't set the time stamp to what it was before
	or after the original change; they both set it to the CURRENT time (when the Undo or Redo
	happens). This seems safest, since possibly some other change happened to the object
	that has not been undone; with this approach, the update time should always be the real
	last time it was modified. The downside is that version control tools might see a modify
	time that reflects a canceled change.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetTimeStamp(HVO hvo)
{
	BEGIN_COM_METHOD;
	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	Assert(hvo != 0);
	if (!hvo)
		return E_INVALIDARG;

	//  Update the Int in the database.
	IOleDbCommandPtr qodc;
	StrUni stuSql;

	//  Form the SQL query.
	CheckHr(m_qode->CreateCommand(&qodc));
	stuSql.Format(L"update [CmObject] set [UpdDttm]=GetDate() where id=%d", hvo);

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if (m_qacth)
	{
		// Create the SqlUndoAction and set the RedoCommand
		ISqlUndoActionPtr qsqlua;
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		// Undo and Redo are EXACTLY the same (see comment on method).
		qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr());
		qsqlua->AddUndoCommand(m_qode, qodc, stuSql.Bstr());

		// Set the "reload" SQL command.
		StrUni stuReload;
		stuReload.Format(L"select [id], [UpdStmp] from [CmObject] "
			L" where [id] = %d", hvo);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctTimeStamp, 1, 0, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));

		// The only requirement for undoing/redoing this is that the object exists.
		StrUni stuVerify;
		stuVerify.Format(L"select count(id) from CmObject where id = %d", hvo);
		CheckHr(qsqlua->VerifyUndoable(m_qode, qodc, stuVerify.Bstr()));
		CheckHr(qsqlua->VerifyRedoable(m_qode, qodc, stuVerify.Bstr()));

		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	// Actually execute the command.
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}

/*----------------------------------------------------------------------------------------------
	Load the current timestamp for the object, but only if it isn't already loaded. This is
	used when loading an individual property, knowing that other properties may have been
	loaded earlier, just possibly with an earlier timestamp.
----------------------------------------------------------------------------------------------*/
void VwOleDbDa::CacheCurrTimeStampIfMissing(HVO hvo)
{
	StrAnsi sta;
	if (m_hmostamp.Retrieve(hvo, &sta))
		return;
	CheckHr(CacheCurrTimeStamp(hvo));
}

/*----------------------------------------------------------------------------------------------
	${IVwOleDbDa#CacheCurrTimeStamp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::CacheCurrTimeStamp(HVO hvo)
{
	BEGIN_COM_METHOD;
	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	Assert(hvo != 0);
	if (!hvo)
		return E_INVALIDARG;
	// Can't cache a meaningful timestamp for a dummy object (and the retrieve method also checks).
	if (IsDummyId(hvo))
		return S_OK;

	byte rgbStampDb[8];
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG luSpaceTaken;

	try
	{
		// Form SQL command.
		StrUni stuSql;
		stuSql.Format(L"select [UpdStmp] from [CmObject] where [Id]=%d", hvo);
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(1));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			luSpaceTaken = 0;
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&rgbStampDb), 8,
				&luSpaceTaken, &fIsNull, 0));
			if ((luSpaceTaken != 8) || fIsNull)
			{
				// The time stamp has been corrupted somehow.
				Assert(0);
				return E_FAIL;
			}

			// There shouldn't be any more rows, however, just to be safe, simply exhaust
			// the rowset so it doesn't cause any trouble.
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		else
		{
			// The record has been deleted.
			m_hmostamp.Delete(hvo);
			return S_OK;
		}

		// Cache the info.
		StrAnsi sta(reinterpret_cast<const char *>(rgbStampDb), 8);
		m_hmostamp.Insert(hvo, sta, true);	// Allow overwrites.
	}
	catch(...)
	{
		// Ignore errors.  (!?)
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}




//:>********************************************************************************************
//:>	VwOleDbDa methods used to retrieve object REFERENCE information.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	This method is called when we are doing kalpLoadAllOfClassForReadOnly. If the indicated
	property is already recorded as fully loaded, we return true. Otherwise, we record it as
	fully loaded and return false.
----------------------------------------------------------------------------------------------*/
int VwOleDbDa::TestAndNoteLoadAllForReadOnly(int tag, int ws, int clsid)
{
	AutoloadKey alk;
	alk.tag = tag;
	alk.ws = ws;
	alk.clsid = clsid;
	if (m_salkLoadedProps.IsMember(alk))
		return true;
	m_salkLoadedProps.Insert(alk);
	return false;
}
/*----------------------------------------------------------------------------------------------
	This method is overridden from ${VwCacheDa} to provide for lazy retrieval of data.  If the
	data requested is not found in the cache (because it was cleared or was never loaded), a
	simple query is made of the database to retrieve the value and cache it.  If it is not
	found after that, an error value is returned.  (Look for the word "lazy retrieval" in the
	document ~FWROOT/doc/database/ActionHandler.htm for more information.)

	${ISilDataAccess#ObjectProp}, ${VwCacheDa#get_ObjectProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_ObjectProp(HVO hvo, PropTag tag, HVO * phvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);

	HRESULT hr = SuperClass::get_ObjectProp(hvo, tag, phvo);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(hvo))
		return S_OK;
	// autoload

	if (tag == kflidCmObject_Owner)
	{
		CheckHr(LoadObjInfo(hvo));
	}
	else
	{
		//  Get the field$ info from the tag.
		SmartBstr sbstrClsName;
		SmartBstr sbstrFldName;
		CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
		CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

		// Form the SQL query and execute. Unfortunately, it depends on whether the prop is
		// owning or not.
		int cpt;
		CheckHr(m_qmdc->GetFieldType(tag, &cpt));
		StrUni stuSql;
		const OLECHAR * pszClassToLoad = sbstrClsName.Chars();
		SmartBstr sbstrInstClassName;
		int clsid = 0; // use if load all of base class.
		if (m_alpAutoloadPolicy == kalpLoadForAllOfObjectClass)
		{
			// class to load is class of hvo
			CheckHr(this->get_IntProp(hvo, kflidCmObject_Class, &clsid));
			CheckHr(m_qmdc->GetClassName((uint)clsid, &sbstrInstClassName));
			// We probably need a new field name too!
			unsigned long flid = 0;
			CheckHr(m_qmdc->GetFieldId2(clsid, sbstrFldName, TRUE, &flid));
			if (flid != 0)
				pszClassToLoad = sbstrInstClassName.Chars();
		}
		if (m_alpAutoloadPolicy == kalpLoadAllOfClassForReadOnly)
		{
			// Don't use the class ID, makes the query much less efficient. The OwnFlid$ will
			// restrict it pretty well usually.
			if (TestAndNoteLoadAllForReadOnly(tag, 0, 0))
				return S_OK; // null value was not autoloaded.
		}
		if (cpt == kcptOwningAtom)
		{
			if (m_alpAutoloadPolicy == kalpLoadAllOfClassForReadOnly)
			{
					stuSql.Format(L"select owner$, id from CmObject where OwnFlid$ = %d", tag);
			}
			else
			{
				stuSql.Format(L"select src.id, dst.id, src.UpdStmp from %s_ src"
					L" left outer join CmObject dst on dst.owner$ = src.id and dst.OwnFlid$ = %d",
					pszClassToLoad, tag);
			}
		}
		else
		{
			if (m_alpAutoloadPolicy == kalpLoadAllOfClassForReadOnly)
			{
				ULONG clsidBase;
				CheckHr(m_qmdc->GetOwnClsId((ULONG)tag, &clsidBase));
				// The difference between these two is the _ after the second %s, which allows us to pick up
				// a base class field, but makes the query more expensive.
				if (clsidBase == (ULONG)clsid)
				{
					stuSql.Format(L"select id, [%s] from [%s] src",
						sbstrFldName.Chars(), pszClassToLoad);
				}
				else
				{
					stuSql.Format(L"select id, [%s] from [%s_] src",
						sbstrFldName.Chars(), pszClassToLoad);
				}
			}
			else
			{
				stuSql.Format(L"select id, [%s], UpdStmp from [%s_] src",
					sbstrFldName.Chars(), pszClassToLoad);
			}
		}

		if (m_alpAutoloadPolicy == kalpLoadForThisObject || TestAndNoteRecentAutoloads(tag, 0, clsid))
			stuSql.FormatAppend(L" where src.id = %d", hvo);

		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec);
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(cpt == kcptOwningAtom ? koctObjOwn : koctObj, 1, tag, 0));
		if (m_alpAutoloadPolicy != kalpLoadAllOfClassForReadOnly)
			CheckHr(qdcs->Push(koctTimeStampIfMissing, 1, 0, 0));
		CheckHr(Load(stuSql.Bstr(), qdcs, 0, INT_MAX, NULL, false));
	}

	// And get the result.

	ObjPropRec oprKey(hvo, tag);
	m_hmoprobj.Retrieve(oprKey, phvo);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}
/*----------------------------------------------------------------------------------------------
	This method is an internal version of get_ObjectProp. Unlike get_ObjectProp, it does not
	crash if the tag does not identify an object property, just returns zero.
----------------------------------------------------------------------------------------------*/
HVO VwOleDbDa::GetObjPropCheckType(HVO hvo, PropTag tag)
{
	HVO hvoT = SuperClass::GetObjPropCheckType(hvo, tag);
	if (hvoT != 0)
		return hvoT;
	int type;
	CheckHr(m_qmdc->GetFieldType(tag, &type));
	if (type != kcptOwningAtom && type != kcptReferenceAtom)
		return 0; // not an atomic property at all.
	CheckHr(get_ObjectProp(hvo, tag, &hvoT));
	return hvoT;
}

/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#VecItem}, ${VwCacheDa#get_VecItem}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_VecItem(HVO hvo, PropTag tag, int index, HVO * phvo)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(phvo);

	HRESULT hr;
	IgnoreHr(hr = SuperClass::get_VecItem(hvo, tag, index, phvo));
	if (hr != E_FAIL && hr != E_INVALIDARG)
		return hr;

	// presume cache miss if base class returns E_FAIL.
	// REVIEW (EberhardB): I'm not sure why we don't return on E_INVALIDARG.
	// If hr == E_INVALIDARG it is possible that we have a virtual property. In that case
	// LoadVecProp will fail. I'm not sure if there are situations where we get E_INVALIDARG
	// and we can recover by calling LoadVecProp
	// JohnT: it's unlikely, but possible if the cache is stale. For example, a client might
	// have added an object to a vector using some SQL process that bypasses the cache, leaving
	// it out of date. If the client assumes the new object will be found, it might use an
	// out-of-range index. Reloading would then fix things.
	// However, you are right, calling LoadVector is a mistake if the property is virtual.
	if (hr == E_INVALIDARG)
	{
		IVwVirtualHandlerPtr qvh;
		if (m_hmtagvh.Retrieve(tag, qvh))
			return hr; // virtual, no good trying to load from database.
	}
	LoadVecProp(hvo, tag);
	return SuperClass::get_VecItem(hvo, tag, index, phvo);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#VecSize}, ${VwCacheDa#get_VecSize}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_VecSize(HVO hvo, PropTag tag, int * pchvo)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pchvo);

	HRESULT hr;
	IgnoreHr(hr = SuperClass::get_VecSize(hvo, tag, pchvo));
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(hvo))
		return S_OK;
	// Means not in cache.
	LoadVecProp(hvo, tag);
	// We use CheckHr here, because we don't want to return S_FALSE even if the superclass
	// method does. If the property is still empty, it is not because it's not loaded!
	CheckHr(SuperClass::get_VecSize(hvo, tag, pchvo));

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}



/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#VecProp}; Get the full contents of the specified sequence in one go.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::VecProp(HVO hvo, PropTag tag, int chvoMax, int * pchvo, HVO * prghvo)
{
	BEGIN_COM_METHOD;
	HRESULT hr;
	IgnoreHr(hr = SuperClass::VecProp(hvo, tag, chvoMax, pchvo, prghvo));
	if (hr == S_FALSE && m_alpAutoloadPolicy != kalpNoAutoload)
	{
		if (IsDummyId(hvo))
			return S_OK;
		LoadVecProp(hvo, tag);
		CheckHr(SuperClass::VecProp(hvo, tag, chvoMax, pchvo, prghvo));
	}
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	Load all the entries in a vector property.
----------------------------------------------------------------------------------------------*/
void VwOleDbDa::LoadVecProp(HVO hvo, PropTag tag)
{
	if (m_alpAutoloadPolicy == kalpNoAutoload)
		return; // want this option as fast as possible
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	// These may well fail for dummy properties. If so give up.
	HRESULT hr;
	IgnoreHr(hr = m_qmdc->GetFieldName(tag, &sbstrFldName));
	if (FAILED(hr) || sbstrFldName.Length() == 0)
		return;
	IgnoreHr(hr = m_qmdc->GetOwnClsName(tag, &sbstrClsName));
	if (FAILED(hr) || sbstrClsName.Length() == 0)
		return;
	if (IsDummyId(hvo))
		return;

	// Form the SQL query and execute.
	int cpt;
	CheckHr(m_qmdc->GetFieldType(tag, &cpt));
	int oct = koctObjVecOwn;
	if (cpt == kcptReferenceCollection || cpt == kcptReferenceSequence)
		oct = koctObjVec;
	IDbColSpecPtr qdcs;
	qdcs.CreateInstance(CLSID_DbColSpec);
	StrUni stuSql;
	OLECHAR * pszOrderBy = L"";
	switch(m_alpAutoloadPolicy)
	{
	case kalpLoadAllOfClassForReadOnly:
		// Since not distinguishing object class here, can use 0 for clsid.
		if (TestAndNoteLoadAllForReadOnly(tag, 0, 0))
			return; // null value was not autoloaded.
		if (cpt == kcptOwningSequence || cpt == kcptOwningCollection)
		{
			if (cpt == kcptOwningSequence)
				pszOrderBy = L", OwnOrd$";
			stuSql.Format(L"select owner$, id from CmObject where OwnFlid$ = %d order by Owner$%s",
					tag, pszOrderBy);
		}
		else
		{
			if (cpt == kcptReferenceSequence)
				pszOrderBy = L", ord";
			stuSql.Format(L"select src, dst from %s_%s order by src%s",
				sbstrClsName.Chars(), sbstrFldName.Chars(), pszOrderBy);
		}
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(oct, 1, tag, 0));
		CheckHr(Load(stuSql.Bstr(), qdcs, hvo, 0, NULL, false));
		break;
	case kalpLoadForAllOfBaseClass: // should be slightly different, but not yet impl
	case kalpLoadForAllOfObjectClass:
		// Since we're not distinguishing object class from base class yet, only one
		// autoload for this property is possible, and we can ignore class in the recent autloads tests.
		if (!TestAndNoteRecentAutoloads(tag, 0, 0))
		{
			if (cpt == kcptOwningSequence)
				pszOrderBy = L", x.ownord$";
			CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
			CheckHr(qdcs->Push(oct, 1, tag, 0));

			if (cpt == kcptOwningSequence || cpt == kcptOwningCollection)
			{
				stuSql.Format(L"select cmo.id, x.id from %s_ cmo "
						L"left outer join CmObject x on x.owner$ = cmo.id and x.ownflid$ = %d order by cmo.id%s",
					sbstrClsName.Chars(), tag, pszOrderBy);
			}
			else
			{
			if (cpt == kcptReferenceSequence)
				pszOrderBy = L", x.ord";
				stuSql.Format(L"select cmo.id, x.dst from %s_ cmo "
						L"left outer join %s_%s x on x.src = cmo.id order by cmo.id%s",
					sbstrClsName.Chars(), sbstrClsName.Chars(), sbstrFldName.Chars(), pszOrderBy);
			}
			CheckHr(Load(stuSql.Bstr(), qdcs, hvo, 0, NULL, false));
			// Todo: make a separate query to load timestamp info.
			break;
		}
		// FALL THROUGH (if we didn't break above, it's because TestAndNoteRecentAutloads
		// returned false, so we go ahead with the one-object case.
	case kalpLoadForThisObject:
		{
			if (cpt == kcptReferenceSequence || cpt == kcptOwningSequence)
				pszOrderBy = L" order by [ord] ";
			stuSql.Format(L"select [Dst] from [%s_%s] where [Src] = %d%s",
				sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, pszOrderBy);
			CheckHr(qdcs->Push(oct, 0, tag, 0));
			CheckHr(Load(stuSql.Bstr(), qdcs, hvo, 0, NULL, false));
			ObjPropRec oprKey(hvo, tag);
			ObjSeq os;
			if (!m_hmoprsobj.Retrieve(oprKey, &os))
			{
				// Must be an empty prop. Cache an empty value so we don't keep retrieving.
				os.m_cobj = 0;
				os.m_prghvo = NULL;
				m_hmoprsobj.Insert(oprKey, os);
			}
			CacheCurrTimeStampIfMissing(hvo);
		}
		break;
	}
}

//:>********************************************************************************************
//:>	VwOleDbDa methods used to retrieve object PROPERTY information from the cache (except
//:>	references).
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#BinaryPropRgb}, ${VwCacheDa#BinaryPropRgb}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::BinaryPropRgb(HVO hvo, PropTag tag, byte * prgb, int cbMax, int * pcb)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(prgb);
	ChkComArgPtrN(pcb);

	HRESULT hr = SuperClass::BinaryPropRgb(hvo, tag, prgb, cbMax, pcb);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(hvo))
		return S_OK;
	// Autoload
	LoadSimpleProp(hvo, tag, koctBinary);

	return SuperClass::BinaryPropRgb(hvo, tag, prgb, cbMax, pcb);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#GuidProp}, ${VwCacheDa#get_GuidProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_GuidProp(HVO hvo, PropTag tag, GUID * puid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(puid);

	// TODO 1725 (PaulP): Implement lazy retrieval.
	HRESULT hr = SuperClass::get_GuidProp(hvo, tag, puid);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(hvo))
		return S_OK;

	LoadSimpleProp(hvo, tag, koctGuid);

	return SuperClass::get_GuidProp(hvo, tag, puid);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#ObjFromGuid}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_ObjFromGuid(GUID uid, HVO * pHvo)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pHvo);

	HRESULT hr = SuperClass::get_ObjFromGuid(uid, pHvo);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;

	// Form the SQL query and execute.
	StrUni stuSql;
	IDbColSpecPtr qdcs;
	qdcs.CreateInstance(CLSID_DbColSpec);
	CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
	CheckHr(qdcs->Push(koctGuid, 1, kflidCmObject_Guid, 0));

	OLECHAR szGuid[40];
	::StringFromGUID2(uid, szGuid, 40);
	stuSql.Format(L"select id, Guid$ from CmObject where Guid$ = '%s'", szGuid);
	CheckHr(Load(stuSql.Bstr(), qdcs, 0, INT_MAX, NULL, false));

	// Now try again...
	if (m_hmoguidobj.Retrieve(uid, pHvo))
		return S_OK;
	*pHvo = 0;
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#Int64Prop}, ${VwCacheDa#get_Int64Prop}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_Int64Prop(HVO hvo, PropTag tag, int64 * plln)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(plln);

	HRESULT hr = SuperClass::get_Int64Prop(hvo, tag, plln);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(hvo))
		return S_OK;
	// OK, load from DB.
	LoadSimpleProp(hvo, tag, koctInt64);

	ObjPropRec oprKey(hvo, tag);
	m_hmoprlln.Retrieve(oprKey, plln);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#IntProp}, ${VwCacheDa#get_IntProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_IntProp(HVO hvo, PropTag tag, int * pn)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pn);

	HRESULT hr = SuperClass::get_IntProp(hvo, tag, pn);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(hvo))
		return S_OK;
	// Not virtual, not in cache. Try to load from DB.
	// object class, owner, and  own flid doesn't load just like other props, but this method
	// knows how.
	if (tag == kflidCmObject_Class || tag == kflidCmObject_OwnFlid)
	{
		CheckHr(LoadObjInfo(hvo));
	}
	else
	{
		// OK, load from DB.
		LoadSimpleProp(hvo, tag, koctInt);
	}
	ObjPropRec oprKey(hvo, tag);
	m_hmoprn.Retrieve(oprKey, pn);
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#MultiStringAlt}, ${VwCacheDa#get_MultiStringAlt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_MultiStringAlt(HVO hvo, PropTag tag, int ws,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pptss);

	HRESULT hr;
	hr = SuperClass::get_MultiStringAlt(hvo, tag, ws, pptss);
	if (hr == S_FALSE && m_alpAutoloadPolicy != kalpNoAutoload)
	{
		if (IsDummyId(hvo))
			return S_OK;
		// Load it!
		ULONG clsidBase;
		CheckHr(m_qmdc->GetOwnClsId((ULONG)tag, &clsidBase));
		if (m_alpAutoloadPolicy == kalpLoadAllOfClassForReadOnly)
		{
			// It complicates the query too much to limit it to the exact class, load all of
			// the property in this mode.
			if (TestAndNoteLoadAllForReadOnly(tag, ws, clsidBase))
				return S_OK; // null value was not autoloaded.
		}
		(*pptss)->Release(); //avoid mem leak, we will read again.
		*pptss = NULL;
		int cpt;
		CheckHr(m_qmdc->GetFieldType(tag, &cpt));
		SmartBstr sbstrFieldName;
		CheckHr(m_qmdc->GetFieldName(tag, &sbstrFieldName));
		SmartBstr sbstrClassName;
		CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClassName));
		StrUni stuStmt;
		if (!sbstrFieldName.Length() || !sbstrClassName.Length())
		{
			StrUni stuMsg;
			stuMsg.Format(L"Could not get class or field info for supposed ML field %d of object %d", tag, hvo);
			ThrowInternalError(E_UNEXPECTED, stuMsg.Chars(), 0);
		}
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec);
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		const OLECHAR * pszClassToLoad = sbstrClassName.Chars();
		SmartBstr sbstrInstClassName;
		int clsid = 0; // use 0 for load all of base class.
		if (m_alpAutoloadPolicy == kalpLoadForAllOfObjectClass)
		{
			// class to load is class of hvo
			CheckHr(this->get_IntProp(hvo, kflidCmObject_Class, &clsid));
			CheckHr(m_qmdc->GetClassName((uint)clsid, &sbstrInstClassName));
			pszClassToLoad = sbstrInstClassName.Chars();
		}
		if (cpt == kcptMultiUnicode || cpt == kcptMultiBigUnicode)
		{
			if (m_alpAutoloadPolicy == kalpLoadAllOfClassForReadOnly)
			{
				stuStmt.Format(L"select obj, txt from %s_%s where ws = %d",
					sbstrClassName.Chars(), sbstrFieldName.Chars(), ws);
			}
			else
			{
				stuStmt.Format(L"select cmo.id, itm.txt, cmo.UpdStmp "
					L"from %s_ cmo "
					L"left outer join %s_%s itm on cmo.id=itm.obj and itm.ws = %d",
					pszClassToLoad, sbstrClassName.Chars(), sbstrFieldName.Chars(), ws);
			}
			CheckHr(qdcs->Push(koctMltAlt, 1, tag, ws));
		}
		else
		{
			if (m_alpAutoloadPolicy == kalpLoadAllOfClassForReadOnly)
			{
				stuStmt.Format(L"select obj, txt, fmt from %s_%s where ws = %d",
					sbstrClassName.Chars(), sbstrFieldName.Chars(), ws);
			}
			else
			{
				stuStmt.Format(L"select cmo.id, itm.txt, itm.Fmt, cmo.UpdStmp "
					L"from %s_ cmo "
					L"left outer join %s_%s itm on cmo.id=itm.obj and itm.ws = %d",
					pszClassToLoad, sbstrClassName.Chars(), sbstrFieldName.Chars(), ws);
			}
			CheckHr(qdcs->Push(koctMlsAlt, 1, tag, ws));
			CheckHr(qdcs->Push(koctFmt, 1, tag, ws));
		}
		if (m_alpAutoloadPolicy == kalpLoadForThisObject || TestAndNoteRecentAutoloads(tag, ws, clsid))
			stuStmt.FormatAppend(L" where cmo.id = %d", hvo);
		if (m_alpAutoloadPolicy != kalpLoadAllOfClassForReadOnly)
			CheckHr(qdcs->Push(koctTimeStampIfMissing, 1, 0, 0));
		CheckHr(Load(stuStmt.Bstr(), qdcs, 0, INT_MAX,
			NULL, false));
		CheckHr(SuperClass::get_MultiStringAlt(hvo, tag, ws, pptss));
		return S_OK;
	}
	return hr;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#StringProp}, ${VwCacheDa#get_StringProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_StringProp(HVO hvo, PropTag tag, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);

	HRESULT hr = SuperClass::get_StringProp(hvo, tag, pptss);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	ReleaseObj(*pptss); // We got a ref on an empty string, but we don't want it.
	*pptss = NULL;
	if (IsDummyId(hvo))
		return S_OK;

	// OK, load from DB (if it's a valid type of property).
	int cpt;
	CheckHr(m_qmdc->GetFieldType(tag, &cpt));
	// This covers some bugs in current code. Don't try to autoload if it's really a
	// multistring.
	if (cpt == kcptString || cpt == kcptBigString)
	{
		SmartBstr sbstrClsName;
		SmartBstr sbstrFldName;

		IgnoreHr(hr = m_qmdc->GetFieldName(tag, &sbstrFldName));
		if (FAILED(hr) || sbstrFldName.Length() == 0)
			return S_FALSE;
		IgnoreHr(hr = m_qmdc->GetOwnClsName(tag, &sbstrClsName));
		if (FAILED(hr) || sbstrClsName.Length() == 0)
			return S_FALSE;
		const OLECHAR * pszClassToLoad = sbstrClsName.Chars();
		SmartBstr sbstrInstClassName;
		int clsid = 0; // use for load all of base class
		if (m_alpAutoloadPolicy == kalpLoadForAllOfObjectClass || m_alpAutoloadPolicy == kalpLoadAllOfClassForReadOnly)
		{
			// class to load is class of hvo
			CheckHr(this->get_IntProp(hvo, kflidCmObject_Class, &clsid));
			CheckHr(m_qmdc->GetClassName((uint)clsid, &sbstrInstClassName));
			pszClassToLoad = sbstrInstClassName.Chars();
		}
		// We need to pass the default WS for custom fields for creating empty strings.
		// See LT-6650 (secondary complaint).
		int nWs = 0;
		CheckHr(m_qmdc->GetFieldWs(tag, &nWs));
		// Form the SQL query and execute.
		StrUni stuSql;
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec);
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctString, 1, tag, nWs));
		CheckHr(qdcs->Push(koctFmt, 1, tag, 0));
		if (m_alpAutoloadPolicy == kalpLoadAllOfClassForReadOnly)
		{
			if (TestAndNoteLoadAllForReadOnly(tag, 0, clsid))
				return S_OK; // null value was not autoloaded.
			ULONG clsidBase;
			CheckHr(m_qmdc->GetOwnClsId((ULONG)tag, &clsidBase));
			// The difference between these two is the _ after the third %s, which allows us to pick up
			// a base class field, but makes the query more expensive.
			if (clsidBase == (ULONG)clsid)
			{
				stuSql.Format(L"select id, [%s], [%s_fmt] from [%s]"
							L"", sbstrFldName.Chars(), sbstrFldName.Chars(),
							pszClassToLoad);
			}
			else
			{
				stuSql.Format(L"select id, [%s], [%s_fmt] from [%s_]"
							L"", sbstrFldName.Chars(), sbstrFldName.Chars(),
							pszClassToLoad);
			}
		}
		else
		{
			stuSql.Format(L"select id, [%s], [%s_fmt], UpdStmp from [%s_]"
						L"", sbstrFldName.Chars(), sbstrFldName.Chars(),
						pszClassToLoad);
			CheckHr(qdcs->Push(koctTimeStampIfMissing, 1, 0, 0));
		}
		if (m_alpAutoloadPolicy == kalpLoadForThisObject || TestAndNoteRecentAutoloads(tag, 0, clsid))
			stuSql.FormatAppend(L" where [id] = %d", hvo);
		CheckHr(Load(stuSql.Bstr(), qdcs, 0, INT_MAX, NULL, false));
	}

	// And try again to get the result. Be sure to use this method, it does special
	// things like creating an empty default string.
	return SuperClass::get_StringProp(hvo, tag, pptss);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	This implements a common pattern of autoloading data, where the required Sql is
	"select <field> from <class> where id = <hvo>"
	and this is to be passed to Load with a column spec that loads a single column
	of type <oct> for the specified property.
----------------------------------------------------------------------------------------------*/
void VwOleDbDa::LoadSimpleProp(HVO hvo, PropTag tag, int oct)
{
	// no point in trying to load dummy objects, and it can be VERY expensive, especially when
	// in one of the modes that loads a property for all instances of an object, since that
	// only loads it for REAL instances, and fake objects continue to generate cache misses.
	if (IsDummyId(hvo))
		return;
	//  Get the field$ info from the tag.
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	if (tag == 0)
		return;
	// It can fail for reasons other than being 0, such as not existing at all.
	// (But, if we can get a field name, we should be able to get a class name too.)
	CheckHr(m_qmdc->GetFieldNameOrNull(tag, &sbstrFldName));
	if (sbstrFldName.Length() == 0)
		return;
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	// Form the SQL query and execute.
	StrUni stuSql;
	const OLECHAR * pszClassToLoad = sbstrClsName.Chars();
	SmartBstr sbstrInstClassName;
	int clsid = 0; // use for load all of base class
	if (m_alpAutoloadPolicy == kalpLoadForAllOfObjectClass || m_alpAutoloadPolicy == kalpLoadAllOfClassForReadOnly)
	{
		// class to load is class of hvo
		CheckHr(this->get_IntProp(hvo, kflidCmObject_Class, &clsid));
		CheckHr(m_qmdc->GetClassName((uint)clsid, &sbstrInstClassName));
		pszClassToLoad = sbstrInstClassName.Chars();
	}
	IDbColSpecPtr qdcs;
	qdcs.CreateInstance(CLSID_DbColSpec);
	CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
	CheckHr(qdcs->Push(oct, 1, tag, 0));
	if (m_alpAutoloadPolicy == kalpLoadAllOfClassForReadOnly)
	{
		if (TestAndNoteLoadAllForReadOnly(tag, 0, clsid))
			return; // null value was not autoloaded.
		ULONG clsidBase;
		CheckHr(m_qmdc->GetOwnClsId((ULONG)tag, &clsidBase));
		// The difference between these two is the _ after the second %s, which allows us to pick up
		// a base class field, but makes the query more expensive.
		if (clsidBase == (ULONG)clsid)
		{
			stuSql.Format(L"select id, [%s] from [%s]",
					sbstrFldName.Chars(), pszClassToLoad);
		}
		else
		{
			stuSql.Format(L"select id, [%s] from [%s_]",
					sbstrFldName.Chars(), pszClassToLoad);
		}
	}
	else
	{
		stuSql.Format(L"select id, [%s], UpdStmp from [%s_]",
				sbstrFldName.Chars(), pszClassToLoad);
		if (m_alpAutoloadPolicy == kalpLoadForThisObject || TestAndNoteRecentAutoloads(tag, 0, clsid))
			stuSql.FormatAppend(L" where [id] = %d", hvo);
		CheckHr(qdcs->Push(koctTimeStampIfMissing, 1, 0, 0));
	}
	CheckHr(Load(stuSql.Bstr(), qdcs, hvo, INT_MAX, NULL, false));
}

/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#Prop}, ${VwCacheDa#get_Prop}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_Prop(HVO hvo, PropTag tag, VARIANT * pvar)
{
	BEGIN_COM_METHOD;

	HRESULT hr = SuperClass::get_Prop(hvo, tag, pvar);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(hvo))
		return S_OK;
	int cpt;
	CheckHr(m_qmdc->GetFieldType(tag, &cpt));

	switch(cpt)
	{
	default:
		// Other types of property we don't know how to auto-load. However, we may still use
		// other types...it may even be a dummy property that isn't in the database at all.
		return S_FALSE;
	case kcptTime:
		LoadSimpleProp(hvo, tag, koctTime);
		break;
	case kcptInteger:
		{
			int n;
			CheckHr(get_IntProp(hvo, tag, &n));
		}
		break;
	case kcptString:
		{
			ITsStringPtr qtss;
			CheckHr(get_StringProp(hvo, tag, &qtss));
		}
		break;
	}

	return SuperClass::get_Prop(hvo, tag, pvar);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#TimeProp}, ${VwCacheDa#get_TimeProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_TimeProp(HVO hvo, PropTag tag, int64 * ptim)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ptim)

	HRESULT hr = SuperClass::get_TimeProp(hvo, tag, ptim);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(hvo))
		return S_OK;

	// OK, load from DB.
	LoadSimpleProp(hvo, tag, koctTime);

	ObjPropRec oprKey(hvo, tag);
	if (!m_hmoprlln.Retrieve(oprKey, ptim))
		return S_FALSE;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#UnknownProp}, ${VwCacheDa#get_UnknownProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_UnknownProp(HVO hvo, PropTag tag, IUnknown ** ppunk)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppunk);

	HRESULT hr;
	hr = SuperClass::get_UnknownProp(hvo, tag, ppunk);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(hvo))
		return S_OK;

	// we assume ppunk to be a ITsTextProps!
	// Autoload.
	LoadSimpleProp(hvo, tag, koctTtp);

	return SuperClass::get_UnknownProp(hvo, tag, ppunk);
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}



/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	Subclasses and other stuff in the same DLL can get it directly (and much more efficiently)
	as an StrUni.
	DO NOT USE THIS METHOD FOR ANY EXTERNAL PURPOSE.

	${ISilDataAccess#UnicodeProp}, ${VwCacheDa#UnicodeProp}
----------------------------------------------------------------------------------------------*/
bool VwOleDbDa::UnicodeProp(HVO obj, PropTag tag, StrUni & stu)
{
	bool fTemp = false;
	fTemp = SuperClass::UnicodeProp(obj, tag, stu);
	// TODO 1725 (PaulP): Implement lazy retrieval.
	return fTemp;
}


/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#UnicodeProp}, ${VwCacheDa#get_UnicodeProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_UnicodeProp(HVO obj, PropTag tag, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	HRESULT hr = SuperClass::get_UnicodeProp(obj, tag, pbstr);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(obj))
		return S_OK;
	LoadSimpleProp(obj, tag, koctUnicode);
	return SuperClass::get_UnicodeProp(obj, tag, pbstr);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	See ${VwOleDbDa#get_ObjectProp} for more information on why this method is overridden.

	${ISilDataAccess#UnicodePropRgch}, ${VwCacheDa#UnicodePropRgch}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::UnicodePropRgch(HVO obj, PropTag tag, OLECHAR * prgch, int cchMax,
	int * pcch)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgch, cchMax);
	ChkComOutPtr(pcch);

	HRESULT hr = SuperClass::UnicodePropRgch(obj, tag, prgch, cchMax, pcch);
	if (hr != S_FALSE || m_alpAutoloadPolicy == kalpNoAutoload)
		return hr;
	if (IsDummyId(obj))
		return S_OK;
	LoadSimpleProp(obj, tag, koctUnicode);
	return SuperClass::UnicodePropRgch(obj, tag, prgch, cchMax, pcch);
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

//:>********************************************************************************************
//:>	Methods to manage the undo/redo mechanism.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BeginUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::BeginUndoTask(BSTR bstrUndo, BSTR bstrRedo)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrUndo);
	ChkComBstrArgN(bstrRedo);
	if (!m_qacth)
		return S_OK; // ignore if no action handler.

	if (m_nUndoLevel == 0)
	{
		ComBool fOpen;
		CheckHr(m_qode->IsTransactionOpen(&fOpen));
		if (fOpen)
		{
			// Usually there shouldn't be a transaction already open if we're at level 0.
			// Review JohnT: when we get all this working, we may want to change this to an
			// Assert.
			Warn("transaction already open at level 0 of BeginUndoTask");
		}
		else
		{
			CheckHr(m_qode->BeginTrans());
		}
	}
	m_nUndoLevel ++;

	return m_qacth->BeginUndoTask(bstrUndo, bstrRedo);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#EndUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::EndUndoTask()
{
	BEGIN_COM_METHOD;
	if (!m_qacth)
		return S_OK; // ignore if no action handler.

	CheckHr(m_qacth->EndUndoTask());
	m_nUndoLevel --;
	if (m_nUndoLevel == 0)
		CheckHr(m_qode->CommitTrans());

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#ContinueUndoTask}
	The new activity is made to look like part of the same Undo task, but it requires a
	new transaction.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::ContinueUndoTask()
{
	BEGIN_COM_METHOD;
	if (!m_qacth)
		return S_OK; // ignore if no action handler.

	if (m_nUndoLevel == 0)
	{
		ComBool fOpen;
		CheckHr(m_qode->IsTransactionOpen(&fOpen));
		if (fOpen)
		{
			// Usually there shouldn't be a transaction already open if we're at level 0.
			// Review JohnT: when we get all this working, we may want to change this to an
			// Assert.
			Warn("transaction already open at level 0 of ContinueUndoTask");
		}
		else
		{
			CheckHr(m_qode->BeginTrans());
		}
	}
	m_nUndoLevel ++;
	return m_qacth->ContinueUndoTask();

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#EndOuterUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::EndOuterUndoTask()
{
	BEGIN_COM_METHOD;
	if (!m_qacth)
		return S_OK; // ignore if no action handler.

	CheckHr(m_qacth->EndOuterUndoTask());
	ComBool fOpen;
	CheckHr(m_qode->IsTransactionOpen(&fOpen));
	if (fOpen)
	{
		CheckHr(m_qode->CommitTrans());
#if DEBUG // need for release build
		if (m_nUndoLevel == 0)
			Warn("undo level 0 in EndOuterUndoTask, but transaction was open");
#endif
	}
	m_nUndoLevel = 0;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BreakUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::BreakUndoTask(BSTR bstrUndo, BSTR bstrRedo)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrUndo);
	ChkComBstrArgN(bstrRedo);
	if (!m_qacth)
		return S_OK; // ignore if no action handler.
	return m_qacth->BreakUndoTask(bstrUndo, bstrRedo);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#Rollback}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::Rollback()
{
	BEGIN_COM_METHOD;
	if (!m_qacth)
		return S_OK; // ignore if no action handler.

	CheckHr(m_qacth->Rollback(0));
	ComBool fOpen;
	CheckHr(m_qode->IsTransactionOpen(&fOpen));
	if (fOpen)
	{
		CheckHr(m_qode->RollbackTrans());
//#if DEBUG // need for release build
//		if (m_nUndoLevel == 0)
//			Warn("undo level 0 in Rollback, but transaction was open");
//#endif
	}
	m_nUndoLevel = 0;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GetActionHandler}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::GetActionHandler(IActionHandler ** ppacth)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppacth);

	*ppacth = m_qacth;
	AddRefObj(*ppacth);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetActionHandler}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetActionHandler(IActionHandler * pacth)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pacth);

	m_qacth = pacth;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

//:>********************************************************************************************
//:>	VwOleDbDa methods used to create new objects, delete existing objects, or a combination
//:>	of both these actions (in the case of MoveOwnSeq and Replace).  These are the only
//:>	methods that actually change OWNERSHIP RELATIONSHIPS of objects.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Used to delete @b{root level} objects or objects that do not have their owner cached.
	If either of these situations are not the case, the DeleteObjOwner method should be used
	instead.

	This method calls the "exec DeleteObjects" stored procedure in the database which deletes
	all owned descendents, references to other objects, and references to the object itself.
	This may leave some unused records in the cache, however they should be benign.

	It does not re-load the information on the object's owner or relationships that the
	owner has with other objects (i.e.  sequence or collection ownership).  @b{Thus, this
	method should not be used that often.}

	${ISilDataAccess#DeleteObj}, ${VwCacheDa#DeleteObj}
	// Todo JohnT(Undo): Requires lots of work to be able to restore child objects and refs.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::DeleteObj(HVO hvoObj)
{
	BEGIN_COM_METHOD;
	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	Assert(hvoObj != 0);
	if (!hvoObj)
		return E_INVALIDARG;

	IOleDbCommandPtr qodc;

	// Create SQL command.
	CheckHr(m_qode->CreateCommand(&qodc));
	StrUni stuSql;
	stuSql.Format(L"EXEC DeleteObjects '%d'", hvoObj);

	// Create a UndoAction if this VwOleDbDa was given an ActionHandler.
	DelObjUndoActionPtr qdoua;
	if (m_qacth)
	{
		qdoua.Attach(NewObj DelObjUndoAction());
		StrUni stuId;
		stuId.Format(L"%d", hvoObj);
		CheckHr(qdoua->GatherUndoInfo(stuId.Bstr(), m_qode, m_qmdc, this));
	}

	// Actually execute the command.
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// Add the UndoAction to the ActionHandler stack.
	if (m_qacth)
	{
		// Push the SqlUndoAction on the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qdoua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	// Remove the Id value from the cache.
	m_hmostamp.Delete(hvoObj);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	Primary method to delete objects in the cache (and database).  This calls the DeleteObjects
	stored procedure in the database which deletes all owned descendents, references to other
	objects, and references to that object.

	This will also clean up the owning object's references (to this and other objects) in the
	cache, assuming you have the arguments right. (Note that the ${VwOleDbDa#DeleteObj} method
	does @b{not} do this.)

	@list{Pass ihvo = -2 for atomic properties.}
	@list{For collections or sequences, if you know the position, pass it; otherwise, pass -1}

	If the owning property is not cached at all, the object is simply deleted.  This is not an
	error.

	${ISilDataAccess#DeleteObjOwner}, ${VwCacheDa#DeleteObjOwner}
	// Todo JohnT(Undo): Requires lots of work to be able to restore child objects and refs.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::DeleteObjOwner(HVO hvoOwner, HVO hvoObj, PropTag tag, int ihvo)
{
	BEGIN_COM_METHOD;

	//  Delete the object in the database.
	IOleDbCommandPtr qodc;
	CheckHr(m_qode->CreateCommand(&qodc));
	StrUni stuSql;
	stuSql.Format(L"EXEC DeleteObjects '%d'", hvoObj);

	// Create a UndoAction if this VwOleDbDa was given an ActionHandler.
	DelObjUndoActionPtr qdoua;
	bool fClearIncomingRefs = true;
	if (m_qacth)
	{
		qdoua.Attach(NewObj DelObjUndoAction());
		ComBool fHasIncomingReferences;
		// ENHANCE (TE-4685): To improve performance, we could make DelObjUndoAction responsible
		// for cleaning up the cache instead of leaving that for DeleteObjOwnerCore.
		// this is especially necessary for making it faster to delete objects that have
		// incoming references, such as paragraphs that have annotations.
		StrUni stuId;
		stuId.Format(L"%d", hvoObj);
		CheckHr(qdoua->GatherUndoInfo(stuId.Bstr(), m_qode, m_qmdc, this));
		fClearIncomingRefs = qdoua->HasIncomingReferences();
	}

	// Actually execute the command.
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// Add the UndoAction to the ActionHandler stack.
	if (m_qacth)
	{
		// Push the SqlUndoAction on the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qdoua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	//  Delete the object in the cache (replace the vector).
	m_hmostamp.Delete(hvoObj);
	DeleteObjOwnerCore(hvoOwner, hvoObj, tag, ihvo, fClearIncomingRefs);

	// The database stored procedure updated the timestamp on the owner.
	// We need to make sure the cache copy is kept in sync.
	// If we are deleting a paragraph from an StText, the database updates both
	// the StText and its owner, so we need to update both timestamps in this case.
	CheckHr(CacheCurrTimeStamp(hvoOwner));
	if (tag == kflidStText_Paragraphs) {
		HVO hvoGrandparent; // Owner of hvoOwner
		CheckHr(get_ObjOwner(hvoOwner, &hvoGrandparent));
		if(hvoGrandparent) {
			CheckHr(CacheCurrTimeStamp(hvoGrandparent));
		}
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	Typically used when splitting the paragraph at ihvo. New objects are inserted after the one
	at ihvo.  The new objects should generally be similar to the one at ihvo, except that the
	main text property that forms the paragraph body should be empty.  If the object has a
	paragraph style property, the new objects should have the same style as the one at ihvo,
	except that, if a stylesheet is passed, each successive paragraph inserted should have the
	appropriate next style for the one named in the previous paragraph.

	${ISilDataAccess#InsertNew}, ${VwCacheDa#InsertNew}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::InsertNew(HVO hvoObj, PropTag tag, int ihvo, int chvo,
	IVwStylesheet * pss)
{
	BEGIN_COM_METHOD;

	ChkComArgPtrN(pss);
	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvoObj != 0);
	if (ihvo < 0)
	{
		return E_INVALIDARG;
	}

	CheckHr(SuperClass::InsertNew(hvoObj, tag, ihvo, chvo, pss));

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	This method does the real work of moving an object from one owner to another. Wraps the
	MoveOwnedObject stored procedure. Called by MoveOwn and MoveOwnSeq.
----------------------------------------------------------------------------------------------*/
void VwOleDbDa::MoveOwnedObject(HVO hvoSrcOwner, PropTag tagSrc, HVO hvoStart, HVO hvoEnd,
	HVO hvoDstOwner, PropTag tagDst, HVO hvoDstStart, ISqlUndoAction* qsqlua, HVO hvoUndoDst,
	const StrUni& stuVerifyUndoable, const StrUni& stuVerifyRedoable)
{
	Assert(hvoSrcOwner != 0);
	Assert(hvoDstOwner != 0);

	int iSrcType;
	m_qmdc->GetFieldType(tagSrc, &iSrcType);
	int iDstType;
	m_qmdc->GetFieldType(tagDst, &iDstType);
	// only a sequence should have different values for hvoStart and hvoEnd
	if (iSrcType != kcptOwningSequence && hvoStart != hvoEnd)
	{
		ThrowInternalError(E_INVALIDARG);
	}

	StrUni stuSql;
	IOleDbCommandPtr qodc;
	CheckHr(m_qode->CreateCommand(&qodc));
	if (hvoDstStart == 0)
	{
		//  Append the selected records to the end of the sequence.
		stuSql.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d, null",
			hvoSrcOwner, tagSrc,
			hvoStart, hvoEnd,
			hvoDstOwner, tagDst);
	}
	else
	{
		//  Insert the selected records before the DstStart object.
		stuSql.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d, %d",
			hvoSrcOwner, tagSrc,
			hvoStart, hvoEnd,
			hvoDstOwner, tagDst, hvoDstStart);
	}

	if (qsqlua)
	{
		//  Set the Redo command.
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));

		// Create and set the Undo command.
		StrUni stuUndo;
		if (hvoUndoDst == 0)
		{
			//  Re-append the selected records to the end of the sequence.
			stuUndo.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d, null",
				hvoDstOwner, tagDst,
				hvoStart, hvoEnd,
				hvoSrcOwner, tagSrc);
		}
		else
		{
			//  Re-insert the selected records before the object following the End object.
			stuUndo.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d, %d",
				hvoDstOwner, tagDst,
				hvoStart, hvoEnd,
				hvoSrcOwner, tagSrc, hvoUndoDst);
		}
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodc, stuUndo.Bstr()));

		CheckHr(qsqlua->VerifyUndoable(m_qode, qodc, stuVerifyUndoable.Bstr()));
		CheckHr(qsqlua->VerifyRedoable(m_qode, qodc, stuVerifyRedoable.Bstr()));

		// Reload the source vector, along with new owner and flid.
		StrUni stuReload;
		stuReload.Format(L"select [id], [owner$], [ownflid$] from CmObject "
			L"where [owner$]=%d and [OwnFlid$]=%d order by [OwnOrd$]",
			hvoSrcOwner, tagSrc);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(iSrcType == kcptOwningAtom ? koctObj : koctObjVec, 0, tagSrc, 0));
		CheckHr(qdcs->Push(koctObj, 1, kflidCmObject_Owner, 0));
		CheckHr(qdcs->Push(koctInt, 1, kflidCmObject_OwnFlid, 0));
		// Note: The undo and redo reload statements are the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvoSrcOwner, 0,
			NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvoSrcOwner, 0,
			NULL));

		// Reload the destination vector along with new owner and flid.
		stuReload.Format(L"select [id], [owner$], [ownflid$] from CmObject "
			L"where [owner$]=%d and [OwnFlid$]=%d order by [OwnOrd$]",
			hvoDstOwner, tagDst);
		IDbColSpecPtr qdcs2;
		qdcs2.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs2->Push(iDstType == kcptOwningAtom ? koctObj : koctObjVec, 0, tagDst, 0));
		CheckHr(qdcs2->Push(koctObj, 1, kflidCmObject_Owner, 0));
		CheckHr(qdcs2->Push(koctInt, 1, kflidCmObject_OwnFlid, 0));
		// Note: The undo and redo reload statements are the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs2, hvoDstOwner, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs2, hvoDstOwner, 0, NULL));
	}

	// Actually execute the command
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));

	// The database stored procedure updated the timestamp(s) on the owner.
	// We need to make sure the cache copy is kept in sync.
	CheckHr(CacheCurrTimeStamp(hvoSrcOwner));
	if (hvoSrcOwner != hvoDstOwner)
		CheckHr(CacheCurrTimeStamp(hvoDstOwner));
}

/*----------------------------------------------------------------------------------------------
	Append to strSql a check that object hvo occurs at index ihvo in property tagOwner of
	object hvoOwner.
	The caller should already have declared variable @res and @ihvo
----------------------------------------------------------------------------------------------*/
void AppendObjectPositionCheck(HVO hvoOwner, int tagOwner, int ihvo, HVO hvo, StrUni & strSql)
{
	strSql.FormatAppend(L"select @ihvo = count(id) from CmObject%n"
		L"where owner$ = %d and ownflid$ = %d and ownord$ < "
		L"(select ownord$ from CmObject where id = %d)%n"
		L"if (@ihvo != %d) set @res = 0%n",
		hvoOwner, tagOwner, hvo, ihvo);
}

/*----------------------------------------------------------------------------------------------
	Build an sql query to verify that (ihvoEnd - ihvoStart + 1) objects in prghvo
	are found at position ihvoStart in property tagSrc of object hvoSrc,
	and (if hvoDst is non-zero) that object hvoIns is found at position ihvoDst
	in property tagDst of object hvoDst.
	It's especially important that the first and last objects are at the correct positions.
----------------------------------------------------------------------------------------------*/
StrUni BuildMoveSeqVerify(HVO hvoSrcOwner, int tagSrc, HVO hvoDstOwner, int tagDst, int
	ihvoStart, int ihvoEnd, int ihvoDst, HVO * prghvoObjects, HVO hvoDstBefore)
{
	int cobjMove = ihvoEnd - ihvoStart + 1;
	StrUni stuSql(L"declare @cobj int, @res int, @ihvo int \r\n"
		L"set @res = 1 \r\n");
	// Verify the location of the first source object.
	AppendObjectPositionCheck(hvoSrcOwner, tagSrc, ihvoStart, prghvoObjects[0], stuSql);
	// If there's more than one verify the location of the last.
	if (ihvoEnd != ihvoStart)
	{
		AppendObjectPositionCheck(hvoSrcOwner, tagSrc, ihvoEnd, prghvoObjects[cobjMove - 1],
			stuSql);
	}
	// If there's a destination object to insert before, verify its position.
	if (hvoDstBefore)
		AppendObjectPositionCheck(hvoDstOwner, tagDst, ihvoDst, hvoDstBefore, stuSql);
	// If we moved more than two objects, verify that the right ones occur between the first and
	// last.  (We could try to verify their exact order, but that's difficult and seems more
	// than necessary).   Note that we retrieve objects AFTER the first and BEFORE the last that
	// are 'in' the expected list; if we get the exact expected number (two less than the total
	// number we're moving), all the right objects must be present, because we don't allow
	// duplicates in owning properties.
	if (cobjMove > 2)
	{
		stuSql.FormatAppend(L"select @cobj = count(id) from CmObject%n"
			L"where owner$ = %d and ownflid$ = %d%n"
			L"and ownord$ > (select ownord$ from CmObject where id = %d)%n"
			L"and ownord$ < (select ownord$ from CmObject where id = %d)%n"
			L"and id in (",
			hvoSrcOwner, tagSrc, prghvoObjects[0], prghvoObjects[cobjMove - 1]);
		for (int i = 1; i < cobjMove - 1; ++i)
			stuSql.FormatAppend(L"%s%d", (i > 1 ? L"," : L""), prghvoObjects[i]);
		stuSql.FormatAppend(L")%nif (@cobj != %d) set @res = 0;%n", cobjMove - 2);
	}
	stuSql.Append(L"select @res");
	return stuSql;
}

/*----------------------------------------------------------------------------------------------
	This method inserts the objects delimited by the indexes ihvoStart and ihvoEnd in another
	owning sequence (or in the same owning sequence at a different position).  The "ord"
	value of the objects will be changed accordingly (starting with the "ord" value of ihvoDst)
	as well as the "OwnOrd$" column (if a new owning object is specified).

	All objects selected by ihvoStart and ihvoEnd will be inserted in the sequence BEFORE the
	position of the object found at ihvoDstStart (which is in the destination owning sequence).

	To append the (ihvoStart to ihvoEnd) objects to the end of the sequence (owned by
	hvoDstOwner), specify an ihvoDstStart greater than the highest value in that sequence.
	eg.  if there are 4 ownSeq objects already in hvoDstOwner (0,1,2,3) set ihvoDstStart = 4
	(or higher).

	NOTE!  While the "ord" values of sequences in the database are guaranteed to be sequential,
	they are NOT guaranteed to be contiguous.  That is, there may be gaps in the numbering.
	(eg. 0, 1, 4, 6, 23 rather than 0, 1, 2, 3, 4).  However, the elements in the ObjSeq record
	in the VwCacheDa database cache IS guaranteed to be both sequential and contiguous.  That
	is, if there are 5 elements in a sequence, there will be only 5 elements in the array and
	they will be indexed 0, 1, 2, 3, 4.

	${ISilDataAccess#MoveOwnSeq}, ${VwCacheDa#MoveOwnSeq}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::MoveOwnSeq(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart, int ihvoEnd,
	HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart)
{
	BEGIN_COM_METHOD;

	Assert(hvoSrcOwner != 0);
	Assert(hvoDstOwner != 0);

	int iDstType;
	m_qmdc->GetFieldType(tagDst, &iDstType);
	int cobjMove = ihvoEnd - ihvoStart + 1;

	//  Make sure the given parameters values are OK.
	if (ihvoStart < 0 || ihvoStart > ihvoEnd || ihvoDstStart < 0
		|| (iDstType == kcptOwningAtom && cobjMove > 1))
	{
		ThrowInternalError(E_INVALIDARG);
	}

	//  If the source ObjSeq record is not in the hash map (of the cache) already,
	//  return an error.
	ObjSeq osSrc;
	ObjPropRec oprSrcKey(hvoSrcOwner, tagSrc);
	if (!m_hmoprsobj.Retrieve(oprSrcKey, &osSrc))
	{
		ThrowInternalError(E_FAIL);
	}

	int cobjSrc = osSrc.m_cobj;
	if ((ihvoStart >= cobjSrc) || (ihvoEnd >= cobjSrc))
	{
		ThrowInternalError(E_INVALIDARG);
	}

	HVO hvoDstStart = 0;
	ObjSeq osDst;
	ObjPropRec oprDstKey(hvoDstOwner, tagDst);
	if (m_hmoprsobj.Retrieve(oprDstKey, &osDst) && ihvoDstStart < osDst.m_cobj)
		hvoDstStart = osDst.m_prghvo[ihvoDstStart];

	HVO hvoUndoDst = 0;
	ISqlUndoActionPtr qsqlua;
	StrUni stuVerifyUndoable;
	StrUni stuVerifyRedoable;
	if (m_qacth)
	{
		// Create the SqlUndoAction
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.

		// If we undo, the (sequence of) object(s) will insert before hvoUndoDst.  If that
		// equals 0, we'll insert at the end.
		if (ihvoEnd < cobjSrc - 1)
			hvoUndoDst = osSrc.m_prghvo[ihvoEnd + 1];
		int ihvoBeforeUndo = ihvoStart; // where insert before object must be for undo
		int ihvoStartUndo = ihvoDstStart;  // where moved seq must be found for undo
		if (hvoSrcOwner == hvoDstOwner && tagSrc == tagDst)
		{
			// Typically, the object to insert before for Undo should be at the index
			// of the first object we moved.
			// But, if we moved the objects earlier in the same property, this is not true.
			// Instead, it's the original position of that object, one beyond the last
			// one moved.
			if (ihvoStart > ihvoDstStart)
				ihvoBeforeUndo = ihvoEnd + 1;
			// Typically, the first object must be at position ihvoDstStart in the destination
			// in order to Undo. But if moving later in the same property, chvo objects are
			// 'deleted' before the new position, so it is off by that much.
			if (ihvoStart < ihvoDstStart)
				ihvoStartUndo -= cobjMove;
		}
		// Create and set the Verify commands.
		// Basically, check the sizes of the source and destination, and check for the
		// presence of known objects.  We could be paranoid and check for all of the
		// objects in both sequences.  We could be even more paranoid and verify the
		// order for true sequences.
		stuVerifyUndoable = BuildMoveSeqVerify(hvoDstOwner, tagDst,
			hvoSrcOwner, tagSrc, ihvoStartUndo, ihvoStartUndo + cobjMove - 1,
			ihvoBeforeUndo, osSrc.m_prghvo + ihvoStart, hvoUndoDst);
		stuVerifyRedoable = BuildMoveSeqVerify(hvoSrcOwner, tagSrc,
			hvoDstOwner, tagDst, ihvoStart, ihvoEnd,
			ihvoDstStart, osSrc.m_prghvo + ihvoStart, hvoDstStart);

	}
	MoveOwnedObject(hvoSrcOwner, tagSrc, osSrc.m_prghvo[ihvoStart], osSrc.m_prghvo[ihvoEnd],
		hvoDstOwner, tagDst, hvoDstStart, qsqlua, hvoUndoDst, stuVerifyUndoable, stuVerifyRedoable);

	//  Affect the change in the cache.
	CheckHr(SuperClass::MoveOwnSeq(hvoSrcOwner, tagSrc, ihvoStart, ihvoEnd, hvoDstOwner,
		tagDst, ihvoDstStart));

	if (m_qacth)
	{
		// Push the SqlUndoAction on the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	Append to strSql a check that object hvo occurs in property tagOwner of
	object hvoOwner.
	The caller should already have declared variable @res and @ihvo
----------------------------------------------------------------------------------------------*/
void AppendObjectCheck(HVO hvoOwner, int tagOwner, HVO hvo, StrUni & strSql)
{
	strSql.FormatAppend(L"select @ihvo = count(id) from CmObject%n"
		L"where owner$ = %d and ownflid$ = %d and id = %d%n"
		L"if (@ihvo = 0) set @res = 0%n",
		hvoOwner, tagOwner, hvo);
}

/*----------------------------------------------------------------------------------------------
	Build an sql query to verify that the HVO is found in property tagSrc of object hvoSrc,
	and (if hvoDst is non-zero) that object hvoIns is found at position ihvoDst
	in property tagDst of object hvoDst.
----------------------------------------------------------------------------------------------*/
StrUni BuildMoveObjVerify(HVO hvoSrcOwner, int tagSrc, HVO hvoDstOwner, int tagDst, int
	HVO hvo, int ihvoDst, HVO hvoDstBefore)
{
	StrUni stuSql(L"declare @cobj int, @res int, @ihvo int \r\n"
		L"set @res = 1 \r\n");

	AppendObjectCheck(hvoSrcOwner, tagSrc, hvo, stuSql);
	if (hvoDstBefore)
		AppendObjectPositionCheck(hvoDstOwner, tagDst, ihvoDst, hvoDstBefore, stuSql);
	stuSql.Append(L"select @res");
	return stuSql;
}

/*----------------------------------------------------------------------------------------------
  This method moves an object from one owner to another. The source and destination can be of
  any type. If the destination is a sequence, one can specifiy the location to insert the
  object. The object is inserted in the destination sequence before the object located at
  ihvoDstStart.

	${ISilDataAccess#MoveOwnSeq}, ${VwCacheDa#MoveOwnSeq}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::MoveOwn(HVO hvoSrcOwner, PropTag tagSrc, HVO hvo,
								HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart)
{
	BEGIN_COM_METHOD;

	Assert(hvoSrcOwner != 0);
	Assert(hvoDstOwner != 0);

	int iSrcType;
	m_qmdc->GetFieldType(tagSrc, &iSrcType);
	int iDstType;
	m_qmdc->GetFieldType(tagDst, &iDstType);
	if (iSrcType == kcptOwningSequence)
	{
		// if the source is a sequence, simply call MoveOwnSeq
		int ihvo;
		CheckHr(GetObjIndex(hvoSrcOwner, tagSrc, hvo, &ihvo));
		CheckHr(MoveOwnSeq(hvoSrcOwner, tagSrc, ihvo, ihvo, hvoDstOwner, tagDst,
			ihvoDstStart));
	}
	else
	{
		HVO hvoDstStart = 0;
		// only grab the HVO for ihvoDstStart if the destination is a sequence
		if (iDstType == kcptOwningSequence)
		{
			ObjSeq osDst;
			ObjPropRec oprDstKey(hvoDstOwner, tagDst);
			if (m_hmoprsobj.Retrieve(oprDstKey, &osDst) && ihvoDstStart < osDst.m_cobj)
				hvoDstStart = osDst.m_prghvo[ihvoDstStart];
		}

		ISqlUndoActionPtr qsqlua;
		StrUni stuVerifyUndoable;
		StrUni stuVerifyRedoable;
		if (m_qacth)
		{
			// Create the SqlUndoAction
			qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.

			// build verify queries
			stuVerifyUndoable = BuildMoveObjVerify(hvoDstOwner, tagDst, hvoSrcOwner, tagSrc,
				hvo, 0, 0);
			stuVerifyRedoable = BuildMoveObjVerify(hvoSrcOwner, tagSrc, hvoDstOwner, tagDst,
				hvo, ihvoDstStart, hvoDstStart);
		}

		MoveOwnedObject(hvoSrcOwner, tagSrc, hvo, hvo, hvoDstOwner, tagDst, hvoDstStart, qsqlua, 0,
			stuVerifyUndoable, stuVerifyRedoable);

		//  Affect the change in the cache.
		CheckHr(SuperClass::MoveOwn(hvoSrcOwner, tagSrc, hvo, hvoDstOwner, tagDst, ihvoDstStart));

		if (m_qacth)
		{
			// Push the SqlUndoAction on the ActionHandler stack.
			IUndoActionPtr qua;
			CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
			CheckHr(m_qacth->AddAction(qua));
		}
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	Create a new object of the specified type. Give it the specified owner and the ord value.
	If the ord parameter is set to -1, either the object is not part of an ownning sequence
	or is part of an owning collection (in which case, order does not matter and so, by default,
	it will be added as the last sequence).  This updates the vector property given by hvoOwner
	and tag in the cache.

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

	${ISilDataAccess#MakeNewObject}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::MakeNewObject(int clid, HVO hvoOwner, PropTag tag, int ord, HVO * phvoNew)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvoNew);

#ifdef DEBUG
	// ENHANCE PaulP: Using the tag, determine the CmType and assert if the wrong type of
	// ord value is supplied.
#endif

	ComBool fIsNull1;
	ComBool fIsNull2;
	ComBool fTransAlreadyOpen;
	HVO hvoNew = 0;
	StrUni stuSql;
	ISqlUndoActionPtr qsqlua;

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if (m_qacth)
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.

	//  Execute the stored procedure CreateOwnedObject$(clid, hvo, guid, hvoOwner, tag, ord, 1)
	//  to create a new object (with no values).
	IOleDbCommandPtr qodc;
	CheckHr(m_qode->CreateCommand(&qodc));
	int iFldType;
	m_qmdc->GetFieldType(tag, &iFldType);

	if (iFldType == kcptOwningAtom)
	{
		// If there's an existing object we must clobber it.
		HVO hvoOld;
		CheckHr(get_ObjectProp(hvoOwner, tag, &hvoOld));
		if (hvoOld)
			CheckHr(DeleteObjOwner(hvoOwner, hvoOld, tag, -2));
	}

	int chvo;
	CheckHr(get_VecSize(hvoOwner, tag, &chvo));

	HVO hvoObjAfter;  // Insert before this object.
	if ((uint)ord < (uint)chvo)
	{
		// get the HVO at position ord from cache. The property is a sequence and we will
		// insert before this object
		CheckHr(get_VecItem(hvoOwner, tag, ord, &hvoObjAfter));

		stuSql.Format(L"set IDENTITY_INSERT CmObject OFF; exec CreateOwnedObject$ %d, ? "
			L"output, ? output, %d, %d, %d, %d", clid, hvoOwner, tag, iFldType, hvoObjAfter);
	}
	else
	{
		// Either it is an atomic or collection property (ord < 0), or we are inserting at the
		// end (ord = chvo). In either case we pass null for the object to insert before.
		stuSql.Format(L"set IDENTITY_INSERT CmObject OFF; exec CreateOwnedObject$ %d, ? "
			L"output, ? output, %d, %d, %d, null", clid, hvoOwner, tag, iFldType);
	}
	qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *) &hvoNew,
		sizeof(HVO));
	GUID guid;
	qodc->SetParameter(2, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_GUID,
		reinterpret_cast<ULONG *>(&guid), sizeof(GUID));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
	qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoNew), sizeof(HVO), &fIsNull1);
	qodc->GetParameter(2, reinterpret_cast<BYTE *>(&guid), sizeof(GUID), &fIsNull2);

	// The database stored procedure updated the timestamp on the owner as well as the original
	// item so we need to update both timestamps in the cache. But when we are adding
	// a paragraph to a structured text, it not only updates the timestamp on the owning
	// StText, but it also updates the timestamp on the owner of the StText, so we need to
	// update 3 timestamps.
	CheckHr(CacheCurrTimeStamp(hvoNew));
	HVO hvoParent;
	CheckHr(get_ObjOwner(hvoNew, &hvoParent));
	if(hvoParent)
	{
		if ((uint)ord < (uint)chvo && ord >= 0)
		{
			// sequence - need to get timestamps for all subsequent objects in sequence
			// (TE-4701/TE-4529)
			ObjPropRec oprKey(hvoParent, tag);
			ObjSeq osSeq;
			if (m_hmoprsobj.Retrieve(oprKey, &osSeq))
			{
				// There's probably an easier way to do this...
				int iHvo;
				for (iHvo = 0; iHvo < osSeq.m_cobj; iHvo++)
				{
					if (osSeq.m_prghvo[iHvo] == hvoObjAfter)
						break; // found the starting index
				}

				// Cache time stamps for all following objects in sequence
				for (int i = iHvo; i < osSeq.m_cobj; i++)
				{
					CheckHr(CacheCurrTimeStamp(osSeq.m_prghvo[i]));
				}
			}
		}

		CheckHr(CacheCurrTimeStamp(hvoParent));
		if (tag == kflidStText_Paragraphs)
		{
			HVO hvoGrandparent;
			CheckHr(get_ObjOwner(hvoParent, &hvoGrandparent));
			if(hvoGrandparent)
			{
				CheckHr(CacheCurrTimeStamp(hvoGrandparent));
			}
		}
	}

	if (fIsNull1 || fIsNull2)
	{
		// TODO 1726: Some sort of error, I suppose.
		return -1;
	}

	if (m_qacth)
	{
		// Create SQL statement using the same HVO (i.e. ID) value that was retrieved.
		stuSql = L"set IDENTITY_INSERT CmObject ON; ";
		if ((uint)ord < (uint)chvo)
		{
			stuSql.FormatAppend(L"exec CreateOwnedObject$ %d, %d, ?, %d, %d, %d, %d",
				clid, hvoNew, hvoOwner, tag, iFldType, hvoObjAfter);
		}
		else
		{
			stuSql.FormatAppend(L"exec CreateOwnedObject$ %d, %d, ?, %d, %d, %d, null",
				clid, hvoNew, hvoOwner, tag, iFldType);
		}
		stuSql.Append(L"; set IDENTITY_INSERT CmObject OFF");

		// Create a new IOleDbCommand.
		IOleDbCommandPtr qodcRedo;
		CheckHr(m_qode->CreateCommand(&qodcRedo));
		qodcRedo->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_GUID,
			reinterpret_cast<ULONG *>(&guid), sizeof(GUID));

		CheckHr(qsqlua->AddRedoCommand(m_qode, qodcRedo, stuSql.Bstr()));

		IOleDbCommandPtr qodcUndo;
		CheckHr(m_qode->CreateCommand(&qodcUndo));
		StrUni stuUndo;
		stuUndo.Format(L"EXEC DeleteObjects '%d'", hvoNew);
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodcUndo, stuUndo.Bstr()));

		// We can Undo if the inserted object is back in it's default state.
		// It's probably enough to check that it doesn't own anything and nothing refers to it.
		StrUni stuVerifyUndo;
		stuVerifyUndo.Format(L"declare @cobj int, @res int%n"
			L"set @res = 1%n"
			L"select @cobj = COUNT(id) from CmObject where Owner$ = %<0>d%n"
			L"if @cobj != 0 set @res = 0%n"
			L"if @res = 1 begin%n"
			L"	create table [#ObjInfoTbl$]%n"
			L"	(%n"
			L"		[ObjId]			int not null,%n"
			L"		[ObjClass]		int null,%n"
			L"		[InheritDepth]	int null default(0),%n"
			L"		[OwnerDepth]	int null default(0),%n"
			L"		[RelObjId]		int null,%n"
			L"		[RelObjClass]	int null,%n"
			L"		[RelObjField]	int null,%n"
			L"		[RelOrder]		int null,%n"
			L"		[RelType]		int null,%n"
			L"		[OrdKey]		varbinary(250) null default(0)%n"
			L"	)%n"
		L"	create nonclustered index #ObjInfoTbl$_Ind_ObjId on [#ObjInfoTbl$] (ObjId)%n"
		L"	create nonclustered index #ObjInfoTbl$_Ind_ObjClass on [#ObjInfoTbl$] (ObjClass)%n"
			L"	exec GetLinkedObjs$ '%<0>d', %<1>d, 0, 0, 0, -1, null%n"
			L"	select @cobj = COUNT(*) from [#ObjInfoTbl$]%n"
			L"	drop table [#ObjInfoTbl$]%n"
			L"	if @cobj != 0 set @res = 0%n"
			L"end%n"
			L"select @res",
			hvoNew, kgrfcptReference);
		CheckHr(qsqlua->VerifyUndoable(m_qode, qodcUndo, stuVerifyUndo.Bstr()));

		// We can always Redo, so no need to create a query for that.

		// If this call to MakeNewObject adds to a record to a collection or sequence, we can't
		// define SQL statements specific enough to reload all the related property fields.
		// However, we can reload the owning vector and count on lazy data retrieval to get
		// the rest.
		StrUni stuReload;
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		if (ord == -2)
		{
			stuReload.Format(L"select [id] from CmObject where [owner$]=%d and "
				L"[OwnFlid$]=%d order by [OwnOrd$]", hvoOwner, tag);
			CheckHr(qdcs->Push(koctObj, 0, tag, 0));
		}
		else
		{
			stuReload.Format(L"select [id] from CmObject where [owner$]=%d and "
				L"[OwnFlid$]=%d order by [OwnOrd$]", hvoOwner, tag);
			CheckHr(qdcs->Push(koctObjVec, 0, tag, 0));
		}
		// Note:  The undo and redo reload statements are the same.
		qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvoOwner, 0, NULL);
		qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvoOwner, 0, NULL);
		// Push the SqlUndoAction on the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	//  Add the new record to the cache.
	DoInsert(hvoOwner, hvoNew, tag, ord);
	*phvoNew = hvoNew;
	return S_OK;

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	Build an sql command to insert chvoIns (possibly 0) objects into the ref collection
	or ref sequence property (determined by cpt) of hvoObj, while deleting the ones
	between ihvoMin and ihvoLim of the current value in os.
	Also build a command to verify that this change can be made (e.g., when it is later
	executed as a Redo).
	This is also called with the array of objects to delete and the new value, to
	produce the Undo and VerifyUndo strings.
----------------------------------------------------------------------------------------------*/
void BuildReplaceCommands(HVO hvoObj, PropTag tag, int cpt, HVO * prghvo, int chvoIns,
	int ihvoMin, int ihvoLim, ObjSeq os, const OLECHAR * pszClass, const OLECHAR * pszField,
	StrUni & stuSql, StrUni & stuVerify)
{

	StrUni stuInsIds;
	StrUni stuDelIds;
	stuSql.Clear();
	stuVerify.Clear();
	if (cpt == kcptReferenceSequence)
	{
		// Make the XML document of ids to insert.
		// e.g., <root><Obj Id="4708" Ord="0"/><Obj Id="4708" Ord="1"/></root>
		int nOrd = 0;
		int ihvo;
		stuSql = L"declare @hdoc int;\r\n"
			L"exec sp_xml_preparedocument @hdoc output, '<root>";
		for (ihvo = 0; ihvo < chvoIns; ++ihvo, ++nOrd)
			stuSql.FormatAppend(L"<Obj Id=\"%d\" Ord=\"%d\"/>", prghvo[ihvo], nOrd);
		stuSql.Append(L"</root>';\r\n");

		// Fill in the rest of the query.
		stuSql.FormatAppend(L"exec ReplaceRefSeq$ %d, %d, NULL, @hdoc", tag, hvoObj);

		// If min or lim are at the end, we need to let the parameter(s) null.
		HVO hvoStart = 0;
		int cStart = 0;
		HVO hvoEnd = 0;
		int cEnd = 0;
		if (ihvoMin < os.m_cobj)
		{
			// Calculate the start position.
			// Example: (list of HVOs) 5, 2, 3, 1, 2, 3, 1, 4, 5, 5, 8
			//   ihvoMin = 6, ihvoLim = 10
			//   hvoStart = 1, cStart = 2, hvoEnd = 5, cEnd = 3
			hvoStart = os.m_prghvo[ihvoMin];
			for (ihvo = 0; ihvo <= ihvoMin; ++ihvo)
			{
				if (os.m_prghvo[ihvo] == hvoStart)
					++cStart;
			}
			stuSql.FormatAppend(L", %d, %d", hvoStart, cStart);

			// Omit the end information if no deletions are to be made.
			if (ihvoLim != ihvoMin)
			{
				// Note ReplaceRef$ wants the last item to delete while ihvoLim is beyond.
				// Calculate the occurrence count of the last object to delete.
				hvoEnd = os.m_prghvo[ihvoLim - 1];
				for (ihvo = 0; ihvo < ihvoLim; ++ihvo)
				{
					if (os.m_prghvo[ihvo] == hvoEnd)
						++cEnd;
				}
				stuSql.FormatAppend(L", %d, %d", hvoEnd, cEnd);
			}
		}
		// Note: We do not need sp_xml_removedocument because ReplaceRefSeq$ provides
		// this automatically as long as the ninth parameter is missing.

		// Generate the verification SQL for replacing in a ref sequence.
		if (hvoEnd == 0)
		{
			if (hvoStart != 0)
			{
				// Need to verify that the starting object occurs the right number of times.
				stuVerify.Format(L"declare @cStart int%n"
					L"select @cStart = Count(*) from %s_%s where Src = %d and Dst = %d%n"
					L"if @cStart >= %d%n"
					L"	select 1%n"
					L"else%n"
					L"	select 0",
					pszClass, pszField, hvoObj, hvoStart, cStart);
			}
			// If appending, don't worry about verification.
		}
		else
		{
			Assert(hvoStart != 0);
/*
-- This sql produces a row set containing a single integer that is non-zero if the
-- Replace can proceed, zero if it cannot.
-- We check that the expected list of objects occurs between the specified occurrence
-- of the start object and the specified occurrence of the end object.
-- Specifically, the first object to delete is the chvoStart'th occurrence of hvoStart;
-- the last to delete is the chvoEnd'th occurrence of hvoEnd (1-based).
-- The objects in the xml document must occur in that range of the indicated property
-- (the class/field are inserted into the query, hvoObj is the object having the property).
-- We don't check for their occurring in the exact order, but we do have to account
-- for the possibility of duplicates, which must occur the correct number of times.

declare @hvoObj int, @hvoStart int, @chvoStart int, @hvoEnd int, @chvoEnd int
set @hvoObj = 6787
set @hvoStart = 6737
set @chvoStart = 1
set @hvoEnd = 6737
set @chvoEnd = 2
declare @hdoc int
exec sp_xml_preparedocument @hdoc output,
	'<root><Obj Id="6737"/><Obj Id="6775"/><Obj Id="6737"/><Obj Id="6737"/></root>'
declare @ordStart int, @ordEnd int, @cStart int, @cEnd int

-- We start by checking that the delimiter objects occur the correct number of times,
-- because the queries we use to find ordStart and ordEnd will just produce the last
-- occurrence if there are too few.

select @cStart = Count(*)
from RnAnalysis_SupportingEvidence
where Src = @hvoObj and dst = @hvoStart

select @cEnd = COUNT(*)
from RnAnalysis_SupportingEvidence
where Src = @hvoObj and dst = @hvoEnd

if (@cStart >= @chvoStart and @cEnd >= @chvoEnd) BEGIN

	-- By limiting the number of rows returned and using ascending order, we get the
	-- nth occurrence of this object in the property. Of those rows, the one with
	-- the max(ord) is the last, and hence nth.
	-- another approach is shown below which avoids using rowcount, but for SQL Server would
	-- force us to insert the number (minus 1) literally.
	--	select top 1 @ordStart = x.Ord
	--	from RnAnalysis_SupportingEvidence x
	--	left outer join (select top (@chvoStart - 1) Src, Dst, Ord
	--			from RnAnalysis_SupportingEvidence
	--			where Src = @hvoObj and dst = @hvoStart order by Ord asc) as y on y.Ord = x.Ord
	--	where y.Ord is null and x.Src = @hvoObj and x.Dst = @hvoStart
	--	order by x.Ord asc

	set rowcount @chvoStart
	select @ordStart = max(Ord)
	from RnAnalysis_SupportingEvidence
	where Src = @hvoObj and dst = @hvoStart
	group by Ord order by Ord asc

	-- Same trick to get the ord of the other delimiter object
	set rowcount @chvoEnd
	select @ordEnd = max(Ord)
	from RnAnalysis_SupportingEvidence
	where Src = @hvoObj and dst = @hvoEnd
	group by Ord order by Ord asc

	set rowcount 0 -- following queries return all rows

	-- now compute the number of distinct IDs that occur in the relevant range
	-- of the property, the number of distinct IDs that we are trying to delete,
	-- and the number of that occur the same number of times in both lists.
	-- if all three numbers are equal, we've found the right objects in the
	-- expected places and can go ahead.
	declare @cGood int, @cIdsPres int, @cIdsDel int
	-- this counts the number of distinct values of dst that occur in the specified
	-- property of the specified object in the right range of ords.
	select @cIdsPres = count(*) from (
		select distinct dst
		from RnAnalysis_SupportingEvidence
		where src = @hvoObj and ord >= @ordStart and ord <= @ordEnd) as x

	-- The first subselect produces the frequency of occurrence of each object in the
	-- relevant range of the property. The second produces the frequency of occurrence
	-- of each ID in the xml document. The join produces the cases where the same ID has
	-- the same count in both places.
	select @cGood = count(*) from (
		select dst ObjId, count(*) PresCnt
		from RnAnalysis_SupportingEvidence
		where src = @hvoObj and ord >= @ordStart and ord <= @ordEnd group by Dst) as pc
		join (select [Id] ObjId, count(*) DelCnt
			from openxml(@hdoc, '/root/Obj') with ([Id] int) group by [Id]) as dc
			on pc.ObjId = dc.ObjId and dc.DelCnt = pc.PresCnt;

	-- This counts distinct IDs in the xml document.
	select @cIdsDel = count(*) from (
		select distinct [Id]
		from openxml(@hdoc, '/root/Obj') with ([Id] int) group by [Id]) as x

	if @cGood = @cIdsDel and @cGood = @cIdsPres
		select 1
	else
		select 0

END
ELSE BEGIN
	select 0
END
-- clean up the xml document created above
if @hdoc is not null exec sp_xml_removedocument @hdoc

-- for debugging you can enable this to see the partial results.
--select @ordStart ordStart, @ordEnd ordEnd, @cGood good, @cIdsPres Present, @cIdsDel del
 */
/*
-- This sql produces a row set containing a single integer that is non-zero if the
-- Replace can proceed, zero if it cannot.
-- We check that the expected list of objects occurs between the specified occurrence
-- of the start object and the specified occurrence of the end object.
-- Specifically, the first object to delete is the chvoStart'th occurrence of hvoStart;
-- the last to delete is the chvoEnd'th occurrence of hvoEnd (1-based).
-- The objects in the xml document must occur in that range of the indicated property
-- (the class/field are inserted into the query, hvoObj is the object having the property).
-- We don't check for their occurring in the exact order, but we do have to account
-- for the possibility of duplicates, which must occur the correct number of times.
 */
			stuVerify.Format(L"declare "
				L"@hvoObj int, @hvoStart int, @chvoStart int, @hvoEnd int, @chvoEnd int%n"
				L"set @hvoObj = %d%n"
				L"set @hvoStart = %d%n"
				L"set @chvoStart = %d%n"
				L"set @hvoEnd = %d%n"
				L"set @chvoEnd = %d%n"
				L"declare @hdoc int%n"
				L"exec sp_xml_preparedocument @hdoc output, '<root>",
				hvoObj, hvoStart, cStart, hvoEnd, cEnd);
			for (int ihvo = ihvoMin; ihvo < ihvoLim; ++ihvo)
				stuVerify.FormatAppend(L"<Obj Id=\"%d\"/>", os.m_prghvo[ihvo]);
			stuVerify.FormatAppend(L"</root>'%n"
				L"declare @ordStart int, @ordEnd int, @cStart int, @cEnd int%n"
/*
-- We start by checking that the delimiter objects occur the correct number of times,
-- because the queries we use to find ordStart and ordEnd will just produce the last
-- occurrence if there are too few.
*/
				L"select @cStart = Count(*)%n"
				L"from %<0>s_%<1>s%n"
				L"where Src = @hvoObj and dst = @hvoStart%n"

				L"select @cEnd = COUNT(*)%n"
				L"from %<0>s_%<1>s%n"
				L"where Src = @hvoObj and dst = @hvoEnd%n"

				L"if (@cStart >= @chvoStart and @cEnd >= @chvoEnd) BEGIN%n"
/*
	-- By limiting the number of rows returned and using ascending order, we get the
	-- nth occurrence of this object in the property. Of those rows, the one with
	-- the max(ord) is the last, and hence nth.
	-- another approach is shown below which avoids using rowcount, but for SQL Server would
	-- force us to insert the number (minus 1) literally.
	--	select top 1 @ordStart = x.Ord
	--	from RnAnalysis_SupportingEvidence x
	--	left outer join (select top (@chvoStart - 1) Src, Dst, Ord
	--			from RnAnalysis_SupportingEvidence
	--			where Src = @hvoObj and dst = @hvoStart order by Ord asc) as y on y.Ord = x.Ord
	--	where y.Ord is null and x.Src = @hvoObj and x.Dst = @hvoStart
	--	order by x.Ord asc
*/
				L"	set rowcount @chvoStart%n"
				L"	select @ordStart = max(Ord)%n"
				L"	from %<0>s_%<1>s%n"
				L"	where Src = @hvoObj and dst = @hvoStart%n"
				L"	group by Ord order by Ord asc%n"
/*
	-- Same trick to get the ord of the other delimiter object
*/
				L"	set rowcount @chvoEnd%n"
				L"	select @ordEnd = max(Ord)%n"
				L"	from %<0>s_%<1>s%n"
				L"	where Src = @hvoObj and dst = @hvoEnd%n"
				L"	group by Ord order by Ord asc%n"

				L"	set rowcount 0%n"	// -- following queries return all rows
/*
	-- now compute the number of distinct IDs that occur in the relevant range
	-- of the property, the number of distinct IDs that we are trying to delete,
	-- and the number of that occur the same number of times in both lists.
	-- if all three numbers are equal, we've found the right objects in the
	-- expected places and can go ahead.
*/
				L"	declare @cGood int, @cIdsPres int, @cIdsDel int%n"
/*
	-- this counts the number of distinct values of dst that occur in the specified
	-- property of the specified object in the right range of ords.
*/
				L"	select @cIdsPres = count(*) from (%n"
				L"		select distinct dst%n"
				L"		from %<0>s_%<1>s%n"
				L"		where src = @hvoObj and ord >= @ordStart and ord <= @ordEnd) as x%n"
/*
	-- The first subselect produces the frequency of occurrence of each object in the
	-- relevant range of the property. The second produces the frequency of occurrence
	-- of each ID in the xml document. The join produces the cases where the same ID has
	-- the same count in both places.
*/
	L"	select @cGood = count(*) from (%n"
	L"		select dst ObjId, count(*) PresCnt%n"
	L"		from %<0>s_%<1>s%n"
	L"		where src = @hvoObj and ord >= @ordStart and ord <= @ordEnd group by Dst) as pc%n"
	L"		join (select [Id] ObjId, count(*) DelCnt%n"
	L"			from openxml(@hdoc, '/root/Obj') with ([Id] int) group by [Id]) as dc%n"
	L"			on pc.ObjId = dc.ObjId and dc.DelCnt = pc.PresCnt;%n"
/*
	-- This counts distinct IDs in the xml document.
*/
				L"	select @cIdsDel = count(*) from (%n"
				L"		select distinct [Id]%n"
				L"		from openxml(@hdoc, '/root/Obj') with ([Id] int) group by [Id]) as x%n"

				L"	if @cGood = @cIdsDel and @cGood = @cIdsPres%n"
				L"		select 1%n"
				L"	else%n"
				L"		select 0%n"
				L"%n"
				L"END%n"
				L"ELSE BEGIN%n"
				L"	select 0%n"
				L"END%n"
/*
-- clean up the xml document created above
*/
				L"if @hdoc is not null exec sp_xml_removedocument @hdoc%n",
/*
-- for debugging you can enable this to see the partial results.
--select @ordStart ordStart, @ordEnd ordEnd, @cGood good, @cIdsPres Present, @cIdsDel del
*/
				pszClass, pszField);
		}
	}
	else if (cpt == kcptReferenceCollection)
	{
		if (chvoIns == 0 && ihvoLim - ihvoMin == 1)
		{
			// optimize for single delete
			stuSql.Format(
				L"set ROWCOUNT 1 "
				L"delete from %s_%s where Src = %d and Dst = %d "
				L"set ROWCOUNT 0",
				pszClass, pszField, hvoObj, os.m_prghvo[ihvoMin]);
			stuVerify.Format(L"select count(*) from %s_%s where Src = %d and Dst = %d",
				pszClass, pszField, hvoObj, os.m_prghvo[ihvoMin]);
		}
		else if (ihvoLim == ihvoMin && chvoIns == 1)
		{
			// optimize for single insert
			stuSql.Format(
				L"insert into %s_%s values (%d,%d)", pszClass, pszField, hvoObj, prghvo[0]);
			stuVerify.Assign(L"select 1"); // always OK to insert
		}
		else
		{
			// Ids to insert and delete. e.g., '4708,4709'
			int ihvo;
			if (chvoIns)
			{
				for (ihvo = 0; ihvo < chvoIns; ++ihvo)
					stuInsIds.FormatAppend(L"%d,", prghvo[ihvo]);
				stuInsIds.Format(L"'%s'", stuInsIds.Chars());
			}
			if (ihvoMin < ihvoLim)
			{
				for (ihvo = ihvoMin; ihvo < ihvoLim; ++ihvo)
					stuDelIds.FormatAppend(L"%d,", os.m_prghvo[ihvo]);
				stuDelIds.Format(L"'%s'", stuDelIds.Chars());
			}
			if (stuInsIds.Length() || stuDelIds.Length())
			{
				stuSql.Assign(L" ");
				stuSql.FormatAppend(L"exec ReplaceRefColl$ %d, %d, %s, %s;", tag, hvoObj,
					(chvoIns) ? stuInsIds.Chars() : L"NULL",
					(ihvoMin < ihvoLim) ? stuDelIds.Chars() : L"NULL");
			}

			// Generate the verification SQL for replacing in a ref collection.
			stuVerify.Assign(L"declare @hdoc int\r\n"
				L"exec sp_xml_preparedocument @hdoc output, '<root>");
			for (int i = ihvoMin; i < ihvoLim; ++i)
				stuVerify.FormatAppend(L"<Obj Id=\"%d\"/>", os.m_prghvo[i]);
			stuVerify.FormatAppend(L"</root>'\r\n"
				L"declare @cBad int\r\n"
				L"select @cBad = count(*)\r\n"
				L"from (select dst ObjId, count(*) PresCnt\r\n"
				L"	from	%s_%s jt where jt.src = %d\r\n"
				L"	group by Dst) pc\r\n"
				L"join (select	[Id] ObjId, count(*) DelCnt\r\n"
				L"	from	openxml(@hdoc, '/root/Obj') with ([Id] int)\r\n"
				L"	group by [Id]) dc on pc.ObjId = dc.ObjId\r\n"
				L"where dc.DelCnt > pc.PresCnt;\r\n"
				L"if @cBad = 0\r\n"
				L"	select 1\r\n"
				L"else\r\n"
				L"	select 0",
				pszClass, pszField, hvoObj);
		}
	}
	else
	{
		Assert(false); // Do not use this method for atomic or owning properties!
	}

}

//:>********************************************************************************************
//:>	VwOleDbDa
//:>	The "SetObjProp" method changes the values of atomic REFERENCES and the "Replace"
//:>	method changes the values of collection/sequence references.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Replace a range of objects in a collection or sequence (ie. a vector prop).  The objects
	delineated by the indexes ihvoMin and ihvoLim in the collection or sequence are deleted and
	the array of objects given in prghvo are inserted just before ihvoMin.  Note that if ihvoMin
	and ihvoLim are the same, no deletion occurs, just a simple insertion.  To insert the array
	of new objects at the start of the collection or sequence, designate an ihvoMin of zero.
	To append the array to the end, designate an ihvoMin=ihvoLim=(number of items currently
	in the vector).

	${ISilDataAccess#Replace}, ${VwCacheDa#Replace}
	// Todo JohnT(Undo): rework this method. Break out the code that sets up the lists
	// of objects to insert, and execute it twice, once for the objects to insert, and
	// once for the ones to delete (to create SQL for re-inserting them).
	// verify code needs to check that the objects that Un/Redo will delete are present
	// (at roughly the right positions, for sequences), and that the ones that will be
	// inserted exist.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::Replace(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
	HVO * prghvo, int chvoIns)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prghvo, chvoIns);

	// Check for no actual change...important to detect this and not create an empty SQL
	// command, then no OleDbCommand gets created and we start getting internal errors setting
	// up the Undo.
	if (ihvoLim == ihvoMin && chvoIns == 0)
		return S_OK;

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvoObj != 0);
	switch(TryVirtualReplace(hvoObj, tag, ihvoMin, ihvoLim, prghvo, chvoIns))
	{
	case kwvDone:
		return S_OK;
	case kwvCache:
		return ReplaceAux(hvoObj, tag, ihvoMin, ihvoLim, prghvo, chvoIns);
	}
	//  Make sure the given parameters values are OK.
	ObjPropRec oprKey(hvoObj, tag);
	if (chvoIns && !prghvo)
	{
		ThrowHr(WarnHr(E_POINTER));
	}
	if (ihvoMin < 0 || ihvoMin > ihvoLim)
	{
		ThrowHr(WarnHr(E_INVALIDARG));
	}

	//  If the ObjSeq record is not in the hash map (of the cache) already, the only
	//  valid replacement parameters are 0,0
	ObjSeq os;
	if (!m_hmoprsobj.Retrieve(oprKey, &os))
	{
		if (ihvoLim)
		{
			ThrowHr(WarnHr(E_FAIL));
		}
		else
		{
			// Work with empty old list.
			os.m_cobj = 0;
		}
	}
	if (ihvoLim > os.m_cobj)
	{
		ThrowHr(WarnHr(E_INVALIDARG));
	}


	//  Save the changes to the database.
	int cpt;
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFieldName;
	StrUni stuSql;

	//  Obtain information from the flid and take appropriate action.
	m_qmdc->GetOwnClsName(tag, &sbstrClsName);
	m_qmdc->GetFieldName(tag, &sbstrFieldName);
	m_qmdc->GetFieldType(tag, &cpt);
	ISqlUndoActionPtr qsqlua;
	Vector<HVO> vhvoDel; // objects deleted (save if we have an action handler)

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if ((m_qacth) &&
		((cpt == kcptReferenceSequence) || (cpt == kcptReferenceCollection)))
	{
		// Create the SqlUndoAction
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.

		// Reload the reference collection vector.
		StrUni stuReload;
		stuReload.Format(L"select [dst] from %s_%s where [src]=%d",
			sbstrClsName.Chars(), sbstrFieldName.Chars(), hvoObj);

		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctObjVec, 0, tag, 0));
		// Note:  The undo and redo reload statements are the same.
		qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvoObj, 0, NULL);
		qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvoObj, 0, NULL);

		if (ihvoLim > ihvoMin)
		{
			// Save the list of deleted objects to use in constructing the Undo sql.
			vhvoDel.Resize(ihvoLim - ihvoMin);
			::memcpy(vhvoDel.Begin(), os.m_prghvo + ihvoMin, (ihvoLim - ihvoMin) * sizeof(HVO));
		}
	}

	StrUni stuVerify;
	BuildReplaceCommands(hvoObj, tag, cpt, prghvo, chvoIns, ihvoMin, ihvoLim, os,
		sbstrClsName.Chars(), sbstrFieldName.Chars(), stuSql, stuVerify);
	//  Call the stored procedure that executes the change.
	CheckHr(m_qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));

	//  Save changes to the cache.
	InformNowDirty();
	ReplaceAux(hvoObj, tag, ihvoMin, ihvoLim, prghvo, chvoIns);
	// The database stored procedure updated the timestamp on the owner.
	// We need to make sure the cache copy is kept in sync.
	CheckHr(CacheCurrTimeStamp(hvoObj));

	// Add undo/redo command to SqlUndoAction, and the action to the stack.
	if (m_qacth)
	{
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));
		if (stuVerify.Length())
			CheckHr(qsqlua->VerifyRedoable(m_qode, qodc, stuVerify.Bstr()));
		m_hmoprsobj.Retrieve(oprKey, &os); // get the new value.
		// Now build new strings based on the new value.
		BuildReplaceCommands(hvoObj, tag, cpt, vhvoDel.Begin(), vhvoDel.Size(), ihvoMin,
			ihvoMin + chvoIns, os, sbstrClsName.Chars(), sbstrFieldName.Chars(),
			stuSql, stuVerify);
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodc, stuSql.Bstr()));
		if (stuVerify.Length())
			CheckHr(qsqlua->VerifyUndoable(m_qode, qodc, stuVerify.Bstr()));

		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	Utility function to make an SQL query that sets property (pszClass)_(pszField) of object
	hvo to nVal.
----------------------------------------------------------------------------------------------*/
StrUni MakeSetObjSql(const OLECHAR * pszClass, const OLECHAR * pszField, HVO hvo, HVO hvoObj)
{
	StrUni stuArg(L"null");
	if (hvoObj)
		stuArg.Format(L"%d", hvoObj);
	StrUni stuSql;
	stuSql.Format(L"update [%s] set [%s]=%s where id=%d", pszClass,
		pszField, stuArg.Chars(), hvo);
	return stuSql;
}

/*----------------------------------------------------------------------------------------------
	Utility function to make an SQL query that tests whether property (pszClass)_(pszField) of
	object hvo is hvoObj (null if hvoObj is zero). It produces a one-row rowset with one column
	that is non-zero if the property has the indicated value.
----------------------------------------------------------------------------------------------*/
StrUni MakeVerifyObjSql(const OLECHAR * pszClass, const OLECHAR * pszField, HVO hvo, HVO hvoObj)
{
	// stuArg is the bit of the query that needs to change depending on whether hvoObj is zero.
	StrUni stuArg(L"is null");
	if (hvoObj)
		stuArg.Format(L"= %d", hvoObj);
	StrUni stuSql;
	stuSql.Format(L"select count(id) from %s where %s %s and id = %d",
		pszClass, pszField, stuArg.Chars(), hvo);
	return stuSql;
}

/*----------------------------------------------------------------------------------------------
	Set an object reference value.  To set the atomic reference (ie. the database column) to
	NULL, simply set hvoObj=0

	${ISilDataAccess#SetObjProp}, ${VwCacheDa#SetObjProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetObjProp(HVO hvo, PropTag tag, HVO hvoObj)
{
	BEGIN_COM_METHOD;

	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	Assert(hvo != 0);
	switch(TryVirtualAtomic(hvo, tag, hvoObj))
	{
	case kwvDone:
		return S_OK;
	case kwvCache:
		SetObjPropVal(hvo, tag, hvoObj);
		return S_OK;
	}

	//  Update the Int in the database.
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	StrUni stuSql;

	//  Get the field$ info from the tag.
	CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	//  Form the SQL query, set the parameters, and execute the command.
	CheckHr(m_qode->CreateCommand(&qodc));
	stuSql = MakeSetObjSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, hvoObj);

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if (m_qacth)
	{
		// Create the SqlUndoAction and set the RedoCommand
		ISqlUndoActionPtr qsqlua;
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));

		// Create the sql for the Undo.
		HVO hvoOld;
		CheckHr(get_ObjectProp(hvo, tag, &hvoOld));
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodc,
			MakeSetObjSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, hvoOld).Bstr()));

		// Create sql to verify we can Undo, that is, that the current value of the property
		// is what we expect.
		CheckHr(qsqlua->VerifyUndoable(m_qode, qodc,
			MakeVerifyObjSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, hvoObj).Bstr()));
		CheckHr(qsqlua->VerifyRedoable(m_qode, qodc,
			MakeVerifyObjSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, hvoOld).Bstr()));

		// Set the "reload" SQL command.
		StrUni stuReload;
		stuReload.Format(L"select [id], [%s] from [%s] where [id] = %d",
			sbstrFldName.Chars(), sbstrClsName.Chars(), hvo);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctObj, 1, tag, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));

		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	// Actually execute the command.
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// The database trigger updated the timestamp on the owner, so we need to keep the
	// cached copy current.
	CheckHr(CacheCurrTimeStamp(hvo));

	// Update the Int in the cache.
	CheckHr(SuperClass::SetObjProp(hvo, tag, hvoObj));

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


//:>********************************************************************************************
//:>	VwOleDbDa methods used to change object PROPERTY information (outside of reference
//:>	properties).
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Sets a binary or varbinary column in the database.
	To set the value to NULL, simply set cb = 0

	${ISilDataAccess#SetBinary}, ${VwCacheDa#SetBinary}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetBinary(HVO hvo, PropTag tag, byte * prgb, int cb)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(prgb);

	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	Assert(hvo != 0);
	//  Update the binary field in the database.
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	StrUni stuSql;

	//  Get the field$ info from the tag.
	CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	//  Form the SQL query, set the parameters, and execute the command.
	CheckHr(m_qode->CreateCommand(&qodc));
	stuSql.Format(L"update [%s] set [%s]=? where id=%d", sbstrClsName.Chars(),
		sbstrFldName.Chars(), hvo);
	if (cb)
	{
		CheckHr(qodc->SetParameter(1,
			DBPARAMFLAGS_ISINPUT,
			NULL,
			DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(prgb),
			cb));
	}
	else
	{
		CheckHr(qodc->SetParameter(1,
			DBPARAMFLAGS_ISINPUT,
			NULL,
			DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(NULL),
			cb));
	}

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if (m_qacth)
	{
		// Create the SqlUndoAction and set the RedoCommand
		ISqlUndoActionPtr qsqlua;
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));

		// And the Undo command. Do a normal get (without retrieving data) to make sure
		// it is loaded.
		int cbOld;
		CheckHr(BinaryPropRgb(hvo, tag, NULL, 0, &cbOld));
		// Then we can use this shorthand to get the bytes.
		ObjPropRec oprKey(hvo, tag);
		StrAnsi sta;
		m_hmoprsta.Retrieve(oprKey, &sta);
		IOleDbCommandPtr qodcUndo;
		CheckHr(m_qode->CreateCommand(&qodcUndo));
		if (cbOld)
		{
			CheckHr(qodcUndo->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(const_cast<char *>(sta.Chars())), cbOld));
		}
		else
		{
			CheckHr(qodcUndo->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(NULL), cbOld));
		}
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodcUndo, stuSql.Bstr()));

		StrUni stuVerify;
		stuVerify.Format(L"select count(id) from %s where id = %d and %s like ?",
			sbstrClsName.Chars(), hvo, sbstrFldName);
		StrUni stuVerifyEmpty;
		if (cbOld == 0 || sta.Length() == 0)
		{
			// We need a special verify if the property is empty, because (the old value particluarly)
			// may well be null, and X like ? does not match when the value is null and ? is
			// an empty byte array. However, we may well set it to an empty array, so our code
			// has to treat the two the same.
			stuVerifyEmpty.Format(L"select count(id) from %s where id = %d and (%s like ? or %s is null)",
				sbstrClsName.Chars(), hvo, sbstrFldName.Chars(), sbstrFldName.Chars());
		}
		CheckHr(qsqlua->VerifyUndoable(m_qode, qodc,
			(cb == 0 ? stuVerifyEmpty.Bstr() : stuVerify.Bstr())));
		CheckHr(qsqlua->VerifyRedoable(m_qode, qodcUndo,
			(cbOld == 0 ? stuVerifyEmpty.Bstr() : stuVerify.Bstr())));

		// Set the "reload" SQL command.
		StrUni stuReload;
		stuReload.Format(L"select [id], [%s] from [%s] where [id] = %d",
			sbstrFldName.Chars(), sbstrClsName.Chars(), hvo);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctBinary, 1, tag, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));

		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	// Actually execute the command.
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// The database trigger updated the timestamp on the owner, so we need to keep the
	// cached copy current.
	CheckHr(CacheCurrTimeStamp(hvo));

	// Update the Int in the cache.
	CheckHr(SuperClass::SetBinary(hvo, tag, prgb, cb));

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetGuid}, ${VwCacheDa#SetGuid}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetGuid(HVO hvo, PropTag tag, GUID uid)
{
	BEGIN_COM_METHOD;

	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	Assert(hvo != 0);
	//  Update the GUID in the database.
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	StrUni stuSql;

	//  Get the field$ info from the tag.
	CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	//  Form the SQL query, set the parameters, and execute the command.
	CheckHr(m_qode->CreateCommand(&qodc));
	stuSql.Format(L"update [%s] set [%s]=? where id=%d",
		sbstrClsName.Chars(), sbstrFldName.Chars(), hvo);
	CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_GUID,
		reinterpret_cast<ULONG *>(&uid), sizeof(uid)));

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if (m_qacth)
	{
		// Create the SqlUndoAction and set the RedoCommand
		ISqlUndoActionPtr qsqlua;
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));

		// And set the UndoCommand.
		GUID uidOld;
		CheckHr(get_GuidProp(hvo, tag, &uidOld));
		IOleDbCommandPtr qodcUndo;
		CheckHr(m_qode->CreateCommand(&qodcUndo));
		CheckHr(qodcUndo->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_GUID,
			reinterpret_cast<ULONG *>(&uidOld), sizeof(uidOld)));
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodcUndo, stuSql.Bstr()));

		StrUni stuVerify;
		stuVerify.Format(L"select COUNT(id) from [%s] WHERE id = %d and [%s] = ?",
			sbstrClsName.Chars(), hvo, sbstrFldName.Chars());
		StrUni stuVerifyNull;
		if (uidOld == GUID_NULL || uid == GUID_NULL)
		{
			stuVerifyNull.Format(L"select COUNT(id) from [%s] WHERE id = %d and (%s = ? or %s is null)",
				sbstrClsName.Chars(), hvo, sbstrFldName.Chars(), sbstrFldName.Chars());
		}
		CheckHr(qsqlua->VerifyUndoable(m_qode, qodc,
			(uid == GUID_NULL ? stuVerifyNull.Bstr() : stuVerify.Bstr())));
		CheckHr(qsqlua->VerifyRedoable(m_qode, qodcUndo,
			(uidOld == GUID_NULL ? stuVerifyNull.Bstr() : stuVerify.Bstr())));

		// Set the "reload" SQL command.
		StrUni stuReload;
		stuReload.Format(L"select [id], [%s] from [%s] where [id] = %d",
			sbstrFldName.Chars(), sbstrClsName.Chars(), hvo);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctGuid, 1, tag, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));

		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	// Actually execute the command.
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// The database trigger updated the timestamp on the owner, so we need to keep the
	// cached copy current.
	CheckHr(CacheCurrTimeStamp(hvo));

	//  Update the Int in the cache.
	CheckHr(SuperClass::SetGuid(hvo, tag, uid));

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	Utility function to set property (pszClass)_(pszField) of object hvo to nVal.
----------------------------------------------------------------------------------------------*/
StrUni MakeSetIntSql(const OLECHAR * pszClass, const OLECHAR * pszField, HVO hvo, int nVal)
{
	StrUni stuSql;
	// Note square delimiters allow field names to have SQL reserved words.
	stuSql.Format(L"update [%s] set [%s]=%d where id=%d", pszClass, pszField, nVal, hvo);
	return stuSql;
}

/*----------------------------------------------------------------------------------------------
	Utility function to make an SQL query that tests whether property (pszClass)_(pszField) of
	object hvo is nVal.  It produces a one-row rowset with one column that is non-zero if the
	property has the indicated value.
----------------------------------------------------------------------------------------------*/
StrUni MakeVerifyIntSql(const OLECHAR * pszClass, const OLECHAR * pszField, HVO hvo, int nVal)
{
	StrUni stuSql;
	stuSql.Format(L"select count(id) from [%s] where [%s]=%d and id = %d",
		pszClass, pszField, nVal, hvo);
	return stuSql;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetInt}, ${VwCacheDa#SetInt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetInt(HVO hvo, PropTag tag, int n)
{
	BEGIN_COM_METHOD;
	switch(TryWriteVirtualInt64(hvo, tag, n))
	{
	case kwvDone:
		return S_OK;
	case kwvCache:
		SetIntVal(hvo, tag, n);
		return S_OK;
	// otherwise not virtual, do normal write.
	}
	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	if (!hvo)
		ThrowHr(WarnHr(E_INVALIDARG));

	//  Update the Int in the database.

	//  Get the field$ info from the tag.
	SmartBstr sbstrFldName;
	// The try-catch blocks here prevent an assert every time we click on a checkbox in a
	// browse view.  See Generic/Throwable.h for an explanation.
	try
	{
		CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
	}
	catch (Throwable & thr)
	{
		HRESULT hr = thr.Result();
		if (hr == E_INVALIDARG)
		{
			// JohnT: this is a temporary expedient to support the click behavior associated
			// with AddIntPropPic. Eventually, with proper support of virtual properties,
			// we will not need 'fake' ones that can't be written properly. For now, if
			// the property is not recognized, just stick the new value in the cache.
			SetIntVal(hvo, tag, n);
			return S_OK;
		}
		else
		{
			throw thr;
		}
	}
	SmartBstr sbstrClsName;
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	//  Form the SQL query, set the parameters, and execute the command.
	IOleDbCommandPtr qodc;
	CheckHr(m_qode->CreateCommand(&qodc));
	StrUni stuSql = MakeSetIntSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, n);

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if (m_qacth)
	{
		// Create the SqlUndoAction and set the RedoCommand
		ISqlUndoActionPtr qsqlua;
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));

		// And set the UndoCommand.
		int nOld;
		CheckHr(get_IntProp(hvo, tag, &nOld));
		StrUni stuUndo = MakeSetIntSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, nOld);
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodc, stuUndo.Bstr()));

		// Create sql to verify we can Undo, that is, that the current value of the property
		// is what we expect.
		CheckHr(qsqlua->VerifyUndoable(m_qode, qodc,
			MakeVerifyIntSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, n).Bstr()));
		CheckHr(qsqlua->VerifyRedoable(m_qode, qodc,
			MakeVerifyIntSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, nOld).Bstr()));

		// Set the "reload" SQL command.
		StrUni stuReload;
		stuReload.Format(L"select [id], [%s] from [%s] where [id] = %d",
			sbstrFldName.Chars(), sbstrClsName.Chars(), hvo);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctInt, 1, tag, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));

		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	// Actually execute the command
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// The database trigger updated the timestamp on the owner, so we need to keep the
	// cached copy current.
	CheckHr(CacheCurrTimeStamp(hvo));

	//  Update the Int in the cache.
	SetIntVal(hvo, tag, n);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetBoolean}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetBoolean(HVO hvo, PropTag tag, ComBool f)
{
	BEGIN_COM_METHOD;
	// NOTE: For TRUE, we need to pass in an explicit 1 because when we attempt a redo, it
	// checks the value in the DB to make sure it's actual value is the same as when we set it.
	// Since booleans are retrieved as ints with a value of 0 or 1, any attempt to set this to
	// some other "true" value will cause redo to fail. ComBool's true value is -1, but that
	// won't work.
	// return SetInt(hvo, tag, f ? 1 : 0);
	// THIS IS  NO LONGER TRUE (at least with SQL Server 2005). The COM objects we use to
	// retrieve the data from SQL server convert the value to a -1 even when it gets stored as
	// 1. If we set the value in the cache to 1 then the Redo fails (TE-6256).
	//
	// NOTE: this code is similar to the code in VwOleDbDa::SetInt except that we want to store
	// -1 as value in the cache, but use 1 in the SQL queries.

	// we have to use a tertiary here, otherwise our ComBool implementation casts it to a
	// bool which gets converted to 1.
	int nCacheValue = f ? -1 : 0;

	switch(TryWriteVirtualInt64(hvo, tag, nCacheValue))
	{
	case kwvDone:
		return S_OK;
	case kwvCache:
		SetIntVal(hvo, tag, nCacheValue);
		return S_OK;
	// otherwise not virtual, do normal write.
	}

	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	if (!hvo)
		ThrowHr(WarnHr(E_INVALIDARG));

	//  Update the Int in the database.

	//  Get the field$ info from the tag.
	SmartBstr sbstrFldName;
	// The try-catch blocks here prevent an assert every time we click on a checkbox in a
	// browse view.  See Generic/Throwable.h for an explanation.
	try
	{
		CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
	}
	catch (Throwable & thr)
	{
		HRESULT hr = thr.Result();
		if (hr == E_INVALIDARG)
		{
			// JohnT: this is a temporary expedient to support the click behavior associated
			// with AddIntPropPic. Eventually, with proper support of virtual properties,
			// we will not need 'fake' ones that can't be written properly. For now, if
			// the property is not recognized, just stick the new value in the cache.
			SetIntVal(hvo, tag, nCacheValue);
			return S_OK;
		}
		else
		{
			throw thr;
		}
	}
	SmartBstr sbstrClsName;
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	//  Form the SQL query, set the parameters, and execute the command.
	IOleDbCommandPtr qodc;
	CheckHr(m_qode->CreateCommand(&qodc));
	StrUni stuSql = MakeSetIntSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, f);

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if (m_qacth)
	{
		// Create the SqlUndoAction and set the RedoCommand
		ISqlUndoActionPtr qsqlua;
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));

		// And set the UndoCommand.
		ComBool fOld;
		CheckHr(get_BooleanProp(hvo, tag, &fOld));
		StrUni stuUndo = MakeSetIntSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, fOld);
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodc, stuUndo.Bstr()));

		// Create sql to verify we can Undo, that is, that the current value of the property
		// is what we expect.
		CheckHr(qsqlua->VerifyUndoable(m_qode, qodc,
			MakeVerifyIntSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, f).Bstr()));
		CheckHr(qsqlua->VerifyRedoable(m_qode, qodc,
			MakeVerifyIntSql(sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, fOld).Bstr()));

		// Set the "reload" SQL command.
		StrUni stuReload;
		stuReload.Format(L"select [id], [%s] from [%s] where [id] = %d",
			sbstrFldName.Chars(), sbstrClsName.Chars(), hvo);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctInt, 1, tag, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));

		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	// Actually execute the command
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// The database trigger updated the timestamp on the owner, so we need to keep the
	// cached copy current.
	CheckHr(CacheCurrTimeStamp(hvo));

	//  Update the Int in the cache.
	SetIntVal(hvo, tag, nCacheValue);

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetInt64}, ${VwCacheDa#SetInt64}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetInt64(HVO hvo, PropTag tag, int64 lln)
{
	BEGIN_COM_METHOD;

	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	Assert(hvo != 0);
	switch(TryWriteVirtualInt64(hvo, tag, lln))
	{
	case kwvDone:
		return S_OK;
	case kwvCache:
		SetInt64Val(hvo, tag, lln);
		return S_OK;
	// otherwise not virtual, do normal write.
	}
	//  Update the Int64 in the database.
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	StrUni stuSql;

	//  Get the field$ info from the tag.
	CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	//  Form the SQL query, set the parameters, and execute the command.
	CheckHr(m_qode->CreateCommand(&qodc));
	stuSql.Format(L"update [%s] set [%s]=? where id=%d", sbstrClsName.Chars(),
		sbstrFldName.Chars(), hvo);
	CheckHr(qodc->SetParameter(1,
		DBPARAMFLAGS_ISINPUT,
		NULL,
		DBTYPE_I8,
		reinterpret_cast<ULONG *>(&lln),
		sizeof(int64)));

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if (m_qacth)
	{
		// Create the SqlUndoAction and set the RedoCommand
		ISqlUndoActionPtr qsqlua;
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));

		// And set the UndoCommand.
		int64 llnOld;
		CheckHr(get_Int64Prop(hvo, tag, &llnOld));
		IOleDbCommandPtr qodcUndo;
		CheckHr(m_qode->CreateCommand(&qodcUndo));
		CheckHr(qodcUndo->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_GUID,
			reinterpret_cast<ULONG *>(&llnOld), sizeof(llnOld)));
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodcUndo, stuSql.Bstr()));

		StrUni stuVerify;
		stuVerify.Format(L"select COUNT(id) from [%s] WHERE id = %d and [%s] = ?",
			sbstrClsName.Chars(), hvo, sbstrFldName.Chars());
		CheckHr(qsqlua->VerifyUndoable(m_qode, qodc, stuVerify.Bstr()));
		CheckHr(qsqlua->VerifyRedoable(m_qode, qodcUndo, stuVerify.Bstr()));

		// Set the "reload" SQL command.
		StrUni stuReload;
		stuReload.Format(L"select [id], [%s] from [%s] where [id] = %d",
			sbstrFldName.Chars(), sbstrClsName.Chars(), hvo);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctInt64, 1, tag, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));

		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	// Actually execute the command
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// The database trigger updated the timestamp on the owner, so we need to keep the
	// cached copy current.
	CheckHr(CacheCurrTimeStamp(hvo));

	//  Update the Int64 in the cache.
	CheckHr(SuperClass::SetInt64(hvo, tag, lln));

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	Set parameter 1 of *podc to the text of *ptss, and (if fWantFmt is true)
	also set parameter 2 to the format data.
----------------------------------------------------------------------------------------------*/
void SetupCommandParamsFromTsString(IOleDbCommand * podc, ITsString * ptss, bool fWantFmt)
{
	SmartBstr sbstrText;
	CheckHr(ptss->get_Text(&sbstrText));
	CheckHr(podc->SetParameter(1,
		DBPARAMFLAGS_ISINPUT,
		NULL,
		DBTYPE_WSTR,
		(ULONG *)(sbstrText.Chars()),
		sbstrText.Length() * 2));
	if (fWantFmt)
	{
		int cbFmtBufSize = kcbFmtBufMax;
		int cbFmtSpaceTaken;
		byte rgbFmt[kcbFmtBufMax]; // Use this for simple strings
		byte * prgbFmt = rgbFmt;
		Vector<byte> vbFmt; // Use this if format won't fit in rgbFmt; automatic cleanup.
		// Copy "format" information of the TsString to the byte array "rgbFmt".
		HRESULT hr = ptss->SerializeFmtRgb(prgbFmt, cbFmtBufSize, &cbFmtSpaceTaken);
		if (hr != S_OK)
		{
			if (hr == S_FALSE)
			{
				// If the supplied buffer is too small, try it again with the value
				// that cbFmtSpaceTaken was set to. If this fails, throw error.
				vbFmt.Resize(cbFmtSpaceTaken);
				prgbFmt = vbFmt.Begin();
				CheckHr(ptss->SerializeFmtRgb(prgbFmt, cbFmtSpaceTaken, &cbFmtSpaceTaken));
			}
			else
			{
				ThrowHr(WarnHr(hr));
			}
		}
		// Param2 is the formatting information.
		CheckHr(podc->SetParameter(2,
			DBPARAMFLAGS_ISINPUT,
			NULL,
			DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(prgbFmt),
			cbFmtSpaceTaken));
	}
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetMultiStringAlt}, ${VwCacheDa#SetMultiStringAlt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetMultiStringAlt(HVO hvo, PropTag tag, int ws, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptss);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	//  Update the MultiString in the database.
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	StrUni stuSql;
	ITsStringPtr qtssNorm;
	CheckHr(ptss->get_NormalizedForm(knmNFD, &qtssNorm));

	switch (TryWriteVirtualObj(hvo, tag, ws, qtssNorm))
	{
	case kwvDone:
		return S_OK;
	case kwvCache:
		SetMultiStringAltVal(hvo, tag, ws, qtssNorm);
		return S_OK;
	// otherwise not virtual, do normal write.
	}

	//  Get the Field$ info.
	CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	// Get the command object for storing the data (and for Redo).
	CheckHr(m_qode->CreateCommand(&qodc));

	// Figure what kind of MultiString it is.
	int cpt;
	CheckHr(m_qmdc->GetFieldType(tag, &cpt));

	switch (cpt)
	{
	case kcptMultiString:
		//exec proc [SetMultiStr$]
		//	@flid int,
		//	@obj int,
		//	@enc int,
		//	@txt nvarchar(4000),
		//	@fmt varbinary(8000)
		stuSql.Format(L"exec SetMultiStr$ %d, %d, %d, ?, ?", tag, hvo, ws);
			break;
	case kcptMultiBigString:
		//exec proc [SetMultiBigStr$]
		//	@flid int,
		//	@obj int,
		//	@enc int,
		//	@txt ntext,
		//	@fmt image
		stuSql.Format(L"exec SetMultiBigStr$ %d, %d, %d, ?, ?", tag, hvo, ws);
			break;
	case kcptMultiUnicode:
		//exec proc [SetMultiTxt$]
		//	@flid int,
		//	@obj int,
		//	@enc int,
		//	@txt nvarchar(4000)
		stuSql.Format(L"exec SetMultiTxt$ %d, %d, %d, ?", tag, hvo, ws);
			break;
	case kcptMultiBigUnicode:
		//exec proc [SetMultiBigTxt$]
		//	@flid int,
		//	@obj int,
		//	@enc int,
		//	@txt ntext
		stuSql.Format(L"exec SetMultiBigTxt$ %d, %d, %d, ?", tag, hvo, ws);
			break;
	default:
		ThrowHr(WarnHr(E_UNEXPECTED));
		break;
	}

	SetupCommandParamsFromTsString(qodc, qtssNorm,
		cpt == kcptMultiString || cpt == kcptMultiBigString);

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	ISqlUndoActionPtr qsqlua;
	if (m_qacth)
	{
		// Create the SqlUndoAction and set the RedoCommand
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));

		// Get the command object for Undo, and set its parameters to the existing values.
		IOleDbCommandPtr qodcUndo;
		CheckHr(m_qode->CreateCommand(&qodcUndo));
		ITsStringPtr qtssOld;
		CheckHr(get_MultiStringAlt(hvo, tag, ws, &qtssOld));
		ITsStringPtr qtssNormOld;
		CheckHr(qtssOld->get_NormalizedForm(knmNFD, &qtssNormOld));
		SetupCommandParamsFromTsString(qodcUndo, qtssNormOld,
			cpt == kcptMultiString || cpt == kcptMultiBigString);
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodcUndo, stuSql.Bstr()));
		int cchOld, cchNew;
		CheckHr(qtssOld->get_Length(&cchOld));
		CheckHr(ptss->get_Length(&cchNew));

		// Enhance JohnT(Undo): See the comments for SetString() below.
		StrUni stuVerify;
		if (cpt == kcptMultiString || cpt == kcptMultiBigString)
		{
			stuVerify.Format(L"select COUNT(Obj) from %s_%s where Obj = %d and Ws = %d and "
				L"convert(varbinary(8000), convert(nvarchar(4000), Txt)) = "
				L"convert(varbinary(8000), convert(nvarchar(4000), ?)) and Fmt Like ?",
				sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, ws);
		}
		else
		{
			// Omit the Fmt, which plain Unicode fields don't have.
			stuVerify.Format(L"select COUNT(Obj) from %s_%s where Obj = %d and ws = %d and "
				L"convert(varbinary(8000), convert(nvarchar(4000), Txt)) = "
				L"convert(varbinary(8000), convert(nvarchar(4000), ?))",
				sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, ws);
		}
		StrUni stuVerifyEmpty;
		if (cchOld == 0 || cchNew == 0)
		{
			// Empty strings result in rows being removed from the table, so if the string is
			// empty, we have to check for there being no rows for that id and ws.
			stuVerifyEmpty.Format(
				L"declare @crow int, @res int "
				L"select @crow = count(obj) from %s_%s where obj = %d and ws = %d "
				L"if @crow = 0 set @res = 1 else set @res = 0 "
				L"select @res ",
				sbstrClsName.Chars(), sbstrFldName.Chars(), hvo, ws);
		}
		qsqlua->VerifyUndoable(m_qode, qodc,
			(cchNew == 0 ? stuVerifyEmpty.Bstr() : stuVerify.Bstr()));
		qsqlua->VerifyRedoable(m_qode, qodcUndo,
			(cchOld == 0 ? stuVerifyEmpty.Bstr() : stuVerify.Bstr()));

		// Set the "reload" SQL command.
		StrUni stuReload;
		if (cpt == kcptMultiUnicode)
		{
			// The only reason to join on CmObject is so that we can do a left outer join and get
			// a row even if there is nothing recorded for this hvo/ws combination in the
			// MultiTxt view/table. This allows us to load the string as empty even if restoring
			// it to absent.
			stuReload.Format(L"select co.id, mt.txt, %d, %d, cast(null as varbinary) from CmObject co "
				L"left outer join [%s_%s] mt "
				L"on mt.obj = co.id and ws=%d "
				L"where co.id =%d",
				tag, ws, sbstrClsName.Chars(), sbstrFldName.Chars(), ws, hvo);
		}
		else
		{
			stuReload.Format(L"select co.id, mt.txt, %d, %d, mt.fmt from CmObject co "
				L"left outer join [%s_%s] mt "
				L"on mt.obj = co.id and ws=%d "
				L"where co.id =%d",
				tag, ws, sbstrClsName.Chars(), sbstrFldName.Chars(), ws, hvo);
		}
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctMlaAlt, 1, 0, 0));
		CheckHr(qdcs->Push(koctFlid, 1, 0, 0));
		CheckHr(qdcs->Push(koctEnc, 1, 0, 0));
		CheckHr(qdcs->Push(koctFmt, 1, 0, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
	}

	// Actually execute the command
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// The database trigger updated the timestamp on the owner, so we need to keep the
	// cached copy current.
	CheckHr(CacheCurrTimeStamp(hvo));

	//  Update the MultiString in the cache.
	SetMultiStringAltVal(hvo, tag, ws, qtssNorm);

	if (m_qacth)
	{
		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetString}, ${VwCacheDa#SetString}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetString(HVO hvo, PropTag tag, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptss);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	HRESULT hr = E_FAIL;

	// Proceed with the change.
	//  Update the TsString in the database.
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	int cpt;
	StrUni stuSql;
	int cchString;
	ITsStringPtr qtssNorm;
	CheckHr(ptss->get_NormalizedForm(knmNFD, &qtssNorm));

	switch(TryWriteVirtualObj(hvo, tag, 0, qtssNorm))
	{
	case kwvDone:
		return S_OK;
	case kwvCache:
		SetStringVal(hvo, tag, qtssNorm);
		return S_OK;
	// otherwise not virtual, do normal write.
	}

	// Check that the length of the TsString is within the soft limit unless it is Big.
	// Get the Field type and check that it isn't kcptBigString, etc.
	CheckHr(m_qmdc->GetFieldType(tag, &cpt));
	if (cpt != kcptBigString && cpt != kcptMultiBigString &&
		cpt != kcptBigUnicode && cpt != kcptMultiBigUnicode)
	{
		CheckHr(qtssNorm->get_Length(&cchString));
		if (cchString > kcchMaxString)
		{
			// Notify the user and abandon the save.
			StrApp staOverflow(kstidOverflowText);
			StrApp sta;
			sta.Format(staOverflow.Chars(), kcchMaxString);
/*
			HWND hwnd = AfApp::Papp()->GetCurMainWnd()->Hwnd();
			::MessageBox(hwnd, sta.Chars(), "Error", MB_OK); // hwnd here makes the Box modal.
*/
			::MessageBox(NULL, sta.Chars(), _T("Error"), MB_OK | MB_TASKMODAL);
			ThrowHr(WarnHr(E_FAIL));

		}
	}

	//  Get the Field$ info.
	CheckHr(hr = m_qmdc->GetFieldName(tag, &sbstrFldName));
	CheckHr(hr = m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	ISqlUndoActionPtr qsqlua;
	if (m_qacth)
	{
		// Create the SqlUndoAction.
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
	}

	//  Form the SQL query, set the parameters, and execute the command.
	CheckHr(m_qode->CreateCommand(&qodc));
	SetupCommandParamsFromTsString(qodc, qtssNorm, cpt != kcptUnicode);
	if (cpt != kcptUnicode)
	{
		stuSql.Format(L"update [%s] set [%s]=?, %s_Fmt=? where id=%d", sbstrClsName.Chars(),
			sbstrFldName.Chars(), sbstrFldName.Chars(), hvo);
	}
	else
	{
		// Omit the _Fmt, which plain Unicode fields don't have.
		stuSql.Format(L"update [%s] set [%s]=? where id=%d", sbstrClsName.Chars(),
			sbstrFldName.Chars(), hvo);
	}

	if (m_qacth)
	{
		// command with params for Undo and verify redo, the old string.
		// sql to verify that string property has expected value and return 0 if not, non-zero
		// if so.
		IOleDbCommandPtr qodcUndo;
		StrUni stuVerify;
		// Enhance JohnT(Undo): The following only tests the first 4000 characters of the
		// string.  We can't, in general, use a simple = because many of our fields are NText
		// and SqlServer doesn't allow the = operator on NText.  Also Case differences are
		// ignored by =.  Like doesn't work because it ignores case (and also would require
		// various magic characters to be escaped).
		// Options that have been considered:
		// 1. Accept a tiny risk of undetected edit conflict (leave as is).
		// 2. Develop an extended stored procedure to compare NText...not sure, though, that
		//    NText can successfully be passed to an XSP.
		// 3. Research primitives within SQLServer that can manipulate parts of an NText.
		// 4. Add to SqlUndoAction an interface that allows undo to be verified by retrieving
		//    the text and fmt info, and do the comparison in C++ code.
		// 5. Decide to accept a maximum length of 4000 characters for all strings.
		// 6. Wait for the Firebird port (or SqlServer 5) and hope it is better.
		if (cpt != kcptUnicode)
		{
			// We could get away without the Convert(varbinary) for the Fmt by using like
			// for short format information, but if it gets large...don't know the exact limit,
			// but 11K broke it...like becomes a syntax error!
			stuVerify.Format(
				L"select COUNT(id) from %s where id = %d and convert(varbinary(8000), "
				L"convert(nvarchar(4000), %s)) = convert(varbinary(8000), convert(nvarchar(4000), ?)) "
				L"and convert(varbinary(8000),%s_Fmt) = convert(varbinary(8000), ?)",
				sbstrClsName.Chars(), hvo, sbstrFldName.Chars(), sbstrFldName.Chars());
		}
		else
		{
			// Omit the _Fmt, which plain Unicode fields don't have.
			stuVerify.Format(
				L"select COUNT(id) from %s where id = %d and convert(varbinary(8000), "
				L"convert(nvarchar(4000), %s)) = convert(varbinary(8000), convert(nvarchar(4000), ?))",
				sbstrClsName.Chars(), hvo, sbstrFldName.Chars());
		}
		ITsStringPtr qtssOld;
		CheckHr(get_StringProp(hvo, tag, &qtssOld));
		ITsStringPtr qtssNormOld;
		CheckHr(qtssOld->get_NormalizedForm(knmNFD, &qtssNormOld));

		StrUni stuVerifyNull;
		int cchOld, cchNew;
		CheckHr(qtssNorm->get_Length(&cchNew));
		CheckHr(qtssNormOld->get_Length(&cchOld));
		if (cchOld == 0 || cchNew == 0)
		{
			stuVerifyNull.Format(
				L"select COUNT(id) from %s where id = %d and (%s is null or len(convert(nvarchar(40), %s)) = 0)",
				sbstrClsName.Chars(), hvo, sbstrFldName.Chars(), sbstrFldName.Chars());
		}
		// qodcUndo is like qodc, but based on the OLD value of the string.
		CheckHr(m_qode->CreateCommand(&qodcUndo));
		SetupCommandParamsFromTsString(qodcUndo, qtssNormOld, cpt != kcptUnicode);

		qsqlua->AddUndoCommand(m_qode, qodcUndo, stuSql.Bstr());
		qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr());
		qsqlua->VerifyUndoable(m_qode, qodc,
			(cchNew == 0 ? stuVerifyNull.Bstr() : stuVerify.Bstr()));
		qsqlua->VerifyRedoable(m_qode, qodcUndo,
			(cchOld == 0 ? stuVerifyNull.Bstr() : stuVerify.Bstr()));

		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec);
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		// Set the "reload" SQL command, and usually push an extra cs.
		StrUni stuReload;
		if (cpt != kcptUnicode)
		{
			stuReload.Format(L"select [id], [%s], [%s_fmt] from [%s] "
				L"where [id] = %d", sbstrFldName.Chars(), sbstrFldName.Chars(),
				sbstrClsName.Chars(), hvo);
			CheckHr(qdcs->Push(koctString, 1, tag, 0));
			CheckHr(qdcs->Push(koctFmt, 1, tag, 0));
		}
		else
		{
			stuReload.Format(L"select [id], [%s] from [%s] "
				L"where [id] = %d", sbstrFldName.Chars(),
				sbstrClsName.Chars(), hvo);
			CheckHr(qdcs->Push(koctUnicode, 1, tag, 0));
		}
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL);
		qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL);
		//
	}

	// Actually execute the command.
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	// Now we've made the change, it makes sense to add the action to the undo stack.
	if (m_qacth)
	{
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	// The database stored procedure updated the timestamp on the owner.
	// We need to make sure the cache copy is kept in sync.
	// If we are modifying a string in a StTxtPara, the database updates the StTxtPara
	// plus the StText that owns it, plus the object that owns the StText, so we need to
	// update these time stamps as well.
	CheckHr(CacheCurrTimeStamp(hvo));
	if (tag == kflidStTxtPara_Contents || tag == kflidStTxtPara_Label)
	{
		HVO hvoParent;
		CheckHr(get_ObjOwner(hvo, &hvoParent));
		if(hvoParent)
		{
			CheckHr(CacheCurrTimeStamp(hvoParent));
			HVO hvoGrandparent;
			CheckHr(get_ObjOwner(hvoParent, &hvoGrandparent));
			if(hvoGrandparent)
			{
				CheckHr(CacheCurrTimeStamp(hvoGrandparent));
			}
		}
	}

	// Update the TsString in the cache.
	if (cpt != kcptUnicode)
		SetStringVal(hvo, tag, qtssNorm);
	else
	{
		SmartBstr sbstrVal;
		CheckHr(qtssNorm->get_Text(&sbstrVal));
		SetUnicodeVal(hvo, tag, const_cast<OLECHAR *>(sbstrVal.Chars()), sbstrVal.Length());
	}

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	Set the first parameter of an IOleDbCommand object to the time value given by tim.
----------------------------------------------------------------------------------------------*/
void SetTimeParamFromInt64(IOleDbCommand * podc, int64 tim)
{
	// This used to use a double to update the time value in the database. However, it
	// seemed that due to a bug in MSDE, the value returned was not the same as the value
	// we put into the database. I (DarrellZ) used Query Analyzer to ensure that the value
	// in the database was identical to the value we put there (when converted to a double),
	// but the value that was returned as a DBTIMESTAMP was still incorrect. So I changed
	// this method to put the time into a DBTIMESTAMP and then pass that to ExecCommand.
	// The method that returns a time from the database already used DBTIMESTAMP, so now
	// they both use the same method to store and get time values from the database.
	SilTime stim(tim);
	SilTimeInfo sti;
	stim.GetTimeInfo(&sti);
	DBTIMESTAMP dbts;
	dbts.year = (short)sti.year;
	dbts.month = (short)sti.ymon;
	dbts.day = (short)sti.mday;
	dbts.hour = (short)sti.hour;
	dbts.minute = (short)sti.min;
	dbts.second = (short)sti.sec;
	dbts.fraction = sti.msec * 1000000;

	CheckHr(podc->SetParameter(1,
		DBPARAMFLAGS_ISINPUT,
		NULL,
		DBTYPE_DBTIMESTAMP,
		reinterpret_cast<ULONG *>(&dbts),
		sizeof(DBTIMESTAMP)));
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetTime}, ${VwCacheDa#SetTime}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetTime(HVO hvo, PropTag tag, int64 tim)
{
	BEGIN_COM_METHOD;

	//  Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	//  object at all.
	Assert(hvo != 0);
	//  Update the Int64 in the database.
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	StrUni stuSql;

	//  Get the field$ info from the tag.
	CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	//  Form the SQL query, set the parameters, and execute the command.
	CheckHr(m_qode->CreateCommand(&qodc));
	stuSql.Format(L"update [%s] set [%s]=? where id=%d", sbstrClsName.Chars(),
		sbstrFldName.Chars(), hvo);

	SetTimeParamFromInt64(qodc, tim);

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	ISqlUndoActionPtr qsqlua;
	if (m_qacth)
	{
		int64 time_old;
		IOleDbCommandPtr qodcUndo;
		// Create the SqlUndoAction and set the RedoCommand
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));
		CheckHr(m_qode->CreateCommand(&qodcUndo));
		CheckHr(get_TimeProp(hvo, tag, &time_old));
		SetTimeParamFromInt64(qodcUndo, time_old);
		qsqlua->AddUndoCommand(m_qode, qodcUndo, stuSql.Bstr());

		// add the verification commands.
		// (Enhance: try to get this working if we ever need it. Leaving it out just means
		// that if user A changes a time property, then user B changes it again,
		// user A will still be able to Undo it.)
		//StrUni stuVerify;
		//stuVerify.Format(L"select COUNT(id) from [%s] where [id]=%d and [%s] = ?",
		//	sbstrClsName.Chars(), hvo, sbstrFldName.Chars());
		//StrUni stuVerifyNull;
		//if (time_old == 0 || tim == 0)
		//{
		//	// Allow null or 0 for a zero value.
		//	stuVerifyNull.Format(L"select COUNT(id) from [%s] where [id]=%d and (%s = ? or %s is null)",
		//		sbstrClsName.Chars(), hvo, sbstrFldName.Chars(), sbstrFldName.Chars());
		//}
		//qsqlua->VerifyUndoable(m_qode, qodc,
		//	(tim == 0 ? stuVerifyNull.Bstr() : stuVerify.Bstr()));
		//qsqlua->VerifyRedoable(m_qode, qodcUndo,
		//	(time_old == 0 ? stuVerifyNull.Bstr() : stuVerify.Bstr()));

		// Set the "reload" SQL command.
		StrUni stuReload;
		stuReload.Format(L"select [id], [%s] from [%s] where [id] = %d",
			sbstrFldName.Chars(), sbstrClsName.Chars(), hvo);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctTime, 1, tag, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
	}

	// Actually execute the command
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// The database trigger updated the timestamp on the owner, so we need to keep the
	// cached copy current.
	CheckHr(CacheCurrTimeStamp(hvo));

	//  Update the Int64 in the cache.
	CheckHr(SuperClass::SetInt64(hvo, tag, tim));

	if (m_qacth)
	{
		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	Use ICU objects/functions to normalize the Unicode string.
----------------------------------------------------------------------------------------------*/
void NormalizeUnicodeString(const OLECHAR * prgch, int cch, UnicodeString & ustOut)
{

	UErrorCode uerr = U_ZERO_ERROR;
	UnicodeString ustIn(prgch, cch);

	Normalizer::normalize(ustIn, UNORM_NFD, 0, ustOut, uerr);
	if (U_FAILURE(uerr))
		ThrowInternalError(E_FAIL, "Normalize failure in VwOleDbDa::SetUnicode");

}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetUnicode}, ${VwCacheDa#SetUnicode}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetUnicode(HVO hvo, PropTag tag, OLECHAR * prgch, int cch)
{
	BEGIN_COM_METHOD;
	UnicodeString ustOut;

	NormalizeUnicodeString(prgch, cch, ustOut);

	switch (TryWriteVirtualUnicode(hvo, tag, (OLECHAR *)ustOut.getBuffer(), ustOut.length()))
	{
	case kwvDone:
		return S_OK;
	case kwvCache:
		SetUnicodeVal(hvo, tag, (OLECHAR *)ustOut.getBuffer(), ustOut.length());
		return S_OK;
	// otherwise not virtual, do normal write.
	}
	ChkComArrayArg(prgch, cch);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);

	//  Update the Unicode value in the database.
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	StrUni stuSql;

	//  Get the field$ info from the tag.
	CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	//  Form the SQL query, set the parameters, and execute the command.
	CheckHr(m_qode->CreateCommand(&qodc));
	stuSql.Format(L"update [%s] set [%s]=? where id=%d", sbstrClsName.Chars(),
		sbstrFldName.Chars(), hvo);
	CheckHr(qodc->SetParameter(1,
		DBPARAMFLAGS_ISINPUT,
		NULL,
		DBTYPE_WSTR,
		reinterpret_cast<ULONG *>(const_cast<UChar *>(ustOut.getBuffer())),
		(ustOut.length() * sizeof(OLECHAR))));

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	ISqlUndoActionPtr qsqlua;
	if (m_qacth)
	{
		// Create the SqlUndoAction and set the RedoCommand
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));
		// set the UndoCommand.
		SmartBstr sbstrOld;
		CheckHr(get_UnicodeProp(hvo, tag, &sbstrOld));
		UnicodeString ustOld;
		NormalizeUnicodeString(sbstrOld.Chars(), sbstrOld.Length(), ustOld);
		IOleDbCommandPtr qodcUndo;
		CheckHr(m_qode->CreateCommand(&qodcUndo));
		CheckHr(qodcUndo->SetParameter(1,
			DBPARAMFLAGS_ISINPUT,
			NULL,
			DBTYPE_WSTR,
			reinterpret_cast<ULONG *>(const_cast<UChar *>(ustOld.getBuffer())),
			(ustOld.length() * sizeof(OLECHAR))));
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodcUndo, stuSql.Bstr()));
		// Set the verification commands.
		// Enhance JohnT(Undo): See the comments for SetString().
		StrUni stuVerify;
		stuVerify.Format(
			L"select COUNT(id) from %s where id = %d and convert(varbinary(8000), "
			L"convert(nvarchar(4000), %s)) = convert(varbinary(8000), convert(nvarchar(4000), ?))",
			sbstrClsName.Chars(), hvo, sbstrFldName.Chars());
		StrUni stuVerifyNull;
		if (sbstrOld.Length() == 0 || cch == 0)
		{
			stuVerifyNull.Format(
				L"select COUNT(id) from %s where id = %d and (%s is null or len(convert(nvarchar(40), %s)) = 0)",
				sbstrClsName.Chars(), hvo, sbstrFldName.Chars(), sbstrFldName.Chars());
		}
		qsqlua->VerifyUndoable(m_qode, qodc,
			(cch == 0 ? stuVerifyNull.Bstr(): stuVerify.Bstr()));
		qsqlua->VerifyRedoable(m_qode, qodcUndo,
			(sbstrOld.Length() == 0 ? stuVerifyNull.Bstr(): stuVerify.Bstr()));

		// Set the "reload" SQL command.
		StrUni stuReload;
		stuReload.Format(L"select [id], [%s] from [%s] where [id] = %d",
			sbstrFldName.Chars(), sbstrClsName.Chars(), hvo);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctUnicode, 1, tag, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
	}

	// Actually execute the command
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

	// The database trigger updated the timestamp on the owner, so we need to keep the
	// cached copy current.
	CheckHr(CacheCurrTimeStamp(hvo));

	//  Update the Unicode value in the cache.
	SetUnicodeVal(hvo, tag, (OLECHAR *)ustOut.getBuffer(), ustOut.length());

	if (m_qacth)
	{
		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	Utility function to stuff a serialized IUnknown into a IOleDbCommand parameter.  At the
	moment, it works only for ITsTextProps.
----------------------------------------------------------------------------------------------*/
void SetParameterForUnknown(IOleDbCommand * podc, IUnknown * punk)
{
	byte rgbFmt[kcbFmtBufMax];
	Vector<byte> vbFmt;
	byte * prgbFmt = rgbFmt;
	int cbFmtSpaceTaken = 0;
	ITsTextPropsPtr qttp;
	punk->QueryInterface(IID_ITsTextProps, (void **)&qttp);
	//  Add more QueryInterface calls here if we support more types.
	if (qttp)
	{
		CheckHr(qttp->SerializeRgb(rgbFmt, kcbFmtBufMax, &cbFmtSpaceTaken));
		if (cbFmtSpaceTaken > kcbFmtBufMax)
		{
			vbFmt.Resize(cbFmtSpaceTaken);
			prgbFmt = vbFmt.Begin();
			CheckHr(qttp->SerializeRgb(vbFmt.Begin(), vbFmt.Size(), &cbFmtSpaceTaken));
		}
	}
	//  Add more clauses here if we support more types.
	//  Each should appropriately convert its type into rgbFmt, noting the
	//  length in cbFmtSpaceTaken.
	else
	{
		//  Not a type of object we know how to convert to binary
		ThrowHr(WarnHr(E_NOTIMPL));
	}
	CheckHr(podc->SetParameter(1,
		DBPARAMFLAGS_ISINPUT,
		NULL,
		DBTYPE_BYTES,
		reinterpret_cast<ULONG *>(prgbFmt),
		cbFmtSpaceTaken));
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetUnknown}, ${VwCacheDa#SetUnknown}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::SetUnknown(HVO hvo, PropTag tag, IUnknown * punk)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(punk);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvo != 0);
	//  Update the "Unknown" object in the database.

	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFldName;
	StrUni stuSql;

	//  Get the field$ info from the tag.
	CheckHr(m_qmdc->GetFieldName(tag, &sbstrFldName));
	CheckHr(m_qmdc->GetOwnClsName(tag, &sbstrClsName));

	CheckHr(m_qode->CreateCommand(&qodc));

	if (punk)
	{
		SetParameterForUnknown(qodc, punk);
		stuSql.Format(L"update [%s] set [%s]=? where id=%d", sbstrClsName.Chars(),
			sbstrFldName.Chars(), hvo);
	}
	else
	{
		//  Null object: set prop to null
		stuSql.Format(L"update [%s] set [%s]=null where id=%d", sbstrClsName.Chars(),
			sbstrFldName.Chars(), hvo);
	}

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	ISqlUndoActionPtr qsqlua;
	if (m_qacth)
	{
		// Create the SqlUndoAction and set the RedoCommand, UndoCommand, and verification
		// commands.
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.
		StrUni stuVerifyUndo;
		if (punk)
		{
			stuVerifyUndo.Format(L"select COUNT(id) from [%s] where id=%d and [%s]=?",
				sbstrClsName.Chars(), hvo, sbstrFldName.Chars());
		}
		else
		{
			stuVerifyUndo.Format(L"select COUNT(id) from [%s] where id=%d and [%s] is null",
				sbstrClsName.Chars(), hvo, sbstrFldName.Chars());
		}
		IUnknownPtr qunkOld;
		IUnknownPtr qunkTtp;
		CheckHr(get_UnknownProp(hvo, tag, &qunkTtp));
		if (qunkTtp)
			CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **)&qunkOld));
		IOleDbCommandPtr qodcUndo;
		CheckHr(m_qode->CreateCommand(&qodcUndo));
		StrUni stuSqlUndo;
		StrUni stuVerifyRedo;
		if (qunkOld)
		{
			SetParameterForUnknown(qodcUndo, qunkOld);
			stuSqlUndo.Format(L"update [%s] set [%s]=? where id=%d", sbstrClsName.Chars(),
				sbstrFldName.Chars(), hvo);
			stuVerifyRedo.Format(L"select COUNT(id) from [%s] where id=%d and [%s]=?",
				sbstrClsName.Chars(), hvo, sbstrFldName.Chars());
		}
		else
		{
			//  Null object: set prop to null
			stuSqlUndo.Format(L"update [%s] set [%s]=null where id=%d", sbstrClsName.Chars(),
				sbstrFldName.Chars(), hvo);
			stuVerifyRedo.Format(L"select COUNT(id) from [%s] where id=%d and [%s] is null",
				sbstrClsName.Chars(), hvo, sbstrFldName.Chars());
		}
		CheckHr(qsqlua->AddRedoCommand(m_qode, qodc, stuSql.Bstr()));
		CheckHr(qsqlua->AddUndoCommand(m_qode, qodcUndo, stuSqlUndo.Bstr()));
		CheckHr(qsqlua->VerifyRedoable(m_qode, qodcUndo, stuVerifyRedo.Bstr()));
		CheckHr(qsqlua->VerifyUndoable(m_qode, qodc, stuVerifyUndo.Bstr()));

		// Set the "reload" SQL command.
		StrUni stuReload;
		stuReload.Format(L"select [id], [%s] from [%s] where [id] = %d",
			sbstrFldName.Chars(), sbstrClsName.Chars(), hvo);
		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctTtp, 1, tag, 0));
		// Note that for all "Set" methods, the undo and redo reload statement is the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvo, 0, NULL));
	}

	// Actually execute the command
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));

	//  Update the "Unknown" object in the cache.
	CheckHr(SuperClass::SetUnknown(hvo, tag, punk));

	// The database trigger updated the timestamp on the owner, so we need to keep the
	// cached copy current. If we are changing the style of an StPara, the database also
	// updates the StText and its owner, so we need to update these timestamps as well.
	if (tag == kflidStPara_StyleRules)
	{
		HVO hvoParent;
		CheckHr(get_ObjOwner(hvo, &hvoParent));
		if(hvoParent)
		{
			CheckHr(CacheCurrTimeStamp(hvoParent));

			HVO hvoGrandParent;
			CheckHr(get_ObjOwner(hvoParent, &hvoGrandParent));
			if(hvoGrandParent)
			{
				CheckHr(CacheCurrTimeStamp(hvoGrandParent));
			}
		}
	}
	else
	{
		CheckHr(CacheCurrTimeStamp(hvo));
	}

	if (m_qacth)
	{
		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

//:>****************************************************************************************
//:>	Methods to set and retrieve extra info for collection/sequence references.
//:>****************************************************************************************


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#InsertRelExtra}
	ENHANCE JohnT: Change other methods that deal with Reference data to work correctly if
	extra information is in use.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::InsertRelExtra(HVO hvoSrc, PropTag tag, int ihvo, HVO hvoDst,
	BSTR bstrExtra)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrExtra);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvoSrc != 0);

	// Verify that ihvo is in range
	ObjPropRec oprKey(hvoSrc, tag);
	SeqExtra sx;
	// If it is not there already the only valid insertion is 0,0
	if (!m_hmoprsx.Retrieve(oprKey, &sx))
		sx.m_cstu = 0;
	if ((uint)ihvo > (uint)sx.m_cstu)	// one over is okay (insert at end)
		ThrowHr(WarnHr(E_INVALIDARG)); // Index out of range.

	//  Save the changes to the database.
	int cpt;
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFieldName;
	StrUni stuSql;

	//  Obtain information from the flid and take appropriate action.
	m_qmdc->GetOwnClsName(tag, &sbstrClsName);
	m_qmdc->GetFieldName(tag, &sbstrFieldName);
	m_qmdc->GetFieldType(tag, &cpt);
	CheckHr(m_qode->CreateCommand(&qodc));
	ISqlUndoActionPtr qsqlua;

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if ((m_qacth) &&
		((cpt == kcptReferenceSequence) || (cpt == kcptReferenceCollection)))
	{
		// Create the SqlUndoAction
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.

		// Set the SavePoint to which this action will rollback.
		// Todo JohnT(Undo): complete setting up new style undo action object
		//qsqlua->SetSavePoint(m_qode);

		// Reload the reference collection vector.
		StrUni stuReload;
		stuReload.Format(L"select [dst], [extra] from %s_%s where [src]=%d",
			sbstrClsName.Chars(), sbstrFieldName.Chars(), hvoSrc);

		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		CheckHr(qdcs->Push(koctObjVecExtra, 0, tag, 0));
		// Note:  The undo and redo reload statements are the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvoSrc, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvoSrc, 0, NULL));

		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}


	if (cpt == kcptReferenceSequence)
	{
		ComBool fIsNull;
		ComBool fMoreRows;
		GUID uid;
		CheckHr(CoCreateGuid(&uid));

		// Call the InsertRef_... stored procedure.
		// TODO 1724(PaulP): Check if the time stamp has changed for the sequence and update
		// it so that other users are blocked from making changes.
		// ENHANCE JohnT(?): If you need to store binary data (with nulls) in the Extra info,
		// you'll need to pass the last param with a ?.
		stuSql.Format(L"exec InsertRef_%s_%s_Extra$ %d, %d, %d, '%s'", sbstrClsName.Chars(),
			sbstrFieldName.Chars(), hvoSrc, ihvo, hvoDst, bstrExtra);
		CheckHr(m_qode->CreateCommand(&qodc)); // Get clean command to get rid of parameters.
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	}
	else if (cpt == kcptReferenceCollection)
	{
		Assert(false); // Collections with Extra info not yet implemented.
	}
	CheckHr(SuperClass::InsertRelExtra(hvoSrc, tag, ihvo, hvoDst, bstrExtra));

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#UpdateRelExtra}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::UpdateRelExtra(HVO hvoSrc, PropTag tag, int ihvo, BSTR bstrExtra)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrExtra);

	// Don't use 0 as an object handle! 0 is used in so many places to stand for not an
	// object at all.
	Assert(hvoSrc != 0);

	// Verify that ihvo is in range
	ObjPropRec oprKey(hvoSrc, tag);
	SeqExtra sx;
	// If it is not there already the only valid insertion is 0,0
	if (!m_hmoprsx.Retrieve(oprKey, &sx))
		sx.m_cstu = 0;
	if ((uint)ihvo > (uint)(sx.m_cstu - 1))
		ThrowHr(WarnHr(E_INVALIDARG)); // Index out of range.

	//  Save the changes to the database.
	int cpt;
	IOleDbCommandPtr qodc;
	SmartBstr sbstrClsName;
	SmartBstr sbstrFieldName;
	StrUni stuSql;

	//  Obtain information from the flid and take appropriate action.
	m_qmdc->GetOwnClsName(tag, &sbstrClsName);
	m_qmdc->GetFieldName(tag, &sbstrFieldName);
	m_qmdc->GetFieldType(tag, &cpt);
	CheckHr(m_qode->CreateCommand(&qodc));
	ISqlUndoActionPtr qsqlua;

	// Create a SqlUndoAction if this VwOleDbDa was given an ActionHandler.
	if ((m_qacth) &&
		((cpt == kcptReferenceSequence) || (cpt == kcptReferenceCollection)))
	{
		// Create the SqlUndoAction
		qsqlua.CreateInstance(CLSID_SqlUndoAction); // NOTE: This can throw an exception.

		// Todo JohnT(Undo): complete setting up new style undo action object
		//qsqlua->SetSavePoint(m_qode);

		// Reload the reference collection vector.
		StrUni stuReload;
		stuReload.Format(L"select [dst], [extra] from %s_%s where [src]=%d",
			sbstrClsName.Chars(), sbstrFieldName.Chars(), hvoSrc);

		IDbColSpecPtr qdcs;
		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
		// ENHANCE JohnT: If koctObjVecExtra is implemented, make sure this is correct usage.
		CheckHr(qdcs->Push(koctObjVec, 0, tag, 0));
		CheckHr(qdcs->Push(koctObjVecExtra, 0, tag, 0));
		// Note:  The undo and redo reload statements are the same.
		CheckHr(qsqlua->AddUndoReloadInfo(this, stuReload.Bstr(), qdcs, hvoSrc, 0, NULL));
		CheckHr(qsqlua->AddRedoReloadInfo(this, stuReload.Bstr(), qdcs, hvoSrc, 0, NULL));

		// Add the SqlUndoAction to the ActionHandler stack.
		IUndoActionPtr qua;
		CheckHr(qsqlua->QueryInterface(IID_IUndoAction, (void **)&qua));
		CheckHr(m_qacth->AddAction(qua));
	}


	if (cpt == kcptReferenceSequence)
	{
		ComBool fIsNull;
		ComBool fMoreRows;
		GUID uid;
		CheckHr(CoCreateGuid(&uid));

		// Call the UpdateRef_... stored procedure.
		// TODO 1724 (PaulP): Check if the time stamp has changed for the sequence and update
		// it so that other users are blocked from making changes.
		// ENHANCE JohnT(?): If you need to store binary data (with nulls) in the Extra info,
		// you'll need to pass the last param with a ?.
		stuSql.Format(L"exec UpdateRef_%s_%s_Extra$ %d, %d, '%s'", sbstrClsName.Chars(),
			sbstrFieldName.Chars(), hvoSrc, ihvo, bstrExtra);
		CheckHr(m_qode->CreateCommand(&qodc)); // Get clean command to get rid of parameters.
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	}
	else if (cpt == kcptReferenceCollection)
	{
		Assert(false); // Collections with Extra info not yet implemented.
	}
	CheckHr(SuperClass::UpdateRelExtra(hvoSrc, tag, ihvo, bstrExtra));

	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	This gets an object's owner from the cache. If is isn't in the cache, it automatically
	loads it into the cache. The automatic load also gets the clid and flid at the same time.
	NOTE: This can probably be eliminated when the cache handles lazy load.
	@param hvo The object for which we want the owner.
	@param phvo A pointer to receive the owner of hvo.
	@return S_OK for success. E_POINTER if phvoOwn is null, E_INVALIDARG if hvo is zero.
		E_FAIL if something else goes wrong.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_ObjOwner(HVO hvo, HVO * phvoOwn)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(phvoOwn);
	if (!hvo)
		ThrowHr(WarnHr(E_INVALIDARG));

	get_ObjectProp(hvo, kflidCmObject_Owner, phvoOwn);
	// TODO 1725 (KenZ): We have a major problem here. A phvoOwn value of 0 may mean that
	// the owner is not currently loaded, or it may mean the owner is actually 0. There
	// isn't any way to tell the difference without forcing a read of the database every
	// time which is terribly inefficient.
	if (!*phvoOwn)
	{
		// Owner not in cache, so go load it.
		CheckHr(LoadObjInfo(hvo));
		get_ObjectProp(hvo, kflidCmObject_Owner, phvoOwn);
	}

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}

/*----------------------------------------------------------------------------------------------
	${IVwOleDbDa#UpdatePropIfCached}; See if prop has been cached. If so, reload it and issue
	any necessary PropChanged.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::UpdatePropIfCached(HVO hvo, PropTag tag, int cpt, int ws)
{
	BEGIN_COM_METHOD;
	ObjPropRec oprKey(hvo, tag);
	switch (cpt & 0x1f) // exclude virtual bit.
	{
	case kcptBoolean:
	case kcptInteger:
	case kcptNumeric:
		// Note: only Integer are definitely stored here at present. The others
		// probably will be.
		{
			int n;
			if (m_hmoprn.Retrieve(oprKey, &n))
			{
				m_hmoprn.Delete(oprKey);
				// Will be reloaded if anyone still cares about the value.
				CheckHr(PropChanged(NULL, kpctNotifyAll, hvo, tag, 0, 0, 0));
			}
		}
		break;
	case kcptOwningAtom:
	case kcptReferenceAtom:
		{
			HVO hvo;
			if (m_hmoprobj.Retrieve(oprKey, &hvo))
			{
				m_hmoprobj.Delete(oprKey);
				// Here it is important to check the new value to see whether there is a real
				// change.
				HVO hvo2;
				CheckHr(get_ObjectProp(hvo, tag, &hvo2));
				if (hvo != hvo2)
				{
					PropChanged(NULL, kpctNotifyAll, hvo, tag, 0,
						(hvo2 == 0 ? 0 : 1), (hvo == 0 ? 0 : 1));
				}
			}
		}
		break;
	case kcptOwningCollection:
	case kcptReferenceCollection:
	case kcptOwningSequence:
	case kcptReferenceSequence:
		{
			ObjSeq os;
			if (m_hmoprsobj.Retrieve(oprKey, &os))
			{
				// normally would first delete os.m_prghvo, but we want it later.
				m_hmoprsobj.Delete(oprKey);
				int chvoNew;
				CheckHr(get_VecSize(hvo, tag, &chvoNew)); // reloads it
				ObjSeq os2;
				m_hmoprsobj.Retrieve(oprKey, &os2);
				// Figure what actually changed.
				Assert(os2.m_cobj == chvoNew);
				int chvoMin = min(chvoNew, os.m_cobj);
				int dcobj = chvoNew - os.m_cobj;
				int ihvoFirstDiff = 0;
				for ( ; ihvoFirstDiff < chvoMin &&
					 os.m_prghvo[ihvoFirstDiff] == os2.m_prghvo[ihvoFirstDiff]; ihvoFirstDiff++)
					;
				// This is an index such that objects at and after index ihvoLimDiff in the old
				// value match objects the corresponding distance from the end of the new value.
				// In effect, we conclude that we have deleted the objects from ihvoFirstDiff to
				// (but not including) ihvoLimDiff, and replaced them with that number of
				// objects plus (or minus) the difference in length.
				// REVIEW: This can overestimate ihvoLimDiff when the two vectors are unchanged.
				// There may be other inaccuracies as well.
				int ihvoLimDiff = os.m_cobj;
				for (; ihvoLimDiff > 0 && ihvoLimDiff + dcobj > 0 &&
						ihvoLimDiff <= os.m_cobj && ihvoLimDiff + dcobj <= os2.m_cobj &&
						os.m_prghvo[ihvoLimDiff - 1] == os2.m_prghvo[ihvoLimDiff + dcobj - 1];
					ihvoLimDiff++)
					;
				// done with this, and must delete to prevent memory leaks.
				delete[] os.m_prghvo;
				PropChanged(NULL, kpctNotifyAll, hvo, tag, ihvoFirstDiff,
					ihvoLimDiff - ihvoFirstDiff, ihvoLimDiff - ihvoFirstDiff + dcobj);
			}
		}
		break;
	case kcptBigString:
	case kcptString:
		{
			ITsStringPtr qtss;
			if (m_hmoprtss.Retrieve(oprKey, qtss))
			{
				m_hmoprtss.Delete(oprKey);
				// Will auto-load if anyone really cares.
				CheckHr(PropChanged(NULL, kpctNotifyAll, hvo, tag, 0, 0, 0));
			}
		}
		break;
	case kcptGuid:
		{
			GUID guid;
			if (m_hmoprguid.Retrieve(oprKey, &guid))
			{
				m_hmoprguid.Delete(oprKey);

				if (tag == kflidCmObject_Guid)
					m_hmoguidobj.Delete(guid);

				CheckHr(PropChanged(NULL, kpctNotifyAll, hvo, tag, 0, 0, 0));
			}
		}
		break;
	case kcptTime:
		{
			int64 lln;
			if (m_hmoprlln.Retrieve(oprKey, &lln))
			{
				m_hmoprlln.Delete(oprKey);
				CheckHr(PropChanged(NULL, kpctNotifyAll, hvo, tag, 0, 0, 0));
			}
		}
		break;
	case kcptBinary:
		{
			// JohnT: seems both of these may be used for binary props, under
			// differnt circs? Clear them both out if it is that sort of prop...
			IUnknownPtr qunk;
			if (m_hmoprunk.Retrieve(oprKey, qunk))
			{
				m_hmoprunk.Delete(oprKey);
				CheckHr(PropChanged(NULL, kpctNotifyAll, hvo, tag, 0, 0, 0));
			}

			StrAnsi sta;
			if (m_hmoprsta.Retrieve(oprKey, &sta))
			{
				m_hmoprsta.Delete(oprKey);
				CheckHr(PropChanged(NULL, kpctNotifyAll, hvo, tag, 0, 0, 0));
			}
		}
		break;
	case kcptBigUnicode:
	case kcptUnicode:
		{
			StrUni stu;
			if (m_hmoprstu.Retrieve(oprKey, &stu))
			{
				m_hmoprstu.Delete(oprKey);
				CheckHr(PropChanged(NULL, kpctNotifyAll, hvo, tag, 0, 0, 0));
			}
		}
		break;

	case kcptMultiString:
	case kcptMultiUnicode:
	case kcptMultiBigString:
	case kcptMultiBigUnicode:
		{
			ObjPropEncRec opreKey(hvo, tag, ws);
			ITsStringPtr qtss;
			if (m_hmopertss.Retrieve(opreKey, qtss))
			{
				m_hmopertss.Delete(opreKey);
				// Note that the ws is passed here as the 'index of what changed' to indicate
				// which ws changed.
				CheckHr(PropChanged(NULL, kpctNotifyAll, hvo, tag, ws, 0, 0));
			}
		}
		break;

	case kcptFloat:
	case kcptImage:
	case kcptGenDate:
		break; // nothing cached for these yet.
	default:
		Assert(false); // no others should occur.
	}
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}

/*----------------------------------------------------------------------------------------------
	This gets an object's class from the cache. If is isn't in the cache, it automatically
	loads it into the cache. The automatic load also gets the owner and flid at the same time.
	NOTE: This can probably be eliminated when the cache handles lazy load.
	@param hvo The object for which we want the class.
	@param pclid A pointer to receive the class of hvo.
	@return S_OK for success. E_POINTER if pclid is null, E_INVALIDARG if hvo is zero.
		E_FAIL if something else goes wrong.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_ObjClid(HVO hvo, int * pclid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pclid);
	if (!hvo)
		ThrowHr(WarnHr(E_INVALIDARG));

	get_IntProp(hvo, kflidCmObject_Class, pclid);
	if (!*pclid)
	{
		// Class not in cache, so go load it.
		CheckHr(LoadObjInfo(hvo));
		get_IntProp(hvo, kflidCmObject_Class, pclid);
	}

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}


/*----------------------------------------------------------------------------------------------
	This gets an object's owning flid from the cache. If is isn't in the cache, it automatically
	loads it into the cache. The automatic load also gets the clid and owner at the same time.
	NOTE: This can probably be eliminated when the cache handles lazy load.
	@param hvo The object for which we want the owning flid.
	@param pflidOwn A pointer to receive the flid in which hvo is owned.
	@return S_OK for success. E_POINTER if pflidOwn is null, E_INVALIDARG if hvo is zero.
		E_FAIL if something else goes wrong.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_ObjOwnFlid(HVO hvo, int * pflidOwn)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pflidOwn);
	if (!hvo)
		ThrowHr(WarnHr(E_INVALIDARG));

	get_IntProp(hvo, kflidCmObject_OwnFlid, pflidOwn);
	if (!*pflidOwn)
	{
		// Flid not in cache, so go load it.
		CheckHr(LoadObjInfo(hvo));
		get_IntProp(hvo, kflidCmObject_OwnFlid, pflidOwn);
	}

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}


/*----------------------------------------------------------------------------------------------
	Load the owner, classID, and owning flid for the given object.
	NOTE: This can probably be eliminated when the cache handles lazy load, although this
	does get several things in one query that wouldn't happen otherwise. So maybe we should
	keep it.
	@param hvo The object for which we want the owner, class, and owning flid.
	@return S_OK for success. E_INVALIDARG if hvo is zero. E_FAIL if something else goes wrong.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::LoadObjInfo(HVO hvo)
{
	BEGIN_COM_METHOD;

	if (!hvo)
		ThrowHr(WarnHr(E_INVALIDARG));
	// can't load anything from the database for dummies.
	if (IsDummyId(hvo))
		return S_OK;

	StrUni stuSql;
	IDbColSpecPtr qdcs;
	qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
	CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
	CheckHr(qdcs->Push(koctInt, 1, kflidCmObject_Class, 0));
	CheckHr(qdcs->Push(koctObj, 1, kflidCmObject_Owner, 0));
	CheckHr(qdcs->Push(koctInt, 1, kflidCmObject_OwnFlid, 0));
	// Execute the query and store results in the cache.
	switch(m_alpAutoloadPolicy)
	{
	case kalpLoadForThisObject: // original
		stuSql.Format(L"select id, Class$, Owner$, OwnFlid$ from CmObject "
			L"where id = %d", hvo);
		break;
	case kalpLoadAllOfClassForReadOnly: // Can't do anything usefully different.
	case kalpLoadForAllOfObjectClass: // load for objects of the exact same class.
		// Unfortunately, we have to do a query to find out the class.
		// If we end up suppressing the autoload, that may as well get us all we need.
		stuSql.Format(L"select id, Class$, Owner$, OwnFlid$ from CmObject "
			L"where id = %d", hvo);
		CheckHr(Load(stuSql.Bstr(), qdcs, hvo, 0, NULL, NULL));
		// MUST user superclass method; if the object has been deleted (we sometimes call this
		// as part of ClearInfoAbout) then we still won't have a class cached, and could
		// get a stack overflow.
		int clsid;
		CheckHr(SuperClass::get_IntProp(hvo, kflidCmObject_Class, &clsid));
		if (clsid == 0)
			return S_OK; // no point in trying to load info about other nonexistent objects!
		if (TestAndNoteRecentAutoloads(kflidCmObject_Class, 0, clsid))
			return S_OK;
		// arguably, should load for all subclasses as well, but at most that generates
		// one query per subclass.
		stuSql.Format(L"select cmo.id, cmo.Class$, cmo.Owner$, cmo.OwnFlid$ from CmObject cmo "
			L"where cmo.Class$ = %d ", clsid);
		break;
	case kalpLoadForAllOfBaseClass: // load for absolutely all objects (!)
		if (TestAndNoteRecentAutoloads(kflidCmObject_Class, 0, 0))
		{
			// Fall back to single-object query.
			stuSql.Format(L"select id, Class$, Owner$, OwnFlid$ from CmObject "
				L"where id = %d", hvo);
		}
		else
		{
			stuSql.Format(L"select id, Class$, Owner$, OwnFlid$ from CmObject ");
		}
		break;

	}
	CheckHr(Load(stuSql.Bstr(), qdcs, hvo, 0, NULL, false));

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
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

	Note that the property that owns this object is not modified.  References and backreferences
	that point at this object are removed only if cia = kciaRemoveAllObjectInfo.  Otherwise, only
	outward references from the object (and its children if cia = kciaRemoveObjectAndOwnedInfo)
	are cleared.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::ClearInfoAbout(HVO hvo, VwClearInfoAction cia)
{
	BEGIN_COM_METHOD;

	m_shvoDeleted.Clear();

	switch (cia)
	{
		case kciaRemoveObjectInfoOnly:
			ClearOwnedInfoAbout(hvo, false);
			break;
		case kciaRemoveObjectAndOwnedInfo:
			ClearOwnedInfoAbout(hvo, true);
			break;
		case kciaRemoveAllObjectInfo:
			ClearOwnedInfoAbout(hvo, true);
			// here we clear incoming references for each object stored in m_hvoDeleted.
			ClearIncomingReferences();
			break;
	}

	m_shvoDeleted.Clear();

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}

/*----------------------------------------------------------------------------------------------
	This routine similarly (but much more efficiently, especially when clearing incoming refs)
	removes information about a group of objects.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::ClearInfoAboutAll(HVO * prghvo, int chvo, VwClearInfoAction cia)
{
	BEGIN_COM_METHOD;

	m_shvoDeleted.Clear();

	switch (cia)
	{
		case kciaRemoveObjectInfoOnly:
			for (HVO * phvo = prghvo; phvo < prghvo + chvo; phvo++)
				ClearOwnedInfoAbout(*phvo, false);
			break;
		case kciaRemoveObjectAndOwnedInfo:
			for (HVO * phvo = prghvo; phvo < prghvo + chvo; phvo++)
				ClearOwnedInfoAbout(*phvo, true);
			break;
		case kciaRemoveAllObjectInfo:
			for (HVO * phvo = prghvo; phvo < prghvo + chvo; phvo++)
				ClearOwnedInfoAbout(*phvo, true);
			// here we clear incoming references for each object stored in m_hvoDeleted.
			ClearIncomingReferences();
			break;
	}

	m_shvoDeleted.Clear();

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}


void VwOleDbDa::ClearOwnedInfoAbout(HVO hvo, ComBool fIncludeOwnedObjects)
{
	m_shvoDeleted.Insert(hvo);		// keep track so we can check for incoming references to delete

	// If by any chance we don't have a metaclassinfo, we can't do anything.
	// See whether we know the class of the object. If not, we can't do much.
	int clid;
	CheckHr(get_IntProp(hvo, kflidCmObject_Class, &clid));
	if (!clid)
		return; // It's OK for this method not to do anything useful.

	// Delete the four special bits of info about the object.
	ObjPropRec oprKeySpecial(hvo, kflidCmObject_Class);
	m_hmoprn.Delete(oprKeySpecial);
	oprKeySpecial.m_tag = kflidCmObject_Owner;
	m_hmoprn.Delete(oprKeySpecial); // OK if not found.
	oprKeySpecial.m_tag = kflidCmObject_OwnFlid;
	m_hmoprn.Delete(oprKeySpecial);
	oprKeySpecial.m_tag = kflidCmObject_Guid;
	GUID guid;
	if (m_hmoprguid.Retrieve(oprKeySpecial, &guid))
	{
		m_hmoprguid.Delete(oprKeySpecial);
		m_hmoguidobj.Delete(guid);
	}

	// Remove cached time stamp
	m_hmostamp.Delete(hvo);

#define MAXFLID 10000
	ULONG rgflid[MAXFLID];
	int cflid;
	CheckHr(m_qmdc->GetFields(clid, true, kgrfcptAll, MAXFLID, rgflid, &cflid));
	HvoVec vhvoOwned;
	for (int iflid = 0; iflid < cflid; iflid++)
	{
		int flid = rgflid[iflid];
		ObjPropRec oprKey(hvo, flid);
		int cpt;
		CheckHr(m_qmdc->GetFieldType(flid, &cpt));
		switch (cpt & 0x1f) // exclude virtual bit.
		{
		case kcptBoolean:
		case kcptInteger:
		case kcptNumeric:
			// Note: only Integer are definitely stored here at present. The others
			// probably will be.
			m_hmoprn.Delete(oprKey);
			break;
		case kcptOwningAtom:
		case kcptReferenceAtom:
			{
				HVO hvo;
				if (m_hmoprobj.Retrieve(oprKey, &hvo))
				{
					m_hmoprobj.Delete(oprKey);
					if (hvo && fIncludeOwnedObjects && cpt == kcptOwningAtom)
						vhvoOwned.Push(hvo);
				}
			}
			break;
		case kcptOwningCollection:
		case kcptReferenceCollection:
		case kcptOwningSequence:
		case kcptReferenceSequence:
			{
				ObjSeq os;
				if (m_hmoprsobj.Retrieve(oprKey, &os))
				{
					if (fIncludeOwnedObjects &&
						(cpt == kcptOwningCollection || cpt == kcptOwningSequence))
					{
						for (int ihvo = 0; ihvo < os.m_cobj; ihvo++)
							vhvoOwned.Push(os.m_prghvo[ihvo]);
					}
					delete[] os.m_prghvo;
					m_hmoprsobj.Delete(oprKey);
				}
			}
			break;
		case kcptBigString:
		case kcptString:
			m_hmoprtss.Delete(oprKey);
			break;
		case kcptGuid:
			m_hmoprguid.Delete(oprKey);
			break;
		case kcptTime:
			m_hmoprlln.Delete(oprKey);
			break;
		case kcptBinary:
		case kcptImage: // stored same as binary for now.
			// JohnT: seems both of these may be used for binary props, under
			// differnt circs? Clear them both out if it is that sort of prop...
			m_hmoprunk.Delete(oprKey);
			m_hmoprsta.Delete(oprKey);
			break;
		case kcptBigUnicode:
		case kcptUnicode:
			m_hmoprstu.Delete(oprKey);
			break;

		case kcptMultiString:
		case kcptMultiUnicode:
			// ENHANCE JohnT: to really clear out, need to clear from m_hmopertss. But,
			// for that we need to know the relevant encodings.
			// One option: change so that the map uses the usual obj,tag key, leading to
			// an array of (tag, TSS) pairs, code wanting a single alternative does a
			// linear search since we seldom have multiple ones loaded. Then this code
			// can easily find and delete them all.

			// For now nothing we can do, just leave in cache.
			break;

		case kcptFloat:
		case kcptGenDate:
		case kcptMultiBigString:
		case kcptMultiBigUnicode:
			break; // nothing cached for these yet.
		default:
			Assert(false); // no others should occur.
		}
	}

	// now recursively delete props of owned objects. (We put nothing in the vector
	// if not doing this.) If we are doing this for any objects, we are going to keep
	// recursing; hence always pass true here.
	for (int ihvo = 0; ihvo < vhvoOwned.Size(); ihvo++)
	{
		ClearOwnedInfoAbout(vhvoOwned[ihvo], true);
	}
}

/*----------------------------------------------------------------------------------------------
	Remove from the cache all incoming references to objects in m_shvoDeleted.
	This complements the ClearInfoAbout() method.
----------------------------------------------------------------------------------------------*/
void VwOleDbDa::ClearIncomingReferences()
{
	// Remove references from the cache of atomic object properties.
	if (m_hmoprobj.Size() != 0)
	{
		Vector<ObjPropRec> voprDelAtomic;
		ObjPropObjMap::iterator it;

		for (it = m_hmoprobj.Begin(); it != m_hmoprobj.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();

			int cpt;
			CheckHr(m_qmdc->GetFieldType(oprKey.m_tag, &cpt));

			cpt &= 0x1f;

			if (cpt == kcptReferenceAtom && m_shvoDeleted.IsMember(it->GetValue()))
			{
				voprDelAtomic.Push(oprKey);
				//voprChg.Push(oprKey);
				//vihvoChg.Push(0);
			}
		}
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
		ObjPropSeqMap::iterator it;
		ObjSeq os;

		for (it = m_hmoprsobj.Begin(); it != m_hmoprsobj.End(); ++it)
		{
			ObjPropRec oprKey = it->GetKey();

			int cpt;
			CheckHr(m_qmdc->GetFieldType(oprKey.m_tag, &cpt));

			cpt &= 0x1f;

			if (cpt == kcptReferenceCollection || cpt == kcptReferenceSequence)
			{
				os = it->GetValue();
				int cobjSurviving = os.m_cobj;
				for (int ihvo = os.m_cobj; --ihvo >= 0; )
				{
					if (m_shvoDeleted.IsMember(os.m_prghvo[ihvo]))
					{
						// Remove the reference from the current sequence/collection.
						cobjSurviving--;
						MoveItems(os.m_prghvo + ihvo + 1, os.m_prghvo + ihvo, cobjSurviving - ihvo);
						// We must NOT use this, it deletes m_prghvo which we are iterating over.
						// Then we miss any others that need deleting.
						//CheckHr(ReplaceAux(oprKey.m_hvo, oprKey.m_tag, ihvo, ihvo+1, NULL, 0));
					}
				}
				if (cobjSurviving < os.m_cobj)
				{
					// some deletions. Replace the hashtable entry.
					ObjSeq osRep;
					osRep.m_cobj = cobjSurviving;
					osRep.m_prghvo = NewObj HVO[cobjSurviving];
					MoveItems(os.m_prghvo, osRep.m_prghvo, cobjSurviving);
					m_hmoprsobj.Insert(oprKey, osRep, true);
					delete[] os.m_prghvo; // free the old memory
				}
			}
		}
	}
}

//:>********************************************************************************************
//:>	Methods moved from VwOleDbDa, to do with higher-level database load functions.
//:>********************************************************************************************

typedef HashMap<ClsLevel, Vector<HVO> *> ClevVhvoMap; // Hungarian hmclv.

/*----------------------------------------------------------------------------------------------
	Qsort comparison function to order HVOs.
----------------------------------------------------------------------------------------------*/
int compareHvos(const void *arg1, const void *arg2)
{
	HVO hvo1 = * (HVO *) arg1;
	HVO hvo2 = * (HVO *) arg2;
	if (hvo1 < hvo2)
		return -1;
	if (hvo1 == hvo2)
		return 0;
	return 1;
}

/*----------------------------------------------------------------------------------------------
	Load the properties indicated by the data spec for the objects in the prghvo array,
	which are of the classes indicated by prgclsid.
	If fIncludeOwnedObjects is true, also loads appropriate information about all
	objects owned by objects in the start array.
	[Note: for structured text fields, the paragraphs owned by the StText are loaded,
	even if fIncludeOwnedObjects is false.]
	Report progress using padvi if it is not null.
	(This is a thin wrapper around the subsequent LoadData method.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::LoadData(HVO * prghvo, int * prgclsid, int chvo, IVwDataSpec *pdts,
	 IAdvInd * padvi, ComBool fIncludeOwnedObjects)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prghvo, chvo);
	ChkComArrayArg(prgclsid, chvo);
	ChkComArgPtr(pdts);
	ChkComArgPtrN(padvi);

	HvoClsidVec vhc;
	for (int i = 0; i < chvo; ++i)
	{
		HvoClsid hc;
		hc.clsid = prgclsid[i];
		hc.hvo = prghvo[i];
		vhc.Push(hc);
	}

	VwDataSpecPtr qds;
	CheckHr(pdts->QueryInterface(CLSID_VwDataSpec, (void **) &qds));
	qds->SetMetaNames(m_qmdc);
	LoadData(vhc, qds->ViewSpec(), padvi, fIncludeOwnedObjects);

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}

/*----------------------------------------------------------------------------------------------
	Load the data into the data access object.
	1. When loading all owned objects, it also loads the owner and clsid for each one (other
		than structured text classes).
	2. Constructs a list in a hashmap for each combination of clsid and nLevel from both the
		root items as well as all owned objects (if fRecurse is true).
	3. For each list, execute a query to load relevant information into the data cache.
	4. Load reference vectors.
	5. Load structured text data for all owned objects (if fRecurse is true), or just for the
		current root objects (if fRecurse is false).
	@param vhcRootItems The IDs of the items in the main root property, or a subset of them.
	@param puvs Pointer to the UserViewSpec giving details on what should be loaded.
	@param padvi Optional pointer to an status bar for progress indicator. can be NULL.
	@param fRecurse If true, it loads all recursively owned objects for the root objects.
----------------------------------------------------------------------------------------------*/
void VwOleDbDa::LoadData(HvoClsidVec & vhcRootItems, UserViewSpec * puvs, IAdvInd * padvi,
	bool fRecurse)
{
	AssertPtr(puvs);
	AssertPtrN(padvi);
	Assert(m_qode);

	int ibsp;
	//int ihvo;  // used for multiple loops
	IOleDbCommandPtr qodc;
	IFwMetaDataCachePtr qmdc;
	SmartBstr sbstrClassName;
	StrUni stuDatabase;
	StrUni stuItems;
	StrUni stuQ;
	StrUni stuQ1;
	StrUni stuQ3;
	StrUni stuQ4From;
	StrUni stuQ4Select;
	StrUni stuQ5;
	StrUni stuQMla; // Used for MultiStrings.
	HvoVec vhvoTxtParas;
	IDbColSpecPtr qdcs;
	ClevVhvoMap hmclv; // Hashmap of lists to process based on clid and level.
	ClevVhvoMap::iterator ithmclvLim;
	ClevVhvoMap::iterator ithmclv;
	Vector<HVO> * pvhvo;
	int clid;
	int chvo;
	StrUni stuVernWss;
	StrUni stuAnalWss;
	StrUni stuVernWs;
	StrUni stuAnalWs;
	StrUni stuAnalVernWss;
	StrUni stuVernAnalWss;
	int cws;
	int iws;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;

	// Add an item to the hashmap for each RecordSpec in the UserViewSpec.
	ClevRspMap::iterator ithmclevrspLim = puvs->m_hmclevrsp.End();
	for (ClevRspMap::iterator it = puvs->m_hmclevrsp.Begin(); it != ithmclevrspLim; ++it)
	{
		ClsLevel clev = it.GetKey();
		pvhvo = NewObj Vector<HVO>;
		hmclv.Insert(clev, pvhvo, true);
	}

	try
	{
		// Make an ASCII list of all of the vernacular writing systems.
		stuVernWss.Format(L"%d",  VernWss()[0]);
		cws = VernWss().Size();
		if (cws)
			stuVernWs.Format(L"%d", VernWss()[0]);
		for (iws = 1; iws < cws; ++iws)
		{
			stuVernWss.FormatAppend(L",%d", VernWss()[iws]);
		}

		// Make an ASCII list of all of the analysis writing systems.
		stuAnalWss.Format(L"%d",  AnalWss()[0]);
		cws = AnalWss().Size();
		if (cws)
			stuAnalWs.Format(L"%d", AnalWss()[0]);
		for (iws = 1; iws < cws; ++iws)
		{
			stuAnalWss.FormatAppend(L",%d", AnalWss()[iws]);
		}

		// Make an ASCII list of all of the AnalysisVernacular writing systems.
		stuAnalVernWss.Format(L"%d",  AnalVernWss()[0]);
		cws = AnalVernWss().Size();
		for (iws = 1; iws < cws; ++iws)
		{
			stuAnalVernWss.FormatAppend(L",%d", AnalVernWss()[iws]);
		}

		// Make an ASCII list of all of the VernacularAnalysis writing systems.
		stuVernAnalWss.Format(L"%d",  VernAnalWss()[0]);
		cws = VernAnalWss().Size();
		for (iws = 1; iws < cws; ++iws)
		{
			stuVernAnalWss.FormatAppend(L",%d", VernAnalWss()[iws]);
		}

		//  Obtain a pointer to the FwMetaDataCache interface.
		qmdc = m_qmdc;

		int chvoItems = vhcRootItems.Size();
		if (!chvoItems)
			goto LExit; // Nothing to do, but clear out hmclv map before bailing out,
						// else there is a memory leak.

		// Split the root items into appropriate lists based on their clids.
		int ihc;
		for (ihc = 0; ihc < chvoItems; ++ihc)
		{
			clid = vhcRootItems[ihc].clsid;
			ClsLevel clev(clid, 0);
			if (hmclv.Retrieve(clev, &pvhvo))
				pvhvo->Push(vhcRootItems[ihc].hvo);
		}
		if (padvi)
			CheckHr(padvi->Step(0));

		qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.

		// Get a list of the unique roots. First assemble them...
		Vector<HVO> vhvoRoots;
		for (ihc = 0; ihc < chvoItems; ++ihc)
			vhvoRoots.Push(vhcRootItems[ihc].hvo);
		// Then sort them...
		qsort((void *)vhvoRoots.Begin(), (size_t)vhvoRoots.Size(), sizeof(HVO), compareHvos);
		// Then eliminate duplicates. (In some cases it would be a little more efficient to
		// delete runs of duplicates together, but I expect duplicates to be rare.)
		int chvoRoots = vhvoRoots.Size();
		for (int ihvo = 0; ihvo < chvoRoots - 1; )
		{
			if (vhvoRoots[ihvo] == vhvoRoots[ihvo + 1])
			{
				vhvoRoots.Delete(ihvo + 1);
				chvoRoots--;
			}
			else
			{
				ihvo++;
			}
		}
		// Get a list of all owned objects for each of the root items. They are sorted
		// so that owning sequences come out properly. We are currently taking this approach
		// under the assumption that it is faster than making multiple recursive queries of
		// owned objects based on kftSubItems, although this has not actually been tested.
		// In most cases, the user will want all owned objects anyway. However, there are
		// exceptions. Browse view will perhaps never want this. Also, the list chooser does
		// not want to recurse through all sub items when displaying a top item. However, even
		// when not recursing, we want to get all structured texts. For now we use a Boolean
		// to indicate whether to recurse or not. There may be cases where we want to recurse
		// on some properties but not others. The current approach would load extra items
		// unnecessarily. However, to implement this, we would need to scrap using
		// GetLinkedObjects and go to multiple recursive queries.
		if (fRecurse)
		{
			/* Load all owned objects.
			Results:
				obj         clsid       owner       flid        type
				----------- ----------- ----------- ----------- -----------
				1577        14          1576        4006001     23
				1581        4010        1576        4006002     25
				1582        4010        1576        4006002     25
				1583        14          1576        4006010     23
				1578        16          1577        14001       27
				1579        16          1577        14001       27
				1580        16          1577        14001       27
				1584        16          1583        14001       27
				1586        4006        1585        4004009     27
				1587        4006        1585        4004009     27
				1618        4006        1585        4004009     27
				1625        4006        1585        4004009     27
				1630        4005        1585        4004009     27
				1639        14          1585        4006001     23
				1641        4010        1585        4006002     25
				1642        4010        1585        4006002     25
				1643        4010        1585        4006002     25
				1644        4010        1585        4006002     25
				1645        4010        1585        4006002     25
				1646        4010        1585        4006002     25
				1588        4006        1587        4004009     27
				1593        4006        1587        4004009     27
				1598        4006        1587        4004009     27
				1602        4005        1587        4004009     27
				1611        14          1587        4006001     23
				1615        4010        1587        4006002     25
				1616        4010        1587        4006002     25
				1617        4010        1587        4006002     25
			*/
			// Put all of the items in ObjInfoTbl$.
			GUID guid;
			CheckHr(CoCreateGuid(&guid));
			const int chvoMax = 1975; // Limit on SetObjList$.
			int ihvo = 0;
			int chvoLeft = chvoRoots;
			while (chvoLeft)
			{
				int chvo = chvoLeft > chvoMax ? chvoMax : chvoLeft;
				CheckHr(m_qode->CreateCommand(&qodc));
				stuQ3 = L"SetObjList$ ?, ?, 0";
				CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
					reinterpret_cast<ULONG *>(vhvoRoots.Begin() + ihvo), chvo * isizeof(HVO)));
				CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_GUID,
					(ULONG *)&guid, isizeof(GUID)));
				// Execute the command to add the items to the list.
				CheckHr(qodc->ExecCommand(stuQ3.Bstr(), knSqlStmtNoResults));
				qodc.Clear();
				ihvo += chvo;
				chvoLeft -= chvo;
			}

			// Now load all owned objects into the cache, and clean up ObjInfoTbl$.
			stuQ3.Format(L"declare @uid uniqueidentifier, @retval int%n"
				L"set @uid = '%g'%n"
				L"exec @retval=GetLinkedObjects$ @uid, NULL, %d%n"
				L"if @retval = 0 begin%n"
				L"  select DISTINCT ObjId obj, UpdStmp, ObjClass clsid, RelObjId owner,%n"
				L"  RelObjField flid, RelType type, RelOrder%n"
				L"  from ObjInfoTbl$ oit%n"
				L"  join CmObject cmo on oit.ObjId = cmo.Id%n"
				L"  where uid = @uid and InheritDepth = 0 and RelType is not null%n"
				L"  order by owner, flid, RelOrder%n"
				L"end%n"
				L"exec CleanObjInfoTbl$ @uid ", &guid, kgrfcptOwning);
			CheckHr(m_qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuQ3.Bstr(), knSqlStmtStoredProcedure));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));

			HVO hvoLastOwner = 0;
			int flidLast = 0;
			byte rgbTimeStamp[8];
			// Get each owned object from the database and store it, along with its clsid
			// and owner in the cache, then store it in the appropriate hashmap list.
			while (fMoreRows)
			{
				HVO hvo;
				int clsid;
				HVO hvoOwner;
				int flid;
				int cpt;
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
					isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&rgbTimeStamp),
					isizeof(rgbTimeStamp), &cbSpaceTaken, &fIsNull, 0));
				// Cache the TimeStamp
				if ((cbSpaceTaken == 8) && !fIsNull)
				{
					StrAnsi sta(reinterpret_cast<const char *>(rgbTimeStamp), 8);
					m_hmostamp.Insert(hvo, sta, true);	// Allow overwrites.
				}
				CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&clsid),
					isizeof(int), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&hvoOwner),
					isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&flid),
					isizeof(int), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(&cpt),
					isizeof(int), &cbSpaceTaken, &fIsNull, 0));
				// Save the object, owner, and clsid.
				CacheIntProp(hvo, kflidCmObject_Class, clsid);
				CacheObjProp(hvo, kflidCmObject_Owner, hvoOwner);
				if (cpt == kcptOwningAtom)
					CacheObjProp(hvoOwner, flid, hvo);
				else
				{
					int ihvoMin;
					int ihvoLim;
					get_VecSize(hvoOwner, flid, &ihvoLim);
					// If we are starting a new property, clear the old items.
					ihvoMin = (hvoLastOwner == hvoOwner && flidLast == flid) ? ihvoLim : 0;
					CacheReplace(hvoOwner, flid, ihvoMin, ihvoLim, &hvo, 1);
				}
				hvoLastOwner = hvoOwner;
				flidLast = flid;

				// Store the object in the appropriate list.
				if (clsid != kclidStText && clsid != kclidStTxtPara)
				{
					// Look for level 1 first. If not found, then look for level 0.
					// If we ever go for more than 2 levels, this will need to change.
					ClsLevel clev(clsid, 1);
					if (hmclv.Retrieve(clev, &pvhvo))
						pvhvo->Push(hvo);
					else
					{
						clev.m_nLevel = 0;
						if (hmclv.Retrieve(clev, &pvhvo))
							pvhvo->Push(hvo);
					}
				}
				else if (clsid == kclidStTxtPara)
					vhvoTxtParas.Push(hvo); // Separate list for StText paragraphs.
				CheckHr(qodc->NextRow(&fMoreRows));
			}
			// TODO KenZ(JohnT) PaulP says this is needed to get rid of some extra results
			// that cause problems under certain circumstances. We need to figure out what
			// is happening here. Should we be using nocount to avoid the extra results?
			CheckHr(qodc->GetRowset(0));
			qodc.Clear();
		}
		else // !fRecurse
		{
			// Don't load all owned objects, but do load all structured texts.
			stuQ5.Format(L"select st.id txt, stp.id para, stp.StyleRules, stp.Contents,"
				L" stp.Contents_Fmt, st.UpdStmp, stp.UpdStmp from StText_ st"
				L" join StTxtPara_ stp on st.id = stp.Owner$"
				L" where st.Owner$ in (%d", vhvoRoots[0]);
			for (int ihvo = 1; ihvo < chvoRoots; ++ihvo)
			{
				stuQ5.FormatAppend(L",%d", vhvoRoots[ihvo]);
			}
			stuQ5.Append(L") order by st.Owner$, st.OwnFlid$, stp.OwnOrd$");
			qdcs->Clear();
			CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
			CheckHr(qdcs->Push(koctObjVec, 1, kflidStText_Paragraphs, 0));
			CheckHr(qdcs->Push(koctTtp, 2, kflidStPara_StyleRules, 0));
			CheckHr(qdcs->Push(koctString, 2, kflidStTxtPara_Contents, 0));
			CheckHr(qdcs->Push(koctFmt, 2, kflidStTxtPara_Contents, 0));
			CheckHr(qdcs->Push(koctTimeStamp, 1, 0, 0));
			CheckHr(qdcs->Push(koctTimeStamp, 2, 0, 0));

			/* Example query:
				select st.id txt, stp.id para, stp.StyleRules, stp.Contents,
					stp.Contents_Fmt from StText_ st
				join StTxtPara_ stp on st.id = stp.Owner$
				where st.Owner$ in (1576, 1585, 1586, 1587, 1618, 1625, 1630)
				order by st.Owner$, st.OwnFlid$, stp.OwnOrd$
			Example results:
				txt   para  StyleRules Contents              Contents_Fmt
				1530  1531  NULL       Went fishing with...  0x01000000000000000000000001000...
				1530  1532  NULL       When cleaning the...  0x01000000000000000000000001000...
				1530  1533  NULL       Tiga's family and...  0x01000000000000000000000001000...
			*/
			Load(stuQ5.Bstr(), qdcs, 0, 0, padvi, FALSE);
		}
		if (padvi)
			CheckHr(padvi->Step(0));

		// Next we need to go through each list of ids and execute a query that will load
		// the properties for each item specified in appropriate RecordSpecs.
		ithmclvLim = hmclv.End();
		for (ithmclv = hmclv.Begin(); ithmclv != ithmclvLim; ++ithmclv)
		{
			ClsLevel clev = ithmclv.GetKey();
			hmclv.Retrieve(clev, &pvhvo);
			// Get the next list.
			chvo = pvhvo->Size();
			if (!chvo)
				continue; // Nothing in this list to process.

			// Get the block specs for the type of records in this list.
			RecordSpecPtr qrsp;
			puvs->m_hmclevrsp.Retrieve(clev, qrsp);
			AssertPtr(qrsp); // If missing, we have a class or level without a RecordSpec.
			BlockVec & vbsp = qrsp->m_vqbsp;

			// Make an ASCII list of all of the items in the list.
			stuItems.Format(L"%d",  (*pvhvo)[0]);
			for (int ihvo = 1; ihvo < chvo; ++ihvo)
			{
				stuItems.FormatAppend(L",%d", (*pvhvo)[ihvo]);
			}

			qdcs->Clear(); // Clear out records from previous pass.

			//  We have loaded the root property and all sub-item properties into the cache,
			//  and we have a list of all the item ids in pvhvo. Now get the atomic properties
			//  of the main items.
			CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
			CheckHr(qdcs->Push(koctTimeStamp, 1, 0, 0));
			stuQMla.Clear(); // Nuke any previous queries.
			stuQ4Select = L"select itm.id, itm.updstmp"; // Start new query string.

			//  Note that we use the database view that contains all the fields for a given
			//  "class" and all its "superclasses".  (eg. "RnEvent_" which contains columns from
			//  RnEvent, RnGenericRec, CmObject).
			CheckHr(qmdc->GetClassName(clev.m_clsid, &sbstrClassName));
			stuQ4From.Format(L" from %s_ as itm ", sbstrClassName.Chars());
			for (ibsp = 0; ibsp < vbsp.Size(); ++ibsp)
			{
				BlockSpecPtr qbsp = vbsp[ibsp];
				// Don't bother loading invisible attributes.
				if (qbsp->m_eVisibility == kFTVisNever)
				{
					// We don't want to skip reading the date created since this
					// is used in the status bar. At some point it would be more appropriate
					// for the status bar to call a method to make sure what it needs is loaded
					// but for now we'll take this simpler approach.
					ULONG luFlid = qbsp->m_flid;
					if (luFlid != 0)
					{
						SmartBstr sbstr;
						CheckHr(qmdc->GetFieldName(qbsp->m_flid, &sbstr));
						if (!sbstr.Equals(L"DateCreated"))
							continue;
					}
				}

				switch(qbsp->m_ft)
				{
				case kftString:
				case kftTitleGroup:	// A special kind of string with a subfield. Load same data.
					// A column for the string and one for its format.
					// e.g, , itm.Title, itm.Title_Fmt
					stuQ4Select.FormatAppend(L",itm.%s, itm.%s_Fmt",
						qbsp->m_stuFldName.Chars(), qbsp->m_stuFldName.Chars());
					// And note the extra 2 columns -- a string prop, based
					// on column 1, whose tag is the tag noted in the bsp, then its format info
					CheckHr(qdcs->Push(koctString, 1, qbsp->m_flid, 0));
					CheckHr(qdcs->Push(koctFmt, 1, qbsp->m_flid, 0));
					break;
				case kftMta:
				case kftMsa:
					{
						// Add (or append) a new select statement for the second Rowset.
						StrUni stu;
						// Convert the writing system (with magic values) to a string of writing
						// system(s).
						switch (qbsp->m_ws)
						{
						case kwsAnals:
							stu = stuAnalWss;
							break;
						case kwsVerns:
							stu = stuVernWss;
							break;
						case kwsAnal:
							stu = stuAnalWs;
							break;
						case kwsVern:
							stu = stuVernWs;
							break;
						case kwsAnalVerns:
							stu = stuAnalVernWss;
							break;
						case kwsVernAnals:
							stu = stuVernAnalWss;
							break;

						default:
							{
								StrUni stuT;
								stuT.Format(L"%d", qbsp->m_ws);
								stu = stuT;
								break;
							}
						}
						if (stuQMla.Length())
							stuQMla.Append(L"union all ");
						stuQMla.FormatAppend(
							L"select itm.obj, itm.txt, %d, itm.ws, %s, cmo.UpdStmp%n"
							L"from %s_%s itm%n"
							L"join CmObject cmo on cmo.id=itm.obj%n"
							L"where itm.obj in (%s) and itm.ws in (%s) ",
							qbsp->m_flid, qbsp->m_ft == kftMta ? L"null" : L"itm.fmt",
							qbsp->m_stuClsName.Chars(), qbsp->m_stuFldName.Chars(),
							stuItems.Chars(), stu.Chars());
						break;
					}
				case kftObjRefAtomic:
				case kftRefAtomic:
				case kftRefCombo:
					// The name is not read here because we are using the PossList
					// cache to provide this name for possibility lists, and other
					// smarts are used to generate a name for other references.
					// A column for the id of the CmPossibility or object.
					// e.g, , itm.Location
					stuQ4Select.FormatAppend(L",itm.%s",
						qbsp->m_stuFldName.Chars());
					// And note the extra column--an object prop, based
					// on column 1, whose tag is the tag noted in the qfsp
					CheckHr(qdcs->Push(koctObj, 1, qbsp->m_flid, 0));
					break;
				case kftEnum:
				case kftInteger:
					// A column for the int.
					// e.g, , itm.Type
					stuQ4Select.FormatAppend(L",itm.%s",
						qbsp->m_stuFldName.Chars());
					// And note the extra column -- an int prop, based
					// on column 1, whose tag is the tag noted in the qfsp
					CheckHr(qdcs->Push(koctInt, 1, qbsp->m_flid, 0));
					break;
				case kftExpandable:	// Fall through.
				case kftSubItems:
					// Nothing needed here since we are currently loading all owned objects
					// automatically.
					break;
				case kftGroup:
				case kftGroupOnePerLine:
					// Typically there are sub-items to be loaded, but we now handle that for
					// all field types.
					break; // case kftGroup
				case kftStText:
					// A column for the id of the text
					// e.g, , itm2.Dst
					stuQ4Select.FormatAppend(L",itm%d.Dst ", ibsp);
					// Get the required item from the appropriate table, e.g.
					// left outer join RnEvent_Description as itm2 on itm2.Src = itm.id
					stuQ4From.FormatAppend(L"left outer join %s_%s as itm%d"
						L" on itm%d.Src = itm.id ",
						qbsp->m_stuClsName.Chars(), qbsp->m_stuFldName.Chars(), ibsp, ibsp);
					// And note the extra columns. The first is an object prop, based
					// on column 1, whose tag is the tag noted in the qfsp
					CheckHr(qdcs->Push(koctObj, 1, qbsp->m_flid, 0));
					break;
				case kftUnicode:
					// A column for the Unicode string itself.
					// e.g, , itm.Name (for an StStyle)
					stuQ4Select.FormatAppend(L",itm.%s",
						qbsp->m_stuFldName.Chars());
					// And note the extra column -- a Unicode prop, based
					// on column 1, whose tag is the tag noted in the qfsp
					CheckHr(qdcs->Push(koctUnicode, 1, qbsp->m_flid, 0));
					break;
				case kftTtp:
					// A column containing binary data to be interpreted as a Ttp.
					// e.g, , itm.Rules (for a StStyle)
					stuQ4Select.FormatAppend(L",itm.%s",
						qbsp->m_stuFldName.Chars());
					// And note the extra column -- a Unicode prop, based
					// on column 1, whose tag is the tag noted in the qfsp
					CheckHr(qdcs->Push(koctTtp, 1, qbsp->m_flid, 0));
					break;
				case kftDateRO:
					// A column for the int64.
					// e.g, , itm.DateModified
					stuQ4Select.FormatAppend(L",itm.%s",
						qbsp->m_stuFldName.Chars());
					// And note the extra column -- an int prop, based
					// on column 1, whose tag is the tag noted in the qfsp
					CheckHr(qdcs->Push(koctTime, 1, qbsp->m_flid, 0));
					break;
				case kftGenDate:
					// A column for the gendate (int).
					// e.g, , itm.DateOfEvent
					stuQ4Select.FormatAppend(L",itm.%s",
						qbsp->m_stuFldName.Chars());
					// And note the extra column -- an int prop, based
					// on column 1, whose tag is the tag noted in the qfsp
					CheckHr(qdcs->Push(koctInt, 1, qbsp->m_flid, 0));
					break;
				default:
					break;
				}
				// Also process any sub-items (these don't have to be in a group field type)
				for (int ifsp = 0; ifsp < qbsp->m_vqfsp.Size(); ++ifsp)
				{
					FldSpecPtr qfsp = qbsp->m_vqfsp[ifsp];
					switch(qfsp->m_ft)
					{
					case kftRefAtomic:
					case kftRefCombo:
						// Similar to same thing at Block level above.
						stuQ4Select.FormatAppend(L",itm.%s",
							qfsp->m_stuFldName.Chars());
						CheckHr(qdcs->Push(koctObj, 1, qfsp->m_flid, AnalWs()));
						break;
					case kftEnum:
						// Similar to same thing at Block level above.
						stuQ4Select.FormatAppend(L",itm.%s",
							qfsp->m_stuFldName.Chars());
						CheckHr(qdcs->Push(koctInt, 1, qfsp->m_flid, 0));
						break;
					case kftString:
						stuQ4Select.FormatAppend(L",itm.%s, itm.%s_Fmt",
							qfsp->m_stuFldName.Chars(), qfsp->m_stuFldName.Chars());
						CheckHr(qdcs->Push(koctString, 1, qfsp->m_flid, 0));
						CheckHr(qdcs->Push(koctFmt, 1, qfsp->m_flid, 0));
						break;
					case kftMta:
					case kftMsa:
						{
							// Add (or append) a new select statement for the second
							// Rowset.
							StrUni stu;
							// Convert the writing system (with magic values) to a string of
							// writing system(s).
							switch (qbsp->m_ws)
							{
							case kwsAnals:
								stu = stuAnalWss;
								break;
							case kwsVerns:
								stu = stuVernWss;
								break;
							case kwsAnal:
								stu = stuAnalWs;
								break;
							case kwsVern:
								stu = stuVernWs;
								break;
							case kwsAnalVerns:
								stu = stuAnalVernWss;
								break;
							case kwsVernAnals:
								stu = stuVernAnalWss;
								break;
							default:
								{
									StrUni stuT;
									stuT.Format(L"%d", qbsp->m_ws);
									stu = stuT;
									break;
								}
							}
							if (stuQMla.Length())
								stuQMla.Append(L"union all ");
							stuQMla.FormatAppend(L"select itm.obj, itm.txt, %d, itm.ws, "
								L"%s, cmo.UpdStmp from %s_%s itm "
								L"join CmObject cmo on cmo.id=itm.obj "
								L"where itm.obj in (%s) and itm.ws in (%s) ",
								qfsp->m_flid, qfsp->m_ft == kftMta ? L"null" : L"itm.fmt",
								qfsp->m_stuClsName.Chars(), qfsp->m_stuFldName.Chars(),
								stuItems.Chars(), stu.Chars());
							break;
						}
					case kftExpandable:	// Fall through.
					case kftSubItems:
						// Nothing needed here since we are currently loading all owned objects
						// automatically.
						break;
					case kftDateRO:
						stuQ4Select.FormatAppend(L",itm.%s",
							qfsp->m_stuFldName.Chars());
						CheckHr(qdcs->Push(koctTime, 1, qfsp->m_flid, 0));
						break;
					case kftGenDate:
						stuQ4Select.FormatAppend(L",itm.%s",
							qfsp->m_stuFldName.Chars());
						CheckHr(qdcs->Push(koctInt, 1, qfsp->m_flid, 0));
						break;
					case kftStText:
						// A column for the id of the text
						// e.g, , itm2.Dst
						stuQ4Select.FormatAppend(L",itm%d%d.Dst ", ibsp, ifsp);
						// Get the required item from the appropriate table, e.g.
						// left outer join RnEvent_Description as itm2 on itm2.Src = itm.id
						stuQ4From.FormatAppend(L"left outer join %s_%s as itm%d%d "
							L"on itm%d%d.Src = itm.id ",
							qfsp->m_stuClsName.Chars(), qfsp->m_stuFldName.Chars(), ibsp, ifsp,
							ibsp, ifsp);
						// And note the extra columns. The first is an object prop, based
						// on column 1, whose tag is the tag noted in the qfsp
						CheckHr(qdcs->Push(koctObj, 1, qfsp->m_flid, 0));
						break;
					default:
						break;
					}
				}
			}

			/* Example queries:

select itm.id,itm.DateOfEvent,itm.TimeOfEvent,itm.Title, itm.Title_Fmt,itm.Type,itm4.Dst ,
	itm.Confidence,itm11.Dst ,itm13.Dst ,itm14.Dst ,itm.DateCreated,itm.DateModified
from RnEvent_ as itm
left outer join RnEvent_Description as itm4 on itm4.Src = itm.id
left outer join RnGenericRec_ExternalMaterials as itm11 on itm11.Src = itm.id
left outer join RnGenericRec_VersionHistory as itm13 on itm13.Src = itm.id
left outer join RnEvent_PersonalNotes as itm14 on itm14.Src = itm.id
where itm.id in (1570);

select itm.id,itm.Name,itm.BasedOn,itm.Next,itm.Type,itm.Rules
from StStyle_ as itm
where itm.id in (1681,1682,1683,1684,1685,1686,1687,1688,1689,1690,1691)

select itm.id,itm.Title, itm.Title_Fmt,itm.Hypothesis, itm.Hypothesis_Fmt,itm2.Dst ,itm3.Dst ,
	itm4.Dst ,itm5.Dst ,itm.Confidence,itm.Status,itm11.Dst ,itm.DateCreated,itm.DateModified
from RnAnalysis_ as itm
left outer join RnAnalysis_ResearchPlan as itm2 on itm2.Src = itm.id
left outer join RnAnalysis_Discussion as itm3 on itm3.Src = itm.id
left outer join RnAnalysis_Conclusions as itm4 on itm4.Src = itm.id
left outer join RnAnalysis_FurtherQuestions as itm5 on itm5.Src = itm.id
left outer join RnGenericRec_VersionHistory as itm11 on itm11.Src = itm.id
where itm.id in (1641)
			*/

			if (padvi)
			{
				CheckHr(padvi->Step(0));
			}

			// Load the non-multilingual strings/texts, if there are any.
			int cCol;
			qdcs->Size(&cCol);
			if (cCol > 2) // The id and the timestamp.
			{
				// Finish the query off.
				stuQ4Select.Append(stuQ4From);
				// If we get too many columns, then we run out of memory trying to load
				// multiple records.  Split it into multiple calls in this case.
				if (cCol < 100)
				{
					stuQ4Select.FormatAppend(L"where itm.id in (%s)", stuItems.Chars());
					Load(stuQ4Select.Bstr(), qdcs, 0, 0, padvi, FALSE);
					if (padvi)
						CheckHr(padvi->Step(0));
				}
				else
				{
					StrUni stuCmd;
					for (int ihvo = 0; ihvo < chvo; ++ihvo)
					{
						stuCmd.Format(L"%s where itm.id = %d",
							stuQ4Select.Chars(), (*pvhvo)[ihvo]);
						Load(stuCmd.Bstr(), qdcs, 0, 0, padvi, FALSE);
						if (padvi)
							CheckHr(padvi->Step(0));
					}
				}
			}

			// Load the multilingual strings/texts, if there are any.
			/* Sample query in stuQMla:
				select itm.obj, itm.txt, itm.flid, itm.ws, itm.fmt
				from CmPossibility_Name as itm
				where itm.obj in (270) and itm.ws in (740664001,931905001)
				union all
				select itm.obj, itm.txt, itm.flid, itm.ws, itm.fmt
				from CmPossibility_Abbreviation as itm
				where itm.obj in (270) and itm.ws in (740664001,931905001)
				union all
				select itm.obj, itm.txt, itm.flid, itm.ws, itm.fmt
				from CmPossibility_Description as itm
				where itm.obj in (270) and itm.ws in (740664001,931905001)
			*/
			if (stuQMla.Length())
			{
				qdcs->Clear();
				CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
				CheckHr(qdcs->Push(koctMlaAlt, 1, 0, 0));
				CheckHr(qdcs->Push(koctFlid, 1, 0, 0));
				CheckHr(qdcs->Push(koctEnc, 1, 0, 0));
				CheckHr(qdcs->Push(koctFmt, 1, 0, 0));
				// OPTIMIZE KenZ: We may be able to eliminate reading this timestamp.
				// In our MLA query above we joined to CmObject to get UpdStmp. And here
				// we add a line to load it into the cache. However, as long as there
				// are other properties being read, the timestamp will already be loaded
				// by stuQ4Select, so it is not needed here. However, pulling this out
				// when not needed gets a little tricky since we already have the query
				// strings built in stuQMla.
				CheckHr(qdcs->Push(koctTimeStamp, 1, 0, 0));
				Load(stuQMla.Bstr(), qdcs, 0, 0, padvi, FALSE);
				if (padvi)
					CheckHr(padvi->Step(0));
			}

			// Load sequence refs.
			for (ibsp = 0; ibsp < vbsp.Size(); ++ibsp)
			{
				BlockSpecPtr qbsp = vbsp[ibsp];
				switch(qbsp->m_ft)
				{
				case kftGroup:
				case kftGroupOnePerLine:
					{ // BLOCK, to prevent spurious warnings
						for (int ifsp = 0; ifsp < qbsp->m_vqfsp.Size(); ++ifsp)
						{
							FldSpecPtr qfsp = qbsp->m_vqfsp[ifsp];
							switch(qfsp->m_ft)
							{
							case kftRefSeq:
								LoadRefSeq(qfsp, stuItems);
								break;
							case kftBackRefAtomic:
								LoadAtomicBackRefSeq(qfsp, stuItems);
							default:
								// The others are currently handled in the atomic case above.
								break;
							}
						}
					}
					break;
				case kftObjRefSeq:
				case kftRefSeq:
					LoadRefSeq(qbsp, stuItems);
					break;
				case kftBackRefAtomic:
					// Although the forward ref is atomic (which has implications for
					// the query we generate, quite different from a forward ref that is
					// sequence, the back ref is a collection.
					LoadAtomicBackRefSeq(qbsp, stuItems);
					break;
				default:
					break;
				}
			}
			if (padvi)
				CheckHr(padvi->Step(0));
		}

		// Get the info about the paragraphs of all structured texts.
		// Note when we are not recursing, the texts are loaded above, so this isn't used.
		if (vhvoTxtParas.Size() > 0)
		{
			stuQ5.Format(
				L"select stp.Id, sp.StyleRules, stp.Contents, stp.Contents_Fmt, "
				L"cmo.UpdStmp "
				L"from StTxtPara as stp "
				L"join CmObject cmo on stp.id=cmo.id "
				L"join StPara sp on sp.id = stp.id "
				L"where stp.id in (%d ",vhvoTxtParas[0]);
			for (int ihvo = 1; ihvo < vhvoTxtParas.Size(); ++ihvo)
			{
				stuQ5.FormatAppend(L",%d", vhvoTxtParas[ihvo]);
			}
			stuQ5.Append(L")");
			qdcs->Clear();
			CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
			CheckHr(qdcs->Push(koctTtp, 1, kflidStPara_StyleRules, 0));
			CheckHr(qdcs->Push(koctString, 1, kflidStTxtPara_Contents, 0));
			CheckHr(qdcs->Push(koctFmt, 1, kflidStTxtPara_Contents, 0));
			CheckHr(qdcs->Push(koctTimeStamp, 1, 0, 0));

			/* Example query:
				select stp.Id, sp.StyleRules, stp.Contents, stp.Contents_Fmt, cmo.UpdStmp
				from StTxtPara as stp
				join StPara sp on sp.id = stp.id
				where stp.id in (1612,1613,1614)
			Example results:
				Id    StyleRules Contents              Contents_Fmt
				1612  NULL       Went fishing with...  0x010000000000000000000000010006C1A2252C
				1613  NULL       When cleaning the...  0x010000000000000000000000010006C1A2252C
				1614  NULL       Tiga's family and...  0x010000000000000000000000010006C1A2252C
			*/
			Load(stuQ5.Bstr(), qdcs, 0, 0, padvi, FALSE);
			if (padvi)
				CheckHr(padvi->Step(0));
		}
	}
	catch (...)
	{
		throw;	// For now we have nothing to add, so pass it on up.
	}

LExit:
	ithmclvLim = hmclv.End();
	for (ithmclv = hmclv.Begin(); ithmclv != ithmclvLim; ++ithmclv)
	{
		ClsLevel clev = ithmclv.GetKey();
		hmclv.Retrieve(clev, &pvhvo);
		delete pvhvo;
	}
}


/*----------------------------------------------------------------------------------------------
	Load a sequence reference property into the data cache from the database.
	This can be possibility list references or other object references.
	We use the PossList cache for names, so they aren't loaded here.
	For other objects, we have other ways of generating the names, which normally means
	loading additional information.
----------------------------------------------------------------------------------------------*/
void VwOleDbDa::LoadRefSeq(FldSpec * pfsp, StrUni stuItems)
{
	IDbColSpecPtr qdcs;
	qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
	StrUni stuQ6;

	/*
		select rnetgs.Src, rnetgs.Dst from RnGenericRec_Tags as rnetgs
		where rnetgs.Src in (1579)
	*/
	stuQ6.Format(
		L"select "
			L"rnetgs.Src, "
			L"rnetgs.Dst "
		L"from "
			L"%s_%s as rnetgs "
		L"where "
			L"rnetgs.Src in (%s) ",
	pfsp->m_stuClsName.Chars(), pfsp->m_stuFldName.Chars(), stuItems.Chars());
	qdcs->Clear();
	CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
	CheckHr(qdcs->Push(koctObjVec, 1, pfsp->m_flid, 0));
	Load(stuQ6.Bstr(), qdcs, 0, 0, NULL, FALSE);
}

/*----------------------------------------------------------------------------------------------
	Load a back reference reference property into the data cache from the database,
	where the forward reference is atomic.
	Todo JohnT: we need to find some way to allow the ordering of the back refs
	to be specified.
----------------------------------------------------------------------------------------------*/
void VwOleDbDa::LoadAtomicBackRefSeq(FldSpec * pfsp, StrUni stuItems)
{
	IDbColSpecPtr qdcs;
	StrUni stuQ6;

	/*
		select Gloss, id from TxtWordformInContext as tbl
			where tbl.Gloss in (5024, 5031) order by Gloss.
		Note that we use the inverse direction of the table, so the Gloss field functions as
		the source for creating the property in the table.
	*/
	stuQ6.Format(
		L"select %s, id from %s as tbl where tbl.%s in (%s) order by %s",
		pfsp->m_stuFldName.Chars(),
		pfsp->m_stuClsName.Chars(), pfsp->m_stuFldName.Chars(), stuItems.Chars(),
		pfsp->m_stuFldName.Chars());
	qdcs.CreateInstance(CLSID_DbColSpec); // NOTE: This can throw an exception.
	qdcs->Clear();
	CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
	CheckHr(qdcs->Push(koctObjVec, 1, pfsp->m_flid, 0));
	Load(stuQ6.Bstr(), qdcs, 0, 0, NULL, FALSE);
}


/***********************************************************************************************
	FldSpec methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Make a new copy of self.

	@return false if it failed.
----------------------------------------------------------------------------------------------*/
bool FldSpec::NewCopy(FldSpec ** ppfsp)
{
	FldSpecPtr qfsp;
	qfsp.Attach(NewObj FldSpec(m_qtssLabel, m_qtssHelp,	m_flid, m_ft, m_eVisibility,
		m_fRequired, m_stuSty.Chars(), m_ws, m_fCustFld, m_hvoPssl));
	if (!qfsp)
		return false;
	qfsp->m_pnt = m_pnt;
	qfsp->m_fHier = m_fHier;
	qfsp->m_fVert = m_fVert;
	qfsp->m_fExpand = m_fExpand;
	qfsp->m_ons = m_ons;
	qfsp->m_hvo = m_hvo;
	qfsp->m_stuFldName = m_stuFldName.Chars();
	qfsp->m_stuClsName = m_stuClsName.Chars();
	qfsp->m_fHideLabel = m_fHideLabel;
	*ppfsp = qfsp.Detach();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Initialize a FldSpec.
----------------------------------------------------------------------------------------------*/
void FldSpec::Init(bool fIsDocView, int stidLabel, int stidHelp, FldType ft, FldVis vis,
	FldReq req, LPCOLESTR pszSty, int flid, int ws, bool fCustFld, bool fHideLabel,
	ILgWritingSystemFactory * pwsf)
{
	AssertPtr(pwsf);

	// Used for getting writing system codes and such.
	m_qwsf = pwsf;

	if (stidLabel) // may be 0 when used in DataSpec.
		GetResourceTss(pwsf, stidLabel, &m_qtssLabel);
	if (stidHelp && !fIsDocView)	// No help for document view, even if we have stidHelp.
		GetResourceTss(pwsf, stidHelp, &m_qtssHelp);
	m_ft = ft;
	m_eVisibility = vis;
	m_fRequired = req;
	m_stuSty = pszSty;
	m_flid = flid;
	m_ws = ws;
	m_fCustFld = fCustFld;

	// Possibility list data.
	m_hvoPssl = 0;
	m_pnt = kpntName;
	m_fHier = false;
	m_fVert = false;

	// Hierarchical fields (e.g., subrecords).
	m_fExpand = false;
	m_ons = konsNone;

	// Used for Document view.
	m_fHideLabel = fHideLabel;

	// Used for Browse view.
	m_dxpColumn = kdxpDefBrowseColumn;
}

/*----------------------------------------------------------------------------------------------
	Initialize possibility list data for a FldSpec.
----------------------------------------------------------------------------------------------*/
void FldSpec::InitPssl(HVO hvoPssl, PossNameType pnt, bool fHier, bool fVert)
{
	Assert(hvoPssl);

	m_hvoPssl = hvoPssl;
	m_pnt = pnt;
	m_fHier = fHier;
	m_fVert = fVert;
}

/*----------------------------------------------------------------------------------------------
	Initialize hierarchical data for a FldSpec.
----------------------------------------------------------------------------------------------*/
void FldSpec::InitHier(OutlineNumSty ons, bool fExpand)
{
	m_ons = ons;
	m_fExpand = fExpand;
}


/***********************************************************************************************
	BlockSpec methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Make a new copy of self, complete with recursively new underlying objects.

	@return false if it failed.
----------------------------------------------------------------------------------------------*/
bool BlockSpec::NewCopy(BlockSpec ** ppbsp)
{
	BlockSpecPtr qbsp;
	qbsp.Attach(NewObj BlockSpec(m_qtssLabel, m_qtssHelp, m_flid, m_ft, m_eVisibility,
		m_fRequired, m_stuSty.Chars(), m_ws, m_fCustFld, m_hvoPssl));
	if (!qbsp)
		return false;
	qbsp->m_pnt = m_pnt;
	qbsp->m_fHier = m_fHier;
	qbsp->m_fVert = m_fVert;
	qbsp->m_fExpand = m_fExpand;
	qbsp->m_dxpColumn = m_dxpColumn;
	qbsp->m_ons = m_ons;
	qbsp->m_hvo = m_hvo;
	qbsp->m_fHideLabel = m_fHideLabel;
	qbsp->m_stuFldName = m_stuFldName.Chars();
	qbsp->m_stuClsName = m_stuClsName.Chars();
	// Fill field specs with newly created objects.
	for (int i = 0; i < m_vqfsp.Size(); ++i)
	{
		FldSpecPtr qfsp;
		// I'm not sure this will work to copy nested BlockSpec and FldSpec
		// objects. Our current sample does not have this, so it isn't being tested. NewCopy
		// isn't virtual since the arguments are different. I believe the model will be
		// simplified shortly so that there will only be one type, so we won't worry with this
		// problem yet. Hopefully it will go away.
		if (!m_vqfsp[i]->NewCopy(&qfsp))
			return false;
		qbsp->m_vqfsp.Push(qfsp);
	}
	*ppbsp = qbsp.Detach();
	return true;
}


/***********************************************************************************************
	RecordSpec methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Initialize a record spec.
----------------------------------------------------------------------------------------------*/
void RecordSpec::Init(UserViewSpec * puvs, int clid, int iLevel, UserViewType vwt,
	ILgWritingSystemFactory * pwsf)
{
	AssertPtr(puvs);
	Assert(vwt >= 0 && vwt < kvwtLim);
	AssertPtr(pwsf);

	m_vwt = vwt;
	// Used for getting writing system codes and such.
	m_qwsf = pwsf;

	int stid;
	switch (vwt)
	{
	default:
		Assert(false);
		return;
	case kvwtBrowse:	// Browse View
		stid = kstidBrowse;
		break;
	case kvwtDE:		// Data Entry View
		stid = kstidDataEntry;
		break;
	case kvwtDoc:		// Document View
		stid = kstidDocument;
		break;
	case kvwtConc:		// Concordance View
		stid = kstidConcordance;
		break;
	case kvwtDraft:		// Draft View
		stid = kstidDraft;
		break;
	}
	GetResourceTss(pwsf, stid, &puvs->m_qtssName);

	m_clsid = clid;
	m_nLevel = iLevel;

	ClsLevel clevKey;
	clevKey.m_clsid = clid;
	clevKey.m_nLevel = iLevel;
	puvs->m_hmclevrsp.Insert(clevKey, this, true);
}

/*----------------------------------------------------------------------------------------------
	Add a basic BlockSpec to the record spec.
----------------------------------------------------------------------------------------------*/
FldSpec * RecordSpec::AddField(bool fTopLevel, int stidLabel, int flid, FldType ft, int ws,
	int stidHelp, FldVis vis, FldReq req, LPCOLESTR pszSty, bool fCustFld, bool fHideLabel)
{
	FldSpecPtr qfsp;
	if (fTopLevel)
	{
		qfsp.Attach(new BlockSpec());
		m_vqbsp.Push(dynamic_cast<BlockSpec *>(qfsp.Ptr()));
	}
	else
	{
		qfsp.Attach(new FldSpec());
		(*m_vqbsp.Top())->m_vqfsp.Push(qfsp);
	}

	qfsp->Init((m_vwt == kvwtDoc), stidLabel, stidHelp, ft, vis, req, pszSty, flid, ws,
		fCustFld, fHideLabel, m_qwsf);

	return qfsp;
}

/*----------------------------------------------------------------------------------------------
	Add a possibility BlockSpec to the record spec.
----------------------------------------------------------------------------------------------*/
void RecordSpec::AddPossField(bool fTopLevel, int stidLabel, int flid, FldType ft, int stidHelp,
	HVO hvoPssl, PossNameType pnt, bool fHier, bool fVert, int ws, FldVis vis, FldReq req,
	LPCOLESTR pszSty, bool fCustFld, bool fHideLabel)
{
	FldSpecPtr qfsp = AddField(fTopLevel, stidLabel, flid, ft, ws, stidHelp, vis, req, pszSty,
		fCustFld, fHideLabel);
	qfsp->InitPssl(hvoPssl, pnt, fHier, fVert);
}

/*----------------------------------------------------------------------------------------------
	Add a hierarchical BlockSpec to the record spec.
----------------------------------------------------------------------------------------------*/
void RecordSpec::AddHierField(bool fTopLevel, int stidLabel, int flid, int ws, int stidHelp,
	OutlineNumSty ons, bool fExpand, FldVis vis, FldReq req, LPCOLESTR pszSty, bool fCustFld,
	bool fHideLabel)
{
	FldSpecPtr qfsp = AddField(fTopLevel, stidLabel, flid, kftSubItems, ws, stidHelp, vis, req,
		pszSty, fCustFld, fHideLabel);
	qfsp->InitHier(ons, fExpand);
}

/*----------------------------------------------------------------------------------------------
	Add a hierarchical BlockSpec to the record spec. This is for an owning collection property.
----------------------------------------------------------------------------------------------*/
void RecordSpec::AddCollectionField(bool fTopLevel, int stidLabel, int flid, int ws,
	int stidHelp, OutlineNumSty ons, bool fExpand, FldVis vis, FldReq req, LPCOLESTR pszSty,
	bool fCustFld, bool fHideLabel)
{
	FldSpecPtr qfsp = AddField(fTopLevel, stidLabel, flid, kftObjOwnCol, ws, stidHelp, vis, req,
		pszSty, fCustFld, fHideLabel);
	qfsp->InitHier(ons, fExpand);
}

/*----------------------------------------------------------------------------------------------
	Add a hierarchical BlockSpec to the record spec. This is for an owning sequence property.
----------------------------------------------------------------------------------------------*/
void RecordSpec::AddSequenceField(bool fTopLevel, int stidLabel, int flid, int ws, int stidHelp,
	OutlineNumSty ons, bool fExpand, FldVis vis, FldReq req, LPCOLESTR pszSty, bool fCustFld,
	bool fHideLabel)
{
	FldSpecPtr qfsp = AddField(fTopLevel, stidLabel, flid, kftObjOwnSeq, ws, stidHelp, vis, req,
		pszSty, fCustFld, fHideLabel);
	qfsp->InitHier(ons, fExpand);
}

/*----------------------------------------------------------------------------------------------
	Make a new copy of self, complete with recursively new underlying objects.
	Returns false if it failed.
----------------------------------------------------------------------------------------------*/
bool RecordSpec::NewCopy(RecordSpec ** pprsp)
{
	RecordSpecPtr qrsp;
	qrsp.Attach(NewObj RecordSpec(m_clsid, m_nLevel));
	if (!qrsp)
		return false;
	// Fill block specs with newly created objects.
	for (int i = 0; i < m_vqbsp.Size(); ++i)
	{
		BlockSpecPtr qbsp;
		if (!m_vqbsp[i]->NewCopy(&qbsp))
			return false;
		qrsp->m_vqbsp.Push(qbsp);
	}
	qrsp->m_hvo = m_hvo;
	qrsp->m_fNoSave = m_fNoSave;
	qrsp->m_vwt = m_vwt;
	// We don't want to copy m_fDirty.
	*pprsp = qrsp.Detach();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the field and class names from the Meta table.
----------------------------------------------------------------------------------------------*/
void RecordSpec::SetMetaNames(IFwMetaDataCache * pmdc)
{
	AssertPtr(pmdc);

	SmartBstr sbstrClassName;
	SmartBstr sbstrFieldName;
	int ibsp;
	ULONG lubspFlid;
	ULONG lufspFlid;

	//  Given the Flid contained with the given blockSpecs, set the FieldName and ClassName
	//  as well.
	for (ibsp = 0; ibsp < m_vqbsp.Size(); ++ibsp)
	{
		BlockSpecPtr qbsp = m_vqbsp[ibsp];
		lubspFlid = qbsp->m_flid;
		if (lubspFlid != 0)
		{
			CheckHr(pmdc->GetFieldName(lubspFlid, &sbstrFieldName));
			qbsp->m_stuFldName = sbstrFieldName.Chars();
			CheckHr(pmdc->GetOwnClsName(lubspFlid, &sbstrClassName));
			qbsp->m_stuClsName = sbstrClassName.Chars();
		}
		for (int ifsp = 0; ifsp < qbsp->m_vqfsp.Size(); ++ifsp)
		{
			FldSpecPtr qfsp = qbsp->m_vqfsp[ifsp];
			lufspFlid = qfsp->m_flid;
			if (lufspFlid != 0)
			{
				CheckHr(pmdc->GetFieldName(lufspFlid, &sbstrFieldName));
				qfsp->m_stuFldName = sbstrFieldName.Chars();
				CheckHr(pmdc->GetOwnClsName(lufspFlid, &sbstrClassName));
				qfsp->m_stuClsName = sbstrClassName.Chars();
			}
		}
	}
}


/***********************************************************************************************
	UserViewSpec methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
UserViewSpec::UserViewSpec(const UserViewSpec & uvs)
{
	m_vwt = uvs.m_vwt;
	m_nst = uvs.m_nst;
	m_qtssName = uvs.m_qtssName;
	m_nMaxLines = uvs.m_nMaxLines;
	m_fIgnorHier = uvs.m_fIgnorHier;
	m_hvo = uvs.m_hvo;
	m_fv = uvs.m_fv; // TlsOptDlg uses a copy that must have this set in its original state.
	m_ws = uvs.m_ws;
	memcpy(&m_guid, (void *)&uvs.m_guid, isizeof(GUID));
	ClevRspMap & hmclevrspOther = const_cast<ClevRspMap &>(uvs.m_hmclevrsp);
	hmclevrspOther.CopyTo(m_hmclevrsp);
}


/*----------------------------------------------------------------------------------------------
	Make a copy of an exisiting view spec with new objects, not just new pointers to existing
	objects. Return true if successful.
----------------------------------------------------------------------------------------------*/
bool UserViewSpec::NewCopy(UserViewSpec ** ppuvs)
{
	UserViewSpecPtr qvuvs;
	qvuvs.Attach(NewObj UserViewSpec);
	if (!qvuvs)
		return false;
	qvuvs->m_vwt = m_vwt;
	qvuvs->m_nst = m_nst;
	qvuvs->m_qtssName = m_qtssName;
	qvuvs->m_nMaxLines = m_nMaxLines;
	qvuvs->m_fIgnorHier = m_fIgnorHier;
	qvuvs->m_hvo = m_hvo; // We need this in TlsOptDlg copies to identify Items to delete.
	qvuvs->m_ws = m_ws;
	memcpy(&qvuvs->m_guid, &m_guid, isizeof(GUID));
	qvuvs->m_iwndClient = m_iwndClient;
	qvuvs->m_fv = m_fv;

	// Copy the map of RecordSpecs.
	ClevRspMap::iterator ithmclevrspLim = m_hmclevrsp.End();
	for (ClevRspMap::iterator it = m_hmclevrsp.Begin(); it != ithmclevrspLim; ++it)
	{
		ClsLevel clev = it.GetKey();
		RecordSpecPtr qrspSrc = it.GetValue();
		Assert(clev.m_clsid == qrspSrc->m_clsid);
		Assert(clev.m_nLevel == qrspSrc->m_nLevel);
		RecordSpecPtr qrsp;
		if (!qrspSrc->NewCopy(&qrsp))
			return false;
		qvuvs->m_hmclevrsp.Insert(clev, qrsp);
	}
	*ppuvs = qvuvs.Detach();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Save the UserViewSpec to the database. In the process, it adds the newly created hvos
	to each class. By using DbAccess calls directly and thus reducing the number of queries,
	this code executes 3-4 times as fast as using the various Set calls on VwOleDbDa.
----------------------------------------------------------------------------------------------*/
bool UserViewSpec::Save(IOleDbEncap * pode, bool fForceNewObj)
{
	AssertPtr(pode);
	ComBool fIsNull;
	HVO hvoUvw = 0;
	StrUni stu;
	ITsStringPtr qtss;
	ComBool fEqual;
	BYTE rgb[4]; // One int.
	(int &)rgb[0] = m_nMaxLines;
	// The int stores m_fIgnorHier as bit 31, and m_nMaxLines as the lower bits.
	if (m_fIgnorHier)
		(int &)rgb[0] = 0x80000000 | m_nMaxLines;
	else
		(int &)rgb[0] = m_nMaxLines;

	ClevRspMap::iterator ithmclevrspLim = m_hmclevrsp.End();
	ClevRspMap::iterator it;

	Assert(pode);
	IOleDbCommandPtr qodc;

	try
	{
		// It's essential that we not allow partial updates or we can damage the database to where
		// a user can't get started again.
		CheckHr(pode->BeginTrans());

		if (fForceNewObj || !m_hvo)
		{
			// Create a new UserView.
			CheckHr(pode->CreateCommand(&qodc));
			qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *) &hvoUvw,
				sizeof(HVO));
			stu.Format(L"exec CreateObject$ %d, ? output, null", kclidUserView);
			CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
			qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoUvw), sizeof(HVO), &fIsNull);
			if (fIsNull)
			{
				pode->RollbackTrans();
				return false;
			}
			Assert(hvoUvw);
			m_hvo = hvoUvw; // Save the hvo in the UserViewSpec.
		}

		// By using parameters, we are hoping SQL will cache the execution plans to increase speed.
		// Save the fields of the UserView.
		CheckHr(pode->CreateCommand(&qodc));
		stu = L"update [UserView] set [Type]=?, [App]=?, [System]=?, [Details]=?, [SubType]=? "
			L"where [id]=?";
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
			reinterpret_cast<ULONG *>(&m_vwt), sizeof(int)));
		CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_GUID,
			reinterpret_cast<ULONG *>(&m_guid), sizeof(GUID)));
		CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
			reinterpret_cast<ULONG *>(&m_fv), sizeof(int)));
		CheckHr(qodc->SetParameter(4, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(rgb), 4));
		CheckHr(qodc->SetParameter(5, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
			reinterpret_cast<ULONG *>(&m_nst), sizeof(int)));
		CheckHr(qodc->SetParameter(6, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
			reinterpret_cast<ULONG *>(&m_hvo), sizeof(HVO)));
		// update UserView
		// set type=0, app='33386581-4DD5-11D4-8078-0000C0FB81B5', system=1, details=0x03000000
		// where id = 4477
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtNoResults));

		// Save the UserView name.
		const OLECHAR * prgch;
		int cch;
		// First, make sure that the name string is normalized.  This may be paranoid overkill.
		CheckHr(m_qtssName->get_NormalizedForm(knmNFD, &qtss));
		CheckHr(m_qtssName->Equals(qtss, &fEqual));
		if (!fEqual)
			m_qtssName = qtss;
		CheckHr(pode->CreateCommand(&qodc));
		stu.Format(L"exec SetMultiTxt$ %d, %d, %d, ?", kflidUserView_Name, m_hvo, m_ws);
		m_qtssName->LockText(&prgch, &cch);
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)prgch, cch * sizeof(OLECHAR)));
		m_qtssName->UnlockText(prgch);
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));

		// Loop through the RecordSpecs and save each one.
		for (it = m_hmclevrsp.Begin(); it != ithmclevrspLim; ++it)
		{
			ClsLevel clev = it.GetKey();
			RecordSpecPtr qrsp = it.GetValue();
			AssertPtr(qrsp);
			if (qrsp->m_fNoSave || !qrsp->m_fDirty && !fForceNewObj)
				continue;
			HVO hvoUvr = qrsp->m_hvo;
			Assert(clev.m_clsid == qrsp->m_clsid);
			Assert(clev.m_nLevel == qrsp->m_nLevel);
			bool fNewObj = false;

			if (fForceNewObj || !qrsp->m_hvo)
			{
				// Create a new UserViewRec.
				fNewObj = true;
				CheckHr(pode->CreateCommand(&qodc));
				stu.Format(L"exec CreateOwnedObject$ %d, ? output, null, ?, %d, %d",
					kclidUserViewRec, kflidUserView_Records, kcptOwningCollection);
				qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *)&hvoUvr,
					sizeof(HVO));
				qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4, (ULONG *)&m_hvo,
					sizeof(HVO));
				CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
				qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoUvr), sizeof(HVO), &fIsNull);
				if (fIsNull)
				{
					pode->RollbackTrans();
					return false;
				}
				Assert(hvoUvr);
				qrsp->m_hvo = hvoUvr; // Save the hvo in the RecordSpec.
			}

			// Save the fields on the UserViewRec.
			CheckHr(pode->CreateCommand(&qodc));
			stu = L"update [UserViewRec] set [clsid]=?, [level]=? where [id]=?";
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
				reinterpret_cast<ULONG *>(&qrsp->m_clsid), sizeof(int)));
			CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
				reinterpret_cast<ULONG *>(&qrsp->m_nLevel), sizeof(int)));
			CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
				reinterpret_cast<ULONG *>(&hvoUvr), sizeof(HVO)));
			CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtNoResults));
			qodc.Clear();	// Prevent deadlock writing to database in qfsp->Save().

			// Loop through the BlockSpecs and save or update each one.
			int cbsp = qrsp->m_vqbsp.Size();
			int ibsp;
			for (ibsp = 0; ibsp < cbsp; ++ibsp)
			{
				BlockSpecPtr qbsp = qrsp->m_vqbsp[ibsp];
				AssertPtr(qbsp);
				FldSpecPtr qfsp = dynamic_cast<FldSpec *>(qbsp.Ptr());
				AssertPtr(qfsp);
				HVO hvoBsp = qfsp->Save(pode, hvoUvr, m_ws, 0, fNewObj);
				if (!hvoBsp)
				{
					pode->RollbackTrans();
					return false;
				}

				// Go through each FldSpec and save each one.
				int cfsp = qbsp->m_vqfsp.Size();
				for (int ifsp = 0; ifsp < cfsp; ++ifsp)
				{
					FldSpecPtr qfsp = qbsp->m_vqfsp[ifsp];
					AssertPtr(qfsp);
					// Save a pointer to the BlockSpec to indicate this is a subfield.
					HVO hvoFsp = qfsp->Save(pode, hvoUvr, m_ws, hvoBsp, fNewObj);
					if (!hvoFsp)
					{
						pode->RollbackTrans();
						return false;
					}
				}
			}

			// If we are updating an existing record, we need to handle these changes.
			if (!fNewObj)
			{
				// We now need to reorder the BlockSpecs and delete any extra ones.
				// We already added new ones above. So first, have SQL assign new ord
				// values to all BlockSpecs for this RecordSpec in the database starting
				// above the current maximum ord.
				CheckHr(pode->CreateCommand(&qodc));
				stu = L"declare @cnt int "
					L"select @cnt = max(ownord$) from cmobject where owner$=? "
					L"set @cnt = @cnt + 1 "
					L"update cmobject set ownord$=ownord$ + @cnt where owner$=?";
				CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
					reinterpret_cast<ULONG *>(&hvoUvr), sizeof(int)));
				CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
					reinterpret_cast<ULONG *>(&hvoUvr), sizeof(int)));
				CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtNoResults));

				// Now go through the current BlockSpecs and set their ord values in the new
				// order, starting at 0.
				int nOrd = 0;
				for (ibsp = 0; ibsp < cbsp; ++ibsp)
				{
					BlockSpecPtr qbsp = qrsp->m_vqbsp[ibsp];
					AssertPtr(qbsp);
					CheckHr(pode->CreateCommand(&qodc));
					stu = L"update cmobject set ownord$=? where id=?";
					CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
						reinterpret_cast<ULONG *>(&nOrd), sizeof(int)));
					CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
						reinterpret_cast<ULONG *>(&qbsp->m_hvo), sizeof(HVO)));
					CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtNoResults));
					++nOrd;
					// Go through each FldSpec and reset the order.
					int cfsp = qbsp->m_vqfsp.Size();
					for (int ifsp = 0; ifsp < cfsp; ++ifsp)
					{
						FldSpecPtr qfsp = qbsp->m_vqfsp[ifsp];
						AssertPtr(qfsp);
						CheckHr(pode->CreateCommand(&qodc));
						stu = L"update cmobject set ownord$=? where id=?";
						CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
							reinterpret_cast<ULONG *>(&nOrd), sizeof(int)));
						CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
							reinterpret_cast<ULONG *>(&qfsp->m_hvo), sizeof(HVO)));
						CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtNoResults));
						++nOrd;
					}
				}

				// Now delete any BlockSpecs that are above the current number of BlockSpecs.
				CheckHr(pode->CreateCommand(&qodc));
				//stu.Format(L"declare @id int "
				//	L"select top 1 @id = id from cmobject where owner$=? "
				//	L"and ownord$>=? "
				//	L"if @id is not null "
				//	L"exec DeleteOwnSeq$ ?, %d, 0, @id",
				//	kflidUserViewRec_Fields);
				//CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
				//	reinterpret_cast<ULONG *>(&hvoUvr), sizeof(HVO)));
				//CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
				//	reinterpret_cast<ULONG *>(&nOrd), sizeof(int)));
				//CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
				//	reinterpret_cast<ULONG *>(&hvoUvr), sizeof(HVO)));
				stu.Format(L"declare @min int,@max int,@idMin int,@idMax int%n"
					L"select @min=min(ownord$),@max=max(ownord$)%n"
					L"from cmobject where owner$=%<0>u and ownflid$=%<2>u and ownord$>=%<1>u%n"
					L"select @idMin=mn.id,@idMax=mx.id%n"
					L"from cmobject as mn%n"
					L"inner join cmobject as mx on mn.owner$=mx.owner$ and mn.ownflid$=mx.ownflid$%n"
					L"where mn.owner$=%<0>u and mn.ownflid$=%<2>u and mn.ownord$=@min and mx.ownord$=@max%n"
					L"if @idMin is not null%n"
					L"exec DeleteOwnSeq$ %<0>u,%<2>u,0,@idMin,@idMax",
					hvoUvr,nOrd,kflidUserViewRec_Fields);

				CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtNoResults));
			}
			qrsp->m_fDirty = false;
		}
		pode->CommitTrans();
	}
	catch(...)
	{
		pode->RollbackTrans();
		throw;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Save the UserViewField to the database.
----------------------------------------------------------------------------------------------*/
HVO FldSpec::Save(IOleDbEncap * pode, HVO hvoOwn, int ws, HVO hvoBsp, bool fForceNewObj)
{
	AssertPtr(pode);

	ComBool fIsNull;
	StrUni stu;
	IOleDbCommandPtr qodc;
	ITsStringPtr qtss;
	ComBool fEqual;

	if (fForceNewObj || !m_hvo)
	{
		// Create a new UserViewField.
		HVO hvoUvf;
		CheckHr(pode->CreateCommand(&qodc));
		stu.Format(L"exec CreateOwnedObject$ %d, ? output, null, ?, %d, %d, null",
			kclidUserViewField, kflidUserViewRec_Fields, kcptOwningSequence);
		qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *)&hvoUvf,
			sizeof(HVO));
		qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4, (ULONG *)&hvoOwn,
			sizeof(HVO));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
		qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoUvf), sizeof(HVO), &fIsNull);
		if (fIsNull)
			return false; // Something went wrong.
		Assert(hvoUvf);
		m_hvo = hvoUvf; // Save the hvo in the FldSpec.
	}

	// One or two ints are currently stored in the Details field (lsb first)
	BYTE rgb[8]; // Two ints.
	int cb = 8;

	// The first int stores m_fHideLabel as bit 31, and m_dxpColumn as the lower bits.
	Assert(m_dxpColumn >= 0);
	(int &)rgb[0] = m_fHideLabel << 31 | m_dxpColumn;

	switch (m_ft)
	{
	case kftRefAtomic:
	case kftRefCombo:
	case kftRefSeq:
		{
			// It is a Choices List field type.
			// The second int stores m_fVert as bit 31, m_fHier as bit 30, and m_pnt as the
			// low bits (29-0).
			int n = m_fVert << 31 | m_fHier << 30 | m_pnt;
			(int &)rgb[4] = n;
			break;
		}
	case kftExpandable:
		{
			// It is an Expandable List field type.
			// The second int stores m_fExpand as bit 31, m_fHier as bit 30, and m_pnt as the
			// low bits (29-0).
			int n = m_fExpand << 31 | m_fHier << 30 | m_pnt;
			(int &)rgb[4] = n;
			break;
		}
	case kftSubItems:
		{
			// It is a Hierarchical field type.
			// The second int stores m_fExpand as bit 31 and m_ons as low bits.
			int n = m_fExpand << 31 | m_ons;
			(int &)rgb[4] = n;
			break;
		}
	default:
		// Other types.
		// The second int is not stored.
		cb = 4;
		break;
	}

	// Save the fields for the UserViewField.
	CheckHr(pode->CreateCommand(&qodc));
	if ((unsigned)m_ws < (unsigned)kwsLim)
	{
		stu = L"UPDATE [UserViewField] "
			L"SET [type]=?, [flid]=?, [visibility]=?, [required]=?, "
			L"[style]=?, [WritingSystem]=?, [isCustomField]=?, [PossList]=?, [details]=?, "
			L"[SubfieldOf]=?, [WsSelector]=null"
			L" WHERE [id]=?";
	}
	else
	{
		stu = L"UPDATE [UserViewField] "
			L"SET [type]=?, [flid]=?, [visibility]=?, [required]=?, "
			L"[style]=?, [WsSelector]=?, [isCustomField]=?, [PossList]=?, [details]=?, "
			L"[SubfieldOf]=?, [WritingSystem]=null"
			L" WHERE [id]=?";
	}

	CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		reinterpret_cast<ULONG *>(&m_ft), sizeof(int)));
	CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		reinterpret_cast<ULONG *>(&m_flid), sizeof(int)));
	CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		reinterpret_cast<ULONG *>(&m_eVisibility), sizeof(int)));
	CheckHr(qodc->SetParameter(4, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		reinterpret_cast<ULONG *>(&m_fRequired), sizeof(int)));
	StrUtil::NormalizeStrUni(m_stuSty, UNORM_NFD);
	CheckHr(qodc->SetParameter(5, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
		(ULONG *)m_stuSty.Chars(), m_stuSty.Length() * sizeof(OLECHAR)));
	CheckHr(qodc->SetParameter(6, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		reinterpret_cast<ULONG *>(&m_ws), sizeof(int)));
	ComBool fT = m_fCustFld;
	CheckHr(qodc->SetParameter(7, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BOOL,
		reinterpret_cast<ULONG *>(&fT), sizeof(ComBool)));
	if (m_hvoPssl)
		CheckHr(qodc->SetParameter(8, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
			reinterpret_cast<ULONG *>(&m_hvoPssl), sizeof(HVO)));
	else
		CheckHr(qodc->SetParameter(8, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
			reinterpret_cast<ULONG *>(NULL), 0));
	CheckHr(qodc->SetParameter(9, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
		reinterpret_cast<ULONG *>(rgb), cb));
	if (hvoBsp)
		CheckHr(qodc->SetParameter(10, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
			reinterpret_cast<ULONG *>(&hvoBsp), sizeof(HVO)));
	else
		CheckHr(qodc->SetParameter(10, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
			reinterpret_cast<ULONG *>(NULL), 0));
	CheckHr(qodc->SetParameter(11, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		reinterpret_cast<ULONG *>(&m_hvo), sizeof(HVO)));
	CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtNoResults));

	// Save the label for the UserViewField.
	// First, make sure that the label string is normalized.  This may be paranoid overkill.
	CheckHr(m_qtssLabel->get_NormalizedForm(knmNFD, &qtss));
	CheckHr(m_qtssLabel->Equals(qtss, &fEqual));
	if (!fEqual)
		m_qtssLabel = qtss;
	const OLECHAR * prgch;
	int cch;
	CheckHr(pode->CreateCommand(&qodc));
	stu.Format(L"exec SetMultiTxt$ %d, %d, %d, ?", kflidUserViewField_Label, m_hvo, ws);
	m_qtssLabel->LockText(&prgch, &cch);
	CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
		(ULONG *)prgch, cch * sizeof(OLECHAR)));
	m_qtssLabel->UnlockText(prgch);
	CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));

	// Save the help string for the UserViewField.
	if (m_qtssHelp)
	{
		// First, make sure that the help string is normalized.  This may be paranoid overkill.
		CheckHr(m_qtssHelp->get_NormalizedForm(knmNFD, &qtss));
		CheckHr(m_qtssHelp->Equals(qtss, &fEqual));
		if (!fEqual)
			m_qtssHelp = qtss;
		CheckHr(pode->CreateCommand(&qodc));
		stu.Format(L"exec SetMultiTxt$ %d, %d, %d, ?", kflidUserViewField_HelpString, m_hvo,
			ws);
		m_qtssHelp->LockText(&prgch, &cch);
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)prgch, cch * sizeof(OLECHAR)));
		m_qtssHelp->UnlockText(prgch);
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
	}

	return m_hvo;
}


/*----------------------------------------------------------------------------------------------
	Save the UserViewField details information to the database.
----------------------------------------------------------------------------------------------*/
void FldSpec::SaveDetails(ISilDataAccess * pda)
{
	AssertPtr(pda);
	Assert(m_hvo); // The hvo must be set prior to calling this.

	// One or two ints are currently stored in the Details field (lsb first)
	BYTE rgb[8]; // Two ints.

	// The first int stores m_fHideLabel as bit 31, and m_dxpColumn as the lower bits.
	Assert(m_dxpColumn >= 0);
	(int &)rgb[0] = m_fHideLabel << 31 | m_dxpColumn;

	switch (m_ft)
	{
	case kftRefAtomic:
	case kftRefCombo:
	case kftRefSeq:
		{
			// It is a Choices List field type.
			// The second int stores m_fVert as bit 31, m_fHier as bit 30, and m_pnt as the
			// low bits (29-0).
			int n = m_fVert << 31 | m_fHier << 30 | m_pnt;
			(int &)rgb[4] = n;
			CheckHr(pda->SetBinary(m_hvo, kflidUserViewField_Details, rgb, 8));
			break;
		}
	case kftExpandable:
		{
			// It is an Expandable field type.
			// The second int stores m_fExpand as bit 31, m_fHier as bit 30, and m_pnt as the
			// low bits (29-0).
			int n = m_fExpand << 31 | m_fHier << 30 | m_pnt;
			(int &)rgb[4] = n;
			CheckHr(pda->SetBinary(m_hvo, kflidUserViewField_Details, rgb, 8));
			break;
		}
	case kftSubItems:
		{
			// It is a Hierarchical field type.
			// The second int stores m_fExpand as bit 31 and m_ons as low bits.
			int n = m_fExpand << 31 | m_ons;
			(int &)rgb[4] = n;
			CheckHr(pda->SetBinary(m_hvo, kflidUserViewField_Details, rgb, 8));
			break;
		}
	default:
		// Other types.
		// The second int is not stored.
		CheckHr(pda->SetBinary(m_hvo, kflidUserViewField_Details, rgb, 4));
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Load the three vectors with current analysis and vernacular encodings and a combination.
	@return true if successful, false if not.
	Mainly derived from the same method on AfLpInfo.
	This version however assume there is only one language project and hence just loads
	all the vernacular and analysis writing systems.
	Enhance JohnT: find a better strategy for multiple-lang-proj databases.
	Also the AfLpInfo version uses AfApp::UserEnc as a default for any empty lists, this one
	just uses English (as it doesn't assume there is an AfApp).
	Enhance JohnT: maybe it should use the UserEnc info from the resource file?
----------------------------------------------------------------------------------------------*/
void VwOleDbDa::LoadWritingSystems()
{
	IOleDbCommandPtr qodc;
	StrUni stu;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;

	// Get the encodings for the language project.
	// This version just does its best, ignore any problems.
	try
	{
		// Get the current vernacular encodings for the language project.
		stu.Format(L"SELECT lpc.Dst"
			L" FROM LangProject_CurVernWss lpc"
			L" ORDER BY lpc.Ord");
		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		m_vwsVern.Clear(); // Clear values from old project.
		while (fMoreRows)
		{
			int ws;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&ws),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			m_vwsVern.Push(ws);
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		if (!m_vwsVern.Size())
			m_vwsVern.Push(m_wsUser); // Use User interface writing system as a default

		// Get the current analysis encodings for the language project.
		stu.Format(L"SELECT lpc.Dst"
			L" FROM LangProject_CurAnalysisWss lpc"
			L" ORDER BY lpc.Ord");
		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		m_vwsAnal.Clear(); // Clear values from old project.
		while (fMoreRows)
		{
			int ws;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&ws),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			m_vwsAnal.Push(ws);
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		if (!m_vwsAnal.Size())
			m_vwsAnal.Push(m_wsUser); // Use User interface writing system as a default

		// Get the current AnalVern encodings for the language project.
		m_vwsAnalVern.Clear(); // Clear values
		int iWsA;
		int iWsV;
		for (iWsA = 0; iWsA < m_vwsAnal.Size(); iWsA++)
		{
			m_vwsAnalVern.Push(m_vwsAnal[iWsA]);
		}
		for (iWsV = 0; iWsV < m_vwsVern.Size(); iWsV++)
		{
			for (iWsA = 0; iWsA < m_vwsAnal.Size(); iWsA++)
			{
				if (m_vwsAnal[iWsA] == m_vwsVern[iWsV])
					break;
			}
			if(iWsA == m_vwsAnal.Size())
				m_vwsAnalVern.Push(m_vwsVern[iWsV]);
		}

		// Get the current VernAnal writing systems for the language project.
		m_vwsVernAnal.Clear(); // Clear values from old project.
		for (iWsV = 0; iWsV < m_vwsVern.Size(); iWsV++)
		{
			m_vwsVernAnal.Push(m_vwsVern[iWsV]);
		}
		for (iWsA = 0; iWsA < m_vwsAnal.Size(); iWsA++)
		{
			for (iWsV = 0; iWsV < m_vwsVern.Size(); iWsV++)
			{
				if (m_vwsVern[iWsV] == m_vwsAnal[iWsA])
					break;
			}
			if(iWsV == m_vwsVern.Size())
				m_vwsVernAnal.Push(m_vwsAnal[iWsA]);
		}
		// If all succeeds set the flag
		m_fLoadedWsInfo = true;
	}
	catch (...)
	{
		return;
	}
}

static DummyFactory g_factDS(_T("SIL.Views.VwDataSpec"));

/*----------------------------------------------------------------------------------------------
	Method to support using GenericFactory to create an instance. An actual generic factory
	instance is not made in this file, because it is included in many places. Instead, currently
	one generic factory exists in VwRootBox.cpp.
----------------------------------------------------------------------------------------------*/
void VwDataSpec::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	OutputDebugStr(L"called CreateCom VwDataSpec\n");
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwDataSpec> qdts;
	qdts.Attach(NewObj VwDataSpec());		// ref count initially 1
	CheckHr(qdts->QueryInterface(riid, ppv));
	OutputDebugStr(L"CreateCom completed\n");
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwDataSpec::VwDataSpec()
{
	// COM object behavior
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_quvs.Attach(NewObj UserViewSpec());
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwDataSpec::~VwDataSpec()
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Standard COM method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwDataSpec::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwDataSpec)
		*ppv = static_cast<IVwDataSpec *>(this);
	else if (&riid == &CLSID_VwDataSpec) // special for internal use
		*ppv = static_cast<VwDataSpec *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwDataSpec);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

/*----------------------------------------------------------------------------------------------
	Adds to the specification the information that for the specified class (clsid),
	data should be loaded about the specified property (tag). The FieldType indicates
	what kind of data is expected for that property. If it is a multilingual string
	property, ws indicates that a particular writing system's data should be loaded.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwDataSpec::AddField(int clsid, PropTag tag, FldType ft,
	ILgWritingSystemFactory * pwsf, int ws)
{
	BEGIN_COM_METHOD;

	m_fGotMetaNames = false;
	ClsLevel cl(clsid, 0);
	RecordSpecPtr qrsp;
	if (!m_quvs->m_hmclevrsp.Retrieve(cl, qrsp))
	{
		qrsp.Attach(NewObj RecordSpec(clsid, 0));
		// Init also installs it.
		qrsp->Init(m_quvs, clsid, 0, kvwtDE, pwsf); // view type is arbitrary for our purpose.
	}
	qrsp->AddField(true, 0, tag, ft, ws);
	// Review JohnT: do we need to add one at level 1 also to get correct behavior?

	END_COM_METHOD(g_factDS, IID_IVwDataSpec);
}

/*----------------------------------------------------------------------------------------------
	Set all of the field and class names from the Meta table.
----------------------------------------------------------------------------------------------*/
void VwDataSpec::SetMetaNames(IFwMetaDataCache * pmdc)
{
	if (m_fGotMetaNames)
		return;
	ClevRspMap::iterator ithmclevrspLim = m_quvs->m_hmclevrsp.End();
	for (ClevRspMap::iterator it = m_quvs->m_hmclevrsp.Begin(); it != ithmclevrspLim; ++it)
	{
		it.GetValue()->SetMetaNames(pmdc);
	}
	m_fGotMetaNames = true; // need not do this again unless more fields added.
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#IsValidObject}
	Test whether an HVO is in the range of dummy IDs.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_IsDummyId(HVO hvo, ComBool * pfDummy)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfDummy);

	*pfDummy = IsDummyId(hvo);
	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#IsValidObject}
	Test whether an HVO represents a valid object.  For the database
	cache, it will test whether the object is of a known class.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_IsValidObject(HVO hvo, ComBool * pfValid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfValid);
	if (!hvo)
	{
		// Zero fails as an HVO in LoadObjInfo, and here, we don't want to return an error.
		// ChkComOutPtr has already set the result to false.
		return S_OK;
	}
	if (IsDummyId(hvo))
	{
		// Generated dummy ID is considered valid if it has a class (already!) cached.
		int clid;
		CheckHr(get_IntProp(hvo, kflidCmObject_Class, &clid));
		*pfValid = (clid != 0);
		return S_OK;
	}

	// This is not a great idea because, if we are in load-all-of-class state, it does a
	// LOT of work. Also there is no point in removing the information from the cache
	// only to put it back.
	//// Delete (possibly stale) cached kflidCmObject_Class property
	//ObjPropRec oprKeySpecial(hvo, kflidCmObject_Class);
	//m_hmoprn.Delete(oprKeySpecial);

	//int clid;
	//CheckHr(get_IntProp(hvo, kflidCmObject_Class, &clid));
	//*pfValid = (clid != 0);
	StrUni stuSql;
	stuSql.Format(L"select Class$ from CmObject where id = %d", hvo);
	IOleDbCommandPtr qodc;
	CheckHr(m_qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	ComBool fIsNull;
	ComBool fMoreRows;
	int nVal;
	ULONG cbData;

	// Process each row.
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (!fMoreRows)
		return S_OK; // not valid
	CheckHr(qodc->GetColValue(1, (BYTE*)&nVal, sizeof(int), &cbData, &fIsNull, 0));
	CheckHr(CacheIntProp(hvo, kflidCmObject_Class, nVal));
	*pfValid = true;

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}

/*----------------------------------------------------------------------------------------------
	Get/Set the policy the cache should follow when asked for the value of a property
	that is not found.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::get_AutoloadPolicy(AutoloadPolicies * palp)
{
	BEGIN_COM_METHOD;
	if (palp == NULL) // can't use ChkComOutPtr because won't reliably cast to int*
		ThrowHr(WarnHr(E_POINTER));
	*palp = m_alpFullAutoloadPolicy;
	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}

/*----------------------------------------------------------------------------------------------
	Get/Set the policy the cache should follow when asked for the value of a property
	that is not found.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOleDbDa::put_AutoloadPolicy(AutoloadPolicies alp)
{
	BEGIN_COM_METHOD;

	AutoloadPolicies alpOldFull = m_alpFullAutoloadPolicy;
	AutoloadPolicies alpInternal = alp;
	AutoloadPolicies alpFull = alp;

	if (alp < 0 || alp >= kalpLim)
	{
		alpInternal = kalpLoadForThisObject; // use default (for forwards oompatibility)
		alpFull = kalpLoadForThisObject;
	}
	else if (alp == kalpLoadAllOfClassIncludingAllVirtuals)
	{
		alpInternal = kalpLoadForAllOfObjectClass;
	}
	m_alpFullAutoloadPolicy = alpFull;
	m_alpAutoloadPolicy = alpInternal;

	if (alpOldFull != alpFull &&
		(alpOldFull == kalpLoadAllOfClassIncludingAllVirtuals ||
			alpFull == kalpLoadAllOfClassIncludingAllVirtuals))
	{
		// Go through the list of virtual handlers to enable/disable bulk loading.  Disabling
		// including removing stored data.
		TagVhMap::iterator it;
		for (it = m_hmtagvh.Begin(); it != m_hmtagvh.End(); ++it)
		{
			it.GetValue()->SetLoadForAllOfClass(
				alpFull == kalpLoadAllOfClassIncludingAllVirtuals);
		}
	}

	END_COM_METHOD(g_fact, IID_IVwOleDbDa);
}

/*----------------------------------------------------------------------------------------------
	This is called when we are about to autoload property tag (for the specified ws, if
	multilingual) (for the specified class, if that is non-zero).
	If this is one of the properties we have recently autloaded in this way, don't do it again.
	This avoids doing massive reloads for properties of newly created objects.
	Return true to suppress autoloading this property now.
	Return false and record this as a new recent item if not recently autoloaded.
	Since the array is initialized to all zeros, we can scan even entries we have not
	otherwise initialized harmlessly.
----------------------------------------------------------------------------------------------*/
bool VwOleDbDa::TestAndNoteRecentAutoloads(PropTag tag, int ws, int clsid)
{
	for (int i = 0; i < kcRecentAutoloads; i++)
	{
		if (tag == m_rgalkRecentAutoLoads[i].tag
			&& ws == m_rgalkRecentAutoLoads[i].ws
			&& clsid == m_rgalkRecentAutoLoads[i].clsid)
		{
			return true; // recently autoloaded, don't do it again.
		}
	}
	m_ialkNext++;
	if (m_ialkNext >= kcRecentAutoloads)
		m_ialkNext = 0;
	m_rgalkRecentAutoLoads[m_ialkNext].tag = tag;
	m_rgalkRecentAutoLoads[m_ialkNext].ws = ws;
	m_rgalkRecentAutoLoads[m_ialkNext].clsid = clsid;
	return false;
}


#include "Vector_i.cpp"
//template Vector<FldSpec>;
template Vector<HvoClsid>;
template Vector<FldSpecPtr>;
template Vector<BlockSpecPtr>;
template Vector<UserViewSpecPtr>; // UserViewSpecVec; // Hungarian vuvs

#include "GpHashMap_i.cpp"
template GpHashMap<ClsLevel, RecordSpec>;

#include "Set_i.cpp"
template Set<AutoloadKey>; // m_salkLoadedProps;
