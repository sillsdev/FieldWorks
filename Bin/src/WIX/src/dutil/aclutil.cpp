//-------------------------------------------------------------------------------------------------
// <copyright file="aclutil.cpp" company="Microsoft">
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
//    Access Control List helper funtions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

/********************************************************************
 AclCheckAccess - determines if token has appropriate privileges

 NOTE: paa->fDenyAccess and paa->dwAccessMask are ignored and must be zero
	   if hToken is NULL, the thread will be checked
	   if hToken is not NULL the token must be an impersonation token
********************************************************************/
extern "C" HRESULT DAPI AclCheckAccess(
	__in HANDLE hToken,
	ACL_ACCESS* paa
	)
{
	Assert(paa && 0 == paa->fDenyAccess && 0 == paa->dwAccessMask);
	HRESULT hr;

	PSID psid = NULL;
	BOOL fIsMember = FALSE;

	if (paa->pwzAccountName)
	{
		hr = AclGetAccountSid(NULL, paa->pwzAccountName, &psid);
		ExitOnFailure1(hr, "failed to get SID for account: %S", paa->pwzAccountName);
	}
	else
	{
		if (!::AllocateAndInitializeSid(&paa->sia, paa->nSubAuthorityCount, paa->nSubAuthority[0], paa->nSubAuthority[1], paa->nSubAuthority[2], paa->nSubAuthority[3], paa->nSubAuthority[4], paa->nSubAuthority[5], paa->nSubAuthority[6], paa->nSubAuthority[7], &psid))
			ExitOnLastError(hr, "failed to check membership");
	}

	if (!::CheckTokenMembership(hToken, psid, &fIsMember))
		ExitOnLastError(hr, "failed to check membership");

	fIsMember ? hr = S_OK : hr = S_FALSE;
LExit:
	if (psid)
		::FreeSid(psid);  // TODO: does this have bad behavior if SID was allocated by Heap from AclGetAccountSid?

	return hr;
}


/********************************************************************
 AclCheckAdministratorAccess - determines if token has Administrator privileges

 NOTE: if hToken is NULL, the thread will be checked
	   if hToken is not NULL the token must be an impersonation token
********************************************************************/
extern "C" HRESULT DAPI AclCheckAdministratorAccess(
	__in HANDLE hToken
	)
{
	ACL_ACCESS aa;
	SID_IDENTIFIER_AUTHORITY siaNt = SECURITY_NT_AUTHORITY;

	memset(&aa, 0, sizeof(aa));
	aa.sia = siaNt;
	aa.nSubAuthorityCount = 2;
	aa.nSubAuthority[0] = SECURITY_BUILTIN_DOMAIN_RID;
	aa.nSubAuthority[1] = DOMAIN_ALIAS_RID_ADMINS;

	return AclCheckAccess(hToken, &aa);
}


/********************************************************************
 AclCheckLocalSystemAccess - determines if token has LocalSystem privileges

 NOTE: if hToken is NULL, the thread will be checked
	   if hToken is not NULL the token must be an impersonation token
********************************************************************/
extern "C" HRESULT DAPI AclCheckLocalSystemAccess(
	__in HANDLE hToken
	)
{
	ACL_ACCESS aa;
	SID_IDENTIFIER_AUTHORITY siaNt = SECURITY_NT_AUTHORITY;

	memset(&aa, 0, sizeof(aa));
	aa.sia = siaNt;
	aa.nSubAuthorityCount = 1;
	aa.nSubAuthority[0] = SECURITY_LOCAL_SYSTEM_RID;

	return AclCheckAccess(hToken, &aa);
}


