// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
