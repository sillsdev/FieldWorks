/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TsString.cpp
Responsibility: Jeff Gayle
Last reviewed: 8/25/99

	Implementations of ITsString and the builder interfaces.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE


/*----------------------------------------------------------------------------------------------
	This Class is only used by TsStrBase<TxtBuf>::GetXmlString. Its purpose it to allow
	TsString to return an FieldWorks Xml Representation without the use if IStream.
	Since IStream implementation is now a managed object, The process of getting an xml represntation
	of a TsString was invoking too many CCW (native -> managed) calls.
	This class recives the IStream Writes and returns the results as a BStr.
----------------------------------------------------------------------------------------------*/
class StreamBuffer : IStream
{
protected:

	char * m_buffer;
	char * m_ptr;
	static const int InitialBufferSize = 4096;
	unsigned int m_bufferSize;

public:
	StreamBuffer() : m_bufferSize(InitialBufferSize), m_buffer(new char[InitialBufferSize])
	{
		m_ptr = m_buffer;
	}

	~StreamBuffer()
	{
		delete[] m_buffer;
	}

	// Return a bstr representation of the data. Caller gains ownership.
	BSTR GetBstr()
	{
		int currentPos = m_ptr - m_buffer;
		m_buffer[currentPos] = 0;

		StrUni stu;
		StrUtil::StoreUtf16FromUtf8(m_buffer, currentPos, stu, false);
		BSTR ret = SysAllocStringLen(stu.Bstr(), stu.Length());
		return ret;
	}

	virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void ** ppv) { return E_NOTIMPL; }
	virtual UCOMINT32 STDMETHODCALLTYPE AddRef() { return 0;}
	virtual UCOMINT32 STDMETHODCALLTYPE Release() {	return 0; }


	virtual HRESULT STDMETHODCALLTYPE Read( void *pv, UCOMINT32 cb, UCOMINT32 *pcbRead) {	return E_NOTIMPL; }
	virtual HRESULT STDMETHODCALLTYPE Write( const void *pv, UCOMINT32 cb, UCOMINT32 *pcbWritten)
	{
		unsigned int currentPos = m_ptr - m_buffer;

		// - 1 to ensure we have space for null term.
		while (currentPos + cb > m_bufferSize - 1)
		{
			m_bufferSize *= 2;
			char * oldBuffer = m_buffer;
			m_buffer = new char[m_bufferSize];
			if (currentPos > 0)
				memcpy(m_buffer, oldBuffer, currentPos);
			m_ptr = m_buffer + currentPos;
			currentPos = m_ptr - m_buffer;
			delete[] oldBuffer;
		}

		memcpy(m_ptr, pv, cb);
		*pcbWritten = cb;
		m_ptr += cb;
		return S_OK;
	}

	virtual HRESULT STDMETHODCALLTYPE Seek( LARGE_INTEGER dlibMove, DWORD dwOrigin, ULARGE_INTEGER *plibNewPosition) { return E_NOTIMPL; }
	virtual HRESULT STDMETHODCALLTYPE SetSize( ULARGE_INTEGER libNewSize) { return E_NOTIMPL; }
	virtual HRESULT STDMETHODCALLTYPE CopyTo(IStream *pstm, ULARGE_INTEGER cb, ULARGE_INTEGER *pcbRead, ULARGE_INTEGER *pcbWritten) { return E_NOTIMPL; }
	virtual HRESULT STDMETHODCALLTYPE Commit( DWORD grfCommitFlags){ return E_NOTIMPL; }
	virtual HRESULT STDMETHODCALLTYPE Revert( void) { return E_NOTIMPL; }
	virtual HRESULT STDMETHODCALLTYPE LockRegion( ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType) { return E_NOTIMPL; }
	virtual HRESULT STDMETHODCALLTYPE UnlockRegion( ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType) { return E_NOTIMPL; }
	virtual HRESULT STDMETHODCALLTYPE Stat( STATSTG *pstatstg, DWORD grfStatFlag) {	return E_NOTIMPL; }
	virtual HRESULT STDMETHODCALLTYPE Clone( IStream **ppstm) {	return E_NOTIMPL; }
};


