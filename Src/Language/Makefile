
#	$Id: Makefile 2650 2009-12-15 14:15:18Z beilharz $
#
#	Makefile for FW Language directory
#
#	Neil Mayhew - 2007-03-15
#
#	Adapted from Language.mak


BUILD_ROOT = ../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(LANGUAGE_NAME)
include $(BUILD_ROOT)/Bld/_init.mak.lnx

DEFINES := $(DEFINES) -DGR_FW -DSUPPRESS_FW_EXCEPTION_HANDLING -DWIN32_KEY_VALUES=1

ifeq ($(BUILD_CONFIG),Debug)
	DEBUG_LIBS = $(OUT_DIR)/libDebugProcs.a
	DEFINES  := $(DEFINES) -D_DEBUG
endif

LANG_SRC = $(SRC)/Language
GENERIC_SRC = $(SRC)/Generic
APPCORE_SRC = $(SRC)/AppCore
TEXT_SRC = $(SRC)/Text
CELLAR_SRC = $(SRC)/Cellar
GRENGINE_SRC = $(SRC)/Graphite/GrEngine
GRUTIL_LIB = $(SRC)/Graphite/lib
TTFUTIL_LIB = $(SRC)/Graphite/TtfUtil
KERNEL_SRC = $(SRC)/Kernel
DBACCESS_SRC = $(SRC)/DbAccess
VIEWS_SRC = $(SRC)/views
VIEWS_LIB = $(SRC)/views/lib
GRFW_SRC = $(SRC)/Graphite/FwOnly
# FWUTILS_SRC = $(SRC)/FWUtils

PACKAGES = glib-2.0 gtk+-2.0 glibmm-2.4 gtkmm-2.4 cairomm-1.0 pangomm-1.4

# Make sure Lang is first, as we want to get the Main.h from there,
# not any of the others (e.g., in Views)
INCLUDES := -I$(LANG_SRC) -I$(GENERIC_SRC) -I$(APPCORE_SRC) -I$(TEXT_SRC)
INCLUDES := $(INCLUDES) -I$(KERNEL_SRC) -I$(CELLAR_SRC) -I$(DBACCESS_SRC)
INCLUDES := $(INCLUDES) -I$(GRUTIL_LIB) -I$(TTFUTIL_LIB) -I$(VIEWS_SRC) -I$(VIEWS_LIB) -I$(GRFW_SRC)
INCLUDES := $(shell icu-config --cppflags) \
	$(INCLUDES) \
	-I$(FWINCLUDE) \
	-I$(COM_OUT_DIR) \
	-I$(LIBRARIES)/Win32More/include \
	-I$(LIBRARIES)/COM/include \
	-I$(LIBRARIES)/Win32Base/include \
	$(shell pkg-config --cflags $(PACKAGES)) \


LDLIBS = \
	-L$(FWINCLUDE) \
	$(DEBUG_LIBS) \
	-L$(LIBRARIES)/Win32More/src/build$(ARCH) -lWin32More \
	-L$(LIBRARIES)/COM/build$(ARCH)/src/.libs -lcom \
	-L$(LIBRARIES)/Win32Base/src/build$(ARCH) -lWin32Base \
	$(shell pkg-config --libs $(PACKAGES)) \
	$(shell icu-config --ldflags) \
	-luuid -lexpat

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g -O3 -fPIC -fvisibility=hidden -Werror -Waddress -Warray-bounds -Wmissing-field-initializers -Wclobbered -Wreturn-type -fstack-protector-all -D_FORTIFY_SOURCE=0

ARFLAGS  = -crs

# XML_INC = $(CELLAR_XML) -I$(LANG_XML)

#PATH = $(COM_OUT_DIR);$(PATH)

RCFILE = Language.rc
DEFFILE = Language.def

LINK_LIBS := \
	$(DEBUG_LIBS) \
	$(OUT_DIR)/libGeneric.a \
	$(COM_OUT_DIR)/libFwKernelTlb.a \
	$(COM_OUT_DIR)/libGraphiteTlb.a \
	$(COM_OUT_DIR)/libViewsTlb.a \

#	$(OUT_DIR)/libAppCore.a \
#	$(OUT_DIR)/libCellar.a \

# === Object Lists ===

OBJ_LANGSERV =

