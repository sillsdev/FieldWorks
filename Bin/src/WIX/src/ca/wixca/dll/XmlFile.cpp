//-------------------------------------------------------------------------------------------------
// <copyright file="XmlFile.cpp" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
//    Code to configure XML files.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define XMLFILE_CREATE_ELEMENT 0x00000001
#define XMLFILE_DELETE_VALUE 0x00000002

#define XMLFILE_DONT_UNINSTALL 0x00010000

enum eXmlAction
{
	xaOpenFile = 1,
	xaWriteValue,
	xaDeleteValue,
	xaCreateElement,
	xaDeleteElement,
};

LPCWSTR vcsXmlFileQuery =
	L"SELECT `XmlFile`, `File`, `ElementPath`, `Name`, `Value`, `Flags`, `Component_` "
	L"FROM `XmlFile` ORDER BY `File`, `Sequence`";
enum eXmlFileQuery { xfqXmlFile = 1, xfqFile, xfqXPath, xfqName, xfqValue, xfqFlags, xfqComponent  };

struct XML_FILE_CHANGE
{
	WCHAR wzId[MAX_DARWIN_KEY];

	INSTALLSTATE isInstalled;
	INSTALLSTATE isAction;

	WCHAR wzFile[MAX_PATH];
	LPWSTR pwzElementPath;
	WCHAR wzName[MAX_DARWIN_COLUMN];
	WCHAR wzValue[MAX_DARWIN_COLUMN];

	INT iFlags;

	XML_FILE_CHANGE* pxfcPrev;
	XML_FILE_CHANGE* pxfcNext;
};

static HRESULT FreeXmlFileChangeList(
	__in XML_FILE_CHANGE* pxfcList
	)
{
	HRESULT hr = S_OK;

	XML_FILE_CHANGE* pxfcDelete;
	while(pxfcList)
	{
		pxfcDelete = pxfcList;
		pxfcList = pxfcList->pxfcNext;

		if (pxfcDelete->pwzElementPath)
		{
			hr = MemFree(pxfcDelete->pwzElementPath);
			ExitOnFailure(hr, "failed to free xml file element path in change list item");
		}

		hr = MemFree(pxfcDelete);
		ExitOnFailure(hr, "failed to free xml file change list item");
	}

LExit:
	return hr;
}

static HRESULT AddXmlFileChangeToList(
	__inout XML_FILE_CHANGE** ppxfcHead,
	__inout XML_FILE_CHANGE** ppxfcTail
	)
{
	Assert(ppxfcHead && ppxfcTail);

	HRESULT hr = S_OK;

	XML_FILE_CHANGE* pxfc = (XML_FILE_CHANGE*)MemAlloc(sizeof(XML_FILE_CHANGE), TRUE);
	ExitOnNull(pxfc, hr, E_OUTOFMEMORY, "failed to allocate memory for new xml file change list element");

	// Add it to the end of the list
	if (NULL == *ppxfcHead)
	{
		*ppxfcHead = pxfc;
		*ppxfcTail = pxfc;
	}
	else
	{
		Assert(*ppxfcTail && (*ppxfcTail)->pxfcNext == NULL);
		(*ppxfcTail)->pxfcNext = pxfc;
		pxfc->pxfcPrev = *ppxfcTail;
		*ppxfcTail = pxfc;
	}

LExit:
	return hr;
}


