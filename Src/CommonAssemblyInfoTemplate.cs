/*----------------------------------------------------------------------------------------------
Copyright (c) 2002-$YEAR SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

$GENERATEDFILECOMMENT

CommonAssemblyInfo.cs should be included in FieldWorks.csproj (the entry assembly)
so that $BASEBUILDNUMBER can be updated by a custom build task
(Currently Substitute in MSBuild).

REVIEW (Hasso) 2023.01: this assembly info is no longer common, since only one project needs it.
It could be included in each project for consistency; see LT-21309.
Should this file be included in each project, renamed, or moved?
----------------------------------------------------------------------------------------------*/
using System.Reflection;

// Note: the BuildNumber should not have a default value in this file (because it is not in the substitutions file)
// Format: Major.Minor.Revision.BuildNumber
[assembly: AssemblyFileVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.$BUILDNUMBER")]
// Format: Major.Minor.Revision.BuildNumber Day Alpha/Beta/RC
[assembly: AssemblyInformationalVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.$BUILDNUMBER $NUMBEROFDAYS $!FWBETAVERSION")]
// Format: Major.Minor.Revision.BuildNumber
[assembly: AssemblyVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.$BUILDNUMBER")]
// Format: The build number of the base build (used to select patches for automatic updates)
[assembly: AssemblyMetadataAttribute("BaseBuildNumber", "$BASEBUILDNUMBER")]