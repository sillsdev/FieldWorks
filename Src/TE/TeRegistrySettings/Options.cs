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
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using Microsoft.Win32;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides a means to store in and retrieve from the registry misc. TE settings.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class Options
	{
		#region Enumerations
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
		#endregion

		#region Variables
		private static RegistryKey s_teRegistryKey;

		// display stuff options
		private static RegistryBoolSetting s_showMarkerlessIconsSetting;
		private static RegistryBoolSetting s_showEmptyParagraphPromptsSetting;
		private static RegistryBoolSetting s_showFormatMarksSetting;

		// locale options
		private static RegistryStringSetting s_userInterfaceLanguage;

		// testing options
		//private static RegistryBoolSetting s_UseEnableSendReceiveSyncMsgs;
		private static RegistryBoolSetting s_useVerticalDraftView;

		// Experimental feature options
		private static RegistryBoolSetting s_useInterlinearBackTranslation;
		private static RegistryBoolSetting s_useXhtmlExport;

		// footnote display options
		private static RegistryBoolSetting s_footnoteSynchronousScrollingSetting;

		// display style options
		private static RegistryStringSetting s_showTheseStylesSetting;
		private static RegistryStringSetting s_showStyleLevelSetting;
		private static RegistryBoolSetting s_showUserDefinedStylesSetting;

		// Other
		private static RegistryBoolSetting s_autoStartLibronix;
		#endregion

		#region Static constructor
		static Options()
		{
			Init();
		}
		#endregion

		#region Tools options settings (Properties)
		// This group of properties is used to access all of the options that are set in the
		// Tools/Options dialog.  Any code outside of the tools/options dialog can determine
		// what the values are or change them through these properties.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to start Libronix when TE starts or not.
		/// On Linux this always returns false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool AutoStartLibronix
		{
			get { return s_autoStartLibronix.Value && !MiscUtils.IsUnix; }
			set { s_autoStartLibronix.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the UserWs setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string UserInterfaceWritingSystem
		{
			get { return s_userInterfaceLanguage.Value; }
			set { s_userInterfaceLanguage.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the ShowUserDefinedStyles setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowUserDefinedStylesSetting
		{
			get {return s_showUserDefinedStylesSetting.Value;}
			set {s_showUserDefinedStylesSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the ShowFormatMarks setting. Format marks
		/// are end of paragraph and end of StText markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowFormatMarksSetting
		{
			get {return s_showFormatMarksSetting.Value; }
			set {s_showFormatMarksSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or sets the value for the "Show Markerless Footnote Icons" setting from
		/// the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowMarkerlessIconsSetting
		{
			get	{return s_showMarkerlessIconsSetting.Value;}
			set	{s_showMarkerlessIconsSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the setting value for the "Synchronous Footnote Scrolling" setting
		/// in the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool FootnoteSynchronousScrollingSetting
		{
			get	{return s_footnoteSynchronousScrollingSetting.Value;}
			set	{s_footnoteSynchronousScrollingSetting.Value = value;}
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
			get { return s_useVerticalDraftView.Value; }
			set { s_useVerticalDraftView.Value = value; }
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
			get { return s_useInterlinearBackTranslation.Value; }
			set { s_useInterlinearBackTranslation.Value = value; }
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
			get { return s_useXhtmlExport.Value; }
			set { s_useXhtmlExport.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the "Show Empty Paragraph Prompts" setting
		/// in the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowEmptyParagraphPromptsSetting
		{
			get {return s_showEmptyParagraphPromptsSetting.Value;}
			set	{s_showEmptyParagraphPromptsSetting.Value = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the ShowTheseStyles setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ShowTheseStyles ShowTheseStylesSetting
		{
			get
			{
				string s = s_showTheseStylesSetting.Value;
				if (s == "basic")
					return ShowTheseStyles.Basic;
				if (s == "custom")
					return ShowTheseStyles.Custom;
				return ShowTheseStyles.All;
			}
			set
			{
				switch (value)
				{
					case ShowTheseStyles.All:
						s_showTheseStylesSetting.Value = "all";
						break;
					case ShowTheseStyles.Basic:
						s_showTheseStylesSetting.Value = "basic";
						break;
					case ShowTheseStyles.Custom:
						s_showTheseStylesSetting.Value = "custom";
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the ShowStyleLevel setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static StyleLevel ShowStyleLevelSetting
		{
			get
			{
				switch (s_showStyleLevelSetting.Value)
				{
					case "intermediate":
						return StyleLevel.Intermediate;
					case "advanced":
						return StyleLevel.Advanced;
					case "expert":
						return StyleLevel.Expert;
					default:
						return StyleLevel.Basic;
				}
			}
			set
			{
				switch (value)
				{
					case StyleLevel.Basic:
						s_showStyleLevelSetting.Value = "basic";
						break;
					case StyleLevel.Intermediate:
						s_showStyleLevelSetting.Value = "intermediate";
						break;
					case StyleLevel.Advanced:
						s_showStyleLevelSetting.Value = "advanced";
						break;
					case StyleLevel.Expert:
						s_showStyleLevelSetting.Value = "expert";
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
			ErrorReporter.AddProperty("ShowMarkerlessIconsSetting", ShowMarkerlessIconsSetting.ToString());
			ErrorReporter.AddProperty("FootnoteSynchronousScrollingSetting", FootnoteSynchronousScrollingSetting.ToString());
			ErrorReporter.AddProperty("UseVerticalDraftView", UseVerticalDraftView.ToString());
			ErrorReporter.AddProperty("UseInterlinearBackTranslation", UseInterlinearBackTranslation.ToString());
			ErrorReporter.AddProperty("ShowEmptyParagraphPromptsSetting", ShowEmptyParagraphPromptsSetting.ToString());
			ErrorReporter.AddProperty("ShowTheseStylesSetting", ShowTheseStylesSetting.ToString());
			ErrorReporter.AddProperty("ShowStyleLevelSetting", ShowStyleLevelSetting.ToString());
		}

		/// <summary>
		/// Initializes the variables.
		/// NOTE: this method should be directly called only be unit tests.
		/// </summary>
		public static void Init()
		{
			if (s_teRegistryKey != null)
				return;

			s_teRegistryKey = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey(FwSubKey.TE);
			s_showMarkerlessIconsSetting = new RegistryBoolSetting(s_teRegistryKey, "FootnoteShowMarkerlessIcons", true);
			s_showEmptyParagraphPromptsSetting = new RegistryBoolSetting(s_teRegistryKey, "ShowEmptyParagraphPrompts", true);
			s_showFormatMarksSetting = new RegistryBoolSetting(s_teRegistryKey, "ShowFormatMarks", false);
			s_userInterfaceLanguage = new RegistryStringSetting(FwRegistryHelper.FieldWorksRegistryKey,
					FwRegistryHelper.UserLocaleValueName, MiscUtils.CurrentUICulture);
			s_useVerticalDraftView = new RegistryBoolSetting(s_teRegistryKey, "UseVerticalDraftView", false);
			s_useInterlinearBackTranslation = new RegistryBoolSetting(s_teRegistryKey, "UseInterlinearBackTranslation", false);
			s_useXhtmlExport = new RegistryBoolSetting(s_teRegistryKey, "UseXhtmlExport", false);
			s_footnoteSynchronousScrollingSetting = new RegistryBoolSetting(s_teRegistryKey, "FootnoteSynchronousScrolling", true);
			s_showTheseStylesSetting = new RegistryStringSetting(s_teRegistryKey, "ShowTheseStyles", "all");
			s_showStyleLevelSetting = new RegistryStringSetting(s_teRegistryKey, "ShowStyleLevel", DlgResources.ResourceString("kstidStyleLevelBasic"));
			s_showUserDefinedStylesSetting = new RegistryBoolSetting(s_teRegistryKey, "ShowUserDefinedStyles", true);
			s_autoStartLibronix = new RegistryBoolSetting(s_teRegistryKey, "AutoStartLibronix", false);
			//s_UseEnableSendReceiveSyncMsgs = new RegistryBoolSetting(FwSubKey.TE, "UseSendReceiveSyncMsgs", false);
		}

		/// <summary>
		/// Frees all registry keys/settings.
		/// NOTE: This method should only be called by unit tests.
		/// </summary>
		public static void Release()
		{
			var disposable = s_teRegistryKey as IDisposable;
			if (disposable != null)
				disposable.Dispose();
			s_teRegistryKey = null;
			if (s_showMarkerlessIconsSetting != null)
				s_showMarkerlessIconsSetting.Dispose();
			s_showMarkerlessIconsSetting = null;
			if (s_showEmptyParagraphPromptsSetting != null)
				s_showEmptyParagraphPromptsSetting.Dispose();
			s_showEmptyParagraphPromptsSetting = null;
			if (s_showFormatMarksSetting != null)
				s_showFormatMarksSetting.Dispose();
			s_showFormatMarksSetting = null;
			if (s_userInterfaceLanguage != null)
				s_userInterfaceLanguage.Dispose();
			s_userInterfaceLanguage = null;
			if (s_useVerticalDraftView != null)
				s_useVerticalDraftView.Dispose();
			s_useVerticalDraftView = null;
			if (s_useInterlinearBackTranslation != null)
				s_useInterlinearBackTranslation.Dispose();
			s_useInterlinearBackTranslation = null;
			if (s_useXhtmlExport != null)
				s_useXhtmlExport.Dispose();
			s_useXhtmlExport = null;
			if (s_footnoteSynchronousScrollingSetting != null)
				s_footnoteSynchronousScrollingSetting.Dispose();
			s_footnoteSynchronousScrollingSetting = null;
			if (s_showTheseStylesSetting != null)
				s_showTheseStylesSetting.Dispose();
			s_showTheseStylesSetting = null;
			if (s_showStyleLevelSetting != null)
				s_showStyleLevelSetting.Dispose();
			s_showStyleLevelSetting = null;
			if (s_showUserDefinedStylesSetting != null)
				s_showUserDefinedStylesSetting.Dispose();
			s_showUserDefinedStylesSetting = null;
			if (s_autoStartLibronix != null)
				s_autoStartLibronix.Dispose();
			s_autoStartLibronix = null;
			//if (s_UseEnableSendReceiveSyncMsgs != null)
			//    s_UseEnableSendReceiveSyncMsgs.Dispose();
			//s_UseEnableSendReceiveSyncMsgs = null;
		}
	}
}
