# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=CmnFwDlgs
BUILD_EXTENSION=dll
BUILD_REGSVR=1


COMDLGS_SRC=$(BUILD_ROOT)\Src\CommonCOMDlgs
COMDLGS_RES=$(BUILD_ROOT)\Src\CommonCOMDlgs\Res
DBACCESS_SRC=$(BUILD_ROOT)\Src\DbAccess
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFCORE_RES=$(BUILD_ROOT)\Src\AppCore\Res
AFLIB_SRC=$(BUILD_ROOT)\Src\AppCore\AfLib
VIEWS_SRC=$(BUILD_ROOT)\Src\Views
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
WIDGETS_RES=$(BUILD_ROOT)\src\Widgets\Res
CELLARLIB_SRC=$(BUILD_ROOT)\Src\Cellar\lib

# Set the USER_INCLUDE environment variable.
UI=$(COMDLGS_SRC);$(COMDLGS_RES);$(DBACCESS_SRC);$(GENERIC_SRC);$(AFCORE_SRC);$(AFCORE_RES)
UI=$(UI);$(AFLIB_SRC);$(VIEWS_LIB_SRC);$(WIDGETS_SRC);$(WIDGETS_RES);$(CELLARLIB_SRC);$(VIEWS_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE);
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=CmnFwDlgs.rc
DEFFILE=CmnFwDlgs.def
LINK_LIBS= Generic.lib AfLib.lib Widgets.lib Htmlhelp.lib xmlparse.lib ws2_32.lib Mpr.lib $(LINK_LIBS)

# === Object Lists ===

# ModuleEntry must always be included explicitly, because some components need to compile
# a DLL version of it, others an EXE version.
OBJ_COMDLGS=\
	$(INT_DIR)\autopch\OpenProjDlg.obj\
	$(INT_DIR)\autopch\OpenFWProjectDlg.obj\
	$(INT_DIR)\autopch\FWStylesDlg.obj\
	$(INT_DIR)\autopch\TeStylesDlg.obj\
	$(INT_DIR)\autopch\FwExportDlg.obj\
	$(INT_DIR)\autopch\AfExportDlg.obj\
	$(INT_DIR)\autopch\DbStringCrawler.obj\
	$(INT_DIR)\autopch\FwDbMergeWrtSys.obj\
	$(INT_DIR)\autopch\FwDbMergeStyles.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\RnAnthroListDlg.obj\
	$(INT_DIR)\autopch\FwCheckAnthroList.obj\

OBJ_AUTOPCH=$(OBJ_COMDLGS) $(OBJ_GENERIC) $(OBJ_APPCORE)

IDL_MAIN=$(COM_OUT_DIR)\CmnFwDlgsTlb.idl

PS_MAIN=CmnFwDlgsPs

OBJ_ALL= $(OBJ_COMDLGS)

#OBJECTS_IDH=$(COM_INT_DIR)\Objects.idh

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(INT_DIR)\$(RCFILE:.rc=.res): $(COMDLGS_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(COMDLGS_RES)\*.rc
# $(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.bmp
# $(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.h
# $(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.rc

# === Rules ===
PCHNAME=COMDLGS

ARG_SRCDIR=$(COMDLGS_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_XML)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(LANG_XML)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(COMDLGS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===

# === Special dependencies ===
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\aflib.lib
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Generic.lib
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Widgets.lib
