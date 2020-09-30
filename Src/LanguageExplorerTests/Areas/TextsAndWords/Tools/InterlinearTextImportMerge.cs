// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Tools;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Areas.TextsAndWords.Tools
{
	/// <summary>InterlinearTextImport for testing the choice to merge, without actually showing the dialog during unit tests</summary>
	internal sealed class InterlinearTextImportMerge : InterlinearTextImport
	{
		/// <summary/>
		public int NumTimesDlgShown { get; private set; }

		/// <summary />
		public InterlinearTextImportMerge(LcmCache cache) : base(cache)
		{
			NumTimesDlgShown = 0;
		}

		protected override DialogResult ShowPossibleMergeDialog(IThreadedProgress progress)
		{
			NumTimesDlgShown++;
			return DialogResult.Yes;
		}
	}
}