/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TsString.h
Responsibility: Jeff Gayle
Last reviewed: 8/25/99

	Implementations of ITsString and ITsStrBldr.

	TsStrBase is a template that provides implementations of COM methods on top of
	TxtBufSingle, TxtBufMulti and TxtBufBldr, TxtBufGeneral.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TsString_H
#define TsString_H 1

/*----------------------------------------------------------------------------------------------
	Describes a run of characters. m_ichLim indicates the lim of the run, and m_qttp holds the
	properties of the run.  m_qttp should not be NULL.
	Hungarian: run
----------------------------------------------------------------------------------------------*/
class TxtRun
{
public:
	ITsTextPropsPtr m_qttp;
	int m_ichLim;

	TxtRun()
	{
		m_ichLim = 0;
	}
	int IchLim(void) const
	{
		return m_ichLim;
	}
	bool PropsEqual(const TxtRun & run) const
	{
		return m_qttp == run.m_qttp;
	}
};


/*----------------------------------------------------------------------------------------------
	ITsStringRaw is used to provide the method GetRawPtrs to internal methods on the TxtBuf*
	classes. This allows us to get at the raw pointers directly instead of through the
	interface. This improves our efficiency in certain operations, ReplaceTsString and
	AppendTsString in particular.
	Hungarian: tssr
----------------------------------------------------------------------------------------------*/
interface ITsStringRaw : public ITsString
{
public:
	// Get pointers to the internal data structures of the TsString object. Any of the
	// input pointers may be NULL, indicating that the information isn't needed.
	STDMETHOD(GetRawPtrs)(const OLECHAR ** pprgch, int * pcch, const TxtRun ** pprgrun,
		int * pcrun) = 0;
};


/*----------------------------------------------------------------------------------------------
	IID for ITsStringRaw
----------------------------------------------------------------------------------------------*/
interface __declspec(uuid("2AC0CB90-B14B-11d2-B81D-00400541F9DA")) ITsStringRaw;
#define IID_ITsStringRaw __uuidof(ITsStringRaw)
DEFINE_COM_PTR(ITsStringRaw);


/*----------------------------------------------------------------------------------------------
	Internal representation of a single-run string.
	Hungarian: NONE - never sees the light of day..
----------------------------------------------------------------------------------------------*/
class TxtBufSingle : public ITsStringRaw
{
protected:
	long m_cref;

#ifdef DEBUG
	// Used to track the number of locks.
	int m_cactLock;
#endif // DEBUG
#if WIN32
	IUnknownPtr m_qunkMarshaler;
#endif

	byte m_nNormalFlags;

	// WARNING: Don't add any fields after m_run!
	TxtRun m_run;

	// The characters all follow including a terminating zero. We call NewObjExtra to allocate
	// enough space. Note that Prgch() points to a valid BSTR.
	// OLECHAR m_rgch[cch + 1];

	// Static method to compute the byte size of the TxtBufSingle needed to store cch
	// characters.
	static int GetExtraSize(int cch)
	{
		Assert(0 <= cch);
		return (cch + 1) * isizeof(OLECHAR); // We must add one to cch for NULL termination.
	}

	TxtBufSingle(void);
	~TxtBufSingle(void);

	// Return a pointer to the TxtRun.
	TxtRun * Prun(int irun)
	{
		Assert(0 == irun);
		return &m_run;
	}

	// Return the number of characters.
	int Cch(void)
	{
		return m_run.m_ichLim;
	}

	// Return a pointer to the characters.
	OLECHAR * Prgch(void)
	{
		return reinterpret_cast<OLECHAR *>(&m_run + 1);
	}

	// Return the number of runs. This always returns 1.
	int Crun(void)
	{
		return 1;
	}

	// Return the index of the run containing ich. This always returns 0.
	int IrunAt(int ich)
	{
		Assert((uint)ich <= (uint)Cch());
		return 0;
	}

	// Fetch information about a run.
	void FetchRunAt(int ich, TsRunInfo * ptri, ITsTextProps ** ppttp);
	void FetchRun(int irun, TsRunInfo * ptri, ITsTextProps ** ppttp);

	// Return the start of the run.
	int IchMinRun(int irun)
	{
		Assert(0 == irun);
		return 0;
	}

	// Return the end of the run.
	int IchLimRun(int irun)
	{
		Assert(0 == irun);
		return Cch();
	}

