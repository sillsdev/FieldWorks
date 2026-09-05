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
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class ProjectLayoutCompositionTests : MemoryOnlyBackendProviderTestBase
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
    <slice label='Citation Form' editor='multistring' field='CitationForm' ws='all vernacular'/>
  </part>
  <part id='LexEntry-Detail-Bibliography'>
    <slice label='Bibliography' editor='multistring' field='Bibliography'/>
  </part>
  <part id='LexEntry-Detail-Senses'>
    <seq field='Senses'/>
  </part>
  <part id='LexEntry-Detail-LexemeForm'>
    <obj field='LexemeForm' layout='Normal'/>
  </part>
  <part id='LexSense-Detail-ProjectGloss'>
    <slice label='Project-only Gloss' editor='multistring' field='Gloss'/>
  </part>
  <part id='LexSense-Detail-InjectedDefinition'>
    <slice label='Injected Definition' editor='multistring' field='Definition'/>
  </part>
  <part id='MoStemAllomorph-Detail-BaseForm'>
    <slice label='Base Form' editor='multistring' field='Form'/>
  </part>
  <part id='MoStemAllomorph-Detail-InjectedIsAbstract'>
    <slice label='Injected Abstract' editor='boolean' field='IsAbstract'/>
  </part>
</bin></PartInventory>";

		private ILexEntry m_entry;
		private ILexSense m_sense;
		private IMoStemAllomorph m_lexemeForm;
		private string m_projectPath;

		[Test]
		public void Compose_PassesAnExplicitEmptyRootLayoutToTheSource()
		{
			string requestedLayout = null;
			ViewDefinitionSourceResolver source = (obj, layoutName, choiceGuid, callerXml) =>
			{
				requestedLayout = layoutName;
				return null;
			};

			DetailComposer.Compose(m_entry, Cache, string.Empty, source: source);

			Assert.That(requestedLayout, Is.Empty,
				"an explicit empty layout request must reach the source unchanged");
		}

		[Test]
		public void Compose_PassesNullRootLayoutToTheSourceForDefaultResolution()
		{
			string requestedLayout = "sentinel";
			ViewDefinitionSourceResolver source = (obj, layoutName, choiceGuid, callerXml) =>
			{
				requestedLayout = layoutName;
				return null;
			};

			DetailComposer.Compose(m_entry, Cache, null, source: source);

			Assert.That(requestedLayout, Is.Null,
				"null means default to the source and must not be rewritten to Normal");
		}

		public override void TestSetup()
		{
			base.TestSetup();
			m_projectPath = Path.Combine(Path.GetTempPath(),
				"fw-composer-inventory-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(m_projectPath);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				m_lexemeForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = m_lexemeForm;
				m_lexemeForm.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
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
		public void Compose_BaseLayoutStillResolvesPartsFromTheConcreteObjectClass()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='MoForm' type='detail' name='BaseOnly'>
    <part ref='SharedForm'/>
  </layout>
</LayoutInventory>";
			const string partsXml = @"
<PartInventory><bin>
  <part id='MoStemAllomorph-Detail-SharedForm'>
    <slice label='Concrete Form' editor='multistring' field='Form'/>
  </part>
  <part id='MoForm-Detail-SharedForm'>
    <slice label='Base Form' editor='multistring' field='Form'/>
  </part>
</bin></PartInventory>";
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "base-layout-parts"),
				layoutXml);
			var source = CreateSource(layouts, partsXml);

			var composed = DetailComposer.Compose(m_lexemeForm, Cache, "BaseOnly",
				source: source.GetSnapshot);

			Assert.That(composed.Model.Fields.Single().Label, Is.EqualTo("Concrete Form"));
			Assert.That(composed.Model.Fields.Single().ClassName, Is.EqualTo("MoForm"));
		}

		[Test]
		public void ExpandCustomFields_UsesRuntimeClassWhenLayoutResolvesToBaseClass()
		{
			var field = new FieldDescription(Cache)
			{
				Userlabel = "Concrete-only custom (runtime class)",
				HelpString = string.Empty,
				Class = m_lexemeForm.ClassID,
				Type = CellarPropertyType.String,
				WsSelector = WritingSystemServices.kwsVern
			};
			field.UpdateCustomField();
			FieldDescription.ClearDataAbout();

			var layout = XElement.Parse("<layout class='MoForm' type='detail' name='BaseOnly'>"
				+ "<part customFields='here'/></layout>");
			DetailComposer.ExpandCustomFields(layout, Cache, m_lexemeForm.ClassID, null);

			Assert.That(layout.Descendants("part").Any(part =>
				(string)part.Attribute("ref") == "Custom"
				&& (string)part.Attribute("param") == field.Name), Is.True);
		}

		[Test]
		public void Compose_ExpandsCustomFieldsWhenSourceHasNoCache()
		{
			var field = new FieldDescription(Cache)
			{
				Userlabel = "Concrete-only custom (no cache)",
				HelpString = string.Empty,
				Class = m_lexemeForm.ClassID,
				Type = CellarPropertyType.String,
				WsSelector = WritingSystemServices.kwsVern
			};
			field.UpdateCustomField();
			FieldDescription.ClearDataAbout();

			const string layoutXml = @"
<LayoutInventory>
  <layout class='MoForm' type='detail' name='BaseOnly'>
    <part customFields='here'/>
  </layout>
</LayoutInventory>";
			const string partsXml = @"
<PartInventory><bin>
  <part id='MoForm-Detail-Custom'>
    <slice editor='string'/>
  </part>
</bin></PartInventory>";
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "source-null-cache"),
				layoutXml);
			var source = CreateSource(layouts, partsXml);
			var snapshot = source.GetSnapshot(m_lexemeForm, "BaseOnly");

			Assert.That(snapshot.CustomFieldsExpanded, Is.False);

			var composed = DetailComposer.Compose(m_lexemeForm, Cache, "BaseOnly",
				source: source.GetSnapshot);

			Assert.That(composed.Model.Fields.Count(fieldModel => fieldModel.Field == field.Name),
				Is.EqualTo(1));
		}

		[Test]
		public void Compose_SublayoutUsesProjectInventoryAndCarriesSublayoutIdentity()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal'>
    <sublayout name='Inline'/>
  </layout>
  <layout class='LexEntry' type='detail' name='Inline'>
    <part ref='CitationForm' visibility='always'/>
  </layout>
