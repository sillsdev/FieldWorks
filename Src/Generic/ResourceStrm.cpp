/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ResourceStrm.cpp
Original author: John Landon
Responsibility: Steve McConnel (was Alistair Imrie)
Last reviewed: Not yet.

Description:
	This class provides an IStream COM interface to a Windows resource object.
----------------------------------------------------------------------------------------------*/
#include "main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	Methods
***********************************************************************************************/
//:End Ignore

static DummyFactory g_fact(_T("SIL.AppCore.ResourceStream"));

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ResourceStream::ResourceStream()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}


/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
ResourceStream::~ResourceStream()
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Static method to create a new ResourceStream object, to open the specified resource for
	reading, and to return the associated IStream interface pointer if successful.
		@param hmod Handle of the module containing the resource
		@param pszType Pointer to resource type
		@param rid Integer identifier of the resource
----------------------------------------------------------------------------------------------*/
void ResourceStream::Create(HMODULE hmod, const achar * pszType,
	int rid, IStream ** ppstrm)
{
	AssertPtr(ppstrm);
	Assert(!*ppstrm);

	ComSmartPtr<ResourceStream> qrest;
	qrest.Attach(NewObj ResourceStream);
	qrest->Init(hmod, pszType, rid);
	*ppstrm = qrest.Detach();
}


/*----------------------------------------------------------------------------------------------
	This method opens the given resource for reading.
		@param hmod Handle of the module containing the resource
		@param pszType Pointer to resource type
		@param rid Integer identifier of the resource
----------------------------------------------------------------------------------------------*/
void ResourceStream::Init(HMODULE hmod, const achar * pszType, int rid)
{
	Assert(hmod);
	// int value assumed for pszType if high 16-bit word is zero
	Assert(((int)pszType > 0 && (int)pszType < 0x10000) || ValidPsz(pszType));
	Assert(rid > 0 && rid < 0x10000); // high 16-bit word must be zero
	// 1. find the resource
	// 2. get the resource size
	// 3. load the resource data
	// 4. lock the resource data into memory, getting a pointer to the data
	HRSRC hResInfo = FindResource(hmod, reinterpret_cast<const achar *>(rid), pszType);
	if (hResInfo == NULL)
		ThrowHr(WarnHr(E_FAIL));
	m_cbData = SizeofResource(hmod, hResInfo);
	if (m_cbData == 0)
		ThrowHr(WarnHr(E_FAIL));
	HGLOBAL hResData = LoadResource(hmod, hResInfo);
	if (hResData == NULL)
		ThrowHr(WarnHr(E_FAIL));
	m_prgbData = (byte *)LockResource(hResData);
	if (m_prgbData == NULL)
		ThrowHr(WarnHr(E_FAIL));
	m_pbCur = m_prgbData;
}


