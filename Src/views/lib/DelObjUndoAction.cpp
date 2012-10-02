/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DelObjUndoAction.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file contains class definitions for the following class:
		DelObjUndoAction

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

#undef TRACE_SQLUNDOACTION
//#define TRACE_SQLUNDOACTION

//:>********************************************************************************************
//:>	DelObjUndoAction methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
DelObjUndoAction::DelObjUndoAction()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
DelObjUndoAction::~DelObjUndoAction()
{
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Method to support using GenericFactory to create an instance. An actual generic factory
	instance is not made in this file, because it is included in many places. Instead, currently
	one generic factory exists in VwRootBox.cpp.
----------------------------------------------------------------------------------------------*/
void DelObjUndoAction::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<DelObjUndoAction> qudo;
	qudo.Attach(NewObj DelObjUndoAction());		// ref count initially 1
	CheckHr(qudo->QueryInterface(riid, ppv));
}

static DummyFactory g_factSqlUndoAct(_T("SIL.Views.DelObjUndoAction"));

//:>********************************************************************************************
//:>	DelObjUndoAction - IUnknown Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Standard COM IUnknown method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DelObjUndoAction::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IUndoAction *>(this));
	else if (iid == IID_IUndoAction)
		*ppv = static_cast<IUndoAction *>(this);
	else if (iid == IID_IInitUndoDeleteObject)
		*ppv = static_cast<IInitUndoDeleteObject *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<IUnknown *>(static_cast<IUndoAction *>(this)),
			IID_IUndoAction, IID_IInitUndoDeleteObject);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Standard COM IUnknown method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) DelObjUndoAction::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}

/*----------------------------------------------------------------------------------------------
	Standard COM IUnknown method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) DelObjUndoAction::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


//:>********************************************************************************************
//:>	DelObjUndoAction - IUndoAction Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Utility function to execute a query and read one integer from the first column of the first
	row and return it.
	return 0 if no row set or value is null.
----------------------------------------------------------------------------------------------*/
int GetOneIntFromResultSet(IOleDbCommand * podc, BSTR bstrQry)
{
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	CheckHr(podc->ExecCommand(bstrQry, knSqlStmtSelectWithOneRowset));
	CheckHr(podc->GetRowset(0));
	CheckHr(podc->NextRow(&fMoreRows));
	if (!fMoreRows)
		return 0;
	int result = 0;
	CheckHr(podc->GetColValue(1, reinterpret_cast<BYTE *>(&result),
		isizeof(result), &cbSpaceTaken, &fIsNull, 0));
	if (fIsNull)
		result = 0;
	// Consume any other rows returned.
	return result;
}

/*----------------------------------------------------------------------------------------------
	Utility function for storing or executing a batch of queries.
	If stuQry is empty or there is not room to append it to stuQryBatch, execute the query and
	clear stuQryBatch. Then append stuQry to stuQryBatch, and clear stuQry.
----------------------------------------------------------------------------------------------*/
void AccumulateOrRunBatch(StrUniBufBig & stuQry, StrUniBufBig & stuQryBatch,
	IOleDbCommand * podc)
{
	if ((stuQry.Length() == 0 || stuQry.Length() + stuQryBatch.Length() > kcchMaxBufBig) &&
		stuQryBatch.Length() > 0)
	{
		CheckHr(podc->ExecCommand(stuQryBatch.Bstr(), knSqlStmtNoResults));
		stuQryBatch = stuQry;
	}
	else
	{
		stuQryBatch.Append(stuQry);
	}
	stuQry.Clear();
}

/*----------------------------------------------------------------------------------------------
	Utility function to set a binary/image/guid type field value as a command parameter.  Its
	interface is somewhat simpler than doing podc->SetParameter() directly.
----------------------------------------------------------------------------------------------*/
void SetBinaryParam(int iparam, WORD dbtype, Vector<byte> & vb, IOleDbCommand * podc)
{
	CheckHr(podc->SetParameter(iparam, DBPARAMFLAGS_ISINPUT, NULL, dbtype,
		reinterpret_cast<ULONG *>(vb.Begin()), vb.Size()));
}

/*----------------------------------------------------------------------------------------------
	Utility function to set a nvarchar/text type field value as a command parameter.  Its
	interface is somewhat simpler than doing podc->SetParameter() directly.
----------------------------------------------------------------------------------------------*/
void SetStringParam(int iparam, StrUni & stu, IOleDbCommand * podc)
{
	CheckHr(podc->SetParameter(iparam, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
		reinterpret_cast<ULONG *>(const_cast<OLECHAR *>(stu.Chars())),
		stu.Length() * isizeof(OLECHAR)));
}


