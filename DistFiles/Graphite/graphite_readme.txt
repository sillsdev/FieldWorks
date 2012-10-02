GRAPHITE

Report any bugs in the Graphite engine or compiler to graphite_support@sil.org. Please include as much of the following as is relevant:

* the GDL source code file and any included header files
* the Graphite font that exhibited the problem (include as an attachment)
* if possible, the original (non-Graphitized) font
* details about how to reproduce the problem, preferrably a file to load into WorldPad, or specific instructions on characters to type
* a Keyman keyboard, if needed to type the data that produced the problem

NOTE: the IPA Unicode font and keyboard included as part of this release are preliminary versions. Later and more complete versions can be found on the NRTC2 Resource Collection CD, or write to nrsi_ipub@sil.org.


The following are known bugs and unexpected behaviors in the Graphite system:

* If you recompile a Graphite font after installing it using "grfontinst," it may corrupt the font, causing the Graphite engine to crash the next time you use the font.

Workaround #1: Before recompiling, always uninstall the font using "grfontinst -u ...", and after recompiling, reinstall it.

Workaround #2 (preferred): After installing using "grfontinst," go to the Fonts control panel, delete your Graphite font (it will probably appear as a shortcut), and reinstall it using the Install New Font option. After doing this you should be able to recompile as often as you like with impunity.


* If you try to compile a GDL program that does not exist, you may get a very unhelpful error message.


* Postscript names: some problems have been reported with the handling of Postscript names by the compiler. The development team is unsure as to the extent of the problem.


* GDL syntax: it is not possible to put a minus sign in front of an expression in order to make it negative (eg, "-boundingbox.left").

Workaround: multiply it by -1, or substract it from zero.


* The Graphite compiler may occasionally give spurious warnings about inconsistent or inappropriate glyph attribute types. These may usually be safely ignored.