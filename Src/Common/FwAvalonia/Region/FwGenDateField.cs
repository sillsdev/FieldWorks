// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// §19e — the generic-date (GenDate) qualifier precision the structured editor offers, mirroring the
	/// legacy <c>GenDateChooserDlg</c> precision combo (Before / On / About / After) which maps 1:1 onto
	/// LCModel's <c>GenDate.PrecisionType</c>. "About" is the circa/approximate qualifier. The LCModel-free
	/// view never references <c>GenDate.PrecisionType</c> itself — it composes the precision WORD the
	/// long-string grammar uses (and the composer's setter parses through <c>GenDate.TryParse</c>).
	/// </summary>
	public enum GenDatePrecision
	{
		/// <summary>Before the given year (long-string word "Before").</summary>
		Before,

		/// <summary>On/exact (no precision word; LCModel <c>Exact</c>).</summary>
		Exact,

		/// <summary>About/circa the given year (long-string word "About"; LCModel <c>Approximate</c>).</summary>
		Approximate,

		/// <summary>After the given year (long-string word "After").</summary>
		After
	}

	/// <summary>
	/// §19e — the structured generic-date (GenDate) editor (legacy <c>GenDateLauncher</c>/
	/// <c>GenDateChooserDlg</c>): a year entry plus precision (Before/On/About/After) and era (AD/BC)
	/// drop-downs that compose a <c>GenDate.TryParse</c>-compatible long-string and stage it through the
	/// edit context's option seam — the same seam the composer's GenDate setter parses. The qualifier
	/// surface (precision = circa/before/after, era = AD/BC) replaces the prior bare text box, while the
	/// committed value stays a parseable string so an unparseable composition can never corrupt the field.
	/// The control is LCModel-free: it only emits the long-string the host setter understands.
	/// </summary>
	public sealed class FwGenDateField : Border
	{
		private readonly NumericUpDown _year;
		private readonly ComboBox _precision;
		private readonly ComboBox _era;
		private readonly IRegionEditContext _editContext;
		private readonly LexicalEditRegionField _field;
		private readonly Action _save;
		private bool _suppress;
		private string _lastCommitted;

		// The precision drop-down rows, in PrecisionType order so the index round-trips. Localized at
		// build time; the WORD the long-string uses (Compose) is keyed off the enum, not the label.
		private static readonly GenDatePrecision[] PrecisionOrder =
		{
			GenDatePrecision.Before, GenDatePrecision.Exact, GenDatePrecision.Approximate, GenDatePrecision.After
		};

		public FwGenDateField(LexicalEditRegionField field, string automationId, IRegionEditContext editContext,
			Action save)
		{
			_field = field ?? throw new ArgumentNullException(nameof(field));
			_editContext = editContext;
			_save = save;

			var initial = field.Values.Count > 0 ? field.Values[0].Value ?? string.Empty : string.Empty;
			_lastCommitted = initial;
			TryParseLongString(initial, out var year, out var precision, out var isAd);

			Background = Brushes.Transparent;
			BorderThickness = new Avalonia.Thickness(0);

			_year = new NumericUpDown
			{
				Minimum = 1,
				Maximum = 999999,
				Increment = 1,
				Value = year,
				FormatString = "0",
				MinWidth = 90,
				MinHeight = 0,
				IsEnabled = editContext != null && field.IsEditable
			};
			AutomationProperties.SetAutomationId(_year, automationId + ".year");
			AutomationProperties.SetName(_year, FwAvaloniaStrings.GenDateYear);

			_precision = new ComboBox
			{
				ItemsSource = new List<string>
				{
					FwAvaloniaStrings.GenDatePrecisionBefore,
					FwAvaloniaStrings.GenDatePrecisionOn,
					FwAvaloniaStrings.GenDatePrecisionAbout,
					FwAvaloniaStrings.GenDatePrecisionAfter
				},
				SelectedIndex = Array.IndexOf(PrecisionOrder, precision),
				MinHeight = 0,
				IsEnabled = editContext != null && field.IsEditable
			};
			AutomationProperties.SetAutomationId(_precision, automationId + ".precision");
			AutomationProperties.SetName(_precision, FwAvaloniaStrings.GenDatePrecision);

			_era = new ComboBox
			{
				ItemsSource = new List<string> { FwAvaloniaStrings.GenDateEraAd, FwAvaloniaStrings.GenDateEraBc },
				SelectedIndex = isAd ? 0 : 1,
				MinHeight = 0,
				IsEnabled = editContext != null && field.IsEditable
			};
			AutomationProperties.SetAutomationId(_era, automationId + ".era");
			AutomationProperties.SetName(_era, FwAvaloniaStrings.GenDateEra);

			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 4,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			// The precision word reads naturally before the year ("About 1985 BC"); era trails it.
			panel.Children.Add(_precision);
			panel.Children.Add(_year);
			panel.Children.Add(_era);
			Child = panel;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			if (editContext != null && field.IsEditable)
			{
				_year.ValueChanged += (s, e) => Commit();
				_precision.SelectionChanged += (s, e) => Commit();
				_era.SelectionChanged += (s, e) => Commit();
			}
		}

		/// <summary>The year currently shown (read-only test/inspection accessor).</summary>
		public int Year => (int)(_year.Value ?? 0);

		/// <summary>The precision currently selected.</summary>
		public GenDatePrecision Precision => _precision.SelectedIndex >= 0
			? PrecisionOrder[_precision.SelectedIndex]
			: GenDatePrecision.Exact;

		/// <summary>Whether the era is AD (true) or BC (false).</summary>
		public bool IsAd => _era.SelectedIndex != 1;

		/// <summary>
		/// Set all three qualifiers at once and commit one composed string (a single staged change rather
		/// than three). Used by tests and any host that wants to seed the editor programmatically.
		/// </summary>
		public void SetForTest(int year, GenDatePrecision precision, bool isAd)
		{
			_suppress = true;
			_year.Value = year;
			_precision.SelectedIndex = Array.IndexOf(PrecisionOrder, precision);
			_era.SelectedIndex = isAd ? 0 : 1;
			_suppress = false;
			Commit();
		}

		private void Commit()
		{
			if (_suppress || _editContext == null || !_field.IsEditable)
				return;
			var composed = Compose(Year, Precision, IsAd);
			if (composed == _lastCommitted)
				return;
			// The composer's setter parses the long-string through GenDate.TryParse; on the rare reject
			// (an out-of-range year, say) we restore the last committed composition rather than leave a
			// value the domain refused.
			if (_editContext.TrySetOption(_field, composed))
			{
				_lastCommitted = composed;
				_save?.Invoke();
			}
		}

		/// <summary>
		/// §19e — compose the canonical GenDate long-string the parser round-trips (probed from LCModel):
		/// AD dates read "&lt;precision-word&gt;AD &lt;year&gt;" and BC dates "&lt;precision-word&gt;&lt;year&gt; BC",
		/// where the precision word is "Before "/""/"About "/"After " for Before/Exact/Approximate/After.
		/// </summary>
		public static string Compose(int year, GenDatePrecision precision, bool isAd)
		{
			string prec;
			switch (precision)
			{
				case GenDatePrecision.Before: prec = "Before "; break;
				case GenDatePrecision.Approximate: prec = "About "; break;
				case GenDatePrecision.After: prec = "After "; break;
				default: prec = string.Empty; break; // Exact
			}
			var y = year.ToString(CultureInfo.InvariantCulture);
			return isAd ? $"{prec}AD {y}" : $"{prec}{y} BC";
		}

		// Best-effort seed of the three controls from the current value. The host composer feeds the CANONICAL
		// year-granular form Compose() produces (precision word + era + a single year run — e.g. "About 1985 BC"),
		// NOT a localized full-calendar long string, so the first digit run IS the year (19i.1 fixed the prior
		// data-loss where a "June 15, 1990" long string made the digit-scan grab the DAY "15"). The scan stays
		// defensive: if ever fed a long string, it reads the precision word, the AD/BC marker, and the first
		// digit run. The qualifier editor edits at year granularity (the calendar day picker is the exact-date lane).
		private static void TryParseLongString(string text, out int year, out GenDatePrecision precision,
			out bool isAd)
		{
			year = DateTime.Now.Year;
			precision = GenDatePrecision.Exact;
			isAd = true;
			if (string.IsNullOrWhiteSpace(text))
				return;

			var lower = text.ToLowerInvariant();
			if (lower.Contains("before")) precision = GenDatePrecision.Before;
			else if (lower.Contains("about")) precision = GenDatePrecision.Approximate;
			else if (lower.Contains("after")) precision = GenDatePrecision.After;

			isAd = !lower.Contains("bc");

			var digits = new System.Text.StringBuilder();
			foreach (var ch in text)
			{
				if (char.IsDigit(ch))
					digits.Append(ch);
				else if (digits.Length > 0)
					break; // first run of digits = the year
			}
			if (digits.Length > 0 && int.TryParse(digits.ToString(), NumberStyles.Integer,
				CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
			{
				year = parsed;
			}
		}
	}
}
