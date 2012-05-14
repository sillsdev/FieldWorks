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

using System.Windows.Forms;

namespace NUnit.Extensions.Forms
{
	/// <summary>
	/// Internal use only.  Finds controls according to their name property.
	/// </summary>
	/// <remarks>
	/// It is also used by the recorder application.
	/// </remarks>
	/// the recorder application.
	public class ControlFinder : Finder
	{
		private string name;

		private FormCollection forms = null;

		/// <summary>
		/// Creates a ControlFinder that will find controls on a specific Form according to their name.
		/// </summary>
		/// <param name="name">The name of the Control to find.</param>
		/// <param name="form">The form to search for the control.</param>
		public ControlFinder(string name, Form form)
		{
			this.name = name;
			if (form != null)
			{
				forms = new FormCollection();
				forms.Add(form);
			}
		}

		/// <summary>
		/// Creates a ControlFinder that will find controls according to their name.
		/// </summary>
		/// <param name="name">The name of the Control to find.</param>
		public ControlFinder(string name)
		{
			this.name = name;
		}

		/// <summary>
		/// Finds a control.
		/// </summary>
		/// <exception>
		/// If there is more than one with the specified name, it will
		/// throw an AmbiguousNameException.  If the Control does not exist, it will throw
		/// a NoSuchControlException.
		/// </exception>
		/// <returns>The control if one is found.</returns>
		public Control Find()
		{
			return Find(-1);
		}

		private FormCollection FormCollection
		{
			get
			{
				if (forms == null)
				{
					return new FormFinder().FindAll();
				}
				return forms;
			}
		}

		internal int Count
		{
			get
			{
				return FindControls().Count;
			}
		}

		private ControlCollection FindControls()
		{
			ControlCollection found = new ControlCollection();
			foreach (Form form in FormCollection)
			{
				found.Add(Find(name, form));
			}
			return found;
		}

		internal Control Find(int index)
		{
			ControlCollection found = FindControls();
			if (index < 0)
			{
				if (found.Count == 1)
				{
					return found[0];
				}
				else if (found.Count == 0)
				{
					throw new NoSuchControlException(name);
				}
				else
				{
					throw new AmbiguousNameException(name);
				}
			}
			else
			{
				if (found.Count > index)
				{
					return found[index];
				}
				else
				{
					throw new NoSuchControlException(name + "[" + index + "]");
				}
			}
		}

		private ControlCollection Find(string name, Control control)
		{
			ControlCollection results = new ControlCollection();

			if (Matches(name, control))
			{
				results.Add(control);
			}

			results.Add(Find(name, control.Controls));

			return results;
		}

		/// <summary>
		/// Find all controls with the given name in a collection
		/// </summary>
		/// <param name="name"></param>
		/// <param name="collection"></param>
		/// <returns></returns>
		private ControlCollection Find(string name, Control.ControlCollection collection)
		{
			ControlCollection results = new ControlCollection();

			foreach (Control c in collection)
			{
				results.Add(Find(name, c));
				// If the control is a ToolStripContainer we need to search in it's
				// panels for controls matching the name we serching for.
				if (c is ToolStripContainer)
				{
					ToolStripContainer container = (ToolStripContainer)c;
					results.Add(Find(name, container.TopToolStripPanel.Controls));
					results.Add(Find(name, container.LeftToolStripPanel.Controls));
					results.Add(Find(name, container.RightToolStripPanel.Controls));
					results.Add(Find(name, container.BottomToolStripPanel.Controls));
					results.Add(Find(name, container.ContentPanel.Controls));
				}
			}
			return results;
		}

		private bool Matches(string name, object control)
		{
			object c = control;
			string[] names = name.Split('.');
			for (int i = names.Length - 1; i >= 0; i--)
			{
				if (!names[i].Equals(Name(c)))
				{
					return false;
				}
				c = Parent(c);
			}
			return true;
		}
	}
}