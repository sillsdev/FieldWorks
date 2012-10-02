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
		protected override MenuStrip MyMenuStrip
		{
			get
			{
				if (m_window.MainMenuStrip == null)
				{
					MenuStrip ms = new MenuStrip();
					ms.Dock = DockStyle.None;
					m_window.MainMenuStrip = ms;
					Manager.AddMenuStrip(ms);

					m_control = m_window.MainMenuStrip;
				}

				return m_window.MainMenuStrip;
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
			foreach(ChoiceGroup group in groupCollection)
			{
				string label = group.Label.Replace("_", "&");
				MenuStrip s = MyMenuStrip;
				ToolStripMenuItem item = new ToolStripMenuItem();
				item.Text = label;
				item.Tag = group;
				group.ReferenceWidget = item;
				s.Items.Add(item);
				item.MouseEnter += new EventHandler(item_MouseEnter);
				item.DropDownOpening += ItemDropDownOpening;
				// item.MouseEnter += ItemDropDownOpening; // TODO-Linux: just captuing DropDownOpening only shows menu if its been clicked on at least once.
				item.DropDownClosed += ItemDropDownClosed;

			}
		}

		void item_MouseEnter(object sender, EventArgs e)
		{

			ToolStripMenuItem m = sender as ToolStripMenuItem;
			m.ShowDropDown();
		}

		void ItemDropDownClosed (object sender, EventArgs e)
		{
			ToolStripMenuItem toolStripItem = sender as ToolStripMenuItem;
			if (toolStripItem.DropDown != null)
			{
				toolStripItem.DropDown.Close();
			}
		}

		void ItemDropDownOpening (object sender, EventArgs e)
		{
			ToolStripMenuItem  item = sender as ToolStripMenuItem;
			ChoiceGroup group = item.Tag as ChoiceGroup;
			group.OnDisplay(sender, null);
		}

		/// <summary>
		/// Populate a menu.
		/// This is called by the OnDisplay() method of some ChoiceGroup
		/// </summary>
		/// <param name="group">The group that defines this menu item.</param>
		public override void CreateUIForChoiceGroup(ChoiceGroup group)
		{
			m_control.SuspendLayout();//doesn't help

			ToolStripMenuItem  toolStripItem = group.ReferenceWidget as ToolStripMenuItem;
			toolStripItem.DropDown.Items.Clear();

			bool wantsSeparatorBefore = false;
			foreach(ChoiceRelatedClass item in group)
			{
				if(item is SeparatorChoice)
					wantsSeparatorBefore = true;
				else if(item is ChoiceBase)
				{
					ToolStripItem newItem = (ToolStripItem)CreateButtonItem(item as ChoiceBase, wantsSeparatorBefore);
					toolStripItem.DropDown.Items.Add(newItem);
					//toolStripItem.
					wantsSeparatorBefore = false;
				}
				else if(item is ChoiceGroup) // Submenu
				{
					ChoiceGroup choiceGroup = (ChoiceGroup)item;
					if (choiceGroup.IsSubmenu)
					{
						string label = item.Label.Replace("_", "&");
						UIItemDisplayProperties display = choiceGroup.GetDisplayProperties();
						var submenu = new ToolStripMenuItem()
							{
								Tag = item,
								Text = label
							};

						item.ReferenceWidget = submenu;

						toolStripItem.DropDown.Items.Add(submenu);

						if (display.Visible && display.Enabled)
						{
							choiceGroup.OnDisplay(m_window, null);
						}

					}
					else if (choiceGroup.IsInlineChoiceList)
					{
						choiceGroup.PopulateNow();
						foreach(ChoiceRelatedClass inlineItem in choiceGroup)
						{
							Debug.Assert(inlineItem is ChoiceBase, "It should not be possible for an in line choice list to contain anything other than simple items!");
							toolStripItem.DropDown.Items.Add(CreateButtonItem((ChoiceBase)inlineItem, false));

						}
					}
				}
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

			// TODO-Linux FWNX-345: Review - don't use TemporaryColleague's
			// This needs to be done before PopulateNow
			if (m_temporaryColleagueParam != null)
				m_temporaryColleagueParam.Mediator.AddTemporaryColleague(m_temporaryColleagueParam.TemporaryColleague);

			// item is used in calling CreateUIForChoiceGroup to attach the choiceGroup
			// menu items to. It is not added to the shown contextMenu.
			ToolStripMenuItem item = new ToolStripMenuItem();
			item.Tag = group;
			group.ReferenceWidget = item;
			group.PopulateNow();
			CreateUIForChoiceGroup(group);

			var contextMenu = new ContextMenuStrip();
			foreach(ToolStripMenuItem menuItem  in item.DropDown.Items)
			{
				contextMenu.Items.Add(menuItem as ToolStripMenuItem);
			}

			contextMenu.Show(location);
		}

		#endregion IUIMenuAdapter implementation


		#region ITestableUIAdapter

		public int GetItemCountOfGroup (string groupId)
		{
			// TODO: Implement for this version of adapter.
			return 0;
		}

		/// <summary>
		/// simulate a click on a menu item.
		/// </summary>
		/// <param name="groupId">The id of the menu</param>
		/// <param name="itemId">the id of the item.  As of this writing, this often defaults to the label without the "_"</param>
		public  void ClickItem (string groupId, string itemId)
		{
			// TODO: Implement for this version of adapter.
		}

		public bool IsItemEnabled(string groupId, string itemId)
		{
			// TODO: Implement for this version of adapter.
			return false;
		}

		public bool HasItem(string groupId, string itemId)
		{
			// TODO: Implement for this version of adapter.
			return false;
		}

		public void ClickOnEverything()
		{
			// TODO: Implement for this version of adapter.
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
			// TODO: Implement for this version of adapter.
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
