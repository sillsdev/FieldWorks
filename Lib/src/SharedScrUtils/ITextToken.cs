using System;
using System.Collections.Generic;
using System.Text;

namespace SILUBS.SharedScrUtils
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
		/// This is the style name for the paragraph containing this text. It is needed by
		/// * Matched Pair check
		/// * Uncapitalized Styles check
		/// * Quotations check
		///
		/// The quotation check which must have a list of paragraph types which
		/// require continuation quotes.
		///
		/// Quotation checking requires being able to say things like:
		///     (these rules assume USFM markup, not sure how this would translate
		///      into TE markup model, maybe just different style names?)
		///     * If there is an open quote all \p paragraphs should start with
		///       a continuer
		///     * If there is an open quote all \q1 paragraphs should start with
		///       a continuer if they follow a \p or \b but not if they follow
		///       \q2 or \q1.
		///
		/// ALTERNATIVE
		/// have two properties just for Quotations check
		///       bool ParagraphRequiresQuoteContinuer
		///       bool ParagraphRequiresQuoteInQuoteContinuer
		/// have two properties just for Matched Pair check
		///       bool IsPoeticStyle
		///       bool IsIntroductionOutlineStyle
		/// no alternative for the Uncapitalized Styles check
		///
		/// Checks will use the ParaStyleName or CharStyleName below, and not any of the alternatives.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ParaStyleName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the character style (if any).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string CharStyleName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Scripture reference as a string,
		/// suitable for displaying in the UI
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ScrRefString { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This property is primarily for the chapter/verse check and allows the
		/// check to set the beginning reference of a missing chapter or verse range.
		/// If the missing verse is not part of a range, then this just contains
		/// the missing verse reference and the MissingEndRef is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		BCVRef MissingStartRef { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This property is primarily for the chapter/verse check and allows the
		/// check to set the ending reference of a missing chapter or verse range.
		/// If the missing verse is not part of a range, then this is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		BCVRef MissingEndRef { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a deep copy of this text token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITextToken Clone();
	}
}