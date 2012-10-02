#!/bin/sh

# Combine .glade files (FindReplaceDlg, FileOpenDlg)
# ==================================================
#
# Note: <!DOCTYPE...> in the files to be merged generates the following errors:
#
# error : Operation in progress
# FindReplaceDlg.glade:2: warning: failed to load external entity "http://glade.gnome.org/glade-2.0.dtd"
# <!DOCTYPE glade-interface SYSTEM "http://glade.gnome.org/glade-2.0.dtd">
#                                                                         ^
# error : Operation in progress
# FileOpenDlg.glade:2: warning: failed to load external entity "http://glade.gnome.org/glade-2.0.dtd"
# <!DOCTYPE glade-interface SYSTEM "http://glade.gnome.org/glade-2.0.dtd">
#                                                                         ^
#
xsltproc -o dialogs.glade xslt/merger.xsl xslt/merger.xml

# The following attempted solution to the "DOCTYPE" problem did not work:
#
# XML_CATALOG_FILES='catalog.xml /etc/xml/catalog' xsltproc -o dialogs.glade xslt/merger.xsl xslt/merger.xml
