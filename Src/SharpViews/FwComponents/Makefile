BUILD_ROOT = ../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(VIEWS_NAME)
include $(BUILD_ROOT)/Bld/_init.mak.lnx

OUTPUT  = $(FWOUTPUT)
INCLUDE = $(FWINCLUDE)

COMMONDLGS_SRC = $(SRC)/CommonCOMDlgs
GRENG_LIB_SRC  = $(SRC)/Graphite/lib
GRFW_SRC       = $(SRC)/Graphite/FwOnly
TLB_SRC        = $(OUTPUT)/Common

# -I$(AFCORE_SRC)

PACKAGES = gdk-2.0 gtkmm-2.4 cairomm-1.0 pangomm-1.4 freetype2 uuid enchant

DEFINES  = -DGR_FW -DVIEWSDLL -DSUPPRESS_FW_EXCEPTION_HANDLING -DWIN32_KEY_VALUES=1 -DENABLE_TSF

ifeq ($(BUILD_CONFIG),Debug)
	DEBUG_LIBS = -lDebugProcs
	DEFINES  := $(DEFINES) -D_DEBUG
endif

INCLUDES := \
	$(shell icu-config --cppflags) \
	-I. \
	-I$(VIEWS_LIB_SRC) -I$(GENERIC_SRC) -I$(APPCORE_SRC) -I$(DEBUGPROCS_SRC) \
	-I$(COMMONDLGS_SRC) -I$(GRENG_LIB_SRC) -I$(GRFW_SRC) \
	-I$(TLB_SRC) -I$(INCLUDE) \
	-I$(WIN32MORE_INC) \
	-I$(COM_INC) \
	-I$(WIN32BASE_INC) \
	$(shell pkg-config --cflags $(PACKAGES)) \


LDLIBS := \
	-L$(OUT_DIR) -lGeneric -lAppCore $(DEBUG_LIBS) \
	-L$(WIN32MORE_LIB) -lWin32More \
	-L$(COM_LIB) -lcom \
	-L$(WIN32BASE_LIB) -lWin32Base \
	$(shell pkg-config --libs $(PACKAGES)) \
	$(shell icu-config --ldflags) \


CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g -O0 -fPIC -fvisibility=hidden -Werror -Waddress -Warray-bounds -Wmissing-field-initializers -Wclobbered -Wreturn-type -fstack-protector-all

ARFLAGS  = -crs

RCFILE    = Views.rc
DEFFILE   = Views.def

PCHNAME = $(INT_DIR)/Main.h.gch

LINK_LIBS = \
	$(COM_OUT_DIR)/libFwKernelTlb.a

#	VwAccessRoot.o

#	$(AFCORE_SRC)/AfColorTable.o
#	$(AFCORE_SRC)/AfGfx.o
#	$(AFCORE_SRC)/FwStyledText.o
#	$(AFCORE_SRC)/WriteXml.o

# === Object Lists ===

OBJS = \
	$(INT_DIR)/ViewsGlobals.o \
	$(INT_DIR)/VwTextStoreX11.o \
	$(INT_DIR)/ExplicitInstantiation.o \
	$(INT_DIR)/VwEnv.o \
	$(INT_DIR)/VwLayoutStream.o \
	$(INT_DIR)/VwLazyBox.o \
	$(INT_DIR)/VwNotifier.o \
	$(INT_DIR)/VwOverlay.o \
	$(INT_DIR)/VwPattern.o \
	$(INT_DIR)/VwPrintContext.o \
	$(INT_DIR)/VwPropertyStore.o \
	$(INT_DIR)/VwRootBox.o \
	$(INT_DIR)/VwSelection.o \
	$(INT_DIR)/VwSimpleBoxes.o \
	$(INT_DIR)/VwSynchronizer.o \
	$(INT_DIR)/VwTableBox.o \
	$(INT_DIR)/VwTextBoxes.o \
	$(INT_DIR)/VwTxtSrc.o \
	$(INT_DIR)/VwInvertedViews.o \
	$(INT_DIR)/lib/GrJustifier.o \
	$(INT_DIR)/lib/GrTxtSrc.o \
	$(INT_DIR)/lib/VwBaseDataAccess.o \
	$(INT_DIR)/lib/VwBaseVirtualHandler.o \
	$(INT_DIR)/lib/VwCacheDa.o \
	$(INT_DIR)/lib/VwColor.o \
	$(INT_DIR)/lib/VwGraphicsCairo.o \
	$(INT_DIR)/lib/VwJustifier.o \
	$(INT_DIR)/lib/VwUndo.o \
	$(INT_DIR)/lib/Enchant.o \
	$(INT_DIR)/lib/DisplayCapsInfo.o \
	$(COM_OUT_DIR)/libViewsTlb.a \

