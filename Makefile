#
#	FieldWorks Makefile
#
#	MarkS - 2007-08-08
#
# SIL FieldWorks
# Copyright (C) 2007 SIL International
#
# This library is free software; you can redistribute it and/or
# modify it under the terms of the GNU Lesser General Public
# License as published by the Free Software Foundation; either
# version 2.1 of the License, or (at your option) any later version.
#
# This library is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
# Lesser General Public License for more details.
#
# You should have received a copy of the GNU Lesser General Public
# License along with this library; if not, write to the Free Software
# Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
#
# http://www.gnu.org/licenses/lgpl.html
#

BUILD_ROOT = $(shell pwd)
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = FieldWorks
include $(BUILD_ROOT)/Bld/_init.mak.lnx
SHELL=/bin/bash

all: tlbs-copy teckit alltargets

# This is to facilitate easier use of CsProj files.
linktoOutputDebug:
	@-mkdir -p Output/
	@ln -sf $(OUT_DIR) Output/Debug
	@ln -sf $(OUT_DIR)/../XMI Output/XMI
	@ln -sf $(OUT_DIR)/../Common Output/Common


externaltargets: \
	Win32Base \
	COM-all \
	COM-install \
	Win32More \
	RegisterServer \
	RegisterServer-install \
	ManagedComBridge-all \

nativetargets: \
	externaltargets \
	Generic-all \
	generic-Test \
	libAppCore \
	Win32Base-test-build \
	libFwKernel \
	Kernel-link \
	libCellar \
	libGraphiteTlb \
	libLanguage \
	libViews \
	libVwGraphics \
	views-link \
	views-Test \
	kernel-Test \
	language-Test \

alltargets: \
	nativetargets \
	FwResources \
	Utilities-BasicUtils \
	common-COMInterfaces \
	common-Utils \
	Utilities-MessageBoxExLib \
	Utilities-XMLUtils \
	common-FwUtils \
	common-Controls-Design \
	common-ScrUtilsInterfaces\
	common-ScriptureUtils \
	common-Controls-FwControls \
	Utilities-Reporting \
	FDO \
	XCore-Interfaces \
	common-UIAdapterInterfaces \
	common-SimpleRootSite \
	common-RootSite \
	common-Framework \
	common-Widgets \
	common-Filters \
	XCore \
	FwCoreDlgsGTK \
	FwCoreDlgGTKWidgets \
	FwCoreDlgs-FwCoreDlgControls \
	FwCoreDlgs-FwCoreDlgControlsGTK \
	FwCoreDlgs \
	DbAccess \
	LangInst \
	ComponentsMap \
	IcuDataFiles \
	install-strings \

test: \
	views-Test-check \
	kernel-Test-check \
	language-Test-check \
	Win32Base-test-run \
	generic-Test-check \

# COM-check currently fails FWNX-65 FIXME	COM-check \

test-interactive: \
	FwCoreDlgs-SimpleTest \
	FwCoreDlgs-SimpleTest-check \

test-database: \
	DbAccessFirebird-check \

