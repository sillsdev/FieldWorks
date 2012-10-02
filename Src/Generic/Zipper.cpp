/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Zipper.cpp
Responsibility:
Last reviewed: never

Description:
	This file contains the class definitions for the Xceed Zip wrapper and event sink
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include "ZipDispIds.h"	// Comment out if this file is not available.

#include "AtlBase.h"
#include "AtlCom.h"

#undef THIS_FILE
DEFINE_THIS_FILE

// Handle instantiating collection class methods.
#include "Vector_i.cpp"

//:>********************************************************************************************
//:>	InvokeHelper methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Set the particular IDispatch interface we want to communicate with.
	@param qdisp Smart pointer to IDispatch interface to be used from now on
----------------------------------------------------------------------------------------------*/
void InvokeHelper::SetIDispatch(IDispatchPtr qdisp)
{
	AssertPtr(qdisp);
	m_qdisp = qdisp;
}

/*----------------------------------------------------------------------------------------------
	Make a variant out of an arbitrary data structure. This can then be passed to the Go()
	method.
	@param pStructureBytes Pointer to arbitrary data
	@param nStructureSize Number of bytes in data
	@return Newly created VARIANT, which the caller must delete.
----------------------------------------------------------------------------------------------*/
VARIANT * InvokeHelper::MakeVariantFromStucture(void * pStructureBytes, int nStructureSize)
{
	Assert(pStructureBytes || nStructureSize == 0);

	VARIANT * pvarResult = NewObj(VARIANT);
	WriteVariantWithStucture(pvarResult, pStructureBytes, nStructureSize);
	return pvarResult;
}

/*----------------------------------------------------------------------------------------------
	Write data from an arbitrary structure into a VARIANT, via a SAFEARRAY.
	@param pvar [out] Pointer to structure to be filled with data
	@param pStructureBytes [in] Pointer to arbitrary data
	@param nStructureSize [in] Number of bytes in data
----------------------------------------------------------------------------------------------*/
void InvokeHelper::WriteVariantWithStucture(VARIANT * pvar, void * pStructureBytes,
											int nStructureSize)
{
	Assert(pStructureBytes || nStructureSize == 0);

	// Set up the bounds of the SAFEARRAY:
	SAFEARRAYBOUND rgsabound[1];
	rgsabound[0].lLbound = 0;
	rgsabound[0].cElements = nStructureSize;

	// Create the SAFEARRAY:
	SAFEARRAY * psa = SafeArrayCreate(VT_UI1, 1, rgsabound);
	// REVIEW: Not sure if system deletes this memory for me or not.

	void * pBytes;
	CheckHr(SafeArrayAccessData(psa, &pBytes));
	memcpy(pBytes, pStructureBytes, nStructureSize);
	CheckHr(SafeArrayUnaccessData(psa));

	VariantInit(pvar);
	pvar->vt = VT_UI1 | VT_ARRAY;
	pvar->parray = psa;
}

/*----------------------------------------------------------------------------------------------
	Write data into an arbitrary structure from a VARIANT, via a SAFEARRAY.
	@param pvar [in] Pointer to structure containing the data
	@param pStructureBytes [out] Pointer to buffer to receive data
	@param nStructureSize [in] Number of bytes in data
----------------------------------------------------------------------------------------------*/
void InvokeHelper::WriteStructureFromVariant(VARIANT * pvar, void * pStructureBytes,
											 int nStructureSize)
{
	AssertPtr(pvar);
	Assert(pvar->vt == (VT_UI1 | VT_ARRAY));
	Assert(pStructureBytes || nStructureSize == 0);

	// Fetch the SAFEARRAY:
	SAFEARRAY * psa = pvar->parray;
	// Determine the bounds of the SAFEARRAY:
	long nHigh = 0;
	SafeArrayGetUBound(psa, 1, &nHigh);
	long nLow = 0;
	SafeArrayGetLBound(psa, 1, &nLow);
	long nSize = nHigh - nLow + 1;
	// Get at the SAFEARRAY's data:
	void * pBytes;
	CheckHr(SafeArrayAccessData(psa, &pBytes));
	// Write the data, taking care not to exceed the either the range of the array, or the size
	// of the structure:
	memcpy(pStructureBytes, pBytes, min(nStructureSize, nSize));
	CheckHr(SafeArrayUnaccessData(psa));
}

/*----------------------------------------------------------------------------------------------
	Invoke a method in the dispatch interface. This method was adapted from MFC, and takes any
	number of parameters.
	@param dwDispID ID of dispatch function to invoke
	@param wFlags Flags to indicate type of function e.g. DISPATCH_PROPERTYPUT
	@param vtRet Type of return value (VT_EMPTY if none)
	@param pvRet Pointer to return value (may be NULL if none is given)
	@param nParams Number of parameters to expect
	@param pbParamInfo Array of types of subsequent parameters
----------------------------------------------------------------------------------------------*/
void InvokeHelper::Go(DISPID dwDispID, WORD wFlags, VARTYPE vtRet, void * pvRet, int nParams,
					  const VARTYPE * pbParamInfo, ...)
{
	va_list argList;
	va_start(argList, pbParamInfo);
	InvokeHelperV(dwDispID, wFlags, vtRet, pvRet, nParams, pbParamInfo, argList);
	va_end(argList);
}

