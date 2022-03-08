# Copyright (c) 2007-2020 SIL International
# This software is licensed under the LGPL, version 2.1 or later
# (http://www.gnu.org/licenses/lgpl-2.1.html)
#
#	FieldWorks Makefile
#
#	MarkS - 2007-08-08

FW_PACKAGE_DEBUG ?= false
BUILD_TOOL = msbuild
# Verbosity: normal or detailed
MSBUILD_ARGS ?= -verbosity:normal
ICU_VERSION = 70
BUILD_ROOT = $(shell pwd)
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = FieldWorks
include $(BUILD_ROOT)/Bld/_init.mak.lnx
SHELL=/bin/bash
BITS := $(shell test `arch` = x86_64 && echo 64 || echo 32)
PLATFORM := $(shell test `arch` = x86_64 && echo x64 || echo x86)
INSTALLATION_PREFIX ?= /usr
BUILD_CONFIG ?= Release

all:

externaltargets: \
	Win32Base \
	COM-all \
	COM-install \
	Win32More \
	installable-COM-all \

externaltargets-test: \
	Win32Base-check \
	COM-check \
	Win32More-check \


# This build item isn't run on a normal build.
generate-strings:
	(cd $(SRC)/Language/ && $(BUILD_ROOT)/Bin/make-strings.sh Language.rc > $(BUILD_ROOT)/DistFiles/strings-en.txt)
	(cd $(SRC)/Generic/ && $(BUILD_ROOT)/Bin/make-strings.sh Generic.rc >> $(BUILD_ROOT)/DistFiles/strings-en.txt)
	(cd $(SRC)/Kernel/ && $(BUILD_ROOT)/Bin/make-strings.sh FwKernel.rc >> $(BUILD_ROOT)/DistFiles/strings-en.txt)
	(cd $(SRC)/views/ && $(BUILD_ROOT)/Bin/make-strings.sh Views.rc >> $(BUILD_ROOT)/DistFiles/strings-en.txt)
	(cd $(SRC)/AppCore/ && C_INCLUDE_PATH=./Res $(BUILD_ROOT)/Bin/make-strings.sh Res/AfApp.rc >> $(BUILD_ROOT)/DistFiles/strings-en.txt)


clean: \
	COM-clean \
	COM-uninstall \
	COM-distclean \
	COM-autodegen \
	installable-COM-clean \
	Cellar-clean \
	Generic-clean \
	views-clean \
	AppCore-clean \
	Kernel-clean \
	Win32Base-clean \
	Win32More-clean \
	Language-clean \
	views-Test-clean \
	kernel-Test-clean \
	language-Test-clean \
	ComponentsMap-clean \
	generic-Test-clean \
	tlbs-clean \
	l10n-clean \
	manpage-clean \

idl: idl-do
# extracting the GUIDs is now done with a msbuild target, please run 'msbuild /t:generateLinuxIdlFiles'

idl-do:
	$(MAKE) -C$(SRC)/Common/ViewsInterfaces -f IDLMakefile all
	$(MAKE) -C$(SRC)/Common/FwKernelInterfaces -f IDLMakefile all

idl-clean:
	$(MAKE) -C$(SRC)/Common/ViewsInterfaces -f IDLMakefile clean
	$(MAKE) -C$(SRC)/Common/FwKernelInterfaces -f IDLMakefile clean

fieldworks-flex.1.gz: DistFiles/Linux/fieldworks-flex.1.xml
	docbook2x-man DistFiles/Linux/fieldworks-flex.1.xml
	gzip fieldworks-flex.1
unicodechareditor.1.gz: DistFiles/Linux/unicodechareditor.1.xml
	docbook2x-man DistFiles/Linux/unicodechareditor.1.xml
	gzip unicodechareditor.1
manpage-clean:
	rm -f fieldworks-flex.1.gz unicodechareditor.1.gz

