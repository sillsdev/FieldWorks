/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfStylesDlg.h
Responsibility: LarryW
Last reviewed: never

Description:
	This file contains the class declaration for the Format/Style dialog.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef AFSTYLESDLG_INCLUDED
#define AFSTYLESDLG_INCLUDED 1
/*:End Ignore*/

// This is the string "Normal", to be used wherever the normal style's name is needed. Do not
// use a resource or hard-code for this name, as it is used in the database, and any change
// should be possible from as few places as possible.
extern const wchar * g_pszwStyleNormal;

/*----------------------------------------------------------------------------------------------
	This class keeps track of all information related to a style, for use by the Format/Styles
	dialog and its tabs.

	@h3{Hungarian: styi}
----------------------------------------------------------------------------------------------*/
class StyleInfo
{
public:
	// C'tor - simply set all values
	StyleInfo()
		: m_hvoStyle(0), m_st(kstParagraph), m_hvoBasedOn(0), m_hvoNext(0),
		m_fBuiltIn(false), m_fModified(false), m_fDirty(false), m_fDeleted(false),
		m_fJustCreated(false), m_nContext(0), m_nStructure(0), m_nFunction(0), m_nUserLevel(0)
	{}

	HVO m_hvoStyle;			// HVO of style.
	StrUni m_stuName;		// Name of style.
	StrUni m_stuUsage;		// Usage information to display to user.
	StrUni m_stuNameOrig;	// original name, to facilitate process of renaming
	StyleType m_st;			// StyleType (currently kstParagraph or kstCharacter).
	HVO m_hvoBasedOn;		// HVO of BasedOn style.
	HVO m_hvoNext;			// HVO of Next style.
	StrUni m_stuShortcut;	// Not implemented yet.
	ITsTextPropsPtr m_qttp;	// Props that define the style's behavior.
	bool m_fBuiltIn;		// true if style is predefined
	bool m_fModified;		// true if (predefined) style was modified by user
	bool m_fDirty;			// Has been modified, needs saving to style sheet if OK or Apply.
	bool m_fDeleted;		// Has been deleted by the user.
	bool m_fJustCreated;	// True if style created since dialog activated.
	int m_nContext;			// Style context
	int m_nStructure;		// Style structure
	int m_nFunction;		// Style function
	int m_nUserLevel;		// Style user level
};

typedef GenSmartPtr<StyleInfo> StyleInfoPtr; // Hungarian qstyi.
typedef Vector<StyleInfo> StyleInfoVec; // Hungarian vstyi.

namespace TestCmnFwDlgs
{
	class TestFwStylesDlg;
};

/*----------------------------------------------------------------------------------------------
	Format Styles dialog shell class.

	@h3{Hungarian: afsd}
----------------------------------------------------------------------------------------------*/
class AfStylesDlg : public AfDialog
{
	typedef AfDialog SuperClass;
	friend class FmtGenDlg;
	friend class FmtParaDlg;
	friend class AfStyleFntDlg;
	friend class FmtBulNumDlg;
	friend class FmtBdrDlgPara;
	friend class ModFldSetDlg;

public:
	// Identifier of normal style
	static const int kiNormalStyle;
	static const int kcdlgv = 5; // The number of tabs for a Format/Styles dialog.

	// Generic constructor.
	AfStylesDlg();
	// Generic destructor.
	~AfStylesDlg();
	// Initialize a new Format/Styles dialog; called by AdjustTsTextProps(...).
	void Initialize(IVwStylesheet * pasts, bool fCanDoRtl, bool fOuterRtl, bool fFontFeatures,
		bool f1DefaultFont, bool * pfReloadDb);

