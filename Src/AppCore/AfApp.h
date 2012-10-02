/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfApp.h
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

Description:
	This file contains class declarations for the following classes:
		FilterMenuNode : GenRefObj - This class represents one node in the popup menu used in
			the Filter dialogs.
		SortMenuNode : GenRefObj - This class represents one node in the popup menu used in
			the Sort Method dialogs.
		AfApp : CmdHandler - This is the main application class. It handles retrieving and
			dispatching window and command messages. It also keeps track of the top-level
			frame windows used in the application.
		AfDbApp : AfApp - This is the main application class for an application that needs
			database support.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AfApp_H
#define AfApp_H 1

#import "FwCoreDlgs.tlb" raw_interfaces_only rename_namespace("FwCoreDlgs")

// Key and button modifier state flags.
enum ModifierState
{
	kfmstNil   = 0x000,
	kfmstCtrl  = 0x0001,
	kfmstShift = 0x0002,
	kfmstAlt   = 0x0004,

	kfmstLBtn  = 0x0008,
	kfmstRBtn  = 0x0010,
	kfmstMBtn  = 0x0020,
};

const int kdxpMinClient = 215;	// Minimum width for client window.
const int kdypMin = 280;		// Minimum height for window (allows 2 windows in 800x600 res.)
const int kdxpMinViewBar = 45;	// Minimum width for view bar.

const int kcmhlApp = 0x7FFFFFFF;	// Make this the last thing in the command handler list.
const int kdyptMinSize = 1;			// Minimum font size (in points).
const int kdyptMaxSize = 1638;		// Maximum font size (in points).

// Macro for extracting the class id ("clid") from a field id ("flid").
#define MAKECLIDFROMFLID(flid) ((flid) / 1000)

typedef enum
{
	// Code in PossChsrDlg::LoadDlgSettings assumes these
	kStyleDisableApply = 0,
	kStyleEnChrApply = 1,
	kStyleEnableApply = 2,
} StyleApplyType;

// This enum is used by AfDbApp::ProcessDBOptions, as its return value.
typedef enum
{
	korvSuccess = 0,		// Success in calling method. Output params are all valid.
	korvNoServer,			// No SQL server found.
	korvNoFWDatabases,		// No FieldWorks databases in the specified server.
	korvInvalidOwner,		// The major object is not owned by the project.
	korvInvalidProjectName,	// Specified project not found.
	korvInvalidObjectName,	// Specified object name not found.
	korvInvalidObject,		// Specified object class not found. (It had no name.)
	korvSQLError,			// Unspecified SQL error.
	korvInvalidDatabaseName,	// Specified database not found.

	korvLim					// Must be last in enum.
} OptionsReturnValue;

class FwSettings;
class AfApp;
class AfDbApp;
class AfMainWnd;
class RecMainWnd;
class FilterMenuNode;
class SortMenuNode;
class FileOpenProjectInfo;
class RecMainWnd;

typedef GenSmartPtr<AfMainWnd> AfMainWndPtr;
typedef GenSmartPtr<FilterMenuNode> FilterMenuNodePtr;
typedef Vector<FilterMenuNodePtr> FilterMenuNodeVec;
typedef ComSmartPtr<IOleDbEncap> IOleDbEncapPtr;
typedef GenSmartPtr<SortMenuNode> SortMenuNodePtr;
typedef Vector<SortMenuNodePtr> SortMenuNodeVec;
typedef GenSmartPtr<FileOpenProjectInfo> FileOpenProjectInfoPtr;


/*************************************************************************************
	Macros to use at the top level of processing a windows message, typically a
	command. This will trap all error conditions and report them to the user using
	MessageBox. The message should indicate to the user if he ought to then quit, but
	he is not forced to.
*************************************************************************************/
#define BEGIN_TOP_LEVEL_ACTION \
	try \
	{ \

#define END_TOP_LEVEL_ACTION \
	} \
	catch (ThrowableSd & thr) \
	{ \
		AfApp::HandleTopLevelError(&thr); \
	} \
	catch (Throwable & thr) \
	{ \
		AfApp::HandleTopLevelError(&thr); \
	} \
	catch (...) \
	{ \
		AfApp::HandleTopLevelError(NULL); \
	} \


/*----------------------------------------------------------------------------------------------
	This "class" is used to store the results of calling the File-Open COM code.
	It really is more like a struct, but it is helpful to have it be dericved from GenRefObj.

	@h3{Hungarian: fopi}
----------------------------------------------------------------------------------------------*/
class FileOpenProjectInfo : public GenRefObj
{
public:
	FileOpenProjectInfo(void)
	{
		// Project information.
		m_fHaveProject = false;
		m_hvoProj = 0;
		m_stuProject = L"";
		m_stuDatabase = L"";
		m_stuMachine = L"";
		m_guid;

		// Subitem information.
		m_fHaveSubitem = false;
		m_hvoSubitem = 0;
		m_stuSubitemName = L"";
	}

