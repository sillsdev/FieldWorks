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
using Microsoft.Win32;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides a means to store in and retrieve from the registry misc. TE settings.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class Options
	{
		#region OptionsImpl class
		// ReSharper disable MemberHidesStaticFromOuterClass

		private sealed class OptionsImpl: IDisposable
		{
			#region Properties
			private RegistryKey TeRegistryKey { get; set; }

			// display stuff options
			/// <summary/>
			public RegistryBoolSetting ShowMarkerlessIconsSetting { get; private set; }
			/// <summary/>
			public RegistryBoolSetting ShowEmptyParagraphPromptsSetting { get; private set; }
			/// <summary/>
			public RegistryBoolSetting ShowFormatMarksSetting { get; private set; }

			// locale options
			/// <summary/>
			public RegistryStringSetting UserInterfaceLanguage { get; private set; }

			// testing options
			//public RegistryBoolSetting UseEnableSendReceiveSyncMsgs;
			/// <summary/>
			public RegistryBoolSetting UseVerticalDraftView { get; private set; }

			// Experimental feature options
			/// <summary/>
			public RegistryBoolSetting UseInterlinearBackTranslation { get; private set; }
			/// <summary/>
			public RegistryBoolSetting UseXhtmlExport { get; private set; }
			/// <summary/>
			public RegistryBoolSetting ShowTranslateUnsQuestions { get; private set; }

			// footnote display options
			/// <summary/>
			public RegistryBoolSetting FootnoteSynchronousScrollingSetting { get; private set; }

			// display style options
			/// <summary/>
			public RegistryStringSetting ShowTheseStylesSetting { get; private set; }
			/// <summary/>
			public RegistryStringSetting ShowStyleLevelSetting { get; private set; }
			/// <summary/>
			public RegistryBoolSetting ShowUserDefinedStylesSetting { get; private set; }

			// Other
			/// <summary/>
			public RegistryBoolSetting AutoStartLibronix { get; private set; }
			#endregion

			/// <summary>
			/// Initializes the variables.
			/// </summary>
			public void Init()
			{
				if (TeRegistryKey != null)
					return;

				TeRegistryKey = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey(FwSubKey.TE);
				ShowMarkerlessIconsSetting = new RegistryBoolSetting(TeRegistryKey, "FootnoteShowMarkerlessIcons", true);
				ShowEmptyParagraphPromptsSetting = new RegistryBoolSetting(TeRegistryKey, "ShowEmptyParagraphPrompts", true);
				ShowFormatMarksSetting = new RegistryBoolSetting(TeRegistryKey, "ShowFormatMarks", false);
				UserInterfaceLanguage = new RegistryStringSetting(FwRegistryHelper.FieldWorksRegistryKey,
						FwRegistryHelper.UserLocaleValueName, MiscUtils.CurrentUICulture);
				UseVerticalDraftView = new RegistryBoolSetting(TeRegistryKey, "UseVerticalDraftView", false);
				UseInterlinearBackTranslation = new RegistryBoolSetting(TeRegistryKey, "UseInterlinearBackTranslation", false);
				UseXhtmlExport = new RegistryBoolSetting(TeRegistryKey, "UseXhtmlExport", false);
				ShowTranslateUnsQuestions = new RegistryBoolSetting(TeRegistryKey, "ShowTranslateUnsQuestions", false);
				FootnoteSynchronousScrollingSetting = new RegistryBoolSetting(TeRegistryKey, "FootnoteSynchronousScrolling", true);
				ShowTheseStylesSetting = new RegistryStringSetting(TeRegistryKey, "ShowTheseStyles", "all");
				ShowStyleLevelSetting = new RegistryStringSetting(TeRegistryKey, "ShowStyleLevel", DlgResources.ResourceString("kstidStyleLevelBasic"));
				ShowUserDefinedStylesSetting = new RegistryBoolSetting(TeRegistryKey, "ShowUserDefinedStyles", true);
				AutoStartLibronix = new RegistryBoolSetting(TeRegistryKey, "AutoStartLibronix", false);
				//UseEnableSendReceiveSyncMsgs = new RegistryBoolSetting(FwSubKey.TE, "UseSendReceiveSyncMsgs", false);
			}

			#region IDisposable Members
			#if DEBUG
			/// <summary/>
			~OptionsImpl()
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
					var disposable = TeRegistryKey as IDisposable;
					if (disposable != null)
						disposable.Dispose();
					if (ShowMarkerlessIconsSetting != null)
						ShowMarkerlessIconsSetting.Dispose();
					if (ShowEmptyParagraphPromptsSetting != null)
						ShowEmptyParagraphPromptsSetting.Dispose();
					if (ShowFormatMarksSetting != null)
						ShowFormatMarksSetting.Dispose();
					if (UserInterfaceLanguage != null)
						UserInterfaceLanguage.Dispose();
					if (UseVerticalDraftView != null)
						UseVerticalDraftView.Dispose();
					if (UseInterlinearBackTranslation != null)
						UseInterlinearBackTranslation.Dispose();
					if (UseXhtmlExport != null)
						UseXhtmlExport.Dispose();
					if (FootnoteSynchronousScrollingSetting != null)
						FootnoteSynchronousScrollingSetting.Dispose();
					if (ShowTheseStylesSetting != null)
						ShowTheseStylesSetting.Dispose();
					if (ShowStyleLevelSetting != null)
						ShowStyleLevelSetting.Dispose();
					if (ShowUserDefinedStylesSetting != null)
						ShowUserDefinedStylesSetting.Dispose();
					if (AutoStartLibronix != null)
						AutoStartLibronix.Dispose();
					if (ShowTranslateUnsQuestions != null)
						ShowTranslateUnsQuestions.Dispose();
					//if (UseEnableSendReceiveSyncMsgs != null)
					//    UseEnableSendReceiveSyncMsgs.Dispose();
					//UseEnableSendReceiveSyncMsgs = null;
				}
				TeRegistryKey = null;
				ShowMarkerlessIconsSetting = null;
				ShowEmptyParagraphPromptsSetting = null;
				ShowFormatMarksSetting = null;
				UserInterfaceLanguage = null;
				UseVerticalDraftView = null;
				UseInterlinearBackTranslation = null;
				UseXhtmlExport = null;
				FootnoteSynchronousScrollingSetting = null;
				ShowTheseStylesSetting = null;
				ShowStyleLevelSetting = null;
				ShowUserDefinedStylesSetting = null;
				AutoStartLibronix = null;
				ShowTranslateUnsQuestions = null;
			}

			#endregion
		}
		// ReSharper restore MemberHidesStaticFromOuterClass
		#endregion // OptionsImpl

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

		#region Static methods
		private static OptionsImpl Create()
		{
			var optionsImpl = new OptionsImpl();
			optionsImpl.Init();
			return optionsImpl;
		}
		#endregion

		#region Tools options settings (Properties)
		private static OptionsImpl Instance
		{
			get { return SingletonsContainer.Get(() => Create()); }
		}

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
			get { return Instance.AutoStartLibronix.Value && !MiscUtils.IsUnix; }
			set { Instance.AutoStartLibronix.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the UserWs setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string UserInterfaceWritingSystem
		{
			get { return Instance.UserInterfaceLanguage.Value; }
			set { Instance.UserInterfaceLanguage.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the ShowUserDefinedStyles setting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowUserDefinedStylesSetting
		{
			get { return Instance.ShowUserDefinedStylesSetting.Value; }
			set { Instance.ShowUserDefinedStylesSetting.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the ShowFormatMarks setting. Format marks
		/// are end of paragraph and end of StText markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowFormatMarksSetting
		{
			get { return Instance.ShowFormatMarksSetting.Value; }
			set { Instance.ShowFormatMarksSetting.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or sets the value for the "Show Markerless Footnote Icons" setting from
		/// the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowMarkerlessIconsSetting
		{
			get { return Instance.ShowMarkerlessIconsSetting.Value; }
			set { Instance.ShowMarkerlessIconsSetting.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the setting value for the "Synchronous Footnote Scrolling" setting
		/// in the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool FootnoteSynchronousScrollingSetting
		{
			get { return Instance.FootnoteSynchronousScrollingSetting.Value; }
			set { Instance.FootnoteSynchronousScrollingSetting.Value = value; }
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
			get { return Instance.UseVerticalDraftView.Value; }
			set { Instance.UseVerticalDraftView.Value = value; }
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
			get { return Instance.UseInterlinearBackTranslation.Value; }
			set { Instance.UseInterlinearBackTranslation.Value = value; }
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
			get { return Instance.UseXhtmlExport.Value; }
			set { Instance.UseXhtmlExport.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value indicating whether the XHTML Export feature should be used.
		/// Currently this is determined by the exprimental features control in the
		/// advanced tab of the options dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowTranslateUnsQuestions
		{
			get { return Instance.ShowTranslateUnsQuestions.Value; }
			set { Instance.ShowTranslateUnsQuestions.Value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the "Show Empty Paragraph Prompts" setting
		/// in the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowEmptyParagraphPromptsSetting
		{
			get { return Instance.ShowEmptyParagraphPromptsSetting.Value; }
			set { Instance.ShowEmptyParagraphPromptsSetting.Value = value; }
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
				string s = Instance.ShowTheseStylesSetting.Value;
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
						Instance.ShowTheseStylesSetting.Value = "all";
						break;
					case ShowTheseStyles.Basic:
						Instance.ShowTheseStylesSetting.Value = "basic";
						break;
					case ShowTheseStyles.Custom:
						Instance.ShowTheseStylesSetting.Value = "custom";
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
				switch (Instance.ShowStyleLevelSetting.Value)
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
						Instance.ShowStyleLevelSetting.Value = "basic";
						break;
					case StyleLevel.Intermediate:
						Instance.ShowStyleLevelSetting.Value = "intermediate";
						break;
					case StyleLevel.Advanced:
						Instance.ShowStyleLevelSetting.Value = "advanced";
						break;
					case StyleLevel.Expert:
						Instance.ShowStyleLevelSetting.Value = "expert";
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
	}
}
