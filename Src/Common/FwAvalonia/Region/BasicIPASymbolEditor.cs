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
	/// avalonia-rule-formula-editor (task 3.1) — the Basic IPA Symbol editor (legacy <c>BasicIPASymbolSlice</c>
	/// parity): an editable single-line entry for a phoneme's <c>BasicIPASymbol</c>. LCModel-free: it binds to
	/// a string + an <see cref="IBasicIpaSymbolCommandSink"/>; the xWorks plugin owns the read/write.
	///
	/// <para>// PARITY: derive-on-commit (auto-filling the phoneme Description + Features from
	/// <c>BasicIPAInfo.xml</c> when the symbol changes) is DEFERRED — this ships the editable symbol field;
	/// the derive is an auto-population convenience to be ported by extracting the legacy slice's
	/// <c>SetDescriptionBasedOnIPA</c>/<c>SetFeaturesBasedOnIPA</c> into a reusable domain helper. Read-only
	/// when no sink is wired.</para>
	/// </summary>
	public sealed class BasicIPASymbolEditor : Border
	{
		/// <summary>Stable automation id for the symbol entry.</summary>
		public const string BasicIpaSymbolAutomationId = "BasicIPASymbolEditor";

		private readonly TextBox m_box;
		private string m_committed;

		/// <summary>The LCModel-side write seam. Null = read-only.</summary>
		public IBasicIpaSymbolCommandSink Sink { get; set; }

		/// <summary>True when a sink is wired (the field is editable).</summary>
		public bool IsEditable => Sink != null;

		/// <summary>The last committed symbol (domain truth after a successful commit).</summary>
		public string CommittedText => m_committed;

		public BasicIPASymbolEditor() : this(string.Empty, null) { }

		public BasicIPASymbolEditor(string symbol, IBasicIpaSymbolCommandSink sink)
		{
			FwSurfaceStyles.Apply(this);
			Sink = sink;
			m_committed = symbol ?? string.Empty;
			Background = Brushes.White;
			Padding = new Thickness(6, 3, 6, 3);

			m_box = new TextBox
			{
				Text = m_committed,
				MinWidth = 120,
				IsReadOnly = sink == null,
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(m_box, BasicIpaSymbolAutomationId);
			Child = m_box;

			if (sink != null)
			{
				m_box.LostFocus += (s, e) => Commit();
				m_box.KeyDown += (s, e) =>
				{
					if (e.Key == Key.Enter) { Commit(); e.Handled = true; }
					else if (e.Key == Key.Escape) { m_box.Text = m_committed; e.Handled = true; }
				};
			}
		}

		/// <summary>Programmatic commit (used by tests). Writes the symbol through the sink.</summary>
		public bool TryCommit() => Commit();

		private bool Commit()
		{
			var text = m_box.Text ?? string.Empty;
			if (text == m_committed)
				return true;
			if (Sink == null || !Sink.Commit(text))
			{
				m_box.Text = m_committed;
				return false;
			}
			m_committed = text;
			return true;
		}
	}

	/// <summary>
	/// avalonia-rule-formula-editor (task 3.1) — the LCModel-free write seam for the Basic IPA Symbol editor.
	/// The xWorks plugin implements <see cref="Commit"/> to write the phoneme's <c>BasicIPASymbol</c> (and,
	/// in a follow-up, run the derive-on-commit) in one fenced undo step.
	/// </summary>
	public interface IBasicIpaSymbolCommandSink
	{
		/// <summary>Persist <paramref name="symbol"/> as the phoneme's basic IPA symbol.</summary>
		bool Commit(string symbol);
	}
}
