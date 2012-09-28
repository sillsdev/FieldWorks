# Input
# =====
#
# MSVC_ROOT
# MSDEVDIR
#
# BUILD_ROOT: Typically "C:\FW-WW"
# BUILD_TYPE: b, d, r, p
# BUILD_CONFIG: Bounds, Debug, Release, Profile
# BUILD_OUTPUT: Typically "C:\FW-WW\Output"
# BUILD_EXTENSION: exe, dll, lib, ocx, (or empty indicating no main target)
# BUILD_PRODUCT: LangProj
# BUILD_REGSVR: 0 (no) or 1 (yes)
# DEBUG_ICU: 0 (no) or 1 (yes)
#

# The following table describes what should go in the different output
# directories. "Releasable" means that the target is to be distributed to others.
# BUILD_CONFIG-specific means the target is potentially different for Debug and
# Release builds.
#
#   dir      Releasable  BUILD_CONFIG-specific
# OUT_DIR       yes             yes
# INT_DIR       no              yes
# COM_OUT_DIR   yes             no
# COM_INT_DIR   no              no
# BSC_INT_DIR   no              no (debug builds only)  --- Added 27-MAR-2001 TLB
#


!IF "$(OUT_DIR)"==""
!IF "$(BUILD_EXTENSION)"=="lib"
OUT_DIR=$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)
!ELSE
!IF "$(BUILD_OUTPUT)"==""
OUT_DIR=$(BUILD_ROOT)\Output\$(BUILD_CONFIG)
!ELSE
OUT_DIR=$(BUILD_OUTPUT)\$(BUILD_CONFIG)
!ENDIF
!ENDIF
!ENDIF

!IF "$(OBJ_DIR)"==""
OBJ_DIR=$(BUILD_ROOT)\Obj
!ENDIF

!IF "$(INT_DIR)"==""
INT_DIR=$(OBJ_DIR)\$(BUILD_CONFIG)\$(BUILD_PRODUCT)
!ENDIF

!IF "$(COM_OUT_DIR)"==""
!IF "$(BUILD_OUTPUT)"==""
COM_OUT_DIR=$(BUILD_ROOT)\Output\Common
!ELSE
COM_OUT_DIR=$(BUILD_OUTPUT)\Common
!ENDIF
!ENDIF

!IF "$(COM_OUT_DIR_RAW)"==""
COM_OUT_DIR_RAW=$(COM_OUT_DIR)\Raw
!ENDIF

!IF "$(COM_INT_DIR)"==""
COM_INT_DIR=$(OBJ_DIR)\Common\$(BUILD_PRODUCT)
!ENDIF

# Added 27-MAR-2001 TLB: Directory for SBR and BSC files is needed if this
# is a debug build and user has environment variable BUILD_BSC set to build
# source code browser database for use in Visual Studio
!IF "$(BUILD_CONFIG)"=="Debug" || "$(BUILD_CONFIG)"=="Bounds"
!IF "$(BSC_INT_DIR)"=="" && "$(BUILD_BSC)"=="Y"
BSC_INT_DIR=$(OBJ_DIR)\SrcBrwsr
!ENDIF
!ENDIF

# Initialize BUILD_MAIN - the main target.
!IF "$(BUILD_EXTENSION)"!="" && "$(BUILD_MAIN)"==""
BUILD_MAIN=$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION)
!ENDIF

# Initialize BUILD_TARGETS
BUILD_TARGETS=$(BUILD_TARGETS) $(BUILD_MAIN)

# Added 27-MAR-2001 TLB: Build source code browser database
!IF "$(BSC_INT_DIR)"!=""
BUILD_TARGETS=$(BUILD_TARGETS) $(BSC_INT_DIR)\Fw.bsc
!ENDIF

!IF "$(BUILD_CONFIG)"=="Bounds"

PREPROCESS=nmcl.exe /nologo

!IF "$(ANAL_TYPE)"=="performance"
CL=nmcl.exe /NMttOn /NMtcOn /nologo
LINK=nmlink.exe /NMttOn /NMtcOn
!ELSE
CL=nmcl.exe /NMbcOn /NMtcOn /nologo
LINK=nmlink.exe /NMbcOn /NMtcOn
!ENDIF

!ELSE

CL=cl.exe /nologo
PREPROCESS=cl.exe /nologo
LINK=link.exe

!ENDIF

LIBLINK=lib.exe
RC=rc.exe
RES=rc.exe
MRC=mrc.exe
MIDL=midl.exe
BSC=bscmake.exe
MAPSYM=mapsym.exe
REGSVR=regsvr32.exe
DISPLAY=@echo
ECHO=@echo
COPYFILE=copy
DELETEFILE=del
TYPEFILE=type
MD=$(BUILD_ROOT)\bin\mkdir.exe -p
DELNODE=$(BUILD_ROOT)\bin\delnode.exe
FIXCOMHEADER=$(BUILD_ROOT)\bin\FixGenComHeaderFile.exe

# next 4 are for .NET
IDLIMP=@$(BUILD_ROOT)\bin\idlimp.exe
TLBIMP=@tlbimp.exe /nologo
MSXSL=@$(BUILD_ROOT)\Bin\MSXSL.exe
GACUTIL=gacutil.exe


# This is so tlb's can be found by midl.
PATH=$(PATH);$(COM_OUT_DIR)


###### Options and switches

