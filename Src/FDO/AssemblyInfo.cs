/*----------------------------------------------------------------------------------------------
Copyright (c) 2002-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
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