</LayoutInventory>";
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "sublayout"), layoutXml);
			var source = CreateSource(layouts);

			var composed = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);
			var field = composed.Model.Fields.Single();

			Assert.That(field.Field, Is.EqualTo("CitationForm"));
			Assert.That(field.LayoutName, Is.EqualTo("Inline"));
			Assert.That(field.SourceCallerPath, Is.EqualTo("part[0]"));
		}

		[Test]
		public void ResolveLayoutChoiceGuid_NonAtomicFieldThrows()
		{
			Assert.That(() => DetailComposer.ResolveLayoutChoiceGuid(Cache, m_entry, "CitationForm"),
				Throws.Exception);
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
			Assert.That(nested.ClassName, Is.EqualTo("LexEntry"),
				"object descent keeps the outer layout as the legacy persistence root");
			Assert.That(nested.LayoutName, Is.EqualTo("Normal"));
			Assert.That(nested.SourceCallerPath, Does.Contain("|"),
				"the caller identity crosses the outer and nested layouts");
			Assert.That(nested.LayoutPath.Select(layout => layout.ClassName),
				Is.EqualTo(new[] { "LexEntry", "LexSense" }));
		}

		// The source maps a CmCustomItem's layout from its OWNING LIST's writing-system selector,
		// so two custom items of one class composed in one pass can need different layouts.
		[Test]
		public void Compose_CustomItemsFromListsWithDifferentWsSelectors_GetTheirOwnLayouts()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='CmCustomItem' type='detail' name='CmPossibilityA'>
    <part ref='AnalysisMarker' visibility='always'/>
    <part ref='Restrictions' param='Normal'/>
  </layout>
  <layout class='CmCustomItem' type='detail' name='CmPossibilityV'>
    <part ref='VernacularMarker' visibility='always'/>
  </layout>
</LayoutInventory>";
			const string partsXml = @"
<PartInventory><bin>
  <part id='CmCustomItem-Detail-AnalysisMarker'>
    <slice label='Analysis Marker' editor='multistring' field='Name'/>
  </part>
  <part id='CmCustomItem-Detail-VernacularMarker'>
    <slice label='Vernacular Marker' editor='multistring' field='Name'/>
  </part>
  <part id='CmCustomItem-Detail-Restrictions'>
    <seq field='Restrictions'/>
  </part>
