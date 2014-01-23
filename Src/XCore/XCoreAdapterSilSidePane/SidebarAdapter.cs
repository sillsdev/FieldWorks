// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SidebarAdapter.cs
// Authorship History: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;  //for ImageList
using System.Collections;
using System.Diagnostics;

using System.Collections.Generic;

using SIL.Utils; // for ImageCollection
using XCore;

public class PanelCollection : Panel
{
	public List<Panel> m_panels = new List<Panel>();

	public PanelCollection()
	{
		var p = new Panel();
		p.AccessibilityObject.Name = "panelCollection"; // p.GetType().Name;
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
				if (m_control == null)
				{

					m_control = new PanelCollection();
				}

				return m_control;
			}
		}



		/// <summary>
		/// called by xWindow when everything is all set-up.
		/// </summary>
		/// <remarks> (this comment no longer valid, I think (SFM). this is needed in this adapter, though it was not in previous sidebar adapters.
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

		/// <summary>
		/// Gets the sidebar.
		/// </summary>
		protected PanelCollection MySideBar
		{
			get { return (PanelCollection)MyControl; }
		}

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

		}


		/// <summary>
		/// Overrides method to add itmes to the selected main itme in the sidebar.
		/// </summary>
		/// <param name="group">The group that defines this part of the sidebar.</param>
		public override void CreateUIForChoiceGroup(ChoiceGroup group)
		{
		}

		/// <summary>
		/// Redraw ths expanded item, so that the selected and enabled items are up to date.
		/// </summary>
		public override void OnIdle()
		{
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
		}


		/// <summary>
		/// Handle the Button Click event.
		/// </summary>
		/// <param name="something">The button that was clicked.</param>
		/// <param name="args">Unused event arguments.</param>
		private void OnClick(object something, System.EventArgs args)
		{
		}

		#endregion Other methods

		#region Tree control methods


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