install-tree-fdo:
	# Create directories
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/Firefox-Linux$(BITS)
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks
	install -d $(DESTDIR)/var/lib/fieldworks
	# Install libraries and their support files
	install -m 644 $(OUT_DIR)/*.{dll*,so} $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install -m 644 $(OUT_DIR)/Firefox-Linux$(BITS)/*.* $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/Firefox-Linux$(BITS)
	install -m 644 $(OUT_DIR)/{*.compmap,components.map} $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	# Install executables and scripts
	install Lib/linux/setup-user $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks/
	# Install content and plug-ins
	# For reasons I don't understand we need strings-en.txt otherwise the tests fail when run from xbuild
	install -m 644 DistFiles/strings-en.txt $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks
	install -m 644 DistFiles/*.{xml,map,tec,dtd} $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks
	cp -pdr DistFiles/{Ethnologue,Icu70,Templates} $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks
	# Remove unwanted items
	case $(ARCH) in i686) OTHERWIDTH=64;; x86_64) OTHERWIDTH=32;; esac; \
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/lib{xample,patr}$$OTHERWIDTH.so
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/lib{ecdriver,IcuConvEC,IcuRegexEC,IcuTranslitEC,PyScriptEncConverter}*.so
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/{AIGuesserEC,CcEC,IcuEC,PerlExpressionEC,PyScriptEC,SilEncConverters40,ECInterfaces}.dll{,.config}
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/libTECkit{,_Compiler}*.so

install-tree: fieldworks-flex.1.gz unicodechareditor.1.gz install-tree-fdo
	if [ "$(FW_PACKAGE_DEBUG)" = "true" ]; then find "$(BUILD_ROOT)" "$(DESTDIR)"; fi
	# Create directories
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/bin
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/share/man/man1
	install -d $(DESTDIR)/etc/profile.d
	# Install libraries and their support files
	install -m 644 DistFiles/*.{dll*,so} $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install -m 644 DistFiles/Linux/*.so $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install -m 644 $(OUT_DIR)/*.{dll*,so} $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	# Install executables and scripts
	install $(OUT_DIR)/*.exe $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install DistFiles/*.exe $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install Bin/ReadKey.exe $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install Bin/WriteKey.exe $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install Lib/linux/fieldworks-flex $(DESTDIR)$(INSTALLATION_PREFIX)/bin
	install Lib/linux/fieldworks-lcmbrowser $(DESTDIR)$(INSTALLATION_PREFIX)/bin
	install Lib/linux/unicodechareditor $(DESTDIR)$(INSTALLATION_PREFIX)/bin
	install Lib/linux/{run-app,extract-userws.xsl,launch-xchm} $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install -m 644 environ{,-xulrunner} $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install -m 644 Lib/linux/fieldworks.sh $(DESTDIR)/etc/profile.d
	# Install content and plug-ins
	install -m 644 DistFiles/*.{txt,reg} $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks
	cp -pdr DistFiles/{"Editorial Checks",EncodingConverters} $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks
	cp -pdr DistFiles/{Helps,Fonts,Graphite,Keyboards,"Language Explorer",Parts} $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks
	# Install man pages
	install -m 644 *.1.gz $(DESTDIR)$(INSTALLATION_PREFIX)/share/man/man1
	# Handle the Converter files
	mv $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/{Converter.exe,ConvertLib.dll,ConverterConsole.exe} $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks
	# Remove unwanted items
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/DevComponents.DotNetBar.dll
	case $(ARCH) in i686) OTHERWIDTH=64;; x86_64) OTHERWIDTH=32;; esac; \
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/lib{xample,patr}$$OTHERWIDTH.so
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/lib{ecdriver,IcuConvEC,IcuRegexEC,IcuTranslitEC,PyScriptEncConverter}*.so
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/{AIGuesserEC,CcEC,IcuEC,PerlExpressionEC,PyScriptEC,SilEncConverters40,ECInterfaces}.dll{,.config}
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/libTECkit{,_Compiler}*.so
	rm -Rf $(DESTDIR)$(INSTALLATION_PREFIX)/lib/share/fieldworks/Icu70/tools
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/share/fieldworks/Icu70/Keyboards
	# Windows dll and exe files.
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/{aspell-15,iconv,libglib-2.0-0,libglib-2.0-0-vs8,libgmodule-2.0-0,libgmodule-2.0-0-vs8,TextFormStorage,unicows,wrtXML,xample32,xample64,XceedZip,xmlparse_u}.dll
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/{SFconv,TxtConv,vs_piaredist,ZEdit}.exe
	# Remove localization data that came from "DistFiles/Language Explorer", which is handled separately by l10n-install
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks/Language\ Explorer/Configuration/strings-*.xml
	# Except we still want English :-) (this also seems like a sensible place to install English .xlf files for common libraries)
	mkdir -p "$(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks/CommonLocalizations"
	install -m 644 DistFiles/CommonLocalizations/*.en.xlf $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks/CommonLocalizations
	install -m 644 DistFiles/Language\ Explorer/Configuration/strings-en.xml $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks/Language\ Explorer/Configuration

install-menuentries:
	# Add to Applications menu
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/share/pixmaps
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/share/icons/hicolor/64x64/apps
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/share/icons/hicolor/128x128/apps
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/share/applications
	install -m 644 Src/LexText/LexTextExe/LT.png $(DESTDIR)$(INSTALLATION_PREFIX)/share/pixmaps/fieldworks-flex.png
	install -m 644 Src/LexText/LexTextExe/LT64.png $(DESTDIR)$(INSTALLATION_PREFIX)/share/icons/hicolor/64x64/apps/fieldworks-flex.png
	install -m 644 Src/LexText/LexTextExe/LT128.png $(DESTDIR)$(INSTALLATION_PREFIX)/share/icons/hicolor/128x128/apps/fieldworks-flex.png
	desktop-file-install --dir $(DESTDIR)$(INSTALLATION_PREFIX)/share/applications Lib/linux/fieldworks-applications.desktop
	desktop-file-install --dir $(DESTDIR)$(INSTALLATION_PREFIX)/share/applications Lib/linux/unicodechareditor.desktop

install-packagemetadata:
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/share/appdata
	install -m 644 DistFiles/Linux/fieldworks-applications.desktop.appdata.xml $(DESTDIR)$(INSTALLATION_PREFIX)/share/appdata

install: install-tree install-menuentries l10n-install install-packagemetadata

install-package: install install-COM
	:

install-package-fdo: install-tree-fdo install-COM

uninstall: uninstall-menuentries
	rm -rf $(DESTDIR)$(INSTALLATION_PREFIX)/bin/flex $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks

uninstall-menuentries:
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/share/pixmaps/fieldworks-flex.png
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/share/applications/fieldworks-applications.desktop
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/share/icons/hicolor/64x64/apps/fieldworks-flex.png
	rm -f $(DESTDIR)$(INSTALLATION_PREFIX)/share/icons/hicolor/128x128/apps/fieldworks-flex.png

installable-COM-all:
	mkdir -p $(COM_DIR)/installer$(ARCH)
	-(cd $(COM_DIR)/installer$(ARCH) && [ ! -e Makefile ] && autoreconf -isf .. && \
		../configure --prefix=$(INSTALLATION_PREFIX)/lib/fieldworks --libdir=$(INSTALLATION_PREFIX)/lib/fieldworks)
	$(MAKE) -C$(COM_DIR)/installer$(ARCH) all

installable-COM-clean:
	$(RM) -r $(COM_DIR)/installer$(ARCH)

install-COM: installable-COM-all
	$(MAKE) -C$(COM_DIR)/installer$(ARCH) install

uninstall-COM:
	[ -e $(COM_DIR)/installer$(ARCH)/Makefile ] && \
	$(MAKE) -C$(COM_DIR)/installer$(ARCH) uninstall || true

##########


CTags-background-generation:
	echo Running ctags in the background...
	(nice -n20 /usr/bin/ctags -R --c++-types=+px --excmd=pattern --exclude=Makefile -f $(BUILD_ROOT)/tags.building $(BUILD_ROOT) $(WIN32BASE_DIR) $(WIN32MORE_DIR) $(COM_DIR) /usr/include && mv -f $(BUILD_ROOT)/tags.building $(BUILD_ROOT)/tags) &

Win32Base:
	$(MAKE) -C$(WIN32BASE_DIR) all
Win32Base-clean:
	$(MAKE) -C$(WIN32BASE_DIR) clean
Win32Base-check:
	$(MAKE) -C$(WIN32BASE_DIR) check

Win32More:
	$(MAKE) -C$(WIN32MORE_DIR) all
Win32More-clean:
	$(MAKE) -C$(WIN32MORE_DIR) clean
Win32More-check:
	$(MAKE) -C$(WIN32MORE_DIR) check

Generic-all: Generic-nodep
Generic-nodep:
	$(MAKE) -C$(SRC)/Generic all
Generic-clean:
	$(MAKE) -C$(SRC)/Generic clean
Generic-link:
	$(MAKE) -C$(SRC)/Generic link_check

DebugProcs-all: DebugProcs-nodep
DebugProcs-nodep:
	$(MAKE) -C$(SRC)/DebugProcs all
DebugProcs-clean:
	$(MAKE) -C$(SRC)/DebugProcs clean
DebugProcs-link:
	$(MAKE) -C$(SRC)/DebugProcs link_check

COM-all:
	-mkdir -p $(COM_BUILD)
	(cd $(COM_BUILD) && [ ! -e Makefile ] && autoreconf -isf .. && ../configure --prefix=`abs.py .`; true)
	REMOTE_WIN32_DEV_HOST=$(REMOTE_WIN32_DEV_HOST) $(MAKE) -C$(COM_BUILD) all
COM-install:
	$(MAKE) -C$(COM_BUILD) install
	@mkdir -p $(OUT_DIR)
	cp -pf $(COM_BUILD)/ManagedComBridge/libManagedComBridge.so $(OUT_DIR)/
COM-check:
	$(MAKE) -C$(COM_BUILD) check
COM-uninstall:
	[ -e $(COM_BUILD)/Makefile ] && \
	$(MAKE) -C$(COM_BUILD) uninstall || true
	rm -f $(OUT_DIR)/libManagedComBridge.so
COM-clean:
	[ -e $(COM_BUILD)/Makefile ] && \
	$(MAKE) -C$(COM_BUILD) clean || true
COM-distclean:
	[ -e $(COM_BUILD)/Makefile ] && \
	$(MAKE) -C$(COM_BUILD) distclean || true
COM-autodegen:
	(cd $(COM_DIR) && sh autodegen.sh)

Kernel-all: Kernel-nodep
Kernel-nodep: libFwKernel Kernel-link
libFwKernel:
	$(MAKE) -C$(SRC)/Kernel all
Kernel-componentsmap:
	$(MAKE) -C$(SRC)/Kernel ComponentsMap
Kernel-clean:
	$(MAKE) -C$(SRC)/Kernel clean
Kernel-link:
	$(MAKE) -C$(SRC)/Kernel link_check

views-all: views-nodep
views-nodep: libViews libVwGraphics views-link
libViews:
	$(MAKE) -C$(SRC)/views all
views-componentsmap:
	$(MAKE) -C$(SRC)/views ComponentsMap
views-clean:
	$(MAKE) -C$(SRC)/views clean
libVwGraphics:
	$(MAKE) -C$(SRC)/views libVwGraphics
views-link:
	$(MAKE) -C$(SRC)/views link_check

Cellar-all: Cellar-nodep
Cellar-nodep: libCellar
libCellar:
	$(MAKE) -C$(SRC)/Cellar all
Cellar-componentsmap:
	$(MAKE) -C$(SRC)/Cellar ComponentsMap
Cellar-clean:
	$(MAKE) -C$(SRC)/Cellar clean

AppCore-all: AppCore-nodep
AppCore-nodep: libAppCore
libAppCore:
	$(MAKE) -C$(SRC)/AppCore all
AppCore-clean:
	$(MAKE) -C$(SRC)/AppCore clean

Language-all: libFwKernel libViews Language-nodep
Language-nodep: libLanguage
libLanguage:
	$(MAKE) -C$(SRC)/Language all
Language-clean:
	$(MAKE) -C$(SRC)/Language clean
Language-link:
	$(MAKE) -C$(SRC)/Language link_check

unit++-all:
	-mkdir -p $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)
	([ ! -e $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)/Makefile ] && cd $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH) && autoreconf -isf .. && ../configure ; true)
	$(MAKE) -C$(BUILD_ROOT)/Lib/src/unit++/build$(ARCH) all
unit++-clean:
	([ -e $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)/Makefile ] && $(MAKE) -C$(BUILD_ROOT)/Lib/src/unit++/build$(ARCH) clean ; true)
	-rm -rf $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)

views-Test:
	$(MAKE) -C$(SRC)/views/Test all
views-Test-clean:
	$(MAKE) -C$(SRC)/views/Test clean
views-Test-check:
	$(MAKE) -C$(SRC)/views/Test check

generic-Test-all: generic-Test
generic-Test:
	$(MAKE) -C$(SRC)/Generic/Test all
generic-Test-clean:
	$(MAKE) -C$(SRC)/Generic/Test clean
generic-Test-check:
	$(MAKE) -C$(SRC)/Generic/Test check

kernel-Test:
	$(MAKE) -C$(SRC)/Kernel/Test all
kernel-Test-clean:
	$(MAKE) -C$(SRC)/Kernel/Test clean
kernel-Test-check:
	$(MAKE) -C$(SRC)/Kernel/Test check

language-Test:
	$(MAKE) -C$(SRC)/Language/Test all
language-Test-clean:
	$(MAKE) -C$(SRC)/Language/Test clean
language-Test-check:
	$(MAKE) -C$(SRC)/Language/Test check

language-Test-check:

Unit++-package: unit++-all

Unit++-clean: unit++-clean

ComponentsMap: COM-all COM-install libFwKernel libLanguage libViews libCellar DbAccess ComponentsMap-nodep

ComponentsMap-nodep:
# the info gets now added by the msbuild process.

ComponentsMap-clean:
	$(RM) $(OUT_DIR)/components.map

check-have-build-dependencies:
	$(BUILD_ROOT)/Build/Agent/install-deps --verify

# As of 2017-03-27, localize is more likely to crash running on mono 3 than to actually have a real localization problem. So try it a few times so that a random crash doesn't fail a packaging job that has been running for over an hour.
# Make the lcm artifacts dir so it is a valid path for later processing appending things like '/..'.
Fw-build-package: check-have-build-dependencies
	export LcmLocalArtifactsDir="$(BUILD_ROOT)/../liblcm/artifacts/$(BUILD_CONFIG)" \
		&& mkdir -p $$LcmLocalArtifactsDir \
		&& . environ \
		&& cd $(BUILD_ROOT)/Build \
		&& $(BUILD_TOOL) /t:refreshTargets /property:installation_prefix=$(INSTALLATION_PREFIX) $(MSBUILD_ARGS) \
		&& $(BUILD_TOOL) '/t:remakefw' /property:config=$(BUILD_CONFIG) /property:Platform=$(PLATFORM) /property:packaging=yes /property:installation_prefix=$(INSTALLATION_PREFIX) $(MSBUILD_ARGS) \
		&& ./multitry $(BUILD_TOOL) '/t:localize-binaries' /property:config=$(BUILD_CONFIG) /property:packaging=yes /property:installation_prefix=$(INSTALLATION_PREFIX) $(MSBUILD_ARGS)
	if [ "$(FW_PACKAGE_DEBUG)" = "true" ]; then find "$(BUILD_ROOT)/.."; fi

Fw-build-package-fdo: check-have-build-dependencies
	cd $(BUILD_ROOT)/Build \
		&& $(BUILD_TOOL) /t:refreshTargets /property:installation_prefix=$(INSTALLATION_PREFIX) $(MSBUILD_ARGS) \
		&& $(BUILD_TOOL) '/t:build4package-fdo' /property:config=$(BUILD_CONFIG) /property:packaging=yes /property:installation_prefix=$(INSTALLATION_PREFIX) $(MSBUILD_ARGS)

RestoreNuGetPackages:
	. environ \
		&& cd Build \
		&& $(BUILD_TOOL) /t:RestoreNuGetPackages /property:config=$(BUILD_CONFIG) \
			/property:packaging=yes /property:installation_prefix=$(INSTALLATION_PREFIX) $(MSBUILD_ARGS)

# Begin localization section

localize-source: RestoreNuGetPackages
	. environ && \
	(cd Build && $(BUILD_TOOL) /t:localize-source /property:config=$(BUILD_CONFIG) /property:packaging=yes /property:installation_prefix=$(INSTALLATION_PREFIX) $(MSBUILD_ARGS))
	# Remove symbolic links from Output - we don't want those in the source package
	find Output -type l -delete
	# Copy localization files to Localizations folder so that they survive a 'clean'
	cp -a Output Localizations/

LOCALIZATIONS := $(shell ls $(BUILD_ROOT)/Localizations/l10ns/*/messages.*.po | sed 's/.*messages\.\(.*\)\.po/\1/')

