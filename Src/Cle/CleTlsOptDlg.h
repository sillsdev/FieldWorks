/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2000, SIL International. All rights reserved.

File: CleTlsOptDlg.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Header file for the Tools Options Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef CLETLSOPTDLG_H_INCLUDED
#define CLETLSOPTDLG_H_INCLUDED

typedef enum // VwRecType defined differently for each application.
{
	// These are the types of objects that show up in the "Fields in" combo boxes.
	// Actually they don't show up in the list editor because we only have a single item.
	kvrtCmPossibility = 0,
	kvrtCmPerson = 1,
	kvrtCmLocation = 2,
	kvrtCmAnthroItem = 3,
	kvrtCmCustomItem = 4,
	kvrtMoMorphType = 5,
	kvrtLexEntryType = 6,
	kvrtPartOfSpeech = 7,
	kvrtCleLim // End of list.
};


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Tools Options Dialog. It embeds child dialogs
	using a tabbed interface.
	@h3{Hungarian: ctod}
----------------------------------------------------------------------------------------------*/

class CleTlsOptDlg : public TlsOptDlg
{
typedef TlsOptDlg SuperClass;
public:
	CleTlsOptDlg();

	enum
	{
		// Indices to tabs in the CleTlsOptDlg tabbed dialog.
		// CAUTION: If the order is changed, you also need to make the change
		// in CleTlsOptDlg::OnInitDlg where the m_vdlgv vector is initially filled.
		kidlgCustom = 0,
		kidlgViews,
		kidlgFilters,
		kidlgSortMethods,
		kidlgGeneral,

		kcdlgv,
	};

	void SaveDialogValues();
	void GetBlockVec(UserViewSpecVec & vuvs, int ivuvs, int vrt, RecordSpec ** pprsp);
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool CheckReqList(int flid);
	StrApp GetIncludeLabel();
	FldVis GetCustVis(int vwt, int nrt);
	// Override to return more appropriate index.
	virtual int GetInitialTabIndex(int cid);
};

typedef GenSmartPtr<CleTlsOptDlg> CleTlsOptDlgPtr;

#endif  // !CLETLSOPTDLG_H_INCLUDED
