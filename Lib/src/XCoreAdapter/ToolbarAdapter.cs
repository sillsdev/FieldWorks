// --------------------------------------------------------------------------------------------
// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ReBarAdapter.cs
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

using DevComponents.DotNetBar;
using DevComponents.Editors;

using SIL.Utils; // for ImageCollection

namespace XCore
{
	/// <summary>
	/// Creates the toolbars for an XCore application.
	/// </summary>
	public class ReBarAdapter : BarAdapterBase
	{
		protected ChoiceGroupCollection m_choiceGroupCollection;
		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public ReBarAdapter()
		{
		}



		#endregion Constructor

		#region IUIAdapter implementation
		/// <summary>
		/// store the location/settings of various widgets so that we can restore them next time
		/// </summary>
		public override void PersistLayout()
		{
			foreach (Bar bar in this.Manager.Bars)
			{
				string prefix ="Bars_"+bar.Name+ "_";
				m_mediator.PropertyTable.SetProperty(prefix+ "dockline", bar.DockLine, false);
				m_mediator.PropertyTable.SetProperty(prefix+ "dockoffset", bar.DockOffset, false);
			}
		}

		/// <summary>
		/// get the location/settings of various widgets so that we can restore them
		/// </summary>
		protected void DepersistLayout()
		{
			//comes from the user settings file
			string previousVersion = (string)m_mediator.PropertyTable.GetValue("PreviousToolbarVersion", "notSet");

			//Comes from the configuration file in the properties section. Increment it there to force a reset
			// of user toobars.
			string currentVersion = (string)m_mediator.PropertyTable.GetValue("CurrentToolbarVersion", "notSet");
			m_mediator.PropertyTable.SetPropertyPersistence("CurrentToolbarVersion", false);

			if(previousVersion.Equals(currentVersion)) //else, leave as default
			{
				foreach (Bar bar in this.Manager.Bars)
				{
					string prefix ="Bars_"+bar.Name+ "_";
					object orange =m_mediator.PropertyTable.GetValue(prefix+ "dockline",1);
					bar.DockLine = ((int) orange);
					orange =m_mediator.PropertyTable.GetValue(prefix+ "dockoffset");
					if (orange != null)
						bar.DockOffset = ((int) orange);
				}
			}
			//remember now
			m_mediator.PropertyTable.SetProperty("PreviousToolbarVersion", currentVersion);
		}

		/// <summary>
		/// Overrides method to create the toolbars, but witout the items on them,
		/// which are handled elsewhere.
		/// </summary>
		/// <param name="groupCollection">Collection of toolbar definitions to create.</param>
		public override void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
			m_choiceGroupCollection= groupCollection;
			DotNetBarManager manager = (DotNetBarManager)m_mediator.PropertyTable.GetValue("DotNetBarManager");
//			manager.SaveLayout("testLayout.xml");

			// If the menubar has already been added, remove it,
			// and re-add it after adding the toolbars, or it won't be at the top.
			Bar menuBar = null;
			foreach (Bar bar in manager.Bars)
			{
				if (bar.MenuBar)
				{
					menuBar = bar;
					bar.Name="menubar";//there may be some better place to put this, but I couldn't quickly find where the menubar is created.
					manager.Bars.Remove(menuBar);
					break;
				}
			}




			// Create new Bars.
			foreach(ChoiceGroup group in groupCollection)
			{
				Bar toolbar = new Bar(group.Id);
				toolbar.Name = group.Id;
				toolbar.CanHide = true;
				if(	m_mediator.PropertyTable.GetBoolProperty("UseOffice2003Style", false))
				{
					Manager.Style = eDotNetBarStyle.Office2003;
					toolbar.ThemeAware = false;
					toolbar.Style = eDotNetBarStyle.Office2003;
					toolbar.ItemsContainer.Style = eDotNetBarStyle.Office2003;
					toolbar.GrabHandleStyle = eGrabHandleStyle.Office2003;
				}
				else
				{
					toolbar.Style = eDotNetBarStyle.OfficeXP;
					toolbar.ItemsContainer.Style = eDotNetBarStyle.OfficeXP;
					toolbar.GrabHandleStyle = eGrabHandleStyle.StripeFlat;
				}
				toolbar.WrapItemsDock = true;
				toolbar.WrapItemsFloat = false;
				toolbar.Tag = group;
				toolbar.CanCustomize = false;
				toolbar.CanUndock = false;
				group.ReferenceWidget = toolbar;
				group.OnDisplay(null, null); // Ensure the toolbar has its items added.
				manager.Bars.Add(toolbar);
				toolbar.DockSide = eDockSide.Top;
			}

			// Re-add the menu bar
			if (menuBar != null)
			{
				manager.Bars.Add(menuBar);;
				menuBar.DockSide = eDockSide.Top;
			}

			DepersistLayout();
		}


