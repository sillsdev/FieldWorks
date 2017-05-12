// Copyright (c) 2002-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Resources;

namespace SIL.CoreImpl.Text
{
	///-----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ColorUtil.
	/// </summary>
	///-----------------------------------------------------------------------------------------
	public static class ColorUtil
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
			public readonly string m_name;

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

		private static readonly List<ColorInfo> ColorNameTable = new List<ColorInfo>();

		/// <summary>Number of colors</summary>
		public const int kNumberOfColors = 40;

		#region	Public static methods to convert between Color and BGR values.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///  Convert a .NET color to the BGR value understood by Views code.
		/// </summary>
		/// <param name="c">.NET Color value</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static uint ConvertColorToBGR(Color c)
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
		public static Color ConvertBGRtoColor(uint bgrColor)
		{
			if (bgrColor == 0xC0000000)
				return Color.FromKnownColor(KnownColor.Transparent);
			return ColorTranslator.FromWin32((int)bgrColor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts an integer from a RGB format to a BGR format
		/// </summary>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		public static uint ConvertRGBtoBGR(uint value)
		{
			return ((value & 0xFF) << 16 | (value & 0xFF00) | (value & 0xFF0000) >> 16);
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
			int index = ColorNameTable.IndexOf(new ColorInfo(color));
			if (index >= 0)
				return ColorNameTable[index].m_name;
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
			if (index < 0 || index >= ColorNameTable.Count)
				return Color.Empty;
			return ColorNameTable[index].m_color;
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
			if (index < 0 || index >= ColorNameTable.Count)
				return string.Empty;
			return ColorNameTable[index].m_name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populates the color name table.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void PopulateColorNameTable()
		{
			if (ColorNameTable.Count != 0)
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
				ColorNameTable.Add(new ColorInfo(color, safeEntryParts[0]));
			}
		}

	}
}
