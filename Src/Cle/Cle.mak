# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=FwListEditor
BUILD_EXTENSION=exe
BUILD_REGSVR=1

# EXE_MODULE is defined, to get correct ModuleEntry sections.
DEFS=$(DEFS) /DEXE_MODULE=1
CLE_SRC=$(BUILD_ROOT)\Src\Cle
CLE_RES=$(BUILD_ROOT)\Src\Cle\Res
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFCORE_RES=$(BUILD_ROOT)\Src\AppCore\Res
CELLAR_SRC=$(BUILD_ROOT)\src\Cellar\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
VIEW_LIB_SRC=$(BUILD_ROOT)\src\views\lib
HTMLHELP_LIB=$(BUILD_ROOT)\src\lib
HELP_DIR=$(BUILD_ROOT)\help
# LINGLIB_SRC=$(BUILD_ROOT)\Src\LingLib
# LINGLIB_RES=$(BUILD_ROOT)\Src\LingLib\Res

# Set the USER_INCLUDE environment variable.
# ;$(LINGLIB_SRC);$(LINGLIB_RES)
UI=$(CLE_SRC);$(CLE_RES);$(GENERIC_SRC);$(AFCORE_SRC);$(AFCORE_RES)
UI=$(UI);$(CELLAR_SRC);$(WIDGETS_SRC);$(VIEW_LIB_SRC);$(HTMLHELP_LIB)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=Cle.rc
!INCLUDE "$(AFCORE_SRC)\AfCoreInc.mak"
LINK_LIBS= AfLib.lib Widgets.lib Generic.lib uuid.lib advapi32.lib\
	kernel32.lib ole32.lib oleaut32.lib odbc32.lib $(LINK_LIBS)
# Ling.lib
# === Object Lists ===

OBJ_CLE=\
	$(INT_DIR)\autopch\CleDeSplitChild.obj\
	$(INT_DIR)\autopch\Cle.obj\
	$(INT_DIR)\autopch\CleCustDocVc.obj\
	$(INT_DIR)\autopch\CleTlsOptDlg.obj\
	$(INT_DIR)\autopch\CleLstNotFndDlg.obj\
	$(INT_DIR)\autopch\AfFwTool.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\CleCustomExport.obj\


OBJ_ALL=$(OBJ_CLE) $(OBJ_GENERIC)


IDL_MAIN=$(COM_OUT_DIR)\CleTlb.idl

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(INT_DIR)\$(RCFILE:.rc=.res): $(CLE_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(CLE_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(CLE_RES)\*.rc
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.rc
# $(INT_DIR)\$(RCFILE:.rc=.res): $(LINGLIB_RES)\*.rc


# === Rules ===
PCHNAME=Notebk

ARG_SRCDIR=$(CMNDLGUTILS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CLE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CLE_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(VIEW_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(WIDGETS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Targets ===

# === Special dependencies ===
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\aflib.lib
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Generic.lib
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Widgets.lib
# $(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Ling.lib
