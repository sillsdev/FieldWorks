# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=GrCompiler
BUILD_EXTENSION=exe
BUILD_REGSVR=0

# EXE_MODULE is defined, to get correct ModuleEntry sections.
# DEFS=$(DEFS) /DEXE_MODULE=1
DEFS=$(DEFS) /D"_CONSOLE" /D"_MBCS" /DGR_FW

GRC_SRC=$(BUILD_ROOT)\Src\Graphite\GrCompiler
GRC_RES=$(BUILD_ROOT)\Src\Graphite\GrCompiler
GRC_GRMR_SRC=$(BUILD_ROOT)\Src\Graphite\GrCompiler\Grammar
GRC_ANTLR_SRC=$(BUILD_ROOT)\Src\Graphite\GrCompiler\Grammar\Antlr
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
GR_LIB_SRC=$(BUILD_ROOT)\Src\Graphite\lib
TTF_LIB_SRC=$(BUILD_ROOT)\Src\Graphite\TtfUtil

# Set the USER_INCLUDE environment variable.
UI=$(GRC_SRC);$(GRC_GRMR_SRC);$(GRC_ANTLR_SRC);$(GENERIC_SRC);$(GR_LIB_SRC);$(TTF_LIB_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

#adjust switches to same value as original Visual Studio project
CL_OPTS=$(CL_OPTS:/WX=)		# turn off "treat warnings as errors" switch
CL_OPTS=$(CL_OPTS:/W4=/W3)	# use warning level 3 instead of 4
#CL_OPTS=$(CL_OPTS:/Gr= )	# turn off _fastcall convention, use _cdecl convention by default
#CL_OPTS=/GZ $(CL_OPTS)		# turn on "catch release build errors in debug build" switch
#CL_OPTS=$(CL_OPTS:/Zi=/ZI)	# build edit & continue debug info instead of standard debug info

LINK_OPTS=$(LINK_OPTS:subsystem:windows=subsystem:console)
#LINK_OPTS=$(LINK_OPTS:incremental:no=incremental:yes) # turn on incremental linking

#CL_OPTS=/FR$(INT_DIR)\autopch\ $(CL_OPTS) # build browser info, need to run bscmake by hand

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

#PATH=$(BUILD_ROOT)\Output\Common;$(PATH)

RCFILE=GrCompiler.rc
# DEFFILE=GrCompiler.def
LINK_LIBS=Generic.lib $(LINK_LIBS)

# === Object Lists ===

# We need ModuleEntry here because the version in Generic.lib was compiled without /DEXE_MODULE=1
# and will not give the right functions for an exe.
OBJ_GRC=\
	$(INT_DIR)\autopch\ANTLRException.obj\
	$(INT_DIR)\autopch\AST.obj\
	$(INT_DIR)\autopch\ASTFactory.obj\
	$(INT_DIR)\autopch\BitSet.obj\
	$(INT_DIR)\autopch\CharBuffer.obj\
	$(INT_DIR)\autopch\CharScanner.obj\
	$(INT_DIR)\autopch\CommonASTNode.obj\
	$(INT_DIR)\autopch\CommonToken.obj\
	$(INT_DIR)\autopch\Compiler.obj\
	$(INT_DIR)\autopch\ErrorCheckFeatures.obj\
	$(INT_DIR)\autopch\ErrorCheckClasses.obj\
	$(INT_DIR)\autopch\ErrorCheckRules.obj\
	$(INT_DIR)\autopch\explicit_instantiations.obj\
	$(INT_DIR)\autopch\Fsm.obj\
	$(INT_DIR)\autopch\GdlExpression.obj\
	$(INT_DIR)\autopch\GdlFeatures.obj\
	$(INT_DIR)\autopch\GdlGlyphClassDefn.obj\
	$(INT_DIR)\autopch\GdlRenderer.obj\
	$(INT_DIR)\autopch\GdlRule.obj\
	$(INT_DIR)\autopch\GdlTablePass.obj\
	$(INT_DIR)\autopch\GrcErrorList.obj\
	$(INT_DIR)\autopch\GrcFont.obj\
	$(INT_DIR)\autopch\GrcGlyphAttrMatrix.obj\
	$(INT_DIR)\autopch\GrcManager.obj\
	$(INT_DIR)\autopch\GrcMasterTable.obj\
	$(INT_DIR)\autopch\GrcSymTable.obj\
	$(INT_DIR)\autopch\GrpExtensions.obj\
	$(INT_DIR)\autopch\GrpLexer.obj\
	$(INT_DIR)\autopch\GrpParser.obj\
	$(INT_DIR)\autopch\GrpParserDebug.obj\
	$(INT_DIR)\autopch\InputBuffer.obj\
	$(INT_DIR)\autopch\LexerSharedInputState.obj\
	$(INT_DIR)\autopch\LLkParser.obj\
	$(INT_DIR)\autopch\MismatchedTokenException.obj\
	$(INT_DIR)\autopch\main.obj\
	$(INT_DIR)\autopch\NoViableAltException.obj\
	$(INT_DIR)\autopch\OutputToFont.obj\
	$(INT_DIR)\autopch\Parser.obj\
	$(INT_DIR)\autopch\ParserException.obj\
	$(INT_DIR)\autopch\ParserSharedInputState.obj\
	$(INT_DIR)\autopch\ParserTreeWalker.obj\
	$(INT_DIR)\autopch\PostParser.obj\
	$(INT_DIR)\autopch\ScannerException.obj\
	$(INT_DIR)\autopch\String.obj\
	$(INT_DIR)\autopch\Token.obj\
	$(INT_DIR)\autopch\TokenBuffer.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\TtfUtil.obj\
	$(INT_DIR)\autopch\GrPlatform.obj\

# LINK_LIBS=Version.lib $(LINK_LIBS)

OBJ_ALL=$(OBJ_GRC)

# === Targets ===
BUILD_TARGETS=$(BUILD_TARGETS)

!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(INT_DIR)\$(RCFILE:.rc=.res): $(GRC_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(GRC_RES)\*.rc

# === Rules ===
PCHNAME=GrCompiler

ARG_SRCDIR=$(GRC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GRC_GRMR_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GRC_ANTLR_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GR_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(TTF_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Targets ===
