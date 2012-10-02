.SUFFIXES : .odl .idl .def .r .sql .sqo .cm

# Remove extra space
DEFS=$(DEFS:  = )
CL_OPTS=$(CL_OPTS:  = )
LIBLINK_OPTS=$(LIBLINK_OPTS:  = )
LINK_OPTS=$(LINK_OPTS:  = )
RC_OPTS=$(RC_OPTS:  = )
RES_OPTS=$(RES_OPTS:  = )
MRC_OPTS=$(MRC_OPTS:  = )
MIDL_OPTS=$(MIDL_OPTS:  = )

!IF "$(BUILD_OS)"=="win95"
NUL=nul
!ELSE
NUL=
!ENDIF


!IF "$(SQL_MAIN)"!="" && "$(SQL_DST_MAIN)"==""
SQL_DST_MAIN=$(COM_OUT_DIR)\$(SQL_MAIN)
!ENDIF

!IF "$(SQO_MAIN)"!="" && "$(SQO_DST_MAIN)"==""
SQO_DST_MAIN=$(COM_INT_DIR)\$(SQO_MAIN)
SQH_DST_MAIN=$(COM_OUT_DIR)\$(SQO_MAIN:.sqo=.sqh)
!ENDIF

!IF "$(SQL_DST_MAIN)"!=""
BUILD_TARGETS=$(SQL_DST_MAIN) $(BUILD_TARGETS)
!ENDIF


!IF "$(PS_MAIN)"!=""
PS_DLL_MAIN=$(COM_OUT_DIR)\$(PS_MAIN).dll
PS_IDL_MAIN=$(COM_OUT_DIR)\$(PS_MAIN).idl

PS_H_MAIN=$(COM_OUT_DIR)\$(PS_MAIN).h
PS_DEF_MAIN=$(COM_INT_DIR)\$(PS_MAIN).def

CL_OPTS_PS=/c /Ox /W0 /I"$(BUILD_ROOT)\Output\Common\Raw" /I"$(COM_OUT_DIR_RAW)" $(CL_OPTS_PS) /DWIN32 /D_WIN32_WINNT=0x0500 /DREGISTER_PROXY_DLL
!ENDIF


!IF "$(PS_DLL_MAIN)"!=""
BUILD_TARGETS=$(BUILD_TARGETS) $(PS_DLL_MAIN)
!ENDIF


# === Resource and definition files ===
!IF "$(DEFFILE)"!=""
OBJ_DEFFILE=$(INT_DIR)\$(DEFFILE)
!ENDIF

!IF "$(RCFILE)"!=""
OBJ_RCFILE=$(INT_DIR)\$(RCFILE:.rc=.res)
!ENDIF


# === Targets ===
INCLUDE=$(USER_INCLUDE);$(INCLUDE)


# Default stuff to build
build: dirs $(BUILD_TARGETS)


# Main executable target
main: dirs $(BUILD_MAIN)


# Added 27-MAR-2001 TLB: Target to build source code browser database
!IF "$(BSC_INT_DIR)"!=""
!CMDSWITCHES +I
$(BSC_INT_DIR)\Fw.bsc: $(BSC_INT_DIR)\*.sbr
	if exist $(BSC_INT_DIR)\*.sbr bscmake /o $@ $(BSC_INT_DIR)\*.sbr
!CMDSWITCHES -I
!ENDIF

# Erase just the main build target
delmain:
!IF "$(BUILD_MAIN)"!=""
	if exist "$(BUILD_MAIN)" del /q $(BUILD_MAIN)
!ENDIF


# Directories
dirs: $(OUT_DIR) $(INT_DIR) $(COM_OUT_DIR) $(COM_OUT_DIR_RAW) $(COM_INT_DIR) $(BSC_INT_DIR)


sql: dirs $(SQL_DST_MAIN)


# Erase just the main sql target
delsql:
!IF "$(SQL_DST_MAIN)"!=""
	if exist "$(SQL_DST_MAIN)" del $(SQL_DST_MAIN)
	if exist "$(SQO_DST_MAIN)" del $(SQO_DST_MAIN)
	if exist "$(SQH_DST_MAIN)" del $(SQH_DST_MAIN)
