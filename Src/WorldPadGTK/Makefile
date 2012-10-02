#
#	$Id$
#
#	WorldPadGTK Makefile
#
#	MarkS - 2008-05-01
#
# FieldWorks
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

BUILD_ROOT = ../..
include $(BUILD_ROOT)/Bld/_names.mak
BUILD_PRODUCT = $(WORLDPADGTK_NAME)
include $(BUILD_ROOT)/Bld/_init.mak.lnx

# Definitions for compiling and linking
DEFS += DEBUG;TRACE

# Source code files
SOURCE_CODE_FILES =  \
	AfStyleDlgController.cs \
	AssemblyInfo.cs \
	BulNumPreviewWidget.cs \
	DialogFactory.cs \
	DrawItemEventArgs.cs \
	DummyIAdvInd4.cs \
	FileOpenDlgController.cs \
	FileOpenDlgModel.cs \
	FileSaveDlgController.cs \
	FileSaveModel.cs \
	FilPgSetDlgController.cs \
	FilPgSetDlgModel.cs \
	FmtBdrDlgPController.cs \
	FmtBdrDlgPModel.cs \
	FmtBulNumDlgController.cs \
	FmtBulNumDlgModel.cs \
	FmtParaDlgRtlController.cs \
	FmtParaDlgRtlModel.cs \
	FmtWrtSysDlgController.cs \
	FmtWrtSysDlgModel.cs \
	GtkPrintUnixDialog.cs \
	IPrintDialog.cs \
	IWorldPadAppController.cs \
	IWorldPadAppModel.cs \
	IWorldPadDocController.cs \
	IWorldPadDocModel.cs \
	IWorldPadDocView.cs \
	IWorldPadPaneView.cs \
	MySpinButton.cs \
	OldWritingSystemsDlgController.cs \
	OldWritingSystemsDlgModel.cs \
	OptionsDlgController.cs \
	OptionsDlgModel.cs \
	PrintDialogFactory.cs \
	PropertiesHelper.cs \
	SelectionState.cs \
	Tools2.cs \
	WorldPad.cs \
	WorldPadAppController.cs \
	WorldPadAppModel.cs \
	WorldPadDocController.cs \
	WorldPadDocModel.cs \
	WorldPadDocView.cs \
	WorldPadPaneView.cs \
	WorldPadView.cs \
	WpDoc.cs \
	WpFindReplaceDlgController.cs \
	WpgtkMainWnd.cs \
	WpStylesheet.cs

UNUSED_SOURCE_CODE_FILES =  \
	FileDialog.cs \
	FmtFntDlgController.cs \
	FmtFntDlgModel.cs \
	MessageBox.cs \
	OpenFileDialog.cs \
	Porting.cs \
	SaveFileDialog.cs

# Resources
RESOURCES =

# Assembly References
ASSEMBLY_REFERENCES =  \
	$(OUT_DIR)/BasicUtils.dll \
	$(OUT_DIR)/FDO.dll \
	$(OUT_DIR)/Framework.dll \
	$(OUT_DIR)/FwCoreDlgControls.dll \
	$(OUT_DIR)/FwCoreDlgs.dll \
	$(OUT_DIR)/FwCoreDlgsGTK.dll \
	$(OUT_DIR)/FwCoreDlgWidgets.dll \
	$(OUT_DIR)/FwResources.dll \
	$(OUT_DIR)/RootSite.dll \
	$(OUT_DIR)/Utils.dll \
	../../Output/Debug/BasicUtils.dll \
	../../Output/Debug/FDO.dll \
	../../Output/Debug/Framework.dll \
	../../Output/Debug/FwCoreDlgsGTK.dll \
	../../Output/Debug/FwCoreDlgWidgets.dll \
	../../Output/Debug/FwResources.dll \
	../../Output/Debug/RootSite.dll \
	../../Output/Debug/Utils.dll

# Package and System References
PACKAGE_REFERENCES =  \
	Mono.Posix \
	-pkg:glade-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	System.Drawing

	# System.Windows.Forms

# MonoDevelop Project References
PROJECT_REFERENCES =  \
	$(OUT_DIR)/COMInterfaces.dll \
	$(OUT_DIR)/SimpleRootSite.dll \
	../../Output/Debug/COMInterfaces.dll

