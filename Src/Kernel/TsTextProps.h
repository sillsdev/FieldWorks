/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TsTextProps.h
Responsibility: Jeff Gayle
Last reviewed: 8/25/99

	Implementations of ITsTextProps and related interfaces.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TxTextProps_H
#define TxTextProps_H 1


// Forward declaration.
class TsPropHolder;

/*----------------------------------------------------------------------------------------------
	Describes an integer property.
	Hungarian: tip
----------------------------------------------------------------------------------------------*/
struct TsIntProp
{
	int m_tpt;
	int m_nVar;
	int m_nVal;
	TsIntProp ()
	{
		m_tpt = 0;
		m_nVar = 0;
		m_nVal = 0;
	}
};

/*----------------------------------------------------------------------------------------------
	Describes a string property.
	Hungarian: tsp
----------------------------------------------------------------------------------------------*/
struct TsStrProp
{
	int m_tpt;
	int m_hstuVal;		// m_hstuVal is a cookie into the StrHolder.
	TsStrProp ()
	{
		m_tpt = 0;
		m_hstuVal = 0;
	}
};


/*----------------------------------------------------------------------------------------------
	ITsTextPropsRaw exposes the SerializeCore method indirectly through SerializeDataWriter to
	other object methods such as: TsIncStrBldr::Serialize() and TsIncStrBldr::SerlializeRgb().
	Hungarian: ttpr
----------------------------------------------------------------------------------------------*/
interface ITsTextPropsRaw : public ITsTextProps
{
public:
	// SerializeCore accepts a DataWriter which uses polymorphism to hide a stream or
	// byte buffer.
	STDMETHOD(SerializeDataWriter)(DataWriter * pdwrt) = 0;
};


/*----------------------------------------------------------------------------------------------
	IID for ITsTextPropsRaw
----------------------------------------------------------------------------------------------*/
interface __declspec(uuid("31BDA5F0-D286-11d3-9BBC-00400541F9E9")) ITsTextPropsRaw;
#define IID_ITsTextPropsRaw __uuidof(ITsTextPropsRaw)
DEFINE_COM_PTR(ITsTextPropsRaw);


/*----------------------------------------------------------------------------------------------
	Implementation of ITsTextProps. This is a thread-safe, "agile" component.
	Hungarian: zttp
----------------------------------------------------------------------------------------------*/
class TsTextProps : public ITsTextPropsRaw
{
public:
	static void Create(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
		TsTextProps ** ppzttp);
	static void Create(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
		ITsTextProps ** ppttp);
	static void DeserializeDataReader(DataReader * pdrdr, ITsTextProps ** ppttp);
	static void CreateCanonical(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
		TsTextProps ** ppzttp);
	// IUnknown methods.

	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void);
	STDMETHOD_(UCOMINT32, Release)(void);

	// ITsTextProps methods.

	STDMETHOD(get_IntPropCount)(int * pcv);
	STDMETHOD(GetIntProp)(int iv, int * ptpt, int * pnVar, int * pnVal);
	STDMETHOD(GetIntPropValues)(int tpt, int * pnVar, int * pnVal);

	STDMETHOD(get_StrPropCount)(int * pcv);
	STDMETHOD(GetStrProp)(int iv, int * ptpt, BSTR * pbstrVal);
	STDMETHOD(GetStrPropValue)(int tpt, BSTR * pbstrVal);

	STDMETHOD(GetBldr)(ITsPropsBldr ** pptpb);

	STDMETHOD(GetFactoryClsid)(CLSID * pclsid);
	STDMETHOD(Serialize)(IStream * pstrm);
	STDMETHOD(SerializeRgb)(byte * prgb, int cbMax, int * pcb);
	STDMETHOD(SerializeRgPropsRgb)(int cpttp, ITsTextProps ** rgpttp, int * rgich,
		BYTE * prgb, int cbMax, int * pcb);
	STDMETHOD(WriteAsXml)(IStream * pstrm, ILgWritingSystemFactory * pwsf, int cchIndent);

	// ITsTextPropsRaw methods.

	STDMETHOD(SerializeDataWriter)(DataWriter * pdwrt);

	HRESULT GetStrPropValueInternal(int tpt, BSTR * pbstrVal);
	bool FindIntProp(int tpt, int * pitip);
	bool FindStrProp(int tpt, int * pitsp);
	// Returns a pointer to an element in the Integer Property list.
	TsIntProp * Ptip(int itip)
	{
		Assert((uint)itip <= (uint)m_ctip);
		return reinterpret_cast<TsIntProp *>((byte *)(this + 1)) + itip;
	}
protected:
	friend class TsPropsHolder;

	long m_cref;

	// Used by TsPropsHolder - the thread global hash table of text properties.
	TsTextProps * m_pzttpNext;
	uint m_uHash;

	byte m_ctip;
	byte m_ctsp;
	// Memory for these objects is allocated when the object is instantiated.
	// TsIntProp m_rgtip[m_ctip];
	// TsStrProp m_rgtsp[m_ctsp];

	IUnknownPtr m_qunkMarshaler;

	// Static method to compute the size of the TsTextProps needed to store entries.
	// ctip is the number of integer properties and ctsp is the number of string properties.
	static int GetExtraSize(int ctip, int ctsp)
	{
		Assert((int)(byte)ctip == ctip);
		Assert((int)(byte)ctsp == ctsp);
		return ctip * isizeof(TsIntProp) + ctsp * isizeof(TsStrProp);
	}

	static bool IsDataCanonical(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp,
		int ctsp);
	static void MakeDataCanonical(TsIntProp * prgtip, int & ctip, TsStrProp * prgtsp,
		int & ctsp);

	TsTextProps(void);
	~TsTextProps(void);

	// Returns a pointer to an element in the the String Property list.
	TsStrProp * Ptsp(int itsp)
	{
		Assert((uint)itsp <= (uint)m_ctsp);
		return reinterpret_cast<TsStrProp *>((byte *)(this + 1) + m_ctip * isizeof(TsIntProp)) +
			itsp;
	}

	void SerializeCore(DataWriter * pdwrt);

