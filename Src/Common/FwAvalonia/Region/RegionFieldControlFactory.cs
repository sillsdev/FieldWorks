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
			bool showWritingSystemAbbreviation = true)
		{
			EditContext = editContext;
			WritingSystemFocused = writingSystemFocused;
			MenuRequested = menuRequested;
			LinkRequested = linkRequested;
			Clipboard = clipboard;
			Save = save;
			ShowWritingSystemAbbreviation = showWritingSystemAbbreviation;
		}

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
				case RegionFieldKind.Chooser:
					return new FwChooserField(field, automationId, context.EditContext, context.LinkRequested);
				case RegionFieldKind.Boolean:
					return BuildBoolean(field, automationId, context);
				case RegionFieldKind.Date:
					return BuildDate(field, automationId, context);
				case RegionFieldKind.Image:
					return BuildImage(field, automationId);
				case RegionFieldKind.Command:
					return BuildCommand(field, automationId);
				case RegionFieldKind.Unsupported:
					return BuildUnsupported(field, automationId);
				default:
					return new FwMultiWsTextField(field, automationId, context.EditContext,
						context.WritingSystemFocused, context.MenuRequested, context.Clipboard,
						context.ShowWritingSystemAbbreviation);
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
				catch (Exception e)
				{
					// Graceful degrade (legacy PictureSlice shows the path for a bad image), but never
					// silently: record what failed to load and why.
					System.Diagnostics.Debug.WriteLine(
						$"Picture field '{field.StableId}' failed to load image '{path}': {e.Message}");
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
				MinWidth = 130
			};
			AutomationProperties.SetAutomationId(box, automationId);
			AutomationProperties.SetName(box, field.Label ?? field.Field ?? automationId);

			if (box.IsReadOnly)
				return box;

			var lastCommitted = initial;
			Action commit = () =>
			{
				var text = box.Text ?? string.Empty;
				if (text == lastCommitted)
					return;
				// The setter parses+stages; a failed parse leaves the stored value untouched, so the
				// box restores what the domain actually holds rather than presenting bad text as saved.
				if (context.EditContext.TrySetOption(field, text))
				{
					lastCommitted = text;
					context.Save?.Invoke();
				}
				else
				{
					box.Text = lastCommitted;
				}
			};
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
			return box;
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
