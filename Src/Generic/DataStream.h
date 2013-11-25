/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: DataStream.h
Original author: Shon Katzenberger
Responsibility: Alistair Imrie
Last reviewed:

	Implements a data stream for editing large amounts of streamed data (like text).
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef DataStream_H
#define DataStream_H 1

/*----------------------------------------------------------------------------------------------
	File class.
	REVIEW ShonK: Should this be specific to DataStream or a general file class?

	@h3{Hungarian: fil}
----------------------------------------------------------------------------------------------*/
class DataFile : public GenRefObj
{
public:
	static void Create(Pcsz pszFile, int grfstgm, DataFile ** ppfil);
	static void CreateTemp(DataFile ** ppfil);

	void Close(void);
	bool GetDelOnClose(void);
	void SetDelOnClose(bool fDel);

	void Read(int ib, void * pv, int cb);
	void ReadInc(int & ib, void * pv, int cb);
	//:> REVIEW ShonK: Should this have Write and WriteInc methods? Or just Append?
	void Write(int ib, const void * pv, int cb);
	void WriteInc(int & ib, const void * pv, int cb);
	int Append(const void * pv, int cb);
	void SetSize(int cb);
	void CopyTo(int ibSrc, int cb, DataFile * pfilDst, int ibDst);

protected:
	HANDLE m_hfile;
	StrApp m_strPath;
	bool m_fDel;

	// Constructor
	DataFile(void)
	{
		Assert(!m_hfile);
		Assert(!m_fDel);
		AssertObj(&m_strPath);
	}
	// Destructor
	~DataFile(void)
	{
		Close();
	}

	void Init(Pcsz pszFile, int grfstgm);

	void Seek(int ib)
	{
		if (SetFilePointer(m_hfile, ib, NULL, FILE_BEGIN) == 0xFFFFFFFF)
			ThrowHr(WarnHr(STG_E_SEEKERROR));
	}
};

typedef GenSmartPtr<DataFile> DataFilePtr;


/*----------------------------------------------------------------------------------------------
	A data stream is a stream of scalars (eg, bytes or characters). It uses files to store
	the information but does extensive caching for performance. Note that the DataStream only
	ever appends to a file and then only to its own temp file. This is so multiple data
	streams can reference the same data without invalidating each other when an edit is
	performed. These use a piece table scheme to represent the data.

	@h3{Hungarian: dast}
----------------------------------------------------------------------------------------------*/
class DataStream : public GenRefObj
{
public:
	// Constructor
	DataStream(int cbElem = isizeof(wchar))
	{
		Assert(cbElem > 0);
		m_cbElem = cbElem;
		AssertObj(this);
	}

	// Destructor
	~DataStream(void)
	{
	}

	// Get the size of the data stream.
	int Size(void)
	{
		AssertObj(this);
		return m_ieLim;
	}

	void Fetch(int ieMin, int ieLim, void * pv);
	void Replace(int ieMin, int ieLimDel, const void * pvIns, int ceIns);
	void Replace(int ieMin, int ieLimDel, DataStream * pdast, int ieMinSrc, int ieLimSrc);

#ifdef DEBUG
	bool AssertValid()
	{
		AssertPtrN(m_qfilTmp.Ptr());
		Assert(m_cbElem > 0 && m_cbElem <= kcbInsChk);
		unsigned cchkInsert = 0;
		if (m_vchk.Size())
			Assert(m_ieLim >= m_vchk.Top()->m_ieMin);
		for (int ichk = 0; ichk < m_vchk.Size(); ++ichk)
		{
			AssertPtrN(m_vchk[ichk].m_qfil.Ptr());
			if (!m_vchk[ichk].m_qfil.Ptr())
			{
				// Insertion chunk.
				++cchkInsert;
				Assert(m_vchk[ichk].m_ib == 0);
				Assert(m_ichkIns == ichk);
			}
			if (ichk == 0)
				Assert(m_vchk[ichk].m_ieMin == 0);
			else
				Assert(m_vchk[ichk].m_ieMin >= m_vchk[ichk - 1].m_ieMin);
		}
		Assert(cchkInsert <= 1);
		Assert(m_ichkIns == -1 || cchkInsert == 1);
		if (!m_vchk.AssertValid())
			return false;
		return true;
	}
#endif // DEBUG

protected:
	struct Chunk
	{
		// Character position of the chunk.
		int m_ieMin;

		// File the chunk comes from. This is NULL for the insertion chunk.
		DataFilePtr m_qfil;

		// Position in the file of the chunk. Zero for the insertion chunk.
		int m_ib;
	};

	// The pieces.
	Vector<Chunk> m_vchk;
	int m_ieLim;

	// Size of each element.
	int m_cbElem;

	// The maximum size of the insertion chunk.
	enum { kcbInsChk = 1024 };

	// The characters for the insertion chunk.
	byte m_rgbIns[kcbInsChk];

	// The insertion chunk. -1 for none.
	int m_ichkIns;

	// The temporary file.
	DataFilePtr m_qfilTmp;

	// Get the ieMin of the ichk'th chunk.
	int IeMinChk(int ichk)
	{
		Assert(ichk <= m_vchk.Size());
		return ichk >= m_vchk.Size() ? m_ieLim : m_vchk[ichk].m_ieMin;
	}

	// Get the ieLim of the ichk'th chunk.
	int IeLimChk(int ichk)
	{
		Assert(ichk <= m_vchk.Size());
		return ichk >= m_vchk.Size() - 1 ? m_ieLim : m_vchk[ichk + 1].m_ieMin;
	}

	// Get the size of the ichk'th chunk.
	int CeChk(int ichk)
	{
		Assert(ichk <= m_vchk.Size());
		return ichk >= IeLimChk(ichk) - IeMinChk(ichk);
	}

	int FindChunk(int ie);
	void AttemptMerge(int ichkMin, int ichkLim);
	void ConvertInsChk(void);

	DataFile * GetTempFile(void)
	{
		if (!m_qfilTmp)
			DataFile::CreateTemp(&m_qfilTmp);
		return m_qfilTmp;
	}

	int AddToTemp(const void * pv, int cb);
	void Adjust(int ichk, int die);
};

typedef GenSmartPtr<DataStream> DataStreamPtr;

#endif // !DataStream_H
