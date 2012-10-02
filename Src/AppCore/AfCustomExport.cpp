/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2004, SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfCustomExport.cpp
Responsibility: Steve McConnel
Last reviewed: never.

Description:
	Implementation of the File / Export dialog classes for the Data Notebook.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

static DummyFactory g_fact(_T("Sil.Notebk.AfCustomExport"));

//:>********************************************************************************************
//:>	AfCustomExport methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfCustomExport::AfCustomExport(AfLpInfo * plpi, AfMainWnd * pafw)
{
	AssertPtr(plpi);
	AssertPtr(pafw);

	m_cref = 1;
	m_plpi = plpi;
	m_pafw = pafw;
	plpi->GetDbInfo()->GetDbAccess(&m_qode);
	plpi->GetDbInfo()->GetFwMetaDataCache(&m_qmdc);
	AssertPtr(m_qode.Ptr());
	AssertPtr(m_qmdc.Ptr());
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfCustomExport::~AfCustomExport()
{
}


//:>********************************************************************************************
//:>	IUnknown methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IFwCustomExport are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IFwCustomExport *>(this));
	else if (iid == IID_IFwCustomExport)
		*ppv = static_cast<IFwCustomExport *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IFwCustomExport);
		return S_OK;
	}
	else
	{
		return E_NOINTERFACE;
	}
	reinterpret_cast<IUnknown *>(*ppv)->AddRef();

	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Standard COM AddRef method.

	@return The reference count after incrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) AfCustomExport::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Standard COM Release method.

	@return The reference count after decrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) AfCustomExport::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