	// Project information.
	ComBool m_fHaveProject;
	HVO m_hvoProj;
	StrUni m_stuProject;
	StrUni m_stuDatabase;
	StrUni m_stuMachine;
	GUID m_guid;

	// Subitem infomration.
	ComBool m_fHaveSubitem;
	HVO m_hvoSubitem;
	StrUni m_stuSubitemName;
};


// This enum is only used in the FilterMenuNode class.
typedef enum
{
	kfmntLeaf,
	kfmntField,
	kfmntClass,
} FilterMenuNodeType;


/*----------------------------------------------------------------------------------------------
	This class contains the necessary information for creating a hierarchical popup menu that
	allows the user to select which field to filter on.

	@h3{Hungarian: fmn)
----------------------------------------------------------------------------------------------*/
class FilterMenuNode : public GenRefObj
{
public:
	StrUni m_stuText;				// Text for this menu node.
	FilterMenuNodeType m_fmnt;		// Type of this menu node: Leaf, Field, or Class.
	union
	{
		int m_flid;					// m_fmnt = Leaf or Field: the field id
		int m_clid;					// m_fmnt = Class: the base class id
	};
	int m_proptype;					// type of leaf node: either kfpt__ or kcpt__ value.
	union
	{
		HVO m_hvo;					// m_proptype = kfptPossList: list's database id
		int m_stid;					// m_proptype = kfptEnumList: list's string resource id
		int m_ialiasLastClass;		// m_fmnt = kfmntClass: Used while constructing filter SQL.
	};
	StrUni m_stuAlias;				// Used while constructing filter SQL (SQL table alias).
	FilterMenuNodeVec m_vfmnSubItems;	// Vector of subitems in menu tree

	void AddSortedSubItem(FilterMenuNode * pfmn);
	static void AddSortedMenuNode(FilterMenuNodeVec & vfmn, FilterMenuNode * pfmn);
};

// This enum is only used in the SortMenuNode class.
typedef enum
{
	ksmntClass,
	ksmntField,
	ksmntLeaf,
	ksmntWs,
	ksmntOws,
	ksmntColl,
} SortMenuNodeType;

/*----------------------------------------------------------------------------------------------
	This class contains the necessary information for creating a list that
	allows the user to select which field to sort on.

	@h3{Hungarian: smn)
----------------------------------------------------------------------------------------------*/
class SortMenuNode : public GenRefObj
{
public:

	StrUni m_stuText;				// Text for this menu node.
	int m_wsMagic;					// WritingSystem/WsSelector from UserViewField.
	SortMenuNodeType m_smnt;		// Type of this menu node.
	union
	{
		int m_clid;					// ksmntClass: class id
		int m_flid;					// ksmntField or ksmntLeaf: field id
		int m_ws;					// ksmntWs: writing system code
		int m_coll;					// ksmntColl: collating sequence code
	};
	int m_proptype;					// type of leaf node: either kfpt__ or kcpt__ value.
	union
	{
		HVO m_hvo;					// m_proptype = kfptPossList: list's database id
		int m_stid;					// m_proptype = kfptEnumList: list's string resource id
	};
	SortMenuNodeVec m_vsmnSubItems;	// Vector of subitems in menu tree

	void AddSortedSubItem(SortMenuNode * psmn);
	static void AddSortedMenuNode(SortMenuNodeVec & vsmn, SortMenuNode * psmn);
};


/*----------------------------------------------------------------------------------------------
	This is the base class for FieldWorks applications.

	@h3{Hungarian: app)
----------------------------------------------------------------------------------------------*/
class AfApp : public CmdHandler
{
public:
	typedef CmdHandler SuperClass;

	//:>****************************************************************************************
	//:>	Static methods.
	//:>****************************************************************************************
	//:> Get the current state of the modifiers.
	static uint GrfmstCur(bool fAsync = false);
	static bool ConfirmUndoableAction();

	// Return a pointer to the application object.
	static AfApp * Papp()
	{
		return s_papp;
	}

	// Return a pointer to the application's command dispatcher.
	static CmdExec * GetCmdExec()
	{
		return &s_papp->m_cex;
	}

	//:> This gets the menu manager of the current frame window.
	static AfMenuMgr * GetMenuMgr(AfMenuMgr ** ppmum = NULL);


	// Return a pointer to the application's settings object.
	static FwSettings * GetSettings()
	{
		// When unregistering a DLL, for example when uninstalling FW, the s_papp member doesn't
		// get assigned a real value, so we have to test it first:
		if (!s_papp)
			return NULL;
		return &s_papp->s_fws;
	}

	static bool LaunchHL(HWND hwnd, LPCTSTR pszOperation, LPCTSTR pszFile,
		LPCTSTR pszParameters, LPCTSTR pszDirectory, int nShowCmd);

