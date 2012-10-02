# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=AfLib
BUILD_EXTENSION=lib
BUILD_REGSVR=1

# EXE_MODULE is defined, to get correct ModuleEntry sections for AfApp.
DEFS=$(DEFS) /DEXE_MODULE=1

AF_SRC=$(BUILD_ROOT)\Src\AppCore
AFRES_SRC=$(BUILD_ROOT)\Src\AppCore\Res
# AFLIB_SRC is necessary to pull in the main.h file for the AfLib.lib.
AFLIB_SRC=$(BUILD_ROOT)\Src\AppCore\AfLib
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets

# Set the USER_INCLUDE environment variable.
UI=$(AFLIB_SRC);$(AF_SRC);$(AFRES_SRC);$(GENERIC_SRC);$(VIEWS_LIB_SRC);$(WIDGETS_SRC);

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

!INCLUDE "$(AF_SRC)\AfCoreInc.mak
!INCLUDE "$(AF_SRC)\AfDeCoreInc.mak


#IDL_MAIN=$(COM_OUT_DIR)\LanguageTlb.idl

#PS_MAIN=LanguagePs

OBJ_ALL= $(OBJ_AFCORE) $(OBJ_AFDECORE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=Language

ARG_SRCDIR=$(AF_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(VIEWS_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===
