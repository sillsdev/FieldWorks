// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the PNG harness
using FwAvaloniaDialogsTests;        // DialogLayoutAssert — the shared geometry tripwire

namespace FwAvaloniaTests
{
	/// <summary>
	/// The per-writing-system multistring editor (<see cref="FwMultiWsTextField"/>): the owned region
	/// control behind every multistring field. It renders ONE row per writing system — a small raised WS
	/// abbreviation hanging at the value start (the legacy 12.3 look) plus a flat, borderless value editor
	/// (RootSite parity: no per-value box). These pin the structure (one row per WS, the WS label, empty vs
	/// populated, single- vs multi-WS) and emit a PNG per stage for subjective review, paired with the
	/// AssertNoCrowding tripwire. The editing/commit/teardown behavior itself lives in the Region tests.
	/// </summary>
	[TestFixture]
	public class FwMultiWsTextFieldTests
	{
		private static LexicalEditRegionField Field(IReadOnlyList<RegionWsValue> values,
			string label = "Lexeme Form", string automationId = "LexEntry_Form", bool isEditable = true)
			=> new LexicalEditRegionField(
				stableId: "LexEntry/Form", label: label, field: "Form", writingSystem: null,
				kind: RegionFieldKind.Text, editorClassification: EditorClassification.Known,
				automationId: automationId, localizationKey: null, routing: SurfaceRouting.Product,
				values: values, options: null, selectedOptionKey: null, isEditable: isEditable);

		private static (FwMultiWsTextField Field, Window Window) Show(LexicalEditRegionField field,
			IRegionEditContext editContext = null, bool showAbbrev = true)
		{
			var control = new FwMultiWsTextField(field, field.AutomationId, editContext, null,
				showWritingSystemAbbreviation: showAbbrev);
			var window = new Window { Content = control, Width = 360, Height = 160 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			return (control, window);
		}

		private static IReadOnlyList<TextBox> Boxes(FwMultiWsTextField control)
			=> control.GetVisualDescendants().OfType<TextBox>().ToList();

		private static IReadOnlyList<string> AbbrevLabels(FwMultiWsTextField control, params string[] wanted)
			=> control.GetVisualDescendants().OfType<TextBlock>()
				.Select(t => t.Text)
				.Where(t => wanted.Contains(t))
				.ToList();

		[AvaloniaTest]
		public void EmptyMultiRow_RendersOneRowPerWritingSystem_WithTheWsLabels()
		{
			var field = Field(new List<RegionWsValue>
			{
				new RegionWsValue("en", string.Empty, wsTag: "en"),
				new RegionWsValue("fr", string.Empty, wsTag: "fr")
			});
			var (control, window) = Show(field);

			DialogSnapshot.Capture(window, "FwMultiWsTextField-01-empty-multi-row");
			DialogLayoutAssert.AssertNoCrowding(control);

			Assert.That(Boxes(control), Has.Count.EqualTo(2), "one editor row per writing system");
			Assert.That(AbbrevLabels(control, "en", "fr"), Is.EquivalentTo(new[] { "en", "fr" }),
				"each row shows its writing-system abbreviation label");
			Assert.That(Boxes(control).All(b => string.IsNullOrEmpty(b.Text)), Is.True, "empty rows hold no text");
		}

		[AvaloniaTest]
		public void Populated_ShowsTheValuesInEachWritingSystemRow()
		{
			var field = Field(new List<RegionWsValue>
			{
				new RegionWsValue("en", "house", wsTag: "en"),
				new RegionWsValue("fr", "maison", wsTag: "fr")
			});
			var (control, window) = Show(field);

			DialogSnapshot.Capture(window, "FwMultiWsTextField-02-populated");
			DialogLayoutAssert.AssertNoCrowding(control);

			Assert.That(Boxes(control).Select(b => b.Text), Is.EqualTo(new[] { "house", "maison" }),
				"each row shows its writing system's value");
			Assert.That(AbbrevLabels(control, "en", "fr"), Is.EquivalentTo(new[] { "en", "fr" }));
		}

		[AvaloniaTest]
		public void SingleWs_RendersExactlyOneRow()
		{
			var field = Field(new List<RegionWsValue> { new RegionWsValue("en", "casa", wsTag: "en") });
			var (control, window) = Show(field);

			DialogSnapshot.Capture(window, "FwMultiWsTextField-03-single-ws");
			DialogLayoutAssert.AssertNoCrowding(control);

			Assert.That(Boxes(control), Has.Count.EqualTo(1), "a single-WS field renders a single row");
			Assert.That(Boxes(control).Single().Text, Is.EqualTo("casa"));
			Assert.That(AbbrevLabels(control, "en"), Is.EqualTo(new[] { "en" }));
		}

		[AvaloniaTest]
		public void MultiWs_RendersARowForEveryWritingSystem_InOrder()
		{
			var field = Field(new List<RegionWsValue>
			{
				new RegionWsValue("en", "house", wsTag: "en"),
				new RegionWsValue("fr", "maison", wsTag: "fr"),
				new RegionWsValue("es", "casa", wsTag: "es")
			});
			var (control, window) = Show(field);

			DialogSnapshot.Capture(window, "FwMultiWsTextField-04-multi-ws");
			DialogLayoutAssert.AssertNoCrowding(control);

			Assert.That(Boxes(control).Select(b => b.Text), Is.EqualTo(new[] { "house", "maison", "casa" }),
				"every writing system gets its own row, in field order");
			Assert.That(AbbrevLabels(control, "en", "fr", "es"),
				Is.EqualTo(new[] { "en", "fr", "es" }), "the WS labels render in order");
		}
	}
}
