// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ProgressLineDesigner.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms.Design;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ProgressLineDesigner: ControlDesigner
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProgressLineDesigner"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ProgressLineDesigner()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Receives a call when the control that the designer is managing has painted its
		/// surface so the designer can paint any additional adornments on top of the control.
		/// </summary>
		/// <param name="pe">A <see cref="T:System.Windows.Forms.PaintEventArgs"></see> the
		/// designer can use to draw on the control.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaintAdornments(System.Windows.Forms.PaintEventArgs pe)
		{
			base.OnPaintAdornments(pe);

			Rectangle progressBarRectangle = Control.ClientRectangle;
			int valueToPaint = (int)(0.5 * (double)progressBarRectangle.Width);

			Color foreColor2 = (Color)ShadowProperties["ForeColor2"];
			LinearGradientMode gradMode = (LinearGradientMode)ShadowProperties["LinearGradientMode"];
			using (Brush backBrush = new SolidBrush(Control.BackColor))
			{
				Brush foreBrush = null;
				try
				{
					if (foreColor2 == Color.Transparent)
						foreBrush = new SolidBrush(Control.ForeColor);
					else
						foreBrush = new LinearGradientBrush(
							new Rectangle(0, 0, valueToPaint, Control.Height),
							Control.ForeColor, foreColor2, gradMode);

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
					if (foreBrush != null)
						foreBrush.Dispose();
				}
			}
		}
	}
}
