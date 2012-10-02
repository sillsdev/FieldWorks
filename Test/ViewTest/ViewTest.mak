# Input
# =====
# BUILD_ROOT: d:\FW
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

# Override build type to always be debug or bounds
!IF "$(BUILD_TYPE)"!="b"
BUILD_TYPE=d
BUILD_CONFIG=Debug
!ENDIF

BUILD_PRODUCT=ViewTest
BUILD_EXTENSION=dll
BUILD_REGSVR=1

TESTHARNESS_SRC=$(BUILD_ROOT)\Test
TEST_SRC=$(BUILD_ROOT)\Test\ViewTest
GENERIC_SRC=$(BUILD_ROOT)\src\Generic
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
VIEWCLASS_SRC=$(BUILD_ROOT)\Test\TestViewer\ViewClasses

# Enable generation of .SBR file (for browsing)
# CL_OPTS=$(CL_OPTS) /FR

# add special target for final browse step
# build_and_browse: build $(BUILD_PRODUCT).bsc


# Set the USER_INCLUDE environment variable.
UI=$(VIEWCLASS_SRC);$(AFCORE_SRC);$(TEST_SRC);$(TESTHARNESS_SRC);$(GENERIC_SRC);$(VIEWS_LIB_SRC);$(BUILD_ROOT)\Output\Common


!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF


!IF "$(BUILD_TYPE)"!="b"
OUT_DIR=$(BUILD_ROOT)\Output\Test
INT_DIR=$(BUILD_ROOT)\Obj\Test\$(BUILD_PRODUCT)
!ELSE
OUT_DIR=$(BUILD_ROOT)\Output\Bounds-Test
INT_DIR=$(BUILD_ROOT)\Obj\Bounds-Test\$(BUILD_PRODUCT)
!ENDIF
COM_OUT_DIR=$(OUT_DIR)
COM_INT_DIR=$(INT_DIR)


!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=
DEFFILE=ViewTest.def

# === Object Lists ===

!INCLUDE "$(GENERIC_SRC)\GenericInc.mak"

OBJ_TEST=\
	$(INT_DIR)\autopch\ViewTest.obj\
	$(INT_DIR)\autopch\TestBase.obj\
	$(INT_DIR)\autopch\VwTestRootSite.obj\
	$(INT_DIR)\autopch\TestStVc.obj\
	$(INT_DIR)\autopch\VwCacheDa.obj\
	$(INT_DIR)\autopch\VwBaseDataAccess.obj\
	$(INT_DIR)\autopch\VwBaseVc.obj\
	$(INT_DIR)\autopch\VwGraphics.obj\
	$(INT_DIR)\autopch\SilTestSite.obj\
	$(INT_DIR)\autopch\MacroBase.obj\
	$(INT_DIR)\autopch\TestVwRoot.obj\
# === Application Framework objs
	$(INT_DIR)\autopch\AfGfx.obj\
	$(INT_DIR)\autopch\AfColorTable.obj\


OBJ_ALL=$(OBJ_TEST) $(OBJ_GENERIC)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=ViewTest

ARG_SRCDIR=$(AFCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(TEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(VIEWS_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(TESTHARNESS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(VIEWCLASS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===

# Target for browse information file
# ViewTest.bsc: *.sbr
# 	bscmake /o $@ $**
