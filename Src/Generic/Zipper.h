/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Zipper.h
Responsibility:
Last reviewed: Not yet.

Description:
	Header file of class to interface to the Xceed Zip module.  It would be nice to be able to
	use an open source Zip library rather than a commercial tool, but we need the ability to
	split an archive across multiple floppy disks, which InfoZip doesn't offer, or at least
	didn't when we needed it.  The Xceed Zip module can be distributed without incremental
	royalty charges so long as it's part of a distributed program.
-------------------------------------------------------------------------------*//*:End Ignore*/

#pragma once
#ifndef ZIPPER_H_INCLUDED
#define ZIPPER_H_INCLUDED

// The Xceed Zip documentation lists these values as being part of the xcdError enum, but I
// don't know where the header that defines that enum is!
#ifndef xerSuccess
#define xerSuccess 0
#define xerProcessStarted 1
#define xerWarnings 526
#define xerFilesSkipped 527
#define xerEmptyZipFile 500
#define xerSeekInZipFile 501
#define xerEndOfZipFile 502
#define xerOpenZipFile 503
#define xerCreateTempFile 504
#define xerReadZipFile 505
#define xerWriteTempZipFile 506
#define xerWriteZipFile 507
#define xerMoveTempFile 508
#define xerNothingToDo 509
#define xerCannotUpdateAndSpan 510
#define xerMemory 511
#define xerSplitSizeTooSmall 512
#define xerSFXBinaryNotFound 513
#define xerReadSFXBinary 514
#define xerCannotUpdateSpanned 515
#define xerBusy 516
#define xerInsertDiskAbort 517
#define xerUserAbort 518
#define xerNotAZipFile 519
#define xerUninitializedString 520
#define xerUninitializedArray 521
#define xerInvalidArrayDimensions 522
#define xerInvalidArrayType 523
#define xerCannotAccessArray 524
#define xerUnsupportedDataType 525
#define xerDiskNotEmptyAbort 528
#define xerRemoveWithoutTemp 529
#define xerNotLicensed 530
#define xerInvalidSfxProperty 531
#define xerInternalError 999
#endif

/*----------------------------------------------------------------------------------------------
	This class is used to call the Invoke method of an IDispatch interface. It enables
	parameters to be passed as in a normal C++ method, and arranges them into the DISPPARAMS
	structure required by Invoke().

	@h3{Hungarian: invh}
----------------------------------------------------------------------------------------------*/
class InvokeHelper
{
public:
	void SetIDispatch(IDispatchPtr qdisp);

	VARIANT * MakeVariantFromStucture(void * pStructureBytes, int nStructureSize);
	void WriteVariantWithStucture(VARIANT * pvar, void * pStructureBytes, int nStructureSize);
	void WriteStructureFromVariant(VARIANT * pvar, void * pStructureBytes, int nStructureSize);
	void Go(DISPID dwDispID, WORD wFlags, VARTYPE vtRet, void * pvRet, int nParams,
		const VARTYPE * pbParamInfo, ...);

protected:
	void InvokeHelperV(DISPID dwDispID, WORD wFlags, VARTYPE vtRet,  void * pvRet, int nParams,
		const VARTYPE * pbParamInfo, va_list argList);

	IDispatchPtr m_qdisp; // The IDispatch interface we want to control
};


/*----------------------------------------------------------------------------------------------
	This class stores the data associated with a zip file.

	@h3{Hungarian: zipd}
----------------------------------------------------------------------------------------------*/
class ZipData
{
public:
	ZipData();
	~ZipData();

	void Init(BSTR bstrDevice, BSTR bstrPath, HWND hwndProgress);
	virtual void TransmitData(IDispatchPtr qdisp, DISPID dwDispID);
	virtual void ParseReceivedParams(DISPPARAMS * pDispParams);

	//:> Data members are public, and can be set up and read like any struct:
	int m_nProgressPercent;
	int m_nProgressGetEvent;

	SmartBstr m_sbstrDevice; // Device letter representing where to store the zip file.
	SmartBstr m_sbstrPath; // Destination path of the zip file.
	HWND m_hwndProgressDialog; // Handle to progress indicator dialog (may be NULL).
	//:> NOTE: if you add any more data members, put them immediately above this comment, and
	//:> alter the TransmitData() and ParseReceivedParams() methods as instructed there.

protected:
	InvokeHelper m_invh;
};


//:> We have to keep our own copy of the Xceed Zip Event Sink CLSID, as it is not readily
//:> available in any Xceed header file:
DEFINE_GUID(IID__IXceedZipEvents, 0xDB797691, 0x40E0, 0x11D2, 0x9B, 0xD5, 0x00, 0x60, 0x8, 0x2A,
	0xE3, 0x72);

