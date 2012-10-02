# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=Generic
BUILD_EXTENSION=lib
BUILD_REGSVR=1

GENERIC_SRC=$(BUILD_ROOT)\Src\Generic

# Set the USER_INCLUDE environment variable.
UI=$(GENERIC_SRC);

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=
DEFFILE=


# === Object Lists ===

# This includes ModuleEntry, which we don't want.
!INCLUDE "$(GENERIC_SRC)\GenericInc.mak

OBJ_ALL= $(OBJ_GENERIC)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=Language

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===