# Make copies of pre-generated files in Common.
tlbs-copy:
	@-mkdir -p $(COM_OUT_DIR)
	-cp -rf $(BUILD_ROOT)/Lib/linux/Common/* $(COM_OUT_DIR)/

tlbs-clean:
	$(RM) -rf $(COM_OUT_DIR)

# ICU on Linux looks for files (like unorm.icu uprops.icu in
# $(BUILD_ROOT)/DistFiles/Icu40/icudt40l/icudt40l/icudt40l
# I think we need to move the *.icu into a sub dir
# currently copying incase anything trys to access *.icu in the first
# icudt40l directory.icudt
IcuDataFiles:
	(cd $(BUILD_ROOT)/DistFiles/Icu40 && unzip -u ../Icu40.zip)
	-(cd $(BUILD_ROOT)/DistFiles/Icu40/icudt40l && mkdir icudt40l)
	-(cd $(BUILD_ROOT)/DistFiles/Icu40/icudt40l && cp *.icu icudt40l)


# This build item isn't run on a normal build.
generate-strings:
	(cd $(BUILD_ROOT)/Src/Language/ && $(BUILD_ROOT)/Bin/make-strings.sh Language.rc > $(BUILD_ROOT)/DistFiles/strings-en.txt)
	(cd $(BUILD_ROOT)/Src/Generic/ && $(BUILD_ROOT)/Bin/make-strings.sh Generic.rc >> $(BUILD_ROOT)/DistFiles/strings-en.txt)
	(cd $(BUILD_ROOT)/Src/Kernel/ && $(BUILD_ROOT)/Bin/make-strings.sh FwKernel.rc >> $(BUILD_ROOT)/DistFiles/strings-en.txt)
	(cd $(BUILD_ROOT)/Src/views/ && $(BUILD_ROOT)/Bin/make-strings.sh Views.rc >> $(BUILD_ROOT)/DistFiles/strings-en.txt)
	(cd $(BUILD_ROOT)/Src/AppCore/ && C_INCLUDE_PATH=./Res $(BUILD_ROOT)/Bin/make-strings.sh Res/AfApp.rc >> $(BUILD_ROOT)/DistFiles/strings-en.txt)

# now done in NAnt
install-strings:
	cp -f $(BUILD_ROOT)/DistFiles/strings-en.txt $(OUT_DIR)/strings-en.txt

# setup current sets up the mono registry necessary to run certain program
# This is now done in NAnt
setup:
	(cd $(BUILD_ROOT)/Bld && ../Bin/nant/bin/nant build setupRegistry icudlls)


clean: \
	ManagedComBridge-clean \
	COM-clean \
	COM-uninstall \
	COM-distclean \
	COM-autodegen \
	Cellar-clean \
	Generic-clean \
	views-clean \
	AppCore-clean \
	Kernel-clean \
	Win32Base-test-clean \
	Win32Base-clean \
	Win32More-clean \
	RegisterServer-clean \
	RegisterServer-uninstall \
	Graphite-GrEngine-clean \
	Language-clean \
	views-Test-clean \
	kernel-Test-clean \
	language-Test-clean \
	common-COMInterfaces-clean \
	common-SimpleRootSite-clean \
	common-RootSite-clean \
	common-Framework-clean \
	common-Widgets-clean \
	common-Utils-clean \
	common-FwUtils-clean \
	common-Filters-clean \
	common-UIAdapterInterfaces-clean \
	common-ScriptureUtils-clean \
	common-ScrUtilsInterfaces-clean \
	FwResources-clean \
	Utilities-BasicUtils-clean \
	Utilities-MessageBoxExLib-clean \
	Utilities-XMLUtils-clean \
	common-Controls-FwControls-clean \
	common-Controls-Design-clean \
	Utilities-Reporting-clean \
	FDO-clean \
	FwCoreDlgsGTK-clean \
	FwCoreDlgGTKWidgets-clean \
	FwCoreDlgs-FwCoreDlgControls-clean \
	FwCoreDlgs-FwCoreDlgControlsGTK-clean \
	FwCoreDlgs-clean \
	DbAccess-clean \
	XCore-Interfaces-clean \
	XCore-clean \
	LangInst-clean \
	ComponentsMap-clean \
	generic-Test-clean \
	tlbs-clean \
	teckit-clean \

# IDLImp is a C# app, so there is no reason to re-create that during our build.
# We should be able to just use the version in $(BUILD_ROOT)\Bin
tools: \
	Unit++-package \

tools-clean: \
	Unit++-clean \

idl: idl-do regen-GUIDs

idl-do:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/COMInterfaces -f IDLMakefile all

regen-GUIDs: $(BUILD_ROOT)/Lib/linux/Common/FwKernelTlb.h $(BUILD_ROOT)/Lib/linux/Common/LanguageTlb.h $(BUILD_ROOT)/Lib/linux/Common/ViewsTlb.h
	$(MAKE) tlbs-clean
	$(MAKE) tlbs-copy
	(cd $(COM_OUT_DIR)  && $(COM_DIR)/test/extract_iids.sh FwKernelTlb.h > $(BUILD_ROOT)/Src/Kernel/FwKernel_GUIDs.cpp)
	(cd $(COM_OUT_DIR)  && $(COM_DIR)/test/extract_iids.sh LanguageTlb.h > $(BUILD_ROOT)/Src/Language/Language_GUIDs.cpp)
	echo '#include "FwKernelTlb.h"' > $(BUILD_ROOT)/Src/views/Views_GUIDs.cpp
	(cd $(COM_OUT_DIR)  && $(COM_DIR)/test/extract_iids.sh ViewsTlb.h >> $(BUILD_ROOT)/Src/views/Views_GUIDs.cpp)

idl-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/COMInterfaces -f IDLMakefile clean

install-tree:
	# Create directories
	install -d $(DESTDIR)/usr/bin
	install -d $(DESTDIR)/usr/lib/fieldworks
	install -d $(DESTDIR)/usr/lib/fieldworks/icu-bin
	install -d $(DESTDIR)/usr/share/fieldworks
	install -d $(DESTDIR)/usr/share/fieldworks-movies
	install -d $(DESTDIR)/usr/share/fieldworks-examples
	install -d $(DESTDIR)/usr/lib/fieldworks/EC/Plugins
	install -m 1777 -d $(DESTDIR)/var/lib/fieldworks
	# Install libraries and their support files
	install -m 644 $(OUT_DIR)/*.so $(DESTDIR)/usr/lib/fieldworks
	install -m 644 $(OUT_DIR)/*.{dll,dll.config,mdb} $(DESTDIR)/usr/lib/fieldworks
	install -m 644 environ $(OUT_DIR)/{*.compmap,components.map} $(DESTDIR)/usr/lib/fieldworks
	install -m 644 DistFiles/*.so $(DESTDIR)/usr/lib/fieldworks
	install -m 644 DistFiles/*.{dll,so,dll.config} $(DESTDIR)/usr/lib/fieldworks
	install -m 644 Lib/src/icu/install$(ARCH)/lib/lib* $(DESTDIR)/usr/lib/fieldworks
	install -m 644 $(OUT_DIR)/EC/Plugins/*.xml $(DESTDIR)/usr/lib/fieldworks/EC/Plugins
	# Install read-only configuration files
	install -m 644 $(OUT_DIR)/remoting_tcp_server.config $(DESTDIR)/usr/lib/fieldworks
	# Install executables and scripts
	install $(OUT_DIR)/*.exe $(DESTDIR)/usr/lib/fieldworks
	install DistFiles/*.exe $(DESTDIR)/usr/lib/fieldworks
	install Bin/WriteKey.exe $(DESTDIR)/usr/lib/fieldworks
	install Lib/src/icu/install$(ARCH)/bin/* $(DESTDIR)/usr/lib/fieldworks/icu-bin
	install Lib/src/icu/source/build$(ARCH)/bin/* $(DESTDIR)/usr/lib/fieldworks/icu-bin
	install Lib/linux/fieldworks-{te,flex} $(DESTDIR)/usr/bin
	install Lib/linux/{cpol-action,run-app} $(DESTDIR)/usr/lib/fieldworks
	install Lib/linux/setup-user $(DESTDIR)/usr/share/fieldworks/
	install Lib/linux/ShareFwProjects $(DESTDIR)/usr/lib/fieldworks
	install -m 644 Lib/linux/ShareFwProjects.desktop $(DESTDIR)/usr/share/fieldworks
	# Install content and plug-ins
	install -m 644 DistFiles/*.{pdf,txt,xml,map,tec,reg,dtd,rng} $(DESTDIR)/usr/share/fieldworks
	cp -dr --preserve=mode DistFiles/{"Editorial Checks",EncodingConverters,lib} $(DESTDIR)/usr/share/fieldworks
	cp -dr --preserve=mode DistFiles/{Ethnologue,Fonts,Graphite,Helps,Icu40,Keyboards,"Language Explorer",Parts,ReleaseData,SIL,Templates,"Translation Editor"} $(DESTDIR)/usr/share/fieldworks
	# Relocate items that are in separate packages
	rm -rf $(DESTDIR)/usr/share/fieldworks-movies/"Language Explorer"
	mv $(DESTDIR)/usr/share/fieldworks/"Language Explorer"/Movies $(DESTDIR)/usr/share/fieldworks-movies/"Language Explorer"
	ln -s /usr/share/fieldworks-movies/"Language Explorer" $(DESTDIR)/usr/share/fieldworks/"Language Explorer"/Movies
	rm -rf $(DESTDIR)/usr/share/fieldworks-examples/ReleaseData
	mv $(DESTDIR)/usr/share/fieldworks/ReleaseData $(DESTDIR)/usr/share/fieldworks-examples/ReleaseData
	ln -s /usr/share/fieldworks-examples/ReleaseData $(DESTDIR)/usr/share/fieldworks/ReleaseData
	# Handle the Converter files
	mv $(DESTDIR)/usr/lib/fieldworks/{Converter.exe,ConvertLib.dll,ConverterConsole.exe} $(DESTDIR)/usr/share/fieldworks
	# Remove unwanted items
	rm -f $(DESTDIR)/usr/lib/fieldworks/DevComponents.DotNetBar.dll
	case $(ARCH) in i686) OTHERWIDTH=64;; x86_64) OTHERWIDTH=32;; esac; \
	rm -f $(DESTDIR)/usr/lib/fieldworks/lib{xample,patr}$$OTHERWIDTH.so
	case $(ARCH) in i686) SUFFIX=x86_64;; x86_64) SUFFIX=x86;; esac; \
	rm -f $(DESTDIR)/usr/lib/fieldworks/libTECkit{,_Compiler}_$$SUFFIX.so
	rm -Rf $(DESTDIR)/usr/lib/share/fieldworks/Icu40/tools
	rm -f $(DESTDIR)/usr/lib/share/fieldworks/Icu40/Keyboards

install-menuentries:
	# Add to Applications menu
	install -d $(DESTDIR)/usr/share/pixmaps
	install -d $(DESTDIR)/usr/share/applications
	install -m 644 Src/LexText/LexTextExe/LT.png $(DESTDIR)/usr/share/pixmaps/fieldworks-flex.png
	install -m 644 Src/TeExe/Res/TE.png $(DESTDIR)/usr/share/pixmaps/fieldworks-te.png
	desktop-file-install --dir $(DESTDIR)/usr/share/applications Lib/linux/fieldworks-te.desktop
	desktop-file-install --dir $(DESTDIR)/usr/share/applications Lib/linux/fieldworks-flex.desktop

install: install-tree install-menuentries

install-package: install install-COM
	$(DESTDIR)/usr/lib/fieldworks/cpol-action pack

uninstall: uninstall-menuentries
	rm -rf $(DESTDIR)/usr/bin/{te,flex} $(DESTDIR)/usr/lib/fieldworks $(DESTDIR)/usr/share/fieldworks

uninstall-menuentries:
	rm -f $(DESTDIR)/usr/share/pixmaps/fieldworks-{te,flex}.png
	rm -f $(DESTDIR)/usr/share/applications/fieldworks-{te,flex}.desktop

install-COM:
	mkdir -p $(COM_DIR)/installer$(ARCH)
	(cd $(COM_DIR)/installer$(ARCH) && [ ! -e Makefile ] && autoreconf -isf .. && ../configure --prefix=/usr; true)
	$(MAKE) -C$(COM_DIR)/installer$(ARCH) install
	install -d $(DESTDIR)/usr/lib/fieldworks
	install ../COM/ManagedComBridge/build$(ARCH)/libManagedComBridge.so $(DESTDIR)/usr/lib/fieldworks

uninstall-COM:
	[ -e $(COM_DIR)/installer$(ARCH)/Makefile ] && \
	$(MAKE) -C$(COM_DIR)/installer$(ARCH) uninstall || true
	rm -rf $(COM_DIR)/installer$(ARCH)

##########


CTags-background-generation:
	echo Running ctags in the background...
	(nice -n20 /usr/bin/ctags -R --c++-types=+px --excmd=pattern --exclude=Makefile -f $(BUILD_ROOT)/tags.building $(BUILD_ROOT) $(WIN32BASE_DIR) $(WIN32MORE_DIR) $(COM_DIR) /usr/include && mv -f $(BUILD_ROOT)/tags.building $(BUILD_ROOT)/tags) &

Win32Base:
	$(MAKE) -C$(WIN32BASE_DIR)/src all
Win32Base-clean:
	$(MAKE) -C$(WIN32BASE_DIR)/src clean
Win32Base-test-build:
	$(MAKE) -C$(WIN32BASE_DIR)/test all
Win32Base-test-run:
	$(MAKE) -C$(WIN32BASE_DIR)/test run-test
Win32Base-test-clean:
	$(MAKE) -C$(WIN32BASE_DIR)/test clean

Win32More:
	$(MAKE) -C$(WIN32MORE_DIR)/src all
Win32More-clean:
	$(MAKE) -C$(WIN32MORE_DIR)/src clean

Generic-all: Generic-nodep
Generic-nodep:
	$(MAKE) -C$(BUILD_ROOT)/Src/Generic all
Generic-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Generic clean
Generic-link:
	$(MAKE) -C$(BUILD_ROOT)/Src/Generic link_check

DebugProcs-all: DebugProcs-nodep
DebugProcs-nodep:
	$(MAKE) -C$(BUILD_ROOT)/Src/DebugProcs all
DebugProcs-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/DebugProcs clean
DebugProcs-link:
	$(MAKE) -C$(BUILD_ROOT)/Src/DebugProcs link_check

ManagedComBridge-all:
	$(MAKE) -C$(COM_DIR)/ManagedComBridge all
	-mkdir -p $(OUT_DIR)
	cp -f ../COM/ManagedComBridge/build$(ARCH)/libManagedComBridge.so $(OUT_DIR)

ManagedComBridge-clean:
	$(MAKE) -C$(COM_DIR)/ManagedComBridge clean
	rm -f $(OUT_DIR)/libManagedComBridge.so

COM-all:
	-mkdir -p $(COM_DIR)/build$(ARCH)
	(cd $(COM_DIR)/build$(ARCH) && [ ! -e Makefile ] && autoreconf -isf .. && ../configure --prefix=`abs.py .`; true)
	REMOTE_WIN32_DEV_HOST=$(REMOTE_WIN32_DEV_HOST) $(MAKE) -C$(COM_DIR)/build$(ARCH) all
COM-test-componentsmap:
	$(MAKE) -C$(COM_DIR)/build$(ARCH)/test components.map
COM-install:
	$(MAKE) -C$(COM_DIR)/build$(ARCH) install
COM-check:
	$(MAKE) -C$(COM_DIR)/build$(ARCH)/test check
COM-uninstall:
	[ -e $(COM_DIR)/build$(ARCH)/Makefile ] && \
	$(MAKE) -C$(COM_DIR)/build$(ARCH) uninstall || true
COM-clean:
	[ -e $(COM_DIR)/build$(ARCH)/Makefile ] && \
	$(MAKE) -C$(COM_DIR)/build$(ARCH) clean || true
COM-distclean:
	[ -e $(COM_DIR)/build$(ARCH)/Makefile ] && \
	$(MAKE) -C$(COM_DIR)/build$(ARCH) distclean || true
COM-autodegen:
	(cd $(COM_DIR) && sh autodegen.sh)

Kernel-all: Kernel-nodep
Kernel-nodep: libFwKernel Kernel-link
libFwKernel:
	$(MAKE) -C$(BUILD_ROOT)/Src/Kernel all
Kernel-componentsmap:
	$(MAKE) -C$(BUILD_ROOT)/Src/Kernel ComponentsMap
Kernel-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Kernel clean
Kernel-link:
	$(MAKE) -C$(BUILD_ROOT)/Src/Kernel link_check

views-all: views-nodep
views-nodep: libViews libVwGraphics views-link
libViews:
	$(MAKE) -C$(BUILD_ROOT)/Src/views all
views-componentsmap:
	$(MAKE) -C$(BUILD_ROOT)/Src/views ComponentsMap
views-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/views clean
libVwGraphics:
	$(MAKE) -C$(BUILD_ROOT)/Src/views libVwGraphics
views-link:
	$(MAKE) -C$(BUILD_ROOT)/Src/views link_check

Cellar-all: Cellar-nodep
Cellar-nodep: libCellar
libCellar:
	$(MAKE) -C$(BUILD_ROOT)/Src/Cellar all
Cellar-componentsmap:
	$(MAKE) -C$(BUILD_ROOT)/Src/Cellar ComponentsMap
Cellar-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Cellar clean

AppCore-all: AppCore-nodep
AppCore-nodep: libAppCore
libAppCore:
	$(MAKE) -C$(BUILD_ROOT)/Src/AppCore all
AppCore-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/AppCore clean

RegisterServer:
	$(MAKE) -C$(COM_DIR)/src/RegisterServer
RegisterServer-install:
	$(MAKE) -C$(COM_DIR)/src/RegisterServer install
RegisterServer-uninstall:
	$(MAKE) -C$(COM_DIR)/src/RegisterServer uninstall
RegisterServer-clean:
	$(MAKE) -C$(COM_DIR)/src/RegisterServer clean

Language-all: libFwKernel libViews Language-nodep
Language-nodep: libLanguage
libLanguage:
	$(MAKE) -C$(BUILD_ROOT)/Src/Language all
Language-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Language clean
Language-link:
	$(MAKE) -C$(BUILD_ROOT)/Src/Language link_check

Graphite-GrEngine-all: Graphite-GrEngine-nodep
Graphite-GrEngine-nodep: libGraphiteTlb
libGraphiteTlb:
	$(MAKE) -C$(BUILD_ROOT)/Src/Graphite/GrEngine all
Graphite-GrEngine-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Graphite/GrEngine clean

Nant-Task-FdoGenerate:
	$(MAKE) -C$(BUILD_ROOT)/Bin/nant/src/FwTasks/FdoGenerate all

Nant-Task-FdoGenerate-clean:
	$(MAKE) -C$(BUILD_ROOT)/Bin/nant/src/FwTasks/FdoGenerate clean

unit++-all:
	-mkdir -p $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)
	([ ! -e $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)/Makefile ] && cd $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH) && autoreconf -isf .. && ../configure ; true)
	$(MAKE) -C$(BUILD_ROOT)/Lib/src/unit++/build$(ARCH) all
unit++-clean:
	([ -e $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)/Makefile ] && $(MAKE) -C$(BUILD_ROOT)/Lib/src/unit++/build$(ARCH) clean ; true)
	-rm -rf $(BUILD_ROOT)/Lib/src/unit++/build$(ARCH)

views-Test:
	$(MAKE) -C$(BUILD_ROOT)/Src/views/Test all
views-Test-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/views/Test clean
views-Test-check:
	$(MAKE) -C$(BUILD_ROOT)/Src/views/Test check

generic-Test-all: generic-Test
generic-Test:
	$(MAKE) -C$(BUILD_ROOT)/Src/Generic/Test all
generic-Test-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Generic/Test clean
generic-Test-check:
	$(MAKE) -C$(BUILD_ROOT)/Src/Generic/Test check

kernel-Test:
	$(MAKE) -C$(BUILD_ROOT)/Src/Kernel/Test all
kernel-Test-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Kernel/Test clean
kernel-Test-check:
	$(MAKE) -C$(BUILD_ROOT)/Src/Kernel/Test check

language-Test:
	$(MAKE) -C$(BUILD_ROOT)/Src/Language/Test all
language-Test-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Language/Test clean
language-Test-check:
	$(MAKE) -C$(BUILD_ROOT)/Src/Language/Test check

language-Test-check:

FwCoreDlgs-SimpleTest:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgs/SimpleTest all

FwCoreDlgs-SimpleTest-check:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgs/SimpleTest run


DbAccessFirebird-check:
	$(MAKE) -C$(BUILD_ROOT)/Src/DbAccessFirebird check

# $(MAKE) Common items
common-COMInterfaces:
	(cd $(BUILD_ROOT)/Bld && ../Bin/nant/bin/nant build COMInterfaces-nodep)
common-COMInterfaces-clean:
	(cd $(BUILD_ROOT)/Bld && ../Bin/nant/bin/nant clean COMInterfaces-nodep)

common-Utils:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Utils all
common-Utils-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Utils clean

common-FwUtils:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/FwUtils all
common-FwUtils-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/FwUtils clean

common-SimpleRootSite:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/SimpleRootSiteGtk all
common-SimpleRootSite-clean:
	$(MAKE)  -C$(BUILD_ROOT)/Src/Common/SimpleRootSiteGtk clean

common-RootSite: common-SimpleRootSite
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/RootSite all
common-RootSite-clean:
	 $(MAKE) -C$(BUILD_ROOT)/Src/Common/RootSite clean

common-Framework:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Framework all
common-Framework-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Framework clean

common-Widgets:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Controls/Widgets all
common-Widgets-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Controls/Widgets clean

common-Filters:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Filters all
common-Filters-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Filters clean

common-UIAdapterInterfaces:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/UIAdapterInterfaces all
common-UIAdapterInterfaces-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/UIAdapterInterfaces clean

common-ScriptureUtils:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/ScriptureUtils all
common-ScriptureUtils-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/ScriptureUtils clean

common-ScrUtilsInterfaces:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/ScrUtilsInterfaces all
common-ScrUtilsInterfaces-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/ScrUtilsInterfaces clean

common-Controls-FwControls:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Controls/FwControls all
common-Controls-FwControls-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Controls/FwControls clean

common-Controls-Design:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Controls/Design all
common-Controls-Design-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Common/Controls/Design clean

Utilities-BasicUtils:
	$(MAKE) -C$(BUILD_ROOT)/Src/Utilities/BasicUtils all
Utilities-BasicUtils-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Utilities/BasicUtils clean

Utilities-MessageBoxExLib:
	$(MAKE) -C$(BUILD_ROOT)/Src/Utilities/MessageBoxExLib all
Utilities-MessageBoxExLib-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Utilities/MessageBoxExLib clean

Utilities-XMLUtils:
	$(MAKE) -C$(BUILD_ROOT)/Src/Utilities/XMLUtils all
Utilities-XMLUtils-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Utilities/XMLUtils clean

Utilities-Reporting:
	$(MAKE) -C$(BUILD_ROOT)/Src/Utilities/Reporting all
Utilities-Reporting-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/Utilities/Reporting clean

FDO:
	$(MAKE) -C$(BUILD_ROOT)/Src/FDO all
FDO-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/FDO clean

FwResources:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwResources all
FwResources-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwResources clean

FwCoreDlgsGTK:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgsGTK all
FwCoreDlgsGTK-clean:
	 $(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgsGTK clean

FwCoreDlgGTKWidgets:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgGTKWidgets all
FwCoreDlgGTKWidgets-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgGTKWidgets clean

FwCoreDlgs-FwCoreDlgControls:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgs/FwCoreDlgControls all
FwCoreDlgs-FwCoreDlgControls-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgs/FwCoreDlgControls clean

FwCoreDlgs-FwCoreDlgControlsGTK:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgs/FwCoreDlgControlsGTK all
FwCoreDlgs-FwCoreDlgControlsGTK-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgs/FwCoreDlgControlsGTK clean

FwCoreDlgs:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgs all
FwCoreDlgs-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/FwCoreDlgs clean

LangInst:
	$(MAKE) -C$(BUILD_ROOT)/Src/LangInst all
LangInst-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/LangInst clean

DbAccess-all: DbAccess-nodep
DbAccess: DbAccess-nodep
DbAccess-nodep:
	$(MAKE) -C$(BUILD_ROOT)/Src/DbAccessFirebird all
DbAccess-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/DbAccessFirebird clean

XCore-Interfaces:
	$(MAKE) -C$(BUILD_ROOT)/Src/XCore/xCoreInterfaces all
XCore-Interfaces-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/XCore/xCoreInterfaces clean

XCore:
	$(MAKE) -C$(BUILD_ROOT)/Src/XCore all
XCore-clean:
	$(MAKE) -C$(BUILD_ROOT)/Src/XCore clean

IDLImp-package:
	$(MAKE) -C$(BUILD_ROOT)/Bin/src/IDLImp package
IDLImp-clean:
	$(MAKE) -C$(BUILD_ROOT)/Bin/src/IDLImp clean

Unit++-package: unit++-all

Unit++-clean: unit++-clean

# We don't want CodeGen to be built by default. This messes things up if cmcg.exe gets checked
# in accidentally. Besides we currently don't use it, so I don't see a need for building it.
CodeGen:
	$(MAKE) -C$(BUILD_ROOT)/Bin/src/CodeGen
	# Put the cmcg executable where nant is expecting it (cm.build.xml). Only do the cp if
	# the source file is newer than the target file, or if the target doesn't exist.
	if [ ! -e $(BUILD_ROOT)/Bin/cmcg.exe ] || \
		[ $(BUILD_ROOT)/Bin/src/CodeGen/CodeGen -nt $(BUILD_ROOT)/Bin/cmcg.exe ]; then \
		cp -p $(BUILD_ROOT)/Bin/src/CodeGen/CodeGen $(BUILD_ROOT)/Bin/cmcg.exe; fi

CodeGen-clean:
	$(MAKE) -C$(BUILD_ROOT)/Bin/src/CodeGen clean
	rm -f $(BUILD_ROOT)/Bin/cmcg.exe

teckit:
	if [ ! -e "$(OUT_DIR)/libTECkit_x86.so" ] || \
		[ "$(BUILD_ROOT)/DistFiles/libTECkit_x86.so" -nt "$(OUT_DIR)/libTECkit_x86.so" ]; then \
		mkdir -p $(OUT_DIR); \
		cp -p "$(BUILD_ROOT)/DistFiles/libTECkit_x86.so" "$(OUT_DIR)/libTECkit_x86.so"; fi
	if [ ! -e "$(OUT_DIR)/libTECkit_Compiler_x86.so" ] || \
		[ "$(BUILD_ROOT)/DistFiles/libTECkit_Compiler_x86.so" -nt "$(OUT_DIR)/libTECkit_Compiler_x86.so" ]; then \
		mkdir -p $(OUT_DIR); \
		cp -p "$(BUILD_ROOT)/DistFiles/libTECkit_Compiler_x86.so" "$(OUT_DIR)/libTECkit_Compiler_x86.so"; fi

teckit-clean:
	rm -f $(OUT_DIR)/libTECkit_x86.so $(OUT_DIR)/libTECkit_Compiler_x86.so

ComponentsMap: COM-all COM-install RegisterServer RegisterServer-install libFwKernel libLanguage libViews libCellar DbAccess ComponentsMap-nodep

ComponentsMap-nodep:
# the info gets now added by the NAnt build

ComponentsMap-clean:
	$(RM) $(OUT_DIR)/components.map

install-between-DistFiles:
	(cd $(OUT_DIR) && cp -f ../../DistFiles/UIAdapters-simple.dll TeUIAdapters.dll)
	(cd DistFiles && cp -f $(OUT_DIR)/FwResources.dll .)
	-(cd DistFiles && cp -f $(OUT_DIR)/TeResources.dll .)
	(cd $(OUT_DIR) && ln -sf ../../DistFiles/Language\ Explorer/Configuration/ContextHelp.xml contextHelp.xml)

uninstall-between-DistFiles:
	rm $(OUT_DIR)/TeUIAdapters.dll
	rm DistFiles/FwResources.dll
	rm DistFiles/TeResources.dll

# // TODO-Linux: delete all C# makefiles and replace with Nant calls
BasicUtils-Nant-Build:
	(cd $(BUILD_ROOT)/Bld && mono ../Bin/nant/bin/NAnt.exe build BasicUtils)

Te-Nant-Build:
	(cd $(BUILD_ROOT)/Bld && mono ../Bin/nant/bin/NAnt.exe build allTe)

Te-Nant-Run:
	(cd $(BUILD_ROOT)/Bld && mono ../Bin/nant/bin/NAnt.exe allTe)

Flex-Nant-Build:
	(cd $(BUILD_ROOT)/Bld && mono ../Bin/nant/bin/NAnt.exe build LexTextExe)

Flex-Nant-Run:
	(cd $(BUILD_ROOT)/Bld && mono ../Bin/nant/bin/NAnt.exe LexTextExe)

# InstallLanguage.exe is redundant now. Included for now to make test results match windows tests.
InstallLanguage-Nant:
	(cd $(BUILD_ROOT)/Bld && mono ../Bin/nant/bin/NAnt.exe InstallLanguage-nodep)

TE: linktoOutputDebug tlbs-copy teckit externaltargets Te-Nant-Build install install-strings ComponentsMap-nodep Te-Nant-Run

Flex: linktoOutputDebug tlbs-copy externaltargets Flex-Nant-Build install-strings ComponentsMap-nodep Flex-Nant-Run

Fw:
	(cd $(BUILD_ROOT)/Bld && mono ../Bin/nant/bin/NAnt.exe remakefw)

Fw-build:
	(cd $(BUILD_ROOT)/Bld && mono ../Bin/nant/bin/NAnt.exe build remakefw)

Fw-build-package:
	(cd $(BUILD_ROOT)/Bld && mono ../Bin/nant/bin/NAnt.exe release build remakefw)

TE-run: ComponentsMap-nodep
	(. ./environ && cd $(OUT_DIR) && mono --debug TE.exe -db "$${TE_DATABASE}")

###############################################################################
### Below is local section that shouldn't get clobbered by merging with vcs ###
###############################################################################
