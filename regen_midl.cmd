@echo off
call C:\BuildTools\VC\Auxiliary\Build\vcvarsall.bat x64
cd /d C:\fw-mounts\C\Users\johnm\Documents\repos\fw-worktrees\agent-1\Output\Common
echo Running MIDL for x64...
midl /env x64 /Oicf /out Raw /dlldata FwKernelPs_d.c FwKernelPs.idl
echo MIDL exit code: %ERRORLEVEL%