/***********************************************************************************************
	TxtBufSingle implementation.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor for TxtBufSingle.
----------------------------------------------------------------------------------------------*/
TxtBufSingle::TxtBufSingle(void)
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
	Assert(!m_cactLock);
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TxtBufSingle::~TxtBufSingle(void)
{
	Assert(!m_cactLock);
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	This asserts ich is in range and fills in the TsRunInfo.
----------------------------------------------------------------------------------------------*/
void TxtBufSingle::FetchRunAt(int ich, TsRunInfo * ptri, ITsTextProps ** ppttp)
{
	AssertObj(this);
	Assert(0 <= ich && ich <= Cch());
	AssertPtr(ptri);
	AssertPtr(ppttp);
	Assert(!*ppttp);

	ptri->ichMin = 0;
	ptri->ichLim = Cch();
	ptri->irun = 0;

	*ppttp = m_run.m_qttp;
	AddRefObj(*ppttp);
}


/*----------------------------------------------------------------------------------------------
	This asserts irun is zero and fills in the TsRunInfo.
----------------------------------------------------------------------------------------------*/
void TxtBufSingle::FetchRun(int irun, TsRunInfo * ptri, ITsTextProps ** ppttp)
{
	AssertObj(this);
	Assert(0 == irun);
	AssertPtr(ptri);
	AssertPtr(ppttp);
	Assert(!*ppttp);

	ptri->ichMin = 0;
	ptri->ichLim = Cch();
	ptri->irun = 0;

	*ppttp = m_run.m_qttp;
	AddRefObj(*ppttp);
}


/***********************************************************************************************
	Implementation of TxtBufMulti.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TxtBufMulti::TxtBufMulti(void)
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
	Assert(!m_cactLock);
}


/*----------------------------------------------------------------------------------------------
	We have to manually release the smart pointers in the runs since the compiler doesn't
	know how many there are.
----------------------------------------------------------------------------------------------*/
TxtBufMulti::~TxtBufMulti(void)
{
	int irun;

	for (irun = 0; irun < m_crun; irun++)
		Prun(irun)->m_qttp.Clear();

	Assert(!m_cactLock);
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Find the run containing ich. If ich == Cch(), return irunLast (not irunLim). IrunAt always
	returns a valid run index, ie, IrunAt(ich) < Crun().

	Assumptions:
		0 <= ich <= Cch().

	Exit conditions:
		0 <= irun < Crun().
		IchMinRun(irun) <= ich.
		ich < IchLimRun(irun) || ich == Cch() irun = (crun - 1).
----------------------------------------------------------------------------------------------*/
int TxtBufMulti::IrunAt(int ich)
{
	Assert((uint)ich <= (uint)Cch());
	int irunMin = 0;
	int irunLim = m_crun;

	// Perform a binary search.
	while (irunMin < irunLim)
	{
		int irunT = (irunMin + irunLim) >> 1;
		if (ich >= Prun(irunT)->m_ichLim)
			irunMin = irunT + 1;
		else
			irunLim = irunT;
	}
	if (irunMin >= m_crun)
		return m_crun - 1;
	return irunMin;
}


/*----------------------------------------------------------------------------------------------
	Fill in the TsRunInfo given irun.
----------------------------------------------------------------------------------------------*/
void TxtBufMulti::FetchRun(int irun, TsRunInfo * ptri, ITsTextProps ** ppttp)
{
	Assert((uint)irun < (uint)Crun());
	AssertPtr(ptri);
	AssertPtr(ppttp);
	Assert(!*ppttp);

	TxtRun * prun = Prun(irun);

	if (irun)
		ptri->ichMin = prun[-1].IchLim();
	else
		ptri->ichMin = 0;
	ptri->ichLim = prun->IchLim();
	ptri->irun = irun;

	*ppttp = prun->m_qttp;
	AddRefObj(*ppttp);
}


/***********************************************************************************************
	Implementation of TxtBufBldr.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TxtBufBldr::TxtBufBldr(void)
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TxtBufBldr::~TxtBufBldr(void)
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Find the run containing ich. If ich == Cch(), return irunLast (not irunLim). IrunAt always
	returns a valid run index, ie, IrunAt(ich) < Crun().

	Assumptions:
		0 <= ich <= Cch().

	Exit conditions:
		0 <= irun < Crun().
		IchMinRun(irun) <= ich.
		ich < IchLimRun(irun) || ich == Cch() irun = (crun - 1).
----------------------------------------------------------------------------------------------*/
int TxtBufBldr::IrunAt(int ich)
{
	Assert(0 <= ich && ich <= Cch());
	int crun = Crun();
	int irunMin = 0;
	int irunLim = crun;

	// Perform a binary search.
	while (irunMin < irunLim)
	{
		int irunT = (irunMin + irunLim) >> 1;
		if (ich >= m_vrun[irunT].m_ichLim)
			irunMin = irunT + 1;
		else
			irunLim = irunT;
	}
	if (irunMin >= crun)
		return crun - 1;
	return irunMin;
}


/*----------------------------------------------------------------------------------------------
	Fill in the TsRunInfo.
----------------------------------------------------------------------------------------------*/
void TxtBufBldr::FetchRun(int irun, TsRunInfo * ptri, ITsTextProps ** ppttp)
{
	Assert((uint)irun < (uint)Crun());
	AssertPtr(ptri);
	AssertPtr(ppttp);
	Assert(!*ppttp);

	TxtRun * prun = &m_vrun[irun];

	if (irun)
		ptri->ichMin = prun[-1].IchLim();
	else
		ptri->ichMin = 0;
	ptri->ichLim = prun->IchLim();
	ptri->irun = irun;

	*ppttp = prun->m_qttp;
	AddRefObj(*ppttp);
}


#ifdef DEBUG
/*----------------------------------------------------------------------------------------------
	Validate the objects state.
----------------------------------------------------------------------------------------------*/
bool TxtBufBldr::AssertValid(void)
{
	AssertPtr(this);

	int	cch = m_stu.Length();
	int crun = m_vrun.Size();

	Assert(crun > 0);
	if (crun)
	{
		TxtRun * prun = m_vrun.Begin();
		TxtRun * prunLim = m_vrun.End();

		Assert(!cch || prun->m_ichLim > 0);
		Assert(prunLim[-1].m_ichLim == cch);

		for (prun++; prun < prunLim; prun++)
		{
			// Make sure the lim of each run is greater then the previous one.
			Assert(prun[-1].m_ichLim < prun->m_ichLim);
			// Make sure there are no adjacent runs with the same properties and object cookies.
			Assert(!prun->PropsEqual(prun[-1]));
		}
	}

	return true;
}
#endif // DEBUG


/***********************************************************************************************
	Implementation of TsStrBase. This takes one of the TxtBuf classes as a template argument
	and provides implementations of most of the interface methods.
***********************************************************************************************/

static DummyFactory g_fact(_T("SIL.AppCore.TsStrBase<TxtBuf>"));

/*----------------------------------------------------------------------------------------------
	AddRef.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP_(UCOMINT32) TsStrBase<TxtBuf>::AddRef(void)
{
	AssertObj(this);
	Assert(m_cref > 0);
	return InterlockedIncrement(&m_cref);
}


/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP_(UCOMINT32) TsStrBase<TxtBuf>::Release(void)
{
	AssertObj(this);
	Assert(m_cref > 0);
	long cref = InterlockedDecrement(&m_cref);
	if (cref == 0)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}


/*----------------------------------------------------------------------------------------------
	GetFactoryClsid.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::GetFactoryClsid(CLSID * pclsid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pclsid);
	AssertObj(this);

	*pclsid = CLSID_TsStrFactory;

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Serialize the formatting information of the string to the given stream.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::SerializeFmt(IStream * pstrm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);
	AssertObj(this);

	DataWriterStrm dws(pstrm);
	SerializeFmtCore(&dws);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Serialize the formatting information of the string to the given byte array.  If cbMax is
	too small this sets *pcb to the required size and returns S_FALSE.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::SerializeFmtRgb(byte * prgb, int cbMax,
																	   int * pcb)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgb, cbMax);
	ChkComOutPtr(pcb);
	AssertObj(this);

	DataWriterRgb dwr(prgb, cbMax, true /*fIgnoreError*/);

	SerializeFmtCore(&dwr);
	Assert(dwr.IbMax() == dwr.IbCur());
	*pcb = dwr.IbMax();

	return *pcb <= cbMax ? S_OK : S_FALSE;

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Serialize the formatting information of the string to the given DataWriter.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> void TsStrBase<TxtBuf>::SerializeFmtCore(DataWriter * pdwrt)
{
	AssertObj(this);
	AssertPtr(pdwrt);

	// Write the number of runs.
	int crun = BaseClass::Crun();
	pdwrt->WriteInt(crun);
	if (!crun)
		return;

	// Convert the text properties into a byte array, and save the byte index for each run's
	// text property.  Also write the ichMin and text property byte index for each run.
	Vector<byte> vbProp;		// Temporary space for property regions.
	vbProp.Resize(16 * crun);
	Vector<int> vibProp;
	vibProp.Resize(crun);
	TxtRun * prun;
	int irun;
	int irunPrev;
	bool fRepeat;
	int ibNext = 0;
	int cbAvail;
	int cbProp;
	ITsTextPropsPtr qttp;
	int ichMin = 0;
	for (irun = 0; irun < crun; ++irun)
	{
		prun = BaseClass::Prun(irun);
		// Check whether this run's properties are a duplicate of a previous run.
		fRepeat = false;
		qttp = prun->m_qttp;
		for (irunPrev = 0; irunPrev < irun; ++irunPrev)
		{
			if (qttp == BaseClass::Prun(irunPrev)->m_qttp)
			{
				vibProp[irun] = vibProp[irunPrev];
				fRepeat = true;
				break;
			}
		}
		if (!fRepeat)
		{
			// Serialize this run's properties to the temporary buffer.
			cbAvail = vbProp.Size() - ibNext;
			qttp->SerializeRgb(vbProp.Begin() + ibNext, cbAvail, &cbProp);
			if (cbProp > cbAvail)
			{
				vbProp.Resize(vbProp.Size() + cbProp * (crun - irun));
				cbAvail = vbProp.Size() - ibNext;
				qttp->SerializeRgb(vbProp.Begin() + ibNext, cbAvail, &cbProp);
			}
			vibProp[irun] = ibNext;
			ibNext += cbProp;
		}
		// Write the ichMin and ibProp values.
		pdwrt->WriteInt(ichMin);
		pdwrt->WriteInt(vibProp[irun]);
		// Get the ichMin for the next run.
		ichMin = prun->IchLim();
	}

	// Write the collected property information.
	pdwrt->WriteBuf(vbProp.Begin(), ibNext);
}


/*----------------------------------------------------------------------------------------------
	Get the text as a BSTR.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_Text(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);
	AssertObj(this);

	int cch = BaseClass::Cch();
	if (!cch)
	{
		*pbstr = NULL;
		return S_FALSE;
	}
	*pbstr = SysAllocStringLen(BaseClass::Prgch(), cch);
	if (!*pbstr)
		ThrowOutOfMemory();

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Get the length of the text.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_Length(int * pcch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcch);
	AssertObj(this);

	*pcch = BaseClass::Cch();

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Get the number of runs.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_RunCount(int * pcrun)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcrun);
	AssertObj(this);

	*pcrun = BaseClass::Crun();

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Get the run number of the run containing ich. If ich == Cch(), this returns irunLast, ie,
	Crun() - 1. If ich is not in the interval [0, Cch()], it returns an error.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_RunAt(int ich, int * pirun)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pirun);
	if ((uint)ich > (uint)BaseClass::Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	*pirun = BaseClass::IrunAt(ich);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Returns the first character position of the run.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_MinOfRun(int irun, int * pichMin)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichMin);
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	*pichMin = BaseClass::IchMinRun(irun);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Returns the first character position of the next run. If irun == irunLast, returns Cch().
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_LimOfRun(int irun, int * pichLim)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichLim);
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	*pichLim = BaseClass::IchLimRun(irun);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Performs IchMinRun and IchLimRun in a single call.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::GetBoundsOfRun(int irun, int * pichMin,
	int * pichLim)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichMin);
	ChkComOutPtr(pichLim);
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	*pichMin = BaseClass::IchMinRun(irun);
	*pichLim = BaseClass::IchLimRun(irun);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Fills the TsRunInfo, given a character position.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::FetchRunInfoAt(int ich, TsRunInfo * ptri,
	ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptri);
	ChkComOutPtr(ppttp);
	if ((uint)ich > (uint)BaseClass::Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	BaseClass::FetchRunAt(ich, ptri, ppttp);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Fills the TsRunInfo given a run number.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::FetchRunInfo(int irun, TsRunInfo * ptri,
	ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptri);
	ChkComOutPtr(ppttp);
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowInternalError(E_INVALIDARG);
	AssertObj(this);

	BaseClass::FetchRun(irun, ptri, ppttp);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Get the text for the specified run as a BSTR.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_RunText(int irun, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	if (BaseClass::Cch())
	{
		*pbstr = SysAllocStringLen(BaseClass::Prgch() + BaseClass::IchMinRun(irun),
			BaseClass::CchRun(irun));
		if (!*pbstr)
			ThrowOutOfMemory();
	}
	else
	{
		*pbstr = NULL;
		return S_FALSE;
	}
	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Get an arbitrary range of text as a BSTR.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::GetChars(int ichMin, int ichLim,
	BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);
	if ((uint)ichMin > (uint)ichLim || (uint)ichLim > (uint)BaseClass::Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	*pbstr = NULL;
	if (ichMin < ichLim)
	{
		*pbstr = SysAllocStringLen(BaseClass::Prgch() + ichMin, ichLim - ichMin);
		if (!*pbstr)
			ThrowOutOfMemory();
	}
	END_COM_METHOD(g_fact, IID_ITsString);
}

/*----------------------------------------------------------------------------------------------
	Get an substring from the ITsString.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::GetSubstring(int ichMin, int ichLim,
	ITsString ** pptssRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptssRet);
	if ((uint)ichMin > (uint)ichLim || (uint)ichLim > (uint)BaseClass::Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	if (ichMin == 0 && ichLim == BaseClass::Cch())
	{
		// special case: the whole string.
		*pptssRet = this;
		AddRef();
		return S_OK;
	}
	int cch = ichLim - ichMin;
	DataReaderRgb drr(BaseClass::Prgch() + ichMin, cch * isizeof(OLECHAR));
	if (BaseClass::Crun() == 1)
	{
		// optimized special case.
		TsStrSingle::Create(&drr, cch, BaseClass::Prun(0)->m_qttp, pptssRet);
		return S_OK;
	}
	int irunMin = BaseClass::IrunAt(ichMin); // run containing first character
	if (ichLim == ichMin)
	{
		TsStrSingle::Create(&drr, cch, BaseClass::Prun(irunMin)->m_qttp, pptssRet);
		return S_OK;
	}
	int irunLast = BaseClass::IrunAt(ichLim - 1); // run containing last character

	if (irunLast == irunMin)
	{
		TsStrSingle::Create(&drr, cch, BaseClass::Prun(irunMin)->m_qttp, pptssRet);
	}
	else
	{
		int crun = irunLast - irunMin + 1;
		Vector<TxtRun> vrun;
		for (int i = irunMin; i < irunMin + crun ; i++)
		{
			TxtRun run;
			TxtRun * prun = BaseClass::Prun(i);
			run.m_ichLim = prun->m_ichLim - ichMin;
			run.m_qttp = prun->m_qttp;
			vrun.Push(run);
		}
		vrun[crun - 1].m_ichLim = cch;
		TsStrMulti::Create(&drr, vrun.Begin(), irunLast - irunMin + 1, pptssRet);
	}

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Fetch characters into a buffer instead returning a BSTR.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::FetchChars(int ichMin, int ichLim,
	OLECHAR * prgch)
{
	BEGIN_COM_METHOD;
	if ((uint)ichMin > (uint)ichLim || (uint)ichLim > (uint)BaseClass::Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	ChkComArrayArg(prgch, ichLim - ichMin);
	AssertObj(this);

	if (ichMin < ichLim)
		CopyItems(BaseClass::Prgch() + ichMin, prgch, ichLim - ichMin);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	This is a local method that returns a pointer to the characters. The client should not
	write to the buffer!
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::LockText(const OLECHAR ** pprgch,
																int * pcch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pprgch);
	ChkComOutPtr(pcch);
	AssertObj(this);

	Debug(BaseClass::m_cactLock++);

	*pprgch = BaseClass::Prgch();
	*pcch = BaseClass::Cch();

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Balance a call to LockText.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::UnlockText(const OLECHAR * prgch)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgch);
	if (prgch != BaseClass::Prgch())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	Assert(BaseClass::m_cactLock > 0);
	Debug(BaseClass::m_cactLock--);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Locks the text for a given run and returns a pointer to the characters. The client should
	not write to the buffer!
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::LockRun(int irun,
															   const OLECHAR ** pprgch,
															   int * pcch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pprgch);
	ChkComOutPtr(pcch);
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	// REVIEW JeffG(?): Should we keep a lock count?
	*pprgch = BaseClass::Prgch() + BaseClass::IchMinRun(irun);
	*pcch = BaseClass::CchRun(irun);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Balance a call to LockRun.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::UnlockRun(int irun,
																 const OLECHAR * prgch)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgch);
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowHr(WarnHr(E_INVALIDARG));
	if (prgch != BaseClass::Prgch() + BaseClass::IchMinRun(irun))
		ThrowHr(WarnHr(E_POINTER));
	AssertObj(this);

	// REVIEW JeffG(?): Should we keep a lock count?

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Get the properties for a character. If ich == Cch(), returns the properties of the last
	run.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_PropertiesAt(int ich,
																		ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppttp);
	if ((uint)ich > (uint)BaseClass::Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	*ppttp = this->PropsRun(BaseClass::IrunAt(ich));
	AddRefObj(*ppttp);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Get the properties for a run.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_Properties(int irun,
																	  ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppttp);
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	*ppttp = BaseClass::PropsRun(irun);
	AddRefObj(*ppttp);

	END_COM_METHOD(g_fact, IID_ITsString);
}

/*----------------------------------------------------------------------------------------------
	Shortcut to get the string property ktptNamedStyle for the indicated run.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_StringProperty(int irun, int tpt, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowHr(WarnHr(E_INVALIDARG));
	ChkComOutPtr(pbstr);

	((TsTextProps *)BaseClass::PropsRun(irun))->GetStrPropValueInternal(tpt, pbstr);

	END_COM_METHOD(g_fact, IID_ITsString);
}

/*----------------------------------------------------------------------------------------------
	Shortcut to get the indicated string property for the indicated character position.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_StringPropertyAt(int ich, int tpt, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	if ((uint)ich > (uint)BaseClass::Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	ChkComOutPtr(pbstr);

	((TsTextProps *)BaseClass::PropsRun(this->IrunAt(ich)))->GetStrPropValueInternal(tpt, pbstr);

	END_COM_METHOD(g_fact, IID_ITsString);
}

/*----------------------------------------------------------------------------------------------
	Shortcut to get WS at given ich
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_WritingSystem(int irun, int * pws)
{
	BEGIN_COM_METHOD;
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowHr(WarnHr(E_INVALIDARG));
	ChkComOutPtr(pws);

	TsTextProps * pttp = ((TsTextProps *)BaseClass::PropsRun(irun));

	int itip;
	if (pttp->FindIntProp(ktptWs, &itip))
	{
		TsIntProp * ptip = pttp->Ptip(itip);
		*pws = ptip->m_nVal;
		return S_OK;
	}

	*pws = -1;

	END_COM_METHOD(g_fact, IID_ITsString);
}

/*----------------------------------------------------------------------------------------------
	Shortcut to get WS at given ich
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_WritingSystemAt(int ich, int * pws)
{
	BEGIN_COM_METHOD;
	if ((uint)ich > (uint)BaseClass::Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	ChkComOutPtr(pws);

	TsTextProps * pttp = ((TsTextProps *)BaseClass::PropsRun(this->IrunAt(ich)));

	int itip;
	if (pttp->FindIntProp(ktptWs, &itip))
	{
		TsIntProp * ptip = pttp->Ptip(itip);
		*pws = ptip->m_nVal;
		return S_OK;
	}

	*pws = -1;

	END_COM_METHOD(g_fact, IID_ITsString);
}
/*----------------------------------------------------------------------------------------------
	Shortcut to determine whether a run is an ORC
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_IsRunOrc(int irun, ComBool * pfIsOrc)
{

	BEGIN_COM_METHOD;
	if ((uint)irun >= (uint)BaseClass::Crun())
		ThrowHr(WarnHr(E_INVALIDARG));
	ChkComOutPtr(pfIsOrc);

	*pfIsOrc = BaseClass::CchRun(irun) == 1
		&& *(BaseClass::Prgch() + BaseClass::IchMinRun(irun)) == 0xfffc;

	END_COM_METHOD(g_fact, IID_ITsString);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the chars and runs pointers.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::GetRawPtrs(const OLECHAR ** pprgch,
	int * pcch,	const TxtRun ** pprgrun, int * pcrun)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pprgch);
	ChkComArgPtrN(pcch);
	ChkComArgPtrN(pprgrun);
	ChkComArgPtrN(pcrun);
	AssertObj(this);

	if (pprgch)
		*pprgch = BaseClass::Prgch();

	if (pcch)
		*pcch = BaseClass::Cch();

	if (pprgrun)
		*pprgrun = BaseClass::Prun(0);

	if (pcrun)
		*pcrun = BaseClass::Crun();

	END_COM_METHOD(g_fact, IID_ITsStringRaw);
}


/*----------------------------------------------------------------------------------------------
	Create a string builder initialized to the same information as in the string.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::GetBldr(ITsStrBldr ** pptsb)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptsb);
	AssertObj(this);

	TsStrBldr::Create(BaseClass::Prgch(), BaseClass::Cch(), BaseClass::Prun(0),
		BaseClass::Crun(), pptsb);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Create an incremental string builder initialized to the same information as in the string.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::GetIncBldr(ITsIncStrBldr ** pptisb)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptisb);
	AssertObj(this);

	TsIncStrBldr::Create(BaseClass::Prgch(), BaseClass::Cch(), BaseClass::Prun(0),
		BaseClass::Crun(), pptisb);

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Test two TsStrings for equality. They are equal if they have the exact same text,
	formatting, etc.
	TODO JeffG(KenZ); I wasn't sure what I was doing, so this should be reviewed and
		fixed where needed. It should also include objects or anything else that can
		be embedded in strings.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::Equals(ITsString * ptss,
	ComBool * pfEqual)
{
	BEGIN_COM_METHOD;
	//ChkComArgPtr(ptss);
	ChkComOutPtr(pfEqual);
	AssertObj(this);

	*pfEqual = false;
	if (!ptss)
		return S_OK; // Not equal.

	const OLECHAR * pwrgch;
	int cch;
	CheckHr(ptss->LockText(&pwrgch, &cch));
	if (cch != BaseClass::Cch() || u_strcmp(pwrgch, BaseClass::Prgch()) != 0)
	{
		ptss->UnlockText(pwrgch);
		return S_OK; // Not equal.
	}
	ptss->UnlockText(pwrgch);

	int crun;
	CheckHr(ptss->get_RunCount(&crun));
	if (crun != BaseClass::Crun())
		return S_OK; // Not identical properties.

	ITsTextPropsPtr qttpA;
	ITsTextPropsPtr qttpB;
	int ichMinA;
	int ichLimA;
	int ichMinB;
	int ichLimB;
	for (int irun = 0; irun < crun; ++irun)
	{
		CheckHr(ptss->get_Properties(irun, &qttpA));
		CheckHr(this->get_Properties(irun, &qttpB));
		if (!SameObject(qttpA, qttpB))
			return S_OK; // Not identical properties.
		// Test that the bounds of the runs also match (cf. LT-1417)
		ptss->GetBoundsOfRun(irun, &ichMinA, &ichLimA);
		this->GetBoundsOfRun(irun, &ichMinB, &ichLimB);
		if (ichLimA != ichLimB || ichMinA != ichMinB)
			return S_OK; // Not identical runs.
	}

	*pfEqual = true;

	END_COM_METHOD(g_fact, IID_ITsString);
}

/*----------------------------------------------------------------------------------------------
	Write this string to the stream in the standard FieldWorks XML format.  This is complicated
	by the demands of FieldWorks generic export using special property tags to indicate field
	boundaries and item boundaries.  It is also complicated by generic export not being able to
	handle embedded pictures or links.

	@param pstrm Pointer to the output stream.
	@param pwsf Pointer to an ILgWritingSystemFactory so that we can convert writing system
					integer codes (which are database object ids) to the corresponding strings.
	@param cchIndent Number of spaces to indent.  May be zero to indicate no indenting.
	@param ws If nonzero, the writing system for a multilingual string (<AStr>).  If zero, then
					this is a monolingual string (<Str>).
	@param fWriteObjData If true, then write out embedded pictures and links.  If false, ignore
					any runs that contain such objects.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::WriteAsXml(IStream * pstrm,
	ILgWritingSystemFactory * pwsf, int cchIndent, int ws, ComBool fWriteObjData)
{
	return WriteAsXmlExtended(pstrm, pwsf, cchIndent, ws, fWriteObjData, false);
}

/*----------------------------------------------------------------------------------------------
	Write this string to a Bstr in the standard FieldWorks XML format.  This is complicated
	by the demands of FieldWorks generic export using special property tags to indicate field
	boundaries and item boundaries.  It is also complicated by generic export not being able to
	handle embedded pictures or links.

	@param pstrm Pointer to the output stream.
	@param pwsf Pointer to an ILgWritingSystemFactory so that we can convert writing system
					integer codes (which are database object ids) to the corresponding strings.
	@param cchIndent Number of spaces to indent.  May be zero to indicate no indenting.
	@param ws If nonzero, the writing system for a multilingual string (<AStr>).  If zero, then
					this is a monolingual string (<Str>).
	@param fWriteObjData If true, then write out embedded pictures and links.  If false, ignore
					any runs that contain such objects.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::GetXmlString(ILgWritingSystemFactory * pwsf, int cchIndent, int ws, ComBool fWriteObjData, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	StreamBuffer cachedStreamToPreventCCWCalls;
	WriteAsXmlExtended((IStream*)&cachedStreamToPreventCCWCalls, pwsf, cchIndent, ws, fWriteObjData, false);

	*pbstr = cachedStreamToPreventCCWCalls.GetBstr();

	END_COM_METHOD(g_fact, IID_ITsString);
}


/*----------------------------------------------------------------------------------------------
	Return the RFC4646bis equivalent of the given ICU Locale.
----------------------------------------------------------------------------------------------*/
static void ConvertToRFC4646(BSTR bstrIcu, StrUni & stuRFC)
{
	StrAnsi staIcu(bstrIcu);
	UErrorCode uerr = U_ZERO_ERROR;
	char rgchLang[256];
	int cch = uloc_getLanguage(staIcu.Chars(), rgchLang, 255, &uerr);
	if (uerr == U_ZERO_ERROR)
		rgchLang[cch] = '\0';
	char rgchScript[256];
	cch = uloc_getScript(staIcu.Chars(), rgchScript, 255, &uerr);
	if (uerr == U_ZERO_ERROR)
		rgchScript[cch] = '\0';
	char rgchCountry[256];
	if (uerr == U_ZERO_ERROR)
		cch = uloc_getCountry(staIcu.Chars(), rgchCountry, 255, &uerr);
	rgchCountry[cch] = '\0';
	char rgchVariant[256];
	if (uerr == U_ZERO_ERROR)
		cch = uloc_getVariant(staIcu.Chars(), rgchVariant, 255, &uerr);
	rgchVariant[cch] = '\0';
	if (uerr != U_ZERO_ERROR)
	{
		stuRFC.Format(L"x-%b", bstrIcu);
	}
	else
	{
		if (rgchLang[0] == 'x' && strlen(rgchLang) == 4)
			stuRFC.Format(L"x-%S", rgchLang + 1);
		else
			stuRFC.Assign(rgchLang);
		if (rgchScript[0] != 0)
			stuRFC.FormatAppend(L"-%S", rgchScript);
		if (rgchCountry[0] != 0)
			stuRFC.FormatAppend(L"-%S", rgchCountry);
		if (rgchVariant[0] != 0)
		{
			if (strcmp(rgchVariant, "IPA") == 0)
				stuRFC.Append(L"-fonipa");
			else
				stuRFC.FormatAppend(L"-x-%S", rgchVariant);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Write this string to the stream in the standard FieldWorks XML format.  This is complicated
	by the demands of FieldWorks generic export using special property tags to indicate field
	boundaries and item boundaries.  It is also complicated by generic export not being able to
	handle embedded pictures or links.

	@param pstrm Pointer to the output stream.
	@param pwsf Pointer to an ILgWritingSystemFactory so that we can convert writing system
					integer codes (which are database object ids) to the corresponding strings.
	@param cchIndent Number of spaces to indent.  May be zero to indicate no indenting.
	@param ws If nonzero, the writing system for a multilingual string (<AStr>).  If zero, then
					this is a monolingual string (<Str>).
	@param fWriteObjData If true, then write out embedded pictures and links.  If false, ignore
					any runs that contain such objects.
	@param fUseRFC4646 If true, then write ws attributes using RFC4646 values.  If false, use
					the ICULocale values.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::WriteAsXmlExtended(IStream * pstrm,
	ILgWritingSystemFactory * pwsf, int cchIndent, int ws, ComBool fWriteObjData,
	ComBool fUseRFC4646)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);
	ChkComArgPtr(pwsf);

	// We export only the NFSC form (NFC with exceptions for the parallel style information).
	if (!IsKnownNormalized(knmNFSC))
	{
		ITsStringPtr qtss;
		CheckHr(get_NormalizedForm(knmNFSC, &qtss));
		return qtss->WriteAsXmlExtended(pstrm, pwsf, cchIndent, ws, fWriteObjData, fUseRFC4646);
	}

	if (cchIndent < 0)
		cchIndent = 0;		// Ignore negative numbers.
	Vector<char> vchIndent;
	if (cchIndent)
	{
		vchIndent.Resize(cchIndent + 3);
		memset(vchIndent.Begin(), ' ', cchIndent + 2);
	}
	const char * pszIndent = cchIndent ? vchIndent.Begin() + 2 : "";
	const char * pszIndent2 = cchIndent ? vchIndent.Begin() : "";

	if (ws)
	{
		SmartBstr sbstrWs;
		CheckHr(pwsf->GetStrFromWs(ws, &sbstrWs));
		if (!sbstrWs.Length())
			ThrowInternalError(E_INVALIDARG, "Writing system invalid for <AStr>");
		FormatToStream(pstrm, "%s<AStr ws=\"", pszIndent);
		if (fUseRFC4646)
		{
			StrUni stuRFC;
			ConvertToRFC4646(sbstrWs, stuRFC);
			WriteXmlUnicode(pstrm, stuRFC.Chars(), stuRFC.Length());
		}
		else
		{
		WriteXmlUnicode(pstrm, sbstrWs.Chars(), sbstrWs.Length());
		}
		FormatToStream(pstrm, "\">%n", pszIndent);
	}
	else
	{
		FormatToStream(pstrm, "%s<Str>%n", pszIndent);
	}

	// Write the properties and text for each run.
	int crun;
	CheckHr(get_RunCount(&crun));
	int ctip;
	int ctsp;
	int tpt;
	int nVar;
	int nVal;
	TsRunInfo tri;
	int cch;
	SmartBstr sbstrField;
	for (int irun = 0; irun < crun; ++irun)
	{
		SmartBstr sbstrFieldRun;
		SmartBstr sbstrPropVal;
		ITsTextPropsPtr qttp;
		const byte * pbPict = NULL;
		int cbPict = 0;
		bool fHotGuid = false;
		CheckHr(FetchRunInfo(irun, &tri, &qttp));
		CheckHr(qttp->get_IntPropCount(&ctip));
		CheckHr(qttp->get_StrPropCount(&ctsp));
		CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstrPropVal));
		if (sbstrPropVal.Length() && !fWriteObjData)
		{
			// WorldPad can't handle embedded pictures or guid links: ignore this run if it
			// falls into one of those categories.
			wchar chType = sbstrPropVal.Chars()[0];
			if (chType == kodtPictEvenHot || chType == kodtPictOddHot
				|| chType == kodtNameGuidHot || chType == kodtOwnNameGuidHot)
			{
				continue;
			}
		}
		bool fMarkItem;
		CheckHr(qttp->GetStrPropValue(ktptFieldName, &sbstrFieldRun));
		if (sbstrField != sbstrFieldRun)
		{
			if (sbstrField.Length())
				FormatToStream(pstrm, "%s</Field>%n", pszIndent2);
			if (sbstrFieldRun.Length())
				FormatToStream(pstrm, "%s<Field name=\"%S\">%n",
					pszIndent2, sbstrFieldRun.Chars());
			sbstrField = sbstrFieldRun;
			sbstrFieldRun.Clear();
		}
		CheckHr(qttp->GetIntPropValues(ktptMarkItem, &nVar, &nVal));
		if (nVal == kttvForceOn && nVar == ktpvEnum)
		{
			FormatToStream(pstrm, "%s<Item><Run", pszIndent2);
			fMarkItem = true;
		}
		else
		{

			FormatToStream(pstrm, "%s<Run", pszIndent2);
			fMarkItem = false;
		}
		for (int itip = 0; itip < ctip; itip++)
		{
			CheckHr(qttp->GetIntProp(itip, &tpt, &nVar, &nVal));
			if (fUseRFC4646 && (tpt == ktptWs || tpt == ktptBaseWs))
			{
				if (nVal == 0)
				{
					Assert(nVar == 0);
				}
				else
				{
					const char * pszAttr = (tpt == ktptWs) ? "ws" : "wsBase";
					SmartBstr sbstrWs;
					CheckHr(pwsf->GetStrFromWs(nVal, &sbstrWs));
					if (!sbstrWs.Length())
					{
						StrAnsi staErr;
						staErr.Format("Writing system invalid for <%s ws>", pszAttr);
						ThrowInternalError(E_INVALIDARG, staErr.Chars());
					}
					FormatToStream(pstrm, " %s=\"", pszAttr);
					StrUni stuRFC;
					ConvertToRFC4646(sbstrWs, stuRFC);
					WriteXmlUnicode(pstrm, stuRFC.Chars(), stuRFC.Length());
					FormatToStream(pstrm, "\"", nVal);
				}
			}
			else if (tpt != ktptMarkItem)
			{
				FwXml::WriteIntTextProp(pstrm, pwsf, tpt, nVar, nVal);
		}
		}
		for (int itsp = 0; itsp < ctsp; itsp++)
		{
			CheckHr(qttp->GetStrProp(itsp, &tpt, &sbstrPropVal));
			Assert(tpt != ktptBulNumFontInfo && tpt != ktptWsStyle);
			FwXml::WriteStrTextProp(pstrm, tpt, sbstrPropVal);
			if (tpt == ktptObjData)
			{
				wchar chType = sbstrPropVal.Chars()[0];
				switch (chType)
				{
				// The element data associated with a picture is the actual picture data
				// since it is much too large to want embedded as an XML attribute value.
				// (This is an antique kludge that isn't really used in practice, but some
				// of our test data still exercises it.)
				case kodtPictEvenHot:
					cbPict = (sbstrPropVal.Length() - 1) * isizeof(OLECHAR);
					pbPict = reinterpret_cast<const byte *>(sbstrPropVal.Chars() + 1);
					break;
				case kodtPictOddHot:
					cbPict = (sbstrPropVal.Length() - 1) * isizeof(OLECHAR) - 1;
					pbPict = reinterpret_cast<const byte *>(sbstrPropVal.Chars() + 1);
					break;

				// The generated XML contains both the link value as an attribute and the
				// (possibly edited) display string as the run's element data.
				case kodtExternalPathName:
					break;

				// used ONLY in the clipboard...contains XML representation of (currently) a footnote.
				case kodtEmbeddedObjectData:
					break;

				// The string data associated with this run is assumed to be a dummy magic
				// character that flags (redundantly for XML) that the actual data to
				// display is based on the ktptObjData attribute.
				case kodtNameGuidHot:
				case kodtOwnNameGuidHot:
				case kodtContextString:
				case kodtGuidMoveableObjDisp:
					fHotGuid = true;
					break;
				}
			}
		}
		if (pbPict && cbPict)
		{
			FormatToStream(pstrm, ">");
			// Write the bytes of the picture data.
			for (int ib = 0; ib < cbPict; ++ib)
			{
				FormatToStream(pstrm, "%02x", pbPict[ib]);
				if (ib % 32 == 31)
					FormatToStream(pstrm, "%n");
			}
		}
		else if (!fHotGuid)
		{
			cch = tri.ichLim - tri.ichMin;
			if (cch > 0)
			{
				SmartBstr sbstrRun;
				CheckHr(get_RunText(irun, &sbstrRun));
				if (IsAllWhiteSpace(sbstrRun.Chars(), sbstrRun.Length()))
					FormatToStream(pstrm, " xml:space=\"preserve\">");
				else
					FormatToStream(pstrm, ">");

				WriteXmlUnicode(pstrm, sbstrRun.Chars(), cch);
			}
			else
				FormatToStream(pstrm, ">");
		}
		else
			FormatToStream(pstrm, ">");

		if (fMarkItem)
			FormatToStream(pstrm, "</Run></Item>%n");
		else
			FormatToStream(pstrm, "</Run>%n");
	}
	if (sbstrField.Length())
		FormatToStream(pstrm, "%s</Field>%n", pszIndent2);

	if (ws)
		FormatToStream(pstrm, "%s</AStr>%n", pszIndent);
	else
		FormatToStream(pstrm, "%s</Str>%n", pszIndent);

	END_COM_METHOD(g_fact, IID_ITsString);
}
// Each implementation has methods GetFlagByte and SetFlagByte


