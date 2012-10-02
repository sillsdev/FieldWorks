using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Common.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class ColorHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a lightened up version of the system's hightlight color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Color LightHighlight
		{
			get { return CalculateColor(SystemColors.Window, SystemColors.Highlight, 150); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a very lightened up version of the system's hightlight color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Color LightLightHighlight
		{
			get { return CalculateColor(SystemColors.Window, SystemColors.Highlight, 200); }
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates a color by applying the specified alpha value to the specified front
		/// color, assuming the color behind the front color is the specified back color. The
		/// returned color has the alpha channel set to completely opaque, but whose alpha
		/// channel value appears to be the one specified.
		/// </summary>
		///-------------------------------------------------------------------------------------
		public static Color CalculateColor(Color front, Color back, int alpha)
		{
			// Use alpha blending to brigthen the colors but don't use it
			// directly. Instead derive an opaque color that we can use.
			// -- if we use a color with alpha blending directly we won't be able
			// to paint over whatever color was in the background and there
			// would be shadows of that color showing through
			Color frontColor = Color.FromArgb(255, front);
			Color backColor = Color.FromArgb(255, back);

			float frontRed = frontColor.R;
			float frontGreen = frontColor.G;
			float frontBlue = frontColor.B;
			float backRed = backColor.R;
			float backGreen = backColor.G;
			float backBlue = backColor.B;

			float fRed = frontRed * alpha / 255 + backRed * ((float)(255 - alpha) / 255);
			byte newRed = (byte)fRed;
			float fGreen = frontGreen * alpha / 255 + backGreen * ((float)(255 - alpha) / 255);
			byte newGreen = (byte)fGreen;
			float fBlue = frontBlue * alpha / 255 + backBlue * ((float)(255 - alpha) / 255);
			byte newBlue = (byte)fBlue;

			return Color.FromArgb(255, newRed, newGreen, newBlue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Takes and image and converts it to a disabled-looking image.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image MakeDisabledImage(Image image, Color clrBase)
		{
			Bitmap bmp = new Bitmap(image);
			Color notSoDark = Color.FromArgb(210, SystemColors.ControlDark);

			for (int x = 0; x < bmp.Width; x++)
			{
				for (int y = 0; y < bmp.Height; y++)
				{
					Color clr = bmp.GetPixel(x, y);

					if (clr != clrBase && clr.ToArgb() != 0)
					{
						float brightness = clr.GetBrightness();
						bmp.SetPixel(x, y, (brightness > 0.7 ? clrBase : notSoDark));
					}
				}
			}

			return (Image)bmp;
		}
	}
}
