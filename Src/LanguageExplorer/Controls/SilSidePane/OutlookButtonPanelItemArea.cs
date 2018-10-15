// SilSidePane, Copyright 2010-2018 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.PlatformUtilities;

namespace LanguageExplorer.Controls.SilSidePane
{
	/// <summary>
	/// Item area in a side pane which uses large buttons with large icons.
	/// </summary>
	internal class OutlookButtonPanelItemArea : OutlookButtonPanel, IItemArea
	{
		#region IItemArea Members
		/// <summary />
		public new event ItemAreaItemClickedEventHandler ItemClicked;

		/// <summary />
		public virtual void Add(Item item)
		{
			var widget = new ToolStripButton
			{
				Text = item.Text,
				Name = item.Name,
				Image = item.Icon,
				ImageScaling = ToolStripItemImageScaling.None,
				ImageTransparentColor = System.Drawing.Color.Magenta,
				Tag = item,
			};
			widget.Click += HandleWidgetClick;
			item.UnderlyingWidget = widget;

			base.Items.Add(widget);
			Items.Add(item);
		}

		/// <summary />
		public new List<Item> Items { get; } = new List<Item>();

		/// <summary />
		public Item CurrentItem { get; private set; } = null;

		/// <summary />
		public void SelectItem(Item item)
		{
			var widget = (ToolStripButton)item.UnderlyingWidget;
			widget.PerformClick();
			if (Platform.IsMono)
			{
				// We need to explicitly uncheck the previous selection.  See FWNX-661.
				foreach (var button in widget.Owner.Items.OfType<ToolStripButton>())
				{
					if (button.Checked && button != widget)
					{
						button.Checked = false;
					}
				}

				widget.Checked = true;
			}
		}

		/// <summary />
		public Control AsControl()
		{
			return this;
		}
		#endregion IItemArea Members

		/// <summary>
		/// Handles when a widget (ToolStripButton) in this item area is clicked.
		/// </summary>
		private void HandleWidgetClick(object sender, EventArgs e)
		{
			var widget = sender as ToolStripButton;
			if (widget == null)
			{
				return;
			}

			var item = widget.Tag as Item;

			CurrentItem = item;
			ItemClicked(item);
		}
	}
}
