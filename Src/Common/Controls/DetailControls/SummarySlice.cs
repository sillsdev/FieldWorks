using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// SummarySlice is like ViewSlice, except that the context menu icon appears on the right,
	/// along with additional hot links derived from the context menu if there is room.
	/// </summary>
	public class SummarySlice : ViewSlice
	{
		private ExpandCollapseButton m_button;
		private RootSite m_view;
		private SummaryCommandControl m_commandControl;
		private string m_layout;
		private string m_collapsedLayout;

		private int m_lastWidth;
		private bool m_fActive;

		protected override bool ShouldHide
		{
			get
			{
				return false;
			}
		}

		public override void FinishInit()
		{
			base.FinishInit();

			string paramType = XmlUtils.GetOptionalAttributeValue(m_configurationNode.ParentNode, "paramType");
			if (paramType == "LiteralString")
			{
				// Instead of the parameter being a layout name, it is literal text which will be
				// the whole contents of the slice, with standard properties.
				string text = XmlUtils.GetManditoryAttributeValue(m_callerNode, "label");
				text = StringTable.Table.LocalizeAttributeValue(text);
				m_view = new LiteralLabelView(text, this);
			}
			else
			{
				m_layout = XmlUtils.GetOptionalAttributeValue(m_callerNode, "param")
					?? XmlUtils.GetManditoryAttributeValue(m_configurationNode, "layout");
				m_collapsedLayout = XmlUtils.GetOptionalAttributeValue(m_callerNode, "collapsedLayout")
					?? XmlUtils.GetOptionalAttributeValue(m_configurationNode, "collapsedLayout");
				m_view = new SummaryXmlView(m_obj.Hvo, m_layout, this);
				m_view.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			}

			var panel = new Panel { Dock = DockStyle.Fill };
			Control = panel;

			m_view.Dock = DockStyle.Left;
			m_view.LayoutSizeChanged += m_view_LayoutSizeChanged;
			panel.Controls.Add(m_view);

			m_button = new ExpandCollapseButton { Dock = DockStyle.Left };
			m_button.Click += m_button_Click;
			panel.Controls.Add(m_button);
			panel.MouseDown += OnMouseDownInPanel;

			m_commandControl = new SummaryCommandControl(this)
			{
				Dock = DockStyle.Fill,
				Visible = XmlUtils.GetOptionalBooleanAttributeValue(m_callerNode, "commandVisible", false)
			};
			panel.Controls.Add(m_commandControl);
		}

		/// <summary>
		/// Handle mousedown in the panel that groups the controls. Sometimes this can be clicked directly, if the
		/// command control is hidden.
		/// </summary>
		void OnMouseDownInPanel(object sender, MouseEventArgs e)
		{
			OnMouseDown(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
				HandleMouseDown(new Point(e.X, e.Y));
			else
				ContainingDataTree.CurrentSlice = this;
		}

		public override DataTree.TreeItemState Expansion
		{
			get
			{
				return base.Expansion;
			}
			set
			{
				base.Expansion = value;
				switch (value)
				{
					case DataTree.TreeItemState.ktisExpanded:
						m_button.Visible = true;
						m_button.IsOpened = true;
						if (m_view is XmlView && m_collapsedLayout != null)
							((XmlView)m_view).ResetTables(m_layout);
						break;

					case DataTree.TreeItemState.ktisCollapsed:
						m_button.Visible = true;
						m_button.IsOpened = false;
						if (m_view is XmlView && m_collapsedLayout != null)
							((XmlView)m_view).ResetTables(m_collapsedLayout);
						break;

					case DataTree.TreeItemState.ktisFixed:
					case DataTree.TreeItemState.ktisCollapsedEmpty:
						m_button.Visible = false;
						break;
				}
			}
		}

		void m_button_Click(object sender, EventArgs e)
		{
			switch (Expansion)
			{
				case DataTree.TreeItemState.ktisCollapsed:
					Expand();
					break;
				case DataTree.TreeItemState.ktisExpanded:
					Collapse();
					break;
			}
		}

		public override void Expand(int iSlice)
		{
			base.Expand(iSlice);
			if (m_collapsedLayout != null)
				((XmlView) m_view).ResetTables(m_layout);
		}

		public override void Collapse(int iSlice)
		{
			base.Collapse(iSlice);
			if (m_collapsedLayout != null)
				((XmlView) m_view).ResetTables(m_collapsedLayout);
		}

		/// <summary>
		/// This is sent when something internal changes the size of the view. One example is a change
		/// in the definition of our stylesheet. This may affect our layout.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_view_LayoutSizeChanged(object sender, EventArgs e)
		{
			m_lastWidth = 0; // force AdjustMainViewWidth to really do something
			AdjustMainViewWidth();
		}

		/// <summary>
		/// Indicates whether this is an active summary, which controls whether the command
		/// control is visible.
		///
		/// Note: it is tempting to use m_commandControl.Visible as the value of Active.
		/// Don't do it: setting m_commandControl.Visible to true will not necessarily
		/// make it true, if some parent is not visible.
		/// </summary>
		public override bool Active
		{
			get
			{
				CheckDisposed();
				return m_fActive;
			}
			set
			{
				CheckDisposed();
				if (m_fActive == value)
					return;
				m_fActive = value;
				if (TreeNode != null)
					TreeNode.Invalidate();
				if (m_commandControl == null)
				{
					// m_commandControl should be null only in the early part of the constructor
					// and the later part of Dispose. But it's possible to be disposing an
					// active slice, in which case, this may get set during Dispose. It should only
					// be set to false at that point, however.
					Debug.Assert(value == false);
					return;
				}
				m_commandControl.Visible = value;
			}
		}

		/// <summary>
		/// Gets and sets the label used to identify the item in the tree diagram.
		/// </summary>
		public override string Label
		{
			get
			{
				CheckDisposed();
				return m_strLabel;
			}
			set
			{
				CheckDisposed();
				// For LiteralString Summary slices we don't want to set the label since
				// it's already been set in m_view and setting it here would double labels.
				if (m_layout != null)
					m_strLabel = value;
			}
		}

		/// <summary>
		/// This class's root site is NOT its control.
		/// </summary>
		public override RootSite RootSite
		{
			get
			{
				CheckDisposed();
				return m_view;
			}
		}

		/// <summary>
		/// We display the context menu icon on the right in a summary slice whenever we show
		/// the other hot links on the right.
		/// </summary>
		/// <returns></returns>
		public override bool ShowContextMenuIconInTreeNode()
		{
			CheckDisposed();
			return Active;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			AdjustMainViewWidth();
			base.OnSizeChanged(e);
		}

		/// <summary>
		/// Adjust the width of the main view, giving it as much of the total available
		/// width as it wants to use.
		/// </summary>
		internal void AdjustMainViewWidth()
		{
			// Skip handling this, if the DataTree hasn't
			// set the official width using SetWidthForDataTreeLayout
			if (!m_widthHasBeenSetByDataTree)
				return;
			IVwRootBox rootb = m_view.RootBox;
			if (rootb != null && m_lastWidth != Width)
			{
				m_lastWidth = Width; // only set this if we actually adjust the layout.
				Control.SuspendLayout();
				m_view.Width = Width;
				m_view.PerformLayout();
				// Some layouts don't work with adding only 4 to the root box width, so we'll
				// add a little more.  See the later comments on LT-4821.
				m_view.Width = Math.Min(Width, rootb.Width + 20);
				Control.ResumeLayout();
			}
		}

		#region Moving items
#if RANDYTODO
		/// <summary>
		/// See if it makes sense to provide the "Move Up" command.
		/// </summary>
		public bool OnDisplayMoveItemUpInVector(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			var command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			bool fIsValid = false;
			if (className == "RnGenericRec")
			{
				if (this.Object != null)
				{
					IRnGenericRec rec = this.Object as IRnGenericRec;
					if (rec != null && rec.Owner is IRnGenericRec && rec.OwnOrd > 0)
						fIsValid = true;
				}
			}
			display.Enabled = fIsValid;
			return true;
		}
#endif

		/// <summary>
		/// Implement the "Move Up" command.
		/// </summary>
		public bool OnMoveItemUpInVector(object argument)
		{
			CheckDisposed();

			if (this.Object == null)
				return false;
			IRnGenericRec rec = this.Object as IRnGenericRec;
			if (rec == null)
				return false;		// shouldn't get here
			IRnGenericRec recOwner = rec.Owner as IRnGenericRec;
			if (recOwner == null)
				return false;		// shouldn't get here
			int idxOrig = rec.OwnOrd;
			Debug.Assert(recOwner.SubRecordsOS[idxOrig] == rec);
			if (idxOrig == 0)
				return false;		// shouldn't get here.
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Resources.DetailControlsStrings.ksUndoMoveUp,
				Resources.DetailControlsStrings.ksRedoMoveUp, Cache.ActionHandlerAccessor, () =>
				{
					recOwner.SubRecordsOS.MoveTo(idxOrig, idxOrig, recOwner.SubRecordsOS, idxOrig - 1);
				});
			return true;
		}

#if RANDYTODO
		/// <summary>
		/// See if it makes sense to provide the "Move Down" command.
		/// </summary>
		public bool OnDisplayMoveItemDownInVector(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			var command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			bool fIsValid = false;
			if (className == "RnGenericRec")
			{
				if (this.Object != null)
				{
					IRnGenericRec rec = this.Object as IRnGenericRec;
					if (rec != null && rec.Owner is IRnGenericRec &&
						rec.OwnOrd < (rec.Owner as IRnGenericRec).SubRecordsOS.Count - 1)
					{
						fIsValid = true;
					}
				}
			}
			display.Enabled = fIsValid;
			return true;
		}
#endif

		/// <summary>
		/// Implement the "Move Down" command.
		/// </summary>
		public bool OnMoveItemDownInVector(object argument)
		{
			CheckDisposed();

			if (this.Object == null)
				return false;
			IRnGenericRec rec = this.Object as IRnGenericRec;
			if (rec == null)
				return false;		// shouldn't get here
			IRnGenericRec recOwner = rec.Owner as IRnGenericRec;
			if (recOwner == null)
				return false;		// shouldn't get here
			int idxOrig = rec.OwnOrd;
			Debug.Assert(recOwner.SubRecordsOS[idxOrig] == rec);
			if (idxOrig == recOwner.SubRecordsOS.Count - 1)
				return false;		// shouldn't get here.
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Resources.DetailControlsStrings.ksUndoMoveDown,
				Resources.DetailControlsStrings.ksRedoMoveDown, Cache.ActionHandlerAccessor, () =>
				{
					// idxOrig + 2 looks strange, but it's the correct value to make this work.
					recOwner.SubRecordsOS.MoveTo(idxOrig, idxOrig, recOwner.SubRecordsOS, idxOrig + 2);
				});
			return true;
		}

