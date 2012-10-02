/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: OleDbEncap.h
Responsibility: Paul Panek
Last reviewed: Not yet.

Description:
	Header file for the OleDbEncap data access module.

	This file contains class declarations for the following classes:
		FwMetaFieldRec - Used by the FwMetaDataCache; contains info for a particular field.
		FwMetaClassRec - Used by the FwMetaDataCache; contains info for a particular class.
		FwMetaDataCache - Caches and provides FieldWorks metadata information.
		OleDbCommand - Allows for an SQL command to be executed and data to be retrieved.
		OleDbEncap - Establishes a database connection and session.  Allows for a database
			transaction to be opened, committed, or rolled back and for savepoints to be
			set and rolled back to.  Allows for the creation of OleDbCommand objects.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef __OLEDBENCAP_H__
#define __OLEDBENCAP_H__

//:> OLE DB Header
#include "oledb.h"
//:> OLE DB Errors
#include "oledberr.h"

//:> OLE DB Service Component header.
#include "msdasc.h"
//:> OLE DB Root Enumerator.
#include "msdaguid.h"
//:> MSDASQL - Default OLE DB Provider.
#include "msdasql.h"

//:> SQL Server specific header. (Now in .cpp file).
//#include "sqloledb.h"
//:> SQL Server specific header.
#pragma warning(push)
#pragma warning(disable : 4268)	// Prevents spurious warnings about initialisation if IIDs.
#include "sqloledb.h"
#pragma warning(pop)
#undef DBINITCONSTANTS

#include <stdio.h>
#include <comdef.h>
#include <objbase.h>

// Size of buffer used to read through a BLOB if the allocated buffer is too small.
const int kcBlobBufferSize = 1024;
// Start index for Rowsets in a CommandRec.
const ULONG kluRowsetStartIndex = 1;
// RoundUp value used to properly byte align values in buffers.
const ULONG kluRoundupAmount = 8;
// Maximum number of parameters per SQL command.
const int knMaxParamPerCommand = 11;
// Value indicating that the default number of rows should be pulled in from the server to the
// data buffer at one time.
const int knRowsetBufferDefaultRows = 0;
// Value indicating that a maximum number of rows should be pulled in from the server to the
// data buffer at one time.
const int knRowsetBufferMaxRows = -1;
// First part of a standard SavePoint name.  The second part is a non-negative integer.
const OLECHAR kwchSavePointName[] = L"SP";

// Database-related error types.
enum DbErrTypes
{
	kdberrGeneral,
	kdberrCommLink,
	kdberrResources,
	kdberrIntegrity,
	kdberrHardware,
	kdberrTruncate,
	kdberrLockTimeout
};

typedef ComSmartPtr<IAccessor> IAccessorPtr;
typedef ComSmartPtr<IColumnsInfo> IColumnsInfoPtr;
typedef ComSmartPtr<ICommandText> ICommandTextPtr;
typedef ComSmartPtr<ICommandProperties> ICommandPropertiesPtr;
typedef ComSmartPtr<ICommandWithParameters> ICommandWithParametersPtr;
typedef ComSmartPtr<IDBCreateCommand> IDBCreateCommandPtr;
typedef ComSmartPtr<IDataInitialize> IDataInitializePtr;
typedef ComSmartPtr<IDBInitialize> IDBInitializePtr;
typedef ComSmartPtr<IDBCreateCommand> IDBCreateCommandPtr;
typedef ComSmartPtr<IDBCreateSession> IDBCreateSessionPtr;
typedef ComSmartPtr<IMultipleResults> IMultipleResultsPtr;
typedef ComSmartPtr<IOleDbEncap> IOleDbEncapPtr;
typedef ComSmartPtr<IRowset> IRowsetPtr;
typedef ComSmartPtr<ITransactionLocal> ITransactionLocalPtr;
typedef ComSmartPtr<IUnknown> IUnknownPtr;

