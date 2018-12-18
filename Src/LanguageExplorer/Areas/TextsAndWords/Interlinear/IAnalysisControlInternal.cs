// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// implements everything needed by the FocuxBoxControl
	/// </summary>
	internal interface IAnalysisControlInternal
	{
		bool RightToLeftWritingSystem { get; }
		bool HasChanged { get; }
		void Undo();
		void SwitchWord(AnalysisOccurrence selected);
		void MakeDefaultSelection();
		bool ShouldSave(bool fSaveGuess);
		AnalysisTree GetRealAnalysis(bool fSaveGuess, out IWfiAnalysis obsoleteAna);
		int GetLineOfCurrentSelection();
		bool SelectOnOrBeyondLine(int startLine, int increment);
		void UpdateLineChoices(InterlinLineChoices choices);
		int MultipleAnalysisColor { set; }
		bool IsDirty { get; }
	}
}