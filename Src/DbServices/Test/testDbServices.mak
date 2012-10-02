# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testDbServices
BUILD_EXTENSION=exe
BUILD_REGSVR=0

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

UNITPP_INC=$(BUILD_ROOT)\Include\unit++
DBSERVICESTEST_SRC=$(BUILD_ROOT)\Src\DbServices\Test
DBSERVICES_SRC=$(BUILD_ROOT)\Src\DbServices
DBACCESS_SRC=$(BUILD_ROOT)\Src\DbAccess
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
APPCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFRES_SRC=$(BUILD_ROOT)\Src\AppCore\Res
AFLIB_SRC=$(BUILD_ROOT)\Src\AppCore\AfLib
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
CELLARLIB_SRC=$(BUILD_ROOT)\Src\Cellar\lib

# Set the USER_INCLUDE environment variable.
UI=$(UNITPP_INC);$(DBSERVICESTEST_SRC);$(DBSERVICES_SRC);$(DBACCESS_SRC);$(GENERIC_SRC)
UI=$(UI);$(APPCORE_SRC);$(AFRES_SRC);$(AFLIB_SRC);$(VIEWS_LIB_SRC);$(WIDGETS_SRC)
UI=$(UI);$(CELLARLIB_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

OBJ_RCFILE=$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\DbServices\DbServices.res

LINK_OPTS=$(LINK_OPTS:/subsystem:windows=/subsystem:console)\
 /LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)"
CPPUNIT_LIBS=unit++.lib
LINK_LIBS=$(CPPUNIT_LIBS) Generic.lib AfLib.lib Widgets.lib xmlparse.lib Htmlhelp.lib\
 $(LINK_LIBS)

# === Object Lists ===

OBJ_DBSERVICESTESTSUITE=\
	$(INT_DIR)\genpch\testDbServices.obj\
	$(INT_DIR)\genpch\Collection.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\DbServices\autopch\Backup.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\DbServices\autopch\ZipInvoke.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\DbServices\autopch\Remote.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\DbServices\autopch\Disconnect.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\DbServices\autopch\ModuleEntry.obj\

OBJ_ALL=$(OBJ_DBSERVICESTESTSUITE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=testDbServices

ARG_SRCDIR=$(DBSERVICESTEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===


# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\gawk.exe -f $(BUILD_ROOT)\Bin\CollectUnit++Tests.awk

$(INT_DIR)\genpch\Collection.obj: $(DBSERVICESTEST_SRC)\Collection.cpp

$(DBSERVICESTEST_SRC)\Collection.cpp: $(DBSERVICESTEST_SRC)\testDbServices.h\
 $(DBSERVICESTEST_SRC)\TestFwBackupDb.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** >$(DBSERVICESTEST_SRC)\Collection.cpp
