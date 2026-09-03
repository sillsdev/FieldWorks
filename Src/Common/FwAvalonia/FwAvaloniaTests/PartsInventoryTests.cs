// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Pins <see cref="PartsInventory"/>'s two-directory search path against the real shipped
	/// files: that both directories are searched, and that a hand-authored definition wins a
	/// colliding part id. Legacy <c>Inventory</c> searches the build-generated DistFiles/Parts
	/// (GeneratedParts.xml/Generated.fwlayout, one part per model field) as well as
	/// Configuration/Parts, and a part the search path misses drops its slice with no error
	/// (LT-22772).
	/// </summary>
	[TestFixture]
	public class PartsInventoryTests
	{
		private static string RepoRoot()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
			{
				dir = dir.Parent;
			}

			Assert.That(dir, Is.Not.Null, "could not locate the repo root from the test directory");
			return dir.FullName;
		}

		/// <summary>The shipped search path, resolved against the repo's DistFiles instead of an
		/// installed code directory. Order and membership stay <see
		/// cref="PartsInventory"/>'s.</summary>
		private static IReadOnlyList<string> ShippedSearchPath()
			=> PartsInventory.SearchPath(sub => Path.Combine(RepoRoot(), "DistFiles", sub));

		private static string ShippedCustomPartsDirectory() => ShippedSearchPath()[0];

		[Test]
		public void SingleDirectoryLoader_OverConfigurationPartsAlone_CannotResolveGeneratedOnlyParts()
		{
			// A one-directory path cannot see DistFiles/Parts, so a generated-only part is
			// invisible -- the failure the two-directory path exists to prevent.
			var partsXml = PartsInventory.LoadMergedPartsXml(new[] { ShippedCustomPartsDirectory() });

			Assert.That(partsXml, Does.Not.Contain("LexSense-Detail-Pictures"),
				"Configuration/Parts alone must not carry the generated-only Pictures part -- "
				+ "if this starts failing, the part was hand-authored and this test should be deleted.");
		}

		[Test]
		public void LoadMergedPartsXml_OverBothDirectories_ResolvesTheGeneratedOnlyPicturesPart()
		{
			// GREEN: PartsInventory searches both directories, so the generated-only part is
			// visible even though it has no hand-authored counterpart.
			var partsXml = PartsInventory.LoadMergedPartsXml(ShippedSearchPath());

			Assert.That(partsXml, Does.Contain("LexSense-Detail-Pictures"));
		}

		[Test]
		public void DictionaryPartResolver_OverPartsInventoryMerge_ResolvesLexSensePicturesRef()
		{
			// End-to-end: the same resolver DetailComposer/LexiconFirstSlice compile through now
			// resolves the 'Pictures' ref on LexSense, which the single-directory loader dropped.
			var partsXml = PartsInventory.LoadMergedPartsXml(ShippedSearchPath());
			Assert.That(partsXml, Is.Not.Null);

			var resolver = new DictionaryPartResolver(XElement.Parse(partsXml));

			var pictures = resolver.ResolvePart("LexSense", "detail", "Pictures");

			Assert.That(pictures, Is.Not.Null,
				"LexSense-Detail-Pictures ships only in DistFiles/Parts/GeneratedParts.xml");
		}

		/// <summary>
		/// Characterizes the precedence PartsInventory must reproduce: legacy's
		/// last-loaded-directory-wins merge (<c>Inventory.AddNode</c>/<c>InsertNodeInDoc</c>)
		/// means
		/// the hand-authored directory -- loaded second in production
		/// (<c>LayoutCache.InitializePartInventories</c>) -- overrides the generated one on a
		/// colliding id. Pinned against the real shipped files (231 ids collide) in
		/// <c>PartsDirectoryPrecedenceCharacterizationTests</c> (Src/XCore/xCoreTests), which
		/// exercises the real legacy <c>Inventory</c> class; this test exercises PartsInventory's
		/// own merge with a controlled synthetic collision so the precedence direction is
		/// unambiguous.
		/// </summary>
		[Test]
		public void LoadMergedPartsXml_OnIdCollision_HandAuthoredDirectoryWins()
		{
			var customDir = Path.Combine(Path.GetTempPath(), "PartsInventoryTests-custom-" + Guid.NewGuid());
			var generatedDir = Path.Combine(Path.GetTempPath(), "PartsInventoryTests-generated-" + Guid.NewGuid());
			Directory.CreateDirectory(customDir);
			Directory.CreateDirectory(generatedDir);
			try
			{
				File.WriteAllText(Path.Combine(generatedDir, "GeneratedParts.xml"),
					"<PartInventory><bin><part id='X-Detail-Y'><slice field='Y' editor='defaultAtomicReference'/></part></bin></PartInventory>");
				File.WriteAllText(Path.Combine(customDir, "XParts.xml"),
					"<PartInventory><bin><part id='X-Detail-Y'><slice field='Y' editor='possAtomicReference'/></part></bin></PartInventory>");

				var partsXml = PartsInventory.LoadMergedPartsXml(new[] { customDir, generatedDir });
				var resolver = new DictionaryPartResolver(XElement.Parse(partsXml));

				var resolved = resolver.ResolvePart("X", "detail", "Y");

				Assert.That(resolved, Is.Not.Null);
				Assert.That((string)resolved.Attribute("editor"), Is.EqualTo("possAtomicReference"),
					"the hand-authored (custom) directory must win the collision, matching legacy");
			}
			finally
			{
				Directory.Delete(customDir, true);
				Directory.Delete(generatedDir, true);
			}
		}

		[Test]
		public void LoadLayoutFiles_ConcatenatesBothDirectories_CustomFirst()
		{
			var searchPath = ShippedSearchPath();
			var files = PartsInventory.LoadLayoutFiles(searchPath);

			var customCount = LayoutSourceLoader.LoadLayoutFiles(searchPath[0]).Count;
			var generatedCount = LayoutSourceLoader.LoadLayoutFiles(searchPath[1]).Count;

			Assert.That(files.Count, Is.EqualTo(customCount + generatedCount));
		}

		[Test]
		public void LoadMergedPartsXml_BothDirectoriesMissing_ReturnsNull()
		{
			var missingA = Path.Combine(Path.GetTempPath(), "no-such-dir-a-" + Guid.NewGuid());
			var missingB = Path.Combine(Path.GetTempPath(), "no-such-dir-b-" + Guid.NewGuid());

			Assert.That(PartsInventory.LoadMergedPartsXml(new[] { missingA, missingB }), Is.Null);
		}
	}
}
