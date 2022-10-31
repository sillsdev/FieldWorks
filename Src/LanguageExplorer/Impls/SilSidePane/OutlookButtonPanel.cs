// Copyright (C) 2008-2022 SIL International.
// Copyright (C) 2007 Star Vega.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Impls.SilSidePane
{
	/// <summary>
	/// Area to hold items. An OutlookButtonPanel holds items in a Tab.
	/// </summary>
	internal class OutlookButtonPanel : ToolStrip
	{
		private Size m_buttonSize;
		private IContainer components = new Container();

		/// <summary />
		internal OutlookButtonPanel()
		{
			AutoSize = false;
			GripStyle = ToolStripGripStyle.Hidden;
			LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
			Padding = new Padding(0, 15, 0, 5);
			ImageScalingSize = new Size(32, 32);
			Renderer = new OutlookBarPanelRenderer();
			m_buttonSize = new Size(100, 47 + Font.Height * 2);
			OverflowButton.DropDownOpening += OverflowButton_DropDownOpening;
		}

		/// <summary />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "******* Missing Dispose() call for " + GetType() + ". *******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary />
		private void OverflowButton_DropDownOpening(object sender, EventArgs e)
		{
			OverflowButton.DropDown = null;
			var cmnu = components.ContextMenuStrip("contextMenu");
			foreach (ToolStripButton button in Items)
			{
				if (button.IsOnOverflow)
				{
					var mnu = new ToolStripMenuItem(button.Text)
					{
						Checked = button.Checked,
						Tag = button
					};
					mnu.Click += HandleOverflowMenuClick;
					cmnu.Items.Add(mnu);
				}
			}
			var pt = OverflowButton.Bounds.Location;
			pt.X += OverflowButton.Width;
			pt = PointToScreen(pt);
			cmnu.Show(pt);
		}

		/// <summary />
		private static void HandleOverflowMenuClick(object sender, EventArgs e)
		{
			((sender as ToolStripMenuItem)?.Tag as ToolStripButton)?.PerformClick();
		}

		/// <summary>
		/// Gets or sets a value indicating how much margin to include above and below each
		/// sub button.
		/// </summary>
		internal int SubButtonVerticalMargin { get; set; } = 3;

		/// <summary />
		protected override void OnItemAdded(ToolStripItemEventArgs e)
		{
			if (e.Item is ToolStripButton button)
			{
				button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
				button.ImageAlign = ContentAlignment.MiddleCenter;
				button.TextAlign = ContentAlignment.BottomCenter;
				button.TextImageRelation = TextImageRelation.ImageAboveText;
				button.ImageScaling = ToolStripItemImageScaling.None;
				button.AutoSize = false;
				button.Size = m_buttonSize;
				var margin = button.Margin;
				button.Margin = new Padding(margin.Left, margin.Top, margin.Right, SubButtonVerticalMargin);
			}
		}

		/// <summary />
		protected override void OnItemClicked(ToolStripItemClickedEventArgs e)
		{
			base.OnItemClicked(e);
			var clickedButton = (ToolStripButton)e.ClickedItem;
			foreach (ToolStripButton button in Items)
			{
				if (clickedButton != button)
				{
					button.Checked = false;
				}
			}
			if (!clickedButton.Checked)
			{
				clickedButton.Checked = true;
			}
		}
	}
}