!ENDIF


# Erase OBJ_ALL
delobjs:
	if exist "$(INT_DIR)\genpch" $(DELNODE) /q "$(INT_DIR)\genpch"
	if exist "$(INT_DIR)\usepch" $(DELNODE) /q "$(INT_DIR)\usepch"
	if exist "$(INT_DIR)\autopch" $(DELNODE) /q "$(INT_DIR)\autopch"
	if exist "$(INT_DIR)\nopch" $(DELNODE) /q "$(INT_DIR)\nopch"


# Erase build specific stuff
clean: delmain delsql
	if exist "$(INT_DIR)/$(NUL)" $(DELNODE) /q "$(INT_DIR)"
	if exist "$(OUT_DIR)\$(BUILD_PRODUCT)" del /q $(OUT_DIR)\$(BUILD_PRODUCT)\*.*

# Erase build specific stuff and common stuff
# Modified 27-MAR-2001 TLB: added "clean" as a dependency rather than continuing to maintain
# the body of the clean target in two places. If there is a problem with this, add all the lines
# from the clean target body (above) immediately following the cleancom target (next line).
cleancom: delps clean
	if exist "$(COM_OUT_DIR)\$(BUILD_PRODUCT)Tlb.*" del /q $(COM_OUT_DIR)\$(BUILD_PRODUCT)Tlb.*
	if exist "$(COM_OUT_DIR_RAW)\$(BUILD_PRODUCT)Tlb*.*" del /q $(COM_OUT_DIR_RAW)\$(BUILD_PRODUCT)Tlb*.*
	-if not "$(PS_MAIN)"=="" if exist "$(COM_OUT_DIR)/$(NUL)" del /q $(COM_OUT_DIR)\$(PS_MAIN)*.*
	-if not "$(PS_MAIN)"=="" if exist "$(COM_OUT_DIR_RAW)/$(NUL)" del /q $(COM_OUT_DIR_RAW)\$(PS_MAIN)*.*


!IF "$(BUILD_EXTENSION)"=="dll" || "$(BUILD_EXTENSION)"=="ocx"


register: build
!IF "$(BUILD_REGSVR)"=="1"
	$(REGSVR) $(REGSVR_OPTS) $(BUILD_MAIN)
!IF "$(PS_DLL_MAIN)"!="" && "$(ISLUA)"!="1"
	$(REGSVR) $(REGSVR_OPTS) $(PS_DLL_MAIN)
!ENDIF
!ENDIF

unregister:
!IF "$(BUILD_REGSVR)"=="1"
	if exist "$(BUILD_MAIN)" $(REGSVR) /u $(REGSVR_OPTS) $(BUILD_MAIN)
!IF "$(PS_DLL_MAIN)"!="" && "$(ISLUA)"!="1"
	if exist "$(PS_DLL_MAIN)" $(REGSVR) /u $(REGSVR_OPTS) $(PS_DLL_MAIN)
!ENDIF
!ENDIF

!ELSEIF "$(BUILD_EXTENSION)"=="exe"

!IF "$(BUILD_TYPE)"=="b"

register: build
!IF "$(BUILD_REGSVR)"=="1"
	tcdev /B /S $(OUT_DIR)\dummy.tcs $(BUILD_MAIN) /RegServer
!ENDIF

unregister:
!IF "$(BUILD_REGSVR)"=="1"
	if exist "$(BUILD_MAIN)" tcdev /B /S $(OUT_DIR)\dummy.tcs $(BUILD_MAIN) /UnregServer
!ENDIF

!ELSE

register: build
!IF "$(BUILD_REGSVR)"=="1"
	$(BUILD_MAIN) /RegServer
!ENDIF

unregister:
!IF "$(BUILD_REGSVR)"=="1"
	if exist "$(BUILD_MAIN)" $(BUILD_MAIN) /UnregServer
!ENDIF

!ENDIF

!ELSE

register: build

unregister:

!ENDIF


