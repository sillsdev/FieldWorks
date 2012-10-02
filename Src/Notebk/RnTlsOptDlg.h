/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2000, SIL International. All rights reserved.

File: RnTlsOptDlg.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Header file for the Tools Options Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RNTlSOPTDLG_H_INCLUDED
#define RNTlSOPTDLG_H_INCLUDED

typedef enum // VwRecType defined differently for each application.
{
	// These are the types of objects that show up in the "Fields in" combo boxes.
	kvrtEvent = 0,	// Event Entry
	kvrtAnal,		// Analysis Entry
	kvrtRnLim // End of list.
};


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Tools Options Dialog. It embeds child dialogs
	using a tabbed interface.
	@h3{Hungarian: rtod}
----------------------------------------------------------------------------------------------*/

class RnTlsOptDlg : public TlsOptDlg
{
typedef TlsOptDlg SuperClass;
public:
	RnTlsOptDlg();

	enum
	{
		// Indices to tabs in the RnTlsOptDlg tabbed dialog.
		// CAUTION: If the order is changed, you also need to make the change
		// in RnTlsOptDlg::OnInitDlg where the m_vdlgv vector is initially filled.
		kidlgCustom = 0,
		kidlgViews,
		kidlgFilters,
		kidlgSortMethods,
		kidlgOverlays,
		kidlgGeneral,

		kcdlgv,
	};

	void SaveDialogValues();
	void GetBlockVec(UserViewSpecVec & vuvs, int ivuvs, int vrt, RecordSpec ** pprsp);
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool CheckReqList(int flid);
	FldVis GetCustVis(int vwt, int nrt);
	// Override to return more appropriate index.
	virtual int GetInitialTabIndex(int cid);

protected:
	virtual void ProcessBrowseSpec(UserViewSpec * puvs, AfLpInfo * plpi);
};

typedef GenSmartPtr<RnTlsOptDlg> RnTlsOptDlgPtr;

#endif  // !RNTlSOPTDLG_H_INCLUDED