#ifdef DEBUG
	const OLECHAR * StrPropValue(int tpt);

#if WIN32 // TODO-Linux: port to get debugging information.
	class Dbw1 : public DebugWatch
	{
		virtual OLECHAR * Watch();
	public:
		TsTextProps *m_pzttp;
	};
	Dbw1 m_dbw1;

	class Dbw2 : public DebugWatch
	{
		virtual OLECHAR * Watch();
	public:
		TsTextProps *m_pzttp;
	};
	Dbw2 m_dbw2;
#endif
	StrUni m_stuDebug; // Holds a description useful only for debug.
	StrUni AddIntPropToDebugInfo(int tpt, int nVar, int nVal);
	void BuildDebugInfo();
#endif
};


/*----------------------------------------------------------------------------------------------
	TsPropsHolder is a hash table containing all TsTextProps active in the system.
	It is used to share TsTextProps.
	Hungarian: tph.
----------------------------------------------------------------------------------------------*/
class TsPropsHolder
{
public:
	static TsPropsHolder * GetPropsHolder(void);

	TsPropsHolder(void);
	~TsPropsHolder(void);

protected:
	friend class TsTextProps;

	TsTextProps ** m_prgpzttpHash;

	// m_cpzttpHash is the number of buckets.
	int m_cpzttpHash;

	// Total number of entries.
	int m_cpzttp;

	Mutex m_mutex;

	bool Find(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
		uint uHash, TsTextProps ** ppzttp);
	void Add(TsTextProps * pzttp);
	void Remove(TsTextProps * pzttp);
	void Rehash(void);
};


/*----------------------------------------------------------------------------------------------
	Used to hold common strings such as style names.
	Hungarian: tsh.
----------------------------------------------------------------------------------------------*/
class TsStrHolder
{
public:
	static TsStrHolder * GetStrHolder(void);

	TsStrHolder(void);
	~TsStrHolder(void);

	int GetCookieFromString(StrUni & stu);
	void GetStringFromCookie(int hstu, StrUni & stu);
	void GetBstr(int hstu, BSTR * pbstr);

protected:
	/*------------------------------------------------------------------------------------------
		String entry.
		Hungarian: she.
	------------------------------------------------------------------------------------------*/
	class StrHolderEntry
	{
	public:
		StrUni m_stu;
		uint m_uHash;
		// A m_isheNext value of -1 indicates the end of a bucket chain.
		int m_isheNext;
	};

	// Holds all the str holder entries.
	Vector<StrHolderEntry> m_vshe;
	// A m_visheHash value of -1 indicates the end of a bucket chain.
	Vector<int> m_visheHash;

	Mutex m_mutex;

	bool Find(StrUni & stu, uint uHash, int * pishe);
	void Add(StrUni & stu, uint uHash, int * pishe);
	void Rehash(void);
public:
#ifdef DEBUG
	const OLECHAR * GetString(int hstu);
#endif
};


/*----------------------------------------------------------------------------------------------
	Implementation of ITsPropsBldr interface.
	Hungarian: tpb.
----------------------------------------------------------------------------------------------*/
class TsPropsBldr : public ITsPropsBldr
{
public:
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);
	static void Create(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
		TsPropsBldr ** ppztpb);
	static void Create(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
		ITsPropsBldr ** pptpb);

	TsPropsBldr(void);
	~TsPropsBldr(void);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void);
	STDMETHOD_(UCOMINT32, Release)(void);

	// Methods to get the current state.

	STDMETHOD(get_IntPropCount)(int * pcv);
	STDMETHOD(GetIntProp)(int iv, int * ptpt, int * pnVar, int * pnVal);
	STDMETHOD(GetIntPropValues)(int tpt, int * pnVar, int * pnVal);

	STDMETHOD(get_StrPropCount)(int * pcv);
	STDMETHOD(GetStrProp)(int iv, int * ptpt, BSTR * pbstrVal);
	STDMETHOD(GetStrPropValue)(int tpt, BSTR * pbstrVal);

	// Methods to modify the current state.
	STDMETHOD(SetIntPropValues)(int tpt, int nVar, int nVal);
	STDMETHOD(SetStrPropValue)(int tpt, BSTR bstrVal);
	STDMETHOD(SetStrPropValueRgch)(int tpt, const byte* rgchVal, int nValLength);

	// Create an ITsTextProps from the current state.
	STDMETHOD(GetTextProps)(ITsTextProps ** ppttp);

	STDMETHOD(Clear)();

protected:
	/*------------------------------------------------------------------------------------------
		Implementation of a string property.
		Hungarian: tspi.
		REVIEW ShonK, JeffG(JeffG) Should we keep track of a reference count?
	------------------------------------------------------------------------------------------*/
	class TsStrPropImp
	{
	public:
		int m_tpt;
		int m_nVar;
		StrUni m_stuVal;
		TsStrPropImp()
		{
			m_tpt = 0;
			m_nVar = 0;
		}
	};

	long m_cref;
	Vector<TsIntProp> m_vtip;
	Vector<TsStrPropImp> m_vtspi;

	void Init(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp);
	bool FindIntProp(int tpt, int * pitip);
	bool FindStrProp(int tpt, int * pitsp);
};


#endif // !TxTextProps_H
