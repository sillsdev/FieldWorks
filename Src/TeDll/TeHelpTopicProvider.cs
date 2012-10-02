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
// File: TeHelpTopicProvider.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using SIL.FieldWorks.Common.Framework;
namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TE-specific HelpTopicProvider
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeHelpTopicProvider : HelpTopicProviderBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a help file URL or topic
		/// </summary>
		/// <param name="stid"></param>
		/// <returns>The requested string</returns>
		/// ------------------------------------------------------------------------------------
		public override string GetHelpString(string stid)
		{
			// First check if the stid starts with the marker that tells us the user is wanting
			// help on a particular style displayed in the styles combo box. If so, then find
			// the correct URL for the help topic of that style's example.
			const string kStylePrefix = "style:";
			if (stid.StartsWith(kStylePrefix))
				return TeStylesXmlAccessor.GetHelpTopicForStyle(stid.Substring(kStylePrefix.Length));
			if (stid.StartsWith("khtpScrChecks_"))
				return TeResourceHelper.GetHelpString(stid) ?? base.GetHelpString("khtpScrChecksUndocumented");

			return TeResourceHelper.GetHelpString(stid) ?? base.GetHelpString(stid);
		}
	}
}
