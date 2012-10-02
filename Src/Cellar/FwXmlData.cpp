/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: FwXmlData.cpp
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "Cellar.sqi"
//#define ODBC_BIT SQL_C_CHAR

#undef THIS_FILE
DEFINE_THIS_FILE

// The class factory for FwXmlData.
GenericFactory FwXmlData::s_fact(
	_T("FieldWorks.FwXmlData"),
	&CLSID_FwXmlData,
	_T("FieldWorks XML Data Object"),
	_T("Apartment"),
	&FwXmlData::CreateCom);

//:End Ignore

/*----------------------------------------------------------------------------------------------
	This static method is called by the class factory to create a FwXmlData object.

	@param punkOuter Must be NULL since we do not support "aggregation".
	@param iid GUID of the desired interface.
	@param ppv Address of a pointer to store the returned COM interface pointer.
----------------------------------------------------------------------------------------------*/
void FwXmlData::CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkOuter)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwXmlData> qzfwxd;

	qzfwxd.Attach(NewObj FwXmlData);
	CheckHr(qzfwxd->QueryInterface(iid, ppv));
}


/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IFwXmlData are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwXmlData::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_IFwXmlData)
		*ppv = static_cast<IFwXmlData *>(this);
	else if (iid == IID_IFwXmlData2)
		*ppv = static_cast<IFwXmlData2 *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IFwXmlData);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Standard COM AddRef method.

	@return The reference count after incrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) FwXmlData::AddRef()
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Standard COM Release method.

	@return The reference count after decrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) FwXmlData::Release()
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	Close();
	delete this;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Open a connection to the database.

	@param bstrServer Name of the database server machine.
	@param bstrDatabase Name of the database.

	@return S_OK or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwXmlData::Open(BSTR bstrServer, BSTR bstrDatabase)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrServer);
	ChkComBstrArgN(bstrDatabase);
	if (!bstrServer || !bstrDatabase){
		fprintf(stdout,"returning E_INVALIDARG\n");
		ReturnHr(E_INVALIDARG);
	}

	Close();
	CheckHr(m_sdb.Open(bstrServer, bstrDatabase));
	LoadMetaInfo();
	m_stuServer.Assign(bstrServer, BstrLen(bstrServer));
	m_stuDatabase.Assign(bstrDatabase, BstrLen(bstrDatabase));

	END_COM_METHOD(s_fact, IID_IFwXmlData);
}


