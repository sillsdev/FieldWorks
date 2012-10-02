
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
	DEBUG_INCLUDES = -I$(SRC)/DebugProcs
endif

FWKERNEL_SRC = $(SRC)/Kernel
GENERIC_SRC = $(SRC)/Generic
APPCORE_SRC = $(SRC)/AppCore
CELLAR_SRC = $(SRC)/Cellar
DBACCESS_SRC=$(SRC)/DbAccess
LANGUAGE_SRC=$(SRC)/Language
DBSERVICES_SRC = $(SRC)/DbServices

INCLUDES := -I$(FWKERNEL_SRC) -I$(CELLAR_SRC) -I$(GENERIC_SRC) -I$(APPCORE_SRC) $(DEBUG_INCLUDES)
INCLUDES := $(INCLUDES) -I$(LANGUAGE_SRC) -I$(DBACCESS_SRC) -I$(DBSERVICES_SRC)

INCLUDES := $(shell icu-config --cppflags) \
	$(INCLUDES) \
	-I$(FWINCLUDE) \
	-I$(COM_OUT_DIR) \
	-I$(LIBRARIES)/Win32More/include \
	-I$(LIBRARIES)/COM/include \
	-I$(LIBRARIES)/Win32Base/include \


LDLIBS := $(LDLIBS) \
	-L$(LIBRARIES)/Win32More/src/build$(ARCH) -lWin32More \
	-L$(LIBRARIES)/COM/build$(ARCH)/src/.libs -lcom \
	-L$(LIBRARIES)/Win32Base/src/build$(ARCH) -lWin32Base \
	$(shell icu-config --ldflags) \
	-luuid -lexpat

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g -O3 -fPIC -fvisibility=hidden -Werror -Waddress -Warray-bounds -Wmissing-field-initializers -Wclobbered -Wreturn-type -fstack-protector-all -D_FORTIFY_SOURCE=0

ARFLAGS  = -crs

RCFILE = FwKernel.rc
DEFFILE = FwKernel.def

LINK_LIBS := \
	$(LINK_LIBS) \
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
	$(COM_OUT_DIR)/libFwKernelTlb.a \

OBJ_OTHER = \
	$(GENERIC_OBJ)/ModuleEntry.o \
	$(GENERIC_OBJ)/TextProps1.o \
	$(APPCORE_OBJ)/FwStyledText.o \
	$(APPCORE_OBJ)/WriteXml.o \

#	$(CELLAR_OBJ)/FwXml.o \

# === Targets ===

all: $(OUT_DIR)/libFwKernel.so $(COM_OUT_DIR)/libFwKernelTlb.a ComponentsMap link_check

$(OUT_DIR)/libFwKernel.so: $(OBJ_FWKERNEL) $(OBJ_OTHER) $(LINK_LIBS) $(LIBRARIES)/Win32More/src/build$(ARCH)/libWin32More.a $(LIBRARIES)/Win32Base/src/build$(ARCH)/libWin32Base.a
	$(LINK.cc) -shared -o $@ $^ $(LDLIBS)

$(COM_OUT_DIR)/libFwKernelTlb.a: $(INT_DIR)/FwKernel_GUIDs.o $(INT_DIR)/FwKernelExtra_GUIDs.o
	$(AR) $(ARFLAGS) $@ $^

link_check: libFwKernel

libFwKernel: $(OBJ_FWKERNEL) $(OBJ_OTHER) $(GENERIC_OBJ)/main.o $(LINK_LIBS)
	$(LINK.cc) -o $@ $^ $(LDLIBS)

ComponentsMap: $(OUT_DIR)/libFwKernel.so
	(export PATH=$(PATH):$(COM_DIR)/bin$(ARCH) && cd $(OUT_DIR) && generate-map.sh libFwKernel.so > $(OUT_DIR)/libFwKernel.compmap)

# === Rules ===

PCHNAME = $(INT_DIR)/Main.h.gch

$(OBJ_FWKERNEL): $(PCHNAME)

# ensure object dependies are built.
$(GENERIC_OBJ)/ModuleEntry.o $(GENERIC_OBJ)/TextProps1.o:
	@$(MAKE) -C $(SRC)/Generic $@ -q || \
	 $(MAKE) -C $(SRC)/Generic $@

$(APPCORE_OBJ)/FwStyledText.o $(APPCORE_OBJ)/WriteXml.o:
	@$(MAKE) -C $(SRC)/AppCore $@ -q || \
	 $(MAKE) -C $(SRC)/AppCore $@

#$(CELLAR_OBJ)/FwXml.o:
#	@$(MAKE) -C $(SRC)/Cellar $@ -q || \
#	 $(MAKE) -C $(SRC)/Cellar $@

$(GENERIC_OBJ)/main.o:
	@$(MAKE) -C $(SRC)/$(GENERIC_NAME)/ -q $@ || \
	 $(MAKE) -C $(SRC)/$(GENERIC_NAME)/ $@

$(OUT_DIR)/libCellar.a:
	@$(MAKE) -C $(SRC)/Cellar $@ -q || \
	 $(MAKE) -C $(SRC)/Cellar $@

$(OUT_DIR)/libAppCore.a:
	@$(MAKE) -C $(SRC)/AppCore $@ -q || \
	 $(MAKE) -C $(SRC)/AppCore $@

$(OUT_DIR)/libGeneric.a:
	@$(MAKE) -C $(SRC)/Generic $@ -q || \
	 $(MAKE) -C $(SRC)/Generic $@

$(LIBRARIES)/Win32More/src/libWin32More.a:
	@$(MAKE) -C $(BUILD_ROOT) -q libWin32More.a || \
	 $(MAKE) -C $(BUILD_ROOT) libWin32More.a

$(LIBRARIES)/Win32Base/src/libWin32Base.a:
	@$(MAKE) -C $(BUILD_ROOT) -q libWin32Base.a || \
	 $(MAKE) -C $(BUILD_ROOT) libWin32Base.a

%.h.gch: %.h
	$(COMPILE.cc) -o $@ $<

-include $(OBJ_ALL:%.o=%.d)
-include $(PCHNAME:%.gch=%.d)

clean:
	$(RM) $(OUT_DIR)/libFwKernel.so $(COM_OUT_DIR)/libFwKernelTlb.a libFwKernel $(INT_DIR)/*.[od] $(INT_DIR)/*.gch
