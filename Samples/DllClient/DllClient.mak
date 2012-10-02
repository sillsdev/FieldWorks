# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=DllClient
BUILD_EXTENSION=exe
BUILD_REGSVR=1

# EXE_MODULE is defined, to get correct ModuleEntry sections.
DEFS=$(DEFS) /DEXE_MODULE=1

DC_SRC=$(BUILD_ROOT)\Samples\DllClient
DC_RES=$(BUILD_ROOT)\Samples\DllClient\Res
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFCORE_RES=$(BUILD_ROOT)\Src\AppCore\Res
CELLAR_SRC=$(BUILD_ROOT)\src\Cellar\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
VIEW_LIB_SRC=$(BUILD_ROOT)\src\views\lib
HELP_DIR=$(BUILD_ROOT)\help

# Set the USER_INCLUDE environment variable.
UI=$(DC_SRC);$(DC_RES);$(GENERIC_SRC);$(AFCORE_SRC);$(AFCORE_RES);$(CELLAR_SRC);$(WIDGETS_SRC);$(VIEW_LIB_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(BUILD_ROOT)\Output\Common;$(PATH)

RCFILE=DllClient.rc
LINK_LIBS= AfLib.lib Widgets.lib Generic.lib Version.lib HtmlHelp.lib atl.lib uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib odbc32.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_HW=\
	$(INT_DIR)\autopch\DllClient.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\

OBJ_ALL=$(OBJ_HW) $(OBJ_GENERIC)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(INT_DIR)\$(RCFILE:.rc=.res): $(DC_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(DC_RES)\*.rc
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.rc


# === Rules ===
PCHNAME=DllClient

ARG_SRCDIR=$(DC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(DC_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(VIEW_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Targets ===

# === Special dependencies ===
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\aflib.lib
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Generic.lib
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Widgets.lib
