/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfContextHelp.h
Responsibility: Darrell Zook
Last reviewed:

	Provides context-sensitive help for the application. When the user clicks on the question
	mark icon in a dialog box or the main window, he gets a special mouse cursor. When the
	user clicks the mouse again, a small popup balloon help window will be displayed, which
	gives information about the control or window the user clicked on. The user can then click
	or type any key, and this balloon window disappears.

	This file contains class declarations for the following classes:
		ContextHelpVc : VwBaseVc - This is the view constructor for the other two classes.
		ToolTipVc : VwBaseVc - This is the view constructor for an AfContextHelpWnd.
		AfContextHelpWnd : AfVwWnd - This is the popup window that contains the help text.
		AfToolTipWnd : AfVwWnd - This is the popup window that contains the tooltip text.

Usage:
	Context-sensitive help in a dialog
	----------------------------------
	In the resource file, define a string in the string resource with the same ID as each
	dialog control, e.g.,

		STRINGTABLE DISCARDABLE
		BEGIN
			kctidListTypeBullet	"Formats the paragraph as a bulleted list."
			kctidListTypeNumber	"Formats the paragraph as a numbered list."
			...
		END

	Define the dialog to have both the DS_CONTEXTHELP and the WS_EX_CONTEXTHELP styles.
	Note that WS_EX_CONTEXTHELP is an extended style.

	Context-sensitive help in the main window
	-----------------------------------------
	Override AfWnd::GetHelpStrFromPt and create a TsString based on the location of the
	mouse cursor when the user clicked on the window.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFCONTEXTHELP_H
#define AFCONTEXTHELP_H 1

class ContextHelpVc;
typedef GenSmartPtr<ContextHelpVc> ContextHelpVcPtr;

class ToolTipVc;
typedef GenSmartPtr<ToolTipVc> ToolTipVcPtr;

class AfContextHelpWnd;
DEFINE_COM_PTR(AfContextHelpWnd);

class AfToolTipWnd;
DEFINE_COM_PTR(AfToolTipWnd);


/*----------------------------------------------------------------------------------------------
	The main view constructor for the context help window.
	Hungarian: chvc.
----------------------------------------------------------------------------------------------*/
class ContextHelpVc : public VwBaseVc
{
	typedef VwBaseVc SuperClass;

public:
	// IVwViewConstructor methods.
	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);

	enum
	{
		khvoCaption = 1000,
		kflidCaption = 1001,

		kfrCaption = 0,
	};
};


/*----------------------------------------------------------------------------------------------
	The main view constructor for the tooltip window.
	Hungarian: ttvc.
----------------------------------------------------------------------------------------------*/
class ToolTipVc : public VwBaseVc
{
	typedef VwBaseVc SuperClass;

public:
	// IVwViewConstructor methods.
	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);

	enum
	{
		khvoCaption = 1000,
		kflidCaption = 1001,

		kfrCaption = 0,
	};
};


/*----------------------------------------------------------------------------------------------
	Provides the context-sensitive help window. This class takes care of creating the window,
	sizing it to fit the text, displaying the text, and destroying the window after a user
	action such as a mouse click.

	Hungarian: chw
----------------------------------------------------------------------------------------------*/
class AfContextHelpWnd : public AfVwWnd
{
	typedef AfVwWnd SuperClass;

public:
	AfContextHelpWnd();

	// Create and show the window
	void Create(HWND hwndPar, HELPINFO * phi, bool fHorizCenter = true);
	void Create(HWND hwndPar, ITsString * ptss, Point pt, bool fHorizCenter = true);

	void ConvertFormatting(StrUni & stu, ITsString ** pptss);

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb);
	virtual void HandleSelectionChange(IVwSelection * pvwsel)
		{ } // Do nothing.
	virtual void HandleKeyboardChange(IVwSelection * pvwsel, int nLangID)
		{ } // Do nothing.

	enum
	{
		kdxpMax = 200,
		kdzpMargin = 4,
		kdzpShadow = 6,
	};

protected:
	// Do nothing. NOTE: Don't call the superclass.
	virtual void PreCreateHwnd(CREATESTRUCT & cs)
		{ }
	// Do nothing. NOTE: Don't call the superclass.
	virtual void MakeSelectionVisible1()
		{ }
	virtual void ScrollSelectionNearTop1()
		{ }
	virtual COLORREF GetWindowColor()
		{ return ::GetSysColor(COLOR_INFOBK); }
	virtual int GetHorizMargin()
		{ return 0; }
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnPaint(HDC hdcDef);

	// Attributes
	HWND m_hwndParent;
	IVwCacheDaPtr m_qvcd;
	// For some reason, when the help window is created for an item on a menu, the system
	// takes the capture away from this window after we've already set it once. This variable
	// will be set to true for the lifetime of the help window, so if we get a window message
	// and m_fSetCapture is true, we recapture the window if we need to.
	bool m_fSetCapture;
};


/*----------------------------------------------------------------------------------------------
	Provides a tooltip window. This class takes care of creating the window,
	sizing it to fit the text and displaying the text.

	Hungarian: ttw
----------------------------------------------------------------------------------------------*/
class AfToolTipWnd : public AfVwWnd
{
	typedef AfVwWnd SuperClass;

public:
	AfToolTipWnd();

	// Create and show the window
	void Create(HWND hwndPar, ITsString * ptss, Point pt, bool fHorizCenter = true);

	void SubclassToolTip(HWND hwndToolTip);
	void UpdateText(ITsString * ptss);

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb);
	virtual void HandleSelectionChange(IVwSelection * pvwsel)
		{ } // Do nothing.
	virtual void HandleKeyboardChange(IVwSelection * pvwsel, int nLangID)
		{ } // Do nothing.

	enum
	{
		kdxpMargin = 2,
	};

protected:
	// Do nothing. NOTE: Don't call the superclass.
	virtual void PreCreateHwnd(CREATESTRUCT & cs)
		{ }
	// Do nothing. NOTE: Don't call the superclass.
	virtual void MakeSelectionVisible1()
		{ }
	virtual void ScrollSelectionNearTop1()
		{ }
	virtual COLORREF GetWindowColor()
		{ return ::GetSysColor(COLOR_INFOBK); }
	virtual int GetHorizMargin()
		{ return 0; }
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnPaint(HDC hdcDef);

	// Attributes
	HWND m_hwndParent;
	IVwCacheDaPtr m_qvcd;
};


#endif // !AFCONTEXTHELP_H
