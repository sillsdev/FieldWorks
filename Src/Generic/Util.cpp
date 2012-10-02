/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Util.cpp
Responsibility: Shon Katzenberger
Last reviewed:

	Code for general utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#ifdef _MSC_VER
#include <shlobj.h>
#include <shobjidl.h>
DEFINE_COM_PTR(IShellFolder);
#include <OleDbErr.h>
#endif

#ifndef WIN32
#include <olectl.h>
#include <oledberr.h>
#endif //!WIN32

#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	Integer utilities.
***********************************************************************************************/

/*
 * table of powers of 2, and largest prime smaller than each power of 2
 *   n    2**n       prime      diff
 *  --- ----------  ----------  ----
 *   2:          4           3  ( -1)
 *   3:          8           7  ( -1)
 *   4:         16          13  ( -3)
 *   5:         32          31  ( -1)
 *   6:         64          61  ( -3)
 *   7:        128         127  ( -1)
 *   8:        256         251  ( -5)
 *   9:        512         509  ( -3)
 *  10:       1024        1021  ( -3)
 *  11:       2048        2039  ( -9)
 *  12:       4096        4093  ( -3)
 *  13:       8192        8191  ( -1)
 *  14:      16384       16381  ( -3)
 *  15:      32768       32749  (-19)
 *  16:      65536       65521  (-15)
 *  17:     131072      131071  ( -1)
 *  18:     262144      262139  ( -5)
 *  19:     524288      524287  ( -1)
 *  20:    1048576     1048573  ( -3)
 *  21:    2097152     2097143  ( -9)
 *  22:    4194304     4194301  ( -3)
 *  23:    8388608     8388593  (-15)
 *  24:   16777216    16777213  ( -3)
 *  25:   33554432    33554393  (-39)
 *  26:   67108864    67108859  ( -5)
 *  27:  134217728   134217689  (-39)
 *  28:  268435456   268435399  (-57)
 *  29:  536870912   536870909  ( -3)
 *  30: 1073741824  1073741789  (-35)
 *  31: 2147483648  2147483647  ( -1)
 *  32: 4294967296  4294967291	( -5)
 */
const static uint g_rguPrimes[] = {
	3, 7, 13, 31, 61, 127, 251, 509, 1021, 2039, 4093,	8191, 16381, 32749, 65521, 131071,
	262139, 524287, 1048573, 2097143, 4194301, 8388593, 16777213, 33554393, 67108859,
	134217689, 268435399, 536870909, 1073741789, 2147483647, 4294967291U
};


/*----------------------------------------------------------------------------------------------
	Returns the prime in g_rguPrimes that is closest to u.
----------------------------------------------------------------------------------------------*/
uint GetPrimeNear(uint u)
{
	int cu = isizeof(g_rguPrimes) / isizeof(uint);
	int iuMin;
	int iuLim;
	int iu;

	for (iuMin = 0, iuLim = cu; iuMin < iuLim; )
	{
		iu = (iuMin + iuLim) / 2;
		if (u > g_rguPrimes[iu])
			iuMin = iu + 1;
		else
			iuLim = iu;
	}
	Assert(iuMin == cu || iuMin < cu && u <= g_rguPrimes[iuMin]);
	Assert(iuMin == 0 || iuMin > 0 && u > g_rguPrimes[iuMin - 1]);

	if (!iuMin)
		return g_rguPrimes[0];
	if (iuMin == cu)
		return g_rguPrimes[cu - 1];
	if (g_rguPrimes[iuMin] - u < u - g_rguPrimes[iuMin - 1])
		return g_rguPrimes[iuMin];
	return g_rguPrimes[iuMin - 1];
}


/*----------------------------------------------------------------------------------------------
	Returns the prime in g_rguPrimes that is larger than u or is the largest in the list.
----------------------------------------------------------------------------------------------*/
uint GetLargerPrime(uint u)
{
	int cu = isizeof(g_rguPrimes) / isizeof(uint);
	int iuMin;
	int iuLim;
	int iu;

	for (iuMin = 0, iuLim = cu; iuMin < iuLim; )
	{
		iu = (iuMin + iuLim) / 2;
		if (u >= g_rguPrimes[iu])
			iuMin = iu + 1;
		else
			iuLim = iu;
	}
	Assert(iuMin == cu || iuMin < cu && u < g_rguPrimes[iuMin]);
	Assert(iuMin == 0 || iuMin > 0 && u >= g_rguPrimes[iuMin - 1]);

	if (iuMin == cu)
		return g_rguPrimes[cu - 1];
	return g_rguPrimes[iuMin];
}


/*----------------------------------------------------------------------------------------------
	Returns the prime in g_rguPrimes that is smaller than u or is the smallest in the list.
----------------------------------------------------------------------------------------------*/
uint GetSmallerPrime(uint u)
{
	int cu = isizeof(g_rguPrimes) / isizeof(uint);
	int iuMin;
	int iuLim;
	int iu;

	for (iuMin = 0, iuLim = cu; iuMin < iuLim; )
	{
		iu = (iuMin + iuLim) / 2;
		if (u > g_rguPrimes[iu])
			iuMin = iu + 1;
		else
			iuLim = iu;
	}
	Assert(iuMin == cu || iuMin < cu && u <= g_rguPrimes[iuMin]);
	Assert(iuMin == 0 || iuMin > 0 && u > g_rguPrimes[iuMin - 1]);

	if (!iuMin)
		return g_rguPrimes[0];
	return g_rguPrimes[iuMin - 1];
}


/*----------------------------------------------------------------------------------------------
	Calcuates the GCD of two unsigned integers.
----------------------------------------------------------------------------------------------*/
uint GetGcdU(uint u1, uint u2)
{
	// Euclidean algorithm - keep mod'ing until we hit zero.
	if (!u1)
	{
		// If both are zero, return 1.
		return !u2 ? 1 : u2;
	}

	for (;;)
	{
		if (0 == (u2 %= u1))
			return u1;
		if (0 == (u1 %= u2))
			return u2;
	}
}


/***********************************************************************************************
	Hash functions.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	Computes a hash value for an array of bytes. uHash can be passed in to effectively combine
	hash values from different pieces of data.
----------------------------------------------------------------------------------------------*/
uint ComputeHashRgb(const byte * prgb, int cb, uint uHash)
{
	const uint * pu = (const uint *)prgb;
	const uint * puLim = pu + cb / isizeof(uint);

	for ( ; pu < puLim; pu++)
		uHash = uHash * 4097 + *pu;
	Assert(pu == puLim);

	if (!(cb & 3))
		return uHash;

	const byte * pb = (const byte *)pu;

	if (cb & 2)
	{
		uHash = uHash * 127 + *(ushort *)pb;
		pb += 2;
	}
	if (cb & 1)
		uHash = uHash * 17 + *pb;

	return uHash;
}


/*----------------------------------------------------------------------------------------------
	Computes a case sensitive hash function on the string.
----------------------------------------------------------------------------------------------*/
uint CaseSensitiveComputeHash(LPCOLESTR psz, uint uHash)
{
	UOLECHAR ch;

	while (0 != (ch = *(UOLECHAR *) psz++))
		uHash = 17 * uHash + ch;
	return uHash;
}


/*----------------------------------------------------------------------------------------------
	Computes a case sensitive hash function on the string.
----------------------------------------------------------------------------------------------*/
uint CaseSensitiveComputeHashCch(const OLECHAR * prgch, int cch, uint uHash)
{
	while (cch-- > 0)
		uHash = 17 * uHash + *(UOLECHAR *) prgch++;
	return uHash;
}


/*----------------------------------------------------------------------------------------------
	Computes a case insensitive hash function on the string. This only deals with the roman
	letters A to Z.
----------------------------------------------------------------------------------------------*/
uint CaseInsensitiveComputeHash(LPCOLESTR psz, uint uHash )
{
	UOLECHAR ch;

	while (0 != (ch = *(UOLECHAR *) psz++)) {
		if (ch <= 'Z' && ch >= 'A')
			ch += 'a' - 'A';
		uHash = 17 * uHash + ch;
	}
	return uHash;
}


/*----------------------------------------------------------------------------------------------
	Computes a case insensitive hash function on the string. This only deals with the roman
	letters A to Z.
----------------------------------------------------------------------------------------------*/
uint CaseInsensitiveComputeHashCch(const OLECHAR * prgch, int cch, uint uHash)
{
	UOLECHAR ch;

	while (cch-- > 0) {
		ch = *(UOLECHAR *) prgch++;
		if (ch <= 'Z' && ch >= 'A')
			ch += 'a' - 'A';
		uHash = 17 * uHash + ch;
	}
	return uHash;
}


/*************************************************************************************
	COM Utilities.
*************************************************************************************/
bool SameObject(IUnknown *punk1, IUnknown *punk2)
{
	AssertPtrN(punk1);
	AssertPtrN(punk2);
	if (punk1 == punk2)
		return true;
	if (NULL == punk1 || NULL == punk2)
		return false;

	IUnknown *punkCtl2;
	if (FAILED(punk2->QueryInterface(IID_IUnknown, (void **)&punkCtl2)))
		return false;
	AssertPtr(punkCtl2);
	punkCtl2->Release();
	if (punk1 == punkCtl2)
		return true;
	IUnknown *punkCtl1;
	if (FAILED(punk1->QueryInterface(IID_IUnknown, (void **)&punkCtl1)))
		return false;
	AssertPtr(punkCtl1);
	punkCtl1->Release();
	return punkCtl1 == punkCtl2;
}


/*************************************************************************************
	Persistance Utilities.
*************************************************************************************/

template void ReadString(IStream * pstrm, StrBase<wchar> & stb);
template void ReadString(IStream * pstrm, StrBase<schar> & stb);
template void WriteString(IStream * pstrm, StrBase<wchar> & stb);
template void WriteString(IStream * pstrm, StrBase<schar> & stb);

template<typename XChar>
	void ReadString(IStream * pstrm, StrBase<XChar> & stb)
{
	AssertPtr(pstrm);

	// Read the length of the string.
	int cch;
	ReadInt(pstrm, &cch);

	if (cch < 0)
		ThrowHr(WarnHr(E_UNEXPECTED));

	// Read the contents of the string.
	stb.ReadChars(pstrm, cch);
}


template<typename XChar>
	void WriteString(IStream * pstrm, StrBase<XChar> & stb)
{
	AssertPtr(pstrm);

	int cch = stb.Length();

	// Write the length of the string.
	WriteInt(pstrm, cch);

	// Write the contents of the string.
	WriteBuf(pstrm, stb.Chars(), cch * isizeof(XChar));
}


#if WIN32
/*----------------------------------------------------------------------------------------------
	Take a string and resolve it to a full path name.
	Example: d:\work\src\test\test.exe\..\..\test.kb is output as d:\work\src\test.kb
	Arguments:
		psz		Null-terminated input string
		staPath	A pointer to a StrAnsi holding the output string
----------------------------------------------------------------------------------------------*/
void GetFullPathName(const achar * psz, StrAnsi & staPath)
{
	achar szPath[256];
	ULONG cch;

	cch = GetFullPathName(psz, isizeof(szPath), szPath, NULL);
	// Fail if the function fails or the szPath buffer is too small.
	if (!cch || cch > isizeof(szPath))
		ThrowHr(WarnHr(E_UNEXPECTED));

	staPath.Assign(szPath);
}
#endif // WIN32


/*----------------------------------------------------------------------------------------------
	Copy bytes from one stream to another.
----------------------------------------------------------------------------------------------*/
void CopyBytes(IStream * pstrmSrc, IStream * pstrmDst, int cb)
{
	AssertPtr(pstrmSrc);
	AssertPtr(pstrmDst);
	Assert(!SameObject(pstrmSrc, pstrmDst));

	byte rgb[1024];
	int cbT;

	while (cb > 0)
	{
		cbT = Min(cb, (int)isizeof(rgb));
		ReadBuf(pstrmSrc, rgb, cbT);
		WriteBuf(pstrmDst, rgb, cbT);
		cb -= cbT;
	}
}


/*----------------------------------------------------------------------------------------------
	Fill bytes in the stream.
----------------------------------------------------------------------------------------------*/
void FillBytes(IStream * pstrm, byte b, int cb)
{
	AssertPtr(pstrm);

	byte rgb[1024];
	int cbT;

	FillBytes(rgb, b, Min(cb, isizeof(rgb)));
	while (cb > 0)
	{
		cbT = Min(cb, isizeof(rgb));
		WriteBuf(pstrm, rgb, cbT);
		cb -= cbT;
	}
}


#ifdef _MSC_VER
/*************************************************************************************
	Registry Utilities.
*************************************************************************************/
int DeleteSubKey(HKEY hk, const achar *psz)
{
	achar szBuf[MAX_PATH + 1];
	HKEY hkSub;

	if (0 == RegOpenKeyEx(hk, psz, 0, KEY_READ | KEY_WRITE, &hkSub))
	{
		while (0 == RegEnumKey(hkSub, 0, szBuf, isizeof(szBuf)))
		{
			if (DeleteSubKey(hkSub, szBuf) == ERROR_ACCESS_DENIED)
				break;
		}
		RegCloseKey(hkSub);
	}
	return RegDeleteKey(hk, psz);
}


/*************************************************************************************
	Type Library Utilities.
*************************************************************************************/
void TypeInfoHolder::GetTI(ITypeInfo ** ppti)
{
	AssertPtr(ppti);
	AssertPtr(m_piid);

	*ppti = NULL;

	if (!m_pti)
		LoadTypeInfo(m_ridTypeLib, *m_piid, &m_pti);

	*ppti = m_pti;
	(*ppti)->AddRef();
}


/*************************************************************************************
	Hook the dll entry points to register and unregister the type libraries.
*************************************************************************************/
static void RegisterAllTypeLibraries(BOOL fRegister);


class TypeLibraryModuleEntry : public ModuleEntry
{
public:
	virtual void RegisterServer(void)
		{  ::RegisterAllTypeLibraries(true); }
	virtual void UnregisterServer(void)
		{ ::RegisterAllTypeLibraries(false); }
};


static TypeLibraryModuleEntry g_tlde;


/*************************************************************************************
	Loads the type library with resource id = rid. If rid is 0, the default type
	library is loaded.
*************************************************************************************/
static void LoadTypeLibraryCore(int rid, ITypeLib ** pptl, BOOL fRegister)
{
	AssertPtr(pptl);

	StrUniBufPath stubp;
	HRESULT hr;

	*pptl = NULL;

	if (rid > 1)
		stubp.Format(L"%S\\%d", ModuleEntry::GetModulePathName(), rid);
	else
		stubp.Assign(ModuleEntry::GetModulePathName());

	hr = LoadTypeLibEx(stubp.Chars(), REGKIND_NONE, pptl);
	if (FAILED(hr))
	{
		*pptl = NULL;
		ThrowHr(hr);
	}

	if (fRegister)
	{
		Assert(rid != 0);

		// Handle registering in HKCU instead of HKCR. This is only supported on W2003 and Vista so we have
		// to get the function pointer dynamically
		typedef HRESULT (STDAPICALLTYPE *PFNREGISTERTYPELIB)(ITypeLib *, LPCOLESTR /* const szFullPath */, LPCOLESTR /* const szHelpDir */);
		PFNREGISTERTYPELIB pfnRegisterTypeLib = NULL;

		if (ModuleEntry::PerUserRegistration())
		{
			HMODULE hmodOleAut=::GetModuleHandleW(L"OLEAUT32.DLL");
			if (hmodOleAut)
			{
				pfnRegisterTypeLib=reinterpret_cast<PFNREGISTERTYPELIB>(::GetProcAddress(hmodOleAut, "RegisterTypeLibForUser"));
			}
		}

		if (!pfnRegisterTypeLib)
		{
			pfnRegisterTypeLib = (PFNREGISTERTYPELIB)&RegisterTypeLib;
		}

		if (FAILED(hr = pfnRegisterTypeLib(*pptl, const_cast<wchar *>(stubp.Chars()), NULL)))
		{
			ReleaseObj(*pptl);
			ThrowHr(hr);
		}
	}
}


