/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: DataReader.h
Responsibility: Shon Katzenberger
Last reviewed:

	Defines a DataReader base class and derived classes that implement reading from an IStream
	and a byte array.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef DataReader_H
#define DataReader_H 1


/*----------------------------------------------------------------------------------------------
	Base class to read data.
	Hungarian: drdr
----------------------------------------------------------------------------------------------*/
class DataReader
{
public:
	void ReadInt(int * pn)
		{ ReadBuf(pn, isizeof(int)); }

	virtual void ReadBuf(void * pv, int cb) = 0;
	virtual void SeekAbs(int ib) = 0;
	virtual void SeekRel(int dib) = 0;
	virtual int IbCur(void) = 0;
	virtual int Size(void) = 0;
};


/*----------------------------------------------------------------------------------------------
	Implements reading data from an IStream.
	Hungarian: drs
----------------------------------------------------------------------------------------------*/
class DataReaderStrm : public DataReader
{
public:
	DataReaderStrm(IStream * pstrm = NULL)
	{
		Init(pstrm);
	}

	void Init(IStream * pstrm)
	{
		AssertPtrN(pstrm);
		m_qstrm = pstrm;
		m_ibCur = 0;
		if (pstrm)
		{
			STATSTG statstg;
			CheckHr(pstrm->Stat(&statstg, STATFLAG_NONAME));
			m_cb = statstg.cbSize.LowPart;
			Assert(!statstg.cbSize.HighPart);
			LARGE_INTEGER dlib = { 0, 0 };
			ULARGE_INTEGER lib = { 0, 0 };
			CheckHr(pstrm->Seek(dlib, STREAM_SEEK_CUR, &lib));
			m_ibInit = lib.LowPart;
			Assert(!lib.HighPart);
		}
		else
		{
			m_ibInit = 0;
			m_cb = 0;
		}
	}

	virtual void ReadBuf(void * pv, int cb)
	{
		AssertPtrSize(pv, cb);
		if (!m_qstrm)
			ThrowHr(WarnHr(E_UNEXPECTED));

		UCOMINT32 cbRead;
		CheckHr(m_qstrm->Read(pv, cb, &cbRead));
		m_ibCur += cbRead;
		if ((ulong)cb != cbRead)
			ThrowHr(WarnHr(STG_E_READFAULT));
	}

	virtual void SeekAbs(int ib)
	{
		Assert(ib >= 0);
		if (!m_qstrm)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (ib == m_ibCur)
			return;

		SeekStream(m_qstrm, ib - m_ibCur, STREAM_SEEK_CUR);
		m_ibCur = ib;
	}

	virtual void SeekRel(int dib)
	{
		Assert(dib >= -m_ibCur && dib + m_ibCur > dib);
		if (!m_qstrm)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (dib == 0)
			return;

		SeekStream(m_qstrm, dib, STREAM_SEEK_CUR);
		m_ibCur += dib;
	}

	virtual int IbCur(void)
	{
		if (!m_qstrm)
			ThrowHr(WarnHr(E_UNEXPECTED));
		return m_ibCur;
	}

	virtual int Size(void)
	{
		if (!m_qstrm)
			ThrowHr(WarnHr(E_UNEXPECTED));
		return m_cb;
	}

protected:
	IStreamPtr m_qstrm;
	int m_ibInit;
	int m_ibCur;
	int m_cb;
};


/*----------------------------------------------------------------------------------------------
	Implements reading data from a byte array.
	Hungarian: drr
----------------------------------------------------------------------------------------------*/
class DataReaderRgb : public DataReader
{
public:
	DataReaderRgb(const void * pv = NULL, int cb = 0)
	{
		AssertPtrSize(pv, cb);
		m_prgb = reinterpret_cast<const byte *>(pv);
		m_cb = cb;
		m_ibCur = 0;
	}

	void Init(const void * pv, int cb)
	{
		AssertPtrSize(pv, cb);
		m_prgb = reinterpret_cast<const byte *>(pv);
		m_cb = cb;
		m_ibCur = 0;
	}

	virtual void ReadBuf(void * pv, int cb)
	{
		AssertPtrSize(pv, cb);
		Assert(m_ibCur <= m_cb);
		if ((uint)cb > (uint)(m_cb - m_ibCur))
			ThrowHr(WarnHr(E_UNEXPECTED));
		CopyBytes(m_prgb + m_ibCur, pv, cb);
		m_ibCur += cb;
	}

	int IbCur(void)
	{
		Assert(m_ibCur <= m_cb);
		return m_ibCur;
	}

	void Skip(int cb)
	{
		Assert(m_ibCur <= m_cb);
		if ((uint)cb > (uint)(m_cb - m_ibCur))
			ThrowHr(WarnHr(E_UNEXPECTED));
		m_ibCur += cb;
	}

	virtual void SeekAbs(int ib)
	{
		Assert(ib >= 0);
		if (ib < 0 || ib > m_cb)
			ThrowHr(WarnHr(E_INVALIDARG));
		m_ibCur = ib;
	}

	virtual void SeekRel(int dib)
	{
		Assert(dib >= -m_ibCur && dib + m_ibCur > dib);
		int ib = m_ibCur + dib;
		if (ib < 0 || ib >= m_cb)
			ThrowHr(WarnHr(E_INVALIDARG));
		m_ibCur += dib;
	}

	virtual int Size(void)
	{
		return m_cb;
	}

protected:
	const byte * m_prgb;
	int m_cb;
	int m_ibCur;
};

#endif // !DataReader_H
