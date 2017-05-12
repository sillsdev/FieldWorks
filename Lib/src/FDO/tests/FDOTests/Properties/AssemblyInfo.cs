using System.Reflection;
using SIL.FieldWorks.Common.FwKernelInterfaces.Attributes;
using SIL.TestUtilities;
using SIL.Utils.Attributes;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("FDOTests")]
[assembly: AssemblyDescription("")]

[assembly: CleanupSingletons]
[assembly: InitializeIcu(IcuDataPath = "IcuData")]
[assembly: OfflineSldr]
