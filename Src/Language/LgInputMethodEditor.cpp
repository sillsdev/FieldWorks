/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgInputMethodEditor.cpp
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

LgInputMethodEditor::LgInputMethodEditor()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

LgInputMethodEditor::~LgInputMethodEditor()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language.LgInputMethodEditor"),
	&CLSID_LgInputMethodEditor,
	_T("SIL Roman renderer"),
	_T("Apartment"),
	&LgInputMethodEditor::CreateCom);


void LgInputMethodEditor::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgInputMethodEditor> qime;
	qime.Attach(NewObj LgInputMethodEditor());		// ref count initialy 1
	CheckHr(qime->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgInputMethodEditor::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgInputMethodEditor *>(this));
	else if (riid == IID_ISimpleInit)
		*ppv = static_cast<ISimpleInit *>(this);
	else if (riid == IID_ILgInputMethodEditor)
		*ppv = static_cast<ILgInputMethodEditor *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<ILgInputMethodEditor *>(this),
			IID_ISimpleInit, IID_ILgInputMethodEditor);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ISimpleInit Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize an instance, typically from a ClassInitMoniker

	To create a suitable moniker, do something like this:

	const wchar * psz = L"Times New Roman;Helvetica,Arial;Courier";
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	hr = qcim->InitNew(CLSID_LgSystemCollater, (const BYTE *)psz, StrLen(psz) * isizeof(wchar));

	Commas separate font names in a list; semi-colons separate lists for Serif, SansSerif,
	and Monospace (in that order).

	ENHANCE JohnT: should we verify that the fonts in question are installed?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgInputMethodEditor::InitNew(const BYTE * prgb, int cb)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgb, cb);

	return S_OK;

	END_COM_METHOD(g_fact, IID_ISimpleInit);
}

