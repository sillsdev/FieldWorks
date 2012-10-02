// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IViewFootnotes.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO.Cellar;

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
		void ShowFootnoteView(StFootnote footnote);

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
