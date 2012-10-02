// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IPublicationView.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implement that any publication control that appears as a view in the client area of a
	/// main window must implement.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IPublicationView : IRootSite
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the publication overrides on styles in the view.
		/// </summary>
		/// <param name="pubCharSize">Base character size to use for normal in the stylesheet
		/// in millipoints.</param>
		/// <param name="pubLineSpacing">The line spacing to use for normal in the stylesheet
		/// in millipoints.</param>
		/// ------------------------------------------------------------------------------------
		void ApplyPubOverrides(decimal pubCharSize, decimal pubLineSpacing);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle Print command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool OnFilePrint();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of pages in the publication
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int PageCount
		{
			get;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface that any page setup dialog must implement (and ours does!)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IPageSetupDialog : IDisposable
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the size of the base character in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int BaseCharacterSize { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the base (normal) line spacing in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int BaseLineSpacing { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text associated with this control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Text { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Runs the page setup dialog box with a default owner.
		/// </summary>
		/// <returns><see cref="DialogResult.OK"/> if the user clicks OK in the dialog box;
		/// otherwise, <see cref="DialogResult.Cancel"/>. </returns>
		/// ------------------------------------------------------------------------------------
		DialogResult ShowDialog();
	}
}
