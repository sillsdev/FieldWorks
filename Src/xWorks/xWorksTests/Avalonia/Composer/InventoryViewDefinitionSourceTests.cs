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
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
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
			// Custom-field metadata survives the per-test undo, so the static descriptor list
			// must not leak into the next test.
			FieldDescription.ClearDataAbout();
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
		public void GetSnapshot_UsesEffectiveLaterPartReplacementFromInventory()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal'>
    <part ref='CitationForm'/>
  </layout>
</LayoutInventory>";
			var parts = CreatePartsInventory();
			parts.LoadElements(@"
<PartInventory><bin>
  <part id='LexEntry-Detail-CitationForm'>
    <slice label='Replacement' editor='multistring' field='CitationForm'/>
  </part>
</bin></PartInventory>", 0);
			var source = CreateSource(CreateLayoutInventory(layoutXml), parts);

			var model = new ViewDefinitionCompiler().Compile(
				source.GetSnapshot("LexEntry", "Normal"));

			Assert.That(model.Roots.Single().Label, Is.EqualTo("Replacement"));
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
		public void GetSnapshot_ChoiceGuidUsesExactLayoutButDoesNotUseChoicelessFallback()
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
			ViewDefinitionSourceSnapshot fallback = null;
			Assert.That(() => fallback = source.GetSnapshot("LexEntry", "Normal", unknownGuid),
				Throws.InstanceOf<InvalidOperationException>());

			Assert.That(XElement.Parse(exact.LayoutXml).Attribute("marker")?.Value, Is.EqualTo("exact"));
			Assert.That(fallback, Is.Null);
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
			Assert.That(snapshot.ClassName, Is.EqualTo("MoStemAllomorph"));
			Assert.That(new ViewDefinitionCompiler().Compile(snapshot).ClassName,
				Is.EqualTo("MoForm"));
			Assert.That(snapshot.BaseClassMap, Is.EquivalentTo(new Dictionary<string, string>
			{
				["MoStemAllomorph"] = "MoForm",
				["MoForm"] = "CmObject"
			}));
		}

		[Test]
		public void GetSnapshot_ExpandsConcreteOnlyCustomFieldWhenUsingBaseLayout()
		{
			var allomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			var field = new FieldDescription(Cache)
			{
				Userlabel = "Concrete-only source field",
				HelpString = string.Empty,
				Class = allomorph.ClassID,
				Type = CellarPropertyType.String,
				WsSelector = WritingSystemServices.kwsVern
			};
			field.UpdateCustomField();

			const string layoutXml = @"
<LayoutInventory>
  <layout class='MoForm' type='detail' name='Normal'>
    <part customFields='here'/>
  </layout>
</LayoutInventory>";
			var layouts = CreateLayoutInventory(layoutXml);
			var source = CreateSource(layouts, CreatePartsInventory(), Cache);

			var snapshot = source.GetSnapshot(allomorph, "Normal");
			var effectiveLayout = XElement.Parse(snapshot.LayoutXml);

			Assert.That(snapshot.CustomFieldsExpanded, Is.True);
			Assert.That(effectiveLayout.Attribute("class")?.Value, Is.EqualTo("MoForm"),
				"the concrete object resolves to the base layout");
			Assert.That(effectiveLayout.Descendants("part").Single(part =>
				(string)part.Attribute("ref") == "_CustomFieldPlaceholder"), Is.Not.Null);
			Assert.That(effectiveLayout.Descendants("part").Any(part =>
				(string)part.Attribute("ref") == "Custom"
				&& (string)part.Attribute("param") == field.Name), Is.True);
			Assert.That(Directory.GetFiles(Path.Combine(_projectPath, "ConfigurationSettings"),
				"*.fwlayout"), Is.Not.Empty,
				"adding the missing placeholder through the source persists the effective layout");
		}

		[Test]
		public void GetSnapshot_SetsMissingPlaceholderRefOnLiveInventoryNode()
		{
			var allomorph = CreateAllomorphWithCustomField();
			var layouts = CreateLayoutInventory(PlaceholderLayoutXml);
			var liveLayout = layouts.GetElement("layout",
				new[] { "MoForm", "detail", "Normal", null });
			var source = CreateSource(layouts, CreatePartsInventory(), Cache);

			source.GetSnapshot(allomorph, "Normal");

			Assert.That(PlaceholderRef(liveLayout), Is.EqualTo("_CustomFieldPlaceholder"),
				"the placeholder ref is set on the live node the inventory handed out");
			Assert.That(PlaceholderRef(layouts.GetElement("layout",
				new[] { "MoForm", "detail", "Normal", null })),
				Is.EqualTo("_CustomFieldPlaceholder"),
				"the inventory keeps serving the layout with the placeholder ref");
		}

		[Test]
		public void GetSnapshot_DoesNotRewriteProjectLayoutOnceThePlaceholderRefIsPersisted()
		{
			var allomorph = CreateAllomorphWithCustomField();
			var source = CreateSource(CreateLayoutInventory(PlaceholderLayoutXml),
				CreatePartsInventory(), Cache);

			source.GetSnapshot(allomorph, "Normal");
			var persistedPath = PersistedLayoutPath();
			var persistedAfterFirstCall = File.ReadAllBytes(persistedPath);
			var sentinel = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			File.SetLastWriteTimeUtc(persistedPath, sentinel);
			source.GetSnapshot(allomorph, "Normal");

			Assert.That(File.GetLastWriteTimeUtc(persistedPath), Is.EqualTo(sentinel),
				"the second snapshot finds the placeholder ref already in the inventory");
			Assert.That(File.ReadAllBytes(persistedPath), Is.EqualTo(persistedAfterFirstCall));
		}

		[Test]
		public void GetSnapshot_PersistsOnlyThePlaceholderRefNotGeneratedCustomParts()
		{
			var allomorph = CreateAllomorphWithCustomField();
			var source = CreateSource(CreateLayoutInventory(PlaceholderLayoutXml),
				CreatePartsInventory(), Cache);

			var snapshot = source.GetSnapshot(allomorph, "Normal");

			var persistedParts = XElement.Load(PersistedLayoutPath()).Descendants("part").ToList();
			Assert.That(XElement.Parse(snapshot.LayoutXml).Descendants("part").Any(part =>
				(string)part.Attribute("ref") == "Custom"), Is.True,
				"the snapshot still expands the custom field");
			Assert.That(persistedParts.Single(part => part.Attribute("customFields") != null)
				.Attribute("ref")?.Value, Is.EqualTo("_CustomFieldPlaceholder"),
				"the placeholder ref is written to the project layout file");
			Assert.That(persistedParts.Any(part => (string)part.Attribute("ref") == "Custom"),
				Is.False,
				"generated custom parts are regenerated on every load, never written to disk");
		}

		[Test]
		public void GetSnapshot_CompilerResolvesPartFromCmObjectBaseClass()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='MoForm' type='detail' name='Normal'><part ref='Generic'/></layout>