/********************************************************************
 AclGetWellKnownSid - returns a SID for the specified account

********************************************************************/
extern "C" HRESULT DAPI AclGetWellKnownSid(
	__in WELL_KNOWN_SID_TYPE wkst,
	__out PSID* ppsid
	)
{
	Assert(ppsid);

	HRESULT hr = S_OK;;
	PSID psid = NULL;
	DWORD cbSid = SECURITY_MAX_SID_SIZE;

	PSID psidTemp = NULL;
#if(_WIN32_WINNT < 0x0501)
	SID_IDENTIFIER_AUTHORITY siaNT = SECURITY_NT_AUTHORITY;
	SID_IDENTIFIER_AUTHORITY siaWorld = SECURITY_WORLD_SID_AUTHORITY;
	SID_IDENTIFIER_AUTHORITY siaCreator = SECURITY_CREATOR_SID_AUTHORITY;
	BOOL fSuccess = FALSE;
#endif

	//
	// allocate memory for the SID and get it
	//
	psid = static_cast<PSID>(MemAlloc(cbSid, TRUE));
	ExitOnNull(psid, hr, E_OUTOFMEMORY, "failed allocate memory for well known SID");

#if(_WIN32_WINNT < 0x0501)
	switch (wkst)
	{
	case WinWorldSid:                 // Everyone
		fSuccess = ::AllocateAndInitializeSid(&siaWorld, 1, SECURITY_WORLD_RID, 0, 0, 0, 0, 0, 0, 0, &psidTemp);
		break;
	case WinAuthenticatedUserSid:     // Authenticated Users
		fSuccess = ::AllocateAndInitializeSid(&siaNT, 1, SECURITY_AUTHENTICATED_USER_RID, 0, 0, 0, 0, 0, 0, 0, &psidTemp);
		break;
	case WinLocalSystemSid:           // LocalSystem
		fSuccess = ::AllocateAndInitializeSid(&siaNT, 1, SECURITY_LOCAL_SYSTEM_RID, 0, 0, 0, 0, 0, 0, 0, &psidTemp);
		break;
	case WinLocalServiceSid:          // LocalService
		fSuccess = ::AllocateAndInitializeSid(&siaNT, 1, SECURITY_LOCAL_SERVICE_RID, 0, 0, 0, 0, 0, 0, 0, &psidTemp);
		break;
	case WinNetworkServiceSid:        // NetworkService
		fSuccess = ::AllocateAndInitializeSid(&siaNT, 1, SECURITY_NETWORK_SERVICE_RID, 0, 0, 0, 0, 0, 0, 0, &psidTemp);
		break;
	case WinBuiltinGuestsSid: // Guests
		fSuccess = ::AllocateAndInitializeSid(&siaNT, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_GUESTS, 0, 0, 0, 0, 0, 0, &psidTemp);
		break;
	case WinBuiltinAdministratorsSid: // Administrators
		fSuccess = ::AllocateAndInitializeSid(&siaNT, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &psidTemp);
		break;
	case WinBuiltinUsersSid:          // Users
		fSuccess = ::AllocateAndInitializeSid(&siaNT, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_USERS, 0, 0, 0, 0, 0, 0, &psidTemp);
		break;
	case WinCreatorOwnerSid:  //CREATOR OWNER
		fSuccess = ::AllocateAndInitializeSid(&siaCreator, 1, SECURITY_CREATOR_OWNER_RID, 0, 0, 0, 0, 0, 0, 0, &psidTemp);
		break;
	case WinInteractiveSid:  // INTERACTIVE
		fSuccess = ::AllocateAndInitializeSid(&siaNT, 1, SECURITY_INTERACTIVE_RID, 0, 0, 0, 0, 0, 0, 0, &psidTemp);
		break;
	default:
		ExitOnFailure1(hr = E_INVALIDARG, "unknown well known SID: %d", wkst);
	}

	if (!fSuccess)
		ExitOnLastError1(hr, "failed to allocate well known SID: %d", wkst);

	if (!::CopySid(cbSid, psid, psidTemp))
		ExitOnLastError1(hr, "failed to create well known SID: %d", wkst);
#else
	Assert(NULL == psidTemp);
	if (!::CreateWellKnownSid(wkst, NULL, psid, &cbSid))
		ExitOnLastError1(hr, "failed to create well known SID: %d", wkst);
#endif

	*ppsid = psid;
	psid = NULL;   // null it here so it won't be released below

	Assert(S_OK == hr && ::IsValidSid(*ppsid));
LExit:
	if (psidTemp)
		::FreeSid(psidTemp);
	if (psid)
		MemFree(psid);

	return hr;
}


