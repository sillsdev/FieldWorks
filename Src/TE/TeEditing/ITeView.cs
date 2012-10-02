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
// File: ITeView.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ITeView
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location tracker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ILocationTracker LocationTracker { get;}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface implemented by TE draft views to provide access to the VC and rootbox.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ITeDraftView
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this is a back translation view, this is the corresponding vernacular view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITeDraftView VernacularDraftView { get;}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the view constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		TeStVc Vc { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rootbox.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IVwRootBox RootBox { get; }
	}
}
