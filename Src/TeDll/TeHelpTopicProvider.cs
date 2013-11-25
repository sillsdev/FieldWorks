// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
