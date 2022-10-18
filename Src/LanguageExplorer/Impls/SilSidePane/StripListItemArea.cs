// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System.Drawing;
using System.Windows.Forms;

namespace LanguageExplorer.Impls.SilSidePane
{
	/// <summary>
	/// Item area in a sidepane which uses a list of small icons next to text labels,
	/// implemented using ToolStrip.
	/// </summary>
	internal sealed class StripListItemArea : OutlookButtonPanelItemArea
	{
		/// <summary />
		internal StripListItemArea()
		{
			LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
			Dock = DockStyle.Fill;
			TextDirection = ToolStripTextDirection.Horizontal;
		}

		/// <summary />
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