template<class TxtBuf> bool TsStrBase<TxtBuf>::IsKnownNormalized(FwNormalizationMode nm)
{
	return GetFlagByte() & (1 << nm);
}

template<class TxtBuf> void TsStrBase<TxtBuf>::NoteNormalized(FwNormalizationMode nm)
{
	byte flag = GetFlagByte();
	flag |=  (1 << nm);
	switch(nm)
	{
	case knmNFKD:
		flag |= (1 << knmNFD);
		break;
	case knmNFKC:
		flag |= (1 << knmNFC);
		// fall through
	case knmNFC:
		flag |= (1 << knmNFSC);
		break;
	}
	SetFlagByte(flag);
}

/*----------------------------------------------------------------------------------------------
	Return whether the string is already in the specified normal form.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_IsNormalizedForm(
	FwNormalizationMode nm, ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet); // sets *pfRet to false by default.
	if (IsKnownNormalized(nm))
	{
		*pfRet = true;
		return S_OK;
	}
	// Todo..what if not known?
	const OLECHAR * prgchContents;
	int cch;
	CheckHr(LockText(&prgchContents, &cch));
	// empty strings are always normalized, so if empty we are normalized
	if (cch == 0)
	{
		CheckHr(UnlockText(prgchContents));
		*pfRet = true;
		NoteNormalized(nm);
		return S_OK;
	}
	try
	{
		UnicodeString usInput(prgchContents, cch);
		const Normalizer2* norm = SilUtil::GetIcuNormalizer((UNormalizationMode)(nm == knmNFSC ? knmNFC : nm));
		UErrorCode uerr = U_ZERO_ERROR;
		if (norm->isNormalized(usInput, uerr))
		{
			Assert(U_SUCCESS(uerr));
			NoteNormalized(nm == knmNFSC ? knmNFC : nm);	// remember, NFC => NFSC
			CheckHr(UnlockText(prgchContents));
			*pfRet = true;
			return S_OK;
		}
		else if (nm == knmNFSC)
		{
			Assert(U_SUCCESS(uerr));
			ITsStringPtr qtssNorm;
			CheckHr(get_NormalizedForm(nm, &qtssNorm));
			CheckHr(UnlockText(prgchContents));
			CheckHr(Equals(qtssNorm, pfRet));
			if (*pfRet)
				NoteNormalized(nm);
			return S_OK;
		}
		// else leave pfRet false and return S_OK;
		Assert(U_SUCCESS(uerr));
	}
	catch(...)
	{
		CheckHr(UnlockText(prgchContents));
		throw;
	}
	CheckHr(UnlockText(prgchContents));

	END_COM_METHOD(g_fact, IID_ITsString);
}

// qsort function for sorting an array of pointers to integers by the magnitude of the
// integers pointed to.
int compareIntPtrs(const void * ppv1, const void * ppv2)
{
	return **((int **)ppv1) - **((int **)ppv2);
}

/*----------------------------------------------------------------------------------------------
	Method object that is used in normalization of TsString data.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> class TsNormalizeMethod
{
	FwNormalizationMode m_nm; // The normalization mode we want to apply.
	ITsString ** m_pptssRet; // Where to put the output
	TsStrBase<TxtBuf> * m_ptssThis; // The string we're normalizing
	Vector<OLECHAR> m_vch; // buffer of characters in current run not yet added to builder.
	UChar32 m_ch; // Character from norm->current we're trying to add to the output.
	int m_cchBuf; // count of current actually valid characters in m_vch.
	ITsStrBldrPtr m_qtsbResult; // Result is accumulated here.
	// Text props to apply to characters in m_vch.
	ITsTextPropsPtr m_qttpBuf;
	const OLECHAR * m_prgchInput; // The characters we're normalizing.
	int m_cchInput; // How many input characters there are.
	int m_ichLimRun; // Limit of current input run.
	int m_iRun;
	// nominal position in input of current character. When reordering occurs,
	// several characters in succession may be output with input position that of the
	// base character. Hence may also function as the limit of characters to deal with
	// in HandleReordering.
	int m_ichInput;
	// Value of m_ichInput last time round the loop. Beginning of input run
	// for HandleReordering.
	int m_ichLastInput;
	// Number of characters in m_vch known for sure to have m_qttpBuf as their properties.
	// Serves as the min of the range to fix in HandleReordering.
	int m_cchBufGood;
	// These two variables define a list of offsets into the string that need to be
	// adjusted in place if affected by normalization.
	int ** m_prgpichOffsetsToFix;
	int m_cichOffsetsToFix;
	int m_iichNextOffsetToFix;

public:
	TsNormalizeMethod(FwNormalizationMode nm, ITsString ** pptssRet,
		TsStrBase<TxtBuf> *ptssThis)
	{
		m_nm = nm;
		m_pptssRet = pptssRet;
		m_ptssThis = ptssThis;
		m_cichOffsetsToFix = 0;
		m_prgpichOffsetsToFix = NULL;
		m_iichNextOffsetToFix = 0;
	}
	// Set and sort the OffsetsToFix, if any
	void SetOffsetsToFix(int ** prgpichOffsetsToFix, int cichOffsetsToFix)
	{
		if (!cichOffsetsToFix)
			return;
		m_cichOffsetsToFix = cichOffsetsToFix;
		m_prgpichOffsetsToFix = prgpichOffsetsToFix;
		qsort(m_prgpichOffsetsToFix, m_cichOffsetsToFix, isizeof(int *), compareIntPtrs);
	}
	//Add one or two characters to m_vch (growing it if necessary) and adjust m_cchBuf to
	//indicate the new amount of it that is in use.
	void Add32BitCharToVch()
	{
		Add32BitCharToVch(m_vch, m_cchBuf, m_ch);
	}
	void Add32BitCharToVch(Vector<OLECHAR> & vch, int & cchBuf, UChar32 ch )
	{
		if (vch.Size() <  cchBuf + 2)
			vch.Resize(cchBuf * 3 / 2); // Min size is 10, so this adds at least 2.

		UErrorCode uerr = U_ZERO_ERROR;
		int32_t cchAdded;
		u_strFromUTF32(
			vch.Begin() + cchBuf,
			2, // never need to add more than 2, don't need trailing null
			& cchAdded,
			& ch, // simulate input array
			1, // ...with just one character in it
			&uerr);
		Assert(U_SUCCESS(uerr));
		cchBuf += cchAdded;
	}
	// Flush the buffer into the string builder.
	void FlushBuffer()
	{
		FlushBuffer(m_cchBuf);
		m_cchBufGood = 0;
		m_cchBuf = 0;
	}
	void FlushBuffer(int & cchBuf)
	{
		if (cchBuf != 0)
		{
			int cchSoFar;
			CheckHr(m_qtsbResult->get_Length(&cchSoFar));
			// Add run to new string
			CheckHr(m_qtsbResult->ReplaceRgch(cchSoFar, cchSoFar, m_vch.Begin(), cchBuf,
				m_qttpBuf));
		}
	}
	// Handle reordering around a run boundary. The last cchReorder characters in m_qtsbResult
	// are the result of normalizing the input chracters from [m_ichLastInput, m_ichInput);
	// but their properties may not be correct, as the normalizer treated them as a single group
	// but the input character properties vary.
	// We normalize each input character individually and try to find matches in the output.
	void HandleReordering(int cchReorder)
	{
		Assert(m_cchBuf == 0); // Should always be called right after flushbuffer
		// Get the output characters we need to check into a UnicodeString.
		int cchSoFar;
		CheckHr(m_qtsbResult->get_Length(&cchSoFar));
		Assert(cchSoFar >= cchReorder);
		int ichMinProblem = cchSoFar - cchReorder;
		if (cchReorder > m_vch.Size())
			m_vch.Resize(cchReorder * 3 / 2);
		CheckHr(m_qtsbResult->FetchChars(ichMinProblem, cchSoFar, m_vch.Begin()));
		UnicodeString ucWholeOutput(m_vch.Begin(), cchSoFar - ichMinProblem);
		m_cchBuf = 0;

		int cchProcess = 1;
		for (int ich = m_ichLastInput; ich < m_ichInput; ich += cchProcess)
		{
			cchProcess =  1;
			if (U16_IS_LEAD(m_prgchInput[ich]) && ich < m_ichInput - 1 &&
				U16_IS_TRAIL(m_prgchInput[ich + 1]))
			{
				cchProcess = 2;
			}
			UnicodeString ucInput(m_prgchInput + ich, cchProcess);
			UErrorCode uerr = U_ZERO_ERROR;
			const Normalizer2* norm = SilUtil::GetIcuNormalizer((UNormalizationMode) m_nm);
			UnicodeString ucOutput = norm->normalize(ucInput, uerr);
			ITsTextPropsPtr qttp;
			CheckHr(m_ptssThis->get_PropertiesAt(ich, &qttp));
			for (int ich2 = 0; ich2 < ucOutput.length(); ich2++)
			{
				// Note: we can ignore surrogates here because if a surrogate pair
				// occurs we can just handle them one at a time, the same as a decomposition.
				int ichPos = ucWholeOutput.indexOf(ucOutput.charAt(ich2));
				if (ichPos >= 0)
				{
					// Make sure we never use this target position again
					ucWholeOutput.setCharAt(ichPos, 0xffff);
					// Set props of that part of string builder to those of corresponding input
					CheckHr(m_qtsbResult->SetProperties(ichPos + ichMinProblem,
						ichPos + ichMinProblem + 1,
						qttp));
				}
			}
		}
	}

	// Between m_cchBufGood and m_cchBuf is some output in NFC.
	// However, the corresponding input between m_ichLastInput and m_ichInput has run boundaries
	// which may interfere with doing such compression.
	// We may assume that no reordering is going on (because we always do NFD before NFSC).
	// First, flush m_cchBufGood characters.
	void HandleStyledComposition()
	{
		FlushBuffer(m_cchBufGood);
		m_cchBuf = 0;
		m_cchBufGood = 0;
		// Note: the two members of a surrogate pair should never have different
		// properties, so we don't have to deal with that special case.
		int run1, run2;
		CheckHr(m_ptssThis->get_RunAt(m_ichLastInput, &run1));
		CheckHr(m_ptssThis->get_RunAt(m_ichInput - 1, &run2));

		if (m_ichInput - m_ichLastInput > 2)
		{
			// It is possible we can compress some characters at the start
			// of the sequence that have common properties. Unicode canonical
			// decompositions are so organized that if a longer sequence
			// can be composed, so can any leading subsequence.
			int ichLimOfRun;
			CheckHr(m_ptssThis->get_LimOfRun(run1, &ichLimOfRun));
			if (ichLimOfRun - m_ichLastInput > 1)
			{
				// We can compress the leading (ichLimOfRun - m_ichLastInput) characters
				UnicodeString input(m_prgchInput + m_ichLastInput, ichLimOfRun - m_ichLastInput);
				const Normalizer2* norm = SilUtil::GetIcuNormalizer(UNORM_NFC);
				UErrorCode uerr = U_ZERO_ERROR;
				UnicodeString output = norm->normalize(input, uerr);
				for (int i = 0; i < output.length(); output.getChar32Limit(++i))
				{
					m_ch = output.char32At(i);
					Add32BitCharToVch();
				}
				// adjust limits
				FlushBuffer();
				m_ichLastInput = ichLimOfRun; // adjust remaining chars to copy.
			}
		}
		// Now make a TsString with just the piece that's left. Since it's already in order,
		// the property changes prevent any more changes. The piece that's left is from
		// m_ichLastInput (as adjusted) to m_ichInput
		ITsStrBldrPtr m_qtsbResultFrag;
		CheckHr(m_ptssThis->GetBldr(&m_qtsbResultFrag));
		if (m_ichInput < m_cchInput)
			CheckHr(m_qtsbResultFrag->ReplaceRgch(m_ichInput, m_cchInput, NULL, 0, NULL));
		if (m_ichLastInput > 0)
			CheckHr(m_qtsbResultFrag->ReplaceRgch(0, m_ichLastInput, NULL, 0, NULL));
		ITsStringPtr qtssFrag;
		CheckHr(m_qtsbResultFrag->GetString(&qtssFrag));
		int cchSoFar;
		CheckHr(m_qtsbResult->get_Length(&cchSoFar));
		CheckHr(m_qtsbResult->ReplaceTsString(cchSoFar, cchSoFar, qtssFrag));
	}

	// The main method that the real method calls to make everything happen.
	HRESULT Execute()
	{
		// If the string is already normalized, then just copy the text.
		if (m_ptssThis->IsKnownNormalized(m_nm))
		{
			m_ptssThis->AddRef();
			*m_pptssRet = m_ptssThis;
			return S_OK;
		}

		// If we want the "Styled Compressed" Form, then first get the fully
		// decompressed form to normalize the order of diacritics, then the
		// code below only has to worry about styled compression as a special
		// case.
		if (m_nm == knmNFSC)
		{
			if (!m_ptssThis->IsKnownNormalized(knmNFD))
			{
				ITsStringPtr qtssNFD;
				CheckHr(m_ptssThis->get_NormalizedForm(knmNFD, &qtssNFD));
				CheckHr(qtssNFD->get_NormalizedForm(knmNFSC, m_pptssRet));
				return S_OK;
			}
		}

		CheckHr(m_ptssThis->LockText(&m_prgchInput, &m_cchInput));
		// empty strings are always normalized, so we should not get this far.
		Assert(m_cchInput > 0);
		try
		{
			// The text props that should be applied to the characters in the buffer when they are
			// put into the string. If the buffer is empty, its value is meaningless and kept null.
			m_qtsbResult.CreateInstance(CLSID_TsStrBldr);
			m_cchBuf = 0; // characters in use in m_vch
			m_ichLimRun = 0; // force first char to start new run.
			m_iRun = -1;
			m_cchBufGood = 0;
			m_ichInput = 0;

			// 30% + 10 should mean it rarely needs to grow
			m_vch.Resize(m_cchInput * 13 / 10 + 10);

			const Normalizer2* norm = SilUtil::GetIcuNormalizer((UNormalizationMode)(m_nm == knmNFSC ? knmNFC : m_nm));
			int bufferPos = 0;
			UnicodeString buffer;
			UCharCharacterIterator iter(m_prgchInput, m_cchInput);
			for (;;)
			{
				// Fix any offsets not yet handled up to m_ichInput.
				while (m_iichNextOffsetToFix < m_cichOffsetsToFix &&
					*m_prgpichOffsetsToFix[m_iichNextOffsetToFix] <= m_ichInput)
				{
					// m_ichInput characters in the input string have produced the output
					// which is currently split between cchSoFar characters in the partly
					// built output string builder, and m_cchBuf characters in the partly
					// built next run. Adding these together gives the corresponding
					// position in the output.
					int cchSoFar;
					CheckHr(m_qtsbResult->get_Length(&cchSoFar));
					*m_prgpichOffsetsToFix[m_iichNextOffsetToFix] = m_cchBuf + cchSoFar;
					m_iichNextOffsetToFix++;
				}
				if (m_ichInput >= m_ichLimRun)
				{
					if (m_ichInput > m_ichLimRun && m_nm == knmNFSC)
					{
						HandleStyledComposition();
					}

					int cchReorder = m_cchBuf - m_cchBufGood;
					FlushBuffer();
					if (m_ichInput > m_ichLimRun && (m_nm == knmNFD || m_nm == knmNFKD))
					{
						HandleReordering(cchReorder);
					}
					if (m_ichInput == m_cchInput)
					{
						Assert(!iter.hasNext());
						break;
					}
					CheckHr(m_ptssThis->get_RunAt(m_ichInput, &m_iRun));
					CheckHr(m_ptssThis->get_LimOfRun(m_iRun, &m_ichLimRun));
					CheckHr(m_ptssThis->get_PropertiesAt(m_ichInput, &m_qttpBuf));
				}
				else if (m_ichInput > m_ichLastInput)
				{
					// We've handled one or more input characters completely; the ones thus put
					// in the buffer are sure to have properties m_qttpBuf.
					m_cchBufGood = m_cchBuf;
				}
				m_ichLastInput = m_ichInput; // Remember for next time.

				if (bufferPos == buffer.length())
				{
					m_ichInput = iter.getIndex();
					UnicodeString segment(iter.next32PostInc());
					while (iter.hasNext())
					{
						UChar32 c = iter.next32PostInc();
						if (norm->hasBoundaryBefore(c))
						{
							iter.move32(-1, CharacterIterator::kCurrent);
							break;
						}
						segment.append(c);
					}

					buffer.remove();
					bufferPos = 0;
					UErrorCode uerr = U_ZERO_ERROR;
					norm->normalize(segment, buffer, uerr);
				}

				m_ch = buffer.char32At(bufferPos);
				bufferPos += U16_LENGTH(m_ch);

				Add32BitCharToVch();

				if (bufferPos == buffer.length())
					m_ichInput = iter.getIndex();
			}
			// Add final run to new string.
			FlushBuffer();
		}
		catch(...)
		{
			CheckHr(m_ptssThis->UnlockText(m_prgchInput));
			throw;
		}
		CheckHr(m_ptssThis->UnlockText(m_prgchInput));

		CheckHr(m_qtsbResult->GetString(m_pptssRet));

		// Note that the returned string is normalized as requested (plus maybe some
		// subsidiary normalizations).
		TsStrSingle * pztss = dynamic_cast<TsStrSingle *>(*m_pptssRet);
		if (pztss)
		{
			pztss->NoteNormalized(m_nm);
		}
		else
		{
			TsStrMulti * pztsm = dynamic_cast<TsStrMulti *>(*m_pptssRet);
			AssertPtr(pztsm);
			pztsm->NoteNormalized(m_nm);
		}
		return S_OK;
	}
};

/*----------------------------------------------------------------------------------------------
	 Return an equivalent string in the specified normal form.
	 This may be the same object as the recipient, if it is already in
	 that normal form.
	 Note that TsStrings normalized to NFC may not have text
	 (the sequence of plain characters) that is so normalized.
	 This is because we don't collapse otherwise collapsible pairs if they
	 have different style properties.

	 Our basic approach is to make an ICU normalizer on the complete input text. As we retrieve
	 each character from the normalizer, we first get the index of what it considers to be
	 the corresponding input character. We arrange to give the output character the properties
	 of that input character.

	 It is almost impossible to keep track of the effects of NFC normalization in this
	 way in general, because a character may be expanded, its parts re-ordered, and some of
	 the parts may then be recombined with other original characters. To get around this,
	 we perform NFC in two steps: first normalize to NFD, then continue to NFC. This ensures
	 that the NFC normalization performs no expansions nor re-orderings. The only case we have
	 to worry about is consolidation of two or more adjacent characters in different runs.

	 We can detect consolidation by the fact that the input position jumps by more than one
	 position. When this occurs and the characters concerned are not all in the same run,
	 we preserve the original characters.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::get_NormalizedForm(
	FwNormalizationMode nm, ITsString ** pptssRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptssRet);
	TsNormalizeMethod<TxtBuf> tnm(nm, pptssRet, this);
	return tnm.Execute();
	END_COM_METHOD(g_fact, IID_ITsString);
}

/*----------------------------------------------------------------------------------------------
	 Return an equivalent string in NFD.
	 This may be the same object as the recipient, if it is already in
	 that normal form.

	 The values pointed to by the array of pointers to offsets to fix are each offsets into
	 the string. The code attempts to adjust them to corresponding offsets in the output
	 string. An exact correspondence is not always achieved; if the offset is in the middle
	 of a diacritic sequence, it may be moved to the start of the following base character
	 (or the end of the string).
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> STDMETHODIMP TsStrBase<TxtBuf>::NfdAndFixOffsets(
	ITsString ** pptssRet, int ** prgpichOffsetsToFix, int cichOffsetsToFix)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptssRet);
	TsNormalizeMethod<TxtBuf> tnm(knmNFD, pptssRet, this);
	tnm.SetOffsetsToFix(prgpichOffsetsToFix, cichOffsetsToFix);
	return tnm.Execute();
	END_COM_METHOD(g_fact, IID_ITsString);
}


/***********************************************************************************************
	Implementation of TsStrSingle. This derives from TsStrBase<TxtBufSingle>.
	This is a thread-safe, "agile" component.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Static method to create a new single run string given the text and optional properties.
----------------------------------------------------------------------------------------------*/
void TsStrSingle::Create(DataReader * pdrdr, int cch, ITsTextProps * pttp, TsStrSingle ** ppsts)
{
	AssertPtrN(pdrdr);
	Assert(!cch || pdrdr);
	AssertPtr(pttp);
	AssertPtr(ppsts);
	Assert(!*ppsts);

	ComSmartPtr<TsStrSingle> qsts;

	// Allocate the memory for the new object.
	qsts.Attach(NewObjExtra(GetExtraSize(cch)) TsStrSingle);

	qsts->m_run.m_qttp = pttp;
	qsts->m_run.m_ichLim = cch;

	int itip;

	// Copy the text.
	if (cch > 0)
	{
		pdrdr->ReadBuf(qsts->Prgch(), cch * isizeof(OLECHAR));

		// Ensure that this run has a writing system specified unless it is a newline. Rather than
		// checking for newlines rigorously, we just look to see if the last character of the run is greater than
		// 13 since CR and LF are 10 and 13.
		if (!((TsTextProps *)pttp)->FindIntProp(ktptWs, &itip) && qsts->Prgch()[qsts->m_run.m_ichLim - 1] > 13)
		{
			qsts.Clear();
			qsts = NULL;
			ThrowInternalError(E_UNEXPECTED, "Writing system is required for every run in a TsString (except for newlines)");
		}
	}
	else
	{
		if (!((TsTextProps *)pttp)->FindIntProp(ktptWs, &itip))
		{
			qsts.Clear();
			qsts = NULL;
			ThrowInternalError(E_UNEXPECTED, "Writing system is required even for an empty TsString");
		}

		// FWR-2366: Remove any object data props for this empty string. They cause problems when
		// saving and then subequently restoring this string from XML because they can be referring
		// to an object which has been deleted. Object data is only valid for non-empty runs.
		int itsp;
		if (((TsTextProps *)pttp)->FindStrProp(ktptObjData, &itsp))
		{
			ITsPropsBldrPtr qpropsBldr;
			CheckHr(pttp->GetBldr(&qpropsBldr));
			CheckHr(qpropsBldr->SetStrPropValue(ktptObjData, NULL));
			CheckHr(qpropsBldr->GetTextProps(&pttp));
			qsts->m_run.m_qttp = pttp;
		}
		// empty string is known to be fully normalized any way you look at it.
		byte nFlags = qsts->GetFlagByte();
		nFlags |= 1<<knmNFD | 1<<knmNFKD | 1<<knmNFC | 1<<knmNFKC | 1<<knmFCD |
			1<<knmNFSC;
		qsts->SetFlagByte(nFlags);
	}
	*ppsts = qsts.Detach();
}

