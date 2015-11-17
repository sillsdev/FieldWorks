// --------------------------------------------------------------------------------------------
// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File:
// Authorship History:
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
using System.Collections.Generic;
using System.Diagnostics;

#if USE_DOTNETBAR
using DevComponents.DotNetBar;
#endif

using SIL.Utils; // for ImageCollection
using SIL.SilSidePane;

namespace XCore
{
	/// <summary>
	/// Sidebar adapter to use SilSidePane
	/// </summary>
	public class SidebarAdapter : AdapterBase, IxCoreColleague
	{
		protected ArrayList m_panelSubControls;
		protected bool m_populatingList;
		protected bool 	m_suspendEvents;
		protected bool m_firstLoad = true;
		private SidePane m_sidepane; // sidebar on which to show tabs and items
		private ChoiceGroupCollection m_choiceGroupCollectionCache; // cache of what was received by CreateUIForChoiceGroupCollection()
		private List<ChoiceGroup> m_choiceGroupCache = new List<ChoiceGroup>(); // cache of what was received by CreateUIForChoiceGroup()
		private string areasLabel; // label of the special case ChoiceGroup for Areas

		#region Properties

		/// <summary>
		/// Overrides property, so the right kind of control gets created.
		/// </summary>
		protected override Control MyControl
		{
			get
			{
#if USE_DOTNETBAR
				//m_control=  new NavigationPane();
				if (m_control == null)
				{
					NavigationPane navPane = new NavigationPane();

					//				m_window.SuspendLayout();

					navPane.SuspendLayout();

					navPane.AccessibleRole = System.Windows.Forms.AccessibleRole.Outline;
					navPane.AllowDrop = false;
					navPane.Dock = System.Windows.Forms.DockStyle.Left;
					navPane.Images = m_largeImages.ImageList;
					navPane.Name = "navPane";
					navPane.TabIndex = 0;
					// part of the can't quit bug!:					navPane.TabStop = false;

					navPane.Width = 140;
					navPane.NavigationBarHeight = 300;
					//
					//					navPane.TitlePanel.Dock = System.Windows.Forms.DockStyle.Top;
					//					navPane.TitlePanel.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
					//					navPane.TitlePanel.Location = new System.Drawing.Point(0, 0);
					//					navPane.TitlePanel.Name = "panelEx1";
					//					navPane.TitlePanel.Size = new System.Drawing.Size(184, 24);
					//					navPane.TitlePanel.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
					//					navPane.TitlePanel.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
					//					navPane.TitlePanel.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
					//					navPane.TitlePanel.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
					//					navPane.TitlePanel.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
					//					navPane.TitlePanel.Style.GradientAngle = 90;
					//					navPane.TitlePanel.Style.MarginLeft = 4;
					//					navPane.TitlePanel.TabIndex = 0;
					navPane.TitlePanel.Text = AdapterStrings.Loading;

					//					TestAddButton(navPane, "alpha");
					//					TestAddButton(navPane,"beta");

					//	navPane.Controls.Add(navPane.TitlePanel);
					m_control = navPane;



					//					navPane.NavigationBar.Dock = System.Windows.Forms.DockStyle.Top;
					navPane.NavigationBar.SplitterVisible = true;

					navPane.ResumeLayout(false);
					//
					//					m_window.ResumeLayout(false);

				}
				return base.MyControl;
#else
				if (m_control == null)
				{
					m_control = new Panel();
					m_control.Width  = 140;
					m_control.Height = 300;
				}

				return m_control;
#endif
			}
		}

#if USE_DOTNETBAR // NavigationPane part of DotNetBar
		private void TestAddButton(NavigationPane navPane, string t)
		{
			ButtonItem  b = new DevComponents.DotNetBar.ButtonItem();
			navPane.Items.Add(b);

			b.ButtonStyle = DevComponents.DotNetBar.eButtonStyle.ImageAndText;
			//b.Image = ((System.Drawing.Image)(resources.GetObject("buttonItem2.Image")));
			b.ImageIndex=1;
			//b.Name = "b";
			//	b.OptionGroup = "navBar";
			b.Style = DevComponents.DotNetBar.eDotNetBarStyle.Office2003;
			b.Text =t;
			//	b.Tooltip = t;

			MakePanelToGoWithButton(navPane, b, null);
		}
#endif



