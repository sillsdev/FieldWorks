// Copyright 2007 Herre Kuijpers - <herre@xs4all.nl>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using Microsoft.Win32;
using SIL.FieldWorks.Common.Utils;

namespace ControlExtenders
{
	/// <summary>
	/// this class contains basically all the logic for making a control floating and dockable
	/// note that it is an internal class, only it's IFloaty interface is exposed to the client
	/// </summary>
	internal sealed class Floaty : Form, IFloaty
	{
		#region a teeny weeny tiny bit of API functions used
		private const int WM_NCLBUTTONDBLCLK = 0x00A3;
		private const int WM_LBUTTONUP = 0x0202;
		private const int WM_SYSCOMMAND = 0x0112;
		private const int SC_MOVE = 0xF010;

		// NOTE: I don't like using API's in .Net... so I try to avoid them if possible.
		// this time there was no way around it.

		// this function is used to be able to send some very specific (uncommon) messages
		// to the floaty forms. It is used particularly to switch between start dragging a docked panel
		// to dragging a floaty form.
		[DllImport("User32.dll", EntryPoint = "SendMessage")]
		private static extern int SendMessage(int hWnd, int Msg, int wParam, int lParam);
		#endregion private members

		#region private members

		// this is the orgigional state of the panel. This state is used to reset a control to its
		// origional state if it was floating
		private DockState m_dockState;

		// this is a flag that indicates if a control can start floating
		private bool m_startFloating;

		/// <summary>
		/// flag set when we suspend layout of our original parent (and cleared when we successfully resume).
		/// </summary>
		private bool m_parentLayoutIsSuspended;

		// indicates if the container is floating or docked
		private bool m_isFloating;

		// this is the dockmananger that manages this floaty.
		private DockExtender m_dockExtender;

		private bool m_dockOnHostOnly = true;
		private bool m_dockOnInside = true;
		private bool m_hideHandle;
		private AnchorStyles m_allowedDocking = AnchorStyles.Left | AnchorStyles.Top |
			AnchorStyles.Right | AnchorStyles.Bottom;
		private bool m_fAllowFloating = true;

		private Persistence m_persistence;
		private bool m_ignoreSaveSettings = false;

		private string m_regValuePrefix = string.Empty;
		private DockStyle m_defaultLocation = DockStyle.Left;
		private RegistryKey m_settingsKey = null;
		#endregion private members

		#region initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="DockExtender">requires the DockExtender</param>
		/// ------------------------------------------------------------------------------------
		public Floaty(DockExtender DockExtender)
		{
			m_dockExtender = DockExtender;
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// Floaty
			//
			this.ClientSize = new System.Drawing.Size(178, 122);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.Name = "Floaty";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.ResumeLayout(false);
		}

		#endregion initialization

