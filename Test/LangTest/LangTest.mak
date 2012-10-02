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

BUILD_PRODUCT=LangTest
BUILD_EXTENSION=dll
BUILD_REGSVR=1

TESTHARNESS_SRC=$(BUILD_ROOT)\Test
TEST_SRC=$(BUILD_ROOT)\Test\LangTest
GENERIC_SRC=$(BUILD_ROOT)\src\Generic
APPCORE_SRC=$(BUILD_ROOT)\src\AppCore


# Enable generation of .SBR file (for browsing)
# CL_OPTS=$(CL_OPTS) /FR

# add special target for final browse step
# build_and_browse: build $(BUILD_PRODUCT).bsc


# Set the USER_INCLUDE environment variable.
UI=$(TEST_SRC);$(TESTHARNESS_SRC);$(GENERIC_SRC);$(BUILD_ROOT)\Output\Common;$(APPCORE_SRC)


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

RCFILE=langtest.rc
DEFFILE=LangTest.def

# === Object Lists ===

!INCLUDE "$(GENERIC_SRC)\GenericInc.mak"

OBJ_TEST=\
	$(INT_DIR)\autopch\LangTest.obj\
	$(INT_DIR)\autopch\TestBase.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\WriteXml.obj\


OBJ_ALL=$(OBJ_TEST) $(OBJ_GENERIC)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=LangTest

ARG_SRCDIR=$(TEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(TESTHARNESS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(APPCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===

# Target for browse information file
# LangTest.bsc: *.sbr
# 	bscmake /o $@ $**
