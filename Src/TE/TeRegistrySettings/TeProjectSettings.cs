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
using System;
using Microsoft.Win32;
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
		#region TeProjectSettingsImpl class
		// ReSharper disable MemberHidesStaticFromOuterClass

		private sealed class TeProjectSettingsImpl : IDisposable
		{
			/// <summary/>
			public RegistryBoolSetting SendSyncMessages { get; private set; }
			/// <summary/>
			public RegistryBoolSetting ReceiveSyncMessages { get; private set; }
			/// <summary/>
			public RegistryBoolSetting ShowSpellingErrors { get; private set; }
			/// <summary/>
			public RegistryBoolSetting ChangeAllBtWs { get; private set; }
			/// <summary/>
			public RegistryBoolSetting BookFilterEnabled { get; private set; }
			/// <summary/>
			public RegistryBoolSetting ShowUsfmResources { get; private set; }
			/// <summary/>
			public RegistryStringSetting FiltersKey { get; private set; }

			#region Public methods

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Loads the project settings from the specified registry key.
			/// </summary>
			/// <param name="key">The registry key.</param>
			/// ------------------------------------------------------------------------------------
			public void InitSettings(RegistryKey key)
			{
				if (key == null)
					throw new ArgumentNullException("key");

				if (SendSyncMessages != null)
					DisposeVariables();

				SendSyncMessages = new RegistryBoolSetting(key, "SendSyncMessage", true);
				ReceiveSyncMessages = new RegistryBoolSetting(key, "ReceiveSyncMessage", false);
				ShowSpellingErrors = new RegistryBoolSetting(key, "ShowSpellingErrors", false);
				ChangeAllBtWs = new RegistryBoolSetting(key, "ChangeAllBtViews", true);
				BookFilterEnabled = new RegistryBoolSetting(key, "BookFilterEnabled", false);
				ShowUsfmResources = new RegistryBoolSetting(key, "ShowUsfmResources", false);
				FiltersKey = new RegistryStringSetting(key, "BookFilterBooks", string.Empty);
			}
			#endregion

			#region IDisposable Members
			#if DEBUG
			/// <summary/>
			~TeProjectSettingsImpl()
			{
				Dispose(false);
			}
			#endif

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Frees all registry keys/settings.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
				if (fDisposing)
				{
					DisposeVariables();
				}
				SendSyncMessages = null;
				ReceiveSyncMessages = null;
				ShowSpellingErrors = null;
				ChangeAllBtWs = null;
				BookFilterEnabled = null;
				ShowUsfmResources = null;
				FiltersKey = null;
			}

			private void DisposeVariables()
			{
				if (SendSyncMessages != null)
					SendSyncMessages.Dispose();
				if (ReceiveSyncMessages != null)
					ReceiveSyncMessages.Dispose();
				if (ShowSpellingErrors != null)
					ShowSpellingErrors.Dispose();
				if (ChangeAllBtWs != null)
					ChangeAllBtWs.Dispose();
				if (BookFilterEnabled != null)
					BookFilterEnabled.Dispose();
				if (ShowUsfmResources != null)
					ShowUsfmResources.Dispose();
				if (FiltersKey != null)
					FiltersKey.Dispose();
			}

			#endregion
		}
		// ReSharper restore MemberHidesStaticFromOuterClass
		#endregion // TeProjectSettingsImpl

		#region Properties
		private static TeProjectSettingsImpl Instance
		{
			get
			{
				return SingletonsContainer.Get<TeProjectSettingsImpl>();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to send sync messages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool SendSyncMessages
		{
			get { return Instance.SendSyncMessages.Value; }
			set { Instance.SendSyncMessages.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to receive sync messages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ReceiveSyncMessages
		{
			get { return Instance.ReceiveSyncMessages.Value; }
			set { Instance.ReceiveSyncMessages.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether spelling errors should be displayed for
		/// all TE windows for this project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowSpellingErrors
		{
			get { return Instance.ShowSpellingErrors.Value; }
			set { Instance.ShowSpellingErrors.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to change all back translation views to
		/// a selected writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ChangeAllBtWs
		{
			get { return Instance.ChangeAllBtWs.Value; }
			set { Instance.ChangeAllBtWs.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the book filter is enabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool BookFilterEnabled
		{
			get { return Instance.BookFilterEnabled.Value; }
			set { Instance.BookFilterEnabled.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the USFM resource pane
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowUsfmResources
		{
			get { return Instance.ShowUsfmResources.Value; }
			set { Instance.ShowUsfmResources.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list of books in the book filter as a comma seperated list of guids
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string BookFilterBooks
		{
			get { return Instance.FiltersKey.Value; }
			set { Instance.FiltersKey.Value = value; }
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

			Instance.InitSettings(key);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Release the project settings.
		/// NOTE: This method should only be used in tests!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Release()
		{
			Instance.Dispose();
			SingletonsContainer.Remove(Instance);
		}
		#endregion
	}
}
