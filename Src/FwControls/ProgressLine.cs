// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls.Design;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// A line that can be used as progress bar. Progress is painted in foreground color.
	/// </summary>
	[Designer(typeof(ProgressLineDesigner))]
	public partial class ProgressLine : LineControl
	{
		private int m_MinValue;
		private int m_MaxValue = 100;
		private int m_Value;
		private Brush m_BackBrush;

		/// <summary />
		public ProgressLine()
		{
			InitializeComponent();
			DoubleBuffered = true;
		}

		/// <summary>
		/// Updates the background brush.
		/// </summary>
		private void UpdateBackgroundBrush()
		{
			m_BackBrush?.Dispose();
			m_BackBrush = null;
		}

		/// <inheritdoc />
		public override Color BackColor
		{
			get { return base.BackColor; }
			set
			{
				base.BackColor = value;
				UpdateBackgroundBrush();
			}
		}

		/// <summary>
		/// Gets or sets the min value.
		/// </summary>
		[Description("The minimum value of the range of the control")]
		[DefaultValue(0)]
		public int MinValue
		{
			get { return m_MinValue; }
			set
			{
				if (value > m_MaxValue)
				{
					throw new ArgumentException();
				}
				m_MinValue = value;
			}
		}

		/// <summary>
		/// Gets or sets the max value.
		/// </summary>
		[Description("The maximum value of the range of the control")]
		[DefaultValue(100)]
		public int MaxValue
		{
			get { return m_MaxValue; }
			set
			{
				if (value < m_MinValue)
				{
					throw new ArgumentException();
				}
				m_MaxValue = value;
			}
		}

		/// <summary>
		/// Gets or sets the current value.
		/// </summary>
		[Description("The current position of the progress line.")]
		[DefaultValue(0)]
		public int Value
		{
			get { return m_Value; }
			set
			{
				var oldValue = m_Value;
				if (value < m_MinValue)
				{
					m_Value = m_MinValue;
				}
				else if (value > m_MaxValue)
				{
					if (WrapAround)
					{
						m_Value = 0;
					}
					else
					{
						m_Value = m_MaxValue;
					}
				}
				else
				{
					m_Value = value;
				}
				var valueToInvalidate = Math.Max(m_Value, oldValue);
				Invalidate(new Rectangle(0, 0, ValueToPaint(valueToInvalidate), ClientRectangle.Height));
				Update();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to restart with 0 if value exceeds max value.
		/// Defaults is <c>true</c>.
		/// </summary>
		[Description("Indicates whether or not to restart with MinValue if Value exceeds MaxValue.")]
		[DefaultValue(true)]
		public bool WrapAround { get; set; } = true;

		/// <summary>
		/// Gets or sets the step width.
		/// </summary>
		[Description("The amount by which a call to the PerformStep method increases the current position of the progress line.")]
		[DefaultValue(1)]
		public int Step { get; set; } = 1;

		/// <summary>
		/// Gets the background brush.
		/// </summary>
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

		/// <summary>
		/// Increments by specified step.
		/// </summary>
		public void Increment(int nStep)
		{
			Value += nStep;
		}

		/// <summary>
		/// Performs a step.
		/// </summary>
		public void PerformStep()
		{
			Value += Step;
		}

		/// <summary>
		/// Gets the value to paint.
		/// </summary>
		private int ValueToPaint(int inValue)
		{
			return (int)(inValue / (double)m_MaxValue * ClientRectangle.Width);
		}

		/// <summary>
		/// Paints the foreground.
		/// </summary>
		protected override void PaintForeground(PaintEventArgs e)
		{
			// Don't call base class to minimize flicker
			var progressBarRectangle = e.ClipRectangle;
			var valueToPaint = ValueToPaint(m_Value);

			// Paint the progress
			if (valueToPaint > 0)
			{
				progressBarRectangle.Width = Math.Min(valueToPaint, e.ClipRectangle.Width);
				e.Graphics.FillRectangle(Brush, progressBarRectangle);
			}
		}

		/// <summary>
		/// Raises the paint background event.
		/// </summary>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			base.OnPaintBackground(e);

			var progressBarRectangle = e.ClipRectangle;
			var valueToPaint = ValueToPaint(m_Value);

			if (valueToPaint < e.ClipRectangle.Width)
			{
				progressBarRectangle.Width = e.ClipRectangle.Width - valueToPaint;
				progressBarRectangle.X = valueToPaint > 0 ? valueToPaint + 1 : 0;
				e.Graphics.FillRectangle(BackgroundBrush, progressBarRectangle);
			}
		}
	}
}