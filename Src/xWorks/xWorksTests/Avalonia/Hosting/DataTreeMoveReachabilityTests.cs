// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Move (message DataTreeMove) is answered for an affix slot and an affix template only:
	/// the Avalonia view asks for the category with a chooser, and WinForms drops a tree on
	/// the row. These tests pin that only the two Grammar commands,
	/// <c>CmdDataTree-Move-POS-AffixSlot</c> and <c>CmdDataTree-Move-POS-AffixTemplate</c>,
	/// send the message, so a new command that sends it fails here until both views answer it.
	/// </summary>
	[TestFixture]
	public class DataTreeMoveReachabilityTests
	{
		private static string LexiconConfigDir => FwDirectoryFinder.GetCodeSubDirectory(
			@"Language Explorer\Configuration\Lexicon");

		[Test]
		public void DataTreeMoveCommand_IsNotDeclaredInTheLexiconConfiguration()
		{
			var offenders = Directory.GetFiles(LexiconConfigDir, "*.xml", SearchOption.AllDirectories)
				.SelectMany(path => LoadCommands(path)
					.Where(c => (string)c.Attribute("message") == "DataTreeMove")
					.Select(c => Path.GetFileName(path) + ":" + (string)c.Attribute("id")))
				.ToList();

			Assert.That(offenders, Is.Empty,
				"No Lexicon command may send DataTreeMove: both detail views answer it only for an affix "
				+ "slot or an affix template. Offenders: " + string.Join(", ", offenders));
		}

		[Test]
		public void DataTreeMoveCommand_IsConfinedToTheGrammarAffixCommands()
		{
			// The only DataTreeMove declarations in the shipped configuration are the two Grammar
			// commands.
			var configRoot = FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer\Configuration");
			var ids = Directory.GetFiles(configRoot, "*.xml", SearchOption.AllDirectories)
				.SelectMany(LoadCommands)
				.Where(c => (string)c.Attribute("message") == "DataTreeMove")
				.Select(c => (string)c.Attribute("id"))
				.OrderBy(id => id)
				.ToList();

			Assert.That(ids, Is.EquivalentTo(new[]
			{
				"CmdDataTree-Move-POS-AffixSlot",
				"CmdDataTree-Move-POS-AffixTemplate"
			}), "DataTreeMove is answered only for an affix slot or an affix template; a new command that "
				+ "sends it needs an answer in both detail views.");
		}

		private static System.Collections.Generic.IEnumerable<XElement> LoadCommands(string path)
		{
			XDocument doc;
			try
			{
				doc = XDocument.Load(path);
			}
			catch
			{
				return Enumerable.Empty<XElement>();
			}
			return doc.Descendants("command");
		}
	}
}