	// Return the end of the run as a byte count.
	int IbLimRun(int irun)
	{
		Assert(0 == irun);
		return m_run.m_ichLim * isizeof(OLECHAR);
	}

	// Return the number of characters in the run.
	int CchRun(int irun)
	{
		Assert(0 == irun);
		return m_run.m_ichLim;
	}

	// Return the properties of the run.
	ITsTextProps * PropsRun(int irun)
	{
		Assert(0 == irun);
		return m_run.m_qttp;
	}

#ifdef DEBUG
	// Check the validity of the TxtBufSingle.
	bool AssertValid(void)
	{
		AssertPtrSize(this, isizeof(TxtBufSingle) + Cch());
		Assert(m_cref > 0);
		AssertPtr(m_run.m_qttp);
		Assert(m_run.m_ichLim >= 0);
		Assert(Prgch()[Cch()] == 0);
		return true;
	}
#endif // DEBUG
};


/*----------------------------------------------------------------------------------------------
	Internal representation of a multi-run string.
	Hungarian: tbm.
----------------------------------------------------------------------------------------------*/
class TxtBufMulti : public ITsStringRaw
{
protected:
	long m_cref;

#ifdef DEBUG
	// Used to track the number of locks.
	int m_cactLock;
#endif // DEBUG
#if WIN32
	IUnknownPtr m_qunkMarshaler;
#endif

	// This byte records the known normalizations which are valid for this string.
	// The bits are defined by 1 << knmNFC | 1 << knmNFSC ...
	byte m_nNormalFlags;

	// Cached length.
	int m_cch;

	int m_crun;
	// From here on all the runs are stored and then all the characters, including a
	// terminating zero. Note that Prgch() points to a valid BSTR.
	//
	// TxtRun m_rgrun[m_crun];
	// OLECHAR m_cch[Cch() + 1];

	// Static method to compute the size of the TxtBufMulti needed to store cch characters
	// and crun runs.
	static int GetExtraSize(int cch, int crun)
	{
		Assert(2 <= cch);
		Assert(2 <= crun);
		return crun * isizeof(TxtRun) + (cch + 1) * isizeof(OLECHAR);
	}

	TxtBufMulti(void);
	~TxtBufMulti(void);

	// Return a pointer to the TxtRun.
	TxtRun * Prun(int irun)
	{
		Assert((uint)irun < (uint)m_crun);
		return reinterpret_cast<TxtRun *>(this + 1) + irun;
	}

	// Return the number of characters.
	int Cch(void)
	{
		return m_cch;
	}

	// Return a pointer to the characters.
	OLECHAR * Prgch(void)
	{
		return reinterpret_cast<OLECHAR *>(reinterpret_cast<TxtRun *>(this + 1) + m_crun);
	}

	// Return the number of runs.
	int Crun(void)
	{
		return m_crun;
	}

	// Return the index of the run containing ich.
	int IrunAt(int ich);

	// Fetch information about a run.
	void FetchRun(int irun, TsRunInfo * ptri, ITsTextProps ** ppttp);
	void FetchRunAt(int ich, TsRunInfo * ptri, ITsTextProps ** ppttp)
	{
		Assert((uint)ich <= (uint)Cch());
		AssertPtr(ptri);
		AssertPtr(ppttp);
		Assert(!*ppttp);

		FetchRun(IrunAt(ich), ptri, ppttp);
	}

	// Return the start of the run.
	int IchMinRun(int irun)
	{
		Assert((uint)irun < (uint)m_crun);
		if (0 == irun)
			return 0;
		return Prun(irun - 1)->IchLim();
	}

	// Return the end of the run.
	int IchLimRun(int irun)
	{
		Assert((uint)irun < (uint)m_crun);
		return Prun(irun)->IchLim();
	}

	// Return the end of the run as a byte count.
	int IbLimRun(int irun)
	{
		Assert((uint)irun < (uint)m_crun);
		return Prun(irun)->m_ichLim * isizeof(OLECHAR);
	}

	// Return the number of characters in the run.
	int CchRun(int irun)
	{
		Assert((uint)irun < (uint)m_crun);
		if (0 == irun)
			return Prun(0)->IchLim();
		return Prun(irun)->m_ichLim - Prun(irun - 1)->m_ichLim;
	}

