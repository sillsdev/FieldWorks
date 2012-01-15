using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Resources;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This control simply draws the blue circle that FieldWorks often uses as an indication that a popup menu is available.
	/// </summary>
	public partial class BlueCircleButton : Control
	{
		private Image m_blueCircle;
		/// <summary>
		/// Stupid mandatory comment.
		/// </summary>
		public BlueCircleButton()
		{
			InitializeComponent();

			m_blueCircle = ResourceHelper.BlueCircleDownArrowForView;
			Height = m_blueCircle.Height + 3;
			Width = m_blueCircle.Width + 3;
			Cursor = Cursors.Arrow;
		}

		/// <summary>
		/// Stupid mandatory comment.
		/// </summary>
		/// <param name="pe"></param>
		protected override void OnPaint(PaintEventArgs pe)
		{
			pe.Graphics.DrawImage(m_blueCircle, 0, 0);
		}
	}
}
