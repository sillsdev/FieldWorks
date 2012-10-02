/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpWrSysDlg.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Manages a dialog to allow editing of the list of encodings, and their rendering information.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef WRSYS_DLG_INCLUDED
#define WRSYS_DLG_INCLUDED 1

class WpWrSysDlg;
typedef GenSmartPtr<WpWrSysDlg> WpWrSysDlgPtr;

class WpNewWsDlg;
typedef GenSmartPtr<WpNewWsDlg> WpNewWsDlgPtr;

class WpDelEncDlg;
typedef GenSmartPtr<WpDelEncDlg> WpDelEncDlgPtr;

class WpDa;

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Writing System Set-up dialog.
----------------------------------------------------------------------------------------------*/
class WpWrSysDlg : public AfDialog
{
	typedef AfDialog SuperClass;

public:
	WpWrSysDlg();
	~WpWrSysDlg();

	void Init(int encInit, bool fGrLog)
	{
		m_fGrLog = fGrLog;
		m_wsInit = encInit;
		m_qrenengSel = NULL;
	}

	void ModifyEncodings(WpDa * pda);

	bool RenderingChanged()
	{
		return m_fRendChngd;
	}

	enum {
		kMaxFeature = FmtFntDlg::kMaxFeature,
		kGrLangFeature = FmtFntDlg::kGrLangFeature,		// Graphite built-in 'lang' feature
	};

	// list of selected font features; hungarian: fl
	struct FeatList
	{
		// This array holds the actual setting values, not the indices into the list of values.
		// However, it is indexed by feature INDEX, not feature ID.
		int rgn[kMaxFeature];

		void Init()
		{
			for (int i = 0; i < kMaxFeature; i++)
				rgn[i] = INT_MAX; // default
		}
	};

	struct WsData	// hungarian: encdat
	{
		int m_ws;			// id
		StrUni m_stuName;	// name
		StrUni m_stuDescr;	// description
		int m_rt;			// renderer type
		int m_kt;			// keyboard type
		bool m_fRtl;		// right-to-left
		int m_iFont;		// index of font from m_vstrAllFonts
		// TODO SharonC: may need a separate string to hold a font name that is not in the list
		StrAnsi m_staWs;
		StrApp m_strID; // name if there, otherwise code
		int m_iLangId;		// the index of the lang ID in the list, not the ID itself
		StrUni m_stuKeymanKeyboard;
		StrUni m_stuSpecInfo;
		FeatList m_fl;

		WsData()
		{
			m_ws = 0;
			m_rt = 0;
			m_kt = 0;
			m_fRtl = false;
			m_iFont = -1;
			m_iLangId = -1;
			m_fl.Init();
		}
	};

	// For putting the focus back on the combo boxes after an error:
	bool CmdFocusFonts()
	{
		::SetFocus(::GetDlgItem(m_hwnd, kctidFont));
		return true;
	}
	bool CmdFocusKeyboardType()
	{
		::SetFocus(::GetDlgItem(m_hwnd, kctidKeyboardType));
		return true;
	}
	bool CmdFocusLangId()
	{
		::SetFocus(::GetDlgItem(m_hwnd, kctidLangId));
		return true;
	}

protected:

	// return result of superclass method
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp); // init controls
	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);


	enum	// renderer types
	{
		krtStandard = 0,
		krtGraphite = 1,
		krtUniscribe = 2,

		krtLim
	};

	enum	// keyboard types
	{
		kktStandard = 0,
		kktKeyMan = 1,

		kktLim
	};

	typedef enum
	{
		katRead = KEY_READ,
		katWrite = KEY_WRITE,
		katBoth = katRead | katWrite,
	} AccessType;

	void InitEncList();
	void GetAvailableFonts();
	int AllFontsIndex(SmartBstr sbstr);
	void UpdateControlsForSel(int iwsSel);
	void UpdateRenderingControls(int iwsSel, int * pgrfsdc = NULL);
	void UpdateKeyboardControls(int iwsSel, int kt);
	void AddNewEncoding();
	void DeleteEncoding(int iwsSel);
	void InitRenderer(WsData * pwsdat, IRenderEnginePtr & qreneng);
	void InitCurrentRenderer(int iwsSel);
	void MakeCurrFeatSettingsDefault(int iwsSel, bool fInit);
	StrUni GenerateFeatureString(IRenderEngine * preneng, WsData * pwsdat);
	void ParseFeatureString(StrUni stu, IRenderEngine * preneng, WsData * pwsdat);
	void CreateFeaturesMenu(WsData * pwsdat);
	bool CmdFeaturesPopup(Cmd * pcmd);

protected:

	//	member variables
	int m_cws;
	WsData * m_rgencdat;

	Vector<StrApp> m_vstrAllFonts;

	IRenderEnginePtr m_qrenengSel;  // copy of current rendering engine being modified
	bool m_fRendChngd;	// true if any of the renderer items were modified

	// The following is used to record the current font feature settings for each Graphite font.
	// This is so if the user is switching among fonts, they don't have to reset the features
	// each time.
	// This feature is disabled; see MakeCurrFeatSettingsDefault.
	//Vector<FeatList> m_vflGrDefFeats;

	HMENU m_hmenuFeatures;
	Vector<int> m_vnFeatMenuIDs;

	bool m_fGrLog;	// how to initialize logging for Graphite engines
	int m_wsInit;	// initially selected writing system

	HWND m_hwndParent;

	CMD_MAP_DEC(WpWrSysDlg);

	bool FontsComboOk(HWND hwndFocus, int * piFont);
	bool KeyboardComboOk(HWND hwndFocus, int * pi);
	bool LangIdComboOk(HWND hwndFocus, int * pi);
};


/*----------------------------------------------------------------------------------------------
	New Writing system dialog
----------------------------------------------------------------------------------------------*/
class WpNewWsDlg : public AfDialog
{
	typedef AfDialog SuperClass;
public:
	WpNewWsDlg()
	{
		m_rid = kridNewWs;
		m_pszHelpUrl = _T("Advanced_Tasks\\Writing_Systems\\Add_a_New_Writing_System.htm");
	}

	StrApp NewEncStr()
	{
		return m_strEnc;
	}

	void SetWsData(WpWrSysDlg::WsData * prgencdat, int cwsdat)
	{
		m_prgencdat = prgencdat;
		m_cwsdat = cwsdat;
	}

protected:

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);

	virtual bool OnCancel()
	{
		return SuperClass::OnCancel();
	}
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);

protected:
	StrApp m_strEnc;
	WpWrSysDlg::WsData * m_prgencdat;
	int m_cwsdat;
};


/*----------------------------------------------------------------------------------------------
	Delete Writing system dialog
----------------------------------------------------------------------------------------------*/
class WpDelEncDlg : public AfDialog
{
	typedef AfDialog SuperClass;

public:
	WpDelEncDlg()
	{
		m_rid = kridDeleteWs;
	}

protected:

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	virtual bool OnApply(bool fClose)
	{
		return SuperClass::OnApply(fClose);
	}

	virtual bool OnCancel()
	{
		return SuperClass::OnCancel();
	}

	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
	{
		return SuperClass::OnNotifyChild(id, pnmh, lnRet);
	}

public:
	void SetDelEnc(int iws, StrApp str)
	{
		m_iws = iws;
		m_str = str;
	}

protected:
	int m_iws;	// writing system to delete
	StrApp m_str;
};

#endif // !WRSYS_DLG_INCLUDED