/*----------------------------------------------------------------------------------------------
	Invoke a method in a dispatch interface. This method was adapted from MFC, and is called by
	Go() once the variable argument list has been established.
	@param dwDispID ID of dispatch function to invoke
	@param wFlags Flags to indicate type of function e.g. DISPATCH_PROPERTYPUT
	@param vtRet Type of return value (VT_EMPTY if none)
	@param pvRet Pointer to return value (may be NULL if none is given)
	@param nParams Number of parameters to expect
	@param pbParamInfo Array of types of subsequent parameters
	@param argList Variable argument list prepared by ${#Go} method
----------------------------------------------------------------------------------------------*/
void InvokeHelper::InvokeHelperV(DISPID dwDispID, WORD wFlags, VARTYPE vtRet,
	void * pvRet, int nParams, const VARTYPE * pbParamInfo, va_list argList)
{
	AssertPtr(m_qdisp);

	USES_CONVERSION; // ATL macro

	// Instantiate the structure that will actually be sent to the function to be invoked:
	DISPPARAMS dispparams;
	memset(&dispparams, 0, isizeof(dispparams));

	dispparams.cArgs = nParams;

	DISPID dispidNamed = DISPID_PROPERTYPUT;
	if (wFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF))
	{
		Assert(dispparams.cArgs > 0);
		dispparams.cNamedArgs = 1;
		dispparams.rgdispidNamedArgs = &dispidNamed;
	}

	if (dispparams.cArgs != 0)
	{
		// Allocate memory for all VARIANT parameters
		VARIANT * pArg = NewObj VARIANT[dispparams.cArgs];
		dispparams.rgvarg = pArg;
		memset(pArg, 0, isizeof(VARIANT) * dispparams.cArgs);

		// Get ready to walk vararg list:
		pArg += dispparams.cArgs - 1;   // params go in opposite order
		for (int i=0; i<nParams; i++)
		{
			Assert(pArg >= dispparams.rgvarg);

			pArg->vt = pbParamInfo[i]; // set the variant type
			switch (pArg->vt)
			{
			case VT_UI1:
				pArg->bVal = va_arg(argList, BYTE);
				break;
			case VT_I2:
				pArg->iVal = va_arg(argList, short);
				break;
			case VT_I4:
				pArg->lVal = va_arg(argList, long);
				break;
			case VT_R4:
				pArg->fltVal = (float)va_arg(argList, double);
				break;
			case VT_R8:
				pArg->dblVal = va_arg(argList, double);
				break;
			case VT_DATE:
				pArg->date = va_arg(argList, DATE);
				break;
			case VT_CY:
				pArg->cyVal = *va_arg(argList, CY *);
				break;
			case VT_BSTR:
				{ // Block
					SmartBstr sbstr = va_arg(argList, BSTR);
					pArg->bstrVal = ::SysAllocString(sbstr.Chars());
				} // End block
				break;
			case VT_DISPATCH:
				pArg->pdispVal = va_arg(argList, LPDISPATCH);
				break;
			case VT_ERROR:
				pArg->scode = va_arg(argList, SCODE);
				break;
			case VT_BOOL:
				V_BOOL(pArg) = (VARIANT_BOOL)(va_arg(argList, bool) ? -1 : 0);
				break;
			case VT_VARIANT:
				*pArg = *va_arg(argList, VARIANT *);
				break;
			case VT_UNKNOWN:
				pArg->punkVal = va_arg(argList, LPUNKNOWN);
				break;

			case VT_I2 | VT_BYREF:
				pArg->piVal = va_arg(argList, short *);
				break;
			case VT_UI1 | VT_BYREF:
				pArg->pbVal = va_arg(argList, BYTE *);
				break;
			case VT_I4 | VT_BYREF:
				pArg->plVal = va_arg(argList, long *);
				break;
			case VT_R4 | VT_BYREF:
				pArg->pfltVal = va_arg(argList, float *);
				break;
			case VT_R8 | VT_BYREF:
				pArg->pdblVal = va_arg(argList, double *);
				break;
			case VT_DATE | VT_BYREF:
				pArg->pdate = va_arg(argList, DATE *);
				break;
			case VT_CY | VT_BYREF:
				pArg->pcyVal = va_arg(argList, CY *);
				break;
			case VT_BSTR | VT_BYREF:
				pArg->pbstrVal = va_arg(argList, BSTR *);
				break;
			case VT_DISPATCH | VT_BYREF:
				pArg->ppdispVal = va_arg(argList, LPDISPATCH *);
				break;
			case VT_ERROR | VT_BYREF:
				pArg->pscode = va_arg(argList, SCODE *);
				break;
			case VT_BOOL | VT_BYREF:
				{
					// coerce bool into VARIANT_BOOL
					bool * pboolVal = va_arg(argList, bool *);
					*pboolVal = *pboolVal ? MAKELONG(-1, 0) : 0;
					pArg->pboolVal = (VARIANT_BOOL *)pboolVal;
				}
				break;
			case VT_VARIANT | VT_BYREF:
				pArg->pvarVal = va_arg(argList, VARIANT *);
				break;
			case VT_UNKNOWN | VT_BYREF:
				pArg->ppunkVal = va_arg(argList, LPUNKNOWN *);
				break;

			default:
				Assert(false);  // unknown type!
				break;
			}

			--pArg; // get ready to fill next argument
		} // Next parameter
	}

	// initialize return value
	VARIANT * pvarResult = NULL;
	VARIANT vaResult;
	VariantInit(&vaResult);
	if (vtRet != VT_EMPTY)
		pvarResult = &vaResult;

	// Initialize EXCEPINFO struct
	EXCEPINFO excepInfo;
	memset(&excepInfo, 0, isizeof(excepInfo));

	UINT nArgErr = (UINT)-1;  // initialize to invalid arg

	// IMPORTANT NOTE: the following call, when m_qdisp points to the Xceed Zip module, and the
	// dwDispID is XCD_ZIP_DISPID_ZIP, may well result in an access violation error. It appears
	// that this can be ignored: simply press F5 again, and agree to pass the exception to the
	// program. (The error only occurs when in VS.)
	HRESULT hr = m_qdisp->Invoke(dwDispID, IID_NULL, 0, wFlags, &dispparams, pvarResult,
		&excepInfo, &nArgErr);

	// cleanup any arguments that need cleanup
	if (dispparams.cArgs != 0)
	{
		VARIANT * pArg = dispparams.rgvarg + dispparams.cArgs - 1;
		for (int i=0; i<nParams; i++)
		{
			switch (pbParamInfo[i])
			{
			case VT_BSTR:
				VariantClear(pArg);
				break;
			}
			--pArg;
		}
	}
	delete[] dispparams.rgvarg;

	// throw exception on failure
	if (FAILED(hr))
	{
		VariantClear(&vaResult);
		if (hr != DISP_E_EXCEPTION)
		{
			// Non-exception error code - the programmer has probably made an error.
			Assert(false);
		}

		ThrowHr(WarnHr(E_UNEXPECTED));
	}

	if (vtRet != VT_EMPTY)
	{
		// convert return value
		if (vtRet != VT_VARIANT)
		{
			HRESULT hr = VariantChangeType(&vaResult, &vaResult, 0, vtRet);
			if (FAILED(hr))
			{
				VariantClear(&vaResult);
				Assert(false);
			}
			Assert(vtRet == vaResult.vt);
		}

		// copy return value into return spot!
		switch (vtRet)
		{
		case VT_UI1:
			*(BYTE *)pvRet = vaResult.bVal;
			break;
		case VT_I2:
			*(short *)pvRet = vaResult.iVal;
			break;
		case VT_I4:
			*(long *)pvRet = vaResult.lVal;
			break;
		case VT_R4:
			*(float *)pvRet = *(float *)&vaResult.fltVal;
			break;
		case VT_R8:
			*(double *)pvRet = *(double *)&vaResult.dblVal;
			break;
		case VT_DATE:
			*(double *)pvRet = *(double *)&vaResult.date;
			break;
		case VT_CY:
			*(CY *)pvRet = vaResult.cyVal;
			break;
		case VT_BSTR:
			*(BSTR *)pvRet = vaResult.bstrVal;
			break;
		case VT_DISPATCH:
			*(LPDISPATCH *)pvRet = vaResult.pdispVal;
			break;
		case VT_ERROR:
			*(SCODE *)pvRet = vaResult.scode;
			break;
		case VT_BOOL:
			*(bool *)pvRet = (V_BOOL(&vaResult) != 0);
			break;
		case VT_VARIANT:
			*(VARIANT *)pvRet = vaResult;
			break;
		case VT_UNKNOWN:
			*(LPUNKNOWN *)pvRet = vaResult.punkVal;
			break;

		default:
			Assert(FALSE);  // invalid return type specified
		}
	}
}

//:>********************************************************************************************
//:>	ZipData methods.
//:>********************************************************************************************

