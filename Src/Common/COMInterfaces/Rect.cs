// Copyright (c) 2015 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System.Drawing;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	#region Struct Rect
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
		/// ------------------------------------------------------------------------------------
		public bool Contains(Point pt)
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
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with this instance.</param>
		/// <returns><c>true</c> if the specified <see cref="T:System.Object"/> is equal to this
		/// instance; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			return (obj is Rect && (Rect)obj == this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data
		/// structures like a hash table.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return top ^ (bottom >> 4) ^ (left >> 8) ^ (right >> 12);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="rc1">The first rectangle.</param>
		/// <param name="rc2">The second rectangle.</param>
		/// <returns>The result of the operator.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool operator == (Rect rc1, Rect rc2)
		{
			return (rc1.top == rc2.top && rc1.bottom == rc2.bottom && rc1.left == rc2.left &&
				rc1.right == rc2.right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="rc1">The first rectangle.</param>
		/// <param name="rc2">The second rectangle.</param>
		/// <returns>The result of the operator.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool operator != (Rect rc1, Rect rc2)
		{
			return !(rc1 == rc2);
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
	#endregion

}
