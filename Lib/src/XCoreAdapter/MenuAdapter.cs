// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: ReBarMenuAdapter.cs
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
using Microsoft.Win32;//registrykey

using DevComponents.DotNetBar;

using SIL.Utils; // for ImageCollection

namespace XCore
{
	/// <summary>
	/// Creates the menu bar and menus for an XCore application.
	/// </summary>
	public class MenuAdapter : BarAdapterBase, IUIMenuAdapter,  ITestableUIAdapter
	{
		private TemporaryColleagueParameter m_temporaryColleagueParam;
		private MessageSequencer m_sequencer;
		private bool m_sequencerIsPaused = false;

		#region Properties

		/// <summary>
		/// Override, so it can create the main menu bar. There is to be only one of these.
		/// </summary>
		protected override Bar MyBar
		{
			get
			{
				if (m_control == null)
				{
					Bar bar = new Bar("MenuBar");
					bar.CanCustomize = false;
					m_control = bar;
					bar.MenuBar = true;
					bar.Stretch = true;
					bar.CanUndock = false;
					bar.GrabHandleStyle = eGrabHandleStyle.None;
					if(	m_mediator.PropertyTable.GetBoolProperty("UseOffice2003Style", false))
					{
						bar.Style = eDotNetBarStyle.Office2003;
						bar.ItemsContainer.Style = eDotNetBarStyle.Office2003;
					}
					else
					{
						bar.Style = eDotNetBarStyle.OfficeXP;
						bar.ItemsContainer.Style = eDotNetBarStyle.OfficeXP;
					}
					Manager.Bars.Add(bar);
					bar.DockSide = eDockSide.Top;
				}
				return (Bar)m_control;
			}
		}

		#endregion Properties

		#region Construction

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public MenuAdapter()
		{
		}

		#endregion Construction

		#region IUIAdapter implementation

		// Note: Init method is handled by superclass.

		/// <summary>
		/// Create a menu, but not its items.
		/// </summary>
		/// <param name="groupCollection">Collection of menu definitions to create.</param>
		public override void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
			//for the automated gui testing
			bool accessibilityMode = GetStringRegistryValue("AccessibilityTestingMode","false")=="true";

			foreach(ChoiceGroup group in groupCollection)
			{
				Bar bar = MyBar;
				string label = group.Label.Replace("_", "&");
				ButtonItem menu = new ButtonItem(group.Id, label);
				bar.Items.Add(menu);
				menu.Tag = group;
				group.ReferenceWidget = menu;

				menu.SubItems.Add(new ButtonItem("just to make popup happen"));
				menu.PopupOpen+=new DevComponents.DotNetBar.DotNetBarManager.PopupOpenEventHandler(menu_PopupOpen);
				if(!accessibilityMode)
					menu.PopupClose+=new EventHandler(menu_PopupClose);
			}
		}
		public void menu_PopupOpen(Object sender, DevComponents.DotNetBar.PopupOpenEventArgs args)
		{
			ButtonItem  b = (ButtonItem)sender;
			ChoiceGroup group = (ChoiceGroup)b.Tag;
			group.OnDisplay(sender, null);
			// If none of the menu items are visible, cancel the popup altogether.  See LT-4342.
			int cVisible = 0;
			for (int i = 0; i < b.SubItems.Count; ++i)
			{
				if (b.SubItems[i].Visible)
					++cVisible;
			}
			if (cVisible == 0)
				args.Cancel = true;
		}