		/// <summary>
		/// Called by xWindow when everything is all set up.
		/// Will take the information that was received from CreateUIForChoiceGroupCollection and
		/// CreateUIForChoiceGroup and put appropriate Tabs and Items on
		/// the sidebar.
		/// </summary>
		public override void FinishInit()
		{
			AddTabsAndItemsFromCache();
		}

		/// <summary>
		/// Gets the sidebar.
		/// </summary>
#if USE_DOTNETBAR
		protected NavigationPane NavPane
		{
			get { return (NavigationPane)MyControl; }
		}
#else
		protected Panel MyPanel
		{
			get { return (Panel)MyControl; }
		}
#endif

		#endregion Properties

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public SidebarAdapter()
		{
			m_panelSubControls = new ArrayList();
		}

		#endregion Constructor

		/// <summary></summary>
		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			mediator.AddColleague(this);

			areasLabel = m_mediator.StringTbl.LocalizeAttributeValue("Areas");

			m_sidepane = new SidePane(MyControl, SidePaneItemAreaStyle.List);
			m_sidepane.ItemClicked += SidePaneItemClickedHandler;
			m_sidepane.TabClicked += SidePaneTabClickedHandler;
		}

		/// <summary>
		/// Handle when an item on the sidepane is clicked.
		/// </summary>
		private void SidePaneItemClickedHandler(Item itemClicked)
		{
			var choice = itemClicked.Tag as ChoiceBase;
			Debug.Assert(choice != null, "SidePaneItemClickedHandler received an Item with an invalid Tag.");
			if (choice != null)
				choice.OnClick(this, null);
		}

		/// <summary>
		/// Handle when a tab on the sidepane is clicked.
		/// </summary>
		private void SidePaneTabClickedHandler(Tab tabClicked)
		{
			var choice = tabClicked.Tag as ChoiceBase;
			Debug.Assert(choice != null, "SidePaneTabClickedHandler received a Tab with an invalid Tag.");
			if (choice != null)
				choice.OnClick(this, null);
		}

		public override void PersistLayout()
		{
#if USE_DOTNETBAR
			NavPane.SaveLayout(LayoutPersistPath);
#else
			// use MyPanel
#endif
		}

		protected string LayoutPersistPath
		{
			get
			{
				return System.IO.Path.Combine(SettingsPath (),"NavPaneLayout.xml");
			}
		}
		/// <summary>
		/// get the location/settings of various widgets so that we can restore them
		/// </summary>
		protected void DepersistLayout()
		{
#if USE_DOTNETBAR
			if (System.IO.File.Exists(LayoutPersistPath))
			{
				try
				{
					NavPane.LoadLayout(LayoutPersistPath);
				}
				catch(Exception)
				{}
			}
#else
			// use MyPanel
#endif
		}


		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[]{this};
		}

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
		/// Caches the ChoiceGroupCollection received, since it's not guaranteed that
		/// tab information will be received before item information, and
		/// SilSidePane Items cannot be added to an SilSidePane without Tabs.
		/// </summary>
		/// <param name="groupCollection">Collection of groups for this sidebar.</param>
		public override void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
#if USE_DOTNETBAR
			string sAreas = m_mediator.StringTbl.LocalizeAttributeValue("Areas");
			foreach (ChoiceGroup group in groupCollection)
			{
				if (group.Label == sAreas)
				{
					group.ReferenceWidget = this.NavPane.NavigationBar;
					this.NavPane.NavigationBar.Tag = group;
				}
				else
				{
					MakeListControl(group);
				}
			}
#else
			// Cache CGC
			m_choiceGroupCollectionCache = groupCollection;

			foreach (ChoiceGroup group in groupCollection)
			{
				if (group.Label == areasLabel)
				{
					group.ReferenceWidget = this.MyPanel;
					this.MyPanel.Tag = group;
				}
				else
				{
					MakeListControl(group);
				}
			}
