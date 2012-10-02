/*
	File ServiceWrapper.cpp

	This file conatains all functions for making this program run as a Windows Service.
	(See ms-help://MS.VSCC/MS.MSDNVS/dllproc/services_0p0z.htm)

	IMPORTANT: Before compiling this file, check the name of the account on which this
	program will run, and add the correct password, both in the call to CreateService() later
	in this file

	To use it, run the executable with -y option to start service, and -n to stop it.
	Running the executable with no flags is undefined, as that is how the Windows Service
	Controller runs it, and it does some funny stuff then.

*/

#include "stdafx.h"

// Variables needed in various places in this file:
SERVICE_STATUS ssStatus; // General status of service calls
SERVICE_STATUS_HANDLE ssStatusHandle; // Handle to our service

// Forward declarations:
VOID WINAPI ServiceStart(DWORD argc, LPTSTR *argv);
void WINAPI ServiceCtrlHandler(DWORD opcode);
DWORD ServiceInitialization(DWORD argc, LPTSTR *argv, DWORD *specificError);
DWORD InstallService(void);
void UninstallService(void);
void SvcDebugOut(LPSTR String, DWORD Status);

// Global flags:
bool fStopRequested = false;
HANDLE hEventStopper = NULL;

// Service name:
#define SERVICE_NAME "AutoSurveyor"

// Main entry point:
int main(int argc, char * argv[])
{
	// Normal use is with command line argument either -y or -n :
	if (argc > 1)
	{
		if (stricmp(argv[1], "-y") == 0)
		{
			return InstallService();
		}
		else if (stricmp(argv[1], "-n") == 0)
		{
			UninstallService();
			return 0;
		}
	}
	else // called by Windows Service Controller.
	{
		// Connect the main thread of our service process to the service control manager:
		SERVICE_TABLE_ENTRY DispatchTable[] = { { SERVICE_NAME, ServiceStart }, { NULL, NULL } };
		if (!StartServiceCtrlDispatcher(DispatchTable))
		{
			printf("StartServiceCtrlDispatcher error = %d\n", GetLastError());
			printf("\nCheck usage: Scheduler -y to install and start service\n");
			printf("\n             Scheduler -n to stop and uninstall service\n");
		}
	}
	return 0;
}

// This is the entry point if our service has just been told to start.
void WINAPI ServiceStart(DWORD argc, LPTSTR *argv)
{
	DWORD status;
	DWORD specificError;

	// Set up status info:
	ssStatus.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
	ssStatus.dwCurrentState = SERVICE_START_PENDING;
	ssStatus.dwControlsAccepted = SERVICE_ACCEPT_STOP | SERVICE_ACCEPT_SHUTDOWN;
	ssStatus.dwWin32ExitCode = 0;
	ssStatus.dwServiceSpecificExitCode = 0;
	ssStatus.dwCheckPoint = 0;
	ssStatus.dwWaitHint = 0;

	// Get handle to our service:
	ssStatusHandle = RegisterServiceCtrlHandler(SERVICE_NAME, ServiceCtrlHandler);
	if (ssStatusHandle == (SERVICE_STATUS_HANDLE)0)
	{
		SvcDebugOut("RegisterServiceCtrlHandler failed %d\n", GetLastError());
		return;
	}

	// Perform any Initialization needed:
	status = ServiceInitialization(argc, argv, &specificError);

	// Handle error condition
	if (status != NO_ERROR)
	{
		ssStatus.dwCurrentState = SERVICE_STOPPED;
		ssStatus.dwCheckPoint = 0;
		ssStatus.dwWaitHint = 0;
		ssStatus.dwWin32ExitCode = status;
		ssStatus.dwServiceSpecificExitCode = specificError;

		SetServiceStatus(ssStatusHandle, &ssStatus);
		return;
	}

	// Initialization complete - report running status.
	ssStatus.dwCurrentState = SERVICE_RUNNING;
	ssStatus.dwCheckPoint = 0;
	ssStatus.dwWaitHint = 0;
	if (!SetServiceStatus(ssStatusHandle, &ssStatus))
	{
		status = GetLastError();
		SvcDebugOut("SetServiceStatus error %ld\n", status);
	}

	// This is where the service does its work. In our case, we are going to schedule
	// our own tasks:
	RunScheduler();

	return;
}

// Respond to messages sent from Windows Service Controller.
void WINAPI ServiceCtrlHandler(DWORD Opcode)
{
	DWORD status;

	switch(Opcode)
	{
		case SERVICE_CONTROL_STOP:
			fStopRequested = true;
			::SetEvent(hEventStopper);
			ssStatus.dwWin32ExitCode = 0;
			ssStatus.dwCurrentState = SERVICE_STOPPED;
			ssStatus.dwCheckPoint = 0;
			ssStatus.dwWaitHint = 0;

			if (!SetServiceStatus(ssStatusHandle, &ssStatus))
			{
				status = GetLastError();
				SvcDebugOut("SetServiceStatus error %ld\n",status);
			}

			SvcDebugOut("Leaving Service \n",0);
			return;

		case SERVICE_CONTROL_INTERROGATE:
			// Fall through to send current status.
			break;

		default:
			SvcDebugOut("Unrecognized opcode %ld\n",
				Opcode);
	}

	// Send current status.
	if (!SetServiceStatus(ssStatusHandle, &ssStatus))
	{
		status = GetLastError();
		SvcDebugOut("SetServiceStatus error %ld\n",status);
	}
	return;
}

