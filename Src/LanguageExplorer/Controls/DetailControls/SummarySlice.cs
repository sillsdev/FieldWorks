// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// SummarySlice is like ViewSlice, except that the context menu icon appears on the right,
	/// along with additional hot links derived from the context menu if there is room.
	/// </summary>
	internal class SummarySlice : ViewSlice
	{
		private ExpandCollapseButton m_button;
		private RootSite m_view;
		private SummaryCommandControl m_commandControl;
		private string m_layout;
		private string m_collapsedLayout;
		private int m_lastWidth;
		private bool m_fActive;
		protected override bool ShouldHide => false;

		#region Overrides of Slice
		/// <inheritdoc />
		internal override ContextMenuName HotlinksMenuId
		{
			get
			{
				// Try the normal hotlinks attribute value.
				var hotlinksMenuId = base.HotlinksMenuId;
				if (hotlinksMenuId == ContextMenuName.nullValue)
				{
					// Try the ordinary context menu for the CallerNode.
					 var ordinaryMenuId = XmlUtils.GetOptionalAttributeValue(CallerNode, "menu");
					if (string.IsNullOrEmpty(ordinaryMenuId))
					{
						// Try the ordinary context menu for the configuation node.
						ordinaryMenuId = XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "menu");
					}
					hotlinksMenuId = string.IsNullOrEmpty(ordinaryMenuId) ? hotlinksMenuId : (ContextMenuName)Enum.Parse(typeof(ContextMenuName), ordinaryMenuId);
				}
				return hotlinksMenuId;
			}
		}

		#endregion

		#region Overrides of ViewSlice

		/// <summary />
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);
			m_commandControl = new SummaryCommandControl(this, MyDataTreeStackContextMenuFactory.HotlinksMenuFactory, MyDataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory)
			{
				Dock = DockStyle.Fill,
				Visible = XmlUtils.GetOptionalBooleanAttributeValue(CallerNode, "commandVisible", false)
			};
			Control.Controls.Add(m_commandControl);
		}
		#endregion

		public override void FinishInit()
		{
			base.FinishInit();
			var paramType = XmlUtils.GetOptionalAttributeValue(ConfigurationNode.Parent, "paramType");
			if (paramType == "LiteralString")
			{
				// Instead of the parameter being a layout name, it is literal text which will be
				// the whole contents of the slice, with standard properties.
				var text = XmlUtils.GetMandatoryAttributeValue(CallerNode, "label");
				text = StringTable.Table.LocalizeAttributeValue(text);
				m_view = new LiteralLabelView(text, this);
			}
			else
			{
				m_layout = XmlUtils.GetOptionalAttributeValue(CallerNode, "param") ?? XmlUtils.GetMandatoryAttributeValue(ConfigurationNode, "layout");
				m_collapsedLayout = XmlUtils.GetOptionalAttributeValue(CallerNode, "collapsedLayout") ?? XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "collapsedLayout");
				m_view = new SummaryXmlView(MyCmObject.Hvo, m_layout, this);
			}
			var panel = new Panel
			{
				Dock = DockStyle.Fill
			};
			Control = panel;
			m_view.Dock = DockStyle.Left;
			m_view.LayoutSizeChanged += m_view_LayoutSizeChanged;
			panel.Controls.Add(m_view);
			m_view.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			m_button = new ExpandCollapseButton { Dock = DockStyle.Left };
			m_button.Click += m_button_Click;
			panel.Controls.Add(m_button);
			panel.MouseDown += OnMouseDownInPanel;
		}

		/// <summary>
		/// Handle mousedown in the panel that groups the controls. Sometimes this can be clicked directly, if the
		/// command control is hidden.
		/// </summary>
		private void OnMouseDownInPanel(object sender, MouseEventArgs e)
		{
			OnMouseDown(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				HandleMouseDown(new Point(e.X, e.Y));
			}
			else
			{
				ContainingDataTree.CurrentSlice = this;
			}
		}

		public override TreeItemState Expansion
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
					case TreeItemState.ktisExpanded:
						m_button.Visible = true;
						m_button.IsOpened = true;
						if (m_view is XmlView && m_collapsedLayout != null)
						{
							((XmlView)m_view).ResetTables(m_layout);
						}
						break;
					case TreeItemState.ktisCollapsed:
						m_button.Visible = true;
						m_button.IsOpened = false;
						if (m_view is XmlView && m_collapsedLayout != null)
						{
							((XmlView)m_view).ResetTables(m_collapsedLayout);
						}
						break;
					case TreeItemState.ktisFixed:
					case TreeItemState.ktisCollapsedEmpty:
						m_button.Visible = false;
						break;
				}
			}
		}

		void m_button_Click(object sender, EventArgs e)
		{
			switch (Expansion)
			{
				case TreeItemState.ktisCollapsed:
					Expand();
					break;
				case TreeItemState.ktisExpanded:
					Collapse();
					break;
			}
		}

		public override void Expand(int iSlice)
		{
			base.Expand(iSlice);
			if (m_collapsedLayout != null)
			{
				((XmlView)m_view).ResetTables(m_layout);
			}
		}

		public override void Collapse(int iSlice)
		{
			base.Collapse(iSlice);
			if (m_collapsedLayout != null)
			{
				((XmlView)m_view).ResetTables(m_collapsedLayout);
			}
		}

		/// <summary>
		/// This is sent when something internal changes the size of the view. One example is a change
		/// in the definition of our stylesheet. This may affect our layout.
		/// </summary>
		private void m_view_LayoutSizeChanged(object sender, EventArgs e)
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
				return m_fActive;
			}
			set
			{
				if (m_fActive == value)
				{
					return;
				}
				m_fActive = value;
				TreeNode?.Invalidate();
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
				return m_strLabel;
			}
			set
			{
				// For LiteralString Summary slices we don't want to set the label since
				// it's already been set in m_view and setting it here would double labels.
				if (m_layout != null)
				{
					m_strLabel = value;
				}
			}
		}

		/// <summary>
		/// This class's root site is NOT its control.
		/// </summary>
		public override RootSite RootSite => m_view;

		/// <summary>
		/// We display the context menu icon on the right in a summary slice whenever we show
		/// the other hot links on the right.
		/// </summary>
		public override bool ShowContextMenuIconInTreeNode()
		{
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
			{
				return;
			}
			var rootb = m_view.RootBox;
			if (rootb == null || m_lastWidth == Width)
			{
				return;
			}
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
}