//:> Smart pointers to error interfaces.
typedef ComSmartPtr<ICreateErrorInfo> ICreateErrorInfoPtr;
typedef ComSmartPtr<IErrorInfo> IErrorInfoPtr;
typedef ComSmartPtr<ISupportErrorInfo> ISupportErrorInfoPtr;

typedef Vector<ULONG> FldIdVec; // Hungarian vu

/*----------------------------------------------------------------------------------------------
	Used by FwMetaDataCache to hold FieldWorks metadata information on fields.
	This is used to cache field definition information for fast access.
	These records are accessible from the FwMetaDataCache object using the ID of the field.

	Example data:	@code{@b{
	Id          Type        Class       DstCls      Name}
	----------- ----------- ----------- ----------- --------------
	1001		16          1           NULL        Name
	1002        5           1           NULL        TimeCreated
	1003        25          1           2           Folders
	2001        16          2           NULL        Name}
	@h3{Hungarian: mfr}
----------------------------------------------------------------------------------------------*/
class FwMetaFieldRec
{
public:
	// Field type.  This value is defined in ~FWROOT\src\Cellar\lib\CmTypes.h
	int m_iType;
	// Class id ("Id" column in the field$ database table)
	ULONG m_luOwnClsid;
	// Destination class id ("DstCls" column in the field$ database table)
	ULONG m_luDstClsid;
	// Name of the field. (i.e. property name)
	StrUni m_stuFieldName;
	// Label of the field (i.e. property UserLabel)
	StrUni m_stuFieldLabel;
	// HelpString of the field (i.e. property HelpString)
	StrUni m_stuFieldHelp;
	// ListRootId of the field (i.e. property ListRootId)
	int m_iFieldListRoot;
	// WsSelector of the field (i.e. property WsSelector)
	int m_iFieldWs;
	// XmlUI of the field (i.e. property XmlUI)
	StrUni m_stuFieldXml;

	FwMetaFieldRec::FwMetaFieldRec()
	{
		m_iType = 0;
		m_luOwnClsid = 0;
		m_luDstClsid = 0;
		m_iFieldListRoot = 0;
		m_iFieldWs = 0;
	}

	FwMetaFieldRec::~FwMetaFieldRec()
	{
	}
};

/*----------------------------------------------------------------------------------------------
	Used by the FwMetaClassRec to hold a list of the fields of the class.
	@h3{Hungarian: mfl}
----------------------------------------------------------------------------------------------*/
class FwMetaFieldList : public GenRefObj
{
public:
	FldIdVec m_vuFields;
};
typedef GenSmartPtr<FwMetaFieldList> FwMetaFieldListPtr;


/*----------------------------------------------------------------------------------------------
	Used by the FwMetaDataCache to hold FieldWorks metadata information on classes.
	This is used to cache class definition information for fast access.
	These records are accessible from the FwMetaDataCache object using the ID of the class.

	Example data: @code{@b{
	Id          Base        Abstract Name}
	----------- ----------- -------- --------------------
	0           0           1        CmObject
	1           0           1        CmProject
	2           0           0        CmFolder
	3           0           0        CmFolderObject
	5           0           1        CmMajorObject
	7           0           0        CmPossibility}
	@h3{Hungarian: mcr}
----------------------------------------------------------------------------------------------*/
class FwMetaClassRec
{
public:
	// Class id for the base of this class (CmObject = 0)
	ULONG m_luBaseClsid;
	// Set to "true" if the class is abstract.
	ComBool m_fAbstract;
	// Name of the class (which is the same as the table name in the database).
	StrUni m_stuClassName;

	FwMetaFieldListPtr m_qmfl;
	FldIdVec m_vuDirectSubclasses;

	FwMetaClassRec::FwMetaClassRec()
	{
	}

	FwMetaClassRec::~FwMetaClassRec()
	{
	}
};