# === Linking ===
!IF "$(OBJ_DEFFILE)"!=""
LINK_OPTS=$(LINK_OPTS) /def:$(OBJ_DEFFILE)
!ENDIF

!IF "$(BUILD_EXTENSION)"=="lib"

$(BUILD_MAIN): $(OBJ_ALL) $(OBJ_RCFILE)
	$(DISPLAY) Linking $@
	$(LIBLINK) $(LIBLINK_OPTS) @<<$(INT_DIR)\$(@B).txt
$(OBJ_RCFILE) $(OBJ_ALL) /SUBSYSTEM:WINDOWS
<<KEEP

!ELSEIF "$(BUILD_EXTENSION)"=="dll" || "$(BUILD_EXTENSION)"=="ocx" || "$(BUILD_EXTENSION)"=="exe"

$(BUILD_MAIN): $(OBJ_ALL) $(OBJ_RCFILE) $(OBJ_DEFFILE)
	$(DISPLAY) Linking $@
	$(LINK) $(LINK_OPTS) @<<$(INT_DIR)\$(@B).txt
$(OBJ_RCFILE) $(OBJ_ALL) $(LINK_LIBS)
<<KEEP

!ENDIF


# === Type libs ===
!IF "$(IDL_MAIN)"!=""
TLB_MAIN=$(IDL_MAIN:.idl=.tlb)

$(TLB_MAIN): $(IDL_MAIN)

!IF "$(OBJ_ALL)"!=""
$(OBJ_ALL): $(TLB_MAIN)
!ENDIF

!IF "$(OBJ_RCFILE)"!=""
$(OBJ_RCFILE): $(TLB_MAIN)
!ENDIF

!ENDIF

!IF "$(ODL_MAIN)"!=""
TLB_MAIN=$(ODL_MAIN:.odl=.tlb)

$(TLB_MAIN): $(ODL_MAIN)

!IF "$(OBJ_ALL)"!=""
$(OBJ_ALL): $(TLB_MAIN)
!ENDIF

!IF "$(OBJ_RCFILE)"!=""
$(OBJ_RCFILE): $(TLB_MAIN)
!ENDIF

!ENDIF


# === SQL Script Generation ===
!IF "$(SQL_DST_MAIN)"!=""
$(SQL_DST_MAIN): dirs $(SQO_DST_MAIN)
!ENDIF


# === Proxy / Stub Generation ===
!IF "$(PS_MAIN)"!=""
$(PS_H_MAIN): $(PS_IDL_MAIN)


proxystub: dirs $(PS_DLL_MAIN)


delps:
	if exist "$(PS_DLL_MAIN)" del $(PS_DLL_MAIN)


regps: proxystub
	$(REGSVR) $(REGSVR_OPTS) $(PS_DLL_MAIN)


unregps:
	if exist "$(PS_DLL_MAIN)" $(REGSVR) /u $(REGSVR_OPTS) $(PS_DLL_MAIN)


PS_OBJ=$(PS_OBJ) $(COM_INT_DIR)\$(PS_MAIN)_d.obj  $(COM_INT_DIR)\$(PS_MAIN)_i.obj $(COM_INT_DIR)\$(PS_MAIN)_p.obj


$(PS_DLL_MAIN): $(PS_H_MAIN) $(PS_DEF_MAIN) $(PS_OBJ)
	link /dll /out:$(PS_DLL_MAIN) /def:$(PS_DEF_MAIN) $(LINK_OPTS_PS) /entry:DllMain /implib:$(COM_INT_DIR)\$(PS_MAIN).lib $(PS_OBJ) kernel32.lib rpcns4.lib rpcrt4.lib oleaut32.lib uuid.lib ole32.lib


{$(COM_OUT_DIR_RAW)}.c{$(COM_INT_DIR)}.obj:
	cl $(CL_OPTS_PS) /Fo"$(COM_INT_DIR)/" $<


{$(BUILD_ROOT)\Output\Common\Raw}.c{$(COM_INT_DIR)}.obj:
	cl $(CL_OPTS_PS) /Fo"$(COM_INT_DIR)/" $<

