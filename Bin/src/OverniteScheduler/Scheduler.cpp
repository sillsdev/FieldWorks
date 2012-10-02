#include "stdafx.h"

// This scheduler currently runs file c:\BuildFw.bat at just after midnight every weekday.
void RunScheduler()
{
	while (!fStopRequested)
	{
		// Get current time:
		SYSTEMTIME CurrentTime;
		GetLocalTime(&CurrentTime);

		// Set first trigger time:
		SYSTEMTIME NextTrigger = CurrentTime;
		// Set 1 minute past midnight:
		NextTrigger.wHour = 0;
		NextTrigger.wMinute = 1;
		NextTrigger.wSecond = 0;
		NextTrigger.wMilliseconds = 0;
		// Now see how many days to wait for next firing:
		ULONGLONG DaysToAdd = 1;
		switch (CurrentTime.wDayOfWeek)
		{
		case 5: // Friday
			DaysToAdd = 3;
			break;
		case 6: // Saturday
			DaysToAdd = 2;
			break;
		default:
			DaysToAdd = 1;
			break;
		}
		// Add in pause of DaysToAdd:
		FILETIME ftTimeTrigger;
		SystemTimeToFileTime(&NextTrigger, &ftTimeTrigger);
		ULARGE_INTEGER ulTimeTrigger;
		ulTimeTrigger.HighPart = ftTimeTrigger.dwHighDateTime;
		ulTimeTrigger.LowPart = ftTimeTrigger.dwLowDateTime;
		ulTimeTrigger.QuadPart += DaysToAdd * 24 * 60 * 60 * 1000 * 1000 * 10;
		ftTimeTrigger.dwHighDateTime = ulTimeTrigger.HighPart;
		ftTimeTrigger.dwLowDateTime = ulTimeTrigger.LowPart;
		FileTimeToSystemTime(&ftTimeTrigger, &NextTrigger);

		// Now find out how long there is to wait until NextTrigger:
		FILETIME ftTimeCurrent;
		SystemTimeToFileTime(&CurrentTime, &ftTimeCurrent);
		ULARGE_INTEGER ulTimeCurrent;
		ulTimeCurrent.HighPart = ftTimeCurrent.dwHighDateTime;
		ulTimeCurrent.LowPart = ftTimeCurrent.dwLowDateTime;

		ULONGLONG Gap = ulTimeTrigger.QuadPart - ulTimeCurrent.QuadPart;
		// Reduce time from 100-Nanosecond counts to milliseconds:
		Gap /= 10000;

		// Wait until next trigger:
		::WaitForSingleObject(hEventStopper, (DWORD)Gap);
		if (fStopRequested)
		{
			::CloseHandle(hEventStopper);
			hEventStopper = NULL;
		}
		else
		{
			// Start scheduled task:
			STARTUPINFO sui;
			PROCESS_INFORMATION pi;
			ZeroMemory(&sui, sizeof(sui));
			sui.cb = sizeof(STARTUPINFO);
			BOOL fRet = CreateProcess("C:\\BuildFw.bat", NULL, NULL, NULL, false, CREATE_NEW_CONSOLE, NULL, "C:\\", &sui, &pi);
			if (fRet == 0)
			{
				// Error
				;
			}
		}
	} // End while
}
