
#	$Id: Makefile 2072 2008-12-03 22:25:08Z hindlet $
#
#	Makefile for DebugProcs library
#
#	Eberhard Beilharz, 11 Dec 2008

BUILD_ROOT = ../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(DEBUGPROCS_NAME)
include $(BUILD_ROOT)/Bld/_init.mak.lnx

OUTPUT  = $(FWOUTPUT)
INCLUDE = $(FWINCLUDE)

TLB_SRC      = $(OUTPUT)/Common

DEFINES  := $(DEFINES) -DGR_FW -DSUPPRESS_FW_EXCEPTION_HANDLING -D_DEBUG

ifeq ($(BUILD_CONFIG),Debug)
	OPTIMIZATIONS = -O0
else
	OPTIMIZATIONS = -O3
endif

PACKAGES := gtkmm-2.4

INCLUDES := $(shell icu-config --cppflags) \
	 $(INCLUDES) \
	-I$(GENERIC_SRC) -I$(TLB_SRC) -I$(INCLUDE) \
	-I$(WIN32MORE_INC) \
	-I$(COM_INC) \
	-I$(WIN32BASE_INC) \
	-I$(ICU_INC_DIR) \
	$(shell pkg-config --cflags $(PACKAGES)) \


LDLIBS = \
	-L$(FWINCLUDE) \
	-L$(WIN32MORE_LIB) -lWin32More \
	-L$(COM_LIB) -lcom \
	-L$(WIN32BASE_LIB) -lWin32Base \
	$(shell pkg-config --libs $(PACKAGES)) \
	$(shell icu-config --ldflags) \
	-luuid -lexpat

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g $(OPTIMIZATIONS) -fPIC -fvisibility=hidden -Werror -Wshadow

ARFLAGS  = -crs

# === Object Lists ===

OBJS = \
	$(INT_DIR)/DebugProcs.o \

# === Targets ===

all: $(OUT_DIR)/libDebugProcs.a link_check

$(OUT_DIR)/libDebugProcs.a: $(OBJS)
	$(AR) $(ARFLAGS) $@ $^

link_check: $(INT_DIR)/libDebugProcs

$(INT_DIR)/libDebugProcs: $(OBJS) $(GENERIC_OBJ)/main.o
	$(LINK.cc) -o $@ $^ $(LDLIBS)

# === Rules ===

$(GENERIC_OBJ)/main.o:
	@$(MAKE) -C $(GENERIC_SRC)/ -q $@ || \
	 $(MAKE) -C $(GENERIC_SRC)/ $@

-include $(OBJS:%.o=%.d)
-include $(PCHNAME:%.gch=%.d)

clean:
	$(RM) $(OUT_DIR)/libDebugProcs.a $(INT_DIR)/libDebugProcs $(INT_DIR)/*.[od] $(INT_DIR)/*.gch
