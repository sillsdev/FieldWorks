set FBBIN=C:\Firebird\bin\
set FBSRC=C:\Fw\Src\Cellar\Firebird\
set FBUSER='sysdba'
set FBFW='inscrutable'

cd %FBSRC%
%FBBIN%isql -u %FBUSER% -p %FBFW% -q  -i Build.sql