	//:> Constructor and destructor.
	AfApp();
	~AfApp();

	//:>****************************************************************************************
	//:>	Virtual methods.
	//:>****************************************************************************************

	//:>****************************************************************************************
	//:> App management.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Close any windows associated with a database, save the database, clear all caches, and
		shutdown the connection to the database.
		@param pszDbName Name of the database to close.
		@param pszSvrName Name of the server hosting the database.
		@param fOkToClose True to close the application if there are no further connections
			after the requested connection is closed. False leaves the application open.
		@return True if any windows were closed.
	------------------------------------------------------------------------------------------*/
	virtual bool CloseDbAndWindows(const OLECHAR * pszDbName, const OLECHAR * pszSvrName,
		bool fOkToClose)
	{ return false; } // Ignore this for apps without database connections
	// Method to reopen after a Database Restore or other major database update.
	// @param pszDbName Name of the database to open.
	// @param pszSvrNam Name of the database server.
	// @param hvo Optional HVO of object to open (needed for list editor).
	virtual void ReopenDbAndOneWindow(const OLECHAR * pszDbName, const OLECHAR * pszSvrName,
		HVO hvo = 0) { }

	virtual void KillApp();
	virtual void Quit(bool fForce = false);
	virtual int Run(HINSTANCE hinst, Pcsz pszCmdLine, int nShowCmd);
	void ShowSplashScreen();
	void ShowHelpAbout();

	//:>****************************************************************************************
	//:> Accelerator table management.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Add an accelerator table to the application's menu manager.

		@param hact Handle to the accelerator table.
		@param apl Accelerator priority level used to sort accelerator tables.
		@param hwnd Handle to window which receives commands from this table.

		@return The assigned accelerator table ID used in future calls to RemoveAccelTable or
						SetAccelHandle.
	------------------------------------------------------------------------------------------*/
	virtual int AddAccelTable(HACCEL hact, int apl, HWND hwnd)
	{
		return GetMenuMgr()->AddAccelTable(hact, apl, hwnd);
	}
	/*------------------------------------------------------------------------------------------
		Load an accelerator table for the application's menu manager.

		@param rid Resource id for the accelerator table.
		@param apl Accelerator priority level used to sort accelerator tables.
		@param hwnd Handle to window which receives commands from this table.

		@return The assigned accelerator table ID used in future calls to RemoveAccelTable or
						SetAccelHandle.
	------------------------------------------------------------------------------------------*/
	virtual int LoadAccelTable(int rid, int apl, HWND hwnd)
	{
		return GetMenuMgr()->LoadAccelTable(rid, apl, hwnd);
	}
	/*------------------------------------------------------------------------------------------
		Remove an accelerator from the application's menu manager.

		@param atid Accelerator table ID previously returned by AddAccelTable or LoadAccelTable.
	------------------------------------------------------------------------------------------*/
	virtual void RemoveAccelTable(int atid)
	{
		GetMenuMgr()->RemoveAccelTable(atid);
	}
	/*------------------------------------------------------------------------------------------
		Set the window which receives commands through the given accelerator table.  If hwnd is
		NULL, then the accelerator table is disabled.

		@param atid Accelerator table ID previously returned by AddAccelTable or LoadAccelTable.
		@param hwnd Handle to window which receives commands from this table, or NULL.
	------------------------------------------------------------------------------------------*/
	virtual void SetAccelHwnd(int atid, HWND hwnd)
	{
		GetMenuMgr()->SetAccelHwnd(atid, hwnd);
	}

	//:>****************************************************************************************
	//:> Command handler management.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Add a command handler to the application's command dispatcher.

		@param pcmh Pointer to a command handler object.
		@param cmhl Command handler level (used for sorting command handlers).
		@param grfcmm Flag bits indicating the target of the command handler.
	------------------------------------------------------------------------------------------*/
	virtual void AddCmdHandler(CmdHandler * pcmh, int cmhl, uint grfcmm = kfcmmNobody)
	{
		GetCmdExec()->AddCmdHandler(pcmh, cmhl, grfcmm);
	}
	/*------------------------------------------------------------------------------------------
		Remove a command handler to the command handler list.

		@param pcmh Pointer to a command handler object.
		@param cmhl Command handler level (used for sorting command handlers).
	------------------------------------------------------------------------------------------*/
	virtual void RemoveCmdHandler(CmdHandler * pcmh, int cmhl)
	{
		GetCmdExec()->RemoveCmdHandler(pcmh, cmhl);
	}
	/*------------------------------------------------------------------------------------------
		Remove a command handler from all internal structures (so it can die in peace).

		@param pcmh Pointer to a command handler object.
	------------------------------------------------------------------------------------------*/
	virtual void BuryCmh(CmdHandler * pcmh)
	{
		GetCmdExec()->BuryCmdHandler(pcmh);
	}

