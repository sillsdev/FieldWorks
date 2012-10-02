/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.
File: TsTextProps.cpp
Responsibility: Jeff Gayle
Last reviewed:

	Implementations of ITsTextProps and related interfaces.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	TsTextProps implementation.
	This is an "agile", thread-safe component.
***********************************************************************************************/

static DummyFactory g_fact(_T("SIL.Kernel.TsTextProps"));

/*----------------------------------------------------------------------------------------------
	Static method to check the data (used to create a TsTextProps) to determine if it is
	canonical.
	The integer and string properties are canonical if they are sorted by tpt and there
	are no duplicate tpts.
----------------------------------------------------------------------------------------------*/
bool TsTextProps::IsDataCanonical(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp,
	int ctsp)
{
	AssertArray(prgtip, ctip);
	AssertArray(prgtsp, ctsp);

	// Check the sort order of the integer properties.
	if (ctip > 1)
	{
		const TsIntProp * ptip;
		for (ptip = prgtip + ctip; --ptip > prgtip; )
		{
			if (ptip->m_tpt <= ptip[-1].m_tpt)
				return false;
		}
	}

	// Check the sort order of the string properties.
	if (ctsp > 1)
	{
		const TsStrProp * ptsp;
		for (ptsp = prgtsp + ctsp; --ptsp > prgtsp; )
		{
			if (ptsp->m_tpt <= ptsp[-1].m_tpt)
				return false;
		}
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Static method to make the data (used to create a TsTextProps) canonical.
----------------------------------------------------------------------------------------------*/
void TsTextProps::MakeDataCanonical(TsIntProp * prgtip, int & ctip, TsStrProp * prgtsp,
	int & ctsp)
{
	AssertArray(prgtip, ctip);
	AssertArray(prgtsp, ctsp);

	if (ctip > 1)
	{
		// Sort the integer props.
		int itip1, itip2;
		for (itip1 = 0; itip1 < ctip; itip1++)
		{
			for (itip2 = itip1 + 1; itip2 < ctip; itip2++)
			{
				if (prgtip[itip1].m_tpt > prgtip[itip2].m_tpt)
					continue;
				if (prgtip[itip1].m_tpt < prgtip[itip2].m_tpt)
				{
					SwapBytes(prgtip + itip1, prgtip + itip2, isizeof(TsIntProp));
					continue;
				}
				// Duplicate prop. Delete the second one.
				--ctip;
				MoveItems(prgtip + itip2 + 1, prgtip + itip2, ctip - itip2);
				--itip2;
			}
		}
	}

	if (ctsp > 1)
	{
		// Sort the string props.
		int itsp1, itsp2;
		for (itsp1 = 0; itsp1 < ctsp; itsp1++)
		{
			for (itsp2 = itsp1 + 1; itsp2 < ctsp; itsp2++)
			{
				if (prgtsp[itsp1].m_tpt > prgtsp[itsp2].m_tpt)
					continue;
				if (prgtsp[itsp1].m_tpt < prgtsp[itsp2].m_tpt)
				{
					SwapBytes(prgtsp + itsp1, prgtsp + itsp2, isizeof(TsStrProp));
					continue;
				}
				// Duplicate prop. Delete the second one.
				--ctsp;
				MoveItems(prgtsp + itsp2 + 1, prgtsp + itsp2, ctsp - itsp2);
				--itsp2;
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Static method to create a text props and return it as a TsTextProps (not an interface).
----------------------------------------------------------------------------------------------*/
void TsTextProps::Create(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
	TsTextProps ** ppzttp)
{
	AssertArray(prgtip, ctip);
	AssertArray(prgtsp, ctsp);
	AssertPtr(ppzttp);
	Assert(!*ppzttp);

	Vector<byte> vbT;

	if (!IsDataCanonical(prgtip, ctip, prgtsp, ctsp))
	{
		// Data is not canonical. Copy it and make it canonical.
		int cbTip = ctip * isizeof(TsIntProp);
		int cbTsp = ctsp * isizeof(TsStrProp);

		vbT.Resize(cbTip + cbTsp);

		TsIntProp * prgtipNew = reinterpret_cast<TsIntProp *>(vbT.Begin());
		TsStrProp * prgtspNew = reinterpret_cast<TsStrProp *>(vbT.Begin() + cbTip);
		CopyBytes(prgtip, prgtipNew, cbTip);
		CopyBytes(prgtsp, prgtspNew, cbTsp);
		MakeDataCanonical(prgtipNew, ctip, prgtspNew, ctsp);
		prgtip = prgtipNew;
		prgtsp = prgtspNew;
	}

	CreateCanonical(prgtip, ctip, prgtsp, ctsp, ppzttp);
}

// Create, when caller can guaranteed properties are already canonical.
void TsTextProps::CreateCanonical(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
	TsTextProps ** ppzttp)
{
	TsPropsHolder * ptph = TsPropsHolder::GetPropsHolder();
	ComSmartPtr<TsTextProps> qzttp;

	// Try to find an equivalent property in the global props holder.
	uint uHash = ComputeHashRgb((byte *)prgtip, ctip * isizeof(TsIntProp));
	uHash = ComputeHashRgb((byte *)prgtsp, ctsp * isizeof(TsStrProp), uHash);

	LOCK (ptph->m_mutex)
	{
		if (ptph->Find(prgtip, ctip, prgtsp, ctsp, uHash, &qzttp))
		{
			// We found one that matches.
			*ppzttp = qzttp.Detach();
			return;
		}

		// Otherwise, create a new one and add it to the global props holder.
		qzttp.Attach(NewObjExtra(GetExtraSize(ctip, ctsp)) TsTextProps);

		qzttp->m_ctip = static_cast<byte>(ctip);
		qzttp->m_ctsp = static_cast<byte>(ctsp);
		CopyItems(prgtip, qzttp->Ptip(0), ctip);
		CopyItems(prgtsp, qzttp->Ptsp(0), ctsp);
		qzttp->m_uHash = uHash;

		ptph->Add(qzttp);
	}
#ifdef DEBUG
	qzttp->BuildDebugInfo();
#endif

	*ppzttp = qzttp.Detach();
}


/*----------------------------------------------------------------------------------------------
	Static method to create a text props and return it as an ITsTextProps.
----------------------------------------------------------------------------------------------*/
void TsTextProps::Create(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
	ITsTextProps ** ppttp)
{
	AssertPtrN(ppttp);
	Assert(!*ppttp);

	ComSmartPtr<TsTextProps> qzttp;

	Create(prgtip, ctip, prgtsp, ctsp, &qzttp);
	CheckHr(qzttp->QueryInterface(IID_ITsTextProps, (void **)ppttp));
}


/*----------------------------------------------------------------------------------------------
	Constructor for TsTextProps.
----------------------------------------------------------------------------------------------*/
TsTextProps::TsTextProps(void)
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
#if WIN32
#ifdef DEBUG
	m_dbw1.m_pzttp = this;
	m_dbw2.m_pzttp = this;
#endif //DEBUG
	CoCreateFreeThreadedMarshaler(this, &m_qunkMarshaler);
#endif
}


/*----------------------------------------------------------------------------------------------
	Destructor for TsTextProps.
----------------------------------------------------------------------------------------------*/
TsTextProps::~TsTextProps(void)
{

	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ITsTextProps)
		*ppv = static_cast<ITsTextProps *>(this);
	else if (iid == IID_ITsTextPropsRaw)
		*ppv = static_cast<ITsTextPropsRaw *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ITsTextProps);
//		*ppv = NewObj CSupportErrorInfo(this, IID_ITsTextPropsRaw);
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


/*----------------------------------------------------------------------------------------------
	AddRef.

	If you change the logic of this method make sure you run this test:
	while true; do ./testFwKernel ; done; (for about five mins) or the
	equivalent on windows.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) TsTextProps::AddRef(void)
{
	// We are not doing a Assert(m_cref > 0) here because TsTextProps::Find can
	// call AddRef while another thread is TsTextProps::Release between the InterlockedDecrement(&m_cref)
	// and the LOCK (ptph->m_mutex)

	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release.

	The check is done before the lock for performance reasons rather than
	locking the whole of AddRef and Release methods.
	If you change the logic of this method make sure you run this test:
	while true; do ./testFwKernel ; done; (for about five mins) or the
	equivalent on windows.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) TsTextProps::Release(void)
{
	Assert(m_cref > 0);

	if (InterlockedDecrement(&m_cref) > 0)
		return m_cref;

	TsPropsHolder * ptph = TsPropsHolder::GetPropsHolder();
	AssertPtr(ptph);

	// LOCK is necessary because another thread could call AddRef on this object by using
	// TsPropsHolder::Find method while this method is executing.
	LOCK (ptph->m_mutex)
	{
		// if AddRef has been called by another thread between the InterlockedDecrement
		// and the LOCK then m_cref will not be 0, thus it shouldn't be deleted.
		if (m_cref != 0)
			return m_cref;

		if (m_cref == 0) // safe to remove and delete object
		{
			TsPropsHolder * ptph = TsPropsHolder::GetPropsHolder();
			AssertPtr(ptph);
			ptph->Remove(this);
			m_cref = -9999; // make it clear that this object has been deleted.
			delete this;
		}
	}
	return 0;

}


/*----------------------------------------------------------------------------------------------
	Return the class id of TsPropsFact.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::GetFactoryClsid(CLSID * pclsid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pclsid);

	*pclsid = CLSID_TsPropsFactory;

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}


/*----------------------------------------------------------------------------------------------
	Write the text properties to the given stream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::Serialize(IStream * pstrm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);

	DataWriterStrm dws(pstrm);
	SerializeCore(&dws);

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}


/*----------------------------------------------------------------------------------------------
	Write the text properties to the given byte array. If the data doesn't fit, this sets
	*pcb to the required size and returns S_FALSE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::SerializeRgb(byte * prgb, int cbMax, int * pcb)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgb, cbMax);
	ChkComOutPtr(pcb);

	DataWriterRgb dwr(prgb, cbMax, true /*fIgnoreError*/);
	SerializeCore(&dwr);
	Assert(dwr.IbMax() == dwr.IbCur());
	*pcb = dwr.IbMax();
	return *pcb <= cbMax ? S_OK : S_FALSE;

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}

/*----------------------------------------------------------------------------------------------
	Write a set of text properties to the given byte array, as would be needed for the header
	section of a TsString. If the data doesn't fit, this sets *pcb to the required size
	and returns S_FALSE.

	It is assumed that for any runs for which we want to use duplicated property
	information, the ITsTextProps themselves are redundant in rgpttp.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::SerializeRgPropsRgb(int cpttp, ITsTextProps ** rgpttp, int * rgich,
	BYTE * prgb, int cbMax, int * pcb)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rgpttp, cpttp);
	ChkComArrayArg(rgich, cpttp);
	ChkComArrayArg(prgb, cbMax);
	ChkComOutPtr(pcb);

	Vector<int> vcbOffsets;
	int crun = cpttp;
	vcbOffsets.Resize(crun);

	// To handle the header stuff--run-count and char-min+prop-offset pairs:
	int cbHeader = min(cbMax, isizeof(int) + (isizeof(int) * crun * 2));
	DataWriterRgb dwrHdr(prgb, cbHeader, true /* don't throw errors */);
	dwrHdr.WriteInt(crun);

	byte * pbThisRun = prgb + isizeof(int) // run-count
		+ (isizeof(int) * crun * 2);  // char-min+prop-offset array
	byte * pbRun0 = pbThisRun;
	int cbSoFar = pbRun0 - prgb;
	for (int irun = 0; irun < crun; irun++)
	{
		// Char-min:
		dwrHdr.WriteInt(rgich[irun]);

		// Check for a duplicate set of properties.
		int irunDup;
		for (irunDup = 0; irunDup < irun; irunDup++)
		{
			if (rgpttp[irunDup] == rgpttp[irun])
			{
				// Duplicate; copy previous offset.
				vcbOffsets[irun] = vcbOffsets[irunDup];
				break;
			}
		}
		if (irunDup >= irun) // didn't find duplicate
		{
			int cbSpaceLeft = cbMax - (pbThisRun - prgb);
			int cbNeeded;
			CheckHr(rgpttp[irun]->SerializeRgb(
				((cbSpaceLeft <= 0) ? NULL : pbThisRun),
				max(cbSpaceLeft, 0), &cbNeeded));
			cbSoFar += cbNeeded;
			vcbOffsets[irun] = pbThisRun - pbRun0;
			pbThisRun += cbNeeded;
		}
		dwrHdr.WriteInt(vcbOffsets[irun]);
	}
	*pcb = cbSoFar;

	return (*pcb <= cbMax) ? S_OK : S_FALSE;

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}


