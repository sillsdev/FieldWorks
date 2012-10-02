# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=DbAccess
BUILD_EXTENSION=dll
BUILD_REGSVR=1

DBACCESS_SRC=$(BUILD_ROOT)\Src\DbAccess
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
CELLARLIB_SRC=$(BUILD_ROOT)\Src\Cellar\lib
TEXT_SRC=$(BUILD_ROOT)\Src\Text
HELP_DIR=$(BUILD_ROOT)\help

# Set the USER_INCLUDE environment variable. Make sure DbAccess is first, as we want
# to get the Main.h from there, not any of the others (e.g., in Views)
UI=$(DBACCESS_SRC);$(GENERIC_SRC);$(CELLARLIB_SRC);$(TEXT_SRC)

# todo: do we still need this? Copied but not understood from an early version of LingServ.mak...
!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=DbAccess.rc
DEFFILE=DbAccess.def
LINK_LIBS= Generic.lib xmlparse.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_DBACCESS=\
	$(INT_DIR)\autopch\OleDbEncap.obj\
	$(INT_DIR)\autopch\DbAdmin.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\


IDL_MAIN=$(COM_OUT_DIR)\DbAccessTlb.idl

PS_MAIN=DbAccessPs

OBJ_ALL= $(OBJ_DBACCESS)

#OBJECTS_IDH=$(COM_INT_DIR)\Objects.idh

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=DbAccess

ARG_SRCDIR=$(DBACCESS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===

# === Custom Targets ===

# remove any optimizations: we're getting bogus code generation for this file
# symptom: When it entered OleDbEncap::Init() the BSTR strings were bogus.
CL_OPTS=$(CL_OPTS: /O2 = )

$(INT_DIR)\autopch\OleDbEncap.obj: $(DBACCESS_SRC)\OleDbEncap.cpp
	$(DISPLAY) Compiling $(DBACCESS_SRC)\OleDbEncap.cpp
	$(CL) @<<
/c /Fo"$(INT_DIR)/autopch/" $(CL_OPTS) $(DEFS) $(DBACCESS_SRC)\OleDbEncap.cpp
<<NOKEEP
