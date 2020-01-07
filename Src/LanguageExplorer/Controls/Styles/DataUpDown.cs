// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary>
	/// Subclass of the NumericUpDown class to support various number systems
	/// </summary>
	internal sealed class DataUpDown : UpDownBase
	{
		/// <summary>
		/// Event that indicates when the value has changed.
		/// </summary>
		public event EventHandler Changed;

		#region Data Members
		private DataUpDownMode m_mode = DataUpDownMode.Normal;
		/// <summary />
		private int m_currentValue;
		private string m_previousText = string.Empty;
		private bool m_validating;
		#endregion

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}

		/// <summary>
		/// Gets or sets the mode.
		/// </summary>
		public DataUpDownMode Mode
		{
			get
			{
				return m_mode;
			}
			set
			{
				m_mode = value;
				UpdateEditText();
			}
		}

		/// <summary>
		/// Gets or sets the min value.
		/// </summary>
		public int MinValue { get; set; }

		/// <summary>
		/// Gets or sets the max value.
		/// </summary>
		public int MaxValue { get; set; }

		/// <summary>
		/// Gets/Sets the current value.
		/// </summary>
		public int Value
		{
			get
			{
				return m_currentValue;
			}
			set
			{
				if (value >= MinValue && value <= MaxValue)
				{
					m_currentValue = value;
					UpdateEditText();
					Changed?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		/// <inheritdoc />
		public override void DownButton()
		{
			Value--;
		}

		/// <inheritdoc />
		public override void UpButton()
		{
			Value++;
		}

		/// <inheritdoc />
		protected override void UpdateEditText()
		{
			switch (m_mode)
			{
				case DataUpDownMode.Normal:
					Text = m_currentValue.ToString();
					break;

				case DataUpDownMode.Letters:
					Text = AlphaOutline.NumToAlphaOutline(m_currentValue);
					break;

				case DataUpDownMode.LettersLowerCase:
					Text = AlphaOutline.NumToAlphaOutline(m_currentValue).ToLower();
					break;

				case DataUpDownMode.Roman:
					Text = RomanNumerals.IntToRoman(m_currentValue);
					break;

				case DataUpDownMode.RomanLowerCase:
					Text = RomanNumerals.IntToRoman(m_currentValue).ToLower();
					break;
			}
		}

		/// <inheritdoc />
		protected override void OnTextBoxTextChanged(object source, EventArgs e)
		{
			ValidateEditText();
			base.OnTextBoxTextChanged(source, e);
		}

		/// <inheritdoc />
		protected override void ValidateEditText()
		{
			var newValue = 0;
			var text = Text;
			if (text == string.Empty)
			{
				return;
			}
			// don't allow validation to recurse
			if (m_validating)
			{
				return;
			}
			m_validating = true;
			switch (m_mode)
			{
				case DataUpDownMode.Normal:
					foreach (var ch in text)
					{
						if (!char.IsDigit(ch))
						{
							newValue = -1;
							break;
						}
					}
					if (newValue != -1)
					{
						newValue = int.Parse(text);
					}
					break;
				case DataUpDownMode.Letters:
				case DataUpDownMode.LettersLowerCase:
					newValue = AlphaOutline.AlphaOutlineToNum(text);
					// If the text does not validate and the old text does not validate then
					// switch to a value of 1.
					if (newValue == -1 && AlphaOutline.AlphaOutlineToNum(m_previousText) == -1)
					{
						newValue = 1;
						text = AlphaOutline.NumToAlphaOutline(newValue);
					}
					text = m_mode == DataUpDownMode.Letters ? text.ToUpper() : text.ToLower();
					break;
				case DataUpDownMode.Roman:
				case DataUpDownMode.RomanLowerCase:
					newValue = RomanNumerals.RomanToInt(text);
					// If the text does not validate and the old text does not validate then
					// switch to a value of 1.
					if (newValue == -1 && RomanNumerals.RomanToInt(m_previousText) == -1)
					{
						newValue = 1;
						text = RomanNumerals.IntToRoman(newValue);
					}
					text = m_mode == DataUpDownMode.Roman ? text.ToUpper() : text.ToLower();
					break;
			}
			if (newValue >= 0 && newValue <= MaxValue)
			{
				m_previousText = text;
				Value = newValue;
			}
			else
			{
				Text = m_previousText;
				Select(m_previousText.Length, 0);
			}
			m_validating = false;
		}
	}
}