	// Return the properties of the run.
	ITsTextProps * PropsRun(int irun)
	{
		Assert((uint)irun < (uint)m_crun);
		return Prun(irun)->m_qttp;
	}

#ifdef DEBUG
	// Check the validity of the TxtBufMulti.
	bool AssertValid(void)
	{
		Assert(m_cref > 0);
		Assert((uint)m_cch == (uint)Prun(m_crun - 1)->m_ichLim);
		Assert(m_crun >= 2);
		Assert(m_cch >= m_crun);
		AssertPtrSize(this, isizeof(TxtBufMulti) + Cch());
		Assert(Prgch()[m_cch] == 0);
		return true;
	}
#endif // DEBUG
};


/*----------------------------------------------------------------------------------------------
	Internal representation of a string builder.
	Hungarian: NONE.
----------------------------------------------------------------------------------------------*/
class TxtBufBldr : public ITsStrBldr
{
protected:
	long m_cref;
	// The runs.
	Vector<TxtRun> m_vrun;
	// The text.
	StrUni m_stu;

	TxtBufBldr(void);
	~TxtBufBldr(void);

	// Return a pointer to the TxtRun.
	TxtRun * Prun(int irun)
	{
		Assert((uint)irun < (uint)m_vrun.Size());
		return &m_vrun[irun];
	}

	// Return the number of characters.
	int Cch(void)
	{
		return m_stu.Length();
	}

	// Return a pointer to the characters.
	const OLECHAR * Prgch(void)
	{
		return m_stu.Chars();
	}

	// Return the number of runs. This always returns 1.
	int Crun(void)
	{
		return m_vrun.Size();
	}

	// Return the index of the run containing ich.
	int IrunAt(int ich);

	// Fetch information about a run.
	void FetchRun(int irun, TsRunInfo * ptri, ITsTextProps ** ppttp);
	void FetchRunAt(int ich, TsRunInfo * ptri, ITsTextProps ** ppttp)
	{
		Assert((uint)ich <= (uint)Cch());
		AssertPtr(ptri);
		AssertPtr(ppttp);
		Assert(!*ppttp);

		FetchRun(IrunAt(ich), ptri, ppttp);
	}


	// Return the start of the run.
	int IchMinRun(int irun)
	{
		Assert((uint)irun < (uint)m_vrun.Size());
		if (0 == irun)
			return 0;
		return m_vrun[irun - 1].IchLim();
	}

	// Return the end of the run.
	int IchLimRun(int irun)
	{
		Assert((uint)irun < (uint)m_vrun.Size());
		return m_vrun[irun].IchLim();
	}

	// Return the end of the run as a byte count.
	int IbLimRun(int irun)
	{
		Assert((uint)irun < (uint)m_vrun.Size());
		return m_vrun[irun].m_ichLim * isizeof(OLECHAR);
	}

	// Return the number of characters in the run.
	int CchRun(int irun)
	{
		Assert((uint)irun < (uint)m_vrun.Size());
		if (0 == irun)
			return m_vrun[0].IchLim();
		return m_vrun[irun].m_ichLim - m_vrun[irun - 1].m_ichLim;
	}

	// Return the properties of the run.
	ITsTextProps * PropsRun(int irun)
	{
		Assert((uint)irun < (uint)m_vrun.Size());
		return m_vrun[irun].m_qttp;
	}

#ifdef DEBUG
	bool AssertValid(void);
#endif // DEBUG
};


/*
	Layout of persistent (serialized) storage
	-----------------------------------------

	Txt: array of Unicode characters (SQL_WVARCHAR or SQL_WLONGVARCHAR)

	Fmt: array of binary data (SQL_BINARY or SQL_LONGBINARY)
		This is divided into 3 regions:

		1. number of runs
			4 bytes: integer containing number of runs (crun)

		2. array of basic run information (crun entries)
			crun * 8 bytes: run information array
				4 bytes: integer containing the Lim of the run
				4 bytes: integer containing the offset into the run property region

		3. detailed property information for each run
			Each section contains the following (<= crun successive sections):
			numbers of elements in this set of run properties
				2 bytes: number of encodings (cws)
				1 byte:  number of scalar properties (cscp)
				1 byte:  number of text properties (cstp)
				cws * 8 bytes: writing system stack for this run [struct TsEncProp]
					4 bytes: integer writing system the language (must not be zero)
					4 bytes: integer writing system the old writing system (may be zero)
				cscp * 12 bytes: array of scalar properties for this run [struct TsIntProp]
					4 bytes: integer containing the type of this property
					4 bytes: integer containing the variation of this property type
					4 bytes: integer containing the value of this property
				cstp * (12 + N) bytes: array of text properties for this run [struct TsStrProp]
					4 bytes: integer containing the type of this property
					4 bytes: integer containing the variation of this property type
					4 bytes: integer containing the number of characters in the value (cch)
					cch * 2 bytes: array of Unicode characters

		Note that multiple runs can share a single run properties section.

	The minimal Fmt array contains the following 4 4-byte integers:

		0x00000001					= number of runs
		<length of Txt array>		= Lim of the first run
		0x00000000					= offset into run property region for first run
		0x00000000					= empty writing system stack, no scalar properties, and no text
									  properties
 */
