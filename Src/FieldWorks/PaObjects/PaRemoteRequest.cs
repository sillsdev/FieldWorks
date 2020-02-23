// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaRemoteRequest : RemoteRequest
	{
		/// <summary>
		/// Determines whether [is same project] [the specified name].
		/// </summary>
		public bool ShouldWait(string name, string server)
		{
			var matchStatus = FieldWorks.GetProjectMatchStatus(new ProjectId(name));
			return matchStatus == ProjectMatch.DontKnowYet
				   || matchStatus == ProjectMatch.WaitingForUserOrOtherFw
				   || matchStatus == ProjectMatch.SingleProcessMode;
		}

		/// <summary />
		public bool IsMyProject(string name, string server) => FieldWorks.GetProjectMatchStatus(new ProjectId(name)) == ProjectMatch.ItsMyProject;

		/// <summary />
		public string GetWritingSystems() => PaWritingSystem.GetWritingSystemsAsXml(FieldWorks.Cache.ServiceLocator);

		/// <summary />
		public string GetLexEntries() => PaLexEntry.GetAllAsXml(FieldWorks.Cache.ServiceLocator);

		/// <summary />
		public void ExitProcess()
		{
			Application.Exit();
		}
	}
}