static HRESULT ReadXmlFileTable(
	__inout XML_FILE_CHANGE** ppxfcHead,
	__inout XML_FILE_CHANGE** ppxfcTail
	)
{
	Assert(ppxfcHead && ppxfcTail);

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	PMSIHANDLE hView = NULL;
	PMSIHANDLE hRec = NULL;

	LPWSTR pwzData = NULL;

	// loop through all the xml configurations
	hr = WcaOpenExecuteView(vcsXmlFileQuery, &hView);
	ExitOnFailure(hr, "failed to open view on XmlFile table");

	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		hr = AddXmlFileChangeToList(ppxfcHead, ppxfcTail);
		ExitOnFailure(hr, "failed to add xml file change to list");

		// Get record Id
		hr = WcaGetRecordString(hRec, xfqXmlFile, &pwzData);
		ExitOnFailure(hr, "failed to get XmlFile record Id");
		hr = StringCchCopyW((*ppxfcTail)->wzId, countof((*ppxfcTail)->wzId), pwzData);
		ExitOnFailure(hr, "failed to copy XmlFile record Id");

		// Get component name
		hr = WcaGetRecordString(hRec, xfqComponent, &pwzData);
		ExitOnFailure1(hr, "failed to get component name for XmlFile: %S", (*ppxfcTail)->wzId);

		// Get the component's state
		er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &(*ppxfcTail)->isInstalled, &(*ppxfcTail)->isAction);
		ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed to get install state for Component: %S", pwzData);

		// Get the xml file
		hr = WcaGetRecordFormattedString(hRec, xfqFile, &pwzData);
		ExitOnFailure1(hr, "failed to get xml file for XmlFile: %S", (*ppxfcTail)->wzId);
		hr = StringCchCopyW((*ppxfcTail)->wzFile, countof((*ppxfcTail)->wzFile), pwzData);
		ExitOnFailure1(hr, "failed to copy xml file: %S", (*ppxfcTail)->wzFile);

		// Get the flags
		hr = WcaGetRecordInteger(hRec, xfqFlags, &(*ppxfcTail)->iFlags);
		ExitOnFailure1(hr, "failed to get Flags for XmlFile: %S", (*ppxfcTail)->wzId);

		// Get the XPath
		hr = WcaGetRecordFormattedString(hRec, xfqXPath, &(*ppxfcTail)->pwzElementPath);
		ExitOnFailure1(hr, "failed to get XPath for XmlFile: %S", (*ppxfcTail)->wzId);

		// Get the name
		hr = WcaGetRecordFormattedString(hRec, xfqName, &pwzData);
		ExitOnFailure1(hr, "failed to get Name for XmlFile: %S", (*ppxfcTail)->wzId);
		hr = StringCchCopyW((*ppxfcTail)->wzName, countof((*ppxfcTail)->wzName), pwzData);
		ExitOnFailure1(hr, "failed to copy name: %S", (*ppxfcTail)->wzName);

		// Get the value
		hr = WcaGetRecordFormattedString(hRec, xfqValue, &pwzData);
		ExitOnFailure1(hr, "failed to get Value for XmlFile: %S", (*ppxfcTail)->wzId);
		hr = StringCchCopyW((*ppxfcTail)->wzValue, countof((*ppxfcTail)->wzValue), pwzData);
		ExitOnFailure1(hr, "failed to copy value: %S", (*ppxfcTail)->wzValue);
	}

	// if we looped through all records all is well
	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "failed while looping through all objects to secure");

LExit:
	ReleaseStr(pwzData);

	return hr;
}


static HRESULT BeginChangeFile(
	__in LPCWSTR pwzFile,
	__inout LPWSTR* ppwzCustomActionData
	)
{
	Assert(pwzFile && *pwzFile && ppwzCustomActionData);

	HRESULT hr = S_OK;

	LPBYTE pbData = NULL;
	DWORD cbData = 0;

	LPWSTR pwzRollbackCustomActionData = NULL;

	hr = WcaWriteIntegerToCaData((int)xaOpenFile, ppwzCustomActionData);
	ExitOnFailure(hr, "failed to write file indicator to custom action data");

	hr = WcaWriteStringToCaData(pwzFile, ppwzCustomActionData);
	ExitOnFailure1(hr, "failed to write file to custom action data: %S", pwzFile);

	// If the file already exits, then we have to put it back the way it was on failure
	if (FileExistsEx(pwzFile, NULL))
	{
		hr = FileRead(&pbData, &cbData, pwzFile);
		ExitOnFailure1(hr, "failed to read file: %S", pwzFile);

		// Set up the rollback for this file
		hr = WcaWriteStringToCaData(pwzFile, &pwzRollbackCustomActionData);
		ExitOnFailure1(hr, "failed to write file name to rollback custom action data: %S", pwzFile);

		hr = WcaWriteStreamToCaData(pbData, cbData, &pwzRollbackCustomActionData);
		ExitOnFailure(hr, "failed to write file contents to rollback custom action data.");

		hr = WcaDoDeferredAction(L"ExecXmlFileRollback", pwzRollbackCustomActionData, COST_XMLFILE);
		ExitOnFailure1(hr, "failed to schedule ExecXmlFileRollback for file: %S", pwzFile);

		ReleaseStr(pwzRollbackCustomActionData);
	}
LExit:
	if (NULL != pbData)
		MemFree(pbData);

	return hr;
}


