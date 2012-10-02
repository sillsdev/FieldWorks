# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=DbServices
BUILD_EXTENSION=dll
BUILD_REGSVR=1

DBSERVICES_SRC=$(BUILD_ROOT)\Src\DbServices
DBACCESS_SRC=$(BUILD_ROOT)\Src\DbAccess
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
APPCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFRES_SRC=$(BUILD_ROOT)\Src\AppCore\Res
AFLIB_SRC=$(BUILD_ROOT)\Src\AppCore\AfLib
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
CELLARLIB_SRC=$(BUILD_ROOT)\Src\Cellar\lib


# Set the USER_INCLUDE environment variable. Make sure DbServices is first, as we want
# to get the Main.h from there, not any of the others (e.g., in Views)
UI=$(DBSERVICES_SRC);$(DBACCESS_SRC);$(GENERIC_SRC);$(APPCORE_SRC);$(AFRES_SRC);$(AFLIB_SRC);$(VIEWS_LIB_SRC);$(WIDGETS_SRC);$(CELLARLIB_SRC)

# todo: do we still need this? Copied but not understood from an early version of LingServ.mak...
!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=DbServices.rc
DEFFILE=DbServices.def
LINK_LIBS=Generic.lib AfLib.lib Widgets.lib xmlparse.lib Htmlhelp.lib $(LINK_LIBS)
#mstask.lib doesn't seem to be needed.

# === Object Lists ===

OBJ_DBSERVICES=\
	$(INT_DIR)\autopch\Backup.obj\
	$(INT_DIR)\autopch\ZipInvoke.obj\
	$(INT_DIR)\autopch\Remote.obj\
	$(INT_DIR)\autopch\Disconnect.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\

OBJ_AUTOPCH=$(OBJ_DBSERVICES) $(OBJ_GENERIC) $(OBJ_APPCORE)

IDL_MAIN=$(COM_OUT_DIR)\DbServicesTlb.idl

PS_MAIN=DbServicesPs

OBJ_ALL= $(OBJ_DBSERVICES)

#OBJECTS_IDH=$(COM_INT_DIR)\Objects.idh

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=DbServices

ARG_SRCDIR=$(APPCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(DBSERVICES_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"


# === Custom Rules ===

# === Custom Targets ===
