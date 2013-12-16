// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: HelpTopicProviderBase.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using XCore;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class HelpTopicProviderBase : IHelpTopicProvider
	{
		#region IHelpTopicProvider implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a URL identifying a Help topic.
		/// </summary>
		/// <param name="stid">An identifier for the desired Help topic</param>
		/// <returns>The requested string</returns>
		/// ------------------------------------------------------------------------------------
		public virtual string GetHelpString(string stid)
		{
			return ResourceHelper.GetHelpString(stid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The HTML help file (.chm) for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string HelpFile
		{
			get
			{
				return DirectoryFinder.FWCodeDirectory + GetHelpString("UserHelpFile");
			}
		}
		#endregion
	}
}
