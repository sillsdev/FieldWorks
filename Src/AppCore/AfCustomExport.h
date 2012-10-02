/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2004, SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfCustomExport.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Header file for the File / Export Dialog customization class.

		AfCustomExport : IFwCustomExport
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFCUSTOMEXPORT_H_INCLUDED
#define AFCUSTOMEXPORT_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This class provides custom methods used in exporting from AfApp based applications.  It
	should be subclassed by specific applications as needed.

	Hungarian: acex
----------------------------------------------------------------------------------------------*/
class AfCustomExport : public IFwCustomExport
{
public:
	AfCustomExport(AfLpInfo * plpi, AfMainWnd * pafw);
	~AfCustomExport();

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// IFwCustomExport methods.
	STDMETHOD(SetLabelStyles)(BSTR bstrLabel, BSTR bstrSubLabel);
	STDMETHOD(AddFlidCharStyleMapping)(int flid, BSTR bstrStyle);
	STDMETHOD(BuildSubItemsString)(IFwFldSpec * pffsp, int hvoRec, int ws, ITsString ** pptss);
	STDMETHOD(BuildObjRefSeqString)(IFwFldSpec * pffsp, int hvoRec, int ws, ITsString ** pptss);
	STDMETHOD(BuildObjRefAtomicString)(IFwFldSpec * pffsp, int hvoRec, int ws,
		ITsString ** pptss);
	STDMETHOD(BuildExpandableString)(IFwFldSpec * pffsp, int hvoRec, int ws,
		ITsString ** pptss);
	STDMETHOD(GetEnumString)(int flid, int itss, BSTR * pbstrName);
	STDMETHOD(GetActualLevel)(int nLevel, int hvoRec, int ws, int * pnActualLevel);
	STDMETHOD(BuildRecordTags)(int nLevel, int hvo, int clid, BSTR * pbstrStartTag,
		BSTR * pbstrEndTag);
	STDMETHOD(GetPageSetupInfo)(int * pnOrientation, int * pnPaperSize, int * pdxmpLeftMargin,
		int * pdxmpRightMargin, int * pdympTopMargin, int * pdympBottomMargin,
		int * pdympHeaderMargin, int * pdympFooterMargin, int * pdxmpPageWidth,
		int * pdympPageHeight, ITsString ** pptssHeader, ITsString ** pptssFooter);
	STDMETHOD(PostProcessFile)(BSTR bstrInputFile, BSTR * pbstrOutputFile);
	STDMETHOD(IncludeObjectData)(ComBool * pbWriteObjData);

protected:
	// The default constructor is used only for testing GetEnumString.  All other methods
	// crash and burn without the AfLpInfo or the AfMainWnd objects!
	AfCustomExport()
	{
		m_cref = 1;
	}

	long m_cref;
	IOleDbEncapPtr m_qode;
	IFwMetaDataCachePtr m_qmdc;
	AfLpInfo * m_plpi;
	AfMainWnd * m_pafw;

	StrUni m_stuLabelFormat;
	StrUni m_stuSubLabelFormat;
	HashMap<int, StrUni> m_hmflidstuCharStyle;

	void ExtractFldSpec(IFwFldSpec * pffsp, FldSpec * pfsp);
	void BuildDateCreatedString(int flid, HVO hvoRec, StrUni & stuDate);
};
typedef GenSmartPtr<AfCustomExport> AfCustomExportPtr;

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkaflib.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif  // !AFCUSTOMEXPORT_H_INCLUDED