SRCS = $(SOURCE_CODE_FILES)
RESS = $(RESOURCES)
REFS = $(ASSEMBLY_REFERENCES) $(PROJECT_REFERENCES)
PKGREFS = $(patsubst -pkg:%,%, $(filter -pkg:%, $(PACKAGE_REFERENCES)))
SYSREFS = $(filter-out -pkg:%, $(PACKAGE_REFERENCES))

VIEWS_COM_OBJECT=$(OUT_DIR)/libViews.so
KERNEL_COM_OBJECT=$(OUT_DIR)/libFwKernel.so
LANGUAGE_COM_OBJECT=$(OUT_DIR)/libLanguage.so

COM_OBJECTS = $(VIEWS_COM_OBJECT) $(KERNEL_COM_OBJECT) $(LANGUAGE_COM_OBJECT)

GLADE=	\
	$(OUT_DIR)/glade/dialogs.glade \
	$(OUT_DIR)/glade/main.glade \
	$(OUT_DIR)/glade/FwCoreDlgs.glade \
	$(OUT_DIR)/glade/FwCoreDlgControlsGTK.glade \
	$(OUT_DIR)/glade/WorldPad.ico \
	$(OUT_DIR)/glade/FilPgSetDlgDate.bmp \
	$(OUT_DIR)/glade/FilPgSetDlgFont.bmp \
	$(OUT_DIR)/glade/FilPgSetDlgLand.bmp \
	$(OUT_DIR)/glade/FilPgSetDlgPgNum.bmp \
	$(OUT_DIR)/glade/FilPgSetDlgPort.bmp \
	$(OUT_DIR)/glade/FilPgSetDlgTime.bmp \
	$(OUT_DIR)/glade/FilPgSetDlgTit.bmp \
	$(OUT_DIR)/glade/FilPgSetDlgTotPg.bmp \
	$(OUT_DIR)/glade/FmtBdrDlgAll.bmp \
	$(OUT_DIR)/glade/FmtBdrDlgNoneP.bmp \
	$(OUT_DIR)/glade/HelpAbout.bmp

WPX= \
	$(OUT_DIR)/blank.wpx \
	$(OUT_DIR)/hello.wpx \
	$(OUT_DIR)/Welcome-linux.wpx \
	$(OUT_DIR)/default.wpt \

DTD= $(OUT_DIR)/WorldPad.dtd

all: $(OUT_DIR)/WorldPad.exe $(GLADE) $(WPX)