/********************************************************************
 AclGetAccountSid - returns a SID for the specified account

********************************************************************/
extern "C" HRESULT DAPI AclGetAccountSid(
	__in LPCWSTR wzSystem,
	__in LPCWSTR wzAccount,
	__out PSID* ppsid
	)
{
	Assert(wzAccount && *wzAccount && ppsid);

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	PSID psid = NULL;
	DWORD cbSid = SECURITY_MAX_SID_SIZE;
	LPWSTR pwzDomainName = NULL;
	DWORD cbDomainName = 255;
	SID_NAME_USE peUse;

	//
	// allocate memory for the SID and domain name
	//
	psid = static_cast<PSID>(MemAlloc(cbSid, TRUE));
	ExitOnNull(psid, hr, E_OUTOFMEMORY, "failed to allocate memory for SID");
	hr = StrAlloc(&pwzDomainName, cbDomainName);
	ExitOnFailure(hr, "failed to allocate string for domain name");

	//
	// try to lookup the account now
	//
	if (!::LookupAccountNameW(wzSystem, wzAccount, psid, &cbSid, pwzDomainName, &cbDomainName, &peUse))
	{
		// if one of the buffers wasn't large enough
		er = ::GetLastError();
		if (ERROR_INSUFFICIENT_BUFFER == er)
		{
			if (SECURITY_MAX_SID_SIZE < cbSid)
			{
				PSID psidNew = static_cast<PSID>(MemReAlloc(psid, cbSid, TRUE));
				if (!psidNew)
					ExitOnLastError1(hr, "failed to allocate memory for account: %S", wzAccount);
				psid = psidNew;
			}
			if (255 < cbDomainName)
			{
				hr = StrAlloc(&pwzDomainName, cbDomainName);
				ExitOnFailure(hr, "failed to allocate string for domain name");
			}

			if (!::LookupAccountNameW(wzSystem, wzAccount, psid, &cbSid, pwzDomainName, &cbDomainName, &peUse))
				ExitOnLastError1(hr, "failed to lookup account: %S", wzAccount);
		}
		else
		{
			ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "failed to lookup account: %S", wzAccount);
		}
	}

	*ppsid = psid;
	psid = NULL;

	hr = S_OK;
LExit:
	if (pwzDomainName)
		StrFree(pwzDomainName);
	if (psid)
		MemFree(psid);

	return hr;
}


/********************************************************************
 AclGetAccountSidString - gets a string version of the user's SID

 NOTE: ppwzSid should be freed with StrFree()
********************************************************************/
extern "C" HRESULT DAPI AclGetAccountSidString(
	__in LPCWSTR wzSystem,
	__in LPCWSTR wzAccount,
	__out LPWSTR* ppwzSid
	)
{
	Assert(ppwzSid);
	HRESULT hr = S_OK;
	PSID psid = NULL;
	LPWSTR pwz = NULL;

	*ppwzSid = NULL;

	hr = AclGetAccountSid(wzSystem, wzAccount, &psid);
	ExitOnFailure1(hr, "failed to get SID for account: %S", wzAccount);
	Assert(::IsValidSid(psid));

	if (!::ConvertSidToStringSidW(psid, &pwz))
		ExitOnLastError1(hr, "failed to convert SID to string for Account: %S", wzAccount);

	hr = StrAllocString(ppwzSid, pwz, 0);
LExit:
	if (FAILED(hr))
		ReleaseNullStr(*ppwzSid);

	if (pwz)
		::LocalFree(pwz);
	if (psid)
		AclFreeSid(psid);

	return hr;
}