# not currently used
OBJ_LANMODEL = \
	$(INT_DIR)/LgCharOverride.o \
	$(INT_DIR)/LgCharPropSpec.o \
	$(INT_DIR)/LgCharSpec.o \
	$(INT_DIR)/LgCollaterSpec.o \
	$(INT_DIR)/LgComponent.o \
	$(INT_DIR)/LgConverterSpec.o \
	$(INT_DIR)/LgConvertStringSpec.o \
	$(INT_DIR)/LgExternalSpecComponent.o \
	$(INT_DIR)/LgLineBreakSpaceSpec.o \
	$(INT_DIR)/LgLineBreakSpec.o \
	$(INT_DIR)/LgOwnedSpecComponent.o \
	$(INT_DIR)/LgNumericConverterSpec.o \
	$(INT_DIR)/LgRenderSpec.o \
	$(INT_DIR)/LgRomanRendererSpec.o \
	$(INT_DIR)/LgSpec.o \
	$(INT_DIR)/LgSpellCheckSpec.o \
	$(INT_DIR)/LgSysCollaterSpec.o \
	$(INT_DIR)/LgTokenizerSpec.o \
	$(INT_DIR)/LgUnicodeCollaterSpec.o \
	$(INT_DIR)/LgUserClassSpec.o \
	$(INT_DIR)/LgWfiCheckerSpec.o \
	$(INT_DIR)/LgWinRendSpec.o \
	$(INT_DIR)/LgWordBreakSpaceSpec.o \
	$(INT_DIR)/LgCPWordTokenizer.o \
	$(INT_DIR)/LgNumericEngine.o \
	$(INT_DIR)/TsStringPlus.o \
	$(INT_DIR)/FwXml.o \

OBJ_LANG = \
	$(INT_DIR)/LanguageGlobals.o \
	$(INT_DIR)/LgIcuWrappers.o \
	$(INT_DIR)/LgSimpleEngines.o \
	$(INT_DIR)/LgIcuCharPropEngine.o \
	$(INT_DIR)/LgFontManager.o \
	$(INT_DIR)/LgUnicodeCollater.o \
	$(INT_DIR)/LgIcuCollator.o \
	$(INT_DIR)/LgInputMethodEditor.o \
	$(INT_DIR)/LgTextServices.o \
	$(INT_DIR)/LocaleIndex.o \
	$(INT_DIR)/LgCodePageEnumerator.o \
	$(INT_DIR)/LgKeymanHandler.o \
	$(INT_DIR)/FwXml.o \
	$(INT_DIR)/WriteXml.o \
	$(INT_DIR)/FwStyledText.o \
	$(INT_DIR)/UniscribeLinux.o \
	$(INT_DIR)/UniscribeSegment.o \
	$(INT_DIR)/UniscribeEngine.o \
	$(INT_DIR)/RomRenderEngine.o \
	$(INT_DIR)/RomRenderSegment.o \


#	LgCodePageEnumerator.o \  # This is used only in FwCoreDlgs (written in C#) - Need by FwCoreDlgs as loaded as a COM object
#	LgKeymanHandler.o \
#	UniscribeEngine.o \
#	UniscribeSegment.o \

# Not used
XMO_LANGSERV = \
	$(COM_INT_DIR)/LgCharOverride.xmo \
	$(COM_INT_DIR)/LgCharPropSpec.xmo \
	$(COM_INT_DIR)/LgCharSpec.xmo \
	$(COM_INT_DIR)/LgCollaterSpec.xmo \
	$(COM_INT_DIR)/LgComponent.xmo \
	$(COM_INT_DIR)/LgConverterSpec.xmo \
	$(COM_INT_DIR)/LgConvertStringSpec.xmo \
	$(COM_INT_DIR)/LgWritingSystemModel.xmo \
	$(COM_INT_DIR)/LgExternalSpecComponent.xmo \
	$(COM_INT_DIR)/LgLineBreakSpaceSpec.xmo \
	$(COM_INT_DIR)/LgLineBreakSpec.xmo \
	$(COM_INT_DIR)/LgOwnedSpecComponent.xmo \
	$(COM_INT_DIR)/LgNumericConverterSpec.xmo \
	$(COM_INT_DIR)/LgRenderSpec.xmo \
	$(COM_INT_DIR)/LgRomanRendererSpec.xmo \
	$(COM_INT_DIR)/LgSpec.xmo \
	$(COM_INT_DIR)/LgSpellCheckSpec.xmo \
	$(COM_INT_DIR)/LgSysCollaterSpec.xmo \
	$(COM_INT_DIR)/LgTokenizerSpec.xmo \
	$(COM_INT_DIR)/LgUnicodeCollaterSpec.xmo \
	$(COM_INT_DIR)/LgUserClassSpec.xmo \
	$(COM_INT_DIR)/LgWfiCheckerSpec.xmo \
	$(COM_INT_DIR)/LgWinRendSpec.xmo \
	$(COM_INT_DIR)/LgWordBreakSpaceSpec.xmo \

