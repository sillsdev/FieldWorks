# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testGenericLib
BUILD_EXTENSION=exe
BUILD_REGSVR=0

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

UNITPP_INC=$(BUILD_ROOT)\Include\unit++
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
GENERICTEST_SRC=$(BUILD_ROOT)\Src\Generic\Test

# Set the USER_INCLUDE environment variable.
UI=$(UNITPP_INC);$(GENERICTEST_SRC);$(GENERIC_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

LINK_OPTS=$(LINK_OPTS:/subsystem:windows=/subsystem:console) /LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)"
CPPUNIT_LIBS=unit++.lib
LINK_LIBS=$(CPPUNIT_LIBS) Generic.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_KERNELTESTSUITE=\
	$(INT_DIR)\genpch\Collection.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\testGeneric.obj\

OBJ_ALL=$(OBJ_KERNELTESTSUITE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=testGenericLib

ARG_SRCDIR=$(GENERICTEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===


# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\gawk.exe -f $(BUILD_ROOT)\Bin\CollectUnit++Tests.awk

$(INT_DIR)\genpch\Collection.obj: $(GENERICTEST_SRC)\Collection.cpp

$(GENERICTEST_SRC)\Collection.cpp: $(GENERICTEST_SRC)\testGenericLib.h\
 $(GENERICTEST_SRC)\TestUtil.h\
 $(GENERICTEST_SRC)\TestUtilXml.h\
 $(GENERICTEST_SRC)\TestUtilString.h\
 $(GENERICTEST_SRC)\TestErrorHandling.h\
 $(GENERICTEST_SRC)\TestFwSettings.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** >$(GENERICTEST_SRC)\Collection.cpp

$(INT_DIR)\genpch\testGeneric.obj: $(GENERICTEST_SRC)\testGeneric.cpp
