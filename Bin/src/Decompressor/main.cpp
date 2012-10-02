#include <windows.h>

#include "StatusDialog.h"
#include "DecompressAndDecrypt.h"

/*----------------------------------------------------------------------------------------------
	This application examines the FieldWorks database folder, and attempts to decompress and
	decrypt the folder and any database files in it.
	It requires administrator priviledges, and the compiled .exe should be signed with an SIL
	digital certificate.
----------------------------------------------------------------------------------------------*/

// Application entry point
int APIENTRY WinMain(HINSTANCE /*hInstance*/, HINSTANCE /*hPrevInstance*/,
					 LPSTR /*lpCmdLine*/, int /*nCmdShow*/)
{
	ShowStatusDialog();

	DecompressAndDecrypt();

	KillStatusDialog();

	return 0;
}