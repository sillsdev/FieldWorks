The classes and interfaces in this folder provide a cross-platform implementation of the
FileOpenDialog and FileSaveDialog. By using the adapter class the .NET
implementation of the dialog will be displayed on Windows. On Linux the corresponding GTK
dialog is used.

How to use these classes:
Instead of creating a OpenFileDialog create a OpenFileDialogAdapter. The interface on this class
is (almost) identical to the one on OpenFileDialog, thus usually requiring to change only one
line of code.

Similar for the other dialogs.

Some options that are not used in FieldWorks code are not implemented in the adapter.

There's also a Manager class that allows to replace the implementation with a fake implementation
for unit testing.
