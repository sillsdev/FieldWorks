// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace FieldWorks.TestUtilities
{
	/// <summary>How to display the text</summary>
	[Flags]
	public enum DisplayType
	{
		/// <summary>Display lazy boxes</summary>
		kLazy = 1,
		/// <summary>Display non-lazy boxes</summary>
		kNormal = 2,
		/// <summary>Display paragraphs with top-margin</summary>
		kWithTopMargin = 4,
		/// <summary>Display lazy and non-lazy boxes (this isn't really "all", because it
		/// doesn't include outer object details or literal string labels)</summary>
		kAll = 7,
		/// <summary>Display outer object details (for testing GetOuterObject)</summary>
		kOuterObjDetails = 8,
		/// <summary>View adds a read-only label literal string as a label before
		/// each paragraph</summary>
		kLiteralStringLabels = 16,
		/// <summary>View adds each paragraph an additional time (only applies when kNormal flag is set)</summary>
		kDuplicateParagraphs = 32,
		/// <summary>Display a mapped paragraph</summary>
		kMappedPara = 64,
		/// <summary>In addition to displaying the normal StTexts as requested in the
		/// constructor, also display the ScrBook.Title. (This will only work if the root
		/// object is a ScrBook.)</summary>
		kBookTitle = 128,
		/// <summary>Display a Footnote by displaying its "FootnoteMarker" in a paragraph
		/// by itself, followed by its sequence of paragraphs.</summary>
		kFootnoteDetailsSeparateParas = 256,
		/// <summary>Display a Footnote by displaying its "FootnoteMarker" followed by the
		/// contents of its first paragraph (similar to the way footnotes are displayed in
		/// real life.</summary>
		kFootnoteDetailsSinglePara = 512,
		/// <summary>When displaying normal (non-tagged) paragraphs, apply the properties</summary>
		kUseParaProperties = 1024,
		/// <summary>Strangely enough, the "normal" behavior of this VC is to display paragraph
		/// contents three times. use this flag to suppress this.</summary>
		kOnlyDisplayContentsOnce = 2048,
	}
}