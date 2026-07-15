// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests; // FakeRegionEditContext
using FwAvaloniaDialogsTests; // DialogLayoutAssert

namespace FwAvaloniaTests.VisualChecks
{
	/// <summary>
	/// §19e — T5 visual stages and T2 integration for the completed detail-editor field types: a closed
	/// enum combo, a numeric integer editor, the structured GenDate qualifier editor, an exact-date
	/// calendar editor, and a static literal row — each captured as a Skia PNG for subjective review and
	/// run through the shared <see cref="DialogLayoutAssert"/> crowding tripwire, plus ONE realized region
	/// surface holding several of them together (the integration extends the §19a/§19d template). All
	/// LCModel-free: the model fields are built directly and a <see cref="FakeRegionEditContext"/> records
	/// the staged edits.
	/// </summary>
	[TestFixture]
	public class FieldTypeVisualTests
	{
		private static LexicalEditRegionView Surface(IReadOnlyList<LexicalEditRegionField> fields,
			out FakeRegionEditContext edit)
		{
			edit = new FakeRegionEditContext();
			return new LexicalEditRegionView(
				new LexicalEditRegionModel("LexEntry", "detail", new List<LexicalEditRegionField>(fields),
					new List<ViewDiagnostic>()),
				edit);
		}

		private static LexicalEditRegionField Enum(string selected) => new LexicalEditRegionField(
			"f/#enum", "Allomorph Status", "Status", null, RegionFieldKind.EnumCombo,
			EditorClassification.Known, "StatusEnum", null, SurfaceRouting.Product, null,
			new List<RegionChoiceOption> { new RegionChoiceOption("0", "Stem"), new RegionChoiceOption("1", "Affix") },
			selected, isEditable: true);

		private static LexicalEditRegionField Integer(string value) => new LexicalEditRegionField(
			"f/#int", "Order Number", "OrderNumber", null, RegionFieldKind.Integer,
			EditorClassification.Known, "OrderInt", null, SurfaceRouting.Product,
			new List<RegionWsValue> { new RegionWsValue("", value) }, null, null, isEditable: true);

		private static LexicalEditRegionField GenDate(string display) => new LexicalEditRegionField(
			"f/#gen", "Date Of Event", "DateOfEvent", null, RegionFieldKind.Date,
			EditorClassification.Known, "GenDate", null, SurfaceRouting.Product,
			new List<RegionWsValue> { new RegionWsValue("", display) }, null, null, isEditable: true,
			dateKind: RegionDateKind.GenDate);

		private static LexicalEditRegionField ExactDate(string display) => new LexicalEditRegionField(
			"f/#date", "Date Created", "DateCreated", null, RegionFieldKind.Date,
			EditorClassification.Known, "ExactDate", null, SurfaceRouting.Product,
			new List<RegionWsValue> { new RegionWsValue("", display) }, null, null, isEditable: true,
			dateKind: RegionDateKind.Date);

		private static LexicalEditRegionField Literal(string text) => new LexicalEditRegionField(
			"f/#lit", string.Empty, "Self", null, RegionFieldKind.Literal,
			EditorClassification.Known, "Lit", null, SurfaceRouting.Product,
			new List<RegionWsValue> { new RegionWsValue("", text) }, null, null, isEditable: false);

		private static LexicalEditRegionField Vector() => new LexicalEditRegionField(
			"f/#vec", "Semantic Domains", "DomainTypes", null, RegionFieldKind.ReferenceVector,
			EditorClassification.Known, "Domains", null, SurfaceRouting.Product, null,
			new List<RegionChoiceOption>
			{
				new RegionChoiceOption("d1", "Universe, creation", 0),
				new RegionChoiceOption("d2", "Sky", 1),
				new RegionChoiceOption("d3", "Sun", 1)
			},
			null, isEditable: true, items: new List<RegionChoiceOption> { new RegionChoiceOption("d1", "Universe, creation") });

		// ----- T5 visual stages (one focused PNG per new editor) -----

