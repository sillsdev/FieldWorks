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
// File: IStylesTab.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for the styles dialog to communicate to its tabs
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IStylesTab
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the information on the tab to the specified style info.
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		void SaveToInfo(StyleInfo styleInfo);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the information on the tab with the information in the specified style info.
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		void UpdateForStyle(StyleInfo styleInfo);
	}
}
