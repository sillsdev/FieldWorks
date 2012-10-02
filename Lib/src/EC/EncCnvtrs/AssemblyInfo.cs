using System.Reflection;
using System.Runtime.CompilerServices;
using System.Resources;
using System.Runtime.InteropServices;	// For type library.
using System;

// JohnT added the following on suggestion from Randy Regneir. Also the InteropServices using clause.
// This was in hopes of correcting a problem where a new GUID is generated for each build, also
// a problem where we get warnings on #import (on a clean build or where the tlb changed only)
// saying we need to import mscorlib.
[assembly: GuidAttribute("78E6A648-5360-498f-9BB0-FE4B14A87813")]	// Type library guid.

//
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyTitle("Encoding Converters")]
[assembly: AssemblyDescription("Encoding Converters Repository and basic converter engine wrappers")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("SIL")]
[assembly: AssemblyProduct("Encoding Converters")]
[assembly: AssemblyCopyright("Copyright © 2003-2009 SIL. All rights reserved.")]
[assembly: AssemblyTrademark("Copyright © 2003-2009 SIL. All rights reserved.")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguageAttribute("en")]

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// NOTE: if you change this value, then you have to update the function:
//  DirectableEncConverter.cs:DirectableEncConverterDeserializationBinder to
//  support the new version number and allow for the old one when serializing in
//  the data.
[assembly: AssemblyVersion("3.1.0.0")]
[assembly: AssemblyFileVersionAttribute("3.1.0.0")]

[assembly: CLSCompliantAttribute(true)]

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
// done in project settings now (was causing a warning)
// [assembly: AssemblyKeyFile("..\\..\\..\\..\\..\\..\\src\\FieldWorks.snk")]
[assembly: AssemblyKeyName("")]
