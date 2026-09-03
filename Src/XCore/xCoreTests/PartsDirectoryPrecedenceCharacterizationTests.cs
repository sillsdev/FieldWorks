// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace XCore
{
	/// <summary>
	/// Pins the real merge precedence between DistFiles/Parts (build-generated defaults) and
	/// Language Explorer/Configuration/Parts (hand-authored customizations), reproducing exactly
	/// how LayoutCache.InitializePartInventories wires production Inventory objects
	/// (Src/Common/Controls/XMLViews/LayoutCache.cs:96-131). Written against the real shipped
	/// files so the FwAvalonia PartsInventory port (Src/Common/FwAvalonia/ViewDefinition) can
	/// copy
	/// a proven precedence instead of guessing one. See LT-22772.
	/// </summary>
	[TestFixture]
	public class PartsDirectoryPrecedenceCharacterizationTests
	{
		/// <summary>
		/// DistFiles/Parts is always searched first (Inventory ctor), then Configuration/Parts is
		/// added as the one customInventoryPath -- the same order production code uses.
		/// </summary>
		private static Inventory BuildProductionPartInventory()
		{
			var partDirectory = Path.Combine(FwDirectoryFinder.FlexFolder,
				Path.Combine("Configuration", "Parts"));
			var keyAttrs = new Dictionary<string, string[]> { ["part"] = new[] { "id" } };
			return new Inventory(new[] { partDirectory }, "*Parts.xml", "/PartInventory/bin/*",
				keyAttrs, "PartsDirectoryPrecedenceCharacterizationTests", Path.GetTempPath());
		}

		[Test]
		public void HandAuthoredConfigurationPartsWinsOverGeneratedPartsOnIdCollision()
		{
			// CmPossibility-Detail-Status collides between the two directories; if generated
			// wins, the hand-authored status-list chooser silently disappears (231 ids collide).
			var inventory = BuildProductionPartInventory();

			var node = inventory.GetElement("part", new[] { "CmPossibility-Detail-Status" });

			Assert.That(node, Is.Not.Null);
			var slice = node.SelectSingleNode("slice");
			Assert.That(slice, Is.Not.Null);
			Assert.That(slice.Attributes["editor"]?.Value, Is.EqualTo("possAtomicReference"),
				"Configuration/Parts (added after DistFiles/Parts) must win: legacy "
				+ "Inventory.AddNode replaces the table entry on every later occurrence of the same key.");
		}

		[Test]
		public void GeneratedOnlyPartIsStillReachable()
		{
			// LexSense-Detail-Pictures exists only in the generated inventory; legacy still
			// resolves it because DistFiles/Parts is unconditionally in the search path
			// (Inventory.cs:174).
			var inventory = BuildProductionPartInventory();

			var node = inventory.GetElement("part", new[] { "LexSense-Detail-Pictures" });

			Assert.That(node, Is.Not.Null,
				"LexSense-Detail-Pictures ships only in DistFiles/Parts/GeneratedParts.xml.");
		}
	}
}
