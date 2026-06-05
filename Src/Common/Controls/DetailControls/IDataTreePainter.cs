// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Abstracts the painting operations that DataTree performs between
	/// slices, allowing tests to intercept/record draw calls without a
	/// real screen.
	/// </summary>
	/// <remarks>
	/// DataTree implements this interface as its default behavior. Tests
	/// can substitute a recording implementation via the
	/// <see cref="DataTree.Painter"/> property to verify rendering
	/// without a visible window.
	/// </remarks>
	public interface IDataTreePainter
	{
		/// <summary>
		/// Paint separator lines between slices onto the given Graphics context.
		/// </summary>
		/// <param name="gr">The Graphics surface to draw on (may be bitmap-backed for tests).</param>
		/// <param name="width">The width of the drawing area in pixels.</param>
		void PaintLinesBetweenSlices(Graphics gr, int width);
	}
}
