# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testMigrateData
BUILD_EXTENSION=exe
BUILD_REGSVR=0

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

UNITPP_INC=$(BUILD_ROOT)\Include\unit++
MIGRATEDATATEST_SRC=$(BUILD_ROOT)\Src\MigrateData\Test
MIGRATEDATA_SRC=$(BUILD_ROOT)\Src\MigrateData
DBACCESS_SRC=$(BUILD_ROOT)\Src\DbAccess
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
APPCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFRES_SRC=$(BUILD_ROOT)\Src\AppCore\Res
AFLIB_SRC=$(BUILD_ROOT)\Src\AppCore\AfLib
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
CELLARLIB_SRC=$(BUILD_ROOT)\Src\Cellar\lib

# Set the USER_INCLUDE environment variable.
UI=$(UNITPP_INC);$(MIGRATEDATATEST_SRC);$(MIGRATEDATA_SRC);$(DBACCESS_SRC);$(GENERIC_SRC);$(APPCORE_SRC);$(AFRES_SRC);$(AFLIB_SRC);$(VIEWS_LIB_SRC);$(WIDGETS_SRC);$(CELLARLIB_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

OBJ_RCFILE=$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\MigrateData\MigrateData.res
LINK_OPTS=$(LINK_OPTS:/subsystem:windows=/subsystem:console)\
 /LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)"
CPPUNIT_LIBS=unit++.lib
LINK_LIBS=$(CPPUNIT_LIBS) Generic.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_MIGRATEDATATESTSUITE=\
	$(INT_DIR)\genpch\testMigrateData.obj\
	$(INT_DIR)\genpch\Collection.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\MigrateData\genpch\MigrateData.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\MigrateData\autopch\ModuleEntry.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\MigrateData\autopch\DbStringCrawler.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\MigrateData\autopch\FwDbChangeOverlayTags.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\MigrateData\autopch\FwStyledText.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\MigrateData\usepch\TextProps1.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\MigrateData\autopch\UtilSil2.obj\

OBJ_ALL=$(OBJ_MIGRATEDATATESTSUITE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=testMigrateData

ARG_SRCDIR=$(MIGRATEDATATEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===


# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\gawk.exe -f $(BUILD_ROOT)\Bin\CollectUnit++Tests.awk

$(INT_DIR)\genpch\Collection.obj: $(MIGRATEDATATEST_SRC)\Collection.cpp

$(MIGRATEDATATEST_SRC)\Collection.cpp: $(MIGRATEDATATEST_SRC)\testMigrateData.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** >$(MIGRATEDATATEST_SRC)\Collection.cpp
