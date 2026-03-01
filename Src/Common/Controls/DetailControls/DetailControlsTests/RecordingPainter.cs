// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Test double that implements <see cref="IDataTreePainter"/> and records
	/// every paint invocation rather than drawing anything on screen.
	/// </summary>
	/// <remarks>
	/// Inject via <see cref="DataTree.Painter"/> to intercept painting.
	/// Useful for asserting that painting was triggered, how many times,
	/// and with what parameters.
	/// </remarks>
	internal sealed class RecordingPainter : IDataTreePainter
	{
		/// <summary>
		/// Record of a single call to <see cref="PaintLinesBetweenSlices"/>.
		/// </summary>
		public struct PaintCall
		{
			public Graphics Graphics;
			public int Width;
		}

		/// <summary>
		/// All recorded paint calls, in order.
		/// </summary>
		public List<PaintCall> Calls { get; } = new List<PaintCall>();

		/// <summary>
		/// Convenience: number of times painting was invoked.
		/// </summary>
		public int PaintCallCount => Calls.Count;

		/// <summary>
		/// When true, forward the paint call to an optional delegate
		/// after recording it (useful for partial mocking / spy pattern).
		/// </summary>
		public IDataTreePainter Delegate { get; set; }

		public void PaintLinesBetweenSlices(Graphics gr, int width)
		{
			Calls.Add(new PaintCall { Graphics = gr, Width = width });
			Delegate?.PaintLinesBetweenSlices(gr, width);
		}
	}
}