// Optimized creation from an array of characters, with non-empty pttp.
void TsStrSingle::Create(const OLECHAR * prgch, int cch, TsTextProps * pttp, ITsString ** ppsts)
{
	Assert(!cch || prgch);
	AssertPtr(pttp);
	AssertPtr(ppsts);
	Assert(!*ppsts);

	int itip;
	if (!((TsTextProps *)pttp)->FindIntProp(ktptWs, &itip))
		ThrowInternalError(E_UNEXPECTED, "Writing system is required for every run in a TsString");

	ComSmartPtr<TsStrSingle> qsts;

	// Allocate the memory for the new object.
	qsts.Attach(NewObjExtra(GetExtraSize(cch)) TsStrSingle);
	qsts->m_run.m_qttp = pttp;
	qsts->m_run.m_ichLim = cch;

	// Copy the text.
	if (cch > 0)
	{
		CopyBytes(prgch, qsts->Prgch(), cch * isizeof(OLECHAR));
	}
	else
	{
		// empty string is known to be fully normalized any way you look at it.
		byte nFlags = qsts->GetFlagByte();
		nFlags |= 1<<knmNFD | 1<<knmNFKD | 1<<knmNFC | 1<<knmNFKC | 1<<knmFCD |
			1<<knmNFSC;
		qsts->SetFlagByte(nFlags);
	}
	*ppsts = qsts.Detach();
}

