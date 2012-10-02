// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UsfmStyFileAccessor.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Special exception that can be caught by the export process
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InvalidOrMissingStyFileException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:InvalidOrMissingStyFileException"/> class.
		/// </summary>
		/// <param name="e">The e.</param>
		/// ------------------------------------------------------------------------------------
		public InvalidOrMissingStyFileException(Exception e) : base("Error reading Usfm.sty file", e)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <returns>The error message that explains the reason for the exception.</returns>
		/// ------------------------------------------------------------------------------------
		public override string Message
		{
			get
			{
				return base.InnerException.Message;
			}
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores info about a P6 Stylesheet entry
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UsfmStyEntry : BaseStyleInfo
	{
		#region Data members
		/// <summary>The Paratext 6 Marker (without the leading backslash)</summary>
		public string P6Marker;
		/// <summary>The P6 Stylename</summary>
		public string P6Name;
		/// <summary>List of all markers that this field occurs under</summary>
		public string OccursUnder;
		/// <summary>Specifies order of occurence of markers</summary>
		public int Rank;
		/// <summary>End marker, if any, corresponding to this tag, i.e., Marker = x and Endmarker = x*.</summary>
		public string Endmarker;
		/// <summary>True if this marker must not be repeated under its parent marker.</summary>
		public bool NotRepeatable;
		/// <summary>Use this tag when exporting this marker and its contents as XML.</summary>
		public string XmlTag;

		/// <summary>If this style entry is based on a Sty file marker, this will be used
		/// instead of calculating the text properties based on the FW style</summary>
		protected string m_textPropertiesFromStyFile;
		/// <summary>Indicates whether this field is poetic</summary>
		public bool Poetic;
		/// <summary>Level of title, section, or poetic line</summary>
		public int Level;

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether a FW style begins a P6 paragraph.
		/// </summary>
		/// <value><c>true</c> if [begins paragraph]; otherwise, <c>false</c>.</value>
		/// <remarks>According to the P6 specification, footnote and cross-reference styles
		/// are not regarded as paragraphs. Do not use this property if this style entry is
		/// based on a USFM Sty entry rather than a FW StStyle.</remarks>
		/// ------------------------------------------------------------------------------------
		private bool BeginsParagraph
		{
			get { return (IsParagraphStyle && m_context != ContextValues.Note); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a TextProperties token indicating whether a style is publishable.
		/// </summary>
		/// <value>"publishable " if this style is publishable; "nonpublishable " if not
		/// publishable; and string.Empty if style is regarded as semi-publishable meta-data
		/// by P6</value>
		/// <remarks>According to the P6 specification, Chapter and verse number styles
		/// are neither publishable nor nonpublishable</remarks>
		/// ------------------------------------------------------------------------------------
		public string PublishableAsString
		{
			get
			{
				if (m_context == ContextValues.Book || m_context == ContextValues.Annotation)
					return "nonpublishable ";
				else if (m_function == FunctionValues.Chapter || m_function == FunctionValues.Verse)
					return string.Empty;
				else
					return "publishable ";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a TextProperties token indicating whether a style is vernacular.
		/// </summary>
		/// <value>"vernacular " if this style is vernacular; "nonvernacular " if not
		/// vernacular; and string.Empty if style is regarded as semi-vernacular meta-data
		/// by P6</value>
		/// <remarks>According to the P6 specification, Chapter and verse number styles
		/// are neither vernacular nor nonvernacular</remarks>
		/// ------------------------------------------------------------------------------------
		public string VernacularAsString
		{
			get
			{
				if (m_context == ContextValues.Book || m_context == ContextValues.Annotation)
					return "nonvernacular ";
				else if (m_function == FunctionValues.Chapter || m_function == FunctionValues.Verse)
					return string.Empty;
				else
					return "vernacular ";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the TextProperies info as a string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string TextProperties
		{
			get
			{
				if (m_textPropertiesFromStyFile != null)
					return m_textPropertiesFromStyFile;

				StringBuilder properties = new StringBuilder();

				if (BeginsParagraph)
					properties.Append("paragraph ");

				properties.Append(PublishableAsString);
				properties.Append(VernacularAsString);

				if (Level > 0)
				{
					properties.Append("level_");
					properties.Append(Level);
					properties.Append(" ");
				}
				if (m_context == ContextValues.Book)
					properties.Append("book ");
				if (m_function == FunctionValues.Chapter)
					properties.Append("chapter ");
				if (m_function == FunctionValues.Verse)
					properties.Append("verse ");
				if (m_context == ContextValues.Note)
				{
					if (m_name == ScrStyleNames.CrossRefFootnoteParagraph)
						properties.Append("crossreference ");
					else
						properties.Append("note ");
				}

				if (Poetic)
					properties.Append("poetic ");

				return properties.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the TextType info for the paratext style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string TextType
		{
			get
			{
				if (m_function == FunctionValues.Chapter)
					return "ChapterNumber";
				if (m_function == FunctionValues.Verse)
					return "VerseNumber";
				if (m_context == ContextValues.Note)
					return "NoteText";
				if (m_context == ContextValues.Title)
					return "Title";
				if (m_structure == StructureValues.Heading)
					return "Section";
				if (m_structure == StructureValues.Body
					&& m_context != ContextValues.Intro)
				{
					return "VerseText";
				}

				return "Other";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the style as a string suitable for writing out in the StyleType
		/// field of a P6 sty file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StyleType
		{
			get
			{
				// handle note context styles in the paratext way
				if (m_context == ContextValues.Note)
				{
					if (m_styleType == SIL.FieldWorks.Common.COMInterfaces.StyleType.kstParagraph)
						return "Note";
					return "Character";
				}
				// handle chapter style as if it were a paragraph style
				if (m_function == FunctionValues.Chapter)
					return "Paragraph";
				// handle secondary and tertiary title styles as if they were paragraph styles
				// ENHANCE: If in the future we create distinctive TE styles to handle the
				// paragraph/character distinction properly, we might need to change the logic of
				// this condition.
				if (m_name == ScrStyleNames.SecondaryBookTitle ||
					m_name == ScrStyleNames.TertiaryBookTitle)
				{
					return "Paragraph";
				}

				return (m_styleType == SIL.FieldWorks.Common.COMInterfaces.StyleType.kstCharacter) ?
					"Character" : "Paragraph";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the properties of this entry based on the given FW style.
		/// </summary>
		/// <param name="style">An StStyle.</param>
		/// ------------------------------------------------------------------------------------
		public override void SetPropertiesBasedOnStyle(IStStyle style)
		{
			base.SetPropertiesBasedOnStyle(style);
			m_textPropertiesFromStyFile = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font info for the given writing system.
		/// </summary>
		/// <param name="ws">The writing system</param>
		/// <returns>The font information, which may be either the default info for the style
		/// or an override that is specific to the given writing system</returns>
		/// <remarks>This override will return the default info if there is no
		/// override information for the given writing system</remarks>
		/// ------------------------------------------------------------------------------------
		public override FontInfo FontInfoForWs(int ws)
		{
			if (ws == -1 || !m_fontInfoOverrides.ContainsKey(ws))
				return m_defaultFontInfo;

			return m_fontInfoOverrides[ws];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serializes this object to the specified writer.
		/// </summary>
		/// <param name="ws">HVO of the writing system whose properties should be used for
		/// figuring out which set of font properties to use (FW allows for overrides based on
		/// WS).
		/// </param>
		/// <param name="writer">The writer.</param>
		/// ------------------------------------------------------------------------------------
		internal void Serialize(int ws, FileWriter writer)
		{
			writer.WriteLine(@"\Marker " + P6Marker);
			if (m_name != null)
				writer.WriteLine(@"\TEStyleName " + m_name);
			if (Endmarker != null)
				writer.WriteLine(@"\Endmarker " + Endmarker);
			writer.WriteLine(@"\Name " + (P6Name == null ? m_name : P6Name));
			if (m_usage != null)
				writer.WriteLine(@"\Description " + m_usage);
			if (OccursUnder != null)
				writer.WriteLine(@"\OccursUnder " + OccursUnder);
			if (Rank > 0)
				writer.WriteLine(@"\Rank " + Rank);
			if (NotRepeatable)
				writer.WriteLine(@"\NotRepeatable");
			writer.WriteLine(@"\TextType " + TextType);
			writer.WriteLine(@"\TextProperties " + TextProperties);
			writer.WriteLine(@"\StyleType " + StyleType);

			FontInfo fontInfo = FontInfoForWs(ws);
			if (fontInfo.m_italic.ValueIsSet && fontInfo.m_italic.Value)
				writer.WriteLine(@"\Italic");
			if (fontInfo.m_underline.ValueIsSet && fontInfo.m_underline.Value != FwUnderlineType.kuntNone)
				writer.WriteLine(@"\Underline");
			if (fontInfo.m_bold.ValueIsSet && fontInfo.m_bold.Value)
				writer.WriteLine(@"\Bold");
			if (fontInfo.m_superSub.ValueIsSet && fontInfo.m_superSub.Value == FwSuperscriptVal.kssvSuper)
				writer.WriteLine(@"\Superscript");
			if (!fontInfo.m_fontName.IsInherited) // TODO (TE-5185): This isn't good enough - if a char style inherits from another char style that sets a specific font, the derived char style also needs to output that font spec in the sty file because the sty file doesn't handle inheritance.
				writer.WriteLine(@"\FontName " + RealFontNameForWs(ws));
			// Write out the font size, converting it from millipoints to points
			if (fontInfo.m_fontSize.ValueIsSet)
				writer.WriteLine(@"\FontSize " + fontInfo.m_fontSize.Value / 1000);
			if (fontInfo.m_fontColor.ValueIsSet && fontInfo.m_fontColor.Value != Color.Black)
				writer.WriteLine(@"\Color " + ColorUtil.ConvertColorToBGR(fontInfo.m_fontColor.Value));

			if (m_alignment.ValueIsSet)
			{
				switch (m_alignment.Value)
				{
					case FwTextAlign.ktalCenter:
						writer.WriteLine(@"\Justification Center");
						break;
					case FwTextAlign.ktalLeading:
						break;
					case FwTextAlign.ktalJustify:
						writer.WriteLine(@"\Justification Both");
						break;
					case FwTextAlign.ktalTrailing:
						writer.WriteLine(@"\Justification Right"); // "right" really means "trailing" in Paratext
						break;
					case FwTextAlign.ktalLeft:
						if (m_rtl.Value == TriStateBool.triTrue)
							writer.WriteLine(@"\Justification Right"); // "right" really means "trailing" in Paratext
						break;
					case FwTextAlign.ktalRight:
						if (m_rtl.Value != TriStateBool.triTrue)
							writer.WriteLine(@"\Justification Right"); // "right" really means "trailing" in Paratext
						break;
				}
			}

			if (m_firstLineIndent.ValueIsSet && m_firstLineIndent.Value > 0)
				writer.WriteLine(@"\FirstLineIndent " + (((double)m_firstLineIndent.Value)/72000).ToString("#.000"));
			if (m_leadingIndent.ValueIsSet && m_leadingIndent.Value > 0)
				writer.WriteLine(@"\LeftMargin " + (((double)m_leadingIndent.Value)/72000).ToString("#.000"));
			if (m_trailingIndent.ValueIsSet && m_trailingIndent.Value > 0)
				writer.WriteLine(@"\RightMargin " + (((double)m_trailingIndent.Value)/72000).ToString("#.000"));
			if (m_spaceBefore.ValueIsSet && m_spaceBefore.Value > 0)
				writer.WriteLine(@"\SpaceBefore " + m_spaceBefore.Value / 1000); // convert from millipoints to points
			if (m_spaceAfter.ValueIsSet && m_spaceAfter.Value > 0)
				writer.WriteLine(@"\SpaceAfter " + m_spaceAfter.Value / 1000); // convert from millipoints to points

			if (XmlTag != null)
				writer.WriteLine(@"\XMLTag " + XmlTag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a propery based on an entry in a P6 sty file.
		/// </summary>
		/// <param name="marker">The marker (see Advanced Paratext 6 Help for complete list and
		/// explanation of possible markers)</param>
		/// <param name="value">The value</param>
		/// ------------------------------------------------------------------------------------
		protected internal void SetUsfmStyProperty(string marker, string value)
		{
			try
			{
				switch (marker.ToLowerInvariant())
				{
					case @"\testylename":
						m_name = value;
						break;
					case @"\name":
						P6Name = value;
						break;
					case @"\description":
						m_usage = value;
						break;
					case @"\styletype":
						switch (value.ToLowerInvariant())
						{
							case "character":
								m_styleType = SIL.FieldWorks.Common.COMInterfaces.StyleType.kstCharacter;
								break;
							case "paragraph":
								m_styleType = SIL.FieldWorks.Common.COMInterfaces.StyleType.kstParagraph;
								break;
							case "note":
								m_context = ContextValues.Note;
								break;
						}
						break;
					case @"\occursunder":
						OccursUnder = value;
						break;
					case @"\rank":
						Rank = Int32.Parse(value);
						break;
					case @"\notrepeatable": // source is null for the NotRepeatable attribute
						NotRepeatable = true;
						break;
					case @"\texttype":
						switch (value.ToLowerInvariant())
						{
							case "title":
								m_context = ContextValues.Title;
								break;
							case "section":
								m_context = ContextValues.Text;
								m_structure = StructureValues.Heading;
								break;
							case "versetext":
								m_context = ContextValues.Text;
								m_structure = StructureValues.Body;
								m_function = FunctionValues.Prose;
								break;
							case "versenumber":
								m_context = ContextValues.Text;
								m_structure = StructureValues.Body;
								m_function = FunctionValues.Verse;
								break;
							case "chapternumber":
								m_context = ContextValues.Text;
								m_structure = StructureValues.Body;
								m_function = FunctionValues.Chapter;
								break;
							case "notetext":
								m_context = ContextValues.Note;
								break;
							case "translationnote": // This is documented but doesn't appear to be used
								m_context = ContextValues.Annotation;
								break;
							case "other":
							default:
								m_context = ContextValues.Internal; // This is one of several context values that correspond to "other" TextType
								break;
						}
						break;
					case @"\endmarker":
						Endmarker = value;
						break;
					case @"\xmltag":
						XmlTag = value;
						break;

					case @"\textproperties":
						m_textPropertiesFromStyFile = value;
						value = value.ToLowerInvariant();
						List<string> tokens = new List<string>(value.Split(new char[] { ' ' }));
						Poetic = tokens.Contains("poetic");
						foreach (string prop in tokens)
							if (prop.StartsWith("level_"))
								Level = Int32.Parse(prop.Substring(6));
						break;

					case @"\fontsize":
						m_defaultFontInfo.m_fontSize.ExplicitValue = Int32.Parse(value) * 1000; // convert from points to millipoints
						break;
					case @"\color":
						m_defaultFontInfo.m_fontColor.ExplicitValue = ColorUtil.ConvertBGRtoColor((uint)Int32.Parse(value));
						break;
					case @"\bold": // source is null for the Bold attribute
						m_defaultFontInfo.m_bold.ExplicitValue = true;
						break;
					case @"\italic": // source is null for the Italic attribute
						m_defaultFontInfo.m_italic.ExplicitValue = true;
						break;
					case @"\superscript": // source is null for the Subscript attribute
						m_defaultFontInfo.m_superSub.ExplicitValue = FwSuperscriptVal.kssvSuper;
						break;
					case @"\underline": // source is null for the Underline attribute
						m_defaultFontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntSingle;
						break;

					case @"\spacebefore":
						m_spaceBefore.ExplicitValue = Int32.Parse(value) * 1000; // convert from points to millipoints
						break;
					case @"\spaceafter":
						m_spaceAfter.ExplicitValue = Int32.Parse(value) * 1000; // convert from points to millipoints
						break;
					case @"\firstlineindent":
						m_firstLineIndent.ExplicitValue = (int)(double.Parse(value) * MiscUtils.kdzmpInch);
						break;
					case @"\justification":
						switch (value.ToLowerInvariant())
						{
							case "center":
								m_alignment.ExplicitValue = FwTextAlign.ktalCenter;
								break;
							case "left":
								m_alignment.ExplicitValue = FwTextAlign.ktalLeft;
								break;
							case "right":
								m_alignment.ExplicitValue = FwTextAlign.ktalRight;
								break;
							case "both":
								m_alignment.ExplicitValue = FwTextAlign.ktalJustify;
								break;
						}
						break;
					case @"\leftmargin":
						m_leadingIndent.ExplicitValue = (int)(Double.Parse(value) * MiscUtils.kdzmpInch);
						break;
					case @"\rightmargin":
						m_trailingIndent.ExplicitValue = (int)(Double.Parse(value) * MiscUtils.kdzmpInch);
						break;
					default:
						Debug.Fail(marker);
						break;
				}
			}
			catch { }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// UsfmStyFileAccessor reads and writes a Paratext-style sty file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UsfmStyFileAccessor : StyleInfoTable
	{
		#region Member Data
		private StreamReader m_fileReader = null;
		private UsfmStyEntry m_currentEntry;
		private bool m_defaultRightToLeft;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:UsfmStyFileAccessor"/> class.
		/// </summary>
		/// <param name="defaultRightToLeft">if set to <c>true</c> [default right to left].</param>
		/// <param name="wsf">The Writing System Factory (needed to resolve magic font names
		/// to real ones)</param>
		/// ------------------------------------------------------------------------------------
		public UsfmStyFileAccessor(bool defaultRightToLeft, ILgWritingSystemFactory wsf)
			: base(ScrStyleNames.Normal, wsf)
		{
			m_defaultRightToLeft = defaultRightToLeft;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given style info entry to the table.
		/// </summary>
		/// <param name="key">The key of the element to add (typically a TE Stylename, but may
		/// be another unique token (if this entry represents a style which is not known to
		/// exist)</param>
		/// <param name="value">The value of the element to add (must not be null)</param>
		/// <exception cref="T:System.ArgumentException">An element with the same key already
		/// exists in the <see cref="T:System.Collections.Generic.Dictionary`2"></see>.</exception>
		/// <exception cref="T:System.ArgumentNullException">key or value is null.</exception>
		/// ------------------------------------------------------------------------------------
		public override void Add(string key, BaseStyleInfo value)
		{
			base.Add(key, value);
			// We need to set the P6 Marker based on the key, but only if it's null. If it's
			// not null, this is probably a case where an entry is being re-keyed based on the
			// TE style name, so we should leave the P6 marker as is.
			UsfmStyEntry entry = (UsfmStyEntry)value;
			if (entry.P6Marker == null)
				entry.P6Marker = key.Replace(' ', '_');
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads a Paratext6 stylesheet
		/// </summary>
		/// <param name="sFile">Name of the P6 stylesheet to read as the basis for the one
		/// we will write</param>
		/// ------------------------------------------------------------------------------------
		public void ReadStylesheet(string sFile)
		{
			try
			{
				using (m_fileReader = new StreamReader(sFile))
				{
					string line;
					// Get the next line from the file
					while ((line = ReadNextLine()) != null)
					{
						// Split up the line into marker and contents
						string[] tokens = line.Split(new char[] { ' ', '\t' }, 2); // remove whitespace from front of line
						string marker = tokens[0];
						string lineContents = null;
						if (tokens.Length == 2)
						{
							lineContents = tokens[1];
							// Strip any trailing comments at the end of the line
							int ichComment = lineContents.IndexOf('#');
							if (ichComment >= 0)
								lineContents = lineContents.Substring(0, ichComment);
							lineContents = lineContents.Trim();
						}

						// Process the next segment from the line.
						ProcessSegment(marker, lineContents);
					}
				}
			}
			catch (Exception e)
			{
				throw new InvalidOrMissingStyFileException(e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the next set of lines from the source file that associate with a marker
		/// </summary>
		/// <returns>a string for the line text or null if the end of file was reached</returns>
		/// <remarks>standard format text can be broken across lines. Any line that does not
		/// start with a backslash will be appended to the previous line. Any lines at the
		/// beginning of the file that do not start with a backslash will be ignored as
		/// will blank lines.</remarks>
		/// ------------------------------------------------------------------------------------
		private string ReadNextLine()
		{
			string line;

			// Read the next line from the file. Keep reading until a line that
			// starts with a backslash is found.
			do
			{
				line = m_fileReader.ReadLine();
				if (line == null)
					return null;
			} while (line == string.Empty || line[0] != '\\');
			return line;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a marker/contents pair.
		/// </summary>
		/// <param name="marker">text of the marker</param>
		/// <param name="source">source text to process</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessSegment(string marker, string source)
		{
			if (marker == @"\Marker")
			{
				m_currentEntry = new UsfmStyEntry();
				Add(source, m_currentEntry);
			}
			else
			{
				m_currentEntry.SetUsfmStyProperty(marker, source);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the sty file.
		/// </summary>
		/// <param name="LangProjectName">Name of the language project.</param>
		/// <param name="writer">The writer.</param>
		/// <param name="ws">The HVO of the writing system for the current export.</param>
		/// ------------------------------------------------------------------------------------
		public void SaveStyFile(string LangProjectName, FileWriter writer, int ws)
		{
			ConnectStyles();

			writer.WriteLine("## Stylesheet for exported TE project " + LangProjectName + " ##");
			writer.WriteLine(string.Empty, true);

			// Write out the style sheet entries
			foreach (UsfmStyEntry entry in Values)
			{
				entry.Serialize(ws, writer);
				writer.WriteLine(string.Empty, true);
			}
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the underyling right-to-left value based on the context.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override TriStateBool DefaultRightToLeft
		{
			get { return m_defaultRightToLeft ? TriStateBool.triTrue : TriStateBool.triFalse; }
		}
		#endregion
	}
}