/*----------------------------------------------------------------------------------------------
	This class is used as an event sink for the Xceed Zip object to indicate current status etc.
	It appears to have its own interface, _IXceedZipEvents, for which no documentation is
	available. However, it also seems to work as an IDispatch interface, so this is how it has
	been used here.
	Note that no class factory is provided, as this class is curently used only as embedded in
	class XceedZip.
	@h3{Hungarian: xczs}
----------------------------------------------------------------------------------------------*/
class XceedZipSink : public IDispatch
{
public:
	enum
	{
		//:> Operating modes:
		kidZip = 1,
		kidUnzip = 2
	};

	XceedZipSink();
	~XceedZipSink();

	void Init(ZipData * pzipd);
	void SetOperatingMode(int nMode);
	void SetZipFilename(StrAppBufPath strbp);
	bool UserCanceled()
	{
		return m_fUserCanceled;
	}

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
	long m_cref; // COM reference count.

	int m_nOperatingMode; // One of the enumerated values (kidZip, kidUnzip)
	ZipData * m_pzipd;
	StrAppBufPath m_strbpZipFileName; // Used to extract project and version details
	bool m_fUserCanceled; // True if user opts out of entering password or inserting new disk
	int m_nLastDisk; // Disk number last requested by zip/unzip
//	bool m_fPasswordAttempted; // True when at least one attempt at a password has been made
};



//:> We have to keep our own copy of the Xceed Zip CLSID, as it is not readily available in any
//:> Xceed header file:
DEFINE_GUID(CLSID_IXceedZipControl, 0xDB797690, 0x40E0, 0x11D2, 0x9B, 0xD5, 0x00, 0x60, 0x8,
	0x2A, 0xE3, 0x72);

/*----------------------------------------------------------------------------------------------
	This class wraps the Xceed Zip COM interface.

	@h3{Hungarian: xcz}
----------------------------------------------------------------------------------------------*/
class XceedZip
{
public:
	XceedZip();
	XceedZip(XceedZipSink * pxczs);
	~XceedZip();

	bool Init(ZipData * pzipd);
	bool UserCanceled()
	{
		return m_pxczs->UserCanceled();
	}
	bool ErrorDetails(long nErrorCode, bool & fFatal, int & rid);
	virtual bool TestError(long nResult, const achar * pszZipFile, bool fRestore = false);