/*----------------------------------------------------------------------------------------------
	Standard implementation of the IFwMetaDataCache interface.

	Cross Reference: ${IFwMetaDataCache}
----------------------------------------------------------------------------------------------*/
class FwMetaDataCache : public IFwMetaDataCache
{
protected:
	long m_cref;
	IOleDbEncapPtr m_qode;
	HashMap<ULONG, FwMetaFieldRec> m_hmmfr;
	HashMap<ULONG, FwMetaClassRec> m_hmmcr;
	HashMapStrUni<ULONG> m_hmsulNameToClid;
	HashMapStrUni<ULONG> m_hmsulNameToFlid; // First two chars of name are binary Clid

public:
	FwMetaDataCache();
	~FwMetaDataCache();

	STDMETHOD_(ULONG, AddRef)(void);
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	STDMETHOD(Init)(IOleDbEncap * pode);
	STDMETHOD(Reload)(IOleDbEncap * pode, ComBool fKeepVirtuals);
	STDMETHOD(InitXml)(BSTR bstrPathname, ComBool fClearPrevCache);

	//:>****************************************************************************************
	//:> Field access methods
	//:>****************************************************************************************
	STDMETHOD(get_FieldCount)(int * pcflid);
	STDMETHOD(GetFieldIds)(int cflid, ULONG * rgflid);
	STDMETHOD(GetOwnClsName)(ULONG luFlid, BSTR * pbstrOwnClsName);
	STDMETHOD(GetDstClsName)(ULONG luFlid, BSTR * pbstrDstClsName);
	STDMETHOD(GetOwnClsId)(ULONG luFlid, ULONG * pluOwnClsid);
	STDMETHOD(GetDstClsId)(ULONG luFlid, ULONG * pluDstClsid);
	STDMETHOD(GetFieldName)(ULONG luFlid, BSTR * pbstrFieldName);
	STDMETHOD(GetFieldNameOrNull)(ULONG luFlid, BSTR * pbstrFieldName);
	STDMETHOD(GetFieldType)(ULONG luFlid, int * piType);
	STDMETHOD(GetFieldLabel)(ULONG luFlid, BSTR * pbstrFieldLabel);
	STDMETHOD(GetFieldHelp)(ULONG luFlid, BSTR * pbstrFieldHelp);
	STDMETHOD(GetFieldListRoot)(ULONG luFlid, int * piListRoot);
	STDMETHOD(GetFieldWs)(ULONG luFlid, int * piWs);
	STDMETHOD(GetFieldXml)(ULONG luFlid, BSTR * pbstrFieldXml);
	STDMETHOD(get_IsValidClass)(ULONG luFlid, ULONG luClid, ComBool * pfValid);

	//:>****************************************************************************************
	//:> Class access methods
	//:>****************************************************************************************
	STDMETHOD(get_ClassCount)(int * pcclid);
	STDMETHOD(GetClassIds)(int cclid, ULONG * rgclid);
	STDMETHOD(GetClassName)(ULONG luClid, BSTR * pbstrClassName);
	STDMETHOD(GetAbstract)(ULONG luClid, ComBool * pfAbstract);
	STDMETHOD(GetBaseClsId)(ULONG luClid, ULONG * pluClid);
	STDMETHOD(GetBaseClsName)(ULONG luClid, BSTR * pbstrBaseClsName);
	STDMETHOD(GetFields)(ULONG luClid, ComBool fIncludeSuperclasses, int grfcpt,
		int cflidMax, ULONG * prgflid, int * pcflid);

