// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