		/// <summary>
		/// Populate a menu.
		/// This is called by the OnDisplay() method of some ChoiceGroup
		/// </summary>
		/// <param name="group">The group that defines this menu item.</param>
		public override void CreateUIForChoiceGroup(ChoiceGroup group)
		{
			m_control.SuspendLayout();//doesn't help

			ButtonItem menu = (ButtonItem)group.ReferenceWidget;

			if(menu.SubItems.Count>0)
				Debug.WriteLine("CreateUIForChoiceGroup "+group.Label);

			menu.SubItems.Clear();
			bool wantsSeparatorBefore = false;
			foreach(ChoiceRelatedClass item in group)
			{
				if(item is SeparatorChoice)
					wantsSeparatorBefore = true;
				else if(item is ChoiceBase)
				{
					menu.SubItems.Add(CreateButtonItem((ChoiceBase)item, wantsSeparatorBefore));
					wantsSeparatorBefore = false;
				}
				else if(item is ChoiceGroup) // Submenu
				{
					ChoiceGroup choiceGroup = (ChoiceGroup)item;
					if (choiceGroup.IsSubmenu)
					{
						UIItemDisplayProperties display = choiceGroup.GetDisplayProperties();

						string label = item.Label.Replace("_", "&");
						ButtonItem submenu = new ButtonItem(item.Id, label);
						submenu.BeginGroup = wantsSeparatorBefore;
						wantsSeparatorBefore=false;
						menu.SubItems.Add(submenu);
						submenu.Tag = item;
						item.ReferenceWidget = submenu;
						submenu.Visible = display.Visible;
						if (display.Visible)
						{
							// Can be visible and either enabled or disabled.
							submenu.Enabled = display.Enabled;
							if(display.Enabled)
							{
								//the drop down the event seems to be ignored by the submenusof this package.
								//submenu.DropDown += new System.EventHandler(((ChoiceGroup)item).OnDisplay);
								//therefore, we need to populate the submenu right now and not wait
								choiceGroup.OnDisplay(m_window, null);
								//hide if no sub items were added REVIEW: would it be better to just disable?
								//submenu.Visible = submenu.SubItems.Count > 0;
							}
							else	//if we don't have *any* items, it won't look like a submenu
							{
								submenu.SubItems.Add(new DevComponents.DotNetBar.ButtonItem("dummy","dummy"));
							}
						}
						else
							submenu.Enabled = false; // Not visible, so no need for it to be enabled.

						//this was enough to make the menu enabled, but not enough to trigger the drop down event when chosen
						//submenu.Items.Add(new CommandBarSeparator());
					}
					else if (choiceGroup.IsInlineChoiceList)
					{
						choiceGroup.PopulateNow();
						foreach(ChoiceRelatedClass inlineItem in choiceGroup)
						{
							Debug.Assert(inlineItem is ChoiceBase, "It should not be possible for an in line choice list to contain anything other than simple items!");
							menu.SubItems.Add(CreateButtonItem((ChoiceBase)inlineItem, false));
						}
					}
					else
						throw new ApplicationException("Unknown kind of ChoiceGroup.");
				}
				else
					throw new ApplicationException("Unknown kind of ChoiceRelatedClass.");
			}

			m_control.ResumeLayout();//doesn't help

		}

		#endregion IUIAdapter implementation

		#region IUIMenuAdapter implementation


