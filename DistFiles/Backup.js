/*
	Backup.js
	Script to trigger a backup operation. Intended for use by Windows Task Scheduler.
*/

Backup = new ActiveXObject("SIL.DbServices.Backup");
Backup.Backup();
