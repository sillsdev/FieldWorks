# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=TstAf
BUILD_EXTENSION=exe
BUILD_REGSVR=0


# EXE_MODULE is defined, to get correct ModuleEntry sections.
DEFS=$(DEFS) /DEXE_MODULE=1

TSTAF_SRC=$(BUILD_ROOT)\Test\TstAf
TSTAF_RES=$(BUILD_ROOT)\Test\TstAf\Res
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore

# Set the USER_INCLUDE environment variable.
UI=$(TSTAF_SRC);$(TSTAF_RES);$(GENERIC_SRC);$(AFCORE_SRC);

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE);
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

RCFILE=TstAf.rc
# DEFFILE=

# === Object Lists ===

!INCLUDE "$(GENERIC_SRC)\GenericInc.mak"


!INCLUDE "$(AFCORE_SRC)\AfCoreInc.mak"


OBJ_TSTAF=\
	$(INT_DIR)\autopch\TstAf.obj\
	$(INT_DIR)\autopch\BorderDialog.obj\


OBJ_ALL=$(OBJ_TSTAF) $(OBJ_AFCORE) $(OBJ_GENERIC)


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=TstAf

ARG_SRCDIR=$(TSTAF_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(TSTAF_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===
