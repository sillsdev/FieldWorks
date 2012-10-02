/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Backup.h
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	Header file for the Backup & Restore dialog classes.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef BACKUP_H_INCLUDED
#define BACKUP_H_INCLUDED
/*:End Ignore*/


//:> Define smart pointers to the interfaces we'll be using:
DEFINE_COM_PTR(IPersistFile);
DEFINE_COM_PTR(DIFwBackupDb);

//:> Forward declarations:
class XceedZipBackup;	// defined fully in ZipInvoke.h
class ZipSystemData;	// defined fully in ZipInvoke.h
class BackupInfo;		// defined later in this file.
class BackupMutex;						// defined in Backup.cpp

namespace TestDbServices
{
	class TestFwBackupDb;
	class TestBackupHandler;
	class TestAvailableProjects;
}


/*----------------------------------------------------------------------------------------------
	This class keeps track of information related to backup reminders.

	@h3{Hungarian: bkprmi}
----------------------------------------------------------------------------------------------*/
class BackupReminderInfo
{
public:
	BackupReminderInfo();
	void ReadFromRegistry(FwSettings * pfws);
	void WriteToRegistry(FwSettings * pfws);

	int m_nDays; // Number of days that can elapse after a backup before a reminder is given
	bool m_fTurnOff; // True if reminders are disabled

protected:
	enum { kDefaultDays = 7, kDefaultTurnOff = 0 };
};


/*----------------------------------------------------------------------------------------------
	This class keeps track of information related to backup passwords.

	@h3{Hungarian: bkppwi}
----------------------------------------------------------------------------------------------*/
class BackupPasswordInfo
{
public:
	BackupPasswordInfo();
	void ReadFromRegistry(FwSettings * pfws);
	void WriteToRegistry(FwSettings * pfws);

	bool m_fLock; // True if a password is to be used.
	StrAppBuf m_strbPassword; // User's password
	StrAppBuf m_strbMemoryJog; // User's memory jog to help remember password

protected:
	enum { kVersion = 1 };
};


/*----------------------------------------------------------------------------------------------
	This class keeps track of all information related to a backup. Its data members are
	configured by the backup/restore dialog and its tabs. The data is used in various aspects
	of the backup system. Hence the decision to make data members public.

	@h3{Hungarian: bkpi}
----------------------------------------------------------------------------------------------*/
class BackupInfo
{
public:
	BackupInfo();
	bool Init(FwSettings * pfws);

	void ReadFromRegistry(FwSettings * pfws, BSTR defaultBackupDir);
	void WriteSettingsToRegistry(FwSettings * pfws);
	bool ReadDbProjectsFromRegistry(FwSettings * pfws);
	void WriteDbProjectsToRegistry(FwSettings * pfws);
	bool AutoSelectBackupProjects(int & nTotalAltered);

	StrAppBuf m_strbDeviceName; // Device letter representing where to store backup
	StrAppBuf m_strbDirectoryPath; // Last used backup directory (includes the device name)
	StrAppBuf m_strbDefaultDirectoryPath; // Default backup directory
	StrAppBuf m_strbSelectedDirectory; // Backup directory currently selected in the combobox
	int m_iCbSelection; // Index of selected comboxbox selection
	StrApp m_strZipFileName; // The zip file on the backup device
	StrApp m_strProjectFullName; // The project + database name used in the restore
	bool m_fXml; // True if XML backup to be included
	BackupReminderInfo m_bkprmi;
	BackupPasswordInfo m_bkppwi;
	// Flag whether the initially checked databases should be limited to those currently opened
	// by the calling application.
	bool m_fLimitBackupToActive;
	IBackupDelegates * m_pbkupd;
	FwSettings m_fws;

	struct ProjectData
	{
		StrUni m_stuDatabase;
		StrUni m_stuProject;
		bool m_fBackup;
		FILETIME m_filtLastBackup;
		ProjectData() : m_fBackup(false)
			{ m_filtLastBackup.dwHighDateTime = m_filtLastBackup.dwLowDateTime = 0; }
	};
	Vector<ProjectData> m_vprojd;

protected:
	enum { kDefaultXml = 0 };
	void CollectAllFwDbs(Vector<StrUni> & vstu, IStream * pfist, BSTR bstrServer);
	void CleanupRegistry(Vector<StrApp> & vstrRemainingKeys);
	void CheckProjectForBackup(ProjectData & projd, IOleDbEncap * pode, IOleDbCommand * podc,
		int & nTotalAltered, bool & fResult);
};


