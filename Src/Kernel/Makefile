
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

DEFINES := $(DEFINES) -DGR_FW -DWIN32_KEY_VALUES=1

ifeq ($(BUILD_CONFIG),Debug)
	DEBUG_LIBS = $(OUT_DIR)/libDebugProcs.a
	DEFINES  := $(DEFINES) -D_DEBUG -DDEBUG
	DEBUG_INCLUDES = -I$(DEBUGPROCS_SRC)
	OPTIMIZATIONS = -O0
else
	OPTIMIZATIONS = -O3
endif

CELLAR_SRC = $(SRC)/Cellar
DBSERVICES_SRC = $(SRC)/DbServices

INCLUDES := -I$(KERNEL_SRC) -I$(CELLAR_SRC) -I$(GENERIC_SRC) -I$(APPCORE_SRC) $(DEBUG_INCLUDES)
INCLUDES := $(INCLUDES) -I$(LANGUAGE_SRC) -I$(DBACCESS_SRC) -I$(DBSERVICES_SRC)

INCLUDES := $(shell icu-config --cppflags) \
	$(INCLUDES) \
	-I$(FWINCLUDE) \
	-I$(COM_OUT_DIR) \
	-I$(WIN32MORE_INC) \
	-I$(COM_INC) \
	-I$(WIN32BASE_INC) \


LDLIBS := $(LDLIBS) \
	-L$(WIN32MORE_LIB) -lWin32More \
	-L$(COM_LIB) -lcom \
	-L$(WIN32BASE_LIB) -lWin32Base \
	$(shell icu-config --ldflags) \
	-luuid -lexpat

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g $(OPTIMIZATIONS) -fPIC -fvisibility=hidden -Werror -Waddress -Warray-bounds -Wmissing-field-initializers -Wclobbered -Wreturn-type -fstack-protector-all -Wshadow -rdynamic

ARFLAGS  = -crs

RCFILE = FwKernel.rc
DEFFILE = FwKernel.def

LINK_LIBS := \
	$(LINK_LIBS) \
	$(COM_OUT_DIR)/libFwKernelTlb.a \
	$(OUT_DIR)/libGeneric.a \
	$(DEBUG_LIBS) \

# === Object Lists ===

OBJ_FWKERNEL = \
	$(INT_DIR)/KernelGlobals.o\
	$(INT_DIR)/TsString.o \
	$(INT_DIR)/TsTextProps.o \
	$(INT_DIR)/TsStrFactory.o \
	$(INT_DIR)/TsPropsFactory.o \
	$(INT_DIR)/TsMultiStr.o \
	$(INT_DIR)/TextServ.o \
	$(INT_DIR)/ActionHandler.o \
	$(INT_DIR)/DebugReport.o \

OBJ_OTHER = \
	$(GENERIC_OBJ)/ModuleEntry.o \
	$(GENERIC_OBJ)/TextProps1.o \
	$(APPCORE_OBJ)/FwStyledText.o \
	$(APPCORE_OBJ)/WriteXml.o \

# === Targets ===

all: $(OUT_DIR)/libFwKernel.so $(COM_OUT_DIR)/libFwKernelTlb.a ComponentsMap link_check

$(OUT_DIR)/libFwKernel.so: $(OBJ_FWKERNEL) $(OBJ_OTHER) $(LINK_LIBS) $(WIN32MORE_LIB)/libWin32More.a $(WIN32BASE_LIB)/libWin32Base.a
ifeq "$(GCC46)" "1"
	$(LINK.cc) -shared -o $@ -Wl,-whole-archive $(LINK_LIBS) -Wl,-no-whole-archive $(OBJ_FWKERNEL) $(OBJ_OTHER) $(LDLIBS)
else
	$(LINK.cc) -shared -o $@ $^ $(LDLIBS)
endif

$(COM_OUT_DIR)/libFwKernelTlb.a: $(INT_DIR)/FwKernel_GUIDs.o $(INT_DIR)/FwKernelExtra_GUIDs.o
	$(AR) $(ARFLAGS) $@ $^

link_check: $(INT_DIR)/libFwKernel

$(INT_DIR)/libFwKernel: $(OBJ_OTHER) $(OBJ_FWKERNEL) $(GENERIC_OBJ)/main.o $(LINK_LIBS)
	$(LINK.cc) -o $@ -Wl,-whole-archive $(LINK_LIBS) -Wl,-no-whole-archive $(GENERIC_OBJ)/main.o  $(OBJ_OTHER) $(OBJ_FWKERNEL) $(LDLIBS)

ComponentsMap: $(OUT_DIR)/libFwKernel.so
	(export PATH=$(PATH):$(COM_DIR)/bin$(ARCH) && cd $(OUT_DIR) && generate-map.sh libFwKernel.so > $(OUT_DIR)/libFwKernel.compmap)

# === Rules ===

PCHNAME = $(INT_DIR)/Main.h.gch

$(OBJ_FWKERNEL): $(PCHNAME)

# ensure object dependies are built.
$(GENERIC_OBJ)/ModuleEntry.o $(GENERIC_OBJ)/TextProps1.o:
	@$(MAKE) -C $(GENERIC_SRC) $@ -q || \
	 $(MAKE) -C $(GENERIC_SRC) $@

$(APPCORE_OBJ)/FwStyledText.o $(APPCORE_OBJ)/WriteXml.o:
	@$(MAKE) -C $(APPCORE_SRC) $@ -q || \
	 $(MAKE) -C $(APPCORE_SRC) $@

$(GENERIC_OBJ)/main.o:
	@$(MAKE) -C $(GENERIC_SRC) -q $@ || \
	 $(MAKE) -C $(GENERIC_SRC) $@

$(OUT_DIR)/libCellar.a:
	@$(MAKE) -C $(CELLAR_SRC) $@ -q || \
	 $(MAKE) -C $(CELLAR_SRC) $@

$(OUT_DIR)/libAppCore.a:
	@$(MAKE) -C $(APPCORE_SRC) $@ -q || \
	 $(MAKE) -C $(APPCORE_SRC) $@

$(OUT_DIR)/libGeneric.a:
	@$(MAKE) -C $(GENERIC_SRC) $@ -q || \
	 $(MAKE) -C $(GENERIC_SRC) $@

%.h.gch: %.h
	$(COMPILE.cc) -o $@ $<

-include $(OBJ_ALL:%.o=%.d)
-include $(PCHNAME:%.gch=%.d)

clean:
	$(RM) $(OUT_DIR)/libFwKernel.so $(COM_OUT_DIR)/libFwKernelTlb.a $(INT_DIR)/libFwKernel $(INT_DIR)/*.[od] $(INT_DIR)/*.gch
