// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
// </copyright>
#endregion
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


using SIL.Utils; // for ImageCollection
using SIL.SilSidePane;

namespace XCore
{
	/// <summary>
	/// Sidebar adapter to use SilSidePane
	/// </summary>
	public class SidebarAdapter : AdapterBase, IxCoreColleague, IDisposable
	{
		protected ArrayList m_panelSubControls;
		protected bool m_populatingList;
		protected bool 	m_suspendEvents;
		protected bool m_firstLoad = true;
		private SidePane m_sidepane; // sidebar on which to show tabs and items
		private ChoiceGroupCollection m_choiceGroupCollectionCache; // cache of what was received by CreateUIForChoiceGroupCollection()
		private List<ChoiceGroup> m_choiceGroupCache = new List<ChoiceGroup>(); // cache of what was received by CreateUIForChoiceGroup()
		private string areasLabel; // label of the special case ChoiceGroup for Areas

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~SidebarAdapter()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_sidepane != null)
					m_sidepane.Dispose();
			}
			m_sidepane = null;
			IsDisposed = true;
		}
		#endregion
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
					m_control = new Panel();
					m_control.Width  = 140;
					m_control.Height = 300;
				}
				m_control.AccessibilityObject.Name = "panel";
					//m_control.GetType().Name;
				return m_control;

			}
		}





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

		protected Panel MyPanel
		{
			get { return (Panel)MyControl; }
		}


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
			m_sidepane.AccessibilityObject.Name = "sidepane";
				//m_sidepane.GetType().Name;
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
			// use MyPanel
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
			// use MyPanel
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
		}

		/// <summary>
		/// Make sidebar tabs from group. Cache other information we receive to help
		/// make items later.
		/// </summary>
		/// <param name="group">The group that defines this part of the sidebar.</param>
		public override void CreateUIForChoiceGroup(ChoiceGroup group)
		{
			if (group.ReferenceWidget == this.MyPanel) // Is this an "Areas" group?
				LoadAreaButtons(group);
			else
				PopulateList(group);

			if(m_firstLoad)
			{
				m_firstLoad = false;
				DepersistLayout();
			}
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
			foreach (ChoiceRelatedClass tab in group)
			{
				//Debug.Assert(item is ChoiceBase, "Only things that can be made into buttons should be appearing here.");
				Debug.Assert(tab is ListPropertyChoice, "Only things that can be made into buttons should be appearing here.");
				MakeAreaButton((ListPropertyChoice)tab);
			}

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
		/// Create a tab on the sidebar from a choice.
		/// </summary>
		protected void MakeAreaButton(ListPropertyChoice choice)
		{
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

		}


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
		/// this ensures that the matching button is highlighted.
		/// This also handles getting the tool right when the area (or the tool) changes.
		/// </summary>
		public void OnPropertyChanged(string propertyName)
		{
			switch(propertyName)
			{
				case "areaChoice":
					string areaName = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
					foreach(ChoiceGroup group in m_choiceGroupCollectionCache)
					{
						foreach (ListPropertyChoice choice in group) // group must already be populated
						{
							if (choice.Value == areaName)
							{
								string listId = GetListIdFromListPropertyChoice(choice);
								var tab = m_sidepane.GetTabByName(listId);
								if (tab != null)
								{
									// We need to explicitly set the tool if we can, since the sidepane will
									// choose the first tool if one has never been chosen, and end up
									// overwriting the property table value.  See FWR-2004.
									string propToolForArea = "ToolForAreaNamed_" + areaName;
									string toolForArea = m_mediator.PropertyTable.GetStringProperty(propToolForArea, null);
									m_sidepane.SelectTab(tab);
									// FWR-2895 Deleting a Custom list could result in needing to
									// update the PropertyTable.
									if (!String.IsNullOrEmpty(toolForArea) && !toolForArea.StartsWith("?"))
									{
										var fsuccess = m_sidepane.SelectItem(tab, toolForArea);
										if (!fsuccess)
											m_mediator.PropertyTable.SetProperty(propToolForArea, null);
									}
								}
								break;
							}
						}
					}
					break;

				default:
					// This helps fix FWR-2004.
					if (propertyName.StartsWith("ToolForAreaNamed_"))
					{
						string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
						string propToolForArea = "ToolForAreaNamed_" + areaChoice;
						if (propertyName == propToolForArea)
						{
							string toolForArea = m_mediator.PropertyTable.GetStringProperty(propertyName, null);
							SetToolForCurrentArea(toolForArea);
						}
					}
					break;
			}
		}

		private void SetToolForCurrentArea(string tool)
		{
			foreach (Control control in m_panelSubControls)
			{
				ChoiceGroup group = (ChoiceGroup)control.Tag;
				foreach (ListPropertyChoice choice in group)
				{
					if (choice.Value == tool)
					{

						var item = choice.ReferenceWidget as Item;
						var tab = m_sidepane.GetTabByName(group.ListId);
						m_sidepane.SelectItem(tab, item.Name);
						break;
					}
				}
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
		}
	}
}