/********************************************************************
 AclCreateDacl - creates a DACL from ACL_ACE structures

********************************************************************/
extern "C" HRESULT DAPI AclCreateDacl(
	__in_ecount(cDeny) ACL_ACE rgaaDeny[],
	__in DWORD cDeny,
	__in_ecount(cAllow) ACL_ACE rgaaAllow[],
	__in DWORD cAllow,
	__out ACL** ppAcl
	)
{
	Assert(ppAcl);
	HRESULT hr = S_OK;
	ACL* pAcl = NULL;
	DWORD cbAcl = 0;
	DWORD i;

	*ppAcl = NULL;

	// initialize the ACL
	cbAcl = sizeof(ACL);
	for (i = 0; i < cDeny; i++)
		cbAcl += sizeof(ACCESS_DENIED_ACE) + ::GetLengthSid(rgaaDeny[i].psid) - sizeof(DWORD);
	for (i = 0; i < cAllow; i++)
		cbAcl += sizeof(ACCESS_ALLOWED_ACE) + ::GetLengthSid(rgaaAllow[i].psid) - sizeof(DWORD);

	pAcl = static_cast<ACL*>(MemAlloc(cbAcl, TRUE));
	ExitOnNull(pAcl, hr, E_OUTOFMEMORY, "failed to allocate ACL");

	if (!::InitializeAcl(pAcl, cbAcl, ACL_REVISION))
		ExitOnLastError(hr, "failed to initialize ACL");

	// add in the ACEs (denied first)
	for (i = 0; i < cDeny; i++)
	{
		if (!::AddAccessDeniedAceEx(pAcl, ACL_REVISION, rgaaDeny[i].dwFlags, rgaaDeny[i].dwMask, rgaaDeny[i].psid))
			ExitOnLastError1(hr, "failed to add access denied ACE #%d to ACL", i);
	}
	for (i = 0; i < cAllow; i++)
	{
		if (!::AddAccessAllowedAceEx(pAcl, ACL_REVISION, rgaaAllow[i].dwFlags, rgaaAllow[i].dwMask, rgaaAllow[i].psid))
			ExitOnLastError1(hr, "failed to add access allowed ACE #$d to ACL", i);
	}

	*ppAcl = pAcl;
	pAcl = NULL;
	AssertSz(::IsValidAcl(*ppAcl), "AclCreateDacl() - created invalid ACL");
	Assert(S_OK == hr);
LExit:
	if (pAcl)
		AclFreeDacl(pAcl);
	return hr;
}


