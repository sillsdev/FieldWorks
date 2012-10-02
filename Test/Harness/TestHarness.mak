# Input
# =====
# BUILD_ROOT: C:\fieldworks (or equivalent)
# BUILD_TYPE: b, d, r, p
# BUILD_CONFIG: Bounds, Debug, Release, Profile
#

# Override build type to always be debug or bounds
!IF "$(BUILD_TYPE)"!="b"
BUILD_TYPE=d
BUILD_CONFIG=Debug
!ENDIF

BUILD_PRODUCT=TestHarness
BUILD_EXTENSION=dll
BUILD_REGSVR=1


TEST_SRC=$(BUILD_ROOT)\Test
TESTHARNESS_SRC=$(BUILD_ROOT)\Test\Harness
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
COMMON_SRC=$(BUILD_ROOT)\Output\Common

# Set the USER_INCLUDE environment variable.
UI=$(TEST_SRC);$(TESTHARNESS_SRC);$(GENERIC_SRC);$(COMMON_SRC)


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

# Ensure that the ICU dlls can be accessed at registration.
PATH=$(COM_OUT_DIR);$(PATH);$(BUILD_ROOT)\output\$(BUILD_CONFIG)

RCFILE=
DEFFILE=TestHarness.def
LINK_LIBS= Generic.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_HARNESS=\
	$(INT_DIR)\autopch\TestHarness.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\

OBJ_ALL=$(OBJ_HARNESS)

IDL_MAIN=$(COM_OUT_DIR)\TestHarnessTlb.idl

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=TestHarness

ARG_SRCDIR=$(TESTHARNESS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(COMMON_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===
