# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=testLanguage
BUILD_EXTENSION=exe
BUILD_REGSVR=0

DEFS=$(DEFS) /DGR_FW

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

UNITPP_INC=$(BUILD_ROOT)\Include\unit++
LANGUAGE_SRC=$(BUILD_ROOT)\Src\Language
LANGUAGETEST_SRC=$(BUILD_ROOT)\Src\Language\Test
GENERIC_SRC=$(BUILD_ROOT)\Src\Generic
APPCORE_SRC=$(BUILD_ROOT)\Src\AppCore
CELLAR_SRC=$(BUILD_ROOT)\Src\Cellar
GRUTIL_LIB=$(BUILD_ROOT)\Src\Graphite\lib
GRFW_SRC=$(BUILD_ROOT)\Src\Graphite\FwOnly
VIEWS_LIB=$(BUILD_ROOT)\Src\Views\lib

# Set the USER_INCLUDE environment variable.
UI=$(UNITPP_INC);$(LANGUAGETEST_SRC);$(LANGUAGE_SRC);$(GENERIC_SRC);$(APPCORE_SRC);$(CELLAR_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE);$(GRUTIL_LIB);$(VIEWS_LIB);$(GRFW_SRC)
!ELSE
USER_INCLUDE=$(UI);$(GRUTIL_LIB);$(VIEWS_LIB);$(GRFW_SRC)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

RCFILE=Language.rc

LINK_OPTS=$(LINK_OPTS:/subsystem:windows=/subsystem:console) /LIBPATH:"$(BUILD_ROOT)\Lib\$(BUILD_CONFIG)"
CPPUNIT_LIBS=unit++.lib
LINK_LIBS=$(CPPUNIT_LIBS) Generic.lib Usp10.lib xmlparse.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_LANGUAGETESTSUITE=\
	$(INT_DIR)\genpch\testLanguage.obj\
	$(INT_DIR)\genpch\Collection.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgWritingSystem.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgCollation.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\UniscribeEngine.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\UniscribeSegment.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\RomRenderEngine.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\RomRenderSegment.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgSimpleEngines.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgIcuCharPropEngine.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgIcuCollator.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgWritingSystemFactory.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgFontManager.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgUnicodeCollater.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgInputMethodEditor.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\ModuleEntry.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\FwStyledText.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\WriteXml.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgTsDataObject.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LgTsStringPlus.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\LangDef.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\usepch\TextProps1.obj\
	$(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\autopch\FwXml.obj\

OBJ_GRUTIL=\
	$(INT_DIR)\autopch\GrUtil.obj\

OBJ_ALL=$(OBJ_LANGUAGETESTSUITE) $(OBJ_GRUTIL) $(OBJ_CELLAR)

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=testLanguage

ARG_SRCDIR=$(LANGUAGETEST_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GRUTIL_LIB)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Rules ===


# === Custom Targets ===

COLLECT=$(BUILD_ROOT)\Bin\gawk.exe -f $(BUILD_ROOT)\Bin\CollectUnit++Tests.awk

$(INT_DIR)\genpch\Collection.obj: $(LANGUAGETEST_SRC)\Collection.cpp

$(LANGUAGETEST_SRC)\Collection.cpp: $(LANGUAGETEST_SRC)\testLanguage.h\
 $(LANGUAGETEST_SRC)\TestRegexMatcher.h\
 $(LANGUAGETEST_SRC)\TestLgCollatingEngine.h\
 $(LANGUAGETEST_SRC)\TestLgCollation.h\
 $(LANGUAGETEST_SRC)\TestLgWritingSystem.h\
 $(LANGUAGETEST_SRC)\TestLgWritingSystemFactory.h\
 $(LANGUAGETEST_SRC)\TestLgWritingSystemFactoryBuilder.h\
 $(LANGUAGETEST_SRC)\TestLgIcuCharPropEngine.h\
 $(LANGUAGETEST_SRC)\TestLgIcuCollator.h\
 $(LANGUAGETEST_SRC)\TestLgFontManager.h\
 $(LANGUAGETEST_SRC)\TestLgTsDataObject.h\
 $(LANGUAGETEST_SRC)\TestLgTsStringPlusWss.h\
 $(LANGUAGETEST_SRC)\TestUniscribeEngine.h\
 $(LANGUAGETEST_SRC)\TestRomRenderEngine.h\
 $(LANGUAGETEST_SRC)\RenderEngineTestBase.h\
 $(LANGUAGETEST_SRC)\TestLangDef.h
	$(DISPLAY) Collecting tests for $(BUILD_PRODUCT).$(BUILD_EXTENSION)
	$(COLLECT) $** >$(LANGUAGETEST_SRC)\Collection.cpp

$(INT_DIR)\Language.res: $(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\Language.res
	copy $(BUILD_ROOT)\Obj\$(BUILD_CONFIG)\Language\Language.res $(INT_DIR)\Language.res >nul
