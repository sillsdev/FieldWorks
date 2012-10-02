# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=FwCellar
# BUILD_EXTENSION is empty indicating that there is no main executable target
BUILD_EXTENSION=
BUILD_REGSVR=1


CELLAR_SRC=$(BUILD_ROOT)\src\Cellar
CELLAR_LIB_SRC=$(BUILD_ROOT)\src\Cellar\Lib
GENERIC_SRC=$(BUILD_ROOT)\src\Generic

# Set the USER_INCLUDE environment variable.
UI=$(CELLAR_SRC);$(CELLAR_LIB_SRC);$(GENERIC_SRC)


!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF


!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

RCFILE=FwCellar.rc
DEFFILE=FwCellar.def
LINK_LIBS=uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib $(LINK_LIBS)

# === Object Lists ===

IDL_MAIN=$(COM_OUT_DIR)\FwCellarTlb.idl


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=FwCellar

ARG_SRCDIR=$(CELLAR_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Targets ===
