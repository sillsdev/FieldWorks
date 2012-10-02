// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2010' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
