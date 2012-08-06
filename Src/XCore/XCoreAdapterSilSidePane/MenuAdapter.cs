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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;  //for ImageList
using System.Diagnostics;
using Microsoft.Win32;
using System.ComponentModel;//registrykey

// for ImageCollection

namespace XCore
{
	/// <summary>
	/// Creates the menu bar and menus for an XCore application.
	/// </summary>
	public class MenuAdapter : BarAdapterBase, IUIMenuAdapter,  ITestableUIAdapter
	{
		private TemporaryColleagueParameter m_TemporaryColleagueParameter;

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
					ms.AccessibilityObject.Name = "MainMenuStrip";
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

		#endregion Construction

		#region IUIAdapter implementation

		// Note: Init method is handled by superclass.

		/// <summary>
		/// Create a menu, but not its items.
		/// </summary>
		/// <param name="groupCollection">Collection of menu definitions to create.</param>
		public override void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
			MenuStrip s = MyMenuStrip;
			//s.MouseEnter += s_MouseEnter;
				//new MouseEventHandler(s_MouseEnter);
			//s.MouseLeave +=s_MouseLeave;
			foreach(ChoiceGroup group in groupCollection)
			{
				string label = group.Label.Replace("_", "&");

				ToolStripMenuItem item = new ToolStripMenuItem();

				item.AccessibilityObject.Name = group.Id;

				item.Text = label;

				item.Tag = group;
				group.ReferenceWidget = item;

				s.Items.Add(item);

				// This next line deals with the chicken and egg problem of
				// the dynamic dropdown menus.  Although we don't yet
				// know all the details (visibility, etc.) the dropdown
				// (and alt-key shortcuts) don't work properly without
				// being 'primed' with something before their first use.

				item_DropDownOpening(item, null);

				item.DropDownOpening += item_DropDownOpening;

			}
		}


/*  This is uneccesary, causes menus to misbehave with regards to maintaining open dropdowns once the menustrip has been entered.
		void ItemDropDownClosed (object sender, EventArgs e)
		{
			ToolStripMenuItem toolStripItem = sender as ToolStripMenuItem;
			if (toolStripItem.DropDown != null)
			{
				toolStripItem.DropDown.Close();
			}
		}
*/

		void item_DropDownOpening (object sender, EventArgs e)
		{

			ToolStripMenuItem  item = sender as ToolStripMenuItem;

			ChoiceGroup group = item.Tag as ChoiceGroup;
			if (group != null) group.OnDisplay(sender, null);

		}

