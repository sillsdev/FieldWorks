/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TsPropsFactory.cpp
Responsibility: Jeff Gayle
Last reviewed: 8/25/99

	Implementation of ITsPropsFactory.

	This is a thread-safe, "agile" component.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE


// There's a single global instance of the ITsPropsFactory.
TsPropsFact TsPropsFact::g_tpf;


// The class factory for TsPropsFact.
static GenericFactory g_factPropsFact(
	_T("FieldWorks.TsPropsFactory"),
	&CLSID_TsPropsFactory,
	_T("FieldWorks Text Properties Factory"),
	_T("Both"),
	&TsPropsFact::CreateCom);


/*----------------------------------------------------------------------------------------------
	Called by the GenericFactory to "create" an ITsPropsFactory. It just returns the global
	one.
----------------------------------------------------------------------------------------------*/
void TsPropsFact::CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkOuter)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	CheckHr(g_tpf.QueryInterface(iid, ppv));
}


/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsFact::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ITsPropsFactory)
		*ppv = static_cast<ITsPropsFactory *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ITsPropsFactory);
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
	Since this is a singleton, just addref the module.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) TsPropsFact::AddRef(void)
{
	return ModuleEntry::ModuleAddRef();
}


/*----------------------------------------------------------------------------------------------
	Since this is a singleton, just release the module.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) TsPropsFact::Release(void)
{
	return ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Read an ITsTextProps from the stream and return it.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsFact::DeserializeProps(IStream * pstrm, ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);
	ChkComOutPtr(ppttp);

	DataReaderStrm drs(pstrm);
	TsTextProps::DeserializeDataReader(&drs, ppttp);

	END_COM_METHOD(g_factPropsFact, IID_ITsPropsFactory);
}


/*----------------------------------------------------------------------------------------------
	Read an ITsTextProps from a byte array and return it.  Return S_FALSE if the array is not
	totally consumed, but an ITsTextProps is successfully read.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsFact::DeserializePropsRgb(const byte * prgb, int * pcb,
	ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pcb);
	ChkComOutPtr(ppttp);
	ChkComArrayArg(prgb, *pcb);

	DataReaderRgb drr(prgb, *pcb);
	TsTextProps::DeserializeDataReader(&drr, ppttp);
	return drr.IbCur() < drr.Size() ? S_FALSE : S_OK;

	END_COM_METHOD(g_factPropsFact, IID_ITsPropsFactory);
}

/*----------------------------------------------------------------------------------------------
	Read an array of ITsTextProps from a byte array and return it. If cpttpMax == 0, return
	in cpttpRet the number of items present. Return S_FALSE if the array is not totally
	consumed, but at least one ITsTextProps is successfully read.

	For runs that have duplicate properties (ie, where the offsets into the properties stuff
	are identical), returns duplicate pointers in rgpttp.

	Also return the character counts for each run, so that we can reconstruct the
	run information later on.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsFact::DeserializeRgPropsRgb(int cpttpMax, const BYTE * prgb, int * pcb,
		int * pcpttpRet, ITsTextProps ** rgpttp, int * rgich)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pcb);
	ChkComOutPtr(pcpttpRet);
	ChkComArrayArg(prgb, *pcb);
	ChkComArrayArg(rgpttp, cpttpMax);
	ChkComArrayArg(rgich, cpttpMax);

	DataReaderRgb drrHdr(prgb, *pcb);
	int crun;
	drrHdr.ReadInt(&crun);
	if (cpttpMax == 0)
	{
		*pcpttpRet = crun;
	}
	else
	{
		Vector<int> vcbOffsets;
		vcbOffsets.Resize(crun);

		const byte * pbRun0 = prgb + isizeof(int) + (crun * isizeof(int) * 2);

		for (int irun = 0; irun < min(cpttpMax, crun); irun++)
		{
			drrHdr.ReadInt(rgich + irun); // character min
			int cbOffsetThisRun;
			drrHdr.ReadInt(&cbOffsetThisRun);
			vcbOffsets[irun] = cbOffsetThisRun;

			// Check for a duplicate set of properties.
			int irunDup;
			for (irunDup = 0; irunDup < irun; irunDup++)
			{
				if (cbOffsetThisRun == vcbOffsets[irunDup])
				{
					// Duplicate offset: copy the previous ITsTextProps.
					rgpttp[irun] = rgpttp[irunDup];
					rgpttp[irun]->AddRef();
					break;
				}
			}
			if (irunDup >= irun) // didn't find duplicate
			{
				const byte * pbThisRun = pbRun0 + cbOffsetThisRun;
				// Note that the buffer size is provided below simply as a sanity check. The
				// data itself is structured so that the deserialize method knows how much
				// to read for each run.
				DataReaderRgb drr(pbThisRun, (*pcb - (pbThisRun - prgb)));
				TsTextProps::DeserializeDataReader(&drr, rgpttp + irun);
			}
		}
		*pcpttpRet = min(cpttpMax, crun);
	}

	END_COM_METHOD(g_factPropsFact, IID_ITsPropsFactory);
}

/*----------------------------------------------------------------------------------------------
	Create a property with the given writing system, old writing system, and named style (ktptNamedStyle).
	Note that ktptCharStyle (which this used to use) is effectively obsolete.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsFact::MakeProps(BSTR bstr, int ws, int ows, ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);
	ChkComOutPtr(ppttp);
	if ((uint)ws > kwsLim)
		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");

	return MakePropsRgch(bstr, BstrLen(bstr), ws, ows, ppttp);

	END_COM_METHOD(g_factPropsFact, IID_ITsPropsFactory);
}

/*----------------------------------------------------------------------------------------------
	Create a property with the given writing system, old writing system, and named style (ktptNamedStyle).
	Note that ktptCharStyle (which this used to use) is effectively obsolete.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsFact::MakePropsRgch(const OLECHAR * prgch, int cch,	int ws, int ows,
	ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgch, cch);
	ChkComOutPtr(ppttp);
	if ((uint)ws > kwsLim)
		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");

	TsIntProp tip;
	TsStrProp tsp;
	int ctip = 0;
	int ctsp = 0;

	tip.m_nVal = ws;
	tip.m_nVar = ows;
	tip.m_tpt = ktptWs;
	ctip = 1;
	if (cch)
	{
		StrUni stu(prgch, cch);
		TsStrHolder * ptsh = TsStrHolder::GetStrHolder();
		tsp.m_hstuVal = ptsh->GetCookieFromString(stu);
		tsp.m_tpt = ktptNamedStyle;
		ctsp = 1;
	}
	TsTextProps::Create(ctip ? &tip : NULL, ctip, ctsp ? &tsp : NULL, ctsp, ppttp);

	END_COM_METHOD(g_factPropsFact, IID_ITsPropsFactory);
}


/*----------------------------------------------------------------------------------------------
	Get an ITsPropsBldr initially with empty state.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsPropsFact::GetPropsBldr(ITsPropsBldr ** pptpb)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptpb);

	TsPropsBldr::Create(NULL, 0, NULL, 0, pptpb);

	END_COM_METHOD(g_factPropsFact, IID_ITsPropsFactory);
}
