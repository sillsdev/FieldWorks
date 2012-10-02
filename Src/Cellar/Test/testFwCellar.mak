# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testFwCellar
BUILD_EXTENSION=exe
BUILD_REGSVR=0

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

UNITPP_INC=$(BUILD_ROOT)\Include\unit++
CELLARLIB_SRC=$(BUILD_ROOT)\Src\Cellar\lib
CELLAR_SRC=$(BUILD_ROOT)\Src\Cellar
CELLARTEST_SRC=$(BUILD_ROOT)\Src\Cellar\Test
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
APPCORE_SRC=$(BUILD_ROOT)\Src\AppCore

# Set the USER_INCLUDE environment variable.
UI=$(UNITPP_INC);$(CELLARTEST_SRC);$(CELLAR_SRC);$(GENERIC_SRC);$(CELLARLIB_SRC);$(APPCORE_SRC)

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
LINK_LIBS=xmlparse.lib uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib odbc32.lib $(LINK_LIBS)
LINK_LIBS=$(CPPUNIT_LIBS) Generic.lib $(LINK_LIBS)
RCFILE=FwTestCellar.rc

# === Object Lists ===

OBJ_CELLARTESTSUITE=\
	$(INT_DIR)\genpch\testFwCellar.obj\
	$(INT_DIR)\genpch\Collection.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwCellar\genpch\FwXmlData.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwCellar\autopch\FwXmlImport.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwCellar\autopch\FwXmlExport.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwCellar\autopch\SqlDb.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwCellar\usepch\TextProps1.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwCellar\autopch\ModuleEntry.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwCellar\autopch\WriteXml.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwCellar\autopch\FwStyledText.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\FwCellar\autopch\FwXml.obj\

OBJ_ALL=$(OBJ_CELLARTESTSUITE)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=testFwCellar

ARG_SRCDIR=$(CELLARTEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(APPCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===


# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\gawk.exe -f $(BUILD_ROOT)\Bin\CollectUnit++Tests.awk

$(INT_DIR)\genpch\Collection.obj: $(CELLARTEST_SRC)\Collection.cpp

$(CELLARTEST_SRC)\Collection.cpp: $(CELLARTEST_SRC)\testFwCellar.h\
 $(CELLARTEST_SRC)\TestFwXmlData.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** >$(CELLARTEST_SRC)\Collection.cpp


$(OBJ_RCFILE): $(CELLAR_SRC)\FwCellar.rc $(CELLAR_SRC)\XmlMsgs.rc
