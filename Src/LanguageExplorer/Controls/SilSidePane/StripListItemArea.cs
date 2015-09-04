// SilSidePane, Copyright 2010 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System.Drawing;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.SilSidePane
{
	/// <summary>
	/// Item area in a sidepane which uses a list of small icons next to text labels,
	/// implemented using ToolStrip.
	/// UNFINISHED. Needs to inherit from ToolStrip, implement IItemArea, and use a custom renderer.
	/// </summary>
	internal class StripListItemArea : OutlookButtonPanelItemArea
	{
		/// <summary></summary>
		internal StripListItemArea()
		{
			LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
			Dock = DockStyle.Fill;
			TextDirection = ToolStripTextDirection.Horizontal;
		}

		/// <summary></summary>
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