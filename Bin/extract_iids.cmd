@echo off
:: extract the iids from generated COM header files for use on Linux
:: Don't specify date and time because that produces different files that otherwise would be
:: the same.

for %%f in (%*) do (
	echo // Automatically generated from %%f by %0
	echo #include "%%f"
	echo.

	%WORKSPACE%\Bin\sed.exe -n -f "%WORKSPACE%\Bin\extract_iids.sed" %%f
)
