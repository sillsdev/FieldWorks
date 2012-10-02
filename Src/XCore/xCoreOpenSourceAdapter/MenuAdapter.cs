// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IUIAdaptor.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System. Diagnostics;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for Menu Adapter
	/// </summary>
	public class OldMenuAdapter : IUIAdapter
	{
		protected MainMenu m_menubar;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="IUIAdaptor"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public OldMenuAdapter()
		{
		}

		public System.Windows.Forms.Control Init (System.Windows.Forms.Form window, ImageCollection smallImages, ImageCollection largeImages, Mediator mediator)
		{
			m_menubar = new System.Windows.Forms.MainMenu();
			window.Menu = m_menubar;
			return null;//can't cast this as a control
		}

		/// <summary>
		/// store the location/settings of various widgets so that we can restore them next time
		/// </summary>
		public virtual void PersistLayout()
		{
		}

		/// <summary>
		/// create menus for the menubar
		/// </summary>
		/// <param name="groupCollection"></param>
		public void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
			foreach(ChoiceGroup group in groupCollection)
			{
				MakeMenu(m_menubar, group);
			}
		}

		/// <summary>
		/// Do anything that is needed after all of the other widgets have been Initialize.
		/// </summary>
		public void FinishInit()
		{
		}

		protected MenuItem MakeMenu (Menu parent, ChoiceGroup group)
		{
			string label = group.Label.Replace("_", "&");
			MenuItem menu = new MenuItem(label);
			group.ReferenceWidget = menu;
			//although we put off populating the menu until it is actually displayed,
			//we have to put a dummy menu item in there in order to get the system to fire the right event.
			menu.MenuItems.Add(new MenuItem("dummy"));
			parent.MenuItems.Add(menu);

			//needed for mousing around
			menu.Popup += new System.EventHandler(group.OnDisplay);

#if DEBUG
			//needed for unit testing
			menu.Click += new System.EventHandler(group.OnDisplay);
#endif
			return menu;
		}

		/// <summary>
		/// Populate a main menu, directly contained by the menubar.
		/// This is called by the OnDisplay() method of some ChoiceGroup
		/// </summary>
		/// <param name="group">The group that is the basis for this menu</param>
		public void CreateUIForChoiceGroup (ChoiceGroup group)
		{
			MenuItem menu = (MenuItem) group.ReferenceWidget;
			menu.MenuItems.Clear();

			foreach(ChoiceRelatedClass item in group)
			{
				if(item is ChoiceBase)
					CreateControlWidget (menu, (ChoiceBase)item);
					//if this is a submenu
				else if(item is ChoiceGroup)
				{
					MakeMenu (menu, (ChoiceGroup)item);
					//Notice that we do not need to populate this menu now; it will be populated when its contents are displayed.
					//NO! PopulateMenu(group);
				}
			}
		}



		protected void CreateControlWidget(MenuItem menu , ChoiceBase control)
		{
			string label = control.Label;
			label = label.Replace("_", "&");
			MenuItem menuItem = new MenuItem(label);
			menuItem.Click += new System.EventHandler(control.OnClick);


			//note that we could handle the details of display in two different ways.
			//either weekend of this up to the normal display mechanism, which will do its own polling,
			//or we could just build the menu in the desired state right here (enable checked etc.)

			//for now, I am going to do the latter because with the sidebar code we are using
			//that kind of polling will not be done automatically. so we do it this way here so that
			//the sidebar adapter can be written parallel to this one.
			UIItemDisplayProperties display = control.GetDisplayProperties();
			menuItem.Checked = display.Checked;
			menuItem.Enabled= display.  Enabled;
			menuItem.Text = display.Text;

			control.ReferenceWidget = menuItem;
			menu.MenuItems.Add(menuItem);
		}

		public void OnIdle()
		{
		}

	}
}
