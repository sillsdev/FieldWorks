// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;

namespace FwAvaloniaTests.Detail
{
	/// <summary>
	/// The Reversal Entries control (<see cref="FwReversalEntriesField"/>) rendered
	/// headlessly: its groups and rows, when a row commits, and the row jump. A recording
	/// fake stands in for the edit context, whose LCModel side is tested on its own.
	/// </summary>
	[TestFixture]
	public class FwReversalEntriesFieldTests
	{
		private const string FieldId = "Reversal";

		// Records every row commit and jump, in order, so a test can check which came first.
		private sealed class RecordingReversalContext : IDetailEditContext, IReversalEntryEditing
		{
			public readonly List<string> Events = new List<string>();

			public bool TryCommitRow(string rowKey, string typedText)
			{
				Events.Add("commit " + rowKey + "=" + typedText);
				return true;
			}

			public Guid? TryResolveMainEntryGuid(string rowKey) => Guid.Empty;

			public bool IsOpen => false;

			public bool TrySetText(DetailField field, string ws, string value) => false;

			public bool TrySetRichText(DetailField field, string ws, DetailRichTextValue value) => false;

			public bool TrySetOption(DetailField field, string optionKey) => false;

			public bool TryAddReferenceItem(DetailField field, string optionKey) => false;

			public bool TryRemoveReferenceItem(DetailField field, string optionKey) => false;

			public bool TryMoveReferenceItem(DetailField field, string optionKey, bool forward) => false;

			public IReadOnlyList<string> Validate() => Array.Empty<string>();

			public void Commit()
			{
			}

			public void Cancel()
			{
			}
		}

		private static DetailReversalGroup English(params string[] forms)
		{
			var rows = forms.Select((f, i) => new DetailReversalRow("en" + i, f, false)).ToList();
			rows.Add(new DetailReversalRow("en-add", string.Empty, true));
			return new DetailReversalGroup("en", "Eng", null, false, rows);
		}

		private static (FwReversalEntriesField Field, TextBox Other, Window Window) Show(
			IDetailEditContext context, List<string> jumps, params DetailReversalGroup[] groups)
		{
			Action<string> navigate = null;
			if (jumps != null)
				navigate = key => jumps.Add(key);
			var field = new FwReversalEntriesField("Reversal Entries", FieldId, groups, context,
				navigationRequested: navigate);
			var other = new TextBox();
			var panel = new StackPanel();
			panel.Children.Add(field);
			panel.Children.Add(other);
			var window = new Window { Content = panel, Width = 420, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (field, other, window);
		}

		private static T Find<T>(Control root, string automationId) where T : Control
			=> root.GetVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == automationId);

		private static void TypeAndLeave(TextBox box, string text, TextBox other)
		{
			box.Focus();
			box.Text = text;
			other.Focus();
			Dispatcher.UIThread.RunJobs();
		}

		private static void CtrlClick(Window window, Control control)
		{
			var point = control.TranslatePoint(new Point(2, 2), window);
			Assert.That(point, Is.Not.Null, "the click target must be attached and laid out");
			window.MouseDown(point.Value, MouseButton.Left, RawInputModifiers.Control);
			window.MouseUp(point.Value, MouseButton.Left, RawInputModifiers.Control);
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void AGroup_ShowsItsEntries_ThenOneAddRow_UnderOneLabel()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("dwelling", "abode"));

