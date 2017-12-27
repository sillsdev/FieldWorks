// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Summary description for LiteralMessageSlice.
	/// </summary>
	internal class LiteralMessageSlice : Slice
	{
		/// <summary> Constructor.</summary>
		public LiteralMessageSlice(string message) : base(new Label())
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
		/// <param name="parentDataTree"></param>
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);

			if (this.Control != null)
			{
				this.Control.AccessibilityObject.Value = this.Control.Text;
			}
		}
	}
}