static HRESULT WriteChangeData(
	__in XML_FILE_CHANGE* pxfc,
	__inout LPWSTR* ppwzCustomActionData
	)
{
	Assert(pxfc && ppwzCustomActionData);

	HRESULT hr = S_OK;

	hr = WcaWriteStringToCaData(pxfc->pwzElementPath, ppwzCustomActionData);
	ExitOnFailure1(hr, "failed to write ElementPath to custom action data: %S", pxfc->pwzElementPath);

	hr = WcaWriteStringToCaData(pxfc->wzName, ppwzCustomActionData);
	ExitOnFailure1(hr, "failed to write Name to custom action data: %S", pxfc->wzName);

	hr = WcaWriteStringToCaData(pxfc->wzValue, ppwzCustomActionData);
	ExitOnFailure1(hr, "failed to write Value to custom action data: %S", pxfc->wzValue);

LExit:
	return hr;
}


/******************************************************************
 SchedXmlFile - entry point for XmlFile Custom Action

********************************************************************/
extern "C" UINT __stdcall SchedXmlFile(
	__in MSIHANDLE hInstall
	)
{
//    AssertSz(FALSE, "debug SchedXmlFile");

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	LPWSTR pwzCurrentFile = NULL;
	BOOL fCurrentFileChanged = FALSE;

	PMSIHANDLE hView = NULL;
	PMSIHANDLE hRec = NULL;

	XML_FILE_CHANGE* pxfcHead = NULL;
	XML_FILE_CHANGE* pxfcTail = NULL;
	XML_FILE_CHANGE* pxfc = NULL;
	XML_FILE_CHANGE* pxfcUninstall = NULL;

	LPWSTR pwzCustomActionData = NULL;

	DWORD cFiles = 0;

	// initialize
	hr = WcaInitialize(hInstall, "SchedXmlFile");
	ExitOnFailure(hr, "failed to initialize");

	hr = ReadXmlFileTable(&pxfcHead, &pxfcTail);
	MessageExitOnFailure(hr, msierrXmlFileFailedRead, "failed to read XmlFile table");

	// loop through all the xml configurations
	for (pxfc = pxfcHead; pxfc; pxfc = pxfc->pxfcNext)
	{
		// If this is a different file, or the first file...
		if (NULL == pwzCurrentFile || 0 != lstrcmpW(pwzCurrentFile, pxfc->wzFile))
		{
			// If this isn't the first file
			if (NULL != pwzCurrentFile)
			{
				// Do the uninstall work for the current file by walking backwards through the list (so the sequence is reversed)
				for (pxfcUninstall = pxfc->pxfcPrev; pxfcUninstall && 0 == lstrcmpW(pwzCurrentFile, pxfcUninstall->wzFile); pxfcUninstall = pxfcUninstall->pxfcPrev)
				{
					// If it's being uninstalled
					if (WcaIsUninstalling(pxfcUninstall->isInstalled, pxfcUninstall->isAction))
					{
						// Uninstall the change
						if (!(XMLFILE_DONT_UNINSTALL & pxfcUninstall->iFlags))
						{
							if (!fCurrentFileChanged)
							{
								hr = BeginChangeFile(pwzCurrentFile, &pwzCustomActionData);
								ExitOnFailure1(hr, "failed to begin file change for file: %S", pwzCurrentFile);

								fCurrentFileChanged = TRUE;
								cFiles++;
							}

							if (XMLFILE_CREATE_ELEMENT & pxfcUninstall->iFlags)
							{
								hr = WcaWriteIntegerToCaData((int)xaDeleteElement, &pwzCustomActionData);
								ExitOnFailure(hr, "failed to write delete element action indicator to custom action data");
							}
							else
							{
								hr = WcaWriteIntegerToCaData((int)xaDeleteValue, &pwzCustomActionData);
								ExitOnFailure(hr, "failed to write delete value action indicator to custom action data");
							}

							hr = WriteChangeData(pxfcUninstall, &pwzCustomActionData);
							ExitOnFailure(hr, "failed to write uninstall change data");
						}
					}
				}
			}

			// Remember the file we're currently working on
			hr = StrAllocString(&pwzCurrentFile, pxfc->wzFile, 0);
			ExitOnFailure1(hr, "failed to copy file name: %S", pxfc->wzFile);

			// We haven't changed the current file yet
			fCurrentFileChanged = FALSE;
		}

		// If it's being installed
		if (WcaIsInstalling(pxfc->isInstalled, pxfc->isAction))
		{
			if (!fCurrentFileChanged)
			{
				hr = BeginChangeFile(pwzCurrentFile, &pwzCustomActionData);
				ExitOnFailure1(hr, "failed to begin file change for file: %S", pwzCurrentFile);

				fCurrentFileChanged = TRUE;
				cFiles++;
			}

			// Install the change
			if (XMLFILE_CREATE_ELEMENT & pxfc->iFlags)
			{
				hr = WcaWriteIntegerToCaData((int)xaCreateElement, &pwzCustomActionData);
				ExitOnFailure(hr, "failed to write create element action indicator to custom action data");
			}
			else if (XMLFILE_DELETE_VALUE & pxfc->iFlags)
			{
				hr = WcaWriteIntegerToCaData((int)xaDeleteValue, &pwzCustomActionData);
				ExitOnFailure(hr, "failed to write delete value action indicator to custom action data");
			}
			else
			{
				hr = WcaWriteIntegerToCaData((int)xaWriteValue, &pwzCustomActionData);
				ExitOnFailure(hr, "failed to write file indicator to custom action data");
			}

			hr = WriteChangeData(pxfc, &pwzCustomActionData);
			ExitOnFailure(hr, "failed to write change data");
		}
	}

	// If we looped through all records all is well
	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "failed while looping through all objects to secure");

	// Schedule the custom action and add to progress bar
	if (pwzCustomActionData && *pwzCustomActionData)
	{
		Assert(0 < cFiles);

		hr = WcaDoDeferredAction(L"ExecXmlFile", pwzCustomActionData, cFiles * COST_XMLFILE);
		ExitOnFailure(hr, "failed to schedule ExecXmlFile action");
	}

