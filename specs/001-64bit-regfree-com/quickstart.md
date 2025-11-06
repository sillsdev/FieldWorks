# Quickstart: 64-bit only + Registration-free COM

This guide shows how to build and validate the feature locally.

## Prerequisites
- Visual Studio 2022 with .NET desktop and Desktop C++ workloads
- WiX 3.11.x (if building installer)
- Initialize environment: `source ./environ` (from a Dev Command Prompt)

## Build x64 only
- Build: msbuild FieldWorks.sln /m /p:Configuration=Debug /p:Platform=x64

## Validate registration-free COM
- Ensure no FieldWorks COM registrations exist on the machine (clean VM recommended)
- Launch Output/Debug/FieldWorks.exe
- Expected: no class-not-registered errors; app runs normally

## Test host for COM activation
- Run COM-activating tests under the shared manifest-enabled host (introduced by this feature); no admin rights needed.

## Artifacts to look for
- FieldWorks.exe.manifest and LexText.exe.manifest updated with <file>/<comClass>/<typelib> entries
- Build logs show RegFree task execution
