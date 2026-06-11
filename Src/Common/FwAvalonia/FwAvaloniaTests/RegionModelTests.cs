// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
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
	/// A fake value provider so the mapper can be tested without LCModel. The LCModel-backed provider
	/// lives in xWorks (<c>LexicalEditRegionBuilder</c>).
	/// </summary>
	internal sealed class FakeRegionValueProvider : IRegionValueProvider
	{
		public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
		{
			switch (fieldNode.Field)
			{
				case "LexemeForm":
					return new List<RegionWsValue> { new RegionWsValue("vern", "dog", "Charis SIL", 12) };
				case "Gloss":
					return new List<RegionWsValue> { new RegionWsValue("anal", "canine") };
				default:
					return new List<RegionWsValue>();
			}
		}

		public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode)
			=> new List<RegionChoiceOption> { new RegionChoiceOption("stem", "stem"), new RegionChoiceOption("suffix", "suffix") };

		public string GetSelectedOptionKey(ViewNode fieldNode) => "suffix";
	}

	[TestFixture]
	public class LexicalEditRegionMapperTests
	{
		private static ViewDefinitionModel SampleDefinition()
		{
			var roots = new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "LexemeForm", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: SurfaceRouting.Product),
				new ViewNode("LexEntry/identity/#1", ViewNodeKind.Field, "Morph Type", null, "MorphType", "morphtypeatomicreference",
					EditorClassification.Known, null, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "MorphTypeChooser", routing: SurfaceRouting.Product),
				new ViewNode("LexEntry/identity/#2", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "SenseGlossEditor", routing: SurfaceRouting.Product)
			};
			return new ViewDefinitionModel("LexEntry", "identity", "detail", roots, new List<ViewDiagnostic>());
		}

		[Test]
		public void FromViewDefinition_ProjectsFields_FromTheTypedDefinition()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new FakeRegionValueProvider());

			Assert.That(model.ClassName, Is.EqualTo("LexEntry"));
			Assert.That(model.Fields.Select(f => f.Field), Is.EqualTo(new[] { "LexemeForm", "MorphType", "Gloss" }));
		}

		[Test]
		public void TextFields_AreClassifiedAsText_AndBoundToValues()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new FakeRegionValueProvider());
			var lexeme = model.Fields.Single(f => f.Field == "LexemeForm");

			Assert.That(lexeme.Kind, Is.EqualTo(RegionFieldKind.Text));
			Assert.That(lexeme.Values.Single().Value, Is.EqualTo("dog"));
			Assert.That(lexeme.AutomationId, Is.EqualTo("LexemeFormEditor"));
		}

		[Test]
		public void ChooserField_IsClassifiedAsChooser_WithOptionsAndSelection()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new FakeRegionValueProvider());
			var morph = model.Fields.Single(f => f.Field == "MorphType");

			Assert.That(morph.Kind, Is.EqualTo(RegionFieldKind.Chooser));
			Assert.That(morph.Options.Select(o => o.Key), Is.EqualTo(new[] { "stem", "suffix" }));
			Assert.That(morph.SelectedOptionKey, Is.EqualTo("suffix"));
		}

		[Test]
		public void NeverVisibleFields_AreExcluded()
		{
			var roots = new List<ViewNode>
			{
				new ViewNode("x/#0", ViewNodeKind.Field, "Hidden", null, "Hidden", "multistring",
					EditorClassification.Known, null, ViewVisibility.Never, ViewExpansion.NotApplicable, false, null, null)
			};
			var def = new ViewDefinitionModel("LexEntry", "identity", "detail", roots, new List<ViewDiagnostic>());

			var model = LexicalEditRegionMapper.FromViewDefinition(def, new FakeRegionValueProvider());
			Assert.That(model.Fields, Is.Empty);
		}

		[Test]
		public void ObsoleteEditor_IsClassifiedUnsupported()
		{
			var roots = new List<ViewNode>
			{
				new ViewNode("x/#0", ViewNodeKind.Field, "Old", null, "Old", "message",
					EditorClassification.Obsolete, null, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null)
			};
			var def = new ViewDefinitionModel("LexEntry", "identity", "detail", roots, new List<ViewDiagnostic>());

			var model = LexicalEditRegionMapper.FromViewDefinition(def, new FakeRegionValueProvider());
			Assert.That(model.Fields.Single().Kind, Is.EqualTo(RegionFieldKind.Unsupported));
		}

		[Test]
		public void Diagnostics_ArePreserved_FromTheDefinition()
		{
			var diags = new List<ViewDiagnostic> { new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "x", "m", "p") };
			var def = new ViewDefinitionModel("LexEntry", "identity", "detail", new List<ViewNode>(), diags);

			var model = LexicalEditRegionMapper.FromViewDefinition(def, new FakeRegionValueProvider());
			Assert.That(model.Diagnostics, Has.Count.EqualTo(1));
		}
	}

	[TestFixture]
	public class LexicalEditRegionViewTests
	{
		private static ViewDefinitionModel SampleDefinition() => new ViewDefinitionModel(
			"LexEntry", "identity", "detail",
			new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "LexemeForm", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: SurfaceRouting.Product),
				new ViewNode("LexEntry/identity/#1", ViewNodeKind.Field, "Morph Type", null, "MorphType", "morphtypeatomicreference",
					EditorClassification.Known, null, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "MorphTypeChooser", routing: SurfaceRouting.Product)
			},
			new List<ViewDiagnostic>());

		[AvaloniaTest]
		public void RegionView_RendersFields_WithStableAutomationIds()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new FakeRegionValueProvider());
			var view = new LexicalEditRegionView(model);
			var window = new Window { Content = view, Width = 420, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(AutomationProperties.GetAutomationId(view), Is.EqualTo("LexicalEditRegionView"));

			var lexemeBox = view.GetVisualDescendants().OfType<TextBox>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "LexemeFormEditor.vern");
			Assert.That(lexemeBox, Is.Not.Null, "the text field should render a per-ws box with a stable automation id");
			Assert.That(lexemeBox.Text, Is.EqualTo("dog"));

			var chooser = view.GetVisualDescendants().OfType<ComboBox>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "MorphTypeChooser");
			Assert.That(chooser, Is.Not.Null, "the chooser field should render a combo box");
		}
	}
}
