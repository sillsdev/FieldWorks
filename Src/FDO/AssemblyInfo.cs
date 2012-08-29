/*----------------------------------------------------------------------------------------------
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.
----------------------------------------------------------------------------------------------*/
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("FieldWorks Data Objects")]

// Setting this to false will, by default, prevent classes from being exported for COM.
// For classes that should be exported for COM, they should have the ComVisible(true)
// attribute.
[assembly: ComVisible(false)]
[assembly: InternalsVisibleTo("FDOTests")]
[assembly: InternalsVisibleTo("FDOBrowser")]
[assembly: InternalsVisibleTo("fwdb2xml")]