	//:> The following are all methods provided by the Xceed zip module, although I've modified
	//:> arguments to fit FW string classes. Refer to Xceed help file for method details.
	bool GetAbort();
	void SetAbort(bool fNewValue);
	void GetBasePath(BSTR * pbstr);
	void SetBasePath(BSTR bstr);
	long GetCompressionLevel();
	void SetCompressionLevel(long nNewValue);
	void GetEncryptionPassword(BSTR * pbstr);
	void SetEncryptionPassword(BSTR bstr);
	long GetRequiredFileAttributes();
	void SetRequiredFileAttributes(long nNewValue);
	long GetExcludedFileAttributes();
	void SetExcludedFileAttributes(long nNewValue);
	void GetFilesToProcess(BSTR * pbstr);
	void SetFilesToProcess(BSTR bstr);
	void GetFilesToExclude(BSTR * pbstr);
	void SetFilesToExclude(BSTR bstr);
	DATE GetMinDateToProcess();
	void SetMinDateToProcess(DATE newValue);
	DATE GetMaxDateToProcess();
	void SetMaxDateToProcess(DATE newValue);
	long GetMinSizeToProcess();
	void SetMinSizeToProcess(long nNewValue);
	long GetMaxSizeToProcess();
	void SetMaxSizeToProcess(long nNewValue);
	long GetSplitSize();
	void SetSplitSize(long nNewValue);
	bool GetPreservePaths();
	void SetPreservePaths(bool fNewValue);
	bool GetProcessSubfolders();
	void SetProcessSubfolders(bool fNewValue);
	bool GetSkipIfExisting();
	void SetSkipIfExisting(bool fNewValue);
	bool GetSkipIfNotExisting();
	void SetSkipIfNotExisting(bool fNewValue);
	bool GetSkipIfOlderDate();
	void SetSkipIfOlderDate(bool fNewValue);
	bool GetSkipIfOlderVersion();
	void SetSkipIfOlderVersion(bool fNewValue);
	void GetTempFolder(BSTR * pbstr);
	void SetTempFolder(BSTR bstr);
	bool GetUseTempFile();
	void SetUseTempFile(bool fNewValue);
	void GetUnzipToFolder(BSTR * pbstr);
	void SetUnzipToFolder(BSTR bstr);
	void GetZipFilename(BSTR * pbstr);
	void SetZipFilename(BSTR bstr);
	long GetCurrentOperation();
	long GetSpanMultipleDisks();
	void SetSpanMultipleDisks(long nNewValue);
	long GetExtraHeaders();
	void SetExtraHeaders(long nNewValue);
	bool GetZipOpenedFiles();
	void SetZipOpenedFiles(bool fNewValue);
	bool GetBackgroundProcessing();
	void SetBackgroundProcessing(bool fNewValue);
	void GetSfxBinaryModule(BSTR * pbstr);
	void SetSfxBinaryModule(BSTR bstr);
	void GetSfxButtons(long xIndex, BSTR * pbstr);
	void SetSfxButtons(long xIndex, BSTR bstr);
	void GetSfxMessages(long xIndex, BSTR * pbstr);
	void SetSfxMessages(long xIndex, BSTR bstr);
	void GetSfxStrings(long xIndex, BSTR * pbstr);
	void SetSfxStrings(long xIndex, BSTR bstr);
	void GetSfxDefaultPassword(BSTR * pbstr);
	void SetSfxDefaultPassword(BSTR bstr);
	void GetSfxDefaultUnzipToFolder(BSTR * pbstr);
	void SetSfxDefaultUnzipToFolder(BSTR bstr);
	long GetSfxExistingFileBehavior();
	void SetSfxExistingFileBehavior(long nNewValue);
	void GetSfxReadmeFile(BSTR * pbstr);
	void SetSfxReadmeFile(BSTR bstr);
	void GetSfxExecuteAfter(BSTR * pbstr);
	void SetSfxExecuteAfter(BSTR bstr);
	bool GetSfxInstallMode();
	void SetSfxInstallMode(bool fNewValue);
	void GetSfxProgramGroup(BSTR * pbstr);
	void SetSfxProgramGroup(BSTR bstr);
	void GetSfxProgramGroupItems(BSTR * pbstr);
	void SetSfxProgramGroupItems(BSTR bstr);
	void GetSfxExtensionsToAssociate(BSTR * pbstr);
	void SetSfxExtensionsToAssociate(BSTR bstr);
	void GetSfxIconFilename(BSTR * pbstr);
	void SetSfxIconFilename(BSTR bstr);
	void AddFilesToProcess(BSTR bstrFileMask);
	void AddFilesToExclude(BSTR bstrFileMask);
	long PreviewFiles(bool fCalcCompSize);
	long ListZipContents();

	virtual long Zip();
	virtual long Unzip();

	long RemoveFiles();
	long TestZipFile(bool fCheckCompressedData);
	long GetZipFileInformation(long * lNbFiles, long * lCompressedSize,
		long * lUncompressedSize, short * nCompressionRatio, bool * bSpanned);
	void AboutBox();
	void SfxAddProgramGroupItem(BSTR bstrApplication, BSTR bstrDescription);
	void SfxAddExtensionToAssociate(BSTR bstrDescription, BSTR bstrExtension,
		BSTR bstrApplication);
	void SfxResetButtons();
	void SfxResetMessages();
	void SfxResetStrings();
	void SfxClearButtons();
	void SfxClearMessages();
	void SfxClearStrings();
	long Convert(BSTR bstrDestFilename);
	bool License(BSTR bstrLicense);
	bool SfxLoadConfig(BSTR bstrConfigFilename);
	bool SfxSaveConfig(BSTR bstrConfigFilename);
	void GetErrorDescription(long xType, long lValue, BSTR * pbstr);

protected:
	XceedZipSink * m_pxczs;		// Event sink.

	InvokeHelper invhZipControl; // Used to call methods in the Zip module
	//:> Variables to set up event sink:
	IConnectionPointPtr m_qcnpt; // Used in telling Zip module about event sink
	DWORD m_nCookie; // Used in detaching event sink from Zip module
};

/*----------------------------------------------------------------------------------------------
	This class just provides a couple of routines for encrypting and reverse-encrypting strings.

	@h3{Hungarian: strencr}
----------------------------------------------------------------------------------------------*/
class StringEncrypter
{
public:
	static int EncryptString(const StrAppBuf strb, BYTE * rgbEncryptedBytes);
	static void DecryptString(const BYTE * rgbEncryptedBytes, int nLength, StrAppBuf & strb,
		int nSeed);

protected:
	static void Next(int &i);
	static const BYTE rgkbMask[]; // Random array of bytes with which to encrypt string
};


#endif //:> ZIPPER_H_INCLUDED