static void RegisterTypeLibrary(int rid, BOOL fRegister)
{
	ITypeLibPtr qtl;

	LoadTypeLibraryCore(rid, &qtl, fRegister);
	if (fRegister)
		return;

	TLIBATTR *pla;
	HRESULT hr;

	CheckHr(qtl->GetLibAttr(&pla));

	// Handle registering in HKCU instead of HKCR. This is only supported on W2003 and Vista so we have
	// to get the function pointer dynamically
	typedef HRESULT (STDAPICALLTYPE *PFNUNREGISTERTYPELIB)(REFGUID, unsigned short, unsigned short, LCID, SYSKIND);
	PFNUNREGISTERTYPELIB pfnUnRegisterTypeLib = NULL;

	if (ModuleEntry::PerUserRegistration())
	{
		HMODULE hmodOleAut=::GetModuleHandleW(L"OLEAUT32.DLL");
		if (hmodOleAut)
		{
			pfnUnRegisterTypeLib = reinterpret_cast<PFNUNREGISTERTYPELIB>(::GetProcAddress(hmodOleAut, "UnRegisterTypeLibForUser"));
		}
	}

	if (!pfnUnRegisterTypeLib)
	{
		pfnUnRegisterTypeLib = (PFNUNREGISTERTYPELIB)&UnRegisterTypeLib;
	}

	hr = pfnUnRegisterTypeLib(pla->guid, pla->wMajorVerNum, pla->wMinorVerNum, pla->lcid, pla->syskind);
	qtl->ReleaseTLibAttr(pla);

	// ENHANCE: Is there a better way to trap the error that happens when the libary is
	// not registered and it is unregistered?
	if (FAILED(hr) && hr != TYPE_E_REGISTRYACCESS)
		ThrowHr(hr);
}


static BOOL CALLBACK RegisterTypeLibProc(HMODULE hmod, LPCTSTR pszType,
	LPTSTR pszName, LONG lParam)
{
	long ln = (long)pszName;
	if (ln >= 0x10000)
		return true;

	try
	{
		RegisterTypeLibrary(ln, true);
	}
	catch (Throwable & thr)
	{
		*(HRESULT *)lParam = thr.Error();
	}

	return true;
}


static BOOL CALLBACK UnregisterTypeLibProc(HMODULE hmod, LPCTSTR pszType,
	LPTSTR pszName, LONG lParam)
{
	long ln = (long)pszName;
	if (ln >= 0x10000)
		return true;


	try
	{
		RegisterTypeLibrary(ln, false);
	}
	catch (Throwable & thr)
	{
		*(HRESULT *)lParam = thr.Error();
	}

	return true;
}


