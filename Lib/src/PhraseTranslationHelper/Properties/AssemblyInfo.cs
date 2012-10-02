using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("PhraseTranslationHelper")]
[assembly: AssemblyDescription("Tool for helping translators quickly translate Scripture comprehension checking questions.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIL")]
[assembly: AssemblyProduct("Transcelerator")]
[assembly: AssemblyCopyright("Copyright ©  2011")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b6e07ef9-3e1a-4200-8d7c-4464c4d04f11")]

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
// Format: FwMajorVersion.FwMinorVersion.FwRevision.NumberOfDays
[assembly: AssemblyFileVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.$NUMBEROFDAYS")]
// Format: FwMajorVersion.FwMinorVersion.FwRevision
[assembly: AssemblyInformationalVersionAttribute("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}")]
// Format: FwMajorVersion.FwMinorVersion.FwRevision.Days since Jan 1, 2000.Seconds since midnight
[assembly: AssemblyVersion("$!{FWMAJOR:0}.$!{FWMINOR:0}.$!{FWREVISION:0}.*")]

[assembly: InternalsVisibleTo("PhraseTranslationHelperTests")]
