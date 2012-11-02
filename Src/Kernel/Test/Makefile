
#	$Id: Makefile 1116 2008-04-10 22:51:30Z mccarthy $
#
#	Makefile for FW Kernel Test directory
#
#	Brent McCarthy - 2007-03-28
#
#	Adapted from TestFwKernel.mak

BUILD_ROOT = ../../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(KERNEL_NAME)Test
include $(BUILD_ROOT)/Bld/_init.mak.lnx

UNIT = $(FWINCLUDE)/unit++
LIB_UNIT = $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)

DEFINES := -DGR_FW -DVIEWSDLL -DSUPPRESS_FW_EXCEPTION_HANDLING -D_DEBUG

PACKAGES = gdk-2.0 gtkmm-2.4 gdkmm-2.4 cairomm-1.0 pangomm-1.4

LANGUAGETEST_SRC = $(LANGUAGE_SRC)/Test

INCLUDES := -I$(KERNEL_SRC) -I$(KERNEL_SRC)/Test -I$(CELLAR_SRC) \
	-I$(GENERIC_SRC) -I$(APPCORE_SRC) -I$(DEBUGPROCS_SRC) \

INCLUDES := \
	$(shell icu-config --cppflags) \
	$(INCLUDES) \
	-I$(FWINCLUDE) \
	-I$(UNIT) -I$(LIB_UNIT) \
	-I$(COM_OUT_DIR) \
	-I$(WIN32MORE_INC) \
	-I$(COM_INC) \
	-I$(WIN32BASE_INC) \
	-I$(LANGUAGETEST_SRC) \
	$(shell pkg-config --cflags $(PACKAGES)) \

LDLIBS := \
	$(shell icu-config --ldflags) \
	-L$(FWINCLUDE) \
	-L$(WIN32MORE_LIB) -lWin32More \
	-L$(COM_LIB) -lcom \
	-L$(WIN32BASE_LIB) -lWin32Base \
	-L$(OUT_DIR) -lDebugProcs \
	$(shell pkg-config --libs $(PACKAGES)) \
	-luuid -lexpat \

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g -O0 -fvisibility=hidden -Werror
LDFLAGS = -g -z muldefs

RCFILE = $(KERNEL_SRC)/FwKernel.rc
DEFFILE = $(KERNEL_SRC)/FwKernel.def
LINK_LIBS := \
	$(LIB_UNIT)/libunit++.a \
	$(OUT_DIR)/libGeneric.a \
	$(OUT_DIR)/libDebugProcs.a \
	$(OUT_DIR)/libAppCore.a \
	$(COM_OUT_DIR)/libFwKernelTlb.a

all: $(OUT_DIR)/testFwKernel

FWKERNEL_OBJ := \
	$(KERNEL_OBJ)/KernelGlobals.o \
	$(KERNEL_OBJ)/TsString.o \
	$(KERNEL_OBJ)/TsTextProps.o \
	$(KERNEL_OBJ)/TsStrFactory.o \
	$(KERNEL_OBJ)/TsPropsFactory.o \
	$(KERNEL_OBJ)/TextServ.o \
	$(KERNEL_OBJ)/TsMultiStr.o \
	$(KERNEL_OBJ)/ActionHandler.o \
	$(GENERIC_OBJ)/ModuleEntry.o \
	$(GENERIC_OBJ)/TextProps1.o \

#Run make in the directory of each library
$(LINK_LIBS) $(FWKERNEL_OBJ)::
	@$(MAKE) -C $(@D) $(@F) -q || \
	 $(MAKE) -C $(@D) $(@F)



$(OUT_DIR)/testFwKernel: $(FWKERNEL_OBJ) $(INT_DIR)/testFwKernel.o $(INT_DIR)/Collection.o $(LINK_LIBS)
ifeq "$(GCC46)" "1"
	$(LINK.cc) -o $@ -Wl,-whole-archive $(LINK_LIBS) -Wl,-no-whole-archive $(FWKERNEL_OBJ) $(INT_DIR)/testFwKernel.o $(INT_DIR)/Collection.o $(LDLIBS)
else
	$(LINK.cc) -o $@ $^ $(LDLIBS)
endif

clean:
	$(RM) $(OUT_DIR)/testFwKernel $(INT_DIR)/testFwKernel.o $(INT_DIR)/Collection.cpp $(INT_DIR)/*.[od] *.gch

%.i: %.cpp
	$(COMPILE.cc) -E -o $@ $<

# environ needs to be sourced before running this
check: all
	cd $(OUT_DIR) && ./testFwKernel

COLLECT=$(BUILD_ROOT)/Bin/CollectUnit++Tests.sh Kernel


$(INT_DIR)/Collection.cpp: testFwKernel.h \
	$(LANGUAGETEST_SRC)/MockLgWritingSystemFactory.h\
	$(LANGUAGETEST_SRC)/MockLgWritingSystem.h\
	TestUndoStack.h \
	TestTsStrBldr.h \
	TestTsString.h \
	TestTsPropsBldr.h \
	TestTsTextProps.h
	@echo Collecting tests for testFwKernel
	$(COLLECT) $^ >$@
