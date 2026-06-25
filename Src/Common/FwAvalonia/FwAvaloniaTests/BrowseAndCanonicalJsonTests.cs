// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 7.1 — the virtualized browse/table path: a 10k-row source realizes only visible rows and
	/// materializes only realized cells.
	/// </summary>
	[TestFixture]
	public class LexicalBrowseViewTests
	{
		private sealed class CountingRowSource : IBrowseRowSource
		{
			public int Materialized;

			public int RowCount => 10_000;

			public IReadOnlyList<string> GetCellValues(int rowIndex)
			{
				Materialized++;
				return new[] { $"lexeme {rowIndex}", $"gloss {rowIndex}" };
			}

			// Stable identity (Task 20). NOT counted as a materialization — and not called during render
			// for a checkbox-less table — so the lazy-realization assertions are unaffected.
			public int HvoAt(int rowIndex) => rowIndex + 1;
		}

		private static ViewDefinitionModel TwoColumnDefinition() => new ViewDefinitionModel(
			"LexEntry", "browse", "browse",
			new List<ViewNode>
			{
				new ViewNode("b/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null),
				new ViewNode("b/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null)
			},
			new List<ViewDiagnostic>());

		[AvaloniaTest]
		public void TenThousandRows_RealizeOnlyTheVisibleWindow()
		{
			var source = new CountingRowSource();
			var view = new LexicalBrowseView(TwoColumnDefinition(), source);
			var window = new Window { Content = view, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var realized = view.GetVisualDescendants().OfType<ListBoxItem>().Count();
			Assert.That(realized, Is.GreaterThan(0), "visible rows realize");
			Assert.That(realized, Is.LessThan(100), $"virtualization must cap realization (realized {realized} of 10000)");
			Assert.That(source.Materialized, Is.LessThan(300),
				$"cells materialize only for realized rows (materialized {source.Materialized} of 10000)");

			var header = view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => Avalonia.Automation.AutomationProperties.GetAutomationId(t) == "BrowseHeader.Form");
			Assert.That(header?.Text, Is.EqualTo("Lexeme Form"), "columns come from the typed definition");
		}
	}

	/// <summary>
	/// Tasks 9.2/9.4 — canonical JSON: the typed IR round-trips losslessly (snapshot-identical), the
	/// shipped first slice serializes and reloads with runtime XML fully out of the loop, and version
	/// mismatches are rejected rather than guessed.
	/// </summary>
	[TestFixture]
	public class ViewDefinitionJsonSerializerTests
	{
		private static string ShippedPartsDirectory()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
				dir = dir.Parent;
			Assert.That(dir, Is.Not.Null);
			return Path.Combine(dir.FullName, "DistFiles", "Language Explorer", "Configuration", "Parts");
		}

		[Test]
		public void CompiledFirstSlice_RoundTripsThroughCanonicalJson_SnapshotIdentical()
		{
			var compiled = LexicalEditFirstSlice.CompileFromLayoutDirectory(ShippedPartsDirectory());
			Assert.That(compiled, Is.Not.Null);

			var json = ViewDefinitionJsonSerializer.Serialize(compiled);
			var reloaded = ViewDefinitionJsonSerializer.Deserialize(json);

			Assert.That(reloaded.ToSnapshot(), Is.EqualTo(compiled.ToSnapshot()),
				"the canonical JSON lane must be lossless against the semantic snapshot");
			Assert.That(json, Does.Contain("\"formatVersion\": 1"));
		}

		[Test]
		public void Serialization_IsDeterministic()
		{
			var compiled = LexicalEditFirstSlice.AuthoredFallback();
			Assert.That(ViewDefinitionJsonSerializer.Serialize(compiled),
				Is.EqualTo(ViewDefinitionJsonSerializer.Serialize(LexicalEditFirstSlice.AuthoredFallback())));
		}

		[Test]
		public void UnsupportedFormatVersion_IsRejected()
		{
			Assert.That(() => ViewDefinitionJsonSerializer.Deserialize("{\"formatVersion\": 99, \"nodes\": []}"),
				Throws.InstanceOf<InvalidDataException>());
		}

		[Test]
		public void EveryViewNodeProperty_SurvivesRoundTrip()
		{
			// Every ViewNode property set to a non-default value so any silently dropped field fails.
			var child = new ViewNode(
				"n/#0/#0", ViewNodeKind.Field, "Child Label", "ch", "Gloss", "multistring",
				EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable,
				false, null, null);
			var node = new ViewNode(
				stableId: "n/#0",
				kind: ViewNodeKind.Sequence,
				label: "Senses",
				abbreviation: "sns",
				field: "Senses",
				rawEditor: "seq",
				editorClassification: EditorClassification.Known,
				writingSystem: "vernacular",
				visibility: ViewVisibility.IfData,
				expansion: ViewExpansion.Expanded,
				indented: true,
				targetLayout: "detail",
				children: new List<ViewNode> { child },
				localizationKey: "ksSenses",
				automationId: "Entry.Senses",
				routing: SurfaceRouting.Product,
				boldEmphasis: true,
				fontScalePercent: 120,
				menuId: "mnuDataTree-Sense",
				contextMenuId: "mnuDataTree-SenseContext",
				hotlinksId: "mnuDataTree-Sense-Hotlinks",
				ghostField: "Gloss",
				ghostWs: "analysis",
				ghostClass: "LexSense",
				ghostLabel: "Gloss",
				forVariant: true,
				ghostInitMethod: "SetMorphTypeToRoot",
				condition: new ViewCondition(
					negated: true,
					target: "owner",
					isClass: "CmPossibilityList",
					excludeSubclasses: true,
					field: "Depth",
					boolEquals: true,
					intEquals: 1,
					intLessThan: 2,
					intGreaterThan: 3,
					intMemberOf: "2,3,7",
					lengthAtLeast: 1,
					lengthAtMost: 4,
					guidEquals: "d7f713da-e8cf-11d3-9764-00c04f186933"),
				chooserLinks: new List<ViewChooserLink>
				{
					new ViewChooserLink("goto", "Edit the Publications list", "publicationsEdit"),
					new ViewChooserLink("simple", "Add a slot", "MakeInflAffixSlotChooserCommand", "TopPOS")
				});
			var model = new ViewDefinitionModel("LexEntry", "Normal", "detail",
				new List<ViewNode> { node }, new List<ViewDiagnostic>());

			var reloaded = ViewDefinitionJsonSerializer.Deserialize(ViewDefinitionJsonSerializer.Serialize(model));
			var r = reloaded.Roots[0];

			Assert.Multiple(() =>
			{
				Assert.That(r.StableId, Is.EqualTo("n/#0"), nameof(r.StableId));
				Assert.That(r.Kind, Is.EqualTo(ViewNodeKind.Sequence), nameof(r.Kind));
				Assert.That(r.Label, Is.EqualTo("Senses"), nameof(r.Label));
				Assert.That(r.Abbreviation, Is.EqualTo("sns"), nameof(r.Abbreviation));
				Assert.That(r.Field, Is.EqualTo("Senses"), nameof(r.Field));
				Assert.That(r.RawEditor, Is.EqualTo("seq"), nameof(r.RawEditor));
				Assert.That(r.EditorClassification, Is.EqualTo(EditorClassification.Known), nameof(r.EditorClassification));
				Assert.That(r.WritingSystem, Is.EqualTo("vernacular"), nameof(r.WritingSystem));
				Assert.That(r.Visibility, Is.EqualTo(ViewVisibility.IfData), nameof(r.Visibility));
				Assert.That(r.Expansion, Is.EqualTo(ViewExpansion.Expanded), nameof(r.Expansion));
				Assert.That(r.Indented, Is.True, nameof(r.Indented));
				Assert.That(r.TargetLayout, Is.EqualTo("detail"), nameof(r.TargetLayout));
				Assert.That(r.Children, Has.Count.EqualTo(1), nameof(r.Children));
				Assert.That(r.Children[0].StableId, Is.EqualTo("n/#0/#0"), "child StableId");
				Assert.That(r.LocalizationKey, Is.EqualTo("ksSenses"), nameof(r.LocalizationKey));
				Assert.That(r.AutomationId, Is.EqualTo("Entry.Senses"), nameof(r.AutomationId));
				Assert.That(r.Routing, Is.EqualTo(SurfaceRouting.Product), nameof(r.Routing));
				Assert.That(r.BoldEmphasis, Is.True, nameof(r.BoldEmphasis));
				Assert.That(r.FontScalePercent, Is.EqualTo(120), nameof(r.FontScalePercent));
				Assert.That(r.MenuId, Is.EqualTo("mnuDataTree-Sense"), nameof(r.MenuId));
				Assert.That(r.ContextMenuId, Is.EqualTo("mnuDataTree-SenseContext"), nameof(r.ContextMenuId));
				Assert.That(r.HotlinksId, Is.EqualTo("mnuDataTree-Sense-Hotlinks"), nameof(r.HotlinksId));
				Assert.That(r.GhostField, Is.EqualTo("Gloss"), nameof(r.GhostField));
				Assert.That(r.GhostWs, Is.EqualTo("analysis"), nameof(r.GhostWs));
				Assert.That(r.GhostClass, Is.EqualTo("LexSense"), nameof(r.GhostClass));
				Assert.That(r.GhostLabel, Is.EqualTo("Gloss"), nameof(r.GhostLabel));
				Assert.That(r.ForVariant, Is.True, nameof(r.ForVariant));
				Assert.That(r.GhostInitMethod, Is.EqualTo("SetMorphTypeToRoot"), nameof(r.GhostInitMethod));
				Assert.That(r.Condition, Is.Not.Null, nameof(r.Condition));
				Assert.That(r.Condition.Negated, Is.True, "Condition.Negated");
				Assert.That(r.Condition.Target, Is.EqualTo("owner"), "Condition.Target");
				Assert.That(r.Condition.IsClass, Is.EqualTo("CmPossibilityList"), "Condition.IsClass");
				Assert.That(r.Condition.ExcludeSubclasses, Is.True, "Condition.ExcludeSubclasses");
				Assert.That(r.Condition.Field, Is.EqualTo("Depth"), "Condition.Field");
				Assert.That(r.Condition.BoolEquals, Is.True, "Condition.BoolEquals");
				Assert.That(r.Condition.IntEquals, Is.EqualTo(1), "Condition.IntEquals");
				Assert.That(r.Condition.IntLessThan, Is.EqualTo(2), "Condition.IntLessThan");
				Assert.That(r.Condition.IntGreaterThan, Is.EqualTo(3), "Condition.IntGreaterThan");
				Assert.That(r.Condition.IntMemberOf, Is.EqualTo("2,3,7"), "Condition.IntMemberOf");
				Assert.That(r.Condition.LengthAtLeast, Is.EqualTo(1), "Condition.LengthAtLeast");
				Assert.That(r.Condition.LengthAtMost, Is.EqualTo(4), "Condition.LengthAtMost");
				Assert.That(r.Condition.GuidEquals, Is.EqualTo("d7f713da-e8cf-11d3-9764-00c04f186933"),
					"Condition.GuidEquals");
				// B7: the chooser jump-link block (label/tool/type/target) survives, including the
				// "goto" default-type omission.
				Assert.That(r.ChooserLinks, Has.Count.EqualTo(2), nameof(r.ChooserLinks));
				Assert.That(r.ChooserLinks[0].Type, Is.EqualTo("goto"), "ChooserLinks[0].Type");
				Assert.That(r.ChooserLinks[0].Label, Is.EqualTo("Edit the Publications list"), "ChooserLinks[0].Label");
				Assert.That(r.ChooserLinks[0].Tool, Is.EqualTo("publicationsEdit"), "ChooserLinks[0].Tool");
				Assert.That(r.ChooserLinks[0].Target, Is.Null, "ChooserLinks[0].Target");
				Assert.That(r.ChooserLinks[1].Type, Is.EqualTo("simple"), "ChooserLinks[1].Type");
				Assert.That(r.ChooserLinks[1].Target, Is.EqualTo("TopPOS"), "ChooserLinks[1].Target");
			});
		}
	}

	/// <summary>
	/// Task 9.3 (override-fixture lane): user-override-shaped layout XML — label/visibility overrides
	/// and a hidden part — imports with the overrides surfaced in the typed IR.
	/// </summary>
	[TestFixture]
	public class OverrideFixtureImportTests
	{
		private const string PartsXml = @"
<PartInventory><bin>
  <part id='LexEntry-Detail-CitationForm'>
    <slice label='Citation Form' editor='multistring' field='CitationForm' ws='vernacular'/>
  </part>
  <part id='LexEntry-Detail-Bibliography'>
    <slice label='Bibliography' editor='multistring' field='Bibliography' ws='analysis'/>
  </part>
</bin></PartInventory>";

		[Test]
		public void UserOverrides_LabelRenameHideAndReorder_SurfaceInTheIR()
		{
			// The shape Inventory.PersistOverrideElement stores: a full layout copy with user edits.
			var overridden = @"
<layout class='LexEntry' type='detail' name='Normal%01'>
  <part ref='Bibliography' label='Sources' visibility='always'/>
  <part ref='CitationForm' visibility='never'/>
</layout>";
			var parts = new DictionaryPartResolver(System.Xml.Linq.XElement.Parse(PartsXml));
			var model = new XmlLayoutImporter().Import(System.Xml.Linq.XElement.Parse(overridden), parts);

			Assert.That(model.LayoutName, Is.EqualTo("Normal%01"), "user override layout names import");
			Assert.That(model.Roots[0].Field, Is.EqualTo("Bibliography"), "user reorder is honored");
			Assert.That(model.Roots[0].Label, Is.EqualTo("Sources"), "user label rename overrides the part label");
			Assert.That(model.Roots[1].Visibility, Is.EqualTo(ViewVisibility.Never), "user hide is honored");

			// And the override round-trips the canonical JSON lane too.
			var reloaded = ViewDefinitionJsonSerializer.Deserialize(ViewDefinitionJsonSerializer.Serialize(model));
			Assert.That(reloaded.ToSnapshot(), Is.EqualTo(model.ToSnapshot()));
		}
	}
}
