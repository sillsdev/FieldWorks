// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the PNG harness
using FwAvaloniaDialogsTests;        // DialogLayoutAssert — the shared geometry tripwire

namespace FwAvaloniaTests
{
	/// <summary>
	/// The Date / GenDate field editor (legacy DateSlice / GenDateSlice → <see cref="RegionFieldControlFactory"/>
	/// BuildDate): a flat, borderless single-line entry whose committed text is parsed+staged through the edit
	/// context's option seam. A parseable string stages and commits; an unparseable one is rejected and the box
	/// restores the last committed value, so bad input can never corrupt the stored date. These pin the visible
	/// states (empty, a set date, mid-edit invalid text) with a PNG per stage and the AssertNoCrowding tripwire;
	/// the parse/format/reject routing itself is pinned in RegionEditorParityTests.
	/// </summary>
	[TestFixture]
	public class DateFieldEditorTests
	{
		private static LexicalEditRegionField DateField(string display, bool editable = true)
			=> new LexicalEditRegionField(
				stableId: "d1", label: "Date Created", field: "DateCreated", writingSystem: null,
				kind: RegionFieldKind.Date, editorClassification: EditorClassification.Known,
				automationId: "DateCreated.Auto", localizationKey: null, routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("", display) },
				options: null, selectedOptionKey: null, isEditable: editable,
				dateKind: RegionDateKind.Date);

		private static (TextBox Box, Window Window) Show(FakeRegionEditContext ctx, string display)
		{
			var control = RegionFieldControlFactory.Build(DateField(display), "DateCreated.Auto",
				new RegionFieldControlContext(editContext: ctx));
			var box = (TextBox)control;
			var window = new Window { Content = box, Width = 320, Height = 80 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			return (box, window);
		}

		[AvaloniaTest]
		public void Empty_RendersAnEmptyEditableEntry()
		{
			var (box, window) = Show(new FakeRegionEditContext(), string.Empty);

			DialogSnapshot.Capture(window, "DateField-01-empty");
			DialogLayoutAssert.AssertNoCrowding(box);

			Assert.That(box.IsReadOnly, Is.False, "an edit context makes the date row editable");
			Assert.That(box.Text ?? string.Empty, Is.Empty, "the empty field renders an empty entry");
		}

		[AvaloniaTest]
		public void DateSet_ShowsTheFormattedValue()
		{
			var (box, window) = Show(new FakeRegionEditContext(), "January 1, 2000");

			DialogSnapshot.Capture(window, "DateField-02-date-set");
			DialogLayoutAssert.AssertNoCrowding(box);

			Assert.That(box.Text, Is.EqualTo("January 1, 2000"), "the row shows the formatted current value");
		}

		[AvaloniaTest]
		public void Invalid_TypedGarbage_IsRejectedOnCommit_AndRestoresTheCommittedValue()
		{
			// The setter rejects an unparseable date; the factory restores the last committed text so the
			// invalid string never lingers as if it were saved.
			var ctx = new FakeRegionEditContext { OptionResult = false };
			var (box, window) = Show(ctx, "January 1, 2000");

			// Capture the mid-edit INVALID stage (garbage shown, not yet committed) for visual review.
			box.Text = "not a date";
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, "DateField-03-invalid");
			DialogLayoutAssert.AssertNoCrowding(box);

			// Committing the garbage is rejected and the committed value is restored.
			box.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(ctx.OptionEdits, Has.Count.EqualTo(1), "the rejected value was still offered to the setter");
			Assert.That(box.Text, Is.EqualTo("January 1, 2000"),
				"a rejected stage restores the committed value rather than leaving bad text shown as saved");
		}
	}
}