#endif
		}

		/// <summary>
		/// Make sidebar tabs from group. Cache other information we receive to help
		/// make items later.
		/// </summary>
		/// <param name="group">The group that defines this part of the sidebar.</param>
		public override void CreateUIForChoiceGroup(ChoiceGroup group)
		{
#if USE_DOTNETBAR
			if (group.ReferenceWidget ==this.NavPane.NavigationBar)
				LoadAreaButtons(group);
			else if (group.ReferenceWidget is ListView)
				PopulateList(group);

			if(m_firstLoad)
			{
				m_firstLoad = false;
				DepersistLayout();
			}
#else
			if (group.ReferenceWidget == this.MyPanel) // Is this an "Areas" group?
				LoadAreaButtons(group);
			else
				PopulateList(group);

			if(m_firstLoad)
			{
				m_firstLoad = false;
				DepersistLayout();
			}
#endif
		}

		/// <summary>
		/// Add tabs and items from the information received and cached by
		/// CreateUIForChoiceGroupCollection and CreateUIForChoiceGroup.
		/// </summary>
		private void AddTabsAndItemsFromCache()
		{
			// Add tabs

			if (null != this.MyPanel.Tag)
				((ChoiceGroup)this.MyPanel.Tag).OnDisplay(null, null);

			// Add items

			// m_choiceGroupCache appears to contain 2 kinds of things:
			//  * ChoiceGroups that are a list of items, with the name of their tab in the group.ReferenceWidget
			//  * A ChoiceGroup named "Areas" which is a special case list representing the tabs
			foreach (ChoiceGroup group in m_choiceGroupCache)
			{
				if (group == null)
					continue;

				if (group.Label == areasLabel)
					continue; // Skip the "Areas" special case ChoiceGroup

				foreach (ListPropertyChoice choice in group) // group must already be populated
				{
					if (choice == null)
						continue;

					Tab tab = m_sidepane.GetTabByName((group.ReferenceWidget as ListView).Name);
					if (tab == null)
						continue; // Item's tab doesn't exist in the sidepane for some reason. Skip this item.

					Item item = new Item(choice.Value)
									{
										Icon = m_smallImages.GetImage(choice.ImageName),
										Text = choice.Label,
										Tag = choice,
									};
					choice.ReferenceWidget = item;

					m_sidepane.AddItem(tab, item);
				}
			}
		}

		/// <summary>
		/// Create tabs on sidebar
		/// </summary>
		protected void LoadAreaButtons(ChoiceGroup group)
		{
#if USE_DOTNETBAR
			NavPane.SuspendLayout();
			NavPane.Items.Clear();
#endif
			foreach (ChoiceRelatedClass tab in group)
			{
				//Debug.Assert(item is ChoiceBase, "Only things that can be made into buttons should be appearing here.");
				Debug.Assert(tab is ListPropertyChoice, "Only things that can be made into buttons should be appearing here.");
				MakeAreaButton((ListPropertyChoice)tab);
			}

#if USE_DOTNETBAR
			NavPane.ResumeLayout(true);
#endif
		}

		/// <summary>
		/// Redraw ths expanded item, so that the selected and enabled items are up to date.
		/// </summary>
		public override void OnIdle()
		{
#if USE_DOTNETBAR
			if(NavPane.Items.Count >0)
				return;

			if(null != this.NavPane.NavigationBar.Tag)
				((ChoiceGroup)this.NavPane.NavigationBar.Tag).OnDisplay(null, null);
			//		CreateUIForChoiceGroup((ChoiceGroup)this.NavPane.NavigationBar.Tag);
#endif
		}
		#endregion IUIAdapter implementation

		#region Other methods
		/// <summary>
		/// Create a tab on the sidebar from a choice.
		/// </summary>
		protected void MakeAreaButton(ListPropertyChoice choice)
		{
#if USE_DOTNETBAR
			UIItemDisplayProperties display = choice.GetDisplayProperties();
			display.Text = display.Text.Replace("_", "");
			ButtonItem button = new ButtonItem(choice.Id, display.Text);
			button.Tag = choice;
			choice.ReferenceWidget = button;

			button.ImageIndex = m_largeImages.GetImageIndex(display.ImageLabel);

			button.Text = display.Text;
			button. Name = choice.Value;

			button.ButtonStyle = eButtonStyle.ImageAndText;

			if(!display.Enabled)
				button.Text = button.Text + " NA";

			button.Click += new EventHandler(OnClickAreaButton);

			button.ButtonStyle = DevComponents.DotNetBar.eButtonStyle.ImageAndText;
			//button.Image = ((System.Drawing.Image)(resources.GetObject("buttonItem2.Image")));
			//	button.Name = "buttonItem2";
			button.OptionGroup = "navBar";
			button.Style = DevComponents.DotNetBar.eDotNetBarStyle.Office2003;

			button.Checked =	display.Checked;

			MakePanelToGoWithButton(NavPane, button, choice);

			NavPane.Items.Add(button);
			//a button in this framework not really a Control... so I don't know how to use
			//(the same company's) balloon tip control on a sidebar button!
			//	m_mediator.SendMessage("RegisterHelpTarget", button.ContainerControl);
#else
			string tabname = GetListIdFromListPropertyChoice(choice);
			Tab tab = new Tab(tabname)
					{
						Enabled = choice.Enabled,
						Text = choice.Label,
						Icon = m_largeImages.GetImage(choice.ImageName),
						Tag = choice,
					};
			choice.ReferenceWidget = tab;
			m_sidepane.AddTab(tab);
#endif
		}

		/// <summary>Gets choice's listId from its xml</summary>
		/// <returns>null if can't find it for some reason</returns>
		private string GetListIdFromListPropertyChoice(ListPropertyChoice choice)
		{
			try
			{
				return choice.ParameterNode.SelectSingleNode("panels").SelectSingleNode("listPanel").Attributes["listId"].Value;
			}
			catch
			{
				return null;
			}
		}

