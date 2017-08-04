/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ModuleEntry.h
Responsibility: Darrell Zook
Last reviewed: 9/8/99

Description:
	To receive notification of server level events, subclass ModuleEntry and create a global
	instance of the subclass. Then override the following virtual functions for the server
	level events you want to receive notifications for:
		ProcessAttach
		ThreadAttach (only sent to DLLs)
		ThreadDetach (only sent to DLLs)
		ProcessDetach
		GetClassFactory
		RegisterServer
		UnregisterServer
		CanUnload

	Executable modules that use this code fall into the following categories:
		1) non-MFC EXE Servers (#define EXE_MODULE)
		2) non-MFC DLLs
		3) MFC ActiveX Controls (#define USING_MFC and USING_MFC_ACTIVEX)
		4) MFC DLLs (#define USING_MFC)
		5) MFC EXE Servers (#define USING_MFC and EXE_MODULE, and see below)

	MFC DLLs
	--------
	The following changes must be done for MFC DLLs to work properly:

	1)	Define USING_MFC.
	2)	Add the following files (from Src\Generic) to your project:
			DllModul.cpp
			ModuleEntry.cpp
			Util.cpp
			UtilString.cpp
			UtilTime.cpp
		Turn off precompiled headers for all of the above files except DllModul.cpp. To do
		this, go to the Project\Settings menu item. In the tree on the left, open up the
		Source Files folder underneath your project. Select one of the files to be changed.
		On the right, select the "C/C++" tab. Select the "Precompiled Headers" category.
		Check the "Not using precompiled headers." option box. Then select the other three
		files and do the same thing to those.
	3)	Go to the Project\Settings menu. Click on your project in the tree on the left.
		Select the "C/C++ tab" on the right. Select the "Preprocessor" category. In the
		"Additional include directories" box, enter the current directory and the path to
		Src\Generic separated by a comma. (i.e. ".,..\..\generic" without the quotes)
	4)	In your Main.h file, #include "common.h"
			This file contains support for our common classes.
	5)	In your Stdafx.h file, #include "main.h"


	MFC ActiveX Controls
	--------------------
	Some additional steps are required for MFC ActiveX controls to work properly.
	The steps in the section above on MFC DLLs need to be done as well as the following:

	1)	Define USING_MFC_ACTIVEX.
	2)	In your Main.h file, #include <afxctl.h>
			This fils contains MFC support for ActiveX controls.
	3)	Delete the following functions in your main CPP file:
			STDAPI DllRegisterServer(void)
			STDAPI DllUnregisterServer(void)
	4)	Create a subclass of ModuleEntry and override the following functions as shown:
			virtual void GetClassFactory(REFCLSID clsid, REFIID iid, void ** ppv)
			{
				AFX_MANAGE_STATE(AfxGetStaticModuleState());
				CheckHr(AfxDllGetClassObject(clsid, iid, ppv));
			}
			virtual bool CanUnload()
			{
				AFX_MANAGE_STATE(AfxGetStaticModuleState());
				return AfxDllCanUnloadNow();
			}
		Now create a global instance of this subclass.
	5)	Add a call to ModuleEntry::ModuleAddRef() in the InitInstance method of your
		base class.
	6)	Add a call to ModuleEntry::ModuleRelease() in the ExitInstance method of your
		base class.

	These steps are all required for MFC ActiveX controls to work properly. Other methods of
	the subclass you created in step 4 can be overridden as desired.

  MFC EXEs
  --------
  Additional steps:
  1) call ModuleEntry::Startup(m_hInstance, m_lpCmdLine) from your CWinApp subclass's
	InitInstance() method. If it returns true, quit without showing your main window
	(the app was called just for registering or unregistering)
  2) call ModuleEntry::Shutdown() from your CWinApp subclass's ExitInstance method.

  Note that Startup() calls CoInitialize, and Shutdown calls CoUninitialize. Hence if you are
  doing other initialization or cleanup, calling Startup() should come early, and Shutdown()
  should come late.


-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef ModuleEntry_H
#define ModuleEntry_H 1

/*----------------------------------------------------------------------------------------------
	The constructor of LLBase (defined in Src\Generic\LinkedList.h) adds this ModuleEntry to
		the head of a static linked list of ModuleEntries in this module. The server level
		events are then passed along to every ModuleEntry in the linked list by calling the
		appropriate virtual function.

	Hungarian: me
----------------------------------------------------------------------------------------------*/
class ModuleEntry : public LLBase<ModuleEntry>
{
protected:
	// A handle to the module instance. This value can be used for any Windows APIs that
	// require an HINSTANCE or HMODULE (such as loading resources stored within a module).
	static HMODULE s_hmod;

	// The reference count for the module. This is used by DllCanUnloadNow (for DLLs) and
	// by EXEs to determine when the module can be shut down. This is changed by ModuleAddRef
	// and ModuleRelease.
	static long s_crefModule;

