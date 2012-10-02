// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PaRemoteRequest.cs
// Responsibility: Olson
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------

namespace SIL.FieldWorks.PaObjects
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PaRemoteRequest : RemoteRequest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether [is same project] [the specified name].
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShouldWait(string name, string server)
		{
			var matchStatus = FieldWorks.GetProjectMatchStatus(new ProjectId(name, server));
			return (matchStatus == ProjectMatch.DontKnowYet ||
				matchStatus == ProjectMatch.WaitingForUserOrOtherFw ||
				matchStatus == ProjectMatch.SingleProcessMode);
		}

		/// ------------------------------------------------------------------------------------
		public bool IsMyProject(string name, string server)
		{
			var matchStatus = FieldWorks.GetProjectMatchStatus(new ProjectId(name, server));
			return (matchStatus == ProjectMatch.ItsMyProject);
		}

		/// ------------------------------------------------------------------------------------
		public string GetWritingSystems()
		{
			return PaWritingSystem.GetWritingSystemsAsXml(FieldWorks.Cache.ServiceLocator);
		}

		/// ------------------------------------------------------------------------------------
		public string GetLexEntries()
		{
			return PaLexEntry.GetAllAsXml(FieldWorks.Cache.ServiceLocator);
		}

		/// ------------------------------------------------------------------------------------
		public void ExitProcess()
		{
			System.Windows.Forms.Application.Exit();
		}
	}
}
