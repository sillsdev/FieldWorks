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
using System.ComponentModel;
using System.Windows.Forms;

namespace NUnit.Extensions.Forms
{
	/// <summary>
	/// Internal use only.  Base class for ControlFinder and MenuItemFinder.
	/// </summary>
	/// <remarks>
	/// It is also used by the recorder application to determine names of Controls.
	/// </remarks>
	public class Finder
	{
		/// <summary>
		/// Finds the parent of a Control or MenuItem.
		/// </summary>
		/// <remarks>
		/// Necessary only because Control and MenuItem don't have a shared base class.</remarks>
		/// <param name="o">the Control or MenuItem</param>
		/// <returns>The parent of the Control or MenuItem</returns>
		public object Parent(object o)
		{
			if (o is Control)
			{
				return ((Control)o).Parent;
			}
			if (o is MenuItem)
			{
				return ((MenuItem)o).Parent;
			}
			if (o is Component)
			{
				return ((Component)o).Container;
			}
			return null;
		}

		/// <summary>
		/// Finds the name of a Control or MenuItem.
		/// </summary>
		/// <remarks>
		/// Necessary only because Control and MenuItem don't have a shared base class.</remarks>
		/// <param name="o">the Control or MenuItem</param>
		/// <returns>The name of the Control or MenuItem</returns>
		public string Name(object o)
		{

			if (o is ToolStripControlHost)
			{
				return ((ToolStripControlHost)o).Name;
			}
			if (o is ToolStripItem)
			{
				return ((ToolStripItem)o).Name;
			}

			if (o is Control)
			{
				return ((Control)o).Name;
			}
			if (o is MenuItem)
			{
				return ((MenuItem)o).Text.Replace("&", string.Empty).Replace(".", string.Empty);
			}
			if (o is MainMenu)
			{
				return "MainMenu";
			}
			if (o is ContextMenu)
			{
				return "ContextMenu";
			}


			if (o is Component)
			{
				return ((Component)o).Site.Name;
			}
			throw new Exception("Object name not defined");
		}
	}
}