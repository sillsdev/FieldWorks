// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: ReBarAdapter.cs
// Authorship History: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Windows.Forms;  //for ImageList
using System.Collections.Generic;


// for ImageCollection

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
			Height = 44;
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

			Height = TopToolStripPanel.Height + currentPosition.Y + toolStripHeight;
			ResumeLayout();
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
	public class ReBarAdapter : BarAdapterBase, IUIAdapterForceRegenerate
	{
		protected ChoiceGroupCollection m_choiceGroupCollection;
		#region Constructor

		#endregion Constructor

		#region IUIAdapter implementation
		/// <summary>
		/// store the location/settings of various widgets so that we can restore them next time
		/// </summary>
		public override void PersistLayout()
		{
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
			InitializeToolStrips();
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
				toolStrip.AccessibilityObject.Name = choice.Id;



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
		/// <summary>
		/// Create or update the toolbar or a combobox owned by the toolbar
		/// </summary>
		/// <param name="group">
		///   The group that is the basis for this toolbar.
		/// </param>
		public override void CreateUIForChoiceGroup(ChoiceGroup group)
		{

		   // The following two if clauses may be testing the same thing

			ToolStrip toolbar = group.ReferenceWidget as ToolStrip;
			if(toolbar.Focused)
					return;

			//don't refresh the toolbar if the mouse is hovering over the toolbar. Doing that caused the tool tips to flash.
			if (toolbar.Bounds.Contains(m_window.PointToClient(Control.MousePosition))
							|| InCombo(toolbar))
			{
				   //UpdateToolbar(group);
							return;
			}

			//FillToolbar(group, toolbar);
			FillToolbar(group, group.ReferenceWidget as ToolStrip);

		}

		/// <summary>
		/// Used during Refresh, this indicates that the next idle call to regenerate the toolbar if anything
		/// has changed should behave unconditionally as if something HAS changed, and rebuild the toolbar.
		/// For example, the normal DoesToolStripNeedRegenerating does not notice that unselected items
		/// in the style combo list are different from the ones in the current menu.
		/// </summary>
		public void ForceFullRegenerate()
		{
			m_fullRengenerateRequired = true;
		}

		bool m_fullRengenerateRequired;

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
			if (m_fullRengenerateRequired)
			{
				m_fullRengenerateRequired = false;
				return true;
			}
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
				if (item is ChoiceGroup)
				{
					ChoiceGroup group = item as ChoiceGroup;
					group.PopulateNow();
					string groupPropValue = group.SinglePropertyValue;
					foreach (ChoiceRelatedClass candidate in group)
					{
						if (candidate is SeparatorChoice)
						{
							//TODO
						}
						else if (candidate is ChoiceBase)
						{
							if (groupPropValue == (candidate as ListPropertyChoice).Value)
							{
								if (candidate.ToString() != toolStripItem.Text)
									return true;
							}
						}
					}
					// This handles displaying a blank value in the toolbar Styles combobox when
					// the selection covers multiple Style settings.  Without it, the last valid
					// style setting is still shown when the selection is extended to include
					// another style.  See FWR-824.
					if (groupPropValue == displayProperties.Text &&
						displayProperties.Text != toolStripItem.Text)
					{
						return true;
					}
				}
				else
				{
					if (displayProperties.Text.Replace("_", "&") != toolStripItem.Text)
						return true;
				}

				counter++;
			}

			// nothing has been detected that needs changing.
			return false;
		}

		/// <summary>
		/// returns true if it made any changes.
		/// </summary>
		private void FillToolbar(ChoiceGroup choiceGroup, ToolStrip toolStrip)
		{
			bool wantsSeparatorBefore = false;

			choiceGroup.PopulateNow();

			if (!DoesToolStripNeedRegenerating(choiceGroup, toolStrip))
				return;

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
					UIItemDisplayProperties displayProperties = item.GetDisplayProperties();

					bool reallyVisible;
					ToolStripItem toolStripItem = CreateButtonItem(item as ChoiceBase, out reallyVisible);

					//toolStripItem.ToolTipText = item.Label; // TODO-Linux: add shortcut accessolrator here. // choiceBase.Shortcut. //maybe this should be done by CreateButtonItem?

					toolStripItem.DisplayStyle = ToolStripItemDisplayStyle.Image;

					if (wantsSeparatorBefore && displayProperties != null && displayProperties.Visible)
					{
						var separator = new ToolStripSeparator();
						separator.AccessibilityObject.Name = "separator";
						// separator.GetType().Name;
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

			return;
		}





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
