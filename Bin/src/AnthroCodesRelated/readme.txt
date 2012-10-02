July 25, 2002
Larry Hayashi

FIXES FOR OCM HELP FILES

This directory contains a number of files used to fix the OCM codes dump from LinguaLinks so that a help file can be generated with a working Index.

In order to use the following, you need to have MSXML installed and the latest HTML Help compiler both available from the Microsoft website.

Note: I believe that David Coward had also run some cc tables on the output from LinguaLinks to get it into the form that will serve as an input here. Unfortunately, we do not have these tables.

The fixes include the following:

a. UpdatedFRAME.zip: Zip file of FRAME categories htm files that have an Index link (added in manually).

For some reason the FRAME category files did not have an index link included on them. There are only a dozen or so of these FRAME pages so I have added the index links manually. Use these FRAME files when compiling the help file to replace the ones that LinguaLinks dumped out.

b. IndexFrame.htm and Index.htm.

These are not generated from the LinguaLinks library dump. Index.htm has a javascript to help the user jump around the index by typing a value in a form text box.

c. tidy.zip - this file is used to convert IndexA.htm, IndexB.htm, etc. from HTML to XHTML so that the XSL transformation can be run on them. Tidy is a open source utility from
tidy.sourceforge.net.


d. AddEnt.cct

A cc table to add an entity to the HTML header so that the XSL does not fail on nbsp.

e. FixTheIndexPages.xsl

Use msxsl or another XSL processor to fix the anchors in each of the IndexA.htm, IndexB.htm files (after tidying above).

f. fixIndex.bat

A batch file that will apply tidy, the cc table and the xsl to the htm pages.

g. In addition, the following needs to be done to the TableOfContents.hhc

Change:

<LI> <OBJECT type="text/sitemap">
		  <param name="Name" value="Index">
		  <param name="Local" value="Index.htm">
		  <param name="ImageNumber" value="1">
		  </OBJECT>
		<UL>

to

<LI> <OBJECT type="text/sitemap">
		  <param name="Name" value="Index">
		  <param name="Local" value="IndexFrame.htm">
		  <param name="ImageNumber" value="1">
		  </OBJECT>
		<UL>

h. After doing the above, delete the LI items that for A, B, C, D etc.

i. OCMFrame.hhp: the help project file.

Place all transformed htm files, regular htm files, css and graphics in one folder along with this and compile.

j. Don't forget to replace the IndexA.htm, IndexB.htm etc. pages for the OCM.chm file as well as the OCMFrame.chm file along with the revised IndexFrame.htm and Index.htm.
