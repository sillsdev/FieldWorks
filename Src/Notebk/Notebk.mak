# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=FwNotebook
BUILD_EXTENSION=exe
BUILD_REGSVR=1

# EXE_MODULE is defined, to get correct ModuleEntry sections.
DEFS=$(DEFS) /DEXE_MODULE=1

RN_SRC=$(BUILD_ROOT)\Src\NoteBk
RN_RES=$(BUILD_ROOT)\Src\NoteBk\Res
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFCORE_RES=$(BUILD_ROOT)\Src\AppCore\Res
CELLAR_LIB_SRC=$(BUILD_ROOT)\src\Cellar\Lib
CELLAR_SRC=$(BUILD_ROOT)\src\Cellar
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
VIEW_LIB_SRC=$(BUILD_ROOT)\src\views\lib
HELP_DIR=$(BUILD_ROOT)\help
ENC_CNVTRS_DIR=$(BUILD_ROOT)\DistFiles

NB_XML=$(BUILD_ROOT)\src\Notebk\XML

# Set the USER_INCLUDE environment variable.
UI=$(RN_SRC);$(RN_RES);$(GENERIC_SRC);$(AFCORE_SRC);$(AFCORE_RES);$(CELLAR_LIB_SRC)
UI=$(UI);$(CELLAR_SRC);$(WIDGETS_SRC);$(VIEW_LIB_SRC);$(ENC_CNVTRS_DIR)

KERNEL_SRC=$(BUILD_ROOT)\Src\Kernel
UI=$(UI);$(KERNEL_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

XML_INC=$(NB_XML)

# CL_OPTS=$(CL_OPTS) /showIncludes

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=Notebk.rc
# DEFFILE=Notebk.def
!INCLUDE "$(AFCORE_SRC)\AfCoreInc.mak"
LINK_LIBS= AfLib.lib Widgets.lib Generic.lib uuid.lib advapi32.lib kernel32.lib odbc32.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_RN=\
	$(INT_DIR)\autopch\RnDeSplitChild.obj\
	$(INT_DIR)\autopch\NoteBk.obj\
	$(INT_DIR)\autopch\RnCustDocVc.obj\
	$(INT_DIR)\autopch\RnTlsOptDlg.obj\
	$(INT_DIR)\autopch\AfFwTool.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\RnCustBrowseVc.obj\
	$(INT_DIR)\autopch\RnImportDlg.obj\
	$(INT_DIR)\autopch\RnCustomExport.obj\
	$(INT_DIR)\autopch\RnDeFeRoleParts.obj\
	$(INT_DIR)\autopch\RnDocSplitChild.obj\
	$(INT_DIR)\autopch\RnBrowseSplitChild.obj\


# ENHANCE JohnT: possibly remove $(OBJ_GEN_DATA) if only needed for ODBC.

OBJ_ALL=$(OBJ_RN) $(OBJ_GENERIC) $(OBJ_GEN_DATA)


IDL_MAIN=$(COM_OUT_DIR)\NotebkTlb.idl


SQL_MAIN=Notebk.sql


SQO_MAIN=Notebk.sqo


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(INT_DIR)\$(RCFILE:.rc=.res): $(RN_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(RN_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(RN_RES)\*.rc
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.rc


# === Rules ===
PCHNAME=Notebk

ARG_SRCDIR=$(CMNDLGUTILS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(RN_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(RN_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(NB_XML)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_SRC)
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