#if RANDYTODO
		/// <summary>
		/// See if it makes sense to provide the "Promote" command.
		/// </summary>
		public bool OnDisplayPromoteSubitemInVector(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			var command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			bool fIsValid = false;
			if (className == "RnGenericRec")
			{
				if (Object != null && Object is IRnGenericRec && Object.Owner is IRnGenericRec)
					fIsValid = true;
			}
			display.Enabled = fIsValid;
			return true;
		}
#endif

		/// <summary>
		/// Implement the "Promote" command.
		/// </summary>
		public bool OnPromoteSubitemInVector(object argument)
		{
			CheckDisposed();

			if (this.Object == null)
				return false;
			IRnGenericRec rec = this.Object as IRnGenericRec;
			if (rec == null)
				return false;		// shouldn't get here
			IRnGenericRec recOwner = rec.Owner as IRnGenericRec;
			if (recOwner == null)
				return false;		// shouldn't get here

			IPublisher publisher = Publisher;
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Resources.DetailControlsStrings.ksUndoPromote,
				Resources.DetailControlsStrings.ksRedoPromote,
				Cache.ActionHandlerAccessor, () =>
				{
					if (recOwner.Owner is IRnGenericRec)
					{
						(recOwner.Owner as IRnGenericRec).SubRecordsOS.Insert(recOwner.OwnOrd + 1, rec);
					}
					else if (recOwner.Owner is IRnResearchNbk)
					{
						(recOwner.Owner as IRnResearchNbk).RecordsOC.Add(rec);
					}
					else
					{
						throw new Exception("RnGenericRec object not owned by either RnResearchNbk or RnGenericRec??");
					}
				});
			if (recOwner.Owner is IRnResearchNbk)
			{
				// If possible, jump to the newly promoted record.
				publisher.Publish("JumpToRecord", rec.Hvo);
			}
			return true;
		}

