//-------------------------------------------------------------------------------------------------
// <copyright file="scauser.cpp" company="Microsoft">
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
//    User functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

LPCWSTR vcsUserQuery = L"SELECT `User`, `Component_`, `Name`, `Domain`, `Password`"
									   L"FROM `User` WHERE `User`=?";
enum eUserQuery { vuqUser = 1, vuqComponent, vuqName, vuqDomain, vuqPassword };

LPCWSTR vcsGroupQuery = L"SELECT `Group`, `Component_`, `Name`, `Domain`"
									   L"FROM `Group` WHERE `Group`=?";
enum eGroupQuery { vgqGroup = 1, vgqComponent, vgqName, vgqDomain };

LPCWSTR vcsUserGroupQuery = L"SELECT `User_`, `Group_`"
									   L"FROM `UserGroup` WHERE `User_`=?";
enum eUserGroupQuery { vugqUser = 1, vugqGroup };

LPCWSTR vActionableQuery = L"SELECT `User`,`Component_`,`Name`,`Domain`,`Password`,`Attributes` "
								L"FROM `User` WHERE `Component_` IS NOT NULL";
enum eActionableQuery { vaqUser = 1, vaqComponent, vaqName, vaqDomain, vaqPassword, vaqAttributes };


static HRESULT AddUserToList(
	SCA_USER** ppsuList
	);

static HRESULT AddGroupToList(
	SCA_GROUP** ppsgList
	);


