// --------------------------------------------------------------------------------------------
#region // Copyright © 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright © 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AssemblyInfo.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Contains information about assembly and version
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Reflection;
using System.Runtime.CompilerServices;

//
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyTitle("FieldWorks Common Framework")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIL")]
[assembly: AssemblyProduct("SIL FieldWorks")]
[assembly: AssemblyCopyright("(C) 2002-$YEAR, SIL International")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//

// Format: FwMajorVersion.FwMinorVersion.FwRevision.NumberOfDays
[assembly: AssemblyFileVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.$NUMBEROFDAYS")]
// Format: FwMajorVersion.FwMinorVersion.FwRevision
[assembly: AssemblyInformationalVersionAttribute("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}")]
// Format: FwMajorVersion.FwMinorVersion.FwRevision.*
[assembly: AssemblyVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.*")]

// Setting this to false will, by default, prevent classes from being exported for COM.
// For classes that should be exported for COM, they should have the ComVisible(true)
// attribute.
[assembly: System.Runtime.InteropServices.ComVisible(false)]

[assembly:InternalsVisibleTo("FrameworkTests")]
