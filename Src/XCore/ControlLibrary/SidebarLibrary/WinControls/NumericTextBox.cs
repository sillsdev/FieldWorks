// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

namespace SidebarLibrary.WinControls
{
	/// <summary>
	/// Summary description for NumericTextBox.
	/// </summary>
	public class NumericTextBox : System.Windows.Forms.TextBox
	{
		private int minimum = -1;
		private int maximum = -1;
		private bool useRange = false;
		private string lastChar;

		public NumericTextBox()
		{
			InitializeComponent();
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			base.OnPaint(pe);
		}

		public Size SetRange
		{
			get
			{
				if ( useRange )
					return new Size(minimum, maximum);
				else
					return new Size(-1, -1);
			}
			set
			{
				minimum = value.Width;
				maximum = value.Height;
				useRange = true;
			}

		}

		private void InitializeComponent()
		{
			//
			// NumericTextBox
			//
			this.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);

		}



		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			// Allow only numeric characters
			base.OnKeyPress(e);
			if ( Char.IsLetter(e.KeyChar) || Char.IsPunctuation(e.KeyChar) || Char.IsSeparator(e.KeyChar) )
				e.Handled = true;
			else
			{
				e.Handled = false;
			}
		}


		private void NumericTextBox_TextChanged(object sender, System.EventArgs e)
		{
			if ( useRange )
			{
				if ( Text != "" )
				{
					int val = Convert.ToInt32(Text);
					if ( val > maximum )
					{
						Text = maximum.ToString();
					}
					else if ( val < minimum )
					{
						Text = minimum.ToString();
					}

					if ( Text.Length == 1 )
					{
						// If use delete the last character remaining
						// remember it so that if the user jumps to another
						// edit control we can put back the last digit so that
						// we don't have an empty numeric control which is not good
						lastChar = Text;
					}

				}

			}
		}



		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			if ( Text.Length == 0 && useRange )
			{
				Text = lastChar;
			}
		}


	}
}