static void RegisterAllTypeLibraries(BOOL fRegister)
{
	long l=0;

	if (!EnumResourceNames(ModuleEntry::GetModuleHandle(), _T("TYPELIB"),
			fRegister ? &RegisterTypeLibProc : &UnregisterTypeLibProc,
			l))
	{
		DWORD dw = GetLastError();

		switch (dw)
		{
		case ERROR_RESOURCE_DATA_NOT_FOUND:
		case ERROR_RESOURCE_TYPE_NOT_FOUND:
			return;
		// REVIEW ShonK: For some crazy reason Win98 returns this error if there are no
		// resources or no type lib resources. We should try to find a more robust way of
		// distinguishing this case.
		case ERROR_CALL_NOT_IMPLEMENTED:
			return;
		default:
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
	}
}


/*************************************************************************************
	Load the type library.
*************************************************************************************/
void LoadTypeLibrary(int rid, ITypeLib **pptl)
{
	LoadTypeLibraryCore(rid, pptl, FALSE);
}


/*************************************************************************************
	Loads a type info from the type library with resource id = rid. If rid is 0, the
	default type library is loaded.
*************************************************************************************/
void LoadTypeInfo(int rid, REFIID iid, ITypeInfo **ppti)
{
	AssertPtr(ppti);
	Assert(!*ppti);

	ITypeLibPtr qtl;

	LoadTypeLibrary(rid, &qtl);
	CheckHr(qtl->GetTypeInfoOfGuid(iid, ppti));
}
#endif //_MSC_VER


/***********************************************************************************************
	UtilMem.h functions.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	This fills a block of memory with the given integer value.
----------------------------------------------------------------------------------------------*/
void FillInts(void * pv, int n, int cn)
{
	AssertPtrSize(pv, cn * isizeof(int));

#ifdef NO_ASM

	int * pn = (int *)pv;
	int * pnLim = pn + cn;

	while (pn < pnLim)
		*pn++ = n;

#else // !NO_ASM

	__asm
		{
		// Setup the registers for using REP STOS instruction to set memory.
		// NOTE: Alignment does not effect the speed of STOS.
		//
		// edi -> memory to set
		// eax = value to store in destination
		// direction flag is clear for auto-increment

		mov		edi,pv
		mov		eax,n
		mov		ecx,cn
		rep		stosd
		}

#endif //!NO_ASM
}


/*----------------------------------------------------------------------------------------------
	Fills a block of memory with the given short value.
----------------------------------------------------------------------------------------------*/
void FillShorts(void * pv, short sn, int csn)
{
	AssertPtrSize(pv, csn * isizeof(short));

#ifdef NO_ASM

	short * psn = (short *)pv;
	short * psnLim = psn + csn;

	while (psn < psnLim)
		*psn++ = sn;

#else // !NO_ASM

	__asm
		{
		// Setup the registers for using REP STOS instruction to set memory.
		// NOTE: Alignment does not effect the speed of STOS.
		//
		// edi -> memory to set
		// eax = value to store in destination
		// direction flag is clear for auto-increment

		mov		edi,pv
		mov		ax,sn
		mov		ecx,csn

		mov		edx,ecx
		and		edx,1
		jz		LInts

		// set 1 short
		stosw

LInts:
		shr		ecx,1
		jz		LDone

		mov		ebx,eax
		shl		eax,16
		mov		ax,bx

		rep		stosd
LDone:
		}

#endif //!NO_ASM
}


/*----------------------------------------------------------------------------------------------
	Reverse cb bytes starting at pv.
----------------------------------------------------------------------------------------------*/
void ReverseBytes(void * pv, int cb)
{
	AssertPtrSize(pv, cb);

#ifdef NO_ASM

	byte * pb1 = (byte *)pv;
	byte * pb2 = (byte *)pv + cb - 1;
	byte b;

	while (pb1 < pb2)
	{
		b = *pb1;
		*pb1++ = *pb2;
		*pb2-- = b;
	}

#else // !NO_ASM

	__asm
		{
		// esi - high end of block
		// edi - low end of block
		// ecx - number of bytes to swap

		mov		edi,pv
		mov		esi,edi
		mov		ecx,cb
		add		esi,ecx
		shr		ecx,1
		jz		LDone

LLoop:
		dec		esi
		mov		al,[edi]
		mov		bl,[esi]
		mov		[edi],bl
		mov		[esi],al

		inc		edi
		dec		ecx
		jnz		LLoop
LDone:
		}

#endif //!NO_ASM
}


/*----------------------------------------------------------------------------------------------
	Reverse cn ints starting at pv.
----------------------------------------------------------------------------------------------*/
void ReverseInts(void * pv, int cn)
{
	AssertPtrSize(pv, cn * isizeof(int));

#ifdef NO_ASM

	int * pn1 = (int *)pv;
	int * pn2 = (int *)pv + cn - 1;
	int n;

	while (pn1 < pn2)
	{
		n = *pn1;
		*pn1++ = *pn2;
		*pn2-- = n;
	}

#else // !NO_ASM

	__asm
		{
		// esi - high end of block
		// edi - low end of block
		// ecx - number of bytes to swap

		mov		edi,pv
		mov		esi,edi
		mov		ecx,cn
		add		esi,ecx
		shr		ecx,3
		jz		LDone

LLoop:
		sub		esi,4
		mov		eax,[edi]
		mov		ebx,[esi]
		mov		[edi],ebx
		mov		[esi],eax

		add		edi,4
		dec		ecx
		jnz		LLoop
LDone:
		}

#endif //!NO_ASM
}


/*----------------------------------------------------------------------------------------------
	Swap two adjacent blocks of size cb1 and cb2 respectively.
----------------------------------------------------------------------------------------------*/
void SwapBlocks(void * pv, int cb1, int cb2)
{
	Assert(cb1 >= 0 && cb2 >= 0);
	Assert(cb1 + cb2 >= cb1);
	AssertPtrSize(pv, cb1 + cb2);

	ReverseBytes(pv, cb1);
	ReverseBytes((byte *)pv + cb1, cb2);
	ReverseBytes(pv, cb1 + cb2);
}


/*----------------------------------------------------------------------------------------------
	Swap two blocks of size cb starting at pv1 and pv2. Doesn't handle overlapping blocks.
----------------------------------------------------------------------------------------------*/
void SwapBytes(void * pv1, void * pv2, int cb)
{
	Assert((byte *)pv1 + cb <= (byte *)pv2 || (byte *)pv2 + cb <= (byte *)pv1);
	AssertPtrSize(pv1, cb);
	AssertPtrSize(pv2, cb);

#ifdef NO_ASM

	byte *pb1 = (byte *)pv1;
	byte *pb2 = (byte *)pv2;
	byte b;

	while (--cb >= 0)
	{
		b = *pb1;
		*pb1++ = *pb2;
		*pb2++ = b;
	}

#else // !NO_ASM

	__asm
		{
		// edi -> memory to swap, first pointer
		// esi -> memory to swap, second pointer

		mov		edi,pv1
		mov		esi,pv2

		mov		ecx,cb
		shr		ecx,2
		jz		LBytes

LIntLoop:
		mov		eax,[edi]
		mov		ebx,[esi]
		mov		[edi],ebx
		mov		[esi],eax

		add		edi,4
		add		esi,4
		dec		ecx
		jnz		LIntLoop;

LBytes:
		mov		ecx,cb
		and		ecx,3
		jz		LDone

LByteLoop:
		mov		al,[edi]
		mov		bl,[esi]
		mov		[edi],bl
		mov		[esi],al
		inc		edi
		inc		esi
		dec		ecx
		jnz		LByteLoop

LDone:
		}

#endif //!NO_ASM
}


/*----------------------------------------------------------------------------------------------
	Move the entry at ivSrc to be immediately before the element that is currently at ivTarget.
	If ivTarget > ivSrc, the entry actually ends up at (ivTarget - 1) and the entry at ivTarget
	doesn't move. If ivTarget < ivSrc, the entry ends up at ivTarget and the entry at ivTarget
	moves to (ivTarget + 1). Everything in between is shifted appropriately. pv is the array
	of elements and cbElement is the size of each element.
----------------------------------------------------------------------------------------------*/
void MoveElement(void * pv, int cbElement, int ivSrc, int ivTarget)
{
	Assert(cbElement >= 0 && ivSrc >= 0 && ivTarget >= 0);
	AssertPtrSize(pv, Mul(cbElement, (ivSrc + 1)));
	AssertPtrSize(pv, Mul(cbElement, ivTarget));

	if (ivTarget == ivSrc || ivTarget == ivSrc + 1)
		return;

	const int kcbBuf = 256;

	if (cbElement < kcbBuf)
	{
		byte rgb[kcbBuf];
		if (ivSrc < ivTarget)
		{
			byte * pbSrc = (byte *)pv + Mul(ivSrc, cbElement);
			int cbMove = Mul(ivTarget - 1 - ivSrc, cbElement);
			CopyBytes(pbSrc, rgb, cbElement);
			MoveBytes(pbSrc + cbElement, pbSrc, cbMove);
			CopyBytes(rgb, pbSrc + cbMove, cbElement);
		}
		else
		{
			byte * pbDst = (byte *)pv + Mul(ivTarget, cbElement);
			int cbMove = Mul(ivSrc - ivTarget, cbElement);
			CopyBytes(pbDst + cbMove, rgb, cbElement);
			MoveBytes(pbDst, pbDst + cbElement, cbMove);
			CopyBytes(rgb, pbDst, cbElement);
		}
	}
	else
	{
		// Swap the blocks.
		if (ivSrc < ivTarget)
		{
			SwapBlocks((byte *)pv + Mul(ivSrc, cbElement), cbElement,
				Mul(ivTarget - 1 - ivSrc, cbElement));
		}
		else
		{
			SwapBlocks((byte *)pv + Mul(ivTarget, cbElement),
				Mul(ivSrc - ivTarget, cbElement), cbElement);
		}
	}
}


/***********************************************************************************************
	SmartVariant methods.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	Get a 32 bit integer from the variant. This only converts from scalar types, not from
	objects or strings. It also throws an exception if information would be lost in converting.
----------------------------------------------------------------------------------------------*/
void SmartVariant::GetInteger(int * pn)
{
	AssertPtr(pn);

	void * pv = (vt & VT_BYREF) ? plVal : &lVal;

	switch (vt & ~VT_BYREF)
	{
	default:
		ThrowHr(WarnHr(E_INVALIDARG));

	case VT_I2:
		AssertPtr((short *)pv);
		*pn = *(short *)pv;
		return;

	case VT_I4:
	case VT_INT:
		AssertPtr((int *)pv);
		*pn = *(int *)pv;
		return;

	case VT_R4:
		AssertPtr((float *)pv);
		if ((float)(int)*(float *)pv != *(float *)pv)
			ThrowHr(WarnHr(E_INVALIDARG));
		*pn = (int)*(float *)pv;
		return;

	case VT_R8:
		AssertPtr((double *)pv);
		if ((double)(int)*(double *)pv != *(double *)pv)
			ThrowHr(WarnHr(E_INVALIDARG));
		*pn = (int)*(double *)pv;
		return;

	case VT_I8: // availabe now, fits our use better
	case VT_CY:
		AssertPtr((int64 *)pv);
		if ((int64)(int)*(int64 *)pv != *(int64 *)pv)
			ThrowHr(WarnHr(E_INVALIDARG));
		*pn = (int)*(int64 *)pv;
		return;

	case VT_BOOL:
		AssertPtr((VARIANT_BOOL *)pv);
		*pn = *(VARIANT_BOOL *)pv != VARIANT_FALSE;
		return;

	case VT_I1:
		AssertPtr((char *)pv);
		*pn = *(char *)pv;
		return;

	case VT_UI1:
		AssertPtr((byte *)pv);
		*pn = *(byte *)pv;
		return;

	case VT_UI2:
		AssertPtr((ushort *)pv);
		*pn = *(ushort *)pv;
		return;

	case VT_UI4:
	case VT_UINT:
		AssertPtr((uint *)pv);
		*pn = *(uint *)pv;
		return;
	}
}


/*----------------------------------------------------------------------------------------------
	Get a boolean from the variant. This only converts from scalar types, not from objects
	or strings (it will throw an exception). Any non-zero value maps to true.
----------------------------------------------------------------------------------------------*/
void SmartVariant::GetBoolean(bool * pf)
{
	AssertPtr(pf);

	void * pv = (vt & VT_BYREF) ? plVal : &lVal;

	switch (vt & ~VT_BYREF)
	{
	default:
		ThrowHr(WarnHr(E_INVALIDARG));

	// 1 byte.
	case VT_I1:
	case VT_UI1:
		AssertPtr((char *)pv);
		*pf = *(char *)pv != 0;
		return;

	// 2 bytes.
	case VT_I2:
	case VT_UI2:
	case VT_BOOL:
		AssertPtr((short *)pv);
		*pf = *(short *)pv != 0;
		return;

	// 4 bytes.
	case VT_I4:
	case VT_UI4:
	case VT_R4:
	case VT_INT:
	case VT_UINT:
		AssertPtr((int *)pv);
		*pf = *(int *)pv != 0;
		return;

	// 8 bytes.
	case VT_R8:
	case VT_I8: // availabe now, fits our use better then VT_CY
	case VT_CY:
		AssertPtr((int64 *)pv);
		*pf = *(int64 *)pv != 0;
		return;
	}
}


/*----------------------------------------------------------------------------------------------
	Get a 64 bit integer from the variant. This only converts from scalar types, not from
	objects or strings. It also throws an exception if information would be lost in converting.
----------------------------------------------------------------------------------------------*/
void SmartVariant::GetInt64(int64 * plln)
{
	AssertPtr(plln);

	void * pv = (vt & VT_BYREF) ? plVal : &lVal;

	switch (vt & ~VT_BYREF)
	{
	default:
		ThrowHr(WarnHr(E_INVALIDARG));

	case VT_I2:
		AssertPtr((short *)pv);
		*plln = *(short *)pv;
		return;

	case VT_I4:
	case VT_INT:
		AssertPtr((int *)pv);
		*plln = *(int *)pv;
		return;

	case VT_R4:
		AssertPtr((float *)pv);
		if ((float)(int64)*(float *)pv != *(float *)pv)
			ThrowHr(WarnHr(E_INVALIDARG));
		*plln = (int64)*(float *)pv;
		return;

	case VT_R8:
		AssertPtr((double *)pv);
		if ((double)(int64)*(double *)pv != *(double *)pv)
			ThrowHr(WarnHr(E_INVALIDARG));
		*plln = (int64)*(double *)pv;
		return;

	case VT_I8: // availabe now, fits our use better then VT_CY
	case VT_CY:
		AssertPtr((int64 *)pv);
		*plln = *(int64 *)pv;
		return;

	case VT_BOOL:
		AssertPtr((VARIANT_BOOL *)pv);
		*plln = *(VARIANT_BOOL *)pv != VARIANT_FALSE;
		return;

	case VT_I1:
		AssertPtr((char *)pv);
		*plln = *(char *)pv;
		return;

	case VT_UI1:
		AssertPtr((byte *)pv);
		*plln = *(byte *)pv;
		return;

	case VT_UI2:
		AssertPtr((ushort *)pv);
		*plln = *(ushort *)pv;
		return;

	case VT_UI4:
	case VT_UINT:
		AssertPtr((uint *)pv);
		*plln = *(uint *)pv;
		return;
	}
}


/*----------------------------------------------------------------------------------------------
	Get an SilTime value. The variant must contain either a CY or a DATE, otherwise it throws
	an exception.
----------------------------------------------------------------------------------------------*/
void SmartVariant::GetSilTime(SilTime * pstim)
{
	AssertPtr(pstim);

	void * pv = (vt & VT_BYREF) ? plVal : &lVal;

	switch (vt & ~VT_BYREF)
	{
	default:
		ThrowHr(WarnHr(E_INVALIDARG));

	case VT_I8: // availabe now, fits our use better then VT_CY
	case VT_CY:
		AssertPtr((int64 *)pv);
		*pstim = *(int64 *)pv;
		return;

	case VT_DATE:
		AssertPtr((DATE *)pv);
		pstim->SetToVarTime(*(DATE *)pv);
		return;
	}
}


/*----------------------------------------------------------------------------------------------
	Get a double from the variant. This only converts from scalar types, not from objects or
	strings. It also throws an exception if information would be lost in converting.
----------------------------------------------------------------------------------------------*/
void SmartVariant::GetDouble(double * pdbl)
{
	AssertPtr(pdbl);

	void * pv = (vt & VT_BYREF) ? plVal : &lVal;

	switch (vt & ~VT_BYREF)
	{
	default:
		ThrowHr(WarnHr(E_INVALIDARG));

	case VT_I2:
		AssertPtr((short *)pv);
		*pdbl = *(short *)pv;
		return;

	case VT_I4:
	case VT_INT:
		AssertPtr((int *)pv);
		*pdbl = *(int *)pv;
		return;

	case VT_R4:
		AssertPtr((float *)pv);
		*pdbl = *(float *)pv;
		return;

	case VT_R8:
		AssertPtr((double *)pv);
		*pdbl = *(double *)pv;
		return;

	case VT_I8: // availabe now, fits our use better then VT_CY
	case VT_CY:
		AssertPtr((int64 *)pv);
		if ((int64)(double)*(int64 *)pv != *(int64 *)pv)
			ThrowHr(WarnHr(E_INVALIDARG));
		*pdbl = (double)*(int64 *)pv;
		return;

	case VT_BOOL:
		AssertPtr((VARIANT_BOOL *)pv);
		*pdbl = *(VARIANT_BOOL *)pv != VARIANT_FALSE;
		return;

	case VT_I1:
		AssertPtr((char *)pv);
		*pdbl = *(char *)pv;
		return;

	case VT_UI1:
		AssertPtr((byte *)pv);
		*pdbl = *(byte *)pv;
		return;

	case VT_UI2:
		AssertPtr((ushort *)pv);
		*pdbl = *(ushort *)pv;
		return;

	case VT_UI4:
	case VT_UINT:
		AssertPtr((uint *)pv);
		*pdbl = *(uint *)pv;
		return;
	}
}


/*----------------------------------------------------------------------------------------------
	Get an object from the variant. This returns false if the variant contains a null
	object.
----------------------------------------------------------------------------------------------*/
bool SmartVariant::GetObject(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	IUnknown * punk;

	switch (vt)
	{
	default:
		ThrowHr(WarnHr(E_INVALIDARG));

	case VT_DISPATCH:
	case VT_UNKNOWN:
		punk = punkVal;
		break;

	case VT_DISPATCH | VT_BYREF:
	case VT_UNKNOWN | VT_BYREF:
		AssertPtr(ppunkVal);
		punk = *ppunkVal;
		break;
	}

	AssertPtrN(punk);
	if (!punk)
		return false;

	CheckHr(punk->QueryInterface(iid, ppv));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Get an object from the variant. This succeeds if the variant contains a null object.
----------------------------------------------------------------------------------------------*/
void SmartVariant::GetObjectOrNull(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	IUnknown * punk;

	switch (vt)
	{
	default:
		ThrowHr(WarnHr(E_INVALIDARG));

	case VT_DISPATCH:
	case VT_UNKNOWN:
		punk = punkVal;
		break;

	case VT_DISPATCH | VT_BYREF:
	case VT_UNKNOWN | VT_BYREF:
		AssertPtr(ppunkVal);
		punk = *ppunkVal;
		break;
	}

	AssertPtrN(punk);
	if (punk)
		CheckHr(punk->QueryInterface(iid, ppv));
}


#if WIN32
/*----------------------------------------------------------------------------------------------
	Convert a pathname to its long form.
----------------------------------------------------------------------------------------------*/
void GetLongPathname(const char * psz, StrAnsi & staPath)
{
	AssertPsz(psz);

	IShellFolderPtr qsf;
	ITEMIDLIST * pidl;
	IMallocPtr qmal;
	achar szPath[MAX_PATH + 1];
	StrUniBufPath stubp = psz;

	CheckHr(SHGetDesktopFolder(&qsf));
	CheckHr(qsf->ParseDisplayName(NULL, NULL, (OLECHAR *)stubp.Chars(), NULL,  &pidl, NULL));
	AssertPtr(pidl);
	bool fSuccess = SHGetPathFromIDList(pidl, szPath);
	CheckHr(SHGetMalloc(&qmal));
	qmal->Free(pidl);
	if (!fSuccess)
		ThrowHr(WarnHr(E_UNEXPECTED));

	staPath.Assign(szPath);		// FIX ME FOR PROPER CODE CONVERSION!
}
#endif // WIN32

/*----------------------------------------------------------------------------------------------
	Convert the HRESULT code to an ASCII string containing its name.
----------------------------------------------------------------------------------------------*/
#define CASE_HRESULT(h)  case h: return #h;
const char * AsciiHresult(HRESULT hr)
{
	switch (hr)
	{
	CASE_HRESULT(E_UNEXPECTED)
	CASE_HRESULT(E_NOTIMPL)
	CASE_HRESULT(E_OUTOFMEMORY)
	CASE_HRESULT(E_INVALIDARG)
	CASE_HRESULT(E_NOINTERFACE)
	CASE_HRESULT(E_POINTER)
	CASE_HRESULT(E_HANDLE)
	CASE_HRESULT(E_ABORT)
	CASE_HRESULT(E_FAIL)
	CASE_HRESULT(E_ACCESSDENIED)
	CASE_HRESULT(E_PENDING)
	CASE_HRESULT(CO_E_INIT_TLS)
	CASE_HRESULT(CO_E_INIT_SHARED_ALLOCATOR)
	CASE_HRESULT(CO_E_INIT_MEMORY_ALLOCATOR)
	CASE_HRESULT(CO_E_INIT_CLASS_CACHE)
	CASE_HRESULT(CO_E_INIT_RPC_CHANNEL)
	CASE_HRESULT(CO_E_INIT_TLS_SET_CHANNEL_CONTROL)
	CASE_HRESULT(CO_E_INIT_TLS_CHANNEL_CONTROL)
	CASE_HRESULT(CO_E_INIT_UNACCEPTED_USER_ALLOCATOR)
	CASE_HRESULT(CO_E_INIT_SCM_MUTEX_EXISTS)
	CASE_HRESULT(CO_E_INIT_SCM_FILE_MAPPING_EXISTS)
	CASE_HRESULT(CO_E_INIT_SCM_MAP_VIEW_OF_FILE)
	CASE_HRESULT(CO_E_INIT_SCM_EXEC_FAILURE)
	CASE_HRESULT(CO_E_INIT_ONLY_SINGLE_THREADED)
	CASE_HRESULT(CO_E_CANT_REMOTE)
	CASE_HRESULT(CO_E_BAD_SERVER_NAME)
	CASE_HRESULT(CO_E_WRONG_SERVER_IDENTITY)
	CASE_HRESULT(CO_E_OLE1DDE_DISABLED)
	CASE_HRESULT(CO_E_RUNAS_SYNTAX)
	CASE_HRESULT(CO_E_CREATEPROCESS_FAILURE)
	CASE_HRESULT(CO_E_RUNAS_CREATEPROCESS_FAILURE)
	CASE_HRESULT(CO_E_RUNAS_LOGON_FAILURE)
	CASE_HRESULT(CO_E_LAUNCH_PERMSSION_DENIED)
	CASE_HRESULT(CO_E_START_SERVICE_FAILURE)
	CASE_HRESULT(CO_E_REMOTE_COMMUNICATION_FAILURE)
	CASE_HRESULT(CO_E_SERVER_START_TIMEOUT)
	CASE_HRESULT(CO_E_CLSREG_INCONSISTENT)
	CASE_HRESULT(CO_E_IIDREG_INCONSISTENT)
	CASE_HRESULT(CO_E_NOT_SUPPORTED)
	CASE_HRESULT(S_OK)
	CASE_HRESULT(S_FALSE)
	CASE_HRESULT(OLE_E_OLEVERB)
	CASE_HRESULT(OLE_E_ADVF)
	CASE_HRESULT(OLE_E_ENUM_NOMORE)
	CASE_HRESULT(OLE_E_ADVISENOTSUPPORTED)
	CASE_HRESULT(OLE_E_NOCONNECTION)
	CASE_HRESULT(OLE_E_NOTRUNNING)
	CASE_HRESULT(OLE_E_NOCACHE)
	CASE_HRESULT(OLE_E_BLANK)
	CASE_HRESULT(OLE_E_CLASSDIFF)
	CASE_HRESULT(OLE_E_CANT_GETMONIKER)
	CASE_HRESULT(OLE_E_CANT_BINDTOSOURCE)
	CASE_HRESULT(OLE_E_STATIC)
	CASE_HRESULT(OLE_E_PROMPTSAVECANCELLED)
	CASE_HRESULT(OLE_E_INVALIDRECT)
	CASE_HRESULT(OLE_E_WRONGCOMPOBJ)
	CASE_HRESULT(OLE_E_INVALIDHWND)
	CASE_HRESULT(OLE_E_NOT_INPLACEACTIVE)
	CASE_HRESULT(OLE_E_CANTCONVERT)
	CASE_HRESULT(OLE_E_NOSTORAGE)
	CASE_HRESULT(DV_E_FORMATETC)
	CASE_HRESULT(DV_E_DVTARGETDEVICE)
	CASE_HRESULT(DV_E_STGMEDIUM)
	CASE_HRESULT(DV_E_STATDATA)
	CASE_HRESULT(DV_E_LINDEX)
	CASE_HRESULT(DV_E_TYMED)
	CASE_HRESULT(DV_E_CLIPFORMAT)
	CASE_HRESULT(DV_E_DVASPECT)
	CASE_HRESULT(DV_E_DVTARGETDEVICE_SIZE)
	CASE_HRESULT(DV_E_NOIVIEWOBJECT)
	CASE_HRESULT(DRAGDROP_E_NOTREGISTERED)
	CASE_HRESULT(DRAGDROP_E_ALREADYREGISTERED)
	CASE_HRESULT(DRAGDROP_E_INVALIDHWND)
	CASE_HRESULT(CLASS_E_NOAGGREGATION)
	CASE_HRESULT(CLASS_E_CLASSNOTAVAILABLE)
	CASE_HRESULT(VIEW_E_DRAW)
	CASE_HRESULT(REGDB_E_READREGDB)
	CASE_HRESULT(REGDB_E_WRITEREGDB)
	CASE_HRESULT(REGDB_E_KEYMISSING)
	CASE_HRESULT(REGDB_E_INVALIDVALUE)
	CASE_HRESULT(REGDB_E_CLASSNOTREG)
	CASE_HRESULT(REGDB_E_IIDNOTREG)
	CASE_HRESULT(CACHE_E_NOCACHE_UPDATED)
	CASE_HRESULT(OLEOBJ_E_NOVERBS)
	CASE_HRESULT(OLEOBJ_E_INVALIDVERB)
	CASE_HRESULT(INPLACE_E_NOTUNDOABLE)
	CASE_HRESULT(INPLACE_E_NOTOOLSPACE)
	CASE_HRESULT(CONVERT10_E_OLESTREAM_GET)
	CASE_HRESULT(CONVERT10_E_OLESTREAM_PUT)
	CASE_HRESULT(CONVERT10_E_OLESTREAM_FMT)
	CASE_HRESULT(CONVERT10_E_OLESTREAM_BITMAP_TO_DIB)
	CASE_HRESULT(CONVERT10_E_STG_FMT)
	CASE_HRESULT(CONVERT10_E_STG_NO_STD_STREAM)
	CASE_HRESULT(CONVERT10_E_STG_DIB_TO_BITMAP)
	CASE_HRESULT(CLIPBRD_E_CANT_OPEN)
	CASE_HRESULT(CLIPBRD_E_CANT_EMPTY)
	CASE_HRESULT(CLIPBRD_E_CANT_SET)
	CASE_HRESULT(CLIPBRD_E_BAD_DATA)
	CASE_HRESULT(CLIPBRD_E_CANT_CLOSE)
	CASE_HRESULT(MK_E_CONNECTMANUALLY)
	CASE_HRESULT(MK_E_EXCEEDEDDEADLINE)
	CASE_HRESULT(MK_E_NEEDGENERIC)
	CASE_HRESULT(MK_E_UNAVAILABLE)
	CASE_HRESULT(MK_E_SYNTAX)
	CASE_HRESULT(MK_E_NOOBJECT)
	CASE_HRESULT(MK_E_INVALIDEXTENSION)
	CASE_HRESULT(MK_E_INTERMEDIATEINTERFACENOTSUPPORTED)
	CASE_HRESULT(MK_E_NOTBINDABLE)
	CASE_HRESULT(MK_E_NOTBOUND)
	CASE_HRESULT(MK_E_CANTOPENFILE)
	CASE_HRESULT(MK_E_MUSTBOTHERUSER)
	CASE_HRESULT(MK_E_NOINVERSE)
	CASE_HRESULT(MK_E_NOSTORAGE)
	CASE_HRESULT(MK_E_NOPREFIX)
	CASE_HRESULT(MK_E_ENUMERATION_FAILED)
	CASE_HRESULT(CO_E_NOTINITIALIZED)
	CASE_HRESULT(CO_E_ALREADYINITIALIZED)
	CASE_HRESULT(CO_E_CANTDETERMINECLASS)
	CASE_HRESULT(CO_E_CLASSSTRING)
	CASE_HRESULT(CO_E_IIDSTRING)
	CASE_HRESULT(CO_E_APPNOTFOUND)
	CASE_HRESULT(CO_E_APPSINGLEUSE)
	CASE_HRESULT(CO_E_ERRORINAPP)
	CASE_HRESULT(CO_E_DLLNOTFOUND)
	CASE_HRESULT(CO_E_ERRORINDLL)
	CASE_HRESULT(CO_E_WRONGOSFORAPP)
	CASE_HRESULT(CO_E_OBJNOTREG)
	CASE_HRESULT(CO_E_OBJISREG)
	CASE_HRESULT(CO_E_OBJNOTCONNECTED)
	CASE_HRESULT(CO_E_APPDIDNTREG)
	CASE_HRESULT(CO_E_RELEASED)
	CASE_HRESULT(OLE_S_USEREG)
	CASE_HRESULT(OLE_S_STATIC)
	CASE_HRESULT(OLE_S_MAC_CLIPFORMAT)
	CASE_HRESULT(DRAGDROP_S_DROP)
	CASE_HRESULT(DRAGDROP_S_CANCEL)
	CASE_HRESULT(DRAGDROP_S_USEDEFAULTCURSORS)
	CASE_HRESULT(DATA_S_SAMEFORMATETC)
	CASE_HRESULT(VIEW_S_ALREADY_FROZEN)
	CASE_HRESULT(CACHE_S_FORMATETC_NOTSUPPORTED)
	CASE_HRESULT(CACHE_S_SAMECACHE)
	CASE_HRESULT(CACHE_S_SOMECACHES_NOTUPDATED)
	CASE_HRESULT(OLEOBJ_S_INVALIDVERB)
	CASE_HRESULT(OLEOBJ_S_CANNOT_DOVERB_NOW)
	CASE_HRESULT(OLEOBJ_S_INVALIDHWND)
	CASE_HRESULT(INPLACE_S_TRUNCATED)
	CASE_HRESULT(CONVERT10_S_NO_PRESENTATION)
	CASE_HRESULT(MK_S_REDUCED_TO_SELF)
	CASE_HRESULT(MK_S_ME)
	CASE_HRESULT(MK_S_HIM)
	CASE_HRESULT(MK_S_US)
	CASE_HRESULT(MK_S_MONIKERALREADYREGISTERED)
	CASE_HRESULT(CO_E_CLASS_CREATE_FAILED)
	CASE_HRESULT(CO_E_SCM_ERROR)
	CASE_HRESULT(CO_E_SCM_RPC_FAILURE)
	CASE_HRESULT(CO_E_BAD_PATH)
	CASE_HRESULT(CO_E_SERVER_EXEC_FAILURE)
	CASE_HRESULT(CO_E_OBJSRV_RPC_FAILURE)
	CASE_HRESULT(MK_E_NO_NORMALIZED)
	CASE_HRESULT(CO_E_SERVER_STOPPING)
	CASE_HRESULT(MEM_E_INVALID_ROOT)
	CASE_HRESULT(MEM_E_INVALID_LINK)
	CASE_HRESULT(MEM_E_INVALID_SIZE)
	CASE_HRESULT(CO_S_NOTALLINTERFACES)
	CASE_HRESULT(DISP_E_UNKNOWNINTERFACE)
	CASE_HRESULT(DISP_E_MEMBERNOTFOUND)
	CASE_HRESULT(DISP_E_PARAMNOTFOUND)
	CASE_HRESULT(DISP_E_TYPEMISMATCH)
	CASE_HRESULT(DISP_E_UNKNOWNNAME)
	CASE_HRESULT(DISP_E_NONAMEDARGS)
	CASE_HRESULT(DISP_E_BADVARTYPE)
	CASE_HRESULT(DISP_E_EXCEPTION)
	CASE_HRESULT(DISP_E_OVERFLOW)
	CASE_HRESULT(DISP_E_BADINDEX)
	CASE_HRESULT(DISP_E_UNKNOWNLCID)
	CASE_HRESULT(DISP_E_ARRAYISLOCKED)
	CASE_HRESULT(DISP_E_BADPARAMCOUNT)
	CASE_HRESULT(DISP_E_PARAMNOTOPTIONAL)
	CASE_HRESULT(DISP_E_BADCALLEE)
	CASE_HRESULT(DISP_E_NOTACOLLECTION)
	CASE_HRESULT(TYPE_E_BUFFERTOOSMALL)
	CASE_HRESULT(TYPE_E_INVDATAREAD)
	CASE_HRESULT(TYPE_E_UNSUPFORMAT)
	CASE_HRESULT(TYPE_E_REGISTRYACCESS)
	CASE_HRESULT(TYPE_E_LIBNOTREGISTERED)
	CASE_HRESULT(TYPE_E_UNDEFINEDTYPE)
	CASE_HRESULT(TYPE_E_QUALIFIEDNAMEDISALLOWED)
	CASE_HRESULT(TYPE_E_INVALIDSTATE)
	CASE_HRESULT(TYPE_E_WRONGTYPEKIND)
	CASE_HRESULT(TYPE_E_ELEMENTNOTFOUND)
	CASE_HRESULT(TYPE_E_AMBIGUOUSNAME)
	CASE_HRESULT(TYPE_E_NAMECONFLICT)
	CASE_HRESULT(TYPE_E_UNKNOWNLCID)
	CASE_HRESULT(TYPE_E_DLLFUNCTIONNOTFOUND)
	CASE_HRESULT(TYPE_E_BADMODULEKIND)
	CASE_HRESULT(TYPE_E_SIZETOOBIG)
	CASE_HRESULT(TYPE_E_DUPLICATEID)
	CASE_HRESULT(TYPE_E_INVALIDID)
	CASE_HRESULT(TYPE_E_TYPEMISMATCH)
	CASE_HRESULT(TYPE_E_OUTOFBOUNDS)
	CASE_HRESULT(TYPE_E_IOERROR)
	CASE_HRESULT(TYPE_E_CANTCREATETMPFILE)
	CASE_HRESULT(TYPE_E_CANTLOADLIBRARY)
	CASE_HRESULT(TYPE_E_INCONSISTENTPROPFUNCS)
	CASE_HRESULT(TYPE_E_CIRCULARTYPE)
	CASE_HRESULT(STG_E_INVALIDFUNCTION)
	CASE_HRESULT(STG_E_FILENOTFOUND)
	CASE_HRESULT(STG_E_PATHNOTFOUND)
	CASE_HRESULT(STG_E_TOOMANYOPENFILES)
	CASE_HRESULT(STG_E_ACCESSDENIED)
	CASE_HRESULT(STG_E_INVALIDHANDLE)
	CASE_HRESULT(STG_E_INSUFFICIENTMEMORY)
	CASE_HRESULT(STG_E_INVALIDPOINTER)
	CASE_HRESULT(STG_E_NOMOREFILES)
	CASE_HRESULT(STG_E_DISKISWRITEPROTECTED)
	CASE_HRESULT(STG_E_SEEKERROR)
	CASE_HRESULT(STG_E_WRITEFAULT)
	CASE_HRESULT(STG_E_READFAULT)
	CASE_HRESULT(STG_E_SHAREVIOLATION)
	CASE_HRESULT(STG_E_LOCKVIOLATION)
	CASE_HRESULT(STG_E_FILEALREADYEXISTS)
	CASE_HRESULT(STG_E_INVALIDPARAMETER)
	CASE_HRESULT(STG_E_MEDIUMFULL)
	CASE_HRESULT(STG_E_PROPSETMISMATCHED)
	CASE_HRESULT(STG_E_ABNORMALAPIEXIT)
	CASE_HRESULT(STG_E_INVALIDHEADER)
	CASE_HRESULT(STG_E_INVALIDNAME)
	CASE_HRESULT(STG_E_UNKNOWN)
	CASE_HRESULT(STG_E_UNIMPLEMENTEDFUNCTION)
	CASE_HRESULT(STG_E_INVALIDFLAG)
	CASE_HRESULT(STG_E_INUSE)
	CASE_HRESULT(STG_E_NOTCURRENT)
	CASE_HRESULT(STG_E_REVERTED)
	CASE_HRESULT(STG_E_CANTSAVE)
	CASE_HRESULT(STG_E_OLDFORMAT)
	CASE_HRESULT(STG_E_OLDDLL)
	CASE_HRESULT(STG_E_SHAREREQUIRED)
	CASE_HRESULT(STG_E_NOTFILEBASEDSTORAGE)
	CASE_HRESULT(STG_E_EXTANTMARSHALLINGS)
	CASE_HRESULT(STG_E_DOCFILECORRUPT)
	CASE_HRESULT(STG_E_BADBASEADDRESS)
	CASE_HRESULT(STG_E_INCOMPLETE)
	CASE_HRESULT(STG_E_TERMINATED)
	CASE_HRESULT(STG_S_CONVERTED)
	CASE_HRESULT(STG_S_BLOCK)
	CASE_HRESULT(STG_S_RETRYNOW)
	CASE_HRESULT(STG_S_MONITORING)
	CASE_HRESULT(RPC_E_CALL_REJECTED)
	CASE_HRESULT(RPC_E_CALL_CANCELED)
	CASE_HRESULT(RPC_E_CANTPOST_INSENDCALL)
	CASE_HRESULT(RPC_E_CANTCALLOUT_INASYNCCALL)
	CASE_HRESULT(RPC_E_CANTCALLOUT_INEXTERNALCALL)
	CASE_HRESULT(RPC_E_CONNECTION_TERMINATED)
	CASE_HRESULT(RPC_E_SERVER_DIED)
	CASE_HRESULT(RPC_E_CLIENT_DIED)
	CASE_HRESULT(RPC_E_INVALID_DATAPACKET)
	CASE_HRESULT(RPC_E_CANTTRANSMIT_CALL)
	CASE_HRESULT(RPC_E_CLIENT_CANTMARSHAL_DATA)
	CASE_HRESULT(RPC_E_CLIENT_CANTUNMARSHAL_DATA)
	CASE_HRESULT(RPC_E_SERVER_CANTMARSHAL_DATA)
	CASE_HRESULT(RPC_E_SERVER_CANTUNMARSHAL_DATA)
	CASE_HRESULT(RPC_E_INVALID_DATA)
	CASE_HRESULT(RPC_E_INVALID_PARAMETER)
	CASE_HRESULT(RPC_E_CANTCALLOUT_AGAIN)
	CASE_HRESULT(RPC_E_SERVER_DIED_DNE)
	CASE_HRESULT(RPC_E_SYS_CALL_FAILED)
	CASE_HRESULT(RPC_E_OUT_OF_RESOURCES)
	CASE_HRESULT(RPC_E_ATTEMPTED_MULTITHREAD)
	CASE_HRESULT(RPC_E_NOT_REGISTERED)
	CASE_HRESULT(RPC_E_FAULT)
	CASE_HRESULT(RPC_E_SERVERFAULT)
	CASE_HRESULT(RPC_E_CHANGED_MODE)
	CASE_HRESULT(RPC_E_INVALIDMETHOD)
	CASE_HRESULT(RPC_E_DISCONNECTED)
	CASE_HRESULT(RPC_E_RETRY)
	CASE_HRESULT(RPC_E_SERVERCALL_RETRYLATER)
	CASE_HRESULT(RPC_E_SERVERCALL_REJECTED)
	CASE_HRESULT(RPC_E_INVALID_CALLDATA)
	CASE_HRESULT(RPC_E_CANTCALLOUT_ININPUTSYNCCALL)
	CASE_HRESULT(RPC_E_WRONG_THREAD)
	CASE_HRESULT(RPC_E_THREAD_NOT_INIT)
	CASE_HRESULT(RPC_E_VERSION_MISMATCH)
	CASE_HRESULT(RPC_E_INVALID_HEADER)
	CASE_HRESULT(RPC_E_INVALID_EXTENSION)
	CASE_HRESULT(RPC_E_INVALID_IPID)
	CASE_HRESULT(RPC_E_INVALID_OBJECT)
	CASE_HRESULT(RPC_S_CALLPENDING)
	CASE_HRESULT(RPC_S_WAITONTIMER)
	CASE_HRESULT(RPC_E_CALL_COMPLETE)
	CASE_HRESULT(RPC_E_UNSECURE_CALL)
	CASE_HRESULT(RPC_E_TOO_LATE)
	CASE_HRESULT(RPC_E_NO_GOOD_SECURITY_PACKAGES)
	CASE_HRESULT(RPC_E_ACCESS_DENIED)
	CASE_HRESULT(RPC_E_REMOTE_DISABLED)
	CASE_HRESULT(RPC_E_INVALID_OBJREF)
	CASE_HRESULT(RPC_E_UNEXPECTED)
	CASE_HRESULT(NTE_BAD_UID)
	CASE_HRESULT(NTE_BAD_HASH)
	CASE_HRESULT(NTE_BAD_KEY)
	CASE_HRESULT(NTE_BAD_LEN)
	CASE_HRESULT(NTE_BAD_DATA)
	CASE_HRESULT(NTE_BAD_SIGNATURE)
	CASE_HRESULT(NTE_BAD_VER)
	CASE_HRESULT(NTE_BAD_ALGID)
	CASE_HRESULT(NTE_BAD_FLAGS)
	CASE_HRESULT(NTE_BAD_TYPE)
	CASE_HRESULT(NTE_BAD_KEY_STATE)
	CASE_HRESULT(NTE_BAD_HASH_STATE)
	CASE_HRESULT(NTE_NO_KEY)
	CASE_HRESULT(NTE_NO_MEMORY)
	CASE_HRESULT(NTE_EXISTS)
	CASE_HRESULT(NTE_PERM)
	CASE_HRESULT(NTE_NOT_FOUND)
	CASE_HRESULT(NTE_DOUBLE_ENCRYPT)
	CASE_HRESULT(NTE_BAD_PROVIDER)
	CASE_HRESULT(NTE_BAD_PROV_TYPE)
	CASE_HRESULT(NTE_BAD_PUBLIC_KEY)
	CASE_HRESULT(NTE_BAD_KEYSET)
	CASE_HRESULT(NTE_PROV_TYPE_NOT_DEF)
	CASE_HRESULT(NTE_PROV_TYPE_ENTRY_BAD)
	CASE_HRESULT(NTE_KEYSET_NOT_DEF)
	CASE_HRESULT(NTE_KEYSET_ENTRY_BAD)
	CASE_HRESULT(NTE_PROV_TYPE_NO_MATCH)
	CASE_HRESULT(NTE_SIGNATURE_FILE_BAD)
	CASE_HRESULT(NTE_PROVIDER_DLL_FAIL)
	CASE_HRESULT(NTE_PROV_DLL_NOT_FOUND)
	CASE_HRESULT(NTE_BAD_KEYSET_PARAM)
	CASE_HRESULT(NTE_FAIL)
	CASE_HRESULT(NTE_SYS_ERR)
	CASE_HRESULT(TRUST_E_PROVIDER_UNKNOWN)
	CASE_HRESULT(TRUST_E_ACTION_UNKNOWN)
	CASE_HRESULT(TRUST_E_SUBJECT_FORM_UNKNOWN)
	CASE_HRESULT(TRUST_E_SUBJECT_NOT_TRUSTED)
	CASE_HRESULT(DIGSIG_E_ENCODE)
	CASE_HRESULT(DIGSIG_E_DECODE)
	CASE_HRESULT(DIGSIG_E_EXTENSIBILITY)
	CASE_HRESULT(DIGSIG_E_CRYPTO)
	CASE_HRESULT(PERSIST_E_SIZEDEFINITE)
	CASE_HRESULT(PERSIST_E_SIZEINDEFINITE)
	CASE_HRESULT(PERSIST_E_NOTSELFSIZING)
	CASE_HRESULT(TRUST_E_NOSIGNATURE)
	CASE_HRESULT(CERT_E_EXPIRED)
	CASE_HRESULT(CERT_E_VALIDITYPERIODNESTING)
	CASE_HRESULT(CERT_E_ROLE)
	CASE_HRESULT(CERT_E_PATHLENCONST)
	CASE_HRESULT(CERT_E_CRITICAL)
	CASE_HRESULT(CERT_E_PURPOSE)
	CASE_HRESULT(CERT_E_ISSUERCHAINING)
	CASE_HRESULT(CERT_E_MALFORMED)
	CASE_HRESULT(CERT_E_UNTRUSTEDROOT)
	CASE_HRESULT(CERT_E_CHAINING)
	// OLE controls
	CASE_HRESULT(CTL_E_ILLEGALFUNCTIONCALL)
	CASE_HRESULT(CTL_E_OVERFLOW)
	CASE_HRESULT(CTL_E_OUTOFMEMORY)
	CASE_HRESULT(CTL_E_DIVISIONBYZERO)
	CASE_HRESULT(CTL_E_OUTOFSTRINGSPACE)
	CASE_HRESULT(CTL_E_OUTOFSTACKSPACE)
	CASE_HRESULT(CTL_E_BADFILENAMEORNUMBER)
	CASE_HRESULT(CTL_E_FILENOTFOUND)
	CASE_HRESULT(CTL_E_BADFILEMODE)
	CASE_HRESULT(CTL_E_FILEALREADYOPEN)
	CASE_HRESULT(CTL_E_DEVICEIOERROR)
	CASE_HRESULT(CTL_E_FILEALREADYEXISTS)
	CASE_HRESULT(CTL_E_BADRECORDLENGTH)
	CASE_HRESULT(CTL_E_DISKFULL)
	CASE_HRESULT(CTL_E_BADRECORDNUMBER)
	CASE_HRESULT(CTL_E_BADFILENAME)
	CASE_HRESULT(CTL_E_TOOMANYFILES)
	CASE_HRESULT(CTL_E_DEVICEUNAVAILABLE)
	CASE_HRESULT(CTL_E_PERMISSIONDENIED)
	CASE_HRESULT(CTL_E_DISKNOTREADY)
	CASE_HRESULT(CTL_E_PATHFILEACCESSERROR)
	CASE_HRESULT(CTL_E_PATHNOTFOUND)
	CASE_HRESULT(CTL_E_INVALIDPATTERNSTRING)
	CASE_HRESULT(CTL_E_INVALIDUSEOFNULL)
	CASE_HRESULT(CTL_E_INVALIDFILEFORMAT)
	CASE_HRESULT(CTL_E_INVALIDPROPERTYVALUE)
	CASE_HRESULT(CTL_E_INVALIDPROPERTYARRAYINDEX)
	CASE_HRESULT(CTL_E_SETNOTSUPPORTEDATRUNTIME)
	CASE_HRESULT(CTL_E_SETNOTSUPPORTED)
	CASE_HRESULT(CTL_E_NEEDPROPERTYARRAYINDEX)
	CASE_HRESULT(CTL_E_SETNOTPERMITTED)
	CASE_HRESULT(CTL_E_GETNOTSUPPORTEDATRUNTIME)
	CASE_HRESULT(CTL_E_GETNOTSUPPORTED)
	CASE_HRESULT(CTL_E_PROPERTYNOTFOUND)
	CASE_HRESULT(CTL_E_INVALIDCLIPBOARDFORMAT)
	CASE_HRESULT(CTL_E_INVALIDPICTURE)
	CASE_HRESULT(CTL_E_PRINTERERROR)
	CASE_HRESULT(CTL_E_CANTSAVEFILETOTEMP)
	CASE_HRESULT(CTL_E_SEARCHTEXTNOTFOUND)
	CASE_HRESULT(CTL_E_REPLACEMENTSTOOLONG)
	CASE_HRESULT(CLASS_E_NOTLICENSED)
	// OLEDB Error or Status Codes
	CASE_HRESULT(DB_E_BADACCESSORHANDLE)
	CASE_HRESULT(DB_E_ROWLIMITEXCEEDED)
	CASE_HRESULT(DB_E_READONLYACCESSOR)
	CASE_HRESULT(DB_E_SCHEMAVIOLATION)
	CASE_HRESULT(DB_E_BADROWHANDLE)
	CASE_HRESULT(DB_E_OBJECTOPEN)
	CASE_HRESULT(DB_E_BADCHAPTER)
	CASE_HRESULT(DB_E_CANTCONVERTVALUE)
	CASE_HRESULT(DB_E_BADBINDINFO)
	CASE_HRESULT(DB_E_NOTAREFERENCECOLUMN)
	CASE_HRESULT(DB_E_LIMITREJECTED)
	CASE_HRESULT(DB_E_NOCOMMAND)
	CASE_HRESULT(DB_E_COSTLIMIT)
	CASE_HRESULT(DB_E_BADBOOKMARK)
	CASE_HRESULT(DB_E_BADLOCKMODE)
	CASE_HRESULT(DB_E_PARAMNOTOPTIONAL)
	CASE_HRESULT(DB_E_BADCOLUMNID)
	CASE_HRESULT(DB_E_BADRATIO)
	CASE_HRESULT(DB_E_BADVALUES)
	CASE_HRESULT(DB_E_ERRORSINCOMMAND)
	CASE_HRESULT(DB_E_CANTCANCEL)
	CASE_HRESULT(DB_E_DIALECTNOTSUPPORTED)
	CASE_HRESULT(DB_E_DUPLICATEDATASOURCE)
	CASE_HRESULT(DB_E_CANNOTRESTART)
	CASE_HRESULT(DB_E_NOTFOUND)
	CASE_HRESULT(DB_E_NEWLYINSERTED)
	CASE_HRESULT(DB_E_CANNOTFREE)
	CASE_HRESULT(DB_E_GOALREJECTED)
	CASE_HRESULT(DB_E_UNSUPPORTEDCONVERSION)
	CASE_HRESULT(DB_E_BADSTARTPOSITION)
	CASE_HRESULT(DB_E_NOQUERY)
	CASE_HRESULT(DB_E_NOTREENTRANT)
	CASE_HRESULT(DB_E_ERRORSOCCURRED)
	CASE_HRESULT(DB_E_NOAGGREGATION)
	CASE_HRESULT(DB_E_DELETEDROW)
	CASE_HRESULT(DB_E_CANTFETCHBACKWARDS)
	CASE_HRESULT(DB_E_ROWSNOTRELEASED)
	CASE_HRESULT(DB_E_BADSTORAGEFLAG)
	CASE_HRESULT(DB_E_BADCOMPAREOP)
	CASE_HRESULT(DB_E_BADSTATUSVALUE)
	CASE_HRESULT(DB_E_CANTSCROLLBACKWARDS)
	CASE_HRESULT(DB_E_BADREGIONHANDLE)
	CASE_HRESULT(DB_E_NONCONTIGUOUSRANGE)
	CASE_HRESULT(DB_E_INVALIDTRANSITION)
	CASE_HRESULT(DB_E_NOTASUBREGION)
	CASE_HRESULT(DB_E_MULTIPLESTATEMENTS)
	CASE_HRESULT(DB_E_INTEGRITYVIOLATION)
	CASE_HRESULT(DB_E_BADTYPENAME)
	CASE_HRESULT(DB_E_ABORTLIMITREACHED)
	CASE_HRESULT(DB_E_ROWSETINCOMMAND)
	CASE_HRESULT(DB_E_CANTTRANSLATE)
	CASE_HRESULT(DB_E_DUPLICATEINDEXID)
	CASE_HRESULT(DB_E_NOINDEX)
	CASE_HRESULT(DB_E_INDEXINUSE)
	CASE_HRESULT(DB_E_NOTABLE)
	CASE_HRESULT(DB_E_CONCURRENCYVIOLATION)
	CASE_HRESULT(DB_E_BADCOPY)
	CASE_HRESULT(DB_E_BADPRECISION)
	CASE_HRESULT(DB_E_BADSCALE)
	CASE_HRESULT(DB_E_BADTABLEID)
	CASE_HRESULT(DB_E_BADTYPE)
	CASE_HRESULT(DB_E_DUPLICATECOLUMNID)
	CASE_HRESULT(DB_E_DUPLICATETABLEID)
	CASE_HRESULT(DB_E_TABLEINUSE)
	CASE_HRESULT(DB_E_NOLOCALE)
	CASE_HRESULT(DB_E_BADRECORDNUM)
	CASE_HRESULT(DB_E_BOOKMARKSKIPPED)
	CASE_HRESULT(DB_E_BADPROPERTYVALUE)
	CASE_HRESULT(DB_E_INVALID)
	CASE_HRESULT(DB_E_BADACCESSORFLAGS)
	CASE_HRESULT(DB_E_BADSTORAGEFLAGS)
	CASE_HRESULT(DB_E_BYREFACCESSORNOTSUPPORTED)
	CASE_HRESULT(DB_E_NULLACCESSORNOTSUPPORTED)
	CASE_HRESULT(DB_E_NOTPREPARED)
	CASE_HRESULT(DB_E_BADACCESSORTYPE)
	CASE_HRESULT(DB_E_WRITEONLYACCESSOR)
	CASE_HRESULT(DB_E_CANCELED)
	CASE_HRESULT(DB_E_CHAPTERNOTRELEASED)
	CASE_HRESULT(DB_E_BADSOURCEHANDLE)
	CASE_HRESULT(DB_E_PARAMUNAVAILABLE)
	CASE_HRESULT(DB_E_ALREADYINITIALIZED)
	CASE_HRESULT(DB_E_NOTSUPPORTED)
	CASE_HRESULT(DB_E_MAXPENDCHANGESEXCEEDED)
	CASE_HRESULT(DB_E_BADORDINAL)
	CASE_HRESULT(DB_E_PENDINGCHANGES)
	CASE_HRESULT(DB_E_DATAOVERFLOW)
	CASE_HRESULT(DB_E_BADHRESULT)
	CASE_HRESULT(DB_E_BADLOOKUPID)
	CASE_HRESULT(DB_E_BADDYNAMICERRORID)
	CASE_HRESULT(DB_E_PENDINGINSERT)
	CASE_HRESULT(DB_E_BADCONVERTFLAG)
	CASE_HRESULT(DB_E_BADPARAMETERNAME)
	CASE_HRESULT(DB_E_MULTIPLESTORAGE)
	CASE_HRESULT(DB_E_CANTFILTER)
	CASE_HRESULT(DB_E_CANTORDER)
	CASE_HRESULT(DB_E_NOCOLUMN)
	CASE_HRESULT(DB_E_COMMANDNOTPERSISTED)
	CASE_HRESULT(DB_E_DUPLICATEID)
	CASE_HRESULT(DB_E_OBJECTCREATIONLIMITREACHED)
	CASE_HRESULT(DB_E_BADINDEXID)
	CASE_HRESULT(DB_E_BADINITSTRING)
	CASE_HRESULT(DB_E_NOPROVIDERSREGISTERED)
	CASE_HRESULT(DB_E_MISMATCHEDPROVIDER)
	CASE_HRESULT(DB_E_BADCOMMANDID)
	CASE_HRESULT(DB_E_BADCONSTRAINTTYPE)
	CASE_HRESULT(DB_E_BADCONSTRAINTFORM)
	CASE_HRESULT(DB_E_BADDEFERRABILITY)
	CASE_HRESULT(DB_E_BADMATCHTYPE)
	CASE_HRESULT(DB_E_BADUPDATEDELETERULE)
	CASE_HRESULT(DB_E_BADCONSTRAINTID)
	CASE_HRESULT(DB_E_BADCOMMANDFLAGS)
	CASE_HRESULT(DB_E_OBJECTMISMATCH)
	CASE_HRESULT(DB_E_NOSOURCEOBJECT)
	CASE_HRESULT(DB_E_RESOURCELOCKED)
	CASE_HRESULT(DB_E_NOTCOLLECTION)
	CASE_HRESULT(DB_E_READONLY)
	CASE_HRESULT(DB_E_ASYNCNOTSUPPORTED)
	CASE_HRESULT(DB_E_CANNOTCONNECT)
	CASE_HRESULT(DB_E_TIMEOUT)
	CASE_HRESULT(DB_E_RESOURCEEXISTS)
	CASE_HRESULT(DB_E_RESOURCEOUTOFSCOPE)
	CASE_HRESULT(DB_E_DROPRESTRICTED)
	CASE_HRESULT(DB_E_DUPLICATECONSTRAINTID)
	CASE_HRESULT(DB_E_OUTOFSPACE)
	CASE_HRESULT(DB_SEC_E_PERMISSIONDENIED)
	CASE_HRESULT(DB_SEC_E_AUTH_FAILED)
	CASE_HRESULT(DB_SEC_E_SAFEMODE_DENIED)
	CASE_HRESULT(DB_S_ROWLIMITEXCEEDED)
	CASE_HRESULT(DB_S_COLUMNTYPEMISMATCH)
	CASE_HRESULT(DB_S_TYPEINFOOVERRIDDEN)
	CASE_HRESULT(DB_S_BOOKMARKSKIPPED)
	CASE_HRESULT(DB_S_NONEXTROWSET)
	CASE_HRESULT(DB_S_ENDOFROWSET)
	CASE_HRESULT(DB_S_COMMANDREEXECUTED)
	CASE_HRESULT(DB_S_BUFFERFULL)
	CASE_HRESULT(DB_S_NORESULT)
	CASE_HRESULT(DB_S_CANTRELEASE)
	CASE_HRESULT(DB_S_GOALCHANGED)
	CASE_HRESULT(DB_S_UNWANTEDOPERATION)
	CASE_HRESULT(DB_S_DIALECTIGNORED)
	CASE_HRESULT(DB_S_UNWANTEDPHASE)
	CASE_HRESULT(DB_S_UNWANTEDREASON)
	CASE_HRESULT(DB_S_ASYNCHRONOUS)
	CASE_HRESULT(DB_S_COLUMNSCHANGED)
	CASE_HRESULT(DB_S_ERRORSRETURNED)
	CASE_HRESULT(DB_S_BADROWHANDLE)
	CASE_HRESULT(DB_S_DELETEDROW)
	CASE_HRESULT(DB_S_TOOMANYCHANGES)
	CASE_HRESULT(DB_S_STOPLIMITREACHED)
	CASE_HRESULT(DB_S_LOCKUPGRADED)
	CASE_HRESULT(DB_S_PROPERTIESCHANGED)
	CASE_HRESULT(DB_S_ERRORSOCCURRED)
	CASE_HRESULT(DB_S_PARAMUNAVAILABLE)
	CASE_HRESULT(DB_S_MULTIPLECHANGES)
	CASE_HRESULT(DB_S_NOTSINGLETON)
	CASE_HRESULT(DB_S_NOROWSPECIFICCOLUMNS)
	}
	static StrAnsiBufSmall stab;
	stab.Format("(hr = 0x%08x)", hr);
	return stab.Chars();
}

/*----------------------------------------------------------------------------------------------
	Convert the HRESULT code to a UNICODE string containing its name.
----------------------------------------------------------------------------------------------*/
#undef CASE_HRESULT
#define CASE_HRESULT(h)  case h: return L###h;
const wchar_t* __UnicodeHresult(HRESULT hr)
{
	switch (hr)
	{
	CASE_HRESULT(E_UNEXPECTED)
	CASE_HRESULT(E_NOTIMPL)
	CASE_HRESULT(E_OUTOFMEMORY)
	CASE_HRESULT(E_INVALIDARG)
	CASE_HRESULT(E_NOINTERFACE)
	CASE_HRESULT(E_POINTER)
	CASE_HRESULT(E_HANDLE)
	CASE_HRESULT(E_ABORT)
	CASE_HRESULT(E_FAIL)
	CASE_HRESULT(E_ACCESSDENIED)
	CASE_HRESULT(E_PENDING)
	CASE_HRESULT(CO_E_INIT_TLS)
	CASE_HRESULT(CO_E_INIT_SHARED_ALLOCATOR)
	CASE_HRESULT(CO_E_INIT_MEMORY_ALLOCATOR)
	CASE_HRESULT(CO_E_INIT_CLASS_CACHE)
	CASE_HRESULT(CO_E_INIT_RPC_CHANNEL)
	CASE_HRESULT(CO_E_INIT_TLS_SET_CHANNEL_CONTROL)
	CASE_HRESULT(CO_E_INIT_TLS_CHANNEL_CONTROL)
	CASE_HRESULT(CO_E_INIT_UNACCEPTED_USER_ALLOCATOR)
	CASE_HRESULT(CO_E_INIT_SCM_MUTEX_EXISTS)
	CASE_HRESULT(CO_E_INIT_SCM_FILE_MAPPING_EXISTS)
	CASE_HRESULT(CO_E_INIT_SCM_MAP_VIEW_OF_FILE)
	CASE_HRESULT(CO_E_INIT_SCM_EXEC_FAILURE)
	CASE_HRESULT(CO_E_INIT_ONLY_SINGLE_THREADED)
	CASE_HRESULT(CO_E_CANT_REMOTE)
	CASE_HRESULT(CO_E_BAD_SERVER_NAME)
	CASE_HRESULT(CO_E_WRONG_SERVER_IDENTITY)
	CASE_HRESULT(CO_E_OLE1DDE_DISABLED)
	CASE_HRESULT(CO_E_RUNAS_SYNTAX)
	CASE_HRESULT(CO_E_CREATEPROCESS_FAILURE)
	CASE_HRESULT(CO_E_RUNAS_CREATEPROCESS_FAILURE)
	CASE_HRESULT(CO_E_RUNAS_LOGON_FAILURE)
	CASE_HRESULT(CO_E_LAUNCH_PERMSSION_DENIED)
	CASE_HRESULT(CO_E_START_SERVICE_FAILURE)
	CASE_HRESULT(CO_E_REMOTE_COMMUNICATION_FAILURE)
	CASE_HRESULT(CO_E_SERVER_START_TIMEOUT)
	CASE_HRESULT(CO_E_CLSREG_INCONSISTENT)
	CASE_HRESULT(CO_E_IIDREG_INCONSISTENT)
	CASE_HRESULT(CO_E_NOT_SUPPORTED)
	CASE_HRESULT(S_OK)
	CASE_HRESULT(S_FALSE)
	CASE_HRESULT(OLE_E_OLEVERB)
	CASE_HRESULT(OLE_E_ADVF)
	CASE_HRESULT(OLE_E_ENUM_NOMORE)
	CASE_HRESULT(OLE_E_ADVISENOTSUPPORTED)
	CASE_HRESULT(OLE_E_NOCONNECTION)
	CASE_HRESULT(OLE_E_NOTRUNNING)
	CASE_HRESULT(OLE_E_NOCACHE)
	CASE_HRESULT(OLE_E_BLANK)
	CASE_HRESULT(OLE_E_CLASSDIFF)
	CASE_HRESULT(OLE_E_CANT_GETMONIKER)
	CASE_HRESULT(OLE_E_CANT_BINDTOSOURCE)
	CASE_HRESULT(OLE_E_STATIC)
	CASE_HRESULT(OLE_E_PROMPTSAVECANCELLED)
	CASE_HRESULT(OLE_E_INVALIDRECT)
	CASE_HRESULT(OLE_E_WRONGCOMPOBJ)
	CASE_HRESULT(OLE_E_INVALIDHWND)
	CASE_HRESULT(OLE_E_NOT_INPLACEACTIVE)
	CASE_HRESULT(OLE_E_CANTCONVERT)
	CASE_HRESULT(OLE_E_NOSTORAGE)
	CASE_HRESULT(DV_E_FORMATETC)
	CASE_HRESULT(DV_E_DVTARGETDEVICE)
	CASE_HRESULT(DV_E_STGMEDIUM)
	CASE_HRESULT(DV_E_STATDATA)
	CASE_HRESULT(DV_E_LINDEX)
	CASE_HRESULT(DV_E_TYMED)
	CASE_HRESULT(DV_E_CLIPFORMAT)
	CASE_HRESULT(DV_E_DVASPECT)
	CASE_HRESULT(DV_E_DVTARGETDEVICE_SIZE)
	CASE_HRESULT(DV_E_NOIVIEWOBJECT)
	CASE_HRESULT(DRAGDROP_E_NOTREGISTERED)
	CASE_HRESULT(DRAGDROP_E_ALREADYREGISTERED)
	CASE_HRESULT(DRAGDROP_E_INVALIDHWND)
	CASE_HRESULT(CLASS_E_NOAGGREGATION)
	CASE_HRESULT(CLASS_E_CLASSNOTAVAILABLE)
	CASE_HRESULT(VIEW_E_DRAW)
	CASE_HRESULT(REGDB_E_READREGDB)
	CASE_HRESULT(REGDB_E_WRITEREGDB)
	CASE_HRESULT(REGDB_E_KEYMISSING)
	CASE_HRESULT(REGDB_E_INVALIDVALUE)
	CASE_HRESULT(REGDB_E_CLASSNOTREG)
	CASE_HRESULT(REGDB_E_IIDNOTREG)
	CASE_HRESULT(CACHE_E_NOCACHE_UPDATED)
	CASE_HRESULT(OLEOBJ_E_NOVERBS)
	CASE_HRESULT(OLEOBJ_E_INVALIDVERB)
	CASE_HRESULT(INPLACE_E_NOTUNDOABLE)
	CASE_HRESULT(INPLACE_E_NOTOOLSPACE)
	CASE_HRESULT(CONVERT10_E_OLESTREAM_GET)
	CASE_HRESULT(CONVERT10_E_OLESTREAM_PUT)
	CASE_HRESULT(CONVERT10_E_OLESTREAM_FMT)
	CASE_HRESULT(CONVERT10_E_OLESTREAM_BITMAP_TO_DIB)
	CASE_HRESULT(CONVERT10_E_STG_FMT)
	CASE_HRESULT(CONVERT10_E_STG_NO_STD_STREAM)
	CASE_HRESULT(CONVERT10_E_STG_DIB_TO_BITMAP)
	CASE_HRESULT(CLIPBRD_E_CANT_OPEN)
	CASE_HRESULT(CLIPBRD_E_CANT_EMPTY)
	CASE_HRESULT(CLIPBRD_E_CANT_SET)
	CASE_HRESULT(CLIPBRD_E_BAD_DATA)
	CASE_HRESULT(CLIPBRD_E_CANT_CLOSE)
	CASE_HRESULT(MK_E_CONNECTMANUALLY)
	CASE_HRESULT(MK_E_EXCEEDEDDEADLINE)
	CASE_HRESULT(MK_E_NEEDGENERIC)
	CASE_HRESULT(MK_E_UNAVAILABLE)
	CASE_HRESULT(MK_E_SYNTAX)
	CASE_HRESULT(MK_E_NOOBJECT)
	CASE_HRESULT(MK_E_INVALIDEXTENSION)
	CASE_HRESULT(MK_E_INTERMEDIATEINTERFACENOTSUPPORTED)
	CASE_HRESULT(MK_E_NOTBINDABLE)
	CASE_HRESULT(MK_E_NOTBOUND)
	CASE_HRESULT(MK_E_CANTOPENFILE)
	CASE_HRESULT(MK_E_MUSTBOTHERUSER)
	CASE_HRESULT(MK_E_NOINVERSE)
	CASE_HRESULT(MK_E_NOSTORAGE)
	CASE_HRESULT(MK_E_NOPREFIX)
	CASE_HRESULT(MK_E_ENUMERATION_FAILED)
	CASE_HRESULT(CO_E_NOTINITIALIZED)
	CASE_HRESULT(CO_E_ALREADYINITIALIZED)
	CASE_HRESULT(CO_E_CANTDETERMINECLASS)
	CASE_HRESULT(CO_E_CLASSSTRING)
	CASE_HRESULT(CO_E_IIDSTRING)
	CASE_HRESULT(CO_E_APPNOTFOUND)
	CASE_HRESULT(CO_E_APPSINGLEUSE)
	CASE_HRESULT(CO_E_ERRORINAPP)
	CASE_HRESULT(CO_E_DLLNOTFOUND)
	CASE_HRESULT(CO_E_ERRORINDLL)
	CASE_HRESULT(CO_E_WRONGOSFORAPP)
	CASE_HRESULT(CO_E_OBJNOTREG)
	CASE_HRESULT(CO_E_OBJISREG)
	CASE_HRESULT(CO_E_OBJNOTCONNECTED)
	CASE_HRESULT(CO_E_APPDIDNTREG)
	CASE_HRESULT(CO_E_RELEASED)
	CASE_HRESULT(OLE_S_USEREG)
	CASE_HRESULT(OLE_S_STATIC)
	CASE_HRESULT(OLE_S_MAC_CLIPFORMAT)
	CASE_HRESULT(DRAGDROP_S_DROP)
	CASE_HRESULT(DRAGDROP_S_CANCEL)
	CASE_HRESULT(DRAGDROP_S_USEDEFAULTCURSORS)
	CASE_HRESULT(DATA_S_SAMEFORMATETC)
	CASE_HRESULT(VIEW_S_ALREADY_FROZEN)
	CASE_HRESULT(CACHE_S_FORMATETC_NOTSUPPORTED)
	CASE_HRESULT(CACHE_S_SAMECACHE)
	CASE_HRESULT(CACHE_S_SOMECACHES_NOTUPDATED)
	CASE_HRESULT(OLEOBJ_S_INVALIDVERB)
	CASE_HRESULT(OLEOBJ_S_CANNOT_DOVERB_NOW)
	CASE_HRESULT(OLEOBJ_S_INVALIDHWND)
	CASE_HRESULT(INPLACE_S_TRUNCATED)
	CASE_HRESULT(CONVERT10_S_NO_PRESENTATION)
	CASE_HRESULT(MK_S_REDUCED_TO_SELF)
	CASE_HRESULT(MK_S_ME)
	CASE_HRESULT(MK_S_HIM)
	CASE_HRESULT(MK_S_US)
	CASE_HRESULT(MK_S_MONIKERALREADYREGISTERED)
	CASE_HRESULT(CO_E_CLASS_CREATE_FAILED)
	CASE_HRESULT(CO_E_SCM_ERROR)
	CASE_HRESULT(CO_E_SCM_RPC_FAILURE)
	CASE_HRESULT(CO_E_BAD_PATH)
	CASE_HRESULT(CO_E_SERVER_EXEC_FAILURE)
	CASE_HRESULT(CO_E_OBJSRV_RPC_FAILURE)
	CASE_HRESULT(MK_E_NO_NORMALIZED)
	CASE_HRESULT(CO_E_SERVER_STOPPING)
	CASE_HRESULT(MEM_E_INVALID_ROOT)
	CASE_HRESULT(MEM_E_INVALID_LINK)
	CASE_HRESULT(MEM_E_INVALID_SIZE)
	CASE_HRESULT(CO_S_NOTALLINTERFACES)
	CASE_HRESULT(DISP_E_UNKNOWNINTERFACE)
	CASE_HRESULT(DISP_E_MEMBERNOTFOUND)
	CASE_HRESULT(DISP_E_PARAMNOTFOUND)
	CASE_HRESULT(DISP_E_TYPEMISMATCH)
	CASE_HRESULT(DISP_E_UNKNOWNNAME)
	CASE_HRESULT(DISP_E_NONAMEDARGS)
	CASE_HRESULT(DISP_E_BADVARTYPE)
	CASE_HRESULT(DISP_E_EXCEPTION)
	CASE_HRESULT(DISP_E_OVERFLOW)
	CASE_HRESULT(DISP_E_BADINDEX)
	CASE_HRESULT(DISP_E_UNKNOWNLCID)
	CASE_HRESULT(DISP_E_ARRAYISLOCKED)
	CASE_HRESULT(DISP_E_BADPARAMCOUNT)
	CASE_HRESULT(DISP_E_PARAMNOTOPTIONAL)
	CASE_HRESULT(DISP_E_BADCALLEE)
	CASE_HRESULT(DISP_E_NOTACOLLECTION)
	CASE_HRESULT(TYPE_E_BUFFERTOOSMALL)
	CASE_HRESULT(TYPE_E_INVDATAREAD)
	CASE_HRESULT(TYPE_E_UNSUPFORMAT)
	CASE_HRESULT(TYPE_E_REGISTRYACCESS)
	CASE_HRESULT(TYPE_E_LIBNOTREGISTERED)
	CASE_HRESULT(TYPE_E_UNDEFINEDTYPE)
	CASE_HRESULT(TYPE_E_QUALIFIEDNAMEDISALLOWED)
	CASE_HRESULT(TYPE_E_INVALIDSTATE)
	CASE_HRESULT(TYPE_E_WRONGTYPEKIND)
	CASE_HRESULT(TYPE_E_ELEMENTNOTFOUND)
	CASE_HRESULT(TYPE_E_AMBIGUOUSNAME)
	CASE_HRESULT(TYPE_E_NAMECONFLICT)
	CASE_HRESULT(TYPE_E_UNKNOWNLCID)
	CASE_HRESULT(TYPE_E_DLLFUNCTIONNOTFOUND)
	CASE_HRESULT(TYPE_E_BADMODULEKIND)
	CASE_HRESULT(TYPE_E_SIZETOOBIG)
	CASE_HRESULT(TYPE_E_DUPLICATEID)
	CASE_HRESULT(TYPE_E_INVALIDID)
	CASE_HRESULT(TYPE_E_TYPEMISMATCH)
	CASE_HRESULT(TYPE_E_OUTOFBOUNDS)
	CASE_HRESULT(TYPE_E_IOERROR)
	CASE_HRESULT(TYPE_E_CANTCREATETMPFILE)
	CASE_HRESULT(TYPE_E_CANTLOADLIBRARY)
	CASE_HRESULT(TYPE_E_INCONSISTENTPROPFUNCS)
	CASE_HRESULT(TYPE_E_CIRCULARTYPE)
	CASE_HRESULT(STG_E_INVALIDFUNCTION)
	CASE_HRESULT(STG_E_FILENOTFOUND)
	CASE_HRESULT(STG_E_PATHNOTFOUND)
	CASE_HRESULT(STG_E_TOOMANYOPENFILES)
	CASE_HRESULT(STG_E_ACCESSDENIED)
	CASE_HRESULT(STG_E_INVALIDHANDLE)
	CASE_HRESULT(STG_E_INSUFFICIENTMEMORY)
	CASE_HRESULT(STG_E_INVALIDPOINTER)
	CASE_HRESULT(STG_E_NOMOREFILES)
	CASE_HRESULT(STG_E_DISKISWRITEPROTECTED)
	CASE_HRESULT(STG_E_SEEKERROR)
	CASE_HRESULT(STG_E_WRITEFAULT)
	CASE_HRESULT(STG_E_READFAULT)
	CASE_HRESULT(STG_E_SHAREVIOLATION)
	CASE_HRESULT(STG_E_LOCKVIOLATION)
	CASE_HRESULT(STG_E_FILEALREADYEXISTS)
	CASE_HRESULT(STG_E_INVALIDPARAMETER)
	CASE_HRESULT(STG_E_MEDIUMFULL)
	CASE_HRESULT(STG_E_PROPSETMISMATCHED)
	CASE_HRESULT(STG_E_ABNORMALAPIEXIT)
	CASE_HRESULT(STG_E_INVALIDHEADER)
	CASE_HRESULT(STG_E_INVALIDNAME)
	CASE_HRESULT(STG_E_UNKNOWN)
	CASE_HRESULT(STG_E_UNIMPLEMENTEDFUNCTION)
	CASE_HRESULT(STG_E_INVALIDFLAG)
	CASE_HRESULT(STG_E_INUSE)
	CASE_HRESULT(STG_E_NOTCURRENT)
	CASE_HRESULT(STG_E_REVERTED)
	CASE_HRESULT(STG_E_CANTSAVE)
	CASE_HRESULT(STG_E_OLDFORMAT)
	CASE_HRESULT(STG_E_OLDDLL)
	CASE_HRESULT(STG_E_SHAREREQUIRED)
	CASE_HRESULT(STG_E_NOTFILEBASEDSTORAGE)
	CASE_HRESULT(STG_E_EXTANTMARSHALLINGS)
	CASE_HRESULT(STG_E_DOCFILECORRUPT)
	CASE_HRESULT(STG_E_BADBASEADDRESS)
	CASE_HRESULT(STG_E_INCOMPLETE)
	CASE_HRESULT(STG_E_TERMINATED)
	CASE_HRESULT(STG_S_CONVERTED)
	CASE_HRESULT(STG_S_BLOCK)
	CASE_HRESULT(STG_S_RETRYNOW)
	CASE_HRESULT(STG_S_MONITORING)
	CASE_HRESULT(RPC_E_CALL_REJECTED)
	CASE_HRESULT(RPC_E_CALL_CANCELED)
	CASE_HRESULT(RPC_E_CANTPOST_INSENDCALL)
	CASE_HRESULT(RPC_E_CANTCALLOUT_INASYNCCALL)
	CASE_HRESULT(RPC_E_CANTCALLOUT_INEXTERNALCALL)
	CASE_HRESULT(RPC_E_CONNECTION_TERMINATED)
	CASE_HRESULT(RPC_E_SERVER_DIED)
	CASE_HRESULT(RPC_E_CLIENT_DIED)
	CASE_HRESULT(RPC_E_INVALID_DATAPACKET)
	CASE_HRESULT(RPC_E_CANTTRANSMIT_CALL)
	CASE_HRESULT(RPC_E_CLIENT_CANTMARSHAL_DATA)
	CASE_HRESULT(RPC_E_CLIENT_CANTUNMARSHAL_DATA)
	CASE_HRESULT(RPC_E_SERVER_CANTMARSHAL_DATA)
	CASE_HRESULT(RPC_E_SERVER_CANTUNMARSHAL_DATA)
	CASE_HRESULT(RPC_E_INVALID_DATA)
	CASE_HRESULT(RPC_E_INVALID_PARAMETER)
	CASE_HRESULT(RPC_E_CANTCALLOUT_AGAIN)
	CASE_HRESULT(RPC_E_SERVER_DIED_DNE)
	CASE_HRESULT(RPC_E_SYS_CALL_FAILED)
	CASE_HRESULT(RPC_E_OUT_OF_RESOURCES)
	CASE_HRESULT(RPC_E_ATTEMPTED_MULTITHREAD)
	CASE_HRESULT(RPC_E_NOT_REGISTERED)
	CASE_HRESULT(RPC_E_FAULT)
	CASE_HRESULT(RPC_E_SERVERFAULT)
	CASE_HRESULT(RPC_E_CHANGED_MODE)
	CASE_HRESULT(RPC_E_INVALIDMETHOD)
	CASE_HRESULT(RPC_E_DISCONNECTED)
	CASE_HRESULT(RPC_E_RETRY)
	CASE_HRESULT(RPC_E_SERVERCALL_RETRYLATER)
	CASE_HRESULT(RPC_E_SERVERCALL_REJECTED)
	CASE_HRESULT(RPC_E_INVALID_CALLDATA)
	CASE_HRESULT(RPC_E_CANTCALLOUT_ININPUTSYNCCALL)
	CASE_HRESULT(RPC_E_WRONG_THREAD)
	CASE_HRESULT(RPC_E_THREAD_NOT_INIT)
	CASE_HRESULT(RPC_E_VERSION_MISMATCH)
	CASE_HRESULT(RPC_E_INVALID_HEADER)
	CASE_HRESULT(RPC_E_INVALID_EXTENSION)
	CASE_HRESULT(RPC_E_INVALID_IPID)
	CASE_HRESULT(RPC_E_INVALID_OBJECT)
	CASE_HRESULT(RPC_S_CALLPENDING)
	CASE_HRESULT(RPC_S_WAITONTIMER)
	CASE_HRESULT(RPC_E_CALL_COMPLETE)
	CASE_HRESULT(RPC_E_UNSECURE_CALL)
	CASE_HRESULT(RPC_E_TOO_LATE)
	CASE_HRESULT(RPC_E_NO_GOOD_SECURITY_PACKAGES)
	CASE_HRESULT(RPC_E_ACCESS_DENIED)
	CASE_HRESULT(RPC_E_REMOTE_DISABLED)
	CASE_HRESULT(RPC_E_INVALID_OBJREF)
	CASE_HRESULT(RPC_E_UNEXPECTED)
	CASE_HRESULT(NTE_BAD_UID)
	CASE_HRESULT(NTE_BAD_HASH)
	CASE_HRESULT(NTE_BAD_KEY)
	CASE_HRESULT(NTE_BAD_LEN)
	CASE_HRESULT(NTE_BAD_DATA)
	CASE_HRESULT(NTE_BAD_SIGNATURE)
	CASE_HRESULT(NTE_BAD_VER)
	CASE_HRESULT(NTE_BAD_ALGID)
	CASE_HRESULT(NTE_BAD_FLAGS)
	CASE_HRESULT(NTE_BAD_TYPE)
	CASE_HRESULT(NTE_BAD_KEY_STATE)
	CASE_HRESULT(NTE_BAD_HASH_STATE)
	CASE_HRESULT(NTE_NO_KEY)
	CASE_HRESULT(NTE_NO_MEMORY)
	CASE_HRESULT(NTE_EXISTS)
	CASE_HRESULT(NTE_PERM)
	CASE_HRESULT(NTE_NOT_FOUND)
	CASE_HRESULT(NTE_DOUBLE_ENCRYPT)
	CASE_HRESULT(NTE_BAD_PROVIDER)
	CASE_HRESULT(NTE_BAD_PROV_TYPE)
	CASE_HRESULT(NTE_BAD_PUBLIC_KEY)
	CASE_HRESULT(NTE_BAD_KEYSET)
	CASE_HRESULT(NTE_PROV_TYPE_NOT_DEF)
	CASE_HRESULT(NTE_PROV_TYPE_ENTRY_BAD)
	CASE_HRESULT(NTE_KEYSET_NOT_DEF)
	CASE_HRESULT(NTE_KEYSET_ENTRY_BAD)
	CASE_HRESULT(NTE_PROV_TYPE_NO_MATCH)
	CASE_HRESULT(NTE_SIGNATURE_FILE_BAD)
	CASE_HRESULT(NTE_PROVIDER_DLL_FAIL)
	CASE_HRESULT(NTE_PROV_DLL_NOT_FOUND)
	CASE_HRESULT(NTE_BAD_KEYSET_PARAM)
	CASE_HRESULT(NTE_FAIL)
	CASE_HRESULT(NTE_SYS_ERR)
	CASE_HRESULT(TRUST_E_PROVIDER_UNKNOWN)
	CASE_HRESULT(TRUST_E_ACTION_UNKNOWN)
	CASE_HRESULT(TRUST_E_SUBJECT_FORM_UNKNOWN)
	CASE_HRESULT(TRUST_E_SUBJECT_NOT_TRUSTED)
	CASE_HRESULT(DIGSIG_E_ENCODE)
	CASE_HRESULT(DIGSIG_E_DECODE)
	CASE_HRESULT(DIGSIG_E_EXTENSIBILITY)
	CASE_HRESULT(DIGSIG_E_CRYPTO)
	CASE_HRESULT(PERSIST_E_SIZEDEFINITE)
	CASE_HRESULT(PERSIST_E_SIZEINDEFINITE)
	CASE_HRESULT(PERSIST_E_NOTSELFSIZING)
	CASE_HRESULT(TRUST_E_NOSIGNATURE)
	CASE_HRESULT(CERT_E_EXPIRED)
	CASE_HRESULT(CERT_E_VALIDITYPERIODNESTING)
	CASE_HRESULT(CERT_E_ROLE)
	CASE_HRESULT(CERT_E_PATHLENCONST)
	CASE_HRESULT(CERT_E_CRITICAL)
	CASE_HRESULT(CERT_E_PURPOSE)
	CASE_HRESULT(CERT_E_ISSUERCHAINING)
	CASE_HRESULT(CERT_E_MALFORMED)
	CASE_HRESULT(CERT_E_UNTRUSTEDROOT)
	CASE_HRESULT(CERT_E_CHAINING)
	// OLE controls
	CASE_HRESULT(CTL_E_ILLEGALFUNCTIONCALL)
	CASE_HRESULT(CTL_E_OVERFLOW)
	CASE_HRESULT(CTL_E_OUTOFMEMORY)
	CASE_HRESULT(CTL_E_DIVISIONBYZERO)
	CASE_HRESULT(CTL_E_OUTOFSTRINGSPACE)
	CASE_HRESULT(CTL_E_OUTOFSTACKSPACE)
	CASE_HRESULT(CTL_E_BADFILENAMEORNUMBER)
	CASE_HRESULT(CTL_E_FILENOTFOUND)
	CASE_HRESULT(CTL_E_BADFILEMODE)
	CASE_HRESULT(CTL_E_FILEALREADYOPEN)
	CASE_HRESULT(CTL_E_DEVICEIOERROR)
	CASE_HRESULT(CTL_E_FILEALREADYEXISTS)
	CASE_HRESULT(CTL_E_BADRECORDLENGTH)
	CASE_HRESULT(CTL_E_DISKFULL)
	CASE_HRESULT(CTL_E_BADRECORDNUMBER)
	CASE_HRESULT(CTL_E_BADFILENAME)
	CASE_HRESULT(CTL_E_TOOMANYFILES)
	CASE_HRESULT(CTL_E_DEVICEUNAVAILABLE)
	CASE_HRESULT(CTL_E_PERMISSIONDENIED)
	CASE_HRESULT(CTL_E_DISKNOTREADY)
	CASE_HRESULT(CTL_E_PATHFILEACCESSERROR)
	CASE_HRESULT(CTL_E_PATHNOTFOUND)
	CASE_HRESULT(CTL_E_INVALIDPATTERNSTRING)
	CASE_HRESULT(CTL_E_INVALIDUSEOFNULL)
	CASE_HRESULT(CTL_E_INVALIDFILEFORMAT)
	CASE_HRESULT(CTL_E_INVALIDPROPERTYVALUE)
	CASE_HRESULT(CTL_E_INVALIDPROPERTYARRAYINDEX)
	CASE_HRESULT(CTL_E_SETNOTSUPPORTEDATRUNTIME)
	CASE_HRESULT(CTL_E_SETNOTSUPPORTED)
	CASE_HRESULT(CTL_E_NEEDPROPERTYARRAYINDEX)
	CASE_HRESULT(CTL_E_SETNOTPERMITTED)
	CASE_HRESULT(CTL_E_GETNOTSUPPORTEDATRUNTIME)
	CASE_HRESULT(CTL_E_GETNOTSUPPORTED)
	CASE_HRESULT(CTL_E_PROPERTYNOTFOUND)
	CASE_HRESULT(CTL_E_INVALIDCLIPBOARDFORMAT)
	CASE_HRESULT(CTL_E_INVALIDPICTURE)
	CASE_HRESULT(CTL_E_PRINTERERROR)
	CASE_HRESULT(CTL_E_CANTSAVEFILETOTEMP)
	CASE_HRESULT(CTL_E_SEARCHTEXTNOTFOUND)
	CASE_HRESULT(CTL_E_REPLACEMENTSTOOLONG)
	CASE_HRESULT(CLASS_E_NOTLICENSED)
	// OLEDB Error or Status Codes
	CASE_HRESULT(DB_E_BADACCESSORHANDLE)
	CASE_HRESULT(DB_E_ROWLIMITEXCEEDED)
	CASE_HRESULT(DB_E_READONLYACCESSOR)
	CASE_HRESULT(DB_E_SCHEMAVIOLATION)
	CASE_HRESULT(DB_E_BADROWHANDLE)
	CASE_HRESULT(DB_E_OBJECTOPEN)
	CASE_HRESULT(DB_E_BADCHAPTER)
	CASE_HRESULT(DB_E_CANTCONVERTVALUE)
	CASE_HRESULT(DB_E_BADBINDINFO)
	CASE_HRESULT(DB_E_NOTAREFERENCECOLUMN)
	CASE_HRESULT(DB_E_LIMITREJECTED)
	CASE_HRESULT(DB_E_NOCOMMAND)
	CASE_HRESULT(DB_E_COSTLIMIT)
	CASE_HRESULT(DB_E_BADBOOKMARK)
	CASE_HRESULT(DB_E_BADLOCKMODE)
	CASE_HRESULT(DB_E_PARAMNOTOPTIONAL)
	CASE_HRESULT(DB_E_BADCOLUMNID)
	CASE_HRESULT(DB_E_BADRATIO)
	CASE_HRESULT(DB_E_BADVALUES)
	CASE_HRESULT(DB_E_ERRORSINCOMMAND)
	CASE_HRESULT(DB_E_CANTCANCEL)
	CASE_HRESULT(DB_E_DIALECTNOTSUPPORTED)
	CASE_HRESULT(DB_E_DUPLICATEDATASOURCE)
	CASE_HRESULT(DB_E_CANNOTRESTART)
	CASE_HRESULT(DB_E_NOTFOUND)
	CASE_HRESULT(DB_E_NEWLYINSERTED)
	CASE_HRESULT(DB_E_CANNOTFREE)
	CASE_HRESULT(DB_E_GOALREJECTED)
	CASE_HRESULT(DB_E_UNSUPPORTEDCONVERSION)
	CASE_HRESULT(DB_E_BADSTARTPOSITION)
	CASE_HRESULT(DB_E_NOQUERY)
	CASE_HRESULT(DB_E_NOTREENTRANT)
	CASE_HRESULT(DB_E_ERRORSOCCURRED)
	CASE_HRESULT(DB_E_NOAGGREGATION)
	CASE_HRESULT(DB_E_DELETEDROW)
	CASE_HRESULT(DB_E_CANTFETCHBACKWARDS)
	CASE_HRESULT(DB_E_ROWSNOTRELEASED)
	CASE_HRESULT(DB_E_BADSTORAGEFLAG)
	CASE_HRESULT(DB_E_BADCOMPAREOP)
	CASE_HRESULT(DB_E_BADSTATUSVALUE)
	CASE_HRESULT(DB_E_CANTSCROLLBACKWARDS)
	CASE_HRESULT(DB_E_BADREGIONHANDLE)
	CASE_HRESULT(DB_E_NONCONTIGUOUSRANGE)
	CASE_HRESULT(DB_E_INVALIDTRANSITION)
	CASE_HRESULT(DB_E_NOTASUBREGION)
	CASE_HRESULT(DB_E_MULTIPLESTATEMENTS)
	CASE_HRESULT(DB_E_INTEGRITYVIOLATION)
	CASE_HRESULT(DB_E_BADTYPENAME)
	CASE_HRESULT(DB_E_ABORTLIMITREACHED)
	CASE_HRESULT(DB_E_ROWSETINCOMMAND)
	CASE_HRESULT(DB_E_CANTTRANSLATE)
	CASE_HRESULT(DB_E_DUPLICATEINDEXID)
	CASE_HRESULT(DB_E_NOINDEX)
	CASE_HRESULT(DB_E_INDEXINUSE)
	CASE_HRESULT(DB_E_NOTABLE)
	CASE_HRESULT(DB_E_CONCURRENCYVIOLATION)
	CASE_HRESULT(DB_E_BADCOPY)
	CASE_HRESULT(DB_E_BADPRECISION)
	CASE_HRESULT(DB_E_BADSCALE)
	CASE_HRESULT(DB_E_BADTABLEID)
	CASE_HRESULT(DB_E_BADTYPE)
	CASE_HRESULT(DB_E_DUPLICATECOLUMNID)
	CASE_HRESULT(DB_E_DUPLICATETABLEID)
	CASE_HRESULT(DB_E_TABLEINUSE)
	CASE_HRESULT(DB_E_NOLOCALE)
	CASE_HRESULT(DB_E_BADRECORDNUM)
	CASE_HRESULT(DB_E_BOOKMARKSKIPPED)
	CASE_HRESULT(DB_E_BADPROPERTYVALUE)
	CASE_HRESULT(DB_E_INVALID)
	CASE_HRESULT(DB_E_BADACCESSORFLAGS)
	CASE_HRESULT(DB_E_BADSTORAGEFLAGS)
	CASE_HRESULT(DB_E_BYREFACCESSORNOTSUPPORTED)
	CASE_HRESULT(DB_E_NULLACCESSORNOTSUPPORTED)
	CASE_HRESULT(DB_E_NOTPREPARED)
	CASE_HRESULT(DB_E_BADACCESSORTYPE)
	CASE_HRESULT(DB_E_WRITEONLYACCESSOR)
	CASE_HRESULT(DB_E_CANCELED)
	CASE_HRESULT(DB_E_CHAPTERNOTRELEASED)
	CASE_HRESULT(DB_E_BADSOURCEHANDLE)
	CASE_HRESULT(DB_E_PARAMUNAVAILABLE)
	CASE_HRESULT(DB_E_ALREADYINITIALIZED)
	CASE_HRESULT(DB_E_NOTSUPPORTED)
	CASE_HRESULT(DB_E_MAXPENDCHANGESEXCEEDED)
	CASE_HRESULT(DB_E_BADORDINAL)
	CASE_HRESULT(DB_E_PENDINGCHANGES)
	CASE_HRESULT(DB_E_DATAOVERFLOW)
	CASE_HRESULT(DB_E_BADHRESULT)
	CASE_HRESULT(DB_E_BADLOOKUPID)
	CASE_HRESULT(DB_E_BADDYNAMICERRORID)
	CASE_HRESULT(DB_E_PENDINGINSERT)
	CASE_HRESULT(DB_E_BADCONVERTFLAG)
	CASE_HRESULT(DB_E_BADPARAMETERNAME)
	CASE_HRESULT(DB_E_MULTIPLESTORAGE)
	CASE_HRESULT(DB_E_CANTFILTER)
	CASE_HRESULT(DB_E_CANTORDER)
	CASE_HRESULT(DB_E_NOCOLUMN)
	CASE_HRESULT(DB_E_COMMANDNOTPERSISTED)
	CASE_HRESULT(DB_E_DUPLICATEID)
	CASE_HRESULT(DB_E_OBJECTCREATIONLIMITREACHED)
	CASE_HRESULT(DB_E_BADINDEXID)
	CASE_HRESULT(DB_E_BADINITSTRING)
	CASE_HRESULT(DB_E_NOPROVIDERSREGISTERED)
	CASE_HRESULT(DB_E_MISMATCHEDPROVIDER)
	CASE_HRESULT(DB_E_BADCOMMANDID)
	CASE_HRESULT(DB_E_BADCONSTRAINTTYPE)
	CASE_HRESULT(DB_E_BADCONSTRAINTFORM)
	CASE_HRESULT(DB_E_BADDEFERRABILITY)
	CASE_HRESULT(DB_E_BADMATCHTYPE)
	CASE_HRESULT(DB_E_BADUPDATEDELETERULE)
	CASE_HRESULT(DB_E_BADCONSTRAINTID)
	CASE_HRESULT(DB_E_BADCOMMANDFLAGS)
	CASE_HRESULT(DB_E_OBJECTMISMATCH)
	CASE_HRESULT(DB_E_NOSOURCEOBJECT)
	CASE_HRESULT(DB_E_RESOURCELOCKED)
	CASE_HRESULT(DB_E_NOTCOLLECTION)
	CASE_HRESULT(DB_E_READONLY)
	CASE_HRESULT(DB_E_ASYNCNOTSUPPORTED)
	CASE_HRESULT(DB_E_CANNOTCONNECT)
	CASE_HRESULT(DB_E_TIMEOUT)
	CASE_HRESULT(DB_E_RESOURCEEXISTS)
	CASE_HRESULT(DB_E_RESOURCEOUTOFSCOPE)
	CASE_HRESULT(DB_E_DROPRESTRICTED)
	CASE_HRESULT(DB_E_DUPLICATECONSTRAINTID)
	CASE_HRESULT(DB_E_OUTOFSPACE)
	CASE_HRESULT(DB_SEC_E_PERMISSIONDENIED)
	CASE_HRESULT(DB_SEC_E_AUTH_FAILED)
	CASE_HRESULT(DB_SEC_E_SAFEMODE_DENIED)
	CASE_HRESULT(DB_S_ROWLIMITEXCEEDED)
	CASE_HRESULT(DB_S_COLUMNTYPEMISMATCH)
	CASE_HRESULT(DB_S_TYPEINFOOVERRIDDEN)
	CASE_HRESULT(DB_S_BOOKMARKSKIPPED)
	CASE_HRESULT(DB_S_NONEXTROWSET)
	CASE_HRESULT(DB_S_ENDOFROWSET)
	CASE_HRESULT(DB_S_COMMANDREEXECUTED)
	CASE_HRESULT(DB_S_BUFFERFULL)
	CASE_HRESULT(DB_S_NORESULT)
	CASE_HRESULT(DB_S_CANTRELEASE)
	CASE_HRESULT(DB_S_GOALCHANGED)
	CASE_HRESULT(DB_S_UNWANTEDOPERATION)
	CASE_HRESULT(DB_S_DIALECTIGNORED)
	CASE_HRESULT(DB_S_UNWANTEDPHASE)
	CASE_HRESULT(DB_S_UNWANTEDREASON)
	CASE_HRESULT(DB_S_ASYNCHRONOUS)
	CASE_HRESULT(DB_S_COLUMNSCHANGED)
	CASE_HRESULT(DB_S_ERRORSRETURNED)
	CASE_HRESULT(DB_S_BADROWHANDLE)
	CASE_HRESULT(DB_S_DELETEDROW)
	CASE_HRESULT(DB_S_TOOMANYCHANGES)
	CASE_HRESULT(DB_S_STOPLIMITREACHED)
	CASE_HRESULT(DB_S_LOCKUPGRADED)
	CASE_HRESULT(DB_S_PROPERTIESCHANGED)
	CASE_HRESULT(DB_S_ERRORSOCCURRED)
	CASE_HRESULT(DB_S_PARAMUNAVAILABLE)
	CASE_HRESULT(DB_S_MULTIPLECHANGES)
	CASE_HRESULT(DB_S_NOTSINGLETON)
	CASE_HRESULT(DB_S_NOROWSPECIFICCOLUMNS)
	}
	static wchar_t buf[40];
	swprintf(buf, sizeof(buf), L"(hr = 0x%08x)", hr);
	return buf;
}

const wchar* UnicodeHresult(HRESULT hr)
{
	static StrUniBufSmall stub;
	stub = __UnicodeHresult(hr);
	return stub.Chars();
}

#if WIN32
const wchar_t kchDirSep[] = L"\\";
#else//WIN32
const wchar_t kchDirSep[] = L"/";
#endif//WIN32
#ifdef WIN32
/*----------------------------------------------------------------------------------------------
	looks up and returns the FW ICU directory path.
----------------------------------------------------------------------------------------------*/
StrUni DirectoryFinder::IcuDir()
{
	RegKey rk;
	StrUni stuResult(L"C:\\FieldWorks");
	if (rk.InitLm(_T("Software\\SIL")))
	{
		achar rgch[MAX_PATH];
		DWORD cb = isizeof(rgch);
		DWORD dwT;
		LONG nRet = ::RegQueryValueEx(rk, _T("Icu40Dir"), NULL, &dwT, (BYTE *)rgch, &cb);
		if (nRet == ERROR_SUCCESS)
		{
			Assert(dwT == REG_SZ);
			stuResult.Assign(rgch);
		}
	}
	return stuResult;
}
#else

/*----------------------------------------------------------------------------------------------
	looks up and returns the directory where we find Icu Languages.
----------------------------------------------------------------------------------------------*/
StrUni DirectoryFinder::IcuDir()
{
	StrUni stuResult = FwRootDataDir();
	stuResult += kchDirSep;
	stuResult += L"Icu40";
	return stuResult;
}

#endif


/*----------------------------------------------------------------------------------------------
	looks up and returns the FW ICU directory path.
----------------------------------------------------------------------------------------------*/
StrUni DirectoryFinder::IcuDataDir()
{
#ifdef WIN32
	RegKey rk;
	StrUni stuResult(L"C:\\FieldWorks");
	if (rk.InitLm(_T("Software\\SIL")))
	{
		achar rgch[MAX_PATH];
		DWORD cb = isizeof(rgch);
		DWORD dwT;
		LONG nRet = ::RegQueryValueEx(rk, _T("Icu40DataDir"), NULL, &dwT, (BYTE *)rgch, &cb);
		if (nRet == ERROR_SUCCESS)
		{
			Assert(dwT == REG_SZ);
			stuResult.Assign(rgch);
		}
	}
	return stuResult;
#else
	StrUni stuResult = FwRootDataDir();
	stuResult += kchDirSep;
	stuResult += L"Icu40/icudt40l/";
	return stuResult;
#endif
}
/*----------------------------------------------------------------------------------------------
	looks up and returns the FW root data direcory path.
----------------------------------------------------------------------------------------------*/
StrUni DirectoryFinder::FwRootDataDir()
{
#ifdef WIN32
	RegKey rk;
	StrUni stuResult;
	if (rk.InitLm(_T("Software\\SIL\\FieldWorks\\7.0")))
	{
		achar rgch[MAX_PATH];
		DWORD cb = isizeof(rgch);
		DWORD dwT;
		LONG nRet = ::RegQueryValueEx(rk, _T("RootDataDir"), NULL, &dwT, (BYTE *)rgch, &cb);
		if (nRet == ERROR_SUCCESS)
		{
			Assert(dwT == REG_SZ);
			stuResult.Assign(rgch);
		}
	}
	Assert(stuResult.Length() > 0);
	if (!stuResult.Length())
	{
		achar rgch[MAX_PATH];
		if (SUCCEEDED(SHGetFolderPath(NULL, CSIDL_COMMON_APPDATA, NULL, 0, rgch)))
		{
			stuResult.Assign(rgch);
			stuResult += kchDirSep;
			stuResult += "SIL";
			stuResult += kchDirSep;
			stuResult += "FieldWorks 7";
		}
		else
		{
			stuResult.Assign(L"C:\\FieldWorks");
		}
	}
	return stuResult;
#else//WIN32
	const char* fw_root = std::getenv("FW_ROOTDATA");
	return fw_root ? fw_root : "/usr/lib/FieldWorks";
#endif//WIN32
}




/*----------------------------------------------------------------------------------------------
	looks up and returns the FW root code direcory path.
----------------------------------------------------------------------------------------------*/
StrUni DirectoryFinder::FwRootCodeDir()
{
#ifdef WIN32
	RegKey rk;
	StrUni stuResult;
	if (rk.InitLm(_T("Software\\SIL\\FieldWorks\\7.0")))
	{
		achar rgch[MAX_PATH];
		DWORD cb = isizeof(rgch);
		DWORD dwT;
		LONG nRet = ::RegQueryValueEx(rk, _T("RootCodeDir"), NULL, &dwT, (BYTE *)rgch, &cb);
		if (nRet == ERROR_SUCCESS)
		{
			Assert(dwT == REG_SZ);
			stuResult.Assign(rgch);
		}
	}
	Assert(stuResult.Length() > 0);
	if (!stuResult.Length())
		stuResult.Assign(L"C:\\FieldWorks");
	return stuResult;
#else
	const char* fw_root = std::getenv("FW_ROOTCODE");
	return fw_root ? fw_root : ".";
#endif
}

/*----------------------------------------------------------------------------------------------
	looks up and returns the directory where we find templates, generally connected with
	making new databases.
----------------------------------------------------------------------------------------------*/
StrUni DirectoryFinder::FwTemplateDir()
{
	StrUni stuResult = FwRootCodeDir();
	stuResult += kchDirSep;
	stuResult += L"Templates";
	return stuResult;
}
