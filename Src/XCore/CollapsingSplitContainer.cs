// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CollapsingSplitContainer.cs
// Responsibility: Randy
//
// <remarks>
// </remarks>

using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.Utils;
using System.Xml;
using SIL.CoreImpl;

namespace XCore
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class extends SplitContainer by adding buttons which replace a nearly collpased
	/// control (Panel1 or Panel2 child). When the SplitterDistance gets low enough, then the
	/// button becomes top most, which hides the main control.
	///
	/// Clicking on the button will reset the SplitterDistance, and show the main control again.
	/// TODO: We may be able expand back out to the previous SplitterDistance,
	/// or we may just have to jump out to some other distance.
	/// For now, we will remember (and restore) the previous position.
	///
	/// We will still support complete collapsing of a main control, of course, but client code
	/// will have to do that.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CollapsingSplitContainer : SplitContainer, IFWDisposable, IPostLayoutInit
	{
		public const int kCollapsedSize = 16;
		public const int kCollapseZone = 35;

		protected int m_firstCollapseZone = kCollapseZone;
		protected int m_secondCollapseZone = kCollapseZone;

		private const int kIconOffset = 10;
		private const int kLabelOffset = kIconOffset;

		#region Data Members (non-Designer)

		// The control with the icon.
		private UserControl m_firstIconControl = null;
		// The control with the icon.
		private UserControl m_secondIconControl = null;
		private Control m_firstMainControl = null;
		private Control m_secondMainControl = null;
		private Control m_firstFrontedControl = null;
		private Control m_secondFrontedControl = null;
		// Labels to show on the button when one pane is minimized.
		private string m_firstLabel = "";
		private string m_secondLabel = "";
		// Expand Button images in the four possible orientations.
		private Image[] m_expandSplitterIcons = new Image[4];

		private bool m_fIsInitializing = false;

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
			m_firstIconControl.Click += new EventHandler(m_panel1Btn_Click);
			m_firstIconControl.Paint += new PaintEventHandler(m_panel1Btn_Paint);
			//Panel1.ControlAdded += new ControlEventHandler(CollapsingSplitContainer_Panel_ControlAdded);

			FixedPanel = FixedPanel.Panel1;

			Panel2MinSize = kCollapsedSize;
			m_secondIconControl = new UserControl();
			if (m_secondIconControl.AccessibleName == null)
				m_secondIconControl.AccessibleName = "SecondIconControl";

			m_secondIconControl.Dock = DockStyle.Fill;
			Panel2.Controls.Add(m_secondIconControl);
			m_secondIconControl.Click += new EventHandler(m_panel2Btn_Click);
			m_secondIconControl.Paint += new PaintEventHandler(m_panel2Btn_Paint);
			//Panel2.ControlAdded += new ControlEventHandler(CollapsingSplitContainer_Panel_ControlAdded);

			// make sure Panel1 and Panel2 have AccessibleNames
			if (Panel1 != null && Panel1.AccessibleName == null)
				Panel1.AccessibleName = "CSC.SplitContainer.One";
			if (Panel2 != null && Panel2.AccessibleName == null)
				Panel2.AccessibleName = "CSC.SplitContainer.Two";
			Panel2.Layout += new LayoutEventHandler(Panel2_Layout);
			Panel2.SizeChanged += new EventHandler(Panel2_SizeChanged);

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

			Panel1.SizeChanged += new EventHandler(Panel1_SizeChanged);
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
		public bool IsInitializing
		{
			get { return m_fIsInitializing; }
			set { m_fIsInitializing = value; }
		}

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

				return (m_firstMainControl == null) ? null : m_firstMainControl;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("FirstControl cannot be null.");

				value.Dock = DockStyle.Fill;
				if (Panel1.Controls.Contains(m_firstMainControl))
				{
					Panel1.SuspendLayout();
					Panel1.Controls.Remove(m_firstMainControl);
					//m_firstMainControl.Dispose();
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

				return (m_secondMainControl == null) ? null : m_secondMainControl;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("SecondControl cannot be null.");

				value.Dock = DockStyle.Fill;
				if (Panel2.Controls.Contains(m_secondMainControl))
				{
					Panel2.SuspendLayout();
					Panel2.Controls.Remove(m_secondMainControl);
					//m_secondMainControl.Dispose();
					m_secondMainControl = value;
					Panel2.Controls.Add(value);
					Panel2.ResumeLayout();
				}
				else
					m_secondMainControl = value;

				ResetControl(Panel2, value);
			}
		}

		public string FirstLabel
		{
			get { return m_firstLabel; }
			set
			{
				if (value == null)
					m_firstLabel = "";
				else
					m_firstLabel = value;
			}
		}

		public string SecondLabel
		{
			get { return m_secondLabel; }
			set
			{
				if (value == null)
					m_secondLabel = "";
				else
					m_secondLabel = value;
			}
		}

		#endregion Properties

		#region IFwDisposable implementation

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion IFwDisposable implementation

		#region Private general methods

		#endregion Private general methods

		#region Protected general methods

		protected void ResetSplitterEventHandler(bool reactivate)
		{
			this.SplitterMoved -= new System.Windows.Forms.SplitterEventHandler(this.OnSplitterMoved);

			if (reactivate)
			{
				this.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.OnSplitterMoved);
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
			Image icon = null;
			int x = 0;
			int y = 0;

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
			using (Graphics g = e.Graphics)
			{
				g.DrawImage(icon, x, y);

				if (label != null && label.Length != 0)
				{
					Font font = Font; // rather arbitrary, what should we use??
					using (Brush brush = new SolidBrush(Color.Black)) // what should this really be?
					{
						if (Orientation == Orientation.Vertical)
						{
							y += icon.Height + kLabelOffset;
							StringFormat format = StringFormat.GenericDefault.Clone() as StringFormat;
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
							g.DrawString(label, font, brush, (float)x, (float)0.0);
						}
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
			Image icon = null;
			int x = 0;
			int y = 0;

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

		private bool m_inSplitterMovedMethod = false;
		private int m_previousSplitterPosition = 40;
		private int m_restoreSplitterPosition = 40;
		protected bool InSplitterMovedMethod
		{
			get { return m_inSplitterMovedMethod; }
		}

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
						&& (m_firstMainControl as ISnapSplitPosition).SnapSplitPosition(ref splitDistance)))
					{
						// m_firstMainControl decided to take control. We only do this when the splitter is vertical.
						SplitterDistance = splitDistance;
						m_previousSplitterPosition = splitDistance;
						return;
					}
					// It may be a PaneBarContainer
					if (m_firstMainControl is PaneBarContainer)
					{
						PaneBarContainer pbc = m_firstMainControl as PaneBarContainer;
						// pbc.MainControl could be a nested MultiPane, but we don't care about it.
						// MultiPane won't implement ISnapSplitPosition, so we are good on that point.
						if (pbc.MainControl is ISnapSplitPosition
							&& (pbc.MainControl as ISnapSplitPosition).SnapSplitPosition(ref splitDistance))
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

		protected void ResetControls()
		{
			// TODO: Need to deal with minimum usable sizes of each main control.
			// That is likely to be larger than the minimum sizes (Panel1MinSize and Panel2MinSize).
			// In cases that have such a min suitable size, we also shrink to our real min sizes,
			// when the SD results in it being too narrow.

			// In order to get here, no ISnapSplitPosition took over in the above code,
			// if it had a chance at all, that is.
			bool shouldCollapse = false;
			if (SplitterDistance < m_previousSplitterPosition)
			{
				// Panel1 Shrinking (moved left or up)
				shouldCollapse = (!m_fIsInitializing) && SplitterDistance <= m_firstCollapseZone;
				if (!shouldCollapse && InsideSecondCollapseZone() && m_restoreSplitterPosition > m_firstCollapseZone)
					SplitterDistance = m_restoreSplitterPosition;
				SetControls(Panel1, shouldCollapse, m_firstIconControl, Panel2, m_secondMainControl);
			}
			else if (SplitterDistance > m_previousSplitterPosition)
			{
				// Panel2 shrinking (moved right or down)
				// We need to notice if the new SplitterDistance is at the place where
				// it can't keep going. If it is, then Panel2 gets minimized to the icon state.
				shouldCollapse = (!m_fIsInitializing) && InsideSecondCollapseZone();
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
			newControl.BringToFront();
			if (Panel1 == panel)
				m_firstFrontedControl = newControl;
			else
				m_secondFrontedControl = newControl;
			panel.ResumeLayout();
		}

		private bool m_wantResetControls = true;
		protected override void OnSizeChanged(EventArgs e)
		{
			try
			{
				m_wantResetControls = false;
				base.OnSizeChanged(e);

				if (m_firstFrontedControl != null)
					m_firstFrontedControl.BringToFront();
				if (m_secondFrontedControl != null)
					m_secondFrontedControl.BringToFront();
			}
			finally
			{
				m_wantResetControls = true;
			}
		}

//		/// <summary>
//		/// Handles Dock style for any control added to a panel.
//		/// </summary>
//		/// <param name="sender"></param>
//		/// <param name="e"></param>
//		private void CollapsingSplitContainer_Panel_ControlAdded(object sender, ControlEventArgs e)
//		{
//			e.Control.Dock = DockStyle.Fill;
//		}

		internal void SetFirstCollapseZone(XmlNode node)
		{
			m_firstCollapseZone = GetCollapseZone(node);
		}

		internal void SetSecondCollapseZone(XmlNode node)
		{
			m_secondCollapseZone = GetCollapseZone(node);
		}

		private int GetCollapseZone(XmlNode xmlNode)
		{
			if (xmlNode == null)
				return kCollapseZone;
			string sCollapse = SIL.Utils.XmlUtils.GetOptionalAttributeValue(xmlNode, "collapse", null);
			if (sCollapse == null)
			{
				XmlNode parameters = xmlNode.SelectSingleNode("parameters");
				if (parameters != null)
					sCollapse = SIL.Utils.XmlUtils.GetOptionalAttributeValue(parameters, "collapse", null);
			}
			if (String.IsNullOrEmpty(sCollapse))
				return kCollapseZone;
			int collapse;
			if (Int32.TryParse(sCollapse, out collapse) && collapse > 0)
			{
				using (Graphics gr = CreateGraphics())
				{
					return Math.Max((int)((float)collapse * gr.DpiX) / SIL.Utils.MiscUtils.kdzmpInch,
						kCollapseZone);
				}
			}
			return kCollapseZone;
		}

		#endregion Event handler methods

		public void PostLayoutInit()
		{
			if (FirstControl is IPostLayoutInit)
				((IPostLayoutInit)FirstControl).PostLayoutInit();
			if (SecondControl is IPostLayoutInit)
				((IPostLayoutInit)SecondControl).PostLayoutInit();
		}
	}
}
