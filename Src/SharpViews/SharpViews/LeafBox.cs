// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;

namespace SIL.FieldWorks.SharpViews
{
	public abstract class LeafBox : Box, IClientRun
	{
		/// <summary>
		/// The character (known in Unicode as the Object Replacement Character) that we embed in paragraph source to stand for an
		/// embedded box.
		/// </summary>
		public const char CharThatStandsForBox = '\xfffc';

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
		int IClientRun.Length { get { return 1; } }

		/// <summary>
		/// Considered as a client run, a leaf box's text is always the object replacement character.
		/// </summary>
		string IClientRun.Text { get { return CharThatStandsForBox.ToString(); } }

		public LiteralStringParaHookup Hookup
		{
			get { return null; }
			set { }
		}

		public int WritingSystemAt(int index)
		{
			return Style.Ws;
		}

		public string CharacterStyleNameAt(int index)
		{
			return Style.StyleName;
		}

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
