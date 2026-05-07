// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.Scripture;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>Type of text contained in a token</summary>
	public enum TextType
	{
		Verse,          // Text of verses
		Note,           // Text of a footnote or cross reference
		PictureCaption,	// Text within a picture caption
		ChapterNumber,  // Chapter number, currently assumed to be entirely numeric
		VerseNumber,    // Digits and modifier of verse number, may include bridges
		Other           // Introductions, titles, section heads, ...
	}


	// Issue: how do we deal with multiple writing systems.
	// one possible: only request tokens for a single writing system


	/// <summary>
	/// ScriptureChecks are written to process text in small chunks called Text Tokens.
	/// These are intended to be generatable from a wide variety of sources, e.g.
	/// FieldWorks Translation Editor, Paratext, OurWord, etc.
	///
	/// No tokens will be returned for non-publishable text  (\id, \rem, ...)
	///
	/// The note caller from USFM (i.e. the + in \f + My note\f) will not be
	/// passed with the note text.
	///
	/// Normally this interface will be supported by an application specific class
	/// that also contains additional information specific to the application.
	/// </summary>
	public interface ITextToken
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The text of the token.
		/// Verse and chapter numbers are included in this field.
		/// ISSUE: should footnote callers be included in this field???
		/// should not be, if not needed by any check.
		/// If needed this should be a seperate token property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Text { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True iff this token starts a new typographic paragraph.
		/// In this model section headings, poetic lines, etc are paragraphs since
		/// typographically they all force a newline.
		/// ISSUE: What about chapter numbers? Currently no.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsParagraphStart { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True iff this token starts a new note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsNoteStart { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Application specific name locale name. Fieldworks=ICU Locale.
		/// Paratext=Language name.
		/// This is null if this is the default locale for this text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Locale { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the text contained in token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		TextType TextType { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a deep copy of this text token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITextToken Clone();
	}
}