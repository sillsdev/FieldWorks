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
		/// RootBox currently being edited.
		/// </summary>
		IVwRootBox EditedRootBox { get; }

		/// <summary>
		/// Flag indicating cache or writing system is available.
		/// </summary>
		bool GotCacheOrWs { get; }

		/// <summary>
		/// Perform any processing needed immediately prior to a paste operation.
		/// </summary>
		void PrePasteProcessing();
	}
}