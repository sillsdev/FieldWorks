// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NUnit.Framework;

namespace FwBuildTasks
{
	[TestFixture]
	public class LocalizeFieldWorksTests
	{
		InstrumentedLocalizeFieldWorks m_sut;
		private string m_rootPath;
		private string m_stringsEnPath;
		private string m_FdoFolder;
		private string m_commonFolder;
		private string m_FieldWorksFolder;
		private string m_xCoreFolder;
		private string m_xCoreInterfacesFolder;
		private string m_FieldWorksPropertiesFolder;
		private string m_FieldWorksTestsFolder;
		private string m_sideBarFolder;

		[SetUp]
		public void Setup()
		{
			m_sut = new InstrumentedLocalizeFieldWorks();
			m_rootPath = Path.Combine(Path.GetTempPath(), "XXTestRoot");
			m_sut.RootDirectory = m_rootPath;
			// wipe out anything left from last time
			if (Directory.Exists(m_rootPath))
				Directory.Delete(m_rootPath, true);
			Directory.CreateDirectory(m_rootPath);
			Directory.CreateDirectory(m_sut.PoFileDirectory);

			CreateTestPoFile("es", " first", "A browse view {0}{0:F1}", "Una vista examinar{0}{0:F1}", " second", "A category", "Una categoría",
				". /Language Explorer/Configuration/Lexicon/areaConfiguration.xml::/root/menuAddOn/menu/item/@label", "A_llomorph", "A_lomorfo",
				". /Language Explorer/Configuration/Parts/MorphologyParts.xml::/PartInventory/bin/part[@id=\"MoAlloAdhocProhib-Jt-Type\"]/lit", "lit1", "litTrans1",
				". /Language Explorer/Configuration/ContextHelp.xml::/strings/item[@id=\"AffixForm\"]", "An allomorph of the affix.", "Un alomorfo del afijo.");
			CreateStringsXml();

			CreateProjects();

			CreateAssemblyInfo();
		}

		// Create a minimal CommonAssemblyInfo.cs file with just the lines we care about.
		private void CreateAssemblyInfo()
		{
			var writer = new StreamWriter(m_sut.AssemblyInfoPath, false, Encoding.UTF8);
			writer.WriteLine("[assembly: AssemblyFileVersion(\"8.4.2.1234\")]");
			writer.WriteLine("[assembly: AssemblyInformationalVersionAttribute(\"8.4.2 beta 2\")]");
			writer.WriteLine("[assembly: AssemblyVersion(\"8.4.2.*\")]");
			writer.Close();
		}

		// We want to create a hierarchy of projects under Src.
		// To test certain cases, we need
		// - a folder with a .csproj ("FDO").
		// - a folder with no .csproj ("Common").
		// - a child folder with a .csproj ("Common/FieldWorks").
		// - a child of a .csproj that has no .csproj (but will have resx files): ("Common/FieldWorks/Properties")
		// - a folder whose name ends in "Tests"
		// - a folder whose name is exactly SidebarLibrary
		// - a child of a folder with .csproj that has its own .csproj (xCore and xCore/xCoreInterfaces).
		private void CreateProjects()
		{
			var m_srcFolder = m_sut.SrcFolder;
			m_FdoFolder = CreateProject(m_srcFolder, "FDO");
			m_commonFolder = CreateFolder(m_srcFolder, "Common");
			m_FieldWorksFolder = CreateProject(m_commonFolder, "FieldWorks");
			m_FieldWorksPropertiesFolder = CreateFolder(m_FieldWorksFolder, "Properties");
			m_FieldWorksTestsFolder = CreateProject(m_FieldWorksFolder, "FieldWorksTests");
			m_sideBarFolder = CreateProject(m_srcFolder, "SidebarLibrary");
			m_xCoreFolder = CreateProject(m_srcFolder, "xCore");
			m_xCoreInterfacesFolder = CreateProject(m_xCoreFolder, "xCoreInterfaces", "xCoreIntName");
			CreateResX(m_FieldWorksPropertiesFolder, "more strings");
			CreateResX(m_FieldWorksFolder, "strings");
		}

