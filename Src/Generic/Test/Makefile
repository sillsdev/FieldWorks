#
#	Makefile for Generic test programs. Adapted from views/Test/Makefile by Eberhard Beilharz
#
#	Neil Mayhew - 07 Sep 2006

BUILD_ROOT = ../../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(GENERIC_NAME)Test
include $(BUILD_ROOT)/Bld/_init.mak.lnx

INC_UNIT = $(BUILD_ROOT)/Include/unit++
LIB_UNIT  = $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)

DEFINES := -DGR_FW -DSUPPRESS_FW_EXCEPTION_HANDLING -D_DEBUG

PACKAGES = gtkmm-2.4

INCLUDES := -I$(GENERIC_SRC) -I$(GENERIC_SRC)/Test \

INCLUDES := \
	-I$(SRC)/DebugProcs \
	$(INCLUDES) \
	-I$(COM_OUT_DIR) \
	-I$(INC_UNIT) \
	-I$(LIBRARIES)/Win32More/include \
	-I$(LIBRARIES)/COM/include \
	-I$(LIBRARIES)/Win32Base/include \
	$(shell icu-config --cppflags) \
	-I$(FWINCLUDE) \
	$(shell pkg-config --cflags $(PACKAGES)) \


LDLIBS := \
	$(shell icu-config --ldflags) \
	-L$(OUT_DIR) -lDebugProcs \
	-L$(FWINCLUDE) \
	-L$(LIBRARIES)/Win32More/src/build$(ARCH) -lWin32More \
	-L$(LIBRARIES)/COM/build$(ARCH)/src/.libs -lcom \
	-L$(LIBRARIES)/Win32Base/src/build$(ARCH) -lWin32Base \
	$(shell pkg-config --libs $(PACKAGES)) \
	-lexpat\

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g -O0 -fPIC -fvisibility=hidden -Werror

PRECOMPS = $(GENERIC_OBJ)/Main.h.gch

LINK_LIBS := $(LIB_UNIT)/libunit++.a $(OUT_DIR)/libGeneric.a $(OUT_DIR)/libDebugProcs.a
#COMS :=

PROGS = $(OUT_DIR)/TestUnicodeConverter $(OUT_DIR)/TestOleStringLiteral $(OUT_DIR)/TestCOMBase \
	$(OUT_DIR)/TestHashMap $(OUT_DIR)/TestSmartBstr $(OUT_DIR)/TestGenericFactory \
	$(OUT_DIR)/TestStringTable
OBJS  = $(PROGS:$(OUT_DIR)/%=$(INT_DIR)/%.o)
LIBS  =

GENERIC_OBJS = \
	$(GENERIC_OBJ)/Debug.o \
	$(GENERIC_OBJ)/OleStringLiteral.o \
	$(GENERIC_OBJ)/UnicodeConverter.o \
	$(GENERIC_OBJ)/HashMap.o \
	$(GENERIC_OBJ)/ModuleEntry.o \

DEPS = $(PRECOMPS:%.gch=%.d)

-include $(DEPS)

all: $(OUT_DIR)/testGenericLib $(PROGS)

$(OUT_DIR)/testGenericLib: $(INT_DIR)/testGeneric.o $(INT_DIR)/Collection.o $(GENERIC_OBJS) $(LINK_LIBS) $(COMS)
	$(LINK.cc) -o $@ $(INT_DIR)/testGeneric.o $(INT_DIR)/Collection.o $(GENERIC_OBJS) $(LINK_LIBS) $(LDLIBS)

$(OUT_DIR)/TestUnicodeConverter: $(INT_DIR)/TestUnicodeConverter.o $(GENERIC_OBJ)/UnicodeConverter.o

$(OUT_DIR)/TestOleStringLiteral: $(INT_DIR)/TestOleStringLiteral.o $(GENERIC_OBJ)/OleStringLiteral.o $(GENERIC_OBJ)/UnicodeConverter.o

$(OUT_DIR)/TestCOMBase: $(INT_DIR)/TestCOMBase.o $(LINK_LIBS)

$(OUT_DIR)/TestHashMap: $(INT_DIR)/TestHashMap.o $(LINK_LIBS)

$(OUT_DIR)/TestSmartBstr: $(INT_DIR)/TestSmartBstr.o $(LINK_LIBS)

$(OUT_DIR)/TestGenericFactory: $(INT_DIR)/TestGenericFactory.o $(LINK_LIBS)

