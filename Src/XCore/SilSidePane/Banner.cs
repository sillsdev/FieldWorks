// SilSidePane, Copyright 2010 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Banner at the top of a sidepane to display the name of the currently-selected tab.
	/// </summary>
	internal class Banner : Label
	{
		public Banner()
		{
			ForeColor = Color.White;
		}

		/// <summary>
		/// Paint a blue gradient background
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			var rectangleToPaint = ClientRectangle;
			var beginColor = Color.FromArgb(0x58, 0x80, 0xd0);
			var endColor = Color.FromArgb(0x08, 0x40, 0x98);

			using (var brush = new LinearGradientBrush(rectangleToPaint, beginColor, endColor,
				LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(brush, rectangleToPaint);
			}

			// Then paint the label text on top
			base.OnPaint(e);
		}
	}
}