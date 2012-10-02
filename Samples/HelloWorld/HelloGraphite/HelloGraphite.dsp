# Microsoft Developer Studio Project File - Name="HelloGraphite" - Package Owner=<4>
# Microsoft Developer Studio Generated Build File, Format Version 6.00
# ** DO NOT EDIT **

# TARGTYPE "Win32 (x86) External Target" 0x0106

CFG=HelloGraphite - Win32 Debug
!MESSAGE This is not a valid makefile. To build this project using NMAKE,
!MESSAGE use the Export Makefile command and run
!MESSAGE
!MESSAGE NMAKE /f "HelloGraphite.mak".
!MESSAGE
!MESSAGE You can specify a configuration when running NMAKE
!MESSAGE by defining the macro CFG on the command line. For example:
!MESSAGE
!MESSAGE NMAKE /f "HelloGraphite.mak" CFG="HelloGraphite - Win32 Debug"
!MESSAGE
!MESSAGE Possible choices for configuration are:
!MESSAGE
!MESSAGE "HelloGraphite - Win32 Release" (based on "Win32 (x86) External Target")
!MESSAGE "HelloGraphite - Win32 Debug" (based on "Win32 (x86) External Target")
!MESSAGE

# Begin Project
# PROP AllowPerConfigDependencies 0
# PROP Scc_ProjName ""$/Samples/HelloGraphite/Simple", WWFAAAAA"
# PROP Scc_LocalPath "."

!IF  "$(CFG)" == "HelloGraphite - Win32 Release"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 0
# PROP BASE Output_Dir "Release"
# PROP BASE Intermediate_Dir "Release"
# PROP BASE Cmd_Line "NMAKE /f HelloGraphite.mak"
# PROP BASE Rebuild_Opt "/a"
# PROP BASE Target_File "HelloGraphite.exe"
# PROP BASE Bsc_Name "HelloGraphite.bsc"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 0
# PROP Output_Dir "Release"
# PROP Intermediate_Dir "Release"
# PROP Cmd_Line "..\..\..\bin\mkhv.bat r"
# PROP Rebuild_Opt "cc"
# PROP Target_File "..\..\..\output\release\HelloGraphite.exe"
# PROP Bsc_Name ""
# PROP Target_Dir ""

!ELSEIF  "$(CFG)" == "HelloGraphite - Win32 Debug"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 1
# PROP BASE Output_Dir "HelloGraphite___Win32_Debug"
# PROP BASE Intermediate_Dir "HelloGraphite___Win32_Debug"
# PROP BASE Cmd_Line "NMAKE /f HelloGraphite.mak"
# PROP BASE Rebuild_Opt "/a"
# PROP BASE Target_File "HelloGraphite.exe"
# PROP BASE Bsc_Name "HelloGraphite.bsc"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 1
# PROP Output_Dir "HelloGraphite___Win32_Debug"
# PROP Intermediate_Dir "HelloGraphite___Win32_Debug"
# PROP Cmd_Line "..\..\..\bin\mkhg.bat"
# PROP Rebuild_Opt "cc"
# PROP Target_File "..\..\..\output\debug\HelloGraphite.exe"
# PROP Bsc_Name ""
# PROP Target_Dir ""

!ENDIF

# Begin Target

# Name "HelloGraphite - Win32 Release"
# Name "HelloGraphite - Win32 Debug"

!IF  "$(CFG)" == "HelloGraphite - Win32 Release"

!ELSEIF  "$(CFG)" == "HelloGraphite - Win32 Debug"

!ENDIF

# Begin Group "Res"

# PROP Default_Filter "ico;cur;bmp;dlg;rc2;rct;bin;rgs;gif;jpg;jpeg;jpe"
# Begin Source File

SOURCE=.\Res\HelloGraphite.ico
# End Source File
# Begin Source File

SOURCE=.\Res\HelloGraphite.rc
# End Source File
# Begin Source File

SOURCE=.\Res\Resource.h
# End Source File
# End Group
# Begin Source File

SOURCE=.\HelloGraphite.cpp
# End Source File
# Begin Source File

SOURCE=.\HelloGraphite.h
# End Source File
# Begin Source File

SOURCE=.\HelloGraphite.mak
# End Source File
# Begin Source File

SOURCE=.\Main.h
# End Source File
# End Target
# End Project
