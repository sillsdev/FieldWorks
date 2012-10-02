/*
 *
 * This file was taken from http://weblogs.asp.net/pglavich/archive/2006/02/26/439077.aspx
 * ("SecurePasswordTextBox - A textbox that uses the SecureString class" on
 * Glavs Blog (The dotDude of .Net)
 * There appears to be no licesning information.
 *
 */

#region Using Statements

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Security;
using System.Runtime.InteropServices;

#endregion

namespace SecurePasswordTextBox
{
	/// <summary>
	/// This is a TextBox implementation that uses the System.Security.SecureString as its backing
	/// store instead of standard managed string instance. At no time, is a managed string instance
	/// used to hold a component of the textual entry.
	/// It does not display any text and relies on the 'PasswordChar' character to display the amount of
	/// characters entered. If no password char is defined, then an 'asterisk' is used.
	/// </summary>
	public partial class SecureTextBox : TextBox
	{
		#region Private fields

		private bool _displayChar = false;
		SecureString _secureEntry = new SecureString();

		#endregion

		#region Constructor
		public SecureTextBox()
		{
			InitializeComponent();

			this.PasswordChar = '*'; // default to an asterisk
		}

		#endregion

		#region Public properties

		/// <summary>
		/// The secure string instance captured so far.
		/// This is the preferred method of accessing the string contents.
		/// </summary>
		public SecureString SecureText
		{
			get
			{
				return _secureEntry;
			}
			set
			{
				_secureEntry = value;
			}
		}

		/// <summary>
		/// Allows the consumer to retrieve this string instance as a character array. Note that this is still
		/// visible plainly in memory and should be 'consumed' as quickly as possible, then the contents
		/// 'zero-ed' so that they cannot be viewed.
		/// </summary>
		public char[] CharacterData
		{
			get
			{
				char[] bytes = new char[_secureEntry.Length];
				IntPtr ptr = IntPtr.Zero;

				try
				{
					ptr = Marshal.SecureStringToBSTR(_secureEntry);
					bytes = new char[_secureEntry.Length];
					Marshal.Copy(ptr, bytes,0,_secureEntry.Length);
				}
				finally
				{
					if (ptr != IntPtr.Zero)
						Marshal.ZeroFreeBSTR(ptr);
				}
				return bytes;
			}
		}

		#endregion

		#region ProcessKeyMessage

		protected override bool ProcessKeyMessage(ref Message m)
		{
			// Allow user to press Escape instead of clicking the dialog's Cancel button:
			if (m.Msg == 256 /* WM_KEYDOWN */ && (int)m.WParam == 27 /* Escape */)
			{
				Form Dlg = FindForm();
				Dlg.DialogResult = DialogResult.Cancel;
				Dlg.Close();
			}

			if (_displayChar)
			{
				// Allow user to press Enter instead of clicking the dialog's OK button:
				if (m.Msg == 257 /* WM_KEYUP */ && (int)m.WParam == 13 /* Enter */)
				{
					Form Dlg = FindForm();
					Dlg.DialogResult = DialogResult.OK;
					Dlg.Close();
				}

				return base.ProcessKeyMessage(ref m);
			}
			else
			{
				_displayChar = true;
				return true;
			}
		}

		#endregion

		#region IsInputChar

		protected override bool IsInputChar(char charCode)
		{
			int startPos = this.SelectionStart;

			bool isChar = base.IsInputChar(charCode);
			if (isChar)
			{
				int keyCode = (int)charCode;

				// If the key pressed is NOT a control/cursor type key, then add it to our instance.
				// Note: This does not catch the SHIFT key or anything like that
				if (!Char.IsControl(charCode) && !char.IsHighSurrogate(charCode) && !char.IsLowSurrogate(charCode))
				{

					if (this.SelectionLength > 0)
					{
						for (int i = 0; i < this.SelectionLength; i++)
							_secureEntry.RemoveAt(this.SelectionStart);
					}

					if (startPos == _secureEntry.Length)
					{
						_secureEntry.AppendChar(charCode);
					}
					else
					{
						_secureEntry.InsertAt(startPos, charCode);
					}

					this.Text = new string('*', _secureEntry.Length);

					_displayChar = false;
					startPos++;

					this.SelectionStart = startPos;
				}
				else
				{
					// We need to check what key has been pressed.
					switch (keyCode)
					{
						case (int)Keys.Back:
							if (this.SelectionLength == 0 && startPos > 0)
							{
								startPos--;
								_secureEntry.RemoveAt(startPos);
								this.Text = new string('*', _secureEntry.Length);
								this.SelectionStart = startPos;
							}
							else if (this.SelectionLength > 0)
							{
								for (int i = 0; i < this.SelectionLength; i++)
									_secureEntry.RemoveAt(this.SelectionStart);
							}
							_displayChar = false;   // If we dont do this, we get a 'double' BACK keystroke effect

							break;
					}
				}
			}
			else
				_displayChar = true;

			return isChar;
		}

		#endregion

		#region IsInputKey

		protected override bool IsInputKey(Keys keyData)
		{
			bool result = true;

			// Note: This whole section is only to deal with the 'Delete' key.

			bool allowedToDelete = ((keyData & Keys.Delete) == Keys.Delete);

			// Debugging only
			//this.Parent.Text = keyData.ToString() + " " + ((int)keyData).ToString() + " allowedToDelete = " + allowedToDelete.ToString();

			if (allowedToDelete)
			{
				if (this.SelectionLength == _secureEntry.Length)
					_secureEntry.Clear();
				else if (this.SelectionLength > 0)
				{
					for (int i = 0; i < this.SelectionLength; i++)
						_secureEntry.RemoveAt(this.SelectionStart);
				}
				else
				{
					if ((keyData & Keys.Delete) == Keys.Delete && this.SelectionStart < this.Text.Length)
						_secureEntry.RemoveAt(this.SelectionStart);
				}
			}
			return result;
		}
		#endregion
	}
}
