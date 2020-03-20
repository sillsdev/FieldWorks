// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This interface, implemented currently by SimpleRootSite and PublicationControl,
	/// defines the functions that are not inherited from UserControl which must be
	/// implemented by the EditingHelper client. One argument to the constructor for
	/// EditingHelper is an IEditingCallbacks. It must be capable of being cast to
	/// UserControl.
	/// </summary>
	public interface IEditingCallbacks
	{
		/// <summary>
		/// See the comments on m_wsPending for SimpleRootSite. Used to manage
		/// writing system changes caused by selecting a system input language.
		/// </summary>
		int WsPending { get; set; }

		/// <summary>
		/// Typically the AutoScollPosition of the control, SimpleRootSite
		/// handles this specially.
		/// </summary>
		Point ScrollPosition { get; set; }

		/// <summary>
		/// Return an indication of the behavior of some of the special keys (arrows, home,
		/// end).
		/// </summary>
		/// <param name="chw">Key value</param>
		/// <param name="ss">Shift status</param>
		/// <returns>Return <c>0</c> for physical behavior, <c>1</c> for logical behavior.
		/// </returns>
		/// <remarks>Physical behavior means that left arrow key goes to the left regardless
		/// of the direction of the text; logical behavior means that left arrow key always
		/// moves the IP one character (possibly plus diacritics, etc.) in the underlying text,
		/// in the direction that is to the left for text in the main paragraph direction.
		/// So, in a normal LTR paragraph, left arrow decrements the IP position; in an RTL
		/// paragraph, it increments it. Both produce a movement to the left in text whose
		/// direction matches the paragraph ("downstream" text). But where there is a segment
		/// of upstream text, logical behavior will jump almost to the other end of the
		/// segment and then move the 'wrong' way through it.
		/// </remarks>
		CkBehavior ComplexKeyBehavior(int chw, VwShiftStatus ss);

		/// <summary>
		/// Scroll all the way to the top of the document.
		/// </summary>
		void ScrollToTop();

		/// <summary>
		/// Scroll all the way to the end of the document.
		/// </summary>
		void ScrollToEnd();

		/// <summary>
		/// Show the context menu for the specified root box at the location of
		/// its selection (typically an IP).
		/// </summary>
		void ShowContextMenuAtIp(IVwRootBox rootb);

		/// <summary>
		/// Gets the (estimated) height of one line
		/// </summary>
		int LineHeight { get; }

		/// <summary>
		/// RootBox currently being edited.
		/// </summary>
		IVwRootBox EditedRootBox { get; }

		/// <summary>
		/// Flag indicating cache or writing system is available.
		/// </summary>
		bool GotCacheOrWs { get; }

		/// <summary>
		/// Gets the writing system for the HVO. This could either be the vernacular or
		/// analysis writing system.
		/// </summary>
		/// <param name="hvo">HVO</param>
		/// <returns>Writing system</returns>
		int GetWritingSystemForHvo(int hvo);

		/// <summary>
		/// Perform any processing needed immediately prior to a paste operation.
		/// </summary>
		void PrePasteProcessing();

		/// <summary>
		/// If we need to make a selection, but we can't because edits haven't been updated in
		/// the view, this method requests creation of a selection after the unit of work is
		/// complete. It will also scroll the selection into view.
		/// Derived classes should implement this if they have any hope of supporting multi-
		/// paragraph editing.
		/// </summary>
		/// <param name="helper">The selection to restore</param>
		void RequestVisibleSelectionAtEndOfUow(SelectionHelper helper);
	}
}