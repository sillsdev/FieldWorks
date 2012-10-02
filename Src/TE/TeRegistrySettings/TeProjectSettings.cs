// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2005' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeProjectSettings.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using Microsoft.Win32;
using System;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Store the project-specific settings used in TeEditingHelper.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class TeProjectSettings
	{
		private static RegistryBoolSetting s_sendSyncMessages;
		private static RegistryBoolSetting s_receiveSyncMessages;
		private static RegistryBoolSetting s_showSpellingErrors;
		private static RegistryBoolSetting s_changeAllBtWs;
		private static RegistryBoolSetting s_bookFilterEnabled;
		private static RegistryBoolSetting s_showUsfmResources;
		private static RegistryStringSetting s_filtersKey;

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to send sync messages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool SendSyncMessages
		{
			get { return s_sendSyncMessages.Value; }
			set { s_sendSyncMessages.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to receive sync messages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ReceiveSyncMessages
		{
			get { return s_receiveSyncMessages.Value; }
			set { s_receiveSyncMessages.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether spelling errors should be displayed for
		/// all TE windows for this project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowSpellingErrors
		{
			get { return s_showSpellingErrors.Value; }
			set { s_showSpellingErrors.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to change all back translation views to
		/// a selected writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ChangeAllBtWs
		{
			get { return s_changeAllBtWs.Value; }
			set { s_changeAllBtWs.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the book filter is enabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool BookFilterEnabled
		{
			get { return s_bookFilterEnabled.Value; }
			set { s_bookFilterEnabled.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the USFM resource pane
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowUsfmResources
		{
			get { return s_showUsfmResources.Value; }
			set { s_showUsfmResources.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list of books in the book filter as a comma seperated list of guids
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string BookFilterBooks
		{
			get { return s_filtersKey.Value; }
			set { s_filtersKey.Value = value; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the project settings.
		/// </summary>
		/// <param name="settings">Initialization settings.</param>
		/// ------------------------------------------------------------------------------------
		public static void InitSettings(IProjectSpecificSettingsKeyProvider settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");
			InitSettings(settings.ProjectSpecificSettingsKey);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the project settings from the specified registry key.
		/// NOTE: This overload should only be used in tests!
		/// </summary>
		/// <param name="key">The registry key.</param>
		/// ------------------------------------------------------------------------------------
		public static void InitSettings(RegistryKey key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			System.Diagnostics.Debug.Assert(s_sendSyncMessages == null, "TeProjectSettings.InitSettings was called before!");
			s_sendSyncMessages = new RegistryBoolSetting(key, "SendSyncMessage", true);
			s_receiveSyncMessages = new RegistryBoolSetting(key, "ReceiveSyncMessage", false);
			s_showSpellingErrors = new RegistryBoolSetting(key, "ShowSpellingErrors", false);
			s_changeAllBtWs = new RegistryBoolSetting(key, "ChangeAllBtViews", true);
			s_bookFilterEnabled = new RegistryBoolSetting(key, "BookFilterEnabled", false);
			s_showUsfmResources = new RegistryBoolSetting(key, "ShowUsfmResources", false);
			s_filtersKey = new RegistryStringSetting(key, "BookFilterBooks", string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Release the project settings.
		/// NOTE: This method should only be used in tests!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Release()
		{
			if (s_sendSyncMessages != null)
				s_sendSyncMessages.Dispose();
			s_sendSyncMessages = null;
			if (s_receiveSyncMessages != null)
				s_receiveSyncMessages.Dispose();
			s_receiveSyncMessages = null;
			if (s_showSpellingErrors != null)
				s_showSpellingErrors.Dispose();
			s_showSpellingErrors = null;
			if (s_changeAllBtWs != null)
				s_changeAllBtWs.Dispose();
			s_changeAllBtWs = null;
			if (s_bookFilterEnabled != null)
				s_bookFilterEnabled.Dispose();
			s_bookFilterEnabled = null;
			if (s_showUsfmResources != null)
				s_showUsfmResources.Dispose();
			s_showUsfmResources = null;
			if (s_filtersKey != null)
				s_filtersKey.Dispose();
			s_filtersKey = null;
		}
		#endregion
	}
}