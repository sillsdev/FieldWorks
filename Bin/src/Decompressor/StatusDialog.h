#include <tchar.h>

extern void ShowStatusDialog();
extern void HideStatusDialog();
extern void KillStatusDialog();
extern void LogError(const _TCHAR * pszFormat, ...);
extern void AppendStatusText(const _TCHAR * pszFormat, ...);
extern bool IfStopRequested();
extern bool WriteLogToClipboard();
extern void CopyErrorsToClipboard();