# Don't define WIN32 if we build some idh-dependent files for Linux

!if "$(BUILD4UX)"=="1"

# do nothing

!ELSE

DEFS=$(DEFS) /DWIN32=1

!ENDIF


DEFS=$(DEFS) /D_WINDOWS=1 /D_AFXDLL=1

!IF "$(AFX)"=="MFC"
DEFS=$(DEFS) /D_AFXDLL=1 /D_WINDLL=1 /D_USRDLL=1 /DUSING_MFC=1
CL_OPTS=$(CL_OPTS) /MD
!ELSE IF "$(BUILD_TYPE)"=="d"
CL_OPTS=$(CL_OPTS) /MTd /RTC1
!ELSE IF "$(BUILD_TYPE)"=="b"
CL_OPTS=$(CL_OPTS) /MTd
!ELSE
CL_OPTS=$(CL_OPTS) /MT
!ENDIF

# JohnT: /EHa is required so that our code that converts C exceptions (access violation, div by zero)
# into C++ ThrowableSd exceptions, and catches them at interface boundaries, can work
# reliably. The October 1999 edition of Bugslayer in MSDN has a fuller explanation.
CL_OPTS=$(CL_OPTS) /W4 /WX /Fd"$(INT_DIR)/" /EHa /GR /GF /Zm400 /D_WIN32_WINNT=0x0500

PREPROCESS_OPTS=/E

!IF "$(BUILD_CONFIG)"=="Bounds"
LINK_OPTS=$(LINK_OPTS) /out:"$@" /machine:IX86 /incremental:no\
	/map:$(INT_DIR)\$(@B).map /nod:dbguuid.lib /subsystem:windows\
	/NODEFAULTLIB:LIBC /NODEFAULTLIB:MSVCRT\
	/LIBPATH:"C:\Program Files\Common Files\Compuware\NMShared" \
	/LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)" /LIBPATH:"$(BUILD_ROOT)\Lib"
!ELSE
LINK_OPTS=$(LINK_OPTS) /out:"$@" /machine:IX86 /incremental:no\
	/map:$(INT_DIR)\$(@B).map /nod:dbguuid.lib /subsystem:windows\
	/NODEFAULTLIB:LIBC /NODEFAULTLIB:MSVCRT\
	/LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)" /LIBPATH:"$(BUILD_ROOT)\Lib"
!ENDIF

LINK_OPTS_PS=

#LINK_OPTS=$(LINK_OPTS) /base:@$(BUILD_ROOT)\bld\base.txt,$(BUILD_PRODUCT)

LIBLINK_OPTS=$(LIBLINK_OPTS) /out:"$@" /subsystem:windows

BSC_OPTS=/o"$@"

MIDL_OPTS=$(MIDL_OPTS) /Oicf /env win32 /I"$(COM_OUT_DIR)" /error all /error allocation /error bounds_check /error enum /error ref /error stub_data /error stub_data

REGSVR_OPTS=/s $(REGSVR_OPTS)

USER_INCLUDE=$(OUT_DIR);$(COM_OUT_DIR);$(INT_DIR);$(COM_INT_DIR);$(BUILD_ROOT)\Lib\$(BUILD_CONFIG);$(BUILD_ROOT)\Include;$(USER_INCLUDE)

# Include ICU libraries here to make them available to all components whose .mak file includes this one.
LINK_LIBS=icuin.lib icudt.lib icuuc.lib $(LINK_LIBS)

LINK_LIBS=uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib gdi32.lib comctl32.lib comdlg32.lib shell32.lib imm32.lib ImageHlp.lib Version.lib winspool.lib $(LINK_LIBS)

!IF "$(BUILD_CONFIG)"=="Bounds"
LINK_LIBS=$(LINK_LIBS) BCINTERF.LIB
!ENDIF

!IF "$(BUILD_TYPE)"=="d" || "$(BUILD_TYPE)"=="b"

# Added 27-MAR-2001 TLB: If an intermediate directory has been supplied for creation of
# SBR files, use /FR{filespec} option to create them there
!IF "$(BSC_INT_DIR)"!=""
SBR_OPT=/FR$(BSC_INT_DIR)\$(*B).sbr
!ELSE
SBR_OPT=
!ENDIF

CL_OPTS=$(CL_OPTS) /Zi /Od $(SBR_OPT)

LINK_OPTS=$(LINK_OPTS) /debug /pdb:"$*.pdb"

LIBRARY_SUFFIX=d

CL_OPTS=$(CL_OPTS) /Gm

DEFS=$(DEFS) /DDEBUG=1 /D_DEBUG=1

!IF "$(BUILD_PRODUCT)"!="DebugProcs"
LINK_LIBS=$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\DebugProcs.lib $(LINK_LIBS)
!ENDIF
!ELSE

CL_OPTS=$(CL_OPTS) /Zi /O2 /Ob1 /Gy

LIBRARY_SUFFIX=

LINK_OPTS=$(LINK_OPTS) /opt:ref /debug /pdb:"$*.pdb"

DEFS=$(DEFS) /DNDEBUG=1

!ENDIF


!IF "$(BUILD_EXTENSION)"=="dll" || "$(BUILD_EXTENSION)"=="ocx"
LINK_OPTS=$(LINK_OPTS) /dll /implib:"$(INT_DIR)\$(*B).lib"
!ENDIF
