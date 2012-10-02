// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
//    Copyright (c) 2011, SIL International. All Rights Reserved.
// </copyright>
//
// File: AssemblyInfo.cs
// Responsibility:
// Last reviewed:
// --------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("FixFwDataDll")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIL International")]
[assembly: AssemblyProduct("FieldWorks")]
[assembly: AssemblyCopyright("Copyright © SIL International 2011")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("06ae96bc-52fb-458d-ae43-47f02e67abb5")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
// Format: Version.Milestone.Year.MMDDL
[assembly: AssemblyFileVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.$NUMBEROFDAYS")]
// Format: FwMajorVersion.FwMinorVersion
[assembly: AssemblyInformationalVersionAttribute("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}")]
// Format: Version.Milestone.0.Level
[assembly: AssemblyVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.*")]