/*----------------------------------------------------------------------------------------------
	Write this text properties object to the stream in standard FieldWorks XML format.

	@param pstrm Pointer to the output stream.
	@param pwsf Pointer to an ILgWritingSystemFactory so that we can convert writing system
					integer codes (which are database object ids) to the corresponding strings.
	@param cchIndent Number of spaces to start the indentation.  Zero means no indentation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::WriteAsXml(IStream * pstrm, ILgWritingSystemFactory * pwsf,
	int cchIndent)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);
	ChkComArgPtr(pwsf);

	if (cchIndent < 0)
		cchIndent = 0;		// Ignore negative numbers.
	Vector<char> vchIndent;
	if (cchIndent)
	{
		vchIndent.Resize(cchIndent + 1);
		memset(vchIndent.Begin(), ' ', cchIndent);
	}
	const char * pszIndent = cchIndent ? vchIndent.Begin() : "";

	int ctip;
	CheckHr(get_IntPropCount(&ctip));
	int ctsp;
	CheckHr(get_StrPropCount(&ctsp));

	if (ctip + ctsp > 0)
	{
		FormatToStream(pstrm, "%s<Prop", pszIndent);
		int tpt;
		int nVar, nVal;
		SmartBstr sbstrPropVal;
		SmartBstr sbstrWsStyles;
		SmartBstr sbstrBulNumFontInfo;
		for (int itip = 0; itip < ctip; itip++)
		{
			CheckHr(GetIntProp(itip, &tpt, &nVar, &nVal));
			FwXml::WriteIntTextProp(pstrm, pwsf, tpt, nVar, nVal);
		}
		for (int itsp = 0; itsp < ctsp; itsp++)
		{
			CheckHr(GetStrProp(itsp, &tpt, &sbstrPropVal));
			if (tpt == ktptBulNumFontInfo)
				sbstrBulNumFontInfo = sbstrPropVal;
			else if (tpt == ktptWsStyle)
				sbstrWsStyles = sbstrPropVal;
			else
				FwXml::WriteStrTextProp(pstrm, tpt, sbstrPropVal);
		}
		if (sbstrBulNumFontInfo || sbstrWsStyles)
		{
			FormatToStream(pstrm, ">%n");
			if (sbstrBulNumFontInfo)
			{
				FwXml::WriteBulNumFontInfo(pstrm, sbstrBulNumFontInfo,
					cchIndent ? cchIndent + 2 : 0);
			}
			if (sbstrWsStyles)
			{
				FwXml::WriteWsStyles(pstrm, pwsf, sbstrWsStyles, cchIndent ? cchIndent + 2 : 0);
			}
			FormatToStream(pstrm, "%s</Prop>%n", pszIndent);
		}
		else
		{
			FormatToStream(pstrm, "/>%n");
		}
	}
	else
	{
		// For the C# code to parse style rules properly, we need to show the text property even
		// when it doesn't have any internal properties.
		FormatToStream(pstrm, "%s<Prop/>%n", pszIndent);
	}

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}


/*----------------------------------------------------------------------------------------------
	Permits internal objects to use the SerializeCore method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::SerializeDataWriter(DataWriter * pdwrt)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pdwrt);

	SerializeCore(pdwrt);

	END_COM_METHOD(g_fact, IID_ITsTextPropsRaw);
}

/*----------------------------------------------------------------------------------------------
	Deserialize one text property from a DataReader object.
----------------------------------------------------------------------------------------------*/
void TsTextProps::DeserializeDataReader(DataReader * pdrdr, ITsTextProps ** ppttp)
{
	AssertPtr(pdrdr);
	AssertPtr(ppttp);
	int ctip;
	int ctsp;
	Vector<TsIntProp> vtip;
	Vector<TsStrProp> vtsp;

	// First, read the number of entries for IntProps and StrProps, and decode the counts from
	// this combined value.
	if (pdrdr->IbCur() >= pdrdr->Size())
	{
		// We're trying to read a nonexistent property region.  Ignore it instead of
		// crashing.
		ctip = 0;
		ctsp = 0;
	}
	else
	{
		byte rgbCnt[2];
		pdrdr->ReadBuf(rgbCnt, 2);
		ctip = rgbCnt[0];
		ctsp = rgbCnt[1];
		Assert((int)(byte)ctip == ctip);
		Assert((int)(byte)ctsp == ctsp);
	}
	// Read the TsIntProps.
	// TODO SteveMc(JohnT): figure a way to represent non-standard props and variations,
	// and make both reading and writing handle it.
	if (ctip)
	{
		vtip.Resize(ctip);
		memset(vtip.Begin(), 0, ctip * isizeof(TsIntProp));
		for (int itip = 0; itip < ctip; ++itip)
		{
			TextProps::TextIntProp txip;
			TextProps::ReadTextIntProp(pdrdr, &txip);
			vtip[itip].m_tpt = txip.m_tpt;
			vtip[itip].m_nVal = txip.m_nVal;
			vtip[itip].m_nVar = txip.m_nVar;

			// This is a views based property with only one variant: ktpvEnum.
			if (vtip[itip].m_tpt == ktptBulNumScheme && vtip[itip].m_nVar == ktpvDefault)
				vtip[itip].m_nVar = ktpvEnum;
		}
	}
	// Read the TsStrProps.
	if (ctsp)
	{
		vtsp.Resize(ctsp);
		TsStrProp * ptsp = vtsp.Begin();
		TsStrProp * ptspLim = vtsp.Begin() + ctsp;
		TsStrHolder * ptsh = TsStrHolder::GetStrHolder();
		for ( ; ptsp < ptspLim; ptsp++)
		{
			if (pdrdr->IbCur() == pdrdr->Size())
			{
				// We're trying to read a nonexistent string property.  Ignore it instead of
				// crashing.
				int i = ptsp - vtsp.Begin();
				vtsp.Delete(i);
				--ctsp;
				--ptspLim;
				--ptsp;
				continue;
			}
			TextProps::TextStrProp txsp;
			TextProps::ReadTextStrProp(pdrdr, &txsp);
			ptsp->m_tpt = txsp.m_tpt;
			ptsp->m_hstuVal = ptsh->GetCookieFromString(txsp.m_stuVal);
		}
	}
	Create(vtip.Begin(), ctip, vtsp.Begin(), ctsp, ppttp);
}


