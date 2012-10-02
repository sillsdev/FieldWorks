/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Backup.cpp
Responsibility: Alistair Imrie
Last reviewed: never

Description:
	This file contains most of the classes needed for the backup and restore operations,
	including the user interface. It does not include the classes for handling zip files.

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include <direct.h>
// This next bit enables us to use the WIN32 API GetUserNameEx():
#define SECURITY_WIN32
#include <Security.h>
#include "..\..\AppCore\Res\ImagesSmallIdx.h" //Button images removed. Is this still needed?

#include <uxtheme.h>

#undef THIS_FILE
DEFINE_THIS_FILE

// Handle instantiating collection class methods.
#include "Vector_i.cpp"

typedef HRESULT (WINAPI *EnableThemeDialogTextureFunc)(HWND, DWORD);
typedef HRESULT (WINAPI *DllGetVersionFunc)(DLLVERSIONINFO*);


/*----------------------------------------------------------------------------------------------
	Backup/Restore dialog shell class.

	@h3{Hungarian: bkpd}
----------------------------------------------------------------------------------------------*/
class BackupDlg : public BackupDialogBase
{
	typedef BackupDialogBase SuperClass;
	friend class BackupBkpDlg;
	friend class BackupRstDlg;

public:
	// Constructor.
	BackupDlg();
	// Destructor.
	~BackupDlg();
	// Initialize a new Backup/Restore dialog:
	void Init(BackupHandler * pbkph, bool fShowRestore, HANDLE hEvent, HANDLE hMapping,
		IHelpTopicProvider * phtprovHelpUrls);

	enum BkpDlgResponse { kDoNothing, kDoBackup, kDoRestore };
	// BackupHandler::UserConfigure() calls this method to activate the Backup/Restore dialog:
	static int BackupDialog(HWND hwnd, BackupHandler * pbkph, bool fShowRestore,
		IHelpTopicProvider * phtprovHelpUrls);
	virtual SmartBstr GetHelpTopic();

protected:
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	// Process notifications from the user.
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	// Update the controls for the selected tab to show changed values.
	bool ShowChildDlg(int itab);
	void DeactivateStartButton(int rid);
	void SwapStartButtons(int ridActivate);
	static int CreateBackupDialog(HWND hwnd, BackupHandler * pbkph, bool fShowRestore,
		IHelpTopicProvider * phtprovHelpUrls, HANDLE hMapping, HANDLE hEvent);

	// The number of tabs for a Backup/Restore dialog:
	enum { kcdlgv = 2 };

	Vector<AfDialogViewPtr> m_vdlgv; // Vector of dialog tabs (each an AfDialogView).
	int m_itabCurrent; // Index of current tab.
	int m_itabInitial; // Initial tab selection.
	HWND m_hwndTab; // Handle to the tab control.
	//:> Variables used only for initialization.
	int m_dxsClient; // x position of client window; used for initialization.
	int m_dysClient; // y position of client window; used for initialization.
	StrApp m_strButtonTextCache; // Saved text from concealed Start button

	BackupHandler * m_pbkph; // Contains user's settings
	HANDLE m_hEventWnd; // Handle to Event to be signaled when we have a valid window handle.
	HANDLE m_hMapping; // Handle to File Mapping to which we copy our window handle.
	enum
	{
		kBackupRequest = 10, // Start with a number beyond Windows's own OK and Cancel ids.
		kRestoreRequest = 11,
		kClosed = 12,
	};

	EnableThemeDialogTextureFunc m_enableThemeDialogTexture;
	HINSTANCE m_hinstUxTheme;
};
typedef GenSmartPtr<BackupDlg> BackupDlgPtr;



/*----------------------------------------------------------------------------------------------
	Backup Password dialog class.
	@h3{Hungarian: bkppwd}
----------------------------------------------------------------------------------------------*/
class BackupPasswordDlg : public BackupDialogBase
{
	typedef BackupDialogBase SuperClass;

public:
	// Constructor.
	BackupPasswordDlg();

	// Initialize a new Password dialog:
	void Init(BackupPasswordInfo * pbkppwi);

	// BackupDlg calls this method to get a backup password:
	static bool PasswordDialog(HWND hwnd, BackupPasswordInfo * pbkppwi);

	virtual SmartBstr GetHelpTopic();

protected:
	// Process window messages for the static text color.
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	// Process notifications from the user.
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	virtual bool OnApply(bool fClose);
	bool ValidatePasswordChars(StrAppBuf & strbPassword);
	bool ConfirmPassword(StrAppBuf & strbPassword);

	BackupPasswordInfo * m_pbkppwi;
};
typedef GenSmartPtr<BackupPasswordDlg> BackupPasswordDlgPtr;

/*----------------------------------------------------------------------------------------------
	Scheduled Backup Warning dialog class.
	@h3{Hungarian: shdbkw}
----------------------------------------------------------------------------------------------*/
class ScheduledBackupWarningDlg : public BackupDialogBase
{
	typedef BackupDialogBase SuperClass;

public:
	enum
	{
		kStartNow,
		kOptions,
		kCancel,
	};
	// Constructor.
	ScheduledBackupWarningDlg();
	// Destructor.
	~ScheduledBackupWarningDlg();
	// Use this method to run a proper count-down warning:
	static int ScheduledBackupWarning(int nSeconds, HWND hwndPar, bool fLocked);
	void Init(int nSeconds, bool fLocked);

protected:
	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	void OnTimer(UINT nIDEvent);
	// Process notifications from the user.
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	void SetTimeLeft(int nSeconds);

	int m_nSeconds; // Number of seconds to count down from
	int m_nResponse; // How user response
	int64 m_nEndTime; // Time at which countdown will terminate
	bool m_fLocked;
};
typedef GenSmartPtr<ScheduledBackupWarningDlg> ScheduledBackupWarningDlgPtr;

/*----------------------------------------------------------------------------------------------
	Backup Nag/Reminder dialog class.
	@h3{Hungarian: bkpnag}
----------------------------------------------------------------------------------------------*/
class BackupNagDlg : public BackupDialogBase
{
	typedef BackupDialogBase SuperClass;

public:
	enum
	{
		kDoBackup,
		kCancel,
		kConfigure,
	};
	// Constructor.
	BackupNagDlg();

	// Use this method to dispaly the nag:
	static int BackupNag(int nDays, bool fLocked, HWND hwndPar = NULL, IHelpTopicProvider * phtprov = NULL);
	void Init(int nDays, IHelpTopicProvider * phtprov, bool fLocked);
	virtual SmartBstr GetHelpTopic();

protected:
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	// Process notifications from the user.
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	int m_nDays; // Number of days to display in text message.
	int m_nResponse; // How user responds
	bool m_fLocked;
};
typedef GenSmartPtr<BackupNagDlg> BackupNagDlgPtr;

/*----------------------------------------------------------------------------------------------
	Class to handle warning that user is about to overwrite an existing database with a restore,
	with option to rename project.
	@h3{Hungarian: rstdbe}
----------------------------------------------------------------------------------------------*/
class RestoreOptionsDlg : public BackupDialogBase
{
	typedef BackupDialogBase SuperClass;

public:
	// Define possible outcomes:
	enum
	{
		kNoDbClash,
		kOverwriteExisting,
		kOverwriteOther,
		kCreateNew,
		kDbReadError,
		kUserCancel,
	};

	RestoreOptionsDlg();
	void Init(StrUni stuProjectName, StrUni stuDatabaseName);

	// BackupHandler::Restore() calls this method to test for database pre-existence:
	static int DbClashAction(HWND hwnd, StrUni stuDatabaseName, StrUni & stuProjectName,
		StrUni & stuNewDatabaseName);
	//static int DbClashAction(HWND hwnd,	RestoreInfo * rsti, StrUni stuDatabaseName,
	//	StrUni & stuNewDatabaseName, StrUni & stuProjectName);

	virtual SmartBstr GetHelpTopic()
	{
		return _T("khtpRestoreOptions");
	}

protected:
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	bool m_fNewDatabaseName; // Becomes true if user opts to rename database
	StrUni m_stuProjectName; // Original name and then user's new name of project
	StrUni m_stuDatabaseName; // Name of existing database
	int m_nUserResponse; // One of a limited set of the enumerated values.
};
typedef GenSmartPtr<RestoreOptionsDlg> RestoreOptionsDlgPtr;

/*----------------------------------------------------------------------------------------------
	This class handles the mutual exclusivity of backup operations.

	@h3{Hungarian: bkpm}
----------------------------------------------------------------------------------------------*/
class BackupMutex
{
public:
	enum VStatus
	{
		kOK,
		kFailed,
		kAlreadyExists,
		kKilled,
	};
	BackupMutex();
	~BackupMutex();
	VStatus GetStatus() { return m_Status; }
	void Kill();

protected:
	HANDLE m_h;
	VStatus m_Status;
};

/*----------------------------------------------------------------------------------------------
	This class provides functionality for a combobox that displays suitable backup drives. It
	can be used as a class member inside a dialog, as long as the dialog has a ComboboxEx
	control.
	@h3{Hungarian: cbdrv}
----------------------------------------------------------------------------------------------*/
class ComboboxDrives
{
public:
	ComboboxDrives();
	void Init(HWND hwndDialog, int ctidCombobox, StrAppBuf strbSelectedDeviceName);

	void FillList();
	bool GetCurrentSelection(StrAppBuf & strb);
	static UINT CALLBACK BrowseFolderHookProc(HWND hdlg, UINT uiMsg, WPARAM wParam,
		LPARAM lParam);
	bool BrowseCurrentDeviceFolders(StrAppBuf & strb);
	bool BrowseCurrentDeviceFolders(StrAppBuf & strbPath, StrAppBuf & strbFile);
	void WriteSelectionString(StrAppBuf strb);
	bool IsValidDrive(StrAppBuf strb, StrAppBuf & strbValidDrive);

protected:
	int AddValidDrivesToComboList(achar * pchList);

	HWND m_hwndDialog; // Handle of owning dialog
	int m_ctidCombobox; // ID of ComboboxEx control
	StrAppBuf m_strbSelectedDeviceName; // Currently selected device
	Vector<StrAppBuf> m_vstrbAcceptableDrives; // List of possible backup device drive-letters
};


/*----------------------------------------------------------------------------------------------
	This class provides functionality for a combobox that displays folders for backup/restore.
	It can be used as a class member inside a dialog, as long as the dialog has a ComboboxEx
	control.
	@h3{Hungarian: cbdrv}
----------------------------------------------------------------------------------------------*/
class ComboboxFolders
{
public:
	ComboboxFolders();
	void Init(HWND hwndDialog, int ctidCombobox, StrAppBuf strbSelectedFolderName);

	void FillList();
	int GetCurrentSelection(StrAppBuf & strb);
	static UINT CALLBACK BrowseFolderHookProc(HWND hdlg, UINT uiMsg, WPARAM wParam,
		LPARAM lParam);
	bool BrowseFolders(StrAppBuf & strb, int stidTitle);
	bool BrowseFolders(StrAppBuf & strbPath, StrAppBuf & strbFile, int stidTitle);
	void WriteSelectionString(StrAppBuf strb);
	void SetBackInfo(BackupInfo* val) { m_pbkpi = val; }

protected:
	HWND m_hwndDialog; // Handle of owning dialog
	int m_ctidCombobox; // ID of ComboboxEx control
	StrAppBuf m_strbSelectedFolderName; // Currently selected device
	Vector<StrAppBuf> m_vstrbFolders; // List of possible backup folders
	BackupInfo * m_pbkpi; // Backup information (backup paths)
};


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Backup/Restore dialog's Backup tab.
	@h3{Hungarian: bkpb}
----------------------------------------------------------------------------------------------*/
class BackupBkpDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;

public:
	BackupBkpDlg();
	BackupBkpDlg(BackupDlg * pbkpd);
	~BackupBkpDlg();

	ComboboxFolders* GetAvailableFolders();

protected:
	void BasicInit();
	void SetPasswordWarningLabel();

	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	bool OnInitDlg(HWND hwndCtrl, LPARAM lParam);
	bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	bool OnButtonClicked(int ctidFrom);
	bool OnListViewItemChanged(int ctidFrom, NMHDR * pnmh);
	bool OnComboBoxEditChange(int ctidFrom);
	bool OnComboBoxSelChange(int ctidFrom);
	void UpdateStartButton();
	virtual bool SetActive();

	void ConfigureReminders();
	void ConfigureSchedule();
	void ConfigurePassword();
	void InitializeListView();

	BackupInfo * m_pbkpi; // User's choices
	HWND * m_phwndDlg; // Location of handle of owning dialog
	FwSettings * m_pfws;
	bool m_fNonUserControlSettings; // Signal to ignore certain messages while altering controls
	ComboboxDrives m_cbdrv; // Class to handle list of available drives
	ComboboxFolders m_cbfldrs; // Class to handle list of available folders
	bool m_fIsVista;		// flag that we don't need the Windows XP bug workaround in FWndProc
	EnableThemeDialogTextureFunc m_enableThemeDialogTexture;

};
typedef GenSmartPtr<BackupBkpDlg> BackupBkpDlgPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Backup/Restore dialog's Restore tab.
	@h3{Hungarian: bkprs}
----------------------------------------------------------------------------------------------*/
class BackupRstDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;

public:
	BackupRstDlg();
	BackupRstDlg(BackupDlg * pbkpd);
	~BackupRstDlg();
	void BasicInit();

	ComboboxFolders* GetAvailableFolders();

protected:
	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	bool OnInitDlg(HWND hwndCtrl, LPARAM lParam);
	bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	bool OnButtonClicked(int ctidFrom);
	bool OnListViewItemChanged(int ctidFrom, NMHDR * pnmh);
	bool OnComboBoxEditChange(int ctidFrom);
	bool OnComboBoxSelChange(int ctidFrom);
	void UpdateStartButton();
	virtual bool SetActive();

	BackupInfo * m_pbkpi;		// registry settings (etc) for backup and restore
	//RestoreInfo * m_prsti; // User's choices
	HWND * m_phwndDlg; // Location of handle of owning dialog
	ComboboxDrives m_cbdrv; // Class to handle list of available drives
	ComboboxFolders m_cbfldrs; // Class to handle list of default backup/restore folders
	AvailableProjects m_avprj; // Class to handle list of available projects
	bool m_fNonUserControlSettings; // Signal to ignore certain messages while altering controls
	bool m_fIsVista;		// flag that we don't need the Windows XP bug workaround in FWndProc
	EnableThemeDialogTextureFunc m_enableThemeDialogTexture;
};
typedef GenSmartPtr<BackupRstDlg> BackupRstDlgPtr;

/*----------------------------------------------------------------------------------------------
	Backup Reminder dialog class.
	@h3{Hungarian: bkprmd}
----------------------------------------------------------------------------------------------*/
class BackupReminderDlg : public BackupDialogBase
{
	typedef BackupDialogBase SuperClass;

public:
	enum
	{
		kMinDaysInterval = 1,
		kMaxDaysInterval = 14,
	};

	// Constructor.
	BackupReminderDlg();

	// Initialize a new Reminder dialog:
	void Init(BackupReminderInfo * pbkprmi);

	// BackupDlg calls this method to activate the Reminder dialog:
	static bool ReminderDialog(HWND hwnd, BackupReminderInfo * pbkprmi);

	virtual SmartBstr GetHelpTopic();

protected:
	// Process window messages for the static text color.
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual void SetDays(int nValue);
	virtual bool OnApply(bool fClose);
	virtual bool OnDeltaSpin(NMHDR * pnmh, long & lnRet);
	// Process notifications from the user.
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	BackupReminderInfo * m_pbkprmi; // User's choices
};
typedef GenSmartPtr<BackupReminderDlg> BackupReminderDlgPtr;


// Factor for converting seconds into 100s of nanoseconds, for use with system time functions:
static const int64 kn100NanoSeconds = 10 * 1000 * 1000;

// Useful data from host DbApp (should one be offered), or sensible defaults otherwise:
static StrUni s_stuLocalServer;
static IStreamPtr s_qfist = NULL;
static HINSTANCE s_hinst = NULL;

//:>********************************************************************************************
//:>	Structure to record user's choices for backup reminders: BackupReminderInfo.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
BackupReminderInfo::BackupReminderInfo()
{
	// Set the default values, according to spec:
	m_nDays = kDefaultDays;
	m_fTurnOff = (bool)kDefaultTurnOff;
}

/*----------------------------------------------------------------------------------------------
	Retrieve from registry the last known choices for backup reminders.
	@param pfws Pointer to object which deals with FieldWorks registry settings.
----------------------------------------------------------------------------------------------*/
void BackupReminderInfo::ReadFromRegistry(FwSettings * pfws)
{
	AssertPtr(pfws);

	// Get the number of days
	if (!pfws->GetBinary(_T("Reminders"), _T("Days"), (BYTE *)(&m_nDays), isizeof(m_nDays)))
		m_nDays = kDefaultDays;

	// Get the TurnOff flag
	if (!pfws->GetBinary(_T("Reminders"), _T("TurnOff"), (BYTE *)&m_fTurnOff, 1))
		m_fTurnOff = (bool)kDefaultTurnOff;
}

/*----------------------------------------------------------------------------------------------
	Store in registry the user's choices for backup reminders.
	@param pfws Pointer to object which deals with FieldWorks registry settings.
----------------------------------------------------------------------------------------------*/
void BackupReminderInfo::WriteToRegistry(FwSettings * pfws)
{
	AssertPtr(pfws);

	// Save the number of days
	pfws->SetBinary(_T("Reminders"), _T("Days"), (BYTE *)(&m_nDays), isizeof(m_nDays));

	// Save the TurnOff flag
	pfws->SetBinary(_T("Reminders"), _T("TurnOff"), (BYTE *)&m_fTurnOff, 1);
}



//:>********************************************************************************************
//:>	Structure to record user's choices for backup passwords: BackupPasswordInfo.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
BackupPasswordInfo::BackupPasswordInfo()
{
	// Set the default values:
	m_fLock = false;
}

/*----------------------------------------------------------------------------------------------
	Retrieve from registry the last known choices for backup passwords.
	@param pfws Pointer to object which deals with FieldWorks registry settings.
----------------------------------------------------------------------------------------------*/
void BackupPasswordInfo::ReadFromRegistry(FwSettings * pfws)
{
	AssertPtr(pfws);

	// Get the Version (for backward compatibility)
	WORD nVersion = 0;
	pfws->GetBinary(_T("ZipInfo"), _T("Version"), (BYTE *)&nVersion, 2);
	if (nVersion == 0)
	{
		// Delete the old "Password" key and data:
		HKEY hKey;
		LONG nRegErr = RegOpenKeyEx(HKEY_CURRENT_USER,
			_T("Software\\SIL\\Fieldworks\\ProjectBackup"), 0, KEY_ALL_ACCESS, &hKey);
		if (nRegErr == ERROR_SUCCESS)
			RegDeleteKey(hKey, _T("Password"));
	}

	// Get the Lock flag
	if (!pfws->GetBinary(_T("ZipInfo"), _T("Lock"), (BYTE *)&m_fLock, 1))
		m_fLock = false;

	// Get the Password encryption seed:
	int nSeed;
	if (!pfws->GetBinary(_T("ZipInfo"), _T("Seed"), (BYTE *)(&nSeed), isizeof(nSeed)))
		m_strbPassword.Clear();
	else
	{
		// Get the Password:
		int nLen = 0;
		if (pfws->GetBinary(_T("ZipInfo"), _T("Length"), (BYTE *)(&nLen), isizeof(nLen)) &&
			nLen)
		{
			BYTE * rgbEncryptedPwd = NewObj BYTE [nLen];
			if (!pfws->GetBinary(_T("ZipInfo"), _T("Data"), rgbEncryptedPwd, nLen))
				m_strbPassword.Clear();
			else
				StringEncrypter::DecryptString(rgbEncryptedPwd, nLen, m_strbPassword, nSeed);
			delete[] rgbEncryptedPwd;
		}
		else
			m_strbPassword.Clear();
	}

	// Get the Memory Jog:
	StrApp str;
	if (!pfws->GetString(_T("ZipInfo"), _T("MemoryJog"), str))
		m_strbMemoryJog.Clear();
	else
		m_strbMemoryJog.Assign(str.Chars());
}

/*----------------------------------------------------------------------------------------------
	Store to registry the user's choices for backup passwords.
	@param pfws Pointer to object which deals with FieldWorks registry settings.
----------------------------------------------------------------------------------------------*/
void BackupPasswordInfo::WriteToRegistry(FwSettings * pfws)
{
	AssertPtr(pfws);

	// Save the version info:
	WORD nVersion = kVersion;
	pfws->SetBinary(_T("ZipInfo"), _T("Version"), (BYTE *)(&nVersion), 2);

	// Save the Lock flag:
	pfws->SetBinary(_T("ZipInfo"), _T("Lock"), (BYTE *)(&m_fLock), 1);

	// Save the Password:
	int nLen = m_strbPassword.Length();
	BYTE * rgbEncryptedPwd = NewObj BYTE [nLen];
	int nSeed = 0;
	if (!rgbEncryptedPwd)
		nLen = 0;
	else
		nSeed = StringEncrypter::EncryptString(m_strbPassword, rgbEncryptedPwd);
	pfws->SetBinary(_T("ZipInfo"), _T("Length"), (BYTE *)(&nLen), isizeof(nLen));
	pfws->SetBinary(_T("ZipInfo"), _T("Data"), rgbEncryptedPwd, nLen);
	delete[] rgbEncryptedPwd;
	rgbEncryptedPwd = NULL;

	// Save the Password encryption seed:
	pfws->SetBinary(_T("ZipInfo"), _T("Seed"), (BYTE *)(&nSeed), isizeof(nSeed));

	// Save the Memory Jog:
	StrApp str(m_strbMemoryJog.Chars());
	pfws->SetString(_T("ZipInfo"), _T("MemoryJog"), str);
}


//:>********************************************************************************************
//:>	Main structure to record user's choices for backups: BackupInfo.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
BackupInfo::BackupInfo()
{
	m_fXml = false;
	m_fLimitBackupToActive = false;
	m_pbkupd = NULL;
}

