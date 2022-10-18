// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Runtime.CompilerServices;
using SIL.Acknowledgements;

[assembly: InternalsVisibleTo("FieldWorks")]
[assembly: InternalsVisibleTo("LCMBrowser")]
[assembly: InternalsVisibleTo("LanguageExplorer.TestUtilities")]
[assembly: InternalsVisibleTo("LanguageExplorerTests")]
[assembly: InternalsVisibleTo("ConvertSFM")]

// SilSidePane is derived from OutlookBar <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, by Star Vega.
// It was changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.
[assembly: Acknowledgement("SilSidePane",
	Name = "SilSidePane",
	Copyright = "Copyright (C) 2008-$YEAR SIL International. Copyright (C) 2007 Star Vega. Licensed under the MIT license.",
	Url = "https://github.com/sillsdev/FieldWorks/tree/develop/Src/XCore/SilSidePane",
	LicenseUrl = "https://opensource.org/licenses/MIT",
	Location = "./SilSidePane.dll")]