# Microsoft Developer Studio Project File - Name="ViewTest" - Package Owner=<4>
# Microsoft Developer Studio Generated Build File, Format Version 6.00
# ** DO NOT EDIT **

# TARGTYPE "Win32 (x86) External Target" 0x0106

CFG=ViewTest - Win32 Debug
!MESSAGE This is not a valid makefile. To build this project using NMAKE,
!MESSAGE use the Export Makefile command and run
!MESSAGE
!MESSAGE NMAKE /f "ViewTest.mak".
!MESSAGE
!MESSAGE You can specify a configuration when running NMAKE
!MESSAGE by defining the macro CFG on the command line. For example:
!MESSAGE
!MESSAGE NMAKE /f "ViewTest.mak" CFG="ViewTest - Win32 Debug"
!MESSAGE
!MESSAGE Possible choices for configuration are:
!MESSAGE
!MESSAGE "ViewTest - Win32 Release" (based on "Win32 (x86) External Target")
!MESSAGE "ViewTest - Win32 Debug" (based on "Win32 (x86) External Target")
!MESSAGE

# Begin Project
# PROP AllowPerConfigDependencies 0
# PROP Scc_ProjName ""
# PROP Scc_LocalPath ""

!IF  "$(CFG)" == "ViewTest - Win32 Release"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 0
# PROP BASE Output_Dir "Release"
# PROP BASE Intermediate_Dir "Release"
# PROP BASE Cmd_Line "NMAKE /f ViewTest.mak"
# PROP BASE Rebuild_Opt "/a"
# PROP BASE Target_File "ViewTest.exe"
# PROP BASE Bsc_Name "ViewTest.bsc"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 0
# PROP Output_Dir "Release"
# PROP Intermediate_Dir "Release"
# PROP Cmd_Line "..\..\bin\mkvwt.bat r"
# PROP Rebuild_Opt "cc"
# PROP Target_File "..\..\output\test\ViewTest.exe"
# PROP Bsc_Name ""
# PROP Target_Dir ""

!ELSEIF  "$(CFG)" == "ViewTest - Win32 Debug"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 1
# PROP BASE Output_Dir "Debug"
# PROP BASE Intermediate_Dir "Debug"
# PROP BASE Cmd_Line "NMAKE /f ViewTest.mak"
# PROP BASE Rebuild_Opt "/a"
# PROP BASE Target_File "ViewTest.exe"
# PROP BASE Bsc_Name "ViewTest.bsc"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 1
# PROP Output_Dir "Debug"
# PROP Intermediate_Dir "Debug"
# PROP Cmd_Line "..\..\bin\mkvwt.bat"
# PROP Rebuild_Opt "cc"
# PROP Target_File "..\..\output\test\ViewTest.exe"
# PROP Bsc_Name ""
# PROP Target_Dir ""

!ENDIF

# Begin Target

# Name "ViewTest - Win32 Release"
# Name "ViewTest - Win32 Debug"

!IF  "$(CFG)" == "ViewTest - Win32 Release"

!ELSEIF  "$(CFG)" == "ViewTest - Win32 Debug"

!ENDIF

# Begin Group "Source Files"

# PROP Default_Filter "cpp;c;cxx;rc;def;r;odl;idl;hpj;bat"
# Begin Source File

SOURCE=.\TestVwRoot.cpp
# End Source File
# Begin Source File

SOURCE=.\ViewTest.cpp
# End Source File
# Begin Source File

SOURCE=.\ViewTest.def
# End Source File
# Begin Source File

SOURCE=.\VwTestRootSite.cpp
# End Source File
# End Group
# Begin Group "Header Files"

# PROP Default_Filter "h;hpp;hxx;hm;inl"
# Begin Source File

SOURCE=.\Main.h
# End Source File
# Begin Source File

SOURCE=.\TestVwRoot.h
# End Source File
# Begin Source File

SOURCE=.\ViewTest.h
# End Source File
# Begin Source File

SOURCE=.\VwTestRootSite.h
# End Source File
# End Group
# Begin Group "Resource Files"

# PROP Default_Filter "ico;cur;bmp;dlg;rc2;rct;bin;rgs;gif;jpg;jpeg;jpe"
# End Group
# Begin Source File

SOURCE=..\..\src\views\Views.idh
# End Source File
# Begin Source File

SOURCE=.\ViewTest.mak
# End Source File
# Begin Source File

SOURCE=..\..\TestLog\Log\VwGraphics.bsn
# End Source File
# End Target
# End Project
