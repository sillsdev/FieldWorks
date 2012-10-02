# === C Source ===
{$(ARG_SRCDIR)}.cpp{$(INT_DIR)}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)/" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.c{$(INT_DIR)}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)/" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

# === GenPch ===
{$(ARG_SRCDIR)}.c{$(INT_DIR)\genpch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\genpch/" /Fp"$(INT_DIR)\$(PCHNAME).pch" /Yc$(PCHNAME).h $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\genpch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\genpch/" /Fp"$(INT_DIR)\$(PCHNAME).pch" /Yc$(PCHNAME).h $(CL_OPTS) $(DEFS) $<
<<NOKEEP

# === UsePch ===
{$(ARG_SRCDIR)}.c{$(INT_DIR)\usepch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\usepch/" /Fp"$(INT_DIR)\$(PCHNAME).pch" /Yu$(PCHNAME).h $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\usepch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\usepch/" /Fp"$(INT_DIR)\$(PCHNAME).pch" /Yu$(PCHNAME).h $(CL_OPTS) $(DEFS) $<
<<NOKEEP


# === AutoPch ===
!IF "$(PCHVER)"==""

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ELSEIF "$(PCHVER)"=="0"

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME0).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME0).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ELSEIF "$(PCHVER)"=="1"

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME1).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME1).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ELSEIF "$(PCHVER)"=="2"

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME2).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME2).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ELSEIF "$(PCHVER)"=="3"

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME3).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME3).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ELSEIF "$(PCHVER)"=="4"

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME4).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME4).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ELSEIF "$(PCHVER)"=="5"

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME5).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME5).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ELSEIF "$(PCHVER)"=="6"

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME6).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME6).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ELSEIF "$(PCHVER)"=="7"

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME7).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME7).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ELSEIF "$(PCHVER)"=="8"

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME8).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME8).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ELSEIF "$(PCHVER)"=="9"

{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME9).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\autopch/" /Fp"$(INT_DIR)\$(PCHNAME9).pch" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

!ENDIF

