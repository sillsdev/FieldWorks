# Microsoft Developer Studio Project File - Name="TestViewer" - Package Owner=<4>
# Microsoft Developer Studio Generated Build File, Format Version 6.00
# ** DO NOT EDIT **

# TARGTYPE "Win32 (x86) External Target" 0x0106

CFG=TestViewer - Win32 Debug
!MESSAGE This is not a valid makefile. To build this project using NMAKE,
!MESSAGE use the Export Makefile command and run
!MESSAGE
!MESSAGE NMAKE /f "TestViewer.mak".
!MESSAGE
!MESSAGE You can specify a configuration when running NMAKE
!MESSAGE by defining the macro CFG on the command line. For example:
!MESSAGE
!MESSAGE NMAKE /f "TestViewer.mak" CFG="TestViewer - Win32 Debug"
!MESSAGE
!MESSAGE Possible choices for configuration are:
!MESSAGE
!MESSAGE "TestViewer - Win32 Release" (based on "Win32 (x86) External Target")
!MESSAGE "TestViewer - Win32 Debug" (based on "Win32 (x86) External Target")
!MESSAGE

# Begin Project
# PROP AllowPerConfigDependencies 0
# PROP Scc_ProjName ""
# PROP Scc_LocalPath ""

!IF  "$(CFG)" == "TestViewer - Win32 Release"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 0
# PROP BASE Output_Dir "Release"
# PROP BASE Intermediate_Dir "Release"
# PROP BASE Cmd_Line "NMAKE /f TestViewer.mak"
# PROP BASE Rebuild_Opt "/a"
# PROP BASE Target_File "TestViewer.exe"
# PROP BASE Bsc_Name "TestViewer.bsc"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 0
# PROP Output_Dir "Release"
# PROP Intermediate_Dir "Release"
# PROP Cmd_Line "c:\fw\bin\mktv.bat"
# PROP Rebuild_Opt "cc"
# PROP Target_File "..\..\Output\Debug\TestViewer.exe"
# PROP Bsc_Name ""
# PROP Target_Dir ""

!ELSEIF  "$(CFG)" == "TestViewer - Win32 Debug"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 1
# PROP BASE Output_Dir "Debug"
# PROP BASE Intermediate_Dir "Debug"
# PROP BASE Cmd_Line "NMAKE /f TestViewer.mak"
# PROP BASE Rebuild_Opt "/a"
# PROP BASE Target_File "TestViewer.exe"
# PROP BASE Bsc_Name "TestViewer.bsc"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 1
# PROP Output_Dir "Debug"
# PROP Intermediate_Dir "Debug"
# PROP Cmd_Line "..\..\bin\mktv.bat"
# PROP Rebuild_Opt "cc"
# PROP Target_File "..\..\Output\Debug\TestViewer.exe"
# PROP Bsc_Name ""
# PROP Target_Dir ""

!ENDIF

# Begin Target

# Name "TestViewer - Win32 Release"
# Name "TestViewer - Win32 Debug"

!IF  "$(CFG)" == "TestViewer - Win32 Release"

!ELSEIF  "$(CFG)" == "TestViewer - Win32 Debug"

!ENDIF

# Begin Group "Source Files"

# PROP Default_Filter "cpp;c;cxx;rc;def;r;odl;idl;hpj;bat"
# Begin Source File

SOURCE=.\explicit_instantiations.cpp
# End Source File
# Begin Source File

SOURCE=.\StVc.cpp
# End Source File
# Begin Source File

SOURCE=.\TestScriptDlg.cpp
# End Source File
# Begin Source File

SOURCE=.\TestViewer.cpp
# End Source File
# Begin Source File

SOURCE=.\WpDa.cpp
# End Source File
# End Group
# Begin Group "Header Files"

# PROP Default_Filter "h;hpp;hxx;hm;inl"
# Begin Source File

SOURCE=.\main.h
# End Source File
# Begin Source File

SOURCE=.\StVc.h
# End Source File
# Begin Source File

SOURCE=.\TestScriptDlg.h
# End Source File
# Begin Source File

SOURCE=.\TestViewer.h
# End Source File
# Begin Source File

SOURCE=.\WpDa.h
# End Source File
# End Group
# Begin Group "Resource Files"

# PROP Default_Filter "ico;cur;bmp;dlg;rc2;rct;bin;rgs;gif;jpg;jpeg;jpe"
# End Group
# Begin Source File

SOURCE=..\..\TestLog\Log\TestViewer.bsn
# End Source File
# Begin Source File

SOURCE=.\TestViewer.mak
# End Source File
# End Target
# End Project
