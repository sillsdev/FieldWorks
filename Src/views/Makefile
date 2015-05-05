BUILD_ROOT = ../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(VIEWS_NAME)
include $(BUILD_ROOT)/Bld/_init.mak.lnx

OUTPUT  = $(FWOUTPUT)
INCLUDE = $(FWINCLUDE)

COMMONDLGS_SRC = $(SRC)/CommonCOMDlgs
TLB_SRC        = $(OUTPUT)/Common

# -I$(AFCORE_SRC)

PACKAGES = gdk-2.0 gtkmm-2.4 cairomm-1.0 pangomm-1.4 freetype2 uuid

DEFINES  = -DGR_FW -DVIEWSDLL -DSUPPRESS_FW_EXCEPTION_HANDLING -DWIN32_KEY_VALUES=1

ifeq ($(BUILD_CONFIG),Debug)
	DEBUG_LIBS = $(OUT_DIR)/libDebugProcs.a
	DEFINES  := $(DEFINES) -D_DEBUG -DDEBUG
	OPTIMIZATIONS = -O0
else
	OPTIMIZATIONS = -O3
endif

INCLUDES := \
	$(shell icu-config --detect-prefix --cppflags) \
	-I. -Ilib \
	-I$(GENERIC_SRC) -I$(APPCORE_SRC) -I$(DEBUGPROCS_SRC) \
	-I$(COMMONDLGS_SRC) \
	-I$(TLB_SRC) -I$(INCLUDE) \
	-I$(WIN32MORE_INC) \
	-I$(COM_INC) \
	-I$(WIN32BASE_INC) \
	-I$(ICU_INC_DIR) \
	$(shell pkg-config --cflags $(PACKAGES)) \


LDLIBS := \
	-L$(WIN32MORE_LIB) -lWin32More \
	-L$(COM_LIB) -lcom \
	-L$(WIN32BASE_LIB) -lWin32Base \
	$(shell pkg-config --libs $(PACKAGES)) \
	$(shell icu-config --detect-prefix --ldflags) \
	-ldl \


CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g $(OPTIMIZATIONS) -fPIC -fvisibility=hidden -Werror -Waddress -Warray-bounds -Wmissing-field-initializers -Wclobbered -Wreturn-type -fstack-protector-all -Wshadow -rdynamic

ARFLAGS  = -crs

RCFILE    = Views.rc
DEFFILE   = Views.def

PCHNAME = $(INT_DIR)/Main.h.gch

LINK_LIBS := \
	$(LINK_LIBS) \
	$(COM_OUT_DIR)/libFwKernelTlb.a \
	$(COM_OUT_DIR)/libViewsTlb.a \
	$(OUT_DIR)/libGeneric.a \
	$(OUT_DIR)/libAppCore.a \
	$(DEBUG_LIBS) \

# === Object Lists ===

OBJS = \
	$(INT_DIR)/ViewsGlobals.o \
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
	$(INT_DIR)/lib/VwBaseDataAccess.o \
	$(INT_DIR)/lib/VwBaseVirtualHandler.o \
	$(INT_DIR)/lib/VwCacheDa.o \
	$(INT_DIR)/lib/VwColor.o \
	$(INT_DIR)/lib/VwGraphicsCairo.o \
	$(INT_DIR)/lib/VwUndo.o \
	$(INT_DIR)/lib/DisplayCapsInfo.o \

VWG_OBJS = \
	$(INT_DIR)/lib/VwGraphicsCairo.o \
	$(INT_DIR)/lib/VwColor.o \
	$(INT_DIR)/lib/DisplayCapsInfo.o \

OBJS_OTHER = \
	$(INT_DIR)/ModuleEntry.o \

# === Targets ===

all: $(OUT_DIR)/libViews.so $(OUT_DIR)/libVwGraphics.so $(COM_OUT_DIR)/libViewsTlb.a ComponentsMap link_check

$(OUT_DIR)/libViews.so: $(OBJS) $(OBJS_OTHER) $(LINK_LIBS) $(WIN32MORE_LIB)/libWin32More.a  $(WIN32BASE_LIB)/libWin32Base.a
ifeq "$(GCC46)" "1"
	$(LINK.cc) -shared -o $@ -Wl,-whole-archive $(LINK_LIBS) -Wl,-no-whole-archive $(OBJS) $(OBJS_OTHER) $(LDLIBS)
else
	$(LINK.cc) -shared -o $@ $^ $(LDLIBS)
endif
	# TODO: this is a hack. Remove after implementing FWNX-823.
	-cd $(OUT_DIR) && ln -s $(@F) oleaut32.dll

$(OUT_DIR)/libVwGraphics.so: $(VWG_OBJS) $(OBJS_OTHER) $(LINK_LIBS)
	$(LINK.cc) -shared -o $@ $^ $(LDLIBS)

$(COM_OUT_DIR)/libViewsTlb.a: $(INT_DIR)/Views_GUIDs.o $(INT_DIR)/ViewsExtra_GUIDs.o
	$(AR) $(ARFLAGS) $@ $^

link_check: $(INT_DIR)/libViews $(INT_DIR)/libVwGraphics

$(INT_DIR)/libViews: $(OBJS) $(OBJS_OTHER) $(GENERIC_OBJ)/main.o $(LINK_LIBS)
	$(LINK.cc) -o $@ $^ $(LDLIBS)

libVwGraphics: $(INT_DIR)/libVwGraphics

$(INT_DIR)/libVwGraphics: $(VWG_OBJS) $(OBJS_OTHER) $(GENERIC_OBJ)/main.o $(LINK_LIBS)
	$(LINK.cc) -o $@ $^ $(LDLIBS)

ComponentsMap: $(OUT_DIR)/libViews.so $(OUT_DIR)/libVwGraphics.so
	(export PATH=$(PATH):$(COM_DIR)/build$(ARCH)/bin && cd $(OUT_DIR) && generate-map.sh libViews.so > $(OUT_DIR)/libViews.compmap)

# === Rules ===

$(OBJS): $(PCHNAME)

$(INT_DIR)/%.o: $(GENERIC_SRC)/%.cpp
	$(CXX) $(CXXFLAGS) $(CPPFLAGS) $(INCLUDES) $(TARGET_ARCH) \
	  -c $< -o $@ -MMD -MF $(@:%.o=%.d)

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
	$(RM) $(OUT_DIR)/libViews.so $(OUT_DIR)/libVwGraphics.so $(COM_OUT_DIR)/libViewsTlb.a $(INT_DIR)/libViews $(INT_DIR)/libVwGraphics $(INT_DIR)/*.[od] $(INT_DIR)/*.gch $(INT_DIR)/lib/*.[od] $(PCHNAME)

%.h.gch: %.h
	$(COMPILE.cc) -o $@ $<
