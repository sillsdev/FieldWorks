// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ITeImportCallbacks.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for callbacks that happen during an import
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ITeImportCallbacks
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FilteredScrBooks BookFilter { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view zoom percent.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		float DraftViewZoomPercent { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote zoom percent.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		float FootnoteZoomPercent { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the selection of the current view to the specified reference
		/// </summary>
		/// <param name="targetRef">The target reference.</param>
		/// <returns>True if the selection was moved, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		bool GotoVerse(ScrReference targetRef);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the key terms view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void UpdateKeyTermsView();
	}
}