OBJ_OTHER = \
	$(INT_DIR)/TextProps1.o \

#OBJ_GRUTIL = \
#	$(GRUTIL_LIB)/GrUtil.o \
#	$(TTFUTIL_LIB)/TtfUtil.o \

# XMO_ALL = $(XMO_LANGSERV)
#
# IDL_MAIN = $(COM_OUT_DIR)/LanguageTlb.idl
#
# PS_MAIN = LanguagePs
#
# OBJECTS_IDH = $(COM_INT_DIR)/Objects.idh
#
# ? OBJECTS_H = $(COM_INT_DIR)/Objects.h

OBJ_ALL = $(OBJ_LANG) $(OBJ_LANGSERV) $(OBJ_OTHER) $(OBJ_GRUTIL)

# === Targets ===

all: $(OUT_DIR)/libLanguage.so $(COM_OUT_DIR)/libLanguageTlb.a ComponentsMap link_check

$(OUT_DIR)/libLanguage.so: $(OBJ_ALL) $(LINK_LIBS)
	$(LINK.cc) -shared -o $@ $^ $(LDLIBS)


$(COM_OUT_DIR)/libLanguageTlb.a: $(INT_DIR)/Language_GUIDs.o
	$(AR) $(ARFLAGS) $@ $^

link_check: libLanguage

libLanguage: $(OBJ_ALL) $(GENERIC_OBJ)/main.o $(LINK_LIBS)
	$(LINK.cc) -o $@ $^ $(LDLIBS)

ComponentsMap: $(OUT_DIR)/libLanguage.so
	(export PATH=$(PATH):$(COM_DIR)/bin$(ARCH) && cd $(OUT_DIR) && generate-map.sh libLanguage.so > libLanguage.compmap)

# === Rules ===

PCHNAME = $(INT_DIR)/Main.h.gch

$(OBJ_LANG) $(OBJ_LANGSERV): $(PCHNAME)

# ensure object dependies are built.

$(INT_DIR)/%.o: $(APPCORE_SRC)/%.cpp
	$(CXX) $(CXXFLAGS) $(CPPFLAGS) $(INCLUDES) $(TARGET_ARCH) \
	  -c $< -o $@ -MMD -MF $(@:%.o=%.d)

$(INT_DIR)/%.o: $(CELLAR_SRC)/%.cpp
	$(CXX) $(CXXFLAGS) $(CPPFLAGS) $(INCLUDES) $(TARGET_ARCH) \
	  -c $< -o $@ -MMD -MF $(@:%.o=%.d)

$(INT_DIR)/%.o: $(GENERIC_SRC)/%.cpp
	$(CXX) $(CXXFLAGS) $(CPPFLAGS) $(INCLUDES) $(TARGET_ARCH) \
	  -c $< -o $@ -MMD -MF $(@:%.o=%.d)

$(OUT_DIR)/libDebugProcs.a:
	@$(MAKE) -C $(SRC)/DebugProcs $@ -q || \
	 $(MAKE) -C $(SRC)/DebugProcs $@

$(OUT_DIR)/libGeneric.a:
	@$(MAKE) -C $(SRC)/Generic $@ -q || \
	 $(MAKE) -C $(SRC)/Generic $@

$(COM_OUT_DIR)/libCellar.a:
	@$(MAKE) -C $(SRC)/Cellar all -q || \
	 $(MAKE) -C $(SRC)/Cellar all

$(COM_OUT_DIR)/libAppCore.a:
	@$(MAKE) -C $(SRC)/AppCore all -q || \
	 $(MAKE) -C (SRC)/AppCore all

$(COM_OUT_DIR)/libFwKernelTlb.a:
	@$(MAKE) -C $(SRC)/Kernel $@ -q || \
	 $(MAKE) -C $(SRC)/Kernel $@

$(COM_OUT_DIR)/libGraphiteTlb.a:
	@$(MAKE) -C $(SRC)/Graphite/GrEngine all -q || \
	 $(MAKE) -C $(SRC)/Graphite/GrEngine all

$(COM_OUT_DIR)/libViewsTlb.a:
	@$(MAKE) -C $(SRC)/views/ $@ -q || \
	 $(MAKE) -C $(SRC)/views/ $@

%.h.gch: %.h
	$(COMPILE.cc) -o $@ $<

-include $(OBJ_ALL:%.o=%.d)
-include $(PCHNAME:%.gch=%.d)

clean:
	$(RM) $(OUT_DIR)/libLanguage.so $(COM_OUT_DIR)/libLanguageTlb.a libLanguage $(INT_DIR)/*.[od] $(INT_DIR)/*.gch
