# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testCle
BUILD_EXTENSION=exe
BUILD_REGSVR=0

# EXE_MODULE is defined, to get correct ModuleEntry sections.
DEFS=$(DEFS) /DEXE_MODULE=1

UNITPP_INC=$(BUILD_ROOT)\Include\unit++
CLETEST_SRC=$(BUILD_ROOT)\Src\Cle\Test
CLE_SRC=$(BUILD_ROOT)\Src\Cle
CLE_RES=$(BUILD_ROOT)\Src\Cle\Res
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFCORE_RES=$(BUILD_ROOT)\Src\AppCore\Res
CELLAR_SRC=$(BUILD_ROOT)\src\Cellar\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
VIEW_LIB_SRC=$(BUILD_ROOT)\src\views\lib
HTMLHELP_LIB=$(BUILD_ROOT)\src\lib
HELP_DIR=$(BUILD_ROOT)\help
# LINGLIB_SRC=$(BUILD_ROOT)\Src\LingLib
# LINGLIB_RES=$(BUILD_ROOT)\Src\LingLib\Res

# Set the USER_INCLUDE environment variable.
# ;$(LINGLIB_SRC);$(LINGLIB_RES)
UI=$(UNITPP_INC);$(CLETEST_SRC);$(CLE_SRC);$(CLE_RES)
UI=$(UI);$(GENERIC_SRC);$(AFCORE_SRC);$(AFCORE_RES);$(CELLAR_SRC);$(WIDGETS_SRC);$(VIEW_LIB_SRC)
UI=$(UI);$(HTMLHELP_LIB)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=Cle.rc
LINK_OPTS=$(LINK_OPTS:/subsystem:windows=/subsystem:console)\
 /LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)"
UNITPP_LIBS=unit++.lib
LINK_LIBS=$(UNITPP_LIBS) AfLib.lib Widgets.lib Generic.lib uuid.lib advapi32.lib\
 kernel32.lib ole32.lib oleaut32.lib odbc32.lib xmlparse.lib Version.lib Htmlhelp.lib\
 atl.lib winmm.lib mstask.lib $(LINK_LIBS)
#  Ling.lib

# === Object Lists ===

OBJ_KERNELTESTSUITE=\
	$(INT_DIR)\genpch\Collection.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwListEditor\autopch\CleDeSplitChild.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwListEditor\autopch\Cle.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwListEditor\autopch\CleCustDocVc.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwListEditor\autopch\CleTlsOptDlg.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwListEditor\autopch\CleLstNotFndDlg.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwListEditor\autopch\AfFwTool.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwListEditor\autopch\ModuleEntry.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwListEditor\autopch\CleCustomExport.obj\

OBJ_ALL=$(OBJ_KERNELTESTSUITE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(INT_DIR)\$(RCFILE:.rc=.res): $(CLE_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(CLE_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(CLE_RES)\*.rc
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.rc
# $(INT_DIR)\$(RCFILE:.rc=.res): $(LINGLIB_RES)\*.rc

# === Rules ===
PCHNAME=testCle

ARG_SRCDIR=$(CLETEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CLE_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===


# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\gawk.exe -f $(BUILD_ROOT)\Bin\CollectUnit++Tests.awk

$(INT_DIR)\genpch\Collection.obj: $(CLETEST_SRC)\Collection.cpp

$(CLETEST_SRC)\Collection.cpp: $(CLETEST_SRC)\testCle.h\
 $(CLETEST_SRC)\TestCleCustomExport.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** >$(CLETEST_SRC)\Collection.cpp
