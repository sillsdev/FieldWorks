// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace ParatextImport
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