	//:>****************************************************************************************
	//:> Command management.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Dispatch the given command immediately via the application's command dispatcher.

		@param pcmd Pointer to a command object.

		@return True if the command was handled, otherwise false.
	------------------------------------------------------------------------------------------*/
	virtual bool FDispatchCmd(Cmd * pcmd)
	{
		return GetCmdExec()->FDispatchCmd(pcmd);
	}
	/*------------------------------------------------------------------------------------------
		Post the command to the back of the application's command dispatcher queue.

		@param pcmd Pointer to a command object.
	------------------------------------------------------------------------------------------*/
	virtual void EnqueueCmd(Cmd * pcmd)
	{
		GetCmdExec()->EnqueueCmd(pcmd);
	}
	/*------------------------------------------------------------------------------------------
		Post the command to the front of the application's command dispatcher queue.

		@param pcmd Pointer to a command object.
	------------------------------------------------------------------------------------------*/
	virtual void PushCmd(Cmd * pcmd)
	{
		GetCmdExec()->PushCmd(pcmd);
	}
	/*------------------------------------------------------------------------------------------
		Post the command to the back of the application's command dispatcher queue.

		@param cid Command id.
		@param pcmh Pointer to the command handler for the command.
		@param n0 First argument for the command.
		@param n1 Second argument for the command.
		@param n2 Third argument for the command.
		@param n3 Fourth argument for the command.
	------------------------------------------------------------------------------------------*/
	virtual void EnqueueCid(int cid, CmdHandler * pcmh = NULL, int n0 = 0, int n1 = 0,
		int n2 = 0, int n3 = 0)
	{
		CmdPtr qcmd;
		qcmd.Attach(NewObj Cmd(cid, pcmh, n0, n1, n2, n3));
		EnqueueCmd(qcmd);
	}
	/*------------------------------------------------------------------------------------------
		Post the command to the front of the application's command dispatcher queue.

		@param cid Command id.
		@param pcmh Pointer to the command handler for the command.
		@param n0 First argument for the command.
		@param n1 Second argument for the command.
		@param n2 Third argument for the command.
		@param n3 Fourth argument for the command.
	------------------------------------------------------------------------------------------*/
	virtual void PushCid(int cid, CmdHandler * pcmh = NULL, int n0 = 0, int n1 = 0,
		int n2 = 0, int n3 = 0)
	{
		CmdPtr qcmd;
		qcmd.Attach(NewObj Cmd(cid, pcmh, n0, n1, n2, n3));
		PushCmd(qcmd);
	}
	/*------------------------------------------------------------------------------------------
		Remove any commands from the application's command dispatcher queue that match these
		values.

		@param cid Command id.
		@param pcmh Pointer to the command handler for the command.
		@param n0 First argument for the command.
		@param n1 Second argument for the command.
		@param n2 Third argument for the command.
		@param n3 Fourth argument for the command.
	------------------------------------------------------------------------------------------*/
	virtual void FlushCid(int cid, CmdHandler * pcmh = NULL, int n0 = 0, int n1 = 0,
		int n2 = 0, int n3 = 0)
	{
		GetCmdExec()->FlushCid(cid, pcmh, n0, n1, n2, n3);
	}


	//:>****************************************************************************************
	//:> Methods for handling multiple top-level windows.
	//:>****************************************************************************************

	void SetCurrentWindow(AfMainWnd * pafw);
	void AddWindow(AfMainWnd * pafw);
	void RemoveWindow(AfMainWnd * pafw);

	/*------------------------------------------------------------------------------------------
		Return a pointer to the main window.

		WARNING: Be careful where you use this method. It is much safer to use
		AfWnd::MainWindow(), and that method should be used for any case where you have a
		pointer to an AfWnd. This method should only be used for when you want to see if a
		pointer is the current main window, not to actually get a pointer to the main window
		to call a method on it.
	------------------------------------------------------------------------------------------*/
	AfMainWnd * GetCurMainWnd()
	{
		return m_qafwCur;
	}
	/*------------------------------------------------------------------------------------------
		Return the number of main windows that are currently open for this application.
	------------------------------------------------------------------------------------------*/
	int GetMainWndCount()
	{
		return m_vqafw.Size();
	}

	/*------------------------------------------------------------------------------------------
		Return the vector of main windows that are currently open for this application.
	------------------------------------------------------------------------------------------*/
	Vector<AfMainWndPtr> & GetMainWindows()
	{
		return m_vqafw;
	}
	void EnableMainWindows(bool fEnable);