	// Points to the head of a linked list of ModuleEntries.
	static ModuleEntry * s_pmeFirst;

	// Cached value of the module pathname.
#if WIN32
	static StrAppBufPath ModuleEntry::s_strbpPath;
#else
	static TCHAR s_strbpPath[MAX_PATH];
#endif

	// The main thread ID of an EXE module.  Zero for non-EXE modules.
	static ulong s_tid;

	// Supports knowing whether the component is a DLL or EXE; convenient for some other
	// generic code which otherwise would need conditional compilation.
	static bool s_fIsExe;


	// true to register classes under HKCU instead of HKCR
	static bool s_fPerUserRegistration;

public:

	static void SetPerUserRegistration(bool fPerUser)
	{
		s_fPerUserRegistration = fPerUser;
	}

	static bool PerUserRegistration()
	{
		return s_fPerUserRegistration;
	}

	static bool IsExe()
	{
		return s_fIsExe;
	}

	static ulong MainThreadId()
	{
		return s_tid;		// Always zero for non-EXE modules.
	}

#ifdef EXE_MODULE
	// Data placed on the clipboard by this program.
	static IDataObjectPtr s_qdobjClipboard;
#ifdef USING_MFC

	static bool Startup(HINSTANCE hinst, LPSTR pszCmdLine);
	static void ShutDown();
#else // !USING_MFC, but still EXE_MODULE

	// This is the main entry point into the EXE.
	static int WinMain(HINSTANCE hinst, HINSTANCE hinstPrev, LPSTR pszCmdLine, int nShowCmd);

	// Run is declared here but must be provided by client code.
	static int Run(HINSTANCE hinst, LPSTR pszCmdLine, int nShowCmd);
#endif // !USING_MFC

#else // !EXE_MODULE

	// This is the main entry point into the DLL.
	static BOOL DllMain(HMODULE hmod, DWORD dwReason);
#endif // !EXE_MODULE


	// These methods implement DllGetClassObject, DllRegisterServer, DllUnregisterServer, and
	// DllCanUnloadNow, which are required for all COM DLLs.
	static HRESULT ModuleGetClassObject(REFCLSID clsid, REFIID iid, void ** ppv);
	static HRESULT ModuleRegisterServer(void);
	static HRESULT ModuleUnregisterServer(void);
	static HRESULT ModuleCanUnloadNow(void);

	static HRESULT ModuleProcessAttach(void);
	static HRESULT ModuleThreadAttach(void);
	static HRESULT ModuleThreadDetach(void);
	static HRESULT ModuleProcessDetach(void);

	static HMODULE GetModuleHandle(void)
		{ return s_hmod; }
	static LPCTSTR GetModulePathName(void);

	static void SetClipboard(IDataObject * pdobjClipboard);

	// These methods increment and decrement the reference count for the module. A module
	// will not be unloaded from memory as long as something is still referencing it.
	// If these are not called properly, the module might be unloaded from memory too early,
	// or it might not be unloaded from memory at all. ModuleAddRef should be called in the
	// constructor of each class that can be accessed outside of the DLL/EXE it is in. The
	// destructor should then call ModuleRelease.
	// NOTE: These must be thread safe.
	static long ModuleAddRef(void)
		{ return InterlockedIncrement(&s_crefModule); }
	static long ModuleRelease(void);

	// Constructor and destructor
	ModuleEntry(void);
	virtual ~ModuleEntry(); // Although we never new/delete ModuleEntrys, avoid warnings

	// These four virtual methods should be overridden if you want to receive notification
	// of any of these module entry events. For more information about module entry points,
	// see the article in MSDN on DllMain. EXEs will not receive ThreadAttach or ThreadDetach
	// messages. They will, however, receive ProcessAttach (when the program starts--before
	// the Run method is called), and ProcessDetach (when the program exits--after the
	// Run method is called) messages.
	virtual void ProcessAttach(void)
		{ }
	virtual void ThreadAttach(void)
		{ }
	virtual void ThreadDetach(void)
		{ }
	virtual void ProcessDetach(void)
		{ }

	// These four virtual methods should be overridden if you want to receive notification
	// of any of these COM events. They are called for all ModuleEntries in the linked
	// list when the corresponding COM methods are called. Both DLLs and EXEs can override
	// these methods if desired.
	virtual void GetClassFactory(REFCLSID clsid, REFIID iid, void ** ppv)
	{
		// At this point, ppv should be non-NULL (from ModuleEntry::ModuleGetClassObject).
		AssertPtr(ppv);
		Assert(!*ppv);
	}
	virtual void RegisterServer(void)
		{ }
	virtual void UnregisterServer(void)
		{ }
	virtual bool CanUnload(void)
		{ return true; }
};


#endif // !ModuleEntry_H
