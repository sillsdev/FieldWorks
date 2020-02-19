// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using NUnit.Framework;
using SIL.FieldWorks.Build.Tasks.Localization;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	public class LocalizeFieldWorksTests
	{
		InstrumentedLocalizeFieldWorks m_sut;
		private string m_rootPath;
		private string m_l10nFolder;
		private string m_srcFolder;
		private string m_FdoFolder;
		private string m_commonFolder;
		private string m_FieldWorksFolder;
		private string m_xCoreFolder;
		private string m_xCoreInterfacesFolder;
		private string m_FieldWorksPropertiesFolder;
		private string m_FieldWorksTestsFolder;
		private string m_sideBarFolder;

		private const string LocaleEs = "es";
		private const string SampleStringEn = "A category";
		private const string SampleStringEs = "Una categoría";

		private const string LocaleGe = "ge";

		private const string FieldWorksNamespace = "SIL.FieldWorks";
		private const string FdoNamespace = "SIL.FDO";
		private const string FieldWorksStringsFilenameNoExt = "FieldWorks-strings";
		private const string MoreStringsFilenameNoExt = "Properties.more strings";
		private const string FdoStringsFilenameNoExt = "FDO-strings";

		[SetUp]
		public void Setup()
		{
			m_sut = new InstrumentedLocalizeFieldWorks();
			m_rootPath = Path.Combine(Path.GetTempPath(), "XXTestRoot");
			m_sut.RootDirectory = m_rootPath;
			m_sut.OutputFolder = Path.Combine(m_rootPath, "Output");
			m_sut.SrcFolder = Path.Combine(m_rootPath, "Src");
			m_sut.L10nFileDirectory = Path.Combine(m_rootPath, "Localizations", "l10ns");
			// wipe out anything left from last time
			if (Directory.Exists(m_rootPath))
				Directory.Delete(m_rootPath, true);
			Directory.CreateDirectory(m_rootPath);
			Directory.CreateDirectory(m_sut.L10nFileDirectory);
		}

		[TearDown]
		public void TearDown()
		{
			InstrumentedProjectLocalizer.Reset();
		}

		/// <summary>Set up Assembly Info and localized projects for tests</summary>
		private void FullSetup()
		{
			CreateLocalizedProjects();

			CreateAssemblyInfo();

			CreateLocalizedFiles();
		}

		/// <summary>Sets up the bare minimum to test localization: strings.**.xml, a project (FDO), and assembly info</summary>
		private void SimpleSetupFDO(string locale, string localizedStringsXml = "safe sample text", string localizedProjStrings = "safe sample text")
		{
			CreateStringsXml(m_sut.StringsXmlSourcePath(locale), localizedStringsXml);
			m_srcFolder = m_sut.SrcFolder;
			m_l10nFolder = Path.Combine(m_sut.L10nFileDirectory, locale);
			m_FdoFolder = CreateLocalizedProject(m_srcFolder, "FDO", "FDO", locale, localizedProjStrings);
			CreateAssemblyInfo();
		}

		/// <summary>Sets up the bare minimum to test the localization of a single string</summary>
		/// <returns>The path to the localized ResX containing the test string</returns>
		private string SimpleSetupWithResX(string locale, string english, string localized)
		{
			SimpleSetupFDO(locale);
			return CreateLocalizedResX(m_FdoFolder, "FDO", "simple_sample", locale, english, localized);
		}

		/// <summary>Create a minimal CommonAssemblyInfo.cs file</summary>
		private void CreateAssemblyInfo()
		{
			var writer = new StreamWriter(m_sut.AssemblyInfoPath, false, Encoding.UTF8);
			writer.WriteLine("[assembly: AssemblyFileVersion(\"8.4.2.1234\")]");
			writer.WriteLine("[assembly: AssemblyInformationalVersionAttribute(\"8.4.2 beta 2\")]");
			writer.WriteLine("[assembly: AssemblyVersion(\"8.4.2.*\")]");
			writer.Close();
		}

		/// <remarks>
		/// We want to create a hierarchy of projects under Src.
		/// To test certain cases, we need
		/// - a folder with a .csproj ("FDO").
		/// - a folder with no .csproj ("Common").
		/// - a child folder with a .csproj ("Common/FieldWorks").
		/// - a child of a .csproj that has no .csproj (but will have resx files): ("Common/FieldWorks/Properties")
		/// - a folder whose name ends in "Tests"
		/// - a folder whose name is exactly SidebarLibrary
		/// - a child of a folder with .csproj that has its own .csproj (xCore and xCore/xCoreInterfaces).
		/// - a project whose assembly name is different from its folder and filenames (xCoreInterfaces named xCoreIntName)
		/// </remarks>
		private void CreateLocalizedProjects()
		{
			CreateStringsXml(m_sut.StringsEnPath, SampleStringEn);
			CreateStringsXml(m_sut.StringsXmlSourcePath(LocaleEs), SampleStringEs); // REVIEW (Hasso) 2019.11: where?

			m_srcFolder = m_sut.SrcFolder;
			m_l10nFolder = Path.Combine(m_sut.L10nFileDirectory, LocaleEs);
			m_FdoFolder = CreateLocalizedProject(m_srcFolder, "FDO");
			m_commonFolder = CreateFolder(m_srcFolder, "Common");
			m_FieldWorksFolder = CreateLocalizedProject(m_commonFolder, "FieldWorks");
			m_FieldWorksPropertiesFolder = CreateFolder(m_FieldWorksFolder, "Properties");
			m_FieldWorksTestsFolder = CreateLocalizedProject(m_FieldWorksFolder, "FieldWorksTests");
			m_sideBarFolder = CreateLocalizedProject(m_srcFolder, "SidebarLibrary");
			m_xCoreFolder = CreateLocalizedProject(m_srcFolder, "xCore");
			m_xCoreInterfacesFolder = CreateLocalizedProject(m_xCoreFolder, "xCoreInterfaces", "xCoreIntName");
			CreateLocalizedResX(m_FieldWorksPropertiesFolder, "FieldWorks", "more strings");
			CreateLocalizedResX(m_FieldWorksFolder, "FieldWorks", "strings");
		}

		private static string CreateFolder(string parent, string name)
		{
			var result = Path.Combine(parent, name);
			Directory.CreateDirectory(result);
			return result;
		}

		/// <summary>
		/// Create a minimal convertible project in the specified folder (with default assembly name same as project).
		/// </summary>
		private string CreateLocalizedProject(string parent, string name)
		{
			return CreateLocalizedProject(parent, name, name);
		}

		/// <summary>
		/// Create a minimal convertible project in the specified folder.
		/// </summary>
		private string CreateLocalizedProject(string parent, string name, string assemblyName,
			string locale = LocaleEs, string projectStringsText = SampleStringEs)
		{
			var projectFolder = CreateFolder(parent, name);
			CreateProjectInExistingFolder(projectFolder, name, assemblyName);
			CreateLocalizedResXFor(projectFolder, name, $"{name}-strings", locale, projectStringsText);
			return projectFolder;
		}

		private static void CreateProjectInExistingFolder(string folder, string project, string assemblyName)
		{
			XNamespace ns = @"http://schemas.microsoft.com/developer/msbuild/2003";
			var doc = new XDocument(
				new XElement(ns + "Project",
					new XAttribute("DefaultTargets", "Build"),
					new XAttribute("ToolsVersion", "4.0"),
					new XElement(ns + "PropertyGroup",
						new XElement(ns + "RootNamespace",
							new XText("SIL." + project)),
						new XElement(ns + "AssemblyName",
							new XText(assemblyName))),
					new XElement(ns + "ItemGroup",
						new XElement(ns + "Compile",
							new XAttribute("Include", "ApplicationBusyDialog.cs"))),
					new XElement(ns + "ItemGroup",
						new XElement(ns + "EmbeddedResource",
							new XAttribute("Include", "ApplicationBusyDialog.resx")))));
			string projectPath = Path.ChangeExtension(Path.Combine(folder, project), "csproj");
			doc.Save(projectPath);
			CreateResX(folder, project + "-strings", SampleStringEn);
		}

		private void CreateLocalizedFiles()
		{
			CreateResX(m_FieldWorksFolder.Replace(m_srcFolder, m_l10nFolder),
				$"{FieldWorksNamespace}.{FieldWorksStringsFilenameNoExt}.{LocaleEs}.resx", SampleStringEs);
			CreateResX(m_FieldWorksPropertiesFolder.Replace(m_srcFolder, m_l10nFolder),
				$"{FieldWorksNamespace}.{MoreStringsFilenameNoExt}.{LocaleEs}.resx", SampleStringEs);
			CreateResX(m_FdoFolder.Replace(m_srcFolder, m_l10nFolder),
				$"{FdoNamespace}.{FdoStringsFilenameNoExt}.{LocaleEs}.resx", SampleStringEs);
		}

		/// <summary>creates an English and a localized version of the same ResX file</summary>
		/// <returns>the path to the localized version of the ResX file</returns>
		private string CreateLocalizedResX(string projectFolder, string projectName, string fileNameNoExt,
			string locale = LocaleEs, string englishText = SampleStringEn, string localizedText = SampleStringEs)
		{
			CreateResX(projectFolder, fileNameNoExt, englishText);
			return CreateLocalizedResXFor(projectFolder, projectName, fileNameNoExt, locale, localizedText);
		}

		private string CreateLocalizedResXFor(string projectFolder, string projectName,
			string fileNameNoExt, string locale, string localizedText)
		{
			return CreateResX(projectFolder.Replace(m_rootPath, m_l10nFolder), $"{fileNameNoExt}.{locale}.resx", localizedText);
		}

		private static string CreateResX(string folder, string fileName, string textValue)
		{
			var doc = new XDocument(
				new XElement("root",
					new XElement("data",
						new XAttribute("name", "ksTest"),
						new XElement("value",
							new XText(textValue)))));
			var path = Path.ChangeExtension(Path.Combine(folder, fileName), "resx");
			Directory.CreateDirectory(folder);
			doc.Save(path);
			return path;
		}

		/// <summary>
		/// Create some test data in DistFiles/Language Explorer/Configuration/strings-en.txt which we can try to localize.
		/// </summary>
		private static void CreateStringsXml(string path, string txt)
		{
			var doc = new XDocument(
				new XElement("strings",
					new XElement("group",
						new XAttribute("id", "Misc"),
						new XElement("string",
							new XAttribute("id", "test"),
							new XAttribute("txt", "try out")),
						new XElement("string",
							new XAttribute("id", "fix"),
							new XAttribute("txt", txt)))));
			// ReSharper disable once AssignNullToNotNullAttribute - StringsEnPath will always have a directory name
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			doc.Save(path);
		}

		[Test]
		public void DoIt()
		{
			FullSetup();

			var result = m_sut.Execute();

			Assert.That(result, Is.True, m_sut.ErrorMessages);
			var stringsEsPath = m_sut.StringsXmlPath("es");
			Assert.That(File.Exists(stringsEsPath));
			// TODO (Hasso) 2019.11: further verification of strings-es.xml?

			VerifyExpectedResx(m_FieldWorksFolder, FieldWorksStringsFilenameNoExt, FieldWorksNamespace);
			VerifyExpectedResx(m_FieldWorksPropertiesFolder, MoreStringsFilenameNoExt, FieldWorksNamespace);
			VerifyExpectedResx(m_FdoFolder, FdoStringsFilenameNoExt, FdoNamespace);

			VerifyExpectedResGenArgs(m_FieldWorksFolder, FieldWorksStringsFilenameNoExt, FieldWorksNamespace);
			VerifyExpectedResGenArgs(m_FieldWorksPropertiesFolder, MoreStringsFilenameNoExt, FieldWorksNamespace);
			VerifyExpectedResGenArgs(m_FdoFolder, FdoStringsFilenameNoExt, FdoNamespace);

			// The Assembly Linker should be run (once for each desired project) with expected arguments.
			Assert.That(InstrumentedProjectLocalizer.LinkerPath.Count, Is.EqualTo(4));
			VerifyLinkerArgs(Path.Combine(m_sut.OutputFolder, "Release", "es", "FDO.resources.dll"), new[] {
				new EmbedInfo(Path.Combine(m_sut.OutputFolder, "es", "FDO", "SIL.FDO.FDO-strings.es.resources"),
							  "SIL.FDO.FDO-strings.es.resources")
				});
			VerifyLinkerArgs(Path.Combine(m_sut.OutputFolder, "Release", "es", "FieldWorks.resources.dll"), new[] {
				new EmbedInfo(Path.Combine(m_sut.OutputFolder, "es", "Common", "FieldWorks", "SIL.FieldWorks.FieldWorks-strings.es.resources"),
							  "SIL.FieldWorks.FieldWorks-strings.es.resources"),
				new EmbedInfo(Path.Combine(m_sut.OutputFolder, "es", "Common", "FieldWorks", "SIL.FieldWorks.strings.es.resources"),
							  "SIL.FieldWorks.strings.es.resources"),
				new EmbedInfo(Path.Combine(m_sut.OutputFolder, "es", "Common", "FieldWorks", "Properties", "SIL.FieldWorks.Properties.more strings.es.resources"),
							  "SIL.FieldWorks.Properties.more strings.es.resources")
				});
			VerifyLinkerArgs(Path.Combine(m_sut.OutputFolder, "Release", "es", "xCoreIntName.resources.dll"), new[] {
				new EmbedInfo(Path.Combine(m_sut.OutputFolder, "es", "xCore", "xCoreInterfaces", "SIL.xCoreInterfaces.xCoreInterfaces-strings.es.resources"),
							  "SIL.xCoreInterfaces.xCoreInterfaces-strings.es.resources")
				});
		}

		[Test]
		public void DoIt_SourceOnly()
		{
			FullSetup();

			m_sut.Build = "SourceOnly";
			var result = m_sut.Execute();

			Assert.That(result, Is.True, m_sut.ErrorMessages);
			var stringsEsPath = m_sut.StringsXmlPath("es");
			Assert.That(File.Exists(stringsEsPath));

			// The Assembly Linker should not be run for source-only
			Assert.That(InstrumentedProjectLocalizer.LinkerPath.Count, Is.EqualTo(0));
		}

		[Test]
		public void DoIt_BinaryOnly()
		{
			// Setup
			FullSetup();
			m_sut.Build = "SourceOnly";
			var result = m_sut.Execute();
			Assert.That(result, Is.True, $"setup failed:{Environment.NewLine}{m_sut.ErrorMessages}");

			// Execute
			m_sut.Build = "BinaryOnly";
			result = m_sut.Execute();

			Assert.That(result, Is.True, $"SUT failed:{Environment.NewLine}{m_sut.ErrorMessages}");

			// The Assembly Linker should be run (once for each desired project) with expected arguments.
			Assert.That(InstrumentedProjectLocalizer.LinkerPath.Count, Is.EqualTo(4));
		}

		private static void VerifyLinkerArgs(string linkerPath, EmbedInfo[] expectedResources)
		{
			var index = InstrumentedProjectLocalizer.LinkerPath.IndexOf(linkerPath);
			Assert.That(index, Is.GreaterThanOrEqualTo(0), $"LinkerPath not found: {linkerPath}");
			Assert.That(InstrumentedProjectLocalizer.LinkerCulture[index], Is.EqualTo(LocaleEs));
			Assert.That(InstrumentedProjectLocalizer.LinkerFileVersion[index], Is.EqualTo("8.4.2.1234"));
			Assert.That(InstrumentedProjectLocalizer.LinkerProductVersion[index], Is.EqualTo("8.4.2 beta 2"));
			Assert.That(InstrumentedProjectLocalizer.LinkerVersion[index], Is.EqualTo("8.4.2.*"));
			Assert.That(InstrumentedProjectLocalizer.LinkerAlArgs[index], Is.StringContaining("\"8.4.2 beta 2\""));
			var embeddedResources = InstrumentedProjectLocalizer.LinkerResources[index];
			Assert.That(embeddedResources.Count, Is.EqualTo(expectedResources.Length));
			foreach (var resource in expectedResources)
				Assert.That(embeddedResources, Has.Member(resource));
		}

		/// <summary>
		/// Verify that the specified resx file has had its translated version copied to
		/// ${dir.fwoutput}/${language}/${partialDir}/${fileName}.${language}.resx. That is:
		/// The folder is Output/[locale]/[the path from Src to the folder that has the resx]
		/// The file name is [the root namespace from the csproj].[the original filename (no extension)].[locale].resx
		/// </summary>
		private void VerifyExpectedResx(string folder, string filename, string rootNamespace)
		{
			const string locale = "es";
			var partialDir = folder.Substring(m_sut.SrcFolder.Length);
			var expectedFolder = Path.Combine(m_sut.OutputFolder, locale) + partialDir; // Todo: Linux?
			var expectedFileName = rootNamespace + "." + filename + "." + locale + ".resx";
			var expectedPath = Path.Combine(expectedFolder, expectedFileName);
			Assert.That(File.Exists(expectedPath), $"should exist: {expectedPath}");
			var doc = XDocument.Load(expectedPath);
			var translation = doc.Descendants("value").First().Value;
			// We generate .resx files where the first (currently only) Value element is the child of a data element which
			// the stylesheet should try to translate. We give it the contents SampleStringEn (see CreateResx).
			// The generated PO file (see Setup() is configured to translate this to the string below.
			// This confirms that the transformation is actually doing localization.
			Assert.That(translation, Is.EqualTo(SampleStringEs));
		}

		private void VerifyExpectedResGenArgs(string folder, string filename, string rootNamespace)
		{
			string locale = "es";
			var partialDir = folder.Substring(m_sut.SrcFolder.Length);
			var expectedFolder = Path.Combine(m_sut.OutputFolder, locale) + partialDir; // Todo: Linux?
			var expectedResxName = rootNamespace + "." + filename + "." + locale + ".resx";
			var expectedResourceName = Path.ChangeExtension(expectedResxName, "resources");
			var expectedResxPath = Path.Combine(expectedFolder, expectedResxName);
			var expectedResourcePath = Path.Combine(expectedFolder, expectedResourceName);
			Assert.That(InstrumentedProjectLocalizer.ResGenOutputPaths, Has.Member(expectedResourcePath));
			Assert.That(InstrumentedProjectLocalizer.ResGenResxPaths, Has.Member(expectedResxPath));
			Assert.That(InstrumentedProjectLocalizer.ResGenOriginalFolders, Has.Member(folder));
		}

		[Test]
		public void SelectsCorrectProjects()
		{
			FullSetup();

			m_sut.Execute();
			List<string> projects = m_sut.GetProjectFolders();
			Assert.That(projects.Contains(m_FieldWorksFolder));
			Assert.That(projects.Contains(m_xCoreFolder));
			Assert.That(projects.Contains(m_xCoreInterfacesFolder));
			Assert.That(projects.Contains(m_sideBarFolder), Is.False);
			Assert.That(projects.Contains(m_FieldWorksTestsFolder), Is.False);
			Assert.That(projects.Contains(m_commonFolder), Is.False);
			Assert.That(projects.Contains(m_FieldWorksPropertiesFolder), Is.False); // Review: we want to do the resx here, but it isn't a true project folder.
		}

		[Test]
		public void BadBraceLetterReported()
		{
			var badResXFilePath = SimpleSetupWithResX(LocaleGe, "test {0}", "test {o}");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badResXFilePath));
		}

		[Test]
		public void MismatchedFinalBraceReported()
		{
			var badResXFilePath = SimpleSetupWithResX(LocaleGe, "test {0}", "test {0{");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badResXFilePath));
		}

		[Test]
		public void MismatchedFinalBraceInPrecedingReported()
		{
			var badResXFilePath = SimpleSetupWithResX(LocaleGe, "{0} test {1}", "{0 test {1}");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badResXFilePath));
		}

		[Test]
		public void MismatchedInitialBraceReported()
		{
			var badResXFilePath = SimpleSetupWithResX(LocaleGe, "test {0}", "test }3}");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badResXFilePath));
		}

		[Test]
		public void MismatchedInitialBraceInFollowingReported()
		{
			var badResXFilePath = SimpleSetupWithResX(LocaleGe, "{0} test {1}", "{0} test 1}");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badResXFilePath));
		}

		[Test]
		public void MissingOpenBraceReported()
		{
			var badResXFilePath = SimpleSetupWithResX(LocaleGe, "test {0}", "test 0}");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badResXFilePath));
		}

		[Test]
		[Ignore("not implemented")]
		public void InsideOutBracesReported()
		{
			var badResXFilePath = SimpleSetupWithResX(LocaleGe, "test {0} test", "test }0{ text");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badResXFilePath));
		}

		[Test]
		[Ignore("Crowdin does this better than we used to, and it's now harder for us to match localized with original strings")]
		// ENHANCE (Hasso) 2019.12: Crowdin warns localizers of both extra and missings args (only when the original string has args). Should we?
		// see <c>Localizer.CheckMsgidAndMsgstr</c>
		public void ExtraStringArgInMsgStrReported()
		{
			var badResXFilePath = SimpleSetupWithResX(LocaleGe, "test {0} {1}", "test {2} {1} {0}");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badResXFilePath));
		}

		[Test]
		[Ignore("not yet implemented")]
		public void AddedOrMissingStringsReported()
		{
			// TODO (Hasso) 2019.12: test the following cases:
			// - a resx file has not been localized
			// - a resx file is missing strings that the original has
			// - a resx file has strings that the original does not
			throw new NotImplementedException();
		}

		[Test]
		public void MissingFinalBraceReported()
		{
			var badResXFilePath = SimpleSetupWithResX(LocaleGe, "test {0}", "test {0 things");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badResXFilePath));
		}

		[Test]
		public void ArgOrderChanged_OK()
		{
			SimpleSetupWithResX(LocaleGe, "test {0} {1}", "{1} le'Test {0}");

			var result = m_sut.Execute();

			Assert.That(result, Is.True, m_sut.ErrorMessages);
		}

		[Test]
		public void DoubleDigitArgs_OK()
		{
			SimpleSetupWithResX(LocaleGe, "test {10}", "{10} le'Test");

			var result = m_sut.Execute();

			Assert.That(result, Is.True, m_sut.ErrorMessages);
		}

		[Test]
		public void EscapedArgs_OK()
		{
			SimpleSetupWithResX(LocaleGe, "first format call replaces {0}; second replaces {{0}}.", "Erst {0}; danach {{0}}");

			var result = m_sut.Execute();

			Assert.That(result, Is.True, m_sut.ErrorMessages);
		}

		[Test]
		public void ErrorsReportedInStringsXml()
		{
			SimpleSetupFDO(LocaleGe, localizedStringsXml: "test {o}");
			var badXmlFilePath = m_sut.StringsXmlSourcePath(LocaleGe);

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badXmlFilePath));
		}

		[Test]
		public void ErrorsReportedInProjStringsResX()
		{
			SimpleSetupFDO(LocaleGe, localizedProjStrings: "test {o}");
			var badResXFilePath = Path.Combine(m_l10nFolder, "Src", "FDO", $"FDO-strings.{LocaleGe}.resx");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badResXFilePath));
		}

		[Test]
		public void MultipleCsProjFilesReported()
		{
			FullSetup();
			CreateProjectInExistingFolder(m_FieldWorksFolder, "BadProject", "BadProject");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining("FieldWorks"));

		}
	}

	internal class MockBuildEngine : IBuildEngine
	{
		#region IBuildEngine Members

		public bool BuildProjectFile(string projectFileName, string[] targetNames,
			System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs)
		{
			throw new NotImplementedException();
		}

		public int ColumnNumberOfTaskNode
		{
			get { return 0; }
		}

		public bool ContinueOnError
		{
			get { return false; }
		}

		public int LineNumberOfTaskNode
		{
			get { return 0; }
		}

		public void LogCustomEvent(CustomBuildEventArgs e)
		{
		}

		public void LogErrorEvent(BuildErrorEventArgs e)
		{
		}

		public void LogMessageEvent(BuildMessageEventArgs e)
		{
		}

		public void LogWarningEvent(BuildWarningEventArgs e)
		{
		}

		public string ProjectFileOfTaskNode
		{
			get { throw new NotImplementedException(); }
		}

		#endregion
	}
}