l10n-all:
	(cd $(BUILD_ROOT)/Build && $(BUILD_TOOL) /t:localize-binaries /property:installation_prefix=$(INSTALLATION_PREFIX) $(MSBUILD_ARGS))

l10n-clean:
	# We don't want to remove strings-en.xml
	for LOCALE in $(LOCALIZATIONS); do \
		rm -rf "$(BUILD_ROOT)/Output/{Debug,Release}/$$LOCALE" \
			"$(BUILD_ROOT)/DistFiles/CommonLocalizations/*.$$LOCALE.xlf" \
			"$(BUILD_ROOT)/DistFiles/Language Explorer/Configuration/strings-$$LOCALE.xml" ;\
	done

l10n-install:
	if [ "$(FW_PACKAGE_DEBUG)" = "true" ]; then find "$(BUILD_ROOT)/DistFiles" "$(DESTDIR)"; fi
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks
	install -d $(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks/CommonLocalizations
	install -d "$(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks/Language Explorer/Configuration"
	for LOCALE in $(LOCALIZATIONS); do \
		DESTINATION=$(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks-l10n-$${LOCALE,,} ;\
		install -d $$DESTINATION ;\
		install -m 644 Output/$(BUILD_CONFIG)/$$LOCALE/*.dll $$DESTINATION/ ;\
		install -m 644 "$(BUILD_ROOT)/DistFiles/CommonLocalizations/Palaso.$$LOCALE.xlf" $$DESTINATION/ ;\
		install -m 644 "$(BUILD_ROOT)/DistFiles/CommonLocalizations/Chorus.$$LOCALE.xlf" $$DESTINATION/ ;\
		install -m 644 "$(BUILD_ROOT)/DistFiles/Language Explorer/Configuration/strings-$$LOCALE.xml" $$DESTINATION/ ;\
		ln -sf ../fieldworks-l10n-$${LOCALE,,} $(DESTDIR)$(INSTALLATION_PREFIX)/lib/fieldworks/$$LOCALE ;\
		ln -sf ../../../lib/fieldworks-l10n-$${LOCALE,,}/Palaso.$$LOCALE.xlf "$(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks/CommonLocalizations/Palaso.$$LOCALE.xlf" ;\
		ln -sf ../../../lib/fieldworks-l10n-$${LOCALE,,}/Chorus.$$LOCALE.xlf "$(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks/CommonLocalizations/Chorus.$$LOCALE.xlf" ;\
		ln -sf ../../../../lib/fieldworks-l10n-$${LOCALE,,}/strings-$$LOCALE.xml "$(DESTDIR)$(INSTALLATION_PREFIX)/share/fieldworks/Language Explorer/Configuration/strings-$$LOCALE.xml" ;\
	done

# End localization section
