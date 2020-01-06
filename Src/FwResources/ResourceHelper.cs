// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Text;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Resources
{
	/// <summary>
	/// Provides access to resources
	/// </summary>
	/// <remarks>The non-static methods and fields are in a separate class so that clients can
	/// use this class without the need for a reference to Windows.Forms if all they need is to
	/// get some strings.</remarks>
	public static class ResourceHelper
	{
		#region Member variables
		/// <summary />
		private static ResourceHelperImpl s_form;
		/// <summary />
		internal static ResourceManager s_stringResources;
		/// <summary />
		internal static ResourceManager s_helpResources;
		/// <summary />
		internal static readonly Dictionary<FileFilterType, string> s_fileFilterExtensions;
		#endregion

		#region Construction and destruction

		/// <summary />
		static ResourceHelper()
		{
			s_fileFilterExtensions = new Dictionary<FileFilterType, string>
			{
				[FileFilterType.AllFiles] = "*.*",
				[FileFilterType.DefaultStandardFormat] = "*.sf",
				[FileFilterType.AllScriptureStandardFormat] = "*.db; *.sf; *.sfm; *.txt",
				[FileFilterType.XML] = "*.xml",
				[FileFilterType.RichTextFormat] = "*.rtf",
				[FileFilterType.PDF] = "*.pdf",
				[FileFilterType.OXES] = "*" + FwFileExtensions.ksOpenXmlForEditingScripture,
				[FileFilterType.OXESA] = "*" + FwFileExtensions.ksOpenXmlForExchangingScrAnnotations,
				[FileFilterType.Text] = "*.txt",
				[FileFilterType.OpenOffice] = "*.odt",
				[FileFilterType.XHTML] = "*.xhtml",
				[FileFilterType.HTM] = "*.htm",
				[FileFilterType.HTML] = "*.html",
				[FileFilterType.TECkitCompiled] = "*.tec",
				[FileFilterType.TECkitMapping] = "*.map",
				[FileFilterType.ImportMapping] = "*.map",
				[FileFilterType.AllCCTable] = "*.cc; *.cct",
				[FileFilterType.AllImage] = "*.bmp; *.jpg; *.jpeg; *.gif; *.png; *.tif; *.tiff; *.ico; *.wmf; *.pcx; *.cgm",
				[FileFilterType.AllAudio] = "*.wav; *.snd; *.au; *.aif; *.aifc; *.aiff; *.wma; *.mp3",
				[FileFilterType.AllVideo] = "*.mp4; *.avi; *.wmv; *.wvx; *.mpeg; *.mpg; *.mpe; *.m1v; *.mp2; *.mpv2; *.mpa",
				[FileFilterType.LIFT] = "*" + FwFileExtensions.ksLexiconInterchangeFormat,
				[FileFilterType.AllShoeboxDictionaryDatabases] = "*.mdf; *.di; *.dic; *.db; *.sfm; *.sf",
				[FileFilterType.ToolboxLanguageFiles] = "*.lng",
				[FileFilterType.ParatextLanguageFiles] = "*.lds",
				[FileFilterType.ShoeboxAnthropologyDatabase] = "*.db; *.sfm; *.sf",
				[FileFilterType.InterlinearSfm] = "*.db; *.sfm; *.sf; *.it; *.itx; *.txt",
				[FileFilterType.ShoeboxProjectFiles] = "*.prj",
				[FileFilterType.FieldWorksProjectFiles] = "*" + LcmFileHelper.ksFwDataXmlFileExtension,
				[FileFilterType.FieldWorksBackupFiles] = "*" + LcmFileHelper.ksFwBackupFileExtension,
				[FileFilterType.FieldWorksAllBackupFiles] = $"*{LcmFileHelper.ksFwBackupFileExtension}; *{LcmFileHelper.ksFw60BackupFileExtension}; *.xml",
				[FileFilterType.FieldWorksTranslatedLists] = "*.xml; *.zip",
				[FileFilterType.OXEKT] = "*" + FwFileExtensions.ksOpenXmlForExchangingKeyTerms,
				[FileFilterType.FLExText] = "*" + FwFileExtensions.ksFLexText
			};
		}

		/// <summary>
		/// Shut down the one instance of ResourceHelper.
		/// </summary>
		/// <remarks>
		/// This should be called once when the application shuts down.
		/// </remarks>
		public static void ShutdownHelper()
		{
			if (s_form != null)
			{
				s_form.DisposeStaticMembers();
				s_form.Dispose();
			}
			s_form = null;
		}

		#endregion

		internal static ResourceHelperImpl Helper
		{
			get { return s_form ?? (s_form = new ResourceHelperImpl()); }
			set
			{
				s_form?.Dispose();
				s_form = value;
			}
		}

		#region Public methods

		/// <summary>
		/// Function to create appropriate labels for Undo tasks, with the action names coming
		/// from the stid.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <param name="stUndo">Returns string for Undo task</param>
		/// <param name="stRedo">Returns string for Redo task</param>
		public static void MakeUndoRedoLabels(string stid, out string stUndo, out string stRedo)
		{
			var stRes = GetResourceString(stid);

			// If we get here from a test, it might not find the correct resource.
			// Just ignore it and set some dummy values
			if (string.IsNullOrEmpty(stRes))
			{
				stUndo = "Resource not found for Undo";
				stRedo = "Resource not found for Redo";
				return;
			}
			var stStrings = stRes.Split('\n');
			if (stStrings.Length > 1)
			{
				// The resource string contains two separate strings separated by a new-line.
				// The first half is for Undo and the second for Redo.
				stUndo = stStrings[0];
				stRedo = stStrings[1];
			}
			else
			{
				// Insert the string (describing the task) into the undo/redo frames.
				stUndo = string.Format(GetResourceString("kstidUndoFrame"), stRes);
				stRedo = string.Format(GetResourceString("kstidRedoFrame"), stRes);
			}
		}

		/// <summary>
		/// Return a string from a resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		public static string GetResourceString(string stid)
		{
			if (s_stringResources == null)
			{
				s_stringResources = new ResourceManager("SIL.FieldWorks.Resources.FwStrings", Assembly.GetExecutingAssembly());
			}

			return (stid == null ? "NullStringID" : s_stringResources.GetString(stid));
		}

		/// <summary>
		/// Return a string from a resource ID, with formatting placeholders replaced by the
		/// supplied parameters.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <param name="parameters">zero or more parameters to format the resource string</param>
		public static string FormatResourceString(string stid, params object[] parameters)
		{
			return string.Format(GetResourceString(stid), parameters);
		}

		/// <summary>
		/// Return a help topic or help file path.
		/// </summary>
		/// <param name="stid">String resource id</param>
		public static string GetHelpString(string stid)
		{
			if (s_helpResources == null)
			{
				s_helpResources = new ResourceManager("SIL.FieldWorks.Resources.HelpTopicPaths", Assembly.GetExecutingAssembly());
			}

			return (stid == null ? "NullStringID" : s_helpResources.GetString(stid));
		}

		/// <summary>
		/// Gets the one column selected icon for page layout.
		/// </summary>
		public static Image OneColumnSelectedIcon => Helper.m_imgLst53x43.Images[7];

		/// <summary>
		/// Gets the portrait page layout icon.
		/// </summary>
		public static Image PortraitIcon => Helper.m_imgLst53x43.Images[5];

		/// <summary>
		/// Gets the icon displayed in the styles combo box for paragraph styles.
		/// </summary>
		public static Image ParaStyleIcon => Helper.m_imgLst16x16.Images[0];

		/// <summary>
		/// Gets the icon displayed in the styles combo box for the selected paragraph style.
		/// </summary>
		public static Image SelectedParaStyleIcon => Helper.m_imgLst16x16.Images[2];

		/// <summary>
		/// Gets the icon displayed in the styles combo box for character styles.
		/// </summary>
		public static Image CharStyleIcon => Helper.m_imgLst16x16.Images[1];

		/// <summary>
		/// Gets the icon displayed in the styles combo box for the selected character style.
		/// </summary>
		public static Image SelectedCharStyleIcon => Helper.m_imgLst16x16.Images[3];

		/// <summary>
		/// Gets the icon displayed in the styles combo box for data property pseudo-styles.
		/// </summary>
		public static Image DataPropStyleIcon => Helper.m_imgLst16x16.Images[4];

		/// <summary>
		/// Gets the icon displayed in the styles combo box for data property pseudo-styles.
		/// </summary>
		public static Image UpArrowIcon => Helper.m_imgLst16x16.Images[5];

		/// <summary>
		/// Gets the icon displayed on the MoveHere buttons in Discourse Constituent Chart layout.
		/// </summary>
		public static Image MoveUpArrowIcon => Helper.m_imgLst16x16.Images[8];

		/// <summary>
		/// Gets the icon that looks like a yellow light bulb
		/// </summary>
		public static Image SuggestLightbulb => Helper.m_imgLst16x16.Images[11];

		/// <summary>
		/// Gets the icon that looks like ABC with check mark. (Review: this is from the MSVS2005
		/// image library...can we make it part of an OS project like this?)
		/// </summary>
		public static Image SpellingIcon => Helper.m_imgLst16x16.Images[9];

		/// <summary>
		/// Gets the icon that looks like two green arrows circling back on each other, typically
		/// used to indicate that something should be refreshed (see e.g. Change spelling dialog)
		/// </summary>
		public static Image RefreshIcon => Helper.m_imgLst16x16.Images[10];

		/// <summary>
		/// Gets the double down arrow icon displayed on "More" buttons.
		/// </summary>
		public static Image MoreButtonDoubleArrowIcon => Helper.m_imgLst11x7.Images[1];

		/// <summary>
		/// Gets the double up arrow icon displayed on "Less" buttons.
		/// </summary>
		public static Image LessButtonDoubleArrowIcon => Helper.m_imgLst11x7.Images[0];

		/// <summary>
		/// Gets the down arrow icon used on buttons that display popup menus when clicked.
		/// </summary>
		public static Image ButtonMenuArrowIcon => Helper.m_imgLst11x7.Images[2];

		/// <summary>
		/// Gets the down arrow icon used on buttons that display popup menus when clicked.
		/// </summary>
		public static Image ButtonMenuHelpIcon => Helper.menuToolBarImages.Images[9];

		/// <summary>
		/// Gets a slightly different version of the down arrow icon used on buttons that
		/// display popup menus when clicked. This one is used in the FwComboBox widget.
		/// To get the right appearance, the arrow needs to be one pixel further left than
		/// in ButtonMenuArrowIcon
		/// </summary>
		public static Image ComboMenuArrowIcon => Helper.m_imgLst11x7.Images[3];

		/// <summary>
		/// Gets a pull-down arrow on a yellow background with a black border, used in IText.
		/// </summary>
		public static Image InterlinPopupArrow => Helper.m_imgLst11x12.Images[0];

		/// <summary>
		/// Pull-down arrow used for Some context menus
		/// </summary>
		public static Image BlueCircleDownArrow => Helper.m_imgLst11x11.Images[0];

		/// <summary>Same arrow, but with explicit white background suitable for use in views.
		/// </summary>
		public static Image BlueCircleDownArrowForView => Helper.m_imgLst11x11.Images[3];

		/// <summary>
		/// Pull-down arrow used for Column configuration
		/// </summary>
		public static Image ColumnChooser => Helper.m_imgLst11x11.Images[1];

		/// <summary>
		/// Pull-down arrow used for bulk edit check marks
		/// </summary>
		public static Image CheckMarkHeader => Helper.m_imgLst11x11.Images[2];

		/// <summary>
		/// Gets an image of a box to use for an unexpanded item in a tree.
		/// </summary>
		public static Image PlusBox => Helper.m_imgLst9x9.Images[1];

		/// <summary>
		/// Gets an image of a box to use for an expanded item in a tree.
		/// </summary>
		public static Image MinusBox => Helper.m_imgLst9x9.Images[0];

		/// <summary>
		/// Gets an image of a button to bring up the chooser dialog.
		/// </summary>
		public static Image ChooserButton => Helper.m_imgLst14x13.Images[0];

		/// <summary>
		/// Gets an image exactly matching a standard checked checkbox. To look right it should
		/// be on a light grey background or have some border around it.
		/// </summary>
		public static Image CheckedCheckBox => Helper.m_imgLst13x13.Images[1];

		/// <summary>
		/// Gets an image exactly matching a standard unchecked checkbox. To look right it should
		/// be on a light grey background or have some border around it.
		/// </summary>
		public static Image UncheckedCheckBox => Helper.m_imgLst13x13.Images[2];

		/// <summary>
		/// Gets an image exactly matching a standard unchecked checkbox. To look right it should
		/// be on a light grey background or have some border around it.
		/// </summary>
		public static Image DisabledCheckBox => Helper.m_imgLst13x13.Images[7];

		/// <summary>
		/// Gets an image of a red X (typically used for 'wrong') on a transparent background.
		/// </summary>
		public static Image RedX => Helper.m_imgLst13x13.Images[3];

		/// <summary>
		/// Gets an image of a green check (typically used for 'right') on a transparent
		/// background.
		/// </summary>
		public static Image GreenCheck => Helper.m_imgLst13x13.Images[4];

		/// <summary>
		/// Replace all of the transparent pixels in the image with the given color
		/// </summary>
		public static Bitmap ReplaceTransparentColor(Image img, Color replaceColor)
		{
			var bmp = new Bitmap(img);
			for (var x = 0; x < bmp.Width; x++)
			{
				for (var y = 0; y < bmp.Height; y++)
				{
					if (bmp.GetPixel(x, y).A == 0)
					{
						bmp.SetPixel(x, y, replaceColor);
					}
				}
			}

			return bmp;
		}

		/// <summary>
		/// Builds a filter specification for multiple file types for a SaveFileDialog or
		/// OpenFileDialog.
		/// </summary>
		/// <param name="types">The types of files to include in the filter, in the order they
		/// should be included. Do not use any of the enumeration values starting with "All"
		/// for a filter intended to be used in a SaveFileDialog.</param>
		/// <returns>A string suitable for setting the Filter property of a SaveFileDialog or
		/// OpenFileDialog</returns>
		public static string BuildFileFilter(params FileFilterType[] types)
		{
			return BuildFileFilter((IEnumerable<FileFilterType>)types);
		}

		/// <summary>
		/// Builds a filter specification for multiple file types for a SaveFileDialog or
		/// OpenFileDialog.
		/// </summary>
		/// <param name="types">The types of files to include in the filter, in the order they
		/// should be included. Do not use any of the enumeration values starting with "All"
		/// for a filter intended to be used in a SaveFileDialog.</param>
		/// <returns>A string suitable for setting the Filter property of a SaveFileDialog or
		/// OpenFileDialog</returns>
		public static string BuildFileFilter(IEnumerable<FileFilterType> types)
		{
			var bldr = new StringBuilder();
			foreach (var type in types)
			{
				bldr.AppendFormat("{0}|", FileFilter(type));
			}
			bldr.Length--;
			return bldr.ToString();
		}

		/// <summary>
		/// Builds a filter specification for a single file type for SaveFileDialog or
		/// OpenFileDialog.
		/// </summary>
		/// <param name="type">The type of files to include in the filter. Do not use any of the
		/// enumeration values starting with "All" for a filter intended to be used in a
		/// SaveFileDialog.</param>
		/// <returns>A string suitable for setting the Filter property of a SaveFileDialog or
		/// OpenFileDialog</returns>
		public static string FileFilter(FileFilterType type)
		{
			return FileUtils.FileDialogFilterCaseInsensitiveCombinations(string.Format("{0} ({1})|{1}", GetResourceString("kstid" + type), s_fileFilterExtensions[type]));
		}
		#endregion
	}
}