# === NoPch ===
{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\nopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\nopch/" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.c{$(INT_DIR)\nopch}.obj:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"$(INT_DIR)\nopch/" $(CL_OPTS) $(DEFS) $<
<<NOKEEP


# === Resources ===
{$(ARG_SRCDIR)}.rc{$(INT_DIR)}.res:
	$(DISPLAY) Compiling $<
	$(PREPROCESS) >$(INT_DIR)\$(<F) @<<
/I$(<D) /E $(PREPROCESS_OPTS) $(DEFS) /DRC_INVOKED=1 $<
<<NOKEEP
	$(RC) $(RC_OPTS) /I$(<D) /I$(COM_OUT_DIR) /fo$@ $(INT_DIR)\$(<F)

{$(ARG_SRCDIR)}.rc{$(INT_DIR)}.rsc:
	$(DISPLAY) Compiling $<
	$(PREPROCESS) >$(INT_DIR)\$(<F) @<<
/I$(<D) /E $(PREPROCESS_OPTS) $(DEFS) /DRC_INVOKED=1 $<
<<NOKEEP
	$(RC) $(RC_OPTS) /I$(<D) /I$(COM_OUT_DIR) /fo$@ $(INT_DIR)\$(<F)

{$(ARG_SRCDIR)}.r{$(INT_DIR)}.rsc:
	$(DISPLAY) Compiling $<
	$(PREPROCESS) >$(INT_DIR)\$(<F) @<<
/I$(<D) /E $(PREPROCESS_OPTS) $(DEFS) $<
<<NOKEEP
	$(MRC) $(MRC_OPTS) /o$@ /I$(<D) /I$(COM_OUT_DIR) $(INT_DIR)\$(<F)


# === Module Definition ===
!IF "$(ARG_SRCDIR)"!="$(INT_DIR)"
{$(ARG_SRCDIR)}.def{$(INT_DIR)}.def:
	$(DISPLAY) Compiling $@
	$(COPYFILE) $< $(TEMP)\$(*B).tmp
	$(PREPROCESS) >$@ @<<
/EP $(PREPROCESS_OPTS) $(DEFS) $(TEMP)\$(*B).tmp
<<NOKEEP
	$(DELETEFILE) $(TEMP)\$(*B).tmp
!ENDIF


# == Midl ===
{$(ARG_SRCDIR)}.idl{$(COM_OUT_DIR)}.idl:
	$(DISPLAY) Preprocessing $<
	if exist "$@" del "$@"
	$(PREPROCESS) /E $(PREPROCESS_OPTS) $(DEFS) $< >$@

.idl.tlb:
	$(DISPLAY) Compiling $<
	if exist "$@" del "$@"
	$(MIDL) $(MIDL_OPTS) $(DEFS) /out $(COM_OUT_DIR_RAW) /tlb $(<R).tlb /dlldata $(COM_OUT_DIR_RAW)\$(<B)_d.c $<
	$(SED) -e "s/EXTERN_C const \(IID\|CLSID\|LIBID\|DIID\) \(IID\|CLSID\|LIBID\|DIID\)_\(..*\);/#define \2_\3 __uuidof(\3)/" $(COM_OUT_DIR_RAW)\$(<B).h > $(<R).h

.idl.h:
	$(DISPLAY) Compiling $<
	if exist "$@" del "$@"
	$(MIDL) $(MIDL_OPTS) $(DEFS) /out $(COM_OUT_DIR_RAW) /tlb $(<R).tlb /dlldata $(COM_OUT_DIR_RAW)\$(<B)_d.c $<
	$(SED) -e "s/EXTERN_C const \(IID\|CLSID\|LIBID\|DIID\) \(IID\|CLSID\|LIBID\|DIID\)_\(..*\);/#define \2_\3 __uuidof(\3)/" $(COM_OUT_DIR_RAW)\$(<B).h > $(<R).h

{$(ARG_SRCDIR)}.odl{$(COM_OUT_DIR)}.odl:
	$(DISPLAY) Copying $<
	if exist "$@" del "$@"
	$(COPYFILE) $< $@

.odl.tlb:
	$(DISPLAY) Compiling $<
	if exist "$@" del "$@"
	$(MIDL) /mktyplib203 /tlb $(<R).tlb $<

.odl.h:
	$(DISPLAY) Compiling $<
	if exist "$@" del "$@"
	$(MIDL) /mktyplib203 /tlb $(<R).tlb $<


# == Sql ===
{$(ARG_SRCDIR)}.cm{$(COM_INT_DIR)}.sqo:
	$(DISPLAY) Compiling $<
	$(CMCG) -p"$(XML_INC)" $< $@ $(COM_OUT_DIR)\$(@B).sqh

{$(ARG_SRCDIR)}.sql{$(COM_OUT_DIR)}.sql:
	$(DISPLAY) Preprocessing $<
	if exist "$@" del "$@"
	$(PREPROCESS) /EP $(PREPROCESS_OPTS) $(DEFS) $< >$@


# === For Generating Dependency Info ===
{$(ARG_SRCDIR)}.c{$(INT_DIR)\genpch}.dep:
	@$(ECHO) $< >>$(INT_DIR)\genpch.in

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\genpch}.dep:
	@$(ECHO) $< >>$(INT_DIR)\genpch.in


{$(ARG_SRCDIR)}.c{$(INT_DIR)\usepch}.dep:
	@$(ECHO) $< >>$(INT_DIR)\usepch.in

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\usepch}.dep:
	@$(ECHO) $< >>$(INT_DIR)\usepch.in


{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.dep:
	@$(ECHO) $< >>$(INT_DIR)\autopch.in

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.dep:
	@$(ECHO) $< >>$(INT_DIR)\autopch.in


{$(ARG_SRCDIR)}.c{$(INT_DIR)\nopch}.dep:
	@$(ECHO) $< >>$(INT_DIR)\nopch.in

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\nopch}.dep:
	@$(ECHO) $< >>$(INT_DIR)\nopch.in