</bin></PartInventory>";
			ICmCustomItem root = null;
			ICmCustomItem analysisItem = null;
			ICmCustomItem vernacularItem = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var items = Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>();
				var lists = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
				var analysisList = lists.CreateUnowned("Analysis list", Cache.DefaultUserWs);
				analysisList.WsSelector = WritingSystemServices.kwsAnals;
				var vernacularList = lists.CreateUnowned("Vernacular list", Cache.DefaultUserWs);
				vernacularList.WsSelector = WritingSystemServices.kwsVerns;
				root = items.Create();
				analysisList.PossibilitiesOS.Add(root);
				analysisItem = items.Create();
				analysisList.PossibilitiesOS.Add(analysisItem);
				analysisItem.Name.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("analysis item", Cache.DefaultAnalWs));
				vernacularItem = items.Create();
				vernacularList.PossibilitiesOS.Add(vernacularItem);
				vernacularItem.Name.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("vernacular item", Cache.DefaultVernWs));
				root.RestrictionsRC.Add(analysisItem);
				root.RestrictionsRC.Add(vernacularItem);
			});
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "custom-items"),
				layoutXml);
			var source = CreateSource(layouts, partsXml);

			var composed = DetailComposer.Compose(root, Cache, "Normal", source: source.GetSnapshot);

			var labelsByItem = composed.Model.Fields.Where(field => field.Field == "Name")
				.ToLookup(field => field.ObjectHvo, field => field.Label);
			Assert.That(labelsByItem[analysisItem.Hvo], Is.EqualTo(new[] { "Analysis Marker" }),
				"an item of the analysis-selector list composes the CmPossibilityA layout");
			Assert.That(labelsByItem[vernacularItem.Hvo], Is.EqualTo(new[] { "Vernacular Marker" }),
				"an item of the vernacular-selector list composes ITS list's CmPossibilityV layout, "
				+ "not the layout compiled for the previous item of the same class");
		}

		[Test]
		public void Compose_MissingNestedProjectLayoutThrows()
		{
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "nested-fallback"),
				"<LayoutInventory><layout class='LexEntry' type='detail' name='Normal'/>"
				+ "</LayoutInventory>");
			PersistLayout(layouts, includeSenses: true, injectSenseGloss: true);
			var source = CreateSource(layouts);

			Assert.That(() => DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot),
				Throws.InstanceOf<InvalidOperationException>());
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
		public void Compose_NestedSublayoutChoiceSelectsExactVariantAndResetsPersistenceRoot()
		{
			var stemGuid = MoMorphTypeTags.kguidMorphStem.ToString();
			var prefixGuid = MoMorphTypeTags.kguidMorphPrefix.ToString();
			var layoutXml = $@"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal'><part ref='SelectedForm'/></layout>
  <layout class='MoStemAllomorph' type='detail' name='Normal'>
    <sublayout name='Variant' layoutChoiceField='MorphType'/>
  </layout>
  <layout class='MoStemAllomorph' type='detail' name='Variant' choiceGuid='{stemGuid}'>
    <part ref='BaseForm'/>
  </layout>
  <layout class='MoStemAllomorph' type='detail' name='Variant' choiceGuid='{prefixGuid}'>
    <part ref='InjectedIsAbstract'/>
  </layout>
</LayoutInventory>";
			const string partsXml = @"
<PartInventory><bin>
  <part id='LexEntry-Detail-SelectedForm'>
    <obj field='LexemeForm' layout='Normal'/>
  </part>
  <part id='MoStemAllomorph-Detail-BaseForm'>
    <slice label='Base Form' editor='multistring' field='Form'/>
  </part>
  <part id='MoStemAllomorph-Detail-InjectedIsAbstract'>
    <slice label='Abstract' editor='boolean' field='IsAbstract'/>
  </part>
</bin></PartInventory>";
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "nested-choice"),
				layoutXml);
			var source = CreateSource(layouts, partsXml);
			var morphTypes = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_lexemeForm.MorphTypeRA = morphTypes.GetObject(MoMorphTypeTags.kguidMorphStem));

			var stem = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot)
				.Model.Fields.Single();

			Assert.That(stem.Field, Is.EqualTo("Form"));
			Assert.That(stem.LayoutName, Is.EqualTo("Variant"));
			Assert.That(stem.ChoiceGuid, Is.EqualTo(stemGuid).IgnoreCase);
			Assert.That(stem.LayoutPath, Has.Count.EqualTo(1));

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_lexemeForm.MorphTypeRA = morphTypes.GetObject(MoMorphTypeTags.kguidMorphPrefix));
			var prefix = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot)
				.Model.Fields.Single();

			Assert.That(prefix.Field, Is.EqualTo("IsAbstract"));
			Assert.That(prefix.ChoiceGuid, Is.EqualTo(prefixGuid).IgnoreCase);
			Assert.That(prefix.LayoutSliceIdentity, Is.Not.EqualTo(stem.LayoutSliceIdentity));
		}

		[Test]
		public void Compose_ObjectCallerChildrenUnifyWithExistingTargetLayout()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal'>
    <part ref='LexemeForm'><part ref='InjectedIsAbstract'/></part>
  </layout>
  <layout class='MoStemAllomorph' type='detail' name='Normal'>
    <part ref='BaseForm'/>
  </layout>
