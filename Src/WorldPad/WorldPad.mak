# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=WorldPad
BUILD_EXTENSION=exe
BUILD_REGSVR=1

# EXE_MODULE is defined, to get correct ModuleEntry sections.
DEFS=$(DEFS) /DEXE_MODULE=1 /DGR_FW

WP_SRC=$(BUILD_ROOT)\Src\WorldPad
WP_RES=$(BUILD_ROOT)\Src\WorldPad\Res
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFCORE_RES=$(BUILD_ROOT)\Src\AppCore\Res
CELLAR_LIB_SRC=$(BUILD_ROOT)\src\Cellar\Lib
CELLAR_SRC=$(BUILD_ROOT)\src\Cellar
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
VIEW_LIB_SRC=$(BUILD_ROOT)\src\views\lib
LANG_LIB_SRC=$(BUILD_ROOT)\src\language\lib
HTMLHELP_LIB=$(BUILD_ROOT)\src\lib
HELP_DIR=$(BUILD_ROOT)\help
GRAPHITE_LIB_SRC=$(BUILD_ROOT)\Src\Graphite\lib
GRFW_SRC=$(BUILD_ROOT)\Src\Graphite\FwOnly
TTFUTIL_LIB=$(BUILD_ROOT)\Src\Graphite\TtfUtil
PARSE_XML_LIB=$(BUILD_ROOT)\Lib

# NB_XML=$(BUILD_ROOT)\src\NoteBk\XML

# Set the USER_INCLUDE environment variable.
UI=$(WP_SRC);$(RN_SRC);$(WP_RES);$(RN_RES);$(GENERIC_SRC);$(AFCORE_SRC);$(AFCORE_RES);$(CELLAR_LIB_SRC);$(CELLAR_SRC);$(WIDGETS_SRC);$(VIEW_LIB_SRC);$(LANG_LIB_SRC);$(HTMLHELP_LIB);$(TTFUTIL_LIB);$(GRFW_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

#XML_INC=$(NB_XML)

#CL_OPTS=$(CL_OPTS) /showIncludes

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

# Turn on profiling if needed. The /profile switch is needed when making an .exe so that the /fixed
# switch is not used by default.
!IF "$(BUILD_TYPE)"=="p"
LINK_OPTS=$(LINK_OPTS) /PROFILE
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=WorldPad.rc
# DEFFILE=WorldPad.def
!INCLUDE "$(AFCORE_SRC)\AfCoreInc.mak"
LINK_LIBS= AfLib.lib Widgets.lib Generic.lib xmlparse.lib uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib odbc32.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_VW=\
	$(INT_DIR)\autopch\VwUndo.obj\

# We need ModuleEntry here because the version in Generic.lib was compiled without /DEXE_MODULE=1
# and will not give the right functions for an exe.
OBJ_WP=\
	$(INT_DIR)\autopch\WpStylesheet.obj\
	$(INT_DIR)\autopch\WpDa.obj\
	$(INT_DIR)\autopch\WpXml.obj\
	$(INT_DIR)\autopch\WpXslt.obj\
	$(INT_DIR)\autopch\explicit_instantiations.obj\
	$(INT_DIR)\autopch\GrUtil.obj\
	$(INT_DIR)\autopch\WpWrSysDlg.obj\
	$(INT_DIR)\autopch\WpOptionsDlg.obj\
	$(INT_DIR)\autopch\WpDocDlg.obj\
	$(INT_DIR)\autopch\WorldPad.obj\
	$(INT_DIR)\autopch\AfFwTool.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\UtilSil2.obj\
	$(INT_DIR)\autopch\FwXml.obj\


# ENHANCE JohnT: this is basically AfCoreInc.mak as of 21 June 2000,
# except we leave out the database access-related classes.
# We need to find a better way to isolate database support in AppCore.
LINK_LIBS=HtmlHelp.lib Version.lib $(LINK_LIBS)


# will want something like this one day	$(INT_DIR)\autopch\TlsOptDlg.obj\

OBJ_ALL=$(OBJ_VW) $(OBJ_WP)

IDL_MAIN=$(COM_OUT_DIR)\WorldPadTlb.idl

SQL_MAIN=WorldPad.sql


# not needed for WP? SQO_MAIN=WorldPad.sqo


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.rc
$(INT_DIR)\$(RCFILE:.rc=.res): $(WP_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(WP_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(WP_RES)\*.rc


# === Rules ===
PCHNAME=WorldPad

ARG_SRCDIR=$(WP_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(WP_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(AFCORE_RES)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(VIEW_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(WIDGETS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GRAPHITE_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Targets ===

# === Special dependencies ===
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\aflib.lib
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Generic.lib
$(OUT_DIR)\$(BUILD_PRODUCT).$(BUILD_EXTENSION): $(BUILD_ROOT)\Lib\$(BUILD_CONFIG)\Widgets.lib