LExit:
	ReleaseStr(pwzCurrentFile);
	ReleaseStr(pwzCustomActionData);

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/******************************************************************
 ExecXmlFile - entry point for XmlFile Custom Action

*******************************************************************/
extern "C" UINT __stdcall ExecXmlFile(
	__in MSIHANDLE hInstall
	)
{
//    AssertSz(FALSE, "debug ExecXmlFile");
	HRESULT hr = S_OK;
	HRESULT hrOpenFailure = S_OK;
	UINT er = ERROR_SUCCESS;

	LPWSTR pwzCustomActionData = NULL;
	LPWSTR pwzData = NULL;
	LPWSTR pwzFile = NULL;
	LPWSTR pwzXPath = NULL;
	LPWSTR pwzName = NULL;
	LPWSTR pwzValue = NULL;
	LPWSTR pwz = NULL;

	IXMLDOMDocument* pixd = NULL;
	IXMLDOMNode* pixn = NULL;
	IXMLDOMNode* pixnNewNode = NULL;

	eXmlAction xa;

	// initialize
	hr = WcaInitialize(hInstall, "ExecXmlFile");
	ExitOnFailure(hr, "failed to initialize");

	hr = XmlInitialize();
	ExitOnFailure(hr, "failed to initialize xml utilities");

	hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzCustomActionData);

	pwz = pwzCustomActionData;

	hr = WcaReadIntegerFromCaData(&pwz, (int*) &xa);
	ExitOnFailure(hr, "failed to process CustomActionData");

	if (xaOpenFile != xa)
		ExitOnFailure(hr = E_INVALIDARG, "invalid custom action data");

	// loop through all the passed in data
	while (pwz && *pwz)
	{
		hr = WcaReadStringFromCaData(&pwz, &pwzFile);
		ExitOnFailure(hr, "failed to read file name from custom action data");

		// Open the file
		ReleaseNullObject(pixd);

		hr = XmlLoadDocumentFromFile(pwzFile, &pixd);
		if (FAILED(hr))
		{
			// Ignore the return code for now.  If they try to add something, we'll fail the install.  If all they do is remove stuff then it doesn't matter.
			hrOpenFailure = hr;
			hr = S_OK;
		}
		else
		{
			hrOpenFailure = S_OK;
		}

		WcaLog(LOGMSG_VERBOSE, "Configuring Xml File: %S", pwzFile);

		while (pwz && *pwz)
		{
			hr = WcaReadIntegerFromCaData(&pwz, (int*) &xa);
			ExitOnFailure(hr, "failed to process CustomActionData");

			// Break if we need to move on to a different file
			if (xaOpenFile == xa)
				break;

			// Get path, name, and value to be written
			hr = WcaReadStringFromCaData(&pwz, &pwzXPath);
			ExitOnFailure(hr, "failed to process CustomActionData");
			hr = WcaReadStringFromCaData(&pwz, &pwzName);
			ExitOnFailure(hr, "failed to process CustomActionData");
			hr = WcaReadStringFromCaData(&pwz, &pwzValue);
			ExitOnFailure(hr, "failed to process CustomActionData");

			// If we failed to open the file and we're adding something to the file, we've got a problem.  Otherwise, just continue on since the file's already gone.
			if (FAILED(hrOpenFailure))
			{
				if (xaCreateElement == xa || xaWriteValue == xa)
				{
					MessageExitOnFailure1(hr = hrOpenFailure, msierrXmlFileFailedOpen, "failed to load XML file: %S", pwzFile);
				}
				else
				{
					continue;
				}
			}

			// Select the node we're about to modify
			ReleaseNullObject(pixn);

			hr = XmlSelectSingleNode(pixd, pwzXPath, &pixn);
			if (S_FALSE == hr)
				hr = HRESULT_FROM_WIN32(ERROR_OBJECT_NOT_FOUND);
			MessageExitOnFailure2(hr, msierrXmlFileFailedSelect, "failed to find node: %S in XML file: %S", pwzXPath, pwzFile);

			// Make the modification
			if (xaWriteValue == xa)
			{
				if (pwzName && *pwzName)
				{
					// We're setting an attribute
					hr = XmlSetAttribute(pixn, pwzName, pwzValue);
					ExitOnFailure2(hr, "failed to set attribute: %S to value %S", pwzName, pwzValue);
				}
				else
				{
					// We're setting the text of the node
					hr = XmlSetText(pixn, pwzValue);
					ExitOnFailure2(hr, "failed to set text to: %S for element %S.  Make sure that XPath points to an elment.", pwzValue, pwzXPath);
				}
			}
			else if (xaCreateElement == xa)
			{
				hr = XmlCreateChild(pixn, pwzName, &pixnNewNode);
				ExitOnFailure1(hr, "failed to create child element: %S", pwzName);

				if (pwzValue && *pwzValue)
				{
					hr = XmlSetText(pixnNewNode, pwzValue);
					ExitOnFailure2(hr, "failed to set text to: %S for node: %S", pwzValue, pwzName);
				}

				ReleaseNullObject(pixnNewNode);
			}
			else if (xaDeleteValue == xa)
			{
				if (pwzName && *pwzName)
				{
					// Delete the attribute
					hr = XmlRemoveAttribute(pixn, pwzName);
					ExitOnFailure1(hr, "failed to remove attribute: %S", pwzName);
				}
				else
				{
					// Clear the text value for the node
					hr = XmlSetText(pixn, L"");
					ExitOnFailure(hr, "failed to clear text value");
				}
			}
			else if (xaDeleteElement == xa)
			{
				// TODO: This may be a little heavy handed
				hr = XmlRemoveChildren(pixn, pwzName);
				ExitOnFailure1(hr, "failed to delete child node: %S", pwzName);
			}
			else
			{
				ExitOnFailure(hr = E_UNEXPECTED, "Invalid modification specified in custom action data");
			}
		}

		// Now that we've made all of the changes to this file, save it and move on to the next
		if (S_OK == hrOpenFailure)
		{
			hr = XmlSaveDocument(pixd, pwzFile);
			MessageExitOnFailure1(hr, msierrXmlFileFailedSave, "failed to save changes to XML file: %S", pwzFile);
		}
	}

