# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testFwKernel
BUILD_EXTENSION=exe
BUILD_REGSVR=0

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

UNITPP_INC=$(BUILD_ROOT)\Include\unit++
FWKERNEL_SRC=$(BUILD_ROOT)\Src\Kernel
FWKERNELTEST_SRC=$(BUILD_ROOT)\Src\Kernel\Test
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
APPCORE_SRC=$(BUILD_ROOT)\Src\AppCore
DEBUGPROCS_SRC=$(BUILD_ROOT)\src\DebugProcs
LANGUAGETEST_SRC=$(BUILD_ROOT)\Src\Language\Test

# Set the USER_INCLUDE environment variable.
UI=$(UNITPP_INC);$(FWKERNELTEST_SRC);$(FWKERNEL_SRC);$(GENERIC_SRC);$(APPCORE_SRC);$(DEBUGPROCS_SRC);$(LANGUAGETEST_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

LINK_OPTS=$(LINK_OPTS:/subsystem:windows=/subsystem:console) /LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)"
CPPUNIT_LIBS=unit++.lib
LINK_LIBS=$(CPPUNIT_LIBS) Generic.lib xmlparse.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_KERNELTESTSUITE=\
	$(INT_DIR)\genpch\testFwKernel.obj\
	$(INT_DIR)\genpch\Collection.obj\
	$(INT_DIR)\ModuleEntry.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\autopch\KernelGlobals.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\genpch\TsString.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\autopch\TsTextProps.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\autopch\TsStrFactory.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\autopch\TsPropsFactory.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\autopch\TextServ.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\autopch\TsMultiStr.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\usepch\TextProps1.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\autopch\ActionHandler.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\autopch\FwStyledText.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwKernel\autopch\WriteXml.obj\

OBJ_ALL=$(OBJ_KERNELTESTSUITE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=testFwKernel

ARG_SRCDIR=$(FWKERNELTEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===


# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\CollectUnit++Tests.cmd Kernel

$(INT_DIR)\genpch\Collection.obj: $(FWKERNELTEST_SRC)\Collection.cpp

$(FWKERNELTEST_SRC)\Collection.cpp: $(FWKERNELTEST_SRC)\testFwKernel.h\
 $(LANGUAGETEST_SRC)\MockLgWritingSystemFactory.h\
 $(LANGUAGETEST_SRC)\MockLgWritingSystem.h\
 $(FWKERNELTEST_SRC)\TestUndoStack.h\
 $(FWKERNELTEST_SRC)\TestTsStrBldr.h\
 $(FWKERNELTEST_SRC)\TestTsString.h\
 $(FWKERNELTEST_SRC)\TestTsPropsBldr.h\
 $(FWKERNELTEST_SRC)\TestTsTextProps.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** $(FWKERNELTEST_SRC)\Collection.cpp