/*----------------------------------------------------------------------------------------------
	${IUndoAction#Undo}

	Undo the action: re-create the objects and restore all relevant properties, both internal
	and external. (!)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DelObjUndoAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);

	IOleDbCommandPtr qodc;
	CheckHr(m_qode->CreateCommand(&qodc));
	// Verify that things are in a reasonable state to recreate the objects.
	// 1. All external objects that refer to one of the deleted obejcts must still exist.
	// 2. (Enhance:)All external objects that refer to one of the deleted objects in an atomic
	//    property must still have that property null.
	// 3. All external objects that the deleted objects used to point to must still exist.
	Set<HVO> shvoDelObjects; // ones that have been deleted.
	for (int i = 0; i < m_vcmiCoreInfo.Size(); ++i)
		shvoDelObjects.Insert(m_vcmiCoreInfo[i].m_hvoObj);
	Set<HVO> shvoRequiredObjects; // ones that must exist.
	for (int ihvo = 0; ihvo < m_vhvoBefore.Size(); ihvo++)
	{
		HVO hvoBefore = m_vhvoBefore[ihvo];
		if (hvoBefore != 0)
			shvoRequiredObjects.Insert(hvoBefore);
	}
	for (int i = 0; i < m_viriIncomingRefInfo.Size(); ++i)
	{
		// Incoming ref sources include ONLY objects outside the deleted objects
		// collection, ensured by the SQL that generates the info.
		shvoRequiredObjects.Insert(m_viriIncomingRefInfo[i].m_hvoSrc);
	}
	int cpt;
	for (int i = 0; i < m_vidoiIntInfo.Size(); ++i)
	{
		CheckHr(m_qmdc->GetFieldType(m_vidoiIntInfo[i].m_flid, &cpt));
		if (cpt == kcptReferenceAtom)
		{
			HVO hvo = m_vidoiIntInfo[i].m_val;
			if (!shvoDelObjects.IsMember(hvo))
				shvoRequiredObjects.Insert(hvo);
		}
	}
	for (int i = 0; i < m_vsqdoiSeqInfo.Size(); ++i)
	{
		for (int ihvo = 0; ihvo < m_vsqdoiSeqInfo[i].m_vhvoValue.Size(); ihvo++)
		{
			HVO hvoDst = m_vsqdoiSeqInfo[i].m_vhvoValue[ihvo];
			if (!shvoDelObjects.IsMember(hvoDst))
				shvoRequiredObjects.Insert(hvoDst);
		}
	}
	StrUniBufBig stuSql;
	if (shvoRequiredObjects.Size() != 0)
	{
		stuSql.Assign(L"select count(id) from CmObject where id in (");
		Set<HVO>::iterator it = shvoRequiredObjects.Begin();
		Set<HVO>::iterator itLim = shvoRequiredObjects.End();
		int chvo = 0; // number put into this string
		for(;it != itLim; ++it)
		{
			stuSql.FormatAppend(L"%s%d", (chvo > 0 ? L"," : L""), it->GetValue());
			chvo++;
			if (stuSql.Length() > 990)
			{
				stuSql.Append(L")");
				if (GetOneIntFromResultSet(qodc, stuSql.Bstr()) != chvo)
					return S_OK; // fSuccess is false.
				chvo = 0;
				stuSql = L"select count(id) from CmObject where id in (";
			}
		}
		if (chvo > 0)
		{
			stuSql.Append(L")");
			if (GetOneIntFromResultSet(qodc, stuSql.Bstr()) != chvo)
				return S_OK; // fSuccess is false.
		}
	}
	// Enhance JohnT(Undo): Could verify that external ref atomics are currently null.
	// Enhance JohnT(Undo): Could verify that each m_vhvoBefore (if non-zero) is still owned in the
	//                      right property.


	// Re-create the actual objects, with the correct class, owner, ownflid, ownord, and guid.
	for (int i = 0; i < m_vcmiCoreInfo.Size(); ++i)
	{
		int cpt = 0;
		CmObjectInfo & cmi = m_vcmiCoreInfo[i];
		if (cmi.m_flidOwner != 0)
			CheckHr(m_qmdc->GetFieldType(cmi.m_flidOwner, &cpt));
		// Re-create the objects.
		if (i < m_vhvoBefore.Size() && m_vhvoBefore[i] != 0)
		{
			stuSql.Format(L"set IDENTITY_INSERT CmObject ON; "
				L"exec CreateOwnedObject$ %d, %d, ?, %d, %d, %d, %d",
				cmi.m_clsid,  cmi.m_hvoObj, cmi.m_hvoOwner, cmi.m_flidOwner, cpt, m_vhvoBefore[i]);
		}
		else if (cmi.m_hvoOwner == 0)
		{
			// an unowned object
			Assert(cpt == 0 && cmi.m_flidOwner == 0);
			stuSql.Format(L"set IDENTITY_INSERT CmObject ON; "
				L"exec CreateObject$ %d, %d, ?",
				cmi.m_clsid, cmi.m_hvoObj);
		}
		else
		{
			// Either it is an atomic or collection property (ord < 0), or we are inserting at
			// the end (ord = chvo). In either case we pass null for the object to insert
			// before.  Because our original query ordered the objects by ownord$, object other
			// than the root are always created at the end of their property.
			// these lines should be the same as the other branch except for a final null
			// instead of %d in the query, and removing the before argument.
			stuSql.Format(L"set IDENTITY_INSERT CmObject ON; "
				L"exec CreateOwnedObject$ %d, %d, ?, %d, %d, %d, null",
				cmi.m_clsid,  cmi.m_hvoObj, cmi.m_hvoOwner, cmi.m_flidOwner, cpt);
		}
		qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_GUID,
			reinterpret_cast<ULONG *>(&cmi.m_guid), sizeof(cmi.m_guid));
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(m_qode->CreateCommand(&qodc)); // may need a clean one with no param set.
	}

	StrUniBufBig stuQry;
	StrUniBufBig stuQryBatch;
	SmartBstr sbstrClassName;
	SmartBstr sbstrFieldName;

	// Restore other properties.
	for (int i = 0; i < m_vidoiIntInfo.Size(); ++i)
	{
		IntDelObjInfo & ido =  m_vidoiIntInfo[i];
		GetNames(ido.m_flid, cpt, sbstrFieldName, sbstrClassName);
		stuQry.Format(L"update %s set [%s] = %d where Id = %d; ",
			sbstrClassName.Chars(), sbstrFieldName.Chars(), ido.m_val, ido.m_hvoObj);
		AccumulateOrRunBatch(stuQry, stuQryBatch, qodc);
	}
	AccumulateOrRunBatch(stuQry, stuQryBatch, qodc);

	for (int i = 0; i < m_vtdoiTimeInfo.Size(); ++i)
	{
		TimeDelObjInfo & tdo = m_vtdoiTimeInfo[i];
		GetNames(tdo.m_flid, cpt, sbstrFieldName, sbstrClassName);
		stuQry.Format(L"update %s set %s = ? where Id = %d",
			sbstrClassName.Chars(), sbstrFieldName.Chars(), tdo.m_hvoObj);
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_DBTIMESTAMP,
			reinterpret_cast<ULONG *>(&tdo.m_time), sizeof(tdo.m_time)));
		CheckHr(qodc->ExecCommand(stuQry.Bstr(), knSqlStmtNoResults));
		CheckHr(m_qode->CreateCommand(&qodc));
	}

	for (int i = 0; i < m_vsdoiStringInfo.Size(); ++i)
	{
		StringDelObjInfo & sdo = m_vsdoiStringInfo[i];
		GetNames(sdo.m_flid, cpt, sbstrFieldName, sbstrClassName);
		switch (cpt)
		{
		case kcptGuid:
			stuQry.Format(L"update %s set [%s] = ? where Id = %d",
				sbstrClassName.Chars(), sbstrFieldName.Chars(), sdo.m_hvoObj);
			SetBinaryParam(1, DBTYPE_GUID, sdo.m_vb, qodc);
			break;
		case kcptImage:
		case kcptBinary:
			stuQry.Format(L"update %s set [%s] = ? where Id = %d",
				sbstrClassName.Chars(), sbstrFieldName.Chars(), sdo.m_hvoObj);
			SetBinaryParam(1, DBTYPE_BYTES, sdo.m_vb, qodc);
			break;
		case kcptString:
		case kcptBigString:
			stuQry.Format(L"update [%<0>s] set [%<1>s] = ?, %<1>s_Fmt = ? where Id = %<2>d",
				sbstrClassName.Chars(), sbstrFieldName.Chars(), sdo.m_hvoObj);
			SetStringParam(1, sdo.m_stuText, qodc);
			SetBinaryParam(2, DBTYPE_BYTES, sdo.m_vb, qodc);
			break;
		case kcptUnicode:
		case kcptBigUnicode:
			stuQry.Format(L"update %s set [%s] = ? where Id = %d",
				sbstrClassName.Chars(), sbstrFieldName.Chars(), sdo.m_hvoObj);
			SetStringParam(1, sdo.m_stuText, qodc);
			break;
		case kcptMultiString:
			//exec proc [SetMultiStr$]
			//	@flid int,
			//	@obj int,
			//	@enc int,
			//	@txt nvarchar(4000),
			//	@fmt varbinary(8000)
			stuQry.Format(L"exec SetMultiStr$ %d, %d, %d, ?, ?", sdo.m_flid, sdo.m_hvoObj, sdo.m_ws);
			SetStringParam(1, sdo.m_stuText, qodc);
			SetBinaryParam(2, DBTYPE_BYTES, sdo.m_vb, qodc);
			break;
		case kcptMultiBigString:
			//exec proc [SetMultiBigStr$]
			//	@flid int,
			//	@obj int,
			//	@enc int,
			//	@txt ntext,
			//	@fmt image
			stuQry.Format(L"exec SetMultiBigStr$ %d, %d, %d, ?, ?", sdo.m_flid, sdo.m_hvoObj, sdo.m_ws);
			SetStringParam(1, sdo.m_stuText, qodc);
			SetBinaryParam(2, DBTYPE_BYTES, sdo.m_vb, qodc);
			break;
		case kcptMultiUnicode:
			//exec proc [SetMultiTxt$]
			//	@flid int,
			//	@obj int,
			//	@enc int,
			//	@txt nvarchar(4000)
			stuQry.Format(L"exec SetMultiTxt$ %d, %d, %d, ?", sdo.m_flid, sdo.m_hvoObj, sdo.m_ws);
			SetStringParam(1, sdo.m_stuText, qodc);
			break;
		case kcptMultiBigUnicode:
			//exec proc [SetMultiBigTxt$]
			//	@flid int,
			//	@obj int,
			//	@enc int,
			//	@txt ntext
			stuQry.Format(L"exec SetMultiBigTxt$ %d, %d, %d, ?", sdo.m_flid, sdo.m_hvoObj, sdo.m_ws);
			SetStringParam(1, sdo.m_stuText, qodc);
			break;
		// Doesn't work: some of these are views, and can't insert into them.
		//case kcptMultiUnicode:
		//case kcptMultiBigUnicode:
		//	stuQry.Format(L"insert into %s_%s (Obj, Ws, Txt) values(%d, %d, ?)",
		//		sbstrClassName.Chars(), sbstrFieldName.Chars(), sdo.m_hvoObj, sdo.m_ws);
		//	SetStringParam(1, sdo.m_stuText, qodc);
		//	break;
		//case kcptMultiString:
		//case kcptMultiBigString:
		//	stuQry.Format(L"insert into %s_%s (Obj, Ws, Txt, Fmt) values(%d, %d, ?, ?)",
		//		sbstrClassName.Chars(), sbstrFieldName.Chars(), sdo.m_hvoObj, sdo.m_ws);
		//	SetStringParam(1, sdo.m_stuText, qodc);
		//	SetBinaryParam(2, DBTYPE_BYTES, sdo.m_vb, qodc);
		//	break;
		}
		CheckHr(qodc->ExecCommand(stuQry.Bstr(), knSqlStmtNoResults));
		CheckHr(m_qode->CreateCommand(&qodc));
	}

	for (int i = 0; i < m_vsqdoiSeqInfo.Size(); ++i)
	{
		SeqDelObjInfo & sqdo = m_vsqdoiSeqInfo[i];
		GetNames(sqdo.m_flid, cpt, sbstrFieldName, sbstrClassName);
		switch (cpt)
		{
		case kcptReferenceCollection:
			for (int ihvo = 0; ihvo < sqdo.m_vhvoValue.Size(); ++ihvo)
			{
				stuQry.Format(L"insert into %s_%s (Src, Dst) values(%d, %d); ",
					sbstrClassName.Chars(), sbstrFieldName.Chars(),
					sqdo.m_hvoObj, sqdo.m_vhvoValue[ihvo]);
				AccumulateOrRunBatch(stuQry, stuQryBatch, qodc);
			}
			break;
		case kcptReferenceSequence:
			for (int ihvo = 0; ihvo < sqdo.m_vhvoValue.Size(); ++ihvo)
			{
				stuQry.Format(L"insert into %s_%s (Src, Dst, Ord) values(%d, %d, %d); ",
					sbstrClassName.Chars(), sbstrFieldName.Chars(),
					sqdo.m_hvoObj, sqdo.m_vhvoValue[ihvo], ihvo + 1);
				AccumulateOrRunBatch(stuQry, stuQryBatch, qodc);
			}
			break;
		default:
			Assert(cpt == kcptReferenceCollection || cpt == kcptReferenceSequence);
			break;
		}
	}

	for (int i = 0; i < m_viriIncomingRefInfo.Size(); ++i)
	{
		IncomingRefInfo & iri = m_viriIncomingRefInfo[i];
		GetNames(iri.m_flid, cpt, sbstrFieldName, sbstrClassName);
		switch (cpt)
		{
		case kcptReferenceAtom:
			stuQry.Format(L"update [%s] set [%s] = %d where Id = %d; ",
				sbstrClassName.Chars(), sbstrFieldName.Chars(), iri.m_hvoDst, iri.m_hvoSrc);
			break;
		case kcptReferenceCollection:
			stuQry.Format(L"insert into %s_%s (Src, Dst) values(%d, %d); ",
				sbstrClassName.Chars(), sbstrFieldName.Chars(), iri.m_hvoSrc, iri.m_hvoDst);
			break;
		case kcptReferenceSequence:
			stuQry.Format(L"insert into %s_%s (Src, Dst, Ord) values(%d, %d, %d); ",
				sbstrClassName.Chars(), sbstrFieldName.Chars(), iri.m_hvoSrc, iri.m_hvoDst,
				iri.m_ord);
			break;
		default:
			Assert(cpt == kcptReferenceAtom || cpt == kcptReferenceCollection ||
				cpt == kcptReferenceSequence);
			break;
		}
		AccumulateOrRunBatch(stuQry, stuQryBatch, qodc);
	}
	AccumulateOrRunBatch(stuQry, stuQryBatch, qodc);
	UpdateCache();

	// Issue a PropChanged for the owner.
	// If a refresh is pending we don't issue the PropChanged since the Refresh will
	// reload the cache.
	if (!fRefreshPending)
	{
		ISilDataAccessPtr qsda;
		IgnoreHr(m_qcda->QueryInterface(IID_ISilDataAccess, (void **)&qsda));
		if (qsda)
		{
			// Prior to the Undo, the objects didn't exist, so we don't need PropChanged calls for any of their own properties.
			// But we do need to put the information back in the cache.
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			for (int i = 0; i < m_vsdoiStringInfo.Size(); i++)
			{
				// Note: several other kinds of info can be stored in the objects of this vector, but they aren't
				// currently supported without Refresh.
				int cchData = m_vsdoiStringInfo[i].m_stuText.Length();
				int cbData = m_vsdoiStringInfo[i].m_vb.Size();
				ITsStringPtr qtssVal;
				CheckHr(qtsf->DeserializeStringRgch(m_vsdoiStringInfo[i].m_stuText.Chars(), &cchData, m_vsdoiStringInfo[i].m_vb.Begin(),
					&cbData, &qtssVal));
				CheckHr(m_qcda->CacheStringProp(m_vsdoiStringInfo[i].m_hvoObj, m_vsdoiStringInfo[i].m_flid, qtssVal));
			}
			// Put the object itself back AFTER we restore its properties.
			// Normally, if we are deleting more than one object, fRefreshPending will be true, because our own
			// RequiresRefresh method answers true. However, this can be overridden when this class is embedded in
			// CmObject.ObjectGroupUndoItem, which can be used for deleting annotations (and we don't care about PropChanged
			// for restoring the owning property for annotations!).
			if (m_vhvoBefore.Size() == 1)
			{
				Assert(IsTopLevelObjInOwningSeq() || IsTopLevelObjInType(kcptOwningCollection));  // We assume its an owning relationship

				// REVIEW (EberhardB): Do we have to call PropChanged for anything other than
				// an owning sequence?
				//if (!IsTopLevelObjInOwningSeq())
				//{
				//	m_qcda->PropChanged(NULL, kpctNotifyAll, m_vcmiCoreInfo[0].m_hvoOwner,
				//		m_vcmiCoreInfo[0].m_flidOwner, 0, 1, 1);
				//}
				//else
				{
					// restore our one object to its proper place in its owner.
					int ihvo;
					CheckHr(qsda->GetObjIndex(m_vcmiCoreInfo[0].m_hvoOwner,
						m_vcmiCoreInfo[0].m_flidOwner, m_vcmiCoreInfo[0].m_hvoObj, &ihvo));
					CheckHr(qsda->PropChanged(NULL, kpctNotifyAll, m_vcmiCoreInfo[0].m_hvoOwner,
						m_vcmiCoreInfo[0].m_flidOwner, ihvo, 1, 0));
				}
			}

			// Incoming refs must be restored with PropChanged.
			for (int i = 0; i < m_viriIncomingRefInfo.Size(); i++)
			{
				// Since we're undoing the effect of a delete on an atomic reference (sequences require refresh),
				// the property must have been empty before the Undo, and contain one thing now.
				CheckHr(qsda->PropChanged(NULL, kpctNotifyAll, m_viriIncomingRefInfo[0].m_hvoSrc,
					m_viriIncomingRefInfo[i].m_flid, 0, 1, 0));
			}
		}
	}

	*pfSuccess = true;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}


/*----------------------------------------------------------------------------------------------
	${IUndoAction#Redo}

	Executes all the redo SQL commands that were set for the DelObjUndoAction and reloads data
	into the affected data caches by using the redo reload SQL commands that were set for the
	DelObjUndoAction via the ${IDelObjUndoAction#AddRedoReloadInfo} method.  By doing this, the
	data in the cache(s) should reflect the values in the database.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DelObjUndoAction::Redo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);

	// Verify Redoable.
	if (m_vcmiCoreInfo.Size() == 0)
		return S_OK;	// *pfSuccess = false already.

	ISilDataAccessPtr qsda;
	try
	{
		CheckHr(m_qcda->QueryInterface(IID_ISilDataAccess, (void **)&qsda));
	}
	catch (Throwable)
	{
		// Don't care if this fails
	}

	int ihvoObj = -1;
	// Right now we do this only in the case where one hvoRoot was passed in to
	// GatherUndoInfo (i.e. not for the call from CmObject.ObjectGroupUndoItem).
	if (qsda && m_vhvoBefore.Size() == 1)
	{
		CheckHr(qsda->GetObjIndex(m_vcmiCoreInfo[0].m_hvoOwner,
			m_vcmiCoreInfo[0].m_flidOwner, m_vcmiCoreInfo[0].m_hvoObj, &ihvoObj));
	}

	StrUni stuSql;
	if (m_vhvoBefore.Size() <= 1)
	{
		HVO hvoObj = m_vcmiCoreInfo[0].m_hvoObj;

		// Create SQL command.
		stuSql.Format(L"EXEC DeleteObjects '%d'", hvoObj);
	}
	else
	{
		// We need to delete the first m_vhvoBefore.Size() objects which are the roots.
		// Create SQL command.
		stuSql.FormatAppend(L"EXEC DeleteObjects ',");
		for (int ihvo = 0; ihvo < m_vhvoBefore.Size(); ++ihvo)
			stuSql.FormatAppend(L"%d,", m_vcmiCoreInfo[ihvo].m_hvoObj);
		stuSql.FormatAppend(L"'%n");
	}
	IOleDbCommandPtr qodc;
	CheckHr(m_qode->CreateCommand(&qodc));
	// Actually execute the command.
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	UpdateCache();

	// Issue a PropChanged for the owner.
	// Right now we do this only in the case where one hvoRoot was passed in to
	// GatherUndoInfo (i.e. not for the call from CmObject.ObjectGroupUndoItem).
	if (m_vhvoBefore.Size() == 1)
	{
		Assert(IsTopLevelObjInOwningSeq() || IsTopLevelObjInType(kcptOwningCollection));  // We assume its an owning sequence

		// REVIEW (EberhardB): Do we have to call PropChanged for anything other then
		// a owning sequence?
		//if (IsTopLevelObjInOwningSeq())
		//{
		//	m_qcda->PropChanged(NULL, kpctNotifyAll, m_vcmiCoreInfo[0].m_hvoOwner,
		//		m_vcmiCoreInfo[0].m_flidOwner, 0, 1, 1);
		//}
		//else
		{
			if (qsda)
			{
				CheckHr(qsda->PropChanged(NULL, kpctNotifyAll, m_vcmiCoreInfo[0].m_hvoOwner,
					m_vcmiCoreInfo[0].m_flidOwner, ihvoObj, 0, 1));
			}
		}
	}

	*pfSuccess = true;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

// For now we do a minimal cache update by clearing out obsolete information
void DelObjUndoAction::UpdateCache()
{
	for (int i = 0; i < m_viriIncomingRefInfo.Size(); i++)
		CheckHr(m_qcda->ClearInfoAbout(m_viriIncomingRefInfo[i].m_hvoSrc, kciaRemoveObjectInfoOnly));
	for (int i = 0; i < m_vcmiCoreInfo.Size(); i++)
	{
		if (m_vcmiCoreInfo[i].m_hvoOwner != 0)
			CheckHr(m_qcda->ClearInfoAbout(m_vcmiCoreInfo[i].m_hvoOwner, kciaRemoveObjectInfoOnly));
		CheckHr(m_qcda->ClearInfoAbout(m_vcmiCoreInfo[i].m_hvoObj, kciaRemoveObjectInfoOnly));
	}
}


/*----------------------------------------------------------------------------------------------
	${IUndoAction#Commit}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DelObjUndoAction::Commit()
{
	BEGIN_COM_METHOD;

	ComBool fTransOpen;
	m_qode->IsTransactionOpen(&fTransOpen);
	if (fTransOpen)
	{
		CheckHr(m_qode->CommitTrans());
	}

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#IsDataChange}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DelObjUndoAction::IsDataChange(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);

	*pfRet = true;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#IsRedoable}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DelObjUndoAction::IsRedoable(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);

	*pfRet = true;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#RequiresRefresh}
	Currently we can handle (without refresh) incoming atomic references and objects
	that have only non-multilingual strings...it's mainly intended to handle deleting a
	paragraph in a text.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DelObjUndoAction::RequiresRefresh(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);

	*pfRet = true; // All our early-return scenarios require refresh.
	// Currently we can only work without a Refresh in certain specific cases.
	if (!m_qmdc)
		return S_OK; // we need an MDC (technically only if we have incoming refs?)
	// We don't (yet) know how to restore the cache on Undo for time, integer, or sequence props.
	if (m_vtdoiTimeInfo.Size() > 0 || m_vidoiIntInfo.Size() > 0 || m_vsqdoiSeqInfo.Size() > 0)
		return S_OK;
	// Nor for multi-strings (though it wouldn't be much harder)
	for (int i = 0; i < m_vsdoiStringInfo.Size(); i++)
	{
		int cpt;
		CheckHr(m_qmdc->GetFieldType(m_vsdoiStringInfo[i].m_flid, &cpt));
		if (cpt != kcptString && cpt != kcptBigString)
			return S_OK;
	}
	// Nor for incoming references other than atomic ones.
	for (int i = 0; i < m_viriIncomingRefInfo.Size(); i++)
	{
		int cpt;
		CheckHr(m_qmdc->GetFieldType(m_viriIncomingRefInfo[i].m_flid, &cpt));
		if (cpt != kcptReferenceAtom)
			return S_OK;
	}
	// And, so far, we can only handle a single delete.
	if (m_vhvoBefore.Size() != 1)
		return S_OK;
	*pfRet = false;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#SuppressNotification}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DelObjUndoAction::put_SuppressNotification(ComBool fSuppress)
{
	BEGIN_COM_METHOD;

	if (fSuppress)
		CheckHr(m_qcda->SuppressPropChanges());
	else
		CheckHr(m_qcda->ResumePropChanges());

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

//:>********************************************************************************************
//:>	DelObjUndoAction - other methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Utility method to retrieve field info from the meta data cache.
----------------------------------------------------------------------------------------------*/
void DelObjUndoAction::GetNames(int flid, int & cpt, SmartBstr & sbstrFieldName,
	SmartBstr & sbstrClassName)
{
	CheckHr(m_qmdc->GetFieldType(flid, &cpt));
	CheckHr(m_qmdc->GetFieldName(flid, &sbstrFieldName));
	CheckHr(m_qmdc->GetOwnClsName(flid, &sbstrClassName));
}

