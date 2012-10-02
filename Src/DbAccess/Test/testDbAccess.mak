# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testDbAccess
BUILD_EXTENSION=exe
BUILD_REGSVR=0

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

UNITPP_INC=$(BUILD_ROOT)\Include\unit++
CELLARLIB_SRC=$(BUILD_ROOT)\Src\Cellar\lib
DBACCESS_SRC=$(BUILD_ROOT)\Src\DbAccess
DBACCESSTEST_SRC=$(BUILD_ROOT)\Src\DbAccess\Test
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic

# Set the USER_INCLUDE environment variable.
UI=$(UNITPP_INC);$(DBACCESSTEST_SRC);$(DBACCESS_SRC);$(GENERIC_SRC);$(CELLARLIB_SRC)

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
LINK_LIBS=$(CPPUNIT_LIBS) Generic.lib xmlparse.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_DBACCESSTESTSUITE=\
	$(INT_DIR)\genpch\testDbAccess.obj\
	$(INT_DIR)\genpch\Collection.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\DbAccess\autopch\OleDbEncap.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\DbAccess\autopch\DbAdmin.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\DbAccess\autopch\ModuleEntry.obj\

OBJ_ALL=$(OBJ_DBACCESSTESTSUITE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=testDbAccess

ARG_SRCDIR=$(DBACCESSTEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===


# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\gawk.exe -f $(BUILD_ROOT)\Bin\CollectUnit++Tests.awk

$(INT_DIR)\genpch\Collection.obj: $(DBACCESSTEST_SRC)\Collection.cpp

$(DBACCESSTEST_SRC)\Collection.cpp: $(DBACCESSTEST_SRC)\testDbAccess.h\
 $(DBACCESSTEST_SRC)\TestDbAdmin.h \
 $(DBACCESSTEST_SRC)\TestOleDbEncap.h \
 $(DBACCESSTEST_SRC)\TestOleDbCommand.h \
 $(DBACCESSTEST_SRC)\TestMetaDataCacheXml.h \
 $(DBACCESSTEST_SRC)\TestMetaDataCache.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** >$(DBACCESSTEST_SRC)\Collection.cpp
