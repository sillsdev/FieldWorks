// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: SidebarAdapter.cs
// Authorship History: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;  //for ImageList
using System.Collections;
using System.Diagnostics;

#if USE_DOTNETBAR
using DevComponents.DotNetBar;
#else
using System.Collections.Generic;
#endif

using SIL.Utils; // for ImageCollection
using XCore;

#if !USE_DOTNETBAR
public class PanelCollection : Panel
{
	public List<Panel> m_panels = new List<Panel>();

	public PanelCollection()
	{
		var p = new Panel();
		m_panels.Add(p); // Add a default panel for now.
	}

	public Panel[] Panels
	{
		get
		{
			return m_panels.ToArray();
		}
	}
}

#endif

namespace XCoreUnused
{
	/// <summary>
	/// Creates a SidebarAdapter.
	/// </summary>
	public class SidebarAdapter : AdapterBase
	{
		#region Properties

		/// <summary>
		/// Overrides property, so the right kind of control gets created.
		/// </summary>
		protected override Control MyControl
		{
			get
			{
#if USE_DOTNETBAR
				if (m_control == null)
				{
					SideBar sidebar = new SideBar();

					m_window.SuspendLayout();
					sidebar.AccessibleRole = System.Windows.Forms.AccessibleRole.ToolBar;
					sidebar.AllowDrop = true;
					sidebar.AllowExternalDrop = true;
					sidebar.Dock = System.Windows.Forms.DockStyle.Left;
					sidebar.Images = m_smallImages.ImageList;
					sidebar.ImagesLarge = m_largeImages.ImageList;
					sidebar.ImagesMedium = null;
					sidebar.Name = "sideBar";
					sidebar.TabIndex = 0;
					sidebar.TabStop = false;
					sidebar.UseNativeDragDrop = false;
					sidebar.Style = eDotNetBarStyle.Office2003;
					sidebar.Width = 140;
					m_window.ResumeLayout(false);

					m_control = sidebar;
				}
				return base.MyControl;
#else
				if (m_control == null)
				{

					m_control = new PanelCollection();
				}

				return m_control;
#endif
			}
		}



		/// <summary>
		/// called by xWindow when everything is all set-up.
		/// </summary>
		/// <remarks> this is needed in this adapter, though it was not in previous sidebar adapters.
		/// we use it here because the expanded event of theDotNetBar fires before it is even painted,
		/// and if the listed is showing is based on with the initial tool will be, well, that tool has not been
		/// identified and instantiated at the point the sidebar is just been created.</remarks>
		public override void FinishInit()
		{
			//note: this is obviously not enough if we ever choose to not show the first handle initially.
			//at the moment,, we do not currently support identifying the initially expanded panel anyways,
			//so they should work.
			ChoiceGroup group = (ChoiceGroup) this.MySideBar.Panels[0].Tag;
			group.OnDisplay(this, null);
		}

#if USE_DOTNETBAR
		/// <summary>
		/// Gets the sidebar.
		/// </summary>
		protected SideBar MySideBar
		{
			get { return (SideBar)MyControl; }
		}
#else
		/// <summary>
		/// Gets the sidebar.
		/// </summary>
		protected PanelCollection MySideBar
		{
			get { return (PanelCollection)MyControl; }
		}
#endif

		#endregion Properties

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public SidebarAdapter()
		{
		}

		#endregion Constructor

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return false; }
		}

		#region IUIAdapter implementation

		// Note: The Init method is handled by the superclass.

		/// <summary>
		/// Overrides method to create main elements of the sidebar.
		/// </summary>
		/// <param name="groupCollection">Collection of groups for this sidebar.</param>
		public override void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