/*----------------------------------------------------------------------------------------------
	Static method to create a new single run string given the text and properties.
----------------------------------------------------------------------------------------------*/
void TsStrSingle::Create(DataReader * pdrdr, int cch, ITsTextProps * pttp, ITsString ** pptss)
{
	AssertPtrN(pdrdr);
	Assert(!cch || pdrdr);
	AssertPtrN(pttp);
	AssertPtrN(pptss);
	Assert(!*pptss);

	ComSmartPtr<TsStrSingle> qsts;

	Create(pdrdr, cch, pttp, &qsts);
	CheckHr(qsts->QueryInterface(IID_ITsString, (void **)pptss));
}


/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrSingle::QueryInterface(REFIID iid, void ** ppv)
{
	AssertObj(this);
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ITsString)
		*ppv = static_cast<ITsString *>(this);
	else if (iid == IID_ITsStringRaw)
		*ppv = static_cast<ITsStringRaw *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ITsString);
//		*ppv = NewObj CSupportErrorInfo(this, IID_ITsStringRaw);
		return S_OK;
	}
#if WIN32
	else if (iid == IID_IMarshal)
		return m_qunkMarshaler->QueryInterface(iid, ppv);
#endif
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/***********************************************************************************************
	Implementation of TsStrMulti. This derives from TsStrBase<TxtBufMulti>.
	This is a thread-safe, "agile" component.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Static method to create a new multi run string given the text and run information.
----------------------------------------------------------------------------------------------*/
void TsStrMulti::Create(DataReader * pdrdr, const TxtRun * prgrun, int crun,
	TsStrMulti ** ppstm)
{
	AssertPtr(pdrdr);
	Assert(crun >= 2);
	AssertArray(prgrun, crun);
	AssertPtr(ppstm);
	Assert(!*ppstm);

	ComSmartPtr<TsStrMulti> qstm;
	int cch = prgrun[crun - 1].m_ichLim;
	Assert(cch >= crun);

	// Allocate the memory for the new object.
	qstm.Attach(NewObjExtra(GetExtraSize(cch, crun)) TsStrMulti);

	// Warning: These must be set before calling Prgch(), Prun(), etc.
	qstm->m_crun = crun;
	qstm->m_cch = cch;

	// Copy the characters.
	pdrdr->ReadBuf(qstm->Prgch(), cch * isizeof(OLECHAR));

	TxtRun * prgrunDst = qstm->Prun(0);

	// Copy the run information.
	for (int irun = crun; --irun >= 0; )
	{
		AssertPtr(prgrun[irun].m_qttp);
		Assert(prgrun[irun].m_ichLim > 0);
		Assert(irun == crun - 1 || prgrun[irun].m_ichLim < prgrun[irun + 1].m_ichLim);
		Assert(irun == crun - 1 || !prgrun[irun].PropsEqual(prgrun[irun + 1]));

		// Ensure that this run has a writing system specified unless it is a newline. Rather than
		// checking for newlines rigorously, we just look to see if the last character of the run is greater than
		// 13 since CR and LF are 10 and 13.
		int itip;
		if (!((TsTextProps *)prgrun[irun].m_qttp.Ptr())->FindIntProp(ktptWs, &itip) &&
			qstm->Prgch()[prgrun[irun].m_ichLim - 1] > 13)
		{
			qstm.Clear();
			qstm = NULL;
			ThrowInternalError(E_UNEXPECTED, "Writing system is required for every run in a TsString (except for newlines)");
		}

		prgrunDst[irun] = prgrun[irun];
	}

	*ppstm = qstm.Detach();
}


/*----------------------------------------------------------------------------------------------
	Static method to create a new multi run string given the text and run information.
----------------------------------------------------------------------------------------------*/
void TsStrMulti::Create(DataReader * pdrdr, const TxtRun * prgrun, int crun, ITsString ** pptss)
{
	AssertPtr(pdrdr);
	AssertPtr(pptss);
	Assert(!*pptss);

	ComSmartPtr<TsStrMulti> qstm;

	Create(pdrdr, prgrun, crun, &qstm);
	CheckHr(qstm->QueryInterface(IID_ITsString, (void **)pptss));
}


/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrMulti::QueryInterface(REFIID iid, void ** ppv)
{
	AssertObj(this);
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ITsString)
		*ppv = static_cast<ITsString *>(this);
	else if (iid == IID_ITsStringRaw)
		*ppv = static_cast<ITsStringRaw *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ITsString);
//		*ppv = NewObj CSupportErrorInfo(this, IID_ITsStringRaw);
		return S_OK;
	}
#if WIN32
	else if (iid == IID_IMarshal)
		return m_qunkMarshaler->QueryInterface(iid, ppv);
#endif
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/***********************************************************************************************
	Implementation of TsStrBldr. This derives from TsStrBase<TxtBufBldr> and implements
	ITsStrBldr.
	This is a "Both" threading model component that is NOT thread-safe.
***********************************************************************************************/


