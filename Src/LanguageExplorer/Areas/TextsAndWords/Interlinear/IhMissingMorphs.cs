// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This combo box appears in the same place in the view as the IhMorphForm one, but
	/// when an analysis is missing. Currently it has the same options
	/// as the IhMorphForm, but the process of building the combo is slightly different
	/// because the initial text is taken from the word form, not the morph forms. There
	/// may eventually be other differences, such as subtracting an item to delete the
	/// current analysis.
	/// </summary>
	internal class IhMissingMorphs : IhMorphForm
	{
		public override void SetupCombo()
		{
			InitCombo();
			ComboList.Text = StrFromTss(m_caches.DataAccess.get_MultiStringAlt(m_hvoSbWord, SandboxBase.ktagSbWordForm, m_sandbox.RawWordformWs));
			ComboList.Items.Add(ITextStrings.ksEditMorphBreaks_);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}
	}
}