# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testCmnFwDlgs
BUILD_EXTENSION=exe
BUILD_REGSVR=0

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"




UNITPP_INC=$(BUILD_ROOT)\Include\unit++
COMDLGSTEST_SRC=$(BUILD_ROOT)\Src\CommonCOMDlgs\Test
COMDLGS_SRC=$(BUILD_ROOT)\Src\CommonCOMDlgs
COMDLGS_RES=$(BUILD_ROOT)\Src\CommonCOMDlgs\Res
DBACCESS_SRC=$(BUILD_ROOT)\Src\DbAccess
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFCORE_RES=$(BUILD_ROOT)\Src\AppCore\Res
AFLIB_SRC=$(BUILD_ROOT)\Src\AppCore\AfLib
VIEWS_SRC=$(BUILD_ROOT)\Src\Views
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
WIDGETS_RES=$(BUILD_ROOT)\src\Widgets\Res
CELLARLIB_SRC=$(BUILD_ROOT)\Src\Cellar\lib

# Set the USER_INCLUDE environment variable.
UI=$(UNITPP_INC);$(COMDLGSTEST_SRC);$(COMDLGS_SRC);$(COMDLGS_RES);$(DBACCESS_SRC);$(GENERIC_SRC)
UI=$(UI);$(AFCORE_SRC);$(AFCORE_RES);$(AFLIB_SRC);$(VIEWS_LIB_SRC);$(WIDGETS_SRC);$(WIDGETS_RES)
UI=$(UI);$(CELLARLIB_SRC);$(VIEWS_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=CmnFwDlgs.rc

LINK_OPTS=$(LINK_OPTS:/subsystem:windows=/subsystem:console)\
 /LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)"
CPPUNIT_LIBS=unit++.lib
LINK_LIBS= $(CPPUNIT_LIBS) Generic.lib AfLib.lib Widgets.lib Htmlhelp.lib xmlparse.lib\
 ws2_32.lib Mpr.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_COMDLGSTESTSUITE=\
	$(INT_DIR)\genpch\testCmnFwDlgs.obj\
	$(INT_DIR)\genpch\Collection.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\CmnFwDlgs\autopch\OpenProjDlg.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\CmnFwDlgs\autopch\OpenFWProjectDlg.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\CmnFwDlgs\autopch\FWStylesDlg.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\CmnFwDlgs\autopch\TeStylesDlg.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\CmnFwDlgs\autopch\ModuleEntry.obj\

OBJ_ALL=$(OBJ_COMDLGSTESTSUITE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(INT_DIR)\$(RCFILE:.rc=.res): $(COMDLGS_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(COMDLGS_RES)\*.rc

# === Rules ===
PCHNAME=testCmnFwDlgs

ARG_SRCDIR=$(COMDLGS_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(COMDLGSTEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===


# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\CollectUnit++Tests.cmd CmnFwDlgs

$(INT_DIR)\genpch\Collection.obj: $(COMDLGSTEST_SRC)\Collection.cpp

$(COMDLGSTEST_SRC)\Collection.cpp: $(COMDLGSTEST_SRC)\testCmnFwDlgs.h\
 $(COMDLGSTEST_SRC)\TestFwStylesDlg.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** >$(COMDLGSTEST_SRC)\Collection.cpp