// The class factory for TsStrBldr.
static GenericFactory g_factStrBldr(
	_T("FieldWorks.TsStrBldr"),
	&CLSID_TsStrBldr,
	_T("FieldWorks String Builder"),
	_T("Both"),
	&TsStrBldr::CreateCom);


/*----------------------------------------------------------------------------------------------
	Static method called by the class factory to create a string builder.
----------------------------------------------------------------------------------------------*/
void TsStrBldr::CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkOuter)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<TsStrBldr> qztsb;

	qztsb.Attach(NewObj TsStrBldr);
	qztsb->Init(NULL, 0, NULL, 0);
	CheckHr(qztsb->QueryInterface(iid, ppv));
}


/*----------------------------------------------------------------------------------------------
	Static method to create a new string builder given the text and run information.
----------------------------------------------------------------------------------------------*/
void TsStrBldr::Create(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun,
	TsStrBldr ** ppztsb)
{
	AssertArray(prgch, cch);
	AssertArray(prgrun, crun);
	AssertPtr(ppztsb);
	Assert(!*ppztsb);

	ComSmartPtr<TsStrBldr> qztsb;

	qztsb.Attach(NewObj TsStrBldr);
	qztsb->Init(prgch, cch, prgrun, crun);
	*ppztsb = qztsb.Detach();
}


/*----------------------------------------------------------------------------------------------
	Static method to create a new string builder given the text and run information.
----------------------------------------------------------------------------------------------*/
void TsStrBldr::Create(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun,
	ITsStrBldr ** pptsb)
{
	AssertPtr(pptsb);
	Assert(!*pptsb);

	ComSmartPtr<TsStrBldr> qztsb;

	qztsb.Attach(NewObj TsStrBldr);
	qztsb->Init(prgch, cch, prgrun, crun);
	CheckHr(qztsb->QueryInterface(IID_ITsStrBldr, (void **)pptsb));
}


