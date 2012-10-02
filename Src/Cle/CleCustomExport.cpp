/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2004, SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: CleCustomExport.cpp
Responsibility: Steve McConnel
Last reviewed: never.

Description:
	Implementation of the File / Export dialog classes for the Data Notebook.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	CleCustomExport methods.
//:>********************************************************************************************

static DummyFactory g_fact(_T("Sil.Notebk.CleCustomExport"));

/*----------------------------------------------------------------------------------------------
	Constructor: just call the base class constructor.
----------------------------------------------------------------------------------------------*/
CleCustomExport::CleCustomExport(AfLpInfo * plpi, AfMainWnd * pafw)
	: AfCustomExport(plpi, pafw)
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CleCustomExport::~CleCustomExport()
{
}


//:>********************************************************************************************
//:>	IUnknown methods: use the inherited implementations.
//:>********************************************************************************************


//:>********************************************************************************************
//:>	IFwCustomExport methods.
//:>	Use the inherited implementations for the following methods:
//:>
//:>	HRESULT SetLabelStyles(BSTR bstrLabel, BSTR bstrSubLabel)
//:>	HRESULT AddFlidCharStyleMapping(int flid, BSTR bstrStyle)
//:>	HRESULT BuildSubItemsString(IFwFldSpec * pffsp, int hvoRec, int ws, ITsString ** pptss)
//:>	HRESULT BuildObjRefSeqString(IFwFldSpec * pffsp, int hvoRec, int ws, ITsString ** pptss)
//:>	HRESULT BuildObjRefAtomicString(IFwFldSpec * pffsp, int hvoRec, int ws, ...)
//:>	HRESULT BuildExpandableString(IFwFldSpec * pffsp, int hvoRec, int ws, ...)
//:>	HRESULT BuildRecordTags(int nLevel, int hvo, int clid, BSTR * pbstrStartTag,
//:>		BSTR * pbstrEndTag);
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Obtain the string for the enumeration value stored by the given field.  This method
	should be overridden for the specific type of export.

	@param flid Id of field containing an enumeration value.
	@param itss Index of enumeration value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleCustomExport::GetEnumString(int flid, int itss, BSTR * pbstrName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrName);
	if (!flid || itss < 0)
		ThrowInternalError(E_INVALIDARG);

	StrUni stuEnum;
	switch (flid)
	{
	case kflidCmPerson_Gender:
		stuEnum.Load(kstidEnumGender);
		break;
	case kflidCmPerson_IsResearcher:
	case kflidMoInflAffixSlot_Optional:
		stuEnum.Load(kstidEnumNoYes);
		break;
	default:
		return S_FALSE;
	}
	const wchar * pszEnum = stuEnum.Chars();
	const wchar * pszEnumLim = stuEnum.Chars() + stuEnum.Length();
	int itssTry = 0;
	while (pszEnum < pszEnumLim && itssTry <= itss)
	{
		const wchar * pszEnumNl = wcschr(pszEnum, '\n');
		if (!pszEnumNl)
			pszEnumNl = pszEnumLim;
		if (itss == itssTry)
		{
			StrUni stu(pszEnum, pszEnumNl - pszEnum);
			stu.GetBstr(pbstrName);
			return S_OK;
		}
		itssTry++;
		pszEnum = pszEnumNl + 1;
	}

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Get the actual indentation level for the current record.  In some cases (such as the
	Data Notebook), this value is implicitly maintained by the structure of the object.  In
	other cases (such as Topics List Editor), the hiearchy is marked explicitly in the data.

	@param nLevel (Indentation) level of the record (0 means top level, >0 means subrecord)
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleCustomExport::GetActualLevel(int nLevel, int hvoRec, int ws,
	int * pnActualLevel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnActualLevel);
	AssertPtr(m_plpi);

	PossListInfoPtr qpli;
	PossItemInfo * ppii;
	if (m_plpi->GetPossListAndItem(hvoRec, ws, &ppii, &qpli))
	{
		*pnActualLevel = ppii->GetLevel(qpli);
	}
	else
	{
		*pnActualLevel = nLevel;
	}

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkcle.bat"
// End: (These 4 lines are useful to Steve McConnel.)