		/// <summary>
		/// do a limited update of the toolbar
		/// </summary>
		/// <remarks>when the user's cursor is hovering over a button and we do a redraw,
		/// this method is called. This allows us to update the enabled state of the button
		/// without getting flashing tool tips,
		/// which is what we get if we were to clear the toolbar and rebuild the buttons,
		/// as is normally done at idle time.
		/// </remarks>
		/// <param name="group"></param>
		protected void UpdateToolbar (ChoiceGroup group)
		{
			Bar toolbar = (Bar)group.ReferenceWidget;
			foreach(BaseItem item in toolbar.Items)
			{
				if( item.Tag == null)
					continue;
				ChoiceBase choice = item.Tag as ChoiceBase;
				if(choice != null)
				{
					UIItemDisplayProperties display = choice.GetDisplayProperties();

					// If what should be displayed has changed, refill the toolbar.
					if (item.Visible && !display.Visible)
					{
						FillToolbar(group, toolbar);
						return;
					}
					item.Enabled = display.Enabled;
				}
				//update combo box
				ChoiceGroup comboGroup = item.Tag as ChoiceGroup;
				if(comboGroup != null)
				{
					//UIItemDisplayProperties display = group.GetDisplayProperties();

					//item.Enabled = display.Enabled;
					comboGroup.PopulateNow(); // make sure list of items is up to date.
					FillCombo(comboGroup);//maybe too drastic
				}
			}
		}

		/// <summary>
		/// populate a combo box on the toolbar
		/// </summary>
		/// <param name="group">The group that is the basis for this combo box.</param>
		private void FillCombo(ChoiceGroup group)
		{
			UIItemDisplayProperties groupDisplay = group.GetDisplayProperties();

			ComboBoxItem combo = group.ReferenceWidget as ComboBoxItem;
			if(combo.Focused)
				return;//don't mess while we're in the combo

			// Disable if needed, but still show what's current, as for unicode fields where you can't change it, but you want to see what it is set to.
			combo.Enabled = groupDisplay.Enabled;

			ArrayList newItems = new ArrayList();
			bool fDifferent = false;
			ComboItem selectedItem = null;
			foreach (ChoiceRelatedClass item in group)
			{
				if (item is SeparatorChoice)
				{
					//TODO
				}
				else if (item is ChoiceBase)
				{
					UIItemDisplayProperties display = ((ChoiceBase)item).GetDisplayProperties();
					ComboItem ci = CreateComboItem((ChoiceBase)item, display);
					if (ci != null)//will be null if item is supposed to be invisible now
					{
						newItems.Add(ci);
						if (display.Checked)
						{
							selectedItem = ci; //nb: if there should be more than one, only the last one will be selected
						}
						if (combo.Items.Count < newItems.Count || ((combo.Items[newItems.Count - 1]) as ComboItem).Tag != item)
							fDifferent = true;
					}
				}
			}

			combo.AccessibleName = group.Label;
			if (fDifferent || selectedItem.Tag != (combo.SelectedItem as ComboItem).Tag)
			{
				combo.SuspendLayout = true;
				combo.Click -= new EventHandler(OnComboClick);	//don't generate clicks (which end up being onpropertychanged() calls)
				combo.Items.Clear();
				combo.Items.AddRange(newItems.ToArray());
				combo.SelectedItem = selectedItem;
				combo.Click += new EventHandler(OnComboClick);
				combo.SuspendLayout = false;
			}

			//Set the ComboWidth of the combo box so that is is wide enough to show
			//the text of all items in the list.
			int maxStringLength = 0;
			for (int i = 0; i < combo.Items.Count; i++)
			{
				if (combo.Items[i].ToString().Length > maxStringLength)
				{
					maxStringLength = combo.Items[i].ToString().Length;
				}
			}
			int factor = 6;
			if (maxStringLength > 0 && combo.ComboWidth < maxStringLength * factor)
				combo.ComboWidth = maxStringLength * factor;
			combo.Tooltip = combo.ToString();

		}

		protected ComboItem  CreateComboItem(ChoiceBase choice, UIItemDisplayProperties display)
		{

			string label = display.Text;
			if (label ==null)
				label = AdapterStrings.ErrorGeneratingLabel;

			if(!display.Visible)
				return null;

			label = label.Replace("_", "&");
			DevComponents.Editors.ComboItem item = new DevComponents.Editors.ComboItem();
			item.Text=label;

			Debug.Assert(item != null);
			item.Tag = choice;
			choice.ReferenceWidget = item;
			return item;
		}

		protected virtual void OnComboClick(object something, System.EventArgs args)
		{
			ComboBoxItem combo = (ComboBoxItem)something;

			ChoiceGroup group = (ChoiceGroup)combo.Tag;
			ComboItem selectedItem = combo.SelectedItem as ComboItem;
			if(selectedItem == null)
				return;

			ChoiceBase choice = selectedItem.Tag as ChoiceBase;
			choice.OnClick(combo, null);
		}

