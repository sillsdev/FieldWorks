/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwStylesDlg.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Header file for the IFwCppStylesDlg interface implementation.

	FwCppStylesDlg : IFwCppStylesDlg
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FWSTYLESDLG_H_INCLUDED
#define FWSTYLESDLG_H_INCLUDED

namespace TestCmnFwDlgs
{
	class TestFwStylesDlg;
};

/*----------------------------------------------------------------------------------------------
	Cross-Reference: ${IFwCppStylesDlg}

	@h3{Hungarian: zfwst)
----------------------------------------------------------------------------------------------*/
class FwCppStylesDlg : public IFwCppStylesDlg
{
public:
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	// IFwCppStylesDlg methods.
	STDMETHOD(put_DlgType)(StylesDlgType sdt);
	STDMETHOD(put_ShowAll)(ComBool fShowAll);
	STDMETHOD(put_SysMsrUnit)(int nMsrSys);
	STDMETHOD(put_UserWs)(int wsUser);
	STDMETHOD(put_HelpFile)(BSTR bstrHelpFile);
	STDMETHOD(put_TabHelpFileUrl)(int tabNum, BSTR bstrHelpFileUrl);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);
	STDMETHOD(put_ParentHwnd)(DWORD hwndParent);
	STDMETHOD(put_CanDoRtl)(ComBool fCanDoRtl);
	STDMETHOD(put_OuterRtl)(ComBool fOuterRtl);
	STDMETHOD(put_FontFeatures)(ComBool fFontFeatures);
	// TODO: Add put_OneDefaultFont
	STDMETHOD(putref_Stylesheet)(IVwStylesheet * pasts);
	STDMETHOD(SetApplicableStyleContexts)(int * rgnContexts, int cpnContexts);
	STDMETHOD(put_CanFormatChar)(ComBool fCanFormatChar);
	STDMETHOD(put_OnlyCharStyles)(ComBool fOnlyCharStyles);
	STDMETHOD(put_StyleName)(BSTR bstrStyleName);
	STDMETHOD(put_CustomStyleLevel)(int level);
	STDMETHOD(SetTextProps)(ITsTextProps ** rgpttpPara, int cttpPara,
		ITsTextProps ** rgpttpChar, int cttpChar);
	STDMETHOD(put_RootObjectId)(int hvoRootObj);
	STDMETHOD(SetWritingSystemsOfInterest)(int * rgws, int cws);
	STDMETHOD(putref_LogFile)(IStream * pstrmLog);
	STDMETHOD(putref_HelpTopicProvider)(IHelpTopicProvider * phtprov);
	STDMETHOD(put_AppClsid)(GUID clsidApp);

	STDMETHOD(ShowModal)(int * pnResult);
	STDMETHOD(GetResults)(BSTR * pbstrStyleName, ComBool * pfStylesChanged, ComBool * pfApply,
		ComBool * pfReloadDb, ComBool * pfResult);

protected:
	FwCppStylesDlg();
	~FwCppStylesDlg();

	void SetupForDoModal();
	void DoModalDialog(int * pncid);
	void GetModalResults(int ncid);

	AfStylesDlgPtr m_qafsd;

	StylesDlgType m_sdt;
	bool m_fShowAll;
	MsrSysType m_nMsrSys;
	int m_wsUser;
	StrApp m_strHelpFile;
	ILgWritingSystemFactoryPtr m_qwsf;
	IVwStylesheetPtr m_qvss;
	StrApp m_strTabHelpFileUrl[AfStylesDlg::kcdlgv];

	HWND m_hwndParent;
	bool m_fCanDoRtl;
	bool m_fOuterRtl;
	bool m_fFontFeatures;
	bool m_f1DefaultFont;
	TtpVec m_vqttpPara;
	TtpVec m_vqttpChar;
	bool m_fCanFormatChar;
	bool m_fOnlyCharStyles;
	StrUni m_stuStyleName;
	int m_hvoRootObj;
	Vector<int> m_vwsAvailable;
	IStreamPtr m_qstrmLog;
	Vector<int> m_vApplicableContexts;
	IHelpTopicProviderPtr m_qhtprov;
	GUID m_clsidApp;

	bool m_fStylesChanged;
	bool m_fApply;
	bool m_fReloadDb;

	bool m_fResult;

	int m_nCustomStyleLevel;

	int m_cref;

	friend class TestCmnFwDlgs::TestFwStylesDlg;

	friend class FwExportDlg;
	friend class OpenFWProjectDlg;
	// Shared with other classes in this DLL.
	static long s_cFwStylesDlg;
};
DEFINE_COM_PTR(FwCppStylesDlg);

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*FWSTYLESDLG_H_INCLUDED*/
