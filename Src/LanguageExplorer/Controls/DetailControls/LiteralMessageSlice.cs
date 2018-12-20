// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal class LiteralMessageSlice : Slice
	{
		/// <summary />
		public LiteralMessageSlice(string message) : base(new Label())
		{
			Control.Text = message;
			Control.BackColor = System.Drawing.SystemColors.ControlLight;
		}

		/// <summary />
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);
			if (Control != null)
			{
				Control.AccessibilityObject.Value = Control.Text;
			}
		}
	}
}