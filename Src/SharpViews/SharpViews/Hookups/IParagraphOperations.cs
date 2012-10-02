using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// This interface is designed to be implemented by SharpViews clients to allow them to control
	/// editing operations at the paragraph level. Typical cases and behaviors:
	/// 1. User hits return, with IP at the end of a paragraph. Client wishes to insert an empty
	/// following paragraph, possibly following a rule to apply the 'next' style derived from the
	/// style of the original paragraph.
	/// 2. User hits retun, with IP at the start of a paragraph. Client wishes to insert an empty
	/// preceding paragraph (typically in the same style).
	/// 3. User hits return, with IP in the middle of a paragraph. Client wishes t insert a new
	/// paragraph (typically in the same style) following the current one, and divide the text between them.
	/// Possibly associated annotations need to move with the part of the text that goes to the new paragaraph.
	/// 4. User hits backspace, with IP at the start of a paragraph other than the first one. Client
	/// wishes to delete the paragraph containing the IP, and move its text (and any associated annotations)
	/// into the previous paragraph.
	/// 5. User hits Delete, with IP at the end of a paragraph other than the last one. Client wishes to
	/// act as in (4).
	/// 6. User hits backspace or delete, with a range selection extending across two or more paragraphs.
	/// Client wishes to delete any paragraphs entirely selected (except possibly if the selection is
	/// exactly all of one paragraph we might keep an empy paragraph?). The last partly-selected paragraph
	/// is also deleted, moving any unselected text into the first partly-selected paragraph.
	/// 7. User pastes a string containing newlines. Client wishes to add any text before the first newline
	/// to the current paragraph, make new complete paragraphs out of each following newline-terminated chunk,
	/// and make one more paragraph containing first any material after the last newline, then any material
	/// from the original text after the IP. If the source is a TsString or other styled material, the
	/// client may wish to obtain styles for the new paragraphs from it, e.g., based on styles applied
	/// to the embedded newlines.
	///
	/// The client may wish to do complex things with related information, e.g., attempting to partition back
	/// translations and move them with the text.
	///
	/// It is also theoretically possible that what is displayed is a complex structure such as a
	/// presentation of a lexical entry, and the client wishes to implement some algorithm for splitting
	/// an entry. For example if the IP is at a boundary between senses, it might make a new entry and
	/// move the following senses to it. We haven't attempted this in code based on the old Views, so YAGNI
	/// may apply, but it is worth keeping in mind.
	///
	/// A further complication is that any of the insert operations (or simply inserting a typed character)
	/// may be combined with deleting an existing complex range selection, and the client may wish to have
	/// both operations be part of a single Unit of Work. This problem is not unique to paragraph operations;
	/// it can happen deleting a single paragraph run and typing a character. Currently the client is
	/// responsible to create a UOW to wrap the process of sending the view a character to process. However,
	/// this does complicate things, because for the insert operations, the view may never be in a stable
	/// state where all the effects of deleting the range have occurred. Thus we cannot simply have an
	/// interface in which the Insert operations are passed an insertion point, possibly resulting from the
	/// delete. Nor can the SharpViews code reliably determine what effect the delete will have. Conceivably
	/// a client might decide to delete the whole of all the partly-selected objects for a multi-paragraph delete.
	/// For this reason the interface includes overloads of each method, taking either an insertion
	/// point or a range selection.
	///
	/// Operations involving IParagraphOperations occur when the SharpViews code detects that one of the
	/// user actions descibed above is occurring, and that there is a SequenceHookup associated with
	/// the current paragraph which has a non-empty ParagraphOperations property.
	/// </summary>
	public interface IParagraphOperations
	{
		/// <summary>
		/// Insert a new paragraph, after the user types Enter, where the previous selection
		/// is an insertion point at the end of the paragraph.
		/// </summary>
		bool InsertFollowingParagraph(InsertionPoint ip, out Action makeSelection);

		/// <summary>
		/// Insert a new paragraph, after the user types Enter, where the previous selection
		/// is a range at the end of the paragraph.
		/// </summary>
		bool InsertFollowingParagraph(RangeSelection range, out Action makeSelection);

		/// <summary>
		/// Insert a new paragraph, after the user types Enter, where the previous selection
		/// is an insertion point in the middle of the paragraph.
		/// </summary>
		bool SplitParagraph(InsertionPoint ip, out Action makeSelection);

		/// <summary>
		/// Insert a new paragraph, after the user types Enter, where the previous selection
		/// is an insertion point at the start of the paragraph.
		/// </summary>
		bool InsertPrecedingParagraph(InsertionPoint ip, out Action makeSelection);

		/// <summary>
		/// Replace the text in the range, presumed to extend across more than one paragraph,
		/// with the supplied text.
		/// This one serves also for backspace at start of paragraph and delete at end, since
		/// these can readily be transformed into deleting a range from the end of one para
		/// to the start of the next. The given range is replace by a simple string.
		/// </summary>
		bool InsertString(RangeSelection range, string text, out Action makeSelection);

		// Todo: other range selection overloads, insert TsString.
	}

	/// <summary>
	/// Extensions to IParagraphOperations that require the object type that can be inserted.
	/// </summary>
	public interface IParagraphOperations<T> : IParagraphOperations
	{
		/// <summary>
		/// When a paragraph operations is passed as the PO of a particular hookup, the hookup is
		/// passed to this method.
		/// </summary>
		SequenceHookup<T> Hookup { get; set; }
	}

	/// <summary>
	/// Marks a hookup which has a ParagraphOperations method.
	/// </summary>
	internal interface IHaveParagagraphOperations
	{
		IParagraphOperations GetParagraphOperations();
	}

	/// <summary>
	/// If a paragraphOperations implements this, and the Fetcher for a hookup using that PO returns a
	/// suitable list, it will be passed on.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal interface IParaOpsList<T>
	{
		IList<T> List { get; set; }
	}
}