#if USE_DOTNETBAR
		protected void MakePanelToGoWithButton (NavigationPane np, ButtonItem button,ListPropertyChoice choice)
		{
			m_suspendEvents = true;
			NavigationPanePanel p = new DevComponents.DotNetBar.NavigationPanePanel();
			p.Dock = System.Windows.Forms.DockStyle.Fill;
			p.Location = new System.Drawing.Point(0, 24);
			p.ParentItem = button;

			p.Style.Alignment = System.Drawing.StringAlignment.Center;
			p.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.BarBackground;
			p.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.BarBackground2;
			p.Style.BackgroundImagePosition = DevComponents.DotNetBar.eBackgroundImagePosition.Tile;
			p.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
			p.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
			p.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemText;
			p.Style.GradientAngle = 90;
			p.Style.WordWrap = true;
			p.StyleMouseDown.Alignment = System.Drawing.StringAlignment.Center;
			p.StyleMouseDown.WordWrap = true;
			p.StyleMouseOver.Alignment = System.Drawing.StringAlignment.Center;
			p.StyleMouseOver.WordWrap = true;
			p.TabIndex = 3;

			//			Object maker =m_mediator.PropertyTable.GetValue("PanelMaker");
			//			if (maker == null)
			//			{
			//				p.Text = "You must provide a PanelMaker in the property table to create panels";
			//				p.Name = "navigationPanePanel2";
			//			}
			//			else
			//			{
			AddControlsToPanel(choice, p);
			//			}

			//p.VisibleChanged +=new EventHandler(OnPanelVisibleChanged);
			np.Controls.Add(p);
			p.Layout+=new LayoutEventHandler(OnPanelLayout);
			m_suspendEvents = false;

		}
#endif

