// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
