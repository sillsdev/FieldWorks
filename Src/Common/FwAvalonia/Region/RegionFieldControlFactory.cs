// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// The small bundle of (all-nullable) collaborators a <see cref="RegionFieldKind"/> editor needs,
	/// passed to <see cref="RegionFieldControlFactory.Build"/> so the SAME field→control dispatch serves
	/// both the detail-pane region view (<c>LexicalEditRegionView.BuildEditor</c>, full callbacks) and the
	/// browse table's in-cell editor (<c>LexicalBrowseView.EditableCellHost</c>, gutter/abbreviation and
	/// the menu callbacks null). Every member is optional: a null edit context yields read-only display; a
	/// null callback simply disables that affordance. Task 21 — one switch, so new kinds live in one place.
	/// </summary>
	public sealed class RegionFieldControlContext
	{
		public RegionFieldControlContext(
			IRegionEditContext editContext = null,
			Action<string> writingSystemFocused = null,
			Action<RegionMenuRequest> menuRequested = null,
			Action<RegionLinkRequest> linkRequested = null,
			IFwClipboard clipboard = null,
			Action save = null,
			bool showWritingSystemAbbreviation = true,
			IRegionMediaServices mediaServices = null)
		{
			EditContext = editContext;
			WritingSystemFocused = writingSystemFocused;
			MenuRequested = menuRequested;
			LinkRequested = linkRequested;
			Clipboard = clipboard;
			Save = save;
			ShowWritingSystemAbbreviation = showWritingSystemAbbreviation;
			MediaServices = mediaServices;
		}

		/// <summary>
		/// §19d: the media seam for picture insert/replace/delete/properties and audio play/record (null →
		/// the picture/audio affordances are disabled, read-only display). Supplied by the host.
		/// </summary>
		public IRegionMediaServices MediaServices { get; }

		/// <summary>The shared edit-session/staging context; null → read-only display.</summary>
		public IRegionEditContext EditContext { get; }

		/// <summary>Per-WS keyboard activation callback for text fields (null → no keyboard switch).</summary>
		public Action<string> WritingSystemFocused { get; }

		/// <summary>Right-click slice/section menu callback (null on the browse cell surface today).</summary>
		public Action<RegionMenuRequest> MenuRequested { get; }

		/// <summary>Hyperlink follow callback for choosers/vectors (null → no link affordance).</summary>
		public Action<RegionLinkRequest> LinkRequested { get; }

		/// <summary>Clipboard seam for text fields (null → framework default).</summary>
		public IFwClipboard Clipboard { get; }

		/// <summary>
		/// Validation-gated commit invoked when a reference-vector add/remove gesture completes (the detail
		/// pane's autosave). Null on surfaces that drive commit themselves (the browse cell commits on
		/// Enter/Tab through its own active-cell session), in which case the vector field just stages.
		/// </summary>
		public Action Save { get; }

		/// <summary>
		/// Whether a multi-WS text field shows its per-WS abbreviation gutter. The detail pane shows it; the
		/// dense browse cell suppresses it (matching the legacy in-cell editor).
		/// </summary>
		public bool ShowWritingSystemAbbreviation { get; }
	}

	/// <summary>
	/// The single <see cref="RegionFieldKind"/>→Avalonia-control dispatch (Task 21). Previously the detail
	/// pane (<c>LexicalEditRegionView.BuildEditor</c>, all 7 kinds) and the browse in-cell editor
	/// (<c>EditableCellHost.Activate</c>, a 2-kind Chooser/Text subset that had DIVERGED) hand-rolled their
	/// own dispatch; both now route here, so adding a kind (or changing how a kind is built) happens once.
	/// The factory is pure (static) — all per-surface variation arrives through the
	/// <see cref="RegionFieldControlContext"/>.
	/// </summary>
	public static class RegionFieldControlFactory
	{
		public static Control Build(LexicalEditRegionField field, string automationId,
			RegionFieldControlContext context)
		{
			if (field == null) throw new ArgumentNullException(nameof(field));
			context = context ?? new RegionFieldControlContext();

			switch (field.Kind)
			{
				case RegionFieldKind.Custom:
					return BuildCustom(field, automationId);
				case RegionFieldKind.ReferenceVector:
					// Reference add/remove gestures commit immediately (legacy chooser-dialog behavior): the
					// staged session would otherwise sit open — LCModel broadcasts PropChanged only at
					// EndUndoTask and the row's Items are a compose-time snapshot, so the user would see no
					// change. The gesture-completed callback runs the SAME validation-gated save the
					// focus-loss autosave uses, whose re-show rebuilds the row from domain truth. A surface
					// that owns its own commit (the browse cell) passes a null Save and just stages.
					return new FwReferenceVectorField(field, automationId, context.EditContext,
						context.EditContext == null ? null : context.Save, context.LinkRequested);
				case RegionFieldKind.StructuredText:
					// §19a: an editable multi-paragraph StText field. Per-paragraph text edits stage and
					// ride the focus-loss autosave; structural gestures (add/delete/style) commit
					// immediately through the SAME validation-gated Save the reference vector uses, whose
					// re-show rebuilds the paragraph rows from domain truth (the Paragraphs list is a
					// compose-time snapshot). A surface owning its own commit passes a null Save.
					return new FwStructuredTextField(field, automationId, context.EditContext,
						context.WritingSystemFocused,
						context.EditContext == null ? null : context.Save, context.Clipboard);
				case RegionFieldKind.Chooser:
					return new FwChooserField(field, automationId, context.EditContext, context.LinkRequested);
				case RegionFieldKind.Boolean:
					return BuildBoolean(field, automationId, context);
				case RegionFieldKind.Date:
					return BuildDate(field, automationId, context);
				case RegionFieldKind.EnumCombo:
					return BuildEnumCombo(field, automationId, context);
				case RegionFieldKind.Integer:
					return BuildInteger(field, automationId, context);
				case RegionFieldKind.Literal:
					return BuildLiteral(field, automationId);
				case RegionFieldKind.Image:
					return BuildImage(field, automationId, context);
				case RegionFieldKind.Command:
					return BuildCommand(field, automationId);
				case RegionFieldKind.Unsupported:
					return BuildUnsupported(field, automationId);
				default:
					return new FwMultiWsTextField(field, automationId, context.EditContext,
						context.WritingSystemFocused, context.MenuRequested, context.Clipboard,
						context.ShowWritingSystemAbbreviation, context.MediaServices, context.Save);
			}
		}

		// §19d: pictures render the actual image (legacy PictureSlice) PLUS the edit affordances —
		// insert / replace / properties / delete — when the host supplies an edit context + media seam.
		// An empty field (PictureHvo 0) shows only the insert affordance ("Add a picture"). A missing
		// file shows its path. With no edit context / media seam the row is read-only display (the legacy
		// browse-cell / preview path), preserving the prior behavior. All affordances route LCModel-free
		// through the edit context's picture methods + the media seam (file pick / properties dialog).
		private static Control BuildImage(LexicalEditRegionField field, string automationId,
			RegionFieldControlContext context)
		{
			var path = field.Values.Count > 0 ? field.Values[0].Value : null;
			var hasPicture = field.PictureHvo != 0 && !string.IsNullOrEmpty(path);
			Control content;
			if (hasPicture && System.IO.File.Exists(path))
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
				catch (Exception e)
				{
					// Graceful degrade (legacy PictureSlice shows the path for a bad image), but never
					// silently: record what failed to load and why. Edge: a very large or unsupported image
					// never crashes the row — it falls back to the path text.
					System.Diagnostics.Debug.WriteLine(
						$"Picture field '{field.StableId}' failed to load image '{path}': {e.Message}");
					content = new TextBlock { Text = path, Foreground = Brushes.Gray };
				}
			}
			else if (field.PictureHvo == 0)
			{
				// Empty "insert a picture" ghost row.
				content = new TextBlock { Text = FwAvaloniaStrings.PictureInsert, Foreground = Brushes.Gray };
			}
			else
			{
				content = new TextBlock { Text = path ?? string.Empty, Foreground = Brushes.Gray };
			}

			AutomationProperties.SetAutomationId(content, automationId);
			AutomationProperties.SetName(content, field.Label ?? automationId);

			var affordances = BuildPictureAffordances(field, automationId, context);
			if (affordances == null)
				return content;

			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = FwAvaloniaDensity.RowSpacing,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			panel.Children.Add(content);
			panel.Children.Add(affordances);
			return panel;
		}

		// §19d: the insert / replace / properties / delete buttons for a picture row, or null when the
		// affordances are unavailable (no edit context or no media seam → read-only display).
		private static Control BuildPictureAffordances(LexicalEditRegionField field, string automationId,
			RegionFieldControlContext context)
		{
			if (context?.EditContext == null || context.MediaServices == null || !field.IsEditable)
				return null;

			var media = context.MediaServices;
			var edit = context.EditContext;
			var save = context.Save;
			var buttons = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = FwAvaloniaDensity.RowSpacing,
				VerticalAlignment = VerticalAlignment.Center
			};

			if (field.PictureHvo == 0)
			{
				// Empty row: a single "insert" affordance.
				var insert = MakePictureButton(FwAvaloniaStrings.PictureInsert, automationId + ".insert");
				insert.Click += (s, e) =>
				{
					var result = media.ShowPictureProperties(null, isNew: true);
					if (result?.SourceFile == null)
						return;
					if (edit.TryInsertPicture(field, result.SourceFile, result.Metadata))
						save?.Invoke();
				};
				buttons.Children.Add(insert);
				return buttons;
			}

			// Existing picture: properties (edit caption/license/creator + replace file) and delete.
			var properties = MakePictureButton(FwAvaloniaStrings.PictureProperties, automationId + ".properties");
			properties.Click += (s, e) =>
			{
				var result = media.ShowPictureProperties(field.PictureMetadata, isNew: false);
				if (result == null)
					return;
				var changed = false;
				if (result.SourceFile != null)
					changed |= edit.TryReplacePictureFile(field, result.SourceFile);
				changed |= edit.TrySetPictureMetadata(field, result.Metadata);
				if (changed)
					save?.Invoke();
			};
			buttons.Children.Add(properties);

			var delete = MakePictureButton(FwAvaloniaStrings.PictureDelete, automationId + ".delete");
			delete.Click += (s, e) =>
			{
				if (edit.TryDeletePicture(field))
					save?.Invoke();
			};
			buttons.Children.Add(delete);
			return buttons;
		}

		private static Button MakePictureButton(string text, string automationId)
		{
			var button = new Button { Content = text, MinHeight = 0, Padding = FwAvaloniaDensity.EditorPadding };
			AutomationProperties.SetAutomationId(button, automationId);
			AutomationProperties.SetName(button, text);
			return button;
		}

		// Command slices render their button (legacy CommandSlice); execution arrives with the xCore command
		// bridge (shell phase), so the button is present but disabled until then.
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
		private static Control BuildBoolean(LexicalEditRegionField field, string automationId,
			RegionFieldControlContext context)
		{
			var box = new CheckBox
			{
				IsChecked = field.SelectedOptionKey == "true",
				IsEnabled = context.EditContext != null && field.IsEditable,
				MinHeight = 0
			};
			AutomationProperties.SetAutomationId(box, automationId);
			AutomationProperties.SetName(box, field.Label ?? field.Field ?? automationId);

			if (context.EditContext != null && field.IsEditable)
			{
				box.IsCheckedChanged += (s, e) =>
					context.EditContext.TrySetOption(field, box.IsChecked == true ? "true" : "false");
			}

			return box;
		}

		// Date / generic-date fields render an editable single-line entry (legacy DateSlice/GenDateSlice).
		// The committed text is staged through the edit context's option seam — the composer-supplied
		// setter PARSES it (DateTime.TryParse / GenDate.TryParse): a parseable string stages and the
		// gesture commits; an unparseable one is REJECTED (the box restores the last committed value,
		// like the legacy launchers), so invalid input can never corrupt the stored date/gendate.
		// An empty box clears the field (the setter treats empty as the empty value).
		private static Control BuildDate(LexicalEditRegionField field, string automationId,
			RegionFieldControlContext context)
		{
			// §19e: a generic (vague) date gets the structured qualifier editor (year + precision + era +
			// circa) — the legacy GenDateLauncher/GenDateChooserDlg surface — instead of a bare text box.
			if (field.DateKind == RegionDateKind.GenDate)
				return new FwGenDateField(field, automationId, context.EditContext, context.Save);

			var initial = field.Values.Count > 0 ? field.Values[0].Value ?? string.Empty : string.Empty;
			var box = new TextBox
			{
				Text = initial,
				Padding = FwAvaloniaDensity.EditorPadding,
				MinHeight = 0,
				AcceptsReturn = false,
				IsReadOnly = context.EditContext == null || !field.IsEditable,
				BorderThickness = new Thickness(0),
				Background = Brushes.Transparent,
				HorizontalAlignment = HorizontalAlignment.Left,
				MinWidth = 130,
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(box, automationId);
			AutomationProperties.SetName(box, field.Label ?? field.Field ?? automationId);

			if (box.IsReadOnly)
				return box;

			var lastCommitted = initial;
			Action<string> stage = text =>
			{
				if (text == lastCommitted)
					return;
				// The setter parses+stages; a failed parse leaves the stored value untouched, so the
				// box restores what the domain actually holds rather than presenting bad text as saved.
				if (context.EditContext.TrySetOption(field, text))
				{
					lastCommitted = text;
					box.Text = text;
					context.Save?.Invoke();
				}
				else
				{
					box.Text = lastCommitted;
				}
			};
			Action commit = () => stage(box.Text ?? string.Empty);
			EventHandler<Avalonia.Interactivity.RoutedEventArgs> lostFocus = (s, e) => commit();
			EventHandler<Avalonia.Input.KeyEventArgs> keyDown = (s, e) =>
			{
				if (e.Key == Avalonia.Input.Key.Enter)
				{
					commit();
					e.Handled = true;
				}
			};
			box.LostFocus += lostFocus;
			box.KeyDown += keyDown;

			// §19e: an exact date (legacy DateSlice) also offers a calendar day picker (the legacy
			// MonthCalendar). Picking a day commits its short-date string through the same parse-on-commit
			// seam, so the calendar and the text box share one staging path.
			var picker = new CalendarDatePicker
			{
				MinHeight = 0,
				MinWidth = 36,
				VerticalAlignment = VerticalAlignment.Center,
				Watermark = string.Empty
			};
			AutomationProperties.SetAutomationId(picker, automationId + ".calendar");
			AutomationProperties.SetName(picker, field.Label ?? field.Field ?? automationId);
			// Track the picker's known date so its change handler only stages a GENUINE user day-pick — not
			// the initial programmatic seed (headless raises SelectedDateChanged for it during layout). The
			// seed and any later state we set update lastPickerDate WITHOUT staging.
			DateTime? lastPickerDate = null;
			if (DateTime.TryParse(initial, System.Globalization.CultureInfo.CurrentUICulture,
				System.Globalization.DateTimeStyles.None, out var seeded))
			{
				lastPickerDate = seeded.Date;
				picker.SelectedDate = seeded;
			}
			picker.SelectedDateChanged += (s, e) =>
			{
				var picked = picker.SelectedDate?.Date;
				if (picked == lastPickerDate)
					return; // the seed echo (or a no-op re-set) — not a user pick
				lastPickerDate = picked;
				if (picked.HasValue)
					stage(picked.Value.ToString("f", System.Globalization.CultureInfo.CurrentUICulture));
			};

			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = FwAvaloniaDensity.RowSpacing,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			panel.Children.Add(box);
			panel.Children.Add(picker);
			AutomationProperties.SetAutomationId(panel, automationId + ".row");
			return panel;
		}

		// §19e — Enum closed-combo (legacy EnumComboSlice): a CLOSED (non-editable) ComboBox over the
		// field's options (key = the 0-based enum index that IS the stored int). Selecting an option
		// stages its key through the option seam; the combo cannot accept free text, so an out-of-range
		// enum value can never be typed in. No edit context => disabled (read-only display). This is the
		// structural difference from the search/flyout chooser: a small bounded enum is a plain drop-down.
		private static Control BuildEnumCombo(LexicalEditRegionField field, string automationId,
			RegionFieldControlContext context)
		{
			var combo = new ComboBox
			{
				ItemsSource = field.Options,
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				HorizontalAlignment = HorizontalAlignment.Left,
				MinWidth = 130,
				IsEnabled = context?.EditContext != null && field.IsEditable,
				DisplayMemberBinding = new Avalonia.Data.Binding(nameof(RegionChoiceOption.Name))
			};
			AutomationProperties.SetAutomationId(combo, automationId);
			AutomationProperties.SetName(combo, field.Label ?? field.Field ?? automationId);

			// Preselect the stored option (null/unknown key selects nothing — a blank combo, like the
			// legacy combo whose SelectedIndex would be invalid for an out-of-range stored value).
			for (var i = 0; i < field.Options.Count; i++)
			{
				if (field.Options[i].Key == field.SelectedOptionKey)
				{
					combo.SelectedIndex = i;
					break;
				}
			}

			if (context?.EditContext != null && field.IsEditable)
			{
				var edit = context.EditContext;
				var save = context.Save;
				combo.SelectionChanged += (s, e) =>
				{
					if (!(combo.SelectedItem is RegionChoiceOption option))
						return;
					// Closed combo: the selected option's key is always a known index — the setter still
					// defensively rejects anything else, leaving the stored value untouched.
					if (edit.TrySetOption(field, option.Key))
						save?.Invoke();
				};
			}
			return combo;
		}

		// §19e — Integer (legacy IntegerSlice): a single-line numeric entry. The editor rejects
		// non-numeric keystrokes as you type (an optional leading '-' plus digits) AND restores the last
		// committed value when a commit is rejected (the composer's int-parsing setter returns false for
		// empty/overflow), so a non-numeric/empty value can never reach the int property. Staged through
		// the text seam (the composer registers the int-parse TextSetter for this field).
		private static Control BuildInteger(LexicalEditRegionField field, string automationId,
			RegionFieldControlContext context)
		{
			var initial = field.Values.Count > 0 ? field.Values[0].Value ?? string.Empty : string.Empty;
			var ws = field.Values.Count > 0 ? field.Values[0].WsAbbrev ?? string.Empty : string.Empty;
			var box = new TextBox
			{
				Text = initial,
				Padding = FwAvaloniaDensity.EditorPadding,
				MinHeight = 0,
				AcceptsReturn = false,
				IsReadOnly = context?.EditContext == null || !field.IsEditable,
				BorderThickness = new Thickness(0),
				Background = Brushes.Transparent,
				HorizontalAlignment = HorizontalAlignment.Left,
				MinWidth = 80
			};
			AutomationProperties.SetAutomationId(box, automationId);
			AutomationProperties.SetName(box, field.Label ?? field.Field ?? automationId);

			if (box.IsReadOnly)
				return box;

			// Reject any keystroke that would make the text non-integer (the legacy IntegerSlice's
			// effective contract: Convert.ToInt32 only accepts an optional sign + digits). A pasted or
			// programmatic non-integer is still caught by the setter's reject-and-restore below.
			box.AddHandler(Avalonia.Input.InputElement.TextInputEvent, (s, e) =>
			{
				if (string.IsNullOrEmpty(e.Text))
					return;
				foreach (var ch in e.Text)
				{
					if (char.IsDigit(ch))
						continue;
					// A single leading '-' is allowed (negative integers).
					if (ch == '-' && box.CaretIndex == 0 && !(box.Text ?? string.Empty).StartsWith("-"))
						continue;
					e.Handled = true;
					return;
				}
			}, Avalonia.Interactivity.RoutingStrategies.Tunnel);

			var lastCommitted = initial;
			Action commit = () =>
			{
				var text = box.Text ?? string.Empty;
				if (text == lastCommitted)
					return;
				// The int-parsing setter rejects empty/overflow/non-numeric (false); restore the last
				// committed value rather than present unsaved text as if it were stored.
				if (context.EditContext.TrySetText(field, ws, text))
				{
					lastCommitted = text;
					context.Save?.Invoke();
				}
				else
				{
					box.Text = lastCommitted;
				}
			};
			box.LostFocus += (s, e) => commit();
			box.KeyDown += (s, e) =>
			{
				if (e.Key == Avalonia.Input.Key.Enter)
				{
					commit();
					e.Handled = true;
				}
			};
			return box;
		}

		// §19e — Literal / "lit" slice (legacy MessageSlice): static read-only label text in the value
		// column (the label/message text IS the content). No edit affordance, no value binding.
		private static Control BuildLiteral(LexicalEditRegionField field, string automationId)
		{
			var text = field.Values.Count > 0 && !string.IsNullOrEmpty(field.Values[0].Value)
				? field.Values[0].Value
				: field.Label ?? field.Field ?? string.Empty;
			var block = new TextBlock
			{
				Text = text,
				TextWrapping = TextWrapping.Wrap,
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(block, automationId);
			AutomationProperties.SetName(block, text);
			return block;
		}

		// winforms-free-lexeme-editor.md D1: a plugin-claimed custom slice renders its plugin's own Avalonia
		// control in the value column, at the slice's real position. Guarded lane: a missing, null-returning,
		// or throwing factory degrades to the explicit unsupported row — never a crash, never a silently
		// blank row.
		private static Control BuildCustom(LexicalEditRegionField field, string automationId)
		{
			if (field.ControlFactory == null)
			{
				System.Diagnostics.Debug.WriteLine(
					$"Custom region field '{field.StableId}' has no control factory; rendering the unsupported row.");
				return BuildUnsupported(field, automationId);
			}

			try
			{
				var control = field.ControlFactory();
				if (control == null)
				{
					System.Diagnostics.Debug.WriteLine(
						$"Custom region field '{field.StableId}' factory returned null; rendering the unsupported row.");
					return BuildUnsupported(field, automationId);
				}

				// Plugins may carry their own automation identity; only fill in the row's when absent.
				if (string.IsNullOrEmpty(AutomationProperties.GetAutomationId(control)))
					AutomationProperties.SetAutomationId(control, automationId);
				return control;
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(
					$"Custom region field '{field.StableId}' factory threw; rendering the unsupported row: {e}");
				return BuildUnsupported(field, automationId);
			}
		}

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
