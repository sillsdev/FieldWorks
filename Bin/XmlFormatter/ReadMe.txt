XmlFormatter normalizes the format of an XML file.

It takes one argument, which is either a file name or a directory name.

If the argument is a file, XmlFormatter will reformat that file (but only if it writeable).  It creates a backup of the file first.  The back up is the original file name appended with ".xml"

If the argument is a directory, XmlFormatter will search the directory and all subdirectories for *.xml files.  It then processes these as above.  We do not recommend using this option unless you are absolutely sure you want to reformat every .xml file in the directory and its subdirectories.

We suggest you create a shortcut to Bin\XmlFormatter.exe and put it in your Send To folder.  Then you can use explorer to navigate to the file you want to reformat, right-click on it, select Send To, and then your shortcut.

Andy Black
andy_black@sil.org
22-Jul-2004
