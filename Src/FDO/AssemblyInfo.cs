/*----------------------------------------------------------------------------------------------
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.
----------------------------------------------------------------------------------------------*/
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("FieldWorks Data Objects")]

[assembly: ComVisible(false)]
[assembly: InternalsVisibleTo("FDOTests")]
[assembly: InternalsVisibleTo("FDOBrowser")]
[assembly: InternalsVisibleTo("fwdb2xml")]
[assembly: InternalsVisibleTo("FwControls")] // To get at FDOBackendProvider.ModelVersion
[assembly: InternalsVisibleTo("LexEdDll")] // To get at FDOBackendProvider.ModelVersion