#if USE_DOTNETBAR
			foreach(ChoiceGroup group in groupCollection)
			{
				string label = group.Label;
				label = label.Replace("_", "");
				SideBarPanelItem panelItem = new SideBarPanelItem(group.Id, label);
				if (group.HasSubGroups())
					MakeTree(group, label, ref panelItem);

				panelItem.Tag = group;
				group.ReferenceWidget = panelItem;
				SideBar sidebar = MySideBar;
				sidebar.Panels.Add(panelItem);
				sidebar.Images = m_smallImages.ImageList;
				panelItem.BackgroundStyle.BackColor1.Alpha = ((System.Byte)(255));
				panelItem.BackgroundStyle.BackColor1.Color = System.Drawing.SystemColors.ControlLightLight;
				//
				// sideBarPanelItem1.BackgroundStyle.BackColor2
				//
				panelItem.BackgroundStyle.BackColor2.Alpha = ((System.Byte)(255));
				panelItem.BackgroundStyle.BackColor2.Color = System.Drawing.SystemColors.ControlDark;
				panelItem.BackgroundStyle.BackgroundImageAlpha = ((System.Byte)(255));
				panelItem.BackgroundStyle.Border = DevComponents.DotNetBar.eBorderType.None;
				panelItem.BackgroundStyle.TextTrimming = System.Drawing.StringTrimming.EllipsisCharacter;
				panelItem.ItemImageSize = DevComponents.DotNetBar.eBarImageSize.Default;
			}

			MySideBar.ExpandedChange += new EventHandler(Sidebar_ExpandedChange);
#endif

		}


		/// <summary>
		/// Overrides method to add itmes to the selected main itme in the sidebar.
		/// </summary>
		/// <param name="group">The group that defines this part of the sidebar.</param>
		public override void CreateUIForChoiceGroup(ChoiceGroup group)
		{
#if USE_DOTNETBAR
			SideBarPanelItem panelItem = (SideBarPanelItem)group.ReferenceWidget;
			if (panelItem.SubItems.Count > 0 && panelItem.SubItems[0] is ControlContainerItem)
			{
				ControlContainerItem item = (ControlContainerItem)panelItem.SubItems[0];
				TreeView tree = (TreeView)item.Control;
				if (tree.Nodes.Count == 0)
					FillTreeNodes(tree.Nodes, group);
				// It keeps growing with this. tree.Size = item.Size;
			}
			else if (group.ReferenceWidget is SideBarPanelItem)
			{
				panelItem.SubItems.Clear();
				foreach(ChoiceRelatedClass item in group)
				{
					Debug.Assert(item is ChoiceBase, "Only things that can be made into buttons should be appearing here, else we should have a tree.");
					MakeButton(panelItem, (ChoiceBase)item);
				}
			}
#endif
		}

		/// <summary>
		/// Redraw ths expanded item, so that the selected and enabled items are up to date.
		/// </summary>
		public override void OnIdle()
		{
#if USE_DOTNETBAR
			SideBar sidebar = MySideBar;
			SideBarPanelItem panelItem = sidebar.ExpandedPanel;
			if (panelItem != null)
			{
//				CreateUIForChoiceGroup((ChoiceGroup)panelItem.Tag);
//				sidebar.Refresh();
				((ChoiceGroup)panelItem.Tag).OnDisplay(this, null); //jh experiment
			}
#endif
		}

		#endregion IUIAdapter implementation

		#region Other methods

		/// <summary>
		/// Handle the ExpandedChange event of the sidebar.
		/// </summary>
		/// <param name="sender">The main section that has been expanded.</param>
		/// <param name="e"></param>
		protected void Sidebar_ExpandedChange(object sender, EventArgs e)
		{
#if USE_DOTNETBAR
			SideBarPanelItem item = (SideBarPanelItem)sender;
			ChoiceGroup group = (ChoiceGroup)item.Tag;
			Debug.Assert(group != null);
			group.OnDisplay(this, null);
#endif
		}