/*----------------------------------------------------------------------------------------------
	Generic base class for all dialogs in this module

	@h3{Hungarian: badib}
----------------------------------------------------------------------------------------------*/
class BackupDialogBase : public AfDialog
{
	typedef AfDialog SuperClass;
public:
	virtual bool OnHelp();
	virtual SmartBstr GetHelpTopic();

protected:
	static IHelpTopicProviderPtr s_qHelpTopicProvider;
};


/*----------------------------------------------------------------------------------------------
	The methods of this namespace generate a suitable filename for a backup file, and can also
	interpret such names to determine if they are likely to be valid backup files. There are
	also methods to recreate the original project name, and a string representing the project
	backup version.

	@h3{Hungarian: bkpfnp}
----------------------------------------------------------------------------------------------*/
namespace BackupFileNameProcessor
{
	void GenerateFileName(StrAppBuf strbPath, StrUni stuProjectName,
		StrUni stuDatabaseName, StrUni & stuFileName);
	bool IsValidFileName(StrAppBufPath strbpName);
	bool GetProjectName(StrAppBufPath strbpName, StrApp & strProjectName);
	bool GetProjectFullName(StrAppBufPath strbpName, StrApp & strProjectFullName);
	bool GetDatabaseName(StrAppBufPath strbpName, StrApp & strDatabaseName);
	bool GetVersionName(StrAppBufPath strbpName, StrApp & strVersionName);
	bool GetVersion(StrAppBufPath strbpName, StrApp & strVersion);
};

/*----------------------------------------------------------------------------------------------
	The elements of this namespace handle all backup and restore errors.
	REVIEW: may not conform to FW standard for error handling.

	@h3{Hungarian: bkperr}
----------------------------------------------------------------------------------------------*/
namespace BackupErrorHandler
{
	// Error categories:
	enum
	{
		kBackupFailure,
		kBackupPossibleFailure,
		kBackupNonFatalFailure,
		kRestoreFailure,
		kRestorePossibleFailure,
		kRestoreNonFatalFailure,
		kWarning,
	};
	int MessageBox(HWND hwnd, int nCategory, int ridTitle, int ridMessage, UINT uType, ...);
	void ErrorBox(HWND hwnd, int nCategory, int ridMessage, int nLastError = 0,
		const achar * pszZipFile = NULL);
};


/*----------------------------------------------------------------------------------------------
	Class to handle password entry during restore operations.
	@h3{Hungarian: rstpwd}
----------------------------------------------------------------------------------------------*/
class RestorePasswordDlg : public BackupDialogBase
{
	typedef BackupDialogBase SuperClass;

public:
	RestorePasswordDlg();
	void Init(StrApp strMemoryJog, StrApp strDatabase, StrApp strProject, StrApp strVersion);

	// ExplorerApp::Restore() calls this method to activate the dialog:
	static bool GetPassword(HWND hwnd, StrApp strMemoryJog, StrApp strDatabase,
		StrApp strProject, StrApp strVersion, StrApp & strPassword);

	virtual SmartBstr GetHelpTopic()
	{
		return _T("khtpRestoreEnterPassword");
	}

protected:
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);

	bool m_fMemJogInUse; // True if there is a memory jog available
	StrApp m_strMemoryJog; // User's password memory jog
	StrApp m_strDatabase; // Name of database to be restored
	StrApp m_strProject; // Name of project to be restored
	StrApp m_strVersion; // Version details (date, time) in backup file
	StrApp m_strPassword; // User's password guess
};
typedef GenSmartPtr<RestorePasswordDlg> RestorePasswordDlgPtr;

/*----------------------------------------------------------------------------------------------
	Backup/Restore Progress dialog class. This will run in its own thread, to enable it to
	handle messages independently of the thread actually performing backup/restore. This means
	that responses to user actions such as pressing the Abort button should be immediate, and it
	also means we can set up a timer to control the progress indicator, when we know roughly how
	long a CPU-hogging activity will take. If a new thread cannot be created, the dialog will
	still run, but will not necessarily respond quickly to user input, or update the progress
	indicator for some activities.
	The way to use this class is as follows:
	1) Instantiate either locally on stack, or through dynamic memory allocation;
	2) Call ${#OmitActivity} for each activity to be excluded from Backup/Restore;
	3) Call ${#ShowDialog} to display the dialog, effectively as modeless.
	4) Call ${#SetActivityNumber} with zero before starting first activity.
	Note that some methods may only be called when the dialog is shown. See method comments for
	details.

	@h3{Hungarian: bkpprg}
----------------------------------------------------------------------------------------------*/
class BackupProgressDlg : public BackupDialogBase
{
	typedef BackupDialogBase SuperClass;

public:
	enum
	{
		BKP_PRG_PERCENT = WM_APP + 1, // Message indicating new update for progress bar.
		BKP_PRG_GET_EVENT = WM_APP + 2, // Message requesting handle to synchronization event
	};

