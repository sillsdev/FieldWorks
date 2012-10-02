# the configuration defaults to Debug
!IF "$(CFG)" == ""
CFG=Debug
!ENDIF

OUTDIR=..\..\$(CFG)
INTDIR0=..\..\..\Obj
INTDIR1=$(INTDIR0)\ParserObject
INTDIR=$(INTDIR1)\$(CFG)
NULL=

CPP_PROJ=/nologo /W3 /GX /I "../../../Include/" /D "WIN32" /D "_MBCS" /D "_LIB" /D "XML_UNICODE_WCHAR_T" /Fp"$(INTDIR)\ParserObject.pch" /YX /Fo"$(INTDIR)\\" /Fd"$(INTDIR)\\" /FD /c

!IF  "$(CFG)" == "Release"

CPP_PROJ=$(CPP_PROJ) /MT /O2 /D "NDEBUG"

!ELSE

CPP_PROJ=$(CPP_PROJ) /MTd /Gm /GZ /ZI /Od /D "_DEBUG"

!ENDIF

all : "$(OUTDIR)\ParserObject.lib"

clean :
	-@erase "$(INTDIR)\CExPat.obj"
	-@erase "$(INTDIR)\CXMLDefRec.obj"
	-@erase "$(INTDIR)\*.idb"
	-@erase "$(INTDIR)\*.pch"
	-@erase "$(OUTDIR)\ParserObject.lib"

"$(OUTDIR)" :
	if not exist "$(OUTDIR)\$(NULL)" mkdir "$(OUTDIR)"

"$(INTDIR0)" :
	if not exist "$(INTDIR0)\$(NULL)" mkdir "$(INTDIR0)"
"$(INTDIR1)" :
	if not exist "$(INTDIR1)\$(NULL)" mkdir "$(INTDIR1)"
"$(INTDIR)" :
	if not exist "$(INTDIR)\$(NULL)" mkdir "$(INTDIR)"

CPP=cl.exe

.c{$(INTDIR)}.obj::
   $(CPP) @<<
   $(CPP_PROJ) $<
<<

.cpp{$(INTDIR)}.obj::
   $(CPP) @<<
   $(CPP_PROJ) $<
<<

.cxx{$(INTDIR)}.obj::
   $(CPP) @<<
   $(CPP_PROJ) $<
<<

.c{$(INTDIR)}.sbr::
   $(CPP) @<<
   $(CPP_PROJ) $<
<<

.cpp{$(INTDIR)}.sbr::
   $(CPP) @<<
   $(CPP_PROJ) $<
<<

.cxx{$(INTDIR)}.sbr::
   $(CPP) @<<
   $(CPP_PROJ) $<
<<

LIB32=link.exe -lib
LIB32_FLAGS=/nologo /out:"$(OUTDIR)\ParserObject.lib"
LIB32_OBJS= \
	"$(INTDIR)\CExPat.obj" \
	"$(INTDIR)\CXMLDefRec.obj"

"$(OUTDIR)\ParserObject.lib" : "$(OUTDIR)" $(DEF_FILE) $(LIB32_OBJS)
	$(LIB32) @<<
  $(LIB32_FLAGS) $(DEF_FLAGS) $(LIB32_OBJS)
<<
	-copy CEXPat.h ..\..\..\Include
	-copy CXMLDataRec.h ..\..\..\Include

"$(INTDIR)\CExPat.obj" : .\CExPat.cpp "$(INTDIR)"

"$(INTDIR)\CXMLDefRec.obj" : .\CXMLDefRec.cpp "$(INTDIR)"