	/*------------------------------------------------------------------------------------------
		Override this method and include AfFwTool in your make to support launching from the
		Explorer. Caller assumes that a new window gets created and becomes the current one.
		If this fails, implementation should throw an exception.
		The argments are the same as IFwTool::NewMainWnd

		@param bstrServerName Name of the MSDE/SQLServer computer.
		@param bstrDbName Name of the database.
		@param hvoLangProj Which languate project within the database.
		@param hvoMainObj The top-level object on which to open the window.
		@param encUi The user-interface writing system.
		@param nTool A tool-dependent identifier of which tool to use.
		@param nParam Another tool-dependent parameter.
		@param dwRegister value of the registration in the Running Object Table
	------------------------------------------------------------------------------------------*/
	virtual void NewMainWnd(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
		int hvoMainObj, int encUi, int nTool, int nParam, DWORD dwRegister)
	{
		ThrowHr(WarnHr(E_FAIL));
	}
	virtual const CLSID * GetAppClsid()
	{
		return NULL;
	}

	/*------------------------------------------------------------------------------------------
		Override this method and include AfFwTool in your make to support launching from the
		Explorer. Caller assumes that a new window gets created and becomes the current one.
		If this fails, implementation should throw an exception.
		The argments are the same as IFwTool::NewMainWnd

		@param bstrServerName Name of the MSDE/SQLServer computer.
		@param bstrDbName Name of the database.
		@param hvoLangProj Which languate project within the database.
		@param hvoMainObj The top-level object on which to open the window.
		@param encUi The user-interface writing system.
		@param nTool A tool-dependent identifier of which tool to use.
		@param nParam Another tool-dependent parameter.
		@param prghvo Pointer to an array of object ids.
		@param chvo Number of object ids in prghvo.
		@param prgflid Pointer to an array of flids.
		@param cflid Number of flids in prgflid.
		@param ichCur Cursor offset from beginning of field.
		@param nView The view to display when showing the first object. Use -1 to use the first
			data entry view.
		@param dwRegister value of the registration in the Running Object Table
	------------------------------------------------------------------------------------------*/
	virtual void NewMainWndWithSel(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
		int hvoMainObj, int encUi, int nTool, int nParam, const HVO * prghvo, int chvo,
		const int * prgflid, int cflid, int ichCur, int nView, DWORD dwRegister)
	{
		ThrowHr(WarnHr(E_FAIL));
	}
#ifdef DEBUG
	/*------------------------------------------------------------------------------------------
		Perform some basic sanity checks.  This is used in Assert, and typically consists of a
		a series of Asserts.

		@return True if everything seems okay, otherwise false.
	------------------------------------------------------------------------------------------*/
	bool AssertValid()
	{
		AssertPtrN(m_qSplashScreenWnd.Ptr());
		AssertPszN(m_pszCmdLine);
		Assert(m_nMsrSys==kninches || m_nMsrSys==knmm || m_nMsrSys==kncm || m_nMsrSys==knpt);
		AssertPtrN(m_qfistLog);
		for (int iwnd = 0; iwnd < m_vqafw.Size(); ++iwnd)
			AssertPtr(m_vqafw[iwnd].Ptr());
		AssertPtrN(m_qafwCur.Ptr());
		CmdHandler::AssertValid();
		AssertPtr(this);
		if (!m_strFwCodePath.AssertValid())
			return false;
		if (!m_strFwDataPath.AssertValid())
			return false;
		if (!m_strHelpFilename.AssertValid())
			return false;
		if (!m_hmsuvstuCmdLine.AssertValid())
			return false;
		if (!m_vqafw.AssertValid())
			return false;

		return true;
	}
#endif // DEBUG

	/*------------------------------------------------------------------------------------------
		Increment the count of objects (currently, typically FwTool objects) that this
		application has made available to other processes.
	------------------------------------------------------------------------------------------*/
	void IncExportedObjects()
	{
		::InterlockedIncrement(&m_cunkExport);
	}

	void DecExportedObjects();

	StrApp GetFwDataPath()
	{
		return m_strFwDataPath;
	}

	StrApp GetFwCodePath()
	{
		return m_strFwCodePath;
	}

	/*------------------------------------------------------------------------------------------
		Override this method to specify where the help file for the application is.

		@return The pathname of the help file relative to the application.  DON'T EVEN THINK OF
						MAKING THIS AN ABSOLUTE PATH.
	------------------------------------------------------------------------------------------*/
	virtual const achar * GetHelpFile()
	{
		return NULL;
	}

	/*------------------------------------------------------------------------------------------
		Override this method to specify where the help file for the application is.

	------------------------------------------------------------------------------------------*/
	virtual void SetHelpFilename(StrApp strHelpFilename)
	{
		m_strHelpFilename = strHelpFilename;
	}

	StrApp GetHelpFilename()
	{
		return m_strHelpFilename;
	}

	virtual void SetHelpBaseName(const achar * pszHelpBaseName)
	{
		m_strHelpBaseName = pszHelpBaseName;
		m_strHelpFilename = GetFwCodePath().Chars();
		m_strHelpFilename.Append(pszHelpBaseName);
	}

