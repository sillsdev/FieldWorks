# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=Graphite
BUILD_EXTENSION=dll
BUILD_REGSVR=1

# EXE_MODULE not defined, to get correct ModuleEntry sections.
# DEFS=$(DEFS) /DEXE_MODULE=1
DEFS=$(DEFS) /DGR_FW /DTRACING

GRE_SRC=$(BUILD_ROOT)\Src\Graphite\GrEngine
GRE_RES=$(BUILD_ROOT)\Src\Graphite\GrEngine
GRE_FW=$(BUILD_ROOT)\Src\Graphite\FwOnly
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
GR_LIB_SRC=$(BUILD_ROOT)\Src\Graphite\lib
TTF_LIB_SRC=$(BUILD_ROOT)\Src\Graphite\TtfUtil
VIEW_LIB_SRC=$(BUILD_ROOT)\src\views\lib


# 4 - This allows the compiler to find the headers needed when building the cpp files
# Set the USER_INCLUDE environment variable.
UI=$(GRE_SRC);$(GRE_RES);$(GRE_FW);$(GENERIC_SRC);$(GR_LIB_SRC);$(TTF_LIB_SRC);$(VIEW_LIB_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

#adjust switches to same value as original Visual Studio project
CL_OPTS=$(CL_OPTS:/WX=)		# turn off "treat warnings as errors" switch
CL_OPTS=$(CL_OPTS:/W4=/W3)	# use warning level 3 instead of 4
#CL_OPTS=$(CL_OPTS:/Gr= )   # turn off _fastcall convention, use _cdecl convention by default
#CL_OPTS=/GZ $(CL_OPTS)     # turn on "catch release build errors in debug build" switch
#CL_OPTS=$(CL_OPTS:/Zi=/ZI) # build edit & continue debug info instead of standard debug info

#LINK_OPTS=$(LINK_OPTS:incremental:no=incremental:yes) # turn on incremental linking

#CL_OPTS=/FR$(INT_DIR)\autopch\ $(CL_OPTS) # build browser info, need to run bscmake by hand

# === Profiling ===
# Turn on profiling if needed. This should probably go in _init.mak eventually
# Actually profiling seems to work with a release build. The /profile switch on the linker
#  probably isn't needed since the release build creates a map file anyway.
!IF "$(BUILD_TYPE)"=="p"
LINK_OPTS=$(LINK_OPTS) /PROFILE
# shell32.lib causes a warning with profile build though unresolved externals appear
#  if below uncommented
#LINK_LIBS=$(LINK_LIBS:shell32.lib= )
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# PATH=$(BUILD_ROOT)\Output\Common;$(PATH)

RCFILE=Graphite.rc
DEFFILE=Graphite.def
LINK_LIBS= Generic.lib $(LINK_LIBS)

# === Object Lists ===
# 2 - This indicates the object files needed to create BUILD_PRODUCT
OBJ_GRE=\
	$(INT_DIR)\autopch\explicit_instantiations.obj\
	$(INT_DIR)\autopch\FileInput.obj\
	$(INT_DIR)\autopch\GrCharStream.obj\
	$(INT_DIR)\autopch\GrClassTable.obj\
	$(INT_DIR)\autopch\Font.obj\
	$(INT_DIR)\autopch\WinFont.obj\
	$(INT_DIR)\autopch\FontCache.obj\
	$(INT_DIR)\autopch\GrEngine.obj\
	$(INT_DIR)\autopch\FontFace.obj\
	$(INT_DIR)\autopch\FwGrEngine.obj\
	$(INT_DIR)\autopch\GrFeature.obj\
	$(INT_DIR)\autopch\GrFSM.obj\
	$(INT_DIR)\autopch\GrGlyphTable.obj\
	$(INT_DIR)\autopch\GrPass.obj\
	$(INT_DIR)\autopch\GrPassActionCode.obj\
	$(INT_DIR)\autopch\SegmentPainter.obj\
	$(INT_DIR)\autopch\WinSegmentPainter.obj\
	$(INT_DIR)\autopch\SegmentAux.obj\
	$(INT_DIR)\autopch\Segment.obj\
	$(INT_DIR)\autopch\FwGrSegment.obj\
	$(INT_DIR)\autopch\GrSlotState.obj\
	$(INT_DIR)\autopch\GrSlotStream.obj\
	$(INT_DIR)\autopch\GrTableManager.obj\
	$(INT_DIR)\autopch\TestFSM.obj\
	$(INT_DIR)\autopch\TestPasses.obj\
	$(INT_DIR)\autopch\TransductionLog.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\TtfUtil.obj\
	$(INT_DIR)\autopch\GrUtil.obj\
	$(INT_DIR)\autopch\GrPlatform.obj\
	$(INT_DIR)\autopch\FwGraphiteProcess.obj\
	$(INT_DIR)\autopch\FwGr.obj\

#LINK_LIBS=Version.lib $(LINK_LIBS)

OBJ_ALL=$(OBJ_GRE)

# === Targets ===
#BUILD_TARGETS=$(BUILD_TARGETS)

# 1 - this indicates that BUILD_PRODUCT is the primary target, files needed by link drive everything
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(INT_DIR)\$(RCFILE:.rc=.res): $(GRE_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(GRE_RES)\*.rc

# === Rules ===
PCHNAME=GrEngine

# 3 - These indicate how to build the object files needed for BUILD_PRODUCT from cpp files
# no dependency on .h files
ARG_SRCDIR=$(GRE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GRE_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GR_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(TTF_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Targets ===

# $(OUT_DIR)\Graphite.dll:$(OUT_DIR)\GrEngine.dll
# 	ren $(OUT_DIR)\Graphite.dll $(OUT_DIR)\GrEngine.dll
# 	ren $(OUT_DIR)\Graphite.pdb $(OUT_DIR)\GrEngine.pdb