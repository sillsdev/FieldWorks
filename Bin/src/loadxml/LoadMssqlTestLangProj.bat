REM ===========================================================================
REM This is for testing only. It drops TestLangProj, recreates it, then calls
REM loadxml to load it. If TestLangProj has data already, loadxml will fail.
REM
REM Note that I (Steve) wrote this for my computer. You will need to change
REM the server name for your own use. If someone knows how to do this better,
REM be my guest.
REM ===========================================================================

cd c:\fw\Output\WWData

osql -SLS-MILLER\SILFW -Usa -Pinscrutable -dmaster -Q"drop database TestLangProj"

osql -SLS-MILLER\SILFW -Usa -Pinscrutable -dmaster -Q"create database [TestLangProj] ON (NAME = 'TestLangProj', FILENAME = 'c:\Program Files\Microsoft SQL Server\MSSQL.1\MSSQL\Data\TestLangProj.mdf') LOG ON ( NAME = 'TestLangProj_log', FILENAME = 'c:\Program Files\Microsoft SQL Server\MSSQL.1\MSSQL\Data\TestLangProj_log.ldf',SIZE = 10MB,MAXSIZE = UNLIMITED,FILEGROWTH = 5MB )"

osql -SLS-MILLER\SILFW -Usa -Pinscrutable -dTestLangProj -i c:\fw\output\common\NewLangProj.sql

loadxml -i TestLangProj.xml -d TestLangProj

cd \fw\bin\src\loadxml