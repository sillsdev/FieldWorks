# Input
# =====
# BUILD_ROOT: d:\src\fw
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=ConvertString
BUILD_EXTENSION=dll
BUILD_REGSVR=1


CS_SRC=$(BUILD_ROOT)\bin\src\Uniconvert\ConvertString
GENERIC_SRC=$(BUILD_ROOT)\src\Generic

# Set the USER_INCLUDE environment variable.
UI=$(CS_SRC);$(GENERIC_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF


!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

DEFFILE=ConvertString.def
LINK_LIBS= Generic.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_CS=\
	$(INT_DIR)\autopch\ConvertString.obj\
	$(INT_DIR)\autopch\Trie.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\

OBJ_ALL=$(OBJ_CS)


IDL_MAIN=$(COM_OUT_DIR)\ConvertStringTlb.idl


PS_MAIN=ConvertStringPs


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=ConvertString

ARG_SRCDIR=$(CS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===
