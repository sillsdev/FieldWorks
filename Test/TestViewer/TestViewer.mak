# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=TestViewer
BUILD_EXTENSION=exe
BUILD_REGSVR=1

# EXE_MODULE is defined, to get correct ModuleEntry sections.
DEFS=$(DEFS) /DEXE_MODULE=1

VIEWS_LIB_SRC=$(BUILD_ROOT)\Src\Views\Lib
TESTBASE_SRC=$(BUILD_ROOT)\Test
WP_SRC=$(BUILD_ROOT)\Test\TestViewer
WP_RES=$(BUILD_ROOT)\Test\TestViewer\Res
VIEWCLASS_SRC=$(BUILD_ROOT)\Test\TestViewer\ViewClasses
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
AFCORE_SRC=$(BUILD_ROOT)\Src\AppCore
AFCORE_RES=$(BUILD_ROOT)\Src\AppCore\Res
CELLAR_SRC=$(BUILD_ROOT)\src\Cellar\Lib
WIDGETS_SRC=$(BUILD_ROOT)\src\Widgets
HTMLHELP_LIB=$(BUILD_ROOT)\src\lib
HELP_DIR=$(BUILD_ROOT)\help

# NB_XML=$(BUILD_ROOT)\src\NoteBk\XML

# Set the USER_INCLUDE environment variable.
UI=$(VIEWCLASS_SRC);$(WP_SRC);$(VIEWS_LIB_SRC);$(TESTBASE_SRC);$(RN_SRC);$(WP_RES);$(RN_RES);$(GENERIC_SRC);$(AFCORE_SRC);$(AFCORE_RES);$(CELLAR_SRC);$(WIDGETS_SRC);$(HTMLHELP_LIB)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

#XML_INC=$(NB_XML)


!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(BUILD_ROOT)\Output\Common;$(PATH)

RCFILE=TestViewer.rc
# DEFFILE=TestViewer.def
LINK_LIBS=uuid.lib advapi32.lib kernel32.lib ole32.lib oleaut32.lib $(LINK_LIBS)

# === Object Lists ===

!INCLUDE "$(GENERIC_SRC)\GenericInc.mak"

#!INCLUDE "$(AFCORE_SRC)\AfCoreInc.mak"

# not using data entry framework!INCLUDE "$(AFCORE_SRC)\AfDeCoreInc.mak"

!INCLUDE "$(WIDGETS_SRC)\WidgetsInc.mak"

# not using filters !INCLUDE "$(FWFLTRCORE_SRC)\FWFilterInc.mak"

OBJ_WP=\
	$(INT_DIR)\autopch\WpDa.obj\
	$(INT_DIR)\autopch\explicit_instantiations.obj\
	$(INT_DIR)\autopch\AfVwWnd.obj\
	$(INT_DIR)\autopch\RnDialog.obj\
	$(INT_DIR)\autopch\FmtParaDlg.obj\
	$(INT_DIR)\autopch\UiColor.obj\
	$(INT_DIR)\autopch\FmtBdrDlg.obj\
	$(INT_DIR)\autopch\FmtFntDlg.obj\
	$(INT_DIR)\autopch\FmtBulNumDlg.obj\
	$(INT_DIR)\autopch\FmtGenDlg.obj\
	$(INT_DIR)\autopch\FilPgSetDlg.obj\
	$(INT_DIR)\autopch\VwBaseVc.obj\
	$(INT_DIR)\autopch\StVc.obj\
	$(INT_DIR)\autopch\VwGraphics.obj\
	$(INT_DIR)\autopch\SilTestSite.obj\
	$(INT_DIR)\autopch\MacroBase.obj\
	$(INT_DIR)\autopch\TestVwRoot.obj\
	$(INT_DIR)\autopch\TestScriptDlg.obj\
	$(INT_DIR)\autopch\TestViewer.obj\

# Todo Johnt: this is basicallly AfCorInc.mak as of 21 June 2000,
# except we leave out the database access-related classes.
# We need to find a better way to isolate database support in AppCore.
LINK_LIBS=HtmlHelp.lib Version.lib $(LINK_LIBS)

OBJ_AFCORE=\
	$(INT_DIR)\autopch\AfFrameWnd.obj\
	$(INT_DIR)\autopch\AfMainWnd.obj\
	$(INT_DIR)\autopch\AfBars.obj\
	$(INT_DIR)\autopch\AfCmd.obj\
	$(INT_DIR)\autopch\AfApp.obj\
	$(INT_DIR)\autopch\AfWnd.obj\
	$(INT_DIR)\autopch\AfMenuMgr.obj\
	$(INT_DIR)\autopch\AfGfx.obj\
	$(INT_DIR)\autopch\AfDialog.obj\
	$(INT_DIR)\autopch\AfContextHelp.obj\
	$(INT_DIR)\autopch\AfSplitter.obj\
	$(INT_DIR)\autopch\VwBaseDataAccess.obj\
	$(INT_DIR)\autopch\VwCacheDa.obj\
	$(INT_DIR)\autopch\AfTagOverlay.obj\
	$(INT_DIR)\autopch\PossChsrDlg.obj\
	$(INT_DIR)\autopch\AfColorTable.obj\

# will want something like this one day	$(INT_DIR)\autopch\TlsOptDlg.obj\

OBJ_ALL=$(OBJ_WP) $(OBJ_AFCORE) $(OBJ_GENERIC) $(OBJ_WIDGETS)


SQL_MAIN=TestViewer.sql


# not needed for WP? SQO_MAIN=WorldPad.sqo


# === Targets ===
BUILD_TARGETS=$(BUILD_TARGETS)

!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(AFCORE_RES)\*.rc
$(INT_DIR)\$(RCFILE:.rc=.res): $(WP_RES)\*.bmp
$(INT_DIR)\$(RCFILE:.rc=.res): $(WP_RES)\*.h
$(INT_DIR)\$(RCFILE:.rc=.res): $(WP_RES)\*.rc



# === Rules ===
PCHNAME=TestViewer

ARG_SRCDIR=$(VIEWS_LIB_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(TESTBASE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

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

ARG_SRCDIR=$(WIDGETS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(VIEWCLASS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Targets ===
