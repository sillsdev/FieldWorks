# Input
# =====
# BUILD_ROOT: C:\Development\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

# Override build type to always be debug or bounds
!IF "$(BUILD_TYPE)"!="b"
BUILD_TYPE=d
BUILD_CONFIG=Debug
!ENDIF

BUILD_PRODUCT=TstLanguage
BUILD_EXTENSION=dll
BUILD_REGSVR=1


TEST_SRC=$(BUILD_ROOT)\Test\TstLanguage
TESTHARNESS_SRC=$(BUILD_ROOT)\Test
GENERIC_SRC=$(BUILD_ROOT)\src\Generic
FWUTILS_SRC=$(BUILD_ROOT)\src\FWUtils

# Set the USER_INCLUDE environment variable.
UI=$(TEST_SRC);$(TESTHARNESS_SRC);$(GENERIC_SRC);$(FWUTILS_SRC);$(BUILD_ROOT)\Output\Common


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
# CL=$(CL) /E

RCFILE=
DEFFILE=TstLanguage.def
LINK_LIBS=DebugProcs.lib uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib $(LINK_LIBS)

# === Object Lists ===

!INCLUDE "$(GENERIC_SRC)\GenericInc.mak"
# !INCLUDE "$(FWUTILS_SRC)\FWUtilsInc.mak"

OBJ_TEST=\
	$(INT_DIR)\autopch\TstLanguage.obj\


OBJ_ALL=$(OBJ_TEST) $(OBJ_GENERIC) $(OBJ_FWUTILS)\
	$(INT_DIR)\autopch\TestBase.obj\


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=TstLanguage

ARG_SRCDIR=$(TEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(TESTHARNESS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(FWUTILS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===
