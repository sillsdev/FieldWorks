# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=FwKernel
BUILD_EXTENSION=dll
BUILD_REGSVR=1

DEFS=$(DEFS) /DGRAPHITE2_STATIC /D_MERGE_PROXYSTUB /I"$(COM_OUT_DIR)" /I"$(COM_OUT_DIR_RAW)"

FWKERNEL_SRC=$(BUILD_ROOT)\src\Kernel
GENERIC_SRC=$(BUILD_ROOT)\src\Generic
APPCORE_SRC=$(BUILD_ROOT)\src\AppCore
DEBUGPROCS_SRC=$(BUILD_ROOT)\src\DebugProcs
CELLAR_SRC=$(BUILD_ROOT)\Src\Cellar
VIEWS_LIB=$(BUILD_ROOT)\Src\Views\lib
GR2_INC=$(BUILD_ROOT)\Lib\src\graphite2\include

# Set the USER_INCLUDE environment variable.
UI=$(FWKERNEL_SRC);$(GENERIC_SRC);$(APPCORE_SRC);$(DEBUGPROCS_SRC);$(CELLAR_SRC);$(VIEWS_LIB);$(GR2_INC)

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
LINK_LIBS=Generic.lib xmlparse-utf16.lib Usp10.lib graphite2.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_FWKERNEL=\
	$(INT_DIR)\autopch\KernelGlobals.obj\
	$(INT_DIR)\genpch\TsString.obj\
	$(INT_DIR)\autopch\TsTextProps.obj\
	$(INT_DIR)\autopch\TsStrFactory.obj\
	$(INT_DIR)\autopch\TsPropsFactory.obj\
	$(INT_DIR)\autopch\TextServ.obj\
	$(INT_DIR)\autopch\TsMultiStr.obj\
	$(INT_DIR)\usepch\TextProps1.obj\
	$(INT_DIR)\autopch\ActionHandler.obj\
	$(INT_DIR)\genpch\RegexMatcherWrapper.obj\
	$(INT_DIR)\autopch\LgIcuWrappers.obj\
	$(INT_DIR)\autopch\UniscribeEngine.obj\
	$(INT_DIR)\autopch\UniscribeSegment.obj\
	$(INT_DIR)\autopch\GraphiteEngine.obj\
	$(INT_DIR)\autopch\GraphiteSegment.obj\
	$(INT_DIR)\autopch\RomRenderEngine.obj\
	$(INT_DIR)\autopch\RomRenderSegment.obj\
	$(INT_DIR)\autopch\LgSimpleEngines.obj\
	$(INT_DIR)\autopch\LgIcuCharPropEngine.obj\
	$(INT_DIR)\autopch\LgUnicodeCollater.obj\
	$(INT_DIR)\autopch\LgKeymanHandler.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\FwStyledText.obj\
	$(INT_DIR)\autopch\WriteXml.obj\
	$(INT_DIR)\autopch\DebugReport.obj\
	$(INT_DIR)\autopch\FwXml.obj\
	$(INT_DIR)\autopch\dlldatax.obj\


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

ARG_SRCDIR=$(CELLAR_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"


# === Custom Rules ===


# === Custom Targets ===