LExit:
	ReleaseStr(pwzCustomActionData);
	ReleaseStr(pwzData);
	ReleaseStr(pwzFile);
	ReleaseStr(pwzXPath);
	ReleaseStr(pwzName);
	ReleaseStr(pwzValue);

	ReleaseObject(pixn);
	ReleaseObject(pixd);
	ReleaseObject(pixnNewNode);

	XmlUninitialize();

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}


/******************************************************************
 ExecXmlFileRollback - entry point for XmlFile rollback Custom Action

*******************************************************************/
extern "C" UINT __stdcall ExecXmlFileRollback(
	__in MSIHANDLE hInstall
	)
{
//    AssertSz(FALSE, "debug ExecXmlFileRollback");
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	LPWSTR pwzCustomActionData = NULL;
	LPWSTR pwz = NULL;
	LPWSTR pwzFileName = NULL;
	LPBYTE pbData = NULL;
	DWORD_PTR cbData = 0;
	DWORD cbDataWritten = 0;

	HANDLE hFile = INVALID_HANDLE_VALUE;

	// initialize
	hr = WcaInitialize(hInstall, "ExecXmlFile");
	ExitOnFailure(hr, "failed to initialize");


	hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
	ExitOnFailure(hr, "failed to get CustomActionData");

	WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %S", pwzCustomActionData);

	pwz = pwzCustomActionData;

	hr = WcaReadStringFromCaData(&pwz, &pwzFileName);
	ExitOnFailure(hr, "failed to read file name from custom action data");

	hr = WcaReadStreamFromCaData(&pwz, &pbData, &cbData);
	ExitOnFailure(hr, "failed to read file contents from custom action data");

	// Open the file
	hFile = ::CreateFileW(pwzFileName, GENERIC_WRITE, NULL, NULL, TRUNCATE_EXISTING, NULL, NULL);
	if (INVALID_HANDLE_VALUE == hFile)
		ExitOnLastError1(hr, "failed to open file: %S", pwzFileName);

	// Write out the old data
	if (!::WriteFile(hFile, pbData, (DWORD)cbData, &cbDataWritten, NULL))
		ExitOnLastError1(hr, "failed to write to file: %S", pwzFileName);

	Assert(cbData == cbDataWritten);

LExit:
	ReleaseStr(pwzCustomActionData);
	ReleaseStr(pwzFileName);

	if (INVALID_HANDLE_VALUE != hFile)
		::CloseHandle(hFile);

	if (NULL != pbData)
		MemFree(pbData);

	if (FAILED(hr))
		er = ERROR_INSTALL_FAILURE;
	return WcaFinalize(er);
}
