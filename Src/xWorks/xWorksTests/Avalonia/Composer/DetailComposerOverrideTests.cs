// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class DetailComposerOverrideTests : MemoryOnlyBackendProviderTestBase
	{
		private const string LayoutXml = @"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal'>
    <part ref='CitationForm' visibility='always'/>
    <part ref='Bibliography' visibility='always'/>
  </layout>
  <layout class='LexSense' type='detail' name='Normal'>
    <part ref='ProjectGloss' visibility='always'/>
  </layout>
</LayoutInventory>";

		private const string PartsXml = @"
<PartInventory><bin>
  <part id='LexEntry-Detail-CitationForm'>
    <slice label='Citation Form' editor='multistring' field='CitationForm'/>
  </part>
  <part id='LexEntry-Detail-Bibliography'>
    <slice label='Bibliography' editor='multistring' field='Bibliography'/>
  </part>
  <part id='LexEntry-Detail-Senses'>
    <seq field='Senses'/>
  </part>
  <part id='LexSense-Detail-ProjectGloss'>
    <slice label='Project-only Gloss' editor='multistring' field='Gloss'/>
  </part>
</bin></PartInventory>";

		private ILexEntry m_entry;
		private ILexSense m_sense;
		private string m_projectPath;

		public override void TestSetup()
		{
			base.TestSetup();
			m_projectPath = Path.Combine(Path.GetTempPath(),
				"fw-composer-inventory-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(m_projectPath);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				m_entry.CitationForm.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				m_entry.Bibliography.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("source", Cache.DefaultAnalWs));
				m_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(m_sense);
				m_sense.Gloss.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("house", Cache.DefaultAnalWs));
			});
		}

		public override void TestTearDown()
		{
			try
			{
				base.TestTearDown();
			}
			finally
			{
				if (Directory.Exists(m_projectPath))
					Directory.Delete(m_projectPath, true);
			}
		}

		[Test]
		public void Compose_InventoryRootFieldsCarryLayoutContext()
		{
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "root-context"));
			PersistLayout(layouts);
			var source = CreateSource(layouts);

			var composed = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);
			var rootFields = composed.Model.Fields.Where(field => field.ObjectHvo == m_entry.Hvo).ToList();

			Assert.That(rootFields, Is.Not.Empty);
			Assert.That(rootFields, Has.All.Property("ClassName").EqualTo("LexEntry"));
			Assert.That(rootFields, Has.All.Property("LayoutName").EqualTo("Normal"));
		}

		[Test]
		public void Compose_NestedObjectUsesTheSameInventorySourceAndCarriesLayoutContext()
		{
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "nested-source"));
			PersistLayout(layouts, includeSenses: true);
			PersistSenseLayout(layouts);
			var source = CreateSource(layouts);

			var composed = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);
			var nested = composed.Model.Fields.Single(field => field.ObjectHvo == m_sense.Hvo
				&& field.Field == "Gloss");

			Assert.That(nested.Label, Is.EqualTo("Project-only Gloss"));
			Assert.That(nested.Values.Any(value => value.Value == "house"), Is.True);
			Assert.That(nested.ClassName, Is.EqualTo("LexSense"));
			Assert.That(nested.LayoutName, Is.EqualTo("Normal"));
		}

		[Test]
		public void Compose_InventoryVisibilityOverrideMatchesLegacyShowHiddenBehavior()
		{
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "visibility"));
			PersistLayout(layouts, citationVisibility: "never");
			var source = CreateSource(layouts);

			var hidden = DetailComposer.Compose(m_entry, Cache, showHiddenFields: false,
				source: source.GetSnapshot);
			var shown = DetailComposer.Compose(m_entry, Cache, showHiddenFields: true,
				source: source.GetSnapshot);

			Assert.That(hidden.Model.Fields.Any(field => field.Field == "CitationForm"), Is.False);
			Assert.That(shown.Model.Fields.Any(field => field.Field == "CitationForm"), Is.True);
		}

		[Test]
		public void Compose_InventoryReorderOverrideChangesSiblingOrder()
		{
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "reorder"));
			PersistLayout(layouts, reverse: true);
			var source = CreateSource(layouts);

			var composed = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);

			Assert.That(FieldNames(composed), Is.EqualTo(new[] { "Bibliography", "CitationForm" }));
		}

		[Test]
		public void Compose_SecondInventoryDoesNotSeeFirstProjectsOverride()
		{
			var firstLayouts = CreateLayoutInventory(Path.Combine(m_projectPath, "first"));
			var secondLayouts = CreateLayoutInventory(Path.Combine(m_projectPath, "second"));
			PersistLayout(firstLayouts, reverse: true);
			PersistLayout(secondLayouts);
			var firstSource = CreateSource(firstLayouts);
			var secondSource = CreateSource(secondLayouts);

			var first = DetailComposer.Compose(m_entry, Cache, source: firstSource.GetSnapshot);
			var second = DetailComposer.Compose(m_entry, Cache, source: secondSource.GetSnapshot);

			Assert.That(FieldNames(first), Is.EqualTo(new[] { "Bibliography", "CitationForm" }));
			Assert.That(FieldNames(second), Is.EqualTo(new[] { "CitationForm", "Bibliography" }));
		}

		[Test]
		public void Compose_PersistedChangeIsVisibleOnNextCompose()
		{
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "refresh"));
			var source = CreateSource(layouts);
			var before = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);

			PersistLayout(layouts, citationVisibility: "never");
			var after = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);

			Assert.That(before.Model.Fields.Any(field => field.Field == "CitationForm"), Is.True);
			Assert.That(after.Model.Fields.Any(field => field.Field == "CitationForm"), Is.False);
		}

		[Test]
		public void CompileForObject_InventoryContentFingerprintReusesAndRefreshesCompiledModel()
		{
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "fingerprint"));
			PersistLayout(layouts);
			var source = CreateSource(layouts);

			var first = DetailComposer.CompileForObject(Cache, m_entry, "Normal", source.GetSnapshot);
			var sameContent = DetailComposer.CompileForObject(Cache, m_entry, "Normal", source.GetSnapshot);

			PersistLayout(layouts, citationVisibility: "never");
			var changed = DetailComposer.CompileForObject(Cache, m_entry, "Normal", source.GetSnapshot);

			Assert.That(sameContent, Is.SameAs(first));
			Assert.That(changed, Is.Not.SameAs(first));
			Assert.That(changed.Roots.First().Visibility, Is.EqualTo(ViewVisibility.Never));
		}

		[Test]
		public void CompileForObject_NullSourceResultFallsBackToShippedLayout()
		{
			ViewDefinitionSourceResolver source = (className, layoutName, choiceGuid) => null;

			var compiled = DetailComposer.CompileForObject(Cache, m_entry, "Normal", source);

			Assert.That(compiled, Is.Not.Null);
			Assert.That(compiled.Roots, Has.Count.GreaterThan(2));
		}

		[Test]
		public void CompileForObject_SourceExceptionPropagates()
		{
			ViewDefinitionSourceResolver source = (className, layoutName, choiceGuid) =>
				throw new InvalidOperationException("source failed");

			Assert.That(() => DetailComposer.CompileForObject(Cache, m_entry, "Normal", source),
				Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("source failed"));
		}

		private InventoryViewDefinitionSource CreateSource(Inventory layouts)
		{
			var parts = new Inventory("*Parts.xml", "/PartInventory/bin/*",
				new Dictionary<string, string[]> { ["part"] = new[] { "id" } },
				"DetailComposerOverrideTests", "unused");
			parts.LoadElements(PartsXml, 0);
			return new InventoryViewDefinitionSource(layouts, parts.Root.OuterXml,
				Cache.MetaDataCacheAccessor);
		}

		private static Inventory CreateLayoutInventory(string projectPath)
		{
			var layouts = new Inventory("*.fwlayout", "/LayoutInventory/*",
				new Dictionary<string, string[]>
				{
					["layout"] = new[] { "class", "type", "name", "choiceGuid" }
				}, "DetailComposerOverrideTests", projectPath);
			layouts.LoadElements(LayoutXml, 0);
			return layouts;
		}

		private static void PersistLayout(Inventory layouts, string citationVisibility = "always",
			bool reverse = false, bool includeSenses = false)
		{
			var first = reverse
				? "<part ref='Bibliography' visibility='always'/>"
				: "<part ref='CitationForm' visibility='" + citationVisibility + "'/>";
			var second = reverse
				? "<part ref='CitationForm' visibility='" + citationVisibility + "'/>"
				: "<part ref='Bibliography' visibility='always'/>";
			var document = new XmlDocument();
			document.LoadXml("<layout class='LexEntry' type='detail' name='Normal'>"
				+ first + second + (includeSenses ? "<part ref='Senses' param='Normal'/>" : "")
				+ "</layout>");
			layouts.PersistOverrideElement(document.DocumentElement);
		}

		private static void PersistSenseLayout(Inventory layouts)
		{
			var document = new XmlDocument();
			document.LoadXml("<layout class='LexSense' type='detail' name='Normal'>"
				+ "<part ref='ProjectGloss' visibility='always'/></layout>");
			layouts.PersistOverrideElement(document.DocumentElement);
		}

		private static IReadOnlyList<string> FieldNames(ComposedDetail composed)
			=> composed.Model.Fields.Select(field => field.Field).ToList();
	}
}