/*----------------------------------------------------------------------------------------------
	Close our connection to the database.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwXmlData::Close()
{
	BEGIN_COM_METHOD;

	m_stuServer.Clear();
	m_stuDatabase.Clear();
	m_sdb.Close();

	END_COM_METHOD(s_fact, IID_IFwXmlData);
}


/*----------------------------------------------------------------------------------------------
	Read the module, class, and field meta-information from the database, and store it for
	future reference.
----------------------------------------------------------------------------------------------*/
void FwXmlData::LoadMetaInfo()
{
	Assert(m_sdb.IsOpen());

	// Flush any existing data.
	m_hmmidimod.Clear();
	m_hmsuimod.Clear();
	m_vfdmi.Clear();
	m_vstumod.Clear();
	m_hmcidicls.Clear();
	m_hmsuicls.Clear();
	m_vfdci.Clear();
	m_vstucls.Clear();
	m_hmfidifld.Clear();
	m_mmsuifld.Clear();
	m_hmsuXmlifld.Clear();
	m_vfdfi.Clear();
	m_vstufld.Clear();
	m_vstufldXml.Clear();
	m_mpmodclss.Clear();
	m_mpclsflds.Clear();
	m_nVersion = 0;

	StrUni stu;
	RETCODE rc;
	SqlStatement sstmt;
	SQLWCHAR rgchName[kcchMaxName+1];
	SQLCHAR fAbs;
	SDWORD cbId;
	SDWORD cbName;
	SDWORD cbVer;
	SDWORD cbVerBack;
	SDWORD cbMod;
	SDWORD cbBase;
	SDWORD cbAbstract;
	SDWORD cbType;
	SDWORD cbClass;
	SDWORD cbDstCls;
	SQLCHAR fCustom;
	SDWORD cbCustom;

	OdbcType ot;


	// Load the FieldWorks database "module" information.
	FwDbModuleInfo fdmi;
	int imod;
	sstmt.Init(m_sdb);
	CheckSqlRc(SQLExecDirectW(sstmt.Hstmt(), L"SELECT Id,Name,Ver,VerBack FROM Module$;",
		SQL_NTS));
	/*CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &fdmi.mid, isizeof(fdmi.mid), &cbId));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 2, SQL_C_WCHAR, rgchName, kcchMaxName, &cbName));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &fdmi.ver, isizeof(fdmi.ver), &cbVer));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 4, SQL_C_SLONG, &fdmi.verBack, isizeof(fdmi.verBack),
		&cbVerBack));*/
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 1, ot.SLONG, &fdmi.mid, isizeof(fdmi.mid), &cbId));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 2, ot.WCHAR, rgchName, kcchMaxName, &cbName));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 3, ot.SLONG, &fdmi.ver, isizeof(fdmi.ver), &cbVer));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 4, ot.SLONG, &fdmi.verBack, isizeof(fdmi.verBack),
		&cbVerBack));
	for (imod = 0; ; ++imod)
	{
		Assert(imod == m_vfdmi.Size());
		Assert(imod == m_vstumod.Size());
		rc = SQLFetch(sstmt.Hstmt());
		/*switch(rc){
			case SQL_SUCCESS: fprintf(stdout,"rc=SQL_SUCCESS=%d, iter=%d\t",rc,imod); break;
			case SQL_SUCCESS_WITH_INFO: fprintf(stdout,"rc=SQL_SUCCESS_WITH_INFO=%d, iter=%d\t",rc,imod); break;
			case SQL_STILL_EXECUTING: fprintf(stdout,"rc=SQL_STILL_EXECUTING=%d, iter=%d\t",rc,imod); break;
			case SQL_ERROR: fprintf(stdout,"rc=SQL_ERROR=%d, iter=%d\t",rc,imod); break;
			case SQL_INVALID_HANDLE: fprintf(stdout,"rc=SQL_INVALID_HANDLE=%d, iter=%d\t",rc,imod); break;
			case SQL_NO_DATA_FOUND: fprintf(stdout,"rc=SQL_NO_DATA_FOUND=%d, iter=%d\t",rc,imod); break;
			default: fprintf(stdout,"rc=dontknow=%d, iter=%d\t",rc,imod); break;
		}*/
		if (rc != SQL_SUCCESS)
			break;
		stu = rgchName;

		/*fprintf(stdout, "Id=%d, Ver=%d, VerBack=%d, Name=", fdmi.mid, fdmi.ver, fdmi.verBack);
		for(int i=0; i<stu.Length(); i++)
			fprintf(stdout, "%c", stu[i]);
		fprintf(stdout,"\n");*/
		m_hmmidimod.Insert(fdmi.mid, imod);
		m_hmsuimod.Insert(stu, imod);
		m_vfdmi.Push(fdmi);
		m_vstumod.Push(stu);
	}
	if (rc != SQL_NO_DATA)
	{
		// REVIEW SteveMc: Handle possible error message?
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	Assert(m_vfdmi.Size() == m_vstumod.Size());
	Assert(m_vfdmi.Size() == m_hmmidimod.Size());
	Assert(m_vfdmi.Size() == m_hmsuimod.Size());
	sstmt.Clear();
	m_mpmodclss.Resize(m_vfdmi.Size());

	// Load the FieldWorks database "class" information.

	FwDbClassInfo fdci;
	int icls;
	sstmt.Init(m_sdb);
	CheckSqlRc(SQLExecDirectW(sstmt.Hstmt(), L"SELECT Id,Mod,Base,Abstract,Name FROM Class$;",
		SQL_NTS));
	/*CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &fdci.cid, isizeof(fdci.cid), &cbId));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &fdci.mid, isizeof(fdci.mid), &cbMod));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &fdci.cidBase, isizeof(fdci.cidBase),
		&cbBase));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 4, SQL_C_BIT, &fAbs, isizeof(fAbs), &cbAbstract));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 5, SQL_C_WCHAR, rgchName, kcchMaxName, &cbName));*/
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 1, ot.SLONG, &fdci.cid, isizeof(fdci.cid), &cbId));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 2, ot.SLONG, &fdci.mid, isizeof(fdci.mid), &cbMod));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 3, ot.SLONG, &fdci.cidBase, isizeof(fdci.cidBase),
		&cbBase));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 4, ot.BIT, &fAbs, isizeof(fAbs), &cbAbstract));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 5, ot.WCHAR, rgchName, kcchMaxName, &cbName));

	for (icls = 0; ; ++icls)
	{
		Assert(icls == m_vfdci.Size());
		Assert(icls == m_vstucls.Size());
		rc = SQLFetch(sstmt.Hstmt());

		/*switch(rc){
			case SQL_SUCCESS: fprintf(stdout,"rc=SQL_SUCCESS=%d, iter=%d\t",rc,icls); break;
			case SQL_SUCCESS_WITH_INFO: fprintf(stdout,"rc=SQL_SUCCESS_WITH_INFO=%d, iter=%d\t",rc,icls); break;
			case SQL_STILL_EXECUTING: fprintf(stdout,"rc=SQL_STILL_EXECUTING=%d, iter=%d\t",rc,icls); break;
			case SQL_ERROR: fprintf(stdout,"rc=SQL_ERROR=%d, iter=%d\t",rc,icls); break;
			case SQL_INVALID_HANDLE: fprintf(stdout,"rc=SQL_INVALID_HANDLE=%d, iter=%d\t",rc,icls); break;
			case SQL_NO_DATA_FOUND: fprintf(stdout,"rc=SQL_NO_DATA_FOUND=%d, iter=%d\t",rc,icls); break;
			default: fprintf(stdout,"rc=dontknow=%d, iter=%d\t",rc,icls); break;
		}*/

		if (rc != SQL_SUCCESS){
			break;
		}

		fdci.fAbstract = fAbs ? VARIANT_TRUE : VARIANT_FALSE;
		stu = rgchName;
		/*fprintf(stdout, "clid=%d, mod=%d, base=%d, abs=%d name=", fdci.cid, fdci.mid, fdci.cidBase, fAbs);
		for(int i=0; i<stu.Length(); i++)
			fprintf(stdout, "%c", stu[i]);
		fprintf(stdout,"\n");*/
		m_hmcidicls.Insert(fdci.cid, icls);
		m_hmsuicls.Insert(stu, icls);
		m_vfdci.Push(fdci);
		m_vstucls.Push(stu);
		// Link modules to their classes (map module index to list of class indices).
		if (m_hmmidimod.Retrieve(fdci.mid, &imod))
			m_mpmodclss[imod].Push(icls);
		else
			ThrowHr(WarnHr(E_UNEXPECTED));		// This should never happen!
	}
	if (rc != SQL_NO_DATA)
	{
		// REVIEW SteveMc: Handle possible error message?
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	Assert(m_vfdci.Size() == m_vstucls.Size());
	Assert(m_vfdci.Size() == m_hmcidicls.Size());
	Assert(m_vfdci.Size() == m_hmsuicls.Size());
	sstmt.Clear();

	// Load the FieldWorks database "field" information.

	FwDbFieldInfo fdfi;
	int jfld;
	sstmt.Init(m_sdb);

	CheckSqlRc(SQLExecDirectW(sstmt.Hstmt(),
		L"SELECT Id,Type,Class,DstCls,Name,Custom,"
		L"\"MIN\",\"MAX\",Big,UserLabel,HelpString,ListRootId,WsSelector,XmlUI"
		L" FROM Field$;",
		SQL_NTS));


	Assert(sizeof(int) == sizeof(SQLINTEGER));
	/*CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &fdfi.fid, isizeof(fdfi.fid), &cbId));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &fdfi.cpt, isizeof(fdfi.cpt),
		&cbType));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &fdfi.cid, isizeof(fdfi.cid),
		&cbClass));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 4, SQL_C_SLONG, &fdfi.cidDst, isizeof(fdfi.cidDst),
		&cbDstCls));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 5, SQL_C_WCHAR, rgchName, isizeof(rgchName),
		&cbName));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 6, SQL_C_BIT, &fCustom, isizeof(fCustom), &cbCustom));*/
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 1, ot.SLONG, &fdfi.fid, isizeof(fdfi.fid), &cbId));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 2, ot.SLONG, &fdfi.cpt, isizeof(fdfi.cpt),
		&cbType));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 3, ot.SLONG, &fdfi.cid, isizeof(fdfi.cid),
		&cbClass));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 4, ot.SLONG, &fdfi.cidDst, isizeof(fdfi.cidDst),
		&cbDstCls));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 5, ot.WCHAR, rgchName, isizeof(rgchName),
		&cbName));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 6, ot.BIT, &fCustom, isizeof(fCustom), &cbCustom));

	SQLCHAR fBig;
	SQLWCHAR rgchUserLabel[kcchMaxName+1];
	SQLWCHAR rgchHelpString[kcchMaxName+1];
	Vector<SQLWCHAR> vchXmlUI;
	vchXmlUI.Resize(1000);
	StrUni stuXmlUI;

	SDWORD cbMin;
	SDWORD cbMax;
	SDWORD cbBig;
	SDWORD cbUserLabel;
	SDWORD cbHelpString;
	SDWORD cbListRootId;
	SDWORD cbWsSelector;
	SDWORD cbXmlUI;

	/*CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 7, SQL_C_SBIGINT, &fdfi.nMin, isizeof(fdfi.nMin),
		&cbMin));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 8, SQL_C_SBIGINT, &fdfi.nMax, isizeof(fdfi.nMax),
		&cbMax));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 9, SQL_C_BIT, &fBig, isizeof(fBig), &cbBig));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 10, SQL_C_WCHAR, rgchUserLabel,
		isizeof(rgchUserLabel), &cbUserLabel));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 11, SQL_C_WCHAR, rgchHelpString,
		isizeof(rgchHelpString), &cbHelpString));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 12, SQL_C_SLONG, &fdfi.nListRootId,
		isizeof(fdfi.nListRootId), &cbListRootId));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 13, SQL_C_SLONG, &fdfi.nWsSelector,
		isizeof(fdfi.nWsSelector), &cbWsSelector));*/
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 7, ot.SBIGINT, &fdfi.nMin, isizeof(fdfi.nMin),
		&cbMin));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 8, ot.SBIGINT, &fdfi.nMax, isizeof(fdfi.nMax),
		&cbMax));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 9, ot.BIT, &fBig, isizeof(fBig), &cbBig));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 10, ot.WCHAR, rgchUserLabel,
		isizeof(rgchUserLabel), &cbUserLabel));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 11, ot.WCHAR, rgchHelpString,
		isizeof(rgchHelpString), &cbHelpString));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 12, ot.SLONG, &fdfi.nListRootId,
		isizeof(fdfi.nListRootId), &cbListRootId));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 13, ot.SLONG, &fdfi.nWsSelector,
		isizeof(fdfi.nWsSelector), &cbWsSelector));
	for (jfld = 0; ; ++jfld)
	{
		Assert(jfld == m_vfdfi.Size());
		Assert(jfld == m_vstufld.Size());
		memset(&fdfi, 0, sizeof(fdfi));
		rc = SQLFetch(sstmt.Hstmt());

		/*switch(rc){
			case SQL_SUCCESS: fprintf(stdout,"rc=SQL_SUCCESS=%d, iter=%d\t",rc,jfld); break;
			case SQL_SUCCESS_WITH_INFO: fprintf(stdout,"rc=SQL_SUCCESS_WITH_INFO=%d, iter=%d\t",rc,jfld); break;
			case SQL_STILL_EXECUTING: fprintf(stdout,"rc=SQL_STILL_EXECUTING=%d, iter=%d\t",rc,jfld); break;
			case SQL_ERROR: fprintf(stdout,"rc=SQL_ERROR=%d, iter=%d\t",rc,jfld); break;
			case SQL_INVALID_HANDLE: fprintf(stdout,"rc=SQL_INVALID_HANDLE=%d, iter=%d\t",rc,jfld); break;
			case SQL_NO_DATA_FOUND: fprintf(stdout,"rc=SQL_NO_DATA_FOUND=%d, iter=%d\t",rc,jfld); break;
			default: fprintf(stdout,"rc=dontknow=%d, iter=%d\t",rc,jfld); break;
		}*/

		if (rc != SQL_SUCCESS)
			break;

		// Read the big text field containing the XML defining the User Interfaces for this
		// field.
		rc = SQLGetData(sstmt.Hstmt(), 14, /*SQL_C_WCHAR*/ot.WCHAR, vchXmlUI.Begin(),
			vchXmlUI.Size() * isizeof(SQLWCHAR), &cbXmlUI);

		if (rc != SQL_SUCCESS && rc != SQL_NO_DATA && rc != SQL_SUCCESS_WITH_INFO)
		{
			--jfld;		// ignore this field.  REVIEW: is this the right thing to do?
			continue;
		}
		stuXmlUI.Clear();
		if (rc != SQL_NO_DATA && cbXmlUI != SQL_NULL_DATA && cbXmlUI != 0)
		{
			int cch = vchXmlUI.Size();
			int cchTotal = cbXmlUI / isizeof(SQLWCHAR);
			if (cch > cchTotal)
				cch = cchTotal;
			int cch1 = cch;
			if (!vchXmlUI[cch - 1])
				cch1 = cch - 1;							// Don't include trailing NUL.
			stuXmlUI.Assign(vchXmlUI.Begin(), cch1);
			if (cchTotal > cch)
			{
				// Get the rest of the string.
				Assert(rc == SQL_SUCCESS_WITH_INFO);
				vchXmlUI.Resize(cchTotal + 1);
				rc = SQLGetData(sstmt.Hstmt(), 15, SQL_C_WCHAR, vchXmlUI.Begin(),
					vchXmlUI.Size() * isizeof(SQLWCHAR), &cbXmlUI);
				CheckSqlRc(rc);
				stuXmlUI.Append(vchXmlUI.Begin(), cchTotal - cch1);
			}
		}
		fdfi.fBig = fBig ? VARIANT_TRUE : VARIANT_FALSE;
		fdfi.fNullMin = cbMin == SQL_NULL_DATA || cbMin == 0;
		fdfi.fNullMax = cbMax == SQL_NULL_DATA || cbMax == 0;
		fdfi.fNullBig = cbBig == SQL_NULL_DATA || cbBig == 0;
		if (cbListRootId == SQL_NULL_DATA || cbListRootId == 0)
			fdfi.nListRootId = 0;		// zero is invalid, so use it to flag NULL.
		if (cbWsSelector == SQL_NULL_DATA || cbWsSelector == 0)
			fdfi.nWsSelector = 0;		// zero is invalid, so use it to flag NULL.
		fdfi.fCustom = fCustom ? VARIANT_TRUE : VARIANT_FALSE;
		Assert(cbName != SQL_NULL_DATA && cbName != 0);	// Detect corrupted database.
		stu = rgchName;
	/*fprintf(stdout,"flid=%d, type=%d, custom=%d, big=%d name=", fdfi.fid, fdfi.cpt, fCustom, fBig);
	for(int i=0; i<stu.Length(); i++)
		fprintf(stdout, "%c", stu[i]);
	fprintf(stdout,"\n");*/
		m_hmfidifld.Insert(fdfi.fid, jfld);
		m_mmsuifld.Insert(stu, jfld);
		m_vfdfi.Push(fdfi);
		m_vstufld.Push(stu);
		stu.FormatAppend(L"%d",fdfi.cid);
		m_vstufldXml.Push(stu);
		m_hmsuXmlifld.Insert(stu, jfld);
		if (cbUserLabel == SQL_NULL_DATA || cbUserLabel == 0)
			stu.Clear();
		else
			stu = rgchUserLabel;
		m_vstufldUserLabel.Push(stu);
		if (cbHelpString == SQL_NULL_DATA || cbHelpString == 0)
			stu.Clear();
		else
			stu = rgchHelpString;
		m_vstufldHelpString.Push(stu);
		m_vstufldXmlUI.Push(stuXmlUI);
	}
	if (rc != SQL_NO_DATA)
	{
		// REVIEW SteveMc: Handle possible error message?
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	Assert(m_vfdfi.Size() == m_vstufld.Size());
	Assert(m_vfdfi.Size() == m_hmfidifld.Size());
	Assert(m_vfdfi.Size() == m_mmsuifld.Size());
	sstmt.Clear();

	// Load the FieldWorks database "class closure" information

	// REVIEW SteveMc: do we need to keep the class closure mappings after loading meta data?
	// They are currently discarded when this method finishes.
	MultiMap<int,int> mmcidcidSub;			// Map class id to all parent class ids.
	int cidSrc;
	int cidDst;
	int cDepth;
	SDWORD cbSrc;
	SDWORD cbDst;
	SDWORD cbDepth;
	sstmt.Init(m_sdb);
	CheckSqlRc(SQLExecDirectW(sstmt.Hstmt(), L"SELECT Src,Dst,Depth From ClassPar$;", SQL_NTS));
	/*CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &cidSrc, isizeof(cidSrc), &cbSrc));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &cidDst, isizeof(cidDst), &cbDst));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &cDepth, isizeof(cDepth), &cbDepth));*/
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 1, ot.SLONG, &cidSrc, isizeof(cidSrc), &cbSrc));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 2, ot.SLONG, &cidDst, isizeof(cidDst), &cbDst));
	CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 3, ot.SLONG, &cDepth, isizeof(cDepth), &cbDepth));
	for (jfld = 0; ;++jfld)
	{
		rc = SQLFetch(sstmt.Hstmt());
		/*switch(rc){
			case SQL_SUCCESS: fprintf(stdout,"rc=SQL_SUCCESS=%d, iter=%d\t",rc,jfld); break;
			case SQL_SUCCESS_WITH_INFO: fprintf(stdout,"rc=SQL_SUCCESS_WITH_INFO=%d, iter=%d\t",rc,jfld); break;
			case SQL_STILL_EXECUTING: fprintf(stdout,"rc=SQL_STILL_EXECUTING=%d, iter=%d\t",rc,jfld); break;
			case SQL_ERROR: fprintf(stdout,"rc=SQL_ERROR=%d, iter=%d\t",rc,jfld); break;
			case SQL_INVALID_HANDLE: fprintf(stdout,"rc=SQL_INVALID_HANDLE=%d, iter=%d\t",rc,jfld); break;
			case SQL_NO_DATA_FOUND: fprintf(stdout,"rc=SQL_NO_DATA_FOUND=%d, iter=%d\t",rc,jfld); break;
			default: fprintf(stdout,"rc=dontknow=%d, iter=%d\t",rc,jfld); break;
		}*/
		if (rc != SQL_SUCCESS)
			break;
		//fprintf(stdout,"Src=%d, Dst=%d\n",cidSrc, cidDst);
		mmcidcidSub.Insert(cidDst, cidSrc);
	}
	if (rc != SQL_NO_DATA)
	{
		// REVIEW SteveMc: Handle possible error message?
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	sstmt.Clear();

	// Link classes to their fields (map class index to list of field indices).

	m_mpclsflds.Resize(m_vfdci.Size());
	MultiMap<int,int>::iterator itcids;
	MultiMap<int,int>::iterator itcidsLim;
	for (int ifld = 0; ifld < m_vfdfi.Size(); ++ifld)
	{
		if (mmcidcidSub.Retrieve(m_vfdfi[ifld].cid, &itcids, &itcidsLim))
		{
			for (; itcids != itcidsLim; ++itcids)
			{
				if (m_hmcidicls.Retrieve(*itcids, &icls))
					m_mpclsflds[icls].Push(ifld);
			}
		}
		else
		{
			Assert(false);		// This should never happen!
			continue;
		}
	}

	// Get the database version number.
	sstmt.Init(m_sdb);
	CheckSqlRc(SQLExecDirectW(sstmt.Hstmt(), L"SELECT DbVer FROM Version$;", SQL_NTS));
	CheckSqlRc(SQLFetch(sstmt.Hstmt()));
	SDWORD cbVersion;
	CheckSqlRc(SQLGetData(sstmt.Hstmt(), 1, SQL_C_SLONG, &m_nVersion, isizeof(m_nVersion), &cbVersion));
	sstmt.Clear();

#if 99-99
	DumpMetaInfo();
#endif
}

