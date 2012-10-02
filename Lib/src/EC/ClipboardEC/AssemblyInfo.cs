using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;	// GuidAttribute

// JohnT added the following on suggestion from Randy Regneir. Also the InteropServices using clause.
// This was in hopes of correcting a problem where a new GUID is generated for each build, also
// a problem where we get warnings on #import (on a clean build or where the tlb changed only)
// saying we need to import mscorlib.
[assembly: GuidAttribute("836FFC49-9817-4681-99B7-1DE52DC7B6BA")]	// Type library guid.

//
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyTitle("Clipboard EncConverters")]
[assembly: AssemblyDescription("Converter clipboard data with one of your system converters")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIL")]
[assembly: AssemblyProduct("Clipboard EncConverters")]
[assembly: AssemblyCopyright("Copyright © 2004-2009 SIL. All rights reserved.")]
[assembly: AssemblyTrademark("Copyright © 2004-2009 SIL. All rights reserved.")]
[assembly: AssemblyCulture("")]

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers
// by using the '*' as shown below:

[assembly: AssemblyVersion("3.1.0.0")]

//
// In order to sign your assembly you must specify a key to use. Refer to the
// Microsoft .NET Framework documentation for more information on assembly signing.
//
// Use the attributes below to control which key is used for signing.
//
// Notes:
//   (*) If no key is specified, the assembly is not signed.
//   (*) KeyName refers to a key that has been installed in the Crypto Service
//       Provider (CSP) on your machine. KeyFile refers to a file which contains
//       a key.
//   (*) If the KeyFile and the KeyName values are both specified, the
//       following processing occurs:
//       (1) If the KeyName can be found in the CSP, that key is used.
//       (2) If the KeyName does not exist and the KeyFile does exist, the key
//           in the KeyFile is installed into the CSP and used.
//   (*) In order to create a KeyFile, you can use the sn.exe (Strong Name) utility.
//       When specifying the KeyFile, the location of the KeyFile should be
//       relative to the project output directory which is
//       %Project Directory%\obj\<configuration>. For example, if your KeyFile is
//       located in the project directory, you would specify the AssemblyKeyFile
//       attribute as [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) Delay Signing is an advanced option - see the Microsoft .NET Framework
//       documentation for more information on this.
//
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile("")]
[assembly: AssemblyKeyName("")]
[assembly: AssemblyFileVersionAttribute("3.1.0.0")]
[assembly: ComVisibleAttribute(false)]
[assembly: NeutralResourcesLanguageAttribute("en")]
