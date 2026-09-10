// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The importer captures the legacy menu bindings (`menu=`, `contextMenu=`,
	/// `hotlinks=`) into the typed IR, from both the caller part ref and the slice/seq content,
	/// so the Avalonia detail view can show the SAME xCore-defined menus legacy DTMenuHandler shows.
	/// </summary>
	[TestFixture]
	public class XmlLayoutImporterMenuBindingTests
	{
		private const string PartsXml = @"
<PartInventory><bin>
  <part id='LexEntry-Detail-CitationForm'>
    <slice label='Citation Form' editor='multistring' field='CitationForm' ws='vernacular'
           menu='mnuDataTree-Help' contextMenu='mnuDataTree-CitationFormContext'/>
  </part>
  <part id='LexEntry-Detail-Summary'>
    <slice editor='summary' label='Section' menu='mnuDataTree-WordGloss'
           hotlinks='mnuDataTree-WordGloss-Hotlinks'/>
  </part>
  <part id='LexEntry-Detail-Senses'>
    <seq field='Senses' menu='mnuDataTree-Sense' hotlinks='mnuDataTree-Sense-Hotlinks'/>
  </part>
  <part id='LexEntry-Detail-Subentries'>
	<slice field='Subentries' label='Subentries' menu='mnuReorderVector' reorder='true'
		   editor='custom' assemblyPath='LexEdDll.dll' class='SIL.FieldWorks.XWorks.LexEd.EntrySequenceReferenceSlice'/>
  </part>
</bin></PartInventory>";

		private static ViewDefinitionModel Import(string layoutXml)
		{
			var parts = new DictionaryPartResolver(XElement.Parse(PartsXml));
			return new XmlLayoutImporter().Import(XElement.Parse(layoutXml), parts);
		}

		[Test]
		public void Import_SliceMenuAndContextMenu_LandOnTheNode_WithoutDropDiagnostics()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Menus'>
  <part ref='CitationForm'/>
</layout>");

			var cf = model.Roots[0];
			Assert.That(cf.MenuId, Is.EqualTo("mnuDataTree-Help"),
				"the legacy slice menu id is carried on the typed node");
			Assert.That(cf.ContextMenuId, Is.EqualTo("mnuDataTree-CitationFormContext"),
				"the in-string context menu id is carried on the typed node");
			Assert.That(model.Diagnostics, Is.Empty,
				"menu bindings are handled attributes, not dropped functional attributes");
		}

		[Test]
		public void Import_CallerMenu_OverridesContentMenu_LikeLegacyCallerNodePrecedence()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Menus'>
  <part ref='CitationForm' menu='mnuDataTree-Object'/>
</layout>");

			Assert.That(model.Roots[0].MenuId, Is.EqualTo("mnuDataTree-Object"),
				"legacy resolves the menu from the CALLER part ref first (DTMenuHandler.ShowSliceContextMenu)");
			Assert.That(model.Roots[0].ContextMenuId, Is.EqualTo("mnuDataTree-CitationFormContext"));
		}

		[Test]
		public void Import_SummaryHotlinks_AndSequenceMenus_AreCaptured()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Menus'>
  <part ref='Summary'/>
  <part ref='Senses'/>