</LayoutInventory>";
			var parts = CreatePartsInventory();
			parts.LoadElements(@"
<PartInventory><bin>
  <part id='CmObject-Detail-Generic'>
    <slice label='Generic' editor='literalString' field='Guid'/>
  </part>
</bin></PartInventory>", 0);
			var source = CreateSource(CreateLayoutInventory(layoutXml), parts);

			var model = new ViewDefinitionCompiler().Compile(
				source.GetSnapshot("MoStemAllomorph", "Normal"));

			Assert.That(model.Roots.Single().Label, Is.EqualTo("Generic"));
			Assert.That(model.Diagnostics.Any(item => item.Code == "unresolved-part"), Is.False);
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
		public void GetSnapshot_BaseExactChoiceWinsOverDerivedChoicelessLayout()
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
				Is.EqualTo("base-exact"));
		}

		[Test]
		public void GetSnapshot_MissingNamedLayoutSkipsConcreteDefaultBeforeBaseDefaultLikeWinForms()
		{
			const string layoutXml = @"
<LayoutInventory>
				<layout class='MoStemAllomorph' type='detail' name='default' marker='concrete-default'/>
				<layout class='MoForm' type='detail' name='default' marker='base-default'/>
				<layout class='MoForm' type='detail' name='Normal' marker='base-named'/>
</LayoutInventory>";
			var source = CreateSource(CreateLayoutInventory(layoutXml), CreatePartsInventory());

			var snapshot = source.GetSnapshot("MoStemAllomorph", "Missing");

			Assert.That(XElement.Parse(snapshot.LayoutXml).Attribute("marker")?.Value,
				Is.EqualTo("base-default"));
		}

		[Test]
		public void GetSnapshot_RnGenericRecChoiceClonesAndPersistsCurrentClassLayout()
		{
			const string selectedGuid = "11111111-1111-1111-1111-111111111111";
			const string layoutXml = @"
<LayoutInventory>
  <layout class='RnGenericRec' type='detail' name='Normal' marker='generic'/>
</LayoutInventory>";
			var layouts = CreateLayoutInventory(layoutXml);
			var source = CreateSource(layouts, CreatePartsInventory());

			var snapshot = source.GetSnapshot("RnGenericRec", "Normal", selectedGuid);

			var selected = XElement.Parse(snapshot.LayoutXml);
			Assert.That(selected.Attribute("marker")?.Value, Is.EqualTo("generic"));
			Assert.That(selected.Attribute("choiceGuid")?.Value, Is.EqualTo(selectedGuid));
			Assert.That(layouts.GetElement("layout",
				new[] { "rngenericrec", "DETAIL", "normal", selectedGuid.ToUpperInvariant() }),
				Is.Not.Null, "all four inventory keys are case-insensitive");
			Assert.That(Directory.GetFiles(Path.Combine(_projectPath, "ConfigurationSettings"),
				"*.fwlayout"), Is.Not.Empty, "the custom type clone is persisted immediately");
		}

		[Test]
		public void GetSnapshot_RnGenericRecExplicitEmptyChoiceClonesAndPersists()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='RnGenericRec' type='detail' name='Normal' marker='generic'/>
