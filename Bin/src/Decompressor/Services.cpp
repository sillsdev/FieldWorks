#pragma once

#include "Services.h"
#include "StatusDialog.h"

ServiceManager_t::ServiceManager_t()
{
	// Open a handle to the SC Manager database:
	schSCManager = OpenSCManager(
		NULL,					// local machine
		NULL,					// ServicesActive database
		SC_MANAGER_ALL_ACCESS);	// full access rights

	if (NULL == schSCManager)
		LogError(_T("OpenSCManager failed (%d)"), GetLastError());
}

/*
This function attempts to start a service.

Parameters:
pszServiceName - service name string

If the operation is successful, returns true. Otherwise, returns false.
*/
bool ServiceManager_t::StartService(const _TCHAR * pszServiceName)
{
	SC_HANDLE schService;
	SERVICE_STATUS_PROCESS ssStatus;
	DWORD dwOldCheckPoint;
	DWORD dwStartTickCount;
	DWORD dwWaitTime;
	DWORD dwBytesNeeded;

	schService = OpenService(schSCManager, pszServiceName, SERVICE_ALL_ACCESS);

	if (schService == NULL)
	{
		LogError(_T("Could not open service '%s'."), pszServiceName);
		return false;
	}

	if (!::StartService(schService, 0, NULL))
	{
		if (GetLastError() == ERROR_SERVICE_ALREADY_RUNNING)
		{
			AppendStatusText(_T("Service '%s' already running.\r\n"), pszServiceName);
			CloseServiceHandle(schService);
			return true;
		}

		LogError(_T("Could not start service '%s'."), pszServiceName);
		CloseServiceHandle(schService);
		return false;
	}
	else
	{
		AppendStatusText(_T("Service start (%s) pending.\r\n"), pszServiceName);
	}

	// Check the status until the service is no longer start pending:
	if (!QueryServiceStatusEx(schService, SC_STATUS_PROCESS_INFO, (BYTE *)&ssStatus,
		sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded))
	{
		LogError(_T("Could not check status of service '%s'."), pszServiceName);
		CloseServiceHandle(schService);
		return false;
	}

	// Save the tick count and initial checkpoint:
	dwStartTickCount = GetTickCount();
	dwOldCheckPoint = ssStatus.dwCheckPoint;

	while (ssStatus.dwCurrentState == SERVICE_START_PENDING)
	{
		// Do not wait longer than the wait hint. A good interval is
		// one tenth the wait hint, but no less than 1 second and no
		// more than 10 seconds.
		dwWaitTime = ssStatus.dwWaitHint / 10;

		if (dwWaitTime < 1000)
			dwWaitTime = 1000;
		else if (dwWaitTime > 10000)
			dwWaitTime = 10000;

		Sleep(dwWaitTime);

		// Check the status again:
		if (!QueryServiceStatusEx(schService, SC_STATUS_PROCESS_INFO, (BYTE *)&ssStatus,
			sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded))
		{
			break;
		}

		if (ssStatus.dwCheckPoint > dwOldCheckPoint)
		{
			// The service is making progress.
			dwStartTickCount = GetTickCount();
			dwOldCheckPoint = ssStatus.dwCheckPoint;
		}
		else
		{
			if (GetTickCount() - dwStartTickCount > ssStatus.dwWaitHint)
			{
				// No progress made within the wait hint
				break;
			}
		}
	}

	CloseServiceHandle(schService);

	if (ssStatus.dwCurrentState == SERVICE_RUNNING)
	{
		AppendStatusText(_T("Service '%s' started.\r\n"), pszServiceName);
	}
	else
	{
		LogError(_T("Service '%s' failed to start."), pszServiceName);
		AppendStatusText(_T("    Current State: %d\r\n"), ssStatus.dwCurrentState);
		AppendStatusText(_T("    Exit Code: %d\r\n"), ssStatus.dwWin32ExitCode);
		AppendStatusText(_T("    Service Specific Exit Code: %d\r\n"), ssStatus.dwServiceSpecificExitCode);
		AppendStatusText(_T("    Check Point: %d\r\n"), ssStatus.dwCheckPoint);
		AppendStatusText(_T("    Wait Hint: %d\r\n"), ssStatus.dwWaitHint);
		return false;
	}
	return true;
}

