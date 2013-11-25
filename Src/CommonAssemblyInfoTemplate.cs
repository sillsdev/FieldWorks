/*----------------------------------------------------------------------------------------------
Copyright (c) 2002-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

If this file is CommonAssemblyInfo.cs, it is a generated file, and should not be hand edited.
If it is CommonAssemblyInfoTemplate.cs, it is the template from which CommonAssemblyInfo.cs
is generated.

CommonAssemblyInfo.cs should be included in every FieldWorks project.
It holds common directives that are usually part of AssemblyInfo.cs.
Some are kept here so that certain symbols (starting with $ in the template) can be replaced
with appropriate values, typically version numbers, by a custom build task
(Currently VersionEx in Nant, Substitute in the new MSBuild/XBuild approach).
Other directives are merely here because we want them to be the same for all FieldWorks projects.
----------------------------------------------------------------------------------------------*/
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIL")]
[assembly: AssemblyProduct("SIL FieldWorks")]
[assembly: AssemblyCopyright("Copyright (c) 2002-$YEAR SIL International")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Format: Version.Milestone.Year.MMDDL
[assembly: AssemblyFileVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.$NUMBEROFDAYS")]
// Format: FwMajorVersion.FwMinorVersion
[assembly: AssemblyInformationalVersionAttribute("$!{FWMAJOR:0}.$!{FWMINOR:0} $!{FWBETAVERSION:0}")]
// Format: Version.Milestone.0.Level
[assembly: AssemblyVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.*")]
