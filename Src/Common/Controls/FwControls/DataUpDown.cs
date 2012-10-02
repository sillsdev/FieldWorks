// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataUpDown.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>Modes supported by the up/down control</summary>
	public enum DataUpDownMode
	{
		/// <summary></summary>
		Normal,
		/// <summary></summary>
		Roman,
		/// <summary></summary>
		RomanLowerCase,
		/// <summary></summary>
		Letters,
		/// <summary></summary>
		LettersLowerCase
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Subclass of the NumericUpDown class to support various number systems
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DataUpDown : UpDownBase, IFWDisposable
	{
		/// <summary>
		/// Event that indicates when the value has changed.
		/// </summary>
		public event EventHandler Changed;

		#region Data Members
		private DataUpDownMode m_mode = DataUpDownMode.Normal;
		/// <summary></summary>
		protected int m_currentValue;
		private int m_minValue;
		private int m_maxValue;
		private string m_previousText = string.Empty;
		private bool m_validating = false;
		#endregion

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DataUpDownMode Mode
		{
			get
			{
				CheckDisposed();
				return m_mode;
			}
			set
			{
				CheckDisposed();
				m_mode = value;
				UpdateEditText();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the min value.
		/// </summary>
		/// <value>The min value.</value>
		/// ------------------------------------------------------------------------------------
		public int MinValue
		{
			get
			{
				CheckDisposed();
				return m_minValue;
			}
			set
			{
				CheckDisposed();
				m_minValue = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the max value.
		/// </summary>
		/// <value>The max value.</value>
		/// ------------------------------------------------------------------------------------
		public int MaxValue
		{
			get
			{
				CheckDisposed();
				return m_maxValue;
			}
			set
			{
				CheckDisposed();
				m_maxValue = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets the current value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Value
		{
			get
			{
				CheckDisposed();
				return m_currentValue;
			}
			set
			{
				CheckDisposed();
				if (value >= m_minValue && value <= m_maxValue)
				{
					m_currentValue = value;
					UpdateEditText();
					if (Changed != null)
						Changed(this, EventArgs.Empty);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the clicking of the down button on the spin box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void DownButton()
		{
			CheckDisposed();
			Value--;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the clicking of the up button on the spin box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void UpButton()
		{
			CheckDisposed();
			Value++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the text displayed in the spin box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void UpdateEditText()
		{
			switch(m_mode)
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the text box contents change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnTextBoxTextChanged(object source, EventArgs e)
		{
			ValidateEditText();
			base.OnTextBoxTextChanged(source, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates the text displayed in the spin box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ValidateEditText()
		{
			int newValue = 0;
			string text = Text;
			if (text == string.Empty)
				return;

			// don't allow validation to recurse
			if (m_validating)
				return;
			m_validating = true;

			switch (m_mode)
			{
				case DataUpDownMode.Normal:
					foreach (char ch in text)
					{
						if (!Char.IsDigit(ch))
						{
							newValue = -1;
							break;
						}
					}
					if (newValue != -1)
						newValue = Int32.Parse(text);
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

					if (m_mode == DataUpDownMode.Letters)
						text = text.ToUpper();
					else
						text = text.ToLower();
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

					if (m_mode == DataUpDownMode.Roman)
						text = text.ToUpper();
					else
						text = text.ToLower();
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