			Assert.That(Find<TextBox>(field, "Reversal.en.0").Text, Is.EqualTo("dwelling"));
			Assert.That(Find<TextBox>(field, "Reversal.en.1").Text, Is.EqualTo("abode"));
			Assert.That(Find<TextBox>(field, "Reversal.en.Add").Text, Is.Empty);
			Assert.That(field.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text == "Eng"), Is.EqualTo(1),
				"the abbreviation labels the group once, not every entry");
		}

		[AvaloniaTest]
		public void AGroupsSlots_ShareOneLine_WithABarBetweenEachPair()
		{
			var (field, _, window) = Show(new RecordingReversalContext(), null, English("dwelling", "abode"));
			var group = Find<WrapPanel>(field, "Reversal.en");
			var first = Find<TextBox>(field, "Reversal.en.0");
			var second = Find<TextBox>(field, "Reversal.en.1");
			var add = Find<TextBox>(field, "Reversal.en.Add");

			Assert.That(group.Children.OfType<Border>().Count(), Is.EqualTo(2),
				"three slots, so two bars");
			Assert.That(second.TranslatePoint(new Point(0, 0), window).Value.Y,
				Is.EqualTo(first.TranslatePoint(new Point(0, 0), window).Value.Y), "short entries share a line");
			Assert.That(add.TranslatePoint(new Point(0, 0), window).Value.X,
				Is.GreaterThan(second.TranslatePoint(new Point(0, 0), window).Value.X), "the add slot comes last");
		}

		[AvaloniaTest]
		public void ALongEntry_StaysOnOneLine_InsideItsSlot()
		{
			var longForm = string.Join(" ", Enumerable.Repeat("dwelling", 30));
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("home", longForm));

			Assert.That(Find<TextBox>(field, "Reversal.en.1").Bounds.Height,
				Is.EqualTo(Find<TextBox>(field, "Reversal.en.0").Bounds.Height),
				"text wider than the line does not wrap inside its slot");
		}

		[AvaloniaTest]
		public void ALoneAddSlot_HasNoBar()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English());

			Assert.That(Find<WrapPanel>(field, "Reversal.en").Children.OfType<Border>(), Is.Empty);
		}

		[AvaloniaTest]
		public void ClickingAGroupsFreeSpace_StartsTypingInItsAddSlot()
		{
			var (field, _, window) = Show(new RecordingReversalContext(), null, English("dwelling"));
			var group = Find<WrapPanel>(field, "Reversal.en");
			var point = group.TranslatePoint(new Point(group.Bounds.Width - 2, 2), window);

			window.MouseDown(point.Value, MouseButton.Left);
			window.MouseUp(point.Value, MouseButton.Left);
			Dispatcher.UIThread.RunJobs();

			Assert.That(Find<TextBox>(field, "Reversal.en.Add").IsFocused, Is.True);
		}

		[AvaloniaTest]
		public void EachIndex_IsItsOwnGroup()
		{
			var french = new DetailReversalGroup("fr", "Fre", null, false,
				new[] { new DetailReversalRow("fr0", "maison", false), new DetailReversalRow("fr-add", "", true) });

			var (field, _, _) = Show(new RecordingReversalContext(), null, English("house"), french);

			Assert.That(Find<WrapPanel>(field, "Reversal.en"), Is.Not.Null);
			Assert.That(Find<WrapPanel>(field, "Reversal.fr"), Is.Not.Null);
			Assert.That(Find<TextBox>(field, "Reversal.fr.0").Text, Is.EqualTo("maison"));
		}

		[AvaloniaTest]
		public void OtherWritingSystemForms_ShowAsAReadOnlySuffix()
		{
			var rows = new[]
			{
				new DetailReversalRow("en0", "house", false,
					new[] { new DetailReversalAlternative("EnGB", "houze", null) }),
				new DetailReversalRow("en1", "home", false),
				new DetailReversalRow("en-add", "", true)
			};
			var (field, _, _) = Show(new RecordingReversalContext(), null,
				new DetailReversalGroup("en", "Eng", null, false, rows));

			var suffix = Find<TextBlock>(field, "Reversal.en.0.OtherWs");
			Assert.That(suffix, Is.Not.Null);
			Assert.That(string.Concat(suffix.Inlines.OfType<Avalonia.Controls.Documents.Run>().Select(r => r.Text)),
				Is.EqualTo("[EnGB houze]"));
			Assert.That(Find<TextBlock>(field, "Reversal.en.1.OtherWs"), Is.Null, "an entry with no alternatives has none");
			Assert.That(Find<TextBlock>(field, "Reversal.en.Add.OtherWs"), Is.Null, "an add row has none");
			Assert.That(field.GetVisualDescendants().OfType<TextBox>().Any(b => b.Text?.Contains("houze") == true),
				Is.False, "the alternatives are display text, never an editor");
		}

		[AvaloniaTest]
		public void AChangedRow_CommitsOnceWhenItLosesFocus()
		{
			var context = new RecordingReversalContext();
			var (field, other, _) = Show(context, null, English("dwelling"));
			var add = Find<TextBox>(field, "Reversal.en.Add");

			add.Focus();
			add.Text = "h";
			add.Text = "home";
			Assert.That(context.Events, Is.Empty, "typing alone commits nothing");
			other.Focus();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.Events, Is.EqualTo(new[] { "commit en-add=home" }));

			add.Focus();
			other.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.Events, Has.Count.EqualTo(1), "leaving again without a change commits nothing more");
		}

		[AvaloniaTest]
		public void AnUntouchedRow_NeverCommits()
		{
			var context = new RecordingReversalContext();
			var (field, other, _) = Show(context, null, English("dwelling"));

			TypeAndLeave(Find<TextBox>(field, "Reversal.en.0"), "dwelling", other);
			TypeAndLeave(Find<TextBox>(field, "Reversal.en.Add"), string.Empty, other);

			Assert.That(context.Events, Is.Empty);
		}

		[AvaloniaTest]
		public void ClearingARow_CommitsEmptyText()
		{
			var context = new RecordingReversalContext();
			var (field, other, _) = Show(context, null, English("dwelling"));

			TypeAndLeave(Find<TextBox>(field, "Reversal.en.0"), string.Empty, other);

			Assert.That(context.Events, Is.EqualTo(new[] { "commit en0=" }));
		}

		[AvaloniaTest]
		public void CtrlClick_CommitsPendingTextThenJumps()
		{
			var context = new RecordingReversalContext();
			var jumps = new List<string>();
			var (field, _, window) = Show(context, jumps, English("dwelling"));
			var add = Find<TextBox>(field, "Reversal.en.Add");

			add.Focus();
			add.Text = "home";
			CtrlClick(window, add);

			Assert.That(context.Events, Is.EqualTo(new[] { "commit en-add=home" }),
				"the typed entry exists before it is shown");
			Assert.That(jumps, Is.EqualTo(new[] { "en-add" }));
		}

		[AvaloniaTest]
		public void CtrlClick_OnAnEmptyAddRow_DoesNothing()
		{
			var jumps = new List<string>();
			var (field, _, window) = Show(new RecordingReversalContext(), jumps, English("dwelling"));

			CtrlClick(window, Find<TextBox>(field, "Reversal.en.Add"));

			Assert.That(jumps, Is.Empty);
		}

		[AvaloniaTest]
		public void TheRowMenu_OffersTheJump()
		{
			var jumps = new List<string>();
			var (field, _, _) = Show(new RecordingReversalContext(), jumps, English("dwelling"));
			var menu = Find<TextBox>(field, "Reversal.en.0").ContextFlyout as MenuFlyout;

			Assert.That(menu, Is.Not.Null);
			var item = menu.Items.OfType<MenuItem>().Single();
			Assert.That(item.Header, Is.EqualTo(FwAvaloniaStrings.ReversalShowInReversalIndex));
			item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

			Assert.That(jumps, Is.EqualTo(new[] { "en0" }));
		}

		[AvaloniaTest]
		public void WithoutTheReversalCapability_RowsAreReadOnly_AndOfferNoJump()
		{
			var jumps = new List<string>();
			var (field, _, _) = Show(new FakeDetailEditContext(), jumps, English("dwelling"));
			var box = Find<TextBox>(field, "Reversal.en.0");

			Assert.That(box.IsReadOnly, Is.True);
			var jumpItems = (box.ContextFlyout as MenuFlyout)?.Items.OfType<MenuItem>()
				.Where(i => Equals(i.Header, FwAvaloniaStrings.ReversalShowInReversalIndex));
			Assert.That(jumpItems ?? Enumerable.Empty<MenuItem>(), Is.Empty,
				"the text box keeps its own menu, but without the jump");
		}

		[AvaloniaTest]
		public void ARightToLeftGroup_FlowsRightToLeft()
		{
			var arabic = new DetailReversalGroup("ar", "Ara", null, true,
				new[] { new DetailReversalRow("ar0", "بيت", false), new DetailReversalRow("ar-add", "", true) });

			var (field, _, _) = Show(new RecordingReversalContext(), null, arabic);

			Assert.That(Find<TextBox>(field, "Reversal.ar.0").FlowDirection, Is.EqualTo(FlowDirection.RightToLeft));
			Assert.That(Find<TextBox>(field, "Reversal.ar.Add").FlowDirection, Is.EqualTo(FlowDirection.RightToLeft));
		}

		[AvaloniaTest]
		public void Dispose_DetachesEveryHandler()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), new List<string>(), English("dwelling"));
			var box = Find<TextBox>(field, "Reversal.en.0");
			var jumpMenu = box.ContextFlyout;
			Assert.That(field.AttachedHandlerCount, Is.GreaterThan(0));

			field.Dispose();

			Assert.That(field.AttachedHandlerCount, Is.Zero);
			Assert.That(box.ContextFlyout, Is.Not.SameAs(jumpMenu), "the row menu is released");
		}
	}
}
