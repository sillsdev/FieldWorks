// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using FwBuildTasks;
using Microsoft.Build.Utilities;
using Microsoft.CSharp;
using NUnit.Framework;
using SIL.FieldWorks.Build.Tasks;
using SIL.TestUtilities;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	public sealed class RegFreeCreatorTests
	{
		private const string AsmNamespace = "urn:schemas-microsoft-com:asm.v1";

		[Test]
		public void ProcessManagedAssembly_PlacesClrClassAsChildOfAssembly()
		{
			var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			var assemblyPath = Path.Combine(tempDir, "SampleComClass.dll");

			try
			{
				CompileComVisibleAssembly(assemblyPath);

				var doc = new XmlDocument();
				var root = doc.CreateElement("assembly", AsmNamespace);
				doc.AppendChild(root);
				var logger = new TaskLoggingHelper(new TestBuildEngine(), nameof(RegFreeCreatorTests));
				var creator = new RegFreeCreator(doc, logger);

				var foundClrClass = creator.ProcessManagedAssembly(root, assemblyPath);
				Assert.That(foundClrClass, Is.True, "Test assembly should produce clrClass entries.");

				var ns = new XmlNamespaceManager(doc.NameTable);
				ns.AddNamespace("asmv1", AsmNamespace);
				var fileNode = root.SelectSingleNode("asmv1:file", ns);
				Assert.That(fileNode, Is.Not.Null, "Managed manifest entries must create a file node.");

				// Windows SxS requires clrClass to be a direct child of assembly, not nested under file.
				// Nesting under file causes "side-by-side configuration is incorrect" errors at runtime.
				// See: specs/003-convergence-regfree-com-coverage/REGFREE_BEST_PRACTICES.md
				var nestedClrClass = fileNode.SelectSingleNode("asmv1:clrClass", ns);
				Assert.That(nestedClrClass, Is.Null, "clrClass must NOT be nested under file element (causes SxS errors).");

				var clrClassUnderAssembly = root.SelectSingleNode("asmv1:clrClass", ns);
				Assert.That(clrClassUnderAssembly, Is.Not.Null, "clrClass must be a direct child of the assembly element.");
			}
			finally
			{
				if (Directory.Exists(tempDir))
				{
					Directory.Delete(tempDir, true);
				}
			}
		}

		private static void CompileComVisibleAssembly(string outputPath)
		{
			const string source = @"using System.Runtime.InteropServices;
[assembly: ComVisible(true)]
[assembly: Guid(""3D757DD4-8985-4CA6-B2C4-FA2B950C9F6D"")]
namespace RegFreeCreatorTestAssembly
{
	[ComVisible(true)]
	[Guid(""3EF2F542-4954-4B13-8B8D-A68E4D50D7A3"")]
	[ProgId(""RegFreeCreator.SampleClass"")]
	public class SampleComClass
	{
	}
}";

			var provider = new CSharpCodeProvider();
			var parameters = new CompilerParameters
			{
				GenerateExecutable = false,
				OutputAssembly = outputPath,
				CompilerOptions = "/target:library"
			};
			parameters.ReferencedAssemblies.Add(typeof(object).Assembly.Location);
			parameters.ReferencedAssemblies.Add(typeof(GuidAttribute).Assembly.Location);

			var results = provider.CompileAssemblyFromSource(parameters, source);
			if (results.Errors.HasErrors)
			{
				var message = string.Join(Environment.NewLine, results.Errors.Cast<CompilerError>().Select(e => e.ToString()));
				throw new InvalidOperationException($"Failed to compile COM-visible test assembly:{Environment.NewLine}{message}");
			}
		}
	}
}
