// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IBtAwareView.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface to be implemented by views and view wrappers that might host a back
	/// translation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IBtAwareView
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets the back translation writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int BackTranslationWS
		{
			get;
			set;
		}
	}
}
