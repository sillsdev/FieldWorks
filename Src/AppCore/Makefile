
#	$Id: Makefile 2650 2009-12-15 14:15:18Z beilharz $
#
#	Makefile for FW AppCore directory
#
#	Neil Mayhew - 2007-03-23
#
#	Adapted from Language.mak

BUILD_ROOT = ../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(APPCORE_NAME)
include $(BUILD_ROOT)/Bld/_init.mak.lnx

AFLIB_SRC = $(APPCORE_SRC)/AfLib
GRAPHITE_SRC = $(SRC)/Graphite
GRFW_SRC = $(GRAPHITE_SRC)/FwOnly
GRUTIL_LIB = $(GRAPHITE_SRC)/lib
TTFUTIL_LIB = $(GRAPHITE_SRC)/TtfUtil
VIEWS_LIB = $(VIEWS_SRC)/lib

PACKAGES = glib-2.0 gtk+-2.0 glibmm-2.4 gtkmm-2.4 cairomm-1.0

ifeq ($(BUILD_CONFIG),Debug)
	DEBUG_LIBS = $(OUT_DIR)/libDebugProcs.a
	DEFINES  := $(DEFINES) -D_DEBUG
endif

# Make sure AfLib is first, as we want to get the Main.h from there,
# not any of the others (e.g., in Views)
INCLUDES := -I$(AFLIB_SRC) -I$(GENERIC_SRC) -I$(APPCORE_SRC) -I$(TEXT_SRC) -I$(DEBUGPROCS_SRC)

INCLUDES := $(INCLUDES) -I$(KERNEL_SRC)
INCLUDES := $(INCLUDES) -I$(GRUTIL_LIB) -I$(TTFUTIL_LIB) -I$(VIEWS_SRC) -I$(VIEWS_LIB) -I$(GRFW_SRC)

INCLUDES := $(INCLUDES) \
	-I$(FWINCLUDE) \
	-I$(COM_OUT_DIR) \
	-I$(WIN32MORE_INC) \
	-I$(COM_INC) \
	-I$(WIN32BASE_INC) \
	$(shell pkg-config --cflags $(PACKAGES)) \
	$(shell icu-config --cppflags) \

LDLIBS = \
	-L$(WIN32MORE_LIB) -lWin32More \
	-L$(COM_LIB) -lcom \
	-L$(WIN32BASE_LIB) -lWin32Base \
	$(shell pkg-config --libs $(PACKAGES)) \
	$(shell icu-config --ldflags) \

ifeq ($(BUILD_CONFIG),Debug)
	OPTIMIZATIONS = -O0
else
	OPTIMIZATIONS = -O3
endif

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g $(OPTIMIZATIONS) -fPIC -fvisibility=hidden -Werror -Wshadow

ARFLAGS = -crs

RCFILE =
DEFFILE =

LINK_LIBS = \
	$(OUT_DIR)/libGeneric.a \
	$(DEBUG_LIBS) \

# === Object Lists ===

OBJ_APPCORE = \
	$(INT_DIR)/FwStyledText.o \
	$(INT_DIR)/WriteXml.o \

OBJ_ALL = $(OBJ_APPCORE)

# === Targets ===

all: $(OUT_DIR)/libAppCore.a link_check

$(OUT_DIR)/libAppCore.a: $(OBJ_ALL)
	$(AR) $(ARFLAGS) $@ $^

link_check: $(INT_DIR)/libAppCore

$(INT_DIR)/libAppCore: $(OBJ_ALL) $(LINK_LIBS) $(GENERIC_OBJ)/main.o
	$(LINK.cc) -o $@ -Wl,-whole-archive $(LINK_LIBS) -Wl,-no-whole-archive $(GENERIC_OBJ)/main.o 	$(GENERIC_OBJ)/ModuleEntry.o $(LDLIBS)

# === Rules ===

-include $(OBJ_ALL:%.o=%.d)

# this target is only for lib_check (test?) targets
$(GENERIC_OBJ)/main.o:
	@$(MAKE) -C $(GENERIC_SRC)/ -q $@ || \
	 $(MAKE) -C $(GENERIC_SRC)/ $@

clean:
	$(RM) $(OUT_DIR)/libAppCore.a $(INT_DIR)/libAppCore $(INT_DIR)/*.[od] $(INT_DIR)/*.gch

%.h.gch: %.h
	$(COMPILE.cc) -o $@ $<
