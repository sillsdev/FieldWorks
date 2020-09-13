// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// Control for displaying the digits in an LDML numbering system and for entering Custom digits
	/// </summary>
	public class CustomDigitEntryControl : FlowLayoutPanel
	{
		private TextBox[] _digitControls = new TextBox[10];
		private Font _font;
		/// <summary>
		/// Raised when one of the digits changes
		/// </summary>
		public event EventHandler CustomDigitsChanged;

		/// <summary />
		public CustomDigitEntryControl()
		{
			for (var i = 0; i < 10; ++i)
			{
				var digitBox = new TextBox { Size = new Size(30, 30) };
				_digitControls[i] = digitBox;
				digitBox.KeyDown += DigitBoxOnKeyDown;
				digitBox.KeyUp += DigitBox_KeyUp;
				Controls.Add(digitBox);
			}
		}

		private void DigitBox_KeyUp(object sender, KeyEventArgs e)
		{
			// Currently, if there aren't exactly 10, it gets reset to default.
			// Probably better to leave things unchanged.
			// This is done on Key UP so the effect of the key press has already happened.
			if (GetDigits().Length == 10)
			{
				CustomDigitsChanged.Invoke(this, new EventArgs());
			}
		}

		private void DigitBoxOnKeyDown(object sender, KeyEventArgs keyEventArgs)
		{
			// Reset to the default background color when the user starts editing
			var digitBox = sender as TextBox;
			if (digitBox != null)
			{
				digitBox.BackColor = Color.Empty;
			}
		}

		/// <summary />
		/// <returns>A concatenation of each digit from the control</returns>
		public string GetDigits()
		{
			var digits = string.Empty;
			for (var i = 0; i < 10; ++i)
			{
				digits += _digitControls[i].Text;
			}

			return digits;
		}

		/// <summary>
		/// Set the digits in the character boxes from the string with the given writing system
		/// </summary>
		public void SetDigits(string digits, string wsFontFamily, float wsFontSize)
		{
			var isEmpty = digits.Equals(string.Empty);
			DigitFont = new Font(wsFontFamily, wsFontSize);
			if (!isEmpty && new StringInfo(digits).LengthInTextElements != 10)
			{
				throw new ArgumentException("digits string must include 10 characters, or be the empty string to clear all boxes", nameof(digits));
			}

			var tee = StringInfo.GetTextElementEnumerator(digits);
			for (var i = 0; i < 10; ++i)
			{
				_digitControls[i].Font = DigitFont;
				_digitControls[i].Text = (!isEmpty && tee.MoveNext()) ? tee.GetTextElement() : string.Empty;
			}

			ResetColor();
		}

		private Font DigitFont
		{
			get => _font;
			set
			{
				if (_font != null)
				{
					_font = value;
				}
			}
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");

			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_font?.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <summary />
		public bool AreAllDigitsValid()
		{
			for (var i = 0; i < 10; ++i)
			{
				if (new StringInfo(_digitControls[i].Text).LengthInTextElements != 1)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary />
		public void HighlightProblemDigits()
		{
			for (var i = 0; i < 10; ++i)
			{
				if (new StringInfo(_digitControls[i].Text).LengthInTextElements != 1)
				{
					_digitControls[i].BackColor = Color.Red;
				}
			}
		}

		/// <summary>
		/// Set digit text box colors back to default.
		/// </summary>
		public void ResetColor()
		{
			for (var i = 0; i < 10; ++i)
			{
				_digitControls[i].BackColor = Color.Empty;
			}
		}
	}
}
