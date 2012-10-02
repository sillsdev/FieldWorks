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
// File: FlexHelpTopicProvider.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Reflection;
using System.Resources;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FLEx-specific HelpTopicProvider
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FlexHelpTopicProvider : HelpTopicProviderBase
	{
		private static ResourceManager s_helpResources = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a help file URL or topic
		/// </summary>
		/// <param name="stid"></param>
		/// <returns>The requested string</returns>
		/// ------------------------------------------------------------------------------------
		public override string GetHelpString(string stid)
		{
			if (s_helpResources == null)
			{
				s_helpResources = new ResourceManager("SIL.FieldWorks.XWorks.LexText.HelpTopicPaths",
					Assembly.GetExecutingAssembly());
			}

			if (stid == null)
				return "NullStringID";

			// First try to find it in our resource file. If that doesn't work, try the more general one
			return s_helpResources.GetString(stid) ?? base.GetHelpString(stid);
		}
	}
}