	// Split AdjustTsTextProps() into 3 methods to facilitate testing.
	void SetupForAdjustTsTextProps(bool fCanDoRtl, bool fOuterRtl, bool fFontFeatures,
		bool f1DefaultFont,
		IVwStylesheet * past, TtpVec & vqttpPara, TtpVec & vqttpChar, bool fCanFormatChar,
		bool & fReloadDb, Vector<int> & vwsAvailable, int hvoRootObj, IStream * pstrmLog,
		StrUni stuStyleName, bool fOnlyCharStyles);
	int DoModalForAdjustTsTextProps(HWND hwnd);
	bool ResultsForAdjustTsTextProps(int ncid, StrUni * pstuStyleName, bool & fStylesChanged,
		bool & fApply);

	// AfVwRootSite calls this method to initialize the format styles dialog.
	bool AdjustTsTextProps(HWND hwnd, bool fCanDoRtl, bool fOuterRtl, bool fFontFeatures,
		bool f1DefaultFont,
		IVwStylesheet * past, TtpVec & vqttpPara, TtpVec & vqttpChar, bool fCanFormatChar,
		Vector<int> & vwsAvailable, int hvoRootObj, IStream * pstrmLog,
		StrUni * pstuStyleName, bool & fStylesChanged, bool & fApply, bool & fReloadDb);

	// The Shoebox import wizard calls this method to initialize the format styles dialog in
	// order to allow the user to add more styles to fit data being imported.
	static bool EditImportStyles(HWND hwnd, bool fCanDoRtl, bool fOuterRtl, bool fFontFeat,
		bool f1DefaultFont, IVwStylesheet * past, ILgWritingSystemFactory * pwsf, int stType,
		StrUni & stuStyleName, bool & fStylesChanged, const GUID * pclsidApp = NULL,
		int hvoRootObj = 0);

	void RenameAndDeleteStyles(int hvoRootObj, Vector<StrUni> & vstuOldNames,
		Vector<StrUni> & vstuNewNames, Vector<StrUni> & vstuDelNames, IStream * pstrmLog,
		IVwOleDbDa * podde, const GUID * pclsidApp);

	// Sets the initial values when created to modify char styles.
	void SetChrStyle(StrUni stuCharStyle);

	bool CanDoRtl()
	{
		return m_fCanDoRtl;
	}

	bool OuterRtl()
	{
		return m_fOuterRtl;
	}

	// Specifies a set of style contexts that should be used to determine which styles can be
	// applied. Selecting any style having a context not in this array will cause the Apply
	// button to be grayed out.
	void SetApplicableStyleContexts(Vector<int>& vContexts)
	{
		vContexts.CopyTo(m_vApplicableContexts);
	}

	//:>****************************************************************************************
	//:>	Access methods.
	//:>****************************************************************************************

	// Return true if a style is selected in the style list.
	bool StyleIsSelected()
	{
		return m_istyiSelected > -1;
	}
	// Store the system's measurement type for use by subdialogs.
	void SetMsrSys(MsrSysType nMsrSys)
	{
		m_nMsrSys = nMsrSys;
		if (m_rgdlgv[2])
			reinterpret_cast<FmtParaDlg *>(m_rgdlgv[2].Ptr())->SetMsrSys(nMsrSys);
	}
	// Return the system's measurement type for use by subdialogs.
	MsrSysType GetMsrSys()
	{
		return m_nMsrSys;
	}
	// Store the program's user interface writing system for use by subdialogs.
	void SetUserWs(int encUser)
	{
		m_wsUser = encUser;
	}
	// Return the program's user interface writing system for use by subdialogs.
	int GetUserWs()
	{
		return m_wsUser;
	}
	// Store the program's help file (relative) pathname.
	void SetHelpFile(const achar * pszHelp)
	{
		m_pszHelpFile = pszHelp;
	}
	// Sets the help file url for the given tab
	void SetTabHelpFileUrl(const int tabNum, const achar * pszHelpUrl)
	{
		m_rgpszTabDlgHelpUrl[tabNum] = pszHelpUrl;
	}
	// Return the stored help file pathname.
	const achar * GetHelpFile()
	{
		return m_pszHelpFile;
	}
	void SetCustomUserLevel(int level)
	{
		m_nCustomStyleLevel = level;
	}
	void SetLgWritingSystemFactory(ILgWritingSystemFactory * pwsf)
	{
		m_qwsf = pwsf;
	}
	void SetHelpTopicProvider(IHelpTopicProvider * phtprov)
	{
		m_qhtprov = phtprov;
	}
	void GetLgWritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
	{
		AssertPtr(ppwsf);
		Assert(*ppwsf == NULL);
		*ppwsf = m_qwsf;
		AddRefObj(*ppwsf);
	}
	void SetAppClsid(const GUID * pclsidApp)
	{
		m_pclsidApp = pclsidApp;
	}
	void SetRootObj(int hvo)
	{
		m_hvoRootObj = hvo;
	}

