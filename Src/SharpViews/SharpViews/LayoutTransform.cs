// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// A LayoutTransform contains the information that a Box needs to transform points in its own coordinate system (or possibly its container's
	/// coordinate system) into points relative to the view as a whole.
	/// This class is immutable; if you need a different one, make a new one.
	/// </summary>
	public class LayoutTransform
	{
		/// <summary>
		/// This transform is for a box whose top left is XOffset pixels right of the top left of the whole view.
		/// </summary>
		internal int XOffset { get; private set; }
		/// <summary>
		/// This transform is for a box whose top left is YOffset pixels below of the top left of the whole view.
		/// </summary>
		internal int YOffset { get; private set; }

		public int DpiX { get; private set; }
		public int DpiY { get; private set; }


		/// <summary>
		/// This constructor makes a transform suitable for a box whose top left is dx below and dy to the right of the top left of the
		/// whole view area, and which will be laid out as if for a device resolution of dpiX pixels per inch horizontally and dpiY vertically.
		/// </summary>
		/// <param name="dx"></param>
		/// <param name="dy"></param>
		/// <param name="dpiX"></param>
		/// <param name="dpiY"></param>
		public LayoutTransform(int dx, int dy, int dpiX, int dpiY)
		{
			XOffset = dx;
			YOffset = dy;
			DpiX = dpiX;
			DpiY = dpiY;
		}

		/// <summary>
		/// Answer a layout transform suitable for a child box whose left and top are dx and dy.
		/// (Keep InitializeOnlyOffsetBy consistent with this.)
		/// </summary>
		public LayoutTransform OffsetBy(int dx, int dy)
		{
			return new LayoutTransform(XOffset + dx, YOffset + dy, DpiX, DpiY);
		}

		/// <summary>
		/// This method is internal only because I definitely don't want it as public as 'protected'.
		/// It should ONLY be used by subclass methods constructing copies offset by the specified amount.
		/// </summary>
		internal void InitializeOnlyOffsetBy(int dx, int dy)
		{
			XOffset += dx;
			YOffset += dy;
		}

		/// <summary>
		/// Converts a Point in box coordinates to whole-view coordinates.
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public Point ToRoot(Point p)
		{
			return new Point(p.X + XOffset, p.Y + YOffset);
		}

		/// <summary>
		/// Transform a distance in millipoints (72,000 per inch) into pixels in the X direction.
		/// </summary>
		/// <param name="mp"></param>
		/// <returns></returns>
		public int MpToPixelsX(int mp)
		{
			return MulDiv(mp, DpiX, kmpInch);
		}

		public const int kmpInch = 72000; // millipoints per inch.
		/// <summary>
		/// Transform a distance in millipoints (72,000 per inch) into pixels in the X direction.
		/// </summary>
		/// <param name="mp"></param>
		/// <returns></returns>
		public int MpToPixelsY(int mp)
		{
			return MulDiv(mp, DpiY, kmpInch);
		}


		/// <summary>
		/// Multiply the first two arguments and divide by the third. Assume the result will not overflow, but the intermediate
		/// product may be larger than an int. Round to the closest integer result. Assumes div is not zero.
		/// </summary>
		internal static int MulDiv(int mp, int dpi, int div)
		{
			long product = Math.BigMul(mp, dpi);
			long rem;
			long result = Math.DivRem(product, div, out rem); // expect this truncates toward zero
			if (Math.Abs(rem) >= div / 2) // closer to the number one larger, round up.
				result += rem > 0 ? 1 : -1;
			return (int) result;
		}

		/// <summary>
		/// Convert a border width in mp to pixels. Should not answer zero unless mp is zero.
		/// </summary>
		/// <param name="mp"></param>
		/// <returns></returns>
		public int MpToBorderPixelsX(int mp)
		{
			return MulDivNotToZero(mp, DpiX, kmpInch);
		}

		/// <summary>
		/// Convert a border height in mp to pixels. Should not answer zero unless mp is zero.
		/// </summary>
		/// <param name="mp"></param>
		/// <returns></returns>
		public int MpToBorderPixelsY(int mp)
		{
			return MulDivNotToZero(mp, DpiX, kmpInch);
		}

		/// <summary>
		/// Multiply the first two arguments and divide by the third. Assume the result will not overflow, but the intermediate
		/// product may be larger than an int. Round to the closest integer result, except that the result must not be zero
		/// unless the first argument is zero.
		/// </summary>
		internal static int MulDivNotToZero(int mp, int dpi, int div)
		{
			if (mp == 0)
				return 0; // this case is VERY common, handle efficiently.
			int result = MulDiv(mp, dpi, div);
			if (result != 0 )
				return result;
			return mp > 0 ? 1 : -1;
		}
	}
}