/*----------------------------------------------------------------------------------------------
	Initializaton.
	@param pfws Pointer to object which deals with FieldWorks registry settings.
----------------------------------------------------------------------------------------------*/
bool BackupInfo::Init(FwSettings * pfws)
{
	m_fws = *pfws;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Retrieve from registry the last known choices for backups.
	@param pfws Pointer to object which deals with FieldWorks registry settings.
----------------------------------------------------------------------------------------------*/
void BackupInfo::ReadFromRegistry(FwSettings * pfws, BSTR defaultBackupDir)
{
	AssertPtr(pfws);

	// Get the XML flag:
	if (!pfws->GetBinary(_T("Basics"), _T("XML"), (BYTE *)&m_fXml, 1))
		m_fXml = (bool)kDefaultXml;

	//// Get the device name:
	//if (!pfws->GetString(_T("Basics"), _T("Device"), str))
	//	str.Assign(L"c:\\");
	//m_strbDeviceName.Assign(str.Chars());

	// Get the full path of the default backup directory:
	StrApp str;
	str.Clear();
	if (!pfws->GetString(_T("Basics"), _T("DefaultBackupDirectory"), str))
	{
		if (defaultBackupDir != NULL)
			m_strbDefaultDirectoryPath = defaultBackupDir;
		else
			m_strbDefaultDirectoryPath.Assign(L"C:\\");
	}
	else
		m_strbDefaultDirectoryPath.Assign(str.Chars());

	// Get the full path of the last used backup directory:
	str.Clear();
	if (!pfws->GetString(_T("Basics"), _T("Path"), str))
		m_strbDirectoryPath = m_strbDefaultDirectoryPath; // set to default if not previously set
	else
		m_strbDirectoryPath.Assign(str.Chars());

	m_bkprmi.ReadFromRegistry(pfws);
	m_bkppwi.ReadFromRegistry(pfws);
}

/*----------------------------------------------------------------------------------------------
	Store to registry the user's choices for backups.
	@param pfws Pointer to object which deals with FieldWorks registry settings.
----------------------------------------------------------------------------------------------*/
void BackupInfo::WriteSettingsToRegistry(FwSettings * pfws)
{
	AssertPtr(pfws);

	// Save the number XML flag
	pfws->SetBinary(_T("Basics"), _T("XML"), (BYTE *)(&m_fXml), 1);

	//// Save the device name:
	//StrApp str(m_strbDeviceName.Chars());
	//pfws->SetString(_T("Basics"), _T("Device"), str);

	// Save the directory path:
	StrApp str(m_strbDirectoryPath.Chars());
	pfws->SetString(_T("Basics"), _T("Path"), str);

	m_bkprmi.WriteToRegistry(pfws);
	m_bkppwi.WriteToRegistry(pfws);
}

/*----------------------------------------------------------------------------------------------
	Read from the master database which databases are FieldWorks databases, and get the project
	name. Read the last backup time of each database from the registry. If any
	database listed in the registry is now obsolete, its registry record is deleted.

	@param pfws Pointer to object which deals with FieldWorks registry settings.
----------------------------------------------------------------------------------------------*/
bool BackupInfo::ReadDbProjectsFromRegistry(FwSettings * pfws)
{
	ComBool fIsNull;
	ComBool fMoreRows;
	IOleDbEncapPtr qode; // Declare before qodc.
	IOleDbCommandPtr qodc;
	StrUni stuSql;
	StrUni stu;
	IStreamPtr qfist;
	Vector<StrApp> vstrRemainingKeys;

	try
	{
		m_vprojd.Clear();
		HRESULT hr;
		ComBool fIsNull;
		ComBool fMoreRows;
		UINT luSpaceTaken;
		IOleDbEncapPtr qode; // Declare before qodc.
		IOleDbCommandPtr qodc;
		StrUni stuSql;
		StrUni stuMaster = "master";

		StrUtil::InitIcuDataDir();

		qode.CreateInstance(CLSID_OleDbEncap);
		CheckHr(hr = qode->Init(s_stuLocalServer.Bstr(), stuMaster.Bstr(), qfist, koltMsgBox, koltvForever));

		stuSql = L"exec master..sp_GetFWDBs";
		CheckHr(hr = qode->CreateCommand(&qodc));
		CheckHr(hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(hr = qodc->GetRowset(0));
		CheckHr(hr = qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			ProjectData projd;

			// Fetch database name:
			OLECHAR rgchDbName[MAX_PATH];
			CheckHr(hr = qodc->GetColValue(1,
						reinterpret_cast <BYTE *>(rgchDbName), isizeof(OLECHAR) * MAX_PATH,
						&luSpaceTaken, &fIsNull, 2));
			if (fIsNull)
				ThrowHr(E_UNEXPECTED);
			projd.m_stuDatabase.Assign(rgchDbName);

			// Remember this key name in our list:
			StrApp strKeyName(projd.m_stuDatabase.Chars());
			vstrRemainingKeys.Push(strKeyName);
			StrApp strSubKey;
			strSubKey.Format(_T("Projects\\%s"), strKeyName.Chars());

			// Get time of project's last backup:
			FILETIME filt;
			if (!pfws->GetBinary(strSubKey.Chars(), _T("LastBackupTime"), (BYTE *)&filt, 8))
			{
				// Last backup time not found. Assume this is the first time application has
				// been run, and set 'last backup time' to earliest possible date:
				filt.dwHighDateTime = 0;
				filt.dwLowDateTime = 0;
				pfws->SetBinary(strSubKey.Chars(), _T("LastBackupTime"), (BYTE *)&filt, 8);
			}
			projd.m_filtLastBackup = filt;
			m_vprojd.Push(projd);

			CheckHr(hr = qodc->NextRow(&fMoreRows));
		} // Next project in current database
		qodc.Clear(); // Clear before qode.
		qode.Clear();
	}
	catch (...)
	{
		return false;
	}

	// Now check all projects in our registry area, to see which are still alive:
	CleanupRegistry(vstrRemainingKeys);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Read from the master database all FieldWorks databases.

	@param vstu - reference to a vector for returning FieldWorks database names
	@param pfist - stream for logging
	@param bstrServer - database server name
----------------------------------------------------------------------------------------------*/
void BackupInfo::CollectAllFwDbs(Vector<StrUni> & vstu, IStream * pfist, BSTR bstrServer)
{
	IOleDbEncapPtr qode; // Declare before qodc.
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	HRESULT hr;
	UINT luSpaceTaken;
	OLECHAR rgchDbName[MAX_PATH];
	StrUni stu;

	// Connect to the "master" table of the local database.
	StrUni stuDatabase(L"master");
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(hr = qode->Init(bstrServer, stuDatabase.Bstr(), pfist, koltMsgBox, koltvForever));

	// Find the FW databases available on the currently selected system.
	StrUni stuSql(L"exec master..sp_GetFWDBs");
	CheckHr(hr = qode->CreateCommand(&qodc));
	CheckHr(hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
	CheckHr(hr = qodc->GetRowset(0));
	CheckHr(hr = qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		// If there are indeed some FW databases, add them to our list.
		luSpaceTaken = 0;
		CheckHr(hr = qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchDbName),
			isizeof(OLECHAR) * MAX_PATH, &luSpaceTaken, &fIsNull, 2));
		if (luSpaceTaken)
		{
			stu = rgchDbName;
			vstu.Push(stu);
		}
		CheckHr(hr = qodc->NextRow(&fMoreRows));
	}
	qodc.Clear(); // Clear before qode.
	qode.Clear();
}

/*----------------------------------------------------------------------------------------------
	Remove any registry keys for databases/projects which no longer exist.

	@param vstrRemainingKeys - vector of registry keys to check
----------------------------------------------------------------------------------------------*/
void BackupInfo::CleanupRegistry(Vector<StrApp> & vstrRemainingKeys)
{
	HKEY hKey;
	LONG nRegErr = RegOpenKeyEx(HKEY_CURRENT_USER,
		_T("Software\\SIL\\Fieldworks\\ProjectBackup\\Projects"), 0, KEY_ALL_ACCESS, &hKey);
	if (nRegErr == ERROR_SUCCESS)
	{
		// Get data to enable us to enumerate subkeys:
		DWORD nSubKeys = 0;
		DWORD nLongestLen = 0;
		nRegErr = RegQueryInfoKey(hKey, NULL, NULL, NULL, &nSubKeys, &nLongestLen, NULL, NULL,
			NULL, NULL, NULL, NULL);
		nLongestLen++;
		if (nRegErr == ERROR_SUCCESS)
		{
			Vector<StrApp> vstrDoomedKeys;
			// Enumerate all subkeys in our Projects Key:
			achar * szKey = NewObj achar [nLongestLen];
			int i;
			for (i = 0; i < (int)nSubKeys; i++)
			{
				DWORD nLen = nLongestLen;
				nRegErr = RegEnumKeyEx(hKey, i, szKey, &nLen, NULL, NULL, NULL, NULL);
				if (nRegErr == ERROR_SUCCESS)
				{
					int i2;
					bool fFound = false;
					for (i2 = 0; i2 < vstrRemainingKeys.Size(); i2++)
					{
						if (vstrRemainingKeys[i2].Equals(szKey))
						{
							fFound = true;
							break;
						}
					} // Next key in our list of remaining keys
					if (!fFound)
						vstrDoomedKeys.Push(StrApp(szKey));
				} // End if key could be enumerated
			} // Next key in enumeration
			delete[] szKey;

			// Now delete all keys named in our vstrDoomedKeys vector:
			for (i = 0; i < vstrDoomedKeys.Size(); i++)
				RegDeleteKey(hKey, vstrDoomedKeys[i].Chars());

		} // End if key info could be queried
	} // End if key could be opened

	RegCloseKey(hKey);
}


/*----------------------------------------------------------------------------------------------
	Store to registry the last backup time of all FW database projects.
	@param pfws Pointer to object which deals with FieldWorks registry settings.
----------------------------------------------------------------------------------------------*/
void BackupInfo::WriteDbProjectsToRegistry(FwSettings * pfws)
{
	for (int i = 0; i < m_vprojd.Size(); i++)
	{
		ProjectData projd = m_vprojd[i];
		StrApp strSubKey;
		strSubKey.Format(_T("Projects\\%s"), projd.m_stuDatabase.Chars());
		pfws->SetBinary(strSubKey.Chars(), _T("LastBackupTime"),
			(BYTE *)(&projd.m_filtLastBackup), 8);
	}
}

/*----------------------------------------------------------------------------------------------
	Examine each project in the database, and determine if it has been altered since its last
	backup. If so, select it for backup.
	@param nTotalAltered [out] number of projects altered since their last backup.
	@return False if there were any problems getting the data.
----------------------------------------------------------------------------------------------*/
bool BackupInfo::AutoSelectBackupProjects(int & nTotalAltered)
{
	nTotalAltered = 0;

	if (!m_vprojd.Size())
		ReadDbProjectsFromRegistry(&m_fws);

	bool fResult = true;
	HRESULT hr;

	for (int i = 0; fResult && i < m_vprojd.Size(); i++)
	{
		ProjectData & projd = m_vprojd[i];

		projd.m_fBackup = false;

		IOleDbEncapPtr qode; // Declare before qodc.
		IOleDbCommandPtr qodc;
		IStreamPtr qfist;

		try
		{
			if (m_fLimitBackupToActive)
			{
				ComBool fIsActive;
				// Always use the local machine.
				CheckHr(m_pbkupd->IsDbOpen_Bkupd(s_stuLocalServer.Chars(),
					projd.m_stuDatabase.Chars(), &fIsActive));
				if (!fIsActive)
					continue;
			}

			// Connect to the database containing the current project:
			qode.CreateInstance(CLSID_OleDbEncap);
			CheckHr(hr = qode->Init(s_stuLocalServer.Bstr(), projd.m_stuDatabase.Bstr(),
				s_qfist, koltMsgBox, koltvForever));
			CheckHr(hr = qode->CreateCommand(&qodc));
			// Check whether the project needs to be backed up.
			CheckProjectForBackup(projd, qode, qodc, nTotalAltered, fResult);
		}
		catch (...)
		{
			fResult = false;
		}
		qodc.Clear(); // just in case...
		qode.Clear();

	} // Next project.

	return fResult;
}


/*----------------------------------------------------------------------------------------------
	Examine a project in the database, and determine if it has been altered since its last
	backup. If so, select it for backup.

	@param projd - reference to a ProjectData object
	@param pode - pointer to IOleDbEncap object
	@param podc - pointer to IOleDbCommand object
	@param nTotalAltered - reference to number of projects altered since their last backup.
	@param fResult - set true if everything okay, set false if an error occurs
----------------------------------------------------------------------------------------------*/
void BackupInfo::CheckProjectForBackup(ProjectData & projd, IOleDbEncap * pode,
	IOleDbCommand * podc, int & nTotalAltered, bool & fResult)
{
	HRESULT hr;
	ComBool fIsNull;
	ComBool fMoreRows;
	UINT luSpaceTaken;
	unsigned short rgnDateTime[8];
	StrUni stuSql;

	// Find the most recent time/date stamp on the database:
	stuSql = L"select max([upddttm]) from [cmobject]";
	CheckHr(hr = podc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(hr = podc->GetRowset(0));
	CheckHr(hr = podc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		luSpaceTaken = 0;
		CheckHr(hr = podc->GetColValue(1, reinterpret_cast <BYTE *>(rgnDateTime),
			isizeof(rgnDateTime), &luSpaceTaken, &fIsNull, 0));
		if (luSpaceTaken)
		{
			// Get last backup time:
			SYSTEMTIME systLastBackupTime;
			FileTimeToSystemTime(&projd.m_filtLastBackup, &systLastBackupTime);

			// Compare, to see which is earlier:
			bool fBackupNeeded = false;
			if (rgnDateTime[0] >= systLastBackupTime.wYear)
			{	// Year:
				if (rgnDateTime[0] > systLastBackupTime.wYear)
					fBackupNeeded = true;
				else if (rgnDateTime[1] >= systLastBackupTime.wMonth)
				{	// Month:
					if (rgnDateTime[1] > systLastBackupTime.wMonth)
						fBackupNeeded = true;
					else if (rgnDateTime[2] >= systLastBackupTime.wDay)
					{	// Day:
						if (rgnDateTime[2] > systLastBackupTime.wDay)
							fBackupNeeded = true;
						else if (rgnDateTime[3] >= systLastBackupTime.wHour)
						{	// Hour:
							if (rgnDateTime[3] > systLastBackupTime.wHour)
								fBackupNeeded = true;
							else if (rgnDateTime[4] >= systLastBackupTime.wMinute)
							{	// Minute
								if (rgnDateTime[4] > systLastBackupTime.wMinute)
									fBackupNeeded = true;
								// Second
								else if (rgnDateTime[5] > systLastBackupTime.wSecond)
									fBackupNeeded = true;
							}
						}
					}
				}
			}
			if (fBackupNeeded)
			{
				projd.m_fBackup = true;
				nTotalAltered++;
			}
		}
		else // Problem getting database info
		{
			fResult = false;
		}
	}
	else // No results from database query:
	{
		fResult = false;
	}
}



//:>********************************************************************************************
//:>	BackupFileNameProcessor methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Declare the internal methods of the BackupFileNameProcessor namespace which should not be
	visible to the outside world.  (In a class, they would be "protected" or "private".
----------------------------------------------------------------------------------------------*/
namespace BackupFileNameProcessor
{
	static int GetProjectNameLastCharIndex(StrAppBufPath strbpName);
};

/*----------------------------------------------------------------------------------------------
	Make up zip file name. This consists of the name of the language project plus the date and
	time. E.g. "Indonesian 2000-11-27 1430.zip". If the project name and database name differ,
	the database name will be included, in brackets "()", after the project name, e.g.
	"Indonesian (Indonesian-Old) 2000-11-27 1430.zip"
	@param strbPath The path for the file.
	@param stuProjectName The name of the project.
	@param stuDatabaseName The name of the database containing the project.
	@param stuFileName [out] The returned file name, with a .zip extension.
----------------------------------------------------------------------------------------------*/
void BackupFileNameProcessor::GenerateFileName(StrAppBuf strbPath, StrUni stuProjectName,
	StrUni stuDatabaseName, StrUni & stuFileName)
{
	stuFileName.Clear();
	StrUni stuPath(strbPath.Chars()); // Make sure path name is Unicode
	if (strbPath.Length() == 0 || strbPath[strbPath.Length() - 1] != '\\')
		stuPath.Append("\\");

	SYSTEMTIME systTime;
	GetLocalTime(&systTime);
	Assert(systTime.wMinute >= 0 && systTime.wMinute <= 59);
	Assert(systTime.wHour >= 0 && systTime.wHour <= 23);
	Assert(systTime.wDay >= 1 && systTime.wDay <= 31);
	Assert(systTime.wMonth >= 1 && systTime.wMonth <= 12);

	stuFileName.Format(L"%s%s %04d-%02d-%02d %02d%02d.zip", stuPath.Chars(),
	stuDatabaseName.Chars(), systTime.wYear, systTime.wMonth, systTime.wDay,
	systTime.wHour, systTime.wMinute);
}

/*----------------------------------------------------------------------------------------------
	Check to see that the given file name contains all the elements of a FieldWorks backup
	file i.e. it must end with a date and time stamp and an extension, correctly formatted.
----------------------------------------------------------------------------------------------*/
bool BackupFileNameProcessor::IsValidFileName(StrAppBufPath strbpName)
{
	if (GetProjectNameLastCharIndex(strbpName) > 0)
		return true;

	return false;
}

/*----------------------------------------------------------------------------------------------
	Analyzes a backup's file name, and recreates the original project name. This does not
	include the database name in any circumstances. If the database name may be required, see
	${#GetProjectFullName}.
	@param strbpName The file name of the backup.
	@param strProjectName [out] The original project's name.
	@return True if the name could be recreated.
----------------------------------------------------------------------------------------------*/
bool BackupFileNameProcessor::GetProjectName(StrAppBufPath strbpName, StrApp & strProjectName)
{
	// Get full project name, then remove words in brackets, if they exist:
	StrApp strFullName;
	if (!GetProjectFullName(strbpName, strFullName))
		return false;

	strProjectName.Clear();

	// Look for last '(':
	StrAppBuf strbOpenBracket("(");
	int ichEnd = strFullName.ReverseFindCh(strbOpenBracket[0]);
	if (ichEnd == -1)
	{
		// There was no open bracket, so the full name we already have is the project name:
		strProjectName.Assign(strFullName.Chars());
		return true;
	}
	// Move ahead of bracket character:
	ichEnd--;

	strProjectName.Assign(strFullName.Left(ichEnd).Chars());

	return true;
}

/*----------------------------------------------------------------------------------------------
	Analyzes a backup's file name, and recreates the original project name, which will include
	the database name in brackets, if it is different from the project name.
	@param strbpName The file name of the backup.
	@param strProjectFullName [out] The original project's name.
	@return True if the name could be recreated.
----------------------------------------------------------------------------------------------*/
bool BackupFileNameProcessor::GetProjectFullName(StrAppBufPath strbpName,
	StrApp & strProjectFullName)
{
	strProjectFullName.Clear();
	int ichEnd = GetProjectNameLastCharIndex(strbpName);
	if (ichEnd == -1)
		return false;
	ichEnd++;

	// Look for last backslash:
	StrAppBuf strbSlash("\\");
	int ichStart = strbpName.ReverseFindCh(strbSlash[0]);
	ichStart++;

	strProjectFullName.Assign(strbpName.Mid(ichStart, ichEnd-ichStart).Chars());

	return true;
}

/*----------------------------------------------------------------------------------------------
	Analyzes a backup's file name, and recreates the original database name, which is contained
	in parentheses. For example, a file name of Kalaba (TestLangProj) 2002-07-09 1409.zip
	will produce a database name of TestLangProj. If there are no parentheses, the project name
	will be returned. For example, Lela-Teli Sample 2002-06-06 1450.zip will return
	Lela-Teli Sample.
	@param strbpName The file name of the backup.
	@param strDatabaseName [out] The database name.
	@return True if the name could be created.
----------------------------------------------------------------------------------------------*/
bool BackupFileNameProcessor::GetDatabaseName(StrAppBufPath strbpName, StrApp & strDatabaseName)
{
	// Get full project name, then isolate words in brackets, if they exist:
	StrApp strFullName;
	if (!GetProjectFullName(strbpName, strFullName))
		return false;

	strDatabaseName.Clear();

	// Look for last ')':
	StrAppBuf strbCloseBracket(")");
	int ichEnd = strFullName.ReverseFindCh(strbCloseBracket[0]);
	if (ichEnd == -1)
	{
		// There was no close bracket, so the full name we already have is the project name:
		strDatabaseName.Assign(strFullName.Chars());
		return true;
	}
	// Look for last '(':
	StrAppBuf strbOpenBracket("(");
	int ichStart = strFullName.ReverseFindCh(strbOpenBracket[0]);
	if (ichStart == -1)
	{
		// There was no open bracket, so the full name we already have is the project name:
		strDatabaseName.Assign(strFullName.Chars());
		return true;
	}

	strDatabaseName.Assign(strFullName.Mid(++ichStart, ichEnd - ichStart).Chars());

	return true;
}

/*----------------------------------------------------------------------------------------------
	Analyzes a backup's file name, and creates the version name needed for display in the list
	of available backed-up projects.
	@param strbpName The file name of the backup.
	@param strVersionName [out] The project's version display name.
	@return True if the name could be created.
----------------------------------------------------------------------------------------------*/
bool BackupFileNameProcessor::GetVersionName(StrAppBufPath strbpName, StrApp & strVersionName)
{
	strVersionName.Clear();

	// Find end of project name:
	int ichEnd = GetProjectNameLastCharIndex(strbpName);
	if (ichEnd == -1)
		return false;
	// Point to space after project name:
	ichEnd++;

	// Look for last backslash:
	StrAppBuf strbSlash("\\");
	int ichStart = strbpName.ReverseFindCh(strbSlash[0]);
	ichStart++;

	// Now make up a string including the project name, the date, and the time with a ':'
	// inserted before the minutes:
	Assert(strbpName.Length() >= ichEnd + 16);
	strVersionName.Format(_T("%s -%s:%s"), strbpName.Mid(ichStart, ichEnd - ichStart).Chars(),
		strbpName.Mid(ichEnd, 14).Chars(), strbpName.Mid(ichEnd + 14, 2).Chars());

	return true;
}

/*----------------------------------------------------------------------------------------------
	Analyzes a backup's file name, and creates the 'version' string, which is the time and date
	of the backup.
	@param strbpName The file name of the backup.
	@param strVersion [out] The project's version string.
	@return True if the name could be created.
----------------------------------------------------------------------------------------------*/
bool BackupFileNameProcessor::GetVersion(StrAppBufPath strbpName, StrApp & strVersion)
{
	strVersion.Clear();

	// Find end of project name:
	int ichStart = GetProjectNameLastCharIndex(strbpName);
	if (ichStart == -1)
		return false;

	// Now make up a string including the project date and the time with a ':' inserted before
	// the minutes:
	Assert(strbpName.Length() >= ichStart + 17);
	strVersion.Format(_T("%s:%s"), strbpName.Mid(ichStart + 2, 13).Chars(),
		strbpName.Mid(ichStart + 15, 2).Chars());

	return true;
}

/*----------------------------------------------------------------------------------------------
	Tests a file name to find where the last character of the project name is. This involves
	reverse-searching through the file name, to check that the time and date stamp is correctly
	formatted.
	@return -1 if file name is incorrectly formatted, else index of project name last character.
----------------------------------------------------------------------------------------------*/
static int BackupFileNameProcessor::GetProjectNameLastCharIndex(StrAppBufPath strbpName)
{
	StrAppBuf strbDot(_T("."));
	StrAppBuf strbDash(_T("-"));
	StrAppBuf strbSpace(_T(" "));

	// Look for start of file name's extension:
	int ich = strbpName.ReverseFindCh(strbDot[0]);
	// Check if there are at least enough characters for date and time:
	if (ich == -1 || ich <= 15)
		return -1;

	// Check if the previous 2 characters are numeric (the minute of the backup):
	if (!_istdigit(strbpName[--ich]))
		return -1;
	if (!_istdigit(strbpName[--ich]))
		return -1;

	// Check if the previous 2 characters are numeric (the hour of the backup):
	if (!_istdigit(strbpName[--ich]))
		return -1;
	if (!_istdigit(strbpName[--ich]))
		return -1;

	// Check if the previous character is a space:
	if (strbpName[--ich] != strbSpace[0])
		return -1;

	// Check if the previous 2 characters are numeric (the day in month of the backup):
	if (!_istdigit(strbpName[--ich]))
		return -1;
	if (!_istdigit(strbpName[--ich]))
		return -1;

	// Check if the previous character is a dash:
	if (strbpName[--ich] != strbDash[0])
		return -1;

	// Check if the previous 2 characters are numeric (the month of the backup):
	if (!_istdigit(strbpName[--ich]))
		return -1;
	if (!_istdigit(strbpName[--ich]))
		return -1;

	// Check if the previous character is a dash:
	if (strbpName[--ich] != strbDash[0])
		return -1;

	// Check if the previous 4 characters are numeric (the year of the backup):
	if (!_istdigit(strbpName[--ich]))
		return -1;
	if (!_istdigit(strbpName[--ich]))
		return -1;
	if (!_istdigit(strbpName[--ich]))
		return -1;
	if (!_istdigit(strbpName[--ich]))
		return -1;

	// Check if the previous character is a space:
	if (strbpName[--ich] != strbSpace[0])
		return -1;

	return --ich;
}

//:>********************************************************************************************
//:>	BackupDialogBase methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	This is an alternative to ${AfDialog#OnHelp}, so that we don't need to rely on
	${AfApp::Papp}, which may not be available.
----------------------------------------------------------------------------------------------*/
bool BackupDialogBase::OnHelp()
{
	if (!AfDialog::s_qhtprovHelpUrls)
		return false; // Can't determine what help file/topic is needed.

	StrApp strHelpFile;
	StrApp strHelpTopic;
	SmartBstr sbstr;
	SmartBstr helpFile = _T("UserHelpFile");
	AfDialog::s_qhtprovHelpUrls->GetHelpString(helpFile, -1, &sbstr);
	strHelpFile.Assign(sbstr.Chars());

	SmartBstr helpTopic = GetHelpTopic();
	AfDialog::s_qhtprovHelpUrls->GetHelpString(helpTopic, -1, &sbstr);
	strHelpTopic.Assign(sbstr.Chars());

	StrAppBufPath strbpFwPath;
	strbpFwPath.Assign(DirectoryFinder::FwRootCodeDir().Chars());

	StrApp strHelp;
	strHelp.Assign(strbpFwPath.Chars());
	strHelp.Append(strHelpFile);
	strHelp.Append(_T("::/"));
	strHelp.Append(strHelpTopic);

	HtmlHelp(::GetDesktopWindow(), strHelp.Chars(), HH_DISPLAY_TOPIC, NULL);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Specify "not implemented" help topic if the class does not specify a help topic.
----------------------------------------------------------------------------------------------*/
SmartBstr BackupDialogBase::GetHelpTopic()
{
	return _T("unknown");
}

//:>********************************************************************************************
//:>	BackupDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
BackupDlg::BackupDlg()
{
	m_rid = kridDlgBackupRestore;
	m_pszHelpUrl =
		_T("User_Interface/Menus/File/Backup_and_Restore/Backup_and_Restore_Backup_tab.htm");
	m_pbkph = NULL;
	m_itabCurrent = -1;
	m_itabInitial = 0; // 0 is the Backup tab.
	m_hwndTab = NULL; // Handle of tab control.
	m_dxsClient = 0;
	m_dysClient = 0;
	m_hEventWnd = NULL;
	m_hMapping = NULL;

	m_enableThemeDialogTexture = NULL;
	m_hinstUxTheme = LoadLibrary(L"uxtheme.dll");
	if (m_hinstUxTheme != NULL)
	{
		HINSTANCE hinstComCtrl = LoadLibrary(L"comctl32.dll");
		if (hinstComCtrl != NULL)
		{
			DllGetVersionFunc dllGetVersion = (DllGetVersionFunc) GetProcAddress(hinstComCtrl, "DllGetVersion");
			if (dllGetVersion != NULL)
			{
				DLLVERSIONINFO dvi;
				dvi.cbSize = sizeof(DLLVERSIONINFO);
				if (dllGetVersion(&dvi) == S_OK && dvi.dwMajorVersion >= 6)
					m_enableThemeDialogTexture = (EnableThemeDialogTextureFunc) GetProcAddress(m_hinstUxTheme,
						"EnableThemeDialogTexture");
			}
			FreeLibrary(hinstComCtrl);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
BackupDlg::~BackupDlg()
{
	if (m_hinstUxTheme != NULL)
		FreeLibrary(m_hinstUxTheme);
}


/*----------------------------------------------------------------------------------------------
	Initialization. This is called by ${#BackupDialog}.
	@param pbkph The current user-settings.
	@param fShowRestore True if Restore tab should be the initial tab shown.
	@param hEvent Handle to Event to be signaled when we have a valid window handle.
	@param hMapping Handle to File Mapping (shared memory) to which we copy our window handle.
	@param phtprovHelpUrls pointer to a help topic provider to get app-specific information
	about the help file and topic for this dialog.
----------------------------------------------------------------------------------------------*/
void BackupDlg::Init(BackupHandler * pbkph, bool fShowRestore, HANDLE hEvent, HANDLE hMapping,
					 IHelpTopicProvider * phtprovHelpUrls)
{
	AssertPtr(pbkph);

	m_pbkph = pbkph;
	m_itabInitial = fShowRestore ? 1 : 0;
	m_hEventWnd = hEvent;
	m_hMapping = hMapping;
	if (phtprovHelpUrls)
		AfDialog::s_qhtprovHelpUrls = phtprovHelpUrls;
}

/*----------------------------------------------------------------------------------------------
	${BackupHandler#UserConfigure} calls this method to activate the Backup/Restore dialog.
	@param hwnd Window handle to be used as parent
	@param pbkph User's backup and restore settings.
	@param fShowRestore True if Restore tab should be the initial tab shown.
	@return Enumerated value, describing what user did.
	@param phtprovHelpUrls - pointer to a help topic provider to get app-specific information
----------------------------------------------------------------------------------------------*/
int BackupDlg::BackupDialog(HWND hwnd, BackupHandler * pbkph, bool fShowRestore,
	IHelpTopicProvider * phtprovHelpUrls)
{
	AssertPtr(pbkph);

	// Initialize result value to worst case scenario:
	int nResult = BackupHandler::kUnknownError;

	// If this dialog is already running, because it has been triggered by some other thread or
	// process, then we just need to make it the top window and give it focus. To test this, we
	// will use a block of shared memory to store the window handle of the active dialog, and a
	// Windows Event to block other dialogs being created, during the time between the creation
	// of the dialog object and the creation of its window handle.
	bool fDialogAlreadyPresent = false;
	// Create an Event, initially reset so we can block this routine if another thread calls it:
	HANDLE hEvent = ::CreateEvent(NULL, true, false, _T("SIL FieldWorks Backup Options"));
	// Remember if we created it new, or access a pre-existing one:
	DWORD nEventLastError = GetLastError();
	// A mapped file will serve as our shared memory block, with enough bytes to store an HWND:
	HANDLE hMapping = ::CreateFileMapping((HANDLE)0xFFFFFFFF, NULL, PAGE_READWRITE, 0,
		isizeof(HWND), _T("SIL FieldWorks Backup Options HWND"));

	// If anything goes wrong with this mechanism, we will go ahead and create a new dialog - it
	// is not a fatal problem:
	if (hEvent)
	{
		// See if we created the Event ourselves, or if it pre-existed:
		if (nEventLastError == ERROR_ALREADY_EXISTS)
		{
			// It pre-existed, which means the dialog is already in the process of being
			// created. We must wait for the event to be signaled, so that the dialog's handle
			// can be accessed:
			::WaitForSingleObject(hEvent, INFINITE);

			// See if we got access to the shared memory:
			if (hMapping)
			{
				// Get a pointer to the shared memory block:
				void * pSharedMem = ::MapViewOfFile(hMapping, FILE_MAP_ALL_ACCESS, 0, 0, 0);
				if (pSharedMem)
				{
					// Read the window handle of the existing dialog:
					HWND hwnd = *((HWND *)pSharedMem);
					if (hwnd)
					{
						// Make the existing dialog obvious to the user:
						::BringWindowToTop(hwnd);
						::SetFocus(hwnd);
						// Remember that we found another instance:
						fDialogAlreadyPresent = true;
					}
					::UnmapViewOfFile(pSharedMem);
				}
			} // End if File Mapping exxists
		} // End if Event already exists
	} // End if Event found or created

	if (!fDialogAlreadyPresent)
	{
		// We didn't manage to find another instance and bring it up, so go ahead and create a
		// new dialog:
		nResult = CreateBackupDialog(hwnd, pbkph, fShowRestore, phtprovHelpUrls, hMapping,
			hEvent);
	}

	// Clear up:
	::CloseHandle(hMapping);
	::CloseHandle(hEvent);

	return nResult;
}

/*----------------------------------------------------------------------------------------------
	BackupDialog() calls this method to actually create the Backup/Restore dialog.

	@param hwnd - Window handle to be used as parent
	@param pbkph - User's backup and restore settings.
	@param fShowRestore - True if Restore tab should be the initial tab shown.
	@param phtprovHelpUrls - pointer to a help topic provider to get app-specific information
	@param hMapping - handle used to prevent multiple instances, even from different apps.
	@param hEvent - handle used to prevent multiple instances, even from different apps.
----------------------------------------------------------------------------------------------*/
int BackupDlg::CreateBackupDialog(HWND hwnd, BackupHandler * pbkph, bool fShowRestore,
	IHelpTopicProvider * phtprovHelpUrls, HANDLE hMapping, HANDLE hEvent)
{
	// Initialize result value to worst case scenario:
	int nResult = BackupHandler::kUnknownError;

	BackupDlgPtr qbkpd;
	qbkpd.Create();

	qbkpd->Init(pbkph, fShowRestore, hEvent, hMapping, phtprovHelpUrls);

	// Run the dialog.
	switch (qbkpd->DoModal(hwnd))
	{
	case kBackupRequest:
		if (pbkph->Backup(BackupHandler::kManual, hwnd))
			nResult = BackupHandler::kBackupOk;
		else
			nResult = BackupHandler::kBackupFail;
		break;
	case kRestoreRequest:
		{
			int stid = pbkph->CheckTargetDirectory();
			if (stid == 1)
			{
				nResult = pbkph->Restore();
			}
			else
			{
				nResult = BackupHandler::kCannotRestore;
				StrApp strMsg(stid);
				StrApp strAppTitle(kstidBkpSystem);
				if (stid == kstidCantWriteToRestore)
					strMsg.Format(strMsg.Chars(), pbkph->m_strbpTargetDir.Chars());
				::MessageBox(NULL, strMsg.Chars(), strAppTitle.Chars(), MB_ICONEXCLAMATION);
			}
		}
		break;
	case kClosed:
		// Fall through:
	case kctidCancel:
		// Fall through:
	case kctidOk:
		nResult = BackupHandler::kUserClosed;
		break;
	default:
		Assert(false);
		break;
	}

	// Save backup settings, however user closed dialog:
	pbkph->m_bkpi.WriteSettingsToRegistry(&pbkph->m_fws);

	return nResult;
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they need
	initial values.)
----------------------------------------------------------------------------------------------*/
bool BackupDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// First, write our m_hwnd to the block of shared memory, so that if another request for
	// this dialog is made, we can just bring this instance up:
	if (m_hMapping)
	{
		void * pSharedMem = ::MapViewOfFile(m_hMapping, FILE_MAP_ALL_ACCESS, 0, 0, 0);
		if (pSharedMem)
		{
			HWND * phwnd = (HWND *)pSharedMem;
			*phwnd = m_hwnd;
			::UnmapViewOfFile(pSharedMem);
		}
	}
	if (m_hEventWnd)
		::SetEvent(m_hEventWnd);

	// Set up the tabs.
	Assert(kcdlgv == 2); // Ensure that the number of dialogs is what we expect.
	Assert(m_hwnd);
	m_hwndTab = ::GetDlgItem(m_hwnd, kctidBackupTabs);

	AfDialogViewPtr qdlgv;
	qdlgv.Attach(NewObj BackupBkpDlg(this));
	m_vdlgv.Push(qdlgv);
	qdlgv.Attach(NewObj BackupRstDlg(this));
	m_vdlgv.Push(qdlgv);

	// Insert the title of each tab.
	TCITEM tci;
	tci.mask = TCIF_TEXT;
	StrApp str;
	str.Load(kridBackupTab);
	tci.pszText = const_cast<achar *>(str.Chars());
	TabCtrl_InsertItem(m_hwndTab, 0, &tci);
	str.Load(kridRestoreTab);
	tci.pszText = const_cast<achar *>(str.Chars());
	TabCtrl_InsertItem(m_hwndTab, 1, &tci);

	// This section must be after at least one tab gets added to the tab control.
	RECT rcTab;
	GetWindowRect(m_hwndTab, &rcTab);
	TabCtrl_AdjustRect(m_hwndTab, false, &rcTab);
	POINT pt = { rcTab.left, rcTab.top };
	ScreenToClient(m_hwnd, &pt);
	m_dxsClient = pt.x;
	m_dysClient = pt.y;

	// Show the initial default tab.
	ShowChildDlg(m_itabInitial);
	// This next line is needed because if m_itabInitial is not zero, the main section of the
	// tab would show correctly, but the actual tab at the top would show the first tab to be
	// selected.
	TabCtrl_SetCurSel(m_hwndTab, m_itabInitial);

	// Deactivate the inactive Start button. This prevents confusion over keyboard shortcuts:
	DeactivateStartButton((m_itabCurrent == 0) ? kctidBackupStartRestore :
		kctidBackupStartBackup);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Handle notifications.
----------------------------------------------------------------------------------------------*/
bool BackupDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);
	Assert(m_hwnd);

	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctid)
		{
		case kctidBackupStartBackup:
			// Request backup:
			Assert(m_itabCurrent == 0); // Check that tab and start button are in sync

			// Set the last used directory to the selected directory. Need to do this before
			// we check if it is a valid path :)
			m_pbkph->m_bkpi.m_strbDirectoryPath = m_pbkph->m_bkpi.m_strbSelectedDirectory;

			// Check that the backup destination path exists:
			switch (m_pbkph->ValidateBackupPath(true, m_hwnd))
			{
			case BackupHandler::kDirectoryAlreadyExisted:
				break;
			case BackupHandler::kUserQuit:
				{
					// Set focus to destination control:
					::SetFocus(::GetDlgItem(m_vdlgv[m_itabCurrent]->Hwnd(),
						kctidBackupDestination));
				}
				return true;
			case BackupHandler::kCreationSucceeded:
				break;
			case BackupHandler::kCreationFailed:
				// Do not allow dialog to end:
				return true;
			}
			::EndDialog(m_hwnd, kBackupRequest);
			break;
		case kctidBackupStartRestore:
			// Request restore:
			Assert(m_itabCurrent == 1); // Check that tab and start button are in sync
			// Set the last used directory to the selected directory
			m_pbkph->m_bkpi.m_strbDirectoryPath = m_pbkph->m_bkpi.m_strbSelectedDirectory;
			::EndDialog(m_hwnd, kRestoreRequest);
			break;
		case kctidBackupClose:
			::EndDialog(m_hwnd, kClosed);
			return true;
		}
		break;
	case TCN_SELCHANGE:
		{ // Block
			int itab = TabCtrl_GetCurSel(m_hwndTab);

			// Display correct start button:
			SwapStartButtons((itab == 0) ? kctidBackupStartBackup : kctidBackupStartRestore);

			// Make sure we can move to the current tab.
			Assert((uint)itab < (uint)kcdlgv);
			if (!ShowChildDlg(itab))
			{
				// Move back to the old tab:
				TabCtrl_SetCurSel(m_hwndTab, m_itabCurrent);
				// Restore previous start button:
				SwapStartButtons(
					(m_itabCurrent == 0) ? kctidBackupStartBackup : kctidBackupStartRestore);
			}
		} // End block
		return true;
	default:
		return false;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Update Dialog changes. In particular, update the selected tab.
----------------------------------------------------------------------------------------------*/
bool BackupDlg::ShowChildDlg(int itab)
{
	Assert((uint)itab < (uint)kcdlgv);
	AssertPtr(m_vdlgv[itab]);
	Assert(m_hwnd);

	if (m_itabCurrent == itab)
	{
		// We already have the tab selected, so we can return without doing anything.
		return true;
	}

	if (!m_vdlgv[itab]->Hwnd())
	{
		HWND hwnd = ::GetFocus();

		// This is the first time this tab has been selected, and the dialog has not
		// been created yet, so create it now.
		m_vdlgv[itab]->DoModeless(m_hwnd);

		// This is needed so the new dialog has the correct z-order in the parent dialog.
		::SetWindowPos(m_vdlgv[itab]->Hwnd(), NULL, m_dxsClient, m_dysClient, 0, 0,
			SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

		// If this is the first time we are creating the internal dialog and the focus was
		// on the tab control, Windows moves the focus to the dialog, so set it back.
		if (hwnd == m_hwndTab)
			::SetFocus(m_hwndTab);
		else if (itab == 1)
		{
			// We want the Start Restore button to initially have focus for the restore tab:
			HWND hwndButton = ::GetDlgItem(m_hwnd, kctidBackupStartRestore);
			::SetFocus(hwndButton);
		}
	}

	// This needs to come after creating the tab, in case we switch successfully,
	// but before we set the active tab, in case we fail.
	int itabCurrent = m_itabCurrent; // Keep record of current tab.
	m_itabCurrent = itab; // Update current tab, in anticipation of change.
	if (!m_vdlgv[itab]->SetActive())
	{
		static bool s_fRecursive = false;
		if (s_fRecursive)
		{
			// This is to keep us from getting into an infinite loop if the user tries to
			// select a new tab and can't, but it fails when we try to set the selection back
			// to the previously selected tab.
			// ENHANCE AlistairI: Can we do something better here? (Copied from styles dialog)
			Assert(false);
			return false;
		}
		s_fRecursive = true;
		TabCtrl_SetCurSel(m_hwndTab, itabCurrent);
		ShowChildDlg(itabCurrent);
		s_fRecursive = false;
		return true;
	}

	// Show the new dialog view and hide the old one.
	::ShowWindow(m_vdlgv[itab]->Hwnd(), SW_SHOW);

	if (itabCurrent != -1)
		::ShowWindow(m_vdlgv[itabCurrent]->Hwnd(), SW_HIDE);

	// Adjust the help reference:
	if (itab == 0)
	{
		m_pszHelpUrl = _T("User_Interface/Menus/File/Backup_and_Restore/")
			_T("Backup_and_Restore_Backup_tab.htm");
	}
	else if (itab == 1)
	{
		m_pszHelpUrl = _T("User_Interface/Menus/File/Backup_and_Restore/")
			_T("Backup_and_Restore_Restore_tab.htm");
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Stores the text from the specified button into the text cache, and deletes the text from the
	button. It also disables and hides the button. This is so that we can have a start button
	for backup that is different from the start button for restore, but both can have the same
	keyboard accelerator. (We need separate buttons to enable "what's this?" helps to work.)
	@param rid The resource id of the button to deactivate.
----------------------------------------------------------------------------------------------*/
void BackupDlg::DeactivateStartButton(int rid)
{
	Assert(rid == kctidBackupStartBackup || rid == kctidBackupStartRestore);

	Vector<achar> vch;
	int nLen = ::SendDlgItemMessage(m_hwnd, rid, WM_GETTEXTLENGTH, 0, 0);
	vch.Resize(nLen + 1);
	::SendDlgItemMessage(m_hwnd, rid, WM_GETTEXT, nLen+1, (LPARAM)vch.Begin());
	m_strButtonTextCache.Assign(vch.Begin());
	::SendDlgItemMessage(m_hwnd, rid, WM_SETTEXT, 0, (LPARAM)_T(""));

	// Hide and disable the button:
	HWND hwndButton = ::GetDlgItem(m_hwnd, rid);
	::ShowWindow(hwndButton, SW_HIDE);
	::EnableWindow(hwndButton, false);
}


/*----------------------------------------------------------------------------------------------
	Enables the hidden start button to be shown by restoring its text from the cache, and moving
	the text of the other start button to the cache.
	@param ridActivate The resource id of the start button to be activated.
----------------------------------------------------------------------------------------------*/
void BackupDlg::SwapStartButtons(int ridActivate)
{
	Assert(ridActivate == kctidBackupStartBackup || ridActivate == kctidBackupStartRestore);

	// Determine the button to be deactivated:
	int ridDeactivate = (ridActivate == kctidBackupStartBackup ? kctidBackupStartRestore :
		kctidBackupStartBackup);

	// Restore the text of the button to be activated:
	::SendDlgItemMessage(m_hwnd, ridActivate, WM_SETTEXT, 0,
		(LPARAM)m_strButtonTextCache.Chars());

	// Remove text from the button to be deactivated:
	DeactivateStartButton(ridDeactivate);

	// Activate the other button:
	HWND hwndButton = ::GetDlgItem(m_hwnd, ridActivate);
	::ShowWindow(hwndButton, SW_SHOW);
	::EnableWindow(hwndButton, true);
	::SetFocus(hwndButton);
}

/*----------------------------------------------------------------------------------------------
	Specify help topic for this dialog.
----------------------------------------------------------------------------------------------*/
SmartBstr BackupDlg::GetHelpTopic()
{
	if (m_itabCurrent == 0)
		return _T("khtpBackupRestore_BackupTab");
	else
		return _T("khtpBackupRestore_RestoreTab");
}


//:>********************************************************************************************
//:>	ComboboxDrives implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
ComboboxDrives::ComboboxDrives()
{
	m_hwndDialog = NULL;
	m_ctidCombobox = 0;
}

/*----------------------------------------------------------------------------------------------
	Initialization
	@param hwndDialog The m_hwnd member of an existing (and 'created') AfDialog object.
	@param ctidCombobox The resource id of an existing "ComboBoxEx32" combobox.
	@param strbSelectedDeviceName The drive to initially highlight.
----------------------------------------------------------------------------------------------*/
void ComboboxDrives::Init(HWND hwndDialog, int ctidCombobox, StrAppBuf strbSelectedDeviceName)
{
	Assert(hwndDialog);
	Assert(ctidCombobox);
	m_hwndDialog = hwndDialog;
	m_ctidCombobox = ctidCombobox;
	m_strbSelectedDeviceName = strbSelectedDeviceName;
}

/*----------------------------------------------------------------------------------------------
	Fill the referenced combobox with descriptions of available drives. The drive that is
	initially selected will be the one that was selected last time, or if this is the first
	time, the first item in the list.
----------------------------------------------------------------------------------------------*/
void ComboboxDrives::FillList()
{
	Assert(m_hwndDialog);
	Assert(m_ctidCombobox);

	// Clear out old list:
	::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CB_RESETCONTENT, 0, 0);

	// Get a list of available drives for backup:
	const int kcchInitialListSize = 260; // This should be big enough, but we can change later.
	Vector<achar> vchDriveList;
	vchDriveList.Resize(kcchInitialListSize);
	int cchListLen = ::GetLogicalDriveStrings(kcchInitialListSize, vchDriveList.Begin());
	// Check if we had enough room for the list of drives:
	if (cchListLen > kcchInitialListSize)
	{
		// We needed more room:
		vchDriveList.Resize(cchListLen);
		cchListLen = ::GetLogicalDriveStrings(cchListLen, vchDriveList.Begin());
		Assert(vchDriveList.Size() == cchListLen);
	}
	if (cchListLen != 0) // If zero, then a failure occurred.
	{
		int iHighlight = AddValidDrivesToComboList(vchDriveList.Begin());

		// Select the drive that was used last time or, failing that, the one with the highest
		// capacity:
		if (iHighlight == -1 && m_vstrbAcceptableDrives.Size() > 0)
		{
			iHighlight = 0;
			Assert(iHighlight >= 0 && iHighlight < m_vstrbAcceptableDrives.Size());
			m_strbSelectedDeviceName.Assign(m_vstrbAcceptableDrives[iHighlight]);
		}
		::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CB_SETCURSEL, iHighlight, 0);
	}
}

/*----------------------------------------------------------------------------------------------
	Add the drives to the combobox, noting if one of them should be selected.  This fills in
	m_vstrbAcceptableDrives as a side-effect.

	@param pchList - NUL-separated and extra NUL-terminated string of drives.
	@return - index of selected drive, or -1 if no selection is known yet
----------------------------------------------------------------------------------------------*/
int ComboboxDrives::AddValidDrivesToComboList(achar * pchList)
{
	achar * pchCurrent = pchList;
	int iHighlight = -1;
	SHFILEINFO sfiFileInfo;

	// This call to SHGetFileInfo is really only to get the system image list:
	long himlImageList = (long)SHGetFileInfo(pchCurrent, 0, &sfiFileInfo,
		isizeof(sfiFileInfo), SHGFI_SYSICONINDEX | SHGFI_SMALLICON);
	::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CBEM_SETIMAGELIST, 0, himlImageList);

	// Scan through all drives in our list, adding their details to our combo box.
	// Note that we're assuming all drives' icons come from the same image list.
	// I (Alistair) think this is reasonable, but can't prove it.
	int nIndex = 0;
	m_vstrbAcceptableDrives.Clear();
	while (*pchCurrent)
	{
		// Filter out certain types of drives e.g. RAM disks:
		UINT iType = GetDriveType(pchCurrent);
		// Just comment out types that are *unacceptable*:
		if (iType == DRIVE_UNKNOWN ||
			//iType == DRIVE_NO_ROOT_DIR ||
			iType == DRIVE_REMOVABLE ||
			iType == DRIVE_FIXED ||
			iType == DRIVE_REMOTE ||
			iType == DRIVE_CDROM ||
			//iType == DRIVE_RAMDISK ||
			0)
		{
			m_vstrbAcceptableDrives.Push(StrAppBuf(pchCurrent));

			SHGetFileInfo(pchCurrent, 0, &sfiFileInfo, isizeof(sfiFileInfo),
				SHGFI_SYSICONINDEX | SHGFI_SMALLICON | SHGFI_DISPLAYNAME);
			COMBOBOXEXITEM CBItem;
			CBItem.mask = CBEIF_TEXT | CBEIF_IMAGE | CBEIF_SELECTEDIMAGE;
			CBItem.iItem = -1;
			CBItem.pszText = sfiFileInfo.szDisplayName;
			CBItem.iImage = sfiFileInfo.iIcon;
			CBItem.iSelectedImage = sfiFileInfo.iIcon;
			::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CBEM_INSERTITEM, 0,
				(long)&CBItem);

			// If the current drive corresponds to the device in the user's previous
			// settings, the current combobox entry must be selected.
			if (m_strbSelectedDeviceName.EqualsCI(pchCurrent))
				iHighlight = nIndex;
		} // End if current drive was suitable for backups.
		// Skip to end of current drive string:
		pchCurrent += _tcslen(pchCurrent);
		// Skip past null terminator:
		pchCurrent++;
		nIndex++;
	}
	return iHighlight;
}

/*----------------------------------------------------------------------------------------------
	Assign selected drive to given string
	@param strb [out] String to receive selected drive letter.
	@return true if there was a selection, false otherwise.
----------------------------------------------------------------------------------------------*/
bool ComboboxDrives::GetCurrentSelection(StrAppBuf & strb)
{
	Assert(m_hwndDialog);
	Assert(m_ctidCombobox);

	int i = ::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CB_GETCURSEL, 0, 0);

	if (i == CB_ERR)
		return false;

	Assert(i >= 0 && i < m_vstrbAcceptableDrives.Size());
	strb.Assign(m_vstrbAcceptableDrives[i]);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Allow user to select a folder within the currently selected device.
	@param strb [in, out] The (optional) starting path, and eventually the user's selected path.
	@return True if user selects a valid path.
----------------------------------------------------------------------------------------------*/
bool ComboboxDrives::BrowseCurrentDeviceFolders(StrAppBuf & strb)
{
	StrAppBuf strbDummy;
	return BrowseCurrentDeviceFolders(strb, strbDummy);
}


/*----------------------------------------------------------------------------------------------
	Allow user to select a folder and file within the currently selected device.
	@param strbPath [in, out] The starting path, and eventually the user's selected path.
	@param strbFile [out] The actual file that the user selected, if any.
	@return True if user selects a valid path.
----------------------------------------------------------------------------------------------*/
bool ComboboxDrives::BrowseCurrentDeviceFolders(StrAppBuf & strbPath, StrAppBuf & strbFile)
{
	StrAppBuf strb;

	// Get the current folder:
	strb = strbPath;
	DWORD nFlags = GetFileAttributes(strb.Chars());
	if (nFlags == -1 || !(nFlags & FILE_ATTRIBUTE_DIRECTORY))
	{
		// Current folder not valid, so use combobox selection:
		GetCurrentSelection(strb);
	}

	// Set up a filter likely to show up only FW backup files:
	const achar * pszFilter = _T("Backup files\0*????????????????.zip\0\0");

	// Get user to select a folder:
	if (FolderSelectDlg::ChooseFolder(m_hwndDialog, strb, strbFile, kstidBkpBrowseInfo, pszFilter))
	{
		strbPath = strb;
		WriteSelectionString(strbPath);
		return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Artificially display a 'current selection'
----------------------------------------------------------------------------------------------*/
void ComboboxDrives::WriteSelectionString(StrAppBuf strb)
{
	::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, WM_SETTEXT, 0, (LPARAM)strb.Chars());
}

/*----------------------------------------------------------------------------------------------
	Test if a string represents a valid drive (e.g. "A:")
	@param strb [in] The candidate string. May be a full pathname.
	@param strbValidDrive [out] The 'official' string representing the drive.
	@return True if strb was (or started with) a valid drive.
----------------------------------------------------------------------------------------------*/
bool ComboboxDrives::IsValidDrive(StrAppBuf strb, StrAppBuf & strbValidDrive)
{
	int i;

	if (strb.Length() == 0)
		return false;

	// Our vector of acceptable drives includes a backslash for each drive, so make sure we have
	// one in our candidate string:
	if (-1 == strb.FindCh('\\'))
		strb.Append("\\");

	for (i = 0; i < m_vstrbAcceptableDrives.Size(); i++)
	{
		// Make a string only as long as the valid drive string:
		StrAppBuf strbTemp;
		if (m_vstrbAcceptableDrives[i].Length() < strb.Length())
			strbTemp = strb.Left(m_vstrbAcceptableDrives[i].Length());
		else
			strbTemp = strb;
		if (strbTemp.EqualsCI(m_vstrbAcceptableDrives[i]))
		{
			strbValidDrive = m_vstrbAcceptableDrives[i];
			return true;
		}
	}
	return false;
}

//:>********************************************************************************************
//:>	ComboboxFolders implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
ComboboxFolders::ComboboxFolders()
{
	m_hwndDialog = NULL;
	m_ctidCombobox = 0;

}

/*----------------------------------------------------------------------------------------------
	Initialization
	@param hwndDialog The m_hwnd member of an existing (and 'created') AfDialog object.
	@param ctidCombobox The resource id of an existing "ComboBoxEx32" combobox.
	@param strbSelectedFolderName The folder to initially highlight.
----------------------------------------------------------------------------------------------*/
void ComboboxFolders::Init(HWND hwndDialog, int ctidCombobox, StrAppBuf strbSelectedFolderName)
{
	Assert(hwndDialog);
	Assert(ctidCombobox);
	m_hwndDialog = hwndDialog;
	m_ctidCombobox = ctidCombobox;
	m_strbSelectedFolderName = strbSelectedFolderName;
}

/*----------------------------------------------------------------------------------------------
	Fill the referenced combobox with the last used backup/restore folder (and the default folder,
	if it's different).
----------------------------------------------------------------------------------------------*/
void ComboboxFolders::FillList()
{
	Assert(m_hwndDialog);
	Assert(m_ctidCombobox);

	// Clear out old list:
	::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CB_RESETCONTENT, 0, 0);

	// Add last used backup/restore folder used
	m_vstrbFolders.Push(StrAppBuf(m_pbkpi->m_strbDirectoryPath));
	COMBOBOXEXITEM CBItem;
	CBItem.mask = CBEIF_TEXT;
	CBItem.iItem = -1;
	CBItem.pszText = const_cast<achar *>(m_pbkpi->m_strbDirectoryPath.Chars()); // Assumes Unicode compile
	LRESULT result;
	result = ::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CBEM_INSERTITEM, 0,
		(long)&CBItem);
	Assert(result != -1);

	// Add default backup/restore folder if different from last used
	if (m_pbkpi->m_strbDirectoryPath != m_pbkpi->m_strbDefaultDirectoryPath)
	{
		m_vstrbFolders.Push(StrAppBuf(m_pbkpi->m_strbDefaultDirectoryPath));
		CBItem.mask = CBEIF_TEXT;
		CBItem.iItem = -1;
		CBItem.pszText = const_cast<achar *>(m_pbkpi->m_strbDefaultDirectoryPath.Chars());
		result = ::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CBEM_INSERTITEM, 0,
			(long)&CBItem);
		Assert(result != -1);
	}

	// Highlight the folder selected
	result = ::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CB_SETCURSEL,
		m_pbkpi->m_iCbSelection, 0);
	Assert(result != CB_ERR);
}

/*----------------------------------------------------------------------------------------------
	Assign selected folder to given string
	@param strb [out] String to receive selected drive letter.
	@return the index of the selection which will be -1 if unable to get the selection from
	the combobox.
----------------------------------------------------------------------------------------------*/
int ComboboxFolders::GetCurrentSelection(StrAppBuf & strb)
{
	Assert(m_hwndDialog);
	Assert(m_ctidCombobox);

	int i = ::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CB_GETCURSEL, 0, 0);

	if (i == CB_ERR)
		return i;

	strb.Assign(m_vstrbFolders[i]);

	return i;
}

/*----------------------------------------------------------------------------------------------
	Allow user to select a folder within the currently selected device.
	@param strb [in, out] The (optional) starting path, and eventually the user's selected path.
	@return True if user selects a valid path.
----------------------------------------------------------------------------------------------*/
bool ComboboxFolders::BrowseFolders(StrAppBuf & strb, int stidTitle)
{
	StrAppBuf strbDummy;
	return BrowseFolders(strb, strbDummy, stidTitle);
}

/*----------------------------------------------------------------------------------------------
	Allow user to select a folder and file within the currently selected device.
	@param strbPath [in, out] The starting path, and eventually the user's selected path.
	@param strbFile [out] The actual file that the user selected, if any.
	@return True if user selects a valid path.
----------------------------------------------------------------------------------------------*/
bool ComboboxFolders::BrowseFolders(StrAppBuf & strbPath, StrAppBuf & strbFile, int stidTitle)
{
	StrAppBuf strb;

	// Get the current folder:
	strb = strbPath;
	DWORD nFlags = GetFileAttributes(strb.Chars());
	if (nFlags == -1 || !(nFlags & FILE_ATTRIBUTE_DIRECTORY))
	{
		// Current folder not valid, so use combobox selection:
		GetCurrentSelection(strb);
	}

	// Set up a filter likely to show up only FW backup files:
	const achar * pszFilter = _T("Backup files\0*????????????????.zip\0\0");

	// Get user to select a folder:
	if (FolderSelectDlg::ChooseFolder(m_hwndDialog, strb, strbFile, stidTitle, pszFilter))
	{
		strbPath = strb;
		WriteSelectionString(strbPath);
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Artificially display a 'current selection'
----------------------------------------------------------------------------------------------*/
void ComboboxFolders::WriteSelectionString(StrAppBuf strb)
{
	::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, WM_SETTEXT, 0, (LPARAM)strb.Chars());
	::SendDlgItemMessage(m_hwndDialog, m_ctidCombobox, CB_SELECTSTRING, 0, (LPARAM)strb.Chars());
}

//:>********************************************************************************************
//:>	AvailableProjects implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AvailableProjects::AvailableProjects()
{
	m_hwndDialog = NULL;
	m_ctidProjects = 0;
	m_ctidVersions = 0;
	m_nCurrentProject = 0;
	m_nCurrentVersion = -1; // Indicates 'first in sorted list'
	m_fIgnoreRetriesToLoad = false;
}

/*----------------------------------------------------------------------------------------------
	Initialization.
	@param hwndDialog The m_hwnd member of an existing (and 'created') AfDialog object.
	@param ctidProjects The resource id of an existing "ComboBoxEx32" combobox.
	@param ctidVersions The resource id of an existing "SysListView32" listbox.
----------------------------------------------------------------------------------------------*/
void AvailableProjects::Init(HWND hwndDialog, int ctidProjects, int ctidVersions)
{
	Assert(hwndDialog);

	m_hwndDialog = hwndDialog;
	m_ctidProjects = ctidProjects;
	m_ctidVersions = ctidVersions;
}


/*----------------------------------------------------------------------------------------------
	Read available projects from given path, and display the project names in the combobox, and
	the project versions in the listbox.
	@param strbpPath Path to search for available projects
	@return true if there was at least one valid project file in the given directory.
----------------------------------------------------------------------------------------------*/
bool AvailableProjects::DisplayAvailableProjects(StrAppBufPath strbpPath)
{
	StrAppBufPath strbpDummy;
	strbpDummy.Assign(_T(""));
	return DisplayAvailableProjects(strbpPath, strbpDummy);
}


/*----------------------------------------------------------------------------------------------
	Read available projects from given path, and display the project names in the combobox, and
	the project versions in the listbox. Highlight the specified entry.
	@param strbpPath - Path to search for available projects
	@param strbpHighlight - File (full path) representing the project to highlight initially
	@return true if there was at least one valid project file in the given directory.
----------------------------------------------------------------------------------------------*/
bool AvailableProjects::DisplayAvailableProjects(StrAppBufPath strbpPath,
	StrAppBufPath strbpHighlight)
{
	if (m_fIgnoreRetriesToLoad)
		return true;	// prevent accidental recursion.
	m_fIgnoreRetriesToLoad = true;

	// Fill in m_vprj with the relevant information.
	CollectProjectBackupFiles(strbpPath, strbpHighlight);

	// Now feed our project data into the combobox:
	Assert(m_hwndDialog);
	::SendDlgItemMessage(m_hwndDialog, m_ctidProjects, CB_RESETCONTENT, 0, 0);

	if (m_vprj.Size() > 0)
	{
		int i;
		LRESULT lresult;
		// Fill in the combobox list from the projects that we found.
		for (i = 0; i < m_vprj.Size(); i++)
		{
			Project prj = m_vprj[i];
			COMBOBOXEXITEM CBItem;
			CBItem.mask = CBEIF_TEXT | CBEIF_IMAGE | CBEIF_SELECTEDIMAGE | CBEIF_LPARAM;
			CBItem.iItem = -1;
			CBItem.iImage = kridOpenProjFileDrawerClosed;
			CBItem.iSelectedImage = kridOpenProjFileDrawerClosed;
			CBItem.pszText = const_cast<achar *>(prj.m_strbDatabaseName.Chars());
			CBItem.lParam = i;
			lresult = ::SendDlgItemMessage(m_hwndDialog, m_ctidProjects, CBEM_INSERTITEM, 0,
				(LPARAM)&CBItem);
			Assert(lresult != -1);
		}
		// The control may have been sorted, but we need to select the item with ID ==
		// m_nCurrentProject:
		for (i = 0; i < m_vprj.Size(); i++)
		{
			COMBOBOXEXITEM CBItem;
			CBItem.mask = CBEIF_LPARAM;
			CBItem.iItem = i;

			lresult = ::SendDlgItemMessage(m_hwndDialog, m_ctidProjects, CBEM_GETITEM,
				0, (LPARAM)&CBItem);
			Assert(lresult != 0);
			// If we find the match, select it in the combobox.
			if (CBItem.lParam == m_nCurrentProject)
			{
				lresult = ::SendDlgItemMessage(m_hwndDialog, m_ctidProjects, CB_SETCURSEL,
					(WPARAM)i, 0);
				Assert(lresult != CB_ERR);
				break;
			}
		}
	}
	bool fResult = true; // Assume we found a valid file
	// Check if the combobox list is still empty.
	if (::SendDlgItemMessage(m_hwndDialog, m_ctidProjects, CB_GETCOUNT, 0, 0) == 0)
	{
		// We didn't find anything, so put in an 'option' saying no files available:
		fResult = false;
		COMBOBOXEXITEM CBItem;
		CBItem.mask = CBEIF_TEXT | CBEIF_LPARAM;
		CBItem.iItem = -1;
		StrApp str(kstidRstNoFiles);
		CBItem.pszText = const_cast<achar *>(str.Chars());
		CBItem.lParam = -1;
		::SendDlgItemMessage(m_hwndDialog, m_ctidProjects, CBEM_INSERTITEM, 0, (long)&CBItem);
		::SendDlgItemMessage(m_hwndDialog, m_ctidProjects, CB_SETCURSEL, 0, 0);
		HWND hwndList = ::GetDlgItem(m_hwndDialog, m_ctidVersions);
		ListView_DeleteAllItems(hwndList);
	}
	else
		UpdateVersionsDisplay();

	m_fIgnoreRetriesToLoad = false;
	return fResult;
}

/*----------------------------------------------------------------------------------------------
	Fill m_vprj with the relevant project information based on the file pathname mask.

	@param strbpPath - Path to search for available projects
	@param strbpHighlight - File (full path) representing the project to highlight initially
----------------------------------------------------------------------------------------------*/
void AvailableProjects::CollectProjectBackupFiles(StrAppBufPath strbpPath,
	StrAppBufPath strbpHighlight)
{
	// Reset current data:
	m_vprj.Clear();
	m_nCurrentProject = 0;
	m_nCurrentVersion = -1; // Indicates 'first in sorted list'

	// Search for all zip files at given path:
	if (strbpPath.Length() == 0 || strbpPath[strbpPath.Length() - 1] != '\\')
		strbpPath.Append("\\");
	StrAppBufPath strbpMask = strbpPath;
	strbpMask.Append("*.zip");

	WIN32_FIND_DATA wfd;
	HANDLE hFind;

	hFind = ::FindFirstFile(strbpMask.Chars(), &wfd);
	if (hFind != INVALID_HANDLE_VALUE)
	{
		do
		{
			// See if format of file name matches our project-date-time format:
			StrAppBufPath strbpName(wfd.cFileName);
			if (BackupFileNameProcessor::IsValidFileName(strbpName))
			{
				StrAppBuf strbProject;
				StrAppBuf strbDatabase;
				StrApp str;
				BackupFileNameProcessor::GetProjectFullName(strbpName, str);
				strbProject.Assign(str.Chars());
				BackupFileNameProcessor::GetDatabaseName(strbpName, str);
				strbDatabase.Assign(str.Chars());

				Version ver;
				BackupFileNameProcessor::GetVersionName(strbpName, str);
				ver.m_strbDisplayName.Assign(str.Chars());
				ver.m_strbFileName = strbpName;

				// See if we've seen this project already:
				bool fFound = false;
				for (int i = 0; i < m_vprj.Size() && !fFound; i++)
				{
					if (m_vprj[i].m_strbDatabaseName.Equals(strbDatabase))
					{
						fFound = true;
						// If this is the project we need to highlight, note its index:
						if (strbpHighlight.EqualsCI(strbpName))
						{
							m_nCurrentProject = i;
							m_nCurrentVersion = m_vprj[i].m_vver.Size();
						}
						m_vprj[i].m_vver.Push(ver);
					}
				}
				if (!fFound)
				{
					// If this is the project we need to highlight, note its index:
					if (strbpHighlight.EqualsCI(strbpName))
					{
						m_nCurrentProject = m_vprj.Size();
						m_nCurrentVersion = 0;
					}

					// We have to start a new project record:
					Project prj;
					prj.m_strbDatabaseName = strbDatabase;
					prj.m_strbpDirectoryPath = strbpPath;
					prj.m_vver.Push(ver);
					m_vprj.Push(prj);
				}
			} // End if current file name is a valid backup file
		} while (::FindNextFile(hFind, &wfd));
		::FindClose(hFind);
	}
	hFind = NULL;
}


/*----------------------------------------------------------------------------------------------
	Refreshes the versions listbox with versions of the project currently selected in the
	combobox.
----------------------------------------------------------------------------------------------*/
void AvailableProjects::UpdateVersionsDisplay()
{
	Assert(m_hwndDialog);
	HWND hwndList = ::GetDlgItem(m_hwndDialog, m_ctidVersions);
	ListView_DeleteAllItems(hwndList);

	Assert(m_nCurrentProject >= 0 && m_nCurrentProject < m_vprj.Size());
	Project prj = m_vprj[m_nCurrentProject];

	// Fill in listbox with all versions of selected project:
	LVITEM lvi = { LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM };
	int i;
	for (i = 0; i < prj.m_vver.Size(); i++)
	{
		lvi.iItem = i;
		lvi.iSubItem = 0;
		lvi.iImage = kridOpenProjFileDrawerClosed;
		Version ver = prj.m_vver[i];
		lvi.pszText = const_cast<achar *>(ver.m_strbDisplayName.Chars());
		// Because the listview sorts the projects, we have to keep a note of the index of each
		// project in our vector:
		lvi.lParam = i;
		ListView_InsertItem(hwndList, &lvi);
	}

	// See if requested selection is first in (sorted) list:
	if (m_nCurrentVersion == -1)
	{
		// Select first item in list:
		ListView_SetItemState(hwndList, 0, LVIS_SELECTED, LVIS_SELECTED);
		// See which item that corresponds to in our internal list:
		lvi.mask = LVIF_PARAM;
		lvi.iItem = 0;
		bool f; // Need this for Release build.
		f = ListView_GetItem(hwndList, &lvi);
		Assert(f);
		m_nCurrentVersion = lvi.lParam;
	}
	else
	{
		// Select item indexed as m_nCurrentVersion in our internal list. The index in the
		// control may not be the same, as the control sorts the entries:
		for (i = 0; i < prj.m_vver.Size(); i++)
		{
			lvi.mask = LVIF_PARAM;
			lvi.iItem = i;
			bool f; // Need this for Release build.
			f = ListView_GetItem(hwndList, &lvi);
			Assert(f);
			if (m_nCurrentVersion == lvi.lParam)
			{
				// Select item:
				ListView_SetItemState(hwndList, i, LVIS_SELECTED, LVIS_SELECTED);
				ListView_EnsureVisible(hwndList, i, false);
				break;
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Lets the object know which project the user has just selected
	@param nIndex Index into internal project list (found in lParam member of COMBOBOXEXITEM).
----------------------------------------------------------------------------------------------*/
void AvailableProjects::ProjectSelected(int nIndex)
{
	Assert(nIndex >= 0 && nIndex < m_vprj.Size());
	m_nCurrentProject = nIndex;
	m_nCurrentVersion = -1; // Indicates 'first in sorted list'
}

/*----------------------------------------------------------------------------------------------
	Lets the object know which version of the current project the user has just selected
	@param nIndex Index into internal version list (found in lParam member of LVITEM).
----------------------------------------------------------------------------------------------*/
void AvailableProjects::VersionSelected(int nIndex)
{
	Assert(m_nCurrentProject >= 0 && m_nCurrentProject < m_vprj.Size());
	Project prj = m_vprj[m_nCurrentProject];
	Assert(nIndex >= 0 && nIndex < prj.m_vver.Size());

	m_nCurrentVersion = nIndex;
}

/*----------------------------------------------------------------------------------------------
	Returns the directory path where the currently selected project is.
----------------------------------------------------------------------------------------------*/
StrAppBufPath AvailableProjects::GetCurrentDirectoryPath()
{
	Assert(m_hwndDialog);

	StrAppBufPath strbpResult;
	// See if there are any projects at all:
	if (m_vprj.Size() == 0)
		return strbpResult;

	Assert(m_nCurrentProject >= 0 && m_nCurrentProject < m_vprj.Size());
	Project prj = m_vprj[m_nCurrentProject];
	strbpResult = prj.m_strbpDirectoryPath;

	return strbpResult;
}

/*----------------------------------------------------------------------------------------------
	Returns the file name of the file which comes from the currently selected project AND
	the currently selected version.
----------------------------------------------------------------------------------------------*/
StrAppBuf AvailableProjects::GetCurrentFileName()
{
	StrAppBuf strbResult;
	// See if there are any projects at all:
	if (m_vprj.Size() == 0)
		return strbResult;

	Assert(m_nCurrentProject >= 0 && m_nCurrentProject < m_vprj.Size());
	Project prj = m_vprj[m_nCurrentProject];
	Assert(m_nCurrentVersion >= 0 && m_nCurrentVersion < prj.m_vver.Size());

	strbResult = prj.m_vver[m_nCurrentVersion].m_strbFileName;

	return strbResult;
}

/*----------------------------------------------------------------------------------------------
	Returns the name of the currently selected project.
----------------------------------------------------------------------------------------------*/
StrAppBuf AvailableProjects::GetCurrentProjectName()
{
	Assert(m_hwndDialog);

	StrAppBuf strbResult;
	// See if there are any projects at all:
	if (m_vprj.Size() == 0)
		return strbResult;

	Assert(m_nCurrentProject >= 0 && m_nCurrentProject < m_vprj.Size());
	Project prj = m_vprj[m_nCurrentProject];
	strbResult = prj.m_strbDatabaseName;

	return strbResult;
}

/*----------------------------------------------------------------------------------------------
	Returns true if a valid project is selected.
----------------------------------------------------------------------------------------------*/
bool AvailableProjects::ValidProjectSelected()
{
	// See if there are any projects at all:
	if (m_vprj.Size() == 0)
		return false;

	if (m_nCurrentProject < 0 || m_nCurrentProject >= m_vprj.Size())
		return false;
	Project prj = m_vprj[m_nCurrentProject];
	if (m_nCurrentVersion < 0 || m_nCurrentVersion >= prj.m_vver.Size())
		return false;

	return true;
}


//:>********************************************************************************************
//:>	BackupBkpDlg implementation.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Default constructor.
----------------------------------------------------------------------------------------------*/
BackupBkpDlg::BackupBkpDlg()
{
	BasicInit();
}


/*----------------------------------------------------------------------------------------------
	Constructor for use by BackupDlg parent.
----------------------------------------------------------------------------------------------*/
BackupBkpDlg::BackupBkpDlg(BackupDlg * pbkpd)
{
	AssertPtr(pbkpd);

	BasicInit();

	AssertPtr(pbkpd->m_pbkph);

	m_pbkpi = &(pbkpd->m_pbkph->m_bkpi);
	m_pfws = &(pbkpd->m_pbkph->m_fws);
	m_phwndDlg = &(pbkpd->m_hwnd);
	m_enableThemeDialogTexture = pbkpd->m_enableThemeDialogTexture;

	GetAvailableFolders()->SetBackInfo(m_pbkpi);
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
BackupBkpDlg::~BackupBkpDlg()
{
}

/*----------------------------------------------------------------------------------------------
	Class accessor for folders comboxbox. If this changes, also change in BackupRstDlg.
----------------------------------------------------------------------------------------------*/
ComboboxFolders* BackupBkpDlg::GetAvailableFolders()
{
	return &m_cbfldrs;
}

/*----------------------------------------------------------------------------------------------
	Changes kctidBackupPasswordWarning label text to indicate whether or not
	a backup password is in force.
----------------------------------------------------------------------------------------------*/
void BackupBkpDlg::SetPasswordWarningLabel()
{
	StrApp str((m_pbkpi->m_bkppwi.m_fLock) ? kstidPasswordInUse : kstidNoPasswordInUse);
	::SendDlgItemMessage(m_hwnd, kctidBackupPasswordWarning, WM_SETTEXT, 0, (LPARAM)str.Chars());
}

/*----------------------------------------------------------------------------------------------
	Check whether we're running under Windows Vista (or later).  This code is shared with
	BackupRstDlg.
----------------------------------------------------------------------------------------------*/
static bool IsVista()
{
	OSVERSIONINFOEX oviex;
	oviex.dwOSVersionInfoSize = sizeof(oviex);
	BOOL fOk = ::GetVersionEx((LPOSVERSIONINFO)&oviex);
	DWORD dw;
	if (fOk)
	{
		return oviex.dwMajorVersion >= 6;
	}
	else
	{
		dw = ::GetLastError();
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Initialization.
----------------------------------------------------------------------------------------------*/
void BackupBkpDlg::BasicInit()
{
	m_rid = kridBackupTab;
	m_pszHelpUrl =
		_T("User_Interface/Menus/File/Backup_and_Restore/Backup_and_Restore_Backup_tab.htm");
	m_pbkpi = NULL;
	m_pfws = NULL;
	m_fNonUserControlSettings = false;
	m_phwndDlg = NULL;
	m_fIsVista = IsVista();
	m_enableThemeDialogTexture = NULL;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog (tab).
----------------------------------------------------------------------------------------------*/
bool BackupBkpDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_fNonUserControlSettings = true;

	if (m_enableThemeDialogTexture != NULL)
		m_enableThemeDialogTexture(m_hwnd, ETDT_ENABLETAB);

	InitializeListView();

	// Add a shield icon to the Schedule button:
	::SendDlgItemMessage(m_hwnd, kctidBackupSchedule, (BCM_FIRST + 0x000C), 0, 0xFFFFFFFF);

	// Set the selected directory to the last one used and its index in the combobox.
	m_pbkpi->m_strbSelectedDirectory = m_pbkpi->m_strbDirectoryPath;
	m_pbkpi->m_iCbSelection = 0;
	// Setup the combobox.
	m_cbfldrs.Init(m_hwnd, kctidBackupDestination, m_pbkpi->m_strbSelectedDirectory);
	m_cbfldrs.FillList();
	if (m_pbkpi->m_strbSelectedDirectory.Length() > 0)
	{
		m_cbfldrs.WriteSelectionString(m_pbkpi->m_strbSelectedDirectory);
	}
	m_cbfldrs.GetCurrentSelection(m_pbkpi->m_strbSelectedDirectory);

	SetPasswordWarningLabel();

	// Set XML check box to unchecked by default.
	m_pbkpi->m_fXml = false;
	::SendDlgItemMessage(m_hwnd, kctidBackupIncludeXml, BM_SETCHECK, BST_UNCHECKED, 0);

	UpdateStartButton();

	m_fNonUserControlSettings = false;

	return SuperClass::OnInitDlg(hwndCtrl, lp);
} // BackupBkpDlg::OnInitDlg.

/*----------------------------------------------------------------------------------------------
	Initialize the embedded ListView.
----------------------------------------------------------------------------------------------*/
void BackupBkpDlg::InitializeListView()
{
	Assert(m_hwnd);
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidBackupProjects);

	// Set up list view to display available projects:
	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	Rect rc;
	::GetClientRect(hwndList, &rc);
	// Set up a column as wide as the list view window minus its scroll bar:
	lvc.cx = rc.Width() - ::GetSystemMetrics(SM_CXVSCROLL);
	ListView_InsertColumn(hwndList, 0, &lvc);

	// Add check boxes:
	ListView_SetExtendedListViewStyleEx(hwndList, LVS_EX_CHECKBOXES, LVS_EX_CHECKBOXES);

	AssertPtr(m_pbkpi);

	// Set backup flags for projects that need them:
	int nDummy = 0;
	m_pbkpi->AutoSelectBackupProjects(nDummy);

	LVITEM lvi = { LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM };

	// Add each project to list view:
	for (int i = 0; i < m_pbkpi->m_vprojd.Size(); i++)
	{
		BackupInfo::ProjectData projd = m_pbkpi->m_vprojd[i];

		lvi.iItem = i;
		lvi.iSubItem = 0;
		lvi.iImage = kridOpenProjFileDrawerClosed;
		StrApp str(projd.m_stuDatabase);
		lvi.pszText = const_cast<achar *>(str.Chars());
		// Because the listview sorts the projects, we have to keep a note of the index of each
		// project in our vector:
		lvi.lParam = i;
		int nInsertionIndex = ListView_InsertItem(hwndList, &lvi);
		ListView_SetCheckState(hwndList, nInsertionIndex, projd.m_fBackup);
	}
	// Select first item in list:
	ListView_SetItemState(hwndList, 0, LVIS_SELECTED, LVIS_SELECTED);
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool BackupBkpDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);

	if (!m_fIsVista && wm == WM_ERASEBKGND)
	{
		// This is needed because of a bug in the list view control that causes
		// it not to be redrawn sometimes.  (not wanted for Vista -- see TE-5435)
		Assert(m_hwnd);
		::RedrawWindow(::GetDlgItem(m_hwnd, kctidBackupProjects), NULL, NULL,
			RDW_ERASE | RDW_FRAME | RDW_INVALIDATE);

		// Re-fill the folder list.
		m_cbfldrs.FillList();
		if ( m_pbkpi->m_strbSelectedDirectory.Length() > 0)
		{
			m_fNonUserControlSettings = true;
			m_cbfldrs.WriteSelectionString(m_pbkpi->m_strbSelectedDirectory);
			m_fNonUserControlSettings = false;
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Handle a notification.
----------------------------------------------------------------------------------------------*/
bool BackupBkpDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);
	Assert(m_hwnd);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		return OnButtonClicked(ctidFrom);

	case LVN_ITEMCHANGED:
		return OnListViewItemChanged(ctidFrom, pnmh);

	case CBN_EDITCHANGE:
		return OnComboBoxEditChange(ctidFrom);

	case CBN_SELCHANGE:
		return OnComboBoxSelChange(ctidFrom);

	default:
		return false;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle a button being clicked.

	@param ctidFrom - id of the sending control
----------------------------------------------------------------------------------------------*/
bool BackupBkpDlg::OnButtonClicked(int ctidFrom)
{
	switch (ctidFrom)
	{
	case kctidBackupReminders:
		ConfigureReminders();
		return true;
	case kctidBackupSchedule:
		ConfigureSchedule();
		return true;
	case kctidBackupPassword:
		ConfigurePassword();
		return true;
	case kctidBackupIncludeXml:
		m_pbkpi->m_fXml = (::SendDlgItemMessage(m_hwnd, kctidBackupIncludeXml, BM_GETCHECK,
			0, 0) == BST_CHECKED);
		return true;
	case kctidBackupBrowseDestination:
		// change last used directory if user made another valid selection
		if (m_cbfldrs.BrowseFolders(m_pbkpi->m_strbSelectedDirectory, kstidBkpBrowseInfo))
			m_pbkpi->m_strbDirectoryPath = m_pbkpi->m_strbSelectedDirectory;
		UpdateStartButton();
		return true;
	default:
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle a list view item being changed (checked or unchecked?).

	@param ctidFrom - id of the sending control
	@param pnmh - pointer to notifier data
----------------------------------------------------------------------------------------------*/
bool BackupBkpDlg::OnListViewItemChanged(int ctidFrom, NMHDR * pnmh)
{
	// Check that this notification came from our list view, and that we weren't in the
	// middle of setting it up:
	if (ctidFrom == kctidBackupProjects && !m_fNonUserControlSettings)
	{
		NMLISTVIEW * pnmlv = ((NMLISTVIEW *) pnmh);
		if (pnmlv->uChanged & LVIF_STATE)
		{
			// Something changed. It could be a check box setting.
			AssertPtr(m_pbkpi);
			HWND hwndList = ::GetDlgItem(m_hwnd, kctidBackupProjects);
			// Make sure our project's backup flag is in agreement with control.
			// Note that the index of the project in our vector is stored in the lParam
			// member, and not the iItem member:
			Assert(pnmlv->lParam >= 0 && pnmlv->lParam < m_pbkpi->m_vprojd.Size());
			m_pbkpi->m_vprojd[pnmlv->lParam].m_fBackup =
				(bool)ListView_GetCheckState(hwndList, pnmlv->iItem);
			UpdateStartButton();
		}
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle an edit inside a combobox.

	@param ctidFrom - id of the sending control
----------------------------------------------------------------------------------------------*/
bool BackupBkpDlg::OnComboBoxEditChange(int ctidFrom)
{
	if (ctidFrom == kctidBackupDestination)
	{
		if (!m_fNonUserControlSettings)
		{
			// Get text that user has typed so far:
			StrAppBuf strb;
			int cch = ::SendDlgItemMessage(m_hwnd, kctidBackupDestination, WM_GETTEXT,
				strb.kcchMaxStr, (LPARAM)strb.Chars());
			strb.SetLength(cch);
			m_pbkpi->m_strbSelectedDirectory = strb;
		}
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle an selection change inside a combobox.

	@param ctidFrom - id of the sending control
----------------------------------------------------------------------------------------------*/
bool BackupBkpDlg::OnComboBoxSelChange(int ctidFrom)
{
	if (ctidFrom == kctidBackupDestination)
	{
		// Get index and string for newly selected item:
		m_pbkpi->m_iCbSelection =
			m_cbfldrs.GetCurrentSelection(m_pbkpi->m_strbSelectedDirectory);
		// reset combobox selection to 0 if unable to get selection
		if (m_pbkpi->m_iCbSelection < 0)
			m_pbkpi->m_iCbSelection = 0;
		UpdateStartButton();
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Check that the owning dialog's start button is enabled/disabled as appropriate
----------------------------------------------------------------------------------------------*/
void BackupBkpDlg::UpdateStartButton()
{
	AssertPtr(m_phwndDlg); // Owning dialog must have a hwnd by now.

	bool fEnable = false; // Assume disabled until certain otherwise.

	// See if at least one project has been selected for backup:
	for (int i = 0; i < m_pbkpi->m_vprojd.Size(); i++)
	{
		if (m_pbkpi->m_vprojd[i].m_fBackup)
		{
			fEnable = true;
			break;
		}
	}

	::EnableWindow(::GetDlgItem(*m_phwndDlg, kctidBackupStartBackup), fEnable);
}


/*----------------------------------------------------------------------------------------------
	Respond to tab becoming active by updating the start button status.
----------------------------------------------------------------------------------------------*/
bool BackupBkpDlg::SetActive()
{
	if (m_hwnd)
		UpdateStartButton();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Configure the reminder system.
----------------------------------------------------------------------------------------------*/
void BackupBkpDlg::ConfigureReminders()
{
	AssertPtr(m_pbkpi);
	Assert(m_hwnd);

	// Store settings temporarily, in case we need to undo changes:
	BackupReminderInfo bkprmiTemp = m_pbkpi->m_bkprmi;

	// Get the user to interact with the reminder dialog:
	bool fRepeat;
	do
	{
		fRepeat = false;
		if (BackupReminderDlg::ReminderDialog(m_hwnd, &m_pbkpi->m_bkprmi))
		{
			// Save reminder settings:
			m_pbkpi->m_bkprmi.WriteToRegistry(m_pfws);
		}
		else
		{
			// User canceled, but this may be after originally pressing OK and quitting
			// schedule password dialog, so restore original settings:
			m_pbkpi->m_bkprmi = bkprmiTemp;
		}
	} while (fRepeat);
}


/*----------------------------------------------------------------------------------------------
	Configure the backup scheduling system.
----------------------------------------------------------------------------------------------*/
void BackupBkpDlg::ConfigureSchedule()
{
	// Form a path to the Backup Scheduler utility:
	StrAppBufPath strbpBackupSchedExe;
	strbpBackupSchedExe.Assign(DirectoryFinder::FwRootCodeDir().Chars());
	if (strbpBackupSchedExe.Length() == 0 ||
		strbpBackupSchedExe[strbpBackupSchedExe.Length() - 1] != '\\')
	{
		strbpBackupSchedExe.Append(_T("\\"));
	}
	strbpBackupSchedExe.Append(_T("BackupScheduler.exe"));

	//  Launch the BackupScheduler.exe application:
	int nRet = (int)ShellExecute(NULL, _T("open"), strbpBackupSchedExe.Chars(), NULL,
		NULL, SW_SHOW);

	if (nRet <= 32) // Scheduler did not launch:
	{
		StrApp strMsg(kstidCantLaunchScheduler);
		StrApp strAppTitle(kstidBkpSystem);
		::MessageBox(NULL, strMsg.Chars(), strAppTitle.Chars(), MB_ICONSTOP | MB_OK);
	}
}


/*----------------------------------------------------------------------------------------------
	Configure the backup password system.
----------------------------------------------------------------------------------------------*/
void BackupBkpDlg::ConfigurePassword()
{
	AssertPtr(m_pbkpi);
	Assert(m_hwnd);

	// Get the user to interact with the password dialog:
	if (BackupPasswordDlg::PasswordDialog(m_hwnd, &m_pbkpi->m_bkppwi))
	{
		// Save password settings:
		m_pbkpi->m_bkppwi.WriteToRegistry(m_pfws);
	}
	SetPasswordWarningLabel();
}


//:>********************************************************************************************
//:>	BackupRstDlg implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Default constructor.
----------------------------------------------------------------------------------------------*/
BackupRstDlg::BackupRstDlg()
{
	BasicInit();
}


/*----------------------------------------------------------------------------------------------
	Constructor for use by BackupDlg parent.
----------------------------------------------------------------------------------------------*/
BackupRstDlg::BackupRstDlg(BackupDlg * pbkpd)
{
	AssertPtr(pbkpd);

	BasicInit();

	AssertPtr(pbkpd->m_pbkph);
	m_pbkpi = &(pbkpd->m_pbkph->m_bkpi);
	m_pszHelpUrl =
		_T("User_Interface/Menus/File/Backup_and_Restore/Backup_and_Restore_Restore_tab.htm");
	m_phwndDlg = &(pbkpd->m_hwnd);
	m_enableThemeDialogTexture = pbkpd->m_enableThemeDialogTexture;

	m_avprj.Init(pbkpd->m_hwnd, kctidRestoreProject, kctidRestoreVersion);

	GetAvailableFolders()->SetBackInfo(m_pbkpi);
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
BackupRstDlg::~BackupRstDlg()
{
}


/*----------------------------------------------------------------------------------------------
	Class accessor for folders comboxbox. If this changes, also change in BackupBkpDlg.
----------------------------------------------------------------------------------------------*/
ComboboxFolders* BackupRstDlg::GetAvailableFolders()
{
	return &m_cbfldrs;
}

/*----------------------------------------------------------------------------------------------
	Initialization
----------------------------------------------------------------------------------------------*/
void BackupRstDlg::BasicInit()
{
	m_rid = kridRestoreTab;
	m_fNonUserControlSettings = false;
	m_phwndDlg = NULL;
	m_fIsVista = IsVista();
	m_enableThemeDialogTexture = NULL;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog (restore tab).
----------------------------------------------------------------------------------------------*/
bool BackupRstDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AssertPtr(m_pbkpi);
	Assert(m_hwnd);

	if (m_enableThemeDialogTexture != NULL)
		m_enableThemeDialogTexture(m_hwnd, ETDT_ENABLETAB);

	HWND hwndList = ::GetDlgItem(m_hwnd, kctidRestoreVersion);
	ListView_SetExtendedListViewStyle(hwndList, LVS_EX_LABELTIP);

	// Set up list view to display available projects:
	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	Rect rc;
	::GetClientRect(hwndList, &rc);
	// Set up a column as wide as the list view window minus its scroll bar:
	lvc.cx = rc.Width() - ::GetSystemMetrics(SM_CXVSCROLL);
	ListView_InsertColumn(hwndList, 0, &lvc);

	// Initialize the combobox for the list of folders used for backup/restore.
	m_cbfldrs.Init(m_hwnd, kctidRestoreFrom, m_pbkpi->m_strbSelectedDirectory);
	// Also initialize the combobox for the list of projects now before we
	//  fill the folder list.
	m_avprj.Init(m_hwnd, kctidRestoreProject, kctidRestoreVersion);

	// Fill the folder list and project list.
	m_cbfldrs.FillList();
	if (m_pbkpi->m_strbSelectedDirectory.Length() > 0)
	{
		m_fNonUserControlSettings = true;
		m_cbfldrs.WriteSelectionString(m_pbkpi->m_strbSelectedDirectory);
		m_fNonUserControlSettings = false;
	}
	m_cbfldrs.GetCurrentSelection(m_pbkpi->m_strbSelectedDirectory);

	// Get the selected project and associated zip file from the restore dialog.
	m_pbkpi->m_strZipFileName.Assign(m_avprj.GetCurrentFileName().Chars());
	m_pbkpi->m_strProjectFullName.Assign(m_avprj.GetCurrentProjectName().Chars());

	m_pbkpi->m_fXml = false;
	::SendDlgItemMessage(m_hwnd, kctidRestoreXml, BM_SETCHECK, BST_UNCHECKED, 0);

	UpdateStartButton();

	return SuperClass::OnInitDlg(hwndCtrl, lp);
} // BackupRstDlg::OnInitDlg.


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool BackupRstDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(m_hwnd);

	if (!m_fIsVista && wm == WM_ERASEBKGND)
	{
		// This is needed because of a bug in the list view control that causes
		// it not to be redrawn sometimes.
		::RedrawWindow(::GetDlgItem(m_hwnd, kctidRestoreVersion), NULL, NULL,
			RDW_ERASE | RDW_FRAME | RDW_INVALIDATE);

		// Re-fill the folder list and project list.
		m_cbfldrs.FillList();
		if (m_pbkpi->m_strbSelectedDirectory.Length() > 0)
		{
			m_fNonUserControlSettings = true;
			m_cbfldrs.WriteSelectionString(m_pbkpi->m_strbSelectedDirectory);
			m_fNonUserControlSettings = false;
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Handle a notification.
----------------------------------------------------------------------------------------------*/
bool BackupRstDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);
	Assert(m_hwnd);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		return OnButtonClicked(ctidFrom);

	case CBN_EDITCHANGE:
		return OnComboBoxEditChange(ctidFrom);

	case CBN_SELCHANGE:
		return OnComboBoxSelChange(ctidFrom);

	case LVN_ITEMCHANGED:
		return OnListViewItemChanged(ctidFrom, pnmh);

	default:
		return false;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle a button being clicked.

	@param ctidFrom - id of the sending control
----------------------------------------------------------------------------------------------*/
bool BackupRstDlg::OnButtonClicked(int ctidFrom)
{
	switch (ctidFrom)
	{
	case kctidRestoreBrowseFrom:
		{ // Block
			StrAppBuf strbFile;
			m_fNonUserControlSettings = true;
			if (m_cbfldrs.BrowseFolders(m_pbkpi->m_strbSelectedDirectory, strbFile,
				kstidRstBrowseInfo))
			{
				m_avprj.DisplayAvailableProjects(m_pbkpi->m_strbSelectedDirectory, strbFile);
				m_pbkpi->m_strbDirectoryPath.Assign(
					m_avprj.GetCurrentDirectoryPath().Chars());
				m_pbkpi->m_strZipFileName.Assign(m_avprj.GetCurrentFileName().Chars());
				m_pbkpi->m_strProjectFullName.Assign(m_avprj.GetCurrentProjectName().Chars());
				UpdateStartButton();
			}
			m_fNonUserControlSettings = false;
		}
		return true;
	case kctidRestoreXml:
		m_pbkpi->m_fXml = (::SendDlgItemMessage(m_hwnd, kctidRestoreXml, BM_GETCHECK, 0, 0)
			== BST_CHECKED);
		return true;
	default:
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle a list view item being changed (checked or unchecked?).

	@param ctidFrom - id of the sending control
	@param pnmh - pointer to notifier data
----------------------------------------------------------------------------------------------*/
bool BackupRstDlg::OnListViewItemChanged(int ctidFrom, NMHDR * pnmh)
{
	if (ctidFrom == kctidRestoreVersion && !m_fNonUserControlSettings)
	{
		NMLISTVIEW * pnmlv = (NMLISTVIEW *)pnmh;
		if (pnmlv->uNewState & LVIS_SELECTED)
		{
			m_avprj.VersionSelected(pnmlv->lParam);
			m_pbkpi->m_strZipFileName.Assign(m_avprj.GetCurrentFileName().Chars());
		}
		UpdateStartButton();
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle an edit inside a combobox.

	@param ctidFrom - id of the sending control
----------------------------------------------------------------------------------------------*/
bool BackupRstDlg::OnComboBoxEditChange(int ctidFrom)
{
	if (ctidFrom == kctidRestoreFrom)
	{
		if (!m_fNonUserControlSettings)
		{
			// Get text that user has typed so far:
			StrAppBuf strb;
			int cch = ::SendDlgItemMessage(m_hwnd, kctidRestoreFrom, WM_GETTEXT,
				strb.kcchMaxStr, (LPARAM)strb.Chars());
			strb.SetLength(cch);
			m_pbkpi->m_strbSelectedDirectory = strb;

			// See if this text constitutes a valid directory:
			DWORD nFlags = GetFileAttributes(strb.Chars());
			if (nFlags != -1 && (nFlags & FILE_ATTRIBUTE_DIRECTORY))
			{
				m_fNonUserControlSettings = true;
				if (m_avprj.DisplayAvailableProjects(strb,
						m_avprj.GetCurrentFileName().Chars()))
				{
					m_pbkpi->m_strbSelectedDirectory = m_avprj.GetCurrentDirectoryPath();
					m_pbkpi->m_strZipFileName.Assign(m_avprj.GetCurrentFileName().Chars());
					m_pbkpi->m_strProjectFullName.Assign(
						m_avprj.GetCurrentProjectName().Chars());
				}
				m_fNonUserControlSettings = false;
			}
			UpdateStartButton();
		}
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle an selection change inside a combobox.

	@param ctidFrom - id of the sending control
----------------------------------------------------------------------------------------------*/
bool BackupRstDlg::OnComboBoxSelChange(int ctidFrom)
{
	if (ctidFrom == kctidRestoreFrom)
	{
		AssertPtr(m_pbkpi);
		// Get index and path name of newly selected combobox item:
		m_pbkpi->m_iCbSelection =
			m_cbfldrs.GetCurrentSelection(m_pbkpi->m_strbSelectedDirectory);
		// reset combobox selection to 0 if unable to get selection from combobox
		if (m_pbkpi->m_iCbSelection < 0)
			m_pbkpi->m_iCbSelection = 0;
		m_fNonUserControlSettings = true;
		m_avprj.DisplayAvailableProjects(m_pbkpi->m_strbSelectedDirectory);
		m_fNonUserControlSettings = false;
		m_pbkpi->m_strZipFileName.Assign(m_avprj.GetCurrentFileName().Chars());
		m_pbkpi->m_strProjectFullName.Assign(m_avprj.GetCurrentProjectName().Chars());
		UpdateStartButton();
		return true;
	}
	else if (ctidFrom == kctidRestoreProject)
	{
		// Get currently selected project:
		int i = ::SendDlgItemMessage(m_hwnd, kctidRestoreProject, CB_GETCURSEL, 0, 0);
		achar rgchNew[MAX_PATH];
		Vector<achar> vch;
		achar * pszT;
		int cch = ::SendDlgItemMessage(m_hwnd, kctidRestoreProject, CB_GETLBTEXTLEN, i,
			(LPARAM)0);
		if (cch < MAX_PATH)
		{
			pszT = rgchNew;
		}
		else
		{
			vch.Resize(cch + 1);
			pszT = vch.Begin();
		}
		cch = ::SendDlgItemMessage(m_hwnd, kctidRestoreProject, CB_GETLBTEXT, i,
			(LPARAM)pszT);
		if (cch < 0)
			pszT = _T("");
		StrApp strCbo(pszT);
		StrApp strRC(kstidRstNoFiles);

		if (strCbo == strRC)
			return true;
		Assert(i != CB_ERR);
		COMBOBOXEXITEM CBItem;
		CBItem.mask = CBEIF_LPARAM;
		CBItem.iItem = i;
		::SendDlgItemMessage(m_hwnd, kctidRestoreProject, CBEM_GETITEM, 0, (long)&CBItem);
		// We need the lParam value, as it is the index into our vector:
		m_avprj.ProjectSelected(CBItem.lParam);
		m_fNonUserControlSettings = true;
		m_avprj.UpdateVersionsDisplay();
		m_fNonUserControlSettings = false;
		m_pbkpi->m_strZipFileName.Assign(m_avprj.GetCurrentFileName().Chars());
		m_pbkpi->m_strProjectFullName.Assign(m_avprj.GetCurrentProjectName().Chars());
		UpdateStartButton();
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Check that the owning dialog's start button is enabled/disabled as appropriate
----------------------------------------------------------------------------------------------*/
void BackupRstDlg::UpdateStartButton()
{
	AssertPtr(m_phwndDlg); // Owning dialog must have a hwnd by now.

	bool fEnable = false; // Assume disabled until certain otherwise.

	// The three conditions listed in the spec can be boiled down to this: is a valid project
	// selected?
	fEnable = m_avprj.ValidProjectSelected();

	::EnableWindow(::GetDlgItem(*m_phwndDlg, kctidBackupStartRestore), fEnable);
}


/*----------------------------------------------------------------------------------------------
	Respond to tab becoming active by updating the start button status.
----------------------------------------------------------------------------------------------*/
bool BackupRstDlg::SetActive()
{
	if (m_hwnd)
		UpdateStartButton();
	return true;
}



//:>********************************************************************************************
//:>	BackupReminderDlg implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
BackupReminderDlg::BackupReminderDlg()
{
	m_rid = kridBackupReminder;
	m_pszHelpUrl = _T("User_Interface/Menus/File/Backup_and_Restore/Backup_Reminders.htm");
	m_pbkprmi = NULL;
}


/*----------------------------------------------------------------------------------------------
	Initialization. This is called by ${#ReminderDialog}.
----------------------------------------------------------------------------------------------*/
void BackupReminderDlg::Init(BackupReminderInfo * pbkprmi)
{
	m_pbkprmi = pbkprmi;
}


/*----------------------------------------------------------------------------------------------
	Specify help topic for this dialog.
----------------------------------------------------------------------------------------------*/
SmartBstr BackupReminderDlg::GetHelpTopic()
{
	return _T("khtpBackupReminders");
}

/*----------------------------------------------------------------------------------------------
	${BackupDlg} calls this method to activate the Reminder dialog.
	@param hwnd Window handle passed by ${BackupDlg}.
	@param pbkprmi Pointer to the BackupReminderInfo structure.
	@return True if a Reminder is configured.
----------------------------------------------------------------------------------------------*/
bool BackupReminderDlg::ReminderDialog(HWND hwnd, BackupReminderInfo * pbkprmi)
{
	BackupReminderDlgPtr qbkprmd;
	qbkprmd.Create();
	qbkprmd->Init(pbkprmi);

	// Run the Reminder dialog.
	int ncid = qbkprmd->DoModal(hwnd);
	// ncid takes on the following values based on how the Reminder dialog returns:
	//   Close box in upper right corner	- 2
	//   Cancel button						- 2
	//   OK button							- 1

	switch (ncid)
	{
	case 2:
		// Either the Cancel button or the close box was pressed.
		return false; // No further action.

	case 1:
		// OK button was pressed.
		return true;

	default:
		Assert(false);
		break;
	}

	return false; // No changes.
}


/*----------------------------------------------------------------------------------------------
	Handle window messages. Process window messages for the warning text.
----------------------------------------------------------------------------------------------*/
bool BackupReminderDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(m_hwnd);

	if (wm == WM_CTLCOLORSTATIC)
		if ((HWND)lp == ::GetDlgItem(m_hwnd, kctidBkpRmndWarn))
		{
			// This enables us to set the color of the warning message.
			::SetTextColor((HDC)wp, kclrRed);
			::SetBkColor((HDC)wp, GetSysColor(COLOR_3DFACE));
			// This next line signals to Windows that we've altered the device context,
			// as well as telling it to stick with the dialog color for unused space within
			// static control.
			lnRet = (long)GetSysColorBrush(COLOR_3DFACE);
			return true;
		}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they need
	initial values.)
----------------------------------------------------------------------------------------------*/
bool BackupReminderDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	Assert(m_hwnd);

	// Initialize the spin control.
	UDACCEL udAccel;
	udAccel.nSec = 0;
	udAccel.nInc = 1;

	HWND hwndT = ::GetDlgItem(m_hwnd, kctidBkpRmndDaysSpin);
	::SendMessage(hwndT, UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(hwndT, UDM_SETRANGE32, (uint) 1, (int) 14);

	AssertPtr(m_pbkprmi);
	SetDays(m_pbkprmi->m_nDays);

	::SendDlgItemMessage(m_hwnd, kctidBkpRmndOn, BM_SETCHECK,
		m_pbkprmi->m_fTurnOff ? BST_UNCHECKED : BST_CHECKED, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Set the number of days control value.
----------------------------------------------------------------------------------------------*/
void BackupReminderDlg::SetDays(int nValue)
{
	Assert(m_hwnd);

	// Don't exceed the minimum or maximum values in the spin control.
	nValue = NBound(nValue, kMinDaysInterval, kMaxDaysInterval);

	// Update the edit box.
	StrAppBuf strb;
	strb.Format(_T("%d"), nValue);
	::SendDlgItemMessage(m_hwnd, kctidBkpRmndDays, WM_SETTEXT, 0, (LPARAM)strb.Chars());
}


/*----------------------------------------------------------------------------------------------
	The OK button was pushed.
----------------------------------------------------------------------------------------------*/
bool BackupReminderDlg::OnApply(bool fClose)
{
	Assert(m_hwnd);
	StrAppBuf strb;

	// Get the text from the edit box and convert it to a number.
	int cch = ::SendDlgItemMessage(m_hwnd, kctidBkpRmndDays, WM_GETTEXT, strb.kcchMaxStr,
		(LPARAM)strb.Chars());
	strb.SetLength(cch);

	AssertPtr(m_pbkprmi);
	m_pbkprmi->m_nDays = _tstoi(strb.Chars());

	m_pbkprmi->m_fTurnOff = (::SendDlgItemMessage(m_hwnd, kctidBkpRmndOn, BM_GETCHECK, 0, 0) ==
		BST_UNCHECKED);

	return SuperClass::OnApply(fClose);
} // BackupReminderDlg::OnApply


/*----------------------------------------------------------------------------------------------
	Handles a click on a spin control.
----------------------------------------------------------------------------------------------*/
bool BackupReminderDlg::OnDeltaSpin(NMHDR * pnmh, long & lnRet)
{
	Assert(m_hwnd);

	// If the edit box has changed and is out of sync with the spin control, this will update
	// the spin's position to correspond to the edit box.
	StrAppBuf strb;
	HWND hwndEdit;
	HWND hwndSpin;

	// Get handle for the edit and spin controls.
	if (pnmh->code == UDN_DELTAPOS)
	{
		// Called from a spin control.
		hwndSpin = pnmh->hwndFrom;
		hwndEdit = (HWND)::SendMessage(hwndSpin, UDM_GETBUDDY, 0, 0);
	}
	else
	{
		// Called from an edit control.
		hwndEdit = pnmh->hwndFrom;
		switch (pnmh->idFrom)
		{
		case kctidBkpRmndDays:
			hwndSpin = ::GetDlgItem(m_hwnd, kctidBkpRmndDaysSpin);
			break;
		default:
			Assert(false);
			break;
		}
	}

	// Get the text from the edit box and convert it to a number.
	int cch = ::SendMessage(hwndEdit, WM_GETTEXT, strb.kcchMaxStr, (LPARAM)strb.Chars());
	strb.SetLength(cch);
	int nValue =  _tstoi(strb.Chars());

	if (pnmh->code == UDN_DELTAPOS)
	{
		int nDelta = ((NMUPDOWN *)pnmh)->iDelta;
		nValue += nDelta;
	}
	SetDays(nValue);

	lnRet = 0;
	return true;
} // BackupReminderDlg::OnDeltaSpin.


/*----------------------------------------------------------------------------------------------
	Handle notifications.
----------------------------------------------------------------------------------------------*/
bool BackupReminderDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case UDN_DELTAPOS: // Spin control is activated.
		return OnDeltaSpin(pnmh, lnRet);
	case EN_UPDATE:
		if (ctid == kctidBkpRmndDays)
		{
			// Make sure the value in the edit box is within range:
			StrAppBuf strb;
			int cch = ::SendMessage(pnmh->hwndFrom, WM_GETTEXT, strb.kcchMaxStr,
				(LPARAM)strb.Chars());
			strb.SetLength(cch);
			int nValue =  _tstoi(strb.Chars());
			if (nValue < kMinDaysInterval || nValue > kMaxDaysInterval)
			{
				SetDays(nValue);
				// Select all text in edit box:
				::SendMessage(pnmh->hwndFrom, EM_SETSEL, 0, -1);
			}
			::SendDlgItemMessage(m_hwnd, kctidBkpRmndOn, BM_SETCHECK, BST_CHECKED, 0);
		}
		return true;
	default:
		return false;
	}
	return false;
}


//:>********************************************************************************************
//:>	Main BackupPasswordDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
BackupPasswordDlg::BackupPasswordDlg()
{
	m_rid = kridBackupPassword;
	m_pszHelpUrl = _T("User_Interface/Menus/File/Backup_and_Restore/Backup_Password.htm");
	m_pbkppwi = NULL;
}


/*----------------------------------------------------------------------------------------------
	Initialization. This is called by ${#PasswordDialog}.
----------------------------------------------------------------------------------------------*/
void BackupPasswordDlg::Init(BackupPasswordInfo * pbkppwi)
{
	m_pbkppwi = pbkppwi;
}

/*----------------------------------------------------------------------------------------------
	Specify help topic for this dialog.
----------------------------------------------------------------------------------------------*/
SmartBstr BackupPasswordDlg::GetHelpTopic()
{
	return _T("khtpBackupPassword");
}


/*----------------------------------------------------------------------------------------------
	${BackupDlg} calls this method to activate the Password dialog.
	@param hwnd Window handle passed by BackupDlg.
	@param pbkppwi Pointer to the BackupPasswordInfo structure.
	@return True if a password was configured (even if blank).
----------------------------------------------------------------------------------------------*/
bool BackupPasswordDlg::PasswordDialog(HWND hwnd, BackupPasswordInfo * pbkppwi)
{
	BackupPasswordDlgPtr qbkppwd;
	qbkppwd.Create();
	qbkppwd->Init(pbkppwi);

	// Run the Password dialog.
	int ncid = qbkppwd->DoModal(hwnd);
	// ncid takes on the following values based on how the Password dialog returns:
	//   Close box in upper right corner	- 2
	//   Cancel button						- 2
	//   OK button							- 1

	switch (ncid)
	{
	case 2:
		// Either the Cancel button or the close box was pressed.
		return false; // No further action.

	case 1:
		// OK button was pressed.
		return true;

	default:
		Assert(false);
		break;
	}

	return false; // No changes.
}


/*----------------------------------------------------------------------------------------------
	Handle window messages. Process window messages for the warning text.
----------------------------------------------------------------------------------------------*/
bool BackupPasswordDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(m_hwnd);

	if (wm == WM_CTLCOLORSTATIC)
		if ((HWND)lp == ::GetDlgItem(m_hwnd, kctidBkpPswdWarn))
		{
			// This enables us to set the color of the warning message.
			::SetTextColor((HDC)wp, kclrRed);
			::SetBkColor((HDC)wp, GetSysColor(COLOR_3DFACE));
			// This next line signals to Windows that we've altered the device context,
			// as well as telling it to stick with the dialog color for unused space within
			// static control.
			lnRet = (long)GetSysColorBrush(COLOR_3DFACE);
			return true;
		}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they need
	initial values.)
----------------------------------------------------------------------------------------------*/
bool BackupPasswordDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	AssertPtr(m_pbkppwi);
	Assert(m_hwnd);

	::SendDlgItemMessage(m_hwnd, kctidBkpPswdPassword, WM_SETTEXT, 0,
		(LPARAM)m_pbkppwi->m_strbPassword.Chars());

	::SendDlgItemMessage(m_hwnd, kctidBkpPswdMemJog, WM_SETTEXT, 0,
		(LPARAM)m_pbkppwi->m_strbMemoryJog.Chars());

	// Set the check box state after the password boxe because setting
	// the password causes this checkbox to get selected automatically.
	::SendDlgItemMessage(m_hwnd, kctidBkpPswdLock, BM_SETCHECK,
		m_pbkppwi->m_fLock ? BST_CHECKED : BST_UNCHECKED, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Handle notifications.
----------------------------------------------------------------------------------------------*/
bool BackupPasswordDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);
	Assert(m_hwnd);

	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case EN_UPDATE:
		switch (ctid)
		{
		// We treat both password boxes the same way, so we fall through:
		case kctidBkpPswdPassword:
		case kctidBkpPswdConfirm:
			// See if there is any text in either edit box:
			{ // Block
				achar rgchT[2]; // We don't need this text
				if (::SendDlgItemMessage(m_hwnd, kctidBkpPswdPassword, WM_GETTEXT, 2,
					(LPARAM)rgchT) ||
					::SendDlgItemMessage(m_hwnd, kctidBkpPswdConfirm, WM_GETTEXT, 2,
					(LPARAM)rgchT))
				{
					AssertPtr(m_pbkppwi);
					::SendDlgItemMessage(m_hwnd, kctidBkpPswdLock, BM_SETCHECK, BST_CHECKED, 0);
				}
			} // End block
			return true;
		default:
			return false;
		}
		break;
	default:
		return false;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	The OK button was pushed.
----------------------------------------------------------------------------------------------*/
bool BackupPasswordDlg::OnApply(bool fClose)
{
	AssertPtr(m_pbkppwi);
	Assert(m_hwnd);

	m_pbkppwi->m_fLock = (::SendDlgItemMessage(m_hwnd, kctidBkpPswdLock, BM_GETCHECK, 0, 0) ==
		BST_CHECKED);

	// Test if user did want password protection:
	if (m_pbkppwi->m_fLock)
	{
		bool fPasswordOK = true;
		StrAppBuf strbPassword;

		// Get the text from the Password edit box.
		int cch = ::SendDlgItemMessage(m_hwnd, kctidBkpPswdPassword, WM_GETTEXT,
			strbPassword.kcchMaxStr, (LPARAM)strbPassword.Chars());
		strbPassword.SetLength(cch);

		// See if password is at least 8 characters long:
		if (cch < 8)
		{
			BackupErrorHandler::ErrorBox(m_hwnd, BackupErrorHandler::kWarning,
				kstidBkpPswdLenError);
			fPasswordOK = false;
		}
		if (fPasswordOK)
		{
			fPasswordOK = ValidatePasswordChars(strbPassword);
		}
		if (fPasswordOK)
		{
			fPasswordOK = ConfirmPassword(strbPassword);
		}
		if (fPasswordOK)
		{
			// Store password.
			m_pbkppwi->m_strbPassword = strbPassword;

			// Get memory jog.
			m_pbkppwi->m_strbMemoryJog.SetLength(m_pbkppwi->m_strbMemoryJog.kcchMaxStr);
			int cch = ::SendDlgItemMessage(m_hwnd, kctidBkpPswdMemJog, WM_GETTEXT,
				m_pbkppwi->m_strbMemoryJog.kcchMaxStr,
				(LPARAM)m_pbkppwi->m_strbMemoryJog.Chars());
			m_pbkppwi->m_strbMemoryJog.SetLength(cch);
		}
		else
		{
			// Delete password confirmation.
			::SendDlgItemMessage(m_hwnd, kctidBkpPswdConfirm, WM_SETTEXT, 0, (LPARAM)_T(""));
			::SetFocus(::GetDlgItem(m_hwnd, kctidBkpPswdPassword));
			::SendDlgItemMessage(m_hwnd, kctidBkpPswdPassword, EM_SETSEL, 0, -1);
			return false;
		}
	}
	//else // Password lock not required
	//{
	//	// Reset password strings:
	//	m_pbkppwi->m_strbPassword.Clear();
	//	m_pbkppwi->m_strbMemoryJog.Clear();
	//}
	return SuperClass::OnApply(fClose);
} // BackupPasswordDlg::OnApply

/*----------------------------------------------------------------------------------------------
	See if there are any invalid characters in the password:

	@param strbPassword - reference to the password string
----------------------------------------------------------------------------------------------*/
bool BackupPasswordDlg::ValidatePasswordChars(StrAppBuf & strbPassword)
{

	for (int i = 0; i < strbPassword.Length(); i++)
	{
		bool fCharOK = false;
		// REVIEW: Will this work for all character sets?
		if (strbPassword[i] >= '0' && strbPassword[i] <= '9')
			fCharOK = true;
		else if (strbPassword[i] >= 'a' && strbPassword[i] <= 'z')
			fCharOK = true;
		else if (strbPassword[i] >= 'A' && strbPassword[i] <= 'Z')
			fCharOK = true;
		else if (strbPassword[i] == '_')
			fCharOK = true;
		if (!fCharOK)
		{
			BackupErrorHandler::ErrorBox(m_hwnd, BackupErrorHandler::kWarning,
				kstidBkpPswdPuncError);
			return false;
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Check that Password and Confirmation match:

	@param strbPassword - reference to the password string
----------------------------------------------------------------------------------------------*/
bool BackupPasswordDlg::ConfirmPassword(StrAppBuf & strbPassword)
{
	StrAppBuf strbConfirm;

	// Get the text from the Confirmation edit box.
	int cch = ::SendDlgItemMessage(m_hwnd, kctidBkpPswdConfirm, WM_GETTEXT,
		strbConfirm.kcchMaxStr, (LPARAM)strbConfirm.Chars());
	strbConfirm.SetLength(cch);

	if (!strbPassword.Equals(strbConfirm))
	{
		BackupErrorHandler::ErrorBox(m_hwnd, BackupErrorHandler::kWarning,
			kstidBkpPswdMatchError);
		return false;
	}
	else
	{
		return true;
	}
}


//:>********************************************************************************************
//:>	BackupProgressDlg methods.
//:>********************************************************************************************

// arrays of resource IDs for placing checkmark icons and graying out tasks:
const int BackupProgressDlg::m_krgnBackupActivityIds[] =
	{ kridBkpProgActivity1, kridBkpProgActivity2, kridBkpProgActivity3 };
const int BackupProgressDlg::m_krgnRestoreActivityIds[] =
	{ kridBkpProgActivity1, kridBkpProgActivity2, kridBkpProgActivity3, kridBkpProgActivity4,
	kridBkpProgActivity5};
const int BackupProgressDlg::m_krgnBackupActivityIconIds[] =
	{ kridBkpProgIcon1, kridBkpProgIcon2, kridBkpProgIcon3 };
const int BackupProgressDlg::m_krgnRestoreActivityIconIds[] =
	{ kridBkpProgIcon1, kridBkpProgIcon2, kridBkpProgIcon3, kridBkpProgIcon4, kridBkpProgIcon5 };

/*----------------------------------------------------------------------------------------------
	Constructor.
	This method creates the dialog object, and also a new thread, for running the message queue
	independently. This new thread is suspended until ${#ShowDialog} is called.

	@param hEvent Handle to Event which enables backup to suspend until user confirms abort.
	@param fRestore True if we're about to do a restore, false if about to do a backup
----------------------------------------------------------------------------------------------*/
BackupProgressDlg::BackupProgressDlg(HANDLE hEvent, bool fRestore)
{
	// Set up resource according to required job:
	m_rid = (fRestore ? kridRestoreInProgress : kridBackupInProgress);

	// Set the number of activities:
	m_nNumActivities = (fRestore ? (isizeof(m_krgnRestoreActivityIds) / isizeof(int)) :
		(isizeof(m_krgnBackupActivityIds) / isizeof(int)));

	// Set up activity and icon ID lists for our task:
	m_kpnActivityIds = (fRestore ? m_krgnRestoreActivityIds : m_krgnBackupActivityIds);
	m_kpnActivityIconIds = (fRestore ? m_krgnRestoreActivityIconIds :
		m_krgnBackupActivityIconIds);

	// Initialize other data:
	m_pxczZipObject = NULL;
	m_nCompletedActivities = 0;
	m_nCurrentActivity = -1; // Not started
	m_fTimedActivity = false;
	m_nStartTime = 0;
	m_nEstimatedEndTime = 0;
	m_fAborted = false;
	m_fRestore = fRestore;
	m_fClosureAuthorized = false;

	// Get a parent window for the progress dialog.
	m_hwndParent = ::GetActiveWindow();

	// Note which cursor is the default
	m_hcurDefault = (HCURSOR)::GetClassLong(m_hwnd, GCL_HCURSOR);

	// Try to load check mark and arrow icon resources:
	m_hicon_check = ::LoadIcon(s_hinst, MAKEINTRESOURCE(kridBackupIconCheck));
	m_hicon_arrow = ::LoadIcon(s_hinst, MAKEINTRESOURCE(kridBackupIconArrow));

	m_hEventAbort = hEvent;
}


/*----------------------------------------------------------------------------------------------
	Destructor. Checks that dialog is not still shown, and kills independent thread if it was
	running.
----------------------------------------------------------------------------------------------*/
BackupProgressDlg::~BackupProgressDlg()
{
	m_fClosureAuthorized = true;

	if (m_pxczZipObject)
		m_pxczZipObject->DetachProgressDlg();
	m_pxczZipObject = NULL;

	if (m_hwnd)
		::DestroyWindow(m_hwnd);

	// Re-enable the parent window if it still exists.
	if (m_hwndParent != NULL && IsWindow(m_hwndParent))
		::EnableWindow(m_hwndParent, true);
}

/*----------------------------------------------------------------------------------------------
	Call this instead of ${AfDialog#DoModeless}, as this method resumes the thread for handling
	messages independently.
----------------------------------------------------------------------------------------------*/
void BackupProgressDlg::ShowDialog()
{
	if (m_hwndParent != NULL)
		::EnableWindow(m_hwndParent, false);

	DoModeless(NULL);

	// Once the window is created, reposition it at the center of the parent window
	if (m_hwndParent != NULL)
	{
		RECT parentRect;
		::GetWindowRect(m_hwndParent, &parentRect);

		RECT progressRect;
		::GetWindowRect(m_hwnd, &progressRect);

		::MoveWindow(m_hwnd,
			(parentRect.right + parentRect.left - progressRect.right + progressRect.left) / 2,
			(parentRect.bottom + parentRect.top - progressRect.bottom + progressRect.top) / 2,
			progressRect.right - progressRect.left,
			progressRect.bottom - progressRect.top,
			true);
	}
}

/*----------------------------------------------------------------------------------------------
	Adds the specified activity index to list list of activities to omit, which will result in
	the corresponding text being grayed out.
----------------------------------------------------------------------------------------------*/
void BackupProgressDlg::OmitActivity(int iActivity)
{
	// Check range of activity number.
	Assert(iActivity > m_nCurrentActivity && iActivity < m_nNumActivities);

	m_viOmittedActivities.Push(iActivity);

	if (m_hwnd)
	{
		// The dialog is already displayed, so we must now gray out the specified activity:
		::EnableWindow(::GetDlgItem(m_hwnd, m_kpnActivityIds[iActivity]), false);
	}
}

/*----------------------------------------------------------------------------------------------
	Stores a reference to the Xceed Zip object, so we can tell it to stop if the abort button is
	pressed.
----------------------------------------------------------------------------------------------*/
void BackupProgressDlg::SetXceedObject(XceedZipBackup * pxcz)
{
	m_pxczZipObject = pxcz;
}

/*----------------------------------------------------------------------------------------------
	Tell the dialog the name of the current project being backed up or restored.
	This may only be called after ${#ShowDialog}.
----------------------------------------------------------------------------------------------*/
void BackupProgressDlg::SetProjectName(StrUni stuName)
{
	// Make sure the dialog is displayed:
	Assert(m_hwnd);

	m_strProject.Assign(stuName.Chars());
	StrApp strFmt(m_fRestore ? kstidRstProgressProj : kstidBkpProgressProj);
	StrApp str;
	str.Format(strFmt.Chars(), m_strProject.Chars());
	::SendDlgItemMessage(m_hwnd, kctidBkpProgAction, WM_SETTEXT, 0, (LPARAM)str.Chars());
	UpdateWindowControls();
}


/*----------------------------------------------------------------------------------------------
	Move the progress indicator to start of specified activity, and put check by previous
	activity.
	This may only be called after ${#ShowDialog}.
----------------------------------------------------------------------------------------------*/
void BackupProgressDlg::SetActivityNumber(int iActivity)
{
	// Check range of activity number:
	Assert(iActivity >= 0 && iActivity <= m_nNumActivities);
	// Make sure the dialog is displayed:
	Assert(m_hwnd);

	if (m_nCurrentActivity >= 0 && m_nCurrentActivity < iActivity && m_hicon_check && m_hicon_arrow)
	{
		Assert(m_nCurrentActivity < m_nNumActivities);

		// Put check by current (now completed) activity:
		int nIconId_check = m_kpnActivityIconIds[m_nCurrentActivity];
		::SendDlgItemMessage(m_hwnd, nIconId_check, STM_SETICON, (WPARAM)m_hicon_check, (LPARAM)0);

		if (iActivity < m_nNumActivities)
		{
			// Put arrow by next activity:
			int nIconId_arrow = m_kpnActivityIconIds[iActivity];
			::SendDlgItemMessage(m_hwnd, nIconId_arrow, STM_SETICON, (WPARAM)m_hicon_arrow, (LPARAM)0);
		}
		m_nCompletedActivities++;
	}

	m_nCurrentActivity = iActivity;

	// Cancel timer, if it existed:
	if (m_fTimedActivity)
	{
		::KillTimer(m_hwnd, 1);
		m_fTimedActivity = false;
		m_nEstimatedEndTime = 0;
		m_nStartTime = 0;
	}

	// See if we've completed the last activity:
	if (iActivity == m_nNumActivities && !m_fAborted)
	{
		// Signal that we're done:
		StrAppBuf strbMessage;
		StrAppBuf strbFmt;
		if (m_fRestore)
			strbFmt.Load(kstidRstComplete);
		else
			strbFmt.Load(kstidBkpComplete);
		strbMessage.Format(strbFmt.Chars(), m_strProject.Chars());
		::SendDlgItemMessage(m_hwnd, kctidBkpProgAction, WM_SETTEXT, 0,
			(LPARAM)strbMessage.Chars());
		SetPercentComplete(100);

		// Hide & disable the abort button, and show & enable the close button:
		HWND hwndButton = ::GetDlgItem(m_hwnd, kctidBkpProgAbort);
		::ShowWindow(hwndButton, SW_HIDE);
		::EnableWindow(hwndButton, false);
		// Make sure close button is shown:
		hwndButton = ::GetDlgItem(m_hwnd, kctidBkpProgClose);
		::ShowWindow(hwndButton, SW_SHOW);
		::EnableWindow(hwndButton, true);
	}
	else // We're not done yet:
	{
		// Move progress indicator to start of new activity:
		SetPercentComplete(0);
	}
	UpdateWindowControls();
}


/*----------------------------------------------------------------------------------------------
	Work out what time it should be when the current activity is concluded, and set timer in
	motion to update the progress indicator at reasonable intervals.
	This may only be called after ${#ShowDialog}.
----------------------------------------------------------------------------------------------*/
void BackupProgressDlg::SetActivityEstimatedTime(int nSeconds)
{
	// Make sure the dialog is displayed:
	Assert(m_hwnd);
	// Make sure we're not on the pre-initialization activity:
	Assert(m_nCurrentActivity > -1);
	// Check estimated time is not negative:
	Assert(nSeconds >= 0);

	// Cancel timer, if it already existed:
	if (m_fTimedActivity)
	{
		::KillTimer(m_hwnd, 1);
		m_fTimedActivity = false;
		m_nEstimatedEndTime = 0;
		m_nStartTime = 0;
	}

	// Check the current time:
	SYSTEMTIME systTime;
	GetLocalTime(&systTime);

	// Change format of local time to one we can use to compare with another:
	FILETIME filtTime;
	SystemTimeToFileTime(&systTime, &filtTime);

	m_nStartTime = *((int64 *)(&filtTime));

	// Make an adjustment, to allow for a margin of error, plus the fact that the indicator bar
	// will not move along all the way on timed mode anyway, so we wouldn't want it to appear
	// jammed, just because our estimate was correct.
	nSeconds = (int)(1.2 * nSeconds);

	// Determine time to finish:
	m_nEstimatedEndTime = m_nStartTime + nSeconds * kn100NanoSeconds;

	::SetTimer(m_hwnd, 1, 2000, 0);
	m_fTimedActivity = true;
	UpdateWindowControls();
}


/*----------------------------------------------------------------------------------------------
	Sets the amount of the progress bar to be filled in, within current activity.
	This may only be called after ${#ShowDialog}.
----------------------------------------------------------------------------------------------*/
void BackupProgressDlg::SetPercentComplete(int nPercent)
{
	Assert(nPercent >= 0 && nPercent <= 100); // Check percentage range.
	Assert(m_nNumActivities > 0);
	// Make sure the dialog is displayed:
	Assert(m_hwnd);
	// Make sure we're not on the pre-initialization activity:
	Assert(m_nCurrentActivity > -1);

	// If the user has already chosen to abort, don't confuse them by altering indicator:
	if (m_fAborted)
		return;

	// Adjust setting to fit within the range for the current activity:
	int nSetting = nPercent + 100 * m_nCompletedActivities;

	::SendDlgItemMessage(m_hwnd, kctidBkpProgProgress, PBM_SETPOS, nSetting, 0);
	UpdateWindowControls();
}


/*----------------------------------------------------------------------------------------------
	Enables or disables the Abort button. The button should be disabled once the 'point of no
	return' is reached in Restore operations.
	@param fEnabled True to enable the button, false to disable it.
----------------------------------------------------------------------------------------------*/
void BackupProgressDlg::EnableAbortButton(bool fEnable)
{
	HWND hwndButton = ::GetDlgItem(m_hwnd, kctidBkpProgAbort);
	Assert(hwndButton);
	::EnableWindow(hwndButton, fEnable);
	UpdateWindowControls();
}

/*----------------------------------------------------------------------------------------------
	Update the progress dialog controls and dispatch any messages
----------------------------------------------------------------------------------------------*/
void BackupProgressDlg::UpdateWindowControls()
{
	UpdateWindow(m_hwnd);
	MSG msg;
	while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE))
	{
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}
}

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they need
	initial values.)
----------------------------------------------------------------------------------------------*/
bool BackupProgressDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the range of the progress bar:
	::SendDlgItemMessage(m_hwnd, kctidBkpProgProgress, PBM_SETRANGE32, 0,
		100 * (m_nNumActivities - m_viOmittedActivities.Size()));
	// Give the progress bar a white background:
	::SendDlgItemMessage(m_hwnd, kctidBkpProgProgress, PBM_SETBKCOLOR, 0, (LPARAM)kclrWhite);

	// Gray out omitted activities:
	for (int i = 0; i < m_viOmittedActivities.Size(); i++)
		::EnableWindow(::GetDlgItem(m_hwnd, m_kpnActivityIds[m_viOmittedActivities[i]]), false);

	// Make sure abort button is enabled:
	::EnableWindow(::GetDlgItem(m_hwnd, kctidBkpProgAbort), true);
	// Make sure close button is hidden:
	HWND hwndClose = ::GetDlgItem(m_hwnd, kctidBkpProgClose);
	::ShowWindow(hwndClose, SW_HIDE);

	// Put arrow by first activity to indicate it is in progress:
	if (m_hicon_arrow)
	{
		int nIconId_arrow = m_kpnActivityIconIds[0];
		::SendDlgItemMessage(m_hwnd, nIconId_arrow, STM_SETICON, (WPARAM)m_hicon_arrow, (LPARAM)0);
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool BackupProgressDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);

	switch (wm)
	{
	case BKP_PRG_PERCENT:
		SetPercentComplete(wp);
		return true;
	case BKP_PRG_GET_EVENT:
		lnRet = (long)m_hEventAbort;
		return true;
	case WM_TIMER:
		OnTimer(wp);
		return true;
	case WM_CLOSE:
		// Restore default cursor, in case we altered it during an abort:
		::SetClassLong(m_hwnd, GCL_HCURSOR, (long)m_hcurDefault);
		break;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Artificially advance progress indicator.
----------------------------------------------------------------------------------------------*/
void BackupProgressDlg::OnTimer(UINT)
{
	// Make sure timed activity was properly set up:
	Assert(m_fTimedActivity);
	Assert(m_nStartTime);
	Assert(m_nEstimatedEndTime);

	// Get the current time:
	SYSTEMTIME systTime;
	FILETIME filtTime;
	GetLocalTime(&systTime);
	SystemTimeToFileTime(&systTime, &filtTime);
	int64 nTime = *((int64 *)(&filtTime));

	int64 nDuration = nTime - m_nStartTime;
	int64 nTotalEstimatedDuration = m_nEstimatedEndTime - m_nStartTime;

	// If estimated time is less than 1 second, fiddle it to look like 1 second:
	if (nTotalEstimatedDuration <= kn100NanoSeconds)
		nTotalEstimatedDuration = kn100NanoSeconds;

	// Check no data will be lost when we convert percentage time to int:
	Assert(((100 * nDuration) / nTotalEstimatedDuration) <
		((int64)1 << (int64)(8 * isizeof(int))));
	// Check that activity has not been running more than 75% of its estimated time:
	int nPercentageDuration = int((100 * nDuration) / nTotalEstimatedDuration);
	if (nPercentageDuration <= 75)
	{
		// Advance progress indicator:
		SetPercentComplete(nPercentageDuration);
	}
}

/*----------------------------------------------------------------------------------------------
	Handle notifications.
----------------------------------------------------------------------------------------------*/
bool BackupProgressDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);
	Assert(m_hwnd);

	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctid)
		{
		case kctidBkpProgClose:
			m_fClosureAuthorized = true;
			OnCancel();
			break;
		case kctidBkpProgAbort:
			{ // Begin block
				// Reset our event to non-signaled, so that backup won't keep running in the
				// other thread:
				::ResetEvent(m_hEventAbort);

				StrAppBuf strbMessage;
				m_fAborted = true;
				strbMessage.Load(kstidBkpAborting);
				::SendDlgItemMessage(m_hwnd, kctidBkpProgAction, WM_SETTEXT, 0,
					(LPARAM)strbMessage.Chars());
				::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)strbMessage.Chars());
				if (m_pxczZipObject)
					m_pxczZipObject->SetAbort(true);
				// Disable abort button:
				::EnableWindow(::GetDlgItem(m_hwnd, kctidBkpProgAbort), false);
				// Set cursor to include small hourglass:
				HCURSOR hcurWait = ::LoadCursor(NULL, IDC_APPSTARTING);
				::SetClassLong(m_hwnd, GCL_HCURSOR, (long)hcurWait);

				// Set our event to signaled, so that backup may resume in the other thread:
				::SetEvent(m_hEventAbort);
			} // End block
		default:
			return false;
		}
		return true;
	default:
		return false;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	See if we're allowed to close. This will only be possible if the destructor has been called.
	Otherwise, the user could destroy the progress dialog just by pressing the escape key.
----------------------------------------------------------------------------------------------*/
bool BackupProgressDlg::OnCancel()
{
	if (m_fClosureAuthorized)
		return SuperClass::OnCancel();

	return false;
}

//:>********************************************************************************************
//:>	ScheduledBackupWarningDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
ScheduledBackupWarningDlg::ScheduledBackupWarningDlg()
{
	m_rid = kridScheduledBackupWarning;
	m_nSeconds = 0;
	m_nResponse = kCancel;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
ScheduledBackupWarningDlg::~ScheduledBackupWarningDlg()
{
	::KillTimer(m_hwnd, 1);
}

/*----------------------------------------------------------------------------------------------
	Called by ${BackupHandler#Backup}, this method displays a dialog warning of an impending
	backup. There is a set amount of time to count down before the backup will begin, during
	which time the user may abort the backup or choose to see the backup options instead.
	@param nSeconds The time in seconds to count down before returning from modal operation.
	@param hwndPar The handle of the owning window.
	@param fLocked Indicates whether the backups to be performed will result in
	files which are locked with a password.
	@return One of the following enumerated constants: OK, Aborted, kOptions.
----------------------------------------------------------------------------------------------*/
int ScheduledBackupWarningDlg::ScheduledBackupWarning(int nSeconds, HWND hwndPar, bool fLocked)
{
	ScheduledBackupWarningDlgPtr qshdbkw;
	qshdbkw.Create();
	qshdbkw->Init(nSeconds, fLocked);

	// Run the dialog.
	qshdbkw->DoModal(hwndPar);

	return qshdbkw->m_nResponse;
}

/*----------------------------------------------------------------------------------------------
	Initialize with number of seconds to count down and an indication of whether the backups to
	be performed will result in files which are locked with a password.
----------------------------------------------------------------------------------------------*/
void ScheduledBackupWarningDlg::Init(int nSeconds, bool fLocked)
{
	m_nSeconds = nSeconds;
	m_fLocked = fLocked;
}

/*----------------------------------------------------------------------------------------------
	Process timer messages.
----------------------------------------------------------------------------------------------*/
bool ScheduledBackupWarningDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);

	switch (wm)
	{
	case WM_TIMER:
		OnTimer(wp);
		return true;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Make a note of the time left until exit, and start our timer running.
----------------------------------------------------------------------------------------------*/
bool ScheduledBackupWarningDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	Assert(m_hwnd);

	// Set default response:
	m_nResponse = kCancel;

	StrApp str((m_fLocked) ? kstidPasswordInUse : kstidNoPasswordInUse);
	::SendDlgItemMessage(m_hwnd, kctidSchdPasswordWarning, WM_SETTEXT, 0, (LPARAM)str.Chars());


	SYSTEMTIME systTime;
	GetLocalTime(&systTime);
	// Change format of local time to one we can use to compare with another:
	FILETIME filtTime;
	SystemTimeToFileTime(&systTime, &filtTime);
	// Determine time to finish with dialog:
	m_nEndTime = *((int64 *)(&filtTime)) + m_nSeconds * kn100NanoSeconds;

	::SetTimer(m_hwnd, 1, 100, 0);

	// Make sure dialog gets noticed by user:
	SetWindowPos(m_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
	MessageBeep(MB_ICONQUESTION);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	See if we've run out of time yet.
----------------------------------------------------------------------------------------------*/
void ScheduledBackupWarningDlg::OnTimer(UINT)
{
	// Get the current time:
	SYSTEMTIME systTime;
	FILETIME filtTime;
	GetLocalTime(&systTime);
	SystemTimeToFileTime(&systTime, &filtTime);

	if (*((int64 *)(&filtTime)) + m_nSeconds * kn100NanoSeconds > m_nEndTime)
	{
		m_nSeconds--;
		SetTimeLeft(m_nSeconds);
		if (m_nSeconds <= 0)
		{
			// Out of time:
			m_nResponse = kStartNow;
			SuperClass::OnCancel();
		}
	}
}

/*----------------------------------------------------------------------------------------------
	See if user pressed any buttons.
----------------------------------------------------------------------------------------------*/
bool ScheduledBackupWarningDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctid)
		{
		case kctidSchdNow:
			m_nResponse = kStartNow;
			SuperClass::OnCancel();
			break;
		case kctidSchdOptions:
			m_nResponse = kOptions;
			SuperClass::OnCancel();
			break;
		case kctidSchdCancel:
			/* Uncomment this block instead of existing block if you want confirmation box.
			{ // Begin block
				StrAppBuf strbMessage(kstidBkpQueryAbort);
				StrAppBuf strbTitle(kstidBkpAbort);

				if (::MessageBox(m_hwnd, strbMessage.Chars(), strbTitle.Chars(),
					MB_ICONQUESTION | MB_YESNO) == IDYES)
				{
					strbMessage.Load(kstidBkpAborting);
					::SendDlgItemMessage(m_hwnd, kctidSchdBkpTime, WM_SETTEXT, 0,
						(LPARAM)strbMessage.Chars());
					m_nResponse = kCancel;
					SuperClass::OnCancel();
				}
			} // End block
			*/
			m_nResponse = kCancel;
			SuperClass::OnCancel();
			break;
		default:
			return false;
		}
		return true;
	default:
		return false;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Display how many seconds are remaining.
----------------------------------------------------------------------------------------------*/
void ScheduledBackupWarningDlg::SetTimeLeft(int nSeconds)
{
	Assert(m_hwnd);

	StrApp str;
	StrApp strFormat(kstidBkpSchedWarnTime);
	str.Format(strFormat.Chars(), nSeconds);
	::SendDlgItemMessage(m_hwnd, kctidSchdBkpTime, WM_SETTEXT, 0, (LPARAM)str.Chars());
}



//:>********************************************************************************************
//:>	BackupNagDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
BackupNagDlg::BackupNagDlg()
{
	m_rid = kridBackupNag;
	m_pszHelpUrl = _T("User_Interface/Menus/File/Backup_and_Restore/Backup_Reminder.htm");
	m_nResponse = kCancel;
}

/*----------------------------------------------------------------------------------------------
	Initialize with number of days to display in message
----------------------------------------------------------------------------------------------*/
void BackupNagDlg::Init(int nDays, IHelpTopicProvider * phtprov, bool fLocked)
{
	m_nDays = nDays;
	if (phtprov)
		AfDialog::s_qhtprovHelpUrls = phtprov;
	m_fLocked = fLocked;
}

/*----------------------------------------------------------------------------------------------
	Specify help topic for this dialog.
----------------------------------------------------------------------------------------------*/
SmartBstr BackupNagDlg::GetHelpTopic()
{
	return _T("khtpBackupReminder");
}

/*----------------------------------------------------------------------------------------------
	Called by ${BackupHandler#Remind}, this method displays the nag message, and solicits a
	response from the user.
	@param nDays Number of days to display in text message.
	@param hwndPar Handle to window to be used as parent to dialog.
	@param phtprov pointer to a help topic provider to get app-specific information
	@param fLocked Indicates whether the backups to be performed will result in
	files which are locked with a password.
	@return Enumerated value representing user's response.
----------------------------------------------------------------------------------------------*/
int BackupNagDlg::BackupNag(int nDays, bool fLocked, HWND hwndPar, IHelpTopicProvider * phtprov)
{
	BackupNagDlgPtr qbkpnag;
	qbkpnag.Create();
	qbkpnag->Init(nDays, phtprov, fLocked);

	// Run the dialog.
	qbkpnag->DoModal(hwndPar);

	return qbkpnag->m_nResponse;
}

/*----------------------------------------------------------------------------------------------
	Set up icon and text message
----------------------------------------------------------------------------------------------*/
bool BackupNagDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	Assert(m_hwnd);

	// Set default response:
	m_nResponse = kCancel;

	StrApp str((m_fLocked) ? kstidPasswordInUse : kstidNoPasswordInUse);
	::SendDlgItemMessage(m_hwnd, kctidBkpNagPasswordWarning, WM_SETTEXT, 0, (LPARAM)str.Chars());


	StrApp strFmt;
	StrApp strText;
	HWND hwnd;

	HICON hicon = ::LoadIcon(NULL, IDI_QUESTION);
	if (hicon)
	{
		hwnd = ::GetDlgItem(m_hwnd, kctidBkpNagIcon);
		::SendMessage(hwnd, STM_SETICON, (WPARAM)hicon, (LPARAM)0);
	}

	hwnd = ::GetDlgItem(m_hwnd, kctidBkpNagText);
	strFmt.Load(kstidBkpRemind);
	strText.Format(strFmt.Chars(), m_nDays);
	::SetWindowText(hwnd, strText.Chars());

	// Make sure dialog gets noticed by user:
	SetWindowPos(m_hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
	MessageBeep(MB_ICONQUESTION);
	SetFocus(m_hwnd);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	See if user pressed any buttons.
----------------------------------------------------------------------------------------------*/
bool BackupNagDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctid)
		{
		case kctidBkpNagYes:
			m_nResponse = kDoBackup;
			SuperClass::OnCancel();
			break;
		case kctidBkpNagNo:
			m_nResponse = kCancel;
			SuperClass::OnCancel();
			break;
		case kctidBkpNagConfigure:
			m_nResponse = kConfigure;
			SuperClass::OnCancel();
			break;
		default:
			return false;
		}
		return true;
	default:
		return false;
	}
	return false;
}



//:>********************************************************************************************
//:>	RestorePasswordDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RestorePasswordDlg::RestorePasswordDlg()
{
	m_rid = kridRestorePasswordDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/File/Backup_and_Restore/Enter_Password.htm");
	m_fMemJogInUse = false;
}

/*----------------------------------------------------------------------------------------------
	Initialization.
	@param strMemoryJog The user's memory jog (may be blank)
	@param strDatabase the name of the database about to be restored
	@param strProject The name of the project about to be restored
	@param strVersion The version (date and time) of the database about to be restored
----------------------------------------------------------------------------------------------*/
void RestorePasswordDlg::Init(StrApp strMemoryJog, StrApp strDatabase, StrApp strProject,
	StrApp strVersion)
{
	m_strMemoryJog = strMemoryJog;
	m_fMemJogInUse = (m_strMemoryJog.Length() > 0);
	m_strDatabase = strDatabase;
	m_strProject = strProject;
	m_strVersion = strVersion;
}

/*----------------------------------------------------------------------------------------------
	Called by ${XceedZipSink#Invoke}, this method askes the user for the password to unlock a
	zipped backup file.
	@param hwnd [in] Handle of window to be used as dialog parent.
	@param strMemoryJog [in] String containing clue to guessing password.
	@param strDatabase [in] Name of database in backup file.
	@param strPassword [out] User's guess at password.
----------------------------------------------------------------------------------------------*/
bool RestorePasswordDlg::GetPassword(HWND hwnd, StrApp strMemoryJog, StrApp strDatabase,
	StrApp strProject, StrApp strVersion, StrApp & strPassword)
{
	RestorePasswordDlgPtr qrstpwd;
	qrstpwd.Create();
	qrstpwd->Init(strMemoryJog, strDatabase, strProject, strVersion);

	// Run the Password dialog.
	int ncid = qrstpwd->DoModal(hwnd);
	// ncid takes on the following values based on how the Password dialog returns:
	//   Close box in upper right corner	- 2
	//   Cancel button						- 2
	//   OK button							- 1

	switch (ncid)
	{
	case 2:
		// Either the Cancel button or the close box was pressed.
		return false; // No further action.

	case 1:
		// OK button was pressed.
		strPassword.Assign(qrstpwd->m_strPassword);
		return true;

	default:
		Assert(false);
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they need
	initial values.)
----------------------------------------------------------------------------------------------*/
bool RestorePasswordDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	Assert(m_hwnd);

	StrApp str;
	if (m_strProject.Equals(m_strDatabase))
		str.Assign(m_strProject);
	else
		str.Format(_T("%s (%s)"), m_strProject.Chars(), m_strDatabase.Chars());
	::SendDlgItemMessage(m_hwnd, kctidDatabase, WM_SETTEXT, 0, (LPARAM)str.Chars());
	::SendDlgItemMessage(m_hwnd, kctidBackupVersion, WM_SETTEXT, 0,
		(LPARAM)m_strVersion.Chars());

	if (m_fMemJogInUse)
	{
		StrApp str;
		str.Format(_T("(%s)"), m_strMemoryJog.Chars());
		::SendDlgItemMessage(m_hwnd, kctidMemoryJog, WM_SETTEXT, 0, (LPARAM)str.Chars());
		// Hide the edit window we won't be using:
		HWND hwndRedundantEdit = ::GetDlgItem(m_hwnd, kctidPasswordNoJog);
		::ShowWindow(hwndRedundantEdit, SW_HIDE);
	}
	else
	{
		// Hide the edit window we won't be using:
		HWND hwndRedundantEdit = ::GetDlgItem(m_hwnd, kctidPassword);
		::ShowWindow(hwndRedundantEdit, SW_HIDE);
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	User pressed OK. Retrieve their guess at the password.
----------------------------------------------------------------------------------------------*/
bool RestorePasswordDlg::OnApply(bool fClose)
{
	Assert(m_hwnd);

	StrAppBuf strb;
	int cch = ::SendDlgItemMessage(m_hwnd, m_fMemJogInUse? kctidPassword : kctidPasswordNoJog,
		WM_GETTEXT, strb.kcchMaxStr, (LPARAM)strb.Chars());
	strb.SetLength(cch);
	m_strPassword.Assign(strb.Chars(), cch);

	return SuperClass::OnApply(fClose);
}


//:>********************************************************************************************
//:>	RestoreOptionsDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RestoreOptionsDlg::RestoreOptionsDlg()
{
	m_rid = kridRestoreDbExists;
	m_pszHelpUrl = _T("User_Interface/Menus/File/Backup_and_Restore/Restore_Options.htm");
	m_fNewDatabaseName = false;
	m_nUserResponse = kNoDbClash; // Default
}

/*----------------------------------------------------------------------------------------------
	Initialization.
	@param stuProjectName Name of project about to be overwritten
	@param stuDatabaseName Name of database about to be overwritten
----------------------------------------------------------------------------------------------*/
void RestoreOptionsDlg::Init(StrUni stuProjectName, StrUni stuDatabaseName)
{
	m_fNewDatabaseName = false;
	m_stuProjectName = stuProjectName;
	m_stuDatabaseName = stuDatabaseName;
	m_nUserResponse = kNoDbClash; // Default
}

/*----------------------------------------------------------------------------------------------
	Called by ${BackupHandler#Restore}, this method tests if the named database exists already,
	and if so, asks the user whether they want to overwrite or rename.
	@param hwnd [in] Handle of the window to be used as dialog parent.
	@param stuProjectName [in] Original short name of project.
	@param stuDatabaseName [in] Name of database about to be overwritten.
	@param stuNewDatabaseName [out] New name of database, if required by user.
	@return Enumerated value, describing results of clash test and user choice.
----------------------------------------------------------------------------------------------*/
int RestoreOptionsDlg::DbClashAction(HWND hwnd, StrUni stuDatabaseName, StrUni & stuProjectName,
	StrUni & stuNewDatabaseName)
{
	//StrApp strProjectName;
	//StrAppBufPath strbpSourceFileName(rsti->m_strZipFileName.Chars());

	//BackupFileNameProcessor::GetProjectName(strbpSourceFileName, strProjectName);
	//stuProjectName.Assign(strProjectName.Chars());

	// Check if target database already exists:
	HRESULT hr;
	IStreamPtr qfist;
	try
	{
		StrUni stuServerName = s_stuLocalServer;
		IOleDbEncapPtr qodeTest;
		// Try to connect to the stuDatabase:
		qodeTest.CreateInstance(CLSID_OleDbEncap);
		// Get the IStream pointer for logging.
		qfist = s_qfist;
		hr = qodeTest->Init(stuServerName.Bstr(), stuDatabaseName.Bstr(), qfist, koltMsgBox,
			koltvForever);
	}
	catch (...)
	{
		// Clear off any error information so we don't get an assert later on!
		IErrorInfo * pIErrorInfo = NULL;
		hr = ::GetErrorInfo(0, &pIErrorInfo);
		return kDbReadError;
	}

	if (hr != S_OK)
	{
		// Clear off the error information so we don't get an assert later on!
		IErrorInfo * pIErrorInfo = NULL;
		hr = ::GetErrorInfo(0, &pIErrorInfo);
		// Database does not already exist:
		return kNoDbClash;
	}
	RestoreOptionsDlgPtr qrstdbe;
	qrstdbe.Create();
	qrstdbe->Init(stuProjectName, stuDatabaseName);

	// Run the Restore Options dialog.
	int ncid = qrstdbe->DoModal(hwnd);
	// ncid takes on the following values based on how the Restore Options dialog returns:
	//   Close box in upper right corner	- 2
	//   Cancel button						- 2
	//   Replace button						- 1

	switch (ncid)
	{
	case 2:
		// Either the Cancel button or the close box was pressed.
		return kUserCancel;

	case 1:
		// 'Restore' button was pressed.
		if (qrstdbe->m_fNewDatabaseName)
			stuNewDatabaseName = qrstdbe->m_stuDatabaseName;
		return qrstdbe->m_nUserResponse;

	default:
		Assert(false);
		break;
	}
	return kUserCancel;
}

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they need
	initial values.)
----------------------------------------------------------------------------------------------*/
bool RestoreOptionsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	Assert(m_hwnd);

	StrApp strFmt;
	StrApp strText;
	HWND hwnd;

	HICON hicon = ::LoadIcon(NULL, IDI_WARNING);
	if (hicon)
	{
		hwnd = ::GetDlgItem(m_hwnd, kridRestoreDbExistsIcon);
		::SendMessage(hwnd, STM_SETICON, (WPARAM)hicon, (LPARAM)0);
	}

	hwnd = ::GetDlgItem(m_hwnd, kctidRestoreDbExistsText);
	strFmt.Load(kstidRstDbExists2);
	StrApp str(m_stuProjectName.Chars());
	if (!m_stuProjectName.EqualsCI(m_stuDatabaseName))
	{
		str.Append(" (");
		str.Append(m_stuDatabaseName.Chars());
		str.Append(")");
	}

	strText.Format(strFmt.Chars(), str.Chars());
	::SetWindowText(hwnd, strText.Chars());

	::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteRenameBtn, BM_SETCHECK,
		(WPARAM)BST_CHECKED, 0);
	::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteReplaceBtn, BM_SETCHECK,
		(WPARAM)BST_UNCHECKED, 0);

	strText.Assign(m_stuDatabaseName.Chars());
	strText.Append("-");
	str.Load(kstidRstDbOld);
	strText.Append(str.Chars());
	::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteName, WM_SETTEXT, 0,
		(LPARAM)strText.Chars());
	::SetFocus(::GetDlgItem(m_hwnd, kctidRestoreOverwriteName));

	::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteCheck, BM_SETCHECK, (WPARAM)BST_UNCHECKED,
		0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	User pressed OK.
----------------------------------------------------------------------------------------------*/
bool RestoreOptionsDlg::OnApply(bool fClose)
{
	Assert(m_hwnd);

	// See which radio button is checked:
	bool fRename =  (::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteRenameBtn, BM_GETCHECK,
		0, 0) == BST_CHECKED);

	// See if there is any text in the rename box:
	int nLen = ::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteName, WM_GETTEXTLENGTH, 0, 0);
	if (fRename)
	{
		if (nLen == 0)
		{
			// User has selected rename, but left the name box empty. This is not allowed:
			BackupErrorHandler::ErrorBox(m_hwnd, BackupErrorHandler::kWarning,
				kstidRstRenameEmptyError);
			::SetFocus(::GetDlgItem(m_hwnd, kctidRestoreOverwriteName));
			return false;
		}
		m_fNewDatabaseName = true;

		// Get the text from the database name edit box.
		Vector<achar> vch;
		vch.Resize(nLen + 1);
		::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteName, WM_GETTEXT, 1 + nLen,
			(LPARAM)vch.Begin());

		m_stuDatabaseName.Assign(vch.Begin());
		StrUtil::TrimWhiteSpace(m_stuDatabaseName.Chars(), m_stuDatabaseName);

		// Perform NFC normalization
		if (!StrUtil::NormalizeStrUni(m_stuDatabaseName, UNORM_NFC))
			ThrowInternalError(E_FAIL, "Normalize failure in RestoreOptionsDlg::OnApply.");

		// Make sure that the generated database name is valid
		m_stuDatabaseName.Assign(AfDbApp::FilterForFileName(m_stuDatabaseName));

		// See if new name already exists as a database:
		try
		{
			IOleDbEncapPtr qodeTest;
			IStreamPtr qfist;
			// Try to connect to the Database:
			qodeTest.CreateInstance(CLSID_OleDbEncap);
			HRESULT hr;
			// Get the IStream pointer for logging.
			qfist = s_qfist;
			hr = qodeTest->Init(s_stuLocalServer.Bstr(), m_stuDatabaseName.Bstr(), qfist,
				koltMsgBox, koltvForever);
			if (hr == S_OK)
			{
				// We were able to connect to a database of that name, so it must already exist.
				// Check if the overwrite check box was checked:
				if (::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteCheck, BM_GETCHECK, 0, 0)
					== BST_CHECKED)
					m_nUserResponse = kOverwriteOther;
				else
				{
					StrApp str(m_stuDatabaseName.Chars());
					BackupErrorHandler::MessageBox(m_hwnd, BackupErrorHandler::kWarning,
						kstidRstFileExists, kstidRstRenameExists, MB_ICONEXCLAMATION | MB_OK,
						str.Chars());
					::SetFocus(::GetDlgItem(m_hwnd, kctidRestoreOverwriteName));
					return false;
				}
			}
			else
			{
				// Clear off the error information so we don't get an assert later on!
				IErrorInfo * pIErrorInfo = NULL;
				hr = ::GetErrorInfo(0, &pIErrorInfo);
				// New name does not exist as a database
				m_nUserResponse = kCreateNew;
			}
		}
		catch (...)
		{
			m_nUserResponse = kDbReadError;
			return true;
		}
	}
	else // !fRename
			m_nUserResponse = kOverwriteExisting;

	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	User pressed cancel.
----------------------------------------------------------------------------------------------*/
bool RestoreOptionsDlg::OnCancel()
{
	// Confirmation code can go here...

	return SuperClass::OnCancel();
}

/*----------------------------------------------------------------------------------------------
	Handle notifications.
----------------------------------------------------------------------------------------------*/
bool RestoreOptionsDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);
	Assert(m_hwnd);

	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case EN_UPDATE:
		switch (ctid)
		{
		case kctidRestoreOverwriteName:
			// See if there is any text in the edit box:
			{ // Block
				achar rgchT[2]; // We don't need this text
				if (::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteName, WM_GETTEXT, 2,
					(LPARAM)rgchT))
				{
					// User is typing in rename edit box, so adjust the radio buttons:
					::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteReplaceBtn, BM_SETCHECK,
						(WPARAM)BST_UNCHECKED, 0);
					::SendDlgItemMessage(m_hwnd, kctidRestoreOverwriteRenameBtn, BM_SETCHECK,
						(WPARAM)BST_CHECKED, 0);
				}
			} // End block
			return true;
		default:
			return false;
		}
		break;
	case BN_CLICKED:
		switch (ctid)
		{
		case kctidRestoreOverwriteRenameBtn:
			// Make sure the New Name edit box and Overwrite check box are enabled:
			::EnableWindow(::GetDlgItem(m_hwnd, kctidRestoreOverwriteName), true);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidRestoreOverwriteCheck), true);
			return true;
		case kctidRestoreOverwriteReplaceBtn:
			// Make sure the New Name edit box and Overwrite check box are disabled:
			::EnableWindow(::GetDlgItem(m_hwnd, kctidRestoreOverwriteName), false);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidRestoreOverwriteCheck), false);
			return true;
		}
	default:
		return false;
	}

	return false;
}


//:>********************************************************************************************
//:>	BackupMutex methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
	Creates a Mutex uniquely for SIL backups, and checks to see if all was OK.
----------------------------------------------------------------------------------------------*/
BackupMutex::BackupMutex()
{
	m_Status = kOK;
	m_h = CreateMutex(NULL, true, _T("SIL FieldWorks Backup"));

	if (!m_h)
		m_Status = kFailed;
	else if (GetLastError() == ERROR_ALREADY_EXISTS)
		m_Status = kAlreadyExists;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
BackupMutex::~BackupMutex()
{
	Kill();
}


/*----------------------------------------------------------------------------------------------
	Destroy the mutex. This should only be done if there is no chance of doing a backup or
	restore during the rest of the BackupMutex's lifetime.
----------------------------------------------------------------------------------------------*/
void BackupMutex::Kill()
{
	if (m_h)
	{
		CloseHandle(m_h);
		// The mutex is only destroyed once the last handle to it is closed.
		m_h = NULL;
		m_Status = kKilled;
	}
}



//:>********************************************************************************************
//:>	BackupHandler methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
BackupHandler::BackupHandler()
{
	m_pbkupd = NULL;
	m_fInitOk = false;
	m_pFileZipper = new XceedZipBackup();
	m_pZipData = new ZipSystemData();
	m_pbkpprg = NULL;
	GetTempWorkingDirectory(m_strbpTempPath);
	ClearTempWorkingDirectory();
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
BackupHandler::~BackupHandler()
{
	delete m_pFileZipper;
	delete m_pZipData;
}

/*----------------------------------------------------------------------------------------------
	Initialization.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::Init(IBackupDelegates * pbkupd)
{
	m_pbkupd = pbkupd;
	m_bkpi.m_pbkupd = pbkupd;

	m_fws.SetRoot(_T("ProjectBackup"));
	if (!m_bkpi.Init(&m_fws))
		return false;
	BSTR bstrDefaultDir = NULL;
	if (pbkupd)
		pbkupd->GetDefaultBackupDirectory(&bstrDefaultDir);
	m_bkpi.ReadFromRegistry(&m_fws, bstrDefaultDir);

	m_fInitOk = true;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Check if it's time to remind the user to do a backup. The Task Scheduler should take care
	of scheduled backups, but if the machine was switched off, or Task Scheduler was killed,
	then a scheduled backup task may have been missed.
	@param hwnd Handle of parent window for reminder dialog.
----------------------------------------------------------------------------------------------*/
void BackupHandler::CheckForMissedSchedules(HWND hwnd, IHelpTopicProvider * phtprv)
{
	// See how many days have elapsed since last backup:
	int nElapsedDays = GetDaysSinceLastBackup();

	// Assess the user's backup settings:
	if (!m_bkpi.m_bkprmi.m_fTurnOff)
	{
		if (nElapsedDays >= m_bkpi.m_bkprmi.m_nDays && nElapsedDays > 0)
		{
			Remind(nElapsedDays, hwnd, phtprv);
			return;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Compares the time of the last backup, which is stored in the registry, with the current
	time.
----------------------------------------------------------------------------------------------*/
int BackupHandler::GetDaysSinceLastBackup()
{
	// Get the current time:
	SYSTEMTIME systTime;
	GetLocalTime(&systTime);
	// Change format of local time to one we can use to compare with another:
	FILETIME filtTime;
	if (!SystemTimeToFileTime(&systTime, &filtTime))
	{
		// Can't cope with this!
		BackupErrorHandler::ErrorBox(NULL, BackupErrorHandler::kWarning,
			kstidBkpTimeConvertError, GetLastError());
		return 0;
	}

	// Get the time of the last successful backup attempt:
	FILETIME filtBackupTime;
	if (!m_fws.GetBinary(_T("Basics"), _T("LastBackupTime"), (BYTE *)&filtBackupTime, 8))
	{
		// Last backup time not found. Assume this is the first time Explorer has been run, and
		// set 'last backup time' to today:
		m_fws.SetBinary(_T("Basics"), _T("LastBackupTime"), (BYTE *)&filtTime, 8);
		return 0;
	}

	// See how many days have elapsed since last backup:
	int64 nLastBackup = *((int64 *)(&filtBackupTime));
	int64 nTime = *((int64 *)(&filtTime));
	int64 nElapsedTime = nTime - nLastBackup;
	const int64 kn100NanoSecondIntervalsPerDay = (int64)10000000 * (int64)60 * (int64)60 *
		(int64)24;

	return int(nElapsedTime / kn100NanoSecondIntervalsPerDay);
}


/*----------------------------------------------------------------------------------------------
	Perform a backup according to the current settings.
	@param trigger Enumerated type indicating how Backup was triggered.
	@param hwnd Handle to be used as parent window for a dialog.
	@param phtprov pointer to a help topic provider to get app-specific information

	@return True if backup was completed successfully.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::Backup(VTrigger trigger, HWND hwnd, IHelpTopicProvider * phtprov)
{
	// First, make sure that no other process or thread is in the middle of a backup:
	BackupMutex bkpm;
	if (!CheckMutex(&bkpm, BackupErrorHandler::kBackupFailure))
		return false;

	if (!m_bkpi.m_vprojd.Size())
		m_bkpi.ReadDbProjectsFromRegistry(&m_fws);

	LogFile log;
	log.Start(trigger);
	bool fBackupOK = false;
	bool fFinished = false;
	bool fAuto = TryAutomaticBackup(trigger, hwnd, phtprov, &bkpm, log, fFinished, fBackupOK);
	if (fFinished)
		return fBackupOK;

	// Get a time and date stamp for this backup:
	SYSTEMTIME systTime;
	GetLocalTime(&systTime);
	Assert(systTime.wMonth >= 1 && systTime.wMonth <= 12);
	// Turn on the hourglass (wait) cursor.
	WaitCursor wc;
	// Create synchronization event, so that if user presses abort button in progress dialog, we
	// can suspend backup until they have either confirmed or rejected their decision.
	HANDLE hEvent = ::CreateEvent(NULL, true, true, NULL);
	// Now get on with backing up:
	bool fBackupOk = true;
	IStreamPtr qfist;
	for (int iProject = 0; fBackupOk && iProject < m_bkpi.m_vprojd.Size(); iProject++)
	{
		BackupInfo::ProjectData & projd = m_bkpi.m_vprojd[iProject];
		StrUni stuName(projd.m_stuDatabase);
		//StrUni stuName(projd.m_stuProject);
		//if (!projd.m_stuProject.EqualsCI(projd.m_stuDatabase))
		//	stuName.FormatAppend(L" (%s)", projd.m_stuDatabase.Chars());
		StrApp str(stuName.Chars());
		log.Write(str.Chars());
		log.Write(_T(": "));
		if (projd.m_fBackup)
		{
			fBackupOk = BackupProject(projd, stuName, hEvent, systTime, log);
		}
		else if (fAuto)
		{
			log.Write(_T("not changed since last backup."));
		}
		else
		{
			log.Write(_T("not selected for backup."));
		}
		log.Write(_T("\n"));
	}
	// Let's clear the database connections here just to be safe. (LT-2629)
	m_qodc.Clear();		// must be cleared before clearing m_qode
	m_qode.Clear();
	// Don't need synchronization event any more, as progress dialog has gone out of scope:
	::CloseHandle(hEvent);
	hEvent = NULL;

	FILETIME filtTime;
	if (fBackupOk)
	{
		// Save backup time of each database:
		m_bkpi.WriteDbProjectsToRegistry(&m_fws);
		if (SystemTimeToFileTime(&systTime, &filtTime))
		{
			// Store the time of the overall backup attempt:
			m_fws.SetBinary(_T("Basics"), _T("LastBackupTime"), (BYTE *)&filtTime, 8);
		}
		return true;
	}
	else
	{
		log.Write(_T("Backup was not completed.\n"));
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Check that no other backups are in progress.  This is called from Backup().

	@param pmut - pointer to a BackupMutex object created by the Backup() method.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::CheckMutex(BackupMutex * pmut, int nErrCode)
{
	BackupMutex::VStatus vstat = pmut->GetStatus();
	switch (vstat)
	{
	case BackupMutex::kOK:
		return true;
	case BackupMutex::kFailed:
		// System error:
		BackupErrorHandler::ErrorBox(NULL, nErrCode, kstidBkpMutexError, GetLastError());
		return false;
	case BackupMutex::kAlreadyExists:
		if (nErrCode == BackupErrorHandler::kRestoreFailure)
			BackupErrorHandler::ErrorBox(NULL, nErrCode, kstidRstMutexError);
		return false;					// Backup is already happening.
	case BackupMutex::kKilled:
		Assert(vstat != BackupMutex::kKilled);		// This is a runtime error:
		return false;
	default:
		Assert(vstat == BackupMutex::kOK);			// This is a programmer error:
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Check for an automatic backup.  This is called from Backup().

	@param trigger - Enumerated type indicating how Backup was triggered.
	@param hwnd - Handle to be used as parent window for a dialog.
	@param phtprov - pointer to a help topic provider to get app-specific information
	@param pmut - pointer to a BackupMutex object created by the Backup() method.
	@param log - reference to the log file object
	@param fFinished - reference to flag whether no more processing is needed
	@param fBackupOK - reference to the value to return from Backup().
----------------------------------------------------------------------------------------------*/
bool BackupHandler::TryAutomaticBackup(VTrigger trigger, HWND hwnd,
	IHelpTopicProvider * phtprov, BackupMutex * pmut, LogFile & log, bool & fFinished,
	bool & fBackupOK)
{
	bool fAuto = false;
	if (trigger == kExternal)
		fAuto = true;

	// If the fAuto flag is set, then check which databases need backing up, and give the user
	// a 30-second warning:
	if (fAuto)
	{
		int nTotalSelected = 0;
		// If the trigger was an accepted reminder, then we already know which databases need
		// backing up:
		if (trigger == kReminderAccepted)
		{
			for (int i = 0; i<m_bkpi.m_vprojd.Size(); i++)
			{
				if (m_bkpi.m_vprojd[i].m_fBackup)
					nTotalSelected++;
			}
		}
		else
		{
			// See which databases need backing up:
			if (!m_bkpi.AutoSelectBackupProjects(nTotalSelected))
			{
				log.Write(
					_T("Could not determine which projects have changed since last backup.\n"));
				fBackupOK = false;
				fFinished = true;
				return fAuto;
			}
		}
		if (nTotalSelected == 0)
		{
			log.Write(_T("No projects have changed since their last backup.\n"));
			fBackupOK = true; // Signal all was well - we just didn't need to do anything.
			fFinished = true;
			return fAuto;
		}
		switch (ScheduledBackupWarningDlg::ScheduledBackupWarning(30, NULL,
			m_bkpi.m_bkppwi.m_fLock))
		{
		case ScheduledBackupWarningDlg::kCancel:
			log.Write(_T("User canceled during 30 second countdown.\n"));
			fBackupOK = false;
			fFinished = true;
			return fAuto;

		case ScheduledBackupWarningDlg::kOptions:
			log.Write(_T("User decided to reconfigure during 30 second countdown.\n"));
			log.Terminate();
			pmut->Kill();
			fBackupOK = (UserConfigure(hwnd, false, phtprov) == BackupHandler::kBackupOk);
			fFinished = true;
			return fAuto;

		case ScheduledBackupWarningDlg::kStartNow:
			// No special treatment:
			break;

		default:
			Assert(false);
			break;
		}
		log.TimeStamp();
		log.Write(_T(" Countdown completed.\n"));
	}
	CheckBackupPath(ValidateBackupPath(!fAuto), log, fFinished, fBackupOK);
	return fAuto;
}

/*----------------------------------------------------------------------------------------------
	Perform a backup on one project.

	@param projd - reference to a ProjectData object
	@param stuName - name like "Kalaba (TestLangProj)" for the project
	@param hEvent - handle used to prevent multiple instances, even from different apps.
	@param systTime - current time used for backup timestamp
	@param log - reference to the log file object
	@param fTesting - flag whether we're testing the code or really using it (defaults to false)
----------------------------------------------------------------------------------------------*/
bool BackupHandler::BackupProject(BackupInfo::ProjectData & projd, StrUni & stuName,
	HANDLE hEvent, SYSTEMTIME systTime, LogFile & log, bool fTesting)
{
	// Set up a progress dialog:
	BackupProgressDlg bkpprg(hEvent);
	m_pbkpprg = &bkpprg;
	if (!m_bkpi.m_fXml)
		bkpprg.OmitActivity(1);
	bkpprg.ShowDialog();
	bkpprg.SetProjectName(stuName);
	if (fTesting)
		bkpprg.EnableAbortButton(false);	// don't want tests cancelled halfway!

	// Save all of the applications' main windows connected to this database
	if (m_pbkupd)
	{
		CheckHr(m_pbkupd->SaveAllData_Bkupd(s_stuLocalServer.Chars(),
			projd.m_stuDatabase.Chars()));
	}
	if (!ConnectToMasterDb())
	{
		BackupErrorHandler::ErrorBox(bkpprg.Hwnd(), BackupErrorHandler::kBackupFailure,
			kstidBkpMasterDbError);
		log.Write(kstidBkpMasterDbError);
		return false;
	}
	m_stuTargetDatabase = projd.m_stuDatabase;
	StrUni stuIntermediateFile;
	StrUni stuXmlFile;
	CreateBackupFileNames(m_stuCurrentZipFile, stuIntermediateFile, stuXmlFile, projd);
	bkpprg.SetActivityNumber(0);
	bool fBackupOk = GenerateDbBackupFile(log, stuIntermediateFile, hEvent);

	// Check if we also have to backup an XML version:
	if (fBackupOk && m_bkpi.m_fXml)
	{
		bkpprg.SetActivityNumber(1);
		fBackupOk = GenerateXmlBackupFile(log, stuXmlFile, hEvent, projd);
	}
	// Initialize zip system data:
	if (fBackupOk)
	{
		bkpprg.SetActivityNumber(2); // Update progress dialog
		fBackupOk = InitializeFileZipper();
	}
	if (fBackupOk)
	{
		fBackupOk = ZipBackupFiles(log, m_stuCurrentZipFile, stuIntermediateFile, stuXmlFile,
			hEvent);
	}
	// Remove temporary files. It does not matter if an error occurs - this is likely
	// if user aborted backup before files were generated:
	::DeleteFileW(stuIntermediateFile.Chars());
	if (m_bkpi.m_fXml)
		::DeleteFileW(stuXmlFile.Chars());

	if (fBackupOk && !fTesting)
	{
		RecordBackupTime(projd, systTime);
		bkpprg.SetActivityNumber(3);	// Signal on the progress dialog that we're done.
		log.Write(_T("successfully backed up as \""));
		StrApp str(m_stuCurrentZipFile.Chars());
		log.Write(str.Chars());
		log.Write(_T(")\""));
	}
	return fBackupOk;
}

/*----------------------------------------------------------------------------------------------
	Create the needed filenames involved with backing up the given project.  Delete the
	intermediate (.bak) and XML files if they exist.

	@param stuZipFile - reference to Zip filename for output
	@param stuIntermediateFile - reference to intermediate (.bak) filename for output
	@param stuXmlFile - reference to XML filename for output
	@param projd - reference to the current project being backed up
----------------------------------------------------------------------------------------------*/
void BackupHandler::CreateBackupFileNames(StrUni & stuZipFile, StrUni & stuIntermediateFile,
	StrUni & stuXmlFile, BackupInfo::ProjectData & projd)
{
	// Make up zip file name. This consists of the name of the language project plus the
	// date and time. E.g. "Indonesian 2001-10-02 1718.zip"
	BackupFileNameProcessor::GenerateFileName(m_bkpi.m_strbDirectoryPath,
		projd.m_stuProject, projd.m_stuDatabase, stuZipFile);

	// Make temporary file names for MSDE to dump to:
	stuIntermediateFile.Assign(m_strbpTempPath.Chars());
	stuIntermediateFile.Append(m_stuTargetDatabase.Chars());
	stuIntermediateFile.Append(_T(".bak"));

	stuXmlFile.Assign(m_strbpTempPath.Chars());
	stuXmlFile.Append(m_stuTargetDatabase.Chars());
	stuXmlFile.Append(".xml");

	// Make sure the temporary files do not exist already:
	::DeleteFileW(stuIntermediateFile.Chars());
	::DeleteFileW(stuXmlFile.Chars());
}

/*----------------------------------------------------------------------------------------------
	Getting MSDE to write the BAK file is a task that gives us no feedback until complete, yet
	we need to periodically update our progress indicator. To do this, we will use the Backup
	Dialog's timer capability, running on our other thread. So now we need to estimate how long
	it will take MSDE to write the file. This is done by determining the size of the database
	(mdf) file, and using the 'write rate' value recorded last time, when the writing was
	actually timed. If this is the first time we're writing the file, we will have to make a
	rough guess.
	@return estimated time (in seconds) for backing up the database file
----------------------------------------------------------------------------------------------*/
int BackupHandler::GuessBackupTimeRequired()
{
	// Try to work out the size of the database file:
	m_nDbFileSize = 10000000; // Set default guess to ten million bytes.

	// Get MSDE to tell us which file the database uses:
	StrAppBufPath strbpDbFile;
	HRESULT hr;
	CheckHr(hr = m_qode->CreateCommand(&m_qodc));
	if (GetMsdeFileName(strbpDbFile))
	{
		// Find the size of the file:
		WIN32_FIND_DATA wfd;
		HANDLE hFind;
		hFind = ::FindFirstFile(strbpDbFile.Chars(), &wfd);
		if (hFind != INVALID_HANDLE_VALUE)
		{
			m_nDbFileSize = (wfd.nFileSizeHigh * MAXDWORD) + wfd.nFileSizeLow;
			::FindClose(hFind);
		}
	}
	// Doesn't matter if we couldn't find the file, we'll guess the time needed.

	// Now estimate how long it will take to write the file, knowing its length.
	int nEstimatedTime = 120; // Guess two minutes as a default.
	int nDbWriteRate = 1700000; // About right for my machine (AlistairI)
	// See if any previous attempts' values were recorded:
	m_fws.GetBinary(_T("Basics"), _T("DbWriteRate"), (BYTE *)&nDbWriteRate, 4);
	if (nDbWriteRate > 0)
		nEstimatedTime = (int)(m_nDbFileSize / nDbWriteRate);

	return nEstimatedTime;
}

/*----------------------------------------------------------------------------------------------
	Generate the actual database backup (.bak) file via SQL.

	@param log - reference to the log file object
	@param stuIntermediateFile - reference to intermediate (.bak) filename to generate
	@param hEvent - handle used to prevent multiple instances, even from different apps.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::GenerateDbBackupFile(LogFile & log, const StrUni & stuIntermediateFile,
	HANDLE hEvent)
{
	bool fBackupOk = true;

	m_pbkpprg->SetActivityEstimatedTime(GuessBackupTimeRequired());

	// Make a note of the time:
	SYSTEMTIME systCurrentTime;
	GetLocalTime(&systCurrentTime);
	FILETIME filtCurrentTime;
	SystemTimeToFileTime(&systCurrentTime, &filtCurrentTime);
	int64 nStartTime = *((int64 *)(&filtCurrentTime));
	HRESULT hr;

	try
	{
		// Get MSDE to produce backup file:
		StrUni stuSql = L"BACKUP DATABASE ? TO DISK = ?";
		CheckHr(hr = m_qode->CreateCommand(&m_qodc));
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)m_stuTargetDatabase.Chars(),
			m_stuTargetDatabase.Length() * isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuIntermediateFile.Chars(),
			stuIntermediateFile.Length() * isizeof(OLECHAR)));
		CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	}
	catch (...)
	{
		BackupErrorHandler::ErrorBox(m_pbkpprg->Hwnd(),
			BackupErrorHandler::kBackupFailure, kstidBkpDbSaveError);
		log.Write(kstidBkpDbSaveError);
		fBackupOk = false;
	}
	if (fBackupOk)
	{
		// Make a note of the time:
		GetLocalTime(&systCurrentTime);
		SystemTimeToFileTime(&systCurrentTime, &filtCurrentTime);
		int64 nEndTime = *((int64 *)(&filtCurrentTime));

		// Calculate duration of file writing:
		m_nBakFileWriteDuration = (int)((nEndTime - nStartTime) / kn100NanoSeconds);
		// On fast machines, this activity can take less than 1 second, so fudge:
		if (m_nBakFileWriteDuration == 0)
			m_nBakFileWriteDuration = 1;

		// Record actual rate of writing:
		if (m_nBakFileWriteDuration > 0)
		{
			int nDbWriteRate = (int)(m_nDbFileSize / m_nBakFileWriteDuration);
			m_fws.SetBinary(_T("Basics"), _T("DbWriteRate"), (BYTE *)&nDbWriteRate,
				4);
		}
	}

	// Suspend if other thread is awaiting user to confirm abort:
	::WaitForSingleObject(hEvent, INFINITE);
	if (m_pbkpprg->GetAbortedFlag())
	{
		log.Write(_T("user aborted while MSDE was writing BAK file."));
		fBackupOk = false;
	}
	return fBackupOk;
}

/*----------------------------------------------------------------------------------------------
	Connect to the "master" database of the local server.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::ConnectToMasterDb()
{
	try
	{
		StrUni stuServerName = s_stuLocalServer;
		StrUni stuMasterDB = L"master";
		m_qodc.Clear();		// must be released before releasing m_qode (LT-2629).
		m_qode.Clear();		// might as well release explicitly here.
		m_qode.CreateInstance(CLSID_OleDbEncap);
		// Get the IStream pointer for logging. NULL returned if no log file.
		HRESULT hr;
		CheckHr(hr = m_qode->Init(stuServerName.Bstr(), stuMasterDB.Bstr(), s_qfist, koltMsgBox,
			koltvForever));
		return true;
	}
	catch (...)
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Generate the XML database backup.

	@param log - reference to the log file object
	@param stuXmlFile - reference to the XML filename to generate
	@param hEvent - handle used to prevent multiple instances, even from different apps.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::GenerateXmlBackupFile(LogFile & log, const StrUni & stuXmlFile,
	HANDLE hEvent, BackupInfo::ProjectData & projd)
{
	int fBackupOk = true;
	// To estimate the time for writing the XML file, we will use the time taken to
	// Write the equivalent BAK file, multiplied by a factor found to be accurate
	// last time (or a guess if first time).
	float nBakToXmlFactor = 4.0; // About right for my machine (AlistairI)
	// See if any previous attempts' values were recorded:
	m_fws.GetBinary(_T("Basics"), _T("BakToXmlFactor"), (BYTE *)&nBakToXmlFactor,
		4);
	int nEstimatedTime = (int)(m_nBakFileWriteDuration * nBakToXmlFactor);
	m_pbkpprg->SetActivityEstimatedTime(nEstimatedTime);
	// Make a note of the time:
	SYSTEMTIME systCurrentTime;
	GetLocalTime(&systCurrentTime);
	FILETIME filtCurrentTime;
	SystemTimeToFileTime(&systCurrentTime, &filtCurrentTime);
	int64 nStartTime = *((int64 *)(&filtCurrentTime));
	try
	{
		// Get a pointer to the database, and thence to the writing system factory.
		HRESULT hr;
		IOleDbEncapPtr qode;
		qode.CreateInstance(CLSID_OleDbEncap);
		CheckHr(hr = qode->Init(s_stuLocalServer.Bstr(), projd.m_stuDatabase.Bstr(),
			s_qfist, koltMsgBox, koltvForever));
		ILgWritingSystemFactoryBuilderPtr qwsfb;
		qwsfb.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
		ILgWritingSystemFactoryPtr qwsf;
		CheckHr(hr = qwsfb->GetWritingSystemFactory(qode, s_qfist, &qwsf));
		// Save XML version of database:
		IFwXmlDataPtr qfwxd;
		qfwxd.CreateInstance(CLSID_FwXmlData, CLSCTX_INPROC_SERVER);
		CheckHr(hr = qfwxd->Open(s_stuLocalServer.Bstr(), projd.m_stuDatabase.Bstr()));
		// REVIEW: Do we want to instantiate a progress report object that supports
		// the IAdvInd interface?
		CheckHr(hr = qfwxd->SaveXml(stuXmlFile.Bstr(), qwsf, NULL));
		qfwxd->Close();
		qfwxd.Clear();
		qwsf->Shutdown();
		qwsf.Clear();
		qwsfb.Clear();
		qode.Clear();
	}
	catch (...)
	{
		BackupErrorHandler::ErrorBox(m_pbkpprg->Hwnd(),
			BackupErrorHandler::kBackupNonFatalFailure, kstidBkpDbXmlError);
		log.Write(kstidBkpDbXmlError);
	}
	// Make a note of the time and calculate duration of file writing.
	GetLocalTime(&systCurrentTime);
	SystemTimeToFileTime(&systCurrentTime, &filtCurrentTime);
	int64 nEndTime = *((int64 *)(&filtCurrentTime));
	int nDuration = (int)((nEndTime - nStartTime) / kn100NanoSeconds);
	// On fast machines, this activity can take less than 1 second, so fudge:
	if (nDuration == 0)
		nDuration = 1;

	// Record actual BAK to XML factor:
	if (m_nBakFileWriteDuration > 0)
	{
		nBakToXmlFactor = (float)nDuration / (float)m_nBakFileWriteDuration;
		m_fws.SetBinary(_T("Basics"), _T("BakToXmlFactor"),
			(BYTE *)&nBakToXmlFactor, 4);
	}
	// Suspend if other thread is awaiting user to confirm abort:
	::WaitForSingleObject(hEvent, INFINITE);
	if (m_pbkpprg->GetAbortedFlag())
	{
		log.Write(_T("user aborted during XML dump."));
		fBackupOk = false;
	}
	return fBackupOk;
}

/*----------------------------------------------------------------------------------------------
	Zip up the generated backup files.

	@param log - reference to the log file object
	@param stuZipFile - reference to the Zip filename to generate
	@param hEvent - handle used to prevent multiple instances, even from different apps.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::ZipBackupFiles(LogFile & log, const StrUni & stuZipFile,
	const StrUni & stuIntermediateFile, const StrUni & stuXmlFile, HANDLE hEvent)
{
	bool fBackupOk = true;
	try
	{
		// Set up parameters for zip file:
		SmartBstr sbstr;
		stuZipFile.GetBstr(&sbstr);
		m_pFileZipper->SetZipFilename(sbstr);
		// Make sure zip file doesn't already exist, else zip may not do anything:
		::DeleteFileW(stuZipFile.Chars());
		if (m_bkpi.m_bkppwi.m_fLock)
		{
			m_bkpi.m_bkppwi.m_strbPassword.GetBstr(&sbstr);
			m_pFileZipper->SetEncryptionPassword(sbstr);
		}
		m_pFileZipper->SetPreservePaths(false);
		m_pFileZipper->SetProcessSubfolders(false);
		stuIntermediateFile.GetBstr(&sbstr);
		m_pFileZipper->SetFilesToProcess(sbstr);
		if (m_bkpi.m_fXml)
		{
			stuXmlFile.GetBstr(&sbstr);
			m_pFileZipper->AddFilesToProcess(sbstr);
		}
		m_pFileZipper->SetCompressionLevel(9); // Maximum compression (slowest)
		// Disable disk-spanning, else the volume label will be overwritten, which annoys
		// USB memory stick owners:
		m_pFileZipper->SetSpanMultipleDisks(0);
		m_pFileZipper->SetUseTempFile(false); // Allows more accurate progress bar

		// IMPORTANT NOTE: the following call may well result in an access violation
		// error. It appears that this can be ignored: simply press F5 again, and
		// agree to pass the exception to the program. (The error only occurs when
		// in VS.)
		long nResult = m_pFileZipper->Zip();

		// Suspend if other thread is awaiting user to confirm abort:
		::WaitForSingleObject(hEvent, INFINITE);
		if (m_pbkpprg->GetAbortedFlag())
		{
			log.Write(_T("user aborted while zip file was being written."));
			fBackupOk = false;
		}
		else
		{
			// Test to see if there was an error:
			fBackupOk = !m_pFileZipper->TestError(nResult, stuZipFile.Chars());
			if (!fBackupOk)
				log.Write(_T("error while writing zip file."));
		}
		// ENHANCE: Consider doing an unzipped backup for users without the Xceed
		// Zip module.
	}
	catch (...)
	{
		BackupErrorHandler::ErrorBox(m_pbkpprg->Hwnd(),
			BackupErrorHandler::kBackupFailure, kstidBkpZipError);
		log.Write(kstidBkpZipError);
		fBackupOk = false;
	}
	return fBackupOk;
}

/*----------------------------------------------------------------------------------------------
	Store the time of this backup, to maintain the integrity of the reminder system.

	@param projd - reference to the current project being backed up
	@param systTime - current time used for backup timestamp
----------------------------------------------------------------------------------------------*/
void BackupHandler::RecordBackupTime(BackupInfo::ProjectData & projd, SYSTEMTIME systTime)
{
	StrApp strSubKey(projd.m_stuDatabase.Chars());
	//StrApp strSubKey;
	//StrApp strProject(projd.m_stuProject.Chars());
	//StrApp strDatabase(projd.m_stuDatabase.Chars());
	//strSubKey.Format(_T("Projects\\%s(%s)"),
	//	strProject.Chars(), strDatabase.Chars());

	FILETIME filtTime;
	if (SystemTimeToFileTime(&systTime, &filtTime))
	{
		// Write value to registry:
		m_fws.SetBinary(strSubKey.Chars(), _T("LastBackupTime"),
			(BYTE *)(&filtTime), 8);
		projd.m_filtLastBackup = filtTime;
	}
}

/*----------------------------------------------------------------------------------------------
	Process the results from ValidateBackupPath().

	@param nValidated - results from ValidateBackupPath().
	@param log - pointer to the LogFile object.
	@param fFinished - reference to flag whether no more processing is needed
	@param fBackupOK - reference to the value to return from Backup().
----------------------------------------------------------------------------------------------*/
void BackupHandler::CheckBackupPath(int nValidated, LogFile & log, bool & fFinished,
	bool & fBackupOK)
{
	switch (nValidated)
	{
	case kDirectoryAlreadyExisted:
		fFinished = false;
		break;

	case kUserQuit:
		log.Write(_T("User opted to quit instead of creating destination path.\n"));
		fBackupOK = false;
		fFinished = true;
		break;

	case kCreationSucceeded:
		log.Write(_T("Successfully created destination path: "));
		log.Write(m_bkpi.m_strbDirectoryPath.Chars());
		log.Write(_T("\n"));
		fFinished = false;
		break;

	case kCreationFailed:
		log.Write(_T("Failed to create destination path: "));
		log.Write(m_bkpi.m_strbDirectoryPath.Chars());
		log.Write(_T("\n"));
		fBackupOK = false;
		fFinished = true;
		break;

	default:
		Assert(false);
		fBackupOK = false;
		fFinished = true;
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Remind the user that it's time to back up.
	@param nElapsedDays Number of days elapsed since last backup, or -1 if not yet known.
	@param hwnd Handle of parent window for reminder dialog.
	@param phtprov pointer to a help topic provider to get app-specific information
----------------------------------------------------------------------------------------------*/
void BackupHandler::Remind(int nElapsedDays, HWND hwnd, IHelpTopicProvider * phtprov)
{
	// See if a backup is already happening:
	{ // Block
		BackupMutex bkpm;
		if (!CheckMutex(&bkpm, BackupErrorHandler::kWarning))
			return;
	} // End block, Mutex goes out of scope and is cleared.


	// Check if the nElapsedDays argument was set by caller:
	if (nElapsedDays == -1)
		// Not properly set, so calculate value for ourselves:
		nElapsedDays = GetDaysSinceLastBackup();

	// See if any projects need backing up:
	int nTotalAltered = 0;
	if (m_bkpi.AutoSelectBackupProjects(nTotalAltered))
	{
		if (nElapsedDays > 0 && nTotalAltered > 0)
		{
			switch (BackupNagDlg::BackupNag(nElapsedDays, m_bkpi.m_bkppwi.m_fLock, hwnd, phtprov))
			{
			case BackupNagDlg::kDoBackup:
				if (!Backup(kReminderAccepted, NULL, phtprov))
					UserConfigure(hwnd, false, phtprov);
				break;
			case BackupNagDlg::kCancel:
				// Do nothing
				break;
			case BackupNagDlg::kConfigure:
				UserConfigure(hwnd, false, phtprov);
				break;
			default:
				Assert(false);
				break;
			}
		}
	}
	else
	{
		LogFile log;
		log.TimeStamp();
		log.Write(
			_T(" Backup reminder could not determine which projects have changed since last ")
			_T("backup.\n\n"));
	}
}

/*----------------------------------------------------------------------------------------------
	Perform a restore according to the current settings.
	@return True if all went OK.
----------------------------------------------------------------------------------------------*/
int BackupHandler::Restore()
{
	WaitCursor wc;		// Turn on the hourglass (wait) cursor.
	int nResult = 0;

	// Make sure that no other process or thread is in the middle of a backup:
	BackupMutex bkpm;
	if (!CheckMutex(&bkpm, BackupErrorHandler::kRestoreFailure))
		return kRestoreFail;

	// Flag to record if we fiddle count of windows:
	bool fExpObjInc = false;
	// Flag to record that we successfully moved the original database to the working directory:
	bool fDbPreserved = false;
	// Flag to record if database disconnection failed:
	bool fDbDisconnectionFailed = false;
	// Flag to record if a window had to close in order to restore:
	ComBool fClosedWindow = false;
	// Deal with original (now cached) database:
	bool fRecovered = false;
	// Flag indicating that Restore was successful;
	bool fRestoredDb = false;

	// Create synchronization event, so that if user presses abort button in progress dialog, we
	// can suspend restore until they have either confirmed or rejected their decision.
	HANDLE hEvent = ::CreateEvent(NULL, true, true, NULL);

	nResult = Restore(hEvent, fClosedWindow, fRecovered, fDbDisconnectionFailed, fDbPreserved,
		fExpObjInc);
	::CloseHandle(hEvent);  //Clean up the synchronization event

	fRestoredDb = (nResult == 1);

	// If restore failed, attempt to recover:
	if(!fRestoredDb && fDbPreserved)
		fRecovered = RecoverFromFailedRestore();

	// Was database restored successfully OR was a project window closed that is recoverable?
	if (fRestoredDb ||
		(fClosedWindow && (fRecovered || fDbDisconnectionFailed || !fDbPreserved)))
	{
		// Open and connect a window to the database:
		if (m_pbkupd)
			CheckHr(m_pbkupd->ReopenDbAndOneWindow_Bkupd(s_stuLocalServer.Chars(),
				m_stuTargetDatabase.Chars()));
	}
	else
	{
		// TODO: Inform user that we were not able to recover from a canceled or failed restore.
		if (nResult == kstidMustBeAdmin)
		{
			BackupErrorHandler::ErrorBox(NULL, BackupErrorHandler::kWarning, nResult);
		}
		else if (nResult == kstidCantWriteToRestore)
		{
			BackupErrorHandler::ErrorBox(NULL, BackupErrorHandler::kWarning, nResult, 0,
				m_strbpTargetDir.Chars());
		}
		// Inform user of a Restore failure with indication of problem.
		else if (nResult != 0)
		{
			BackupErrorHandler::ErrorBox(NULL, BackupErrorHandler::kRestoreFailure,
				nResult);
		}
	}
	if (fExpObjInc)
	{
		// Reopening the window will add a new count to our exported objects, so we can now
		// clear our temporary hold on the application. Or, if we failed to do a restore,
		// we can't leave the count out of synch forever, or there will be a problem closing
		// down at the end of the application process's life.
		if (m_pbkupd)
			CheckHr(m_pbkupd->DecExportedObjects_Bkupd());
	}
	return fRestoredDb ? kRestoreOk : kRestoreFail;
}

/*----------------------------------------------------------------------------------------------
	Perform a restore according to the current settings.
	@return 1 if all went OK, 0 if user canceled, else error message code.

	@param hEvent - handle used to prevent multiple instances, even from different apps.
	@param fClosedWindow - flag that a window connected to the db was closed (output)
	@param fRecovered
	@param fDbDisconnectionFailed - flag that disconnecting the db failed (output)
	@param fDbPreserved - flag that the db was successfully preserved (output)
	@param fExpObjInc - flag that the count of exported objects was incremented (output)
----------------------------------------------------------------------------------------------*/
int BackupHandler::Restore(HANDLE & hEvent, ComBool & fClosedWindow, bool & fRecovered,
	bool & fDbDisconnectionFailed, bool & fDbPreserved, bool & fExpObjInc)
{
	// Instantiate the progress dialog so that the zip data be initialized.
	// Don't display the progress dialog yet. We have some checking to do first.
	BackupProgressDlg bkpprg(hEvent, true);
	m_pbkpprg = &bkpprg;

	// Initialize zip system.
	int nErrorResult = InitializeFileZipper();
	if (nErrorResult != 1)
		return nErrorResult;

	// Connect to master database, ready to test if database to be restored already exists:
	if (!ConnectToMasterDb())
		return kstidBkpMasterDbError;

	int nRestoreOptions;
	StrUni stuNewDatabaseName;
	StrUni stuSource; // Full path of unzipped file
	StrUni stuSourceXml;
	nErrorResult = GenerateRestoreNames(nRestoreOptions, stuNewDatabaseName, stuSource,
		stuSourceXml);
	if (nErrorResult != 1)
		return nErrorResult;

	bool fDbAlreadyExists = false;
	nErrorResult = SetupWithRestoreOptions(nRestoreOptions, stuNewDatabaseName,
		fDbAlreadyExists);
	if (nErrorResult != 1)
		return nErrorResult;

	// Ready to go with main Restore process if everything is ok
	bool fRestoreFromXml = false;
	nErrorResult = UnzipForRestore(hEvent, stuSource, stuSourceXml, fRestoreFromXml);
	if (nErrorResult != 1)
		return nErrorResult;

	//Do a backup of an existing database before overwriting with Restore.
	nErrorResult = BackupForRestore(fDbAlreadyExists);
	if (nErrorResult != 1)
		return nErrorResult;

	//Arrange database files for the actual restore:
	nErrorResult = DisconnectDatabase(fDbAlreadyExists, fDbDisconnectionFailed,
		fClosedWindow, fDbPreserved, fExpObjInc);
	if (nErrorResult != 1)
		return nErrorResult;
	if (fRestoreFromXml)
		nErrorResult = RestoreFromXml(stuSourceXml);
	else
		nErrorResult = RestoreFromBak(stuSource, nRestoreOptions);
	if (nErrorResult != 1)
		return nErrorResult;

	bkpprg.SetActivityNumber(3); // Update progress dialog to check off Restore step

	return FinishRestoring(hEvent, fDbPreserved);
}

/*----------------------------------------------------------------------------------------------
	Generate the filenames involved with the current restore operation.

	@param nRestoreOptions - reference to options word obtained from dialog (output)
	@param stuNewDatabaseName - reference to the new database name (output)
	@param stuSource - reference to the backup (.bak) filename (output)
	@param stuSourceXml - reference to the XML backup filename (output)
	@return 1 if all went OK, 0 if user canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::GenerateRestoreNames(int & nRestoreOptions, StrUni & stuNewDatabaseName,
	StrUni & stuSource, StrUni & stuSourceXml)
{
	// Get or generate the source path and file names.

	// Get the name of the zipped backup file that is the source for the restore.
	StrAppBufPath strbpZipFileName(m_bkpi.m_strZipFileName.Chars());
	// Generate the project name
	StrApp strProjectName;
	if (!BackupFileNameProcessor::GetProjectName(strbpZipFileName, strProjectName))
	{
		return kstidRstNoProjectName;
	}
	m_stuProjectName.Assign(strProjectName.Chars());
	// Get the root file name of the original database from header file in the zip file
	// or from the zip file name
	StrUni stuOriginalDatabase;
	int nErrorResult = GetOriginalDbName(strbpZipFileName, stuOriginalDatabase);
	if (nErrorResult != 1)
	{
		return nErrorResult;
	}

	// Make some more source file and path names
	m_stuTargetDatabase.Assign(stuOriginalDatabase.Chars());
	nErrorResult = GetSourceFileStrings(stuOriginalDatabase, stuSource, stuSourceXml);
	if (nErrorResult != 1)
	{
		return nErrorResult;
	}
	//
	// Get or generate the target path and file names.
	//
	// Test for existing database, get options for handling existing database
	// Check if target database already exists, and how user wants to proceed if so:
	nRestoreOptions = RestoreOptionsDlg::DbClashAction(GetActiveWindow(),
		stuOriginalDatabase, m_stuProjectName, stuNewDatabaseName);
	if (nRestoreOptions == RestoreOptionsDlg::kUserCancel)
	{
		return 0;
	}
	return 1;
}

/*----------------------------------------------------------------------------------------------
	Show the progress dialog, setting various of its values, and derive the new database name
	along with its actual filenames.

	@param nRestoreOptions - options word returned from restore options dialog
	@param stuNewDatabaseName - name of the new database being created by the restore operation
	@param fDbAlreadyExists - flag that a database of that name already exists (output)
	@return 1 if all went OK, 0 if user canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::SetupWithRestoreOptions(int nRestoreOptions,
	const StrUni & stuNewDatabaseName, bool & fDbAlreadyExists)
{
	// Display progress dialog, configured for restore:
	m_pbkpprg->ShowDialog();
	m_pbkpprg->SetProjectName(m_bkpi.m_strProjectFullName.Chars());
	m_pbkpprg->SetActivityNumber(0);
	StrUni stuNewProjectFullName;
	stuNewProjectFullName.Format(_T("%s (%s)"),
		m_stuProjectName.Chars(), stuNewDatabaseName.Chars());

	//Determine actions based on response to the Restore Options dialog.
	switch (nRestoreOptions)
	{
	case RestoreOptionsDlg::kNoDbClash:
		// No action needed:
		break;

	case RestoreOptionsDlg::kCreateNew:
		// Make sure database name is same as user's new project name:
		m_stuTargetDatabase.Assign(stuNewDatabaseName.Chars());
		m_pbkpprg->SetProjectName(stuNewProjectFullName);
		break;

	case RestoreOptionsDlg::kOverwriteExisting:
		fDbAlreadyExists = true;
		break;

	case RestoreOptionsDlg::kOverwriteOther:
		// Make sure database name is same as user's new project name:
		m_stuTargetDatabase.Assign(stuNewDatabaseName.Chars());
		m_pbkpprg->SetProjectName(stuNewProjectFullName);
		fDbAlreadyExists = true;
		break;

	case RestoreOptionsDlg::kDbReadError:
		return kstidBkpMasterDbError;

	default:
		Assert(nRestoreOptions == RestoreOptionsDlg::kNoDbClash);	// should never happen!
		return 0;
	}
	// Set up some needed Target file strings.  This also checks that the target data directory
	// is writable.
	return GetTargetFileStrings(m_stuTargetMdf, m_stuTargetLdf);
}

/*----------------------------------------------------------------------------------------------
	Unzip the backup file for restore.

	@param hEvent - handle used to prevent multiple instances, even from different apps.
	@param stuSource - reference to the backup (.bak) filename
	@param stuSourceXml - reference to the XML backup filename
	@param fRestoreFromXml - flag to use the XML file for the restore operation (output)
	@return 1 if all went OK, 0 if user canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::UnzipForRestore(HANDLE hEvent, const StrUni & stuSource,
	const StrUni & stuSourceXml, bool & fRestoreFromXml)
{
	try
	{
		// Perform the actual unzip:
		long nResult = m_pFileZipper->Unzip();

		// Suspend if other thread is awaiting user to confirm cancel/abort:
		::WaitForSingleObject(hEvent, INFINITE);
		if (m_pbkpprg->GetAbortedFlag())
		{
			return 0;
		}
		else
		{
			// Test to see if there was an error. Filter out error 509 "Nothing to do" as
			// this will be reported better when the required files are found to be missing.
			// Also, filter out 526, "xerWarnings", meaning something minor went wrong but
			// we got over it.
			StrAppBufPath strbpZipFileName(m_bkpi.m_strZipFileName.Chars());
			if ((nResult != 509) && (nResult != 526) &&
				m_pFileZipper->TestError(nResult, strbpZipFileName.Chars(), true))
			{
				return kstidRstUnzipError;
			}
		}
	}
	catch (...)
	{
		return kstidRstUnzipError;
	}

	// Check if we're to restore from the XML file or not, and if the relevant file is present:
	return CheckForRestoreFromXml(stuSource, stuSourceXml, fRestoreFromXml);
}

/*----------------------------------------------------------------------------------------------
	Check which files are available for restoring, and whether or not to use the XML file if
	it's available.

	@param stuSource - reference to the backup (.bak) filename
	@param stuSourceXml - reference to the XML backup filename
	@param fRestoreFromXml - flag to use the XML file for the restore operation (output)
	@return 1 if all went OK, 0 if user canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::CheckForRestoreFromXml(const StrUni & stuSource, const StrUni & stuSourceXml,
	bool & fRestoreFromXml)
{
	bool fBakExists = false;
	bool fXmlExists = false;

	// Test to see if BAK file was unzipped:
	WIN32_FIND_DATA wfd;
	HANDLE hFind;
	StrApp str(stuSource.Chars());
	hFind = ::FindFirstFile(str.Chars(), &wfd);
	if (hFind != INVALID_HANDLE_VALUE)
	{
		fBakExists = true;
		::FindClose(hFind);
	}
	// Test to see if XML file was unzipped:
	str.Assign(stuSourceXml.Chars());
	hFind = ::FindFirstFile(str.Chars(), &wfd);
	if (hFind != INVALID_HANDLE_VALUE)
	{
		fXmlExists = true;
		::FindClose(hFind);
	}

	fRestoreFromXml = m_bkpi.m_fXml;
	if (!fBakExists && !fXmlExists)
	{
		// Tell user that no backup file is present:
		return kstidRstFilesMissingError;
	}
	else
	{
		if (fRestoreFromXml && !fXmlExists)
		{
			if (BackupErrorHandler::MessageBox(m_pbkpprg->Hwnd(), BackupErrorHandler::kWarning,
				kstidUseDefault, kstidRstXmlMissingError, MB_ICONSTOP | MB_YESNO) == IDYES)
			{
				fRestoreFromXml = false;
			}
			else
			{
				return 0;
			}
		}
		else if (!fRestoreFromXml && !fBakExists)
		{
			if (BackupErrorHandler::MessageBox(m_pbkpprg->Hwnd(), BackupErrorHandler::kWarning,
				kstidUseDefault, kstidRstBakMissingError, MB_ICONSTOP | MB_YESNO) == IDYES)
			{
				fRestoreFromXml = true;
			}
			else
			{
				return 0;
			}
		}
	}
	m_pbkpprg->SetActivityNumber(1); // Update progress dialog to check off Unzip step
	return 1;
}

/*----------------------------------------------------------------------------------------------
	If the database already exists, back it up before trying to restore over it.

	@param fDbAlreadyExists - flag that a database of that name already exists
	@return 1 if all went OK, 0 if user canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::BackupForRestore(bool fDbAlreadyExists)
{
	if (fDbAlreadyExists)
	{
		int nErrorResult = BackupDatabaseBeforeOverwrite();
		if (nErrorResult != 1)
		{
			if (BackupErrorHandler::MessageBox(m_pbkpprg->Hwnd(),
					BackupErrorHandler::kWarning, kstidUseDefault, kstidRstZipError,
					MB_ICONSTOP | MB_YESNO | MB_DEFBUTTON2) == IDNO)
			{
				return 0;
			}
		}
	}
	m_pbkpprg->SetActivityNumber(2); // Update progress dialog to check off Backup step
	return 1;
}

/*----------------------------------------------------------------------------------------------
	If the database already exists, disconnect from it if at all possible.

	@param fDbAlreadyExists - flag that a database of that name already exists
	@param fDbDisconnectionFailed - flag that disconnecting the db failed (output)
	@param fClosedWindow - flag that a window connected to the db was closed (output)
	@param fDbPreserved - flag that the db was successfully preserved (output)
	@param fExpObjInc - flag that the count of exported objects was incremented (output)
	@return 1 if all went OK, 0 if user canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::DisconnectDatabase(bool fDbAlreadyExists, bool & fDbDisconnectionFailed,
	ComBool & fClosedWindow, bool & fDbPreserved, bool & fExpObjInc)
{
	if (!fDbAlreadyExists)
		return 1;

	HRESULT hr;
	StrAppBufPath strbpDbFile;
	CheckHr(hr = m_qode->CreateCommand(&m_qodc));
	bool fHaveMsdeFileName = GetMsdeFileName(strbpDbFile); //Get the current database file name
	if (!fHaveMsdeFileName)
		return kstidRstMsdeFailure;

	//Close, detach and move the existing database for the Restore to proceed.
	//
	// First close the open database windows
	if (m_pbkupd)
	{
		CheckHr(m_pbkupd->IncExportedObjects_Bkupd());
		fExpObjInc = true;
		// Now close the current database with all its windows.
		CheckHr(m_pbkupd->CloseDbAndWindows_Bkupd(s_stuLocalServer.Chars(),
			m_stuTargetDatabase.Chars(), false, &fClosedWindow));
	}

	// Give users time to log off, then force them off anyway:
	ComBool fDisconnected = true;
	try
	{
		IDisconnectDbPtr qzdscdb;
		qzdscdb.CreateInstance(CLSID_FwDisconnect);

		// Set up strings needed for disconnection:
		StrUni stuReason(kstidReasonDisconnectRestore);

		// Get our own name:
		DWORD nBufSize = MAX_COMPUTERNAME_LENGTH + 1;
		achar rgchBuffer[MAX_COMPUTERNAME_LENGTH + 1];
		::GetComputerName(rgchBuffer, &nBufSize);
		StrUni stuFmt(kstidRemoteReasonRestore);
		StrUni stuExternal;
		stuExternal.Format(stuFmt.Chars(), rgchBuffer);

		StrUni stuCancel(kstidCancelDisconnectRestore);

		qzdscdb->Init(m_stuTargetDatabase.Bstr(), s_stuLocalServer.Bstr(), stuReason.Bstr(),
			stuExternal.Bstr(), (ComBool)true, stuCancel.Bstr(), (int)m_pbkpprg->Hwnd());
		qzdscdb->DisconnectAll(&fDisconnected);
	}
	catch (...)
	{
		fDisconnected = false;
	}
	if (fDisconnected)
	{
		return DetachDatabase(fDbPreserved, strbpDbFile);
	}
	else
	{
		fDbDisconnectionFailed = true;
		// User canceled, or it was impossible to disconnect people:
		return 0;
	}
}

/*----------------------------------------------------------------------------------------------
	The database exists and has been disconnected: now we detach it if at all possible.

	@param fDbPreserved - flag that the db was successfully preserved (output)
	@param strbpDbFile - current database filename
	@return 1 if all went OK, 0 if user canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::DetachDatabase(bool & fDbPreserved, const StrAppBufPath & strbpDbFile)
{
	// Detach the existing database, and move its files safely away, so that we can
	// recover if the restore fails:
	fDbPreserved = false;  //Just to be sure.
	HRESULT hr;
	StrUni stuSql = L"EXEC sp_detach_db ?";
	CheckHr(hr = m_qode->CreateCommand(&m_qodc));
	CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
		(BYTE *)m_stuTargetDatabase.Chars(), m_stuTargetDatabase.Length() * isizeof(OLECHAR)));
	//Do the detach and continue if successful
	int nRes;
	do
	{
		nRes = IDCANCEL;
		try
		{
			CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
		}
		catch (Throwable & thr)
		{
			thr;
			nRes = BackupErrorHandler::MessageBox(m_pbkpprg->Hwnd(),
				BackupErrorHandler::kWarning, kstidUseDefault, kstidCantDetachDb,
				MB_ICONSTOP | MB_OKCANCEL);
			if (nRes == IDCANCEL)
				return 0;
		}
	} while (nRes == IDOK);

	// Generate file names for manipulating the existing database
	// Form directory and file root strings:
	StrAppBufPath strbpDbHomeDirectory;
	StrAppBufPath strbpDbOriginalFileRoot;

	StrAppBuf strbSlash("\\");
	int ichLastSlash = strbpDbFile.ReverseFindCh(strbSlash[0]);
	if (ichLastSlash >= 0)
		strbpDbHomeDirectory.Assign(strbpDbFile.Left(ichLastSlash).Chars());

	StrAppBuf strbDot(".");
	int ichLastDot = strbpDbFile.ReverseFindCh(strbDot[0]);
	if (ichLastDot > 0)  // There is a file name to process
	{
		strbpDbOriginalFileRoot.Assign(strbpDbFile.Mid(ichLastSlash + 1,
			ichLastDot - ichLastSlash - 1).Chars());
		GenerateUnusedFilenames(strbpDbOriginalFileRoot.Chars());

		//Current Database file names
		m_strbpDbMdfCurrent.Assign(strbpDbFile.Chars());
		m_strbpDbLdfCurrent.Format(_T("%s\\%s_Log.ldf"),
			strbpDbHomeDirectory.Chars(),
			strbpDbOriginalFileRoot.Chars());
	}
	else //There is no Root source file name to process.
	{
		return kstidRstNoDbName;
	}

	// Attempt to copy the MDF and LDF files.
	// This fixes TE-5821 since SQL server needs administrator privileges to change the file.
	// Unfortunately, this is not as portable as the ::CopyFile command that doesn't work for
	// users with limited privileges.
	if (XpCmdShellCmd(L"copy", strbpDbFile, m_strbpDbMdfCache))
	{
		fDbPreserved = XpCmdShellCmd(L"copy", m_strbpDbLdfCurrent, m_strbpDbLdfCache);
	}

	// If the database was not successfully backed up, notify the user.
	if (!fDbPreserved)
	{
		if (BackupErrorHandler::MessageBox(m_pbkpprg->Hwnd(),
				BackupErrorHandler::kWarning, kstidUseDefault, kstidRstPreserveDbFileError,
				MB_ICONSTOP | MB_YESNO | MB_DEFBUTTON2) == IDNO)
		{
			// If the user indicates that they do not want to proceed with the restore, then we
			// need to re-attach the old database.
			AttachDatabase();
			return 0;
		}
	}

	// Both files were successfully copied to the working directory. We can delete them
	// from the Current directory.
	bool fDbOrigDeleted = false;
	if (XpCmdShellCmd(L"del", m_strbpDbMdfCurrent, _T("")))
	{
		fDbOrigDeleted = BackupHandler::XpCmdShellCmd(L"del", m_strbpDbLdfCurrent, _T(""));
	}

	if (!fDbOrigDeleted)
	{
		if (BackupErrorHandler::MessageBox(m_pbkpprg->Hwnd(),
				BackupErrorHandler::kWarning, kstidUseDefault, kstidRstPreserveDbFileError,
				MB_ICONSTOP | MB_YESNO | MB_DEFBUTTON2) == IDNO)
		{
			return 0;
		}
	}

	return 1;
}

/*----------------------------------------------------------------------------------------------
  Use xp_cmdshell to perform operations on a database.
  Note: Unfortunately, this is not portable to other platforms but, for users with limited
  permissions, it allows them to make a backup of the current database and to delete the current
  database if successful.
	@param cmd - xp_cmdshell command to execute
	@param strbpArg1 - first command-line argument to the xp_cmdshell command
	@param strbpArg2 - second command-line argument to the xp_cmdshell command
	@return true if all went OK, false if the command was not successful.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::XpCmdShellCmd(const StrUni & cmd, const StrAppBufPath & strbpArg1,
								 const StrAppBufPath & strbpArg2)
{
	StrUni stuCmd(L"new");
	StrUni stuArg1(strbpArg1);
	StrUtil::FixForSqlQuotedString(stuArg1);	// See LT-9115.
	if (strbpArg2.Length() == 0)
	{
		// handle an xp_cmdshell command with only one argument.
		stuCmd.Format(L"EXEC xp_cmdshell '%s \"%s\"'", cmd.Chars(), stuArg1.Chars());
	}
	else
	{
		StrUni stuArg2(strbpArg2);
		StrUtil::FixForSqlQuotedString(stuArg2);	// See LT-9115.
		// handle an xp_cmdshell command with two arguments.
		stuCmd.Format(L"EXEC xp_cmdshell '%s \"%s\" \"%s\"'", cmd.Chars(), stuArg1.Chars(),
			stuArg2.Chars());
	}
	try
	{
		HRESULT hr;
		CheckHr(hr = m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	}
	catch (...)
	{
		return false;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Check if either of the destination file names (*.mdf or *_Log.ldf) exist. If so, add
	underscore(s) to the names until they are unique.  This method sets the member variables
	m_strbpDbMdfCache and m_strbpDbLdfCache.

	@param pszDbOriginalFileRoot - original base of the database filename
----------------------------------------------------------------------------------------------*/
void BackupHandler::GenerateUnusedFilenames(const achar * pszDbOriginalFileRoot)
{
	StrAppBufPath strbpDbPreservedFileRoot(pszDbOriginalFileRoot);
	WIN32_FIND_DATA wfd;
	HANDLE hFind;
	bool fExists;
	do
	{
		fExists = false;
		m_strbpDbMdfCache.Format(_T("%s%s.mdf"), m_strbpTempPath.Chars(),
			strbpDbPreservedFileRoot.Chars());
		hFind = ::FindFirstFile(m_strbpDbMdfCache.Chars(), &wfd);
		if (hFind != INVALID_HANDLE_VALUE)
		{
			fExists = true;
			::FindClose(hFind);
		}
		if (!fExists)
		{
			m_strbpDbLdfCache.Format(_T("%s%s_Log.ldf"),
				m_strbpTempPath.Chars(),
				strbpDbPreservedFileRoot.Chars());
			hFind = ::FindFirstFile(m_strbpDbLdfCache.Chars(), &wfd);
			if (hFind != INVALID_HANDLE_VALUE)
			{
				fExists = true;
				::FindClose(hFind);
			}
		}
		if (fExists)
		{
			// Prepend an underscore:
			strbpDbPreservedFileRoot.Replace(0, 0, _T("_"));
		}
	} while (fExists);
}

/*----------------------------------------------------------------------------------------------
	Restore the database from the given XML file.

	@param stuSourceXml - reference to the XML filename
	@return 1 if all went OK, 0 if user canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::RestoreFromXml(const StrUni & stuSourceXml)
{
	// Estimate how long a restore from XML will take.
	// Get size of XML file:
	int64 nXmlFileSize = 10000000; // Set default guess to ten million bytes.
	WIN32_FIND_DATA wfd;
	HANDLE hFind;
	StrApp str(stuSourceXml.Chars());
	hFind = ::FindFirstFile(str.Chars(), &wfd);
	if (hFind != INVALID_HANDLE_VALUE)
	{
		nXmlFileSize = (wfd.nFileSizeHigh * MAXDWORD) + wfd.nFileSizeLow;
		::FindClose(hFind);
	}
	// Now estimate how long it will take to load the file, knowing its length.
	int nEstimatedTime = 120; // Guess two minutes as a default.
	int nXmlLoadRate = 20000; // About right for my machine (AlistairI)
	// See if any previous attempts' values were recorded:
	m_fws.GetBinary(_T("Basics"), _T("XmlLoadRate"), (BYTE *)&nXmlLoadRate, 4);
	if (nXmlLoadRate > 0)
		nEstimatedTime = (int)(nXmlFileSize / nXmlLoadRate);

	m_pbkpprg->SetActivityEstimatedTime(nEstimatedTime);

	// Make a note of the time:
	SYSTEMTIME systTime;
	GetLocalTime(&systTime);
	FILETIME filtTime;
	SystemTimeToFileTime(&systTime, &filtTime);
	int64 nStartTime = *((int64 *)(&filtTime));

	// Copy the template database files, renaming them in the process, and then "attach"
	// the database:
	int nErrorResult = RestoreFromXml(stuSourceXml, m_stuTargetMdf, m_stuTargetLdf);
	if (nErrorResult != 1)
		return nErrorResult;

	// Make a note of the time:
	GetLocalTime(&systTime);
	SystemTimeToFileTime(&systTime, &filtTime);
	int64 nEndTime = *((int64 *)(&filtTime));

	// Calculate duration of file loading:
	int nReadDuration = (int)((nEndTime - nStartTime) / kn100NanoSeconds);
	// On fast machines, this activity can take less than 1 second, so fudge:
	if (nReadDuration == 0)
		nReadDuration = 1;

	// Record actual rate of loading:
	if (nReadDuration > 0)
	{
		nXmlLoadRate = (int)(nXmlFileSize / nReadDuration);
		m_fws.SetBinary(_T("Basics"), _T("XmlLoadRate"), (BYTE *)&nXmlLoadRate, 4);
	}
	return 1;
}

/*----------------------------------------------------------------------------------------------
	Restore the database from the backup (.bak) file.

	@param stuSource - reference to the backup (.bak) filename
	@param nRestoreOptions - options word returned from restore options dialog
	@return 1 if all went OK, 0 if user canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::RestoreFromBak(const StrUni & stuSource, int nRestoreOptions)
{
	// Estimate how long a restore from a BAK file will take.
	// Get size of BAK file:
	int64 nBakFileSize = 10000000; // Set default guess to ten million bytes.
	WIN32_FIND_DATA wfd;
	HANDLE hFind;
	StrApp str(stuSource.Chars());
	hFind = ::FindFirstFile(str.Chars(), &wfd);
	if (hFind != INVALID_HANDLE_VALUE)
	{
		nBakFileSize = (wfd.nFileSizeHigh * MAXDWORD) + wfd.nFileSizeLow;
		::FindClose(hFind);
	}
	// Now estimate how long it will take to load the file, knowing its length.
	int nEstimatedTime = 120; // Guess two minutes as a default.
	int nBakLoadRate = 1200000; // About right for my machine (AlistairI)
	// See if any previous attempts' values were recorded:
	m_fws.GetBinary(_T("Basics"), _T("BakLoadRate"), (BYTE *)&nBakLoadRate, 4);
	if (nBakLoadRate > 0)
		nEstimatedTime = (int)(nBakFileSize / nBakLoadRate);

	m_pbkpprg->SetActivityEstimatedTime(nEstimatedTime);

	// Make a note of the time:
	SYSTEMTIME systTime;
	GetLocalTime(&systTime);
	FILETIME filtTime;
	SystemTimeToFileTime(&systTime, &filtTime);
	int64 nStartTime = *((int64 *)(&filtTime));

	int nErrorResult = RestoreFromBak(stuSource, nRestoreOptions, m_stuTargetMdf,
		m_stuTargetLdf);
	if (nErrorResult != 1)
		return nErrorResult;

	// Make a note of the time:
	GetLocalTime(&systTime);
	SystemTimeToFileTime(&systTime, &filtTime);
	int64 nEndTime = *((int64 *)(&filtTime));

	// Calculate duration of file loading:
	int nReadDuration = (int)((nEndTime - nStartTime) / kn100NanoSeconds);

	// Record actual rate of Loading:
	if (nReadDuration > 0)
	{
		nBakLoadRate = (int)(nBakFileSize / nReadDuration);
		m_fws.SetBinary(_T("Basics"), _T("BakLoadRate"), (BYTE *)&nBakLoadRate, 4);
	}
	return 1;
}

/*----------------------------------------------------------------------------------------------
	Finish the processing needed for restoring a file, the misc. activities which occur after
	the actual restore.

	@param hEvent - handle used to prevent multiple instances, even from different apps.
	@param fDbPreserved - flag that the db was successfully preserved
	@return 1 if all went OK, 0 if user canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::FinishRestoring(HANDLE hEvent, bool fDbPreserved)
{
	// Suspend if other thread is awaiting user to confirm cancel/abort:
	::WaitForSingleObject(hEvent, INFINITE);
	if (m_pbkpprg->GetAbortedFlag())
		return 0;

	if (m_pbkupd)
	{
		ComBool fTemp;
		CheckHr(m_pbkupd->CheckDbVerCompatibility_Bkupd(s_stuLocalServer.Chars(),
			m_stuTargetDatabase.Chars(), &fTemp));
		if (!(bool)fTemp)
			return kstidRstIncompatibleVersn;
	}

	// This is the point of no return, so disable the cancel/abort button:
	m_pbkpprg->EnableAbortButton(false);

	// Suspend if other thread is awaiting user to confirm cancel/abort:
	::WaitForSingleObject(hEvent, INFINITE);
	if (m_pbkpprg->GetAbortedFlag())
		return 0;

	m_pbkpprg->SetActivityNumber(4); // Update progress dialog to check off Upgrading step

	if (fDbPreserved)
	{
		// Restore was successful, so all we need to do is delete the cached DB files:
		::DeleteFile(m_strbpDbMdfCache.Chars());
		::DeleteFile(m_strbpDbLdfCache.Chars());
	}
	return 1;
}


/*----------------------------------------------------------------------------------------------
	Allow the user to configure the backup and restore settings.
	@param hwndParent Parent window for dialog
	@param fShowRestore True if Restore tab should be the initial tab shown.
	@return Enumerated value, describing what user did.
----------------------------------------------------------------------------------------------*/
int BackupHandler::UserConfigure(HWND hwndParent, bool fShowRestore,
								 IHelpTopicProvider * phtprovHelpUrls)
{
	return BackupDlg::BackupDialog(hwndParent, this, fShowRestore, phtprovHelpUrls);
}


/*----------------------------------------------------------------------------------------------
	Check if the current backup destination path exists, and if not, create all necessary
	directories.
	@param fPromptUser True if user may be consulted before creating directories.
	@param hwndParent Handle of parent window, should a dialog box be needed.
	@return One of enumerated constants describing what happened.
----------------------------------------------------------------------------------------------*/
int BackupHandler::ValidateBackupPath(bool fPromptUser, HWND hwndParent)
{
	// Check if destination directory exists:
	DWORD nFlags = GetFileAttributes(m_bkpi.m_strbDirectoryPath.Chars());
	if (nFlags == INVALID_FILE_ATTRIBUTES)
	{
		int nError = GetLastError();
		// File not found occurs if the last directory in the path does not exist.
		// path not found occurs if some other directory in the path does not exist.
		if (nError == ERROR_FILE_NOT_FOUND || nError == ERROR_PATH_NOT_FOUND)
		{
			// This is the place we expect to be when creating a new folder.
			// Destination folder does not exist.
			bool fCreateDirectory = false;
			if (fPromptUser)
			{
				// Ask user if they want to create the destination folder:
				StrApp strFmt(kstidBkpCreateDirectory);
				StrApp str;
				str.Format(strFmt.Chars(), m_bkpi.m_strbDirectoryPath.Chars());
				StrApp strTitle(kstidBkpSystem);
				if (::MessageBox(hwndParent, str.Chars(), strTitle.Chars(),
					MB_YESNO | MB_ICONQUESTION) == IDYES)
				{
					fCreateDirectory = true;
				}
				else
					return kUserQuit;
			}
			else
			{
				// Not allowed to prompt user:
				fCreateDirectory = true;
			}

			if (fCreateDirectory)
			{
				if (!MakeDir(m_bkpi.m_strbDirectoryPath.Chars()))
				{
					if (fPromptUser)
					{
						BackupErrorHandler::ErrorBox(hwndParent,
							BackupErrorHandler::kBackupFailure, kstidBkpCreateDirError);
					}
					return kCreationFailed;
				}
			}
			return kCreationSucceeded;
		}
		else // An unexpected error occurred
		{
			if (fPromptUser)
			{
				BackupErrorHandler::ErrorBox(hwndParent, BackupErrorHandler::kBackupFailure,
					kstidBkpCreateDirError, nError);
			}
		}
		return kCreationFailed;
	}
	else if (!(nFlags & FILE_ATTRIBUTE_DIRECTORY))
	{
		if (fPromptUser)
		{
			BackupErrorHandler::ErrorBox(hwndParent, BackupErrorHandler::kBackupFailure,
				kstidBkpCreateDirError2);
		}
		return kCreationFailed;
	} // End if destination directory did not exist.

	return kDirectoryAlreadyExisted;
}


/*----------------------------------------------------------------------------------------------
	Create in the given string a path to our temporary working directory.
	@param strbpTempPath Path for the temporary working directory.
----------------------------------------------------------------------------------------------*/
void BackupHandler::GetTempWorkingDirectory(StrAppBufPath & strbpTempPath)
{
	achar rgchTempPath[MAX_PATH];
	if (0 == GetTempPath(MAX_PATH, rgchTempPath))
	{
		strbpTempPath.Append("  \\");
	}
	else
	{
		strbpTempPath.Append(rgchTempPath);
	}
	strbpTempPath.Append("SILFwBackupBuffer\\");

	// Make sure our temp directory exists:
	CreateDirectory(strbpTempPath.Chars(), NULL);
}


/*----------------------------------------------------------------------------------------------
	Remove *.bak, *.xml and *.log files from our temporary working directory.
	It would be tidier to remove all files, but just in case the temporary working directory
	gets set to "c:\" or something, it is perhaps better to be a little cautious.
----------------------------------------------------------------------------------------------*/
void BackupHandler::ClearTempWorkingDirectory()
{
	// Get working directory:
	ClearFiles(m_strbpTempPath, L"*.bak");
	ClearFiles(m_strbpTempPath, L"*.xml");
	ClearFiles(m_strbpTempPath, L"*.log");
	ClearFiles(m_strbpTempPath, L"*.mdf");
	ClearFiles(m_strbpTempPath, L"*.ldf");
}


/*----------------------------------------------------------------------------------------------
	Method to clear files from the given path using a mask specification.
	Used by ClearTempWorkingDirectory().
	@param strbpPath Path for the temporary working directory.
	@param strFindSpec Specification for which files to delete.
----------------------------------------------------------------------------------------------*/
void BackupHandler::ClearFiles(StrAppBufPath & strbpPath, StrUni strFindSpec)
{
	// Construct the Full Mask
	StrAppBuf strbFindMask(strbpPath.Chars());
	strbFindMask.Append(strFindSpec.Chars());
	WIN32_FIND_DATA wfd;
	HANDLE hFind;
	hFind = ::FindFirstFile(strbFindMask.Chars(), &wfd);
	if (hFind != INVALID_HANDLE_VALUE)
	{
		do
		{
			StrApp strCondemnedFile(strbpPath.Chars());
			strCondemnedFile.Append(wfd.cFileName);
			DeleteFile(strCondemnedFile.Chars());
		} while (::FindNextFile(hFind, &wfd));
		::FindClose(hFind);
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the file Zip system for Backup or Restore.
	@param bhpprg Instance of the Backup Progress dialog, needed by the zipper for user cancel
	and by the error handler.
	@return 1 if all went OK.
----------------------------------------------------------------------------------------------*/
int BackupHandler::InitializeFileZipper()
{
	Assert(m_pbkpprg != NULL);
	m_pZipData->Init(&m_bkpi, m_pbkpprg);

	// Initialize Xceed Zip module:
	try
	{
		if (!m_pFileZipper->Init(m_pZipData, m_pbkpprg))
			return kstidBkpNoZipError;
	}
	catch (...)
	{
		return kstidBkpNoZipError; // More serious error trying to initialize Zip module.
	}

	return 1;
}
/*----------------------------------------------------------------------------------------------
	Gets the original file name of a backed up database, looking first in the zipped header
	file, then falling back on the zip file name as a guess.
	@param strbpSourceFileName Name of the zip file that is source of the database for restore.
	@param stuOriginalDatabase Place to write file name of the original database.
	@return 1 if all went OK, 0 if canceled, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::GetOriginalDbName(StrAppBufPath & strbpSourceFileName,
	StrUni & stuOriginalDatabase)
{
	try
	{
		Assert(m_pbkpprg != NULL);
		// Set up parameters for zip file:
		SmartBstr sbstr;
		StrAppBufPath strbpArchivePath(m_bkpi.m_strbDirectoryPath.Chars());
		if (strbpArchivePath.Length() == 0 ||
			strbpArchivePath[strbpArchivePath.Length() - 1] != '\\')
		{
			strbpArchivePath.Append("\\");
		}
		strbpArchivePath.Append(strbpSourceFileName);

		strbpArchivePath.GetBstr(&sbstr);
		m_pFileZipper->SetZipFilename(sbstr);
		m_strbpTempPath.GetBstr(&sbstr);
		m_pFileZipper->SetUnzipToFolder(sbstr);

		// Read data from header:
		long nResult = m_pFileZipper->ReadHeader();

		// Test to see if there was an error. Filter out error 509 "Nothing to do" as
		// this means there is no header, which we can deal with.
		if (nResult != 509 &&
			!m_pFileZipper->TestError(nResult, strbpArchivePath.Chars(), true))
		{
			// Get name of database from the zip file's header data:
			m_pFileZipper->GetHeaderDbName(stuOriginalDatabase);
			if (stuOriginalDatabase.Length() == 0)
			{
				// Something is wrong with header, so get database name from file name:
				StrApp strDatabaseName;
				if (!BackupFileNameProcessor::GetDatabaseName(strbpSourceFileName,
					strDatabaseName))
				{
					// There was no database name in the file name. Zip file is probably
					// corrupt.
					return kstidRstZipHeaderError;
				}
				else
				{
					stuOriginalDatabase.Assign(strDatabaseName.Chars());
					SmartBstr sbstr;
					strDatabaseName.GetBstr(&sbstr);
					m_pZipData->SetDBName(sbstr);
				}
			}
		}
	}
	catch (...)
	{
		// Unforseen zip module error:
		return kstidRstUnzipError;
	}
	return 1;
}

/*----------------------------------------------------------------------------------------------
	Gets several strings relating to the restore source file.
	@param stuOriginalDatabase File name of backed-up database      (input)
	@param stuSource Full path of unzipped backup file
	@param stuSourceXml  Full path of unzipped XML backup file
	@return 1 if all went OK, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::GetSourceFileStrings(StrUni & stuOriginalDatabase,
		StrUni & stuSource, StrUni & stuSourceXml)
{
	StrUni stuTargetDir; // Local--Directory path for 'new' database files

	try
	{
		Assert(m_pbkpprg != NULL);
		// Get source file paths ready:
		stuSource.Assign(m_strbpTempPath.Chars());
		stuSource.Append(stuOriginalDatabase.Chars());
		stuSourceXml.Assign(stuSource.Chars());
		stuSource.Append(".bak");
		stuSourceXml.Append(".xml");
	}
	catch(...)
	{
		// Unforseen problem generating file names:
		return kstidRstFileNameError;
	}
	return 1;
}

/*----------------------------------------------------------------------------------------------
	Set the target directory for restoring, and check whether we can create/write files in that
	directory.
	@return 1 if all went OK, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::CheckTargetDirectory()
{
	try
	{
		// Set target directory and make sure it exists.
		StrAppBufPath strbpFwRootDir;
		strbpFwRootDir.Assign(DirectoryFinder::FwRootDataDir().Chars());
		m_strbpTargetDir.Format(_T("%s\\Data"), strbpFwRootDir.Chars());
		// Check whether we can write to this directory.
		StrAppBufPath strbTmp;
		strbTmp.Format(_T("%s\\TestFileToSeeIfDataDirIsWritable.xyz"), m_strbpTargetDir.Chars());
		HANDLE h = ::CreateFile(strbTmp.Chars(), GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
			FILE_ATTRIBUTE_NORMAL, NULL);
		if (h == INVALID_HANDLE_VALUE)
		{
			DWORD dw = ::GetLastError();
			switch (dw)
			{
			case 0:
				break;
			default:
				break;
			}
			return kstidCantWriteToRestore;
		}
		else
		{
			::CloseHandle(h);
			::DeleteFile(strbTmp.Chars());
			return 1;
		}
	}
	catch(...)
	{
		return kstidCantWriteToRestore;
	}
}

/*----------------------------------------------------------------------------------------------
	Gets several strings relating to the restore target file.
	@param stuTargetMdf Target MDF
	@param stuTargetLdf Target LDF
	@return 1 if all went OK, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::GetTargetFileStrings(StrUni & stuTargetMdf, StrUni & stuTargetLdf)
{
	try
	{
		int stid = CheckTargetDirectory();
		if (stid > 1)
			return stid;
		if (stid < 1)
			return kstidRstFileNameError;

		StrUni stuDir(m_strbpTargetDir.Chars());
		stuTargetMdf.Format(L"%s\\%s.mdf", stuDir.Chars(), m_stuTargetDatabase.Chars());
		stuTargetLdf.Format(L"%s\\%s_Log.ldf", stuDir.Chars(), m_stuTargetDatabase.Chars());
	}
	catch(...)
	{
		// Unforeseen problem generating file names:
		return kstidRstFileNameError;
	}
	return 1;
}

/*----------------------------------------------------------------------------------------------
	Restore the database from the XML file.

	@param stuSourceXml  Full path of unzipped XML backup file (input)
	@param stuTargetMdf	  MDF database file name
	@param stuTargetLdf   LDF database file name
	@return 1 if all went OK, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::RestoreFromXml(const StrUni & stuSourceXml,
	StrUni & stuTargetMdf, StrUni & stuTargetLdf)
{
	// Create and attach new database files from FieldWorks templates.
	HRESULT hr;
	StrAppBufPath strbpFwRootDir;
	strbpFwRootDir.Assign(DirectoryFinder::FwRootCodeDir().Chars());
	StrAppBufPath strbpTemplate;
	strbpTemplate.Format(_T("%s%s"), strbpFwRootDir.Chars(), _T("\\Templates\\"));
	StrAppBufPath strbpBlankMdf;
	strbpBlankMdf.Format(_T("%s%s"), strbpTemplate.Chars(), _T("BlankLangProj.mdf"));
	// Create MDF and LDF database files from template.
	if (!MakeDatabaseFile(stuTargetMdf, strbpBlankMdf))
		return kstidRstXmlDbCreateError;
	StrAppBufPath strbpBlankLdf;
	strbpBlankLdf.Format(_T("%s%s"), strbpTemplate.Chars(), _T("BlankLangProj_Log.ldf"));
	if (!MakeDatabaseFile(stuTargetLdf, strbpBlankLdf))
		return kstidRstXmlDbCreateError;
	// Try to attach the restored database
	StrUni stuSql = L"EXEC sp_attach_db ?, ?, ?";
	try
	{
		CheckHr(hr = m_qode->CreateCommand(&m_qodc));
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)m_stuTargetDatabase.Chars(), m_stuTargetDatabase.Length()
			* isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuTargetMdf.Chars(), stuTargetMdf.Length() * isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuTargetLdf.Chars(), stuTargetLdf.Length() * isizeof(OLECHAR)));
		CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	}
	catch (...)
	{
		return kstidRstXmlDbCreateError;
	}
	// Restore from XML file:
	IFwXmlDataPtr qfwxd;
	int retVal = 1;
	try
	{
		// Load XML version of database:
		qfwxd.CreateInstance(CLSID_FwXmlData, CLSCTX_INPROC_SERVER);
		CheckHr(hr = qfwxd->Open(s_stuLocalServer.Bstr(), m_stuTargetDatabase.Bstr()));
		// REVIEW AlistairI: there is a class IAdvInd which will link to a progress dialog
		// AfProgressDlg, which can be used as the second argument to this method?
		CheckHr(hr = qfwxd->LoadXml(stuSourceXml.Bstr(), NULL));
		qfwxd->Close();
		qfwxd.Clear();
	}
	catch (...)
	{
		if (qfwxd)
		{
			qfwxd->Close();
			qfwxd.Clear();
		}
		retVal = kstidRstXmlError;
	}
	if (retVal != 1)
	{
		try
		{
			stuSql.Format(L"DROP DATABASE [%s]", m_stuTargetDatabase.Chars());
			CheckHr(hr = m_qode->CreateCommand(&m_qodc));
			CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
		}
		catch (...)
		{
			// nothing we can do here, but we don't want throw to propagate any further.
		}
	}
	m_qodc.Clear();
	return retVal;
}

/*----------------------------------------------------------------------------------------------
	Restore the database from BAK file.

	@param stuSource		Source of restore (.bak file)
	@param nRestoreOptions	Response from Restore Options dialog
	@param stuTargetMdf		MDF database file name
	@param stuTargetLdf		LDF database file name
	@return 1 if all went OK, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::RestoreFromBak(const StrUni & stuSource, int nRestoreOptions,
	StrUni & stuTargetMdf, StrUni & stuTargetLdf)
{
	// We will try to obtain the names of the DB files in the .BAK file:
	StrUni stuDataFile;
	StrUni stuLogFile;
	int nErrorResult = RestoreFileListFromBak(stuSource, stuDataFile, stuLogFile);
	if (nErrorResult != 1)
		return nErrorResult;

	// We may need to check for pre-existing unattached DB files, depending on
	// clash test outcome:
	bool fCheckForPreExistingFiles = ((nRestoreOptions == RestoreOptionsDlg::kNoDbClash)
		|| (nRestoreOptions == RestoreOptionsDlg::kCreateNew));

	// See if we need to move any pre-existing files:
	if (fCheckForPreExistingFiles)
	{
		// Rename the pre-existing files to .old.
		StrApp strDataFile(stuDataFile.Chars());
		StrApp strLogFile(stuLogFile.Chars());
		StrApp strOld;
		StrApp strNew;
		strOld.Format(_T("%s\\%s.mdf"), m_strbpTargetDir.Chars(), strDataFile.Chars());
		strNew.Format(_T("%s\\%s.old"), m_strbpTargetDir.Chars(), strDataFile.Chars());
		// If an older copy is present, delete it.
		DWORD dwT = GetFileAttributes(strNew.Chars());
		if (dwT != -1)
			DeleteFile(strNew.Chars());
		MoveFile(strOld.Chars(), strNew.Chars());
		// In the event that either the delete or moved failed, the user need not
		// be informed at this point, as the restore operation may work anyway, and
		// even if it doesn't, the error can be reported then.
		strOld.Format(_T("%s\\%s.ldf"), m_strbpTargetDir.Chars(), strLogFile.Chars());
		strNew.Format(_T("%s\\%s.old"), m_strbpTargetDir.Chars(), strLogFile.Chars());
		// If an older copy is present, delete it.
		dwT = GetFileAttributes(strNew.Chars());
		if (dwT != -1)
			DeleteFile(strNew.Chars());
		MoveFile(strOld.Chars(), strNew.Chars());
		// In the event that either the delete or moved failed, the user need not
		// be informed at this point, as the restore operation may work anyway, and
		// even if it doesn't, the error can be reported then.
	}
	// Get MSDE to restore from backup file. We want restored DB files to go into
	// FW's data directory, no matter where they were when they were backed up:
	try
	{
		StrUni stuSql =
			L"RESTORE DATABASE ? FROM DISK = ? WITH MOVE ? TO ?, MOVE ? TO ?, REPLACE";
		HRESULT hr;
		CheckHr(hr = m_qode->CreateCommand(&m_qodc));
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)m_stuTargetDatabase.Chars(), m_stuTargetDatabase.Length()
			* isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuSource.Chars(), stuSource.Length() * isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuDataFile.Chars(), stuDataFile.Length() * isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(4, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuTargetMdf.Chars(), stuTargetMdf.Length() * isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(5, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuLogFile.Chars(), stuLogFile.Length() * isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(6, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuTargetLdf.Chars(), stuTargetLdf.Length() * isizeof(OLECHAR)));
		CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	}
	catch (...)
	{
		return kstidRstDbRestoreError;
	}
	return 1;
}

/*----------------------------------------------------------------------------------------------
	Restore only the file list from the BAK file.

	@param stuSource		Source of restore (.bak file)
	@param nRestoreOptions	Response from Restore Options dialog
	@param stuTargetMdf		MDF database file name
	@param stuTargetLdf		LDF database file name
	@return 1 if all went OK, else error message code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::RestoreFileListFromBak(const StrUni & stuSource, StrUni & stuDataFile,
	StrUni & stuLogFile)
{
	bool fDataFileKnown = false;
	bool fLogFileKnown = false;
	try
	{
		StrUni stuSql = L"RESTORE FILELISTONLY FROM DISK = ?";
		HRESULT hr;
		CheckHr(hr = m_qode->CreateCommand(&m_qodc));
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuSource.Chars(), stuSource.Length() * isizeof(OLECHAR)));
		CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(hr = m_qodc->GetRowset(0));
		ComBool fMoreRows;
		CheckHr(hr = m_qodc->NextRow(&fMoreRows));
		// There is a theoretical possibility that more than one database exists in
		// the .BAK file. However, we will only use the first one:
		while (fMoreRows && (!fDataFileKnown || !fLogFileKnown))
		{
			const int knLogicalFileNameMax = 128;
			OLECHAR rgchLogicalFileName[knLogicalFileNameMax];
			UINT luSpaceTaken;
			ComBool fIsNull;

			// Fetch logical file name:
			CheckHr(hr = m_qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchLogicalFileName),
				isizeof(OLECHAR) * knLogicalFileNameMax, &luSpaceTaken, &fIsNull, 2));
			if (fIsNull)
				break;
			// Fetch file type:
			OLECHAR chType;
			CheckHr(hr = m_qodc->GetColValue(3, reinterpret_cast <BYTE *>(&chType),
				isizeof(OLECHAR), &luSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				break;
			StrUni stuType;
			stuType.Assign(&chType, 1);
			StrUni stuD = "D";
			StrUni stuL = "L";

			if (stuD == stuType)
			{
				stuDataFile.Assign(rgchLogicalFileName);
				fDataFileKnown = true;
			}
			else if (stuL == stuType)
			{
				stuLogFile.Assign(rgchLogicalFileName);
				fLogFileKnown = true;
			}
			CheckHr(hr = m_qodc->NextRow(&fMoreRows));
		} // Next row
	}
	// Check that we successfully got the file names:
	catch (...)
	{
		return kstidRstBakFileListError;
	}
	if (!fDataFileKnown || !fLogFileKnown)
	{
		return kstidRstBakFileListError;
	}
	else
	{
		return 1;
	}
}

/*----------------------------------------------------------------------------------------------
	Rename an existing database file with a ".old" extension.
	@param stuTargetDatabaseFile [in] name of database file
	@param strbpBlankDatabaseFile [in] name of blank (template) database file
	@return True, if all went well
----------------------------------------------------------------------------------------------*/
bool BackupHandler::MakeDatabaseFile(StrUni & stuTargetDatabaseFile,
	StrAppBufPath & strbpBlankDatabaseFile)
{
	// If destination file exists, rename existing version to *.old
	StrAppBufPath strbpTargetDatabaseFile(stuTargetDatabaseFile.Chars());
	StrApp strOld;
	StrAppBuf strbDot(".");
	int ichLastDot = strbpTargetDatabaseFile.ReverseFindCh(strbDot[0]);
	Assert(ichLastDot > 0);
	strOld.Assign(strbpTargetDatabaseFile.Left(ichLastDot).Chars());
	strOld.Append(".old");

	// If an older copy is present, delete it.
	DWORD dwT = GetFileAttributes(strOld.Chars());
	if (dwT != -1)
		DeleteFile(strOld.Chars());
	MoveFile(strbpTargetDatabaseFile.Chars(), strOld.Chars());
	// In the event that either the delete or moved failed, the user need not be
	// informed at this point, as the restore operation may work anyway, and even
	// if it doesn't, the error can be reported then.
	if (!::CopyFile(strbpBlankDatabaseFile.Chars(), strbpTargetDatabaseFile.Chars(), true))
	{
		DWORD dwError = ::GetLastError();
		if (dwError == ERROR_FILE_NOT_FOUND)
		{
			BackupErrorHandler::ErrorBox(m_pbkpprg->Hwnd(),
				BackupErrorHandler::kRestoreFailure, kstidRstNoBlankMdfError);
		}
		else if (dwError == ERROR_FILE_EXISTS)
		{
			BackupErrorHandler::ErrorBox(m_pbkpprg->Hwnd(),
				BackupErrorHandler::kRestoreFailure, kstidRstNewMdfExistsError);
		}
		return false;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Backup specified database.  Since this must reset the Zip database name, don't do this until
	after unzipping the Restore files.

	@return 1 if all went well, else error code.
----------------------------------------------------------------------------------------------*/
int BackupHandler::BackupDatabaseBeforeOverwrite()
{
	StrUni stuSql;
	HRESULT hr;
	// Make distinct temporary subdirectory for this use:
	StrUni stuTempPath(m_strbpTempPath.Chars());
	stuTempPath.Append(L"Bk\\");
	StrAppBufPath strbpTempPath(stuTempPath.Chars());
	_wmkdir(stuTempPath.Chars()); // Make sure directory exists
	ClearFiles(strbpTempPath, L"*.bak"); //Clear any old contents that might interfere

	// Make temporary file name for MSDE to dump to:
	StrUni stuIntermediateFile;
	stuIntermediateFile.Format(L"%s%s%s",
		stuTempPath.Chars(), m_stuTargetDatabase.Chars(), L".bak");
	try
	{
		// Get MSDE to produce backup file:
		stuSql = L"BACKUP DATABASE ? TO DISK = ?";
		CheckHr(hr = m_qode->CreateCommand(&m_qodc));
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)m_stuTargetDatabase.Chars(),
			m_stuTargetDatabase.Length() * isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuIntermediateFile.Chars(),
			stuIntermediateFile.Length() * isizeof(OLECHAR)));
		CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	}
	catch (...)
	{
		return kstidRstZipError;
	}
	//Generate backup zip file name
	StrUni stuFileName;
	BackupFileNameProcessor::GenerateFileName(m_bkpi.m_strbDefaultDirectoryPath,
		m_stuProjectName, m_stuTargetDatabase, stuFileName);
	// Ensure that the default backup directory path actually exists (see LT-2123).
	MakeDir(m_bkpi.m_strbDefaultDirectoryPath.Chars());
	try
	{
		// Set up parameters for zip file:
		m_pFileZipper->SetZipFilename(stuFileName.Bstr());
		// Make sure zip file doesn't already exist, else zip may not do anything:
		StrApp strFileName(stuFileName.Chars());
		DeleteFile(strFileName);
		if (m_bkpi.m_bkppwi.m_strbPassword.Length() > 0)
		{
			StrUni stuPwd(m_bkpi.m_bkppwi.m_strbPassword);
			m_pFileZipper->SetEncryptionPassword(stuPwd.Bstr());
		}
		// Set the backup database name for the zipper
		m_pZipData->SetDBName(m_stuTargetDatabase.Bstr());
		// More zip parameters
		m_pFileZipper->SetPreservePaths(false);
		m_pFileZipper->SetProcessSubfolders(false);
		m_pFileZipper->SetFilesToProcess(stuIntermediateFile.Bstr());
		m_pFileZipper->SetCompressionLevel(9); // Maximum compression (slowest)
		m_pFileZipper->SetUseTempFile(false); // Allows more accurate progress bar

		// IMPORTANT NOTE: the following call may well result in an access violation
		// error. It appears that this can be ignored: simply press F5 again, and
		// agree to pass the exception to the program. (The error only occurs when
		// in VS.)
		long nResult = m_pFileZipper->Zip();

		// Test to see if there was an error:
		StrApp strZipFile(stuFileName);
		bool fBackupOk = !m_pFileZipper->TestError(nResult, strZipFile.Chars());
		if (!fBackupOk)
			return kstidRstZipError;
	}
	catch (...)
	{
		return kstidRstZipError;
	}
	return 1;
}

/*----------------------------------------------------------------------------------------------
	Remove temporary database files or recover them.
	@return 1 if all went well, else error code.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::RecoverFromFailedRestore()
{
	StrUni stuSql;
	HRESULT hr;

	// Restore failed, so we need to delete any remnant of the attempted restore DB:
	// It doesn't matter if this query fails, as we don't know to what extent the
	// restore was successful:
	// Note: Using SetParameter here always gives illegal syntax, so don't try it.
	stuSql.Format(L"DROP DATABASE [%s]", m_stuTargetDatabase.Chars());
	CheckHr(hr = m_qode->CreateCommand(&m_qodc));
	m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults);

	// Just to make sure, try and delete the DB files that restore tried to create:
	DeleteFile(m_stuTargetMdf.Chars());
	DeleteFile(m_stuTargetLdf.Chars());
	// Now try to put back the original files:
	bool Recovered = true; // Assume all will go well
	if (!::MoveFile(m_strbpDbMdfCache.Chars(), m_strbpDbMdfCurrent.Chars()))
		Recovered = false;
	else if (!::MoveFile(m_strbpDbLdfCache.Chars(), m_strbpDbLdfCurrent.Chars()))
		Recovered = false;

	if (!Recovered)
	{
		BackupErrorHandler::ErrorBox(NULL, BackupErrorHandler::kWarning,
			kstidRstDbRecvrError);
		return false;
	}
	else
	{
		return BackupHandler::AttachDatabase();
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Uses the values in the member variables m_strbpDbMdfCurrent and m_strbpDbLdfCurrent to
	attach the database.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::AttachDatabase()
{
	try
	{
		StrUni stuTargetMdf(m_strbpDbMdfCurrent.Chars());
		StrUni stuTargetLdf(m_strbpDbLdfCurrent.Chars());
		StrUni stuSql = L"EXEC sp_attach_db ?, ?, ?";
		int hr;
		CheckHr(hr = m_qode->CreateCommand(&m_qodc));
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)m_stuTargetDatabase.Chars(), m_stuTargetDatabase.Length()
			* isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuTargetMdf.Chars(), stuTargetMdf.Length() * isizeof(OLECHAR)));
		CheckHr(m_qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)stuTargetLdf.Chars(), stuTargetLdf.Length() * isizeof(OLECHAR)));
		CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	}
	catch (...)
	{
		BackupErrorHandler::ErrorBox(NULL, BackupErrorHandler::kWarning,
			kstidRstDbRecvrAttachError);
		return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get MSDE to tell us which file is used for the given database.
	@param strbpFile [out] The full path of the MSDE file
	@return True if all went well.
----------------------------------------------------------------------------------------------*/
bool BackupHandler::GetMsdeFileName(StrAppBufPath & strbpFile)
{
	StrUni stuSql;
	bool fResult = true; // Assume all will go OK.
	HRESULT hr;

	try
	{
		stuSql = L"select filename from sysdatabases where name = ?";
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(BYTE *)m_stuTargetDatabase.Chars(), m_stuTargetDatabase.Length() * isizeof(OLECHAR)));
		CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(hr = m_qodc->GetRowset(0));
		ComBool fMoreRows;
		CheckHr(hr = m_qodc->NextRow(&fMoreRows));

		if (fMoreRows)
		{
			// Fetch file path:
			OLECHAR rgchPath[MAX_PATH];
			UINT luSpaceTaken;
			ComBool fIsNull;
			CheckHr(hr = m_qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchPath),
				isizeof(OLECHAR) * MAX_PATH, &luSpaceTaken, &fIsNull, 2));
			if (fIsNull)
				fResult = false;
			else
				strbpFile.Assign(rgchPath);
		}
		else
			fResult = false;
	}
	catch (...)
	{
		fResult = false;
	}

	return fResult;
}

/*----------------------------------------------------------------------------------------------
	Set the local server to the given value.  This is used by test code.
----------------------------------------------------------------------------------------------*/
void BackupHandler::SetLocalServer(const wchar * pszServerName)
{
	s_stuLocalServer = pszServerName;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the local server name.  This is used by test code.
----------------------------------------------------------------------------------------------*/
StrUni BackupHandler::GetLocalServer()
{
	return s_stuLocalServer;
}

/*----------------------------------------------------------------------------------------------
	Set the instance handle so that we can load icons.  This is used by test code.
----------------------------------------------------------------------------------------------*/
void BackupHandler::SetInstanceHandle()
{
	s_hinst = ModuleEntry::GetModuleHandle();
}


//:>********************************************************************************************
//:>	BackupHandler::LogFile methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
BackupHandler::LogFile::LogFile()
{
	m_fileLog = NULL;
	Initiate();
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
BackupHandler::LogFile::~LogFile()
{
	Terminate();
}

/*----------------------------------------------------------------------------------------------
	Initiates the Backup log file. File will be created if it doesn't already exist.
----------------------------------------------------------------------------------------------*/
void BackupHandler::LogFile::Initiate()
{
	// Get the FieldWorks root data directory:
	StrAppBufPath strbpFwRootDir;
	strbpFwRootDir.Assign(DirectoryFinder::FwRootDataDir().Chars());

	StrAppBufPath strbp(strbpFwRootDir.Chars(), strbpFwRootDir.Length());
	if (strbp.Length() == 0 ||
		strbp[strbp.Length() - 1] != '\\')
	{
		strbp.Append(_T("\\"));
	}
	strbp.Append(_T("Backup.log"));

	// Create/open the log file:
	_tfopen_s(&m_fileLog, strbp.Chars(), _T("a"));
}


/*----------------------------------------------------------------------------------------------
	Writes initial text to Backup log file, based on how Backup was triggered.
	@param trigger Enumerated value indicating how Backup was started.
----------------------------------------------------------------------------------------------*/
void BackupHandler::LogFile::Start(VTrigger trigger)
{
	TimeStamp();
	Write(_T(" Backup started "));
	switch (trigger)
	{
	case kManual:
		Write(_T("manually by user"));
		break;
	case kReminderAccepted:
		Write(_T("having reminded user, and user accepting"));
		break;
	case kExternal:
		Write(_T("externally (probably by Task Scheduler)"));
		break;
	}
	Write(_T(".\n"));
}


/*----------------------------------------------------------------------------------------------
	Writes given text to Backup log file.
	@param szText Text to be written
----------------------------------------------------------------------------------------------*/
void BackupHandler::LogFile::Write(const achar * szText)
{
	if (m_fileLog)
	{
		_fputts(szText, m_fileLog);
	}
}


/*----------------------------------------------------------------------------------------------
	Writes text of given resource id to Backup log file.
----------------------------------------------------------------------------------------------*/
void BackupHandler::LogFile::Write(int stid)
{
	StrApp str(stid);
	Write(str.Chars());
}


/*----------------------------------------------------------------------------------------------
	Writes the current date and time to the Backup log file.
----------------------------------------------------------------------------------------------*/
void BackupHandler::LogFile::TimeStamp()
{
	if (m_fileLog)
	{
		SYSTEMTIME syst;
		GetLocalTime(&syst);

		StrApp str;
		str.Format(_T("%04d-%02d-%02d %02d:%02d:%02d"), syst.wYear, syst.wMonth, syst.wDay,
			syst.wHour, syst.wMinute, syst.wSecond);
		_fputts(str.Chars(), m_fileLog);
	}
}


/*----------------------------------------------------------------------------------------------
	Closes down the log file, in case we wish to restart before object goes out of scope.
----------------------------------------------------------------------------------------------*/
void BackupHandler::LogFile::Terminate()
{
	if (m_fileLog)
	{
		Write(_T("Backup ended at "));
		TimeStamp();
		Write(_T(".\n\n"));
		fclose(m_fileLog);
		m_fileLog = NULL;
	}
}




//:>********************************************************************************************
//:>	BackupErrorHandler methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Report general messages.
	@param hwnd Handle of parent window for messagebox.
	@param nCategory Reflects the severity of the error; whether fatal or not.
	@param rid The resource ID of the text message.
	@param uType Buttons and icons to go on messagebox.
	@return The messagebox return value, indicating which button the user pressed.
----------------------------------------------------------------------------------------------*/
int BackupErrorHandler::MessageBox(HWND hwnd, int nCategory, int ridTitle, int ridMessage,
	UINT uType, ...)
{
	//Set default title if applicable
	if (ridTitle == kstidUseDefault) ridTitle = kstidBkpSystem;

	StrAppBuf strbTitle(ridTitle);
	StrApp strFormat(ridMessage);
	StrApp strCategory;
	switch (nCategory)
	{
	case kBackupFailure:
		strCategory.Load(kstidBkpFailure);
		break;
	case kBackupPossibleFailure:
		strCategory.Load(kstidBkpPossibleFailure);
		break;
	case kBackupNonFatalFailure:
		strCategory.Load(kstidBkpNonFatalFailure);
		break;
	case kRestoreFailure:
		strCategory.Load(kstidRstFailure);
		break;
	case kRestorePossibleFailure:
		strCategory.Load(kstidRstPossibleFailure);
		break;
	case kRestoreNonFatalFailure:
		strCategory.Load(kstidRstNonFatalFailure);
		break;
	case kWarning:
		// Do nothing:
		break;
	default:
		Assert(false);
		break;
	}
	if (strCategory.Length() > 0)
	{
		strFormat.Append(" ");
		strFormat.Append(strCategory.Chars());
	}

	// Format the string:
	StrApp strMessage;
	va_list argList;
	va_start(argList,uType);
	strMessage.FormatCore(strFormat.Chars(), strFormat.Length(), argList);
	va_end(argList);

	return ::MessageBox(hwnd, strMessage.Chars(), strbTitle.Chars(), uType);
}

/*----------------------------------------------------------------------------------------------
	Report error messages.
	@param hwnd Handle of parent window for messagebox.
	@param nCategory Reflects the severity of the error; whether fatal or not.
	@param rid The resource ID of the text message describing the error.
	@param nLastError Optional result of GetLastError() Windows API call.
----------------------------------------------------------------------------------------------*/
void BackupErrorHandler::ErrorBox(HWND hwnd, int nCategory, int ridMessage, int nLastError,
	const achar * pszZipFile)
{
	int iIcon = 0;
	switch (nCategory)
	{
	case kBackupFailure:
	case kBackupNonFatalFailure: // Fall through
	case kRestoreFailure: // Fall through
	case kRestoreNonFatalFailure: // Fall through
		iIcon = MB_ICONSTOP;
		break;
	case kRestorePossibleFailure:
	case kBackupPossibleFailure:
	case kWarning:
		iIcon = MB_ICONEXCLAMATION;
		break;
	default:
		Assert(false);
		break;
	}

	if (!nLastError)
	{
		BackupErrorHandler::MessageBox(hwnd, nCategory, kstidUseDefault, ridMessage,
			iIcon | MB_OK, pszZipFile);
	}
	else
	{
		// Get system text message relating to GetLastError()
		LPVOID lpMsgBuf;
		FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM |
			FORMAT_MESSAGE_IGNORE_INSERTS, NULL, nLastError,
			MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),  (LPTSTR) &lpMsgBuf, 0, NULL);
		StrApp str(ridMessage);
		BackupErrorHandler::MessageBox(hwnd, nCategory, kstidUseDefault, kstidBkpSystemError,
			iIcon | MB_OK, str.Chars(), (LPCTSTR)lpMsgBuf);
		LocalFree(lpMsgBuf);
	}
}




/***********************************************************************************************
	DIFwBackupDb implementation
***********************************************************************************************/


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.DbServices.Backup"),
	&CLSID_FwBackup,
	_T("SIL Database Services Backup"),
	_T("Apartment"),
	&FwBackupDb::CreateCom);


IMPLEMENT_SIL_DISPATCH(DIFwBackupDb, &IID_DIFwBackupDb, &LIBID_FwDbServices)


/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
FwBackupDb::FwBackupDb()
{
	// Make sure the fancy controls we'll be using  are available:
	INITCOMMONCONTROLSEX iccex = { sizeof(iccex), ICC_USEREX_CLASSES | ICC_LISTVIEW_CLASSES };
	::InitCommonControlsEx(&iccex);
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
	m_hwndParent = NULL;
	s_stuLocalServer.Clear();
	s_qfist = NULL;
	s_hinst = ModuleEntry::GetModuleHandle();
	m_fInitDone = false;
	m_bstrHelpFile = NULL;
	m_pbstrHelpTopic = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
FwBackupDb::~FwBackupDb()
{
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	IUnknown Methods
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwBackupDb::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IDispatch)
		*ppv = static_cast<IDispatch *>(this);
	else if (riid == IID_DIFwBackupDb)
		*ppv = static_cast<DIFwBackupDb *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_DIFwBackupDb);
		return NOERROR;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

STDMETHODIMP_(ULONG) FwBackupDb::AddRef(void)
{
	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}


STDMETHODIMP_(ULONG) FwBackupDb::Release(void)
{
	Assert(m_cref > 0);
	ulong cref = ::InterlockedDecrement(&m_cref);
	if (!cref)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}

/*----------------------------------------------------------------------------------------------
	Create class via COM
----------------------------------------------------------------------------------------------*/
void FwBackupDb::CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv)
{
	HRESULT hr;
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwBackupDb> qzbkup;
	qzbkup.Attach(NewObj FwBackupDb);	// ref count initially 1
	CheckHr(hr = qzbkup->QueryInterface(riid, ppv));
}



/*----------------------------------------------------------------------------------------------
	Initialize, and set host DbApp.
	@param pbkupd DbApp host derived from IBackupDelegates.
	@param hwndParent main wnd of host DbApp.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwBackupDb::Init(IBackupDelegates * pbkupd, int hwndParent)
{
	BEGIN_COM_METHOD;
	if (!pbkupd)
		ThrowInternalError(E_POINTER);

	m_hwndParent = (HWND)hwndParent;
	SmartBstr sbstrLocalServer;
	CheckHr(pbkupd->GetLocalServer_Bkupd(&sbstrLocalServer));
	if (s_stuLocalServer.Length() > 0)
		ThrowHr(WarnHr(E_UNEXPECTED)); // only one backup dlg may be active until other closed.
	s_stuLocalServer = sbstrLocalServer.Chars();
	// Get the IStream pointer for logging. NULL returned if no log file.
	CheckHr(pbkupd->GetLogPointer_Bkupd(&s_qfist));
	m_bkph.Init(pbkupd);
	m_fInitDone = true;

	END_COM_METHOD(g_fact, IID_DIFwBackupDb)
}


/*----------------------------------------------------------------------------------------------
	Must call when done.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwBackupDb::Close()
{
	BEGIN_COM_METHOD;
	s_stuLocalServer.Clear();
	s_qfist = NULL;
	s_hinst = NULL;
	END_COM_METHOD(g_fact, IID_DIFwBackupDb)
}

/*----------------------------------------------------------------------------------------------
	Initialize, for internal use when an external caller has no DbApp to use as a host.
----------------------------------------------------------------------------------------------*/
void FwBackupDb::InitNoHost()
{
	m_hwndParent = NULL;

	// Generate local server name:
	achar psz[MAX_COMPUTERNAME_LENGTH + 1];
	ulong cch = isizeof(psz);
	::GetComputerName(psz, &cch);
	StrUni stuMachine(psz);
	s_stuLocalServer.Format(L"%s\\SILFW", stuMachine.Chars());
	s_qfist = NULL;
	m_bkph.Init(NULL);
}

/*----------------------------------------------------------------------------------------------
	See if a scheduled backup has been missed, and offer to do it if so.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwBackupDb::CheckForMissedSchedules(IUnknown * phtprovHelpUrls)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(phtprovHelpUrls);

	HRESULT hr;
	IHelpTopicProviderPtr qhtprv = NULL;
	if (phtprovHelpUrls)
		CheckHr(hr = phtprovHelpUrls->QueryInterface(IID_IHelpTopicProvider, (void **)&qhtprv));
	m_bkph.CheckForMissedSchedules(m_hwndParent, qhtprv);

	END_COM_METHOD(g_fact, IID_DIFwBackupDb)
}

/*----------------------------------------------------------------------------------------------
	Do backup.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwBackupDb::Backup()
{
	BEGIN_COM_METHOD;

	if (!m_fInitDone)
		InitNoHost();

	m_bkph.Backup(BackupHandler::kExternal, m_hwndParent);
	// Here we ignore the return code. The Backup.log file in the FW data folder will contain
	// details of any error. Returning an error code will cause a script engine error to be
	// displayed, which is confuding to the user. See JIRA issue LT-6421.

	END_COM_METHOD(g_fact, IID_DIFwBackupDb)
}

/*----------------------------------------------------------------------------------------------
	Remind the user that it is time to do a backup.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwBackupDb::Remind()
{
	BEGIN_COM_METHOD;

	if (!m_fInitDone)
		InitNoHost();

	m_bkph.Remind(-1, m_hwndParent);

	END_COM_METHOD(g_fact, IID_DIFwBackupDb)
}

/*----------------------------------------------------------------------------------------------
	Allow the user to configure the backup and restore settings.
	@param phtprovHelpUrls pointer to a help topic provider to get app-specific information
	about the help file and topic for this dialog.
	@param fShowRestore True if Restore tab should be the initial tab shown.
	@param pnUserAction [out] Integer value, describing what user did.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwBackupDb::UserConfigure(IUnknown * phtprovHelpUrls,
									   ComBool fShowRestore, int * pnUserAction)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnUserAction);
	HRESULT hr;

	if (!m_fInitDone)
		InitNoHost();

	IHelpTopicProviderPtr qhtprov = NULL;
	if (phtprovHelpUrls)
		CheckHr(hr = phtprovHelpUrls->QueryInterface(IID_IHelpTopicProvider, (void **)&qhtprov));

	m_bkph.LimitBackupToActive();
	*pnUserAction = m_bkph.UserConfigure(m_hwndParent, (bool)fShowRestore, qhtprov);

	END_COM_METHOD(g_fact, IID_DIFwBackupDb)
}
