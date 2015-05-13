// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MenuAdapter.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using Reflector.UserInterface; //commandbar
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for MenuAdapter.
	/// </summary>
	public class MenuAdapter : CommandBarAdaptor, IUIAdapter, ITestableUIAdapter, IUIMenuAdapter
	{
		private CommandBar m_menuBar;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MenuAdapter"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public MenuAdapter()
		{
		}

		protected override void Dispose(bool fDisposing)
		{
			if (fDisposing)
			{
				if (m_menuBar != null)
					m_menuBar.Dispose();
			}
			m_menuBar = null;
			base.Dispose(fDisposing);
		}

		public Control Init(Form window, IImageCollection smallImages, IImageCollection largeImages, Mediator mediator, PropertyTable propertyTable)
		{
			m_window = window;
			m_smallImages = smallImages;
			m_largeImages = largeImages;
			m_menuBar = new CommandBar(CommandBarStyle.Menu);

			return null; //this is not available yet. caller should call GetCommandBarManager() after CreateUIForChoiceGroupCollection() is called
		}


		/// <summary>
		/// store the location/settings of various widgets so that we can restore them next time
		/// </summary>
		public virtual void PersistLayout()
		{
		}

		//HACK, because without this the CommandBar was not getting keyboard events
		public bool HandleAltKey(System.Windows.Forms.KeyEventArgs e, bool wasDown)
		{
			return m_commandBarManager.HandleAltKey(e, wasDown);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="CommandBarItem added to m_menuBar and disposed in Dispose()")]
		public void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
			bool weCreatedTheBarManager = GetCommandBarManager();
			m_commandBarManager.CommandBars.Add(m_menuBar);

			foreach(ChoiceGroup group in groupCollection)
			{
				MakeMenu(m_menuBar, group);
			}

			if(weCreatedTheBarManager)
				m_window.Controls.Add(m_commandBarManager);

		}

		public void OnClick(object something, System.EventArgs args)
		{
			CommandBarItem item = (CommandBarItem)something;

			//			ToolBarButton button = args.Button;
			ChoiceBase control = (ChoiceBase) item.Tag;
			Debug.Assert( control != null);
			control.OnClick(item, null);
		}

		//		public void CreateUIForChoiceGroup (ChoiceGroup group)
		//		{
		//			foreach(ChoiceBase control in group)
		//			{
		//				CommandBar toolbar = (CommandBar)group.ReferenceWidget;
		//				Debug.Assert( toolbar != null);
		//				//System.Windows.Forms.Keys shortcut = System.Windows.Forms.Keys.
		//				UIItemDisplayProperties display = control.GetDisplayProperties();
		//				display.Text  = display.Text .Replace("_", "");
		//
		//				Image image =m_images.GetImage(display.ImageLabel);
		//				CommandBarButton button = new CommandBarButton(image,display.Text, new EventHandler(OnClick));
		//				UpdateDisplay(display, button);
		//				button.Tag = control;
		//
		//				control.ReferenceWidget = button;
		//				toolbar.Items.Add(button);
		//				button.IsEnabled = true;
		//			}
		//		}

		/// <summary>
		/// Populate a normal menu, directly contained by the m_menuBar.
		/// This is called by the OnDisplay() method of some ChoiceGroup
		/// </summary>
		/// <param name="group">The group that is the basis for this menu</param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Items get added to menu and hopefully disposed there")]
		public void CreateUIForChoiceGroup (ChoiceGroup group)
		{
			if(group.ReferenceWidget is ContextMenu)
			{
				CreateContextMenuUIForChoiceGroup(group);
				return;
			}
			CommandBarMenu menu = (CommandBarMenu) group.ReferenceWidget;
			foreach (CommandBarItem item in menu.Items)
				item.Dispose();
			menu.Items.Clear();

			foreach(ChoiceRelatedClass item in group)
			{
				if(item is SeparatorChoice)
					menu.Items.Add(new CommandBarSeparator());
				else if(item is ChoiceBase)
					menu.Items.Add(CreateMenuItem ((ChoiceBase)item));
					//if this is a submenu
				else if(item is ChoiceGroup)
				{
					//					MakeMenu (menu, (ChoiceGroup)item);

					if(((ChoiceGroup)item).IsSubmenu)
					{
						string label = item.Label.Replace("_", "&");
						CommandBarMenu submenu = menu.Items.AddMenu(label);
						submenu.Tag = item;
						item.ReferenceWidget = submenu;
						//submenu.IsEnabled= true;
						//the drop down the event seems to be ignored by the submenusof this package.
						//submenu.DropDown += new System.EventHandler(((ChoiceGroup)item).OnDisplay);
						//therefore, we need to populate the submenu right now and not wait
						((ChoiceGroup)item).OnDisplay(this, null);
						//this was enough to make the menu enabled, but not enough to trigger the drop down event when chosen
						//submenu.Items.Add(new CommandBarSeparator());
					}
					else if(((ChoiceGroup)item).IsInlineChoiceList)
					{
						((ChoiceGroup)item).PopulateNow();
						foreach(ChoiceRelatedClass inlineItem in ((ChoiceGroup)item))
						{
							Debug.Assert(inlineItem is ChoiceBase, "It should not be possible for a in line choice list to contain anything other than simple items!");
							menu.Items.Add(CreateMenuItem ((ChoiceBase)inlineItem));
						}
					}
				}
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Items get added to menu and hopefully disposed there")]
		public void CreateContextMenuUIForChoiceGroup (ChoiceGroup group)
		{
			CommandBarContextMenu menu = (CommandBarContextMenu) group.ReferenceWidget;
			foreach (CommandBarItem item in menu.Items)
				item.Dispose();
			menu.Items.Clear();

			foreach(ChoiceRelatedClass item in group)
			{
				if(item is ChoiceBase)
					menu.Items.Add(CreateMenuItem ((ChoiceBase)item));
					//if this is a submenu
				else if(item is ChoiceGroup)
				{
					//					MakeMenu (menu, (ChoiceGroup)item);
					string label = item.Label.Replace("_", "&");
					CommandBarMenu submenu = menu.Items.AddMenu(label);
					submenu.Tag = item;
					item.ReferenceWidget = submenu;
					submenu.DropDown += new System.EventHandler(((ChoiceGroup)item).OnDisplay);
					//Notice that we do not need to populate this menu now; it will be populated when its contents are displayed.
				}
			}
		}


		//from IUIMenuAdapter
//		public ContextMenu MakeContextMenu (ChoiceGroup group)
//		{
//			CommandBarContextMenu menu= new CommandBarContextMenu();
//			menu.Tag = group;
//			group.ReferenceWidget = menu;
//			menu.Popup += new System.EventHandler(group.OnDisplay);
//
//			return (ContextMenu)menu;
//		}

		public void ShowContextMenu(ChoiceGroup group, Point location,
			TemporaryColleagueParameter temporaryColleagueParam,
			MessageSequencer sequencer)
		{
			using (CommandBarContextMenu menu= new CommandBarContextMenu())
			{
				menu.Tag = group;
				group.ReferenceWidget = menu;
				menu.Popup += new System.EventHandler(group.OnDisplay);

				// Pre-menu process two optional paremeters.
				if (temporaryColleagueParam != null)
					temporaryColleagueParam.Mediator.AddColleague(temporaryColleagueParam.TemporaryColleague);
				bool resume = false;
				if (sequencer != null)
					resume = sequencer.PauseMessageQueueing();

				// NB: This is *always* modal, as it doesn't return until it closes.
				menu.Show(null, location);

				// Post-menu process two optional paremeters.s
				if (temporaryColleagueParam != null)
				{
					IxCoreColleague colleague = temporaryColleagueParam.TemporaryColleague;
					temporaryColleagueParam.Mediator.RemoveColleague(colleague);
					if (temporaryColleagueParam.ShouldDispose && colleague is IDisposable)
						(colleague as IDisposable).Dispose();
				}
				if (sequencer != null && resume)
					sequencer.ResumeMessageQueueing();
			}
		}

		public void ShowContextMenu(ChoiceGroup @group, Point location, TemporaryColleagueParameter temporaryColleagueParam, MessageSequencer sequencer, Action<ContextMenuStrip> adjustMenu)
		{
			throw new NotImplementedException();
		}

		protected CommandBarItem MakeMenu (CommandBar parent, ChoiceGroup group)
		{
			string label = group.Label.Replace("_", "&");
			CommandBarMenu menu = parent.Items.AddMenu(label);
			menu.Tag = group;
			group.ReferenceWidget = menu;
			menu.DropDown += new System.EventHandler(group.OnDisplay);

#if DEBUG
			//needed for unit testing
			//menu.Click += new System.EventHandler(group.OnDisplay);
#endif
			return menu;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Variables are references or returned")]
		protected CommandBarItem CreateMenuItem(ChoiceBase choice)
		{
			//note that we could handle the details of display in two different ways.
			//either we can leave this up to the normal display mechanism, which will do its own polling,
			//or we could just build the menu in the desired state right here (enable checked etc.)

			UIItemDisplayProperties display = choice.GetDisplayProperties();

			string label = display.Text;
			bool isSeparatorBar = (label == "-");
			label = label.Replace("_", "&");
			Image image = null;
			if (display.ImageLabel!= "default")
				image = m_smallImages.GetImage(display.ImageLabel);
			CommandBarItem menuItem;
			if(choice is CommandChoice)
			{
				if (isSeparatorBar)
					menuItem = new CommandBarSeparator();
				else
					menuItem = new CommandBarButton(image, label, new EventHandler(OnClick));
			}
			else
			{
				CommandBarCheckBox cb = new CommandBarCheckBox(image, label);
				cb.Click += new System.EventHandler(choice.OnClick);
				cb.IsChecked = display.Checked;
				menuItem = cb;
			}
			if (!isSeparatorBar)
				((CommandBarButtonBase)menuItem).Shortcut = choice.Shortcut;

			menuItem.Tag = choice;
			menuItem.IsEnabled = !isSeparatorBar && display.Enabled;
			menuItem.IsVisible = display.Visible;
			choice.ReferenceWidget = menuItem;
			return menuItem;
		}

		/// <summary>
		/// we use the idle call here to update the enabled/disabled state of the buttons
		/// </summary>
		public void OnIdle()
		{
		}

		#region ITestableUIAdapter

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Reference only")]
		public int GetItemCountOfGroup (string groupId)
		{
			CommandBarMenu menu = GetMenu(groupId);
			//need to simulate the user clicking on this in order to actually get populated.
			//could just as well have called our method CreateUIForChoiceGroup() instead of the groups OnDisplay()
			//method, but the latter is one step closer to reality.
			((ChoiceGroup)menu.Tag).OnDisplay(null, null);
			return menu.Items.Count;
		}

		protected CommandBarMenu GetMenu(string groupId)
		{
			foreach(CommandBarItem item in  m_menuBar.Items)
			{
				if (((ChoiceRelatedClass)item.Tag).Id == groupId)
					return (CommandBarMenu)item;
				else  //look for submenus
				{
					CommandBarMenu menu = (CommandBarMenu)item;
					//need to simulate the user clicking on this in order to actually get populated.
					((ChoiceGroup)menu.Tag).OnDisplay(null, null);

					//note that some of these may be set menus, others are just items; we don't bother checking
					foreach(CommandBarItem x in menu.Items)
					{

						if (x.Tag!=null //separators don't have tags
							&& ((ChoiceRelatedClass)x.Tag).Id == groupId)
							return (CommandBarMenu)x;
					}
				}
			}
			throw new ConfigurationException("could not find menu '"+groupId +"'.");
		}

		protected CommandBarItem GetMenuItem(CommandBarMenu menu, string id)
		{
			//need to simulate the user clicking on this in order to actually get populated.
			((ChoiceGroup)menu.Tag).OnDisplay(null, null);

			foreach(CommandBarItem item in menu.Items)
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="GetMenu() returns a reference")]
		public void ClickItem (string groupId, string itemId)
		{
			CommandBarMenu menu = GetMenu(groupId);
			if(menu == null)
				throw new ConfigurationException("Could not find the menu with an Id of '"+groupId+"'.");
			CommandBarItem item= GetMenuItem(menu, itemId);
			if(item == null)
				throw new ConfigurationException("Could not find the item with an Id of '"+itemId+"' in the menu '"+groupId+"'.");

			OnClick(item, null);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="GetMenu() returns a reference")]
		public bool IsItemEnabled(string groupId, string itemId)
		{
			CommandBarMenu menu = GetMenu(groupId);
			//need to simulate the user clicking on this in order to actually get populated.
			((ChoiceGroup)menu.Tag).OnDisplay(null, null);
			CommandBarItem item= GetMenuItem(menu, itemId);
			return item.IsEnabled;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="GetMenu() returns a reference")]
		public bool HasItem(string groupId, string itemId)
		{
			CommandBarMenu menu = GetMenu(groupId);
			//need to simulate the user clicking on this in order to actually get populated.
			((ChoiceGroup)menu.Tag).OnDisplay(null, null);
			CommandBarItem item= GetMenuItem(menu, itemId);
			return item != null;
		}

		public void ClickOnEverything()
		{
			foreach(CommandBarMenu menu in  m_menuBar.Items)
			{
				ClickOnAllItems(menu);
			}
		}

		protected void ClickOnAllItems (CommandBarMenu menu)
		{
			//need to simulate the user clicking on this in order to actually get populated.
			((ChoiceGroup)menu.Tag).OnDisplay(null, null);

			foreach(CommandBarItem item in menu.Items)
			{
				if(item.Tag is ChoiceGroup)		//submenu
				{
					ClickOnAllItems((CommandBarMenu)item);
				}
				else
					((ChoiceBase)item.Tag).OnClick(null,null);
			}
		}
		#endregion
	}
}
