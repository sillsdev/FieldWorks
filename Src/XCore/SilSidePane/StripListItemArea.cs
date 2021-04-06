// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System.Drawing;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Item area in a sidepane which uses a list of small icons next to text labels,
	/// implemented using ToolStrip.
	/// UNFINISHED. Needs to inherit from ToolStrip, implement IItemArea, and use a custom renderer.
	/// </summary>
	internal class StripListItemArea : OutlookButtonPanelItemArea
	{
		public StripListItemArea()
		{
			LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
			Dock = DockStyle.Fill;
			TextDirection = ToolStripTextDirection.Horizontal;
		}

		public override void Add(Item item)
		{
			base.Add(item);

			var widget = item.UnderlyingWidget as ToolStripButton;
			widget.TextImageRelation = TextImageRelation.ImageBeforeText;
			widget.ImageScaling = ToolStripItemImageScaling.SizeToFit;
			widget.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
			widget.ImageAlign = ContentAlignment.MiddleCenter;
			widget.TextAlign = ContentAlignment.MiddleCenter;
			widget.AutoSize = true;
		}
	}
}