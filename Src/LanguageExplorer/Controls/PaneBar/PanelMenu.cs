// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		internal PanelMenuContextMenuFactory DataTreeMainPanelContextMenuFactory { private get; set; }
		private string _panelMenuId;
		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> _panelMenuContextMenuAndItems;

		public PanelMenu(string panelMenuId)
		{
			Dock = DockStyle.Right;
			Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
			Location = new Point(576, 2);
			Name = "panelMenu";
			Anchor = AnchorStyles.None;
			Size = new Size(16, 16);
			Click += PanelMenu_Click;
			TabIndex = 0;
			_panelMenuId = panelMenuId;
		}

		public PanelMenu(PanelMenuContextMenuFactory dataTreeMainPanelContextMenuFactory, string panelMenuId)
			: this(panelMenuId)
		{
			DataTreeMainPanelContextMenuFactory = dataTreeMainPanelContextMenuFactory;
		}

		private void PanelMenu_Click(object sender, EventArgs e)
		{
			if (_panelMenuContextMenuAndItems != null)
			{
				_panelMenuContextMenuAndItems = null;
				ContextMenuStrip?.Dispose();
				ContextMenuStrip = null;
			}
			_panelMenuContextMenuAndItems = DataTreeMainPanelContextMenuFactory.GetPanelMenu(_panelMenuId);
			if (_panelMenuContextMenuAndItems != null)
			{
				ContextMenuStrip = _panelMenuContextMenuAndItems.Item1;
				ContextMenuStrip.Show(this, new Point(Location.X, Location.Y + Height));
			}
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				Click -= PanelMenu_Click;
				ContextMenuStrip?.Dispose();
				if (_panelMenuContextMenuAndItems != null)
				{
					DataTreeMainPanelContextMenuFactory.RemovePanelMenuContextMenu(_panelMenuId);
				}
			}
			DataTreeMainPanelContextMenuFactory = null;
			_panelMenuContextMenuAndItems = null;
			_panelMenuId = null;
			ContextMenuStrip = null;

			base.Dispose(disposing);
		}
	}
}