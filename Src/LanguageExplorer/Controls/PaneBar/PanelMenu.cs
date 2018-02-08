// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;

namespace LanguageExplorer.Controls.PaneBar
{
	/// <summary>
	/// PaneBar control that displays a context menu.
	/// </summary>
	internal class PanelMenu : PanelExtension
	{
		private SliceContextMenuFactory _sliceContextMenuFactory;
		private string _panelMenuId;
		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> _contextMenuAndItems;

		public PanelMenu(SliceContextMenuFactory sliceContextMenuFactory, string panelMenuId)
		{
			Dock = DockStyle.Right;
			Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
			Location = new Point(576, 2);
			Name = "panelMenu";
			Anchor = AnchorStyles.None;
			Size = new Size(16, 16);

			Click += PanelMenu_Click;
			TabIndex = 0;
			_sliceContextMenuFactory = sliceContextMenuFactory;
			_panelMenuId = panelMenuId;
		}

		internal SliceContextMenuFactory ContextMenuFactory { get; set; }

		private void PanelMenu_Click(object sender, EventArgs e)
		{
			if (_contextMenuAndItems != null)
			{
				// Get rid of the old ones, since some tools (e.g., ReversalBulkEditReversalEntriesTool) need to rebuild the menu times each time it is shown.
				_sliceContextMenuFactory.DisposeContextMenu(_contextMenuAndItems);
			}
			_contextMenuAndItems = _sliceContextMenuFactory.GetPanelMenu(_panelMenuId);
			ContextMenuStrip = _contextMenuAndItems.Item1;
			ContextMenuStrip.Show(this, new Point(Location.X, Location.Y + Height));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Click -= PanelMenu_Click;
				if (_contextMenuAndItems != null)
				{
					_sliceContextMenuFactory.DisposeContextMenu(_contextMenuAndItems);
				}
			}

			base.Dispose(disposing);

			_sliceContextMenuFactory = null;
			_contextMenuAndItems = null;
		}
	}
}