/*
This function attempts to stop a service. It allows the caller to
specify whether dependent services should also be stopped. It also
accepts a timeout value, to prevent a scenario in which a service
shutdown hangs, then the application stopping the service hangs

Parameters:
pszServiceName - service name string
fStopDependencies - Indicates whether to stop dependent services.
dwTimeout - maximum time (in milliseconds) to wait
fWasRunning [out] - set to true if the service was running when this function was called.

If the operation is successful, returns true. Otherwise, returns false.
*/
bool ServiceManager_t::StopService(const _TCHAR * pszServiceName, bool fStopDependencies,
								   DWORD dwTimeout, bool & fWasRunning)
{
	SC_HANDLE schService;
	SERVICE_STATUS_PROCESS ssp;
	SERVICE_STATUS ssDummy;
	DWORD dwStartTime = GetTickCount();
	DWORD dwBytesNeeded;

	schService = OpenService(schSCManager, pszServiceName, SERVICE_ALL_ACCESS);

	if (schService == NULL)
	{
		LogError(_T("Could not open service '%s'."), pszServiceName);
		return false;
	}

	// Make sure the service is not already stopped:
	if (!QueryServiceStatusEx(schService, SC_STATUS_PROCESS_INFO, (LPBYTE)&ssp,
		sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded))
	{
		LogError(_T("Could not tell if service '%s' already stopped - error %d."),
			pszServiceName, GetLastError());
		return false;
	}

	if (ssp.dwCurrentState == SERVICE_STOPPED)
	{
		fWasRunning = false;
		return true;
	}

	// If a stop is pending, just wait for it:
	while (ssp.dwCurrentState == SERVICE_STOP_PENDING)
	{
		Sleep(ssp.dwWaitHint);
		if (!QueryServiceStatusEx(schService, SC_STATUS_PROCESS_INFO, (LPBYTE)&ssp,
			sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded))
		{
			LogError(_T("Could not tell if service '%s' already stopped - error %d."),
				pszServiceName, GetLastError());
			return false;
		}

		if (ssp.dwCurrentState == SERVICE_STOPPED)
		{
			fWasRunning = false;
			return true;
		}

		if (GetTickCount() - dwStartTime > dwTimeout)
		{
			LogError(_T("Timed out waiting for service '%s' to stop."),
				pszServiceName);
			return false;
		}
	}

	fWasRunning = true;

	// Update status messages with news that we're now trying to stop the service:
	_TCHAR * pszDisplayName = NULL;
	DWORD cchDisplayName = 0;
	// Try to get a nice name for the service:
	GetServiceDisplayName(schService, pszServiceName, pszDisplayName, &cchDisplayName);
	if (cchDisplayName > 0)
	{
		cchDisplayName++;
		pszDisplayName = new _TCHAR [cchDisplayName];
		GetServiceDisplayName(schService, pszServiceName, pszDisplayName, &cchDisplayName);
		AppendStatusText(_T("Stopping the %s service. This can take a while..."), pszDisplayName);
		delete[] pszDisplayName;
		pszDisplayName = NULL;
	}
	else
		AppendStatusText(_T("Stopping the %s service. This can take a while..."), pszServiceName);

	// If the service is running, dependencies must be stopped first:
	if (fStopDependencies)
	{
		DWORD i;
		DWORD dwBytesNeeded;
		DWORD dwCount;

		LPENUM_SERVICE_STATUS lpDependencies = NULL;
		ENUM_SERVICE_STATUS ess;
		SC_HANDLE hDepService;

		// Pass a zero-length buffer to get the required buffer size:
		if (EnumDependentServices(schService, SERVICE_ACTIVE, lpDependencies, 0, &dwBytesNeeded,
			&dwCount))
		{
			// If the Enum call succeeds, then there are no dependent services so do nothing
		}
		else
		{
			if (GetLastError() != ERROR_MORE_DATA)
			{
				LogError(_T("Unexpected error %d while trying to obtain buffer size needed to enumerate dependent services of '%s'."),
					GetLastError(), pszServiceName);
				return false; // Unexpected error.
			}

			// Allocate a buffer for the dependencies:
			lpDependencies = (LPENUM_SERVICE_STATUS)HeapAlloc(GetProcessHeap(),
				HEAP_ZERO_MEMORY, dwBytesNeeded);

			if (!lpDependencies)
			{
				LogError(_T("Unexpected error while trying to create buffer needed to enumerate dependent services of '%s'."),
					pszServiceName);
				return false; // Unexpected error.
			}

			__try
			{
				// Enumerate the dependencies:
				if (!EnumDependentServices(schService, SERVICE_ACTIVE, lpDependencies,
					dwBytesNeeded, &dwBytesNeeded, &dwCount))
				{
					LogError(_T("Unexpected error %d while trying to enumerate dependent services of '%s'."),
						GetLastError(), pszServiceName);
					return false; // Unexpected error.
				}

				for (i = 0; i < dwCount; i++)
				{
					ess = *(lpDependencies + i);

					// Open the service:
					hDepService = OpenService(schSCManager, ess.lpServiceName,
						SERVICE_STOP | SERVICE_QUERY_STATUS);
					if (!hDepService)
					{
						LogError(_T("Unexpected error %d while trying to open dependent service '%s' of '%s'."),
							GetLastError(), ess.lpServiceName, pszServiceName);
						return false; // Unexpected error.
					}

					__try
					{
						// Send a stop code:
						if (!ControlService(hDepService, SERVICE_CONTROL_STOP, &ssDummy))
						{
							LogError(_T("Unexpected error %d while trying to stop dependent service '%s' of '%s'."),
								GetLastError(), ess.lpServiceName, pszServiceName);
							return false; // Unexpected error.
						}

						// Wait for the service to stop:
						do
						{
							if (!QueryServiceStatusEx(hDepService, SC_STATUS_PROCESS_INFO,
								(LPBYTE)&ssp, sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded))
							{
								LogError(_T("Unexpected error %d while trying to get status of dependent service '%s' of '%s'."),
									GetLastError(), ess.lpServiceName, pszServiceName);
								return false; // Unexpected error.
							}

							if (ssp.dwCurrentState == SERVICE_STOPPED)
								break;

							if (GetTickCount() - dwStartTime > dwTimeout)
							{
								LogError(_T("Timed out while trying to stop dependent service '%s' of '%s'."),
									ess.lpServiceName, pszServiceName);
								return false;
							}

							Sleep(ssp.dwWaitHint);

						} while (ssp.dwCurrentState != SERVICE_STOPPED);
					}
					__finally
					{
						// Always release the service handle:
						CloseServiceHandle(hDepService);
					}
				} // Next dependent service
			}
			__finally
			{
				// Always free the enumeration buffer:
				HeapFree(GetProcessHeap(), 0, lpDependencies);
			}
		}
	}

	// Send a stop code to the main service:
	if (!ControlService(schService, SERVICE_CONTROL_STOP, &ssDummy))
	{
		LogError(_T("Unexpected error %d while trying to stop service '%s'."),
			GetLastError(), pszServiceName);
		return false; // Unexpected error.
	}

	// Wait for the service to stop:
	do
	{
		if (!QueryServiceStatusEx(schService, SC_STATUS_PROCESS_INFO, (LPBYTE)&ssp,
			sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded))
		{
			LogError(_T("Unexpected error %d while trying to get status of service '%s'."),
				GetLastError(), pszServiceName);
			return false; // Unexpected error.
		}

		if (ssp.dwCurrentState == SERVICE_STOPPED)
			break;

		if (GetTickCount() - dwStartTime > dwTimeout)
		{
			LogError(_T("Timed out while trying to stop service '%s'."), pszServiceName);
			return false;
		}

		Sleep(ssp.dwWaitHint);

	} while (ssp.dwCurrentState != SERVICE_STOPPED);

	AppendStatusText(_T(" Done.\r\n"));

	// Return success:
	return true;
}

bool ServiceManager_t::RestartService(const _TCHAR * pszServiceName, bool fStopDependencies,
									  DWORD dwStopTimeout)
{
	bool fServiceWasRunning;
	if (!this->StopService(pszServiceName, fStopDependencies, dwStopTimeout, fServiceWasRunning))
		return false;
	return this->StartService(pszServiceName);
}

ServiceManager_t ServiceManager;