	BackupProgressDlg(HANDLE hEvent, bool fRestore = false);
	~BackupProgressDlg();
	void ShowDialog();
	void OmitActivity(int iActivity);
	void SetXceedObject(XceedZipBackup * pxcz);

	void SetProjectName(StrUni stuName);
	void SetActivityNumber(int iActivity);
	void SetActivityEstimatedTime(int nSeconds);
	void SetPercentComplete(int nPercent);
	void EnableAbortButton(bool fEnable = true);
	bool GetAbortedFlag() { return m_fAborted; }
	void SetAbortedFlag() { m_fAborted = true; }

protected:
	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	void OnTimer(UINT nIDEvent);
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	// Process notifications from the user.
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	// Test if closure is allowed:
	virtual bool OnCancel();
	void UpdateWindowControls();

	bool m_fRestore; // True if we're restoring, false if we're backing up.
	StrApp m_strProject; // Name of project being backed up or restored
	// We need this so we can abort a zip, if user requests it:
	XceedZipBackup * m_pxczZipObject;
	// Pointer to array of resource IDs of activity text controls:
	const int * m_kpnActivityIds;
	// Pointer to array of resource IDs of activity icon controls:
	const int * m_kpnActivityIconIds;
	// Number of active listed activities:
	int m_nNumActivities;
	//  List of activity static control IDs in dialog resource:
	Vector<int> m_viOmittedActivities;
	int m_nCompletedActivities; // Number of activities completed.
	int m_nCurrentActivity; // Which of the listed activities is currently being monitored.
	bool m_fTimedActivity; // True if we can't monitor activity progress directly.
	int64 m_nStartTime; // Time at which current activity started.
	int64 m_nEstimatedEndTime; // Time at which current activity is estimated to end.
	bool m_fAborted; // True if user pressed Abort button
	bool m_fClosureAuthorized; // False until destructor called
	HICON m_hicon_check; // Icon of check mark.
	HICON m_hicon_arrow; // Icon of right arrow.
	HCURSOR m_hcurDefault; // Normal operating cursor

	HANDLE m_hThread; // Thread which runs independent message queue.
	HANDLE m_hEventAbort; // Event which suspends backup if abort button is pressed.
	HWND m_hwndParent; // parent window for the progress dialog

	static const int m_krgnBackupActivityIds[];
	static const int m_krgnRestoreActivityIds[];
	static const int m_krgnBackupActivityIconIds[];
	static const int m_krgnRestoreActivityIconIds[];
};


/*----------------------------------------------------------------------------------------------
	This class provides a facility to list all the available backup files in a given directory.
	It feeds details into a pair of specified dialog controls: a combobox to display the project
	name, and a list box to display different versions of that project.
	@h3{Hungarian: avprj}
----------------------------------------------------------------------------------------------*/
class AvailableProjects
{
	friend class TestDbServices::TestAvailableProjects;
public:
	AvailableProjects();
	void Init(HWND hwndDialog, int ctidProjects, int ctidVersions);

	bool DisplayAvailableProjects(StrAppBufPath strbpPath);
	bool DisplayAvailableProjects(StrAppBufPath strbpPath, StrAppBufPath strbpHighlight);
	void UpdateVersionsDisplay();
	void ProjectSelected(int nIndex);
	void VersionSelected(int nIndex);
	StrAppBufPath GetCurrentDirectoryPath();
	StrAppBuf GetCurrentFileName();
	StrAppBuf GetCurrentProjectName();
	bool ValidProjectSelected();

protected:
	void CollectProjectBackupFiles(StrAppBufPath strbpPath, StrAppBufPath strbpHighlight);

	HWND m_hwndDialog; // Handle of owning dialog
	int m_ctidProjects; // ID of ComboboxEx control for listing projects
	int m_ctidVersions; // ID of ListView control for listing project versions
	int m_nCurrentProject; // Index, into our internal list, of selected project
	int m_nCurrentVersion; // Index, into our internal list, of selected version
	bool m_fIgnoreRetriesToLoad; // Signal to ignore certain messages while altering controls

	struct Version
	{
		StrAppBufPath m_strbFileName;
		StrAppBuf m_strbDisplayName;
	};
	struct Project
	{
		StrAppBuf m_strbDatabaseName;
		StrAppBufPath m_strbpDirectoryPath;
		Vector<Version> m_vver;
	};
	Vector<Project> m_vprj; // List of projects
};