		/// <summary>
		/// Create or update the toolbar or a combobox owned by the toolbar
		/// </summary>
		/// <param name="group">The group that is the basis for this toolbar.</param>
		public override void CreateUIForChoiceGroup(ChoiceGroup group)
		{
			if(group.ReferenceWidget is ComboBoxItem)
			{
				FillCombo(group);
				return;
			}

			Bar toolbar = group.ReferenceWidget as Bar;
//			if(toolbar.Focused)
//				return;

			//don't refresh the toolbar if the mouse is hovering over the toolbar. Doing that caused the tool tips to flash.
			if (toolbar.Bounds.Contains(m_window.PointToClient(System.Windows.Forms.Form.MousePosition))
				|| InCombo(toolbar) || ToolbarLocked(toolbar))
			{
				UpdateToolbar(group);
				return;
			}

			FillToolbar(group, toolbar);
		}

		/// <summary>
		/// build (or rebuild) the toolbar
		/// </summary>
		/// <param name="group"></param>
		/// <param name="toolbar"></param>
		private void FillToolbar(ChoiceGroup group, Bar toolbar)
		{
			bool wantsSeparatorBefore = false;
			string groupName =group.Label;

			ClearOutToolbar(toolbar);
			foreach(ChoiceRelatedClass item  in group)
			{
				string itemName =item.Label;
				if(item is SeparatorChoice)
					wantsSeparatorBefore = true;
				else if(item is ChoiceBase)
				{
					ChoiceBase choice = (ChoiceBase) item;
					UIItemDisplayProperties display = choice.GetDisplayProperties();

					if (!display.Visible)
						continue;
					ButtonItem btn = CreateButtonItem(choice, wantsSeparatorBefore);
					btn.Enabled = display.Enabled;
					btn.Category = group.Label;
					toolbar.Items.Add(btn);
					// DIDN'T WORK
					//					if(!Manager.Items.Contains(btn.Name))
					//							Manager.Items.Add(btn.Copy());
					wantsSeparatorBefore = false;
				}
				else if(item is ChoiceGroup) // Submenu
				{
					ChoiceGroup choiceGroup = (ChoiceGroup)item;

					//nb: the DNB class name, "ComboBoxItem" is very misleading. This is the box itself,
					//not an item in the box.
					ComboBoxItem combo = new ComboBoxItem(/*name*/item.Id,/*text*/item.Label);
					toolbar.Items.Add(combo);
					combo.Tag = item;
					int width = 100;
					// Make this configurable from the xml
					XmlAttribute att = item.ConfigurationNode.Attributes["width"];
					if (att != null)
						width = Int32.Parse(att.Value);
					combo.ComboWidth = width;
					//combo.Stretch = true;//doesn't help

					item.ReferenceWidget = combo;
					//submenu.IsEnabled= true;
					//the drop down the event seems to be ignored by the submenusof this package.
					//submenu.DropDown += new System.EventHandler(((ChoiceGroup)item).OnDisplay);
					//therefore, we need to populate the submenu right now and not wait
					choiceGroup.OnDisplay(m_window, null);

					//hide if no sub items were added

					//submenu.Visible = submenu.SubItems.Count>0;
				}
				else
				{
					// TODO: Add support for other types of toolbar objects.
				}
			}
			toolbar.RecalcLayout();
			toolbar.RecalcSize();
		}

		/// <summary>
		///	hack to stop ws toolbar flickering terribly (still does some)
		/// </summary>
		/// <param name="toolbar"></param>
		/// <returns></returns>
		private bool ToolbarLocked(Bar toolbar)
		{
			foreach(DevComponents.DotNetBar.BaseItem x in toolbar.Items)
			{
				ComboBoxItem combo = x as ComboBoxItem;
				if(combo==null)
					continue;
				//has combo box
				return true;
			}
			return false;
		}

		/// <summary>
		/// Tells whether the user is manipulating some combo of the toolbar, in which case the toolbar should not be rebuilt now
		/// </summary>
		/// <param name="toolbar"></param>
		/// <returns></returns>
		private bool InCombo(Bar toolbar)
		{
			foreach(DevComponents.DotNetBar.BaseItem x in toolbar.Items)
			{
				ComboBoxItem combo = x as ComboBoxItem;
				if(combo==null)
					continue;
				if(combo.Focused || combo.Expanded)
					return true;
			}
			return false;
		}

		private void ClearOutToolbar(Bar toolbar)
		{
			foreach(DevComponents.DotNetBar.BaseItem x in toolbar.Items)
			{
				if(x is ComboBoxItem )
				{
					//toolbar.Items.Remove(x);
					x.Dispose();
				}
			}
			toolbar.Items.Clear();
		}


		#endregion IUIAdapter implementation

		/// <summary>
		/// Redraw all of the toolbars on idle in case of some items have been enabled/disabled/made invisible, etc.
		/// </summary>
		public override void OnIdle()
		{
			if(m_choiceGroupCollection!= null)
			{
				foreach(ChoiceGroup group in m_choiceGroupCollection)
				{
					group.OnDisplay(null, null);
				}
			}
		}
	}
}
