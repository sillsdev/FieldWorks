// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace LexEdDllTests
{
	[TestFixture]
	public class MotifApplyConfigurationTests
	{
		[Test]
		public void MainDefinesTheGlobalToolsCommandAndItsListener()
		{
			var repositoryRoot = Directory.GetParent(FwDirectoryFinder.SourceDirectory).FullName;
			var configRoot = Path.Combine(repositoryRoot, "DistFiles", "Language Explorer", "Configuration");
			var areaPath = Path.Combine(configRoot, "Lexicon", "areaConfiguration.xml");
			var area = XDocument.Load(areaPath);
			Assert.That(area.Descendants("command").Any(element =>
				(string)element.Attribute("id") == "CmdApplyMotifChanges"), Is.False);
			Assert.That(area.Descendants("menu").Any(element =>
				(string)element.Attribute("id") == "Tools" && element.Elements("item").Any(item =>
					(string)item.Attribute("command") == "CmdApplyMotifChanges")), Is.False);

			var main = XDocument.Load(Path.Combine(configRoot, "Main.xml"));
			var command = main.Descendants("command").SingleOrDefault(element =>
				(string)element.Attribute("id") == "CmdApplyMotifChanges");

			Assert.That(command, Is.Not.Null);
			Assert.That((string)command.Attribute("label"), Is.EqualTo("_Apply Motif changes..."));
			Assert.That((string)command.Attribute("message"), Is.EqualTo("ApplyMotifChanges"));
			var toolsMenu = main.Descendants("menu").Single(element =>
				(string)element.Attribute("id") == "Tools");
			Assert.That(toolsMenu.Elements("item").Any(item =>
				(string)item.Attribute("command") == "CmdApplyMotifChanges"), Is.True);
			Assert.That(main.Descendants("include").Any(element =>
				(string)element.Attribute("path") == "Lexicon/areaConfiguration.xml" &&
				((string)element.Attribute("query") ?? string.Empty).Contains("root/commands")), Is.True);
			Assert.That(main.Descendants("include").Any(element =>
				(string)element.Attribute("path") == "Lexicon/areaConfiguration.xml" &&
				((string)element.Attribute("query") ?? string.Empty).Contains("menu[@id='Tools']")), Is.True);
			Assert.That(main.Descendants("listener").Any(element =>
				(string)element.Attribute("assemblyPath") == "LexEdDll.dll" &&
				(string)element.Attribute("class") == "SIL.FieldWorks.XWorks.LexEd.FLExBridgeListener"), Is.True);
		}
	}
}
