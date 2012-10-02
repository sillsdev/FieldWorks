// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AssemblyInfo
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("COMInterfacesTests")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIL")]
[assembly: AssemblyProduct("SIL FieldWorks")]
[assembly: AssemblyCopyright("Copyright (C) 2005 SIL International")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("16e8549b-9698-444b-89ec-bb59a28b410f")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// Format: Version.Milestone.Year.MMDDL
[assembly: AssemblyFileVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.$NUMBEROFDAYS")]
// Format: FwMajorVersion.FwMinorVersion
[assembly: AssemblyInformationalVersionAttribute("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}")]
// Format: Version.Milestone.0.Level
[assembly: AssemblyVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.*")]
