// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwPopup : UserControl
	{
		#region Member variables
		/// <summary>handles event when mouse enters the popup</summary>
		public event EventHandler MouseEntered;
		/// <summary>handles event when mouse leaves popup</summary>
		public event EventHandler MouseLeft;
		/// <summary>handles event when the popup is opening</summary>
		public event EventHandler Opening;
		/// <summary>handles event when the popup opens</summary>
		public event EventHandler Opened;
		/// <summary>handles event when popup closes</summary>
		public event ToolStripDropDownClosedEventHandler Closed;

		/// <summary>flag whether the mouse is within the bounds of the popup</summary>
		protected bool m_mouseOver = false;
		/// <summary>flag indicated whether the mouses movement should be monitored</summary>
		protected bool m_monitorMouseOver = true;
		/// <summary>updates m_mouseOver when m_monitorMouseOver is true</summary>
		protected Timer m_timer;
		/// <summary>host for this custom control</summary>
		protected ToolStripControlHost m_host;
		/// <summary>owns the host--drop down containing the host that hosts the custom control</summary>
		protected ToolStripDropDown m_owningDropDown;
		#endregion

		#region Construct/Destruct
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwPopup"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwPopup()
		{
			InitializeComponent();
			base.DoubleBuffered = true;

			if (DesignMode)
				return;

			base.Dock = DockStyle.Fill;

			m_host = new ToolStripControlHost(this);
			m_host.Padding = Padding.Empty;
			m_host.Margin = Padding.Empty;
			m_host.AutoSize = false;
			m_host.Size = Size;
			m_host.Dock = DockStyle.Fill;

			m_owningDropDown = new ToolStripDropDown();
			m_owningDropDown.Padding = Padding.Empty;
			m_owningDropDown.AutoSize = false;
			m_owningDropDown.LayoutStyle = ToolStripLayoutStyle.Table;
			m_owningDropDown.Size = Size;
			m_owningDropDown.Items.Add(m_host);
			m_owningDropDown.VisibleChanged += m_owningDropDown_VisibleChanged;
			m_owningDropDown.Opened += m_owningDropDown_Opened;
			m_owningDropDown.Closed += m_owningDropDown_Closed;
			m_owningDropDown.Opening += m_owningDropDown_Opening;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise,
		/// false.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && (components != null))
			{
				components.Dispose();

				if (m_timer != null)
				{
					m_timer.Stop();
					m_timer.Dispose();
					m_timer = null;
				}

				if (m_owningDropDown != null)
				{
					m_owningDropDown.Dispose();
					m_owningDropDown = null;
				}

				if (m_host != null)
				{
					m_host.Dispose();
					m_host = null;
				}
			}

			base.Dispose(disposing);
		}
		#endregion

		#region Methods to show/hide the popup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the popup at the specified screen location.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Show(Point screenLocation)
		{
			m_owningDropDown.Show(screenLocation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the popup at the specified location (which is relative to ctrl) with the
		/// specified owning control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Show(Control ctrl, Point location)
		{
			if (ctrl == null)
				m_owningDropDown.Show(location);
			else
				m_owningDropDown.Show(ctrl, location);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides the popup.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new virtual void Hide()
		{
			m_owningDropDown.Hide();
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the mouse is over the panel or any
		/// controls in its control collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsMouseOver
		{
			get {return m_mouseOver && m_owningDropDown.Visible;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the owning drop-down control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ToolStripDropDown OwningDropDown
		{
			get { return m_owningDropDown; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the panel should keep track of
		/// when the mouse is over it or any controls contained therein.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MonitorMouseOver
		{
			get { return m_monitorMouseOver; }
			set { m_monitorMouseOver = value; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the popup's right or bottom edge will extend beyond the
		/// bounds of the screen if shown at the specified location.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDesiredPopupLocation(Point ptDesired, out bool tooWide, out bool tooTall)
		{
			// Determine the popup's display rectangle based on it's desired location and size.
			Rectangle rcPopup = new Rectangle(ptDesired, Size);

			// Get the screen on which the popup will be shown. Ususally,
			// this will be the primary and only screen, since most users will
			// probably have a single monitor setup.
			Screen scrn = Screen.FromPoint(ptDesired);

			// Check if the popup will extend beyond the screen's right edge.
			tooWide = (rcPopup.Right > scrn.WorkingArea.Right);

			// Check if the popup will extend below the screen's bottom edge.
			tooTall = (rcPopup.Bottom > scrn.WorkingArea.Bottom);
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Closed event of the popup.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.ToolStripDropDownClosedEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_owningDropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			OnClosed(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Opened event of the popup.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_owningDropDown_Opened(object sender, EventArgs e)
		{
			OnOpened(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Opening event of the m_owningDropDown control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_owningDropDown_Opening(object sender, CancelEventArgs e)
		{
			OnOpening(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Opening event of the popup.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnOpening(EventArgs e)
		{
			if (Opening != null)
				Opening(this, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Closed event of the popup.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnClosed(ToolStripDropDownClosedEventArgs e)
		{
			if (Closed != null)
				Closed(this, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Opened event of the popup.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnOpened(EventArgs e)
		{
			if (Opened != null)
				Opened(this, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start and stop the timer when the owning drop-down's visibility changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_owningDropDown_VisibleChanged(object sender, EventArgs e)
		{
			if (m_owningDropDown.Visible && m_monitorMouseOver)
				InitializeTimer();
			else if (!m_owningDropDown.Visible && m_timer != null)
				m_timer.Stop();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			if (m_owningDropDown != null)
				m_owningDropDown.Size = Size;
		}
		#endregion

		#region Message handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeTimer()
		{
			if (m_timer == null)
			{
				m_timer = new Timer();
				m_timer.Tick += m_timer_Tick;
			}

			m_timer.Interval = 1;
			m_timer.Start();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_timer_Tick(object sender, EventArgs e)
		{
			OnTimerTick();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fires when the timer Tick event occurs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnTimerTick()
		{
			bool prevMouseOverValue = m_mouseOver;
			Point pt = PointToClient(MousePosition);
			m_mouseOver = ClientRectangle.Contains(pt);

			if (!m_mouseOver && prevMouseOverValue)
				OnMouseLeft(EventArgs.Empty);
			else if (m_mouseOver && !prevMouseOverValue)
				OnMouseEntered(EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fires the mouse entered event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnMouseEntered(EventArgs e)
		{
			if (MouseEntered != null)
				MouseEntered(this, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fires the MouseLeft event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnMouseLeft(EventArgs e)
		{
			if (MouseLeft != null)
				MouseLeft(this, e);
		}
		#endregion
	}
}
