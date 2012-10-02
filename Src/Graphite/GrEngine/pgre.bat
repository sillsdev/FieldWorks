del d:\fw\output\profile\graphite.dll
del D:\FW\Lib\Profile\Generic.lib
call d:\fw\bin\mkGenLib p
call d:\fw\bin\mkgre p
copy d:\fw\obj\profile\graphite\graphite.map d:\fw\output\profile
call profiledll d:\fw\output\profile\graphite d:\fw\output\debug\worldpad