		string CreateFolder(string parent, string name)
		{
			var result = Path.Combine(parent, name);
			Directory.CreateDirectory(result);
			return result;
		}
		/// <summary>
		/// Create a minimal convertible project in the specified folder (with default assembly name same as project).
		/// </summary>
		/// <param name="folder"></param>
		string CreateProject(string parent, string name)
		{
			return CreateProject(parent, name, name);
		}

		/// <summary>
		/// Create a minimal convertible project in the specified folder.
		/// </summary>
		/// <param name="folder"></param>
		string CreateProject(string parent, string name, string assemblyName)
		{
			var result = CreateFolder(parent, name);
			CreateProjectInExistingFolder(result, name, assemblyName);
			return result;
		}

		private void CreateProjectInExistingFolder(string folder, string project, string assemblyName)
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
			CreateResX(folder, project + "-strings");
		}

		private void CreateResX(string folder, string fileName)
		{
			var doc = new XDocument(
				new XElement("root",
					new XElement("data",
						new XAttribute("name", "ksTest"),
						new XElement("value",
							new XText("A category")))));
			string projectPath = Path.ChangeExtension(Path.Combine(folder, fileName), "resx");
			doc.Save(projectPath);
		}

		private string CreateTestPoFile(string locale, params string[] data)
		{
			Assert.That(data.Length % 3, Is.EqualTo(0));
			var poPath = Path.Combine(m_sut.PoFileDirectory, LocalizeFieldWorks.PoFileLeadIn + locale + LocalizeFieldWorks.PoFileExtension);
			var writer = new StreamWriter(poPath, false, Encoding.UTF8);
			for (int i = 0; i < data.Length; i += 3 )
			{
				writer.WriteLine("#" + data[i]);
				writer.WriteLine("msgid \"" + data[i + 1] + "\"");
				writer.WriteLine("msgstr \"" + data[i + 2] + "\"");
				writer.WriteLine();

			}
			writer.Close();
			return poPath;
		}

		/// <summary>
		/// Create some test data in DistFiles/Language Explorer/Configuration/strings-en.txt which we can try to localize.
		/// </summary>
		void CreateStringsXml()
		{
			m_stringsEnPath = m_sut.StringsEnPath;
			var doc = new XDocument(
				new XElement("strings",
					new XElement("group",
						new XAttribute("id", "Misc"),
						new XElement("string",
							new XAttribute("id", "test"),
							new XAttribute("txt", "try out")),
						new XElement("string",
							new XAttribute("id", "fix"),
							new XAttribute("txt", "A category")))));
			Directory.CreateDirectory(Path.GetDirectoryName(m_stringsEnPath));
			doc.Save(m_stringsEnPath);
		}

