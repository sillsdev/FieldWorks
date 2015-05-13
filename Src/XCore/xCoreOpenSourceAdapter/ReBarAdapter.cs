// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ReBarAdapter.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using Reflector.UserInterface;//commandbar

namespace XCore
{
	/// <summary>
	/// Summary description for ReBarAdapter.
	/// </summary>
	public class ReBarAdapter : CommandBarAdaptor, IUIAdapter
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReBarAdapter"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ReBarAdapter()
		{
		}

		public Control Init(Form window, IImageCollection smallImages, IImageCollection largeImages, Mediator mediator, PropertyTable propertyTable)
		{
			m_window = window;
			m_smallImages = smallImages; m_largeImages = largeImages;
			return null;
		}


		/// <summary>
		/// store the location/settings of various widgets so that we can restore them next time
		/// </summary>
		public virtual void PersistLayout()
		{
		}

		public void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
			bool weCreatedTheBarManager = GetCommandBarManager();

			foreach(ChoiceGroup group in groupCollection)
			{
				CommandBar toolbar = new CommandBar(CommandBarStyle.ToolBar);
				toolbar.Tag = group;
				group.ReferenceWidget = toolbar;

				//whereas the system was designed to only populate groups when
				//the OnDisplay method is called on a group,
				//this particular widget really really wants to have all of the buttons
				//populated before it gets added to the window.
				//therefore, we don't hope this up but instead call a bogus OnDisplay() now.
				//toolbar.VisibleChanged  += new System.EventHandler(group.OnDisplay);

				group.OnDisplay(null,null);

				this.m_commandBarManager.CommandBars.Add(toolbar);
			}

			if(weCreatedTheBarManager)
				m_window.Controls.Add(m_commandBarManager);
		}

		public void OnClick(object something, System.EventArgs args)
		{
			CommandBarButton button = (CommandBarButton)something;

			//			ToolBarButton button = args.Button;
			ChoiceBase control = (ChoiceBase) button.Tag;
			Debug.Assert( control != null);
			control.OnClick(button, null);
		}

		public void CreateUIForChoiceGroup (ChoiceGroup group)
		{
			foreach(ChoiceRelatedClass item  in group)
			{
				CommandBar toolbar = (CommandBar)group.ReferenceWidget;
				Debug.Assert( toolbar != null);

				if(item is SeparatorChoice)
				{
					toolbar.Items.Add(new CommandBarSeparator());
				}
				else if(item is ChoiceBase)
				{
					ChoiceBase control=(ChoiceBase) item;
					//System.Windows.Forms.Keys shortcut = System.Windows.Forms.Keys.
					UIItemDisplayProperties display = control.GetDisplayProperties();
					display.Text  = display.Text .Replace("_", "");

					Image image =m_smallImages.GetImage(display.ImageLabel);
					CommandBarButton button = new CommandBarButton(image,display.Text, new EventHandler(OnClick));
					UpdateDisplay(display, button);
					button.Tag = control;

					control.ReferenceWidget = button;
					toolbar.Items.Add(button);
					button.IsEnabled = true;
				}
					//if this is a submenu
/*
 * I started playing with being able to have been used in here, but it. Complicated fast because
 * we would need to be referring over to the menubar adapter to fill in the menu items.
 * 				else if(item is ChoiceGroup)
				{
					if(((ChoiceGroup)item).IsSubmenu)
					{
						string label = item.Label.Replace("_", "&");
						CommandBarMenu submenu = new CommandBarMenu(label);// menu.Items.AddMenu(label);
						toolbar.Items.Add(submenu);
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
//					else if(((ChoiceGroup)item).IsInlineChoiceList)
//					{
//						((ChoiceGroup)item).PopulateNow();
//						foreach(ChoiceRelatedClass inlineItem in ((ChoiceGroup)item))
//						{
//							Debug.Assert(inlineItem is ChoiceBase, "It should not be possible for a in line choice list to contain anything other than simple items!");
//							menu.Items.Add(CreateMenuItem ((ChoiceBase)inlineItem));
//						}
//					}

			}
*/			}
		}

		protected void UpdateDisplay(UIItemDisplayProperties display, CommandBarButton button)
		{
			//can't change text after button is created
			button.IsEnabled = display.Enabled;
/*
 * I started on this so that property-related toolbar buttons could somehow looked depressed when
 * the property that they set equalled the value they set, i.e. in the "checked" state.
 * I gave up because
 * 1) there's no way to tell the CommandBar manager to draw it differently others and changing the icon
 * 2) it would be a pain to define or create different icons, though it would be possible
 * 3) when you change the icon, the CommandBar manager does not bother to repaint it
 * 4) so the way I found to repay was to actually tell the CommandBar manager to refresh
 *		the entire view, which caused a visible flicker.
 * 5) so I am giving up for now.
 *
 * 	if(display.Checked)
			{
				button.Image =m_smallImages.GetImage("Pie");
				//m_commandBarManager. Style=this.Style;//goose it
			}
			else
				button.Image  =m_smallImages.GetImage(display.ImageLabel);

			m_commandBarManager.Refresh();
*/		}

		/// <summary>
		/// we use the idle call here to update the enabled/disabled state of the buttons
		/// </summary>
		public void OnIdle()
		{
			if(m_commandBarManager== null || m_commandBarManager.CommandBars==null)
				return;//not really ready for an onIdle yet.

			foreach(CommandBar bar in m_commandBarManager.CommandBars)
			{
				foreach(CommandBarItem button in bar.Items)
				{
					if(!(button is CommandBarButton))
						continue;
					ChoiceBase control =(ChoiceBase) button.Tag;
					UpdateDisplay(control.GetDisplayProperties(), (CommandBarButton)button);
				}
			}
		}

	}
}