template<class TxtBuf> class TsNormalizeMethod; // A private class used to implement the get_Normalized method.
/*----------------------------------------------------------------------------------------------
	This template implements ITsString methods on top of a TxtBuf. It is used to implement both
	ITsString and ITsStrBldr. The viable base classes are TxtBufSingle, TxtBufMulti,
	TxtBufGeneral and TxtBufBldr. The classes derived from these are TsStrSingle, TsStrMulti,
	TsStrGeneral, and TsStrBldr	respectively.

	NOTE: It is very important that the methods be marked STDMETHODIMP, not STDMETHOD.
	This is so they don't end up creating extra vtable slots on TsStrBldr, since
	ITsStrBldr does not have all of these methods.
----------------------------------------------------------------------------------------------*/
template<class TxtBuf> class TsStrBase : public TxtBuf
{
	friend class TsNormalizeMethod<TxtBuf>;
public:
	typedef TxtBuf BaseClass;
	using BaseClass::m_cref;

	TsStrBase()
	{
	}

	// IUnknown methods.
	STDMETHODIMP_(UCOMINT32) AddRef(void);
	STDMETHODIMP_(UCOMINT32) Release(void);

	// ITsString methods.
	STDMETHODIMP get_Text(BSTR * pbstr);
	STDMETHODIMP get_Length(int * pcch);
	STDMETHODIMP get_RunCount(int * pcrun);

	// Mapping between character indexes and run indexes.
	STDMETHODIMP get_RunAt(int ich, int * pirun);
	STDMETHODIMP get_MinOfRun(int irun, int * pichMin);
	STDMETHODIMP get_LimOfRun(int irun, int * pichLim);
	STDMETHODIMP GetBoundsOfRun(int irun, int * pichMin, int * pichLim);

	// Run information.
	STDMETHODIMP FetchRunInfoAt(int ich, TsRunInfo * ptri, ITsTextProps ** ppttp);
	STDMETHODIMP FetchRunInfo(int irun, TsRunInfo * ptri, ITsTextProps ** ppttp);

	// Fetching characters.
	STDMETHODIMP get_RunText(int irun, BSTR * pbstr);
	STDMETHODIMP GetChars(int ichMin, int ichLim, BSTR * pbstr);
	STDMETHODIMP FetchChars(int ichMin, int ichLim, OLECHAR * prgch);

	// Locking text.
	STDMETHODIMP LockText(const OLECHAR ** pprgch, int * pcch);
	STDMETHODIMP UnlockText(const OLECHAR * prgch);
	STDMETHODIMP LockRun(int irun, const OLECHAR ** pprgch, int * pcch);
	STDMETHODIMP UnlockRun(int irun, const OLECHAR * prgch);

	// Getting properties.
	STDMETHODIMP get_PropertiesAt(int ich, ITsTextProps ** ppttp);
	STDMETHODIMP get_Properties(int irun, ITsTextProps ** ppttp);

	// Builders.
	STDMETHODIMP GetBldr(ITsStrBldr ** pptsb);
	STDMETHODIMP GetIncBldr(ITsIncStrBldr ** pptisb);

	// Serialization.
	STDMETHODIMP GetFactoryClsid(CLSID * pclsid);
	STDMETHODIMP SerializeFmt(IStream * pstrm);
	STDMETHODIMP SerializeFmtRgb(byte * prgb, int cbMax, int * pcb);

	// Equality
	STDMETHODIMP Equals(ITsString * ptss, ComBool * pfEqual);

	// XML Export.
	STDMETHODIMP WriteAsXml(IStream * pstrm, ILgWritingSystemFactory * pwsf,
		int cchIndent, int ws, ComBool fWriteObjData);
	STDMETHODIMP GetXmlString(ILgWritingSystemFactory * pwsf,
		int cchIndent, int ws, ComBool fWriteObjData, BSTR * pbstr);
	STDMETHODIMP WriteAsXmlExtended(IStream * pstrm, ILgWritingSystemFactory * pwsf,
		int cchIndent, int ws, ComBool fWriteObjData, ComBool fUseRFC4646);

	// Normalization
	STDMETHODIMP get_IsNormalizedForm(FwNormalizationMode nm, ComBool * pfRet);
	STDMETHODIMP get_NormalizedForm(FwNormalizationMode nm, ITsString ** pptssRet);
	STDMETHODIMP NfdAndFixOffsets(ITsString ** pptssRet, int ** prgpichOffsetsToFix,
		int cichOffsetsToFix);

	// ITsStringRaw method.
	STDMETHODIMP GetRawPtrs(const OLECHAR ** pprgch, int * pcch, const TxtRun ** pprgrun,
		int * pcrun);
	STDMETHODIMP GetSubstring(int ichMin, int ichLim, ITsString ** pptssRet);
	STDMETHODIMP get_StringProperty(int irun, int tpt, BSTR * pbstr);
	STDMETHODIMP get_StringPropertyAt(int ich, int tpt, BSTR * pbstr);
	STDMETHODIMP get_WritingSystem(int irun, int * pws);
	STDMETHODIMP get_WritingSystemAt(int ich, int * pws);
	STDMETHODIMP get_IsRunOrc(int iRun, ComBool * pfIsOrc);
	void NoteNormalized(FwNormalizationMode nm);

protected:
	void SerializeFmtCore(DataWriter * pdwrt);
	bool IsKnownNormalized(FwNormalizationMode nm);
	byte GetFlagByte()
	{
		return BaseClass::m_nNormalFlags;
	};
	void SetFlagByte(byte n)
	{
		BaseClass::m_nNormalFlags = n;
	};
};


