The dialog conversion process
=============================

1. Extract the Win32 dialog definitions into separate files in the 'DialogResourceFiles' directory. The dialog definitions are contained in the WorldPad.rc file and in each of the .rc files #included in that file. The WorldPad.rc file is contained within the <svn root>/FieldWorks/Src/WorldPad/res/ directory. In the rc2glade directory execute the following command:

	rc2glade$ ./extract_dialogs.py '<svn root>/FieldWorks/Src/WorldPad/res/WorldPad.rc' 'DialogResourceFiles'

2. Edit the following files to facilitate the subsequent stages of the conversion process:

	a) AfFindDlg.rc

		Move the kctidFindTab control to follow immediately after BEGIN

	b) FmtBdrDlgP.rc

		Replace the kctidColor control with the following:

			PUSHBUTTON      "",kctidColor,15,20,65,15

	c) FmtBulNumDlg.rc

		Move the first GROUPBOX line to immediately precede the kctidFbnBulSch control. Move the second GROUPBOX line to immediately precede the kctidFbnNumSch control.

	d) PrintCancelDlg.rc

		Change the dialog's X and Y offsets (33 and 32 respectively) to zero

3. Move to the app directory. Execute the shell script, make.sh, to create Rc2glade.exe. Return to the rc2glade directory.

4. Execute the shell script, gendlgs.sh, to create a single .glade file (converted-dialogs.glade) from all the files in the 'DialogResourceFiles' directory. The script first of all executes the C# program Rc2glade.exe and then performs multiple XSLT transformations on the resulting .glade file.

5. Execute the shell script, merge.sh, to create a single .glade file (dialogs.glade) from the files handmade/FindReplaceDlg/FindReplaceDlg.glade and handmade/FileOpenDlg/FileOpenDlg.glade.

Andrew Weaver, Apr 08 2009
