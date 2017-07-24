// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.XWorks;

namespace LanguageExplorer
{
	/// <summary>
	/// Helper class for setting values in various panels in the main status bar on the main window.
	/// </summary>
	internal static class RecordClerkServices
	{
		internal static void SetClerk(DataNavigationManager dataNavigationManager, IRecordClerkRepository recordClerkRepository, RecordClerk clerk)
		{
			dataNavigationManager.Clerk = clerk;
			recordClerkRepository.ActiveRecordClerk = clerk;
		}

		internal static void ClearClerk(DataNavigationManager dataNavigationManager, IRecordClerkRepository recordClerkRepository)
		{
			dataNavigationManager.Clerk = null;
			recordClerkRepository.ActiveRecordClerk = null;
		}
	}
}