// Perform initialization of our service.
DWORD ServiceInitialization(DWORD argc, LPTSTR *argv, DWORD *specificError)
{
	fStopRequested = false;
	// Create a rest event, so that we can signal it if we have to stop our service.
	hEventStopper = ::CreateEvent(NULL, true, false, NULL);
	return 0;
}

// Install and start our service.
DWORD InstallService(void)
{
	DWORD dwStatus = -1; // Assume the worst to start with.

	// Get access to the Windows Service Controller:
	SC_HANDLE schSCManager = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);
	if (schSCManager)
	{
		// Get our executable's file path in a quoted string:
		char _FilePath[MAX_PATH];
		char FilePath[MAX_PATH];
		::GetModuleFileName(NULL, _FilePath, MAX_PATH);
		sprintf(FilePath, "\"%s\"", _FilePath);

		// Create a service pointing at ourselves:
		SC_HANDLE schService = CreateService(
			schSCManager,				// SCManager database
			SERVICE_NAME,				// name of service
			SERVICE_NAME,				// service name to display
			SERVICE_ALL_ACCESS,			// desired access
			SERVICE_WIN32_OWN_PROCESS,	// service type
			SERVICE_AUTO_START,			// start type
			SERVICE_ERROR_NORMAL,		// error control type
			FilePath,					// service's binary
			NULL,						// no load ordering group
			NULL,						// no tag identifier
			NULL,						// no dependencies
			"Dallas\\fwbuilder",		// system account
			"");						// password FILL THIS IN BEFORE COMPILING
			// DO NOT CHECK IN THIS FILE WITH THE PASSWORD STILL IN IT!

		if (schService)
		{
			// Start the service:
			if (StartService(schService, 0, NULL))
			{
				// Check the status until the service is no longer start-pending:
				if (QueryServiceStatus(schService, &ssStatus))
				{
					// Save the tick count and initial checkpoint:
					DWORD dwStartTickCount = GetTickCount();
					DWORD dwOldCheckPoint = ssStatus.dwCheckPoint;

					while (ssStatus.dwCurrentState == SERVICE_START_PENDING)
					{
						// Do not wait longer than the wait hint. A good interval is
						// one tenth the wait hint, but no less than 1 second and no
						// more than 10 seconds.
						DWORD dwWaitTime = ssStatus.dwWaitHint / 10;

						if (dwWaitTime < 1000)
							dwWaitTime = 1000;
						else if (dwWaitTime > 10000)
							dwWaitTime = 10000;

						Sleep(dwWaitTime);

						// Check the status again:
						if (!QueryServiceStatus(schService, &ssStatus))
							break;

						if (ssStatus.dwCheckPoint > dwOldCheckPoint)
						{
							// The service is making progress:
							dwStartTickCount = GetTickCount();
							dwOldCheckPoint = ssStatus.dwCheckPoint;
						}
						else
						{
							if (GetTickCount() - dwStartTickCount > ssStatus.dwWaitHint)
							{
								// No progress made within the wait hint:
								break;
							}
						}
					}

					if (ssStatus.dwCurrentState == SERVICE_RUNNING)
					{
						printf("Service started.\n");
						dwStatus = NO_ERROR;
					}
					else
					{
						printf("\nService not started. \n");
						printf("  Current State: %d\n", ssStatus.dwCurrentState);
						printf("  Exit Code: %d\n", ssStatus.dwWin32ExitCode);
						printf("  Service Specific Exit Code: %d\n", ssStatus.dwServiceSpecificExitCode);
						printf("  Check Point: %d\n", ssStatus.dwCheckPoint);
						printf("  Wait Hint: %d\n", ssStatus.dwWaitHint);
						dwStatus = GetLastError();
					}
				} // End if QueryServiceStatus() worked.
			} // End if StartService() worked.
			CloseServiceHandle(schService);
		} // End if CreateService() worked.
		CloseServiceHandle(schSCManager);
	} // End if OpenSCManager() worked.

	if (dwStatus == -1)
		printf("Error starting service.\n");

	return dwStatus;
}

// Stop and delete our service.
void UninstallService(void)
{
	bool fError = true; // Assume the worst to start with.

	// Get access to the Windows Service Controller:
	SC_HANDLE schSCManager = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);
	if (schSCManager)
	{
		// Get our particular service:
		SC_HANDLE schService = OpenService(schSCManager, SERVICE_NAME, SERVICE_ALL_ACCESS);
		if (schService)
		{
			// Tell it to stop:
			ControlService(schService, SERVICE_CONTROL_STOP, &ssStatus);
			// Delete it:
			if (DeleteService(schService))
			{
				printf("Service stopped.\n");
				fError = false;
			}
			CloseServiceHandle(schService);
		}
		CloseServiceHandle(schSCManager);
	}

	if (fError)
		printf("Error stopping service.\n");
}

// General error output when debugging.
void SvcDebugOut(LPSTR String, DWORD Status)
{
	CHAR  Buffer[1024];
	if (strlen(String) < 1000)
	{
		sprintf(Buffer, String, Status);
		OutputDebugStringA(Buffer);
	}
}
