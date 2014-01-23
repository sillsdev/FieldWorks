/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: StringStrm.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Implementation of StrAnsiStream, an IStream wrapper for StrAnsi objects.
----------------------------------------------------------------------------------------------*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	StrAnsiStream methods.
***********************************************************************************************/
//:End Ignore

static DummyFactory g_factAnsi(_T("SIL.AppCore.StrAnsiStream"));

/*----------------------------------------------------------------------------------------------
	Create a new StrAnsiStream object, returning it through ppstas.

	@param ppstas Address of a pointer to a StrAnsiStream object.
----------------------------------------------------------------------------------------------*/
void StrAnsiStream::Create(StrAnsiStream ** ppstas)
{
	AssertPtr(ppstas);
	*ppstas = NewObj StrAnsiStream();
}

/*----------------------------------------------------------------------------------------------
	Standard COM QueryInterface method.

	@param iid Reference to a COM Interface GUID.
	@param ppv Address of a pointer to receive the desired COM interface pointer, or NULL.

	@return S_OK or E_NOINTERFACE
----------------------------------------------------------------------------------------------*/
STDMETHODIMP StrAnsiStream::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_IStream)
		*ppv = static_cast<IStream *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IStream);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Standard COM AddRef method.

	@return The reference count after incrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) StrAnsiStream::AddRef()
{
	Assert(m_cref > 0);
	return ++m_cref;
}

/*----------------------------------------------------------------------------------------------
	Standard COM Release method.

	@return The reference count after decrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) StrAnsiStream::Release()
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Copy bytes from m_sta to the buffer at pv, setting *pcbRead to the number of bytes placed
	in the buffer (if pcbRead is non-NULL).  The caller can detect "End-of-stream" when
	*pcbRead < cb.

	This implements a standard ISequentialStream method.

	@param pv Pointer to buffer into which the stream is read.
	@param cb Number of bytes to read.
	@param pcbRead Pointer to integer that contains the actual number of bytes read.

	@return S_OK, STG_E_INVALIDPOINTER, or E_FAIL
----------------------------------------------------------------------------------------------*/
STDMETHODIMP StrAnsiStream::Read(void * pv, UCOMINT32 cb, UCOMINT32 * pcbRead)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg((byte *)pv, cb);
	ChkComArgPtrN(pcbRead);

	ulong cbRead = 0;
	int cch = m_sta.Length();
	if (m_ich < cch)
	{
		cbRead = cch - m_ich;
		if (cbRead > cb)
			cbRead = cb;
		memcpy(pv, m_sta.Chars() + m_ich, cbRead);
		m_ich += cbRead;
	}
	if (pcbRead)
		*pcbRead = cbRead;

	END_COM_METHOD(g_factAnsi, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Copy the cb bytes at pv to m_sta.  If pcbWritten is non-NULL, set it to the number of bytes
	copied.

	This implements a standard ISequentialStream method.

	@param pv Pointer to the buffer address from which the stream is written.
	@param cb Number of bytes to write.
	@param pcbWritten Pointer to integer that contains the actual number of bytes written.

	@return S_OK, STG_E_INVALIDPOINTER, or E_FAIL
----------------------------------------------------------------------------------------------*/
STDMETHODIMP StrAnsiStream::Write(const void * pv, UCOMINT32 cb, UCOMINT32 * pcbWritten)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg((byte *)pv, cb);
	ChkComArgPtrN(pcbWritten);

	// Append the input to m_sta.
	if (cb)
	{
		int cch = m_sta.Length();
		if (m_ich < cch)
		{
			int ichLim = m_ich + cb;
			if (ichLim > cch)
				ichLim = cch;
			m_sta.Replace(m_ich, ichLim, (const char *)pv, cb);
			m_ich += cb;
		}
		else
		{
			m_sta.Append((const char *)pv, cb);
			m_ich = m_sta.Length();
		}
	}
	if (pcbWritten)
		*pcbWritten = cb;

	END_COM_METHOD(g_factAnsi, IID_IStream);
}

/*----------------------------------------------------------------------------------------------
	Adjust the stream seek pointer, returning the new value through plibNewPosition.

	This implements a standard IStream method.

	@param dlibMove Offset relative to dwOrigin.
	@param dwOrigin Specifies the origin for the offset: STREAM_SEEK_SET, STREAM_SEEK_CUR, or
					STREAM_SEEK_END.
	@param plibNewPosition Pointer to location containing new seek pointer.

	@return S_OK, STG_E_SEEKERROR, or STG_E_INVALIDFUNCTION.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP StrAnsiStream::Seek(LARGE_INTEGER dlibMove, DWORD dwOrigin,
	ULARGE_INTEGER * plibNewPosition)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(plibNewPosition);

	int cch = m_sta.Length();
	LARGE_INTEGER dlibNew; // attempted new seek position
	switch (dwOrigin)
	{
	case STREAM_SEEK_SET:
		dlibNew.QuadPart = dlibMove.QuadPart;
		break;
	case STREAM_SEEK_CUR:
		dlibNew.QuadPart = dlibMove.QuadPart + m_ich;
		break;
	case STREAM_SEEK_END:
		// Work out new attempted seek pointer value
		dlibNew.LowPart  = cch;
		dlibNew.HighPart = 0;
		dlibNew.QuadPart += dlibMove.QuadPart;
		break;
	default:
		ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));
	}
	if (dlibNew.QuadPart < 0)
		ThrowHr(WarnHr(STG_E_SEEKERROR));

	// Update the current position.
	if (dlibNew.QuadPart <= (int64)cch)
		m_ich = (int)dlibNew.QuadPart;
	else
		m_ich = cch;
	if (plibNewPosition != NULL)
		plibNewPosition->QuadPart = (uint64)dlibNew.QuadPart;
	END_COM_METHOD(g_factAnsi, IID_IStream);
}

//:Ignore
/***********************************************************************************************
	We are not using the remaining methods for anything.  If the XML parser (and possibly other
	applications) didn't need to rewind the input stream, then we could probably get by with
	implementing the ISequentialStream interface instead of the IStream interface.
***********************************************************************************************/

STDMETHODIMP StrAnsiStream::SetSize(ULARGE_INTEGER libNewSize)
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factAnsi, IID_IStream);
}

STDMETHODIMP StrAnsiStream::CopyTo(IStream * pstm, ULARGE_INTEGER cb,
	ULARGE_INTEGER * pcbRead, ULARGE_INTEGER * pcbWritten)
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factAnsi, IID_IStream);
}

STDMETHODIMP StrAnsiStream::Commit(DWORD grfCommitFlags)
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factAnsi, IID_IStream);
}

STDMETHODIMP StrAnsiStream::Revert()
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factAnsi, IID_IStream);
}

STDMETHODIMP StrAnsiStream::LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb,
	DWORD dwLockType)
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factAnsi, IID_IStream);
}

STDMETHODIMP StrAnsiStream::UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb,
	DWORD dwLockType)
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factAnsi, IID_IStream);
}

STDMETHODIMP StrAnsiStream::Stat(STATSTG * pstatstg, DWORD grfStatFlag)
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factAnsi, IID_IStream);
}

STDMETHODIMP StrAnsiStream::Clone(IStream ** ppstm)
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factAnsi, IID_IStream);
}
//:End Ignore
