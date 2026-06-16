using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;

namespace SIL.InstallValidator
{
	[TestFixture]
	public sealed class DependencyVersionConsistencyTests
	{
		[Test]
		public void EncodingConvertersPathsStayAlignedWithCentralPackageVersion()
		{
			var repoRoot = FindRepoRoot();
			Assert.That(repoRoot, Is.Not.Null, "Could not locate repo root (FieldWorks.sln).");

			var silVersionsPath = Path.Combine(repoRoot, "Build", "SilVersions.props");
			var expectedVersion = ReadSingleMatch(
				silVersionsPath,
				@"<EncodingConvertersCoreVersion>(?<version>[^<]+)</EncodingConvertersCoreVersion>",
				"EncodingConvertersCoreVersion");

			var packageVersionExpression = ReadCentralPackageVersion(repoRoot, "encoding-converters-core");
			Assert.That(
				packageVersionExpression,
				Is.EqualTo("$(EncodingConvertersCoreVersion)"),
				"Directory.Packages.props should reference the shared EncodingConvertersCoreVersion property.");

			var windowsTargetsPath = Path.Combine(repoRoot, "Build", "Windows.targets");
			var windowsTargetsVersion = ReadSingleMatch(
				windowsTargetsPath,
				@"<ECNugetVersion[^>]*>(?<version>[^<]+)</ECNugetVersion>",
				"ECNugetVersion");

			Assert.That(
				windowsTargetsVersion,
				Is.EqualTo("$(EncodingConvertersCoreVersion)"),
				$"{windowsTargetsPath} should reference the shared EncodingConvertersCoreVersion property.");

			var customComponentsPath = Path.Combine(repoRoot, "FLExInstaller", "CustomComponents.wxi");
			var customComponentsContent = File.ReadAllText(customComponentsPath);

			Assert.That(
				Regex.IsMatch(customComponentsContent, @"encoding-converters-core\\__EncodingConvertersCoreVersion__\\runtimes\\EcDistFiles", RegexOptions.IgnoreCase),
				Is.True,
				$"{customComponentsPath} should use the shared EncodingConvertersCoreVersion placeholder instead of a hardcoded version.");

			Assert.That(
				customComponentsContent.Contains(expectedVersion),
				Is.False,
				$"{customComponentsPath} should not hardcode version {expectedVersion}.");

			var installerLegacyTargetsPath = Path.Combine(repoRoot, "Build", "Installer.legacy.targets");
			var installerLegacyTargetsContent = File.ReadAllText(installerLegacyTargetsPath);

			Assert.That(
				installerLegacyTargetsContent.Contains("__EncodingConvertersCoreVersion__") && installerLegacyTargetsContent.Contains("$(EncodingConvertersCoreVersion)"),
				Is.True,
				$"{installerLegacyTargetsPath} should materialize the legacy installer CustomComponents.wxi from the shared EncodingConvertersCoreVersion property.");
		}

		private static string FindRepoRoot()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null)
			{
				if (File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
					return dir.FullName;

				dir = dir.Parent;
			}

			return null;
		}

		private static string ReadCentralPackageVersion(string repoRoot, string packageId)
		{
			var document = XDocument.Load(Path.Combine(repoRoot, "Directory.Packages.props"));
			var package = document
				.Descendants("PackageVersion")
				.FirstOrDefault(element => string.Equals((string) element.Attribute("Include"), packageId, StringComparison.OrdinalIgnoreCase));

			return package == null ? null : (string) package.Attribute("Version");
		}

		private static string ReadSingleMatch(string path, string pattern, string label)
		{
			var content = File.ReadAllText(path);
			var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
			Assert.That(match.Success, Is.True, $"Could not find {label} in {path}.");

			return match.Groups["version"].Value;
		}
	}
}