/********************************************************************
 AclAddToDacl - creates a new DACL from an ACL plus new ACL_ACE structure

********************************************************************/
extern "C" HRESULT DAPI AclAddToDacl(
	__in ACL* pAcl,
	__in ACL_ACE rgaaDeny[],
	__in DWORD cDeny,
	__in ACL_ACE rgaaAllow[],
	__in DWORD cAllow,
	__out ACL** ppAclNew
	)
{
	Assert(pAcl && ::IsValidAcl(pAcl) && ppAclNew);
	HRESULT hr = S_OK;

	ACL_SIZE_INFORMATION asi;
	ACL_ACE* paaNewDeny = NULL;
	DWORD cNewDeny = 0;
	ACL_ACE* paaNewAllow = NULL;
	DWORD cNewAllow = 0;

	ACCESS_ALLOWED_ACE* paaa;
	ACCESS_DENIED_ACE* pada;
	DWORD i;

	// allocate memory for all the new ACEs (NOTE: this over calculates the memory necessary, but that's okay)
	if (!::GetAclInformation(pAcl, &asi, sizeof(asi), AclSizeInformation))
		ExitOnFailure(hr, "failed to get information about original ACL");

	if ((asi.AceCount + cDeny) < asi.AceCount || // check for overflow
		(asi.AceCount + cDeny) < cDeny || // check for overflow
		(asi.AceCount + cDeny) >= MAXSIZE_T / sizeof(ACL_ACE))
	{
		ExitOnFailure1(hr = E_OUTOFMEMORY, "Not enough memory to allocate %d ACEs", (asi.AceCount + cDeny));
	}

	paaNewDeny = static_cast<ACL_ACE*>(MemAlloc(sizeof(ACL_ACE) * (asi.AceCount + cDeny), TRUE));
	ExitOnNull(paaNewDeny, hr, E_OUTOFMEMORY, "failed to allocate memory for new deny ACEs");

	if ((asi.AceCount + cAllow) < asi.AceCount || // check for overflow
		(asi.AceCount + cAllow) < cAllow || // check for overflow
		(asi.AceCount + cAllow) >= MAXSIZE_T / sizeof(ACL_ACE))
	{
		ExitOnFailure1(hr = E_OUTOFMEMORY, "Not enough memory to allocate %d ACEs", (asi.AceCount + cAllow));
	}

	paaNewAllow = static_cast<ACL_ACE*>(MemAlloc(sizeof(ACL_ACE) * (asi.AceCount + cAllow), TRUE));
	ExitOnNull(paaNewAllow, hr, E_OUTOFMEMORY, "failed to allocate memory for new allow ACEs");

	// fill in the new structures with old data then new data (denied first)
	for (i = 0; i < asi.AceCount; i++)
	{
		if (!::GetAce(pAcl, i, reinterpret_cast<LPVOID*>(&pada)))
			ExitOnLastError1(hr, "failed to get ACE #%d from ACL", i);

		if (ACCESS_DENIED_ACE_TYPE != pada->Header.AceType)
			continue;  // skip non-denied aces

		paaNewDeny[i].dwFlags = pada->Header.AceFlags;
		paaNewDeny[i].dwMask = pada->Mask;
		paaNewDeny[i].psid = reinterpret_cast<PSID>(&(pada->SidStart));
		cNewDeny++;
	}

	memcpy(paaNewDeny + cNewDeny, rgaaDeny, sizeof(ACL_ACE) * cDeny);
	cNewDeny += cDeny;


	for (i = 0; i < asi.AceCount; i++)
	{
		if (!::GetAce(pAcl, i, reinterpret_cast<LPVOID*>(&paaa)))
			ExitOnLastError1(hr, "failed to get ACE #%d from ACL", i);

		if (ACCESS_ALLOWED_ACE_TYPE != pada->Header.AceType)
			continue;  // skip non-allowed aces

		paaNewAllow[i].dwFlags = paaa->Header.AceFlags;
		paaNewAllow[i].dwMask = paaa->Mask;
		paaNewAllow[i].psid = reinterpret_cast<PSID>(&(paaa->SidStart));
		cNewAllow++;
	}

	memcpy(paaNewAllow + cNewAllow, rgaaAllow, sizeof(ACL_ACE) * cAllow);
	cNewAllow += cAllow;

	// create the dacl with the new
	hr = AclCreateDacl(paaNewDeny, cNewDeny, paaNewAllow, cNewAllow, ppAclNew);
	ExitOnFailure(hr, "failed to create new ACL from existing ACL");

	AssertSz(::IsValidAcl(*ppAclNew), "AclAddToDacl() - created invalid ACL");
	Assert(S_OK == hr);
LExit:
	if (paaNewAllow)
		MemFree(paaNewAllow);
	if (paaNewDeny)
		MemFree(paaNewDeny);
	return hr;
}


/********************************************************************
 AclMergeDacls - creates a new DACL from two existing ACLs

********************************************************************/
extern "C" HRESULT DAPI AclMergeDacls(
	__in ACL* pAcl1,
	__in ACL* pAcl2,
	__out ACL** ppAclNew
	)
{
	Assert(pAcl1 && pAcl2 && ppAclNew);
	HRESULT hr = E_NOTIMPL;

//LExit:
	return hr;
}


