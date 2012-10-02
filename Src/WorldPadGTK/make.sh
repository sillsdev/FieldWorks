#!/bin/sh
#
#    make.sh
#
#    Shell script for building WorldPad.exe
#
#    Andrew Weaver - 2008-05-01
#
#    $Id$
#
# TODO: Use correct locations within FieldWorks directory tree for all DLLs
#
gmcs -out:WorldPad.exe -debug+ -lib:../../Output/ -pkg:gtk-sharp-2.0 -pkg:glade-sharp-2.0 -r:System.Drawing -r:Debug/SimpleRootSite.dll -r:Debug/COMInterfaces.dll -r:Debug/FDOStubs.dll -r:Debug/UtilsStubs.dll  AfStyleDlgController.cs BorderWidget.cs CustomWidgetHandler.cs DialogController.cs DialogFactory.cs FileOpenDlgController.cs FilPgSetDlgController.cs FilPgSetDlgModel.cs FmtBdrDlgPController.cs FmtBulNumDlgController.cs FmtFntDlgController.cs FmtParaDlgRtlController.cs FmtWrtSysDlgController.cs HelpAboutDlgController.cs IDialogModel.cs IWorldPadAppController.cs IWorldPadAppModel.cs IWorldPadDocController.cs IWorldPadDocModel.cs IWorldPadDocView.cs IWorldPadPaneView.cs OldWritingSystemsDlgController.cs OptionsDlgController.cs WorldPadAppController.cs WorldPadAppModel.cs WorldPad.cs WorldPadDocController.cs WorldPadDocModel.cs WorldPadDocView.cs WorldPadPaneView.cs WorldPadVc.cs WorldPadView.cs WpDoc.cs WpFindReplaceDlgController.cs FontPreviewWidget.cs ParaPreviewWidget.cs MyDrawingArea.cs MySpinButton.cs Tools.cs
