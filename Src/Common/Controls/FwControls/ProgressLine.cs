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
// File: ProgressLine.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A line that can be used as progress bar. Progress is painted in foreground color.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Designer("SIL.FieldWorks.Common.Controls.Design.ProgressLineDesigner")]
	public partial class ProgressLine : LineControl
	{
		private int m_MinValue = 0;
		private int m_MaxValue = 100;
		private int m_Value;
		private int m_Step = 1;
		private bool m_fWrapAround = true;
		private Brush m_BackBrush;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProgressLine"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ProgressLine()
		{
			InitializeComponent();
			DoubleBuffered = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the background brush.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateBackgroundBrush()
		{
			if (m_BackBrush != null)
				m_BackBrush.Dispose();

			m_BackBrush = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the background color for the control.
		/// </summary>
		/// <value></value>
		/// <returns>A <see cref="T:System.Drawing.Color"></see> that represents the background
		/// color of the control. The default is ControlDark.</returns>
		/// <PermissionSet><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
		/// ------------------------------------------------------------------------------------
		public override Color BackColor
		{
			get { return base.BackColor; }
			set
			{
				base.BackColor = value;
				UpdateBackgroundBrush();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the min value.
		/// </summary>
		/// <value>The min value.</value>
		/// ------------------------------------------------------------------------------------
		[Description("The minimum value of the range of the control")]
		[DefaultValue(0)]
		public int MinValue
		{
			get { return m_MinValue; }
			set
			{
				if (value > m_MaxValue)
					throw new ArgumentException();
				m_MinValue = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the max value.
		/// </summary>
		/// <value>The max value.</value>
		/// ------------------------------------------------------------------------------------
		[Description("The maximum value of the range of the control")]
		[DefaultValue(100)]
		public int MaxValue
		{
			get { return m_MaxValue; }
			set
			{
				if (value < m_MinValue)
					throw new ArgumentException();
				m_MaxValue = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the current value.
		/// </summary>
		/// <value>The value.</value>
		/// ------------------------------------------------------------------------------------
		[Description("The current position of the progress line.")]
		[DefaultValue(0)]
		public int Value
		{
			get { return m_Value; }
			set
			{
				int oldValue = m_Value;

				if (value < m_MinValue)
					m_Value = m_MinValue;
				else if (value > m_MaxValue)
				{
					if (m_fWrapAround)
						m_Value = 0;
					else
						m_Value = m_MaxValue;
				}
				else
					m_Value = value;

				int valueToInvalidate = Math.Max(m_Value, oldValue);
				Invalidate(new Rectangle(0, 0, ValueToPaint(valueToInvalidate), ClientRectangle.Height));
				Update();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to restart with 0 if value exceeds max value.
		/// Defaults is <c>true</c>.
		/// </summary>
		/// <value><c>true</c> to wrap around; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		[Description("Indicates whether or not to restart with MinValue if Value exceeds MaxValue.")]
		[DefaultValue(true)]
		public bool WrapAround
		{
			get { return m_fWrapAround; }
			set { m_fWrapAround = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the step width.
		/// </summary>
		/// <value>The step width.</value>
		/// ------------------------------------------------------------------------------------
		[Description("The amount by which a call to the PerformStep method increases the current position of the progress line.")]
		[DefaultValue(1)]
		public int Step
		{
			get { return m_Step; }
			set { m_Step = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the background brush.
		/// </summary>
		/// <value>The background brush.</value>
		/// ------------------------------------------------------------------------------------
		protected Brush BackgroundBrush
		{
			get
			{
				if (m_BackBrush == null)
				{
					m_BackBrush = new SolidBrush(BackColor);
					Invalidate();
				}
				return m_BackBrush;
			}
		}



		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Increments by specified step.
		/// </summary>
		/// <param name="nStep">The step width.</param>
		/// ------------------------------------------------------------------------------------
		public void Increment(int nStep)
		{
			Value += nStep;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs a step.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PerformStep()
		{
			Value += Step;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value to paint.
		/// </summary>
		/// <param name="inValue">The progress bar value.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int ValueToPaint(int inValue)
		{
			return (int)(((double)inValue / (double)m_MaxValue) *
			  (double)ClientRectangle.Width);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paints the foreground.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.PaintEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void PaintForeground(PaintEventArgs e)
		{
			// Don't call base class to minimize flicker

			Rectangle progressBarRectangle = e.ClipRectangle;
			int valueToPaint = ValueToPaint(m_Value);

			// Paint the progress
			if (valueToPaint > 0)
			{
				progressBarRectangle.Width = Math.Min(valueToPaint, e.ClipRectangle.Width);
				e.Graphics.FillRectangle(Brush, progressBarRectangle);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the paint background event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.PaintEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			base.OnPaintBackground(e);

			Rectangle progressBarRectangle = e.ClipRectangle;
			int valueToPaint = ValueToPaint(m_Value);

			if (valueToPaint < e.ClipRectangle.Width)
			{
				progressBarRectangle.Width = e.ClipRectangle.Width - valueToPaint;
				progressBarRectangle.X = valueToPaint > 0 ? valueToPaint + 1 : 0;
				e.Graphics.FillRectangle(BackgroundBrush, progressBarRectangle);
			}
		}
	}
}