	const achar * GetHelpBaseName()
	{
		return m_strHelpBaseName.Chars();
	}


	bool ShowHelpFile(const achar * pszPage = NULL);
	bool ShowTrainingFile(const achar * pszFilespec = NULL);

	static void DefaultFontsForWs(IWritingSystem * pws, StrUni & stuDefSerif,
		StrUni & stuDefSans, StrUni & stuDefMono, StrUni & stuDefBodyFont);

	virtual SilTime DropDeadDate();

	/*------------------------------------------------------------------------------------------
		Override this method to specify the application's resource id for its name.

		@return The resource id for the application's name string (e.g. "Data Notebook").
	------------------------------------------------------------------------------------------*/
	virtual int GetAppNameId()
	{
		return 0;
	}

	/*------------------------------------------------------------------------------------------
		Override this method to specify the application's resource id for its Prop Dialog name.

		@return The resource id for the Fieldworks object name string (e.g. "Notebook").
	------------------------------------------------------------------------------------------*/
	virtual int GetAppPropNameId()
	{
		return 0;
	}

	/*------------------------------------------------------------------------------------------
		Override this method to specify the application's resource id for its main icon.

		@return The resource id for the application's icon.
	------------------------------------------------------------------------------------------*/
	virtual int GetAppIconId()
	{
		return 0;
	}

	/*------------------------------------------------------------------------------------------
		Return the instance handle for this application.
	------------------------------------------------------------------------------------------*/
	HINSTANCE GetInstance()
	{
		return m_hinst;
	}

	/*------------------------------------------------------------------------------------------
		Return the Measurement system used for this application.
	------------------------------------------------------------------------------------------*/
	MsrSysType GetMsrSys()
	{
		return m_nMsrSys;
	}

	/*------------------------------------------------------------------------------------------
		Set the Measurement system used for this application.
	------------------------------------------------------------------------------------------*/
	void SetMsrSys(MsrSysType nMsrSys)
	{
		m_nMsrSys = nMsrSys;
	}

	HRESULT GetLogPointer(IStream ** ppfist);	// Returns m_qfistLog.

	void SuppressIdle();

	bool DeleteObject(HVO hvo);

	static void HandleTopLevelError(Throwable * pthr);
	static void CheckErrorHr(HRESULT hr);

	static LCID GetDefaultKeyboard();

	virtual bool OnStyleNameChange(IVwStylesheet * psts, ISilDataAccess * psda)
	{
		return false;
	}
	bool PreSynchronize(SyncInfo & sync, AfLpInfo * plpi);

private:
	void SetFwPaths();

protected:
	StrApp m_strFwDataPath;
	StrApp m_strFwCodePath;
	StrApp m_strHelpFilename; // Full path to help file.
	StrApp m_strHelpBaseName; // Relative path to help file, relative to FW Root directory.
	FwCoreDlgs::IFwSplashScreenPtr m_qSplashScreenWnd; // The splash window used during startup.

	// Pointer to the single instance of an AfApp object.
	static AfApp * s_papp;
	// This can be used to suppress an idle message if only one thing has happened since the
	// last OnIdle.  The idea is that this variable counts the number of messages processed
	// since the system was last idle. The SuppressIdle method decrements the count, indicating
	// that the message handler calling SuppressIdle knows it does not need to be followed by
	// Idle processing.
	static int s_cmsgSinceIdle;
	// Object for handling persistent settings for this application.
	FwSettings s_fws;
	// Handle to the instance of this application.
	HINSTANCE m_hinst;
	// Pointer to the command line string used to launch this application.
	Pcsz m_pszCmdLine;
	// Map of the command line string used to launch this application.
	HashMapStrUni<Vector<StrUni> > m_hmsuvstuCmdLine;
	// Specify how the main window is to be shown.  See MSDN documentation for ShowWindow for
	// details.
	int m_nShow;
	//:> ENHANCE SteveMc(ShonK): If we go to multi-threaded, make m_cex a TLS thing so each
	//:> thread has its own.
	// Command dispatcher for this application.
	CmdExec m_cex;
	// Flag that the application is in the process of shutting down.
	bool m_fQuit;
	// Contains a vector of top-level windows.
	Vector<AfMainWndPtr> m_vqafw;
	// The top-level window that currently has the focus.
	AfMainWndPtr m_qafwCur;
	// Measurement system that is used in this App.
	MsrSysType m_nMsrSys;
	// Return value from registering the application in the Running Object Table.
	DWORD m_dwRegister;
	IStreamPtr m_qfistLog;				// Log file for errors.
	bool m_fSplashNeeded; // Flag indicating splash screen is needed.