/*----------------------------------------------------------------------------------------------
	Init function.
----------------------------------------------------------------------------------------------*/
void TsStrBldr::Init(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun)
{
	AssertArray(prgch, cch);
	AssertArray(prgrun, crun);
	Assert(cch || crun <= 1);
	Assert(!cch || cch >= crun && crun >= 1);
	Assert(!crun || prgrun[crun - 1].m_ichLim == cch);
	Assert(m_vrun.Size() == 0);
	Assert(m_stu.Length() == 0);

	if (!crun)
	{
		TxtRun run;

		TsTextProps::Create(NULL, 0, NULL, 0, &run.m_qttp);
		run.m_ichLim = cch;
		m_vrun.Push(run);
	}
	else
		m_vrun.Replace(0, 0, prgrun, crun);

	m_stu.Assign(prgch, cch);

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::QueryInterface(REFIID iid, void ** ppv)
{
	AssertObj(this);
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ITsStrBldr)
		*ppv = static_cast<ITsStrBldr *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ITsStrBldr);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Get Builder Class Id.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::GetBldrClsid(CLSID * pclsid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pclsid);
	AssertObj(this);

	*pclsid = CLSID_TsStrBldr;

	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Clear function.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::Clear()
{
	BEGIN_COM_METHOD;

	// Clear the run vector.
	m_vrun.Clear();
	// Clear the text string.
	m_stu.Clear();
	// Re-initialize the object with a single (empty) run.
	Init(NULL, 0, NULL, 0);

	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Replace a range with new text and the given properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::Replace(int ichMin, int ichLim, BSTR bstrIns,
	ITsTextProps * pttp)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrIns);
	ChkComArgPtrN(pttp);

	return ReplaceRgch(ichMin, ichLim, bstrIns, BstrLen(bstrIns), pttp);

	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Replace a range with an ITsString.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::ReplaceTsString(int ichMin, int ichLim, ITsString * ptssIns)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptssIns);
	if ((uint)ichMin > (uint)ichLim || (uint)ichLim > (uint)Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	if (!ptssIns)
	{
		if (ichMin == ichLim)
			return S_OK;
		ReplaceCore(ichMin, ichLim, NULL, 0, NULL, 0);
		return S_OK;
	}

	// If the ITsString passed in is one of our internal strings, get the data directly.
	ITsStringRawPtr qtssr;
	HRESULT hr = ptssIns->QueryInterface(IID_ITsStringRaw, (void **)&qtssr);
	if (SUCCEEDED(hr))
	{
		const OLECHAR * prgch;
		const TxtRun * prgrun;
		int cch, crun;
		CheckHr(qtssr->GetRawPtrs(&prgch, &cch, &prgrun, &crun));

		AssertArray(prgch, cch);
		AssertArray(prgrun, crun);
		// If there's nothing to insert or delete and we aren't empty ourselves, nothing
		// to do. (If we are empty ourselves, we will replace our properties with those
		// of the replacement.)
		if (!cch && ichMin == ichLim && Cch())
			return S_OK;
		ReplaceCore(ichMin, ichLim, prgch, cch, prgrun, crun);
		AssertObj(this);
		return S_OK;
	}

	// Otherwise, go after the data through the interface routines.
	// The most likely reason for this is that we are copying from another application.
	// We need to use only methods that are marshallable.
	int cch;
	int crun;

	CheckHr(ptssIns->get_Length(&cch));

	// If there's nothing to insert or delete and we aren't empty ourselves, nothing
	// to do. (If we are empty ourselves, we will replace our properties with those
	// of the replacement.)
	if (!cch && ichMin == ichLim && Cch())
		return S_OK;

	CheckHr(ptssIns->get_RunCount(&crun));

	Vector<TxtRun> vrun;
	TsRunInfo tri;
	int iv;

	// Get the runs.
	vrun.Resize(crun);

	for (iv = 0; iv < crun; iv++)
	{
		// The TsTextProps may be implemented by another application, and we can't
		// count on it not closing down before we do. Also we expect to be able to
		// do identify comparisions on TsTextProps. So make an equivalent native one.
		// ENHANCE (JohnT): this block might be a useful method on TsTextProps.
		ITsTextPropsPtr qttpForeign;
		ITsPropsBldrPtr qtpb;
		qtpb.CreateInstance(CLSID_TsPropsBldr);
		CheckHr(ptssIns->FetchRunInfo(iv, &tri, &qttpForeign));
		int cpropInt, cpropString;
		CheckHr(qttpForeign->get_IntPropCount(&cpropInt));
		CheckHr(qttpForeign->get_StrPropCount(&cpropString));
		for (int iprop1 = 0; iprop1 < cpropInt; iprop1++)
		{
			int tpt, nVal, nVar;
			CheckHr(qttpForeign->GetIntProp(iprop1, &tpt, &nVar, &nVal));
			CheckHr(qtpb->SetIntPropValues(tpt, nVar, nVal));
		}
		for (int iprop2 = 0; iprop2 < cpropString; iprop2++)
		{
			int tpt;
			SmartBstr sbstr;
			CheckHr(qttpForeign->GetStrProp(iprop2, &tpt, &sbstr));
			CheckHr(qtpb->SetStrPropValue(tpt, sbstr));
		}

		CheckHr(qtpb->GetTextProps(&vrun[iv].m_qttp));
		vrun[iv].m_ichLim = tri.ichLim;
	}

	// Get the text. Do NOT use LockText, it can't be marshalled.
	SmartBstr sbstr;

	CheckHr(ptssIns->get_Text(&sbstr));

	ReplaceCore(ichMin, ichLim, sbstr.Chars(), cch, vrun.Begin(), crun);

	AssertObj(this);

	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Replace a range with new text and optional properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::ReplaceRgch(int ichMin, int ichLim, const OLECHAR * prgchIns,
									int cchIns, ITsTextProps * pttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchIns, cchIns);
	ChkComArgPtrN(pttp);
	if ((uint)ichMin > (uint)ichLim || (uint)ichLim > (uint)Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	// Don't do anything if there is nothing to insert or delete and we are not empty.
	if (!cchIns && ichMin == ichLim && Cch())
		return S_OK;

	TxtRun run;

	if (pttp)
		run.m_qttp = pttp;
	else
	{
		// If ichMin equals ichLim, use the previous characters properties.
		if (ichMin == ichLim && ichMin > 0)
			run.m_qttp = Prun(IrunAt(ichMin - 1))->m_qttp;
		else
			run.m_qttp = Prun(IrunAt(ichMin))->m_qttp;
	}
	run.m_ichLim = cchIns;

	ReplaceCore(ichMin, ichLim, prgchIns, cchIns, &run, 1);

	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Replace a range.
----------------------------------------------------------------------------------------------*/
void TsStrBldr::ReplaceCore(int ichMin, int ichLim, const OLECHAR * prgchIns, int cchIns,
	const TxtRun * prgrunIns, int crunIns)
{
	AssertObj(this);
	Assert((uint)ichMin <= (uint)ichLim && (uint)ichLim <= (uint)Cch());
	AssertArray(prgchIns, cchIns);
	AssertArray(prgrunIns, crunIns);
	// JohnT: allow one run to be passed when doing a "replace" with nothing inserted.
	// This allows things like copying a string one run at a time to successfully copy the
	// formatting of empty strings. The one run is ignored unless the resulting string is empty.
	Assert(crunIns <= cchIns || cchIns == 0 && crunIns == 1);
	Assert(cchIns == 0 || crunIns > 0);
	Assert(crunIns == 0 || prgrunIns[crunIns - 1].m_ichLim == cchIns);

	// This is the only case where we can end up with an empty run.
	if (cchIns == 0)
	{
		// If we're deleting everything and we were passed a run, use it.
		if (ichMin == 0 && ichLim == Cch())
		{
			m_stu.Clear();
			if (m_vrun.Size() > 1)
				m_vrun.Resize(1);
			m_vrun[0].m_ichLim = 0;
			// If we were given some run properties, let them become those of the empty string.
			if (crunIns)
				m_vrun[0].m_qttp = prgrunIns->m_qttp;

			AssertObj(this);
			return;
		}
		else
			// We're not deleting everything, therefore any run we were passed describes
			// no characters and should be ignored.
			crunIns = 0;
	}


	StrUni stu;
	int irunMin;
	int irunLim;
	int dich;
	int iv;

	// Work on a copy so if it fails (or something below fails) we haven't messed up our
	// current state.
	stu = m_stu;
	stu.Replace(ichMin, ichLim, prgchIns, cchIns);

	irunMin = IrunAt(ichMin);
	irunLim = IrunAt(ichLim);

	if (crunIns > 0 && crunIns >= irunLim - irunMin)
		m_vrun.EnsureSpace(crunIns - irunLim + irunMin + 1);

	// dich is the amount that indices >= ichLim should be adjusted by after the replace.
	dich = cchIns - ichLim + ichMin;

	// OPTIMIZE: Should we optimize this? Yes if a profiler shows significant time spent with this
	// case but not otherwise [JohnT]. In the common case we split the run insert into a run
	// with the same properties then nuke 2 of the 3 runs.

	// Ensure ichMin is on a run boundary.
	if (ichMin > IchMinRun(irunMin))
	{
		// Insertion is within a single run.
		if (irunMin == irunLim)
		{
			// Split the run.
			m_vrun.Replace(irunMin, irunMin, NULL, 1);
			++irunLim;
			m_vrun[irunMin].m_qttp = m_vrun[irunLim].m_qttp;
		}
		// Adjust the boundary, even when not splitting.
		m_vrun[irunMin].m_ichLim = ichMin;
		irunMin++;
	}

	m_vrun.Replace(irunMin, irunLim, prgrunIns, crunIns);
	irunLim = irunMin + crunIns;
	if (ichMin > 0)
	{
		for (iv = irunMin; iv < irunLim; iv++)
			m_vrun[iv].m_ichLim += ichMin;
	}
	if (dich)
	{
		for (iv = irunLim; iv < m_vrun.Size(); iv++)
			m_vrun[iv].m_ichLim += dich;
	}

	// See if we can combine on the left.
	if (irunMin > 0)
	{
		Assert(CchRun(irunMin - 1) > 0);
		if (m_vrun[irunMin].PropsEqual(m_vrun[irunMin - 1]))
		{
			m_vrun.Delete(irunMin - 1);
			irunLim--;
		}
	}

	// See if we can combine on the right.
	if (irunLim > 0)
	{
		// Empty right run, delete.
		Assert(CchRun(irunLim - 1) > 0);
		if (m_vrun[irunLim].m_ichLim == m_vrun[irunLim - 1].m_ichLim)
		{
			Assert(irunLim == m_vrun.Size() - 1);
			m_vrun.Delete(irunLim);
		}
		else if (m_vrun[irunLim - 1].PropsEqual(m_vrun[irunLim]))
		{
			m_vrun.Delete(irunLim - 1);
			// irunLim is no longer valid after the above delete.
		}
	}

	m_stu = stu;

	AssertObj(this);
}


#ifdef DEBUG
/*----------------------------------------------------------------------------------------------
	Output graphical text representation of the runs in the bldr object to debug window.
----------------------------------------------------------------------------------------------*/
void TsStrBldr::DebugDumpRunInfo(bool fShowEncodingStack)
{
	StrApp strOut;
	StrApp strT;
	int ichMin;
	TxtRun * prun = m_vrun.Begin();
	TxtRun * prunLim = m_vrun.End();

	ichMin = 0;
	for ( ; prun < prunLim; prun++)
	{
		int cch = prun->IchLim() - ichMin;
		if (cch > 10)
			strT.Format(_T("--<%d>--"), cch - 4);
		else
			strT.Format(_T("%*c"), cch, '-');

		strOut.Append(strT);
		strT.Format(_T("|%d"), prun->IchLim());

		strOut.Append(strT);
		ichMin += cch;
		if (fShowEncodingStack)
		{
			int ws;
			int nVar;
			HRESULT hr = prun->m_qttp->GetIntPropValues(ktptWs, &nVar, &ws);
			int wsBase;
			HRESULT hrBase = prun->m_qttp->GetIntPropValues(ktptBaseWs, &nVar,
				&wsBase);
			if (hr == S_OK)
				strT.Format(_T("<%d>"), ws);
			else
				strT.Clear();
			if (hrBase == S_OK)
				strT.FormatAppend(_T("<[%d]>"), wsBase);
			if (strT.Length())
			{
				strOut.Append("(");
				strOut.Append(strT);
				strOut.Append(")");
			}
		}
	}
	OutputDebugString(strOut.Chars());
	OutputDebugString(_T("\n"));
}
#endif // DEBUG


/*----------------------------------------------------------------------------------------------
	Makes sure that there are run boundaries at ichMin and ichLim.
----------------------------------------------------------------------------------------------*/
void TsStrBldr::EnsureRuns(int ichMin, int ichLim, int * pirunMin, int * pirunLim)
{
	AssertObj(this);
	Assert((uint)ichMin < (uint)ichLim && (uint)ichLim <= (uint)Cch());
	AssertPtr(pirunMin);
	AssertPtr(pirunLim);

	int irunMin;
	int irunLast;
	TxtRun runT;

	// Make sure we have enough room to insert 2 new runs.
	m_vrun.EnsureSpace(2);

	irunMin = IrunAt(ichMin);
	if (IchMinRun(irunMin) < ichMin)
	{
		runT = *Prun(irunMin);
		runT.m_ichLim = ichMin;
		m_vrun.Insert(irunMin, runT);
		irunMin++;
	}
	Assert(ichMin == IchMinRun(irunMin));
	*pirunMin = irunMin;

	irunLast = IrunAt(ichLim - 1);
	if (ichLim < IchLimRun(irunLast))
	{
		runT = *Prun(irunLast);
		runT.m_ichLim = ichLim;
		m_vrun.Insert(irunLast, runT);
	}
	Assert(ichLim == IchLimRun(irunLast));
	*pirunLim = irunLast + 1;
}


/*----------------------------------------------------------------------------------------------
	Replace the existing properties on the range of text with pttp.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::SetProperties(int ichMin, int ichLim, ITsTextProps * pttp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pttp);
	if ((uint)ichMin > (uint)ichLim || (uint)ichLim > (uint)Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	// Handle this simple case here.
	if (ichMin == ichLim)
	{
		if (Cch() > 0)
			return S_OK;
		Assert(m_vrun.Size() == 1);
		m_vrun[0].m_qttp = pttp;

		AssertObj(this);
		return S_OK;
	}

	int irunT;
	int irunMin;
	int irunLim;

	EnsureRuns(ichMin, ichLim, &irunMin, &irunLim);

	// Set the properties and merge duplicate runs.
	for (irunT = irunLim; --irunT >= irunMin; )
	{
		TxtRun * prun = &m_vrun[irunT];
		prun->m_qttp = pttp;
		if (irunT < m_vrun.Size() - 1 && prun->PropsEqual(prun[1]))
			m_vrun.Delete(irunT);
	}
	Assert(irunMin < m_vrun.Size());
	if (irunMin > 0 && m_vrun[irunMin].PropsEqual(m_vrun[irunMin - 1]))
		m_vrun.Delete(irunMin - 1);

	AssertObj(this);

	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Set the integer property values as given. Note that if nVar is -1 and nVal is -1, the
	property will be deleted.
----------------------------------------------------------------------------------------------*/
void TsStrBldr::EditIntProp(ITsTextProps * pttp, int ttpt, int nVar, int nVal,
	ITsTextProps ** ppttp)
{
	AssertPtr(pttp);
	AssertPtr(ppttp);
	Assert(!*ppttp);

	ITsPropsBldrPtr qtpb;

	CheckHr(pttp->GetBldr(&qtpb));
	CheckHr(qtpb->SetIntPropValues(ttpt, nVar, nVal));
	CheckHr(qtpb->GetTextProps(ppttp));
}


/*----------------------------------------------------------------------------------------------
	Set the integer property values on the range.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::SetIntPropValues(int ichMin, int ichLim, int ttpt, int nVar, int nVal)
{
	BEGIN_COM_METHOD;
	if ((uint)ichMin > (uint)ichLim || (uint)ichLim > (uint)Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
//-	if (ktptWs == ttpt && (uint)nVal > kwsLim)
//-		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");
	AssertObj(this);


	// Handle this simple case here.
	if (ichMin == ichLim)
	{
		if (Cch() > 0)
			return S_OK;
		Assert(m_vrun.Size() == 1);

		ITsTextPropsPtr qttp;

		EditIntProp(m_vrun[0].m_qttp, ttpt, nVar, nVal, &qttp);

		m_vrun[0].m_qttp = qttp;
		return S_OK;
	}

	int irunT;
	int irunMin;
	int irunLim;

	EnsureRuns(ichMin, ichLim, &irunMin, &irunLim);

	ComVector<ITsTextProps> vqttp;

	vqttp.Resize(irunLim - irunMin);

	// Edit the TsTextProps.
	for (irunT = irunLim; --irunT >= irunMin; )
	{
		EditIntProp(m_vrun[irunT].m_qttp, ttpt, nVar, nVal, &vqttp[irunT - irunMin]);
	}

	// Set the properties and merge duplicate runs.
	for (irunT = irunLim; --irunT >= irunMin; )
	{
		TxtRun * prun = &m_vrun[irunT];
		prun->m_qttp = vqttp[irunT - irunMin];
		if (irunT < m_vrun.Size() - 1 && prun->PropsEqual(prun[1]))
			m_vrun.Delete(irunT);
	}
	Assert(irunMin < m_vrun.Size());
	if (irunMin > 0 && m_vrun[irunMin].PropsEqual(m_vrun[irunMin - 1]))
		m_vrun.Delete(irunMin - 1);

	AssertObj(this);

	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Set the string property values as given. Note that if bstrVal is empty, the property will
	be deleted.
----------------------------------------------------------------------------------------------*/
void TsStrBldr::EditStrProp(ITsTextProps * pttp, int ttpt, BSTR bstrVal, ITsTextProps ** ppttp)
{
	AssertPtr(pttp);
	AssertBstrN(bstrVal);
	AssertPtr(ppttp);
	Assert(!*ppttp);

	ITsPropsBldrPtr qtpb;

	CheckHr(pttp->GetBldr(&qtpb));
	CheckHr(qtpb->SetStrPropValue(ttpt, bstrVal));
	CheckHr(qtpb->GetTextProps(ppttp));
}


/*----------------------------------------------------------------------------------------------
	Set the string property values on the range.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::SetStrPropValue(int ichMin, int ichLim, int ttpt, BSTR bstrVal)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrVal);
	if ((uint)ichMin > (uint)ichLim || (uint)ichLim > (uint)Cch())
		ThrowHr(WarnHr(E_INVALIDARG));
	AssertObj(this);

	// Handle this simple case here.
	if (ichMin == ichLim)
	{
		if (Cch() > 0)
			return S_OK;
		Assert(m_vrun.Size() == 1);

		ITsTextPropsPtr qttp;

		EditStrProp(m_vrun[0].m_qttp, ttpt, bstrVal, &qttp);
		m_vrun[0].m_qttp = qttp;
		return S_OK;
	}

	int irunT;
	int irunMin;
	int irunLim;

	EnsureRuns(ichMin, ichLim, &irunMin, &irunLim);

	ComVector<ITsTextProps> vqttp;

	vqttp.Resize(irunLim - irunMin);

	// Edit the TsTextProps.
	for (irunT = irunLim; --irunT >= irunMin; )
	{
		EditStrProp(m_vrun[irunT].m_qttp, ttpt, bstrVal, &vqttp[irunT - irunMin]);
	}

	// Set the properties and merge duplicate runs.
	for (irunT = irunLim; --irunT >= irunMin; )
	{
		TxtRun * prun = &m_vrun[irunT];
		prun->m_qttp = vqttp[irunT - irunMin];
		if (irunT < m_vrun.Size() - 1 && prun->PropsEqual(prun[1]))
			m_vrun.Delete(irunT);
	}
	Assert(irunMin < m_vrun.Size());
	if (irunMin > 0 && m_vrun[irunMin].PropsEqual(m_vrun[irunMin - 1]))
		m_vrun.Delete(irunMin - 1);

	AssertObj(this);
	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Create a string from the current state of the builder.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::GetString(ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	AssertObj(this);

	int crun = Crun();
	int cch = Cch();
	Assert(crun > 0);
	Assert(cch >= 0);

	DataReaderRgb drr(Prgch(), cch * isizeof(OLECHAR));

	if (crun == 1)
	{
		TsStrSingle::Create(&drr, cch, Prun(0)->m_qttp, pptss);
	}
	else
	{
		TsStrMulti::Create(&drr, Prun(0), crun, pptss);
		Assert(drr.IbCur() == cch * isizeof(OLECHAR));
	}

	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Serialize the formatting information of the string to the given stream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::SerializeFmt(IStream * pstrm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);
	AssertObj(this);

	DataWriterStrm dws(pstrm);
	SerializeFmtCore(&dws);

	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Serialize the formatting information of the string to the given byte array.  If the cbMax
	is too small this sets *pcb to the required size and returns S_FALSE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsStrBldr::SerializeFmtRgb(byte * prgb, int cbMax, int * pcb)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgb, cbMax);
	ChkComOutPtr(pcb);
	AssertObj(this);

	DataWriterRgb dwr(prgb, cbMax, true /*fIgnoreError*/);

	SerializeFmtCore(&dwr);
	Assert(dwr.IbMax() == dwr.IbCur());
	*pcb = dwr.IbMax();

	return *pcb <= cbMax ? S_OK : S_FALSE;

	END_COM_METHOD(g_factStrBldr, IID_ITsStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Serialize the formatting information of the IncBldr to the given DataWriter.
----------------------------------------------------------------------------------------------*/
void TsStrBldr::SerializeFmtCore(DataWriter * pdwrt)
{
	AssertObj(this);
	AssertPtr(pdwrt);

	// Write the number of runs.
	int crun = m_vrun.Size();
	pdwrt->WriteInt(crun);
	if (!crun)
		return;

	// Convert the text properties into a byte array, and save the byte index for each run's
	// text property.  Also write the ichMin and text property byte index for each run.
	Vector<byte> vbProp;		// Temporary space for property regions.
	vbProp.Resize(16 * crun);
	Vector<int> vibProp;
	vibProp.Resize(crun);
	int irun;
	int irunPrev;
	bool fRepeat;
	int ibNext = 0;
	int cbAvail;
	int cbProp;
	ITsTextPropsPtr qttp;
	int ichMin = 0;
	for (irun = 0; irun < crun; ++irun)
	{
		// Check whether this run's properties are a duplicate of a previous run.
		fRepeat = false;
		qttp = m_vrun[irun].m_qttp;
		for (irunPrev = 0; irunPrev < irun; ++irunPrev)
		{
			if (qttp == m_vrun[irunPrev].m_qttp)
			{
				vibProp[irun] = vibProp[irunPrev];
				fRepeat = true;
				break;
			}
		}
		if (!fRepeat)
		{
			// Serialize this run's properties to the temporary buffer.
			cbAvail = vbProp.Size() - ibNext;
			qttp->SerializeRgb(vbProp.Begin() + ibNext, cbAvail, &cbProp);
			if (cbProp > cbAvail)
			{
				vbProp.Resize(vbProp.Size() + cbProp * (crun - irun));
				cbAvail = vbProp.Size() - ibNext;
				qttp->SerializeRgb(vbProp.Begin() + ibNext, cbAvail, &cbProp);
			}
			vibProp[irun] = ibNext;
			ibNext += cbProp;
		}
		// Write the ichMin and ibProp values.
		pdwrt->WriteInt(ichMin);
		pdwrt->WriteInt(vibProp[irun]);
		// Get the ichMin for the next run.
		ichMin = m_vrun[irun].IchLim();
	}

	// Write the collected property information.
	pdwrt->WriteBuf(vbProp.Begin(), ibNext);
}


/***********************************************************************************************
	Incremental string builder.
	This is a "Both" threading model component that is NOT thread-safe.
***********************************************************************************************/


// The class factory for TsIncStrBldr.
static GenericFactory g_factIncStrBldr(
	_T("FieldWorks.TsIncStrBldr"),
	&CLSID_TsIncStrBldr,
	_T("FieldWorks Incremental String Builder"),
	_T("Both"),
	&TsIncStrBldr::CreateCom);


/*----------------------------------------------------------------------------------------------
	Static method called by the class factory to create an incremental string builder.
----------------------------------------------------------------------------------------------*/
void TsIncStrBldr::CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkOuter)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<TsIncStrBldr> qztisb;

	Create(NULL, 0, NULL, 0, &qztisb);
	CheckHr(qztisb->QueryInterface(iid, ppv));
}


/*----------------------------------------------------------------------------------------------
	Static method to create a new incremental string builder given the text, run and
	object information.
----------------------------------------------------------------------------------------------*/
void TsIncStrBldr::Create(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun,
	TsIncStrBldr ** ppztisb)
{
	AssertArray(prgch, cch);
	AssertArray(prgrun, crun);
	AssertPtr(ppztisb);
	Assert(!*ppztisb);

	ComSmartPtr<TsIncStrBldr> qztisb;

	qztisb.Attach(NewObj TsIncStrBldr);
	qztisb->Init(prgch, cch, prgrun, crun);
	*ppztisb = qztisb.Detach();
}


/*----------------------------------------------------------------------------------------------
	Static method to create a new incremental string builder given the text, run and
	object information.
----------------------------------------------------------------------------------------------*/
void TsIncStrBldr::Create(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun,
	ITsIncStrBldr ** pptisb)
{
	AssertArray(prgch, cch);
	AssertArray(prgrun, crun);
	AssertPtr(pptisb);
	Assert(!*pptisb);

	ComSmartPtr<TsIncStrBldr> qztisb;

	qztisb.Attach(NewObj TsIncStrBldr);
	qztisb->Init(prgch, cch, prgrun, crun);
	CheckHr(qztisb->QueryInterface(IID_ITsIncStrBldr, (void **)pptisb));
}


