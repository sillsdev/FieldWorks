// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Implements a label control whose background appearance mimics that of a list view header.
	/// </summary>
	public class HeaderLabel : FwTextPanel
	{
		/// <summary />
		public HeaderLabel()
		{
			// By default, don't hide mnemonic prefix.
			TextFormatFlags &= ~TextFormatFlags.HidePrefix;
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not a one pixel line on the top and
		/// right edge of the panel is painted the window background color. This is the
		/// way a list view header is drawn... believe it or not.
		/// </summary>
		public bool ShowWindowBackgroundOnTopAndRightEdge { get; set; } = true;

		/// <inheritdoc />
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			var rc = ClientRectangle;
			e.Graphics.FillRectangle(SystemBrushes.Window, rc);
			var element = VisualStyleElement.Header.Item.Normal;

			// Draw the background, preferably using visual styles.
			if (!PaintingHelper.CanPaintVisualStyle(element))
			{
				ControlPaint.DrawButton(e.Graphics, rc, ButtonState.Normal);
			}
			else
			{
				// Add 2 so the separator that's drawn at the right
				// side of normal list resultView header isn't visible.
				rc.Width += 2;

				if (ShowWindowBackgroundOnTopAndRightEdge)
				{
					// Shrink the rectangle so the top and left
					// edge window background don't get clobbered.
					rc.Height--;
					rc.Y++;
					rc.X++;
				}

				var renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(e.Graphics, rc);

				if (ShowWindowBackgroundOnTopAndRightEdge)
				{
					// Draw a window background color line down the right edge.
					rc = ClientRectangle;
					e.Graphics.DrawLine(SystemPens.Window, new Point(rc.Width - 1, 0), new Point(rc.Width - 1, rc.Bottom));
				}
			}
		}
	}
}