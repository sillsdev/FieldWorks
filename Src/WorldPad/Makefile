#
#	Makefile for WorldPad directory
#
#	MarkS - 2008-06-30
#

BUILD_ROOT = ../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(WORLDPAD_NAME)
include $(BUILD_ROOT)/Bld/_init.mak.lnx

.DELETE_ON_ERROR:

# Define VIEWSDLL to bypass AppCore related things in views code
DEFINES := $(DEFINES)  -DDEBUG  -DGR_FW -DVIEWSDLL -DSUPPRESS_FW_EXCEPTION_HANDLING

DBACCESS_SRC=$(SRC)/DbAccess
DBSERVICES_SRC = $(SRC)/DbServices
DEBUGPROCS_SRC = $(SRC)/DebugProcs
WORLDPAD_SRC = $(SRC)/WorldPad
VIEWS_LIB_SRC = $(VIEWS_SRC)/lib
GRENG_LIB_SRC  = $(SRC)/Graphite/lib
GRFW_SRC       = $(SRC)/Graphite/FwOnly
TLB_SRC        = $(COM_OUT_DIR)

#GRAPHITE_OPENSOURCE_SRC = $(SRC)/Graphite/OpenSource
#GRAPHITE_LIB_SRC = $(SRC)/Graphite/lib

ICU_DIR := $(shell $(ICU_BIN_DIR)/icu-config --prefix)/lib

export LD_LIBRARY_PATH := $(ICU_DIR):$(COM_LIB):$(OUT_DIR):$(LD_LIBRARY_PATH)
export PATH := $(COM_DIR)/bin$(ARCH):$(PATH)

PACKAGES = gdk-2.0 gtkmm-2.4 cairomm-1.0 pangomm-1.4 ImageMagick++ freetype2 uuid glibmm-2.4

INCLUDES := \
	-I$(VIEWS_SRC) \
	-I$(VIEWS_LIB_SRC) \
	-I$(APPCORE_SRC) \
	-I$(GENERIC_SRC) \
	-I$(TLB_SRC) \
	-I$(GRFW_SRC) \
	-I$(GRENG_LIB_SRC) \
	-I$(FWINCLUDE) \
	-I$(BUILD_ROOT)/Src \

#	-I$(COMMONDLGS_SRC) \
#	-I$(WORLDPAD_SRC) \
#	-I$(COM_OUT_DIR) \

INCLUDES += \
	-I$(LIBRARIES)/Win32More/include \
	-I$(LIBRARIES)/COM/include \
	-I$(LIBRARIES)/Win32Base/include \
	$(shell $(ICU_BIN_DIR)/icu-config --cppflags) \
	$(shell pkg-config --cflags $(PACKAGES)) \

LDLIBS += \
	$(shell $(ICU_BIN_DIR)/icu-config --ldflags) \
	$(shell pkg-config --libs $(PACKAGES)) \
	-luuid -lexpat -lodbc

CPPFLAGS  = $(DEFINES) $(INCLUDES)
CXXFLAGS  = -g -O0 -fPIC -Werror

RCFILE = WorldPad.rc
DEFFILE = WorldPad.def

LINK_LIBS := \
	$(LINK_LIBS) \
	$(OUT_DIR)/libAppCore.a \
	$(OUT_DIR)/libGeneric.a \
	$(OUT_DIR)/libCellar.a \
	$(COM_OUT_DIR)/libFwKernelTlb.a \
	$(COM_OUT_DIR)/libViewsTlb.a \
	$(COM_OUT_DIR)/libCellarTlb.a \
	$(WIN32MORE_LIB)/libWin32More.a \
	$(COM_LIB)/libcom.so \
	$(WIN32BASE_LIB)/libWin32Base.a \

# === Object Lists ===

OBJ_WORLDPAD = \
	$(INT_DIR)/WpDa.o \
	$(INT_DIR)/WpXml.o \
	$(INT_DIR)/UtilSil2.o \
	$(COM_OUT_DIR)/libWorldPadTlb.a \

OBJ_OTHER = \
	$(VIEWS_OBJ)/lib/VwUndo.o \
	$(VIEWS_OBJ)/lib/VwCacheDa.o \
	$(VIEWS_OBJ)/lib/VwBaseDataAccess.o \
	$(GENERIC_OBJ)/GenericFactory.o \
	$(GENERIC_OBJ)/TextProps1.o \
	$(APPCORE_OBJ)/FwStyledText.o \

# === Targets ===

all: $(OUT_DIR)/libWorldPad.so $(COM_OUT_DIR)/libWorldPadTlb.a ComponentsMap

$(OUT_DIR)/libWorldPad.so: $(OBJ_WORLDPAD) $(OBJ_OTHER) $(LINK_LIBS)
	$(LINK.cc) -DDEBUG -shared -o $@ $^ $(LDLIBS)

$(COM_OUT_DIR)/libWorldPadTlb.a: $(INT_DIR)/WorldPad_GUIDs.o
	$(AR) $(ARFLAGS) $@ $^