#if USE_DOTNETBAR
		private void AddControlsToPanel(ListPropertyChoice choice, NavigationPanePanel p)
		{
			ArrayList controls =AddSubControlsForButton(choice);

			p.Controls.Clear();//DON"T DISPOSE!

			//ArrayList controls =((PanelMaker)maker).GetControlsFromChoice(choice);
			//we reversed these because we want to show them from top to bottom,
			//but the way controls work in forms, we have to actually add them in reverse order.
			if (controls != null)
			{
				controls.Reverse();
				foreach(Control control in controls)
				{

					AddOneControlToPanel(control, p, controls.Count);
				}

			}
		}

		private void AddOneControlToPanel(Control control, NavigationPanePanel navPane, int controlCount)
		{
			// As per LT-3392, this section has been commented out to remove the header
			// If we change our minds, just remove the comments and everything should work again

			//PanelEx header = new PanelEx();
			//header.Text = ((ChoiceGroup)control.Tag).Label;
			//header.Dock = DockStyle.Top;

			//header.Location = new System.Drawing.Point(1, 1);
			//header.Name = "panelEx1";
			//header.Size = new System.Drawing.Size(182, 20);
			//header.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.BarBackground;
			//header.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.BarBackground2;
			//header.Style.BackgroundImagePosition = DevComponents.DotNetBar.eBackgroundImagePosition.Tile;
			//header.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.BarDockedBorder;
			//header.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemText;
			//header.Style.GradientAngle = 90;
			//header.Style.MarginLeft = 4;

			control.Dock = DockStyle.Top;

			navPane.Controls.Add( control);
			//navPane.Controls.Add(header);
		}
#endif

		/// <summary>
		/// make a control to show, for example, the list of tools, or the list of filters.
		/// </summary>
		/// <param name="group"></param>
		protected void MakeListControl(ChoiceGroup group)
		{
			ListView list= new ListView();
			list.View = View.List;
			list.Name = group.ListId;
			list.Dock = System.Windows.Forms.DockStyle.Top;
			list.MultiSelect = false;
			list.Tag = group;
			list.SmallImageList = m_smallImages.ImageList;
			list.LargeImageList = m_largeImages.ImageList;
			list.HideSelection = false;
			group.ReferenceWidget = list;
			//			foreach(ChoiceRelatedClass choice in group)
			//			{
			//				//Debug.Assert(item is ChoiceBase, "Only things that can be made into buttons should be appearing here.");
			//				Debug.Assert(choice is ListPropertyChoice, "Only things that can be made into buttons should be appearing here.");
			//				ListViewItem x =list.Items.Add(choice.Label,1);
			//				x.Tag = choice;
			//			}

			group.OnDisplay(this, null);

			list.SelectedIndexChanged += new EventHandler(OnClickInPanelList);
			list.SizeChanged += new EventHandler(list_SizeChanged);

			m_panelSubControls.Add(list);
		}

		void list_SizeChanged(object sender, EventArgs e)
		{
			ListView list = sender as ListView;
			// if you shrink the height of a ListView while it is in the List view enough to cut off
			// some items, the items are shifted to another column and a horizontal scroll bar will appear
			// if necessary, if you scroll over to see the items in the new column and increase the height
			// of the ListView so that all of the items are in one column, the scrollbar will disappear but
			// not scroll back over to make the items visible. I am not sure if this is by design or not. To
			// solve this problem, we check to see if the top item is null, thus indicating that there are no
			// items currently visible, then we scroll back over to make the currently selected item visible.
			if (list.TopItem == null)
			{
				if (list.SelectedItems.Count > 0)
					list.SelectedItems[0].EnsureVisible();
				else if (list.Items.Count > 0)
					list.EnsureVisible(0);

				// This is a really bad hack, but it seems to be the only way that I know of to get around
				// this bug in XP. EnsureVisible doesn't seem to work when we get in to this weird state
				// in XP, so we go ahead and rebuild the entire list.
				if (list.TopItem == null && list.Items.Count > 0)
				{
					ListViewItem selected = null;
					if (list.SelectedItems.Count > 0)
						selected = list.SelectedItems[0];
					ListViewItem[] items = new ListViewItem[list.Items.Count];
					for (int i = 0; i < list.Items.Count; i++)
						items[i] = list.Items[i];
					list.Items.Clear();
					list.Items.AddRange(items);
					if (selected != null)
					{
						selected.Selected = true;
						selected.EnsureVisible();
					}
				}
			}
		}

		/// <summary>
		/// Populate a control to show, for example, the list of tools, or the list of filters.
		/// </summary>
		/// <param name="group"></param>
		protected void PopulateList(ChoiceGroup group)
		{
			bool wasSuspended = m_suspendEvents;
			m_suspendEvents = true;

			if(m_populatingList)
				return;

			m_populatingList=true;
			ListView list = (ListView) group.ReferenceWidget;
//			if(list.Items.Count == group.Count)
//				UpdateList(group);
//			else
			{
				list.BeginUpdate();
				list.Clear();

				m_choiceGroupCache.Add(group); // cache group to defer adding it to the sidepane until we definitely have tabs already

				list.EndUpdate();
			}
			m_populatingList=false;
			m_suspendEvents = wasSuspended;
		}
		/// <summary>
		/// Populate a control to show, for example, the list of tools, or the list of filters.
		/// </summary>
		/// <param name="group"></param>