#if USE_DOTNETBAR
		/// <summary>
		/// Create a ButtonItem for the sidebar.
		/// </summary>
		/// <param name="panelItem"></param>
		/// <param name="control"></param>
		protected void MakeButton(SideBarPanelItem panelItem, ChoiceBase control)
		{
			UIItemDisplayProperties display = control.GetDisplayProperties();
			display.Text = display.Text.Replace("_", "");
			ButtonItem button = new ButtonItem(control.Id, display.Text);
			button.Tag = control;
			control.ReferenceWidget = button;

			if(panelItem.ItemImageSize == eBarImageSize.Large)
			{
				if(m_largeImages.ImageList.Images.Count > 0)
					button.ImageIndex = m_largeImages.GetImageIndex(display.ImageLabel);
			}
			else
			{
				if(m_smallImages.ImageList.Images.Count > 0)
					button.ImageIndex = m_smallImages.GetImageIndex(display.ImageLabel);
			}

			button.Text = display.Text;
			button.ButtonStyle = eButtonStyle.ImageAndText;

			if(!display.Enabled)
				button.Text = button.Text + " NA";

			button.Click += new EventHandler(OnClick);
			panelItem.SubItems.Add(button);

			//a button in this framework not really a Control... so I don't know how to use
			//(the same company's) balloon tip control on a sidebar button!
		//	m_mediator.SendMessage("RegisterHelpTarget", button.ContainerControl);
		}
#endif

		/// <summary>
		/// Handle the Button Click event.
		/// </summary>
		/// <param name="something">The button that was clicked.</param>
		/// <param name="args">Unused event arguments.</param>
		private void OnClick(object something, System.EventArgs args)
		{
#if USE_DOTNETBAR
			ButtonItem item = (ButtonItem)something;
			ChoiceBase control = (ChoiceBase)item.Tag;
			Debug.Assert(control != null);
			control.OnClick(item, null);
#endif
		}

		#endregion Other methods

		#region Tree control methods

#if USE_DOTNETBAR
		/// <summary>
		/// Create a tree view for the sidebar.
		/// </summary>
		/// <param name="group">The definition for the tree view.</param>
		/// <param name="label"></param>
		/// <param name="panelItem"></param>
		protected void MakeTree(ChoiceGroup group, string label, ref SideBarPanelItem panelItem)
		{
			// TODO: This tree isn't the right size, when the window opens.
			// Figure out how to make it right.
			TreeView tree = new TreeView();
			tree.Tag = group;
			tree.AfterSelect += new TreeViewEventHandler(OnTreeNodeSelected);
			ControlContainerItem containerItem = new ControlContainerItem(group.Id, label);
			containerItem.AllowItemResize = true;
			containerItem.Control = tree;
			panelItem.SubItems.Add(containerItem);
		}
#endif

		/// <summary>
		/// Add the nodes to the tree.
		/// </summary>
		/// <remarks>The first time this is called, the group will be the
		/// maikn element of the sidebar.
		/// It will then be called recursively for each node that contains other nodes.</remarks>
		/// <param name="nodes">Collections of tree view nodes.</param>
		/// <param name="group">Definition of current set of nodes.</param>
		protected void FillTreeNodes(TreeNodeCollection nodes, ChoiceGroup group)
		{
			if (nodes.Count > 0)//hack...without this, we were losing expansion during OnIdle()
				return;
			nodes.Clear();
			group.PopulateNow();
			foreach(ChoiceRelatedClass item in group)
			{
				TreeNode node = MakeTreeNode(item);
				nodes.Add(node);
				if (item is ChoiceGroup)
					FillTreeNodes(node.Nodes, (ChoiceGroup)item);
			}
		}

		/// <summary>
		/// Make an individual tree node.
		/// </summary>
		/// <param name="item">Definition of the node being created.</param>
		/// <returns>The newly created node.</returns>
		protected TreeNode MakeTreeNode(ChoiceRelatedClass item)
		{
			TreeNode node = new TreeNode(item.Label.Replace("_",""));
			node.Tag = item;
			item.ReferenceWidget = node;
			return node;
		}

		/// <summary>
		/// Handles the AfterSelect event of the tree view.
		/// </summary>
		/// <param name="sender">(Not used.)</param>
		/// <param name="arguments">Gets the node that was selected.</param>
		protected void OnTreeNodeSelected(object sender, TreeViewEventArgs arguments)
		{
			ChoiceBase control = null;
			if (arguments.Node.Tag is ChoiceGroup)
				control = ((ChoiceGroup)arguments.Node.Tag).CommandChoice;
			else
				control = (ChoiceBase)arguments.Node.Tag;

			if (control != null)
				control.OnClick(null, null);
		}

		#endregion Tree control methods
	}
}
