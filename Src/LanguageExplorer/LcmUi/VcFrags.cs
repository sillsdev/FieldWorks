// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// This enumeration lists fragments that are supported in some way by all CmObject
	/// subclasses. Many classes may implement them only using the approach here, which
	/// for some of them is very minimal. Override and enhance where possible.
	/// </summary>
	internal enum VcFrags
	{
		// numbers below this are reserved for internal use by VCs.
		kfragShortName = 10567, // an arbitrary number, unlikely to be confused with others.
		kfragName,
		// Currently only MSAs and subclasses have interlinear names.
		kfragInterlinearName, // often just ShortName, what we want to appear in an interlinear view.
		kfragInterlinearAbbr, // use abbreviation for grammatical category.
		kfragFullMSAInterlinearname, // Used for showing MSAs in the MSA editor dlg.
		kfragHeadWord,  // defined only for LexEntry, fancy form of ShortName.
		kfragPosAbbrAnalysis, // display a PartOfSpeech using its analyis Ws abbreviation.
	}
}