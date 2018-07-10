// Copyright (c) 20018 SIL International
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
		/// Default constructor for designer view
		/// </summary>
		public CustomDigitEntryControl()
		{
			for (var i = 0; i < 10; ++i)
			{
				var digitBox = new TextBox {Size = new Size(30, 30)};
				_digitControls[i] = digitBox;
				digitBox.KeyDown += DigitBoxOnKeyPress;
				Controls.Add(digitBox);
			}
		}

		private void DigitBoxOnKeyPress(object sender, KeyEventArgs keyEventArgs)
		{
			// Reset to the default background color when the user starts editing
			var digitBox = sender as TextBox;
			if (digitBox != null)
			{
				digitBox.BackColor = Color.Empty;
			}
		}

		/// <summary/>
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

			this.ResetColor();
		}

		private Font DigitFont
		{
			get { return _font; }
			set
			{
				if (_font != null)
				{
					_font = value;
				}
			}
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing,
				"****************** Missing Dispose() call for " + GetType().Name +
				" ******************");

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

		/// <summary/>
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

		/// <summary/>
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
