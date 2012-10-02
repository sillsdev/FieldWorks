# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=FeatSys
# BUILD_EXTENSION is empty indicating that there is no main executable target (yet).
BUILD_EXTENSION=
BUILD_REGSVR=1


FS_XML=$(BUILD_ROOT)\src\FeatSys\XML

CELLAR_SRC=$(BUILD_ROOT)\src\Cellar\Lib
FS_SRC=$(BUILD_ROOT)\src\FeatSys

# Set the USER_INCLUDE environment variable.
UI=$(FS_SRC);$(CELLAR_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

XML_INC=$(FS_XML)


!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=FeatSys.rc
DEFFILE=FeatSys.def
LINK_LIBS=uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib $(LINK_LIBS)
#LINK_LIBS=DebugProcs.lib xmlparse.lib uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib shlwapi.lib $(LINK_LIBS)

# === Object Lists ===

IDL_MAIN=$(COM_OUT_DIR)\FeatSys.idl


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=FeatSys

ARG_SRCDIR=$(FS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(FS_XML)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Targets ===
