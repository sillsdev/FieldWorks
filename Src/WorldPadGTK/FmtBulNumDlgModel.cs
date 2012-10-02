// FmtBulNumDlgModel.cs
// User: Jean-Marc Giffin at 11:32 A 23/06/2008

using System;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	public class FmtBulNumDlgModel : IDialogModel
	{
		private BulNumType type_;
		private int textBefore_;
		private int textAfter_;

		public enum BulNumType
		{
			None,
			Bullet,
			Number,
			Unspecifed
		}

		public FmtBulNumDlgModel()
		{
		}

		public BulNumType Type
		{
			set { type_ = value; }
			get { return type_; }
		}

		public int TextBefore
		{
			set { textBefore_ = value; }
			get { return textBefore_; }
		}

		public int TextAfter
		{
			set { textAfter_ = value; }
			get { return textAfter_; }
		}


	}
}
