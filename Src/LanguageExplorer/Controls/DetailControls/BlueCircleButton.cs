// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Resources;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This control simply draws the blue circle that FieldWorks often uses as an indication that a popup menu is available.
	/// </summary>
	internal sealed partial class BlueCircleButton : Control
	{
		private readonly Image _blueCircle;

		/// <summary />
		internal BlueCircleButton()
		{
			InitializeComponent();

			_blueCircle = ResourceHelper.BlueCircleDownArrowForView;
			Height = _blueCircle.Height + 3;
			Width = _blueCircle.Width + 3;
			Cursor = Cursors.Arrow;
		}

		/// <summary />
		protected override void OnPaint(PaintEventArgs pe)
		{
			pe.Graphics.DrawImage(_blueCircle, 0, 0);
		}
	}
}
