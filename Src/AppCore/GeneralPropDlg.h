/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GeneralPropDlg.h
Responsibility: Steve McConnel
Last reviewed: never

Description:
	Define the dialog classes that support Properties dialogs, especially the General
	Properties tab.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef GENERALPROPDLG_H_INCLUDED
#define GENERALPROPDLG_H_INCLUDED 1

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Properties dialog.  This must be subclassed, since it has no dialog or help resources of
	its own.

	@h3{Hungarian: propd}
----------------------------------------------------------------------------------------------*/
class PropertiesDlg : public AfDialog
{
	typedef AfDialog SuperClass;
public:
	PropertiesDlg()
	{
		m_itabCurrent = -1;
		m_flidDateCreated = kflidCmMajorObject_DateCreated;
		m_flidDateModified = kflidCmMajorObject_DateModified;
	}

	~PropertiesDlg()
	{
	}

	void Initialize(AfLpInfo * plpi, HICON hicon, const achar * pszName, const achar * pszType,
		const achar * pszLoc, const achar * pszSize, HVO hvo, const achar * pszDesc,
		const achar * pszAbbr, const achar * pszHelpF, unsigned int ctidName, int wsMagic)
	{
		m_plpi = plpi;
		m_hicon = hicon;
		m_strName = pszName;
		m_strAbbr = pszAbbr;
		m_strType = pszType;
		m_strLocation = pszLoc;
		m_strSize = pszSize;
		m_hvoObj = hvo;
		m_strDesc = pszDesc;
		m_strHelpF = pszHelpF;
		m_ctidName = ctidName;
		m_wsMagic = wsMagic;
	}

	void InitializeList(bool fsorted, int nDepth, bool fdup, int nDispOpt,
		const achar * pszWndCaption)
	{
		m_fsorted = fsorted;
		m_nDepth = nDepth;
		m_fdup = fdup;
		m_nDispOpt = nDispOpt;
		m_strWndCaption = pszWndCaption;
	}

	int GetWs()
	{
		return m_wsMagic;
	}

	int GetDispOpt()
	{
		return m_nDispOpt;
	}

	int GetDepth()
	{
		return m_nDepth;
	}

	bool GetDuplicates()
	{
		return m_fdup;
	}

	bool GetSorted()
	{
		return m_fsorted;
	}

	AfLpInfo * GetLangProjInfo()
	{
		return m_plpi;
	}

	const achar * GetName()
	{
		return m_strName.Chars();
	}

	const achar * GetAbbr()
	{
		return m_strAbbr.Chars();
	}

	HICON GetIconHandle()
	{
		return m_hicon;
	}

	const achar * GetType()
	{
		return m_strType.Chars();
	}

	const achar * GetLocation()
	{
		return m_strLocation.Chars();
	}

	const achar * GetSizeString()
	{
		return m_strSize.Chars();
	}

	HVO GetObjId()
	{
		return m_hvoObj;
	}

	const achar * GetDescription()
	{
		return m_strDesc.Chars();
	}

	const achar * GetHelpFile()
	{
		return m_strHelpF.Chars();
	}

	int GetDateCreatedFlid()
	{
		return m_flidDateCreated;
	}

	int GetDateModifiedFlid()
	{
		return m_flidDateModified;
	}

	//:>////////////////////////////////////////////////////////////////////////////////////////

	void SetWs(int wsMagic)
	{
		m_wsMagic = wsMagic;
	}

	void SetDispOpt(int idispOpt)
	{
		m_nDispOpt = idispOpt;
	}

	void SetDepth(int idepth)
	{
		m_nDepth = idepth;
	}

	void SetDuplicates(bool fdup)
	{
		m_fdup = fdup;
	}

	void SetSorted(bool fsorted)
	{
		m_fsorted = fsorted;
	}

	virtual bool CheckName(const achar * pszName)
	{
		return true;
	}

	void SetName(const achar * pszName)
	{
		m_strName.Assign(pszName);
	}

	void SetAbbr(const achar * pszAbbr)
	{
		m_strAbbr.Assign(pszAbbr);
	}

	void SetDescription(const achar * pszDesc)
	{
		m_strDesc.Assign(pszDesc);
	}

	void SetLocation(const achar * pszLoc)
	{
		m_strLocation.Assign(pszLoc);
	}

	void SetHelpFile(const achar * pszHelpF)
	{
		m_strHelpF.Assign(pszHelpF);
	}

	void SetDateCreatedFlid(int flid)
	{
		m_flidDateCreated = flid;
	}

	void SetDateModifiedFlid(int flid)
	{
		m_flidDateModified = flid;
	}

	void SetSizeString(const achar * pszSize)
	{
		m_strSize.Assign(pszSize);
	}

protected:
	//:> This method must be implemented by the subclass.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp)
	{
		return SuperClass::OnInitDlg(hwndCtrl, lp);
	}

//	virtual bool OnHelpInfo(HELPINFO * phi);

	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	bool ShowChildDlg(int itab);

	Vector<AfDialogViewPtr> m_vqdlgv;
	int m_itabCurrent;
	int m_itabInitial;
	HWND m_hwndTab;
	int m_dxsClient;
	int m_dysClient;
	AfLpInfo * m_plpi;
	HICON m_hicon;
	int m_nDepth;
	int m_nDispOpt;
	int m_wsMagic;
	bool m_fsorted;
	bool m_fdup;
	StrApp m_strName;
	StrApp m_strAbbr;
	StrApp m_strType;
	StrApp m_strLocation;
	StrApp m_strSize;
	StrApp m_strWndCaption;
	HVO m_hvoObj;
	StrApp m_strDesc;
	StrApp m_strHelpF;
	int m_flidDateCreated;
	int m_flidDateModified;
	unsigned int m_ctidName;
};


/*----------------------------------------------------------------------------------------------
	FieldWorks Properties dialog.

	@h3{Hungarian: fwpd}
----------------------------------------------------------------------------------------------*/
class FwPropDlg : public PropertiesDlg
{
	typedef PropertiesDlg SuperClass;
public:
	FwPropDlg();

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
};
typedef GenSmartPtr<FwPropDlg> FwPropDlgPtr;



/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the General tab for language project
	properties.

	@h3{Hungarian: genp}
----------------------------------------------------------------------------------------------*/
class GeneralPropDlgTab : public AfDialogView
{
	typedef AfDialogView SuperClass;
public:
	GeneralPropDlgTab(PropertiesDlg * ppropd, unsigned int ctidName);
	~GeneralPropDlgTab();

	void EnableLocation(bool fEnable)
	{
		m_fLocationEnb = fEnable;
	}

	void EnableSize(bool fEnable)
	{
		m_fSizeEnb = fEnable;
	}

	void EnableModified(bool fEnable)
	{
		m_fModifiedEnb = fEnable;
	}

	void EnableDescription(bool fEnable)
	{
		m_fDescriptionEnb = fEnable;
	}

	virtual bool Apply();

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	void GetDateString(int flid, StrAppBuf & strb);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	void FixString(StrAppBufHuge & str);

	PropertiesDlg * m_ppropd;
	HFONT m_hfontLarge;
	bool m_fInitialized;
	bool m_fLocationEnb;
	bool m_fSizeEnb;
	bool m_fModifiedEnb;
	bool m_fDescriptionEnb;
	unsigned int m_ctidName;
};
typedef GenSmartPtr<GeneralPropDlgTab> GeneralPropDlgTabPtr;


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\MkCustomNb.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif // !GENERALPROPDLG_H_INCLUDED
