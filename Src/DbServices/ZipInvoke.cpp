/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ZipInvoke.cpp
Responsibility: Alistair Imrie
Last reviewed: never

Description:
	This file contains the class definitions for the Xceed Zip wrapper and event sink
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include "ZipDispIds.h"	// Comment out if this file is not available.

#undef THIS_FILE
DEFINE_THIS_FILE

// Handle instantiating collection class methods.
#include "Vector_i.cpp"


//:>********************************************************************************************
//:>	ZipFileHeader methods.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ZipFileHeader::ZipFileHeader()
{
	m_nStructSize = isizeof(ZipFileHeader);
	m_fPasswordProtected = false;
	ClearMemoryJog();
	ClearDBName();
}

/*----------------------------------------------------------------------------------------------
	Sets the structure's Memory Jog member, checking that specified string does not overrun
	internal size limits.
	@param sbstr Memory jog to be used
	@return False if string had to be truncated. (A truncated version will still be set.)
----------------------------------------------------------------------------------------------*/
bool ZipFileHeader::SetMemoryJog(SmartBstr sbstr)
{
	int nLength = sbstr.Length();
	bool fReturn = true;

	if (nLength >= knMemJogSize)
	{
		fReturn = false;
		nLength = knMemJogSize - 1;
	}

	memcpy(rgchMemoryJog, sbstr.Chars(), nLength * isizeof(OLECHAR));
	rgchMemoryJog[nLength] = 0;
	m_fMemoryJogUsed = true;

	return fReturn;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the Memory Jog, into the specified SmartBstr.
	@param sbstr [out] Retrieved memory jog
----------------------------------------------------------------------------------------------*/
void ZipFileHeader::GetMemoryJog(BSTR * pbstr)
{
	SmartBstr sbstr(rgchMemoryJog);
	*pbstr = sbstr.Detach();
}

/*----------------------------------------------------------------------------------------------
	Blank out the Memory Jog.
----------------------------------------------------------------------------------------------*/
void ZipFileHeader::ClearMemoryJog()
{
	::ZeroMemory(rgchMemoryJog, isizeof(rgchMemoryJog));
	m_fMemoryJogUsed = false;
}

/*----------------------------------------------------------------------------------------------
	Sets the structure's DatabaseName member, checking that specified string does not overrun
	internal size limits.
	@param sbstr Database name to be used
	@return False if string had to be truncated. (A truncated version will still be set.)
----------------------------------------------------------------------------------------------*/
bool ZipFileHeader::SetDBName(SmartBstr sbstr)
{
	int nLength = sbstr.Length();
	bool fReturn = true;

	if (nLength >= knDatabaseSize)
	{
		fReturn = false;
		nLength = knDatabaseSize - 1;
	}

	memcpy(rgchDatabase, sbstr.Chars(), nLength * isizeof(OLECHAR));
	rgchDatabase[nLength] = 0;

	return fReturn;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the database name, into the specified SmartBstr.
	@param sbstr [out] Retrieved database name
----------------------------------------------------------------------------------------------*/
void ZipFileHeader::GetDBName(BSTR * pbstr)
{
	SmartBstr sbstr(rgchDatabase);
	*pbstr = sbstr.Detach();
}

/*----------------------------------------------------------------------------------------------
	Blank out the DatabaseName name.
----------------------------------------------------------------------------------------------*/
void ZipFileHeader::ClearDBName()
{
	::ZeroMemory(rgchDatabase, isizeof(rgchDatabase));
}


//:>********************************************************************************************
//:>	ZipSystemData methods.
//:>********************************************************************************************

//:>  This macro determines how many elements are in an array declared as VARTYPE parms[] = {..}
#define NUMPAR(par_array) isizeof(par_array) / isizeof(VARTYPE)

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ZipSystemData::ZipSystemData()
{
	m_pProgressDlg = NULL;
	m_nProgressPercent = BackupProgressDlg::BKP_PRG_PERCENT;
	m_nProgressGetEvent = BackupProgressDlg::BKP_PRG_GET_EVENT;
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
ZipSystemData::~ZipSystemData()
{
}

/*----------------------------------------------------------------------------------------------
	Initialize ready for a backup.
	@param pbkpi User's backup/restore settings.
	@param hwndProgressDialog Handle of window which can process BKP_PRG_PERCENT messages.
	@param stuDatabase Name of database to be backed up.
----------------------------------------------------------------------------------------------*/
void ZipSystemData::Init(BackupInfo * pbkpi, BackupProgressDlg * pPrgDlg, StrUni stuDatabase)
{
	SmartBstr sbstr;

	m_pProgressDlg = pPrgDlg;

	// Transfer a few needed items from the BackupInfo to our structure:
	pbkpi->m_strbDeviceName.GetBstr(&m_sbstrDevice);
	pbkpi->m_strbDirectoryPath.GetBstr(&m_sbstrPath);

	m_fPasswordProtected = pbkpi->m_bkppwi.m_fLock;
	if (m_fPasswordProtected)
		pbkpi->m_bkppwi.m_strbPassword.GetBstr(&m_sbstrPassword);
	m_fMemoryJogUsed = (pbkpi->m_bkppwi.m_strbMemoryJog.Length() > 0);
	if (m_fMemoryJogUsed)
	{
		pbkpi->m_bkppwi.m_strbMemoryJog.GetBstr(&sbstr);
		SetMemoryJog(sbstr);
	}
	stuDatabase.GetBstr(&sbstr);
	SetDBName(sbstr);
}

/*----------------------------------------------------------------------------------------------
	Initialize ready for a restore.
	@param pbkpi User's backup/restore settings.
	@param pProgressDialog window which can process BKP_PRG_PERCENT messages.
----------------------------------------------------------------------------------------------*/
void ZipSystemData::Init(BackupInfo * pbkpi, BackupProgressDlg * pPrgDlg)
{
	m_pProgressDlg = pPrgDlg;
	pbkpi->m_strbDeviceName.GetBstr(&m_sbstrDevice);
	pbkpi->m_strbDirectoryPath.GetBstr(&m_sbstrPath);
}

/*----------------------------------------------------------------------------------------------
	Returns the window handle of the backup progress dialog.
----------------------------------------------------------------------------------------------*/
HWND ZipSystemData::GetProgressDlgHwnd()
{
	return m_pProgressDlg->Hwnd();
}

/*----------------------------------------------------------------------------------------------
	Send data down IDispatch interface. This is no longer used, but may be useful if we ever
	want COM to create the zip event sink for us.
	@param qdisp The IDispatch interface
	@param dwDispID The ID of the dispatch method which knows how to receive the data

	NOTE: if you add any more data members, add their types to the end of the parms[] array,
	and add the data members to the end of the invh.Go() parameters.
----------------------------------------------------------------------------------------------*/
void ZipSystemData::TransmitData(IDispatchPtr qdisp, DISPID dwDispID)
{
	invh.SetIDispatch(qdisp);
	VARIANT * pvar = invh.MakeVariantFromStucture((void *)(dynamic_cast<ZipFileHeader *>(this)),
		isizeof(ZipFileHeader));

	static VARTYPE parms[] = { VT_VARIANT, VT_BSTR, VT_I4, VT_BSTR, VT_BSTR/* <- New here */ };
	invh.Go(dwDispID, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL, NUMPAR(parms), parms, pvar,
		(BSTR)m_sbstrDevice, (long)GetProgressDlgHwnd(), (BSTR)m_sbstrPassword,
		(BSTR)m_sbstrPath /* <- New data here */);
}

/*----------------------------------------------------------------------------------------------
	Writes the ZipFileHeader base object into the given VARIANT structure, as a SAFEARRAY.
	@param pvar [out] The VARIANT to receive the data
----------------------------------------------------------------------------------------------*/
void ZipSystemData::WriteHeaderVariant(VARIANT * pvar)
{
	invh.WriteVariantWithStucture(pvar, (void *)(dynamic_cast<ZipFileHeader *>(this)),
		isizeof(ZipFileHeader));
}

/*----------------------------------------------------------------------------------------------
	Reads the ZipFileHeader base object from the given VARIANT structure (as a SAFEARRAY).
	@param pvar The VARIANT to be read
----------------------------------------------------------------------------------------------*/
void ZipSystemData::ReadHeaderVariant(VARIANT * pvar)
{
	// First, set the ZipFileHeader part of this structure to zero:
	memset((void *)(dynamic_cast<ZipFileHeader *>(this)), 0, isizeof(ZipFileHeader));

	// The first four bytes should be the size of the struct that was saved (may have been an
	// old version):
	int nSavedStructureSize;
	invh.WriteStructureFromVariant(pvar, (void *)(&nSavedStructureSize), isizeof(int));

	// Now read either as much data as we can handle, or as much data as was written, whichever
	// is less:
	invh.WriteStructureFromVariant(pvar, (void *)(dynamic_cast<ZipFileHeader *>(this)),
		min(nSavedStructureSize, isizeof(ZipFileHeader)));

	// Reset our structure size:
	m_nStructSize = isizeof(ZipFileHeader);
}

/*----------------------------------------------------------------------------------------------
	Translate data received via an IDispatch interface. This is no longer used, but may be
	useful if we ever want COM to create the zip event sink for us.
	@param pDispParams The DISPPARAMS structure as received by the interface's Invoke() method.

	NOTE: if you add any more data members, follow the pattern laid out for the existing data.
----------------------------------------------------------------------------------------------*/
void ZipSystemData::ParseReceivedParams(DISPPARAMS * pDispParams)
{
	int nArgs = pDispParams->cArgs;

	// ZipFileHeader structure:
	if (nArgs >= 1)
	{
		Assert(pDispParams->rgvarg[nArgs - 1].vt == (VT_UI1 | VT_ARRAY));
		ReadHeaderVariant(&pDispParams->rgvarg[nArgs - 1]);
	}
	// Device:
	if (nArgs >= 2)
	{
		Assert(pDispParams->rgvarg[nArgs - 2].vt == VT_BSTR);
		m_sbstrDevice = pDispParams->rgvarg[nArgs - 2].bstrVal;
		// Get rid of final backslash character:
		StrUni stu = m_sbstrDevice.Chars();
		int i = stu.ReverseFindCh('\\');
		if (i != -1)
			m_sbstrDevice.Assign(stu.Left(i).Chars());
	}
	// Handle to progress dialog:
	if (nArgs >= 3)
	{
		Assert(pDispParams->rgvarg[nArgs - 3].vt == VT_I4);
		m_hwndProgressDialog = (HWND)pDispParams->rgvarg[nArgs - 3].lVal;
	}
	// Password:
	if (nArgs >= 4)
	{
		Assert(pDispParams->rgvarg[nArgs - 4].vt == VT_BSTR);
		m_sbstrPassword = pDispParams->rgvarg[nArgs - 4].bstrVal;
	}
	// Path:
	if (nArgs >= 5)
	{
		Assert(pDispParams->rgvarg[nArgs - 5].vt == VT_BSTR);
		m_sbstrPath = pDispParams->rgvarg[nArgs - 5].bstrVal;
	}
}



//:>********************************************************************************************
//:>	XceedZipSinkBackup methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
XceedZipSinkBackup::XceedZipSinkBackup()
{
	m_fPasswordAttempted = false;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
XceedZipSinkBackup::~XceedZipSinkBackup()
{
	Assert(m_cref == 1);
}

static DummyFactory g_fact(_T("SIL.AppCore.XceedZipSink"));

/*----------------------------------------------------------------------------------------------
	Initialization.
	@param pzipsd Relevant information about current zip operation
----------------------------------------------------------------------------------------------*/
void XceedZipSinkBackup::Init(ZipSystemData * pzipsd)
{
	AssertPtr(pzipsd);
	m_pzipd = pzipsd;
	m_fUserCanceled = false;
	m_nLastDisk = -1;
	m_fPasswordAttempted = false;
}

/*----------------------------------------------------------------------------------------------
	Retrieve database name after zip file header has been read in.
	@param stuDb [out] Name of database read from header
----------------------------------------------------------------------------------------------*/
void XceedZipSinkBackup::GetHeaderDbName(StrUni & stuDb)
{
	AssertPtr(dynamic_cast<ZipSystemData *>(m_pzipd));
	SmartBstr sbstr;
	dynamic_cast<ZipSystemData *>(m_pzipd)->GetDBName(&sbstr);
	stuDb.Assign(sbstr.Chars());
}

/*----------------------------------------------------------------------------------------------
	IUnknown Method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XceedZipSinkBackup::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	// REVIEW:
	// There now follows what is possibly a kludge, in that a request for an _IXceedZipEvents
	// interface is satisfied with an IDispatch interface being returned. I (Alistair) am not
	// sure if this is strictly legal, but it seems to work.
	else if (riid == IID_IDispatch || riid == IID__IXceedZipEvents)
		*ppv = static_cast<IDispatch *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	This method does not need to be implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XceedZipSinkBackup::GetTypeInfoCount(UINT * pctinfo)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This method does not need to be implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XceedZipSinkBackup::GetTypeInfo(UINT iTInfo, LCID lcid, ITypeInfo ** ppTInfo)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This method does not need to be implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XceedZipSinkBackup::GetIDsOfNames(REFIID riid, LPOLESTR * rgszNames, UINT cNames,
	LCID lcid, DISPID * rgDispId)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This IDispatch method gets called when events happen in the Xceed Zip object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XceedZipSinkBackup::Invoke(DISPID dispIdMember, REFIID riid, LCID lcid,
	WORD wFlags, DISPPARAMS * pDispParams, VARIANT * pVarResult, EXCEPINFO * pExcepInfo,
	UINT * puArgErr)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pDispParams);
	ChkComArgPtrN(pVarResult);
	ChkComArgPtrN(pExcepInfo);
	ChkComArgPtrN(puArgErr);

	AssertPtr(dynamic_cast<ZipSystemData *>(m_pzipd));
/*
This code will put up a message box if the zip module fires any "warning" events.
Warnings are for problems in the zip file that are small enough not to cause a failure.

	if (dispIdMember == XCD_ZIP_DISPID_WARNING)
	{
		SmartBstr sbstr = pDispParams->rgvarg[1].bstrVal;
		StrApp strFile;
		strFile.Assign(sbstr.Chars());

		int nWarning = pDispParams->rgvarg[0].lVal;
		StrApp strMsg;
		strMsg.Format(_T("Got warning: %d in file %s"), nWarning, strFile.Chars());

		MessageBox(NULL, strMsg.Chars(), _T("Zip"), 0);
	}
*/
#ifdef XCD_ZIP_DISPID_GLOBALSTATUS
	const wchar * kwszHeader = L"header";

	switch (dispIdMember)
	{
	case XCD_ZIP_DISPID_GLOBALSTATUS:
		return Superclass::Invoke(dispIdMember, riid, lcid, wFlags, pDispParams, pVarResult,
			pExcepInfo,	puArgErr);
	case XCD_ZIP_DISPID_INSERTDISK:
		return Superclass::Invoke(dispIdMember, riid, lcid, wFlags, pDispParams, pVarResult,
			pExcepInfo,	puArgErr);
	case XCD_ZIP_DISPID_QUERYMEMORYFILE:
		// Xceed event, which enables us to prepare to write from memory directly into the zip.
		// We use this to set up the header information file.
		Assert(pDispParams->cArgs >= 10);
		Assert(pDispParams->rgvarg[0].vt == (VT_BOOL|VT_BYREF));
		Assert(pDispParams->rgvarg[2].vt == (VT_BOOL|VT_BYREF));
		Assert(pDispParams->rgvarg[8].vt == (VT_BSTR|VT_BYREF));
		Assert(pDispParams->rgvarg[9].vt == (VT_I4|VT_BYREF));
		// We only need provide a header file if this is the first time this event has happened
		// in this backup:
		if (*(pDispParams->rgvarg[9].plVal) == 0)
		{
			// A header file is required:
			*(pDispParams->rgvarg[0].pboolVal) = (VARIANT_BOOL)-1; // Signal 'file is provided'
			*(pDispParams->rgvarg[2].pboolVal) = (VARIANT_BOOL)0; // Signal 'not encrypted'
			SmartBstr sbstr = kwszHeader;
			*(pDispParams->rgvarg[8].pbstrVal) = ::SysAllocString(sbstr); // File name in zip
		}
		break;
	case XCD_ZIP_DISPID_ZIPPINGMEMORYFILE:
		// Xceed event, which calls for the memory data that is to be written to the zip
		Assert(pDispParams->cArgs >= 2);
		Assert(pDispParams->rgvarg[0].vt == (VT_BOOL|VT_BYREF));
		Assert(pDispParams->rgvarg[1].vt == (VT_VARIANT|VT_BYREF));
		{ // Block
			VARIANT * pvar = pDispParams->rgvarg[1].pvarVal;
			VariantInit(pvar);
			dynamic_cast<ZipSystemData *>(m_pzipd)->WriteHeaderVariant(pvar);
			// Signal 'no more data':
			*(pDispParams->rgvarg[0].pboolVal) = (VARIANT_BOOL)-1;
		}
		break;
	case XCD_ZIP_DISPID_UNZIPPREPROCESSINGFILE:
		// Xceed event, which enables us to extract the header file on its own.
		Assert(pDispParams->cArgs >= 18);
		Assert(pDispParams->rgvarg[0].vt == (VT_I4|VT_BYREF));
		Assert(pDispParams->rgvarg[3].vt == (VT_BOOL|VT_BYREF));
		Assert(pDispParams->rgvarg[17].vt == VT_BSTR);
		{ // Block
			// Compare supplied file name with that of our header file:
			SmartBstr sbstr = pDispParams->rgvarg[17].bstrVal;
			SmartBstr sbstrHeader = kwszHeader;
			if (sbstr == sbstrHeader)
			{
				// We have now established that we are dealing with the header file.
				// Check which operating mode we're in:
				if (m_nOperatingMode == kidReadHeader)
					// Reading header: signal file is to be read into memory:
					*(pDispParams->rgvarg[0].plVal) = 1;
				else
					// Not interested in header, so signal to skip file:
					*(pDispParams->rgvarg[3].pboolVal) = (VARIANT_BOOL)-1;
			}
			else // Current file is not the header file
			{
				// See if we actually wanted the header file this time:
				if (m_nOperatingMode == kidReadHeader)
					// We're only interested in the header file, so skip this one:
					*(pDispParams->rgvarg[3].pboolVal) = (VARIANT_BOOL)-1;
			}
		}
		break;
	case XCD_ZIP_DISPID_UNZIPPINGMEMORYFILE:
		// Xceed event, which gives us the data in the header file:
		Assert(pDispParams->cArgs >= 3);
		Assert(pDispParams->rgvarg[1].vt == (VT_VARIANT|VT_BYREF));
		Assert(pDispParams->rgvarg[2].vt == VT_BSTR);
		{ // Block
			// Check file name matches 'header'
			SmartBstr sbstr = pDispParams->rgvarg[2].bstrVal;
			SmartBstr sbstrHeader = kwszHeader;
			Assert(sbstr == sbstrHeader);
			if (sbstr == sbstrHeader)
			{
				VARIANT * pvar = pDispParams->rgvarg[1].pvarVal;
				dynamic_cast<ZipSystemData *>(m_pzipd)->ReadHeaderVariant(pvar);
			}
		}
		break;
	case XCD_ZIP_DISPID_INVALIDPASSWORD:
		// Xceed event, which enables an(other) attempt at entering the password.
		if (m_fUserCanceled)
			break;
		Assert(pDispParams->cArgs >= 3);
		Assert(pDispParams->rgvarg[0].vt == (VT_BOOL|VT_BYREF));
		Assert(pDispParams->rgvarg[1].vt == (VT_BSTR|VT_BYREF));
		Assert(pDispParams->rgvarg[2].vt == VT_BSTR);
		{ // Block
			// Test if user has already attempted a password:
			if (m_fPasswordAttempted)
			{
				// User has already tried at least once, so inform them that last attempt was
				// invalid:
				StrApp strMsg(kstidRstWrongPasswd);
				StrApp strTitle(kstidBkpSystem);
				if (::MessageBox(((ZipSystemData *)m_pzipd)->GetProgressDlgHwnd(), strMsg.Chars(),
					strTitle.Chars(), MB_ICONEXCLAMATION | MB_OKCANCEL) == IDCANCEL)
				{
					// User wants to abort.
					m_fUserCanceled = true;
					break;
				}
			}
			StrApp strPassword;
			StrApp strMemoryJog;
			StrApp strDatabase;
			StrApp strProject;
			StrApp strVersion;
			if (dynamic_cast<ZipSystemData *>(m_pzipd)->m_fMemoryJogUsed)
				strMemoryJog.Assign(dynamic_cast<ZipSystemData *>(m_pzipd)->rgchMemoryJog);
			strDatabase.Assign(dynamic_cast<ZipSystemData *>(m_pzipd)->rgchDatabase);
			BackupFileNameProcessor::GetProjectName(m_strbpZipFileName, strProject);
			BackupFileNameProcessor::GetVersion(m_strbpZipFileName, strVersion);
			bool fTryAgain;
			// Give the user repeated chances to get the password right:
			do
			{
				fTryAgain = false;
				if (RestorePasswordDlg::GetPassword(((ZipSystemData *)m_pzipd)->GetProgressDlgHwnd(), strMemoryJog,
					strDatabase, strProject, strVersion, strPassword))
				{
					// The user entered a password attempt.
					SmartBstr sbstr;
					strPassword.GetBstr(&sbstr);
					*(pDispParams->rgvarg[1].pbstrVal) = ::SysAllocString(sbstr);
					*(pDispParams->rgvarg[0].pboolVal) = (VARIANT_BOOL)-1; //Signal new password
					m_fPasswordAttempted = true;
				}
				else
				{
					// The user canceled instead of entering a password.
					StrAppBuf strbMessage(kstidRstQueryAbort);
					StrAppBuf strbTitle(kstidRstAbort);
					// Check if user wants to abort the restore:
					if (::MessageBox(((ZipSystemData *)m_pzipd)->GetProgressDlgHwnd(), strbMessage.Chars(),
						strbTitle.Chars(), MB_ICONQUESTION | MB_YESNO) == IDYES)
					{
						// User wants to abort.
						m_fUserCanceled = true;
					}
					else
					{
						// User has changed mind, so loop back to input password again.
						fTryAgain = true;
					}
				}
			} while (fTryAgain);
		}
		break;
	}
#endif

	return S_OK;

	END_COM_METHOD(g_fact, IID_IDispatch)
}


//:>********************************************************************************************
//:>	XceedZipBackup methods.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
XceedZipBackup::XceedZipBackup()
	: XceedZip(NewObj XceedZipSinkBackup)
{
	m_nCookie = 0;
	m_pbkpprgDlg = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
XceedZipBackup::~XceedZipBackup()
{
	if (m_nCookie && m_qcnpt)
	{
		// Detach event sink:
		m_qcnpt->Unadvise(m_nCookie);
	}
	if (m_pbkpprgDlg)
	{
		// Detach self from progress dialog:
		m_pbkpprgDlg->SetXceedObject(NULL);
	}
	m_pbkpprgDlg = NULL;
}

/*----------------------------------------------------------------------------------------------
	Set up the Xceed Zip control. Then pass ZipSystemData structure to the event sink.
	@param pzipsd Configuration data.
	@param pbkpprg Progress dialog.
----------------------------------------------------------------------------------------------*/
bool XceedZipBackup::Init(ZipSystemData * pzipsd, BackupProgressDlg * pbkpprg)
{
	AssertPtr(pbkpprg);
	AssertPtr(pzipsd);

	m_pbkpprgDlg = pbkpprg;
	// Make sure the progress dialog will be able to tell this zip module to stop:
	if (m_pbkpprgDlg)
		m_pbkpprgDlg->SetXceedObject(this);

	// Make sure ZipSystemData knows about progress dialog:
	pzipsd->m_pProgressDlg = m_pbkpprgDlg;

	// Create Xceed Zip module:
	IDispatchPtr qdisp;
	HRESULT hr;
	hr = ::CoCreateInstance(CLSID_IXceedZipControl, NULL, CLSCTX_ALL, IID_IDispatch, (void **)&qdisp);
	if (FAILED(hr))
		return false;

	invhZipControl.SetIDispatch(qdisp);

	// Try to license our Zip module, by reading from the encrypted license file.
	// The license file should be in FieldWorks's root directory, so find that:
	StrAppBufPath strbpFwRootDir;
	strbpFwRootDir.Assign(DirectoryFinder::FwRootCodeDir().Chars());
	StrAppBufPath strbpLicense(strbpFwRootDir);
	if (strbpLicense.Length() == 0 || strbpLicense[strbpLicense.Length() - 1] != '\\')
		strbpLicense.Append(_T("\\"));
	strbpLicense.Append(_T("ZipLicense.bin"));

	FILE * file;
	if (!_tfopen_s(&file, strbpLicense.Chars(), _T("rb")))
	{
		int nLength = _getw(file) - 1;
		BYTE * rgbEncryptedBytes = NewObj BYTE[nLength];
		if (rgbEncryptedBytes)
		{
			int nSeed;
			for (int i = -2; i < nLength; i++)
			{
				// The first two words in the file are the 32-bit encryption seed:
				if (i == -2)
					nSeed = _getw(file);
				else if (i == -1)
					nSeed |= (_getw(file) << 16);
				else
					rgbEncryptedBytes[i] = (char)getc(file);
			}
			StrAppBuf strbLicenseKey;
			StringEncrypter::DecryptString(rgbEncryptedBytes, nLength, strbLicenseKey, nSeed);
			delete[] rgbEncryptedBytes;

			SmartBstr sbstrLicenseKey;
			strbLicenseKey.GetBstr(&sbstrLicenseKey);
			License(sbstrLicenseKey);
			/* The License() method returns false if the license key is incorrect. However, this
			does not matter, as an unlicensed zip module will cause an error later on.
			It may be the case that the user has licensed their own Xceed Zip component aleady
			(very unlikely), in which case this call is not damaging if an invalid license is
			presented.

			Note to developers and testers: If you have a fully licensed (or time-trial) Xceed
			Zip module, and you want to test this piece of code, you can unlicense your module
			by doing the following:
			1) Copy the XceedZip.dll file, to a temporary name
			2) Via the Windows Control Panel "Add/Remove Programs", or some other method,
			uninstall the Xceed Zip files.
			3) Delete the following keys from the registry, if they exist:
				HKEY_CLASSES_ROOT\Licenses\414BA510-C70D-11d4-BFF5-0060082AE372
				HKEY_CLASSES_ROOT\Licenses\423FAB50-BB80-11d2-A5A7-00105A9C91C6
				HKEY_CLASSES_ROOT\Licenses\4507C5F2-9077-40DE-A621-64EC59BF6829\SFX
				HKEY_CLASSES_ROOT\Licenses\4507C5F2-9077-40DE-A621-64EC59BF6829\ZIP
				Any key containing "Xceed", unless you have another Xceed product!
			4) Restore the original name of the XceedZip.dll file, and register it using
			"regsvr32 xceedzip.dll" from a DOS prompt in the relevant directory.
			*/
		}
		fclose(file);
	}

	// Now set up the interface needed to link Xceed Zip to our event sink:
	IConnectionPointContainerPtr qcnpctr;
	CheckHr(qdisp->QueryInterface(IID_IConnectionPointContainer, (void **)&qcnpctr));
	qcnpctr->FindConnectionPoint(IID__IXceedZipEvents, &m_qcnpt);

	// Initialize event sink:
	m_pxczs->Init(pzipsd);

	// Get IUnknown pointer:
	IUnknownPtr qunkSink;
	m_pxczs->QueryInterface(IID_IUnknown, (void **)&qunkSink);

	// Register the event sink with the Xceed module:
	m_qcnpt->Advise(qunkSink, &m_nCookie);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Breaks the connection to the Progress Dialog.
----------------------------------------------------------------------------------------------*/
void XceedZipBackup::DetachProgressDlg()
{
	m_pbkpprgDlg = NULL;
}

/*----------------------------------------------------------------------------------------------
	Method to set up zip event sink with details stored in zip's 'header' file
	@return The return code from the IDispatch::Invoke() method
----------------------------------------------------------------------------------------------*/
long XceedZipBackup::ReadHeader()
{
	m_pxczs->SetOperatingMode(XceedZipSinkBackup::kidReadHeader);

	long nResult = 0;
#ifdef XCD_ZIP_DISPID_UNZIP
	invhZipControl.Go(XCD_ZIP_DISPID_UNZIP, DISPATCH_METHOD, VT_I4, (void *)&nResult, 0, NULL);
#endif
	return nResult;
}


/*----------------------------------------------------------------------------------------------
	Method to retrieve database name after zip file header has been read in.
	@param stuDb [out] The retrieved database name
----------------------------------------------------------------------------------------------*/
void XceedZipBackup::GetHeaderDbName(StrUni & stuDb)
{
	dynamic_cast<XceedZipSinkBackup *>(m_pxczs)->GetHeaderDbName(stuDb);
}

/*----------------------------------------------------------------------------------------------
	Test result code, and categorize and report error as appropriate.
	@param nResult The error code returned from a suspect Zip operation.
	@param fRestore True if zip operation was part of restore, false if backup.
	@return True if error is known to be fatal.
----------------------------------------------------------------------------------------------*/
bool XceedZipBackup::TestError(long nResult, const achar * pszZipFile, bool fRestore)
{
	if (nResult == 0)
		return false;

	HWND hwnd = NULL;
	if (m_pbkpprgDlg)
		hwnd = m_pbkpprgDlg->Hwnd();

	int ridMessage;
	bool fFatal;
	if (!ErrorDetails(nResult, fFatal, ridMessage))
	{
		int nCategory = (fRestore ? BackupErrorHandler::kRestorePossibleFailure :
			BackupErrorHandler::kBackupPossibleFailure);
		BackupErrorHandler::MessageBox(hwnd, nCategory, kstidUseDefault, kstidZipErrUnknown,
			MB_ICONERROR | MB_OK, (int)nResult);
	}
	else
	{
		if (fFatal)
		{
			int nCategory = (fRestore ? BackupErrorHandler::kRestoreFailure :
				BackupErrorHandler::kBackupFailure);
			BackupErrorHandler::ErrorBox(hwnd, nCategory, ridMessage, 0, pszZipFile);
		}
		else
		{
			int nCategory = (fRestore ? BackupErrorHandler::kRestorePossibleFailure :
				BackupErrorHandler::kBackupPossibleFailure);
			BackupErrorHandler::ErrorBox(hwnd, nCategory, ridMessage, 0, pszZipFile);
		}
	}
	return fFatal;
}



//:>********************************************************************************************
//:>	Wrapper methods. See Xceed Zip help for details.
//:>********************************************************************************************

// See Xceed Zip help for details.
long XceedZipBackup::Zip()
{
	// Tell event sink that we are doing a write:
	m_pxczs->SetOperatingMode(XceedZipSink::kidZip);

	long nResult = 0;
#ifdef XCD_ZIP_DISPID_ZIP
	// IMPORTANT NOTE: the following call may well result in an access violation
	// error. It appears that this can be ignored: simply press F5 again, and agree
	// to pass the exception to the program. (The error only occurs when in VS.)
	invhZipControl.Go(XCD_ZIP_DISPID_ZIP, DISPATCH_METHOD, VT_I4, (void *)&nResult, 0, NULL);
#endif

	if (m_pxczs->UserCanceled())
	{
		// Indicate that user canceled by fiddling progress dialog's abort flag:
		m_pbkpprgDlg->SetAbortedFlag();
	}
	return nResult;
}

// See Xceed Zip help for details.
long XceedZipBackup::Unzip()
{
	// Tell event sink that we are doing a restore:
	m_pxczs->SetOperatingMode(XceedZipSink::kidUnzip);

	long nResult = 0;
#ifdef XCD_ZIP_DISPID_UNZIP
	invhZipControl.Go(XCD_ZIP_DISPID_UNZIP, DISPATCH_METHOD, VT_I4, (void *)&nResult, 0, NULL);
#endif

	if (m_pxczs->UserCanceled())
	{
		// Indicate that user canceled by fiddling progress dialog's abort flag:
		m_pbkpprgDlg->SetAbortedFlag();
	}

	return nResult;
}
