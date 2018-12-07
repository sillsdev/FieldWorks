// Copyright (c) 2002-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Possible text fragments
	/// </summary>
	internal enum StTextFrags
	{
		/// <summary>The whole text</summary>
		kfrText,
		/// <summary>regular paragraph</summary>
		kfrPara,
		/// <summary>Footnote</summary>
		kfrFootnote,
		/// <summary>Footnote reference</summary>
		kfrFootnoteReference,
		/// <summary>Footnote marker</summary>
		kfrFootnoteMarker,
		/// <summary>Footnote paragraph</summary>
		kfrFootnotePara,
		/// <summary>CmTranslation instance of a paragraph</summary>
		kfrTranslation,
		/// <summary>Label for the beginning of the first paragraph of an StText</summary>
		kfrLabel,
		/// <summary>Segments of an StTextPara, each displaying its free translation annotation.  </summary>
		kfrSegmentFreeTranslations
	};
}