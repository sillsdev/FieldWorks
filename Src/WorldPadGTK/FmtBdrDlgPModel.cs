// FmtBdrDlgPModel.cs
// User: Jean-Marc Giffin at 3:48 P 16/06/2008

using System;
using Gtk;
using Gdk;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	/// <summary>Model for the Format -> Border dialog.</summary>
	public class FmtBdrDlgPModel : IDialogModel
	{
		private Color borderColor_;
		private bool topChecked_;
		private bool bottomChecked_;
		private bool trailingChecked_;
		private bool leadingChecked_;
		private bool allChecked_;
		private bool noneChecked_;
		private int borderWidth_;

		/// <summary>Set defaults for the model.</summary>
		public FmtBdrDlgPModel()
		{
			topChecked_ = false;
			bottomChecked_ = false;
			trailingChecked_ = false;
			leadingChecked_ = false;
			allChecked_ = false;
			noneChecked_ = false;
			borderWidth_ = 1;
		}

		/// <summary>Color of the Border</summary>
		public Color BorderColor
		{
			get { return borderColor_; }
			set { borderColor_ = value; }
		}

		/// <summary>Is Top Checked?</summary>
		public bool Top
		{
			get { return topChecked_; }
			set { topChecked_ = value; }
		}

		/// <summary>Is Bottom Checked?</summary>
		public bool Bottom
		{
			get { return bottomChecked_; }
			set { bottomChecked_ = value; }
		}

		/// <summary>Is Right Checked?</summary>
		public bool Trailing
		{
			get { return trailingChecked_; }
			set { trailingChecked_ = value; }
		}

		/// <summary>Is Left Checked?</summary>
		public bool Leading
		{
			get { return leadingChecked_; }
			set { leadingChecked_ = value; }
		}

		/// <summary>Is None Pressed?</summary>
		public bool None
		{
			get { return noneChecked_; }
			set { noneChecked_ = value; }
		}

		/// <summary>Is All Pressed?</summary>
		public bool All
		{
			get { return allChecked_; }
			set { allChecked_ = value; }
		}

		/// <summary>The Border's Width</summary>
		public int BorderWidth
		{
			get { return borderWidth_; }
			set { borderWidth_ = value; }
		}
	}
}
