// OldWritingSystemsDlgModel.cs
// User: Jean-Marc Giffin at 2:26 P 18/06/2008

using System;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	public class OldWritingSystemsDlgModel : IDialogModel
	{
		private static FmtWrtSysDlgModel.Language[] languages_ = FmtWrtSysDlgModel.languages_;
		private int selection_;

		public OldWritingSystemsDlgModel()
		{
		}

		public int Selection
		{
			get { return selection_; }
			set { selection_ = value; }
		}

		public FmtWrtSysDlgModel.Language[] Languages
		{
			get { return languages_; }
			set { }
		}

		public static string[] GetLanguageNames()
		{
			string[] names = new string[languages_.Length];
			for (int i = 0; i < languages_.Length; i++)
				names[i] = languages_[i].Name;
			return names;
		}
	}
}
