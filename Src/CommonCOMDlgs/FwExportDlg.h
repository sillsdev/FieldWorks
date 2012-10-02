/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwExportDlg.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Header file for the IFwExportDlg interface implementation.

	FwExportDlg : IFwExportDlg
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FWEXPORTDLG_H_INCLUDED
#define FWEXPORTDLG_H_INCLUDED

namespace TestCmnFwDlgs
{
	class TestFwExportDlg;
	class TestFwFldSpec;
};

/*----------------------------------------------------------------------------------------------
	This class allows us to create an ExpLpInfo structure.

	@h3{Hungarian: xdbi}
----------------------------------------------------------------------------------------------*/
class ExpDbInfo : public AfDbInfo
{
public:
	ExpDbInfo();

	virtual void CleanUp();
	virtual AfLpInfo * GetLpInfo(HVO hvoLp);

	void SetObjId(int hvoObj)
	{
		m_hvoObj = hvoObj;
	}

protected:
	int m_hvoObj;
};
typedef GenSmartPtr<ExpDbInfo> ExpDbInfoPtr;

/*----------------------------------------------------------------------------------------------
	This class contains information about a language project needed for export.

	@h3{Hungarian: xlpi}
----------------------------------------------------------------------------------------------*/
class ExpLpInfo : public AfLpInfo
{
public:
	ExpLpInfo();

	virtual bool OpenProject();
	virtual bool LoadProjBasics();

	virtual const OLECHAR * ObjName()
	{
		return m_stuObjName.Chars();
	}

	virtual HVO ObjId()
	{
		return m_hvoObj;
	}

	void SetObjId(int hvoObj)
	{
		m_hvoObj = hvoObj;
	}
protected:
	int m_hvoObj;
	StrUni m_stuObjName;
};
typedef GenSmartPtr<ExpLpInfo> ExpLpInfoPtr;

/*----------------------------------------------------------------------------------------------
	Cross-Reference: ${IFwExportDlg}

	@h3{Hungarian: zfexp)
----------------------------------------------------------------------------------------------*/
class FwExportDlg : public IFwExportDlg
{
public:
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	// IFwExportDlg methods.
	STDMETHOD(Initialize)(DWORD hwndParent, IVwStylesheet * pvss, IFwCustomExport * pfcex,
		GUID * pclsidApp, BSTR bstrRegProgName, BSTR bstrProgHelpFile, BSTR bstrHelpTopic,
		int hvoLp, int hvoObj, int flidSubitems);
	STDMETHOD(DoDialog)(int vwt, int crec, int * rghvoRec, int * rgclidRec, int * pnRet);

protected:
	FwExportDlg();
	~FwExportDlg();

	long m_cref;
	HWND m_hwndParent;
	IVwStylesheetPtr m_qvss;
	IFwCustomExportPtr m_qfcex;
	const CLSID * m_pclsidApp;
	StrApp m_strRegProgName;
	StrApp m_strProgHelpFile; // help file (.chm) for this dialog box
	StrApp m_strHelpTopic; // help topic for this dialog box
	int m_hvoLp;
	int m_hvoObj;
	int m_flidSubitems;

	friend class TestCmnFwDlgs::TestFwExportDlg;
};
DEFINE_COM_PTR(FwExportDlg);

/*----------------------------------------------------------------------------------------------
	Cross-Reference: ${IFwFldSpec}

	@h3{Hungarian: zffsp)
----------------------------------------------------------------------------------------------*/
class FwFldSpec : public IFwFldSpec
{
public:
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	// IFwFldSpec methods.
	STDMETHOD(put_Visibility)(int nVis);
	STDMETHOD(get_Visibility)(int * pnVis);
	STDMETHOD(put_HideLabel)(ComBool fHide);
	STDMETHOD(get_HideLabel)(ComBool * pfHide);
	STDMETHOD(put_Label)(ITsString * ptssLabel);
	STDMETHOD(get_Label)(ITsString ** pptssLabel);
	STDMETHOD(put_FieldId)(int flid);
	STDMETHOD(get_FieldId)(int * pflid);
	STDMETHOD(put_ClassName)(BSTR bstrClsName);
	STDMETHOD(get_ClassName)(BSTR * pbstrClsName);
	STDMETHOD(put_FieldName)(BSTR bstrFieldName);
	STDMETHOD(get_FieldName)(BSTR * pbstrFieldName);
	STDMETHOD(put_Style)(BSTR bstrStyle);
	STDMETHOD(get_Style)(BSTR * pbstrStyle);

protected:
	FwFldSpec();
	~FwFldSpec();

	long m_cref;

	int m_nVis;
	ComBool m_fHide;
	ITsStringPtr m_qtssLabel;
	int m_flid;
	SmartBstr m_sbstrClsName;
	SmartBstr m_sbstrFieldName;
	SmartBstr m_sbstrStyle;

	friend class TestCmnFwDlgs::TestFwFldSpec;
};
DEFINE_COM_PTR(FwFldSpec);

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*FWEXPORTDLG_H_INCLUDED*/