		[Test]
		public void DoIt()
		{
			var result = m_sut.Execute();

			Assert.That(result, Is.True);
			var stringsEsPath = m_sut.StringsXmlPath("es");
			Assert.That(File.Exists(stringsEsPath));
			var doc = XDocument.Load(stringsEsPath);

			// Any txt attribute in the input file whose value matches a msgid in the PO file should be translated.
			// Note: I don't think it's essential that the added elements come last. It may be necessary at some point
			// to enhance this test to look for the "Misc" group, like the later tests look for the added ones.
			var translation = doc.Root.Element("group").Elements("string").ToList()[1].Attribute("txt").Value;
			Assert.That(translation, Is.EqualTo("Una categoría"));

			// The output strings.xml should have a group with id LocalizedAttributes.
			// It should have a text item for each thing in the PO file that looks like the Allomorph entry in the example
			// (and a few other cases...this test is not yet comprehensive)
			// with the translation.
			VerifyGroup(doc, "LocalizedAttributes", "A_llomorph", "A_lomorfo");
			// The output strings.xml should have a group with id LocalizedLiterals.
			// It should have a text item for each thing in the PO file that looks like the Literal entry in the example
			// with the translation.
			VerifyGroup(doc, "LocalizedLiterals", "lit1", "litTrans1");
			// The output strings.xml should have a group with id LocalizedContextHelp.
			// It should have a text item for each thing in the PO file that looks like the Context help entry in the example
			// with the translation. Here the ID is taken from the comment.
			VerifyGroup(doc, "LocalizedContextHelp", "AffixForm", "Un alomorfo del afijo.");

			// We're checking an intermediate here, but it's almost impossible to verify the final output,
			// so checking some steps in the process is about the best we can do to check the right things
			// are happening.
			// We should generate es.xml in the Output directory (a form of the PO file suitable for including in an XSLT transform).
			var esXmlPath = m_sut.XmlPoFilePath("es");
			Assert.That(File.Exists(esXmlPath));
			doc = XDocument.Load(esXmlPath);
			Assert.That(doc.Root.Name.LocalName, Is.EqualTo("messages"));
			var firstMsg = doc.Root.Element("msg");
			Assert.That(firstMsg, Is.Not.Null);
			var key = firstMsg.Element("key");
			Assert.That(key, Is.Not.Null);
			Assert.That(key.Value, Is.EqualTo("A browse view {0}{0:F1}"));
			var str = firstMsg.Element("str");
			Assert.That(str, Is.Not.Null);
			Assert.That(str.Value, Is.EqualTo("Una vista examinar{0}{0:F1}"));
			var comment = firstMsg.Element("comment");
			Assert.That(comment, Is.Not.Null);
			Assert.That(comment.Value, Is.EqualTo("first"));

			// XML transformation should procude for each resx a file in  ${dir.fwoutput}/${language}/${partialDir}/
			// whose name is ${RootNamespace}.${fileName}.${language}.resx. That is,
			// The folder is
			// - Output
			// - plus the locale
			// - plus the path from Src to the folder that has the resx
			// The file name is
			// - the root namespace from the .csproj
			// - plus the name of the resx
			// - plus the locale again
			// - plus .resx
			VerifyExpectedResx(m_FieldWorksFolder, "FieldWorks-strings", "SIL.FieldWorks");
			VerifyExpectedResx(m_FieldWorksPropertiesFolder, "Properties.more strings", "SIL.FieldWorks");
			VerifyExpectedResx(m_FdoFolder, "FDO-strings", "SIL.FDO");

			VerifyExpectedResGenArgs(m_FieldWorksFolder, "FieldWorks-strings", "SIL.FieldWorks");
			VerifyExpectedResGenArgs(m_FieldWorksPropertiesFolder, "Properties.more strings", "SIL.FieldWorks");
			VerifyExpectedResGenArgs(m_FdoFolder, "FDO-strings", "SIL.FDO");

			// The Assembly Linker should be run (once for each desired project) with expected arguments.
			Assert.That(m_sut.LinkerPath.Count, Is.EqualTo(4));
			VerifyLinkerArgs(Path.Combine(m_sut.OutputFolder, "Release", "es", "FDO.resources.dll"), new EmbedInfo[] {
				new EmbedInfo(Path.Combine(m_sut.OutputFolder, "es", "FDO", "SIL.FDO.FDO-strings.es.resources"),
							  "SIL.FDO.FDO-strings.es.resources")
				});
			VerifyLinkerArgs(Path.Combine(m_sut.OutputFolder, "Release", "es", "FieldWorks.resources.dll"), new EmbedInfo[] {
				new EmbedInfo(Path.Combine(m_sut.OutputFolder, "es", "Common", "FieldWorks", "SIL.FieldWorks.FieldWorks-strings.es.resources"),
							  "SIL.FieldWorks.FieldWorks-strings.es.resources"),
				new EmbedInfo(Path.Combine(m_sut.OutputFolder, "es", "Common", "FieldWorks", "SIL.FieldWorks.strings.es.resources"),
							  "SIL.FieldWorks.strings.es.resources"),
				new EmbedInfo(Path.Combine(m_sut.OutputFolder, "es", "Common", "FieldWorks", "Properties", "SIL.FieldWorks.Properties.more strings.es.resources"),
							  "SIL.FieldWorks.Properties.more strings.es.resources")
				});
			VerifyLinkerArgs(Path.Combine(m_sut.OutputFolder, "Release", "es", "xCoreIntName.resources.dll"), new EmbedInfo[] {
				new EmbedInfo(Path.Combine(m_sut.OutputFolder, "es", "xCore", "xCoreInterfaces", "SIL.xCoreInterfaces.xCoreInterfaces-strings.es.resources"),
							  "SIL.xCoreInterfaces.xCoreInterfaces-strings.es.resources")
				});
		}

