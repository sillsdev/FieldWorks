// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Resources
{
	/// <summary>
	/// Enumeration of standard file types for which the ResourceHelper can provide a file open/
	/// save Filter specification.
	/// Each item in here must have a corresponding resource string named kstid{enum member}
	/// which gives the description. It is looked up in the FileFilter() method by appending
	/// the name of the enumeration member to "kstid".
	/// The ResourceHelper constructor should also have a line where the actual extensions are listed.
	/// </summary>
	public enum FileFilterType
	{
		/// <summary>*.*</summary>
		AllFiles,
		/// <summary>*.sf</summary>
		DefaultStandardFormat,
		/// <summary>*.db, *.sf, *.sfm, *.txt</summary>
		AllScriptureStandardFormat,
		/// <summary>*.xml</summary>
		XML,
		/// <summary>*.rtf</summary>
		RichTextFormat,
		/// <summary>*.pdf</summary>
		PDF,
		/// <summary>Open XML for Editing Scripture (*.oxes)</summary>
		OXES,
		/// <summary>Open XML for Exchanging Scripture Annotations (*.oxesa)</summary>
		OXESA,
		/// <summary>*.txt</summary>
		Text,
		/// <summary>Open Office Files (*.odt)</summary>
		OpenOffice,
		/// <summary>*.xhtml</summary>
		XHTML,
		/// <summary>*.htm (see also HTML)</summary>
		HTM,
		/// <summary>*.html (see also HTM)</summary>
		HTML,
		/// <summary>*.tec</summary>
		TECkitCompiled,
		/// <summary>*.map</summary>
		TECkitMapping,
		/// <summary>*.map</summary>
		ImportMapping,
		/// <summary>Consistent Changes Table (*.cc, *.cct)</summary>
		AllCCTable,
		/// <summary>*.bmp, *.jpg, *.jpeg, *.gif, *.png, *.tif, *.tiff, *.ico, *.wmf, *.pcx, *.cgm</summary>
		AllImage,
		/// <summary>*.wav, *.snd, *.au, *.aif, *.aifc, *.aiff, *.wma, *.mp3</summary>
		AllAudio,
		/// <summary>*.mp4, *.avi, *.wmv, *.wvx, *.mpeg, *.mpg, *.mpe, *.m1v, *.mp2, *.mpv2, *.mpa</summary>
		AllVideo,
		/// <summary>Lift (*.lift)</summary>
		LIFT,
		/// <summary>*.mdf, *.di, *.dic, *.db, *.sfm, *.sf</summary>
		AllShoeboxDictionaryDatabases,
		/// <summary>*.lng</summary>
		ToolboxLanguageFiles,
		/// <summary>*.lds</summary>
		ParatextLanguageFiles,
		/// <summary>*.db, *.sfm, *.sf</summary>
		ShoeboxAnthropologyDatabase,
		/// <summary>*.db, *.sfm, *.sf, *.it</summary>
		InterlinearSfm,
		/// <summary>*.prj</summary>
		ShoeboxProjectFiles,
		/// <summary>*.fwdata</summary>
		FieldWorksProjectFiles,
		/// <summary>*.fwbackup (7.0 or later)</summary>
		FieldWorksBackupFiles,
		/// <summary>*.fwbackup, *.zip (6.0.x or earlier)</summary>
		FieldWorksAllBackupFiles,
		/// <summary>*.xml</summary>
		FieldWorksTranslatedLists,
		/// <summary>Open XML for Exchanging Key Terms (*.oxekt)</summary>
		OXEKT,
		/// <summary>
		/// FLExText interlinear format (*.flextext)
		/// </summary>
		FLExText
	}
}