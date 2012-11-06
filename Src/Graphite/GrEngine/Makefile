# Makefile for GrEngine
# TODO:
# Needs a 'USER_INCLUDE' macro

# CXX = g++-3.3

BUILD_ROOT = ../../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = Graphite/GrEngine
include $(BUILD_ROOT)/Bld/_init.mak.lnx

FWROOT       = $(BUILD_ROOT)
INCLUDE      = $(FWINCLUDE)
OUTPUT       = $(FWOUTPUT)
OUTPUT_COMMON= $(COM_OUT_DIR)

GRAPHITE_SRC = $(SRC)/Graphite
GR_FW_SRC    = $(GRAPHITE_SRC)/FwOnly
GR_LIB_SRC   = $(GRAPHITE_SRC)/lib
GR_LIB_DIR   = $(OBJ_DIR)/$(BUILD_CONFIG)/Graphite/lib
GR_OPEN_SRC  = $(GRAPHITE_SRC)/OpenSource
GR_TTFUTIL   = $(GRAPHITE_SRC)/TtfUtil

VIEWS_LIB_SRC = $(VIEWS_SRC)/lib

PACKAGES = silgraphite glib-2.0 gtk+-2.0 glibmm-2.4 gtkmm-2.4 cairomm-1.0

DEFINES  += -DGR_FW -DVIEWSDLL
INCLUDES += \
	-I$(INCLUDE) \
	-I. \
	-I$(GENERIC_SRC) -I$(GR_LIB_SRC) \
	-I$(GR_TTFUTIL) -I$(VIEWS_LIB_SRC) \
	-I$(OUTPUT) -I$(OUTPUT_COMMON) \
	-I$(WIN32MORE_INC) \
	-I$(COM_INC) \
	-I$(WIN32BASE_INC) \
	$(shell pkg-config --cflags $(PACKAGES)) \
	$(shell icu-config --cppflags) \

LDLIBS = \
	-L$(GENERIC_SRC) -lGeneric \
	-L$(WIN32MORE_LIB) -lWin32More \
	-L$(COM_LIB) -lcom \
	-L$(WIN32BASE_LIB) -lWin32Base \
	$(shell pkg-config --libs $(PACKAGES)) \
	$(shell icu-config --ldflags) \
	-lMagick++ -lMagick -luuid

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g -O0 -fPIC -fvisibility=hidden

ARFLAGS  = -crs

#libGraphite doesn't build
#all: $(OUT_DIR)/libGraphite.a $(COM_OUT_DIR)/libGraphiteTlb.a
all: $(COM_OUT_DIR)/libGraphiteTlb.a $(GR_LIB_DIR)/GrUtil.o

# only file currently used
$(GR_LIB_DIR)/GrUtil.o: $(GR_LIB_SRC)/GrUtil.cpp
	@mkdir -p $(@D)
	$(CXX) $(CXXFLAGS) $(CPPFLAGS) $(INCLUDES) $(TARGET_ARCH) \
	  -c $< -o $@ -MMD -MF $(@:%.o=%.d)

RCFILE  = Graphite.rc
DEFFILE = Graphite.def
LD_LIBS = -lGeneric


# OBJS = \
	explicit_instantiations.o \
	FileInput.o \
	GrCharStream.o \
	GrClassTable.o \
	Font.o \
	FontCache.o \
	GrEngine.o \
	FontFace.o \
	GrFeature.o \
	GrFSM.o \
	GrGlyphTable.o \
	GrPass.o \
	GrPassActionCode.o \
	SegmentPainter.o \
	WinSegmentPainter.o \
	SegmentAux.o \
	Segment.o \
	FwGrSegment.o \
	GrSlotState.o \
	GrSlotStream.o \
	GrTableManager.o \
	TestFSM.o \
	TestPasses.o \
	TransductionLog.o \
	$(GENERIC_SRC)/ModuleEntry.o \
	$(GR_TTFUTIL)/TtfUtil.o \
	$(GR_LIB_DIR)/GrUtil.o \
	$(GR_LIB_DIR)/GrPlatform.o \
	$(GR_LIB_DIR)/FwGraphiteProcess.o \
	FwGr.o

OBJS = \
	FwGrSegment.o \
	GrEngine.o \
	FileInput.o \
	GrCharStream.o \
	GrClassTable.o \
	GrFeature.o \
	GrFSM.o \
	GrGlyphTable.o \
	GrPass.o \
	GrPassActionCode.o \
	GrSlotState.o \
	GrSlotStream.o \
	GrTableManager.o \
	TestFSM.o \
	TestPasses.o \
	TransductionLog.o \
	$(GR_TTFUTIL)/TtfUtil.o \
	$(GR_LIB_DIR)/GrUtil.o \
	$(GR_LIB_DIR)/GrPlatform.o \
	$(GR_LIB_DIR)/FwGraphiteProcess.o \
	FwGr.o \
	explicit_instantiations.o \
	Font.o \
	FontCache.o \
	FontFace.o \
	SegmentPainter.o \
	WinSegmentPainter.o \
	SegmentAux.o \
	Segment.o \
	$(GENERIC_OBJ)/ModuleEntry.o \

$(OUT_DIR)/libGraphite.a: $(OBJS)
	$(AR) $(ARFLAGS) $@ $^

$(COM_OUT_DIR)/libGraphiteTlb.a: $(INT_DIR)/Graphite_GUIDs.o
	$(AR) $(ARFLAGS) $@ $^

-include $(PCHNAME:%.gch=%.d)

clean:
	$(RM) $(OUT_DIR)/libGraphite.a $(COM_OUT_DIR)/libGraphiteTlb.a $(OBJS) $(INT_DIR)/*.[od]  $(INT_DIR)/*.gch $(GR_LIB_DIR)/*.[od]
