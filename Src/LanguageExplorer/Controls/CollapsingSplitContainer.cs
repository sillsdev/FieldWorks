// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// This class extends SplitContainer by adding buttons which replace a nearly collapsed
	/// control (Panel1 or Panel2 child). When the SplitterDistance gets low enough, then the
	/// button becomes top most, which hides the main control.
	///
	/// Clicking on the button will restore the previous SplitterDistance, and show the main control again.
	///
	/// We will still support complete collapsing of a main control, of course, but client code
	/// will have to do that.
	/// </summary>
	public partial class CollapsingSplitContainer : SplitContainer, ICollapsingSplitContainer, IPostLayoutInit
	{
		/// <summary />
		public const int kCollapsedSize = 16;
		/// <summary />
		public const int kCollapseZone = 35;

		/// <summary />
		protected int m_firstCollapseZone = kCollapseZone;
		/// <summary />
		protected int m_secondCollapseZone = kCollapseZone;

		private const int kIconOffset = 10;
		private const int kLabelOffset = kIconOffset;

		#region Data Members (non-Designer)

		// The control with the icon.
		private UserControl m_firstIconControl;
		// The control with the icon.
		private UserControl m_secondIconControl;
		private Control m_firstMainControl;
		private Control m_secondMainControl;
		private Control m_firstFrontedControl;
		private Control m_secondFrontedControl;
		// Labels to show on the button when one pane is minimized.
		private string m_firstLabel = "";
		private string m_secondLabel = "";
		// Expand Button images in the four possible orientations.
		private readonly Image[] m_expandSplitterIcons = new Image[4];
		#endregion Data Members (non-Designer)

		#region Construction

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CollapsingSplitContainer"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CollapsingSplitContainer()
		{
			InitializeComponent();
			if (AccessibleName == null)
				AccessibleName = "CollapsingSplitContainer";

			// These buttons are set for Fill Dock.
			// Another main control can be added,
			// and it will be set for Fill in an event handler here.
			// Only one of the two controls will actually be used, however,
			// as they get swapped in and out of the parent control.
			SuspendLayout();

			Panel1MinSize = kCollapsedSize;
			m_firstIconControl = new UserControl();
			if (m_firstIconControl.AccessibleName == null)
				m_firstIconControl.AccessibleName = "FirstIconControl";

			m_firstIconControl.Dock = DockStyle.Fill;
			Panel1.Controls.Add(m_firstIconControl);
			m_firstIconControl.Click += m_panel1Btn_Click;
			m_firstIconControl.Paint += m_panel1Btn_Paint;
			//Panel1.ControlAdded += new ControlEventHandler(CollapsingSplitContainer_Panel_ControlAdded);

			FixedPanel = FixedPanel.Panel1;

			Panel2MinSize = kCollapsedSize;
			m_secondIconControl = new UserControl();
			if (m_secondIconControl.AccessibleName == null)
				m_secondIconControl.AccessibleName = "SecondIconControl";

			m_secondIconControl.Dock = DockStyle.Fill;
			Panel2.Controls.Add(m_secondIconControl);
			m_secondIconControl.Click += m_panel2Btn_Click;
			m_secondIconControl.Paint += m_panel2Btn_Paint;
			//Panel2.ControlAdded += new ControlEventHandler(CollapsingSplitContainer_Panel_ControlAdded);

			// make sure Panel1 and Panel2 have AccessibleNames
			if (Panel1.AccessibleName == null)
				Panel1.AccessibleName = "CSC.SplitContainer.One";
			if (Panel2 != null && Panel2.AccessibleName == null)
				Panel2.AccessibleName = "CSC.SplitContainer.Two";
			Panel2.Layout += Panel2_Layout;
			Panel2.SizeChanged += Panel2_SizeChanged;

			ResumeLayout(false);

			// Convert the one icon into its four variants.
			// Down
			Image image = m_imageList16x16.Images[0].Clone() as Image;
			image.RotateFlip(RotateFlipType.Rotate90FlipNone);
			m_expandSplitterIcons[0] = image;
			// Left
			image = m_imageList16x16.Images[0].Clone() as Image;
			image.RotateFlip(RotateFlipType.RotateNoneFlipX);
			m_expandSplitterIcons[1] = image;
			// Right
			m_expandSplitterIcons[2] = m_imageList16x16.Images[0];
			// Up
			image = m_imageList16x16.Images[0].Clone() as Image;
			image.RotateFlip(RotateFlipType.Rotate270FlipNone);
			m_expandSplitterIcons[3] = image;

			Panel1.SizeChanged += Panel1_SizeChanged;
			BackColor = Color.FromKnownColor(KnownColor.Control); // so the splitter itself is gray
			Panel1.BackColor = Color.FromKnownColor(KnownColor.Window); // the content areas should be white if not covered.
			Panel2.BackColor = Color.FromKnownColor(KnownColor.Window); // the content areas should be white if not covered.
		}

		/// <summary>
		/// Try to make sure the main control stays the same size as the panel.
		/// It is docked to fill the panel, so eventually it will be that size.
		/// But, if it's not visible, the docking doesn't take effect.
		/// We get temporary visual artifacts from painting it the wrong size as it becomes visible.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Panel2_SizeChanged(object sender, EventArgs e)
		{
			if (m_secondMainControl != null && m_secondMainControl.Size != Panel2.Size)
				m_secondMainControl.Size = Panel2.Size;
		}

		void Panel2_Layout(object sender, LayoutEventArgs e)
		{
			if (m_secondMainControl != null && m_secondMainControl.Size != Panel2.Size)
				m_secondMainControl.Size = Panel2.Size;
		}

		void Panel1_SizeChanged(object sender, EventArgs e)
		{
			int height_width = Orientation == Orientation.Vertical ? Panel1.Width : Panel1.Height;
		}

		#endregion Construction

		#region Properties

		/// <summary>
		/// Flag whether this control has been fully initialized (size, etc).
		/// </summary>
		public bool IsInitializing { get; set; } = false;

		/// <summary>
		/// The first (visible) child control.
		/// </summary>
		public Control FirstVisibleControl
		{
			get
			{
				CheckDisposed();

				if (m_firstMainControl.Parent != null)
					return m_firstMainControl;

				if (Panel1Collapsed || SplitterDistance == kCollapsedSize || m_secondMainControl.Parent != null)
					return m_secondMainControl;

				return null; // No sensible control can be found, so return null.
			}
		}

		/// <summary>
		/// The control that should not be resized in the shared dimension when resizing the window.
		/// It is always the left/top one at this point.
		/// </summary>
		/// <remarks>
		/// This will return null if the data member is null.
		/// </remarks>
		public Control FirstControl
		{
			get
			{
				CheckDisposed();

				return m_firstMainControl;
			}
			set
			{
				if (value == null)
					value = new Panel();

				value.Dock = DockStyle.Fill;
				if (Panel1.Controls.Contains(m_firstMainControl))
				{
					Panel1.SuspendLayout();
					Panel1.Controls.Remove(m_firstMainControl);
					if (!m_firstMainControl.IsDisposed)
					{
						m_firstMainControl.Dispose();
					}
					m_firstMainControl = value;
					Panel1.Controls.Add(value);
					Panel1.ResumeLayout();
				}
				else
					m_firstMainControl = value;

				ResetControl(Panel1, value);
			}
		}

		/// <summary>
		/// Set the width, which if less than the provided value, will trigger a collapse of the left/top pane.
		/// </summary>
		public int FirstCollapseZone
		{
			set
			{
				m_firstCollapseZone = GetNewCollapseZoneValue(value);
			}
		}

		/// <summary>
		/// Gets the right or bottom panel of the implementation, depending on its Orientation.
		/// </summary>
		public SplitterPanel SecondPanel
		{
			get { return Panel2; }
		}

		/// <summary>
		/// The control that should be resized in the shared dimension when resizing the window.
		/// It is always the right/bottom one at this point.
		/// </summary>
		/// <remarks>
		/// This will return null if the data member is null.
		/// </remarks>
		public Control SecondControl
		{
			get
			{
				CheckDisposed();

				return m_secondMainControl;
			}
			set
			{
				if (value == null)
					value = new Panel();

				value.Dock = DockStyle.Fill;
				if (SecondPanel.Controls.Contains(m_secondMainControl))
				{
					SecondPanel.SuspendLayout();
					SecondPanel.Controls.Remove(m_secondMainControl);
					if (!m_secondMainControl.IsDisposed)
					{
						m_secondMainControl.Dispose();
					}
					m_secondMainControl = value;
					SecondPanel.Controls.Add(value);
					SecondPanel.ResumeLayout();
				}
				else
					m_secondMainControl = value;

				ResetControl(SecondPanel, value);
			}
		}

		/// <summary>
		/// Set the width, which if less than the provided value, will trigger a collapse of the right/bottom pane.
		/// </summary>
		public int SecondCollapseZone
		{
			set
			{
				m_secondCollapseZone = GetNewCollapseZoneValue(value);
			}
		}

		private int GetNewCollapseZoneValue(int newValue)
		{
			int newZoneValue;
			using (var gr = CreateGraphics())
			{
				newZoneValue = Math.Max((int) (newValue*gr.DpiX)/MiscUtils.kdzmpInch,
					kCollapseZone);
			}
			return newZoneValue;
		}

		/// <summary>
		/// Get/Set the label on the first pane.
		/// </summary>
		public string FirstLabel
		{
			get { return m_firstLabel; }
			set
			{
				if (value == null)
					m_firstLabel = string.Empty;
				else
					m_firstLabel = value;
			}
		}

		/// <summary>
		/// Get/Set the label on the second pane.
		/// </summary>
		public string SecondLabel
		{
			get { return m_secondLabel; }
			set
			{
				if (value == null)
					m_secondLabel = string.Empty;
				else
					m_secondLabel = value;
			}
		}

		#endregion Properties

		#region IDisposable implementation

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
		}

		#endregion IDisposable implementation

		#region Protected general methods

		/// <summary />
		protected void ResetSplitterEventHandler(bool reactivate)
		{
			SplitterMoved -= OnSplitterMoved;

			if (reactivate)
			{
				SplitterMoved += OnSplitterMoved;
			}
		}

		#endregion Protected general methods

		#region Event handler methods

		private void m_panel1Btn_Click(object sender, EventArgs e)
		{
			SplitterDistance = m_restoreSplitterPosition;
		}

		private void m_panel1Btn_Paint(object sender, PaintEventArgs e)
		{
			Image icon;
			var x = 0;
			var y = 0;

			if (Orientation == Orientation.Vertical)
			{
				y = kIconOffset;
				icon = m_expandSplitterIcons[2];
			}
			else
			{
				x = kIconOffset;
				icon = m_expandSplitterIcons[0];
			}
			DoBtnPaint(e, x, y, icon, m_firstLabel);
		}

		private void DoBtnPaint(PaintEventArgs e, int x, int y, Image icon, string label)
		{
			using (var g = e.Graphics)
			{
				g.DrawImage(icon, x, y);

				if (string.IsNullOrEmpty(label))
				{
					return;
				}
				var font = Font; // rather arbitrary, what should we use??
				using (Brush brush = new SolidBrush(Color.Black)) // what should this really be?
				{
					if (Orientation == Orientation.Vertical)
					{
						y += icon.Height + kLabelOffset;
						var format = StringFormat.GenericDefault.Clone() as StringFormat;
						format.FormatFlags = StringFormatFlags.DirectionVertical;
						try
						{
							g.DrawString(label, font, brush, (float)0.0, (float)y, format);
						}
						catch (Exception ex)
						{
							System.Diagnostics.Debug.WriteLine(ex.Message);
						}
					}
					else
					{
						x += icon.Width + kLabelOffset;
						g.DrawString(label, font, brush, x, (float)0.0);
					}
				}
			}
		}

		private void m_panel2Btn_Click(object sender, EventArgs e)
		{
			SplitterDistance = m_restoreSplitterPosition;
		}

		private void m_panel2Btn_Paint(object sender, PaintEventArgs e)
		{
			Image icon;
			var x = 0;
			var y = 0;

			if (Orientation == Orientation.Vertical)
			{
				y = kIconOffset;
				icon = m_expandSplitterIcons[1];
			}
			else
			{
				x = kIconOffset;
				icon = m_expandSplitterIcons[3];
			}
			DoBtnPaint(e, x, y, icon, m_secondLabel);
		}

		private bool m_inSplitterMovedMethod;
		private int m_previousSplitterPosition = 40;
		private int m_restoreSplitterPosition = 40;
		/// <summary />
		protected bool InSplitterMovedMethod => m_inSplitterMovedMethod;

		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void OnSplitterMoved(object sender, SplitterEventArgs e)
		{
			if (m_inSplitterMovedMethod)
				return;

			if (m_firstMainControl == null || m_secondMainControl == null)
			{
				m_previousSplitterPosition = SplitterDistance; // Remember it, if nothing else.
				return;
			}

			m_inSplitterMovedMethod = true;
			try
			{
				// See if some ISnapSplitPosition wants to take over.
				if (Orientation == Orientation.Vertical && m_previousSplitterPosition != kCollapsedSize)
				{
					// The old position was not shrunk to the icon, and this has a vertical splitter bar.
					int splitDistance = SplitterDistance;
					if ((m_firstMainControl is ISnapSplitPosition
						&& ((ISnapSplitPosition)m_firstMainControl).SnapSplitPosition(ref splitDistance)))
					{
						// m_firstMainControl decided to take control. We only do this when the splitter is vertical.
						SplitterDistance = splitDistance;
						m_previousSplitterPosition = splitDistance;
						return;
					}
					// It may be a PaneBarContainer
					if (m_firstMainControl is PaneBarContainer)
					{
						var pbc = (PaneBarContainer)m_firstMainControl;
						// pbc.MainControl could be a nested MultiPane, but we don't care about it.
						// MultiPane won't implement ISnapSplitPosition, so we are good on that point.
						if (pbc.MainControl is ISnapSplitPosition
							&& ((ISnapSplitPosition)pbc.MainControl).SnapSplitPosition(ref splitDistance))
						{
							SplitterDistance = splitDistance;
							m_previousSplitterPosition = splitDistance;
							return;
						}
					}
				}

				// In order to get here, no ISnapSplitPosition took over in the above code,
				// if it had a chance at all, that is.
				if (m_wantResetControls)
				{
					ResetControls();
					m_previousSplitterPosition = SplitterDistance;
				}
			}
			finally
			{
				m_inSplitterMovedMethod = false;
			}
		}

		/// <summary />
		protected void ResetControls()
		{
			// TODO: Need to deal with minimum usable sizes of each main control.
			// That is likely to be larger than the minimum sizes (Panel1MinSize and Panel2MinSize).
			// In cases that have such a min suitable size, we also shrink to our real min sizes,
			// when the SD results in it being too narrow.

			// In order to get here, no ISnapSplitPosition took over in the above code,
			// if it had a chance at all, that is.
			bool shouldCollapse;
			if (SplitterDistance < m_previousSplitterPosition)
			{
				// Panel1 Shrinking (moved left or up)
				shouldCollapse = (!IsInitializing) && SplitterDistance <= m_firstCollapseZone;
				if (!shouldCollapse && InsideSecondCollapseZone() && m_restoreSplitterPosition > m_firstCollapseZone)
					SplitterDistance = m_restoreSplitterPosition;
				SetControls(Panel1, shouldCollapse, m_firstIconControl, Panel2, m_secondMainControl);
			}
			else if (SplitterDistance > m_previousSplitterPosition)
			{
				// Panel2 shrinking (moved right or down)
				// We need to notice if the new SplitterDistance is at the place where
				// it can't keep going. If it is, then Panel2 gets minimized to the icon state.
				shouldCollapse = (!IsInitializing) && InsideSecondCollapseZone();
				SetControls(Panel2, shouldCollapse, m_secondIconControl, Panel1, m_firstMainControl);
			}
			else
			{
				// Ensure correct controls are showing.
				if (Orientation == Orientation.Vertical)
				{
					if (Panel1.Width > kCollapsedSize)
					{
						// Wrong control showing.
						ResetControl(Panel1, m_firstMainControl);
					}
					if (Panel2.Width > kCollapsedSize)
					{
						// Wrong control showing.
						ResetControl(Panel2, m_secondMainControl);
					}
				}
				else
				{
					// Horizontal splitter bar.
					if (Panel1.Height > kCollapsedSize)
					{
						// Wrong control showing.
						ResetControl(Panel1, m_firstMainControl);
					}
					if (Panel2.Height > kCollapsedSize)
					{
						// Wrong control showing.
						ResetControl(Panel2, m_secondMainControl);
					}
				}
			}
		}

		private bool InsideSecondCollapseZone()
		{
			if (Orientation == Orientation.Vertical)
				return (Width - SplitterDistance - SplitterWidth <= m_secondCollapseZone);
			else
				return (Height - SplitterDistance - SplitterWidth <= m_secondCollapseZone);
		}

		private void SetControls(SplitterPanel panelA, bool shouldCollapse, UserControl minControl, SplitterPanel panelB, Control otherMainControl)
		{
			if (shouldCollapse)
			{
				ResetControl(panelA, minControl);
				// May need to reset the SplitterDistance so the minControl is the right width.
				if (minControl.Width != kCollapsedSize)
				{
					int potentialSplitterDistance = kCollapsedSize;
					if (minControl == m_secondIconControl)
					{
						int adjustMainDim = (Orientation == Orientation.Vertical) ? Width : Height;
						potentialSplitterDistance = (adjustMainDim - SplitterWidth - kCollapsedSize);
					}
					if (SplitterDistance != potentialSplitterDistance)
						SplitterDistance = potentialSplitterDistance;
				}
				m_restoreSplitterPosition = m_previousSplitterPosition;

				if (Panel1 == panelA && FixedPanel == FixedPanel.Panel2)
					FixedPanel = FixedPanel.Panel1;
				else if (Panel2 == panelA && FixedPanel == FixedPanel.Panel1)
					FixedPanel = FixedPanel.Panel2;
			}
			else if (FixedPanel != FixedPanel.Panel1)
			{
				FixedPanel = FixedPanel.Panel1;
			}

			ResetControl(panelB, otherMainControl);

		}

		private void ResetControl(SplitterPanel panel, Control newControl)
		{
			panel.SuspendLayout();
			if (!panel.Controls.Contains(newControl))
				panel.Controls.Add(newControl);
			newControl.Dock = DockStyle.Fill;
			newControl.BringToFront();
			if (Panel1 == panel)
				m_firstFrontedControl = newControl;
			else
				m_secondFrontedControl = newControl;
			panel.ResumeLayout();
		}

		private bool m_wantResetControls = true;
		/// <summary />
		protected override void OnSizeChanged(EventArgs e)
		{
			try
			{
				m_wantResetControls = false;
				base.OnSizeChanged(e);

				m_firstFrontedControl?.BringToFront();
				m_secondFrontedControl?.BringToFront();
			}
			finally
			{
				m_wantResetControls = true;
			}
		}

		#endregion Event handler methods

		/// <summary />
		public void PostLayoutInit()
		{
			if (FirstControl is IPostLayoutInit)
				((IPostLayoutInit)FirstControl).PostLayoutInit();
			if (SecondControl is IPostLayoutInit)
				((IPostLayoutInit)SecondControl).PostLayoutInit();
		}
	}
}
