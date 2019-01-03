// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas.Lists;
using SIL.LCModel;

namespace LanguageExplorerTests
{
	internal sealed class DeleteListHelper : DeleteCustomList
	{
		internal string PossNameInDlg { get; set; }

		public DeleteListHelper(LcmCache cache) : base(cache)
		{
			PossNameInDlg = string.Empty;
		}

		protected override DialogResult CheckWithUser(string name)
		{
			PossNameInDlg = name;
			return ExpectedTestResponse;
		}

		public DialogResult ExpectedTestResponse { get; set; }
	}
}