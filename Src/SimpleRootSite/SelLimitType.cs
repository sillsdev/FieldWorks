// Copyright (c) 2002-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Types of selection limits
	/// </summary>
	public enum SelLimitType
	{
		/// <summary>
		/// Use this to retrieve info about the limit of the selection which is closest
		/// (logically) to the top of the view.
		/// </summary>
		Top,
		/// <summary>
		/// Use this to retrieve info about the limit of the selection which is closest
		/// (logically) to the bottom of the view.
		/// </summary>
		Bottom,
		/// <summary>
		/// Use this to retrieve info about the anchor of the selection (i.e., where the
		/// user clicked to start the selection).
		/// </summary>
		Anchor,
		/// <summary>
		/// Use this to retrieve info about the end of the selection (i.e., the location
		/// to which the user shift-clicked, shift-arrowed, or ended their mouse drag to
		/// extend the selection).
		/// </summary>
		End,
	}
}