/********************************************************************
 AclCreateDaclOld - creates a DACL from an ACL_ACCESS structure

********************************************************************/
extern "C" HRESULT DAPI AclCreateDaclOld(
	__in_ecount(cAclAccesses) ACL_ACCESS* paa,
	__in DWORD cAclAccesses,
	__out ACL** ppACL
	)
{
	Assert(ppACL);
	HRESULT hr = S_OK;
	DWORD* pdwAccessMask = NULL;
	PSID* ppsid = NULL;

	DWORD i;
	int cbAcl;

	*ppACL = NULL;

	//
	// create the SIDs and calculate the space for the ACL
	//
	pdwAccessMask = static_cast<DWORD*>(MemAlloc(sizeof(DWORD) * cAclAccesses, TRUE));
	ExitOnNull(pdwAccessMask, hr, E_OUTOFMEMORY, "failed allocate memory for access mask");
	ppsid = static_cast<PSID*>(MemAlloc(sizeof(PSID) * cAclAccesses, TRUE));
	ExitOnNull(ppsid, hr, E_OUTOFMEMORY, "failed allocate memory for sid");

	cbAcl = sizeof (ACL);  // start with the size of the header
	for (i = 0; i < cAclAccesses; i++)
	{
		if (paa[i].pwzAccountName)
		{
			hr = AclGetAccountSid(NULL, paa[i].pwzAccountName, ppsid + i);
			ExitOnFailure1(hr, "failed to get SID for account: %S", paa[i].pwzAccountName);
		}
		else
		{
			if ((!::AllocateAndInitializeSid(&paa[i].sia, paa[i].nSubAuthorityCount,
											 paa[i].nSubAuthority[0], paa[i].nSubAuthority[1],
											 paa[i].nSubAuthority[2], paa[i].nSubAuthority[3],
											 paa[i].nSubAuthority[4], paa[i].nSubAuthority[5],
											 paa[i].nSubAuthority[6], paa[i].nSubAuthority[7],
											 (void**)(ppsid + i))))
			{
				ExitOnLastError1(hr, "failed to initialize SIDs #%d", i);
			}
		}

		// add the newly allocated SID size to the count of bytes for this ACL
		cbAcl +=::GetLengthSid(*(ppsid + i)) - sizeof(DWORD);
		if (paa[i].fDenyAccess)
			cbAcl += sizeof(ACCESS_DENIED_ACE);
		else
			cbAcl += sizeof(ACCESS_ALLOWED_ACE);

		pdwAccessMask[i] = paa[i].dwAccessMask;
	}

	//
	// allocate the ACL and set the appropriate ACEs
	//
	*ppACL = static_cast<ACL*>(MemAlloc(cbAcl, FALSE));
	ExitOnNull(*ppACL, hr, E_OUTOFMEMORY, "failed allocate memory for ACL");

	if (!::InitializeAcl(*ppACL, cbAcl, ACL_REVISION))
		ExitOnLastError(hr, "failed to initialize ACLs");

	// add an access-allowed ACE for each of the SIDs
	for (i = 0; i < cAclAccesses; i++)
	{
		if (paa[i].fDenyAccess)
		{
			if (!::AddAccessDeniedAceEx(*ppACL, ACL_REVISION, CONTAINER_INHERIT_ACE | OBJECT_INHERIT_ACE, pdwAccessMask[i], *(ppsid + i)))
				ExitOnLastError(hr, "failed to add access denied for ACE");
		}
		else
		{
			if (!::AddAccessAllowedAceEx(*ppACL, ACL_REVISION, CONTAINER_INHERIT_ACE | OBJECT_INHERIT_ACE, pdwAccessMask[i], *(ppsid + i)))
				ExitOnLastError(hr, "failed to add access allowed for ACE");
		}
	}

LExit:
	if (FAILED(hr))
	{
		if (*ppACL)
			MemFree(*ppACL);

		*ppACL = NULL;
	}

	if (ppsid)
	{
		for (i = 0; i < cAclAccesses; i++)
			if (ppsid[i])
				::FreeSid(ppsid[i]);

		MemFree(ppsid);
	}

	if (pdwAccessMask)
		MemFree(pdwAccessMask);

	return hr;
}


