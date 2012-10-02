// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BookVersionAgent.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface needed to support AutoMerge feature so that the version of a book before the
	/// automerge happens can be archived.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IBookVersionAgent
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a saved version of the current version of the book in the given BookMerger if
		/// needed (the agent is responsible for knowing whether a backup already exists).
		/// </summary>
		/// <param name="bookMerger">The book merger.</param>
		/// ------------------------------------------------------------------------------------
		void MakeBackupIfNeeded(BookMerger bookMerger);
	}
}