</LayoutInventory>";
			var layouts = CreateLayoutInventory(layoutXml);
			var source = CreateSource(layouts, CreatePartsInventory());

			var snapshot = source.GetSnapshot("RnGenericRec", "Normal", string.Empty);

			var selected = XElement.Parse(snapshot.LayoutXml);
			Assert.That(selected.Attribute("choiceGuid"), Is.Not.Null,
				"an explicit empty choice is still a choice key");
			Assert.That(selected.Attribute("choiceGuid").Value, Is.Empty);
			Assert.That(layouts.GetElement("layout",
				new[] { "RnGenericRec", "detail", "Normal", string.Empty }), Is.Not.Null);
		}

		[Test]
		public void GetSnapshot_CustomItemUsesLegacyListWritingSystemLayoutMappings()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='CmCustomItem' type='detail' name='Requested' marker='requested'/>
  <layout class='CmCustomItem' type='detail' name='CmPossibilityA' marker='a'/>
  <layout class='CmCustomItem' type='detail' name='CmPossibilityV' marker='v'/>
  <layout class='CmCustomItem' type='detail' name='CmPossibilityAV' marker='av'/>
  <layout class='CmCustomItem' type='detail' name='CmPossibilityVA' marker='va'/>
</LayoutInventory>";
			var source = CreateSource(CreateLayoutInventory(layoutXml), CreatePartsInventory());
			ICmCustomItem item = null;
			ICmPossibilityList list = null;
			item = Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>().Create();
			list = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>()
				.CreateUnowned("Custom list", Cache.DefaultUserWs);

			Assert.That(XElement.Parse(source.GetSnapshot(item, "Requested").LayoutXml)
				.Attribute("marker")?.Value, Is.EqualTo("a"),
				"an unowned custom item uses the analysis layout");

			var parent = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			list.PossibilitiesOS.Add(parent);
			parent.SubPossibilitiesOS.Add(item);

			var mappings = new[]
			{
				(Ws: WritingSystemServices.kwsAnals, Marker: "a"),
				(Ws: WritingSystemServices.kwsVerns, Marker: "v"),
				(Ws: WritingSystemServices.kwsAnalVerns, Marker: "av"),
				(Ws: WritingSystemServices.kwsVernAnals, Marker: "va")
			};
			foreach (var mapping in mappings)
			{
				list.WsSelector = mapping.Ws;
				Assert.That(XElement.Parse(source.GetSnapshot(item, "Requested").LayoutXml)
					.Attribute("marker")?.Value, Is.EqualTo(mapping.Marker));
			}

			list.WsSelector = 123456;
			Assert.That(XElement.Parse(source.GetSnapshot(item, "Requested").LayoutXml)
				.Attribute("marker")?.Value, Is.EqualTo("requested"),
				"an unknown selector preserves the requested layout name");
		}

		[Test]
		public void GetSnapshot_NoLayoutInClassHierarchyThrows()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='MoForm' type='detail' name='Different'/>
</LayoutInventory>";
			var source = CreateSource(CreateLayoutInventory(layoutXml), CreatePartsInventory());

			Assert.That(() => source.GetSnapshot("MoStemAllomorph", "Normal"),
				Throws.InstanceOf<InvalidOperationException>());
		}

		private const string PlaceholderLayoutXml = @"
<LayoutInventory>
  <layout class='MoForm' type='detail' name='Normal'>
    <part customFields='here'/>
  </layout>
</LayoutInventory>";

		private IMoStemAllomorph CreateAllomorphWithCustomField()
		{
			var allomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			var field = new FieldDescription(Cache)
			{
				Userlabel = "Placeholder source field " + Guid.NewGuid().ToString("N"),
				HelpString = string.Empty,
				Class = allomorph.ClassID,
				Type = CellarPropertyType.String,
				WsSelector = WritingSystemServices.kwsVern
			};
			field.UpdateCustomField();
			FieldDescription.ClearDataAbout();
			return allomorph;
		}

		private static string PlaceholderRef(XmlNode layout)
		{
			return layout.SelectSingleNode("part[@customFields]")?.Attributes?["ref"]?.Value;
		}

		private string PersistedLayoutPath()
		{
			return Directory.GetFiles(Path.Combine(_projectPath, "ConfigurationSettings"),
				"*.fwlayout").Single();
		}

		private InventoryViewDefinitionSource CreateSource(Inventory layouts, Inventory parts,
			LcmCache cache = null)
		{
			return new InventoryViewDefinitionSource(layouts, parts.Root.OuterXml,
				Cache.MetaDataCacheAccessor, cache);
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
