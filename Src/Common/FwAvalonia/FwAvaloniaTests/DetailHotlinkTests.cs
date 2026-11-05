// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogsTests;
using FwAvaloniaTests.VisualChecks;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Which section header's hotlinks apply to a field, decided from the field list alone
	/// (LT-22693).
	/// </summary>
	[TestFixture]
	public class DetailHotlinksHeaderTests
	{
		private static DetailField Row(string id, int indent, bool header = false, string hotlinks = null)
			=> new DetailField(id, id, id, null, header ? DetailFieldKind.Header : DetailFieldKind.Text,
				EditorClassification.Known, id, null, HostRouting.Inherit, null, null, null,
				isEditable: false, indent: indent, isCollapsible: header, hotlinksId: hotlinks);

		private static readonly List<DetailField> Fields = new List<DetailField>
		{
			Row("CitationForm", 0),
			Row("Sense1", 0, header: true, hotlinks: "mnuDataTree-Sense-Hotlinks"),
			Row("Gloss", 1),
			Row("Examples", 1, header: true),
			Row("Example", 2),
			Row("Subsense", 1, header: true, hotlinks: "mnuDataTree-Sense-Hotlinks"),
			Row("SubGloss", 2),
			Row("Etymology", 0)
		};

		[TestCase(0, -1, TestName = "A field outside every hotlink section has none")]
		[TestCase(1, 1, TestName = "A hotlink header is its own section")]
		[TestCase(2, 1, TestName = "A field takes its section header's hotlinks")]
		[TestCase(3, 1, TestName = "A header without hotlinks takes its section's")]
		[TestCase(4, 1, TestName = "A header without hotlinks between is passed over")]
		[TestCase(5, 5, TestName = "A nested hotlink header is its own section")]
		[TestCase(6, 5, TestName = "The nearest hotlink section wins")]
		[TestCase(7, -1, TestName = "A field after a section is not in it")]
		public void GetHotlinksHeaderIndex_FindsTheSectionsHeader(int index, int expected)
		{
			Assert.That(DetailVisibility.GetHotlinksHeaderIndex(Fields, index), Is.EqualTo(expected));
		}

		[Test]
		public void GetHotlinksHeaderIndex_StopsAtALessIndentedRow()
		{
			var fields = new List<DetailField>
			{
				Row("Section", 1, header: true, hotlinks: "hot"),
				Row("Outside", 0),
				Row("Deeper", 2)
			};

			Assert.That(DetailVisibility.GetHotlinksHeaderIndex(fields, 2), Is.EqualTo(-1),
				"a row at indent 0 between them ends the section");
		}

		[Test]
		public void GetHotlinksHeaderIndex_RejectsAnIndexOutsideTheList()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => DetailVisibility.GetHotlinksHeaderIndex(Fields, Fields.Count));
		}
	}

	/// <summary>
	/// A section header's hotlinks: one link per command, at the right of the header's own line,
	/// shown only while keyboard focus is in that section (LT-22693).
	/// </summary>
	[TestFixture]
	public class DetailHotlinkStripTests
	{
		private const string SenseHotlinks = "mnuDataTree-Sense-Hotlinks";

		private static DetailField Header(string id, int indent = 0, string hotlinks = SenseHotlinks,
			bool expanded = true)
			=> new DetailField(id, id, id, null, DetailFieldKind.Header,
				EditorClassification.Known, id, null, HostRouting.Inherit, null, null, null,
				isEditable: false, indent: indent, isCollapsible: true, isInitiallyExpanded: expanded,
				hotlinksId: hotlinks, objectHvo: 1234);

		private static DetailField Text(string id, int indent)
		{
			var field = new DetailField(id, id, id, null, DetailFieldKind.Text,
				EditorClassification.Known, id, null, HostRouting.Inherit,
				new List<DetailWsValue> { new DetailWsValue("en", "value") }, null, null,
				isEditable: true, indent: indent, objectHvo: 1234);
			field.IsMultiStringRow = true;
			return field;
		}

		private sealed class Host
		{
			public Window Window;
			public DataTree View;
			// The header each request for hotlinks named, in order.
			public readonly List<string> AskedFor = new List<string>();
		}

		private static Host Show(Func<DetailField, IReadOnlyList<DetailMenuItem>> items, double width,
			params DetailField[] fields)
		{
			var host = new Host();
			var model = new DetailModel("LexEntry", "Normal", fields.ToList(), new List<ViewDiagnostic>());
			host.View = new DataTree(model, menuRequested: request => { },
				hotlinkItems: field =>
				{
					host.AskedFor.Add(field.StableId);
					return items(field);
				});
			host.Window = new Window { Content = host.View, Width = width, Height = 300 };
			host.Window.Show();
			Dispatcher.UIThread.RunJobs();
			return host;
		}

		private static IReadOnlyList<DetailMenuItem> SenseCommands(DetailField field)
			=> new[] { new DetailMenuItem("Insert Example", execute: () => { }), new DetailMenuItem("Insert Sense", execute: () => { }) };

		private static Button Link(Visual view, string rowId, int index)
			=> view.GetVisualDescendants().OfType<Button>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == rowId + ".Hotlinks." + index);

		private static IReadOnlyList<Button> Links(Visual view, string rowId)
			=> view.GetVisualDescendants().OfType<Button>()
				.Where(b => (AutomationProperties.GetAutomationId(b) ?? string.Empty).StartsWith(rowId + ".Hotlinks.", StringComparison.Ordinal))
				.ToList();

		private static void FocusEditorOf(Visual view, string rowId)
		{
			var editor = view.GetVisualDescendants().OfType<TextBox>()
				.First(t => (AutomationProperties.GetAutomationId(t) ?? string.Empty).StartsWith(rowId, StringComparison.Ordinal));
			editor.Focus();
			Dispatcher.UIThread.RunJobs();
		}

		private static void LeftClick(Window window, Control control)
		{
			var point = control.TranslatePoint(new Point(2, 2), window);
			Assert.That(point, Is.Not.Null, "the click target must be attached and laid out");
			window.MouseDown(point.Value, MouseButton.Left);
			window.MouseUp(point.Value, MouseButton.Left);
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void Hotlinks_AreShown_OnlyWhileFocusIsInTheirSection()
		{
			var host = Show(SenseCommands, 480,
				Header("Sense1"), Text("Gloss", 1), Text("Etymology", 0));

			Assert.That(Links(host.View, "Sense1"), Is.Empty, "no section is being worked in yet");
			Assert.That(host.AskedFor, Is.Empty, "nothing asks for commands before they are shown");

			FocusEditorOf(host.View, "Gloss");
			Assert.That(Links(host.View, "Sense1").Select(l => AutomationProperties.GetName(l)),
				Is.EqualTo(new[] { "Insert Example", "Insert Sense" }));

			FocusEditorOf(host.View, "Etymology");
			Assert.That(Links(host.View, "Sense1"), Is.Empty, "focus left the section");
		}

		[AvaloniaTest]
		public void Hotlinks_FollowFocus_FromOneSectionToTheNext()
		{
			var host = Show(SenseCommands, 480,
				Header("Sense1"), Text("Gloss1", 1), Header("Sense2"), Text("Gloss2", 1));

			FocusEditorOf(host.View, "Gloss1");
			FocusEditorOf(host.View, "Gloss2");

			Assert.That(Links(host.View, "Sense1"), Is.Empty);
			Assert.That(Links(host.View, "Sense2"), Has.Count.EqualTo(2));
			Assert.That(host.AskedFor, Is.EqualTo(new[] { "Sense1", "Sense2" }),
				"each section's commands are asked for as focus enters it");
		}

		[AvaloniaTest]
		public void Hotlinks_StayPut_WhileFocusMovesWithinTheSection()
		{
			var host = Show(SenseCommands, 480,
				Header("Sense1"), Text("Gloss", 1), Text("Definition", 1));

			FocusEditorOf(host.View, "Gloss");
			var link = Link(host.View, "Sense1", 0);
			FocusEditorOf(host.View, "Definition");

			Assert.That(Link(host.View, "Sense1", 0), Is.SameAs(link), "the same links, not rebuilt");
			Assert.That(host.AskedFor, Has.Count.EqualTo(1));
		}

		[AvaloniaTest]
		public void Hotlinks_ShowOneLinkPerCommand_WithEachCommandsEnabledState()
		{
			var host = Show(field => new[]
				{
					new DetailMenuItem("Insert Example", execute: () => { }),
					DetailMenuItem.Separator(),
					new DetailMenuItem("More", children: new[] { new DetailMenuItem("Hidden") }),
					DetailMenuItem.Disabled("Insert Sense")
				}, 480,
				Header("Sense1"), Text("Gloss", 1));

			FocusEditorOf(host.View, "Gloss");

			var links = Links(host.View, "Sense1");
			Assert.That(links.Select(l => AutomationProperties.GetName(l)), Is.EqualTo(new[] { "Insert Example", "Insert Sense" }),
				"one link per command; separators and submenus are not links");
			Assert.That(links[0].IsEnabled, Is.True);
			Assert.That(links[1].IsEnabled, Is.False, "a command that cannot run shows a disabled link");
		}

		private static void PressKey(Window window, PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
		{
			window.KeyPressQwerty(key, modifiers);
			Dispatcher.UIThread.RunJobs();
		}

		private static string FocusedId(Window window)
			=> AutomationProperties.GetAutomationId(window.FocusManager?.GetFocusedElement() as Control);

		[AvaloniaTest]
		public void Hotlinks_AreReachedWithShiftTab_FromTheSectionsFirstField_AndRunWithEnter()
		{
			var ran = new List<string>();
			var host = Show(field => new[]
				{
					new DetailMenuItem("Insert Example", execute: () => ran.Add("Example")),
					new DetailMenuItem("Insert Sense", execute: () => ran.Add("Sense"))
				}, 480,
				Header("Sense1"), Text("Gloss", 1), Text("Definition", 1));
			FocusEditorOf(host.View, "Gloss");

			PressKey(host.Window, PhysicalKey.Tab, RawInputModifiers.Shift);
			Assert.That(FocusedId(host.Window), Is.EqualTo("Sense1.Hotlinks.1"), "the last link comes first going back");
			PressKey(host.Window, PhysicalKey.Tab, RawInputModifiers.Shift);
			Assert.That(FocusedId(host.Window), Is.EqualTo("Sense1.Hotlinks.0"));
			PressKey(host.Window, PhysicalKey.Enter);
			Assert.That(ran, Is.EqualTo(new[] { "Example" }));

			PressKey(host.Window, PhysicalKey.Tab);
			PressKey(host.Window, PhysicalKey.Tab);
			Assert.That(FocusedId(host.Window), Does.StartWith("Gloss"), "Tab carries on from the links into the section");
		}

		[AvaloniaTest]
		public void Tab_IntoASection_GoesToItsFirstField_NotToLinksNotYetShown()
		{
			var host = Show(SenseCommands, 480,
				Text("Citation", 0), Header("Sense1"), Text("Gloss", 1));
			FocusEditorOf(host.View, "Citation");

			PressKey(host.Window, PhysicalKey.Tab);

			Assert.That(FocusedId(host.Window), Does.StartWith("Gloss"));
			Assert.That(Links(host.View, "Sense1"), Has.Count.EqualTo(2), "arriving shows the section's links");
		}

		[AvaloniaTest]
		public void AnEmptySection_ShowsItsHotlinks_WithoutFocus_AndTabReachesThem()
		{
			var host = Show(field => new[] { new DetailMenuItem("Insert Variant", execute: () => { }) }, 480,
				Text("Citation", 0), Header("Variants"), Text("Etymology", 0));

			Assert.That(Links(host.View, "Variants"), Has.Count.EqualTo(1),
				"no field in the section can take focus to bring its links");
			var link = Link(host.View, "Variants", 0);
			FocusEditorOf(host.View, "Etymology");
			Assert.That(Link(host.View, "Variants", 0), Is.SameAs(link), "focus elsewhere keeps them");

			FocusEditorOf(host.View, "Citation");
			PressKey(host.Window, PhysicalKey.Tab);

			Assert.That(FocusedId(host.Window), Is.EqualTo("Variants.Hotlinks.0"));
			Assert.That(Link(host.View, "Variants", 0), Is.SameAs(link), "the focused link is not rebuilt");
			Assert.That(host.AskedFor, Is.EqualTo(new[] { "Variants" }));
		}

		[AvaloniaTest]
		public void ACollapsedSection_ShowsItsHotlinks_UntilItIsExpanded()
		{
			var host = Show(SenseCommands, 480,
				Header("Sense1", expanded: false), Text("Gloss", 1), Text("Etymology", 0));
			Assert.That(Links(host.View, "Sense1"), Has.Count.EqualTo(2), "its fields are hidden");

			var header = host.View.GetVisualDescendants().OfType<Control>()
				.First(c => AutomationProperties.GetAutomationId(c) == "Sense1");
			LeftClick(host.Window, header);
			FocusEditorOf(host.View, "Etymology");
			Assert.That(Links(host.View, "Sense1"), Is.Empty, "expanded, its fields can take focus");

			LeftClick(host.Window, header);
			Assert.That(Links(host.View, "Sense1"), Has.Count.EqualTo(2), "collapsed again");
		}

		private static IBrush DrawnBackground(Button link)
			=> link.GetVisualDescendants().OfType<ContentPresenter>()
				.First(p => p.Name == "PART_ContentPresenter").Background;

		[AvaloniaTest]
		public void Hotlinks_AreTinted_AndDeepenTheTintUnderThePointer_ButNotWhenDisabled()
		{
			var host = Show(field => new[]
				{
					new DetailMenuItem("Insert Example", execute: () => { }),
					DetailMenuItem.Disabled("Insert Sense")
				}, 480,
				Header("Sense1"), Text("Gloss", 1));
			FocusEditorOf(host.View, "Gloss");
			var available = Link(host.View, "Sense1", 0);
			var unavailable = Link(host.View, "Sense1", 1);

			Assert.That(DrawnBackground(available), Is.SameAs(FwAvaloniaDensity.HotlinkButtonBackground));
			Assert.That(DrawnBackground(unavailable), Is.Not.SameAs(FwAvaloniaDensity.HotlinkButtonBackground),
				"an unavailable command does not wear the tint of one that can run");

			var point = available.TranslatePoint(new Point(4, 4), host.Window).Value;
			host.Window.MouseMove(point);
			Dispatcher.UIThread.RunJobs();
			Assert.That(DrawnBackground(available), Is.SameAs(FwAvaloniaDensity.HotlinkButtonPointeroverBackground));
			DialogSnapshot.Capture(host.Window, "LT-22693-02-hover-and-unavailable-hotlinks");
		}

		[AvaloniaTest]
		public void ClickingAHotlink_RunsItsCommand()
		{
			var ran = new List<string>();
			var host = Show(field => new[]
				{
					new DetailMenuItem("Insert Example", execute: () => ran.Add("Example")),
					new DetailMenuItem("Insert Sense", execute: () => ran.Add("Sense"))
				}, 480,
				Header("Sense1"), Text("Gloss", 1));
			FocusEditorOf(host.View, "Gloss");

			LeftClick(host.Window, Link(host.View, "Sense1", 1));

			Assert.That(ran, Is.EqualTo(new[] { "Sense" }));
		}

		[AvaloniaTest]
		public void Hotlinks_SitAtTheRightOfTheHeadersOwnLine()
		{
			var host = Show(SenseCommands, 480, Header("Sense1"), Text("Gloss", 1));
			FocusEditorOf(host.View, "Gloss");

			var header = host.View.GetVisualDescendants().OfType<Control>()
				.First(c => AutomationProperties.GetAutomationId(c) == "Sense1");
			var headerTop = header.TranslatePoint(new Point(0, 0), host.View).Value.Y;
			var lastLink = Link(host.View, "Sense1", 1);
			var linkTopLeft = lastLink.TranslatePoint(new Point(0, 0), host.View).Value;
			var linkRight = linkTopLeft.X + lastLink.Bounds.Width;

			Assert.That(linkTopLeft.Y, Is.LessThan(headerTop + header.Bounds.Height),
				"the links share the header's line instead of taking one below it");
			Assert.That(linkRight, Is.GreaterThan(host.View.Bounds.Width - 40),
				"the links are right-aligned");
		}

		[AvaloniaTest]
		public void Hotlinks_ThatDoNotFit_AreNotDrawn_AndTheHeaderKeepsItsText()
		{
			var longCommands = new Func<DetailField, IReadOnlyList<DetailMenuItem>>(field => new[]
			{
				new DetailMenuItem("Insert", execute: () => { }),
				new DetailMenuItem("Insert another sense below this one, with all of its own fields", execute: () => { })
			});
			var host = Show(longCommands, 420, Header("Sense one with a long heading"), Text("Gloss", 1));
			FocusEditorOf(host.View, "Gloss");

			var links = Links(host.View, "Sense one with a long heading");
			Assert.That(links, Has.Count.EqualTo(2), "every command keeps its link");
			Assert.That(links[0].Bounds.Width, Is.GreaterThan(0), "the first link fits");
			Assert.That(links[1].Bounds.Width, Is.EqualTo(0), "the link that does not fit is not drawn");
			Assert.That(KeyboardNavigation.GetIsTabStop(links[0]), Is.True);
			Assert.That(KeyboardNavigation.GetIsTabStop(links[1]), Is.False, "an unseen link takes no Tab focus");
			var headerText = host.View.GetVisualDescendants().OfType<TextBlock>()
				.First(t => t.Text == "Sense one with a long heading");
			Assert.That(headerText.Bounds.Width, Is.GreaterThanOrEqualTo(headerText.DesiredSize.Width - 1),
				"the header text is not squeezed to make room for links");
		}

		[AvaloniaTest]
		public void AHeaderWhoseMenuOffersNoCommand_ShowsNoLinks()
		{
			var host = Show(field => Array.Empty<DetailMenuItem>(), 480,
				Header("Publication", hotlinks: "mnuDataTree-Help"), Text("PublishIn", 1));

			FocusEditorOf(host.View, "PublishIn");

			Assert.That(Links(host.View, "Publication"), Is.Empty);
		}

		[AvaloniaTest]
		public void WithoutAHotlinkSource_NoLinksAreShown()
		{
			var model = new DetailModel("LexEntry", "Normal",
				new List<DetailField> { Header("Sense1"), Text("Gloss", 1) }, new List<ViewDiagnostic>());
			var view = new DataTree(model, menuRequested: request => { });
			var window = new Window { Content = view, Width = 480, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			FocusEditorOf(view, "Gloss");

			Assert.That(Links(view, "Sense1"), Is.Empty);
		}

		[AvaloniaTest]
		public void Hotlinks_RenderCleanly_OnTheActiveSensesLine()
		{
			var host = Show(SenseCommands, 480,
				Header("Sense 1"), Text("Gloss 1", 1), Header("Sense 2"), Text("Gloss 2", 1), Text("Etymology", 0));
			FocusEditorOf(host.View, "Gloss 2");

			DialogSnapshot.Capture(host.Window, "LT-22693-01-active-sense-hotlinks");
			DialogLayoutAssert.AssertNoCrowding(host.View);
		}
	}
}
