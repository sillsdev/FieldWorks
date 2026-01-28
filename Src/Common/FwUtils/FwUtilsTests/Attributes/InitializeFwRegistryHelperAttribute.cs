// Copyright (c) 2012-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace SIL.FieldWorks.Common.FwUtils.Attributes
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NUnit helper attribute that sets the company and product name for use in tests.
	/// </summary>
	/// <remarks>When running unit tests the company is NUnit.org. This attribute overrides
	/// this setting so that the tests get the name they expect.
	/// Typically you'd include Src/AssemblyInfoForTests.cs in your unit tests project which
	/// applies the attribute on the assembly level. Alternatively you can include
	/// [assembly:SetCompanyAndProductForTests] in your code, or apply the attribute
	/// to a single unit test class.
	/// (see http://www.nunit.org/index.php?p=actionAttributes&amp;r=2.6.4)
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
	public class InitializeFwRegistryHelperAttribute : TestActionAttribute
	{
		/// <summary/>
		public override void BeforeTest(ITest test)
		{
			base.BeforeTest(test);
			FwRegistryHelper.Initialize();
			TrySetTestRootDirs();
		}

		private static void TrySetTestRootDirs()
		{
			try
			{
				// When running tests from Output/<Configuration>, prefer DistFiles in the repo/worktree
				// over potentially-stale registry paths from a machine install.
				var codeBase = Assembly.GetExecutingAssembly().CodeBase;
				var uriBase = new Uri(codeBase);
				var exeOrDllDir = Path.GetDirectoryName(
					Uri.UnescapeDataString(uriBase.AbsolutePath)
				);
				if (string.IsNullOrEmpty(exeOrDllDir))
					return;

				var distFiles = Path.GetFullPath(
					Path.Combine(exeOrDllDir, "..", "..", "DistFiles")
				);
				if (!Directory.Exists(distFiles))
					return;

				using (var userKey = FwRegistryHelper.FieldWorksRegistryKey)
				{
					if (userKey != null)
					{
						userKey.SetValue("RootCodeDir", distFiles);
						userKey.SetValue("RootDataDir", distFiles);
					}
				}

				Directory.CreateDirectory(Path.Combine(distFiles, "SIL", "Repository"));
			}
			catch
			{
				// Best-effort: tests should still run if we can't update registry or create folders.
			}
		}
	}
}
