using System;
using System.Drawing;
using SidebarLibrary.General;

namespace SidebarLibrary.Menus
{
	/// <summary>
	/// Summary description for ColorGroup.
	/// Helper class to get VSNet IDE colors
	/// </summary>
	public class ColorGroup
	{
		public ColorGroup(Color bgcolor, Color stripecolor, Color selectioncolor,
					Color bordercolor, Color darkselectioncolor)
		{
			this.bgcolor = bgcolor;
			this.stripecolor = stripecolor;
			this.selectioncolor = selectioncolor;
			this.bordercolor = bordercolor;
			this.darkselectioncolor = darkselectioncolor;
		}

		private Color bgcolor;
		private Color stripecolor;
		private Color selectioncolor;
		private Color bordercolor;
		private Color darkselectioncolor;

		public Color bgColor
		{
			get
			{
				return bgcolor;
			}
		}

		public Color stripeColor
		{
			get
			{
				return stripecolor;
			}
		}

		public Color selectionColor
		{
			get
			{
				return selectioncolor;
			}
		}

		public Color borderColor
		{
			get
			{
				return bordercolor;
			}
		}

		public Color darkSelectionColor
		{
			get
			{
				return darkselectioncolor;
			}
		}

		public static ColorGroup GetColorGroup()
		{
			ColorGroup colorGroup = null;
			Color backgroundColor = ColorUtil.VSNetBackgroundColor;
			Color selectionColor = ColorUtil.VSNetSelectionColor;
			Color stripeColor = ColorUtil.VSNetControlColor;
			colorGroup = new ColorGroup(backgroundColor, stripeColor, selectionColor,
				ColorUtil.VSNetBorderColor, ColorUtil.VSNetPressedColor);

			return colorGroup;
		}
	}
}