/*----------------------------------------------------------------------------------------------
	This class handles all backup and restore actions.

	@h3{Hungarian: bkph}
----------------------------------------------------------------------------------------------*/
class BackupHandler
{
	friend class BackupDlg;
	friend class BackupBkpDlg;
	friend class BackupRstDlg;
	friend class TestDbServices::TestBackupHandler;
	friend class TestDbServices::TestFwBackupDb;
public:
	enum VTrigger
	{
		kManual,
		kReminderAccepted,
		kExternal,
	};
	enum // Possible responses from ValidateBackupPath()
	{
		kDirectoryAlreadyExisted,
		kUserQuit,
		kCreationSucceeded,
		kCreationFailed
	};
	enum // Possible responses from UserConfigure()
	{
		kUnknownError,
		kUserClosed,
		kBackupOk,
		kBackupFail,
		kRestoreOk,
		kRestoreFail,
		kCannotRestore
	};

	BackupHandler();
	~BackupHandler();
	bool Init(IBackupDelegates * pbkupd);

	void CheckForMissedSchedules(HWND hwnd = NULL, IHelpTopicProvider * phtprovHelpUrls = NULL);

	bool Backup(VTrigger trigger, HWND hwnd = NULL, IHelpTopicProvider * phtprov = NULL);
	void Remind(int nElapsedDays = -1, HWND hwnd = NULL, IHelpTopicProvider * phtprov = NULL);
	int Restore();
	int UserConfigure(HWND hwndParent = NULL, bool fShowRestore = false,
		IHelpTopicProvider * phtprovHelpUrls = NULL);
	int ValidateBackupPath(bool fPromptUser, HWND hwndParent = NULL);

	// Let the caller limit the initial set of checked databases to those which are open.
	void LimitBackupToActive()
	{
		m_bkpi.m_fLimitBackupToActive = true;
	}

protected:
	class LogFile
	{
	public:
		LogFile();
		~LogFile();
		void Start(VTrigger trigger);
		void Write(const achar * szText);
		void Write(int stid);
		void TimeStamp();
		void Terminate();

	protected:
		FILE * m_fileLog;
		void Initiate();
	};

	int GetDaysSinceLastBackup();
	void GetTempWorkingDirectory(StrAppBufPath & strbpTempPath);
	void ClearTempWorkingDirectory();
	bool GetMsdeFileName(StrAppBufPath & strbpFile);
	bool CheckMutex(BackupMutex * pmut, int nErrCode);
	bool TryAutomaticBackup(VTrigger trigger, HWND hwnd, IHelpTopicProvider * phtprov,
		BackupMutex * pmut, LogFile & log, bool & fFinished, bool & fBackupOK);
	void CheckBackupPath(int nValidated, LogFile & log, bool & fFinished,
		bool & fBackupOK);
	bool BackupProject(BackupInfo::ProjectData & projd, StrUni & stuName, HANDLE hEvent,
		SYSTEMTIME systTime, LogFile & log, bool fTesting = false);
	bool ConnectToMasterDb();
	void CreateBackupFileNames(StrUni & stuZipFile, StrUni & stuIntermediateFile,
		StrUni & stuXmlFile, BackupInfo::ProjectData & projd);
	int GuessBackupTimeRequired();
	bool GenerateDbBackupFile(LogFile & log, const StrUni & stuIntermediateFile, HANDLE hEvent);
	bool GenerateXmlBackupFile(LogFile & log, const StrUni & stuXmlFile, HANDLE hEvent,
		BackupInfo::ProjectData & projd);
	bool ZipBackupFiles(LogFile & log, const StrUni & stuZipFile,
		const StrUni & stuIntermediateFile, const StrUni & stuXmlFile, HANDLE hEvent);
	void RecordBackupTime(BackupInfo::ProjectData & projd, SYSTEMTIME systTime);
	int GenerateRestoreNames(int & nRestoreOptions, StrUni & stuNewDatabaseName,
		StrUni & stuSource, StrUni & stuSourceXml);
	int SetupWithRestoreOptions(int nRestoreOptions, const StrUni & stuNewDatabaseName,
		bool & fDbAlreadyExists);
	int UnzipForRestore(HANDLE hEvent, const StrUni & stuSource, const StrUni & stuSourceXml,
		bool & fRestoreFromXml);
	int CheckForRestoreFromXml(const StrUni & stuSource, const StrUni & stuSourceXml,
		bool & fRestoreFromXml);
	int BackupForRestore(bool fDbAlreadyExists);
	int DisconnectDatabase(bool fDbAlreadyExists, bool & fDbDisconnectionFailed,
		ComBool & fClosedWindow, bool & fDbPreserved, bool & fExpObjInc);
	int DetachDatabase(bool & fDbPreserved, const StrAppBufPath & strbpDbFile);
	void GenerateUnusedFilenames(const achar * pszDbOriginalFileRoot);
	int RestoreFromXml(const StrUni & stuSourceXml);
	int RestoreFromBak(const StrUni & stuSource, int nRestoreOptions);
	int FinishRestoring(HANDLE hEvent, bool fDbPreserved);
	// The following methods are used by test code.
	static StrUni GetLocalServer();
	static void SetLocalServer(const wchar * pszServerName);
	static void SetInstanceHandle();

