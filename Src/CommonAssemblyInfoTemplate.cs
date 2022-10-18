/*----------------------------------------------------------------------------------------------
Copyright (c) 2002-2021 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

$GENERATEDFILECOMMENT

CommonAssemblyInfo.cs should be included in every FieldWorks project.
It holds common directives that are usually part of AssemblyInfo.cs.
Some are kept here so that certain symbols (starting with $ in the template) can be replaced
with appropriate values, typically version numbers, by a custom build task
(Currently Substitute in MSBuild).
Other directives are merely here because we want them to be the same for all FieldWorks projects.
----------------------------------------------------------------------------------------------*/
using System.Reflection;

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIL")]
[assembly: AssemblyProduct("SIL FieldWorks")]
[assembly: AssemblyCopyright("Copyright (c) 2002-$YEAR SIL International")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Note: the BuildNumber should not have a default value in this file (because it is not in the substitutions file)
// Format: Major.Minor.Revision.BuildNumber
[assembly: AssemblyFileVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.$BUILDNUMBER")]
// Format: Major.Minor.Revision.BuildNumber Day Alpha/Beta/RC
[assembly: AssemblyInformationalVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.$BUILDNUMBER $NUMBEROFDAYS $!FWBETAVERSION")]
// Format: Major.Minor.Revision.BuildNumber?
[assembly: AssemblyVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.*")]
// Format: The build number of the base build (used to select patches for automatic updates)
[assembly: AssemblyMetadataAttribute("BaseBuildNumber", "$BASEBUILDNUMBER")]