using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements a label control whose background appearance mimics that of a list view
	/// header.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class HeaderLabel : FwTextPanel
	{
		private bool m_showWindowBackgroudOnTopAndRightEdge = true;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="HeaderLabel"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public HeaderLabel()
		{
			// By default, don't hide mnemonic prefix.
			TextFormatFlags &= ~TextFormatFlags.HidePrefix;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not a one pixel line on the top and
		/// right edge of the panel is painted the window background color. This is they
		/// way a list view header is drawn... believe it or not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowWindowBackgroudOnTopAndRightEdge
		{
			get { return m_showWindowBackgroudOnTopAndRightEdge; }
			set { m_showWindowBackgroudOnTopAndRightEdge = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw a background that looks like a list view header.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			Rectangle rc = ClientRectangle;
			e.Graphics.FillRectangle(SystemBrushes.Window, rc);
			VisualStyleElement element = VisualStyleElement.Header.Item.Normal;

			// Draw the background, preferrably using visual styles.
			if (!PaintingHelper.CanPaintVisualStyle(element))
				ControlPaint.DrawButton(e.Graphics, rc, ButtonState.Normal);
			else
			{
				// Add 2 so the separator that's drawn at the right
				// side of normal list resultView header isn't visible.
				rc.Width += 2;

				if (m_showWindowBackgroudOnTopAndRightEdge)
				{
					// Shrink the rectangle so the top and left
					// edge window background don't get clobbered.
					rc.Height--;
					rc.Y++;
					rc.X++;
				}

				VisualStyleRenderer renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(e.Graphics, rc);

				if (m_showWindowBackgroudOnTopAndRightEdge)
				{
					// Draw a window background color line down the right edge.
					rc = ClientRectangle;
					e.Graphics.DrawLine(SystemPens.Window,
						new Point(rc.Width - 1, 0), new Point(rc.Width - 1, rc.Bottom));
				}
			}
		}
	}
}