		private void VerifyLinkerArgs(string linkerPath, EmbedInfo[] expectedResources )
		{
			string locale = "es";
			var index = m_sut.LinkerPath.IndexOf(linkerPath);
			Assert.That(index >= 0);
			Assert.That(m_sut.LinkerCulture[index], Is.EqualTo(locale));
			Assert.That(m_sut.LinkerFileVersion[index], Is.EqualTo("8.4.2.1234"));
			Assert.That(m_sut.LinkerProductVersion[index], Is.EqualTo("8.4.2 beta 2"));
			Assert.That(m_sut.LinkerVersion[index], Is.EqualTo("8.4.2.*"));
			Assert.That(m_sut.LinkerAlArgs[index], Is.StringContaining("\"8.4.2 beta 2\""));
			var embeddedResources = m_sut.LinkerResources[index];
			Assert.That(embeddedResources.Count, Is.EqualTo(expectedResources.Length));
			foreach (var resource in expectedResources)
				Assert.That(embeddedResources, Has.Member(resource));
		}

		private void VerifyExpectedResx(string folder, string filename, string rootNamespace)
		{
			string locale = "es";
			var partialDir = folder.Substring(m_sut.SrcFolder.Length);
			var expectedFolder = Path.Combine(m_sut.OutputFolder, locale) + partialDir; // Todo: Linux?
			var expectedFileName = rootNamespace + "." + filename + "." + locale + ".resx";
			var expectedPath = Path.Combine(expectedFolder, expectedFileName);
			Assert.That(File.Exists(expectedPath));
			var doc = XDocument.Load(expectedPath);
			var translation = doc.Descendants("value").First().Value;
			// We generate .resx files where the first (currently only) Value element is the child of a data element which
			// the stylesheet should try to translate. We give it the contents "A category" (see CreateResx).
			// The generated PO file (see Setup() is configured to translate this to the string below.
			// This confirms that the transformation is actually doing localization.
			Assert.That(translation, Is.EqualTo("Una categoría"));
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
			Assert.That(m_sut.ResGenOutputPaths, Has.Member(expectedResourcePath));
			Assert.That(m_sut.ResGenResxPaths, Has.Member(expectedResxPath));
			Assert.That(m_sut.ResGenOriginalFolders, Has.Member(folder));
		}

		private static void VerifyGroup(XDocument doc, string groupName, string expectedId, string expectedTxt)
		{
			XElement localAttrGroup = doc.Root.Elements("group").FirstOrDefault(x => x.Attribute("id").Value == groupName);
			Assert.That(localAttrGroup, Is.Not.Null);
			var stringItem = localAttrGroup.Element("string");
			Assert.That(stringItem, Is.Not.Null);

			Assert.That(stringItem.Attribute("id").Value, Is.EqualTo(expectedId));

			Assert.That(stringItem.Attribute("txt").Value, Is.EqualTo(expectedTxt));
		}

