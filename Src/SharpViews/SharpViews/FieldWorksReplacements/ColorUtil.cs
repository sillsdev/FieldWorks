using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SIL.Utils
{
	public static class ColorUtil
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///  Convert a .NET color to the BGR value understood by Views code.
		/// </summary>
		/// <param name="c">.NET Color value</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static uint ConvertColorToBGR(System.Drawing.Color c)
		{
			if (c.A == 0)
				return 0xC0000000;
			return (uint)ColorTranslator.ToWin32(c);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a BGR color (as used in FW props, for example) to a .Net Color object.
		/// </summary>
		/// <param name="bgrColor"></param>
		/// <returns>An instance of the <see>Color</see>class representing the given BGR value
		/// </returns>
		/// ------------------------------------------------------------------------------------
		static public Color ConvertBGRtoColor(uint bgrColor)
		{
			if (bgrColor == 0xC0000000)
				return Color.FromKnownColor(KnownColor.Transparent);
			return ColorTranslator.FromWin32((int)bgrColor);
		}
	}
}
