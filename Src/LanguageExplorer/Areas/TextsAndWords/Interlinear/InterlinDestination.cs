// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// These are the destinations we currently care about in SFM interlinear import.
	/// For each of these there should be a ksFldX that is its localizable name (see
	/// InterlinearSfmImportWizard.GetDestinationName()).
	/// It is public only because XmlSerializer requires everything to be.
	/// </summary>
	public enum InterlinDestination
	{
		Ignored, // pay no attention to this field (except it terminates the previous one).
		Id, // marks start of new text (has no data)
		Abbreviation, // maps to Text.Abbreviation (and may start new text)
		Title, // maps to Text.Name (inherited from CmMajorObject) (and may start new text)
		Source, // Text.Source (and may start new text)
		Comment, // Text.Description (and may start new text)
		ParagraphBreak, // causes us to start a new paragraph
		Reference, // forces segment break and sets Segment.Reference
		Baseline, // Becomes part of the StTxtPara.Contents
		FreeTranslation, // Segment.FreeTranslation
		LiteralTranslation, // Segment.LiteralTranslation
		Note, // each generates a Segment.Note and is its content.
		Wordform,
		WordGloss
	}
}
