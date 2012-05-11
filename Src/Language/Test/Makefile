
#	$Id: Makefile 1119 2008-04-10 23:23:04Z mccarthy $
#
#	Makefile for FW Kernel directory
#
#	Brent McCarthy - 2007-06-22
#
#	Adapted from testLanguage.mak

BUILD_ROOT = ../../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(LANGUAGE_NAME)Test
include $(BUILD_ROOT)/Bld/_init.mak.lnx

UNIT = $(BUILD_ROOT)/Include/unit++
LIB_UNIT = $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)

GRAPHITE = $(SRC)/Graphite
GR_LIB_SRC = $(GRAPHITE)/lib
GR_LIB_OBJ = $(OBJ_DIR)/$(BUILD_CONFIG)/Graphite/lib
GRAPHITE_FW = $(GRAPHITE)/FwOnly
VIEWS_LIB = $(VIEWS_SRC)/lib

DEFINES := -DGR_FW -DVIEWSDLL -DSUPPRESS_FW_EXCEPTION_HANDLING -D_DEBUG
PACKAGES = gdk-2.0 gtkmm-2.4 cairomm-1.0 pangomm-1.4

# Make sure Lang is first, as we want to get the Main.h from there,
# not any of the others (e.g., in Views)
INCLUDES := -I$(LANGUAGE_SRC) -I$(LANGUAGE_SRC)/Test -I$(KERNEL_SRC) -I$(CELLAR_SRC) \
	-I$(GENERIC_SRC) -I$(APPCORE_SRC)

INCLUDES := $(INCLUDES) -I$(GRENGINE_SRC) -I$(GR_LIB_SRC) -I$(GRAPHITE_FW) -I$(VIEWS_SRC) -I$(VIEWS_LIB)
INCLUDES := \
	$(shell icu-config --cppflags) \
	$(INCLUDES) \
	-I$(FWINCLUDE) \
	-I$(UNIT) -I$(LIB_UNIT) \
	-I$(COM_OUT_DIR) \
	-I$(LIBRARIES)/Win32More/include \
	-I$(LIBRARIES)/COM/include \
	-I$(LIBRARIES)/Win32Base/include \
	$(shell pkg-config --cflags $(PACKAGES)) \

#`	-L$(FWKERNEL_SRC) -lFwKernel

LDLIBS = \
	-L$(LIBRARIES)/Win32More/src/build$(ARCH) -lWin32More \
	-L$(LIBRARIES)/COM/build$(ARCH)/src/.libs -lcom \
	-L$(LIBRARIES)/Win32Base/src/build$(ARCH) -lWin32Base \
	-L$(OUT_DIR) -lDebugProcs \
	-L$(LIB_UNIT) -lunit++ \
	$(shell pkg-config --libs $(PACKAGES)) \
	$(shell icu-config --ldflags) \
	-luuid -lexpat


CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g -O0 -fvisibility=hidden -Werror

RCFILE = $(LANGUAGE_SRC)/FwLanguage.rc
DEFFILE = $(LANGUAGE_SRC)/FwLanguage.def
LINK_LIBS := \
	$(OUT_DIR)/libGeneric.a \
	$(OUT_DIR)/libAppCore.a \
	$(OUT_DIR)/libDebugProcs.a \
	$(COM_OUT_DIR)/libGraphiteTlb.a \
	$(COM_OUT_DIR)/libViewsTlb.a \
	$(COM_OUT_DIR)/libLanguageTlb.a

all: $(OUT_DIR)/testLanguage

FWLANG_OBJ := \
	$(LANGUAGE_OBJ)/LgIcuCollator.o \
	$(LANGUAGE_OBJ)/LgUnicodeCollater.o \
	$(LANGUAGE_OBJ)/RomRenderEngine.o \
	$(LANGUAGE_OBJ)/RomRenderSegment.o \
	$(LANGUAGE_OBJ)/LgSimpleEngines.o \
	$(LANGUAGE_OBJ)/LgIcuCharPropEngine.o \
	$(LANGUAGE_OBJ)/LgFontManager.o \
	$(LANGUAGE_OBJ)/LgInputMethodEditor.o \
	$(LANGUAGE_OBJ)/LocaleIndex.o \
	$(LANGUAGE_OBJ)/LanguageGlobals.o \
	$(GENERIC_OBJ)/TextProps1.o \
	$(GR_LIB_OBJ)/GrUtil.o \
	$(LANGUAGE_OBJ)/FwXml.o \
	$(GENERIC_OBJ)/ModuleEntry.o \

#Run make in the directory of each library
$(LINK_LIBS) $(FWLANG_OBJ)::
	@$(MAKE) -C $(@D) $(@F) -q || \
	 $(MAKE) -C $(@D) $(@F)

$(OUT_DIR)/testLanguage: $(INT_DIR)/testLanguage.o $(INT_DIR)/Collection.o $(FWLANG_OBJ) $(LINK_LIBS)
ifeq "$(GCC46)" "1"
	$(LINK.cc) -o $@ -Wl,-whole-archive $(LINK_LIBS) -Wl,-no-whole-archive $(INT_DIR)/testLanguage.o $(INT_DIR)/Collection.o $(FWLANG_OBJ) $(LDLIBS)
else
	$(LINK.cc) -o $@ $^ $(LDLIBS)
endif

clean:
	$(RM) $(OUT_DIR)/testLanguage $(INT_DIR)/*.[od] $(INT_DIR)/Collection.cpp *.[od] *.gch

%.i: %.cpp
	$(COMPILE.cc) -E -o $@ $<

# environ needs to be source before running this.
check: all
	cd $(OUT_DIR) && ./testLanguage


COLLECT=$(BUILD_ROOT)/Bin/CollectUnit++Tests.sh Language

$(INT_DIR)/Collection.cpp: testLanguage.h \
	MockLgWritingSystemFactory.h \
	MockLgWritingSystem.h \
	TestLgCollatingEngine.h \
	TestLgIcuCharPropEngine.h \
	TestLgFontManager.h \
	TestRomRenderEngine.h \
	RenderEngineTestBase.h
	@echo Collecting tests for testLanguage
	$(COLLECT) $^ >$@

#	TestLgWritingSystemFactoryBuilder.h \
#	RenderEngineTestBase.h \
#	TestUniscribeEngine.h\
