// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SIL.Acknowledgements;

// [assembly: AssemblyTitle("SilSidePane")] // Sanitized by convert_generate_assembly_info

// [assembly: ComVisible(false)] // Sanitized by convert_generate_assembly_info

// Expose IItemArea to unit tests
[assembly: InternalsVisibleTo("SilSidePaneTests")]

// SilSidePane is derived from OutlookBar <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, by Star Vega.
// It was changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.
[assembly: Acknowledgement("SilSidePane",
	Name = "SilSidePane",
	Copyright = "Copyright (C) 2008-$YEAR SIL International. Copyright (C) 2007 Star Vega. Licensed under the MIT license.",
	Url = "https://github.com/sillsdev/FieldWorks/tree/develop/Src/XCore/SilSidePane",
	LicenseUrl = "https://opensource.org/licenses/MIT",
	Location = "./SilSidePane.dll")]