/*----------------------------------------------------------------------------------------------
	Utility function to retrieve data from the ImageVal column (7) of the output from the
	stored procedure GetUndoDelObjInfo.
----------------------------------------------------------------------------------------------*/
void ReadBinary(DelObjUndoAction::StringDelObjInfo & sdoi, IOleDbCommand * podc)
{
	Vector<byte> & vb = sdoi.m_vb;
	if (vb.Size() < 1000)
		vb.Resize(1000);
	ULONG cbData;
	ComBool fIsNull;
	CheckHr(podc->GetColValue(7, reinterpret_cast <BYTE *>(vb.Begin()),
		vb.Size(), &cbData, &fIsNull, 0));
	//  If buffer was too small, reallocate and try again.
	if ((cbData > (ULONG)vb.Size()) && (!fIsNull))
	{
		vb.Resize(cbData);
		CheckHr(podc->GetColValue(7, reinterpret_cast <BYTE *>(vb.Begin()),
			vb.Size(), &cbData, &fIsNull, 0));
	}
	else
	{
		vb.Resize(cbData);
	}
}

/*----------------------------------------------------------------------------------------------
	Utility function to retrieve data from the TextVal column (8) of the output from the
	stored procedure GetUndoDelObjInfo.
----------------------------------------------------------------------------------------------*/
void ReadText(DelObjUndoAction::StringDelObjInfo & sdoi, IOleDbCommand * podc)
{
	StrUni & stu = sdoi.m_stuText;
	OLECHAR * prgch;
	if (stu.Length() < 1000)
		stu.SetSize(1000, &prgch);
	else
		prgch = const_cast<OLECHAR *>(stu.Chars());
	ULONG cchData;
	ComBool fIsNull;
	CheckHr(podc->GetColValue(8, reinterpret_cast <BYTE *>(prgch),
		stu.Length() * isizeof(OLECHAR), &cchData, &fIsNull, 0));
	cchData /= isizeof(OLECHAR);
	//  If buffer was too small, reallocate and try again.
	if ((cchData > (ULONG)stu.Length()) && (!fIsNull))
	{
		stu.SetSize(cchData, &prgch);
		CheckHr(podc->GetColValue(8, reinterpret_cast <BYTE *>(prgch),
			stu.Length() * isizeof(OLECHAR), &cchData, &fIsNull, 0));
		cchData /= isizeof(OLECHAR);
	}
	if (cchData != (ULONG)stu.Length())
		stu.SetSize(cchData, & prgch);
}