/*----------------------------------------------------------------------------------------------
	Init function.
----------------------------------------------------------------------------------------------*/
void TsIncStrBldr::Init(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun)
{
	AssertArray(prgch, cch);
	AssertArray(prgrun, crun);
	Assert(crun || !cch);
	Assert(!crun || prgrun[crun - 1].m_ichLim == cch);

	// Checking cch below is not a typo. An Incremental Str builder should
	// never contain empty runs.
	if (cch)
		m_vrun.Replace(0, 0, prgrun, crun);

	m_stu.Assign(prgch, cch);

	// Create the props builder.
	if (!crun)
		TsPropsBldr::Create(NULL, 0, NULL, 0, &m_qtpb);
	else
		CheckHr(prgrun[crun - 1].m_qttp->GetBldr(&m_qtpb));

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TsIncStrBldr::TsIncStrBldr(void)
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TsIncStrBldr::~TsIncStrBldr(void)
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ITsIncStrBldr)
		*ppv = static_cast<ITsIncStrBldr *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ITsIncStrBldr);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	AddRef.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) TsIncStrBldr::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) TsIncStrBldr::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Get IncBuilder Class Id.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::GetIncBldrClsid(CLSID * pclsid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pclsid);
	AssertObj(this);

	*pclsid = CLSID_TsIncStrBldr;

	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Clear function.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::Clear()
{
	BEGIN_COM_METHOD;

	// Clear the run vector.
	m_vrun.Clear();
	// Clear the text string.
	m_stu.Clear();

	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Get the text as a BSTR.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::get_Text(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);
	AssertObj(this);

	if (!m_stu.Length())
		return S_FALSE;
	m_stu.GetBstr(pbstr);

	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Append the given characters with the current properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::Append(BSTR bstrIns)
{
	return AppendRgch(bstrIns, BstrLen(bstrIns));
}


/*----------------------------------------------------------------------------------------------
	Append the given string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::AppendTsString(ITsString * ptssIns)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptssIns);
	AssertObj(this);

	int cch;
	int crun;
	int cchCur;
	int crunCur;
	StrUni stu;
	ITsPropsBldrPtr qtpb;

	// TODO	JeffG, ShonK(JeffG) Split out common case to new func See Shon.
	// If the ITsString passed in is one of our internal strings, get the data directly.
	ITsStringRawPtr qtssr;

	CheckHr(ptssIns->QueryInterface(IID_ITsStringRaw, (void **)&qtssr));

	const OLECHAR * prgch;
	const TxtRun * prgrun;

	// Get pointers to the text and runs.
	CheckHr(qtssr->GetRawPtrs(&prgch, &cch, &prgrun, &crun));

	AssertArray(prgch, cch);
	AssertArray(prgrun, crun);
	Assert(crun > 0);

	TxtRun * prunCur;
	int ichLimCur;

	CheckHr(prgrun[crun - 1].m_qttp->GetBldr(&qtpb));

	if (!cch)
	{
		// No characters to insert - just update the current properties.
		m_qtpb = qtpb;
		return S_OK;
	}

	// Make sure we can add the new string.
	cchCur = m_stu.Length();
	stu = m_stu;
	stu.Append(prgch, cch);

	// Determine if the last run of the bldr should be merged with the first run of the
	// append string.
	crunCur = m_vrun.Size();
	if (crun && crunCur && m_vrun[crunCur - 1].PropsEqual(prgrun[0]))
		crunCur--;

	// Make sure we can add the new runs.
	m_vrun.Resize(crunCur + crun);

	// Copy runs.
	ichLimCur = cchCur;
	prunCur = m_vrun.Begin() + crunCur;
	for (int irun = 0; irun < crun; irun++)
	{
		prunCur->m_ichLim = prgrun[irun].m_ichLim + ichLimCur;
		prunCur->m_qttp = prgrun[irun].m_qttp;
		prunCur++;
	}

	// Copy Characters.
	m_stu = stu;

	// Update the current properties.
	m_qtpb = qtpb;

#ifdef TO_DO_IF_NEEDED // REVIEW ShonK: Do we need to worry about this case?
	// Otherwise, get the chars, runs, and objects through the interface methods,
	// creating temporary equivalents.

	ITsTextPropsPtr qttpLast;

	CheckHr(ptssIns->get_Length(&cch));

	CheckHr(ptssIns->get_RunCount(&crun));

	Assert(crun > 0);
	CheckHr(ptssIns->get_Properties(crun - 1, &qttpLast));

	CheckHr(qttpLast->GetBldr(&qtpb));

	if (!cch)
	{
		// No characters to insert - just update the current properties.
		m_qtpb = qtpb;
		return S_OK;
	}

	Vector<TxtRun> vrun;
	TsRunInfo tri;
	int iv;

	// Get the runs.
	CheckHr(vrun.Resize(crun));

	// Copy the runs from TsString to temp array and update lim for each.
	cchCur = m_stu.Length();
	for (iv = 0; iv < crun; iv++)
	{
		CheckHr(ptssIns->FetchRunInfo(iv, &tri, &vrun[iv].m_qttp));
		vrun[iv].m_ibLim = (cchCur + tri.ichLim) * isizeof(OLECHAR);
	}

	OLECHAR * prgch;

	// Size the char buffer to hold new string.
	CheckHr(stu.SetSize(cchCur + cch, &prgch));
	CopyItems(m_stu.Chars(), prgch, cchCur);

	// Append the new chars.
	CheckHr(ptssIns->FetchChars(0, cch, prgch + cchCur));

	// Append the temp run array.
	crunCur = m_vrun.Size();
	if (crunCur > 0 && m_vrun[crunCur - 1].PropsEqual(vrun[0]))
		CheckHr(m_vrun.Replace(crunCur - 1, crunCur, vrun.Begin(), crun));
	else
		CheckHr(m_vrun.Replace(crunCur, crunCur, vrun.Begin(), crun));

	// Set the text.
	m_stu = stu;

	// Update the current properties.
	m_qtpb = qtpb;
#endif /*TO_DO_IF_NEEDED*/

	AssertObj(this);
	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}

/*----------------------------------------------------------------------------------------------
	Clear all properties for subsequent runs.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::ClearProps()
{
	BEGIN_COM_METHOD;
	m_qtpb->Clear();
	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}

/*----------------------------------------------------------------------------------------------
	Append the given characters with the current properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::AppendRgch(const OLECHAR * prgchIns, int cchIns)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchIns, cchIns);
	//AssertObj(this);

	if (!cchIns)
		return S_OK;

	m_stu.Append(prgchIns, cchIns);

	TxtRun * prun;
	TxtRun run;

	// Get the current properties.
	CheckHr(m_qtpb->GetTextProps(&run.m_qttp));
	run.m_ichLim = m_stu.Length();

	if (m_vrun.Size() > 0 && (prun = m_vrun.End() - 1)->PropsEqual(run))
		prun->m_ichLim = run.m_ichLim;
	else
		m_vrun.Push(run);


	//AssertObj(this);

	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Set the values for the indicated scalar property. If nVar and nVal are both -1, the
	property is deleted.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::SetIntPropValues(int ttpt, int nVar, int nVal)
{
	BEGIN_COM_METHOD;
//-	if (!(nVar == -1 && nVal == -1) && ktptWs == ttpt && (uint)nVal > kwsLim)
//-		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");
	// AssertObj(this);
	// Redundant: text props asserts the same thing. Assert(ttpt != ktptWs || nVal != 0);

	return m_qtpb->SetIntPropValues(ttpt, nVar, nVal);

	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Set the value for the indicated string property. If nVar is -1 and bstrVal is empty, the
	property is deleted.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::SetStrPropValue(int ttpt, BSTR bstrVal)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrVal);
	//AssertObj(this);

	return m_qtpb->SetStrPropValueRgch(ttpt, (const byte*)bstrVal, BstrLen(bstrVal) * isizeof(OLECHAR));

	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}

/*----------------------------------------------------------------------------------------------
	Set the values for the indicated string property. If rgchVal is NULL and nValLength is 0,
	the property is deleted.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::SetStrPropValueRgch(int tpt, const byte* rgchVal, int nValLength)
{
	BEGIN_COM_METHOD;
	return m_qtpb->SetStrPropValueRgch(tpt, rgchVal, nValLength);
	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}

/*----------------------------------------------------------------------------------------------
	Get an ITsString from the current state.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::GetString(ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	AssertObj(this);

	int crun = m_vrun.Size();
	Assert(crun >= 0);

	if (crun == 0)
	{
		ITsTextPropsPtr qttp;
		m_qtpb->GetTextProps(&qttp);
		TsStrSingle::Create(NULL, 0, qttp, pptss);
		return S_OK;
	}

	int cch = m_stu.Length();
	DataReaderRgb drr(m_stu.Chars(), cch * isizeof(wchar));

	if (crun == 1)
	{
		TsStrSingle::Create(&drr, cch, m_vrun[0].m_qttp, pptss);
		return S_OK;
	}

	TsStrMulti::Create(&drr, m_vrun.Begin(), m_vrun.Size(), pptss);
	Assert(drr.IbCur() == cch * isizeof(OLECHAR));

	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Serialize the formatting information of the string to the given stream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::SerializeFmt(IStream * pstrm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);
	AssertObj(this);

	DataWriterStrm dws(pstrm);
	SerializeFmtCore(&dws);

	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Serialize the formatting information of the string to the given byte array. If the cbMax is
	too small this sets *pcb to the required size and returns S_FALSE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsIncStrBldr::SerializeFmtRgb(byte * prgb, int cbMax, int * pcb)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgb, cbMax);
	ChkComOutPtr(pcb);
	AssertObj(this);

	DataWriterRgb dwr(prgb, cbMax, true /*fIgnoreError*/);
	SerializeFmtCore(&dwr);
	Assert(dwr.IbMax() == dwr.IbCur());
	*pcb = dwr.IbMax();
	return *pcb <= cbMax ? S_OK : S_FALSE;

	END_COM_METHOD(g_factIncStrBldr, IID_ITsIncStrBldr);
}


/*----------------------------------------------------------------------------------------------
	Serialize the formatting information of the IncBldr to the given DataWriter.
----------------------------------------------------------------------------------------------*/
void TsIncStrBldr::SerializeFmtCore(DataWriter * pdwrt)
{
	AssertObj(this);
	AssertPtr(pdwrt);

	int irun;
	// Byte offset from the begining of the text props data.
	const int kcMaxProp = 10;
	FmtRunInfo * prgfri;
	FmtRunInfo rgfri[kcMaxProp];
	Vector<FmtRunInfo> vfri;
	int crun = m_vrun.Size();

	// Make sure we can save off the text prop data offsets.
	if (crun <= kcMaxProp)
	{
		prgfri = rgfri;
	}
	else
	{
		vfri.Resize(crun);
		prgfri = vfri.Begin();
	}

	// Skip past the run information for now. One int for the number of runs and two ints
	// for each run.
	pdwrt->SeekAbs(isizeof(int) + crun * isizeof(FmtRunInfo));

	// Byte offset where the prop data starts.
	int ibMin = pdwrt->IbCur();

	// Write the text props data, filling in the offset for each.  Write a duplicated
	// text prop only once.
	for (irun = 0; irun < crun; irun++)
	{
		int irunT;
		ITsTextProps * pttp;
		ITsTextPropsRawPtr qttpr;

		pttp = m_vrun[irun].m_qttp;
		for (irunT = 0; irunT < irun; irunT++)
		{
			if (m_vrun[irunT].m_qttp == pttp)
				break;
		}
		prgfri[irun].m_ichLim = m_vrun[irun].m_ichLim;
		if (irunT < irun)
		{
			prgfri[irun].m_ibProp = prgfri[irunT].m_ibProp;
		}
		else
		{
			// Need to save off the offset and write out the text props data.
			prgfri[irun].m_ibProp = pdwrt->IbCur() - ibMin;
			CheckHr(m_vrun[irun].m_qttp->QueryInterface(IID_ITsTextPropsRaw, (void **)&qttpr));
			CheckHr(qttpr->SerializeDataWriter(pdwrt));
		}
	}

	// Save off end of data	first.
	int ibLim = pdwrt->IbCur();

	// Return the data writer back to the begining of the buffer.
	pdwrt->SeekAbs(0);

	// Write number of runs.
	pdwrt->WriteInt(crun);

	// Write the run information.
	pdwrt->WriteBuf(prgfri, crun * isizeof(FmtRunInfo));

	// Return to the end of the data written.
	pdwrt->SeekAbs(ibLim);
}


#ifdef DEBUG
/*----------------------------------------------------------------------------------------------
	Find the run containing ich. If ich == Cch(), return irunLast (not irunLim). IrunAt always
	returns a valid run index, ie, IrunAt(ich) < Crun().

	Assumptions:
		0 <= ich <= Cch().

	Exit conditions:
		0 <= irun < Crun().
		IchMinRun(irun) <= ich.
		ich < IchLimRun(irun) || ich == Cch() irun = (crun - 1).
----------------------------------------------------------------------------------------------*/
int TsIncStrBldr::IrunAt(int ich)
{
	Assert(0 <= ich && ich <= m_stu.Length());
	int ib = ich * isizeof(OLECHAR);
	int irunMin = 0;
	int irunLim;
	int	crun;
	TxtRun * prgrun;

	crun = irunLim = m_vrun.Size();
	prgrun = m_vrun.Begin();
	// Perform a binary search.
	while (irunMin < irunLim)
	{
		int irunT = (irunMin + irunLim) >> 1;
		if (ib >= prgrun[irunT].m_ichLim)
			irunMin = irunT + 1;
		else
			irunLim = irunT;
	}
	if (irunMin >= crun)
		return crun - 1;
	return irunMin;
}

/*----------------------------------------------------------------------------------------------
	Validate the object's state.
----------------------------------------------------------------------------------------------*/
bool TsIncStrBldr::AssertValid(void)
{
	AssertPtr(this);

	int	cch = m_stu.Length();
	int crun = m_vrun.Size();
	int irun;
	TxtRun * prgrun;

	// Validate the runs.
	Assert(cch >= crun);
	Assert(crun || !cch);
	if (crun)
	{
		prgrun = m_vrun.Begin();
		// Make sure the last run's lim is the same as the cch.
		Assert(prgrun[crun - 1].m_ichLim == cch);

		if (crun > 1)
		{
			for (irun = 0; irun < crun - 1; irun++)
			{
				// Make sure the lim of each run is greater then the previous one.
				Assert(prgrun[irun + 1].m_ichLim > prgrun[irun].m_ichLim);
				// Make sure there are no adjacent runs with the same properties.
				Assert(!prgrun[irun + 1].PropsEqual(prgrun[irun]));
			}
		}
	}

	return true;
}

#endif // DEBUG

#include "HashMap_i.cpp"
template class HashMapChars<int>;
