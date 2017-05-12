// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Reflection;
using SIL.FieldWorks.Common.FwKernelInterfaces.Attributes;
using SIL.TestUtilities;
using SIL.Utils.Attributes;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("CoreImplTests")]
[assembly: AssemblyDescription("")]

// Cleanup all singletons after running tests
[assembly: CleanupSingletons]
[assembly: InitializeIcu(IcuDataPath = "IcuData")]
[assembly: OfflineSldr]