//:>********************************************************************************************
//:>	IFwCustomExport methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Store label style strings for use by the other methods.  This method is called once by the
	Export process, before any of the Build* or Get* methods are called.

	@param bstrLabel Label string used in top level records.
	@param bstrSubLabel Label string used in subrecords.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::SetLabelStyles(BSTR bstrLabel, BSTR bstrSubLabel)
{
	BEGIN_COM_METHOD;

	m_stuLabelFormat.Assign(bstrLabel);
	m_stuSubLabelFormat.Assign(bstrSubLabel);

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Store a mapping associating a field id with a character style name.  This method may be
	called any number of of times, but only before any of the Build* or Get* methods are called.

	@param flid Field id value.
	@param bstrStyle Style name string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::AddFlidCharStyleMapping(int flid, BSTR bstrStyle)
{
	BEGIN_COM_METHOD;

	StrUni stu(bstrStyle);
	m_hmflidstuCharStyle.Insert(flid, stu, true);

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the sequence of certain
	subitems owned by the current record.

	@param pffsp Pointer to the current user view field specification COM object.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::BuildSubItemsString(IFwFldSpec * pffsp, int hvoRec, int ws,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pffsp);
	ChkComOutPtr(pptss);
	if (!hvoRec || !ws)
		ThrowInternalError(E_INVALIDARG);

	return E_NOTIMPL;

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the object reference contained
	by the current record.

	@param pffsp Pointer to the current user view field specification COM object.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::BuildObjRefSeqString(IFwFldSpec * pffsp, int hvoRec, int ws,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pffsp);
	ChkComOutPtr(pptss);
	if (!hvoRec || !ws)
		ThrowInternalError(E_INVALIDARG);

	return E_NOTIMPL;

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the object reference contained
	by the current record.

	@param pffsp Pointer to the current user view field specification COM object.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::BuildObjRefAtomicString(IFwFldSpec * pffsp, int hvoRec, int ws,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pffsp);
	ChkComOutPtr(pptss);
	if (!hvoRec || !ws)
		ThrowInternalError(E_INVALIDARG);

	return E_NOTIMPL;

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Build a TsString that contains the label (if desired) and the "expandable" information
	contained by the current record.

	@param pffsp Pointer to the current user view field specification COM object.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::BuildExpandableString(IFwFldSpec * pffsp, int hvoRec, int ws,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pffsp);
	ChkComOutPtr(pptss);
	if (!hvoRec || !ws)
		ThrowInternalError(E_INVALIDARG);

	return E_NOTIMPL;

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Obtain the string for the enumeration value stored by the given field.  This method
	should be overridden for the specific type of export.

	@param flid Id of field containing an enumeration value.
	@param itss Index of enumeration value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::GetEnumString(int flid, int itss, BSTR * pbstrName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrName);
	if (!flid || itss < 0)
		ThrowInternalError(E_INVALIDARG);

	return E_NOTIMPL;

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
STDMETHODIMP AfCustomExport::GetActualLevel(int nLevel, int hvoRec, int ws, int * pnActualLevel)
{
	BEGIN_COM_METHOD;

	*pnActualLevel = nLevel;

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Build the start and end tags for the current record.  This method may return empty
	strings if the default of <Entry level="0">, <Entry level="1">, etc. is acceptable.

	@param nLevel (Indentation) level of the record (0 means top level, >0 means subrecord)
	@param hvo Database id of the current record (object).
	@param clid Database class id of the current record (object).
	@param pbstrStartTag Pointer to the output BSTR start tag.
	@param pbstrEndTag Pointer to the output BSTR end tag.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::BuildRecordTags(int nLevel, int hvo, int clid,
	BSTR * pbstrStartTag, BSTR * pbstrEndTag)
{
	BEGIN_COM_METHOD;

	return E_NOTIMPL;

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


/*----------------------------------------------------------------------------------------------
	Return all the interesting page setup values used for printing.  It is permissible to
	set all of these to 0 (or NULL), although that would not result in very interesting
	export output.

	@param pnOrientation Pointer to page orientation value (from AfCore::POrientType enum).
	@param pnPaperSize Pointer to paper size value (from AfCore::PgSizeType enum).
	@param pdxmpLeftMargin Pointer to left margin size in millipoints.
	@param pdxmpRightMargin Pointer to right margin size in millipoints.
	@param pdympTopMargin Pointer to top margin size in millipoints.
	@param pdympBottomMargin Pointer to bottom margin size in millipoints.
	@param pdympHeaderMargin Pointer to header margin size in millipoints.
	@param pdympFooterMargin Pointer to footer margin size in millipoints.
	@param pdxmpPageWidth Pointer to page width in millipoints.
	@param pdympPageHeight Pointer to page height in millipoints.
	@param pptssHeader Pointer to the header string COM object pointer.
	@param pptssFooter Pointer to the footer string COM object pointer.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::GetPageSetupInfo(int * pnOrientation, int * pnPaperSize,
	int * pdxmpLeftMargin, int * pdxmpRightMargin, int * pdympTopMargin,
	int * pdympBottomMargin, int * pdympHeaderMargin, int * pdympFooterMargin,
	int * pdxmpPageWidth, int * pdympPageHeight, ITsString ** pptssHeader,
	ITsString ** pptssFooter)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnOrientation);
	ChkComOutPtr(pnPaperSize);
	ChkComOutPtr(pdxmpLeftMargin);
	ChkComOutPtr(pdxmpRightMargin);
	ChkComOutPtr(pdympTopMargin);
	ChkComOutPtr(pdympBottomMargin);
	ChkComOutPtr(pdympHeaderMargin);
	ChkComOutPtr(pdympFooterMargin);
	ChkComOutPtr(pdxmpPageWidth);
	ChkComOutPtr(pdympPageHeight);
	ChkComOutPtr(pptssHeader);
	ChkComOutPtr(pptssFooter);
	AssertPtr(m_pafw);

	POrientType potPageOrient;
	PgSizeType pstPageSize;
	bool fHeaderOnFirstPage;

	m_pafw->GetPageSetupInfo(&potPageOrient, &pstPageSize, pdxmpLeftMargin, pdxmpRightMargin,
		pdympTopMargin, pdympBottomMargin, pdympHeaderMargin, pdympFooterMargin,
		pdxmpPageWidth, pdympPageHeight, pptssHeader, pptssFooter, &fHeaderOnFirstPage);
	*pnOrientation = potPageOrient;
	*pnPaperSize = pstPageSize;

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}

/*----------------------------------------------------------------------------------------------
	Provides chance to post-process the XML file that was created by export before the
	transformation has been done.  Default implementation is to return the input file
	name.

	@param bstrInputFile Path of input file.
	@param pbstrOutputFile Path of output file created in post-processing
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::PostProcessFile(BSTR bstrInputFile, BSTR * pbstrOutputFile)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrOutputFile);

	SmartBstr sbstr = bstrInputFile;
	sbstr.Copy(pbstrOutputFile);

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}

/*----------------------------------------------------------------------------------------------
	Provides a chance to include object data in TsString XML

	@param pbWriteObjData Set to true if the data was processed, false to ignore it
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfCustomExport::IncludeObjectData(ComBool *pbWriteObjData)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbWriteObjData);

	*pbWriteObjData = false;

	END_COM_METHOD(g_fact, IID_IFwCustomExport);
}


//:>********************************************************************************************
//:>	Internal methods that are useful to various subclass implementations.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Extract the values from an IFwFldSpec COM object into a dummy FldSpec object.

	@param pffsp Pointer to the current user view field specification COM object.
	@param hvoRec Database id of the current record (object).
	@param ws Language writing system desired for output.
	@param pptss Address of the TsString pointer used for returning the computed value.
----------------------------------------------------------------------------------------------*/
void AfCustomExport::ExtractFldSpec(IFwFldSpec * pffsp, FldSpec *pfsp)
{
	AssertPtr(pffsp);
	AssertPtr(pfsp);

	int eFspVisibility;
	CheckHr(pffsp->get_Visibility(&eFspVisibility));
	pfsp->m_eVisibility = (FldVis)eFspVisibility;

	ComBool fFspHideLabel;
	CheckHr(pffsp->get_HideLabel(&fFspHideLabel));
	pfsp->m_fHideLabel = (bool)fFspHideLabel;

	CheckHr(pffsp->get_Label(&pfsp->m_qtssLabel));

	CheckHr(pffsp->get_FieldId(&pfsp->m_flid));

	SmartBstr sbstr;
	CheckHr(pffsp->get_ClassName(&sbstr));
	pfsp->m_stuClsName = sbstr.Chars();

	CheckHr(pffsp->get_FieldName(&sbstr));
	pfsp->m_stuFldName = sbstr.Chars();

	CheckHr(pffsp->get_Style(&sbstr));
	pfsp->m_stuSty = sbstr.Chars();
}


/*----------------------------------------------------------------------------------------------
	Fill an ANSI character string with the date value given by the field id and object id.
	The pattern for XML data primitive dateTime is CCYY-MM-DDThh:mm:ss where CC represents the
	century, YY the year, MM the month, and DD the day, preceded by an optional leading negative
	(-) character to indicate a negative number.  Note also the T between the date and the time.

	@param flid Database field id.
	@param hvo Database object id.
	@param stuDate Reference to the output string.
----------------------------------------------------------------------------------------------*/
void AfCustomExport::BuildDateCreatedString(int flid, HVO hvo, StrUni & stuDate)
{
	Assert(flid);
	Assert(hvo);
	AssertPtr(m_qmdc);
	AssertPtr(m_qode);

	stuDate.Clear();

	// Get the data from the database.
	try
	{
		IOleDbCommandPtr qodc;
		CheckHr(m_qode->CreateCommand(&qodc));
		StrUni stuQuery;
		ComBool fMoreRows;
		SmartBstr sbstrField;
		SmartBstr sbstrClass;

		CheckHr(m_qmdc->GetOwnClsName(flid, &sbstrClass));
		CheckHr(m_qmdc->GetFieldName(flid, &sbstrField));
		stuQuery.Format(L"SELECT [%s] FROM [%s] WHERE [Id]=%d",
			sbstrField.Chars(), sbstrClass.Chars(), hvo);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			ComBool fIsNull;
			ULONG cbSpaceTaken;
			DBTIMESTAMP tim;

			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&tim),
				sizeof(DBTIMESTAMP), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				stuDate.Format(L"%4d-%02d-%02dT%02d:%02d:%02d",
					tim.year, tim.month, tim.day, tim.hour, tim.minute, tim.second);
			}
		}
	}
	catch (...)
	{
		stuDate.Clear();
	}
}


// Handle explicit instantiation.
#include "HashMap_i.cpp"

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mknb.bat"
// End: (These 4 lines are useful to Steve McConnel.)