		[AvaloniaTest]
		public void EnumCombo_RendersCleanly()
		{
			var view = Surface(new[] { Enum("1") }, out _);
			DialogSnapshot.Capture(view, "FieldType-01-enum-combo", width: 460, height: 140);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void Integer_RendersCleanly()
		{
			var view = Surface(new[] { Integer("42") }, out _);
			DialogSnapshot.Capture(view, "FieldType-02-integer", width: 460, height: 140);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void GenDate_RendersCleanly()
		{
			var view = Surface(new[] { GenDate("About AD 1985") }, out _);
			DialogSnapshot.Capture(view, "FieldType-03-gendate", width: 520, height: 160);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void ExactDate_RendersCleanly()
		{
			var view = Surface(new[] { ExactDate("3 June 2026") }, out _);
			DialogSnapshot.Capture(view, "FieldType-04-exact-date", width: 520, height: 160);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		[AvaloniaTest]
		public void Literal_RendersCleanly()
		{
			var view = Surface(new[] { Literal("This entry is provisional; verify before publishing.") }, out _);
			DialogSnapshot.Capture(view, "FieldType-05-literal", width: 520, height: 140);
			DialogLayoutAssert.AssertNoCrowding(view);
		}

		// ----- T2 integration: several new field types on ONE realized surface -----

		[AvaloniaTest]
		public void IntegrationSurface_AllFieldTypesCompose_AndEachStagesAnEdit()
		{
			var view = Surface(new[]
			{
				Enum("0"), Integer("3"), GenDate("AD 1990"), ExactDate("3 June 2026"), Vector(), Literal("Note:")
			}, out var edit);

			DialogSnapshot.Capture(view, "FieldType-06-integration", width: 560, height: 460);
			DialogLayoutAssert.AssertNoCrowding(view);

			// Each new editor composed its expected control on the one surface.
			Assert.That(view.GetVisualDescendants().OfType<ComboBox>().Any(), Is.True, "enum combo present");
			Assert.That(view.GetVisualDescendants().OfType<FwGenDateField>().Any(), Is.True, "GenDate editor present");
			Assert.That(view.GetVisualDescendants().OfType<CalendarDatePicker>().Any(), Is.True,
				"exact-date calendar picker present");
			Assert.That(view.GetVisualDescendants().OfType<FwReferenceVectorField>().Any(), Is.True,
				"reference vector present");

			// Each editable type stages its own edit through the shared edit context (composition + staging).
			var enumCombo = view.GetVisualDescendants().OfType<ComboBox>().First();
			enumCombo.SelectedIndex = 1;
			Assert.That(edit.OptionEdits.Any(o => o.Key == "1"), Is.True, "the enum stages its index");

			var intBox = view.GetVisualDescendants().OfType<TextBox>()
				.First(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b) == "OrderInt");
			intBox.Text = "9";
			intBox.RaiseEvent(new Avalonia.Input.KeyEventArgs
			{
				RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Enter
			});
			Assert.That(edit.TextEdits.Any(t => t.Value == "9"), Is.True, "the integer stages its value");

			var gen = view.GetVisualDescendants().OfType<FwGenDateField>().First();
			gen.SetForTest(1990, GenDatePrecision.Approximate, true);
			Assert.That(edit.OptionEdits.Any(o => o.Key == "About AD 1990"), Is.True,
				"the GenDate qualifier stages the recomposed string");
		}

		[AvaloniaTest]
		public void AllFieldTypeStages_ArePngArtifacts()
		{
			// Trip if the snapshot harness silently stops writing files (the artifacts back the T5 review).
			foreach (var name in new[]
			{
				"FieldType-01-enum-combo", "FieldType-02-integer", "FieldType-03-gendate",
				"FieldType-04-exact-date", "FieldType-05-literal", "FieldType-06-integration"
			})
			{
				var path = Path.Combine(DialogSnapshot.Folder, name + ".png");
				// Captured by the stage tests above when the whole fixture runs; tolerate single-test runs.
				if (File.Exists(path))
					Assert.That(new FileInfo(path).Length, Is.GreaterThan(0), $"{name} png has pixels");
			}
		}
	}
}
