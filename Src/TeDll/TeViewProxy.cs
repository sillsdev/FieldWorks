// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ViewProxy.cs
// Responsibility: TE Team

using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls.SplitGridView;

namespace SIL.FieldWorks.TE
{
	#region TeViewProxy (abstract)
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Proxy that can create a TE view
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal abstract class TeViewProxy : ViewProxy
	{
		/// <summary>The main window.</summary>
		protected readonly TeMainWnd m_mainWnd;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TeViewProxy"/> class
		/// </summary>
		/// <param name="mainWnd">The main window.</param>
		/// <param name="name">The name of the view.</param>
		/// <param name="fEditable"><c>true</c> if this view is editable, <c>false</c> if
		/// read-only.</param>
		/// ------------------------------------------------------------------------------------
		public TeViewProxy(TeMainWnd mainWnd, string name, bool fEditable)
			: base(name, fEditable)
		{
			m_mainWnd = mainWnd;
		}
	}
	#endregion

	#region TeDraftViewProxy (abstract)
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Proxy that can create a TE view (knows whether or not it has vernacular or back-
	/// translation data).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal abstract class TeDraftViewProxy : TeViewProxy
	{
		/// <summary>The view spec</summary>
		protected readonly TeViewType m_viewType;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TeDraftViewProxy"/> class with a flag
		/// indicating whether it is intended to host a back translation
		/// </summary>
		/// <param name="mainWnd">The main window.</param>
		/// <param name="name">The name of the view.</param>
		/// <param name="fEditable"><c>true</c> if this view is editable, <c>false</c> if
		/// read-only.</param>
		/// <param name="viewType">Bit-flags indicating type of view.</param>
		/// ------------------------------------------------------------------------------------
		public TeDraftViewProxy(TeMainWnd mainWnd, string name, bool fEditable, TeViewType viewType)
			: base(mainWnd, name, fEditable)
		{
			m_viewType = viewType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the writing system to use if this is a back translation view;
		/// otherwise -1.
		/// </summary>
		/// <param name="wrapper">The wrapper that is to host the view being created.</param>
		/// ------------------------------------------------------------------------------------
		protected int GetBtWs(ViewWrapper wrapper)
		{
			return (m_viewType & TeViewType.BackTranslation) == 0 ? -1 :
				m_mainWnd.GetBackTranslationWsForView(wrapper.Name);
		}
	}
	#endregion

	#region TeScrDraftViewProxy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Proxy that can create a TE Scripture draft view.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class TeScrDraftViewProxy : TeDraftViewProxy
	{
		/// <summary><c>true</c> to display the view as a table.</summary>
		private readonly bool m_showInTable;
		/// <summary><c>true</c> to call MakeRoot(), <c>false</c> if caller will call
		/// MakeRoot().</summary>
		private readonly bool m_makeRootAutomatically;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TeScrDraftViewProxy"/> class.
		/// </summary>
		/// <param name="mainWnd">The main window.</param>
		/// <param name="name">The name of the view.</param>
		/// <param name="fEditable"><c>true</c> if this draft view is editable, <c>false</c>
		/// if it is read-only.</param>
		/// <param name="fShowInTable"><c>true</c> to display the view as a table.</param>
		/// <param name="fMakeRootAutomatically"><c>true</c> to call MakeRoot(), <c>false</c>
		/// if caller will call MakeRoot().</param>
		/// <param name="viewType">Bit-flags indicating type of view.</param>
		/// ------------------------------------------------------------------------------------
		public TeScrDraftViewProxy(TeMainWnd mainWnd, string name, bool fEditable,
			bool fShowInTable, bool fMakeRootAutomatically, TeViewType viewType) :
			base(mainWnd, name, fEditable, viewType)
		{
			m_showInTable = fShowInTable;
			m_makeRootAutomatically = fMakeRootAutomatically;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method to create the view when needed
		/// </summary>
		/// <param name="host">The control that will host (or "wrap") the view (can be
		/// <c>null</c>)</param>
		/// <returns>The created view</returns>
		/// ------------------------------------------------------------------------------------
		public override Control CreateView(Control host)
		{
			ViewWrapper wrapper = (ViewWrapper)host;
			DraftView draftView = new DraftView(m_mainWnd.Cache, m_mainWnd.Handle.ToInt32(),
				m_mainWnd.App, m_name, m_editable, m_showInTable, m_makeRootAutomatically,
				m_viewType, GetBtWs(wrapper), m_mainWnd.App);

			m_mainWnd.RegisterFocusableView(draftView);
			draftView.TheViewWrapper = wrapper;
			return draftView;
		}
	}
	#endregion

	#region TeFootnoteDraftViewProxy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Proxy that can create a TE Footnote draft view.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class TeFootnoteDraftViewProxy : TeDraftViewProxy
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TeFootnoteDraftViewProxy"/> class.
		/// </summary>
		/// <param name="mainWnd">The main window.</param>
		/// <param name="name">The name of the view.</param>
		/// <param name="fEditable"><c>true</c> if this draft view is editable, <c>false</c>
		/// if it is read-only.</param>
		/// <param name="fBackTrans"><c>true</c> if this view displays a back translation.</param>
		/// ------------------------------------------------------------------------------------
		public TeFootnoteDraftViewProxy(TeMainWnd mainWnd, string name, bool fEditable,
			bool fBackTrans)
			: base(mainWnd, name, fEditable, TeViewType.FootnoteView | TeViewType.Horizontal |
			(fBackTrans ? TeViewType.BackTranslation : TeViewType.Scripture))
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method to create the view when needed
		/// </summary>
		/// <param name="host">The control that will host (or "wrap") the view (can be
		/// <c>null</c>)</param>
		/// <returns>The created view</returns>
		/// ------------------------------------------------------------------------------------
		public override Control CreateView(Control host)
		{
			ViewWrapper wrapper = (ViewWrapper)host;
			FootnoteView footnoteView = new FootnoteView(m_mainWnd.Cache,
				m_mainWnd.Handle.ToInt32(), m_mainWnd.App, m_name, m_editable, m_viewType,
				GetBtWs(wrapper), wrapper.DraftView);
			m_mainWnd.RegisterFocusableView(footnoteView);
			return footnoteView;
		}
	}
	#endregion

	#region DraftStylebarProxy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds information necessary to create a style bar.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DraftStylebarProxy : TeViewProxy
	{
		/// <summary><c>true</c> if this stylebar is for footnotes.</summary>
		private bool m_forFootnotes;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DraftStylebarProxy"/> class.
		/// </summary>
		/// <param name="mainWnd">The main window.</param>
		/// <param name="name">The root name of the view ("StyleBar" will be appended).</param>
		/// <param name="fForFootnotes"><c>true</c> if this stylebar is for footnotes.</param>
		/// ------------------------------------------------------------------------------------
		public DraftStylebarProxy(TeMainWnd mainWnd, string name, bool fForFootnotes) :
			base(mainWnd, name + "StyleBar", false)
		{
			m_forFootnotes = fForFootnotes;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method to create the view when needed
		/// </summary>
		/// <param name="host">The control that will host (or "wrap") the view (can be
		/// <c>null</c>)</param>
		/// <returns>The created view</returns>
		/// ------------------------------------------------------------------------------------
		public override Control CreateView(Control host)
		{
			DraftStyleBar styleBar = new DraftStyleBar(m_mainWnd.Cache, m_forFootnotes,
				m_mainWnd.Handle.ToInt32());
			styleBar.Name = m_name;
			styleBar.AccessibleName = styleBar.Name;
			return styleBar;
		}
	}
	#endregion

	#region CheckingViewProxy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds information necessary to create the Scripture drafting portion of a Checking view.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class CheckingViewProxy : TeViewProxy
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CheckingViewProxy"/> class.
		/// </summary>
		/// <param name="mainWnd">The main window.</param>
		/// <param name="name">The name of the view.</param>
		/// ------------------------------------------------------------------------------------
		public CheckingViewProxy(TeMainWnd mainWnd, string name) :
			base(mainWnd, name, true)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method to create the view when needed
		/// </summary>
		/// <param name="host">The control that will host (or "wrap") the view (can be
		/// <c>null</c>)</param>
		/// <returns>The created view</returns>
		/// ------------------------------------------------------------------------------------
		public override Control CreateView(Control host)
		{
			var draftViewProxy = new TeScrDraftViewProxy(m_mainWnd, m_name, m_editable, false,
				false, TeViewType.DraftView);

			string name = m_name.Replace("DraftView", string.Empty);

			// ENHANCE: If users ever request it, we could also make it possible to show the
			// style pane for Editorial Checks and Biblical Terms views.
			//var draftStylebarProxy = new DraftStylebarProxy(m_mainWnd, name + "DraftStyles", false);

			var footnoteViewProxy = new TeFootnoteDraftViewProxy(m_mainWnd, name + "FootnoteView",
				m_editable, false);

			//var footnoteStylebarInfo = new DraftStylebarProxy(m_mainWnd, name + "FootnoteStyles", true);

			SimpleDraftViewWrapper checkingDraftView = new SimpleDraftViewWrapper(
				TeMainWnd.kDraftViewWrapperName, m_mainWnd, m_mainWnd.Cache, m_mainWnd.StyleSheet,
				m_mainWnd.SettingsKey, draftViewProxy, null /*draftStylebarProxy*/,
				footnoteViewProxy, null /*footnoteStylebarInfo*/);

			checkingDraftView.Name = m_name + "Wrapper";
			checkingDraftView.AccessibleName = m_name;
			return checkingDraftView;
		}
	}
	#endregion
}
