#!/bin/sh
(cd 'DialogResourceFiles';rm *.rc~)
rm converted-dialogs.glade
# Create GladeInterface.glade
mono app/Rc2glade.exe > Rc2glade.txt
# Remove unused, partially converted dialogs
xsltproc -o GladeInterface1.glade xslt/omit-unused.xsl GladeInterface.glade
# <comment>
xsltproc -o GladeInterface2.glade xslt/SpinButton.xsl GladeInterface1.glade
# <comment>
xsltproc -o GladeInterface3.glade xslt/AfStyleDlg.xsl GladeInterface2.glade
# <comment>
xsltproc -o GladeInterface4.glade xslt/AfStyleDlg2.xsl GladeInterface3.glade
# <comment>
xsltproc -o GladeInterface4a.glade xslt/FmtBdrDlgP.xsl GladeInterface4.glade
# <comment>
xsltproc -o GladeInterface4b.glade xslt/FmtBulNumDlg.xsl GladeInterface4a.glade
# Replace OK, Cancel & Help buttons with those in dialog action area
xsltproc --stringparam dialogname kridFmtFntDlg -o GladeInterface4c.glade xslt/OkCancelHelp.xsl GladeInterface4b.glade
# <comment>
xsltproc -o GladeInterface5.glade xslt/FmtParaDlgRtl.xsl GladeInterface4c.glade
# Replace OK, Cancel & Help buttons with those in dialog action area
xsltproc --stringparam dialogname kridNewWs -o GladeInterface6.glade xslt/OkCancelHelp.xsl GladeInterface5.glade

# Apply various modifications to kridOldWritingSystemsDlg
xsltproc -o GladeInterface6a.glade xslt/OldWritingSystemsDlg.xsl GladeInterface6.glade
# Replace OK, Cancel & Help buttons with those in dialog action area
xsltproc --stringparam dialogname kridOldWritingSystemsDlg -o GladeInterface7.glade xslt/OkCancelHelp.xsl GladeInterface6a.glade

# Add a 'group' property for the kctidArrLog Gtk.RadioButton
xsltproc -o GladeInterface7a.glade xslt/OptionsDlg.xsl GladeInterface7.glade
# Replace OK, Cancel & Help buttons with those in dialog action area
xsltproc --stringparam dialogname kridOptionsDlg -o GladeInterface8.glade xslt/OkCancelHelp.xsl GladeInterface7a.glade

# Replace OK, Cancel & Help buttons with those in dialog action area
xsltproc --stringparam dialogname kridRemFmtDlg -o GladeInterface9.glade xslt/OkCancelHelp.xsl GladeInterface8.glade
# Replace OK, Cancel & Help buttons with those in dialog action area
xsltproc --stringparam dialogname kridSavePlainTextDlg -o GladeInterface10.glade xslt/OkCancelHelp.xsl GladeInterface9.glade
# <comment>
xsltproc -o GladeInterface11.glade xslt/DeleteWs.xsl GladeInterface10.glade
# <comment>
xsltproc -o GladeInterface12.glade xslt/DocDlg.xsl GladeInterface11.glade
# <comment>
xsltproc -o GladeInterface13.glade xslt/HelpAboutDlg.xsl GladeInterface12.glade
# <comment>
xsltproc -o GladeInterface14.glade xslt/PrintCancelDlg.xsl GladeInterface13.glade
# <comment>
xsltproc -o GladeInterface15.glade xslt/ProgressWithCancelDlg.xsl GladeInterface14.glade
# <comment>
xsltproc -o GladeInterface16.glade xslt/FmtWrtSysDlg.xsl GladeInterface15.glade
# <comment>
xsltproc -o GladeInterface17.glade xslt/DelAndChgStylesWarningDlg.xsl GladeInterface16.glade
# <comment>
xsltproc -o GladeInterface18.glade xslt/EmptyReplaceDlg.xsl GladeInterface17.glade
# <comment>
xsltproc -o GladeInterface19.glade xslt/FilPgSetDlg.xsl GladeInterface18.glade
# Replace 'UNKNOWN' Gtk.Label widgets in AfStyleDlg & FmtBdrDlgP (and more besides!!!!)
xsltproc -o GladeInterface20.glade xslt/AfStyleDlg3.xsl GladeInterface19.glade
# Create 'tab sites' in AfStyleDlg for Paragraph, Bullets & Numbering, Border widgets
xsltproc -o converted-dialogs.glade xslt/common-widgets.xsl GladeInterface20.glade
# Tidy up by deleting all intermediate files
rm GladeInterface*.glade
