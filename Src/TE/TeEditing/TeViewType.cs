// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ViewType.cs
// Responsibility: TE Team

using System;

namespace SIL.FieldWorks.TE
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Gives a generic way to determine what type of view this is and what is showing in it
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[Flags]
	public enum TeViewType
	{
		// View         Content
		// type         type
		// 0000001 0000 0001
		// ||||||| |||| |||- Scripture
		// ||||||| |||| ||-  Back Translation
		// ||||||| |||| |-   Checks
		// ||||||| |||| -    Not used
		// ||||||| ||||
		// ||||||| |||-      Draft
		// ||||||| ||-       Print
		// ||||||| |-        Horizontal
		// ||||||| -         Vertical
		// |||||||
		// |||||||                  Scripture         |       Back translation                  | Checks
		// |||||||                 Draft| Print       | Draft           | Print                 |
		// |||||||                 -------------------------------------------------------------------------------
		// ||||||-         View 1: Draft| Print layout| BT draft        | BT parallel print     | Editorial Checks
		// |||||-          View 2: -    | Trial publ. | -               | -                     | Keyterms
		// ||||-           View 3: -    | Correction  | Consultant check| BT simple print layout| Concordance
		// |||-            Footnote View
		// ||-             NotesDataEntry
		// |-              StyleView
		// -               Diff View

		// Content types
		/// <summary></summary>
		Scripture = 1 << 0,			// 1
		/// <summary></summary>
		BackTranslation = 1 << 1,	// 2
		/// <summary></summary>
		Checks = 1 << 2,			// 4

		// Flags
		/// <summary></summary>
		Draft = 1 << 4,				// 16
		/// <summary></summary>
		Print = 1 << 5,				// 32
		/// <summary></summary>
		Horizontal = 1 << 6,		// 64
		/// <summary></summary>
		Vertical = 1 << 7,			// 128

		// View types
		/// <summary></summary>
		View1 = 1 << 8,				// 256
		/// <summary></summary>
		View2 = 1 << 9,				// 512
		/// <summary></summary>
		View3 = 1 << 10,			// 1024
		/// <summary></summary>
		FootnoteView = 1 << 11,		// 2048
		/// <summary></summary>
		StyleView = 1 << 13,		 // 8192
		/// <summary></summary>
		DiffView = 1 << 14,	         // 16384


		// Masks
		/// <summary></summary>
		ViewTypeMask = 0xFF00,
		/// <summary></summary>
		ContentTypeMask = BackTranslation | Scripture | Checks,
		/// <summary>Used to determine whether a view is a printable view of vernacular Scripture</summary>
		ScripturePrintMask = Scripture | Print,

		/// <summary></summary>
		DraftView = Scripture | View1 | Draft | Horizontal,
		/// <summary></summary>
		PrintLayout = Scripture | View1 | Print | Horizontal,
		/// <summary></summary>
		BackTranslationDraft = BackTranslation | View1 | Draft | Horizontal,
		/// <summary></summary>
		BackTranslationParallelPrint = BackTranslation | View1 | Print | Horizontal,
		/// <summary></summary>
		EditorialChecks = Checks | View1,
		/// <summary></summary>
		TrialPublication = Scripture | View2 | Print | Horizontal,
		/// <summary></summary>
		KeyTerms = Checks | View2,
		/// <summary></summary>
		Correction = Scripture | View3 | Print | Horizontal,
		/// <summary></summary>
		BackTranslationConsultantCheck = BackTranslation | View3 | Draft | Horizontal,
		/// <summary></summary>
		BackTranslationSimplePrintLayout = BackTranslation | View3 | Print | Horizontal,
		/// <summary></summary>
		Concordance = Checks | View3,
		/// <summary></summary>
		VerticalView = Scripture | View1 | Draft | Vertical,
	}
}