/********************************************************************
 AclCreateSecurityDescriptorFromDacl - creates a self-relative security
									   descriptor from an existing DACL

********************************************************************/
extern "C" HRESULT DAPI AclCreateSecurityDescriptorFromDacl(
	__in ACL* pACL,
	__out SECURITY_DESCRIPTOR** ppsd
	)
{
	Assert(ppsd);
	HRESULT hr = S_OK;

	SECURITY_DESCRIPTOR sd;
	DWORD cbSD;

	*ppsd = NULL;

	//
	// create the absolute securtity descriptor
	//

	// initialize our security descriptor, throw the ACL into it, and set the owner
	if (!::InitializeSecurityDescriptor(&sd, SECURITY_DESCRIPTOR_REVISION) ||
		(!::SetSecurityDescriptorDacl(&sd, TRUE, pACL, FALSE)) ||
		(!::SetSecurityDescriptorOwner(&sd, NULL, FALSE)))
	{
		ExitOnLastError(hr, "failed to initialize security descriptor");
	}

	//
	// create the self-relative security descriptor
	//
	cbSD = ::GetSecurityDescriptorLength(&sd);
	*ppsd = reinterpret_cast<SECURITY_DESCRIPTOR*>(MemAlloc(cbSD, FALSE));
	ExitOnNull(*ppsd, hr, E_OUTOFMEMORY, "failed allocate memory for security descriptor");

	::MakeSelfRelativeSD(&sd, (BYTE*)*ppsd, &cbSD);
	Assert(::IsValidSecurityDescriptor(*ppsd));

LExit:
	if (FAILED(hr))
	{
		MemFree(*ppsd);
		*ppsd = NULL;
	}

	return hr;
}


/********************************************************************
 AclCreateSecurityDescriptor - creates a self-relative security descriptor from an
							  ACL_ACCESS structure

 NOTE: ppsd should be freed with AclFreeSecurityDescriptor()
********************************************************************/
extern "C" HRESULT DAPI AclCreateSecurityDescriptor(
	__in_ecount(cAclAccesses) ACL_ACCESS* paa,
	__in DWORD cAclAccesses,
	__out SECURITY_DESCRIPTOR** ppsd
	)
{
	Assert(ppsd);
	HRESULT hr = S_OK;

	ACL* pACL;

	*ppsd = NULL;

	//
	// create the DACL
	//
	hr = AclCreateDaclOld(paa, cAclAccesses, &pACL);
	ExitOnFailure(hr, "failed to create DACL for security descriptor");

	//
	// create self-relative security descriptor
	//
	hr = AclCreateSecurityDescriptorFromDacl(pACL, ppsd);

LExit:
	return hr;
}


/********************************************************************
 AclCreateSecurityDescriptorFromString - creates a self-relative security
										 descriptor from an SDDL string

 NOTE: ppsd should be freed with AclFreeSecurityDescriptor()
********************************************************************/
extern "C" HRESULT DAPI AclCreateSecurityDescriptorFromString(
	__out SECURITY_DESCRIPTOR** ppsd,
	__in LPCWSTR wzSddlFormat,
	...
	)
{
	Assert(ppsd);
	HRESULT hr = S_OK;
	LPWSTR pwzSddl = NULL;
	va_list args;
	PSECURITY_DESCRIPTOR psd = NULL;
	DWORD cbSD = 0;

	*ppsd = NULL;

	va_start(args, wzSddlFormat);
	hr = StrAllocFormattedArgs(&pwzSddl, wzSddlFormat, args);
	va_end(args);
	ExitOnFailure1(hr, "failed to create SDDL string for format: %S", wzSddlFormat);

	if (!::ConvertStringSecurityDescriptorToSecurityDescriptorW(pwzSddl, SDDL_REVISION_1, &psd, &cbSD))
		ExitOnLastError1(hr, "failed to create security descriptor from SDDL: %S", pwzSddl);

	*ppsd = reinterpret_cast<SECURITY_DESCRIPTOR*>(MemAlloc(cbSD, FALSE));
	ExitOnNull(*ppsd, hr, E_OUTOFMEMORY, "failed to allocate memory for security descriptor");

	memcpy(*ppsd, psd, cbSD);
	Assert(::IsValidSecurityDescriptor(*ppsd));

	Assert(S_OK == hr);
LExit:
	if (FAILED(hr) && *ppsd)
	{
		MemFree(*ppsd);
		*ppsd = NULL;
	}

	if (psd)
		::LocalFree(psd);
	ReleaseStr(pwzSddl);
	return hr;
}


