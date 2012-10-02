# Input
# =====
# BUILD_ROOT: c:\zook\FieldWorks
# BUILD_TYPE: b, d, r, p
# BUILD_CONFIG: Bounds, Debug, Release, Profile
#

# Override build type to always be debug or bounds
!IF "$(BUILD_TYPE)"!="b"
BUILD_TYPE=d
BUILD_CONFIG=Debug
!ENDIF

BUILD_PRODUCT=DebugProcs
BUILD_EXTENSION=dll
BUILD_REGSVR=0

LIB_DST_DIR=$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)

DEBUGPROCS_SRC=$(BUILD_ROOT)\Src\DebugProcs

# Set the USER_INCLUDE environment variable.
UI=$(DEBUGPROCS_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

# === Object Lists ===

OBJ_DP=\
	$(INT_DIR)\autopch\DebugProcs.obj\

OBJ_ALL=$(OBJ_DP)

LINK_LIBS= user32.lib $(LINK_LIBS)

# === Targets ===
BUILD_TARGETS=$(BUILD_TARGETS) $(LIB_DST_DIR)\$(BUILD_PRODUCT).lib

!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(LIB_DST_DIR)\$(BUILD_PRODUCT).lib: $(INT_DIR)\$(BUILD_PRODUCT).lib
	if not exist $(LIB_DST_DIR)\$(NUL) $(MD) $(LIB_DST_DIR)
	$(DISPLAY) Copying $? to $@
	if exist "$?" $(COPYFILE) $? $@

# === Rules ===
PCHNAME=DebugProcs

ARG_SRCDIR=$(DEBUGPROCS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===
