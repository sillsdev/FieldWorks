// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Options.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides a means to store in and retrieve from the registry misc. TE settings.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class Options
	{
		/// <summary>Display "these styles"</summary>
		public enum ShowTheseStyles
		{
			/// <summary>All styles will be shown</summary>
			All,
			/// <summary>Only basic styles will be shown</summary>
			Basic,
			/// <summary>A custom set of styles will be shown</summary>
			Custom
		};

		/// <summary>Display style levels</summary>
		public enum StyleLevel
		{
			/// <summary>basic styles will be shown</summary>
			Basic,
			/// <summary>intermediate styles will be shown</summary>
			Intermediate,
			/// <summary>advanced styles will be shown</summary>
			Advanced,
			/// <summary>expert styles will be shown</summary>
			Expert
		};

		// display stuff options
		private static RegistryBoolSetting s_ShowMarkerlessIconsSetting =
			new RegistryBoolSetting(FwSubKey.TE, "FootnoteShowMarkerlessIcons", true);
		private static RegistryBoolSetting s_ShowEmptyParagraphPromptsSetting =
			new RegistryBoolSetting(FwSubKey.TE, "ShowEmptyParagraphPrompts", true);
		private static RegistryBoolSetting s_ShowFormatMarksSetting =
			new RegistryBoolSetting(FwSubKey.TE, "ShowFormatMarks", false);
		private static RegistryBoolSetting s_ShowImportBackupSetting =
			new RegistryBoolSetting(FwSubKey.TE, "ShowImportBackup", true);
		// REVIEW: What should the default value be for pasting?
		private static RegistryBoolSetting s_ShowPasteWsChoice =
			new RegistryBoolSetting(FwSubKey.TE, "ShowPasteWsChoice", false);

		// locale options
		private static RegistryStringSetting s_UserInterfaceLanguage =
			new RegistryStringSetting(FwSubKey.TE, "UserWs",
			MiscUtils.CurrentUIClutureICU);

		// testing options
		//private static RegistryBoolSetting s_UseEnableSendReceiveSyncMsgs =
		//    new RegistryBoolSetting(FwSubKey.TE, "UseSendReceiveSyncMsgs", false);
		private static RegistryBoolSetting s_UseVerticalDraftView =
			new RegistryBoolSetting(FwSubKey.TE, "UseVerticalDraftView", false);

		// Experimental feature options
		private static RegistryBoolSetting s_UseInterlinearBackTranslation =
			new RegistryBoolSetting(FwSubKey.TE, "UseInterlinearBackTranslation", false);
		private static RegistryBoolSetting s_UseXhtmlExport =
			new RegistryBoolSetting(FwSubKey.TE, "UseXhtmlExport", false);

		// footnote display options
		private static RegistryBoolSetting s_FootnoteSynchronousScrollingSetting =
			new RegistryBoolSetting(FwSubKey.TE, "FootnoteSynchronousScrolling", true);

		// display style options
		private static RegistryStringSetting s_ShowTheseStylesSetting =
			new RegistryStringSetting(FwSubKey.TE, "ShowTheseStyles", "all");
		private static RegistryStringSetting s_ShowStyleLevelSetting =
			new RegistryStringSetting(FwSubKey.TE, "ShowStyleLevel",
			DlgResources.ResourceString("kstidStyleLevelBasic"));
		private static RegistryBoolSetting s_ShowUserDefinedStylesSetting =
			new RegistryBoolSetting(FwSubKey.TE, "ShowUserDefinedStyles", true);

		#region Tools options settings (Properties)
		// This group of properties is used to access all of the options that are set in the
		// Tools/Options dialog.  Any code outside of the tools/options dialog can determine
		// what the values are or change them through these properties.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the UserWs setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string UserInterfaceWritingSystem
		{
			get { return s_UserInterfaceLanguage.Value; }
			set { s_UserInterfaceLanguage.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the ShowUserDefinedStyles setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowUserDefinedStylesSetting
		{
			get {return s_ShowUserDefinedStylesSetting.Value;}
			set {s_ShowUserDefinedStylesSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the ShowFormatMarks setting. Format marks
		/// are end of paragraph and end of StText markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowFormatMarksSetting
		{
			get {return s_ShowFormatMarksSetting.Value; }
			set {s_ShowFormatMarksSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the value for the "Show Import Backup Reminder" setting from the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowImportBackupSetting
		{
			get	{return s_ShowImportBackupSetting.Value;}
			set	{s_ShowImportBackupSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the "Enable writing system choice for paste" setting from
		/// the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowPasteWsChoice
		{
			get { return s_ShowPasteWsChoice.Value; }
			set { s_ShowPasteWsChoice.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or sets the value for the "Show Markerless Footnote Icons" setting from
		/// the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowMarkerlessIconsSetting
		{
			get	{return s_ShowMarkerlessIconsSetting.Value;}
			set	{s_ShowMarkerlessIconsSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the setting value for the "Synchronous Footnote Scrolling" setting
		/// in the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool FootnoteSynchronousScrollingSetting
		{
			get	{return s_FootnoteSynchronousScrollingSetting.Value;}
			set	{s_FootnoteSynchronousScrollingSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value indicating whether the vertical draft view should be
		/// enabled. Currently this is determined by the exprimental features control in the
		/// advanced tab of the options dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool UseVerticalDraftView
		{
			get { return s_UseVerticalDraftView.Value; }
			set { s_UseVerticalDraftView.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value indicating whether the interlinear back translation feature
		/// should be used. Currently this is determined by the exprimental features control in the
		/// advanced tab of the options dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool UseInterlinearBackTranslation
		{
			get { return s_UseInterlinearBackTranslation.Value; }
			set { s_UseInterlinearBackTranslation.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value indicating whether the XHTML Export feature should be used.
		/// Currently this is determined by the exprimental features control in the
		/// advanced tab of the options dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool UseXhtmlExport
		{
			get { return s_UseXhtmlExport.Value; }
			set { s_UseXhtmlExport.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the "Show Empty Paragraph Prompts" setting
		/// in the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowEmptyParagraphPromptsSetting
		{
			get {return s_ShowEmptyParagraphPromptsSetting.Value;}
			set	{s_ShowEmptyParagraphPromptsSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the ShowTheseStyles setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Options.ShowTheseStyles ShowTheseStylesSetting
		{
			get
			{
				string s = s_ShowTheseStylesSetting.Value;
				if (s == "basic")
					return Options.ShowTheseStyles.Basic;
				if (s == "custom")
					return Options.ShowTheseStyles.Custom;
				return Options.ShowTheseStyles.All;
			}
			set
			{
				switch (value)
				{
					case Options.ShowTheseStyles.All:
						s_ShowTheseStylesSetting.Value = "all";
						break;
					case Options.ShowTheseStyles.Basic:
						s_ShowTheseStylesSetting.Value = "basic";
						break;
					case Options.ShowTheseStyles.Custom:
						s_ShowTheseStylesSetting.Value = "custom";
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the ShowStyleLevel setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Options.StyleLevel ShowStyleLevelSetting
		{
			get
			{
				switch (s_ShowStyleLevelSetting.Value)
				{
					case "intermediate":
						return Options.StyleLevel.Intermediate;
					case "advanced":
						return Options.StyleLevel.Advanced;
					case "expert":
						return Options.StyleLevel.Expert;
					case "basic":
					default:
						return Options.StyleLevel.Basic;
				}
			}
			set
			{
				switch (value)
				{
					case Options.StyleLevel.Basic:
						s_ShowStyleLevelSetting.Value = "basic";
						break;
					case Options.StyleLevel.Intermediate:
						s_ShowStyleLevelSetting.Value = "intermediate";
						break;
					case Options.StyleLevel.Advanced:
						s_ShowStyleLevelSetting.Value = "advanced";
						break;
					case Options.StyleLevel.Expert:
						s_ShowStyleLevelSetting.Value = "expert";
						break;
				}
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the options as properties of the error reporter so that they show up in a
		/// call stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void AddErrorReportingInfo()
		{
			ErrorReporter.AddProperty("UserInterfaceWritingSystem", UserInterfaceWritingSystem);
			ErrorReporter.AddProperty("ShowUserDefinedStylesSetting", ShowUserDefinedStylesSetting.ToString());
			ErrorReporter.AddProperty("ShowFormatMarksSetting", ShowFormatMarksSetting.ToString());
			ErrorReporter.AddProperty("ShowImportBackupSetting", ShowImportBackupSetting.ToString());
			ErrorReporter.AddProperty("ShowMarkerlessIconsSetting", ShowMarkerlessIconsSetting.ToString());
			ErrorReporter.AddProperty("FootnoteSynchronousScrollingSetting", FootnoteSynchronousScrollingSetting.ToString());
			ErrorReporter.AddProperty("UseVerticalDraftView", UseVerticalDraftView.ToString());
			ErrorReporter.AddProperty("UseInterlinearBackTranslation", UseInterlinearBackTranslation.ToString());
			ErrorReporter.AddProperty("ShowEmptyParagraphPromptsSetting", ShowEmptyParagraphPromptsSetting.ToString());
			ErrorReporter.AddProperty("ShowTheseStylesSetting", ShowTheseStylesSetting.ToString());
			ErrorReporter.AddProperty("ShowStyleLevelSetting", ShowStyleLevelSetting.ToString());
		}
	}
}