/********************************************************************
 AclDuplicateSecurityDescriptor - creates a copy of a self-relative security descriptor

 NOTE: passed in security descriptor must be in self-relative format
********************************************************************/
extern "C" HRESULT DAPI AclDuplicateSecurityDescriptor(
	__in SECURITY_DESCRIPTOR* psd,
	__out SECURITY_DESCRIPTOR** ppsd
	)
{
	HRESULT hr = S_OK;
	DWORD cbSD;
	*ppsd = NULL;

	//
	// create the self-relative security descriptor
	//
	cbSD = ::GetSecurityDescriptorLength(psd);
	*ppsd = reinterpret_cast<SECURITY_DESCRIPTOR*>(MemAlloc(cbSD, 0));
	ExitOnNull(*ppsd, hr, E_OUTOFMEMORY, "failed allocate memory for security descriptor");

	memcpy(*ppsd, psd, cbSD);
	Assert(::IsValidSecurityDescriptor(*ppsd));

LExit:
	if (FAILED(hr))
	{
		MemFree(*ppsd);
		*ppsd = NULL;
	}

	return hr;
}


/********************************************************************
 AclGetSecurityDescriptor - returns self-relative security descriptor for named object

 NOTE: free ppsd with AclFreeSecurityDescriptor()
********************************************************************/
extern "C" HRESULT DAPI AclGetSecurityDescriptor(
	__in LPCWSTR wzObject,
	__in SE_OBJECT_TYPE sot,
	__out SECURITY_DESCRIPTOR** ppsd
	)
{
	HRESULT hr = S_OK;
	DWORD er;

	PSECURITY_DESCRIPTOR psd = NULL;
	DWORD cbSD;
	*ppsd = NULL;

	// get the security descriptor fo rth object
	er = ::GetNamedSecurityInfoW(const_cast<LPWSTR>(wzObject), sot, 0, NULL, NULL, NULL, NULL, &psd);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure1(hr, "failed to get security info from object: %S", wzObject);
	Assert(::IsValidSecurityDescriptor(psd));

	// copy the self-relative security descriptor
	cbSD = ::GetSecurityDescriptorLength(psd);
	*ppsd = reinterpret_cast<SECURITY_DESCRIPTOR*>(MemAlloc(cbSD, 0));
	ExitOnNull(*ppsd, hr, E_OUTOFMEMORY, "failed allocate memory for security descriptor");

	memcpy(*ppsd, psd, cbSD);
	Assert(::IsValidSecurityDescriptor(*ppsd));

LExit:
	if (FAILED(hr))
	{
		MemFree(*ppsd);
		*ppsd = NULL;
	}

	if (psd)
		::LocalFree(psd);

	return hr;
}


/********************************************************************
 AclFreeSid - frees a SID created by any Acl* functions

********************************************************************/
extern "C" HRESULT DAPI AclFreeSid(
	__in PSID psid
	)
{
	Assert(psid && ::IsValidSid(psid));
	HRESULT hr = S_OK;

	MemFree(psid);
	return hr;
}


/********************************************************************
 AclFreeDacl - frees a DACL created by any Acl* functions

********************************************************************/
extern "C" HRESULT DAPI AclFreeDacl(
	__in ACL* pACL
	)
{
	Assert(pACL);
	HRESULT hr = S_OK;

	MemFree(pACL);
	return hr;
}


/********************************************************************
 AclFreeSecurityDescriptor - frees a security descriptor created by any Acl* functions

********************************************************************/
extern "C" HRESULT DAPI AclFreeSecurityDescriptor(
	__in SECURITY_DESCRIPTOR* psd
	)
{
	Assert(psd && ::IsValidSecurityDescriptor(psd));
	HRESULT hr = S_OK;

	MemFree(psd);
	return hr;
}
