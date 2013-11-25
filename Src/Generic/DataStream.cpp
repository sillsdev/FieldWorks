/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: DataStream.cpp
Original author: Shon Katzenberger
Responsibility: Alistair Imrie
Last reviewed:

	Implemention of the data stream class.
----------------------------------------------------------------------------------------------*/
#include "main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

// Explicit instantiations.
template class Vector<DataStream::Chunk>;


/***********************************************************************************************
	File class.
	TODO ShonK: implement buffering.
***********************************************************************************************/
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Static method to open/create a file.
----------------------------------------------------------------------------------------------*/
void DataFile::Create(Pcsz pszFile, int grfstgm, DataFile ** ppfil)
{
	AssertPsz(pszFile);
	AssertPtr(ppfil);
	Assert(!*ppfil);

	// TODO ShonK: implement.
	Assert(false);
}


/*----------------------------------------------------------------------------------------------
	Create a temporary file.
----------------------------------------------------------------------------------------------*/
void DataFile::CreateTemp(DataFile ** ppfil)
{
	AssertPtr(ppfil);

	achar szTmp[MAX_PATH + 1];
	achar szFil[MAX_PATH + 1];

	if (!::GetTempPath(MAX_PATH, szTmp))
		ThrowHr(WarnHr(E_FAIL));

	if (!::GetTempFileName(szTmp, _T("TTB"), 0, szFil))
		ThrowHr(WarnHr(E_FAIL));

	DataFilePtr qfil;
	qfil.Attach(NewObj DataFile);
	qfil->Init(szFil, kfstgmReadWrite | kfstgmCreate);
	qfil->m_fDel = true;
	*ppfil = qfil.Detach();
}


/*----------------------------------------------------------------------------------------------
	Open the file.
----------------------------------------------------------------------------------------------*/
void DataFile::Init(Pcsz pszFile, int grfstgm)
{
	AssertPsz(pszFile);
	Assert(*pszFile);
	Assert(!m_hfile);

	HANDLE hfile = 0;
	DWORD dwDesiredAccess = 0;
	DWORD dwShareMode;
	DWORD dwCreationDisposition = 0;

	// Set the dwShareMode parameter.
	if (grfstgm & kfstgmShareDenyNone)
		dwShareMode = FILE_SHARE_READ | FILE_SHARE_WRITE;
	else if (grfstgm & kfstgmShareDenyRead)
		dwShareMode = FILE_SHARE_WRITE;
	else if (grfstgm & kfstgmShareExclusive)
		dwShareMode = 0;
	else
		dwShareMode = FILE_SHARE_READ;

	if (grfstgm & kfstgmReadWrite)
		dwDesiredAccess = GENERIC_READ | GENERIC_WRITE;
	else if (grfstgm & kfstgmWrite)
		dwDesiredAccess = GENERIC_WRITE;
	else
		dwDesiredAccess = GENERIC_READ;

	if (dwDesiredAccess & GENERIC_WRITE)
	{
		if (grfstgm & kfstgmCreate)
			dwCreationDisposition = CREATE_ALWAYS;
		else
			dwCreationDisposition = CREATE_NEW;
	}
	else
		dwCreationDisposition = OPEN_EXISTING;

	hfile = ::CreateFile(pszFile, dwDesiredAccess, dwShareMode, NULL, dwCreationDisposition,
		FILE_ATTRIBUTE_NORMAL, NULL);
	if (hfile == INVALID_HANDLE_VALUE)
		ThrowHr(WarnHr(E_FAIL));

	m_hfile = hfile;
	m_strPath = pszFile;
}


