using System;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using SIL.WritingSystems;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[SetUpFixture]
public sealed class TestAssemblySetup
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		var baseDir = AppContext.BaseDirectory;
		var repoRoot = FindRepoRoot(baseDir);

		// Find ICU DLL directory
		var icuDllDir = Path.Combine(repoRoot, "Output", "Debug", "lib", "x64");
		if (Directory.Exists(icuDllDir))
		{
			SetDllDirectory(icuDllDir);
			var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
			if (!currentPath.Contains(icuDllDir, StringComparison.OrdinalIgnoreCase))
			{
				Environment.SetEnvironmentVariable(
					"PATH",
					$"{icuDllDir}{Path.PathSeparator}{currentPath}"
				);
			}
		}

		// Find ICU Data directory
		var icuDataDir = Path.Combine(repoRoot, "DistFiles", "Icu70", "icudt70l");
		if (Directory.Exists(icuDataDir))
		{
			Environment.SetEnvironmentVariable("ICU_DATA", icuDataDir);
		}

		Icu.Wrapper.ConfineIcuVersions(70);
		Icu.Wrapper.Init();

		Sldr.Initialize();
	}

	private static string FindRepoRoot(string baseDir)
	{
		var directory = new DirectoryInfo(baseDir);
		while (directory != null)
		{
			var distFilesPath = Path.Combine(directory.FullName, "DistFiles", "Icu70", "icudt70l");
			var outputPath = Path.Combine(directory.FullName, "Output");
			if (Directory.Exists(distFilesPath) && Directory.Exists(outputPath))
				return directory.FullName;

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException($"Could not locate FieldWorks repo root from '{baseDir}'.");
	}

	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool SetDllDirectory(string lpPathName);
}