HRESULT __stdcall ScaGetUser(LPCWSTR wzUser, SCA_USER* pscau)
{
	if (!wzUser || !pscau)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	PMSIHANDLE hView, hRec;

	LPWSTR pwzData = NULL;

	// clear struct and bail right away if no user key was passed to search for
	::ZeroMemory(pscau, sizeof(*pscau));
	if (!*wzUser)
	{
		hr = S_OK;
		goto LExit;
	}

	hRec = ::MsiCreateRecord(1);
	hr = WcaSetRecordString(hRec, 1, wzUser);
	ExitOnFailure(hr, "Failed to look up User");

	hr = WcaOpenView(vcsUserQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on User table");
	hr = WcaExecuteView(hView, hRec);
	ExitOnFailure(hr, "Failed to execute view on User table");

	hr = WcaFetchSingleRecord(hView, &hRec);
	if (S_OK == hr)
	{
		hr = WcaGetRecordString(hRec, vuqUser, &pwzData);
		ExitOnFailure(hr, "Failed to get User.User");
		StringCchCopyW(pscau->wzKey, countof(pscau->wzKey), pwzData);

		hr = WcaGetRecordString(hRec, vuqComponent, &pwzData);
		ExitOnFailure(hr, "Failed to get User.Component_");
		StringCchCopyW(pscau->wzComponent, countof(pscau->wzComponent), pwzData);

		hr = WcaGetRecordFormattedString(hRec, vuqName, &pwzData);
		ExitOnFailure(hr, "Failed to get User.Name");
		StringCchCopyW(pscau->wzName, countof(pscau->wzName), pwzData);

		hr = WcaGetRecordFormattedString(hRec, vuqDomain, &pwzData);
		ExitOnFailure(hr, "Failed to get User.Domain");
		StringCchCopyW(pscau->wzDomain, countof(pscau->wzDomain), pwzData);

		hr = WcaGetRecordFormattedString(hRec, vuqPassword, &pwzData);
		ExitOnFailure(hr, "Failed to get User.Password");
		StringCchCopyW(pscau->wzPassword, countof(pscau->wzPassword), pwzData);
	}
	else if (E_NOMOREITEMS == hr)
	{
		WcaLog(LOGMSG_STANDARD, "Error: Cannot locate User.User='%S'", wzUser);
		hr = E_FAIL;
	}
	else
		ExitOnFailure(hr, "Error or found multiple matching User rows");

LExit:
	ReleaseStr(pwzData);

	return hr;
}


HRESULT __stdcall ScaGetGroup(
	LPCWSTR wzGroup,
	SCA_GROUP* pscag
	)
{
	if (!wzGroup || !pscag)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	PMSIHANDLE hView, hRec;

	LPWSTR pwzData = NULL;

	hRec = ::MsiCreateRecord(1);
	hr = WcaSetRecordString(hRec, 1, wzGroup);
	ExitOnFailure(hr, "Failed to look up Group");

	hr = WcaOpenView(vcsGroupQuery, &hView);
	ExitOnFailure(hr, "Failed to open view on Group table");
	hr = WcaExecuteView(hView, hRec);
	ExitOnFailure(hr, "Failed to execute view on Group table");

	hr = WcaFetchSingleRecord(hView, &hRec);
	if (S_OK == hr)
	{
		hr = WcaGetRecordString(hRec, vgqGroup, &pwzData);
		ExitOnFailure(hr, "Failed to get Group.Group");
		StringCchCopyW(pscag->wzKey, countof(pscag->wzKey), pwzData);

		hr = WcaGetRecordString(hRec, vgqComponent, &pwzData);
		ExitOnFailure(hr, "Failed to get Group.Component_");
		StringCchCopyW(pscag->wzComponent, countof(pscag->wzComponent), pwzData);

		hr = WcaGetRecordFormattedString(hRec, vgqName, &pwzData);
		ExitOnFailure(hr, "Failed to get Group.Name");
		StringCchCopyW(pscag->wzName, countof(pscag->wzName), pwzData);

		hr = WcaGetRecordFormattedString(hRec, vgqDomain, &pwzData);
		ExitOnFailure(hr, "Failed to get Group.Domain");
		StringCchCopyW(pscag->wzDomain, countof(pscag->wzDomain), pwzData);
	}
	else if (E_NOMOREITEMS == hr)
	{
		WcaLog(LOGMSG_STANDARD, "Error: Cannot locate Group.Group='%S'", wzGroup);
		hr = E_FAIL;
	}
	else
		ExitOnFailure(hr, "Error or found multiple matching Group rows");

LExit:
	ReleaseStr(pwzData);

	return hr;
}


HRESULT ScaBuildDomainUserName(__out_ecount(cchDest) WCHAR* wzDest, __in int cchDest, __in SCA_USER* pscau)
{
	if (wzDest == NULL || cchDest == 0 || pscau == NULL)
		return E_INVALIDARG;

	// error if there is no user name
	if (pscau->wzName[0] == NULL)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	DWORD cchLeft = cchDest;
	WCHAR* pwz = wzDest;
	DWORD cchWz = cchDest;
	DWORD cch;

	if( cchLeft > MAX_DARWIN_COLUMN + 1 )
		cchLeft = MAX_DARWIN_COLUMN + 1;

	cch = lstrlenW(pscau->wzDomain);
	if (cch >= cchLeft)
	{
		hr = ERROR_MORE_DATA;
		goto LExit;
	}
	else if (cch > 0)
	{
	// handle the domain case

		StringCchCopyNW(pwz, cchWz, pscau->wzDomain, cchLeft-1);//last parameter does not include '\0'

		cchLeft -= cch;
		pwz += cch;
		cchWz -= cch;


		if (1 >= cchLeft)
		{
			hr = ERROR_MORE_DATA;
			goto LExit;
		}

		StringCchCopyNW(pwz, cchWz, L"\\", cchLeft-1);//last parameter does not include '\0'
		cchLeft--;
		++pwz; --cchWz;
	}

	cch = lstrlenW(pscau->wzName);
	if (cch >= cchLeft)
	{
		hr = ERROR_MORE_DATA;
		goto LExit;
	}

	StringCchCopyNW(pwz, cchWz, pscau->wzName, cchLeft-1);//last parameter does not include '\0'

LExit:
	return hr;
}


void ScaUserFreeList(
	SCA_USER* psuList
	)
{
	SCA_USER* psuDelete = psuList;
	while (psuList)
	{
		psuDelete = psuList;
		psuList = psuList->psuNext;

		ScaGroupFreeList(psuDelete->psgGroups);
		MemFree(psuDelete);
	}
}


void ScaGroupFreeList(
	SCA_GROUP* psgList
	)
{
	SCA_GROUP* psgDelete = psgList;
	while (psgList)
	{
		psgDelete = psgList;
		psgList = psgList->psgNext;

		MemFree(psgDelete);
	}
}


HRESULT ScaUserRead(
	SCA_USER** ppsuList
	)
{
	//Assert(FALSE);
	Assert(ppsuList);

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	PMSIHANDLE hView, hRec, hUserRec, hUserGroupView;

	LPWSTR pwzData = NULL;

	BOOL fUserGroupExists  = FALSE;

	SCA_USER *psu = NULL;
	SCA_GROUP *psg = NULL;

	INSTALLSTATE isInstalled, isAction;

	if (S_OK != WcaTableExists(L"User"))
	{
		WcaLog(LOGMSG_VERBOSE, "User Table does not exist, exiting");
		ExitFunction1(hr = S_FALSE);
	}

	if (S_OK == WcaTableExists(L"UserGroup"))
	{
		fUserGroupExists = TRUE;
	}

	//
	// loop through all the users
	//
	hr = WcaOpenExecuteView(vActionableQuery, &hView);
	ExitOnFailure(hr, "failed to open view on User table");
	while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
	{
		hr = WcaGetRecordString(hRec, vaqComponent, &pwzData);
		ExitOnFailure(hr, "failed to get User.Component");

		er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &isInstalled, &isAction);
		hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure(hr, "failed to get Component state for User");

		// don't bother if we aren't installing or uninstalling this component
		if (WcaIsInstalling(isInstalled,  isAction) || WcaIsUninstalling(isInstalled, isAction))
		{
			//
			// Add the user to the list and populate it's values
			//
			hr = AddUserToList(ppsuList);
			ExitOnFailure(hr, "failed to add user to list");

			psu = *ppsuList;

			psu->isInstalled = isInstalled;
			psu->isAction = isAction;
			hr = StringCchCopyW(psu->wzComponent, countof(psu->wzComponent), pwzData);
			ExitOnFailure1(hr, "failed to copy component name: %S", pwzData);

			hr = WcaGetRecordString(hRec, vaqUser, &pwzData);
			ExitOnFailure(hr, "failed to get User.User");
			hr = StringCchCopyW(psu->wzKey, countof(psu->wzKey), pwzData);
			ExitOnFailure1(hr, "failed to copy user key: %S", pwzData);

			hr = WcaGetRecordFormattedString(hRec, vaqName, &pwzData);
			ExitOnFailure(hr, "failed to get User.Name");
			hr = StringCchCopyW(psu->wzName, countof(psu->wzName), pwzData);
			ExitOnFailure1(hr, "failed to copy user name: %S", pwzData);

			hr = WcaGetRecordFormattedString(hRec, vaqDomain, &pwzData);
			ExitOnFailure(hr, "failed to get User.Domain");
			hr = StringCchCopyW(psu->wzDomain, countof(psu->wzDomain), pwzData);
			ExitOnFailure1(hr, "failed to copy user domain: %S", pwzData);

			hr = WcaGetRecordFormattedString(hRec, vaqPassword, &pwzData);
			ExitOnFailure(hr, "failed to get User.Password");
			hr = StringCchCopyW(psu->wzPassword, countof(psu->wzPassword), pwzData);
			ExitOnFailure(hr, "failed to copy user password");

			hr = WcaGetRecordInteger(hRec, vaqAttributes, &psu->iAttributes);
			ExitOnFailure(hr, "failed to get User.Attributes");

			// Check if this user is to be added to any groups
			if (fUserGroupExists)
			{
				hUserRec = ::MsiCreateRecord(1);
				hr = WcaSetRecordString(hUserRec, 1, psu->wzKey);
				ExitOnFailure(hr, "Failed to creat user record for querying UserGroup table");

				hr = WcaOpenView(vcsUserGroupQuery, &hUserGroupView);
				ExitOnFailure1(hr, "Failed to open view on UserGroup table for user %S", psu->wzKey);
				hr = WcaExecuteView(hUserGroupView, hUserRec);
				ExitOnFailure1(hr, "Failed to execute view on UserGroup table for user: %S", psu->wzKey);

				while (S_OK == (hr = WcaFetchRecord(hUserGroupView, &hRec)))
				{
					hr = WcaGetRecordString(hRec, vugqGroup, &pwzData);
					ExitOnFailure(hr, "failed to get UserGroup.Group");

					hr = AddGroupToList(&(psu->psgGroups));
					ExitOnFailure(hr, "failed to add group to list");

					hr = ScaGetGroup(pwzData, psu->psgGroups);
					ExitOnFailure1(hr, "failed to get information for group: %S", pwzData);
				}
				if (E_NOMOREITEMS == hr)
					hr = S_OK;
				ExitOnFailure(hr, "failed to enumerate selected rows from UserGroup table");
			}
		}
	}
	if (E_NOMOREITEMS == hr)
		hr = S_OK;
	ExitOnFailure(hr, "failed to enumerate selected rows from User table");

LExit:
	ReleaseStr(pwzData);

	return hr;
}


