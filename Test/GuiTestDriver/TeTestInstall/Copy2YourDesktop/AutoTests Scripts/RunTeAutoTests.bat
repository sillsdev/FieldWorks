
:: **  Execute an TE_AUTOTEST Category Test **

SET TEST_CATEGORY=%1

"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:%TEST_CATEGORY%"