//:>  This macro determines how many elements are in an array declared as VARTYPE parms[] = {..}
#define NUMPAR(par_array) isizeof(par_array) / isizeof(VARTYPE)

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ZipData::ZipData()
{
	m_hwndProgressDialog = NULL;
	m_nProgressPercent = 0;
	m_nProgressGetEvent = 0;
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
ZipData::~ZipData()
{
}

/*----------------------------------------------------------------------------------------------
	Initialize ready for a restore.
----------------------------------------------------------------------------------------------*/
void ZipData::Init(BSTR bstrDevice, BSTR bstrPath, HWND hwndProgress)
{
	m_sbstrDevice = bstrDevice;
	m_sbstrPath = bstrPath;
	m_hwndProgressDialog = hwndProgress;
}


/*----------------------------------------------------------------------------------------------
	Send data down IDispatch interface. This is no longer used, but may be useful if we ever
	want COM to create the zip event sink for us.
	@param qdisp The IDispatch interface
	@param dwDispID The ID of the dispatch method which knows how to receive the data

	NOTE: if you add any more data members, add their types to the end of the parms[] array,
	and add the data members to the end of the invh.Go() parameters.
----------------------------------------------------------------------------------------------*/
void ZipData::TransmitData(IDispatchPtr qdisp, DISPID dwDispID)
{
	static VARTYPE parms[] = { VT_BSTR, VT_I4, VT_BSTR/* <- New here */ };
	m_invh.SetIDispatch(qdisp);
	m_invh.Go(dwDispID, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL, NUMPAR(parms), parms,
		(BSTR)m_sbstrDevice, (long)m_hwndProgressDialog, (BSTR)m_sbstrPath
		/* <- New data here */);
}

/*----------------------------------------------------------------------------------------------
	Translate data received via an IDispatch interface. This is no longer used, but may be
	useful if we ever want COM to create the zip event sink for us.
	@param pDispParams The DISPPARAMS structure as received by the interface's Invoke() method.

	NOTE: if you add any more data members, follow the pattern laid out for the existing data.
----------------------------------------------------------------------------------------------*/
void ZipData::ParseReceivedParams(DISPPARAMS * pDispParams)
{
	int nArgs = pDispParams->cArgs;

	// Device:
	if (nArgs >= 1)
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
	if (nArgs >= 2)
	{
		Assert(pDispParams->rgvarg[nArgs - 3].vt == VT_I4);
		m_hwndProgressDialog = (HWND)pDispParams->rgvarg[nArgs - 3].lVal;
	}
	// Path:
	if (nArgs >= 3)
	{
		Assert(pDispParams->rgvarg[nArgs - 5].vt == VT_BSTR);
		m_sbstrPath = pDispParams->rgvarg[nArgs - 5].bstrVal;
	}
}



//:>********************************************************************************************
//:>	XceedZipSink methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
XceedZipSink::XceedZipSink()
{
	m_nOperatingMode = kidZip;
	m_cref = 1;
	m_pzipd = NULL;
	m_fUserCanceled = false;
	m_nLastDisk = -1;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
XceedZipSink::~XceedZipSink()
{
	Assert(m_cref == 1);
}

static DummyFactory g_fact(_T("SIL.AppCore.XceedZipSink"));

/*----------------------------------------------------------------------------------------------
	Initialization.
	@param pzipd Relevant information about current zip operation
----------------------------------------------------------------------------------------------*/
void XceedZipSink::Init(ZipData * pzipd)
{
	AssertPtr(pzipd);
	m_pzipd = pzipd;
	m_fUserCanceled = false;
	m_nLastDisk = -1;
}

/*----------------------------------------------------------------------------------------------
	Record operating mode.
	@param nMode Enumerated value, one of kidZip, kidUnzip, or kidReadHeader
----------------------------------------------------------------------------------------------*/
void XceedZipSink::SetOperatingMode(int nMode)
{
	m_nOperatingMode = nMode;
	m_fUserCanceled = false;
	m_nLastDisk = -1;
}

/*----------------------------------------------------------------------------------------------
	Record zip file name.
	@param strbp Zip file name, used to extract project and version details
----------------------------------------------------------------------------------------------*/
void XceedZipSink::SetZipFilename(StrAppBufPath strbp)
{
	m_strbpZipFileName = strbp;
}

/*----------------------------------------------------------------------------------------------
	IUnknown Method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XceedZipSink::QueryInterface(REFIID riid, void ** ppv)
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
STDMETHODIMP XceedZipSink::GetTypeInfoCount(UINT * pctinfo)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This method does not need to be implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XceedZipSink::GetTypeInfo(UINT iTInfo, LCID lcid, ITypeInfo ** ppTInfo)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This method does not need to be implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XceedZipSink::GetIDsOfNames(REFIID riid, LPOLESTR * rgszNames, UINT cNames,
	LCID lcid, DISPID * rgDispId)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This IDispatch method gets called when events happen in the Xceed Zip object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP XceedZipSink::Invoke(DISPID dispIdMember, REFIID riid, LCID lcid, WORD wFlags,
	DISPPARAMS * pDispParams, VARIANT * pVarResult, EXCEPINFO * pExcepInfo,
	UINT * puArgErr)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pDispParams);
	ChkComArgPtrN(pVarResult);
	ChkComArgPtrN(pExcepInfo);
	ChkComArgPtrN(puArgErr);

	AssertPtr(m_pzipd);

#ifdef XCD_ZIP_DISPID_GLOBALSTATUS	/* defined in Xceed Zip header file */
	switch (dispIdMember)
	{
	case XCD_ZIP_DISPID_GLOBALSTATUS:
		// Xceed event, which contains progress data.
		if (m_pzipd->m_hwndProgressDialog &&
			(m_nOperatingMode == kidZip || m_nOperatingMode == kidUnzip))
		{
			Assert(pDispParams->cArgs >= 3);
			Assert(pDispParams->rgvarg[2].vt == VT_I2);
			int nBytesPercent = pDispParams->rgvarg[2].iVal;
			// Update the progress dialog:
			if (m_pzipd->m_nProgressPercent)
			{
				::SendMessage(m_pzipd->m_hwndProgressDialog, m_pzipd->m_nProgressPercent,
					nBytesPercent, 0);
			}
			// Suspend if other thread is awaiting user to confirm abort:
			if (m_pzipd->m_nProgressGetEvent)
			{
				HANDLE hEvent = (HANDLE)::SendMessage(m_pzipd->m_hwndProgressDialog,
					m_pzipd->m_nProgressGetEvent, 0, 0);
				::WaitForSingleObject(hEvent, INFINITE);
			}
		}
		break;
	case XCD_ZIP_DISPID_INSERTDISK:
		// Xceed event, used when next disk is required, during spanning.
		{ // Block
			Assert(pDispParams->cArgs >= 2);
			Assert(pDispParams->rgvarg[1].vt == VT_I4);
			int nDiskNum = pDispParams->rgvarg[1].lVal;
			if (nDiskNum == m_nLastDisk)
			{
				// User has been asked for the same disk at least once:
				bool fTryAgain;
				do
				{
					fTryAgain = false;
					StrApp str;
					switch (m_nOperatingMode)
					{
					case kidZip:
						str.Load(kstidZipDiskInvalid);
						break;
					default:
						if (nDiskNum == 0)
							str.Load(kstidUnzipLastDiskInvalid);
						else
						{
							StrApp strFmt(kstidUnzipDiskInvalid);
							str.Format(strFmt.Chars(), nDiskNum, nDiskNum);
						}
						break;
					}
					StrAppBuf strbTitle(kstidZipSystem);
					if (::MessageBox(m_pzipd->m_hwndProgressDialog, str.Chars(),
						strbTitle.Chars(), MB_ICONEXCLAMATION | MB_OKCANCEL) == IDOK)
					{
						// Signal that the user has inserted the disk.
						Assert(pDispParams->rgvarg[0].vt == (VT_BOOL|VT_BYREF));
						*(pDispParams->rgvarg[0].pboolVal) = (VARIANT_BOOL)-1;
					}
					else
					{
						// User didn't want to change disk. Give option of aborting:
						StrAppBuf strbMessage;
						if (m_nOperatingMode == kidZip)
						{
							strbMessage.Load(kstidZipQueryAbort);
							strbTitle.Load(kstidZipAbort);
						}
						else
						{
							strbMessage.Load(kstidUnzipQueryAbort);
							strbTitle.Load(kstidUnzipAbort);
						}
						if (::MessageBox(m_pzipd->m_hwndProgressDialog, strbMessage.Chars(),
							strbTitle.Chars(), MB_ICONQUESTION | MB_YESNO) == IDYES)
						{
							// User opted to abort.
							m_fUserCanceled = true;
						}
						else
						{
							// User changed mind, so loop back to ask for a new disk.
							fTryAgain = true;
						}
					}
				} while (fTryAgain);
			}
			else // Requested disk is not the same as last disk
			{
				m_nLastDisk = nDiskNum;
				bool fTryAgain;
				do
				{
					fTryAgain = false;
					// Compose a relevant message, depending on disk number and current mode.
					StrApp strDrive(m_pzipd->m_sbstrDevice.Chars());
					if (strDrive.Length() > 2)
						strDrive = strDrive.Left(2);
					StrApp str;
					StrApp strFormat;
					if (nDiskNum == 0)
					{
						// If the requested disk is 'zero', this has a different meaning,
						// depending on whether we're reading or writing.
						switch (m_nOperatingMode)
						{
						case kidZip:
							// Disk is missing or write-protected:
							strFormat.Load(kstidZipDiskWriteError);
							str.Format(strFormat.Chars(), strDrive.Chars());
							break;
						default:
							// We need the last disk in the set:
							strFormat.Load(kstidUnzipInsertLastDisk);
							str.Format(strFormat.Chars(), strDrive.Chars());
							break;
						}
					}
					else // nDiskNum not zero
					{
						strFormat.Load(kstidZipInsertDiskNum);
						str.Format(strFormat.Chars(), nDiskNum, strDrive.Chars());
					}
					// Give message to user, and wait for acknowledgement (following disk
					// insertion)
					StrAppBuf strbTitle(kstidZipSystem);
					if (::MessageBox(m_pzipd->m_hwndProgressDialog, str.Chars(),
						strbTitle.Chars(), MB_ICONEXCLAMATION | MB_OKCANCEL) == IDOK)
					{
						// Make the required path exist on the disk:
						StrAppBufPath strbp;
						strbp.Assign(m_pzipd->m_sbstrPath.Chars());
						MakeDir(strbp.Chars());

						// Signal that the user has inserted the disk.
						Assert(pDispParams->rgvarg[0].vt == (VT_BOOL|VT_BYREF));
						*(pDispParams->rgvarg[0].pboolVal) = (VARIANT_BOOL)-1;
					}
					else
					{
						// User didn't want to insert a disk. Give option of aborting:
						StrAppBuf strbMessage;
						if (m_nOperatingMode == kidZip)
						{
							strbMessage.Load(kstidZipQueryAbort);
							strbTitle.Load(kstidZipAbort);
						}
						else
						{
							strbMessage.Load(kstidUnzipQueryAbort);
							strbTitle.Load(kstidUnzipAbort);
						}
						if (::MessageBox(m_pzipd->m_hwndProgressDialog, strbMessage.Chars(),
							strbTitle.Chars(), MB_ICONQUESTION | MB_YESNO) == IDYES)
						{
							// User opted to abort.
							m_fUserCanceled = true;
						}
						else
						{
							// User changed mind, so loop back to ask for a new disk.
							fTryAgain = true;
						}
						// If user agrees to abort, the fact that pDispParams->rgvarg[0].boolVal
						// has not been set to true will cause the termination of the zip
						// procedure.
					}
				} while (fTryAgain);
			}
		} // End block
		break;
	}
#endif /*XCD_ZIP_DISPID_GLOBALSTATUS*/

	return S_OK;

	END_COM_METHOD(g_fact, IID_IDispatch)
}


//:>********************************************************************************************
//:>	XceedZip methods.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Default constructor.
----------------------------------------------------------------------------------------------*/
XceedZip::XceedZip()
{
	m_nCookie = 0;
	m_pxczs = NewObj XceedZipSink;
}

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
XceedZip::XceedZip(XceedZipSink * pxczs)
{
	m_nCookie = 0;
	m_pxczs = pxczs;
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
XceedZip::~XceedZip()
{
	if (m_nCookie && m_qcnpt)
	{
		// Detach event sink:
		m_qcnpt->Unadvise(m_nCookie);
	}
	if (m_pxczs)
	{
		delete m_pxczs;
		m_pxczs = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Set up the Xceed Zip control. Then pass ZipData structure to the event sink.
	@param pzipd Configuration data.
	@param hwndProgress handle to Progress dialog.
----------------------------------------------------------------------------------------------*/
bool XceedZip::Init(ZipData * pzipd)
{
	AssertPtr(pzipd);

	// Create Xceed Zip module:
	IDispatchPtr qdisp;
	HRESULT hr;
	try{
		CheckHr(hr = ::CoCreateInstance(CLSID_IXceedZipControl, NULL, CLSCTX_ALL, IID_IDispatch, (void **)&qdisp));
	}
	catch(Throwable& thr){
		thr;
		return false;
	}

	invhZipControl.SetIDispatch(qdisp);

	// Try to license our Zip module, by reading from the encrypted license file.
	// The license file should be in FieldWorks's root directory, so find that:
	StrAppBufPath strbpLicense;
	strbpLicense.Append(DirectoryFinder::FwRootCodeDir().Chars());
	if (strbpLicense.Length() == 0 || strbpLicense[strbpLicense.Length() - 1] != '\\')
		strbpLicense.Append(_T("\\"));
	strbpLicense.Append(_T("ZipLicense.bin"));

	FILE * file;
	_tfopen_s(&file, strbpLicense.Chars(), _T("rb"));
	if (file)
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
	m_pxczs->Init(pzipd);

	// Get IUnknown pointer:
	IUnknownPtr qunkSink;
	m_pxczs->QueryInterface(IID_IUnknown, (void **)&qunkSink);

	// Register the event sink with the Xceed module:
	m_qcnpt->Advise(qunkSink, &m_nCookie);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Returns details of the specified Xceed Zip error.
	@param nErrorCode [in] The error code returned by a previous zip operation.
	@param fFatal [out] True if specified error means we are certain zip operation failed.
	@param rid [out] Resource ID of text description.
	@return True if nErrorCode was a recognized error, false if we don't know what it means.
----------------------------------------------------------------------------------------------*/
bool XceedZip::ErrorDetails(long nErrorCode, bool & fFatal, int & rid)
{
	// Assume we will recognize the error code:
	bool fFound = true;

	switch (nErrorCode)
	{

	// Various read errors:

	case xerOpenZipFile:
		rid = kstidZipErrOpen;
		fFatal = true;
		break;
	case xerSeekInZipFile: // Fall through
	case xerReadZipFile:
		rid = kstidZipErrRead;
		fFatal = true;
		break;

	// Various write errors:

	case xerWriteTempZipFile: // Fall through
	case xerWriteZipFile:
		rid = kstidZipErrWrite;
		fFatal = true;
		break;

	// Various corruption of zip errors:

	case xerEndOfZipFile: // Fall through
	case xerNotAZipFile:
		rid = kstidZipErrCorrupt;
		fFatal = true;
		break;

	// Various user-abort errors:

	case xerInsertDiskAbort:	// Fall through
	case xerUserAbort:			// Fall through
	case xerDiskNotEmptyAbort:
		rid = kstidZipErrAborted;
		fFatal = true;
		break;

	// Empty zip file:

	case xerEmptyZipFile:
		rid = kstidZipErrEmpty;
		fFatal = true;
		break;

	// Can't create temp file:

	case xerCreateTempFile:
		rid = kstidZipErrCreateTemp;
		fFatal = true;
		break;

	// Nothing to do:

	case xerNothingToDo:
		rid = kstidZipErrNothingToDo;
		fFatal = false;
		break;

	// Out of memory:

	case xerMemory:
		rid = kstidZipErrMemory;
		fFatal = true;
		break;

	// Zip issued warnings:

	case xerWarnings:
		rid = kstidZipErrWarnings;
		fFatal = false;
		break;

	// Zip skipped files:

	case xerFilesSkipped:
		rid = kstidZipErrFilesSkipped;
		fFatal = false;
		break;

	// Zip not licensed

	case xerNotLicensed:
		rid = kstidZipErrUnlicensed;
		fFatal = true;
		break;

	default:
		rid = 0;
		fFatal = false; // We can't tell if error was fatal
		fFound = false;
		break;
	}
	return fFound;
}


/*----------------------------------------------------------------------------------------------
	Test result code, and categorize and report error as appropriate.
	@param nResult The error code returned from a suspect Zip operation.
	@param fRestore True if zip operation was part of restore, false if backup.
	@return True if error is known to be fatal.
----------------------------------------------------------------------------------------------*/
bool XceedZip::TestError(long nResult, const achar * pszZipFile, bool fRestore)
{
	if (nResult == 0)
		return false;

//	HWND hwnd = m_pzipd->m_hwndProgressDialog;	// May be null.
	HWND hwnd = NULL;
	StrAppBuf strbTitle(kstidZipSystem);
	StrApp strFormat;
	StrApp strMessage;

	int ridMessage;
	bool fFatal;
	if (!ErrorDetails(nResult, fFatal, ridMessage))
	{
		strFormat.Format(_T("%r  %r"), kstidZipErrUnknown, kstidZipPossibleFailure);
		strMessage.Format(strFormat.Chars(), nResult);
		::MessageBox(hwnd, strMessage.Chars(), strbTitle.Chars(), MB_ICONERROR | MB_OK);
	}
	else
	{
		if (fFatal)
		{
			strMessage.Format(_T("%r  %r"), ridMessage, kstidZipFailure);
			if (strMessage.FindStr(_T("%s")) >= 0)
			{
				strFormat = strMessage;
				strMessage.Format(strFormat.Chars(), pszZipFile);
			}
			::MessageBox(hwnd, strMessage.Chars(), strbTitle.Chars(), MB_ICONSTOP | MB_OK);
		}
		else
		{
			strMessage.Format(_T("%r  %r"), ridMessage, kstidZipPossibleFailure);
			if (strMessage.FindStr(_T("%s")) >= 0)
			{
				strFormat = strMessage;
				strMessage.Format(strFormat.Chars(), pszZipFile);
			}
			::MessageBox(hwnd, strMessage.Chars(), strbTitle.Chars(),
				MB_ICONEXCLAMATION | MB_OK);
		}
	}
	return fFatal;
}



//:>********************************************************************************************
//:>	Wrapper methods. See Xceed Zip help for details.
//:>********************************************************************************************

// See Xceed Zip help for details.
bool XceedZip::GetAbort()
{
	bool fResult = false;
#ifdef XCD_ZIP_DISPID_ABORT
	invhZipControl.Go(XCD_ZIP_DISPID_ABORT, DISPATCH_PROPERTYGET, VT_BOOL, (void *)&fResult, 0,
		NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetAbort(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_ABORT
	invhZipControl.Go(XCD_ZIP_DISPID_ABORT, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL, NUMPAR(parms),
		parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetBasePath(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_BASEPATH
	invhZipControl.Go(XCD_ZIP_DISPID_BASEPATH, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetBasePath(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_BASEPATH
	invhZipControl.Go(XCD_ZIP_DISPID_BASEPATH, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
long XceedZip::GetCompressionLevel()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_COMPRESSIONLEVEL
	invhZipControl.Go(XCD_ZIP_DISPID_COMPRESSIONLEVEL, DISPATCH_PROPERTYGET, VT_I4,
		(void *)&nResult, 0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
void XceedZip::SetCompressionLevel(long nNewValue)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_COMPRESSIONLEVEL
	invhZipControl.Go(XCD_ZIP_DISPID_COMPRESSIONLEVEL, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, nNewValue);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetEncryptionPassword(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_ENCRYPTIONPASSWORD
	invhZipControl.Go(XCD_ZIP_DISPID_ENCRYPTIONPASSWORD, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetEncryptionPassword(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_ENCRYPTIONPASSWORD
	invhZipControl.Go(XCD_ZIP_DISPID_ENCRYPTIONPASSWORD, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
long XceedZip::GetRequiredFileAttributes()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_REQUIREDFILEATTRIBUTES
	invhZipControl.Go(XCD_ZIP_DISPID_REQUIREDFILEATTRIBUTES, DISPATCH_PROPERTYGET, VT_I4,
		(void *)&nResult, 0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
void XceedZip::SetRequiredFileAttributes(long nNewValue)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_REQUIREDFILEATTRIBUTES
	invhZipControl.Go(XCD_ZIP_DISPID_REQUIREDFILEATTRIBUTES, DISPATCH_PROPERTYPUT, VT_EMPTY,
		NULL, NUMPAR(parms), parms, nNewValue);
#endif
}

// See Xceed Zip help for details.
long XceedZip::GetExcludedFileAttributes()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_EXCLUDEDFILEATTRIBUTES
	invhZipControl.Go(XCD_ZIP_DISPID_EXCLUDEDFILEATTRIBUTES, DISPATCH_PROPERTYGET, VT_I4,
		(void *)&nResult, 0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
void XceedZip::SetExcludedFileAttributes(long nNewValue)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_EXCLUDEDFILEATTRIBUTES
	invhZipControl.Go(XCD_ZIP_DISPID_EXCLUDEDFILEATTRIBUTES, DISPATCH_PROPERTYPUT, VT_EMPTY,
		NULL, NUMPAR(parms), parms, nNewValue);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetFilesToProcess(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_FILESTOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_FILESTOPROCESS, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetFilesToProcess(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_FILESTOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_FILESTOPROCESS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetFilesToExclude(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_FILESTOEXCLUDE
	invhZipControl.Go(XCD_ZIP_DISPID_FILESTOEXCLUDE, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetFilesToExclude(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_FILESTOEXCLUDE
	invhZipControl.Go(XCD_ZIP_DISPID_FILESTOEXCLUDE, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
DATE XceedZip::GetMinDateToProcess()
{
	DATE dtResult;
#ifdef XCD_ZIP_DISPID_MINDATETOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_MINDATETOPROCESS, DISPATCH_PROPERTYGET, VT_DATE,
		(void *)&dtResult, 0, NULL);
#endif
	return dtResult;
}

// See Xceed Zip help for details.
void XceedZip::SetMinDateToProcess(DATE newValue)
{
	static VARTYPE parms[] = { VT_DATE };
#ifdef XCD_ZIP_DISPID_MINDATETOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_MINDATETOPROCESS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, newValue);
#endif
}

// See Xceed Zip help for details.
DATE XceedZip::GetMaxDateToProcess()
{
	DATE dtResult;
#ifdef XCD_ZIP_DISPID_MAXDATETOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_MAXDATETOPROCESS, DISPATCH_PROPERTYGET, VT_DATE,
		(void *)&dtResult, 0, NULL);
#endif
	return dtResult;
}

// See Xceed Zip help for details.
void XceedZip::SetMaxDateToProcess(DATE newValue)
{
	static VARTYPE parms[] = { VT_DATE };
#ifdef XCD_ZIP_DISPID_MAXDATETOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_MAXDATETOPROCESS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, newValue);
#endif
}

// See Xceed Zip help for details.
long XceedZip::GetMinSizeToProcess()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_MINSIZETOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_MINSIZETOPROCESS, DISPATCH_PROPERTYGET, VT_I4,
		(void *)&nResult, 0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
void XceedZip::SetMinSizeToProcess(long nNewValue)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_MINSIZETOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_MINSIZETOPROCESS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, nNewValue);
#endif
}

// See Xceed Zip help for details.
long XceedZip::GetMaxSizeToProcess()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_MAXSIZETOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_MAXSIZETOPROCESS, DISPATCH_PROPERTYGET, VT_I4,
		(void *)&nResult, 0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
void XceedZip::SetMaxSizeToProcess(long nNewValue)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_MAXSIZETOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_MAXSIZETOPROCESS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, nNewValue);
#endif
}

// See Xceed Zip help for details.
long XceedZip::GetSplitSize()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_SPLITSIZE
	invhZipControl.Go(XCD_ZIP_DISPID_SPLITSIZE, DISPATCH_PROPERTYGET, VT_I4, (void *)&nResult,
		0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
void XceedZip::SetSplitSize(long nNewValue)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_SPLITSIZE
	invhZipControl.Go(XCD_ZIP_DISPID_SPLITSIZE, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, nNewValue);
#endif
}

// See Xceed Zip help for details.
bool XceedZip::GetPreservePaths()
{
	bool fResult = false;
#ifdef XCD_ZIP_DISPID_PRESERVEPATHS
	invhZipControl.Go(XCD_ZIP_DISPID_PRESERVEPATHS, DISPATCH_PROPERTYGET, VT_BOOL,
		(void *)&fResult, 0, NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetPreservePaths(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_PRESERVEPATHS
	invhZipControl.Go(XCD_ZIP_DISPID_PRESERVEPATHS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
bool XceedZip::GetProcessSubfolders()
{
	bool fResult = false;
#ifdef XCD_ZIP_DISPID_PROCESSSUBFOLDERS
	invhZipControl.Go(XCD_ZIP_DISPID_PROCESSSUBFOLDERS, DISPATCH_PROPERTYGET, VT_BOOL,
		(void *)&fResult, 0, NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetProcessSubfolders(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_PROCESSSUBFOLDERS
	invhZipControl.Go(XCD_ZIP_DISPID_PROCESSSUBFOLDERS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
bool XceedZip::GetSkipIfExisting()
{
	bool fResult = false;
#ifdef XCD_ZIP_DISPID_SKIPIFEXISTING
	invhZipControl.Go(XCD_ZIP_DISPID_SKIPIFEXISTING, DISPATCH_PROPERTYGET, VT_BOOL,
		(void *)&fResult, 0, NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetSkipIfExisting(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_SKIPIFEXISTING
	invhZipControl.Go(XCD_ZIP_DISPID_SKIPIFEXISTING, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
bool XceedZip::GetSkipIfNotExisting()
{
	bool fResult = false;
#ifdef XCD_ZIP_DISPID_SKIPIFNOTEXISTING
	invhZipControl.Go(XCD_ZIP_DISPID_SKIPIFNOTEXISTING, DISPATCH_PROPERTYGET, VT_BOOL,
		(void *)&fResult, 0, NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetSkipIfNotExisting(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_SKIPIFNOTEXISTING
	invhZipControl.Go(XCD_ZIP_DISPID_SKIPIFNOTEXISTING, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
bool XceedZip::GetSkipIfOlderDate()
{
	bool fResult = false;
#ifdef XCD_ZIP_DISPID_SKIPIFOLDERDATE
	invhZipControl.Go(XCD_ZIP_DISPID_SKIPIFOLDERDATE, DISPATCH_PROPERTYGET, VT_BOOL,
		(void *)&fResult, 0, NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetSkipIfOlderDate(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_SKIPIFOLDERDATE
	invhZipControl.Go(XCD_ZIP_DISPID_SKIPIFOLDERDATE, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
bool XceedZip::GetSkipIfOlderVersion()
{
	bool fResult = false;
#ifdef XCD_ZIP_DISPID_SKIPIFOLDERVERSION
	invhZipControl.Go(XCD_ZIP_DISPID_SKIPIFOLDERVERSION, DISPATCH_PROPERTYGET, VT_BOOL,
		(void *)&fResult, 0, NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetSkipIfOlderVersion(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_SKIPIFOLDERVERSION
	invhZipControl.Go(XCD_ZIP_DISPID_SKIPIFOLDERVERSION, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetTempFolder(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_TEMPFOLDER
	invhZipControl.Go(XCD_ZIP_DISPID_TEMPFOLDER, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetTempFolder(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_TEMPFOLDER
	invhZipControl.Go(XCD_ZIP_DISPID_TEMPFOLDER, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
bool XceedZip::GetUseTempFile()
{
	bool fResult = false;
#ifdef XCD_ZIP_DISPID_USETEMPFILE
	invhZipControl.Go(XCD_ZIP_DISPID_USETEMPFILE, DISPATCH_PROPERTYGET, VT_BOOL,
		(void *)&fResult, 0, NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetUseTempFile(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_USETEMPFILE
	invhZipControl.Go(XCD_ZIP_DISPID_USETEMPFILE, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetUnzipToFolder(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_UNZIPTOFOLDER
	invhZipControl.Go(XCD_ZIP_DISPID_UNZIPTOFOLDER, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetUnzipToFolder(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_UNZIPTOFOLDER
	invhZipControl.Go(XCD_ZIP_DISPID_UNZIPTOFOLDER, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetZipFilename(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_ZIPFILENAME
	invhZipControl.Go(XCD_ZIP_DISPID_ZIPFILENAME, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetZipFilename(BSTR bstr)
{
	// Record name in Event Sink:
	StrAppBufPath strbp(bstr);
	m_pxczs->SetZipFilename(strbp);

	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_ZIPFILENAME
	invhZipControl.Go(XCD_ZIP_DISPID_ZIPFILENAME, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
long XceedZip::GetCurrentOperation()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_CURRENTOPERATION
	invhZipControl.Go(XCD_ZIP_DISPID_CURRENTOPERATION, DISPATCH_PROPERTYGET, VT_I4,
		(void *)&nResult, 0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
long XceedZip::GetSpanMultipleDisks()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_SPANMULTIPLEDISKS
	invhZipControl.Go(XCD_ZIP_DISPID_SPANMULTIPLEDISKS, DISPATCH_PROPERTYGET, VT_I4,
		(void *)&nResult, 0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
void XceedZip::SetSpanMultipleDisks(long nNewValue)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_SPANMULTIPLEDISKS
	invhZipControl.Go(XCD_ZIP_DISPID_SPANMULTIPLEDISKS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, nNewValue);
#endif
}

// See Xceed Zip help for details.
long XceedZip::GetExtraHeaders()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_EXTRAHEADERS
	invhZipControl.Go(XCD_ZIP_DISPID_EXTRAHEADERS, DISPATCH_PROPERTYGET, VT_I4,
		(void *)&nResult, 0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
void XceedZip::SetExtraHeaders(long nNewValue)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_EXTRAHEADERS
	invhZipControl.Go(XCD_ZIP_DISPID_EXTRAHEADERS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, nNewValue);
#endif
}

// See Xceed Zip help for details.
bool XceedZip::GetZipOpenedFiles()
{
	bool fResult = false;
#ifdef XCD_ZIP_DISPID_ZIPOPENEDFILES
	invhZipControl.Go(XCD_ZIP_DISPID_ZIPOPENEDFILES, DISPATCH_PROPERTYGET, VT_BOOL,
		(void *)&fResult, 0, NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetZipOpenedFiles(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_ZIPOPENEDFILES
	invhZipControl.Go(XCD_ZIP_DISPID_ZIPOPENEDFILES, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
bool XceedZip::GetBackgroundProcessing()
{
	bool fResult = false;
#ifdef XCD_ZIP_DISPID_BACKGROUNDPROCESSING
	invhZipControl.Go(XCD_ZIP_DISPID_BACKGROUNDPROCESSING, DISPATCH_PROPERTYGET, VT_BOOL,
		(void *)&fResult, 0, NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetBackgroundProcessing(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_BACKGROUNDPROCESSING
	invhZipControl.Go(XCD_ZIP_DISPID_BACKGROUNDPROCESSING, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxBinaryModule(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_SFXBINARYMODULE
	invhZipControl.Go(XCD_ZIP_DISPID_SFXBINARYMODULE, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxBinaryModule(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXBINARYMODULE
	invhZipControl.Go(XCD_ZIP_DISPID_SFXBINARYMODULE, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxButtons(long xIndex, BSTR * pbstr)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_SFXBUTTONS
	invhZipControl.Go(XCD_ZIP_DISPID_SFXBUTTONS, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, NUMPAR(parms), parms, xIndex);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxButtons(long xIndex, BSTR bstr)
{
	static VARTYPE parms[] = { VT_I4, VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXBUTTONS
	invhZipControl.Go(XCD_ZIP_DISPID_SFXBUTTONS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, xIndex, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxMessages(long xIndex, BSTR * pbstr)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_SFXMESSAGES
	invhZipControl.Go(XCD_ZIP_DISPID_SFXMESSAGES, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, NUMPAR(parms), parms, xIndex);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxMessages(long xIndex, BSTR bstr)
{
	static VARTYPE parms[] = { VT_I4, VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXMESSAGES
	invhZipControl.Go(XCD_ZIP_DISPID_SFXMESSAGES, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, xIndex, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxStrings(long xIndex, BSTR * pbstr)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_SFXSTRINGS
	invhZipControl.Go(XCD_ZIP_DISPID_SFXSTRINGS, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr,  NUMPAR(parms), parms, xIndex);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxStrings(long xIndex, BSTR bstr)
{
	static VARTYPE parms[] = { VT_I4, VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXSTRINGS
	invhZipControl.Go(XCD_ZIP_DISPID_SFXSTRINGS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, xIndex, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxDefaultPassword(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_SFXDEFAULTPASSWORD
	invhZipControl.Go(XCD_ZIP_DISPID_SFXDEFAULTPASSWORD, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxDefaultPassword(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXDEFAULTPASSWORD
	invhZipControl.Go(XCD_ZIP_DISPID_SFXDEFAULTPASSWORD, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxDefaultUnzipToFolder(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_SFXDEFAULTUNZIPTOFOLDER
	invhZipControl.Go(XCD_ZIP_DISPID_SFXDEFAULTUNZIPTOFOLDER, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxDefaultUnzipToFolder(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXDEFAULTUNZIPTOFOLDER
	invhZipControl.Go(XCD_ZIP_DISPID_SFXDEFAULTUNZIPTOFOLDER, DISPATCH_PROPERTYPUT, VT_EMPTY,
		NULL, NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
long XceedZip::GetSfxExistingFileBehavior()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_SFXEXISTINGFILEBEHAVIOR
	invhZipControl.Go(XCD_ZIP_DISPID_SFXEXISTINGFILEBEHAVIOR, DISPATCH_PROPERTYGET, VT_I4,
		(void *)&nResult, 0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
void XceedZip::SetSfxExistingFileBehavior(long nNewValue)
{
	static VARTYPE parms[] = { VT_I4 };
#ifdef XCD_ZIP_DISPID_SFXEXISTINGFILEBEHAVIOR
	invhZipControl.Go(XCD_ZIP_DISPID_SFXEXISTINGFILEBEHAVIOR, DISPATCH_PROPERTYPUT, VT_EMPTY,
		NULL, NUMPAR(parms), parms, nNewValue);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxReadmeFile(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_SFXREADMEFILE
	invhZipControl.Go(XCD_ZIP_DISPID_SFXREADMEFILE, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxReadmeFile(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXREADMEFILE
	invhZipControl.Go(XCD_ZIP_DISPID_SFXREADMEFILE, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxExecuteAfter(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_SFXEXECUTEAFTER
	invhZipControl.Go(XCD_ZIP_DISPID_SFXEXECUTEAFTER, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxExecuteAfter(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXEXECUTEAFTER
	invhZipControl.Go(XCD_ZIP_DISPID_SFXEXECUTEAFTER, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
bool XceedZip::GetSfxInstallMode()
{
	bool fResult = 0;
#ifdef XCD_ZIP_DISPID_SFXINSTALLMODE
	invhZipControl.Go(XCD_ZIP_DISPID_SFXINSTALLMODE, DISPATCH_PROPERTYGET, VT_BOOL,
		(void *)&fResult, 0, NULL);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::SetSfxInstallMode(bool fNewValue)
{
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_SFXINSTALLMODE
	invhZipControl.Go(XCD_ZIP_DISPID_SFXINSTALLMODE, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, fNewValue);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxProgramGroup(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_SFXPROGRAMGROUP
	invhZipControl.Go(XCD_ZIP_DISPID_SFXPROGRAMGROUP, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxProgramGroup(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXPROGRAMGROUP
	invhZipControl.Go(XCD_ZIP_DISPID_SFXPROGRAMGROUP, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxProgramGroupItems(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_SFXPROGRAMGROUPITEMS
	invhZipControl.Go(XCD_ZIP_DISPID_SFXPROGRAMGROUPITEMS, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxProgramGroupItems(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXPROGRAMGROUPITEMS
	invhZipControl.Go(XCD_ZIP_DISPID_SFXPROGRAMGROUPITEMS, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxExtensionsToAssociate(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_SFXEXTENSIONSTOASSOCIATE
	invhZipControl.Go(XCD_ZIP_DISPID_SFXEXTENSIONSTOASSOCIATE, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxExtensionsToAssociate(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXEXTENSIONSTOASSOCIATE
	invhZipControl.Go(XCD_ZIP_DISPID_SFXEXTENSIONSTOASSOCIATE, DISPATCH_PROPERTYPUT, VT_EMPTY,
		NULL, NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::GetSfxIconFilename(BSTR * pbstr)
{
#ifdef XCD_ZIP_DISPID_SFXICONFILENAME
	invhZipControl.Go(XCD_ZIP_DISPID_SFXICONFILENAME, DISPATCH_PROPERTYGET, VT_BSTR,
		(void *)pbstr, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SetSfxIconFilename(BSTR bstr)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXICONFILENAME
	invhZipControl.Go(XCD_ZIP_DISPID_SFXICONFILENAME, DISPATCH_PROPERTYPUT, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstr);
#endif
}

// See Xceed Zip help for details.
void XceedZip::AddFilesToProcess(BSTR bstrFileMask)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_ADDFILESTOPROCESS
	invhZipControl.Go(XCD_ZIP_DISPID_ADDFILESTOPROCESS, DISPATCH_METHOD, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstrFileMask);
#endif
}

// See Xceed Zip help for details.
void XceedZip::AddFilesToExclude(BSTR bstrFileMask)
{
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_ADDFILESTOEXCLUDE
	invhZipControl.Go(XCD_ZIP_DISPID_ADDFILESTOEXCLUDE, DISPATCH_METHOD, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstrFileMask);
#endif
}

// See Xceed Zip help for details.
long XceedZip::PreviewFiles(bool fCalcCompSize)
{
	long nResult = 0;
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_PREVIEWFILES
	invhZipControl.Go(XCD_ZIP_DISPID_PREVIEWFILES, DISPATCH_METHOD, VT_I4, (void *)&nResult,
		NUMPAR(parms), parms, fCalcCompSize);
#endif
	return nResult;
}

// See Xceed Zip help for details.
long XceedZip::ListZipContents()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_LISTZIPCONTENTS
	invhZipControl.Go(XCD_ZIP_DISPID_LISTZIPCONTENTS, DISPATCH_METHOD, VT_I4, (void *)&nResult,
		0, NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
long XceedZip::Zip()
{
	// Tell event sink that we are doing a write:
	m_pxczs->SetOperatingMode(XceedZipSink::kidZip);

	long nResult = 0;
	// IMPORTANT NOTE: the following call may well result in an access violation
	// error. It appears that this can be ignored: simply press F5 again, and agree
	// to pass the exception to the program. (The error only occurs when in VS.)
#ifdef XCD_ZIP_DISPID_ZIP
	invhZipControl.Go(XCD_ZIP_DISPID_ZIP, DISPATCH_METHOD, VT_I4, (void *)&nResult, 0, NULL);
#endif

	return nResult;
}

// See Xceed Zip help for details.
long XceedZip::Unzip()
{
	// Tell event sink that we are doing a restore:
	m_pxczs->SetOperatingMode(XceedZipSink::kidUnzip);

	long nResult = 0;
#ifdef XCD_ZIP_DISPID_UNZIP
	invhZipControl.Go(XCD_ZIP_DISPID_UNZIP, DISPATCH_METHOD, VT_I4, (void *)&nResult, 0, NULL);
#endif

	return nResult;
}

// See Xceed Zip help for details.
long XceedZip::RemoveFiles()
{
	long nResult = 0;
#ifdef XCD_ZIP_DISPID_REMOVEFILES
	invhZipControl.Go(XCD_ZIP_DISPID_REMOVEFILES, DISPATCH_METHOD, VT_I4, (void *)&nResult, 0,
		NULL);
#endif
	return nResult;
}

// See Xceed Zip help for details.
long XceedZip::TestZipFile(bool fCheckCompressedData)
{
	long nResult = 0;
	static VARTYPE parms[] = { VT_BOOL };
#ifdef XCD_ZIP_DISPID_TESTZIPFILE
	invhZipControl.Go(XCD_ZIP_DISPID_TESTZIPFILE, DISPATCH_METHOD, VT_I4, (void *)&nResult,
		NUMPAR(parms), parms, fCheckCompressedData);
#endif
	return nResult;
}

// See Xceed Zip help for details.
long XceedZip::GetZipFileInformation(long * lNbFiles, long * lCompressedSize,
	long * lUncompressedSize, short * nCompressionRatio, bool * bSpanned)
{
	long nResult = 0;
	static VARTYPE parms[] = { VT_I4|VT_BYREF, VT_I4|VT_BYREF, VT_I4|VT_BYREF, VT_I2|VT_BYREF,
		VT_BOOL|VT_BYREF };
#ifdef XCD_ZIP_DISPID_GETZIPFILEINFORMATION
	invhZipControl.Go(XCD_ZIP_DISPID_GETZIPFILEINFORMATION, DISPATCH_METHOD, VT_I4,
		(void *)&nResult, NUMPAR(parms), parms, lNbFiles, lCompressedSize, lUncompressedSize,
		nCompressionRatio, bSpanned);
#endif
	return nResult;
}

// See Xceed Zip help for details.
void XceedZip::AboutBox()
{
	invhZipControl.Go(0xfffffdd8, DISPATCH_METHOD, VT_EMPTY, NULL, 0, NULL);
}

// See Xceed Zip help for details.
void XceedZip::SfxAddProgramGroupItem(BSTR bstrApplication, BSTR bstrDescription)
{
	static VARTYPE parms[] = { VT_BSTR, VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXADDPROGRAMGROUPITEM
	invhZipControl.Go(XCD_ZIP_DISPID_SFXADDPROGRAMGROUPITEM, DISPATCH_METHOD, VT_EMPTY, NULL,
		NUMPAR(parms), parms, bstrApplication, bstrDescription);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SfxAddExtensionToAssociate(BSTR bstrDescription, BSTR bstrExtension,
										  BSTR bstrApplication)
{
	static VARTYPE parms[] = { VT_BSTR, VT_BSTR, VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXADDEXTENSIONTOASSOCIATE
	invhZipControl.Go(XCD_ZIP_DISPID_SFXADDEXTENSIONTOASSOCIATE, DISPATCH_METHOD, VT_EMPTY,
		NULL, NUMPAR(parms), parms, bstrDescription, bstrExtension,
		bstrApplication);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SfxResetButtons()
{
#ifdef XCD_ZIP_DISPID_SFXRESETBUTTONS
	invhZipControl.Go(XCD_ZIP_DISPID_SFXRESETBUTTONS, DISPATCH_METHOD, VT_EMPTY, NULL, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SfxResetMessages()
{
#ifdef XCD_ZIP_DISPID_SFXRESETMESSAGES
	invhZipControl.Go(XCD_ZIP_DISPID_SFXRESETMESSAGES, DISPATCH_METHOD, VT_EMPTY, NULL, 0,
		NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SfxResetStrings()
{
#ifdef XCD_ZIP_DISPID_SFXRESETSTRINGS
	invhZipControl.Go(XCD_ZIP_DISPID_SFXRESETSTRINGS, DISPATCH_METHOD, VT_EMPTY, NULL, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SfxClearButtons()
{
#ifdef XCD_ZIP_DISPID_SFXCLEARBUTTONS
	invhZipControl.Go(XCD_ZIP_DISPID_SFXCLEARBUTTONS, DISPATCH_METHOD, VT_EMPTY, NULL, 0, NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SfxClearMessages()
{
#ifdef XCD_ZIP_DISPID_SFXCLEARMESSAGES
	invhZipControl.Go(XCD_ZIP_DISPID_SFXCLEARMESSAGES, DISPATCH_METHOD, VT_EMPTY, NULL, 0,
		NULL);
#endif
}

// See Xceed Zip help for details.
void XceedZip::SfxClearStrings()
{
#ifdef XCD_ZIP_DISPID_SFXCLEARSTRINGS
	invhZipControl.Go(XCD_ZIP_DISPID_SFXCLEARSTRINGS, DISPATCH_METHOD, VT_EMPTY, NULL, 0, NULL);
#endif
}

// See Xceed Zip help for details.
long XceedZip::Convert(BSTR bstrDestFilename)
{
	long nResult = 0;
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_CONVERT
	invhZipControl.Go(XCD_ZIP_DISPID_CONVERT, DISPATCH_METHOD, VT_I4, (void *)&nResult,
		NUMPAR(parms), parms, bstrDestFilename);
#endif
	return nResult;
}

// See Xceed Zip help for details.
bool XceedZip::License(BSTR bstrLicense)
{
	bool fResult = false;
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_LICENSE
	invhZipControl.Go(XCD_ZIP_DISPID_LICENSE, DISPATCH_METHOD, VT_BOOL, (void *)&fResult,
		NUMPAR(parms), parms, bstrLicense);
#endif
	return fResult;
}

// See Xceed Zip help for details.
bool XceedZip::SfxLoadConfig(BSTR bstrConfigFilename)
{
	bool fResult = false;
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXLOADCONFIG
	invhZipControl.Go(XCD_ZIP_DISPID_SFXLOADCONFIG, DISPATCH_METHOD, VT_BOOL, (void *)&fResult,
		NUMPAR(parms), parms, bstrConfigFilename);
#endif
	return fResult;
}

// See Xceed Zip help for details.
bool XceedZip::SfxSaveConfig(BSTR bstrConfigFilename)
{
	bool fResult = false;
	static VARTYPE parms[] = { VT_BSTR };
#ifdef XCD_ZIP_DISPID_SFXSAVECONFIG
	invhZipControl.Go(XCD_ZIP_DISPID_SFXSAVECONFIG, DISPATCH_METHOD, VT_BOOL, (void *)&fResult,
		NUMPAR(parms), parms, bstrConfigFilename);
#endif
	return fResult;
}

// See Xceed Zip help for details.
void XceedZip::GetErrorDescription(long xType, long lValue, BSTR * pbstr)
{
	static VARTYPE parms[] = { VT_I4, VT_I4 };
#ifdef XCD_ZIP_DISPID_GETERRORDESCRIPTION
	invhZipControl.Go(XCD_ZIP_DISPID_GETERRORDESCRIPTION, DISPATCH_METHOD, VT_BSTR,
		(void *)pbstr, NUMPAR(parms), parms, xType, lValue);
#endif
}


//:>********************************************************************************************
//:>	StringEncrypter routines.
//:>********************************************************************************************

// A fairly random set of bytes with which to encrypt a string.
// NEVER CHANGE OR ADD TO THESE!
const BYTE StringEncrypter::rgkbMask[] = { 0x15,0x47,0xFD,0x1A,0xF3,0x18,0x4C,0x82,0xDF,0x1E,
	0x7B,0x3D,0x3B,0xE5,0xC2,0x41,0x2B,0x35,0x24,0x1D,0xC6,0x38,0x9C,0xC8,0xF2,0xCF,0xDA,0x85,
	0xC2,0x45,0xC4,0xB9,0x42,0x39,0xFD,0x89,0x05,0x1F,0x95,0xC4,0x0F,0xD6,0xFB,0x5F,0xAD,0xDF,
	0x02,0x1B,0x64,0xD4,0x1E,0x19,0x8D,0x1B,0x87,0x11,0xA8,0xD2,0xCB,0xBC,0xE0,0x7B,0x52,0x81,
	0x64,0x0B,0x63,0x1C,0xE6,0xFC,0xE8,0xFB,0x29,0x8B,0x95,0xD6,0xB1,0x41,0x15,0x4A,0xF5,0x6C,
	0x0D,0xF3,0xD9,0xDA,0xEF,0x69,0x9E,0xEF,0x1A,0x1B,0x1E,0xFA,0xB5,0xDE,0x89,0xC7,0x0F,0xC5,
	0xD6,0xA2,0xD1,0xB7,0x32,0x03,0xA1,0x2A,0x8A };

/*----------------------------------------------------------------------------------------------
	Encrypt a text string.
	The algorithm works by adding (modulo 0xFF) some numbers to each byte of the text string.
	The numbers to be added come from a fixed list of 'random' numbers (rgkbMask), and the count
	of how many of these random numbers are to be added also comes from the list. The first
	number read from the list is used as the counter for adding subsequent numbers to the first
	byte of the string. The next number (after the last one added) serves as the counter for
	adding subsequent numbers to the second byte of the string, and so on.
	**This algorithm only works for unicode strings where the top byte of each word is zero.**
	@param strb [in] The string to be encrypted.
	@param rgbEncryptedBytes [out] Pointer to (long-enough) buffer to receive the encryption.
	@return The seed used for the encryption algorithm, which is needed in reverse-encryption.
----------------------------------------------------------------------------------------------*/
int StringEncrypter::EncryptString(const StrAppBuf strb, BYTE * rgbEncryptedBytes)
{
	int nLen = strb.Length();
	int nSeed = abs((rand() << 16) + rand()); // A random number for the encryption seed
	// iMaskIndex will be the random index of the first number to be read from the list.
	int iMaskIndex = MulDiv(nSeed, isizeof(rgkbMask), ((RAND_MAX << 16) + RAND_MAX));
	Next(iMaskIndex); // protect against starting outside range

	for (int ib = 0; ib < nLen; ib++)
	{
		BYTE bCurrent = (BYTE)strb[ib];
		int nDuration = (int)rgkbMask[iMaskIndex];
		Next(iMaskIndex);
		for (int i=0; i<nDuration; i++)
		{
			BYTE bMask = rgkbMask[iMaskIndex];
			Next(iMaskIndex);
			bCurrent = (BYTE)(bCurrent + bMask);
		}
		rgbEncryptedBytes[ib] = bCurrent;
	}
	return nSeed;
}

/*----------------------------------------------------------------------------------------------
	Reverse the encryption on a text string. The algorithm is the exact reverse of
	EncryptString().
	@param rgbEncryptedBytes [in] Pointer to the bytes to be decrypted.
	@param nLength [in] Length of buffer to be decrypted.
	@param strb [out] Reference to the string to receive the resulting data.
	@param nSeed [in] The value that was returned when the string was originally encrypted.
----------------------------------------------------------------------------------------------*/
void StringEncrypter::DecryptString(const BYTE * rgbEncryptedBytes, int nLength,
	StrAppBuf & strb, int nSeed)
{
	int iMaskIndex = MulDiv(nSeed, isizeof(rgkbMask), (RAND_MAX << 16) + RAND_MAX);
	Next(iMaskIndex); // protect against starting outside range;

	strb.SetLength(nLength);

	for (int ib = 0; ib < nLength; ib++)
	{
		BYTE bCurrent = rgbEncryptedBytes[ib];
		int nDuration = (int)rgkbMask[iMaskIndex];
		Next(iMaskIndex);
		for (int i=0; i<nDuration; i++)
		{
			BYTE bMask = rgkbMask[iMaskIndex];
			Next(iMaskIndex);
			bCurrent = (BYTE)(bCurrent - bMask);
		}
		strb[ib] = bCurrent;
	}
}

/*----------------------------------------------------------------------------------------------
	Set the given index to the next item in the rgkbMask array, treating it as a circular
	buffer.
----------------------------------------------------------------------------------------------*/
void StringEncrypter::Next(int &i)
{
	i++;
	if (i >= isizeof(rgkbMask))
		i = 0;
}