/*----------------------------------------------------------------------------------------------
	This implements a single-run ITsString.
	Hungarian: sts
----------------------------------------------------------------------------------------------*/
class TsStrSingle : public TsStrBase<TxtBufSingle>
{
public:
	static void Create(DataReader * pdrdr, int cch, ITsTextProps * pttp, TsStrSingle ** ppsts);
	static void Create(DataReader * pdrdr, int cch, ITsTextProps * pttp, ITsString ** pptss);
	static void Create(const OLECHAR * prgch, int cch, TsTextProps * pttp, ITsString ** ppsts);

	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);

protected:
	TsStrSingle()
	{
#if WIN32
		CoCreateFreeThreadedMarshaler(this, &m_qunkMarshaler);
#endif
	}
};


/*----------------------------------------------------------------------------------------------
	This implements a multi-run ITsString.
	Hungarian: stn
----------------------------------------------------------------------------------------------*/
class TsStrMulti : public TsStrBase<TxtBufMulti>
{
public:
	static void Create(DataReader * pdrdr, const TxtRun * prgrun, int crun,
		TsStrMulti ** ppstm);
	static void Create(DataReader * pdrdr, const TxtRun * prgrun, int crun,
		ITsString ** pptss);

	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);

protected:
	TsStrMulti()
	{
#if WIN32
		CoCreateFreeThreadedMarshaler(this, &m_qunkMarshaler);
#endif
	}
};


/*----------------------------------------------------------------------------------------------
	This implements ITsStrBldr.
	Hungarian: ztsb
----------------------------------------------------------------------------------------------*/
class TsStrBldr : public TsStrBase<TxtBufBldr>
{
public:
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// Static methods to create a string builder initialized with the given text and runs.
	static void Create(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun,
		TsStrBldr ** ppztsb);
	static void Create(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun,
		ITsStrBldr ** pptsb);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);

	// Clear the builder.
	STDMETHOD(Clear)();

	// ITsStrBldr methods to modify the current state.
	STDMETHOD(Replace)(int ichMin, int ichLim, BSTR bstrIns, ITsTextProps * pttp);
	STDMETHOD(ReplaceTsString)(int ichMin, int ichLim, ITsString * ptssIns);
	STDMETHOD(ReplaceRgch)(int ichMin, int ichLim, const OLECHAR * prgchIns, int cchIns,
		ITsTextProps * pttp);

	STDMETHOD(SetProperties)(int ichMin, int ichLim, ITsTextProps * pttp);
	STDMETHOD(SetIntPropValues)(int ichMin, int ichLim, int scp, int nVar, int nVal);
	STDMETHOD(SetStrPropValue)(int ichMin, int ichLim, int stp, BSTR bstrVal);

	// Create an ITsString from the current state.
	STDMETHOD(GetString)(ITsString **pptss);

	// Serialization.
	STDMETHOD(GetBldrClsid)(CLSID * pclsid);
	STDMETHOD(SerializeFmt)(IStream * pstrm);
	STDMETHOD(SerializeFmtRgb)(BYTE * prgb, int cbMax, int * pcbNeeded);

