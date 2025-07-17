# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=Views
BUILD_EXTENSION=dll
BUILD_REGSVR=1

DEFS=$(DEFS) /DGRAPHITE2_STATIC /DGR_FW /DVIEWSDLL /D_MERGE_PROXYSTUB /I"$(COM_OUT_DIR)" /I"$(COM_OUT_DIR_RAW)"

VIEWS_SRC=$(BUILD_ROOT)\Src\Views
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
GR2_INC=$(BUILD_ROOT)\Lib\src\graphite2\include
DEBUGPROCS_SRC=$(BUILD_ROOT)\src\DebugProcs
KERNEL_SRC=$(BUILD_ROOT)\src\Kernel

# Set the USER_INCLUDE environment variable.
UI=$(VIEWS_SRC);$(VIEWS_LIB_SRC);$(GENERIC_SRC);$(AFCORE_SRC);$(DEBUGPROCS_SRC);$(GR2_INC);$(KERNEL_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE);
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=Views.rc
DEFFILE=Views.def
LINK_LIBS= Generic.lib Usp10.lib xmlparse-utf16.lib graphite2.lib $(LINK_LIBS)
PS_OBJ_DEPS= $(OBJ_DIR)\Common\FwKernel\FwKernelPs_p.obj $(OBJ_DIR)\Common\FwKernel\FwKernelPs_i.obj

# === Object Lists ===

# ModuleEntry must always be included explicitly, because some components need to compile
# a DLL version of it, others an EXE version.
OBJ_VIEWS=\
	$(INT_DIR)\autopch\ViewsGlobals.obj\
	$(INT_DIR)\autopch\VwInvertedViews.obj\
	$(INT_DIR)\autopch\VwAccessRoot.obj\
	$(INT_DIR)\autopch\VwOverlay.obj\
	$(INT_DIR)\autopch\VwPropertyStore.obj\
	$(INT_DIR)\autopch\ExplicitInstantiation.obj\
	$(INT_DIR)\autopch\VwSimpleBoxes.obj\
	$(INT_DIR)\autopch\VwTextBoxes.obj\
	$(INT_DIR)\autopch\VwRootBox.obj\
	$(INT_DIR)\autopch\VwLayoutStream.obj\
	$(INT_DIR)\autopch\VwEnv.obj\
	$(INT_DIR)\autopch\VwNotifier.obj\
	$(INT_DIR)\autopch\VwSelection.obj\
	$(INT_DIR)\autopch\VwTableBox.obj\
	$(INT_DIR)\autopch\VwGraphics.obj\
	$(INT_DIR)\autopch\VwTxtSrc.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\AfColorTable.obj\
	$(INT_DIR)\autopch\AfGfx.obj\
	$(INT_DIR)\autopch\VwPrintContext.obj\
	$(INT_DIR)\autopch\VwBaseDataAccess.obj\
	$(INT_DIR)\autopch\VwCacheDa.obj\
	$(INT_DIR)\autopch\ActionHandler.obj\
	$(INT_DIR)\autopch\VwUndo.obj\
	$(INT_DIR)\autopch\VwLazyBox.obj\
	$(INT_DIR)\autopch\VwPattern.obj\
	$(INT_DIR)\autopch\FwStyledText.obj\
	$(INT_DIR)\autopch\VwSynchronizer.obj\
	$(INT_DIR)\autopch\VwTextStore.obj\
	$(INT_DIR)\autopch\VwBaseVirtualHandler.obj\
	$(INT_DIR)\autopch\UniscribeEngine.obj\
	$(INT_DIR)\autopch\UniscribeSegment.obj\
	$(INT_DIR)\autopch\GraphiteEngine.obj\
	$(INT_DIR)\autopch\GraphiteSegment.obj\
	$(INT_DIR)\autopch\LgLineBreaker.obj\
	$(INT_DIR)\autopch\LgUnicodeCollater.obj\
	$(INT_DIR)\autopch\TsString.obj\
	$(INT_DIR)\autopch\TsTextProps.obj\
	$(INT_DIR)\autopch\TsStrFactory.obj\
	$(INT_DIR)\autopch\TsPropsFactory.obj\
	$(INT_DIR)\autopch\TextServ.obj\
	$(INT_DIR)\autopch\TextProps1.obj\
	$(INT_DIR)\autopch\DebugReport.obj\
	$(INT_DIR)\autopch\dlldatax.obj\


OBJ_AUTOPCH=$(OBJ_VIEWS) $(OBJ_GENERIC) $(PS_OBJ_DEPS)

IDL_MAIN=$(COM_OUT_DIR)\ViewsTlb.idl

PS_MAIN=ViewsPs

OBJ_ALL= $(OBJ_VIEWS) $(OBJ_GENERIC) $(OBJ_NOPCH) $(OBJ_GENPCH) $(PS_OBJ_DEPS)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=Views

ARG_SRCDIR=$(VIEWS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(VIEWS_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(FWUTILS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(LANG_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# everything depends on the main IDL, except itself
#$(OBJ_ALL): $(TLB_ALL)

# because of importlib statements, must build lower TLB's before higher.
$(COM_OUT_DIR)\FwKernelTlb.tlb: $(COM_OUT_DIR)\FwKernelTlb.idl

$(COM_OUT_DIR)\ViewsTlb.tlb: $(COM_OUT_DIR)\FwKernelTlb.tlb

# === Custom Targets ===
