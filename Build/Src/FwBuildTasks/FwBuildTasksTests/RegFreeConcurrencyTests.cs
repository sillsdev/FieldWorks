// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CSharp;
using NUnit.Framework;
using SIL.FieldWorks.Build.Tasks;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	/// <summary>
	/// Verifies concurrent <see cref="RegFree"/> executions produce a valid shared manifest.
	/// </summary>
	[TestFixture]
	public sealed class RegFreeConcurrencyTests
	{
		private const string AsmNamespace = "urn:schemas-microsoft-com:asm.v1";

		[Test]
		public void Execute_ConcurrentInvocationsTargetingSameManifest_AllSucceedAndProduceValidXml()
		{
			var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			var assemblyPath = Path.Combine(tempDir, "RegFreeConcurrencyTestAssembly.dll");
			var manifestPath = Path.Combine(tempDir, "Shared.manifest");

			try
			{
				CompileComVisibleAssembly(assemblyPath);

				const int concurrentInvocations = 12;
				var results = new bool[concurrentInvocations];
				var tasks = new Task[concurrentInvocations];
				for (var i = 0; i < concurrentInvocations; i++)
				{
					var index = i;
					tasks[index] = Task.Run(() =>
					{
						var regFree = new RegFree
						{
							BuildEngine = new FwBuildTasks.TestBuildEngine(),
							Executable = assemblyPath,
							Output = manifestPath,
							ManagedAssemblies = new Microsoft.Build.Utilities.TaskItem[]
							{
								new Microsoft.Build.Utilities.TaskItem(assemblyPath)
							},
							Platform = "x64"
						};
						results[index] = regFree.Execute();
					});
				}

				Task.WaitAll(tasks);

				Assert.That(results, Has.All.True,
					"every concurrent RegFree.Execute() call must succeed - none should fail with the " +
					"manifest-file-in-use IOException this test guards against");

				Assert.That(File.Exists(manifestPath), Is.True, "the shared manifest file must exist after all writers finish");

				var doc = new XmlDocument();
				Assert.DoesNotThrow(() => doc.Load(manifestPath),
					"the manifest file must be well-formed XML, not truncated or interleaved by a concurrent write race");

				var ns = new XmlNamespaceManager(doc.NameTable);
				ns.AddNamespace("asmv1", AsmNamespace);
				Assert.That(doc.SelectSingleNode("//asmv1:clrClass", ns), Is.Not.Null,
					"the manifest must still contain the expected clrClass content after the concurrent writes");
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
[assembly: Guid(""6A2C9E1D-6C8B-4B77-9C3E-6F6B6B2C9E1D"")]
namespace RegFreeConcurrencyTestAssembly
{
	[ComVisible(true)]
	[Guid(""7B3DAF2E-7D9C-4C88-AD4F-7A7C7C3DAF2E"")]
	[ProgId(""RegFreeConcurrency.SampleClass"")]
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
				throw new InvalidOperationException($"Failed to compile test assembly:{Environment.NewLine}{message}");
			}
		}
	}
}