</layout>");

			Assert.That(model.Roots[0].MenuId, Is.EqualTo("mnuDataTree-WordGloss"));
			Assert.That(model.Roots[0].HotlinksId, Is.EqualTo("mnuDataTree-WordGloss-Hotlinks"),
				"hotlinks ride summary headers like legacy WFIParts.xml");
			Assert.That(model.Roots[1].MenuId, Is.EqualTo("mnuDataTree-Sense"),
				"sequence nodes keep their menu so per-item headers can show the sense menu");
			Assert.That(model.Roots[1].HotlinksId, Is.EqualTo("mnuDataTree-Sense-Hotlinks"));
			Assert.That(model.Diagnostics, Is.Empty);
		}

		[Test]
		public void Import_ReorderFlag_LandsOnTheNode_AndIsNotReportedAsUnhandled()
		{
			var model = Import(@"
<layout class='LexEntry' type='detail' name='Menus'>
  <part ref='Subentries'/>
  <part ref='CitationForm'/>
</layout>");

			Assert.That(model.Roots[0].Reorder, Is.True, "reorder='true' rides the typed node");
			Assert.That(model.Roots[1].Reorder, Is.False, "absent means not reorderable");
			Assert.That(model.Diagnostics.Where(d => d.Message.Contains("reorder")), Is.Empty,
				"reorder is a handled slice attribute");
		}
	}

	/// <summary>
	/// How a detail row's menus are opened, and which menu each input maps to. Every
	/// row's LABEL cell answers right-click and the keyboard menu key with the slice menu; the
	/// "..." field-options button raises the row's own menu or hotlinks. A row's VALUE box
	/// answers the same inputs with the in-string `contextMenu=` menu; an unbound value
	/// box raises nothing and keeps its local Copy flyout.
	/// </summary>
	[TestFixture]
	public class DetailMenuRequestTests
	{
		private static DetailField Field(string id, DetailFieldKind kind,
			string menuId = null, string contextMenuId = null, string hotlinksId = null,
			bool collapsible = false)
			=> new DetailField(id, id, id, null, kind,
				EditorClassification.Known, id, null, HostRouting.Inherit,
				kind == DetailFieldKind.Text
					? new List<DetailWsValue> { new DetailWsValue("en", "value") }
					: null,
				null, null, isEditable: kind == DetailFieldKind.Text, indent: 0,
				isCollapsible: collapsible, isInitiallyExpanded: true,
				menuId: menuId, contextMenuId: contextMenuId, hotlinksId: hotlinksId,
				objectHvo: 1234);

		private static (Window window, DataTree view, List<DetailMenuRequest> requests) Show(
			params DetailField[] fields)
		{
			var requests = new List<DetailMenuRequest>();
			var model = new DetailModel("LexEntry", "Normal",
				fields.ToList(), new List<ViewDiagnostic>());
			var view = new DataTree(model, null, null, null, null, requests.Add);
			var window = new Window { Content = view, Width = 480, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (window, view, requests);
		}

		// Right-click through the real headless input pipeline: the right-button RELEASE
		// becomes ContextRequested. A null TranslatePoint fails loud so negative tests
		// cannot pass against a detached target.
		private static void RightClick(Window window, Control control)
		{
			Click(window, control, MouseButton.Right);
		}

		private static void LeftClick(Window window, Control control)
		{
			Click(window, control, MouseButton.Left);
		}

		private static void Click(Window window, Control control, MouseButton button)
		{
			var point = control.TranslatePoint(new Point(2, 2), window);
			Assert.That(point, Is.Not.Null, "the click target must be attached and laid out");
			window.MouseDown(point.Value, button);
			window.MouseUp(point.Value, button);
			Dispatcher.UIThread.RunJobs();
		}

		// Control.OnKeyUp raises ContextRequested for the platform's OpenContextMenu
		// hotkeys. Headless lists Apps and Win32 adds Shift+F10, so Apps covers both;
		// the RELEASE is the half that fires.
		private static void PressContextMenuKey(Window window)
		{
#pragma warning disable 618 // the Key-based overload avoids the headless physical-key map
			window.KeyPress(Key.Apps, RawInputModifiers.None);
			window.KeyRelease(Key.Apps, RawInputModifiers.None);
#pragma warning restore 618
			Dispatcher.UIThread.RunJobs();
		}

		private static T Find<T>(Visual view, string automationId) where T : Visual
			=> view.GetVisualDescendants().OfType<T>()
				.First(c => AutomationProperties.GetAutomationId(c) == automationId);

		private static T FindOrNull<T>(Visual view, string automationId) where T : Visual
			=> view.GetVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == automationId);

		// The field-options "..." affordance opens the menu on a click OR
		// keyboard activation; both arrive as Button.Click, so raising it exercises
		// the icon's own path, without depending on hover-reveal opacity.
		private static void ClickKebab(Button kebab)
		{
			kebab.RaiseEvent(new RoutedEventArgs { RoutedEvent = Button.ClickEvent });
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void FieldMenuButton_OnLabelRow_RaisesTheSliceMenuRequest_WithTheLegacyMenuId()
		{
			var (_, view, requests) = Show(Field("Gloss", DetailFieldKind.Text, menuId: "mnuDataTree-Help"));

			ClickKebab(Find<Button>(view, "Gloss.FieldMenu"));

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.SliceMenu));
			Assert.That(requests[0].Field.MenuId, Is.EqualTo("mnuDataTree-Help"));
			Assert.That(requests[0].Field.ObjectHvo, Is.EqualTo(1234),
				"the request carries the bound object so command routing can target it");
		}

		// Right-click and the drop-down icon open the same menu.
		[AvaloniaTest]
		public void RightClick_OnLabel_RaisesTheSliceMenuRequest()
		{
			var (window, view, requests) = Show(
				Field("Gloss", DetailFieldKind.Text, menuId: "mnuDataTree-Help"));

			RightClick(window, Find<TextBlock>(view, "Gloss.Label"));

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.SliceMenu));
			Assert.That(requests[0].Field.MenuId, Is.EqualTo("mnuDataTree-Help"));
		}

		// The empty gutter beside the label is part of the same cell, so it opens the same
		// menu rather than being a dead strip.
		[AvaloniaTest]
		public void RightClick_OnTheLabelGutter_RaisesTheSameSliceMenuRequest()
		{
			var (window, view, requests) = Show(
				Field("Gloss", DetailFieldKind.Text, menuId: "mnuDataTree-Help"));

			var labelCell = (Control)Find<TextBlock>(view, "Gloss.Label").GetVisualParent();
			RightClick(window, labelCell);

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.SliceMenu));
		}

		[AvaloniaTest]
		public void ContextMenuKey_OnTheFocusedLabelCell_RaisesTheSliceMenuRequest()
		{
			var (window, view, requests) = Show(
				Field("Gloss", DetailFieldKind.Text, menuId: "mnuDataTree-Help"));

			// Tab focus reaches the row through its field-options button; the request is
			// answered by the label cell that contains it.
			var kebab = Find<Button>(view, "Gloss.FieldMenu");
			kebab.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(kebab.IsFocused, Is.True, "precondition: the row's affordance has focus");

			PressContextMenuKey(window);

			Assert.That(requests, Has.Count.EqualTo(1),
				"the keyboard menu key opens the same menu right-click does");
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.SliceMenu));
		}

		// A header's right-click ignores hotlinks= and opens the slice menu (the shared
		// object group); the hotlinks stay on the kebab and the inline strip.
		[AvaloniaTest]
		public void RightClick_OnASectionHeader_RaisesTheSliceMenuRequest()
		{
			var (window, view, requests) = Show(
				Field("Senses", DetailFieldKind.Header, hotlinksId: "mnuDataTree-Sense-Hotlinks",
					collapsible: true));

			RightClick(window, Find<Button>(view, "Senses"));

			Assert.That(requests, Has.Count.EqualTo(1),
				"a header row answers right-click like any other row");
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.SliceMenu));
		}

		[AvaloniaTest]
		public void RightClick_OnValueBox_WithContextMenuBinding_RaisesTheContextMenuRequest()
		{
			var (window, view, requests) = Show(Field("CitationForm", DetailFieldKind.Text,
				menuId: "mnuDataTree-Help", contextMenuId: "mnuDataTree-CitationFormContext"));

			RightClick(window, view.GetVisualDescendants().OfType<TextBox>().First());

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.ContextMenu),
				"value-box right-click is the legacy MultiStringSlice in-string context menu");
			Assert.That(requests[0].Field.ContextMenuId, Is.EqualTo("mnuDataTree-CitationFormContext"));
		}

		// The whole value row takes the right-click, including the per-writing-system
		// abbreviation gutter, which shows the same menu the value text does.
		[AvaloniaTest]
		public void RightClick_OnTheWritingSystemAbbreviation_RaisesTheSameContextMenuRequest()
		{
			var (window, view, requests) = Show(Field("LexemeForm", DetailFieldKind.Text,
				menuId: "mnuDataTree-LexemeForm", contextMenuId: "mnuDataTree-LexemeFormContext"));

			var abbrev = view.GetVisualDescendants().OfType<TextBlock>()
				.First(t => t.Text == "en");
			RightClick(window, abbrev);

			Assert.That(requests, Has.Count.EqualTo(1),
				"the abbreviation gutter is part of the value row, not a dead strip");
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.ContextMenu));
			Assert.That(requests[0].Field.ContextMenuId, Is.EqualTo("mnuDataTree-LexemeFormContext"));
		}

		// One right-click must never raise two requests: the value box tunnels ahead of the
		// row handler.
		[AvaloniaTest]
		public void RightClick_InTheValue_RaisesExactlyOneRequest_NotAlsoTheRowHandler()
		{
			var (window, view, requests) = Show(Field("LexemeForm", DetailFieldKind.Text,
				menuId: "mnuDataTree-LexemeForm", contextMenuId: "mnuDataTree-LexemeFormContext"));

			RightClick(window, view.GetVisualDescendants().OfType<TextBox>().First());

			Assert.That(requests, Has.Count.EqualTo(1));
		}

		[AvaloniaTest]
		public void ContextMenuKey_InTheValueBox_RaisesTheContextMenuRequest()
		{
			var (window, view, requests) = Show(Field("CitationForm", DetailFieldKind.Text,
				menuId: "mnuDataTree-Help", contextMenuId: "mnuDataTree-CitationFormContext"));

			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			box.Focus();
			Dispatcher.UIThread.RunJobs();

			PressContextMenuKey(window);

			Assert.That(requests, Has.Count.EqualTo(1),
				"the keyboard menu key works inside the value");
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.ContextMenu));
		}

		// A keyboard-opened menu anchors to the field rather than to the last mouse
		// position; a right-click still opens at the pointer.
		[AvaloniaTest]
		public void RightClick_OpensAtThePointer_AnchoredAtTheControlItCameFrom()
		{
			var (window, view, requests) = Show(Field("CitationForm", DetailFieldKind.Text,
				contextMenuId: "mnuDataTree-CitationFormContext"));

			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			RightClick(window, box);

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].OpenAtPointer, Is.True, "right-click opens at the pointer");
			Assert.That(requests[0].AnchorControl, Is.SameAs(box));
		}

		[AvaloniaTest]
		public void ContextMenuKey_InTheValueBox_AnchorsToTheEditField_NotThePointer()
		{
			var (window, view, requests) = Show(Field("CitationForm", DetailFieldKind.Text,
				contextMenuId: "mnuDataTree-CitationFormContext"));

			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			box.Focus();
			Dispatcher.UIThread.RunJobs();

			PressContextMenuKey(window);

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].OpenAtPointer, Is.False,
				"a keyboard-opened menu carries no pointer position");
			Assert.That(requests[0].AnchorControl, Is.SameAs(box),
				"the menu anchors to the edit field the user is on");
		}

		[AvaloniaTest]
		public void ContextMenuKey_OnTheLabelCell_AnchorsToThatCell()
		{
			var (window, view, requests) = Show(
				Field("Gloss", DetailFieldKind.Text, menuId: "mnuDataTree-Help"));

			var kebab = Find<Button>(view, "Gloss.FieldMenu");
			kebab.Focus();
			Dispatcher.UIThread.RunJobs();

			PressContextMenuKey(window);

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].OpenAtPointer, Is.False);
			Assert.That(requests[0].AnchorControl, Is.Not.Null,
				"the label cell is the anchor for a keyboard-opened menu on the row");
		}

		// The field-options button opens on mouse click AND on Enter/Space, and Button.Click
		// carries no pointer either way, so it always drops from the icon, not the mouse.
		[AvaloniaTest]
		public void FieldMenuButton_AlwaysAnchorsToTheButton()
		{
			var (_, view, requests) = Show(Field("Gloss", DetailFieldKind.Text, menuId: "mnuDataTree-Help"));

			var kebab = Find<Button>(view, "Gloss.FieldMenu");
			ClickKebab(kebab);

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].OpenAtPointer, Is.False);
			Assert.That(requests[0].AnchorControl, Is.SameAs(kebab));
		}

		// The flyout-level half of the same contract: anchored placement drops the menu from the
		// target's bottom-left, pointer placement leaves the framework default alone.
		[AvaloniaTest]
		public void DetailMenuFlyout_AnchoredPlacement_DropsFromTheTargetsBottomLeft()
		{
			var items = new List<DetailMenuItem> { new DetailMenuItem("Show in Concordance") };
			var target = new Button { Content = "field" };
			var window = new Window { Content = target, Width = 300, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var anchored = DetailMenuFlyout.Show(items, target, atPointer: false);
			Dispatcher.UIThread.RunJobs();

			Assert.That(anchored, Is.Not.Null);
			Assert.That(anchored.Placement, Is.EqualTo(PlacementMode.BottomEdgeAlignedLeft),
				"a keyboard-invoked menu drops from the field, not from the pointer");
		}

		[AvaloniaTest]
		public void DetailMenuFlyout_PointerPlacement_LeavesTheDefault()
		{
			var items = new List<DetailMenuItem> { new DetailMenuItem("Show in Concordance") };
			var target = new Button { Content = "field" };
			var window = new Window { Content = target, Width = 300, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var atPointer = DetailMenuFlyout.Show(items, target, atPointer: true);
			Dispatcher.UIThread.RunJobs();

			Assert.That(atPointer, Is.Not.Null);
			Assert.That(atPointer.Placement, Is.Not.EqualTo(PlacementMode.BottomEdgeAlignedLeft),
				"a mouse-invoked menu still opens at the pointer");
		}

		// The two menus are DISTINCT: the label opens the slice menu, the value opens the
		// in-string menu. Neither input may produce the other's menu.
		[AvaloniaTest]
		public void LabelAndValue_RaiseTheirOwnDistinctMenus()
		{
			var (window, view, requests) = Show(Field("LexemeForm", DetailFieldKind.Text,
				menuId: "mnuDataTree-LexemeForm", contextMenuId: "mnuDataTree-LexemeFormContext"));

			RightClick(window, Find<TextBlock>(view, "LexemeForm.Label"));
			RightClick(window, view.GetVisualDescendants().OfType<TextBox>().First());

			Assert.That(requests.Select(r => r.Kind), Is.EqualTo(new[]
			{
				DetailMenuKind.SliceMenu,
				DetailMenuKind.ContextMenu
			}));
		}

		[AvaloniaTest]
		public void FieldMenuButton_OnHotlinksOnlyHeader_RaisesTheHotlinksRequest()
		{
			var (_, view, requests) = Show(Field("Senses", DetailFieldKind.Header,
				hotlinksId: "mnuDataTree-Sense-Hotlinks", collapsible: true));

			// The header's "..." button raises the hotlinks request; the collapsible toggle (id
			// "Senses")
			// is a SEPARATE button that still toggles the section.
			ClickKebab(Find<Button>(view, "Senses.FieldMenu"));

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.Hotlinks));
			Assert.That(requests[0].Field.HotlinksId, Is.EqualTo("mnuDataTree-Sense-Hotlinks"));
		}

		// Discoverability parity (legacy SummaryCommandControl): a hotlink-bearing section header
		// shows
		// an ALWAYS-VISIBLE inline command-link strip beneath it, not just the hover-gated "..."
		// kebab.
		[AvaloniaTest]
		public void HotlinksStrip_AppearsForHotlinkHeader_IsAlwaysVisible_AndKeepsTheKebab()
		{
			var (_, view, _) = Show(Field("Senses", DetailFieldKind.Header,
				hotlinksId: "mnuDataTree-Sense-Hotlinks", collapsible: true));

			var strip = FindOrNull<Button>(view, "Senses.Hotlinks");
			Assert.That(strip, Is.Not.Null, "a header with a HotlinksId renders the inline command strip");
			// Always visible -- NOT hover-gated like the kebab (Opacity 0 / not hit-testable at
			// rest).
			Assert.That(strip.IsVisible, Is.True, "the strip stays in the tree");
			Assert.That(strip.Opacity, Is.EqualTo(1d), "the strip is fully visible at rest, not hover-gated");
			Assert.That(strip.IsHitTestVisible, Is.True, "the strip is clickable at rest");
			Assert.That(AutomationProperties.GetName(strip), Is.EqualTo(FwAvaloniaStrings.FieldOptionsMenu),
				"a screen reader announces the always-visible commands affordance");

			// The hover kebab is kept alongside the always-visible strip.
			var kebab = FindOrNull<Button>(view, "Senses.FieldMenu");
			Assert.That(kebab, Is.Not.Null, "the kebab is retained next to the inline strip");
			Assert.That(kebab.Opacity, Is.EqualTo(0d), "the kebab stays hover-gated (the strip is the always-visible twin)");
		}

		[AvaloniaTest]
		public void HotlinksStrip_Activating_RaisesTheSameHotlinksRequest_AsTheKebab()
		{
			var (_, view, requests) = Show(Field("Senses", DetailFieldKind.Header,
				hotlinksId: "mnuDataTree-Sense-Hotlinks", collapsible: true));

			// Activating the strip arrives as Button.Click (mouse OR keyboard), the same path the kebab uses.
			ClickKebab(Find<Button>(view, "Senses.Hotlinks"));

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.Hotlinks),
				"the strip dispatches the SAME hotlinks request the kebab raises");
			Assert.That(requests[0].Field.HotlinksId, Is.EqualTo("mnuDataTree-Sense-Hotlinks"));
		}

		[AvaloniaTest]
		public void HotlinksStrip_IsAbsent_ForHeaderWithoutHotlinks()
		{
			var (_, view, _) = Show(Field("Notes", DetailFieldKind.Header, menuId: "mnuDataTree-Object"));

			Assert.That(FindOrNull<Button>(view, "Notes.Hotlinks"), Is.Null,
				"a header carrying only a slice menu (no hotlinks) gets no inline command strip");
		}

		// Exactly one menu: a bridged value box must NOT keep the TextBox theme flyout
		// (Cut/Copy/Paste) that otherwise opens alongside the bridged menu; unbound boxes keep
		// the local Copy flyout.
		[AvaloniaTest]
		public void BridgedValueBox_DropsTheThemeFlyout_UnboundKeepsCopy()
		{
			var (_, boundView, _) = Show(Field("CitationForm", DetailFieldKind.Text,
				contextMenuId: "mnuDataTree-CitationFormContext"));
			var boundBox = boundView.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(boundBox.ContextFlyout, Is.Null,
				"a bridged box must not raise a second (built-in) menu");

			var (_, plainView, _) = Show(Field("Comment", DetailFieldKind.Text));
			var plainBox = plainView.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(plainBox.ContextFlyout, Is.Not.Null, "unbound rows keep the local Copy flyout");
		}

		[AvaloniaTest]
		public void RightClick_OnUnboundRow_LabelRaisesTheSliceMenu_ValueRaisesNothing()
		{
			var (window, view, requests) = Show(Field("Comment", DetailFieldKind.Text));

			// A label right-click opens the shared object menu (Field Visibility / Move
			// Field / Help) even when the row binds no menu=.
			RightClick(window, Find<TextBlock>(view, "Comment.Label"));
			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.SliceMenu));

			// The value box of a row without a contextMenu= binding keeps its local
			// Copy flyout only.
			RightClick(window, view.GetVisualDescendants().OfType<TextBox>().First());
			Assert.That(requests, Has.Count.EqualTo(1),
				"an unbound value box raises no host menu request");
			Assert.That(FindOrNull<Button>(view, "Comment.FieldMenu"), Is.Null,
				"a row with no menu/hotlinks binding gets no field-options button");
		}

		[AvaloniaTest]
		public void FieldMenuButton_IsHiddenAtRest_RevealedOnHover_AndKeyboardAddressable()
		{
			var (_, view, _) = Show(Field("Gloss", DetailFieldKind.Text, menuId: "mnuDataTree-Help"));

			var kebab = Find<Button>(view, "Gloss.FieldMenu");
			// Accessibility: a localized name so a screen reader announces the affordance.
			Assert.That(AutomationProperties.GetName(kebab), Is.EqualTo(FwAvaloniaStrings.FieldOptionsMenu));
			// Stays in layout/focusable at rest (so Tab reaches it) but hidden by opacity until revealed.
			Assert.That(kebab.IsVisible, Is.True, "the button stays in the tree (focusable) at rest");
			Assert.That(kebab.Opacity, Is.EqualTo(0d), "hidden by opacity until the row is hovered/focused");
			Assert.That(kebab.IsHitTestVisible, Is.False, "and not clickable until revealed");
			Assert.That(kebab.Focusable, Is.True, "Tab can reach the field-options button");

			// Focusing it (the keyboard path) reveals it synchronously (hit-testable); the opacity then
			// fades in over the transition -- the same mechanism HoverRevealTests covers in
			// detail.
			kebab.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(kebab.IsFocused, Is.True, "the opacity-hidden button is keyboard-focusable");
			Assert.That(kebab.IsHitTestVisible, Is.True, "keyboard focus reveals the field-options button");
		}

		// The host-resolved xCore items render as a native Avalonia flyout: items in order,
		// separators, submenus, disabled state, checkmarks, and click dispatching the execute action.
		[AvaloniaTest]
		public void DetailMenuFlyout_BuildsItems_AndClickExecutes()
		{
			var executed = 0;
			var items = new List<DetailMenuItem>
			{
				new DetailMenuItem("Insert Sense", execute: () => executed++),
				DetailMenuItem.Separator(),
				new DetailMenuItem("Delete this Sense", isEnabled: false),
				new DetailMenuItem("Field Visibility", children: new List<DetailMenuItem>
				{
					new DetailMenuItem("Always visible", isChecked: true)
				})
			};

			var flyout = DetailMenuFlyout.Build(items);
			Assert.That(flyout.Items.Count, Is.EqualTo(4));

			var insert = (MenuItem)flyout.Items[0];
			Assert.That(insert.Header, Is.EqualTo("Insert Sense"));
			Assert.That(insert.IsEnabled, Is.True);

			Assert.That(flyout.Items[1], Is.InstanceOf<Separator>());

			var delete = (MenuItem)flyout.Items[2];
			Assert.That(delete.IsEnabled, Is.False, "display-state Enabled=false renders disabled");

			var submenu = (MenuItem)flyout.Items[3];
			Assert.That(submenu.Items.Count, Is.EqualTo(1), "submenus nest");
			Assert.That(((MenuItem)submenu.Items[0]).Icon, Is.Not.Null, "checked items show a checkmark");

			insert.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(executed, Is.EqualTo(1), "picking an item dispatches the host execute action");
		}

		// Context-menu density: every item pins the explicit compact
		// padding/height of legacy WinForms menus (FwAvaloniaDensity tokens),
		// never the taller Fluent defaults -- nested submenu items included.
		[AvaloniaTest]
		public void DetailMenuFlyout_ItemDensity_IsPinnedCompact_IncludingSubmenus()
		{
			var flyout = DetailMenuFlyout.Build(new List<DetailMenuItem>
			{
				new DetailMenuItem("Insert Sense", execute: () => { }),
				new DetailMenuItem("Field Visibility", children: new List<DetailMenuItem>
				{
					new DetailMenuItem("Always visible")
				})
			});

			var top = flyout.Items.OfType<MenuItem>().ToList();
			Assert.That(top, Has.Count.EqualTo(2));
			foreach (var item in top)
			{
				Assert.That(item.Padding, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.MenuItemPadding),
					$"'{item.Header}' pads at legacy WinForms menu density, not the Fluent default");
				Assert.That(item.MinHeight, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.MenuItemMinHeight),
					$"'{item.Header}' row height mirrors the legacy ~22px menu items");
			}

			var child = (MenuItem)top[1].Items[0];
			Assert.That(child.Padding, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.MenuItemPadding),
				"submenu items compact too");
		}

		// A read-only reference-vector row (no edit context): items still select, since the
		// item-relative commands act on whichever item is current.
		private static DetailField VectorField(string id, params string[] itemKeys)
			=> new DetailField(id, id, id, null, DetailFieldKind.ReferenceVector,
				EditorClassification.Known, id, null, HostRouting.Inherit, null, null, null,
				isEditable: false, indent: 0, menuId: "mnuReorderVector", objectHvo: 1234,
				items: itemKeys.Select(k => new DetailChoiceOption(k, k.ToUpperInvariant())).ToList());

		[AvaloniaTest]
		public void VectorRow_WithNoItemClicked_RequestCarriesNoSelectedItem()
		{
			var (window, view, requests) = Show(VectorField("Subentries", "a", "b"));
			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			Assert.That(vector.SelectedItemKey, Is.Null);
			Assert.That(vector.SelectedItemIndex, Is.EqualTo(-1));

			RightClick(window, Find<TextBlock>(view, "Subentries.Label"));

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].SelectedItemKey, Is.Null);
			Assert.That(requests[0].SelectedItemIndex, Is.EqualTo(-1));
		}

		[AvaloniaTest]
		public void ClickingAVectorItem_SelectsIt_AndTheLabelMenuRequestCarriesIt()
		{
			var (window, view, requests) = Show(VectorField("Subentries", "a", "b"));
			var second = Find<TextBlock>(view, "Subentries.Item.b");

			LeftClick(window, second);

			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			Assert.That(vector.SelectedItemKey, Is.EqualTo("b"));
			Assert.That(vector.SelectedItemIndex, Is.EqualTo(1));
			Assert.That(second.Background, Is.SameAs(FwAvaloniaDensity.SelectedRowBrush),
				"the current item is highlighted");
			Assert.That(requests, Is.Empty, "selecting an item opens no menu");

			RightClick(window, Find<TextBlock>(view, "Subentries.Label"));

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.SliceMenu));
			Assert.That(requests[0].SelectedItemKey, Is.EqualTo("b"));
			Assert.That(requests[0].SelectedItemIndex, Is.EqualTo(1));
		}

		[AvaloniaTest]
		public void SelectingInOneVectorRow_ClearsTheOtherRowsSelection()
		{
			var (window, view, _) = Show(VectorField("Subentries", "a", "b"),
				VectorField("ComplexForms", "c", "d"));
			var subentries = Find<FwReferenceVectorField>(view, "Subentries");
			var complexForms = Find<FwReferenceVectorField>(view, "ComplexForms");

			LeftClick(window, Find<TextBlock>(view, "Subentries.Item.b"));
			Assert.That(subentries.SelectedItemKey, Is.EqualTo("b"));

			LeftClick(window, Find<TextBlock>(view, "ComplexForms.Item.c"));

			Assert.That(complexForms.SelectedItemKey, Is.EqualTo("c"));
			Assert.That(subentries.SelectedItemKey, Is.Null, "one current item per view");
			Assert.That(Find<TextBlock>(view, "Subentries.Item.b").Background,
				Is.SameAs(FwAvaloniaDensity.TransparentBrush), "the old highlight is gone");
			Assert.That(subentries.SelectItem("a"), Is.True);
			Assert.That(complexForms.SelectedItemKey, Is.Null, "a programmatic select clears the others too");
		}

		[AvaloniaTest]
		public void ClickingAnotherVectorItem_MovesTheSelection_AndTheHighlight()
		{
			var (window, view, _) = Show(VectorField("Subentries", "a", "b"));
			var first = Find<TextBlock>(view, "Subentries.Item.a");
			var second = Find<TextBlock>(view, "Subentries.Item.b");

			LeftClick(window, second);
			LeftClick(window, first);

			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			Assert.That(vector.SelectedItemKey, Is.EqualTo("a"));
			Assert.That(first.Background, Is.SameAs(FwAvaloniaDensity.SelectedRowBrush));
			Assert.That(second.Background, Is.SameAs(FwAvaloniaDensity.TransparentBrush),
				"only one item is current at a time");
		}

		[AvaloniaTest]
		public void RightClickingAVectorItem_SelectsIt_SoItsMenuActsOnTheItemUnderThePointer()
		{
			var (window, view, requests) = Show(VectorField("Subentries", "a", "b"));

			RightClick(window, Find<TextBlock>(view, "Subentries.Item.a"));

			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			Assert.That(vector.SelectedItemKey, Is.EqualTo("a"));
			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.ItemMenu), "the item's own menu, not the row's");
			Assert.That(requests[0].SelectedItemKey, Is.EqualTo("a"));

			ClickKebab(Find<Button>(view, "Subentries.FieldMenu"));

			Assert.That(requests, Has.Count.EqualTo(2));
			Assert.That(requests[1].Kind, Is.EqualTo(DetailMenuKind.SliceMenu));
			Assert.That(requests[1].SelectedItemKey, Is.EqualTo("a"),
				"the field-options button carries the current item too");
		}

		[AvaloniaTest]
		public void RightClickingAVectorItem_RaisesTheItemMenuRequest_ForThatItem()
		{
			var (window, view, requests) = Show(VectorField("Subentries", "a", "b"));

			var second = Find<TextBlock>(view, "Subentries.Item.b");
			RightClick(window, second);

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.ItemMenu));
			Assert.That(requests[0].SelectedItemKey, Is.EqualTo("b"),
				"the item under the pointer is the request's item");
			Assert.That(requests[0].SelectedItemIndex, Is.EqualTo(1));
			Assert.That(requests[0].Field.ObjectHvo, Is.EqualTo(1234), "the row's object still rides along");
			Assert.That(requests[0].OpenAtPointer, Is.True);
			Assert.That(ReferenceEquals(requests[0].AnchorControl, second), Is.True);
			Assert.That(second.ContextFlyout, Is.Null, "one menu: the bridged item menu replaces the local flyout");
		}

		[AvaloniaTest]
		public void CtrlClickingAVectorItem_RaisesTheDefaultActivation_ForThatItem()
		{
			var (window, view, requests) = Show(VectorField("Subentries", "a", "b"));
			var second = Find<TextBlock>(view, "Subentries.Item.b");

			var point = second.TranslatePoint(new Point(2, 2), window);
			Assert.That(point, Is.Not.Null);
			window.MouseDown(point.Value, MouseButton.Left, RawInputModifiers.Control);
			window.MouseUp(point.Value, MouseButton.Left, RawInputModifiers.Control);
			Dispatcher.UIThread.RunJobs();

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].IsDefaultActivation, Is.True, "Ctrl+click runs the default jump");
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.ItemMenu));
			Assert.That(requests[0].SelectedItemKey, Is.EqualTo("b"));
			Assert.That(requests[0].OpenAtPointer, Is.False);

			LeftClick(window, second);
			Assert.That(requests, Has.Count.EqualTo(1), "a plain click only selects");
		}

		[AvaloniaTest]
		public void FocusingAVectorItem_SelectsIt_AndTheContextMenuKey_RaisesTheItemMenuUnderIt()
		{
			var (window, view, requests) = Show(VectorField("Subentries", "a", "b"));
			var second = Find<TextBlock>(view, "Subentries.Item.b");

			second.Focus();
			Dispatcher.UIThread.RunJobs();
			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			Assert.That(vector.SelectedItemKey, Is.EqualTo("b"), "keyboard focus selects the item");

			PressContextMenuKey(window);

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(DetailMenuKind.ItemMenu));
			Assert.That(requests[0].IsDefaultActivation, Is.False);
			Assert.That(requests[0].SelectedItemKey, Is.EqualTo("b"));
			Assert.That(requests[0].OpenAtPointer, Is.False, "a keyboard-raised menu anchors under the item");
			Assert.That(ReferenceEquals(requests[0].AnchorControl, second), Is.True);
		}

		// Items take focus from a click (so the keys and the context-menu key reach them) but
		// add no Tab stops to the detail view.
		[AvaloniaTest]
		public void VectorItems_AreFocusable_ButNotTabStops()
		{
			var (_, view, _) = Show(VectorField("Subentries", "a", "b"));
			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			var first = Find<TextBlock>(view, "Subentries.Item.a");
			Assert.That(first.Focusable, Is.True);
			Assert.That(KeyboardNavigation.GetIsTabStop(first), Is.False, "items are not tab stops");
			Assert.That(vector.Focusable, Is.False, "nor is the row");
		}

		// Backspace or Delete on a focused item removes it, staging through the edit context
		// and completing the gesture; read-only rows ignore the keys.
		[AvaloniaTest]
		public void DeleteOrBackspace_OnAFocusedVectorItem_RemovesIt_ThroughTheEditContext()
		{
			var field = new DetailField("v", "Publish In", "PublishIn", null, DetailFieldKind.ReferenceVector,
				EditorClassification.Known, "PublishIn", null, HostRouting.Inherit, null,
				new List<DetailChoiceOption> { new DetailChoiceOption("p1", "Main Dictionary") }, null,
				isEditable: true, items: new List<DetailChoiceOption>
				{
					new DetailChoiceOption("p1", "Main Dictionary"),
					new DetailChoiceOption("p2", "Pocket")
				});
			var context = new FakeDetailEditContext();
			var gestures = 0;
			var vector = new FwReferenceVectorField(field, "PublishIn", context, () => gestures++,
				menuRequested: r => { });
			var window = new Window { Content = vector, Width = 480, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Find<TextBlock>(vector, "PublishIn.Item.p2").Focus();
			Dispatcher.UIThread.RunJobs();
#pragma warning disable 618 // the Key-based overload avoids the headless physical-key map
			window.KeyPress(Key.Delete, RawInputModifiers.None);
			window.KeyRelease(Key.Delete, RawInputModifiers.None);
#pragma warning restore 618
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ReferenceRemoves, Is.EqualTo(new[] { ("PublishIn", "p2") }));
			Assert.That(gestures, Is.EqualTo(1), "a successful remove completes the gesture once");

			Find<TextBlock>(vector, "PublishIn.Item.p1").Focus();
			Dispatcher.UIThread.RunJobs();
#pragma warning disable 618
			window.KeyPress(Key.Back, RawInputModifiers.None);
			window.KeyRelease(Key.Back, RawInputModifiers.None);
#pragma warning restore 618
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ReferenceRemoves.Count, Is.EqualTo(2));
			Assert.That(context.ReferenceRemoves[1], Is.EqualTo(("PublishIn", "p1")), "Backspace removes too");

			// A read-only row (no edit context) ignores the keys.
			var readOnly = new FwReferenceVectorField(field, "ReadOnly", null);
			window.Content = readOnly;
			Dispatcher.UIThread.RunJobs();
			Find<TextBlock>(readOnly, "ReadOnly.Item.p1").Focus();
			Dispatcher.UIThread.RunJobs();