{$(ARG_SRCDIR)}.def{$(INT_DIR)}.def_dep:
	@$(ECHO) $< >>$(INT_DIR)\other.in

{$(ARG_SRCDIR)}.rc{$(INT_DIR)}.rc_dep:
	@$(ECHO) $< >>$(INT_DIR)\other.in

{$(ARG_SRCDIR)}.r{$(INT_DIR)}.r_dep:
	@$(ECHO) $< >>$(INT_DIR)\other.in


# === For Generating Automatic Targets ===
{$(ARG_SRCDIR)}.c{$(INT_DIR)\genpch}.target:
	@$(ECHO) $(<B).obj : $(INT_DIR)\genpch\$(<B).obj >>$(TARGETFILE)

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\genpch}.target:
	@$(ECHO) $(<B).obj : $(INT_DIR)\genpch\$(<B).obj >>$(TARGETFILE)


{$(ARG_SRCDIR)}.c{$(INT_DIR)\usepch}.target:
	@$(ECHO) $(<B).obj : $(INT_DIR)\usepch\$(<B).obj >>$(TARGETFILE)

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\usepch}.target:
	@$(ECHO) $(<B).obj : $(INT_DIR)\usepch\$(<B).obj >>$(TARGETFILE)


{$(ARG_SRCDIR)}.c{$(INT_DIR)\autopch}.target:
	@$(ECHO) $(<B).obj : $(INT_DIR)\autopch\$(<B).obj >>$(TARGETFILE)

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\autopch}.target:
	@$(ECHO) $(<B).obj : $(INT_DIR)\autopch\$(<B).obj >>$(TARGETFILE)


{$(ARG_SRCDIR)}.c{$(INT_DIR)\nopch}.target:
	@$(ECHO) $(<B).obj : $(INT_DIR)\nopch\$(<B).obj >>$(TARGETFILE)

{$(ARG_SRCDIR)}.cpp{$(INT_DIR)\nopch}.target:
	@$(ECHO) $(<B).obj : $(INT_DIR)\nopch\$(<B).obj >>$(TARGETFILE)


{$(ARG_SRCDIR)}.def{$(INT_DIR)}.def_target:
	@$(ECHO) $(<F) : $< >>$(TARGETFILE)

{$(ARG_SRCDIR)}.rc{$(INT_DIR)}.rc_target:
	@$(ECHO) $(<F) : $< >>$(TARGETFILE)

{$(ARG_SRCDIR)}.r{$(INT_DIR)}.r_target:
	@$(ECHO) $(<F) : $< >>$(TARGETFILE)


# === Command Line Targets ===
{$(ARG_SRCDIR)}.c.cod:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"nul" /Fc"$(INT_DIR)\$(*B).cod" $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp.cod:
	$(DISPLAY) Compiling $<
	$(CL) @<<
/c /Fo"nul" /Fc"$(INT_DIR)\$(*B).cod" $(CL_OPTS) $(DEFS) $<
<<NOKEEP


{$(ARG_SRCDIR)}.c.i:
	$(DISPLAY) Compiling $<
	$(CL) >$(INT_DIR)\$(*B).i @<<
/E $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp.i:
	$(DISPLAY) Compiling $<
	$(CL) >$(INT_DIR)\$(*B).i @<<
/E $(CL_OPTS) $(DEFS) $<
<<NOKEEP

{$(ARG_SRCDIR)}.cpp.del:
	$(DELETEFILE) $(INT_DIR)\autopch\$(*B).obj
	$(DELETEFILE) $(INT_DIR)\nopch\$(*B).obj
	$(DELETEFILE) $(INT_DIR)\usepch\$(*B).obj
	$(DELETEFILE) $(INT_DIR)\genpch\$(*B).obj