		[Test]
		public void SelectsCorrectProjects()
		{
			m_sut.Execute();
			List<string> projects= m_sut.GetProjectFolders();
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
			string badPoFile = CreateTestPoFile("ge", "test", "test {0}", "test {o}");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badPoFile));
		}

		[Test]
		public void MisMatchedFinalBraceReported()
		{
			string badPoFile = CreateTestPoFile("ge", "test", "test {0}", "test {0{");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badPoFile));
		}
		[Test]
		public void MisMatchedInitialBraceReported()
		{
			string badPoFile = CreateTestPoFile("ge", "test", "test {0}", "test }3}");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badPoFile));
		}

		[Test]
		public void MissingOpenBraceReported()
		{
			string badPoFile = CreateTestPoFile("ge", "test", "test {0}", "test 0}");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badPoFile));
		}

		[Test]
		public void ExtraStringArgInMsgStrreported()
		{
			string badPoFile = CreateTestPoFile("ge", "test", "test {0} {1}", "test {2} {1} {0}");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badPoFile));
		}

		[Test]
		public void MissingMsgIdOpenQuoteReported()
		{
			var poPath = Path.Combine(m_sut.PoFileDirectory,
				LocalizeFieldWorks.PoFileLeadIn + "es" + LocalizeFieldWorks.PoFileExtension);
			var writer = new StreamWriter(poPath, false, Encoding.UTF8);

			writer.WriteLine("# comment");
			writer.WriteLine("msgid idWithNoOpenQuote\"");
			writer.WriteLine("msgstr \"translation\"");
			writer.Close();

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(poPath));
		}

		[Test]
		public void MissingStrReported()
		{
			var poPath = Path.Combine(m_sut.PoFileDirectory,
				LocalizeFieldWorks.PoFileLeadIn + "es" + LocalizeFieldWorks.PoFileExtension);
			var writer = new StreamWriter(poPath, false, Encoding.UTF8);

			writer.WriteLine("# comment");
			writer.WriteLine("msgid \"id no str\"");
			writer.WriteLine("msgid \"id2\"");
			writer.WriteLine("msgstr \"translation\"");
			writer.Close();

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(poPath));
		}

		[Test]
		public void MissingStrReportedWithCommentsBetween()
		{
			var poPath = Path.Combine(m_sut.PoFileDirectory,
				LocalizeFieldWorks.PoFileLeadIn + "es" + LocalizeFieldWorks.PoFileExtension);
			var writer = new StreamWriter(poPath, false, Encoding.UTF8);

			writer.WriteLine("# comment");
			writer.WriteLine("msgid \"id no str\"");
			writer.WriteLine("# comment");
			writer.WriteLine("");
			writer.WriteLine("msgid \"id2\"");
			writer.WriteLine("msgstr \"translation\"");
			writer.Close();

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(poPath));
		}

		[Test]
		public void MissingKeyReported()
		{
			var poPath = Path.Combine(m_sut.PoFileDirectory,
				LocalizeFieldWorks.PoFileLeadIn + "es" + LocalizeFieldWorks.PoFileExtension);
			var writer = new StreamWriter(poPath, false, Encoding.UTF8);

			writer.WriteLine("# comment");
			writer.WriteLine("msgid \"id\"");
			writer.WriteLine("msgstr \"translation\"");
			writer.WriteLine("# comment");
			writer.WriteLine("");
			writer.WriteLine("msgstr \"translation wit no id\"");
			writer.Close();

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(poPath));
		}

		[Test]
		public void DuplicateKeyReported()
		{
			var poPath = Path.Combine(m_sut.PoFileDirectory,
				LocalizeFieldWorks.PoFileLeadIn + "es" + LocalizeFieldWorks.PoFileExtension);
			var writer = new StreamWriter(poPath, false, Encoding.UTF8);

			writer.WriteLine("# comment");
			writer.WriteLine("msgid \"id\"");
			writer.WriteLine("msgstr \"translation\"");
			writer.WriteLine("msgid \"id\"");
			writer.WriteLine("msgstr \"another translation of the same id\"");
			writer.Close();

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(poPath));
		}

		[Test]
		public void MissingMsgIdCloseQuoteReported()
		{
			var poPath = Path.Combine(m_sut.PoFileDirectory,
				LocalizeFieldWorks.PoFileLeadIn + "es" + LocalizeFieldWorks.PoFileExtension);
			var writer = new StreamWriter(poPath, false, Encoding.UTF8);

			writer.WriteLine("# comment");
			writer.WriteLine("msgid \"idWithNoCloseQuote");
			writer.WriteLine("msgstr \"translation\"");
			writer.Close();

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(poPath));
		}

		[Test]
		public void MissingMsgStrOpenQuoteReported()
		{
			var poPath = Path.Combine(m_sut.PoFileDirectory,
				LocalizeFieldWorks.PoFileLeadIn + "es" + LocalizeFieldWorks.PoFileExtension);
			var writer = new StreamWriter(poPath, false, Encoding.UTF8);

			writer.WriteLine("# comment");
			writer.WriteLine("msgid \"id\"");
			writer.WriteLine("msgstr translation With No Open Quote\"");
			writer.Close();

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(poPath));
		}
		[Test]
		public void MissingMsgStrCloseQuoteReported()
		{
			var poPath = Path.Combine(m_sut.PoFileDirectory,
				LocalizeFieldWorks.PoFileLeadIn + "es" + LocalizeFieldWorks.PoFileExtension);
			var writer = new StreamWriter(poPath, false, Encoding.UTF8);

			writer.WriteLine("# comment");
			writer.WriteLine("msgid \"id\"");
			writer.WriteLine("msgstr \"translation With No Close Quote");
			writer.Close();

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(poPath));
		}

		[Test]
		public void MissingFinalBraceReported()
		{
			string badPoFile = CreateTestPoFile("ge", "test", "test {0}", "test {0");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining(badPoFile));
		}

		[Test]
		public void MultipleCsProjFilesReported()
		{
			CreateProjectInExistingFolder(m_FieldWorksFolder, "BadProject", "BadProject");

			var result = m_sut.Execute();

			Assert.That(result, Is.False);
			Assert.That(m_sut.ErrorMessages, Is.StringContaining("FieldWorks"));

		}
	}

	class MockBuildEngine : IBuildEngine
	{
		#region IBuildEngine Members

		public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs)
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


	class InstrumentedLocalizeFieldWorks : LocalizeFieldWorks
	{
		public string ErrorMessages = "";

		public InstrumentedLocalizeFieldWorks()
		{
			BuildEngine = new MockBuildEngine();
		}

		/// <summary>
		/// In normal operation, this is the same as RootDirectory. In test, we find the real one, to allow us to
		/// find fixed files like LocalizeResx.xml
		/// </summary>
		internal override string RealFwRoot
		{
			get
			{
				var path = BuildUtils.GetAssemblyFolder();
				while (Path.GetFileName(path) != "Build")
					path = Path.GetDirectoryName(path);
				return Path.GetDirectoryName(path);
			}
		}

		internal override void LogError(string message)
		{
			ErrorMessages += System.Environment.NewLine + message;
		}

		public List<string> LinkerPath = new List<string>();
		public List<string> LinkerCulture = new List<string>();
		public List<string> LinkerFileVersion = new List<string>();
		public List<string> LinkerProductVersion = new List<string>();
		public List<string> LinkerVersion = new List<string>();
		public List<List<EmbedInfo>> LinkerResources = new List<List<EmbedInfo>>();
		public List<string> LinkerAlArgs = new List<string>();
		internal override bool RunAssemblyLinker(string outputDllPath, string culture, string fileversion, string productVersion, string version, List<EmbedInfo> resources)
		{
			LinkerPath.Add(outputDllPath);
			LinkerCulture.Add(culture);
			LinkerFileVersion.Add(fileversion);
			LinkerProductVersion.Add(productVersion);
			LinkerVersion.Add(version);
			LinkerResources.Add(resources);
			LinkerAlArgs.Add(BuildLinkerArgs(outputDllPath, culture, fileversion, productVersion, version, resources));
			return true;
		}

		public List<string> ResGenOutputPaths = new List<string>();
		public List<string> ResGenResxPaths = new List<string>();
		public List<string> ResGenOriginalFolders = new List<string>();

		internal override bool RunResGen(string outputResourcePath, string resxPath, string originalFolder)
		{
			ResGenOutputPaths.Add(outputResourcePath);
			ResGenResxPaths.Add(resxPath);
			ResGenOriginalFolders.Add(originalFolder);
			return true;
		}
	}
}