</LayoutInventory>";
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "object-unify"), layoutXml);
			var source = CreateSource(layouts);

			var composed = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);
			var fields = composed.Model.Fields.Where(field => field.ObjectHvo == m_lexemeForm.Hvo)
				.Select(field => field.Field);

			Assert.That(fields, Is.EqualTo(new[] { "Form", "IsAbstract" }));
		}

		[Test]
		public void Compose_SequenceCallerChildrenUnifyWithExistingTargetLayout()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal'>
				<part ref='Senses' param='Normal'><part ref='InjectedDefinition'/></part>
  </layout>
  <layout class='LexSense' type='detail' name='Normal'>
    <part ref='ProjectGloss'/>
  </layout>
</LayoutInventory>";
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "sequence-unify"), layoutXml);
			var source = CreateSource(layouts);

			var composed = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);
			var fields = composed.Model.Fields.Where(field => field.ObjectHvo == m_sense.Hvo
				&& field.Kind != DetailFieldKind.Header)
				.Select(field => field.Field);

			Assert.That(fields, Is.EqualTo(new[] { "Gloss", "Definition" }));
		}

		[Test]
		public void Compose_RepeatedSequenceCallersKeepTheirUnifiedMenuBindingsSeparate()
		{
			const string layoutXml = @"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal'>
				<part ref='Senses' param='Normal'><part ref='InjectedDefinition' menu='menu-one'/></part>
				<part ref='Senses' param='Normal'><part ref='InjectedDefinition' menu='menu-two'/></part>
  </layout>
  <layout class='LexSense' type='detail' name='Normal'>
    <part ref='ProjectGloss'/>
  </layout>
</LayoutInventory>";
			var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "repeated-sequence"),
				layoutXml);
			var source = CreateSource(layouts);

			var composed = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);
			var itemMenus = composed.Model.Fields
				.Where(field => field.Kind == DetailFieldKind.Header
					&& field.ObjectHvo == m_sense.Hvo)
				.Select(field => field.MenuId);

			Assert.That(itemMenus, Is.EqualTo(new[] { "menu-one", "menu-two" }));
		}

		[Test]
		public void Compose_ShowAllRevealBypassesOnlyTheExactFwLayoutPartRestriction()
		{
			CoreWritingSystemDefinition second = null;
			CoreWritingSystemDefinition third = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out third);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(third);
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second);
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(third);
				m_entry.CitationForm.set_String(second.Handle,
					TsStringUtils.MakeString("casa-es", second.Handle));
			});
			try
			{
				var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "show-all"));
				PersistLayout(layouts, visibleWritingSystems: "fr");
				var source = CreateSource(layouts);
				var restricted = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);
				var field = restricted.Model.Fields.Single(item => item.Field == "CitationForm");
				Assert.That(field.Values.Select(value => value.WsTag), Is.EqualTo(new[] { "fr" }),
					"the configured list restricts the row to its selected alternatives");

				var revealed = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot,
					showAllWritingSystemsSlices: new HashSet<DetailLayoutSliceIdentity>
						{ field.LayoutSliceIdentity });

				Assert.That(revealed.Model.Fields.Single(item => item.Field == "CitationForm")
					.Values.Select(value => value.WsTag), Is.EqualTo(new[] { "fr", "es", "de" }));
			}
			finally
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second);
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(third);
					Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Remove(second);
					Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Remove(third);
				});
			}
		}

		[Test]
		public void Compose_ShowAllRevealIncludesEverySequenceOccurrenceOfTheSamePart()
		{
			CoreWritingSystemDefinition second = null;
			CoreWritingSystemDefinition third = null;
			ILexSense secondSense = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out third);
				Cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(third);
				Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Remove(second);
				Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Remove(third);
				m_sense.Gloss.set_String(second.Handle,
					TsStringUtils.MakeString("house-es", second.Handle));
				secondSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(secondSense);
				secondSense.Gloss.set_String(second.Handle,
					TsStringUtils.MakeString("home-es", second.Handle));
			});
			try
			{
				const string layoutXml = @"
<LayoutInventory>
  <layout class='LexEntry' type='detail' name='Normal'>
    <part ref='Senses' param='Normal'/>
  </layout>
  <layout class='LexSense' type='detail' name='Normal'>
    <part ref='ProjectGloss' visibleWritingSystems='fr'/>
  </layout>
</LayoutInventory>";
				var layouts = CreateLayoutInventory(Path.Combine(m_projectPath, "show-all-sequence"),
					layoutXml);
				var source = CreateSource(layouts);
				var restricted = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot);
				var glosses = restricted.Model.Fields.Where(field => field.Field == "Gloss").ToList();
				Assert.That(glosses, Has.Count.EqualTo(2));

				var revealed = DetailComposer.Compose(m_entry, Cache, source: source.GetSnapshot,
					showAllWritingSystemsSlices: new HashSet<DetailLayoutSliceIdentity>
						{ glosses[0].LayoutSliceIdentity });
				var revealedGlosses = revealed.Model.Fields
					.Where(field => field.Field == "Gloss").ToList();

				Assert.That(revealedGlosses, Has.Count.EqualTo(2));
				Assert.That(revealedGlosses[0].Values.Select(value => value.WsTag),
					Does.Contain("de"));
				Assert.That(revealedGlosses[1].Values.Select(value => value.WsTag),
					Does.Contain("de"),
					"Show All applies to every occurrence expanded from the same caller part");
			}
			finally
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Remove(second);
					Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Remove(third);
					Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Remove(second);
					Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Remove(third);
				});
			}
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
		public void CompileForObject_ProjectSourceMissingLayoutReturnsNull()
		{
			ViewDefinitionSourceResolver source = (obj, layoutName, choiceGuid, callerXml) => null;

			var compiled = DetailComposer.CompileForObject(Cache, m_entry, "Normal", source);

			Assert.That(compiled, Is.Null);
		}

		[Test]
		public void CompileForObject_NoProjectSourceFallsBackToShippedLayout()
		{
			var compiled = DetailComposer.CompileForObject(Cache, m_entry, "Normal");

			Assert.That(compiled, Is.Not.Null);
			Assert.That(compiled.Roots, Has.Count.GreaterThan(2));
		}

		[Test]
		public void CompileForObject_SourceExceptionPropagates()
		{
			ViewDefinitionSourceResolver source = (obj, layoutName, choiceGuid, callerXml) =>
				throw new InvalidOperationException("source failed");

			Assert.That(() => DetailComposer.CompileForObject(Cache, m_entry, "Normal", source),
				Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("source failed"));
		}

		private InventoryViewDefinitionSource CreateSource(Inventory layouts,
			string partsXml = PartsXml)
		{
			var parts = new Inventory("*Parts.xml", "/PartInventory/bin/*",
				new Dictionary<string, string[]> { ["part"] = new[] { "id" } },
				"ProjectLayoutCompositionTests", "unused");
			parts.LoadElements(partsXml, 0);
			return new InventoryViewDefinitionSource(layouts, parts.Root.OuterXml,
				Cache.MetaDataCacheAccessor);
		}

		private static Inventory CreateLayoutInventory(string projectPath,
			string layoutXml = LayoutXml)
		{
			var layouts = new Inventory("*.fwlayout", "/LayoutInventory/*",
				new Dictionary<string, string[]>
				{
					["layout"] = new[] { "class", "type", "name", "choiceGuid" },
					["part"] = new[] { "ref" }
				}, "ProjectLayoutCompositionTests", projectPath);
			layouts.LoadElements(layoutXml, 0);
			return layouts;
		}

		private static void PersistLayout(Inventory layouts, string citationVisibility = "always",
			bool reverse = false, bool includeSenses = false, bool injectSenseGloss = false,
			string visibleWritingSystems = null)
		{
			var visible = visibleWritingSystems == null
				? string.Empty
				: " visibleWritingSystems='" + visibleWritingSystems + "'";
			var first = reverse
				? "<part ref='Bibliography' visibility='always'/>"
				: "<part ref='CitationForm' visibility='" + citationVisibility + "'" + visible + "/>";
			var second = reverse
				? "<part ref='CitationForm' visibility='" + citationVisibility + "'" + visible + "/>"
				: "<part ref='Bibliography' visibility='always'/>";
			var document = new XmlDocument();
			var senses = injectSenseGloss
				? "<part ref='Senses' param='Normal'><part ref='ProjectGloss'/></part>"
				: "<part ref='Senses' param='Normal'/>";
			document.LoadXml("<layout class='LexEntry' type='detail' name='Normal'>"
				+ first + second + (includeSenses ? senses : "")
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
