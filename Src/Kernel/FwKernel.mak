# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=FwKernel
BUILD_EXTENSION=dll
BUILD_REGSVR=1


FWKERNEL_SRC=$(BUILD_ROOT)\src\Kernel
GENERIC_SRC=$(BUILD_ROOT)\src\Generic
APPCORE_SRC=$(BUILD_ROOT)\src\AppCore
DBSERVICES_SRC=$(BUILD_ROOT)\src\DbServices
DEBUGPROCS_SRC=$(BUILD_ROOT)\src\DebugProcs


# Set the USER_INCLUDE environment variable.
UI=$(FWKERNEL_SRC);$(GENERIC_SRC);$(APPCORE_SRC);$(DBSERVICES_SRC);$(DEBUGPROCS_SRC)

DBACCESS_SRC=$(BUILD_ROOT)\src\DbAccess
LANGUAGE_SRC=$(BUILD_ROOT)\src\Language
UI=$(UI);$(DBACCESS_SRC);$(LANGUAGE_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF


!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=FwKernel.rc
DEFFILE=FwKernel.def
LINK_LIBS=Generic.lib xmlparse.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_FWKERNEL=\
	$(INT_DIR)\autopch\IcuCleanupManager.obj\
	$(INT_DIR)\genpch\TsString.obj\
	$(INT_DIR)\autopch\TsTextProps.obj\
	$(INT_DIR)\autopch\TsStrFactory.obj\
	$(INT_DIR)\autopch\TsPropsFactory.obj\
	$(INT_DIR)\autopch\TextServ.obj\
	$(INT_DIR)\autopch\TsMultiStr.obj\
	$(INT_DIR)\usepch\TextProps1.obj\
	$(INT_DIR)\autopch\ActionHandler.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\FwStyledText.obj\
	$(INT_DIR)\autopch\WriteXml.obj\
	$(INT_DIR)\autopch\DebugReport.obj\


OBJ_ALL=$(OBJ_FWKERNEL)


IDL_MAIN=$(COM_OUT_DIR)\FwKernelTlb.idl


PS_MAIN=FwKernelPs


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=main

ARG_SRCDIR=$(FWKERNEL_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(APPCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"


# === Custom Rules ===


# === Custom Targets ===
