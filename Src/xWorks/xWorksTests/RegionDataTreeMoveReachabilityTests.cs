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
	/// Task C1 triage — <c>DTMenuHandler.OnDataTreeMove</c> (message="DataTreeMove") adds a
	/// <c>TreeCombo</c> to <c>CurrentSlice.Control</c> and drops a popup tree on it. On the Avalonia
	/// surface the command target is the HIDDEN, detached, off-screen adapter DataTree, so that combo
	/// would never appear and the move could not complete.
	///
	/// DECISION: no suppression code is added because the command is NOT REACHABLE from the lexical
	/// entry view. <c>message="DataTreeMove"</c> is declared only by the two Grammar-area commands
	/// <c>CmdDataTree-Move-POS-AffixSlot</c> and <c>CmdDataTree-Move-POS-AffixTemplate</c>
	/// (Configuration/Grammar/DataTreeInclude.xml), and those commands appear only in Grammar menus —
	/// never in the Lexicon configuration the entry view composes its right-click menus from. (The
	/// lexicon "Move" commands route through <c>MoveUpObjectInSequence</c>/<c>MoveDownObjectInSequence</c>
	/// and the per-field override layer, neither of which is the TreeCombo path.)
	///
	/// These tests LOCK that reachability conclusion so a future Lexicon-config change cannot silently
	/// introduce the broken-on-Avalonia <c>DataTreeMove</c> command into the entry-view menus without
	/// tripping a red test (which would then require routing it through a real chooser or suppressing it
	/// on the Avalonia menu).
	/// </summary>
	[TestFixture]
	public class RegionDataTreeMoveReachabilityTests
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
				"No Lexicon command may use message=\"DataTreeMove\": that handler (DTMenuHandler.OnDataTreeMove) "
				+ "adds a TreeCombo to CurrentSlice.Control, which cannot work against the Avalonia surface's "
				+ "hidden, detached adapter DataTree. If a Move command is genuinely needed here, route it "
				+ "through a real chooser or suppress it on the Avalonia menu — do not add the TreeCombo path. "
				+ "Offenders: " + string.Join(", ", offenders));
		}

		[Test]
		public void DataTreeMoveCommand_IsConfinedToTheGrammarAffixCommands()
		{
			// Sanity-anchor the conclusion: the only DataTreeMove declarations in the shipped configuration
			// are the two Grammar affix-slot/template commands. If this set changes, the triage above must
			// be revisited (a new surface might now reach the TreeCombo path).
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
			}), "DataTreeMove must remain confined to the Grammar affix commands; a new declaration means a "
				+ "new surface might reach the unsupported TreeCombo path and the C1 triage must be redone.");
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
