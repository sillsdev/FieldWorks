
#	$Id: Makefile 2650 2009-12-15 14:15:18Z beilharz $
#
#	Makefile for Cellar library
#
#	Brent McCarthy - 2007-03-27

BUILD_ROOT = ../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(CELLAR_NAME)
include $(BUILD_ROOT)/Bld/_init.mak.lnx

SRC     = ..
BUILD_ROOT = $(SRC)/..

CELLAR_SRC  = .
CELLARLIB_SRC = lib
GENERIC_SRC = $(SRC)/Generic
APPCORE_SRC = $(SRC)/AppCore
KERNEL_SRC  = $(SRC)/Kernel
TLB_SRC     = $(COM_OUT_DIR)
DBSERVICES_SRC = $(SRC)/DbServices
DBACCESS_SRC = $(SRC)/DbAccess

DEFINES  := $(DEFINES) -DGR_FW -DSUPPRESS_FW_EXCEPTION_HANDLING -D_DEBUG

INCLUDES := -I$(INCLUDES) \
	-I$(CELLAR_SRC) -I$(CELLARLIB_SRC) -I$(GENERIC_SRC) -I$(APPCORE_SRC) \
	-I$(DBSERVICES_SRC) -I$(DBACCESS_SRC) -I$(TLB_SRC)

INCLUDES := \
	$(shell icu-config --cppflags) \
	$(INCLUDES) \
	-I$(FWINCLUDE) \
	-I$(LIBRARIES)/Win32More/include \
	-I$(LIBRARIES)/COM/include \
	-I$(LIBRARIES)/Win32Base/include \


LDLIBS = \
	$(shell icu-config --ldflags) \
	-L$(LIBRARIES)/Win32More/src/build$(ARCH) -lWin32More \
	-L$(LIBRARIES)/COM/build$(ARCH)/src/.libs -lcom \
	-L$(LIBRARIES)/Win32Base/src/build$(ARCH) -lWin32Base \
	-luuid -lexpat \
	-lodbc \
	$(COM_OUT_DIR)/libCellarTlb.a \

CPPFLAGS = $(DEFINES) $(INCLUDES) $(shell pkg-config --cflags gtkmm-2.4)
CXXFLAGS = -g -O0 -fPIC -Werror

ARFLAGS  = -crs

RCFILE = FwCellar.rc
DEFFILE = FwCellar.def

LINK_LIBS := \
	$(OUT_DIR)/libGeneric.a \
	$(OUT_DIR)/libAppCore.a \
	$(COM_OUT_DIR)/libFwKernelTlb.a \
	$(LINK_LIBS)

# === Object Lists ===

#OBJ_FWCELLAR = \
#	$(INT_DIR)/FwXml.o \
#	$(INT_DIR)/FwXmlImport.o \
#	$(INT_DIR)/FwXmlExport.o \
#	$(INT_DIR)/SqlDb.o

# // not used
#	$(INT_DIR)/FwXmlData.o \
# === Targets ===

#no-op
all:

#all: $(COM_OUT_DIR)/libCellarTlb.a $(OUT_DIR)/libCellar.so $(OUT_DIR)/libCellar.a

#$(OUT_DIR)/libCellar.so: $(OBJ_FWCELLAR) $(LINK_LIBS) $(LIBRARIES)/Win32More/src/build$(ARCH)/libWin32More.a $(LIBRARIES)/Win32Base/src/build$(ARCH)/libWin32Base.a
#	$(LINK.cc) -shared -o $@ $^ $(LDLIBS)

#$(OUT_DIR)/libCellar.a: $(OBJ_FWCELLAR)
#	$(AR) $(ARFLAGS) $@ $^

#$(COM_OUT_DIR)/libCellarTlb.a:	$(INT_DIR)/Cellar_GUIDs.o
#	$(AR) $(ARFLAGS) $@ $^

#link_check: libCellar

#libCellar: $(OBJ_FWCELLAR)  $(GENERIC_OBJ)/main.o $(LINK_LIBS)
#	$(LINK.cc) -o $@ $^ $(LDLIBS)

#no-op
#libCellar:

# === Rules ===

PCHNAME = $(INT_DIR)/Main.h.gch

$(OBJ_FWCELLAR): $(PCHNAME)

$(OUT_DIR)/libAppCore.a:
	@$(MAKE) -C $(SRC)/AppCore $@ -q  || \
	 $(MAKE) -C $(SRC)/AppCore $@

$(OUT_DIR)/libGeneric.a:
	@$(MAKE) -C $(SRC)/Generic $@ -q || \
		 $(MAKE) -C $(SRC)/Generic $@

$(COM_OUT_DIR)/libFwKernelTlb.a:
	@$(MAKE) -C $(SRC)/Kernel $@ -q || \
	 $(MAKE) -C $(SRC)/Kernel $@

$(LIBRARIES)/Win32More/src/libWin32More.a:
	@$(MAKE) -C $(BUILD_ROOT) -q libWin32More.a || \
	 $(MAKE) -C $(BUILD_ROOT) libWin32More.a

$(LIBRARIES)/Win32Base/src/libWin32Base.a:
	@$(MAKE) -C $(BUILD_ROOT) -q libWin32Base.a || \
	 $(MAKE) -C $(BUILD_ROOT) libWin32Base.a

-include $(OBJ_ALL:%.o=%.d)
-include $(PCHNAME:%.gch=%.d)

clean:
	$(RM) $(OUT_DIR)/libCellar.so $(OUT_DIR)/libCellar.a $(COM_OUT_DIR)/libCellarTlb.a libCellar $(INT_DIR)/*.[od] $(INT_DIR)/*.gch