		/// <summary>
		/// Populate a menu.
		/// This is called by the OnDisplay() method of some ChoiceGroup
		/// </summary>
		/// <param name="group">The group that defines this menu item.</param>
		public override void CreateUIForChoiceGroup(ChoiceGroup group)
		{
			//m_control.SuspendLayout();//doesn't help

			ToolStripMenuItem  toolStripItem = group.ReferenceWidget as ToolStripMenuItem;
			toolStripItem.DropDown.Items.Clear();

			bool somethingVisibleAlready = false;
			bool wantsSeparatorBefore = false;
			foreach(ChoiceRelatedClass item in group)
			{
				if(item is SeparatorChoice)
				{
					// don't just add one, we don't want it unless another visible item gets
					// added, and one has already been added
					wantsSeparatorBefore = somethingVisibleAlready;
				}
				else if(item is ChoiceBase)
				{
					bool reallyVisible;
					ToolStripItem newItem = (ToolStripItem)CreateMenuItem(item as ChoiceBase, out reallyVisible);
					if (reallyVisible)
					{
						somethingVisibleAlready = true;
						if (wantsSeparatorBefore)
						{
							wantsSeparatorBefore = false;
							toolStripItem.DropDown.Items.Add(new ToolStripSeparator());
						}
					}
					newItem.AccessibilityObject.Name = item.Id;

					//newItem.GetType().Name;
					toolStripItem.DropDown.Items.Add(newItem);
					//toolStripItem.
				}
				else if(item is ChoiceGroup) // Submenu
				{
					ChoiceGroup choiceGroup = (ChoiceGroup)item;
					if (choiceGroup.IsSubmenu)
					{
						string label = item.Label.Replace("_", "&");
						UIItemDisplayProperties display = choiceGroup.GetDisplayProperties();
						if (display.Visible)
						{
							somethingVisibleAlready = true;
							if (wantsSeparatorBefore)
							{
								wantsSeparatorBefore = false;
								toolStripItem.DropDown.Items.Add(new ToolStripSeparator());
							}
						}
						var submenu = new ToolStripMenuItem
							{
								Tag = item,
								Text = label
							};
						submenu.AccessibilityObject.Name = item.Id;
						// Have the submenu display characteristics behave as desired.  See FWR-3104.
						submenu.Visible = display.Visible;
						submenu.Enabled = display.Enabled;
						//submenu.GetType().Name;
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
							bool reallyVisible;
							ToolStripItem newItem = CreateMenuItem((ChoiceBase)inlineItem, out reallyVisible);
							if (reallyVisible)
							{
								somethingVisibleAlready = true;
								if (wantsSeparatorBefore)
								{
									wantsSeparatorBefore = false;
									toolStripItem.DropDown.Items.Add(new ToolStripSeparator());
								}
							}
							toolStripItem.DropDown.Items.Add(newItem);

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
		public bool HandleAltKey(KeyEventArgs e, bool wasDown)
		{
			//return m_commandBarManager.HandleAltKey(e, wasDown);
			return false;
		}

		protected const int WM_CHANGEUISTATE = 0x00000127;
		protected const int UIS_CLEAR = 2;

		protected const short UISF_HIDEACCEL = 0x0002;

		[DllImport("user32.dll")]
		public extern static int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

		/// <summary>
		/// This is supposed to force accelerator keys to be shown in the specified control.
		/// I (JohnT) have not been able to get it to work, but leave it here in case it is
		/// helpful to anyone else trying to make the stupid accelerators appear.
		/// </summary>
		public static void MakeAcceleratorsVisible(Control c)
		{
			SendMessage(c.Handle, WM_CHANGEUISTATE, UISF_HIDEACCEL << 16 | UIS_CLEAR, 0);
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
			ShowContextMenu(group, location, temporaryColleagueParam, sequencer, null);
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
			MessageSequencer sequencer, Action<ContextMenuStrip> adjustMenu)
		{
			// Store optional parameter values.
			m_TemporaryColleagueParameter = temporaryColleagueParam; // Nulls are just fine.

			// TODO-Linux FWNX-345: Review - don't use TemporaryColleague's
			// This needs to be done before PopulateNow
			if (m_TemporaryColleagueParameter != null)
				m_TemporaryColleagueParameter.Mediator.AddTemporaryColleague(m_TemporaryColleagueParameter.TemporaryColleague);

			// item is used in calling CreateUIForChoiceGroup to attach the choiceGroup
			// menu items to. It is not added to the shown contextMenu.
			ToolStripMenuItem item = new ToolStripMenuItem();
			item.AccessibilityObject.Name = group.Id;
			//item.GetType().Name;
			item.Tag = group;
			group.ReferenceWidget = item;
			group.PopulateNow();
			CreateUIForChoiceGroup(group);

			bool menuOK = false;
			foreach (var menuItem in item.DropDown.Items)
			{
				if (menuItem is ToolStripMenuItem)
				{
					if (((ToolStripMenuItem)menuItem).Text == "Show in Word Analyses")
					{
						menuOK = true;
						break;
					}
				}
			}
			if (!menuOK)
			{
				Debug.WriteLine("Show in Word Analyses is missing:");
				Debug.WriteLine(m_mediator.GetColleaguesDumpString());
			}
			// NOTE: we intentionally leave contextMenu undisposed. If we dispose it after
			// contextMenu.Show then the mouse clicks on the menu items don't get handled.
			// We would have to add an Application.DoEvents() after the Show (which might
			// causes other problems), or implement IDisposable.
			var contextMenu = new ContextMenuStrip();
			contextMenu.AccessibilityObject.Name = group.Id;

			// Without building this collection first, somehow we modify the Items collection while
			// iterating.
			var items = new System.Collections.Generic.List<ToolStripItem>();
			foreach (var menuItem in item.DropDown.Items)
			{
				if (menuItem is ToolStripMenuItem )
					items.Add(menuItem as ToolStripMenuItem);
				else if ( menuItem is ToolStripButton)
					items.Add(menuItem as ToolStripButton);
				else if (menuItem is ToolStripSeparator)
					items.Add(menuItem as ToolStripSeparator);
			}
			foreach (var menuItem in items)
			{
				contextMenu.Items.Add(menuItem);
			}
			MakeAcceleratorsVisible(contextMenu);
			if (adjustMenu != null)
				adjustMenu(contextMenu);
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

		static public string GetStringRegistryValue(string key, string defaultValue)
		{
			using (var software = Registry.CurrentUser.OpenSubKey("Software", false))
			using (var company = software.OpenSubKey(Application.CompanyName, false))
			{
				if (company != null)
				{
					using (RegistryKey application = company.OpenSubKey(Application.ProductName, false))
					{
						if (application != null)
						{
							foreach (string sKey in application.GetValueNames())
							{
								if (sKey == key)
								{
									return (string)application.GetValue(sKey);
								}
							}
						}
					}
				}
			}
			return defaultValue;
		}
	}
}
