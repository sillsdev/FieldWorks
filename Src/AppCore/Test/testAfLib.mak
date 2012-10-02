# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testAfLib
BUILD_EXTENSION=exe
BUILD_REGSVR=0

# EXE_MODULE is defined, to get correct ModuleEntry sections for AfApp.
DEFS=$(DEFS) /DEXE_MODULE=1

AF_SRC=$(BUILD_ROOT)\Src\AppCore
AFRES_SRC=$(AF_SRC)\Res
# AFLIB_SRC is necessary to pull in the main.h file for the AfLib.lib.
AFLIB_SRC=$(AF_SRC)\AfLib
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
UNITPP_INC=$(BUILD_ROOT)\Include\unit++
AFTEST_SRC=$(AF_SRC)\Test

# Set the USER_INCLUDE environment variable.
UI=$(AFLIB_SRC);$(AF_SRC);$(AFRES_SRC);$(GENERIC_SRC);$(VIEWS_LIB_SRC);$(WIDGETS_SRC);
UI=$(UNITPP_INC);$(UI)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

# === Object Lists ===

OBJ_ALL= $(OBJ_AFCORE) $(OBJ_AFDECORE)


LINK_OPTS=$(LINK_OPTS:/subsystem:windows=/subsystem:console) /LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)"
LINK_LIBS=unit++.lib AfLib.lib Widgets.lib Generic.lib HtmlHelp.lib xmlparse.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_KERNELTESTSUITE=\
	$(INT_DIR)\genpch\Collection.obj\
	$(INT_DIR)\genpch\MockApp.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\


OBJ_ALL=$(OBJ_KERNELTESTSUITE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=testAfLib

ARG_SRCDIR=$(AFTEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===


# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\gawk.exe -f $(BUILD_ROOT)\Bin\CollectUnit++Tests.awk

$(INT_DIR)\genpch\Collection.obj: $(AFTEST_SRC)\Collection.cpp

$(AFTEST_SRC)\Collection.cpp: $(AFTEST_SRC)\testAfLib.h\
 $(AFTEST_SRC)\TestFwFilter.h\
 $(AFTEST_SRC)\TestPossList.h\
 $(AFTEST_SRC)\TestGetNormalizedClipboardData.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** >$(AFTEST_SRC)\Collection.cpp