	//:>****************************************************************************************
	//:> Reverse (by name) access methods
	//:>****************************************************************************************
	STDMETHOD(GetClassId)(BSTR bstrName, ULONG * pluClid);
	STDMETHOD(GetFieldId)(BSTR bstrClassName, BSTR bstrFieldName, ComBool fIncludeBaseClasses,
		ULONG * pluFlid);
	STDMETHOD(GetFieldId2)(ULONG luClid, BSTR bstrFieldName, ComBool fIncludeBaseClasses,
		ULONG * pluFlid);
	STDMETHOD(GetAllSubclasses)(ULONG luClid, int cluMax, int * pcluOut,
		ULONG * prgluSubclasses);
	STDMETHOD(GetDirectSubclasses)(ULONG luClid, int cluMax, int * pcluOut,
		ULONG * prgluSubclasses);
	//:>****************************************************************************************
	//:> virtual property support
	//:>****************************************************************************************
	STDMETHOD(AddVirtualProp)(BSTR bstrClass, BSTR bstrField, ULONG luFlid, int type);
	STDMETHOD(get_IsVirtual)(ULONG luFlid, ComBool * pfVirtual);
	void InsertCmObjectFields();
	void BuildSubClassInfo();
};

/*----------------------------------------------------------------------------------------------
	Standard implementation of the IOleDbEncap interface.

	Cross Reference: ${IOleDbEncap}
----------------------------------------------------------------------------------------------*/
class OleDbEncap : public IOleDbEncap, IUndoGrouper
{
protected:
	long m_cref;

private:
	ITransactionLocalPtr m_qtranloc;
	IDBInitializePtr m_qdbi;
	IDBInitializePtr m_qunkDBInitialize;
	IUnknownPtr m_qunkSession;
	bool m_fTransactionOpen;
	int m_nSavePointLevel;
	SmartBstr m_sbstrDatabase;
	SmartBstr m_sbstrServer;
	IStreamPtr m_qfistLog;		// Pointer to logging stream.
	OdeLockTimeoutMode m_olt;	// Lock timeout mode for this connection.
	int m_nmsTimeout;			// Lock timeout period in milliseconds.
	bool m_fInitialized;

public:
	OleDbEncap();
	~OleDbEncap();

	STDMETHOD_(ULONG, AddRef)(void);
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	// IOleDbEncap

	STDMETHOD(BeginTrans)(void);
	STDMETHOD(CommitTrans)(void);
	STDMETHOD(CreateCommand)(IOleDbCommand ** ppodc);
	STDMETHOD(Init)(BSTR bstrServer, BSTR bstrDatabase, IStream * pfistLog,
		OdeLockTimeoutMode olt, int nmsTimeout);
	STDMETHOD(IsTransactionOpen)(ComBool * pfTransactionOpen);
	STDMETHOD(RollbackTrans)(void);
	STDMETHOD(RollbackSavePoint)(BSTR bstrSavePoint);
	STDMETHOD(SetSavePoint)(BSTR * pbstr);
	STDMETHOD(SetSavePointOrBeginTrans)(BSTR * pbstr);
	STDMETHOD(InitMSDE)(IStream * pfistLog, ComBool fForce);
	STDMETHOD(get_Server)(BSTR * pbstrSvr);
	STDMETHOD(get_Database)(BSTR * pbstrDb);
	STDMETHOD(GetFreeLogKb)(int nReservespace, int * pnKbFree);
	STDMETHOD(Reinit)(void);
	STDMETHOD(GetSession)(IUnknown ** ppunkSession);

	// IUndoGrouper
	STDMETHOD(BeginGroup)(int * phandle);
	STDMETHOD(EndGroup)(int handle);
	STDMETHOD(CancelGroup)(int handle);


protected:
	bool StartMSDE();
	StrUni GetBadDataReport();
	void OleDbEncap::SearchForBadData(StrUni strMask, StrUni & strOutput, bool & fFoundFirstFile,
		StrUni strDataDir);
	bool StopSqlServer();
	bool InitMSDE_GetRegKey(HKEY & hk, bool & fInitMsdeFlagRead, DWORD &dwInitMsde);
	bool InitMSDE_CreateSession(HRESULT & hr, IDBCreateCommandPtr & qdcc, ICommandTextPtr & qcdt);
	HRESULT InitMSDE_SetupMiscDBStuff(ICommandTextPtr qcdt);
	HRESULT InitMSDE_Execute(ICommandTextPtr qcdt, StrUni stuSQL);
	Vector<StrApp> InitMSDE_FindDBs(HKEY hk, StrAppBufPath & strRootDir);

