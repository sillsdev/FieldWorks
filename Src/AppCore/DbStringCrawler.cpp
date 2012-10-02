/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DbStringCrawler.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Crawls through the database and does something to every text object.

	TODO: Handle accessing the string's characters.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore

//:>********************************************************************************************
//:> DbStringCrawler methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
DbStringCrawler::DbStringCrawler(bool fPlain, bool fFormatting, bool fBytes,
	bool fChars)
	: m_fPlain(fPlain), m_fFormatting(fFormatting),	m_fProcessBytes(fBytes),
		m_fCharacters(fChars)
{
	Assert(!m_fCharacters);  // not yet implemented

	Assert(m_fFormatting || m_fCharacters);

	m_fDontCloseMainWnd = false;
	m_fNeedRelaunch = false;

	m_fExactCount = true;

	m_fAntique = false;		// Use "Ws" by default for column name.
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
DbStringCrawler::~DbStringCrawler()
{
}

/*----------------------------------------------------------------------------------------------
	Set up the crawler to work on a given database connection.

	@param stuServerName - name of the database server
	@param stuDatabase - name of the database
	@param pstrmLog - pointer to the log stream (may be NULL)
	@param padvi3 - pointer to the progress report object (defaults to NULL)
----------------------------------------------------------------------------------------------*/
bool DbStringCrawler::Init(IOleDbEncap * pode, IStream *pstrmLog, IAdvInd3 * padvi3)
{
	m_qodeDb = pode;
	SmartBstr sbstrServer;
	StrUni stuServer;
	CheckHr(pode->get_Server(&sbstrServer));
	stuServer.Assign(sbstrServer.Chars(), sbstrServer.Length());
	SmartBstr sbstrDatabase;
	StrUni stuDatabase;
	CheckHr(pode->get_Database(&sbstrDatabase));
	stuDatabase.Assign(sbstrDatabase.Chars(), sbstrDatabase.Length());
	return Init(stuServer, stuDatabase, pstrmLog, padvi3);
}

/*----------------------------------------------------------------------------------------------
	Set up the crawler to work on a given database.

	@param stuServerName - name of the database server
	@param stuDatabase - name of the database
	@param pstrmLog - pointer to the log stream (may be NULL)
	@param padvi3 - pointer to the progress report object (defaults to NULL)
----------------------------------------------------------------------------------------------*/
bool DbStringCrawler::Init(StrUni stuServerName, StrUni stuDatabase, IStream * pstrmLog,
	IAdvInd3 * padvi3)
{
	m_qstrmLog = pstrmLog;
	m_stuServerName = stuServerName;
	m_stuDatabase = stuDatabase;
	m_qadvi3 = padvi3;

	try
	{
		m_qode.CreateInstance(CLSID_OleDbEncap);
		// Get the IStream pointer for logging. NULL returned if no log file.
		StrUni stuMasterDb(L"master");
		CheckHr(m_qode->Init(m_stuServerName.Bstr(), stuMasterDb.Bstr(), m_qstrmLog,
			koltMsgBox, koltvForever));
		CreateCommand();
	}
	catch (...)
	{
		// Cannot connect to master database.
		StrApp strMsg;
		strMsg.Load(kstidCannotGetMasterDb);
		::MessageBox(NULL, strMsg.Chars(), m_strProgDlgTitle.Chars(), MB_OK | MB_ICONERROR);
		return false;
	}

	if (m_qadvi3)
		m_qadvi3->SetRange(0, 100);

#ifndef NO_AFMAINWINDOW
	if (!m_fDontCloseMainWnd)
	{
		m_fNeedRelaunch = true;
		// In closing the final window, FwTool is going to call
		// AfApp::DecExportedObjects() which would normally post a quit message to
		// the application if there is only one window open. We don't want it to
		// quit yet, so we need to temporarily add an extra count until we get our
		// new window open.
		AfApp::Papp()->IncExportedObjects();
		// Now close the current database with all its windows.
		AfApp::Papp()->CloseDbAndWindows(m_stuDatabase.Chars(), m_stuServerName.Chars(), false);

		// Give users time to log off, then force them off anyway:
		ComBool fDisconnected;
		try
		{
			IDisconnectDbPtr qdscdb;
			qdscdb.CreateInstance(CLSID_FwDisconnect);

			// Set up strings needed for disconnection:
			StrUni stuReason(kstidReasonDisconnectStrCrawl);
			// Get our own name:
			DWORD nBufSize = MAX_COMPUTERNAME_LENGTH + 1;
			achar rgchBuffer[MAX_COMPUTERNAME_LENGTH + 1];
			GetComputerName(rgchBuffer, &nBufSize);
			StrUni stuComputer(rgchBuffer);
			StrUni stuFmt(kstidRemoteReasonStrCrawl);
			StrUni stuExternal;
			stuExternal.Format(stuFmt.Chars(), stuComputer.Chars());

			qdscdb->Init(m_stuDatabase.Bstr(), stuServerName.Bstr(), stuReason.Bstr(),
				stuExternal.Bstr(), (ComBool)false, NULL, 0);
			qdscdb->DisconnectAll(&fDisconnected);
		}
		catch (...)
		{
			fDisconnected = false;
		}
		if (!fDisconnected)
		{
			// User canceled, or it was impossible to disconnect people. This will leave us in
			// possibly a funny state, and nothing will be changed, but at least we'll open up
			// a new window on the app.
			return false;
		}
	}
#endif
	return true;
}

/*----------------------------------------------------------------------------------------------
	When the process is finished, open up a new window on the database.
	@param hvoRootObj - The object on which to bring up a new window
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::Terminate(HVO hvoRootObj)
{
	m_qodc.Clear();
	m_qode.Clear();
#ifndef NO_AFMAINWINDOW
	if (m_fNeedRelaunch)
	{
		AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
		Assert(papp);
		// Open and connect a window to the restored database:
		papp->ReopenDbAndOneWindow(m_stuDatabase.Chars(), m_stuServerName.Chars(), hvoRootObj);
		// Reopening the window will add a new count to our exported objects,
		// so we can now clear our temporary hold on the application.
		AfApp::Papp()->DecExportedObjects();
	}
#endif
	m_qadvi3.Clear();
}

/*----------------------------------------------------------------------------------------------
	Clear the database connection, and open a new one.
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::ResetConnection()
{
	m_qodc.Clear();
	m_qode.Clear();
	if (m_qodeDb)
	{
		m_qode = m_qodeDb;
	}
	else
	{
		m_qode.CreateInstance(CLSID_OleDbEncap);
		CheckHr(m_qode->Init(m_stuServerName.Bstr(), m_stuDatabase.Bstr(), m_qstrmLog,
			koltMsgBox, koltvForever));
	}
	// Don't create a command yet so that one can be created inside a transaction.
}

/*----------------------------------------------------------------------------------------------
	Get all the MONOlingual formatted string fields in the database.  The standard metadata
	cache does not provide the information we need, so we find all the class/field pairs whose
	values are String or BigString.

	@param podc Pointer to data base command object.
	@param vstuClass Reference to output class names (parallel to vstuField).
	@param vstuField Reference to output field names.
	@param rgnFlidsToIgnore array of flids that we don't want to put in output (defaults to
					empty)
	@param cFlidsToIgnore size of rgnFlidsToIgnore
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::GetFieldsForTypes(IOleDbCommand * podc, const int * rgnTypes, int cTypes,
	Vector<StrUni> & vstuClass, Vector<StrUni> & vstuField,
		const int * rgflidToIgnore, const int cflidToIgnore)
{
	AssertPtr(podc);
	AssertArray(rgnTypes, cTypes);
	AssertArray(rgflidToIgnore, cflidToIgnore);
	if (!cTypes)
		return;
	StrUni stuCmd;
	ComBool fMoreRows;
	ComBool fIsNull;
	unsigned long cbSpaceTaken;
	Vector<wchar> vchFieldName;
	Vector<wchar> vchClassName;
	int cchFieldName;
	int cchClassName;
	StrUni stu;

	stuCmd.Assign(L"SELECT f.Name, c.Name FROM Field$ f"
		L" JOIN Class$ c ON c.id = f.Class WHERE f.Type IN (");
	for (int i = 0; i < cTypes; ++i)
	{
		if (i != 0)
			stuCmd.FormatAppend(L",%d", rgnTypes[i]);
		else
			stuCmd.FormatAppend(L"%d", rgnTypes[i]);
	}
	stuCmd.Append(L")");
	if (cflidToIgnore)
	{
		stuCmd.Append(L" AND NOT f.Id IN (");
		for (int i = 0; i < cflidToIgnore; ++i)
		{
			if (i != 0)
				stuCmd.FormatAppend(L",%d", rgflidToIgnore[i]);
			else
				stuCmd.FormatAppend(L"%d", rgflidToIgnore[i]);
		}
		stuCmd.Append(L")");
	}
	CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(podc->GetRowset(0));
	CheckHr(podc->NextRow(&fMoreRows));
	vchFieldName.Resize(100);
	vchClassName.Resize(100);
	while (fMoreRows)
	{
		CheckHr(podc->GetColValue(1, reinterpret_cast<BYTE *>(vchFieldName.Begin()),
			vchFieldName.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
		cchFieldName = cbSpaceTaken / isizeof(wchar);
		Assert(cbSpaceTaken == cchFieldName * sizeof(wchar));
		if (cchFieldName >= vchFieldName.Size())
		{
			vchFieldName.Resize(cchFieldName + 1);
			CheckHr(podc->GetColValue(1,
				reinterpret_cast<BYTE *>(vchFieldName.Begin()),
				vchFieldName.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			cchFieldName = cbSpaceTaken / isizeof(wchar);
			Assert(cchFieldName < vchFieldName.Size());
		}
		CheckHr(podc->GetColValue(2, reinterpret_cast<BYTE *>(vchClassName.Begin()),
			vchClassName.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
		cchClassName = cbSpaceTaken / isizeof(wchar);
		Assert(cbSpaceTaken == cchClassName * sizeof(wchar));
		if (cchClassName >= vchClassName.Size())
		{
			vchClassName.Resize(cchClassName + 1);
			CheckHr(podc->GetColValue(2,
				reinterpret_cast<BYTE *>(vchClassName.Begin()),
				vchClassName.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			cchClassName = cbSpaceTaken / isizeof(wchar);
			Assert(cchClassName < vchClassName.Size());
		}
		stu.Assign(vchFieldName.Begin(), cchFieldName);
		vstuField.Push(stu);
		stu.Assign(vchClassName.Begin(), cchClassName);
		vstuClass.Push(stu);
		CheckHr(podc->NextRow(&fMoreRows));
	}
}



/*----------------------------------------------------------------------------------------------
	Create a simple format string containing only the writing system.  This is needed to handle
	cases where an empty format has somehow been stored in the database.  (See LT-6496.)
----------------------------------------------------------------------------------------------*/
static int CreateDummyFmt(int ws, byte * prgb, int cbLim)
{
	Assert(cbLim >= 19);
	// 01 00 00 00   00 00 00 00    00 00 00 00    01 00    06    XX XX XX XX
	int crun = 1;
	int offset = 0;
	byte ctip = 1;
	byte ctsp = 0;
	byte scp = kscpWs;
	memcpy(prgb, &crun, 4);			// 1 run
	memcpy(prgb + 4, &offset, 4);	// run starts at character offset 0
	memcpy(prgb + 8, &offset, 4);	// run starts at format byte offset 0
	memcpy(prgb + 12, &ctip, 1);	// 1 integer property
	memcpy(prgb + 13, &ctsp, 1);	// 0 string properties
	memcpy(prgb + 14, &scp, 1);		// property type (writing system)
	memcpy(prgb + 15, &ws, 4);		// property value
	return 19;
}


/*----------------------------------------------------------------------------------------------
	Do the main loop of looking through all fields in the database that may include
	monolingual or multilingual strings.
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::DoAll(int stid1, int stid2, bool fTrans, const OLECHAR * pszIdsTable)
{
	if (fTrans)
	{
		m_qodc.Clear();		// Can't have a command open when we start a transaction.
		BeginTrans();
		CreateCommand();
	}

	StrUni stuCmd;
	ComBool fMoreRows;
	ComBool fIsNull;
	unsigned long cbSpaceTaken;

	// Get the version number
	int nVer;
	stuCmd = L"select DbVer from version$";
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	CheckHr(m_qodc->NextRow(&fMoreRows));
	CheckHr(m_qodc->GetColValue(1, reinterpret_cast <BYTE *>(&nVer), sizeof(int),
		&cbSpaceTaken, &fIsNull, 0));

	int hobj = 0;
	int flid = 0;
	int encStr;
	Vector<byte> vbFmt;
	int cbFmt;

	int ieFix;
	Vector<int> vflidFix;
	Vector<int> vhobjFix;
	Vector<int> vwsFix;
	Vector<Vector<byte> > vvbFmtFix;

	ComVector<ITsTextProps> vqttp;
	Vector<int> vich;
	int cpttp;

	ITsPropsFactoryPtr qtpf;
	qtpf.CreateInstance(CLSID_TsPropsFactory);

	StrUni stuMsg;
	if (m_qadvi3)
	{
		stuMsg.Load(stid1);
		m_qadvi3->put_Message(stuMsg.Bstr());
	}

	// Review (SharonC): Does getting the count of the query results slow things down too much?

	// 1. Scan all multilingual formatted strings for embedded runs in the specified
	// writing system.
	// 1a. Regular multilingual strings: MultiStr
	int cRows;
	if (m_fExactCount && m_qadvi3)
	{
		stuCmd.Format(L"SELECT COUNT(*) FROM MultiStr$ a");
		if (pszIdsTable != NULL)
			stuCmd.FormatAppend(L" JOIN %s b ON b.ObjId = a.Obj", pszIdsTable);
		CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(m_qodc->GetRowset(0));
		CheckHr(m_qodc->NextRow(&fMoreRows));
		CheckHr(m_qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cRows),
			isizeof(int), &cbSpaceTaken, &fIsNull, 0));
	}

	// Get the default writing system in case it's needed (see LT-6496).
	int wsDefault;
	stuCmd.Format(
		L"SELECT TOP 1 Dst FROM %s ORDER BY Ord",
		nVer <= 200202 ? L"LanguageProject_CurrentAnalysisWritingSystems" : L"LangProject_CurAnalysisWss");
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	CheckHr(m_qodc->NextRow(&fMoreRows));
	CheckHr(m_qodc->GetColValue(1, reinterpret_cast<BYTE *>(&wsDefault),
		isizeof(int), &cbSpaceTaken, &fIsNull, 0));

	stuCmd.Format(L"SELECT a.Flid, a.Obj, a.%s, a.Fmt FROM MultiStr$ a",
		m_fAntique ? L"Enc" : L"Ws");
	if (pszIdsTable != NULL)
		stuCmd.FormatAppend(L" JOIN %s b ON b.ObjId = a.Obj", pszIdsTable);
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	CheckHr(m_qodc->NextRow(&fMoreRows));
	vbFmt.Resize(1024);
	int iRow = 0;
	while (fMoreRows)
	{
		bool fFixBroken = false;
		CheckHr(m_qodc->GetColValue(1, reinterpret_cast<BYTE *>(&flid),
			isizeof(flid), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(m_qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hobj),
			isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(m_qodc->GetColValue(3, reinterpret_cast<BYTE *>(&encStr),
			isizeof(encStr), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(m_qodc->GetColValue(4, reinterpret_cast<BYTE *>(vbFmt.Begin()),
			vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
		cbFmt = cbSpaceTaken;
		if (cbFmt >= vbFmt.Size())
		{
			vbFmt.Resize(cbFmt + 1);
			CheckHr(m_qodc->GetColValue(4, reinterpret_cast<BYTE *>(vbFmt.Begin()),
				vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
			cbFmt = cbSpaceTaken;
		}
		else if (cbFmt == 0)
		{
			// This should never happen, but it has (see LT-6496).
			cbFmt = CreateDummyFmt(encStr, vbFmt.Begin(), vbFmt.Size());
			fFixBroken = true;
		}
		vbFmt.Resize(cbFmt);

		bool fModify = false;
		if (m_fProcessBytes)
		{
			fModify = ProcessBytes(vbFmt);
		}
		else
		{
			int cb = vbFmt.Size();
			CheckHr(qtpf->DeserializeRgPropsRgb(0, vbFmt.Begin(), &cb,
				&cpttp, NULL, NULL));
			vqttp.Resize(cpttp);
			vich.Resize(cpttp);
			cb = vbFmt.Size();
			CheckHr(qtpf->DeserializeRgPropsRgb(cpttp, vbFmt.Begin(), &cb,
				&cpttp, (ITsTextProps **)vqttp.Begin(), vich.Begin()));
			fModify = ProcessFormatting(vqttp);
		}

		if (fModify || fFixBroken)
		{
			// Change made: mark the string for updating.
			vhobjFix.Push(hobj);
			vflidFix.Push(flid);
			vwsFix.Push(encStr);

			if (m_fProcessBytes)
			{
				MergeRuns(vbFmt);
			}
			else
			{
				MergeRuns(vqttp, vich);
				cpttp = vqttp.Size();
				Assert(cpttp == vich.Size());
				int cbNeeded;
				HRESULT hr;
				CheckHr(hr = vqttp[0]->SerializeRgPropsRgb(cpttp, (ITsTextProps **)vqttp.Begin(),
					vich.Begin(),
					vbFmt.Begin(), vbFmt.Size(), &cbNeeded));
				if (hr == S_FALSE)
				{
					vbFmt.Resize(cbNeeded);
					CheckHr(vqttp[0]->SerializeRgPropsRgb(cpttp, (ITsTextProps **)vqttp.Begin(),
						vich.Begin(),
						vbFmt.Begin(), vbFmt.Size(), &cbNeeded));
				}
				vbFmt.Resize(cbNeeded);
			}
			vvbFmtFix.Push(vbFmt);
		}
		CheckHr(m_qodc->NextRow(&fMoreRows));
		for (int ipttp = 0; ipttp < vqttp.Size(); ipttp++)
			vqttp[ipttp] = NULL;

		if (m_fExactCount && m_qadvi3)
		{
			iRow++;
			m_qadvi3->put_Position((iRow * 20) / cRows);
		}
	}
	if (m_qadvi3)
		m_qadvi3->put_Position(20);

	int nT;
	int ceFix = vhobjFix.Size();
	CreateCommand();
	for (ieFix = 0; ieFix < ceFix; ++ieFix)
	{
		stuCmd.Format(L"UPDATE MultiStr$ SET Fmt=? WHERE Obj = %d AND Flid = %d AND %s = %d",
			vhobjFix[ieFix], vflidFix[ieFix], m_fAntique ? L"Enc" : L"Ws", vwsFix[ieFix]);
		// Set the parameter and execute the command.
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(vvbFmtFix[ieFix].Begin()),
			vvbFmtFix[ieFix].Size()));
		CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		nT = ((ieFix + 1) * 40) / ceFix;
		if (nT && m_qadvi3)
			m_qadvi3->put_Position(20 + nT);
	}
	vhobjFix.Clear();
	vflidFix.Clear();
	vwsFix.Clear();
	vvbFmtFix.Clear();
	vqttp.Clear();
	vich.Clear();
	CreateCommand();
	// 1b. Big multilingual strings: MultiBigStr
	if (m_fExactCount && m_qadvi3)
	{
		stuCmd.Format(L"SELECT COUNT(*) FROM MultiBigStr$ a");
		if (pszIdsTable != NULL)
			stuCmd.FormatAppend(L" JOIN %s b ON b.ObjId = a.Obj", pszIdsTable);
		CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(m_qodc->GetRowset(0));
		CheckHr(m_qodc->NextRow(&fMoreRows));
		CheckHr(m_qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cRows),
			isizeof(int), &cbSpaceTaken, &fIsNull, 0));
	}

	stuCmd.Format(L"SELECT a.Flid, a.Obj, a.%s, a.Fmt FROM MultiBigStr$ a",
		m_fAntique ? L"Enc" : L"Ws");
	if (pszIdsTable != NULL)
		stuCmd.FormatAppend(L" JOIN %s b ON b.ObjId = a.Obj", pszIdsTable);
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	CheckHr(m_qodc->NextRow(&fMoreRows));
	vbFmt.Resize(1024);
	iRow = 0;
	while (fMoreRows)
	{
		bool fFixBroken = false;
		CheckHr(m_qodc->GetColValue(1, reinterpret_cast<BYTE *>(&flid),
			isizeof(flid), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(m_qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hobj),
			isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(m_qodc->GetColValue(3, reinterpret_cast<BYTE *>(&encStr),
			isizeof(encStr), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(m_qodc->GetColValue(4, reinterpret_cast<BYTE *>(vbFmt.Begin()),
			vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
		cbFmt = cbSpaceTaken;
		if (cbFmt >= vbFmt.Size())
		{
			vbFmt.Resize(cbFmt + 1);
			CheckHr(m_qodc->GetColValue(4, reinterpret_cast<BYTE *>(vbFmt.Begin()),
				vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
			cbFmt = cbSpaceTaken;
		}
		else if (cbFmt == 0)
		{
			// This should never happen, but it has (see LT-6496).
			cbFmt = CreateDummyFmt(encStr, vbFmt.Begin(), vbFmt.Size());
			fFixBroken = true;
		}
		vbFmt.Resize(cbFmt);

		bool fModify = false;
		if (m_fProcessBytes)
		{
			fModify = ProcessBytes(vbFmt);
		}
		else
		{
			int cb = vbFmt.Size();
			CheckHr(qtpf->DeserializeRgPropsRgb(0, vbFmt.Begin(), &cb,
				&cpttp, NULL, NULL));
			vqttp.Resize(cpttp);
			vich.Resize(cpttp);
			cb = vbFmt.Size();
			CheckHr(qtpf->DeserializeRgPropsRgb(cpttp, vbFmt.Begin(), &cb,
				&cpttp, (ITsTextProps **)vqttp.Begin(), vich.Begin()));

			fModify = ProcessFormatting(vqttp);
		}
		if (fModify || fFixBroken)
		{
			// Change made: mark the string for updating.
			vhobjFix.Push(hobj);
			vflidFix.Push(flid);
			vwsFix.Push(encStr);

			if (m_fProcessBytes)
			{
				MergeRuns(vbFmt);
			}
			else
			{
				MergeRuns(vqttp, vich);
				cpttp = vqttp.Size();
				Assert(cpttp == vich.Size());
				int cbNeeded;
				HRESULT hr;
				CheckHr(hr = vqttp[0]->SerializeRgPropsRgb(cpttp, (ITsTextProps **)vqttp.Begin(),
					vich.Begin(), vbFmt.Begin(), vbFmt.Size(), &cbNeeded));
				if (hr == S_FALSE)
				{
					vbFmt.Resize(cbNeeded);
					CheckHr(vqttp[0]->SerializeRgPropsRgb(cpttp, (ITsTextProps **)vqttp.Begin(),
						vich.Begin(), vbFmt.Begin(), vbFmt.Size(), &cbNeeded));
				}
				vbFmt.Resize(cbNeeded);
			}
			vvbFmtFix.Push(vbFmt);
		}
		CheckHr(m_qodc->NextRow(&fMoreRows));
		for (int ipttp = 0; ipttp < vqttp.Size(); ipttp++)
		{
			vqttp[ipttp] = NULL;
		}
		if (m_fExactCount && m_qadvi3)
		{
			iRow++;
			m_qadvi3->put_Position(50 + ((iRow * 20) / cRows));
		}
	}
	if (m_qadvi3)
		m_qadvi3->put_Position(70);
	ceFix = vhobjFix.Size();
	CreateCommand();
	for (ieFix = 0; ieFix < ceFix; ++ieFix)
	{
		stuCmd.Format(L"UPDATE MultiBigStr$ SET Fmt=? WHERE Obj = %d AND Flid = %d AND %s = %d",
			vhobjFix[ieFix], vflidFix[ieFix], m_fAntique ? L"Enc" : L"Ws", vwsFix[ieFix]);
		// Set the parameter and execute the command.
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(vvbFmtFix[ieFix].Begin()),
			vvbFmtFix[ieFix].Size()));
		CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		nT = ((ieFix + 1) * 30) / ceFix;
		if (nT && m_qadvi3)
			m_qadvi3->put_Position(70 + nT);
	}
	if (m_qadvi3)
		m_qadvi3->put_Position(100);

	vhobjFix.Clear();
	vflidFix.Clear();
	vwsFix.Clear();
	vvbFmtFix.Clear();
	vqttp.Clear();
	vich.Clear();
	CreateCommand();

	if (m_qadvi3)
	{
		stuMsg.Load(stid2);
		m_qadvi3->put_Message(stuMsg.Bstr());
		m_qadvi3->put_Position(0);
	}

	Vector<StrUni> vstuField;
	Vector<StrUni> vstuClass;

	// 2. Get all the MONOlingual formatted string fields in the database, scan all
	// formatted strings for embedded runs in the desired writing system.
	// Note that the standard metadata cache does not provide the information we need.
	//
	// To do this, find all the class/property pairs whose values are String or BigString.
	const int rgnTypes[] = { kcptString, kcptBigString };
	const int cTypes = isizeof(rgnTypes) / isizeof(int);
	DbStringCrawler::GetFieldsForTypes(m_qodc, rgnTypes, cTypes, vstuClass, vstuField);

	int nCurrPercent = 2;	// to avoid having the progress dlg go backwards!
	if (m_qadvi3)
		m_qadvi3->put_Position(2);

	int istu;
	int cField = vstuField.Size();
	int cRowsProcessEst = 0;
	int cRowsProcessActual = 0;
	int cRowsUpdatedEst = -1;
	int cRowsUpdatedActual = 0;
	int cRowsDone = 0;	// both processed and updated
	StrUni stuLimit;
	if (pszIdsTable != NULL)
		stuLimit.Format(L"JOIN %s b ON b.ObjId = a.Id ", pszIdsTable);
	for (istu = 0; istu < cField; ++istu)
	{
		iRow = 0;
		if (m_qadvi3)
		{
			if (m_fExactCount)
			{
				int cRowsThis;
				stuCmd.Format(L"SELECT COUNT(*) FROM %<0>s a %<1>s"
					L"WHERE a.%<2>s_Fmt IS NOT NULL",
					vstuClass[istu].Chars(), stuLimit.Chars(), vstuField[istu].Chars());
				CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(m_qodc->GetRowset(0));
				CheckHr(m_qodc->NextRow(&fMoreRows));
				CheckHr(m_qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cRowsThis),
							isizeof(int), &cbSpaceTaken, &fIsNull, 0));
				// Estimate of total number of rows for all classes:
				cRowsProcessEst = cField * ((cRowsThis + cRowsProcessActual) / (istu + 1));
			}
			else if (istu == 0)
				cRowsProcessEst = cField; // raw guess: one row per field
			else
				cRowsProcessEst = cField * (cRowsProcessActual / istu);
			if (cRowsUpdatedEst < 0)
				cRowsUpdatedEst = max(1, cRowsProcessEst / 10); // arbitrary
		}
		stuCmd.Format(L"SELECT a.Id, a.%<0>s_Fmt FROM %<1>s a %<2>s"
			L"WHERE a.%<0>s_Fmt IS NOT NULL",
			vstuField[istu].Chars(), vstuClass[istu].Chars(), stuLimit.Chars());
		CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(m_qodc->GetRowset(0));
		CheckHr(m_qodc->NextRow(&fMoreRows));
		vbFmt.Resize(1024);
		while (fMoreRows)
		{
			bool fFixBroken = false;
			CheckHr(m_qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hobj),
				isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(m_qodc->GetColValue(2, reinterpret_cast <BYTE *>(vbFmt.Begin()),
				vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
			cbFmt = cbSpaceTaken;
			if (cbFmt >= vbFmt.Size())
			{
				vbFmt.Resize(cbFmt + 1);
				CheckHr(m_qodc->GetColValue(2, reinterpret_cast <BYTE *>(vbFmt.Begin()),
					vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
				cbFmt = cbSpaceTaken;
			}
			else if (cbFmt == 0)
			{
				// This should never happen, but it has (see LT-6496).
				cbFmt = CreateDummyFmt(wsDefault, vbFmt.Begin(), vbFmt.Size());
				fFixBroken = true;
			}
			vbFmt.Resize(cbFmt);

			bool fModify = false;
			if (m_fProcessBytes)
			{
				fModify = ProcessBytes(vbFmt);
			}
			else
			{
				int cb = vbFmt.Size();
				CheckHr(qtpf->DeserializeRgPropsRgb(0, vbFmt.Begin(), &cb,
					&cpttp, NULL, NULL));
				vqttp.Resize(cpttp);
				vich.Resize(cpttp);
				cb = vbFmt.Size();
				CheckHr(qtpf->DeserializeRgPropsRgb(cpttp, vbFmt.Begin(), &cb,
					&cpttp, (ITsTextProps **)vqttp.Begin(), vich.Begin()));

				fModify = ProcessFormatting(vqttp);
			}
			if (fModify || fFixBroken)
			{
				// Mark the string for updating.
				vhobjFix.Push(hobj);

				if (m_fProcessBytes)
				{
					MergeRuns(vbFmt);
				}
				else
				{
					MergeRuns(vqttp, vich);
					cpttp = vqttp.Size();
					Assert(cpttp == vich.Size());
					int cbNeeded;
					HRESULT hr;
					CheckHr(hr = vqttp[0]->SerializeRgPropsRgb(cpttp,
						reinterpret_cast<ITsTextProps **>(vqttp.Begin()), vich.Begin(),
						vbFmt.Begin(), vbFmt.Size(), &cbNeeded));
					if (hr == S_FALSE)
					{
						vbFmt.Resize(cbNeeded);
						CheckHr(vqttp[0]->SerializeRgPropsRgb(cpttp,
							reinterpret_cast<ITsTextProps **>(vqttp.Begin()), vich.Begin(),
							vbFmt.Begin(), vbFmt.Size(), &cbNeeded));
					}
					vbFmt.Resize(cbNeeded);
				}
				vvbFmtFix.Push(vbFmt);
			}

			CheckHr(m_qodc->NextRow(&fMoreRows));

			for (int ipttp = 0; ipttp < vqttp.Size(); ipttp++)
				vqttp[ipttp] = NULL;

			if (m_qadvi3)
			{
				iRow++;
				nCurrPercent = (cRowsProcessEst + cRowsUpdatedEst) == 0 ?
					nCurrPercent :
					max(nCurrPercent,
						(((iRow + cRowsDone) * 100) / (cRowsProcessEst + cRowsUpdatedEst)));
				m_qadvi3->put_Position(nCurrPercent);
			}
		}
		if (m_qadvi3)
		{
			cRowsDone += iRow;
			cRowsProcessActual += iRow;
			cRowsUpdatedActual += vhobjFix.Size();
			cRowsUpdatedEst = cField * (cRowsUpdatedActual / (istu + 1));
		}
		ceFix = vhobjFix.Size();
		CreateCommand();
		for (ieFix = 0; ieFix < ceFix; ++ieFix)
		{
			stuCmd.Format(L"UPDATE %s SET %s_fmt=? WHERE [Id] = %d",
				vstuClass[istu].Chars(), vstuField[istu].Chars(), vhobjFix[ieFix]);
			// Set the parameter and execute the command.
			CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(vvbFmtFix[ieFix].Begin()),
				vvbFmtFix[ieFix].Size()));
			CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

			if (m_qadvi3)
			{
				nCurrPercent = (cRowsProcessEst + cRowsUpdatedEst) == 0 ?
					nCurrPercent :
					max(nCurrPercent,
						(((ieFix + cRowsDone) * 100) / (cRowsProcessEst + cRowsUpdatedEst)));
				m_qadvi3->put_Position(nCurrPercent);
			}
		}
		if (m_qadvi3)
			cRowsDone += ceFix;

		vhobjFix.Clear();
		vvbFmtFix.Clear();
		vqttp.Clear();
		vich.Clear();
		CreateCommand();
	}

	if (m_qadvi3)
		m_qadvi3->put_Position(100);

	if (fTrans)
	{
		m_qodc.Clear();		// Can't have a command open when we commit a transaction.
		CommitTrans();
	}
}

/*----------------------------------------------------------------------------------------------
	Begin a database transaction.
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::BeginTrans()
{
	CheckHr(m_qode->IsTransactionOpen(&m_fTransOpenAlready));
	if (!m_fTransOpenAlready)
		CheckHr(m_qode->BeginTrans());	// TODO: LOCK ACCESS TO DATABASE TO JUST ME?
}

/*----------------------------------------------------------------------------------------------
	Commit a database transaction.
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::CommitTrans()
{
	if (!m_fTransOpenAlready)
		CheckHr(m_qode->CommitTrans());	// TODO: UNLOCK ACCESS TO DATABASE TO JUST ME?
}

/*----------------------------------------------------------------------------------------------
	Create a new command object.
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::CreateCommand()
{
	CheckHr(m_qode->CreateCommand(&m_qodc));
}

/*----------------------------------------------------------------------------------------------
	After making the requested changes, merge identical runs.
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::MergeRuns(ComVector<ITsTextProps> & vpttp, Vector<int> & vich)
{
	for (int ittp = 0; ittp < vpttp.Size() - 1; ittp++)
	{
		if (vpttp[ittp] == vpttp[ittp + 1])
		{
			// Merge
			vpttp.Delete(ittp + 1, ittp + 2);
			vich.Delete(ittp + 1, ittp + 2);
			ittp--;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	After making the requested changes, merge identical runs.
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::MergeRuns(Vector<byte> & vbFmt)
{
	Assert(sizeof(int) == 4);
	Assert(vbFmt.Size() >= sizeof(int));

	int * pn = reinterpret_cast<int *>(vbFmt.Begin());
	int crun = *pn++;
	if (crun < 2)				// must have at least 2 runs to merge any of them!
		return;
								//  2 for char-min followed by prop offset
	int cbOffsets = isizeof(int) + (2 * crun * isizeof(int));
	Assert(vbFmt.Size() > cbOffsets);
	int irun;
	Vector<int> vibProp;
	Vector<int> vichTxt;
	Vector<int> vcbProp;
	vibProp.Resize(crun);
	vichTxt.Resize(crun);
	vcbProp.Resize(crun);
	// Get the offsets to each text run and property for that run.
	for (irun = 0; irun < crun; ++irun)
	{
		vichTxt[irun] = pn[2 * irun];
		vibProp[irun] = pn[2 * irun + 1];
	}
	// Calculate the size of each property.
	int itip;
	int ctip;
	int itsp;
	int ctsp;
	int scp;
	const byte * pbProp;
	const byte * pb;
	const byte * pbNext;
	const byte * pbLim = vbFmt.Begin() + vbFmt.Size();
	const byte * pbPropsMin = vbFmt.Begin() + cbOffsets;
	int stp;
	int cchTsp;
	int cbTsp;
	int cbProp;
	for (irun = 0; irun < crun; ++irun)
	{
		pbProp = pbPropsMin + vibProp[irun];
		pb = pbProp;
		ctip = *pb++;
		ctsp = *pb++;
		for (itip = 0; itip < ctip; ++itip)
		{
			scp = TextProps::DecodeScp(pb, pbLim - pb, &pbNext);
			if (!pbNext && scp == -1)
				break;
			switch (scp & 0x3)
			{
			case 0:		pb = pbNext + 1;	break;
			case 1:		pb = pbNext + 2;	break;
			case 2:		pb = pbNext + 4;	break;
			case 3:		pb = pbNext + 8;	break;
			}
		}
		for (itsp = 0; itsp < ctsp; ++itsp)
		{
			stp = TextProps::DecodeScp(pb, pbLim - pb, &pbNext);
			if (!pbNext && stp == -1)
				break;
			cchTsp = TextProps::DecodeCch(pbNext, pbLim - pbNext, &pbNext);
			if (!pbNext && cchTsp == -1)
				break;
			cbTsp = cchTsp * isizeof(wchar);
			pb = pbNext + cbTsp;
		}
		cbProp = pb - pbProp;
		vcbProp[irun] = cbProp;
		Assert(pb <= pbLim);
	}
	// Go through all the runs and make sure that we have the minimum number of properties
	// actually stored.
	int crunDel = 0;	// Mostly for debugging.
	int ibMin = 0;
	int ir;
	for (irun = 0; irun < crun; ++irun)
	{
		for (ir = irun + 1; ir < crun; ++ir)
		{
			if (vibProp[irun] == vibProp[ir])
				continue;				// Same property.
			if (vibProp[ir] < ibMin)
				continue;				// Property that's already been processed.
			if (vcbProp[irun] == vcbProp[ir] &&
				!memcmp(pbPropsMin + vibProp[irun], pbPropsMin + vibProp[ir], vcbProp[irun]))
			{
				// Aha!  These are the same and can be merged!
				// 1. Delete the range of (redundant) bytes from vbFmt.
				// 2. Adjust any larger vibProp[] values by subtracting vcbProp[ir], and
				//    any that are the same set to vibProp[irun].
				// 3. Adjust pbLim by subtracting vcbProp[ir].
				// 4. increment crunDel.
				int ibPropDel = vibProp[ir];
				vbFmt.Delete(cbOffsets + ibPropDel, cbOffsets + ibPropDel + vcbProp[ir]);
				// REVIEW: could this loop start at ir+1?
				for (int ir2 = 0; ir2 < crun; ++ir2)
				{
					if (vibProp[ir2] > ibPropDel)
						vibProp[ir2] -= vcbProp[ir];
					else if (vibProp[ir2] == ibPropDel)
						vibProp[ir2] = vibProp[irun];
				}
				pbPropsMin = vbFmt.Begin() + cbOffsets;
				pbLim = vbFmt.Begin() + vbFmt.Size();
				++crunDel;
			}
		}
		if (ibMin == vibProp[irun])
			ibMin += vcbProp[irun];
	}
	// Make sure that consecutive runs have different properties.
	int crunMerge = 0;
	for (irun = 1; irun < vichTxt.Size(); ++irun)
	{
		if (vibProp[irun-1] == vibProp[irun])
		{
			vibProp.Delete(irun);
			vichTxt.Delete(irun);
			vcbProp.Delete(irun);
			++crunMerge;
			--irun;
		}
	}
	if (crunMerge)
	{
		int crunNew = vichTxt.Size();
		int cbOffsetsNew = isizeof(int) + (2 * crunNew * isizeof(int));
		Assert(crunNew < crun);
		Assert(cbOffsetsNew < cbOffsets);
		// Store the number of runs, and the character and property offsets for each run.
		int * pnNew = reinterpret_cast<int *>(vbFmt.Begin());
		*pnNew++ = vichTxt.Size();
		for (irun = 0; irun < crunNew; ++irun)
		{
			pnNew[2 * irun] = vichTxt[irun];
			pnNew[2 * irun + 1] = vibProp[irun];
		}
		// Delete the excess space formerly used by character and property offsets.
		vbFmt.Delete(cbOffsetsNew, cbOffsets);
	}
}



// Struct for holding a row of a reference table.
// Hungarian: sd.
struct SrcDst
{
	HVO m_hvoSrc;
	HVO m_hvoDst;
};

// Struct for holding the overlay reference guids for an RnGenericRec object.
// Hungarian: rtg.
struct RecTagGuids
{
	HVO m_hvo;					// Database id of RnGenericRec object.
	Set<GUID> m_setguidTags;	// Set of overlay tag references in that record.
};

/*----------------------------------------------------------------------------------------------
	Extract any tag references that occur in this binary format block.

	@param rgbFmt
	@param cbFmt
	@param setguidTags
----------------------------------------------------------------------------------------------*/
static void ExtractTagGuids(byte * rgbFmt, int cbFmt, Set<GUID> & setguidTags)
{
	Assert(sizeof(int) == 4);
	int * pn = reinterpret_cast<int *>(rgbFmt);
	int crun = *pn++;
								//  2 for char-min followed by prop offset
	int cbOffsets = isizeof(int) + (2 * crun * isizeof(int));
	int irun;
	Vector<int> vibProp;
	vibProp.Resize(crun);
	for (irun = 0; irun < crun; ++irun)
		vibProp[irun] = pn[2 * irun + 1];

	int itip;
	int ctip;
	int itsp;
	int ctsp;
	int scp;
	const byte * pbProp;
	const byte * pb;
	const byte * pbNext;
	int stp;
	int cchTsp;
	int cbTsp;
	int cguid;
	int iguid;
	GUID * pguidTag;
	for (irun = 0; irun < crun; ++irun)
	{
		pbProp = rgbFmt + cbOffsets + vibProp[irun];
		pb = pbProp;
		ctip = *pb++;
		ctsp = *pb++;
		for (itip = 0; itip < ctip; ++itip)
		{
			scp = TextProps::DecodeScp(pb, (rgbFmt + cbFmt) - pb, &pbNext);
			if (!pbNext && scp == -1)
				break;
			switch (scp & 0x3)
			{
			case 0:
				pb = pbNext + 1;
				break;
			case 1:
				pb = pbNext + 2;
				break;
			case 2:
				pb = pbNext + 4;
				break;
			case 3:
				pb = pbNext + 8;
				break;
			}
		}
		for (itsp = 0; itsp < ctsp; ++itsp)
		{
			stp = TextProps::DecodeScp(pb, (rgbFmt + cbFmt) - pb, &pbNext);
			if (!pbNext && stp == -1)
				break;
			cchTsp = TextProps::DecodeCch(pbNext, (rgbFmt + cbFmt) - pbNext, &pbNext);
			if (!pbNext && cchTsp == -1)
				break;
			cbTsp = cchTsp * isizeof(wchar);
			pb = pbNext + cbTsp;
			if (stp == kstpTags)
			{
				cguid = cbTsp / isizeof(GUID);
				Assert(cbTsp == cguid * isizeof(GUID));
				for (iguid = 0; iguid < cguid; ++iguid)
				{
					pguidTag = reinterpret_cast<GUID *>(const_cast<byte *>(pbNext));
					setguidTags.Insert(*pguidTag);
					pbNext += isizeof(GUID);
				}
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Generate the PhraseTags entries in the given table for the given record(s).

	@param podc Pointer to an open OLEDB command object.
	@param pszClass Name of the class owning the PhraseTags table to update in the database.
	@param pszField Name of the field of PhraseTags to update in the database.
	@param hmguidhvoPss Map from GUID to database ids for CmPossibility objects used in tags.
	@param hvo Database id of record for which to generate, or 0 to generate for all records.
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::FillPhraseTagsTable(IOleDbCommand * podc, const OLECHAR * pszClass,
	const OLECHAR * pszField, HashMap<GUID,HVO> & hmguidhvoPss, HVO hvo)
{
	AssertPtr(podc);
	AssertPsz(pszClass);
	AssertPsz(pszField);

	try
	{
		StrUni stuCmd;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		// Get any existing phrase tags.
		stuCmd.Format(L"SELECT Src,Dst FROM %s_%s",
			pszClass, pszField);
		if (hvo)
			stuCmd.FormatAppend(L" WHERE Src = %d", hvo);
		stuCmd.Append(L" ORDER BY Src,Dst");
		CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(podc->GetRowset(0));
		CheckHr(podc->NextRow(&fMoreRows));
		Vector<SrcDst> vsdTags;
		SrcDst sd;
		while (fMoreRows)
		{
			CheckHr(podc->GetColValue(1, reinterpret_cast<BYTE *>(&sd.m_hvoSrc),
				sizeof(sd.m_hvoSrc), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				CheckHr(podc->GetColValue(2, reinterpret_cast<BYTE *>(&sd.m_hvoDst),
					sizeof(sd.m_hvoDst), &cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
					vsdTags.Push(sd);
			}
			CheckHr(podc->NextRow(&fMoreRows));
		}
		// Get the format strings that are part of this object
		stuCmd.Format(L"SELECT co.Owner$,s.Contents_Fmt FROM StTxtPara s%n"
			L"    JOIN StText_Paragraphs stp ON stp.Dst = s.[id]%n"
			L"    JOIN StText st ON st.[id] = stp.Src%n"
			L"    JOIN CmObject co ON co.[Id] = st.[id]%n"
			L"WHERE co.Owner$ ");
		if (hvo)
			stuCmd.FormatAppend(L"= %d", hvo);
		else
			stuCmd.FormatAppend(L"IN (SELECT [Id] FROM %s)", pszClass);
		stuCmd.FormatAppend(L" AND%n"
			L"      s.Contents_Fmt IS NOT NULL%n");
		CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(podc->GetRowset(0));
		CheckHr(podc->NextRow(&fMoreRows));
		HVO hvo;
		Vector<byte> vbFmt;
		vbFmt.Resize(512);
		Vector<RecTagGuids> vrtg;
		RecTagGuids rtg;
		// Get a list of overlay (CmPossibility) guids from the format strings.
		while (fMoreRows)
		{
			CheckHr(podc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo),
				&cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				CheckHr(podc->GetColValue(2, reinterpret_cast<BYTE *>(vbFmt.Begin()),
					vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull && cbSpaceTaken > (ULONG)vbFmt.Size())
				{
					vbFmt.Resize(cbSpaceTaken);
					CheckHr(podc->GetColValue(2, reinterpret_cast<BYTE *>(vbFmt.Begin()),
						vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
				}
				if (!fIsNull)
				{
					// First, find the proper set of guids for this record, or create it if
					// necessary.
					int iv;
					int ivLim;
					for (iv = 0, ivLim = vrtg.Size(); iv < ivLim; )
					{
						int ivMid = (iv + ivLim) / 2;
						if (vrtg[ivMid].m_hvo < hvo)
							iv = ivMid + 1;
						else
							ivLim = ivMid;
					}
					Assert(iv <= vrtg.Size());
					if (iv == vrtg.Size() || vrtg[iv].m_hvo != hvo)
					{
						rtg.m_hvo = hvo;
						rtg.m_setguidTags.Clear();
						vrtg.Insert(iv, rtg);
					}
					ExtractTagGuids(vbFmt.Begin(), cbSpaceTaken, vrtg[iv].m_setguidTags);
				}
			}
			CheckHr(podc->NextRow(&fMoreRows));
		}
		int irtg;
		Set<GUID>::iterator it;
		GUID guid;
		Vector<SrcDst> vsdNewTags;
		// Get a list of CmPossibility ids corresponding to the list of guids.
		for (irtg = 0; irtg < vrtg.Size(); ++irtg)
		{
			if (vrtg[irtg].m_setguidTags.Size() == 0)
				continue;
			sd.m_hvoSrc = vrtg[irtg].m_hvo;
			for (it = vrtg[irtg].m_setguidTags.Begin();
				it != vrtg[irtg].m_setguidTags.End();
				++it)
			{
				guid = it->GetValue();
				if (!hmguidhvoPss.Retrieve(guid, &sd.m_hvoDst))
				{
					stuCmd.Format(
						L"SELECT [Id] FROM CmObject WHERE Guid$ = '%g'",
						&guid);
					CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
					CheckHr(podc->GetRowset(0));
					CheckHr(podc->NextRow(&fMoreRows));
					if (fMoreRows)
					{
						CheckHr(podc->GetColValue(1, reinterpret_cast<BYTE *>(&sd.m_hvoDst),
							sizeof(sd.m_hvoDst), &cbSpaceTaken, &fIsNull, 0));
						if (fIsNull)
							sd.m_hvoDst = 0;
						else
							hmguidhvoPss.Insert(guid, sd.m_hvoDst);
					}
					else
					{
						sd.m_hvoDst = 0;
					}
				}
				if (sd.m_hvoDst)
				{
					int iv;
					int ivLim;
					for (iv = 0, ivLim = vsdTags.Size(); iv < ivLim; )
					{
						int ivMid = (iv + ivLim) / 2;
						if (vsdTags[ivMid].m_hvoSrc < sd.m_hvoSrc ||
							(vsdTags[ivMid].m_hvoSrc == sd.m_hvoSrc &&
							vsdTags[ivMid].m_hvoDst < sd.m_hvoDst))
						{
							iv = ivMid + 1;
						}
						else
						{
							ivLim = ivMid;
						}
					}
					Assert(iv <= vsdTags.Size());
					if (iv == vsdTags.Size() || vsdTags[iv].m_hvoSrc != sd.m_hvoSrc ||
						vsdTags[iv].m_hvoDst != sd.m_hvoDst)
					{
						vsdTags.Insert(iv, sd);
						vsdNewTags.Push(sd);
					}
				}
				else
				{
					// ERROR MESSAGE?
				}
			}
		}
		int csd = vsdNewTags.Size();
		if (csd)
		{
			// Store the list of ids in the designated properties.
			const int kcsdInc = 50;
			int isd;
			int isdLim = kcsdInc;
			if (isdLim > csd)
				isdLim = csd;
			for (isd = 0; isd < csd; )
			{
				stuCmd.Clear();
				for (; isd < isdLim; ++isd)
				{
					stuCmd.FormatAppend(L"INSERT INTO %s_%s (Src,Dst) VALUES (%d,%d);%n",
						pszClass, pszField, vsdNewTags[isd].m_hvoSrc, vsdNewTags[isd].m_hvoDst);
				}
				CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
				isdLim += kcsdInc;
				if (isdLim > csd)
					isdLim = csd;
			}
		}
	}
	catch (Throwable & thr)
	{
		// ERROR MESSAGE?
		thr;
	}
	catch (...)
	{
	}
}

/*----------------------------------------------------------------------------------------------
	Update the PhraseTags table according to the given overlay tag object and
	selection (paragraph) objects.

	@param flidTable Database field id of the property stored as a PhraseTags table.
	@param fInsert true if overlay tag being added, false if it is being removed.
	@param pode Pointer to the database object.
	@param hvoTag Database id of the overlay tag.
	@param hvoEnd Database id of the top level object (StText?) of the selection end point.
	@param hvoAnchor Database id of the top level object (StText?) of the selection anchor
					point.
----------------------------------------------------------------------------------------------*/
void DbStringCrawler::UpdatePhraseTagsTable(int flidTable, bool fInsert, IOleDbEncap * pode, HVO hvoTag,
	HVO hvoEnd, HVO hvoAnchor)
{
	AssertPtr(pode);
	Assert(hvoTag);
	Assert(hvoEnd);
	Assert(hvoAnchor);

	StrUni stuClass;
	StrUni stuField;

	IFwMetaDataCachePtr qmdc;
	qmdc.CreateInstance(CLSID_FwMetaDataCache);
	AssertPtr(qmdc);
	CheckHr(qmdc->Init(pode));

	SmartBstr sbstrClass;
	SmartBstr sbstrField;
	CheckHr(qmdc->GetOwnClsName(flidTable, &sbstrClass));
	CheckHr(qmdc->GetFieldName(flidTable, &sbstrField));

	IOleDbCommandPtr qodc;
	CheckHr(pode->CreateCommand(&qodc));
	StrUni stuCmd;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;

	// Get the record id(s) from the selection object ids.
	Vector<HVO> vhvoRec;
	HVO hvo;
	stuCmd.Format(L"SELECT DISTINCT [Id] FROM %s%n"
		L"WHERE [Id] IN (SELECT Owner$ FROM CmObject WHERE [Id] IN (%d,%d))",
		sbstrClass.Chars(), hvoEnd, hvoAnchor);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo),
			&cbSpaceTaken, &fIsNull, 0));
		if (!fIsNull)
			vhvoRec.Push(hvo);
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	if (fInsert)
	{
		// Insert (hvoRec,hvoTag) into the table if it is not already there.
		bool fMissing;
		for (int ihvo = 0; ihvo < vhvoRec.Size(); ++ihvo)
		{
			fMissing = true;
			stuCmd.Format(L"SELECT Src, Dst FROM %s_%s"
				L" WHERE Src = %d AND Dst = %d",
				sbstrClass.Chars(), sbstrField.Chars(), vhvoRec[ihvo], hvoTag);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo),
					&cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
				{
					Assert(hvo == vhvoRec[ihvo]);
					CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo),
						&cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull)
					{
						Assert(hvo == hvoTag);
						fMissing = false;
					}
				}
			}
			if (fMissing)
			{
				stuCmd.Format(L"INSERT INTO %s_%s (Src,Dst) VALUES (%d,%d)",
					sbstrClass.Chars(), sbstrField.Chars(), vhvoRec[ihvo], hvoTag);
				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			}
		}
	}
	else
	{
		// Regenerate the table contents for the given record(s).
		HashMap<GUID,HVO> hmguidhvoPss;
		for (int ihvo = 0; ihvo < vhvoRec.Size(); ++ihvo)
		{
			stuCmd.Format(L"DELETE FROM %s_%s WHERE Src = %d",
				sbstrClass.Chars(), sbstrField.Chars(), vhvoRec[ihvo]);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			DbStringCrawler::FillPhraseTagsTable(qodc, sbstrClass.Chars(), sbstrField.Chars(),
				hmguidhvoPss, vhvoRec[ihvo]);
		}
	}
}


#include "Vector_i.cpp"
#include "Set_i.cpp"
#include "HashMap_i.cpp" // Need for release build

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\MkCustomNb.bat"
// End: (These 4 lines are useful to Steve McConnel.)
