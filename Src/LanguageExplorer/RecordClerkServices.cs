// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Works;

namespace LanguageExplorer
{
	/// <summary>
	/// Helper class for setting values in various panels in the main status bar on the main window.
	/// </summary>
	internal static class RecordClerkServices
	{
		internal static void SetClerk(MajorFlexComponentParameters majorFlexComponentParameters, RecordClerk clerk)
		{
			majorFlexComponentParameters.DataNavigationManager.Clerk = clerk;
			majorFlexComponentParameters.ParserMenuManager.Clerk = clerk;
			majorFlexComponentParameters.RecordClerkRepositoryForTools.ActiveRecordClerk = clerk;
		}

		internal static void ClearClerk(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			majorFlexComponentParameters.DataNavigationManager.Clerk = null;
			majorFlexComponentParameters.ParserMenuManager.Clerk = null;
			majorFlexComponentParameters.RecordClerkRepositoryForTools.ActiveRecordClerk = null;
		}
	}
}