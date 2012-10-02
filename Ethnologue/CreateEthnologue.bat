@ECHO OFF
REM To cause the build to regenerate this database, make a minor change to this file to change the date stamp, and save it.

REM Win98 machines don't have ComputerName, so use dot for SQL.
IF "%ComputerName%" == "" SET ComputerName=.

IF "%1" == "" GOTO Error

REM ============================
REM Create Db
REM ============================

ECHO Creating the Ethnologue database...
osql -S%ComputerName%\SILFW -Usa -Pinscrutable -i%1\Src\CreateDb.sql -n

REM ============================
REM Importing data
REM ============================

REM The table with ISO 639-3, -2 (both -2T and -2B), and -1 is at:
REM http://www.sil.org/iso639-3/iso-639-3_20080804.tab. Be sure to remove the
REM first row from the file, which has column headers. The file LanguageCodes.tab
REM can be obtained from http://www.ethnologue.com/codes/LanguageCodes.tab. The
REM big difference between the two is that the latter has the country where the
REM language is most used. (We get country from LanguageIndex.tab, though.) The
REM former has the ISO 639-1 and -2 codes. The particular iso-639-3_20080804.tab
REM I am working with came from Rogger Hanggi.

ECHO Importing ISO 639 Codes...
REM BULK INSERT Iso639Temp FROM 'C:\Fw\Ethnologue\Data\Iso-639-3_20080804.tab' WITH (CODEPAGE = 'ACP')
osql -S%ComputerName%\SILFW -dEthnologue -Usa -Pinscrutable -Q"BULK INSERT Iso639Temp FROM '%1\Data\Iso-639-3_20080804.tab' WITH (CODEPAGE = 'ACP')"

ECHO Importing Language Codes...
REM BULK INSERT LanguageCodesTemp FROM 'C:\Fw\Ethnologue\Data\LanguageCodes.tab' WITH (CODEPAGE = 'ACP')
osql -S%ComputerName%\SILFW -dEthnologue -Usa -Pinscrutable -Q"BULK INSERT LanguageCodesTemp FROM '%1\Data\LanguageCodes.tab' WITH (CODEPAGE = 'ACP')"

REM ---------------------------------------------------------------------------

REM This data in this file was copied and massaged from
REM C:\fw\DistFiles\Icu36\data\locales\en.txt. The section you're looking for
REM is "Languages".

ECHO Importing ICU Languages...
osql -S%ComputerName%\SILFW -dEthnologue -Usa -Pinscrutable -Q"BULK INSERT Icu36Temp FROM '%1\Data\Icu36Languages.tab' WITH (CODEPAGE = 'ACP')"

REM ---------------------------------------------------------------------------

REM This file was obtained from Roger Hanggi of IT, but it can be found at
REM the bottom of http://www.ethnologue.com/codes/default.asp.

ECHO Importing Language Index...
osql -S%ComputerName%\SILFW -dEthnologue -Usa -Pinscrutable -Q"BULK INSERT LanguageIndexTemp FROM '%1\Data\LanguageIndex.tab' WITH (CODEPAGE = 'ACP')"

REM ---------------------------------------------------------------------------

REM This data can also be found at the bottom of
REM http://www.ethnologue.com/codes/default.asp. A different list can be found
REM at http://www.iso.org/iso/list-en1-semic-2.txt. The latter is " are made
REM available by ISO at no charge for internal use and non-commercial
REM purposes". Perhaps that is why we have our own list? When using the
REM Ethnologue list, make sure to remove the first row (of headers), and
REM remove the carriage return at the end.

ECHO Importing CountryCodes...
osql -S%ComputerName%\SILFW -dEthnologue -Usa -Pinscrutable -Q"BULK INSERT CountryTemp FROM '%1\Data\CountryCodes.tab' WITH (CODEPAGE = 'ACP')"

REM ============================
REM Normalizing
REM ============================

ECHO Build in the rest of the data and normalize it...
osql -S%ComputerName%\SILFW -dEthnologue -Usa -Pinscrutable -i%1\Src\NormalizeData.sql -n

REM ============================
REM Stored Procs
REM ============================

ECHO Build in the stored procs...
osql -S%ComputerName%\SILFW -dEthnologue -Usa -Pinscrutable -i%1\Src\ProcsFuncs.sql -n

GOTO Done

REM -------------------------------------------------
:Error
ECHO Need to specify the Ethnologue directory.
ECHO Example: CreateEthnologue c:\fw\ethnologue
GOTO Done

REM -------------------------------------------------
:Done
REM Win98 machines don't have ComputerName, so use dot for SQL.
IF "%ComputerName%" == "." SET ComputerName=
