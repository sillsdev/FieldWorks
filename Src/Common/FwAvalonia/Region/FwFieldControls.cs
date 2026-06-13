// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// FieldWorks-owned multi-writing-system text field over an IR-projected region field
	/// (tasks 6.1/6.2): one compact row per writing-system alternative — abbreviation gutter plus a
	/// text editor carrying the project WS font, right-to-left flow direction for RTL scripts, and
	/// per-WS keyboard activation on focus through the supplied callback (the same behavior legacy
	/// slices get from <c>EditingHelper.SetKeyboardForWs</c>). Write-through staging goes to the
	/// edit context when one is supplied; otherwise the field is read-only display.
	/// The plain-text lane only: the rich TsString editor is the 6.13 gate.
	/// Menus: a row whose layout binds a slice menu (`menu=`, e.g. the Lexeme Form's
	/// mnuDataTree-LexemeForm with Swap/Convert commands) surfaces it on RIGHT-CLICK only (the
	/// label/value right-click lanes) — text rows draw NO gear. The gear is reserved for the
	/// "configure the supporting list" jump on chooser/vector rows; it never opens a menu.
	/// </summary>
	public sealed class FwMultiWsTextField : StackPanel, IHoverAffordanceProvider
	{
		public FwMultiWsTextField(
			LexicalEditRegionField field,
			string automationId,
			IRegionEditContext editContext,
			Action<string> writingSystemFocused,
			Action<RegionMenuRequest> menuRequested = null)
		{
			Spacing = FwAvaloniaDensity.RowSpacing;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			foreach (var value in field.Values)
			{
				// Legacy look (12.3): small raised blue abbreviation hanging at the value start.
				var abbrev = new TextBlock
				{
					Text = value.WsAbbrev,
					MinWidth = FwAvaloniaDensity.WsAbbrevWidth,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(0, 1, 4, 0),
					FontSize = FwAvaloniaDensity.WsAbbrevFontSize,
					Foreground = FwAvaloniaDensity.WsAbbrevBrush
				};

				// Legacy look (12.2): values render flat like RootSite views — no box, no fill.
				// Local values outrank the theme's pointer-over/focus setters, so the editor stays flat.
				var box = new TextBox
				{
					Text = value.Value,
					Padding = FwAvaloniaDensity.EditorPadding,
					MinHeight = 0,
					AcceptsReturn = false,
					IsReadOnly = editContext == null || !field.IsEditable,
					FlowDirection = value.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
					BorderThickness = new Thickness(0),
					Background = Brushes.Transparent,
					TextWrapping = TextWrapping.Wrap // 14.5: long values wrap; the row grows vertically
				};
				if (!string.IsNullOrEmpty(value.FontFamily))
					box.FontFamily = new FontFamily(value.FontFamily);
				if (value.FontSize > 0)
					box.FontSize = value.FontSize;
				if (value.Bold)
					box.FontWeight = FontWeight.Bold; // legacy <properties><bold value='on'/> (11.15)

				if (!string.IsNullOrEmpty(field.GhostPrompt))
				{
					// 14.1: the legacy ghost add-prompt is a watermark — it disappears the moment the
					// user clicks in (focus), and reappears only if they leave without typing.
					box.Watermark = field.GhostPrompt;
					box.GotFocus += (s2, e2) => box.Watermark = string.Empty;
					box.LostFocus += (s2, e2) =>
					{
						if (string.IsNullOrEmpty(box.Text))
							box.Watermark = field.GhostPrompt;
					};
				}

				// Section 13: a row with a legacy `contextMenu=` binding shows the SAME xCore-defined
				// menu the legacy string view shows (MultiStringSlice.HandleRightMouseClickedEvent
				// path), routed through the host bridge. Rows without one keep the local Copy menu.
				if (menuRequested != null && !string.IsNullOrEmpty(field.ContextMenuId))
				{
					box.AddHandler(InputElement.PointerPressedEvent, (s2, e2) =>
					{
						if (!e2.GetCurrentPoint(box).Properties.IsRightButtonPressed)
							return;
						var screen = box.PointToScreen(e2.GetPosition(box));
						menuRequested(new RegionMenuRequest(field, RegionMenuKind.ContextMenu, screen.X, screen.Y));
						e2.Handled = true;
					}, Avalonia.Interactivity.RoutingStrategies.Tunnel);
					// 15.2: exactly ONE menu — drop the TextBox theme flyout (Cut/Copy/Paste, which
					// opens from ContextRequested on right-button RELEASE) so only the bridged menu
					// shows, and swallow the request so nothing else opens.
					box.ContextFlyout = null;
					box.AddHandler(Control.ContextRequestedEvent, (s2, e2) => e2.Handled = true,
						Avalonia.Interactivity.RoutingStrategies.Tunnel);
				}
				else
				{
					// Viewing parity (11.17): a working local Copy menu.
					var copyItem = new MenuItem { Header = FwAvaloniaStrings.Copy };
					copyItem.Click += async (s2, e2) =>
					{
						var top = TopLevel.GetTopLevel(box);
						if (top?.Clipboard != null)
							await top.Clipboard.SetTextAsync(box.SelectedText?.Length > 0 ? box.SelectedText : box.Text ?? string.Empty);
					};
					box.ContextFlyout = new MenuFlyout { Items = { copyItem } };
				}
				// Both edits AND the per-row automation id (which RegionFocusMemory keys focus
				// restore on) address the writing system by its unique IETF tag (WsTag): the
				// abbreviation is user-editable and can collide across writing systems. Tag-less
				// rows (tests/fakes using aliases like "vern") keep the abbreviation fallback.
				var wsKey = string.IsNullOrEmpty(value.WsTag) ? value.WsAbbrev : value.WsTag;
				AutomationProperties.SetAutomationId(box, automationId + "." + wsKey);
				AutomationProperties.SetName(box, (field.Label ?? automationId) + " " + value.WsAbbrev);

				if (editContext != null && field.IsEditable)
				{
					// TextChanged also fires when the template first applies the initial value, so a
					// last-staged guard keeps construction and no-op events from staging. The guard
					// only advances on a SUCCESSFUL stage: a failed TrySetText leaves lastStaged at
					// the last text the domain actually received, so further edits (including retyping
					// the same text) re-attempt instead of being suppressed forever.
					var lastStaged = value.Value ?? string.Empty;
					box.TextChanged += (s, e) =>
					{
						var text = box.Text ?? string.Empty;
						if (text == lastStaged)
							return;
						if (editContext.TrySetText(field, wsKey, text))
							lastStaged = text;
					};
				}

				if (writingSystemFocused != null && !string.IsNullOrEmpty(value.WsTag))
				{
					// Per-WS keyboard switching (6.2): activate this writing system's keyboard when
					// its editor gains focus, exactly as legacy slices do per selection.
					var wsTag = value.WsTag;
					box.GotFocus += (s, e) => writingSystemFocused(wsTag);
				}

				var rowPanel = new DockPanel
				{
					// 14.2: a null background only hit-tests the glyphs — the whole row must
					// receive hover/right-click over the gaps too.
					Background = Brushes.Transparent
				};
				DockPanel.SetDock(abbrev, Dock.Left);
				rowPanel.Children.Add(abbrev);
				rowPanel.Children.Add(box);
				Children.Add(rowPanel);
			}
		}

		/// <summary>Text rows have no hover-revealed chrome (the slice menu is right-click only).</summary>
		public IReadOnlyList<Control> HoverAffordances => Array.Empty<Control>();
	}

	/// <summary>
	/// GEAR = CONFIGURE: the shared gear semantics of the chooser and reference-vector rows.
	/// Clicking the gear DIRECTLY dispatches the list-editor jump — the host's
	/// <see cref="RegionLinkRequest"/> callback rides the same lane the legacy chooser dialog's
	/// "Edit the … list" LinkLabel rides (ReallySimpleListChooser.AddLink kGotoLink →
	/// FollowLink). NO flyout, NO context menu opens from the gear; option flyouts carry zero
	/// link items. The gear renders ONLY when a list-edit target resolved at compose time (the
	/// row carries at least one goto <see cref="RegionChooserLink"/>); the FIRST link wins when
	/// several resolved (rare). Rows without a resolvable list editor draw no gear at all.
	/// </summary>
	internal static class RegionGearChrome
	{
		/// <summary>
		/// Builds the configure gear for a row, or null when no list-edit target resolves
		/// (no links on the row, or no host callback to dispatch through).
		/// </summary>
		internal static Button CreateConfigureGear(LexicalEditRegionField field, string automationId,
			Action<RegionLinkRequest> linkRequested)
		{
			if (linkRequested == null || field.ChooserLinks.Count == 0)
				return null;

			var link = field.ChooserLinks[0]; // first goto wins
			var gear = RegionChrome.CreateGearButton();
			AutomationProperties.SetAutomationId(gear, automationId + ".Settings");
			AutomationProperties.SetName(gear, string.Format(FwAvaloniaStrings.FieldSettingsFormat,
				field.Label ?? field.Field ?? automationId));
			ToolTip.SetTip(gear, link.Label);
			gear.Click += (s, e) =>
			{
				linkRequested(new RegionLinkRequest(field, link));
				e.Handled = true;
			};
			return gear;
		}
	}

	/// <summary>
	/// FieldWorks-owned chooser field (task 6.3): a button opening a flyout of service-backed options
	/// (the options come from the LCModel-sourced region model, not the control). The flyout is the
	/// shared compact <see cref="FwOptionPicker"/> — an AutoCompleteBox-based OPTIONS ONLY selector,
	/// no link items. Committing an
	/// option stages it through the edit context, closes the flyout, and returns focus to the button
	/// — the popup-focus-return behavior the seam specs require. Without an edit context the chooser
	/// is a read-only display of the current selection.
	/// Chrome: the button is transparent/borderless — the value text reads flat like the legacy
	/// combo. When the row's supporting list resolved a list-editor target (a composed goto
	/// <see cref="RegionChooserLink"/>), a hover-revealed CONFIGURE gear sits after the value and
	/// directly dispatches the host jump (<see cref="RegionGearChrome"/>) — it never opens the
	/// options. Rows without a resolvable list editor draw no gear.
	/// </summary>
	public sealed class FwChooserField : Button, IHoverAffordanceProvider
	{
		private string _selectedKey;
		private readonly TextBlock _valueText;
		private readonly Button _gear;

		public FwChooserField(
			LexicalEditRegionField field,
			string automationId,
			IRegionEditContext editContext,
			Action<RegionLinkRequest> linkRequested = null)
		{
			_selectedKey = field.SelectedOptionKey;
			Padding = FwAvaloniaDensity.EditorPadding;
			MinHeight = 0;
			HorizontalAlignment = HorizontalAlignment.Left;
			Background = Brushes.Transparent;
			BorderThickness = new Thickness(0);
			_valueText = new TextBlock
			{
				Text = CurrentName(field),
				VerticalAlignment = VerticalAlignment.Center
			};
			var content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children = { _valueText }
			};
			// GEAR = CONFIGURE: only a resolved list-edit target draws the gear; clicking it
			// dispatches the jump directly (a nested Button handles its own click, so the
			// chooser's flyout does NOT open from a gear click).
			_gear = RegionGearChrome.CreateConfigureGear(field, automationId, linkRequested);
			if (_gear != null)
				content.Children.Add(_gear);
			Content = content;
			// Read-only rows stay ENABLED: disabling the whole button would suppress its pointer
			// events (killing hover-reveal) and disable the nested configure gear — which is
			// NAVIGATION (the "Edit the … list" jump), not editing. Like FwDialogLauncherField,
			// only the value-editing affordance is withheld: no option flyout is wired below, so
			// clicking the value of a read-only row does nothing.
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			// The gear (and only the gear) hides until hover; the button itself is a hover source.
			// The region view widens the hover surface to the whole row (label included).
			HoverReveal.Attach(new Control[] { this }, HoverAffordances);

			if (editContext == null || !field.IsEditable)
				return;

			// "+"/chooser click = OPTIONS ONLY: the one compact filterable picker, zero links.
			var picker = new FwOptionPicker(field.Options, null, automationId);
			var flyout = FwOptionPicker.CreateOptionFlyout(picker, PlacementMode.BottomEdgeAlignedLeft);
			Flyout = flyout;

			picker.OptionCommitted += option =>
			{
				// The options are the field's own RegionChoiceOption instances, so the committed
				// option's key is exact even when display names repeat across options.
				if (option.Key != _selectedKey && editContext.TrySetOption(field, option.Key))
				{
					_selectedKey = option.Key;
					_valueText.Text = option.Name;
				}

				flyout.Hide();
				Focus(); // popup focus return: back to the launcher
			};
			picker.Dismissed += (s, e) =>
			{
				flyout.Hide();
				Focus();
			};
		}

		// Restyled chrome only — the control keeps the Button theme (template, flyout-on-click,
		// focus, automation peer), not a lookup by this derived type's key.
		protected override Type StyleKeyOverride => typeof(Button);

		/// <summary>The currently selected option key (staged or initial).</summary>
		public string SelectedKey => _selectedKey;

		/// <summary>The display text of the current selection (what the value TextBlock shows).</summary>
		public string ValueText => _valueText.Text;

		/// <summary>The configure gear (only when a list-edit target resolved); empty otherwise.</summary>
		public IReadOnlyList<Control> HoverAffordances
			=> _gear == null ? Array.Empty<Control>() : new Control[] { _gear };

		private static string CurrentName(LexicalEditRegionField field)
		{
			var selected = field.Options.FirstOrDefault(o => o.Key == field.SelectedOptionKey);
			return selected?.Name ?? string.Empty;
		}
	}

	/// <summary>
	/// FieldWorks-owned editable reference-vector field (6.3/B8): the current items rendered
	/// inline, each followed by the thin grey separator bar legacy reference slices draw
	/// (VwSeparatorBox), with the TRAILING bar fronting the add slot — a "+" launcher whose flyout
	/// is the shared compact <see cref="FwOptionPicker"/> (AutoCompleteBox-based OPTIONS ONLY,
	/// zero link items): the
	/// possibility tree indented by <see cref="RegionChoiceOption.Depth"/> for enumerated lists,
	/// or the host search delegate's results for search-backed vectors (lexicons search, lists
	/// enumerate — D3), both behind the same filter box and virtualized capped list.
	/// Right-clicking an item offers Remove. Without an edit context the row is read-only display.
	/// Chrome (hover-reveal polish): the separator bars, the "+" launcher, and — only when the
	/// row's list resolved a list-editor target — the CONFIGURE gear (which directly dispatches
	/// the host jump, never a flyout: <see cref="RegionGearChrome"/>) fade in on row hover; the
	/// items/text stay always visible.
	/// </summary>
	public sealed class FwReferenceVectorField : StackPanel, IHoverAffordanceProvider
	{
		private readonly List<Control> _affordances = new List<Control>();

		/// <summary>
		/// <paramref name="gestureCompleted"/> (optional, like the other field callbacks): invoked
		/// after a SUCCESSFUL add/remove stage, so the host view can commit the gesture immediately
		/// — legacy commits each chooser-dialog gesture as it lands, and the row's Items are a
		/// compose-time snapshot, so without a commit + re-show nothing visibly changes.
		/// Failed stages never fire it.
		/// </summary>
		public FwReferenceVectorField(
			LexicalEditRegionField field,
			string automationId,
			IRegionEditContext editContext,
			Action gestureCompleted = null,
			Action<RegionLinkRequest> linkRequested = null)
		{
			Orientation = Orientation.Horizontal;
			// 14.2-style hit-testing rule: a null background only hit-tests the glyphs — the WHOLE
			// row must receive hover so the reveal chrome works over the gaps between items.
			Background = Brushes.Transparent;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			var editable = editContext != null && field.IsEditable;
			foreach (var item in field.Items)
			{
				var text = new TextBlock
				{
					Text = item.Name,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(0, 0, 4, 0),
					// 14.2: a null background only hit-tests the glyphs — the whole item must take
					// the right-click or the Remove flyout only opens over ink.
					Background = Brushes.Transparent
				};
				AutomationProperties.SetAutomationId(text, automationId + ".Item." + item.Key);
				if (editable)
				{
					var removeItem = new MenuItem { Header = FwAvaloniaStrings.Remove };
					var key = item.Key;
					removeItem.Click += (s, e) =>
					{
						// Only a successful stage completes the gesture (commit + host re-show).
						if (editContext.TryRemoveReferenceItem(field, key))
							gestureCompleted?.Invoke();
					};
					text.ContextFlyout = new MenuFlyout { Items = { removeItem } };
				}
				Children.Add(text);
				AddSeparatorBar();
			}

			if (!editable)
			{
				// Read-only rows still get the hover-reveal chrome for their separator bars.
				HoverReveal.Attach(new Control[] { this }, _affordances);
				return;
			}

			// The legacy empty add slot: a trailing bar (added above for the last item; one leads
			// the launcher when the vector is empty) plus the chooser launcher.
			if (field.Items.Count == 0)
				AddSeparatorBar();

			var addButton = new Button
			{
				Content = "+",
				Padding = new Thickness(4, 0, 4, 0),
				MinHeight = 0,
				MinWidth = 0,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Foreground = FwAvaloniaDensity.WsAbbrevBrush
			};
			AutomationProperties.SetAutomationId(addButton, automationId + ".Add");
			AutomationProperties.SetName(addButton, FwAvaloniaStrings.AddItem);

			// "+" = OPTIONS ONLY: the one compact filterable picker — static options enumerate
			// (with Depth hierarchy), search-backed vectors ride the host search delegate (D3).
			// No link items ever ride this flyout.
			var picker = new FwOptionPicker(field.Options, field.SearchOptions, automationId,
				field.Items.Select(i => i.Key));
			var flyout = FwOptionPicker.CreateOptionFlyout(picker, PlacementMode.BottomEdgeAlignedLeft);
			addButton.Flyout = flyout;
			picker.OptionCommitted += option =>
			{
				var added = editContext.TryAddReferenceItem(field, option.Key);
				flyout.Hide();
				addButton.Focus(); // popup focus return, like the chooser
				// Only a successful stage completes the gesture (commit + host re-show).
				if (added)
					gestureCompleted?.Invoke();
			};
			picker.Dismissed += (s, e) =>
			{
				flyout.Hide();
				addButton.Focus();
			};
			Children.Add(addButton);
			_affordances.Add(addButton);

			// GEAR = CONFIGURE (only when the row's list resolved a list-editor target): clicking
			// dispatches the host jump directly — it does NOT open the add flyout.
			var gearButton = RegionGearChrome.CreateConfigureGear(field, automationId, linkRequested);
			if (gearButton != null)
			{
				Children.Add(gearButton);
				_affordances.Add(gearButton);
			}

			// Bars, launcher, and gear hide until hover; the whole field panel is a hover source
			// (the region view widens the surface to the row's label too). Items stay always visible.
			HoverReveal.Attach(new Control[] { this }, _affordances);
		}

		/// <summary>The separator bars, "+" launcher, and configure gear reveal on row hover.</summary>
		public IReadOnlyList<Control> HoverAffordances => _affordances;

		// The legacy VwSeparatorBox: a ~2px, font-height, light grey vertical bar after each item
		// (and fronting the add slot) — the affordance that marks where content can be added.
		private void AddSeparatorBar()
		{
			var bar = new Border
			{
				Width = 2,
				Height = 14,
				Background = Brushes.LightGray,
				Margin = new Thickness(2, 0, 6, 0),
				VerticalAlignment = VerticalAlignment.Center
			};
			Children.Add(bar);
			_affordances.Add(bar);
		}
	}

	/// <summary>
	/// FieldWorks-owned dialog-launcher row (winforms-free-lexeme-editor.md D4): the legacy
	/// <c>*DlgLauncherSlice</c> pattern — the field's current value as read-only text plus the
	/// trailing launcher button, now the SAME hover-revealed settings gear the chooser and
	/// reference vector draw (it replaced the always-visible legacy "..."). The button invokes a
	/// host-injected callback (the ILegacyDialogLauncher seam on the xWorks side; this layer stays
	/// LCModel-free, so the callback is a plain delegate). Without a callback the gear renders
	/// DISABLED with an explanatory tooltip — the value still shows, the affordance is visibly
	/// unavailable once hover reveals it.
	/// </summary>
	public sealed class FwDialogLauncherField : DockPanel, IHoverAffordanceProvider
	{
		private readonly Action _launch;
		private readonly Button _button;

		public FwDialogLauncherField(string value, string label, Action launch)
		{
			_launch = launch;
			Value = value ?? string.Empty;
			LastChildFill = true;
			// 14.2: a null background only hit-tests the glyphs — the WHOLE row must receive hover
			// so the gear reveal works over the gaps.
			Background = Brushes.Transparent;
			AutomationProperties.SetName(this, label ?? string.Empty);

			// The legacy ButtonLauncher launch affordance, docked at the row's end like m_panel —
			// drawn as the shared settings gear, hover-revealed like the chooser/vector ones.
			_button = RegionChrome.CreateGearButton();
			_button.IsEnabled = launch != null;
			AutomationProperties.SetName(_button, FwAvaloniaStrings.LaunchDialog);
			if (launch == null)
			{
				// D4 degradation: no host dialog service — the gear shows but cannot launch.
				ToolTip.SetTip(_button, FwAvaloniaStrings.LauncherUnavailable);
			}
			_button.Click += (s, e) => Launch();

			var text = new TextBlock
			{
				Text = value ?? string.Empty,
				VerticalAlignment = VerticalAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 0, 6, 0),
				Background = Brushes.Transparent // 14.2 again: the value text is the hover surface
			};
			AutomationProperties.SetName(text, label ?? string.Empty);

			DockPanel.SetDock(_button, Dock.Right);
			Children.Add(_button);
			Children.Add(text);

			// The gear hides until hover; the whole row panel is a hover source (the region view
			// widens the surface to the row's label too, via IHoverAffordanceProvider).
			HoverReveal.Attach(new Control[] { this }, HoverAffordances);
		}

		/// <summary>The launch gear is the row's only hover-revealed affordance.</summary>
		public IReadOnlyList<Control> HoverAffordances => new[] { (Control)_button };

		/// <summary>The displayed value text (the legacy launcher view's rendering).</summary>
		public string Value { get; }

		/// <summary>Whether a launcher callback was injected (the button is enabled).</summary>
		public bool CanLaunch => _launch != null;

		/// <summary>The button-click path; a no-op without an injected callback.</summary>
		public void Launch() => _launch?.Invoke();
	}
}
