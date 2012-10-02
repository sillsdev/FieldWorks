// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CollapsibleSplitter.cs
// Responsibility: TeTeam
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for CollapsibleSplitter.
	/// </summary>
	public class CollapsibleSplitter: IDisposable
	{
		/// <summary>Fired after the user has moved the splitter</summary>
		/// <param name="sender">The sender</param>
		/// <param name="splitterTop">The new top of the splitter (relative to the top of the
		/// parent control)</param>
		public delegate void SplitterMovedDelegate(object sender, int splitterTop);
		/// <summary>Fired after the user has moved the splitter</summary>
		public event SplitterMovedDelegate SplitterMoved;

		private const int kPaddingForCollapsedLine = 4;
		private Control m_parent;
		private bool m_resizeInProcess;
		private SizingLine m_sizingLine;
		private bool m_fVisible;
		private Rectangle m_sizingRectangle;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CollapsibleSplitter"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// ------------------------------------------------------------------------------------
		public CollapsibleSplitter(Control parent)
		{
			m_parent = parent;
			m_parent.MouseDown += new MouseEventHandler(OnParentMouseDown);
			m_parent.MouseMove += new MouseEventHandler(OnParentMouseMove);
			m_parent.MouseUp += new MouseEventHandler(OnParentMouseUp);
			m_parent.SizeChanged += new EventHandler(OnParentSizeChanged);
			m_parent.Paint += new PaintEventHandler(OnParentPaint);
			Visible = true;
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~CollapsibleSplitter()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_sizingLine != null)
					m_sizingLine.Dispose();
			}
			m_sizingLine = null;
			IsDisposed = true;
		}
		#endregion

		#region properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:CollapsibleSplitter"/>
		/// is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Visible
		{
			get { return m_fVisible; }
			set
			{
				m_fVisible = value;
				m_parent.SuspendLayout();
				m_parent.Padding = new Padding(0, (m_fVisible ? kPaddingForCollapsedLine : 0),
					0, 0);
				m_parent.ResumeLayout(true);
			}
		}
		#endregion

		#region event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user has moved the splitter.
		/// </summary>
		/// <param name="top">The top.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnSplitterMoved(int top)
		{
			if (SplitterMoved != null)
				SplitterMoved(this, top);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Paint event of the m_parent control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.PaintEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void OnParentPaint(object sender, PaintEventArgs e)
		{
			if (m_fVisible)
			{
				e.Graphics.FillRectangle(SystemBrushes.Window, e.ClipRectangle);
				ControlPaint.DrawButton(e.Graphics, m_sizingRectangle, ButtonState.Normal);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SizeChanged event of the m_parent control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnParentSizeChanged(object sender, EventArgs e)
		{
			// Invalidate the old sizing rectangle
			m_parent.Invalidate(m_sizingRectangle);
			// now calculate the new one
			int newHeight = (m_parent.Padding.Top > 0 ? m_parent.Padding.Top : 1);
			m_sizingRectangle = new Rectangle(m_parent.Width - SystemInformation.VerticalScrollBarWidth,
				0, SystemInformation.VerticalScrollBarWidth, newHeight);
			// Invalidate the new location as well
			m_parent.Invalidate(m_sizingRectangle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If terminating a resize process, then resize the container panel based on where
		/// the user left the sizing line.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnParentMouseUp(object sender, MouseEventArgs e)
		{
			m_parent.Cursor = Cursors.Default;

			if (m_resizeInProcess)
			{
				m_resizeInProcess = false;
				OnSplitterMoved(m_sizingLine.Top);
				m_parent.Controls.Remove(m_sizingLine);
				m_sizingLine.Dispose();
				m_sizingLine = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the MouseMove event of the m_parent control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnParentMouseMove(object sender, MouseEventArgs e)
		{
			// Show the sizing cursor if the mouse is over the edge of the container panel.
			if (m_sizingRectangle.Contains(e.Location) && m_parent.Cursor != Cursors.HSplit)
				m_parent.Cursor = Cursors.HSplit;
			else if (!m_sizingRectangle.Contains(e.Location) && m_parent.Cursor != Cursors.Default)
				m_parent.Cursor = Cursors.Default;

			// Move the sizing line when in the process of resizing.
			if (m_resizeInProcess)
				m_sizingLine.Top = e.Y >= 0 ? e.Y : 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the mouse goes down over the edge of the container panel, then put the user
		/// in the resize mode.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnParentMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && m_sizingRectangle.Contains(e.Location))
			{
				m_resizeInProcess = true;
				m_sizingLine = new SizingLine(m_parent.ClientSize.Width, kPaddingForCollapsedLine);
				m_sizingLine.Location = new Point(0, 0);
				m_sizingLine.Visible = true;
				m_parent.Controls.Add(m_sizingLine);
				m_sizingLine.BringToFront();
			}
		}
		#endregion
	}
}
