// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// --------------------------------------------------------------------------------------------

using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for the FilterTextsDialog that is used in Interlinear in FLEx
	/// </summary>
	/// <typeparam name="T">Should only be used with IStText (but because of dependencies we
	/// can't specify that directly)</typeparam>
	/// <remarks>We have to use an interface to decouple the SE and BTE editions.</remarks>
	/// ----------------------------------------------------------------------------------------
	public interface IFilterTextsDialog<T>
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a list of the included IStText nodes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		T[] GetListOfIncludedTexts();

		/// <summary>
		/// Save the information needed to prune the tree later.
		/// </summary>
		void PruneToSelectedTexts(T text);

		/// <summary>
		/// Get/set the label shown above the tree view.
		/// </summary>
		string TreeViewLabel { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text on the dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Text { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the form as a modal dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		DialogResult ShowDialog(IWin32Window owner);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// IFilterScrSectionDialog extension methods. We need this class so that we can use
	/// code like the following:
	/// using (IFilterScrSectionDialog dlg = (IDisposable)new FilterScrSectionDialog()) {}
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class FilterTextDialogExtensions
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Dispose(this IFilterTextsDialog<Type> positionHandler)
		{
			var disposable = positionHandler as IDisposable;
			if (disposable != null)
				disposable.Dispose();
		}
	}
}