	bool m_fInitOk;
	BackupInfo m_bkpi;
	//RestoreInfo m_rsti;
	FwSettings m_fws;
	IBackupDelegates * m_pbkupd;
	BackupProgressDlg * m_pbkpprg;
	//Database names and pointers
	IOleDbEncapPtr m_qode; // Declare before m_qodc.
	IOleDbCommandPtr m_qodc;
	//Generated strings
	StrUni m_stuProjectName;
	StrUni m_stuTargetDatabase;
	StrUni m_stuTargetMdf;
	StrUni m_stuTargetLdf;
	StrUni m_stuCurrentZipFile;
	StrAppBufPath m_strbpTempPath;
	StrAppBufPath m_strbpTargetDir;
	StrAppBufPath m_strbpBackupDir;
	StrAppBufPath m_strbpDbMdfCache;
	StrAppBufPath m_strbpDbLdfCache;
	StrAppBufPath m_strbpDbMdfCurrent;
	StrAppBufPath m_strbpDbLdfCurrent;

	int64 m_nDbFileSize;
	int m_nBakFileWriteDuration;

private:
	XceedZipBackup * m_pFileZipper;
	ZipSystemData * m_pZipData;
	int Restore(HANDLE & hEvent, ComBool & fClosedWindow, bool & fRecovered,
		 bool & fDbDisconnectionFailed, bool & fDbPreserved, bool & fExpObjInc);
	void ClearFiles(StrAppBufPath & strbpPath, StrUni strFindSpec);
	int InitializeFileZipper();
	int GetOriginalDbName(StrAppBufPath & strbpSourceFileName, StrUni & stuOriginalDatabase);
	int GetSourceFileStrings(StrUni & stuOriginalDatabase, StrUni & stuSource,
		StrUni & stuSourceXml);
	int CheckTargetDirectory();
	int GetTargetFileStrings(StrUni & stuTargetMdf, StrUni & stuTargetLdf);
	int RestoreFromXml(const StrUni & stuSourceXml, StrUni & stuTargetMdf,
		StrUni & stuTargetLdf);
	int RestoreFromBak(const StrUni & stuSource, int nRestoreOptions,
		StrUni & stuTargetMdf, StrUni & stuTargetLdf);
	bool MakeDatabaseFile(StrUni & stuTargetDatabaseFile,
		StrAppBufPath & strbpBlankDatabaseFile);
	int BackupDatabaseBeforeOverwrite();
	bool RecoverFromFailedRestore();
	int RestoreFileListFromBak(const StrUni & stuSource, StrUni & stuDataFile,
		StrUni & stuLogFile);
	bool XpCmdShellCmd(const StrUni & cmd, const StrAppBufPath & strbpDbFile,
								 const StrAppBufPath & strbpDstFile);
	bool AttachDatabase();
};


/*----------------------------------------------------------------------------------------------
	This class implements the DIFwBackupDb interface.

	@h3{Hungarian: zbkup}
----------------------------------------------------------------------------------------------*/
class FwBackupDb : public SilDispatchImpl<DIFwBackupDb, &IID_DIFwBackupDb, &LIBID_FwDbServices>
{
	friend class TestDbServices::TestFwBackupDb;
public:
	// Constructors/destructors/etc.
	FwBackupDb();
	virtual ~FwBackupDb();
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// DIFwBackupDb methods
	STDMETHOD(Init)(IBackupDelegates * pbkupd, int hwndParent);
	STDMETHOD(CheckForMissedSchedules)(IUnknown * phtprovHelpUrls);
	STDMETHOD(Backup)();
	STDMETHOD(Remind)();
	STDMETHOD(UserConfigure)(IUnknown * phtprovHelpUrls, ComBool fShowRestore,
		int * pnUserAction);
	STDMETHOD(Close)();

protected:
	void InitNoHost();

	long m_cref;
	BackupHandler m_bkph;
	HWND m_hwndParent;
	bool m_fInitDone;
	BSTR m_bstrHelpFile;
	BSTR m_pbstrHelpTopic;
};

#endif //:> BACKUP_H_INCLUDED