/*----------------------------------------------------------------------------------------------
	Load all the information needed to handle Undo and Redo later on.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DelObjUndoAction::GatherUndoInfo(BSTR bstrIds, IOleDbEncap * pode,
	IFwMetaDataCache * pmdc, IVwCacheDa * pcda)
{
	BEGIN_COM_METHOD;
	m_qode = pode;
	m_qmdc = pmdc;
	m_qcda = pcda;
	// 1. Use GetOwnedObjects$ to create a result set that gives all the relevant CmObject info
	//    for the deleted objects.
	// 2. Use it again to get a list of the CmTypes of properties possessed by all the classes
	//    of deleted objects.
	// 3. For each type of property, generate a result set that has object id, field id, and
	//    whatever other info is needed to reproduce the property (except owning props). For
	//    sequence/collection props, generate multiple rows per object/flid as needed (in order,
	//    if a sequence)
	// 4. Generate incoming reference info as (src, dst, flid) for atomics and collections,
	//    (src, dst, flid, ord) for sequences.
	//    (Review: if a collection contains the same object repeatedly, do we get multiple rows
	//    or a count?)
	// Save this in a custom UndoAction and generate queries if necessary when undone.

	// Undo is possible unless an incoming atomic reference is no longer null, or unless an
	// object that had (any kind of) incoming reference no longer exists. (But an incoming
	// reference from one of the objects we're about to recreate has to be OK.)

	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	CheckHr(m_qode->CreateCommand(&qodc));
	StrUni stuQuery;

	// Collect all the basic CmObject information.
	m_vcmiCoreInfo.Clear();
	stuQuery.Format(L"SELECT co.id,co.Guid$,co.Class$,co.Owner$,co.OwnFlid$,co.OwnOrd$ "
		L"FROM dbo.fnGetOwnedObjects$("
		L"'%s', "		// single object id or xml list of object ids
		L"null,	"		// we want all owning prop types
		L"0, "			// we want base class records
		L"0, "			// but not subclasses
		L"1, "			// we want recursion (all owned, not just direct)
		L"null, "		// we want objects of any class
		L"0) oo	"		// we don't need an 'order key'
		L"join CmObject co on co.id = oo.ObjId "
		L" order by oo.OwnerDepth, co.owner$, co.ownflid$, co.OwnOrd$, co.id", // puts owners before parts, and root first.
		// also ensures that objects get recreated in the order they occur in sequences.
		// ordering by owner$ and ownflid$ keeps ones in the same property together and is helpful
		// in generating m_vhvoBefore below.
		// The final ordering by co.id ensures that things in the same owning collection are in a predictable
		// order so the query that generates m_vhvoBefore can produce them in the same order.
		bstrIds);
	CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		CmObjectInfo cmi;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cmi.m_hvoObj),
			isizeof(cmi.m_hvoObj), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&cmi.m_guid),
			isizeof(cmi.m_guid), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&cmi.m_clsid),
			isizeof(cmi.m_clsid), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&cmi.m_hvoOwner),
			isizeof(cmi.m_hvoOwner), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&cmi.m_flidOwner),
			isizeof(cmi.m_flidOwner), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(&cmi.m_ordOwner),
			isizeof(cmi.m_ordOwner), &cbSpaceTaken, &fIsNull, 0));

		// In certain circumstances (possibly when deleting multiple objects, some of which are
		// already owned by each other...see LT-3957) an object can occur more than one
		// in the result set.
		bool fSkip = false;
		for (int i = 0; i < m_vcmiCoreInfo.Size(); i++)
		{
			if (m_vcmiCoreInfo[i].m_hvoObj == cmi.m_hvoObj)
			{
				fSkip = true;
				break;
			}
		}

		if (!fSkip)
			m_vcmiCoreInfo.Push(cmi);

		CheckHr(qodc->NextRow(&fMoreRows));
	}

	// REVIEW: how do we want to handle nonexistent objects?
	if (m_vcmiCoreInfo.Size() == 0)
		return S_OK;
	OLECHAR * pchFirstComma = (OLECHAR*)wcschr(bstrIds, ',');
	if (!pchFirstComma)
	{
		// If it is an owning sequence, retrieve and save the object (if any) that it comes before.
		if (IsTopLevelObjInOwningSeq() || IsTopLevelObjInType(kcptOwningCollection))
		{
			StrUni stuBefore;
			stuBefore.Format(L"select top 1 Id from CmObject "
				L"where owner$ = %d and ownflid$ = %d and ownord$ > %d order by ownord$",
				m_vcmiCoreInfo[0].m_hvoOwner, m_vcmiCoreInfo[0].m_flidOwner,
				m_vcmiCoreInfo[0].m_ordOwner);
			m_vhvoBefore.Push(GetOneIntFromResultSet(qodc, stuBefore.Bstr()));
		}
	}
	else
	{
		// trickier...we potentially have to retrieve a before object for each root.
		// Note that this must generate co.id in the exact same order as the query above.
		// The final order by co.id ensures this for things in the same owning collection property.
		stuQuery.Format(
			L" select "
			L"	co.id, "
			L"	(select top 1 id "
			L"		from CmObject coNext "
			L"		where coNext.Owner$ = co.owner$ "
			L"			and coNext.OwnFlid$ = co.OwnFlid$ "
			L"			and coNext.OwnOrd$ > co.OwnOrd$) as before "
			L" from fnGetIdsFromString('%<0>s') i "
			L" join [CmObject] co on co.[Id] = i.[Id] "
			L" order by co.owner$, co.ownflid$, co.ownord$, co.id",
			bstrIds);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		m_vhvoBefore.Clear();
		while (fMoreRows)
		{
			HVO hvo;
			HVO hvoBefore;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvo = 0;
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hvoBefore),
				isizeof(hvoBefore), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvoBefore = 0;
			// OK, object hvo comes before object hvoBefore.
			Assert(m_vcmiCoreInfo[m_vhvoBefore.Size()].m_hvoObj == hvo);
			m_vhvoBefore.Push(hvoBefore);
			// But, previous objects may have 'before' values that are the objects we're deleting.
			// Any previous object that thinks it should be inserted before the current object
			// should actually be inserted before the new following object.
			for (int i = m_vhvoBefore.Size() - 2; i >= 0; i--)
			{
				if (m_vhvoBefore[i] == hvo)
					m_vhvoBefore[i] = hvoBefore;
				else
					break;
			}
			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}

	// Collect all the information needed for Undo/Redo.
	stuQuery.Format(L"DECLARE @fIsNocountOn int; SET @fIsNocountOn = @@OPTIONS & 512%n"
		L"IF @fIsNocountOn = 0 SET NOCOUNT ON\n"
		L"CREATE TABLE #UndoDelObjInfo%n"
		L"(%n"
		L"	Type INT,%n"
		L"	ObjId INT,%n"
		L"	Flid INT,%n"
		L"	IntVal INT,%n"
		L"	TimeVal DATETIME,%n"
		L"	GuidVal UNIQUEIDENTIFIER,%n"
		L"	ImageVal IMAGE,%n"
		L"	TextVal NTEXT,%n"
		L"	Ord INT%n"
		L");%n"
		L"exec GetUndoDelObjInfo '%s';%n"
		L"select * from #UndoDelObjInfo ORDER BY Type, ObjId, Flid, Ord;%n"
		L"drop table #UndoDelObjInfo%n"
		L"IF @fIsNocountOn = 0 SET NOCOUNT OFF",
		bstrIds);
	CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		int cpt;
		DelObjInfo doi;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cpt),
			isizeof(cpt), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&doi.m_hvoObj),
			isizeof(doi.m_hvoObj), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&doi.m_flid),
			isizeof(doi.m_flid), &cbSpaceTaken, &fIsNull, 0));
		StringDelObjInfo sdoi;
		TimeDelObjInfo tdoi;
		IntDelObjInfo idoi;
		SeqDelObjInfo sqdoi;
		IncomingRefInfo iri;

		switch (cpt)
		{
		case kcptBoolean:
		case kcptInteger:
		case kcptGenDate:
		case kcptReferenceAtom:
			idoi.m_hvoObj = doi.m_hvoObj;
			idoi.m_flid = doi.m_flid;
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&idoi.m_val),
				isizeof(idoi.m_val), &cbSpaceTaken, &fIsNull, 0));
			m_vidoiIntInfo.Push(idoi);
			break;

		case kcptTime:
			tdoi.m_hvoObj = doi.m_hvoObj;
			tdoi.m_flid = doi.m_flid;
			CheckHr(qodc->GetColValue(5, reinterpret_cast <BYTE *>(&tdoi.m_time),
				isizeof(tdoi.m_time), &cbSpaceTaken, &fIsNull, 0));
			m_vtdoiTimeInfo.Push(tdoi);
			break;
		case kcptGuid:
			sdoi.m_hvoObj = doi.m_hvoObj;
			sdoi.m_flid = doi.m_flid;
			sdoi.m_ws = 0;
			sdoi.m_stuText.Clear();
			sdoi.m_vb.Resize(sizeof(GUID));
			CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(sdoi.m_vb.Begin()),
				sdoi.m_vb.Size(), &cbSpaceTaken, &fIsNull, 0));
			m_vsdoiStringInfo.Push(sdoi);
			break;
		case kcptImage:
		case kcptBinary:
			sdoi.m_hvoObj = doi.m_hvoObj;
			sdoi.m_flid = doi.m_flid;
			sdoi.m_stuText.Clear();
			sdoi.m_ws = 0;
			ReadBinary(sdoi, qodc);
			m_vsdoiStringInfo.Push(sdoi);
			break;

		case kcptString:
		case kcptBigString:
			sdoi.m_hvoObj = doi.m_hvoObj;
			sdoi.m_flid = doi.m_flid;
			sdoi.m_ws = 0;
			ReadBinary(sdoi, qodc);
			ReadText(sdoi, qodc);
			m_vsdoiStringInfo.Push(sdoi);
			break;
		case kcptMultiString:
		case kcptMultiBigString:
			sdoi.m_hvoObj = doi.m_hvoObj;
			sdoi.m_flid = doi.m_flid;
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&sdoi.m_ws),
				isizeof(sdoi.m_ws), &cbSpaceTaken, &fIsNull, 0));
			ReadBinary(sdoi, qodc);
			ReadText(sdoi, qodc);
			m_vsdoiStringInfo.Push(sdoi);
			break;
		case kcptUnicode:
		case kcptBigUnicode:
			sdoi.m_hvoObj = doi.m_hvoObj;
			sdoi.m_flid = doi.m_flid;
			sdoi.m_ws = 0;
			ReadText(sdoi, qodc);
			sdoi.m_vb.Clear();
			m_vsdoiStringInfo.Push(sdoi);
			break;
		case kcptMultiUnicode:
		case kcptMultiBigUnicode:
			sdoi.m_hvoObj = doi.m_hvoObj;
			sdoi.m_flid = doi.m_flid;
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&sdoi.m_ws),
				isizeof(sdoi.m_ws), &cbSpaceTaken, &fIsNull, 0));
			ReadText(sdoi, qodc);
			sdoi.m_vb.Clear();
			m_vsdoiStringInfo.Push(sdoi);
			break;

		case kcptReferenceCollection:
		case kcptReferenceSequence:
			{
				// These are identical because the query for sequences is written to return
				// them in order. Thus, our vector ends up in the correct order in the cases
				// where order matters at all.
				HVO hvoDst;
				CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&hvoDst),
					isizeof(hvoDst), &cbSpaceTaken, &fIsNull, 0));
				if (m_vsqdoiSeqInfo.Size() != 0 &&
					m_vsqdoiSeqInfo.Top()->m_hvoObj == doi.m_hvoObj &&
					m_vsqdoiSeqInfo.Top()->m_flid == doi.m_flid)
				{
					m_vsqdoiSeqInfo.Top()->m_vhvoValue.Push(hvoDst);
				}
				else
				{
					sqdoi.m_hvoObj = doi.m_hvoObj;
					sqdoi.m_flid = doi.m_flid;
					sqdoi.m_vhvoValue.Clear();
					sqdoi.m_vhvoValue.Push(hvoDst);
					m_vsqdoiSeqInfo.Push(sqdoi);
				}
			}
			break;

		case -kcptReferenceAtom:
		case -kcptReferenceCollection:
			iri.m_hvoDst = doi.m_hvoObj;
			iri.m_flid = doi.m_flid;
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&iri.m_hvoSrc),
				isizeof(iri.m_hvoSrc), &cbSpaceTaken, &fIsNull, 0));
			iri.m_ord = 0;
			m_viriIncomingRefInfo.Push(iri);
			break;
		case -kcptReferenceSequence:
			iri.m_hvoDst = doi.m_hvoObj;
			iri.m_flid = doi.m_flid;
			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&iri.m_hvoSrc),
				isizeof(iri.m_hvoSrc), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(9, reinterpret_cast<BYTE *>(&iri.m_ord),
				isizeof(iri.m_ord), &cbSpaceTaken, &fIsNull, 0));
			m_viriIncomingRefInfo.Push(iri);
			break;
		default:
			// Slightly more informative than Assert(false).
			Assert(cpt == kcptBoolean || cpt == kcptInteger);
			break;
		}
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	Returns whether or not the top level object (m_vcmiCoreInfo[0]) is owned in an owning
	sequence.
----------------------------------------------------------------------------------------------*/
bool DelObjUndoAction::IsTopLevelObjInOwningSeq()
{
	// If it doesn't have an owner, assume it isn't in an owning sequence
	// If it is an owning sequence, retrieve and save the object (if any) that it comes before.
	return IsTopLevelObjInType(kcptOwningSequence);
}

bool DelObjUndoAction::IsTopLevelObjInType(int cptTarget)
{
	// If it doesn't have an owner, assume it isn't the type
	if (!m_vcmiCoreInfo[0].m_hvoOwner)
		return false;

	Assert(m_vcmiCoreInfo[0].m_flidOwner);

	int cpt;
	CheckHr(m_qmdc->GetFieldType(m_vcmiCoreInfo[0].m_flidOwner, &cpt));
	return (cpt == cptTarget);
}

#include "Vector_i.cpp"
template Vector<DelObjUndoAction::CmObjectInfo>;		// CmiVec;
template Vector<DelObjUndoAction::StringDelObjInfo>;	// SdoiVec;
template Vector<DelObjUndoAction::TimeDelObjInfo>;		// TdoiVec;
template Vector<DelObjUndoAction::IntDelObjInfo>;		// IdoiVec;
template Vector<DelObjUndoAction::SeqDelObjInfo>;		// SqdoiVec;
template Vector<DelObjUndoAction::IncomingRefInfo>;		// IriVec;