#if 99
/*----------------------------------------------------------------------------------------------
	For all classes, dump the class name and collection of field names to a debugging file.
	This is a temporary hack until we change from using ODBC to using the OLEDB classes that
	encapsulate this meta-data for us.
----------------------------------------------------------------------------------------------*/
void FwXmlData::DumpMetaInfo()
{
	FILE * pfile;
	if (fopen_s(&pfile, "c:/tmp/classinfo.txt", "w"))
		return;
	fprintf(pfile, "%d classes, %d fields\n", m_vfdci.Size(), m_vfdfi.Size());
	int imod;
	int icls;
	int i;
	int ifld;
	StrAnsi staMod;
	StrAnsi staField;
	StrAnsi staBaseCls;
	const char * pszType;
	char szBuffer[64];
	int ic;
	HRESULT hr;
	for (icls = 0; icls < m_vfdci.Size(); ++icls)
	{
		staField = m_vstucls[icls];
		if (m_hmmidimod.Retrieve(m_vfdci[icls].mid, &imod))
			staMod = m_vstumod[imod];
		else
			staMod.Format("??mid = %d??", m_vfdci[icls].mid);
		fprintf(pfile, "Class[%3d] = %-30s (%d fields): module %s\n",
			m_vfdci[icls].cid, staField.Chars(), m_mpclsflds[icls].Size(), staMod.Chars());
		for (i = 0; i < m_mpclsflds[icls].Size(); ++i)
		{
			ifld = (m_mpclsflds[icls])[i];
			staField = m_vstufld[ifld];
			switch (m_vfdfi[ifld].cpt)
			{
			case kcptNil:					pszType = "Nil";					break;
			case kcptBoolean:				pszType = "Boolean";				break;
			case kcptInteger:				pszType = "Integer";				break;
			case kcptNumeric:				pszType = "Numeric";				break;
			case kcptFloat:					pszType = "Float";					break;
			case kcptTime:					pszType = "Time";					break;
			case kcptGuid:					pszType = "Guid";					break;
			case kcptImage:					pszType = "Image";					break;
			case kcptGenDate:				pszType = "GenDate";				break;
			case kcptBinary:				pszType = "Binary";					break;
			case kcptString:				pszType = "String";					break;
			case kcptMultiString:			pszType = "MultiString";			break;
			case kcptUnicode:				pszType = "Unicode";				break;
			case kcptMultiUnicode:			pszType = "MultiUnicode";			break;
			case kcptBigString:				pszType = "BigString";				break;
			case kcptMultiBigString:		pszType = "MultiBigString";			break;
			case kcptBigUnicode:			pszType = "BigUnicode";				break;
			case kcptMultiBigUnicode:		pszType = "MultiBigUnicode";		break;
			case kcptOwningAtom:			pszType = "OwningAtom";				break;
			case kcptReferenceAtom:			pszType = "ReferenceAtom";			break;
			case kcptOwningCollection:		pszType = "OwningCollection";		break;
			case kcptReferenceCollection:	pszType = "ReferenceCollection";	break;
			case kcptOwningSequence:		pszType = "OwningSequence";			break;
			case kcptReferenceSequence:		pszType = "ReferenceSequence";		break;
			default:
				sprintf_s(szBuffer, "??cpt = %d??", m_vfdfi[ifld].cpt);
				pszType = szBuffer;
				break;
			}
			if (m_vfdci[icls].cid == m_vfdfi[ifld].cid)
			{
				fprintf(pfile, "    F[%3d] = %-24s : %s%s\n",
					m_vfdfi[ifld].fid, staField.Chars(), pszType,
					m_vfdfi[ifld].fCustom ? " (Custom)" : "");
			}
			else
			{
				IgnoreHr(hr = m_hmcidicls.Retrieve(m_vfdfi[ifld].cid, &ic));
				if (FAILED(hr))
					staBaseCls.Format("???? [%d]", m_vfdfi[ifld].cid);
				else
					staBaseCls = m_vstucls[ic];
				fprintf(pfile, "    F[%3d] = %-24s : %-19s (from %s)%s\n",
					m_vfdfi[ifld].fid, staField.Chars(), pszType, staBaseCls.Chars(),
					m_vfdfi[ifld].fCustom ? " (Custom)" : "");
			}
		}
	}
	fclose(pfile);
}
#endif

//:> Explicit instantiation.
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
#include "MultiMap_i.cpp"

// Local Variables:
// compile-command:"cmd.exe /E:4096 /C ..\\..\\Bin\\mkcel.bat"
// End:
