# Microsoft Developer Studio Project File - Name="LangTest" - Package Owner=<4>
# Microsoft Developer Studio Generated Build File, Format Version 6.00
# ** DO NOT EDIT **

# TARGTYPE "Win32 (x86) External Target" 0x0106

CFG=LangTest - Win32 Debug
!MESSAGE This is not a valid makefile. To build this project using NMAKE,
!MESSAGE use the Export Makefile command and run
!MESSAGE
!MESSAGE NMAKE /f "LangTest.mak".
!MESSAGE
!MESSAGE You can specify a configuration when running NMAKE
!MESSAGE by defining the macro CFG on the command line. For example:
!MESSAGE
!MESSAGE NMAKE /f "LangTest.mak" CFG="LangTest - Win32 Debug"
!MESSAGE
!MESSAGE Possible choices for configuration are:
!MESSAGE
!MESSAGE "LangTest - Win32 Release" (based on "Win32 (x86) External Target")
!MESSAGE "LangTest - Win32 Debug" (based on "Win32 (x86) External Target")
!MESSAGE

# Begin Project
# PROP AllowPerConfigDependencies 0
# PROP Scc_ProjName ""$/Test/LangTest", RECAAAAA"
# PROP Scc_LocalPath "."

!IF  "$(CFG)" == "LangTest - Win32 Release"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 0
# PROP BASE Output_Dir "Release"
# PROP BASE Intermediate_Dir "Release"
# PROP BASE Cmd_Line "NMAKE /f LangTest.mak"
# PROP BASE Rebuild_Opt "/a"
# PROP BASE Target_File "LangTest.exe"
# PROP BASE Bsc_Name "LangTest.bsc"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 0
# PROP Output_Dir "Release"
# PROP Intermediate_Dir "Release"
# PROP Cmd_Line "mklgt r"
# PROP Rebuild_Opt "cc"
# PROP Target_File "output\release\LangTest.dll"
# PROP Bsc_Name ""
# PROP Target_Dir ""

!ELSEIF  "$(CFG)" == "LangTest - Win32 Debug"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 1
# PROP BASE Output_Dir "Debug"
# PROP BASE Intermediate_Dir "Debug"
# PROP BASE Cmd_Line "NMAKE /f LangTest.mak"
# PROP BASE Rebuild_Opt "/a"
# PROP BASE Target_File "LangTest.exe"
# PROP BASE Bsc_Name "LangTest.bsc"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 1
# PROP Output_Dir "Debug"
# PROP Intermediate_Dir "Debug"
# PROP Cmd_Line "mklgt.bat d"
# PROP Rebuild_Opt "cc"
# PROP Target_File "output\debug\LangTest.dll"
# PROP Bsc_Name "D:\FW\Test\LangTest\LangTest.bsc"
# PROP Target_Dir ""

!ENDIF

# Begin Target

# Name "LangTest - Win32 Release"
# Name "LangTest - Win32 Debug"

!IF  "$(CFG)" == "LangTest - Win32 Release"

!ELSEIF  "$(CFG)" == "LangTest - Win32 Debug"

!ENDIF

# Begin Group "Source Files"

# PROP Default_Filter "cpp;c;cxx;rc;def;r;odl;idl;hpj;bat"
# Begin Source File

SOURCE=.\langtest.cpp
# End Source File
# Begin Source File

SOURCE=.\LangTest.def
# End Source File
# Begin Source File

SOURCE=..\..\src\Language\LgNumericConverterSpec.cpp
# End Source File
# End Group
# Begin Group "Header Files"

# PROP Default_Filter "h;hpp;hxx;hm;inl"
# Begin Source File

SOURCE=.\langtest.h
# End Source File
# Begin Source File

SOURCE=..\..\src\Language\LgNumericConverterSpec.h
# End Source File
# Begin Source File

SOURCE=.\Main.h
# End Source File
# End Group
# Begin Group "Resource Files"

# PROP Default_Filter "ico;cur;bmp;dlg;rc2;rct;bin;rgs;gif;jpg;jpeg;jpe"
# End Group
# Begin Source File

SOURCE=.\LangTest.mak
# End Source File
# Begin Source File

SOURCE=..\TestBase.cpp
# End Source File
# Begin Source File

SOURCE=..\TestBase.h
# End Source File
# End Target
# End Project
