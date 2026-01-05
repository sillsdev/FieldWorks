@echo off
REM Call out to the buildMsi.bat and buildExe.bat to build and sign each part
REM This is used if being run on a developer machine, or on a CI runner that has native signing
REM If you need to pass signing off to a separate step, like in GHA, then you can call each batch separately
(call buildMsi.bat %*) && ( call buildExe.bat %*)