//		protected void UpdateList(ChoiceGroup group)
//		{
//	//		Debug.WriteLine("Grp:"+group.Label);
//			ListView list = (ListView) group.ReferenceWidget;
//			foreach(ListPropertyChoice choice in group)
//			{
////				Debug.WriteLine(choice.Label);
//				//if(choice.Checked)
//					((ListViewItem)(choice.ReferenceWidget)).Selected = (choice.Checked);
////				else
////					((ListViewItem)(choice.ReferenceWidget)).Selected = (choice.Checked);
////				Debug.WriteLine("foo:");
//
//			}
//		}
		protected ArrayList AddSubControlsForButton (ListPropertyChoice choice)
		{
			ArrayList controls= new ArrayList ();
			foreach(Control control in m_panelSubControls)
			{
				//				if(control.Parent !=null)
				//				{
				//					control.Parent.Controls.Remove(control);
				//					//		//			string s = control.Parent.Name;
				//				}
				if (null != choice.ParameterNode.SelectSingleNode("descendant::listPanel[@listId='"+ control.Name+ "']"))
				{
					controls.Add(control);
					//string x = control.Parent.Name;
				}
			}
			return controls;
		}

		/// <summary>
		/// Handle the Button Click event.
		/// </summary>
		/// <param name="something">The button that was clicked.</param>
		/// <param name="args">Unused event arguments.</param>
		private void OnClickAreaButton(object something, System.EventArgs args)
		{
#if USE_DOTNETBAR
			ButtonItem item = (ButtonItem)something;
			ChoiceBase choice = (ChoiceBase)item.Tag;
			Debug.Assert(choice != null);
			m_control.SuspendLayout();
			choice.OnClick(item, null);
			m_control.ResumeLayout(true);
#endif

		}

#if USE_DOTNETBAR
		/// <summary>
		///	do an update of the panels assigned to this button in order to, for
		///	example, show the tools that go with the currently selected area button.
		/// </summary>
		/// <param name="panel"></param>
		private void RefreshPanel(NavigationPanePanel panel)
		{
			//			if (panel != null)
			//			{
			//				AddControlsToPanel((ListPropertyChoice)panel.ParentItem.Tag, panel);
			//				//AddSubControlsForButton((ListPropertyChoice)panel.ParentItem.Tag);
			//				panel.Refresh();
			//
			//				((NavigationPane)(m_control)).RecalcLayout();
			//				//				this.MyControl.Refresh();
			//				foreach(Control subcontrol in panel. Controls)
			//				{
			//					if (subcontrol. Tag == null)
			//						continue;
			//					ChoiceGroup group = (ChoiceGroup)subcontrol.Tag;
			//
			//					//this will cause any listeners to be able to update the list,
			//					//and then the list will turn around and request that we
			//					//update the display of the list.
			//					group.OnDisplay(this,null);
			//
			//				}
			//			}
		}
