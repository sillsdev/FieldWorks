/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DataWriter.h
Responsibility: Shon Katzenberger
Last reviewed:

	Defines a DataWriter base class and derived classes that implement writing to an IStream
	and to a byte array.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef DataWriter_H
#define DataWriter_H 1


/*----------------------------------------------------------------------------------------------
	Base class to write data.
	Hungarian: dwrt
----------------------------------------------------------------------------------------------*/
class DataWriter
{
public:
	void WriteInt(int n)
		{ WriteBuf(&n, isizeof(int)); }

	virtual void WriteBuf(const void * pv, int cb) = 0;
	virtual void SeekAbs(int ib) = 0;
	virtual void SeekRel(int dib) = 0;
	virtual int IbCur(void) = 0;
};


/*----------------------------------------------------------------------------------------------
	Implements writing data to an IStream.
	Hungarian: dws
----------------------------------------------------------------------------------------------*/
class DataWriterStrm : public DataWriter
{
public:
	DataWriterStrm(IStream * pstrm = NULL)
	{
		AssertPtrN(pstrm);
		m_qstrm = pstrm;
		m_ibCur = 0;
	}

	void Init(IStream * pstrm)
	{
		AssertPtrN(pstrm);
		m_qstrm = pstrm;
		m_ibCur = 0;
	}

	virtual void WriteBuf(const void * pv, int cb)
	{
		AssertPtrSize(pv, cb);
		AssertPtr(m_qstrm);

		ulong cbWrote = 0;

		CheckHr(m_qstrm->Write(pv, cb, &cbWrote));
		m_ibCur += cbWrote;
		if ((ulong)cb != cbWrote)
			ThrowHr(WarnHr(STG_E_READFAULT));
	}

	virtual void SeekAbs(int ib)
	{
		Assert(ib >= 0);

		if (ib == m_ibCur)
			return;

		SeekStream(m_qstrm, ib - m_ibCur, STREAM_SEEK_CUR);
		m_ibCur = ib;
	}

	virtual void SeekRel(int dib)
	{
		Assert(dib >= -m_ibCur && dib + m_ibCur > dib);

		SeekStream(m_qstrm, dib, STREAM_SEEK_CUR);
		m_ibCur += dib;
	}

	virtual int IbCur(void)
	{
		return m_ibCur;
	}

protected:
	int m_ibCur;
	IStreamPtr m_qstrm;
};


/*----------------------------------------------------------------------------------------------
	Implements writing data to a byte array.
	Hungarian: dwr
----------------------------------------------------------------------------------------------*/
class DataWriterRgb : public DataWriter
{
public:
	DataWriterRgb(void * pv = NULL, int cb = 0, bool fIgnoreError = false)
	{
		Init(pv, cb, fIgnoreError);
	}

	void Init(void * pv, int cb, bool fIgnoreError = false)
	{
		AssertPtrSize(pv, cb);
		m_prgb = reinterpret_cast<byte *>(pv);
		m_cb = cb;
		m_ibCur = 0;
		m_ibMax = 0;
		m_fIgnoreError = fIgnoreError;
	}

	virtual void WriteBuf(const void * pv, int cb)
	{
		AssertPtrSize(pv, cb);
		Assert(m_ibCur + cb >= m_ibCur); // Assert that m_ibCur won't overflow.

		if (m_ibCur + cb > m_cb)
		{
			if (!m_fIgnoreError)
				ThrowHr(WarnHr(E_UNEXPECTED));
			// REVIEW ShonK: Should we bother writing the data that fits?
		}
		else
			CopyBytes(pv, m_prgb + m_ibCur, cb);

		m_ibCur += cb;
		if (m_ibCur > m_ibMax)
			m_ibMax = m_ibCur;
	}

	int IbMax(void)
	{
		return m_ibMax;
	}

	virtual void SeekAbs(int ib)
	{
		Assert(ib >= 0);
		m_ibCur = ib;
	}

	virtual void SeekRel(int dib)
	{
		Assert(dib >= -m_ibCur && dib + m_ibCur > dib);
		m_ibCur += dib;
	}

	virtual int IbCur(void)
	{
		return m_ibCur;
	}

protected:
	byte * m_prgb;
	int m_cb;
	int m_ibCur;
	int m_ibMax;
	bool m_fIgnoreError;
};

#endif // !DataWriter_H
