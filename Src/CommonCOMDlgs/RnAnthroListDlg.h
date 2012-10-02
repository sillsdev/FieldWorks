/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2003 by SIL International. All rights reserved.

File: RnAnthroListDlg.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Header file for the Dialog class for choosing an anthropology list for a new project.
		RnAnthroListDlg : AfDialog
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RNANTHROLISTDLG_H_INCLUDED
#define RNANTHROLISTDLG_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This class provides a dialog for choosing the anthropology categories list for a new
	language project.

	@h3{Hungarian: rald}
----------------------------------------------------------------------------------------------*/
class RnAnthroListDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnAnthroListDlg();
	~RnAnthroListDlg();

	void SetValues(bool fHaveOCM, bool fHaveFRAME, const Vector<StrApp> & vstrXmlFiles,
		const achar * pszHelpFilename);
	int GetChoice()
	{
		return m_nChoice;
	}

	enum
	{
		kralUserDef = -3,
		kralOCM = -2,
		kralFRAME = -1
		// values >= 0 index into m_vstrXmlFiles.
	};

	void SetDescription(BSTR bstrDescription);

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	virtual bool OnCancel();
	virtual bool OnHelp();

	//:> Variables.

	bool m_fHaveOCM;
	bool m_fHaveFRAME;
	Vector<StrApp> m_vstrXmlFiles;
	StrApp m_strDescription;
	StrApp m_strHelpFilename;

	int m_nChoice;
};

typedef GenSmartPtr<RnAnthroListDlg> RnAnthroListDlgPtr;

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif  // !RNANTHROLISTDLG_H_INCLUDED