#if RANDYTODO
		/// <summary>
		/// See if it makes sense to provide the "Demote..." command.
		/// </summary>
		public bool OnDisplayDemoteSubitemInVector(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			var command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			bool fIsValid = false;
			if (className == "RnGenericRec")
			{
				if (Object != null && Object is IRnGenericRec)
				{
					if (Object.Owner is IRnGenericRec && (Object.Owner as IRnGenericRec).SubRecordsOS.Count > 1)
						fIsValid = true;
				}
			}
			display.Enabled = fIsValid;
			return true;
		}
#endif

		/// <summary>
		/// Implement the "Demote..." command.
		/// </summary>
		public bool OnDemoteSubitemInVector(object argument)
		{
			CheckDisposed();

			if (this.Object == null)
				return false;
			IRnGenericRec rec = this.Object as IRnGenericRec;
			if (rec == null)
				return false;		// shouldn't get here
			IRnGenericRec newOwner = null;
			if (Object.Owner is IRnGenericRec)
			{
				IRnGenericRec recOwner = Object.Owner as IRnGenericRec;
				if (recOwner.SubRecordsOS.Count == 2)
				{
					if (Object.OwnOrd == 0)
						newOwner = recOwner.SubRecordsOS[1];
					else
						newOwner = recOwner.SubRecordsOS[0];
				}
				else
				{
					List<IRnGenericRec> owners = new List<IRnGenericRec>();
					foreach (var recT in recOwner.SubRecordsOS)
					{
						if (recT != rec)
							owners.Add(recT);
					}
					newOwner = ContainingDataTree.ChooseNewOwner(owners.ToArray(),
						Resources.DetailControlsStrings.ksChooseOwnerOfDemotedSubrecord);
				}
			}
			else
			{
				return false;
			}
			if (newOwner == null)
				return true;
			if (newOwner == rec)
				throw new Exception("RnGenericRec cannot own itself!");

			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Resources.DetailControlsStrings.ksUndoDemote,
				Resources.DetailControlsStrings.ksRedoDemote, Cache.ActionHandlerAccessor, () =>
				{
					newOwner.SubRecordsOS.Insert(0, rec);
				});
			return true;
		}
		#endregion
	}

	internal class ExpandCollapseButton : Button
	{
		bool m_opened = false;

		public ExpandCollapseButton()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			BackColor = SystemColors.Window;
		}

		protected override Size DefaultSize
		{
			get
			{
				return new Size(PreferredWidth, PreferredHeight);
			}
		}

		public bool IsOpened
		{
			get
			{
				return m_opened;
			}

			set
			{
				if (m_opened == value)
					return;

				m_opened = value;
				Invalidate();
			}
		}

		VisualStyleRenderer Renderer
		{
			get
			{
				if (!Application.RenderWithVisualStyles)
					return null;

				return new VisualStyleRenderer(m_opened ? VisualStyleElement.TreeView.Glyph.Opened : VisualStyleElement.TreeView.Glyph.Closed);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.FillRectangle(new SolidBrush(BackColor), ClientRectangle);

			VisualStyleRenderer renderer = Renderer;
			if (renderer != null)
			{
				if (renderer.IsBackgroundPartiallyTransparent())
					renderer.DrawParentBackground(e.Graphics, ClientRectangle, this);
				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
			}
			else
			{
				using (var boxLinePen = new Pen(SystemColors.ControlDark, 1))
				{
					e.Graphics.DrawRectangle(boxLinePen, ClientRectangle);
					int ctrY = ClientRectangle.Y + (ClientRectangle.Height / 2);
					// Draw the minus sign.
					e.Graphics.DrawLine(boxLinePen, ClientRectangle.X + 2, ctrY, ClientRectangle.X + ClientRectangle.Width - 2, ctrY);
					if (!m_opened)
					{
						// Draw the vertical part of the plus, if we are collapsed.
						int ctrX = ClientRectangle.X + (ClientRectangle.Width / 2);
						e.Graphics.DrawLine(boxLinePen, ctrX, ClientRectangle.Y + 4, ctrX, ClientRectangle.Y + ClientRectangle.Height - 4);
					}
				}
			}
		}

		public int PreferredHeight
		{
			get
			{
				VisualStyleRenderer renderer = Renderer;
				if (renderer != null)
				{
					using (Graphics g = CreateGraphics())
					{
						return renderer.GetPartSize(g, ThemeSizeType.True).Height;
					}
				}
				else
				{
					return 5;
				}
			}
		}

		public int PreferredWidth
		{
			get
			{
				VisualStyleRenderer renderer = Renderer;
				if (renderer != null)
				{
					using (Graphics g = CreateGraphics())
					{
						return renderer.GetPartSize(g, ThemeSizeType.True).Width;
					}
				}
				else
				{
					return 11;
				}
			}
		}

		public override void NotifyDefault(bool value)
		{
			base.NotifyDefault(false);
		}

		protected override bool ShowFocusCues
		{
			get
			{
				return false;
			}
		}
	}

	internal class LiteralLabelView : RootSiteControl
	{
		string m_text;
		LiteralLabelVc m_vc;
		SummarySlice m_slice;

		public LiteralLabelView(string text, SummarySlice slice)
		{
			m_text = text;
			m_slice = slice;
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_vc = null;
			m_text = null;
			m_slice = null;
		}

		#endregion IDisposable override

		public override void MakeRoot()
		{
			CheckDisposed();
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			IVwRootBox rootb = VwRootBoxClass.Create();
			rootb.SetSite(this);

			m_vc = new LiteralLabelVc(m_text, m_fdoCache.WritingSystemFactory.UserWs);

			rootb.DataAccess = m_fdoCache.DomainDataByFlid;

			// Since the VC just displays a literal, both the root HVO and the root frag are arbitrary.
			rootb.SetRootObject(1, m_vc, 2, StyleSheet);
			m_rootb = rootb;
			// pathologically (mainly during Refresh, it seems) the slice width may get set before
			// the root box is created, and no further size adjustment may take place, in which case,
			// when we have made the root, we need to adjust the width it occupies in the parent slice.
			m_slice.AdjustMainViewWidth();
		}

		/// <summary>
		/// Suppress left clicks, except for selecting the slice, and process right clicks by
		/// invoking the menu.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
				m_slice.HandleMouseDown(new Point(e.X,e.Y));
			else
				m_slice.ContainingDataTree.CurrentSlice = m_slice;
			//base.OnMouseDown (e);
		}

		/// <summary>
		/// Summary slices don't need cursors. The blue context menu icon is sufficient.
		/// </summary>
		protected override void EnsureDefaultSelection()
		{
			// don't set an IP.
		}
	}

	internal class LiteralLabelVc : FwBaseVc
	{
		ITsString m_text;

		public LiteralLabelVc(string text, int ws)
		{
			m_text = MakeUiElementString(text, ws, null);
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.ControlDarkDark)));
			// By default, the paragraph that is created automatically by AddString will automatically inherit
			// the background color of the whole view (typically white). A paragraph with a background color
			// of white rather than transparent is automatically as wide as it is allowed to be (so as to display
			// the background color over the whole area the user things of as being that paragraph).
			// However, we want LiteralLabelView to adjust its size so it is just big enough to show the label,
			// so we can use the rest of the space for the command menu items. So we need to make the paragraph
			// transparent background, which allows it to be just as wide as the text content.
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)FwTextColor.kclrTransparent);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			vwenv.AddString(m_text);
		}
	}

	internal class SummaryXmlView : XmlView
	{
		SummarySlice m_slice;

		public SummaryXmlView(int hvo, string label, SummarySlice slice) : base( hvo, label, false)
		{
			m_slice = slice;
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_slice = null;
		}

		#endregion IDisposable override

		/// <summary>
		/// Suppress left clicks, except for selecting the slice, and process right clicks by
		/// invoking the menu.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
				m_slice.HandleMouseDown(new Point(e.X,e.Y));
			else
				m_slice.ContainingDataTree.CurrentSlice = m_slice;
			//base.OnMouseDown (e);
		}
		public override void MakeRoot()
		{
			base.MakeRoot();
			// pathologically (mainly during Refresh, it seems) the slice width may get set before
			// the root box is created, and no further size adjustment may take place, in which case,
			// when we have made the root, we need to adjust the width it occupies in the parent slice.
			m_slice.AdjustMainViewWidth();
		}

		/// <summary>
		/// Summary slices don't need cursors. The blue context menu icon is sufficient.
		/// </summary>
		protected override void EnsureDefaultSelection()
		{
			// don't set an IP.
		}
	}
}