#endif

		/// <summary>
		/// triggered when the user clicks on one of the list used in the navigation bar, e.g., the tools list.
		/// </summary>
		/// <param name="something"></param>
		/// <param name="args"></param>
		private void OnClickInPanelList(object something, System.EventArgs args)
		{
			if(m_suspendEvents)
				return;

			ListView list = (ListView)something;
			if (list.SelectedIndices == null || list.SelectedIndices.Count == 0)
				return;

			ChoiceBase control = (ChoiceBase)list.SelectedItems[0].Tag;
			Debug.Assert(control != null);
			control.OnClick(this, null);
		}

		#endregion Other methods


		/// <summary>
		/// When something changes the property areaChoice (via something like a menubar),
		/// this ensures that the matching button
		/// is highlighted.
		/// </summary>
		public void OnPropertyChanged(string propertyName)
		{

			switch(propertyName)
			{
					//nb:  this is a complete hack (and even this doesn't work right now)
					//this adapter should not need to know about the specifics like that there is a
					//property with his name.

					//NB:this processing could be moved to be on idle eventrather than
					//waiting for this property to change, but then it will
					//waste this time even more often.
				case "currentContentControl":
					string contentControl = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
					foreach (Control control in m_panelSubControls)
					{
//						if(control.Visible == false)
//							break;
						ChoiceGroup g = (ChoiceGroup)control.Tag;
						foreach (ListPropertyChoice c in g)
						{
							if (c.Value == contentControl)
							{

#if USE_DOTNETBAR
								(c.ReferenceWidget as ListViewItem).Selected = true;
#else
								var item = c.ReferenceWidget as Item;
								var tab = m_sidepane.GetTabByName(g.ListId);
								this.m_sidepane.SelectItem(tab, item.Name);
#endif
								return;
							}
						}
					}
					break;
				case "areaChoice":
					string areaName = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
					//	((ChoiceGroup)this.NavPane.NavigationBar.Tag).OnDisplay(null, null);//!!!!  test
					//RefreshPanel(this.NavPane.SelectedPanel);
#if USE_DOTNETBAR
					foreach(ButtonItem button in this.NavPane.Items)
					{
						if (button.Name == areaName)
						{
							button.Checked= true;
							break;
						}
					}
#else
				foreach(ChoiceGroup group in this.m_choiceGroupCollectionCache)
				{
					foreach (ListPropertyChoice choice in group) // group must already be populated
					{
						if (choice.Value == areaName)
						{
							string listId = GetListIdFromListPropertyChoice(choice);
							var tab = m_sidepane.GetTabByName(listId);
							if (tab != null)
								this.m_sidepane.SelectTab(tab);
							break;
						}
					}
				}
#endif
					break;

				default: break;
			}
		}

#if nono
		//this did not work out well because this event, though it is a result of clicking and
		//area button, actually comes before the event that tells us the button was changed!
		//therefore, stuff that would set up the list dynamically depending on the area will get it wrong.
		//therefore, we stopped using this and moved to putting all of the handling into the
		//OnClick event for the Area button.
		private void OnPanelVisibleChanged(object sender, EventArgs e)
		{
			NavigationPanePanel panel = (NavigationPanePanel) sender;
			if (!panel.Visible)
				return;
			foreach(Control control in panel. Controls)
			{
				if (control. Tag == null)
					continue;
				ChoiceGroup group = (ChoiceGroup)control.Tag;
				group.OnDisplay(this,null);

			}
		}
#endif

		private void OnPanelLayout(object sender, LayoutEventArgs e)
		{
#if USE_DOTNETBAR
			NavigationPanePanel p = ((NavigationPanePanel)sender);

			if (p.Controls.Count == 1 && !(p.Controls[0] is PanelEx)) // No header, expand to full height
			{
				p.Controls[0].Height = p.Height;
				return;
			}

			if (p.Controls.Count < 2)//just a header
				return;
			//assume all headers are the same height
			int headerHeight =p.Controls[1].Height; //these are in reverse order so the  last header is actually slot 1 rather than 0

			foreach(Control control in p.Controls)
			{
				//assume that anything of this class is a header
				//(might be a bad assumption someday)
				if (!(control is PanelEx))
				{
					control.Height = (p.Height - (headerHeight*(p.Controls.Count/2)))/(p.Controls.Count/2);
				}
			}
#endif
		}
	}
}