	//:> NOTE JohnT: if we have object types that are used both in-process and cross-process,
	//:> it will be hard if not impossible to make sure this count stays right. Should an object
	//:> of such a class increment it or not? It can't tell whether a particular AddRef is
	//:> cross-platform or not.
	//:> On the other hand, if we just depend on the count maintained by ModuleAddRef and
	//:> ModuleRelease, then any memory leak of a COM object will prevent the application from
	//:> shutting down at all, making the memory leak all but impossible to detect.
	//:> Hence, at least for now, I favor using this count. The effect is that any
	//:> memory leak involving an exported object will prevent shut-down; we just have to
	//:> be extra careful about their ref counts.
	// Count objects (currently, typically FwTool objects) that this application has made
	// available to other processes.  The application cannot shut down, even if all windows
	// close, while this count is non-zero.
	long m_cunkExport;

	virtual void OnIdle();

	virtual void ParseCommandLine(Pcsz pszCmdLine);

	void GetVersionInfo(StrUni & stuProdVersion, DWORD & cDaysSince1900, StrUni & stuFwVersion);

	/*------------------------------------------------------------------------------------------
		Check to see if the -Embedding command line option exists.

		@return true if it does, false otherwise.
	------------------------------------------------------------------------------------------*/
	bool HasEmbedding()
	{
		StrUni stuKey(L"embedding");
		Vector<StrUni> vstu;
		return m_hmsuvstuCmdLine.Retrieve(stuKey, &vstu);
	}

	//:>****************************************************************************************
	//:>	Command handlers.
	//:>****************************************************************************************
	void _GetZOrders(Vector<HWND> & vhwnd, bool fAllWindows = false);
	virtual bool CmdWndCascade(Cmd * pcmd);
	virtual bool CmdWndTileHoriz(Cmd * pcmd);
	virtual bool CmdWndTileVert(Cmd * pcmd);
	virtual bool CmdFileExit(Cmd * pcmd);
	virtual bool CmdHelpAbout(Cmd * pcmd);

	//:>****************************************************************************************
	//:>	Main loop management.
	//:>****************************************************************************************
	virtual void TopOfLoop();
	virtual void Init();
	virtual int Loop();
	virtual void CleanUp();
	virtual bool FQueryQuit(bool fForce = false);

	//:>****************************************************************************************
	//:>	Message dispatching.
	//:>****************************************************************************************
	virtual bool FGetNextMessage(MSG * pmsg);

	/*------------------------------------------------------------------------------------------
		Translate accelerators, ie, process an accelerator key message for menu commands.

		@return True if the message should be dispatched, otherwise false since it has already
						been handled.
	------------------------------------------------------------------------------------------*/
	virtual bool FTransAccel(MSG * pmsg)
	{
		return GetMenuMgr()->FTransAccel(pmsg);
	}

	virtual bool FTransMsg(MSG * pmsg);
	virtual bool FDispatchMsg(MSG * pmsg);

	/*------------------------------------------------------------------------------------------
		Dispatch the next command from the application command dispatcher queue.

		@return True if a command was dispatched, false if the queue was empty.
	------------------------------------------------------------------------------------------*/
	virtual bool FDispatchNextCmd()
	{
		return GetCmdExec()->FDispatchNextCmd();
	}

	friend class PossChsrDlg;
};


/*----------------------------------------------------------------------------------------------
	This application base class supports database connections.

	@h3{Hungarian: dapp)
----------------------------------------------------------------------------------------------*/
class AfDbApp : public AfApp, public IBackupDelegates
{
public:
	typedef AfApp SuperClass;

	//:> IBackupDelegates methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);
	STDMETHOD(GetLocalServer_Bkupd)(BSTR * pbstrSvrName);
	STDMETHOD(GetLogPointer_Bkupd)(IStream** ppfist);
	STDMETHOD(SaveAllData_Bkupd)(const OLECHAR * pszServer, const OLECHAR * pszDbName);
	STDMETHOD(CloseDbAndWindows_Bkupd)(const OLECHAR * pszSvrName, const OLECHAR * pszDbName,
		ComBool fOkToClose, ComBool * pfWindowsClosed);
	STDMETHOD(IncExportedObjects_Bkupd)(void);
	STDMETHOD(DecExportedObjects_Bkupd)(void);
	STDMETHOD(CheckDbVerCompatibility_Bkupd)(const OLECHAR * pszSvrName,
		const OLECHAR * pszDbName, ComBool * pfCompatible);
	STDMETHOD(ReopenDbAndOneWindow_Bkupd)(const OLECHAR * pszSvrName,
		const OLECHAR * pszDbName);
	STDMETHOD(IsDbOpen_Bkupd)(const OLECHAR * pszServer, const OLECHAR * pszDbName,
		ComBool * pfIsOpen);

	//:>****************************************************************************************
	//:>	Static methods.
	//:>****************************************************************************************

	//:> These static methods to update old versions of the database have to go somewhere.
	//:> Why not here?
	static StrApp FilterForFileName(StrApp strName);

	//:>****************************************************************************************
	//:>	Virtual methods.
	//:>****************************************************************************************

