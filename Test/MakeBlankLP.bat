@echo off
rem ***** Set FWROOT to the root of the FieldWorks project. *****
call %0\..\..\bin\_EnsureRoot.bat

echo Recreating BlankLangProj database...
rem This assumes SQL data is in C:\Program Files\Microsoft SQL Server\MSSQL.2\MSSQL\Data
rem   If your SQLServer data is not in this directory, you should define an environment
rem   variable SQLDataDir that gives the path to the data
rem Nant automatically sets this environment variable when running nant BlankLP-nodep.
if not "%OS%"=="" setlocal
if "%ComputerName%" == "" set ComputerName=.
if "%COM_OUT_DIR%" == "" set COM_OUT_DIR=..\output\common
if "%SQLDataDir%" == "" set SQLDataDir=%ProgramFiles%\Microsoft SQL Server\MSSQL.2\MSSQL\Data
osql -S%ComputerName%\SILFW -Usa -Pinscrutable -dmaster -Q"drop database BlankLangProj" -n
osql -S%ComputerName%\SILFW -Usa -Pinscrutable -dmaster -Q"create database [BlankLangProj] ON (NAME = 'BlankLangProj', FILENAME = '%SQLDataDir%\BlankLangProj.mdf') LOG ON ( NAME = 'BlankLangProj_log', FILENAME = '%SQLDataDir%\BlankLangProj_log.ldf',SIZE = 10MB,MAXSIZE = UNLIMITED,FILEGROWTH = 5MB )" -n
echo Building database model...
osql -S%ComputerName%\SILFW -Usa -Pinscrutable -dBlankLangProj -i%COM_OUT_DIR%\NewLangProj.sql -a 8192 -n -m 1
osql -S%ComputerName%\SILFW -Usa -Pinscrutable -dmaster -Q"alter database [BlankLangProj] set recovery simple"
osql -S%ComputerName%\SILFW -Usa -Pinscrutable -dmaster -Q"alter database [BlankLangProj] set auto_shrink on"
osql -S%ComputerName%\SILFW -Usa -Pinscrutable -dmaster -Q"alter database [BlankLangProj] set auto_close off"
osql -S%ComputerName%\SILFW -Usa -Pinscrutable -dmaster -Q"DBCC shrinkdatabase ('BlankLangProj')"
osql -S%ComputerName%\SILFW -Usa -Pinscrutable -dmaster -Q"EXEC sp_detach_db 'BlankLangProj', 'true'" -n
copy "%SQLDataDir%\BlankLangProj.mdf" %fwroot%\DistFiles\Templates
copy "%SQLDataDir%\BlankLangProj_log.LDF" %fwroot%\DistFiles\Templates
del "%SQLDataDir%\BlankLangProj*.*"
:end
if "%ComputerName%" == "." set ComputerName=
