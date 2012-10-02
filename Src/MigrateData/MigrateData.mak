# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=MigrateData
BUILD_EXTENSION=dll
BUILD_REGSVR=1

DEFS=$(DEFS) /DGR_FW

MIGRATEDATA_SRC=$(BUILD_ROOT)\Src\MigrateData
DBACCESS_SRC=$(BUILD_ROOT)\Src\DbAccess
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
APPCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFRES_SRC=$(BUILD_ROOT)\Src\AppCore\Res
AFLIB_SRC=$(BUILD_ROOT)\Src\AppCore\AfLib
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
CELLARLIB_SRC=$(BUILD_ROOT)\Src\Cellar\lib


# Set the USER_INCLUDE environment variable. Make sure MigrateData is first, as we want
# to get the Main.h from there, not any of the others (e.g., in Views)
UI=$(MIGRATEDATA_SRC);$(DBACCESS_SRC);$(GENERIC_SRC);$(APPCORE_SRC);$(AFRES_SRC);$(AFLIB_SRC);$(VIEWS_LIB_SRC);$(WIDGETS_SRC);$(CELLARLIB_SRC)

# todo: do we still need this? Copied but not understood from an early version of LingServ.mak...
!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=MigrateData.rc
DEFFILE=MigrateData.def
LINK_LIBS=Widgets.lib AfLib.lib Generic.lib xmlparse.lib Htmlhelp.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_MIGRATEDATA=\
	$(INT_DIR)\genpch\MigrateData.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\DbStringCrawler.obj\
	$(INT_DIR)\autopch\FwDbChangeOverlayTags.obj\
	$(INT_DIR)\autopch\FwStyledText.obj\
	$(INT_DIR)\usepch\TextProps1.obj\
	$(INT_DIR)\autopch\UtilSil2.obj\


OBJ_AUTOPCH=$(OBJ_MIGRATEDATA) $(OBJ_GENERIC)

IDL_MAIN=$(COM_OUT_DIR)\MigrateDataTlb.idl

PS_MAIN=MigrateDataPs

OBJ_ALL= $(OBJ_MIGRATEDATA)

#OBJECTS_IDH=$(COM_INT_DIR)\Objects.idh

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=main

ARG_SRCDIR=$(MIGRATEDATA_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(APPCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(DBACCESS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===
