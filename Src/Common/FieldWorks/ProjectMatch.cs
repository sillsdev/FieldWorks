// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ProjectMatch.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------

namespace SIL.FieldWorks
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Possible return values for requesting a project match for the project owned by a
	/// remote client.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum ProjectMatch
	{
		/// <summary>The remote client doesn't yet know the project that will be running
		/// </summary>
		DontKnowYet,
		/// <summary>The remote client knows the project that its running and the project
		/// requested is that same project</summary>
		ItsMyProject,
		/// <summary>The remote client knows the project that its running, but the project
		/// requested is not the same project</summary>
		ItsNotMyProject,
		/// <summary>The remote client doesn't yet know the project that will be running and
		/// is currently waiting for another FieldWorks.exe to see if the other has its
		/// project or is waiting for the user to specify the project.</summary>
		WaitingForUserOrOtherFw,
		/// <summary>The remote client is in "single process mode" which means that it
		/// is doing something the requires that all other FW processes be shut down
		/// (e.g. changing the default project location).</summary>
		SingleProcessMode,
	}
}
