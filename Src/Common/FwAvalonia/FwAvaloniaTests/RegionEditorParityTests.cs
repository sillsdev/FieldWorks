// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Editor-type parity for the lexical detail view (WinForms → Avalonia):
	/// Task A — an editable Date/GenDate row stages + commits a parseable value and rejects garbage;
	/// Task B — the importer carries an enumComboBox's stringList ids/group onto the node so the row
	/// can render a closed option chooser instead of a raw read-only int;
	/// Task C — FwReferenceVectorField.Dispose detaches every handler it wired (count >0 → 0).
	/// </summary>
	[TestFixture]
	public class RegionEditorParityTests
	{
		// ---- Task A: editable Date / GenDate ----

		private static LexicalEditRegionField DateField(IRegionEditContext editContext,
			RegionDateKind dateKind, string display)
			=> new LexicalEditRegionField(
				stableId: "d1", label: "When", field: "When", writingSystem: null,
				kind: RegionFieldKind.Date, editorClassification: EditorClassification.Known,
				automationId: "When.Auto", localizationKey: null, routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("", display) },
				options: null, selectedOptionKey: null, isEditable: true, dateKind: dateKind);

		// §19e: the exact-date editor is now a text box + calendar picker row; the text box (the
		// canonical parse-on-commit entry) is extracted from that row for these parity assertions.
		private static TextBox BuildDate(FakeRegionEditContext ctx, RegionDateKind kind, string display,
			System.Action save = null)
		{
			var control = RegionFieldControlFactory.Build(DateField(ctx, kind, display), "When.Auto",
				new RegionFieldControlContext(editContext: ctx, save: save));
			var window = new Window { Content = control, Width = 320, Height = 80 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return control as TextBox
				?? control.GetVisualDescendants().OfType<TextBox>().First();
		}

		[AvaloniaTest]
		public void DateEdit_StagesAndCommitsOnEnter()
		{
			var ctx = new FakeRegionEditContext();
			var commits = 0;
			var box = BuildDate(ctx, RegionDateKind.Date, "January 1, 2000", () => commits++);

			box.Text = "March 5, 2010";
			box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
			Dispatcher.UIThread.RunJobs();

			// A date stages through the option seam (a single string the composer setter parses).
			Assert.That(ctx.OptionEdits, Has.Count.EqualTo(1));
			Assert.That(ctx.OptionEdits[0], Is.EqualTo(("When", "March 5, 2010")));
			Assert.That(commits, Is.EqualTo(1), "a successful date stage triggers the autosave (Save) once");
		}

		[AvaloniaTest]
		public void DateEdit_CommitsOnFocusLoss()
		{
			// §19e: an EXACT date keeps the parse-on-commit text box; focus loss stages the typed string.
			// (The generic-date row is now the structured qualifier editor — covered by FwGenDateField tests.)
			var ctx = new FakeRegionEditContext();
			var box = BuildDate(ctx, RegionDateKind.Date, "January 1, 2000");

			box.Text = "March 5, 2010";
			box.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(ctx.OptionEdits, Has.Count.EqualTo(1));
			Assert.That(ctx.OptionEdits[0].Key, Is.EqualTo("March 5, 2010"));
		}

		[AvaloniaTest]
		public void GenDate_StructuredEditor_StagesAComposedQualifierString()
		{
			// §19e: the generic-date row is the structured qualifier editor (year + precision + era), not a
			// free-text box. Changing a qualifier stages the recomposed GenDate.TryParse-compatible string.
			var ctx = new FakeRegionEditContext();
			var control = RegionFieldControlFactory.Build(DateField(ctx, RegionDateKind.GenDate, "AD 2000"),
				"When.Auto", new RegionFieldControlContext(editContext: ctx, save: () => { }));
			Assert.That(control, Is.InstanceOf<FwGenDateField>());
			((FwGenDateField)control).SetForTest(1850, GenDatePrecision.Approximate, true);

			Assert.That(ctx.OptionEdits, Has.Count.EqualTo(1));
			Assert.That(ctx.OptionEdits[0].Key, Is.EqualTo("About AD 1850"));
		}

		[AvaloniaTest]
		public void DateEdit_EmptyBox_StagesEmpty_ToClearTheField()
		{
			var ctx = new FakeRegionEditContext();
			var box = BuildDate(ctx, RegionDateKind.Date, "January 1, 2000");

			box.Text = string.Empty;
			box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
			Dispatcher.UIThread.RunJobs();

			// Empty is a real edit (the composer setter clears the field) — it must stage, not be swallowed.
			Assert.That(ctx.OptionEdits, Has.Count.EqualTo(1));
			Assert.That(ctx.OptionEdits[0].Key, Is.EqualTo(string.Empty));
		}

		[AvaloniaTest]
		public void DateEdit_RejectedStage_RestoresTheCommittedValue()
		{
			// The factory restores the last committed text when the setter rejects the input — so an
			// invalid date never lingers in the box as if it were saved.
			var ctx = new FakeRegionEditContext { OptionResult = false };
			var box = BuildDate(ctx, RegionDateKind.Date, "January 1, 2000");

			box.Text = "not a date";
			box.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(ctx.OptionEdits, Has.Count.EqualTo(1), "the rejected value was still offered to the setter");
			Assert.That(box.Text, Is.EqualTo("January 1, 2000"),
				"a rejected stage restores the committed value rather than leaving bad text shown as saved");
		}

		[AvaloniaTest]
		public void DateField_NoEditContext_IsReadOnly()
		{
			var control = RegionFieldControlFactory.Build(DateField(null, RegionDateKind.Date, "2000"),
				"When.Auto", new RegionFieldControlContext(editContext: null));
			Assert.That(((TextBox)control).IsReadOnly, Is.True);
		}

		// ---- Task B: the importer carries the enumComboBox stringList ----

		private static ViewDefinitionModel Import(string layoutXml, params (string id, string xml)[] parts)
		{
			var resolver = new InlinePartResolver(parts);
			return new XmlLayoutImporter().Import(XElement.Parse(layoutXml), resolver);
		}

		[AvaloniaTest]
		public void Importer_EnumComboBox_CarriesStringListIdsAndGroup()
		{
			var model = Import(
				"<layout class='WfiWordform' type='detail' name='T'><part ref='SpellingStatus'/></layout>",
				("SpellingStatus", @"<slice label='Spelling Status' field='SpellingStatus' editor='enumComboBox'>
					<deParams>
						<stringList group='Linguistics/WFI/SpellingStatus'
							ids='UndecidedSpellingStatus, CorrectSpellingStatus, IncorrectSpellingStatus'/>
					</deParams>
				</slice>"));

			var node = model.Roots.Single();
			Assert.That(node.EnumStringList, Is.Not.Null, "the importer no longer drops the stringList");
			Assert.That(node.EnumStringList.Ids, Is.EqualTo(new[]
			{
				"UndecidedSpellingStatus", "CorrectSpellingStatus", "IncorrectSpellingStatus"
			}), "ids are split and trimmed in document order (the stored enum int indexes this list)");
			Assert.That(node.EnumStringList.Group, Is.EqualTo("Linguistics/WFI/SpellingStatus"));
		}

		[AvaloniaTest]
		public void Importer_EnumComboBox_NoStringList_ReportsAndCarriesNothing()
		{
			var model = Import(
				"<layout class='X' type='detail' name='T'><part ref='S'/></layout>",
				("S", "<slice label='S' field='S' editor='enumComboBox'><deParams/></slice>"));

			var node = model.Roots.Single();
			Assert.That(node.EnumStringList, Is.Null);
			Assert.That(model.Diagnostics.Any(d => d.Code == "slice-content-dropped"
				&& d.Message.Contains("deParams")), Is.True, "a deParams without a stringList is reported, not silently dropped");
		}

		// ---- Task C: FwReferenceVectorField.Dispose detaches every handler ----

		private static LexicalEditRegionField VectorFieldWithItems() => new LexicalEditRegionField(
			"v1", "Publish In", "PublishIn", null, RegionFieldKind.ReferenceVector,
			EditorClassification.Known, "PublishIn", null, SurfaceRouting.Inherit, null,
			new List<RegionChoiceOption>
			{
				new RegionChoiceOption("p1", "Main Dictionary"),
				new RegionChoiceOption("p2", "Pocket")
			},
			null, isEditable: true,
			items: new List<RegionChoiceOption> { new RegionChoiceOption("p1", "Main Dictionary") });

		[AvaloniaTest]
		public void ReferenceVector_Dispose_DetachesEveryHandler()
		{
			var vector = new FwReferenceVectorField(VectorFieldWithItems(), "PublishIn",
				new FakeRegionEditContext());
			var window = new Window { Content = vector, Width = 480, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(vector.AttachedHandlerCount, Is.GreaterThan(0),
				"the editable vector wired per-item Remove handlers and the add picker subscriptions");
			vector.Dispose();
			Assert.That(vector.AttachedHandlerCount, Is.EqualTo(0),
				"Dispose detaches every wired handler/subscription");

			vector.Dispose(); // idempotent
			Assert.That(vector.AttachedHandlerCount, Is.EqualTo(0));
		}

		[AvaloniaTest]
		public void ReferenceVector_ReadOnly_HasNothingToDetach()
		{
			// A read-only vector (no edit context) wires no edit handlers, so its teardown is empty —
			// Dispose is a safe no-op.
			var vector = new FwReferenceVectorField(VectorFieldWithItems(), "PublishIn", editContext: null);
			Assert.That(vector.AttachedHandlerCount, Is.EqualTo(0));
			vector.Dispose();
			Assert.That(vector.AttachedHandlerCount, Is.EqualTo(0));
		}

		// A minimal IPartResolver that returns the inline part content by ref name.
		private sealed class InlinePartResolver : IPartResolver
		{
			private readonly Dictionary<string, XElement> _parts = new Dictionary<string, XElement>();

			public InlinePartResolver((string id, string xml)[] parts)
			{
				foreach (var (id, xml) in parts)
					_parts[id] = XElement.Parse(xml);
			}

			public XElement ResolvePart(string className, string layoutType, string refName)
				=> _parts.TryGetValue(refName, out var el) ? el : null;

			public XElement ResolvePartByRef(string refName)
				=> _parts.TryGetValue(refName, out var el) ? el : null;
		}
	}
}
