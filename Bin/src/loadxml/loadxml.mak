# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=loadxml
BUILD_EXTENSION=exe
BUILD_REGSVR=1

# EXE_MODULE is defined, to get correct ModuleEntry sections for LoadXML.
DEFS=$(DEFS) /DEXE_MODULE=1

CELLAR_XML=$(BUILD_ROOT)\src\Cellar\XML
PROG_SRC=$(BUILD_ROOT)\Bin\Src\loadxml
CELLAR_SRC=$(BUILD_ROOT)\src\Cellar
CELLAR_LIB_SRC=$(BUILD_ROOT)\src\Cellar\Lib
GENERIC_SRC=$(BUILD_ROOT)\src\Generic

# Set the USER_INCLUDE environment variable.
UI=$(CELLAR_SRC);$(CELLAR_LIB_SRC);$(GENERIC_SRC);C:\Program Files\PlatformSDK\Include

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

XML_INC=$(CELLAR_XML)


!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(BUILD_ROOT)\Output\Common;$(PATH)

RCFILE=
DEFFILE=
LINK_LIBS=cport.lib odbc32.lib $(LINK_LIBS) /subsystem:console
LINK_OPTS=/NODEFAULTLIB:LIBCD $(LINK_OPTS)

# === Object Lists ===

!INCLUDE "$(GENERIC_SRC)\GenericInc.mak"

OBJ_LOADXML=\
	$(INT_DIR)\autopch\loadxml.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\


OBJ_ALL=$(OBJ_LOADXML) $(OBJ_GENERIC)


IDL_MAIN=


PS_MAIN=


# This Make file contains the conceptual model object lists.
# !INCLUDE "$(BUILD_ROOT)\src\Cellar\Objects.mak"

SQL_ALL=$(SQL_CELLAR)

OBJECTS_SQI=$(COM_INT_DIR)\Objects.sqi

SQL_MAIN=

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

SQL: $(OUT_DIR) $(INT_DIR) $(COM_OUT_DIR) $(COM_OUT_DIR_RAW) $(COM_INT_DIR) $(SQL_MAIN)

# === Rules ===
PCHNAME=loadxml

ARG_SRCDIR=$(CELLAR_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_XML)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===
#$(OBJECTS_SQI): $(CELLAR_XML)\Cellar.cm
#	$(SED) -n -e "s/.*\""\(..*\)\.xml.*/#include \""\1.sql\""/p" $(CELLAR_XML)\Cellar.cm > $@
#
#$(SQL_MAIN): $(OBJECTS_SQI) $(SQL_ALL) $(CELLAR_SRC)\Cellar.sql
#	$(DISPLAY) Preprocessing $@
#	if exist "$@" del "$@"
#	$(PREPROCESS) /EP $(PREPROCESS_OPTS) $(DEFS) $(CELLAR_SRC)\Cellar.sql >$@

# === Custom Targets ===
