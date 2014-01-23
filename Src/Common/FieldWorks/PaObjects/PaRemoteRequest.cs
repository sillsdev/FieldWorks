// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