VWG_OBJS = \
	$(INT_DIR)/lib/VwGraphicsCairo.o \
	$(INT_DIR)/lib/VwColor.o \
	$(INT_DIR)/lib/DisplayCapsInfo.o \
	$(COM_OUT_DIR)/libViewsTlb.a \

# === Targets ===

all: $(OUT_DIR)/libViews.so $(OUT_DIR)/libVwGraphics.so $(COM_OUT_DIR)/libViewsTlb.a ComponentsMap link_check

$(OUT_DIR)/libViews.so: $(OBJS) $(OUT_DIR)/libGeneric.a $(OUT_DIR)/libDebugProcs.a $(WIN32MORE_LIB)/libWin32More.a  $(WIN32BASE_LIB)/libWin32Base.a $(OUT_DIR)/libAppCore.a $(LINK_LIBS)
	$(LINK.cc) -shared -o $@ $^ $(LDLIBS)

$(OUT_DIR)/libVwGraphics.so: $(VWG_OBJS) $(LINK_LIBS) $(OUT_DIR)/libGeneric.a $(OUT_DIR)/libDebugProcs.a
	$(LINK.cc) -shared -o $@ $^ $(LDLIBS)

$(COM_OUT_DIR)/libViewsTlb.a: $(INT_DIR)/Views_GUIDs.o $(INT_DIR)/ViewsExtra_GUIDs.o
	$(AR) $(ARFLAGS) $@ $^

link_check: libViews libVwGraphics

libViews: $(OBJS) $(GENERIC_OBJ)/main.o $(LINK_LIBS)
	$(LINK.cc) -o $@ $^ $(LDLIBS)

libVwGraphics: $(VWG_OBJS) $(GENERIC_OBJ)/main.o $(LINK_LIBS)
	$(LINK.cc) -o $@ $^ $(LDLIBS)

ComponentsMap: $(OUT_DIR)/libViews.so $(OUT_DIR)/libVwGraphics.so
	(export PATH=$(PATH):$(COM_DIR)/bin$(ARCH) && cd $(OUT_DIR) && generate-map.sh libViews.so > $(OUT_DIR)/libViews.compmap)

# === Rules ===

$(OBJS): $(PCHNAME)

$(OUT_DIR)/libAppCore.a:
	@$(MAKE) -C $(APPCORE_SRC)/ -q all || \
	 $(MAKE) -C $(APPCORE_SRC)/

$(OUT_DIR)/libGeneric.a:
	@$(MAKE) -C $(GENERIC_SRC)/ -q all || \
	 $(MAKE) -C $(GENERIC_SRC)/

$(GENERIC_OBJ)/main.o:
	@$(MAKE) -C $(GENERIC_SRC)/ -q $@ || \
	 $(MAKE) -C $(GENERIC_SRC)/ $@

$(OUT_DIR)/libDebugProcs.a:
	@$(MAKE) -C $(DEBUGPROCS_SRC)/ -q all || \
	 $(MAKE) -C $(DEBUGPROCS_SRC)/

-include $(PCHNAME:%.gch=%.d)
-include $(OBJ_ALL:%.o=%.d)

clean:
	$(RM) $(OUT_DIR)/libViews.so $(OUT_DIR)/libVwGraphics.so $(COM_OUT_DIR)/libViewsTlb.a libViews libVwGraphics $(INT_DIR)/*.[od] $(INT_DIR)/*.gch $(INT_DIR)/lib/*.[od] $(PCHNAME)

%.h.gch: %.h
	$(COMPILE.cc) -o $@ $<
