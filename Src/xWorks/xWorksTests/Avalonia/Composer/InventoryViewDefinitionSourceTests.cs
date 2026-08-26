// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class InventoryViewDefinitionSourceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private const string PartsXml = @"
<PartInventory><bin>
  <part id='LexEntry-Detail-CitationForm'>
    <slice label='CitationForm' editor='multistring' field='CitationForm'/>
  </part>
</bin></PartInventory>";

		private string _projectPath;

		public override void TestSetup()
		{
			base.TestSetup();
			_projectPath = Path.Combine(Path.GetTempPath(), "fw-inventory-source-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_projectPath);
		}

		public override void TestTearDown()
		{
			base.TestTearDown();
			if (Directory.Exists(_projectPath))
				Directory.Delete(_projectPath, true);
		}

		[Test]
		public void GetSnapshot_ReturnsShippedLayoutAndMergedParts()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal'>
    <part ref='CitationForm'/>
  </layout>
</LayoutInventory>";
			var layouts = CreateLayoutInventory(layoutXml);
			var parts = CreatePartsInventory();
			var source = CreateSource(layouts, parts);

			var snapshot = source.GetSnapshot("LexEntry", "Normal");

			Assert.That(snapshot, Is.Not.Null);
			Assert.That(XElement.Parse(snapshot.LayoutXml).Element("part")?.Attribute("ref")?.Value,
				Is.EqualTo("CitationForm"));
			Assert.That(XElement.Parse(snapshot.PartsXml).Descendants("part").Single().Attribute("id")?.Value,
				Is.EqualTo("LexEntry-Detail-CitationForm"));
		}

		[Test]
		public void GetSnapshot_AfterPersistedOverrideReturnsNewXmlWithoutChangingPriorSnapshot()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal'>
    <part ref='CitationForm' visibility='always'/>
  </layout>
</LayoutInventory>";
			var layouts = CreateLayoutInventory(layoutXml);
			var source = CreateSource(layouts, CreatePartsInventory());
			var first = source.GetSnapshot("LexEntry", "Normal");
			var changed = new XmlDocument();
			changed.LoadXml(@"<layout class='LexEntry' type='detail' name='Normal'>
  <part ref='CitationForm' visibility='never'/>
</layout>");

			layouts.PersistOverrideElement(changed.DocumentElement);
			var second = source.GetSnapshot("LexEntry", "Normal");

			Assert.That(GetVisibility(first), Is.EqualTo("always"));
			Assert.That(GetVisibility(second), Is.EqualTo("never"));
		}

		[Test]
		public void GetSnapshot_ChoiceGuidUsesExactLayoutThenFallsBackToLayoutWithoutChoiceGuid()
		{
			const string selectedGuid = "11111111-1111-1111-1111-111111111111";
			const string unknownGuid = "22222222-2222-2222-2222-222222222222";
			const string layoutXml = @"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal' marker='fallback'/>
  <layout class='LexEntry' type='detail' name='Normal'
          choiceGuid='11111111-1111-1111-1111-111111111111' marker='exact'/>
</LayoutInventory>";
			var source = CreateSource(CreateLayoutInventory(layoutXml), CreatePartsInventory());

			var exact = source.GetSnapshot("LexEntry", "Normal", selectedGuid);
			var fallback = source.GetSnapshot("LexEntry", "Normal", unknownGuid);

			Assert.That(XElement.Parse(exact.LayoutXml).Attribute("marker")?.Value, Is.EqualTo("exact"));
			Assert.That(XElement.Parse(fallback.LayoutXml).Attribute("marker")?.Value, Is.EqualTo("fallback"));
		}

		[Test]
		public void GetSnapshot_MissingDerivedLayoutUsesBaseLayoutAndRecordsPartResolutionMap()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='MoForm' type='detail' name='Normal'/>
</LayoutInventory>";
			var source = CreateSource(CreateLayoutInventory(layoutXml), CreatePartsInventory());

			var snapshot = source.GetSnapshot("MoStemAllomorph", "Normal");

			Assert.That(snapshot, Is.Not.Null);
			Assert.That(snapshot.ClassName, Is.EqualTo("MoForm"));
			Assert.That(snapshot.BaseClassMap, Is.EquivalentTo(new Dictionary<string, string>
			{
				["MoStemAllomorph"] = "MoForm"
			}));
		}

		[Test]
		public void GetSnapshot_BaseClassMapRejectsMutationThroughDictionaryContract()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='MoForm' type='detail' name='Normal'/>
</LayoutInventory>";
			var source = CreateSource(CreateLayoutInventory(layoutXml), CreatePartsInventory());
			var snapshot = source.GetSnapshot("MoStemAllomorph", "Normal");
			var map = (IDictionary<string, string>)snapshot.BaseClassMap;

			Assert.That(() => map["MoStemAllomorph"] = "CmObject",
				Throws.TypeOf<NotSupportedException>());
		}

		[Test]
		public void GetSnapshot_DerivedChoiceFallbackWinsBeforeBaseExactChoice()
		{
			const string selectedGuid = "11111111-1111-1111-1111-111111111111";
			const string layoutXml = @"
<LayoutInventory>
  <layout class='MoStemAllomorph' type='detail' name='Normal' marker='derived-fallback'/>
  <layout class='MoForm' type='detail' name='Normal'
          choiceGuid='11111111-1111-1111-1111-111111111111' marker='base-exact'/>
</LayoutInventory>";
			var source = CreateSource(CreateLayoutInventory(layoutXml), CreatePartsInventory());

			var snapshot = source.GetSnapshot("MoStemAllomorph", "Normal", selectedGuid);

			Assert.That(XElement.Parse(snapshot.LayoutXml).Attribute("marker")?.Value,
				Is.EqualTo("derived-fallback"));
		}

		[Test]
		public void GetSnapshot_NoLayoutInClassHierarchyReturnsNull()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='MoForm' type='detail' name='Different'/>
</LayoutInventory>";
			var source = CreateSource(CreateLayoutInventory(layoutXml), CreatePartsInventory());

			var snapshot = source.GetSnapshot("MoStemAllomorph", "Normal");

			Assert.That(snapshot, Is.Null);
		}

		private InventoryViewDefinitionSource CreateSource(Inventory layouts, Inventory parts)
		{
			return new InventoryViewDefinitionSource(layouts, parts.Root.OuterXml,
				Cache.MetaDataCacheAccessor);
		}

		private Inventory CreateLayoutInventory(string xml)
		{
			var keyAttributes = new Dictionary<string, string[]>
			{
				["layout"] = new[] { "class", "type", "name", "choiceGuid" }
			};
			var inventory = new Inventory("*.fwlayout", "/LayoutInventory/*", keyAttributes,
				"InventoryViewDefinitionSourceTests", _projectPath);
			inventory.LoadElements(xml, 0);
			return inventory;
		}

		private static Inventory CreatePartsInventory()
		{
			var keyAttributes = new Dictionary<string, string[]>
			{
				["part"] = new[] { "id" }
			};
			var inventory = new Inventory("*Parts.xml", "/PartInventory/bin/*", keyAttributes,
				"InventoryViewDefinitionSourceTests", "unused");
			inventory.LoadElements(PartsXml, 0);
			return inventory;
		}

		private static string GetVisibility(Common.FwAvalonia.ViewDefinition.ViewDefinitionSourceSnapshot snapshot)
		{
			return XElement.Parse(snapshot.LayoutXml).Element("part")?.Attribute("visibility")?.Value;
		}
	}
}
