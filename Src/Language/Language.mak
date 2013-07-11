# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=Language
BUILD_EXTENSION=dll
BUILD_REGSVR=1

# LANG_XML=$(BUILD_ROOT)\src\Services\language\XML

DEFS=$(DEFS) /DGR_FW /D_MERGE_PROXYSTUB /I"$(COM_OUT_DIR)" /I"$(COM_OUT_DIR_RAW)"

LANG_SRC=$(BUILD_ROOT)\Src\Language
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
APPCORE_SRC=$(BUILD_ROOT)\Src\AppCore
TEXT_SRC=$(BUILD_ROOT)\Src\Text
CELLAR_SRC=$(BUILD_ROOT)\Src\Cellar
GRUTIL_LIB=$(BUILD_ROOT)\Src\Graphite\lib
TTFUTIL_LIB=$(BUILD_ROOT)\Src\Graphite\TtfUtil
VIEWS_LIB=$(BUILD_ROOT)\Src\Views\lib
GRFW_SRC=$(BUILD_ROOT)\Src\Graphite\FwOnly
# FWUTILS_SRC=$(BUILD_ROOT)\src\FWUtils

# Set the USER_INCLUDE environment variable. Make sure Lang is first, as we want
# to get the Main.h from there, not any of the others (e.g., in Views)
UI=$(LANG_SRC);$(GENERIC_SRC);$(APPCORE_SRC);$(TEXT_SRC);$(CELLAR_SRC)

KERNEL_SRC=$(BUILD_ROOT)\src\Kernel
UI=$(UI);$(KERNEL_SRC)


!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE);$(GRUTIL_LIB);$(TTFUTIL_LIB);$(VIEWS_LIB);$(GRFW_SRC)
!ELSE
USER_INCLUDE=$(UI);$(GRUTIL_LIB);$(TTFUTIL_LIB);$(VIEWS_LIB);$(GRFW_SRC)
!ENDIF

# XML_INC=$(CELLAR_XML);$(LANG_XML)

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=Language.rc
DEFFILE=Language.def
# add Usp10.lib for Uniscribe.
!IF "$(BUILD_TYPE)"=="d" || "$(BUILD_TYPE)"=="b"
LINK_LIBS=Generic.lib Usp10.lib xmlparse.lib $(LINK_LIBS)
!ELSE
LINK_LIBS=Generic.lib Usp10.lib xmlparse.lib $(LINK_LIBS)
!ENDIF

# === Object Lists ===

OBJ_LANG=\
	$(INT_DIR)\genpch\RegexMatcherWrapper.obj\
	$(INT_DIR)\autopch\LgIcuWrappers.obj\
	$(INT_DIR)\autopch\UniscribeEngine.obj\
	$(INT_DIR)\autopch\UniscribeSegment.obj\
	$(INT_DIR)\autopch\RomRenderEngine.obj\
	$(INT_DIR)\autopch\RomRenderSegment.obj\
	$(INT_DIR)\autopch\LgSimpleEngines.obj\
	$(INT_DIR)\autopch\LgIcuCharPropEngine.obj\
	$(INT_DIR)\autopch\LgUnicodeCollater.obj\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\FwStyledText.obj\
	$(INT_DIR)\autopch\WriteXml.obj\
	$(INT_DIR)\usepch\TextProps1.obj\
	$(INT_DIR)\autopch\FwXml.obj\
	$(INT_DIR)\autopch\dlldatax.obj\


OBJ_GRUTIL=\
##	$(INT_DIR)\autopch\TtfUtil.obj\
	$(INT_DIR)\autopch\GrUtil.obj\


IDL_MAIN=$(COM_OUT_DIR)\LanguageTlb.idl

PS_MAIN=LanguagePs

OBJ_ALL= $(OBJ_LANG) $(OBJ_GRUTIL)

OBJECTS_IDH=$(COM_INT_DIR)\Objects.idh

# ? OBJECTS_H=$(COM_INT_DIR)\Objects.h

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=main

ARG_SRCDIR=$(CELLAR_XML)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(LANG_XML)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(LANG_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(APPCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(FWUTILS_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GRUTIL_LIB)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

## ARG_SRCDIR=$(TTFUTIL_LIB)
## !INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===
$(OBJ_ALL): $(OBJECTS_H)

$(COM_OUT_DIR)\LanguageTlb.tlb: $(COM_OUT_DIR)\LanguageTlb.idl

$(COM_OUT_DIR)\LanguageTlb.idl: $(LANG_SRC)\Render.idh

# === Custom Targets ===
