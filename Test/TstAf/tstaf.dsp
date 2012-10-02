# Microsoft Developer Studio Project File - Name="tstaf" - Package Owner=<4>
# Microsoft Developer Studio Generated Build File, Format Version 6.00
# ** DO NOT EDIT **

# TARGTYPE "Win32 (x86) External Target" 0x0106

CFG=tstaf - Win32 Debug
!MESSAGE This is not a valid makefile. To build this project using NMAKE,
!MESSAGE use the Export Makefile command and run
!MESSAGE
!MESSAGE NMAKE /f "tstaf.mak".
!MESSAGE
!MESSAGE You can specify a configuration when running NMAKE
!MESSAGE by defining the macro CFG on the command line. For example:
!MESSAGE
!MESSAGE NMAKE /f "tstaf.mak" CFG="tstaf - Win32 Debug"
!MESSAGE
!MESSAGE Possible choices for configuration are:
!MESSAGE
!MESSAGE "tstaf - Win32 Release" (based on "Win32 (x86) External Target")
!MESSAGE "tstaf - Win32 Debug" (based on "Win32 (x86) External Target")
!MESSAGE

# Begin Project
# PROP AllowPerConfigDependencies 0
# PROP Scc_ProjName ""
# PROP Scc_LocalPath ""

!IF  "$(CFG)" == "tstaf - Win32 Release"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 0
# PROP BASE Output_Dir "Release"
# PROP BASE Intermediate_Dir "Release"
# PROP BASE Cmd_Line "NMAKE /f tstaf.mak"
# PROP BASE Rebuild_Opt "/a"
# PROP BASE Target_File "tstaf.exe"
# PROP BASE Bsc_Name "tstaf.bsc"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 0
# PROP Output_Dir "Release"
# PROP Intermediate_Dir "Release"
# PROP Cmd_Line "..\..\bin\mkaft.bat r"
# PROP Rebuild_Opt "cc"
# PROP Target_File "..\..\output\release\tstaf.exe"
# PROP Bsc_Name ""
# PROP Target_Dir ""

!ELSEIF  "$(CFG)" == "tstaf - Win32 Debug"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 1
# PROP BASE Output_Dir "Debug"
# PROP BASE Intermediate_Dir "Debug"
# PROP BASE Cmd_Line "NMAKE /f tstaf.mak"
# PROP BASE Rebuild_Opt "/a"
# PROP BASE Target_File "tstaf.exe"
# PROP BASE Bsc_Name "tstaf.bsc"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 1
# PROP Output_Dir "Debug"
# PROP Intermediate_Dir "Debug"
# PROP Cmd_Line "..\..\bin\mkaft.bat d"
# PROP Rebuild_Opt "cc"
# PROP Target_File "..\..\output\debug\tstaf.exe"
# PROP Bsc_Name ""
# PROP Target_Dir ""

!ENDIF

# Begin Target

# Name "tstaf - Win32 Release"
# Name "tstaf - Win32 Debug"

!IF  "$(CFG)" == "tstaf - Win32 Release"

!ELSEIF  "$(CFG)" == "tstaf - Win32 Debug"

!ENDIF

# Begin Source File

SOURCE=.\main.h
# End Source File
# Begin Source File

SOURCE=..\..\bin\mkaft.bat
# End Source File
# Begin Source File

SOURCE=.\tstaf.cpp
# End Source File
# Begin Source File

SOURCE=.\tstaf.h
# End Source File
# Begin Source File

SOURCE=.\tstaf.mak
# End Source File
# End Target
# End Project
