using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews
{
	public abstract class LeafBox : Box, ClientRun
	{
		/// <summary>
		/// A root class for all varieties of Box that do not contain other boxes.
		/// </summary>
		/// <param name="styles"></param>
		public LeafBox(AssembledStyles styles) : base(styles)
		{

		}

		/// <summary>
		/// Considered as a client run, a leaf box is always exactly one character long.
		/// </summary>
		int ClientRun.Length { get { return 1; } }

		/// <summary>
		/// Considererd as a client run, a leaf box's text is always the object replacement character.
		/// </summary>
		string ClientRun.Text { get { return "\0xfffc"; } }


		/// <summary>
		/// Make whatever selection is appropriate for the given click. The transformation is the usual one passed to this box,
		/// that is, it transforms where (which is in paint coords) into a point in the same coordinate system as our own top, left.
		/// </summary>
		internal virtual Selections.Selection MakeSelectionAt(Point where, IVwGraphics vg, PaintTransform leafTrans)
		{
			return null;
		}
	}
}