/*----------------------------------------------------------------------------------------------
	Return a pointer to a supported interface.  Only IUnknown and IStream are supported.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IStream)
		*ppv = static_cast<IStream *>(this);
	else if (riid == IID_ISupportErrorInfo)
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
	Increment the reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) ResourceStream::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Decrement the reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) ResourceStream::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Read a specified number of bytes from this stream into memory, starting at the current seek
	pointer.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::Read(void * pv, UCOMINT32 cb, UCOMINT32 * pcbRead)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg((byte *)pv, cb);
	ChkComArgPtrN(pcbRead);
	UCOMINT32 cbRead;

	//Avoid reading past end of resource data.
	if (m_pbCur + cb > m_prgbData + m_cbData)
		cbRead = m_cbData - (m_pbCur - m_prgbData);
	else
		cbRead = cb;
	if (cbRead != 0)
		CopyBytes(m_pbCur, pv, cbRead);
	m_pbCur += cbRead;
	if (pcbRead)
		*pcbRead = cbRead;

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Write a specified number of bytes to the stream, starting at the current seek pointer.
	NOTE: this method is not supported by ResourceStream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::Write(const void * pv, UCOMINT32 cb, UCOMINT32 * pcbWritten)
{
	BEGIN_COM_METHOD;

	// Resource streams are read-only!!
	ThrowHr(WarnHr(STG_E_ACCESSDENIED));

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Change the seek pointer to a new location relative to the beginning of the stream, the end
	of the stream, or the current seek pointer.  ("Seek pointer" is defined as the current
	location for the next read or write operation.)
	For each calculation there is a check for overflow beforehand.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::Seek(LARGE_INTEGER dlibMove, DWORD dwOrigin,
	ULARGE_INTEGER * plibNewPosition)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(plibNewPosition);

	// We can deal only with 32-bit values.
	long cbHigh = dlibMove.HighPart;
	long cbLow = dlibMove.LowPart;
	if (cbHigh != -(cbLow < 0))
		ThrowHr(WarnHr(STG_E_INVALIDPARAMETER));
	byte * pbNew = NULL;
	switch (dwOrigin)
	{
	case STREAM_SEEK_SET:
		if (cbLow < 0 || (uint)(cbLow + m_prgbData) > 0x7fffffff)
			ThrowHr(WarnHr(STG_E_INVALIDPARAMETER));
		pbNew = m_prgbData + cbLow;
		break;
	case STREAM_SEEK_CUR:
		if (m_pbCur + cbLow < m_prgbData || (uint)(cbLow + m_pbCur) > 0x7fffffff)
			ThrowHr(WarnHr(STG_E_INVALIDPARAMETER));
		pbNew = m_pbCur + cbLow;
		break;
	case STREAM_SEEK_END:
		if (m_cbData + cbLow < 0 || (uint)(cbLow + m_prgbData + m_cbData) > 0x7fffffff)
			ThrowHr(WarnHr(STG_E_INVALIDPARAMETER));
		pbNew = m_prgbData + m_cbData + cbLow;
		break;
	default:
		ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));
	}

	m_pbCur = pbNew;
	if (plibNewPosition) // Note: NULL is a valid value for caller to pass.
	{
		plibNewPosition->HighPart = 0;
		plibNewPosition->LowPart = m_pbCur - m_prgbData;
	}
	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Change the size of the data stream.
	NOTE: this is not supported by ResourceStream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::SetSize(ULARGE_INTEGER libNewSize)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(STG_E_ACCESSDENIED));

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Copy a specified number of bytes from the current seek pointer in this stream to the
	current seek pointer in another stream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::CopyTo(IStream * pstm, ULARGE_INTEGER cb, ULARGE_INTEGER * pcbRead,
	ULARGE_INTEGER * pcbWritten)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(STG_E_ACCESSDENIED)); // These streams are always read-only.

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Ensure that any changes made to this transacted stream are reflected in the parent storage
	object.
	NOTE: this is not supported by ResourceStream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::Commit(DWORD grfCommitFlags)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Discard all changes that have been made to this transacted stream since the last
	IStream::Commit call.
	NOTE: this is not supported by ResourceStream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::Revert(void)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Restrict access to a specified range of bytes in this stream.
	NOTE: this is not supported by ResourceStream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb,
	DWORD dwLockType)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Remove the access restriction on a range of bytes previously restricted with
	IStream::LockRegion.
	NOTE: this is not supported by ResourceStream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb,
	DWORD dwLockType)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Retrieve the STATSTG structure for this stream. Note that we will always use integer ids
	for the Resource. It is not thought worthwhile to convert this to a name string here. Hence
	the returned name is always NULL even if the name is requested.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::Stat(STATSTG * pstatstg, DWORD grfStatFlag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstatstg);

	// Clear the whole structure initially
	ClearBytes(pstatstg, isizeof(STATSTG));
	switch (grfStatFlag)
	{
	case STATFLAG_DEFAULT:	// resource will always be identified by an integer id.
	case STATFLAG_NONAME:
		pstatstg->type = STGTY_STREAM;
		pstatstg->cbSize.LowPart = m_cbData;
		return S_OK;

	default:
		ThrowHr(WarnHr(STG_E_INVALIDFLAG));
	}
	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Create a new stream object that references the same bytes as the original stream but
	provides a separate seek pointer to those bytes.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ResourceStream::Clone(IStream ** ppstm)
{
	BEGIN_COM_METHOD;
	// Validate the pointer
	ChkComOutPtr(ppstm);

	// Create a new object
	ComSmartPtr<ResourceStream> qrest;
	qrest.Attach(NewObj ResourceStream);

	// Initialize the new object
	qrest->m_cbData = m_cbData;
	qrest->m_prgbData = m_prgbData;
	qrest->m_pbCur = m_pbCur;

	*ppstm = qrest.Detach();
	END_COM_METHOD(g_fact, IID_IStream);
}
