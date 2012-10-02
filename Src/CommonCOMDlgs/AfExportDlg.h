/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfExportDlg.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Header file for the File / Export Dialog classes.

		AfExportDlg : AfDialog
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFEXPORTDLG_H_INCLUDED
#define AFEXPORTDLG_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the File / Export menu command.

	Hungarian: red
----------------------------------------------------------------------------------------------*/
class AfExportDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	AfExportDlg();
	// The superclass methods are overridden by these methods.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	void Initialize(AfLpInfo * plpi, IVwStylesheet * pvss, IFwCustomExport * pfcex,
		const achar * pszRegProgName, const achar * pszProgHelpFile, const achar * pszHelpTopic,
		int vwt, int flidSubItems, int crec, int * rghvoRec, int * rgclidRec);

	void ExportData(HWND hwndParent);

protected:
	virtual bool OnHelp();
	virtual bool OnHelpInfo(HELPINFO * phi);

	void GetFwFldSpec(FldSpec * pfsp, IFwFldSpec ** ppffsp);

	void Export();
	void WriteRecord(IStream * pstrm, HvoClsid & hcRec, int nLevel);
	void GetExportOptions();
	void BuildRefAtomicString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void BuildRefSeqString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void BuildGenDateString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void BuildDateROString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void BuildEnumString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void BuildGroupString(FldVec & vqfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void BuildGroupLine(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void BuildMultiTsString(FldSpec * pfsp, HVO hvoRec, Vector<int> & vws, ITsString ** pptss);
	void BuildTsString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void LoadTsString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void BuildIntegerString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void BuildGuidString(FldSpec * pfsp, HVO hvoRec, int ws, ITsString ** pptss);
	void WriteParagraph(IStream * pstrm, ITsString * ptss, int flid, int nLevel,
		StrUni & stuField);
	void WriteParagraph(IStream * pstrm, ITsString * ptss, StrUni & stuStyle, int nIndent,
		StrUni & stuField, int nLevel);
	void WriteStructuredText(IStream * pstrm, FldSpec * pfsp, HVO hvoRec, int ws,
		int nLevel);
	void WriteGroupOnePerLine(IStream * pstrm, BlockSpec * pbsp, HVO hvoRec, int ws,
		int nLevel);
	void WriteSubItems(IStream * pstrm, FldSpec * pfsp, HVO hvoRec, int ws, int nLevel);
	void GetDefaultFileAndDir(achar * pszFile, int cchFileMax, achar * pszDir, int cchDirMax);
	void AdjustFilename();
	void Transform();
	void LoadDOM(IXMLDOMDocument * pDOM, BSTR bstrFile);
	bool ProcessXsl(StrUni & stuInput, StrUni & stuStylesheet, StrUni & stuOutput, int iXsl);
	void BuildXslChain(AfExportStyleSheet & ess);

	bool m_fSelectionOnly;
	StrApp m_strPathname;
	StrApp m_strFilter;					// Parallels m_vess, used for ::GetSaveFileName().
	Vector<AfExportStyleSheet> m_vess;
	int m_iess;
	bool m_fOpenWhenDone;
	int m_flidSubItems;
	Set<int> m_setclidSubItems;

	AfLpInfo * m_plpi;
	IVwStylesheetPtr m_qvss;
	IFwCustomExportPtr m_qfcex;
	int m_vwt;
	int m_crec;
	int * m_rghvoRec;
	int * m_rgclidRec;
	ILgWritingSystemFactoryPtr m_qwsf;
	UserViewSpec * m_puvs;
	IFwMetaDataCachePtr m_qmdc;
	IOleDbEncapPtr m_qode;

	StrUni m_stuLabelFormat;
	StrUni m_stuSubLabelFormat;
	enum
	{
		knMaxLevels = 5,			// Maximum expected nesting level of records/subrecords.
		knIndentPerLevel = 30000	// Indentation per level in mpt.
	};
	StrUni m_rgstuHeadings[knMaxLevels];
	HashMap<int, StrUni> m_hmflidstuCharStyle;
	HashMap<int, StrUni> m_hmflidstuParaStyle;
	StrAnsi m_staTmpFile;
	SmartBstr m_sbstrError;
	long m_nErrorCode;
	long m_nErrorLine;
	FwSettings m_fws;
	StrApp m_stuErrorProcessStep;
	StrApp m_strHelpFilename;
	HWND m_hwndParent;
	bool m_fCancelled;				// Flag that user wants to cancel export process.
	AfProgressDlgPtr m_qprog;
};
typedef GenSmartPtr<AfExportDlg> AfExportDlgPtr;

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif  // !AFEXPORTDLG_H_INCLUDED
