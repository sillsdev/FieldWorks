using System.Reflection;
using SIL.LCModel.Core.Attributes;
using SIL.LCModel.Utils.Attributes;
using SIL.TestUtilities;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SIL.LCModel.Tests")]

[assembly: CleanupSingletons]
[assembly: InitializeIcu(IcuDataPath = "IcuData")]
[assembly: OfflineSldr]
