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
// File: IScrProjMetaDataProvider.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a scripture reference
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IScrProjMetaDataProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current versification scheme
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ScrVers Versification { get;}
	}
}
