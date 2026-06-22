// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia.Poc;

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
	/// </summary>
	public sealed class FwMultiWsTextField : StackPanel
	{
		public FwMultiWsTextField(
			LexicalEditRegionField field,
			string automationId,
			IRegionEditContext editContext,
			Action<string> writingSystemFocused,
			Action<RegionMenuRequest> menuRequested = null)
		{
			Spacing = PocDensity.RowSpacing;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			foreach (var value in field.Values)
			{
				// Legacy look (12.3): small raised blue abbreviation hanging at the value start.
				var abbrev = new TextBlock
				{
					Text = value.WsAbbrev,
					MinWidth = PocDensity.WsAbbrevWidth,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(0, 1, 4, 0),
					FontSize = PocDensity.WsAbbrevFontSize,
					Foreground = PocDensity.WsAbbrevBrush
				};

				// Legacy look (12.2): values render flat like RootSite views — no box, no fill.
				// Local values outrank the theme's pointer-over/focus setters, so the editor stays flat.
				var box = new TextBox
				{
					Text = value.Value,
					Padding = PocDensity.EditorPadding,
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
					// last-staged guard keeps construction and no-op events from staging.
					var lastStaged = value.Value ?? string.Empty;
					box.TextChanged += (s, e) =>
					{
						var text = box.Text ?? string.Empty;
						if (text == lastStaged)
							return;
						lastStaged = text;
						editContext.TrySetText(field, wsKey, text);
					};
				}

				if (writingSystemFocused != null && !string.IsNullOrEmpty(value.WsTag))
				{
					// Per-WS keyboard switching (6.2): activate this writing system's keyboard when
					// its editor gains focus, exactly as legacy slices do per selection.
					var wsTag = value.WsTag;
					box.GotFocus += (s, e) => writingSystemFocused(wsTag);
				}

				var rowPanel = new DockPanel();
				DockPanel.SetDock(abbrev, Dock.Left);
				rowPanel.Children.Add(abbrev);
				rowPanel.Children.Add(box);
				Children.Add(rowPanel);
			}
		}
	}

	/// <summary>
	/// FieldWorks-owned chooser field (task 6.3): a button opening a flyout of service-backed options
	/// (the options come from the LCModel-sourced region model, not the control). Selecting an option
	/// stages it through the edit context, closes the flyout, and returns focus to the button — the
	/// popup-focus-return behavior the seam specs require. Without an edit context the chooser is a
	/// read-only display of the current selection.
	/// </summary>
	public sealed class FwChooserField : Button
	{
		private string _selectedKey;

		public FwChooserField(
			LexicalEditRegionField field,
			string automationId,
			IRegionEditContext editContext)
		{
			_selectedKey = field.SelectedOptionKey;
			Padding = PocDensity.EditorPadding;
			MinHeight = 0;
			HorizontalAlignment = HorizontalAlignment.Left;
			Content = CurrentName(field);
			IsEnabled = editContext != null && field.IsEditable;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			if (editContext == null || !field.IsEditable)
				return;

			var list = new ListBox
			{
				ItemsSource = field.Options.Select(o => o.Name).ToList(),
				MinWidth = 120
			};
			AutomationProperties.SetAutomationId(list, automationId + ".Options");

			var flyout = new Flyout { Content = list, Placement = PlacementMode.BottomEdgeAlignedLeft };
			Flyout = flyout;

			list.SelectionChanged += (s, e) =>
			{
				// The list items were materialized in field.Options order, so the selected INDEX is
				// the only safe way back to the option: display names may repeat across options.
				var index = list.SelectedIndex;
				if (index < 0 || index >= field.Options.Count)
					return;
				var option = field.Options[index];
				if (option.Key == _selectedKey)
					return;

				if (editContext.TrySetOption(field, option.Key))
				{
					_selectedKey = option.Key;
					Content = option.Name;
				}

				flyout.Hide();
				Focus(); // popup focus return: back to the launcher
			};
		}

		/// <summary>The currently selected option key (staged or initial).</summary>
		public string SelectedKey => _selectedKey;

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
	/// lists the possibility tree indented by <see cref="RegionChoiceOption.Depth"/> (the legacy
	/// chooser tree; virtualized ListBox so the ~1800-node semantic-domain list stays usable).
	/// Right-clicking an item offers Remove. Without an edit context the row is read-only display.
	/// </summary>
	public sealed class FwReferenceVectorField : StackPanel
	{
		public FwReferenceVectorField(
			LexicalEditRegionField field,
			string automationId,
			IRegionEditContext editContext)
		{
			Orientation = Orientation.Horizontal;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			var editable = editContext != null && field.IsEditable;
			foreach (var item in field.Items)
			{
				var text = new TextBlock
				{
					Text = item.Name,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(0, 0, 4, 0)
				};
				AutomationProperties.SetAutomationId(text, automationId + ".Item." + item.Key);
				if (editable)
				{
					var removeItem = new MenuItem { Header = FwAvaloniaStrings.Remove };
					var key = item.Key;
					removeItem.Click += (s, e) => editContext.TryRemoveReferenceItem(field, key);
					text.ContextFlyout = new MenuFlyout { Items = { removeItem } };
				}
				Children.Add(text);
				Children.Add(SeparatorBar());
			}

			if (!editable)
				return;

			// The legacy empty add slot: a trailing bar (added above for the last item; one leads
			// the launcher when the vector is empty) plus the chooser launcher.
			if (field.Items.Count == 0)
				Children.Add(SeparatorBar());

			var addButton = new Button
			{
				Content = "+",
				Padding = new Thickness(4, 0, 4, 0),
				MinHeight = 0,
				MinWidth = 0,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Foreground = PocDensity.WsAbbrevBrush
			};
			AutomationProperties.SetAutomationId(addButton, automationId + ".Add");
			AutomationProperties.SetName(addButton, FwAvaloniaStrings.AddItem);

			var list = new ListBox
			{
				ItemsSource = field.Options,
				MaxHeight = 320,
				MinWidth = 180,
				ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<RegionChoiceOption>(
					(option, _) => option == null
						? null
						: new TextBlock
						{
							Text = option.Name,
							Margin = new Thickness(option.Depth * 14, 0, 0, 0)
						})
			};
			AutomationProperties.SetAutomationId(list, automationId + ".Options");
			var flyout = new Flyout { Content = list, Placement = PlacementMode.BottomEdgeAlignedLeft };
			addButton.Flyout = flyout;
			list.SelectionChanged += (s, e) =>
			{
				if (list.SelectedItem is RegionChoiceOption option)
					editContext.TryAddReferenceItem(field, option.Key);
				flyout.Hide();
				list.SelectedItem = null;
				addButton.Focus(); // popup focus return, like the chooser
			};
			Children.Add(addButton);
		}

		// The legacy VwSeparatorBox: a ~2px, font-height, light grey vertical bar after each item
		// (and fronting the add slot) — the affordance that marks where content can be added.
		private static Control SeparatorBar() => new Border
		{
			Width = 2,
			Height = 14,
			Background = Brushes.LightGray,
			Margin = new Thickness(2, 0, 6, 0),
			VerticalAlignment = VerticalAlignment.Center
		};
	}
}