# Modified 9-MAY-200@ RandyR: Removed 'description' and ordinal numbers for exported methods,
# since the .Net compiler isn't happy with them.
$(PS_DEF_MAIN):
	@$(ECHO) LIBRARY "$(PS_MAIN)" > $(PS_DEF_MAIN)
	@$(ECHO). >> $(PS_DEF_MAIN)
	@$(ECHO) EXPORTS >> $(PS_DEF_MAIN)
	@$(ECHO)     DllGetClassObject	  PRIVATE >> $(PS_DEF_MAIN)
	@$(ECHO)     DllCanUnloadNow	  PRIVATE >> $(PS_DEF_MAIN)
	@$(ECHO)     GetProxyDllInfo	  PRIVATE >> $(PS_DEF_MAIN)
	@$(ECHO)     DllRegisterServer    PRIVATE >> $(PS_DEF_MAIN)
	@$(ECHO)     DllUnregisterServer  PRIVATE >> $(PS_DEF_MAIN)

!ELSE

proxystub: dirs

delps:

regps: proxystub

unregps:

!ENDIF


# === Directories ===
!IF "$(OBJ_ALL)"!=""
$(OBJ_ALL) : $(INT_DIR)\genpch $(INT_DIR)\usepch $(INT_DIR)\autopch $(INT_DIR)\nopch
!ENDIF

!IF "$(OUT_DIR)"!=""
$(OUT_DIR):; if not exist "$@/$(NUL)" $(MD) "$@"
!ENDIF

!IF "$(INT_DIR)"!="" && "$(INT_DIR)"!="$(OUT_DIR)"
$(INT_DIR):; if not exist "$@/$(NUL)" $(MD) "$@"
!ENDIF

!IF "$(COM_OUT_DIR)"!="" && "$(COM_OUT_DIR)"!="$(OUT_DIR)" && "$(COM_OUT_DIR)"!="$(INT_DIR)"
$(COM_OUT_DIR):; if not exist "$@/$(NUL)" $(MD) "$@"
!ENDIF

!IF "$(COM_INT_DIR)"!="" && "$(COM_INT_DIR)"!="$(OUT_DIR)" && "$(COM_INT_DIR)"!="$(INT_DIR)" && "$(COM_INT_DIR)"!="$(COM_OUT_DIR)"
$(COM_INT_DIR):; if not exist "$@/$(NUL)" $(MD) "$@"
!ENDIF

!IF "$(COM_OUT_DIR_RAW)"!="" && "$(COM_OUT_DIR_RAW)"!="$(OUT_DIR)" && "$(COM_OUT_DIR_RAW)"!="$(INT_DIR)" && "$(COM_OUT_DIR_RAW)"!="$(COM_OUT_DIR)" && "$(COM_OUT_DIR_RAW)"!="$(COM_INT_DIR)"
$(COM_OUT_DIR_RAW):; if not exist "$@/$(NUL)" $(MD) "$@"
!ENDIF

# Added 27-MAR-2001 TLB: If there is no intermediate directory for source code browser
# files, create it
!IF "$(BSC_INT_DIR)"!="" && "$(BSC_INT_DIR)"!="$(OUT_DIR)" && "$(BSC_INT_DIR)"!="$(INT_DIR)" && "$(BSC_INT_DIR)"!="$(COM_OUT_DIR)" && "$(BSC_INT_DIR)"!="$(COM_INT_DIR)"
$(BSC_INT_DIR):; if not exist "$@/$(NUL)" $(MD) "$@"
!ENDIF

$(INT_DIR)\genpch: $(INT_DIR); if not exist "$@/$(NUL)" $(MD) "$@"

$(INT_DIR)\usepch: $(INT_DIR); if not exist "$@/$(NUL)" $(MD) "$@"

$(INT_DIR)\autopch: $(INT_DIR); if not exist "$@/$(NUL)" $(MD) "$@"

$(INT_DIR)\nopch: $(INT_DIR); if not exist "$@/$(NUL)" $(MD) "$@"
