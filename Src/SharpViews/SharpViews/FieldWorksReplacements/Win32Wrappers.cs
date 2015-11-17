// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SIL.Utils
{
	/// <summary>
	/// Redefine Rect structure.
	/// </summary>
	/// <remarks>We can't simply use Rectangle, because the struct layout
	/// is different (Rect uses left, top, right, bottom, whereas Rectangle uses x, y,
	/// width, height).</remarks>
	public struct Rect
	{
		/// <summary>Specifies the x-coordiante of the upper-left corner of the rectangle</summary>
		public int left;
		/// <summary>Specifies the y-coordiante of the upper-left corner of the rectangle</summary>
		public int top;
		/// <summary>Specifies the x-coordiante of the lower-right corner of the rectangle</summary>
		public int right;
		/// <summary>Specifies the y-coordiante of the lower-right corner of the rectangle</summary>
		public int bottom;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a rectangle with the specified coordinates
		/// </summary>
		/// <param name="l">left</param>
		/// <param name="t">top</param>
		/// <param name="r">right</param>
		/// <param name="b">bottom</param>
		/// ------------------------------------------------------------------------------------
		public Rect(int l, int t, int r, int b)
		{
			left = l; top = t; right = r; bottom = b;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a Rect struct to a .NET Rectangle
		/// </summary>
		/// <param name="rc">Windows rectangle</param>
		/// <returns>.NET rectangle</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator Rectangle(Rect rc)
		{
			return Rectangle.FromLTRB(rc.left, rc.top, rc.right, rc.bottom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a .NET rectangle to a windows rectangle
		/// </summary>
		/// <param name="rc">.NET rectangle</param>
		/// <returns>Windows rectangle</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator Rect(Rectangle rc)
		{
			return new Rect(rc.Left, rc.Top, rc.Right, rc.Bottom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test whether the rectangle contains the specified point.
		/// </summary>
		/// <param name="pt"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool Contains(System.Drawing.Point pt)
		{
			if (pt.X < left)
				return false;
			if (pt.X > right)
				return false;
			if (pt.Y < top)
				return false;
			if (pt.Y > bottom)
				return false;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return "left=" + left + ", top=" + top +
				", right=" + right + ", bottom=" + bottom;
		}
	}
}
