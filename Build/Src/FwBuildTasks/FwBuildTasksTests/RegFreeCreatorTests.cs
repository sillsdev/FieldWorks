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
		private const string RemovedManagedLgIcuCollatorClsid = "{e771361c-ff54-4120-9525-98a0b7a9accf}";

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

		[Test]
		public void ProcessManagedAssembly_TargetComVisibleFalseClass_DoesNotEmitClrClass()
		{
			var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			var assemblyPath = Path.Combine(tempDir, "SampleComVisibleFalseClass.dll");

			try
			{
				const string source = @"using System.Runtime.InteropServices;
[assembly: ComVisible(true)]
[assembly: Guid(""3D757DD4-8985-4CA6-B2C4-FA2B950C9F6D"")]
namespace RegFreeCreatorTestAssembly
{
	[ComVisible(false)]
	[Guid(""e771361c-ff54-4120-9525-98a0b7a9accf"")]
	public class SampleComVisibleFalseClass
	{
	}
}";
				CompileAssembly(assemblyPath, source);

				var doc = new XmlDocument();
				var root = doc.CreateElement("assembly", AsmNamespace);
				doc.AppendChild(root);
				var logger = new TaskLoggingHelper(new TestBuildEngine(), nameof(RegFreeCreatorTests));
				var creator = new RegFreeCreator(doc, logger);

				var foundClrClass = creator.ProcessManagedAssembly(root, assemblyPath);
				Assert.That(foundClrClass, Is.False, "Assembly with only ComVisible(false) class should not produce clrClass entries.");

				var ns = new XmlNamespaceManager(doc.NameTable);
				ns.AddNamespace("asmv1", AsmNamespace);
				var clrClassUnderAssembly = root.SelectSingleNode("asmv1:clrClass", ns);
				Assert.That(clrClassUnderAssembly, Is.Null, "clrClass must NOT be produced for ComVisible(false) class.");
				var removedClrClass = root.SelectSingleNode("asmv1:clrClass[@clsid='" + RemovedManagedLgIcuCollatorClsid + "']", ns);
				Assert.That(removedClrClass, Is.Null, "Removed ManagedLgIcuCollator CLSID must not appear in generated clrClass entries.");
				Assert.That(root.OuterXml, Does.Not.Contain(RemovedManagedLgIcuCollatorClsid));
			}
			finally
			{
				if (Directory.Exists(tempDir))
				{
					Directory.Delete(tempDir, true);
				}
			}
		}

		[Test]
		public void AddExcludedClsids_NormalizesClsidValues()
		{
			var doc = new XmlDocument();
			var root = doc.CreateElement("assembly", AsmNamespace);
			doc.AppendChild(root);
			var logger = new TaskLoggingHelper(new TestBuildEngine(), nameof(RegFreeCreatorTests));
			var creator = new RegFreeCreator(doc, logger);

			creator.AddExcludedClsids(new[] { "e771361c-ff54-4120-9525-98a0b7a9accf", "{3fb0fcd2-ac55-42a8-b580-73b89a2b6215}" });

			// Use reflection to verify the private field _excludedClsids was populated and normalized
			var field = typeof(RegFreeCreator).GetField("_excludedClsids", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			Assert.That(field, Is.Not.Null, "Should find the private _excludedClsids field.");

			var excludedHashSet = (System.Collections.Generic.HashSet<string>)field.GetValue(creator);
			Assert.That(excludedHashSet, Is.Not.Null);
			Assert.That(excludedHashSet.Count, Is.EqualTo(2));
			Assert.That(excludedHashSet.Contains(RemovedManagedLgIcuCollatorClsid), Is.True, "Should normalize Clsid without braces.");
			Assert.That(excludedHashSet.Contains("{3fb0fcd2-ac55-42a8-b580-73b89a2b6215}"), Is.True, "Should preserve Clsid with braces.");
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
			CompileAssembly(outputPath, source);
		}

		private static void CompileAssembly(string outputPath, string source)
		{
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
				throw new InvalidOperationException($"Failed to compile test assembly:{Environment.NewLine}{message}");
			}
		}
	}
}