/*----------------------------------------------------------------------------------------------
	Write the text properties to the DataWriter object.
----------------------------------------------------------------------------------------------*/
void TsTextProps::SerializeCore(DataWriter * pdwrt)
{
	AssertPtr(pdwrt);

	// Write the combined counts for IntProps, and StrProps as one 16 bit int.
	byte rgbT[2];
	rgbT[0] = m_ctip;
	rgbT[1] = m_ctsp;
	pdwrt->WriteBuf(rgbT, 2);

	// Write the TsIntProps.
	if (m_ctip)
	{
		TsIntProp * ptip;
		TsIntProp * ptipLim;
		for (ptip = Ptip(0), ptipLim = Ptip(m_ctip); ptip < ptipLim; ++ptip)
		{
			TextProps::TextIntProp txip;
			txip.m_scp = TextProps::ConvertTptToScp(ptip->m_tpt);
			txip.m_nVal = ptip->m_nVal;
			txip.m_nVar = ptip->m_nVar;
			TextProps::WriteTextIntProp(pdwrt, &txip);
		}
	}

	// Write the TsStrProps.
	if (m_ctsp)
	{
		TsStrProp * ptsp;
		TsStrProp * ptspLim;
		TsStrHolder * ptsh = TsStrHolder::GetStrHolder();
		for (ptsp = Ptsp(0), ptspLim = Ptsp(m_ctsp); ptsp < ptspLim; ++ptsp)
		{
			TextProps::TextStrProp txsp;
			txsp.m_tpt = ptsp->m_tpt;
			ptsh->GetStringFromCookie(ptsp->m_hstuVal, txsp.m_stuVal);
			TextProps::WriteTextStrProp(pdwrt, &txsp);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Return the number of scalar properties set.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::get_IntPropCount(int * pcv)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcv);

	*pcv = static_cast<int>(m_ctip);

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}


/*----------------------------------------------------------------------------------------------
	Return the indicated scalar property.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::GetIntProp(int iv, int * ptpt, int * pnVar, int * pnVal)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ptpt);
	ChkComOutPtr(pnVar);
	ChkComOutPtr(pnVal);
	if ((uint)iv >= (uint)m_ctip)
		ThrowHr(WarnHr(E_INVALIDARG));

	TsIntProp * ptip = Ptip(iv);
	*ptpt = ptip->m_tpt;
	*pnVar = ptip->m_nVar;
	*pnVal = ptip->m_nVal;

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}


/*----------------------------------------------------------------------------------------------
	Return the values associated with the given scalar property. If the property isn't
	specified in this TsTextProps, this sets *pnVar and *pnVal both to -1 and returns S_FALSE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::GetIntPropValues(int tpt, int * pnVar, int * pnVal)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnVar);
	ChkComOutPtr(pnVal);

	int itip;
	if (FindIntProp(tpt, &itip))
	{
		TsIntProp * ptip = Ptip(itip);
		*pnVar = ptip->m_nVar;
		*pnVal = ptip->m_nVal;
		return S_OK;
	}

	*pnVar = -1;
	*pnVal = -1;
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}


/*----------------------------------------------------------------------------------------------
	Return the number of string valued properties set.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::get_StrPropCount(int * pcv)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcv);

	*pcv = static_cast<int>(m_ctsp);

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}


/*----------------------------------------------------------------------------------------------
	Return the indicated string valued property.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::GetStrProp(int iv, int * ptpt, BSTR * pbstrVal)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ptpt);
	ChkComArgPtr(pbstrVal);
	if ((uint)iv >= (uint)m_ctsp)
		ThrowHr(WarnHr(E_INVALIDARG));

	*pbstrVal = NULL;
	TsStrHolder * ptsh = TsStrHolder::GetStrHolder();
	TsStrProp * ptsp = Ptsp(iv);
	*ptpt = ptsp->m_tpt;
	ptsh->GetBstr(ptsp->m_hstuVal, pbstrVal);

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}

/*----------------------------------------------------------------------------------------------
	Return an actual character pointer. This may not have a long lifetime. It is meant
	for use in the debugger only.
----------------------------------------------------------------------------------------------*/
#ifdef DEBUG
const OLECHAR * TsTextProps::StrPropValue(int tpt)
{
	int itsp;
	if (FindStrProp(tpt, &itsp))
	{
		TsStrHolder * ptsh = TsStrHolder::GetStrHolder();
		TsStrProp * ptsp = Ptsp(itsp);
		return ptsh->GetString(ptsp->m_hstuVal);
	}
	else
		return NULL;
}
#endif //DEBUG


/*----------------------------------------------------------------------------------------------
	Return an actual character pointer of a description of the current properties.
	This is meant for use in the debugger, to be displayed in the Watch window.
----------------------------------------------------------------------------------------------*/
#ifdef DEBUG

StrUni TsTextProps::AddIntPropToDebugInfo(int tpt, int nVar, int nVal)
{
	StrUni stu;
	switch(tpt)
	{
	// Some of the common cases are done nicely.
	case ktptWs:
		stu.Format(L"{ws:%x}", nVal);
		break;
	case ktptItalic:
	case ktptBold:
		{
			StrUni stuTemp;
			switch(nVal)
			{
			case kttvOff:
				stuTemp = L"off";
				break;
			case kttvForceOn:
				stuTemp = L"on";
				break;
			case kttvInvert:
				stuTemp = L"inv";
				break;
			default:
				stuTemp = L"bad";
				break;
			}
			stu.Format(L"{%s:%s}", tpt==ktptItalic ? L"ital" : L"bold", stuTemp.Chars());
			break;
		}
	case ktptSuperscript:
		{
			StrUni stuTemp;
			switch(nVal)
			{
			case kssvOff:
				stuTemp = L"off";
				break;
			case kssvSuper:
				stuTemp = L"sup";
				break;
			case kssvSub:
				stuTemp = L"sub";
				break;
			default:
				stuTemp = L"bad";
				break;
			}
			stu.Format(L"{super:%s}", stuTemp.Chars());
		}
		break;
	case ktptUnderline:
		{
			StrUni stuTemp;
			switch(nVal)
			{
			case kuntNone:
				stuTemp = L"none";
				break;
			case kuntDotted:
				stuTemp = L"dot";
				break;
			case kuntDashed:
				stuTemp = L"dash";
				break;
			case kuntSingle:
				stuTemp = L"sing";
				break;
			case kuntDouble:
				stuTemp = L"doub";
				break;
			case kuntStrikethrough:
				stuTemp = L"strike";
				break;
			case kuntSquiggle:
				stuTemp = L"squiggle";
				break;
			default:
				stuTemp = L"bad";
				break;
			}
			stu.Format(L"{und:%s}", stuTemp.Chars());
		}
		break;
	case ktptFontSize:
		stu.Format(L"{size:%dmp}", nVal);
		break;
	case ktptForeColor:
		stu.Format(L"{fgclr:%x}", nVal);
		break;
	case ktptBackColor:
		stu.Format(L"{bgclr:%x}", nVal);
		break;
	case ktptUnderColor:
		stu.Format(L"{uclr:%x}", nVal);
		break;
	case ktptAlign:
		{
			StrUni stuTemp;
			switch(nVal)
			{
			case ktalLeading:
				stuTemp = L"lead";
				break;
			case ktalLeft:
				stuTemp = L"left";
				break;
			case ktalCenter:
				stuTemp = L"cent";
				break;
			case ktalRight:
				stuTemp = L"right";
				break;
			case ktalTrailing:
				stuTemp = L"trail";
				break;
			case ktalJustify:
				stuTemp = L"just";
				break;
			default:
				stuTemp = L"bad";
				break;
			}
			stu.Format(L"{align:%s}", stuTemp.Chars());
		}
		break;

	case ktptFirstIndent:
		stu.Format(L"{ind1:%dmp}", nVal);
		break;

	case ktptMarginLeading:
		stu.Format(L"{mlead:%dmp}", nVal);
		break;
	case ktptMarginTrailing:
		stu.Format(L"{mtop:%dmp}", nVal);
		break;
	case ktptMarginBottom:
		stu.Format(L"{mb:%dmp}", nVal);
		break;
	case ktptMswMarginTop:
		stu.Format(L"{mtopmsw:%dmp}", nVal);
		break;
	case ktptEditable:
	default:
		stu.Format(L"{%d:%d,%d} ", tpt, nVar, nVal);
		break;
	}
	return stu;
}

// Build a useable description of the props in a member variable for debugging.
void TsTextProps::BuildDebugInfo()
{
	int cv, iv, tpt, nVar, nVal;
	SmartBstr sbstr;

	get_IntPropCount(&cv);
	StrUni stu;
	for (iv = 0; iv < cv; iv++)
	{
		// Enhance JohnT: use prop names.
		GetIntProp(iv, &tpt, &nVar, &nVal);
		stu = AddIntPropToDebugInfo(tpt, nVar, nVal);
		m_stuDebug.Append(stu);
	}

	get_StrPropCount(&cv);
	for (iv = 0; iv < cv; iv++)
	{
		GetStrProp (iv, &tpt, &sbstr);
		switch(tpt)
		{
		case ktptFontFamily:
			stu.Format(L"{ff:%s}", sbstr.Chars());
			break;
		case ktptNamedStyle:
			stu.Format(L"{style:%s}", sbstr.Chars());
			break;
		case ktptObjData:
			{
				if (sbstr.Length() == 0)
				{
					stu = L"empty obj data";
					break;
				}
				switch(sbstr.Chars()[0])
				{
				case kodtOwnNameGuidHot:
				case kodtNameGuidHot:
					{
						if (sbstr.Length() != 9)
						{
							stu = L"Bad guid data";
							break;
						}
						GUID * pguid = (GUID *)(sbstr.Chars() + 1);
						stu.Format(L"hot guid:%g", pguid);
					}
					break;
				case kodtExternalPathName:
					stu.Format(L"link:%s", sbstr.Chars() + 1);
					break;
				case kodtPictOddHot:
				case kodtPictEvenHot:
					stu = L"picture data";
					break;
				default:
					stu.Format(L"{Unknown obj data: (%d?) %s} ",
						sbstr.Chars()[0], sbstr.Chars());
					break;
				}
				break;
			}
		case ktptBulNumFontInfo:
			{
				// Enhance JohnT: make a nice display of this.
				// (However, as it's binary info, inserting the string into our output
				// is not helpful.)
				stu = L"{Bullet and number info}";
				break;
			}
		case ktptWsStyle:
			{
				// A whole collection of font properties about a whole collection
				// of writing systems.
				const OLECHAR * pch = sbstr.Chars();
				const OLECHAR * pchLim = pch + sbstr.Length();
				// The minimum size of a valid field is 4 chars: 2 for ws,
				// a length for the font name, if any; and a number of properties.
				if (pchLim - pch < 4)
				{
					stu = L"invalid WsStyle";
					break;
				}
				stu = L"<WsStyle:";
				while (pch < pchLim)
				{
					int ws = *pch | (*(pch + 1)) << 16;
					stu.FormatAppend(L"<%d:", ws);
					pch += 2;
					int cchFont = *pch;
					const OLECHAR * pchFont = pch + 1;
					// We may have an antique WsStyle with Enc/Ws in two 4-byte ints.
					// In practice, the second int was always zero, which shouldn't occur
					// otherwise: we should have either a font defined or at least one property
					// for each WsStyle!
					if (pch + 2 < pchLim)
					{
						int wsOld = *pch | (*(pch + 1)) << 16;
						if (wsOld == 0 && pch + 4 < pchLim)
						{
							int nT = *(pch + 2) | (*(pch + 3)) << 16;
							if (nT != 0)
							{
								pch += 2;
								cchFont = *pch;
								pchFont += 2;
							}
						}
					}
					if (pch + cchFont >= pchLim)
					{
						stu.Append(L"..bad_cchFont>");
						break;
					}
					stu.Append(pchFont, cchFont);
					pch = pchFont + cchFont;
					if (pch >= pchLim)
					{
						stu.Append(L"..bad>");
						break;
					}
					int cprop = SignedInt(*pch++);
					if (abs(cprop) > 12)
					{
						stu.FormatAppend(L"..bad_propcount[%d]>", cprop);
						break;
					}
					if (cprop < 0)
					{
						// String properties.
						for (; cprop < 0; cprop++)
						{
							if (pch >= pchLim)
								break;
							int tpt = *pch++;
							int cch = *pch++;
							StrUni stuP(pch, cch);
							stu.FormatAppend(L"{%d:%s}", tpt, stuP.Chars());
							pch += cch;
						}
						if (pch >= pchLim)
						{
							stu.Append(L"..bad>");
							break;
						}
						cprop = *pch++;
						if (cprop > 12)
						{
							stu.FormatAppend(L"..bad_propcount[%d]>", cprop);
							break;
						}
					}
					// Integer properties.
					for (; --cprop >= 0; )
					{
						if (pch >= pchLim)
						{
							++pch;	// ensure greater than.
							break;
						}
						int tpt = *pch++;
						int ttv = *pch++;
						int nVal = *pch | (*(pch + 1)) << 16;
						pch += 2;
						StrUni stuT = AddIntPropToDebugInfo(tpt, ttv, nVal);
						stu.Append(stuT.Chars());
					}
					if (pch > pchLim)
					{
						stu.Append(L"..bad>");
						break;
					}
					stu.Append(L">"); // mark end of ws
				}
				stu.Append(L">"); // mark end of WsStyle info
			}
			break;
		default:
			stu.Format(L"{%d:%s} ", tpt, sbstr.Chars());
			break;
		}
		m_stuDebug.Append(stu);
	}
}

#if WIN32 // TODO-Linux: port to get debugging information.
OLECHAR * TsTextProps::Dbw1::Watch()
{
	static wchar wcsTmp[161];
	int cv, iv, tpt, nVar, nVal;
	SmartBstr sbstr;

	m_nSerial++;
	int len = swprintf_s(wcsTmp, SizeOfArray(wcsTmp), L"#%d ", m_nSerial);

	m_pzttp->get_IntPropCount(&cv);
	len += swprintf_s(wcsTmp + len, SizeOfArray(wcsTmp) - len, L"%d Int ", cv);
	for (iv = 0; iv < cv; iv++)
	{
		m_pzttp->GetIntProp(iv, &tpt, &nVar, &nVal);
		len += swprintf_s(wcsTmp + len, SizeOfArray(wcsTmp) - len, L"{%d:%d,%d} ", tpt, nVar, nVal);
		if (len > SizeOfArray(wcsTmp) - 6)
			goto Full;
	}

	m_pzttp->get_StrPropCount(&cv);
	len += swprintf_s(wcsTmp + len, SizeOfArray(wcsTmp) - len, L"%d Str ", cv);
	if (len > SizeOfArray(wcsTmp) - 6)
		goto Full;
	for (iv = 0; iv < cv; iv++)
	{
		m_pzttp->GetStrProp (iv, &tpt, &sbstr);
		len += swprintf_s(wcsTmp + len, SizeOfArray(wcsTmp) - len, L"{%d:%ld} ", tpt, (long)sbstr.Length());
		if (len > SizeOfArray(wcsTmp) - 6)
			goto Full;
	}

	return wcsTmp;
Full:
	wcscpy_s (wcsTmp + len, SizeOfArray(wcsTmp) - len, L" ...");
	return wcsTmp;
}
#endif
#endif //DEBUG


/*----------------------------------------------------------------------------------------------
	Display this TsTextProps in the Debugger Output window.
	This is meant for use in the debugger.
----------------------------------------------------------------------------------------------*/
#ifdef DEBUG
#if WIN32 // TODO-Linux: port to get debugging information.
OLECHAR * TsTextProps::Dbw2::Watch()
{
	int cv, iv, tpt, nVar, nVal;
	SmartBstr sbstr;

	m_nSerial++;
	Output("Begin ------ #%d TsTextProps --------\n", m_nSerial);

	m_pzttp->get_IntPropCount(&cv);
	Output("%d Int: tpt nVar nVal\n", cv);
	for (iv = 0; iv < cv; iv++)
	{
		m_pzttp->GetIntProp(iv, &tpt, &nVar, &nVal);
		Output("  %d %d %d\n", tpt, nVar, nVal);
	}

	m_pzttp->get_StrPropCount(&cv);
	Output("%d Str: tpt Length: data\n", cv);
	for (iv = 0; iv < cv; iv++)
	{
		int i;
		m_pzttp->GetStrProp(iv, &tpt, &sbstr);

		Output("  %d %ld: \"", tpt, (long)sbstr.Length());
		for (i = 0; i < sbstr.Length(); i++)
			Output("%lc", sbstr[i]);
		Output("\"\n");

		Output("  %d %ld:", tpt, (long)sbstr.Length());
		for (i = 0; i < sbstr.Length(); i++)
			Output(" %04X", sbstr[i]);
		Output("\n");
	}

	Output("End -------- #%d TsTextProps --------\n", m_nSerial);
	return L"See Debugger Output window";
}
#endif
#endif //DEBUG

HRESULT TsTextProps::GetStrPropValueInternal(int tpt, BSTR * pbstrVal)
{
	*pbstrVal = NULL;

	int itsp;
	if (FindStrProp(tpt, &itsp))
	{
		TsStrHolder * ptsh = TsStrHolder::GetStrHolder();
		TsStrProp * ptsp = Ptsp(itsp);
		ptsh->GetBstr(ptsp->m_hstuVal, pbstrVal);
		return S_OK;
	}
	return S_FALSE;
}

/*----------------------------------------------------------------------------------------------
	Return the values associated with the given string property. If the property isn't
	specified in this TsTextProps, this sets *pnVar to -1 and *pbstrVal to NULL and returns
	S_FALSE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::GetStrPropValue(int tpt, BSTR * pbstrVal)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrVal);

	return GetStrPropValueInternal(tpt, pbstrVal);

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}

/*----------------------------------------------------------------------------------------------
	Return a properties builder initialized to be the same as this TextProps.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsTextProps::GetBldr(ITsPropsBldr ** pptpb)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptpb);

	TsPropsBldr::Create(Ptip(0), m_ctip, Ptsp(0), m_ctsp, pptpb);

	END_COM_METHOD(g_fact, IID_ITsTextProps);
}


/*----------------------------------------------------------------------------------------------
	Look in the array of Int Properties for the given FwTextPropType. Set *pitip to the index
	where it was found and return true or set *pitip to where it would be inserted and return
	false.
----------------------------------------------------------------------------------------------*/
bool TsTextProps::FindIntProp(int tpt, int * pitip)
{
	AssertPtrN(pitip);
	int itipMin = 0;
	int itipLim = (int)m_ctip;

	// Perform a binary search
	while (itipMin < itipLim)
	{
		int itipT = (itipMin + itipLim) >> 1;
		if (Ptip(itipT)->m_tpt < tpt)
			itipMin = itipT + 1;
		else
			itipLim = itipT;
	}
	if (pitip)
		*pitip = itipMin;

	return itipMin < (int)m_ctip && Ptip(itipMin)->m_tpt == tpt;
}


/*----------------------------------------------------------------------------------------------
	Look in the array of Str Properties for the given FwTextPropType. Set *pitsp to the index
	where it was found and return true or set *pitsp to where it would be inserted and return
	false.
----------------------------------------------------------------------------------------------*/
bool TsTextProps::FindStrProp(int tpt, int * pitsp)
{
	AssertPtrN(pitsp);
	int itspMin = 0;
	int itspLim = (int)m_ctsp;

	// Perform a binary search
	while (itspMin < itspLim)
	{
		int itspT = (itspMin + itspLim) >> 1;
		if (Ptsp(itspT)->m_tpt < tpt)
			itspMin = itspT + 1;
		else
			itspLim = itspT;
	}
	if (pitsp)
		*pitsp = itspMin;

	return itspMin < (int)m_ctsp && Ptsp(itspMin)->m_tpt == tpt;
}


/***********************************************************************************************
	Text Properties Holder class.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	Get the global property pool.
----------------------------------------------------------------------------------------------*/
TsPropsHolder * TsPropsHolder::GetPropsHolder(void)
{
	return KernelGlobals::g_tph;
}


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TsPropsHolder::TsPropsHolder(void)
{
	Assert(!m_prgpzttpHash);
	Assert(!m_cpzttpHash);
	Assert(!m_cpzttp);
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TsPropsHolder::~TsPropsHolder(void)
{
	if (m_prgpzttpHash)
	{
		// m_prgpzttpHash is not an object. It was allocated malloc.
		free(m_prgpzttpHash);
		m_prgpzttpHash = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Search for a TsTextProps with the given data.
	Increases refcount on the returned TsTextProps.
----------------------------------------------------------------------------------------------*/
bool TsPropsHolder::Find(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
	uint uHash, TsTextProps ** ppzttpRet)
{
	AssertArray(prgtip, ctip);
	AssertArray(prgtsp, ctsp);
	AssertPtr(ppzttpRet);

	LOCK (m_mutex)
	{
		if (!m_cpzttpHash)
			return false;

		TsTextProps * pzttp;
		TsTextProps ** ppzttpHead = &m_prgpzttpHash[uHash % m_cpzttpHash];
		TsTextProps ** ppzttp;

		for (ppzttp = ppzttpHead; (pzttp = *ppzttp) != NULL; ppzttp = &pzttp->m_pzttpNext)
		{
			if (uHash == pzttp->m_uHash &&
				ctip == pzttp->m_ctip && ctsp == pzttp->m_ctsp &&
				0 == memcmp(prgtip, pzttp->Ptip(0), ctip * isizeof(TsIntProp)) &&
				0 == memcmp(prgtsp, pzttp->Ptsp(0), ctsp * isizeof(TsStrProp)))
			{
				*ppzttpRet = pzttp;
				AddRefObj(*ppzttpRet);
				if (ppzttp != ppzttpHead)
				{
					// Move to the head of the chain.
					*ppzttp = pzttp->m_pzttpNext;
					pzttp->m_pzttpNext = *ppzttpHead;
					*ppzttpHead = pzttp;
				}
				return true;
			}
		}
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Add a TsTextProps to the hash table.
----------------------------------------------------------------------------------------------*/
void TsPropsHolder::Add(TsTextProps * pzttp)
{
	AssertPtr(pzttp);
	Assert(!pzttp->m_pzttpNext);

	LOCK (m_mutex)
	{
		if (m_cpzttp >= 4 * m_cpzttpHash)
			Rehash();

		int ipzttp = pzttp->m_uHash % m_cpzttpHash;
		pzttp->m_pzttpNext = m_prgpzttpHash[ipzttp];
		m_prgpzttpHash[ipzttp] = pzttp;
		m_cpzttp++;
		Assert(m_cpzttp > 0);
	}
}


/*----------------------------------------------------------------------------------------------
	Remove a TsTextProps from the hash table.
----------------------------------------------------------------------------------------------*/
void TsPropsHolder::Remove(TsTextProps * pzttp)
{
	AssertPtr(pzttp);

	LOCK (m_mutex)
	{
		// This method is vulnerable to the order of deletion of global objects.
		// If the global object (g_tph) has aready been deleted this method will
		// seg fault. The order of deleting is influenced by order object files are
		// linked and possibly platform differences.
		if (!m_prgpzttpHash)
			return;

		if (!m_cpzttpHash)
		{
			Assert(false);
			Warn("Removing from an empty hash table.");
			return;
		}

		if(pzttp->m_cref != 0)
			return; // TsPropsHolder::Find could have been running when ::Remove was called.

		TsTextProps ** ppzttp = &m_prgpzttpHash[pzttp->m_uHash % m_cpzttpHash];

		for ( ; *ppzttp; ppzttp = &(*ppzttp)->m_pzttpNext)
		{
			if (pzttp == *ppzttp)
			{
				*ppzttp = pzttp->m_pzttpNext;
				pzttp->m_pzttpNext = NULL;
				m_cpzttp--;
				Assert(m_cpzttp >= 0);
				return;
			}
		}
		Assert(false);
		Warn("Removing from an empty hash table.");
	}
}


/*----------------------------------------------------------------------------------------------
	Resize the hash table so it has more buckets.
----------------------------------------------------------------------------------------------*/
void TsPropsHolder::Rehash(void)
{
	LOCK (m_mutex)
	{
		// Need to grow the number of hash buckets.
		int cpzttpNew = GetPrimeNear(Max(2 * (m_cpzttp + 1), 10));
		if (cpzttpNew <= m_cpzttpHash)
			return;

		TsTextProps ** prgpzttpNew = (TsTextProps **)calloc(cpzttpNew, isizeof(TsTextProps *));
		if (!prgpzttpNew)
			ThrowHr(WarnHr(E_OUTOFMEMORY));

		TsTextProps * pzttp;
		TsTextProps * pzttpNext;
		int ipzttp;

		for (ipzttp = 0; ipzttp < m_cpzttpHash; ipzttp++)
		{
			for (pzttp = m_prgpzttpHash[ipzttp]; pzttp; pzttp = pzttpNext)
			{
				pzttpNext = pzttp->m_pzttpNext;

				int ipzttpNew = pzttp->m_uHash % cpzttpNew;
				pzttp->m_pzttpNext = prgpzttpNew[ipzttpNew];
				prgpzttpNew[ipzttpNew] = pzttp;
			}
		}

		if (m_prgpzttpHash)
			free(m_prgpzttpHash);

		m_prgpzttpHash = prgpzttpNew;
		m_cpzttpHash = cpzttpNew;
	}
}


/***********************************************************************************************
	String Holder class.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Get the global string holder.
----------------------------------------------------------------------------------------------*/
TsStrHolder * TsStrHolder::GetStrHolder(void)
{
	return KernelGlobals::g_tsh;
}


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TsStrHolder::TsStrHolder(void)
{
	// Buckets will be associated when the first item is added.
	Assert(!m_visheHash.Size());
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TsStrHolder::~TsStrHolder(void)
{
}


/*----------------------------------------------------------------------------------------------
	If the string is already in the holder, return its cookie. Otherwise add it to the pool and
	return its cookie.
----------------------------------------------------------------------------------------------*/
int TsStrHolder::GetCookieFromString(StrUni & stu)
{
	if (!stu.Length())
	{
		return 0;	// REVIEW ShonK(JeffG): assign zero, I think.
	}

	int ishe;
	uint uHash = ComputeHashRgb((byte *)stu.Chars(), stu.Length() * isizeof(wchar));

	LOCK (m_mutex)
	{
		if (!Find(stu, uHash, &ishe))
			Add(stu, uHash, &ishe);
	}

	return CookieFromIndex(ishe);
}


/*----------------------------------------------------------------------------------------------
	Get the string for the given cookie.
----------------------------------------------------------------------------------------------*/
void TsStrHolder::GetStringFromCookie(int hstu, StrUni & stu)
{
	Assert(ValidCookie(hstu));
	if (!hstu)
		stu.Clear();
	else
	{
		int ishe = IndexFromCookie(hstu);
		LOCK (m_mutex)
			stu = m_vshe[ishe].m_stu;
	}
}


/*----------------------------------------------------------------------------------------------
	Get a BSTR from the given cookie. The caller is responsible for freeing the BSTR.
----------------------------------------------------------------------------------------------*/
void TsStrHolder::GetBstr(int hstu, BSTR * pbstr)
{
	Assert(ValidCookie(hstu));
	AssertPtr(pbstr);
	Assert(!*pbstr);

	if (hstu)
	{
		int ishe = IndexFromCookie(hstu);
		LOCK (m_mutex)
			m_vshe[ishe].m_stu.GetBstr(pbstr);
	}
}

#ifdef DEBUG
const OLECHAR * TsStrHolder::GetString(int hstu)
{
	if (hstu)
	{
		int ishe = IndexFromCookie(hstu);
		const OLECHAR* str;
		LOCK (m_mutex)
			str = m_vshe[ishe].m_stu.Chars();
		return str;
	}
	else
		return NULL;
}
#endif
/*----------------------------------------------------------------------------------------------
	Look in the hash table for the given string.
----------------------------------------------------------------------------------------------*/
bool TsStrHolder::Find(StrUni & stu, uint uHash, int * pisheRet)
{
	AssertPtr(pisheRet);

	LOCK (m_mutex)
	{
		if (!m_visheHash.Size())
			return false;

		StrHolderEntry * pshe;
		int * pishe;
		int * pisheHead = &m_visheHash[uHash % m_visheHash.Size()];

		for (pishe = pisheHead; *pishe != -1; pishe = &m_vshe[*pishe].m_isheNext)
		{
			pshe = &m_vshe[*pishe];
			if (uHash == pshe->m_uHash && stu == pshe->m_stu)
			{
				*pisheRet = *pishe;
				if (pishe != pisheHead)
				{
					// Move to the head of the chain.
					*pishe = pshe->m_isheNext;
					pshe->m_isheNext = *pisheHead;
					*pisheHead = *pisheRet;
				}
				return true;
			}
		}
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Add the string to the hash table.
----------------------------------------------------------------------------------------------*/
void TsStrHolder::Add(StrUni & stu, uint uHash, int * pishe)
{
	LOCK (m_mutex)
	{
		int ishe = m_vshe.Size();

		if ((ishe + 1) >= 4 * m_visheHash.Size())
			Rehash();

		StrHolderEntry she;
		int iishe = uHash % m_visheHash.Size();

		she.m_stu = stu;
		she.m_uHash = uHash;
		she.m_isheNext = m_visheHash[iishe];

		m_vshe.Push(she);
		Assert(m_vshe.Size() == ishe + 1);

		m_visheHash[iishe] = ishe;
		*pishe = ishe;
	}
}


/*----------------------------------------------------------------------------------------------
	Resize the hash table so it has more buckets.
----------------------------------------------------------------------------------------------*/
void TsStrHolder::Rehash(void)
{
	LOCK (m_mutex)
	{
		// Need to grow the number of hash buckets.
		int cishe = GetPrimeNear(Max(2 * (m_vshe.Size() + 1), 10));
		if (cishe <= m_visheHash.Size())
			return;

		m_visheHash.Resize(cishe);

		FillInts(m_visheHash.Begin(), -1, cishe);

		int ishe;
		int iishe;
		StrHolderEntry * pshe;

		for (ishe = 0, pshe = m_vshe.Begin(); ishe < m_vshe.Size(); ishe++, pshe++)
		{
			iishe = pshe->m_uHash % cishe;
			pshe->m_isheNext = m_visheHash[iishe];
			m_visheHash[iishe] = ishe;
		}
	}
}


/***********************************************************************************************
	Property builder.
	This is a "Both" threading model component that is NOT thread-safe.
***********************************************************************************************/


// The class factory for TsPropsBldr.
static GenericFactory g_factPropsBldr(
	_T("FieldWorks.TsPropsBldr"),
	&CLSID_TsPropsBldr,
	_T("FieldWorks Text Properties Builder"),
	_T("Both"),
	&TsPropsBldr::CreateCom);


/*----------------------------------------------------------------------------------------------
	Static method called by the class factory to create a string builder.
----------------------------------------------------------------------------------------------*/
void TsPropsBldr::CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkOuter)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<TsPropsBldr> qztpb;

	qztpb.Attach(NewObj TsPropsBldr);
	qztpb->Init(NULL, 0, NULL, 0);
	CheckHr(qztpb->QueryInterface(iid, ppv));
}


/*----------------------------------------------------------------------------------------------
	Static method to create a property builder with the given initial state.
----------------------------------------------------------------------------------------------*/
void TsPropsBldr::Create(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
	TsPropsBldr ** ppztpb)
{
	AssertArray(prgtip, ctip);
	AssertArray(prgtsp, ctsp);
	AssertPtr(ppztpb);
	Assert(!*ppztpb);

	ComSmartPtr<TsPropsBldr> qztpb;

	qztpb.Attach(NewObj TsPropsBldr);
	qztpb->Init(prgtip, ctip, prgtsp, ctsp);
	*ppztpb = qztpb.Detach();
}


/*----------------------------------------------------------------------------------------------
	Static method to create a property builder with the given initial state.
----------------------------------------------------------------------------------------------*/
void TsPropsBldr::Create(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp,
	ITsPropsBldr ** pptpb)
{
	AssertArray(prgtip, ctip);
	AssertArray(prgtsp, ctsp);
	AssertPtr(pptpb);
	Assert(!*pptpb);

	ComSmartPtr<TsPropsBldr> qztpb;

	qztpb.Attach(NewObj TsPropsBldr);
	qztpb->Init(prgtip, ctip, prgtsp, ctsp);
	CheckHr(qztpb->QueryInterface(IID_ITsPropsBldr, (void **)pptpb));
}


/*----------------------------------------------------------------------------------------------
	Constructor for TsPropsBldr.
----------------------------------------------------------------------------------------------*/
TsPropsBldr::TsPropsBldr(void)
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}


/*----------------------------------------------------------------------------------------------
	Destructor for TsPropsBldr.
----------------------------------------------------------------------------------------------*/
TsPropsBldr::~TsPropsBldr(void)
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Init function.
----------------------------------------------------------------------------------------------*/
void TsPropsBldr::Init(const TsIntProp * prgtip, int ctip, const TsStrProp * prgtsp, int ctsp)
{
	AssertArray(prgtip, ctip);
	AssertArray(prgtsp, ctsp);

	// Copy the TsIntProps array.
	bool fFoundProp;
	int	iv;

	if (ctip)
	{
		m_vtip.EnsureSpace(ctip);

		for (iv = 0; iv < ctip; iv++)
		{
			int itip;

			// Sort by finding what index to insert at.
			fFoundProp = FindIntProp(prgtip[iv].m_tpt, &itip);
			Assert(fFoundProp == FALSE);
			m_vtip.Insert(itip, prgtip[iv]);
		}
	}

	// Copy the TsStrProp to TsStrPropImp.
	if (ctsp)
	{
		TsStrHolder * ptsh = TsStrHolder::GetStrHolder();

		m_vtspi.EnsureSpace(ctsp);

		for (iv = 0; iv < ctsp; iv++)
		{
			int itsp;
			TsStrPropImp tspiT;

			// Sort by finding what index to insert at.
			fFoundProp = FindStrProp(prgtsp[iv].m_tpt, &itsp);
			Assert(!fFoundProp);
			tspiT.m_tpt = prgtsp[iv].m_tpt;
			if (prgtsp[iv].m_hstuVal)
				ptsh->GetStringFromCookie(prgtsp[iv].m_hstuVal, tspiT.m_stuVal);
			m_vtspi.Insert(itsp, tspiT);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ITsPropsBldr)
		*ppv = static_cast<ITsPropsBldr *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ITsPropsBldr);
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
STDMETHODIMP_(UCOMINT32) TsPropsBldr::AddRef(void)
{
	Assert(m_cref > 0);
	return InterlockedIncrement(&m_cref);
}


/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) TsPropsBldr::Release(void)
{
	Assert(m_cref > 0);
	if (InterlockedDecrement(&m_cref) > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Return the number of scalar properties set.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::get_IntPropCount(int * pcv)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcv);

	*pcv = m_vtip.Size();

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}


/*----------------------------------------------------------------------------------------------
	Look in the vector of Int Properties for the given FwTextPropType. Set *pitip to the index
	where it was found and return true or set *pitip to where it would be inserted and return
	false.
 ----------------------------------------------------------------------------------------------*/
bool TsPropsBldr::FindIntProp(int tpt, int * pitip)
{
	AssertPtrN(pitip);
	int itipMin = 0;
	int itipLim = m_vtip.Size();

	// Perform a binary search
	while (itipMin < itipLim)
	{
		int itipT = (itipMin + itipLim) >> 1;
		if (m_vtip[itipT].m_tpt < tpt)
			itipMin = itipT + 1;
		else
			itipLim = itipT;
	}
	if (pitip)
		*pitip = itipMin;

	return itipMin < m_vtip.Size() && m_vtip[itipMin].m_tpt == tpt;
}


/*----------------------------------------------------------------------------------------------
	Look in the vector of String Properties for the given FwTextPropType.Set *pitsp to the index
	where it was found and return true or set *pitsp to where it would be inserted and return
	false.
----------------------------------------------------------------------------------------------*/
bool TsPropsBldr::FindStrProp(int tpt, int * pitsp)
{
	AssertPtrN(pitsp);
	int itspMin = 0;
	int itspLim = m_vtspi.Size();

	// Perform a binary search
	while (itspMin < itspLim)
	{
		int itspT = (itspMin + itspLim) >> 1;
		if (m_vtspi[itspT].m_tpt < tpt)
			itspMin = itspT + 1;
		else
			itspLim = itspT;
	}
	if (pitsp)
		*pitsp = itspMin;

	return itspMin < m_vtspi.Size() && m_vtspi[itspMin].m_tpt == tpt;
}


/*----------------------------------------------------------------------------------------------
	Return the indicated scalar property.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::GetIntProp(int iv, int * ptpt, int * pnVar, int * pnVal)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ptpt);
	ChkComOutPtr(pnVar);
	ChkComOutPtr(pnVal);
	if ((uint)iv >= (uint)m_vtip.Size())
		ThrowHr(WarnHr(E_INVALIDARG));

	*ptpt = m_vtip[iv].m_tpt;
	*pnVar = m_vtip[iv].m_nVar;
	*pnVal = m_vtip[iv].m_nVal;

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}


/*----------------------------------------------------------------------------------------------
	Return the values associated with the given scalar property. If the property isn't
	specified in this TsTextProps, this sets *pnVar and *pnVal both to -1 and returns
	S_FALSE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::GetIntPropValues(int tpt, int * pnVar, int * pnVal)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnVar);
	ChkComOutPtr(pnVal);

	int itip;
	if (FindIntProp(tpt, &itip))
	{
		*pnVar = m_vtip[itip].m_nVar;
		*pnVal = m_vtip[itip].m_nVal;
		return S_OK;
	}
	*pnVar = -1;
	*pnVal = -1;
	return S_FALSE;

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}


/*----------------------------------------------------------------------------------------------
	Return the number of string valued properties set.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::get_StrPropCount(int * pcv)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcv);

	*pcv = m_vtspi.Size();

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}


/*----------------------------------------------------------------------------------------------
	Return the indicated string valued property.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::GetStrProp(int iv, int * ptpt, BSTR * pbstrVal)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ptpt);
	ChkComArgPtr(pbstrVal);
	if ((uint)iv >= (uint)m_vtspi.Size())
		ThrowHr(WarnHr(E_INVALIDARG));

	*pbstrVal = NULL;
	*ptpt = m_vtspi[iv].m_tpt;
	m_vtspi[iv].m_stuVal.GetBstr(pbstrVal);

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}


/*----------------------------------------------------------------------------------------------
	Return the values associated with the given string property. If the property isn't
	specified in this TsTextProps, this sets *pnVar to -1 and *pbstrVal to NULL and returns
	S_FALSE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::GetStrPropValue(int tpt, BSTR * pbstrVal)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrVal);

	*pbstrVal = NULL;
	int itsp;
	if (FindStrProp(tpt, &itsp))
	{
		m_vtspi[itsp].m_stuVal.GetBstr(pbstrVal);
		return S_OK;
	}
	return S_FALSE;

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}


/*----------------------------------------------------------------------------------------------
	Set the values for the indicated scalar property. If nVar and nVal are both -1, the
	property is deleted.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::SetIntPropValues(int tpt, int nVar, int nVal)
{
	BEGIN_COM_METHOD;

	int itip;
	bool fDelete = nVar == -1 && nVal == -1;

	if (!fDelete && ktptWs == tpt && (uint)nVal > kwsLim)
		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");

	Assert(tpt != ktptWs || nVal != 0);

	if (FindIntProp(tpt, &itip))
	{
		if (fDelete)
			m_vtip.Delete(itip);
		else
		{
			m_vtip[itip].m_nVar = nVar;
			m_vtip[itip].m_nVal = nVal;
		}
	}
	else if (!fDelete)
	{
		TsIntProp tip;

		tip.m_tpt = tpt;
		tip.m_nVar = nVar;
		tip.m_nVal = nVal;

		// Insert the new int prop in the proper place to keep list sorted.
		m_vtip.Insert(itip, tip);
	}

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}


/*----------------------------------------------------------------------------------------------
	Set the values for the indicated string property. If bstrVal is NULL, the property is
	deleted.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::SetStrPropValue(int tpt, BSTR bstrVal)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrVal);

	SetStrPropValueRgch(tpt, (const byte*)bstrVal, BstrLen(bstrVal) * isizeof(OLECHAR));

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}

/*----------------------------------------------------------------------------------------------
	Set the values for the indicated string property. If rgchVal is NULL and nValLength is 0,
	the property is deleted.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::SetStrPropValueRgch(int tpt, const byte* rgchVal, int nValLength)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rgchVal, nValLength);

	int itsp;
	StrUni stu;
	stu.Assign((const OLECHAR*)rgchVal, nValLength / isizeof(OLECHAR));
	bool fDelete = !nValLength && !rgchVal;
	if (FindStrProp(tpt, &itsp))
	{
		if (fDelete)
			m_vtspi.Delete(itsp);
		else
			m_vtspi[itsp].m_stuVal = stu;
	}
	else if (!fDelete)
	{
		TsStrPropImp tspi;
		tspi.m_tpt = tpt;
		tspi.m_stuVal = stu;
		// Insert the new int prop in the proper place to keep list sorted.
		m_vtspi.Insert(itsp, tspi);
	}

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}

/*----------------------------------------------------------------------------------------------
	Get an ITsTextProps from the current state.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::GetTextProps(ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppttp);

	if (!m_vtspi.Size())
	{
		TsTextProps::Create(m_vtip.Begin(), m_vtip.Size(), NULL, 0, ppttp);
		return S_OK;
	}

	Vector<TsStrProp> vtsp;
	TsStrHolder * ptsh = TsStrHolder::GetStrHolder();

	vtsp.Resize(m_vtspi.Size());

	// Copy the TsStrPropImp to TsStrProp
	for (int itspi = 0; itspi < m_vtspi.Size(); itspi++)
	{
		TsStrPropImp & tspi = m_vtspi[itspi];
		TsStrProp & tsp = vtsp[itspi];

		tsp.m_tpt = tspi.m_tpt;
		tsp.m_hstuVal = ptsh->GetCookieFromString(tspi.m_stuVal);
	}

	ComSmartPtr<TsTextProps> qzttp;
	TsTextProps::CreateCanonical(m_vtip.Begin(), m_vtip.Size(), vtsp.Begin(), vtsp.Size(), &qzttp);
	*ppttp = qzttp.Detach();

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}

/*----------------------------------------------------------------------------------------------
	Clear completely for a new set of props.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsBldr::Clear()
{
	BEGIN_COM_METHOD;

	m_vtspi.Clear();
	m_vtip.Clear();

	END_COM_METHOD(g_factPropsBldr, IID_ITsPropsBldr);
}
