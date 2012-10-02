// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MessageSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// Implements a simple Message XDE editor.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;


namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for MessageSlice.
	/// </summary>
	public class MessageSlice : Slice
	{
		/// <summary> Constructor.</summary>
		public MessageSlice(string message) : base(new Label())
		{
			this.Control.Text = message;
			this.Control.BackColor = System.Drawing.SystemColors.ControlLight;
		}
//		// Overhaul Aug 05: want all Window backgrounds in Detail controls.
//		/// <summary>
//		/// This is passed the color that the XDE specified, if any, otherwise null.
//		/// The default is to use the normal window color for editable text.
//		/// Messages have a default already set in the constructor, so ignore
//		/// if null.
//		/// </summary>
//		/// <param name="clr"></param>
//		public override void OverrideBackColor(String backColorName)
//		{
//			CheckDisposed();
//
//			if (this.Control == null)
//				return;
//			if (backColorName != null)
//				this.Control.BackColor = Color.FromName(backColorName);
		//		}

		/// <summary>
		///
		/// </summary>
		/// <param name="parent"></param>
		public override void Install(DataTree parent)
		{
			CheckDisposed();

			base.Install(parent);

			if (this.Control != null)
			{
				this.Control.AccessibilityObject.Value = this.Control.Text;
			}
		}
	}
}
