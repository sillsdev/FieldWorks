// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 21: the shared <see cref="RegionFieldKind"/>→control dispatch both the detail-pane region view
	/// and the browse in-cell editor route through. These pin that one switch produces the right control
	/// per kind, and that the all-nullable <see cref="RegionFieldControlContext"/> serves both surfaces —
	/// the browse cell passes null menu/link callbacks and suppresses the WS-abbreviation gutter while the
	/// detail pane passes the full set — without either surface hand-rolling its own dispatch.
	/// </summary>
	[TestFixture]
	public class RegionFieldControlFactoryTests
	{
		private static LexicalEditRegionField Field(RegionFieldKind kind, string selectedOption = null,
			System.Func<Control> controlFactory = null)
			=> new LexicalEditRegionField(
				stableId: "f1", label: "Label", field: "Field", writingSystem: "en", kind: kind,
				editorClassification: EditorClassification.Known, automationId: "Auto.Id",
				localizationKey: null, routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("en", "v", wsTag: "en") },
				options: null, selectedOptionKey: selectedOption, isEditable: true, controlFactory: controlFactory);

		[AvaloniaTest]
		public void TextKind_BuildsMultiWsTextField()
			=> Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.Text), "Auto.Id", null),
				Is.InstanceOf<FwMultiWsTextField>());

		[AvaloniaTest]
		public void ChooserKind_BuildsChooserField()
			=> Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.Chooser), "Auto.Id", null),
				Is.InstanceOf<FwChooserField>());

		[AvaloniaTest]
		public void ReferenceVectorKind_BuildsReferenceVectorField()
			=> Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.ReferenceVector), "Auto.Id", null),
				Is.InstanceOf<FwReferenceVectorField>());

		[AvaloniaTest]
		public void BooleanKind_BuildsCheckBox_CheckedReflectsSelectedOption()
		{
			var control = RegionFieldControlFactory.Build(Field(RegionFieldKind.Boolean, selectedOption: "true"),
				"Auto.Id", null);
			Assert.That(control, Is.InstanceOf<CheckBox>());
			Assert.That(((CheckBox)control).IsChecked, Is.True);
		}

		[AvaloniaTest]
		public void CommandKind_BuildsDisabledButton()
		{
			var control = RegionFieldControlFactory.Build(Field(RegionFieldKind.Command), "Auto.Id", null);
			Assert.That(control, Is.InstanceOf<Button>());
			Assert.That(((Button)control).IsEnabled, Is.False);
		}

		[AvaloniaTest]
		public void DateKind_BuildsTextBox_ReadOnlyWithoutEditContext()
		{
			var control = RegionFieldControlFactory.Build(Field(RegionFieldKind.Date), "Auto.Id", null);
			Assert.That(control, Is.InstanceOf<TextBox>());
			// No edit context => read-only display (no setter wired).
			Assert.That(((TextBox)control).IsReadOnly, Is.True);
		}

		[AvaloniaTest]
		public void DateKind_WithEditContext_IsEditableAndShowsValue()
		{
			var ctx = new RegionFieldControlContext(editContext: new FakeRegionEditContext());
			var control = RegionFieldControlFactory.Build(Field(RegionFieldKind.Date), "Auto.Id", ctx);
			// §19e: an exact date with an edit context is a text box + calendar picker row.
			var box = control as TextBox ?? control.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(box.IsReadOnly, Is.False, "an edit context makes the date row editable");
			Assert.That(box.Text, Is.EqualTo("v"), "the row shows the formatted current value");
			Assert.That(control.GetVisualDescendants().OfType<CalendarDatePicker>().Any(), Is.True,
				"the exact-date row carries a calendar day picker");
		}

		[AvaloniaTest]
		public void UnsupportedKind_BuildsUnsupportedTextBlock()
			=> Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.Unsupported), "Auto.Id", null),
				Is.InstanceOf<TextBlock>());

		// §19e — Enum closed-combo: a dedicated kind that renders a CLOSED combo (never a free-form
		// text box), so an invalid enum value can never be typed in.
		[AvaloniaTest]
		public void EnumComboKind_BuildsClosedComboBox()
		{
			var field = new LexicalEditRegionField(
				stableId: "e1", label: "Status", field: "Status", writingSystem: null,
				kind: RegionFieldKind.EnumCombo, editorClassification: EditorClassification.Known,
				automationId: "Auto.Enum", localizationKey: null, routing: SurfaceRouting.Product,
				values: null,
				options: new List<RegionChoiceOption>
				{
					new RegionChoiceOption("0", "Stem"), new RegionChoiceOption("1", "Affix")
				},
				selectedOptionKey: "1", isEditable: true);
			var ctx = new RegionFieldControlContext(editContext: new FakeRegionEditContext());
			var control = RegionFieldControlFactory.Build(field, "Auto.Enum", ctx);
			Assert.That(control, Is.InstanceOf<ComboBox>(), "the closed enum combo is a ComboBox, not a TextBox");
			var combo = (ComboBox)control;
			Assert.That(combo.IsEditable, Is.False, "a closed combo rejects free text by construction");
			Assert.That(((RegionChoiceOption)combo.SelectedItem).Key, Is.EqualTo("1"),
				"the combo preselects the stored option");
		}

		[AvaloniaTest]
		public void EnumComboKind_SelectingAnOption_StagesItsKey()
		{
			var ctx = new FakeRegionEditContext();
			var field = new LexicalEditRegionField(
				stableId: "e1", label: "Status", field: "Status", writingSystem: null,
				kind: RegionFieldKind.EnumCombo, editorClassification: EditorClassification.Known,
				automationId: "Auto.Enum", localizationKey: null, routing: SurfaceRouting.Product,
				values: null,
				options: new List<RegionChoiceOption>
				{
					new RegionChoiceOption("0", "Stem"), new RegionChoiceOption("1", "Affix")
				},
				selectedOptionKey: "0", isEditable: true);
			var combo = (ComboBox)RegionFieldControlFactory.Build(field, "Auto.Enum",
				new RegionFieldControlContext(editContext: ctx));

			combo.SelectedIndex = 1;

			Assert.That(ctx.OptionEdits, Has.Count.EqualTo(1));
			Assert.That(ctx.OptionEdits[0].Key, Is.EqualTo("1"), "selecting an option stages its index key");
		}

		[AvaloniaTest]
		public void EnumComboKind_WithoutEditContext_IsDisabled()
		{
			var field = new LexicalEditRegionField(
				stableId: "e1", label: "Status", field: "Status", writingSystem: null,
				kind: RegionFieldKind.EnumCombo, editorClassification: EditorClassification.Known,
				automationId: "Auto.Enum", localizationKey: null, routing: SurfaceRouting.Product,
				values: null,
				options: new List<RegionChoiceOption> { new RegionChoiceOption("0", "Stem") },
				selectedOptionKey: "0", isEditable: true);
			var combo = (ComboBox)RegionFieldControlFactory.Build(field, "Auto.Enum", null);
			Assert.That(combo.IsEnabled, Is.False, "no edit context => read-only display");
		}

		// §19e — Integer: a dedicated numeric editor that rejects non-numeric keystrokes (the legacy
		// IntegerSlice TextBox + Convert.ToInt32 guard), so free text never reaches the int property.
		[AvaloniaTest]
		public void IntegerKind_BuildsTextBox_ShowingTheValue()
		{
			var ctx = new RegionFieldControlContext(editContext: new FakeRegionEditContext());
			var control = RegionFieldControlFactory.Build(IntegerField("42"), "Auto.Int", ctx);
			Assert.That(control, Is.InstanceOf<TextBox>());
			Assert.That(((TextBox)control).Text, Is.EqualTo("42"));
			Assert.That(((TextBox)control).IsReadOnly, Is.False);
		}

		[AvaloniaTest]
		public void IntegerKind_CommittingAValidInt_Stages_RejectsNonNumeric()
		{
			var ctx = new FakeRegionEditContext();
			var box = (TextBox)RegionFieldControlFactory.Build(IntegerField("1"),
				"Auto.Int", new RegionFieldControlContext(editContext: ctx, save: () => { }));

			// A valid integer commits on Enter.
			box.Text = "7";
			box.RaiseEvent(new Avalonia.Input.KeyEventArgs
			{
				RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Enter
			});
			Assert.That(ctx.TextEdits, Has.Count.EqualTo(1));
			Assert.That(ctx.TextEdits[0].Value, Is.EqualTo("7"));

			// Non-numeric text is rejected by the setter; the box restores the last committed value.
			ctx.TextResult = false; // simulate the int-parse setter rejecting "abc"
			box.Text = "abc";
			box.RaiseEvent(new Avalonia.Input.KeyEventArgs
			{
				RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Enter
			});
			Assert.That(box.Text, Is.EqualTo("7"), "a rejected commit restores the last committed value");
		}

		// §19e — Literal: a static text renderer (legacy MessageSlice) — the label/message text is the
		// content, no editable value column.
		[AvaloniaTest]
		public void LiteralKind_BuildsStaticTextBlock_ShowingTheLabel()
		{
			var field = new LexicalEditRegionField(
				stableId: "l1", label: "Read this carefully:", field: "Self", writingSystem: null,
				kind: RegionFieldKind.Literal, editorClassification: EditorClassification.Known,
				automationId: "Auto.Lit", localizationKey: null, routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("", "Read this carefully:") },
				options: null, selectedOptionKey: null, isEditable: false);
			var control = RegionFieldControlFactory.Build(field, "Auto.Lit", null);
			Assert.That(control, Is.InstanceOf<TextBlock>());
			Assert.That(((TextBlock)control).Text, Is.EqualTo("Read this carefully:"));
		}

		private static LexicalEditRegionField IntegerField(string value)
			=> new LexicalEditRegionField(
				stableId: "i1", label: "Order", field: "Order", writingSystem: null,
				kind: RegionFieldKind.Integer, editorClassification: EditorClassification.Known,
				automationId: "Auto.Int", localizationKey: null, routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("", value) },
				options: null, selectedOptionKey: null, isEditable: true);

		// §19e — GenDate qualifiers: the generic-date row builds a structured editor (year + precision +
		// era + circa) that composes a GenDate.TryParse-compatible string, NOT a bare text box. The
		// composition grammar is pinned by FwGenDateField.Compose; here we pin the dispatch + commit.
		private static LexicalEditRegionField GenDateField(string display)
			=> new LexicalEditRegionField(
				stableId: "g1", label: "Date Of Event", field: "DateOfEvent", writingSystem: null,
				kind: RegionFieldKind.Date, editorClassification: EditorClassification.Known,
				automationId: "Auto.Gen", localizationKey: null, routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("", display) },
				options: null, selectedOptionKey: null, isEditable: true,
				dateKind: RegionDateKind.GenDate);

		[AvaloniaTest]
		public void GenDateKind_BuildsStructuredEditor_WithYearPrecisionEra()
		{
			var ctx = new RegionFieldControlContext(editContext: new FakeRegionEditContext());
			var control = RegionFieldControlFactory.Build(GenDateField("About AD 1985"), "Auto.Gen", ctx);
			Assert.That(control, Is.InstanceOf<FwGenDateField>(),
				"a GenDate row gets the structured qualifier editor, not a plain text box");
			var gen = (FwGenDateField)control;
			Assert.That(gen.Year, Is.EqualTo(1985));
			Assert.That(gen.Precision, Is.EqualTo(GenDatePrecision.Approximate), "'About' seeds Approximate");
			Assert.That(gen.IsAd, Is.True);
		}

		[AvaloniaTest]
		public void GenDateField_Compose_MatchesTheParseableLongStringGrammar()
		{
			// These are the exact ToLongString forms GenDate.TryParse round-trips (probed from LCModel):
			// AD: "<prec?>AD <year>"; BC: "<prec?><year> BC".
			Assert.That(FwGenDateField.Compose(1990, GenDatePrecision.Exact, true), Is.EqualTo("AD 1990"));
			Assert.That(FwGenDateField.Compose(1985, GenDatePrecision.Approximate, true), Is.EqualTo("About AD 1985"));
			Assert.That(FwGenDateField.Compose(1200, GenDatePrecision.Before, true), Is.EqualTo("Before AD 1200"));
			Assert.That(FwGenDateField.Compose(500, GenDatePrecision.After, false), Is.EqualTo("After 500 BC"));
			Assert.That(FwGenDateField.Compose(300, GenDatePrecision.Approximate, false), Is.EqualTo("About 300 BC"));
		}

		[AvaloniaTest]
		public void GenDateField_ChangingAQualifier_StagesTheComposedString()
		{
			var ctx = new FakeRegionEditContext();
			var gen = (FwGenDateField)RegionFieldControlFactory.Build(GenDateField("AD 1990"),
				"Auto.Gen", new RegionFieldControlContext(editContext: ctx, save: () => { }));

			gen.SetForTest(1990, GenDatePrecision.Approximate, true);

			Assert.That(ctx.OptionEdits, Has.Count.EqualTo(1));
			Assert.That(ctx.OptionEdits[0].Key, Is.EqualTo("About AD 1990"),
				"changing the precision stages the recomposed GenDate string the setter parses");
		}

		// §19e — exact-date calendar picker: the exact-date row offers a calendar day picker (legacy
		// DateSlice MonthCalendar) alongside its text, NOT just free text.
		[AvaloniaTest]
		public void ExactDateKind_BuildsCalendarPicker()
		{
			var field = new LexicalEditRegionField(
				stableId: "d1", label: "Date Created", field: "DateCreated", writingSystem: null,
				kind: RegionFieldKind.Date, editorClassification: EditorClassification.Known,
				automationId: "Auto.Date", localizationKey: null, routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("", string.Empty) },
				options: null, selectedOptionKey: null, isEditable: true, dateKind: RegionDateKind.Date);
			var ctx = new RegionFieldControlContext(editContext: new FakeRegionEditContext());
			var control = RegionFieldControlFactory.Build(field, "Auto.Date", ctx);
			Assert.That(control.GetVisualDescendants().OfType<CalendarDatePicker>().Any()
				|| control is CalendarDatePicker, Is.True,
				"the exact-date row offers a calendar day picker");
		}

		[AvaloniaTest]
		public void CustomKind_NullFactory_DegradesToUnsupportedRow()
			=> Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.Custom, controlFactory: null),
				"Auto.Id", null), Is.InstanceOf<TextBlock>());

		[AvaloniaTest]
		public void CustomKind_FactoryControl_IsReturned()
		{
			var marker = new Border();
			var control = RegionFieldControlFactory.Build(
				Field(RegionFieldKind.Custom, controlFactory: () => marker), "Auto.Id", null);
			Assert.That(control, Is.SameAs(marker));
		}

		[AvaloniaTest]
		public void BrowseStyleContext_TextField_SuppressesWritingSystemAbbreviation()
		{
			// The dense browse cell context (null callbacks, no abbreviation gutter) must still build a
			// usable text field — the same control the detail pane gets, just configured for the cell.
			var browseContext = new RegionFieldControlContext(
				editContext: null, writingSystemFocused: _ => { }, showWritingSystemAbbreviation: false);
			Assert.That(RegionFieldControlFactory.Build(Field(RegionFieldKind.Text), "Auto.Id", browseContext),
				Is.InstanceOf<FwMultiWsTextField>());
		}
	}
}
