#include <windows.h>

// We need a DllMain function to keep Windows happy when the DLL is loaded.

int WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
/* We don't need any of this rubbish:
	switch (fdwReason)
	{
		case DLL_PROCESS_ATTACH:
			// Init Code here
			break;

		case DLL_THREAD_ATTACH:
			// Thread-specific init code here.
			break;

		case DLL_THREAD_DETACH:
			// Thread-specific cleanup code here.
			break;

		case DLL_PROCESS_DETACH:
			// Cleanup code here
			break;
	}
*/
	// The return value is used for successful DLL_PROCESS_ATTACH:
	return TRUE;
}
