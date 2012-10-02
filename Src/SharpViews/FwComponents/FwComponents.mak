# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=FwComponents
BUILD_EXTENSION=dll
BUILD_REGSVR=1

DEFS=$(DEFS) /DGR_FW /DVIEWSDLL

VIEWS_SRC=$(BUILD_ROOT)\Src\SharpViews\FwComponents
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\SharpViews\FwComponents\Lib
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
COMMONDLGS_SRC=$(BUILD_ROOT)\Src\CommonCOMDlgs
GRENG_LIB_SRC=$(BUILD_ROOT)\Src\Graphite\lib
GRFW_SRC=$(BUILD_ROOT)\Src\Graphite\FwOnly

# Set the USER_INCLUDE environment variable.
UI=$(VIEWS_SRC);$(VIEWS_LIB_SRC);$(GENERIC_SRC);$(AFCORE_SRC);$(COMMONDLGS_SRC);$(GRENG_LIB_SRC);$(GRFW_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE);
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=FwComponents.rc
DEFFILE=FwComponents.def
LINK_LIBS= Generic.lib $(LINK_LIBS)

# === Object Lists ===

# ModuleEntry must always be included explicitly, because some components need to compile
# a DLL version of it, others an EXE version.
OBJ_VIEWS=\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\VwGraphics.obj\
	$(INT_DIR)\autopch\AfColorTable.obj\
	$(INT_DIR)\autopch\AfGfx.obj\


OBJ_AUTOPCH=$(OBJ_VIEWS) $(OBJ_GENERIC)

IDL_MAIN=$(COM_OUT_DIR)\FwComponentsTlb.idl

PS_MAIN=FwComponentsPs

OBJ_ALL= $(OBJ_VIEWS) $(OBJ_GENERIC) $(OBJ_NOPCH) $(OBJ_GENPCH)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=FwComponents

ARG_SRCDIR=$(VIEWS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(VIEWS_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# everything depends on the main IDL, except itself
#$(OBJ_ALL): $(TLB_ALL)

# === Custom Targets ===