link_check: libWorldPad

libWorldPad: $(OBJ_WORLDPAD) $(OBJ_OTHER) $(GENERIC_OBJ)/main.o $(LINK_LIBS)
	$(LINK.cc) -o $@ $^ $(LDLIBS)

print-helpful-info:
	@echo LDLIBS=$(LDLIBS)
	@echo LINK_LIBS=$(LINK_LIBS)
	@echo CPPFLAGS=$(CPPFLAGS)
	@echo LDFLAGS=$(LDFLAGS)
	@echo LD_LIBRARY_PATH=$(LD_LIBRARY_PATH)
	@echo PATH=$(PATH)

clean:
	$(RM) \
		$(OUT_DIR)/libWorldPad.so \
		$(COM_OUT_DIR)/libWorldPadTlb.a \
		$(OUT_DIR)/$(BUILD_PRODUCT).components.map \
		libWorldPad \
		$(INT_DIR)/*.[od] $(INT_DIR)/*.gch \

ComponentsMap: $(OUT_DIR)/libWorldPad.so
	LD_LIBRARY_PATH=$(shell $(ICU_BIN_DIR)/icu-config --prefix)/lib:$(ICU_LIB_DIR):$(COM_LIB) PATH=$(PATH):$(COM_DIR)/bin$(ARCH) generate-map.sh $(shell $(BUILD_ROOT)/Bin/abs.py $(OUT_DIR)/libWorldPad.so) > $(OUT_DIR)/libWorldPad.compmap

# === Dependencies ===

#PCHNAME = $(INT_DIR)/Main.h.gch

#$(OBJ_WORLDPAD): $(PCHNAME)

# Ensure object dependencies are built.

$(VIEWS_OBJ)/lib/VwUndo.o:
	@$(MAKE) -C $(VIEWS_SRC) $@ -q || \
	 $(MAKE) -C $(VIEWS_SRC) $@

$(VIEWS_OBJ)/lib/VwCacheDa.o:
	@$(MAKE) -C $(VIEWS_SRC) $@ -q || \
	 $(MAKE) -C $(VIEWS_SRC) $@

$(VIEWS_OBJ)/lib/VwBaseDataAccess.o:
	@$(MAKE) -C $(VIEWS_SRC) $@ -q || \
	 $(MAKE) -C $(VIEWS_SRC) $@

$(GENERIC_OBJ)/GenericFactory.o:
	@$(MAKE) -C $(GENERIC_SRC) $@ -q || \
	 $(MAKE) -C $(GENERIC_SRC) $@

$(GENERIC_OBJ)/TextProps1.o:
	@$(MAKE) -C $(GENERIC_SRC) $@ -q || \
	 $(MAKE) -C $(GENERIC_SRC) $@

# This file is only for link_check targets
$(GENERIC_OBJ)/main.o:
	@$(MAKE) -C $(SRC)/$(GENERIC_NAME)/ -q $@ || \
	 $(MAKE) -C $(SRC)/$(GENERIC_NAME)/ $@

# Ensure static library dependencies are built.

$(OUT_DIR)/libAppCore.a:
	@$(MAKE) -C $(SRC)/AppCore $@ -q || \
	 $(MAKE) -C $(SRC)/AppCore $@

$(OUT_DIR)/libGeneric.a:
	@$(MAKE) -C $(SRC)/Generic $@ -q || \
	 $(MAKE) -C $(SRC)/Generic $@

$(OUT_DIR)/libCellar.a:
	@$(MAKE) -C $(SRC)/Cellar $@ -q || \
	 $(MAKE) -C $(SRC)/Cellar $@

$(COM_OUT_DIR)/libFwKernelTlb.a:
	@$(MAKE) -C $(SRC)/Kernel $@ -q || \
	 $(MAKE) -C $(SRC)/Kernel $@

$(COM_OUT_DIR)/libViewsTlb.a:
	@$(MAKE) -C $(SRC)/views $@ -q || \
	 $(MAKE) -C $(SRC)/views $@

$(COM_OUT_DIR)/libCellarTlb.a:
	@$(MAKE) -C $(SRC)/Cellar $@ -q || \
	 $(MAKE) -C $(SRC)/Cellar $@

$(LIBRARIES)/Win32More/src/libWin32More.a:
	@$(MAKE) -C $(BUILD_ROOT) libWin32More.a -q || \
	 $(MAKE) -C $(BUILD_ROOT) libWin32More.a

$(LIBRARIES)/Win32Base/src/libWin32Base.a:
	@$(MAKE) -C $(BUILD_ROOT) libWin32Base.a -q || \
	 $(MAKE) -C $(BUILD_ROOT) libWin32Base.a

# === Rules ===

%.h.gch: %.h
	$(COMPILE.cc)  -DDEBUG  -o $@ $<

-include $(OBJ_ALL:%.o=%.d)
-include $(PCHNAME:%.gch=%.d)
