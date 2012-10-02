# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=FwCellar
BUILD_EXTENSION=dll
BUILD_REGSVR=1


CELLAR_XML=$(BUILD_ROOT)\src\Cellar\XML

CELLAR_SRC=$(BUILD_ROOT)\src\Cellar
CELLAR_LIB_SRC=$(BUILD_ROOT)\src\Cellar\Lib
GENERIC_SRC=$(BUILD_ROOT)\src\Generic
APPCORE_SRC=$(BUILD_ROOT)\src\AppCore

DBSERVICES_SRC=$(BUILD_ROOT)\src\DbServices
DBACCESS_SRC=$(BUILD_ROOT)\src\DbAccess
LANGUAGE_SRC=$(BUILD_ROOT)\src\Language

# Set the USER_INCLUDE environment variable.
UI=$(CELLAR_SRC);$(CELLAR_LIB_SRC);$(GENERIC_SRC);$(APPCORE_SRC)
UI=$(UI);$(DBSERVICES_SRC);$(DBACCESS_SRC);$(LANGUAGE_SRC)


!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

XML_INC=$(CELLAR_XML)


!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

RCFILE=FwCellar.rc
DEFFILE=FwCellar.def
LINK_LIBS=xmlparse.lib uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib odbc32.lib $(LINK_LIBS)
LINK_LIBS=Generic.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_CELLAR=\
	$(INT_DIR)\genpch\FwXmlData.obj\
	$(INT_DIR)\autopch\FwXmlImport.obj\
	$(INT_DIR)\autopch\FwXmlExport.obj\
	$(INT_DIR)\autopch\SqlDb.obj\
	$(INT_DIR)\usepch\TextProps1.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\WriteXml.obj\
	$(INT_DIR)\autopch\FwStyledText.obj\
	$(INT_DIR)\autopch\FwXml.obj\


OBJ_ALL=$(OBJ_CELLAR)


IDL_MAIN=$(COM_OUT_DIR)\FwCellarTlb.idl


PS_MAIN=FwCellarPs


SQL_MAIN=FwCellar.sql


SQO_MAIN=Cellar.sqo


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=main

ARG_SRCDIR=$(CELLAR_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_XML)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(APPCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Targets ===

$(OBJ_RCFILE): $(CELLAR_SRC)\XmlMsgs.rc
