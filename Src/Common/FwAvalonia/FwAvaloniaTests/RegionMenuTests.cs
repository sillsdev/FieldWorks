// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Section 13.1 — the importer captures the legacy menu bindings (`menu=`, `contextMenu=`,
	/// `hotlinks=`) into the typed IR, from both the caller part ref and the slice/seq content,
	/// so the Avalonia surface can show the SAME xCore-defined menus legacy DTMenuHandler shows.
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
	}

	/// <summary>
	/// Section 13.3 — right-click on the Avalonia surface raises a <see cref="RegionMenuRequest"/>
	/// through the host bridge with the legacy menu binding and screen coordinates: labels/headers
	/// raise the slice menu (or hotlinks when only those exist), value boxes with a `contextMenu=`
	/// binding raise the in-string context menu, and unbound rows raise nothing (they keep the
	/// local Copy flyout).
	/// </summary>
	[TestFixture]
	public class RegionMenuRequestTests
	{
		private static LexicalEditRegionField Field(string id, RegionFieldKind kind,
			string menuId = null, string contextMenuId = null, string hotlinksId = null,
			bool collapsible = false)
			=> new LexicalEditRegionField(id, id, id, null, kind,
				EditorClassification.Known, id, null, SurfaceRouting.Inherit,
				kind == RegionFieldKind.Text
					? new List<RegionWsValue> { new RegionWsValue("en", "value") }
					: null,
				null, null, isEditable: kind == RegionFieldKind.Text, indent: 0,
				isCollapsible: collapsible, isInitiallyExpanded: true,
				menuId: menuId, contextMenuId: contextMenuId, hotlinksId: hotlinksId,
				objectHvo: 1234);

		private static (LexicalEditRegionView view, List<RegionMenuRequest> requests) Show(
			params LexicalEditRegionField[] fields)
		{
			var requests = new List<RegionMenuRequest>();
			var model = new LexicalEditRegionModel("LexEntry", "Normal",
				fields.ToList(), new List<ViewDiagnostic>());
			var view = new LexicalEditRegionView(model, null, null, null, null, requests.Add);
			var window = new Window { Content = view, Width = 480, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (view, requests);
		}

		private static void RightClick(Control control)
		{
			var root = (Visual)control.GetVisualRoot();
			var position = control.TranslatePoint(new Point(2, 2), root) ?? new Point(0, 0);
			control.RaiseEvent(new PointerPressedEventArgs(control,
				new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true),
				root, position, 0,
				new PointerPointProperties(RawInputModifiers.RightMouseButton,
					PointerUpdateKind.RightButtonPressed),
				KeyModifiers.None));
			Dispatcher.UIThread.RunJobs();
		}

		private static T Find<T>(Visual view, string automationId) where T : Visual
			=> view.GetVisualDescendants().OfType<T>()
				.First(c => AutomationProperties.GetAutomationId(c) == automationId);

		private static T FindOrNull<T>(Visual view, string automationId) where T : Visual
			=> view.GetVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == automationId);

		// The field-options "⋮" affordance opens the menu on a click OR keyboard activation; both arrive
		// as Button.Click, so raising it exercises the same path the icon does (no hit-test dependence on
		// the hover-reveal opacity state).
		private static void ClickKebab(Button kebab)
		{
			kebab.RaiseEvent(new RoutedEventArgs { RoutedEvent = Button.ClickEvent });
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void FieldMenuButton_OnLabelRow_RaisesTheSliceMenuRequest_WithTheLegacyMenuId()
		{
			var (view, requests) = Show(Field("Gloss", RegionFieldKind.Text, menuId: "mnuDataTree-Help"));

			// The "⋮" field-options button (which replaced right-click) opens the slice menu.
			ClickKebab(Find<Button>(view, "Gloss.FieldMenu"));

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(RegionMenuKind.SliceMenu));
			Assert.That(requests[0].Field.MenuId, Is.EqualTo("mnuDataTree-Help"));
			Assert.That(requests[0].Field.ObjectHvo, Is.EqualTo(1234),
				"the request carries the bound object so command routing can target it");
		}

		[AvaloniaTest]
		public void RightClick_OnLabel_NoLongerRaisesAnyRequest()
		{
			var (view, requests) = Show(Field("Gloss", RegionFieldKind.Text, menuId: "mnuDataTree-Help"));

			// Right-click was retired in favor of the "⋮" button — the label no longer opens the menu.
			RightClick(Find<TextBlock>(view, "Gloss.Label"));

			Assert.That(requests, Is.Empty, "the slice menu now opens only from the field-options button");
		}

		[AvaloniaTest]
		public void RightClick_OnValueBox_WithContextMenuBinding_RaisesTheContextMenuRequest()
		{
			var (view, requests) = Show(Field("CitationForm", RegionFieldKind.Text,
				menuId: "mnuDataTree-Help", contextMenuId: "mnuDataTree-CitationFormContext"));

			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			RightClick(box);

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(RegionMenuKind.ContextMenu),
				"value-box right-click is the legacy MultiStringSlice in-string context menu");
			Assert.That(requests[0].Field.ContextMenuId, Is.EqualTo("mnuDataTree-CitationFormContext"));
		}

		[AvaloniaTest]
		public void FieldMenuButton_OnHotlinksOnlyHeader_RaisesTheHotlinksRequest()
		{
			var (view, requests) = Show(Field("Senses", RegionFieldKind.Header,
				hotlinksId: "mnuDataTree-Sense-Hotlinks", collapsible: true));

			// The header's "⋮" button raises the hotlinks request; the collapsible toggle (id "Senses")
			// is a SEPARATE button that still toggles the section.
			ClickKebab(Find<Button>(view, "Senses.FieldMenu"));

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(RegionMenuKind.Hotlinks));
			Assert.That(requests[0].Field.HotlinksId, Is.EqualTo("mnuDataTree-Sense-Hotlinks"));
		}

		// Discoverability parity (legacy SummaryCommandControl): a hotlink-bearing section header shows
		// an ALWAYS-VISIBLE inline command-link strip beneath it, not just the hover-gated "⋮" kebab.
		[AvaloniaTest]
		public void HotlinksStrip_AppearsForHotlinkHeader_IsAlwaysVisible_AndKeepsTheKebab()
		{
			var (view, _) = Show(Field("Senses", RegionFieldKind.Header,
				hotlinksId: "mnuDataTree-Sense-Hotlinks", collapsible: true));

			var strip = FindOrNull<Button>(view, "Senses.Hotlinks");
			Assert.That(strip, Is.Not.Null, "a header with a HotlinksId renders the inline command strip");
			// Always visible — NOT hover-gated like the kebab (Opacity 0 / not hit-testable at rest).
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
			var (view, requests) = Show(Field("Senses", RegionFieldKind.Header,
				hotlinksId: "mnuDataTree-Sense-Hotlinks", collapsible: true));

			// Activating the strip arrives as Button.Click (mouse OR keyboard), the same path the kebab uses.
			ClickKebab(Find<Button>(view, "Senses.Hotlinks"));

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Kind, Is.EqualTo(RegionMenuKind.Hotlinks),
				"the strip dispatches the SAME hotlinks request the kebab raises");
			Assert.That(requests[0].Field.HotlinksId, Is.EqualTo("mnuDataTree-Sense-Hotlinks"));
		}

		[AvaloniaTest]
		public void HotlinksStrip_IsAbsent_ForHeaderWithoutHotlinks()
		{
			var (view, _) = Show(Field("Notes", RegionFieldKind.Header, menuId: "mnuDataTree-Object"));

			Assert.That(FindOrNull<Button>(view, "Notes.Hotlinks"), Is.Null,
				"a header carrying only a slice menu (no hotlinks) gets no inline command strip");
		}

		// 15.2 — exactly one menu: a bridged value box must NOT keep the TextBox theme flyout
		// (Cut/Copy/Paste) that otherwise opens alongside the bridged menu; unbound boxes keep
		// the local Copy flyout.
		[AvaloniaTest]
		public void BridgedValueBox_DropsTheThemeFlyout_UnboundKeepsCopy()
		{
			var (boundView, _) = Show(Field("CitationForm", RegionFieldKind.Text,
				contextMenuId: "mnuDataTree-CitationFormContext"));
			var boundBox = boundView.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(boundBox.ContextFlyout, Is.Null,
				"a bridged box must not raise a second (built-in) menu");

			var (plainView, _) = Show(Field("Comment", RegionFieldKind.Text));
			var plainBox = plainView.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(plainBox.ContextFlyout, Is.Not.Null, "unbound rows keep the local Copy flyout");
		}

		[AvaloniaTest]
		public void RightClick_OnUnboundRow_RaisesNoRequest()
		{
			var (view, requests) = Show(Field("Comment", RegionFieldKind.Text));

			RightClick(Find<TextBlock>(view, "Comment.Label"));
			RightClick(view.GetVisualDescendants().OfType<TextBox>().First());

			Assert.That(requests, Is.Empty,
				"rows without a legacy menu binding keep local behavior (Copy flyout) only");
			Assert.That(FindOrNull<Button>(view, "Comment.FieldMenu"), Is.Null,
				"a row with no menu/hotlinks binding gets no field-options button");
		}

		[AvaloniaTest]
		public void FieldMenuButton_IsHiddenAtRest_RevealedOnHover_AndKeyboardAddressable()
		{
			var (view, _) = Show(Field("Gloss", RegionFieldKind.Text, menuId: "mnuDataTree-Help"));

			var kebab = Find<Button>(view, "Gloss.FieldMenu");
			// Accessibility: a localized name so a screen reader announces the affordance.
			Assert.That(AutomationProperties.GetName(kebab), Is.EqualTo(FwAvaloniaStrings.FieldOptionsMenu));
			// Stays in layout/focusable at rest (so Tab reaches it) but hidden by opacity until revealed.
			Assert.That(kebab.IsVisible, Is.True, "the button stays in the tree (focusable) at rest");
			Assert.That(kebab.Opacity, Is.EqualTo(0d), "hidden by opacity until the row is hovered/focused");
			Assert.That(kebab.IsHitTestVisible, Is.False, "and not clickable until revealed");
			Assert.That(kebab.Focusable, Is.True, "Tab can reach the field-options button");

			// Focusing it (the keyboard path) reveals it synchronously (hit-testable); the opacity then
			// fades in over the transition — the same mechanism HoverRevealTests covers in detail.
			kebab.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(kebab.IsFocused, Is.True, "the opacity-hidden button is keyboard-focusable");
			Assert.That(kebab.IsHitTestVisible, Is.True, "keyboard focus reveals the field-options button");
		}

		// 15.1 — the host-resolved xCore items render as a native Avalonia flyout: items in order,
		// separators, submenus, disabled state, checkmarks, and click dispatching the execute action.
		[AvaloniaTest]
		public void RegionMenuFlyout_BuildsItems_AndClickExecutes()
		{
			var executed = 0;
			var items = new List<RegionMenuItem>
			{
				new RegionMenuItem("Insert Sense", execute: () => executed++),
				RegionMenuItem.Separator(),
				new RegionMenuItem("Delete this Sense", isEnabled: false),
				new RegionMenuItem("Field Visibility", children: new List<RegionMenuItem>
				{
					new RegionMenuItem("Always visible", isChecked: true)
				})
			};

			var flyout = RegionMenuFlyout.Build(items);
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

		// Context-menu density: every item pins the explicit compact padding/height of the legacy
		// WinForms menus (FwAvaloniaDensity tokens), never the (much taller) Fluent theme defaults —
		// nested submenu items included.
		[AvaloniaTest]
		public void RegionMenuFlyout_ItemDensity_IsPinnedCompact_IncludingSubmenus()
		{
			var flyout = RegionMenuFlyout.Build(new List<RegionMenuItem>
			{
				new RegionMenuItem("Insert Sense", execute: () => { }),
				new RegionMenuItem("Field Visibility", children: new List<RegionMenuItem>
				{
					new RegionMenuItem("Always visible")
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

		[AvaloniaTest]
		public void LeftClick_OnBoundLabel_RaisesNoRequest()
		{
			var (view, requests) = Show(Field("Gloss", RegionFieldKind.Text, menuId: "mnuDataTree-Help"));

			var label = Find<TextBlock>(view, "Gloss.Label");
			var root = (Visual)label.GetVisualRoot();
			var position = label.TranslatePoint(new Point(2, 2), root) ?? new Point(0, 0);
			label.RaiseEvent(new PointerPressedEventArgs(label,
				new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true),
				root, position, 0,
				new PointerPointProperties(RawInputModifiers.LeftMouseButton,
					PointerUpdateKind.LeftButtonPressed),
				KeyModifiers.None));
			Dispatcher.UIThread.RunJobs();

			Assert.That(requests, Is.Empty, "only the right button opens the slice menu, like legacy");
		}
	}
}
