#pragma once

#include <tchar.h>
#include <windows.h>

class ServiceManager_t
{
public:
	ServiceManager_t(void);

	bool StartService(const _TCHAR * pszServiceName);
	bool StopService(const _TCHAR * pszServiceName, bool fStopDependencies, DWORD dwTimeout,
		bool & fWasRunning);
	bool RestartService(const _TCHAR * pszServiceName, bool fStopDependencies,
		DWORD dwStopTimeout);

protected:
	SC_HANDLE schSCManager;
};

extern ServiceManager_t ServiceManager;