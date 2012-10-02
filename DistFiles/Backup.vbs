'
'   Backup.vbs
'   Script to trigger a backup operation. Intended for use by Windows Task Scheduler.
'

Dim Backup
Set Backup = WScript.CreateObject("SIL.DbServices.Backup")
Backup.Backup