#pragma warning disable 618
			window.KeyPress(Key.Delete, RawInputModifiers.None);
			window.KeyRelease(Key.Delete, RawInputModifiers.None);
#pragma warning restore 618
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.ReferenceRemoves.Count, Is.EqualTo(2), "nothing wired on a read-only row");
		}

		[AvaloniaTest]
		public void EditableVectorItem_WithAMenuBridge_HasNoLocalRemoveFlyout_AndWithoutOneKeepsIt()
		{
			var field = new DetailField("v", "Publish In", "PublishIn", null, DetailFieldKind.ReferenceVector,
				EditorClassification.Known, "PublishIn", null, HostRouting.Inherit, null,
				new List<DetailChoiceOption> { new DetailChoiceOption("p1", "Main Dictionary") }, null,
				isEditable: true, items: new List<DetailChoiceOption> { new DetailChoiceOption("p1", "Main Dictionary") });

			var bridged = new FwReferenceVectorField(field, "Bridged", new FakeDetailEditContext(),
				menuRequested: r => { });
			Assert.That(Find<TextBlock>(bridged, "Bridged.Item.p1").ContextFlyout, Is.Null);

			var local = new FwReferenceVectorField(field, "Local", new FakeDetailEditContext());
			Assert.That(Find<TextBlock>(local, "Local.Item.p1").ContextFlyout, Is.Not.Null,
				"without a host bridge the row keeps its local Remove flyout");
		}

		[AvaloniaTest]
		public void LeftClick_OnBoundLabel_RaisesNoRequest()
		{
			var (window, view, requests) = Show(
				Field("Gloss", DetailFieldKind.Text, menuId: "mnuDataTree-Help"));

			LeftClick(window, Find<TextBlock>(view, "Gloss.Label"));

			Assert.That(requests, Is.Empty, "only the right button opens the slice menu, like legacy");
		}
	}
}
