# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=SimpleDll
BUILD_EXTENSION=dll
BUILD_REGSVR=1

SIMPLEDLL_SRC=$(BUILD_ROOT)\Sample\SimpleDll
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic

UI=$(SIMPLEDLL_SRC);$(GENERIC_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=SimpleDll.rc
DEFFILE=SimpleDll.def
LINK_LIBS=Generic.lib $(LINK_LIBS)


# === Object Lists ===

OBJ_SIMPLEDLL=\
	$(INT_DIR)\autopch\SampleInterface.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\


IDL_MAIN=$(COM_OUT_DIR)\SimpleDllTlb.idl

PS_MAIN=SimpleDllPs

OBJ_ALL= $(OBJ_SIMPLEDLL)

OBJECTS_IDH=$(COM_INT_DIR)\Objects.idh


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=SimpleDll

ARG_SRCDIR=$(SIMPLEDLL_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===
$(OBJ_ALL): $(OBJECTS_H)

# === Custom Targets ===