	// Return a reference to the selected style, m_vstyi[m_istyiSelected].
	StyleInfo & SelectedStyle();
	// Return the name of the selected style.
	StrUni GetNameOfSelectedStyle();
	// Return the name of the style identified by the given HVO (handle to viewable object).
	StrUni GetNameOfStyle(HVO hvoStyle);
	// Return the index of the style in m_vstyi whose HVO is hvoStyle.
	int GetIndexFromHVO(HVO hvoStyle) const;
	// Return the index of the style in m_vstyi with name stuName.
	int GetIndexFromName(const StrUni& stuName) const;
	// Get the BasedOn HVO of the style in m_vstyi whose HVO is hvoStyle.
	HVO GetBasedOnHvoOfStyle(HVO hvoStyle);
	// Get the hvo of the style in m_vstyi whose name is stuName.
	HVO GetHvoOfStyleNamed(StrUni stuName);
	// Return true if styi is directly or indirectly based on hvoNewBasedOn.
	bool IsBasedOn(StyleInfo * pstyi, HVO & hvoNewBasedOn);
	// Answer true if the given style name is one that is protected from fundamental
	// changes by the user.
	virtual bool IsStyleProtected(const StrUni stuStyleName);

	//:>****************************************************************************************
	//:>	Use these methods to set StyleInfo member variables whenever the m_fDirty variable
	//:>	needs to be set.
	//:>****************************************************************************************

	// Set the name of styi to the given name; set m_fDirty to true. Return false if another
	// style already has the suggested name; otherwise return true.
	bool SetName(StyleInfo & styi, const StrUni & stuNewName);
	// Make styi be based on hvoNewBasedOn; set m_fDirty to true. Return false if basing this
	// style on the style identified as hvoNewBasedOn results in a circular route; otherwise
	// return true.
	bool SetBasedOn(StyleInfo & styi, HVO & hvoNewBasedOn);
	// Set the next style for this paragraph style to be hvoNewNext and return true;
	// set m_fDirty to true.
	bool SetNext(StyleInfo & styi, HVO & hvoNewNext);
	// Set the TsTextProps for styi to be pttp; set m_fDirty to true. Return true.
	bool AttachPttp(StyleInfo & styi, ITsTextProps * pttp);

	// This is called by the format tab (i.e. FmtGenDlg) to notify the AfStylesDlg that the
	// user changed the based on style. This allows the AfStylesDlg to determine whether or
	// not the Apply button should be enabled or not (determined by the based on style's
	// context). This also is done in the SetBasedOn method but that only gets called by
	// the FmtGenDlg when all the dialog values are being saved, and at that point, it's too
	// late.
	void BasedOnStyleChangeNotification(HVO hvoNewBasedOn);

	//:>****************************************************************************************
	//:>	Message handling.
	//:>****************************************************************************************

	// Respond to the Apply button being pressed.
	bool OnApply(bool fClose);
	// Close the dialog in response to the Cancel button being pressed.
	bool OnCancel();
	// Handle the What's This? help functionality for both the main dialog and the tab panes.
	bool DoHelpInfo(HELPINFO * phi, HWND hwnd);

protected:
	// array of help file URLs for all of the tab dialogs
	const achar * m_rgpszTabDlgHelpUrl[kcdlgv];
	// We may need to preserve StrApp objects so that the internal tab page dialog pointers
	// remain valid.
	StrApp m_rgstrTabHelpUrls[kcdlgv];

