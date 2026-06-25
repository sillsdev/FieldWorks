// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 3.2) — the phonological-environment string editor (legacy
	/// <c>PhEnvStrRepresentationSlice</c> parity): an editable single-line entry for an
	/// <c>IPhEnvironment.StringRepresentation</c> (e.g. <c>/ [C] _ #</c>) plus an insert toolbar for the
	/// boundary/optional/slash/underscore glyphs. On commit the string is validated through the host's
	/// <c>PhonEnvRecognizer</c> seam; an invalid string is rejected (the box restores the last committed
	/// value and shows the error) so a malformed environment can never be stored.
	///
	/// <para>LCModel-free (design Decision 3 + engine-isolation audit): it binds to a string and an
	/// <see cref="IPhEnvironmentCommandSink"/>; the xWorks plugin owns the LCModel read/write + validator.
	/// Read-only when no sink is wired. // PARITY: the natural-class insert chooser is deferred (the literal
	/// insert toolbar ships; NC insertion reuses the shared chooser kit in a follow-up).</para>
	/// </summary>
	public sealed class PhEnvironmentEditor : Border
	{
		/// <summary>Stable automation id for the environment editor surface.</summary>
		public const string EnvironmentAutomationId = "PhEnvironmentEditor";

		private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xB2, 0x22, 0x22));

		private readonly TextBox m_box;
		private readonly TextBlock m_error;
		private string m_committed;

		/// <summary>The LCModel-side validate/commit seam. Null = read-only.</summary>
		public IPhEnvironmentCommandSink Sink { get; set; }

		/// <summary>True when a sink is wired (the field is editable).</summary>
		public bool IsEditable => Sink != null;

		public PhEnvironmentEditor() : this(string.Empty, null) { }

		public PhEnvironmentEditor(string representation, IPhEnvironmentCommandSink sink)
		{
			FwSurfaceStyles.Apply(this);
			Sink = sink;
			m_committed = representation ?? string.Empty;
			Background = Brushes.White;
			Padding = new Thickness(6, 3, 6, 3);

			m_box = new TextBox
			{
				Text = m_committed,
				MinWidth = 220,
				IsReadOnly = sink == null,
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(m_box, EnvironmentAutomationId);

			m_error = new TextBlock
			{
				Foreground = ErrorBrush,
				IsVisible = false,
				FontSize = FwSurfaceStyles.SurfaceFontSize,
				Margin = new Thickness(0, 2, 0, 0)
			};

			var root = new StackPanel { Orientation = Orientation.Vertical };
			if (sink != null)
				root.Children.Add(BuildInsertToolbar());
			root.Children.Add(m_box);
			root.Children.Add(m_error);
			Child = root;

			if (sink != null)
			{
				m_box.LostFocus += (s, e) => CommitOrReject();
				m_box.KeyDown += (s, e) =>
				{
					if (e.Key == Key.Enter)
					{
						CommitOrReject();
						e.Handled = true;
					}
					else if (e.Key == Key.Escape)
					{
						RestoreCommitted();
						e.Handled = true;
					}
				};
			}
		}

		/// <summary>The last committed string representation (domain truth after a successful commit).</summary>
		public string CommittedText => m_committed;

		/// <summary>Programmatic commit (used by the toolbar and tests); validates then stages, or rejects.</summary>
		public bool TryCommit() => CommitOrReject();

		private bool CommitOrReject()
		{
			var text = m_box.Text ?? string.Empty;
			if (text == m_committed)
			{
				ClearError();
				return true;
			}
			if (Sink == null || !Sink.Validate(text))
			{
				ShowError(text);
				RestoreCommitted();
				return false;
			}
			if (!Sink.Commit(text))
			{
				ShowError(text);
				RestoreCommitted();
				return false;
			}
			m_committed = text;
			ClearError();
			return true;
		}

		private void RestoreCommitted() => m_box.Text = m_committed;

		private void ShowError(string attempted)
		{
			// TODO localization: prototype hard-coded string — route through the LocalizationManager catalog
			// (reuse a Morphology environment-error id) before product use.
			m_error.Text = $"'{attempted}' is not a valid environment.";
			m_error.IsVisible = true;
		}

		private void ClearError()
		{
			m_error.IsVisible = false;
			m_error.Text = string.Empty;
		}

		// The literal-insert toolbar: word boundary, optional brackets, the segment slash, the environment bar.
		private Control BuildInsertToolbar()
		{
			var bar = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(0, 0, 0, 3)
			};
			foreach (var glyph in new[] { "#", "[", "]", "/", "_" })
				bar.Children.Add(InsertButton(glyph));
			return bar;
		}

		private Control InsertButton(string glyph)
		{
			var button = new Button
			{
				Content = glyph,
				Padding = new Thickness(6, 1, 6, 1),
				Margin = new Thickness(0, 0, 3, 0),
				MinWidth = 22
			};
			AutomationProperties.SetAutomationId(button, "PhEnvInsert-" + glyph);
			button.Click += (s, e) =>
			{
				var caret = Math.Max(0, Math.Min(m_box.CaretIndex, (m_box.Text ?? string.Empty).Length));
				m_box.Text = (m_box.Text ?? string.Empty).Insert(caret, glyph);
				m_box.CaretIndex = caret + glyph.Length;
				m_box.Focus();
			};
			return button;
		}
	}

	/// <summary>
	/// avalonia-rule-formula-editor (task 3.2) — the LCModel-free validate/commit seam for the environment
	/// editor. The xWorks plugin implements it: <see cref="Validate"/> runs the phonological-environment
	/// recognizer; <see cref="Commit"/> writes the string representation in one fenced undo step.
	/// </summary>
	public interface IPhEnvironmentCommandSink
	{
		/// <summary>True when <paramref name="representation"/> is a well-formed environment.</summary>
		bool Validate(string representation);

		/// <summary>Persist <paramref name="representation"/> as the environment's string representation.</summary>
		bool Commit(string representation);
	}
}
