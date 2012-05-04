#region Copyright (c) 2003-2005, Luke T. Maxon

/********************************************************************************************************************
'
' Copyright (c) 2003-2005, Luke T. Maxon
' All rights reserved.
'
' Redistribution and use in source and binary forms, with or without modification, are permitted provided
' that the following conditions are met:
'
' * Redistributions of source code must retain the above copyright notice, this list of conditions and the
' 	following disclaimer.
'
' * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and
' 	the following disclaimer in the documentation and/or other materials provided with the distribution.
'
' * Neither the name of the author nor the names of its contributors may be used to endorse or
' 	promote products derived from this software without specific prior written permission.
'
' THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
' WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
' PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
' ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
' LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
' INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
' OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
' IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
'
'*******************************************************************************************************************/

#endregion

using System;
using System.Windows.Forms;

namespace NUnit.Extensions.Forms
{
	/// <summary>
	/// A class to help find a form according to its name.  NUnitForms users should not need to use
	/// this class.  Consider it as internal.
	/// </summary>
	/// <remarks>
	/// It is also used by the recorder application.</remarks>
	public class FormFinder
	{
		private FormCollection forms;

		private string name;

		private int OnEnumWindow(IntPtr hwnd, IntPtr lParam)
		{
			Control control = Form.FromHandle(hwnd);
			if (control != null)
			{
				Form form = control as Form;
				if (form != null)
				{
					if (name == null || form.Name == name)
					{
						forms.Add(form);
					}
				}
			}
			return 1;
		}

		/// <summary>
		/// Finds all of the forms with a specified name and returns them in a FormCollection.
		/// </summary>
		/// <param name="name">The name of the form to search for.</param>
		/// <returns>the FormCollection of all found forms.</returns>
		public FormCollection FindAll(string name)
		{
			lock (this)
			{
				forms = new FormCollection();
				this.name = name;
				IntPtr desktop = Win32.GetDesktopWindow();
				Win32.EnumChildWindows(desktop, new Win32.WindowEnumProc(OnEnumWindow), IntPtr.Zero);
				return forms;
			}
		}

		/// <summary>
		/// Finds one form with the specified name.
		/// </summary>
		/// <param name="name">The name of the form to search for.</param>
		/// <returns>The form it finds.</returns>
		/// <exception cref="NoSuchControlException">
		/// Thrown if there are no forms with the specified name.
		/// </exception>
		/// <exception cref="AmbiguousNameException">
		/// Thrown if there is more than one form with the specified name.</exception>
		public Form Find(string name)
		{
			FormCollection list = FindAll(name);
			if (list.Count == 0)
			{
				throw new NoSuchControlException("Could not find form with name '" + name + "'");
			}
			if (list.Count > 1)
			{
				throw new AmbiguousNameException("Found too many forms with the name '" + name + "'");
			}
			return list[0];
		}

		/// <summary>
		/// Finds all of the forms.
		/// </summary>
		/// <returns>FormCollection with all of the forms regardless of name.</returns>
		public FormCollection FindAll()
		{
			return FindAll(null);
		}
	}
}