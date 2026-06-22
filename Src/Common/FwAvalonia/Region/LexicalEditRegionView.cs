// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia.Poc;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// A data-driven Avalonia view that renders a <see cref="LexicalEditRegionModel"/> (task 4.8). Unlike
	/// <c>PocLexEntrySlice</c>, which hard-codes three fields over a detached DTO, this view builds one
	/// row per region field from the typed view definition, so the same renderer scales as more fields
	/// are added to the definition. Each field's renderer is chosen from its <see cref="RegionFieldKind"/>.
	/// Stable, nonlocalized automation ids come from the field (falling back to the stable node id).
	///
	/// Editing (tasks 6.8/6.10, autosave 14.4): when an <see cref="IRegionEditContext"/> is supplied,
	/// field editors stage writes through it (which opens the fenced LCModel session on the first
	/// edit) and the session auto-commits on focus loss — the legacy save-as-you-go behavior, one
	/// undo step per field, no Save/Cancel chrome. Validation failures show inline and block the
	/// commit; Escape rolls the session back. Without a context the view is read-only display.
	/// </summary>
	public sealed class LexicalEditRegionView : UserControl
	{
		private readonly IRegionEditContext _editContext;
		private readonly Action<string> _writingSystemFocused;
		private readonly List<List<Control>> _rowControls = new List<List<Control>>();
		private static double s_labelColumnWidth = PocDensity.LabelColumnWidth;
		private TextBlock _validationBlock;

		private readonly Func<string, bool?> _getExpansionState;
		private readonly Action<string, bool> _expansionChanged;
		private readonly Action<RegionMenuRequest> _menuRequested;

		/// <summary>
		/// Optional expansion-state hooks (11.8): <paramref name="getExpansionState"/> supplies the
		/// persisted state per header stable id (overriding the layout's initial state) and
		/// <paramref name="expansionChanged"/> records toggles, so collapse state survives record
		/// switches/re-shows — the legacy PropertyTable expansion persistence.
		/// </summary>
		public LexicalEditRegionView(LexicalEditRegionModel model, IRegionEditContext editContext = null,
			Action<string> writingSystemFocused = null,
			Func<string, bool?> getExpansionState = null,
			Action<string, bool> expansionChanged = null,
			Action<RegionMenuRequest> menuRequested = null)
		{
			Model = model ?? throw new ArgumentNullException(nameof(model));
			_editContext = editContext;
			_writingSystemFocused = writingSystemFocused;
			_getExpansionState = getExpansionState;
			_expansionChanged = expansionChanged;
			_menuRequested = menuRequested;

			Name = "LexicalEditRegionView";
			AutomationProperties.SetAutomationId(this, "LexicalEditRegionView");
			AutomationProperties.SetName(this, FwAvaloniaStrings.LexicalEditRegionName);

			// Viewing parity (11.15): a draggable splitter divides the label and value columns like
			// the legacy slice splitter; its position is remembered for the session.
			var grid = new Grid
			{
				Margin = PocDensity.SliceMargin,
				ColumnDefinitions = new ColumnDefinitions
				{
					new ColumnDefinition(s_labelColumnWidth, GridUnitType.Pixel),
					new ColumnDefinition(PocDensity.SplitterWidth, GridUnitType.Pixel),
					new ColumnDefinition(GridLength.Star)
				}
			};
			var splitter = new GridSplitter
			{
				ResizeDirection = GridResizeDirection.Columns,
				Background = Brushes.Transparent, // legacy splitter is window-colored/invisible (12.6)
				Width = PocDensity.SplitterWidth
			};
			AutomationProperties.SetAutomationId(splitter, "LexicalEditRegionView.Splitter");
			Grid.SetColumn(splitter, 1);
			Grid.SetRowSpan(splitter, Math.Max(1, model.Fields.Count * 2));
			grid.Children.Add(splitter);
			grid.LayoutUpdated += (s, e) =>
			{
				var w = grid.ColumnDefinitions[0].Width;
				if (w.IsAbsolute && w.Value > 0)
					s_labelColumnWidth = w.Value;
			};

			for (var i = 0; i < model.Fields.Count; i++)
			{
				// Two grid rows per field: content + the legacy 1px rule between slices (12.1).
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
				grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Pixel));
				_rowControls.Add(new List<Control>());
				AddField(grid, i, model.Fields[i]);

				if (i < model.Fields.Count - 1)
				{
					var rule = new Border { Background = PocDensity.SliceRuleBrush, Height = 1 };
					AutomationProperties.SetAutomationId(rule, $"SliceRule.{i}");
					Grid.SetRow(rule, i * 2 + 1);
					// 14.3: the rule underlines the VALUE side only; the label panel stays clean.
					Grid.SetColumn(rule, 2);
					grid.Children.Add(rule);
					_rowControls[i].Add(rule); // collapses with its row
				}
			}

			WireCollapsibleHeaders(model.Fields);

			// Viewing parity (11.x): the whole region scrolls, like legacy DataTree's AutoScroll panel.
			Control body = grid;
			if (_editContext != null)
			{
				var panel = new StackPanel();
				panel.Children.Add(grid);
				panel.Children.Add(BuildEditFooter());
				body = panel;
			}

			var scroller = new ScrollViewer
			{
				Content = body,
				HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
			};
			AutomationProperties.SetAutomationId(scroller, "LexicalEditRegionView.Scroll");
			Content = scroller;

			// Screen-local command shortcuts (local phase of avalonia-command-focus, task 6.6):
			// Enter commits (validation-gated), Escape cancels — handled at the view so they work
			// from any field editor.
			AddHandler(Avalonia.Input.InputElement.KeyDownEvent, OnViewKeyDown,
				Avalonia.Interactivity.RoutingStrategies.Bubble);

			// Auto-save (14.4): legacy slices commit as the user moves on — any editor losing focus
			// while a session is open commits it (validation-gated; one undo step per field).
			AddHandler(Avalonia.Input.InputElement.LostFocusEvent, (s, e) =>
			{
				if (_editContext != null && _editContext.IsOpen)
					OnSave();
			}, Avalonia.Interactivity.RoutingStrategies.Bubble);
		}

		private void OnViewKeyDown(object sender, Avalonia.Input.KeyEventArgs e)
		{
			if (_editContext == null)
				return;
			switch (e.Key)
			{
				case Avalonia.Input.Key.Enter:
					OnSave();
					e.Handled = true;
					break;
				case Avalonia.Input.Key.Escape:
					OnCancel();
					e.Handled = true;
					break;
			}
		}

		/// <summary>The region model this view renders.</summary>
		public LexicalEditRegionModel Model { get; }

		/// <summary>
		/// Raised after a commit or cancel completed, so the host can re-resolve and re-show the
		/// region from current domain state.
		/// </summary>
		public event EventHandler EditCompleted;

		// 14.4: no Save/Cancel buttons — the legacy view saves as you go. The footer carries only the
		// inline validation messages (a failed autosave is never silent).
		private Control BuildEditFooter()
		{
			_validationBlock = new TextBlock
			{
				Foreground = Brushes.Firebrick,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 4, 0, 2),
				IsVisible = false
			};
			AutomationProperties.SetAutomationId(_validationBlock, "RegionEditor.ValidationErrors");

			var footer = new StackPanel { Margin = PocDensity.SliceMargin };
			footer.Children.Add(_validationBlock);
			return footer;
		}

		private void OnSave()
		{
			// Validation seam: commit only when clean; deterministic messages are shown in place
			// (never a silent failed save).
			var errors = _editContext.Validate();
			if (errors.Count > 0)
			{
				_validationBlock.Text = string.Join(Environment.NewLine, errors);
				_validationBlock.IsVisible = true;
				return;
			}

			_validationBlock.IsVisible = false;
			_editContext.Commit();
			EditCompleted?.Invoke(this, EventArgs.Empty);
		}

		private void OnCancel()
		{
			_validationBlock.IsVisible = false;
			_editContext.Cancel();
			EditCompleted?.Invoke(this, EventArgs.Empty);
		}

		private void AddField(Grid grid, int row, LexicalEditRegionField field)
		{
			var automationId = string.IsNullOrEmpty(field.AutomationId) ? field.StableId : field.AutomationId;
			var indent = new Thickness(field.Indent * 12, 0, 0, 0);

			// Section/group headers from full-layout composition span both columns (task 7.4-style
			// structure: the legacy tree's section rows).
			if (field.Kind == RegionFieldKind.Header)
			{
				Control header;
				if (field.IsCollapsible)
				{
					// Legacy SliceTreeNode +/- box equivalent: the header toggles its nested rows.
					var button = new Button
					{
						Content = (field.IsInitiallyExpanded ? "\u25bc " : "\u25b6 ") + (field.Label ?? field.Field ?? string.Empty),
						FontWeight = FontWeight.Bold,
						Background = Brushes.Transparent,
						BorderThickness = new Thickness(0),
						Padding = new Thickness(0),
						Margin = new Thickness(indent.Left, 4, 0, PocDensity.FieldSpacing)
					};
					header = button;
				}
				else
				{
					header = new TextBlock
					{
						Text = field.Label ?? field.Field ?? string.Empty,
						FontWeight = FontWeight.Bold,
						Margin = new Thickness(indent.Left, 4, 0, PocDensity.FieldSpacing),
						// 14.2: a null background only hit-tests the glyphs; the whole header area
						// must take the right-click.
						Background = Brushes.Transparent
					};
				}

				AutomationProperties.SetAutomationId(header, automationId);
				AutomationProperties.SetName(header, field.Label ?? string.Empty);
				WireSliceMenu(header, field); // 13.3/13.5: header right-click = section menu/hotlinks

				// Viewing parity (11.15): top-level sections get the legacy heavy-weight separator rule.
				Control headerControl = header;
				if (field.Indent == 0 && row > 0)
				{
					var withRule = new StackPanel();
					withRule.Children.Add(new Border
					{
						Height = 2,
						Background = Brushes.LightGray,
						Margin = new Thickness(0, 6, 0, 2)
					});
					withRule.Children.Add(header);
					headerControl = withRule;
				}

				Grid.SetRow(headerControl, row * 2);
				Grid.SetColumn(headerControl, 0);
				Grid.SetColumnSpan(headerControl, 3);
				grid.Children.Add(headerControl);
				_rowControls[row].Add(headerControl);
				return;
			}

			var labelBlock = new TextBlock
			{
				Text = field.Label ?? field.Field ?? string.Empty,
				Margin = new Thickness(indent.Left, 1, 6, PocDensity.FieldSpacing),
				VerticalAlignment = VerticalAlignment.Top,
				TextAlignment = TextAlignment.Left, // legacy labels are left-aligned in the label panel
				Foreground = PocDensity.LabelBrush,
				FontSize = PocDensity.LabelFontSize,
				// 14.2: a null background only hit-tests the glyphs; the whole label area must take
				// the right-click for the slice menu.
				Background = Brushes.Transparent
			};
			AutomationProperties.SetAutomationId(labelBlock, automationId + ".Label");
			AutomationProperties.SetName(labelBlock, field.Label ?? field.Field ?? string.Empty);
			ToolTip.SetTip(labelBlock, field.Label ?? field.Field); // 11.17: legacy label tooltips
			WireSliceMenu(labelBlock, field); // 13.3: right-click on the label = legacy slice menu
			Grid.SetRow(labelBlock, row * 2);
			Grid.SetColumn(labelBlock, 0);
			grid.Children.Add(labelBlock);
			_rowControls[row].Add(labelBlock);

			var editor = BuildEditor(field, automationId);
			editor.Margin = new Thickness(0, 0, 0, PocDensity.FieldSpacing);
			Grid.SetRow(editor, row * 2);
			Grid.SetColumn(editor, 2);
			grid.Children.Add(editor);
			_rowControls[row].Add(editor);
		}

		// Section 13: right-click on a label/header surfaces the legacy slice menu (or the section's
		// hotlinks when only those exist) through the host bridge — the same menu ids legacy
		// DTMenuHandler resolves from the layout.
		private void WireSliceMenu(Control control, LexicalEditRegionField field)
		{
			if (_menuRequested == null)
				return;
			var hasMenu = !string.IsNullOrEmpty(field.MenuId);
			var hasHotlinks = !string.IsNullOrEmpty(field.HotlinksId);
			if (!hasMenu && !hasHotlinks)
				return;

			control.AddHandler(Avalonia.Input.InputElement.PointerPressedEvent, (s, e) =>
			{
				if (!e.GetCurrentPoint(control).Properties.IsRightButtonPressed)
					return;
				var screen = control.PointToScreen(e.GetPosition(control));
				var kind = hasMenu ? RegionMenuKind.SliceMenu : RegionMenuKind.Hotlinks;
				_menuRequested(new RegionMenuRequest(field, kind, screen.X, screen.Y));
				e.Handled = true;
			}, Avalonia.Interactivity.RoutingStrategies.Tunnel);
		}

		// Viewing parity (11.x): a collapsible header owns every following row with greater indent,
		// up to the next field at its own indent or shallower — collapsing hides them (nested
		// sections collapse with their parent), expanding restores them, and the layout's expansion
		// attribute supplies the initial state.
		private void WireCollapsibleHeaders(IReadOnlyList<LexicalEditRegionField> fields)
		{
			for (var i = 0; i < fields.Count; i++)
			{
				var field = fields[i];
				if (field.Kind != RegionFieldKind.Header || !field.IsCollapsible)
					continue;
				var headerControl = _rowControls[i].FirstOrDefault();
				var button = headerControl as Button
					?? (headerControl as StackPanel)?.Children.OfType<Button>().FirstOrDefault();
				if (button == null)
					continue;

				var start = i + 1;
				var end = start;
				while (end < fields.Count && fields[end].Indent > field.Indent)
					end++;
				if (end == start)
					continue;

				var expanded = _getExpansionState?.Invoke(field.StableId) ?? field.IsInitiallyExpanded;
				var stableId = field.StableId;
				var label = field.Label ?? field.Field ?? string.Empty;
				void Apply()
				{
					button.Content = (expanded ? "\u25bc " : "\u25b6 ") + label;
					for (var r = start; r < end; r++)
					{
						foreach (var control in _rowControls[r])
							control.IsVisible = expanded;
					}
				}

				button.Click += (s, e) =>
				{
					expanded = !expanded;
					_expansionChanged?.Invoke(stableId, expanded);
					Apply();
				};

				if (!expanded)
					Apply();
			}
		}

		private Control BuildEditor(LexicalEditRegionField field, string automationId)
		{
			switch (field.Kind)
			{
				case RegionFieldKind.Chooser:
					return BuildChooser(field, automationId);
				case RegionFieldKind.Boolean:
					return BuildBoolean(field, automationId);
				case RegionFieldKind.Image:
					return BuildImage(field, automationId);
				case RegionFieldKind.Command:
					return BuildCommand(field, automationId);
				case RegionFieldKind.Unsupported:
					return BuildUnsupported(field, automationId);
				default:
					return BuildText(field, automationId);
			}
		}

		// Pictures render the actual image (legacy PictureSlice); a missing file shows its path.
		private static Control BuildImage(LexicalEditRegionField field, string automationId)
		{
			var path = field.Values.Count > 0 ? field.Values[0].Value : null;
			Control content;
			if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
			{
				try
				{
					content = new Image
					{
						Source = new Avalonia.Media.Imaging.Bitmap(path),
						MaxHeight = 120,
						Stretch = Stretch.Uniform,
						HorizontalAlignment = HorizontalAlignment.Left
					};
				}
				catch (Exception)
				{
					content = new TextBlock { Text = path, Foreground = Brushes.Gray };
				}
			}
			else
			{
				content = new TextBlock { Text = path ?? string.Empty, Foreground = Brushes.Gray };
			}

			AutomationProperties.SetAutomationId(content, automationId);
			AutomationProperties.SetName(content, field.Label ?? automationId);
			return content;
		}

		// Command slices render their button (legacy CommandSlice); execution arrives with the
		// xCore command bridge (shell phase), so the button is present but disabled until then.
		private static Control BuildCommand(LexicalEditRegionField field, string automationId)
		{
			var button = new Button
			{
				Content = field.Label ?? field.Field ?? string.Empty,
				IsEnabled = false,
				MinWidth = 130
			};
			AutomationProperties.SetAutomationId(button, automationId);
			AutomationProperties.SetName(button, field.Label ?? automationId);
			return button;
		}

		// Boolean fields render as checkboxes (legacy CheckboxSlice), staging through the option seam.
		private Control BuildBoolean(LexicalEditRegionField field, string automationId)
		{
			var box = new CheckBox
			{
				IsChecked = field.SelectedOptionKey == "true",
				IsEnabled = _editContext != null && field.IsEditable,
				MinHeight = 0
			};
			AutomationProperties.SetAutomationId(box, automationId);
			AutomationProperties.SetName(box, field.Label ?? field.Field ?? automationId);

			if (_editContext != null && field.IsEditable)
			{
				box.IsCheckedChanged += (s, e) =>
					_editContext.TrySetOption(field, box.IsChecked == true ? "true" : "false");
			}

			return box;
		}

		// Owned controls (tasks 6.1/6.2/6.3): multi-WS text field with project fonts, RTL flow, and
		// per-WS keyboard focus callback; service-backed flyout chooser with popup focus return.
		private Control BuildText(LexicalEditRegionField field, string automationId)
			=> new FwMultiWsTextField(field, automationId, _editContext, _writingSystemFocused, _menuRequested);

		private Control BuildChooser(LexicalEditRegionField field, string automationId)
			=> new FwChooserField(field, automationId, _editContext);

		private static Control BuildUnsupported(LexicalEditRegionField field, string automationId)
		{
			var block = new TextBlock
			{
				Text = FwAvaloniaStrings.UnsupportedEditor,
				Foreground = Brushes.Gray,
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(block, automationId);
			AutomationProperties.SetName(block, field.Label ?? field.Field ?? automationId);
			return block;
		}
	}
}
