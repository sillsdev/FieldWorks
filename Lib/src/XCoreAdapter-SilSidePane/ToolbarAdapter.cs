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
using System.Collections.Generic;

#if USE_DOTNETBAR
using DevComponents.DotNetBar;
using DevComponents.Editors;
#endif

using SIL.Utils; // for ImageCollection

namespace XCore
{
	// TODO-Linux: Move this class into own file.
	/// <summary>
	/// Controls the position of a single MenuStrip and 1 or more ToolStrips
	/// </summary>
	public class ToolStripManager : ToolStripContainer
	{
		// flag to store if layout need updating.
		protected bool m_needLayout = true;

		// Store a collection of ToolStrips passed in by AddToolStrip
		protected List<ToolStrip> m_toolStripList = new List<ToolStrip>();

		// Store the single MenuStrip
		protected MenuStrip m_MenuStrip;

		public ToolStripManager()
		{
			Dock = DockStyle.Top;

			// default to a resonable height to reduce window moving.
			this.Height = 44;
		}

		/// <summary>
		/// Adds the one and only MenuStrip
		/// Throws ApplicationException if this is called multiple times.
		/// </summary>
		public void AddMenuStrip(MenuStrip menuStrip)
		{
			if (m_MenuStrip != null)
				throw new ApplicationException("Can only add a MenuStrip once");

			TopToolStripPanel.Controls.Add(menuStrip);
		}

		/// <summary>
		/// Add ToolStrips - This can be callled Multiple times
		/// </summary>
		public void AddToolStrip(ToolStrip toolStrip)
		{
			m_needLayout = true;
			m_toolStripList.Insert(0, toolStrip);
			ContentPanel.Controls.Add(toolStrip);

			toolStrip.ClientSizeChanged += HandleToolStripClientSizeChanged;
		}


		/// <summary>
		/// layout toolstrips by size
		/// Assumes all toolstrips are same height
		/// </summary>
		public void LayoutToolStrips()
		{
			m_needLayout = false;

			this.SuspendLayout();
			Point currentPosition = new Point(0,0);
			int toolStripHeight = 0;
			foreach(ToolStrip t in m_toolStripList)
			{
				t.SuspendLayout();
				t.Location = currentPosition;

				// if toolstrip position excedes width and it is not the inital toolbar on row
				if (currentPosition.X + t.Size.Width > ContentPanel.Size.Width && currentPosition.X > 0)
				{
					// Move ToolStrip to next row.
					currentPosition.Y += t.Height;
					currentPosition.X = 0;
					t.Location = currentPosition;
					currentPosition.X += t.Size.Width;
				}
				else
				{
					currentPosition.X += t.Size.Width;
				}


				toolStripHeight = Math.Max(t.Height, toolStripHeight);
				t.ResumeLayout();
			}

			this.Height = this.TopToolStripPanel.Height + currentPosition.Y + toolStripHeight;
			this.ResumeLayout();
		}

		/// <summary>
		/// Calls LayoutToolStrips if layout is needed
		/// </summary>
		public void LayoutToolStripsIfNeeded()
		{
			if (m_needLayout)
			{
				LayoutToolStrips();
			}
		}

		/// <summary>
		/// Mark that the layout needs to be reflowed
		/// </summary>
		public void InvalidateLayout()
		{
			m_needLayout = true;
		}