$(OUT_DIR)/TestStringTable: $(INT_DIR)/TestStringTable.o $(LINK_LIBS)

test: $(PROGS)
	cd $(OUT_DIR) && \
	@for PROG in $(PROGS); \
	do \
		ICU_DATA=$(shell $(BUILD_ROOT)/Bin/abs.py $(BUILD_ROOT)/DistFiles/Icu40/) \
		FW_ROOT=$(shell $(BUILD_ROOT)/Bin/abs.py $(BUILD_ROOT)/DistFiles/) \
		FW_ROOTCODE=$(shell $(BUILD_ROOT)/Bin/abs.py $(BUILD_ROOT)/DistFiles/) \
		COMPONENTS_MAP_PATH=$(shell $(BUILD_ROOT)/Bin/abs.py $(OUT_DIR)) \
		LD_LIBRARY_PATH=$(LD_LIBRARY_PATH):$(shell icu-config --prefix)/lib:$(shell $(BUILD_ROOT)/Bin/abs.py $(BUILD_ROOT)/../COM/build$(ARCH)/lib) \
		./$$PROG || echo $$PROG failed; \
	done

check: all
	cd $(OUT_DIR) && \
	ICU_DATA=$(shell $(BUILD_ROOT)/Bin/abs.py $(BUILD_ROOT)/DistFiles/Icu40/) \
	FW_ROOT=$(shell $(BUILD_ROOT)/Bin/abs.py $(BUILD_ROOT)/DistFiles/) \
	FW_ROOTCODE=$(shell $(BUILD_ROOT)/Bin/abs.py $(BUILD_ROOT)/DistFiles/) \
	COMPONENTS_MAP_PATH=$(shell $(BUILD_ROOT)/Bin/abs.py $(OUT_DIR)) \
	LD_LIBRARY_PATH=$(LD_LIBRARY_PATH):$(shell icu-config --prefix)/lib:$(shell $(BUILD_ROOT)/Bin/abs.py $(BUILD_ROOT)/../COM/build$(ARCH)/lib) \
	./testGenericLib

clean:
	$(RM) $(OUT_DIR)/testGenericLib $(INT_DIR)/testGeneric.o $(INT_DIR)/Collection.cpp *.[od] \
		*.gch $(OBJS) $(DEPS) $(LIBS) $(PROGS)

clean.other:
	$(RM) $(OTHER_PROGS) $(OTHER_OBJS) $(OTHER_DEPS) $(OTHER_LIBS)

%.h.gch: %.h
	$(COMPILE.cc) -o $@ $<

# Rule to make a program from a .o, but including LDLIBS
$(OUT_DIR)/%: $(INT_DIR)/%.o
	$(LINK.cc) -o $@ $^ $(LDLIBS)

# Rule to make assembler output (for debugging)
%.s: %.cpp
	$(COMPILE.cc) -S -o $@.tmp $<
	@c++filt <$@.tmp >$@
	@$(RM) $@.tmp

# Rule to make pre-processor output (for debugging)
%.i: %.cpp
	$(COMPILE.cc) -E -o $@ $<

COLLECT=$(BUILD_ROOT)/Bin/CollectUnit++Tests.sh Generic

$(INT_DIR)/Collection.cpp: \
	TestErrorHandling.h \
	TestFwSettings.h \
	testGenericLib.h \
	TestSmartBstr.h \
	TestStringTable.h \
	TestUtil.h \
	TestUtilString.h \
	TestUtilXml.h
	@echo Collecting tests for testGenericLib
	$(COLLECT) $^ >$@

OTHER_PROGS =
OTHER_OBJS  = $(GENERIC_OBJ)/UnicodeConverter.o $(GENERIC_OBJ)/HashMap.o $(GENERIC_OBJ)/OleStringLiteral.o \
	$(GENERIC_OBJ)/Debug.o
OTHER_LIBS  = $(OUT_DIR)/libGeneric.a $(OUT_DIR)/libDebugProcs.a

$(OTHER_PROGS) $(OTHER_OBJS) $(OTHER_LIBS)::
	@$(MAKE) -C $(@D) $(@F) -q || \
	 $(MAKE) -C $(@D) $(@F)

DEPS       = $(OBJS:%.o=%.d)
OTHER_DEPS = $(OTHER_OBJS:%.o=%.d)
