//---------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002-2008, SIL International. All Rights Reserved.
// <copyright from='2002' to='2008' company='SIL International'>
//    Copyright (c) 2008, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: FwRegistrySettings.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides a means to store in and retrieve from the registry misc. fieldworks settings.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class FwRegistrySettings
	{
		private static RegistryBoolSetting s_firstTimeAppHasBeenRun =
			new RegistryBoolSetting(FwApp.App.SettingsKey, "FirstTime", true);

		private static RegistryBoolSetting s_successfulStartup =
			new RegistryBoolSetting(FwApp.App.SettingsKey, "OpenSuccessful", true);

		private static RegistryBoolSetting s_showSideBar =
			new RegistryBoolSetting(FwApp.App.SettingsKey, "ShowSideBar", true);

		private static RegistryBoolSetting s_showStatusBar =
			new RegistryBoolSetting(FwApp.App.SettingsKey, "ShowStatusBar", true);

		private static RegistryStringSetting s_backupDirectory =
			new RegistryStringSetting(FwSubKey.ProjectBackup + @"\Basics", "DefaultBackupDirectory",
			Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\My FieldWorks\Backups");

		// Data Notebook has the MeasurementUnits setting in the Data Notebook registry
		//  folder rather than in the folder for general FieldWorks settings.
		//  The MeasurementUnits setting should be set for all FieldWorks applications,
		//  not just in the individual applications.
		private static RegistryIntSetting s_MeasurementUnitSetting =
			new RegistryIntSetting(FwSubKey.FW, "MeasurementSystem", (int)MsrSysType.Cm);

		// This affects all FieldWorks apps.
		private static RegistryBoolSetting s_disableSplashScreen =
			new RegistryBoolSetting(FwSubKey.FW, "DisableSplashScreen", false);

		private static RegistryIntSetting s_numberOfLaunches =
			new RegistryIntSetting(FwApp.App.SettingsKey, "launches", 0);

		private static RegistryIntSetting s_numberOfSeriousCrashes =
			new RegistryIntSetting(FwApp.App.SettingsKey, "NumberOfSeriousCrashes", 0);

		private static RegistryIntSetting s_numberOfAnnoyingCrashes =
			new RegistryIntSetting(FwApp.App.SettingsKey, "NumberOfAnnoyingCrashes", 0);

		private static RegistryIntSetting s_totalAppRuntime =
			new RegistryIntSetting(FwApp.App.SettingsKey, "TotalAppRuntime", 0);

		private static RegistryStringSetting s_appStartupTime =
			new RegistryStringSetting(FwApp.App.SettingsKey, "LatestAppStartupTime", "");

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not there was an error opening a project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool StartupSuccessfulSetting
		{
			get {return s_successfulStartup.Value;}
			set {s_successfulStartup.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not this is the first time this app has
		/// ever been run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool FirstTimeAppHasBeenRun
		{
			get { return s_firstTimeAppHasBeenRun.Value; }
			set { s_firstTimeAppHasBeenRun.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of times this app has been run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int NumberOfLaunches
		{
			get { return s_numberOfLaunches.Value; }
			set { s_numberOfLaunches.Value = value; }
		}

		/// <summary>
		/// Get/set the number of serious (Green Dialog Box) crashes that have happened.
		/// </summary>
		public static int NumberOfSeriousCrashes
		{
			get { return s_numberOfSeriousCrashes.Value; }
			set { s_numberOfSeriousCrashes.Value = value; }
		}

		/// <summary>
		/// Get/set the number of annoying (Yellow Dialog Box) crashes that have happened.
		/// </summary>
		public static int NumberOfAnnoyingCrashes
		{
			get { return s_numberOfAnnoyingCrashes.Value; }
			set { s_numberOfAnnoyingCrashes.Value = value; }
		}

		/// <summary>
		/// Get/set the total number of seconds that the application has run on this computer.
		/// </summary>
		public static int TotalAppRuntime
		{
			get { return s_totalAppRuntime.Value; }
			set { s_totalAppRuntime.Value = value; }
		}

		/// <summary>
		/// Get/set the startup time (in ticks) for the current run of the application.
		/// </summary>
		public static string LatestAppStartupTime
		{
			get { return s_appStartupTime.Value; }
			set { s_appStartupTime.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value in the registry for the sidebar's visibility.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowSideBarSetting
		{
			get {return s_showSideBar.Value;}
			set {s_showSideBar.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value in the registry for the statusbar's visibility.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowStatusBarSetting
		{
			get {return s_showStatusBar.Value;}
			set {s_showStatusBar.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value in the registry for the splash screen enable flag.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool DisableSplashScreenSetting
		{
			get {return s_disableSplashScreen.Value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the value for the "Measurement Units" setting from the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int MeasurementUnitSetting
		{
			get	{return s_MeasurementUnitSetting.Value;}
			set	{s_MeasurementUnitSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the BackupDirectory setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string BackupDirectorySetting
		{
			get {
				if (s_backupDirectory.Value.IndexOf(@":\\", 0, s_backupDirectory.Value.Length) != -1)
				{
					// The "My Documents" folder is the root directory of a drive so it has two
					// backslash characters preceeded by a colon(":\\") from the initial default
					// default backup registry setting. Rebuild the BackupDirectorySetting without
					// the extra backslash.
					s_backupDirectory.Value =
						Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
						@"My FieldWorks\Backups\";
				}
				return s_backupDirectory.Value;
			}
			set {s_backupDirectory.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the options as properties of the error reporter so that they show up in a
		/// call stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void AddErrorReportingInfo()
		{
			ErrorReporter.AddProperty("FirstTimeAppHasBeenRun", FirstTimeAppHasBeenRun.ToString());
			int cLaunches = NumberOfLaunches + 1;	// this is stored before it's incremented.
			ErrorReporter.AddProperty("NumberOfLaunches", cLaunches.ToString());
			int cmin = TotalAppRuntime / 60;
			ErrorReporter.AddProperty("TotalRuntime", String.Format("{0}:{1}", cmin / 60, cmin % 60));
			ErrorReporter.AddProperty("NumberOfSeriousCrashes", NumberOfSeriousCrashes.ToString());
			ErrorReporter.AddProperty("NumberOfAnnoyingCrashes", NumberOfAnnoyingCrashes.ToString());
			ErrorReporter.AddProperty("RuntimeBeforeCrash", "0:00");
			ErrorReporter.AddProperty("StartupSuccessfulSetting", StartupSuccessfulSetting.ToString());
			ErrorReporter.AddProperty("ShowSideBarSetting", ShowSideBarSetting.ToString());
			ErrorReporter.AddProperty("ShowStatusBarSetting", ShowStatusBarSetting.ToString());
			ErrorReporter.AddProperty("DisableSplashScreenSetting", DisableSplashScreenSetting.ToString());
			ErrorReporter.AddProperty("MeasurementUnitSetting", ((MsrSysType)MeasurementUnitSetting).ToString());
			ErrorReporter.AddProperty("BackupDirectorySetting", BackupDirectorySetting);
		}
	}
}