		/// <summary>
		/// If a ToolStip in m_toolStripList is modified invalidate Layout
		/// </summary>
		protected void HandleToolStripClientSizeChanged (object sender, EventArgs e)
		{
			InvalidateLayout();
		}
	}

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
#if USE_DOTNETBAR
			foreach (Bar bar in this.Manager.Bars)
			{
				string prefix ="Bars_"+bar.Name+ "_";
				m_mediator.PropertyTable.SetProperty(prefix+ "dockline", bar.DockLine, false);
				m_mediator.PropertyTable.SetProperty(prefix+ "dockoffset", bar.DockOffset, false);
			}
#endif
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
#if USE_DOTNETBAR
				foreach (Bar bar in this.Manager.Bars)
				{
					string prefix ="Bars_"+bar.Name+ "_";
					object orange =m_mediator.PropertyTable.GetValue(prefix+ "dockline",1);
					bar.DockLine = ((int) orange);
					orange =m_mediator.PropertyTable.GetValue(prefix+ "dockoffset");
					if (orange != null)
						bar.DockOffset = ((int) orange);
				}
#endif
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
#if USE_DOTNETBAR
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
#else
			InitializeToolStrips();
#endif
			DepersistLayout();
		}

		/// <summary>
		/// Helper method Creates all the ToolStrips based upon m_choiceGroupCollection
		/// And adds them to m_toolStripManager
		/// </summary>
		protected void InitializeToolStrips()
		{
			if (m_window == null)
				return;

			foreach(ChoiceGroup choice in m_choiceGroupCollection)
			{
				ToolStrip toolStrip = new ToolStrip()
					{
						Height = 15, // TODO-Linux: this should not be hard coded. Set the height when toolstipitem is added.
						Tag = choice,
						Dock = DockStyle.None
					};

				choice.ReferenceWidget = toolStrip;
				Manager.AddToolStrip(toolStrip);
			}

			m_window.Resize += delegate(object sender, EventArgs e)
			{
				Manager.InvalidateLayout();
			};
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
#if USE_DOTNETBAR
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
#endif
		}

		/// <summary>
		/// populate a combo box on the toolbar
		/// </summary>
		/// <param name="group">The group that is the basis for this combo box.</param>
		private void FillCombo(ChoiceGroup group)
		{
			UIItemDisplayProperties groupDisplay = group.GetDisplayProperties();

#if USE_DOTNETBAR
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
#endif
		}

#if USE_DOTNETBAR
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
#endif

		protected virtual void OnComboClick(object something, System.EventArgs args)
		{
#if USE_DOTNETBAR
			ComboBoxItem combo = (ComboBoxItem)something;

			ChoiceGroup group = (ChoiceGroup)combo.Tag;
			ComboItem selectedItem = combo.SelectedItem as ComboItem;
			if(selectedItem == null)
				return;

			ChoiceBase choice = selectedItem.Tag as ChoiceBase;
			choice.OnClick(combo, null);
#endif
		}

		/// <summary>
		/// Create or update the toolbar or a combobox owned by the toolbar
		/// </summary>
		/// <param name="group">The group that is the basis for this toolbar.</param>
		public override void CreateUIForChoiceGroup(ChoiceGroup group)
		{
#if USE_DOTNETBAR
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
#else
		   // The following two if clauses may be testing the same thing

			ToolStrip toolbar = group.ReferenceWidget as ToolStrip;
			if(toolbar.Focused)
					return;

			//don't refresh the toolbar if the mouse is hovering over the toolbar. Doing that caused the tool tips to flash.
			if (toolbar.Bounds.Contains(m_window.PointToClient(System.Windows.Forms.Form.MousePosition))
							|| InCombo(toolbar))
			{
				   //UpdateToolbar(group);
							return;
			}

			//FillToolbar(group, toolbar);
			FillToolbar(group, group.ReferenceWidget as ToolStrip);
#endif
		}

