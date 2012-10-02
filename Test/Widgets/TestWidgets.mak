# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: b, d, r, p
# BUILD_CONFIG: Bounds, Debug, Release, Profile
#

BUILD_PRODUCT=TestWidgets
BUILD_EXTENSION=exe
BUILD_REGSVR=0

# EXE_MODULE is defined, to get correct ModuleEntry sections.
DEFS=$(DEFS) /DEXE_MODULE=1

VIEWLIB_SRC=$(BUILD_ROOT)\src\Views\lib
TSTWIDGETS_SRC=$(BUILD_ROOT)\Test\Widgets
WIDGETS_SRC=$(BUILD_ROOT)\Src\Widgets
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFCORE_RES=$(BUILD_ROOT)\Src\AppCore\Res
COMMON_SRC=$(BUILD_ROOT)\Output\Common

# Set the USER_INCLUDE environment variable.
UI=$(TSTWIDGETS_SRC);$(VIEWLIB_SRC);$(WIDGETS_SRC);$(GENERIC_SRC);$(AFCORE_SRC);$(AFCORE_RES);$(COMMON_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE);
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(AFCORE_SRC)\AfCoreInc.mak"
LINK_LIBS=AfLib.lib Widgets.lib Generic.lib uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib odbc32.lib $(LINK_LIBS) uxtheme.lib


!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# Ensure that the ICU dlls can be accessed at registration.
PATH=$(PATH);$(BUILD_ROOT)\output\$(BUILD_CONFIG)

RCFILE=SdkWidgets.rc
# DEFFILE=

# === Object Lists ===

OBJ_TSTWIDGETS=\
	$(INT_DIR)\autopch\SdkWidgets.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\


OBJ_ALL=$(OBJ_TSTWIDGETS)


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=TstAf

ARG_SRCDIR=$(TSTWIDGETS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(COMMON_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===

# === Special dependencies ===
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\aflib.lib
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Generic.lib
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Widgets.lib