		#region properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value that prefixes all registry value settings for the floaty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string RegistryValuePrefix
		{
			get { return m_regValuePrefix; }
			set { m_regValuePrefix = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating the default dock location if the previous dock
		/// location isn't found in the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DockStyle DefaultLocation
		{
			get { return m_defaultLocation; }
			set { m_defaultLocation = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the state of the dock.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DockState DockState
		{
			get { return m_dockState; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// indicates if a floaty may dock only on the host docking control (e.g. the form)
		/// and not inside other floaties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DockOnHostOnly
		{
			get { return m_dockOnHostOnly; }
			set { m_dockOnHostOnly = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// indicates if a floaty may dock on the inside or on the outside of a form/control
		/// default is true
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DockOnInside
		{
			get { return m_dockOnInside; }
			set { m_dockOnInside = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value that indicates whether to hide the handle when floating or not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HideHandle
		{
			get { return m_hideHandle; }
			set { m_hideHandle = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value that indicates where the floaty might be docked. Default is
		/// Left | Top | Right | Bottom.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AnchorStyles AllowedDocking
		{
			get { return m_allowedDocking; }
			set { m_allowedDocking = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the size of the floating container.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Rectangle FloatingContainerBounds
		{
			get { return m_dockState.OrgFloatingBounds; }
			set { m_dockState.OrgFloatingBounds = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to show or hide the splitter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowSplitter
		{
			get { return m_dockState.Splitter.Visible; }
			set
			{
				m_dockState.Splitter.SplitterMoved -= new SplitterEventHandler(Splitter_SplitterMoved);
				m_dockState.Splitter.Visible = value;

				if (m_dockState.Splitter.Visible)
					m_dockState.Splitter.SplitterMoved += new SplitterEventHandler(Splitter_SplitterMoved);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to show or hide the splitter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Splitter_SplitterMoved(object sender, SplitterEventArgs e)
		{
			if (m_dockState.Splitter.Visible && SplitterMoved != null)
				SplitterMoved(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether to show the close button (red X) in the title bar
		/// of the floaty.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool ShowCloseButton
		{
			set { ControlBox = value; }
		}
		#endregion properties

		#region overrides
		/// ------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_NCLBUTTONDBLCLK) // doubleclicked on border, so reset.
				DockFloaty();

			base.WndProc(ref m);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnResizeEnd(EventArgs e)
		{
			// Now allow parent to resize
			ResumeParentLayoutIfSuspended();

			if (m_dockExtender.Overlay.Visible == true && m_dockExtender.Overlay.DockHostControl != null) //ok found new docking position
			{
				m_dockState.OrgDockingParent = m_dockExtender.Overlay.DockHostControl;
				m_dockState.OrgDockStyle = m_dockExtender.Overlay.Dock;
				m_dockExtender.Overlay.Hide();
				DockFloaty(); // dock the container
			}

			m_dockExtender.Overlay.DockHostControl = null;
			m_dockExtender.Overlay.Hide();
			base.OnResizeEnd(e);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnMove(EventArgs e)
		{
			if (IsDisposed) return;

			Point pt = Cursor.Position;
			Point pc = PointToClient(pt);
			if (pc.Y < -21 || pc.Y > 0) return;
			if (pc.X < -1 || pc.X > Width) return;

			Control t = m_dockExtender.FindDockHost(this, pt);
			if (t == null)
				m_dockExtender.Overlay.Hide();
			else
				SetOverlay(t, pt);

			base.OnMove(e);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			e.Cancel = true;
			this.Hide(); // hide but don't close
			base.OnClosing(e);
		}

		#endregion overrides

		#region public methods (implements IFloaty)
		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		void IFloaty.Hide()
		{
			if (Visible)
				base.Hide();

			m_dockState.Container.Hide();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return how the control is docked. None is used for 'floating'.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DockStyle DockMode
		{
			get	{ return (m_isFloating ? DockStyle.None : m_dockState.Container.Dock); }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public new void Show()
		{
			if (!Visible && m_isFloating)
				base.Show(m_dockState.OrgDockHost);

			m_dockState.Container.Show();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public new void Show(IWin32Window win)
		{
			Show();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the floaty and make control undocked
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Float()
		{
			if (m_isFloating)
				return;

			OnDockUndockBegin(new DockingEventArgs(Rectangle.Empty, DockStyle.None));
			MakeFloatable(m_dockState, Cursor.Position.X, Cursor.Position.Y);
			m_dockState.OrgDockingParent.ResumeLayout(true);
			OnDockUndockEnd(new DockingEventArgs(DesktopBounds, DockStyle.None));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Docks the control
		/// </summary>
		/// <param name="dockStyle">The dock style.</param>
		/// ------------------------------------------------------------------------------------
		void IFloaty.Dock(DockStyle dockStyle)
		{
			if (dockStyle == DockStyle.Fill)
			{
				throw new ArgumentException(
					"DockStyle.None and DockStyle.Fill are invalid values for this method");
			}

			if (dockStyle == DockStyle.None)
			{
				Float();
				return;
			}

			if (m_dockState.Container.Dock == dockStyle && !m_isFloating &&
				m_dockState.Container.Parent == m_dockState.OrgDockingParent)
			{
				// already docked in desired position
				return;
			}

			OnDockUndockBegin(
				new DockingEventArgs((m_isFloating ? DesktopBounds : Rectangle.Empty), dockStyle));

			m_dockState.OrgDockStyle = dockStyle;
			if (m_dockState.OrgDockingParent == null)
				m_dockState.OrgDockingParent = m_dockState.Container.Parent;

			DockFloaty();
			OnDockUndockEnd(new DockingEventArgs(Rectangle.Empty, dockStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating the user can make the window floating.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowFloating
		{
			get { return m_fAllowFloating; }
			set
			{
				m_startFloating = false;
				m_fAllowFloating = value;
			}
		}

		#endregion

		#region helper functions - this contains most of the logic
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// determines the client area of the control. The area of docked controls are excluded
		/// </summary>
		/// <param name="c">the control to which to determine the client area</param>
		/// <returns>
		/// returns the docking area in screen coordinates
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private Rectangle GetDockingArea(Control c)
		{
			Rectangle r = c.Bounds;

			if (c.Parent != null)
				r = c.Parent.RectangleToScreen(r);

			Rectangle rc = c.ClientRectangle;

			int borderwidth = (r.Width - rc.Width) / 2;
			r.X += borderwidth;
			r.Y += (r.Height - rc.Height) - borderwidth;

			if (!m_dockOnInside)
			{
				rc.X += r.X;
				rc.Y += r.Y;
				return rc;
			}

			foreach (Control cs in c.Controls)
			{
				if (!cs.Visible) continue;
				switch (cs.Dock)
				{
					case DockStyle.Left:
						rc.X += cs.Width;
						rc.Width -= cs.Width;
						break;
					case DockStyle.Right:
							rc.Width -= cs.Width;
						break;
					case DockStyle.Top:
						rc.Y += cs.Height;
						rc.Height -= cs.Height;
						break;
					case DockStyle.Bottom:
							rc.Height -= cs.Height;
						break;
					default:
						break;
				}
			}
			rc.X += r.X;
			rc.Y += r.Y;

			//Console.WriteLine("Client = " + c.Name + " " + rc.ToString());

			return rc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method will check if the overlay needs to be displayed or not
		/// for display it will position the overlay
		/// </summary>
		/// <param name="c"></param>
		/// <param name="pc">position of cursor in screen coordinates</param>
		/// ------------------------------------------------------------------------------------
		private void SetOverlay(Control c, Point pc)
		{
			Rectangle r = GetDockingArea(c);
			Rectangle rc = r;

			//determine relative coordinates
			float rx = (pc.X - r.Left) / (float)(r.Width);
			float ry = (pc.Y - r.Top) / (float)(r.Height);

			//Console.WriteLine("Moving over " + c.Name + " " +  rx.ToString() + "," + ry.ToString());

			m_dockExtender.Overlay.Dock = DockStyle.None; // keep floating

			// this section determines when the overlay is to be displayed.
			// it depends on the position of the mouse cursor on the client area.
			// the overlay is currently only shown if the mouse is moving over either the Northern, Western,
			// Southern or Eastern parts of the client area.
			// when the mouse is in the center or in the NE, NW, SE or SW, no overlay preview is displayed, hence
			// allowing the user to dock the container.

			// dock to left, checks the Western area
			if (rx > 0 && rx < ry && rx < 0.25 && ry < 0.75 && ry > 0.25 &&
				(m_allowedDocking & AnchorStyles.Left) != 0)
			{
				if (m_dockState.OrgWidth <= 0)
					r.Width = r.Width / 2;
				else
				{
					r.Width = m_dockState.OrgWidth;
					if (r.Width > Width / 2)
						r.Width = Width / 2;
				}

				if (r.Width > this.Width)
					r.Width = this.Width;

				m_dockExtender.Overlay.Dock = DockStyle.Left; // dock to left
			}

			// dock to the right, checks the Easter area
			if (rx < 1 && rx > ry && rx > 0.75 && ry < 0.75 && ry > 0.25 &&
				(m_allowedDocking & AnchorStyles.Right) != 0)
			{
				if (m_dockState.OrgWidth <= 0)
					r.Width = r.Width / 2;
				else
				{
					r.Width = m_dockState.OrgWidth;
					if (r.Width > Width / 2)
						r.Width = Width / 2;
				}

				if (r.Width > this.Width)
					r.Width = this.Width;

				r.X = rc.X + rc.Width - r.Width;
				m_dockExtender.Overlay.Dock = DockStyle.Right;
			}

			// dock to top, checks the Northern area
			if (ry > 0 && ry < rx && ry < 0.25 && rx < 0.75 && rx > 0.25 &&
				(m_allowedDocking & AnchorStyles.Top) != 0)
			{
				if (m_dockState.OrgHeight <= 0)
					r.Height = r.Height / 2;
				else
				{
					r.Height = m_dockState.OrgHeight;
					if (r.Height > Height / 2)
						r.Height = Height / 2;
				}

				if (r.Height > this.Height)
					r.Height = this.Height;

				m_dockExtender.Overlay.Dock = DockStyle.Top;
			}

			// dock to the bottom, checks the Southern area
			if (ry < 1 && ry > rx && ry > 0.75 && rx < 0.75 && rx > 0.25 &&
				(m_allowedDocking & AnchorStyles.Bottom) != 0)
			{
				if (m_dockState.OrgHeight <= 0)
					r.Height = r.Height / 2;
				else
				{
					r.Height = m_dockState.OrgHeight;
					if (r.Height > Height / 2)
						r.Height = Height / 2;
				}

				if (r.Height > this.Height)
					r.Height = this.Height;
				r.Y = rc.Y + rc.Height - r.Height;
				m_dockExtender.Overlay.Dock = DockStyle.Bottom;
			}

			if (m_dockExtender.Overlay.Dock != DockStyle.None)
				m_dockExtender.Overlay.Bounds = r;
			else
				m_dockExtender.Overlay.Hide();

			if (!m_dockExtender.Overlay.Visible && m_dockExtender.Overlay.Dock != DockStyle.None)
			{
				m_dockExtender.Overlay.DockHostControl = c;
				m_dockExtender.Overlay.Show(m_dockState.OrgDockHost);
				BringToFront();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attaches to the dock state.
		/// </summary>
		/// <param name="dockState">State of the dock.</param>
		/// ------------------------------------------------------------------------------------
		internal void Attach(DockState dockState)
		{
			// track the handle's mouse movements
			m_dockState = dockState;
			Text = m_dockState.Handle.Text;
			m_dockState.Handle.MouseUp += Handle_MouseUp;
			m_dockState.Handle.MouseMove += Handle_MouseMove;
			m_dockState.Handle.MouseHover += Handle_MouseHover;
			m_dockState.Handle.MouseLeave += Handle_MouseLeave;

			//m_dockState.Container.SizeChanged += HandleDockStateContainerSizeChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		void HandleDockStateContainerSizeChanged(object sender, EventArgs e)
		{
			SaveSettings();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void OnDockUndockBegin(DockingEventArgs args)
		{
			m_dockState.Container.SizeChanged -= HandleDockStateContainerSizeChanged;

			if (DockUndockBegin != null)
				DockUndockBegin(this, args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void OnDockUndockEnd(DockingEventArgs args)
		{
			if (DockUndockEnd != null)
				DockUndockEnd(this, args);

			m_dockState.Container.SizeChanged += HandleDockStateContainerSizeChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// makes the docked control floatable in this Floaty form
		/// </summary>
		/// <param name="dockState">State of the dock.</param>
		/// <param name="offsetx">The offset in x direction from left of container.</param>
		/// <param name="offsety">The offset in y direction from top of container.</param>
		/// <remarks>This method calls SuspendLayout on the control's parent.</remarks>
		/// ------------------------------------------------------------------------------------
		private void MakeFloatable(DockState dockState, int offsetx, int offsety)
		{
			if (m_isFloating)
				return;

			Point location = Cursor.Position;
			m_dockState = dockState;
			Text = m_dockState.Handle.Text;

			Size containerSize = m_dockState.OrgFloatingBounds.Size;
			if (m_dockState.Container.Equals(m_dockState.Handle))
			{
				// REVIEW: what are these values?
				containerSize.Width += 18;
				containerSize.Height += 28;
			}
			else
			{
				containerSize.Width += 2 * SystemInformation.FrameBorderSize.Width;
				containerSize.Height += SystemInformation.ToolWindowCaptionHeight +
					2 * SystemInformation.FrameBorderSize.Height;
			}
			if (containerSize.Width > 600)
				containerSize.Width = 600;
			if (containerSize.Height > 600)
				containerSize.Height = 600;

			// We try to put the left/top corner of the floaty at the location of the
			// container. However, if we'll display the container too small, we have
			// to move the top/left corner so that the Cursor.Position is inside of the
			// floaty window title bar.
			location.X -= Math.Min(Math.Max(0, offsetx), containerSize.Width);
			location.Y -= Math.Min(Math.Max(0, offsety), containerSize.Height);

			Bounds = new Rectangle(location, containerSize);

			m_dockState.OrgDockingParent = m_dockState.Container.Parent;
			m_dockState.OrgDockStyle = m_dockState.Container.Dock;

			if (m_dockState.Container.Dock == DockStyle.Left || m_dockState.Container.Dock == DockStyle.Right)
				m_dockState.OrgWidth = m_dockState.Container.Width;
			else
				m_dockState.OrgHeight = m_dockState.Container.Height;

			if (m_hideHandle)
				m_dockState.Handle.Hide();

			// Prevent resizing of parent until the user releases mouse button
			m_dockState.OrgDockingParent.SuspendLayout();
			m_parentLayoutIsSuspended = true;

			m_dockState.Container.Parent = this;
			m_dockState.Container.Dock = DockStyle.Fill;

			if (m_dockState.Splitter != null)
			{
				m_dockState.Splitter.Visible = false; // hide splitter
				m_dockState.Splitter.Parent = this;
			}
			// allow redraw of floaty and container
		   // Application.DoEvents();

			// this is kind of tricky
			// disable the mousemove events of the handle
			SendMessage(m_dockState.Handle.Handle.ToInt32(), WM_LBUTTONUP, 0, 0);

			m_isFloating = true;

			// If we're not undocking via dragging the window away from the host, then
			// place the window where it was the last time it was floating. If we called
			// the PlaceUndockedWindow method even when dragging window from it's host
			// then as the user dragged the window, it would snap to some position that
			// is likely unexpected.
			if (!m_startFloating)
				PlaceUndockedWindow();

			Show();

			if (Undocked != null)
				Undocked(this, EventArgs.Empty);

			// enable the mousemove events of the new floating form, start dragging the form immediately
			SendMessage(this.Handle.ToInt32(), WM_SYSCOMMAND, SC_MOVE | 0x02, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// this will dock the floaty control
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DockFloaty()
		{
			// Before docking, save the settings of the current dock mode so
			// they can be restored if the user goes back to docking it this way.
			SaveDockSettings(SettingsKey, DockMode);

			// bring dockhost to front first to prevent flickering
			m_dockState.OrgDockHost.TopLevelControl.BringToFront();
			if (m_isFloating)
				m_dockState.OrgFloatingBounds = m_dockState.Container.Bounds;

			this.Hide();

			m_dockState.Container.SizeChanged -= HandleDockStateContainerSizeChanged;
			m_dockState.Container.Visible = false; // hide it temporarely
			m_dockState.Container.Parent = m_dockState.OrgDockingParent;
			m_dockState.Container.Dock = m_dockState.OrgDockStyle;

			if ((m_dockState.OrgDockStyle == DockStyle.Left || m_dockState.OrgDockStyle == DockStyle.Right) &&
				m_dockState.OrgWidth > 0)
			{
				m_dockState.Container.Width = m_dockState.OrgWidth;
			}
			else if (m_dockState.OrgHeight > 0)
				m_dockState.Container.Height = m_dockState.OrgHeight;

			m_dockState.Container.SizeChanged += HandleDockStateContainerSizeChanged;
			LoadDockedXYSettings(m_dockState.OrgDockStyle);
			m_dockState.Handle.Visible = true; // show handle again
			m_dockState.Container.Visible = true; // it's good, show it

			if (m_dockOnInside)
				m_dockState.Container.BringToFront(); // set to front

			//show splitter
			if (m_dockState.Splitter != null && m_dockState.OrgDockStyle != DockStyle.Fill &&
				m_dockState.OrgDockStyle != DockStyle.None)
			{
				m_dockState.Splitter.Parent = m_dockState.OrgDockingParent;
				m_dockState.Splitter.Dock = m_dockState.OrgDockStyle;
				m_dockState.Splitter.Visible = true; // show splitter

				if (m_dockOnInside)
					m_dockState.Splitter.BringToFront();
				else
					m_dockState.Splitter.SendToBack();
			}

			if (!m_dockOnInside)
				m_dockState.Container.SendToBack(); // set to back

			m_isFloating = false;

			if (Docking != null)
			{
				Docking(this, new DockingEventArgs(m_dockState.OrgFloatingBounds,
					m_dockState.OrgDockStyle));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detaches the handle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DetachHandle()
		{
			m_dockState.Handle.MouseMove -= new MouseEventHandler(Handle_MouseMove);
			m_dockState.Handle.MouseHover -= new EventHandler(Handle_MouseHover);
			m_dockState.Handle.MouseLeave -= new EventHandler(Handle_MouseLeave);
			m_dockState.Container = null;
			m_dockState.Handle = null;
		}

		#endregion helper functions

		#region Container Handle tracking methods
		void Handle_MouseHover(object sender, EventArgs e)
		{
			m_startFloating = m_fAllowFloating;
		}

		void Handle_MouseLeave(object sender, EventArgs e)
		{
			m_startFloating = false;
			// See comment on Handle_MouseUp. We can miss the mouse up because the handle moves
			// when we start floating, so the mouse up happens in the title bar, not the handle.
			ResumeParentLayoutIfSuspended();
		}

		void Handle_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && m_startFloating && m_fAllowFloating)
				MakeFloatable(m_dockState, e.X, e.Y);
		}

		// There is a pathological case where MakeFloatable suspends the parent's layout,
		// but we never get onResizeEnd to re-enable it. (One way is to click on the handle
		// when it is docked and ANOTHER window is active, and not move the mouse. This
		// makes the floaty float (we get an apparently spurious mouse move event) but
		// we get no resume, so things are left in a weird state.)
		// We REALLY don't want to leave the parent permanently in a state of suspended layout.
		void Handle_MouseUp(object sender, MouseEventArgs e)
		{
			ResumeParentLayoutIfSuspended();
		}

		private void ResumeParentLayoutIfSuspended()
		{
			if (m_parentLayoutIsSuspended)
			{
				m_parentLayoutIsSuspended = false;
				m_dockState.OrgDockingParent.ResumeLayout();
			}
		}
		#endregion Container Handle tracking methods

		#region Persistence stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the settings key where floaty registry settings will be saved.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RegistryKey SettingsKey
		{
			get
			{
				return (m_settingsKey ??
					(m_persistence != null ? m_persistence.SettingsKey : null));
			}
			set { m_settingsKey = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the floaty's persistence object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Persistence Persistence
		{
			get { return m_persistence; }
			set
			{
				if (m_persistence != null)
				{
					m_persistence.LoadSettings -= OnLoadSettings;
					m_persistence.SaveSettings -= OnSaveSettings;
				}

				m_persistence = value;

				if (m_persistence != null)
				{
					m_persistence.LoadSettings += OnLoadSettings;
					m_persistence.SaveSettings += OnSaveSettings;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnLoadSettings(RegistryKey key)
		{
			LoadSettings(key);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void LoadSettings()
		{
			LoadSettings(SettingsKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void LoadSettings(RegistryKey key)
		{
			if (key == null)
				return;

			string dockLoc = key.GetValue(ModifiedRegValue("DockedLocation"),
				m_defaultLocation.ToString()) as string;

			DockStyle dock;
			try
			{
				dock = (DockStyle)Enum.Parse(typeof(DockStyle), dockLoc);
			}
			catch
			{
				dock = DockStyle.Left;
			}

			LoadDockedXYSettings(dock);
			m_ignoreSaveSettings = true;
			((IFloaty)this).Dock(dock);
			m_ignoreSaveSettings = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadDockedXYSettings(DockStyle dock)
		{
			RegistryKey key = SettingsKey;
			if (key == null || m_dockState.Container == null)
				return;

			m_dockState.Container.SizeChanged -= HandleDockStateContainerSizeChanged;

			if (dock == DockStyle.Top || dock == DockStyle.Bottom)
			{
				int val = (int)key.GetValue(ModifiedRegValue("DockedHeight"), 0);
				if (val > 0)
					m_dockState.Container.Height = val;
			}
			else
			{
				int val = (int)key.GetValue(ModifiedRegValue("DockedWidth"), 0);
				if (val > 0)
					m_dockState.Container.Width = val;
			}

			m_dockState.Container.SizeChanged += HandleDockStateContainerSizeChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnSaveSettings(RegistryKey key)
		{
			SaveSettings(key);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveSettings()
		{
			SaveSettings(SettingsKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveSettings(RegistryKey key)
		{
			SaveDockSettings(key, DockMode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the dock window's settings for the specified dock style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveDockSettings(RegistryKey key, DockStyle dock)
		{
			key = (key ?? SettingsKey);
			if (m_ignoreSaveSettings || key == null)
				return;

			if (m_dockState.Container != null)
			{
				if (dock == DockStyle.Bottom || dock == DockStyle.Top)
					key.SetValue(ModifiedRegValue("DockedHeight"), m_dockState.Container.Height);
				else if (dock == DockStyle.Left || dock == DockStyle.Right)
					key.SetValue(ModifiedRegValue("DockedWidth"), m_dockState.Container.Width);
			}

			if (dock == DockStyle.None)
				key.SetValue(ModifiedRegValue("UndockedBounds"), DesktopBounds);

			key.SetValue(ModifiedRegValue("DockedLocation"), dock);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is called just before this form is shown in undocked mode. It will
		/// read the registry to determine it's undocked size and location.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PlaceUndockedWindow()
		{
			RegistryKey key = SettingsKey;
			if (key == null)
				return;

			string bounds = key.GetValue(ModifiedRegValue("UndockedBounds"), null) as string;

			if (bounds == null)
				key.SetValue(ModifiedRegValue("UndockedBounds"), DesktopBounds);
			else
			{
				Rectangle rc = MiscUtils.GetRcFromString(bounds);
				if (rc != Rectangle.Empty)
					DesktopBounds = rc;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string ModifiedRegValue(string baseValue)
		{
			return m_regValuePrefix + baseValue;
		}

		#endregion

		#region events
		public event DockingEventHandler Docking;
		public event EventHandler Undocked;
		public event SplitterEventHandler SplitterMoved;
		public event DockingEventHandler DockUndockBegin;
		public event DockingEventHandler DockUndockEnd;
		#endregion
	}

	/// <summary>
	/// define a Floaty collection used for enumerating all defined floaties
	/// </summary>
	public class Floaties : List<IFloaty>
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the specified container.
		/// </summary>
		/// <param name="container">The container.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IFloaty Find(Control container)
		{
			foreach (Floaty f in this)
			{
				if (f.DockState.Container.Equals(container))
					return f;
			}
			return null;
		}
	}

}
