/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ZipInvoke.h
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	Header file to interface between Backup system and the Xceed Zip module.
-------------------------------------------------------------------------------*//*:End Ignore*/

#pragma once
#ifndef ZIPINVOKE_H_INCLUDED
#define ZIPINVOKE_H_INCLUDED


/*----------------------------------------------------------------------------------------------
	This struct stores the data that will go onto the backup zip file under the filename
	"header". It contains things like the memory jog for the password, to assist the user
	during a restore operation. Data can be added to this without compromising previously saved
	backups, AS LONG AS it is added at the end of the struct (before the methods will do).

	@h3{Hungarian: zipfh}
----------------------------------------------------------------------------------------------*/
struct ZipFileHeader
{
	enum { knMemJogSize = 255, knDatabaseSize = 255, };
	int m_nStructSize; // Enables this structure to be added to.
	bool m_fPasswordProtected; // True if backup zip file is password protected
	bool m_fMemoryJogUsed; // True if user bothered to use a password memory jog
	OLECHAR rgchMemoryJog[knMemJogSize]; // String containing user's password memory jog
	OLECHAR rgchDatabase[knDatabaseSize]; // String containing name of database backed up

	ZipFileHeader();
	bool SetMemoryJog(SmartBstr sbstr);
	void GetMemoryJog(BSTR * pbstr);
	void ClearMemoryJog();
	bool SetDBName(SmartBstr sbstr);
	void GetDBName(BSTR * pbstr);
	void ClearDBName();
};

/*----------------------------------------------------------------------------------------------
	This class stores the extra data associated with a backup zip file, not needed in struct
	ZipFileHeader.

	@h3{Hungarian: zipsd}
----------------------------------------------------------------------------------------------*/
class ZipSystemData : public ZipData, public ZipFileHeader
{
	typedef ZipData Superclass;
public:
	ZipSystemData();
	~ZipSystemData();
	void Init(BackupInfo * pbkpi, BackupProgressDlg * pPrgDlg, StrUni stuDatabase);
	void Init(BackupInfo * pbkpi, BackupProgressDlg * pPrgDlg);
	virtual void TransmitData(IDispatchPtr qdisp, DISPID dwDispID);
	void WriteHeaderVariant(VARIANT * pvar);
	void ReadHeaderVariant(VARIANT * pvar);
	virtual void ParseReceivedParams(DISPPARAMS * pDispParams);
	HWND GetProgressDlgHwnd();

	BackupProgressDlg * m_pProgressDlg;

	//:> Data members are public, and can be set up and read like any struct:
	SmartBstr m_sbstrPassword; // Password to lock zip file with.
	//:> NOTE: if you add any more data members, put them immediately above this comment, and
	//:> alter the TransmitData() and ParseReceivedParams() methods as instructed there.

protected:
	InvokeHelper invh;
};


/*----------------------------------------------------------------------------------------------
	This class is used as an event sink for the Xceed Zip object to indicate current status etc.
	It appears to have its own interface, _IXceedZipEvents, for which no documentation is
	available. However, it also seems to work as an IDispatch interface, so this is how it has
	been used here.
	Note that no class factory is provided, as this class is curently used only as embedded in
	class XceedZip.
	@h3{Hungarian: xczs}
----------------------------------------------------------------------------------------------*/
class XceedZipSinkBackup : public XceedZipSink
{
	typedef XceedZipSink Superclass;
public:
	enum
	{
		//:> Additional operating modes:
		kidReadHeader = 3,
	};

	XceedZipSinkBackup();
	~XceedZipSinkBackup();
	void Init(ZipSystemData * pzipsd);
	void GetHeaderDbName(StrUni & stuDb);

	//:> IUnknown methods.
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

	//:> IDispatch methods.
	STDMETHOD(GetTypeInfoCount)(UINT * pctinfo);
	STDMETHOD(GetTypeInfo)(UINT iTInfo, LCID lcid, ITypeInfo ** ppTInfo);
	STDMETHOD(GetIDsOfNames)(REFIID riid, LPOLESTR * rgszNames, UINT cNames, LCID lcid,
		DISPID * rgDispId);
	STDMETHOD(Invoke)(DISPID dispIdMember, REFIID riid, LCID lcid, WORD wFlags,
		DISPPARAMS * pDispParams, VARIANT * pVarResult, EXCEPINFO * pExcepInfo,
		UINT * puArgErr);

protected:
//	long m_cref; // COM reference count.

//	int m_nOperatingMode; // One of the enumerated values kidZip, kidUnzip, or kidReadHeader
//	StrAppBufPath m_strbpZipFileName; // Used to extract project and version details
//	bool m_fUserCanceled; // True if user opts out of entering password or inserting new disk
//	int m_nLastDisk; // Disk number last requested by zip/unzip
	bool m_fPasswordAttempted; // True when at least one attempt at a password has been made
};



/*----------------------------------------------------------------------------------------------
	This class wraps the Xceed Zip COM interface.
	@h3{Hungarian: xczb}
----------------------------------------------------------------------------------------------*/
class XceedZipBackup : public XceedZip
{
public:
	XceedZipBackup();
	~XceedZipBackup();

	bool Init(ZipSystemData * pzipsd, BackupProgressDlg * pbkpprg);
	void DetachProgressDlg();
	long ReadHeader();
	void GetHeaderDbName(StrUni & stuDb);
	virtual bool TestError(long nResult, const achar * pszZipFile, bool fRestore = false);

	virtual long Zip();
	virtual long Unzip();

protected:
	BackupProgressDlg * m_pbkpprgDlg; // Progress indicator dialog
};

#endif //:> ZIPINVOKE_H_INCLUDED