	HRESULT InitMSDE_GetAttachedDBs(IDBCreateCommandPtr qdcc, Vector<StrApp> vstr,
		Vector<StrApp> & vstrAttached);
	HRESULT InitMSDE_DetachDBs(IDBCreateCommandPtr qdcc,Vector<StrApp> vstr,
		Vector<StrApp> vstrAttached);
	HRESULT InitMSDE_AttachDBFiles(IDBCreateCommandPtr qdcc,
		Vector<StrApp> vstr, StrAppBufPath strRootDir);
	void InitMSDE_Finish(bool fInitMsdeFlagRead, HRESULT hr,
		DWORD dwInitMsde, ComBool fForce, HKEY hk);

	void LogError(HRESULT hr, StrAnsi methodAndCmd, StrAnsi info);
	void LogError(StrAnsi msg);
	void LogError(StrAnsi msg, HRESULT hr, StrAnsi param);

	StrUni GetCreateGetFWDBsSQLString();
	StrUni GetCreateDbStartupSQLString();
	HRESULT SetUpErrorInfo(int rid);

	// This class handles permitting remote machines to issue us with a warning to disconnect
	// from their database:
	class RemoteConnectionMonitor
	{
	public:
		RemoteConnectionMonitor();
		~RemoteConnectionMonitor();
		bool NewConnection(BSTR bstrServer);
		bool TerminatingConnection();

	protected:
		HANDLE m_hFileMapping; // Handle for shared memory - count of remote connections.
		HANDLE m_hMutex; // Handle for mutual exclusively accessing shared memory.
		bool m_fRemote; // True if this connection is to a remote computer.
		bool m_fConnectionNoted; // True if ${NewConnection} returned OK.
		static const achar * m_pszMutexName;
		static const achar * m_pszFileMappingName;
		void PermitRemoteWarnings();
		void RefuseRemoteWarnings();
	};
	RemoteConnectionMonitor m_rmcm;

private:
	inline HRESULT XCheckHr(HRESULT hr);
	inline HRESULT XCheckMemory(HRESULT hr, void * pv);
//	friend HRESULT OleDbCommand::Init(IUnknown * punkSession, IStream * pfistLog);
};


/*----------------------------------------------------------------------------------------------
	Standard implementation of the IOleDbCommand interface.

	Cross Reference: ${IOleDbCommand}
----------------------------------------------------------------------------------------------*/

class OleDbCommand : public IOleDbCommand
{
private:
	IUnknownPtr m_qunkCommand;
	//:>  ENHANCE PaulP:  Should probably make parameters and parameters sets, COM objects as well.
	// Used for Commands with parameters.  By default, it is NULL.
	DBPARAMS m_dbpParams;
	//  Array of pointers to parameter data  (we don't know what kind it will be till later).
	void * m_rgParamData[knMaxParamPerCommand];
	// Number of bytes that each element of m_rgParamData takes up.
	ULONG m_rgluParamDataSize[knMaxParamPerCommand];
	DBBINDING m_rgdbbParamBindings[knMaxParamPerCommand];
	DBPARAMBINDINFO m_rgdbpbi[knMaxParamPerCommand];
	ULONG m_cluParameters;
	IMultipleResultsPtr m_qmres;
	// When we execute a command with parameters, we create an accessor for them and put it in
	// m_dbpParams.hAccessor. When we are done with the command we must release it. To do so
	// we have to keep a pointer to the object that created it.
	IAccessorPtr m_qacc;
	// After a SQL command is executed, we hold this smart pointer to an IRowset interface.
	// If you try to execute a new SQL command without releasing this IRowset, the execute
	// command will fail. There isn't any obvious place where we can automatically release this
	// IRowset since the user is free to get next or previous rows as long as they want.
	// When you execute a new command on the same OleDbCommand, we automatically release
	// the old information so it works fine. However, if you have a smart pointer on
	// IOleDbCommand (qodc) and then want to use a different IOleDbCommand (qodc1), you will
	// get a failure unless you first release the smart pointer on qodc (qodc.Clear()).
	IUnknownPtr m_qunkRowset;
	void * m_pRowData;
	ULONG m_luRowSize;
	DBBINDING * m_rgBindings;
	long m_cColumns;
	HROW * m_rghRows;
	int m_cRows;
	// This value is changed each time a new rowset is retrieved (it is one-based).
	int m_iIndex;
	// Indicates if the last column value retrieved was null.
	ComBool m_fLastColWasNull;
	IStreamPtr m_qfistLog;	// Pointer to logging stream.
	OdeLockTimeoutMode m_oltCmd;	// Lock timeout mode.
	StrUni m_stuCommand; // text passed to ExecCommand
	IUnknownPtr m_qunkOde;

protected:
	long m_cref;
	ComSmartPtr<ISequentialStream> m_qsst;	// Pointer for application to get rest of BLOB.

public:
	OleDbCommand();
	~OleDbCommand();

