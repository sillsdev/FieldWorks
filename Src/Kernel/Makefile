
#	$Id: Makefile 2650 2009-12-15 14:15:18Z beilharz $
#
#	Makefile for FW Kernel directory
#
#	Neil Mayhew - 2007-03-15
#
#	Adapted from FwKernel.mak

BUILD_ROOT = ../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(KERNEL_NAME)
include $(BUILD_ROOT)/Bld/_init.mak.lnx

DEFINES := $(DEFINES) -DWIN32_KEY_VALUES=1

ifeq ($(BUILD_CONFIG),Debug)
	DEBUG_LIBS = $(OUT_DIR)/libDebugProcs.a
	DEFINES  := $(DEFINES) -D_DEBUG -DDEBUG
	DEBUG_INCLUDES = -I$(DEBUGPROCS_SRC)
	OPTIMIZATIONS = -O0
else
	OPTIMIZATIONS = -O3
endif

INCLUDES := -I$(KERNEL_SRC) -I$(GENERIC_SRC) $(DEBUG_INCLUDES)

INCLUDES := $(shell icu-config --cppflags) \
	$(INCLUDES) \
	-I$(FWINCLUDE) \
	-I$(COM_OUT_DIR) \
	-I$(WIN32MORE_INC) \
	-I$(COM_INC) \
	-I$(WIN32BASE_INC)

LDLIBS := $(LDLIBS) \
	-L$(WIN32MORE_LIB) -lWin32More \
	-L$(COM_LIB) -lcom \
	-L$(WIN32BASE_LIB) -lWin32Base \
	$(shell icu-config --ldflags) \
	-ldl

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g $(OPTIMIZATIONS) -fPIC -fvisibility=hidden -Werror -Waddress -Warray-bounds -Wmissing-field-initializers -Wclobbered -Wreturn-type -fstack-protector-all -Wshadow -rdynamic

ARFLAGS  = -crs

DEFFILE = FwKernel.def

LINK_LIBS := \
	$(LINK_LIBS) \
	$(COM_OUT_DIR)/libFwKernelTlb.a \
	$(OUT_DIR)/libGeneric.a \
	$(DEBUG_LIBS) \

# === Object Lists ===

OBJ_OTHER = \
	$(GENERIC_OBJ)/ModuleEntry.o \

# === Targets ===

all: $(OUT_DIR)/libFwKernel.so $(COM_OUT_DIR)/libFwKernelTlb.a ComponentsMap link_check

$(OUT_DIR)/libFwKernel.so: $(OBJ_OTHER) $(LINK_LIBS) $(WIN32MORE_LIB)/libWin32More.a $(WIN32BASE_LIB)/libWin32Base.a
ifeq "$(GCC46)" "1"
	$(LINK.cc) -shared -o $@ -Wl,-whole-archive $(LINK_LIBS) -Wl,-no-whole-archive $(OBJ_OTHER) $(LDLIBS)
else
	$(LINK.cc) -shared -o $@ $^ $(LDLIBS)
endif

$(COM_OUT_DIR)/libFwKernelTlb.a: $(INT_DIR)/FwKernel_GUIDs.o
	$(AR) $(ARFLAGS) $@ $^

link_check: $(INT_DIR)/libFwKernel

$(INT_DIR)/libFwKernel: $(OBJ_OTHER) $(GENERIC_OBJ)/main.o $(LINK_LIBS)
	$(LINK.cc) -o $@ -Wl,-whole-archive $(LINK_LIBS) -Wl,-no-whole-archive $(GENERIC_OBJ)/main.o  $(OBJ_OTHER) $(LDLIBS)

ComponentsMap: $(OUT_DIR)/libFwKernel.so
	(export PATH=$(PATH):$(COM_DIR)/build$(ARCH)/bin && cd $(OUT_DIR) && generate-map.sh libFwKernel.so > $(OUT_DIR)/libFwKernel.compmap)

# === Rules ===

# ensure object dependies are built.
$(GENERIC_OBJ)/ModuleEntry.o:
	@$(MAKE) -C $(GENERIC_SRC) $@ -q || \
	 $(MAKE) -C $(GENERIC_SRC) $@

$(GENERIC_OBJ)/main.o:
	@$(MAKE) -C $(GENERIC_SRC) -q $@ || \
	 $(MAKE) -C $(GENERIC_SRC) $@

$(OUT_DIR)/libGeneric.a:
	@$(MAKE) -C $(GENERIC_SRC) $@ -q || \
	 $(MAKE) -C $(GENERIC_SRC) $@

-include $(OBJ_ALL:%.o=%.d)

clean:
	$(RM) $(OUT_DIR)/libFwKernel.so $(COM_OUT_DIR)/libFwKernelTlb.a $(INT_DIR)/libFwKernel $(INT_DIR)/*.[od] $(INT_DIR)/*.gch