# The sole .glade file in ../FwCoreDlgsGTK/glade/ is duplicated in glade/.
# Use the latter instead!
# $(GLADE): glade/*.glade glade/*.ico glade/*.bmp ../FwCoreDlgsGTK/glade/*.glade
$(GLADE): glade/*.glade glade/*.ico glade/*.bmp FmtBdrDlgDiag2.png
	@mkdir -p $(OUT_DIR)/glade
	cp -f glade/*.glade $(OUT_DIR)/glade/.
	cp -f glade/*.ico $(OUT_DIR)/glade/.
	cp -f glade/*.bmp $(OUT_DIR)/glade/.
	cp -f *.png $(OUT_DIR)/.

$(WPX): blank.wpx hello.wpx Welcome-linux.wpx default.wpt
	cp -f *.wp[xt] $(OUT_DIR)/

$(OUT_DIR)/WorldPad.exe: $(SRCS) $(REFS) $(RESS)
	$(LINK.net) -g

$(DTD):
	cp -f WorldPad.dtd $(OUT_DIR)/.

clean:
	$(RM) $(OUT_DIR)/WorldPad.exe $(OUT_DIR)/WorldPad.exe.mdb $(RESS) $(OUT_DIR)/glade/* $(DTD) $(WPX)

MONO = mono
MONOFLAGS = --debug

ICU_LIB = $(shell $(ICU_BIN_DIR)/icu-config --prefix)/lib
DISTFILES = $(BUILD_ROOT)/DistFiles

export FW_ROOT = $(DISTFILES)
export FW_ROOTCODE = $(DISTFILES)
export COMPONENTS_MAP_PATH = $(OUT_DIR)
export LD_LIBRARY_PATH := $(OUT_DIR):$(COM_DIR)/build$(ARCH)/lib:$(ICU_LIB):$(LD_LIBRARY_PATH)
export MONO_PATH := $(DISTFILES):$(OUT_DIR):$(COM_LIB):$(MONO_PATH)

# in order to run WorldPadGTK we need to
# 1. copy glade files
# 2. copy DTD
# 3. Ensure we have working com implementation from MONO
# 4. Ensure COM objects are built.

run: all $(GLADE) $(DTD) $(COM_DIR)/build$(ARCH)/lib/ole32.dll $(COM_OBJECTS)
	cd $(OUT_DIR) && \
	ICU_DATA=$(shell $(BUILD_ROOT)/Bin/abs.py $(BUILD_ROOT)/DistFiles/Icu40/) \
	$(MONO) $(MONOFLAGS) ./WorldPad.exe

$(OUT_DIR)/COMInterfaces.dll:
	@$(MAKE) -C $(SRC)/Common/COMInterfaces -q all || \
	 $(MAKE) -C $(SRC)/Common/COMInterfaces all

$(OUT_DIR)/SimpleRootSite.dll: $(OUT_DIR)/Utils.dll
	@$(MAKE) -C $(SRC)/Common/SimpleRootSite -q all || \
	 $(MAKE) -C $(SRC)/Common/SimpleRootSite all

$(OUT_DIR)/Utils.dll:
	@$(MAKE) -C $(SRC)/Common/Utils -q all || \
	 $(MAKE) -C $(SRC)/Common/Utils all

$(OUT_DIR)/FDOStubs.dll:
	@$(MAKE) -C $(SRC)/FDO -q all || \
	 $(MAKE) -C $(SRC)/FDO all

$(OUT_DIR)/FwResourcesStubs.dll:
	@$(MAKE) -C $(SRC)/FwResources -q all || \
	 $(MAKE) -C $(SRC)/FwResources all

$(LIBRARIES)/COM/src/.libs/libcom.so:
	@$(MAKE) -C $(BUILD_ROOT) -q COM-all  || \
	 $(MAKE) -C $(BUILD_ROOT) COM-all

# Ensure com sym link is created as COM doesn't create this until it is installed.
$(COM_DIR)/build$(ARCH)/lib/ole32.dll: $(COM_DIR)/build$(ARCH)/lib/libcom.so
	ln -sf $(COM_DIR)/build$(ARCH)/lib/libcom.so $(COM_DIR)/build$(ARCH)/lib/ole32.dll

$(VIEWS_COM_OBJECT):
	@$(MAKE) -C $(SRC)/$(VIEWS_NAME) -q all || \
	 $(MAKE) -C $(SRC)/$(VIEWS_NAME) all

$(KERNEL_COM_OBJECT):
	@$(MAKE) -C $(SRC)/$(KERNEL_NAME) -q all || \
	 $(MAKE) -C $(SRC)/$(KERNEL_NAME) all

$(LANGUAGE_COM_OBJECT):
	@$(MAKE) -C $(SRC)/$(LANGUAGE_NAME) -q all || \
	 $(MAKE) -C $(SRC)/$(LANGUAGE_NAME) all

# Boilerplate C# Makefile code

.PHONY: all clean

COMPILE.net = \
	echo MONO_PATH=$$MONO_PATH; \
	gmcs -t:library -out:$@ $(patsubst %,-define:"%", $(DEFS)) \
		-debug \
		$(filter-out %.resources %.dll, $^) \
		$(patsubst %,-resource:%, $(filter %.resources, $^)) \
		-resource:glade/WorldPad.ico,ApplicationIcon \
		$(patsubst %,-r:%, $(filter %.dll, $^)) \
		$(patsubst %,-pkg:%, $(PKGREFS)) \
		$(patsubst %,-r:%, $(SYSREFS))

LINK.net = \
	echo MONO_PATH=$$MONO_PATH; \
	gmcs -t:exe -out:$@ $(patsubst %, -define:"%", $(DEFS)) \
		-debug \
		$(filter-out %.resources %.dll, $^) \
		$(patsubst %,-resource:%, $(filter %.resources, $^)) \
		-resource:glade/WorldPad.ico,ApplicationIcon \
		$(patsubst %,-r:%, $(filter %.dll, $^)) \
		$(patsubst %,-pkg:%, $(PKGREFS)) \
		$(patsubst %,-r:%, $(SYSREFS))

%.resources: %.resx
	resgen2 $<
