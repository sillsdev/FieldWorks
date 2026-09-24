// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// One of a reversal entry's forms in a writing system other than its index's own.
	/// </summary>
	public sealed class DetailReversalAlternative
	{
		/// <summary>Creates the alternative.</summary>
		/// <param name="wsAbbrev">The writing system's abbreviation; empty shows the form
		/// alone.</param>
		/// <param name="text">The form.</param>
		/// <param name="fontFamily">The writing system's font; null or empty keeps the
		/// default.</param>
		public DetailReversalAlternative(string wsAbbrev, string text, string fontFamily)
		{
			WsAbbrev = wsAbbrev;
			Text = text;
			FontFamily = fontFamily;
		}

		public string WsAbbrev { get; }

		public string Text { get; }

		public string FontFamily { get; }
	}

	/// <summary>
	/// One row of a <see cref="DetailReversalGroup"/>: an entry linked to the sense, or the
	/// group's add row.
	/// </summary>
	public sealed class DetailReversalRow
	{
		/// <summary>Creates the row.</summary>
		/// <param name="rowKey">The opaque identity the edit context issued for this row.</param>
		/// <param name="text">The entry's form, a subentry's ancestors colon-joined before
		/// it; empty for an add row.</param>
		/// <param name="isAddSlot">True for the group's add row.</param>
		/// <param name="otherWsForms">The entry's forms in other writing systems; null means
		/// none.</param>
		public DetailReversalRow(string rowKey, string text, bool isAddSlot,
			IReadOnlyList<DetailReversalAlternative> otherWsForms = null)
		{
			RowKey = rowKey;
			Text = text ?? string.Empty;
			IsAddSlot = isAddSlot;
			OtherWsForms = otherWsForms ?? Array.Empty<DetailReversalAlternative>();
		}

		public string RowKey { get; }

		public string Text { get; }

		public bool IsAddSlot { get; }

		/// <summary>The entry's forms in other writing systems, in display order; never
		/// null.</summary>
		public IReadOnlyList<DetailReversalAlternative> OtherWsForms { get; }
	}

	/// <summary>
	/// One reversal index's rows: the sense's entries in that index, followed by its add row.
	/// </summary>
	public sealed class DetailReversalGroup
	{
		/// <summary>Creates the group.</summary>
		/// <param name="wsTag">The index's writing system tag.</param>
		/// <param name="wsAbbrev">The index's writing system abbreviation, the group's
		/// label.</param>
		/// <param name="fontFamily">The writing system's font; null or empty keeps the
		/// default.</param>
		/// <param name="rightToLeft">Whether the writing system is right-to-left.</param>
		/// <param name="rows">The group's rows in display order; null means none.</param>
		public DetailReversalGroup(string wsTag, string wsAbbrev, string fontFamily, bool rightToLeft,
			IReadOnlyList<DetailReversalRow> rows)
		{
			WsTag = wsTag;
			WsAbbrev = wsAbbrev;
			FontFamily = fontFamily;
			RightToLeft = rightToLeft;
			Rows = rows ?? Array.Empty<DetailReversalRow>();
		}

		public string WsTag { get; }

		public string WsAbbrev { get; }

		public string FontFamily { get; }

		public bool RightToLeft { get; }

		public IReadOnlyList<DetailReversalRow> Rows { get; }
	}

	/// <summary>
	/// FieldWorks-owned editor for a sense's reversal entries. Each reversal index is a group
	/// labeled with its writing system abbreviation: one wrapping line of editable slots, one
	/// per linked entry and a final empty one for adding, with a bar between neighboring
	/// slots. A row commits once, when it loses focus,
	/// through <see cref="IReversalEntryEditing"/>; the rows are a snapshot that the host
	/// rebuilds after its save. Right-clicking a row offers "Show in Reversal Index", and
	/// Ctrl+click runs it directly. An edit context without
	/// <see cref="IReversalEntryEditing"/> shows the rows read-only.
	/// </summary>
	public sealed class FwReversalEntriesField : StackPanel, IDisposable
	{
		private readonly List<Action> _teardown = new List<Action>();
		private readonly IReversalEntryEditing _editing;
		private readonly Action<string> _navigationRequested;
		private bool _disposed;

		/// <summary>Builds the editor.</summary>
		/// <param name="label">The field label, used in accessible names.</param>
		/// <param name="automationId">The field's automation id, the prefix of every row's
		/// id.</param>
		/// <param name="groups">The groups in display order; null means none.</param>
		/// <param name="editContext">The detail view's edit context; null shows the rows
		/// read-only.</param>
		/// <param name="writingSystemFocused">Called with a group's writing system tag when
		/// one of its rows gains focus; null disables that.</param>
		/// <param name="navigationRequested">Called with a row key to show that row's entry
		/// in the Reversal Index tool; null leaves out the jump.</param>
		/// <param name="wsAbbrevColumnWidth">Width of the abbreviation column; null uses the
		/// default.</param>
		public FwReversalEntriesField(string label, string automationId,
			IReadOnlyList<DetailReversalGroup> groups, IDetailEditContext editContext,
			Action<string> writingSystemFocused = null, Action<string> navigationRequested = null,
			double? wsAbbrevColumnWidth = null)
		{
			Spacing = FwAvaloniaDensity.RowSpacing;
			var name = label ?? automationId;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, name);
			_editing = editContext as IReversalEntryEditing;
			_navigationRequested = _editing == null ? null : navigationRequested;

			var abbrevWidth = wsAbbrevColumnWidth ?? FwAvaloniaDensity.WsAbbrevWidth;
			foreach (var group in groups ?? Array.Empty<DetailReversalGroup>())
				Children.Add(CreateGroup(name, automationId, group, writingSystemFocused, abbrevWidth));
		}

		private Control CreateGroup(string label, string automationId, DetailReversalGroup group,
			Action<string> writingSystemFocused, double abbrevWidth)
		{
			var groupId = automationId + "." + group.WsTag;
			// The group's entries run together on one wrapping line, a bar between each pair,
			// the add row last.
			var rows = new WrapPanel
			{
				Orientation = Orientation.Horizontal,
				Background = FwAvaloniaDensity.TransparentBrush,
				FlowDirection = group.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
			};
			AutomationProperties.SetAutomationId(rows, groupId);
			TextBox addBox = null;
			for (var i = 0; i < group.Rows.Count; i++)
			{
				if (i > 0)
					rows.Children.Add(FwReferenceVectorField.CreateSeparatorBar());
				var row = group.Rows[i];
				rows.Children.Add(CreateRow(label, groupId, group, row, i, writingSystemFocused, out var box));
				if (row.IsAddSlot)
					addBox = box;
			}

			if (_editing != null && addBox != null)
			{
				// An empty add row is barely wider than its caret, so a click anywhere on the
				// group's free space starts typing there.
				EventHandler<PointerPressedEventArgs> pressed = (s, e) =>
				{
					if (!ReferenceEquals(e.Source, rows))
						return;
					addBox.Focus();
					e.Handled = true;
				};
				rows.PointerPressed += pressed;
				_teardown.Add(() => rows.PointerPressed -= pressed);
			}

			var abbrev = FwMultiWsTextField.CreateWsAbbrev(group.WsAbbrev, abbrevWidth);
			var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
			Grid.SetColumn(abbrev, 0);
			Grid.SetColumn(rows, 1);
			grid.Children.Add(abbrev);
			grid.Children.Add(rows);
			return grid;
		}

		private Control CreateRow(string label, string groupId, DetailReversalGroup group,
			DetailReversalRow row, int index, Action<string> writingSystemFocused, out TextBox box)
		{
			var rowId = row.IsAddSlot ? groupId + ".Add" : groupId + "." + index;
			var editor = new TextBox
			{
				Text = row.Text,
				Padding = FwAvaloniaDensity.EditorPadding,
				MinHeight = 0,
				AcceptsReturn = false,
				IsReadOnly = _editing == null,
				FlowDirection = group.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
				BorderThickness = new Thickness(0),
				Background = FwAvaloniaDensity.TransparentBrush,
				TextWrapping = TextWrapping.Wrap
				TextWrapping = TextWrapping.NoWrap
			};
			box = editor;
			if (!string.IsNullOrEmpty(group.FontFamily))
				editor.FontFamily = new FontFamily(group.FontFamily);
			AutomationProperties.SetAutomationId(editor, rowId);
			AutomationProperties.SetName(editor, row.IsAddSlot
				? FwAvaloniaStrings.ReversalAddEntryName(label, group.WsAbbrev)
				: label + " " + group.WsAbbrev);

			// Tracks what the model holds for this row, so an unchanged row never stages.
			var committed = row.Text;
			Action commit = () =>
			{
				var text = editor.Text ?? string.Empty;
				if (_editing != null && text != committed && _editing.TryCommitRow(row.RowKey, text))
					committed = text;
			};

			if (_editing != null)
			{
				// Runs before the host's own focus-loss save, which bubbles up from this box.
				EventHandler<RoutedEventArgs> lost = (s, e) => commit();
				editor.LostFocus += lost;
				_teardown.Add(() => editor.LostFocus -= lost);
			}

			if (writingSystemFocused != null && !string.IsNullOrEmpty(group.WsTag))
			{
				EventHandler<GotFocusEventArgs> got = (s, e) => writingSystemFocused(group.WsTag);
				editor.GotFocus += got;
				_teardown.Add(() => editor.GotFocus -= got);
			}

			if (_navigationRequested != null)
				WireNavigation(editor, row, commit);

			var suffix = CreateOtherWsSuffix(row, rowId + ".OtherWs");
			if (suffix == null)
				return editor;
			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Background = FwAvaloniaDensity.TransparentBrush
			};
			panel.Children.Add(editor);
			panel.Children.Add(suffix);
			return panel;
		}

		// The jump commits the row first, so a form typed into the add row exists when shown.
		private void WireNavigation(TextBox box, DetailReversalRow row, Action commit)
		{
			Action jump = () =>
			{
				commit();
				_navigationRequested(row.RowKey);
			};

			EventHandler<PointerPressedEventArgs> pressed = (s, e) =>
			{
				if (string.IsNullOrEmpty(box.Text)
					|| !e.KeyModifiers.HasFlag(KeyModifiers.Control)
					|| !e.GetCurrentPoint(box).Properties.IsLeftButtonPressed)
				{
					return;
				}
				jump();
				e.Handled = true;
			};
			box.AddHandler(InputElement.PointerPressedEvent, pressed, RoutingStrategies.Tunnel);

			var show = new MenuItem { Header = FwAvaloniaStrings.ReversalShowInReversalIndex };
			EventHandler<RoutedEventArgs> click = (s, e) => jump();
			show.Click += click;
			var menu = new MenuFlyout { Items = { show } };
			EventHandler opening = (s, e) => show.IsEnabled = !string.IsNullOrEmpty(box.Text);
			menu.Opening += opening;
			var previousMenu = box.ContextFlyout;
			box.ContextFlyout = menu;
			var popupTeardown = PopupReporting.Wire(menu);
			_teardown.Add(() =>
			{
				box.RemoveHandler(InputElement.PointerPressedEvent, pressed);
				show.Click -= click;
				menu.Opening -= opening;
				popupTeardown();
				box.ContextFlyout = previousMenu;
			});
		}

		// Read-only, in display order: [abbrev form, abbrev form]. Null when the entry has none.
		private static Control CreateOtherWsSuffix(DetailReversalRow row, string automationId)
		{
			if (row.OtherWsForms.Count == 0)
				return null;

			var block = new TextBlock
			{
				TextWrapping = TextWrapping.Wrap,
				VerticalAlignment = VerticalAlignment.Top,
				Padding = FwAvaloniaDensity.EditorPadding,
				FlowDirection = FlowDirection.LeftToRight
			};
			block.Inlines.Add(new Run("["));
			for (var i = 0; i < row.OtherWsForms.Count; i++)
			{
				var alternative = row.OtherWsForms[i];
				if (i > 0)
					block.Inlines.Add(new Run(", "));
				if (!string.IsNullOrEmpty(alternative.WsAbbrev))
				{
					block.Inlines.Add(new Run(alternative.WsAbbrev)
					{
						FontSize = FwAvaloniaDensity.WsAbbrevFontSize,
						Foreground = FwAvaloniaDensity.WsAbbrevBrush
					});
					block.Inlines.Add(new Run(" "));
				}
				var form = new Run(alternative.Text ?? string.Empty);
				if (!string.IsNullOrEmpty(alternative.FontFamily))
					form.FontFamily = new FontFamily(alternative.FontFamily);
				block.Inlines.Add(form);
			}
			block.Inlines.Add(new Run("]"));
			AutomationProperties.SetAutomationId(block, automationId);
			return block;
		}

		/// <summary>
		/// The count of still-attached handler teardowns; zero after <see cref="Dispose"/>.
		/// </summary>
		public int AttachedHandlerCount => _teardown.Count;

		/// <summary>Detaches every wired handler and drops the row menus. Idempotent.</summary>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			foreach (var detach in _teardown)
				detach();
			_teardown.Clear();
		}
	}
}
