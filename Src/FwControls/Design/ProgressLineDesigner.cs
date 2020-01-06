// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms.Design;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// <summary />
	public class ProgressLineDesigner: ControlDesigner
	{
		/// <inheritdoc />
		protected override void OnPaintAdornments(System.Windows.Forms.PaintEventArgs pe)
		{
			base.OnPaintAdornments(pe);

			var progressBarRectangle = Control.ClientRectangle;
			var valueToPaint = (int)(0.5 * (double)progressBarRectangle.Width);
			var foreColor2 = (Color)ShadowProperties["ForeColor2"];
			var gradMode = (LinearGradientMode)ShadowProperties["LinearGradientMode"];
			using (Brush backBrush = new SolidBrush(Control.BackColor))
			{
				Brush foreBrush = null;
				try
				{
					if (foreColor2 == Color.Transparent)
					{
						foreBrush = new SolidBrush(Control.ForeColor);
					}
					else
					{
						foreBrush = new LinearGradientBrush(new Rectangle(0, 0, valueToPaint, Control.Height), Control.ForeColor, foreColor2, gradMode);
					}

					// Paint the progress
					progressBarRectangle.Width = valueToPaint;
					pe.Graphics.FillRectangle(foreBrush, progressBarRectangle);

					// Paint the rest of our control
					progressBarRectangle.Width = Control.ClientRectangle.Width - valueToPaint;
					progressBarRectangle.X = valueToPaint + 1;
					pe.Graphics.FillRectangle(backBrush, progressBarRectangle);
				}
				finally
				{
					foreBrush?.Dispose();
				}
			}
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}
	}
}
