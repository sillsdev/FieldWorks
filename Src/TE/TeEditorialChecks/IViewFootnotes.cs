// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IViewFootnotes.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IViewFootnotes
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the footnote view.
		/// </summary>
		/// <param name="footnote">The footnote to scroll to, or null to just open the footnote
		/// pane.</param>
		/// ------------------------------------------------------------------------------------
		void ShowFootnoteView(IStFootnote footnote);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides the footnote view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HideFootnoteView();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether the footnote pane or the other (Scripture) pane has
		/// focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool FootnoteViewFocused { set; }
	}
}
