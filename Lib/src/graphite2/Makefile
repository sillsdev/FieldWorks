
#	$Id: Makefile 2650 2009-12-15 14:15:18Z beilharz $
#
#	Makefile for Graphite2 library
#
#	Neil Mayhew - 29 Aug 2006

ifndef BUILD_TYPE
BUILD_TYPE = d
endif
ifndef BUILD_CONFIG
BUILD_CONFIG = Debug
endif

BUILD_ROOT = ../../..

_NS = GR2
GR2_MACHINE = direct
GR2_BASE = $(BUILD_ROOT)/Lib/src/graphite2
GR2_SRC = $(GR2_BASE)/src
GR2_INC = $(GR2_BASE)/include
INT_DIR = $(GR2_BASE)/Obj/$(BUILD_CONFIG)
OUT_DIR = $(BUILD_ROOT)/Lib/linux/$(BUILD_CONFIG)
GR2_LIB = $(OUT_DIR)/libgraphite2.a

DEFINES  := $(DEFINES) -DGRAPHITE2_STATIC

ifeq ($(BUILD_CONFIG),Debug)
	DEFINES  := $(DEFINES) -D_DEBUG
	OPTIMIZATIONS = -O0
else
	OPTIMIZATIONS = -O3
endif

INCLUDES := $(INCLUDES) -I$(GR2_SRC) -I$(GR2_INC)

CPPFLAGS = $(DEFINES) $(INCLUDES) -MMD
CXXFLAGS = -g $(OPTIMIZATIONS) -fPIC -fvisibility=hidden -Werror -Waddress -Warray-bounds -Wmissing-field-initializers -Wclobbered -Wreturn-type -fstack-protector-all -Wshadow -rdynamic

ARFLAGS  = -crs

# === Object Lists ===
include $(GR2_SRC)/files.mk

OBJS = $(patsubst $(GR2_SRC)/%.cpp, $(INT_DIR)/%.o, $(GR2_SOURCES))

# === Targets ===

all: $(GR2_LIB)

$(GR2_LIB): $(OBJS)
	$(AR) $(ARFLAGS) $@ $^

# === Rules ===

-include $(OBJS:%.o=%.d)

$(INT_DIR)/%.o: $(GR2_SRC)/%.cpp
	@mkdir -p $(@D)
	$(CXX) $(CXXFLAGS) $(CPPFLAGS) $(INCLUDES) $(TARGET_ARCH) \
	  -c $< -o $@ -MMD -MF $(@:%.o=%.d)

clean:
	$(RM) -r $(GR2_LIB) $(INT_DIR)