/* ****************************************************************
 ScaUserExecute - Schedules user account creation or removal based on
   component state.

 ******************************************************************/
 HRESULT ScaUserExecute(
	SCA_USER *psuList
	)
{
	HRESULT hr = S_OK;
	UINT er = 0;

	SCA_USER *psu = NULL;
	LPWSTR pwzActionData = NULL;
	USER_INFO_0 *pUserInfo = NULL;

	for (psu = psuList; psu; psu = psu->psuNext)
	{
		if (WcaIsInstalling(psu->isInstalled, psu->isAction))
		{
			// MMode reinstalls will fail without this
			if (psu->isInstalled == INSTALLSTATE_LOCAL)
				psu->iAttributes &= ~SCAU_FAIL_IF_EXISTS; // clear the fail if exists bit if it has been set

			// check to see if we should fail if user exists, if so check before scheduling
			if((SCAU_FAIL_IF_EXISTS & (psu->iAttributes)) && !(SCAU_UPDATE_IF_EXISTS & (psu->iAttributes)))
			{
				er = ::NetApiBufferAllocate(sizeof(USER_INFO_0), (LPVOID*)&pUserInfo);
				if(NERR_Success == er)
				{
					er = ::NetUserGetInfo(psu->wzDomain, psu->wzName, 0, (LPBYTE*)pUserInfo);

					// if user exists then fail install
					if(NERR_Success == er)
					{
						er = NERR_UserExists;
						MessageExitOnFailure1(hr = HRESULT_FROM_WIN32(er), msierrUSRFailedUserCreateExists, "failed to create user: %S due to user already exists", psu->wzName);
					}
					else if(NERR_UserNotFound == er) // clear error if User not found
					{
						er = NERR_Success;
					}

					er = ::NetApiBufferFree((LPVOID)pUserInfo);
					pUserInfo = NULL;
				}
				ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed user existence check on user: %S", psu->wzName);
			}

			//
			// Schedule the rollback first
			//
			Assert(psu->wzName && *psu->wzName);
			hr = WcaWriteStringToCaData(psu->wzName, &pwzActionData);
			ExitOnFailure1(hr, "failed to add user name to custom action data: %S", psu->wzName);
			hr = WcaWriteStringToCaData(psu->wzDomain, &pwzActionData);
			ExitOnFailure1(hr, "failed to add user domain to custom action data: %S", psu->wzDomain);

			//Should not remove user on roll back if user would not be created
			//during the install as this could lead to the removal of a pre-existing user.
			if (!(psu->iAttributes & SCAU_DONT_CREATE_USER))
			{
				// All you need for deletion is the domain and user names
				hr = WcaDoDeferredAction(L"CreateUserRollback", pwzActionData, COST_USER_DELETE);
				ExitOnFailure(hr, "failed to schedule RemoveUser");
			}

			//
			// Schedule the creation now
			//
			hr = WcaWriteStringToCaData(psu->wzPassword, &pwzActionData);
			ExitOnFailure1(hr, "failed to add user password to custom action data for user: %S", psu->wzKey);

			hr = WcaWriteIntegerToCaData(psu->iAttributes, &pwzActionData);
			ExitOnFailure1(hr, "failed to add user attributes to custom action data for user: %S", psu->wzKey);

			for (SCA_GROUP* psg = psu->psgGroups; psg; psg = psg->psgNext)
			{
				hr = WcaWriteStringToCaData(psg->wzName, &pwzActionData);
				ExitOnFailure1(hr, "failed to add guest name to custom action data: %S", psg->wzName);
				hr = WcaWriteStringToCaData(psg->wzDomain, &pwzActionData);
				ExitOnFailure1(hr, "failed to add guest domain to custom action data: %S", psg->wzDomain);
			}

			hr = WcaDoDeferredAction(L"CreateUser", pwzActionData, COST_USER_ADD);
			ExitOnFailure(hr, "failed to schedule CreateUser");
		}
		else if (WcaIsUninstalling(psu->isInstalled, psu->isAction) &&
			!((psu->iAttributes & SCAU_DONT_REMOVE_ON_UNINSTALL)  || (psu->iAttributes & SCAU_DONT_CREATE_USER)))
		{
			//
			// Schedule the removal
			//
			Assert(psu->wzName && *psu->wzName);
			hr = WcaWriteStringToCaData(psu->wzName, &pwzActionData);
			ExitOnFailure1(hr, "failed to add user name to custom action data: %S", psu->wzName);
			hr = WcaWriteStringToCaData(psu->wzDomain, &pwzActionData);
			ExitOnFailure1(hr, "failed to add user domain to custom action data: %S", psu->wzDomain);

			hr = WcaDoDeferredAction(L"RemoveUser", pwzActionData, COST_USER_DELETE);
			ExitOnFailure(hr, "failed to schedule RemoveUser");

			// Can't really roll this back which is why RemoveUser should be a deferred commit action
		}

		if (pwzActionData)
		{
			ReleaseNullStr(pwzActionData);
		}
	}

LExit:
	if (pUserInfo)
		::NetApiBufferFree((LPVOID)pUserInfo);

	ReleaseStr(pwzActionData);

	return hr;
}


static HRESULT AddUserToList(
	SCA_USER** ppsuList
	)
{
	HRESULT hr = S_OK;
	SCA_USER* psu = (SCA_USER*)MemAlloc(sizeof(SCA_USER), TRUE);
	ExitOnNull(psu, hr, E_OUTOFMEMORY, "failed to allocate memory for new user list element");

	psu->psuNext = *ppsuList;
	*ppsuList = psu;

LExit:
	return hr;
}


static HRESULT AddGroupToList(
	SCA_GROUP** ppsgList
	)
{
	HRESULT hr = S_OK;
	SCA_GROUP* psg = (SCA_GROUP*)MemAlloc(sizeof(SCA_GROUP), TRUE);
	ExitOnNull(psg, hr, E_OUTOFMEMORY, "failed to allocate memory for new group list element");

	psg->psgNext = *ppsgList;
	*ppsgList = psg;

LExit:
	return hr;
}