	STDMETHOD_(ULONG, AddRef)(void);
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	STDMETHOD(ColValWasNull)(int * pfIsNull);
	STDMETHOD(ExecCommand)(BSTR bstrSqlStatement, int nStatementType);
	STDMETHOD(GetColValue)(ULONG iluColIndex, BYTE * prgbDataBuffer,
		ULONG cluBufferLength, ULONG * pluAmtBuffUsed, ComBool * pfIsNull, int cbPad);
	STDMETHOD(GetInt)(int iColIndex, int * pnValue);
	STDMETHOD(GetParameter)(ULONG iluColIndex, BYTE * prgbDataBuffer,
		ULONG cluBufferLength, ComBool * fIsNull);
	STDMETHOD(GetRowset)(int nRowsBuffered);
	STDMETHOD(NextRow)(ComBool * pfMoreRows);
	STDMETHOD(SetParameter)(ULONG iluParamIndex, DWORD dwFlags, BSTR bstrParamName,
		WORD nDataType, ULONG * prgluDataBuffer, ULONG cluBufferLength);
	STDMETHOD(SetByteBuffParameter)(ULONG iluParamIndex, DWORD dwFlags, BSTR bstrParamName,
		BYTE * prgbDataBuffer, ULONG cbBufferLength);
	STDMETHOD(SetStringParameter)(ULONG iluParamIndex, DWORD dwFlags, BSTR bstrParamName,
		OLECHAR * prgchDataBuffer, ULONG cbBufferLength);
	STDMETHOD(ReleaseExceptParams)();


protected:
	STDMETHOD(Init)(IUnknown * punkOde, IStream * pfistLog);
	void InitTimeoutMode(OdeLockTimeoutMode olt);
	friend HRESULT OleDbEncap::CreateCommand(IOleDbCommand ** ppodc);
	HRESULT FullErrorCheck(HRESULT hr, IUnknown * punk, REFIID iid);

private:
	void AddProperty(DBPROP * pProp, DBPROPID dwPropertyID, VARTYPE vtType = VT_BOOL,
		LONG lValue = VARIANT_TRUE, DBPROPOPTIONS dwOptions = DBPROPOPTIONS_OPTIONAL);
	void CloseCommand();
	void CloseCommandExceptParams();
	HRESULT SetUpFullErrorInfo(int ierr, const OLECHAR * pszInfo, IErrorInfo ** ppei);
	HRESULT Execute1(REFIID IID_Interface, DBPARAMS * pParams, DBROWCOUNT * pcRows,
					   IUnknown ** ppunk, const ICommandTextPtr& qcdt);
};


#endif   // __OLEDBENCAP_H__
