// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 4.10: the product first-slice definition is compiled from the live shipped layout inventory
	/// (not hand-authored), with stable ids derived from the real layout paths.
	/// </summary>
	[TestFixture]
	public class LexicalEditFirstSliceTests
	{
		private static string ShippedPartsDirectory()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
			{
				dir = dir.Parent;
			}

			Assert.That(dir, Is.Not.Null, "could not locate the repo root from the test directory");
			return Path.Combine(dir.FullName, "DistFiles", "Language Explorer", "Configuration", "Parts");
		}

		[Test]
		public void CompileFromLayoutDirectory_OverShippedLayouts_YieldsTheThreeFirstSliceFields()
		{
			var definition = LexicalEditFirstSlice.CompileFromLayoutDirectory(ShippedPartsDirectory());

			Assert.That(definition, Is.Not.Null, "the shipped layouts must compile (fallback means a regression)");
			Assert.That(definition.Roots.Select(r => r.Field), Is.EqualTo(new[] { "Form", "MorphType", "Gloss" }));
			Assert.That(definition.Diagnostics, Is.Empty, "the compiled definition carries no authored-fallback diagnostic");
		}

		[Test]
		public void CompiledFields_CarryRealLayoutBindings_AndProductMetadata()
		{
			var definition = LexicalEditFirstSlice.CompileFromLayoutDirectory(ShippedPartsDirectory());
			Assert.That(definition, Is.Not.Null);

			var form = definition.Roots[0];
			Assert.That(form.RawEditor, Is.EqualTo("multistring"), "from MoForm-Detail-AsLexemeForm");
			Assert.That(form.WritingSystem, Is.EqualTo("all vernacular"));
			Assert.That(form.Label, Is.EqualTo("Lexeme Form"));
			Assert.That(form.EditorClassification, Is.EqualTo(EditorClassification.Known));

			var morphType = definition.Roots[1];
			Assert.That(morphType.RawEditor, Is.EqualTo("MorphTypeAtomicReference"), "from MoForm-Detail-MorphTypeBasic");
			Assert.That(morphType.EditorClassification, Is.EqualTo(EditorClassification.Known),
				"editor classification must be case-insensitive like DataTree's editor.ToLower()");

			var gloss = definition.Roots[2];
			Assert.That(gloss.RawEditor, Is.EqualTo("multistring"), "from LexSense-Detail-GlossAllA");
			Assert.That(gloss.WritingSystem, Is.EqualTo("all analysis"));

			foreach (var node in definition.Roots)
			{
				Assert.That(node.Routing, Is.EqualTo(SurfaceRouting.Product));
				Assert.That(node.AutomationId, Is.Not.Null.And.Not.Empty);
				Assert.That(node.StableId, Does.Not.StartWith("LexEntry/identity"),
					"stable ids must derive from the real compiled layout paths, not the authored ones");
			}
		}

		[Test]
		public void CompiledDefinition_MapsToRegionFields_WithChooserAndTextKinds()
		{
			var definition = LexicalEditFirstSlice.CompileFromLayoutDirectory(ShippedPartsDirectory());
			Assert.That(definition, Is.Not.Null);

			var region = LexicalEditRegionMapper.FromViewDefinition(definition, new FakeRegionValueProvider());

			Assert.That(region.Fields, Has.Count.EqualTo(3));
			Assert.That(region.Fields[0].Kind, Is.EqualTo(RegionFieldKind.Text));
			Assert.That(region.Fields[1].Kind, Is.EqualTo(RegionFieldKind.Chooser));
			Assert.That(region.Fields[2].Kind, Is.EqualTo(RegionFieldKind.Text));
		}

		[Test]
		public void MissingDirectory_ReturnsNull_AndAuthoredFallbackCarriesDiagnostic()
		{
			Assert.That(LexicalEditFirstSlice.CompileFromLayoutDirectory(null), Is.Null);
			Assert.That(LexicalEditFirstSlice.CompileFromLayoutDirectory(
				Path.Combine(Path.GetTempPath(), "no-such-layout-dir")), Is.Null);

			var fallback = LexicalEditFirstSlice.AuthoredFallback();
			Assert.That(fallback.Roots.Select(r => r.Field), Is.EqualTo(new[] { "Form", "MorphType", "Gloss" }));
			Assert.That(fallback.Diagnostics.Single().Code, Is.EqualTo("authored-fallback"),
				"falling back must be visible, not silent");
		}

		[Test]
		public void BaseClassFallback_ResolvesSubclassPartRefs()
		{
			var parts = new DictionaryPartResolver(
				XElement.Parse(@"
<PartInventory><bin>
  <part id='MoForm-Detail-AsLexemeForm'><slice field='Form' editor='multistring'/></part>
</bin></PartInventory>"),
				new System.Collections.Generic.Dictionary<string, string> { { "MoStemAllomorph", "MoForm" } });

			Assert.That(parts.ResolvePart("MoStemAllomorph", "detail", "AsLexemeForm"), Is.Not.Null,
				"unresolved subclass refs must retry on the base class chain");
			Assert.That(parts.ResolvePart("MoStemAllomorph", "detail", "Nope"), Is.Null);
		}
	}
}
