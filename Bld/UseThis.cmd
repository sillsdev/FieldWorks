@echo off
if exist %1\UseThis.build.xml nant -f:%1\UseThis.build.xml -D:unregister=true doregisternet
mkdir %1
copy %0\..\*.* %1\
nant -f:%1\UseThis.build.xml -D:targetdir=%1 -D:srcdir=%0\.. %2 %3 %4 %5 %6 %7 %8 %9 all