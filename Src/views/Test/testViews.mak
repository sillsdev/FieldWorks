# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testViews
BUILD_EXTENSION=exe
BUILD_REGSVR=0

DEFS=$(DEFS) /DGR_FW /DVIEWSDLL /DGRAPHITE2_STATIC

UNITPP_INC=$(BUILD_ROOT)\Include\unit++
VIEWS_SRC=$(BUILD_ROOT)\Src\Views
VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
VIEWSTEST_SRC=$(BUILD_ROOT)\Src\Views\Test
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
APPCORE_SRC=$(BUILD_ROOT)\Src\AppCore
FWKERNELTEST_SRC=$(BUILD_ROOT)\Src\Kernel\Test
GR2_INC=$(BUILD_ROOT)\Lib\src\graphite2\include

# Set the USER_INCLUDE environment variable.
UI=$(UNITPP_INC);$(VIEWSTEST_SRC);$(VIEWS_SRC);$(VIEWS_LIB_SRC);$(GENERIC_SRC);$(APPCORE_SRC);$(FWKERNELTEST_SRC);$(GR2_INC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

RCFILE=TestViews.rc
PATH=$(COM_OUT_DIR);$(PATH)

LINK_OPTS=$(LINK_OPTS:/subsystem:windows=/subsystem:console) /LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)"
CPPUNIT_LIBS=unit++.lib
LINK_LIBS=$(CPPUNIT_LIBS) Generic.lib xmlparse.lib Usp10.lib graphite2.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_VIEWSTESTSUITE=\
	$(INT_DIR)\genpch\testViews.obj\
	$(INT_DIR)\genpch\Collection.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwAccessRoot.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwOverlay.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwPropertyStore.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\ExplicitInstantiation.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwSimpleBoxes.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwTextBoxes.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwRootBox.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwEnv.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwNotifier.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwSelection.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwTableBox.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwGraphics.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwTxtSrc.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\AfColorTable.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\AfGfx.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwPrintContext.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwBaseDataAccess.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwCacheDa.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwUndo.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwBaseVirtualHandler.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwLazyBox.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwPattern.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\FwStyledText.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwSynchronizer.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwTextStore.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\VwLayoutStream.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\UniscribeEngine.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\UniscribeSegment.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\RomRenderEngine.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\RomRenderSegment.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\GraphiteEngine.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\GraphiteSegment.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\LgLineBreaker.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\LgUnicodeCollater.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Views\autopch\ViewsGlobals.obj\


OBJ_ALL=$(OBJ_VIEWSTESTSUITE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=testViews

ARG_SRCDIR=$(VIEWSTEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(VIEWS_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\CollectUnit++Tests.cmd Views

$(INT_DIR)\genpch\Collection.obj: $(VIEWSTEST_SRC)\Collection.cpp

$(VIEWSTEST_SRC)\Collection.cpp: $(VIEWSTEST_SRC)\DummyBaseVc.h $(VIEWSTEST_SRC)\DummyRootsite.h\
 $(VIEWSTEST_SRC)\testViews.h\
 $(FWKERNELTEST_SRC)\MockLgWritingSystemFactory.h\
 $(FWKERNELTEST_SRC)\MockLgWritingSystem.h\
 $(VIEWSTEST_SRC)\TestNotifier.h\
 $(VIEWSTEST_SRC)\TestLayoutPage.h\
 $(VIEWSTEST_SRC)\TestLgCollatingEngine.h\
 $(VIEWSTEST_SRC)\TestVirtualHandlers.h\
 $(VIEWSTEST_SRC)\TestVwTxtSrc.h\
 $(VIEWSTEST_SRC)\TestVwParagraph.h\
 $(VIEWSTEST_SRC)\TestVwPattern.h\
 $(VIEWSTEST_SRC)\TestVwSync.h\
 $(VIEWSTEST_SRC)\TestVwEnv.h\
 $(VIEWSTEST_SRC)\TestVwOverlay.h\
 $(VIEWSTEST_SRC)\TestLazyBox.h\
 $(VIEWSTEST_SRC)\TestVwRootBox.h\
 $(VIEWSTEST_SRC)\TestVwSelection.h\
 $(VIEWSTEST_SRC)\TestInsertDiffPara.h\
 $(VIEWSTEST_SRC)\TestVwTextStore.h \
 $(VIEWSTEST_SRC)\TestVwGraphics.h \
 $(VIEWSTEST_SRC)\TestVwTextBoxes.h \
 $(VIEWSTEST_SRC)\TestVwTableBox.h \
 $(VIEWSTEST_SRC)\TestLgLineBreaker.h\
 $(VIEWSTEST_SRC)\TestUniscribeEngine.h\
 $(VIEWSTEST_SRC)\TestRomRenderEngine.h\
 $(VIEWSTEST_SRC)\TestGraphiteEngine.h\
 $(VIEWSTEST_SRC)\RenderEngineTestBase.h\
 $(VIEWSTEST_SRC)\MockRenderEngineFactory.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** $(VIEWSTEST_SRC)\Collection.cpp
