#	$Id: Makefile 2650 2009-12-15 14:15:18Z beilharz $
#
#	Makefile for FW views Test directory
#
#	Brent McCarthy - 2007-08-07
#
#	Adapted from testViews.mak

BUILD_ROOT = ../../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(VIEWS_NAME)Test
include $(BUILD_ROOT)/Bld/_init.mak.lnx

UNIT = $(FWINCLUDE)/unit++
LIB_UNIT = $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)

VIEWS_LIB	= $(VIEWS_OBJ)/lib
GRENG   = $(SRC)/Graphite/lib
GRFW    = $(SRC)/Graphite/FwOnly

KERNELTEST_SRC = $(KERNEL_SRC)/Test

DEFINES := -DGR_FW -DVIEWSDLL -DSUPPRESS_FW_EXCEPTION_HANDLING \
	-D_VW_GRAPHICS_DEBUG -DVIEWSTEST -DWIN32_KEY_VALUES=1

ifeq ($(BUILD_CONFIG),Debug)
	DEFINES  := $(DEFINES) -D_DEBUG
	OPTIMIZATIONS = -O0
else
	OPTIMIZATIONS = -O3
endif

PACKAGES = glib-2.0 gtk+-2.0 glibmm-2.4 gtkmm-2.4 gdkmm-2.4 cairomm-1.0 pangomm-1.4 freetype2 uuid

INCLUDES := -I$(VIEWS_SRC) -I$(VIEWS_SRC)/lib -I$(VIEWS_SRC)/Test \
		-I$(GENERIC_SRC) -I$(APPCORE_SRC) -I$(CELLAR_SRC) -I$(KERNEL_SRC) \
		-I$(GRENG) -I$(GRFW) -I$(KERNELTEST_SRC)\

INCLUDES := \
	$(shell icu-config --cppflags) \
	$(INCLUDES) \
	-I$(FWINCLUDE) \
	-I$(UNIT) -I$(LIB_UNIT) \
	-I$(COM_OUT_DIR) \
	-I$(WIN32MORE_INC) \
	-I$(COM_INC) \
	-I$(WIN32BASE_INC) \
	$(shell pkg-config --cflags $(PACKAGES)) \


LDLIBS := \
	$(shell icu-config --ldflags) \
	-L$(FWINCLUDE) \
	-L$(WIN32MORE_LIB) -lWin32More \
	-L$(COM_LIB) -lcom \
	-L$(WIN32BASE_LIB) -lWin32Base \
	-L$(OUT_DIR) -lDebugProcs \
	$(shell pkg-config --libs $(PACKAGES)) \
	-lexpat -ldl \

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g $(OPTIMIZATIONS) -fPIC -fvisibility=hidden -Werror -Waddress -Warray-bounds -Wmissing-field-initializers -Wclobbered -Wreturn-type -fstack-protector-all
LDFLAGS = -g -z muldefs

PRECOMPS = $(VIEWS_OBJ)/Main.h.gch

LINK_LIBS := \
	$(LIB_UNIT)/libunit++.a \
	$(OUT_DIR)/libGeneric.a \
	$(OUT_DIR)/libAppCore.a \
	$(COM_OUT_DIR)/libFwKernelTlb.a \
	$(COM_OUT_DIR)/libViewsTlb.a \
	$(OUT_DIR)/libDebugProcs.a

VIEWS_OBJS = \
	$(VIEWS_OBJ)/ViewsGlobals.o \
	$(VIEWS_OBJ)/ExplicitInstantiation.o \
	$(VIEWS_OBJ)/VwEnv.o \
	$(VIEWS_OBJ)/VwLayoutStream.o \
	$(VIEWS_OBJ)/VwLazyBox.o \
	$(VIEWS_OBJ)/VwNotifier.o \
	$(VIEWS_OBJ)/VwOverlay.o \
	$(VIEWS_OBJ)/VwPattern.o \
	$(VIEWS_OBJ)/VwPrintContext.o \
	$(VIEWS_OBJ)/VwPropertyStore.o \
	$(VIEWS_OBJ)/VwRootBox.o \
	$(VIEWS_OBJ)/VwSelection.o \
	$(VIEWS_OBJ)/VwSimpleBoxes.o \
	$(VIEWS_OBJ)/VwSynchronizer.o \
	$(VIEWS_OBJ)/VwTableBox.o \
	$(VIEWS_OBJ)/VwTextBoxes.o \
	$(VIEWS_OBJ)/VwTxtSrc.o \
	$(VIEWS_LIB)/DisplayCapsInfo.o \
	$(VIEWS_LIB)/GrJustifier.o \
	$(VIEWS_LIB)/GrTxtSrc.o \
	$(VIEWS_LIB)/VwBaseDataAccess.o \
	$(VIEWS_LIB)/VwBaseVirtualHandler.o \
	$(VIEWS_LIB)/VwCacheDa.o \
	$(VIEWS_LIB)/VwColor.o \
	$(VIEWS_LIB)/VwJustifier.o \
	$(VIEWS_LIB)/VwUndo.o \
	$(GENERIC_OBJ)/ModuleEntry.o \

DEPS = $(PRECOMPS:%.gch=%.d)

all: $(OUT_DIR)/testViews

$(OUT_DIR)/testViews: $(INT_DIR)/testViews.o $(INT_DIR)/Collection.o $(VIEWS_OBJS) $(LINK_LIBS)
ifeq "$(GCC46)" "1"
	$(LINK.cc) -o $@ -Wl,-whole-archive $(LINK_LIBS) -Wl,-no-whole-archive $(INT_DIR)/testViews.o $(INT_DIR)/Collection.o $(VIEWS_OBJS) $(LDLIBS)
else
	$(LINK.cc) -o $@ $^ $(LDLIBS)
endif


# this now assumes environ has been sourced
check: all
	cd $(OUT_DIR) && ./testViews

clean:
	$(RM) $(OUT_DIR)/testViews $(INT_DIR)/testViews.o $(INT_DIR)/Collection.cpp $(INT_DIR)/*.[od] *.gch

%.h.gch: %.h
	$(COMPILE.cc) -o $@ $<

%.i: %.cpp
	$(COMPILE.cc) -E -o $@ $<

COLLECT=$(BUILD_ROOT)/Bin/CollectUnit++Tests.sh Views

$(INT_DIR)/Collection.cpp: \
	DummyBaseVc.h \
	DummyRootsite.h \
	testViews.h \
	$(KERNELTEST_SRC)/MockLgWritingSystemFactory.h \
	$(KERNELTEST_SRC)/MockLgWritingSystem.h \
	TestNotifier.h \
	TestLayoutPage.h \
	TestVwTxtSrc.h \
	TestVwParagraph.h \
	TestVwPattern.h \
	TestVwEnv.h \
	TestVwOverlay.h \
	TestLazyBox.h \
	TestVwRootBox.h \
	TestVwSelection.h \
	TestInsertDiffPara.h \
	TestVwTextBoxes.h \
	TestVwTableBox.h
	@echo Collecting tests for testViews
	$(COLLECT) $^ $@

-include $(DEPS)