#if USE_DOTNETBAR
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
#else
		/// <summary>
		/// The ToolStrip is out of date if it doesn't match the ChoiceGroup
		/// eg. are the number of visible items different?
		///
		/// A better way of doing this would be asking the choiceGroup if it has changed
		/// or deep coping storing the choicegroup and caching it in the UI adapter, then
		/// comparing the difference.
		/// </summary>
		/// <returns>
		/// Returns true if the ToolStrip is out of date.
		/// </returns>
		private bool DoesToolStripNeedRegenerating(ChoiceGroup choiceGroup, ToolStrip toolStrip)
		{
			if (toolStrip == null)
				return true;

			int counter = 0;
			foreach(ChoiceRelatedClass item in choiceGroup)
			{
				UIItemDisplayProperties displayProperties = null;
				if (item is SeparatorChoice == false)
					displayProperties = item.GetDisplayProperties();

				// toolStrip has less items then visiable items in the ChoiceGroup so regenerate
				if (toolStrip.Items.Count <= counter)
					return true;

				// skip over seperators
				// Note: sepeartors contained in the ChoiceGroup are only always added to the ToolStrip
				// if they are followed by a visiable non sepeartor item.
				// So a missing sepeartor doesn't necessary mean the toolbar needs regenerating.
				if (displayProperties == null)
				{
					if (toolStrip.Items[counter] is ToolStripSeparator)
					{
						counter++;
					}
					continue;
				}

				// InVisible items are not added to the toolStrip
				//if (displayProperties.Visible == false)
				//	continue;

				var toolStripItem = toolStrip.Items[counter];

				if (displayProperties.Enabled != toolStripItem.Enabled)
					return true;


				// if the text doesn't match then needs regenerating
				// TODO duplicating Text.Replace here and in (CreateButtonItem)BarAdapterBase.cs is a bit horrible.
				if (displayProperties.Text.Replace("_", "&") != toolStripItem.Text)
				   return true;

				counter++;
			}

			// nothing has been detected that needs changing.
			return false;
		}

		/// <summary>
		/// returns true if it made any changes.
		/// </summary>
		private bool FillToolbar(ChoiceGroup choiceGroup, ToolStrip toolStrip)
		{
			bool wantsSeparatorBefore = false;

			choiceGroup.PopulateNow();

			if (!DoesToolStripNeedRegenerating(choiceGroup, toolStrip))
				return false;

			// Don't let the GC run dispose.
			for(int i = toolStrip.Items.Count - 1; i >= 0; --i)
			{
				toolStrip.Items[i].Dispose();
			}

			toolStrip.Items.Clear();
			foreach(ChoiceRelatedClass item in choiceGroup)
			{
				if(item is SeparatorChoice)
				{
					wantsSeparatorBefore = true;
				}
				else if (item is ChoiceBase)
				{
					UIItemDisplayProperties displayProperties = null;
					if (item is SeparatorChoice == false)
						displayProperties = item.GetDisplayProperties();

					ToolStripItem toolStripItem = CreateButtonItem(item as ChoiceBase, false);

					//toolStripItem.ToolTipText = item.Label; // TODO-Linux: add shortcut accessolrator here. // choiceBase.Shortcut. //maybe this should be done by CreateButtonItem?

					toolStripItem.DisplayStyle = ToolStripItemDisplayStyle.Image;

					if (wantsSeparatorBefore && displayProperties != null && displayProperties.Visible)
					{
						var separator = new ToolStripSeparator();
						toolStrip.Items.Add(separator);
					}
					wantsSeparatorBefore = false;
					toolStrip.Items.Add(toolStripItem);
				}
				else if (item is ChoiceGroup)
				{

					ToolStripComboBox toolStripItem = CreateComboBox(item as ChoiceGroup, true);

					toolStrip.Items.Add(toolStripItem);
				}
				else
				{
					//debugging
					continue;
				}

			}

			toolStrip.PerformLayout();

			return true;
		}
#endif

#if USE_DOTNETBAR
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
#endif

#if USE_DOTNETBAR
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
#endif
		/// <summary>
		/// Tells whether the user is manipulating some combo of the toolbar, in which case the toolbar should not be rebuilt now
		/// </summary>
		/// <param name="toolbar"></param>
		/// <returns></returns>
		private bool InCombo(ToolStrip toolbar)
		{
			foreach (ToolStripItem x in toolbar.Items)
			{
				var combo = x as ToolStripComboBox;
				if (combo == null)
					continue;
				if (combo.Focused)
					return true;
			}
			return false;
		}

#if USE_DOTNETBAR
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
#endif


		#endregion IUIAdapter implementation

		/// <summary>
		/// Redraw all of the toolbars on idle in case of some items have been enabled/disabled/made invisible, etc.
		/// </summary>
		public override void OnIdle()
		{
			if(m_choiceGroupCollection != null)
			{
				Manager.SuspendLayout();
				foreach(ChoiceGroup group in m_choiceGroupCollection)
				{
					group.OnDisplay(null, null);
				}
				Manager.LayoutToolStripsIfNeeded();
				Manager.ResumeLayout();
			}
		}
	}
}
