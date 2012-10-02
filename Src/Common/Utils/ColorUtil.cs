// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2006' company='SIL International'>
//    Copyright (c) 2006, SIL International. All Rights Reserved.
// </copyright>
//
// File: ColorUtil.cs
// Responsibility: TeTeam
// Last reviewed:
//
// Implementation of ColorUtil
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Resources;

namespace SIL.FieldWorks.Common.Utils
{
	///-----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ColorUtil.
	/// </summary>
	///-----------------------------------------------------------------------------------------
	public class ColorUtil
	{
		#region private ColorInfo class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Holds Color/Name pairs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class ColorInfo
		{
			/// <summary>Color</summary>
			public Color m_color;
			/// <summary>Color name</summary>
			public string m_name;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:ColorInfo"/> class.
			/// </summary>
			/// <param name="color">The color.</param>
			/// --------------------------------------------------------------------------------
			public ColorInfo(Color color): this(color, null)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:ColorInfo"/> class.
			/// </summary>
			/// <param name="color">The color.</param>
			/// <param name="name">The name.</param>
			/// --------------------------------------------------------------------------------
			public ColorInfo(Color color, string name)
			{
				m_color = color;
				m_name = name;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Determines whether the specified <see cref="T:System.Object"></see> is equal
			/// to the current <see cref="T:System.Object"></see>.
			/// </summary>
			/// <param name="obj">The <see cref="T:System.Object"></see> to compare with the
			/// current <see cref="T:System.Object"></see>.</param>
			/// <returns>
			/// true if the specified <see cref="T:System.Object"></see> is equal to the
			/// current <see cref="T:System.Object"></see>; otherwise, false.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public override bool Equals(object obj)
			{
				if (obj is Color)
				{
					Color objColor = (Color)obj;
					return objColor.ToArgb() == m_color.ToArgb();
				}
				else if (!(obj is ColorInfo))
					return false;

				ColorInfo objColorInfo = (ColorInfo)obj;
				return objColorInfo.m_color.ToArgb() == m_color.ToArgb();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
			/// </summary>
			/// <returns>
			/// A hash code for the current <see cref="T:System.Object"></see>.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public override int GetHashCode()
			{
				return m_color.GetHashCode();
			}
		}
		#endregion

		#region Member variables
		private static Color m_backgroundColor = Color.Empty;
		private static Color m_selectionColor = Color.Empty;
		private static Color m_controlColor = Color.Empty;
		private static Color m_pressedColor = Color.Empty;
		private static Color m_checkedColor = Color.Empty;
		private static Color m_borderColor = Color.Empty;
		private static bool m_useCustomColor = false;
		private static List<ColorInfo> s_colorNameTable = new List<ColorInfo>();
		#endregion

		/// <summary>Number of colors</summary>
		public const int kNumberOfColors = 40;

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// No need to construct this object
		/// </summary>
		///-------------------------------------------------------------------------------------
		private ColorUtil()
		{
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		///-------------------------------------------------------------------------------------
		static public bool UsingCustomColor
		{
			get {return m_useCustomColor;}
		}

		#region Visual Studio .NET colors calculation helpers
		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		///-------------------------------------------------------------------------------------
		static public Color BackgroundColor
		{
			get
			{
				if (m_useCustomColor && m_backgroundColor != Color.Empty)
					return m_backgroundColor;
				else
					return CalculateColor(SystemColors.Window, SystemColors.Control, 220);
			}
			set
			{
				// Flag that we are going to use custom colors instead
				// of calculating the color based on the system colors
				// -- this is a way of hooking up into the VSNetColors that I use throughout
				// the MiscGUIStuff
				m_useCustomColor = true;
				m_backgroundColor = value;
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		///-------------------------------------------------------------------------------------
		static public Color SelectionColor
		{
			get
			{
				if (m_useCustomColor && m_selectionColor != Color.Empty)
					return m_selectionColor;
				else
					return CalculateColor(SystemColors.Highlight, SystemColors.Window, 70);
			}
			set
			{
				// Flag that we are going to use custom colors instead
				// of calculating the color based on the system colors
				// -- this is a way of hooking up into the VSNetColor that I use throughout
				// the MiscGUIStuff
				m_useCustomColor = true;
				m_selectionColor = value;
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		///-------------------------------------------------------------------------------------
		static public Color ControlColor
		{
			get
			{
				if ( m_useCustomColor && m_controlColor != Color.Empty )
					return m_controlColor;
				else
					return CalculateColor(SystemColors.Control, BackgroundColor, 195);
			}
			set
			{
				// Flag that we are going to use custom colors instead
				// of calculating the color based on the system colors
				// -- this is a way of hooking up into the VSNetColors that I use throughout
				// the MiscGUIStuff
				m_useCustomColor = true;
				m_controlColor = value;
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		///-------------------------------------------------------------------------------------
		static public Color PressedColor
		{
			get
			{
				if ( m_useCustomColor && m_pressedColor != Color.Empty )
					return m_pressedColor;
				else
					return CalculateColor(SystemColors.Highlight, ColorUtil.SelectionColor, 70);
			}
			set
			{
				// Flag that we are going to use custom colors instead
				// of calculating the color based on the system colors
				// -- this is a way of hooking up into the VSNetColors that I use throughout
				// the MiscGUIStuff
				m_useCustomColor = true;
				m_pressedColor = value;
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		///-------------------------------------------------------------------------------------
		static public Color CheckedColor
		{
			get
			{
				if ( m_useCustomColor && m_pressedColor != Color.Empty )
					return m_checkedColor;
				else
					return CalculateColor(SystemColors.Highlight,  SystemColors.Window, 30);
			}
			set
			{
				// Flag that we are going to use custom colors instead
				// of calculating the color based on the system colors
				// -- this is a way of hooking up into the VSNetColors that I use throughout
				// the MiscGUIStuff
				m_useCustomColor = true;
				m_checkedColor = value;
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		///-------------------------------------------------------------------------------------
		static public Color BorderColor
		{
			get
			{
				if (m_useCustomColor && m_borderColor != Color.Empty)
					return m_borderColor;
				else
				{
					// This color is the default color unless we are using
					// custom colors
					return SystemColors.Highlight;
				}
			}
			set
			{
				// Flag that we are going to use custom colors instead
				// of calculating the color based on the system colors
				// -- this is a way of hooking up into the VSNetColors that I use throughout
				// the MiscGUIStuff
				m_useCustomColor = true;
				m_borderColor = value;
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="front"></param>
		/// <param name="back"></param>
		/// <param name="alpha"></param>
		/// <returns></returns>
		///-------------------------------------------------------------------------------------
		internal static Color CalculateColor(Color front, Color back, int alpha)
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

			float fRed = frontRed*alpha/255 + backRed*((float)(255-alpha)/255);
			byte newRed = (byte)fRed;
			float fGreen = frontGreen*alpha/255 + backGreen*((float)(255-alpha)/255);
			byte newGreen = (byte)fGreen;
			float fBlue = frontBlue*alpha/255 + backBlue*((float)(255-alpha)/255);
			byte newBlue = (byte)fBlue;

			return  Color.FromArgb(255, newRed, newGreen, newBlue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compute a color which is the inverse of the given color, but just a little lighter.
		/// </summary>
		/// <param name="baseColor">Color whose light inverse is to be computed.</param>
		/// <returns>A color which ia a little lighter than the inverse of the given color
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static Color LightInverse(Color baseColor)
		{
			return Color.FromArgb((int)((int)(baseColor.R * 0.7) ^ 0xFF),
				(int)((int)(baseColor.G * 0.7) ^ 0xFF), (int)((int)(baseColor.B * 0.7) ^ 0xFF));
		}
		#endregion

		#region General functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Takes and image and converts it to a disabled-looking image.
		/// </summary>
		/// <param name="image"></param>
		/// <param name="clrBase"></param>
		/// <param name="forHotTracking"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static Image MakeDisabledImage(Image image, Color clrBase, bool forHotTracking)
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

		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="color"></param>
		/// <param name="knownColor"></param>
		/// <param name="useTransparent"></param>
		/// <returns></returns>
		///-------------------------------------------------------------------------------------
		static public bool IsKnownColor(Color color, ref Color knownColor, bool useTransparent)
		{
			// Using the Color structrure "FromKnowColor" does not work if
			// we did not create the color as a known color to begin with
			// we need to compare the rgbs of both color
			Color currentColor = Color.Empty;
			bool badColor = false;

			for (KnownColor clr = 0; clr <= KnownColor.YellowGreen; clr++)
			{
				currentColor = Color.FromKnownColor(clr);
				string colorName = currentColor.Name;

				if (!useTransparent)
					badColor = (colorName == "Transparent");

				if (color.A == currentColor.A && color.R == currentColor.R &&
					color.G == currentColor.G && color.B == currentColor.B &&
					!currentColor.IsSystemColor && !badColor)
				{
					knownColor = currentColor;
					return true;
				}
			}

			return false;
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="color"></param>
		/// <param name="knownColor"></param>
		/// <returns></returns>
		///-------------------------------------------------------------------------------------
		static public bool IsSystemColor(Color color, ref Color knownColor)
		{
			// Using the Color structrure "FromKnowColor" does not work if
			// we did not create the color as a known color to begin with
			// we need to compare the rgbs of both color
			Color currentColor = Color.Empty;

			for (KnownColor clr = 0; clr <= KnownColor.YellowGreen; clr++)
			{
				currentColor = Color.FromKnownColor(clr);
				string colorName = currentColor.Name;

				if (color.R == currentColor.R && color.G == currentColor.G &&
					color.B == currentColor.B && currentColor.IsSystemColor)
				{
					knownColor = currentColor;
					return true;
				}

			}

			return false;
		}
		#endregion

		#region	Public static methods to convert between Color and BGR values.
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
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the name of a color
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>The name</returns>
		/// ------------------------------------------------------------------------------------
		public static string ColorToName(Color color)
		{
			PopulateColorNameTable();

			// lookup the color in the static color table
			int index = s_colorNameTable.IndexOf(new ColorInfo(color));
			if (index >= 0)
				return s_colorNameTable[index].m_name;
			return ColorStrings.kstidColorCustom;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the color value for the given index
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static Color ColorAtIndex(int index)
		{
			if (index < 0 || index >= s_colorNameTable.Count)
				return Color.Empty;
			return s_colorNameTable[index].m_color;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the color name for the given index
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string ColorNameAtIndex(int index)
		{
			if (index < 0 || index >= s_colorNameTable.Count)
				return string.Empty;
			return s_colorNameTable[index].m_name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populates the color name table.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void PopulateColorNameTable()
		{
			if (s_colorNameTable.Count != 0)
				return;

			ResourceManager resMngr = ColorStrings.ResourceManager;
			string clrNameFmt = "kstidColor{0}";

			// Read all of the color entries from the resource file and put them into
			// the static color table.
			for (int i = 0; i < kNumberOfColors; i++)
			{
				// Get the entry from the resources that has the color name and RGB value.
				string resxEntry = resMngr.GetString(string.Format(clrNameFmt, i));
				if (resxEntry == null)
					continue;

				// Parse the entry from the resx file.
				string[] entryParts = resxEntry.Split(",".ToCharArray());
				Debug.Assert(entryParts.Length == 4, "Bad PO file entry for kstidColor"+i, resxEntry);

				string[] safeEntryParts = new string[4] {"BadColor", "0", "0", "0"};
				entryParts.CopyTo(safeEntryParts, 0);

				// Store the name in the color table
				Color color = Color.FromArgb(int.Parse(safeEntryParts[1]),
					int.Parse(safeEntryParts[2]), int.Parse(safeEntryParts[3]));
				s_colorNameTable.Add(new ColorInfo(color, safeEntryParts[0]));
			}
		}

	}
}