	// Switch to showing tab itab and style istyi. If fSave is true, save the values from the
	// current style/tab combination. If some kind of validation fails when saving the current
	// style/tab info, return false.
	bool UpdateTabCtrl(int itab, int istyi, bool fSave = true);
	// Gets a style to replace the given deleted style
	virtual StrUni GetStyleForDeletedStyle(StyleInfo styiDeletedStyle);
	// Process window messages for the listview of styles.
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	// Process notifications from the user.
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	// Open the help dialog for the currently selected tab.
	virtual bool OnHelp();
	// Handle the What's This? help functionality.
	virtual bool OnHelpInfo(HELPINFO * phi);
	// Add a new style in response to the Add button being pressed.
	virtual bool CmdAdd(Cmd * pcmd);
	// Copy a style in response to the Copy button being pressed.
	virtual bool CmdCopyStyle();
	// Delete the selected style in response to the Delete button being pressed.
	virtual bool CmdDel();
	// Assign values to the member variables of pstyi, with the type "stype". Return true if
	// successful.
	virtual bool MakeNewStyi(const StyleType stype, StyleInfo * pstyi);
	// Copy a style to a new one
	virtual bool CopyStyi(const StyleInfo & styiSrc, StyleInfo & styiDest);
	// Copy info from m_pasts to our local vector, m_vstyi.
	void CopyToLocal();
	// If anything changed, copy the changes back to the database and style sheet.
	bool CopyToDb();
	// Compute the inherited properties for the style in m_vstyi[istyi] based on ppttp.
	void GetInheritedProperties(int istyi, ITsTextProps ** ppttp);

	// Determine if apply button should be enabled or disabled
	bool CanApplyStyle(int istyi);

	// Update the controls for the selected tab to show changed values.
	bool ShowChildDlg(int itab, bool fSave = true);
	// Initialize the Styles listview control using m_vstyi.
	bool UpdateStyleList();
	// Add styiNew to the list of styles.
	void InstallStyle(StyleInfo & styiNew);
	// Update the Style name.
	virtual bool OnEndLabelEdit(NMLVDISPINFO * plvdi, long & lnRet);
	// Returns true if style should not be shown in dialog
	// The default implementation always returns false and so shows all styles.
	virtual bool SkipStyle(int iStyle) const
	{
		return false;
	}
	// create the sub dialogs
	virtual void SetupTabs();
	// Set initial values for the dialog controls, prior to displaying the dialog.
	virtual void SetDialogValues();


	void FullyInitializeNormalStyle(IVwStylesheet * past);
	static void SetIntPropIfBlank(ITsPropsBldr * ptpb, int tpt, int nVar, int nVal);
//	static void WriteFntProp(OLECHAR * & pch, int tpt, int nVar, int nVal, int & cprop);

	//:>----------------------------------------------------------------------------------------
	//:>	Member variables.
	AfDialogViewPtr m_rgdlgv[kcdlgv]; // Array of five tabs (each an AfDialogView).
	int m_ctabVisible; // number of tabs visible.
	int m_itabCurrent; // Index of current tab.
	int m_ihvoNextNewStyi; // Temporary hvo for next newly created style. The real hvo will be
							// generated when the style is added to the database.
	int m_nCharStyles; // Number of new character styles.
	int m_nParaStyles; // Number of new paragraph styles.

	//:> Variables used only for initialization.
	int m_dxsClient; // x position of client window; used for initialization.
	int m_dysClient; // y position of client window; used for initialization.

	IVwStylesheet * m_pasts; // Pointer to an IVwStylesheet.
	StyleInfoVec m_vstyi; // This vector stores a temporary copy of all the relevant info in
							// the style sheet, so that we don't change the real style sheet
							// in case the user clicks Cancel.
	int m_istyiSelected; // Index of style contained in m_vstyi.
	ITsTextPropsPtr m_qttpDefault; // TsTextProps which contains the system default properties.

