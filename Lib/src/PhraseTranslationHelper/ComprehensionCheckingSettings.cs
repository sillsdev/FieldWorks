// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ComprehensionCheckingSettings.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Serialization;
using SIL.Utils;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to allow a caller to specify settings for the Comprehension Checking Tool,
	/// typically by deserializing from a previously serialized string.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlRoot("CCSettings")]
	public class ComprehensionCheckingSettings
	{
		#region XML attributes
		/// <summary>The path to the file containing the untranslated questions</summary>
		[XmlAttribute("questionsFile")]
		public string QuestionsFile { get; set; }

		/// <summary>The state of the Comprehension Checking Tool window</summary>
		[XmlAttribute("windowstate")]
		public FormWindowState DefaultWindowState { get; set; }

		/// <summary>The type of Key terms filtering being done</summary>
		[XmlAttribute("ktFilter")]
		public PhraseTranslationHelper.KeyTermFilterType KeyTermFilterType { get; set; }

		/// <summary>Indicates whether the textual question filter allows partial-word matches.</summary>
		[XmlAttribute("matchPartialWors")]
		public bool MatchPartialWords { get; set; }

		/// <summary>Indicates whether toolbar is displayed</summary>
		[XmlAttribute("showToolbar")]
		public bool ShowToolbar { get; set; }

		/// <summary>Indicates whether to send Scripture references as Santa-Fe messages</summary>
		[XmlAttribute("sendScrRefs")]
		public bool SendScrRefs { get; set; }

		/// <summary>Indicates whether to receive Santa-Fe Scripture reference focus messages</summary>
		[XmlAttribute("recvScrRefs")]
		public bool ReceiveScrRefs { get; set; }

		/// <summary>Indicates whether to show a pane with the answers and comments on the questions</summary>
		[XmlAttribute("showAnswers")]
		public bool ShowAnswersAndComments { get; set; }
		#endregion

		#region XML elements
		/// <summary>The location of the Comprehension Checking Tool window</summary>
		[XmlElement("location")]
		public Point Location { get; set; }

		/// <summary>The size of the Comprehension Checking Tool window</summary>
		[XmlElement("size")]
		public Size DialogSize { get; set; }

		/// <summary>The settings used for initializing the GenerateTempletDlg</summary>
		[XmlElement("GenTemplateSettings")]
		public GenerateTemplateSettings GenTemplateSettings { get; set; }

		/// <summary>The maximum height of the key terms pane</summary>
		[XmlElement("maxHeightOfTermsPane")]
		public int MaximumHeightOfKeyTermsPane { get; set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Needed for serialization
		/// </summary>
		private ComprehensionCheckingSettings()
		{
			ShowToolbar = true;
			ShowAnswersAndComments = true;
			MaximumHeightOfKeyTermsPane = 76;
		}

		public ComprehensionCheckingSettings(string questionsFile) : this()
		{
			QuestionsFile = questionsFile;
		}

		internal ComprehensionCheckingSettings(UNSQuestionsDialog dlg)
		{
			DefaultWindowState = dlg.WindowState;
			Location = dlg.Location;
			DialogSize = dlg.Size;
			MatchPartialWords = !dlg.MatchWholeWords;
			KeyTermFilterType = dlg.CheckedKeyTermFilterType;
			ShowToolbar = dlg.ShowToolbar;
			GenTemplateSettings = dlg.GenTemplateSettings;
			SendScrRefs = dlg.SendScrRefs;
			ReceiveScrRefs = dlg.ReceiveScrRefs;
			ShowAnswersAndComments = dlg.ShowAnswersAndComments;
			MaximumHeightOfKeyTermsPane = dlg.MaximumHeightOfKeyTermsPane;
		}
		#endregion

		#region Serialization and Deserialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates settings oibject based on the values in the given XML string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ComprehensionCheckingSettings LoadFromString(string xmlSettings)
		{
			try
			{
				return XmlSerializationHelper.DeserializeFromString<ComprehensionCheckingSettings>(xmlSettings, true);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string respresentation of the settings (suitable for passing to LoadFromString).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return XmlSerializationHelper.SerializeToString(this);
		}
		#endregion
	}

	[XmlType("GenTemplateSettings")]
	public class GenerateTemplateSettings
	{
		[XmlAttribute("range")]
		public GenerateTemplateDlg.RangeOption Range { get; set; }
		[XmlAttribute("book")]
		public string Book { get; set; }
		[XmlAttribute("section")]
		public string Section { get; set; }
		[XmlAttribute("endSection")]
		public string EndSection { get; set; }

		[XmlAttribute("passageBeforeOverview")]
		public bool PassageBeforeOverview { get; set; }
		[XmlAttribute("inclEnglishQuestions")]
		public bool EnglishQuestions { get; set; }
		[XmlAttribute("inclEnglishAnswers")]
		public bool EnglishAnswers { get; set; }
		[XmlAttribute("inclComments")]
		public bool IncludeComments { get; set; }
		[XmlAttribute("useOrigQuestion")]
		public bool UseOriginalQuestionIfNotTranslated { get; set; }

		[XmlAttribute("folder")]
		public string Folder { get; set; }

		[XmlAttribute("numBlankLines")]
		public int BlankLines { get; set; }
		[XmlAttribute("useQuestionNumbers")]
		public bool NumberQuestions { get; set; }

		[XmlAttribute("useExternCss")]
		public bool UseExternalCss { get; set; }
		[XmlAttribute("cssFilename")]
		public string CssFile { get; set; }
		[XmlAttribute("absCssPath")]
		public bool AbsoluteCssPath { get; set; }

		[XmlElement("grpHeadColor")]
		public Color QuestionGroupHeadingsColor { get; set; }
		[XmlElement("engQuestionColor")]
		public Color EnglishQuestionTextColor { get; set; }
		[XmlElement("engAnserColor")]
		public Color EnglishAnswerTextColor { get; set; }
		[XmlElement("commentColor")]
		public Color CommentTextColor { get; set; }

		public GenerateTemplateSettings()
		{
			PassageBeforeOverview = true;
			EnglishQuestions = true;
			EnglishAnswers = true;
			IncludeComments = true;
			NumberQuestions = true;
			EnglishQuestionTextColor = Color.Gray;
			EnglishAnswerTextColor = Color.Green;
			CommentTextColor = Color.Red;
		}

		#region Serialization and Deserialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates settings oibject based on the values in the given XML string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static GenerateTemplateSettings LoadFromString(string xmlSettings)
		{
			try
			{
				return XmlSerializationHelper.DeserializeFromString<GenerateTemplateSettings>(xmlSettings, true);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string respresentation of the settings (suitable for passing to LoadFromString).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return XmlSerializationHelper.SerializeToString(this);
		}
		#endregion
	}
}
