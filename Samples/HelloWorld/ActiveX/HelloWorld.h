/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: HelloWorld.h
Responsibility: Darrell Zook
Last reviewed: never

Description:
	This file contains the base classes for Hello World.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef HELLOWORLD_INCLUDED
#define HELLOWORLD_INCLUDED 1

#import "sa control.ocx" no_namespace, named_guids, raw_native_types raw_dispinterfaces


class HwApp;
class HwMainWnd;
class SaCtrlEvent;
typedef GenSmartPtr<HwApp> HwAppPtr;
typedef GenSmartPtr<HwMainWnd> HwMainWndPtr;
typedef ComSmartPtr<SaCtrlEvent> SaCtrlEventPtr;
typedef _DSA SaCtrl;
typedef ComSmartPtr<SaCtrl> SaCtrlPtr;

/*----------------------------------------------------------------------------------------------
	Our Hello World application class.
----------------------------------------------------------------------------------------------*/
class HwApp : public AfApp
{
	typedef AfApp SuperClass;

public:
	HwApp();
	virtual ~HwApp()
		{ } // Do nothing.

	int GetAppNameId()
	{
		return kstidAppName;
	}

protected:
	virtual void Init(void);

	CMD_MAP_DEC(HwApp);
};


/*----------------------------------------------------------------------------------------------
	Our main window frame class.
----------------------------------------------------------------------------------------------*/
class HwMainWnd : public AfMainWnd
{
	typedef AfMainWnd SuperClass;

public:
	virtual ~HwMainWnd()
		{ } // Do nothing.

	virtual void OnReleasePtr();

	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);

protected:
	virtual void PostAttach(void);

	/*------------------------------------------------------------------------------------------
		Message handlers.
	------------------------------------------------------------------------------------------*/
	virtual bool OnClientSize();
	virtual bool CmdFileAbout(Cmd * pcmd);

	enum
	{
		kmskShowMenuBar           = 0x0001,
		kmskShowStandardToolBar   = 0x0002,
		kmskShowInsertToolBar     = 0x0004,
		kmskShowToolsToolBar      = 0x0008,
		kmskShowWindowToolBar     = 0x0010,
	};

	AfWndPtr m_qwndHost;
	SaCtrlPtr m_qctrl;
	SaCtrlEventPtr m_qsce;

	CMD_MAP_DEC(HwMainWnd);
};


#define CLSID_SaCtrlEvent __uuidof(SaCtrlEvent)

class DECLSPEC_UUID("0d2ffb9b-09d8-11d2-a1dd-0000c0be95b5") SaCtrlEvent;

/*----------------------------------------------------------------------------------------------
	This class traps a few events that the Speech Analysis ActiveX control fires.
	To do this, it implements from IDispEventImpl, using the interfaces and GUIDs
	generated from the ActiveX control by the #import statement.
	NOTE: The first argument to the IDispEventImpl template class is a dummy
	integer. It does not matter what number you use here as long as it is positive
	and it matches the first argument to the SINK_ENTRY_EX macros below.

	Hungarian: sca
----------------------------------------------------------------------------------------------*/
class ATL_NO_VTABLE SaCtrlEvent :
	public CComObjectRootEx<CComSingleThreadModel>,
	public IDispEventImpl<1, SaCtrlEvent, &DIID__DSAEvents, &LIBID_SACONTROLLib, 1, 0>
{
public:
	void Init(HwMainWnd * pwndMain, SaCtrl * pctrl)
	{
		AssertPtr(pwndMain);
		AssertPtr(pctrl);
		m_qwndMain = pwndMain;
		m_qctrl = pctrl;
	}

protected:
	DECLARE_NOT_AGGREGATABLE(SaCtrlEvent)

	BEGIN_COM_MAP(SaCtrlEvent)
		COM_INTERFACE_ENTRY_IID(DIID__DSAEvents, SaCtrlEvent)
	END_COM_MAP()

	// Add additional event handlers here if desired. The third argument comes from
	// the SA ActiveX control. It is the DISP_ID of the event within the control.
	// For other ActiveX controls, you will need to somehow find out the DISP_IDs
	// that correspond to the events you want to handle.
	BEGIN_SINK_MAP(SaCtrlEvent)
	   SINK_ENTRY_EX(1, DIID__DSAEvents, kedidOnCursorMoved, OnCursorMoved)
	   SINK_ENTRY_EX(1, DIID__DSAEvents, kedidOnCursorContextMenu, OnCursorContextMenu)
	   SINK_ENTRY_EX(1, DIID__DSAEvents, kedidOnContextMenu, OnContextMenu)
	END_SINK_MAP()

	void __stdcall OnCursorMoved(short nID, long dwPosition);
	void __stdcall OnCursorContextMenu(long x, long y, short nID);
	void __stdcall OnContextMenu(long x, long y);

	HwMainWndPtr m_qwndMain;
	SaCtrlPtr m_qctrl;
};

#endif // HELLOWORLD_INCLUDED