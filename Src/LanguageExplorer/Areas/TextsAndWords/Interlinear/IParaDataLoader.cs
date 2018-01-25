// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// interface for loading the decorator.
	/// </summary>
	public interface IParaDataLoader
	{
		void LoadParaData(IStTxtPara para);
		void LoadSegmentData(ISegment seg);
		void ResetGuessCache();
		bool UpdatingOccurrence(IAnalysis oldAnalysis, IAnalysis newAnalysis);
		void RecordGuessIfNotKnown(AnalysisOccurrence occurrence);
		IAnalysis GetGuessForWordform(IWfiWordform wf, int ws);
		AnalysisGuessServices GuessServices { get; }
	}
}