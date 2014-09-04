# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

GR2_BASE=$(BUILD_ROOT)\Lib\src\graphite2
GR2_SRC=$(GR2_BASE)\src
GR2_INC=$(GR2_BASE)\include
OUT_DIR=$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)
INT_DIR=$(GR2_BASE)\Obj\$(BUILD_CONFIG)
GR2_LIB=$(OUT_DIR)\graphite2.lib

INCLUDE=$(INCLUDE);$(GR2_SRC);$(GR2_INC)

DEFS=/DGRAPHITE2_STATIC /DWIN32
CL_OPTS=/EHsc /Zi

!IF "$(BUILD_TYPE)"=="d"
CL_OPTS=$(CL_OPTS) /MTd /Od
DEFS=$(DEFS) /D_DEBUG
!ELSE
CL_OPTS=$(CL_OPTS) /MT /O2
DEFS=$(DEFS) /DNDEBUG
!ENDIF

LIBLINK_OPTS=/subsystem:windows

LIBLINK=lib.exe
CL=cl.exe /nologo
DISPLAY=@echo
DELETEFILE=del /q
DELNODE=rmdir /s /q
MD=$(BUILD_ROOT)\bin\mkdir.exe -p

# === Object Lists ===

!INCLUDE "$(GR2_SRC)\files.mk.win

OBJ_ALL=$(GR2_OBJECTS)

# === Targets ===

build: $(INT_DIR) $(GR2_LIB)

clean:
	if exist "$(INT_DIR)/$(NUL)" $(DELNODE) "$(INT_DIR)"
	$(DELETEFILE) $(GR2_LIB)

$(GR2_LIB): $(OBJ_ALL)
	$(DISPLAY) Linking $@
	$(LIBLINK) $(LIBLINK_OPTS) /out:"$@" $(OBJ_ALL)

$(INT_DIR):; if not exist "$@/$(NUL)" $(MD) "$@"

# === Rules ===

{$(GR2_SRC)}.cpp{$(INT_DIR)}.obj:
	$(DISPLAY) Compiling $<
	$(CL) /c /Fo"$(INT_DIR)/" /Fd"$(INT_DIR)/" $(CL_OPTS) $(DEFS) "$<"
