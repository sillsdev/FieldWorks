# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=Widgets
BUILD_EXTENSION=lib
BUILD_REGSVR=1

GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
WIDGETS_SRC=$(BUILD_ROOT)\Src\Widgets
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFCORE_RES=$(BUILD_ROOT)\Src\AppCore\Res
# AFLIB_SRC is necessary to pull in the main.h file for the AfLib.lib.
AFLIB_SRC=$(BUILD_ROOT)\Src\AppCore\AfLib
VIEW_LIB_SRC=$(BUILD_ROOT)\src\views\lib

# Set the USER_INCLUDE environment variable.
UI=$(AFLIB_SRC);$(WIDGETS_SRC);$(AFCORE_SRC);$(AFCORE_RES);$(VIEW_LIB_SRC);$(GENERIC_SRC)

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

!INCLUDE "$(WIDGETS_SRC)\WidgetsInc.mak


OBJ_ALL= $(OBJ_WIDGETS)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=Language

ARG_SRCDIR=$(WIDGETS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===
