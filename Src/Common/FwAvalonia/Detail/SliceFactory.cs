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

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// The small bundle of (all-nullable) collaborators a <see cref="DetailFieldKind"/> editor needs,
	/// passed to <see cref="SliceFactory.Build"/> so the SAME field→control dispatch serves
	/// every hosting surface (today the detail-pane detail view, <c>DataTree.BuildEditor</c>;
	/// any future in-cell editor passes only the collaborators it has). Every member is optional: a null
	/// edit context yields read-only display; a null callback simply disables that affordance.
	/// One switch, so new kinds live in one place.
	/// </summary>
	public sealed class SliceFactoryContext
	{
		public SliceFactoryContext(
			IDetailEditContext editContext = null,
			Action<string> writingSystemFocused = null,
			Action<DetailMenuRequest> menuRequested = null,
			Action<DetailLinkRequest> linkRequested = null,
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
		public IDetailEditContext EditContext { get; }

		/// <summary>Per-WS keyboard activation callback for text fields (null → no keyboard switch).</summary>
		public Action<string> WritingSystemFocused { get; }

		/// <summary>Right-click slice/section menu callback (null on surfaces without a slice menu).</summary>
		public Action<DetailMenuRequest> MenuRequested { get; }

		/// <summary>Hyperlink follow callback for choosers/vectors (null → no link affordance).</summary>
		public Action<DetailLinkRequest> LinkRequested { get; }

		/// <summary>Clipboard seam for text fields (null → framework default).</summary>
		public IFwClipboard Clipboard { get; }

		/// <summary>
		/// Validation-gated commit invoked when a reference-vector add/remove gesture completes (the detail
		/// pane's autosave). Null on surfaces that drive commit themselves (an in-cell editor commits on
		/// Enter/Tab through its own active-cell session), in which case the vector field just stages.
		/// </summary>
		public Action Save { get; }

		/// <summary>
		/// Whether a multi-WS text field shows its per-WS abbreviation gutter. The detail pane shows it;
		/// a dense in-cell editor suppresses it (matching the legacy in-cell editor).
		/// </summary>
		public bool ShowWritingSystemAbbreviation { get; }
	}

	/// <summary>
	/// The single <see cref="DetailFieldKind"/>→Avalonia-control dispatch. The detail
	/// pane (<c>DataTree.BuildEditor</c>, all 7 kinds) and the browse in-cell editor
	/// (<c>EditableCellHost.Activate</c>, a 2-kind Chooser/Text subset) both route here rather than
	/// hand-rolling their own dispatch, so adding a kind (or changing how a kind is built) happens once.
	/// The factory is pure (static) — all per-surface variation arrives through the
	/// <see cref="SliceFactoryContext"/>.
	/// </summary>
	public static class SliceFactory
	{
		public static Control Build(DetailField field, string automationId,
			SliceFactoryContext context)
		{
			if (field == null) throw new ArgumentNullException(nameof(field));
			context = context ?? new SliceFactoryContext();

			switch (field.Kind)
			{
				case DetailFieldKind.Custom:
					return BuildCustom(field, automationId);
				case DetailFieldKind.ReferenceVector:
					// Reference add/remove gestures commit immediately (legacy chooser-dialog behavior): the
					// staged session would otherwise sit open — LCModel broadcasts PropChanged only at
					// EndUndoTask and the row's Items are a compose-time snapshot, so the user would see no
					// change. The gesture-completed callback runs the SAME validation-gated save the
					// focus-loss autosave uses, whose re-show rebuilds the row from domain truth. A surface
					// that owns its own commit (an in-cell editor) passes a null Save and just stages.
					return new FwReferenceVectorField(field, automationId, context.EditContext,
						context.EditContext == null ? null : context.Save, context.LinkRequested);
				case DetailFieldKind.StructuredText:
					// An editable multi-paragraph StText field. Per-paragraph text edits stage and
					// ride the focus-loss autosave; structural gestures (add/delete/style) commit
					// immediately through the SAME validation-gated Save the reference vector uses, whose
					// re-show rebuilds the paragraph rows from domain truth (the Paragraphs list is a
					// compose-time snapshot). A surface owning its own commit passes a null Save.
					return new FwStructuredTextField(field, automationId, context.EditContext,
						context.WritingSystemFocused,
						context.EditContext == null ? null : context.Save, context.Clipboard);
				case DetailFieldKind.Chooser:
					return new FwChooserField(field, automationId, context.EditContext, context.LinkRequested);
				case DetailFieldKind.Literal:
					return BuildLiteral(field, automationId);
				case DetailFieldKind.Unsupported:
					return BuildUnsupported(field, automationId);
				default:
					return new FwMultiWsTextField(field, automationId, context.EditContext,
						context.WritingSystemFocused, context.MenuRequested, context.Clipboard,
						context.ShowWritingSystemAbbreviation, context.Save);
			}
		}

		// Literal / "lit" slice (legacy MessageSlice): static read-only label text in the value
		// column (the label/message text IS the content). No edit affordance, no value binding.
		private static Control BuildLiteral(DetailField field, string automationId)
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

		// A plugin-claimed custom slice renders its plugin's own Avalonia
		// control in the value column, at the slice's real position. Null guard: a missing, null-returning,
		// or throwing factory degrades to the explicit unsupported row — never a crash, never a silently
		// blank row.
		private static Control BuildCustom(DetailField field, string automationId)
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

		private static Control BuildUnsupported(DetailField field, string automationId)
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