		//HACK, because without the CommandBar was not getting keyboard events
		/// <summary>
		/// TODO: Implement this.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="wasDown"></param>
		/// <returns></returns>
		public bool HandleAltKey(System.Windows.Forms.KeyEventArgs e, bool wasDown)
		{
			//return m_commandBarManager.HandleAltKey(e, wasDown);
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="group"></param>
		/// <param name="location"></param>
		/// <param name="temporaryColleagueParam"></param>
		/// <param name="sequencer"></param>
		public void ShowContextMenu(ChoiceGroup group, Point location,
			TemporaryColleagueParameter temporaryColleagueParam,
			MessageSequencer sequencer)
		{
			// Store optional parameter values.
			m_temporaryColleagueParam = temporaryColleagueParam; // Nulls are just fine.
			m_sequencer = sequencer; // Nulls are just fine.

			ButtonItem b = new ButtonItem();
			b.PopupType = DevComponents.DotNetBar.ePopupType.Menu;
			//ContextMenu menu = new ContextMenu();

			b.Tag = group;
			group.ReferenceWidget = b;
			b.SubItems.Add(new ButtonItem("just to make popup happen"));

			Manager.RegisterPopup(b);
			// This will be populated when this event fires, just to make it parallel to how menubar menus work.
			b.PopupOpen += new DevComponents.DotNetBar.DotNetBarManager.PopupOpenEventHandler(menu_PopupOpen);
			// It's too early to remove the temporary colleague, even in these event handlers,
			// since the Mediator hasn't invoked anything on it yet.
			// b.PopupClose += new EventHandler(OnContextMenuClose);
			// b.PopupFinalized += new EventHandler(b_PopupFinalized);

			// 'b' is not modal, so if we have either a temporaryColleagueParam,
			// or a sequecner, or both, we need to preprocess them, before showing the menu.
			// We also need to do some post-processing with them, after the menu closes.
			// That is done in the OnContextMenuClose handler.
			if (m_temporaryColleagueParam != null)
				m_temporaryColleagueParam.Mediator.AddTemporaryColleague(m_temporaryColleagueParam.TemporaryColleague);
			m_sequencerIsPaused = false;
			//if (m_sequencer != null)
			//	m_sequencerIsPaused = m_sequencer.PauseMessageQueueing();

			b.Popup(location.X, location.Y);
		}

		/*
		void b_PopupFinalized(object sender, EventArgs e)
		{
			// It's too early to remove the temporary colleague, even now,
			// since the Mediator hasn't invoked anything on it yet.
			//if (m_temporaryColleagueParam != null)
			//{
			//    IxCoreColleague colleague = m_temporaryColleagueParam.TemporaryColleague;
			//    m_temporaryColleagueParam.Mediator.RemoveColleague(colleague);
			//    if (m_temporaryColleagueParam.ShouldDispose && colleague is IDisposable)
			//        (colleague as IDisposable).Dispose();
			//}
			//if (m_sequencer != null && m_sequencerIsPaused)
			//    m_sequencer.ResumeMessageQueueing();
		}*/

		#endregion IUIMenuAdapter implementation

		/*
		/// <summary>
		/// Make sure the temorary XCCore colleague is removed, if we have it,
		/// and that the MessageSequencer is resumed, if we have it.
		/// Otherwise, just close.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnContextMenuClose(object sender, EventArgs e)
		{
			// It's too early to remove the temporary colleague, even now,
			// since the Mediator hasn't invoked anything on it yet.
			//if (m_temporaryColleagueParam != null)
			//{
			//    IxCoreColleague colleague = m_temporaryColleagueParam.TemporaryColleague;
			//    m_temporaryColleagueParam.Mediator.RemoveColleague(colleague);
			//    if (m_temporaryColleagueParam.ShouldDispose && colleague is IDisposable)
			//        (colleague as IDisposable).Dispose();
			//}
			//if (m_sequencer != null && m_sequencerIsPaused)
			//    m_sequencer.ResumeMessageQueueing();
		}*/


		#region ITestableUIAdapter

		public int GetItemCountOfGroup (string groupId)
		{
			ButtonItem menu = GetMenu(groupId);
			//need to simulate the user clicking on this in order to actually get populated.
			//could just as well have called our method CreateUIForChoiceGroup() instead of the groups OnDisplay()
			//method, but the latter is one step closer to reality.
			((ChoiceGroup)menu.Tag).OnDisplay(null, null);
			return menu.SubItems.Count;

		}



		protected ButtonItem GetMenu(string groupId)
		{
			foreach(ButtonItem item in  this.MyBar.Items)
			{
				if (((ChoiceRelatedClass)item.Tag).Id == groupId)
					return (ButtonItem)item;
				else  //look for submenus
				{
					ButtonItem menu = (ButtonItem)item;
					//need to simulate the user clicking on this in order to actually get populated.
					((ChoiceGroup)menu.Tag).OnDisplay(null, null);

					//note that some of these may be set menus, others are just items; we don't bother checking
					foreach(ButtonItem x in  menu.SubItems)
					{

						if (x.Tag!=null //separators don't have tags
							&& ((ChoiceRelatedClass)x.Tag).Id == groupId)
							return (ButtonItem)x;
					}
				}
			}
			throw new ConfigurationException("could not find menu '"+groupId +"'.");
		}

		protected ButtonItem GetMenuItem(ButtonItem menu, string id)
		{
			//need to simulate the user clicking on this in order to actually get populated.
			((ChoiceGroup)menu.Tag).OnDisplay(null, null);

			foreach(ButtonItem item in menu.SubItems)
			{
				if (((ChoiceRelatedClass)item.Tag).Id == id)
					return item;

			}
			//NO NO: this is not necessarily an error. remember, we are testing!
			//		throw new ConfigurationException("could not find item '"+id +"'.");
			return null;
		}


		/// <summary>
		/// simulate a click on a menu item.
		/// </summary>
		/// <param name="groupId">The id of the menu</param>
		/// <param name="itemId">the id of the item.  As of this writing, this often defaults to the label without the "_"</param>
		public  void ClickItem (string groupId, string itemId)
		{
			ButtonItem menu = GetMenu(groupId);
			if(menu == null)
				throw new ConfigurationException("Could not find the menu with an Id of '"+groupId+"'.");
			ButtonItem item= GetMenuItem(menu, itemId);
			if(item == null)
				throw new ConfigurationException("Could not find the item with an Id of '"+itemId+"' in the menu '"+groupId+"'.");

			OnClick(item, null);
		}

		public bool IsItemEnabled(string groupId, string itemId)
		{
			ButtonItem menu = GetMenu(groupId);
			//need to simulate the user clicking on this in order to actually get populated.
			((ChoiceGroup)menu.Tag).OnDisplay(null, null);
			ButtonItem item= GetMenuItem(menu, itemId);
			return item.Enabled;
		}

		public bool HasItem(string groupId, string itemId)
		{
			ButtonItem menu = GetMenu(groupId);
			//need to simulate the user clicking on this in order to actually get populated.
			((ChoiceGroup)menu.Tag).OnDisplay(null, null);
			ButtonItem item= GetMenuItem(menu, itemId);
			return item != null;
		}

		public void ClickOnEverything()
		{
			foreach(ButtonItem menu in  this.MyBar.Items)
			{
				ClickOnAllItems(menu);
			}
		}

		protected void ClickOnAllItems (ButtonItem menu)
		{
			//need to simulate the user clicking on this in order to actually get populated.
			((ChoiceGroup)menu.Tag).OnDisplay(null, null);

			foreach(ButtonItem item in menu.SubItems)
			{
				if(item.Tag is ChoiceGroup)		//submenu
				{
					ClickOnAllItems((ButtonItem)item);
				}
				else
					((ChoiceBase)item.Tag).OnClick(null,null);
			}
		}
		#endregion

		/// <summary>
		/// Do any actions that are needed on the menu when it is closing.
		/// </summary>
		/// <remarks> This fixes a long-running and difficult to find bug.
		/// Your is the scenario: the current text selection is a simple insertion point.
		/// The user clicks on the edit menu, causing the enabled flag on the copy command
		/// to be set to disabled.  The user now selects some texts, and presses control C.
		/// DotNetBar is swallowing that key press, either executing it nor
		/// allowing it to go through to our
		/// handler on xWindow, because it considers the EditCopy command to be disabled.
		/// Even though DotNetBar has a property for telling it to ignore certain keys
		/// (AutoDispatchShortcuts), putting control C in that property does not help at all...
		/// it's not clear if this is a bug in DotNetBar or just something I (JH)
		/// don't understand.</remarks>
		///
		/// <remarks>There are several apparent solutions:
		/// One is to somehow to keep the enabled state of the menu items up to date.This is
		/// difficult to do without either complexity or doing something at idle time which
		/// would seem to open the possibility of timing-related bugs (The TE team
		/// has done it this way currently).
		/// Another solution is to take DotNetBar out of the key handling business,
		/// and just let xWindow.XWindow_KeyDown() handle all key presses. We could do this by
		/// taking action as the menu is closing to either remove all shortcut keys, or
		/// clear the menu of all items. DotNetBar wilt us not try to catch any keys.
		///	Here, I have chosen to clear the menu of all real items.
		/// This is no problem because each menu is recreated in XCore each time it is displayed anyways.
		/// I've gone with the solution that seems the least likely to have any side effects or related bugs,
		/// while also being probably the fastest-execution time solution. Note this has the
		/// nice side effect that all shortcut key-activated commands are handled by the same code,
		/// rather than some been handled by xwindow (if their menu has not been made visible yet),
		/// and some being handled by the DotNetBar adapter.  Now they are all always
		/// handled by xWindow.XWindow_KeyDown().
		/// </remarks>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menu_PopupClose(object sender, EventArgs e)
		{
			ButtonItem  menu = (ButtonItem)sender;

			//clearing if a submenu is open strands it on the screen after the parent is gone
			//so first close them (note that it is possible to have 2 open at once, momentarily)
			foreach(ButtonItem m in  menu.SubItems)
			{
				if(m.Expanded)
				{
					m.Expanded = false;
				}
			}
			menu.SubItems.Clear();
			menu.SubItems.Add(new ButtonItem());//need a dummy otherwise it never pops open again
		}

		static public string GetStringRegistryValue(string key, string defaultValue)
		{
			RegistryKey company = Registry.CurrentUser.OpenSubKey("Software", false).OpenSubKey(Application.CompanyName, false);
			if( company != null )
			{
				RegistryKey application = company.OpenSubKey( Application.ProductName, false);
				if( application != null )
				{
					foreach(string sKey in application.GetValueNames())
					{
						if( sKey == key )
						{
							return (string)application.GetValue(sKey);
						}
					}
				}
			}
			return defaultValue;
		}

	}
}