/*----------------------------------------------------------------------------------------------
	Close the file.
----------------------------------------------------------------------------------------------*/
void DataFile::Close(void)
{
	AssertObj(this);
	if (m_hfile)
	{
		::CloseHandle(m_hfile);
		m_hfile = NULL;
		if (m_fDel)
			::DeleteFile(m_strPath);
		m_strPath.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	@return True if the file will be deleted when closed.
----------------------------------------------------------------------------------------------*/
bool DataFile::GetDelOnClose(void)
{
	AssertObj(this);
	return m_fDel;
}


/*----------------------------------------------------------------------------------------------
	Set or clear whether the file should be deleted on close.
----------------------------------------------------------------------------------------------*/
void DataFile::SetDelOnClose(bool fDel)
{
	AssertObj(this);
	m_fDel = fDel;
}


/*----------------------------------------------------------------------------------------------
	Read the data.
----------------------------------------------------------------------------------------------*/
void DataFile::Read(int ib, void * pv, int cb)
{
	Assert(ib >= 0);
	Assert(cb >= 0);
	AssertPtrSize((char *)pv, cb);
	Assert(m_hfile);

	if (!cb)
		return;

	DWORD cbRead = 0;

	Seek(ib);
	if (!ReadFile(m_hfile, pv, cb, &cbRead, NULL) || cbRead != (uint)cb)
		ThrowHr(WarnHr(STG_E_READFAULT));
}


/*----------------------------------------------------------------------------------------------
	Read the data and update ib.
----------------------------------------------------------------------------------------------*/
void DataFile::ReadInc(int & ib, void * pv, int cb)
{
	Read(ib, pv, cb);
	ib += cb;
}


/*----------------------------------------------------------------------------------------------
	Write the data.
----------------------------------------------------------------------------------------------*/
void DataFile::Write(int ib, const void * pv, int cb)
{
	Assert(ib >= 0);
	Assert(cb >= 0);
	AssertPtrSize((char *)pv, cb);
	Assert(m_hfile);

	if (!cb)
		return;

	DWORD cbWrote = 0;

	Seek(ib);
	if (!WriteFile(m_hfile, pv, cb, &cbWrote, NULL) || cbWrote != (uint)cb)
		ThrowHr(WarnHr(STG_E_WRITEFAULT));
}


/*----------------------------------------------------------------------------------------------
	Write the data and update ib.
----------------------------------------------------------------------------------------------*/
void DataFile::WriteInc(int & ib, const void * pv, int cb)
{
	Write(ib, pv, cb);
	ib += cb;
}


/*----------------------------------------------------------------------------------------------
	Append the data to the file and return the offset at which the data was written.
----------------------------------------------------------------------------------------------*/
int DataFile::Append(const void * pv, int cb)
{
	int ib = SetFilePointer(m_hfile, 0, NULL, FILE_END);
	if (ib == 0xFFFFFFFF)
		ThrowHr(WarnHr(STG_E_SEEKERROR));

	if (!cb)
		return ib;

	DWORD cbWrote = 0;

	if (!WriteFile(m_hfile, pv, cb, &cbWrote, NULL) || cbWrote != (uint)cb)
		ThrowHr(WarnHr(STG_E_WRITEFAULT));

	return ib;
}


/*----------------------------------------------------------------------------------------------
	Set the size of the file.
----------------------------------------------------------------------------------------------*/
void DataFile::SetSize(int cb)
{
	Assert(cb >= 0);
	Assert(m_hfile);

	Seek(cb);
	if (!SetEndOfFile(m_hfile))
		ThrowHr(WarnHr(STG_E_ACCESSDENIED));
}


//:>********************************************************************************************
//:>	Data stream implementation.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Fetch elements from ieMin to ieLim into pv.
----------------------------------------------------------------------------------------------*/
void DataStream::Fetch(int ieMin, int ieLim, void * pv)
{
	AssertObj(this);
	Assert((uint)ieMin <= (uint)ieLim && (uint)ieLim <= (uint)Size());
	AssertPtrSize(pv, Mul(ieLim - ieMin, m_cbElem));

	if (ieMin >= ieLim)
		return;

	int ichk = FindChunk(ieMin);
	int ieMinChk = IeMinChk(ichk);
	int ieLimChk = Min(IeLimChk(ichk), ieLim);
	Chunk * pchk = m_vchk.Begin() + ichk;

	// Read data from the first chunk. For the first chunk, we can't assume the chunk starts
	// at ie.
	if (pchk->m_qfil)
	{
		pchk->m_qfil->Read(pchk->m_ib + Mul(ieMin - ieMinChk, m_cbElem), pv,
			Mul(ieLimChk - ieMin, m_cbElem));
	}
	else
	{
		CopyBytes(m_rgbIns + Mul(ieMin - ieMinChk, m_cbElem), pv,
			Mul(ieLimChk - ieMin, m_cbElem));
	}

	while (ieLimChk < ieLim)
	{
		ichk++;
		pchk++;
		ieMinChk = ieLimChk;
		ieLimChk = Min(IeLimChk(ichk), ieLim);

		if (pchk->m_qfil)
		{
			pchk->m_qfil->Read(pchk->m_ib, (byte *)pv + Mul(ieMinChk - ieMin, m_cbElem),
				Mul(ieLimChk - ieMinChk, m_cbElem));
		}
		else
		{
			CopyBytes(m_rgbIns, (byte *)pv + Mul(ieMinChk - ieMin, m_cbElem),
				Mul(ieLimChk - ieMinChk, m_cbElem));
		}
	}

	Assert(ieLimChk == ieLim);
}


/*----------------------------------------------------------------------------------------------
	Replace [ieMin, ieLim) with the given elements.
	Handles the insertion piece.
----------------------------------------------------------------------------------------------*/
void DataStream::Replace(int ieMin, int ieLim, const void * pvIns, int ceIns)
{
	AssertObj(this);
	Assert((uint)ieMin <= (uint)ieLim && (uint)ieLim <= (uint)Size());
	AssertPtrSize(pvIns, Mul(ceIns, m_cbElem));

	// Amount to adjust (above ieLim).
	int die = ceIns - ieLim + ieMin;

	if (!die && !ceIns)
	{
		// Nothing to insert and nothing to delete.
		return;
	}

	// The new chunks.
	Chunk rgchk[2];
	int cchk = 0;

	// The new value of m_ichkIns. This is assigned at the end.
	int ichkInsNew;

	// These are used at the end to copy stuff to the insertion piece.
	int ceLeft;
	int ceRight;
	int ieMinInsChk;
	int ieLimInsChk;

	// Find the chunks.
	int ichkMin = FindChunk(ieMin);
	int ichkLim = FindChunk(ieLim);

	if (m_ichkIns < 0)
	{
		// There is no insertion piece. Create one.
		goto LNewIns;
	}

	// There is an insertion piece. Get its bounds.
	ieMinInsChk = IeMinChk(m_ichkIns);
	ieLimInsChk = IeLimChk(m_ichkIns);

	// If the entire insertion piece is being deleted, just pretend there isn't one.
	if (ieMin <= ieMinInsChk && ieLimInsChk <= ieLim)
		goto LNewIns;

	ceLeft = Max(0, ieMin - ieMinInsChk);
	ceRight = Max(0, ieLimInsChk - ieLim);

	// See if [ieMin, ieLim) touches the insertion piece.
	if (ieLimInsChk < ieMin || ieLim < ieMinInsChk)
	{
		// They don't touch. Convert the insertion piece to a file based piece and create
		// a new insertion piece.
		ConvertInsChk();
		Assert(m_ichkIns < 0);

LNewIns:
		// Ignore any existing insertion piece.
		ichkInsNew = -1;
		ceLeft = ceRight = 0;

		// See if ichkMin should be left alone.
		if (IeMinChk(ichkMin) < ieMin)
			ichkMin++;

		// Add the insertion piece.
		if (ceIns > 0)
		{
			rgchk[cchk].m_ieMin = ieMin;
			if (ceIns <= kcbInsChk / m_cbElem)
			{
				// Create an insertion piece.
				ichkInsNew = ichkMin;
				rgchk[cchk].m_ib = 0;
			}
			else
			{
				rgchk[cchk].m_qfil = GetTempFile();
				rgchk[cchk].m_ib = AddToTemp(pvIns, ceIns);
			}
			cchk++;
		}

		// See if ichkLim needs fixed up.
		if (IeMinChk(ichkLim) < ieLim)
		{
			rgchk[cchk] = m_vchk[ichkLim];
			rgchk[cchk].m_ieMin = ieLim + die;
			cchk++;
			ichkLim++;
		}
	}
	else if (ceLeft + ceIns + ceRight > kcbInsChk / m_cbElem)
	{
		// All the elements won't fit. Create a file based piece and nuke the
		// insertion piece.
		if (ceLeft)
		{
			rgchk[cchk].m_ieMin = ieMinInsChk;
			rgchk[cchk].m_ib = AddToTemp(m_rgbIns, ceLeft);
			AddToTemp(pvIns, ceIns);
			ichkMin = m_ichkIns;
		}
		else
		{
			rgchk[cchk].m_ieMin = ieMin;
			rgchk[cchk].m_ib = AddToTemp(pvIns, ceIns);
			if (IeMinChk(ichkMin) < ieMin)
				ichkMin++;
		}
		rgchk[cchk].m_qfil = GetTempFile();
		cchk++;

		if (ceRight)
		{
			AddToTemp(m_rgbIns + Mul(ieLim - ieMinInsChk, m_cbElem), ceRight);
			ichkLim = m_ichkIns + 1;
		}
		else if (IeMinChk(ichkLim) < ieLim)
		{
			rgchk[cchk] = m_vchk[ichkLim];
			rgchk[cchk].m_ieMin = ieLim + die;
			cchk++;
			ichkLim++;
		}

		// No insertion piece.
		ichkInsNew = -1;
	}
	else
	{
		// All the elements should fit in the insertion piece.
		if (ieMinInsChk <= ieMin)
		{
			// The insertion piece can be left alone.
			Assert(ichkMin == m_ichkIns || ichkMin == m_ichkIns + 1);
			Assert(ichkLim >= m_ichkIns);
			ichkMin = m_ichkIns + 1;
			if (ichkLim < ichkMin)
				ichkLim = ichkMin;
			else if (IeMinChk(ichkLim) < ieLim)
			{
				rgchk[cchk] = m_vchk[ichkLim];
				rgchk[cchk].m_ieMin = ieLim + die;
				cchk++;
				ichkLim++;
			}
			ichkInsNew = m_ichkIns;
		}
		else
		{
			// Need to fix the ieMin field of the insertion piece.
			Assert(ichkMin <= m_ichkIns);
			Assert(ichkLim == m_ichkIns);

			rgchk[cchk].m_ieMin = ieMin;
			rgchk[cchk].m_ib = 0;
			ichkLim = m_ichkIns + 1;
			ichkInsNew = ichkMin;
		}
	}

	Assert(ichkMin <= ichkLim);

	// Do the replace.
	if (cchk || ichkMin < ichkLim)
		m_vchk.Replace(ichkMin, ichkLim, rgchk, cchk);
	ichkLim = ichkMin + cchk;
	m_ichkIns = ichkInsNew;

	// Adjust chunks.
	Adjust(ichkLim, die);

	// Establish the insertion chunk or attempt to merge at the new boundary.
	if (ichkInsNew >= 0)
	{
		// Make room for the new stuff.
		if (ceRight && die)
		{
			MoveBytes(
				m_rgbIns + Mul(ieLim - ieMinInsChk, m_cbElem),
				m_rgbIns + Mul(ceLeft + ceIns, m_cbElem),
				Mul(ceRight, m_cbElem));
		}
		// Copy the new stuff.
		if (ceIns)
			CopyBytes(pvIns, m_rgbIns + Mul(ceLeft, m_cbElem), Mul(ceIns, m_cbElem));
	}
	else
		AttemptMerge(ichkMin, ichkLim);
}


#ifdef TO_DO_IF_NEEDED // ShonK: Fix
void DataStream::Replace(int ieMin, int ieLimDel, DataStream * pdast, int ieMinSrc, int ieLimSrc)
{
}


void DataStream::ReplaceCore(int ieMin, int ieLimDel,
	const Chunk * prgchkIns, int cchkIns, int ieMinSrc, int ieLimSrc, byte * prgbIns)
{
	Assert((uint)ieMin <= (uint)ieLimDel && (uint)ieLimDel <= (uint)Size());
	AssertArray(prgchkIns, cchkIns);
	Assert((uint)ieMinSrc <= (uint)ieLimSrc);

	int ichkMin = FindChunk(ieMin);
	int ichkLim = FindChunk(ieLim);

	// Make sure we'll have enough room for the chunks.
	if (cchkIns > 0 && cchkIns >= ichkLim - ichkMin)
		m_vchk.EnsureSpace(cchkIns - ichkLim + ichkMin + 1);

	// Ensure ieLim is on a run boundary.
	if (IeMinChk(ichkLim) < ieLim && ieLim < m_ieLim)
	{
		Assert(ichkLim < m_vchk.Size() && ieLim < IeLimChk(ichkLim));
		SplitChunk(ichkLim, ieLim);
		ichkLim++;
		if (ieMin == ieLim)
			ichkMin = ichkLim;
	}
	Assert(ieLim == IeMinChk(ichkLim));
	if (ichkMin < ichkLim)
		ichkMin++;

	m_vchk.Replace(ichkMin, ichkLim, prgchkIns, cchkIns);
	ichkLim = ichkMin + cchkIns;

	// die is the amount that indices >= ieLim should be adjusted by after the replace.
	int die = ceIns - ieLim + ieMin;

	if (ieMin > 0)
	{
		for (iv = ichkMin; iv < ichkLim; iv++)
			m_vchk[iv].m_ieMin += ieMin;
	}
	if (die)
	{
		for (iv = ichkLim; iv < m_vchk.Size(); iv++)
			m_vchk[iv].m_ieMin += die;
	}
	m_ieLim += die;

	// See if we can combine on the left.
	if (FAttemptMerge(ichkMin))
		ichkLim--;

	// See if we can combine on the right.
	FAttemptMerge(ichkLim);
}
#endif /*TO_DO_IF_NEEDED*/


/*----------------------------------------------------------------------------------------------
	Protected method to find the chunk containing ie. If ie is the size of the stream buf,
	the returned index is the number of chunks.
----------------------------------------------------------------------------------------------*/
int DataStream::FindChunk(int ie)
{
	Assert((uint)ie <= (uint)Size());

	if (ie >= m_ieLim)
		return m_vchk.Size();
	Assert(m_vchk.Size() > 0);

	int ivMin = 0;
	int ivLim = m_vchk.Size() - 1;

	// Find ichk for which m_vchk[ichk].m_ieMin <= ie
	while (ivMin < ivLim)
	{
		int ivMid = (ivMin + ivLim) / 2;
		if (IeLimChk(ivMid) <= ie)
			ivMin = ivMid + 1;
		else
			ivLim = ivMid;
	}

	Assert(IeMinChk(ivMin) <= ie && ie < IeLimChk(ivMin));
	return ivMin;
}


/*----------------------------------------------------------------------------------------------
	If the chunks at ichk and ichk - 1 can be merged, then merge them and return true.
----------------------------------------------------------------------------------------------*/
void DataStream::AttemptMerge(int ichkMin, int ichkLim)
{
	int ichk;

	if (ichkLim >= m_vchk.Size())
		ichkLim = m_vchk.Size() - 1;
	if (ichkMin > 0)
		ichkMin--;

	for (ichk = ichkLim; --ichk >= ichkMin; )
	{
		Assert(ichk < m_vchk.Size() - 1 && ichk >= 0);
		Assert(IeMinChk(ichk) < IeMinChk(ichk + 1));

		if (m_vchk[ichk].m_qfil == m_vchk[ichk + 1].m_qfil &&
			m_vchk[ichk].m_ib ==
				m_vchk[ichk + 1].m_ib + Mul(IeMinChk(ichk + 1) - IeMinChk(ichk), m_cbElem))
		{
			Assert(m_ichkIns < ichk || m_ichkIns > ichk + 1);
			if (m_ichkIns > ichk)
				m_ichkIns--;
			m_vchk.Delete(ichk + 1);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Add die to m_ieMin for all chunks starting at ichk. Also add die to m_ieLim.
----------------------------------------------------------------------------------------------*/
void DataStream::Adjust(int ichk, int die)
{
	if (!die)
		return;

	for ( ; ichk < m_vchk.Size(); ichk++)
		m_vchk[ichk].m_ieMin += die;

	m_ieLim += die;
}


/*----------------------------------------------------------------------------------------------
	Returns the offset into the file at which the new data was added.
----------------------------------------------------------------------------------------------*/
int DataStream::AddToTemp(const void * pv, int cb)
{
	AssertObj(this);
	Assert(cb >= 0);
	AssertPtrSize(pv, Mul(cb, m_cbElem));

	return GetTempFile()->Append(pv, cb);
}


/*----------------------------------------------------------------------------------------------
	Convert the insertion piece to a file based piece.
----------------------------------------------------------------------------------------------*/
void DataStream::ConvertInsChk(void)
{
	AssertObj(this);

	if (m_ichkIns < 0)
		return;

	int ichk = m_ichkIns;
	int ce = CeChk(ichk);
	Assert(ce > 0);

	int ib = AddToTemp(m_rgbIns, ce);
	m_vchk[ichk].m_qfil = GetTempFile();
	m_vchk[ichk].m_ib = ib;
	m_ichkIns = -1;

	AttemptMerge(ichk, ichk + 1);
}