protected:
	TsStrBldr()
	{
	}

	// Initialize the string builder from the given characters and runs.
	void Init(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun);
	void ReplaceCore(int ichMin, int ichLim, const OLECHAR * prgchIns, int cchIns,
		const TxtRun * prgrun, int crun);
	void EnsureRuns(int ichMin, int ichLim, int * pirunMin, int * pirunLim);
	void EditIntProp(ITsTextProps * pttp, int ttpt, int nVar, int nVal, ITsTextProps ** ppttp);
	void EditStrProp(ITsTextProps * pttp, int ttpt, BSTR bstrVal, ITsTextProps ** ppttp);

	void SerializeFmtCore(DataWriter * pdwrt);
#ifdef DEBUG
	void DebugDumpRunInfo(bool fShowEncodingStack);
#endif // DEBUG
};


/*----------------------------------------------------------------------------------------------
	This implements ITsIncStrBldr. An incremental string builder builds strings by appending
	segments on the end of an existing string segment. If bulding a string from scratch, the
	first append operation will initialize the first segment. This differs from the TsStrBldr
	above in that most of its methods replace an existing portion of the string. TsStrBuldr can
	perform an append by replacing the ichLim character with a string but it is more convenient
	to use the TsIncStrBldr for append operations.

	This class does not implement TsStrBase because the incremental builder does not need the
	accessor methods.
	Hungarian: ztisb
----------------------------------------------------------------------------------------------*/
class TsIncStrBldr : public ITsIncStrBldr
{
public:
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// Static methods used to create an incremental string builder initialized with a given
	// string and runs.
	static void Create(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun,
		TsIncStrBldr ** ppztisb);
	static void Create(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun,
		ITsIncStrBldr ** pptisb);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void);
	STDMETHOD_(UCOMINT32, Release)(void);

	// Clear the Inc builder.
	STDMETHOD(Clear)();

	// Retrieve the text string.
	STDMETHOD(get_Text)(BSTR * pbstr);

	// Methods to modify the current state.
	STDMETHOD(Append)(BSTR bstrIns);
	STDMETHOD(AppendTsString)(ITsString * ptssIns);
	STDMETHOD(AppendRgch)(const OLECHAR * prgchIns, int cchIns);

	STDMETHOD(SetIntPropValues)(int scp, int nVar, int nVal);
	STDMETHOD(SetStrPropValue)(int stp, BSTR bstrVal);
	STDMETHOD(SetStrPropValueRgch)(int tpt, const byte* rgchVal, int nValLength);
	// Get an ITsString from the current state.
	STDMETHOD(GetString)(ITsString ** pptss);

	// Serialization.
	STDMETHOD(GetIncBldrClsid)(CLSID * pclsid);
	STDMETHOD(SerializeFmt)(IStream * pstrm);
	STDMETHOD(SerializeFmtRgb)(BYTE * prgb, int cbMax, int * pcbNeeded);

	STDMETHOD(ClearProps)();

protected:
	long m_cref;
	Vector<TxtRun> m_vrun;
	StrUni m_stu;

	// Current properties
	ITsPropsBldrPtr m_qtpb;

	TsIncStrBldr(void);
	~TsIncStrBldr(void);

	void Init(const OLECHAR * prgch, int cch, const TxtRun * prgrun, int crun);
	void SerializeFmtCore(DataWriter * pdwrt);

#ifdef DEBUG
	// The following four methods are used to support AssertValid()
	// Return the index of the run containing ich.
	int IrunAt(int ich);

	// Return the start of the run.
	int IchMinRun(int irun)
	{
		Assert(0 <= irun && irun < m_vrun.Size());
		if (0 == irun)
			return 0;
		return m_vrun[irun - 1].IchLim();
	}

	bool AssertValid(void);
#endif // DEBUG
};

/*----------------------------------------------------------------------------------------------
	This data structure stores the information recorded in the persistent store for every
	run in a formatted string.
	Hungarian: fri
----------------------------------------------------------------------------------------------*/
struct FmtRunInfo
{
	int m_ichLim;
	int m_ibProp;
};


#endif // !TsString_H
