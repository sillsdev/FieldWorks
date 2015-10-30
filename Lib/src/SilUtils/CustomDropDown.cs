// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace SilUtils.Controls
{
	/// ----------------------------------------------------------------------------------------
	public class CustomDropDown : ToolStripDropDown
	{
		private Timer m_tmrMouseMonitor;
		private Timer m_tmrVisibilityTimeout;
		private bool m_mouseOver;
		private bool m_autoCloseWhenMouseLeaves = true;
		private Control m_hostedControl;

		/// ------------------------------------------------------------------------------------
		public CustomDropDown()
		{
			Padding = Padding.Empty;
			AutoSize = false;
			LayoutStyle = ToolStripLayoutStyle.Table;
			DropShadowEnabled = true;
			DoubleBuffered = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			if (disposing)
			{
				if (m_hostedControl != null)
					m_hostedControl.Resize -= HandleHostedControlResize;
			}

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the drop down will automatically
		/// close several seconds after the mouse is no longer over the drop-down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AutoCloseWhenMouseLeaves
		{
			get { return m_autoCloseWhenMouseLeaves; }
			set
			{
				m_autoCloseWhenMouseLeaves = value;
				if (!value && m_tmrMouseMonitor != null)
				{
					m_tmrMouseMonitor.Stop();
					m_tmrMouseMonitor.Dispose();
					m_tmrMouseMonitor = null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a host to the drop-down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddHost(ToolStripControlHost host)
		{
			m_hostedControl = host.Control;
			Size = m_hostedControl.Size;
			m_hostedControl.Dock = DockStyle.Fill;

			host.AutoSize = false;
			host.Dock = DockStyle.Fill;
			host.Padding = Padding.Empty;
			host.Margin = Padding.Empty;
			host.Size = m_hostedControl.Size;
			Items.Add(host);

			m_hostedControl.Resize += HandleHostedControlResize;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the size of the popup as the size of the hosted control changes. This
		/// is necessary in case the hosted control can be resized by the user at runtime.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleHostedControlResize(object sender, EventArgs e)
		{
			Size = m_hostedControl.Size;
		}

		// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripControlHost gets added to Items collection and disposed there")]
		public void AddControl(Control ctrl)
		{
			if (ctrl != null)
			{
				Items.Clear();
				AddHost(new ToolStripControlHost(ctrl));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start and stop the timer when the owning drop-down's visibility changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			if (Visible && m_autoCloseWhenMouseLeaves && m_hostedControl != null)
			{
				m_hostedControl.Invalidate();
				InitializeMouseMonitorTimer();
			}
			else if (!Visible && m_tmrMouseMonitor != null)
			{
				m_tmrMouseMonitor.Stop();
				m_tmrMouseMonitor.Dispose();
				m_tmrMouseMonitor = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		private void InitializeMouseMonitorTimer()
		{
			m_tmrMouseMonitor = new Timer();
			m_tmrMouseMonitor.Interval = 1;
			m_tmrMouseMonitor.Tick += m_tmrMouseMonitor_Tick;
			m_tmrMouseMonitor.Start();
		}

		/// ------------------------------------------------------------------------------------
		private void m_tmrMouseMonitor_Tick(object sender, EventArgs e)
		{
			bool prevMouseOverValue = m_mouseOver;
			Point pt = m_hostedControl.PointToClient(MousePosition);
			m_mouseOver = m_hostedControl.ClientRectangle.Contains(pt);

			if (!m_mouseOver && prevMouseOverValue)
			{
				// The mouse has left the popup so setup a timer to make it disappear
				// in 2 seconds if the mouse doesn't come back over it sooner.
				InitializeVisibilityTimeoutTimer();
			}
			else if (m_mouseOver && !prevMouseOverValue && m_tmrVisibilityTimeout != null)
			{
				// The mouse has come back over the popup so
				// terminate the visibility timeout timer.
				m_tmrVisibilityTimeout.Stop();
				m_tmrVisibilityTimeout = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		private void InitializeVisibilityTimeoutTimer()
		{
			m_tmrVisibilityTimeout = new Timer();
			m_tmrVisibilityTimeout.Interval = 2000;
			m_tmrVisibilityTimeout.Tick += m_tmrVisibilityTimeout_Tick;
			m_tmrVisibilityTimeout.Start();
		}

		/// ------------------------------------------------------------------------------------
		void m_tmrVisibilityTimeout_Tick(object sender, EventArgs e)
		{
			m_tmrVisibilityTimeout.Stop();
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnOpening(System.ComponentModel.CancelEventArgs e)
		{
			Opacity = 0;

			base.OnOpening(e);

			if (e.Cancel)
				return;

			var timer = new Timer();
			timer.Interval = 1;
			timer.Tick += delegate
			{
				Opacity += 0.1;
				if (Opacity == 1f)
					timer.Stop();
			};

			timer.Start();
		}
	}
}