/*----------------------------------------------------------------------------------------------
	Return the initialization value previously set by InitNew.

	@param pbstr Pointer to a BSTR for returning the initialization data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgInputMethodEditor::get_InitializationData(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);

	*pbstr = NULL;

	END_COM_METHOD(g_fact, IID_ISimpleInit);
}


//:>********************************************************************************************
//:>	   ILgInputMethodEditor methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
Setup();
	Given something the user typed, following any preprocessing that happens
	automatically as a result of Setup (e.g., Keyman processing), do any further
	processing required to actually replace the selected part of the string with  what the user typed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgInputMethodEditor::Setup()
{
	BEGIN_COM_METHOD;

	return S_OK;

	END_COM_METHOD(g_fact, IID_ILgFontManager);
}

/*----------------------------------------------------------------------------------------------
Replace
	The default implementation just replaces characters from ichMin to ichLim with
	those from bstrInput, then set *pichModMin to ichMin, and *pichModLim and
	*pichIP both to ichMin + BstrLen(bstrInput).

	 Arguments:
		bstrInput				what user typed
		pttpInput          		text properties desired for new text
		ptsbOld        			original, unedited text, gets modified
		ichMin,	ichLim			range in original to replace
		pichModMin, pichModLim  range in output text affected
		pichIP                  position of IP in modified string

----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgInputMethodEditor::Replace(BSTR bstrInput, ITsTextProps * pttpInput,
		ITsStrBldr * ptsbOld, int ichMin, int ichLim, int * pichModMin, int * pichModLim,
		int * pichIP)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrInput);
	ChkComArgPtrN(pttpInput);
	ChkComArgPtr(ptsbOld);
	ChkComOutPtr(pichModMin);
	ChkComOutPtr(pichModLim);
	ChkComOutPtr(pichIP);

	int cCh;
	SmartBstr sbstr;
	CheckHr(ptsbOld->get_Length(&cCh));
	if (ichMin < 0 || ichLim > cCh || ichMin > ichLim)
	{
		*pichModMin = 0;
		*pichModLim = 0;
		*pichIP = 0;
		ThrowHr(WarnHr(E_INVALIDARG));
	}

	// Check to make sure the ichMin is not between a surrognte pair.
	do
	{
		if (0 < ichMin)
		{
			CheckHr(ptsbOld->GetChars(ichMin, ichMin + 1, &sbstr));
			if (sbstr[0] < 0xDC00 || sbstr[0] > 0xDFFF)
				break;
		}
		else
		{
			break;
		}
	} while (--ichMin > 0);

	// Check to make sure the ichLim is not between a surrgante pair.
	do
	{
		if (cCh > ichLim)
		{
			CheckHr(ptsbOld->GetChars(ichLim, ichLim + 1, &sbstr));
			if (sbstr[0] < 0xDC00 || sbstr[0] > 0xDFFF)
				break;
		}
		else
		{
			break;
		}
	} while (++ichLim < cCh);

	// Now, do the real work

	CheckHr(ptsbOld->Replace(ichMin, ichLim, bstrInput, pttpInput));
	*pichModMin = ichMin;
	*pichModLim = ichMin + BstrLen(bstrInput);
	*pichIP = ichMin + BstrLen(bstrInput);

	END_COM_METHOD(g_fact, IID_ILgFontManager);
}

/*----------------------------------------------------------------------------------------------
	Backspace
		The user pressed a certain number of backspaces. Delete an appropriate amount of
		the string represented by the input string builder, indicating exactly what changed.
		Also, if there were not enough characters to delete, indicate how many backspaces
		were left over.

	 Arguments:
		ichStart				starting position of backspaces
		cactBackspace			number of backspaces pressed
		ptsbOld        			original, unedited text, gets modified
		pichModMin, pichModLim  range in output text affected
		pichIP					position of IP in modified string
		pcactBsRemaining        Number not handled, to affect previous writing system

----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgInputMethodEditor::Backspace(int ichStart, int cactBackspace,
		ITsStrBldr * ptsbOld, int * pichModMin, int * pichModLim, int * pichIP,
		int * pcactBsRemaining)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptsbOld);
	ChkComOutPtr(pichModMin);
	ChkComOutPtr(pichModLim);
	ChkComOutPtr(pichIP);
	ChkComOutPtr(pcactBsRemaining);

	int cCh;
	int ichMin;
	int ich;
	int ichMin1;
	int iBsIn;
	int cBsOut=0;
	SmartBstr sbstr;

	if (ichStart < 1)
	{
		*pichModMin = 0;
		*pichModLim = 0;
		*pichIP = 0;
		*pcactBsRemaining = cactBackspace;
		return S_OK;
	}

	// Check to make sure the ichStart is not between a surrgante pair.
	CheckHr(ptsbOld->get_Length(&cCh));
	if (cCh > ichStart)
	{
		CheckHr(ptsbOld->GetChars(ichStart, ichStart + 1, &sbstr));
		if (sbstr[0] > 0xDBFF && sbstr[0] < 0xE000)
			ichStart++;
	}

	// increase backspace count for any other surrgate pairs.
	iBsIn = cactBackspace;
	ichMin1 = ichStart - (cactBackspace * 2) - 1;
	if (ichMin1 < 0)
		ichMin1 = 0;
	CheckHr(ptsbOld->GetChars(ichMin1, ichStart, &sbstr));
	ich = ichStart - ichMin1 - 1;
	do
	{
		if (sbstr[ich] < 0xDC00 || sbstr[ich] > 0xDFFF)
			--iBsIn;
		++cBsOut;
		if (--ich < 0)
			break;
	} while (iBsIn > 0);
	cactBackspace = cBsOut + iBsIn;

		// Now, do the real work
	if (ichStart > cactBackspace)
	{
		ichMin = ichStart - cactBackspace;
		CheckHr(ptsbOld->Replace(ichMin, ichStart, NULL, NULL));
		*pichModMin = ichMin;
		*pichModLim = ichMin;
		*pichIP = ichMin;
		*pcactBsRemaining = 0;
	}
	else
	{
		CheckHr(ptsbOld->Replace(0, ichStart, NULL, NULL));
		*pichModMin = 0;
		*pichModLim = 0;
		*pichIP = 0;
		*pcactBsRemaining = cactBackspace - ichStart;
	}

	END_COM_METHOD(g_fact, IID_ILgFontManager);
}

/*----------------------------------------------------------------------------------------------
	DeleteForward
		The user pressed a certain number of forward deletes. Forward delete an appropriate
		amount of the string represented by the input string builder, indicating exactly what
		changed.  lso, if there were not enough characters to delete, indicate how many deletes
		were left over.

	 Arguments:
		ichStart				starting position of deletes
		cactDelForward			number of DF pressed
		ptsbOld        			original, unedited text, gets modified
		pichModMin, pichModLim  range in output text affected
		pichIP					position of IP in modified string
		pcactDfRemaining        Number not handled, to affect next writing system

----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgInputMethodEditor::DeleteForward(int ichStart, int cactDelForward,
	ITsStrBldr * ptsbOld, int * pichModMin, int * pichModLim, int * pichIP,
	int * pcactDfRemaining)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptsbOld);
	ChkComOutPtr(pichModMin);
	ChkComOutPtr(pichModLim);
	ChkComOutPtr(pichIP);
	ChkComOutPtr(pcactDfRemaining);

	int cCh;
	int ichLim;
	int ich;
	int iDfIn;
	int cDfOut=0;
	SmartBstr sbstr;

	CheckHr(ptsbOld->get_Length(&cCh));
	if (ichStart > cCh - 2)
	{
		*pichModMin = cCh;
		*pichModLim = cCh;
		*pichIP = cCh;
		*pcactDfRemaining = cactDelForward;
		ThrowHr(WarnHr(E_INVALIDARG));
	}

	// Check to make sure the ichStart is not between a surrgante pair.
	if (cCh > ichStart)
	{
		CheckHr(ptsbOld->GetChars(ichStart, ichStart + 1, &sbstr));
		if (sbstr[0] > 0xDBFF && sbstr[0] < 0xE000)
			ichStart--;
	}

	// increase DeleteForward count for any other surrgate pairs.
	iDfIn = cactDelForward;
	ichLim = ichStart + (cactDelForward * 2) + 1;
	if (ichLim > cCh)
		ichLim = cCh;
	CheckHr(ptsbOld->GetChars(ichStart, ichLim, &sbstr));
	ich = ichStart;
	do
	{
		if (sbstr[ich] < 0xDC00 || sbstr[ich] > 0xDFFF)
			--iDfIn;
		++cDfOut;
		if (++ich >= cCh)
			break;
	} while (iDfIn > 0);
	cactDelForward = cDfOut + iDfIn;

	// Now, do the real work
	if (ichStart + cactDelForward < cCh)
	{
		ichLim = ichStart + cactDelForward;
		CheckHr(ptsbOld->Replace(ichStart, ichLim, NULL, NULL));
		*pichModMin = ichStart;
		*pichModLim = ichStart;
		*pichIP = ichStart;
		*pcactDfRemaining = 0;
	}
	else
	{
		CheckHr(ptsbOld->Replace(ichStart, cCh, NULL, NULL));
		*pichModMin = ichStart;
		*pichModLim = ichStart;
		*pichIP = ichStart;
		*pcactDfRemaining = iDfIn;
	}

	END_COM_METHOD(g_fact, IID_ILgFontManager);
}

/*----------------------------------------------------------------------------------------------
	IsValidInsertionPoint
		True if input method considers an IP at the specified index reasonable.
		Note that really useful IPs should also satisfy the Renderer; see
		ILgSegment>>IsValidInsertionPoint.

	 Arguments:
		ich
		ptss
		pfValid

----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgInputMethodEditor::IsValidInsertionPoint(int ich, ITsString * ptss,
														BOOL * pfValid)
// TODO: Shouldn't pfValid be a ComBool?
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptss);
	ChkComOutPtr(pfValid);

	int cCh;
	SmartBstr sbstr;
	CheckHr(ptss->get_Length(&cCh));
	if (ich == cCh)
	{
		*pfValid = 1;
	}
	else if ((ich >= 0) && (ich < cCh))
	{
		CheckHr(ptss->GetChars(ich, ich + 1, &sbstr));
		*pfValid = ((sbstr[0] < 0xDC00) || (sbstr[0] > 0xDFFF)) && (ich >= 0) && (ich <= cCh);
	}
	else
	{
		*pfValid = 0;
	}
	END_COM_METHOD(g_fact, IID_ILgFontManager);
}