	int m_itabInitial; // Initial tab selection.
	HWND m_hwndTab; // Handle to the tab control.
	HWND m_hwndStylesList; // Handle to the Styles list view control.

	//:> Variables set by AdjustTsTextProps to initialize the dialog.
	StrUni m_stuCharStyleNameOrig; // Character style name when this dialog is opened;
									// set by AdjustTsTextProps to initialize the dialog.
	StrUni m_stuParaStyleNameOrig; // Paragraph style name when this dialog is opened;
									// set by AdjustTsTextProps to initialize the dialog.
	bool m_fCharStyleFound; // true if one character style was found in the selection;
							// set by AdjustTsTextProps to initialize the dialog.
	bool m_fParaStyleFound; // true if one paragraph style was found in the selection;
							// set by AdjustTsTextProps to initialize the dialog.

	//:> Variables set as the dialog is used. AdjustTsTextProps uses them after the dialog is
	//:> run.
	StrUni m_stuStyleSelected; // This is set if the Apply button is pressed; set as the dialog
								// is used, for later use by AdjustTsTextProps.
	bool m_fStyleChanged; // This is set to true if any of the styles were modified; set as the
							// dialog is used, for later use by AdjustTsTextProps.
	StyleApplyType m_nEnableApply;	// enable or disable the apply button.
	bool m_fOnlyCharStyles;	// true if called from TlsOptDlg.
	//:> Variables that store information needed by UpdateTabCtrl: information from the process
	//:> of initializing a tab that is needed when it is saved.
	ParaPropRec m_xprOrig; // Paragraph properties; used by UpdateTabCtrl.

	bool m_fCanDoRtl;	// true if we want the bidirectional version of the paragraph fmt dlg
	bool m_fOuterRtl;
	bool m_fFontFeatures;	// true if we want a Features button on the Font tab
	bool m_f1DefaultFont;	// e.g., WorldPad
	int m_nCustomStyleLevel;

	StrUni m_stuDefParaChars; // Dummy style name for "no character style at all"

	bool * m_pfReloadDb;

	MsrSysType m_nMsrSys;			// Eliminates dependence on AfApp::Papp()->GetMsrSys().
	int m_wsUser;					// Eliminates dependence on outside semiglobal info.
	const achar * m_pszHelpFile;	// Eliminates dependence on AfApp::Papp()->GetHelpFile().
	ILgWritingSystemFactoryPtr m_qwsf;
	IHelpTopicProviderPtr m_qhtprov;

	Vector<int> m_vwsAvailable;
	int m_hvoRootObj;
	const GUID * m_pclsidApp;
	IStreamPtr m_qstrmLog;
	Vector<int> m_vApplicableContexts;

	bool m_fInLabelEdit; // true while we are editing the label in the list view

	CMD_MAP_DEC(AfStylesDlg);

	friend class TestCmnFwDlgs::TestFwStylesDlg;
};
typedef GenSmartPtr<AfStylesDlg> AfStylesDlgPtr; // Hungarian qafsd.

/*----------------------------------------------------------------------------------------------
	Delete/Rename Styles Warning dialog class.
	Warns user of impending changes to styles, and allows them to cancel.

	@h3{Hungarian: afswd}
----------------------------------------------------------------------------------------------*/
class AfStylesWarningDlg : public AfDialog
{
	typedef AfDialog SuperClass;

public:
	static bool WarnUser(int nDeletes, int nRenames, HWND hwnd, const achar * pszHelpFile);

protected:
	// Constructor.
	AfStylesWarningDlg();
	// Initialize a new dialog.
	void Initialize(int nDeletes, int nRenames, const achar * pszHelpFile);
	// Process message to color red text.
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	// Open the help dialog.
	virtual bool OnHelp();

	int m_nDeletes;
	int m_nRenames;
	const achar * m_pszHelpFile;	// Eliminates dependence on AfApp::Papp()->GetHelpFile().
};
#endif //:> !AFSTYLESDLG_INCLUDED