	virtual AfDbInfo * GetDbInfo(const OLECHAR * pszDbName, const OLECHAR * pszSvrName) = 0;
	int GetcDbi()
	{
		return m_vdbi.Size();
	}
	void DelDbInfo(AfDbInfo * pdbi);

	/*------------------------------------------------------------------------------------------
		Close any windows associated with a database, save the database, clear all caches, and
		shutdown the connection to the database.
		@param pszDbName Name of the database to close.
		@param pszSvrName Name of the server hosting the database.
		@param fOkToClose True to close the application if there are no further connections
			after the requested connection is closed. False leaves the application open.
		@return True if any windows were closed.
	------------------------------------------------------------------------------------------*/
	virtual bool CloseDbAndWindows(const OLECHAR * pszDbName, const OLECHAR * pszSvrName,
		bool fOkToClose);
	virtual bool GetAppVer(int & nAppVer, int & nErlyVer, int & nLastVer);
	virtual void CleanUp();
	virtual void NewMainWnd(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
		int hvoMainObj, int encUi, int nTool, int nParam, RecMainWnd * prmw,
		achar * pszClassT, int nridInitialMessage, DWORD dwRegister);
	virtual bool MakeUniqueTopicsListName(IOleDbEncap * pode, int ws, StrUni &stuName);
	virtual int NewTopicsList(IOleDbEncap * pode);
	virtual int NewTopicsList(IOleDbEncap * pode, StrUni stuName, int ws, int wsMagic,
		HVO hvoCopy = -1);
	virtual int TopicsListProperties(AfLpInfo * plpi, HVO hvoPssl, int ws, HWND hwndOwner);
	virtual void Quit(bool fForce);

	//:> Send messages to the splash window, if it is up.
	void SetSplashMessage(const wchar * pwszMessage);
	void SetSplashMessage(uint nMessageId);
	void SetSplashLoadingMessage(const wchar * pwszItemBeingLoaded);
	StrUni GetLocalServer()
	{
		return m_stuLocalServer;
	}

	int GetDbVersion(const OLECHAR * pszSvrName, const OLECHAR * pszDbName);
	void SetDbVersion(const OLECHAR * pszSvrName, const OLECHAR * pszDbName, int nVersion);
	bool CheckDbVerCompatibility(const OLECHAR * pszSvrName, const OLECHAR * pszDbName);
	void DisplayErrorInfo(IUnknown * punk = NULL);

	virtual bool OnStyleNameChange(IVwStylesheet * psts, ISilDataAccess * psda);
	virtual bool DbSynchronize(AfLpInfo * plpi);
	virtual bool Synchronize(SyncInfo & sync, AfLpInfo * plpi);
	virtual bool FullRefresh(AfLpInfo * plpi);
	bool AreAllWndsOkToChange(AfDbInfo * pdbi, bool fChkReq = true);
	bool CloseAllWndsEdits();
	bool SaveAllWndsEdits(AfDbInfo * pdbi);

	static int UserWs(IOleDbEncap * pode);

protected:

	virtual void Init();

	virtual OptionsReturnValue ProcessDBOptions(int clidRootObj, StrUni * pstuDefRootObj,
					int clidProject, StrUni * pstuProjTableName,
					HashMapStrUni<StrUni> * phmsustuOptions, HVO & hvoPId, HVO & hvoRootObjId,
					bool fUseOptions = true, bool fAllowNoOwner = false);
	virtual int ProcessOptRetVal(OptionsReturnValue orv,
					HashMapStrUni<StrUni> * phmsustuOptions, bool bUseDialog = false);
	FileOpenProjectInfo * DoFileOpenProject();
	virtual void GetOpenProjHelpUrl(BSTR * pbstr)
	{
		StrApp str(m_strHelpFilename);
		str.Append("::/User_Interface/Menus/File/Open_a_FieldWorks_project.htm");
		str.GetBstr(pbstr);
	}
	virtual ComBool GetAllowOPPopupMenu()
	{
		ComBool fAllowMenu(true);
		return fAllowMenu;
	}
	virtual int GetOPSubitemClid()
	{ return 0; }
	virtual RecMainWnd * CreateMainWnd(WndCreateStruct & wcs, FileOpenProjectInfo * pfopi)
	{ return NULL; }

	//:>****************************************************************************************
	//:>	Command handlers.
	//:>****************************************************************************************
	virtual bool CmdFileOpenProj(Cmd * pcmd);
	virtual bool CmdFileBackup(Cmd * pcmd);

	//:>****************************************************************************************
	//:>	Member variables.
	//:>****************************************************************************************

	Vector<AfDbInfoPtr> m_vdbi;			// Vector of database connections.
	StrUni m_stuLocalServer;			// The name of the local server (e.g., ls-zook\\SILFW).
};

#endif // !AfApp_H
