// FmtFntDlgModel.cs
// User: Jean-Marc Giffin at 4:23 P 17/06/2008

using System;
using Gdk;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	public class FmtFntDlgModel : IDialogModel
	{
		private int font_;
		private int size_;
		private Color fgColor_;
		private Color bgColor_;
		private int underlineStyle_;
		private Color underlineColor_;
		private bool bold_;
		private bool italic_;
		private bool superscript_;
		private bool subscript_;
		private int position_;
		private double positionBy_;

		public FmtFntDlgModel()
		{
		}

		public int Font
		{
			get { return font_; }
			set { font_ = value; }
		}

		public int Size
		{
			get { return size_; }
			set { size_ = value; }
		}

		public int UnderlineStyle
		{
			get { return underlineStyle_; }
			set { underlineStyle_ = value; }
		}

		public int Position
		{
			get { return position_; }
			set { position_ = value; }
		}

		public double PositionBy
		{
			get { return positionBy_; }
			set { positionBy_ = value; }
		}

		public Color FontColor
		{
			get { return fgColor_; }
			set { fgColor_ = value; }
		}

		public Color BackgroundColor
		{
			get { return bgColor_; }
			set { bgColor_ = value; }
		}

		public Color UnderlineColor
		{
			get { return underlineColor_; }
			set { underlineColor_ = value; }
		}

		public bool Bold
		{
			get { return bold_; }
			set { bold_ = value; }
		}

		public bool Italic
		{
			get { return italic_; }
			set { italic_ = value; }
		}

		public bool Superscript
		{
			get { return superscript_; }
			set { superscript_ = value; }
		}

		public bool Subscript
		{
			get { return subscript_; }
			set { subscript_ = value; }
		}
	}
}
