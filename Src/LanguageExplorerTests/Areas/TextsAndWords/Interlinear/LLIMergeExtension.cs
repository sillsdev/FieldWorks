// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
	/// <summary>LinguaLinksImport for testing the choice to merge, without actually showing the dialog during unit tests</summary>
	internal sealed class LLIMergeExtension : LinguaLinksImport
	{
		/// <summary/>
		public int NumTimesDlgShown { get; private set; }

		/// <summary />
		public LLIMergeExtension(LcmCache cache, string tempDir, string rootDir) : base(cache, tempDir, rootDir)
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